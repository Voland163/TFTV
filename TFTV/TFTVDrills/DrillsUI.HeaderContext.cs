using Base.Entities.Abilities;
using Base.UI;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private sealed class DrillConfirmationContext
        {
            public TacticalAbilityDef Ability;
            public bool BaseAbilityLearned;
            public TacticalAbilityDef ReplacementAbility;
            public int SkillPointCost;
        }

        public sealed class HeaderContext
        {
            public bool BaseAbilityLearned;
            public int BaseAbilityCost;
            public int DrillSkillPointCost;
            public int AbilityLevel;
            public bool SlotUnlocked;
            public bool CanLearnBaseAbility;
            public bool CanAffordBaseAbility;
            public string HeaderLabel;
            public string MissingRequirements;
            public AbilityTrack Track;
            public AbilityTrackSkillEntryElement EntryElement;
            public AbilityTrackSlot Slot;
            public UIModuleCharacterProgression Ui;
            public TacticalAbilityDef Ability;

            public bool CanPurchaseBaseAbility => BaseAbilityLearned || (SlotUnlocked && CanLearnBaseAbility && CanAffordBaseAbility);
        }

        private static string DetermineDrillActionLabel(ConfirmBuyAbilityDataBind.Data data, TacticalAbilityDef ability, DrillConfirmationContext context)
        {
            bool isReplacement = false;

            if (context != null && context.Ability == ability)
            {
                isReplacement = context.BaseAbilityLearned;
            }
            else if (data.AbilitySlot?.Ability != null && data.AbilitySlot.Ability != ability)
            {
                isReplacement = true;
            }

            return isReplacement ? "REPLACE DRILL" : "ACQUIRE DRILL";
        }

        private static Text ResolveConfirmationHeaderText(ConfirmBuyAbilityDataBind bind, UIModal modal)
        {
            if (bind == null)
            {
                return null;
            }

            Text header = TryGetHeaderFromFields(bind);
            if (header != null)
            {
                return header;
            }

            Text stringMatch = null;
            if (modal != null)
            {
                foreach (var text in modal.GetComponentsInChildren<Text>(true))
                {
                    if (text == null || IsLikelyAbilityField(text, bind))
                    {
                        continue;
                    }

                    if (IsHeaderNameMatch(text.gameObject?.name) || MatchesHeaderLocalization(text))
                    {
                        return text;
                    }

                    if (IsConfirmationHeaderText(text.text))
                    {
                        stringMatch = stringMatch ?? text;
                    }
                }
            }

            if (stringMatch == null)
            {
                string abilityInfo = bind.AbilityNameText != null ? bind.AbilityNameText.text : "<unknown ability>";
                TFTVLogger.Debug($"[DrillsUI] Unable to locate confirmation header text for drill modal (ability: {abilityInfo}).");
            }

            return stringMatch;
        }

        private static Text TryGetHeaderFromFields(ConfirmBuyAbilityDataBind bind)
        {
            var fields = bind.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Text stringMatch = null;
            foreach (var field in fields)
            {
                if (!typeof(Text).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (!(field.GetValue(bind) is Text text) || text == null)
                {
                    continue;
                }

                if (text == bind.AbilityNameText || text == bind.AbilitiyDescriptionText)
                {
                    continue;
                }

                if (IsHeaderNameMatch(field.Name) || IsHeaderNameMatch(text.gameObject?.name) || MatchesHeaderLocalization(text))
                {
                    return text;
                }

                if (IsConfirmationHeaderText(text.text))
                {
                    stringMatch = stringMatch ?? text;
                }
            }

            return stringMatch;
        }

        private static bool IsConfirmationHeaderText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            return trimmed.Equals("ACQUIRE ABILITY", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("REPLACE ABILITY", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("ACQUIRE DRILL", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("REPLACE DRILL", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHeaderNameMatch(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string normalized = value.Replace(" ", string.Empty);
            normalized = normalized.ToLowerInvariant();
            foreach (var keyword in HeaderNameKeywords)
            {
                if (normalized.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLikelyAbilityField(Text text, ConfirmBuyAbilityDataBind bind)
        {
            if (text == null)
            {
                return false;
            }

            if (text == bind?.AbilityNameText || text == bind?.AbilitiyDescriptionText)
            {
                return true;
            }

            if (IsHeaderNameMatch(text.gameObject?.name))
            {
                return false;
            }

            string objectName = text.gameObject?.name;
            if (!string.IsNullOrEmpty(objectName))
            {
                string normalizedName = objectName.Replace(" ", string.Empty).ToLowerInvariant();
                foreach (var keyword in AbilityFieldNameKeywords)
                {
                    if (normalizedName.Contains(keyword))
                    {
                        return true;
                    }
                }
            }

            var localized = text.GetComponent<LocalizedTextBind>();
            if (localized != null && !string.IsNullOrEmpty(localized.LocalizationKey))
            {
                string key = localized.LocalizationKey.ToUpperInvariant();
                if (key.Contains("ABILITY_NAME")
                    || key.Contains("ABILITY_DESCRIPTION")
                    || key.Contains("ABILITY_DESC")
                    || key.Contains("SKILL_DESCRIPTION")
                    || key.Contains("SKILL_DESC"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesHeaderLocalization(Text text)
        {
            if (text == null)
            {
                return false;
            }

            var localized = text.GetComponent<LocalizedTextBind>();
            if (localized == null || string.IsNullOrEmpty(localized.LocalizationKey))
            {
                return false;
            }

            string key = localized.LocalizationKey;
            foreach (var knownKey in KnownHeaderLocalizationKeys)
            {
                if (string.Equals(key, knownKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            string upper = key.ToUpperInvariant();
            return upper.Contains("CONFIRM") && upper.Contains("ABILITY");
        }

        private static readonly string[] HeaderNameKeywords = { "header", "title", "question" };
        private static readonly string[] AbilityFieldNameKeywords = { "abilityname", "abilitydescription", "abilitydesc", "skillname", "skilldescription", "skilldesc" };
        private static readonly string[] KnownHeaderLocalizationKeys =
        {
            "UI_CHARACTERPROGRESSION_CONFIRM_ABILITY_TITLE",
            "CHARACTERPROGRESSION/CONFIRM_ABILITY_TITLE",
            "UI/CHARACTERPROGRESSION/CONFIRMABILITY/TITLE"
        };
    }
}
