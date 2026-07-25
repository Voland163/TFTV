using Base;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PhoenixPoint.Tactical.Entities.TacticalActorViewBase;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class UITacticalVanillaFixes
    {

        [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.GetStatusesFiltered))]
        public static class TacticalActorViewBase_GetStatusesFiltered_patch
        {

            public static bool Prefix(TacticalActorViewBase __instance, Func<TacStatus, bool> statusesFilter, StatusComponent ____statusComponent, ref List<StatusInfo> __result, bool stackAsSingle)
            {
                try
                {
                    // TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] Prefix Checking for {__instance?.ActorBase?.name} display name: {__instance?.ActorBase?.DisplayName} statusesFilter null? {statusesFilter==null}");

                    if (____statusComponent == null)
                    {
                        __result = new List<StatusInfo>();
                        return false;
                    }

                    List<StatusInfo> list = new List<StatusInfo>();
                    List<TacStatus> list2 = (from st in ____statusComponent.Statuses.OfType<TacStatus>().Where(statusesFilter)
                                             orderby st.TacStatusDef.HealthbarPriority
                                             select st).ToList();
                    // TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] Prefix found {list2?.Count} statuses after filtering with provided filter and sorting by healthbar priority.");

                    while (!list2.IsEmpty())
                    {
                        TacStatus tacStatus = list2.PopLast();
                        if (tacStatus == null)
                        {
                            continue;
                        }



                        //  TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] Prefix status {tacStatus?.TacStatusDef?.name} has StackMultipleStatusesAsSingleIcon set to {tacStatus?.TacStatusDef?.StackMultipleStatusesAsSingleIcon} and stackAsSingle is {stackAsSingle}");

                        if (tacStatus.TacStatusDef.StackMultipleStatusesAsSingleIcon && stackAsSingle)
                        {
                            float num = tacStatus.Value;
                            float num2 = tacStatus.Limit;

                            //   TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] Prefix status {tacStatus?.TacStatusDef?.name} initial value is {num} and limit is {num2}");
                            //  TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] tacStatus.GetTargetSlotsNames()==null?: {tacStatus.GetTargetSlotsNames()==null} ");

                            List<string> list3 = tacStatus.GetTargetSlotsNames().ToList();
                            int num3 = 0;
                            for (int count = list2.Count; num3 < count; num3++)
                            {
                                // TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] list2[num3]==null: {list2[num3]==null}");
                                //  TFTVLogger.Always($"[TacticalActorViewBase.GetStatusesFiltered] list2[num3]?.TacStatusDef?.name: {list2[num3]?.TacStatusDef?.name}");


                                TacStatus tacStatus2 = list2[num3];

                                if (tacStatus2 == null)
                                {
                                    continue;
                                }

                                if (tacStatus2.TacStatusDef == tacStatus.TacStatusDef)
                                {


                                    list2[num3] = null;
                                    num += tacStatus2.Value;
                                    num2 += tacStatus2.Limit;
                                    list3.AddRange(tacStatus2.GetTargetSlotsNames());
                                }
                            }

                            list.Add(new StatusInfo
                            {
                                Def = tacStatus.TacStatusDef,
                                Value = num,
                                Limit = num2,
                                TargetSlots = list3
                            });
                        }
                        else
                        {
                            list.Add(new StatusInfo
                            {
                                Def = tacStatus.TacStatusDef,
                                Value = tacStatus.Value,
                                Limit = tacStatus.Limit,
                                TargetSlots = tacStatus.GetTargetSlotsNames().ToList()
                            });
                        }
                    }

                    __result = list;
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        /// <summary>
        /// Removes the empty target icons from destroyed vehicles
        /// </summary>
        /// 

        [HarmonyPatch(typeof(UIModuleSpottedEnemies), nameof(UIModuleSpottedEnemies.AddCrateObjects))]
        public static class UIModuleSpottedEnemies_AddCrateObjects_patch
        {
            public static void Prefix(UIModuleSpottedEnemies __instance, ref List<TacticalActorBase> crateObjects)
            {
                try
                {

                    List<TacticalActorBase> targetsToRemove = new List<TacticalActorBase>();

                    foreach (TacticalActorBase target in crateObjects)
                    {
                        // TFTVLogger.Always($"[UIModuleSpottedEnemies.AddCrateObjects] looking at {target?.name}");

                        if (target.ViewElementDef == null || target.ViewElementDef.SmallIcon == null)
                        {
                            targetsToRemove.Add(target);
                            TFTVLogger.Always($"[UIModuleSpottedEnemies.AddCrateObjects] {target.name} has no viewelement");
                        }
                    }

                    crateObjects.RemoveRange(targetsToRemove);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIModuleSpottedEnemies), nameof(UIModuleSpottedEnemies.SetAllEnemies))]
        public static class UIModuleSpottedEnemies_SetAllEnemies_patch
        {
            public static void Prefix(UIModuleSpottedEnemies __instance, ref IList<TacticalAbilityTarget> allSortedKnownTargets)
            {
                try
                {

                    List<TacticalAbilityTarget> targetsToRemove = new List<TacticalAbilityTarget>();

                    foreach (TacticalAbilityTarget target in allSortedKnownTargets)
                    {
                        // TFTVLogger.Always($"[UIModuleSpottedEnemies.SetAllEnemies] looking at {target?.Actor?.name}");

                        if (target.Actor != null)
                        {
                            TacticalActorBase tacticalActorBase = target.Actor;

                            if (tacticalActorBase.ViewElementDef == null || tacticalActorBase.ViewElementDef.SmallIcon == null)
                            {
                                targetsToRemove.Add(target);
                                TFTVLogger.Always($"[UIModuleSpottedEnemies.SetAllEnemies] {tacticalActorBase.name} has no viewelement");
                            }
                        }

                    }

                    allSortedKnownTargets.RemoveRange(targetsToRemove);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        //Remove negative damage notices with very large numbers when character with elemental immunity hit by elemental damage
        [HarmonyPatch(typeof(HealthbarUIActorElement), nameof(HealthbarUIActorElement.AddNotificationMessage))]
        public class HealthbarUIActorElement_AddNotificationMessage_VanillaBugFix_Patch
        {
            static bool Prefix(int? val = null)
            {
                try
                {
                    // Check if val is outside the specified range
                    if (val.HasValue && (val.Value > 1000000 || val.Value < -1000000))
                    {
                        //TFTVLogger.Always("it worked");
                        // Return false to cancel the original method call
                        return false;
                    }

                    // Return true to allow the original method call
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }
    }
}
