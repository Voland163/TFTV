using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
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
        private static class DrillIndicator
        {
            public static bool ShouldShow(GeoCharacter character, GeoPhoenixFaction phoenixFaction, TacticalAbilityDef ability, Image availableImage)
            {
                if (DrillsDefs._drillAvailable == null || availableImage == null || character.IsMutoid)
                {
                    return false;
                }

                if (character?.Progression?.PersonalAbilityTrack == null || ability == null)
                {
                    return false;
                }

                var personalTrack = character.Progression.PersonalAbilityTrack;
                bool isPersonalAbility = personalTrack.AbilitiesByLevel.Any(slot => slot?.Ability == ability);
                bool isDrill = DrillsDefs.Drills.Contains(ability);

                if (!isPersonalAbility && !isDrill)
                {
                    return false;
                }

                if (!character.Progression.Abilities.Contains(ability) && !isDrill)
                {
                    return false;
                }

                return true;


                //  var availableDrills = DrillsDefs.GetAvailableDrills(phoenixFaction, character);
                //  return availableDrills != null && availableDrills.Any(def => def != null && def != ability);
            }

            public static void Update(AbilityTrackSkillEntryElement element)
            {
                if (element == null)
                {
                    return;
                }

                var availableImage = element.Available;
                if (availableImage == null || DrillsDefs._drillAvailable == null)
                {
                    return;
                }

                if (availableImage.sprite != DrillsDefs._drillAvailable)
                {
                    return;
                }

                if (!element.AvailableSkill || !availableImage.gameObject.activeSelf)
                {
                    return;
                }

                availableImage.color = DrillPulseColor;
            }
        }
        private static class CharacterLookup
        {
            public static (AbilityTrack track, AbilityTrackSlot slot) FindTrackSlot(GeoCharacter character, TacticalAbilityDef ability)
            {
                if (character?.Progression?.AbilityTracks == null || ability == null)
                {
                    return (null, null);
                }

                foreach (var track in character.Progression.AbilityTracks)
                {
                    if (track?.AbilitiesByLevel == null)
                    {
                        continue;
                    }

                    foreach (var slot in track.AbilitiesByLevel)
                    {
                        if (slot?.Ability == ability)
                        {
                            return (track, slot);
                        }
                    }
                }

                return (null, null);
            }

            public static bool IsPersonalTrack(AbilityTrack track, AbilityTrack personalTrack)
            {
                if (track == null || personalTrack == null)
                {
                    return false;
                }

                return ReferenceEquals(track, personalTrack) || track == personalTrack;
            }

            public static AbilityTrackSource ResolveTrackSource(AbilityTrack track)
            {
                if (track == null)
                {
                    return AbilityTrackSource.Personal;
                }

                var property = track.GetType().GetProperty("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && typeof(AbilityTrackSource).IsAssignableFrom(property.PropertyType))
                {
                    return (AbilityTrackSource)property.GetValue(track);
                }

                var field = track.GetType().GetField("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && typeof(AbilityTrackSource).IsAssignableFrom(field.FieldType))
                {
                    return (AbilityTrackSource)field.GetValue(track);
                }

                return AbilityTrackSource.Personal;
            }

            public static int GetAbilityLevel(AbilityTrack track, AbilityTrackSlot slot)
            {
                if (track == null || slot == null)
                {
                    return 0;
                }

                try
                {
                    return track.GetAbilityLevel(slot);
                }
                catch
                {
                    return 0;
                }
            }
        }

        private static class DrillSwapUI
        {
            public static void Show(UIModuleCharacterProgression ui,
                AbilityTrackSlot slot, TacticalAbilityDef original,
                List<TacticalAbilityDef> choices, AbilityTrackSkillEntryElement entry,
                bool baseAbilityLearned, AbilityTrack track,
                int abilityLevel, int baseAbilityCost)
            {
                if (ui == null || slot == null || original == null || choices == null)
                {
                    return;
                }

                var popupGO = Reflection.GetPrivate<GameObject>(ui, "DualClassPopupWindow");
                var container = Reflection.GetPrivate<GameObject>(ui, "DualClassButtonsContainer");
                var prefab = Reflection.GetPrivate<GameObject>(ui, "DualClassButtonsPrefab");

                if (popupGO != null && container != null && prefab != null)
                {
                    foreach (Transform child in container.transform)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    HeaderContext headerContext = BuildHeaderContext(ui, slot, original, baseAbilityLearned, track, abilityLevel, baseAbilityCost, entry);
                    Action acquireAction = null;
                    if (!headerContext.BaseAbilityLearned)
                    {
                        acquireAction = () =>
                        {
                            popupGO.SetActive(false);
                            AcquireBaseAbility(ui, slot, original, track, abilityLevel);
                        };
                    }

                    var containerRect = container.GetComponent<RectTransform>();
                    UIBuilder.AddHeader(containerRect, original, headerContext, onAcquire: acquireAction);
                    foreach (var def in choices.Where(def => def != null))
                    {
                        var go = UnityEngine.Object.Instantiate(prefab, container.transform);
                        WireButton(go, def, () =>
                        {
                            popupGO.SetActive(false);
                            ShowDrillConfirmation(ui, slot, original, def, headerContext.BaseAbilityLearned, headerContext.DrillSkillPointCost);
                        });
                    }

                    popupGO.SetActive(true);
                    return;
                }

                BuildOverlay(ui, slot, original, choices, entry, baseAbilityLearned, track, abilityLevel, baseAbilityCost);
            }

            private static void BuildOverlay(UIModuleCharacterProgression ui, AbilityTrackSlot slot,
                TacticalAbilityDef original, List<TacticalAbilityDef> choices,
                AbilityTrackSkillEntryElement entry, bool baseAbilityLearned,
                AbilityTrack track, int abilityLevel, int baseAbilityCost)
            {
                var overlay = UIBuilder.CreateHoverOverlay(ui, entry, out var panelRect, out var contentRect, out var viewportRect, out var controller, out var tooltipParent);
                if (overlay == null || panelRect == null || contentRect == null || viewportRect == null || controller == null)
                {
                    return;
                }

                controller.AttachTooltipSuppression(TooltipSuppressor.Begin());

                var canvas = overlay.GetComponentInParent<Canvas>() ?? ui.GetComponentInParent<Canvas>();

                var headerContext = BuildHeaderContext(ui, slot, original, baseAbilityLearned, track, abilityLevel, baseAbilityCost, entry);
                UIBuilder.AddHeader(contentRect, original, headerContext, tooltipParent, panelRect, canvas, () => controller.Close(() => AcquireBaseAbility(ui, slot, original, track, abilityLevel)));

                var gridRect = UIBuilder.CreateOptionGrid(contentRect, out var gridLayout);

                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = Reflection.GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);
                bool hasTrainingFacility = DrillsUnlock.HasFunctioningTrainingFacility(phoenixFaction);

                if (!hasTrainingFacility)
                {
                    UIBuilder.CreateTrainingFacilityOverlay(panelRect);
                }


                var availableChoices = choices?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();
                var availableSet = new HashSet<TacticalAbilityDef>(availableChoices);
                var ordered = DrillsDefs.Drills?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();

                foreach (var ability in availableChoices)
                {
                    if (!ordered.Contains(ability))
                    {
                        ordered.Add(ability);
                    }
                }

                ordered.RemoveAll(def => def == null || def == original);
                ordered = ordered
                    .Distinct()
                    .OrderBy(def =>
                    {
                        bool acquired = DrillsUnlock.CharacterHasDrill(character, def);
                        bool unlocked = DrillsUnlock.IsDrillUnlocked(phoenixFaction, character, def);
                        bool selectable = !acquired && unlocked;
                        if (selectable && availableSet.Contains(def))
                        {
                            return 0;
                        }

                        if (selectable)
                        {
                            return 1;
                        }

                        if (acquired)
                        {
                            return 2;
                        }

                        return 3;
                    }).ThenBy(def => def.ViewElementDef?.DisplayName1?.Localize() ?? def.name)
                    .ToList();

                int optionCount = 0;

                foreach (var def in ordered)
                {
                    bool unlocked = DrillsUnlock.IsDrillUnlocked(phoenixFaction, character, def);
                    bool acquired = DrillsUnlock.CharacterHasDrill(character, def);
                    bool locked = !unlocked;
                    string missingRequirements = locked ? string.Join("\n", DrillsUnlock.GetMissingRequirementDescriptions(phoenixFaction, character, def) ?? Enumerable.Empty<string>()) : string.Empty;

                    if (!hasTrainingFacility)
                    {
                        missingRequirements = string.Empty;
                    }

                    if (acquired)
                    {
                        missingRequirements = string.IsNullOrEmpty(missingRequirements)
                            ? "Already acquired"
                            : missingRequirements + "\nAlready acquired";
                    }

                    Action onChoose = null;
                    if (!locked && !acquired)
                    {
                        onChoose = () => controller.Close(() =>
                        {
                            ShowDrillConfirmation(ui, slot, original, def, headerContext.BaseAbilityLearned, headerContext.DrillSkillPointCost);
                        });
                    }

                    var option = UIBuilder.CreateDrillOption(gridRect, panelRect, def, locked, acquired, missingRequirements, tooltipParent, canvas, onChoose, headerContext.DrillSkillPointCost);
                    if (option != null)
                    {
                        optionCount++;
                    }
                }


                if (gridRect != null)
                {
                    if (optionCount > 0)
                    {
                        UIBuilder.ResizeOptionGrid(gridRect, gridLayout, optionCount);
                    }
                    else
                    {
                        gridRect.gameObject.SetActive(false);
                    }
                }

                if (optionCount == 0)
                {
                    UIBuilder.AddEmptyLabel(contentRect, "No drills available");
                }

                controller.ConfigureContent(viewportRect, contentRect, MenuWidth, MenuMaxHeight);
                overlay.transform.SetAsLastSibling();
            }

            private static HeaderContext BuildHeaderContext(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, bool baseAbilityLearned, AbilityTrack track, int abilityLevel, int baseAbilityCost, AbilityTrackSkillEntryElement entry)
            {
                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                int currentSp = Reflection.GetPrivate<int>(ui, "_currentSkillPoints");
                int currentFp = Reflection.GetPrivate<int>(ui, "_currentFactionPoints");
                int currentStr = Reflection.GetPrivate<int>(ui, "_currentStrengthStat");
                int currentWill = Reflection.GetPrivate<int>(ui, "_currentWillStat");
                int currentSpeed = Reflection.GetPrivate<int>(ui, "_currentSpeedStat");

                bool slotUnlocked = abilityLevel <= 0 || (character?.Progression?.LevelProgression?.Level ?? 0) >= abilityLevel;
                bool canLearn = baseAbilityLearned || (character?.Progression?.CanLearnAbility(slot, currentStr, currentWill, currentSpeed) ?? false);
                bool canAfford = baseAbilityLearned || (currentSp + currentFp) >= baseAbilityCost;

                var abilityName = original?.ViewElementDef?.DisplayName1?.Localize() ?? original?.name ?? string.Empty;

                var missingDetails = new List<string>();
                if (!baseAbilityLearned)
                {
                    if (!slotUnlocked)
                    {
                        missingDetails.Add("Level requirement not met");
                    }

                    if (!canAfford)
                    {
                        missingDetails.Add("Not enough Skill Points");
                    }

                    if (!canLearn && slotUnlocked)
                    {
                        missingDetails.Add("Requirements not met");
                    }
                }

                return new HeaderContext
                {
                    BaseAbilityLearned = baseAbilityLearned,
                    BaseAbilityCost = baseAbilityCost,
                    DrillSkillPointCost = baseAbilityLearned ? SwapSpCost : Math.Max(0, baseAbilityCost),
                    AbilityLevel = abilityLevel,
                    SlotUnlocked = slotUnlocked,
                    CanLearnBaseAbility = canLearn,
                    CanAffordBaseAbility = canAfford,
                    HeaderLabel = baseAbilityLearned ? $"Replace: {abilityName}" : $"Acquire: {abilityName}",
                    MissingRequirements = missingDetails.Count > 0 ? string.Join("\n", missingDetails.Distinct()) : string.Empty,
                    Track = track,
                    EntryElement = entry,
                    Slot = slot,
                    Ui = ui,
                    Ability = original
                };
            }



            private static void AcquireBaseAbility(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef ability, AbilityTrack track, int abilityLevel)
            {
                if (ui == null || slot == null || ability == null)
                {
                    return;
                }

                try
                {
                    int resolvedLevel = abilityLevel > 0 ? abilityLevel : CharacterLookup.GetAbilityLevel(track, slot);
                    var source = CharacterLookup.ResolveTrackSource(track);

                    Reflection.SetPrivate(ui, "_boughtAbilitySlot", slot);
                    Reflection.SetPrivate(ui, "_boughtAbility", ability);
                    Reflection.SetPrivate(ui, "_boughtAbilitySource", source);
                    Reflection.SetPrivate(ui, "_boughtAbilityLevel", resolvedLevel);

                    ui.AbilityBoughtConfirmation?.Invoke(slot, ability);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

        }
    }
}
