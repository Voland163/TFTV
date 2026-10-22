using Base.UI;
using HarmonyLib;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    /// <summary>
    /// Vanilla bug: the geoscape opens queued screens (modals such as Activate Base, events, the replenish screen,
    /// deployment) one at a time through GeoscapeViewSwitchQuery. While one is "current" every other request waits,
    /// and only the queued screen itself releases the queue, by calling GeoscapeView.FinishQueriedState when it is
    /// confirmed or cancelled. A queued screen left any other way - the replenish screen after a mission closed by
    /// clicking a section in the top bar, for one - never does, so every later modal and event piles up unseen:
    /// context-menu actions and events seem to do nothing, then all pop up one after another the next time some
    /// screen happens to call FinishQueriedState (a level-up confirmation, for instance) or the game is reloaded.
    ///
    /// The watchdog releases the queue once the current request's screen has been off the state stack for a second.
    /// It only clears the request: the screen is already gone, so there is no previous state to return to.
    /// </summary>
    internal static class StateQueueGeoscapeVanillaFixes
    {
        private const float OrphanGraceSeconds = 1f;

        private static readonly FieldInfo SwitchQueryField = AccessTools.Field(typeof(GeoscapeView), "_viewSwichQuery");
        private static readonly FieldInfo ViewStatesStackField = AccessTools.Field(typeof(GeoscapeView), "_statesStack");
        private static readonly FieldInfo CurrentRequestField = AccessTools.Field(typeof(GeoscapeViewSwitchQuery), "_currentStateSwitchRequest");
        private static readonly FieldInfo InnerStackField = AccessTools.Field(typeof(StateStack<GeoscapeViewContext>), "_statesStack");

        private static GeoscapeViewStateSwitchRequest _orphanRequest;
        private static float _orphanSince;

        private static void Check(GeoscapeView view)
        {
            if (SwitchQueryField == null || ViewStatesStackField == null || CurrentRequestField == null || InnerStackField == null)
            {
                return;
            }

            object query = SwitchQueryField.GetValue(view);

            if (query == null || !(CurrentRequestField.GetValue(query) is GeoscapeViewStateSwitchRequest request) || request.State == null)
            {
                _orphanRequest = null;
                return;
            }

            object stateStack = ViewStatesStackField.GetValue(view);

            if (stateStack == null || !(InnerStackField.GetValue(stateStack) is Stack<IState<GeoscapeViewContext>> states) || states.Contains(request.State))
            {
                _orphanRequest = null;
                return;
            }

            if (!ReferenceEquals(_orphanRequest, request))
            {
                _orphanRequest = request;
                _orphanSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _orphanSince < OrphanGraceSeconds)
            {
                return;
            }

            CurrentRequestField.SetValue(query, null);
            _orphanRequest = null;
            TFTVLogger.Always($"[GeoscapeStateQueue] {request.State.GetType().Name} was left without finishing its queued switch; released the queue so waiting screens can open");
        }

        [HarmonyPatch(typeof(GeoscapeView), "Update")]
        internal static class GeoscapeView_Update_ReleaseOrphanedQueuedState_Patch
        {
            private static void Postfix(GeoscapeView __instance)
            {
                try
                {
                    Check(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
