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
    /// </summary>
    internal static class HealthbarSelectedTacticalVanillaFixes
    {
        private const float GraceSeconds = 4f;

        private static readonly int SelectedHash = Animator.StringToHash("Selected");

        private static readonly AccessTools.FieldRef<HealthbarUIActorElement, Animator> AnimatorField =
            AccessTools.FieldRefAccess<HealthbarUIActorElement, Animator>("_animator");

        private static readonly AccessTools.FieldRef<TacActorHealthbarStatusPopups, Action> OnClearField =
            AccessTools.FieldRefAccess<TacActorHealthbarStatusPopups, Action>("_onClear");

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

                    if (now - since < GraceSeconds)
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

        private static void Close(HealthbarUIActorElement bar, Animator animator, float seconds)
        {
            string actorName = (bar.Actor as TacticalActorBase)?.DisplayName ?? bar.Actor?.name ?? "?";
            string popupsText = "no popups component";

            TacActorHealthbarStatusPopups popups = bar.StatusPopups;

            if (popups != null)
            {
                bool waiting = OnClearField(popups) != null;
                List<string> entries = popups.GetComponentsInChildren<TacActorHealthbarStatusPopup>(true)
                    .Where(p => p != null && p.gameObject.activeSelf)
                    .Select(p => $"{p.PopupInfo?.MessageId ?? "?"}{(p.PopupInfo != null && p.PopupInfo.Important ? " (important)" : "")}")
                    .ToList();

                popupsText = $"popups active {popups.isActiveAndEnabled}, deselect waiting {waiting}, entries [{string.Join(", ", entries)}]";

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
