using Base.Core;
using Base.Input;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.SiteEncounters;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVIncidents
{
    /// <summary>
    /// Walking away from an incident, as a cancel button rather than as a third response.
    ///
    /// The incident's own cancel is a choice like the other two, which puts "do nothing" in the same
    /// row, in the same shape and at the same weight as the two things the player came here to
    /// decide between - and on a screen whose whole job is to compare two responses, a third box
    /// that is not a response is noise. Every other geoscape screen the player can back out of
    /// (trade above all) puts that on a cancel button in the corner and on right-click, so this does
    /// the same: the vanilla choice button is hidden, a copy of trade's own exit button takes its
    /// place, and right-click or the Cancel action (Escape, or B on a pad) does the same thing.
    ///
    /// The choice itself is untouched. Everything the cancel path does - restoring the intro event
    /// so the incident can be picked up later, clearing the objective - hangs off that choice being
    /// selected, so the button selects it rather than reimplementing it.
    /// </summary>
    internal static class IncidentCancelButton
    {
        private const string CancelButtonName = "[Mod]IncidentCancelButton";

        /// <summary>Marks the incident's walk-away choice. Mirrors Resolution.IsCancelChoice.</summary>
        private const string CancelChoiceKeyFragment = "_CHOICE_2";

        /// <summary>Gap between the bottom of the left response and the cancel button under it.</summary>
        private const float GapBelowChoices = 16f;

        private static readonly MethodInfo OnChoiceClickedMethod =
            AccessTools.Method(typeof(SiteBaseChoiceButton), "OnChoiceClicked");

        private static SiteBaseChoiceButton _cancelChoiceButton;
        private static GameObject _cancelButtonObject;

        // Set the moment the choice is selected. Cancelling tears this screen down, and a click can
        // land on the same frame as a right-click, so without this the choice could be selected
        // twice. It is not the choice button reference that guards this, because that reference is
        // still needed afterwards to put the hidden vanilla button back.
        private static bool _cancelInFlight;

        /// <summary>
        /// The cancel button's Selectable, for the controller navigation to include as its own row.
        /// Null when there is no cancel button - an incident whose event has no cancel choice.
        /// </summary>
        internal static Selectable CancelSelectable { get; private set; }

        /// <summary>
        /// Hides the walk-away choice and puts a cancel button under the choices row in its place.
        /// Does nothing when the event has no walk-away choice, which leaves the screen exactly as it
        /// was rather than stranding the player on an incident they cannot leave.
        /// </summary>
        internal static void Install(UIModuleSiteEncounters module)
        {
            try
            {
                Remove(module);

                if (module?.ChoiceButtonsContainer == null)
                {
                    return;
                }

                _cancelChoiceButton = FindCancelChoiceButton(module);
                if (_cancelChoiceButton == null)
                {
                    return;
                }

                PhoenixGeneralButton source = ResolveTradeExitButton();
                if (source == null)
                {
                    // No button to copy: leave the vanilla choice visible rather than removing the
                    // only way out of the incident.
                    TFTVLogger.Always("[IncidentCancelButton] Trade exit button unavailable; keeping the cancel choice.");
                    _cancelChoiceButton = null;
                    return;
                }

                _cancelChoiceButton.gameObject.SetActive(false);
                BuildCancelButton(module, source);
                EnsureDismissWatcher(module);
                EnsurePositionKeeper(module);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Puts the choices row back the way it was found. Runs before every encounter, incident or
        /// not, so a hidden cancel choice never survives into an unrelated event.
        /// </summary>
        internal static void Remove(UIModuleSiteEncounters module)
        {
            try
            {
                if (_cancelChoiceButton != null)
                {
                    _cancelChoiceButton.gameObject.SetActive(true);
                }

                Retire(_cancelButtonObject);

                // A leftover from an earlier encounter whose module was torn down under us.
                if (module != null)
                {
                    Transform stale = module.transform.Find(CancelButtonName);
                    if (stale != null)
                    {
                        Retire(stale.gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            finally
            {
                _cancelChoiceButton = null;
                _cancelButtonObject = null;
                CancelSelectable = null;
                _cancelInFlight = false;
            }
        }

        /// <summary>
        /// Takes a cancel button out of the screen and schedules it for destruction.
        ///
        /// Deferred rather than immediate, because this can run from inside the button's own click
        /// handler - cancelling is what tears the screen down - and destroying an object while one of
        /// its components is mid-callback is not safe. Deactivating and unparenting it first means a
        /// rebuild in the same frame neither finds it by name nor sees it on screen, which is the
        /// only thing an immediate destroy was buying.
        /// </summary>
        private static void Retire(GameObject cancelButton)
        {
            if (cancelButton == null)
            {
                return;
            }

            cancelButton.SetActive(false);
            cancelButton.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(cancelButton);
        }

        /// <summary>
        /// Selects the incident's walk-away choice, as if its button had been clicked. False when
        /// there is nothing to cancel, so callers that fire on a key or a mouse button can tell the
        /// difference between "handled" and "not ours".
        /// </summary>
        internal static bool TryCancel()
        {
            try
            {
                if (_cancelInFlight || _cancelChoiceButton == null || _cancelButtonObject == null)
                {
                    return false;
                }

                if (OnChoiceClickedMethod == null)
                {
                    TFTVLogger.Always("[IncidentCancelButton] SiteBaseChoiceButton.OnChoiceClicked not found.");
                    return false;
                }

                _cancelInFlight = true;
                OnChoiceClickedMethod.Invoke(_cancelChoiceButton, null);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static SiteBaseChoiceButton FindCancelChoiceButton(UIModuleSiteEncounters module)
        {
            foreach (SiteBaseChoiceButton button in module.ChoiceButtonsContainer
                         .GetComponentsInChildren<SiteBaseChoiceButton>(includeInactive: true))
            {
                string key = button?.Choice?.Text?.LocalizationKey ?? string.Empty;
                if (key.IndexOf(CancelChoiceKeyFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }
            }

            return null;
        }

        private static PhoenixGeneralButton ResolveTradeExitButton()
        {
            GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            UIModuleTrade trade = level?.View?.GeoscapeModules?.TradeModule;
            return trade != null ? trade.ExitBtn : null;
        }

        /// <summary>
        /// Clones trade's exit button and hangs it under the choices row's bottom left corner.
        ///
        /// A copy rather than something built here, because this button has to read as the same
        /// control the player already knows: the same art, the same hover and press animation, and
        /// the same label - already translated into every language the game ships, which a string
        /// written here would not be.
        /// </summary>
        private static void BuildCancelButton(UIModuleSiteEncounters module, PhoenixGeneralButton source)
        {
            RectTransform moduleRect = module.transform as RectTransform;
            Transform parent = moduleRect != null ? moduleRect : module.transform;

            PhoenixGeneralButton clone = UnityEngine.Object.Instantiate(source, parent, false);
            clone.gameObject.name = CancelButtonName;
            clone.gameObject.SetActive(true);

            _cancelButtonObject = clone.gameObject;

            // Nothing should lay this out - it is placed against the screen, not in a row.
            LayoutElement layout = clone.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = clone.gameObject.AddComponent<LayoutElement>();
            }
            layout.ignoreLayout = true;

            RectTransform sourceRect = source.transform as RectTransform;
            RectTransform rect = clone.transform as RectTransform;
            if (rect != null)
            {
                // Anchored to the module's bottom left, and moved to its real place by
                // PositionUnderChoices once the responses have been laid out and can be measured.
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 1f);
                rect.localScale = Vector3.one;

                if (sourceRect != null)
                {
                    rect.sizeDelta = sourceRect.rect.size;
                }
            }

            clone.transform.SetAsLastSibling();

            // Tabbing is registered per screen; the clone would otherwise join trade's group and be
            // reachable from a screen it does not belong to.
            clone.TabbingControl = null;

            // No RemoveAllClickedDelegates here: it throws on a fresh clone, and there is nothing for
            // it to remove anyway - Instantiate copies serialized state, and a delegate field is not
            // serialized, so the clone arrives with no listeners.
            clone.PointerClicked += () => TryCancel();
            clone.SetEnabled(true);
            clone.SetInteractable(true);

            CancelSelectable = clone.BaseButton != null ? clone.BaseButton : clone.GetComponent<Selectable>();
        }

        /// <summary>
        /// Puts the cancel button under the left-hand response, aligned with its left edge.
        ///
        /// Measured off that button rather than placed at a screen coordinate. The first attempt hung
        /// it below the choices container, which runs to the bottom of the screen and took the button
        /// off it; the second put it at a fraction of the module's own rect, and that rect stops well
        /// above the bottom of the screen, so it landed on top of the responses instead. The one
        /// thing that is reliably where it looks is the response button itself.
        ///
        /// Call after the grid has been resized: the layout is forced through first, because the
        /// responses are laid out by a layout group and their rects are stale until it runs.
        /// </summary>
        // Reused by the per-frame keeper so it does not allocate.
        private static readonly Vector3[] _cornersBuffer = new Vector3[4];

        /// <summary>
        /// <paramref name="forceLayout"/> rebuilds the responses' layout first so a freshly filled
        /// screen can be measured straight away. The per-frame keeper passes false: the canvas lays
        /// itself out before rendering anyway, so forcing a full rebuild every frame was pure cost.
        /// </summary>
        internal static void PositionUnderChoices(UIModuleSiteEncounters module, bool forceLayout = true)
        {
            try
            {
                if (_cancelButtonObject == null || module?.ChoiceButtonsContainer == null)
                {
                    return;
                }

                RectTransform parentRect = _cancelButtonObject.transform.parent as RectTransform;
                RectTransform rect = _cancelButtonObject.transform as RectTransform;
                RectTransform container = module.ChoiceButtonsContainer.GetComponent<RectTransform>();
                if (parentRect == null || rect == null || container == null)
                {
                    return;
                }

                if (forceLayout)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(container);
                }

                RectTransform anchor = ResolveLeftmostChoiceRect(module) ?? container;

                Vector3[] corners = _cornersBuffer;
                anchor.GetWorldCorners(corners);

                // Corner 0 is the bottom left in world space, whatever the rect's own anchors and
                // pivot happen to be.
                Vector3 local = parentRect.InverseTransformPoint(corners[0]);

                // anchoredPosition is measured from the anchor, and this button is anchored to its
                // parent's bottom left corner - while InverseTransformPoint returns a point measured
                // from the parent's *pivot*. Mixing the two is what put the button off the bottom of
                // the screen: the parent's pivot is its centre, so the offset was out by half the
                // parent in each axis. rect.xMin/yMin are where that corner sits in the same local
                // space, which is exactly the correction.
                Vector2 anchorInLocalSpace = new Vector2(parentRect.rect.xMin, parentRect.rect.yMin);
                Vector2 placed = new Vector2(local.x, local.y - GapBelowChoices) - anchorInLocalSpace;

                if ((rect.anchoredPosition - placed).sqrMagnitude < 0.01f)
                {
                    return;
                }

                rect.anchoredPosition = placed;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// The response that sits furthest left on screen, which is the one the cancel button lines
        /// up under. Null when there is no visible response to measure against.
        /// </summary>
        private static RectTransform ResolveLeftmostChoiceRect(UIModuleSiteEncounters module)
        {
            RectTransform leftmost = null;
            float best = float.MaxValue;
            Vector3[] corners = _cornersBuffer;

            foreach (SiteBaseChoiceButton button in module.ChoiceButtonsContainer
                         .GetComponentsInChildren<SiteBaseChoiceButton>(includeInactive: false))
            {
                RectTransform rect = button != null ? button.transform as RectTransform : null;
                if (rect == null)
                {
                    continue;
                }

                rect.GetWorldCorners(corners);
                if (corners[0].x < best)
                {
                    best = corners[0].x;
                    leftmost = rect;
                }
            }

            return leftmost;
        }

        private static void EnsurePositionKeeper(UIModuleSiteEncounters module)
        {
            if (module.GetComponent<CancelPositionKeeper>() == null)
            {
                module.gameObject.AddComponent<CancelPositionKeeper>();
            }
        }

        /// <summary>
        /// Keeps the cancel button under the responses as the layout settles.
        ///
        /// Placing it once when the screen is built is not enough: the responses are laid out by a
        /// layout group, and on the first incident of a session their rects still hold the positions
        /// they had before the grid was resized - forcing a rebuild of the container does not fix it,
        /// because the container's own size comes from a parent that has not laid out yet either. The
        /// symptom was a cancel button that was wrong the first time an incident opened and right
        /// every time after, which is the signature of reading a layout before it has run.
        ///
        /// LateUpdate runs after that layout pass, so this reads the settled positions. It also keeps
        /// the button in place if the window is resized while the encounter is open.
        /// </summary>
        private sealed class CancelPositionKeeper : MonoBehaviour
        {
            private UIModuleSiteEncounters _module;

            private void Awake()
            {
                _module = GetComponent<UIModuleSiteEncounters>();
            }

            private void LateUpdate()
            {
                try
                {
                    if (_cancelButtonObject == null || _module == null)
                    {
                        return;
                    }

                    PositionUnderChoices(_module, forceLayout: false);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static void EnsureDismissWatcher(UIModuleSiteEncounters module)
        {
            if (module.GetComponent<CancelInputWatcher>() == null)
            {
                module.gameObject.AddComponent<CancelInputWatcher>();
            }
        }

        /// <summary>
        /// Right-click and the Cancel action, both routed to the same choice the button selects.
        ///
        /// Lives on the encounter module rather than on the button, so it is torn down with the
        /// screen; and does nothing at all unless a cancel button is currently installed, which is
        /// only ever true while an incident's intro is on screen.
        /// </summary>
        private sealed class CancelInputWatcher : MonoBehaviour
        {
            private InputController _input;

            private void OnEnable()
            {
                try
                {
                    // Ahead of the free cursor (-100) and the global navigation controller (-90), so
                    // the incident closes before anything underneath reacts to the same press.
                    _input = GameUtl.GameComponent<InputController>();
                    _input?.EventHandlers.AddUnique(HandleInput, -110);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private void OnDisable()
            {
                try
                {
                    _input?.EventHandlers.Remove(HandleInput);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private bool HandleInput(InputEvent ev)
            {
                try
                {
                    // The pad's back button. Escape did not cancel through here when this was the only
                    // route, so it is also read off the keyboard in Update.
                    if (_cancelButtonObject == null || ev.Type != InputEventType.Pressed || ev.Name != "Cancel")
                    {
                        return false;
                    }

                    return TryCancel();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }
            }

            private void Update()
            {
                try
                {
                    if (_cancelButtonObject == null)
                    {
                        return;
                    }

                    // Right-click, and Escape read off the keyboard directly. Escape also goes to the
                    // encounter state's own cancel, which is patched below; reading the key here as
                    // well means it works however the game routes it, and the in-flight guard in
                    // TryCancel stops the two from selecting the choice twice.
                    if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                    {
                        TryCancel();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// The encounter screen's own "back" - the view state's cancel hook, which is where the game
        /// sends a back action for the screen it owns.
        ///
        /// While a cancel button is installed it is routed to the walk-away choice instead, and the
        /// original skipped. Every other encounter is left exactly as vanilla has it.
        /// </summary>
        [HarmonyPatch(typeof(UIStateGeoscapeEvent), "OnCancel")]
        internal static class UIStateGeoscapeEvent_OnCancel_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;

            public static bool Prefix()
            {
                try
                {
                    if (_cancelButtonObject == null)
                    {
                        return true;
                    }

                    TryCancel();
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
