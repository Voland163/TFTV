using Base.AI;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// The Scylla's guns. TFTV readies them (Queen_StartPreparing_AbilityDef) after every Spit or Sonic Blast, aimed
        /// at the head attack's target; the PreparingStatus fires them at that point at the start of the Scylla's next
        /// turn, from where she stands, in a cone the width of the gun's spread with a blast at the far end.
        ///
        /// 1. Before readying them, check the cone: if a friendly that isn't a small critter (worms, facehuggers,
        ///    swarmers, eggs, drones...) stands in it, the guns stay quiet this time.
        /// 2. While they are ready, friendlies don't end a move in the cone: the spots in it are taken out of the AI's
        ///    movement, melee and waypoint target generators for every friendly but the small critters.
        /// </summary>
        internal static class ScyllaGunsAI
        {
            private static StartPreparingShootAbilityDef _startPreparing;

            /// <summary>Actor defs of critters too small (or too expendable) to hold the guns back.</summary>
            private static readonly string[] SmallActorDefs =
            {
                "worm", "Facehugger", "Swarmer", "SpiderDrone", "Egg", "InjectorBomb", "Decoy", "Dummy"
            };

            /// <summary>Width added on each side of the cone: the guns are not laser-straight and units are not points.</summary>
            private const float ExtraHalfAngle = 3f;
            private const float MinHalfAngle = 8f;
            private const float UnitRadius = 1f;

            /// <summary>Past the aim point the shots that miss carry on this far.</summary>
            private const float Overshoot = 5f;

            private static readonly HashSet<string> LoggedVetoes = new HashSet<string>();

            private class Cone
            {
                internal TacticalActor Scylla;
                internal Vector3 Origin;
                internal Vector3 Target;
                internal float HalfAngle;
                internal float Length;
                internal float BlastRadius;
            }

            private static StartPreparingShootAbilityDef StartPreparingDef =>
                _startPreparing ?? (_startPreparing = DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef"));

            internal static bool IsSmall(TacticalActorBase actor)
            {
                string def = actor?.ActorDef?.name ?? "";
                return SmallActorDefs.Any(s => def.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            private static bool IsFriendlyOf(TacticalActorBase actor, TacticalActor scylla)
            {
                return actor != null && actor != scylla && actor.IsAlive
                    && actor.TacticalFaction != null
                    && (actor.TacticalFaction == scylla.TacticalFaction || actor.RelationTo(scylla) == FactionRelation.Friend)
                    && !(actor is TacticalActor tacticalActor && tacticalActor.Status != null && tacticalActor.Status.HasStatus<MindControlStatus>());
            }

            private static Cone MakeCone(TacticalActor scylla, Vector3 targetPos, StartPreparingShootAbility ability)
            {
                Weapon weapon = ability?.Weapon;
                float spread = weapon?.WeaponDef != null ? weapon.WeaponDef.SpreadDegrees : 15f;
                float range = weapon?.WeaponDef?.DamagePayload != null && weapon.WeaponDef.DamagePayload.Range > 0f ? weapon.WeaponDef.DamagePayload.Range : 30f;
                float blast = weapon?.WeaponDef?.DamagePayload != null ? weapon.WeaponDef.DamagePayload.AoeRadius : 2f;

                float distance = FlatDistance(scylla.Pos, targetPos);

                return new Cone
                {
                    Scylla = scylla,
                    Origin = scylla.Pos,
                    Target = targetPos,
                    HalfAngle = Mathf.Max(MinHalfAngle, spread / 2f + ExtraHalfAngle),
                    Length = Mathf.Min(range, distance + Overshoot),
                    BlastRadius = Mathf.Max(1f, blast) + UnitRadius
                };
            }

            private static float FlatDistance(Vector3 a, Vector3 b)
            {
                return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            }

            private static bool InCone(Cone cone, Vector3 pos)
            {
                Vector3 direction = cone.Target - cone.Origin;
                direction.y = 0f;
                Vector3 offset = pos - cone.Origin;
                offset.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                {
                    return offset.magnitude <= cone.BlastRadius;
                }

                if (FlatDistance(pos, cone.Target) <= cone.BlastRadius)
                {
                    return true;
                }

                direction.Normalize();
                float along = Vector3.Dot(offset, direction);

                if (along <= 0.5f || along > cone.Length)
                {
                    return false;
                }

                float lateral = (offset - along * direction).magnitude;
                return lateral <= along * Mathf.Tan(cone.HalfAngle * Mathf.Deg2Rad) + UnitRadius;
            }

            /// <summary>Called instead of readying the guns straight away: false (and a log line) when a friendly is in the way.</summary>
            internal static bool ClearToFire(TacticalActor scylla, StartPreparingShootAbility ability, object parameter)
            {
                try
                {
                    TacticalAbilityTarget target = parameter as TacticalAbilityTarget;

                    if (scylla == null || target == null)
                    {
                        return true;
                    }

                    Vector3 targetPos = target.Actor != null ? target.Actor.Pos : target.PositionToApply;
                    Cone cone = MakeCone(scylla, targetPos, ability);

                    List<TacticalActorBase> inTheWay = scylla.Map.GetActors<TacticalActorBase>()
                        .Where(a => IsFriendlyOf(a, scylla) && !IsSmall(a) && InCone(cone, a.Pos))
                        .ToList();

                    if (inTheWay.Count > 0)
                    {
                        TFTVLogger.Always($"[ScyllaGuns] {scylla.DisplayName} holds her guns: {string.Join(", ", inTheWay.Select(a => a.DisplayName))} " +
                            $"in the cone toward {targetPos} (half-angle {cone.HalfAngle:0}, length {cone.Length:0.#})");
                        return false;
                    }

                    TFTVLogger.Always($"[ScyllaGuns] {scylla.DisplayName} readies her guns at {targetPos}; no friendly in the cone");
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }

            /// <summary>Every Scylla friendly to the actor whose guns are ready, with her cone.</summary>
            private static List<Cone> ReadyCones(TacticalActor actor)
            {
                List<Cone> cones = new List<Cone>();

                if (StartPreparingDef == null || actor?.Map == null)
                {
                    return cones;
                }

                foreach (TacticalActor scylla in actor.Map.GetActors<TacticalActor>())
                {
                    if (scylla == actor || !scylla.IsAlive || scylla.Status == null || !IsFriendlyOf(actor, scylla))
                    {
                        continue;
                    }

                    PreparingStatus preparing = scylla.Status.GetStatus<PreparingStatus>();

                    if (preparing == null || float.IsNaN(preparing.TargetPos.x))
                    {
                        continue;
                    }

                    StartPreparingShootAbility ability = scylla.GetAbilityWithDef<StartPreparingShootAbility>(StartPreparingDef);

                    if (ability == null)
                    {
                        continue;
                    }

                    cones.Add(MakeCone(scylla, preparing.TargetPos, ability));
                }

                return cones;
            }

            private static IEnumerator<NextUpdate> WithoutConeSpots(IEnumerator<NextUpdate> original, TacticalActor actor, IList<AIScoredTarget> targets, string generator)
            {
                while (original.MoveNext())
                {
                    yield return original.Current;
                }

                RemoveConeSpots(actor, targets, generator);
            }

            private static void RemoveConeSpots(TacticalActor actor, IList<AIScoredTarget> targets, string generator)
            {
                try
                {
                    if (actor == null || targets == null || targets.Count == 0 || IsSmall(actor))
                    {
                        return;
                    }

                    List<Cone> cones = ReadyCones(actor);

                    if (cones.Count == 0)
                    {
                        return;
                    }

                    int removed = 0;

                    for (int i = targets.Count - 1; i >= 0; i--)
                    {
                        if (targets[i]?.Target is TacAITarget target && cones.Any(c => InCone(c, target.Pos)))
                        {
                            targets.RemoveAt(i);
                            removed++;
                        }
                    }

                    if (removed > 0 && LoggedVetoes.Add($"{actor.name}|{generator}"))
                    {
                        TFTVLogger.Always($"[ScyllaGuns] {actor.DisplayName}: {removed} spots ({generator}) are in " +
                            $"{string.Join(", ", cones.Select(c => c.Scylla.DisplayName))}'s readied gun cone, left out");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            [HarmonyPatch(typeof(AIActorMovementZoneTargetGenerator), nameof(AIActorMovementZoneTargetGenerator.FillTargets))]
            internal static class TFTV_AIActorMovementZoneTargetGenerator_FillTargets_ScyllaCone_patch
            {
                public static void Postfix(IAIActor actor, IList<AIScoredTarget> targets, ref IEnumerator<NextUpdate> __result)
                {
                    __result = WithoutConeSpots(__result, actor as TacticalActor, targets, "move");
                }
            }

            [HarmonyPatch(typeof(AIActorRangeZoneTargetGenerator), nameof(AIActorRangeZoneTargetGenerator.FillTargets))]
            internal static class TFTV_AIActorRangeZoneTargetGenerator_FillTargets_ScyllaCone_patch
            {
                public static void Postfix(IAIActor actor, IList<AIScoredTarget> targets, ref IEnumerator<NextUpdate> __result)
                {
                    __result = WithoutConeSpots(__result, actor as TacticalActor, targets, "melee");
                }
            }

            [HarmonyPatch(typeof(AIWaypointTargetGenerator), nameof(AIWaypointTargetGenerator.FillTargets))]
            internal static class TFTV_AIWaypointTargetGenerator_FillTargets_ScyllaCone_patch
            {
                public static void Postfix(IAIActor actor, IList<AIScoredTarget> targets, ref IEnumerator<NextUpdate> __result)
                {
                    __result = WithoutConeSpots(__result, actor as TacticalActor, targets, "waypoint");
                }
            }

            internal static void ClearTurn()
            {
                LoggedVetoes.Clear();
            }
        }
    }
}
