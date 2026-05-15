using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Android;

namespace TFTV.TFTVBaseRework
{
    internal static class PhoenixBaseExplorationConfig
    {

        public const int InfestationChancePercent = 25;

        /// <summary>
        /// Sites currently completing exploration.
        ///
        /// GeoFaction.OnVehicleSiteExplored() calls GeoFaction.UpdateVehicleSite().
        /// We block UpdateVehicleSite() for unexplored Phoenix bases on normal arrival,
        /// but allow it during real exploration completion.
        /// </summary>
        public static readonly HashSet<GeoSite> ExplorationCompletionInProgress = new HashSet<GeoSite>();

        public static bool IsTargetPhoenixBase(GeoSite site)
        {
            return site != null
                && site.Type == GeoSiteType.PhoenixBase
                && site.State == GeoSiteState.Abandoned
                && site.Owner != null
                && site.Owner.IsEnvironmentFaction;
        }

        public static bool IsPhoenixFaction(GeoFaction faction)
        {
            return faction is GeoPhoenixFaction;
        }

        public static bool IsKnownButUnexploredPhoenixBase(GeoSite site, GeoFaction faction)
        {
            return IsTargetPhoenixBase(site)
                && IsPhoenixFaction(faction)
                && site.GetVisible(faction)
                && site.GetInspected(faction)
                && !site.GetVisited(faction);
        }
    }



    /// <summary>
    /// Prevent normal aircraft arrival from marking inspected Phoenix bases as visited.
    ///
    /// Vanilla UpdateVehicleSite() marks any inspected site visited immediately on arrival.
    /// For this mod, abandoned Phoenix bases should only become visited after the
    /// explicit Explore Site action completes.
    /// </summary>
    [HarmonyPatch(typeof(GeoFaction), "UpdateVehicleSite")]
    internal static class GeoFaction_UpdateVehicleSite_BlockAutoVisit_Patch
    {
        private static bool Prefix(GeoFaction __instance, GeoVehicle vehicle, GeoSite site)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return true;
            }

            if (!PhoenixBaseExplorationConfig.IsKnownButUnexploredPhoenixBase(site, __instance))
            {
                return true;
            }

            // Let vanilla UpdateVehicleSite run only while OnVehicleSiteExplored is executing.
            // This allows real exploration completion to mark the site visited.
            if (PhoenixBaseExplorationConfig.ExplorationCompletionInProgress.Contains(site))
            {
                return true;
            }

