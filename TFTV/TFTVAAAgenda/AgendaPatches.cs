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
        private static readonly MethodInfo UpdateProductionMethod =
            AccessTools.Method(typeof(GeoFaction), "UpdateProduction");

        private static bool _pendingBaseReworkAgendaProductionRefresh;

        internal static void QueueBaseReworkAgendaProductionRefresh()
        {
            _pendingBaseReworkAgendaProductionRefresh = BaseReworkUtils.BaseReworkEnabled;
        }

        private static void RefreshBaseReworkProductionIfPending(GeoscapeViewContext context)
        {
            try
            {
                if (!_pendingBaseReworkAgendaProductionRefresh || !BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }

                GeoPhoenixFaction phoenix = context?.ViewerFaction as GeoPhoenixFaction;
                _pendingBaseReworkAgendaProductionRefresh = false;

                if (phoenix == null || UpdateProductionMethod == null)
                {
                    return;
                }

                UpdateProductionMethod.Invoke(phoenix, new object[] { });
            }
            catch (Exception e)
            {
                _pendingBaseReworkAgendaProductionRefresh = false;
                TFTVLogger.Error(e);
            }
        }

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

        [HarmonyPatch(typeof(GeoAlienFaction), "StartPhoenixBaseAssault")]
        public static class GeoAlienFaction_StartPhoenixBaseAssault_Patch
        {
            public static bool Prefix(GeoAlienFaction __instance, SiteAttackSchedule target)
            {
                try
                {
                    TFTVLogger.Debug($"[StartPhoenixBaseAssault] Base assault on {target.Site.Name} should start");
                    GeoSite pxBase = target.Site;
                    var source = __instance.Bases.Where(p => p.SitesInRange.Contains(pxBase));
                    if (!source.Any())
                        TFTVLogger.Debug("[StartPhoenixBaseAssault] No alien base in range. Overriding. Starting mission NOW.");

                    pxBase.CreatePhoenixBaseDefenseMission(new PhoenixBaseAttacker(__instance, source.Select(s => s.Site)));
                    return false;
                }
                catch (Exception e) { TFTVLogger.Error(e); return true; }
            }
        }

        [HarmonyPatch(typeof(UIModuleStatusBarMessages), "Update")]
        public static class UIModuleStatusBarMessages_Update_Patch
        {
            public static void Postfix(UIModuleStatusBarMessages __instance)
            {
                try { __instance.TimedEventRoot.gameObject.SetActive(false); }
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

        [HarmonyPatch(typeof(UIModuleSiteContextualMenu), nameof(UIModuleSiteContextualMenu.SetMenuItems))]
        public static class UIModuleSiteContextualMenu_SetMenuItems_Patch
        {
            public static void Postfix(GeoSite site, List<SiteContextualMenuItem> ____menuItems)
            {
                try
                {
                    foreach (var item in ____menuItems)
                    {
                        if (item.Ability is MoveVehicleAbility move && move.GeoActor is GeoVehicle v && v.CurrentSite != site)
                        {
                            if (AgendaHelpers.GetTravelTime(v, out float eta, site))
                                item.ItemText.text += AgendaHelpers.AppendTime(eta);
                        }
                        else if (item.Ability is ExploreSiteAbility)
                        {
                            var vehicle = item.Ability?.GeoActor as GeoVehicle;
                            float hours = AgendaHelpers.GetExplorationTime(vehicle, (float)site.ExplorationTime.TimeSpan.TotalHours);
                            item.ItemText.text += AgendaHelpers.AppendTime(hours);
                        }
                    }
                }
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

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnContextualItemSelected")]
        public static class UIStateVehicleSelected_OnContextualItemSelected_Patch
        {
            public static void Postfix(GeoAbility ability)
            {
                try
                {
                    GeoVehicle vehicle = null;
                    string vehicleInfo = null;

                    if (ability is MoveVehicleAbility)
                    {
                        vehicle = (GeoVehicle)ability.GeoActor;
                        vehicleInfo = AgendaHelpers.BuildVehicleText(vehicle, true);
                    }
                    else if (ability is ExploreSiteAbility)
                    {
                        vehicle = (GeoVehicle)ability.GeoActor;
                        vehicleInfo = AgendaHelpers.BuildVehicleText(vehicle, false);
                    }

                    if (vehicle == null) return;
                    AddOrUpdateVehicleTracker(vehicle, vehicleInfo);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateVehicleSelected), "AddTravelSite")]
        public static class UIStateVehicleSelected_AddTravelSite_Patch
        {
            public static void Postfix(UIStateVehicleSelected __instance, GeoSite site)
            {
                try
                {
                    var vehicle = (GeoVehicle)AccessTools.Property(typeof(UIStateVehicleSelected), "SelectedVehicle").GetValue(__instance, null);
                    var ability = vehicle.GetAbility<MoveVehicleAbility>();
                    if (ability == null || !ability.CanActivate(new GeoAbilityTarget(site))) return;

                    string vehicleInfo = AgendaHelpers.BuildVehicleText(vehicle, true);
                    AddOrUpdateVehicleTracker(vehicle, vehicleInfo);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        private static void AddOrUpdateVehicleTracker(GeoVehicle vehicle, string vehicleInfo)
        {
            var existing = AgendaHelpers.FindTrackedElement(vehicle);
            if (existing != null)
            {
                existing.TrackedName.text = vehicleInfo;
                AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                AgendaConstants.OrderElements.Invoke(AgendaConstants.factionTracker, null);
                return;
            }

            AgendaHelpers.AddTrackerElement(vehicle, vehicleInfo, vehicle.VehicleDef.ViewElement);
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

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleArrived")]
        public static class UIStateVehicleSelected_OnVehicleArrived_Patch
        {
            public static void Postfix(GeoVehicle vehicle, bool justPassing)
            {
                try
                {
                    if (justPassing) return;
                    RemoveAllMatchingElements<GeoVehicle>(v => v == vehicle);
                    AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnVehicleSiteExplored")]
        public static class UIStateVehicleSelected_OnVehicleSiteExplored_Patch
        {
            public static void Postfix(GeoVehicle vehicle)
            {
                try
                {
                    RemoveAllMatchingElements<GeoVehicle>(v => v == vehicle);
                    AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }
        }

        private static void RemoveAllMatchingElements<T>(Func<T, bool> predicate) where T : class
        {
            var elements = AgendaConstants.GetTrackedElements();
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
            private static readonly BindingFlags InstanceBindings =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            private static readonly string[] ManufactureQueueMemberNames = new string[]
            {
                "_queue",
                "queue",
                "Queue"
            };

            private static readonly string[] ManufacturableItemMemberNames = new string[]
            {
                "ManufacturableItem",
                "_manufacturableItem",
                "Item",
                "_item"
            };

            private static readonly string[] QueueProgressMemberNames = new string[]
            {
                "Progress",
                "_progress",
                "ManufactureProgress",
                "_manufactureProgress",
                "ProgressPoints",
                "_progressPoints",
                "ManufactureProgressPoints",
                "_manufactureProgressPoints"
            };

            private static readonly string[] ManufacturePointsCostMemberNames = new string[]
            {
                "ManufacturePointsCost",
                "_manufacturePointsCost"
            };

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

                        if (vehicle.Travelling && AgendaHelpers.GetTravelTime(vehicle, out float travelTime))
                        {
                            var arrival = TimeUnit.FromHours(travelTime);
                            element.UpdateData(arrival, true, null);
                            __result = arrival <= TimeUnit.Zero;
                        }
                        else if (vehicle.IsExploringSite)
                        {
                            float hours = AgendaHelpers.GetExplorationTime(vehicle, (float)vehicle.CurrentSite.ExplorationTime.TimeSpan.TotalHours);
                            var explorationEnd = TimeUnit.FromHours(hours);
                            element.UpdateData(explorationEnd, true, null);
                            __result = explorationEnd <= TimeUnit.Zero;
                        }
                        return false;
                    }
                    else if (element.TrackedObject is GeoCharacter character)
                    {
                        var session = TrainingFacilityRework.GetRecruitSession(character);
                        if (session != null)
                        {
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
                            TimeUnit timeLeft = facility.GetTimeLeftToUpdate();
                            element.UpdateData(timeLeft, true, null);
                            __result = timeLeft <= TimeUnit.Zero;
                            return false;
                        }
                    }
                    else if (element.TrackedObject is GeoSite geoSite)
                    {
                        AgendaHelpers.WireClickEvent(element, () => ____context.View.ChaseTarget(geoSite, false));

                        if (AgendaHelpers.TryUpdateCustomSiteElement(element, geoSite, ____context, out bool customExpired))
                        {
                            __result = customExpired;
                            return false;
                        }

                        UpdateSiteTimers(element, geoSite, ____context);
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

            private static void UpdateSiteTimers(UIFactionDataTrackerElement element, GeoSite geoSite, GeoscapeViewContext context)
            {
                if (geoSite.IsArcheologySite)
                {
                    if (geoSite.IsOwnedByViewer)
                    {
                        foreach (GeoFaction f in context.Level.Factions)
                        {
                            if (f.IsViewerFaction || f.IsEnvironmentFaction || f.IsNeutralFaction || f.IsInactiveFaction) continue;
                            foreach (var schedule in f.AncientSiteAttackSchedule)
                            {
                                if (schedule.HasAttackScheduled && schedule.Site == geoSite)
                                {
                                    TimeUnit attackTime = TimeUnit.FromHours((float)(schedule.ScheduledFor - context.Level.Timing.Now).TimeSpan.TotalHours);
                                    element.UpdateData(attackTime, true, null);
                                }
                            }
                        }
                    }
                    else
                    {
                        var excavation = geoSite.GeoLevel.PhoenixFaction.ExcavatingSites.FirstOrDefault(s => s.Site == geoSite);
                        if (excavation != null)
                        {
                            TimeUnit timeLeft = TimeUnit.FromHours((float)(excavation.ExcavationEndDate - context.Level.Timing.Now).TimeSpan.TotalHours);
                            element.UpdateData(timeLeft, true, null);
                        }
                    }
                }
                else if (geoSite.Type == GeoSiteType.PhoenixBase && TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(geoSite.SiteId))
                {
                    TimeUnit timer = TimeUnit.FromSeconds((float)(3600 * TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack[geoSite.SiteId].First().Value));
                    TimeUnit attackTime = timer - context.Level.Timing.Now;
                    element.UpdateData(attackTime, true, null);
                    geoSite.RefreshVisuals();
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
                    if (!(____faction is GeoPhoenixFaction phoenix)) return;

                    // Travelling/exploring vehicles
                    foreach (var vehicle in phoenix.Vehicles.Where(v => v.Travelling || v.IsExploringSite))
                    {
                        string info = vehicle.Travelling
                            ? AgendaHelpers.BuildVehicleText(vehicle, true)
                            : AgendaHelpers.BuildVehicleText(vehicle, false);
                        AgendaHelpers.AddTrackerElement(vehicle, info, vehicle.VehicleDef.ViewElement);
                    }

                    // Facilities under repair
                    foreach (var facility in phoenix.Bases.SelectMany(b => b.Layout.Facilities.Where(f => f.IsRepairing && f.GetTimeLeftToUpdate() != TimeUnit.Zero)))
                    {
                        string name = facility.Def.ViewElementDef.DisplayName1.Localize();
                        AgendaHelpers.AddTrackerElement(facility, $"{AgendaConstants.actionRepairing} {name}", facility.Def.ViewElementDef);
                    }

                    // Recruit training sessions
                    foreach (var session in TrainingFacilityRework.GetActiveRecruitSessions())
                    {
                        string text = AgendaHelpers.GetRecruitTrainingTrackerText(session.Character);
                        var el = AgendaHelpers.AddTrackerElement(session.Character, text, AgendaHelpers.GetTrainingViewElement());
                        AgendaHelpers.ApplyRecruitTrainingTrackerText(el, session.Character);
                    }

                    // Excavations
                    foreach (var excavation in phoenix.ExcavatingSites.Where(s => !s.IsExcavated))
                    {
                        string info = $"{AgendaConstants.actionExcavating} {excavation.Site.Name}";
                        AgendaHelpers.AddTrackerElement(excavation.Site, info, AgendaHelpers.GetGenericSiteViewElement());
                    }

                    // Custom site trackers (incidents, base activation)
                    foreach (var site in ____context.Level.Map.AllSites.Where(s => s != null && AgendaHelpers.HasCustomSiteTracker(s)))
                    {
                        string info = AgendaHelpers.GetCustomSiteTrackerText(site, ____context.ViewerFaction);
                        var el = AgendaHelpers.AddTrackerElement(site, info, AgendaHelpers.GetCustomSiteViewElement(site));
                        AgendaHelpers.ApplyCustomSiteTrackerText(el, site, ____context.ViewerFaction);
                    }

                    // Base defense and ancient site attacks
                    AddAttackTrackers(__instance, ____faction, ____context);

                    AgendaHelpers.ReapplyResolvedTrackerTexts(__instance, ____context.ViewerFaction);
                    AgendaRefresh.TryApplyPendingRefreshAfterBaseReworkRestore();
                }
                catch (Exception e) { TFTVLogger.Error(e); }
            }

            private static void AddAttackTrackers(UIModuleFactionAgendaTracker tracker, GeoFaction faction, GeoscapeViewContext context)
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
                                if (pxBase.HasActiveMission)
                                {
                                    string info = $"{geoFaction.Name.Localize().ToUpperInvariant()} {AgendaConstants.actionAttackOnPX} {pxBase.Name}";
                                    AgendaHelpers.AddTrackerElement(pxBase, info, AgendaHelpers.GetCrabmanViewElement());
                                }
                            }
                        }
                    }

                    // Ancient site attacks
                    foreach (var schedule in geoFaction.AncientSiteAttackSchedule)
                    {
                        if (schedule.HasAttackScheduled && schedule.Site.Owner == context.ViewerFaction)
                        {
                            string info = $"{geoFaction.Name.Localize().ToUpperInvariant()} {AgendaConstants.actionAttack} {schedule.Site.Name}";
                            AgendaHelpers.AddTrackerElement(schedule.Site, info, AgendaHelpers.GetGenericSiteViewElement());
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
    }
}