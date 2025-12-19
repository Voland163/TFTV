using Base.Core;
using Base.Entities.Abilities;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        private static class ElementHelpers
        {
            public static AbilityTrackSlot FindSlot(AbilityTrackSkillEntryElement element)
            {
                if (element == null)
                {
                    return null;
                }

                var field = element.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(fi => typeof(AbilityTrackSlot).IsAssignableFrom(fi.FieldType));

                return field != null ? (AbilityTrackSlot)field.GetValue(element) : null;
            }

            public static Image FindChildImage(Transform parent, string name)
            {
                if (parent == null || string.IsNullOrEmpty(name))
                {
                    return null;
                }

                foreach (Transform child in parent)
                {
                    if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return child.GetComponent<Image>();
                    }

                    var nested = FindChildImage(child, name);
                    if (nested != null)
                    {
                        return nested;
                    }
                }

                return null;
            }
        }

        private static class Reflection
        {
            public static T GetPrivate<T>(object obj, string field)
            {
                if (obj == null || string.IsNullOrEmpty(field))
                {
                    return default;
                }

                var info = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                return info != null ? (T)info.GetValue(obj) : default;
            }

            public static void SetPrivate<T>(object obj, string field, T value)
            {
                if (obj == null || string.IsNullOrEmpty(field))
                {
                    return;
                }

                var info = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                info?.SetValue(obj, value);
            }

            public static void CallPrivate(object obj, string method)
            {
                if (obj == null || string.IsNullOrEmpty(method))
                {
                    return;
                }

                obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(obj, null);
            }
        }



        private static void WireButton(GameObject buttonGO, TacticalAbilityDef def, Action onClick)
        {
            if (buttonGO == null || def == null)
            {
                return;
            }

            var pgb = buttonGO.GetComponentInChildren<PhoenixGeneralButton>();
            if (pgb != null)
            {
                var text = pgb.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name;
                }

                var img = pgb.GetComponentInChildren<Image>();
                if (img != null && def.ViewElementDef?.LargeIcon != null)
                {
                    img.sprite = def.ViewElementDef.LargeIcon;
                }

                pgb.BaseButton.onClick.AddListener(() => onClick?.Invoke());
                return;
            }

            var btn = buttonGO.GetComponentInChildren<Button>() ?? buttonGO.AddComponent<Button>();
            var label = buttonGO.GetComponentInChildren<Text>();
            if (label == null)
            {
                var lg = new GameObject("Label", typeof(RectTransform), typeof(Text));
                lg.transform.SetParent(buttonGO.transform, false);
                label = lg.GetComponent<Text>();
            }

            label.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private static void ShowDrillConfirmation(UIModuleCharacterProgression ui, AbilityTrackSlot slot,
             TacticalAbilityDef original, TacticalAbilityDef replacement, bool baseAbilityLearned, int skillPointCost)
        {
            if (ui == null || slot == null || original == null || replacement == null)
            {
                return;
            }

            AbilityCharacterProgressionDef progressionData = null;
            int originalCost = 0;
            bool costOverridden = false;

            try
            {
                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                if (character?.Progression == null)
                {
                    TryPerformSwap(ui, slot, original, replacement, skillPointCost);
                    return;
                }

                var levelController = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                var view = levelController?.View;
                if (view == null)
                {
                    TryPerformSwap(ui, slot, original, replacement, skillPointCost);
                    return;
                }

                progressionData = replacement.CharacterProgressionData;
                if (progressionData != null)
                {
                    originalCost = progressionData.SkillPointCost;
                    progressionData.SkillPointCost = skillPointCost;
                    costOverridden = true;
                }

                ConfirmBuyAbilityDataBind.Data data = new ConfirmBuyAbilityDataBind.Data
                {
                    Progression = character.Progression,
                    AbilitySlot = slot,
                    Ability = replacement,
                    CostType = ResourceType.None
                };

                _pendingDrillConfirmation = new DrillConfirmationContext
                {
                    Ability = replacement,
                    ReplacementAbility = original,
                    SkillPointCost = Math.Max(0, skillPointCost),
                    BaseAbilityLearned = baseAbilityLearned
                };

                view.OpenModal(ModalType.CharacterProgressionConfirmCharacter, res =>
                {
                    if (costOverridden && progressionData != null)
                    {
                        progressionData.SkillPointCost = originalCost;
                        costOverridden = false;
                    }

                    if (res == ModalResult.Confirm)
                    {
                        TryPerformSwap(ui, slot, original, replacement, skillPointCost);
                    }
                    else
                    {
                        Reflection.CallPrivate(ui, "RefreshAbilityTracks");
                    }
                }, data, 100, true, false);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                if (costOverridden && progressionData != null)
                {
                    progressionData.SkillPointCost = originalCost;
                }
                _pendingDrillConfirmation = null;
                TryPerformSwap(ui, slot, original, replacement, skillPointCost);
            }
        }

        private static void TryPerformSwap(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original,
            TacticalAbilityDef replacement, int skillPointCost)
        {
            try
            {
                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = Reflection.GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);
                if (character?.Progression == null)
                {
                    return;
                }

                if (!DrillsUnlock.IsDrillUnlocked(phoenixFaction, character, replacement))
                {
                    TFTVLogger.Always($"[TFTV Drills] Attempted to swap to locked drill {replacement?.name}; aborting swap.");
                    return;
                }

                if (DrillsUnlock.WouldBreakWeaponProficiencyRequirement(character, original, out var blockingDrills))
                {
                    string abilityName = original?.ViewElementDef?.DisplayName1?.Localize() ?? original?.name ?? "the selected ability";
                    string drillNames = string.Join(", ", blockingDrills.Distinct().Where(name => !string.IsNullOrEmpty(name)));
                    string message = string.IsNullOrEmpty(drillNames)
                        ? $"Cannot replace {abilityName} because it is required for an acquired drill."
                        : $"Cannot replace {abilityName} because it is required for: {drillNames}.";

                    GameUtl.GetMessageBox()?.ShowSimplePrompt(message, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    Reflection.CallPrivate(ui, "RefreshAbilityTracks");
                    ui.RefreshStatPanel();
                    return;
                }

                if (DrillsUnlock.TargetDrillLosesWeaponProficiencyRequirement(character, replacement, original, out var targetDrillName))
                {
                    string abilityName = original?.ViewElementDef?.DisplayName1?.Localize() ?? original?.name ?? "the selected ability";
                    string message = string.IsNullOrEmpty(targetDrillName)
                        ? $"Cannot replace {abilityName} because it is required for the selected drill."
                        : $"Cannot replace {abilityName} because it is required for: {targetDrillName}.";

                    GameUtl.GetMessageBox()?.ShowSimplePrompt(message, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    Reflection.CallPrivate(ui, "RefreshAbilityTracks");
                    ui.RefreshStatPanel();
                    return;
                }

                int currSP = Reflection.GetPrivate<int>(ui, "_currentSkillPoints");
                int currFP = Reflection.GetPrivate<int>(ui, "_currentFactionPoints");
                int originalCurrSP = currSP;
                int originalCurrFP = currFP;

                if (skillPointCost > 0 && (currSP + currFP) < skillPointCost)
                {
                    Debug.LogWarning("[TFTV] Not enough SP/FS for swap; aborting.");
                    GameUtl.GetMessageBox()?.ShowSimplePrompt("Not enough Skill Points.", MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    Reflection.CallPrivate(ui, "RefreshAbilityTracks");
                    ui.RefreshStatPanel();
                    return;
                }

                List<TacticalAbilityDef> abilities = Traverse.Create(character.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();
                bool replacedExisting = abilities.Contains(original);

                if (replacedExisting)
                {
                    abilities.Remove(original);
                }

                slot.Ability = replacement;

                bool learnedNow = false;
                if (!character.Progression.Abilities.Contains(replacement))
                {
                    character.Progression.LearnAbility(slot);
                    learnedNow = character.Progression.Abilities.Contains(replacement);
                }
                else
                {
                    learnedNow = true;
                }

                if (!learnedNow)
                {
                    if (replacedExisting)
                    {
                        abilities.Add(original);
                    }

                    slot.Ability = original;

                    Reflection.SetPrivate(ui, "_currentSkillPoints", originalCurrSP);
                    Reflection.SetPrivate(ui, "_currentFactionPoints", originalCurrFP);

                    Debug.LogWarning("[TFTV] Swap failed (ability not learned); aborting without charging.");
                    Reflection.CallPrivate(ui, "RefreshAbilityTracks");
                    ui.RefreshStatPanel();
                    return;
                }

                if (skillPointCost > 0)
                {
                    int remaining = skillPointCost;
                    if (currSP >= remaining)
                    {
                        currSP -= remaining;
                        remaining = 0;
                    }
                    else
                    {
                        remaining -= currSP;
                        currSP = 0;
                        currFP = Math.Max(0, currFP - remaining);
                    }

                    Reflection.SetPrivate(ui, "_currentSkillPoints", currSP);
                    Reflection.SetPrivate(ui, "_currentFactionPoints", currFP);
                }

                if (replacedExisting && TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                {
                    TFTVCommonMethods.SetStaminaToZero(character);
                    ui.SetStatusesPanel();
                }

                ui.SetStatusesPanel();
                ui.CommitStatChanges();
                ui.RefreshStatPanel();
                Reflection.CallPrivate(ui, "SetAbilityTracks");
            }
            catch (Exception e)
            {
                TFTVLogger.Always($"[TFTV] Ability swap failed: {e}");
            }
        }
    }
}
