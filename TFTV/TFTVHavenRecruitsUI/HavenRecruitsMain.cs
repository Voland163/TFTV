using Base.Core;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using SoftMasking.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVHavenRecruitsUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.HavenRecruitsDetailsPanel;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsOverlayAnimator;
using HavenRecruitsRecruitItem = TFTV.TFTVHavenRecruitsUI.HavenRecruitsRecruitItem;
using HavenRecruitsUtils = TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;
using Object = UnityEngine.Object;


namespace TFTV
{
    class HavenRecruitsMain
    {

        public static void ClearInternalData()
        {
            try
            {
                _sortGroup = null;
                _sortToggles.Clear();
                RecruitOverlayManager.ResetState();
                _recruitListRoot = null;
                _totalRecruitsLabel = null;
                _factionTabGroup = null;
                _factionTabs.Clear();
                _activeFactionFilter = FactionFilter.Anu;

            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        internal static readonly SharedData Shared = TFTVMain.Shared;

        // Spacing / sizing

        // width (as fraction of screen width) used by the 3-column area
        internal const float ColumnsWidthPercent = 0.25f;  // baseline fraction used for overlay width
        internal const float OverlayMinWidthPx = 650f;
        internal const float OverlayMaxWidthFraction = 0.98f;

        internal const float OverlayTopMargin = 0.06f;
        internal const float OverlayBottomMargin = 0.16f;
        internal const float OverlayRightMargin = 0.0f;
        internal const float OverlayLeftMargin = 0.0f;

        internal const float ItemSpacing = 6f;     // space between cards
        internal const int AbilityIconSize = 40;  // abilities
        internal const int ClassIconSize = 46;    // class badge on list entry
        internal const int ArmorIconSize = 48;
        internal const int ResourceIconSize = 24;
        internal const int TextFontSize = 20;
        internal const float AbilityIconsCenterOffsetPx = -160f;



        internal static readonly Color HeaderBackgroundColor = HexToColor("16222a");
        internal static readonly Color HeaderBorderColor = HexToColor("222e40");
        internal static readonly Color TabBorderColor = HexToColor("55606F");
        internal static readonly Color TabHighlightColor = HexToColor("ffb339");
        internal static readonly Color TabDefaultColor = HexToColor("060B16");
        internal static readonly Color CardBackgroundColor = HexToColor("#1E2026");
        internal static readonly Color CardSelectedColor = HexToColor("#ffc02c");
        internal static readonly Color CardBorderColor = HexToColor("#233044");
        internal static readonly Color CardSelectedBorderColor = HexToColor("#fb9716");
        internal static readonly Color DetailSubTextColor = new Color(0.75f, 0.8f, 0.9f, 1f);

        internal sealed class RecruitAtSite
        {
            public GeoUnitDescriptor Recruit;
            public GeoSite Site;
            public GeoHaven Haven;
            public GeoFaction HavenOwner;
        }

        internal static float GetOverlayWidthFraction(out float resolvedPixelWidth)
        {
            float screenWidth = Mathf.Max(1f, Screen.width);
            float fraction = Mathf.Max(ColumnsWidthPercent, OverlayMinWidthPx / screenWidth);
            fraction = Mathf.Min(fraction, OverlayMaxWidthFraction);
            resolvedPixelWidth = fraction * screenWidth;
            return fraction;
        }

        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return Color.white;
            }

            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }

            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
        }



        internal static Font PuristaSemibold = null;

        internal enum SortMode { Level, Class, Distance }
        internal static SortMode _sortMode = SortMode.Level;

        internal static ToggleGroup _sortGroup;
        internal static readonly Dictionary<SortMode, Toggle> _sortToggles = new Dictionary<SortMode, Toggle>();

        internal static Transform _recruitListRoot;
        internal static Text _totalRecruitsLabel;

        internal static GeoRosterAbilityDetailTooltip OverlayAbilityTooltip { get; private set; }
        internal static RectTransform OverlayRootRect { get; private set; }
        internal static UIInventoryTooltip OverlayItemTooltip { get; private set; }
        internal static void RegisterOverlayAbilityTooltip(GeoRosterAbilityDetailTooltip tooltip)
        {
            OverlayAbilityTooltip = tooltip;
        }
        

        internal static Canvas OverlayCanvas { get; private set; }
        internal enum FactionFilter
        {
            Anu,
            NewJericho,
            Synedrion
        }

        internal sealed class FactionTabUI
        {
            public Toggle Toggle;
            public Image Background;
            public Text CountLabel;
            public Image Icon;
        }

        internal static ToggleGroup _factionTabGroup;
        internal static readonly Dictionary<FactionFilter, FactionTabUI> _factionTabs = new Dictionary<FactionFilter, FactionTabUI>();
        internal static readonly Dictionary<FactionFilter, Sprite> _factionIconCache = new Dictionary<FactionFilter, Sprite>();
        internal static FactionFilter _activeFactionFilter = FactionFilter.Anu;

        internal sealed class RecruitCardView : MonoBehaviour
        {
            public Image ClassIconImage;
            public Color ClassIconDefaultColor = Color.white;
            public Text LevelLabel;
            public Color LevelDefaultColor = Color.white;
            public Text NameLabel;
            public Color NameDefaultColor = Color.white;

            private readonly List<Text> _resourceAmountLabels = new List<Text>();
            private readonly List<Color> _resourceAmountDefaultColors = new List<Color>();

