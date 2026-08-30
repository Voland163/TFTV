using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
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
using PhoenixPoint.Geoscape.View.ViewServices;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVBaseRework;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.AgendaTracker
{
    internal static class AgendaPatches
    {


        #region Minor patches

        [HarmonyPatch(typeof(UIStateVehicleSelected), "EnterState")]
        public static class UIStateVehicleSelected_EnterState_Patch
        {
            public static void Postfix(UIStateVehicleSelected __instance)
            {
                try
                {
                    if (AgendaConstants.factionTracker == null)
                    {
                        AgendaConstants.factionTracker = (UIModuleFactionAgendaTracker)AccessTools
                            .Property(typeof(UIStateVehicleSelected), "_factionTracker").GetValue(__instance, null);
                    }

                    if (!AgendaConstants.fetchedSiteNames)
                    {
                        var infoBox = (UIModuleSelectionInfoBox)AccessTools
                            .Property(typeof(UIStateVehicleSelected), "_selectionInfoBoxModule").GetValue(__instance, null);

                        AgendaConstants.unexploredSiteName = infoBox.UnexploredSiteTextKey.Localize();
                        AgendaConstants.explorationSiteName = infoBox.ExplorationSiteTextKey.Localize();
                        AgendaConstants.scavengingSiteName = infoBox.ScavengingSiteTextKey.Localize();
                        AgendaConstants.ancientSiteName = infoBox.AncientSiteTextKey.Localize();
                        AgendaConstants.fetchedSiteNames = true;
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

      


       [HarmonyPatch(typeof(UIModuleStatusBarMessages), "Start")]
        public static class UIModuleStatusBarMessages_Start_Patch
        {
            public static void Postfix(UIModuleStatusBarMessages __instance)
            {
                try { 
                    TFTVLogger.Always("Disabling status bar messages");
                    __instance.TimedEventRoot.gameObject.SetActive(false); }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIFactionDataTrackerElement), "SetTime")]
        public static class UIFactionDataTrackerElement_SetTime_Patch
        {
            public static void Postfix(UIFactionDataTrackerElement __instance)
            {
                try { __instance.TrackedTime.text = $"~ {__instance.TrackedTime.text}"; }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

    

        #endregion

        #region Add/Update tracker items

        [HarmonyPatch(typeof(GeoscapeLog), "PhoenixFaction_OnExcavationStarted")]
        public static class GeoscapeLog_OnExcavationStarted_Patch
        {
            public static void Postfix(GeoFaction faction, SiteExcavationState excavation)
            {
                try
                {
                    if (AgendaConstants.factionTracker == null) return;

                    GeoSite geoSite = excavation.Site;
                    string siteInfo = $"{AgendaConstants.actionExcavating} {geoSite.LocalizedSiteName}";

                    var existing = AgendaHelpers.FindTrackedElement(geoSite);
                    if (existing != null)
                    {
                        existing.TrackedName.text = siteInfo;
                        AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                        return;
                    }

                    AgendaHelpers.AddTrackerElement(geoSite, siteInfo, AgendaHelpers.GetGenericSiteViewElement());
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(GeoscapeLog), "ShowSiteDefenseTimer")]
        public static class GeoscapeLog_ShowSiteDefenseTimer_Patch
        {
            public static void Postfix(GeoFaction faction, SiteAttackSchedule target)
            {
                try
                {
                    if (AgendaConstants.factionTracker == null) return;

                    GeoSite geoSite = target.Site;
                    string siteInfo = $"{faction.Name.Localize(null).ToUpperInvariant()} {AgendaConstants.actionAttack} {geoSite.LocalizedSiteName}";

                    var existing = AgendaHelpers.FindTrackedElement(geoSite);
                    if (existing != null)
                    {
                        existing.TrackedName.text = siteInfo;
                        AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                        return;
                    }

                    var viewElement = AgendaHelpers.GetAttackViewElement(geoSite);
                    AgendaHelpers.AddTrackerElement(geoSite, siteInfo, viewElement);
                    geoSite.ExpiringTimerAt = target.ScheduledFor;
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

  

        #endregion

        #region Remove tracker items

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteExcavated")]
        public static class UIStateVehicleSelected_OnVehicleSiteExcavated_Patch
        {
            public static void Postfix(GeoPhoenixFaction faction, SiteExcavationState excavation)
            {
                try
                {
                    RemoveAllMatchingElements<GeoSite>(s => s == excavation.Site);
                    AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);

                    // Add encounter objective if mission is active
                    if (excavation.Site.ActiveMission != null)
                    {
                        var objective = new MissionGeoFactionObjective(excavation.Site.Owner, excavation.Site.ActiveMission)
                        {
                            Title = excavation.Site.ActiveMission.MissionName,
                            Description = excavation.Site.ActiveMission.MissionDescription
                        };
                        faction.AddObjective(objective);
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        

        private static void RemoveAllMatchingElements<T>(Func<T, bool> predicate) where T : class
        {
            var elements = AgendaConstants.GetLiveTrackedElements();
            if (elements == null) return;

            foreach (var el in elements)
            {
                if (el.TrackedObject is T obj && predicate(obj))
                    AgendaHelpers.RemoveTrackerElement(el);
            }
        }

        #endregion

        #region UpdateData (per-element)

        [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "UpdateData", new Type[] { typeof(UIFactionDataTrackerElement) })]
        public static class UIModuleFactionAgendaTracker_UpdateData_Patch
        {
            public static bool Prefix(ref bool __result, UIFactionDataTrackerElement element, GeoscapeViewContext ____context)
            {
                try
                {
                    if (element.TrackedObject is ResearchElement)
                    {
                        AgendaHelpers.WireClickEvent(element, () =>
                        {
                            ____context.Level.Timing.Paused = true;
                            ____context.View.ToResearchState();
                        });

                        return true;
                    }
                    else if (element.TrackedObject is ItemManufacturing.ManufactureQueueItem)
                    {
                        AgendaHelpers.WireClickEvent(element, () =>
                        {
                            ____context.Level.Timing.Paused = true;
                            ____context.View.ToManufacturingState(null, null, StateStackAction.ClearStackAndPush);
                        });

                        return true;
                    }
                    else if (element.TrackedObject is GeoVehicle vehicle)
                    {
                        AircraftReworkSpeedAndRange.AdjustAircraftSpeed(vehicle);
                        AgendaHelpers.WireClickEvent(element, () => ____context.View.ChaseTarget(vehicle, false));
                        return true;
                    }
                    else if (element.TrackedObject is GeoCharacter character)
                    {
                        var session = TrainingFacilityRework.GetRecruitSession(character);
                        if (session != null)
                        {
                            AgendaHelpers.WireClickEvent(element, () =>
                            {
                                
                                    ____context.View.ToPhoenixRecruitsState();
                                
                            });

                            AgendaHelpers.ApplyRecruitTrainingTrackerText(element, character);
                            TimeUnit remaining = AgendaHelpers.GetRecruitTrainingRemainingTime(character, ____context.Level);
                            element.UpdateData(remaining, true, null);
                            __result = session.Completed || remaining <= TimeUnit.Zero;
                            return false;
                        }
                    }
                    else if (element.TrackedObject is GeoPhoenixFacility facility)
                    {
                        AgendaHelpers.WireClickEvent(element, () => ____context.View.ChaseTarget(facility.PxBase.Site, false));

                        if (facility.IsRepairing)
                        {
                            // Vanilla uses (1 - ConstructionPercentage) * ConstructionTime, which always
                            // multiplies repair progress against the full build time — wrong for partial damage.
                            // GetTimeLeftToUpdate() returns NextUpdate - Now, i.e. actual remaining repair time.
                            TimeUnit timeLeft = facility.GetTimeLeftToUpdate();
                            element.UpdateData(timeLeft, true, null);
                            __result = timeLeft <= TimeUnit.Zero;
                            return false;
                        }

                        // UnderConstruction: let vanilla handle it — its formula is correct for building.
                        return true;
                    }
                    else if (element.TrackedObject is GeoSite geoSite)
                    {
                        AgendaHelpers.WireClickEvent(element, () => ____context.View.ChaseTarget(geoSite, false));
                        AgendaHelpers.ApplyCustomSiteTrackerText(element, geoSite, ____context?.ViewerFaction);

                        if (AgendaHelpers.TryResolveSiteTimer(geoSite, ____context, out TimeUnit remaining))
                        {
                            element.UpdateData(remaining, true, null);
                        }

                        if (geoSite.Type == GeoSiteType.PhoenixBase
                            && TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(geoSite.SiteId))
                        {
                            geoSite.RefreshVisuals();
                        }

                        // A site row lives for as long as the site has something to report, not until
                        // its countdown hits zero: the incident that owns the countdown is completed by
                        // the event system's own tick a moment later, and disposing on zero first would
                        // drop the row only for the next sweep to add it straight back.
                        __result = !AgendaHelpers.HasAnySiteTrackerReason(geoSite, ____context);
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

            // Vanilla OnFacilityStateChanged adds a tracker element for UnderConstruction but has no
            // case for Repairing, so repair started mid-session never appears in the tracker until save/load.
            [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "OnFacilityStateChanged")]
            public static class UIModuleFactionAgendaTracker_OnFacilityStateChanged_Patch
            {
                public static void Postfix(GeoPhoenixFacility facility, GeoPhoenixFacility.FacilityState prevState)
                {
                    try
                    {
                        if (AgendaConstants.factionTracker == null) return;
                        if (facility.State != GeoPhoenixFacility.FacilityState.Repairing) return;

                        // Only add if there is no existing element for this facility.
                        var existing = AgendaConstants.GetTrackedElements()
                            ?.FirstOrDefault(e => e.TrackedObject == facility);
                        if (existing != null) return;

                        string name = facility.Def.ViewElementDef.DisplayName1.Localize();
                        AgendaHelpers.AddTrackerElement(facility, $"{AgendaConstants.actionRepairing} {name}", facility.Def.ViewElementDef);
                    }
                    catch (Exception e) { TFTVLogger.Error(e); }
                }
            }
        }

        #endregion

        #region InitialSetup

        [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "InitialSetup")]
        public static class UIModuleFactionAgendaTracker_InitialSetup_Patch
        {
            public static void Prefix(UIModuleFactionAgendaTracker __instance)
            {
                AgendaConstants.factionTracker = __instance;
                AgendaConstants.trackerElementDefault = __instance.TrackerRowPrefab;
            }

            public static void Postfix(UIModuleFactionAgendaTracker __instance, GeoFaction ____faction, GeoscapeViewContext ____context)
            {
                try
                {
                    EnsureCustomTrackers(__instance, ____faction, ____context);
                    AgendaHelpers.ReapplyResolvedTrackerTexts(__instance, ____context.ViewerFaction);
                    AgendaRefresh.TryApplyPendingRefreshAfterBaseReworkRestore();
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }

            /// <summary>
            /// Adds a row for every incident, pending base activation, excavation, scheduled attack and
            /// recruit training session that does not already have one.
            ///
            /// Vanilla only ever builds the tracker from InitialSetup(), which runs when the player
            /// changes geoscape selection, so a row lost between two of those - to a recycled pooled
            /// element, a stray Dispose, or a throw part-way through the rebuild - stayed lost until the
            /// player happened to click something. Every group here is keyed off the tracked object and
            /// skips objects that already have a live row, so the same call is also made once a second
            /// from UpdateData() and any gap closes on its own within a tick.
            /// </summary>
            internal static void EnsureCustomTrackers(UIModuleFactionAgendaTracker tracker, GeoFaction faction, GeoscapeViewContext context)
            {
                if (tracker == null || context == null || context.Level == null) return;
                if (!(faction is GeoPhoenixFaction phoenix)) return;

                List<UIFactionDataTrackerElement> live = AgendaConstants.GetLiveTrackedElements(tracker);
                if (live == null) return;

                List<object> tracked = new List<object>(live.Count);
                foreach (UIFactionDataTrackerElement element in live)
                {
                    if (element.TrackedObject != null)
                    {
                        tracked.Add(element.TrackedObject);
                    }
                }

                bool IsTracked(object trackedObject)
                {
                    foreach (object candidate in tracked)
                    {
                        if (ReferenceEquals(candidate, trackedObject)) return true;
                    }
                    return false;
                }

                UIFactionDataTrackerElement Add(object trackedObject, string text, ViewElementDef viewElement)
                {
                    if (trackedObject == null || IsTracked(trackedObject)) return null;

                    UIFactionDataTrackerElement added = AgendaHelpers.AddTrackerElement(trackedObject, text, viewElement);
                    if (added != null)
                    {
                        tracked.Add(trackedObject);
                    }
                    return added;
                }

                // Each group is guarded on its own: one bad entry must not cost the tracker every row
                // that would have been added after it.
                try
                {
                    foreach (var session in TrainingFacilityRework.GetActiveRecruitSessions())
                    {
                        if (session?.Character == null || IsTracked(session.Character)) continue;

                        // Training that has run out its clock but has not been marked complete yet is
                        // disposed by UpdateData as finished; adding it back here would have the row
                        // blink in and out once a second until the session closes.
                        if (AgendaHelpers.GetRecruitTrainingRemainingTime(session.Character, context.Level) <= TimeUnit.Zero) continue;

                        UIFactionDataTrackerElement added = Add(
                            session.Character,
                            AgendaHelpers.GetRecruitTrainingTrackerText(session.Character),
                            AgendaHelpers.GetTrainingViewElement());

                        AgendaHelpers.ApplyRecruitTrainingTrackerText(added, session.Character);
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }

                try
                {
                    foreach (SiteExcavationState excavation in phoenix.ExcavatingSites)
                    {
                        if (excavation == null || excavation.IsExcavated || excavation.Site == null) continue;
                        if (IsTracked(excavation.Site)) continue;

                        Add(
                            excavation.Site,
                            $"{AgendaConstants.actionExcavating} {excavation.Site.Name}",
                            AgendaHelpers.GetGenericSiteViewElement());
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }

                try
                {
                    foreach (GeoSite site in context.Level.Map.AllSites)
                    {
                        if (site == null || IsTracked(site) || !AgendaHelpers.HasCustomSiteTracker(site)) continue;

                        UIFactionDataTrackerElement added = Add(
                            site,
                            AgendaHelpers.GetCustomSiteTrackerText(site, context.ViewerFaction),
                            AgendaHelpers.GetCustomSiteViewElement(site));

                        AgendaHelpers.ApplyCustomSiteTrackerText(added, site, context.ViewerFaction);
                    }
                }
                catch (Exception e) { TFTVLogger.Error(e); }

                try
                {
                    AddAttackTrackers(faction, context, IsTracked, Add);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }

            private static void AddAttackTrackers(
                GeoFaction faction,
                GeoscapeViewContext context,
                Func<object, bool> isTracked,
                Func<object, string, ViewElementDef, UIFactionDataTrackerElement> add)
            {
                foreach (GeoFaction geoFaction in context.Level.Factions)
                {
                    if (geoFaction.IsViewerFaction || geoFaction.IsEnvironmentFaction || geoFaction.IsNeutralFaction || geoFaction.IsInactiveFaction)
                        continue;

                    // Phoenix base attacks
                    if (TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Count > 0)
                    {
                        bool isFactionPresent = TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Values
                            .Any(dict => dict.ContainsKey(geoFaction.GetPPName()));

                        if (isFactionPresent)
                        {
                            var controller = faction.GeoLevel;
                            foreach (int siteId in TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.Keys)
                            {
                                GeoSite pxBase = TFTVBaseDefenseGeoscape.InitAttack.FindPhoenixBase(siteId, controller);
                                if (pxBase != null && pxBase.HasActiveMission && !isTracked(pxBase))
                                {
                                    string info = $"{geoFaction.Name.Localize().ToUpperInvariant()} {AgendaConstants.actionAttackOnPX} {pxBase.Name}";
                                    add(pxBase, info, AgendaHelpers.GetCrabmanViewElement());
                                }
                            }
                        }
                    }

                    // Ancient site attacks
                    foreach (var schedule in geoFaction.AncientSiteAttackSchedule)
                    {
                        if (schedule.HasAttackScheduled && schedule.Site != null && schedule.Site.Owner == context.ViewerFaction && !isTracked(schedule.Site))
                        {
                            string info = $"{geoFaction.Name.Localize().ToUpperInvariant()} {AgendaConstants.actionAttack} {schedule.Site.Name}";
                            add(schedule.Site, info, AgendaHelpers.GetGenericSiteViewElement());
                        }
                    }
                }
            }
        }

        #endregion

        #region Init visual adjustments

        [HarmonyPatch(typeof(UIFactionDataTrackerElement), nameof(UIFactionDataTrackerElement.Init))]
        public static class UIFactionDataTrackerElement_Init_Patch
        {
            public static void Postfix(UIFactionDataTrackerElement __instance, string text)
            {
                try
                {
                    // Init() is the single choke point every add/reuse path goes through -
                    // AgendaHelpers.AddTrackerElement, and vanilla's own OnResearchStarted /
                    // OnItemStartedManufacturing / OnVehicleAction / OnFacilityBuilding (called
                    // both from InitialSetup() and from EnsureLiveVanillaTrackers). Only the
                    // first of those clears PendingPurge on reuse; the vanilla methods don't.
                    // If GetFreeElement() hands back an element that's still flagged pending-purge
                    // from its previous row and one of the vanilla paths reuses it without going
                    // through AddTrackerElement, EnsureLiveVanillaTrackers' "already tracked?"
                    // check (which excludes PendingPurge) would keep reporting the new object as
                    // untracked and add a second, genuinely duplicate row for it every tick until
                    // the duplicate happens to land on a non-flagged element. Clearing it here,
                    // unconditionally, on every Init() closes that gap for every path at once.
                    AgendaConstants.PendingPurge.Remove(__instance);

                    __instance.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    __instance.TrackedTime.alignment = TextAnchor.MiddleRight;
                    __instance.TrackedTime.fontSize = 36;
                    __instance.TrackedName.fontSize = 36;

                    ResetColors(__instance);
                    ApplyTypeSpecificVisuals(__instance, text);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }

            private static void ResetColors(UIFactionDataTrackerElement el)
            {
                Color c = AgendaConstants.trackerElementDefault != null
                    ? AgendaConstants.trackerElementDefault.TrackedName.color
                    : AgendaConstants.defaultTrackerColor;

                Color tc = AgendaConstants.trackerElementDefault != null
                    ? AgendaConstants.trackerElementDefault.TrackedTime.color
                    : AgendaConstants.defaultTrackerColor;

                Color ic = AgendaConstants.trackerElementDefault != null
                    ? AgendaConstants.trackerElementDefault.Icon.color
                    : AgendaConstants.defaultTrackerColor;

                el.TrackedName.color = c;
                el.TrackedTime.color = tc;
                el.Icon.color = ic;
            }

            private static void SetColors(UIFactionDataTrackerElement el, Color color)
            {
                el.TrackedName.color = color;
                el.TrackedTime.color = color;
                el.Icon.color = color;
            }

            private static void DisableTrackedNameLocalization(UIFactionDataTrackerElement el)
            {
                var localize = el.TrackedName != null ? el.TrackedName.GetComponent<I2.Loc.Localize>() : null;
                if (localize != null)
                {
                    localize.enabled = false;
                }
            }

            private static void ApplyTypeSpecificVisuals(UIFactionDataTrackerElement el, string text)
            {
                if (el.TrackedObject is GeoVehicle)
                {
                    el.TrackedName.text = text;
                    SetColors(el, AgendaConstants.vehicleTrackerColor);
                    el.Icon.sprite = AgendaConstants.aircraftSprite;
                }
                else if (el.TrackedObject is ResearchElement)
                {
                    SetColors(el, AgendaConstants.researchTrackerColor);
                }
                else if (el.TrackedObject is ItemManufacturing.ManufactureQueueItem)
                {
                    SetColors(el, AgendaConstants.manufactureTrackerColor);
                }
                else if (el.TrackedObject is GeoPhoenixFacility gpf)
                {
                    if (gpf.IsRepairing) el.TrackedName.text = text;
                }
                else if (el.TrackedObject is GeoCharacter character && TrainingFacilityRework.GetRecruitSession(character) != null)
                {
                    AgendaHelpers.ApplyRecruitTrainingTrackerText(el, character);
                    SetColors(el, AgendaConstants.trainingTrackerColor);
                    var viewEl = AgendaHelpers.GetTrainingViewElement();
                    if (viewEl != null) el.Icon.sprite = viewEl.SmallIcon;
                }
                else if (el.TrackedObject is GeoSite gs)
                {
                    ApplySiteVisuals(el, gs, text);
                }
            }

            private static void ApplySiteVisuals(UIFactionDataTrackerElement el, GeoSite gs, string text)
            {
                DisableTrackedNameLocalization(el);

                string resolved = AgendaHelpers.GetCustomSiteTrackerText(gs, gs.GeoLevel?.ViewerFaction ?? gs.Owner) ?? text;

                if (AgendaHelpers.GetPendingBaseAction(gs).HasValue)
                {
                    el.TrackedName.text = resolved;
                    SetColors(el, AgendaConstants.baseActivationTrackerColor);
                    el.Icon.sprite = AgendaConstants.phoenixFactionSprite;
                    return;
                }

                if (AgendaHelpers.GetActiveIncident(gs) != null)
                {
                    el.TrackedName.text = resolved;
                    SetColors(el, AgendaConstants.incidentTrackerColor);
                    el.Icon.sprite = AgendaConstants.ancientSiteProbeSprite;
                    return;
                }

                if (gs.IsArcheologySite)
                {
                    el.TrackedName.text = text;
                    Color c = gs.IsOwnedByViewer ? AgendaConstants.baseAttackTrackerColor : AgendaConstants.excavationTrackerColor;
                    SetColors(el, c);
                }
                else if (gs.Type == GeoSiteType.PhoenixBase)
                {
                    el.gameObject.SetActive(false);
                    el.TrackedName.text = text;
                    SetColors(el, AgendaConstants.baseAttackTrackerColor);
                    el.Icon.sprite = AgendaConstants.phoenixFactionSprite;
                }
            }
        }

        #endregion

        #region Site visuals recolor

        internal static void RecolorTimerBaseAndAncientSiteAttacks(GeoSiteVisualsController controller, GeoSite site)
        {
            try
            {
                bool isRelevant = (site.Type == GeoSiteType.PhoenixBase && site.IsActiveSite)
                    || (site.IsArcheologySite && site.Owner is GeoPhoenixFaction);
                if (!isRelevant) return;

                bool isScheduled = false;
                foreach (GeoFaction f in site.GeoLevel.Factions)
                {
                    if (f.IsViewerFaction || f.IsEnvironmentFaction || f.IsNeutralFaction || f.IsInactiveFaction) continue;

                    if (f.PhoenixBaseAttackSchedule.Any(s => s.HasAttackScheduled && s.Site == site)
                        || f.AncientSiteAttackSchedule.Any(s => s.HasAttackScheduled && s.Site == site))
                    {
                        isScheduled = true;
                        break;
                    }
                }

                if (!isScheduled) return;

                if (site.Type != GeoSiteType.PhoenixBase)
                {
                    foreach (Renderer r in controller.TimerController.gameObject.GetComponentsInChildren<Renderer>())
                    {
                        r.gameObject.SetActive(true);
                        if (r.name == "TimedIcon")
                            r.material.color = AgendaConstants.baseAttackTrackerColor;
                    }
                }
                else
                {
                    controller.TimerController.gameObject.SetChildrenVisibility(false);
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        #endregion

        #region Stability (prevent vanilla full-rebuild flicker + stale re-tracked rows) & ordering

        // Vanilla's Dispose() puts an element straight back into the pool while still leaving it in
        // _currentTrackedElements and queued in _elementsToRemove; the two are only reconciled at the
        // top of the next parameterless UpdateData(). GetFreeElement() can therefore hand out an
        // element the tracker still believes it has to delete, and that pending delete then lands on
        // the row just built from it - the row vanishes a second or so after appearing. An element
        // disposed twice is worse: it sits in the pool twice, so a later row is handed a GameObject a
        // live row is already drawing with, and one of the two rows is lost.
        //
        // Every add path - ours and vanilla's OnResearchStarted / OnItemStartedManufacturing /
        // OnVehicleAction / OnFacilityBuilding alike - goes through GetFreeElement(), so reconciling
        // the recycled element here means an element handed out is always a clean slate. Safe to
        // mutate the tracked list from here: no caller reaches GetFreeElement() while that list is
        // being enumerated (InitialSetup's teardown loop finishes before it re-adds, and UpdateData()'s
        // per-element loop never adds).
        [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "GetFreeElement")]
        public static class UIModuleFactionAgendaTracker_GetFreeElement_Patch
        {
            public static void Postfix(UIModuleFactionAgendaTracker __instance, UIFactionDataTrackerElement __result)
            {
                try
                {
                    if (__result == null) return;

                    var pendingRemoval = AgendaConstants.GetElementsQueuedForRemoval(__instance);
                    pendingRemoval?.RemoveAll(e => ReferenceEquals(e, __result));

                    var tracked = AgendaConstants.GetTrackedElements(__instance);
                    tracked?.RemoveAll(e => ReferenceEquals(e, __result));

                    AgendaConstants.PendingPurge.Remove(__result);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(
     typeof(UIModuleFactionAgendaTracker),
     "Dispose",
     new Type[] { typeof(UIFactionDataTrackerElement) })]
        public static class UIModuleFactionAgendaTracker_Dispose_Patch
        {
            private static readonly FieldInfo NeedsRefreshField =
                AccessTools.Field(typeof(UIModuleFactionAgendaTracker), "_needsRefresh");

            public static void Postfix(
                UIModuleFactionAgendaTracker __instance,
                UIFactionDataTrackerElement element)
            {
                try
                {
                    if (element != null)
                    {
                        // Logical removal for this mod's find/reapply/order helpers.
                        // Physical list removal is still performed by vanilla's next
                        // parameterless UpdateData() call.
                        AgendaConstants.PendingPurge.Add(element);
                    }

                    // Prevent stock's global InitialSetup rebuild/flicker.
                    NeedsRefreshField?.SetValue(__instance, false);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

       /* // Vanilla only calls OrderElements() from OnAddedElement() - i.e. once when a row is
        // created - never on the recurring per-second tick. So as remaining time changes, rows
        // never get re-sorted relative to each other until something new is added. It also sorts
        // descending (longest remaining first); we want the opposite (lowest ETA at top), and we
        // must exclude PendingPurge rows so stale, already-disposed rows still physically present
        // in _currentTrackedElements don't get mixed into the ordering.
        [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "OrderElements")]
        public static class UIModuleFactionAgendaTracker_OrderElements_Patch
        {
            public static bool Prefix(UIModuleFactionAgendaTracker __instance)
            {
                try
                {
                    AgendaConstants.factionTracker = __instance;

                    var elements = AgendaConstants.GetLiveTrackedElements(__instance);
                    if (elements == null) return false;

                    var ordered = elements.OrderBy(t => t.CurrentTimeLeft).ToList();
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        ordered[i].transform.SetSiblingIndex(i);
                    }
                    return false;
                }
                catch (Exception e) { TFTVLogger.Error(e); return true; }
            }
        }*/

        // Vanilla's parameterless UpdateData() is the only place that safely removes disposed
        // elements from _currentTrackedElements (it iterates _elementsToRemove, not the tracked
        // list itself). Once that has run, PendingPurge entries no longer present in the real
        // list can be forgotten. This also re-sorts every tick so ETAs that cross each other over
        // time keep the display in order, not just at row-creation time.
        [HarmonyPatch(typeof(UIModuleFactionAgendaTracker), "UpdateData", new Type[] { })]
        public static class UIModuleFactionAgendaTracker_UpdateDataNoArgs_Patch
        {
            public static void Postfix(UIModuleFactionAgendaTracker __instance, GeoFaction ____faction, GeoscapeViewContext ____context)
            {
                try
                {
                    // Self-heal the cache: this postfix can run (e.g. right after a save load)
                    // before InitialSetup()'s Prefix has assigned it for the current instance.
                    if (AgendaConstants.factionTracker != __instance)
                    {
                        AgendaConstants.factionTracker = __instance;
                    }

                    var elements = AgendaConstants.GetTrackedElements(__instance);
                    if (elements != null)
                    {
                        var stillPresent = new HashSet<UIFactionDataTrackerElement>(elements);
                        AgendaConstants.PendingPurge.RemoveWhere(e => !stillPresent.Contains(e));
                    }

                    EnsureLiveVanillaTrackers(__instance, ____faction);
                    UIModuleFactionAgendaTracker_InitialSetup_Patch.EnsureCustomTrackers(__instance, ____faction, ____context);

                    AgendaConstants.OrderElements.Invoke(__instance, null);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }

            // Vanilla only (re)creates rows for the current research, the current manufacture
            // item, in-progress vehicle actions and building/repairing facilities from
            // InitialSetup() - and InitialSetup() only runs when the player changes geoscape
            // selection (deselecting/selecting a vehicle). There is no live event hookup in
            // vanilla UIModuleFactionAgendaTracker for any of these. So when e.g. a research or
            // manufacture item finishes and the next one in queue starts automatically while the
            // player hasn't touched geoscape selection since (out on a mission, fast-forwarding
            // time, managing a base, etc.), the old row is disposed by the per-second tick but no
            // row is ever created for the new current item - from the player's perspective the
            // agenda entry just vanishes until they next reselect something on the geoscape.
            //
            // This mirrors InitialSetup()'s own conditions and reuses vanilla's own Add methods
            // (skipping any object that already has a live row), so it is idempotent and safe to
            // run every tick; any gap self-heals within about a second.
            //
            // Invoke target must be __instance, not the cached AgendaConstants.factionTracker:
            // that static cache is only assigned by InitialSetup()'s Prefix, and this UpdateData()
            // postfix can run (e.g. right after a save load) before that Prefix has ever fired for
            // the current tracker instance, in which case the cache is still null/stale and
            // MethodInfo.Invoke(null, ...) throws "Non-static method requires a target".
            private static void EnsureLiveVanillaTrackers(UIModuleFactionAgendaTracker tracker, GeoFaction faction)
            {
                if (tracker == null || faction == null) return;

                // Query against this tracker instance directly rather than
                // AgendaHelpers.FindTrackedElement (which reads the static
                // AgendaConstants.factionTracker cache) - for the same reason the Invoke
                // target above is __instance and not that cache.
                var liveElements = AgendaConstants.GetLiveTrackedElements(tracker);
                bool IsTracked(object trackedObject) =>
                    liveElements != null && liveElements.Any(e => ReferenceEquals(e.TrackedObject, trackedObject));

                ResearchElement currentResearch = faction.Research?.Current;
                if (currentResearch != null && !IsTracked(currentResearch))
                {
                    AgendaConstants.OnResearchStartedMethod?.Invoke(tracker, new object[] { currentResearch });
                }

                ItemManufacturing.ManufactureQueueItem currentManufacture = faction.Manufacture?.Current;
                if (currentManufacture != null && !IsTracked(currentManufacture))
                {
                    AgendaConstants.OnItemStartedManufacturingMethod?.Invoke(tracker, new object[] { currentManufacture });
                }

                foreach (GeoVehicle vehicle in faction.Vehicles)
                {
                    if (VehicleActionsViewService.GetCurrentActionTime(vehicle) > TimeUnit.Zero
                        && !IsTracked(vehicle))
                    {
                        AgendaConstants.OnVehicleActionMethod?.Invoke(tracker, new object[] { vehicle });
                    }
                }

                if (faction is GeoPhoenixFaction phoenixFaction)
                {
                    foreach (GeoPhoenixBase pxBase in phoenixFaction.Bases)
                    {
                        foreach (GeoPhoenixFacility facility in pxBase.Layout.Facilities)
                        {
                            if ((facility.IsRepairing || (facility.IsBuilding && facility.ConstructionTime != TimeUnit.Zero))
                                && !IsTracked(facility))
                            {
                                AgendaConstants.OnFacilityBuildingMethod?.Invoke(tracker, new object[] { facility });
                            }
                        }
                    }
                }
            }
        }

        #endregion

        [HarmonyPatch(
        typeof(UIModuleFactionAgendaTracker),
        "OrderElements"
    )]
        internal static class UIModuleFactionAgendaTracker_OrderElements_Patch
        {
            // ETAs within the same hour are treated as equal.
            private const long EtaBucketSizeTicks = TimeSpan.TicksPerHour;

            [HarmonyPrefix]
            private static bool Prefix(
                GeoscapeViewContext ____context,
                List<UIFactionDataTrackerElement> ____currentTrackedElements)
            {
                long currentGeoscapeTicks =
                    ____context.Level.Timing.Now.DateTime.Ticks;

                List<UIFactionDataTrackerElement> orderedElements =
                    ____currentTrackedElements
                        // Ascending: lowest/soonest ETA appears first.
                        .OrderBy(element =>
                            GetCompletionTimeBucket(
                                element,
                                currentGeoscapeTicks
                            ))
                        // Deterministic ordering within the same ETA bucket.
                        .ThenBy(
                            element => element.TrackedName.text,
                            StringComparer.CurrentCultureIgnoreCase
                        )
                        .ToList();

                for (int index = 0; index < orderedElements.Count; index++)
                {
                    orderedElements[index].transform.SetSiblingIndex(index);
                }

                // Fully replace the original OrderElements method.
                return false;
            }

            private static long GetCompletionTimeBucket(
                UIFactionDataTrackerElement element,
                long currentGeoscapeTicks)
            {
                long estimatedCompletionTicks =
                    currentGeoscapeTicks +
                    element.CurrentTimeLeft.TimeSpan.Ticks;

                // Round the estimated completion time to the nearest hour.
                return (estimatedCompletionTicks + EtaBucketSizeTicks / 2L) /
                       EtaBucketSizeTicks;
            }
        }
    }
}