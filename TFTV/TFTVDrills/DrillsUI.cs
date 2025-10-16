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
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal class DrillsUI
    {
        private const int SwapSpCost = 0;

        private static readonly Color FilterActiveColor = new Color(0.25f, 0.55f, 0.85f, 0.6f);
        private static readonly Color FilterInactiveColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color LockedIconTint = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color LockedLabelTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color LockedBackgroundTint = new Color(1f, 1f, 1f, 0.03f);

        [HarmonyPatch(typeof(AbilityTrackSkillEntryElement), "OnPointerClick")]
        public static class AbilityTrackSkillEntryElement_OnPointerClick_Patch
        {
            public static bool Prefix(AbilityTrackSkillEntryElement __instance)
            {
                try
                {

                    if(!TFTVAircraftReworkMain.AircraftReworkOn) { return true; }

                    TFTVLogger.Always($"AbilityTrackSkillEntryElement OnPointerClick invoked");
                    // Find the owning progression UI
                    var ui = __instance.GetComponentInParent<UIModuleCharacterProgression>();
                    if (ui == null) return true;

                    // character + current ability
                    var character = GetPrivate<GeoCharacter>(ui, "_character");
                    if (character?.Progression == null) return true;

                    var ability = __instance.AbilityDef;
                    if (ability == null)
                    {
                        // fallback: some builds store ability only on slot
                        var slotFromElem = FindSlotFieldOnElement(__instance);
                        ability = slotFromElem?.Ability;
                    }
                    if (ability == null) return true;

                    // Only intercept if the ability is already learned
                    if (!character.Progression.Abilities.Contains(ability)) return true;

                    // Build a slot if we can (nice-to-have, not strictly required)
                    var (_, slot) = FindTrackSlotForAbility(character, ability);

                   

                    // We don't care about source here; your ShowReplacementPopup ignores it.
                    AbilityTrackSource dummySource = AbilityTrackSource.Personal;

                    GeoPhoenixFaction phoenixFaction = character?.Faction?.GeoLevel?.PhoenixFaction;
                    List<TacticalAbilityDef> availableChoices = DrillsDefs.GetAvailableDrills(phoenixFaction, character);

                    // Reuse your existing popup
                    ShowReplacementPopup(ui, slot, ability, dummySource, availableChoices);

                    // Swallow vanilla click
                    return false;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw; // fallback to vanilla on error
                }
            }

            static (AbilityTrack track, AbilityTrackSlot slot) FindTrackSlotForAbility(GeoCharacter c, TacticalAbilityDef def)
            {
                foreach (var tr in c.Progression.AbilityTracks)
                    foreach (var s in tr.AbilitiesByLevel)
                        if (s?.Ability == def) return (tr, s);
                return (null, null);
            }

            static AbilityTrackSlot FindSlotFieldOnElement(AbilityTrackSkillEntryElement elem)
            {
                var f = elem.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .FirstOrDefault(fi => typeof(AbilityTrackSlot).IsAssignableFrom(fi.FieldType));
                return f != null ? (AbilityTrackSlot)f.GetValue(elem) : null;
            }

            static T GetPrivate<T>(object obj, string field)
            {
                var f = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                return f != null ? (T)f.GetValue(obj) : default;
            }

            private static void ShowReplacementPopup(
UIModuleCharacterProgression ui,
AbilityTrackSlot slot,
TacticalAbilityDef original,
AbilityTrackSource source,
List<TacticalAbilityDef> choices)
            {



                // If the built-in DualClass popup is present, use it
                var popupGO = GetPrivate<GameObject>(ui, "DualClassPopupWindow");
                var container = GetPrivate<GameObject>(ui, "DualClassButtonsContainer");
                var prefab = GetPrivate<GameObject>(ui, "DualClassButtonsPrefab");

                if (popupGO != null && container != null && prefab != null)
                {
                    foreach (Transform child in container.transform)
                        UnityEngine.Object.Destroy(child.gameObject);

                    AddHeader(container.transform, original);

                    foreach (var def in choices)
                    {
                        var go = UnityEngine.Object.Instantiate(prefab, container.transform);
                        WireButton(go, def, () =>
                        {
                            TryPerformSwap(ui, slot, original, def, source);
                            popupGO.SetActive(false);
                        });
                    }

                    AddCancel(container.transform, () => popupGO.SetActive(false));
                    popupGO.SetActive(true);
                    return;
                }

                // ---- Fallback: build a lightweight modal on the fly ----
                BuildSimplePopup(ui, slot, original, source, choices);
            }


            private static void BuildSimplePopup(
                UIModuleCharacterProgression ui,
                AbilityTrackSlot slot,
                TacticalAbilityDef original,
                AbilityTrackSource source,
                List<TacticalAbilityDef> choices)
            {
               
                var overlay = new GameObject("TFTV_SwapOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                var overlayRect = (RectTransform)overlay.transform;
                overlayRect.SetParent(ui.transform, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                var overlayBg = overlay.GetComponent<Image>();
                overlayBg.color = new Color(0f, 0f, 0f, 0.55f);
                var overlayButton = overlay.GetComponent<Button>();
                overlayButton.transition = Selectable.Transition.None;
                overlayButton.onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));

                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                float panelWidth = Mathf.Clamp(screenWidth * 0.60f, 800f, 1400f);
                float panelHeight = Mathf.Clamp(screenHeight * 0.70f, 560f, 900f);

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button));
                var panelRect = (RectTransform)panel.transform;
                panelRect.SetParent(overlay.transform, false);
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
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

                var titleText = title.GetComponent<Text>();
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.fontSize = 22;
                titleText.text = $"Replace: {(original.ViewElementDef?.DisplayName1?.Localize() ?? original.name)}";

                var filterBar = new GameObject("FilterBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
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

                // ---- scroll view (Viewport + Content) ----
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));

                var viewportRect = (RectTransform)viewport.transform;
                viewportRect.SetParent(panel.transform, false);
                viewportRect.anchorMin = new Vector2(0, 0);
                viewportRect.anchorMax = new Vector2(1, 1);
                const float viewportHorizontalPadding = 16f;
                const float viewportBottomPadding = 96f;
                const float viewportTopExtraPadding = 16f;

                float filterTopOffset = Mathf.Abs(filterRect.anchoredPosition.y);
                float filterHeight = filterRect.rect.height > 0 ? filterRect.rect.height : filterRect.sizeDelta.y;
                float viewportTopPadding = filterTopOffset + filterHeight + viewportTopExtraPadding;

                viewportRect.offsetMin = new Vector2(viewportHorizontalPadding, viewportBottomPadding);
                viewportRect.offsetMax = new Vector2(-viewportHorizontalPadding, -viewportTopPadding);
                viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

                var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));

                var contentRect = (RectTransform)content.transform;
                contentRect.SetParent(viewport.transform, false);
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, 0);

                var grid = content.GetComponent<GridLayoutGroup>();
                const int ICON = 128;
                grid.cellSize = new Vector2(ICON + 56, ICON + 40);
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

          
                var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                var cancelRect = (RectTransform)cancel.transform;
                cancelRect.SetParent(panel.transform, false);
                cancelRect.anchorMin = new Vector2(0.5f, 0);
                cancelRect.anchorMax = new Vector2(0.5f, 0);
                cancelRect.pivot = new Vector2(0.5f, 0);
                cancelRect.anchoredPosition = new Vector2(0, 12);
                cancelRect.sizeDelta = new Vector2(160, 36);

                cancel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
                var cancelLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var cancelLabelRect = (RectTransform)cancelLabel.transform;
                cancelLabelRect.SetParent(cancel.transform, false);
                cancelLabelRect.anchorMin = Vector2.zero;
                cancelLabelRect.anchorMax = Vector2.one;
                cancelLabelRect.offsetMin = Vector2.zero;
                cancelLabelRect.offsetMax = Vector2.zero;

                var cancelText = cancelLabel.GetComponent<Text>();
                cancelText.alignment = TextAnchor.MiddleCenter;
                cancelText.text = "Cancel";         
                cancel.GetComponent<Button>().onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));

                var toggleGroup = filterBar.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = false;
                var availableToggle = CreateFilterToggle(filterBar.transform, "Available To Character");
                var allToggle = CreateFilterToggle(filterBar.transform, "All Drills");
                availableToggle.group = toggleGroup;
                allToggle.group = toggleGroup;

                var character = GetPrivate<GeoCharacter>(ui, "_character");
                var phoenixFaction = GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);

                var availableChoices = choices?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();
                var allChoices = DrillsDefs.Drills?.Where(def => def != null).Distinct().ToList() ?? new List<TacticalAbilityDef>();

                var canvas = ui.GetComponentInParent<Canvas>();
                Transform tooltipParent = canvas != null ? canvas.transform : ui.transform;

                void Populate(bool showAll)
                {
                    for (int i = content.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = content.transform.GetChild(i);
                        UnityEngine.Object.Destroy(child.gameObject);
                    }

                    var sourceList = showAll ? allChoices : availableChoices;
                    foreach (var def in sourceList)
                    {
                        bool unlocked = DrillsDefs.IsDrillUnlocked(phoenixFaction, character, def);
                        bool locked = showAll && !unlocked;

                        string missingRequirements = string.Empty;
                        if (locked)
                        {
                            var missingParts = DrillsDefs.GetMissingRequirementDescriptions(phoenixFaction, character, def)?.ToList();
                            if (missingParts != null && missingParts.Count > 0)
                            {
                                missingRequirements = string.Join("\n", missingParts);
                            }
                        }

                        System.Action onChoose = null;
                        if (!locked)
                        {
                            onChoose = () =>
                            {
                                TryPerformSwap(ui, slot, original, def, source);
                                UnityEngine.Object.Destroy(overlay);
                            };
                        }

                        var card = CreateChoiceCard(def, ICON, onChoose, locked, missingRequirements, tooltipParent);
                        card.transform.SetParent(content.transform, false);
                    }

                    int itemCount = sourceList.Count;
                    int rows = itemCount > 0 ? Mathf.CeilToInt((float)itemCount / columns) : 0;
                    float height = rows > 0
                        ? rows * grid.cellSize.y + (rows - 1) * grid.spacing.y + 16f
                        : 16f;
                    contentRect.sizeDelta = new Vector2(0, height);
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
                        Populate(showAll: false);
                    }
                });

                allToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleVisual(allToggle, isOn);
                    if (isOn)
                    {
                        UpdateToggleVisual(availableToggle, false);
                        Populate(showAll: true);
                    }
                });

                availableToggle.isOn = true;
                UpdateToggleVisual(availableToggle, true);
                UpdateToggleVisual(allToggle, false);
                Populate(showAll: false);

                overlay.transform.SetAsLastSibling();
            }

            private static GameObject CreateChoiceCard(
                 TacticalAbilityDef def,
                 int iconSize,
                 System.Action onChoose,
                 bool isLocked,
                 string missingRequirements,
                 Transform tooltipParent)
            {
                var card = new GameObject(def?.name ?? "Ability", typeof(RectTransform), typeof(Image), typeof(Button));
                var rt = (RectTransform)card.transform;
                rt.sizeDelta = new Vector2(iconSize + 56, iconSize + 40);

                var bg = card.GetComponent<Image>();
                bg.color = isLocked ? LockedBackgroundTint : new Color(1f, 1f, 1f, 0.08f);
                var btn = card.GetComponent<Button>();
                btn.interactable = !isLocked && onChoose != null;
                if (onChoose != null)
                {
                    btn.onClick.AddListener(() => onChoose?.Invoke());
                }

                var cardCanvasGroup = card.AddComponent<CanvasGroup>();
                cardCanvasGroup.alpha = isLocked ? 0.45f : 1f;
                // icon
                var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var irt = (RectTransform)ico.transform; irt.SetParent(card.transform, false);
                irt.anchorMin = new Vector2(0.5f, 1); irt.anchorMax = new Vector2(0.5f, 1);
                irt.pivot = new Vector2(0.5f, 1); irt.anchoredPosition = new Vector2(0, -8);
                irt.sizeDelta = new Vector2(iconSize, iconSize);

                var iconImg = ico.GetComponent<Image>();
                iconImg.sprite = def?.ViewElementDef?.LargeIcon ?? def?.ViewElementDef?.SmallIcon;
                iconImg.preserveAspect = true;
                iconImg.color = isLocked ? LockedIconTint : Color.white;
                // label
                iconImg.raycastTarget = false;
                var lab = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var lrt = (RectTransform)lab.transform; lrt.SetParent(card.transform, false);
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0); lrt.anchoredPosition = new Vector2(0, 6);
                lrt.sizeDelta = new Vector2(0, 22);

                var txt = lab.GetComponent<Text>();
                txt.alignment = TextAnchor.MiddleCenter;
                txt.resizeTextForBestFit = true; txt.resizeTextMinSize = 12; txt.resizeTextMaxSize = 18;
                txt.text = def?.ViewElementDef?.DisplayName1?.Localize() ?? def?.name ?? "Ability";
                txt.color = isLocked ? LockedLabelTint : Color.white;
                txt.raycastTarget = false;
                var tooltipTrigger = card.AddComponent<DrillTooltipTrigger>();
                tooltipTrigger.Initialize(def, missingRequirements, isLocked, tooltipParent);

                return card;
            }

            private static Toggle CreateFilterToggle(Transform parent, string label)
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

            private static void AddCancel(Transform parent, System.Action onClick)
            {
                var go = new GameObject("TFTV_Cancel", typeof(RectTransform), typeof(Button), typeof(Image));
                go.transform.SetParent(parent, false);
                var img = go.GetComponent<Image>(); // give it some bg so it's clickable
                var btn = go.GetComponent<Button>();

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var label = labelGo.GetComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.text = "Cancel";

                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            private static void WireButton(GameObject buttonGO, TacticalAbilityDef def, System.Action onClick)
            {
                // Try to find a PhoenixGeneralButton if prefab uses it, otherwise fall back to Unity Button
                var pgb = buttonGO.GetComponentInChildren<PhoenixGeneralButton>();
                if (pgb != null)
                {
                    var text = pgb.GetComponentInChildren<Text>();
                    if (text != null)
                        text.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name;

                    // Optional: set icon if the prefab has an Image under it
                    var img = pgb.GetComponentInChildren<Image>();
                    if (img != null && def.ViewElementDef?.LargeIcon != null)
                        img.sprite = def.ViewElementDef.LargeIcon;

                    pgb.BaseButton.onClick.AddListener(() => onClick?.Invoke());
                    return;
                }

                // Fallback
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

            private static void TryPerformSwap(
                UIModuleCharacterProgression ui,
                AbilityTrackSlot slot,
                TacticalAbilityDef original,
                TacticalAbilityDef replacement,
                AbilityTrackSource source)
            {
                try
                {
                    var character = GetPrivate<GeoCharacter>(ui, "_character");
                    var phoenixFaction = GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction") ?? (character?.Faction?.GeoLevel?.PhoenixFaction);
                    if (character == null || character.Progression == null)
                        return;

                    if (!DrillsDefs.IsDrillUnlocked(phoenixFaction, character, replacement))
                    {
                        TFTVLogger.Always($"[TFTV Drills] Attempted to swap to locked drill {replacement?.name}; aborting swap.");
                        return;
                    }

                    // (Optional) cost handling
                    if (SwapSpCost > 0)
                    {
                        // Pay with soldier first then faction, like base stat purchase does.
                        var currSP = GetPrivate<int>(ui, "_currentSkillPoints");
                        var currFP = GetPrivate<int>(ui, "_currentFactionPoints");
                        int remaining = SwapSpCost;

                        if (currSP >= remaining)
                        {
                            SetPrivate(ui, "_currentSkillPoints", currSP - remaining);
                        }
                        else
                        {
                            remaining -= currSP;
                            SetPrivate(ui, "_currentSkillPoints", 0);
                            if (currFP >= remaining)
                                SetPrivate(ui, "_currentFactionPoints", currFP - remaining);
                            else
                            {
                                Debug.LogWarning("[TFTV] Not enough SP/FS for swap; aborting.");
                                return;
                            }
                        }
                    }



                    List<TacticalAbilityDef> abilities = Traverse.Create(character.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();


                    if (abilities.Contains(original))
                        abilities.Remove(original);


                    if (!abilities.Contains(replacement))
                        abilities.Add(replacement);

                    if (replacement.name.Contains("fieldpromotion"))
                    {
                        TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points before Field Promotion");
                        UIModuleCharacterProgression uIModuleCharacterProgression = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.CharacterProgressionModule;

                        FieldInfo fieldInfo = typeof(UIModuleCharacterProgression).GetField("_currentSkillPoints", BindingFlags.Instance | BindingFlags.NonPublic);
                        fieldInfo.SetValue(uIModuleCharacterProgression, character.Progression.SkillPoints + 30);

                        uIModuleCharacterProgression.CommitStatChanges();

                        TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points after Field Promotion");

                    }

                    // Update the slot mapping to show the new one in the track UI
                    slot.Ability = replacement;



                    // Refresh UI
                    // Commit changes like the module does after purchases
                    CallPrivate(ui, "CommitStatChanges");
                    CallPrivate(ui, "RefreshStatPanel");
                    CallPrivate(ui, "SetAbilityTracks");
                    CallPrivate(ui, "RefreshAbilityTracks");
                }
                catch (Exception e)
                {
                    TFTVLogger.Always($"[TFTV] Ability swap failed: {e}");
                }
            }

            // ---- small reflection helpers ----


            private static void SetPrivate<T>(object obj, string field, T value)
            {
                var f = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(obj, value);
            }

            private static void CallPrivate(object obj, string method)
            {
                obj.GetType()
                   .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                   ?.Invoke(obj, null);
            }
        }
    }
}
    

