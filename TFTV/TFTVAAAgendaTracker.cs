using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Factions.Archeology;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVAAAgendaTracker
    {
        //This has been copy pasted from the great Mad's Assorted Adjustments (all hail Mad!) with minimal adjustments
        //https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments/tree/main/Source/AssortedAdjustments

        //  public static bool KludgeCheck = false;

        internal static class ExtendedAgendaTracker
        {
            private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

            internal static bool fetchedSiteNames = false;
            internal static string unexploredSiteName = "UNEXPLORED SITE";
            internal static string explorationSiteName = "EXPLORATION SITE";
            internal static string scavengingSiteName = "SCAVENGING SITE";
            internal static string ancientSiteName = "ANCIENT SITE";

            internal static string actionExploring = "INVESTIGATES";
            internal static string actionTraveling = "TRAVELS TO";
            internal static string actionRepairing = "REPAIRING:";
            internal static string actionExcavating = "EXCAVATING:";
            internal static string actionAcquire = "SECURE";
            internal static string actionAttack = "WILL ATTACK:";
            internal static string actionAttackOnPX = "WILL COMPLETE ATTACK ON";

            public static void LocalizeExtendedAgendaUI()
            {
                try
                {
                    unexploredSiteName = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_UNEXPLORED_SITE" }.Localize();
                    explorationSiteName = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_EXPLORATION_SITE" }.Localize();
                    scavengingSiteName = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_SCAVENGING_SITE" }.Localize();
                    ancientSiteName = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_ANCIENT_SITE" }.Localize();
                    actionExploring = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_INVESTIGATES" }.Localize();
                    actionTraveling = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_TRAVELS_TO" }.Localize();
                    actionRepairing = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_REPAIRING" }.Localize();
                    actionExcavating = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_EXCAVATING" }.Localize();
                    actionAcquire = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_SECURE" }.Localize();
                    actionAttack = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_WILL_ATTACK" }.Localize();
                    actionAttackOnPX = new LocalizedTextBind() { LocalizationKey = "EXTENDED_AGENDA_KEY_WILL_COMPLETE_ATTACK_ON" }.Localize();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            //internal static string actionAttack = "DEFEND {0} AGAINST {1}";

            internal static Sprite aircraftSprite = null;
            internal static Sprite ancientSiteProbeSprite = null;
            internal static Sprite archeologyLabSprite = null;
            internal static Sprite phoenixFactionSprite = null;

            internal static Color vehicleTrackerColor = new Color32(251, 191, 31, 255);
            internal static Color manufactureTrackerColor = new Color32(235, 110, 42, 255);
            internal static Color researchTrackerColor = new Color32(42, 245, 252, 255);
            internal static Color excavationTrackerColor = new Color32(93, 153, 106, 255);
            internal static Color baseAttackTrackerColor = new Color32(192, 32, 32, 255);
            internal static Color facilityTrackerColor = new Color32(185, 185, 185, 255);
            internal static Color defaultTrackerColor = new Color32(155, 155, 155, 255);
            internal static UIFactionDataTrackerElement trackerElementDefault = null;

            // Cache reflected methods
            internal static MethodInfo ___GetFreeElement = typeof(UIModuleFactionAgendaTracker).GetMethod("GetFreeElement", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static MethodInfo ___OnAddedElement = typeof(UIModuleFactionAgendaTracker).GetMethod("OnAddedElement", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static MethodInfo ___OrderElements = typeof(UIModuleFactionAgendaTracker).GetMethod("OrderElements", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static MethodInfo ___UpdateData = typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
            internal static MethodInfo ___Dispose = typeof(UIModuleFactionAgendaTracker).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);

            // Cache tracker
            internal static UIModuleFactionAgendaTracker ___factionTracker = null;




            /*
             **
             *** Utility methods
             ** 
            */
            private static bool GetTravelTime(GeoVehicle vehicle, out float travelTime, GeoSite target = null)
            {
                travelTime = 0f;

                if (target == null && vehicle.FinalDestination == null)
                {
                    return false;
                }

                var currentPosition = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
                var targetPosition = target == null ? vehicle.FinalDestination.WorldPosition : target.WorldPosition;
                var travelPath = vehicle.Navigation.FindPath(currentPosition, targetPosition, out bool hasTravelPath);

                if (!hasTravelPath || travelPath.Count < 2)
                {
                    return false;
                }

                float distance = 0;

                for (int i = 0, len = travelPath.Count - 1; i < len;)
                {
                    distance += GeoMap.Distance(travelPath[i].Pos.WorldPosition, travelPath[++i].Pos.WorldPosition).Value;
                }

                travelTime = distance / vehicle.Stats.Speed.Value;
                //TFTVLogger.Debug($"[ExtendedAgendaTracker_GetTravelTime] travelTime: {travelTime}");

                return true;
            }

            private static float GetExplorationTime(GeoVehicle vehicle, float hours)
            {
                try
                {
                    if (vehicle == null)
                    {
                        return hours;
                    }

                    object updateable = typeof(GeoVehicle).GetField("_explorationUpdateable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(vehicle);

                    if (updateable == null)
                    {
                        return hours;
                    }

                    NextUpdate endTime = (NextUpdate)updateable.GetType().GetProperty("NextUpdate").GetValue(updateable);
                    return (float)-(vehicle.Timing.Now - endTime.NextTime).TimeSpan.TotalHours;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return hours;
                }
            }

            private static string GetSiteName(GeoSite site, GeoFaction faction)
            {
                string siteName = null;

                if (String.IsNullOrEmpty(siteName))
                {
                    if (site.GetInspected(faction))
                    {
                        if (site.Type == GeoSiteType.PhoenixBase)
                        {
                            siteName = site.Name;
                        }
                        else if (site.Type == GeoSiteType.Haven)
                        {
                            siteName = site.Name;
                        }
                        else if (site.Type == GeoSiteType.AlienBase)
                        {
                            GeoAlienBase alienBase = site.GetComponent<GeoAlienBase>();
                            GeoAlienBaseTypeDef alienBaseTypeDef = alienBase.AlienBaseTypeDef;

                            siteName = alienBaseTypeDef.Name.Localize();
                        }
                        else if (site.Type == GeoSiteType.Scavenging)
                        {
                            siteName = scavengingSiteName;
                        }
                        else if (site.Type == GeoSiteType.Exploration)
                        {
                            siteName = explorationSiteName;
                        }
                        else if (site.IsArcheologySite)
                        {
                            siteName = ancientSiteName;
                        }
                    }
                    else
                    {
                        siteName = unexploredSiteName;
                    }
                }

                // Last resort
                if (String.IsNullOrEmpty(siteName))
                {
                    siteName = "POI";
                }

                return $"{siteName}";
            }

            private static string AppendTime(float hours)
            {
                string prefix = "   ~ ";
                string time = HoursToText(hours);
                string postfix = "";

                return $"{prefix}{time}{postfix}";
            }

            private static string HoursToText(float hours)
            {
                TimeUnit timeUnit = TimeUnit.FromHours(hours);
                TimeRemainingFormatterDef timeFormatter = new TimeRemainingFormatterDef
                {
                    DaysText = new LocalizedTextBind("{0}d", true),
                    HoursText = new LocalizedTextBind("{0}h", true)
                };
                string timeString = UIUtil.FormatTimeRemaining(timeUnit, timeFormatter);

                return timeString;
            }

            private static void AddAncientSiteEncounterObjective(/*UIStateVehicleSelected uiStateVehicleSelected, */GeoPhoenixFaction geoPhoenixFaction, SiteExcavationState excavatingSite)
            {
                //GeoscapeViewContext ___Context = (GeoscapeViewContext)AccessTools.Property(typeof(GeoscapeViewState), "Context").GetValue(uiStateVehicleSelected, null);
                //GeoPhoenixFaction geoPhoenixFaction = (GeoPhoenixFaction)___Context.ViewerFaction;
                //UIModuleGeoObjectives ____objectivesModule = (UIModuleGeoObjectives)AccessTools.Property(typeof(UIStateVehicleSelected), "_objectivesModule").GetValue(__instance, null);
                //GeoscapeFactionObjectiveSystem FactionObjectiveSystem = ___Context.Level.FactionObjectiveSystem;

                if (excavatingSite.Site.ActiveMission != null)
                {
                    TFTVLogger.Debug($"[AddAncientSiteEncounterObjective] Adding objective for mission: {excavatingSite.Site.ActiveMission?.MissionDef.Description.Localize()} at excavatingSite: {excavatingSite.Site.Name}");

                    MissionGeoFactionObjective objective = new MissionGeoFactionObjective(excavatingSite.Site.Owner, excavatingSite.Site.ActiveMission)
                    {
                        //Title = $"{actionAcquire} {excavatingSite.Site.Name}",
                        Title = excavatingSite.Site.ActiveMission.MissionName,
                        Description = excavatingSite.Site.ActiveMission.MissionDescription
                    };

                    geoPhoenixFaction.AddObjective(objective);
                }
            }



            /*
             **
             *** Minor Patches
             ** 
            */

            // Store tracker and site labels to use them in tracker
            [HarmonyPatch(typeof(UIStateVehicleSelected), "EnterState")]
            public static class UIStateVehicleSelected_EnterState_Patch
            {

                public static void Postfix(UIStateVehicleSelected __instance)
                {
                    try
                    {
                        if (___factionTracker == null)
                        {
                            ___factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                            TFTVLogger.Debug($"[UIStateVehicleSelected_EnterState_POSTFIX] Cached UIModuleFactionAgendaTracker");
                        }

                        if (!fetchedSiteNames)
                        {
                            UIModuleSelectionInfoBox ____selectionInfoBoxModule = (UIModuleSelectionInfoBox)AccessTools.Property(typeof(UIStateVehicleSelected), "_selectionInfoBoxModule").GetValue(__instance, null);

                            unexploredSiteName = ____selectionInfoBoxModule.UnexploredSiteTextKey.Localize();
                            explorationSiteName = ____selectionInfoBoxModule.ExplorationSiteTextKey.Localize();
                            scavengingSiteName = ____selectionInfoBoxModule.ScavengingSiteTextKey.Localize();
                            ancientSiteName = ____selectionInfoBoxModule.AncientSiteTextKey.Localize();

                            fetchedSiteNames = true;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            public static void StoreSpritesForTrackerAndObjectivesList(UIModuleInfoBar uIModuleInfoBar)
            {
                try
                {
                    if (aircraftSprite == null)
                    {
                        aircraftSprite = uIModuleInfoBar.AirVehiclesLabel.transform.parent.gameObject.GetComponentInChildren<Image>(true).sprite;
                    }

                    if (ancientSiteProbeSprite == null)
                    {
                        ancientSiteProbeSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [AncientSiteProbeAbilityDef]").SmallIcon;
                    }

                    if (archeologyLabSprite == null)
                    {
                        archeologyLabSprite = DefCache.GetDef<ViewElementDef>("E_ViewElement [ArcheologyLab_PhoenixFacilityDef]").SmallIcon; 
                    }

                    if (phoenixFactionSprite == null)
                    {
                        phoenixFactionSprite = DefCache.GetDef<GeoFactionViewDef>("E_Phoenix_GeoFactionView [Phoenix_GeoPhoenixFactionDef]").FactionIcon; 
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            // Disable the last safeguard for starting a base defense mission.
            // Vanilla will cancel the assault if no alien base in range is active anymore (because it was detected after a haven defence and subsequently destroyed)
            // With this patch the assault WILL happen and there's no silent abort (because it feels like a bug)
  
            
            [HarmonyPatch(typeof(GeoAlienFaction), "StartPhoenixBaseAssault")]
            public static class GeoAlienFaction_StartPhoenixBaseAssault_Patch
            {

                // Override
                public static bool Prefix(GeoAlienFaction __instance, SiteAttackSchedule target)
                {
                    try
                    {
                        TFTVLogger.Debug($"[GeoAlienFaction_StartPhoenixBaseAssault_PREFIX] Base assault on {target.Site.Name} should start");

                        GeoSite pxBase = target.Site;
                        IEnumerable<GeoAlienBase> source = from p in __instance.Bases where p.SitesInRange.Contains(pxBase) select p;
                        if (!source.Any<GeoAlienBase>())
                        {
                            TFTVLogger.Debug($"[GeoAlienFaction_StartPhoenixBaseAssault_PREFIX] Original method would cancel the mission as there is no alien base in range left. Overriding. Starting mission NOW.");
                            //return;
                        }
                        pxBase.CreatePhoenixBaseDefenseMission(new PhoenixBaseAttacker(__instance, from s in source select s.Site));

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            // Disables the big dumb buttons for excavations and base defense
            [HarmonyPatch(typeof(UIModuleStatusBarMessages), "Update")]
            public static class UIModuleStatusBarMessages_Update_Patch
            {
                public static void Postfix(UIModuleStatusBarMessages __instance)
                {
                    try
                    {
                        // @ToDo: Put somewhere else? Destroy completely?
                        __instance.TimedEventRoot.gameObject.SetActive(false);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            // Prefix the times with an "~"
            [HarmonyPatch(typeof(UIFactionDataTrackerElement), "SetTime")]
            public static class UIFactionDataTrackerElement_SetTime_Patch
            {
                public static void Postfix(UIFactionDataTrackerElement __instance)
                {
                    try
                    {
                        string org = __instance.TrackedTime.text;
                        string pre = "~ ";

                        __instance.TrackedTime.text = $"{pre}{org}";
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            //NOTE TO SELF: maybe not necessary?
            public static void RecolorTimerBaseAndAncientSiteAttacks(GeoSiteVisualsController geoSiteVisualsController, GeoSite site)
            {

                try
                {
                    bool isRelevantPhoenixBase = site.Type == GeoSiteType.PhoenixBase && site.IsActiveSite;
                    bool isRelevantAncientSite = site.IsArcheologySite && site.Owner is GeoPhoenixFaction;
                    bool isRelevantSite = isRelevantPhoenixBase || isRelevantAncientSite;

                    if (!isRelevantSite)
                    {
                        return;
                    }

                    GeoLevelController geoLevel = site.GeoLevel;
                
                    bool isScheduledForAttack = false;
                    foreach (GeoFaction geoFaction in geoLevel.Factions)
                    {
                        if (geoFaction.IsViewerFaction || geoFaction.IsEnvironmentFaction || geoFaction.IsNeutralFaction || geoFaction.IsInactiveFaction)
                        {
                            continue;
                        }
                        foreach (SiteAttackSchedule phoenixBaseAttackSchedule in geoFaction.PhoenixBaseAttackSchedule)
                        {
                            if (phoenixBaseAttackSchedule.HasAttackScheduled && phoenixBaseAttackSchedule.Site == site)
                            {
                                isScheduledForAttack = true;
                                goto quitLoop;
                            }
                        }
                        foreach (SiteAttackSchedule ancientSiteAttackSchedule in geoFaction.AncientSiteAttackSchedule)
                        {
                            if (ancientSiteAttackSchedule.HasAttackScheduled && ancientSiteAttackSchedule.Site == site)
                            {
                                isScheduledForAttack = true;
                                goto quitLoop;
                            }
                        }
                    }

                quitLoop:
                    if (isScheduledForAttack && site.Type != GeoSiteType.PhoenixBase)
                    {
                        TFTVLogger.Debug($"[GeoSiteVisualsController_RefreshSiteVisuals_POSTFIX] Site: {site.Name} is scheduled for an attack.");

                        // Works
                        foreach (Renderer r in geoSiteVisualsController.TimerController.gameObject.GetComponentsInChildren<Renderer>())
                        {

                            r.gameObject.SetActive(true);
                            if (r.name == "TimedIcon")
                            {
                                r.material.color = baseAttackTrackerColor;

                            }

                            // } 
                        }
                    }
                    else if (isScheduledForAttack)
                    {
                        geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

           
            // Displays expected durations of Move and Explore abilities to the context menu
            [HarmonyPatch(typeof(UIModuleSiteContextualMenu), "SetMenuItems")]
            public static class UIModuleSiteContextualMenu_SetMenuItems_Patch
            {

                public static void Postfix(GeoSite site, List<SiteContextualMenuItem> ____menuItems)
                {
                    try
                    {
                        foreach (SiteContextualMenuItem item in ____menuItems)
                        {
                            GeoVehicle vehicle = item.Ability?.GeoActor as GeoVehicle;

                            if (item.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle v && v.CurrentSite != site)
                            {
                                if (GetTravelTime(v, out float eta, site))
                                {
                                    item.ItemText.text += AppendTime(eta);
                                }
                            }
                            else if (item.Ability is ExploreSiteAbility explore)
                            {
                                float hours = GetExplorationTime(vehicle, (float)site.ExplorationTime.TimeSpan.TotalHours);
                                item.ItemText.text += AppendTime(hours);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            /*
             **
             *** Patches that ADD NEW types of tracker items OR UPDATE these at various events
             ** 
            */

            // ADDs or UPDATEs excavations of the tracker
            [HarmonyPatch(typeof(GeoscapeLog), "PhoenixFaction_OnExcavationStarted")]
            public static class GeoscapeLog_PhoenixFaction_OnExcavationStarted_Patch
            {

                public static void Postfix(GeoFaction faction, SiteExcavationState excavation)
                {
                    try
                    {
                        if (___factionTracker == null)
                        {
                            return;
                        }

                        TFTVLogger.Debug($"[GeoscapeLog_PhoenixFaction_OnExcavationStarted_POSTFIX] {faction.Name.Localize(null)} will excavate {excavation.Site.LocalizedSiteName}");

                        GeoSite geoSite = excavation.Site;
                        string siteName = geoSite.LocalizedSiteName;
                        string siteInfo = $"{actionExcavating} {siteName}";

                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);
                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Update
                            if (trackedElement.TrackedObject is GeoSite geoSite1 && geoSite1 == geoSite)
                            {
                                TFTVLogger.Debug($"[GeoscapeLog_PhoenixFaction_OnExcavationStarted_POSTFIX] {geoSite.Name} already tracked. Updating.");

                                trackedElement.TrackedName.text = siteInfo;
                                ___UpdateData.Invoke(___factionTracker, null);

                                // Return early as the first match is always the visible one
                                return;
                            }
                        }

                        // Add
                        TFTVLogger.Debug($"[GeoscapeLog_PhoenixFaction_OnExcavationStarted_POSTFIX] {geoSite.Name} currently not tracked. Adding to tracker.");

                        ViewElementDef borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("0481b9e2-947c-fbb2-3d96-8f769e1e05cd");
                        UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(___factionTracker, null);
                        freeElement.Init(geoSite, siteInfo, borrowedViewElementDef, false);

                        ___OnAddedElement.Invoke(___factionTracker, new object[] { freeElement });



                        // Add a timer to the site too?
                        //geoSite.ExpiringTimerAt = excavation.ExcavationEndDate;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            // ADDs or UPDATEs site defenses of the tracker
            [HarmonyPatch(typeof(GeoscapeLog), "ShowSiteDefenseTimer")]
            public static class GeoscapeLog_ShowSiteDefenseTimer_Patch
            {
                public static void Postfix(GeoFaction faction, SiteAttackSchedule target)
                {
                    try
                    {
                        if (___factionTracker == null)
                        {
                            return;
                        }

                        TFTVLogger.Debug($"[GeoscapeLog_ShowSiteDefenseTimer_POSTFIX] {faction.Name.Localize(null)} will attack {target.Site.LocalizedSiteName}");

                        GeoSite geoSite = target.Site;
                        string siteName = geoSite.LocalizedSiteName;
                        string siteInfo = $"{faction.Name.Localize(null).ToUpperInvariant()} {actionAttack} {siteName}";
                        //string siteInfo = string.Format(actionAttack, siteName, faction.Name.Localize(null));


                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);
                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Update
                            if (trackedElement.TrackedObject is GeoSite geoSite1 && geoSite1 == geoSite)
                            {
                                TFTVLogger.Debug($"[GeoscapeLog_ShowSiteDefenseTimer_POSTFIX] {geoSite.Name} already tracked. Updating.");


                                trackedElement.TrackedName.text = siteInfo;
                                ___UpdateData.Invoke(___factionTracker, null);

                                // Return early as the first match is always the visible one
                                return;
                            }
                        }

                        // Add
                        TFTVLogger.Debug($"[GeoscapeLog_ShowSiteDefenseTimer_POSTFIX] {geoSite.Name} currently not tracked. Adding to tracker.");

                        ViewElementDef borrowedViewElementDef;
                        if (geoSite.IsArcheologySite)
                        {
                            borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("e0148714-8520-fa24-cba8-1a92c20f93cc");//.DefRepositoryDef.AllDefs.OfType<ViewElementDef>().Where(def => def.name.Contains("ArcheologyLab_PhoenixFacilityDef")).FirstOrDefault();
                        }
                        else
                        {
                            borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("8188f3a3-befd-e463-f345-4af1815cd848");// DefRepositoryDef.AllDefs.OfType<ViewElementDef>().Where(def => def.name.Contains("Crabman_ActorViewDef")).FirstOrDefault();
                        }


                        UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(___factionTracker, null);


                        freeElement.Init(geoSite, siteInfo, borrowedViewElementDef, false);

                        ___OnAddedElement.Invoke(___factionTracker, new object[] { freeElement });



                        geoSite.ExpiringTimerAt = target.ScheduledFor;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            // ADDs or UPDATEs vehicle related items of the tracker
            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnContextualItemSelected")]
            public static class UIStateVehicleSelected_OnContextualItemSelected_Patch
            {
                public static void Postfix(GeoAbility ability)
                {
                    try
                    {
                        TFTVLogger.Debug($"[UIStateVehicleSelected_OnContextualItemSelected_POSTFIX] ability: {ability.AbilityDef.name}");

                        GeoVehicle vehicle = null;
                        string siteName = "ERR";
                        string vehicleInfo = "ERR";

                        if (ability is MoveVehicleAbility)
                        {
                            vehicle = (GeoVehicle)ability.GeoActor;
                            siteName = GetSiteName(vehicle.FinalDestination, vehicle.Owner);
                            vehicleInfo = $"{vehicle.Name} {actionTraveling} {siteName}";
                        }
                        else if (ability is ExploreSiteAbility)
                        {
                            vehicle = (GeoVehicle)ability.GeoActor;
                            siteName = GetSiteName(vehicle.CurrentSite, vehicle.Owner);
                            vehicleInfo = $"{vehicle.Name} {actionExploring} {siteName}";
                        }

                        if (vehicle == null)
                        {
                            return;
                        }

                        //UIModuleFactionAgendaTracker ____factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);

                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Update
                            if (trackedElement.TrackedObject is GeoVehicle geoVehicle && geoVehicle == vehicle)
                            {
                                TFTVLogger.Debug($"[UIStateVehicleSelected_OnContextualItemSelected_POSTFIX] {vehicle.Name} already tracked. Updating.");

                                trackedElement.TrackedName.text = vehicleInfo;

                                //AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_needsRefresh").SetValue(___factionTracker, true);
                                //MethodInfo ___UpdateData = typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
                                ___UpdateData.Invoke(___factionTracker, null);
                                ___OrderElements.Invoke(___factionTracker, null);

                                // Return early as the first match is always the visible one
                                return;
                            }
                        }

                        // Add
                        TFTVLogger.Debug($"[UIStateVehicleSelected_OnContextualItemSelected_POSTFIX] {vehicle.Name} currently not tracked. Adding to tracker.");

                        //MethodInfo ___GetFreeElement = typeof(UIModuleFactionAgendaTracker).GetMethod("GetFreeElement", BindingFlags.NonPublic | BindingFlags.Instance);
                        //MethodInfo ___OnAddedElement = typeof(UIModuleFactionAgendaTracker).GetMethod("OnAddedElement", BindingFlags.NonPublic | BindingFlags.Instance);

                        UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(___factionTracker, null);
                        freeElement.Init(vehicle, vehicleInfo, vehicle.VehicleDef.ViewElement, false);
                        //freeElement.Init(vehicle, vehicleInfo, null, false);

                        ___OnAddedElement.Invoke(___factionTracker, new object[] { freeElement });
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            // ADDs or UPDATEs vehicle related items of the tracker
            // NOTE that this is EXCLUSIVELY called from UIStateVehicleSelected.OnRightClickSelection()
            [HarmonyPatch(typeof(UIStateVehicleSelected), "AddTravelSite")]
            public static class UIStateVehicleSelected_AddTravelSite_Patch
            {
                public static void Postfix(UIStateVehicleSelected __instance, GeoSite site)
                {
                    try
                    {
                        GeoVehicle ___SelectedVehicle = (GeoVehicle)AccessTools.Property(typeof(UIStateVehicleSelected), "SelectedVehicle").GetValue(__instance, null);
                        MoveVehicleAbility ability = ___SelectedVehicle.GetAbility<MoveVehicleAbility>();

                        if (ability == null)
                        {
                            return;
                        }
                        GeoAbilityTarget target = new GeoAbilityTarget(site);
                        if (!ability.CanActivate(target))
                        {
                            return;
                        }

                        GeoVehicle vehicle = ___SelectedVehicle;
                        string siteName = GetSiteName(vehicle.FinalDestination, vehicle.Owner);
                        string vehicleInfo = $"{vehicle.Name} {actionTraveling} {siteName}";

                        //UIModuleFactionAgendaTracker ____factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);

                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Update
                            if (trackedElement.TrackedObject is GeoVehicle geoVehicle && geoVehicle == vehicle)
                            {
                                TFTVLogger.Debug($"[UIStateVehicleSelected_AddTravelSite_POSTFIX] {vehicle.Name} already tracked. Updating.");

                                trackedElement.TrackedName.text = vehicleInfo;

                                //AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_needsRefresh").SetValue(___factionTracker, true);
                                //MethodInfo ___UpdateData = typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
                                ___UpdateData.Invoke(___factionTracker, null);
                                ___OrderElements.Invoke(___factionTracker, null);

                                // Return early as the first match is always the visible one
                                return;
                            }
                        }

                        // Add
                        TFTVLogger.Debug($"[UIStateVehicleSelected_AddTravelSite_POSTFIX] {vehicle.Name} currently not tracked. Adding to tracker.");

                        //MethodInfo ___GetFreeElement = typeof(UIModuleFactionAgendaTracker).GetMethod("GetFreeElement", BindingFlags.NonPublic | BindingFlags.Instance);
                        //MethodInfo ___OnAddedElement = typeof(UIModuleFactionAgendaTracker).GetMethod("OnAddedElement", BindingFlags.NonPublic | BindingFlags.Instance);

                        UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(___factionTracker, null);
                        freeElement.Init(vehicle, vehicleInfo, vehicle.VehicleDef.ViewElement, false);
                        //freeElement.Init(vehicle, vehicleInfo, null, false);

                        ___OnAddedElement.Invoke(___factionTracker, new object[] { freeElement });
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            /*
             **
             *** Patches that REMOVE custom tracker items at various events
             ** 
            */

            // REMOVES the related item when an ancient site is excavated
            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteExcavated")]
            public static class UIStateVehicleSelected_OnVehicleSiteExcavated_Patch
            {
                public static void Postfix(GeoPhoenixFaction faction, SiteExcavationState excavation)
                {
                    try
                    {
                        //UIModuleFactionAgendaTracker ____factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);

                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Remove
                            if (trackedElement.TrackedObject is GeoSite geoSite && geoSite == excavation.Site)
                            {
                                TFTVLogger.Debug($"[UIStateVehicleSelected_OnVehicleSiteExcavated_POSTFIX] {excavation.Site.Name} is tracked. Removing.");

                                // Dispose
                                //MethodInfo ___Dispose = typeof(UIModuleFactionAgendaTracker).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
                                ___Dispose.Invoke(___factionTracker, new object[] { trackedElement });

                                // And immediately request an update (which sometimes doesn't trigger automatically because of paused state)
                                //MethodInfo ___UpdateData = typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
                                //___UpdateData.Invoke(___factionTracker, null);
                            }
                        }
                        ___UpdateData.Invoke(___factionTracker, null);



                        AddAncientSiteEncounterObjective(faction, excavation);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            // REMOVES the vehicle related item when an aircraft reached its destination
            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleArrived")]
            public static class UIStateVehicleSelected_OnVehicleArrived_Patch
            {
                public static void Postfix(GeoVehicle vehicle, bool justPassing)
                {
                    try
                    {
                        if (justPassing)
                        {
                            return;
                        }

                        //UIModuleFactionAgendaTracker ____factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);

                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Remove
                            if (trackedElement.TrackedObject is GeoVehicle geoVehicle && geoVehicle == vehicle)
                            {
                                TFTVLogger.Debug($"[UIStateVehicleSelected_OnVehicleArrived_POSTFIX] {vehicle.Name} is tracked. Removing.");

                                // Dispose
                                //MethodInfo ___Dispose = typeof(UIModuleFactionAgendaTracker).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);



                                ___Dispose.Invoke(___factionTracker, new object[] { trackedElement });

                                // And immediately request an update (which sometimes doesn't trigger automatically because of paused state)

                            }
                        }
                        ___UpdateData.Invoke(___factionTracker, null);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            // REMOVES the vehicle related item when an aircraft finished exploration of a site
            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteExplored")]
            public static class UIStateVehicleSelected_OnVehicleSiteExplored_Patch
            {
                public static void Postfix(GeoVehicle vehicle)
                {
                    try
                    {
                        //UIModuleFactionAgendaTracker ____factionTracker = (UIModuleFactionAgendaTracker)AccessTools.Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                        List<UIFactionDataTrackerElement> ____currentTrackedElements = (List<UIFactionDataTrackerElement>)AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_currentTrackedElements").GetValue(___factionTracker);

                        foreach (UIFactionDataTrackerElement trackedElement in ____currentTrackedElements)
                        {
                            // Remove
                            if (trackedElement.TrackedObject is GeoVehicle geoVehicle && geoVehicle == vehicle)
                            {
                                TFTVLogger.Debug($"[UIStateVehicleSelected_OnVehicleSiteExplored_POSTFIX] {vehicle.Name} is tracked. Removing.");

                                // Dispose
                                //MethodInfo ___Dispose = typeof(UIModuleFactionAgendaTracker).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance);
                                ___Dispose.Invoke(___factionTracker, new object[] { trackedElement });

                                // And immediately request an update (which sometimes doesn't trigger automatically because of paused state)
                                //MethodInfo ___UpdateData = typeof(UIModuleFactionAgendaTracker).GetMethod("UpdateData", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
                                //___UpdateData.Invoke(___factionTracker, null);
                            }
                        }
                        ___UpdateData.Invoke(___factionTracker, null);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            /*
             **
             *** Patches that MAINTAIN the new types of tracker items throughout their lifetime
             ** 
            */

            // Updates time left of the various tracker item types and adds mouse events.
            // NOTE that the tracker items get reused multiple times for different tracked objects and we NEED to reinitialize EVERYTHING ALWAYS
            [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "UpdateData", new Type[] { typeof(UIFactionDataTrackerElement) })]
            public static class UIModuleFactionAgendaTracker_UpdateData_Patch
            {
                public static bool Prefix(ref bool __result, UIFactionDataTrackerElement element, GeoscapeViewContext ____context)
                {
                    try
                    {
                        if (element.TrackedObject is ResearchElement)
                        {
                            // Add click event to the item that focuses camera on the tracked object
                            GameObject go = element.gameObject;
                            if (!go.GetComponent<EventTrigger>())
                            {
                                go.AddComponent<EventTrigger>();
                            }
                            EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };
                            click.callback.AddListener((eventData) =>
                            {
                                ____context.Level.Timing.Paused = true;
                                ____context.View.ToResearchState();
                            });
                            eventTrigger.triggers.Add(click);
                        }

                        else if (element.TrackedObject is ItemManufacturing.ManufactureQueueItem)
                        {
                            // Add click event to the item that focuses camera on the tracked object
                            GameObject go = element.gameObject;
                            if (!go.GetComponent<EventTrigger>())
                            {
                                go.AddComponent<EventTrigger>();
                            }
                            EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };
                            click.callback.AddListener((eventData) =>
                            {
                                ____context.Level.Timing.Paused = true;
                                ____context.View.ToManufacturingState(null, null, StateStackAction.ClearStackAndPush);
                            });
                            eventTrigger.triggers.Add(click);
                        }

                        else if (element.TrackedObject is GeoVehicle vehicle)
                        {
                            // Add click event to the item that focuses camera on the tracked object
                            GameObject go = element.gameObject;
                            if (!go.GetComponent<EventTrigger>())
                            {
                                go.AddComponent<EventTrigger>();
                            }
                            EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };
                            click.callback.AddListener((eventData) => { ____context.View.ChaseTarget(vehicle, false); });
                            eventTrigger.triggers.Add(click);

                            if (vehicle.Travelling && GetTravelTime(vehicle, out float travelTime))
                            {
                                TimeUnit arrivalTime = TimeUnit.FromHours(travelTime);
                                //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, arrivalTime: {arrivalTime}");

                                element.UpdateData(arrivalTime, true, null);
                                __result = arrivalTime <= TimeUnit.Zero;
                            }
                            else if (vehicle.IsExploringSite)
                            {
                                float explorationTimeHours = GetExplorationTime(vehicle, (float)vehicle.CurrentSite.ExplorationTime.TimeSpan.TotalHours);
                                TimeUnit explorationTimeEnd = TimeUnit.FromHours(explorationTimeHours);
                                //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, explorationTimeEnd: {explorationTimeEnd}");

                                element.UpdateData(explorationTimeEnd, true, null);
                                __result = explorationTimeEnd <= TimeUnit.Zero;
                            }

                            return false;
                        }

                        else if (element.TrackedObject is GeoPhoenixFacility facility)
                        {
                            // Add click event to the item that focuses camera on the tracked object
                            GameObject go = element.gameObject;
                            if (!go.GetComponent<EventTrigger>())
                            {
                                go.AddComponent<EventTrigger>();
                            }
                            EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };
                            click.callback.AddListener((eventData) => { ____context.View.ChaseTarget(facility.PxBase.Site, false); });
                            eventTrigger.triggers.Add(click);

                            if (facility.IsRepairing)
                            {
                                TimeUnit repairsCarriedOut = facility.GetTimeLeftToUpdate();
                                //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, repairsCarriedOut: {repairsCarriedOut}");

                                element.UpdateData(repairsCarriedOut, true, null);
                                __result = repairsCarriedOut <= TimeUnit.Zero;

                                return false;
                            }
                        }

                        else if (element.TrackedObject is GeoSite geoSite)
                        {
                            // Add click event to the item that focuses camera on the tracked object
                            GameObject go = element.gameObject;
                            if (!go.GetComponent<EventTrigger>())
                            {
                                go.AddComponent<EventTrigger>();
                            }
                            EventTrigger eventTrigger = go.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Clear();
                            EventTrigger.Entry click = new EventTrigger.Entry
                            {
                                eventID = EventTriggerType.PointerClick
                            };
                            click.callback.AddListener((eventData) => { ____context.View.ChaseTarget(geoSite, false); });
                            eventTrigger.triggers.Add(click);


                            if (geoSite.IsArcheologySite)
                            {
                                // Attack scheduled
                                if (geoSite.IsOwnedByViewer)
                                {
                                    foreach (GeoFaction geoFaction in ____context.Level.Factions)
                                    {
                                        if (geoFaction.IsViewerFaction || geoFaction.IsEnvironmentFaction || geoFaction.IsNeutralFaction || geoFaction.IsInactiveFaction)
                                        {
                                            continue;
                                        }
                                        //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] geoFaction: {geoFaction.Name.Localize()}");

                                        foreach (SiteAttackSchedule ancientSiteAttackSchedule in geoFaction.AncientSiteAttackSchedule)
                                        {
                                            if (ancientSiteAttackSchedule.HasAttackScheduled && ancientSiteAttackSchedule.Site == geoSite)
                                            {
                                                TimeUnit attackTime = TimeUnit.FromHours((float)(ancientSiteAttackSchedule.ScheduledFor - ____context.Level.Timing.Now).TimeSpan.TotalHours);
                                                //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, attackTime: {attackTime}");

                                                element.UpdateData(attackTime, true, null);
                                                __result = attackTime <= TimeUnit.Zero;
                                            }
                                        }
                                    }

                                }
                                // Excavating
                                else
                                {
                                    SiteExcavationState siteExcavationState = geoSite.GeoLevel.PhoenixFaction.ExcavatingSites.FirstOrDefault((SiteExcavationState s) => s.Site == geoSite);
                                    TimeUnit excavationTimeEnd = TimeUnit.FromHours((float)(siteExcavationState.ExcavationEndDate - ____context.Level.Timing.Now).TimeSpan.TotalHours);
                                    //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, excavationTimeEnd: {excavationTimeEnd}");

                                    element.UpdateData(excavationTimeEnd, true, null);
                                    __result = excavationTimeEnd <= TimeUnit.Zero;
                                }
                            }
                            else if (geoSite.Type == GeoSiteType.PhoenixBase)
                            {
                                if (TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Keys.Count > 0)
                                {       
                                    foreach (int phoenixBaseId in TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Keys)
                                    {
                                        if (geoSite.SiteId == phoenixBaseId)
                                        {

                                            TimeUnit timer = TimeUnit.FromSeconds((float)(3600 * TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack[phoenixBaseId].First().Value));

                                            TimeUnit attackTime = timer - ____context.Level.Timing.Now; //.TimeSpan.TotalHours)); //;
                                                                                                        //TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_UpdateData_PREFIX] element.TrackedObject: {element.TrackedObject}, attackTime: {attackTime}");
                                                                                                        // TFTVLogger.Always($"attack time is {attackTime}");
                                            element.UpdateData(attackTime, true, null);
                                          
                                            geoSite.RefreshVisuals();

                                        }
                                    }
                                }
                            }

                            return false;
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

            // A "de facto" INIT that is called on aircraft switching or (re-)entering UIStateVehicleSelected
            [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "InitialSetup")]
            public static class UIModuleFactionAgendaTracker_InitialSetup_Patch
            {

                // Store UIFactionDataTrackerElement prefab to be able to reset visuals of the items in UIFactionDataTrackerElement.Init()
                public static void Prefix(UIModuleFactionAgendaTracker __instance)
                {
                   // TFTVLogger.Always($"InitialSetup Prefix running");

                    trackerElementDefault = __instance.TrackerRowPrefab;
                }

                public static void Postfix(UIModuleFactionAgendaTracker __instance, GeoFaction ____faction, GeoscapeViewContext ____context)
                {
                    try
                    {
                        if (____faction is GeoPhoenixFaction geoPhoenixFaction)
                        {
                           // TFTVLogger.Always($"InitialSetup Postfix running post if");
                            //MethodInfo ___GetFreeElement = typeof(UIModuleFactionAgendaTracker).GetMethod("GetFreeElement", BindingFlags.NonPublic | BindingFlags.Instance);
                            //MethodInfo ___OnAddedElement = typeof(UIModuleFactionAgendaTracker).GetMethod("OnAddedElement", BindingFlags.NonPublic | BindingFlags.Instance);

                            // Vehicles

                            foreach (GeoVehicle vehicle in geoPhoenixFaction.Vehicles.Where(v => v.Travelling || v.IsExploringSite))
                            {
                                TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_InitialSetup_POSTFIX] Add/Reapply tracker element for {vehicle.Name}.");

                                UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(__instance, null);

                                string siteName = "ERR";
                                string vehicleInfo = "ERR";
                                if (vehicle.Travelling)
                                {
                                    siteName = GetSiteName(vehicle.FinalDestination, vehicle.Owner);
                                    vehicleInfo = $"{vehicle.Name} {actionTraveling} {siteName}";
                                }
                                else if (vehicle.IsExploringSite)
                                {
                                    siteName = GetSiteName(vehicle.CurrentSite, vehicle.Owner);
                                    vehicleInfo = $"{vehicle.Name} {actionExploring} {siteName}";
                                }

                                freeElement.Init(vehicle, vehicleInfo, vehicle.VehicleDef.ViewElement, false);
                                //freeElement.Init(vehicle, vehicleInfo, null, false);

                                ___OnAddedElement.Invoke(__instance, new object[] { freeElement });
                            }


                            // Facilities under repair
                            IEnumerable<GeoPhoenixFacility> facilitiesUnderRepair = geoPhoenixFaction.Bases.SelectMany((GeoPhoenixBase t) => from f in t.Layout.Facilities where f.IsRepairing && f.GetTimeLeftToUpdate() != TimeUnit.Zero select f);
                            foreach (GeoPhoenixFacility facility in facilitiesUnderRepair)
                            {
                                UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(__instance, null);

                                string facilityName = facility.Def.ViewElementDef.DisplayName1.Localize();
                                string facilityInfo = $"{actionRepairing} {facilityName}";
                                freeElement.Init(facility, facilityInfo, facility.Def.ViewElementDef, false);

                                ___OnAddedElement.Invoke(__instance, new object[] { freeElement });
                            }

                            // Excavations in progress

                            IEnumerable<SiteExcavationState> excavatingSites = geoPhoenixFaction.ExcavatingSites.Where(s => !s.IsExcavated);
                            foreach (SiteExcavationState excavatingSite in excavatingSites)
                            {
                                UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(__instance, null);

                                string siteName = excavatingSite.Site.Name;
                                string excavationInfo = $"{actionExcavating} {siteName}";

                                // Without a viewdef there are... problems. Therefore we borrow one with the correct icon
                                ViewElementDef borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("0481b9e2-947c-fbb2-3d96-8f769e1e05cd");
                                freeElement.Init(excavatingSite.Site, excavationInfo, borrowedViewElementDef, false);

                                ___OnAddedElement.Invoke(__instance, new object[] { freeElement });
                            }


                            // Base defense incoming
                            foreach (GeoFaction geoFaction in ____context.Level.Factions)
                            {
                                if (geoFaction.IsViewerFaction || geoFaction.IsEnvironmentFaction || geoFaction.IsNeutralFaction || geoFaction.IsInactiveFaction)
                                {
                                    continue;
                                }


                                if (TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Keys.Count > 0)
                                {

                                    bool isFactionPresent = false;
                                    foreach (Dictionary<string, double> factionTimeUnits in TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Values)
                                    {
                                        if (factionTimeUnits.ContainsKey(geoFaction.GetPPName()))
                                        {
                                            isFactionPresent = true;
                                            break;
                                        }
                                    }

                                    if (isFactionPresent)
                                    {
                                        GeoLevelController controller = ____faction.GeoLevel;

                                        foreach (int geoSiteId in TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Keys)
                                        {
                                            GeoSite phoenixBase = TFTVBaseDefenseGeoscape.InitAttack.FindPhoenixBase(geoSiteId, controller);

                                            if (phoenixBase.HasActiveMission)
                                            {
                                                UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(__instance, null);

                                                string siteName = phoenixBase.Name;
                                                string attackInfo = $"{geoFaction.Name.Localize().ToUpperInvariant()} {actionAttackOnPX} {siteName}";
                                                //string attackInfo = string.Format(actionAttack, siteName, geoFaction.Name.Localize(null));

                                                // Without a viewdef there are... problems. Therefore we borrow one with the correct icon
                                                ViewElementDef borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("8188f3a3-befd-e463-f345-4af1815cd848"); // DefRepositoryDef.AllDefs.OfType<ViewElementDef>().Where(def => def.name.Contains("Crabman_ActorViewDef")).FirstOrDefault();
                                                freeElement.Init(phoenixBase, attackInfo, borrowedViewElementDef, false);

                                                ___OnAddedElement.Invoke(__instance, new object[] { freeElement });
                                            }

                                        }
                                    }
                                }


                                foreach (SiteAttackSchedule ancientSiteAttackSchedule in geoFaction.AncientSiteAttackSchedule)
                                {
                                    if (ancientSiteAttackSchedule.HasAttackScheduled && ancientSiteAttackSchedule.Site.Owner == ____context.ViewerFaction)
                                    {
                                        TFTVLogger.Debug($"[UIModuleFactionAgendaTracker_InitialSetup_POSTFIX] Add/Reapply tracker element for {ancientSiteAttackSchedule.Site.Name}.");

                                        UIFactionDataTrackerElement freeElement = (UIFactionDataTrackerElement)___GetFreeElement.Invoke(__instance, null);

                                        string siteName = ancientSiteAttackSchedule.Site.Name;
                                        string attackInfo = $"{geoFaction.Name.Localize().ToUpperInvariant()} {actionAttack} {siteName}";
                                        //string attackInfo = string.Format(actionAttack, siteName, geoFaction.Name.Localize(null));

                                        // Without a viewdef there are... problems. Therefore we borrow one with the correct icon
                                        ViewElementDef borrowedViewElementDef = (ViewElementDef)GameUtl.GameComponent<DefRepository>().GetDef("0481b9e2-947c-fbb2-3d96-8f769e1e05cd");//.DefRepositoryDef.AllDefs.OfType<ViewElementDef>().Where(def => def.name.Contains("ArcheologyLab_PhoenixFacilityDef")).FirstOrDefault();
                                        freeElement.Init(ancientSiteAttackSchedule.Site, attackInfo, borrowedViewElementDef, false);

                                        ___OnAddedElement.Invoke(__instance, new object[] { freeElement });
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            // Visual adjustments of the tracker items
            // NOTE that for custom items (vehicles and excavations so far) the text gets overridden from passed parameter too
            [HarmonyPatch(typeof(UIFactionDataTrackerElement), "Init")]
            public static class UIFactionDataTrackerElement_Init_Patch
            {

                public static void Postfix(UIFactionDataTrackerElement __instance, string text)
                {
                    try
                    {
                        //TFTVLogger.Debug($"[UIFactionDataTrackerElement_Init_POSTFIX] objToTrack: {objToTrack}, text: {text}");

                        __instance.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                        __instance.TrackedTime.alignment = TextAnchor.MiddleRight;

                        __instance.TrackedTime.fontSize = 36;
                        __instance.TrackedName.fontSize = 36;

                        // As the elements are re-used and NOT recreated they have to be reset to their default visuals first.
                        // Otherwise, if no condition below is true they retain the colors of their old item, resulting in wrongly colored elements!
                        if (trackerElementDefault != null)
                        {
                            __instance.TrackedName.color = trackerElementDefault.TrackedName.color;
                            __instance.TrackedTime.color = trackerElementDefault.TrackedTime.color;
                            __instance.Icon.color = trackerElementDefault.Icon.color;
                        }
                        else
                        {
                            __instance.TrackedName.color = defaultTrackerColor;
                            __instance.TrackedTime.color = defaultTrackerColor;
                            __instance.Icon.color = defaultTrackerColor;
                        }

                        if (__instance.TrackedObject is GeoVehicle vehicle)
                        {
                            __instance.TrackedName.text = text; // Always use passed text for non-default elements as def disturbs it
                            __instance.TrackedName.color = vehicleTrackerColor;
                            __instance.TrackedTime.color = vehicleTrackerColor;
                            __instance.Icon.color = vehicleTrackerColor;

                            // Borrowed from UIModuleInfoBar, fetched at UIModuleInfoBar.Init()
                            __instance.Icon.sprite = aircraftSprite;
                        }
                        else if (__instance.TrackedObject is ResearchElement re)
                        {
                            __instance.TrackedName.color = researchTrackerColor;
                            __instance.TrackedTime.color = researchTrackerColor;
                            __instance.Icon.color = researchTrackerColor;
                        }
                        else if (__instance.TrackedObject is ItemManufacturing.ManufactureQueueItem mqi)
                        {
                            __instance.TrackedName.color = manufactureTrackerColor;
                            __instance.TrackedTime.color = manufactureTrackerColor;
                            __instance.Icon.color = manufactureTrackerColor;
                        }
                        else if (__instance.TrackedObject is GeoPhoenixFacility gpf)
                        {
                            if (gpf.IsRepairing)
                            {
                                __instance.TrackedName.text = text; // Always use passed text for non-default elements as def disturbs it
                            }

                            //__instance.TrackedName.color = facilityTrackerColor;
                            //__instance.TrackedTime.color = facilityTrackerColor;
                            //__instance.Icon.color = facilityTrackerColor;
                        }
                        else if (__instance.TrackedObject is GeoSite gs)
                        {
                            if (gs.IsArcheologySite)
                            {
                                // Attack scheduled
                                if (gs.IsOwnedByViewer)
                                {
                                    __instance.TrackedName.text = text; // Always use passed text for non-default elements as def disturbs it
                                    __instance.TrackedName.color = baseAttackTrackerColor;
                                    __instance.TrackedTime.color = baseAttackTrackerColor;
                                    __instance.Icon.color = baseAttackTrackerColor;
                                }
                                // Excavating
                                else
                                {
                                    __instance.TrackedName.text = text; // Always use passed text for non-default elements as def disturbs it
                                    __instance.TrackedName.color = excavationTrackerColor;
                                    __instance.TrackedTime.color = excavationTrackerColor;
                                    __instance.Icon.color = excavationTrackerColor;
                                }
                            }
                            else if (gs.Type == GeoSiteType.PhoenixBase)
                            {
                                __instance.gameObject.SetActive(false);

                                __instance.TrackedName.text = text; // Always use passed text for non-default elements as def disturbs it
                                __instance.TrackedName.color = baseAttackTrackerColor;
                                __instance.TrackedTime.color = baseAttackTrackerColor;
                                __instance.Icon.color = baseAttackTrackerColor;

                                // Borrowed from UIModuleInfoBar, fetched at UIModuleInfoBar.Init()
                                __instance.Icon.sprite = phoenixFactionSprite;
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

