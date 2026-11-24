using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    /// <summary>
    /// Health bars stuck "selected": the body-part pictogram and the status row stay up over an enemy nobody is
    /// pointing at (seen after a grenade on a Myrmidon, earlier on an Acheron).
    /// Vanilla HealthbarUIActorElement.UpdateSelected switches the animator's "Selected" on at once, but switching it off
    /// waits for the bar's damage/status popups to clear (TacActorHealthbarStatusPopups.WaitForClear, run from that
    /// component's LateUpdate once no popup, or no "Important" one, is left). If that wait never ends the bar stays
    /// expanded. Here a bar that is not selected but whose animator still says Selected for a few seconds is closed,
    /// and the log says what the wait was stuck on.
    /// Each popup lives 7 s (Duration in the prefab, FadeDuration 3), so a soldier's bar normally stays open up to 7 s
    /// after it fired. Until 2026-11-23 the 4 s grace cut that normal wait short (every close in that day's logs was
    /// one); now a bar waiting on live, on-time popups is left alone (user's call: wait like vanilla). The log marks a
    /// popup past its life as OVERDUE and the line as "STUCK popup".
    /// </summary>
    internal static class HealthbarSelectedTacticalVanillaFixes
    {
        private const float GraceSeconds = 4f;

        private static readonly int SelectedHash = Animator.StringToHash("Selected");

        private static readonly AccessTools.FieldRef<HealthbarUIActorElement, Animator> AnimatorField =
            AccessTools.FieldRefAccess<HealthbarUIActorElement, Animator>("_animator");

        private static readonly AccessTools.FieldRef<TacActorHealthbarStatusPopups, Action> OnClearField =
            AccessTools.FieldRefAccess<TacActorHealthbarStatusPopups, Action>("_onClear");

        /// <summary>Set to Time.time + Duration - FadeDuration by SetData, so the popup is due to go at _startTime + FadeDuration.</summary>
        private static readonly AccessTools.FieldRef<TacActorHealthbarStatusPopup, float> StartTimeField =
            AccessTools.FieldRefAccess<TacActorHealthbarStatusPopup, float>("_startTime");

        private static string Remaining(TacActorHealthbarStatusPopup popup, float time)
        {
            float left = StartTimeField(popup) + popup.FadeDuration - time;
            return left >= 0f ? $"{left:0.0}s left" : $"OVERDUE {-left:0.0}s";
        }

        /// <summary>When each bar was first seen deselected but still showing as selected.</summary>
        private static readonly Dictionary<HealthbarUIActorElement, float> StuckSince = new Dictionary<HealthbarUIActorElement, float>();

        [HarmonyPatch(typeof(HealthbarUIActorElement), "LateUpdate")]
        internal static class HealthbarUIActorElement_LateUpdate_StuckSelected_Patch
        {
            private static void Postfix(HealthbarUIActorElement __instance)
            {
                try
                {
                    if (__instance.GetSelected())
                    {
                        if (StuckSince.Count > 0)
                        {
                            StuckSince.Remove(__instance);
                        }

                        return;
                    }

                    Animator animator = AnimatorField(__instance);

                    if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || !animator.GetBool(SelectedHash))
                    {
                        if (StuckSince.Count > 0)
                        {
                            StuckSince.Remove(__instance);
                        }

                        return;
                    }

                    float now = Time.unscaledTime;

                    if (!StuckSince.TryGetValue(__instance, out float since))
                    {
                        if (StuckSince.Count > 64)
                        {
                            foreach (HealthbarUIActorElement gone in StuckSince.Keys.Where(k => k == null).ToList())
                            {
                                StuckSince.Remove(gone);
                            }
                        }

                        StuckSince[__instance] = now;
                        return;
                    }

                    if (now - since < GraceSeconds || IsNormalWait(__instance))
                    {
                        return;
                    }

                    StuckSince.Remove(__instance);
                    Close(__instance, animator, now - since);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Vanilla still waiting on popups that are alive and on time: the bar stays open, as in vanilla, until the last
        /// important popup goes (up to 7 s after it was shown). Only a wait vanilla can't finish is cut short: a popup
        /// past its life, a popups component that no longer updates, or nothing left to wait for.
        /// </summary>
        private static bool IsNormalWait(HealthbarUIActorElement bar)
        {
            TacActorHealthbarStatusPopups popups = bar.StatusPopups;

            if (popups == null || !popups.isActiveAndEnabled || OnClearField(popups) == null)
            {
                return false;
            }

            float time = Time.time;
            bool important = false;

            foreach (TacActorHealthbarStatusPopup popup in popups.GetComponentsInChildren<TacActorHealthbarStatusPopup>(false))
            {
                if (popup == null || popup.PopupInfo == null)
                {
                    continue;
                }

                if (StartTimeField(popup) + popup.FadeDuration < time - 0.5f)
                {
                    return false;
                }

                important |= popup.PopupInfo.Important;
            }

            return important;
        }

        private static void Close(HealthbarUIActorElement bar, Animator animator, float seconds)
        {
            string actorName = (bar.Actor as TacticalActorBase)?.DisplayName ?? bar.Actor?.name ?? "?";
            string popupsText = "no popups component";

            TacActorHealthbarStatusPopups popups = bar.StatusPopups;

            if (popups != null)
            {
                bool waiting = OnClearField(popups) != null;
                List<TacActorHealthbarStatusPopup> live = popups.GetComponentsInChildren<TacActorHealthbarStatusPopup>(true)
                    .Where(p => p != null && p.gameObject.activeSelf)
                    .ToList();

                // A popup destroys itself Duration (7 s in the prefab) after its last SetData; past that it is stuck.
                float time = Time.time;
                List<string> entries = live
                    .Select(p => $"{p.PopupInfo?.MessageId ?? "?"}{(p.PopupInfo != null && p.PopupInfo.Important ? " (important)" : "")} {Remaining(p, time)}")
                    .ToList();
                bool overdue = live.Any(p => StartTimeField(p) + p.FadeDuration < time - 0.5f);

                popupsText = $"{(overdue ? "STUCK popup" : "normal wait")}: popups active {popups.isActiveAndEnabled}, deselect waiting {waiting}, entries [{string.Join(", ", entries)}]";

                if (waiting)
                {
                    OnClearField(popups) = null; // this is what it was waiting to do
                }
            }

            animator.SetBool(SelectedHash, false);
            TFTVLogger.Always($"[Healthbar] {actorName}'s health bar stayed expanded {seconds:0.0}s after it was deselected; closed it. {popupsText}");
        }
    }
}
