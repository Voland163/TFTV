using Base.Core;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UITooltip;

namespace TFTV
{
    internal class TFTVHarmonyGeoscapeUI
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        

        [HarmonyPatch(typeof(EditUnitButtonsController), nameof(EditUnitButtonsController.SetEditUnitButtonsBasedOnType))]
        internal static class TFTV_EditUnitButtonsController_SetEditUnitButtonsBasedOnType_DeliriumPerksCured_patch
        {
            public static void Postfix(UIModuleActorCycle ____parentModule)
            {
                try
                {
                    TFTVDelirium.DeliriumPerkRecoveryPrompt(____parentModule);
                    TFTVUI.EditScreen.LoadoutsAndHelmetToggle.ShowAndHideHelmetAndLoadoutButtons(____parentModule);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.Init), new Type[] { typeof(string), typeof(int), typeof(float), typeof(float), typeof(float), typeof(Position), typeof(GameObject) })]
        public static class UITooltip_Init_patch
        {
            public static void Prefix(UITooltip __instance, string tipText, ref int maxWidth, float appearTime, float fadeInTime, float fadeOutTime, Position pos, GameObject parent,
                   ref float ____appearDelay, ref float ____fadeInSpeed, ref float ____fadeOutSpeed, ref GameObject ____target, ref Position ____position)
            {

                try
                {
                    if (GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>() != null)
                    {
                        TFTVUIGeoMap.UnpoweredFacilitiesInfo.CheckTopBarTooltip(__instance, parent);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIFacilityInfoPopup), nameof(UIFacilityInfoPopup.Show))]
        public static class UIFacilityInfoPopup_Show_PreventBadDemolition_patch
        {

