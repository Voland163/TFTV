using PhoenixPoint.Common.Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVUI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsUtils;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsRecruitItem
    {
        internal static RecruitCardView EnsureCardHierarchy(Transform parent)

        {
            var card = new GameObject("RecruitCard", typeof(RectTransform));
            card.transform.SetParent(parent, false);

            var cardView = card.AddComponent<RecruitCardView>();

          
            var bg = card.AddComponent<Image>();
            bg.color = CardBackgroundColor;

            var border = card.AddComponent<Outline>();
            border.effectColor = CardBorderColor;
            border.effectDistance = new Vector2(2f, 2f);
            border.useGraphicAlpha = false;

       
            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            card.AddComponent<CardClickHandler>();

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 15f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 6, 4, 4);

         
            var fit = card.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var classIconImage = RecruitOverlayManagerHelpers.MakeFixedIcon(card.transform, null, ClassIconSize);
            if (classIconImage != null)
            {
                cardView.ClassIconImage = classIconImage;
                cardView.ClassIconDefaultColor = classIconImage.color;
                cardView.ClassIconRoot = classIconImage.transform.parent?.gameObject;
                if (cardView.ClassIconRoot != null)
                {
                    cardView.ClassIconRoot.SetActive(false);
                }
            }

            var (levelGO, levelRT) = RecruitOverlayManagerHelpers.NewUI("Level", card.transform);
            var levelText = levelGO.AddComponent<Text>();
            levelText.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.fontSize = TextFontSize;
            levelText.color = Color.white;
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.text = string.Empty;
            cardView.LevelLabel = levelText;
            cardView.LevelDefaultColor = levelText.color;
            levelRT.pivot = new Vector2(0f, 0.5f);
            levelRT.anchorMin = new Vector2(0f, 0.5f);
            levelRT.anchorMax = new Vector2(0f, 0.5f);
            levelRT.anchoredPosition = Vector2.zero;

            var levelSizeFitter = levelGO.AddComponent<ContentSizeFitter>();
            levelSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            levelSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var (nameGO, nameRT) = RecruitOverlayManagerHelpers.NewUI("Name", card.transform);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = TextFontSize;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = string.Empty;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            cardView.NameLabel = nameText;
            cardView.NameDefaultColor = nameText.color;
            nameRT.pivot = new Vector2(0f, 0.5f);
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = Vector2.zero;

            var nameFitter = nameGO.AddComponent<ContentSizeFitter>();
            nameFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var (costRowGO, _) = RecruitOverlayManagerHelpers.NewUI("CostRow", card.transform);
            var costLayout = costRowGO.AddComponent<HorizontalLayoutGroup>();
            costLayout.childAlignment = TextAnchor.MiddleRight;
            costLayout.spacing = 0f;
            costLayout.childControlWidth = true;
            costLayout.childControlHeight = true;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = false;
            var costLE = costRowGO.AddComponent<LayoutElement>();
            costLE.minWidth = 0f;
            costLE.preferredWidth = 0f;
            costLE.flexibleWidth = 1f;
            cardView.CostRow = costRowGO.transform;

            var (abilitiesGO, abilitiesRT) = RecruitOverlayManagerHelpers.NewUI("Abilities", card.transform);
            var abilitiesLE = abilitiesGO.AddComponent<LayoutElement>();
            abilitiesLE.ignoreLayout = true;
            abilitiesRT.anchorMin = new Vector2(1f, 0.5f);
            abilitiesRT.anchorMax = new Vector2(1f, 0.5f);
            abilitiesRT.pivot = new Vector2(1f, 0.5f);
            abilitiesRT.anchoredPosition = new Vector2(AbilityIconsCenterOffsetPx, 0f);

            var abilitiesLayout = abilitiesGO.AddComponent<HorizontalLayoutGroup>();
            abilitiesLayout.childAlignment = TextAnchor.MiddleCenter;
            abilitiesLayout.spacing = 20f;
            abilitiesLayout.childControlWidth = true;
            abilitiesLayout.childControlHeight = true;
            abilitiesLayout.childForceExpandWidth = false;
            abilitiesLayout.childForceExpandHeight = false;

            var abilitiesFitter = abilitiesGO.AddComponent<ContentSizeFitter>();
            abilitiesFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            abilitiesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardView.AbilityContainer = abilitiesGO.transform;
            abilitiesGO.SetActive(false);

            return cardView;
        }
        internal static void PopulateRecruitItem(RecruitCardView cardView, RecruitAtSite data, bool collapse)
        {
            try
            {
                if (cardView == null || data == null)
                {
                    return;
                }

                _ = collapse;

                var card = cardView.gameObject;
                card.name = $"Recruit_{data.Recruit?.GetName()}";

                var bg = card.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = CardBackgroundColor;
                }
                var outline = card.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = CardBorderColor;
                }

                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                }

                var click = card.GetComponent<CardClickHandler>();
                if (click != null)
                {
                    click.OnSingle = () =>
                    {
                        // The free cursor always reports clickCount 1, so a gamepad can never reach the
                        // double-click path. Activating an already-selected card stands in for it there.
                        if (ControllerNav.IsUsingController() && RecruitOverlayManager._currentSelectedCard == card)
                        {
                            OnCardDoubleClick?.Invoke(data.Recruit, data.Site);
                            return;
                        }

                        HavenRecruitsGeoscapeInteractions.FocusOnSite(data.Site);
                        HandleRecruitSelected(card, data);
                    };
                    click.OnDouble = () =>
                    {
                        HandleRecruitSelected(card, data);
                        OnCardDoubleClick?.Invoke(data.Recruit, data.Site);
                    };
                }

                if (cardView.LevelLabel != null)
                {
                    int level = data.Recruit?.Level ?? 0;
                    bool showLevel = !IsVehicleOrMutog(data.Recruit) && level > 0;

                    cardView.LevelLabel.text = showLevel ? level.ToString() : string.Empty;
                    cardView.LevelLabel.color = cardView.LevelDefaultColor;
                    var levelLabelObject = cardView.LevelLabel.gameObject;
                    levelLabelObject?.SetActive(showLevel);
                }

                if (cardView.NameLabel != null)
                {
                    cardView.NameLabel.text = data.Recruit?.GetName() ?? HavenRecruitsMain.UiUnknownRecruitText;
                    cardView.NameLabel.color = cardView.NameDefaultColor;
                }

                var classIcon = GetClassIcon(data.Recruit);
                if (cardView.ClassIconImage != null)
                {
                    cardView.ClassIconImage.sprite = classIcon;
                    cardView.ClassIconImage.color = cardView.ClassIconDefaultColor;
                }
                if (cardView.ClassIconRoot != null)
                {
                    cardView.ClassIconRoot.SetActive(classIcon != null);
                }

                var abilityInfos = IsVehicleOrMutog(data.Recruit)
                     ? new List<HavenRecruitsUtils.AbilityIconData>()
                     : GetSelectedAbilityIcons(data.Recruit).ToList();
                var mutationIcons = GetMutationIcons(data.Recruit).ToList();
                var weaponItems = IsVehicleOrMutog(data.Recruit)
                   ? GetVehicleOrMutogWeapons(data.Recruit).ToList()
                   : new List<ItemDef>();

                var abilitiesTransform = cardView.AbilityContainer;
                if (abilitiesTransform != null)
                {
                    RecruitOverlayManagerHelpers.ClearTransformChildren(abilitiesTransform);
                    bool hasAbilities = abilityInfos.Count > 0 || mutationIcons.Count > 0 || weaponItems.Count > 0;
                    abilitiesTransform.gameObject.SetActive(hasAbilities);

                    if (hasAbilities)
                    {
                        foreach (var icon in mutationIcons)
                        {
                            if (icon.Icon == null)
                            {
                                continue;
                            }
                            RecruitOverlayManagerHelpers.MakeMutationSlot(abilitiesTransform, icon, ArmorIconSize);
                        }

                        foreach (var weapon in weaponItems)
                        {
                            if (weapon == null)
                            {
                                continue;
                            }

                            RecruitOverlayManagerHelpers.MakeInventorySlot(abilitiesTransform, weapon, ArmorIconSize, "WeaponSlot");
                        }


                        foreach (var ability in abilityInfos)
                        {
                            if (ability.Icon == null)
                            {
                                continue;
                            }

                            var abilityView = GetAbilityIconView(abilitiesTransform);
                            if (abilityView == null)
                            {
                                continue;
                            }

                            abilityView.Prepare(ability, _abilityIconBackground);

                          
                        }

                        abilitiesTransform.SetAsLastSibling();
                    }
                }

                var costRow = cardView.CostRow;
                if (costRow != null)
                {
                    
                    var haven = data.Haven;
                    var phoenix = data.Haven?.Site?.GeoLevel?.PhoenixFaction;
                    if (haven != null && phoenix != null)
                    {
                        HavenRecruitsPrice.PopulateCostRow(costRow, haven, phoenix, cardView);
                    }
                }
            }
            catch (System.Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        internal static RecruitCardView CreateRecruitItem(Transform parent, RecruitAtSite data, bool collapse)
        {
            var cardView = EnsureCardHierarchy(parent);
            PopulateRecruitItem(cardView, data, collapse);
            return cardView;
        }



        internal static void CreateEmptyLabel(Transform parent, string msg)
        {
            var go = new GameObject("Empty");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = msg;
            t.font = PuristaSemibold ? PuristaSemibold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = TextFontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);
        }

        /// <summary>
        /// Describes one card as a left-to-right run of gamepad targets: the recruit's name first, so
        /// moving up and down the list parks the cursor on the name, then every icon on the line that
        /// shows a tooltip on hover. The free cursor raycasts wherever it is snapped, so landing on an
        /// icon fires that icon's existing pointer-enter tooltip with no extra wiring.
        /// </summary>
        internal static List<Selectable> BuildNavigationRow(RecruitCardView cardView)
        {
            var row = new List<Selectable>();

            try
            {
                if (cardView == null || cardView.gameObject == null)
                {
                    return row;
                }

                var card = cardView.gameObject;
                var clickHandler = card.GetComponent<CardClickHandler>();

                if (cardView.NameLabel != null)
                {
                    var head = EnsureNavTarget(cardView.NameLabel.gameObject, clickHandler);
                    if (head != null)
                    {
                        row.Add(head);
                    }
                }

                var icons = new List<Selectable>();
                CollectHoverTargets(cardView.AbilityContainer, clickHandler, icons);
                CollectHoverTargets(cardView.CostRow, clickHandler, icons);

                // The two containers are positioned independently, so screen order is the only reliable
                // way to know what sits left of what.
                icons.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                row.AddRange(icons);

                if (row.Count == 0)
                {
                    var cardButton = card.GetComponent<Button>();
                    if (cardButton != null)
                    {
                        row.Add(cardButton);
                    }
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return row;
        }

        private static void CollectHoverTargets(Transform container, CardClickHandler clickHandler, List<Selectable> into)
        {
            if (container == null || !container.gameObject.activeSelf)
            {
                return;
            }

            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                // Search children too: an ability icon carries its tooltip trigger on the inner image,
                // but the frame is what should be navigated to.
                if (child.GetComponentInChildren<IPointerEnterHandler>() == null)
                {
                    continue;
                }

                var target = EnsureNavTarget(child.gameObject, clickHandler);
                if (target != null)
                {
                    into.Add(target);
                }
            }
        }

        /// <summary>
        /// Makes one object navigable, reusing whatever Selectable it already has. A click forwarder is
        /// only attached when this adds the Selectable itself and the object had no click behaviour of its
        /// own, so inventory slots keep handling their own clicks exactly as they do with a mouse.
        /// </summary>
        private static Selectable EnsureNavTarget(GameObject go, CardClickHandler clickHandler)
        {
            if (go == null)
            {
                return null;
            }

            var existing = go.GetComponent<Selectable>();
            if (existing != null)
            {
                return existing;
            }

            bool hadClickHandler = go.GetComponent<IPointerClickHandler>() != null;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            var graphic = go.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }

            if (!hadClickHandler && clickHandler != null)
            {
                var forwarder = go.GetComponent<CardClickForwarder>() ?? go.AddComponent<CardClickForwarder>();
                forwarder.Target = clickHandler;
            }

            return button;
        }
    }
}

