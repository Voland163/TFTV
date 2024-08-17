﻿using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV
{
    internal class TFTVHarmonyGeoscapeUI
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(UIModuleInfoBar), "Init")]
        public static class TFTV_UIModuleInfoBar_Init_GeoscapeUI_Patch
        {
            public static void Prefix(UIModuleInfoBar __instance)
            {
                try
                {
                    TFTVUIGeoMap.AdjustInfoBarGeoscape(__instance);



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

        [HarmonyPatch(typeof(GeoObjectiveElementController), "SetObjective")]
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
        /// </summary>

        [HarmonyPatch(typeof(GeoSiteVisualsController), "RefreshSiteVisuals")]
        public static class GeoSiteVisualsController_RefreshSiteVisuals_Patch
        {

            public static void Postfix(GeoSiteVisualsController __instance, GeoSite site)
            {
                try
                {
                    TFTVAAAgendaTracker.ExtendedAgendaTracker.RecolorTimerBaseAndAncientSiteAttacks(__instance, site);
                    TFTVBaseDefenseGeoscape.Visuals.RefreshBaseDefenseVisuals(__instance, site);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateRosterDeployment), "SetUpInitialDeployment")]
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

        [HarmonyPatch(typeof(UIStateRosterDeployment), "OnDeploySquad")]
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

        



          [HarmonyPatch(typeof(UIInventoryList), "SetItems")]
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
