using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using Base.Core;
using Base.Defs;
using Base.UI.MessageBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TFTV.TFTVVoidOmensExpansion
{
    /// <summary>
    /// The Void Omens that change what Phoenix runs on the geoscape: aircraft in Mist (VO22), facility building against
    /// base attacks (VO23), captives (VO24), a third augment (VO33) and incidents (VO34).
    /// </summary>
    internal static class GeoscapeOmens
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private const float VO22MaintenanceMultiplier = 2f;
        private const float VO23ConstructionMultiplier = 0.5f;
        internal const float VO23BaseAttackMultiplier = 1.5f;
        private const int VO24MinContainmentInUse = 20;
        private const float VO24MutagenMultiplier = 1.5f;
        internal const float VO24EscapeMultiplier = 1.5f;
        private const int VanillaMaxAugments = 2;
        private const int VO33MaxAugments = 3;
        private const string VO33PerkGrantedVariable = "VO33_ThirdAugmentPerk_";

        /// <summary>Which perk VO33 gave, as its position in TFTVDelirium's perk list + 1 (0 = none), saved with the campaign.</summary>
        private const string VO33PerkIndexVariable = "VO33_ThirdAugmentPerkIndex_";

        /// <summary>The Delirium perk this character keeps for good (VO33's third augment), or null.</summary>
        internal static TacticalAbilityDef GetPermanentPerk(GeoCharacter character)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                if (character == null || controller?.EventSystem == null)
                {
                    return null;
                }

                return TFTVDelirium.DeliriumPerkAt(controller.EventSystem.GetVariable(VO33PerkIndexVariable + character.Id) - 1);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }
        internal const int VO34IncidentFrequency = 2;
        private const float VO34RewardMultiplier = 0.5f;

        /// <summary>What each facility took to build before VO23: restored as it was, not from a hard-coded value.</summary>
        private static readonly Dictionary<PhoenixFacilityDef, float> _constructionDays = new Dictionary<PhoenixFacilityDef, float>();

        internal static float GetMaintenanceMultiplier(GeoVehicle aircraft)
        {
            return TFTVVoidOmens.VoidOmensCheck[22] && AircraftReworkSpeedAndRange.IsAircraftInMist(aircraft) ? VO22MaintenanceMultiplier : 1f;
        }

        internal static void ApplyVO23(GeoLevelController controller)
        {
            foreach (PhoenixFacilityDef facility in Repo.GetAllDefs<PhoenixFacilityDef>())
            {
                if (!_constructionDays.ContainsKey(facility))
                {
                    _constructionDays[facility] = facility.ConstructionTimeDays;
                    facility.ConstructionTimeDays *= VO23ConstructionMultiplier;
                }
            }
        }

        internal static void RevertVO23(GeoLevelController controller)
        {
            foreach (KeyValuePair<PhoenixFacilityDef, float> facility in _constructionDays)
            {
                facility.Key.ConstructionTimeDays = facility.Value;
            }

            _constructionDays.Clear();
        }

        internal static bool ContainmentGate(GeoLevelController controller)
        {
            return controller.PhoenixFaction.ContaimentUsage >= VO24MinContainmentInUse;
        }

        internal static float GetMutagenMultiplier()
        {
            return TFTVVoidOmens.VoidOmensCheck[24] ? VO24MutagenMultiplier : 1f;
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), nameof(GeoPhoenixFaction.GetHarvestingUnitResourceAmount))]
        internal static class GeoPhoenixFaction_GetHarvestingUnitResourceAmount_VO24_Patch
        {
            private static void Postfix(ResourceType returnResource, ref int __result)
            {
                try
                {
                    if (returnResource == ResourceType.Mutagen)
                    {
                        __result = Mathf.RoundToInt(__result * GetMutagenMultiplier());
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>A bionics or mutation lab, and someone with the two augments the omen would add to.</summary>
        internal static bool AugmentGate(GeoLevelController controller)
        {
            return (TFTVAugmentations.CheckForFacility(controller, "KEY_BASE_FACILITY_BIONICSLAB_NAME")
                || TFTVAugmentations.CheckForFacility(controller, "KEY_BASE_FACILITY_MUTATION_LAB_NAME"))
                && controller.PhoenixFaction.Soldiers.Any(s => AugmentScreenUtilities.GetNumberOfAugments(s) >= VanillaMaxAugments);
        }

        internal static int GetMaxAugments()
        {
            return TFTVVoidOmens.VoidOmensCheck[33] ? VO33MaxAugments : VanillaMaxAugments;
        }

        /// <summary>
        /// MAX_AUGMENTATIONS is a const, so both screens carry it as the number 2. The one 2 that is not the limit is
        /// AugumentSlotState.AugumentationLimitReached, stored in local 4 of both methods.
        /// </summary>
        [HarmonyPatch]
        internal static class AugmentScreens_InitCharacterInfo_VO33_Patch
        {
            private const int SlotStateLocal = 4;

            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(UIModuleBionics), "InitCharacterInfo");
                yield return AccessTools.Method(typeof(UIModuleMutate), "InitCharacterInfo");
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                MethodInfo getMaxAugments = AccessTools.Method(typeof(GeoscapeOmens), nameof(GetMaxAugments));

                for (int i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].opcode != OpCodes.Ldc_I4_2)
                    {
                        continue;
                    }

                    if (codes[i + 1].opcode == OpCodes.Stloc_S && (codes[i + 1].operand as LocalBuilder)?.LocalIndex == SlotStateLocal)
                    {
                        continue;
                    }

                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = getMaxAugments;
                }

                return codes;
            }
        }

        [HarmonyPatch(typeof(UIModuleBionics), nameof(UIModuleBionics.OnAugmentApplied))]
        internal static class UIModuleBionics_OnAugmentApplied_VO33_Patch
        {
            private static void Postfix(UIModuleBionics __instance)
            {
                GrantThirdAugmentPerk(__instance.CurrentCharacter, __instance.Context?.Level);
            }
        }

        [HarmonyPatch(typeof(UIModuleMutate), nameof(UIModuleMutate.OnAugmentApplied))]
        internal static class UIModuleMutate_OnAugmentApplied_VO33_Patch
        {
            private static void Postfix(UIModuleMutate __instance)
            {
                GrantThirdAugmentPerk(__instance.CurrentCharacter, __instance.Context?.Level);
            }
        }

        /// <summary>
        /// The perk is the omen's price and stays when the omen ends. The character is not entered for the perk recovery
        /// rolls in TFTVDelirium, and the perk is recorded so that recovery from any other Delirium perk (a treatment one
        /// that enters them after all) skips it. One per character: swapping a third augment does not pay out again.
        /// </summary>
        private static void GrantThirdAugmentPerk(GeoCharacter character, GeoLevelController controller)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[33] || character == null || controller == null
                    || AugmentScreenUtilities.GetNumberOfAugments(character) < VO33MaxAugments
                    || controller.EventSystem.GetVariable(VO33PerkGrantedVariable + character.Id) != 0)
                {
                    return;
                }

                TacticalAbilityDef perk = TFTVDelirium.RollDeliriumPerk(character.GetTacticalAbilities());

                if (perk == null)
                {
                    return;
                }

                controller.EventSystem.SetVariable(VO33PerkGrantedVariable + character.Id, 1);
                controller.EventSystem.SetVariable(VO33PerkIndexVariable + character.Id, TFTVDelirium.DeliriumPerkIndex(perk) + 1);
                character.Progression.AddAbility(perk);

                GameUtl.GetMessageBox().ShowSimplePrompt(
                    TFTVCommonMethods.FormatKey("KEY_DELIRIUM_PERK_PROMPT", character.GetName(), perk.ViewElementDef.DisplayName1.Localize(), perk.ViewElementDef.Description.Localize()),
                    MessageBoxIcon.None, MessageBoxButtons.OK, null);

                TFTVLogger.Always($"VO33: {character.GetName()} gained {perk.name} from a third augment");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>VO34. Only what the player gains is halved, not what an incident costs.</summary>
        internal static void HalveIncidentRewards(string eventId, GeoFactionReward reward)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[34] || reward == null || eventId == null
                    || !eventId.StartsWith("TFTV_INCIDENT_", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ResourcePack halved = new ResourcePack();

                foreach (ResourceUnit resource in reward.Resources)
                {
                    halved.Add(new ResourceUnit(resource.Type, resource.Value > 0f ? resource.Value * VO34RewardMultiplier : resource.Value));
                }

                reward.Resources = halved;

                foreach (RewardDiplomacyChange change in reward.Diplomacy.Where(d => d.Value > 0))
                {
                    change.Value = Mathf.Max(1, Mathf.RoundToInt(change.Value * VO34RewardMultiplier));
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
