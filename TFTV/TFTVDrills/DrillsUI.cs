using Base.Core;
using Base.Input;
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
using System.Collections;
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
        private const int SwapSpCost = 10;
        private const float MenuMaxHeight = 900f;
        private const float MenuWidth = 760f;
        private const int GridColumns = 5;
        private const int VisibleGridRows = 5;
        private const float GridCellWidth = 128f;
        private const float GridCellHeight = 140f;
        private const float GridSpacing = 12f;
        private const float GridPadding = 18f;

        private static readonly Color LockedIconTint = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color LockedLabelTint = new Color(0.82f, 0.82f, 0.82f, 1f);
        private static readonly Color DrillPulseColor = new Color(1f, 0.29803923f, 0f, 1f);

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

                    DrillSwapUI.Show(ui, slot, ability, availableChoices, __instance);
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
                    var availableImage = __instance.Available;
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
                        // availableImage.color = DrillPulseColor;
                        // methodInfo.Invoke(__instance, null);
                    }
                    else
                    {
                        availableImage.gameObject.SetActive(isAvailable && isBuyable);
                        __instance.AvailableSkill = isAvailable;
                        availableImage.sprite = _originalAvailableImage;
                    }
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

                availableImage.color = DrillPulseColor;// Color.Lerp(DrillPulseColor, Color.white, t);
            }
        }

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
            public static void Show(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, List<TacticalAbilityDef> choices, AbilityTrackSkillEntryElement entry)
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
            
                    popupGO.SetActive(true);
                    return;
                }

                BuildOverlay(ui, slot, original, choices, entry);
            }

            private static void BuildOverlay(UIModuleCharacterProgression ui, AbilityTrackSlot slot, TacticalAbilityDef original, List<TacticalAbilityDef> choices, AbilityTrackSkillEntryElement entry)
            {
                var overlay = UIBuilder.CreateHoverOverlay(ui, entry, out var panelRect, out var contentRect, out var viewportRect, out var controller, out var tooltipParent);
                if (overlay == null || panelRect == null || contentRect == null || viewportRect == null || controller == null)
                {
                    return;
                }

                controller.AttachTooltipSuppression(TooltipSuppressor.Begin());

                var canvas = overlay.GetComponentInParent<Canvas>() ?? ui.GetComponentInParent<Canvas>();

                var header = UIBuilder.AddHeader(contentRect, original);
                if (header != null)
                {
                    header.text = $"Replace: {original.ViewElementDef?.DisplayName1?.Localize() ?? original.name}";
                }

                UIBuilder.AddDivider(contentRect);

                var gridRect = UIBuilder.CreateOptionGrid(contentRect, out var gridLayout);

                var character = Reflection.GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = Reflection.GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);

                var availableChoices = choices?.Where(def => def != null && !DrillsDefs.CharacterHasDrill(character, def)).Distinct().ToList() ?? new List<TacticalAbilityDef>();
                var ordered = DrillsDefs.Drills?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();

                foreach (var ability in availableChoices)
                {
                    if (!ordered.Contains(ability))
                    {
                        ordered.Add(ability);
                    }
                }

                ordered.RemoveAll(def => def == null || def == original || DrillsDefs.CharacterHasDrill(character, def));
                ordered = ordered
                    .OrderBy(def => availableChoices.Contains(def) ? 0 : 1)
                    .ThenBy(def => def.ViewElementDef?.DisplayName1?.Localize() ?? def.name)
                    .ToList();

                int optionCount = 0;

                foreach (var def in ordered)
                {
                    bool unlocked = DrillsDefs.IsDrillUnlocked(phoenixFaction, character, def);
                    bool locked = !unlocked;
                    string missingRequirements = locked ? string.Join("\n", DrillsDefs.GetMissingRequirementDescriptions(phoenixFaction, character, def) ?? Enumerable.Empty<string>()) : string.Empty;

                    Action onChoose = null;
                    if (!locked)
                    {
                        onChoose = () => controller.Close(() =>
                        {
                            TryPerformSwap(ui, slot, original, def);
                        });
                    }

                    var option = UIBuilder.CreateDrillOption(gridRect, panelRect, def, locked, missingRequirements, tooltipParent, canvas, onChoose);
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

                UIBuilder.AddDivider(contentRect);
                // UIBuilder.AddCancelButton(contentRect, () => controller.Close(), panelRect, tooltipParent, canvas);

                controller.ConfigureContent(viewportRect, contentRect, MenuWidth, MenuMaxHeight);
                overlay.transform.SetAsLastSibling();
            }
        }

        private static class UIBuilder
        {
            private static readonly Color PanelColor = new Color(0.09f, 0.13f, 0.2f, 1f);
            private static readonly Color ButtonNormalColor = new Color(1f, 1f, 1f, 0.08f);
            private static readonly Color ButtonHighlightColor = new Color(0.2f, 0.0588f, 0f, 1f);
            private static readonly Color ButtonPressedColor = new Color(0.3137255f, 0.11764706f, 0.019607844f, 1f);
            private static readonly Color ButtonDisabledColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
            private static readonly Color DrillFrameColor = new Color(0.29803923f, 0.09019608f, 0f, 1f);
            private static readonly Color LockedFrameColor = new Color(0.15294118f, 0.15294118f, 0.15294118f, 1f);
            private static Font _defaultFont;

            public static GameObject CreateHoverOverlay(UIModuleCharacterProgression ui, AbilityTrackSkillEntryElement entry, out RectTransform panelRect, out RectTransform contentRect, out RectTransform viewportRect, out DrillOverlayController controller, out Transform tooltipParent)
            {
                panelRect = null;
                contentRect = null;
                viewportRect = null;
                controller = null;
                tooltipParent = null;

                if (ui == null)
                {
                    return null;
                }

                var canvas = ui.GetComponentInParent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : ui.transform;
                tooltipParent = canvas != null ? canvas.transform : ui.transform;

                var overlay = new GameObject("TFTV_DrillOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                var overlayRect = (RectTransform)overlay.transform;
                overlayRect.SetParent(parent, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var backgroundImage = overlay.GetComponent<Image>();
                backgroundImage.color = new Color(0f, 0f, 0f, 0f);
                var backgroundButton = overlay.GetComponent<Button>();
                backgroundButton.transition = Selectable.Transition.None;

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panelRect = (RectTransform)panel.transform;
                panelRect.SetParent(overlayRect, false);
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MenuWidth);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MenuMaxHeight);

                var panelImage = panel.GetComponent<Image>();
                panelImage.color = PanelColor;
                panelImage.raycastTarget = true;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportRect = (RectTransform)viewport.transform;
                viewportRect.SetParent(panelRect, false);
                viewportRect.anchorMin = new Vector2(0f, 0f);
                viewportRect.anchorMax = new Vector2(1f, 1f);
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;

                var viewportImage = viewport.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0f);
                viewportImage.raycastTarget = true;
                var rectMask = viewport.GetComponent<RectMask2D>();
                rectMask.enabled = true;

                var content = new GameObject("Content", typeof(RectTransform));
                contentRect = (RectTransform)content.transform;
                contentRect.SetParent(viewportRect, false);
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;

                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 18, 18);
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                var scrollRect = panel.AddComponent<ScrollRect>();
                scrollRect.viewport = viewportRect;
                scrollRect.content = contentRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 30f;
                scrollRect.inertia = true;

                var overlayController = overlay.AddComponent<DrillOverlayController>();
                overlayController.Initialize(canvas, overlayRect, panelRect, entry != null ? entry.GetComponent<RectTransform>() : null, backgroundButton);
                controller = overlayController;

                backgroundButton.onClick.AddListener(() => overlayController.Close());

                overlayRect.SetAsLastSibling();
                return overlay;
            }

            public static Text AddHeader(RectTransform content, TacticalAbilityDef original)
            {

                if (content == null)
                {
                    return null;
                }

                var headerGO = new GameObject("Header", typeof(RectTransform));
                var headerRect = (RectTransform)headerGO.transform;
                headerRect.SetParent(content, false);
                headerRect.anchorMin = new Vector2(0f, 0.5f);
                headerRect.anchorMax = new Vector2(1f, 0.5f);
                headerRect.sizeDelta = new Vector2(0f, 48f);

                var layoutGroup = headerGO.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = true;
                layoutGroup.padding = new RectOffset(6, 6, 0, 0);
                layoutGroup.spacing = 8f;

                var headerLayout = headerGO.AddComponent<LayoutElement>();
                headerLayout.minHeight = 48f;
                headerLayout.preferredHeight = 48f;

                var headerTextGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var headerTextRect = (RectTransform)headerTextGO.transform;
                headerTextRect.SetParent(headerRect, false);
                headerTextRect.anchorMin = new Vector2(0f, 0.5f);
                headerTextRect.anchorMax = new Vector2(1f, 0.5f);

                var headerText = headerTextGO.GetComponent<Text>();
                headerText.font = GetDefaultFont();
                headerText.text = original?.ViewElementDef?.DisplayName1?.Localize() ?? original?.name ?? "Replace";
                headerText.color = Color.white;
                headerText.fontSize = 20;
                headerText.alignment = TextAnchor.MiddleCenter;
                headerText.raycastTarget = false;

                var textLayout = headerTextGO.AddComponent<LayoutElement>();
                textLayout.minWidth = 0f;
                textLayout.flexibleWidth = 0f;

                var textFitter = headerTextGO.AddComponent<ContentSizeFitter>();
                textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGO.transform;
                iconRect.SetParent(headerRect, false);
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.localScale = Vector3.one;
                iconRect.anchoredPosition = Vector2.zero;

                var iconImage = iconGO.GetComponent<Image>();
                iconImage.raycastTarget = false;
                iconImage.color = Color.white;

                var iconSprite = original?.ViewElementDef?.LargeIcon ?? original?.ViewElementDef?.SmallIcon;
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.preserveAspect = true;

                    var iconLayout = iconGO.AddComponent<LayoutElement>();
                    const float iconSize = 48f;
                    iconLayout.minWidth = iconSize;
                    iconLayout.minHeight = iconSize;
                    iconLayout.preferredWidth = iconSize;
                    iconLayout.preferredHeight = iconSize;
                    iconLayout.flexibleWidth = 0f;
                    iconLayout.flexibleHeight = 0f;
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                }
                else
                {
                    iconGO.SetActive(false);
                }

               

                return headerText;
            }

            public static void AddDivider(RectTransform content)
            {
                if (content == null)
                {
                    return;
                }

                var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
                var dividerRect = (RectTransform)divider.transform;
                dividerRect.SetParent(content, false);
                dividerRect.anchorMin = new Vector2(0f, 0.5f);
                dividerRect.anchorMax = new Vector2(1f, 0.5f);
                dividerRect.sizeDelta = new Vector2(0f, 2f);

                var dividerImage = divider.GetComponent<Image>();
                dividerImage.color = new Color(1f, 1f, 1f, 0.08f);

                var layout = divider.AddComponent<LayoutElement>();
                layout.minHeight = 8f;
                layout.preferredHeight = 8f;
            }

            public static RectTransform CreateOptionGrid(RectTransform content, out GridLayoutGroup grid)
            {
                grid = null;
                if (content == null)
                {
                    return null;
                }

                var gridRoot = new GameObject("OptionsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                var gridRect = (RectTransform)gridRoot.transform;
                gridRect.SetParent(content, false);
                gridRect.anchorMin = new Vector2(0f, 1f);
                gridRect.anchorMax = new Vector2(1f, 1f);
                gridRect.pivot = new Vector2(0.5f, 1f);

                grid = gridRoot.GetComponent<GridLayoutGroup>();
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = GridColumns;
                grid.cellSize = new Vector2(GridCellWidth, GridCellHeight);
                grid.spacing = new Vector2(GridSpacing, GridSpacing);
                grid.padding = new RectOffset((int)GridPadding, (int)GridPadding, (int)GridPadding, (int)GridPadding);

                var layoutElement = gridRoot.AddComponent<LayoutElement>();
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = 0f;
                layoutElement.flexibleHeight = 0f;
                layoutElement.minWidth = 0f;
                layoutElement.preferredWidth = 0f;
                layoutElement.flexibleWidth = 0f;

                return gridRect;
            }

            public static GameObject CreateDrillOption(RectTransform gridRect, RectTransform panelRect, TacticalAbilityDef def, bool isLocked, string missingRequirements, Transform tooltipParent, Canvas canvas, Action onChoose)
            {
                if (gridRect == null || def == null)
                {
                    return null;
                }

                var option = new GameObject(def.name ?? "Ability", typeof(RectTransform), typeof(Image), typeof(Button));
                var optionRect = (RectTransform)option.transform;
                optionRect.SetParent(gridRect, false);
                optionRect.anchorMin = new Vector2(0f, 1f);
                optionRect.anchorMax = new Vector2(0f, 1f);
                optionRect.pivot = new Vector2(0.5f, 0.5f);
                optionRect.sizeDelta = Vector2.zero;

                var background = option.GetComponent<Image>();
                background.color = ButtonNormalColor;

                var button = option.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = ButtonNormalColor;
                colors.highlightedColor = ButtonHighlightColor;
                colors.pressedColor = ButtonPressedColor;
                colors.selectedColor = ButtonHighlightColor;
                colors.disabledColor = ButtonDisabledColor;
                colors.colorMultiplier = 1f;
                button.colors = colors;

                if (!isLocked && onChoose != null)
                {
                    button.onClick.AddListener(() => onChoose());
                }
                else
                {
                    button.interactable = false;
                    background.color = ButtonDisabledColor;
                }

                var layout = option.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.spacing = 6f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var frameGO = new GameObject("IconFrame", typeof(RectTransform));
                var frameRect = (RectTransform)frameGO.transform;
                frameRect.SetParent(option.transform, false);
                frameRect.anchorMin = new Vector2(0.5f, 0.5f);
                frameRect.anchorMax = new Vector2(0.5f, 0.5f);
                frameRect.pivot = new Vector2(0.5f, 0.5f);
                frameRect.anchoredPosition = Vector2.zero;
                frameRect.sizeDelta = new Vector2(100f, 100f);

                var frameLayout = frameGO.AddComponent<LayoutElement>();
                frameLayout.preferredWidth = 100f;
                frameLayout.preferredHeight = 100f;
                frameLayout.minWidth = 100f;
                frameLayout.minHeight = 100f;

                float borderThickness = 4f;

                Color frameColor = isLocked ? LockedFrameColor : DrillFrameColor;

                void CreateBorder(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
                {
                    var borderGO = new GameObject(name, typeof(RectTransform), typeof(Image));
                    var borderRect = (RectTransform)borderGO.transform;
                    borderRect.SetParent(frameGO.transform, false);
                    borderRect.anchorMin = anchorMin;
                    borderRect.anchorMax = anchorMax;
                    borderRect.pivot = new Vector2(0.5f, 0.5f);
                    borderRect.anchoredPosition = Vector2.zero;
                    borderRect.sizeDelta = sizeDelta;

                    var borderImage = borderGO.GetComponent<Image>();
                    borderImage.color = frameColor;
                    borderImage.raycastTarget = false;
                }

                CreateBorder("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(borderThickness, 0f));
                CreateBorder("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(borderThickness, 0f));
                CreateBorder("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, borderThickness));
                CreateBorder("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, borderThickness));

                var frameBackground = new GameObject("FrameBackground", typeof(RectTransform), typeof(Image));
                var frameBackgroundRect = (RectTransform)frameBackground.transform;
                frameBackgroundRect.SetParent(frameGO.transform, false);
                frameBackgroundRect.anchorMin = new Vector2(0f, 0f);
                frameBackgroundRect.anchorMax = new Vector2(1f, 1f);
                frameBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
                frameBackgroundRect.anchoredPosition = Vector2.zero;
                frameBackgroundRect.offsetMin = new Vector2(borderThickness, borderThickness);
                frameBackgroundRect.offsetMax = new Vector2(-borderThickness, -borderThickness);

                var frameBackgroundImage = frameBackground.GetComponent<Image>();
                frameBackgroundImage.color = Color.clear;
                frameBackgroundImage.raycastTarget = false;

                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGO.transform;
                iconRect.SetParent(frameBackground.transform, false);
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(92f, 92f);

                var iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = def.ViewElementDef?.LargeIcon ?? def.ViewElementDef?.SmallIcon;
                iconImage.preserveAspect = true;
                iconImage.color = isLocked ? LockedIconTint : Color.white;
                iconImage.raycastTarget = false;

                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var labelRect = (RectTransform)labelGO.transform;
                labelRect.SetParent(option.transform, false);
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.sizeDelta = new Vector2(0f, 28f);

                var labelText = labelGO.GetComponent<Text>();
                labelText.font = GetDefaultFont();
                labelText.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name ?? "Ability";
                labelText.color = isLocked ? LockedLabelTint : Color.white;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.resizeTextForBestFit = true;
                labelText.resizeTextMinSize = 12;
                labelText.resizeTextMaxSize = 20;
                labelText.raycastTarget = false;

                var labelLayout = labelGO.AddComponent<LayoutElement>();
                labelLayout.minHeight = 24f;
                labelLayout.preferredHeight = 24f;

                var tooltipTrigger = option.AddComponent<DrillTooltipTrigger>();
                tooltipTrigger.Initialize(def, missingRequirements, isLocked, tooltipParent, panelRect, canvas);

                return option;
            }

            public static void ResizeOptionGrid(RectTransform gridRect, GridLayoutGroup grid, int optionCount)
            {
                if (gridRect == null || grid == null)
                {
                    return;
                }

                int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0
                    ? grid.constraintCount
                    : Mathf.Max(1, GridColumns);

                int rows = optionCount > 0 ? Mathf.CeilToInt(optionCount / (float)columns) : 0;
                int visibleRows = Mathf.Max(rows, VisibleGridRows);

                float height = grid.padding.top + grid.padding.bottom;
                if (visibleRows > 0)
                {
                    height += visibleRows * grid.cellSize.y;
                    height += Mathf.Max(0, visibleRows - 1) * grid.spacing.y;
                }

                float width = grid.padding.left + grid.padding.right;
                width += columns * grid.cellSize.x;
                width += Mathf.Max(0, columns - 1) * grid.spacing.x;

                var layout = gridRect.GetComponent<LayoutElement>() ?? gridRect.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = height;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
                layout.minWidth = width;
                layout.preferredWidth = width;
                layout.flexibleWidth = 0f;

                gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            public static void AddEmptyLabel(RectTransform content, string label)
            {
                if (content == null)
                {
                    return;
                }

                var labelGO = new GameObject("Empty", typeof(RectTransform), typeof(Text));
                var rect = (RectTransform)labelGO.transform;
                rect.SetParent(content, false);
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 48f);

                var text = labelGO.GetComponent<Text>();
                text.font = GetDefaultFont();
                text.text = label;
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;

                var layout = labelGO.AddComponent<LayoutElement>();
                layout.minHeight = 48f;
                layout.preferredHeight = 48f;
            }

            public static void AddCancelButton(RectTransform content, Action onCancel, RectTransform panelRect, Transform tooltipParent, Canvas canvas)
            {
                if (content == null)
                {
                    return;
                }

                var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                var cancelRect = (RectTransform)cancel.transform;
                cancelRect.SetParent(content, false);
                cancelRect.anchorMin = new Vector2(0f, 1f);
                cancelRect.anchorMax = new Vector2(1f, 1f);
                cancelRect.pivot = new Vector2(0.5f, 1f);
                cancelRect.sizeDelta = new Vector2(0f, 60f);

                var cancelImage = cancel.GetComponent<Image>();
                cancelImage.color = new Color(1f, 1f, 1f, 0.08f);

                var button = cancel.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
                colors.highlightedColor = new Color(0.25f, 0.55f, 0.85f, 0.22f);
                colors.pressedColor = new Color(0.25f, 0.55f, 0.85f, 0.35f);
                colors.disabledColor = new Color(1f, 1f, 1f, 0.04f);
                button.colors = colors;
                button.onClick.AddListener(() => onCancel?.Invoke());

                var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var labelRect = (RectTransform)labelGO.transform;
                labelRect.SetParent(cancel.transform, false);
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var labelText = labelGO.GetComponent<Text>();
                labelText.font = GetDefaultFont();
                labelText.text = "Cancel";
                labelText.fontSize = 18;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.color = Color.white;
                labelText.raycastTarget = false;

                var layout = cancel.AddComponent<LayoutElement>();
                layout.minHeight = 60f;
                layout.preferredHeight = 60f;

                var tooltipTrigger = cancel.AddComponent<DrillTooltipTrigger>();
                tooltipTrigger.Initialize(null, string.Empty, false, tooltipParent, panelRect, canvas);
            }

            private static Font GetDefaultFont()
            {
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _defaultFont;
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
            var header = new GameObject("TFTV_SwapHeader", typeof(RectTransform));
            header.transform.SetParent(parent, false);

            var layoutGroup = header.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.padding = new RectOffset(6, 6, 0, 0);
            layoutGroup.spacing = 8f;

            var headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.minHeight = 48f;
            headerLayout.preferredHeight = 48f;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)labelGO.transform;
            labelRect.SetParent(header.transform, false);
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);

            var text = labelGO.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 18;
            text.raycastTarget = false;
            text.color = Color.white;
            //   text.font = GetDefaultFont();
            text.text = $"Replace: {original?.ViewElementDef?.DisplayName1?.Localize() ?? original?.name}";

            var textLayout = labelGO.AddComponent<LayoutElement>();
            textLayout.minWidth = 0f;
            textLayout.flexibleWidth = 0f;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGO.transform;
            iconRect.SetParent(header.transform, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.localScale = Vector3.one;
            iconRect.anchoredPosition = Vector2.zero;

            var iconImage = iconGO.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.color = Color.white;

            var iconSprite = original?.ViewElementDef?.LargeIcon ?? original?.ViewElementDef?.SmallIcon;
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;

                var iconLayout = iconGO.AddComponent<LayoutElement>();
                const float iconSize = 48f;
                iconLayout.minWidth = iconSize;
                iconLayout.minHeight = iconSize;
                iconLayout.preferredWidth = iconSize;
                iconLayout.preferredHeight = iconSize;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            }
            else
            {
                iconGO.SetActive(false);
            }

           

            var labelFitter = labelGO.AddComponent<ContentSizeFitter>();
            labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

                if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                {
                    TFTVCommonMethods.SetStaminaToZero(character);
                    Reflection.CallPrivate(ui, "SetStatusesPanel");
                    Reflection.CallPrivate(ui, "RefreshStatusesPanel");
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

        private static class DrillInputHelper
        {
            public static bool TryGetCursorScreenPosition(InputController controller, out Vector2 position)
            {
                position = default;
                if (controller == null)
                {
                    return false;
                }

                Vector3 cursor = controller.GetCursorPosition();
                if (!IsValid(cursor))
                {
                    cursor = controller.GetCursorPosition(InputType.KeyboardMouse);
                }

                if (!IsValid(cursor))
                {
                    return false;
                }

                position = new Vector2(cursor.x, cursor.y);
                return true;
            }

            public static bool TryGetCursorScreenPosition(out Vector2 position)
            {
                return TryGetCursorScreenPosition(GameUtl.GameComponent<InputController>(), out position);
            }

            private static bool IsValid(Vector3 cursor)
            {
                return !float.IsNaN(cursor.x) && !float.IsNaN(cursor.y) && !float.IsInfinity(cursor.x) && !float.IsInfinity(cursor.y);
            }
        }

        private sealed class DrillOverlayController : MonoBehaviour
        {
            private const float HoverPadding = 40f;
            private const float TooltipPadding = 24f;
            private const float Gap = 12f;
            private const float IntroDistance = 40f;
            private const float IntroDuration = 0.18f;
            private const float OutroDuration = 0.14f;

            private static readonly Vector3[] CornerBuffer = new Vector3[4];

            private Canvas _canvas;
            private RectTransform _overlayRect;
            private RectTransform _panelRect;
            private RectTransform _anchorRect;
            private Button _backgroundButton;
            private CanvasGroup _canvasGroup;
            private RectTransform _viewportRect;
            private RectTransform _contentRect;
            private Vector2 _targetPosition;
            private bool _animating;
            private bool _closing;
            private Action _onClosed;
            private InputController _inputController;
            private TooltipSuppressor.Handle _tooltipSuppression;

            public void Initialize(Canvas canvas, RectTransform overlayRect, RectTransform panelRect, RectTransform anchorRect, Button backgroundButton)
            {
                _canvas = canvas;
                _overlayRect = overlayRect;
                _panelRect = panelRect;
                _anchorRect = anchorRect;
                _backgroundButton = backgroundButton;
                _canvasGroup = panelRect != null ? panelRect.GetComponent<CanvasGroup>() : null;
                _inputController = GameUtl.GameComponent<InputController>();
            }

            public void AttachTooltipSuppression(TooltipSuppressor.Handle handle)
            {
                _tooltipSuppression?.Dispose();
                _tooltipSuppression = handle;
            }

            public void ConfigureContent(RectTransform viewportRect, RectTransform contentRect, float width, float maxHeight)
            {
                _viewportRect = viewportRect;
                _contentRect = contentRect;

                if (_panelRect == null)
                {
                    return;
                }

                _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }

                if (_contentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
                    float preferred = LayoutUtility.GetPreferredHeight(_contentRect);
                    float height = Mathf.Clamp(preferred, 140f, maxHeight);
                    _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    if (_viewportRect != null)
                    {
                        _viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                        _viewportRect.offsetMin = new Vector2(_viewportRect.offsetMin.x, 0f);
                        _viewportRect.offsetMax = new Vector2(_viewportRect.offsetMax.x, 0f);
                    }
                }

                _targetPosition = CalculateTargetPosition();
                _panelRect.anchoredPosition = _targetPosition + new Vector2(IntroDistance, 0f);
                _animating = true;
                StopAllCoroutines();
                StartCoroutine(PlayIntro());
            }

            public void Close(Action onClosed = null)
            {
                if (_closing)
                {
                    return;
                }

                _closing = true;
                _onClosed = onClosed;

                StopAllCoroutines();
                StartCoroutine(PlayOutro());
            }

            private IEnumerator PlayIntro()
            {
                float elapsed = 0f;
                Vector2 start = _panelRect.anchoredPosition;
                Vector2 end = _targetPosition;
                while (elapsed < IntroDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / IntroDuration);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    }
                    yield return null;
                }

                _panelRect.anchoredPosition = end;
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                }
                _animating = false;
            }

            private IEnumerator PlayOutro()
            {
                if (_panelRect == null)
                {
                    yield break;
                }

                Vector2 start = _panelRect.anchoredPosition;
                Vector2 end = start + new Vector2(20f, 0f);
                float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
                float elapsed = 0f;

                while (elapsed < OutroDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / OutroDuration);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    _panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    }
                    yield return null;
                }

                _panelRect.anchoredPosition = end;
                _canvasGroup?.SetAlpha(0f);

                try
                {
                    _onClosed?.Invoke();
                }
                finally
                {
                    var handle = _tooltipSuppression;
                    _tooltipSuppression = null;
                    handle?.Dispose();
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            private void LateUpdate()
            {
                if (_panelRect == null)
                {
                    return;
                }

                if (_anchorRect == null || !_anchorRect.gameObject.activeInHierarchy)
                {
                    Close();
                    return;
                }

                if (!_closing && !_animating)
                {
                    _targetPosition = CalculateTargetPosition();
                    _panelRect.anchoredPosition = Vector2.Lerp(_panelRect.anchoredPosition, _targetPosition, Time.unscaledDeltaTime * 12f);
                }

                if (_closing)
                {
                    return;
                }

                if (_inputController == null)
                {
                    _inputController = GameUtl.GameComponent<InputController>();
                }

                if (!DrillInputHelper.TryGetCursorScreenPosition(_inputController, out var pointer))
                {
                    return;
                }

                if (!IsPointerNear(pointer))
                {
                    Close();
                }
            }

            private void OnDestroy()
            {
                var handle = _tooltipSuppression;
                _tooltipSuppression = null;
                handle?.Dispose();
            }

            private Vector2 CalculateTargetPosition()
            {
                if (_overlayRect == null || _panelRect == null)
                {
                    return Vector2.zero;
                }

                Vector2 anchorLocal = Vector2.zero;
                if (_anchorRect != null)
                {
                    _anchorRect.GetWorldCorners(CornerBuffer);
                    Vector3 topLeftWorld = CornerBuffer[1];
                    Camera camera = GetCameraForCanvas(_canvas);
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, topLeftWorld);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, screenPoint, camera, out anchorLocal))
                    {
                        anchorLocal = Vector2.zero;
                    }
                }

                float canvasHalfWidth = _overlayRect.rect.width * 0.5f;
                float canvasHalfHeight = _overlayRect.rect.height * 0.5f;
                float panelHeight = _panelRect.rect.height;

                float minTop = -canvasHalfHeight + panelHeight + 8f;
                float maxTop = canvasHalfHeight - 8f;
                float topEdge = Mathf.Clamp(anchorLocal.y, minTop, maxTop);
                float pivotOffsetY = (1f - _panelRect.pivot.y) * panelHeight;
                float anchoredY = topEdge - pivotOffsetY;

                float desiredRight = anchorLocal.x - Gap;
                float leftEdge = desiredRight - _panelRect.rect.width;
                float minLeft = -canvasHalfWidth + 12f;
                if (leftEdge < minLeft)
                {
                    desiredRight += minLeft - leftEdge;
                }

                return new Vector2(desiredRight, anchoredY);
            }

            private bool IsPointerNear(Vector2 screenPoint)
            {
                if (ContainsWithPadding(_panelRect, screenPoint, HoverPadding, _canvas))
                {
                    return true;
                }

                if (_anchorRect != null && ContainsWithPadding(_anchorRect, screenPoint, HoverPadding, _anchorRect.GetComponentInParent<Canvas>()))
                {
                    return true;
                }

                var tooltipRect = DrillTooltipTrigger.ActiveTooltipRect;
                if (tooltipRect != null && tooltipRect.gameObject.activeInHierarchy)
                {
                    var tooltipCanvas = tooltipRect.GetComponentInParent<Canvas>();
                    if (ContainsWithPadding(tooltipRect, screenPoint, TooltipPadding, tooltipCanvas))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool ContainsWithPadding(RectTransform rect, Vector2 screenPoint, float padding, Canvas canvas)
            {
                if (rect == null)
                {
                    return false;
                }

                rect.GetWorldCorners(CornerBuffer);
                Camera camera = GetCameraForCanvas(canvas);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[0]);
                Vector2 max = min;
                for (int i = 1; i < 4; i++)
                {
                    Vector2 corner = RectTransformUtility.WorldToScreenPoint(camera, CornerBuffer[i]);
                    min = Vector2.Min(min, corner);
                    max = Vector2.Max(max, corner);
                }

                min -= new Vector2(padding, padding);
                max += new Vector2(padding, padding);
                return screenPoint.x >= min.x && screenPoint.x <= max.x && screenPoint.y >= min.y && screenPoint.y <= max.y;
            }

            private static Camera GetCameraForCanvas(Canvas canvas)
            {
                if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                return canvas.worldCamera;
            }
        }

        private sealed class DrillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private TacticalAbilityDef _ability;
            private bool _isLocked;
            private string _missingRequirements;
            private Transform _tooltipParent;
            private RectTransform _menuRect;
            private RectTransform _selfRect;
            private Canvas _canvas;
            private bool _tooltipVisible;
            private bool _isTooltipOwner;
            private Coroutine _hideRoutine;
            private static GeoRosterAbilityDetailTooltip _sharedTooltip;
            private static Canvas _tooltipCanvas;
            private static bool _tooltipPrimed;
            private static DrillTooltipTrigger _currentTooltipOwner;
            private static readonly Vector3[] TooltipCorners = new Vector3[4];

            private const float TooltipGap = 16f;
            private const int DrillSkillPointCost = 10;
            public static RectTransform ActiveTooltipRect { get; private set; }

            public void Initialize(TacticalAbilityDef ability, string missingRequirements, bool isLocked, Transform tooltipParent, RectTransform menuRect, Canvas canvas)
            {
                _ability = ability;
                _missingRequirements = missingRequirements;
                _isLocked = isLocked;
                _tooltipParent = tooltipParent;
                _menuRect = menuRect;
                _canvas = canvas;
                _selfRect = transform as RectTransform;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                    _hideRoutine = null;
                }

                if (_tooltipVisible && _isTooltipOwner)
                {
                    PositionTooltip();
                    return;
                }

                ShowTooltip();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                }

                if (_isTooltipOwner)
                {
                    _isTooltipOwner = false;
                }


                _hideRoutine = StartCoroutine(DelayedHide());
            }

            private void LateUpdate()
            {
                if (_tooltipVisible && _isTooltipOwner)
                {
                    PositionTooltip();
                }
            }

            private void OnDisable()
            {
                if (_hideRoutine != null)
                {
                    StopCoroutine(_hideRoutine);
                    _hideRoutine = null;
                }

                HideTooltip();
            }

            private IEnumerator DelayedHide()
            {
                yield return null;
                yield return new WaitForSecondsRealtime(0.05f);

                if (!ShouldKeepTooltipVisible())
                {
                    HideTooltip();
                }

                _hideRoutine = null;
            }

            private bool ShouldKeepTooltipVisible()
            {
                if (!DrillInputHelper.TryGetCursorScreenPosition(out var pointer))
                {
                    return false;
                }

                Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

                if (_selfRect != null && RectTransformUtility.RectangleContainsScreenPoint(_selfRect, pointer, camera))
                {
                    return true;
                }

                if (_menuRect != null && RectTransformUtility.RectangleContainsScreenPoint(_menuRect, pointer, camera))
                {
                    return true;
                }

                var tooltipRect = ActiveTooltipRect;
                if (tooltipRect != null && tooltipRect.gameObject.activeInHierarchy)
                {
                    Canvas tooltipCanvas = tooltipRect.GetComponentInParent<Canvas>();
                    Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? tooltipCanvas.worldCamera : null;
                    if (RectTransformUtility.RectangleContainsScreenPoint(tooltipRect, pointer, tooltipCamera))
                    {
                        return true;
                    }
                }

                return false;
            }

            private void ShowTooltip()
            {
                var tooltip = EnsureTooltip();
                var view = _ability?.ViewElementDef;
                if (tooltip == null || view == null)
                {
                    return;
                }

                if (_currentTooltipOwner != null && _currentTooltipOwner != this)
                {
                    _currentTooltipOwner._isTooltipOwner = false;
                    _currentTooltipOwner._tooltipVisible = false;
                }

                var originalTitle = view.DisplayName1;
                string titleText = $"<color=#FF4C00>{originalTitle.Localize()}</color>";
                LocalizedTextBind temporaryTitle = new LocalizedTextBind(titleText, true);
                view.DisplayName1 = temporaryTitle;

                var originalDescription = view.Description;
                LocalizedTextBind temporaryDescription = null;

                if (_isLocked && !string.IsNullOrEmpty(_missingRequirements))
                {
                    string descriptionText = originalDescription?.Localize() ?? string.Empty;
                    if (!string.IsNullOrEmpty(descriptionText))
                    {
                        descriptionText += "\n\n";
                    }

                    descriptionText += $"<color=#E21515><b>Missing requirements:</b>\n{_missingRequirements}</color>";
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
                    tooltip.Show(_ability, view, useMutagens: false, cost: DrillSkillPointCost);
                    if (shouldPrime)
                    {
                        tooltip.Hide();
                        tooltip.Show(_ability, view, useMutagens: false, cost: DrillSkillPointCost);
                    }

                    ApplyTooltipCostOverrides(tooltip);
                    tooltip.transform.SetAsLastSibling();
                    ActiveTooltipRect = tooltip.transform as RectTransform;
                    PositionTooltip();
                    _tooltipVisible = true;
                    _isTooltipOwner = true;
                    _currentTooltipOwner = this;
                }
                finally
                {
                    if (temporaryDescription != null)
                    {
                        view.Description = originalDescription;
                    }

                    if(temporaryTitle != null) 
                    { 
                        view.DisplayName1 = originalTitle;
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

                if (ActiveTooltipRect != null && tooltip != null && tooltip.transform == ActiveTooltipRect.transform)
                {
                    ActiveTooltipRect = null;
                }

                _tooltipVisible = false;

                if (_isTooltipOwner)
                {
                    _isTooltipOwner = false;
                }
                if (_currentTooltipOwner == this)
                {
                    _currentTooltipOwner = null;
                }
            }

            private void PositionTooltip()
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

                var overlayCanvas = _menuRect != null ? _menuRect.GetComponentInParent<Canvas>() : null;
                if (_tooltipParent != null && tooltip.transform.parent != _tooltipParent)
                {
                    tooltip.transform.SetParent(_tooltipParent, false);
                    _tooltipCanvas = null;
                }

                var canvas = _tooltipCanvas;
                if (canvas == null)
                {
                    canvas = tooltip.GetComponentInParent<Canvas>();
                    _tooltipCanvas = canvas;
                }

                if (overlayCanvas != null && canvas != overlayCanvas)
                {
                    tooltip.transform.SetParent(overlayCanvas.transform, false);
                    canvas = overlayCanvas;
                    _tooltipCanvas = canvas;
                }

                if (canvas == null || !(canvas.transform is RectTransform canvasRect))
                {
                    return;
                }

                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                if (_menuRect == null)
                {
                    return;
                }

                _menuRect.GetWorldCorners(TooltipCorners);
                Vector3 overlayTopLeft = TooltipCorners[1];
                Vector2 overlayTopLeftScreen = RectTransformUtility.WorldToScreenPoint(camera, overlayTopLeft);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, overlayTopLeftScreen, camera, out var localPoint))
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(1f, 1f);
                    rectTransform.anchoredPosition = new Vector2(localPoint.x - TooltipGap, localPoint.y);
                }
            }

            private void ApplyTooltipCostOverrides(GeoRosterAbilityDetailTooltip tooltip)
            {
                if (tooltip == null)
                {
                    return;
                }

                if (tooltip.AbilitySkillCostText != null && tooltip.AbilitySkillCostText.transform?.parent != null)
                {
                    tooltip.AbilitySkillCostText.transform.parent.gameObject.SetActive(true);
                    string spFormat = tooltip.SPCostTextKey != null ? tooltip.SPCostTextKey.Localize() : "Skill Points";
                    if (tooltip.SkillCostHeaderText != null)
                    {
                        tooltip.SkillCostHeaderText.text = spFormat;
                    }

                    string spPattern = !string.IsNullOrEmpty(tooltip.SPCostPattern) ? tooltip.SPCostPattern : "{0}";
                    tooltip.AbilitySkillCostText.text = string.Format(spPattern, DrillSkillPointCost);
                }

                if (_ability == null)
                {
                    return;
                }

                int apCost = Mathf.RoundToInt(_ability.ActionPointCost);
                int wpCost = Mathf.RoundToInt(_ability.WillPointCost);

                if (tooltip.AbilitySkillCostGroup != null)
                {
                    tooltip.AbilitySkillCostGroup.SetActive(apCost > 0 || wpCost > 0);
                }

                if (tooltip.AbilitySkillAPCostText != null)
                {
                    tooltip.AbilitySkillAPCostText.gameObject.SetActive(apCost > 0);
                    if (apCost > 0)
                    {
                        string apFormat = tooltip.APCostTextKey != null ? tooltip.APCostTextKey.Localize() : "{0}";
                        tooltip.AbilitySkillAPCostText.text = string.Format(apFormat, apCost);
                    }
                }

                if (tooltip.AbilitySkillWPCostText != null)
                {
                    tooltip.AbilitySkillWPCostText.gameObject.SetActive(wpCost > 0);
                    if (wpCost > 0)
                    {
                        string wpFormat = tooltip.WPCostTextKey != null ? tooltip.WPCostTextKey.Localize() : "{0}";
                        tooltip.AbilitySkillWPCostText.text = string.Format(wpFormat, wpCost);
                    }
                }
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

            public static bool IsSharedTooltip(GeoRosterAbilityDetailTooltip tooltip)
            {
                return tooltip != null && _sharedTooltip == tooltip;
            }
        }

        private static class TooltipSuppressor
        {
            internal sealed class Handle : IDisposable
            {
                private readonly List<GameObject> _disabledTooltips;
                private bool _disposed;

                internal Handle(List<GameObject> disabledTooltips)
                {
                    _disabledTooltips = disabledTooltips;
                }

                public void Dispose()
                {
                    if (_disposed)
                    {
                        return;
                    }

                    foreach (var go in _disabledTooltips)
                    {
                        if (go != null)
                        {
                            go.SetActive(true);
                        }
                    }

                    _disabledTooltips.Clear();
                    _disposed = true;
                }
            }

            public static Handle Begin()
            {
                var disabled = new List<GameObject>();
                foreach (var tooltip in Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>())
                {
                    TFTVLogger.Always($"{tooltip.AbilityTitleText.text} {tooltip == null} {DrillTooltipTrigger.IsSharedTooltip(tooltip)} {tooltip.gameObject.activeSelf}");

                    if (tooltip == null || DrillTooltipTrigger.IsSharedTooltip(tooltip))
                    {
                        continue;
                    }

                    tooltip.Hide();

                    if (tooltip.gameObject.activeSelf)
                    {
                        
                        tooltip.gameObject.SetActive(false);
                        disabled.Add(tooltip.gameObject);
                    }
                }

                return new Handle(disabled);
            }
        }
    }

    internal static class CanvasGroupExtensions
    {
        public static void SetAlpha(this CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}