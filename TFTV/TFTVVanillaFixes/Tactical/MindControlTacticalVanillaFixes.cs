using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical;
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
        private static GameTagDef _mindfraggerTag;

        internal static bool IsMindfragger(TacticalActorBase actor)
        {
            if (actor == null)
            {
                return false;
            }

            if (_mindfraggerTag == null)
            {
                _mindfraggerTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>("Facehugger_ClassTagDef");
            }

            return _mindfraggerTag != null && actor.HasGameTag(_mindfraggerTag);
        }

        /// <summary>
        /// A Mindfragger's death acid (TFTV's BC_SwarmerAcidExplosion, 2 m) skips other Mindfraggers. They ride the heads
        /// of soldiers standing side by side, so one dying used to kill the next and free its soldier, in a chain.
        /// </summary>
        [HarmonyPatch(typeof(DamageAccumulation), nameof(DamageAccumulation.AddTarget))]
        internal static class DamageAccumulation_AddTarget_MindfraggerChain_Patch
        {
            private static bool Prefix(DamageAccumulation __instance, IDamageReceiver target)
            {
                try
                {
                    TacticalActorBase source = __instance.SourceActor ?? TacUtil.GetSourceOfType<TacticalActorBase>(__instance.Source);

                    // Mindfraggers deal no other damage, so anything from one is its death acid.
                    if (!IsMindfragger(source))
                    {
                        return true;
                    }

                    TacticalActorBase hit = target?.GetActor();

                    if (hit == null || hit == source || !IsMindfragger(hit))
                    {
                        return true;
                    }

                    TFTVLogger.Always($"[Mindfragger] {source.name}'s death acid spares {hit.name}");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(MindControlStatus), nameof(MindControlStatus.WillBreakControl))]
        public static class MindControlStatus_WillBreakControl_patch
        {
            public static bool Prefix(MindControlStatus __instance, bool shouldApplyControlCost, float ____minUpkeepCost, ref bool __result)
            {
                try
                {
                    if (!shouldApplyControlCost || IsMindfragger(__instance.ControllerActor))
                    {
                        // Will points are no factor for a Mindfragger (it can't panic either): its control never
                        // breaks or costs anything for lack of them.
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
