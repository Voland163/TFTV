using Base.Core;
using Base.Entities.Abilities;
using Base.Input;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        [HarmonyPatch(typeof(AbilityTrackSkillEntryElement), nameof(AbilityTrackSkillEntryElement.OnPointerClick))]
        public static class AbilityTrackSkillEntryElement_OnPointerClick_Patch
        {
            public static bool Prefix(AbilityTrackSkillEntryElement __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                try
                {
                    var ui = __instance.GetComponentInParent<UIModuleCharacterProgression>();
                    if (ui == null)
                    {
                        return true;
                    }

                    var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");

                    if (character?.Progression == null)
                    {
                        return true;
                    }

                    var slot = ElementHelpers.FindSlot(__instance);
                    var ability = __instance.AbilityDef ?? slot?.Ability;

                    var (track, resolvedSlot) = CharacterLookup.FindTrackSlot(character, ability);
                    if (resolvedSlot != null)
                    {
                        slot = resolvedSlot;
                    }

                    if (ability == null || slot == null)
                    {
                        return true;
                    }

                    if (!CharacterLookup.IsPersonalTrack(track, character.Progression.PersonalAbilityTrack))
                    {
                        return true;
                    }

                    var source = CharacterLookup.ResolveTrackSource(track);
                    if (source != AbilityTrackSource.Personal)
                    {
                        return true;
                    }

                    int abilityLevel = CharacterLookup.GetAbilityLevel(track, slot);
                    if (abilityLevel > 0 && character.Progression.LevelProgression.Level < abilityLevel)
                    {
                        return true;
                    }

                    bool baseAbilityLearned = character.Progression.Abilities.Contains(ability);
                    int baseAbilityCost = character.Progression.GetAbilitySlotCost(slot);


                    GeoPhoenixFaction phoenixFaction = character?.Faction?.GeoLevel?.PhoenixFaction;
                    List<TacticalAbilityDef> availableChoices = DrillsDefs.GetAvailableDrills(phoenixFaction, character);

                    DrillSwapUI.Show(ui, slot, ability, availableChoices, __instance, baseAbilityLearned, track, abilityLevel, baseAbilityCost);
                    return false;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(AbilityTrackSkillEntryElement), "SetSkillState")]
        public static class AbilityTrackSkillEntryElement_SetSkillState_Patch
        {
            public static void Postfix(AbilityTrackSkillEntryElement __instance, bool isAvailable, bool isBuyable)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn || __instance == null)
                {
                    return;
                }

                try
                {
                    var ui = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.CharacterProgressionModule;

                    if (ui == null)
                    {
                        return;
                    }



                    var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");

                    MethodInfo methodInfo = typeof(AbilityTrackSkillEntryElement).GetMethod("SetAnimator", BindingFlags.NonPublic | BindingFlags.Instance);

                    var phoenixFaction = character?.Faction?.GeoLevel?.PhoenixFaction;
                    var ability = __instance.AbilityDef ?? ElementHelpers.FindSlot(__instance)?.Ability;
                    var availableImage = __instance.Available;
                    bool shouldShowIndicator = DrillIndicator.ShouldShow(character, phoenixFaction, ability, availableImage);

                    if (_originalAvailableImage != null)
                    {
                        availableImage.sprite = _originalAvailableImage;
                    }

                    if (shouldShowIndicator)
                    {
                        if (_originalAvailableImage == null)
                        {
                            _originalAvailableImage = availableImage.sprite;
                        }

                        availableImage.sprite = DrillsDefs._drillAvailable;
                        availableImage.gameObject.SetActive(true);
                        __instance.AvailableSkill = true;
                    }
                    else
                    {
                        availableImage.gameObject.SetActive(isAvailable && isBuyable);
                        __instance.AvailableSkill = isAvailable;
                        availableImage.sprite = _originalAvailableImage;
                    }

                    if (ability != null && DrillsDefs.Drills != null && DrillsDefs.Drills.Contains(ability))
                    {
                        if (character != null && DrillsDefs.CharacterHasDrill(character, ability) && __instance.SkillIcon != null)
                        {
                            __instance.SkillIcon.color = DrillPulseColor;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(ConfirmBuyAbilityDataBind), nameof(ConfirmBuyAbilityDataBind.ModalShowHandler))]
        public static class ConfirmBuyAbilityDataBind_ModalShowHandler_Patch
        {
            public static void Postfix(ConfirmBuyAbilityDataBind __instance, UIModal modal)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn || __instance == null || modal == null)
                {
                    return;
                }

                try
                {
                    if (!(modal.Data is ConfirmBuyAbilityDataBind.Data data))
                    {
                        return;
                    }

                    var ability = data.Ability;
                    var confirmationContext = _pendingDrillConfirmation;
                    var drills = DrillsDefs.Drills;
                    if (ability == null || drills == null || !drills.Contains(ability))
                    {
                        _pendingDrillConfirmation = null;
                        return;
                    }

                    string abilityName = ability.ViewElementDef?.DisplayName1?.Localize() ?? ability.name ?? string.Empty;
                    string pulseHex = ColorUtility.ToHtmlStringRGB(DrillPulseColor);

                    if (__instance.AbilityNameText != null)
                    {
                        __instance.AbilityNameText.supportRichText = true;
                        __instance.AbilityNameText.text = string.Format("<color=#{0}>{1}</color>", pulseHex, abilityName);
                    }

                    if (__instance.AbilitiyDescriptionText != null)
                    {
                        __instance.AbilitiyDescriptionText.supportRichText = true;
                        string description = ability.ViewElementDef?.Description?.Localize() ?? string.Empty;
                        if (confirmationContext != null)
                        {
                            if (confirmationContext.ReplacementAbility != null)
                            {
                                string replacementName = confirmationContext.ReplacementAbility.ViewElementDef?.DisplayName1?.Localize() ?? confirmationContext.ReplacementAbility.name ?? string.Empty;
                                description += string.Format("\n\n<color=#{0}><b>Replaces:</b> {1}</color>", pulseHex, replacementName);
                            }

                            if (confirmationContext.SkillPointCost > 0)
                            {
                                description += string.Format("\n\n<color=#{0}><b>Cost:</b> {1} SP</color>", pulseHex, confirmationContext.SkillPointCost);
                            }
                        }

                        __instance.AbilitiyDescriptionText.text = description;
                    }

                    _pendingDrillConfirmation = null;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(AbilityTrackSkillEntryElement), "LateUpdate")]
        public static class AbilityTrackSkillEntryElement_LateUpdate_Patch
        {
            public static void Prefix(AbilityTrackSkillEntryElement __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                try
                {
                    DrillIndicator.Update(__instance);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }
    }
}
