using Base;
using Base.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixPoint.Geoscape.Entities.Sites;
using Base.UI;
using System.Reflection;

namespace TFTV.TFTVIncidents
{
    internal class Roll
    {
        private const string IncidentRolledVariablePrefix = "TFTV_Incident_Rolled_";
        private const string IncidentNextRollHourVariable = "TFTV_Incident_NextRollHour";
        private const int MinRollHours = 10; // 4 * 24;
        private const int MaxRollHours = 20; // 6 * 24;

        internal static void TryRollIncidentOnSchedule(GeoLevelController geoLevelController)
        {
            try
            {
                if (geoLevelController == null || geoLevelController.EventSystem == null)
                {
                    return;
                }

                int currentHour = (int)geoLevelController.Timing.Now.TimeSpan.TotalHours;
                int nextRollHour = geoLevelController.EventSystem.GetVariable(IncidentNextRollHourVariable);

                if (nextRollHour <= 0)
                {
                    ScheduleNextRoll(geoLevelController, currentHour);
                    return;
                }

                if (currentHour < nextRollHour)
                {
                    return;
                }

                TryRollIncident(geoLevelController);
                ScheduleNextRoll(geoLevelController, currentHour);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static bool TryRollIncident(GeoLevelController geoLevelController)
        {
            try
            {
                if (geoLevelController == null || geoLevelController.PhoenixFaction == null)
                {
                    TFTVLogger.Always("[Incidents] Missing GeoLevelController or Phoenix faction.");
                    return false;
                }

                if (GeoscapeEvents.IncidentDefinitions == null || GeoscapeEvents.IncidentDefinitions.Count == 0)
                {
                    TFTVLogger.Always("[Incidents] No incident definitions available.");
                    return false;
                }

                GeoPhoenixFaction phoenixFaction = geoLevelController.PhoenixFaction;
                List<GeoHaven> visitedHavens = GetVisitedHavensWithoutActiveMission(geoLevelController, phoenixFaction);
                if (visitedHavens.Count == 0)
                {
                    TFTVLogger.Always("[Incidents] No visited havens without active missions.");
                    return false;
                }

                List<IncidentCandidate> candidates = GetIncidentCandidates(geoLevelController, phoenixFaction, visitedHavens);
                if (candidates.Count == 0)
                {
                    TFTVLogger.Always("[Incidents] No eligible incidents found.");
                    return false;
                }

                IncidentCandidate chosenIncident = ChooseIncidentCandidate(candidates);
                GeoHaven chosenHaven = chosenIncident.EligibleHavens.GetRandomElement();

                if (chosenHaven?.Site == null || chosenIncident.Definition.IntroEvent == null)
                {
                    TFTVLogger.Always("[Incidents] Failed to resolve incident or haven.");
                    return false;
                }

                SetupTimedEventForSite(chosenIncident.Definition.IntroEvent.EventID, chosenHaven.Site, 120);
                MarkIncidentRolled(geoLevelController, chosenIncident.Definition);
                AddIncidentLogEntryAndPause(geoLevelController, chosenIncident.Definition, chosenHaven);

                TFTVLogger.Always($"[Incidents] Rolled incident {chosenIncident.Definition.Id} at {chosenHaven.Site.LocalizedSiteName}.");
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static void AddIncidentLogEntryAndPause(
            GeoLevelController geoLevelController,
            Objects.GeoIncidentDefinition incident,
            GeoHaven haven)
        {
            try
            {
                if (geoLevelController == null || incident == null || haven?.Site == null)
                {
                    return;
                }

                string text = $"Incident {incident.IntroEvent.GeoscapeEventData.Title.Localize()} at {haven.Site.LocalizedSiteName}.";
                GeoscapeLogEntry entry = new GeoscapeLogEntry
                {
                    Text = new LocalizedTextBind(text, true)
                };

                typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(geoLevelController.Log, new object[] { entry, null });

                geoLevelController.View.SetGamePauseState(true);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string SetupTimedEventForSite(string eventID, GeoSite site, int durationHours)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(eventID))
                {
                    throw new ArgumentException("Event ID must be provided.", "eventID");
                }
                if (site == null)
                {
                    throw new ArgumentNullException("site");
                }
                if (durationHours <= 0)
                {
                    throw new ArgumentOutOfRangeException("durationHours", "Duration in hours must be greater than zero.");
                }

                GeoLevelController controller = site.GeoLevel;

                GeoscapeEventSystem eventSystem = controller.EventSystem;

                GeoscapeEventDef eventByID = eventSystem.GetEventByID(eventID, false);
                eventSystem.SetEventForSite(site, eventID);
                string text = eventByID.GeoscapeEventData.TimerID;
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = eventID + "_TIMER";
                    eventByID.GeoscapeEventData.SetMetadata(GeoscapeEventData.MetadataSchema.TimerKey, text);
                }
                TimeUnit duration = TimeUnit.FromHours((float)durationHours);
                eventSystem.StartTimer(text, duration);
                eventSystem.RemoveEventAfterTimer(text, eventID);
                EventGeoFactionObjective eventGeoFactionObjective = controller.PhoenixFaction.Objectives.OfType<EventGeoFactionObjective>().FirstOrDefault((EventGeoFactionObjective o) => o.EventID == eventID);
                if (eventGeoFactionObjective == null)
                {
                    eventGeoFactionObjective = new EventGeoFactionObjective(controller.PhoenixFaction, eventID)
                    {
                        Title = eventByID.GeoscapeEventData.Title,
                        Description = eventByID.GeoscapeEventData.Summary,
                        IsCriticalPath = eventByID.GeoscapeEventData.IsCriticalPath
                    };
                    controller.PhoenixFaction.AddObjective(eventGeoFactionObjective);
                }
                site.DiplomaticObjectiveFaction = controller.PhoenixFaction;

                return text;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }



        private static void ScheduleNextRoll(GeoLevelController geoLevelController, int currentHour)
        {
            int hoursUntilNextRoll = UnityEngine.Random.Range(MinRollHours, MaxRollHours + 1);
            geoLevelController.EventSystem.SetVariable(IncidentNextRollHourVariable, currentHour + hoursUntilNextRoll);
            TFTVLogger.Always($"[Incidents] Next roll scheduled in {hoursUntilNextRoll} hours.");
        }

        private static List<GeoHaven> GetVisitedHavensWithoutActiveMission(GeoLevelController geoLevelController, GeoPhoenixFaction phoenixFaction)
        {
            return geoLevelController.Map.AllSites
                .Select(site => site?.GetComponent<GeoHaven>())
                .Where(haven => haven?.Site != null
                    && haven.Site.ActiveMission == null
                    && haven.Site.State == PhoenixPoint.Common.Core.GeoSiteState.Functioning
                    && haven.Site.GetVisited(phoenixFaction))
                .ToList();
        }

        private static List<IncidentCandidate> GetIncidentCandidates(
            GeoLevelController geoLevelController,
            GeoPhoenixFaction phoenixFaction,
            List<GeoHaven> visitedHavens)
        {
            List<IncidentCandidate> candidates = new List<IncidentCandidate>();

            foreach (Objects.GeoIncidentDefinition incident in GeoscapeEvents.IncidentDefinitions)
            {
                if (incident == null || incident.IntroEvent == null)
                {
                    continue;
                }

                if (IsIncidentRolled(geoLevelController, incident))
                {
                    continue;
                }

                List<GeoHaven> eligibleHavens = new List<GeoHaven>();
                foreach (GeoHaven haven in visitedHavens)
                {
                    GeoFaction owner = haven.Site.Owner;
                    if (incident.FactionDef != null && (owner == null || owner.Def != incident.FactionDef))
                    {
                        continue;
                    }

                    if (incident.IsEligibleFor(haven, phoenixFaction))
                    {
                        eligibleHavens.Add(haven);
                    }
                }

                if (eligibleHavens.Count > 0)
                {
                    candidates.Add(new IncidentCandidate(incident, eligibleHavens));
                }
            }

            return candidates;
        }

        private static IncidentCandidate ChooseIncidentCandidate(List<IncidentCandidate> candidates)
        {
            int totalWeight = candidates.Sum(candidate => candidate.Weight);
            if (totalWeight <= 0)
            {
                return candidates.GetRandomElement();
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (IncidentCandidate candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0)
                {
                    return candidate;
                }
            }

            return candidates.GetRandomElement();
        }

        private static bool IsIncidentRolled(GeoLevelController geoLevelController, Objects.GeoIncidentDefinition incident)
        {
            string variableName = IncidentRolledVariablePrefix + incident.Id;
            return geoLevelController.EventSystem.GetVariable(variableName) > 0;
        }

        private static void MarkIncidentRolled(GeoLevelController geoLevelController, Objects.GeoIncidentDefinition incident)
        {
            string variableName = IncidentRolledVariablePrefix + incident.Id;
            geoLevelController.EventSystem.SetVariable(variableName, 1);
        }

        private sealed class IncidentCandidate
        {
            public Objects.GeoIncidentDefinition Definition { get; }
            public List<GeoHaven> EligibleHavens { get; }
            public int Weight { get; }

            public IncidentCandidate(Objects.GeoIncidentDefinition definition, List<GeoHaven> eligibleHavens)
            {
                Definition = definition;
                EligibleHavens = eligibleHavens;
                Weight = Math.Max(definition.Priority, 1);
            }
        }
    }
}
