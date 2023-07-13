using Base.Defs;
using Base.Entities;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Input;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.UI.Abilities;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace TFTV
{
    internal class TFTVExperimental
    {

        internal static Color purple = new Color32(149, 23, 151, 255);
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //  private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static List<TacticalVoxel> VoxelsOnFire = new List<TacticalVoxel>();

       

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

        /*   [HarmonyPatch(typeof(UIModuleWeaponSelection), "HandleEquipments")]
            public static class UIModuleWeaponSelection_HandleEquipments_patch
           {
               public static void Postfix(UIModuleWeaponSelection __instance, Equipment ____selectedEquipment)
               {
                   try
                   {
                       EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");

                       if (____selectedEquipment.EquipmentDef==repairKit) 
                       {
                           TFTVLogger.Always("got here");
                           __instance.DamageTypeVisualsTemplate.DamageTypeIcon.gameObject.SetActive(false);
                           __instance.DamageTypeVisualsTemplate.DamageText.gameObject.SetActive(false);


                       }



                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/


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


        // UIModuleAbilities

        // SlotStateStatus

        /* [HarmonyPatch(typeof(TacticalActorBase), "GetDamageMultiplierFor")]
         public static class TacticalActorBase_GetDamageMultiplierFor_patch
         {
             public static void Postfix(TacticalActorBase __instance, ref float __result, DamageTypeBaseEffectDef damageType)
             {
                 try
                 {
                     AcidDamageTypeEffectDef acidDamageTypeEffectDef = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                     if (damageType == acidDamageTypeEffectDef)
                     {
                         TFTVLogger.Always($"GetDamageMultiplierFor  {__instance.name} and result is {__result}");
                         __result = 1;
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetPureDamageBonusFor")]
          public static class DamageAccumulation_GetPureDamageBonusFor_patch
          {
              public static void Postfix(DamageAccumulation __instance, IDamageReceiver target, float __result)
              {
                  try
                  {
                      if (__result != 0)
                      {

                          TFTVLogger.Always($"GetPureDamageBonusFor {target.GetDisplayName()}, result is {__result}");
                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /*     [HarmonyPatch(typeof(AddStatusDamageKeywordData), "ProcessKeywordDataInternal")]
          public static class AddStatusDamageKeywordData_ProcessKeywordDataInternal_Patch
          {
              public static void Postfix(AddStatusDamageKeywordData __instance, DamageAccumulation.TargetData data)
              {
                  try
                  {
                      if (__instance.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                      {
                          TFTVLogger.Always($"target {data.Target.GetSlotName()}");

                          if (data.Target is ItemSlot)
                          {

                              ItemSlot itemSlot = (ItemSlot) data.Target;

                              if (itemSlot.DisplayName == "LEG")
                              {
                                  TacticalActor tacticalActor = data.Target.GetActor() as TacticalActor;

                                  itemSlot = tacticalActor.BodyState.GetSlot("Legs");
                                  TFTVLogger.Always($"itemslot name now {itemSlot.GetSlotName()}");
                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }
                              else
                              {
                                  TFTVLogger.Always($"target {data.Target.GetSlotName()} is itemslot {itemSlot.DisplayName}");

                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }

                          }


                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }

          */

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


        /*   [HarmonyPatch(typeof(DamageKeyword), "AddKeywordStatus")]
             public static class DamageOverTimeResistanceStatus_ApplyResistance_Patch
             {
                 public static void Postfix(IDamageReceiver recv, DamageAccumulation.TargetData data, StatusDef statusDef, int value, object customStatusTarget = null)
                 {
                     try
                     {


                       TFTVLogger.Always($"AddKeywordStatus value {value}");


                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }
             }*/




        /* [HarmonyPatch(typeof(DamageAccumulation), "AddTargetStatus")]
         public static class DamageAccumulation_AddTargetStatus_Patch
         {
             public static void Prefix(DamageAccumulation __instance, StatusDef statusDef, int tacStatusValue, IDamageReceiver target)
             {
                 try
                 {


                     if (statusDef == DefCache.GetDef<AcidStatusDef>("Acid_StatusDef"))
                     {




                         TFTVLogger.Always($"tacstatusvalue is {tacStatusValue}");
                     }


                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*  [HarmonyPatch(typeof(DamageOverTimeStatus), "GetDamageMultiplier")]
          public static class DamageOverTimeStatus_GetDamageMultiplier_Patch
          {
              public static void Postfix(DamageOverTimeStatus __instance, ref float __result)
              {
                  try
                  {
                      TFTVLogger.Always($"GetDamageMultiplier for {__instance.DamageOverTimeStatusDef.name} and result is {__result}");

                      AcidStatusDef acidDamage = DefCache.GetDef<AcidStatusDef>("Acid_StatusDef");

                      if (__instance.DamageOverTimeStatusDef == acidDamage) 
                      {
                          TFTVLogger.Always($"dot status acid {__result}");
                          __result = 1;
                          TFTVLogger.Always($"new dot status acid {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetSourceDamageMultiplier")]
          public static class DamageAccumulation_GetSourceDamageMultiplier_Patch
          {
              public static void Postfix(DamageAccumulation __instance, DamageTypeBaseEffectDef damageType, float __result)
              {
                  try
                  {
                      if (!damageType.name.Equals("Projectile_StandardDamageTypeEffectDef"))
                          {

                          TFTVLogger.Always($"source actor {__instance?.SourceActor?.name} damageType is {damageType.name} and multiplier is {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

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

                    // TFTVLogger.Always($"Removing ability {ability.AbilityDef.name}");

                    return true;



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(AcidDamageEffect), "AddTarget")]
        public static class AcidDamageEffect_ProcessHealAbilityDef_Patch
        {
            public static bool Prefix(AcidDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {
                    //   DamageMultiplierStatusDef scyllaResistance = DefCache.GetDef<DamageMultiplierStatusDef>("ScyllaDamageResistance");
                    float num3 = 1;

                    TacticalActor hitActor = recv?.GetActor() as TacticalActor;
                    ItemSlot itemSlot = recv as ItemSlot;
                    ItemSlot additionalSlot = null; //in case Leg                   

                    ItemSlotStatsModifyStatusDef electricReinforcementStatus = DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]");

                  //  ItemSlotStatsModifyStatus
                    if (hitActor != null) 
                    {
                        if (itemSlot!=null && itemSlot.DisplayName == "LEG")
                        {
                            additionalSlot = hitActor.BodyState.GetSlot("Legs");
                        }
                        TFTVLogger.Always($"itemslot is {itemSlot?.GetSlotName()} and additionalslot is {additionalSlot?.GetSlotName()}");

                        if (itemSlot != null) 
                        {
                            StatModification electricReinforcementMod= itemSlot.DamageImplementation.GetArmor().GetValueModifications().FirstOrDefault(mod => mod.Source is ItemSlotStatsModifyStatus status && status.ItemSlotStatsModifyStatusDef == electricReinforcementStatus);

                            if (electricReinforcementMod != null)
                            {
                                itemSlot.DamageImplementation.GetArmor().RemoveStatModificationsWithSource(electricReinforcementMod.Source);
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
                  
              
                    if (num2 > 0)
                    {
                       
                       

                        TacticalActor tacticalActor = recv.GetActor() as TacticalActor;

                       

                        /*   if (itemSlot.DisplayName == "ARM")
                           {
                               additionalSlot = tacticalActor.BodyState.GetSlot("Torso");
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


        /*    [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbilityDef")]
            public static class AbilitySummaryData_ProcessHealAbilityDef_Patch
            {
                public static void Postfix(AbilitySummaryData __instance, HealAbilityDef healAbilityDef)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbilityDef running");
                        if ((bool)healAbilityDef.GeneralHealSummary && healAbilityDef.GeneralHealAmount > 0f)
                        {
                            TFTVLogger.Always($"{healAbilityDef.GeneralHealSummary} and {healAbilityDef.GeneralHealAmount}");

                        }

                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbility")]
            public static class AbilitySummaryData_ProcessHealAbility_Patch
            {
                public static void Prefix(AbilitySummaryData __instance, HealAbility healAbility)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbility running");
                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                        if (__instance.Keywords.Count() > 0)
                        {
                            KeywordData keywordData = __instance.Keywords.First((KeywordData kd) => kd.Id == "GeneralHeal");

                            if (keywordData == null)
                            {
                                TFTVLogger.Always("somehow null!");
                            }
                        }



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        */


        /*   internal virtual void TriggerHurt(DamageResult damageResult)
           {
               var hurtReactionAbility = GetAbility<TacticalHurtReactionAbility>();
               if (IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
               {
                   return;
               }

               bool useModFlinching = true; // Use a global flag for the mod 
               if (useModFlinching && _ragdollDummy != null && _ragdollDummy.CanFlinch)
               {
                   DoTriggerHurt(damageResult, damageResult.forceHurt);
                   return;
               }

               _pendingHurtDamage = damageResult;
               if (_waitingForHurtReactionCrt == null || _waitingForHurtReactionCrt.Stopped)
               {
                   _waitingForHurtReactionCrt = Timing.Start(PollForPendingHurtReaction(damageResult.forceHurt));
               }
           }*/


        /*
       [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static bool Prefix(TacticalActor __instance, DamageResult damageResult, RagdollDummy ____ragdollDummy, IUpdateable ____waitingForHurtReactionCrt,
                DamageResult ____pendingHurtDamage)
            {
                try
                {


                    MethodInfo doTriggerHurtMethod = typeof(TacticalActor).GetMethod("DoTriggerHurt", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo pollForPendingHurtReaction = typeof(TacticalActor).GetMethod("PollForPendingHurtReaction", BindingFlags.NonPublic | BindingFlags.Instance); 



                 var hurtReactionAbility = __instance.GetAbility<TacticalHurtReactionAbility>();



                    if (__instance.IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
                    {
                        TFTVLogger.Always("Early exit triggers");
                        return true;
                    }

                    bool useModFlinching = true; // Use a global flag for the mod 
                    if (useModFlinching && ____ragdollDummy != null && ____ragdollDummy.CanFlinch)
                    {
                        doTriggerHurtMethod.Invoke(__instance, new object[] { damageResult, damageResult.forceHurt });
                        TFTVLogger.Always("Takes to do trigger hurt method");

                        return false;
                    }

                    ____pendingHurtDamage = damageResult;
                    if (____waitingForHurtReactionCrt == null || ____waitingForHurtReactionCrt.Stopped)
                    {
                        TFTVLogger.Always("waiting for hurt reaction or it is stopped");
                        object[] parameters = new object[] { damageResult.forceHurt };
                        //Timing timingInstance = new Timing();
                        ____waitingForHurtReactionCrt = __instance.Timing.Start((IEnumerator<NextUpdate>)pollForPendingHurtReaction.Invoke(__instance, parameters));

                    }


                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(TacticalActor), "SetFlinchingEnabled")]
        public static class TacticalActor_AddFlinch_Patch
        {
            public static void Postfix(TacticalActor __instance, ref RagdollDummy ____ragdollDummy)
            {
                try
                {
                    TFTVLogger.Always($"SetFlinchingEnabled invoked");
                    ____ragdollDummy.SetFlinchingEnabled(true);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(RagdollDummy), "AddFlinch")]
        public static class RagdollDummy_AddFlinch_Patch
        {
            public static void Prefix(RagdollDummy __instance, float ____ragdollBlendTimeTotal)
            {
                try
                {
                    TFTVLogger.Always($"AddFlinch invoked prefix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            public static void Postfix(RagdollDummy __instance, float ____ragdollBlendTimeTotal, Vector3 force, CastHit hit)
            {
                try
                {
                    RagdollDummyDef ragdollDummyDef = DefCache.GetDef<RagdollDummyDef>("Generic_RagdollDummyDef");
                    TFTVLogger.Always($"AddFlinch invoked postfix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}. original force is {force}, the hit body part is {hit.Collider?.attachedRigidbody?.name}" +
                        $" mass is {hit.Collider?.attachedRigidbody?.mass}, force applied on first hit is {force*ragdollDummyDef.FlinchForceMultiplier}");







                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(RagdollDummy), "get_CanFlinch")]
        public static class RagdollDummy_SetFlinchingEnabled_Patch
        {
            public static void Postfix(RagdollDummy __instance, ref bool __result)
            {
                try
                {
                    TFTVLogger.Always($"get_CanFlinch invoked for {__instance?.Actor?.name} and result is {__result}");

                    __result = true;

                    TFTVLogger.Always($"And now result is {__result}");




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
        */



        //Make bullets go through Umbra and Decoy, method by Dimitar "Codemite" Evtimov from Snapshot Games

        public static IDamageReceiver GetDamageReceiver(DamagePredictor predictor, GameObject gameObject, Vector3 pos, Quaternion rot)
        {
            IDamageable damageableObject = gameObject.GetComponentInParent<IDamageable>();
            if (damageableObject == null)
            {
                return null;
            }

            IDamageReceiver recv = damageableObject.GetDamageReceiverForHit(pos, rot * Vector3.forward); ;
            if (predictor != null)
            {
                recv = predictor.GetPredictingReceiver(recv);
            }

            return recv;
        }

        public static Dictionary<Projectile, List<TacticalActor>> projectileActor = new Dictionary<Projectile, List<TacticalActor>>();

        [HarmonyPatch(typeof(ProjectileLogic), "OnProjectileHit")]

        public static class ProjectileLogic_OnProjectileHit_Umbra_Patch
        {
            public static bool Prefix(ProjectileLogic __instance, ref bool __result, DamageAccumulation ____damageAccum, CastHit hit, Vector3 dir)
            {
                try
                {

                    Vector3 pos = hit.Point;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    IDamageReceiver receiver = GetDamageReceiver(__instance.Predictor, hit.Collider.gameObject, pos, rot);


                    ClassTagDef umbraClassTag = DefCache.GetDef<ClassTagDef>("Umbra_ClassTagDef");
                    SpawnedActorTagDef decoy = DefCache.GetDef<SpawnedActorTagDef>("Decoy_SpawnedActorTagDef");


                    if (__instance.Predictor != null)
                    {
                        receiver = __instance.Predictor.GetPredictingReceiver(receiver);
                    }

                    TacticalActor hitActor = receiver?.GetActor() as TacticalActor;

                    if (hitActor != null && __instance.Projectile != null && (hitActor.HasGameTag(umbraClassTag) || hitActor.HasGameTag(decoy)))
                    {
                        __result = false;

                        if (projectileActor.ContainsKey(__instance.Projectile) && projectileActor[__instance.Projectile] != null && projectileActor[__instance.Projectile].Contains(hitActor))
                        {

                            __instance.Projectile.OnProjectileHit(hit);
                            ____damageAccum?.ResetToInitalAmount();

                        }
                        else
                        {


                            __instance.Projectile.OnProjectileHit(hit);


                            MethodInfo affectTargetMethod = typeof(ProjectileLogic).GetMethod("AffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                            affectTargetMethod.Invoke(__instance, new object[] { hit, dir });

                            ____damageAccum?.ResetToInitalAmount();

                            if (projectileActor.ContainsKey(__instance.Projectile))
                            {
                                projectileActor[__instance.Projectile].Add(hitActor);
                            }
                            else
                            {
                                projectileActor.Add(__instance.Projectile, new List<TacticalActor> { hitActor });
                            }


                        }
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

        //Makes D-Coy disappear instead of dying

        [HarmonyPatch(typeof(DieAbility), "Activate")]
        public static class DieAbility_Activate_Decoy_Patch
        {
            public static bool Prefix(DieAbility __instance)
            {
                try
                {


                    // TFTVLogger.Always("DieTriggered");
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");

                    if (!__instance.TacticalActorBase.IsObject() && __instance.TacticalActorBase.ActorDef == dcoy)
                    {
                        // TFTVLogger.Always("It's a decoy!");
                        __instance.TacticalActor.gameObject.SetActive(false);
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


        //D-Coy patch to remove if attacked by "smart" enemy
        [HarmonyPatch(typeof(TacticalLevelController), "ActorDamageDealt")]
        public static class TacticalLevelController_ActorDamageDealt_Decoy_Patch
        {
            public static void Postfix(TacticalActor actor, IDamageDealer damageDealer)
            {
                try
                {
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");
                    ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                    ClassTagDef tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                    ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                    GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");


                    if (actor.IsAlive)
                    {
                        if (actor.TacticalActorDef == dcoy && damageDealer != null && damageDealer.GetTacticalActorBase() != null)
                        {
                            TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                            TacticalActor attacker = attackerBase as TacticalActor;

                            if (!attacker.IsControlledByPlayer)
                            {
                                //Decoy despawned if attacked by Siren or Scylla
                                if (attacker.GameTags.Contains(sirenTag) || attacker.GameTags.Contains(queenTag)
                                    || attacker.GameTags.Contains(hopliteTag) || attacker.GameTags.Contains(cyclopsTag))
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                }
                                //Decoy despawned if attacked within 5 tiles by human, triton or acheron
                                else if ((attacker.GameTags.Contains(tritonTag)
                                    || attacker.GameTags.Contains(humanTag)
                                    || attacker.GameTags.Contains(acheronTag))
                                    && (actor.Pos - attacker.Pos).magnitude <= 5)
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        public static Vector3 FindPushToTile(TacticalActor attacker, TacticalActor defender, int numTiles)
        {

            try
            {


                Vector3 diff = defender.Pos - attacker.Pos;
                Vector3 pushToPosition = defender.Pos + numTiles * diff.normalized;

                // TFTVLogger.Always($"attacker position is {attacker.Pos} and defender position is {defender.Pos}, so difference is {diff} and pushtoposition is {pushToPosition}");



                return pushToPosition;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        /*  [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

          public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
          {
              public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {
                              TFTVLogger.Always($"got here, target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                              if (tacticalActor != null)
                              {
                                  tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                     TFTVLogger.Always($"got here, added {knockBackAbility.name} to {tacticalActor.name}");
                              }
                          }
                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

              public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                             TFTVLogger.Always($"got here, ability is {ability.TacticalAbilityDef.name}");

                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {

                              TFTVLogger.Always($"got here, target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                              if (tacticalActor != null && tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                              {
                                  RepositionAbility knockBack = tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility);

                                  IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                  TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                  TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                  foreach (TacticalAbilityTarget target in targets)
                                  {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                      if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 2)).magnitude <= 1f)
                                      {
                                          TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                          pushPosition = target;

                                      }
                                  }


                                  //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                  //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                  //  moveAbility.Activate(pushPosition);

                                  knockBack.Activate(pushPosition);



                                  TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                              }
                          }
                      }

                      if (ability.TacticalAbilityDef == knockBackAbility)
                      {
                          __instance.RemoveAbility(ability);

                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

          }

          */


        /* [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

           public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
           {
               public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {
                      // TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                       if (ability.TacticalAbilityDef!=null && ability.TacticalAbilityDef == strikeAbility)
                       {
                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                           //    TFTVLogger.Always($"got here, target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                               if (tacticalActor != null)
                               {
                                   tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                //   TFTVLogger.Always($"got here, added {knockBackAbility.name} to {tacticalActor.name}");
                               }
                           }
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

               public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {


                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                       if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                       {
                        //   TFTVLogger.Always($"got here, ability is {ability.TacticalAbilityDef.name}");

                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                              // TFTVLogger.Always($"got here, target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                               if (tacticalActor != null && tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                               {
                                   JetJumpAbility knockBack = tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility);

                                   IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                   TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                   TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                   foreach (TacticalAbilityTarget target in targets)  
                                   {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                       if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 1)).magnitude <= 1f) 
                                       {
                                           TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                           pushPosition = target;

                                       }
                                   }


                                   //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                   //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                   //  moveAbility.Activate(pushPosition);

                                   knockBack.Activate(pushPosition);



                                   TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                               }
                           }
                       }

                       if (ability.TacticalAbilityDef == knockBackAbility)
                       {
                           __instance.RemoveAbility(ability);

                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

           }


        */




        [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

        public static class TacticalActor_OnAbilityExecuteFinished_Scylla_Experiment_patch
        {
            public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
            {
                try
                {


                    //    TacticalAbilityTarget target = parameter as TacticalAbilityTarget;
                    //  TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName} and the TacticalAbilityTarget position to apply is {target.PositionToApply} ");

                    ShootAbilityDef scyllaSpit = DefCache.GetDef<ShootAbilityDef>("GooSpit_ShootAbilityDef");
                    ShootAbilityDef scyllaScream = DefCache.GetDef<ShootAbilityDef>("SonicBlast_ShootAbilityDef");

                    //   TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");
                    if (ability.TacticalAbilityDef == scyllaSpit || ability.TacticalAbilityDef == scyllaScream)
                    {
                        StartPreparingShootAbilityDef scyllaStartPreparing = DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef");
                        //    TFTVLogger.Always("Got here");
                        StartPreparingShootAbility startPreparingShootAbility = __instance.GetAbilityWithDef<StartPreparingShootAbility>(scyllaStartPreparing);

                        if (startPreparingShootAbility != null)
                        {
                            startPreparingShootAbility.Activate(parameter);
                        }

                    }



                    ApplyEffectAbilityDef parasychosis = DefCache.GetDef<ApplyEffectAbilityDef>("Parasychosis_AbilityDef");
                    GameTagDef infestationSecondObjectiveTag = DefCache.GetDef<GameTagDef>("ScatterRemainingAttackers_GameTagDef");

                    if (ability.TacticalAbilityDef == parasychosis && parameter is TacticalAbilityTarget target && target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.HasGameTag(infestationSecondObjectiveTag))
                    {
                        //  TFTVLogger.Always($"Got here, target is {tacticalActor.name}");
                        tacticalActor.GameTags.Remove(infestationSecondObjectiveTag);

                    }

                    projectileActor.Clear();


                    /*   SpawnActorAbilityDef DecoyAbility = DefCache.GetDef<SpawnActorAbilityDef>("Decoy_AbilityDef");

                       if (ability.TacticalAbilityDef == DecoyAbility)
                       {                 
                           TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                           tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, parameter, parameter);
                       }*/


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }





        /*  [HarmonyPatch(typeof(AIFaction), "GetActionScore")]

           public static class TFTV_Experimental_AIActionMoveAndEscape_GetModuleBonusByType_AdjustFARMRecuperationModule_patch
           {
               public static void Prefix(AIFaction __instance, AIAction action, IAIActor actor, object context, LazyCache<AIConsiderationDef, AIConsideration> ____considerationsCache)
               {
                   try
                   {
                       if (action.ActionDef.name == "Flee_AIActionDef")
                       {
                           StatusDef autoRepairStatusDef = DefCache.GetDef<StatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
                           TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                           foreach (TacticalActor tacticalActor in tacticalLevelController.GetFactionByCommandName("pu").TacticalActors)
                           {
                               if (tacticalActor.HasStatus(autoRepairStatusDef))
                               {
                                   TFTVLogger.Always($"{tacticalActor.name} has autorepair status");
                               }


                           }


                           float num = action.ActionDef.Weight;
                           TFTVLogger.Always($"get action score for action {action.ActionDef.name} with a weight of {num}");
                           AIAdjustedConsideration[] earlyExitConsiderations = action.ActionDef.EarlyExitConsiderations;

                           foreach (AIAdjustedConsideration aIAdjustedConsideration in earlyExitConsiderations)
                           {

                               if (aIAdjustedConsideration.Consideration == null)
                               {
                                   throw new InvalidOperationException($"Missing consideration for {actor} at {action.ActionDef.name}");
                               }

                               float time = ____considerationsCache.Get(aIAdjustedConsideration.Consideration).Evaluate(actor, null, context);
                               float num2 = aIAdjustedConsideration.ScoreCurve.Evaluate(time);

                               num *= num2;

                               TFTVLogger.Always($"early consideration is {aIAdjustedConsideration.Consideration.name} and num2 is {num2}, so score is now {num}");
                               if (num < 0.0001f)
                               {
                                   TFTVLogger.Always($"aIAdjustedConsideration {aIAdjustedConsideration.Consideration.name} reduced score to nearly 0");
                                   break;

                               }
                           }

                       }



                     /*  if (action.ActionDef.name == "MoveAndQuickAim_AIActionDef")
                       {
                           StatusDef autoRepairStatusDef = DefCache.GetDef<StatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
                           TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                           ApplyStatusAbilityDef quickaim = DefCache.GetDef<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef");

                           foreach (TacticalActor tacticalActor in tacticalLevelController.GetFactionByCommandName("pu").TacticalActors) 
                           {
                               if (tacticalActor.GetAbilityWithDef <ApplyStatusAbility> (quickaim)!=null) 
                               {
                                   TFTVLogger.Always($"{tacticalActor.name} has quickaim ability");
                               }


                           }


                           float num = action.ActionDef.Weight;
                           TFTVLogger.Always($"get action score for action {action.ActionDef.name} with a weight of {num}");
                           AIAdjustedConsideration[] earlyExitConsiderations = action.ActionDef.EarlyExitConsiderations;

                           foreach (AIAdjustedConsideration aIAdjustedConsideration in earlyExitConsiderations)
                           {

                               if (aIAdjustedConsideration.Consideration == null)
                               {
                                   throw new InvalidOperationException($"Missing consideration for {actor} at {action.ActionDef.name}");
                               }

                               float time = ____considerationsCache.Get(aIAdjustedConsideration.Consideration).Evaluate(actor, null, context);
                               float num2 = aIAdjustedConsideration.ScoreCurve.Evaluate(time);

                               num *= num2;

                               TFTVLogger.Always($"early consideration is {aIAdjustedConsideration.Consideration.name} and num2 is {num2}, so score is now {num}");
                               if (num < 0.0001f)
                               {
                                   TFTVLogger.Always($"aIAdjustedConsideration {aIAdjustedConsideration.Consideration.name} reduced score to nearly 0");
                                   break;

                               }
                           }

                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }
           }*/


        [HarmonyPatch(typeof(GeoVehicle), "GetModuleBonusByType")]

        public static class TFTV_Experimental_GeoVehicle_GetModuleBonusByType_AdjustFARMRecuperationModule_patch
        {
            public static void Postfix(GeoVehicleModuleDef.GeoVehicleModuleBonusType type, ref float __result, GeoVehicle __instance)
            {
                try
                {
                    GeoVehicleEquipment hybernationPods = __instance.Modules?.FirstOrDefault(gve => gve != null && gve.ModuleDef != null && gve.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation);

                    if (hybernationPods != null && type == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation)
                    {
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (config.ActivateStaminaRecuperatonModule)
                        {
                            TFTVLogger.Always($"geovehicle is {__instance.name}");
                            __result = 0.35f;

                        }
                        else
                        {

                            __result = 0.0f;

                        }

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }




        [HarmonyPatch(typeof(GeoHavenLeader), "CanRecruitWithFaction")]

        public static class TFTV_Experimental_GeoHavenLeader_CanRecruitWithFaction_EnableRecruitingWhenNotAtWar_patch
        {
            public static void Postfix(GeoHavenLeader __instance, IDiplomaticParty faction, ref bool __result)
            {
                try
                {
                    MethodInfo getRelationMethod = AccessTools.Method(typeof(GeoHavenLeader), "GetRelationWith");
                    PartyDiplomacy.Relation relation = (PartyDiplomacy.Relation)getRelationMethod.Invoke(__instance, new object[] { faction });

                    __result = relation.Diplomacy > -50;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        /* [HarmonyPatch(typeof(GeoHavenLeader), "CanTradeWithFaction")]

         public static class TFTV_Experimental_GeoHavenLeader_CanTradeWithFaction_EnableTradingWhenNotAtWar_patch
         {
             public static void Postfix(GeoHavenLeader __instance, IDiplomaticParty faction, ref bool __result)
             {
                 try
                 {
                     MethodInfo getRelationMethod = AccessTools.Method(typeof(GeoHavenLeader), "GetRelationWith");
                     PartyDiplomacy.Relation relation = (PartyDiplomacy.Relation)getRelationMethod.Invoke(__instance, new object[] { faction });

                     __result = relation.Diplomacy > -50;

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }

             }
         }*/



        [HarmonyPatch(typeof(GeoHaven), "GetRecruitCost")]
        public static class TFTV_Experimental_GeoHaven_GetRecruitCost_IncreaseCostDiplomacy_patch
        {
            public static void Postfix(GeoHaven __instance, ref ResourcePack __result, GeoFaction forFaction)
            {
                try
                {
                    GeoHavenLeader leader = __instance.Leader;
                    MethodInfo getRelationMethod = AccessTools.Method(typeof(GeoHavenLeader), "GetRelationWith");
                    PartyDiplomacy.Relation relation = (PartyDiplomacy.Relation)getRelationMethod.Invoke(leader, new object[] { forFaction });
                    ResourcePack price = new ResourcePack(__result);
                    float multiplier = 1f;
                    if (relation.Diplomacy > -50 && relation.Diplomacy <= -25)
                    {
                        multiplier = 1.5f;
                    }
                    else if (relation.Diplomacy > -25 && relation.Diplomacy <= 0)
                    {
                        multiplier = 1.25f;

                    }

                    for (int i = 0; i < price.Count; i++)
                    {
                        //  TFTVLogger.Always("Price component is " + price[i].Type + " amount " + price[i].Value);
                        ResourceUnit resourceUnit = price[i];
                        price[i] = new ResourceUnit(resourceUnit.Type, resourceUnit.Value * multiplier);
                    }

                    __result = price;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }




        /*  [HarmonyPatch(typeof(GeoHaven), "GetResourceTrading")]
          public static class TFTV_Experimental_GeoHaven_GetResourceTrading_IncreaseCostDiplomacy_patch
          {
              public static void Postfix(GeoHaven __instance, ref List<HavenTradingEntry> __result)
              {
                  try
                  {
                      GeoFaction phoenixFaction = __instance.Site.GeoLevel.PhoenixFaction;
                      PartyDiplomacy.Relation relation = __instance.Leader.Diplomacy.GetRelation(phoenixFaction);
                      float multiplier = 1f;
                      List<HavenTradingEntry> offeredTrade = new List<HavenTradingEntry>(__result);

                      if (relation.Diplomacy > -50 && relation.Diplomacy <= -25)
                      {
                          multiplier = 0.5f;
                      }
                      else if (relation.Diplomacy > -25 && relation.Diplomacy <= 0)
                      {
                          multiplier = 0.75f;
                          TFTVLogger.Always("GetResourceTrading");
                      }

                      for (int i = 0; i < offeredTrade.Count; i++)
                      {
                         HavenTradingEntry havenTradingEntry = offeredTrade[i];
                          offeredTrade[i] = new HavenTradingEntry
                          {
                              HavenOfferQuantity = (int)(havenTradingEntry.HavenOfferQuantity*multiplier),
                              HavenOffers = havenTradingEntry.HavenOffers,
                              HavenWants = havenTradingEntry.HavenWants,
                              ResourceStock = havenTradingEntry.ResourceStock,
                              HavenReceiveQuantity = havenTradingEntry.HavenReceiveQuantity,
                          };
                          TFTVLogger.Always("New value is " + offeredTrade[i].HavenOfferQuantity);
                      }

                      __result = offeredTrade;

                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }
          }*/




        [HarmonyPatch(typeof(GeoHaven), "CheckShouldSpawnRecruit")]
        public static class TFTV_Experimental_GeoHaven_CheckShouldSpawnRecruit_IncreaseCostDiplomacy_patch
        {
            public static void Postfix(GeoHaven __instance, ref bool __result, float modifier)
            {
                try
                {
                    if (__result == false)
                    {
                        if (!__instance.IsRecruitmentEnabled || !__instance.ZonesStats.CanGenerateRecruit)
                        {
                            __result = false;
                        }
                        else
                        {
                            GeoFaction phoenixFaction = __instance.Site.GeoLevel.PhoenixFaction;
                            PartyDiplomacy.Relation relation = __instance.Leader.Diplomacy.GetRelation(phoenixFaction);
                            if (relation.Diplomacy <= 0 && relation.Diplomacy > -50)
                            {
                                int num = __instance.HavenDef.RecruitmentBaseChance;
                                num = Mathf.RoundToInt((float)num * modifier);


                                __result = UnityEngine.Random.Range(0, 100) < num;
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }




        public static void CheckUseFireWeaponsAndDifficulty(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 0 && controller.CurrentDifficultyLevel.Order > 1)
                {
                    // TFTVLogger.Always("Checking fire weapons usage to decide wether to add Fire Quenchers");

                    PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));

                    List<SoldierStats> allSoldierStats = new List<SoldierStats>(statisticsManager.CurrentGameStats.LivingSoldiers.Values.ToList());
                    allSoldierStats.AddRange(statisticsManager.CurrentGameStats.DeadSoldiers.Values.ToList());
                    List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                    int scoreFireDamage = 0;
                    StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");

                    foreach (SoldierStats stat in allSoldierStats)
                    {
                        if (stat.ItemsUsed.Count > 0)
                        {
                            usedWeapons.AddRange(stat.ItemsUsed);
                        }
                    }

                    if (usedWeapons.Count() > 0)
                    {
                        // TFTVLogger.Always("Checking use of each weapon... ");
                        foreach (UsedWeaponStat stat in usedWeapons)
                        {
                            //   TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());
                            if (Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Contains(stat.UsedItem.ToString())))
                            {
                                WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                                if (weaponDef != null && weaponDef.DamagePayload.DamageType == fireDamage)
                                {
                                    scoreFireDamage += stat.UsedCount;
                                }
                            }
                        }
                    }

                    // TFTVLogger.Always("Fire weapons used " + scoreFireDamage + " times");

                    if (scoreFireDamage > 1)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(0, 6) + controller.CurrentDifficultyLevel.Order + scoreFireDamage;

                        //    TFTVLogger.Always("The roll is " + roll);

                        if (roll >= 10)
                        {
                            //    TFTVLogger.Always("The roll is passed!");
                            controller.EventSystem.SetVariable("FireQuenchersAdded", 1);
                        }

                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckForFireQuenchers(GeoLevelController controller)
        {
            try
            {
                CheckUseFireWeaponsAndDifficulty(controller);

                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 1)
                {
                    AddFireQuencherAbility();
                    TFTVLogger.Always("Fire Quenchers added!");
                }
                else
                {

                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };
                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AddFireQuencherAbility()
        {
            try
            {
                ApplyStatusAbilityDef fireQuencherAbility = DefCache.GetDef<ApplyStatusAbilityDef>("FireQuencherAbility");
                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunityInvisibleAbility");
                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>() { fireQuencherAbility, fireImmunity };
                //  DefCache.GetDef<TacticalItemDef>("Crabman_Legs_Armoured_ItemDef").Abilities = abilities.ToArray();
                //  DefCache.GetDef<TacticalItemDef>("Crabman_Legs_EliteArmoured_ItemDef").Abilities = new TacticalAbilityDef[] { fireImmunity };
                //    DefCache.GetDef<TacCharacterDef>("Crabman9_Shielder_AlienMutationVariationDef").Data.Abilites = abilities.ToArray();

                DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = abilities.ToArray();
                DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = abilities.ToArray();
                // DefCache.GetDef<TacticalItemDef>("Crabman_LeftLeg_Armoured_BodyPartDef").Abilities = abilities.ToArray();
                //  DefCache.GetDef<TacticalItemDef>("Crabman_RightLeg_Armoured_BodyPartDef").Abilities = abilities.ToArray();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        [HarmonyPatch(typeof(TacticalPerceptionBase), "IsTouchingVoxel")]

        public static class TFTV_Experimental_Evaluate_Experiment_patch
        {
            public static void Postfix(TacticalPerceptionBase __instance)
            {
                try
                {
                    DamageMultiplierStatusDef fireQuencherStatus = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");
                    //   DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                    TacticalActorBase tacticalActorBase = __instance.TacActorBase;

                    //
                    if (tacticalActorBase is TacticalActor && tacticalActorBase.GetActor().Status.HasStatus(fireQuencherStatus)) //tacticalActorBase.GetActor().GetAbility<DamageMultiplierAbility>(fireImmunity)!=null 
                                                                                                                                 // && tacticalActorBase.GetActor().HasGameTag(DefCache.GetDef<GameTagDef>("Crabman_ClassTagDef")))
                    {
                        foreach (TacticalVoxel voxel in tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxels(__instance.GetBounds()))
                        {
                            if (voxel.GetVoxelType() == TacticalVoxelType.Fire)
                            {
                                VoxelsOnFire.Add(voxel);
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, 0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, -0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, 0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, -0.5f)));
                                // VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1.5f, 0, 1.5f)));
                                //  VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1.5f, 0, -1.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, 1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, -1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, -1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, 1)));
                                //  VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1.5f, 0, 0.5f)));
                                // VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1.5f, 0, -0.5f)));
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



        [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

        public static class TacticalActor_OnAbilityExecuteFinished_Experiment_patch
        {
            public static void Postfix()
            {
                try
                {
                    if (VoxelsOnFire != null)
                    {

                        if (VoxelsOnFire.Count > 0)
                        {
                            //  TFTVLogger.Always("Voxels on fire count is " + TFTVExperimental.VoxelsOnFire.Count);
                            // List<TacticalVoxel> voxelsForMist = new List<TacticalVoxel>();
                            foreach (TacticalVoxel voxel in TFTVExperimental.VoxelsOnFire)
                            {
                                if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Fire)
                                {
                                    //    TFTVLogger.Always("Got past the if check");

                                    voxel.SetVoxelType(TacticalVoxelType.Empty, 1);
                                }
                            }
                        }

                        if (VoxelsOnFire.Count > 0)
                        {
                            foreach (TacticalVoxel voxel in TFTVExperimental.VoxelsOnFire)
                            {
                                if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Empty)
                                {
                                    //TFTVLogger.Always("Got past the if check for Mist");
                                    voxel.SetVoxelType(TacticalVoxelType.Mist, 2, 10);
                                }
                            }
                        }

                        VoxelsOnFire.Clear();
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }




        //  public static float Score = 0;
        //  public static List<float> ScoresBeforeCulling = new List<float>();
        //  public static int CounterAIActionsInfluencedBySafetyConsideration = 0;






        public static TacticalActor mutoidReceivingHealing = null;

        [HarmonyPatch(typeof(HealAbility), "HealTargetCrt")]

        public static class HealAbility_HealTargetCrt_Mutoid_Patch
        {
            public static void Prefix(PlayingAction action)
            {
                try
                {
                    if ((TacticalActor)((TacticalAbilityTarget)action.Param).Actor != null)
                    {
                        TacticalActor actor = (TacticalActor)((TacticalAbilityTarget)action.Param).Actor;
                        if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef")))
                        {
                            mutoidReceivingHealing = actor;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "get_GeneralHealAmount")]

        public static class HealAbility_Mutoid_Patch
        {

            public static void Postfix(HealAbility __instance, ref float __result)
            {
                try
                {

                    if (mutoidReceivingHealing != null)
                    {
                        __result = 0;
                        mutoidReceivingHealing = null;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        /* [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
         public static class GeoMission_PrepareLevel_VOObjectives_Patch
         {
             public static void Postfix(TacMissionData missionData, GeoMission __instance)
             {
                 try
                 {
                    // TFTVLogger.Always("PrepareLevel invoked");
                     GeoLevelController controller = __instance.Level;
                     List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                     List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                     // Remove faction objectives that correspond to void omens that are not in play
                     for (int i = listOfFactionObjectives.Count - 1; i >= 0; i--)
                     {
                         FactionObjectiveDef objective = listOfFactionObjectives[i];
                         if (objective.name.StartsWith("VOID_OMEN_TITLE_"))
                         {
                             int vo = int.Parse(objective.name.Substring("VOID_OMEN_TITLE_".Length));
                             if (!TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                             {
                                 TFTVLogger.Always("Removing VO " + vo + " from faction objectives");
                                 listOfFactionObjectives.RemoveAt(i);
                             }
                         }
                     }

                     // Add faction objectives for void omens that are in play
                     foreach (int vo in voidOmens)
                     {
                         if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                         {
                             if (!listOfFactionObjectives.Any(o => o.name == "VOID_OMEN_TITLE_" + vo))
                             {
                                 TFTVLogger.Always("Adding VO " + vo + " to faction objectives");
                                 listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                             }
                         }
                     }

                     missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/




        /* [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
         public static class GeoMission_ModifyMissionData_AddVOObjectives_Patch
         {
             public static void Postfix(TacMissionData missionData, GeoMission __instance)
             {
                 try
                 {
                     TFTVLogger.Always("ModifyMissionData invoked");
                     GeoLevelController controller = __instance.Level;
                     List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                     foreach (int vo in voidOmens)
                     {
                         if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                         {
                             TFTVLogger.Always("VO " + vo + " found");
                             List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                             if (!listOfFactionObjectives.Contains(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo)))
                             {
                                 listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                                 missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                             }
                         }
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/











    }
    /* [HarmonyPatch(typeof(AIStrategicPositionConsideration), "Evaluate")]

        public static class AIStrategicPositionConsideration_Evaluate_Experiment_patch
        {
            public static void Postfix(AIStrategicPositionConsideration __instance, float __result)
            {
                try
                {
                    if (__instance.BaseDef.name == "StrategicPosition_AIConsiderationDef" && __result != 1)
                    {

                        TFTVLogger.Always("StrategicPosition_AIConsiderationDef " + __result);
                        Score = __result;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }*/

}


