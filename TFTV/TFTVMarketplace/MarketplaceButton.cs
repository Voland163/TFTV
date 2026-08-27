using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVMarketplace
{
    /// <summary>
    /// A geoscape shortcut to the Marketplace, sitting in the same row as the vanilla Bases button and
    /// the Haven Recruits button. It appears once the Marketplace has been found, and its icon pulses
    /// while the stock has rotated since the player last looked.
    /// </summary>
    internal static class MarketplaceButton
    {
        private const string MarketplaceBtnName = "UIButton_Icon_Marketplace";
        private const float LeftPaddingPx = 16f;

        // Bases sits at slot 0, Haven Recruits at slot 1, so the Marketplace takes slot 2.
        private const int ButtonSlot = 2;

        // Bumped by one on every stock rotation (see TFTVMarketPlaceGenerateOffers).
        private const string StockRotationsVariable = "MarketPlaceRotations";

        // The rotation count the player has already seen. Kept on the event system so it survives saves.
        private const string StockRotationsSeenVariable = "TFTV_MarketplaceRotationsSeen";

        // Set to 4 the moment the Marketplace site is first visited (TFTVMarketplace/Various.cs).
        private const string MarketplaceDiscoveredVariable = "NumberOfDLC5MissionsCompletedVariable";

        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly Color FlashColor = new Color(1f, 0.4f, 0f, 1f);

        private static Sprite _marketplaceIcon;

        [HarmonyPatch(typeof(UIModuleSiteManagement), nameof(UIModuleSiteManagement.Awake))]
        public static class AddMarketplaceButton_OnSiteManagementAwake
        {
            public static void Postfix(UIModuleSiteManagement __instance)
            {
                try
                {
                    PhoenixGeneralButton basesBtn = __instance?.OpenModuleButton;
                    if (basesBtn == null)
                    {
                        TFTVLogger.Always("[MarketplaceBtn] OpenModuleButton is null; abort.");
                        return;
                    }

                    RectTransform parent = basesBtn.transform.parent as RectTransform;
                    if (parent == null)
                    {
                        TFTVLogger.Always("[MarketplaceBtn] Bases button parent is not a RectTransform; abort.");
                        return;
                    }

                    Transform existingButton = parent.Find(MarketplaceBtnName);
                    if (existingButton != null)
                    {
                        EnsureController(__instance, existingButton.gameObject);
                        return;
                    }

                    GameObject templateGO = basesBtn.gameObject;
                    GameObject cloneGO = UnityEngine.Object.Instantiate(templateGO, parent, worldPositionStays: false);
                    cloneGO.name = MarketplaceBtnName;
                    cloneGO.SetActive(true);

                    RectTransform tplRT = templateGO.GetComponent<RectTransform>();
                    RectTransform rt = cloneGO.GetComponent<RectTransform>();
                    rt.anchorMin = tplRT.anchorMin;
                    rt.anchorMax = tplRT.anchorMax;
                    rt.pivot = tplRT.pivot;
                    rt.sizeDelta = tplRT.sizeDelta;
                    rt.localScale = tplRT.localScale;
                    rt.anchoredPosition = tplRT.anchoredPosition
                        + new Vector2(-ButtonSlot * (tplRT.sizeDelta.x + LeftPaddingPx), 0f);

                    CanvasGroup cg = cloneGO.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

                    RectTransform group = cloneGO.transform.Find("Group") as RectTransform;
                    if (group != null)
                    {
                        CanvasGroup groupCanvas = group.GetComponent<CanvasGroup>();
                        if (groupCanvas != null)
                        {
                            groupCanvas.alpha = 1f;
                            groupCanvas.interactable = true;
                            groupCanvas.blocksRaycasts = true;
                        }
                    }

                    RectTransform stack = BuildContentStack(group);
                    Transform labelParent = (Transform)stack ?? group;

                    Text label = FindLabel(cloneGO, labelParent, group);
                    if (label != null)
                    {
                        SetupLabel(label, TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_MARKETPLACE_BUTTON_TOP"));
                        label.gameObject.name = "Label_Top";
                        label.transform.SetSiblingIndex(0);
                    }

                    Transform iconTr = FindIcon(cloneGO, stack, group);
                    Image iconImg = iconTr != null ? iconTr.GetComponent<Image>() : null;
                    if (iconImg != null)
                    {
                        Sprite icon = GetMarketplaceIcon();
                        if (icon != null)
                        {
                            iconImg.sprite = icon;
                        }

                        SetupIcon(iconImg, labelParent);
                    }

                    AddBottomLabel(labelParent, label, iconTr, TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_MARKETPLACE_BUTTON_BOTTOM"));

                    WireClick(cloneGO);

                    if (stack != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
                    }
                    else if (group != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(group);
                    }

                    EnsureController(__instance, cloneGO);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        /// <summary>
        /// Opening the Marketplace at all - through this button, or by flying an aircraft over as usual -
        /// counts as having seen the current stock, so the button stops calling for attention.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleTheMarketplace), nameof(UIModuleTheMarketplace.ShowEncounter))]
        public static class UIModuleTheMarketplace_ShowEncounter_patch
        {
            public static void Postfix()
            {
                try
                {
                    MarkRestockSeen(GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>());
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        /// <summary>
        /// The stock has rotated since the last time the player opened the Marketplace.
        /// </summary>
        internal static bool HasUnseenRestock(GeoLevelController level)
        {
            GeoscapeEventSystem eventSystem = level?.EventSystem;
            if (eventSystem == null)
            {
                return false;
            }

            return eventSystem.GetVariable(StockRotationsVariable) > eventSystem.GetVariable(StockRotationsSeenVariable);
        }

        internal static void MarkRestockSeen(GeoLevelController level)
        {
            GeoscapeEventSystem eventSystem = level?.EventSystem;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.SetVariable(StockRotationsSeenVariable, eventSystem.GetVariable(StockRotationsVariable));
        }

        private static bool IsMarketplaceDiscovered(GeoLevelController level)
        {
            return level != null
                && level.HasKaosEngines
                && level.EventSystem != null
                && level.EventSystem.GetVariable(MarketplaceDiscoveredVariable) > 0;
        }

        private static void OpenMarketplace()
        {
            try
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                if (!IsMarketplaceDiscovered(level))
                {
                    return;
                }

                GeoscapeEventDef marketplaceEvent = level.TheMarketplaceSettings?.MarketplaceEvent;
                GeoSite marketplaceSite = level.Map?.ActiveSites?.FirstOrDefault(site => site.Type == GeoSiteType.Marketplace);

                if (marketplaceEvent == null || marketplaceSite == null)
                {
                    TFTVLogger.Always($"[MarketplaceBtn] Cannot open the Marketplace: event {(marketplaceEvent == null ? "missing" : "found")}, site {(marketplaceSite == null ? "missing" : "found")}.");
                    return;
                }

                level.View.ToMarketplace(marketplaceEvent.GeoscapeEventData, marketplaceSite);
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// The same icon the Marketplace site wears on the geoscape. That one lives on a Material (the site
        /// icons are drawn by a MeshRenderer, not by uGUI), so its texture has to be wrapped in a Sprite
        /// before an Image can show it. Falls back to the Marketplace ability's icon.
        /// </summary>
        private static Sprite GetMarketplaceIcon()
        {
            try
            {
                if (_marketplaceIcon != null)
                {
                    return _marketplaceIcon;
                }

                _marketplaceIcon = CreateSpriteFromSiteMaterial(GeoSiteVisualsDefs.Instance?.Marketplace);

                if (_marketplaceIcon == null)
                {
                    MarketplaceAbilityDef abilityDef = Repo.GetAllDefs<MarketplaceAbilityDef>().FirstOrDefault();
                    _marketplaceIcon = abilityDef?.ViewElementDef?.LargeIcon ?? abilityDef?.ViewElementDef?.SmallIcon;

                    TFTVLogger.Always($"[MarketplaceBtn] Could not build an icon from the Marketplace site material; {(_marketplaceIcon == null ? "keeping the cloned Bases icon" : "falling back to the Marketplace ability icon")}.");
                }

                return _marketplaceIcon;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        private static Sprite CreateSpriteFromSiteMaterial(Material siteMaterial)
        {
            if (siteMaterial == null)
            {
                return null;
            }

            // Site icon shaders do not all name their texture the same way.
            Texture2D texture = siteMaterial.mainTexture as Texture2D;

            if (texture == null && siteMaterial.HasProperty("_MainTex"))
            {
                texture = siteMaterial.GetTexture("_MainTex") as Texture2D;
            }

            if (texture == null && siteMaterial.HasProperty("_BaseMap"))
            {
                texture = siteMaterial.GetTexture("_BaseMap") as Texture2D;
            }

            if (texture == null)
            {
                return null;
            }

            TFTVLogger.Always($"[MarketplaceBtn] Using the Marketplace site icon texture '{texture.name}' ({texture.width}x{texture.height}).");

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        private static void WireClick(GameObject cloneGO)
        {
            PhoenixGeneralButton pgb = cloneGO.GetComponent<PhoenixGeneralButton>();
            if (pgb?.BaseButton != null)
            {
                pgb.BaseButton.onClick.RemoveAllListeners();
                pgb.BaseButton.onClick.AddListener(OpenMarketplace);
                return;
            }

            Button uiBtn = cloneGO.GetComponent<Button>();
            if (uiBtn != null)
            {
                uiBtn.onClick.RemoveAllListeners();
                uiBtn.onClick.AddListener(OpenMarketplace);
            }
        }

        private static RectTransform BuildContentStack(RectTransform group)
        {
            if (group == null)
            {
                return null;
            }

            RectTransform stack = group.Find("TFTV_ContentStack") as RectTransform;
            if (stack == null)
            {
                GameObject stackGO = new GameObject("TFTV_ContentStack");
                stack = stackGO.AddComponent<RectTransform>();
                stack.SetParent(group, false);
                stack.anchorMin = Vector2.zero;
                stack.anchorMax = Vector2.one;
                stack.pivot = new Vector2(0.5f, 0.5f);
                stack.offsetMin = Vector2.zero;
                stack.offsetMax = Vector2.zero;
            }

            VerticalLayoutGroup stackLayout = stack.GetComponent<VerticalLayoutGroup>()
                ?? stack.gameObject.AddComponent<VerticalLayoutGroup>();
            stackLayout.childAlignment = TextAnchor.MiddleCenter;
            stackLayout.spacing = 8f;
            stackLayout.childControlWidth = true;
            stackLayout.childControlHeight = true;
            stackLayout.childForceExpandWidth = true;
            stackLayout.childForceExpandHeight = false;

            LayoutElement stackLayoutElement = stack.GetComponent<LayoutElement>()
                ?? stack.gameObject.AddComponent<LayoutElement>();
            stackLayoutElement.minWidth = 0f;
            stackLayoutElement.flexibleWidth = 1f;

            stack.SetAsLastSibling();

            return stack;
        }

        private static Text FindLabel(GameObject cloneGO, Transform labelParent, RectTransform group)
        {
            if (labelParent != null)
            {
                foreach (Transform child in labelParent)
                {
                    Text textComponent = child.GetComponent<Text>();
                    if (textComponent != null && !string.Equals(child.gameObject.name, "Label_Bottom", StringComparison.Ordinal))
                    {
                        return textComponent;
                    }
                }
            }

            Text found = (group != null ? group : cloneGO.transform)
                .GetComponentsInChildren<Text>(true)
                .FirstOrDefault(t => !string.Equals(t.gameObject.name, "Label_Bottom", StringComparison.Ordinal));

            if (found != null && labelParent != null)
            {
                found.transform.SetParent(labelParent, false);
            }

            return found;
        }

        private static void SetupLabel(Text label, string text)
        {
            label.enabled = true;
            label.gameObject.SetActive(true);
            label.canvasRenderer.SetAlpha(1f);
            label.text = text;
            label.color = Color.white;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform labelRT = label.rectTransform;
            labelRT.anchorMin = new Vector2(0f, 0.5f);
            labelRT.anchorMax = new Vector2(1f, 0.5f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            float preferredHeight = Mathf.Max(labelRT.sizeDelta.y, 30f);
            labelRT.sizeDelta = new Vector2(0f, preferredHeight);
            labelRT.anchoredPosition = Vector2.zero;

            LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = Mathf.Max(labelLayout.minHeight, preferredHeight);
            labelLayout.preferredHeight = Mathf.Max(labelLayout.preferredHeight, preferredHeight);
            labelLayout.flexibleHeight = 0f;
            labelLayout.minWidth = 0f;
            labelLayout.preferredWidth = -1f;
            labelLayout.flexibleWidth = 1f;
        }

        private static Transform FindIcon(GameObject cloneGO, RectTransform stack, RectTransform group)
        {
            if (stack != null)
            {
                Transform iconTr = stack.Find("Image_Icon");
                if (iconTr != null)
                {
                    return iconTr;
                }

                iconTr = group != null ? group.Find("Image_Icon") : null;
                iconTr?.SetParent(stack, false);
                return iconTr;
            }

            return cloneGO.transform.Find("Group/Image_Icon");
        }

        private static void SetupIcon(Image iconImg, Transform labelParent)
        {
            iconImg.preserveAspect = true;
            iconImg.enabled = true;
            iconImg.gameObject.SetActive(true);
            iconImg.color = Color.white;
            iconImg.canvasRenderer.SetAlpha(1f);

            RectTransform iconRT = iconImg.rectTransform;
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;

            Vector2 targetSize = iconRT.sizeDelta;
            if (targetSize == Vector2.zero)
            {
                targetSize = new Vector2(96f, 96f);
            }

            iconRT.sizeDelta = targetSize;

            LayoutElement iconLayout = iconImg.GetComponent<LayoutElement>() ?? iconImg.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = 0f;
            iconLayout.minHeight = 0f;
            iconLayout.preferredWidth = targetSize.x;
            iconLayout.preferredHeight = targetSize.y;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;

            if (labelParent != null)
            {
                iconImg.transform.SetSiblingIndex(Mathf.Min(1, labelParent.childCount - 1));
            }
        }

        private static void AddBottomLabel(Transform labelParent, Text label, Transform iconTr, string text)
        {
            if (labelParent == null || label == null || labelParent.Find("Label_Bottom") != null)
            {
                return;
            }

            GameObject bottomLabelGO = UnityEngine.Object.Instantiate(label.gameObject, labelParent);
            bottomLabelGO.name = "Label_Bottom";

            Text bottomLabel = bottomLabelGO.GetComponent<Text>();
            if (bottomLabel != null)
            {
                SetupLabel(bottomLabel, text);
            }

            if (iconTr != null)
            {
                bottomLabelGO.transform.SetSiblingIndex(iconTr.GetSiblingIndex() + 1);
            }
            else
            {
                bottomLabelGO.transform.SetAsLastSibling();
            }
        }

        private static Image FindButtonIcon(GameObject button)
        {
            Transform iconTransform = button.transform.Find("Group/TFTV_ContentStack/Image_Icon")
                                   ?? button.transform.Find("Group/Image_Icon");

            return iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        private static void EnsureController(UIModuleSiteManagement module, GameObject button)
        {
            if (module == null || button == null)
            {
                return;
            }

            MarketplaceButtonController controller = module.GetComponent<MarketplaceButtonController>()
                ?? module.gameObject.AddComponent<MarketplaceButtonController>();

            controller.Initialize(button, FindButtonIcon(button));
        }

        /// <summary>
        /// Keeps the button hidden until the Marketplace has been found, and pulses its icon while there
        /// is stock the player has not seen yet.
        /// </summary>
        private sealed class MarketplaceButtonController : MonoBehaviour
        {
            private const float PulsePeriodSeconds = 1.2f;
            private const float StateCheckIntervalSeconds = 0.5f;

            private GameObject _button;
            private Image _icon;
            private Coroutine _refreshRoutine;
            private bool _lastVisible;
            private bool _hasUnseenRestock;

            internal void Initialize(GameObject button, Image icon)
            {
                _button = button;
                _icon = icon;

                ForceRefreshState();

                if (isActiveAndEnabled && _refreshRoutine == null)
                {
                    _refreshRoutine = StartCoroutine(RefreshRoutine());
                }
            }

            private void OnEnable()
            {
                if (_refreshRoutine == null)
                {
                    _refreshRoutine = StartCoroutine(RefreshRoutine());
                }

                ForceRefreshState();
            }

            private void ForceRefreshState()
            {
                try
                {
                    GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                    UpdateVisibility(IsMarketplaceDiscovered(level), force: true);

                    _hasUnseenRestock = _lastVisible && HasUnseenRestock(level);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private void OnDisable()
            {
                if (_refreshRoutine != null)
                {
                    StopCoroutine(_refreshRoutine);
                    _refreshRoutine = null;
                }

                ResetIconColor();
            }

            // Nothing here needs a frame-by-frame heartbeat in the common case: while the button is not
            // pulsing the loop just wakes twice a second to re-read the state, and only steps down to
            // per-frame updates for as long as there is actually a pulse to animate.
            private IEnumerator RefreshRoutine()
            {
                WaitForSecondsRealtime idleWait = new WaitForSecondsRealtime(StateCheckIntervalSeconds);

                while (true)
                {
                    RefreshState();

                    if (!_hasUnseenRestock)
                    {
                        yield return idleWait;
                        continue;
                    }

                    float nextStateCheck = Time.unscaledTime + StateCheckIntervalSeconds;

                    while (_hasUnseenRestock && Time.unscaledTime < nextStateCheck)
                    {
                        UpdateFlashing();
                        yield return null;
                    }
                }
            }

            private void RefreshState()
            {
                bool wasFlashing = _hasUnseenRestock;

                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                UpdateVisibility(IsMarketplaceDiscovered(level), force: false);

                _hasUnseenRestock = _lastVisible && HasUnseenRestock(level);

                // Leaving the pulse behind means the icon is stuck mid-fade.
                if (wasFlashing && !_hasUnseenRestock)
                {
                    ResetIconColor();
                }
            }

            private void UpdateVisibility(bool shouldShow, bool force)
            {
                if (_button == null)
                {
                    return;
                }

                if (!force && shouldShow == _lastVisible)
                {
                    return;
                }

                _lastVisible = shouldShow;
                if (_button.activeSelf != shouldShow)
                {
                    _button.SetActive(shouldShow);
                }
            }

            private void UpdateFlashing()
            {
                if (_icon == null)
                {
                    return;
                }

                float t = Mathf.PingPong(Time.unscaledTime * (2f / PulsePeriodSeconds), 1f);
                _icon.color = Color.Lerp(Color.white, FlashColor, t);
            }

            private void ResetIconColor()
            {
                if (_icon != null && _icon.color != Color.white)
                {
                    _icon.color = Color.white;
                }
            }

        }
    }
}
