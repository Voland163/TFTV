using HarmonyLib;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    internal static class AffinityBenefitChoiceUI
    {
        private const string PanelRootName = "[Mod]AffinityBenefitChoiceRoot";
        private const string DiagTag = "[Incidents][AffinityChoiceUI]";

        private static readonly FieldInfo SummaryByExactKeyField =
            AccessTools.Field(typeof(IncidentOutcomeSummaryUI), "SummaryByExactKey");

        private static readonly FieldInfo SummaryBySiteKeyField =
            AccessTools.Field(typeof(IncidentOutcomeSummaryUI), "SummaryBySiteKey");

        private sealed class AwardRecord
        {
            public string EventId;
            public int SiteId;
            public int VehicleId;
            public LeaderSelection.AffinityApproach Approach;
            public int Rank;
            public DateTime CreatedUtc;
        }

        private static readonly Dictionary<string, AwardRecord> AwardByExactKey =
            new Dictionary<string, AwardRecord>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, AwardRecord> AwardBySiteKey =
            new Dictionary<string, AwardRecord>(StringComparer.OrdinalIgnoreCase);

        internal static void RecordAffinityAward(
            string eventId,
            int siteId,
            int vehicleId,
            LeaderSelection.AffinityApproach approach,
            int rank)
        {
            try
            {
                if (string.IsNullOrEmpty(eventId) || rank <= 0)
                {
                    return;
                }

                AwardRecord record = new AwardRecord
                {
                    EventId = eventId,
                    SiteId = siteId,
                    VehicleId = vehicleId,
                    Approach = approach,
                    Rank = rank,
                    CreatedUtc = DateTime.UtcNow
                };

                string exactKey = BuildExactKey(eventId, siteId, vehicleId);
                string siteKey = BuildSiteKey(eventId, siteId);

                AwardByExactKey[exactKey] = record;
                AwardBySiteKey[siteKey] = record;

                CleanupOldAwardRecords();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetEncounter")]
        private static class UIModuleSiteEncounters_SetEncounter_AffinityChoice_Patch
        {
            private static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, bool pagingEvent)
            {
                try
                {
                    if (__instance == null || geoEvent?.Context == null || pagingEvent)
                    {
                        return;
                    }

                    TryRenderAffinityChoicePanel(__instance, geoEvent);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteEncounters), "SetClosingEncounter")]
        private static class UIModuleSiteEncounters_SetClosingEncounter_AffinityChoice_Patch
        {
            private static void Postfix(UIModuleSiteEncounters __instance, GeoscapeEvent geoEvent, GeoEventChoice closingChoice, bool useEventTexts)
            {
                try
                {
                    if (__instance == null || geoEvent?.Context == null)
                    {
                        return;
                    }

                    TryRenderAffinityChoicePanel(__instance, geoEvent);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void TryRenderAffinityChoicePanel(UIModuleSiteEncounters module, GeoscapeEvent geoEvent)
        {
            Transform panelParent = module.transform;
            if (panelParent == null)
            {
                TFTVLogger.Always($"{DiagTag} No panel parent.");
                return;
            }

            RemoveExistingPanel(panelParent);

            if (!IsIncidentSuccessEvent(geoEvent.EventID))
            {
                return;
            }

            if (!TryGetAffinityAwardFromSummary(geoEvent, out LeaderSelection.AffinityApproach approach, out int rank))
            {
                TFTVLogger.Always($"{DiagTag} No affinity award found for event={geoEvent.EventID}");
                return;
            }

            GeoLevelController level = geoEvent.Context.Level;
            if (level == null)
            {
                return;
            }

            CreatePanel(panelParent, module, level, approach, rank);
        }

        private static bool IsIncidentSuccessEvent(string eventId)
        {
            return !string.IsNullOrEmpty(eventId)
                && eventId.StartsWith("TFTV_INCIDENT_", StringComparison.OrdinalIgnoreCase)
                && eventId.IndexOf("_OUTCOME_", StringComparison.OrdinalIgnoreCase) >= 0
                && eventId.EndsWith("_SUCCESS", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetAffinityAwardFromSummary(
            GeoscapeEvent geoEvent,
            out LeaderSelection.AffinityApproach approach,
            out int rank)
        {
            approach = default(LeaderSelection.AffinityApproach);
            rank = 0;

            // Primary source: explicit award record from Resolution
            if (TryGetRecordedAward(geoEvent, out AwardRecord record))
            {
                approach = record.Approach;
                rank = record.Rank;
                return true;
            }

            // Fallback: previous reflection parsing (kept for safety)
            object summaryData = GetSummaryData(geoEvent);
            if (summaryData == null)
            {
                return false;
            }

            string affinityResultText = GetStringField(summaryData, "AffinityResultText");
            if (string.IsNullOrEmpty(affinityResultText) ||
                string.Equals(affinityResultText, "No change", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryParseAffinityResult(affinityResultText, out approach, out rank);
        }

        private static bool TryGetRecordedAward(GeoscapeEvent geoEvent, out AwardRecord record)
        {
            record = null;

            int siteId = geoEvent.Context.Site?.SiteId ?? -1;
            int vehicleId = geoEvent.Context.Vehicle?.VehicleID ?? -1;
            string eventId = geoEvent.EventID ?? string.Empty;

            string exactKey = BuildExactKey(eventId, siteId, vehicleId);
            if (AwardByExactKey.TryGetValue(exactKey, out record))
            {
                return true;
            }

            string siteKey = BuildSiteKey(eventId, siteId);
            return AwardBySiteKey.TryGetValue(siteKey, out record);
        }

        private static object GetSummaryData(GeoscapeEvent geoEvent)
        {
            IDictionary byExact = SummaryByExactKeyField?.GetValue(null) as IDictionary;
            IDictionary bySite = SummaryBySiteKeyField?.GetValue(null) as IDictionary;
            if (byExact == null && bySite == null)
            {
                return null;
            }

            int siteId = geoEvent.Context.Site?.SiteId ?? -1;
            int vehicleId = geoEvent.Context.Vehicle?.VehicleID ?? -1;
            string eventId = geoEvent.EventID ?? string.Empty;

            string exactKey = BuildExactKey(eventId, siteId, vehicleId);
            if (byExact != null && byExact.Contains(exactKey))
            {
                return byExact[exactKey];
            }

            string siteKey = BuildSiteKey(eventId, siteId);
            if (bySite != null && bySite.Contains(siteKey))
            {
                return bySite[siteKey];
            }

            return null;
        }

        private static string GetStringField(object instance, string fieldName)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
            object value = field != null ? field.GetValue(instance) : null;
            return value as string ?? string.Empty;
        }

        private static bool TryParseAffinityResult(
            string text,
            out LeaderSelection.AffinityApproach approach,
            out int rank)
        {
            approach = default(LeaderSelection.AffinityApproach);
            rank = 0;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int idx = text.LastIndexOf(" rank ", StringComparison.OrdinalIgnoreCase);
            if (idx <= 0)
            {
                return false;
            }

            string approachToken = text.Substring(0, idx).Trim();
            string rankToken = text.Substring(idx + 6).Trim();

            if (!Enum.TryParse(approachToken, true, out approach))
            {
                return false;
            }

            if (!int.TryParse(rankToken, out rank) || rank <= 0)
            {
                return false;
            }

            return true;
        }

        private static void CreatePanel(
            Transform parent,
            UIModuleSiteEncounters module,
            GeoLevelController level,
            LeaderSelection.AffinityApproach approach,
            int rank)
        {
            GameObject root = new GameObject(
                PanelRootName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(LayoutElement));

            // keep parent as module transform for input/raycast consistency
            root.transform.SetParent(module.transform, false);
            root.transform.SetAsLastSibling();

            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            ConfigurePanelRect(rootRect, module);

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
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text style = module.EncounterDescriptionText;

            CreateText(root.transform, style, $"<b>Affinity Benefits</b>\n{approach} rank {rank} granted.", 0);

            int geoCurrent = Affinities.AffinityBenefitsChoices.GetGeoscapeBenefitChoice(level, approach);
            int tacCurrent = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoice(level, approach);

            CreateBenefitSelector(root.transform, style, "Geoscape", approach, rank, geoCurrent, true, level);
            CreateBenefitSelector(root.transform, style, "Tactical", approach, rank, tacCurrent, false, level);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

            int buttonCount = root.GetComponentsInChildren<Button>(true).Length;
           
        }

        private static void CreateBenefitSelector(
            Transform parent,
            Text style,
            string trackName,
            LeaderSelection.AffinityApproach approach,
            int rank,
            int currentOption,
            bool isGeoscape,
            GeoLevelController level)
        {
            CreateText(parent, style, $"<b>{trackName}:</b>", 0);

            Text selectedText = CreateText(
                parent,
                style,
                $"Selected: {BuildBenefitPreview(approach, rank, currentOption, isGeoscape)}",
                2);

            string optionAText = $"Option A: {BuildBenefitPreview(approach, rank, 1, isGeoscape)}";
            string optionBText = $"Option B: {BuildBenefitPreview(approach, rank, 2, isGeoscape)}";

            CreateOptionButton(parent, style, optionAText, () =>
            {
                if (isGeoscape)
                {
                    Affinities.AffinityBenefitsChoices.SetGeoscapeBenefitChoice(level, approach, 1);
                }
                else
                {
                    Affinities.AffinityBenefitsChoices.SetTacticalBenefitChoice(level, approach, 1);
                }

                selectedText.text = $"Selected: {BuildBenefitPreview(approach, rank, 1, isGeoscape)}";
            });

            CreateOptionButton(parent, style, optionBText, () =>
            {
                if (isGeoscape)
                {
                    Affinities.AffinityBenefitsChoices.SetGeoscapeBenefitChoice(level, approach, 2);
                }
                else
                {
                    Affinities.AffinityBenefitsChoices.SetTacticalBenefitChoice(level, approach, 2);
                }

                selectedText.text = $"Selected: {BuildBenefitPreview(approach, rank, 2, isGeoscape)}";
            });
        }

        private static Text CreateText(Transform parent, Text style, string content, int leftPad)
        {
            GameObject textObj = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(Text),
                typeof(ContentSizeFitter),
                typeof(LayoutElement));

            textObj.transform.SetParent(parent, false);

            LayoutElement le = textObj.GetComponent<LayoutElement>();
            le.minHeight = 0f;
            le.preferredHeight = -1f; // let Text preferred height drive layout
            le.flexibleHeight = 0f;

            ContentSizeFitter fitter = textObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text text = textObj.GetComponent<Text>();
            if (style != null)
            {
                text.font = style.font;
                int size = style.fontSize - 3;
                text.fontSize = size > 16 ? size : 16;
                text.color = style.color;
            }
            else
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 18;
                text.color = Color.white;
            }

            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.text = string.IsNullOrEmpty(content) ? string.Empty : new string(' ', leftPad) + content;

            return text;
        }

        private static void CreateOptionButton(Transform parent, Text style, string label, Action onClick)
        {
            GameObject btnObj = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.22f);
            img.raycastTarget = true;

            LayoutElement le = btnObj.GetComponent<LayoutElement>();
            le.minHeight = 64f;
            le.preferredHeight = 72f;
            le.flexibleHeight = 0f;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                TFTVLogger.Always($"{DiagTag} Click {label}");
                onClick?.Invoke();
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10f, 6f);
            txtRect.offsetMax = new Vector2(-10f, -6f);

            Text txt = txtObj.GetComponent<Text>();
            Font fallbackFont = style != null && style.font != null
                ? style.font
                : Resources.GetBuiltinResource<Font>("Arial.ttf");

            txt.font = fallbackFont;

            int baseSize = style != null ? style.fontSize : 24;
            int targetSize = baseSize - 6;
            if (targetSize > 22) targetSize = 22;
            if (targetSize < 14) targetSize = 14;
            txt.fontSize = targetSize;

            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = false;
            txt.raycastTarget = false;
            txt.text = label;

            
        }

        private static void ConfigurePanelRect(RectTransform rootRect, UIModuleSiteEncounters module)
        {
            if (rootRect == null)
            {
                return;
            }

            const float panelWidth = 820f;
            const float rightMargin = 24f;
            const float topMargin = 170f; // push below top header/buttons

            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.localScale = Vector3.one;
            rootRect.sizeDelta = new Vector2(panelWidth, 0f);
            rootRect.anchoredPosition = new Vector2(-rightMargin, -topMargin);
        }

        private static void RemoveExistingPanel(Transform parent)
        {
            Transform existing = parent.Find(PanelRootName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }
        }

        private static string BuildExactKey(string eventId, int siteId, int vehicleId)
        {
            return string.Join("|", new[]
            {
                eventId ?? string.Empty,
                siteId.ToString(),
                vehicleId.ToString()
            });
        }

        private static string BuildSiteKey(string eventId, int siteId)
        {
            return string.Join("|", new[]
            {
                eventId ?? string.Empty,
                siteId.ToString()
            });
        }

        private static void CleanupOldAwardRecords()
        {
            DateTime cutoff = DateTime.UtcNow.AddHours(-24);

            List<string> removeExact = new List<string>();
            foreach (KeyValuePair<string, AwardRecord> kv in AwardByExactKey)
            {
                if (kv.Value == null || kv.Value.CreatedUtc < cutoff)
                {
                    removeExact.Add(kv.Key);
                }
            }

            for (int i = 0; i < removeExact.Count; i++)
            {
                AwardByExactKey.Remove(removeExact[i]);
            }

            List<string> removeSite = new List<string>();
            foreach (KeyValuePair<string, AwardRecord> kv in AwardBySiteKey)
            {
                if (kv.Value == null || kv.Value.CreatedUtc < cutoff)
                {
                    removeSite.Add(kv.Key);
                }
            }

            for (int i = 0; i < removeSite.Count; i++)
            {
                AwardBySiteKey.Remove(removeSite[i]);
            }
        }

        private static string BuildBenefitPreview(
            LeaderSelection.AffinityApproach approach,
            int rank,
            int option,
            bool isGeoscape)
        {
            int r = Math.Max(1, rank);

            if (isGeoscape)
            {
                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology: return option == 1 ? $"+{15 * r}% Haven Defense rewards" : $"{15 * r}% Delirium perk recovery chance";
                    case LeaderSelection.AffinityApproach.Exploration: return option == 1 ? $"{r}h faster exploration" : $"+{2 * r} Stamina after mission";
                    case LeaderSelection.AffinityApproach.Occult: return option == 1 ? $"{6 * r}h base-attack warning" : $"{15 * r}% mist penalty/buff modifier";
                    case LeaderSelection.AffinityApproach.Biotech: return option == 1 ? $"+{30 * r} HP after mission" : $"+{15 * r} Haven Leader Rep";
                    case LeaderSelection.AffinityApproach.Machinery: return option == 1 ? $"+{150 * r} vehicle HP after mission" : $"{25 * r}% less maintenance loss";
                    case LeaderSelection.AffinityApproach.Compute: return option == 1 ? $"+{10 * r}% aircraft speed" : "advance Haven attack warning";
                }
            }
            else
            {
                switch (approach)
                {
                    case LeaderSelection.AffinityApproach.PsychoSociology: return option == 1 ? $"+{2 * r} WP on Recovery" : $"+{r} friendly Haven defender";
                    case LeaderSelection.AffinityApproach.Exploration: return option == 1 ? $"higher enemy loot drops (x{r})" : $"+{r} controlled defender, +{r} WP civ extract";
                    case LeaderSelection.AffinityApproach.Occult: return option == 1 ? $"-{5 * r}% TBTV chance" : $"+{50 * r}% Stamina->Delirium effect";
                    case LeaderSelection.AffinityApproach.Biotech: return option == 1 ? $"Medkit: +{10 * r} organic part HP" : $"Medkit: +{4 * r} Delirium WP restored";
                    case LeaderSelection.AffinityApproach.Machinery: return option == 1 ? $"Repair: +{10 * r} bionic/vehicle part HP" : $"Repair: +{4 * r} armor per part";
                    case LeaderSelection.AffinityApproach.Compute: return option == 1 ? $"Vehicle crew bonus: +{3 * r} move, +{10 * r}% accuracy" : $"+{50 * r}% Delirium-based Perception vs Pandorans";
                }
            }

            return option == 1 ? "Option A" : "Option B";
        }
    }
}