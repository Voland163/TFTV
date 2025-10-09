using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
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

                    TFTVLogger.Always($"[RecruitsBtn] Cloned button '{cloneGO.name}'. ActiveSelf={cloneGO.activeSelf}, ActiveInHierarchy={cloneGO.activeInHierarchy}");

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
                    TFTVLogger.Always($"[RecruitsBtn] Group rect present={group != null}");
                    if (group != null)
                    {
                        var groupCanvas = group.GetComponent<CanvasGroup>();
                        if (groupCanvas != null)
                        {
                            TFTVLogger.Always($"[RecruitsBtn] Group CanvasGroup alpha={groupCanvas.alpha}; forcing visible state.");
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

                    var labelParent = stack != null ? stack : group;
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
                        TFTVLogger.Always("[RecruitsBtn] Label parent is null; falling back to global search for text.");

                        label = cloneGO.GetComponentsInChildren<Text>(true)
                            .FirstOrDefault(t => !string.Equals(t.gameObject.name, "Label_Bottom", StringComparison.Ordinal));
                    }
                    if (label != null)
                    {
                        TFTVLogger.Always($"[RecruitsBtn] Top label found on '{label.gameObject.name}'. Enabled={label.enabled}, ActiveSelf={label.gameObject.activeSelf}, ActiveInHierarchy={label.gameObject.activeInHierarchy}");
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

                        var labelLayout = label.GetComponent<LayoutElement>();
                        if (labelLayout == null)
                        {
                            labelLayout = label.gameObject.AddComponent<LayoutElement>();
                        }

                        labelLayout.minHeight = Mathf.Max(labelLayout.minHeight, preferredHeight);
                        labelLayout.preferredHeight = Mathf.Max(labelLayout.preferredHeight, preferredHeight);
                        labelLayout.flexibleHeight = 0f;
                        labelLayout.minWidth = 0f;
                        labelLayout.preferredWidth = -1f;
                        labelLayout.flexibleWidth = 1f;

                        label.transform.SetSiblingIndex(0);
                    }
                    else
                    {
                        TFTVLogger.Always("[RecruitsBtn] Failed to locate a top Text component for the recruits button.");
                    }

                    Transform iconTr = null;
                    if (stack != null)
                    {
                        iconTr = stack.Find("Image_Icon");
                        if (iconTr == null)
                        {
                            iconTr = group != null ? group.Find("Image_Icon") : null;
                            if (iconTr != null)
                            {
                                iconTr.SetParent(stack, false);
                            }
                        }
                    }
                    else
                    {
                        iconTr = cloneGO.transform.Find("Group/Image_Icon");
                    }
                    if (iconTr == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Could not find 'Group/Image_Icon' on clone.");
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
                        else
                        {
                            TFTVLogger.Always("[RecruitsBtn] Failed to load sprite 'UI_StatusesIcons_CanBeRecruitedIntoPhoenix-2.png'");
                        }
                    }
                    else if (iconTr != null)
                    {
                      
                        TFTVLogger.Always("[RecruitsBtn] Icon image component missing.");
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

                            TFTVLogger.Always($"[RecruitsBtn] Bottom label cloned. Enabled={bottomLabel.enabled}, ActiveSelf={bottomLabel.gameObject.activeSelf}, ActiveInHierarchy={bottomLabel.gameObject.activeInHierarchy}");
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
                    else if (labelParent == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Skipping bottom label setup because label parent is missing.");
                    }
                    else if (label == null)
                    {
                        TFTVLogger.Always("[RecruitsBtn] Skipping bottom label setup because no top label was found.");
                    }

                    // Wire up click -> toggle your overlay (use BaseButton to keep stock animations)
                    var pgb = cloneGO.GetComponent<PhoenixGeneralButton>();
                    if (pgb?.BaseButton != null)
                    {
                        pgb.BaseButton.onClick.RemoveAllListeners(); // cloned button shouldn't open Bases
                        pgb.BaseButton.onClick.AddListener(() =>
                        {
                            TFTVLogger.Always("[RecruitsBtn] Clicked RECRUITS button.");
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
                                TFTVLogger.Always("[RecruitsBtn] Clicked RECRUITS button (fallback).");
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

                    TFTVLogger.Always("[RecruitsBtn] Recruits button added left of Bases.");
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }
    }
}
