using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.TFTVBaseRework;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class BiotechMedkit
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static Dictionary<string, float> ExportOption2RestoreAppliedSnapshot()
        {
            return HealAbility_BiotechMedkitPatch.ExportOption2RestoreAppliedSnapshot();
        }

        internal static void ImportOption2RestoreAppliedSnapshot(Dictionary<string, float> imported)
        {
            if (!BaseReworkUtils.BaseReworkEnabled) return;

            HealAbility_BiotechMedkitPatch.ImportOption2RestoreAppliedSnapshot(imported);
        }

        [HarmonyPatch(typeof(HealAbility))]
        internal static class HealAbility_BiotechMedkitPatch
        {
            private const string DiagTag = "[Incidents][BiotechMedkit]";
            private const float BaseOrganicBodyPartRestoreAmount = 10f;
            private const float BaseDeliriumWillRestoreAmount = 3f;
            private const string DeliriumRestoreSourceName = "BiotechMedkit_DeliriumRestore";
            private static readonly object DeliriumRestoreSource = new object();

            // Per mission: tracks total persistent option-2 restore applied to each actor.
            private static readonly Dictionary<string, float> Option2RestoreAppliedByActorId =
                new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

            [HarmonyPatch(typeof(TacticalLevelController), "OnLevelStart")]
            private static class TacticalLevelController_OnLevelStart_ClearBiotechRestore_Patch
            {
                private static void Postfix()
                {
                    Option2RestoreAppliedByActorId.Clear();
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Activate")]
            private static void Activate_Postfix(HealAbility __instance, object parameter)
            {
                if (!(parameter is TacticalAbilityTarget tacticalAbilityTarget) || !(tacticalAbilityTarget.Actor is TacticalActor))
                {
                    //  TFTVLogger.Always($"{DiagTag} Activate skipped: invalid parameter for {__instance?.HealAbilityDef?.name ?? "<null>"}.");
                    return;
                }

                TacticalActor healer = __instance.TacticalActor;
                TacticalActor target = (TacticalActor)((TacticalAbilityTarget)parameter).Actor;
                Equipment equipment = __instance.GetSource<Equipment>() ?? healer?.Equipments?.SelectedEquipment;

                /*  TFTVLogger.Always(
                      $"{DiagTag} Activate: ability={__instance?.HealAbilityDef?.name ?? "<null>"}, " +
                      $"healer={healer?.name ?? "<null>"}, target={target?.name ?? "<null>"}, " +
                      $"equipment={GetEquipmentDebugName(equipment)}.");*/

                ApplyBiotechHealingBonus(healer, target, equipment);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ShouldReturnTarget")]
            private static void ShouldReturnTarget_Postfix(HealAbility __instance, TacticalActor healer, TacticalActor targetActor, ref bool __result)
            {
                if (__result || healer == null || targetActor == null)
                {
                    /* TFTVLogger.Always(
                         $"{DiagTag} ShouldReturnTarget skipped: baseResult={__result}, " +
                         $"healer={healer?.name ?? "<null>"}, target={targetActor?.name ?? "<null>"}.");*/
                    return;
                }

                Equipment equipment = __instance.GetSource<Equipment>() ?? healer.Equipments.SelectedEquipment;
                /* TFTVLogger.Always(
                     $"{DiagTag} ShouldReturnTarget: ability={__instance?.HealAbilityDef?.name ?? "<null>"}, " +
                     $"healer={healer?.name ?? "<null>"}, target={targetActor?.name ?? "<null>"}, " +
                     $"equipment={GetEquipmentDebugName(equipment)}.");*/

                __result = CanApplyBiotechHealingBonus(healer, targetActor, equipment);
                // TFTVLogger.Always($"{DiagTag} ShouldReturnTarget result: {__result}.");
            }

            private static void ApplyBiotechHealingBonus(TacticalActor healer, TacticalActor targetActor, Equipment equipment)
            {
                int biotechRank = GetBiotechRank(healer);
                if (biotechRank <= 0)
                {
                    // TFTVLogger.Always($"{DiagTag} ApplyBiotechHealingBonus aborted: healer {healer?.name ?? "<null>"} has no Biotech rank.");
                    return;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Biotech);
                /*  TFTVLogger.Always(
                      $"{DiagTag} ApplyBiotechHealingBonus: healer={healer?.name ?? "<null>"}, target={targetActor?.name ?? "<null>"}, " +
                      $"rank={biotechRank}, selectedOption={selectedOption}, equipment={GetEquipmentDebugName(equipment)}.");*/

                if (selectedOption == 1)
                {
                    if (!IsBiotechOption1Equipment(equipment))
                    {
                        //  TFTVLogger.Always($"{DiagTag} ApplyBiotechHealingBonus aborted: equipment is not valid for option 1.");
                        return;
                    }

                    ApplyOrganicBodypartRestore(targetActor, BaseOrganicBodyPartRestoreAmount * biotechRank);
                }
                else
                {
                    ApplyDeliriumWillRestore(targetActor, BaseDeliriumWillRestoreAmount * biotechRank);
                }
            }

            private static bool CanApplyBiotechHealingBonus(TacticalActor healer, TacticalActor targetActor, Equipment equipment)
            {
                int biotechRank = GetBiotechRank(healer);
                if (biotechRank <= 0)
                {
                    // TFTVLogger.Always($"{DiagTag} CanApplyBiotechHealingBonus: healer {healer?.name ?? "<null>"} has no Biotech rank.");
                    return false;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Biotech);
                /* TFTVLogger.Always(
                     $"{DiagTag} CanApplyBiotechHealingBonus: healer={healer?.name ?? "<null>"}, target={targetActor?.name ?? "<null>"}, " +
                     $"rank={biotechRank}, selectedOption={selectedOption}, equipment={GetEquipmentDebugName(equipment)}.");*/

                if (selectedOption == 1)
                {
                    bool validEquipment = IsBiotechOption1Equipment(equipment);
                    bool hasDamagedParts = HasDamagedOrganicBodyParts(targetActor);
                    // TFTVLogger.Always(
                    //     $"{DiagTag} CanApplyBiotechHealingBonus option1: validEquipment={validEquipment}, hasDamagedOrganicBodyParts={hasDamagedParts}.");

                    return validEquipment && hasDamagedParts;
                }

                float remaining = GetRemainingDeliriumRestoreCapacity(targetActor, BaseDeliriumWillRestoreAmount * biotechRank);
                // TFTVLogger.Always($"{DiagTag} CanApplyBiotechHealingBonus option2: remaining={remaining}.");
                return remaining > 0f;
            }

            private static void ApplyMachineryHealingBonus(TacticalActor healer, TacticalActor targetActor)
            {
                int machineryRank = GetMachineryRank(healer);
                if (machineryRank <= 0)
                {
                    return;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Machinery);
                if (selectedOption != 2)
                {
                    return;
                }

                ApplyArmourRestore(targetActor, 4f * machineryRank);
            }

            private static bool CanApplyMachineryHealingBonus(TacticalActor healer, TacticalActor targetActor)
            {
                int machineryRank = GetMachineryRank(healer);
                if (machineryRank <= 0)
                {
                    return false;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Machinery);
                return selectedOption == 2 && HasMissingArmour(targetActor);
            }

            internal static Dictionary<string, float> ExportOption2RestoreAppliedSnapshot()
            {
                return new Dictionary<string, float>(Option2RestoreAppliedByActorId, StringComparer.OrdinalIgnoreCase);
            }

            internal static void ImportOption2RestoreAppliedSnapshot(Dictionary<string, float> imported)
            {
                Option2RestoreAppliedByActorId.Clear();

                if (imported == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, float> pair in imported)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0f)
                    {
                        continue;
                    }

                    Option2RestoreAppliedByActorId[pair.Key] = pair.Value;
                }
            }

            private static string GetActorTrackingKey(TacticalActor targetActor)
            {
                if (targetActor == null || targetActor.GeoUnitId == null || targetActor.GeoUnitId == 0)
                {
                    return null;
                }

                return targetActor.GeoUnitId.ToString();
            }

            private static int GetBiotechRank(TacticalActor actor)
            {
                return GetAffinityRank(actor, Affinities.Biotech);
            }

            private static int GetMachineryRank(TacticalActor actor)
            {
                return GetAffinityRank(actor, Affinities.Machinery);
            }

            private static int GetAffinityRank(TacticalActor actor, PassiveModifierAbilityDef[] affinityTrack)
            {
                if (actor == null || affinityTrack == null || affinityTrack.Length < 3)
                {
                    /* TFTVLogger.Always(
                         $"{DiagTag} GetAffinityRank invalid input: actor={actor?.name ?? "<null>"}, " +
                         $"trackLength={(affinityTrack == null ? 0 : affinityTrack.Length)}.");*/
                    return 0;
                }

                for (int i = affinityTrack.Length - 1; i >= 0; i--)
                {
                    PassiveModifierAbilityDef def = affinityTrack[i];
                    PassiveModifierAbility ability = def != null ? actor.GetAbilityWithDef<PassiveModifierAbility>(def) : null;

                    /*  TFTVLogger.Always(
                          $"{DiagTag} GetAffinityRank check: actor={actor.name}, def={def?.name ?? "<null>"}, found={(ability != null)}.");*/

                    if (ability != null)
                    {
                        // TFTVLogger.Always($"{DiagTag} GetAffinityRank result: {i + 1} for {actor.name}.");
                        return i + 1;
                    }
                }

                //  TFTVLogger.Always($"{DiagTag} GetAffinityRank result: 0 for {actor.name}.");
                return 0;
            }

            private static bool HasDamagedOrganicBodyParts(TacticalActor targetActor)
            {
                if (targetActor == null)
                {
                    //  TFTVLogger.Always($"{DiagTag} HasDamagedOrganicBodyParts: target is null.");
                    return false;
                }

                GameTagDef organicTag = CommonHelpers.GetSharedGameTags().Substances.OrganicTag;
                bool result = false;

                foreach (ItemSlot slot in targetActor.BodyState.GetHealthSlots())
                {
                    StatusStat health = slot.GetHealth();
                    bool damaged = health != null && health.Value < health.Max;
                    bool matchesOrganicTag = organicTag == null || slot.HasDirectGameTag(organicTag, true);

                    /*  TFTVLogger.Always(
                          $"{DiagTag} HasDamagedOrganicBodyParts slot: target={targetActor.name}, " +
                          $"health={(health != null ? health.Value.ToString() : "<null>")}, " +
                          $"max={(health != null ? health.Max.ToString() : "<null>")}, " +
                          $"damaged={damaged}, matchesOrganicTag={matchesOrganicTag}.");*/

                    if (damaged && matchesOrganicTag)
                    {
                        result = true;
                    }
                }

                //TFTVLogger.Always($"{DiagTag} HasDamagedOrganicBodyParts result for {targetActor.name}: {result}.");
                return result;
            }

            private static void ApplyOrganicBodypartRestore(TacticalActor targetActor, float restoreAmount)
            {
                if (targetActor == null || restoreAmount <= 0f)
                {
                    // TFTVLogger.Always(
                    //     $"{DiagTag} ApplyOrganicBodypartRestore skipped: target={targetActor?.name ?? "<null>"}, restoreAmount={restoreAmount}.");
                    return;
                }

                GameTagDef organicTag = CommonHelpers.GetSharedGameTags().Substances.OrganicTag; //DefCache.GetDef<SubstanceTypeTagDef>("Organic_SubstanceTypeTagDef"); // 
                int restoredSlots = 0;

                foreach (ItemSlot itemSlot in targetActor.BodyState.GetHealthSlots())
                {
                    StatusStat health = itemSlot.GetHealth();
                    bool damaged = health != null && health.Value < health.Max;
                    bool matchesOrganicTag = itemSlot.GetAllDirectItems(true).Any(ti => ti.GameTags.Contains(organicTag));

                    /*  TFTVLogger.Always(
                          $"{DiagTag} ApplyOrganicBodypartRestore slot pre-check: target={targetActor.name}, " +
                          $"health={(health != null ? health.Value.ToString() : "<null>")}, " +
                          $"max={(health != null ? health.Max.ToString() : "<null>")}, " +
                          $"damaged={damaged}, matchesOrganicTag={matchesOrganicTag}, restoreAmount={restoreAmount}.");*/

                    if (!damaged || !matchesOrganicTag)
                    {
                        continue;
                    }

                    float before = health.Value;
                    health.Add(restoreAmount);
                    float after = health.Value;
                    restoredSlots++;

                    /*  TFTVLogger.Always(
                          $"{DiagTag} ApplyOrganicBodypartRestore applied: target={targetActor.name}, before={before}, after={after}.");*/
                }

                TFTVLogger.Always($"{DiagTag} ApplyOrganicBodypartRestore finished for {targetActor.name}. RestoredSlots={restoredSlots}.");
            }

            private static void ApplyDeliriumWillRestore(TacticalActor targetActor, float restoreAmount)
            {
                if (targetActor == null || restoreAmount <= 0f)
                {
                    return;
                }

                string actorKey = GetActorTrackingKey(targetActor);
                if (string.IsNullOrEmpty(actorKey))
                {
                    return;
                }

                float alreadyApplied = 0f;
                if (Option2RestoreAppliedByActorId.ContainsKey(actorKey))
                {
                    alreadyApplied = Option2RestoreAppliedByActorId[actorKey];
                }

                float deliriumLossCap = GetCurrentDeliriumWillLoss(targetActor);
                float desiredTotal = Mathf.Min(restoreAmount, deliriumLossCap);
                float delta = desiredTotal - alreadyApplied;
                if (delta <= 0f)
                {
                    return;
                }

                BaseStat willpowerStat = targetActor.CharacterStats.TryGetStat(StatModificationTarget.Willpower);
                if (willpowerStat == null)
                {
                    return;
                }

                willpowerStat.AddStatModification(
                    new StatModification(
                        StatModificationType.Add,
                        DeliriumRestoreSourceName,
                        delta,
                        DeliriumRestoreSource,
                        delta),
                    true);

                willpowerStat.ReapplyModifications();
                targetActor.CharacterStats.WillPoints.AddRestrictedToMax(delta);

                Option2RestoreAppliedByActorId[actorKey] = desiredTotal;

                TFTVLogger.Always($"{DiagTag} Option2 restore applied to {targetActor.name}: cap={deliriumLossCap}, already={alreadyApplied}, delta={delta}, total={desiredTotal}.");
            }

            private static void ApplyArmourRestore(TacticalActor targetActor, float restoreAmount)
            {
                if (targetActor == null || restoreAmount <= 0f)
                {
                    return;
                }

                foreach (ItemSlot itemSlot in targetActor.BodyState.GetSlots())
                {
                    StatusStat armour = itemSlot.GetArmor();
                    if (armour != null && armour.Max > 0f && armour.Value < armour.Max)
                    {
                        armour.Add(restoreAmount);
                    }
                }
            }

            private static float GetRemainingDeliriumRestoreCapacity(TacticalActor targetActor, float maxRestore)
            {
                if (targetActor == null || maxRestore <= 0f)
                {
                    return 0f;
                }

                string actorKey = GetActorTrackingKey(targetActor);
                if (string.IsNullOrEmpty(actorKey))
                {
                    return 0f;
                }

                float alreadyApplied = 0f;
                if (Option2RestoreAppliedByActorId.ContainsKey(actorKey))
                {
                    alreadyApplied = Option2RestoreAppliedByActorId[actorKey];
                }

                float deliriumLossCap = GetCurrentDeliriumWillLoss(targetActor);
                float desiredTotal = Mathf.Min(maxRestore, deliriumLossCap);
                return Mathf.Max(0f, desiredTotal - alreadyApplied);
            }

            private static float GetCurrentDeliriumWillLoss(TacticalActor targetActor)
            {
                if (targetActor == null)
                {
                    return 0f;
                }

                if (TFTVVoidOmens.VoidOmensCheck[3])
                {
                    return 0f;
                }

                int stamina = 40;
                if (TFTVDelirium.StaminaMap != null && TFTVDelirium.StaminaMap.ContainsKey(targetActor.GeoUnitId))
                {
                    stamina = TFTVDelirium.StaminaMap[targetActor.GeoUnitId];
                }

                float wpReduction = Mathf.Max(0f, targetActor.CharacterStats.Corruption);

                if (stamina == 40)
                {
                    wpReduction = Mathf.Max(0f, wpReduction - 4f);
                }
                else if (stamina >= 30 && stamina < 40)
                {
                    wpReduction = Mathf.Max(0f, wpReduction - 3f);
                }
                else if (stamina >= 20 && stamina < 30)
                {
                    wpReduction = Mathf.Max(0f, wpReduction - 2f);
                }
                else if (stamina >= 10 && stamina < 20)
                {
                    wpReduction = Mathf.Max(0f, wpReduction - 1f);
                }

                return wpReduction;
            }

            private static bool IsBiotechOption1Equipment(Equipment equipment)
            {
                return equipment != null
                    && equipment.EquipmentDef != null
                    && (equipment.EquipmentDef.name == "Medkit_EquipmentDef"
                        || equipment.EquipmentDef.name == "VirophageMedkit_EquipmentDef");
            }


            private static bool HasMissingArmour(TacticalActor targetActor)
            {
                if (targetActor == null)
                {
                    return false;
                }

                return targetActor.BodyState.GetSlots().Any((ItemSlot slot) =>
                {
                    StatusStat armour = slot.GetArmor();
                    return armour != null && armour.Max > 0f && armour.Value < armour.Max;
                });
            }
        }
    }
}
