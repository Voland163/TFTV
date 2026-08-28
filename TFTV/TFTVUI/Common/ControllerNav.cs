using Base.Core;
using Base.Input;
using Base.UI;
using PhoenixPoint.Common.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Common
{
    /// <summary>
    /// How vertical movement behaves between rows published by <see cref="ControllerNav.ApplyRows"/>.
    /// </summary>
    internal enum RowVerticalLinking
    {
        /// <summary>
        /// Up and down land on the neighbouring row's head element. Right for ragged rows built around
        /// one primary control - a recruit's name flanked by icons, a choice flanked by its approaches -
        /// where the columns do not line up and landing on the primary control is what reads correctly.
        /// </summary>
        RowHead,

        /// <summary>
        /// Up and down stay in the same column, clamping to the last element when the neighbouring row is
        /// shorter. Right for an actual grid of equivalent cells.
        /// </summary>
        SameColumn
    }

    /// <summary>
    /// Makes mod-built UI reachable with a gamepad.
    ///
    /// Phoenix Point drives its controller UI through <see cref="UIGlobalNavigationController"/>: a panel
    /// registers a <see cref="UINavigationalElementsHolder"/>, the holder exposes a list of
    /// <see cref="Selectable"/>s, and the stick/D-pad moves the virtual cursor
    /// (<see cref="SlimUI.ConsoleCursors.FreeCursorController"/>) from one of them to the next. The cursor
    /// is what actually clicks, so anything the holder does not list can never be reached once a root
    /// holder is active - free cursor movement is switched off for as long as one is registered.
    ///
    /// Nothing the mod builds registers a holder, which is why none of it responds to a controller.
    /// Attach a holder with <see cref="EnsureHolder"/> when the panel is built, call <see cref="Apply"/>
    /// once its buttons exist, and <see cref="Release"/> before tearing them down.
    /// </summary>
    internal static class ControllerNav
    {
        /// <summary>
        /// Root priority for mod panels. Vanilla holders are authored in the scene and sit well below
        /// this, so a mod panel opened on top of a vanilla screen takes navigation focus.
        /// </summary>
        internal const int DefaultRootPriority = 100;

        /// <summary>
        /// Navigation layer for mod panels. The global controller's priority fallbacks
        /// (<see cref="EmptyNavLinkBehaviour"/>) only ever consider holders on the same layer, so keeping
        /// mod holders off layer 0 stops a section switch from escaping into a vanilla screen underneath.
        /// </summary>
        internal const int ModNavigationLayer = 17;

        private static readonly FieldInfo ScrollerInterpolationCurveField =
            typeof(VerticalScrollRectScroller).GetField("_interpolationCurve", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Attaches a dormant holder to <paramref name="panel"/>, leaving it disabled so it does not
        /// register with an empty element list. Cheapest when <paramref name="panel"/> is still inactive;
        /// on a live object the component is added behind a brief deactivation so its OnEnable does not
        /// run before the holder is configured.
        /// </summary>
        internal static UINavigationalElementsHolder EnsureHolder(GameObject panel)
        {
            try
            {
                if (panel == null)
                {
                    return null;
                }

                UINavigationalElementsHolder holder = panel.GetComponent<UINavigationalElementsHolder>();
                if (holder != null)
                {
                    return holder;
                }

                bool wasActive = panel.activeSelf;
                if (wasActive)
                {
                    panel.SetActive(false);
                }

                holder = panel.AddComponent<UINavigationalElementsHolder>();
                holder.enabled = false;
                holder.transition = Selectable.Transition.None;
                holder.navigation = new Navigation { mode = Navigation.Mode.None };

                if (wasActive)
                {
                    panel.SetActive(true);
                }

                return holder;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Publishes <paramref name="items"/> as the holder's navigable elements and registers it as the
        /// active navigation root. Re-registers cleanly if the holder was already live, so it is safe to
        /// call again whenever the panel's contents are rebuilt.
        /// </summary>
        /// <returns>True when the holder ended up registered with at least one element.</returns>
        internal static bool Apply(
            UINavigationalElementsHolder holder,
            IList<Selectable> items,
            NavigationHolderMode mode = NavigationHolderMode.Vertical,
            int gridColumns = 0,
            int rootPriority = DefaultRootPriority,
            bool loop = true,
            int navigationLayer = ModNavigationLayer,
            VerticalScrollRectScroller scrollController = null)
        {
            try
            {
                if (!Configure(holder, items, mode, gridColumns, rootPriority, loop, navigationLayer, scrollController))
                {
                    return false;
                }

                holder.enabled = true;
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Publishes a set of rows - each a left-to-right run of elements - and registers the holder.
        /// Vertical movement always lands on the first element of a row, horizontal movement runs along it.
        ///
        /// Uses <see cref="NavigationHolderMode.Grid"/> because it is the only mode whose links the global
        /// controller honours on all four axes: in Vertical mode a left or right press unconditionally
        /// switches section, and in Horizontal mode so does up or down. The holder's own grid builder
        /// cannot express rows of differing lengths, so the links are rewritten here afterwards - nothing
        /// reads <see cref="UINavigationalElementsHolder.InteractableGridView"/> during navigation.
        /// </summary>
        /// <returns>True when the holder ended up registered with at least one element.</returns>
        internal static bool ApplyRows(
            UINavigationalElementsHolder holder,
            IList<IList<Selectable>> rows,
            IList<int> headIndices = null,
            int rootPriority = DefaultRootPriority,
            bool isRoot = true,
            int navigationLayer = ModNavigationLayer,
            VerticalScrollRectScroller scrollController = null,
            RowVerticalLinking verticalLinking = RowVerticalLinking.RowHead)
        {
            try
            {
                List<IList<Selectable>> validRows = new List<IList<Selectable>>();
                List<int> validHeads = new List<int>();
                List<Selectable> flattened = new List<Selectable>();
                CollectRows(rows, headIndices, validRows, validHeads, flattened);

                // Never loops: the global controller only offers a section switch when a link comes back
                // null, so wrapping around would trap the cursor inside this holder forever.
                if (!Configure(holder, flattened, NavigationHolderMode.Grid, 1, rootPriority, loop: false,
                        navigationLayer: navigationLayer, scrollController: scrollController, isRoot: isRoot))
                {
                    return false;
                }

                WriteRowLinks(validRows, validHeads, verticalLinking);

                holder.enabled = true;
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Gives an existing holder - typically one the game authored and already registered - a row
        /// layout, without touching its registration at all: IsRoot, priority, layer and enabled state are
        /// left exactly as the game set them, so it keeps its place in the navigation order and whatever
        /// focus handling the owning screen does still applies.
        ///
        /// Use this to weave mod-added elements into a vanilla screen rather than competing with it.
        /// </summary>
        internal static bool ApplyRowsToExistingHolder(
            UINavigationalElementsHolder holder,
            IList<IList<Selectable>> rows,
            IList<int> headIndices = null,
            RowVerticalLinking verticalLinking = RowVerticalLinking.RowHead)
        {
            try
            {
                if (holder == null)
                {
                    return false;
                }

                List<IList<Selectable>> validRows = new List<IList<Selectable>>();
                List<int> validHeads = new List<int>();
                List<Selectable> flattened = new List<Selectable>();
                CollectRows(rows, headIndices, validRows, validHeads, flattened);

                if (flattened.Count == 0)
                {
                    return false;
                }

                holder.InteractableContainers = null;
                holder.NavigationMode = NavigationHolderMode.Grid;
                holder.GridColumns = 1;

                // Re-publishing the element list makes the global controller reselect the holder's first
                // element, which would yank the cursor back to the top of the panel. When the same
                // elements are simply being relaid out - a host screen that rebuilds its widgets and
                // resets their navigation - only the links need rewriting, and the cursor stays put.
                if (!SameElements(holder.InteractiveList, flattened))
                {
                    holder.SetFixedInteractableElements(flattened);
                }

                WriteRowLinks(validRows, validHeads, verticalLinking);
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Finds the holder that already lists <paramref name="element"/> - the way to discover which
        /// authored holder owns a region of a vanilla screen, when a mod button has been dropped into it
        /// and needs to join the same list.
        /// </summary>
        internal static UINavigationalElementsHolder FindHolderContaining(Selectable element)
        {
            try
            {
                if (element == null)
                {
                    return null;
                }

                // Usually the holder sits on an ancestor of the elements it owns.
                UINavigationalElementsHolder holder = element.GetComponentInParent<UINavigationalElementsHolder>();
                if (holder != null && holder.Contains(element))
                {
                    return holder;
                }

                Transform root = element.transform.root;
                foreach (UINavigationalElementsHolder candidate in
                         root.GetComponentsInChildren<UINavigationalElementsHolder>(includeInactive: true))
                {
                    if (candidate != null && candidate.Contains(element))
                    {
                        return candidate;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Splices one element into an existing holder's list, immediately after
        /// <paramref name="insertAfter"/>, without touching the holder's registration.
        ///
        /// The position is given explicitly because the holder links its elements in list order, so list
        /// order has to match what is on screen - getting it wrong rewires the neighbouring elements too,
        /// which reads as the screen's own buttons breaking rather than as a misplaced newcomer.
        ///
        /// Does nothing when the element is already in the right place, so this is safe to call on every
        /// refresh: re-publishing the list would otherwise pull focus back to the top of the panel.
        /// </summary>
        internal static bool InsertIntoExistingHolder(
            UINavigationalElementsHolder holder,
            Selectable element,
            Selectable insertAfter)
        {
            try
            {
                if (holder == null || element == null || holder.GlobalNavigation == null)
                {
                    return false;
                }

                // A holder driven by InteractableContainers rebuilds its list from the hierarchy every
                // refresh and ignores FixedInteractableElements entirely. Publishing a fixed list there
                // would be dead weight at best, and would fight the holder's own rebuild at worst - a
                // refresh is all it needs to notice a new child.
                if (holder.InteractableContainers != null && holder.InteractableContainers.Count > 0)
                {
                    holder.RefreshInteractableList();
                    return holder.Contains(element);
                }

                List<Selectable> elements = new List<Selectable>();
                if (holder.InteractiveList != null)
                {
                    elements.AddRange(holder.InteractiveList);
                }

                // Refuse to publish a list that would drop what the holder already owns: without the
                // anchor present this is not the right holder, or its list has not been built yet, and
                // replacing it would strip the screen's own buttons out of navigation.
                if (insertAfter == null || !elements.Contains(insertAfter))
                {
                    return false;
                }

                int target = elements.IndexOf(insertAfter) + 1;

                int existing = elements.IndexOf(element);
                if (existing == target)
                {
                    return true;
                }

                if (existing >= 0)
                {
                    elements.RemoveAt(existing);
                    if (existing < target)
                    {
                        target--;
                    }
                }

                elements.Insert(Mathf.Clamp(target, 0, elements.Count), element);
                holder.SetFixedInteractableElements(elements);
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        private static bool SameElements(IList<Selectable> current, IList<Selectable> candidate)
        {
            if (current == null || current.Count != candidate.Count)
            {
                return false;
            }

            for (int i = 0; i < candidate.Count; i++)
            {
                if (current[i] != candidate[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void CollectRows(
            IList<IList<Selectable>> rows,
            IList<int> headIndices,
            List<IList<Selectable>> validRows,
            List<int> validHeads,
            List<Selectable> flattened)
        {
            if (rows == null)
            {
                return;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                IList<Selectable> row = rows[r];
                if (row == null)
                {
                    continue;
                }

                int requestedHead = headIndices != null && r < headIndices.Count ? headIndices[r] : 0;
                int head = 0;

                List<Selectable> validRow = new List<Selectable>();
                for (int i = 0; i < row.Count; i++)
                {
                    if (row[i] == null)
                    {
                        continue;
                    }

                    // Tracked while filtering so the head survives nulls being dropped.
                    if (i == requestedHead)
                    {
                        head = validRow.Count;
                    }

                    validRow.Add(row[i]);
                }

                if (validRow.Count > 0)
                {
                    validRows.Add(validRow);
                    validHeads.Add(Mathf.Clamp(head, 0, validRow.Count - 1));
                    flattened.AddRange(validRow);
                }
            }
        }

        private static void WriteRowLinks(
            List<IList<Selectable>> validRows,
            List<int> heads,
            RowVerticalLinking verticalLinking)
        {
            for (int r = 0; r < validRows.Count; r++)
            {
                IList<Selectable> row = validRows[r];
                IList<Selectable> rowAbove = r > 0 ? validRows[r - 1] : null;
                IList<Selectable> rowBelow = r < validRows.Count - 1 ? validRows[r + 1] : null;

                for (int i = 0; i < row.Count; i++)
                {
                    Navigation navigation = row[i].navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnLeft = i > 0 ? row[i - 1] : null;
                    navigation.selectOnRight = i < row.Count - 1 ? row[i + 1] : null;
                    navigation.selectOnUp = PickVerticalTarget(rowAbove, r > 0 ? heads[r - 1] : 0, i, verticalLinking);
                    navigation.selectOnDown = PickVerticalTarget(rowBelow, r < validRows.Count - 1 ? heads[r + 1] : 0, i, verticalLinking);
                    row[i].navigation = navigation;
                }
            }
        }

        private static Selectable PickVerticalTarget(
            IList<Selectable> targetRow,
            int targetHead,
            int column,
            RowVerticalLinking verticalLinking)
        {
            if (targetRow == null)
            {
                return null;
            }

            if (verticalLinking == RowVerticalLinking.SameColumn)
            {
                return targetRow[Mathf.Min(column, targetRow.Count - 1)];
            }

            return targetRow[targetHead];
        }

        /// <summary>
        /// Shared setup for <see cref="Apply"/> and <see cref="ApplyRows"/>: validates the elements and
        /// writes the holder's configuration, leaving it dormant so the caller can adjust links before
        /// anything registers with the global controller.
        /// </summary>
        private static bool Configure(
            UINavigationalElementsHolder holder,
            IList<Selectable> items,
            NavigationHolderMode mode,
            int gridColumns,
            int rootPriority,
            bool loop,
            int navigationLayer,
            VerticalScrollRectScroller scrollController,
            bool isRoot = true)
        {
            if (holder == null)
            {
                return false;
            }

            // Nulls only: vanilla holders list inactive and non-interactable elements too (the
            // activation modal's own facility icons are non-interactable), and the global
            // controller skips them at selection time via GetFirstActiveElement.
            List<Selectable> valid = new List<Selectable>();
            if (items != null)
            {
                foreach (Selectable item in items)
                {
                    if (item != null)
                    {
                        valid.Add(item);
                    }
                }
            }

            // Unregister first: reconfiguring a live holder would leave a stale entry in the
            // global controller's root list.
            Release(holder);

            if (valid.Count == 0)
            {
                return false;
            }

            holder.IsRoot = isRoot;
            holder.RootPriority = rootPriority;
            holder.NavigationLayer = navigationLayer;
            holder.NavigationMode = mode;
            holder.GridColumns = mode == NavigationHolderMode.Grid ? Math.Max(1, gridColumns) : 0;
            holder.IsLoopingVertical = loop;
            holder.IsLoopingHorizontal = loop;

            // Lets ForceCursorToPosition scroll a selected element into view instead of parking the
            // virtual cursor outside the viewport.
            holder.ScrollController = scrollController;

            // InitInteractableList prefers InteractableContainers, then FixedInteractableElements,
            // and only falls back to scanning direct children. Clearing the containers keeps it on
            // the explicit list, which is the only reliable option for dynamically built panels.
            holder.InteractableContainers = null;
            holder.FixedInteractableElements = valid;

            // Build the list and the explicit Navigation links while still dormant, so OnEnable
            // finds a populated holder.
            holder.RefreshInteractableList();
            return true;
        }

        /// <summary>
        /// Unregisters the holder from <see cref="UIGlobalNavigationController"/>, restoring the previous
        /// navigation root and re-enabling free cursor movement. Call before destroying the elements the
        /// holder points at.
        /// </summary>
        internal static void Release(UINavigationalElementsHolder holder)
        {
            try
            {
                if (holder == null || !holder.enabled)
                {
                    return;
                }

                // The holder's OnDisable reaches for the level's navigation controller without a null
                // check, so disabling one while the level is being torn down throws. Leaving it enabled
                // is harmless there - it is about to be destroyed with everything else.
                if (GameUtl.CurrentLevel() == null)
                {
                    return;
                }

                holder.enabled = false;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        /// <summary>
        /// Forces navigation focus onto an already-applied holder and parks the virtual cursor on its
        /// first element.
        ///
        /// Registering a holder is not enough on its own: Add() only makes one current when its priority
        /// strictly exceeds the current root's, so a holder deliberately ranked below the panels it opens
        /// can be accepted and then ignored. ForceSelect bypasses that comparison, and free cursor
        /// movement has to be switched off by hand because only Add() does it.
        /// </summary>
        internal static bool Focus(UINavigationalElementsHolder holder)
        {
            try
            {
                // Focusing is a gamepad concept: on mouse the free cursor follows the pointer, and forcing
                // a snap would fight it.
                if (holder == null || !holder.enabled || !IsUsingController())
                {
                    return false;
                }

                UIGlobalNavigationController globalNavigation = holder.GlobalNavigation;
                Selectable first = holder.GetFirstActiveElement();
                if (globalNavigation == null || first == null)
                {
                    return false;
                }

                globalNavigation.ForceSelect(holder, first);
                GameUtl.GetFreeCursorController()?.DisableInput();
                return true;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// True while the game is being driven by a gamepad. Use it to branch on interactions that have no
        /// controller equivalent - double-click, right-click, hover-only affordances.
        /// </summary>
        internal static bool IsUsingController()
        {
            try
            {
                InputController input = GameUtl.GameComponent<InputController>();
                return input != null && input.InputType == InputType.Joystick;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Links two holders that sit side by side, so running off the right edge of
        /// <paramref name="left"/> moves into <paramref name="right"/> and vice versa. Same mechanism as
        /// <see cref="LinkSections"/>, but for a horizontal split - a list with a panel beside it, rather
        /// than stacked sections.
        /// </summary>
        internal static void LinkHorizontally(
            UINavigationalElementsHolder left,
            UINavigationalElementsHolder right)
        {
            try
            {
                if (left == null || right == null)
                {
                    return;
                }

                Navigation leftNav = left.navigation;
                leftNav.mode = Navigation.Mode.Explicit;
                leftNav.selectOnRight = right;
                left.navigation = leftNav;

                Navigation rightNav = right.navigation;
                rightNav.mode = Navigation.Mode.Explicit;
                rightNav.selectOnLeft = left;
                right.navigation = rightNav;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        /// <summary>
        /// Groups elements into rows by where they actually sit on screen - top to bottom, then left to
        /// right within each row. Use this rather than creation order whenever a panel is laid out by
        /// absolute position, so stick directions match what the player sees.
        /// </summary>
        internal static List<IList<Selectable>> GroupIntoRowsByPosition(
            IList<Selectable> items,
            float rowTolerance = 40f)
        {
            List<IList<Selectable>> rows = new List<IList<Selectable>>();

            try
            {
                List<Selectable> remaining = new List<Selectable>();
                foreach (Selectable item in items ?? new List<Selectable>())
                {
                    if (item != null)
                    {
                        remaining.Add(item);
                    }
                }

                // Highest first: screen space y grows upwards.
                remaining.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

                int index = 0;
                while (index < remaining.Count)
                {
                    float rowY = remaining[index].transform.position.y;
                    List<Selectable> row = new List<Selectable>();

                    while (index < remaining.Count
                           && Mathf.Abs(remaining[index].transform.position.y - rowY) <= rowTolerance)
                    {
                        row.Add(remaining[index]);
                        index++;
                    }

                    row.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return rows;
        }

        /// <summary>
        /// Chains holders top to bottom so the right stick switches between them as sections. The global
        /// controller resolves a section switch with <c>FindFirstActiveSelectableUp/Down</c> on the holder
        /// itself - holders are <see cref="Selectable"/>s - so the links have to be explicit.
        /// Pass them in visual order.
        /// </summary>
        internal static void LinkSections(params UINavigationalElementsHolder[] holders)
        {
            try
            {
                if (holders == null)
                {
                    return;
                }

                List<UINavigationalElementsHolder> chain = new List<UINavigationalElementsHolder>();
                foreach (UINavigationalElementsHolder holder in holders)
                {
                    if (holder != null)
                    {
                        chain.Add(holder);
                    }
                }

                for (int i = 0; i < chain.Count; i++)
                {
                    Navigation navigation = chain[i].navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnUp = i > 0 ? chain[i - 1] : null;
                    navigation.selectOnDown = i < chain.Count - 1 ? chain[i + 1] : null;
                    navigation.selectOnLeft = null;
                    navigation.selectOnRight = null;
                    chain[i].navigation = navigation;
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        /// <summary>
        /// Attaches the game's scroller to a mod-built <see cref="ScrollRect"/> so a holder can scroll its
        /// selection into view.
        ///
        /// Two things have to be repaired for a runtime-added instance: the serialised interpolation curve
        /// is null (its default is only applied by the editor's Reset), which would throw inside the scroll
        /// coroutine; and <see cref="ScrollRect.viewport"/> must be set, since the scroller dereferences it
        /// directly. Assigning the ScrollRect's own transform matches what Unity already falls back to
        /// internally, so it changes no layout.
        /// </summary>
        internal static VerticalScrollRectScroller EnsureVerticalScroller(ScrollRect scrollRect)
        {
            try
            {
                if (scrollRect == null)
                {
                    return null;
                }

                if (scrollRect.viewport == null)
                {
                    scrollRect.viewport = scrollRect.transform as RectTransform;
                }

                VerticalScrollRectScroller scroller = scrollRect.GetComponent<VerticalScrollRectScroller>();
                if (scroller == null)
                {
                    scroller = scrollRect.gameObject.AddComponent<VerticalScrollRectScroller>();
                }

                if (ScrollerInterpolationCurveField != null
                    && ScrollerInterpolationCurveField.GetValue(scroller) == null)
                {
                    ScrollerInterpolationCurveField.SetValue(scroller, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
                }

                return scroller;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Collects the underlying <see cref="Selectable"/>s of a set of vanilla buttons, skipping any
        /// that failed to clone.
        /// </summary>
        internal static List<Selectable> SelectablesOf(IEnumerable<PhoenixGeneralButton> buttons)
        {
            List<Selectable> result = new List<Selectable>();

            if (buttons == null)
            {
                return result;
            }

            foreach (PhoenixGeneralButton button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                Selectable selectable = button.BaseButton != null
                    ? button.BaseButton
                    : button.GetComponent<Selectable>();

                if (selectable != null)
                {
                    result.Add(selectable);
                }
            }

            return result;
        }
    }
}
