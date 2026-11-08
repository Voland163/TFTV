using Base.AI.Defs;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;
using static TFTV.TFTVSuppression;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        #region 8. suppress

        /// <summary>
        /// Suppress: a MoveAndShoot clone that fires to pin a soldier down rather than to hurt it. Only does anything
        /// with the suppression option on. Moderate suppression since its last turn takes away a soldier's overwatch
        /// and return fire (TFTVSuppression), so that is what the shot is scored on: how much of the way to Moderate
        /// the actor's bursts this turn would take it.
        ///
        /// Two ways in:
        /// - Ordered (TFTVAISquadPlanner's suppression pass). The actor goes for the soldier it was told to, scored
        ///   above any ordinary shot, and ignores everyone else with this action until the order is done.
        /// - On its own. Only against a soldier on overwatch, scored like a middling shot, and only worth much when
        ///   this actor can pin it alone.
        ///
        /// An early exit keeps the (expensive) movement-zone target generation off unless one of the two can apply.
        /// </summary>
        internal static class SuppressAI
        {
            private const float OrderedMinScore = 0.8f;
            private const float OrderedMaxScore = 1f;
            private const float UnorderedMaxScore = 0.45f;

            /// <summary>Share of a burst expected to land on or pass within a tile of the target.</summary>
            internal const float NearShare = 0.85f;

            /// <summary>Bursts counted towards one turn's suppression.</summary>
            private const int MaxBurstsCounted = 3;

            /// <summary>Overwatching soldiers further than this don't wake the action up.</summary>
            private const float OverwatchSearchRadius = 40f;

            internal static AIActionMoveAndAttackDef ActionDef;
            internal static AIAttackPositionConsiderationDef ConsiderationDef;
            private static AIHasEquipmentConsiderationDef _shouldSuppressDef;

            /// <summary>Templates of the AI that carries guns; each gets the action only if it already has MoveAndShoot.</summary>
            private static readonly string[] Templates =
            {
                SoldierAITemplate,
                "Fishman_AIActionsTemplateDef",
                "Crabman_AIActionsTemplateDef",
                "CrabmanGunner_AIActionsTemplateDef",
            };

            internal static void CreateDefs()
            {
                try
                {
                    AIActionMoveAndAttackDef moveAndShoot = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");

                    ActionDef = Helper.CreateDefFromClone(moveAndShoot, "c4e81a37-5d92-4f06-b8a3-71e0d5c29f14", "TFTV_MoveAndSuppress_AIActionDef");

                    ConsiderationDef = ClonePrivateAttackPositionConsideration(ActionDef,
                        "1b7f3d60-a2c9-4e85-9d14-3e6c80f5b2a7", "TFTV_SuppressAttackPosition_AIConsiderationDef");

                    _shouldSuppressDef = Helper.CreateDefFromClone(DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef"),
                        "8e2a5c91-46d3-4b70-a1f8-c09d7e3b6254", "TFTV_ShouldSuppress_AIConsiderationDef");

                    PeekAndFallBackAI.AddEarlyExit(ActionDef, _shouldSuppressDef);

                    foreach (string templateName in Templates)
                    {
                        AIActionsTemplateDef template = DefCache.GetDef<AIActionsTemplateDef>(templateName);

                        if (template == null || Array.IndexOf(template.ActionDefs, moveAndShoot) < 0)
                        {
                            TFTVLogger.Always($"TFTV AI: {templateName} has no MoveAndShoot; no Suppress action for it");
                            continue;
                        }

                        AddActionToTemplate(templateName, ActionDef);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (_shouldSuppressDef == null || def != _shouldSuppressDef)
                {
                    return false;
                }

                score = actor != null && ShouldConsider(actor) ? 1f : 0f;
                return true;
            }

            private static bool ShouldConsider(TacticalActor actor)
            {
                if (!SuppressionRuntime.IsSuppressionEnabled || !actor.IsAlive)
                {
                    return false;
                }

                if (TFTVAISquadPlanner.GetSuppressOrder(actor) != null)
                {
                    return true;
                }

                // Unordered: only an overwatching soldier this actor could get a burst on this turn, so the extra
                // movement-zone search isn't run for gunners nowhere near one.
                float reach = 0f;

                foreach (Weapon weapon in actor.Equipments.GetWeapons())
                {
                    if (CanFire(weapon))
                    {
                        reach = Mathf.Max(reach, weapon.WeaponDef.EffectiveRange);
                    }
                }

                if (reach <= 0f)
                {
                    return false;
                }

                reach = Mathf.Min(reach + actor.MaxMoveRange * 0.5f, OverwatchSearchRadius);

                foreach (TacticalActorBase enemyBase in actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.Combatant)))
                {
                    if (enemyBase is TacticalActor enemy && IsOverwatchToPin(enemy)
                        && (enemy.Pos - actor.Pos).sqrMagnitude <= reach * reach)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>A live soldier on overwatch that suppression can still pin down.</summary>
            internal static bool IsOverwatchToPin(TacticalActor enemy)
            {
                return enemy != null && enemy.IsAlive && enemy.Status != null && enemy.Status.HasStatus<OverwatchStatus>()
                    && !SuppressionRuntime.IsSuppressionExcluded(enemy) && !SuppressionRuntime.BlocksReactionFire(enemy);
            }

            /// <summary>Suppression points one burst is expected to add: about one per round that comes close.</summary>
            internal static float ExpectedPointsPerBurst(Weapon weapon)
            {
                return Mathf.Max(1, weapon.WeaponDef.DamagePayload.AutoFireShotCount) * NearShare;
            }

            /// <summary>Bursts the actor can afford with <paramref name="actionPoints"/>, capped at MaxBurstsCounted.</summary>
            internal static int AffordableBursts(TacticalActor actor, Weapon weapon, float actionPoints)
            {
                float apCost = actor.Equipments.GetAPToUseEquipment(weapon);

                if (apCost <= 0f)
                {
                    return 1;
                }

                return Mathf.Clamp(Mathf.FloorToInt((actionPoints + 0.001f) / apCost), 0, MaxBurstsCounted);
            }

            /// <summary>A gun that can fire now: usable, loaded, and its shoot ability not disabled.</summary>
            internal static bool CanFire(Weapon weapon)
            {
                if (weapon?.WeaponDef == null || !PeekAndFallBackAI.IsGun(weapon) || !weapon.IsUsable || !weapon.HasCharges)
                {
                    return false;
                }

                ShootAbility shootAbility = weapon.DefaultShootAbility;

                return shootAbility != null
                    && shootAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints);
            }

            /// <summary>Whether <paramref name="weapon"/> has a shot at <paramref name="target"/> from <paramref name="from"/>, via the AI's own cache.</summary>
            internal static bool HasShot(TacticalActor actor, Weapon weapon, TacticalActorBase target, Vector3 from)
            {
                TacticalAbilityTarget abilityTarget = new TacticalAbilityTarget
                {
                    Actor = target,
                    ActorGridPosition = target.Pos
                };

                List<KeyValuePair<TacticalAbilityTarget, float>> shots = actor.TacticalFaction.AIBlackboard.GetShootTargetsWithScores(weapon, abilityTarget, from);

                return shots != null && shots.Count > 0 && shots[0].Key != null;
            }

            /// <summary>
            /// Diagnostic for tuning ExpectedPointsPerBurst: each AI gun burst, with what the estimate expects it to add
            /// and what the target still needs. The [Suppression] lines that follow show what actually landed.
            /// </summary>
            internal static void LogBurst(ShootAbility ability, TacticalAbilityTarget abilityTarget)
            {
                TacticalActor shooter = ability?.TacticalActor;
                Weapon weapon = ability?.Weapon;

                if (!SuppressionRuntime.IsSuppressionEnabled || shooter == null || shooter.IsControlledByPlayer || weapon?.WeaponDef == null
                    || !PeekAndFallBackAI.IsGun(weapon) || !shooter.TacticalFaction.IsPlayingTurn
                    || !((abilityTarget?.GetTargetActor() ?? abilityTarget?.Actor) is TacticalActor target))
                {
                    return;
                }

                TFTVLogger.Always($"[Suppression] AI burst: {shooter.DisplayName} {weapon.WeaponDef.name} " +
                    $"({weapon.WeaponDef.DamagePayload.AutoFireShotCount} x {weapon.WeaponDef.DamagePayload.ProjectilesPerShot}) at {target.DisplayName} " +
                    $"{Vector3.Distance(shooter.Pos, target.Pos):0}m, overwatch {target.Status.HasStatus<OverwatchStatus>()}; " +
                    $"expects +{ExpectedPointsPerBurst(weapon):0.#}, needs {SuppressionRuntime.PointsToModerate(target):0.#}");
            }

            /// <summary>Replaces AIAttackPositionConsideration's damage score for the Suppress action.</summary>
            internal static float Score(TacticalActor actor, TacAITarget aiTarget)
            {
                try
                {
                    if (!SuppressionRuntime.IsSuppressionEnabled || !(aiTarget.Equipment is Weapon weapon) || !CanFire(weapon))
                    {
                        return 0f;
                    }

                    if (!(aiTarget.Actor is TacticalActor target) || !target.IsAlive || actor.RelationTo(target) != FactionRelation.Enemy
                        || SuppressionRuntime.IsSuppressionExcluded(target) || SuppressionRuntime.BlocksReactionFire(target))
                    {
                        return 0f;
                    }

                    TFTVAISquadPlanner.SquadOrder order = TFTVAISquadPlanner.GetSuppressOrder(actor);
                    bool ordered = order != null && order.Target == target;

                    // Under orders the actor stays on its target; ordinary shots at anyone else still score as usual.
                    if (order != null && !ordered)
                    {
                        return 0f;
                    }

                    if (!ordered && !target.Status.HasStatus<OverwatchStatus>())
                    {
                        return 0f;
                    }

                    float need = SuppressionRuntime.PointsToModerate(target);

                    if (need <= 0f || float.IsInfinity(need))
                    {
                        return 0f;
                    }

                    int bursts = AffordableBursts(actor, weapon, actor.CharacterStats.ActionPoints - aiTarget.PathLength);

                    if (bursts == 0 || !HasShot(actor, weapon, target, aiTarget.Pos))
                    {
                        return 0f;
                    }

                    float progress = Mathf.Clamp01(ExpectedPointsPerBurst(weapon) * bursts / need);

                    return ordered
                        ? Mathf.Lerp(OrderedMinScore, OrderedMaxScore, progress)
                        : UnorderedMaxScore * progress * progress;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return 0f;
                }
            }
        }

        #endregion
    }
}
