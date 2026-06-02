using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using I2.Loc;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Personnel
{
    internal class Loadouts
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        /// <summary>
        /// Patches to add toggle helmet button
        /// </summary>

        public static PhoenixGeneralButton HelmetToggle = null;
        private static Text _helmetToggleLabel = null;

        public static void UpdateHelmetButtonLabel()
        {
            if (_helmetToggleLabel == null)
            {
                return;
            }

            // HelmetsOff == true  → pressing will show helmets  → KEY_UI_EDIT_SCREEN_TOGGLEHELMET
            // HelmetsOff == false → pressing will hide face      → KEY_UI_EDIT_SCREEN_TOGGLEFACE
            string key = ShowWithoutHelmet.HelmetsOff
                ? "KEY_UI_EDIT_SCREEN_TOGGLEHELMET"
                : "KEY_UI_EDIT_SCREEN_TOGGLEFACE";

            _helmetToggleLabel.text = TFTVCommonMethods.ConvertKeyToString(key);
        }

        private static void ShadeMutationBionics(UIModuleActorCycle uIModuleActorCycle)
        {
            try
            {
                GeoCharacter geoCharacter = uIModuleActorCycle.CurrentCharacter;

                if (geoCharacter == null)
                {
                    return;
                }

                PhoenixGeneralButton mutationButton = uIModuleActorCycle.EditUnitButtonsController.MutationButton;
                PhoenixGeneralButton bionicsButton = uIModuleActorCycle.EditUnitButtonsController.BionicsButton;

                FieldInfo mutationAvailableFieldInfo = typeof(EditUnitButtonsController).GetField("_mutationAvailable", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo bionicsAvailableFieldInfo = typeof(EditUnitButtonsController).GetField("_bionicsAvailable", BindingFlags.NonPublic | BindingFlags.Instance);

                bool mutationAvailable = (bool)mutationAvailableFieldInfo.GetValue(uIModuleActorCycle.EditUnitButtonsController);
                bool bionicsAvailable = (bool)bionicsAvailableFieldInfo.GetValue(uIModuleActorCycle.EditUnitButtonsController);

                if (!mutationAvailable && !bionicsAvailable)
                {
                    return;
                }

                Text bionicsText = null;
                Text mutateText = null;

                if (bionicsAvailable)
                {
                    bionicsText = uIModuleActorCycle.EditUnitButtonsController.GetComponentsInChildren<Text>().FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_AUMGENTATION_ACTION"));

                    if (bionicsText != null)
                    {
                        bionicsButton.SetInteractable(true);
                        if (bionicsButton.gameObject.GetComponent<UITooltipText>() != null)
                        {
                            bionicsButton.gameObject.GetComponent<UITooltipText>().enabled = false;
                        }

                        bionicsText.color = new Color(0.820f, 0.859f, 0.914f);
                    }
                }

                if (mutationAvailable)
                {
                    mutateText = uIModuleActorCycle.EditUnitButtonsController.GetComponentsInChildren<Text>().FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_GEOSCAPE_MUTATE"));

                    if (mutateText != null)
                    {
                        mutationButton.SetInteractable(true);

                        if (mutationButton.gameObject.GetComponent<UITooltipText>() != null)
                        {
                            mutationButton.gameObject.GetComponent<UITooltipText>().enabled = false;
                        }

                        mutateText.color = new Color(0.820f, 0.859f, 0.914f);
                    }
                }

                TFTVConfig config = TFTVMain.Main.Config;

                if (geoCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag) && !config.MercsCanBeAugmented)
                {
                    if (mutateText != null)
                    {
                        mutationButton.SetInteractable(false);

                        if (mutationButton.gameObject.GetComponent<UITooltipText>() != null)
                        {
                            mutationButton.gameObject.GetComponent<UITooltipText>().enabled = true;
                        }
                        else
                        {
                            mutationButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_ABILITY_NOAUGMENTATONS");
                        }
                        mutateText.color = Color.gray;
                    }

                    if (bionicsText != null)
                    {
                        bionicsButton.SetInteractable(false);

                        if (bionicsButton.gameObject.GetComponent<UITooltipText>() != null)
                        {
                            bionicsButton.gameObject.GetComponent<UITooltipText>().enabled = true;
                        }
                        else
                        {
                            bionicsButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_ABILITY_NOAUGMENTATONS");
                        }
                        bionicsText.color = Color.gray;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void SetButtonVisibility(PhoenixGeneralButton button, bool visible)
        {
            if (button == null)
            {
                return;
            }

            // Use parent wrapper (contains both icon and label), consistent with vanilla SetCircularButtonVisibility
            button.transform.parent.gameObject.SetActive(visible);
            button.ResetButtonAnimations();
        }

        private static void HideHelmetButton()
        {
            SetButtonVisibility(HelmetToggle, false);
        }

        public static void ShowAndHideHelmetButton(UIModuleActorCycle uIModuleActorCycle)
        {
            try
            {
                if (uIModuleActorCycle == null || uIModuleActorCycle.CurrentUnit == null)
                {
                    HideHelmetButton();
                    return;
                }

                switch (uIModuleActorCycle.CurrentState)
                {
                    case UIModuleActorCycle.ActorCycleState.EditSoldierSection:
                        bool hasAugmentedHead = false;
                        ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");
                        foreach (GeoItem bionic in uIModuleActorCycle.CurrentCharacter?.ArmourItems ?? Enumerable.Empty<GeoItem>())
                        {
                            if ((bionic.CommonItemData.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)
                                || bionic.CommonItemData.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                            {
                                hasAugmentedHead = true;
                            }
                        }

                        SetButtonVisibility(HelmetToggle, !hasAugmentedHead);

                        if (!hasAugmentedHead)
                        {
                            // Refresh icon and label now that the stored preference is available
                            ShowWithoutHelmet.SyncCustomHelmetButtonIcon();
                        }

                        ShadeMutationBionics(uIModuleActorCycle);
                        break;

                    case UIModuleActorCycle.ActorCycleState.RosterSection:
                    case UIModuleActorCycle.ActorCycleState.EditVehicleSection:
                    case UIModuleActorCycle.ActorCycleState.EditMutogSection:
                    case UIModuleActorCycle.ActorCycleState.CapturedAlienSection:
                    case UIModuleActorCycle.ActorCycleState.RecruitSection:
                    case UIModuleActorCycle.ActorCycleState.Memorial:
                    case UIModuleActorCycle.ActorCycleState.SubmenuSection:
                    default:
                        HideHelmetButton();
                        break;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIStateRosterRecruits), "EnterState")]
        internal static class TFTV_UIStateRosterRecruits_EnterState_HelmetButton_Patch
        {
            private static void Postfix()
            {
                try
                {
                    HideHelmetButton();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleActorCycle), nameof(UIModuleActorCycle.SetContextButtonsBasedOnType))]
        internal static class TFTV_UIModuleActorCycle_SetContextButtonsBasedOnType_HelmetButton_Patch
        {
            private static void Postfix(UIModuleActorCycle __instance)
            {
                try
                {
                    ShowAndHideHelmetButton(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(EditUnitButtonsController), nameof(EditUnitButtonsController.Awake))]
        internal static class TFTV_EditUnitButtonsController_Awake_ToggleHelmetButton_patch
        {
            public static void Postfix(EditUnitButtonsController __instance)
            {
                try
                {
                    CreateHelmetButtonForUIEditScreen(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void CreateHelmetButtonForUIEditScreen(EditUnitButtonsController editUnitButtonsController)
        {
            try
            {
                if (HelmetToggle == null)
                {
                    Transform toggleLoadoutParent = editUnitButtonsController.ToggleLoadoutButton.transform.parent;
                    Transform saveLoadoutParent = editUnitButtonsController.SaveLoadoutButton.transform.parent;
                    Transform loadoutContainer = toggleLoadoutParent.parent;

                    Vector3 helmetLocalPos = toggleLoadoutParent.localPosition;
                    Vector3 saveLocalPos = saveLoadoutParent.localPosition;
                    Vector3 buttonLocalOffset = saveLocalPos - helmetLocalPos;

                    // Clone the ToggleLoadout wrapper under the same container
                    GameObject helmetWrapper = UnityEngine.Object.Instantiate(
                        toggleLoadoutParent.gameObject, loadoutContainer);
                    helmetWrapper.transform.localPosition = helmetLocalPos;

                    // Get the label Text and destroy any Localize component that would override our text
                    Text labelText = helmetWrapper.GetComponentInChildren<Text>();
                    if (labelText != null)
                    {
                        Localize loc = labelText.GetComponent<Localize>();
                        if (loc != null)
                        {
                            UnityEngine.Object.Destroy(loc);
                        }

                        _helmetToggleLabel = labelText;
                    }

                    // Get the cloned PhoenixGeneralButton and clear any copied handlers
                    PhoenixGeneralButton helmetToggleButton = helmetWrapper.GetComponentInChildren<PhoenixGeneralButton>();
                    helmetToggleButton.PointerClicked = null;
                    helmetToggleButton.PointerClicked += () => ShowWithoutHelmet.ToggleButtonClicked(helmetToggleButton);

                    // Set tooltip
                    UITooltipText existingTip = helmetToggleButton.gameObject.GetComponent<UITooltipText>();
                    if (existingTip != null)
                    {
                        existingTip.TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_TOGGLEHELMET_TIP");
                    }
                    else
                    {
                        helmetToggleButton.gameObject.AddComponent<UITooltipText>().TipText =
                            TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_TOGGLEHELMET_TIP");
                    }

                    // Shift ToggleLoadout down to SaveLoadout's original position
                    toggleLoadoutParent.localPosition = saveLocalPos;

                    // Shift SaveLoadout one step further down
                    saveLoadoutParent.localPosition = saveLocalPos + buttonLocalOffset;

                    HelmetToggle = helmetToggleButton;
                    ShowWithoutHelmet.SyncCustomHelmetButtonIcon();
                    UpdateHelmetButtonLabel();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}