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
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Common.Entities.Characters;
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

        internal static DamageOverTimeDamageTypeEffectDef AcidDamageTypeDef
            => DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

        /// <summary>
        /// One turn of acid on one body part, resolved the same way whether it is being applied for
        /// real or predicted for the UI.
        ///
        /// Acid never splits a tick: while the part still has armour the whole amount goes to armour
        /// and health takes nothing; only once the plate is gone does health take the hit. Both the
        /// damage path and the readout call this, so a forecast cannot drift away from what the
        /// character actually suffers.
        /// </summary>
        internal struct AcidTick
        {
            internal float ArmourDamage;
            internal float HealthDamage;

            internal static AcidTick Resolve(float amount, float armour, float resistance)
            {
                bool armoured = armour > 1E-05f;

                return new AcidTick
                {
                    ArmourDamage = armoured ? amount : 0f,
                    HealthDamage = armoured ? 0f : amount * resistance,
                };
            }
        }

        /// <summary>
        /// The acid a character is carrying, one entry per affected body part.
        ///
        /// The healthbar and the tooltip both show the sum of these, which is a number the game never
        /// works with: each part is its own AcidStatus with its own DamageAccumulation, corroding its
        /// own plate on its own clock, and each one bills the character's health separately once its
        /// plate is gone.
        /// </summary>
        internal class LimbAcid
        {
            internal string SlotName;
            internal string DisplayName;
            internal float Acid;
            internal float AcidAfter;
            internal float Armour;
            internal float ArmourAfter;
            internal float HealthDamage;
            internal float Decay;

            internal bool WillCostHealth => HealthDamage > 1E-05f;
        }

        /// <summary>
        /// The product of every acid multiplier the engine would apply, using the same filter as
        /// TacticalActorBase.GetDamageMultiplierFor so several sources - an acid vest plus the Lab
        /// Assistant background, say - compound exactly as they do in the damage path.
        /// </summary>
        internal static float GetAcidResistance(TacticalActorBase actor)
        {
            if (actor == null)
            {
                return 1f;
            }

            return actor.GetDamageMultiplierFor(AcidDamageTypeDef);
        }

        /// <summary>
        /// The acid multiplier as the damage path resolves it for one body part.
        ///
        /// This is not the same as the actor-wide figure. ItemSlot.GetDamageMultiplierFor keeps a
        /// slot-targeted TacStatus only for the slot it targets, while the actor-wide overload drops
        /// every slot-targeted status outright - so a resistance scoped to one limb makes limbs take
        /// different damage, and a forecast built on the actor-wide number would report them alike.
        /// ApplyAddedDamage_Default uses this slot-level figure for the Hit Point hit.
        /// </summary>
        internal static float GetAcidResistanceForSlot(ItemSlot slot, TacticalActorBase actor)
        {
            if (slot == null)
            {
                return GetAcidResistance(actor);
            }

            return slot.GetDamageMultiplierFor(AcidDamageTypeDef);
        }

        /// <summary>
        /// How many separate abilities or statuses are contributing to that product. Two sources of
        /// 0.5 multiply to 0.25, so the count is worth stating alongside the number.
        /// </summary>
        internal static int GetAcidResistanceSourceCount(TacticalActorBase actor)
        {
            if (actor == null)
            {
                return 0;
            }

            return actor.GetDamageMultipliers(
                DamageMultiplierType.Incoming,
                AcidDamageTypeDef,
                m => !(m is TacStatus status) || !status.GetTargetSlotsNames().Any()).Count();
        }

        internal static List<LimbAcid> GetLimbAcid(TacticalActor actor)
        {
            List<LimbAcid> limbs = new List<LimbAcid>();

            try
            {
                if (actor?.Status == null)
                {
                    return limbs;
                }

                CharacterBodyState bodyState = actor.GetComponent<CharacterBodyState>();
                if (bodyState == null)
                {
                    return limbs;
                }

                foreach (AcidStatus status in actor.Status.GetStatuses<AcidStatus>())
                {
                    foreach (string slotName in status.GetTargetSlotsNames())
                    {
                        ItemSlot slot = bodyState.GetSlot(slotName);
                        if (slot == null)
                        {
                            continue;
                        }

                        float armour = (float)slot.GetArmor().Value;
                        float resistance = GetAcidResistanceForSlot(slot, actor);

                        // Status.Value is the number the UI prints; the accumulation carries it
                        // scaled by the effect's per-point damage, which is what a tick spends.
                        AcidTick tick = AcidTick.Resolve(status.Value * status.DamagePerTurn, armour, resistance);

                        float decay = GetAcidDecayForSlot(status, slot, actor, resistance);

                        limbs.Add(new LimbAcid
                        {
                            SlotName = slotName,
                            DisplayName = slot.DisplayName,
                            Acid = status.Value,
                            AcidAfter = Mathf.Max(0f, status.Value - decay),
                            Armour = armour,
                            ArmourAfter = Mathf.Max(0f, armour - tick.ArmourDamage),
                            HealthDamage = tick.HealthDamage,
                            Decay = decay,
                        });
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return limbs;
        }

        /// <summary>
        /// How far the acid on one body part drops at the end of the turn.
        ///
        /// Any resistance at all doubles the step - LowerDamageOverTimeLevel tests the multiplier
        /// against 1 rather than scaling by it, so a second source of resistance buys more damage
        /// reduction but no extra burn-off. Two Thunderbird workshop modules then call
        /// LowerDamageOverTimeLevel a second time for bionic limbs and vehicles, which doubles it
        /// again for those slots only.
        /// </summary>
        internal static float GetAcidDecayForSlot(AcidStatus status, ItemSlot slot, TacticalActor actor, float resistance)
        {
            float perTurn = status.DamageOverTimeStatusDef.LowerLevelPerTurn;

            if (resistance < 1f)
            {
                perTurn *= 2f;
            }

            if (AircraftReworkTacticalModules.WorkshopModule.LowersAcidOnSlot(actor, slot))
            {
                perTurn *= 2f;
            }

            return perTurn;
        }

        /// <summary>
        /// The decay every affected limb shares, or NaN when they differ - a character with one
        /// bionic limb under two workshop modules burns acid off that limb twice as fast as the
        /// rest, so no single number describes them all.
        /// </summary>
        internal static float GetUniformAcidDecay(TacticalActor actor)
        {
            List<LimbAcid> limbs = GetLimbAcid(actor);

            if (limbs.Count == 0)
            {
                return 0f;
            }

            float first = limbs[0].Decay;

            return limbs.All(limb => Mathf.Approximately(limb.Decay, first)) ? first : float.NaN;
        }


        /// <summary>
        /// Prevents disabled body parts from reducing stats
        /// </summary>
        [HarmonyPatch(typeof(SlotStateStatus), "ApplyItemModifications")]
        static class DisableBodyPartStatPatch
        {
            static bool Prefix(TacticalItem item, bool invertValue)
            {
                if (invertValue && item.IsBodyPart && item.IsHealthAboveMinThreshold)
                {
                    // limb is being “disabled” without having been damaged – keep its stats
                    return false;   // skip the original method
                }

                return true;        // fall back to vanilla behaviour otherwise
            }
        }



        [HarmonyPatch(typeof(TacticalActorBase), "ApplyDamageInternal")]
        public static class TacticalActorBase_ApplyDamageInternal_Patch
        {
            public static void Prefix(TacticalActorBase __instance, ref DamageResult damageResult)
            {
                try
                {
                   // TFTVLogger.Always($"Applying damage to {__instance.name}, HP amount: {damageResult.HealthDamage}.DamageTypeDef? {damageResult.DamageTypeDef?.name}");

                    if (damageResult.DamageTypeDef != null && damageResult.DamageTypeDef == DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef") 
                        && damageResult.HealthDamage > 0)
                    {
                       
                       
                        float resistanceToAcid = 1; //as this is a multiplier, this means a resistance of 0%
                        foreach (var damageMultiplier in __instance.GetDamageMultipliers(DamageMultiplierType.Incoming, damageResult.DamageTypeDef))
                        {
                            resistanceToAcid *= damageMultiplier.GetMultiplier(__instance, __instance);
                        }

                       // TFTVLogger.Always($"Acid damage being applied to {__instance.name}, HP amount: {damageResult.HealthDamage}, resistance to acid: {resistanceToAcid}");

                        if (resistanceToAcid == 1) 
                        {
                            return;
                        }

                        float adjustedHealthDamage = damageResult.HealthDamage / resistanceToAcid;
                     //   TFTVLogger.Always($"reverting damage from acid to {adjustedHealthDamage}");
                        damageResult.HealthDamage = adjustedHealthDamage;
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
                        //  TFTVLogger.Always($"Applyin acid; setting result to value (current value {value} and result {__result})");
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


        private static bool ApplyTFTVAcidDamage(AcidDamageEffect __instance, EffectTarget target, ref DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
        {
            try
            {


                float resistanceToAcid = 1; //as this is a multiplier, this means a resistance of 0%

                if (recv != null)
                {


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


                    AcidTick tick = AcidTick.Resolve(accum.Amount, (float)recv.GetArmor().Value, resistanceToAcid);
                    float armorDamage = tick.ArmourDamage;
                    float num2 = tick.HealthDamage;

                   // TFTVLogger.Always($"Acid damage being applied: initial {accum.Amount}, after armor/resistance {num2}, resistance to acid {resistanceToAcid}");

                    DamageAccumulation.TargetData data = GetDamageData(num2, recv, __instance, armorDamage, damageOrigin, impactForce, impactHit);

                    DisableElectronics(num2, itemSlot, additionalSlot, recv, __instance);

                    accum.AddGeneratedTarget(data);
                }
                else
                {
                    bool num = (float)recv.GetArmor().Value > 1E-05f;
                    float armorDamage = num ? accum.Amount : 0f;
                    float num2 = (num ? 0f : (accum.Amount * accum.GetSourceDamageMultiplierForReceiver(recv)));

                    //TFTVLogger.Always($"Acid damage being applied when recv is null: initial {accum.Amount}, after armor/resistance {num2}, resistance to acid {resistanceToAcid}");

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
                    accum.AddGeneratedTarget(data);
                }

                return false;
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
        [HarmonyPatch(typeof(AcidDamageEffect), nameof(AcidDamageEffect.AddTarget))]
        public static class AcidDamageEffect_AddTarget_Patch
        {
            public static bool Prefix(AcidDamageEffect __instance, EffectTarget target, DamageAccumulation accum, IDamageReceiver recv, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit)
            {
                try
                {
                    return ApplyTFTVAcidDamage(__instance, target, ref accum, recv, damageOrigin, impactForce, impactHit);
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
