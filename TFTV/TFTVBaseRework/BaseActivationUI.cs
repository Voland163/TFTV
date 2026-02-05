using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVBaseRework
{
    internal partial class BaseActivation
    {
        private const string ExtraButtonsRootName = "TFTV_BaseActivation_ExtraButtonsRoot";
        private const string ButtonCostRowName = "TFTV_BaseActivation_CostRow";
        private const string ButtonGainRowName = "TFTV_BaseActivation_GainRow";
        private const string ButtonContentRootName = "TFTV_BaseActivation_ButtonContent";
        private const string LootSlotsRootName = "TFTV_BaseActivation_LootSlotsRoot";
        private const int LootSlotSize = HavenRecruitsDetailsPanel.DetailInventorySlotSize * 2;
        private static readonly Color UnavailableResourceColor = new Color(1f, 0f, 0f, 1f);
        private static Sprite _personnelPlaceholderIcon;
        private static readonly Dictionary<int, int> ModalHandledFrameByInstanceId = new Dictionary<int, int>();
        private const float ButtonsRootOffsetX = 650f;
        private const float ButtonsRootOffsetY = -350f;
        private const float ActionButtonWidth = 820f;
        private const float ActionButtonHeight = 200f;
        private const float CostIconSize = 50f;
        private const float CostRowMinHeight = 70f;
        private const float CostEntryMinHeight = 60f;

        [HarmonyPatch(typeof(GeoAbility), "GetTargetDisabledState")]
        internal static class GeoAbility_GetTargetDisabledState_ActivateBaseIgnoreResourceGate_patch
        {
            public static void Postfix(GeoAbility __instance, GeoAbilityTarget target, ref GeoAbilityTargetDisabledState __result)
            {
                try
                {
                    if (!BaseReworkUtils.BaseReworkEnabled)
                    {
                        return;
                    }

                    if (__instance is ActivateBaseAbility && __result == GeoAbilityTargetDisabledState.NotEnoughResources)
                    {
                        __result = GeoAbilityTargetDisabledState.NotDisabled;
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(ActivateBaseAbilityView), "PayResourcementCost")]
        internal static class ActivateBaseAbilityView_PayResourcementCost_patch
        {
            public static bool Prefix(ActivateBaseAbilityView __instance, ModalResult result)
            {
                try
                {
                    if (!BaseReworkUtils.BaseReworkEnabled)
                    {
                        return true;
                    }

                    if (result != ModalResult.Confirm)
                    {
                        return false;
                    }

                    GeoAbilityTarget target = Traverse.Create(__instance).Field("_target").GetValue<GeoAbilityTarget>();
                    if (target.Actor is GeoSite site && target.Faction is GeoPhoenixFaction faction)
                    {
                        bool fromOutpost = site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);
                        PhoenixBaseVisitFlow.TryQueueFullBaseFromActivationUI(site, faction, fromOutpost);
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return false;
                }
            }
        }

        [HarmonyPatch(typeof(PXBaseActivationDataBind), "ModalShowHandler", new Type[] { typeof(UIModal) })]
        internal static class PXBaseActivationDataBind_ModalShowHandler_CustomPanel_patch
        {
            public static void Postfix(PXBaseActivationDataBind __instance, UIModal modal, PhoenixBaseActivationData ____data)
            {
                try
                {
                    if (!BaseReworkUtils.BaseReworkEnabled || __instance == null || modal == null)
                    {
                        return;
                    }

                    GeoSite site = ____data?.PhoenixBase?.Site;
                  
                    if (site == null)
                    {
                        return;
                    }

                    if (!TryBeginModalOnce(__instance))
                    {
                        return;
                    }                   

                    GeoPhoenixFaction faction = site.GeoLevel.PhoenixFaction;

                    bool hasVehicle = PhoenixBaseVisitFlow.HasPhoenixVehicleAtSite(site, faction);
                    bool isOutpost = site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag);

                    bool isLooted = site.SiteTags.Contains(PhoenixBaseReworkState.LootedTag);
                    BaseInitialLoot.LootUiResult lootAwarded = BaseInitialLoot.TryGiveFirstVisitLootOnUI(site, faction, hasVehicle);

                    TFTVLogger.Always($"[PXBaseActivationDataBind_ModalShowHandler_CustomPanel_patch] Site: {site?.LocalizedSiteName}, HasVehicle: {hasVehicle}, lootAwarded: {lootAwarded?.Text} ");

                    HideVanillaActivationCostBlock(__instance);

                    Text activationCostLabel = FindActivationCostLabel(__instance);
                    if (activationCostLabel != null)
                    {
                        if (isLooted)
                        {
                            SetActivationLootDisplay(activationCostLabel, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_ALREADY_LOOTED"), null);
                        }
                        else if (!hasVehicle)
                        {
                            SetActivationLootDisplay(activationCostLabel, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_VISIT_TO_LOOT"), null);
                        }
                        else if (lootAwarded != null && lootAwarded.HasItems)
                        {
                            SetActivationLootDisplay(activationCostLabel, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_LOOT_YOU_FOUND"), lootAwarded.Items);
                        }
                        else if (!string.IsNullOrEmpty(lootAwarded?.Text))
                        {
                            SetActivationLootDisplay(activationCostLabel,
                                $"{TFTVCommonMethods.ConvertKeyToString("KEY_BASE_LOOT_YOU_FOUND")} {lootAwarded.Text}", null);
                        }
                        else
                        {
                            string preview = BaseInitialLoot.GetOrCreateFirstVisitPreviewText(site, faction);
                            SetActivationLootDisplay(
                                activationCostLabel,
                                string.IsNullOrEmpty(preview)
                                    ? TFTVCommonMethods.ConvertKeyToString("KEY_BASE_VISIT_TO_LOOT")
                                    : $"{TFTVCommonMethods.ConvertKeyToString("KEY_BASE_LOOT_YOU_FOUND")} {preview}",
                                null);
                        }

                        TFTVLogger.Always($"[PXBaseActivationDataBind_ModalShowHandler_CustomPanel_patch] ActivationCostLabel set to: {activationCostLabel.text}");
                    }

                    if (__instance.Confirm != null)
                    {
                        __instance.Confirm.BaseButton.onClick = new Button.ButtonClickedEvent();
                    }

                    Transform root = EnsureButtonsRoot(__instance);

                    ClearChildren(root);

                    PhoenixGeneralButton scanBtn = CreateClonedActionButton(
                        __instance.Confirm,
                        root,
                        TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PING_SCAN_OPTION"),
                        BaseLowQualityScanning.CanAffordLowQualityScan(faction),
                        () => ExecuteAndCloseOnSuccess(modal, () => BaseLowQualityScanning.TryStartLowQualityScan(site, faction)));
                    ApplyCostRow(scanBtn, faction, BaseLowQualityScanning.GetScanCostPack(), 0);
                    SetButtonTooltip(scanBtn, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_PING_SCAN_TOOLTIP"));

                    PhoenixGeneralButton ransackBtn = CreateClonedActionButton(
                        __instance.Confirm,
                        root,
                        TFTVCommonMethods.ConvertKeyToString("KEY_BASE_RANSACK_OPTION"),
                        hasVehicle,
                        () => ExecuteAndCloseOnSuccess(modal, () => PhoenixBaseVisitFlow.TryRansackFromActivationUI(site, faction)));
                    if (!ApplyGainRow(ransackBtn, site))
                    {
                        SetButtonLabel(ransackBtn.gameObject,
                            $"{TFTVCommonMethods.ConvertKeyToString("KEY_BASE_RANSACK_OPTION")}   {BaseRansack.GetRansackPreviewText(site)}");
                    }
                    SetButtonTooltip(ransackBtn, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_RANSACK_TOOLTIP"));

                    if (!isOutpost)
                    {
                        PhoenixGeneralButton outpostBtn = CreateClonedActionButton(
                           __instance.Confirm,
                           root,
                           TFTVCommonMethods.ConvertKeyToString("KEY_BASE_OUTPOST_OPTION"),
                           hasVehicle && PhoenixBaseVisitFlow.CanAffordOutpost(faction),
                           () => ExecuteAndCloseOnSuccess(modal, () => PhoenixBaseVisitFlow.TrySetOutpostFromActivationUI(site, faction)));
                        ApplyCostRow(outpostBtn, faction, PhoenixBaseVisitFlow.GetOutpostCostPack(), PhoenixBaseVisitFlow.GetOutpostPersonnelCost());
                        SetButtonTooltip(outpostBtn, TFTVCommonMethods.ConvertKeyToString("KEY_BASE_OUTPOST_TOOLTIP"));
                    }

                    bool fromOutpost = isOutpost;
                    string activateLabel = fromOutpost
                        ? TFTVCommonMethods.ConvertKeyToString("KEY_BASE_OUTPOST_UPGRADE_OPTION")
                        : TFTVCommonMethods.ConvertKeyToString("KEY_BASE_ACTIVATE_OPTION");
                    PhoenixGeneralButton activateBaseBtn = CreateClonedActionButton(
                       __instance.Confirm,
                       root,
                        activateLabel,
                        hasVehicle && PhoenixBaseVisitFlow.CanAffordBaseQueue(faction, fromOutpost),
                       () => ExecuteAndCloseOnSuccess(modal, () => PhoenixBaseVisitFlow.TryQueueFullBaseFromActivationUI(site, faction, fromOutpost)));
                    ApplyCostRow(activateBaseBtn, faction, PhoenixBaseVisitFlow.GetBaseQueueCostPack(fromOutpost), PhoenixBaseVisitFlow.GetBaseQueuePersonnelCost(fromOutpost));
                    SetButtonTooltip(activateBaseBtn, fromOutpost
                        ? TFTVCommonMethods.ConvertKeyToString("KEY_BASE_OUTPOST_UPGRADE_TOOLTIP")
                        : TFTVCommonMethods.ConvertKeyToString("KEY_BASE_ACTIVATE_OPTION_TOOLTIP"));

                    __instance.Confirm.gameObject.SetActive(false);

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            private static void SetActivationLootDisplay(Text label, string text, IList<ItemDef> items)
            {
                try
                {
                    if (label == null)
                    {
                        return;
                    }

                    label.text = text ?? string.Empty;
                    UpdateLootSlots(label, items);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void UpdateLootSlots(Text label, IList<ItemDef> items)
            {
                try
                {
                    Transform root = EnsureLootSlotsRoot(label);
                    if (root == null)
                    {
                        return;
                    }

                    TFTV.RecruitOverlayManagerHelpers.ClearTransformChildren(root);

                    bool hasItems = items != null && items.Count > 0;
                    root.gameObject.SetActive(hasItems);

                    if (!hasItems)
                    {
                        return;
                    }

                    UIGeoItemTooltip tooltip = BaseInitialLootTooltip.EnsureLootItemTooltip(label.transform.parent);
                    int created = 0;
                    foreach (ItemDef item in items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        GameObject wrapper = new GameObject("LootSlotWrapper", typeof(RectTransform), typeof(LayoutElement));
                        wrapper.transform.SetParent(root, false);

                        RectTransform wrapperRect = wrapper.GetComponent<RectTransform>();
                        wrapperRect.sizeDelta = new Vector2(LootSlotSize, LootSlotSize);

                        LayoutElement wrapperLayout = wrapper.GetComponent<LayoutElement>();
                        wrapperLayout.minWidth = LootSlotSize;
                        wrapperLayout.preferredWidth = LootSlotSize;
                        wrapperLayout.minHeight = LootSlotSize;
                        wrapperLayout.preferredHeight = LootSlotSize;
                        wrapperLayout.flexibleWidth = 0f;
                        wrapperLayout.flexibleHeight = 0f;

                        UIInventorySlot slot = TFTV.RecruitOverlayManagerHelpers.MakeInventorySlot(wrapper.transform, item, LootSlotSize, "LootSlot", tooltip);
                        if (slot == null)
                        {
                            UnityEngine.Object.Destroy(wrapper);
                            continue;
                        }

                        RectTransform slotRect = slot.transform as RectTransform;
                        if (slotRect != null)
                        {
                            slotRect.anchorMin = Vector2.zero;
                            slotRect.anchorMax = Vector2.one;
                            slotRect.offsetMin = Vector2.zero;
                            slotRect.offsetMax = Vector2.zero;
                        }

                        created++;
                    }

                    HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
                    float spacing = layout != null ? layout.spacing : 0f;
                    float width = created > 0 ? (created * LootSlotSize) + ((created - 1) * spacing) : LootSlotSize;

                    LayoutElement rootLayout = root.GetComponent<LayoutElement>() ?? root.gameObject.AddComponent<LayoutElement>();
                    rootLayout.minWidth = width;
                    rootLayout.preferredWidth = width;
                    rootLayout.flexibleWidth = 0f;

                    RectTransform rootRect = root as RectTransform;
                    if (rootRect != null)
                    {
                        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LootSlotSize);
                    }

                    RectTransform parentRect = root.parent as RectTransform;
                    if (parentRect != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                    }

                    Canvas.ForceUpdateCanvases();
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

           
            private static Transform EnsureLootSlotsRoot(Text label)
            {
                try
                {
                    Transform parent = label?.transform?.parent;
                    if (parent == null)
                    {
                        return null;
                    }

                    Transform existing = parent.Find(LootSlotsRootName);
                    if (existing != null)
                    {
                        return existing;
                    }

                    GameObject go = new GameObject(LootSlotsRootName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                    go.transform.SetParent(parent, false);

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(LootSlotSize, LootSlotSize);

                    HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.spacing = 8f;
                    layout.childControlHeight = true;
                    layout.childControlWidth = true;
                    layout.childForceExpandHeight = false;
                    layout.childForceExpandWidth = false;

                    ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    LayoutElement element = go.GetComponent<LayoutElement>();
                    element.minHeight = LootSlotSize;
                    element.preferredHeight = LootSlotSize;

                    go.SetActive(false);
                    return go.transform;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static void SetButtonTooltip(PhoenixGeneralButton btn, string text)
            {
                try
                {
                    if (btn == null)
                    {
                        return;
                    }

                    UITooltipText tooltip = btn.GetComponent<UITooltipText>() ?? btn.gameObject.AddComponent<UITooltipText>();
                    tooltip.TipText = text ?? string.Empty;
                    if (tooltip.TipKey != null)
                    {
                        tooltip.TipKey.LocalizationKey = string.Empty;
                    }
                    tooltip.enabled = true;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void HideVanillaActivationCostBlock(PXBaseActivationDataBind bind)
            {
                try
                {
                    if (bind == null)
                    {
                        return;
                    }

                    if (bind.TechResource != null) bind.TechResource.SetActive(false);
                    if (bind.MaterialResource != null) bind.MaterialResource.SetActive(false);
                    if (bind.FoodResource != null) bind.FoodResource.SetActive(false);

                    Transform warningRoot = bind.InsufficientResources != null
                        ? bind.InsufficientResources.transform.parent
                        : null;

                    Transform[] roots =
                    {
                    bind.TechResource?.transform?.parent,
                    bind.MaterialResource?.transform?.parent,
                    bind.FoodResource?.transform?.parent
                };

                    foreach (Transform root in roots.Where(t => t != null).Distinct())
                    {
                        if (warningRoot != null && (root == warningRoot || warningRoot.IsChildOf(root)))
                        {
                            continue;
                        }

                        root.gameObject.SetActive(false);
                    }

                    bind.InsufficientResources?.gameObject.SetActive(false);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void ExecuteAndCloseOnSuccess(UIModal modal, Func<bool> action)
            {
                try
                {
                    bool ok = false;
                    try
                    {
                        ok = action != null && action();
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                    }

                    if (ok)
                    {
                        modal.Cancel();
                    }
                    else
                    {
                        GameUtl.GetMessageBox().ShowSimplePrompt(
                            "Cannot start this action. Check resources, personnel, and site state.",
                            MessageBoxIcon.Warning,
                            MessageBoxButtons.OK,
                            null);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

            private static Transform EnsureButtonsRoot(PXBaseActivationDataBind bind)
            {
                try
                {
                    Transform parent = bind.Confirm?.transform?.parent;
                    if (parent == null)
                    {
                        return null;
                    }

                    Transform root = parent.Find(ExtraButtonsRootName);
                    if (root != null)
                    {
                        return root;
                    }

                    GameObject go = new GameObject(ExtraButtonsRootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                    go.transform.SetParent(parent, false);

                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(ButtonsRootOffsetX, ButtonsRootOffsetY);

                    VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
                    vlg.spacing = 8f;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;

                    ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    return go.transform;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }


            private static PhoenixGeneralButton CreateClonedActionButton(PhoenixGeneralButton template, Transform root, string label, bool interactable, Action onClick)
            {
                try
                {
                    PhoenixGeneralButton btn = UnityEngine.Object.Instantiate(template, root);
                    btn.gameObject.SetActive(true);
                    btn.ResetButtonAnimations();
                    btn.SetInteractable(interactable);
                    SetButtonLabel(btn.gameObject, label);
                    RebindButtonCompletely(btn, onClick);
                    EnsureButtonSize(btn.gameObject);
                    return btn;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    return null;
                }
            }

            private static void EnsureButtonSize(GameObject buttonObject)
            {
                try
                {
                    if (buttonObject == null)
                    {
                        return;
                    }

                    LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
                    if (layout == null)
                    {
                        layout = buttonObject.AddComponent<LayoutElement>();
                    }

                    layout.minWidth = ActionButtonWidth;
                    layout.preferredWidth = ActionButtonWidth;
                    layout.minHeight = ActionButtonHeight;
                    layout.preferredHeight = ActionButtonHeight;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void RebindButtonCompletely(PhoenixGeneralButton btn, Action onClick)
            {
                try
                {
                    if (btn?.BaseButton == null)
                    {
                        return;
                    }

                    btn.BaseButton.onClick = new Button.ButtonClickedEvent();
                    btn.BaseButton.onClick.AddListener(() => onClick?.Invoke());
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

           
            private static void SetButtonLabel(GameObject go, string text)
            {
                try
                {
                    Text label = go?.GetComponentsInChildren<Text>(true)
                        .OrderByDescending(t => t != null ? t.fontSize : 0)
                        .FirstOrDefault();
                    if (label != null)
                    {
                        label.text = text;
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static void ClearChildren(Transform root)
            {
                try
                {
                    if (root == null)
                    {
                        return;
                    }

                    for (int i = root.childCount - 1; i >= 0; i--)
                    {
                        UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
                    }
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static Text FindActivationCostLabel(PXBaseActivationDataBind bind)
            {
                try
                {
                    if (bind?.InsufficientResources == null)
                    {
                        return null;
                    }

                    Transform parent = bind.InsufficientResources.transform.parent;
                    if (parent == null)
                    {
                        return null;
                    }

                    Text[] texts = parent.GetComponentsInChildren<Text>(true);

                    return texts.FirstOrDefault(t =>
                        t != null && t.name.Equals("title"));
                }
                catch (Exception ex) { TFTVLogger.Error(ex); return null; }
            }

            private static bool TryBeginModalOnce(PXBaseActivationDataBind bind)
            {
                try
                {
                    int instanceId = bind.GetInstanceID();
                    int frame = Time.frameCount;

                    if (ModalHandledFrameByInstanceId.TryGetValue(instanceId, out int lastFrame) && lastFrame == frame)
                    {
                        return false;
                    }

                    ModalHandledFrameByInstanceId[instanceId] = frame;
                    return true;
                }
                catch (Exception ex) { TFTVLogger.Error(ex); return false; }
            }
        }

        private static void ApplyCostRow(PhoenixGeneralButton btn, GeoPhoenixFaction faction, ResourcePack cost, int personnelRequired)
        {
            try
            {
                if (btn == null)
                {
                    TFTVLogger.Always("[ApplyCostRow] btn is null");
                    return;
                }

                Text label = btn.GetComponentInChildren<Text>(true);
                if (label == null || label.transform == null)
                {
                    TFTVLogger.Always($"[ApplyCostRow] label missing for btn={btn?.name}, label={label}");
                    return;
                }

                Transform parent = GetOrCreateButtonContentRoot(btn, label);
                if (!parent)
                {
                    TFTVLogger.Always($"[ApplyCostRow] content root missing for btn={btn?.name}, label={label?.name}");
                    return;
                }

                EnsureButtonCostLayout(parent);
                EnsureLabelLayout(label);

                Transform existing = parent.Find(ButtonCostRowName);
                if (existing != null)
                {
                    UnityEngine.Object.Destroy(existing.gameObject);
                }

                GameObject row = new GameObject(ButtonCostRowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(parent, false);
                EnsureRowLayout(row);

                HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 6f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;

                AddResourceCostEntry(row.transform, label, faction, cost, ResourceType.Materials);
                AddResourceCostEntry(row.transform, label, faction, cost, ResourceType.Tech);
                AddPersonnelCostEntry(row.transform, label, faction, personnelRequired);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static bool ApplyGainRow(PhoenixGeneralButton btn, GeoSite site)
        {
            try
            {
                if (btn == null || site == null)
                {
                    return false;
                }

                Text label = btn.GetComponentInChildren<Text>(true);
                if (label == null || label.transform == null)
                {
                    return false;
                }

                Transform parent = GetOrCreateButtonContentRoot(btn, label);
                if (!parent)
                {
                    return false;
                }


                EnsureButtonCostLayout(parent);
                EnsureLabelLayout(label);

                Transform existing = parent.Find(ButtonGainRowName);
                if (existing != null)
                {
                    UnityEngine.Object.Destroy(existing.gameObject);
                }

                GameObject row = new GameObject(ButtonGainRowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(parent, false);
                EnsureRowLayout(row);

                HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 10f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;

                BaseRansack.TryGetRansackDemolitionValue(site, out int mats, out int tech);

                AddGainLabel(row.transform, label, "+");
                AddGainEntry(row.transform, label, ResourceType.Materials, $"{mats}");
                AddGainEntry(row.transform, label, ResourceType.Tech, $"{tech}");

                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static void EnsureButtonCostLayout(Transform parent)
        {
            try
            {
                if (!parent)
                {
                   // TFTVLogger.Always("[EnsureButtonCostLayout] parent destroyed");
                    return;
                }

             //   TFTVLogger.Always($"[EnsureButtonCostLayout] parent ok name={parent.name}, activeInHierarchy={parent.gameObject.activeInHierarchy}");

                GameObject parentGO = parent.gameObject;
                if (parentGO == null)
                {
                   // TFTVLogger.Always("[EnsureButtonCostLayout] parent.gameObject is null");
                    return;
                }

            //    TFTVLogger.Always($"[EnsureButtonCostLayout] parentGO ok name={parentGO.name}");

                VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                  //  TFTVLogger.Always("[EnsureButtonCostLayout] adding VerticalLayoutGroup");
                    layout = parentGO.AddComponent<VerticalLayoutGroup>();
                    if (layout == null)
                    {
                      //  TFTVLogger.Always("[EnsureButtonCostLayout] AddComponent returned null");
                        return;
                    }

                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.spacing = 2f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }
                else
                {
                   // TFTVLogger.Always("[EnsureButtonCostLayout] VerticalLayoutGroup already exists");
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private static void AddResourceCostEntry(Transform parent, Text label, GeoPhoenixFaction faction, ResourcePack cost, ResourceType type)
        {
            try
            {
                if (parent == null || label == null || cost == null)
                {
                    return;
                }

                ResourceUnit unit = cost.ByResourceType(type);
                if (unit == null || unit.Value <= 0)
                {
                    return;
                }

                bool hasResource = faction?.Wallet != null && faction.Wallet.HasResources(new ResourceUnit(type, unit.Value));
                Sprite icon;
                Color iconColor;
                if (!TryGetResourceVisual(type, out icon, out iconColor))
                {
                    icon = GetPersonnelPlaceholderIcon();
                    iconColor = Color.white;
                }

                AddCostEntry(parent, label, icon, hasResource ? iconColor : UnavailableResourceColor,
                    unit.RoundedValue.ToString(CultureInfo.InvariantCulture),
                    hasResource ? label.color : UnavailableResourceColor);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static void AddPersonnelCostEntry(Transform parent, Text label, GeoPhoenixFaction faction, int requiredPersonnel)
        {
            try
            {

                if (parent == null || label == null || requiredPersonnel <= 0)
                {
                    return;
                }

                int availablePersonnel = PersonnelData.GetAvailablePersonnelCount(faction);
                bool hasPersonnel = availablePersonnel >= requiredPersonnel;
                Sprite icon = GetPersonnelPlaceholderIcon();

                AddCostEntry(parent, label, icon, hasPersonnel ? Color.white : UnavailableResourceColor,
                    requiredPersonnel.ToString(CultureInfo.InvariantCulture),
                    hasPersonnel ? label.color : UnavailableResourceColor);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static void AddCostEntry(Transform parent, Text label, Sprite icon, Color iconColor, string amount, Color amountColor)
        {
            try
            {
                GameObject entry = new GameObject("CostEntry", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                entry.transform.SetParent(parent, false);

                LayoutElement entryLayoutElement = entry.AddComponent<LayoutElement>();
                entryLayoutElement.minHeight = CostEntryMinHeight;
                entryLayoutElement.preferredHeight = CostEntryMinHeight;

                HorizontalLayoutGroup entryLayout = entry.GetComponent<HorizontalLayoutGroup>();
                entryLayout.childAlignment = TextAnchor.MiddleCenter;
                entryLayout.spacing = 4f;
                entryLayout.childControlWidth = true;
                entryLayout.childControlHeight = true;
                entryLayout.childForceExpandWidth = false;
                entryLayout.childForceExpandHeight = false;

                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(entry.transform, false);
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.color = iconColor;
                iconImage.preserveAspect = true;

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(CostIconSize, CostIconSize);

                LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
                iconLayout.minWidth = CostIconSize;
                iconLayout.preferredWidth = CostIconSize;
                iconLayout.minHeight = CostIconSize;
                iconLayout.preferredHeight = CostIconSize;

                GameObject textObject = new GameObject("Amount", typeof(RectTransform));
                textObject.transform.SetParent(entry.transform, false);
                Text amountText = textObject.AddComponent<Text>();
                amountText.font = label.font;
                amountText.fontSize = Mathf.Max(22, label.fontSize - 4);
                amountText.alignment = TextAnchor.MiddleLeft;
                amountText.color = amountColor;
                amountText.text = amount;
                amountText.horizontalOverflow = HorizontalWrapMode.Overflow;

                LayoutElement textLayout = textObject.AddComponent<LayoutElement>();
                textLayout.minHeight = CostEntryMinHeight;
                textLayout.preferredHeight = CostEntryMinHeight;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }

        }

        private static void EnsureRowLayout(GameObject row)
        {
            try
            {
                if (row == null)
                {
                    return;
                }

                LayoutElement layout = row.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = row.AddComponent<LayoutElement>();
                }

                layout.minHeight = CostRowMinHeight;
                layout.preferredHeight = CostRowMinHeight;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static void EnsureLabelLayout(Text label)
        {
            try
            {
                if (label == null)
                {
                    return;
                }

                LayoutElement layout = label.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = label.gameObject.AddComponent<LayoutElement>();
                }

                float targetHeight = Mathf.Max(CostRowMinHeight, label.fontSize + 6f);
                layout.minHeight = targetHeight;
                layout.preferredHeight = targetHeight;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static bool TryGetResourceVisual(ResourceType type, out Sprite icon, out Color color)
        {
            try
            {
                icon = null;
                color = Color.white;

                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                UIModuleInfoBar infoBar = level?.View?.GeoscapeModules?.ResourcesModule;
                if (infoBar == null)
                {
                    return false;
                }

                ResourceIconContainer container = null;
                switch (type)
                {
                    case ResourceType.Materials:
                        container = infoBar.MaterialsController?.transform?.parent?.GetComponent<ResourceIconContainer>();
                        break;
                    case ResourceType.Tech:
                        container = infoBar.TechController?.transform?.parent?.GetComponent<ResourceIconContainer>();
                        break;
                    case ResourceType.Supplies:
                        container = infoBar.FoodController?.transform?.parent?.GetComponent<ResourceIconContainer>();
                        break;
                }

                if (container?.Icon == null)
                {
                    return false;
                }

                icon = container.Icon.sprite;
                color = container.Icon.color;
                return icon != null;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static Sprite GetPersonnelPlaceholderIcon()
        {
            try
            {

                if (_personnelPlaceholderIcon != null)
                {
                    return _personnelPlaceholderIcon;
                }

                _personnelPlaceholderIcon = Helper.CreateSpriteFromImageFile("personnel.png");
                return _personnelPlaceholderIcon;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }



        private static void AddGainLabel(Transform parent, Text label, string text)
        {
            try
            {
                if (parent == null || label == null)
                {
                    return;
                }

                GameObject textObject = new GameObject("GainLabel", typeof(RectTransform));
                textObject.transform.SetParent(parent, false);
                Text gainText = textObject.AddComponent<Text>();
                gainText.font = label.font;
                gainText.fontSize = Mathf.Max(50, label.fontSize - 4);
                gainText.alignment = TextAnchor.UpperLeft;
                gainText.color = label.color;
                gainText.text = text;
                gainText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static void AddGainEntry(Transform parent, Text label, ResourceType type, string amount)
        {
            try
            {
                if (parent == null || label == null)
                {
                    return;
                }

                Sprite icon;
                Color iconColor;
                if (!TryGetResourceVisual(type, out icon, out iconColor))
                {
                    icon = GetPersonnelPlaceholderIcon();
                    iconColor = Color.white;
                }

                AddCostEntry(parent, label, icon, iconColor, amount, label.color);
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }

        private static Transform GetOrCreateButtonContentRoot(PhoenixGeneralButton btn, Text label)
        {
            try
            {

                if (btn == null || label == null)
                {
                    return null;
                }

                Transform existing = btn.transform.Find(ButtonContentRootName);
                if (existing != null)
                {
                    return existing;
                }

                GameObject root = new GameObject(ButtonContentRootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                root.transform.SetParent(btn.transform, false);

                RectTransform rt = root.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.spacing = 2f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;

                ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                label.transform.SetParent(root.transform, false);
                label.transform.SetAsFirstSibling();

                return root.transform;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                throw;
            }
        }
    }
}