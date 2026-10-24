using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVHarshAttrition
    {
        /// <summary>
        /// When an Acheron's healing (Cure Spray, Cure Cloud - anything it heals with) reaches an Arthron whose
        /// right arm is disabled, the arm comes back as a working claw: the arm's parts are healed to full,
        /// and a gun is swapped for a pincer of the same tier, the way Hoplite auto-repair adds parts back.
        /// The Arthron loses its gun for good. A pincer arm is simply restored.
        /// </summary>
        internal static class ArthronClawRegrowth
        {
            private const string RightArmSlotDefName = "Crabman_RightArm_SlotDef";
            private const string HealingDamageTypeName = "Healing_StandardDamageTypeEffectDef";
            private const string RegrownKey = "TFTV_CLAW_REGROWN";

            /// <summary>Right-hand guns and the pincer each one regrows as.</summary>
            private static readonly Dictionary<string, string> PincerForGun = new Dictionary<string, string>
            {
                { "Crabman_RightHand_Gun_WeaponDef", "Crabman_RightHand_Pincer_WeaponDef" },
                { "Crabman_RightHand_Viral_Gun_WeaponDef", "Crabman_RightHand_Pincer_WeaponDef" },
                { "Crabman_RightHand_EliteGun_WeaponDef", "Crabman_RightHand_ElitePincer_WeaponDef" },
                { "Crabman_RightHand_Viral_EliteGun_WeaponDef", "Crabman_RightHand_ElitePincer_WeaponDef" },
            };

            /// <summary>Healed this hit; regrown once the hit has been applied to every target.</summary>
            private static readonly HashSet<TacticalActor> Pending = new HashSet<TacticalActor>();

            private static ClassTagDef _acheronTag;
            private static ClassTagDef _crabmanTag;

            private static ClassTagDef AcheronTag => _acheronTag ?? (_acheronTag = TFTVMain.Main.DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef"));
            private static ClassTagDef CrabmanTag => _crabmanTag ?? (_crabmanTag = TFTVMain.Main.DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef"));

            private static ItemSlot GetRightArm(TacticalActor actor)
            {
                return actor.BodyState?.GetHealthSlots()?.FirstOrDefault(slot => slot?.ItemSlotDef != null && slot.ItemSlotDef.name == RightArmSlotDefName);
            }

            private static void OnApplied(DamageAccumulation.TargetData data)
            {
                if (!IsEnabled || data?.Target == null)
                {
                    return;
                }

                DamageResult result = data.DamageResult;
                bool healing = result.HealValue > 0f || result.DamageTypeDef?.name == HealingDamageTypeName;

                if (!healing)
                {
                    return;
                }

                TacticalActorBase source = TacUtil.GetSourceTacticalActorBase(result.Source);

                if (source?.GameTags == null || AcheronTag == null || !source.GameTags.Contains(AcheronTag))
                {
                    return;
                }

                if (!(data.Target.GetActor() is TacticalActor target) || !target.IsAlive || target.GameTags == null || CrabmanTag == null || !target.GameTags.Contains(CrabmanTag))
                {
                    return;
                }

                ItemSlot arm = GetRightArm(target);

                if (arm != null && !arm.Enabled)
                {
                    Pending.Add(target);
                }
            }

            private static void RegrowPending()
            {
                if (Pending.Count == 0)
                {
                    return;
                }

                List<TacticalActor> actors = Pending.ToList();
                Pending.Clear();

                foreach (TacticalActor actor in actors)
                {
                    try
                    {
                        Regrow(actor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void Regrow(TacticalActor actor)
            {
                ItemSlot arm = GetRightArm(actor);

                if (actor == null || !actor.IsAlive || arm == null || arm.Enabled)
                {
                    return;
                }

                // Healing the arm's parts to full re-enables the slot, as any restoration does.
                foreach (TacticalItem item in arm.GetAllDirectItems(false))
                {
                    item?.GetHealth()?.SetToMax();
                }

                Weapon gun = actor.Equipments?.GetWeapons()?.FirstOrDefault(weapon => weapon?.WeaponDef != null && PincerForGun.ContainsKey(weapon.WeaponDef.name));
                string regrown = "pincer restored";

                if (gun != null)
                {
                    string gunName = gun.WeaponDef.name;
                    WeaponDef pincerDef = TFTVMain.Main.DefCache.GetDef<WeaponDef>(PincerForGun[gunName]);

                    if (pincerDef != null)
                    {
                        actor.Equipments.RemoveItem(gun);
                        gun.Destroy();

                        if (actor.Equipments.AddItem(pincerDef, null) is Equipment pincer)
                        {
                            actor.Equipments.SetSelectedEquipment(pincer);
                        }

                        regrown = $"{gunName} -> {pincerDef.name}";
                    }
                }

                actor.RecalculateUsableHands();
                actor.UpdateStats();

                TFTVTauntsAndQuips.FloatingTextManager.ShowFloatingText(actor, TFTVCommonMethods.ConvertKeyToString(RegrownKey), Color.white);
                TFTVLogger.Always($"[HarshAttrition] Acheron healing regrew {actor.name}'s right arm: {regrown}");
            }

            /// <summary>Where one hit's healing lands on one target (also where the limb malus counts hits).</summary>
            [HarmonyPatch(typeof(DamageAccumulation.TargetData), nameof(DamageAccumulation.TargetData.ApplyToTarget))]
            internal static class DamageAccumulation_TargetData_ApplyToTarget_ClawRegrowth_Patch
            {
                private static void Postfix(DamageAccumulation.TargetData __instance)
                {
                    try
                    {
                        OnApplied(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>After the hit has reached every target: equipment can change without disturbing the damage loop.</summary>
            [HarmonyPatch(typeof(DamageAccumulation), nameof(DamageAccumulation.ApplyAddedDamage))]
            internal static class DamageAccumulation_ApplyAddedDamage_ClawRegrowth_Patch
            {
                private static void Postfix()
                {
                    try
                    {
                        RegrowPending();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }
    }
}
