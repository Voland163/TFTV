using Base;
using Base.Core;
using Base.Entities.Statuses;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class OWNotTriggeringTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(TacticalLevelController), "ExecuteOverwatch")]
        public static class TacticalLevelControllerExecuteOverwatchPatch
        {
            private const float MarkedWatchAccuracyBonus = 0.5f;

            private static readonly FieldInfo OverwatchExecutedEventField =
                AccessTools.Field(typeof(TacticalLevelController), nameof(TacticalLevelController.OverwatchExecutedEvent));

            private static readonly FieldInfo OverwatchFinishedEventField =
                AccessTools.Field(typeof(TacticalLevelController), nameof(TacticalLevelController.OverwatchFinishedEvent));

            private static readonly MethodInfo OverwatchTargetSetter =
                AccessTools.PropertySetter(typeof(TacticalLevelController), nameof(TacticalLevelController.OverwatchTarget));

            public static bool Prefix(
                TacticalLevelController __instance,
                TacticalActor target,
                List<OverwatchStatus> overwatchStatuses,
                ref IEnumerator<NextUpdate> __result)
            {
                __result = ExecuteOverwatch(__instance, target, overwatchStatuses);
                return false;
            }

            private static IEnumerator<NextUpdate> ExecuteOverwatch(
                TacticalLevelController controller,
                TacticalActor target,
                List<OverwatchStatus> overwatchStatuses)
            {
                if (controller == null || controller.Map == null || overwatchStatuses == null)
                {
                    yield break;
                }

                MarkedWatchOverwatchData markedWatchData = BuildMarkedWatchOverwatchData(controller, target);

                SetOverwatchTarget(controller, target);
                if (target != null)
                {
                    target.TimingScale?.AddScale(controller.OverwatchTimeScale, controller);
                }

                try
                {
                    IEnumerable<TacticalActorBase> lockActors = controller.Map.GetActors<TacticalActorBase>(null);
                    if (lockActors == null)
                    {
                        yield break;
                    }

                    using (new MultiForceTargetableLock(lockActors))
                    {
                        foreach (OverwatchStatus overwatch in overwatchStatuses)
                        {
                            if (target == null || target.IsDead)
                            {
                                break;
                            }

                            if (overwatch == null)
                            {
                                continue;
                            }

                            if (!OverwatchAimPointHelper.IsAnyAimPointInCone(overwatch, target))
                            {
                                continue;
                            }

                            Weapon weapon = overwatch.GetWeapon();
                            ShootAbility defaultShootAbility = weapon?.DefaultShootAbility;
                            if (defaultShootAbility == null)
                            {
                                continue;
                            }

                            TacticalActor shooterActor = defaultShootAbility.TacticalActor;
                            if (shooterActor == null)
                            {
                                continue;
                            }

                            if (ShouldSkipMarkedWatchOverwatch(markedWatchData, shooterActor))
                            {
                                continue;
                            }

                            if (defaultShootAbility.GetWeaponDisabledState(IgnoredAbilityDisabledStatesFilter.CreateDefaultFilter()) != AbilityDisabledState.NotDisabled)
                            {
                                continue;
                            }

                            if (!TacticalFactionVision.CheckVisibleLineBetweenActors(
                                    shooterActor,
                                    shooterActor.Pos,
                                    target,
                                    false,
                                    null,
                                    1f,
                                    null))
                            {
                                continue;
                            }

                            TacticalAbilityTarget overwatchTarget = defaultShootAbility.GetAttackActorTarget(target, AttackType.Overwatch);
                            if (overwatchTarget == null)
                            {
                                continue;
                            }

                            if (shooterActor.TacticalPerception == null)
                            {
                                continue;
                            }

                            Weapon sourceWeapon = defaultShootAbility.GetSource<Weapon>();
                            if (sourceWeapon == null)
                            {
                                continue;
                            }

                            if (shooterActor.TacticalPerception.CheckFriendlyFire(
                                    sourceWeapon,
                                    overwatchTarget.ShootFromPos,
                                    overwatchTarget,
                                    out TacticalActor _,
                                    FactionRelation.Neutral | FactionRelation.Friend))
                            {
                                continue;
                            }

                            StatModification? markedWatchAccuracyModifier = null;

                            try
                            {
                                markedWatchAccuracyModifier = ApplyMarkedWatchAccuracyBonus(defaultShootAbility, markedWatchData, shooterActor);

                                overwatch.SetConeVisualsMode(false, false);
                                InvokeOverwatchEvent(controller, OverwatchExecutedEventField, overwatchTarget, shooterActor.gameObject);

                                if (controller.Timing != null)
                                {
                                    IEnumerator<NextUpdate> abilityExecution = defaultShootAbility.Execute(overwatchTarget);
                                    if (abilityExecution != null)
                                    {
                                        yield return controller.Timing.Call(abilityExecution, null);
                                    }
                                }

                                InvokeOverwatchEvent(controller, OverwatchFinishedEventField, overwatchTarget, shooterActor.gameObject);
                            }
                            finally
                            {
                                RemoveMarkedWatchAccuracyBonus(defaultShootAbility, markedWatchAccuracyModifier);
                            }

                            if (overwatch.Applied && shooterActor.Status != null)
                            {
                                shooterActor.Status.UnapplyStatus(overwatch);
                            }
                        }
                    }
                }
                finally
                {
                    if (target != null)
                    {
                        target.TimingScale?.RemoveScale(controller.OverwatchTimeScale, controller);
                    }

                    SetOverwatchTarget(controller, null);
                }
            }

            private static bool ShouldSkipMarkedWatchOverwatch(
                MarkedWatchOverwatchData markedWatchData,
                TacticalActor shooterActor)
            {
                return shooterActor != null
                    && markedWatchData != null
                    && markedWatchData.ShootersWithMarkedTargets.Contains(shooterActor)
                    && !markedWatchData.ShootersAllowedOnCurrentTarget.Contains(shooterActor);
            }

            private static StatModification? ApplyMarkedWatchAccuracyBonus(
                ShootAbility ability,
                MarkedWatchOverwatchData markedWatchData,
                TacticalActor shooterActor)
            {
                if (ability == null || shooterActor == null || markedWatchData == null)
                {
                    return null;
                }

                if (!markedWatchData.ShootersAllowedOnCurrentTarget.Contains(shooterActor))
                {
                    return null;
                }

                BaseStat accuracyStat = shooterActor.CharacterStats?.TryGetStat(StatModificationTarget.Accuracy);
                if (accuracyStat == null)
                {
                    return null;
                }

                StatModification modifier = new StatModification(
                    StatModificationType.Add,
                    accuracyStat.Name,
                    MarkedWatchAccuracyBonus,
                    ability,
                    0f);

                accuracyStat.AddStatModification(modifier, true);
                return modifier;
            }

            private static void RemoveMarkedWatchAccuracyBonus(ShootAbility ability, StatModification? modifier)
            {
                if (ability == null || !modifier.HasValue)
                {
                    return;
                }

                BaseStat accuracyStat = ability.TacticalActor?.CharacterStats?.TryGetStat(StatModificationTarget.Accuracy);
                if (accuracyStat == null)
                {
                    return;
                }

                accuracyStat.RemoveStatModification(modifier.Value, true);
            }

            private static MarkedWatchOverwatchData BuildMarkedWatchOverwatchData(TacticalLevelController controller, TacticalActor target)
            {
                MarkedWatchOverwatchData data = new MarkedWatchOverwatchData();

                DamageMultiplierStatusDef markedWatchStatusDef = TFTVDrills.DrillsDefs._markedwatchStatus;
                if (!TFTVAircraftReworkMain.AircraftReworkOn || controller?.Map == null || markedWatchStatusDef == null || string.IsNullOrEmpty(markedWatchStatusDef.EffectName))
                {
                    return data;
                }

                IEnumerable<TacticalActor> actors = controller.Map.GetActors<TacticalActor>();
                if (actors == null)
                {
                    return data;
                }

                foreach (TacticalActor actor in actors)
                {
                    if (actor?.Status == null)
                    {
                        continue;
                    }

                    IEnumerable<TacStatus> statuses = actor.Status.GetStatusesByName(markedWatchStatusDef.EffectName)?.OfType<TacStatus>();
                    if (statuses == null)
                    {
                        continue;
                    }

                    foreach (TacStatus status in statuses)
                    {
                        TacticalActor sourceActor = status?.Source as TacticalActor;
                        if (sourceActor == null)
                        {
                            continue;
                        }

                        data.ShootersWithMarkedTargets.Add(sourceActor);

                        if (ReferenceEquals(actor, target))
                        {
                            data.ShootersAllowedOnCurrentTarget.Add(sourceActor);
                        }
                    }
                }

                return data;
            }

            private sealed class MarkedWatchOverwatchData
            {
                public HashSet<TacticalActor> ShootersWithMarkedTargets { get; } = new HashSet<TacticalActor>();
                public HashSet<TacticalActor> ShootersAllowedOnCurrentTarget { get; } = new HashSet<TacticalActor>();
            }

            private static void InvokeOverwatchEvent(
                TacticalLevelController controller,
                FieldInfo field,
                TacticalAbilityTarget overwatchTarget,
                GameObject shooter)
            {
                if (controller == null || field == null)
                {
                    return;
                }

                TacticalLevelController.OverwatchExecutedHandler handler =
                    field.GetValue(controller) as TacticalLevelController.OverwatchExecutedHandler;

                handler?.Invoke(overwatchTarget, shooter);
            }

            private static void SetOverwatchTarget(TacticalLevelController controller, TacticalActor target)
            {
                if (controller == null || OverwatchTargetSetter == null)
                {
                    return;
                }

                OverwatchTargetSetter.Invoke(controller, new object[] { target });
            }
        }

        internal static class OverwatchAimPointHelper
        {
            public static bool IsAnyAimPointInCone(OverwatchStatus overwatch, TacticalActor target)
            {
                if (overwatch == null || target == null)
                {
                    return false;
                }

                Cone cone = overwatch.GetCone();
                if (cone.IsDefaultValue<Cone>())
                {
                    return false;
                }

                foreach (Vector3 point in EnumerateAimPointPositions(target))
                {
                    if (cone.Contains(point))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static IEnumerable<Vector3> EnumerateAimPointPositions(TacticalActor actor)
            {
                if (actor == null)
                {
                    yield break;
                }

                yield return actor.Pos;
                yield return actor.VisionPoint;

                foreach (Vector3 point in EnumerateAimPointPositions((TacticalActorBase)actor))
                {
                    yield return point;
                }

                if (actor.IsDummyReady)
                {
                    foreach (Vector3 point in EnumerateAimPointPositions(actor.TargetDummy))
                    {
                        yield return point;
                    }
                }

                if (actor is ITargetDummyProvider provider)
                {
                    foreach (Vector3 point in EnumerateAimPointPositions(provider, actor.TargetDummy))
                    {
                        yield return point;
                    }
                }
            }

            private static IEnumerable<Vector3> EnumerateAimPointPositions(TacticalActorBase actor)
            {
                if (actor == null)
                {
                    yield break;
                }

                IEnumerable<Transform> aimPoints = actor.GetAimPoints();
                if (aimPoints == null)
                {
                    yield break;
                }

                foreach (Transform transform in aimPoints)
                {
                    if (transform != null)
                    {
                        yield return transform.position;
                    }
                }
            }

            private static IEnumerable<Vector3> EnumerateAimPointPositions(ITargetDummyProvider provider, ITargetDummy ignoredDummy = null)
            {
                if (provider == null)
                {
                    yield break;
                }

                ITargetDummy dummy = provider.ITargetDummy;
                if (dummy == null || dummy == ignoredDummy)
                {
                    yield break;
                }

                foreach (Vector3 point in EnumerateAimPointPositions(dummy))
                {
                    yield return point;
                }
            }

            private static IEnumerable<Vector3> EnumerateAimPointPositions(ITargetDummy dummy)
            {
                if (dummy == null)
                {
                    yield break;
                }

                IEnumerable<Transform> aimPoints = dummy.GetAimPoints();
                if (aimPoints == null)
                {
                    yield break;
                }

                foreach (Transform transform in aimPoints)
                {
                    if (transform != null)
                    {
                        yield return transform.position;
                    }
                }
            }
        }
    }
}
