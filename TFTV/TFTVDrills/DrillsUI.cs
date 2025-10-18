using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static class DrillsUI
    {
        private const int SwapSpCost = 0;

        private static readonly Color FilterActiveColor = new Color(0.25f, 0.55f, 0.85f, 0.6f);
        private static readonly Color FilterInactiveColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color LockedIconTint = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color LockedLabelTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color LockedBackgroundTint = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color DrillPulseColor = new Color(1f, 0.29803923f, 0f, 1f); // #FF4C00


        private static Sprite _originalAvailableImage = null;

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

                    TFTVLogger.Always($"AbilityTrackSkillEntryElement.OnPointerClick: {character?.DisplayName}");

                    if (character?.Progression == null)
                    {
                        return true;
                    }

                    var ability = __instance.AbilityDef ?? ElementHelpers.FindSlot(__instance)?.Ability;

                    TFTVLogger.Always($"ability {ability?.name}");

                    if (ability == null || !character.Progression.Abilities.Contains(ability))
                    {
                        return true;
                    }

                    var (track, slot) = CharacterLookup.FindTrackSlot(character, ability);

                    if (slot == null || !CharacterLookup.IsPersonalTrack(track, character.Progression.PersonalAbilityTrack))
                    {
                        return true;
                    }

                    var source = CharacterLookup.ResolveTrackSource(track);
                    if (source != AbilityTrackSource.Personal)
                    {
                        return true;
                    }

                    GeoPhoenixFaction phoenixFaction = character?.Faction?.GeoLevel?.PhoenixFaction;
                    List<TacticalAbilityDef> availableChoices = DrillsDefs.GetAvailableDrills(phoenixFaction, character);
                    if (availableChoices == null || availableChoices.Count == 0)
                    {
                        return true;
                    }

                    DrillSwapUI.Show(ui, slot, ability, availableChoices);
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
                TFTVLogger.Always($"SetSkillState invoked; __instance==null: {__instance == null}");

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
                    TFTVLogger.Always($"var character: {character?.DisplayName}");

                    MethodInfo methodInfo = typeof(AbilityTrackSkillEntryElement).GetMethod("SetAnimator", BindingFlags.NonPublic | BindingFlags.Instance);
                   
                    var phoenixFaction = character?.Faction?.GeoLevel?.PhoenixFaction;
                    var ability = __instance.AbilityDef ?? ElementHelpers.FindSlot(__instance)?.Ability;
                    TFTVLogger.Always($"ability: {ability?.name}");
                    var availableImage = __instance.Available;    //DrillIndicator.FindAvailableImage(__instance);
                    TFTVLogger.Always($"availableImage==null: {availableImage == null}");
                    bool shouldShowIndicator = DrillIndicator.ShouldShow(character, phoenixFaction, ability, availableImage);

                    if (shouldShowIndicator)
                    {
                        if (_originalAvailableImage == null) 
                        {
                            _originalAvailableImage = availableImage.sprite;
                        }

                        availableImage.sprite = DrillsDefs._drillAvailable;
                        availableImage.gameObject.SetActive(true);
                        __instance.AvailableSkill = true;
                        methodInfo.Invoke(__instance, null);                        
                    }
                    else 
                    {
                        availableImage.gameObject.SetActive(isAvailable && isBuyable);
                        __instance.AvailableSkill = isAvailable;
                        availableImage.sprite = _originalAvailableImage;
                    }
                    // DrillIndicator.Update(__instance, availableImage, shouldShowIndicator);
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
            public static void Postfix(AbilityTrackSkillEntryElement __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn || __instance == null)
                {
                    return;
                }

                var availableImage = __instance.Available;
                if (availableImage == null || DrillsDefs._drillAvailable == null)
                {
                    return;
                }

                if (availableImage.sprite != DrillsDefs._drillAvailable)
                {
                    return;
                }

                if (!__instance.AvailableSkill || !availableImage.gameObject.activeSelf)
                {
                    return;
                }

                float t = Time.time * 2f % 2f;
                if (t > 1f)
                {
                    t = 1f - (t - 1f);
                }

                availableImage.color = Color.Lerp(DrillPulseColor, Color.white, t);
            }
        }

        private static class DrillIndicator
        {
            public static bool ShouldShow(GeoCharacter character, GeoPhoenixFaction phoenixFaction, TacticalAbilityDef ability, Image availableImage)
            {

                if (DrillsDefs._drillAvailable == null || availableImage == null)
                {
                    return false;
                }

                if (character?.Progression?.PersonalAbilityTrack == null || ability == null)
                {
                    return false;
                }

                TFTVLogger.Always($"!availableImage.gameObject.activeInHierarchy {!availableImage.gameObject.activeInHierarchy} {!availableImage.enabled}");

                /*  if (!availableImage.gameObject.activeInHierarchy || !availableImage.enabled)
                  {
                      return false;
                  }*/

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

                var availableDrills = DrillsDefs.GetAvailableDrills(phoenixFaction, character);
                return availableDrills != null && availableDrills.Any(def => def != null && def != ability);
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
        }

        private static class DrillSwapUI
        {
            public static void Show(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, List<TacticalAbilityDef> choices)
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

                    AddHeader(container.transform, original);
                    foreach (var def in choices.Where(def => def != null))
                    {
                        var go = UnityEngine.Object.Instantiate(prefab, container.transform);
                        WireButton(go, def, () =>
                        {
                            TryPerformSwap(ui, slot, original, def);
                            popupGO.SetActive(false);
                        });
                    }

                    AddCancel(container.transform, () => popupGO.SetActive(false));
                    popupGO.SetActive(true);
                    return;
                }

                BuildOverlay(ui, slot, original, choices);
            }

            private static void BuildOverlay(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, List<TacticalAbilityDef> choices)
            {
                var overlay = UIBuilder.CreateOverlay(ui.transform);
                var panel = UIBuilder.CreatePanel(overlay.transform, out var filterBar, out var contentRect, out var content, out var gridLayout, out var toggleGroup, out var titleText);
                if (titleText != null)
                {
                    string abilityName = original.ViewElementDef?.DisplayName1?.Localize() ?? original.name;
                    titleText.text = $"Replace: {abilityName}";
                }
                var cancelButton = UIBuilder.CreateCancelButton(panel.transform, () => UnityEngine.Object.Destroy(overlay));
                cancelButton.GetComponentInChildren<Text>().text = "Cancel";

                var availableToggle = UIBuilder.CreateFilterToggle(filterBar.transform, "Available To Character", toggleGroup);
                var allToggle = UIBuilder.CreateFilterToggle(filterBar.transform, "All Drills", toggleGroup);

                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = Reflection.GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);

                var availableChoices = choices?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();
                var allChoices = DrillsDefs.Drills?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();

                var canvas = ui.GetComponentInParent<Canvas>();
                Transform tooltipParent = canvas != null ? canvas.transform : ui.transform;

                void Populate(bool showAll)
                {
                    foreach (Transform child in content.transform)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    var sourceList = showAll ? allChoices : availableChoices;
                    foreach (var def in sourceList)
                    {
                        bool unlocked = DrillsDefs.IsDrillUnlocked(phoenixFaction, character, def);
                        bool locked = showAll && !unlocked;
                        string missingRequirements = locked ? string.Join("\n", DrillsDefs.GetMissingRequirementDescriptions(phoenixFaction, character, def) ?? Enumerable.Empty<string>()) : string.Empty;

                        System.Action onChoose = null;
                        if (!locked)
                        {
                            onChoose = () =>
                            {
                                TryPerformSwap(ui, slot, original, def);
                                UnityEngine.Object.Destroy(overlay);
                            };
                        }

                        var card = UIBuilder.CreateChoiceCard(def, 128, onChoose, locked, missingRequirements, tooltipParent);
                        card.transform.SetParent(content.transform, false);
                    }

                    UIBuilder.ResizeContent(contentRect, gridLayout, content.transform.childCount);
                }

                void UpdateToggleVisual(Toggle toggle, bool isOn)
                {
                    var image = toggle.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = isOn ? FilterActiveColor : FilterInactiveColor;
                    }

                    var label = toggle.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.color = isOn ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
                    }
                }

                availableToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleVisual(availableToggle, isOn);
                    if (isOn)
                    {
                        UpdateToggleVisual(allToggle, false);
                        Populate(false);
                    }
                });

                allToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleVisual(allToggle, isOn);
                    if (isOn)
                    {
                        UpdateToggleVisual(availableToggle, false);
                        Populate(true);
                    }
                });

                availableToggle.isOn = true;
                UpdateToggleVisual(availableToggle, true);
                UpdateToggleVisual(allToggle, false);
                Populate(false);

                overlay.transform.SetAsLastSibling();
            }
        }

        private static class UIBuilder
        {
            public static GameObject CreateOverlay(Transform parent)
            {
                var overlay = new GameObject("TFTV_SwapOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                var overlayRect = (RectTransform)overlay.transform;
                overlayRect.SetParent(parent, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var overlayBg = overlay.GetComponent<Image>();
                overlayBg.color = new Color(0f, 0f, 0f, 0.55f);
                var overlayButton = overlay.GetComponent<Button>();
                overlayButton.transition = Selectable.Transition.None;
                overlayButton.onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));
                return overlay;
            }

            public static GameObject CreatePanel(Transform parent, out GameObject filterBar, out RectTransform contentRect, out GameObject content, out GridLayoutGroup grid, out ToggleGroup toggleGroup, out Text titleText)
            {
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                float panelWidth = Mathf.Clamp(screenWidth * 0.60f, 800f, 1400f);
                float panelHeight = Mathf.Clamp(screenHeight * 0.70f, 560f, 900f);

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button));
                var panelRect = (RectTransform)panel.transform;
                panelRect.SetParent(parent, false);
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

                var panelImage = panel.GetComponent<Image>();
                panelImage.color = new Color(0.10f, 0.10f, 0.10f, 0.96f);
                panel.GetComponent<Button>().onClick.AddListener(() => { });

                var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                var titleRect = (RectTransform)title.transform;
                titleRect.SetParent(panel.transform, false);
                titleRect.anchorMin = new Vector2(0, 1);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0.5f, 1);
                titleRect.sizeDelta = new Vector2(0, 42);
                titleRect.anchoredPosition = new Vector2(0, -10);

                titleText = title.GetComponent<Text>();
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.fontSize = 22;
                titleText.text = "Replace";

                filterBar = new GameObject("FilterBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                var filterRect = (RectTransform)filterBar.transform;
                filterRect.SetParent(panel.transform, false);
                filterRect.anchorMin = new Vector2(0, 1);
                filterRect.anchorMax = new Vector2(1, 1);
                filterRect.pivot = new Vector2(0.5f, 1);
                filterRect.sizeDelta = new Vector2(0, 40f);
                filterRect.anchoredPosition = new Vector2(0, -58f);

                var filterLayout = filterBar.GetComponent<HorizontalLayoutGroup>();
                filterLayout.childControlWidth = true;
                filterLayout.childForceExpandWidth = true;
                filterLayout.childAlignment = TextAnchor.MiddleCenter;
                filterLayout.spacing = 12f;
                filterLayout.padding = new RectOffset(24, 24, 0, 0);

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                var viewportRect = (RectTransform)viewport.transform;
                viewportRect.SetParent(panel.transform, false);
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                const float viewportHorizontalPadding = 16f;
                const float viewportBottomPadding = 96f;
                const float viewportTopExtraPadding = 16f;

                float filterTopOffset = Mathf.Abs(filterRect.anchoredPosition.y);
                float filterHeight = filterRect.rect.height > 0 ? filterRect.rect.height : filterRect.sizeDelta.y;
                float viewportTopPadding = filterTopOffset + filterHeight + viewportTopExtraPadding;

                viewportRect.offsetMin = new Vector2(viewportHorizontalPadding, viewportBottomPadding);
                viewportRect.offsetMax = new Vector2(-viewportHorizontalPadding, -viewportTopPadding);
                viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

                content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
                contentRect = (RectTransform)content.transform;
                contentRect.SetParent(viewport.transform, false);
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;

                grid = content.GetComponent<GridLayoutGroup>();
                const int IconSize = 128;
                grid.cellSize = new Vector2(IconSize + 56, IconSize + 40);
                grid.spacing = new Vector2(12, 12);
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

                float availableWidth = panelWidth - 32f;
                int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
                grid.constraintCount = columns;

                var scroll = panel.AddComponent<ScrollRect>();
                scroll.viewport = viewportRect;
                scroll.content = contentRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 30f;

                toggleGroup = filterBar.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = false;
                return panel;
            }

            public static GameObject CreateCancelButton(Transform parent, Action onClick)
            {
                var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                var cancelRect = (RectTransform)cancel.transform;
                cancelRect.SetParent(parent, false);
                cancelRect.anchorMin = new Vector2(0.5f, 0);
                cancelRect.anchorMax = new Vector2(0.5f, 0);
                cancelRect.pivot = new Vector2(0.5f, 0);
                cancelRect.anchoredPosition = new Vector2(0, 12);
                cancelRect.sizeDelta = new Vector2(160, 36);

                cancel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
                var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var labelRect = (RectTransform)label.transform;
                labelRect.SetParent(cancel.transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var text = label.GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.text = "Cancel";

                cancel.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
                return cancel;
            }

            public static Toggle CreateFilterToggle(Transform parent, string label, ToggleGroup group)
            {
                var toggleGO = new GameObject($"{label}Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
                var rect = (RectTransform)toggleGO.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0, 36f);

                var image = toggleGO.GetComponent<Image>();
                image.color = FilterInactiveColor;

                var toggle = toggleGO.GetComponent<Toggle>();
                toggle.isOn = false;
                toggle.transition = Selectable.Transition.ColorTint;
                toggle.group = group;

                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var labelRect = (RectTransform)labelGO.transform;
                labelRect.SetParent(toggleGO.transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var text = labelGO.GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 18;
                text.text = label;
                text.color = new Color(0.85f, 0.85f, 0.85f, 1f);

                return toggle;
            }

            public static GameObject CreateChoiceCard(TacticalAbilityDef def, int iconSize, Action onChoose, bool isLocked, string missingRequirements, Transform tooltipParent)
            {
                var card = new GameObject(def?.name ?? "Ability", typeof(RectTransform), typeof(Image), typeof(Button));
                var rt = (RectTransform)card.transform;
                rt.sizeDelta = new Vector2(iconSize + 56, iconSize + 40);

                var bg = card.GetComponent<Image>();
                bg.color = isLocked ? LockedBackgroundTint : new Color(1f, 1f, 1f, 0.08f);
                var btn = card.GetComponent<Button>();
                btn.interactable = !isLocked && onChoose != null;
                if (!isLocked && onChoose != null)
                {
                    btn.onClick.AddListener(() => onChoose?.Invoke());
                }

                var cardCanvasGroup = card.AddComponent<CanvasGroup>();
                cardCanvasGroup.alpha = isLocked ? 0.45f : 1f;

                var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var irt = (RectTransform)ico.transform;
                irt.SetParent(card.transform, false);
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1);
                irt.pivot = new Vector2(0.5f, 1);
                irt.anchoredPosition = new Vector2(0, -8);
                irt.sizeDelta = new Vector2(iconSize, iconSize);

                var iconImg = ico.GetComponent<Image>();
                iconImg.sprite = def?.ViewElementDef?.LargeIcon ?? def?.ViewElementDef?.SmallIcon;
                iconImg.preserveAspect = true;
                iconImg.color = isLocked ? LockedIconTint : Color.white;
                iconImg.raycastTarget = false;

                var lab = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var lrt = (RectTransform)lab.transform;
                lrt.SetParent(card.transform, false);
                lrt.anchorMin = new Vector2(0, 0);
                lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0);
                lrt.anchoredPosition = new Vector2(0, 6);
                lrt.sizeDelta = new Vector2(0, 22);

                var txt = lab.GetComponent<Text>();
                txt.alignment = TextAnchor.MiddleCenter;
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 12;
                txt.resizeTextMaxSize = 18;
                txt.text = def?.ViewElementDef?.DisplayName1?.Localize() ?? def?.name ?? "Ability";
                txt.color = isLocked ? LockedLabelTint : Color.white;
                txt.raycastTarget = false;

                var tooltipTrigger = card.AddComponent<DrillTooltipTrigger>();
                tooltipTrigger.Initialize(def, missingRequirements, isLocked, tooltipParent);

                return card;
            }

            public static void ResizeContent(RectTransform contentRect, GridLayoutGroup grid, int itemCount)
            {
                int columns = Mathf.Max(1, grid.constraintCount);
                int rows = itemCount > 0 ? Mathf.CeilToInt((float)itemCount / columns) : 0;
                float height = rows > 0 ? rows * grid.cellSize.y + (rows - 1) * grid.spacing.y + 16f : 16f;
                contentRect.sizeDelta = new Vector2(0, height);
            }
        }

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

        private static void AddHeader(Transform parent, TacticalAbilityDef original)
        {
            var header = new GameObject("TFTV_SwapHeader", typeof(RectTransform), typeof(Text));
            header.transform.SetParent(parent, false);
            var text = header.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 18;
            text.raycastTarget = false;
            string name = original.ViewElementDef?.DisplayName1?.Localize() ?? original.name;
            text.text = $"Replace: {name}";
        }

        private static void AddCancel(Transform parent, Action onClick)
        {
            var go = new GameObject("TFTV_Cancel", typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(parent, false);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "Cancel";
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
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

        private static void TryPerformSwap(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, TacticalAbilityDef replacement)
        {
            try
            {
                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = Reflection.GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);
                if (character?.Progression == null)
                {
                    return;
                }

                if (!DrillsDefs.IsDrillUnlocked(phoenixFaction, character, replacement))
                {
                    TFTVLogger.Always($"[TFTV Drills] Attempted to swap to locked drill {replacement?.name}; aborting swap.");
                    return;
                }

                if (SwapSpCost > 0)
                {
                    var currSP = Reflection.GetPrivate<int>(ui, "_currentSkillPoints");
                    var currFP = Reflection.GetPrivate<int>(ui, "_currentFactionPoints");
                    int remaining = SwapSpCost;

                    if (currSP >= remaining)
                    {
                        Reflection.SetPrivate(ui, "_currentSkillPoints", currSP - remaining);
                    }
                    else
                    {
                        remaining -= currSP;
                        Reflection.SetPrivate(ui, "_currentSkillPoints", 0);
                        if (currFP >= remaining)
                        {
                            Reflection.SetPrivate(ui, "_currentFactionPoints", currFP - remaining);
                        }
                        else
                        {
                            Debug.LogWarning("[TFTV] Not enough SP/FS for swap; aborting.");
                            return;
                        }
                    }
                }

                List<TacticalAbilityDef> abilities = Traverse.Create(character.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();

                if (abilities.Contains(original))
                {
                    abilities.Remove(original);
                }

                if (!abilities.Contains(replacement))
                {
                    abilities.Add(replacement);
                }

                if (replacement.name.Contains("fieldpromotion"))
                {
                    TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points before Field Promotion");
                    var geoController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    var progressionModule = geoController.View.GeoscapeModules.CharacterProgressionModule;

                    FieldInfo fieldInfo = typeof(UIModuleCharacterProgression).GetField("_currentSkillPoints", BindingFlags.Instance | BindingFlags.NonPublic);
                    fieldInfo.SetValue(progressionModule, character.Progression.SkillPoints + 30);

                    progressionModule.CommitStatChanges();
                    TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points after Field Promotion");
                }

                slot.Ability = replacement;

                Reflection.CallPrivate(ui, "CommitStatChanges");
                Reflection.CallPrivate(ui, "RefreshStatPanel");
                Reflection.CallPrivate(ui, "SetAbilityTracks");
                Reflection.CallPrivate(ui, "RefreshAbilityTracks");
            }
            catch (Exception e)
            {
                TFTVLogger.Always($"[TFTV] Ability swap failed: {e}");
            }
        }

        private sealed class DrillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private TacticalAbilityDef _ability;
            private bool _isLocked;
            private string _missingRequirements;
            private Transform _tooltipParent;
            private bool _tooltipVisible;
            private static GeoRosterAbilityDetailTooltip _sharedTooltip;
            private static Canvas _tooltipCanvas;
            private static bool _tooltipPrimed;

            private const float TooltipHorizontalOffset = 280f;
            private const float TooltipVerticalOffset = 80f;

            public void Initialize(TacticalAbilityDef ability, string missingRequirements, bool isLocked, Transform tooltipParent)
            {
                _ability = ability;
                _missingRequirements = missingRequirements;
                _isLocked = isLocked;
                _tooltipParent = tooltipParent;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                ShowTooltip(eventData);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                HideTooltip();
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                if (_tooltipVisible)
                {
                    UpdateTooltipPosition(eventData);
                }
            }

            private void OnDisable()
            {
                HideTooltip();
            }

            private void ShowTooltip(PointerEventData eventData)
            {
                var tooltip = EnsureTooltip();
                var view = _ability?.ViewElementDef;
                if (tooltip == null || view == null)
                {
                    return;
                }

                var originalDescription = view.Description;
                LocalizedTextBind temporaryDescription = null;

                if (_isLocked && !string.IsNullOrEmpty(_missingRequirements))
                {
                    string descriptionText = originalDescription?.Localize() ?? string.Empty;
                    if (!string.IsNullOrEmpty(descriptionText))
                    {
                        descriptionText += "\n\n";
                    }

                    descriptionText += $"<color=#FFA0A0><b>Missing requirements:</b>\n{_missingRequirements}</color>";
                    temporaryDescription = new LocalizedTextBind(descriptionText, true);
                    view.Description = temporaryDescription;
                }

                bool shouldPrime = !_tooltipPrimed;
                if (shouldPrime)
                {
                    _tooltipPrimed = true;
                }

                try
                {
                    tooltip.Show((AbilityTrackSlot)null, view, useMutagens: false, cost: 0);
                    if (shouldPrime)
                    {
                        tooltip.Hide();
                        tooltip.Show((AbilityTrackSlot)null, view, useMutagens: false, cost: 0);
                    }

                    tooltip.transform.SetAsLastSibling();
                    UpdateTooltipPosition(eventData);
                    _tooltipVisible = true;
                }
                finally
                {
                    if (temporaryDescription != null)
                    {
                        view.Description = originalDescription;
                    }
                }
            }

            private void HideTooltip()
            {
                if (!_tooltipVisible)
                {
                    return;
                }

                var tooltip = EnsureTooltip();
                tooltip?.Hide();
                _tooltipVisible = false;
            }

            private void UpdateTooltipPosition(PointerEventData eventData)
            {
                var tooltip = EnsureTooltip();
                if (tooltip == null || !tooltip.gameObject.activeInHierarchy)
                {
                    return;
                }

                if (!(tooltip.transform is RectTransform rectTransform))
                {
                    return;
                }

                var canvas = _tooltipCanvas;
                if (canvas == null)
                {
                    _tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
                    canvas = _tooltipCanvas;
                }

                if (canvas == null || !(canvas.transform is RectTransform canvasRect))
                {
                    return;
                }

                var referenceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, referenceCamera, out var localPoint))
                {
                    return;
                }

                localPoint.x += TooltipHorizontalOffset;
                localPoint.y += TooltipVerticalOffset;
                rectTransform.anchoredPosition = localPoint;
            }

            private GeoRosterAbilityDetailTooltip EnsureTooltip()
            {
                try
                {
                    if (_sharedTooltip == null)
                    {
                        var template = Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>().FirstOrDefault();
                        if (template == null)
                        {
                            return null;
                        }

                        Transform parent = _tooltipParent != null ? _tooltipParent : template.transform.parent;
                        var clone = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                        clone.name = "TFTV_DrillAbilityTooltip";
                        clone.SetActive(false);
                        _sharedTooltip = clone.GetComponent<GeoRosterAbilityDetailTooltip>();
                        _tooltipCanvas = null;
                    }

                    if (_tooltipParent != null && _sharedTooltip.transform.parent != _tooltipParent)
                    {
                        _sharedTooltip.transform.SetParent(_tooltipParent, false);
                        _tooltipCanvas = null;
                    }

                    return _sharedTooltip;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }
        }
    }
}