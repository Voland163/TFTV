using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TFTV.TFTVBaseRework;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class Resolution
    {
        /// <summary>
        /// Full Harmony-only implementation for delayed geoscape operations that:
        /// 1) start from specific encounter options,
        /// 2) lock aircraft movement while resolving,
        /// 3) show site timer + circular progress,
        /// 4) can be cancelled by issuing a move order,
        /// 5) survive save/load by encoding state in timer ids.
        /// </summary>

        internal sealed class IncidentInProgressDefinition
        {
            public string StartEventId;
            public int StartChoiceIndex;
            public string CompletionEventId;
            public string CancelEventId;

            public float BaseHours = 24f;
            public float MinHours = 4f;
            public float MaxHours = 72f;
            public float PerSoldierSpeedBonusHours = 1.5f;

            public bool Matches(string eventId, int choiceIndex)
            {
                return string.Equals(StartEventId, eventId, StringComparison.OrdinalIgnoreCase) && StartChoiceIndex == choiceIndex;
            }

            public float ComputeDurationHours(GeoVehicle vehicle)
            {
                int soldiers = vehicle?.Soldiers?.Count() ?? 0;

                float hours = BaseHours - soldiers * PerSoldierSpeedBonusHours;
                return Mathf.Clamp(hours, MinHours, MaxHours);
            }
        }

        internal sealed class ActiveTimedProblem
        {
            public string TimerId;
            public string CompletionEventId;
            public string CancelEventId;
            public int SiteId;
            public int VehicleId;
            public int LeaderId;
            public string ApproachTokens;
            public TimeUnit StartAt;
            public TimeUnit EndAt;

            public bool IsExpired(Timing timing)
            {
                return timing.Now > EndAt;
            }

            public static string BuildTimerId(
                GeoSite site,
                GeoVehicle vehicle,
                string completionEventId,
                string cancelEventId,
                int leaderId,
                string approachTokens)
            {
                // Format: GTP|siteId|vehicleId|completion|cancel|leaderId|approachTokens|ticksUtc
                return string.Join("|", new[]
                {
                "GTP",
                (site?.SiteId ?? -1).ToString(CultureInfo.InvariantCulture),
                (vehicle?.VehicleID ?? -1).ToString(CultureInfo.InvariantCulture),
                completionEventId ?? string.Empty,
                cancelEventId ?? string.Empty,
                leaderId.ToString(CultureInfo.InvariantCulture),
                approachTokens ?? string.Empty,
                DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)
            });
            }

            public static bool TryParseFromTimer(GeoEventTimer timer, out ActiveTimedProblem active)
            {
                active = null;
                if (timer == null || string.IsNullOrEmpty(timer.ID) || !timer.ID.StartsWith("GTP|", StringComparison.Ordinal))
                {
                    return false;
                }

                string[] parts = timer.ID.Split('|');
                if (parts.Length < 6)
                {
                    return false;
                }

                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int siteId))
                {
                    return false;
                }

                if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int vehicleId))
                {
                    return false;
                }

                int leaderId = -1;
                string approachTokens = string.Empty;

                if (parts.Length >= 7)
                {
                    int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out leaderId);
                }

                if (parts.Length >= 8)
                {
                    approachTokens = parts[6];
                }

                active = new ActiveTimedProblem
                {
                    TimerId = timer.ID,
                    SiteId = siteId,
                    VehicleId = vehicleId,
                    LeaderId = leaderId,
                    ApproachTokens = approachTokens,
                    CompletionEventId = parts[3],
                    CancelEventId = parts[4],
                    StartAt = timer.StartAt,
                    EndAt = timer.EndAt
                };
                return true;
            }
        }

        internal static class IncidentController
        {
            private const float DefaultBaseHours = 24f;
            private const float DefaultMinHours = 4f;
            private const float DefaultMaxHours = 72f;
            private const float DefaultPerSoldierSpeedBonusHours = 1.5f;

            private static readonly List<IncidentInProgressDefinition> Definitions = new List<IncidentInProgressDefinition>();
            private static readonly Dictionary<string, ActiveTimedProblem> ActiveByTimerId = new Dictionary<string, ActiveTimedProblem>(StringComparer.Ordinal);
            private static readonly Dictionary<int, GeoActorProgressionVisualController> SiteProgressVisuals = new Dictionary<int, GeoActorProgressionVisualController>();

            private static readonly AccessTools.FieldRef<UIModuleSiteEncounters, GeoscapeEvent> CurrentEventRef =
                AccessTools.FieldRefAccess<UIModuleSiteEncounters, GeoscapeEvent>("_geoEvent");

            private static readonly AccessTools.FieldRef<GeoscapeEventSystem, Dictionary<string, GeoEventTimer>> TimersRef =
                AccessTools.FieldRefAccess<GeoscapeEventSystem, Dictionary<string, GeoEventTimer>>("_timers");

            private enum AffinityApproach
            {
                PsychoSociology,
                Exploration,
                Occult,
                Biotech,
                Machinery,
                Compute
            }

            private sealed class LeaderCandidate
            {
                public GeoCharacter Character;
                public int Missions;
                public int Rank;
                public AffinityApproach Approach;
            }

            private static readonly Dictionary<string, AffinityApproach> ApproachTokenMap =
                new Dictionary<string, AffinityApproach>(StringComparer.OrdinalIgnoreCase)
                {
                    { "P", AffinityApproach.PsychoSociology },
                    { "E", AffinityApproach.Exploration },
                    { "O", AffinityApproach.Occult },
                    { "B", AffinityApproach.Biotech },
                    { "M", AffinityApproach.Machinery },
                    { "C", AffinityApproach.Compute }
                };

            public static void InitializeDefaults()
            {
                if (Definitions.Count > 0)
                {
                    return;
                }

                InitializeFromIncidents();
            }

            public static void InitializeFromIncidents()
            {
                if (GeoscapeEvents.IncidentDefinitions == null || GeoscapeEvents.IncidentDefinitions.Count == 0)
                {
                    TFTVLogger.Always("[Incidents] No incident definitions available for Resolution.");
                    return;
                }

                foreach (Objects.GeoIncidentDefinition incident in GeoscapeEvents.IncidentDefinitions)
                {
                    string introEventId = incident?.IntroEvent?.EventID;
                    if (string.IsNullOrEmpty(introEventId))
                    {
                        continue;
                    }

                    float baseHours = GetIncidentBaseHours(incident);

                    AddIncidentDefinition(introEventId, 0, incident.ChoiceAResolutionSuccess, incident.ChoiceAResolutionFailure, baseHours);
                    AddIncidentDefinition(introEventId, 1, incident.ChoiceBResolutionSuccess, incident.ChoiceBResolutionFailure, baseHours);
                }
            }

            private static float GetIncidentBaseHours(Objects.GeoIncidentDefinition incident)
            {
                if (incident?.ResolutionTime == null)
                {
                    return DefaultBaseHours;
                }

                double hours = incident.ResolutionTime.TimeSpan.TotalHours;
                if (hours <= 0)
                {
                    return DefaultBaseHours;
                }

                return (float)hours;
            }

            private static void AddIncidentDefinition(
                string introEventId,
                int choiceIndex,
                GeoscapeEventDef successEvent,
                GeoscapeEventDef failureEvent,
                float baseHours)
            {
                string completionEventId = successEvent?.EventID;
                string cancelEventId = failureEvent?.EventID;

                if (string.IsNullOrEmpty(completionEventId) && string.IsNullOrEmpty(cancelEventId))
                {
                    return;
                }

                Definitions.Add(new IncidentInProgressDefinition
                {
                    StartEventId = introEventId,
                    StartChoiceIndex = choiceIndex,
                    CompletionEventId = completionEventId,
                    CancelEventId = cancelEventId,
                    BaseHours = baseHours,
                    MinHours = DefaultMinHours,
                    MaxHours = DefaultMaxHours,
                    PerSoldierSpeedBonusHours = DefaultPerSoldierSpeedBonusHours
                });
            }

            public static void OnGeoscapeLevelStart(GeoscapeEventSystem eventSystem)
            {
                RehydrateFromTimers(eventSystem);
                RefreshAllVisuals(eventSystem?.gameObject?.GetComponent<GeoLevelController>());
            }

            public static void OnChoiceResolved(UIModuleSiteEncounters module, GeoEventChoice selectedChoice)
            {
                try
                {
                    GeoscapeEvent geoscapeEvent = CurrentEventRef(module);
                    if (geoscapeEvent?.Context?.Level == null || geoscapeEvent.Context.Site == null)
                    {
                        return;
                    }

                    if (IsCancelChoice(selectedChoice))
                    {
                        RestoreIntroEventOnCancel(geoscapeEvent);
                        return;
                    }

                    GeoVehicle vehicle = geoscapeEvent.Context.Vehicle ?? ResolveVehicleForSite(geoscapeEvent.Context.Site);
                    if (vehicle == null)
                    {
                        return;
                    }

                    int choiceIndex = geoscapeEvent.EventData?.Choices?.IndexOf(selectedChoice) ?? -1;
                    if (choiceIndex < 0)
                    {
                        return;
                    }

                    IncidentInProgressDefinition definition = Definitions.FirstOrDefault(d => d.Matches(geoscapeEvent.EventID, choiceIndex));
                    if (definition == null)
                    {
                        return;
                    }

                    ClearIntroEventState(geoscapeEvent);
                    StartTimedProblem(geoscapeEvent, geoscapeEvent.Context.Level, geoscapeEvent.Context.Site, vehicle, definition, selectedChoice, choiceIndex);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }

            }

            internal static bool TryComputeIncidentHours(
                GeoscapeEvent geoscapeEvent,
                GeoVehicle vehicle,
                int choiceIndex,
                GeoCharacter leader,
                out float hours)
            {
                hours = 0f;

                if (geoscapeEvent == null || vehicle == null)
                {
                    return false;
                }

                InitializeDefaults();

                IncidentInProgressDefinition definition = Definitions.FirstOrDefault(d => d.Matches(geoscapeEvent.EventID, choiceIndex));
                if (definition == null)
                {
                    return false;
                }

                List<GeoEventChoice> choices = geoscapeEvent.EventData?.Choices;
                if (choices == null || choiceIndex < 0 || choiceIndex >= choices.Count)
                {
                    return false;
                }

                hours = definition.ComputeDurationHours(vehicle);

                GeoEventChoice choice = choices[choiceIndex];
                string approachTokens = LeaderSelection.ExtractApproachTokens(choice?.Text?.LocalizationKey, choiceIndex);
                float bonusHours = LeaderSelection.GetLeaderBonusHours(leader, approachTokens);

                if (bonusHours > 0f)
                {
                    hours = Mathf.Clamp(hours - bonusHours, definition.MinHours, definition.MaxHours);
                }

                return true;
            }

            private static void StartTimedProblem(
                GeoscapeEvent geoscapeEvent,
                GeoLevelController level,
                GeoSite site,
                GeoVehicle vehicle,
                IncidentInProgressDefinition definition,
                GeoEventChoice choice,
                int choiceIndex)
            {
                string approachTokens = LeaderSelection.ExtractApproachTokens(choice?.Text?.LocalizationKey, choiceIndex);
                float durationHours = definition.ComputeDurationHours(vehicle);
                int leaderId = -1;

                if (IncidentResolutionUI.GeoscapeEventCrewListPatch.TryGetSelectedLeader(geoscapeEvent, vehicle, out GeoCharacter selectedLeader))
                {
                    float bonusHours = LeaderSelection.GetLeaderBonusHours(selectedLeader, approachTokens);
                    durationHours = Mathf.Clamp(durationHours - bonusHours, definition.MinHours, definition.MaxHours);
                    leaderId = selectedLeader.Id;

                    if (bonusHours > 0f)
                    {
                        TFTVLogger.Always($"[Incidents] Selected leader {selectedLeader.DisplayName} reduced time by {bonusHours:0.#}h.");
                    }
                }
                else if (LeaderSelection.TrySelectLeader(vehicle, choice, choiceIndex, out LeaderSelection.LeaderSelectionResult leader))
                {
                    float bonusHours = leader.BonusHours;
                    durationHours = Mathf.Clamp(durationHours - bonusHours, definition.MinHours, definition.MaxHours);
                    leaderId = leader.LeaderId;
                    TFTVLogger.Always($"[Incidents] Leader {leader.Character.DisplayName} ({leader.Approach}) rank {leader.Rank} reduced time by {bonusHours:0.#}h.");
                }
                else
                {
                    GeoCharacter fallback = LeaderSelection.SelectFallbackLeader(vehicle);
                    leaderId = fallback?.Id ?? -1;
                }

                TimeUnit duration = TimeUnit.FromHours(durationHours);
                string timerId = ActiveTimedProblem.BuildTimerId(
                    site,
                    vehicle,
                    definition.CompletionEventId,
                    definition.CancelEventId,
                    leaderId,
                    approachTokens);

                GeoEventTimer timer = level.EventSystem.StartTimer(timerId, duration);
                site.ExpiringTimerAt = timer.EndAt;

                vehicle.CanRedirect = false;
                if (vehicle.Navigation != null)
                {
                    vehicle.Navigation.CancelNavigation();
                }

                ActiveByTimerId[timerId] = new ActiveTimedProblem
                {
                    TimerId = timerId,
                    CompletionEventId = definition.CompletionEventId,
                    CancelEventId = definition.CancelEventId,
                    SiteId = site.SiteId,
                    VehicleId = vehicle.VehicleID,
                    LeaderId = leaderId,
                    ApproachTokens = approachTokens,
                    StartAt = timer.StartAt,
                    EndAt = timer.EndAt
                };

                RefreshSiteVisual(level, site.SiteId);
            }

            private static void RestoreIntroEventOnCancel(GeoscapeEvent geoscapeEvent)
            {
                GeoLevelController level = geoscapeEvent?.Context?.Level;
                GeoSite site = geoscapeEvent?.Context?.Site;
                GeoscapeEventSystem eventSystem = level?.EventSystem;

                if (eventSystem == null || site == null || string.IsNullOrEmpty(geoscapeEvent.EventID))
                {
                    return;
                }

                eventSystem.SetEventForSite(site, geoscapeEvent.EventID);
            }

            private static GeoVehicle ResolveVehicleForSite(GeoSite site)
            {
                if (site == null)
                {
                    return null;
                }

                IEnumerable<GeoVehicle> vehicles = site.GetPlayerVehiclesOnSite();
                if (vehicles == null)
                {
                    return null;
                }

                return vehicles
                    .Where(v => v != null)
                    .OrderByDescending(v => v.MaxCharacterSpace)
                    .FirstOrDefault();
            }

            private static bool IsCancelChoice(GeoEventChoice choice)
            {
                string key = choice?.Text?.LocalizationKey;
                return !string.IsNullOrEmpty(key)
                    && key.IndexOf("_CHOICE_2", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static void ClearIntroEventState(GeoscapeEvent geoscapeEvent)
            {
                GeoLevelController level = geoscapeEvent?.Context?.Level;
                GeoSite site = geoscapeEvent?.Context?.Site;
                GeoscapeEventSystem eventSystem = level?.EventSystem;
                if (level == null || site == null || eventSystem == null)
                {
                    return;
                }

                string timerId = geoscapeEvent.EventData?.TimerID;
                if (string.IsNullOrEmpty(timerId))
                {
                    timerId = geoscapeEvent.EventID + "_TIMER";
                }

                if (eventSystem.GetTimerById(timerId) != null)
                {
                    eventSystem.RemoveTimer(timerId);
                }

                RemoveIntroObjective(level.PhoenixFaction, geoscapeEvent.EventID);
                ClearEventForSite(eventSystem, site);
            }

            private static void RemoveIntroObjective(GeoPhoenixFaction phoenixFaction, string eventId)
            {
                if (phoenixFaction == null || string.IsNullOrEmpty(eventId))
                {
                    return;
                }

                EventGeoFactionObjective objective = phoenixFaction.Objectives
                    .OfType<EventGeoFactionObjective>()
                    .FirstOrDefault(o => o.EventID == eventId);

                if (objective != null)
                {
                    phoenixFaction.RemoveObjective(objective);
                }
            }

            private static void ClearEventForSite(GeoscapeEventSystem eventSystem, GeoSite site)
            {
                MethodInfo removeEventForSite = AccessTools.Method(typeof(GeoscapeEventSystem), "RemoveEventForSite");
                if (removeEventForSite != null)
                {
                    removeEventForSite.Invoke(eventSystem, new object[] { site });
                    return;
                }

                MethodInfo clearEventForSite = AccessTools.Method(typeof(GeoscapeEventSystem), "ClearEventForSite");
                if (clearEventForSite != null)
                {
                    clearEventForSite.Invoke(eventSystem, new object[] { site });
                }
            }

            private static void RehydrateFromTimers(GeoscapeEventSystem eventSystem)
            {
                Dictionary<string, GeoEventTimer> timers = TimersRef(eventSystem);
                foreach (GeoEventTimer timer in timers.Values)
                {
                    if (!ActiveByTimerId.ContainsKey(timer.ID) && ActiveTimedProblem.TryParseFromTimer(timer, out ActiveTimedProblem active))
                    {
                        ActiveByTimerId[active.TimerId] = active;
                    }
                }
            }

            private static void RefreshAllVisuals(GeoLevelController level)
            {
                if (level == null)
                {
                    return;
                }

                HashSet<int> activeSiteIds = new HashSet<int>(ActiveByTimerId.Values.Select(v => v.SiteId));
                foreach (int siteId in activeSiteIds)
                {
                    RefreshSiteVisual(level, siteId);
                }

                List<int> obsoleteVisuals = SiteProgressVisuals.Keys.Where(id => !activeSiteIds.Contains(id)).ToList();
                foreach (int siteId in obsoleteVisuals)
                {
                    DestroySiteVisual(siteId);
                }
            }

            private static void RefreshSiteVisual(GeoLevelController level, int siteId)
            {
                GeoSite site = level.Map.AllSites.FirstOrDefault(s => s.SiteId == siteId);
                ActiveTimedProblem active = ActiveByTimerId.Values.FirstOrDefault(v => v.SiteId == siteId);
                if (site == null || active == null)
                {
                    DestroySiteVisual(siteId);
                    return;
                }

                if (!SiteProgressVisuals.TryGetValue(siteId, out GeoActorProgressionVisualController controller) || controller == null)
                {
                    GeoVehicle vehicle = level.Factions.SelectMany(f => f.Vehicles).FirstOrDefault(v => v.VehicleID == active.VehicleId);
                    GeoActorProgressionVisualController prefab = vehicle?.VehicleDef?.ExplorationVisualsPrefab;
                    if (prefab == null || site.Surface == null)
                    {
                        return;
                    }

                    controller = UnityEngine.Object.Instantiate(prefab, site.Surface);
                    if (controller == null)
                    {
                        return;
                    }

                    SiteProgressVisuals[siteId] = controller;
                }

                controller.SetProgression(active.StartAt, active.EndAt, level.Timing);
                controller.gameObject.SetActive(true);
            }

            private static void DestroySiteVisual(int siteId)
            {
                if (!SiteProgressVisuals.TryGetValue(siteId, out GeoActorProgressionVisualController controller))
                {
                    return;
                }

                SiteProgressVisuals.Remove(siteId);
                if (controller != null)
                {
                    UnityEngine.Object.Destroy(controller.gameObject);
                }
            }

            public static void Tick(GeoscapeEventSystem eventSystem)
            {
                if (eventSystem?.gameObject == null)
                {
                    return;
                }

                GeoLevelController level = eventSystem.gameObject.GetComponent<GeoLevelController>();
                if (level == null)
                {
                    return;
                }

                RehydrateFromTimers(eventSystem);

                List<string> toComplete = new List<string>();
                foreach (KeyValuePair<string, ActiveTimedProblem> kv in ActiveByTimerId)
                {
                    GeoEventTimer timer = eventSystem.GetTimerById(kv.Key);
                    if (timer == null || kv.Value.IsExpired(level.Timing))
                    {
                        toComplete.Add(kv.Key);
                    }
                }

                foreach (string timerId in toComplete)
                {
                    CompleteTimedProblem(level, timerId);
                }

                RefreshAllVisuals(level);
            }

            public static bool TryCancelForVehicle(GeoVehicle vehicle)
            {
                if (vehicle?.GeoLevel?.EventSystem == null)
                {
                    return false;
                }

                ActiveTimedProblem active = ActiveByTimerId.Values.FirstOrDefault(v => v.VehicleId == vehicle.VehicleID);
                if (active == null)
                {
                    return false;
                }

                GeoLevelController level = vehicle.GeoLevel;
                GeoSite site = level.Map.AllSites.FirstOrDefault(s => s.SiteId == active.SiteId);

                vehicle.CanRedirect = true;
                if (site != null)
                {
                    site.ExpiringTimerAt = TimeUnit.Zero;
                }

                if (vehicle.Navigation != null)
                {
                    vehicle.Navigation.CancelNavigation();
                }

                if (level.EventSystem.GetTimerById(active.TimerId) != null)
                {
                    level.EventSystem.RemoveTimer(active.TimerId);
                }

                if (!string.IsNullOrEmpty(active.CancelEventId) && site != null)
                {
                    GeoscapeEventContext ctx = new GeoscapeEventContext(site, level.ViewerFaction, vehicle);
                    level.EventSystem.TriggerGeoscapeEvent(active.CancelEventId, ctx);
                }

                ActiveByTimerId.Remove(active.TimerId);
                DestroySiteVisual(active.SiteId);
                return true;
            }

            public static bool HasActiveTimedProblem(GeoVehicle vehicle)
            {
                if (vehicle == null)
                {
                    return false;
                }

                return ActiveByTimerId.Values.Any(v => v.VehicleId == vehicle.VehicleID);
            }

            private static void StartTimedProblem(
                GeoLevelController level,
                GeoSite site,
                GeoVehicle vehicle,
                IncidentInProgressDefinition definition,
                GeoEventChoice choice,
                int choiceIndex)
            {
                float durationHours = definition.ComputeDurationHours(vehicle);
                int leaderId = -1;

                if (LeaderSelection.TrySelectLeader(vehicle, choice, choiceIndex, out LeaderSelection.LeaderSelectionResult leader))
                {
                    float bonusHours = leader.BonusHours;
                    durationHours = Mathf.Clamp(durationHours - bonusHours, definition.MinHours, definition.MaxHours);
                    leaderId = leader.LeaderId;
                    TFTVLogger.Always($"[Incidents] Leader {leader.Character.DisplayName} ({leader.Approach}) rank {leader.Rank} reduced time by {bonusHours:0.#}h.");
                }
                else
                {
                    GeoCharacter fallback = LeaderSelection.SelectFallbackLeader(vehicle);
                    leaderId = fallback?.Id ?? -1;
                }

                string approachTokens = LeaderSelection.ExtractApproachTokens(choice?.Text?.LocalizationKey, choiceIndex);
                TimeUnit duration = TimeUnit.FromHours(durationHours);
                string timerId = ActiveTimedProblem.BuildTimerId(
                    site,
                    vehicle,
                    definition.CompletionEventId,
                    definition.CancelEventId,
                    leaderId,
                    approachTokens);

                GeoEventTimer timer = level.EventSystem.StartTimer(timerId, duration);
                site.ExpiringTimerAt = timer.EndAt;

                vehicle.CanRedirect = false;
                if (vehicle.Navigation != null)
                {
                    vehicle.Navigation.CancelNavigation();
                }

                ActiveByTimerId[timerId] = new ActiveTimedProblem
                {
                    TimerId = timerId,
                    CompletionEventId = definition.CompletionEventId,
                    CancelEventId = definition.CancelEventId,
                    SiteId = site.SiteId,
                    VehicleId = vehicle.VehicleID,
                    LeaderId = leaderId,
                    ApproachTokens = approachTokens,
                    StartAt = timer.StartAt,
                    EndAt = timer.EndAt
                };

                RefreshSiteVisual(level, site.SiteId);
            }

            private static void CompleteTimedProblem(GeoLevelController level, string timerId)
            {
                if (!ActiveByTimerId.TryGetValue(timerId, out ActiveTimedProblem active))
                {
                    return;
                }

                GeoSite site = level.Map.AllSites.FirstOrDefault(s => s.SiteId == active.SiteId);
                GeoVehicle vehicle = level.PhoenixFaction.Vehicles.FirstOrDefault(v => v.VehicleID == active.VehicleId)
                                     ?? level.Factions.SelectMany(f => f.Vehicles).FirstOrDefault(v => v.VehicleID == active.VehicleId);

                if (vehicle != null)
                {
                    vehicle.CanRedirect = true;
                }

                if (site != null)
                {
                    site.ExpiringTimerAt = TimeUnit.Zero;
                }

                TFTVLogger.Always($"[Incidents][OutcomeRecapDiag] COMPLETE timer={timerId} event={active.CompletionEventId} site={active.SiteId} vehicle={active.VehicleId} leaderId={active.LeaderId} tokens={active.ApproachTokens}");

                string affinityResultText = "No change";
                if (IsSuccessOutcome(active.CompletionEventId))
                {
                    affinityResultText = ApplyIncidentAffinity(level, vehicle, active);
                    TFTVLogger.Always($"[Incidents][OutcomeRecapDiag] AFFINITY result={affinityResultText}");
                    IncidentOutcomeSummaryUI.RecordIncidentSuccess(level, vehicle, active, affinityResultText);
                }

                if (!string.IsNullOrEmpty(active.CompletionEventId) && site != null && vehicle != null)
                {
                    TFTVLogger.Always($"[Incidents][OutcomeRecapDiag] TRIGGER event={active.CompletionEventId}");
                    GeoscapeEventContext ctx = new GeoscapeEventContext(site, level.ViewerFaction, vehicle);
                    level.EventSystem.TriggerGeoscapeEvent(active.CompletionEventId, ctx);
                }

                ActiveByTimerId.Remove(timerId);
                DestroySiteVisual(active.SiteId);
            }

            private static bool IsSuccessOutcome(string eventId)
            {
                return !string.IsNullOrEmpty(eventId)
                    && eventId.EndsWith("_SUCCESS", StringComparison.OrdinalIgnoreCase);
            }

            private static string ApplyIncidentAffinity(GeoLevelController level, GeoVehicle vehicle, ActiveTimedProblem active)
            {
                try
                {
                    if (level == null || vehicle == null || active == null || string.IsNullOrEmpty(active.ApproachTokens))
                    {
                        return "No change";
                    }

                    GeoCharacter leader = LeaderSelection.ResolveLeader(level, vehicle, active.LeaderId);
                    if (leader?.Progression == null)
                    {
                        return "No change";
                    }

                    List<LeaderSelection.AffinityApproach> approaches = LeaderSelection.ParseApproachTokens(active.ApproachTokens);
                    if (approaches.Count == 0)
                    {
                        return "No change";
                    }

                    LeaderSelection.AffinityApproach chosenApproach;
                    int targetRank;

                    if (LeaderSelection.TryGetCurrentAffinity(leader, out LeaderSelection.AffinityApproach currentApproach, out int currentRank))
                    {
                        if (!approaches.Contains(currentApproach) || currentRank >= 3)
                        {
                            return "No change";
                        }

                        chosenApproach = currentApproach;
                        targetRank = currentRank + 1;
                    }
                    else
                    {
                        // deterministic first gain from token order (M_P => Machinery)
                        chosenApproach = approaches[0];
                        targetRank = 1;
                        TFTVLogger.Always($"[Incidents][Affinity] Leader had no affinity. Deterministic pick={chosenApproach} from tokens={active.ApproachTokens}");
                    }

                    PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(chosenApproach, targetRank);
                    if (ability == null)
                    {
                        return "No change";
                    }

                    if (!leader.Progression.Abilities.Contains(ability))
                    {
                        leader.Progression.AddAbility(ability);

                        AffinityBenefitChoiceUI.RecordAffinityAward(
                            active.CompletionEventId,
                            active.SiteId,
                            active.VehicleId,
                            chosenApproach,
                            targetRank);

                        TFTVLogger.Always($"[Incidents] {leader.DisplayName} gained {chosenApproach} rank {targetRank}.");
                        return $"{chosenApproach} rank {targetRank}";
                    }

                    return "No change";
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

            [HarmonyPatch(typeof(GeoscapeEventSystem), "OnLevelStart")]
            internal static class GeoscapeEventSystem_OnLevelStart_Patch
            {
                private static void Postfix(GeoscapeEventSystem __instance)
                {
                    IncidentController.OnGeoscapeLevelStart(__instance);
                }
            }

            [HarmonyPatch(typeof(UIModuleSiteEncounters), "SelectChoice")]
            internal static class UIModuleSiteEncounters_SelectChoice_Patch
            {
                private static void Postfix(UIModuleSiteEncounters __instance, GeoEventChoice choice)
                {
                    IncidentController.OnChoiceResolved(__instance, choice);
                }
            }

            [HarmonyPatch(typeof(GeoscapeEventSystem), "Update")]
            internal static class GeoscapeEventSystem_Update_Patch
            {
                private static void Postfix(GeoscapeEventSystem __instance)
                {
                    IncidentController.Tick(__instance);
                }
            }

            [HarmonyPatch(typeof(MoveVehicleAbility), "GetDisabledStateInternal")]
            internal static class MoveVehicleAbility_GetDisabledStateInternal_Patch
            {
                private static void Postfix(MoveVehicleAbility __instance, ref GeoAbilityDisabledState __result)
                {
                    GeoVehicle vehicle = __instance.Actor as GeoVehicle;
                    if (vehicle == null)
                    {
                        return;
                    }

                    if (IncidentController.HasActiveTimedProblem(vehicle))
                    {
                        // Allow selecting move: the actual activation patch performs cancellation first.
                        __result = GeoAbilityDisabledState.NotDisabled;
                    }
                }
            }

            [HarmonyPatch(typeof(MoveVehicleAbility), "ActivateInternal")]
            internal static class MoveVehicleAbility_ActivateInternal_Patch
            {
                private static void Prefix(MoveVehicleAbility __instance)
                {
                    GeoVehicle vehicle = __instance.Actor as GeoVehicle;
                    if (vehicle == null)
                    {
                        return;
                    }

                    // Player-issued movement while operation is active is treated as explicit cancellation.
                    IncidentController.TryCancelForVehicle(vehicle);
                }
            }

            [HarmonyPatch(typeof(GeoscapeEventSystem), nameof(GeoscapeEventSystem.TriggerGeoscapeEvent))]
            internal static class GeoscapeEventSystem_TriggerGeoscapeEvent_IncidentPersonnelReward_Patch
            {
                private sealed class SameHavenTagInfo
                {
                    public int IncidentId;
                    public string FactionSuffix;

                    public SameHavenTagInfo(int incidentId, string factionSuffix)
                    {
                        IncidentId = incidentId;
                        FactionSuffix = factionSuffix;
                    }
                }

                private static readonly Dictionary<string, SameHavenTagInfo> SameHavenTagEvents =
                    new Dictionary<string, SameHavenTagInfo>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "TFTV_INCIDENT_5_AN_OUTCOME_0_SUCCESS", new SameHavenTagInfo(5, "AN") },
                        { "TFTV_INCIDENT_5_AN_OUTCOME_1_SUCCESS", new SameHavenTagInfo(5, "AN") },
                        { "TFTV_INCIDENT_19_NJ_OUTCOME_0_SUCCESS", new SameHavenTagInfo(19, "NJ") },
                        { "TFTV_INCIDENT_19_NJ_OUTCOME_1_SUCCESS", new SameHavenTagInfo(19, "NJ") }
                    };

                private static readonly HashSet<string> DoublePersonnelRewardEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TFTV_INCIDENT_5_AN_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_5_AN_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_9_AN_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_9_AN_OUTCOME_1_SUCCESS",

                "TFTV_INCIDENT_17_NJ_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_19_NJ_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_19_NJ_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_23_NJ_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_29_NJ_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_29_NJ_OUTCOME_1_SUCCESS",

                "TFTV_INCIDENT_32_SY_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_32_SY_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_36_SY_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_36_SY_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_37_SY_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_37_SY_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_42_SY_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_42_SY_OUTCOME_1_SUCCESS",
                "TFTV_INCIDENT_43_SY_OUTCOME_0_SUCCESS",
                "TFTV_INCIDENT_43_SY_OUTCOME_1_SUCCESS"
            };

                private static void Postfix(string eventId, GeoscapeEventSystem __instance, GeoscapeEventContext context)
                {
                    try
                    {
                        if (SameHavenTagEvents.TryGetValue(eventId, out SameHavenTagInfo tagInfo))
                        {
                            GeoscapeEvents.AddSameHavenSiteTag(context?.Site, tagInfo.IncidentId, tagInfo.FactionSuffix);
                        }

                        if (!TryGetIncidentRewardCount(eventId, out int count))
                        {
                            return;
                        }

                        GeoPhoenixFaction phoenixFaction = context?.Level?.PhoenixFaction
                            ?? __instance?.gameObject?.GetComponent<GeoLevelController>()?.PhoenixFaction;

                        if (phoenixFaction == null)
                        {
                            return;
                        }

                        PersonnelData.AddIncidentPersonnelReward(phoenixFaction, count);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static bool TryGetIncidentRewardCount(string eventId, out int count)
                {
                    count = 0;
                    if (string.IsNullOrEmpty(eventId))
                    {
                        return false;
                    }

                    if (!eventId.StartsWith("TFTV_INCIDENT_", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (eventId.IndexOf("_OUTCOME_", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return false;
                    }

                    if (eventId.EndsWith("_SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        count = 2;
                        return ApplyDoublePersonnelRewardIfNeeded(eventId, ref count);
                    }

                    if (eventId.EndsWith("_FAIL", StringComparison.OrdinalIgnoreCase))
                    {
                        count = 1;
                        return ApplyDoublePersonnelRewardIfNeeded(eventId, ref count);
                    }

                    return false;
                }

                private static bool ApplyDoublePersonnelRewardIfNeeded(string eventId, ref int count)
                {
                    if (count > 0 && DoublePersonnelRewardEvents.Contains(eventId))
                    {
                        count *= 2;
                    }

                    return true;
                }
            }

        }
    }
}
