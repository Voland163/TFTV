using Base.AI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    /// <summary>
    /// How the AI picks its next action among the options it scored.
    ///
    /// 1. Haven attackers go for people first. In a haven defense the attackers spent most of their attacks on the
    ///    haven's structures (and civilians): a building is big, close, always fully in view and easy to hit, so a shot
    ///    at it scores better than one at a soldier in cover. Now an attack on a structure or a civilian only counts
    ///    when the attacker has nothing to attack on a player unit or a haven defender. Scores are left alone: each
    ///    action's targets lose their structures and civilians before its best few are kept for the final pick
    ///    (AIFaction.GetTopTargets) if any of them is a person, and at the pick (AIFaction.SelectTarget) the structure
    ///    and civilian options are dropped if any option targets a person. With nobody to attack, the haven itself
    ///    is fair game as in vanilla.
    ///
    /// 2. A clear best is taken. Vanilla keeps every option scoring at least 75% of the best (AISettingsDef
    ///    MinConsideredActionScoreRatio) and draws one at random weighted by score, so an option a third behind still
    ///    won about four times in ten. Now only options within <see cref="RandomBand"/> of the best are drawn from:
    ///    a best that leads the rest by more than that is always picked.
    /// </summary>
    internal static class TFTVAITargetSelection
    {
        /// <summary>Options scoring at least this share of the best are drawn from at random, weighted by score.</summary>
        internal const float RandomBand = 0.9f;

        private static readonly AccessTools.FieldRef<AIFaction, AISettingsDef> SettingsRef = AccessTools.FieldRefAccess<AIFaction, AISettingsDef>("_aiSettingsDef");

        /// <summary>Drops the structure and civilian options when any option targets a person. True if any were dropped.</summary>
        private static bool DropHavenSide(IList<AIScoredTarget> targets, out TacticalActorBase attacker, out AIScoredTarget onPerson)
        {
            attacker = null;
            onPerson = null;

            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            attacker = targets.FirstOrDefault(t => t != null)?.Actor as TacticalActorBase;
            if (attacker == null || !IsHavenDefense(attacker.TacticalLevel))
            {
                return false;
            }

            TacticalActorBase from = attacker;
            onPerson = targets.FirstOrDefault(t => t != null && t.Score > 0f && IsCombatantTarget(from, TargetActor(t)));
            if (onPerson == null)
            {
                return false;
            }

            bool dropped = false;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] != null && IsHavenSideTarget(from, TargetActor(targets[i])))
                {
                    targets.RemoveAt(i);
                    dropped = true;
                }
            }

            return dropped;
        }

        private static MissionTagDef _havenDefenseTag;

        private static bool IsHavenDefense(TacticalLevelController level)
        {
            if (_havenDefenseTag == null)
            {
                _havenDefenseTag = TFTVMain.Main.DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");
            }

            TacMissionTypeDef missionType = level?.TacMission?.MissionData?.MissionType;

            return missionType != null && _havenDefenseTag != null
                && (missionType.MissionTypeTag == _havenDefenseTag || (missionType.MissionTags != null && missionType.MissionTags.Contains(_havenDefenseTag)));
        }

        /// <summary>A structure or a civilian that a haven attacker would rather leave for later.</summary>
        private static bool IsHavenSideTarget(TacticalActorBase attacker, TacticalActorBase target)
        {
            if (attacker == null || target == null || attacker.RelationTo(target) != PhoenixPoint.Common.Core.FactionRelation.Enemy)
            {
                return false;
            }

            ActorType type = AIUtil.GetActorType(target);
            return (type == ActorType.KeyStructure || type == ActorType.Civilian) && IsHavenDefense(attacker.TacticalLevel);
        }

        /// <summary>A player unit or a haven defender: an enemy combatant.</summary>
        private static bool IsCombatantTarget(TacticalActorBase attacker, TacticalActorBase target)
        {
            return attacker != null && target != null && target != attacker && target.IsAlive
                && attacker.RelationTo(target) == PhoenixPoint.Common.Core.FactionRelation.Enemy
                && AIUtil.GetActorType(target) == ActorType.Combatant;
        }

        private static TacticalActorBase TargetActor(AIScoredTarget scored)
        {
            return (scored?.Target as TacAITarget)?.Actor;
        }

        /// <summary>Spare distance beyond the blast radius, for scatter and the blast catching the shooter's edge.</summary>
        private const float OwnBlastMargin = 1f;

        /// <summary>
        /// The radius of a weapon's blast, 0 if it has none. Only area payloads (Sphere, Parabola) explode: AoeRadius is
        /// junk on the others (Infinity on laser rifles and shotguns), and WeaponDef.AreaRadius ignores it for Melee and
        /// DirectLine the same way.
        /// </summary>
        internal static float BlastRadius(PhoenixPoint.Tactical.Entities.Weapons.WeaponDef def)
        {
            PhoenixPoint.Tactical.Entities.DamagePayload payload = def?.DamagePayload;
            if (payload == null)
            {
                return 0f;
            }

            if (payload.DamageDeliveryType != PhoenixPoint.Tactical.Entities.DamageDeliveryType.Sphere
                && payload.DamageDeliveryType != PhoenixPoint.Tactical.Entities.DamageDeliveryType.Parabola)
            {
                return 0f;
            }

            float radius = payload.AoeRadius;
            return float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f ? 0f : radius;
        }

        private static readonly HashSet<string> OwnBlastLogged = new HashSet<string>();
        private static int _ownBlastTurn = -1;

        /// <summary>
        /// A shot with an explosive (rocket, grenade) from a spot inside its own blast scores nothing. A Neu Jericho leader
        /// moved next to a soldier and fired its rocket launcher at her from 1 m: 37 damage to her, 60 to itself
        /// (playtest 2026-11-23). Vanilla scores the shot by the damage to the target and never looks at the shooter.
        /// </summary>
        [HarmonyPatch(typeof(PhoenixPoint.Tactical.AI.Considerations.AIAttackPositionConsideration), "EvaluateWithShootAbility")]
        internal static class AIAttackPositionConsideration_EvaluateWithShootAbility_OwnBlast_Patch
        {
            public static void Postfix(IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (__result <= 0f || !(actor is TacticalActor shooter) || !(target is TacAITarget tacTarget) || tacTarget.Actor == null
                        || !(tacTarget.Equipment is PhoenixPoint.Tactical.Entities.Weapons.Weapon weapon))
                    {
                        return;
                    }

                    float radius = BlastRadius(weapon.WeaponDef);
                    if (radius <= 0f)
                    {
                        return;
                    }

                    UnityEngine.Vector3 from = tacTarget.IsPosValid ? tacTarget.Pos : shooter.Pos;
                    float distance = UnityEngine.Vector3.Distance(from, tacTarget.Actor.Pos);

                    if (distance >= radius + OwnBlastMargin)
                    {
                        return;
                    }

                    __result = 0f;

                    int turn = shooter.TacticalFaction?.TurnNumber ?? -1;
                    if (turn != _ownBlastTurn)
                    {
                        _ownBlastTurn = turn;
                        OwnBlastLogged.Clear();
                    }

                    if (OwnBlastLogged.Add($"{shooter.name}|{weapon.WeaponDef.name}"))
                    {
                        TFTVLogger.Always($"TFTV AI own blast: {shooter.DisplayName} won't fire {weapon.WeaponDef.name} (blast {radius:0.#}m) at anyone within {radius + OwnBlastMargin:0.#}m of where it fires from");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>An action's targets, before its best few are kept: the haven's structures and civilians go if a person is among them.</summary>
        [HarmonyPatch(typeof(AIFaction), nameof(AIFaction.GetTopTargets))]
        internal static class AIFaction_GetTopTargets_HavenAttackers_Patch
        {
            public static void Prefix(IList<AIScoredTarget> scoredTargets)
            {
                try
                {
                    DropHavenSide(scoredTargets, out _, out _);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AIFaction), "SelectTarget")]
        internal static class AIFaction_SelectTarget_TargetSelection_Patch
        {
            private static bool Prefix(AIFaction __instance, IList<AIScoredTarget> scoredTargets, ref AIScoredTarget __result)
            {
                try
                {
                    if (scoredTargets == null || scoredTargets.Count == 0)
                    {
                        return true;
                    }

                    List<AIScoredTarget> options = scoredTargets.Where(s => s != null && s.Score > 0f).ToList();
                    if (options.Count == 0)
                    {
                        return true;
                    }

                    if (DropHavenSide(options, out TacticalActorBase attacker, out AIScoredTarget onPerson))
                    {
                        TFTVLogger.Always($"TFTV AI haven attackers: {attacker.DisplayName} leaves the haven's structures and civilians: it can go after {TargetActor(onPerson)?.DisplayName} ({onPerson.Action?.ActionDef?.name})");
                    }

                    if (options.Count == 0)
                    {
                        return true;
                    }

                    float best = options.Max(s => s.Score);
                    AISettingsDef settings = SettingsRef(__instance);
                    int considered = Math.Max(1, settings != null && settings.NumberOfActionsConsidered > 0 ? settings.NumberOfActionsConsidered : options.Count);

                    List<AIScoredTarget> pool = options
                        .Where(s => s.Score >= best * RandomBand)
                        .OrderByDescending(s => s.Score)
                        .Take(considered)
                        .ToList();

                    __result = WeightedPick(pool);
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        private static AIScoredTarget WeightedPick(List<AIScoredTarget> pool)
        {
            if (pool.Count == 1)
            {
                return pool[0];
            }

            float total = pool.Sum(s => s.Score);
            float roll = UnityEngine.Random.value * total;

            foreach (AIScoredTarget scored in pool)
            {
                roll -= scored.Score;
                if (roll <= 0f)
                {
                    return scored;
                }
            }

            return pool[pool.Count - 1];
        }
    }
}
