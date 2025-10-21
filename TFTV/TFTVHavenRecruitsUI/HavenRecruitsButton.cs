using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsButton
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

                    HavenRecruitsUtils.PopulateFactionNames();

                    var parent = basesBtn.transform.parent as RectTransform;
                    if (parent == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Bases button parent is not a RectTransform; abort.");
                        return;
                    }

                    // Avoid duplicates if Awake runs more than once.
                    var existingButton = parent.Find(RecruitsBtnName);
                    if (existingButton != null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Recruits button already present; skipping.");

                        EnsureVisibilityController(__instance, existingButton.gameObject);
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
                    var group = cloneGO.transform.Find("Group") as RectTransform;
                 
                    if (group != null)
                    {
                        var groupCanvas = group.GetComponent<CanvasGroup>();
                        if (groupCanvas != null)
                        {
                          
                            groupCanvas.alpha = 1f;
                            groupCanvas.interactable = true;
                            groupCanvas.blocksRaycasts = true;
                        }
                    }

                    RectTransform stack = null;
                    int stackSiblingIndex = -1;

                    if (group != null)
                    {
                        foreach (Transform child in group)
                        {
                            if (child.GetComponent<Text>() != null)
                            {
                                stackSiblingIndex = child.GetSiblingIndex();
                                break;
                            }
                        }

                        if (stackSiblingIndex < 0)
                        {
                            var iconCandidate = group.Find("Image_Icon");
                            if (iconCandidate != null)
                            {
                                stackSiblingIndex = iconCandidate.GetSiblingIndex();
                            }
                        }
                    }

                    if (group != null)
                    {

                        stack = group.Find("TFTV_ContentStack") as RectTransform;
                        if (stack == null)
                        {
                            var legacy = group.Find("TFTV_VerticalStack") as RectTransform;
                            if (legacy != null)
                            {
                                stack = legacy;
                                stack.gameObject.name = "TFTV_ContentStack";
                            }
                        }

                        if (stack == null)
                        {
                            var stackGO = new GameObject("TFTV_VerticalStack");
                            stack = stackGO.AddComponent<RectTransform>();
                            stack.SetParent(group, false);
                            stack.anchorMin = Vector2.zero;
                            stack.anchorMax = Vector2.one;
                            stack.pivot = new Vector2(0.5f, 0.5f);
                            stack.offsetMin = Vector2.zero;
                            stack.offsetMax = Vector2.zero;
                        }

                        var stackLayout = stack.GetComponent<VerticalLayoutGroup>();
                        if (stackLayout == null)
                        {
                            stackLayout = stack.gameObject.AddComponent<VerticalLayoutGroup>();
                        }

                        stackLayout.childAlignment = TextAnchor.MiddleCenter;
                        stackLayout.spacing = 8f;
                        stackLayout.childControlWidth = true;
                        stackLayout.childControlHeight = true;
                        stackLayout.childForceExpandWidth = true;
                        stackLayout.childForceExpandHeight = false;

                        var stackLayoutElement = stack.GetComponent<LayoutElement>();
                        if (stackLayoutElement == null)
                        {
                            stackLayoutElement = stack.gameObject.AddComponent<LayoutElement>();
                        }

                        stackLayoutElement.minWidth = 0f;
                        stackLayoutElement.flexibleWidth = 1f;

                        if (stackSiblingIndex >= 0)
                        {
                            stack.SetSiblingIndex(Mathf.Clamp(stackSiblingIndex, 0, group.childCount - 1));
                        }
                        else
                        {
                            stack.SetAsLastSibling();
                        }
                    }

                    var labelParent = stack ?? group;
                    Text label = null;
                    if (labelParent != null)
                    {
                        foreach (Transform child in labelParent)
                        {
                            var textComponent = child.GetComponent<Text>();
                            if (textComponent != null && !string.Equals(child.gameObject.name, "Label_Bottom", StringComparison.Ordinal))
                            {
                                label = textComponent;
                                break;
                            }
                        }

                        if (label == null && group != null)
                        {
                            label = group.GetComponentsInChildren<Text>(true)
                                .FirstOrDefault(t => !string.Equals(t.gameObject.name, "Label_Bottom", StringComparison.Ordinal));
                            if (label != null)
                            {
                                label.transform.SetParent(labelParent, false);
                            }
                        }
                    }
                    else
                    {
                        label = cloneGO.GetComponentsInChildren<Text>(true)
                            .FirstOrDefault(t => !string.Equals(t.gameObject.name, "Label_Bottom", StringComparison.Ordinal));
                    }
                    if (label != null)
                    {
                       
                        label.enabled = true;
                        label.gameObject.SetActive(true);
                        var labelColor = label.color;
                        if (labelColor.a < 1f)
                        {
                            label.color = new Color(labelColor.r, labelColor.g, labelColor.b, 1f);
                        }
                        label.canvasRenderer.SetAlpha(1f);
                        label.gameObject.name = "Label_Top";
                        label.text = "HAVEN";
                        label.color = Color.white;
                        label.enabled = true;
                        label.gameObject.SetActive(true);
                        label.fontSize = 30;
                        label.alignment = TextAnchor.MiddleCenter;
                        label.horizontalOverflow = HorizontalWrapMode.Overflow;
                        label.verticalOverflow = VerticalWrapMode.Overflow;
                        var labelRT = label.rectTransform;
                        labelRT.anchorMin = new Vector2(0f, 0.5f);
                        labelRT.anchorMax = new Vector2(1f, 0.5f);
                        labelRT.pivot = new Vector2(0.5f, 0.5f);
                        var preferredHeight = Mathf.Max(labelRT.sizeDelta.y, 30f);
                        labelRT.sizeDelta = new Vector2(0f, preferredHeight);
                        labelRT.anchoredPosition = Vector2.zero;

                        var labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
                        labelLayout.minHeight = Mathf.Max(labelLayout.minHeight, preferredHeight);
                        labelLayout.preferredHeight = Mathf.Max(labelLayout.preferredHeight, preferredHeight);
                        labelLayout.flexibleHeight = 0f;
                        labelLayout.minWidth = 0f;
                        labelLayout.preferredWidth = -1f;
                        labelLayout.flexibleWidth = 1f;

                        label.transform.SetSiblingIndex(0);
                    }
                   

                    Transform iconTr = null;
                    if (stack != null)
                    {
                        iconTr = stack.Find("Image_Icon");
                        if (iconTr == null)
                        {
                            iconTr = group != null ? group.Find("Image_Icon") : null;
                            iconTr?.SetParent(stack, false);
                        }
                    }
                    else
                    {
                        iconTr = cloneGO.transform.Find("Group/Image_Icon");
                    }
                  
                    var iconImg = iconTr ? iconTr.GetComponent<Image>() : null;


                    if (iconImg != null)
                    {
                        var newSprite = Helper.CreateSpriteFromImageFile("Geoscape_UICanvasIcons_Actions_EliteSoldierRecruitment_uinomipmaps.png");
                        if (newSprite != null)
                        {
                            iconImg.sprite = newSprite;
                            iconImg.preserveAspect = true;
                            // iconImg.rectTransform.sizeDelta = new Vector2(0.2f, 0.2f); // tweak as needed                     
                            iconImg.enabled = true;
                            iconImg.gameObject.SetActive(true);
                            iconImg.color = Color.white;       // ensure fully visible
                            iconImg.canvasRenderer.SetAlpha(1f);     // ensure fully visible
                            iconImg.enabled = true;
                            iconImg.gameObject.SetActive(true);
                            var iconRT = iconImg.rectTransform;
                            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
                            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                            iconRT.pivot = new Vector2(0.5f, 0.5f);
                            iconRT.anchoredPosition = Vector2.zero;

                            var targetSize = iconRT.sizeDelta;
                            if (targetSize == Vector2.zero)
                            {
                                targetSize = new Vector2(96f, 96f);
                            }

                            iconRT.sizeDelta = targetSize;

                            var iconLayout = iconImg.GetComponent<LayoutElement>();
                            if (iconLayout == null)
                            {
                                iconLayout = iconImg.gameObject.AddComponent<LayoutElement>();
                            }

                            iconLayout.minWidth = 0f;
                            iconLayout.minHeight = 0f;
                            iconLayout.preferredWidth = targetSize.x;
                            iconLayout.preferredHeight = targetSize.y;
                            iconLayout.flexibleWidth = 0f;
                            iconLayout.flexibleHeight = 0f;

                            if (labelParent != null)
                            {
                                iconTr.SetSiblingIndex(Mathf.Min(1, labelParent.childCount - 1));
                            }
                        }
                      
                    }
                    
                    if (labelParent != null && label != null && labelParent.Find("Label_Bottom") == null)
                    {
                        var bottomLabelGO = UnityEngine.Object.Instantiate(label.gameObject, labelParent);
                        bottomLabelGO.name = "Label_Bottom";

                        var bottomLabel = bottomLabelGO.GetComponent<Text>();
                        if (bottomLabel != null)
                        {
                            bottomLabel.enabled = true;
                            var bottomColor = bottomLabel.color;
                            if (bottomColor.a < 1f)
                            {
                                bottomLabel.color = new Color(bottomColor.r, bottomColor.g, bottomColor.b, 1f);
                            }
                            bottomLabel.canvasRenderer.SetAlpha(1f);
                            bottomLabel.text = "RECRUITS";
                            bottomLabel.alignment = TextAnchor.MiddleCenter;
                            var bottomRT = bottomLabel.rectTransform;
                            bottomRT.anchorMin = new Vector2(0f, 0.5f);
                            bottomRT.anchorMax = new Vector2(1f, 0.5f);
                            bottomRT.pivot = new Vector2(0.5f, 0.5f);
                            var bottomHeight = Mathf.Max(bottomRT.sizeDelta.y, 30f);
                            bottomRT.sizeDelta = new Vector2(0f, bottomHeight);
                            bottomRT.anchoredPosition = Vector2.zero;

                            var bottomLayout = bottomLabel.GetComponent<LayoutElement>();
                            if (bottomLayout == null)
                            {
                                bottomLayout = bottomLabel.gameObject.AddComponent<LayoutElement>();
                            }

                            bottomLayout.minHeight = Mathf.Max(bottomLayout.minHeight, bottomHeight);
                            bottomLayout.preferredHeight = Mathf.Max(bottomLayout.preferredHeight, bottomHeight);
                            bottomLayout.flexibleHeight = 0f;
                            bottomLayout.minWidth = 0f;
                            bottomLayout.preferredWidth = -1f;
                            bottomLayout.flexibleWidth = 1f;                       
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
                   

                    // Wire up click -> toggle your overlay (use BaseButton to keep stock animations)
                    var pgb = cloneGO.GetComponent<PhoenixGeneralButton>();
                    if (pgb?.BaseButton != null)
                    {
                        pgb.BaseButton.onClick.RemoveAllListeners(); // cloned button shouldn't open Bases
                        pgb.BaseButton.onClick.AddListener(() =>
                        {
                            HavenRecruitsMain.RecruitOverlayManager.ToggleOverlay();
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
                                HavenRecruitsMain.RecruitOverlayManager.ToggleOverlay();
                            });
                        }
                    }

                    if (stack != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
                    }
                    else if (group != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(group);
                    }

                    EnsureVisibilityController(__instance, cloneGO);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        private static void EnsureVisibilityController(UIModuleSiteManagement module, GameObject button)
        {
            if (module == null || button == null)
            {
                return;
            }

            var controller = module.GetComponent<HavenRecruitsButtonVisibilityController>();
            if (controller == null)
            {
                controller = module.gameObject.AddComponent<HavenRecruitsButtonVisibilityController>();
            }

            controller.Initialize(button);
        }

        private sealed class HavenRecruitsButtonVisibilityController : MonoBehaviour
        {
            private GameObject _button;
            private bool _lastVisible;
            private Coroutine _refreshRoutine;

            internal void Initialize(GameObject button)
            {
                _button = button;
                UpdateVisibility(force: true);

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

                UpdateVisibility(force: true);
            }

            private void OnDisable()
            {
                if (_refreshRoutine != null)
                {
                    StopCoroutine(_refreshRoutine);
                    _refreshRoutine = null;
                }
            }

            private IEnumerator RefreshRoutine()
            {
                var wait = new WaitForSecondsRealtime(1f);
                while (true)
                {
                    yield return wait;
                    UpdateVisibility(force: false);
                }
            }

            private void UpdateVisibility(bool force)
            {
                if (_button == null)
                {
                    return;
                }

                bool shouldShow = ShouldShowButton();
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

            private static bool ShouldShowButton()
            {
                try
                {
                    var geoLevelController = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    var research = geoLevelController?.PhoenixFaction?.Research;
                    return research != null && research.HasCompleted(HavenRecruitsMain.HavenRecruitsResearchId);
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }
        }
    }
}
