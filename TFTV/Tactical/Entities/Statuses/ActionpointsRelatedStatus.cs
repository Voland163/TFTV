using Base.Entities.Statuses;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PRMBetterClasses.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(Status))]
    public class ActionpointsRelatedStatus : TacStatus
    {
        public ActionpointsRelatedStatusDef ActionpointsRelatedStatusDef => BaseDef as ActionpointsRelatedStatusDef;

        private void ApplyModification(StatusStat apStat)
        {
            PRMLogger.Debug("----------------------------------------------------", false);
            PRMLogger.Debug($"'{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}()' called ...");
            float num = (apStat.Value - ActionpointsRelatedStatusDef.ActionpointsLowBound) / (apStat.Max - ActionpointsRelatedStatusDef.ActionpointsLowBound);
            num = Mathf.Clamp01(num);
            float num2 = num * ActionpointsRelatedStatusDef.MaxBoost;
            //num2 = Mathf.Max(num2, 1f);
            PRMLogger.Debug($"Calculated modification: {num2}");
            foreach (StatModificationTarget targetStat in ActionpointsRelatedStatusDef.StatModificationTargets)
            {
                BaseStat baseStat = TacticalActor.CharacterStats.TryGetStat(targetStat);
                PRMLogger.Debug($"Stat before modification '{baseStat}'");
                PRMLogger.Debug($"Stat modifications before modification '{baseStat.GetValueModifications().Join()}'");
                baseStat.RemoveStatModificationsWithSource(ActionpointsRelatedStatusDef, true);
                PRMLogger.Debug($"Stat modifications after removing modification '{baseStat.GetValueModifications().Join()}'");
                if (baseStat is StatusStat)
                {
                    num2 += 1;
                    baseStat.AddStatModification(new StatModification(StatModificationType.MultiplyMax, ToString(), num2, ActionpointsRelatedStatusDef, num2), true);
                    baseStat.AddStatModification(new StatModification(StatModificationType.MultiplyRestrictedToBounds, ToString(), num2, ActionpointsRelatedStatusDef, num2), true);
                }
                else
                {
                    baseStat.AddStatModification(new StatModification(StatModificationType.Add, ToString(), num2, ActionpointsRelatedStatusDef, num2), true);
                }
                baseStat.ReapplyModifications();
                PRMLogger.Debug($"Stat after modification '{baseStat}'");
                PRMLogger.Debug($"Stat modifications after modification '{baseStat.GetValueModifications().Join()}'");
            }
            PRMLogger.Debug("----------------------------------------------------", false);
        }

        private void ActionpointsChangedHandler(BaseStat stat, StatChangeType change, float prevValue, float unclampedValue)
        {
            PRMLogger.Debug("----------------------------------------------------", false);
            PRMLogger.Debug($"'{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}()' called ...");
            PRMLogger.Debug("----------------------------------------------------", false);
            StatusStat apStat = stat as StatusStat;
            ApplyModification(apStat);
        }

        public override void OnApply(StatusComponent statusComponent)
        {
            PRMLogger.Debug("----------------------------------------------------", false);
            PRMLogger.Debug($"'{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}()' called ...");
            PRMLogger.Debug("----------------------------------------------------", false);
            base.OnApply(statusComponent);
            if (TacticalActor == null)
            {
                RequestUnapply(statusComponent);
                return;
            }
            ApplyModification(TacticalActor.CharacterStats.ActionPoints);
            TacticalActor.CharacterStats.ActionPoints.StatChangeEvent += ActionpointsChangedHandler;
        }

        public override void OnUnapply()
        {
            PRMLogger.Debug("----------------------------------------------------", false);
            PRMLogger.Debug($"'{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}()' called ...");
            PRMLogger.Debug("----------------------------------------------------", false);
            base.OnUnapply();
            foreach (StatModificationTarget targetStat in ActionpointsRelatedStatusDef.StatModificationTargets)
            {
                BaseStat baseStat = TacticalActor.CharacterStats.TryGetStat(targetStat);
                baseStat.RemoveStatModificationsWithSource(ActionpointsRelatedStatusDef, true);
                baseStat.ReapplyModifications();
            }
            TacticalActor.CharacterStats.ActionPoints.StatChangeEvent -= ActionpointsChangedHandler;
        }
    }
}
