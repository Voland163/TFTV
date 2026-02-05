using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TFTV.TFTVIncidents
{
    internal class GeoscapeEvents
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly GeoFactionDef PhoenixPointFactionDef = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
        private static readonly GeoFactionDef AnuFactionDef = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
        private static readonly GeoFactionDef NewJerichoFactionDef = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
        private static readonly GeoFactionDef SynedrionFactionDef = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

        private static readonly string[] IncidentLocalizationFiles =
        {
            "TFTV_Incidents_Anu_Localization.csv",
            "TFTV_Incidents_NJ_Localization.csv",
            "TFTV_Incidents_SY_Localization.csv"
        };

        public static readonly List<Objects.GeoIncidentDefinition> IncidentDefinitions = new List<Objects.GeoIncidentDefinition>();

        public static void CreateGeoscapeEvents()
        {
            try
            {
                IncidentDefinitions.Clear();

                List<IncidentKeys> incidents = LoadIncidentKeys();
                foreach (IncidentKeys incident in incidents)
                {
                    GeoscapeEventDef introEvent = CreateIntroEvent(incident);
                    GeoscapeEventDef choiceASuccess = CreateOutcomeEvent(incident, 0, true);
                    GeoscapeEventDef choiceAFailure = CreateOutcomeEvent(incident, 0, false);
                    GeoscapeEventDef choiceBSuccess = CreateOutcomeEvent(incident, 1, true);
                    GeoscapeEventDef choiceBFailure = CreateOutcomeEvent(incident, 1, false);

                    IncidentDefinitions.Add(new Objects.GeoIncidentDefinition
                    {
                        Id = incident.Id,
                        IntroEvent = introEvent,
                        ChoiceAResolutionSuccess = choiceASuccess,
                        ChoiceAResolutionFailure = choiceAFailure,
                        ChoiceBResolutionSuccess = choiceBSuccess,
                        ChoiceBResolutionFailure = choiceBFailure,
                        FactionDef = GetFactionDef(incident.FactionSuffix)
                    });

                    ApplyIncidentRequirements(incident);
                }

                LogIncidentSummary(incidents);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static GeoFactionDef GetFactionDef(string factionSuffix)
        {
            switch (factionSuffix)
            {
                case "AN":
                    return AnuFactionDef;
                case "NJ":
                    return NewJerichoFactionDef;
                case "SY":
                    return SynedrionFactionDef;
                default:
                    return null;
            }
        }

        public static void AddIncidentEligibilityCondition(int incidentId, Objects.GeoIncidentEligibilityCondition condition)
        {
            try
            {
                if (condition == null)
                {
                    TFTVLogger.Always("[Incidents] Eligibility condition is null.");
                    return;
                }

                Objects.GeoIncidentDefinition incident = GetIncidentDefinition(incidentId);
                if (incident == null)
                {
                    TFTVLogger.Always($"[Incidents] Incident not found: {incidentId}.");
                    return;
                }

                incident.EligibilityConditions.Add(condition);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static Objects.GeoIncidentDefinition GetIncidentDefinition(int incidentId)
        {
            if (IncidentDefinitions.Count == 0)
            {
                TFTVLogger.Always("[Incidents] Incident definitions are not initialized.");
                return null;
            }

            return IncidentDefinitions.FirstOrDefault(i => i.Id == incidentId);
        }

        private static List<IncidentKeys> LoadIncidentKeys()
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Regex titleRegex = new Regex(@"^TFTV_INCIDENT_(\d+)_TITLE_(AN|NJ|SY)$", RegexOptions.IgnoreCase);
            Dictionary<string, IncidentKeys> incidents = new Dictionary<string, IncidentKeys>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in IncidentLocalizationFiles)
            {
                string path = Path.Combine(TFTVMain.LocalizationDirectory, file);
                if (!File.Exists(path))
                {
                    TFTVLogger.Always($"[Incidents] Missing localization file: {path}");
                    continue;
                }

                foreach (string record in ReadCsvRecords(path))
                {
                    List<string> columns = ParseCsvColumns(record);
                    if (columns.Count == 0)
                    {
                        continue;
                    }

                    string key = columns[0].Trim();
                    if (string.IsNullOrEmpty(key) || key.Equals("Key", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    keys.Add(key);

                    Match match = titleRegex.Match(key);
                    if (!match.Success)
                    {
                        continue;
                    }

                    int id = int.Parse(match.Groups[1].Value);
                    string factionSuffix = match.Groups[2].Value.ToUpperInvariant();
                    string incidentKey = $"{id}_{factionSuffix}";
                    string requirementsText = columns.Count > 2 ? columns[2].Trim() : string.Empty;

                    incidents[incidentKey] = new IncidentKeys
                    {
                        Id = id,
                        FactionSuffix = factionSuffix,
                        TitleKey = key,
                        DescriptionKey = $"TFTV_INCIDENT_{id}_DESC",
                        RequirementsText = requirementsText
                    };
                }
            }

            foreach (IncidentKeys incident in incidents.Values)
            {
                incident.ChoiceAKey = keys.FirstOrDefault(k =>
                    k.StartsWith($"TFTV_INCIDENT_{incident.Id}_CHOICE_0_", StringComparison.OrdinalIgnoreCase));

                incident.ChoiceBKey = keys.FirstOrDefault(k =>
                    k.StartsWith($"TFTV_INCIDENT_{incident.Id}_CHOICE_1_", StringComparison.OrdinalIgnoreCase));

                incident.CancelChoiceKey = keys.FirstOrDefault(k =>
                    k.Equals($"TFTV_INCIDENT_{incident.Id}_CHOICE_2", StringComparison.OrdinalIgnoreCase));

                incident.CancelOutcomeKey = keys.FirstOrDefault(k =>
                    k.Equals($"TFTV_INCIDENT_{incident.Id}_OUTCOME_C", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(incident.ChoiceAKey) || string.IsNullOrEmpty(incident.ChoiceBKey))
                {
                    TFTVLogger.Always($"[Incidents] Missing choice keys for incident {incident.Id} ({incident.FactionSuffix}).");
                }
            }

            return incidents.Values
                .Where(i => !string.IsNullOrEmpty(i.ChoiceAKey) && !string.IsNullOrEmpty(i.ChoiceBKey))
                .OrderBy(i => i.Id)
                .ToList();
        }

        private static IEnumerable<string> ReadCsvRecords(string path)
        {
            string[] lines = File.ReadAllLines(path);
            StringBuilder current = new StringBuilder();
            int quoteCount = 0;

            foreach (string line in lines)
            {
                if (current.Length > 0)
                {
                    current.Append("\n");
                }
                current.Append(line);
                quoteCount += line.Count(c => c == '"');

                if (quoteCount % 2 == 0)
                {
                    yield return current.ToString();
                    current.Length = 0;
                    quoteCount = 0;
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }

        private static List<string> ParseCsvColumns(string line)
        {
            List<string> columns = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                return columns;
            }

            StringBuilder value = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    columns.Add(value.ToString());
                    value.Length = 0;
                    continue;
                }

                value.Append(c);
            }

            columns.Add(value.ToString());
            return columns;
        }

        private static GeoscapeEventDef CreateIntroEvent(IncidentKeys incident)
        {
            string eventName = $"TFTV_INCIDENT_{incident.Id}_{incident.FactionSuffix}_INTRO";
            GeoscapeEventDef introEvent = TFTVCommonMethods.CreateNewEvent(eventName, incident.TitleKey, incident.DescriptionKey, null);

            GeoEventChoice choiceA = introEvent.GeoscapeEventData.Choices[0];
            choiceA.Text.LocalizationKey = incident.ChoiceAKey;

            if (choiceA.Outcome?.OutcomeText?.General != null)
            {
                choiceA.Outcome.OutcomeText.General.LocalizationKey = string.Empty;
            }

            TFTVCommonMethods.GenerateGeoEventChoice(introEvent, incident.ChoiceBKey, string.Empty);

            if (!string.IsNullOrEmpty(incident.CancelChoiceKey))
            {
                string cancelOutcomeKey = string.IsNullOrEmpty(incident.CancelOutcomeKey) ? string.Empty : incident.CancelOutcomeKey;
                TFTVCommonMethods.GenerateGeoEventChoice(introEvent, incident.CancelChoiceKey, cancelOutcomeKey);
            }

            return introEvent;
        }

        private static GeoscapeEventDef CreateOutcomeEvent(IncidentKeys incident, int choiceIndex, bool isSuccess)
        {
            string resultToken = isSuccess ? "SUCCESS" : "FAIL";
            string eventName = $"TFTV_INCIDENT_{incident.Id}_{incident.FactionSuffix}_OUTCOME_{choiceIndex}_{resultToken}";
            string outcomeKey = $"TFTV_INCIDENT_{incident.Id}_OUTCOME_{choiceIndex}_{resultToken}";

            GeoscapeEventDef outcomeEvent = TFTVCommonMethods.CreateNewEvent(eventName, incident.TitleKey, outcomeKey, null);
            ApplyOutcomeRewards(outcomeEvent, incident, outcomeKey);
            return outcomeEvent;
        }

        private const string SameHavenSiteTagPrefix = "TFTV_INCIDENT_SAME_HAVEN_";

        internal static string GetSameHavenSiteTag(int incidentId, string factionSuffix)
        {
            return $"{SameHavenSiteTagPrefix}{incidentId}_{factionSuffix}";
        }

        internal static void AddSameHavenSiteTag(GeoSite site, int incidentId, string factionSuffix)
        {
            if (site?.SiteTags == null)
            {
                return;
            }

            string tag = GetSameHavenSiteTag(incidentId, factionSuffix);
            if (!site.SiteTags.Contains(tag))
            {
                site.SiteTags.Add(tag);
            }
        }

        private static string GetSameHavenSiteTag(IncidentKeys incident)
        {
            return incident == null ? string.Empty : GetSameHavenSiteTag(incident.Id, incident.FactionSuffix);
        }

        private static void ApplyOutcomeRewards(GeoscapeEventDef outcomeEvent, IncidentKeys incident, string outcomeKey)
        {
            if (!TryPrepareOutcome(outcomeEvent, out GeoEventChoiceOutcome outcome))
            {
                return;
            }

            switch (incident.FactionSuffix)
            {
                case "AN":
                    ApplyOutcomeRewardsAnu(outcomeKey, outcome);
                    break;
                case "NJ":
                    ApplyOutcomeRewardsNj(outcomeKey, outcome);
                    break;
                case "SY":
                    ApplyOutcomeRewardsSy(outcomeKey, outcome);
                    break;
            }
        }

        private static bool TryPrepareOutcome(GeoscapeEventDef outcomeEvent, out GeoEventChoiceOutcome outcome)
        {
            outcome = null;
            if (outcomeEvent?.GeoscapeEventData?.Choices == null || outcomeEvent.GeoscapeEventData.Choices.Count == 0)
            {
                return false;
            }

            outcome = outcomeEvent.GeoscapeEventData.Choices[0].Outcome;
            if (outcome == null)
            {
                return false;
            }

            if (outcome.Resources == null)
            {
                outcome.Resources = new ResourcePack();
            }

            if (outcome.Diplomacy == null)
            {
                outcome.Diplomacy = new List<OutcomeDiplomacyChange>();
            }

            if (outcome.CustomCharacters == null)
            {
                outcome.CustomCharacters = new List<TacCharacterDef>();
            }

            if (outcome.VariablesChange == null)
            {
                outcome.VariablesChange = new List<OutcomeVariableChange>();
            }

            return true;
        }

        private static void AddVariableChange(GeoEventChoiceOutcome outcome, string variableName, int value, bool isSet)
        {
            if (outcome?.VariablesChange == null)
            {
                return;
            }

            outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange(variableName, value, isSet));
        }

        private static void ApplyOutcomeRewardsAnu(string outcomeKey, GeoEventChoiceOutcome outcome)
        {
            switch (outcomeKey)
            {
                case "TFTV_INCIDENT_0_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_0_OUTCOME_1_SUCCESS":
                    TryAddCustomCharacter(outcome, GetIncidentRewardNahiaGrivane());
                    break;
                case "TFTV_INCIDENT_1_OUTCOME_0_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    break;
                case "TFTV_INCIDENT_1_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Mutagen, 200);
                    AddResource(outcome, ResourceType.Research, 200);
                    break;
                case "TFTV_INCIDENT_2_OUTCOME_0_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    break;
                case "TFTV_INCIDENT_2_OUTCOME_1_SUCCESS":
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 200);
                    break;
                case "TFTV_INCIDENT_3_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_3_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 5);
                    break;
                case "TFTV_INCIDENT_4_OUTCOME_0_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    AddResource(outcome, ResourceType.Supplies, 400);
                    break;
                case "TFTV_INCIDENT_4_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    TryAddCustomCharacter(outcome, GetIncidentRewardMutog());
                    break;
                case "TFTV_INCIDENT_5_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_5_OUTCOME_1_SUCCESS":
                    AddVariableChange(outcome, "Seeds_of_Reformation", 1, true);
                    break;
                case "TFTV_INCIDENT_6_OUTCOME_0_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 8);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 16);
                    AddResource(outcome, ResourceType.Supplies, 600);
                    break;
                case "TFTV_INCIDENT_6_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 8);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 16);
                    AddResource(outcome, ResourceType.Mutagen, 400);
                    break;
                case "TFTV_INCIDENT_7_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_7_OUTCOME_1_SUCCESS":
                    TryAddCustomCharacter(outcome, GetIncidentRewardMutantOutcast());
                    break;
                case "TFTV_INCIDENT_8_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_8_OUTCOME_1_SUCCESS":
                    TryAddCustomCharacter(outcome, GetIncidentRewardInfiltratorLevel6());
                    break;
                case "TFTV_INCIDENT_10_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_10_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 200);
                    break;
                case "TFTV_INCIDENT_11_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_11_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 5);
                    AddResource(outcome, ResourceType.Tech, 100);
                    break;
                case "TFTV_INCIDENT_12_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_12_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    AddResource(outcome, ResourceType.Supplies, 400);
                    break;
                case "TFTV_INCIDENT_13_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_13_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Research, 300);
                    break;
                case "TFTV_INCIDENT_14_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_14_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, AnuFactionDef, 4);
                    AddLeaderDiplomacy(outcome, AnuFactionDef, 8);
                    AddResource(outcome, ResourceType.Supplies, 300);
                    break;
            }
        }

        private static void ApplyOutcomeRewardsNj(string outcomeKey, GeoEventChoiceOutcome outcome)
        {
            switch (outcomeKey)
            {
                case "TFTV_INCIDENT_15_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_15_OUTCOME_1_SUCCESS":
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 12);
                    AddResource(outcome, ResourceType.Materials, 600);
                    break;
                case "TFTV_INCIDENT_16_OUTCOME_0_SUCCESS":
                    AddResource(outcome, ResourceType.Materials, 300);
                    AddResource(outcome, ResourceType.Supplies, 300);
                    break;
                case "TFTV_INCIDENT_16_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Tech, 100);
                    AddResource(outcome, ResourceType.Research, 200);
                    break;
                case "TFTV_INCIDENT_17_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Tech, 250);
                    break;
                case "TFTV_INCIDENT_18_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_18_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_20_OUTCOME_0_SUCCESS":
                    AddResource(outcome, ResourceType.Orichalcum, 150);
                    break;
                case "TFTV_INCIDENT_20_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Orichalcum, 250);
                    break;
                case "TFTV_INCIDENT_21_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_21_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_22_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_22_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_23_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_24_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_24_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_25_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_25_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_26_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_26_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 400);
                    break;
                case "TFTV_INCIDENT_27_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_27_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, NewJerichoFactionDef, 4);
                    AddLeaderDiplomacy(outcome, NewJerichoFactionDef, 8);
                    AddResource(outcome, ResourceType.Materials, 400);
                    break;
                case "TFTV_INCIDENT_28_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_28_OUTCOME_1_SUCCESS":
                    AddResource(outcome, ResourceType.Research, 300);
                    break;
            }
        }

        private static void ApplyOutcomeRewardsSy(string outcomeKey, GeoEventChoiceOutcome outcome)
        {
            switch (outcomeKey)
            {
                case "TFTV_INCIDENT_30_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_30_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Tech, 80);
                    break;
                case "TFTV_INCIDENT_31_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_31_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacy(outcome, AnuFactionDef, SynedrionFactionDef, 5);
                    AddFactionDiplomacy(outcome, SynedrionFactionDef, AnuFactionDef, 5);
                    AddResource(outcome, ResourceType.Supplies, 200);
                    AddResource(outcome, ResourceType.Tech, 40);
                    break;
                case "TFTV_INCIDENT_33_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_33_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Tech, 80);
                    break;
                case "TFTV_INCIDENT_35_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_35_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 500);
                    break;
                case "TFTV_INCIDENT_38_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_38_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Tech, 80);
                    break;
                case "TFTV_INCIDENT_39_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_39_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Tech, 80);
                    break;
                case "TFTV_INCIDENT_40_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_40_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Tech, 80);
                    break;
                case "TFTV_INCIDENT_41_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_41_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 500);
                    break;
                case "TFTV_INCIDENT_44_OUTCOME_0_SUCCESS":
                case "TFTV_INCIDENT_44_OUTCOME_1_SUCCESS":
                    AddFactionDiplomacyToPhoenix(outcome, SynedrionFactionDef, 4);
                    AddLeaderDiplomacy(outcome, SynedrionFactionDef, 8);
                    AddResource(outcome, ResourceType.Research, 500);
                    break;
            }
        }

        private static void AddFactionDiplomacy(GeoEventChoiceOutcome outcome, GeoFactionDef partyFaction, GeoFactionDef targetFaction, int value)
        {
            if (outcome?.Diplomacy == null || partyFaction == null || targetFaction == null)
            {
                return;
            }

            outcome.Diplomacy.Add(new OutcomeDiplomacyChange
            {
                PartyFaction = partyFaction,
                TargetFaction = targetFaction,
                PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                Value = value
            });
        }

        private static void AddResource(GeoEventChoiceOutcome outcome, ResourceType type, int value)
        {
            if (outcome?.Resources == null)
            {
                return;
            }

            outcome.Resources.Add(new ResourceUnit
            {
                Type = type,
                Value = value
            });
        }

        private static void AddFactionDiplomacyToPhoenix(GeoEventChoiceOutcome outcome, GeoFactionDef faction, int value)
        {
            if (outcome?.Diplomacy == null || faction == null || PhoenixPointFactionDef == null)
            {
                return;
            }

            outcome.Diplomacy.Add(new OutcomeDiplomacyChange
            {
                PartyFaction = faction,
                TargetFaction = PhoenixPointFactionDef,
                PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                Value = value
            });
        }

        private static void AddLeaderDiplomacy(GeoEventChoiceOutcome outcome, GeoFactionDef faction, int value)
        {
            if (outcome?.Diplomacy == null)
            {
                return;
            }

            OutcomeDiplomacyChange change = CreateLeaderDiplomacyChange(faction, value);
            

            outcome.Diplomacy.Add(change);
        }

        private static OutcomeDiplomacyChange CreateLeaderDiplomacyChange(GeoFactionDef faction, int value)
        {
            
            OutcomeDiplomacyChange.ChangeTarget leaderTarget = ResolveLeaderChangeTarget();

            return new OutcomeDiplomacyChange
            {
                PartyFaction = faction,
                TargetFaction = PhoenixPointFactionDef,
                PartyType = leaderTarget,
                Value = value
            };
        }

        private static OutcomeDiplomacyChange.ChangeTarget ResolveLeaderChangeTarget()
        {
            foreach (OutcomeDiplomacyChange.ChangeTarget value in Enum.GetValues(typeof(OutcomeDiplomacyChange.ChangeTarget)))
            {
                string name = Enum.GetName(typeof(OutcomeDiplomacyChange.ChangeTarget), value);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (name.IndexOf("Leader", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Haven", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return value;
                }
            }

            return (OutcomeDiplomacyChange.ChangeTarget)1;
        }

        private static void TryAddCustomCharacter(GeoEventChoiceOutcome outcome, TacCharacterDef characterDef)
        {
            if (outcome == null || characterDef == null)
            {
                return;
            }

            outcome.CustomCharacters.Add(characterDef);
        }

        private static TacCharacterDef GetIncidentRewardNahiaGrivane()
        {
            TacCharacterDef source = DefCache.GetDef<TacCharacterDef>("S_SY_Eileen_CharacterTemplateDef");
            TacCharacterDef nahia = Helper.CreateDefFromClone(source, "{C40430EB-1A4E-4B0C-B148-F57EB9939628}", "TFTV_NahiaGrivane_CharacterDef");

            nahia.Data.Name = "Nahia Grivane";
            nahia.Data.LocalizeName = false;

            List <GameTagDef> tagDefs =  nahia.Data.GameTags.ToList();

            tagDefs.Remove(DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef"));
            tagDefs.Add(DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef"));

            nahia.Data.GameTags = tagDefs.ToArray();

            nahia.Data.BodypartItems = new ItemDef[]
            {
                    DefCache.GetDef<TacticalItemDef>("AN_Priest_Torso_BodyPartDef"),
                    DefCache.GetDef<TacticalItemDef>("AN_Priest_Legs_ItemDef"),
            };

            nahia.Data.EquipmentItems = new ItemDef[]
            {
                    DefCache.GetDef<WeaponDef>("AN_Redemptor_WeaponDef"),
                    DefCache.GetDef<TacticalItemDef>("AN_Redemptor_AmmoClip_ItemDef"),
                    DefCache.GetDef<TacticalItemDef>("AN_Redemptor_AmmoClip_ItemDef"),
            };

            nahia.Data.InventoryItems = new ItemDef[]
            {
                   
            };

            return nahia;
        }

        private static TacCharacterDef GetIncidentRewardInfiltratorLevel6()
        {
            return DefCache.GetDef<TacCharacterDef>("SY_Infiltrator6_CharacterTemplateDef");
        }

        private static TacCharacterDef GetIncidentRewardMutantOutcast()
        {
            return null;
        }

        private static TacCharacterDef GetIncidentRewardMutog()
        {
          return DefCache.GetDef<TacCharacterDef>("AN_Mutog_RamAgileBasher_CharacterTemplateDef");
        }

        private static void ApplyIncidentRequirements(IncidentKeys incident)
        {
            if (incident == null)
            {
                return;
            }

            switch (incident.FactionSuffix)
            {
                case "AN":
                    ApplyIncidentRequirementsAnu(incident);
                    break;
                case "NJ":
                    ApplyIncidentRequirementsNj(incident);
                    break;
                case "SY":
                    ApplyIncidentRequirementsSy(incident);
                    break;
            }
        }

        private static void ApplyIncidentRequirementsAnu(IncidentKeys incident)
        {
            switch (incident.Id)
            {
                case 0:
                    RequireZone(incident, "TrainingElite_GeoHavenZoneDef");
                    RequireLeaderRelationToPhoenix(incident, 24);
                    break;
                case 3:
                    RequireFactionRelationToPhoenix(incident, AnuFactionDef, 24);
                    break;
                case 4:
                    RequireResearch(incident, "ANU_MutogTech_ResearchDef");
                    RequireFactionRelationToPhoenix(incident, AnuFactionDef, 24);
                    RequireZone(incident, "Residential_GeoHavenZoneDef");
                    RequireZone(incident, "ResidentialElite_GeoHavenZoneDef");
                    break;
                case 5:
                    RequirePopulation(incident, 25000);
                    break;
                case 6:
                    RequireVariableEquals(incident, "Seeds_of_Reformation", 1);
                    RequireSameHavenFromIncident(incident, 5);
                    break;
                case 7:
                    RequireFactionRelationToPhoenix(incident, AnuFactionDef, 24);
                    RequireSurvivedAttack(incident, ForsakenFactionShortName);
                    break;
                case 8:
                    RequireZone(incident, "Research_GeoHavenZoneDef");
                    RequireFactionRelationToPhoenix(incident, AnuFactionDef, 74);
                    break;
                case 9:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition
                        {
                            RequiredSiteTag = TFTVInfestation.GetLiberatedInfestedHavenTag(SynedrionFactionDef.PPFactionDef.ShortName)
                        }
                    });
                    break;
                case 11:
                    RequireFactionRelationToPhoenix(incident, SynedrionFactionDef, 49);
                    break;
                case 12:
                    RequireSurvivedAttack(incident, AlienFactionShortName);
                    break;
                case 13:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition
                        {
                            RequiredFaction = SynedrionFactionDef,
                            RequiredZoneDefName = "MistRepeller_GeoHavenZoneDef"
                        }
                    });
                    RequireHavenInMist(incident);
                    break;
                case 14:
                    RequirePopulation(incident, 10000);
                    RequireZone(incident, "TrainingElite_GeoHavenZoneDef");
                    break;
            }
        }

        private const string ForsakenFactionShortName = "FO";
        private const string AlienFactionShortName = "ALN";

        private static void RequireSurvivedAttack(IncidentKeys incident, string attackerShortName)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredSiteTag = TFTVInfestation.GetAttackHavenTag(attackerShortName),
                RequireNotDestroyed = true,
                RequireNotInfested = true
            });
        }

        private static void RequireNearbyDestroyedHaven(IncidentKeys incident)
        {
            RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
            {
                new Objects.GeoIncidentEligibilityCondition
                {
                    RequireDestroyedSite = true
                }
            });
        }

        private static void RequireHavenInMist(IncidentKeys incident)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequireHavenInMist = true
            });
        }
        private static void RequireSameHavenFromIncident(IncidentKeys incident, int sourceIncidentId)
        {
            if (incident == null)
            {
                return;
            }

            string tag = GetSameHavenSiteTag(sourceIncidentId, incident.FactionSuffix);
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredSiteTag = tag
            });
        }

        private static void ApplyIncidentRequirementsNj(IncidentKeys incident)
        {
            switch (incident.Id)
            {
                case 15:
                    RequireLeaderRelationToPhoenix(incident, 50);
                    break;
                case 16:
                    RequireVariableEquals(incident, "Melquiades", 1);
                    break;
                case 17:
                    RequireCharacterBackground(incident, NewJerichoFactionDef);
                    break;
                case 18:
                    RequireZone(incident, "Training_GeoHavenZoneDef");
                    break;
                case 20:
                    RequireLeaderRelationToPhoenix(incident, 50);
                    RequireResearch(incident, "ExoticMaterialsResearch");
                    break;
                case 21:
                    RequireZone(incident, "Factory_GeoHavenZoneDef");
                    break;
                case 22:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition { RequiredFaction = SynedrionFactionDef }
                    });
                    break;
                case 23:
                    RequireSameHavenFromIncident(incident, 19);
                    break;
                case 24:
                    RequireNearbyDestroyedHaven(incident);
                    break;
                case 26:
                    RequireZone(incident, "SatelliteUplink_GeoHavenZoneDef");
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition
                        {
                            RequiredSiteTagPrefix = TFTVInfestation.GetInfestedHavenTag(string.Empty)
                        }
                    });
                    break;
                case 27:
                    RequireSurvivedAttack(incident, AlienFactionShortName);
                    break;
                case 28:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition
                        {
                            RequiredSiteTag = TFTVInfestation.GetLiberatedInfestedHavenTag(NewJerichoFactionDef.PPFactionDef.ShortName)
                        }
                    });
                    break;
                case 29:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition { RequiredZoneDefName = "Research_GeoHavenZoneDef" },
                        new Objects.GeoIncidentEligibilityCondition { RequiredZoneDefName = "TrainingElite_GeoHavenZoneDef" }
                    });
                    break;
            }
        }

        private static void ApplyIncidentRequirementsSy(IncidentKeys incident)
        {
            switch (incident.Id)
            {
                case 31:
                    RequireFactionRelationToPhoenix(incident, AnuFactionDef, 49);
                    RequireFactionRelationToPhoenix(incident, SynedrionFactionDef, 49);
                    break;
                case 33:
                    RequireZone(incident, "Research_GeoHavenZoneDef");
                    break;
                case 37:
                    RequireCharacterBackground(incident, SynedrionFactionDef);
                    break;
                case 39:
                    RequireZone(incident, "MistRepeller_GeoHavenZoneDef");
                    break;
                case 41:
                    RequireVariableComparisonToVariable(incident, "Polyphonic", "Terraformers",
                        GeoEventVariationConditionDef.ComparisonOperator.GreaterOrEqual);
                    break;
                case 42:
                    RequireStarvingHaven(incident);
                    break;
                case 43:
                    RequireNearbyHaven(incident, new List<Objects.GeoIncidentEligibilityCondition>
                    {
                        new Objects.GeoIncidentEligibilityCondition
                        {
                            RequiredSiteTag = TFTVInfestation.GetLiberatedInfestedHavenTag(AnuFactionDef.PPFactionDef.ShortName)
                        }
                    });
                    break;
            }
        }

        private static void RequireStarvingHaven(IncidentKeys incident)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequireStarvingHaven = true
            });
        }

        private static void RequireLeaderRelationToPhoenix(IncidentKeys incident, int threshold)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                LeaderRelationToPhoenixComparison = GeoEventVariationConditionDef.ComparisonOperator.GreaterOrEqual,
                LeaderRelationToPhoenixThreshold = threshold
            });
        }

        private static void RequireFactionRelationToPhoenix(IncidentKeys incident, GeoFactionDef faction, int threshold)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                FactionRelationToPhoenixComparison = GeoEventVariationConditionDef.ComparisonOperator.GreaterOrEqual,
                FactionRelationToPhoenixThreshold = threshold,
                RequiredFaction = faction
            });
        }

        private static void RequirePopulation(IncidentKeys incident, int threshold)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                PopulationComparison = GeoEventVariationConditionDef.ComparisonOperator.GreaterOrEqual,
                PopulationThreshold = threshold
            });
        }

        private static void RequireResearch(IncidentKeys incident, string researchId)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredResearchID = researchId
            });
        }

        private static void RequireZone(IncidentKeys incident, string zoneDefName)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredZoneDefName = zoneDefName
            });
        }

        private static void RequireCharacterBackground(IncidentKeys incident, GeoFactionDef faction)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredCharacterBackgroundFaction = faction
            });
        }

        private static void RequireVariableEquals(IncidentKeys incident, string variableName, int value)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredVariableName = variableName,
                VariableComparison = GeoEventVariationConditionDef.ComparisonOperator.Equal,
                VariableThreshold = value
            });
        }

        private static void RequireVariableComparisonToVariable(
            IncidentKeys incident,
            string variableNameA,
            string variableNameB,
            GeoEventVariationConditionDef.ComparisonOperator comparison)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                RequiredVariableName = variableNameA,
                RequiredVariableNameB = variableNameB,
                VariableComparisonToVariable = comparison
            });
        }

        private static void RequireNearbyHaven(IncidentKeys incident, List<Objects.GeoIncidentEligibilityCondition> conditions)
        {
            AddIncidentEligibilityCondition(incident.Id, new Objects.GeoIncidentEligibilityCondition
            {
                NearbyHavenRange = EarthUnits.One,
                NearbyHavenConditions = conditions
            });
        }

        private static void LogRequirementTodo(IncidentKeys incident, string message)
        {
            TFTVLogger.Always($"[Incidents] TODO: {message} (incident {incident.Id} {incident.FactionSuffix}).");
        }

        private static void LogIncidentSummary(List<IncidentKeys> incidents)
        {
            TFTVLogger.Always($"[Incidents] Created {incidents.Count} incident definitions.");

            foreach (IncidentKeys incident in incidents)
            {
                string approachesA = FormatApproaches(incident.ChoiceAKey, 0);
                string approachesB = FormatApproaches(incident.ChoiceBKey, 1);
                TFTVLogger.Always($"[Incidents] {incident.Id}_{incident.FactionSuffix} -> ChoiceA: {approachesA} | ChoiceB: {approachesB}");
            }
        }

        private static string FormatApproaches(string choiceKey, int choiceIndex)
        {
            if (string.IsNullOrEmpty(choiceKey))
            {
                return "None";
            }

            int index = choiceKey.IndexOf("_CHOICE_" + choiceIndex + "_", StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return "None";
            }

            string suffix = choiceKey.Substring(index + ("_CHOICE_" + choiceIndex + "_").Length);
            string[] tokens = suffix.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> names = new List<string>();
            foreach (string token in tokens)
            {
                switch (token.ToUpperInvariant())
                {
                    case "P":
                        names.Add("Psycho-Sociology");
                        break;
                    case "E":
                        names.Add("Exploration");
                        break;
                    case "O":
                        names.Add("Occult");
                        break;
                    case "B":
                        names.Add("Biotech");
                        break;
                    case "M":
                        names.Add("Machinery");
                        break;
                    case "C":
                        names.Add("Compute");
                        break;
                }
            }

            return names.Count == 0 ? "None" : string.Join(" / ", names);
        }

        private class IncidentKeys
        {
            public int Id;
            public string FactionSuffix;
            public string TitleKey;
            public string DescriptionKey;
            public string ChoiceAKey;
            public string ChoiceBKey;
            public string CancelChoiceKey;
            public string CancelOutcomeKey;
            public string RequirementsText;
        }
    }
}
