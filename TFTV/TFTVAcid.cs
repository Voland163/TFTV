using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Entities;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTagsTypes;

namespace TFTV
{
    internal class TFTVAcid
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        //DoT Medkit UI adjustments to prevent 0 healing appearing when selecting ability
        [HarmonyPatch(typeof(UIModuleAbilityConfirmationButton), "SetAbility")]
        public static class UIModuleAbilityConfirmationButton_SetAbility_patch
        {
            public static void Postfix(UIModuleAbilityConfirmationButton __instance, TacticalAbility ability)
            {
                try
                {
                    // EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");


                    if (ability.AbilityDef.name == "DoTMedkit")
                    {
                        //   TFTVLogger.Always($"the ability is {ability.AbilityDef.name}");                    
                        __instance.DamageTypeTemplateShort.gameObject.SetActive(false);
                        __instance.DamageTypeTemplateExtended.gameObject.SetActive(false);


                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleWeaponSelection), "SetHealAmount")]
        public static class UIModuleWeaponSelection_SetHealAmount_patch
        {
            public static void Prefix(UIModuleWeaponSelection __instance, ref float amount)
            {
                try
                {
                    if (amount <= 1)
                    {

                        amount = 0;
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

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

                    TFTVLogger.Always($"Removing ability {ability.AbilityDef.name}");

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
                    if (__instance.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
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

        //Method to change how Acid damage is applied
        [HarmonyPatch(typeof(AcidDamageEffect), "AddTarget")]
        public static class AcidDamageEffect_ProcessHealAbilityDef_Patch
        {
            public static bool Prefix(AcidDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {

                    float num3 = 1;

                    TacticalActor hitActor = recv?.GetActor() as TacticalActor;
                    ItemSlot itemSlot = recv as ItemSlot;
                    ItemSlot additionalSlot = null; //in case Leg                   

                    ItemSlotStatsModifyStatusDef electricReinforcementStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]");
                    ItemSlotStatsModifyStatusDef hunkerDownStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_ArmourModifier [HunkerDown_AbilityDef]");

                    //  ItemSlotStatsModifyStatus
                    if (hitActor != null)
                    {
                        if (hitActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                        {
                          /*  TFTVLogger.Always($"Damaging vehicle {hitActor.name} with acid; slots # {hitActor.BodyState.GetSlots().Count()}");
             
                            foreach(ItemSlot itemSlot1 in hitActor.BodyState.GetSlots()) 
                            {
                                TFTVLogger.Always($"{hitActor.name} item slot {itemSlot1.DisplayName} and slot name is {itemSlot1.GetSlotName()}");
                            
                            }*/


                        }
                        else

                        {

                            if (itemSlot != null && itemSlot.DisplayName == "LEG")
                            {
                                additionalSlot = hitActor.BodyState.GetSlot("Legs");
                            }
                           

                        }

                        TFTVLogger.Always($"Affected itemslot is {itemSlot?.GetSlotName()} and additionalslot is {additionalSlot?.GetSlotName()}");

                       


                        if (itemSlot != null)
                        {
                            StatModification electricReinforcementHunkerDownMod = itemSlot.DamageImplementation.GetArmor().GetValueModifications().FirstOrDefault(mod => mod.Source is ItemSlotStatsModifyStatus status && (status.ItemSlotStatsModifyStatusDef == electricReinforcementStatus|| status.ItemSlotStatsModifyStatusDef ==hunkerDownStatus));

                            if (electricReinforcementHunkerDownMod != null)
                            {
                                itemSlot.DamageImplementation.GetArmor().RemoveStatModificationsWithSource(electricReinforcementHunkerDownMod.Source);
                            }
                        }



                        foreach (var damageMultiplier in hitActor.GetDamageMultipliers(DamageMultiplierType.Incoming, __instance.AcidDamageEffectDef.DamageTypeDef))
                        {
                            // TFTVLogger.Always($"multiplier is {damageMultiplier.GetMultiplier(recv, recv)} ");
                            num3 *= damageMultiplier.GetMultiplier(recv, recv);
                        }

                        TFTVLogger.Always($"{hitActor.name} has {num3} total acid damage resistance");


                    }

                    bool num = (float)recv.GetArmor().Value > 1E-05f;
                    float armorDamage = num ? accum.Amount : 0f;
                    // float armorDamage = (num ? (accum.Amount * accum.GetSourceDamageMultiplierForReceiver(recv)) : 0f);
                    float num2 = num ? 0f : (accum.Amount * num3);
                    TFTVLogger.Always($"damage to armor from acid is {armorDamage}; damage to HP is {num2}");

                    DamageAccumulation.TargetData data = new DamageAccumulation.TargetData
                    {
                        Target = recv,
                        AmountApplied = num2,
                        DamageResult = new DamageResult
                        {
                            Source = __instance.Source,
                            ArmorDamage = armorDamage,
                            HealthDamage = num2,
                            ImpactForce = impactForce,
                            ImpactHit = impactHit,
                            DamageOrigin = damageOrigin,
                            DamageTypeDef = __instance.AcidDamageEffectDef.DamageTypeDef
                        }
                    };
                    SlotStateStatusDef disabled = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicSlot_StatusDef");
                    ItemMaterialTagDef electronicTag = DefCache.GetDef<ItemMaterialTagDef>("Electronic_ItemMaterialTagDef");


                 


                    if (num2 > 0 && itemSlot.HasDirectGameTag(electronicTag, false))
                    {

                        TacticalActor tacticalActor = recv.GetActor() as TacticalActor;

                   /*     object additionalTarget = additionalSlot;

                        if (itemSlot?.SlotDef == DefCache.GetDef<ItemSlotDef>("PX_Scarab_Turret_SlotDef"))
                        {
                            foreach (TacticalItem tacticalItem in itemSlot.GetAllDirectItems())
                            {
                                TFTVLogger.Always($"in {itemSlot.GetSlotName()} tactical itemDef name {tacticalItem.ItemDef.name}");
                                additionalTarget = tacticalItem;

                            }

                        }*/



                        tacticalActor.ApplyDamage(new DamageResult
                        {
                            ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = disabled, StatusSource = __instance, StatusTarget = itemSlot} }

                        });

                        if (additionalSlot != null)
                        {
                            tacticalActor.ApplyDamage(new DamageResult
                            {
                                ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = disabled, StatusSource = __instance, StatusTarget = additionalSlot} }
                            });

                        }


                        //  TFTVLogger.Always("Status should be applied");
                    }
                    accum.AddGeneratedTarget(data);

                    return false;
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