            public static void Postfix(UIFacilityInfoPopup __instance, GeoPhoenixFacility facility)
            {
                try
                {
                    TFTVBaseDefenseGeoscape.BaseFacilities.EnsureCorrectLayout.ProhibitDemolitionToAvoidCuttingEntranceOff(__instance, facility);
                    TFTVBaseDefenseGeoscape.BaseFacilities.PreventPowerOn(__instance, facility);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }



        [HarmonyPatch(typeof(UIModuleInfoBar), nameof(UIModuleInfoBar.Init))]
        public static class TFTV_UIModuleInfoBar_Init_GeoscapeUI_Patch
        {
            public static void Prefix(UIModuleInfoBar __instance)
            {
                try
                {
                    TFTVUIGeoMap.TopInfoBar.AdjustInfoBarGeoscape(__instance);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void Postfix(UIModuleInfoBar __instance)
            {
                try
                {
                    TFTVAAAgendaTracker.ExtendedAgendaTracker.StoreSpritesForTrackerAndObjectivesList(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        /// <summary>
        /// Add timer to base defense objectives
        /// Add an icon for *secondary* objectives of a faction without icon set (Environment, Inactive)
        /// </summary>

        [HarmonyPatch(typeof(GeoObjectiveElementController), nameof(GeoObjectiveElementController.SetObjective))]
        public static class GeoObjectiveElementController_SetObjective_Patch
        {


            public static void Prefix(ref Sprite icon, ref Color iconColor, string objectiveText, ref Timing ____levelTiming, ref TimeUnit ____endTime)
            {
                try
                {
                    GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    foreach (GeoSite geoSite in geoLevelController.PhoenixFaction.Sites)
                    {
                        if (objectiveText.Contains(geoSite.LocalizedSiteName))
                        {
                            ____levelTiming = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().Timing;
                            ____endTime = geoSite.ExpiringTimerAt;

                        }
                    }

                    if (icon == null)
                    {
                        TFTVLogger.Debug($"[GeoObjectiveElementController_SetObjective_PREFIX] Icon is null, setting a custom one.");

                        // Fallback to some prepared sprite
                        icon = TFTVAAAgendaTracker.ExtendedAgendaTracker.archeologyLabSprite;
                        iconColor = Color.white;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Recolor the timer on geoscape sites for base/ancient site attacks
        /// Controls visuals for Base Defense
        /// Add marker for base with unpowered facilities
        /// </summary>

        [HarmonyPatch(typeof(GeoSiteVisualsController), "RefreshSiteVisuals")] //VERIFIED
        public static class GeoSiteVisualsController_RefreshSiteVisuals_Patch
        {

            public static void Postfix(GeoSiteVisualsController __instance, GeoSite site)
            {
                try
                {
                    TFTVAAAgendaTracker.ExtendedAgendaTracker.RecolorTimerBaseAndAncientSiteAttacks(__instance, site);
                    TFTVUIGeoMap.UnpoweredFacilitiesInfo.AddBlinkingPowerMarkerGeoMap(__instance, site);
                    TFTVBaseDefenseGeoscape.Visuals.RefreshBaseDefenseVisuals(__instance, site);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

     /*   [HarmonyPatch(typeof(GeoFaction), "GetEligibleTemplatesForMission")]
        public static class GeoFaction_GetEligibleTemplatesForMission_Patch
        {

            public static void Postfix(GeoFaction __instance, IEnumerable<TacCharacterDef> __result)
            {
                try
                {
                    foreach(TacCharacterDef tacCharacterDef in __result) 
                    {

                        TFTVLogger.Always($"{tacCharacterDef.name}");
                    }

                    TFTVLogger.Always($"unlockedTemplates:");

                    foreach (TacCharacterDef tacCharacterDef in __instance.UnlockedUnitTemplates)
                    {

                        TFTVLogger.Always($"{tacCharacterDef.name}");
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/


         


        [HarmonyPatch(typeof(UIStateRosterDeployment), "SetUpInitialDeployment")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_SetUpInitialDeployment_patch
        {
            public static void Postfix(UIStateRosterDeployment __instance, List<GeoRosterDeploymentItem> ____deploymentItems)
            {
                try
                {
                    TFTVBaseDefenseGeoscape.Deployment.UI.ModifyForBaseDefense(__instance, ____deploymentItems);
                    TFTVBehemothAndRaids.Behemoth.BehemothMission.ModifyForBehemothMission(__instance, ____deploymentItems);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterDeployment), "OnDeploySquad")] //VERIFIED
        public static class TFTV_UIStateRosterDeployment_OnDeploySquad_patch
        {
            public static void Postfix(UIStateRosterDeployment __instance, List<GeoRosterDeploymentItem> ____deploymentItems)
            {
                try
                {
                    TFTVBaseDefenseGeoscape.Deployment.UI.CheckBeforeDeployment(__instance);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), nameof(UIInventoryList.UpdateList))]
        public static class UIInventoryList_UpdateList_patch
        {
            public static bool Prefix(UIInventoryList __instance, TacticalActorBaseDef ____vehicle)
            {
                try
                {
                    // MethodInfo methodInfoRecalculateVehicleFilter = typeof(UIInventoryList).GetMethod("RecalculateVehicleFilter", BindingFlags.Instance | BindingFlags.NonPublic);


                    __instance.EnsureSize();

                    if (____vehicle != null)
                    {

                        foreach (UIInventorySlot slot in __instance.Slots)
                        {
                            slot.TryRecalculateSlotsFilter(____vehicle);
                        }


                        // methodInfoRecalculateVehicleFilter.Invoke(__instance, new object[] { ____vehicle }); 
                    }

                    List<ICommonItem> filteredItems = __instance.FilteredItems;
                    List<ICommonItem> list = new List<ICommonItem>();

                    foreach (ICommonItem item in filteredItems)
                    {
                        UIInventorySlot firstAvailableSlot = __instance.GetFirstAvailableSlot(item);
                        if (!(firstAvailableSlot == null))
                        {
                            if (firstAvailableSlot.Item != null)
                            {
                                //TFTVLogger.Always($"Inventory list trying to stack filtered items {item.ItemDef.name}, {firstAvailableSlot.name}, vehicle: {____vehicle?.name}");

                                /*  if (____vehicle != null)
                                  {

                                      firstAvailableSlot.Item.CommonItemData.AddItem(item);
                                      list.Add(item);
                                  }*/
                            }
                            else
                            {
                                firstAvailableSlot.Item = item;
                            }
                        }
                    }

                    var field = AccessTools.Property(typeof(UIInventoryList), "FilteredItems");
                    if (field != null)
                    {
                        field.SetValue(__instance, __instance.FilteredItems.Except(list).ToList());
                    }

                    __instance.NavigationElementsHolder?.RefreshNavigation();

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(UIInventoryList), nameof(UIInventoryList.SetItems))]
        public static class UIInventoryList_SetItems_patch
        {
            public static void Prefix(UIInventoryList __instance)
            {
                try
                {
                    __instance.ShouldHidePartialMagazines = false;
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
