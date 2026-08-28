using Base.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Home.View.ViewControllers;
using PhoenixPoint.Home.View.ViewModules;
using System;
using System.Collections.Generic;
using TFTV.TFTVUI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Home
{
    /// <summary>
    /// Gamepad navigation for the rows TFTV adds to the new game settings screen.
    ///
    /// Every added row is either a cloned ModSettingController wrapping an
    /// <see cref="ArrowPickerController"/>, or a cloned <see cref="GameOptionViewController"/> acting as a
    /// collapsible section header. They all live as siblings in one container, in creation order, so a
    /// single vertical holder covers the lot.
    ///
    /// Two details make this work without extra bookkeeping. The global controller reads
    /// <c>GetComponent&lt;ArrowPickerController&gt;()</c> off whatever Selectable is currently selected and
    /// routes the right stick to it, so an option row's Selectable has to sit on the picker's own
    /// GameObject - then left and right change the value while up and down move between rows. And
    /// collapsing a section only deactivates its rows; the controller's neighbour search already walks
    /// past inactive elements, so a collapsed section is skipped with no rebuild.
    /// </summary>
    internal static class NewGameOptionsNav
    {
        private static UINavigationalElementsHolder _holder;

        /// <summary>
        /// Called at the end of the mod's own InitFullContent postfix rather than from a separate patch,
        /// so it is guaranteed to run after every row has been created.
        /// </summary>
        internal static void Setup(UIModuleGameSettings module, Transform optionsContainer)
        {
            try
            {
                if (optionsContainer == null)
                {
                    return;
                }

                _holder = ControllerNav.EnsureHolder(optionsContainer.gameObject);

                List<Selectable> rows = CollectRows(optionsContainer);
                if (rows.Count == 0)
                {
                    return;
                }

                VerticalScrollRectScroller scroller =
                    optionsContainer.GetComponentInParent<VerticalScrollRectScroller>() ?? module?.DlcScroller;

                // Never loops: the global controller only offers a section switch when a link comes back
                // null, so wrapping would trap the cursor in this list.
                ControllerNav.Apply(
                    _holder,
                    rows,
                    NavigationHolderMode.Vertical,
                    rootPriority: ControllerNav.DefaultRootPriority,
                    loop: false,
                    scrollController: scroller);
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
        }

        /// <summary>
        /// Walks the container in sibling order - which is the order the rows were created, and so the
        /// order they appear on screen - and returns one navigable Selectable per row.
        /// </summary>
        private static List<Selectable> CollectRows(Transform container)
        {
            List<Selectable> rows = new List<Selectable>();

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                GameOptionViewController header = child.GetComponent<GameOptionViewController>();
                if (header != null)
                {
                    Selectable headerSelectable = header.SelectButton != null
                        ? (header.SelectButton.BaseButton ?? header.SelectButton.GetComponent<Selectable>())
                        : child.GetComponent<Selectable>();

                    if (headerSelectable != null)
                    {
                        rows.Add(headerSelectable);
                    }
                    continue;
                }

                ModSettingController setting = child.GetComponent<ModSettingController>();
                if (setting?.ListField != null)
                {
                    Selectable pickerSelectable = EnsurePickerSelectable(setting.ListField);
                    if (pickerSelectable != null)
                    {
                        rows.Add(pickerSelectable);
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// The Selectable has to live on the picker's own GameObject, because that is where the global
        /// controller looks for the ArrowPickerController when it decides what the right stick adjusts.
        /// </summary>
        private static Selectable EnsurePickerSelectable(ArrowPickerController picker)
        {
            GameObject go = picker.gameObject;

            Selectable existing = go.GetComponent<Selectable>();
            if (existing != null)
            {
                return existing;
            }

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }

            return button;
        }
    }
}
