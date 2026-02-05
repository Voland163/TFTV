using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    internal static class IncidentOutcomeSummaryUI
    {
        private const string SummaryRootName = "[Mod]IncidentOutcomeSummaryRoot";
        private const string SummaryTextName = "[Mod]IncidentOutcomeSummaryText";
        private const string DiagTag = "[Incidents][OutcomeRecapDiag]";
        private const string AffinityRewardLineName = "[Mod]IncidentAffinityRewardLine";
        private const string PersonnelRewardLineName = "[Mod]IncidentPersonnelRewardLine";

        private static readonly Dictionary<string, SummaryData> SummaryByExactKey = new Dictionary<string, SummaryData>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, SummaryData> SummaryBySiteKey = new Dictionary<string, SummaryData>(StringComparer.OrdinalIgnoreCase);

        private sealed class SummaryData
        {
            public string EventId;
            public int SiteId;
            public int VehicleId;
            public string IntroText;
            public string ChosenApproach;
            public string LeaderName;
            public int PersonnelCount;
            public string AffinityResultText;
            public DateTime CreatedUtc;
        }

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

        internal static void RecordIncidentSuccess(
            GeoLevelController level,
            GeoVehicle vehicle,
            Resolution.ActiveTimedProblem active,
            string affinityResultText)
        {
            try
            {
                if (active == null || string.IsNullOrEmpty(active.CompletionEventId) || !IsIncidentSuccessEvent(active.CompletionEventId))
                {
                    TFTVLogger.Always($"{DiagTag} RecordIncidentSuccess skipped. active null or not success event.");
                    return;
                }

                GeoCharacter leader = LeaderSelection.ResolveLeader(level, vehicle, active.LeaderId);

                SummaryData data = new SummaryData
                {
                    EventId = active.CompletionEventId,
                    SiteId = active.SiteId,
                    VehicleId = active.VehicleId,
                    IntroText = ResolveIntroText(active.CompletionEventId),
                    ChosenApproach = ResolveChosenApproachText(active.CompletionEventId, active.ApproachTokens),
                    LeaderName = leader != null ? leader.DisplayName : "Unknown",
                    PersonnelCount = ResolvePersonnelCount(active.CompletionEventId),
                    AffinityResultText = string.IsNullOrEmpty(affinityResultText) ? "No change" : affinityResultText,
                    CreatedUtc = DateTime.UtcNow
                };

                string exactKey = BuildExactKey(data.EventId, data.SiteId, data.VehicleId);
                string siteKey = BuildSiteKey(data.EventId, data.SiteId);

                TFTVLogger.Always($"{DiagTag} STORE event={data.EventId} site={data.SiteId} vehicle={data.VehicleId} leader={data.LeaderName} approach={data.ChosenApproach} affinity={data.AffinityResultText} personnel={data.PersonnelCount}");
                TFTVLogger.Always($"{DiagTag} STORE keys exact={exactKey} site={siteKey}");

                StoreSummary(data);
                CleanupOldSummaries();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetEncounter")]
        private static class UIModuleSiteEncounters_SetEncounter_OutcomeSummary_Patch
        {
            private static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, bool pagingEvent)
            {
                try
                {
                    if (__instance == null || geoEvent?.Context == null || pagingEvent)
                    {
                        return;
                    }

                    if (!IsIncidentSuccessEvent(geoEvent.EventID))
                    {
                        return;
                    }

                    TryRenderSummary(__instance, geoEvent);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetClosingEncounter")]
        private static class UIModuleSiteEncounters_SetClosingEncounter_OutcomeSummary_Patch
        {
            private static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, GeoEventChoice closingChoice, bool useEventTexts)
            {
                try
                {
                    if (__instance == null || geoEvent?.Context == null)
                    {
                        return;
                    }

                    if (!IsIncidentSuccessEvent(geoEvent.EventID))
                    {
                        return;
                    }

                    TryRenderSummary(__instance, geoEvent);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void TryRenderSummary(UIModuleSiteEncounters module, GeoscapeEvent geoEvent)
        {
            Transform parent = module.SiteEncounterTextContainer != null ? module.SiteEncounterTextContainer.transform : null;
            if (parent == null)
            {
                return;
            }

            RemoveExistingSummary(parent);
            RemoveExistingAffinityRewardLine(parent);
            RemoveExistingPersonnelRewardLine(parent);

            int siteId = geoEvent.Context.Site?.SiteId ?? -1;
            int vehicleId = geoEvent.Context.Vehicle?.VehicleID ?? -1;
            string eventId = geoEvent.EventID ?? string.Empty;

            if (!TryGetSummary(eventId, siteId, vehicleId, out SummaryData data))
            {
                string siteKey = BuildSiteKey(eventId, siteId);
                if (!SummaryBySiteKey.TryGetValue(siteKey, out data))
                {
                    return;
                }
            }

            string introText = data.IntroText ?? string.Empty;
            string chosenApproach = data.ChosenApproach ?? string.Empty;

            if (geoEvent.Context != null)
            {
                introText = geoEvent.Context.ReplaceEventTokens(introText);
                chosenApproach = geoEvent.Context.ReplaceEventTokens(chosenApproach);
            }

            string summaryBody =
                "<b>Incident Recap</b>\n" +
                $"Intro: {introText}\n" +
                $"Chosen approach: {chosenApproach}\n" +
                $"Leading operative: {data.LeaderName}";

            CreateOrUpdateSummaryPanel(parent, module, summaryBody);

            AddPersonnelToRewards(module, parent, data);
            AddAffinityToRewards(module, parent, data);
        }

        private static void AddAffinityToRewards(UIModuleSiteEncounters module, Transform rewardContainer, SummaryData data)
        {
            if (module == null || rewardContainer == null || data == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(data.AffinityResultText) || string.Equals(data.AffinityResultText, "No change", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (module.EncounterRewardTextPrefab == null)
            {
                return;
            }

            GameObject rewardGo = UnityEngine.Object.Instantiate(module.EncounterRewardTextPrefab, rewardContainer);
            rewardGo.name = AffinityRewardLineName;

            Text rewardText = rewardGo.GetComponent<Text>();
            if (rewardText == null)
            {
                return;
            }

            string valueText = data.AffinityResultText;
            if (!string.IsNullOrEmpty(module.PositiveRewardTextPattern) && module.PositiveRewardTextPattern.Contains("{0}"))
            {
                valueText = string.Format(module.PositiveRewardTextPattern, valueText);
            }

            rewardText.supportRichText = true;
            rewardText.text = "Affinity gained/increased: " + valueText;
        }

        private static void RemoveExistingAffinityRewardLine(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            List<Transform> toRemove = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child != null && child.name == AffinityRewardLineName)
                {
                    toRemove.Add(child);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                UnityEngine.Object.Destroy(toRemove[i].gameObject);
            }
        }

        private static void RemoveExistingPersonnelRewardLine(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            List<Transform> toRemove = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child != null && child.name == PersonnelRewardLineName)
                {
                    toRemove.Add(child);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                UnityEngine.Object.Destroy(toRemove[i].gameObject);
            }
        }

        

        private static bool IsIncidentSuccessEvent(string eventId)
        {
            return !string.IsNullOrEmpty(eventId)
                && eventId.StartsWith("TFTV_INCIDENT_", StringComparison.OrdinalIgnoreCase)
                && eventId.IndexOf("_OUTCOME_", StringComparison.OrdinalIgnoreCase) >= 0
                && eventId.EndsWith("_SUCCESS", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveIntroText(string successEventId)
        {
            if (TryGetIncidentDefinitionBySuccessEvent(successEventId, out Objects.GeoIncidentDefinition incident, out _))
            {
                string introKey = incident?.IntroEvent?.GeoscapeEventData?.Description != null
                    ? incident.IntroEvent.GeoscapeEventData.Description
                        .Select(v => v?.General?.LocalizationKey)
                        .FirstOrDefault(k => !string.IsNullOrEmpty(k))
                    : null;

                string localizedIntro = LocalizeKeySafe(introKey);
                if (!string.IsNullOrEmpty(localizedIntro))
                {
                    return localizedIntro;
                }
            }

            // Fallback: legacy key shape
            if (!TryParseIncidentOutcome(successEventId, out int incidentId, out string faction))
            {
                return "N/A";
            }

            string fallbackKey = $"TFTV_INCIDENT_{incidentId}_{faction}_DESC";
            string fallbackLocalized = LocalizeKeySafe(fallbackKey);
            return string.IsNullOrEmpty(fallbackLocalized) ? fallbackKey : fallbackLocalized;
        }

        private static string ResolveChosenApproachText(string successEventId, string approachTokens)
        {
            if (TryGetIncidentDefinitionBySuccessEvent(successEventId, out Objects.GeoIncidentDefinition incident, out int choiceIndex))
            {
                List<GeoEventChoice> choices = incident?.IntroEvent?.GeoscapeEventData?.Choices;
                if (choices != null && choiceIndex >= 0 && choiceIndex < choices.Count)
                {
                    string choiceKey = choices[choiceIndex]?.Text?.LocalizationKey;
                    string localizedChoice = LocalizeKeySafe(choiceKey);
                    if (!string.IsNullOrEmpty(localizedChoice))
                    {
                        return localizedChoice;
                    }
                }
            }

            // Fallback label
            string approachLabel = "Approach";
            if (TryParseChoiceIndex(successEventId, out int fallbackChoiceIndex))
            {
                approachLabel = fallbackChoiceIndex == 0 ? "Approach A" : (fallbackChoiceIndex == 1 ? "Approach B" : "Approach");
            }

            string tokens = FormatApproachTokens(approachTokens);
            return string.IsNullOrEmpty(tokens) ? approachLabel : $"{approachLabel} ({tokens})";
        }

        private static bool TryGetIncidentDefinitionBySuccessEvent(string successEventId, out Objects.GeoIncidentDefinition incident, out int choiceIndex)
        {
            incident = null;
            choiceIndex = -1;

            if (string.IsNullOrEmpty(successEventId) || GeoscapeEvents.IncidentDefinitions == null)
            {
                return false;
            }

            for (int i = 0; i < GeoscapeEvents.IncidentDefinitions.Count; i++)
            {
                Objects.GeoIncidentDefinition candidate = GeoscapeEvents.IncidentDefinitions[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.ChoiceAResolutionSuccess?.EventID, successEventId, StringComparison.OrdinalIgnoreCase))
                {
                    incident = candidate;
                    choiceIndex = 0;
                    return true;
                }

                if (string.Equals(candidate.ChoiceBResolutionSuccess?.EventID, successEventId, StringComparison.OrdinalIgnoreCase))
                {
                    incident = candidate;
                    choiceIndex = 1;
                    return true;
                }
            }

            return false;
        }

        private static string LocalizeKeySafe(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            try
            {
                string localized = TFTVCommonMethods.ConvertKeyToString(key);
                return string.IsNullOrEmpty(localized) || string.Equals(localized, key, StringComparison.Ordinal) ? string.Empty : localized;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int ResolvePersonnelCount(string eventId)
        {
            int count = 2;
            if (DoublePersonnelRewardEvents.Contains(eventId))
            {
                count *= 2;
            }

            return count;
        }

        private static string FormatApproachTokens(string approachTokens)
        {
            List<LeaderSelection.AffinityApproach> approaches = LeaderSelection.ParseApproachTokens(approachTokens);
            if (approaches == null || approaches.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", approaches.Select(a => a.ToString()).ToArray());
        }

        private static void StoreSummary(SummaryData data)
        {
            string exactKey = BuildExactKey(data.EventId, data.SiteId, data.VehicleId);
            string siteKey = BuildSiteKey(data.EventId, data.SiteId);

            SummaryByExactKey[exactKey] = data;
            SummaryBySiteKey[siteKey] = data;
        }

        private static bool TryGetSummary(string eventId, int siteId, int vehicleId, out SummaryData data)
        {
            string exactKey = BuildExactKey(eventId, siteId, vehicleId);
            if (SummaryByExactKey.TryGetValue(exactKey, out data))
            {
                return true;
            }

            string siteKey = BuildSiteKey(eventId, siteId);
            return SummaryBySiteKey.TryGetValue(siteKey, out data);
        }

        private static string BuildExactKey(string eventId, int siteId, int vehicleId)
        {
            return string.Join("|", new[]
            {
                eventId ?? string.Empty,
                siteId.ToString(CultureInfo.InvariantCulture),
                vehicleId.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string BuildSiteKey(string eventId, int siteId)
        {
            return string.Join("|", new[]
            {
                eventId ?? string.Empty,
                siteId.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static bool TryParseIncidentOutcome(string eventId, out int incidentId, out string faction)
        {
            incidentId = 0;
            faction = string.Empty;

            if (string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            string[] parts = eventId.Split('_');
            if (parts.Length < 8)
            {
                return false;
            }

            if (!string.Equals(parts[0], "TFTV", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parts[1], "INCIDENT", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parts[4], "OUTCOME", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out incidentId))
            {
                return false;
            }

            faction = parts[3];
            return true;
        }

        private static bool TryParseChoiceIndex(string eventId, out int choiceIndex)
        {
            choiceIndex = -1;
            if (string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            string[] parts = eventId.Split('_');
            if (parts.Length < 8 || !string.Equals(parts[4], "OUTCOME", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out choiceIndex);
        }

        private static void RemoveExistingSummary(Transform parent)
        {
            List<Transform> toRemove = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child != null && child.name == SummaryRootName)
                {
                    toRemove.Add(child);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                UnityEngine.Object.Destroy(toRemove[i].gameObject);
            }
        }

        private static void CleanupOldSummaries()
        {
            DateTime cutoff = DateTime.UtcNow.AddHours(-24);

            List<string> exactKeysToRemove = SummaryByExactKey
                .Where(kv => kv.Value == null || kv.Value.CreatedUtc < cutoff)
                .Select(kv => kv.Key)
                .ToList();

            for (int i = 0; i < exactKeysToRemove.Count; i++)
            {
                SummaryByExactKey.Remove(exactKeysToRemove[i]);
            }

            List<string> siteKeysToRemove = SummaryBySiteKey
                .Where(kv => kv.Value == null || kv.Value.CreatedUtc < cutoff)
                .Select(kv => kv.Key)
                .ToList();

            for (int i = 0; i < siteKeysToRemove.Count; i++)
            {
                SummaryBySiteKey.Remove(siteKeysToRemove[i]);
            }
        }

        private static void CreateOrUpdateSummaryPanel(Transform parent, UIModuleSiteEncounters module, string summaryBody)
        {
            GameObject root;
            Transform existingRoot = parent.Find(SummaryRootName);
            if (existingRoot == null)
            {
                root = new GameObject(
                    SummaryRootName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Outline),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter),
                    typeof(LayoutElement));
                root.transform.SetParent(parent, false);
            }
            else
            {
                root = existingRoot.gameObject;
            }

            // Critical: opt out of parent layout so anchoredPosition/sizeDelta are honored.
            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            ConfigureSummaryPanelRect(rootRect, module);

            Image background = root.GetComponent<Image>();
            background.raycastTarget = false;
            background.color = new Color(0f, 0f, 0f, 0.55f);

            Outline border = root.GetComponent<Outline>();
            border.effectColor = new Color(1f, 1f, 1f, 0.25f);
            border.effectDistance = new Vector2(1f, 1f);

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text summaryText;
            Transform existingText = root.transform.Find(SummaryTextName);
            if (existingText == null)
            {
                GameObject textObj = new GameObject(SummaryTextName, typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(root.transform, false);
                summaryText = textObj.GetComponent<Text>();
            }
            else
            {
                summaryText = existingText.GetComponent<Text>();
            }

            Text style = module.EncounterDescriptionText;
            if (style != null)
            {
                summaryText.font = style.font;
                summaryText.fontSize = style.fontSize > 2 ? style.fontSize - 2 : style.fontSize;
                summaryText.color = style.color;
            }

            summaryText.alignment = TextAnchor.UpperLeft;
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            summaryText.verticalOverflow = VerticalWrapMode.Overflow;
            summaryText.supportRichText = true;
            summaryText.text = summaryBody;
        }

        private static void ConfigureSummaryPanelRect(RectTransform rootRect, UIModuleSiteEncounters module)
        {
            if (rootRect == null)
            {
                return;
            }

            const float panelWidth = 800f;
            const float gapFromEventText = 1000f;

            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.localScale = Vector3.one;
            rootRect.sizeDelta = new Vector2(panelWidth, 0f);

            Text desc = module != null ? module.EncounterDescriptionText : null;
            if (desc != null)
            {
                RectTransform descRect = desc.rectTransform;
                float leftEdgeX = descRect.anchoredPosition.x - (descRect.rect.width * descRect.pivot.x);
                rootRect.anchoredPosition = new Vector2(leftEdgeX - gapFromEventText, descRect.anchoredPosition.y+800);
            }
            
        }

        private static void AddPersonnelToRewards(UIModuleSiteEncounters module, Transform rewardContainer, SummaryData data)
        {
            if (module == null || rewardContainer == null || data == null || data.PersonnelCount <= 0)
            {
                return;
            }

            if (module.EncounterRewardTextPrefab == null)
            {
                return;
            }

            GameObject rewardGo = UnityEngine.Object.Instantiate(module.EncounterRewardTextPrefab, rewardContainer);
            rewardGo.name = PersonnelRewardLineName;

            Text rewardText = rewardGo.GetComponent<Text>();
            if (rewardText == null)
            {
                return;
            }

            string valueText = data.PersonnelCount.ToString("+#;-#");
            if (!string.IsNullOrEmpty(module.PositiveRewardTextPattern) && module.PositiveRewardTextPattern.Contains("{0}"))
            {
                valueText = string.Format(module.PositiveRewardTextPattern, valueText);
            }

            rewardText.supportRichText = true;
            rewardText.text = "Personnel obtained: " + valueText;
        }
    }
}