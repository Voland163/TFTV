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
using TFTV.TFTVUI.Common;
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

        /// <summary>
        /// Adds the helmet toggle to whichever navigation holder already owns the loadout button column,
        /// rather than registering a competing one - so it inherits the edit screen's own priority and
        /// focus handling.
        ///
        /// It goes in immediately after Save Loadout, which is where it renders - last in the column. The
        /// holder links its elements in list order, so putting it anywhere else rewires the buttons around
        /// it and breaks their navigation too.
        ///
        /// While the button is hidden its wrapper is inactive, and the navigation code skips inactive
        /// elements on its own - so there is nothing to remove when it does not apply.
        /// </summary>
        private static void EnsureHelmetButtonNavigation(UIModuleActorCycle uIModuleActorCycle)
        {
            try
            {
                Selectable helmetSelectable = HelmetToggle != null
                    ? (HelmetToggle.BaseButton ?? HelmetToggle.GetComponent<Selectable>())
                    : null;

                if (helmetSelectable == null)
                {
                    return;
                }

                EditUnitButtonsController buttons =
                    uIModuleActorCycle != null
                        ? uIModuleActorCycle.GetComponentInChildren<EditUnitButtonsController>(true)
                        : null;

                Selectable anchor = buttons?.SaveLoadoutButton != null
                    ? (buttons.SaveLoadoutButton.BaseButton ?? buttons.SaveLoadoutButton.GetComponent<Selectable>())
                    : null;

                if (anchor == null)
                {
                    return;
                }

                UINavigationalElementsHolder holder = ControllerNav.FindHolderContaining(anchor);
                if (holder != null)
                {
                    ControllerNav.InsertIntoExistingHolder(holder, helmetSelectable, anchor);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
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

                        EnsureHelmetButtonNavigation(uIModuleActorCycle);

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

                    Transform toggleLoadoutParent = editUnitButtonsController.UnequipAllButton.transform.parent;
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