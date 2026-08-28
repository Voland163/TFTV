using Base.Core;
using Base.Input;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using TFTV.TFTVUI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Geoscape
{
    /// <summary>
    /// Gamepad access to the row of square buttons at the edge of the geoscape - vanilla's Bases button
    /// plus the mod's Haven Recruits and Marketplace clones.
    ///
    /// Vanilla binds Y straight to opening the Bases panel, which leaves the two mod buttons with no
    /// controller route at all. Here Y instead focuses the row so all three can be picked, and the Bases
    /// panel opens from its own button as it always did.
    ///
    /// The geoscape map runs with <see cref="UIGlobalNavigationController.SupressInputEvents"/> on, which
    /// disables holder navigation entirely so the free cursor can roam the map. Vanilla clears that flag
    /// just before it opens a nav-driven panel and restores it in ResetGamepadCursor; this mirrors that,
    /// and the flag has to stay cleared while a mod overlay opened from one of these buttons is up, or the
    /// overlay's own navigation is dead on arrival.
    /// </summary>
    internal static class SiteManagementNav
    {
        private const string HolderName = "TFTV_SiteManagementButtonsNav";
        private const string RecruitsButtonName = "UIButton_Icon_Recruits";
        private const string MarketplaceButtonName = "UIButton_Icon_Marketplace";

        private static UIModuleSiteManagement _module;
        private static UINavigationalElementsHolder _holder;
        private static bool _focused;

        internal static bool IsFocused => _focused;

        [HarmonyPatch(typeof(UIModuleSiteManagement), nameof(UIModuleSiteManagement.Init))]
        internal static class UIModuleSiteManagement_Init_ButtonGroupNav_patch
        {
            public static void Postfix(UIModuleSiteManagement __instance)
            {
                try
                {
                    // Init runs after both mod buttons have been cloned in Awake, so the row is complete.
                    _module = __instance;
                    _focused = false;
                    _holder = EnsureHolder(__instance);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteManagement), nameof(UIModuleSiteManagement.Deinit))]
        internal static class UIModuleSiteManagement_Deinit_ButtonGroupNav_patch
        {
            public static void Postfix()
            {
                try
                {
                    ReleaseFocus(restoreSuppression: false);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        /// <summary>
        /// Intercepts Y before vanilla expands the Bases panel, so it focuses the button row instead.
        /// Everything else - including Y while the panel is already open, which still closes it - falls
        /// through to the original.
        ///
        /// Both map states carry their own copy of the Y branch and both drive the same module, so both
        /// need intercepting: the geoscape sits in UIStateVehicleSelected whenever an aircraft or site is
        /// selected, which is most of the time.
        /// </summary>
        [HarmonyPatch(typeof(UIStateNothingSelected), "OnInputEvent")]
        internal static class UIStateNothingSelected_OnInputEvent_ButtonGroupNav_patch
        {
            public static bool Prefix(InputEvent ev, ref bool __result)
            {
                return !TryHandleYButton(ev, ref __result);
            }
        }

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnInputEvent")]
        internal static class UIStateVehicleSelected_OnInputEvent_ButtonGroupNav_patch
        {
            public static bool Prefix(InputEvent ev, ref bool __result)
            {
                return !TryHandleYButton(ev, ref __result);
            }
        }

        /// <summary>Returns true when the event was consumed and the original must be skipped.</summary>
        private static bool TryHandleYButton(InputEvent ev, ref bool __result)
        {
            try
            {
                if (ev.Type != InputEventType.Pressed || ev.Name != "Joystick Geoscape YButton")
                {
                    return false;
                }

                if (_module == null || _module.IsModuleExtended)
                {
                    return false;
                }

                if (_focused)
                {
                    ReleaseFocus(restoreSuppression: true);
                }
                else
                {
                    FocusButtons();
                }

                __result = true;
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Cancel on the map already restores the suppression flag in vanilla; the holder has to come off
        /// with it, otherwise it keeps holding navigation focus over the map.
        /// </summary>
        [HarmonyPatch(typeof(UIStateNothingSelected), "OnCancel")]
        internal static class UIStateNothingSelected_OnCancel_ButtonGroupNav_patch
        {
            public static void Prefix()
            {
                ReleaseOnCancel();
            }
        }

        [HarmonyPatch(typeof(UIStateVehicleSelected), "OnCancel")]
        internal static class UIStateVehicleSelected_OnCancel_ButtonGroupNav_patch
        {
            public static void Prefix()
            {
                ReleaseOnCancel();
            }
        }

        private static void ReleaseOnCancel()
        {
            try
            {
                if (_focused)
                {
                    ReleaseFocus(restoreSuppression: true);
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Opening the Bases panel hands navigation over to that panel's own holder, so the button row
        /// steps aside - but the suppression flag stays cleared, because the panel needs it.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleSiteManagement), nameof(UIModuleSiteManagement.ShowModule))]
        internal static class UIModuleSiteManagement_ShowModule_ButtonGroupNav_patch
        {
            public static void Postfix()
            {
                try
                {
                    ReleaseFocus(restoreSuppression: false);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }
        }

        private static UINavigationalElementsHolder EnsureHolder(UIModuleSiteManagement module)
        {
            Transform parent = module?.OpenModuleButton?.transform?.parent;
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(HolderName);
            if (existing != null)
            {
                return existing.GetComponent<UINavigationalElementsHolder>();
            }

            // A dedicated empty child rather than the buttons' own parent: the holder only ever navigates
            // an explicit element list, so it needs no children of its own, and this avoids disturbing the
            // live buttons when the component is attached.
            GameObject go = new GameObject(HolderName, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            UINavigationalElementsHolder holder = ControllerNav.EnsureHolder(go);
            go.SetActive(true);
            return holder;
        }

        /// <summary>
        /// Collects whichever of the three buttons are currently visible, left to right.
        /// </summary>
        private static List<Selectable> CollectButtons()
        {
            List<Selectable> buttons = new List<Selectable>();

            if (_module?.OpenModuleButton == null)
            {
                return buttons;
            }

            Transform parent = _module.OpenModuleButton.transform.parent;

            AddButton(buttons, _module.OpenModuleButton.gameObject);
            AddButton(buttons, parent?.Find(RecruitsButtonName)?.gameObject);
            AddButton(buttons, parent?.Find(MarketplaceButtonName)?.gameObject);

            buttons.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            return buttons;
        }

        private static void AddButton(List<Selectable> into, GameObject go)
        {
            if (go == null || !go.activeInHierarchy)
            {
                return;
            }

            Selectable selectable = go.GetComponent<PhoenixGeneralButton>()?.BaseButton
                                    ?? go.GetComponent<Selectable>();

            if (selectable != null)
            {
                into.Add(selectable);
            }
        }

        private static void FocusButtons()
        {
            List<Selectable> buttons = CollectButtons();

            if (buttons.Count == 0)
            {
                return;
            }

            UIGlobalNavigationController globalNavigation = GetGlobalNavigation();
            if (globalNavigation != null)
            {
                // Must precede Apply: while this is set, Add() refuses to make any holder current.
                globalNavigation.SupressInputEvents = false;
            }

            // Loops horizontally so the row cycles; there is nowhere else to navigate to from here, and
            // the row is left with Y or Cancel rather than by running off an end.
            bool applied = ControllerNav.Apply(_holder, buttons, NavigationHolderMode.Horizontal, rootPriority: 90);

            // Ranked below the panels these buttons open, so Add() will not hand it focus on its own.
            _focused = applied && ControllerNav.Focus(_holder);
        }

        private static void ReleaseFocus(bool restoreSuppression)
        {
            ControllerNav.Release(_holder);
            _focused = false;

            if (!restoreSuppression)
            {
                return;
            }

            UIGlobalNavigationController globalNavigation = GetGlobalNavigation();
            if (globalNavigation != null)
            {
                globalNavigation.SupressInputEvents = true;
            }

            if (ControllerNav.IsUsingController())
            {
                GameUtl.GetFreeCursorController()?.ForceCursorToCenterScreen();
            }
        }

        private static UIGlobalNavigationController GetGlobalNavigation()
        {
            return GameUtl.CurrentLevel()?.GetComponent<UIGlobalNavigationController>();
        }
    }
}