            public void RegisterResourceAmount(Text label)
            {
                if (label == null)
                {
                    return;
                }

                _resourceAmountLabels.Add(label);
                _resourceAmountDefaultColors.Add(label.color);
            }

            public void SetSelected(bool selected)
            {
                if (ClassIconImage != null)
                {
                    ClassIconImage.color = selected ? Color.black : ClassIconDefaultColor;
                }

                if (LevelLabel != null)
                {
                    LevelLabel.color = selected ? Color.black : LevelDefaultColor;
                }

                if (NameLabel != null)
                {
                    NameLabel.color = selected ? Color.black : NameDefaultColor;
                }

                for (int i = 0; i < _resourceAmountLabels.Count; i++)
                {
                    var label = _resourceAmountLabels[i];
                    if (label == null)
                    {
                        continue;
                    }

                    label.color = selected ? Color.black : _resourceAmountDefaultColors[i];
                }
            }
        }



        // Hook you can set from outside if you want:
        public static Action<GeoUnitDescriptor, GeoSite> OnCardDoubleClick;



        // Handles single vs double click without firing both
        internal sealed class CardClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public Action OnSingle;
            public Action OnDouble;
            public float doubleClickDelay = 0.25f;  // seconds, unscaled
            private Coroutine pendingSingle;

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button != PointerEventData.InputButton.Left) return;

                if (e.clickCount >= 2)
                {
                    if (pendingSingle != null) { StopCoroutine(pendingSingle); pendingSingle = null; }
                    OnDouble?.Invoke();
                }
                else
                {
                    if (pendingSingle != null) StopCoroutine(pendingSingle);
                    pendingSingle = StartCoroutine(FireSingleAfterDelay());
                }
            }

            private IEnumerator FireSingleAfterDelay()
            {
                yield return new WaitForSecondsRealtime(doubleClickDelay);
                OnSingle?.Invoke();
                pendingSingle = null;
            }
        }

        public static class RecruitOverlayManager
        {
            static GameObject overlayPanel;
            public static bool isInitialized;
            internal static OverlayAnimator _overlayAnimator;
            internal static bool _isOverlayVisible;

            internal static int _lastLayoutScreenWidth;

            internal static OverlayAnimator _detailAnimator;
            internal static bool _isDetailVisible;
            internal static GameObject _currentSelectedCard;
            internal static RecruitAtSite _selectedRecruit;

          //  internal static Sprite _mutationBound;
          //  internal static Sprite _iconBackground;
            internal static Sprite _abilityIconBackground;
            internal static UIInventorySlot _mutationSlotTemplate;

            internal static void ResetState()
            {
                try
                {
                    ClearSelection(immediate: true);

                    if (_detailPanel != null)
                    {
                        Object.Destroy(_detailPanel);
                        _detailPanel = null;
                    }

                    if (overlayPanel != null)
                    {
                        Object.Destroy(overlayPanel);
                        overlayPanel = null;
                        HavenRecruitAbilityTooltipTrigger.ResetCache();
                    }
                    OverlayAbilityTooltip = null;
                    OverlayRootRect = null;
                    OverlayCanvas = null;
                    _overlayAnimator = null;
                    _detailAnimator = null;
                    _isOverlayVisible = false;
                    _isDetailVisible = false;
                    _currentSelectedCard = null;
                    _detailClassIconImage = null;
                    _detailLevelLabel = null;
                    _selectedRecruit = null;
                    _detailEmptyState = null;
                    _detailInfoRoot = null;
                    _detailNameLabel = null;
                    _detailFactionIconImage = null;
                    _detailHavenLabel = null;
                    // _detailAbilityDescriptionGroup = null;
                    // _detailAbilityDescriptionRoot = null;
                    // _detailMutationGroup = null;
                    _detailMutationRoot = null;
                    //  _detailCostGroup = null;
                    _detailCostRoot = null;
                    _detailFactionLogoImage = null;
                    if (_mutationSlotTemplate != null)
                    {
                        Object.Destroy(_mutationSlotTemplate.gameObject);
                        _mutationSlotTemplate = null;
                    }
                    _lastLayoutScreenWidth = 0;
                    isInitialized = false;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }


            public static void ToggleOverlay()
            {
                try
                {
                    if (!isInitialized)
                    {
                        CreateOverlay();
                        isInitialized = true;
                    }

                   /* if (_mutationBound == null)
                    {
                        _mutationBound = Helper.CreateSpriteFromImageFile("UI_Frame_Mutationbound.png");
                    }
                    if (_iconBackground == null)
                    {
                        _iconBackground = Helper.CreateSpriteFromImageFile("UI_Frame_Feathered.png");
                    }*/
                    if (_abilityIconBackground == null)
                    {
                        _abilityIconBackground = Helper.CreateSpriteFromImageFile("UI_ButtonFrame_Main_Sliced.png");
                    }


                    bool show = !_isOverlayVisible;

                    EnsureOverlayLayout(force: true);

                    HavenRecruitsOverlayObjectivesHider.SetObjectivesHiddenForRecruitsOverlay(GameUtl.CurrentLevel().GetComponent<GeoLevelController>()?.View, show);


                    if (!show)
                    {
                        ClearSelection(immediate: false);
                    }



                    if (show && !overlayPanel.activeSelf)
                    {
                        overlayPanel.SetActive(true);
                    }

                    if (show)
                    {
                        ApplySortMode(SortMode.Level, refresh: false);
                        RefreshColumns(); // repopulate each time it opens
                    }

                    _isOverlayVisible = show;

                    if (_overlayAnimator != null)
                    {
                        _overlayAnimator.Play(show, () =>
                        {
                            if (!show)
                            {
                                overlayPanel.SetActive(false);
                            }
                        });
                    }
                    else
                    {
                        if (!show)
                        {
                            overlayPanel.SetActive(false);
                        }
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static Canvas FindGeoscapeCanvas()
            {

                try
                {
                    // Prefer the existing geoscape canvas
                    var geoscapeView = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View;
                    if (geoscapeView != null)
                    {
                        var canvas = geoscapeView.gameObject.GetComponentInChildren<Canvas>(true);
                        if (canvas != null) return canvas;
                    }
                    // Fallback to any overlay canvas
                    var any = Object.FindObjectsOfType<Canvas>().FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
                    if (any) return any;

                    var canvasObj = new GameObject("TFTV_RecruitOverlayCanvas");
                    var cv = canvasObj.AddComponent<Canvas>();
                    cv.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    return cv;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); throw; }
            }


            private static void CreateOverlay()
            {
                try
                {
                    if (PuristaSemibold == null)
                    {
                        PuristaSemibold = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.PhoenixpediaModule.EntryTitle.font;
                       // TFTVLogger.Always($"Font is {PuristaSemibold?.name}");
                    }

                    var canvas = FindGeoscapeCanvas();
                    CreateDetailPanel(canvas);

                    overlayPanel = new GameObject("TFTV_RecruitOverlay");
                    overlayPanel.transform.SetParent(canvas.transform, false);

                    // draw above the HUD
                    var ovCanvas = overlayPanel.AddComponent<Canvas>();
                    ovCanvas.overrideSorting = false;
                    // ovCanvas.sortingOrder = 5000;
                    OverlayCanvas = ovCanvas;
                    overlayPanel.AddComponent<GraphicRaycaster>();

                    var panelImage = overlayPanel.AddComponent<Image>();
                    panelImage.color = new Color(0f, 0f, 0f, 0.95f);

                    var panelOutline = overlayPanel.AddComponent<Outline>();
                    panelOutline.effectColor = HeaderBorderColor;
                    panelOutline.effectDistance = new Vector2(2f, 2f);

                    var rt = overlayPanel.GetComponent<RectTransform>();
                    OverlayRootRect = rt;

                    // RIGHT-ALIGNED, NARROW BAND: width = ColumnsWidthPercent, right padding = RIGHT_MARGIN
                    float overlayWidth = GetOverlayWidthFraction(out float overlayPixels);
                    rt.anchorMin = new Vector2(1f - OverlayRightMargin - overlayWidth, OverlayBottomMargin);
                    rt.anchorMax = new Vector2(1f - OverlayRightMargin, 1f - OverlayTopMargin);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    GeoLevelController geoLevel = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    CreateHeader(overlayPanel.transform);
                    CreateToolbar(overlayPanel.transform);
                    CreateFactionTabs(overlayPanel.transform);
                    CreateRecruitListArea(overlayPanel.transform);
                    EnsureAbilityTooltipInstance(overlayPanel.transform);
                    EnsureItemTooltipInstance(overlayPanel.transform);
                    EnsureMutationSlotTemplate(overlayPanel.transform);
                    // Double-click on a card = send the closest Phoenix aircraft to that recruit's site
                    OnCardDoubleClick = (recruit, site) =>
                    {
                        try { HavenRecruitsGeoscapeInteractions.SendClosestAircraftToSite(site); } catch (Exception ex) { TFTVLogger.Error(ex); }
                    };

                    _overlayAnimator = overlayPanel.AddComponent<OverlayAnimator>();
                    _overlayAnimator.Initialize(rt, resolvedWidth: overlayPixels);
                    overlayPanel.AddComponent<ScreenSizeWatcher>();
                    _isOverlayVisible = false;


                    overlayPanel.SetActive(false);
                    EnsureOverlayLayout(force: true);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
            internal static GeoRosterAbilityDetailTooltip EnsureOverlayTooltip()
            {
                try
                {
                    if (OverlayAbilityTooltip == null)
                    {
                        Transform parent = overlayPanel != null ? overlayPanel.transform : null;
                        if (parent == null && OverlayCanvas != null)
                        {
                            parent = OverlayCanvas.transform;
                        }

                        EnsureAbilityTooltipInstance(parent);
                    }

                    return OverlayAbilityTooltip;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static void EnsureAbilityTooltipInstance(Transform overlayTransform)
            {
                try
                {
                    if (overlayTransform == null)
                    {
                        return;
                    }

                    if (OverlayAbilityTooltip != null)
                    {
                        if (OverlayAbilityTooltip.transform.parent != overlayTransform)
                        {
                            OverlayAbilityTooltip.transform.SetParent(overlayTransform, false);
                        }
                        return;
                    }

                    var template = FindTooltipTemplate();
                    if (template == null)
                    {
                        TFTVLogger.Always("[RecruitsOverlay] Could not locate GeoRosterAbilityDetailTooltip template.");
                        return;
                    }

                    var cloneGO = Object.Instantiate(template.gameObject, overlayTransform, worldPositionStays: false);
                    cloneGO.transform.localScale = Vector3.one * 0.5f;
                    cloneGO.name = "TFTV_RecruitAbilityTooltip";
                    cloneGO.SetActive(false);
                    OverlayAbilityTooltip = cloneGO.GetComponent<GeoRosterAbilityDetailTooltip>();
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
            internal static UIInventoryTooltip EnsureOverlayItemTooltip()
            {
                try
                {
                    if (OverlayItemTooltip == null)
                    {
                        Transform parent = overlayPanel != null ? overlayPanel.transform : null;
                        if (parent == null && OverlayCanvas != null)
                        {
                            parent = OverlayCanvas.transform;
                        }

                        EnsureItemTooltipInstance(parent);
                    }

                    return OverlayItemTooltip;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static void EnsureItemTooltipInstance(Transform overlayTransform)
            {
                try
                {
                    if (overlayTransform == null)
                    {
                        return;
                    }

                    if (OverlayItemTooltip != null)
                    {
                        if (OverlayItemTooltip.transform.parent != overlayTransform)
                        {
                            OverlayItemTooltip.transform.SetParent(overlayTransform, false);
                        }

                        return;
                    }

                    var template = FindItemTooltipTemplate();
                    if (template == null)
                    {
                        TFTVLogger.Always("[RecruitsOverlay] Could not locate UIInventoryTooltip template.");
                        return;
                    }

                    var cloneGO = Object.Instantiate(template.gameObject, overlayTransform, worldPositionStays: false);
                    cloneGO.name = "TFTV_RecruitItemTooltip";
                    cloneGO.SetActive(false);
                    OverlayItemTooltip = cloneGO.GetComponent<UIInventoryTooltip>();
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
            internal static UIInventorySlot EnsureMutationSlotTemplate(Transform overlayTransform)
            {
                try
                {
                    if (overlayTransform == null)
                    {
                        overlayTransform = overlayPanel?.transform;
                    }

                    if (_mutationSlotTemplate != null)
                    {
                        if (overlayTransform != null && _mutationSlotTemplate.transform.parent != overlayTransform)
                        {
                            _mutationSlotTemplate.transform.SetParent(overlayTransform, false);
                        }

                        return _mutationSlotTemplate;
                    }

                    var template = FindInventorySlotTemplate();
                    if (template == null)
                    {
                        TFTVLogger.Always("[RecruitsOverlay] Could not locate UIInventorySlot template.");
                        return null;
                    }

                    Transform parent = overlayTransform ?? OverlayCanvas?.transform ?? template.transform.parent;
                    var cloneGO = Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                    cloneGO.name = "TFTV_RecruitMutationSlotTemplate";
                    cloneGO.SetActive(false);

                    _mutationSlotTemplate = cloneGO.GetComponent<UIInventorySlot>();
                    if (_mutationSlotTemplate == null)
                    {
                        Object.Destroy(cloneGO);
                        return null;
                    }

                    PrepareMutationSlotTemplate(_mutationSlotTemplate);
                    return _mutationSlotTemplate;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static void PrepareMutationSlotTemplate(UIInventorySlot template)
            {
                if (template == null)
                {
                    return;
                }

                try
                {
                    template.gameObject.name = "TFTV_RecruitMutationSlotTemplate";
                    template.Item = null;

                    var enterHandlers = template.OnPointerEnteredHandlers;
                    if (enterHandlers != null)
                    {
                        var clear = enterHandlers.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                        clear?.Invoke(enterHandlers, null);
                    }

                    var exitHandlers = template.OnPointerExitedHandlers;
                    if (exitHandlers != null)
                    {
                        var clear = exitHandlers.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                        clear?.Invoke(exitHandlers, null);
                    }

                    foreach (var selectable in template.GetComponentsInChildren<Selectable>(true))
                    {
                        if (selectable != null)
                        {
                            selectable.interactable = false;
                        }
                    }

                    foreach (var behaviour in template.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (behaviour == null || behaviour == template)
                        {
                            continue;
                        }

                        if (behaviour is IBeginDragHandler || behaviour is IDragHandler || behaviour is IEndDragHandler || behaviour is IDropHandler)
                        {
                            behaviour.enabled = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            private static UIInventorySlot FindInventorySlotTemplate()
            {
                try
                {
                    UIInventorySlot template = null;

                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var view = geoLevel?.View;
                    if (view != null)
                    {
                        template = view.GetComponentsInChildren<UIInventorySlot>(true)
                            .FirstOrDefault(t => t != null && !IsOurMutationTemplate(t) && t.hideFlags == HideFlags.None);
                    }

                    if (template == null)
                    {
                        template = Resources.FindObjectsOfTypeAll<UIInventorySlot>()
                            .FirstOrDefault(t => t != null && !IsOurMutationTemplate(t) && t.hideFlags == HideFlags.None);
                    }

                    if (template == null)
                    {
                        template = Object.FindObjectsOfType<UIInventorySlot>()
                            .FirstOrDefault(t => t != null && !IsOurMutationTemplate(t) && t.hideFlags == HideFlags.None);
                    }

                    return template;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static bool IsOurMutationTemplate(UIInventorySlot slot)
            {
                if (slot == null)
                {
                    return false;
                }

                if (_mutationSlotTemplate != null && slot == _mutationSlotTemplate)
                {
                    return true;
                }

                var go = slot.gameObject;
                if (go == null)
                {
                    return false;
                }

                return go.name.StartsWith("TFTV_RecruitMutationSlotTemplate", StringComparison.Ordinal);
            }
            private static GeoRosterAbilityDetailTooltip FindTooltipTemplate()
            {
                try
                {
                    GeoRosterAbilityDetailTooltip template = null;

                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var view = geoLevel?.View;
                    if (view != null)
                    {
                        template = view.GetComponentsInChildren<GeoRosterAbilityDetailTooltip>(true)
                            .FirstOrDefault(t => t != null && t != OverlayAbilityTooltip);
                    }

                    if (template == null)
                    {
                        template = Resources.FindObjectsOfTypeAll<GeoRosterAbilityDetailTooltip>()
                            .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None && t != OverlayAbilityTooltip);
                    }

                    if (template == null)
                    {
                        template = Object.FindObjectsOfType<GeoRosterAbilityDetailTooltip>()
                            .FirstOrDefault(t => t != null && t != OverlayAbilityTooltip);
                    }

                    return template;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static UIInventoryTooltip FindItemTooltipTemplate()
            {
                try
                {
                    UIInventoryTooltip template = null;

                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var view = geoLevel?.View;
                    if (view != null)
                    {
                        template = view.GetComponentsInChildren<UIInventoryTooltip>(true)
                            .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None && t != OverlayItemTooltip);
                    }

                    if (template == null)
                    {
                        template = Resources.FindObjectsOfTypeAll<UIInventoryTooltip>()
                            .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None && t != OverlayItemTooltip);
                    }

                    if (template == null)
                    {
                        template = Object.FindObjectsOfType<UIInventoryTooltip>()
                            .FirstOrDefault(t => t != null && t.hideFlags == HideFlags.None && t != OverlayItemTooltip);
                    }

                    return template;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }
            internal static void EnsureOverlayLayout(bool force = false)
            {
                try
                {
                    int screenWidth = Mathf.Max(1, Screen.width);
                    if (!force && screenWidth == _lastLayoutScreenWidth)
                    {
                        return;
                    }

                    float widthFraction = GetOverlayWidthFraction(out float pixelWidth);

                    if (overlayPanel != null)
                    {
                        var overlayRect = overlayPanel.GetComponent<RectTransform>();
                        if (overlayRect != null)
                        {
                            overlayRect.anchorMin = new Vector2(1f - OverlayRightMargin - widthFraction, OverlayBottomMargin);
                            overlayRect.anchorMax = new Vector2(1f - OverlayRightMargin, 1f - OverlayTopMargin);
                            overlayRect.pivot = new Vector2(1f, 0.5f);
                            overlayRect.offsetMin = Vector2.zero;
                            overlayRect.offsetMax = Vector2.zero;
                        }

                        _overlayAnimator?.SetResolvedWidth(pixelWidth);
                    }

                    if (_detailPanel != null)
                    {
                        var detailRect = _detailPanel.GetComponent<RectTransform>();
                        if (detailRect != null)
                        {
                            detailRect.anchorMin = new Vector2(OverlayLeftMargin, OverlayBottomMargin);
                            detailRect.anchorMax = new Vector2(OverlayLeftMargin + widthFraction, 1f - OverlayTopMargin);
                            detailRect.pivot = new Vector2(0f, 0.5f);
                            detailRect.offsetMin = Vector2.zero;
                            detailRect.offsetMax = Vector2.zero;
                        }

                        _detailAnimator?.SetResolvedWidth(pixelWidth);
                    }

                    _lastLayoutScreenWidth = screenWidth;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void OnScreenSizeChanged()
            {
                EnsureOverlayLayout(force: true);
            }



            internal sealed class ScreenSizeWatcher : MonoBehaviour
            {
                private int _lastWidth;
                private int _lastHeight;

                private void OnEnable()
                {
                    _lastWidth = Screen.width;
                    _lastHeight = Screen.height;
                }

                private void Update()
                {
                    if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                    {
                        _lastWidth = Screen.width;
                        _lastHeight = Screen.height;
                        OnScreenSizeChanged();
                    }
                }
            }
            internal static void HandleRecruitSelected(GameObject card, RecruitAtSite data)
            {
                try
                {
                    if (card == null || data == null)
                    {
                        return;
                    }

                    if (_currentSelectedCard != null && _currentSelectedCard != card)
                    {
                        SetCardSelected(_currentSelectedCard, false);
                    }

                    _currentSelectedCard = card;
                    SetCardSelected(card, true);
                    HavenRecruitsDetailsPanel.ShowRecruitDetails(data);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void SetCardSelected(GameObject card, bool selected)
            {
                if (card == null)
                {
                    return;
                }

                var image = card.GetComponent<Image>();
                if (image != null)
                {
                    image.color = selected ? CardSelectedColor : CardBackgroundColor;
                }

                var outline = card.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected ? CardSelectedBorderColor : CardBorderColor;
                }


                var view = card.GetComponent<RecruitCardView>();
                view?.SetSelected(selected);
            }

            private static void ClearSelection(bool immediate = true)
            {
                try
                {
                    if (_currentSelectedCard != null)
                    {
                        SetCardSelected(_currentSelectedCard, false);
                        _currentSelectedCard = null;
                    }

                    _selectedRecruit = null;
                    HavenRecruitsDetailsPanel.HideRecruitDetails(immediate);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }


            private static void RefreshColumns()
            {
                try
                {
                    _siteTravelTimeCache.Clear();

                    var geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    if (!geoLevelController) return;

                    if (_recruitListRoot == null)
                    {
                        return;
                    }

                    ClearSelection();

                    foreach (Transform child in _recruitListRoot)
                    {
                        Object.Destroy(child.gameObject);
                    }

                    var factionRecruits = new Dictionary<FactionFilter, List<RecruitAtSite>>
                    {  { FactionFilter.Anu, geoLevelController.AnuFaction != null ? HavenRecruitsUtils.GetRecruitsForFaction(geoLevelController.AnuFaction) : new List<RecruitAtSite>() },
                        { FactionFilter.NewJericho, geoLevelController.NewJerichoFaction != null ? HavenRecruitsUtils.GetRecruitsForFaction(geoLevelController.NewJerichoFaction) : new List<RecruitAtSite>() },
                        { FactionFilter.Synedrion, geoLevelController.SynedrionFaction != null ? HavenRecruitsUtils.GetRecruitsForFaction(geoLevelController.SynedrionFaction) : new List<RecruitAtSite>() }
                    };
                    foreach (var kvp in factionRecruits)
                    {
                        if (_factionTabs.TryGetValue(kvp.Key, out var tab) && tab.CountLabel != null)
                        {
                            tab.CountLabel.text = kvp.Value.Count.ToString();
                        }

                    }

                    int totalRecruits = factionRecruits.Sum(kvp => kvp.Value.Count);

                    if (!factionRecruits.TryGetValue(_activeFactionFilter, out var recruits))
                    {
                        recruits = new List<RecruitAtSite>();

                    }


                    if (_totalRecruitsLabel != null)
                    {
                        _totalRecruitsLabel.text = totalRecruits.ToString();
                    }

                    UpdateFactionTabVisuals();

                    if (recruits.Count == 0)
                    {
                        HavenRecruitsRecruitItem.CreateEmptyLabel(_recruitListRoot, "No recruits discovered.");
                        return;
                    }

                    HavenRecruitsUtils.SortRecruits(recruits);

                    bool collapse = recruits.Count > 4;   // show compact by default if many
                    foreach (var r in recruits)
                    {
                        HavenRecruitsRecruitItem.CreateRecruitItem(_recruitListRoot, r, collapse);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            internal static void CreateToolbar(Transform overlayRoot)
            {
                var (bar, rt) = RecruitOverlayManagerHelpers.NewUI("Toolbar", overlayRoot);
                rt.anchorMin = new Vector2(0f, 0.88f);
                rt.anchorMax = new Vector2(1f, 0.94f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var h = bar.AddComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleLeft;
                h.spacing = 16;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
                h.padding = new RectOffset(32, 0, 0, 0);

                // "Sort:" label
                var (lblGO, _) = RecruitOverlayManagerHelpers.NewUI("SortLabel", bar.transform);
                var lbl = lblGO.AddComponent<Text>();
                lbl.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                lbl.fontSize = TextFontSize;
                lbl.color = Color.white;
                lbl.text = "Sort:";
                lbl.alignment = TextAnchor.MiddleLeft;
                var lblLE = lblGO.AddComponent<LayoutElement>();
                lblLE.flexibleWidth = 0;

                // ToggleGroup to enforce single selection
                _sortGroup = bar.AddComponent<ToggleGroup>();
                _sortGroup.allowSwitchOff = false;

                _sortToggles.Clear();

                // Create the 3 checkboxes
                AddSortToggle(bar.transform, "Level", SortMode.Level, isOn: true);
                AddSortToggle(bar.transform, "Class", SortMode.Class);
                AddSortToggle(bar.transform, "Closest to Phoenix Aircraft", SortMode.Distance);

                ApplySortMode(SortMode.Level, refresh: false);

            }
            internal static void CreateFactionTabs(Transform overlayRoot)
            {
                var (tabsRoot, rt) = RecruitOverlayManagerHelpers.NewUI("FactionTabs", overlayRoot);
                rt.anchorMin = new Vector2(0f, 0.82f);
                rt.anchorMax = new Vector2(1f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var layout = tabsRoot.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 12f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(24, 24, 4, 4);

                _factionTabGroup = tabsRoot.AddComponent<ToggleGroup>();
                _factionTabGroup.allowSwitchOff = false;

                CreateFactionTab(tabsRoot.transform, FactionFilter.Anu);
                CreateFactionTab(tabsRoot.transform, FactionFilter.NewJericho);
                CreateFactionTab(tabsRoot.transform, FactionFilter.Synedrion);

                UpdateFactionTabVisuals();
            }

            internal static void CreateFactionTab(Transform parent, FactionFilter filter)
            {
                var (tabGO, tabRT) = RecruitOverlayManagerHelpers.NewUI($"FactionTab_{filter}", parent);
                tabRT.anchorMin = new Vector2(0f, 0f);
                tabRT.anchorMax = new Vector2(1f, 1f);
                tabRT.offsetMin = Vector2.zero;
                tabRT.offsetMax = Vector2.zero;

                var layoutElement = tabGO.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;
                layoutElement.preferredHeight = ClassIconSize + 8f;
                layoutElement.minHeight = layoutElement.preferredHeight;

                var background = tabGO.AddComponent<Image>();
                background.color = TabDefaultColor;

                var outline = tabGO.AddComponent<Outline>();
                outline.effectColor = TabBorderColor;
                outline.effectDistance = new Vector2(1f, 1f);

                var toggle = tabGO.AddComponent<Toggle>();
                toggle.group = _factionTabGroup;
                toggle.transition = Selectable.Transition.None;
                toggle.targetGraphic = background;

                bool isActive = filter == _activeFactionFilter;
                toggle.SetIsOnWithoutNotify(isActive);

                var (contentGO, contentRT) = RecruitOverlayManagerHelpers.NewUI("Content", tabGO.transform);
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.offsetMin = new Vector2(6f, 6f);
                contentRT.offsetMax = new Vector2(-6f, -6f);

                var contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.spacing = 6f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;

                var iconSprite = RecruitOverlayManagerHelpers.GetFactionIcon(filter);
                var (iconGO, _) = RecruitOverlayManagerHelpers.NewUI("Icon", contentGO.transform);
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.enabled = iconSprite != null;
                iconImage.color = iconSprite != null ? GetFactionColor(filter) : Color.white;
                var iconLE = iconGO.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 34f;
                iconLE.preferredHeight = 34f;

                var (countGO, _) = RecruitOverlayManagerHelpers.NewUI("Count", contentGO.transform);
                var count = countGO.AddComponent<Text>();
                count.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                count.fontSize = TextFontSize + 4;
                count.color = Color.white;
                count.alignment = TextAnchor.MiddleCenter;
                count.text = "0";

                _factionTabs[filter] = new FactionTabUI
                {
                    Toggle = toggle,
                    Background = background,
                    Icon = iconImage,
                    CountLabel = count
                };

                toggle.onValueChanged.AddListener(on =>
                {
                    if (!on)
                    {
                        return;
                    }

                    _activeFactionFilter = filter;
                    UpdateFactionTabVisuals();
                    RefreshColumns();
                });
            }

            internal static Color GetFactionColor(FactionFilter filter)
            {
                try
                {
                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    if (geoLevel == null)
                    {
                        return Color.white;
                    }

                    GeoFaction faction = null;
                    switch (filter)
                    {
                        case FactionFilter.Anu:
                            faction = geoLevel.AnuFaction;
                            break;
                        case FactionFilter.NewJericho:
                            faction = geoLevel.NewJerichoFaction;
                            break;
                        case FactionFilter.Synedrion:
                            faction = geoLevel.SynedrionFaction;
                            break;
                    }

                    var color = faction.Def.FactionColor;
                    if (color.a <= 0f)
                    {
                        color.a = 1f;
                    }

                    return color;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return Color.white;
                }
            }

            internal static void UpdateFactionTabVisuals()
            {
                foreach (var kvp in _factionTabs)
                {
                    bool isActive = kvp.Key == _activeFactionFilter && kvp.Value.Toggle != null && kvp.Value.Toggle.isOn;
                    if (kvp.Value.Background != null)
                    {
                        kvp.Value.Background.color = isActive ? TabHighlightColor : TabDefaultColor;
                    }
                    if (kvp.Value.Icon != null)
                    {
                        kvp.Value.Icon.color = isActive ? Color.black : GetFactionColor(kvp.Key);
                    }
                    if (kvp.Value.CountLabel != null)
                    {
                        kvp.Value.CountLabel.color = isActive ? Color.black : Color.white;
                    }
                }
            }

            internal static void CreateHeader(Transform overlayRoot)
            {
                var (header, rt) = RecruitOverlayManagerHelpers.NewUI("Header", overlayRoot);
                rt.anchorMin = new Vector2(0f, 0.94f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var background = header.AddComponent<Image>();
                background.color = HeaderBackgroundColor;

                var outline = header.AddComponent<Outline>();
                outline.effectColor = HeaderBorderColor;
                outline.effectDistance = new Vector2(2f, 2f);

                var layout = header.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(24, 24, 6, 6);

                var (titleGO, _) = RecruitOverlayManagerHelpers.NewUI("Title", header.transform);
                var title = titleGO.AddComponent<Text>();
                title.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                title.fontSize = TextFontSize + 2;
                title.color = Color.white;
                title.alignment = TextAnchor.MiddleLeft;
                title.text = "RECRUITS";

                var (spacer, _) = RecruitOverlayManagerHelpers.NewUI("Spacer", header.transform);
                var spacerElement = spacer.AddComponent<LayoutElement>();
                spacerElement.flexibleWidth = 1f;

                var (countGO, _) = RecruitOverlayManagerHelpers.NewUI("TotalCount", header.transform);
                var count = countGO.AddComponent<Text>();
                count.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                count.fontSize = TextFontSize + 2;
                count.color = Color.white;
                count.alignment = TextAnchor.MiddleRight;
                count.text = "0";

                _totalRecruitsLabel = count;
            }

            internal static void CreateRecruitListArea(Transform overlayRoot)
            {
                var listGO = new GameObject("RecruitList");
                listGO.transform.SetParent(overlayRoot, false);

                var listRT = listGO.AddComponent<RectTransform>();
                listRT.anchorMin = new Vector2(0f, 0.01f);
                listRT.anchorMax = new Vector2(1f, 0.82f);
                listRT.offsetMin = Vector2.zero;
                listRT.offsetMax = Vector2.zero;

                var scrollRect = listGO.AddComponent<ScrollRect>();
                var mask = listGO.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var bg = listGO.AddComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.05f);

                var (contentGO, contentRT) = RecruitOverlayManagerHelpers.NewUI("Content", listGO.transform);
                contentRT.anchorMin = new Vector2(0f, 1f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.pivot = new Vector2(0.5f, 1f);
                contentRT.offsetMin = new Vector2(8f, 8f);
                contentRT.offsetMax = new Vector2(-8f, -8f);

                var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = ItemSpacing;
                vlg.childAlignment = TextAnchor.UpperCenter;

                var fitter = contentGO.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.content = contentRT;
                scrollRect.vertical = true;
                scrollRect.horizontal = false;

                _recruitListRoot = contentGO.transform;
            }
            internal static void ApplySortMode(SortMode mode, bool refresh)
            {
                var previousMode = _sortMode;
                _sortMode = mode;

                foreach (var kvp in _sortToggles)
                {
                    var toggle = kvp.Value;
                    if (toggle == null)
                    {
                        continue;
                    }

                    bool shouldBeOn = kvp.Key == mode;
                    toggle.SetIsOnWithoutNotify(shouldBeOn);
                }

                if (previousMode == SortMode.Distance && mode != SortMode.Distance)
                {
                    _siteTravelTimeCache.Clear();
                }

                if (refresh)
                {
                    RefreshColumns();
                }
            }
            internal static void AddSortToggle(Transform parent, string labelText, SortMode mode, bool isOn = false)
            {
                // Container (so the toggle box and label sit side-by-side)
                var (root, _) = RecruitOverlayManagerHelpers.NewUI($"Sort_{mode}", parent);
                var row = root.AddComponent<HorizontalLayoutGroup>();
                row.childAlignment = TextAnchor.MiddleLeft;
                row.spacing = 6;
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;

                var le = root.AddComponent<LayoutElement>();
                le.minHeight = 24;

                // Toggle component on the container
                var toggle = root.AddComponent<Toggle>();
                toggle.group = _sortGroup;

                // Checkbox "box"
                var (boxGO, boxRT) = RecruitOverlayManagerHelpers.NewUI("Box", root.transform);
                var boxImg = boxGO.AddComponent<Image>();
                boxImg.color = new Color(1, 1, 1, 0.25f);
                var boxLE = boxGO.AddComponent<LayoutElement>();
                boxLE.preferredWidth = 18; boxLE.preferredHeight = 18;
                boxRT.sizeDelta = new Vector2(18, 18);

                // Checkmark inside the box
                var (checkGO, checkRT) = RecruitOverlayManagerHelpers.NewUI("Checkmark", boxGO.transform);
                var checkImg = checkGO.AddComponent<Image>();
                checkImg.color = TabHighlightColor;
                checkRT.anchorMin = new Vector2(0, 0);
                checkRT.anchorMax = new Vector2(1, 1);
                checkRT.offsetMin = new Vector2(3, 3);
                checkRT.offsetMax = new Vector2(-3, -3);

                // Label
                var (textGO, _) = RecruitOverlayManagerHelpers.NewUI("Label", root.transform);
                var t = textGO.AddComponent<Text>();
                t.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.fontSize = TextFontSize;
                t.color = Color.white;
                t.alignment = TextAnchor.MiddleLeft;
                t.text = labelText;

                // Wire the toggle graphics
                toggle.targetGraphic = boxImg;  // background tints on hover if desired
                toggle.graphic = checkImg;      // this is shown/hidden by Toggle

                // Initial state
                toggle.isOn = isOn;
                if (isOn) _sortMode = mode;

                // When this toggle turns on, set sort mode and refresh
                toggle.onValueChanged.AddListener(on =>
                {
                    if (!on) return;
                    _sortMode = mode;
                    RefreshColumns();
                });

                // Track it (optional, handy if you ever need to flip programmatically)
                _sortToggles[mode] = toggle;
            }




            // Clear this at the start of a refresh (e.g., in RefreshColumns()).
            internal static readonly Dictionary<GeoSite, float> _siteTravelTimeCache = new Dictionary<GeoSite, float>();

            internal static float GetDistanceScore(GeoSite site)
            {
                try
                {
                    if (site == null || site.GeoLevel == null) return float.PositiveInfinity;

                    if (_siteTravelTimeCache.TryGetValue(site, out var cached))
                        return cached;

                    var pf = site.GeoLevel.PhoenixFaction;
                    if (pf == null || pf.Vehicles == null)
                        return float.PositiveInfinity;

                    float bestTravelTime = float.PositiveInfinity;
                    var targetPos = site.WorldPosition;

                    foreach (GeoVehicle vehicle in pf.Vehicles)
                    {
                        if (vehicle == null || vehicle.Navigation == null) continue;

                        var fromPos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;

                        bool hasPath;
                        var path = vehicle.Navigation.FindPath(fromPos, targetPos, out hasPath);
                        if (!hasPath || path == null || path.Count < 2) continue;

                        // Sum geoscape distances along the path
                        double totalDist = 0.0;
                        for (int i = 0; i < path.Count - 1; i++)
                        {
                            totalDist += GeoMap.Distance(path[i].Pos.WorldPosition, path[i + 1].Pos.WorldPosition).Value;
                        }

                        var speed = vehicle.Stats?.Speed.Value ?? 0f;
                        if (speed <= 0f) continue;

                        float travelTime = (float)(totalDist / speed);
                        if (travelTime < bestTravelTime)
                            bestTravelTime = travelTime;
                    }

                    // Cache (even if +∞) so repeated calls are cheap during this refresh.
                    _siteTravelTimeCache[site] = bestTravelTime;
                    return bestTravelTime;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return float.PositiveInfinity;
                }
            }
        }

    }
}