            // Normal arrival: do not mark visited, do not fire VehicleVisitedSite.
            return false;
        }
    }

    /// <summary>
    /// Allow Explore Site on inspected-but-unvisited abandoned Phoenix bases.
    ///
    /// Vanilla ExploreSiteAbility rejects inspected targets, so we override only this
    /// specific Phoenix-base case.
    /// </summary>
    [HarmonyPatch(typeof(ExploreSiteAbility), "GetTargetDisabledStateInternal")]
    internal static class ExploreSiteAbility_GetTargetDisabledStateInternal_Patch
    {
        private static bool Prefix(
            ExploreSiteAbility __instance,
            GeoAbilityTarget target,
            ref GeoAbilityTargetDisabledState __result)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return true;
            }

            GeoSite site = target.Actor as GeoSite;
            GeoVehicle vehicle = __instance.GeoActor as GeoVehicle;
            GeoFaction viewer = __instance.Viewer;

            if (!PhoenixBaseExplorationConfig.IsKnownButUnexploredPhoenixBase(site, viewer))
            {
                return true;
            }

            if (vehicle == null || vehicle.CurrentSite != site)
            {
                __result = GeoAbilityTargetDisabledState.TargetRequirementsNotMet;
                return false;
            }

            if (site.ExplorationTime == 0f)
            {
                __result = GeoAbilityTargetDisabledState.TargetRequirementsNotMet;
                return false;
            }

            if (!site.GetVisible(viewer))
            {
                __result = GeoAbilityTargetDisabledState.TargetRequirementsNotMet;
                return false;
            }

            __result = GeoAbilityTargetDisabledState.NotDisabled;
            return false;
        }
    }



    /// <summary>
    /// Roll the 15% infestation chance when explicit exploration completes.
    ///
    /// If the roll passes, CreatePhoenixBaseInfestationMission() is called immediately.
    /// Vanilla then marks the site visited and the UI's normal VehicleVisitedSite flow
    /// can notice the active mission.
    /// </summary>
    [HarmonyPatch(typeof(GeoFaction), "OnVehicleSiteExplored")]
    internal static class GeoFaction_OnVehicleSiteExplored_PhoenixBaseInfestation_Patch
    {
        private static void Prefix(GeoFaction __instance, GeoVehicle vehicle)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            GeoSite site = vehicle?.CurrentSite;

            if (!PhoenixBaseExplorationConfig.IsKnownButUnexploredPhoenixBase(site, __instance))
            {
                return;
            }

            PhoenixBaseExplorationConfig.ExplorationCompletionInProgress.Add(site);

            if (site.ActiveMission != null)
            {
                return;
            }

            int roll = UnityEngine.Random.Range(0, 100);

            int chance = PhoenixBaseExplorationConfig.InfestationChancePercent;

            if (site.IsInMist)
            {
                chance *= 2;
            }

            if (roll < chance)
            {
                site.CreatePhoenixBaseInfestationMission();

                TFTVLogger.Always(
                    $"[OnVehicleSiteExplored] Phoenix base exploration infestation triggered at '{site}'. " +
                    $"Roll={roll}, Chance={chance}%"
                );
            }
            else
            {
                TFTVLogger.Always(
                    $"[OnVehicleSiteExplored] Phoenix base exploration infestation did not trigger at '{site}'. " +
                    $"Roll={roll}, Chance={chance}%"
                );
            }
        }

        private static void Postfix(GeoFaction __instance, GeoVehicle vehicle)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            GeoSite site = vehicle?.CurrentSite;

            if (site != null)
            {
                PhoenixBaseExplorationConfig.ExplorationCompletionInProgress.Remove(site);
            }

            // If no mission was triggered by the infestation roll, open the base activation UI
            // so the player can immediately choose to ransack, set up an outpost, or activate.
            if (site == null || site.ActiveMission != null)
            {
                return;
            }

            if (!(__instance is GeoPhoenixFaction faction))
            {
                return;
            }

            ActivateBaseAbility activateAbility = site.GetAbilities<GeoAbility>()
                .OfType<ActivateBaseAbility>()
                .FirstOrDefault();

            if (activateAbility == null)
            {
                return;
            }

            GeoAbilityTarget target = new GeoAbilityTarget(site) { Faction = faction };
            if (!activateAbility.CanActivate(target))
            {
                return;
            }

            if (activateAbility.View != null && activateAbility.View.HasActivationUI)
            {
               // TFTVLogger.Always($"[GeoFaction_OnVehicleSiteExplored] Opening base activation UI for '{site}' after exploration.");
                activateAbility.View.ShowActivationUI(target);
            }
        }
    }

    /// <summary>
    /// Disable vanilla activation-time infestation.
    ///
    /// Vanilla BaseInfestationCheck uses campaign day, mist, and base infestation
    /// counter. This mod wants the only infestation roll to be the 15% exploration
    /// completion roll above.
    /// </summary>
    [HarmonyPatch(typeof(GeoPhoenixBase), nameof(GeoPhoenixBase.BaseInfestationCheck))]
    internal static class GeoPhoenixBase_BaseInfestationCheck_DisableVanilla_Patch
    {
        private static bool Prefix(GeoPhoenixBase __instance, ref bool __result)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return true;
            }

            GeoSite site = __instance.Site;

            if (!PhoenixBaseExplorationConfig.IsTargetPhoenixBase(site))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionModalDataBind), nameof(MissionModalDataBind.ModalShowHandler))]
    internal static class MissionModalDataBind_ModalShowHandler_Patch
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static void Postfix(MissionModalDataBind __instance, UIModal modal)
        {
            //  TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] Checking for base infestation mission to modify briefing.");
            try
            {
                if (__instance == null || modal == null || modal.Data == null)
                {
                    return;
                }

                TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] got here 0");

                GeoMission geoMission = modal.Data as GeoMission;
                
                if(geoMission == null)
                {
                    return;
                }

                TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] got here 1");

                if (geoMission.IsCompleted) return;

                if(geoMission.MissionDef == null) return;

                if (geoMission.MissionDef.MissionTags == null) return;

                TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] got here 2");

                MissionTypeTagDef baseInfestationtTag = DefCache.GetDef<MissionTypeTagDef>("MissionTypeBaseInfestation_MissionTagDef");
               
                
                
                if (!geoMission.MissionDef.MissionTags.Contains(baseInfestationtTag)) return;

                TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] got here 3");

                //  TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] Base infestation mission detected, modifying briefing.");

                if (__instance?.BackgroundImage != null)
                {
                    //    TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] Setting custom background image for base infestation mission.");

                    var sprite = Helper.CreateSpriteFromImageFile("Background_CoolPP.jpg");
                    if (sprite != null)
                    {
                        __instance.BackgroundImage.sprite = sprite;
                        __instance.BackgroundImage.preserveAspect = true;

                        // TFTVLogger.Always($"[MissionModalDataBind_ModalShowHandler_Patch] Custom background image set successfully.");
                    }
                }

                __instance.ObjectiveTitleText.Term = "KEY_MISSION_PX_BASE_INFESTED_BRIEFING_NAME";
                __instance.DescriptionText.Term = "KEY_MISSION_PX_BASE_INFESTED_BRIEFING_DESCRIPTION";


                CommonMissionDataController commonMissionDataController = modal.GetComponentInChildren<CommonMissionDataController>();
                commonMissionDataController.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }

        }
    }


    [HarmonyPatch(typeof(GeoscapeView), "GetMissionBriefModal")]
    internal static class GeoscapeView_BaseInfestationCheck_DisableVanilla_Patch
    {
        private static void Postfix(GeoscapeView __instance, GeoMission mission, ref ModalType __result)
        {
            if (!BaseReworkCheck.BaseReworkEnabled)
            {
                return;
            }

            if (mission is GeoPhoenixBaseInfestationMission && !TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Contains(mission.Site.SiteId))
            {
                TFTVLogger.Always($"[GeoscapeView_GetMissionBriefModal] Returning GeoAmbushBrief for GeoPhoenixBaseInfestationMission.");
                __result = ModalType.GeoAmbushBrief;
            }
        }
    }

    [HarmonyPatch(typeof(GeoPhoenixFaction), "ActivateBaseFromExploration")]
    public static class GeoPhoenixFaction_ActivateBaseFromExploration_Patch
    {

        public static bool Prefix(GeoPhoenixFaction __instance, GeoSite site)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled) return true;

                if (site.ActiveMission != null)
                {
                    site.ActiveMission = null;
                    site.RefreshVisuals();
                    TFTVLogger.Always($"[GeoPhoenixFaction_ActivateBaseFromExploration_Patch] Skipping activation for  " +
                        $"'{site.LocalizedSiteName}' because after infestation.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }
    }
}
