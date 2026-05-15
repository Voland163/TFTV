using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
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

        // -----------------------------------------------------------------------
        // Def creation for the Ignore Pain status clones (1 turn per rank).
        // -----------------------------------------------------------------------
        internal static class BiotechIgnorePainDefs
        {
            private const string DiagTag = "[Incidents][BiotechMedkit][Defs]";

            // Index 0 = rank 1 (1 turn), index 1 = rank 2 (2 turns), index 2 = rank 3 (3 turns).
            internal static FreezeAspectStatsStatusDef[] IgnorePainByRank { get; private set; } =
                new FreezeAspectStatsStatusDef[3];

            private static readonly string[] Guids = new string[]
            {
                "a1b2c3d4-0001-4e5f-9abc-000000000001",
                "a1b2c3d4-0001-4e5f-9abc-000000000002",
                "a1b2c3d4-0001-4e5f-9abc-000000000003",
            };

            internal static void CreateDefs()
            {
                try
                {
                    TFTVLogger.Always($"{DiagTag} CreateDefs starting.");

                    FreezeAspectStatsStatusDef source = DefCache.GetDef<FreezeAspectStatsStatusDef>("IgnorePain_StatusDef");
                    if (source == null)
                    {
                        TFTVLogger.Always($"{DiagTag} CreateDefs FAILED: IgnorePain_StatusDef not found.");
                        return;
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        int turns = i + 1;
                        string defName = $"BiotechIgnorePain_Rank{turns}_StatusDef";

                        FreezeAspectStatsStatusDef clone = Helper.CreateDefFromClone(source, Guids[i], defName);
                        clone.DurationTurns = turns;

                        IgnorePainByRank[i] = clone;
                        TFTVLogger.Always($"{DiagTag} Created rank {turns} def: name={clone.name}, DurationTurns={clone.DurationTurns}.");
                    }

                    TFTVLogger.Always($"{DiagTag} CreateDefs complete.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static bool IsOurDef(FreezeAspectStatsStatusDef def)
            {
                if (def == null)
                {
                    return false;
                }

                foreach (FreezeAspectStatsStatusDef d in IgnorePainByRank)
                {
                    if (d == def)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal static Dictionary<string, float> ExportOption2RestoreAppliedSnapshot()
        {
            return HealAbility_BiotechMedkitPatch.ExportOption2RestoreAppliedSnapshot();
        }

        internal static void ImportOption2RestoreAppliedSnapshot(Dictionary<string, float> imported)
        {
            HealAbility_BiotechMedkitPatch.ImportOption2RestoreAppliedSnapshot(imported);
        }

        [HarmonyPatch(typeof(HealAbility))]
        internal static class HealAbility_BiotechMedkitPatch
        {
            private const string DiagTag = "[Incidents][BiotechMedkit]";
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

            // When our Ignore Pain status expires, RecalculateUsableHands was already called
            // while the status was still in the list (inside OnUnapply base), so the disabled
            // arm would still be counted. Re-run it now that the status is gone.
            [HarmonyPatch(typeof(TacStatus), "OnUnapply")]
            private static class TacStatus_OnUnapply_BiotechIgnorePain_Patch
            {
                private static void Postfix(TacStatus __instance)
                {
                    try
                    {
                        if (__instance == null || __instance.BaseDef == null || !(__instance.BaseDef is FreezeAspectStatsStatusDef))
                        {
                            return;
                        }

                        FreezeAspectStatsStatusDef def = __instance.BaseDef as FreezeAspectStatsStatusDef;
                        if (!BiotechIgnorePainDefs.IsOurDef(def))
                        {
                            return;
                        }

                        TacticalActor actor = __instance.TacticalActor;
                        if (actor == null)
                        {
                            TFTVLogger.Always($"{DiagTag} OnUnapply: actor is null.");
                            return;
                        }

                        // Status is now removed — recalculate so disabled arms no longer
                        // count toward usable hands.
                        actor.RecalculateUsableHands();

                        TFTVLogger.Always($"{DiagTag} OnUnapply: {def.name} expired on {actor.name}, usableHands={actor.GetUsableHands()}, hasFreezeStatus={actor.Status.HasStatus<FreezeAspectStatsStatus>()}.");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Activate")]
            private static void Activate_Postfix(HealAbility __instance, object parameter)
            {
                if (!(parameter is TacticalAbilityTarget tacticalAbilityTarget) || !(tacticalAbilityTarget.Actor is TacticalActor))
                {
                    return;
                }

                TacticalActor healer = __instance.TacticalActor;
                TacticalActor target = (TacticalActor)tacticalAbilityTarget.Actor;
                Equipment equipment = __instance.GetSource<Equipment>() ?? healer?.Equipments?.SelectedEquipment;

                ApplyAffinityHealingBonuses(healer, target, equipment);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ShouldReturnTarget")]
            private static void ShouldReturnTarget_Postfix(HealAbility __instance, TacticalActor healer, TacticalActor targetActor, ref bool __result)
            {
                if (__result || healer == null || targetActor == null)
                {
                    return;
                }

                Equipment equipment = __instance.GetSource<Equipment>() ?? healer.Equipments.SelectedEquipment;
                __result = CanApplyAffinityHealingBonuses(healer, targetActor, equipment);
            }

            private static void ApplyAffinityHealingBonuses(TacticalActor healer, TacticalActor targetActor, Equipment equipment)
            {
                ApplyBiotechHealingBonus(healer, targetActor, equipment);
                ApplyMachineryHealingBonus(healer, targetActor);
            }

            private static bool CanApplyAffinityHealingBonuses(TacticalActor healer, TacticalActor targetActor, Equipment equipment)
            {
                return CanApplyBiotechHealingBonus(healer, targetActor, equipment)
                    || CanApplyMachineryHealingBonus(healer, targetActor);
            }

            private static void ApplyBiotechHealingBonus(TacticalActor healer, TacticalActor targetActor, Equipment equipment)
            {
                int biotechRank = GetBiotechRank(healer);
                if (biotechRank <= 0)
                {
                    return;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Biotech);

                if (selectedOption == 1)
                {
                    if (!IsBiotechOption1Equipment(equipment))
                    {
                        return;
                    }

                    ApplyIgnorePainStatus(targetActor, biotechRank);
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
                    return false;
                }

                int selectedOption = Affinities.AffinityBenefitsChoices.GetTacticalBenefitChoiceFromSnapshot(LeaderSelection.AffinityApproach.Biotech);

                if (selectedOption == 1)
                {
                    if (!IsBiotechOption1Equipment(equipment))
                    {
                        return false;
                    }

                    // Valid target: has a disabled arm AND doesn't already have our status.
                    return HasDisabledHandSlot(targetActor) && !HasOurIgnorePainStatus(targetActor);
                }

                float remaining = GetRemainingDeliriumRestoreCapacity(targetActor, BaseDeliriumWillRestoreAmount * biotechRank);
                return remaining > 0f;
            }

            private static void ApplyIgnorePainStatus(TacticalActor targetActor, int biotechRank)
            {
                if (targetActor == null || biotechRank <= 0)
                {
                    TFTVLogger.Always($"{DiagTag} ApplyIgnorePainStatus: skipped, target={targetActor?.name ?? "<null>"}, rank={biotechRank}.");
                    return;
                }

                int rankIndex = Mathf.Clamp(biotechRank - 1, 0, BiotechIgnorePainDefs.IgnorePainByRank.Length - 1);
                FreezeAspectStatsStatusDef statusDef = BiotechIgnorePainDefs.IgnorePainByRank[rankIndex];

                TFTVLogger.Always($"{DiagTag} ApplyIgnorePainStatus PRE: target={targetActor.name}, rank={biotechRank}, statusDef={statusDef?.name ?? "<null>"}, usableHands={targetActor.GetUsableHands()}, hasFreezeStatus={targetActor.Status.HasStatus<FreezeAspectStatsStatus>()}.");

                if (statusDef == null)
                {
                    TFTVLogger.Always($"{DiagTag} ApplyIgnorePainStatus: statusDef is null — IgnorePainByRank not initialized.");
                    return;
                }

                targetActor.Status.ApplyStatus(statusDef);

                // RecalculateUsableHands() fires inside OnApply before the status is registered,
                // so HasStatus<FreezeAspectStatsStatus>() returns false at that point.
                // Re-run now that ApplyStatus() has returned and the status IS in the list.
                targetActor.RecalculateUsableHands();

                TFTVLogger.Always($"{DiagTag} ApplyIgnorePainStatus POST: target={targetActor.name}, usableHands={targetActor.GetUsableHands()}, hasFreezeStatus={targetActor.Status.HasStatus<FreezeAspectStatsStatus>()}, DurationTurns={statusDef.DurationTurns}.");
            }

            private static bool HasDisabledHandSlot(TacticalActor targetActor)
            {
                if (targetActor == null)
                {
                    return false;
                }

                foreach (ItemSlot slot in targetActor.BodyState.GetHealthSlots())
                {
                    if (!slot.Enabled && slot.ItemSlotDef != null && slot.ItemSlotDef.ProvidesHand)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasOurIgnorePainStatus(TacticalActor targetActor)
            {
                foreach (FreezeAspectStatsStatusDef def in BiotechIgnorePainDefs.IgnorePainByRank)
                {
                    if (def != null && targetActor.HasStatus(def))
                    {
                        return true;
                    }
                }

                return false;
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
                    return 0;
                }

                for (int i = affinityTrack.Length - 1; i >= 0; i--)
                {
                    PassiveModifierAbilityDef def = affinityTrack[i];
                    PassiveModifierAbility ability = def != null ? actor.GetAbilityWithDef<PassiveModifierAbility>(def) : null;

                    if (ability != null)
                    {
                        return i + 1;
                    }
                }

                return 0;
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