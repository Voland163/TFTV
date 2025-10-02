using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                    TFTVLogger.Always("[RecruitsBtn] Recruits button added left of Bases.");
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }
    }
}
