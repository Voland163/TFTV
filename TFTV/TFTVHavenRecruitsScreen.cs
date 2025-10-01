using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TFTV
{
    internal class HavenRecruitButton
    {
        [HarmonyPatch(typeof(UIModuleSiteManagement), "Awake")]
        public static class AddRecruitsButton_OnSiteManagementAwake
        {
            private const string RecruitsBtnName = "UIButton_Icon_Recruits";
            private const float LeftPaddingPx = 16f; // space between new & Bases buttons

            public static void Postfix(UIModuleSiteManagement __instance)
            {
                try
                {
                    var basesBtn = __instance?.OpenModuleButton;
                    if (basesBtn == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] OpenModuleButton is null; abort.");
                        return;
                    }

                    var parent = basesBtn.transform.parent as RectTransform;
                    if (parent == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Bases button parent is not a RectTransform; abort.");
                        return;
                    }

                    // Avoid duplicates if Awake runs more than once.
                    if (parent.Find(RecruitsBtnName) != null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Recruits button already present; skipping.");
                        return;
                    }

                    // Clone the square button 1:1
                    var templateGO = basesBtn.gameObject;
                    var cloneGO = UnityEngine.Object.Instantiate(templateGO, parent, worldPositionStays: false);
                    cloneGO.name = RecruitsBtnName;
                    cloneGO.SetActive(true);

                    // Match rect and offset to the LEFT by width + padding
                    var tplRT = templateGO.GetComponent<RectTransform>();
                    var rt = cloneGO.GetComponent<RectTransform>();
                    rt.anchorMin = tplRT.anchorMin;   // (1,0)
                    rt.anchorMax = tplRT.anchorMax;   // (1,0)
                    rt.pivot = tplRT.pivot;       // (1,0)
                    rt.sizeDelta = tplRT.sizeDelta;   // 150x150
                    rt.localScale = tplRT.localScale;
                    rt.anchoredPosition = tplRT.anchoredPosition + new Vector2(-(tplRT.sizeDelta.x + LeftPaddingPx), 0f);

                    // Make sure it’s interactable
                    var cg = cloneGO.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

                    // Set label to RECRUITS (first Text under the button)
                    var label = cloneGO.GetComponentsInChildren<Text>(true).FirstOrDefault();
                    if (label != null)
                    {
                        label.text = "RECRUITS";
                        label.fontSize = 30;
                    }

                    var iconTr = cloneGO.transform.Find("Group/Image_Icon");
                    var iconImg = iconTr ? iconTr.GetComponent<Image>() : null;


                    if (iconImg != null)
                    {
                        var newSprite = Helper.CreateSpriteFromImageFile("Geoscape_UICanvasIcons_Actions_EliteSoldierRecruitment_uinomipmaps.png");
                        if (newSprite != null)
                        {
                            iconImg.sprite = newSprite;
                            iconImg.preserveAspect = true;
                            // iconImg.rectTransform.sizeDelta = new Vector2(0.2f, 0.2f); // tweak as needed                     
                            iconImg.color = Color.white;       // ensure fully visible

                            // optional: if your PNG looks squashed, uncomment:
                            iconImg.SetNativeSize(); // then tweak RectTransform if needed
                            iconImg.rectTransform.Translate(60f, 50f, 0f); // tweak as needed
                        }
                        else
                        {
                            TFTVLogger.Always("[RecruitsBtn] Failed to load sprite 'UI_StatusesIcons_CanBeRecruitedIntoPhoenix-2.png'");
                        }
                    }
                    else
                    {
                        TFTVLogger.Always("[RecruitsBtn] Could not find 'Group/Image_Icon' on clone.");
                    }


                    // Wire up click -> toggle your overlay (use BaseButton to keep stock animations)
                    var pgb = cloneGO.GetComponent<PhoenixGeneralButton>();
                    if (pgb?.BaseButton != null)
                    {
                        pgb.BaseButton.onClick.RemoveAllListeners(); // cloned button shouldn't open Bases
                        pgb.BaseButton.onClick.AddListener(() =>
                        {
                            TFTVLogger.Always("[RecruitsBtn] Clicked RECRUITS button.");
                            TFTVHavenRecruitsScreen.RecruitOverlayManager.ToggleOverlay();
                        });
                    }
                    else
                    {
                        // Fallback (unlikely for this prefab)
                        var uiBtn = cloneGO.GetComponent<Button>();
                        if (uiBtn != null)
                        {
                            uiBtn.onClick.RemoveAllListeners();
                            uiBtn.onClick.AddListener(() =>
                            {
                                TFTVLogger.Always("[RecruitsBtn] Clicked RECRUITS button (fallback).");
                                TFTVHavenRecruitsScreen.RecruitOverlayManager.ToggleOverlay();
                            });
                        }
                    }

                    TFTVLogger.Always("[RecruitsBtn] Recruits button added left of Bases.");
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }
    }



    /// <summary>
    /// Helper methods for controlling the objectives module visibility when the recruits overlay is displayed.
    /// </summary>
    public static class RecruitsOverlayObjectives
    {
        private sealed class ObjectivesVisibilityState
        {
            public bool HeaderActive { get; }

            public bool ContainerActive { get; }

            public bool PrimaryContainerActive { get; }

            public bool SecondaryContainerActive { get; }

            public bool SeparatorActive { get; }

            public bool DiplomacyIconActive { get; }

            public ObjectivesVisibilityState(UIModuleGeoObjectives module)
            {
                HeaderActive = GetActive(module.ObjectivesHeader);
                ContainerActive = GetActive(module.ObjectivesContainer);
                PrimaryContainerActive = GetActive(module.PrimaryObjectivesContainer);
                SecondaryContainerActive = GetActive(module.SecondaryObjectivesContainer);
                SeparatorActive = GetActive(module.Separator);
                DiplomacyIconActive = GetActive(module.DiplomacyMissionsIconContainer);
            }
        }

        private static readonly ConditionalWeakTable<UIModuleGeoObjectives, ObjectivesVisibilityState> _visibilityStates = new ConditionalWeakTable<UIModuleGeoObjectives, ObjectivesVisibilityState>();

        /// <summary>
        /// Sets the visibility of the objectives module when the recruits overlay is toggled.
        /// </summary>
        /// <param name="view">The geoscape view owning the objectives module.</param>
        /// <param name="hidden">Whether the objectives UI should be hidden.</param>
        public static void SetObjectivesHiddenForRecruitsOverlay(GeoscapeView view, bool hidden)
        {
            if (view == null)
            {
                return;
            }

            SetObjectivesHiddenForRecruitsOverlay(view.GeoscapeModules?.ObjectivesModule, hidden);
        }

        /// <summary>
        /// Sets the visibility of the objectives module when the recruits overlay is toggled.
        /// </summary>
        /// <param name="objectivesModule">The objectives module to toggle.</param>
        /// <param name="hidden">Whether the objectives UI should be hidden.</param>
        public static void SetObjectivesHiddenForRecruitsOverlay(UIModuleGeoObjectives objectivesModule, bool hidden)
        {
            if (objectivesModule == null)
            {
                return;
            }

            if (hidden)
            {
                _ = _visibilityStates.GetValue(objectivesModule, m => new ObjectivesVisibilityState(m));

                SetActive(objectivesModule.ObjectivesHeader, false);
                SetActive(objectivesModule.ObjectivesContainer, false);
                SetActive(objectivesModule.PrimaryObjectivesContainer, false);
                SetActive(objectivesModule.SecondaryObjectivesContainer, false);
                SetActive(objectivesModule.Separator, false);
                SetActive(objectivesModule.DiplomacyMissionsIconContainer, false);
            }
            else
            {
                if (_visibilityStates.TryGetValue(objectivesModule, out ObjectivesVisibilityState state))
                {
                    SetActive(objectivesModule.ObjectivesHeader, state.HeaderActive);
                    SetActive(objectivesModule.ObjectivesContainer, state.ContainerActive);
                    SetActive(objectivesModule.PrimaryObjectivesContainer, state.PrimaryContainerActive);
                    SetActive(objectivesModule.SecondaryObjectivesContainer, state.SecondaryContainerActive);
                    SetActive(objectivesModule.Separator, state.SeparatorActive);
                    SetActive(objectivesModule.DiplomacyMissionsIconContainer, state.DiplomacyIconActive);
                    _visibilityStates.Remove(objectivesModule);
                }
                else
                {
                    SetActive(objectivesModule.ObjectivesHeader, true);
                    SetActive(objectivesModule.ObjectivesContainer, true);
                    SetActive(objectivesModule.PrimaryObjectivesContainer, true);
                    SetActive(objectivesModule.SecondaryObjectivesContainer, true);
                    SetActive(objectivesModule.Separator, true);
                    SetActive(objectivesModule.DiplomacyMissionsIconContainer, true);
                }
            }

            objectivesModule.NavHolder?.RefreshInteractableList();
        }

        private static bool GetActive(GameObject go)
        {
            return go != null && go.activeSelf;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }


    class TFTVHavenRecruitsScreen
    {

        public static void ClearInternalData()
        {
            try
            {
                _sortGroup = null;
                _sortToggles.Clear();
                RecruitOverlayManager.isInitialized = false;
                _recruitListRoot = null;
                _totalRecruitsLabel = null;
                _factionTabGroup = null;
                _factionTabs.Clear();
                _activeFactionFilter = FactionFilter.Anu;

            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        private static readonly SharedData Shared = TFTVMain.Shared;

        // Spacing / sizing

        // width (as fraction of screen width) used by the 3-column area
        private const float ColumnsWidthPercent = 0.25f;  // 0.60 = 60% of screen, centered


        private const float ColumnPadding = 12f;
        private const float ItemSpacing = 6f;     // space between cards
        private const int RowSpacing = 2;      // space between rows inside a card
        private const int AbilityIconSize = 36;  // abilities
        private const int EquipIconSize = 48;  // equipment & armor (match abilities)
        private const int ArmorIconSize = 48;
        private const int ResourceIconSize = 24;
        private const int TextFontSize = 20;

        private static readonly Color HeaderBackgroundColor = HexToColor("16222a");
        private static readonly Color HeaderBorderColor = HexToColor("222e40");
        private static readonly Color TabBorderColor = HexToColor("55606f");
        private static readonly Color TabHighlightColor = HexToColor("ffb339");
        private static readonly Color TabDefaultColor = HexToColor("1a2733");

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



        private static Font _puristaSemibold = null;

        private enum SortMode { Level, Class, Distance }
        private static SortMode _sortMode = SortMode.Level;

        private static ToggleGroup _sortGroup;
        private static readonly Dictionary<SortMode, Toggle> _sortToggles = new Dictionary<SortMode, Toggle>();

        private static Transform _recruitListRoot;
        private static Text _totalRecruitsLabel;

        private enum FactionFilter
        {
            Anu,
            NewJericho,
            Synedrion
        }

        private sealed class FactionTabUI
        {
            public Toggle Toggle;
            public Image Background;
            public Text CountLabel;
            public Image Icon;
        }

        private static ToggleGroup _factionTabGroup;
        private static readonly Dictionary<FactionFilter, FactionTabUI> _factionTabs = new Dictionary<FactionFilter, FactionTabUI>();
        private static readonly Dictionary<FactionFilter, Sprite> _factionIconCache = new Dictionary<FactionFilter, Sprite>();
        private static FactionFilter _activeFactionFilter = FactionFilter.Anu;


        private sealed class RecruitCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public bool Collapsible;
            public readonly List<GameObject> Rows = new List<GameObject>();

            public void OnPointerEnter(PointerEventData e)
            {
                if (!Collapsible) return;
                foreach (var r in Rows) if (r) r.SetActive(true);
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }

            public void OnPointerExit(PointerEventData e)
            {
                if (!Collapsible) return;
                foreach (var r in Rows) if (r) r.SetActive(false);
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }
        }

        // Hook you can set from outside if you want:
        public static Action<GeoUnitDescriptor, GeoSite> OnCardDoubleClick;



        // Handles single vs double click without firing both
        private sealed class CardClickHandler : MonoBehaviour, IPointerClickHandler
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
            private static OverlayAnimator _overlayAnimator;
            private static bool _isOverlayVisible;
            private const float OverlaySlideDuration = 0.3f;

            public static void ToggleOverlay()
            {
                try
                {
                    if (!isInitialized)
                    {
                        CreateOverlay();
                        isInitialized = true;
                    }

                    bool show = !_isOverlayVisible;

                    RecruitsOverlayObjectives.SetObjectivesHiddenForRecruitsOverlay(GameUtl.CurrentLevel().GetComponent<GeoLevelController>()?.View, show);

                    if (show && !overlayPanel.activeSelf)
                    {
                        overlayPanel.SetActive(true);
                    }

                    if (show)
                    {
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
                    if (_puristaSemibold == null)
                    {
                        _puristaSemibold = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.PhoenixpediaModule.EntryTitle.font;
                        TFTVLogger.Always($"Font is {_puristaSemibold?.name}");
                    }

                    var canvas = FindGeoscapeCanvas();

                    // 0..1 screen-space margins for the overlay band
                    const float TOP_MARGIN = 0.06f;
                    const float BOTTOM_MARGIN = 0.16f;  // was 0.06f  ➜  add ~10% more bottom margin
                    const float RIGHT_MARGIN = 0.0f;

                    overlayPanel = new GameObject("TFTV_RecruitOverlay");
                    overlayPanel.transform.SetParent(canvas.transform, false);

                    // draw above the HUD
                    var ovCanvas = overlayPanel.AddComponent<Canvas>();
                    ovCanvas.overrideSorting = true;
                    ovCanvas.sortingOrder = 5000;
                    overlayPanel.AddComponent<GraphicRaycaster>();

                    var panelImage = overlayPanel.AddComponent<Image>();
                    panelImage.color = new Color(0f, 0f, 0f, 0.95f);

                    var panelOutline = overlayPanel.AddComponent<Outline>();
                    panelOutline.effectColor = HeaderBorderColor;
                    panelOutline.effectDistance = new Vector2(2f, 2f);

                    var rt = overlayPanel.GetComponent<RectTransform>();

                    // RIGHT-ALIGNED, NARROW BAND: width = ColumnsWidthPercent, right padding = RIGHT_MARGIN
                    float w = ColumnsWidthPercent;
                    rt.anchorMin = new Vector2(1f - RIGHT_MARGIN - w, BOTTOM_MARGIN);
                    rt.anchorMax = new Vector2(1f - RIGHT_MARGIN, 1f - TOP_MARGIN);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;



                    GeoLevelController geoLevel = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    CreateHeader(overlayPanel.transform);
                    CreateToolbar(overlayPanel.transform);
                    CreateFactionTabs(overlayPanel.transform);
                    CreateRecruitListArea(overlayPanel.transform);
                    // Double-click on a card = send the closest Phoenix aircraft to that recruit's site
                    OnCardDoubleClick = (recruit, site) =>
                    {
                        try { SendClosestAircraftToSite(site); } catch (Exception ex) { TFTVLogger.Error(ex); }
                    };

                    _overlayAnimator = overlayPanel.AddComponent<OverlayAnimator>();
                    _overlayAnimator.Initialize(rt);
                    _isOverlayVisible = false;


                    overlayPanel.SetActive(false);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
            private sealed class OverlayAnimator : MonoBehaviour
            {
                private RectTransform _rectTransform;
                private Coroutine _animation;

                public bool IsVisible { get; private set; }

                public void Initialize(RectTransform rectTransform)
                {
                    _rectTransform = rectTransform ? rectTransform : GetComponent<RectTransform>();
                    Canvas.ForceUpdateCanvases();
                    HideInstant();
                }

                public void Play(bool show, Action onComplete)
                {
                    EnsureRect();

                    if (_animation != null)
                    {
                        StopCoroutine(_animation);
                        _animation = null;
                    }

                    if (show)
                    {
                        Canvas.ForceUpdateCanvases();
                    }

                    float targetX = show ? 0f : GetHiddenOffset();
                    float startX = _rectTransform.anchoredPosition.x;

                    if (Mathf.Approximately(startX, targetX))
                    {
                        SetPosition(targetX);
                        IsVisible = show;
                        onComplete?.Invoke();
                        return;
                    }

                    _animation = StartCoroutine(SlideRoutine(startX, targetX, show, onComplete));
                }

                private IEnumerator SlideRoutine(float startX, float targetX, bool show, Action onComplete)
                {
                    float elapsed = 0f;

                    while (elapsed < OverlaySlideDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(elapsed / OverlaySlideDuration);
                        float eased = Mathf.SmoothStep(0f, 1f, t);
                        SetPosition(Mathf.Lerp(startX, targetX, eased));
                        yield return null;
                    }

                    SetPosition(targetX);
                    IsVisible = show;
                    _animation = null;
                    onComplete?.Invoke();
                }

                private void HideInstant()
                {
                    SetPosition(GetHiddenOffset());
                    IsVisible = false;
                }

                private float GetHiddenOffset()
                {
                    EnsureRect();

                    float width = _rectTransform.rect.width;

                    RectTransform parent = _rectTransform.parent as RectTransform;
                    if (parent != null)
                    {
                        float anchorWidth = parent.rect.width * (_rectTransform.anchorMax.x - _rectTransform.anchorMin.x);
                        if (anchorWidth > 0f)
                        {
                            width = anchorWidth;
                        }
                    }

                    if (width <= 0f)
                    {
                        width = Screen.width * ColumnsWidthPercent;
                    }

                    return Mathf.Max(0f, width);
                }

                private void SetPosition(float x)
                {
                    Vector2 pos = _rectTransform.anchoredPosition;
                    pos.x = x;
                    _rectTransform.anchoredPosition = pos;
                }

                private void EnsureRect()
                {
                    if (_rectTransform == null)
                    {
                        _rectTransform = GetComponent<RectTransform>();
                    }
                }
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

                    foreach (Transform child in _recruitListRoot)
                    {
                        Object.Destroy(child.gameObject);
                    }

                    var factionRecruits = new Dictionary<FactionFilter, List<RecruitAtSite>>
                    {  { FactionFilter.Anu, geoLevelController.AnuFaction != null ? GetRecruitsForFaction(geoLevelController.AnuFaction) : new List<RecruitAtSite>() },
                        { FactionFilter.NewJericho, geoLevelController.NewJerichoFaction != null ? GetRecruitsForFaction(geoLevelController.NewJerichoFaction) : new List<RecruitAtSite>() },
                        { FactionFilter.Synedrion, geoLevelController.SynedrionFaction != null ? GetRecruitsForFaction(geoLevelController.SynedrionFaction) : new List<RecruitAtSite>() }
                    };
                    foreach (var kvp in factionRecruits)
                    {
                        if (_factionTabs.TryGetValue(kvp.Key, out var tab) && tab.CountLabel != null)
                        {
                            tab.CountLabel.text = $"[{kvp.Value.Count}]";
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
                        CreateEmptyLabel(_recruitListRoot, "No recruits discovered.");
                        return;
                    }

                    SortRecruits(recruits);

                    bool collapse = recruits.Count > 4;   // show compact by default if many
                    foreach (var r in recruits)
                    {
                        CreateRecruitItem(_recruitListRoot, r, collapse);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            private static void CreateToolbar(Transform overlayRoot)
            {
                var (bar, rt) = NewUI("Toolbar", overlayRoot);
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
                var (lblGO, _) = NewUI("SortLabel", bar.transform);
                var lbl = lblGO.AddComponent<Text>();
                lbl.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                lbl.fontSize = TextFontSize;
                lbl.color = Color.white;
                lbl.text = "Sort:";
                lbl.alignment = TextAnchor.MiddleLeft;
                var lblLE = lblGO.AddComponent<LayoutElement>();
                lblLE.flexibleWidth = 0;

                // ToggleGroup to enforce single selection
                _sortGroup = bar.AddComponent<ToggleGroup>();
                _sortGroup.allowSwitchOff = false;

                // Create the 3 checkboxes 
                AddSortToggle(bar.transform, "Level", SortMode.Level, isOn: true);
                AddSortToggle(bar.transform, "Class", SortMode.Class);
                AddSortToggle(bar.transform, "Closest to Phoenix Aircraft", SortMode.Distance);

            }
            private static void CreateFactionTabs(Transform overlayRoot)
            {
                var (tabsRoot, rt) = NewUI("FactionTabs", overlayRoot);
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

            private static void CreateFactionTab(Transform parent, FactionFilter filter)
            {
                var (tabGO, tabRT) = NewUI($"FactionTab_{filter}", parent);
                tabRT.anchorMin = new Vector2(0f, 0f);
                tabRT.anchorMax = new Vector2(1f, 1f);
                tabRT.offsetMin = Vector2.zero;
                tabRT.offsetMax = Vector2.zero;

                var layoutElement = tabGO.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;
                layoutElement.preferredHeight = 56f;

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

                var (contentGO, contentRT) = NewUI("Content", tabGO.transform);
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.offsetMin = new Vector2(8f, 8f);
                contentRT.offsetMax = new Vector2(-8f, -8f);

                var contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.spacing = 6f;
                contentLayout.childControlWidth = false;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;

                var iconSprite = GetFactionIcon(filter);
                var (iconGO, _) = NewUI("Icon", contentGO.transform);
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.enabled = iconSprite != null;
                iconImage.color = iconSprite != null ? GetFactionColor(filter) : Color.white;
                var iconLE = iconGO.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 36f;
                iconLE.preferredHeight = 36f;

                var (countGO, _) = NewUI("Count", contentGO.transform);
                var count = countGO.AddComponent<Text>();
                count.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                count.fontSize = TextFontSize - 2;
                count.color = Color.white;
                count.alignment = TextAnchor.MiddleLeft;
                count.text = "[0]";

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

            private static Color GetFactionColor(FactionFilter filter)
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

            private static void UpdateFactionTabVisuals()
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
                }
            }

            private static Sprite GetFactionIcon(FactionFilter filter)
            {
                if (_factionIconCache.TryGetValue(filter, out var cached) && cached != null)
                {
                    return cached;
                }

                string fileName = null;
                switch (filter)
                {
                    case FactionFilter.Anu:
                        fileName = "FactionIcons_Anu.png";
                        break;
                    case FactionFilter.NewJericho:
                        fileName = "FactionIcons_NewJericho.png";
                        break;
                    case FactionFilter.Synedrion:
                        fileName = "FactionIcons_Synedrion.png";
                        break;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                var sprite = Helper.CreateSpriteFromImageFile(fileName);
                if (sprite != null)
                {
                    _factionIconCache[filter] = sprite;
                }

                return sprite;
            }


            private static void CreateHeader(Transform overlayRoot)
            {
                var (header, rt) = NewUI("Header", overlayRoot);
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
                layout.padding = new RectOffset(24, 24, 12, 12);

                var (titleGO, _) = NewUI("Title", header.transform);
                var title = titleGO.AddComponent<Text>();
                title.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                title.fontSize = TextFontSize + 2;
                title.color = Color.white;
                title.alignment = TextAnchor.MiddleLeft;
                title.text = "RECRUITS";

                var (spacer, _) = NewUI("Spacer", header.transform);
                var spacerElement = spacer.AddComponent<LayoutElement>();
                spacerElement.flexibleWidth = 1f;

                var (countGO, _) = NewUI("TotalCount", header.transform);
                var count = countGO.AddComponent<Text>();
                count.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                count.fontSize = TextFontSize + 2;
                count.color = Color.white;
                count.alignment = TextAnchor.MiddleRight;
                count.text = "0";

                _totalRecruitsLabel = count;
            }

            private static void CreateRecruitListArea(Transform overlayRoot)
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

                var (contentGO, contentRT) = NewUI("Content", listGO.transform);
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

            private static void AddSortToggle(Transform parent, string labelText, SortMode mode, bool isOn = false)
            {
                // Container (so the toggle box and label sit side-by-side)
                var (root, _) = NewUI($"Sort_{mode}", parent);
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
                var (boxGO, boxRT) = NewUI("Box", root.transform);
                var boxImg = boxGO.AddComponent<Image>();
                boxImg.color = new Color(1, 1, 1, 0.25f);
                var boxLE = boxGO.AddComponent<LayoutElement>();
                boxLE.preferredWidth = 18; boxLE.preferredHeight = 18;
                boxRT.sizeDelta = new Vector2(18, 18);

                // Checkmark inside the box
                var (checkGO, checkRT) = NewUI("Checkmark", boxGO.transform);
                var checkImg = checkGO.AddComponent<Image>();
                checkImg.color = Color.white;
                checkRT.anchorMin = new Vector2(0, 0);
                checkRT.anchorMax = new Vector2(1, 1);
                checkRT.offsetMin = new Vector2(3, 3);
                checkRT.offsetMax = new Vector2(-3, -3);

                // Label
                var (textGO, _) = NewUI("Label", root.transform);
                var t = textGO.AddComponent<Text>();
                t.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
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


            private static void SortRecruits(List<RecruitAtSite> list)
            {
                switch (_sortMode)
                {
                    case SortMode.Class:
                        list.Sort((a, b) => string.Compare(GetClassName(a.Recruit), GetClassName(b.Recruit), StringComparison.Ordinal));
                        break;
                    case SortMode.Level:
                        list.Sort((a, b) => b.Recruit.Level.CompareTo(a.Recruit.Level)); // high to low
                        break;
                    case SortMode.Distance:
                        list.Sort((a, b) =>
                        {
                            float ta = GetDistanceScore(a.Site);
                            float tb = GetDistanceScore(b.Site);

                            // Put unreachable (+∞) at the end
                            bool aInf = float.IsPositiveInfinity(ta);
                            bool bInf = float.IsPositiveInfinity(tb);
                            if (aInf && !bInf) return 1;
                            if (!aInf && bInf) return -1;

                            int cmp = ta.CompareTo(tb);
                            if (cmp != 0) return cmp;

                            // Tie-breakers
                            return string.Compare(a.Recruit?.GetName(), b.Recruit?.GetName(), StringComparison.Ordinal);
                        });
                        break;

                }
            }

            // Clear this at the start of a refresh (e.g., in RefreshColumns()).
            private static readonly Dictionary<GeoSite, float> _siteTravelTimeCache = new Dictionary<GeoSite, float>();

            private static float GetDistanceScore(GeoSite site)
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





            // ---------- DATA GATHERING ----------

            private sealed class RecruitAtSite
            {
                public GeoUnitDescriptor Recruit;
                public GeoSite Site;
                public GeoHaven Haven;
                public GeoFaction HavenOwner;
            }

            private static List<RecruitAtSite> GetRecruitsForFaction(GeoFaction faction)
            {
                var list = new List<RecruitAtSite>();
                try
                {
                    if (faction == null)
                    {
                        return list;
                    }


                    GeoPhoenixFaction geoPhoenixFaction = faction.GeoLevel.PhoenixFaction; // player faction wrapper
                                                                                           // All sites with havens, owned by factionDef, revealed to player
                    List<GeoHaven> havens = faction.Havens.Where(s => s != null && s.AvailableRecruit != null && s.Site.GetVisible(geoPhoenixFaction)).ToList();

                    foreach (var haven in havens)
                    {

                        list.Add(new RecruitAtSite
                        {
                            Recruit = haven.AvailableRecruit,
                            Site = haven.Site,
                            Haven = haven,
                            HavenOwner = haven.Site.Owner
                        });
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
                return list.OrderBy(r => r.Recruit?.GetName()).ToList();
            }

            // ---------- UI ITEM BUILD ----------





            private static (GameObject go, RectTransform rt) NewUI(string name, Transform parent = null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                if (parent != null) go.transform.SetParent(parent, false);
                return (go, (RectTransform)go.transform);
            }



            private static Sprite GetClassIcon(GeoUnitDescriptor recruit)
            {
                try
                {
                    // Preferred: class def view icon
                    var ve = recruit?.GetClassViewElementDefs()?.FirstOrDefault();
                    if (ve != null)
                    {
                        if (ve.SmallIcon != null) return ve.SmallIcon;

                    }
                }
                catch { }

                // Fallback: sometimes the ClassTag has a ViewElementDef; try reflection
                try
                {
                    var tag = recruit?.ClassTag;
                    if (tag != null)
                    {
                        var vedProp = tag.GetType().GetProperty("ViewElementDef", BindingFlags.Public | BindingFlags.Instance);
                        var ved = vedProp?.GetValue(tag) as ViewElementDef;
                        if (ved != null)
                        {
                            if (ved.SmallIcon != null) return ved.SmallIcon;
                            if (ved.InventoryIcon != null) return ved.InventoryIcon;
                        }
                    }
                }
                catch { }

                return null; // no icon available; header will just show Level + Name
            }


            private static Image MakeFixedIcon(Transform parent, Sprite sp, int px)
            {
                // Frame with RectTransform + LayoutElement fixes size for layout
                var (frame, frt) = NewUI("IconFrame", parent);
                var le = frame.AddComponent<LayoutElement>();
                le.preferredWidth = px; le.minWidth = px;
                le.preferredHeight = px; le.minHeight = px;
                frt.sizeDelta = new Vector2(px, px);

                // Child image stretched to frame + aspect fit
                var (imgGO, imgRT) = NewUI("Img", frame.transform);
                var img = imgGO.AddComponent<Image>();
                img.sprite = sp;
                img.raycastTarget = false;

                imgRT.anchorMin = Vector2.zero; imgRT.anchorMax = Vector2.one;
                imgRT.offsetMin = Vector2.zero; imgRT.offsetMax = Vector2.zero;

                var arf = imgGO.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                if (sp && sp.rect.height > 0f)
                    arf.aspectRatio = sp.rect.width / sp.rect.height;

                return img;
            }







            private static void CreateRecruitItem(Transform parent, RecruitAtSite data, bool collapse)

            {
                _ = collapse;

                var card = new GameObject($"Recruit_{Safe(data.Recruit?.GetName())}");
                card.transform.SetParent(parent, false);

                // background
                var bg = card.AddComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.08f);


                // button (keep it for hover/tint states, but don't use onClick directly)
                var btn = card.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;

                // click handler: single = focus; double = your hook
                var click = card.AddComponent<CardClickHandler>();
                click.OnSingle = () => FocusOnSite(data.Site);
                click.OnDouble = () => OnCardDoubleClick?.Invoke(data.Recruit, data.Site);

                var layout = card.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 12f;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.padding = new RectOffset(12, 12, 8, 8);

                // Let height fit content (no fixed height anymore)
                var fit = card.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var classIcon = GetClassIcon(data.Recruit);
                if (classIcon != null)
                {
                    MakeFixedIcon(card.transform, classIcon, AbilityIconSize);
                }

                var (levelGO, _) = NewUI("Level", card.transform);
                var levelText = levelGO.AddComponent<Text>();
                levelText.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                levelText.fontSize = TextFontSize;
                levelText.color = Color.white;
                levelText.alignment = TextAnchor.MiddleLeft;
                levelText.text = $"Lv {data.Recruit?.Level ?? 0}";
                var levelLE = levelGO.AddComponent<LayoutElement>();
                levelLE.minWidth = 60f;
                levelLE.preferredWidth = 60f;

                var (nameGO, _) = NewUI("Name", card.transform);
                var nameText = nameGO.AddComponent<Text>();
                nameText.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                nameText.fontSize = TextFontSize;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.MiddleLeft;
                nameText.text = data.Recruit?.GetName() ?? "Unknown Recruit";
                nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                var nameLE = nameGO.AddComponent<LayoutElement>();
                nameLE.flexibleWidth = 1f;
                nameLE.minWidth = 120f;

                var abilityIcons = GetSelectedAbilityIcons(data.Recruit).ToList();
                var mutationIcons = GetMutatedArmorIcons(data.Recruit).ToList();

                if (abilityIcons.Count > 0 || mutationIcons.Count > 0)
                {
                    var (abilitiesGO, _) = NewUI("Abilities", card.transform);
                    var abilitiesLayout = abilitiesGO.AddComponent<HorizontalLayoutGroup>();
                    abilitiesLayout.childAlignment = TextAnchor.MiddleLeft;
                    abilitiesLayout.spacing = 4f;
                    abilitiesLayout.childControlWidth = false;
                    abilitiesLayout.childControlHeight = true;
                    abilitiesLayout.childForceExpandWidth = false;
                    abilitiesLayout.childForceExpandHeight = false;

                    foreach (var icon in abilityIcons)
                    {
                        if (icon == null) continue;
                        MakeFixedIcon(abilitiesGO.transform, icon, AbilityIconSize);
                    }

                    foreach (var icon in mutationIcons)
                    {
                        if (icon == null) continue;
                        MakeFixedIcon(abilitiesGO.transform, icon, ArmorIconSize);
                    }
                }

                var (spacerGO, _) = NewUI("Spacer", card.transform);
                var spacerLE = spacerGO.AddComponent<LayoutElement>();
                spacerLE.flexibleWidth = 1f;

                var costRow = CreateCostRow(card.transform, data.Haven, data.Haven.Site.GeoLevel.PhoenixFaction);
                if (costRow != null)
                {
                    var costLE = costRow.GetComponent<LayoutElement>() ?? costRow.AddComponent<LayoutElement>();
                    costLE.minWidth = 0f;
                }



            }

            private static IEnumerable<Sprite> GetSelectedAbilityIcons(GeoUnitDescriptor recruit)
            {
                if (recruit == null)
                {
                    yield break;
                }


                var track = recruit.GetPersonalAbilityTrack();
                var abilities = track?.AbilitiesByLevel?.ToList();
                if (abilities == null || abilities.Count == 0)
                {
                    yield break;
                }

                int[] desiredIndexes = { 0, 3, 4 };
                foreach (int index in desiredIndexes)
                {
                    if (index < 0 || index >= abilities.Count)
                    {
                        continue;
                    }

                    var view = abilities[index]?.Ability?.ViewElementDef;
                    if (view?.SmallIcon != null)
                    {
                        yield return view.SmallIcon;
                    }
                }

            }

            private static IEnumerable<Sprite> GetMutatedArmorIcons(GeoUnitDescriptor recruit)
            {
                if (recruit?.ArmorItems == null)
                {
                    yield break;
                }


                var mutationTag = Shared?.SharedGameTags?.AnuMutationTag;
                if (mutationTag == null)
                {
                    yield break;
                }

                foreach (var def in recruit.ArmorItems.Where(i => i != null))
                {

                    if (def.Tags == null || !def.Tags.Contains(mutationTag))
                    {
                        continue;
                    }

                    var ve = def.ViewElementDef;
                    if (ve?.InventoryIcon != null)
                    {
                        yield return ve.InventoryIcon;
                    }

                }

            }

            private static void CreateEmptyLabel(Transform parent, string msg)
            {
                var go = new GameObject("Empty");
                go.transform.SetParent(parent, false);
                var t = go.AddComponent<Text>();
                t.text = msg;
                t.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.fontSize = TextFontSize;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 48);
            }


            private static string GetClassName(GeoUnitDescriptor recruit)
            {
                if (recruit == null) return "Unknown Class";
                try
                {
                    // Fallback: from tags
                    var tagName = recruit.ClassTag;
                    return tagName.className;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
                return "Unknown Class";
            }

            private sealed class ResourceVisual
            {
                public Sprite Icon;
                public Color Color;
            }

            private static readonly Dictionary<ResourceType, ResourceVisual> _resourceVisuals = new Dictionary<ResourceType, ResourceVisual>();

            private static readonly ResourceType[] _resourceDisplayOrder =
             {
                ResourceType.Materials,
                ResourceType.Tech,
                ResourceType.Supplies
            };

            private static void EnsureResourceVisuals()
            {
                if (_resourceVisuals.Count > 0)
                {
                    return;
                }

                try
                {
                    var geoLevel = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var resourcesModule = geoLevel?.View?.GeoscapeModules?.ResourcesModule;
                    if (resourcesModule == null)
                    {
                        return;
                    }

                    AddResourceVisual(ResourceType.Materials, resourcesModule.MaterialsController);
                    AddResourceVisual(ResourceType.Tech, resourcesModule.TechController);
                    AddResourceVisual(ResourceType.Supplies, resourcesModule.FoodController);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }

            }

            private static void AddResourceVisual(ResourceType resourceType, MonoBehaviour controller)
            {
                if (controller == null)
                {
                    return;
                }

                var container = controller.transform.parent.GetComponent<ResourceIconContainer>();
                if (container?.Icon == null)
                {
                    return;
                }
                _resourceVisuals[resourceType] = new ResourceVisual
                {
                    Icon = container.Icon.sprite,
                    Color = container.Icon.color
                };
            }

            private static GameObject CreateCostRow(Transform parent, GeoHaven haven, GeoPhoenixFaction phoenix)
            {

                EnsureResourceVisuals();

                var resourceCosts = GetRecruitCost(haven, phoenix);

                var (row, _) = NewUI("Row_Cost", parent);

                var h = row.AddComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleRight;
                h.spacing = 12f;
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;

                foreach (var type in _resourceDisplayOrder)
                {
                    if (resourceCosts.TryGetValue(type, out var amount))
                    {
                        CreateResourceChip(row.transform, type, amount);
                        resourceCosts.Remove(type);
                    }
                }

                foreach (var kvp in resourceCosts)
                {
                    CreateResourceChip(row.transform, kvp.Key, kvp.Value);
                }

                return row;

            }


            private static GameObject CreateResourceChip(Transform parent, ResourceType resourceType, int amount)
            {
                if (amount <= 0)
                {
                    return null;
                }


                var (chip, _) = NewUI("Res", parent);

                var layout = chip.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 2f;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;


                Image img = null;
                if (_resourceVisuals.TryGetValue(resourceType, out var visual) && visual?.Icon != null)
                {
                    img = MakeFixedIcon(chip.transform, visual.Icon, ResourceIconSize);
                    img.color = visual.Color;
                }
                else
                {
                    var (typeLabelGO, _) = NewUI("Type", chip.transform);
                    var typeLabel = typeLabelGO.AddComponent<Text>();
                    typeLabel.text = resourceType.ToString();
                    typeLabel.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                    typeLabel.fontSize = TextFontSize - 4;
                    typeLabel.alignment = TextAnchor.MiddleCenter;
                }

                // amount
                var (txtGO, _) = NewUI("Amt", chip.transform);
                var t = txtGO.AddComponent<Text>();
                t.text = amount.ToString();
                t.font = _puristaSemibold ? _puristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.fontSize = TextFontSize - 2;
                t.alignment = TextAnchor.MiddleCenter;

                return chip;
            }


            private static Dictionary<ResourceType, int> GetRecruitCost(GeoHaven haven, GeoPhoenixFaction phoenix)
            {
                try
                {

                    var costs = new Dictionary<ResourceType, int>();
                    ResourcePack cost = haven.GetRecruitCost(phoenix);

                    foreach (var type in _resourceDisplayOrder)
                    {
                        var unit = cost.ByResourceType(type);
                        AddCost(type, unit.Value);
                    }

                    foreach (var unit in cost)
                    {


                        var normalizedType = NormalizeResourceType(unit.Type);
                        if (costs.ContainsKey(normalizedType))
                        {
                            continue;
                        }

                        AddCost(normalizedType, unit.Value);

                    }

                    return costs;

                    void AddCost(ResourceType type, float value)
                    {
                        int amount = Mathf.RoundToInt(value);
                        if (amount <= 0)
                        {
                            return;
                        }

                        costs[NormalizeResourceType(type)] = amount;
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); throw; }

            }

            private static ResourceType NormalizeResourceType(ResourceType type)
            {
                if (string.Equals(type.ToString(), "Food", StringComparison.OrdinalIgnoreCase))
                {
                    return ResourceType.Supplies;
                }

                return type;
            }

            // ====== DOUBLE-CLICK: SEND CLOSEST AIRCRAFT ======

            private static readonly Dictionary<GeoSite, float> _travelTimeCache = new Dictionary<GeoSite, float>();

            private static float EstimateTravelTime(GeoSite site, GeoVehicle vehicle, out bool hasPath)
            {
                hasPath = false;
                try
                {
                    if (site == null || vehicle == null || vehicle.Navigation == null) return float.PositiveInfinity;

                    var fromPos = vehicle.CurrentSite?.WorldPosition ?? vehicle.WorldPosition;
                    var targetPos = site.WorldPosition;

                    var path = vehicle.Navigation.FindPath(fromPos, targetPos, out hasPath);
                    if (!hasPath || path == null || path.Count < 2) return float.PositiveInfinity;

                    double totalDist = 0.0;
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        totalDist += GeoMap.Distance(path[i].Pos.WorldPosition, path[i + 1].Pos.WorldPosition).Value;
                    }

                    var speed = vehicle.Stats?.Speed.Value ?? 0f;
                    if (speed <= 0f) return float.PositiveInfinity;

                    return (float)(totalDist / speed);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return float.PositiveInfinity;
                }
            }

            private static GeoVehicle FindClosestPhoenixAircraft(GeoSite site, out float time)
            {
                time = float.PositiveInfinity;
                try
                {
                    if (site?.GeoLevel?.PhoenixFaction?.Vehicles == null) return null;

                    GeoVehicle best = null;
                    foreach (var v in site.GeoLevel.PhoenixFaction.Vehicles)
                    {
                        bool hasPath;
                        float t = EstimateTravelTime(site, v, out hasPath);
                        if (!hasPath) continue;
                        if (t < time)
                        {
                            time = t;
                            best = v;
                        }
                    }
                    return best;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            /// <summary>
            /// Try to issue a move order to <paramref name="vehicle"/> toward <paramref name="site"/>.
            /// Uses several likely methods via reflection so it works across variants.
            /// </summary>
            private static bool TryOrderVehicleToSite(GeoVehicle vehicle, GeoSite site)
            {
                try
                {
                    if (vehicle == null || site == null) return false;

                    Vector3 src = ((vehicle.CurrentSite != null) ? vehicle.CurrentSite.WorldPosition : vehicle.WorldPosition);
                    bool foundPath = false;
                    IList<SitePathNode> source = vehicle.Navigation.FindPath(src, site.WorldPosition, out foundPath);

                    List<GeoSite> geoSites = new List<GeoSite>();

                    geoSites.AddRange(from pn in source
                                      where pn.Site != null && pn.Site != vehicle.CurrentSite
                                      select pn.Site);
                    vehicle.StartTravel(geoSites);


                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }

            /// <summary>
            /// Entry point you can call from the double-click hook.
            /// </summary>
            public static void SendClosestAircraftToSite(GeoSite site)
            {
                try
                {
                    if (site == null) return;

                    float time;
                    var vehicle = FindClosestPhoenixAircraft(site, out time);
                    if (vehicle == null || float.IsPositiveInfinity(time))
                    {
                        TFTVLogger.Always("[Recruits] No reachable Phoenix aircraft for this site.");
                        return;
                    }

                    if (TryOrderVehicleToSite(vehicle, site))
                    {
                        TFTVLogger.Always($"[Recruits] Sent '{vehicle.name}' to '{site.Name}' (ETA ~{time:0.0} time units).");
                        // Optional: focus the map on the vehicle or site for feedback
                        // site.GeoLevel.View.ChaseTarget(vehicle, false);
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }




            // ---------- NAVIGATION ----------

            private static void FocusOnSite(GeoSite site)
            {
                try
                {
                    if (site == null) return;
                    site.GeoLevel.View.ChaseTarget(site, false);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            // ---------- CLOSE BUTTON ----------

            private static void AddCloseButton(GameObject parentPanel)
            {
                try
                {
                    var close = new GameObject("Close");
                    close.transform.SetParent(parentPanel.transform, false);
                    var img = close.AddComponent<Image>();
                    img.color = new Color(0.6f, 0.2f, 0.2f, 0.95f);
                    var btn = close.AddComponent<Button>();

                    var rt = close.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(80, 32);
                    rt.anchoredPosition = new Vector2(-20, -20);

                    var txt = new GameObject("Text").AddComponent<Text>();
                    txt.transform.SetParent(close.transform, false);
                    txt.text = "Close";
                    txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.white;
                    var tr = txt.GetComponent<RectTransform>();
                    tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                    tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

                    btn.onClick.AddListener(() => parentPanel.SetActive(false));
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static string Safe(string s) => string.IsNullOrEmpty(s) ? "Unknown" : s;
        }



    }
}

