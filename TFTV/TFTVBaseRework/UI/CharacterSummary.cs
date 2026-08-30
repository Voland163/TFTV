using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Levels;
using Base.Entities;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
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

        private const float StatCaptionWidth = 200f;
        private const float StatValueWidth = 130f;
        private const string SlotFactoryName = "TFTV_PersonnelSlotFactory";

        // The Haven Recruits palette: amber for a value, green or red for what changes it.
        private static readonly Color StatValueColor = new Color32(0xD0, 0xA4, 0x56, 0xFF);
        private static readonly Color StatGainColor = Color.green;
        private static readonly Color StatLossColor = Color.red;
        private static readonly Color AbilityLockedColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);

        // The violet the Haven Recruits panel marks delirium with.
        private static readonly Color DeliriumColor = new Color32(0xA2, 0x48, 0xD1, 0xFF);

        private static readonly Dictionary<string, Sprite> _statIcons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static Sprite _abilityFrameSprite;

        /// <summary>
        /// Optional <paramref name="projectedStats"/> shows what training would leave the character
        /// with, as "21 (26)" against the current figure.
        ///
        /// <paramref name="showClassAndAbilities"/> is false for personnel who have never served: the
        /// class they carry is a placeholder and their background perks are only rolled when they
        /// deploy, so showing either before that would promise something the game has not decided.
        /// </summary>
        internal static void CreateCharacterSummary(Transform parent, GeoCharacter character,
            IEnumerable<GeoItem> itemsGoingToStorage, ProjectedStats projectedStats = null,
            bool showClassAndAbilities = true)
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

                CreateSummaryIdentity(content, character, showClassAndAbilities);
                CreateSummaryProgress(content, character);
                CreateSummaryStats(content, character, projectedStats);

                if (showClassAndAbilities)
                {
                    CreateSummaryAbilities(content, character);
                }

                CreateSummaryStorage(content, character, itemsGoingToStorage);
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

        private static void CreateSummaryIdentity(Transform parent, GeoCharacter character, bool showClass)
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

            ICollection<ViewElementDef> classViews = showClass ? character.ClassViewElementDefs : null;
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

            if (showClass)
            {
                Text level = CreateLabel(row.transform, "Level", (character.LevelProgression?.Level ?? 1).ToString(),
                    TitleFontSize, AccentOrangeColor);
                SetSize(level.gameObject, 44f, 64f);
            }

            // Identity.Name is the character's full name; DisplayName is the same string.
            string fullName = character.Identity?.Name ?? character.DisplayName;
            Text name = CreateLabel(row.transform, "Name", fullName, 38, TextPrimaryColor);
            LayoutElement nameElement = SetSize(name.gameObject, 0f, 64f);
            nameElement.flexibleWidth = 1f;

            if (showClass)
            {
                Text classLine = CreateLabel(row.transform, "Class", DescribeClass(character), BodyFontSize, TextDimColor,
                    TextAnchor.MiddleRight);
                SetSize(classLine.gameObject, 0f, 64f);
            }
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

        /// <summary>
        /// Read exactly as the character screen reads them: the three trained attributes as current
        /// against their ceiling, and perception, accuracy and stealth as the bonuses that armour and
        /// passive abilities contribute. CharacterStats holds tactical values that do not match what
        /// the player is shown on the geoscape, which is why an operative's accuracy came out as 0%.
        /// </summary>
        private static void CreateSummaryStats(Transform parent, GeoCharacter character, ProjectedStats projected)
        {
            CharacterStats stats = character.CharacterStats;
            CharacterProgression progression = character.Progression;
            if (stats == null || progression == null)
            {
                return;
            }

            BaseCharacterStats baseStats = character.GetProgressionBaseStats();
            int strength = (int)(baseStats.Endurance + character.BonusStrength);
            int willpower = (int)(baseStats.Willpower + character.BonusWillpower);
            int speed = (int)(baseStats.Speed + character.BonusSpeed);

            GetDisplayBonuses(character, out int perception, out float accuracy, out float stealth);

            string trainedNote = projected != null
                ? "The figure in brackets is what this training would leave them with."
                : "Current value against the highest this operative can reach.";

            string strengthNote = AppendModifiers(trainedNote, character, StatModificationTarget.Endurance, aspect => aspect.Endurance);
            string willNote = AppendModifiers(trainedNote, character, StatModificationTarget.Willpower, aspect => aspect.WillPower);
            string speedNote = AppendModifiers(trainedNote, character, StatModificationTarget.Speed, aspect => aspect.Speed);

            GameObject grid = CreateUIObject("Stats", parent);
            var gridLayout = grid.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 4f;
            gridLayout.childControlWidth = true;
            gridLayout.childControlHeight = true;
            gridLayout.childForceExpandWidth = true;
            gridLayout.childForceExpandHeight = false;

            int deliriumValue = stats.Corruption?.IntValue ?? 0;
            int rows = deliriumValue > 0 ? 4 : 3;
            SetSize(grid, 0f, (SummaryStatRowHeight + 4f) * rows);

            Transform row1 = CreateStatRow(grid.transform, "StatRow1");
            CreateTrainedStatCell(row1, "Strength", strength,
                progression.GetMaxBaseStat(CharacterBaseAttribute.Strength), projected?.Strength, strengthNote);
            CreateStatCell(row1, "Perception", "Perception", $"+{perception}", null, null,
                AppendModifiers("Sight range added by armour and passive abilities.", character,
                    StatModificationTarget.Perception, aspect => aspect.Perception));

            Transform row2 = CreateStatRow(grid.transform, "StatRow2");
            CreateTrainedStatCell(row2, "Willpower", willpower,
                progression.GetMaxBaseStat(CharacterBaseAttribute.Will), projected?.Willpower, willNote);
            CreateStatCell(row2, "Accuracy", "Accuracy", FormatBonusPercent(accuracy), null, null,
                AppendModifiers("Accuracy added by armour and passive abilities.", character,
                    StatModificationTarget.Accuracy, aspect => aspect.Accuracy, asPercent: true));

            Transform row3 = CreateStatRow(grid.transform, "StatRow3");
            CreateTrainedStatCell(row3, "Speed", speed,
                progression.GetMaxBaseStat(CharacterBaseAttribute.Speed), projected?.Speed, speedNote);
            CreateStatCell(row3, "Stealth", "Stealth", FormatBonusPercent(stealth), null, null,
                AppendModifiers("Stealth added by armour and passive abilities.", character,
                    StatModificationTarget.Stealth, aspect => aspect.Stealth, asPercent: true));

            if (deliriumValue > 0)
            {
                Transform row4 = CreateStatRow(grid.transform, "StatRow4");
                CreateDeliriumCell(row4, character, deliriumValue, Mathf.RoundToInt(TFTVDelirium.CalculateMaxCorruption(character)));
                CreateStatCell(row4, null, string.Empty, null, null, null, null);
            }
        }

        /// <summary>
        /// One of the three trained attributes: "21 / 35", with what training would make of it in
        /// green beside it.
        /// </summary>
        private static void CreateTrainedStatCell(Transform parent, string statName, int current, int max,
            int? projected, string tooltip)
        {
            string projectedText = projected.HasValue && projected.Value != current
                ? projected.Value.ToString()
                : null;

            Color projectedColor = projected.HasValue && projected.Value < current ? StatLossColor : StatGainColor;

            CreateStatCell(parent, statName, statName, $"{current} / {max}", projectedText, projectedColor, tooltip);
        }

        /// <summary>
        /// The character screen writes these as bonuses and shows "---" when there is none.
        /// </summary>
        private static void GetDisplayBonuses(GeoCharacter character, out int perception, out float accuracy, out float stealth)
        {
            float perceptionValue = 0f;
            float accuracyValue = 0f;
            float stealthValue = 0f;
            float perceptionMultiplier = 1f;
            float accuracyMultiplier = 1f;
            float stealthMultiplier = 1f;

            PerceptionComponentDef perceptionComponent = character.TemplateDef?.ComponentSetDef?.GetComponentDef<PerceptionComponentDef>();
            if (perceptionComponent != null)
            {
                perceptionValue += perceptionComponent.PerceptionRange;
            }

            foreach (GeoItem armourItem in character.ArmourItems)
            {
                var itemDef = armourItem?.ItemDef as TacticalItemDef;
                if (itemDef?.BodyPartAspectDef == null)
                {
                    continue;
                }

                perceptionValue += itemDef.BodyPartAspectDef.Perception;
                accuracyValue += itemDef.BodyPartAspectDef.Accuracy;
                stealthValue += itemDef.BodyPartAspectDef.Stealth;
            }

            var modifiers = new List<PassiveModifierAbilityDef>();
            if (character.Progression?.Abilities != null)
            {
                modifiers.AddRange(character.Progression.Abilities.OfType<PassiveModifierAbilityDef>());
            }
            if (character.PassiveModifiers != null)
            {
                modifiers.AddRange(character.PassiveModifiers);
            }

            foreach (PassiveModifierAbilityDef modifier in modifiers)
            {
                if (modifier?.StatModifications == null)
                {
                    continue;
                }

                foreach (ItemStatModification modification in modifier.StatModifications)
                {
                    switch (modification.TargetStat)
                    {
                        case StatModificationTarget.Perception:
                            ApplyModification(modification, ref perceptionValue, ref perceptionMultiplier);
                            break;
                        case StatModificationTarget.Accuracy:
                            ApplyModification(modification, ref accuracyValue, ref accuracyMultiplier);
                            break;
                        case StatModificationTarget.Stealth:
                            ApplyModification(modification, ref stealthValue, ref stealthMultiplier);
                            break;
                    }
                }
            }

            perception = (int)(perceptionValue * perceptionMultiplier);
            accuracy = accuracyValue * accuracyMultiplier;
            stealth = stealthValue * stealthMultiplier;
        }

        private static void ApplyModification(ItemStatModification modification, ref float value, ref float multiplier)
        {
            if (modification.Modification == StatModificationType.Add)
            {
                value += modification.Value;
            }
            else if (modification.Modification == StatModificationType.Multiply)
            {
                multiplier += modification.Value;
            }
        }

        /// <summary>
        /// Adds the Haven Recruits style breakdown to a stat's tooltip: one line per armour piece or
        /// passive ability that moves this stat, and by how much.
        /// </summary>
        private static string AppendModifiers(string note, GeoCharacter character, StatModificationTarget target,
            Func<BodyPartAspectDef, float> fromArmour, bool asPercent = false)
        {
            string breakdown = BuildModifierBreakdown(character, target, fromArmour, asPercent);
            return string.IsNullOrEmpty(breakdown) ? note : $"{note}\n\n{breakdown}";
        }

        private static string BuildModifierBreakdown(GeoCharacter character, StatModificationTarget target,
            Func<BodyPartAspectDef, float> fromArmour, bool asPercent)
        {
            var lines = new List<string>();

            foreach (GeoItem armourItem in character.ArmourItems)
            {
                var itemDef = armourItem?.ItemDef as TacticalItemDef;
                if (itemDef?.BodyPartAspectDef == null)
                {
                    continue;
                }

                AddModifierLine(lines, itemDef.ViewElementDef?.DisplayName1?.Localize() ?? itemDef.name,
                    fromArmour(itemDef.BodyPartAspectDef), asPercent);
            }

            var passives = new List<PassiveModifierAbilityDef>();
            if (character.Progression?.Abilities != null)
            {
                passives.AddRange(character.Progression.Abilities.OfType<PassiveModifierAbilityDef>());
            }
            if (character.PassiveModifiers != null)
            {
                passives.AddRange(character.PassiveModifiers);
            }

            foreach (PassiveModifierAbilityDef passive in passives)
            {
                if (passive?.StatModifications == null)
                {
                    continue;
                }

                float total = passive.StatModifications
                    .Where(modification => modification.TargetStat == target
                        && modification.Modification == StatModificationType.Add)
                    .Sum(modification => modification.Value);

                AddModifierLine(lines, passive.ViewElementDef?.DisplayName1?.Localize() ?? passive.name, total, asPercent);
            }

            return string.Join("\n", lines);
        }

        private static void AddModifierLine(List<string> lines, string source, float value, bool asPercent)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            string colour = value > 0f ? "#00FF00" : "#FF0000";
            string sign = value > 0f ? "+" : "-";

            // Perception, accuracy and stealth are carried as ratios: 0.1 is ten percentage points,
            // and printing it raw is what produced a list of "+0.1" against a +15% stat.
            string amount = asPercent
                ? $"{Mathf.Abs(Mathf.RoundToInt(value * 100f))}%"
                : $"{Mathf.Abs(value):0.#}";

            lines.Add($"{source}: <color={colour}>{sign}{amount}</color>");
        }

        private static string FormatBonusPercent(float ratio)
        {
            if (Mathf.Approximately(ratio, 0f))
            {
                return "---";
            }

            int percent = Mathf.RoundToInt(ratio * 100f);
            return percent > 0 ? $"+{percent}%" : $"{percent}%";
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

            // Fixed label and value columns, so every row in the block lines up with the others.
            Text label = CreateLabel(cell.transform, "Label", $"{caption}:", BodyFontSize, TextPrimaryColor);
            SetSize(label.gameObject, StatCaptionWidth, SummaryStatRowHeight);

            Text value = CreateLabel(cell.transform, "Value", baseText ?? finalText, BodyFontSize, StatValueColor);
            SetSize(value.gameObject, StatValueWidth, SummaryStatRowHeight);

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

        private static void CreateDeliriumCell(Transform parent, GeoCharacter character, int delirium, int maxDelirium)
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
                image.color = DeliriumColor;
                image.preserveAspect = true;
                image.raycastTarget = false;
                SetSize(iconGO, 34f, 34f);
            }

            Text label = CreateLabel(cell.transform, "Label", "Delirium:", BodyFontSize, DeliriumColor);
            SetSize(label.gameObject, StatCaptionWidth, SummaryStatRowHeight);

            Text value = CreateLabel(cell.transform, "Value", $"{delirium} / {maxDelirium}", BodyFontSize, DeliriumColor);
            SetSize(value.gameObject, StatValueWidth, SummaryStatRowHeight);

            // The same explanation the character screen gives, tied to the campaign's delirium level.
            GeoLevelController level = character?.Faction?.GeoLevel;
            string explanation = level != null
                ? $"{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DELIRIUM_EXPLANATION")} {TFTVDelirium.CurrentDeliriumLevel(level)}."
                : TFTVCommonMethods.ConvertKeyToString("KEY_UI_DELIRIUM_EXPLANATION");

            AddTextTooltip(cell, explanation);
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

        /// <summary>
        /// One row per source, as the recruit panel splits them: the class track, the second class,
        /// the personal perks, and everything else the operative has picked up along the way -
        /// delirium perks, affinities, the grunt marker.
        /// </summary>
        private static void CreateSummaryAbilities(Transform parent, GeoCharacter character)
        {
            CharacterProgression progression = character.Progression;
            if (progression == null)
            {
                return;
            }

            var learned = new HashSet<TacticalAbilityDef>(progression.Abilities ?? new List<TacticalAbilityDef>());
            var accounted = new HashSet<TacticalAbilityDef>();

            List<AbilityTrackSlot> mainClass = SlotsOf(progression.MainSpecDef?.AbilityTrack?.AbilitiesByLevel, accounted);
            List<AbilityTrackSlot> secondClass = SlotsOf(progression.SecondarySpecDef?.AbilityTrack?.AbilitiesByLevel, accounted);
            List<AbilityTrackSlot> personal = SlotsOf(progression.PersonalAbilityTrack?.AbilitiesByLevel, accounted);

            // Whatever the tracks do not account for: perks the campaign handed out rather than ones
            // the operative trained into.
            List<TacticalAbilityDef> other = learned
                .Where(ability => ability != null && !accounted.Contains(ability))
                .Where(ability => ability.ViewElementDef?.SmallIcon != null)
                .ToList();

            if (mainClass.Count == 0 && secondClass.Count == 0 && personal.Count == 0 && other.Count == 0)
            {
                CreateSummaryTextRow(parent, "ABILITIES AND PERKS", "none");
                return;
            }

            Text caption = CreateLabel(parent, "Caption_Abilities", "ABILITIES AND PERKS", SmallFontSize, TextDimColor);
            SetSize(caption.gameObject, 0f, 32f);

            CreateAbilityRow(parent, "ClassAbilities", mainClass.Select(slot => slot.Ability), learned);
            CreateAbilityRow(parent, "SecondClassAbilities", secondClass.Select(slot => slot.Ability), learned);
            CreateAbilityRow(parent, "PersonalAbilities", personal.Select(slot => slot.Ability), learned);
            CreateAbilityRow(parent, "OtherAbilities", other, learned);
        }

        private static List<AbilityTrackSlot> SlotsOf(AbilityTrackSlot[] trackSlots, HashSet<TacticalAbilityDef> accounted)
        {
            var slots = new List<AbilityTrackSlot>();
            if (trackSlots == null)
            {
                return slots;
            }

            foreach (AbilityTrackSlot slot in trackSlots)
            {
                if (slot?.Ability?.ViewElementDef?.SmallIcon == null)
                {
                    continue;
                }

                slots.Add(slot);
                accounted.Add(slot.Ability);
            }

            return slots;
        }

        private static void CreateAbilityRow(Transform parent, string name, IEnumerable<TacticalAbilityDef> abilities,
            HashSet<TacticalAbilityDef> learned)
        {
            List<TacticalAbilityDef> list = abilities?
                .Where(ability => ability?.ViewElementDef?.SmallIcon != null)
                .ToList() ?? new List<TacticalAbilityDef>();

            if (list.Count == 0)
            {
                return;
            }

            Transform row = CreateSummaryIconStrip(parent, name, SummaryAbilityIconSize);

            foreach (TacticalAbilityDef ability in list)
            {
                ViewElementDef view = ability.ViewElementDef;

                Image icon = RecruitOverlayManagerHelpers.MakeFixedIcon(row, view.SmallIcon, SummaryAbilityIconSize,
                    EnsureAbilityFrameSprite());
                if (icon == null)
                {
                    continue;
                }

                // Abilities the operative has not earned are dimmed, as on the recruit panel.
                icon.color = learned.Contains(ability) ? Color.white : AbilityLockedColor;

                GameObject triggerTarget = icon.transform.parent != null
                    ? icon.transform.parent.gameObject
                    : icon.gameObject;

                var trigger = triggerTarget.AddComponent<PersonnelAbilityTooltipTrigger>();
                trigger.Ability = ability;
                trigger.View = view;
            }
        }

        /// <summary>
        /// Armour, ready slots and inventory each get their own row, as they do on the recruit panel.
        /// </summary>
        private static void CreateSummaryStorage(Transform parent, GeoCharacter character,
            IEnumerable<GeoItem> itemsGoingToStorage)
        {
            var items = new HashSet<GeoItem>(itemsGoingToStorage ?? Enumerable.Empty<GeoItem>());
            if (items.Count == 0)
            {
                CreateSummaryTextRow(parent, "RETURNED TO STORAGE", "nothing");
                return;
            }

            Text caption = CreateLabel(parent, "Caption_Storage", "RETURNED TO STORAGE", SmallFontSize, TextDimColor);
            SetSize(caption.gameObject, 0f, 32f);

            CreateItemRow(parent, "ArmourRow", character.ArmourItems.Where(items.Contains));
            CreateItemRow(parent, "ReadyRow", character.EquipmentItems.Where(items.Contains));
            CreateItemRow(parent, "InventoryRow", character.InventoryItems.Where(items.Contains));
        }

        private static void CreateItemRow(Transform parent, string name, IEnumerable<GeoItem> items)
        {
            List<GeoItem> list = items?.Where(item => item?.ItemDef != null).ToList() ?? new List<GeoItem>();
            if (list.Count == 0)
            {
                return;
            }

            Transform row = CreateSummaryIconStrip(parent, name, SummaryInventorySlotSize);
            UIGeoItemTooltip tooltip = PersonnelVanillaTooltips.EnsureItemTooltip(row);

            // MakeInventorySlot parks its slot template under whichever transform it is handed, and
            // moves it there again on every call. Handing it a hidden holder keeps that template out
            // of the gear rows, where it took up a cell and pushed the last row along.
            Transform factory = EnsureSlotFactory(parent);

            foreach (GeoItem item in list)
            {
                // The game's own inventory slot, so the gear looks and explains itself exactly as it
                // does everywhere else.
                UIInventorySlot slot = RecruitOverlayManagerHelpers.MakeInventorySlot(factory, item.ItemDef,
                    SummaryInventorySlotSize, "PersonnelStorage", tooltip);

                if (slot != null)
                {
                    slot.transform.SetParent(row, false);
                }

                if (slot != null && tooltip == null)
                {
                    // No game tooltip to hand: name the item at least.
                    ViewElementDef view = item.ItemDef.ViewElementDef;
                    string title = view?.DisplayName1?.Localize() ?? item.ItemDef.name;
                    string description = view?.Description?.Localize();
                    AddTextTooltip(slot.gameObject,
                        string.IsNullOrEmpty(description) ? title : $"{title}\n\n{description}");
                }
            }
        }

        /// <summary>
        /// A zero-sized, layout-ignored holder that inventory slots are built in before being moved
        /// into their row.
        /// </summary>
        private static Transform EnsureSlotFactory(Transform parent)
        {
            Transform existing = parent.Find(SlotFactoryName);
            if (existing != null)
            {
                return existing;
            }

            GameObject factory = CreateUIObject(SlotFactoryName, parent);
            LayoutElement element = factory.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            RectTransform rect = factory.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.zero;

            return factory.transform;
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
        /// One line of icons, wrapping only if it runs out of width.
        /// </summary>
        private static Transform CreateSummaryIconStrip(Transform parent, string name, int cellSize)
        {
            GameObject grid = CreateUIObject($"Grid_{name}", parent);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(8f, 8f);
            layout.padding = new RectOffset(2, 2, 2, 2);
            // Without this a short row is centred against the panel and reads as indented.
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;

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
