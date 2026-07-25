using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class MindControlTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(MindControlStatus), nameof(MindControlStatus.WillBreakControl))]
        public static class MindControlStatus_WillBreakControl_patch
        {
            public static bool Prefix(MindControlStatus __instance, bool shouldApplyControlCost, float ____minUpkeepCost, ref bool __result)
            {
                try
                {
                    if (!shouldApplyControlCost)
                    {
                        __result = false;
                    }
                    else
                    {
                        StatusStat willPoints = __instance.ControllerActor.CharacterStats.WillPoints;
                        float num = Mathf.Max(____minUpkeepCost, __instance.TacticalActor.TacticalActorDef.WillPointWorth);

                        if (TFTVDrills.DrillsHarmony.VirulentGrip.CheckForVirulentGripAbility(__instance.ControllerActor, __instance.TacticalActor))
                        {
                            num /= 2;
                            TFTVLogger.Always($"halving cost of MCing {__instance.TacticalActor.name} because infected and {__instance.ControllerActor.DisplayName} has VirulentGrip ability");
                        }

                        bool flag = (willPoints - num) < 0;
                        if (flag)
                        {
                            __instance.RequestUnapply(__instance.TacticalActorBase.Status);
                        }
                        else
                        {
                            willPoints.Subtract(num);
                        }
                        __result = flag;
                    }


                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        //Prevents MC status from throwing errors because of dereferencing.
        [HarmonyPatch(typeof(MindControlStatus), nameof(MindControlStatus.OnUnapply))]
        internal static class MindControlStatus_OnUnapply_DebugPatch
        {

            [HarmonyPrefix]
            private static bool Prefix(MindControlStatus __instance)
            {
                TacticalActor target = Safe(() => __instance?.TacticalActor);
                TacticalLevelController level = Safe(() => __instance?.TacticalLevel);

                if (__instance == null || target == null || level == null)
                {
                    Debug.LogWarning($"[MCDBG] OnUnapply skipped: status/target/level is null or unavailable. {__instance == null} {target == null} {level == null}");
                    return false;
                }

                return true;
            }


            private static T Safe<T>(Func<T> getter) where T : class
            {
                try
                {
                    return getter();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
