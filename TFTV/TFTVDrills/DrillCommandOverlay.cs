using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(
        fileName = "PerceptionAuraStatusDef",
        menuName = "Defs/Statuses/PerceptionAuraStatus")]
    public class PerceptionAuraStatusDef : TacStatusDef
    {
        public float AccuracyBonus = 20f;
    }

    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class PerceptionAuraStatus : TacStatus
    {
        private const float Epsilon = 1E-05f;

        private static GameTagDef _nightVisionTag;

        private static GameTagDef NightVisionTag =>
            _nightVisionTag ??
            (_nightVisionTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>(
                "NightVision_4_Full_SkillTagDef"));

        /// <summary>
        /// True while this particular aura status grants its actor shared
        /// full night vision.
        ///
        /// We do not add the real night-vision GameTagDef to the actor. The
        /// TacticalFactionVision.GetNightVisionValue Harmony patch checks this
        /// field instead. This prevents the aura from accidentally removing a
        /// night-vision tag owned by equipment or another ability.
        /// </summary>
        [SerializeMember]
        private bool _sharedNightVision;

        internal PerceptionAuraStatusDef AuraDef =>
            (PerceptionAuraStatusDef)BaseDef;

        /// <summary>
        /// Whether this aura status currently grants temporary shared night
        /// vision to its actor.
        /// </summary>
        internal bool GrantsSharedNightVision => _sharedNightVision;

        /// <summary>
        /// Whether the actor possesses the actual full-night-vision tag,
        /// independently of this aura.
        ///
        /// Because this implementation never adds that tag itself, finding the
        /// tag here means that it came from the actor, equipment, or another
        /// legitimate game mechanic.
        /// </summary>
        internal bool HasInnateNightVision
        {
            get
            {
                TacticalActor tacticalActor = TacticalActor;
                GameTagDef nightVisionTag = NightVisionTag;

                return tacticalActor != null &&
                       nightVisionTag != null &&
                       tacticalActor.HasGameTag(nightVisionTag);
            }
        }

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);

            /*
             * OnApply can also run while a saved tactical status is being
             * deserialized. These operations are deliberately idempotent:
             *
             * - ApplyAccuracyBonus removes any modification sourced by this
             *   status before adding one replacement.
             * - RefreshFromSource recalculates the complete aura state.
             */
            ApplyAccuracyBonus(AuraDef.AccuracyBonus);
            PerceptionAuraManager.RefreshFromSource(Source, TacticalActor?.Map);
        }

        public override void OnUnapply()
        {
            /*
             * Save these before calling base.OnUnapply so the manager can still
             * refresh the other statuses belonging to the same aura source.
             */
            object auraSource = Source;
            TacticalMap map = TacticalActor?.Map;

            /*
             * Remove this status's effects while the status instance and actor
             * references are still available.
             */
            ClearAccuracyBonus();
            ClearPerceptionOverride();
            ClearNightVisionShare();

            base.OnUnapply();

            /*
             * Recalculate the remaining aura members. The removed actor's aura
             * status should no longer be returned by GetStatusesFromSource.
             */
            PerceptionAuraManager.RefreshFromSource(auraSource, map);
        }

        /// <summary>
        /// Applies the configured additive accuracy bonus.
        ///
        /// The status instance itself is used as the StatModification source.
        /// Status instances are part of the serialized tactical object graph,
        /// unlike anonymous new object() source tokens.
        /// </summary>
        internal void ApplyAccuracyBonus(float accuracyBonus)
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            BaseStat accuracy = tacticalActor.CharacterStats.Accuracy;

            /*
             * Always remove the previous contribution before rebuilding it.
             * This makes repeated refreshes and save/load restoration safe.
             */
            accuracy.RemoveStatModificationsWithSource(this, true);

            if (Mathf.Abs(accuracyBonus) <= Epsilon)
            {
                return;
            }

            StatModification modification = new StatModification(
                StatModificationType.Add,
                accuracy.Name,
                accuracyBonus,
                this,
                accuracyBonus);

            accuracy.AddStatModification(modification, true);
        }

        /// <summary>
        /// Returns this actor's perception without the contribution currently
        /// supplied by this aura status.
        ///
        /// This is calculated from the actual BaseStat.Modifications collection
        /// instead of relying on separately serialized bookkeeping.
        /// </summary>
        internal float GetBaselinePerception()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return 0f;
            }

            BaseStat perception = tacticalActor.CharacterStats.Perception;

            float thisAuraContribution = perception.Modifications
                .Where(modification =>
                    ReferenceEquals(modification.Source, this))
                .Sum(modification =>
                    modification.GetApplicationValue());

            return perception.Value - thisAuraContribution;
        }

        /// <summary>
        /// Adjusts the actor's perception so that it matches targetValue.
        ///
        /// The target is interpreted relative to the actor's perception without
        /// this particular status's existing contribution.
        /// </summary>
        internal void ApplyPerceptionOverride(float targetValue)
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            BaseStat perception = tacticalActor.CharacterStats.Perception;

            /*
             * Calculate baseline before removing the existing modification,
             * because GetBaselinePerception subtracts this status's current
             * contribution.
             */
            float baselinePerception = GetBaselinePerception();
            float desiredDelta = targetValue - baselinePerception;

            /*
             * Always remove and rebuild. Do not skip this based on a separately
             * stored delta, because such bookkeeping can become inconsistent
             * with the serialized BaseStat modification list.
             */
            perception.RemoveStatModificationsWithSource(this, true);

            if (Mathf.Abs(desiredDelta) <= Epsilon)
            {
                return;
            }

            StatModification modification = new StatModification(
                StatModificationType.Add,
                perception.Name,
                desiredDelta,
                this,
                desiredDelta);

            perception.AddStatModification(modification, true);
        }

        /// <summary>
        /// Sets whether this status grants shared full night vision.
        ///
        /// The actor's actual game-tag collection is not modified. The Harmony
        /// patch on TacticalFactionVision.GetNightVisionValue reads this state.
        /// </summary>
        internal void ApplyNightVisionShare(bool shouldHaveNightVision)
        {
            _sharedNightVision = shouldHaveNightVision;
        }

        private void ClearAccuracyBonus()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            tacticalActor.CharacterStats.Accuracy
                .RemoveStatModificationsWithSource(this, true);
        }

        private void ClearPerceptionOverride()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            tacticalActor.CharacterStats.Perception
                .RemoveStatModificationsWithSource(this, true);
        }

        private void ClearNightVisionShare()
        {
            _sharedNightVision = false;
        }
    }

    public static class PerceptionAuraManager
    {
        private const float Epsilon = 1E-05f;

        public static void Refresh(ApplyStatusAbility ability)
        {
            if (!(ability?.ApplyStatusAbilityDef?.StatusDef
                  is PerceptionAuraStatusDef auraStatusDef))
            {
                return;
            }

            TacticalActorBase caster = ability.TacticalActorBase;
            if (caster?.Status == null)
            {
                return;
            }

            RefreshFromSource(
                caster,
                caster.Map,
                auraStatusDef.AccuracyBonus);
        }

        public static void RefreshFromSource(
            object source,
            TacticalMap map,
            float? accuracyBonusOverride = null)
        {
            if (source == null || map == null)
            {
                return;
            }

            List<PerceptionAuraStatus> statuses =
                new List<PerceptionAuraStatus>();

            float accuracyBonus = accuracyBonusOverride ?? 0f;

            foreach (TacticalActorBase actor
                     in map.GetActors<TacticalActorBase>(null))
            {
                StatusComponent statusComponent = actor.Status;
                if (statusComponent == null)
                {
                    continue;
                }

                foreach (PerceptionAuraStatus status
                         in statusComponent
                             .GetStatusesFromSource<PerceptionAuraStatus>(
                                 source))
                {
                    statuses.Add(status);

                    if (accuracyBonusOverride == null &&
                        status.AuraDef != null)
                    {
                        accuracyBonus = status.AuraDef.AccuracyBonus;
                    }
                }
            }

            if (statuses.Count == 0)
            {
                return;
            }

            /*
             * Capture every member's baseline state before modifying anyone.
             * This prevents processing order from affecting the result.
             */
            List<PerceptionAuraMemberSnapshot> members = statuses
                .Select(status =>
                    new PerceptionAuraMemberSnapshot(
                        status,
                        status.GetBaselinePerception(),
                        status.HasInnateNightVision))
                .ToList();

            float maxPerception = members.Max(member =>
                member.BaselinePerception);

            /*
             * Night vision is shared only if an actor tied for the highest
             * baseline perception has innate full night vision.
             *
             * A lower-perception actor with night vision does not provide it to
             * the group.
             */
            bool shareNightVision = members.Any(member =>
                Mathf.Abs(
                    member.BaselinePerception - maxPerception) <= Epsilon &&
                member.HasInnateNightVision);

            foreach (PerceptionAuraMemberSnapshot member in members)
            {
                member.Status.ApplyAccuracyBonus(accuracyBonus);
                member.Status.ApplyPerceptionOverride(maxPerception);
                member.Status.ApplyNightVisionShare(shareNightVision);
            }
        }

        private sealed class PerceptionAuraMemberSnapshot
        {
            internal PerceptionAuraMemberSnapshot(
                PerceptionAuraStatus status,
                float baselinePerception,
                bool hasInnateNightVision)
            {
                Status = status;
                BaselinePerception = baselinePerception;
                HasInnateNightVision = hasInnateNightVision;
            }

            internal PerceptionAuraStatus Status { get; }

            internal float BaselinePerception { get; }

            internal bool HasInnateNightVision { get; }
        }
    }

    internal class DrillCommandOverlay
    {
        [HarmonyPatch(
            typeof(ApplyStatusAbility),
            "SetAuraStatusForActor")] // VERIFIED
        public static class
            ApplyStatusAbility_SetAuraStatusForActor_Patch
        {
            public static void Postfix(ApplyStatusAbility __instance)
            {
                PerceptionAuraManager.Refresh(__instance);
            }
        }

        [HarmonyPatch(
            typeof(ApplyStatusAbility),
            "ToggleStatusForAll")] // VERIFIED
        public static class
            ApplyStatusAbility_ToggleStatusForAll_Patch
        {
            public static void Postfix(ApplyStatusAbility __instance)
            {
                PerceptionAuraManager.Refresh(__instance);
            }
        }

        /// <summary>
        /// Supplies temporary full night vision without adding or removing the
        /// actor's actual full-night-vision game tag.
        ///
        /// If several perception auras overlap, shared night vision remains
        /// active as long as at least one active aura status grants it.
        /// </summary>
        [HarmonyPatch(
            typeof(TacticalFactionVision),
            nameof(TacticalFactionVision.GetNightVisionValue))]
        public static class
            TacticalFactionVision_GetNightVisionValue_Patch
        {
            public static void Postfix(
                TacticalActorBase actor,
                ref float __result)
            {
                /*
                 * Nothing can improve on full night vision.
                 */
                if (__result >= 1f || actor?.Status == null)
                {
                    return;
                }

                bool hasSharedNightVision = actor.Status
                    .GetStatuses<PerceptionAuraStatus>()
                    .Any(status =>
                        status.GrantsSharedNightVision);

                if (hasSharedNightVision)
                {
                    __result = 1f;
                }
            }
        }
    }
}