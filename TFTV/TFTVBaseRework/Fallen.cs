using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVBaseRework
{
    internal class Fallen
    {
        private const string PanelObjectName = "FallenOperativesPanel_Harmony";
        private const string HeaderObjectName = "FallenOperativesHeader_Harmony";
        private const string ContentObjectName = "FallenOperativesContent_Harmony";
        private const string HeaderText = "FALLEN OPERATIVES";
        private const string ProjectOsirisPreparationText = "Preparing for PROJECT OSIRIS";

        private static readonly Color TextColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        private static void ShowForModal(UIModal modal)
        {
            GeoMission mission = modal.Data as GeoMission;
            if (mission == null)
            {
                return;
            }

            List<FallenOperativeInfo> fallenOperatives = GetFallenOperatives(mission);
            Transform root = modal.transform;
            Transform panel = root.Find(PanelObjectName) ?? CreatePanel(root);
            Text header = panel.Find(HeaderObjectName)?.GetComponent<Text>();
            Transform content = panel.Find(ContentObjectName);
            if (header == null || content == null)
            {
                return;
            }

            bool active = fallenOperatives.Count > 0;
            panel.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            header.text = HeaderText;
            RebuildEntries(content, fallenOperatives);
        }

        private static List<FallenOperativeInfo> GetFallenOperatives(GeoMission mission)
        {
            List<FallenOperativeInfo> list = new List<FallenOperativeInfo>();
            if (mission?.Result == null || mission.Site?.GeoLevel == null)
            {
                return list;
            }

            GeoLevelController geoLevel = mission.Site.GeoLevel;
            PhoenixStatistics currentGameStats = GameUtl.GameComponent<PhoenixStatisticsManager>()?.CurrentGameStats;
            FactionResult resultByFacionDef = mission.Result.GetResultByFacionDef(geoLevel.ViewerFaction.Def.PPFactionDef);
            if (resultByFacionDef == null)
            {
                return list;
            }

            foreach (TacActorUnitResult tacActorUnitResult in resultByFacionDef.GetUnitResultsData<TacActorUnitResult>())
            {
                if (!tacActorUnitResult.IsAlive && tacActorUnitResult.GeoUnitId != GeoTacUnitId.None)
                {
                    FallenOperativeInfo operativeInfo = GetOperativeInfo(geoLevel, currentGameStats, tacActorUnitResult.GeoUnitId);
                    if (operativeInfo != null && !string.IsNullOrEmpty(operativeInfo.Name) && !list.Any(i => i.GeoUnitId == operativeInfo.GeoUnitId))
                    {
                        list.Add(operativeInfo);
                    }
                }
            }

            return list;
        }

        private static FallenOperativeInfo GetOperativeInfo(GeoLevelController geoLevel, PhoenixStatistics gameStats, GeoTacUnitId unitId)
        {
            if (!geoLevel.DeadSoldiers.TryGetValue(unitId, out GeoUnitDescriptor value) || value.UnitType.IsVehicle)
            {
                return null;
            }

            FallenOperativeInfo fallenOperativeInfo = new FallenOperativeInfo
            {
                Name = value.GetName(),
                GeoUnitId = (int)unitId,
                IsPreparingForProjectOsiris = unitId == global::TFTV.TFTVProjectOsiris.IdProjectOsirisCandidate
            };

            List<ViewElementDef> classViewElements = new List<ViewElementDef>
            {
                value.Progression.MainSpecDef.ViewElementDef
            };

            if (value.Progression.SecondarySpecDef != null)
            {
                classViewElements.Add(value.Progression.SecondarySpecDef.ViewElementDef);
            }

            fallenOperativeInfo.ClassViewElements = classViewElements.ToArray();

            SoldierStats soldierStat = gameStats?.GetSoldierStat(unitId, false);
            fallenOperativeInfo.Missions = soldierStat?.MissionsParticipated ?? 0;
            fallenOperativeInfo.Kills = soldierStat?.EnemiesKilled.Sum(e => e.KillCount) ?? 0;
            fallenOperativeInfo.SkillPointsReturned = TFTVExperienceDistribution.GetDeathSkillPointRefund(
                soldierStat,
                geoLevel.CurrentDifficultyLevel);
            fallenOperativeInfo.FavoriteWeapon = GetFavoriteWeapon(soldierStat);
            fallenOperativeInfo.FavoriteSkill = GetFavoriteSkill(soldierStat);
            return fallenOperativeInfo;
        }

        private static string GetFavoriteWeapon(SoldierStats soldierStats)
        {
            if (soldierStats == null || !soldierStats.ItemsUsed.Any(i => i.UsedItem != null))
            {
                return "-";
            }

            UsedWeaponStat usedWeaponStat = soldierStats.ItemsUsed
                .Where(i => i.UsedItem != null)
                .OrderByDescending(i => i.UsedCount)
                .FirstOrDefault();

            ViewElementDef viewElementDef = usedWeaponStat.UsedItem.ViewElementDef;
            return viewElementDef?.DisplayName1.Localize(null) ?? usedWeaponStat.UsedItem.name;
        }

        private static string GetFavoriteSkill(SoldierStats soldierStats)
        {
            if (soldierStats == null || !soldierStats.AbilitiesUsed.Any(i => i.UsedAbility != null))
            {
                return "-";
            }

            UsedAbilityStat usedAbilityStat = soldierStats.AbilitiesUsed
                .Where(i => i.UsedAbility != null)
                .OrderByDescending(i => i.UsedCount)
                .FirstOrDefault();

            ViewElementDef viewElementDef = usedAbilityStat.UsedAbility.ViewElementDef;
            return viewElementDef?.DisplayName1.Localize(null) ?? usedAbilityStat.UsedAbility.name;
        }

        private static Transform CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject(PanelObjectName, typeof(RectTransform));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1260f, 700f);

            CreateText(panel.transform, HeaderObjectName, new Vector2(0f, 280f), 72, FontStyle.Bold, TextAnchor.MiddleCenter);

            GameObject content = new GameObject(ContentObjectName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(panel.transform, false);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, -40f);
            contentRect.sizeDelta = new Vector2(1220f, 520f);

            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            return panel.transform;
        }

        private static void RebuildEntries(Transform content, List<FallenOperativeInfo> fallenOperatives)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            foreach (FallenOperativeInfo fallenOperativeInfo in fallenOperatives)
            {
                GameObject entry = new GameObject("FallenOperativeColumn_Harmony", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                RectTransform entryRect = entry.GetComponent<RectTransform>();
                entryRect.SetParent(content, false);
                entryRect.sizeDelta = new Vector2(300f, 500f);

                LayoutElement entryLayout = entry.GetComponent<LayoutElement>();
                entryLayout.preferredWidth = 300f;
                entryLayout.preferredHeight = 500f;

                VerticalLayoutGroup entryGroup = entry.GetComponent<VerticalLayoutGroup>();
                entryGroup.childAlignment = TextAnchor.UpperCenter;
                entryGroup.spacing = 10f;
                entryGroup.childControlHeight = false;
                entryGroup.childControlWidth = true;
                entryGroup.childForceExpandHeight = false;

                GameObject classIconRoot = new GameObject("ClassIcon_Harmony", typeof(RectTransform), typeof(LayoutElement));
                RectTransform classIconRect = classIconRoot.GetComponent<RectTransform>();
                classIconRect.SetParent(entry.transform, false);
                classIconRect.sizeDelta = new Vector2(120f, 120f);

                LayoutElement classIconLayout = classIconRoot.GetComponent<LayoutElement>();
                classIconLayout.preferredWidth = 120f;
                classIconLayout.preferredHeight = 120f;

                PopulateClassIcons(classIconRoot.transform, fallenOperativeInfo.ClassViewElements);

                CreateInfoLine(entry.transform, fallenOperativeInfo.Name, 40, FontStyle.Bold);
                CreateInfoLine(entry.transform, $"Missions: {fallenOperativeInfo.Missions}  |  Kills: {fallenOperativeInfo.Kills}", 28, FontStyle.Normal);
                CreateInfoLine(entry.transform, $"Favorite Weapon: {fallenOperativeInfo.FavoriteWeapon}", 28, FontStyle.Normal);
                CreateInfoLine(entry.transform, $"Favorite Skill: {fallenOperativeInfo.FavoriteSkill}", 28, FontStyle.Normal);
                CreateInfoLine(
                    entry.transform,
                    fallenOperativeInfo.IsPreparingForProjectOsiris
                        ? ProjectOsirisPreparationText
                        : $"SP Returned: {fallenOperativeInfo.SkillPointsReturned}",
                    30,
                    FontStyle.Normal);

                string transferSummary = AffinityInheritance.GetTransferSummary(fallenOperativeInfo.GeoUnitId);
                if (!string.IsNullOrEmpty(transferSummary))
                {
                    CreateInfoLine(entry.transform, transferSummary, 24, FontStyle.Italic);
                }
            }
        }

        private static void CreateInfoLine(Transform parent, string text, int fontSize, FontStyle style)
        {
            GameObject go = new GameObject("InfoLine_Harmony", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(280f, 70f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 280f;
            le.preferredHeight = 70f;

            Text txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = TextColor;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.UpperCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = text;
        }

        private static void PopulateClassIcons(Transform parent, IEnumerable<ViewElementDef> classViewElements)
        {
            List<ViewElementDef> defs = classViewElements?.Where(v => v != null).Take(2).ToList() ?? new List<ViewElementDef>();
            if (defs.Count == 0)
            {
                return;
            }

            Sprite mainSprite = GetClassSprite(defs[0]);
            if (defs.Count == 1 || GetClassSprite(defs[1]) == null)
            {
                Image single = CreateIconImage(parent, "SingleClassIcon_Harmony", new Vector2(96f, 96f));
                single.sprite = mainSprite;
                single.preserveAspect = true;
                return;
            }

            const float halfWidth = 38f;
            const float gap = 5f;
            float halfCenterOffset = (halfWidth / 2f) + (gap / 2f);

            Sprite secondarySprite = GetClassSprite(defs[1]);
            CreateHalfIcon(parent, "MainClassHalf_Harmony", mainSprite, new Vector2(-halfCenterOffset, 0f), new Vector2(halfCenterOffset, 0f), halfWidth);
            CreateHalfIcon(parent, "SecondaryClassHalf_Harmony", secondarySprite, new Vector2(halfCenterOffset, 0f), new Vector2(-halfCenterOffset, 0f), halfWidth);
        }

        private static void CreateHalfIcon(Transform parent, string name, Sprite sprite, Vector2 halfPosition, Vector2 spriteOffset, float halfWidth)
        {
            GameObject halfRoot = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            RectTransform halfRect = halfRoot.GetComponent<RectTransform>();
            halfRect.SetParent(parent, false);
            halfRect.anchorMin = new Vector2(0.5f, 0.5f);
            halfRect.anchorMax = new Vector2(0.5f, 0.5f);
            halfRect.pivot = new Vector2(0.5f, 0.5f);
            halfRect.anchoredPosition = halfPosition;
            halfRect.sizeDelta = new Vector2(halfWidth, 96f);

            Image iconImage = CreateIconImage(halfRoot.transform, $"{name}_Image", new Vector2(96f, 96f));
            iconImage.rectTransform.anchoredPosition = spriteOffset;
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
        }

        private static Sprite GetClassSprite(ViewElementDef viewElementDef)
        {
            if (viewElementDef == null)
            {
                return null;
            }

            return viewElementDef.SmallIcon ?? viewElementDef.LargeIcon;
        }

        private static Image CreateIconImage(Transform parent, string name, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform component = gameObject.GetComponent<RectTransform>();
            component.SetParent(parent, false);
            component.anchorMin = new Vector2(0.5f, 0.5f);
            component.anchorMax = new Vector2(0.5f, 0.5f);
            component.pivot = new Vector2(0.5f, 0.5f);
            component.anchoredPosition = Vector2.zero;
            component.sizeDelta = size;
            return gameObject.GetComponent<Image>();
        }

        private static void CreateText(Transform parent, string objectName, Vector2 position, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            RectTransform component = gameObject.GetComponent<RectTransform>();
            component.SetParent(parent, false);
            component.anchorMin = new Vector2(0.5f, 0.5f);
            component.anchorMax = new Vector2(0.5f, 0.5f);
            component.pivot = new Vector2(0.5f, 0.5f);
            component.anchoredPosition = position;
            component.sizeDelta = new Vector2(1200f, 140f);

            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = string.Empty;
            text.color = TextColor;
            text.fontStyle = style;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        [HarmonyPatch(typeof(AlienBaseOutcomeDataBind), "ModalShowHandler")]
        private static class AlienBaseOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(AmbushOutcomeDataBind), "ModalShowHandler")]
        private static class AmbushOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(AncientSiteOutcomeDataBind), "ModalShowHandler")]
        private static class AncientSiteOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(HavenDefenceOutcomeDataBind), "ModalShowHandler")]
        private static class HavenDefenceOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(HavenInfiltrateMissionOutcomeDataBind), "ModalShowHandler")]
        private static class HavenInfiltrateMissionOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(InfestedHavenOutcomeDataBind), "ModalShowHandler")]
        private static class InfestedHavenOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(PhoenixBaseDefenseOutcomeDataBind), "ModalShowHandler")]
        private static class PhoenixBaseDefenseOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        [HarmonyPatch(typeof(ScavengeOutcomeDataBind), "ModalShowHandler")]
        private static class ScavengeOutcomeDataBindPatch
        {
            private static void Postfix(UIModal modal) => ShowForModal(modal);
        }

        private sealed class FallenOperativeInfo
        {
            public int GeoUnitId;
            public string Name;
            public IEnumerable<ViewElementDef> ClassViewElements = Enumerable.Empty<ViewElementDef>();
            public int Missions;
            public int Kills;
            public int SkillPointsReturned;
            public string FavoriteWeapon;
            public string FavoriteSkill;
            public bool IsPreparingForProjectOsiris;
        }
    }
}