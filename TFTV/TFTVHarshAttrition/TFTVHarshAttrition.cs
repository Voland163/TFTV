using PhoenixPoint.Tactical.Entities.Abilities;
using System;

namespace TFTV
{
    /// <summary>
    /// Harsh Attrition: an experimental new-game option (TFTVNewGameOptions.HarshAttritionSetting, saved
    /// with the campaign) that makes wounds and Pandorans harder to play around. Design spec: the
    /// "Harsh Attrition (Experimental) — Design Spec" doc (2026-10-22).
    ///
    /// The parts: the wound rules, all in <see cref="TFTVLimbHitMalus"/> - disabled arms widen spread and cut
    /// melee damage instead of locking weapons, a disabled head keeps half its Willpower but costs Perception
    /// and aim, and hits that get through armour leave a temporary malus on the part; Acheron healing regrowing
    /// an Arthron's right arm as a claw (<see cref="ArthronClawRegrowth"/>); disarmed humanoids who flee coming
    /// back (<see cref="DisarmedReturn"/>); Pandoran stat variance with autopsy/perception-gated information
    /// (<see cref="PandoranVariance"/>).
    /// </summary>
    internal static partial class TFTVHarshAttrition
    {
        internal static bool IsEnabled => TFTVNewGameOptions.HarshAttritionSetting;

        private const string OneHandedGripDescKey = "TFTV_DRILL_onehandedgrip_DESC";
        private const string OneHandedGripHarshDescKey = "TFTV_DRILL_onehandedgrip_HARSH_DESC";

        /// <summary>
        /// Defs that read differently with the option on. Runs with the other option-dependent defs, on
        /// the geoscape and on loading a tactical save, so it follows the campaign being played.
        /// </summary>
        internal static void ImplementOptionDependentDefs()
        {
            try
            {
                // One-Handed Grip no longer lends a hand (every arm keeps its hand); it softens the disabled-arm spread instead.
                PassiveModifierAbilityDef oneHandedGrip = TFTVDrills.DrillsHarmony.OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGrip;

                if (oneHandedGrip?.ViewElementDef?.Description != null)
                {
                    oneHandedGrip.ViewElementDef.Description.LocalizationKey = IsEnabled ? OneHandedGripHarshDescKey : OneHandedGripDescKey;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
