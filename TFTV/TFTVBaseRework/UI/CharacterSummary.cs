using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// The dossier shown before a decision about a character is taken: who they are, what they are
    /// carrying, and what the decision costs them. Built in the same idiom as the Haven Recruits
    /// panel - stat chips on one line, everything else as icons that explain themselves on hover.
    /// </summary>
    public static partial class PersonnelManagementUI
    {
        private const float SummaryIconSize = 58f;
        private const float SummaryRowLabelWidth = 250f;

        internal static void CreateCharacterSummary(Transform parent, GeoCharacter character,
            IEnumerable<GeoItem> itemsGoingToStorage)
        {
            if (character == null)
            {
                return;
            }

            try
            {
                GameObject panel = CreateFramedPanel(parent, "CharacterSummary", out Transform content,
                    borderThickness: 2f, padding: 10, spacing: 8f);
                LayoutElement panelElement = panel.GetComponent<LayoutElement>() ?? panel.AddComponent<LayoutElement>();
                panelElement.flexibleHeight = 0f;

                var fitter = panel.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateSummaryIdentity(content, character);
                CreateSummaryStats(content, character);
                CreateSummaryWeapons(content, character);
                CreateSummaryAbilities(content, character);
                CreateSummaryStorage(content, itemsGoingToStorage);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        #region Identity and stats

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
            foreach (ViewElementDef classView in classViews ?? (ICollection<ViewElementDef>)new List<ViewElementDef>())
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
                SetSize(iconGO, 52f, 52f);
            }

            Text name = CreateLabel(row.transform, "Name", character.DisplayName, TitleFontSize, TextPrimaryColor);
            LayoutElement nameElement = SetSize(name.gameObject, 0f, 64f);
            nameElement.flexibleWidth = 1f;

            Text classLine = CreateLabel(row.transform, "Class", DescribeClass(character), BodyFontSize, AccentOrangeColor,
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

            int level = character.LevelProgression?.Level ?? 1;
            parts.Add($"Level {level}");

            return string.Join(" / ", parts);
        }

        private static void CreateSummaryStats(Transform parent, GeoCharacter character)
        {
            GameObject row = CreateUIObject("Stats", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetSize(row, 0f, 74f);

            CharacterStats stats = character.CharacterStats;
            if (stats == null)
            {
                return;
            }

            CreateStatChip(row.transform, "HP", $"{stats.Health?.IntValue ?? 0}", TextPrimaryColor,
                "Health. What the operative can take before going down.");
            CreateStatChip(row.transform, "STR", $"{stats.Endurance?.IntValue ?? 0}", TextPrimaryColor,
                "Strength. Carrying capacity and the base for health.");
            CreateStatChip(row.transform, "WILL", $"{stats.Willpower?.IntValue ?? 0}", TextPrimaryColor,
                "Willpower. Resistance to panic and the pool that pays for abilities.");
            CreateStatChip(row.transform, "SPD", $"{stats.Speed?.IntValue ?? 0}", TextPrimaryColor,
                "Speed. Distance covered per action point.");
            CreateStatChip(row.transform, "ACC", $"{stats.Accuracy?.IntValue ?? 0}%", TextPrimaryColor,
                "Accuracy. Applied to every shot the operative takes.");

            int delirium = stats.Corruption?.IntValue ?? 0;
            CreateStatChip(row.transform, "DELIRIUM", $"{delirium}/{stats.Willpower?.IntValue ?? 0}",
                delirium > 0 ? AccentOrangeColor : TextDimColor,
                "Delirium against willpower. At willpower the operative is lost to madness.");
        }

        private static void CreateStatChip(Transform parent, string caption, string value, Color valueColor, string tooltip)
        {
            GameObject chip = CreateUIObject($"Stat_{caption}", parent);
            var background = chip.AddComponent<Image>();
            background.color = RowFillColor;

            var layout = chip.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetSize(chip, 0f, 74f);
            chip.GetComponent<LayoutElement>().flexibleWidth = 1f;

            Text captionLabel = CreateLabel(chip.transform, "Caption", caption, SmallFontSize, TextDimColor,
                TextAnchor.MiddleCenter);
            SetSize(captionLabel.gameObject, 0f, 26f);

            Text valueLabel = CreateLabel(chip.transform, "Value", value, TitleFontSize, valueColor,
                TextAnchor.MiddleCenter);
            SetSize(valueLabel.gameObject, 0f, 40f);

            AddTooltip(chip, $"{caption}\n\n{tooltip}");
        }

        #endregion

        #region Weapons, abilities and gear

        private static void CreateSummaryWeapons(Transform parent, GeoCharacter character)
        {
            List<GeoItem> weapons = character.EquipmentItems
                .Where(item => item?.ItemDef is WeaponDef)
                .ToList();

            string primary = weapons.Count > 0 ? DescribeItem(weapons[0]) : "none";
            string secondary = weapons.Count > 1 ? DescribeItem(weapons[1]) : "none";

            CreateSummaryTextRow(parent, "WEAPONS", $"Primary: {primary}    Secondary: {secondary}");
        }

        private static void CreateSummaryAbilities(Transform parent, GeoCharacter character)
        {
            List<TacticalAbilityDef> abilities = (character.Progression?.Abilities ?? new List<TacticalAbilityDef>())
                .Where(ability => ability?.ViewElementDef?.SmallIcon != null)
                .Where(ability => !string.IsNullOrEmpty(ability.ViewElementDef.DisplayName1?.Localize()))
                .ToList();

            Transform grid = CreateSummaryGridRow(parent, "ABILITIES AND PERKS", abilities.Count);
            if (grid == null)
            {
                return;
            }

            foreach (TacticalAbilityDef ability in abilities)
            {
                string title = ability.ViewElementDef.DisplayName1.Localize();
                string description = ability.ViewElementDef.Description?.Localize();
                CreateSummaryIcon(grid, ability.ViewElementDef.SmallIcon, title,
                    string.IsNullOrEmpty(description) ? title : $"{title}\n\n{description}");
            }
        }

        private static void CreateSummaryStorage(Transform parent, IEnumerable<GeoItem> itemsGoingToStorage)
        {
            List<GeoItem> items = itemsGoingToStorage?.ToList() ?? new List<GeoItem>();

            Transform grid = CreateSummaryGridRow(parent, "RETURNED TO STORAGE", items.Count);
            if (grid == null)
            {
                return;
            }

            foreach (GeoItem item in items)
            {
                ViewElementDef view = item?.ItemDef?.ViewElementDef;
                string title = DescribeItem(item);
                string description = view?.Description?.Localize();
                CreateSummaryIcon(grid, view?.InventoryIcon ?? view?.SmallIcon, title,
                    string.IsNullOrEmpty(description) ? title : $"{title}\n\n{description}");
            }
        }

        private static string DescribeItem(GeoItem item)
        {
            return item?.ItemDef?.ViewElementDef?.DisplayName1?.Localize()
                   ?? item?.ItemDef?.name
                   ?? "unknown";
        }

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
            SetSize(row, 0f, 44f);

            Text captionLabel = CreateLabel(row.transform, "Caption", caption, SmallFontSize, TextDimColor);
            SetSize(captionLabel.gameObject, SummaryRowLabelWidth, 44f);

            Text valueLabel = CreateLabel(row.transform, "Value", value, BodyFontSize, TextPrimaryColor);
            LayoutElement valueElement = SetSize(valueLabel.gameObject, 0f, 44f);
            valueElement.flexibleWidth = 1f;
        }

        /// <summary>
        /// A captioned row of icons. Returns the grid to fill, or null when there is nothing to show -
        /// an empty heading is worse than no heading.
        /// </summary>
        private static Transform CreateSummaryGridRow(Transform parent, string caption, int iconCount)
        {
            if (iconCount <= 0)
            {
                CreateSummaryTextRow(parent, caption, "none");
                return null;
            }

            Text captionLabel = CreateLabel(parent, $"Caption_{caption}", caption, SmallFontSize, TextDimColor);
            SetSize(captionLabel.gameObject, 0f, 32f);

            GameObject grid = CreateUIObject($"Grid_{caption}", parent);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(SummaryIconSize, SummaryIconSize);
            layout.spacing = new Vector2(6f, 6f);
            layout.padding = new RectOffset(2, 2, 2, 2);

            var fitter = grid.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement element = grid.AddComponent<LayoutElement>();
            element.flexibleHeight = 0f;

            return grid.transform;
        }

        private static void CreateSummaryIcon(Transform parent, Sprite icon, string name, string tooltip)
        {
            GameObject cell = CreateUIObject($"Icon_{name}", parent);
            var background = cell.AddComponent<Image>();
            background.color = RowFillColor;

            if (icon != null)
            {
                GameObject iconGO = CreateUIObject("Img", cell.transform);
                var image = iconGO.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                Stretch(image.rectTransform, 5f);
            }
            else
            {
                Text initial = CreateLabel(cell.transform, "Text", name.Substring(0, 1), BodyFontSize, TextPrimaryColor,
                    TextAnchor.MiddleCenter);
                Stretch(initial.rectTransform);
            }

            AddTooltip(cell, tooltip);
        }

        private static void AddTooltip(GameObject target, string content)
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
