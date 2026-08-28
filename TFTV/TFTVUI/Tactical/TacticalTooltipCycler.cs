using Base.Core;
using Base.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using PhoenixPoint.Tactical.Levels;
using UnityEngine.EventSystems;

namespace TFTV.TFTVUI.Tactical
{
    /// <summary>
    /// Makes the mod's tactical HUD widgets readable with a gamepad.
    ///
    /// The widgets show their detail on hover, and hover is unreachable there: unlike the geoscape, the
    /// main tactical view runs no free cursor - it is only used on the inventory screen - so a controller
    /// has no pointer to put over them. Rather than restructure each widget, this drives them with the
    /// same pointer enter/exit events the mouse would send. Both mechanisms the widgets use
    /// (<see cref="EventTrigger"/> and <c>UITooltipText</c>) implement the pointer handler interfaces, so
    /// one synthetic event works for all of them and their existing mouse behaviour is untouched.
    ///
    /// One press advances through the registered widgets and a further press past the last one hides
    /// everything, so a single button reaches them all and always has a way back to a clean HUD.
    /// </summary>
    internal static class TacticalTooltipCycler
    {
        /// <summary>Name of the rebindable action, created in TFTVDefsInjectedOnlyOnce.</summary>
        internal const string ActionName = "TFTVCycleTacticalTooltips";

        private sealed class Target
        {
            public string Name;
            public Func<GameObject> Resolve;
        }

        private static readonly List<Target> _targets = new List<Target>();

        private static int _current = -1;
        private static GameObject _shown;
        private static InputController _input;

        /// <summary>
        /// Registers a widget's hover target. The target is resolved through a delegate rather than stored
        /// directly, because the widgets are rebuilt during a mission and a captured reference would go
        /// stale. Registering the same name twice replaces the previous entry, so widgets can call this
        /// every time they are built. Order of registration is the cycle order.
        /// </summary>
        internal static void Register(string name, Func<GameObject> resolve)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || resolve == null)
                {
                    return;
                }

                for (int i = 0; i < _targets.Count; i++)
                {
                    if (_targets[i].Name == name)
                    {
                        _targets[i].Resolve = resolve;
                        EnsureInputHandler();
                        return;
                    }
                }

                _targets.Add(new Target { Name = name, Resolve = resolve });
                EnsureInputHandler();
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        /// <summary>Called when the tactical UI data is cleared, so nothing survives into a new mission.</summary>
        internal static void Reset()
        {
            try
            {
                HideCurrent();
                _targets.Clear();
                _current = -1;
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        private static void EnsureInputHandler()
        {
            try
            {
                if (_input != null)
                {
                    return;
                }

                _input = GameUtl.GameComponent<InputController>();
                if (_input == null)
                {
                    return;
                }

                // Ahead of everything, so the dedicated key always wins.
                _input.EventHandlers.AddUnique(HandleInput, -110);

                // Behind the view state (which registers at the default priority of 0), so B only reaches
                // us when nothing else consumed it. The tactical states return true from Cancel exactly
                // when they actually cancelled something - closing a contextual menu, dropping a targeting
                // mode - so arriving here already means "there was nothing to cancel".
                _input.EventHandlers.AddUnique(HandleUnhandledCancel, 50);
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        private static bool HandleInput(InputEvent ev)
        {
            try
            {
                if (ev.Type != InputEventType.Pressed || ev.Name != ActionName || _targets.Count == 0)
                {
                    return false;
                }

                Advance();
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Cycles on B when the gamepad has nothing else to cancel. The custom action exists as a
        /// keyboard binding, but a mod-added gamepad binding never resolves to a physical button here, so
        /// the pad reuses Cancel instead of claiming one.
        ///
        /// Gated on the event's own InputType rather than the controller's current mode, matching how the
        /// tactical states themselves separate B from Escape - so Escape keeps its normal meaning.
        /// </summary>
        private static bool HandleUnhandledCancel(InputEvent ev)
        {
            try
            {
                if (ev.Type != InputEventType.Pressed
                    || ev.Name != "Cancel"
                    || ev.InputType != InputType.Joystick
                    || _targets.Count == 0)
                {
                    return false;
                }

                // Targets are cleared between missions, but be certain this is a live tactical map before
                // taking a button that means something everywhere else.
                if (GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>() == null)
                {
                    return false;
                }

                Advance();
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Moves to the next widget that is actually on screen, then past the last one to showing nothing.
        /// Widgets come and go during a mission - the Ancients widget only exists on those missions - so
        /// unresolvable entries are skipped rather than costing the player a press on nothing.
        /// </summary>
        private static void Advance()
        {
            HideCurrent();

            for (int step = 0; step < _targets.Count; step++)
            {
                _current++;

                if (_current >= _targets.Count)
                {
                    // Past the end: everything hidden, next press starts the cycle again.
                    _current = -1;
                    return;
                }

                GameObject target = Resolve(_targets[_current]);
                if (target != null)
                {
                    SendPointerEvent(target, ExecuteEvents.pointerEnterHandler);
                    _shown = target;
                    return;
                }
            }

            _current = -1;
        }

        private static void HideCurrent()
        {
            if (_shown != null)
            {
                SendPointerEvent(_shown, ExecuteEvents.pointerExitHandler);
            }

            _shown = null;
        }

        private static GameObject Resolve(Target target)
        {
            try
            {
                GameObject go = target.Resolve();
                return go != null && go.activeInHierarchy ? go : null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static void SendPointerEvent(
            GameObject target,
            ExecuteEvents.EventFunction<IPointerEnterHandler> handler)
        {
            SendPointerEventInternal(target, (go, data) => ExecuteEvents.Execute(go, data, handler));
        }

        private static void SendPointerEvent(
            GameObject target,
            ExecuteEvents.EventFunction<IPointerExitHandler> handler)
        {
            SendPointerEventInternal(target, (go, data) => ExecuteEvents.Execute(go, data, handler));
        }

        private static void SendPointerEventInternal(
            GameObject target,
            Action<GameObject, PointerEventData> execute)
        {
            try
            {
                if (target == null)
                {
                    return;
                }

                // The widgets position their tooltips from values captured when they were built rather
                // than from the event, so a pointer position is not needed - but one is supplied anyway
                // for anything that reads it.
                PointerEventData data = new PointerEventData(EventSystem.current)
                {
                    position = target.transform.position
                };

                execute(target, data);
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }
    }
}
