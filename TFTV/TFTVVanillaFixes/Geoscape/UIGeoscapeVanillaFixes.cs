using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    internal class UIGeoscapeVanillaFixes
    {

        //Patch to fix Vanilla perception multipliers application
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "ApplyStatModification")]
        public static class Patch_ApplyStatModification_MultiplyFix
        {
            public static bool Prefix(
                ItemStatModification statModifier,
                ref float fPerception,
                ref float fAccuracy,
                ref float fStealth,
                ref float fPerceptionMult,
                ref float fAccuracyMult,
                ref float fStealthMult)
            {
                switch (statModifier.TargetStat)
                {
                    case StatModificationTarget.Perception:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fPerception += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fPerceptionMult *= statModifier.Value; // Option A
                        }
                        break;

                    case StatModificationTarget.Accuracy:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fAccuracy += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fAccuracyMult *= statModifier.Value; // Option A
                        }
                        break;

                    case StatModificationTarget.Stealth:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fStealth += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fStealthMult *= statModifier.Value; // Option A
                        }
                        break;
                }

                // Skip original ApplyStatModification
                return false;
            }
        }





        //Fixes scanner showing colony detected for Palace
        [HarmonyPatch(typeof(SiteSurroundingsScanner), nameof(SiteSurroundingsScanner.AlienBasesAvailableInRange))]
        public static class SiteSurroundingsScanner_AlienBasesAvailableInRange_patch
        {

            public static void Postfix(SiteSurroundingsScanner __instance, GeoSite ____site, ref bool __result)
            {
                try
                {
                    Func<GeoSite, bool> querry = (GeoSite s) => s.GetComponent<GeoAlienBase>() != null && !s.GetComponent<GeoAlienBase>().IsPalace && s.GetInspected(____site.Owner) && s.State == GeoSiteState.Functioning;
                    MethodInfo methodInfo = typeof(SiteSurroundingsScanner).GetMethod("QuerryForAlienBases", BindingFlags.NonPublic | BindingFlags.Instance);
                    IEnumerable<GeoSite> eligibleSites = (IEnumerable<GeoSite>)methodInfo.Invoke(__instance, new object[] { querry });

                    __result = eligibleSites.Any();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        /// <summary>
        /// Vanilla bug: raising a stat and then buying an ability silently throws the stat purchase away.
        ///
        /// A stat bought in the edit soldier screen is only pending inside UIModuleCharacterProgression
        /// (_currentStrengthStat and friends) until CommitStatChanges runs. Opening the buy-ability
        /// confirmation pushes a modal view state, and UIStateEditSoldier.ExitState deliberately skips its
        /// commit while a dialog is up. Closing the modal then runs, in this order:
        ///
        ///   UIStateGeoModal.FinishDialog
        ///     -> FinishQueriedState  -> UIStateEditSoldier.EnterState -> SetCharacterProgression
        ///                                                            -> RefreshStats  (pending stat lost)
        ///     -> the dialog callback -> BuyAbility -> CommitStatChanges (deltas are now zero)
        ///
        /// RefreshStats re-reads both the starting and the current values off the character, so by the time
        /// the confirmation callback commits, there is nothing left to commit: the ability is bought and
        /// paid for, the stat is not. Cancelling the dialog loses the stat the same way, as does any other
        /// modal opened from this screen.
        ///
        /// Committing before EnterState refreshes the module keeps the purchase. This is what vanilla
        /// already does on its other refresh path, which guards SelectCharacterProgression with
        /// "if (IsCharacterChanged()) CommitStatChanges();" - EnterState simply never got the same guard.
        /// </summary>
        [HarmonyPatch(typeof(UIStateEditSoldier), "EnterState")]
        public static class Patch_UIStateEditSoldier_EnterState_KeepPendingStatPurchase
        {
            private static readonly AccessTools.FieldRef<UIModuleCharacterProgression, GeoCharacter> ModuleCharacter =
                AccessTools.FieldRefAccess<UIModuleCharacterProgression, GeoCharacter>("_character");

            // _characterProgressionModule on the state is a property, not a field, so it cannot be
            // injected by Harmony; it reads straight off the shared geoscape modules, which is what we do.
            public static void Prefix(GeoCharacter ____currentCharacter)
            {
                try
                {
                    UIModuleCharacterProgression module = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()
                        ?.View?.GeoscapeModules?.CharacterProgressionModule;

                    if (module == null || ____currentCharacter == null || !module.IsCharacterChanged())
                    {
                        return;
                    }

                    // Only ever commit what belongs to the operative this screen is showing. On the very
                    // first EnterState the module can still be holding another character, and those
                    // deltas are not ours to apply.
                    if (ModuleCharacter(module) != ____currentCharacter)
                    {
                        return;
                    }

                    module.CommitStatChanges();

                    TFTVLogger.Always($"[EditSoldier] Committed pending stat purchase for {____currentCharacter.DisplayName} before the progression panel refreshed.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        /// <summary>
        /// Vanilla bug: backing out of a deployment screen leaves the geoscape stuck in
        /// "deployment mode", after which the Bases / Personnel / Air Force row stops responding
        /// to clicks and then disappears.
        ///
        /// GeoscapeView.ToDeploymentState() sets SetUiInDeploymentMode, and the only place that
        /// ever clears it is GeoscapeView.ResetViewState(). UIStateRosterDeployment's single exit
        /// point, ToPreviousScreen(), calls ResetViewState() only on its shouldResetStateOnReturn
        /// branch; the other branch calls SwitchToPreviousState() and leaves the flag standing.
        /// Two vanilla callers take that branch - HavenFacilityController.OnInfiltrateBriefResult()
        /// and StealAircraftAbility - so opening a deployment that way and leaving without
        /// deploying strands the flag for the rest of the session.
        ///
        /// Everything that draws the section bar keys off it. UIStateVehicleSelected.EnterState()
        /// first calls Show(true), which activates SectionsRoot, and then ShowTabModules(), which
        /// sets that bar's CanvasGroup to interactable = false, alpha = 0 and blocksRaycasts = false
        /// - the row is present but dead. UIStateRosterDeployment.ExitState() calls
        /// Show(!SetUiInDeploymentMode), which deactivates SectionsRoot outright, so the row later
        /// vanishes on whichever state redraws it next. No exception is thrown anywhere, which is
        /// why nothing shows up in the logs.
        ///
        /// Deployment mode is over whichever way the screen is left, so clear it on the way out.
        /// Running before the original also means ExitState() sees the cleared flag and restores
        /// the bar itself.
        /// </summary>
        [HarmonyPatch(typeof(UIStateRosterDeployment), "ToPreviousScreen")]
        public static class Patch_UIStateRosterDeployment_ToPreviousScreen_ClearDeploymentUiMode
        {
            public static void Prefix()
            {
                try
                {
                    GeoscapeView view = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.View;

                    if (view == null || !view.SetUiInDeploymentMode)
                    {
                        return;
                    }

                    view.SetUiInDeploymentMode = false;

                    TFTVLogger.Always("[RosterDeployment] Cleared SetUiInDeploymentMode on leaving the deployment screen, so the geoscape section bar comes back.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }

}
