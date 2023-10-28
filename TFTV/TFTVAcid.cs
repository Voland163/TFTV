using Base.Defs;
using Base.Entities;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PRMBetterClasses.SkillModifications;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVAcid
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;



        //Patch to prevent BionicResistances from being removed when Acid applies Disabled Status
        [HarmonyPatch(typeof(ActorComponent), "RemoveAbility", new Type[] { typeof(Ability) })]
        public static class ActorComponent_RemoveAbilitiesFromSource_Patch
        {
            public static bool Prefix(ActorComponent __instance, Ability ability)
            {
                try
                {
                    if (ability.AbilityDef.name == "BionicDamageMultipliers_AbilityDef")
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Prevents acid resistance from reducing amount of Acid applied 
        [HarmonyPatch(typeof(AddStatusDamageKeywordData), "ApplyDamageMultipliersToValue")]
        public static class AddStatusDamageKeywordData_ApplyDamageMultipliersToValue_Patch
        {
            public static void Postfix(AddStatusDamageKeywordData __instance, float value, ref float __result)
            {
                try
                {
                    if ((__instance.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword
                        || __instance.DamageKeywordDef == SkillModsMain.sharedSoloDamageKeywords.SoloAcidKeyword)
                        && __result != 0)
                    {
                        // TFTVLogger.Always($"Applyin acid; setting result to value (current value {value} and result {__result})");
                        __result = value;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        private static bool ApplyTFTVAcidDamage(AcidDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
        {
            try
            {

                if (recv != null)
                {
                    float resistanceToAcid = 1; //as this is a multiplier, this means a resistance of 0%

                    TacticalActor hitActor = recv?.GetActor() as TacticalActor;
                    ItemSlot itemSlot = recv as ItemSlot;
                    ItemSlot additionalSlot = null; //in case Leg                   

                    if (hitActor != null)
                    {
                        if (hitActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                        {
                            //Currently does nothing; was trying to solve acid not disabling some vehicle weapons
                        }
                        else
                        {
                            if (itemSlot != null && itemSlot.DisplayName == "LEG")
                            {
                                additionalSlot = hitActor.BodyState.GetSlot("Legs");
                            }
                        }

                        RemoveElectricReinforcementAndHunkerDown(itemSlot);

                        foreach (var damageMultiplier in hitActor.GetDamageMultipliers(DamageMultiplierType.Incoming, __instance.AcidDamageEffectDef.DamageTypeDef))
                        {
                            resistanceToAcid *= damageMultiplier.GetMultiplier(recv, recv);
                        }
                    }

                    bool num = (float)recv.GetArmor().Value > 1E-05f;
                    float armorDamage = num ? accum.Amount : 0f;
                    float num2 = num ? 0f : (accum.Amount * resistanceToAcid);

                    DamageAccumulation.TargetData data = GetDamageData(num2, recv, __instance, armorDamage, damageOrigin, impactForce, impactHit);

                    DisableElectronics(num2, itemSlot, additionalSlot, recv, __instance);

                    accum.AddGeneratedTarget(data);

                    return false;
                }
                return true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static DamageAccumulation.TargetData GetDamageData
            (float num2, IDamageReceiver recv, AcidDamageEffect acidDamageEffect, float armorDamage, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
        {
            try 
            { 
            return new DamageAccumulation.TargetData
            {
                Target = recv,
                AmountApplied = num2,
                DamageResult = new DamageResult
                {
                    Source = acidDamageEffect.Source,
                    ArmorDamage = armorDamage,
                    HealthDamage = num2,
                    ImpactForce = impactForce,
                    ImpactHit = impactHit,
                    DamageOrigin = damageOrigin,
                    DamageTypeDef = acidDamageEffect.AcidDamageEffectDef.DamageTypeDef
                }
            };


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        /// <summary>
        /// This is to prevent ER and HD from being used to stop acid from doing damage to limb HP.
        /// In practice, we are simply moving up the time when this statuses are supposed to expire to right before acid damage is applied.
        /// </summary>


        private static void RemoveElectricReinforcementAndHunkerDown(ItemSlot itemSlot)
        {
            try
            {
                if (itemSlot != null)
                {
                    ItemSlotStatsModifyStatusDef electricReinforcementStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]");
                    ItemSlotStatsModifyStatusDef hunkerDownStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_ArmourModifier [HunkerDown_AbilityDef]");

                    StatModification electricReinforcementHunkerDownMod =
                        itemSlot.DamageImplementation.GetArmor().GetValueModifications().
                        FirstOrDefault(mod => mod.Source is ItemSlotStatsModifyStatus status &&
                        (status.ItemSlotStatsModifyStatusDef == electricReinforcementStatus || status.ItemSlotStatsModifyStatusDef == hunkerDownStatus));

                    if (electricReinforcementHunkerDownMod != null)
                    {
                        itemSlot.DamageImplementation.GetArmor().RemoveStatModificationsWithSource(electricReinforcementHunkerDownMod.Source);
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        private static void DisableElectronics(float num2, ItemSlot itemSlot, ItemSlot additionalSlot, IDamageReceiver recv, AcidDamageEffect acidDamageEffect)
        {
            try
            {
                SlotStateStatusDef disabled = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicSlotFromAcid_StatusDef");//"DisabledElectronicSlot_StatusDef");
                ItemMaterialTagDef electronicTag = DefCache.GetDef<ItemMaterialTagDef>("Electronic_ItemMaterialTagDef");


                if (num2 > 0 && itemSlot != null && itemSlot.HasDirectGameTag(electronicTag, false))
                {

                    TacticalActor tacticalActor = recv.GetActor() as TacticalActor;

                    tacticalActor.ApplyDamage(new DamageResult
                    {
                        ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = disabled, StatusSource = acidDamageEffect, StatusTarget = itemSlot} }

                    });

                    if (additionalSlot != null)
                    {
                        tacticalActor.ApplyDamage(new DamageResult
                        {
                            ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = disabled, StatusSource = acidDamageEffect, StatusTarget = additionalSlot} }
                        });

                    }
                    //  TFTVLogger.Always("Status should be applied");
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        //Method to change how Acid damage is applied
        [HarmonyPatch(typeof(AcidDamageEffect), "AddTarget")]
        public static class AcidDamageEffect_AddTarget_Patch
        {
            public static bool Prefix(AcidDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {
                    return ApplyTFTVAcidDamage(__instance, target, accum, recv, damageOrigin, impactForce, impactHit);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
    }
}
