using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The dossier shown before a decision about a character is taken: who they are, what they can
    /// do, and what the decision costs them. Laid out like the Haven Recruits details panel and
    /// using the same pieces - its stat icons, its ability frames, the game's own inventory slots -
    /// so a soldier reads the same way wherever the player meets them.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        private const int SummaryAbilityIconSize = 52;
        private const int SummaryInventorySlotSize = 70;
        private const float SummaryStatRowHeight = 46f;

        private static readonly Color StatValueColor = new Color(1.00f, 0.72f, 0.25f, 1f);
        private static readonly Color StatGainColor = new Color(0.45f, 0.90f, 0.45f, 1f);
        private static readonly Color StatLossColor = new Color(0.95f, 0.45f, 0.40f, 1f);
        private static readonly Color AbilityLockedColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);

        private static readonly Dictionary<string, Sprite> _statIcons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Sprite _abilityFrameSprite;

        /// <summary>
        /// Optional <paramref name="projectedStats"/> shows what training would leave the character
        /// with, as "21 (26)" against the current figure.
        /// </summary>
        internal static void CreateCharacterSummary(Transform parent, GeoCharacter character,
            IEnumerable<GeoItem> itemsGoingToStorage, ProjectedStats projectedStats = null)
        {
            if (character == null)
            {
                return;
            }

            try
            {
                GameObject panel = CreateFramedPanel(parent, "CharacterSummary", out Transform content,
                    borderThickness: 2f, padding: 12, spacing: 8f);
                LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
                panelElement.flexibleHeight = 0f;

                CreateSummaryIdentity(content, character);
                CreateSummaryProgress(content, character);
                CreateSummaryStats(content, character, projectedStats);
                CreateSummaryAbilities(content, character);
                CreateSummaryStorage(content, itemsGoingToStorage);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>What a character's three trained stats would become; null when nothing is pending.</summary>
        internal sealed class ProjectedStats
        {
            public int Strength;
            public int Willpower;
            public int Speed;
        }

        /// <summary>
        /// The stats a character would hold after gaining <paramref name="levelsGained"/> levels of
        /// training, taken from the same per-level figures the training itself applies.
        /// </summary>
        internal static ProjectedStats BuildProjectedStats(GeoCharacter character, int levelsGained)
        {
            if (character?.Progression == null || levelsGained <= 0)
            {
                return null;
            }

            TrainingFacilityRework.GetStatGains(levelsGained, out int strength, out int willpower, out int speed);

            return new ProjectedStats
            {
                Strength = character.Progression.Strength + strength,
                Willpower = character.Progression.Will + willpower,
                Speed = character.Progression.Speed + speed,
            };
        }

        #region Identity

        private static void CreateSummaryIdentity(Transform parent, GeoCharacter character)
        {
            GameObject row = CreateUIObject("Identity", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 64f);

            ICollection<ViewElementDef> classViews = character.ClassViewElementDefs;
            if (classViews != null)
            {
                foreach (ViewElementDef classView in classViews)
                {
                    if (classView?.SmallIcon == null)
                    {
                        continue;
                    }

                    GameObject iconGO = CreateUIObject("ClassIcon", row.transform);
                    var image = iconGO.AddComponent<Image>();
                    image.sprite = classView.SmallIcon;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    SetSize(iconGO, 48f, 48f);
                }
            }

            Text level = CreateLabel(row.transform, "Level", (character.LevelProgression?.Level ?? 1).ToString(),
                TitleFontSize, AccentOrangeColor);
            SetSize(level.gameObject, 44f, 64f);

            // Identity.Name is the character's full name; DisplayName is the same string.
            string fullName = character.Identity?.Name ?? character.DisplayName;
            Text name = CreateLabel(row.transform, "Name", fullName, 38, TextPrimaryColor);
            LayoutElement nameElement = SetSize(name.gameObject, 0f, 64f);
            nameElement.flexibleWidth = 1f;

            Text classLine = CreateLabel(row.transform, "Class", DescribeClass(character), BodyFontSize, TextDimColor,
                TextAnchor.MiddleRight);
            SetSize(classLine.gameObject, 0f, 64f);
        }

        private static string DescribeClass(GeoCharacter character)
        {
            var parts = new List<string>();

            string mainClass = character.Progression?.MainSpecDef?.ViewElementDef?.DisplayName1?.Localize();
            if (!string.IsNullOrEmpty(mainClass))
            {
                parts.Add(mainClass);
            }

            string secondClass = character.Progression?.SecondarySpecDef?.ViewElementDef?.DisplayName1?.Localize();
            if (!string.IsNullOrEmpty(secondClass))
            {
                parts.Add(secondClass);
            }

            return parts.Count > 0 ? string.Join(" / ", parts) : "No class";
        }

        /// <summary>
        /// The character's own skill points and experience - what they have to spend, and how far
        /// they are through their level.
        /// </summary>
        private static void CreateSummaryProgress(Transform parent, GeoCharacter character)
        {
            GameObject row = CreateUIObject("Progress", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 44f);

            int skillPoints = character.Progression?.SkillPoints ?? 0;
            int experience = character.LevelProgression?.Experience ?? 0;

            CreateSummaryInlineValue(row.transform, "SP", skillPoints.ToString(),
                "Skill points this operative has to spend on their own abilities.");
            CreateSummaryInlineValue(row.transform, "XP", experience.ToString(),
                "Experience earned so far.");
        }

        private static void CreateSummaryInlineValue(Transform parent, string caption, string value, string tooltip)
        {
            GameObject cell = CreateUIObject($"Progress_{caption}", parent);
            var background = cell.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.01f);

            var layout = cell.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(cell, 0f, 44f);

            Text captionLabel = CreateLabel(cell.transform, "Caption", caption, SmallFontSize, TextDimColor);
            SetSize(captionLabel.gameObject, 46f, 44f);

            Text valueLabel = CreateLabel(cell.transform, "Value", value, BodyFontSize, StatValueColor);
            SetSize(valueLabel.gameObject, 0f, 44f);

            AddTextTooltip(cell, $"{caption}\n\n{tooltip}");
        }

        #endregion

        #region Stats

        private static void CreateSummaryStats(Transform parent, GeoCharacter character, ProjectedStats projected)
        {
            CharacterStats stats = character.CharacterStats;
            CharacterProgression progression = character.Progression;
            if (stats == null)
            {
                return;
            }

            string modifierNote = projected != null
                ? "The figure in brackets is what training would leave them with."
                : "The figure in brackets is the value after armour and augmentations.";

            // Left column carries the trained stats, right column the ones gear and class decide -
            // the same split the Haven Recruits panel uses.
            GameObject grid = CreateUIObject("Stats", parent);
            var gridLayout = grid.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 4f;
            gridLayout.childControlWidth = true;
            gridLayout.childControlHeight = true;
            gridLayout.childForceExpandWidth = true;
            gridLayout.childForceExpandHeight = false;
            SetSize(grid, 0f, (SummaryStatRowHeight + 4f) * 3f);

            Transform row1 = CreateStatRow(grid.transform, "StatRow1");
            CreateStatCell(row1, "Strength", "Strength", progression?.Strength ?? stats.Endurance.IntValue,
                projected?.Strength ?? stats.Endurance.IntValue, modifierNote);
            CreateStatCell(row1, "Perception", "Perception", stats.Perception.IntValue.ToString(), null, null, null);

            Transform row2 = CreateStatRow(grid.transform, "StatRow2");
            CreateStatCell(row2, "Willpower", "Willpower", progression?.Will ?? stats.Willpower.IntValue,
                projected?.Willpower ?? stats.Willpower.IntValue, modifierNote);
            CreateStatCell(row2, "Accuracy", "Accuracy", null, FormatPercent(stats.Accuracy.Value), null, null);

            Transform row3 = CreateStatRow(grid.transform, "StatRow3");
            CreateStatCell(row3, "Speed", "Speed", progression?.Speed ?? stats.Speed.IntValue,
                projected?.Speed ?? stats.Speed.IntValue, modifierNote);
            CreateStatCell(row3, "Stealth", "Stealth", null, FormatPercent(stats.Stealth.Value), null, null);

            int delirium = stats.Corruption?.IntValue ?? 0;
            if (delirium > 0)
            {
                Transform row4 = CreateStatRow(grid.transform, "StatRow4");
                LayoutElement gridElement = grid.GetComponent<LayoutElement>();
                gridElement.minHeight += SummaryStatRowHeight + 4f;
                gridElement.preferredHeight = gridElement.minHeight;

                CreateDeliriumCell(row4, delirium, stats.Willpower.IntValue);
                CreateStatCell(row4, null, string.Empty, null, string.Empty, null, null);
            }
        }

        private static Transform CreateStatRow(Transform parent, string name)
        {
            GameObject row = CreateUIObject(name, parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, SummaryStatRowHeight);
            return row.transform;
        }

        private static void CreateStatCell(Transform parent, string statIcon, string caption, int baseValue,
            int finalValue, string tooltip)
        {
            string finalText = finalValue != baseValue ? finalValue.ToString() : null;
            Color finalColor = finalValue > baseValue ? StatGainColor : StatLossColor;
            CreateStatCell(parent, statIcon, caption, baseValue.ToString(), finalText, finalColor, tooltip);
        }

        private static void CreateStatCell(Transform parent, string statIcon, string caption, string baseText,
            string finalText, Color? finalColor, string tooltip)
        {
            GameObject cell = CreateUIObject($"Stat_{caption}", parent);
            var background = cell.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.01f);

            var layout = cell.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            LayoutElement cellElement = SetSize(cell, 0f, SummaryStatRowHeight);
            cellElement.flexibleWidth = 1f;

            if (string.IsNullOrEmpty(caption))
            {
                return;
            }

            Sprite icon = GetStatIcon(statIcon);
            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Icon", cell.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                SetSize(iconGO, 34f, 34f);
            }

            Text label = CreateLabel(cell.transform, "Label", $"{caption}:", BodyFontSize, TextPrimaryColor);
            SetSize(label.gameObject, 0f, SummaryStatRowHeight);

            Text value = CreateLabel(cell.transform, "Value", baseText ?? finalText, BodyFontSize, StatValueColor);
            SetSize(value.gameObject, 0f, SummaryStatRowHeight);

            if (baseText != null && finalText != null)
            {
                Text final = CreateLabel(cell.transform, "Final", $"({finalText})", BodyFontSize,
                    finalColor ?? StatGainColor);
                SetSize(final.gameObject, 0f, SummaryStatRowHeight);
            }

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTextTooltip(cell, $"{caption}\n\n{tooltip}");
            }
        }

        private static void CreateDeliriumCell(Transform parent, int delirium, int willpower)
        {
            GameObject cell = CreateUIObject("Stat_Delirium", parent);
            var background = cell.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.01f);

            var layout = cell.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            LayoutElement cellElement = SetSize(cell, 0f, SummaryStatRowHeight);
            cellElement.flexibleWidth = 1f;

            Sprite icon = DefCache.GetDef<ViewElementDef>("E_Visuals [Corruption_StatusDef]")?.SmallIcon;
            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Icon", cell.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.color = AccentOrangeColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                SetSize(iconGO, 34f, 34f);
            }

            Text label = CreateLabel(cell.transform, "Label", $"Delirium: {delirium}", BodyFontSize, AccentOrangeColor);
            SetSize(label.gameObject, 0f, SummaryStatRowHeight);

            AddTextTooltip(cell, $"Delirium\n\nAt {willpower} delirium - this operative's willpower - they are lost to madness.");
        }

        private static string FormatPercent(float ratio)
        {
            return Mathf.RoundToInt(ratio * 100f) + "%";
        }

        private static Sprite GetStatIcon(string statName)
        {
            if (string.IsNullOrEmpty(statName))
            {
                return null;
            }

            if (_statIcons.TryGetValue(statName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // The same Stat_*.png assets the Haven Recruits panel draws.
            Sprite icon = Helper.CreateSpriteFromImageFile($"Stat_{statName}.png");
            if (icon != null)
            {
                _statIcons[statName] = icon;
            }

            return icon;
        }

        #endregion

        #region Abilities and gear

        private static void CreateSummaryAbilities(Transform parent, GeoCharacter character)
        {
            List<AbilityTrackSlot> slots = CollectAbilitySlots(character);
            if (slots.Count == 0)
            {
                CreateSummaryTextRow(parent, "ABILITIES AND PERKS", "none");
                return;
            }

            var learned = new HashSet<TacticalAbilityDef>(character.Progression?.Abilities ?? new List<TacticalAbilityDef>());

            Transform grid = CreateSummaryIconRow(parent, "ABILITIES AND PERKS", SummaryAbilityIconSize);

            foreach (AbilityTrackSlot slot in slots)
            {
                ViewElementDef view = slot.Ability?.ViewElementDef;
                if (view?.SmallIcon == null)
                {
                    continue;
                }

                Image icon = RecruitOverlayManagerHelpers.MakeFixedIcon(grid, view.SmallIcon, SummaryAbilityIconSize,
                    EnsureAbilityFrameSprite());
                if (icon == null)
                {
                    continue;
                }

                // Abilities the operative has not earned are dimmed, as on the recruit panel.
                icon.color = learned.Contains(slot.Ability) ? Color.white : AbilityLockedColor;

                GameObject triggerTarget = icon.transform.parent != null
                    ? icon.transform.parent.gameObject
                    : icon.gameObject;

                var trigger = triggerTarget.AddComponent<PersonnelAbilityTooltipTrigger>();
                trigger.Ability = slot.Ability;
                trigger.View = view;
            }
        }

        /// <summary>
        /// Class track first, then the personal one - the order the recruit panel shows them in.
        /// </summary>
        private static List<AbilityTrackSlot> CollectAbilitySlots(GeoCharacter character)
        {
            var slots = new List<AbilityTrackSlot>();

            AbilityTrackSlot[] classTrack = character.Progression?.MainSpecDef?.AbilityTrack?.AbilitiesByLevel;
            if (classTrack != null)
            {
                slots.AddRange(classTrack.Where(slot => slot?.Ability != null));
            }

            AbilityTrackSlot[] personalTrack = character.Progression?.PersonalAbilityTrack?.AbilitiesByLevel;
            if (personalTrack != null)
            {
                slots.AddRange(personalTrack.Where(slot => slot?.Ability != null));
            }

            return slots;
        }

        private static void CreateSummaryStorage(Transform parent, IEnumerable<GeoItem> itemsGoingToStorage)
        {
            List<GeoItem> items = itemsGoingToStorage?.ToList() ?? new List<GeoItem>();
            if (items.Count == 0)
            {
                CreateSummaryTextRow(parent, "RETURNED TO STORAGE", "nothing");
                return;
            }

            Transform grid = CreateSummaryIconRow(parent, "RETURNED TO STORAGE", SummaryInventorySlotSize);
            UIGeoItemTooltip tooltip = PersonnelVanillaTooltips.EnsureItemTooltip(grid);

            foreach (GeoItem item in items)
            {
                if (item?.ItemDef == null)
                {
                    continue;
                }

                // The game's own inventory slot, so the gear looks and explains itself exactly as it
                // does everywhere else.
                RecruitOverlayManagerHelpers.MakeInventorySlot(grid, item.ItemDef, SummaryInventorySlotSize,
                    "PersonnelStorage", tooltip);
            }
        }

        private static Sprite EnsureAbilityFrameSprite()
        {
            if (_abilityFrameSprite == null)
            {
                _abilityFrameSprite = Helper.CreateSpriteFromImageFile("UI_ButtonFrame_Main_Sliced.png");
            }

            return _abilityFrameSprite;
        }

        #endregion

        #region Row helpers

        private static void CreateSummaryTextRow(Transform parent, string caption, string value)
        {
            GameObject row = CreateUIObject($"Row_{caption}", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 40f);

            Text captionLabel = CreateLabel(row.transform, "Caption", caption, SmallFontSize, TextDimColor);
            SetSize(captionLabel.gameObject, 320f, 40f);

            Text valueLabel = CreateLabel(row.transform, "Value", value, BodyFontSize, TextDimColor);
            LayoutElement valueElement = SetSize(valueLabel.gameObject, 0f, 40f);
            valueElement.flexibleWidth = 1f;
        }

        /// <summary>
        /// A captioned grid of icons that wraps onto as many rows as it needs.
        /// </summary>
        private static Transform CreateSummaryIconRow(Transform parent, string caption, int cellSize)
        {
            Text captionLabel = CreateLabel(parent, $"Caption_{caption}", caption, SmallFontSize, TextDimColor);
            SetSize(captionLabel.gameObject, 0f, 32f);

            GameObject grid = CreateUIObject($"Grid_{caption}", parent);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(8f, 8f);
            layout.padding = new RectOffset(2, 2, 2, 2);

            var fitter = grid.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement element = grid.AddComponent<LayoutElement>();
            element.flexibleHeight = 0f;
            element.minHeight = cellSize + 8f;

            return grid.transform;
        }

        private static void AddTextTooltip(GameObject target, string content)
        {
            Image raycastTarget = target.GetComponent<Image>();
            if (raycastTarget != null)
            {
                raycastTarget.raycastTarget = true;
            }

            var trigger = target.AddComponent<PersonnelTooltipTrigger>();
            trigger.Content = content;
        }

        #endregion
    }
}
