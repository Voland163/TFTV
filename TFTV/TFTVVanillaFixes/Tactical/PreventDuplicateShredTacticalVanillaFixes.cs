using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class PreventDuplicateShredTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(DamageAccumulation), nameof(DamageAccumulation.AddTarget))]
        internal static class DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred
        {
            private const float Epsilon = 1e-5f;

            private static readonly AccessTools.FieldRef<DamageAccumulation, List<DamageAccumulation.TargetData>>
                TargetsDataRef =
                    AccessTools.FieldRefAccess<DamageAccumulation, List<DamageAccumulation.TargetData>>("_targetsData");

            private static void Postfix(DamageAccumulation __instance, IDamageReceiver target)
            {
                // TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Postfix called for target '{target.GetSlotName()}'");

                if (__instance == null || target == null)
                {
                    //  TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] __instance or target is null, skipping.");
                    return;
                }

                // The target may be a DamageTracker named "ARM", not a TacticalItem.
                // Use the receiver contract instead of concrete item classes.
                if (!target.IsBodyPart())
                {
                    // TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Target '{target.GetSlotName()}' is not a body part, skipping.");
                    return;
                }

                object source = __instance.Source;

                // Keep this scoped to projectile accumulation, where duplicate cast hits are the problem.
                if (!(source is Projectile) && !(source is ProjectileLogic))
                {
                    // TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Source is not a projectile, skipping.");   
                    return;
                }

                List<DamageAccumulation.TargetData> targetsData = TargetsDataRef(__instance);
                if (targetsData == null || targetsData.Count < 2)
                {
                    //  TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Not enough targets in accumulation to have duplicates, skipping.");
                    return;
                }

                IDamageReceiver canonicalTarget = target.GetNonTrackerReceiver();

                // TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Checking for duplicate shred on body part '{target.GetSlotName()}' with canonical receiver '{canonicalTarget.GetSlotName()}' across {targetsData.Count} targets in accumulation.");

                bool alreadyAppliedShredToThisBodyPart = false;

                for (int i = 0; i < targetsData.Count; i++)
                {
                    DamageAccumulation.TargetData data = targetsData[i];
                    if (data == null || data.Target == null)
                    {
                        continue;
                    }

                    if (!data.Target.IsBodyPart())
                    {
                        continue;
                    }

                    IDamageReceiver canonicalDataTarget = data.Target.GetNonTrackerReceiver();
                    if (!ReferenceEquals(canonicalDataTarget, canonicalTarget))
                    {
                        continue;
                    }

                    if (!alreadyAppliedShredToThisBodyPart)
                    {
                        if (data.DamageResult.ArmorDamage > Epsilon)
                        {
                            alreadyAppliedShredToThisBodyPart = true;
                        }

                        continue;
                    }

                    if (data.DamageResult.ArmorDamage <= Epsilon)
                    {
                        continue;
                    }

                    // Duplicate same-body-part shred in the same projectile accumulation.
                    // Preserve HP damage/statuses/etc.; only remove the extra armor shred.
                    // TFTVLogger.Always($"[DamageAccumulation_AddTarget_PreventDuplicateBodyPartShred] Found duplicate shred on body part '{data.Target.GetSlotName()}' in target data index {i}, removing armor damage.");
                    data.DamageResult.ArmorDamage = 0f;

                    if (IsNoOpAfterRemovingDuplicateShred(data))
                    {
                        targetsData.RemoveAt(i);
                        i--;
                    }
                }
            }

            private static bool IsNoOpAfterRemovingDuplicateShred(DamageAccumulation.TargetData data)
            {
                DamageResult result = data.DamageResult;

                // Mirrors the game's junk-data logic, which ignores ArmorMitigatedDamage.
                return data.AmountApplied <= Epsilon
                       && result.HealthDamage <= Epsilon
                       && result.ArmorDamage <= Epsilon
                       && result.StunValue <= Epsilon
                       && result.HealValue <= Epsilon
                       && result.ApplyStatuses == null
                       && result.StatModifications == null
                       && result.ActorEffects == null;
            }
        }
    }
}
