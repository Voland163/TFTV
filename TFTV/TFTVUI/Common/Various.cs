using Base.Core;
using Base.Levels;
using Base.UI;
using Base.UI.VideoPlayback;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVUI.Common
{
    internal class Various
    {
        [HarmonyPatch(typeof(UIItemTooltip), "SetTacItemStats")] //VERIFIED
        public static class UIItemTooltip_SetTacItemStats_patch
        {

            public static void Postfix(UIItemTooltip __instance, TacticalItemDef tacItemDef, bool secondObject, int subItemIndex = -1)
            {
                try
                {
                    if (tacItemDef == null)
                    {
                        return;
                    }

                    if (tacItemDef is GroundVehicleModuleDef || tacItemDef is ItemDef itemDef &&
                        (itemDef is GeoVehicleEquipmentDef || itemDef is VehicleItemDef || itemDef is GroundVehicleWeaponDef))
                    {
                        //TFTVLogger.Always($"{tacItemDef.name}");
                        return;

                    }

                    //  TFTVLogger.Always($"is not GroundVehicleModuleDef or GeoVehicleEquipmentDef");

                    BodyPartAspectDef bodyPartAspectDef = tacItemDef.BodyPartAspectDef;
                    if (bodyPartAspectDef != null)
                    {
                        if (bodyPartAspectDef.Endurance > 0)
                        {
                            MethodInfo methodInfo = typeof(UIItemTooltip).GetMethod("SetStat", BindingFlags.NonPublic | BindingFlags.Instance);
                            object[] parameters = { new LocalizedTextBind("KEY_PROGRESSION_STRENGTH"), secondObject, UIUtil.StatsWithSign(bodyPartAspectDef.Endurance), bodyPartAspectDef.Endurance, null, subItemIndex };

                            methodInfo.Invoke(__instance, parameters);
                        }

                        if (bodyPartAspectDef.WillPower > 0)
                        {
                            MethodInfo methodInfo = typeof(UIItemTooltip).GetMethod("SetStat", BindingFlags.NonPublic | BindingFlags.Instance);
                            object[] parameters = { new LocalizedTextBind("KEY_PROGRESSION_WILLPOWER"), secondObject, UIUtil.StatsWithSign(bodyPartAspectDef.WillPower), bodyPartAspectDef.WillPower, null, subItemIndex };

                            methodInfo.Invoke(__instance, parameters);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class CutscenesAndSplashscreens
        {
            //Adapted from Mad's Assorted Adjustments, all hail the Great Mad!
            [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")] //VERIFIED
            public static class TFTV_PhoenixGame_RunGameLevel_SkipLogos_Patch
            {
                public static bool Prefix(PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result)
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    try
                    {
                        if (config.SkipMovies)
                        {
                            if (levelSceneBinding == __instance.Def.IntroLevelSceneDef.Binding)
                            {
                                __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
                                return false;
                            }

                            return true;
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            [HarmonyPatch(typeof(UIStateHomeScreenCutscene), "EnterState")] //VERIFIED
            public static class TFTV_PhoenixGame_RunGameLevel_SkipIntro_Patch
            {
                public static void Postfix(UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
                {

                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;
                        if (config.SkipMovies)
                        {
                            if (____sourcePlaybackDef == null)
                            {
                                return;
                            }

                            if (____sourcePlaybackDef.ResourcePath.Contains("Game_Intro_Cutscene"))
                            {
                                typeof(UIStateHomeScreenCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            [HarmonyPatch(typeof(UIStateTacticalCutscene), "EnterState")] //VERIFIED
            public static class TFTV_PhoenixGame_RunGameLevel_SkipLanding_Patch
            {
                public static void Postfix(UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
                {
                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;
                        // TFTVLogger.Always($"UIStateTacticalCutscene EnterState called");

                        if (config.SkipMovies)
                        {

                            //  TFTVLogger.Always($"Skip Movies check passed");

                            if (____sourcePlaybackDef == null)
                            {
                                return;
                            }
                            if (____sourcePlaybackDef.ResourcePath.Contains("LandingSequences"))
                            {
                                // TFTVLogger.Always($"LandingSequence getting canceled");
                                typeof(UIStateTacticalCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(__instance, null);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



        }
    }
}
