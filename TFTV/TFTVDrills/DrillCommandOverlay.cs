using Base.Entities.Statuses;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVDrills
{

    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "PerceptionAuraStatusDef", menuName = "Defs/Statuses/PerceptionAuraStatus")]
    public class PerceptionAuraStatusDef : TacStatusDef
    {
        public float AccuracyBonus = 20f;
    }

    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class PerceptionAuraStatus : TacStatus
    {
        private readonly object _accuracySource = new object();
        private readonly object _perceptionSource = new object();
        private float _perceptionDelta;

        private PerceptionAuraStatusDef AuraDef => (PerceptionAuraStatusDef)BaseDef;

        public override void OnApply(StatusComponent statusComponent)
        {
            bool alreadyApplied = Applied;
            base.OnApply(statusComponent);
            if (alreadyApplied)
            {
                return;
            }

            ApplyAccuracyBonus(AuraDef.AccuracyBonus);
        }

        public override void OnUnapply()
        {
            base.OnUnapply();
            ClearAccuracyBonus();
            ClearPerceptionOverride();
        }

        internal void ApplyAccuracyBonus(float accuracyBonus)
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            BaseStat accuracy = tacticalActor.CharacterStats.Accuracy;
            accuracy.RemoveStatModificationsWithSource(_accuracySource, true);
            if (Mathf.Abs(accuracyBonus) <= 1E-05f)
            {
                return;
            }

            StatModification statModification = new StatModification(StatModificationType.Add, accuracy.Name, accuracyBonus, _accuracySource, accuracyBonus);
            accuracy.AddStatModification(statModification, true);
        }

        internal float GetBaselinePerception()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return 0f;
            }

            return tacticalActor.CharacterStats.Perception - _perceptionDelta;
        }

        internal void ApplyPerceptionOverride(float targetValue)
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            BaseStat perception = tacticalActor.CharacterStats.Perception;
            float baseline = GetBaselinePerception();
            float desiredDelta = targetValue - baseline;
            if (Mathf.Abs(desiredDelta) <= 1E-05f)
            {
                if (Mathf.Abs(_perceptionDelta) > 1E-05f)
                {
                    perception.RemoveStatModificationsWithSource(_perceptionSource, true);
                    _perceptionDelta = 0f;
                }
                return;
            }

            if (Mathf.Abs(desiredDelta - _perceptionDelta) <= 1E-05f)
            {
                return;
            }

            perception.RemoveStatModificationsWithSource(_perceptionSource, true);
            StatModification statModification = new StatModification(StatModificationType.Add, perception.Name, desiredDelta, _perceptionSource, desiredDelta);
            perception.AddStatModification(statModification, true);
            _perceptionDelta = desiredDelta;
        }

        private void ClearAccuracyBonus()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            tacticalActor.CharacterStats.Accuracy.RemoveStatModificationsWithSource(_accuracySource, true);
        }

        private void ClearPerceptionOverride()
        {
            TacticalActor tacticalActor = TacticalActor;
            if (tacticalActor == null)
            {
                return;
            }

            tacticalActor.CharacterStats.Perception.RemoveStatModificationsWithSource(_perceptionSource, true);
            _perceptionDelta = 0f;
        }
    }

    public static class PerceptionAuraManager
    {
        public static void Refresh(ApplyStatusAbility ability)
        {
            if (!(ability?.ApplyStatusAbilityDef?.StatusDef is PerceptionAuraStatusDef auraStatusDef))
            {
                return;
            }

            TacticalActorBase caster = ability.TacticalActorBase;
            if (caster?.Status == null)
            {
                return;
            }

            TacticalMap map = caster.Map;
            if (map == null)
            {
                return;
            }

            List<PerceptionAuraStatus> statuses = new List<PerceptionAuraStatus>();
            foreach (TacticalActorBase actor in map.GetActors<TacticalActorBase>(null))
            {
                StatusComponent statusComponent = actor.Status;
                if (statusComponent == null)
                {
                    continue;
                }

                statuses.AddRange(statusComponent.GetStatusesFromSource<PerceptionAuraStatus>(caster));
            }

            if (statuses.Count == 0)
            {
                return;
            }

            float maxPerception = statuses.Max(s => s.GetBaselinePerception());
            foreach (PerceptionAuraStatus status in statuses)
            {
                status.ApplyAccuracyBonus(auraStatusDef.AccuracyBonus);
                status.ApplyPerceptionOverride(maxPerception);
            }
        }
    }

    internal class DrillCommandOverlay
    {

        [HarmonyPatch(typeof(ApplyStatusAbility), "SetAuraStatusForActor")] //VERIFIED
        public static class ApplyStatusAbility_SetAuraStatusForActor_Patch
        {
            public static void Postfix(ApplyStatusAbility __instance)
            {
                PerceptionAuraManager.Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(ApplyStatusAbility), "ToggleStatusForAll")] //VERIFIED
        public static class ApplyStatusAbility_ToggleStatusForAll_Patch
        {
            public static void Postfix(ApplyStatusAbility __instance)
            {
                PerceptionAuraManager.Refresh(__instance);
            }
        }





    }
}
