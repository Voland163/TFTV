using Base.Entities.Abilities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
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

            if (modal != null)
            {
                foreach (var text in modal.GetComponentsInChildren<Text>(true))
                {
                    if (text == null || text == bind.AbilityNameText || text == bind.AbilitiyDescriptionText)
                    {
                        continue;
                    }

                    if (IsConfirmationHeaderText(text.text))
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        private static Text TryGetHeaderFromFields(ConfirmBuyAbilityDataBind bind)
        {
            var fields = bind.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
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

                if (IsConfirmationHeaderText(text.text))
                {
                    return text;
                }
            }

            return null;
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
    }
}
