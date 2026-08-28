using Base.Core;
using Base.Input;
using Base.UI;
using PhoenixPoint.Common.View.ViewControllers;
using System;
using System.Collections.Generic;
using TFTV.TFTVUI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal static partial class DrillsUI
    {
        /// <summary>
        /// Gamepad navigation for the drill selection overlay: the header's acquire button as one section,
        /// the 6-column drill grid as another.
        ///
        /// The overlay closes itself when the pointer leaves the panel (see DrillOverlayController's
        /// LateUpdate), which works in our favour - focusing the grid snaps the virtual cursor inside the
        /// panel, so it stays open, and every drill the cursor lands on fires its own hover tooltip with
        /// no extra wiring.
        /// </summary>
        private static class ControllerNavigation
        {
            internal static void Setup(
                GameObject overlay,
                RectTransform panelRect,
                RectTransform contentRect,
                RectTransform gridRect,
                DrillOverlayController controller)
            {
                try
                {
                    if (overlay == null || contentRect == null)
                    {
                        return;
                    }

                    List<Selectable> gridOptions = CollectGridOptions(gridRect);
                    List<Selectable> headerButtons = CollectHeaderButtons(contentRect, gridRect);

                    if (gridOptions.Count == 0 && headerButtons.Count == 0)
                    {
                        return;
                    }

                    VerticalScrollRectScroller scroller = panelRect != null
                        ? ControllerNav.EnsureVerticalScroller(panelRect.GetComponent<ScrollRect>())
                        : null;

                    UINavigationalElementsHolder headerHolder =
                        CreateHolderObject(overlay.transform, "TFTV_DrillsNav_Header");
                    UINavigationalElementsHolder gridHolder =
                        CreateHolderObject(overlay.transform, "TFTV_DrillsNav_Grid");

                    ControllerNav.Apply(
                        headerHolder,
                        headerButtons,
                        NavigationHolderMode.Horizontal,
                        rootPriority: 190,
                        scrollController: scroller);

                    // Grid mode with the same column count the GridLayoutGroup uses, so the built links
                    // match what is on screen. Never loops, so running off an edge hands over to the
                    // header section instead of wrapping.
                    bool gridApplied = ControllerNav.Apply(
                        gridHolder,
                        gridOptions,
                        NavigationHolderMode.Grid,
                        gridColumns: GridColumns,
                        rootPriority: 200,
                        loop: false,
                        scrollController: scroller);

                    ControllerNav.LinkSections(headerHolder, gridHolder);

                    overlay.AddComponent<OverlayInputWatcher>().Initialize(controller);

                    // Snapping the cursor into the panel is also what keeps the overlay from closing:
                    // it only stays open while the pointer is near.
                    ControllerNav.Focus(gridApplied ? gridHolder : headerHolder);
                }
                catch (Exception ex) { TFTVLogger.Error(ex); }
            }

            private static List<Selectable> CollectGridOptions(RectTransform gridRect)
            {
                List<Selectable> options = new List<Selectable>();

                if (gridRect == null || !gridRect.gameObject.activeSelf)
                {
                    return options;
                }

                for (int i = 0; i < gridRect.childCount; i++)
                {
                    Transform child = gridRect.GetChild(i);
                    Button button = child != null ? child.GetComponent<Button>() : null;
                    if (button != null)
                    {
                        options.Add(button);
                    }
                }

                return options;
            }

            /// <summary>
            /// Everything clickable in the header - in practice the acquire button, when the base ability
            /// has not been bought yet. Excludes the grid, which is its own section.
            /// </summary>
            private static List<Selectable> CollectHeaderButtons(RectTransform contentRect, RectTransform gridRect)
            {
                List<Selectable> buttons = new List<Selectable>();

                foreach (Button button in contentRect.GetComponentsInChildren<Button>(includeInactive: false))
                {
                    if (button == null || (gridRect != null && button.transform.IsChildOf(gridRect)))
                    {
                        continue;
                    }

                    buttons.Add(button);
                }

                return buttons;
            }

            private static UINavigationalElementsHolder CreateHolderObject(Transform parent, string name)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.SetActive(false);
                go.transform.SetParent(parent, false);
                go.AddComponent<LayoutElement>().ignoreLayout = true;

                UINavigationalElementsHolder holder = ControllerNav.EnsureHolder(go);
                go.SetActive(true);
                return holder;
            }

            /// <summary>
            /// Closes the overlay on Cancel, and clears the global navigation suppression flag for as long
            /// as the overlay is up - holder navigation does nothing at all while that flag is set, and
            /// whichever screen is underneath may well have set it. The previous value is restored on
            /// close so the host screen is left as it was found.
            /// </summary>
            private sealed class OverlayInputWatcher : MonoBehaviour
            {
                private DrillOverlayController _controller;
                private InputController _input;
                private UIGlobalNavigationController _globalNavigation;
                private bool _restoreSuppression;

                internal void Initialize(DrillOverlayController controller)
                {
                    _controller = controller;
                }

                private void OnEnable()
                {
                    try
                    {
                        _input = GameUtl.GameComponent<InputController>();
                        _input?.EventHandlers.AddUnique(HandleInput, -110);

                        _globalNavigation = GameUtl.CurrentLevel()?.GetComponent<UIGlobalNavigationController>();
                        if (_globalNavigation != null && _globalNavigation.SupressInputEvents)
                        {
                            _restoreSuppression = true;
                            _globalNavigation.SupressInputEvents = false;
                        }
                    }
                    catch (Exception ex) { TFTVLogger.Error(ex); }
                }

                private void OnDisable()
                {
                    try
                    {
                        _input?.EventHandlers.Remove(HandleInput);

                        if (_restoreSuppression && _globalNavigation != null)
                        {
                            _globalNavigation.SupressInputEvents = true;
                        }
                    }
                    catch (Exception ex) { TFTVLogger.Error(ex); }
                }

                private bool HandleInput(InputEvent ev)
                {
                    try
                    {
                        if (ev.Type != InputEventType.Pressed || ev.Name != "Cancel" || _controller == null)
                        {
                            return false;
                        }

                        _controller.Close();
                        return true;
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
}
