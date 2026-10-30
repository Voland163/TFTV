using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using System;
using System.Collections.Generic;

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
    /// back (<see cref="DisarmedReturn"/>); Pandoran stat variance with research/perception-gated information
    /// (<see cref="PandoranVariance"/>), which replaces the vivisections' damage bonus.
    /// </summary>
    internal static partial class TFTVHarshAttrition
    {
        internal static bool IsEnabled => TFTVNewGameOptions.HarshAttritionSetting;

        private const string OneHandedGripDescKey = "TFTV_DRILL_onehandedgrip_DESC";
        private const string OneHandedGripHarshDescKey = "TFTV_DRILL_onehandedgrip_HARSH_DESC";

        private const string LiveAcheronResearch = "PX_Alien_LiveAcheron_ResearchDef";

        /// <summary>
        /// The researches whose reward is a damage bonus against one species (DamageBonusTo&lt;X&gt;_FactionEffectDef):
        /// the vivisections, and the autopsies of the species that cannot be vivisected. With the option on, their
        /// benefit text says what they reveal instead (Acheron's has a second variant for the Aircraft Rework).
        /// </summary>
        private static readonly Dictionary<string, string> HarshBenefitKeys = new Dictionary<string, string>
        {
            { "PX_Alien_LiveCrabman_ResearchDef", "TFTV_HARSH_LIVECRABMAN_BENEFITS" },
            { "PX_Alien_LiveFishman_ResearchDef", "TFTV_HARSH_LIVEFISHMAN_BENEFITS" },
            { "PX_Alien_LiveSiren_ResearchDef", "TFTV_HARSH_LIVESIREN_BENEFITS" },
            { "PX_Alien_LiveChiron_ResearchDef", "TFTV_HARSH_LIVECHIRON_BENEFITS" },
            { LiveAcheronResearch, "TFTV_HARSH_LIVEACHERON_BENEFITS" },
            { "PX_Alien_LiveQueen_ResearchDef", "TFTV_HARSH_LIVEQUEEN_BENEFITS" },
            { "PX_Alien_LiveSwarmer_ResearchDef", "TFTV_HARSH_LIVESWARMER_BENEFITS" },
            { "PX_Alien_LiveMindfragger_ResearchDef", "TFTV_HARSH_LIVEMINDFRAGGER_BENEFITS" },
            { "PX_Alien_LiveAcidworm_ResearchDef", "TFTV_HARSH_LIVEACIDWORM_BENEFITS" },
            { "PX_Alien_LiveFireworm_ResearchDef", "TFTV_HARSH_LIVEFIREWORM_BENEFITS" },
            { "PX_Alien_LivePoisonworm_ResearchDef", "TFTV_HARSH_LIVEPOISONWORM_BENEFITS" },
            { "PX_Alien_HatchingSentinel_ResearchDef", "TFTV_HARSH_HATCHINGSENTINEL_BENEFITS" },
            { "PX_Alien_MistSentinel_ResearchDef", "TFTV_HARSH_MISTSENTINEL_BENEFITS" },
            { "PX_Alien_TerrorSentinel_ResearchDef", "TFTV_HARSH_TERRORSENTINEL_BENEFITS" },
            { "PX_Alien_Spawnery_ResearchDef", "TFTV_HARSH_SPAWNERY_BENEFITS" },
        };

        /// <summary>The key each of those researches bound before the option touched it, to put back with the option off.</summary>
        private static readonly Dictionary<string, string> OriginalBenefitKeys = new Dictionary<string, string>();

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

                SetResearchBenefitTexts();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void SetResearchBenefitTexts()
        {
            foreach (KeyValuePair<string, string> pair in HarshBenefitKeys)
            {
                ResearchDef research = TFTVMain.Main.DefCache.GetDef<ResearchDef>(pair.Key);

                if (research?.ViewElementDef?.BenefitsText == null)
                {
                    continue;
                }

                if (!OriginalBenefitKeys.ContainsKey(pair.Key))
                {
                    OriginalBenefitKeys[pair.Key] = research.ViewElementDef.BenefitsText.LocalizationKey;
                }

                string harshKey = pair.Key == LiveAcheronResearch && TFTVAircraftReworkMain.AircraftReworkOn
                    ? pair.Value + "_AIRCRAFT_REWORK"
                    : pair.Value;

                research.ViewElementDef.BenefitsText.LocalizationKey = IsEnabled ? harshKey : OriginalBenefitKeys[pair.Key];
            }
        }

        /// <summary>
        /// A per-species damage bonus (DamageBonusTo&lt;X&gt;_FactionEffectDef, given by the vivisections and by the
        /// sentinel and spawnery autopsies): research is about knowing the Pandorans under this option, not hitting
        /// them harder. The bonus against all aliens (the moon project) and against humans are left alone.
        /// </summary>
        private static bool IsSpeciesDamageBonus(TacticalFactionEffectDef def)
        {
            string name = def?.name;

            return name != null
                && name.StartsWith("DamageBonusTo", StringComparison.Ordinal)
                && name != "DamageBonusToAliens_FactionEffectDef"
                && name != "DamageBonusToHuman_FactionEffectDef";
        }

        /// <summary>Every faction effect reaches each soldier through TriggerForActor (at mission start, turn starts and entering play).</summary>
        [HarmonyPatch(typeof(TacticalFactionEffect), "TriggerForActor")]
        internal static class TacticalFactionEffect_TriggerForActor_HarshAttrition_Patch
        {
            private static bool Prefix(TacticalFactionEffect __instance)
            {
                try
                {
                    return !(IsEnabled && IsSpeciesDamageBonus(__instance.TacticalFactionEffectDef));
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
