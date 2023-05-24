﻿using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Common.Entities.Items.ItemManufacturing;

namespace TFTV
{
    internal class TFTVAncients
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");

        private static readonly WeaponDef RightDrill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
        private static readonly WeaponDef RightShield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
        private static readonly EquipmentDef LeftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
        private static readonly WeaponDef BeamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
        private static readonly EquipmentDef LeftCrystalShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_CrystalShield_EquipmentDef");

        private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
        private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

        private static readonly PassiveModifierAbilityDef ancientsPowerUpAbility = DefCache.GetDef<PassiveModifierAbilityDef>("AncientMaxPower_AbilityDef");
        private static readonly DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");
        private static readonly PassiveModifierAbilityDef SelfRepairAbility = DefCache.GetDef<PassiveModifierAbilityDef>("RoboticSelfRepair_AbilityDef");


        public static readonly string CyclopsBuiltVariable = "CyclopsBuiltVariable";
        public static bool LOTAReworkActive = false;
        public static bool AutomataResearched = false;

        //This is the number of previous encounters with Ancients. It is added to the Difficulty to determine the number of fully repaired MediumGuardians in battle
        public static int AncientsEncounterCounter = 0;
        public static string AncientsEncounterVariableName = "Ancients_Encounter_Global_Variable";
        public static int HoplitesKilled = 0;
        private static readonly AlertedStatusDef AlertedStatus = DefCache.GetDef<AlertedStatusDef>("Alerted_StatusDef");
        private static readonly DamageMultiplierStatusDef CyclopsDefenseStatus = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
        private static readonly StanceStatusDef AncientGuardianStealthStatus = DefCache.GetDef<StanceStatusDef>("AncientGuardianStealth_StatusDef");
        private static readonly DamageMultiplierStatusDef RoboticSelfRepairStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
        // private static readonly GameTagDef SelfRepairTag = DefCache.GetDef<GameTagDef>("SelfRepair");
        // private static readonly GameTagDef MaxPowerTag = DefCache.GetDef<GameTagDef>("MaxPower");


        public static Dictionary<int, int> CyclopsMolecularDamageBuff = new Dictionary<int, int> { }; //turn number + 0 = none, 1 = mutation, 2 = bionic


        /*  [HarmonyPatch(typeof(TacticalAbility), "IsEnabled")]
          public static class TacticalAbility_IsEnabled_CyclopsScream_Patch
          {
              public static void Postfix(TacticalAbility __instance, ref bool __result)
              {
                  try
                  {
                      TFTVLogger.Always($"ability {__instance.TacticalAbilityDef.name} is enabled {__result}. Changing to disabled.");
                      __result = false;


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/



        [HarmonyPatch(typeof(PsychicScreamAbility), "Activate")]
        public static class PsychicScreamAbility_Activate_CyclopsScream_Patch
        {
            public static void Postfix(PsychicScreamAbility __instance)
            {
                try
                {
                    //    TFTVLogger.Always($"Got here, {__instance.TacticalAbilityDef.name}");



                    if (__instance.TacticalAbilityDef.name.Equals("CyclopsScream"))
                    {
                        // BleedStatusDef screamStatusLevel1Def = DefCache.GetDef<BleedStatusDef>("CyclopsScreamLevel1_BleedStatusDef");
                        SilencedStatusDef silencedStatusDef = DefCache.GetDef<SilencedStatusDef>("ActorSilenced_StatusDef");
                        DamageEffectDef mindCrushEffect = DefCache.GetDef<DamageEffectDef>("E_Effect [Cyclops_MindCrush]");
                        //  SlotStateStatusDef disabledBodyPartStatus = DefCache.GetDef< SlotStateStatusDef>("DisabledElectronicSlot_StatusDef");
                        //  MindControlStatusDef mindControlStatusDef = DefCache.GetDef<MindControlStatusDef>("MindControl_StatusDef");
                        //  DamageEffectDef bleedEffect = DefCache.GetDef< DamageEffectDef>("Bleed_EffectDef");


                        foreach (TacticalAbilityTarget target in __instance.GetTargets())
                        {
                            // TFTVLogger.Always($"Got here");

                            if (target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor tacticalActor)
                            {

                                //   ItemSlot head = tacticalActor.BodyState.GetSlot("Head");
                                //   tacticalActor.ApplyDamage(new DamageResult { ApplyStatuses = new List<StatusApplication> { new StatusApplication { StatusDef = disabledBodyPartStatus, StatusSource = tacticalActor, StatusTarget = head, Value = 100 } } });
                                tacticalActor.ApplyDamage(new DamageResult { ActorEffects = new List<EffectDef> { mindCrushEffect } });
                                tacticalActor.ApplyDamage(new DamageResult { ApplyStatuses = new List<StatusApplication> { new StatusApplication { StatusDef = silencedStatusDef, StatusSource = __instance.TacticalActor, StatusTarget = tacticalActor } } });

                                // EffectTarget effectTarget = new EffectTarget { GameObject = tacticalActor.gameObject };

                                //  Effect.Apply(Repo, bleedEffect, effectTarget);



                                // head.ApplyDamage(new DamageResult {DamageTypeDef = bleedDamage, HealthDamage = 20 });



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

        [HarmonyPatch(typeof(TacticalLevelController), "ActorDamageDealt")]
        public static class TacticalLevelController_ActorDamageDealt_CyclopsMolecularTargeting_Patch
        {
            public static void Postfix(TacticalActor actor, IDamageDealer damageDealer)
            {
                try
                {
                    CyclopsMolecularTargeting(actor, damageDealer);
                    // CyclopsSelfHealing(actor);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalFactionVision), "OnFactionStartTurn")]
        public static class TacticalFactionVision_OnFactionStartTurn_SelfRepair_Patch
        {
            public static void Postfix(TacticalFactionVision __instance)
            {
                try
                {
                    if (!__instance.Faction.TacticalLevel.IsLoadingSavedGame)
                    {
                        TacticalLevelController controller = __instance.Faction.TacticalLevel;
                        TacticalFaction tacticalFaction = __instance.Faction;

                        TFTVLogger.Always($"starting turn {tacticalFaction.TurnNumber} for faction {tacticalFaction.Faction.FactionDef.name}");
                        CheckRoboticSelfRepairStatus(tacticalFaction);
                        CyclopsSelfHealing(tacticalFaction);


                        if (tacticalFaction.TurnNumber > 0)
                        {
                            CheckForAutoRepairAbility(__instance.Faction);
                            AdjustAutomataStats(__instance.Faction);

                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        internal static void CyclopsSelfHealing(TacticalFaction tacticalFaction)
        {
            try
            {
                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.GetAbilityWithDef<PassiveModifierAbility>(SelfRepairAbility) != null))
                {
                    List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                    TFTVLogger.Always($"{tacticalActor.name} has {SelfRepairAbility.name} and {bodyPartAspects.Count} disabled body parts. Applying Robotic Self Repair");

                    if (bodyPartAspects.Count > 0)
                    {
                        tacticalActor.Status.ApplyStatus(RoboticSelfRepairStatus);
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CheckRoboticSelfRepairStatus(TacticalFaction tacticalFaction)

        {
            try
            {
                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                {
                    if (tacticalActor.HasStatus(RoboticSelfRepairStatus))
                    {
                        List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                        foreach (ItemSlot bodyPart in bodyPartAspects)
                        {
                            TFTVLogger.Always($"{tacticalActor.name} has disabled {bodyPart.DisplayName}. Adding {bodyPart.GetHealth().Max / 2} health ");
                            bodyPart.GetHealth().Add(bodyPart.GetHealth().Max / 2);
                            tacticalActor.CharacterStats.WillPoints.Subtract(5);
                        }

                        Status status = tacticalActor.Status.GetStatusByName(RoboticSelfRepairStatus.EffectName);

                        tacticalActor.Status.Statuses.Remove(status);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        //  DamageAccumulation

        /*  [HarmonyPatch(typeof(DamagePayload), "AccumulateDamage")]
          public static class TacticalLevelController_AccumulateDamage_CyclopsMolecularTargeting_Patch
          {
              public static void Postfix(DamagePayload __instance, ref DamageAccumulation accum, CastHit hit, Vector3 dir, IDamageDealer damageDealer, DamagePredictor predictor, EffectTarget target = null, bool ignoreHitCheck = false)
              {
                  try
                  {
                      TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                      if (CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber) && damageDealer.TryGetWeapon().WeaponDef.name.Equals("HumanoidGuardian_Head_WeaponDef"))
                      {
                          GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                          GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                          DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = virophageDamageKeyword };

                          DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                          MethodInfo generateEffectTargetMethod = typeof(DamagePayload).GetMethod("GenerateEffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);

                          if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 1)
                          {
                              DamageAccumulation accum2 = virophageDamage.GenerateDamageAccumulation(__instance, damageDealer);
                              object[] parameters = new object[] { accum2, hit, dir, predictor, target, false };

                              EffectTarget effectTarget = (EffectTarget)generateEffectTargetMethod.Invoke(__instance, parameters);
                              Effect.Apply(Repo, virophageDamage.DamageKeywordDef.DamageTypeDef, effectTarget, damageDealer);
                              accum.AddTargets(effectTarget.GetParam<DamageEffect.Params>().DamageAccum);
                              TFTVLogger.Always($"got here, should apply extra virophage damage");
                          }
                          else
                          {
                              DamageAccumulation accum2 = empDamage.GenerateDamageAccumulation(__instance, damageDealer);
                              object[] parameters = new object[] { accum2, hit, dir, predictor, target, false };

                              EffectTarget effectTarget = (EffectTarget)generateEffectTargetMethod.Invoke(__instance, parameters);
                              Effect.Apply(Repo, empDamage.DamageKeywordDef.DamageTypeDef, effectTarget, damageDealer);
                              accum.AddTargets(effectTarget.GetParam<DamageEffect.Params>().DamageAccum);
                              TFTVLogger.Always($"got here, should apply extra bionic damage");

                          }
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }
        */

        //        public static event Action<int> CyclopsDamageBuffAdded;

        // Subscribe to the event
        //  TFTVAncients.CyclopsDamageBuffAdded += HandleCyclopsDamageBuffAdded;

        // Define the event handler method
        /*private static void HandleCyclopsDamageBuffAdded(int damageBuffValue)
                {
                    // Perform actions based on the damage buff value
                    // For example:
                    if (damageBuffValue == 1)
                    {
                        // Do something
                    }
                    else if (damageBuffValue == 2)
                    {
                        // Do something else
                    }
                }*/




        internal static void CyclopsMolecularTargeting(TacticalActor actor, IDamageDealer damageDealer)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

               

                if (CyclopsMolecularDamageBuff.Count()==0 || !CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                {
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                    if (damageDealer != null && damageDealer.GetTacticalActorBase() != null && damageDealer.GetTacticalActorBase().GameTags.Contains(hopliteTag))
                    {

                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                        WeaponDef beamVsMutants = DefCache.GetDef<WeaponDef>("HopliteVSMutantsBeam");
                        WeaponDef beamVsCyborgs = DefCache.GetDef<WeaponDef>("HopliteVSCyborgs");

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        //    cyclopsLCBeam.DamagePayload.DamageKeywords[0].Value = 120;

                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        //   cyclopsOBeam.DamagePayload.DamageKeywords[0].Value = 120;

                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");
                        //   cyclopsPBeam.DamagePayload.DamageKeywords[0].Value = 120;

                        WeaponDef cyclopsBeamVsMutants = DefCache.GetDef<WeaponDef>("CyclopsVSMutantsBeam");
                        WeaponDef cyclopsBeamVsCyborgs = DefCache.GetDef<WeaponDef>("CyclopsVSCyborgs");


                        TacticalFaction tacticalFaction = damageDealer.GetTacticalActorBase().TacticalFaction;

                        bool cyclopsAlive = false;



                        foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                        {
                            if (tacticalActor.IsAlive && tacticalActor.GameTags.Contains(cyclopsTag))
                            {
                                cyclopsAlive = true;

                            }
                        }

                        if (cyclopsAlive)
                        {

                            int bionics = 0;
                            int mutations = 0;

                            foreach (TacticalItem bodypart in actor.BodyState.GetArmourItems())
                            {
                                if (bodypart.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                {
                                    mutations += 1;
                                }
                                else if (bodypart.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                {
                                    TFTVLogger.Always("bionics");

                                    bionics += 1;
                                }
                            }

                            if (actor.TacticalActorDef.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                            {
                                bionics = 5;

                            }


                            if (bionics > mutations)
                            {
                                TFTVLogger.Always("more bionics");
                                CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 2);
                                //   CyclopsDamageBuffAdded?.Invoke(CyclopsMolecularDamageBuff[controller.TurnNumber]);
                                originalBeam.ViewElementDef = beamVsCyborgs.ViewElementDef;
                                originalBeam.DamagePayload = beamVsCyborgs.DamagePayload;
                                cyclopsLCBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                                cyclopsLCBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;
                                cyclopsOBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                                cyclopsOBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;
                                cyclopsPBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                                cyclopsPBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;


                                TFTVLogger.Always($"{actor.DisplayName} is primarily bionic or a vehicle");

                            }
                            else if (bionics < mutations || actor.HasGameTag(Shared.SharedGameTags.AlienTag))
                            {
                                CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 1);
                                //   CyclopsDamageBuffAdded?.Invoke(CyclopsMolecularDamageBuff[controller.TurnNumber]);
                                originalBeam.ViewElementDef = beamVsMutants.ViewElementDef;
                                originalBeam.DamagePayload = beamVsMutants.DamagePayload;
                                cyclopsLCBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                                cyclopsLCBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                                cyclopsOBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                                cyclopsOBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                                cyclopsPBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                                cyclopsPBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                                TFTVLogger.Always($"{actor.DisplayName} is primarily mutated or an Alien");
                            }
                            else
                            {
                                originalBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [HumanoidGuardian_Head_WeaponDef]");
                                originalBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                { Value = 70, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                                cyclopsLCBeam.DamagePayload.DamageKeywords =
                                   new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                                cyclopsLCBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_LivingCrystal_WeaponDef]");
                                cyclopsOBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                                cyclopsOBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_Orichalcum_WeaponDef]");
                                cyclopsPBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                                cyclopsPBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_ProteanMutane_WeaponDef]");

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


        //Patch giving access to Project Glory research when Player activates 3rd base
        [HarmonyPatch(typeof(GeoPhoenixFaction), "ActivatePhoenixBase")]
        public static class GeoPhoenixFaction_ActivatePhoenixBase_GiveGlory_Patch
        {
            public static void Postfix(GeoPhoenixFaction __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("Photographer") != 1 && __instance.Bases.Count() > 2)
                    {
                        GeoscapeEventContext eventContext = new GeoscapeEventContext(__instance.GeoLevel.ViewerFaction, __instance);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaLotaStart", eventContext);
                        __instance.GeoLevel.EventSystem.SetVariable("Photographer", 1);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Method additional requirements texts to Impossible Weapons if nerf is on.

        public static void CheckImpossibleWeaponsAdditionalRequirements(GeoLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
                if (config.impossibleWeaponsAdjustments)
                {

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {

                        if (controller.PhoenixFaction.Research.HasCompleted("PX_Scorpion_ResearchDef"))
                        {
                            DefCache.GetDef<ResearchViewElementDef>("NJ_VehicleTech_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_NJ_VEHICLETECH_RESEARCHDEF_BENEFITS";
                        }
                        else
                        {
                            DefCache.GetDef<ResearchViewElementDef>("NJ_VehicleTech_ViewElementDef").BenefitsText.LocalizationKey = "";
                        }
                        if (controller.PhoenixFaction.Research.HasCompleted("PX_ShardGun_ResearchDef"))
                        {
                            DefCache.GetDef<ResearchViewElementDef>("ANU_AdvancedInfectionTech_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_ANU_ADVANCEDINFECTIONTECH_RESEARCHDEF_BENEFITS";
                        }
                        else
                        {
                            DefCache.GetDef<ResearchViewElementDef>("ANU_AdvancedInfectionTech_ViewElementDef").BenefitsText.LocalizationKey = "";
                        }
                        if (controller.PhoenixFaction.Research.HasCompleted("PX_Scyther_ResearchDef"))
                        {
                            DefCache.GetDef<ResearchViewElementDef>("SYN_Bionics3_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_SYN_BIONICS3_RESEARCHDEF_BENEFITS";
                        }
                        else
                        {
                            DefCache.GetDef<ResearchViewElementDef>("SYN_Bionics3_ViewElementDef").BenefitsText.LocalizationKey = "SYN_BIONICS3_RESEARCHDEF_BENEFITS";
                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        //Patch preventing manufacturing of IW when all conditions are not fulfilled if LOTA rework active and nerf is on in the config.
        //Note that conditons vary depending on whether nerf is on, but even if off, some conditions are required.
        [HarmonyPatch(typeof(ItemManufacturing), "CanManufacture")]
        public static class GeoFaction_CanManufacture_Patch
        {

            public static void Postfix(ManufacturableItem item, ref ManufactureFailureReason __result, GeoFaction ____faction)
            {
                try

                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    //For TFTV we need to add checks here to see if the player has researched the required Exotic Materials + additional Faction Tech, and if not, return NotUnlocked
                    //However, we may add an option to Config so that additional faction research is not required
                    if (LOTAReworkActive)
                    {
                        //AC Crossbow is not nerfed, but in TFTV it is unlocked by the Living Crystal research
                        if (item.Name.LocalizationKey == "KEY_AC_CROSSBOW_NAME" && !____faction.Research.HasCompleted("PX_LivingCrystalResearchDef"))
                        {
                            //   TFTVLogger.Always("Crossbow is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Rebuke is nerfed, and in TFTV it is unlocked by the Protean Mutane research
                        if (item.Name.LocalizationKey == "KEY_AC_HEAVY_NAME" && !____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef"))
                        {
                            //  TFTVLogger.Always("Rebuke is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Nerfed Mattock in TFTV has a different name, but both nerfed and Vanilla now require Protean Mutane research
                        if ((item.Name.LocalizationKey == "TFTV_KEY_AC_MACE_NAME" || item.Name.LocalizationKey == "KEY_AC_MACE_NAME") && !____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef"))
                        {
                            //   TFTVLogger.Always("Mattock is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Nerfed Shardgun in TFTV requires Advanced Infection Tech
                        if (item.Name.LocalizationKey == "TFTV_KEY_AC_SHOTGUN_NAME" &&
                            (!____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef") || !____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")
                            || !____faction.Research.HasCompleted("ANU_AdvancedInfectionTech_ResearchDef")))
                        {
                            //   TFTVLogger.Always("Shardgun TFTV is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Vanilla Shardgun in TFTV requires Living Crystal research and Protean Mutane Reseach
                        if (item.Name.LocalizationKey == "KEY_AC_SHOTGUN_NAME" &&
                            (!____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef") || !____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")))

                        {
                            //  TFTVLogger.Always("Shardgun is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }


                        //Nerfed Scorpion in TFTV requires Armadillo tech
                        if (item.Name.LocalizationKey == "TFTV_KEY_AC_SNIPER_NAME" &&
                           (!____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef") || !____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")
                           || !____faction.Research.HasCompleted("NJ_VehicleTech_ResearchDef")))
                        {
                            //  TFTVLogger.Always("Scorpion TFTV is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Vanilla Scorpion in TFTV requires Living Crystal research and Protean Mutane Reseach
                        if (item.Name.LocalizationKey == "KEY_AC_SNIPER_NAME" &&
                           (!____faction.Research.HasCompleted("PX_ProteanMutaneResearchDef") || !____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")))
                        {
                            //  TFTVLogger.Always("Scorpion is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Nerfed Scythe in TFTV requires Bionics 3
                        if (item.Name.LocalizationKey == "TFTV_KEY_AC_SCYTHE_NAME" &&
                          (!____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")
                          || !____faction.Research.HasCompleted("SYN_Bionics3_ResearchDef")))
                        {
                            //  TFTVLogger.Always("Scythe TFTV is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }

                        //Vanilla Scythe in TFTV requires Living Crystal research and Protean Mutane Reseach
                        if (item.Name.LocalizationKey == "KEY_AC_SCYTHE_NAME" &&
                          (!____faction.Research.HasCompleted("PX_LivingCrystalResearchDef")))
                        {
                            //  TFTVLogger.Always("Scythe is not unlocked " + item.Name.LocalizationKey);
                            __result = ManufactureFailureReason.NotUnlocked;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Sets the objective to reactivate cyclops
        //Called when Living Crystal or Protean Mutane researches are completed; whichever is completed last
        public static void SetReactivateCyclopsObjective(GeoLevelController controller)
        {
            try
            {
                GeoscapeEventSystem eventSystem = controller.EventSystem;

                if (controller.PhoenixFaction.Research.HasCompleted("PX_ProteanMutaneResearchDef") && controller.PhoenixFaction.Research.HasCompleted("PX_LivingCrystalResearchDef"))
                {
                    GeoscapeEventContext context = new GeoscapeEventContext(controller.PhoenixFaction, controller.PhoenixFaction);
                    eventSystem.TriggerGeoscapeEvent("Helena_Can_Build_Cyclops", context);
                    DiplomaticGeoFactionObjective cyclopsObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                    {
                        Title = new LocalizedTextBind("BUILD_CYCLOPS_OBJECTIVE"),
                        Description = new LocalizedTextBind("BUILD_CYCLOPS_OBJECTIVE"),
                    };
                    cyclopsObjective.IsCriticalPath = true;
                    controller.PhoenixFaction.AddObjective(cyclopsObjective);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        //Sets the objective to protect cyclops
        public static void SetProtectCyclopsObjective(GeoLevelController controller)
        {
            try
            {
                GeoscapeEventSystem eventSystem = controller.EventSystem;

                DiplomaticGeoFactionObjective cyclopsObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                {
                    Title = new LocalizedTextBind("PROTECT_THE_CYCLOPS_OBJECTIVE_GEO_TITLE"),
                    Description = new LocalizedTextBind("PROTECT_THE_CYCLOPS_OBJECTIVE_GEO_TITLE"),
                    IsCriticalPath = true
                };
                controller.PhoenixFaction.AddObjective(cyclopsObjective);


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        //Sets the objective to obtain samples of Living Crystal/Protean Mutane
        public static void SetObtainLCandPMSamplesObjective(GeoLevelController controller)
        {
            try
            {
                GeoscapeEventSystem eventSystem = controller.EventSystem;

                ResourceUnit livingCrystal = new ResourceUnit(ResourceType.LivingCrystals, 1);
                ResourceUnit proteanMutane = new ResourceUnit(ResourceType.ProteanMutane, 1);

                if (!controller.PhoenixFaction.Wallet.HasResources(livingCrystal))
                {
                    DiplomaticGeoFactionObjective obtainLCObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                    {
                        Title = new LocalizedTextBind("OBTAIN_LC_OBJECTIVE"),
                        Description = new LocalizedTextBind("OBTAIN_LC_OBJECTIVE"),
                        IsCriticalPath = true
                    };
                    controller.PhoenixFaction.AddObjective(obtainLCObjective);
                }
                if (!controller.PhoenixFaction.Wallet.HasResources(proteanMutane))
                {
                    DiplomaticGeoFactionObjective obtainPMObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                    {
                        Title = new LocalizedTextBind("OBTAIN_PM_OBJECTIVE"),
                        Description = new LocalizedTextBind("OBTAIN_PM_OBJECTIVE"),
                        IsCriticalPath = true
                    };
                    controller.PhoenixFaction.AddObjective(obtainPMObjective);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        //Checks research state to adjust texts
        public static void AncientsCheckResearchState(GeoLevelController controller)
        {
            try
            {
                //alternative Reveal text for YuggothianEntity Research: 

                ResearchViewElementDef yuggothianEntityVED = DefCache.GetDef<ResearchViewElementDef>("PX_YuggothianEntity_ViewElementDef");

                ArcheologySettingsDef archeologySettingsDef = DefCache.GetDef<ArcheologySettingsDef>("ArcheologySettingsDef");

                if (controller.EventSystem.GetVariable("SymesAlternativeCompleted") == 1)
                {
                    yuggothianEntityVED.UnlockText.LocalizationKey = "PX_YUGGOTHIANENTITY_RESEARCHDEF_REVEALED_TFTV_ALTERNATIVE";
                }
                else
                {
                    yuggothianEntityVED.UnlockText.LocalizationKey = "PX_YUGGOTHIANENTITY_RESEARCHDEF_UNLOCK";
                }

                if (controller.PhoenixFaction.Research.HasCompleted("ExoticMaterialsResearch"))
                {
                    TFTVLogger.Always("ExoticMaterialsResearch completed");

                    archeologySettingsDef.AncientSiteSetting[0].HarvestSiteName.LocalizationKey = "KEY_AC_PROTEAN_HARVEST_AFTER_REVEAL";
                    archeologySettingsDef.AncientSiteSetting[1].HarvestSiteName.LocalizationKey = "KEY_AC_ORICHALCUM_HARVEST_AFTER_REVEAL";
                    archeologySettingsDef.AncientSiteSetting[2].HarvestSiteName.LocalizationKey = "KEY_AC_CRYSTAL_HARVEST_AFTER_REVEAL";
                    archeologySettingsDef.AncientSiteSetting[0].RefinerySiteName.LocalizationKey = "KEY_AC_PROTEAN_REFINERY_AFTER_REVEAL";
                    archeologySettingsDef.AncientSiteSetting[1].RefinerySiteName.LocalizationKey = "KEY_AC_ORICHALCUM_REFINERY_AFTER_REVEAL";
                    archeologySettingsDef.AncientSiteSetting[2].RefinerySiteName.LocalizationKey = "KEY_AC_CRYSTAL_REFINERY_AFTER_REVEAL";
                }
                else
                {
                    archeologySettingsDef.AncientSiteSetting[0].HarvestSiteName.LocalizationKey = "KEY_AC_PROTEAN_HARVEST";
                    archeologySettingsDef.AncientSiteSetting[1].HarvestSiteName.LocalizationKey = "KEY_AC_ORICHALCUM_HARVEST";
                    archeologySettingsDef.AncientSiteSetting[2].HarvestSiteName.LocalizationKey = "KEY_AC_CRYSTAL_HARVEST";
                    archeologySettingsDef.AncientSiteSetting[0].RefinerySiteName.LocalizationKey = "KEY_AC_PROTEAN_REFINERY";
                    archeologySettingsDef.AncientSiteSetting[1].RefinerySiteName.LocalizationKey = "KEY_AC_ORICHALCUM_REFINERY";
                    archeologySettingsDef.AncientSiteSetting[2].RefinerySiteName.LocalizationKey = "KEY_AC_CRYSTAL_REFINERY";
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        //Checks research state on Geoscape End and then on Tactical Start
        public static void CheckResearchStateOnGeoscapeEndAndOnTacticalStart(GeoLevelController controller)
        {
            try
            {
                TacticalActorDef hopliteActorDef = DefCache.GetDef<TacticalActorDef>("HumanoidGuardian_ActorDef");
                TacticalActorDef cyclopsActorDef = DefCache.GetDef<TacticalActorDef>("MediumGuardian_ActorDef");

                List<AbilityDef> hopliteAbilities = new List<AbilityDef>(hopliteActorDef.Abilities.ToList());
                List<AbilityDef> cyclopsAbilites = new List<AbilityDef>(cyclopsActorDef.Abilities.ToList());


                AbilityDef poisonResistance = DefCache.GetDef<AbilityDef>("PoisonResistant_DamageMultiplierAbilityDef");
                AbilityDef psychicResistance = DefCache.GetDef<AbilityDef>("PsychicResistant_DamageMultiplierAbilityDef");
                AbilityDef eMPResistant = DefCache.GetDef<AbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                AbilityDef poisonImmunity = DefCache.GetDef<AbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                //  AbilityDef psychicImmunity = DefCache.GetDef<AbilityDef>("PsychicImmunity_DamageMultiplierAbilityDef");
                AbilityDef paralysisImmunity = DefCache.GetDef<AbilityDef>("ParalysisNotShockImmunity_DamageMultiplierAbilityDef");
                AbilityDef fireImmunity = DefCache.GetDef<AbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                AbilityDef stunStatusImmunity = DefCache.GetDef<AbilityDef>("StunStatusImmunity_AbilityDef");
                //AbilityDef empImmunity = DefCache.GetDef<AbilityDef>("EMPImmunity_DamageMultiplierAbilityDef");

                DamageMultiplierStatusDef cyclopsDefense_StatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
                DamageMultiplierStatusDef selfRepair = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                DamageMultiplierStatusDef poweredUp = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                List<AbilityDef> abilitiesToRemove = new List<AbilityDef>() { poisonResistance };
                List<AbilityDef> abilitiesToAdd = new List<AbilityDef>() { poisonImmunity, paralysisImmunity, fireImmunity };


                /* if (GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>() != null)
                 {
                     GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                     TFTVLogger.Always("Got here");*/

                if (controller != null && controller.PhoenixFaction.Research.HasCompleted("AncientAutomataResearch"))
                {
                    AutomataResearched = true;
                    TFTVLogger.Always($"Geoscape Check Automata Research completed is {AutomataResearched}");
                    //  return;
                }
                else if (controller != null && !controller.PhoenixFaction.Research.HasCompleted("AncientAutomataResearch"))
                {
                    AutomataResearched = false;
                    TFTVLogger.Always($"Geoscape Check Automata Research completed is {AutomataResearched}");
                    //  return;
                }

                if (AutomataResearched)
                {
                    if (!abilitiesToRemove.Contains(stunStatusImmunity))
                    {
                        abilitiesToRemove.Add(stunStatusImmunity);
                    }

                    cyclopsDefense_StatusDef.Visuals.DisplayName1.LocalizationKey = "CYCLOPS_DEFENSE_NAME";
                    cyclopsDefense_StatusDef.Visuals.Description.LocalizationKey = "CYCLOPS_DEFENSE_DESCRIPTION";
                    selfRepair.Visuals.DisplayName1.LocalizationKey = "HOPLITES_SELF_REPAIR_NAME";
                    selfRepair.Visuals.Description.LocalizationKey = "HOPLITES_SELF_REPAIR_DESCRIPTION";
                    poweredUp.Visuals.DisplayName1.LocalizationKey = "POWERED_UP_NAME";
                    poweredUp.Visuals.Description.LocalizationKey = "POWERED_UP_DESCRIPTION";


                }
                else
                {
                    if (!abilitiesToAdd.Contains(stunStatusImmunity))
                    {
                        abilitiesToAdd.Add(stunStatusImmunity);
                    }

                    cyclopsDefense_StatusDef.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    cyclopsDefense_StatusDef.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                    selfRepair.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    selfRepair.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                    poweredUp.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    poweredUp.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                }


                foreach (AbilityDef abilityDef in abilitiesToAdd)
                {
                    if (!hopliteAbilities.Contains(abilityDef))
                    {
                        hopliteAbilities.Add(abilityDef);
                    }
                    if (!cyclopsAbilites.Contains(abilityDef))
                    {
                        cyclopsAbilites.Add(abilityDef);
                    }
                }

                foreach (AbilityDef abilityDef in abilitiesToRemove)
                {
                    if (hopliteAbilities.Contains(abilityDef))
                    {
                        hopliteAbilities.Remove(abilityDef);
                    }
                    if (cyclopsAbilites.Contains(abilityDef))
                    {
                        cyclopsAbilites.Remove(abilityDef);
                    }
                }



                /*   TFTVLogger.Always("The count of Hoplite abilities is " + hopliteAbilities.Count);
                   foreach (AbilityDef ability in hopliteAbilities)
                   {
                       TFTVLogger.Always("The ability is " + ability.name);
                   }

                   TFTVLogger.Always("The count of Cyclops abilities is " + cyclopsAbilites.Count);
                   foreach (AbilityDef ability in cyclopsAbilites)
                   {
                       TFTVLogger.Always("The ability is " + ability.name);
                   }
                */
                hopliteActorDef.Abilities = hopliteAbilities.ToArray();
                cyclopsActorDef.Abilities = cyclopsAbilites.ToArray();
                TFTVLogger.Always($"Tactical: Automata researched is {AutomataResearched}");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Was awkward method, deprecated
        public static void CheckResearchStateOnTacticalStart()
        {
            try
            {
                TacticalActorDef hopliteActorDef = DefCache.GetDef<TacticalActorDef>("HumanoidGuardian_ActorDef");
                TacticalActorDef cyclopsActorDef = DefCache.GetDef<TacticalActorDef>("MediumGuardian_ActorDef");

                List<AbilityDef> hopliteAbilities = new List<AbilityDef>(hopliteActorDef.Abilities.ToList());
                List<AbilityDef> cyclopsAbilites = new List<AbilityDef>(cyclopsActorDef.Abilities.ToList());

                AbilityDef poisonResistance = DefCache.GetDef<AbilityDef>("PoisonResistant_DamageMultiplierAbilityDef");
                AbilityDef psychicResistance = DefCache.GetDef<AbilityDef>("PsychicResistant_DamageMultiplierAbilityDef");
                AbilityDef eMPResistant = DefCache.GetDef<AbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                AbilityDef poisonImmunity = DefCache.GetDef<AbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                AbilityDef fireImmunity = DefCache.GetDef<AbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                //  AbilityDef psychicImmunity = DefCache.GetDef<AbilityDef>("PsychicImmunity_DamageMultiplierAbilityDef");
                AbilityDef paralysisImmunity = DefCache.GetDef<AbilityDef>("ParalysisNotShockImmunity_DamageMultiplierAbilityDef");

                AbilityDef stunStatusImmunity = DefCache.GetDef<AbilityDef>("StunStatusImmunity_AbilityDef");
                //AbilityDef empImmunity = DefCache.GetDef<AbilityDef>("EMPImmunity_DamageMultiplierAbilityDef");

                DamageMultiplierStatusDef cyclopsDefense_StatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
                DamageMultiplierStatusDef selfRepair = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                DamageMultiplierStatusDef poweredUp = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                List<AbilityDef> abilitiesToRemove = new List<AbilityDef>() { poisonResistance };
                List<AbilityDef> abilitiesToAdd = new List<AbilityDef>() { poisonImmunity, paralysisImmunity, fireImmunity };

                if (AutomataResearched)
                {
                    TFTVLogger.Always("Ancient Automata Research Completed");
                    if (!abilitiesToRemove.Contains(stunStatusImmunity))
                    {
                        abilitiesToRemove.Add(stunStatusImmunity);
                    }
                    /* if (!abilitiesToRemove.Contains(eMPResistant))
                     {
                         abilitiesToRemove.Add(eMPResistant);
                     }*/


                    cyclopsDefense_StatusDef.Visuals.DisplayName1.LocalizationKey = "CYCLOPS_DEFENSE_NAME";
                    cyclopsDefense_StatusDef.Visuals.Description.LocalizationKey = "CYCLOPS_DEFENSE_DESCRIPTION";
                    selfRepair.Visuals.DisplayName1.LocalizationKey = "HOPLITES_SELF_REPAIR_NAME";
                    selfRepair.Visuals.Description.LocalizationKey = "HOPLITES_SELF_REPAIR_DESCRIPTION";
                    poweredUp.Visuals.DisplayName1.LocalizationKey = "POWERED_UP_NAME";
                    poweredUp.Visuals.Description.LocalizationKey = "POWERED_UP_DESCRIPTION";

                }
                else
                {
                    if (!abilitiesToAdd.Contains(stunStatusImmunity))
                    {
                        abilitiesToAdd.Add(stunStatusImmunity);
                    }
                    /*   if (!abilitiesToAdd.Contains(eMPResistant))
                       {
                           abilitiesToAdd.Add(eMPResistant);
                       }*/

                    cyclopsDefense_StatusDef.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    cyclopsDefense_StatusDef.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                    selfRepair.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    selfRepair.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                    poweredUp.Visuals.DisplayName1.LocalizationKey = "UNKNOWN_STATUS_NAME";
                    poweredUp.Visuals.Description.LocalizationKey = "UNKNOWN_STATUS_DESCRIPTION";
                }


                foreach (AbilityDef abilityDef in abilitiesToAdd)
                {
                    if (!hopliteAbilities.Contains(abilityDef))
                    {
                        hopliteAbilities.Add(abilityDef);
                    }
                    if (!cyclopsAbilites.Contains(abilityDef))
                    {
                        cyclopsAbilites.Add(abilityDef);
                    }
                }

                foreach (AbilityDef abilityDef in abilitiesToRemove)
                {
                    if (hopliteAbilities.Contains(abilityDef))
                    {
                        hopliteAbilities.Remove(abilityDef);
                    }
                    if (cyclopsAbilites.Contains(abilityDef))
                    {
                        cyclopsAbilites.Remove(abilityDef);
                    }
                }



                /*   TFTVLogger.Always("The count of Hoplite abilities is " + hopliteAbilities.Count);
                   foreach (AbilityDef ability in hopliteAbilities)
                   {
                       TFTVLogger.Always("The ability is " + ability.name);
                   }

                   TFTVLogger.Always("The count of Cyclops abilities is " + cyclopsAbilites.Count);
                   foreach (AbilityDef ability in cyclopsAbilites)
                   {
                       TFTVLogger.Always("The ability is " + ability.name);
                   }*/

                hopliteActorDef.Abilities = hopliteAbilities.ToArray();
                cyclopsActorDef.Abilities = cyclopsAbilites.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Adjusts exotic resources received as reward
        [HarmonyPatch(typeof(RewardsController), "SetResources")]
        public static class RewardsController_SetResources_Patch
        {

            public static void Postfix(ResourcePack reward, RewardsController __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {
                        //  TFTVLogger.Always("Set resources, got here");

                        foreach (ResourceUnit resourceUnit in reward)
                        {
                            //  TFTVLogger.Always($"{resourceUnit.Type} {resourceUnit.Value}");

                            if (resourceUnit.Type == ResourceType.ProteanMutane)
                            {
                                UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                                Resolution resolution = Screen.currentResolution;
                                float resolutionFactorWidth = (float)resolution.width / 1920f;
                                float resolutionFactorHeight = (float)resolution.height / 1080f;


                                Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                                Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("ProteanMutaneRes").GetComponent<Transform>().Find("Requirement_Icon");
                                Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("ProteanMutaneRes").GetComponent<Transform>().Find("Requirement_Text");

                                Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                                Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);

                                exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                                // exoticResourceTextCopy.GetComponent<Text>().text = DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestProteanMissionOutcomeDef").Resources[0].Value.ToString();
                                exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                                exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                                exoticResourceTextCopy.Translate(new Vector3(0f, -10f * resolutionFactorHeight, 0f));

                                __instance.NoResourcesText.gameObject.SetActive(false);
                                __instance.ResourcesRewardsParentObject.SetActive(true);

                                TFTVLogger.Always("Removing Protean Mutane Objective");
                                TFTVCommonMethods.RemoveManuallySetObjective(controller, "OBTAIN_PM_OBJECTIVE");
                            }
                            else if (resourceUnit.Type == ResourceType.LivingCrystals)
                            {
                                UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                                Resolution resolution = Screen.currentResolution;
                                float resolutionFactorWidth = (float)resolution.width / 1920f;
                                float resolutionFactorHeight = (float)resolution.height / 1080f;

                                Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                                Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("LivingCrystalsRes").GetComponent<Transform>().Find("Requirement_Icon");
                                Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("LivingCrystalsRes").GetComponent<Transform>().Find("Requirement_Text");


                                Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                                Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);

                                exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                                // DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestCrystalMissionOutcomeDef").Resources[0].Value.ToString();
                                exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                                exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                                exoticResourceTextCopy.Translate(new Vector3(0f, -10f * resolutionFactorHeight, 0f));

                                __instance.NoResourcesText.gameObject.SetActive(false);
                                __instance.ResourcesRewardsParentObject.SetActive(true);

                                TFTVLogger.Always("Removing Living Crystal Objective");
                                TFTVCommonMethods.RemoveManuallySetObjective(controller, "OBTAIN_LC_OBJECTIVE");

                            }
                            else if (resourceUnit.Type == ResourceType.Orichalcum)
                            {
                                TFTVLogger.Always("Orichalcum, got here");
                                UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                                Resolution resolution = Screen.currentResolution;
                                float resolutionFactorWidth = (float)resolution.width / 1920f;
                                float resolutionFactorHeight = (float)resolution.height / 1080f;


                                Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                                Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("OrichalcumRes").GetComponent<Transform>().Find("Requirement_Icon");
                                Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("OrichalcumRes").GetComponent<Transform>().Find("Requirement_Text");


                                Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                                Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);
                                // TFTVLogger.Always($"{reward.Values[0].Value}");
                                exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                                //  TFTVLogger.Always($"{exoticResourceTextCopy.GetComponent<Text>().text}");
                                //DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestOrichalcumMissionOutcomeDef").Resources[0].Value.ToString();
                                exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                                exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                                exoticResourceTextCopy.Translate(new Vector3(0f, -10f * resolutionFactorHeight, 0f));

                                __instance.NoResourcesText.gameObject.SetActive(false);
                                __instance.ResourcesRewardsParentObject.SetActive(true);

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

        //Removes exotic resource harvesting from game
        [HarmonyPatch(typeof(GeoVehicle), "get_CanHarvestFromSites")]
        public static class GeoVehicle_get_CanHarvestFromSites_Patch
        {

            public static void Postfix(ref bool __result, GeoVehicle __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("NewGameStarted") == 1)
                    {
                        __result = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Prevents player from building Cyclops
        [HarmonyPatch(typeof(AncientGuardianGuardAbility), "GetDisabledStateInternal")]
        public static class AncientGuardianGuardAbility_GetDisabledStateInternal_Patch
        {

            public static void Postfix(ref GeoAbilityDisabledState __result, AncientGuardianGuardAbility __instance)
            {
                try
                {
                    GeoLevelController controller = __instance.GeoLevel;

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {


                        if (controller.PhoenixFaction.Research.HasCompleted("PX_LivingCrystalResearchDef")
                            && controller.PhoenixFaction.Research.HasCompleted("PX_ProteanMutaneResearchDef")
                            && controller.EventSystem.GetVariable(CyclopsBuiltVariable) == 0)
                        {


                        }
                        else
                        {
                            __result = GeoAbilityDisabledState.RequirementsNotMet;

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Prevents attacks on ancient sites, except for story mission
        [HarmonyPatch(typeof(GeoFaction), "AttackAncientSite")]
        public static class GeoFaction_AttackAncientSite_Patch
        {
            public static bool Prefix(GeoSite ancientSite, GeoFaction __instance)
            {


                try
                {
                    TFTVLogger.Always("AttackAncientSite " + ancientSite.Name);


                    GeoLevelController controller = __instance.GeoLevel;

                    GameTagDef lcGuardian = DefCache.GetDef<GameTagDef>("LivingCrystalGuardianGameTagDef");
                    GameTagDef oGuardian = DefCache.GetDef<GameTagDef>("OrichalcumGuardianGameTagDef");
                    GameTagDef pmGuardian = DefCache.GetDef<GameTagDef>("ProteanMutaneGuardianGameTagDef");
                    List<GameTagDef> guardianTags = new List<GameTagDef> { lcGuardian, oGuardian, pmGuardian };

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1 && guardianTags.Any(tag => ancientSite.GameTags.Contains(tag)))
                    {
                        TFTVLogger.Always("AttackAncientSite " + ancientSite.Name + " Guardian");
                        SetProtectCyclopsObjective(controller);
                        return true;
                    }

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }

            }

        }

        //If Player builds cyclops, schedules an Attack on the site
        [HarmonyPatch(typeof(AncientGuardianGuardAbility), "ActivateInternal")]
        public static class AncientGuardianGuardAbility_ActivateInternal_Patch
        {
            public static void Postfix(AncientGuardianGuardAbility __instance, GeoAbilityTarget target)
            {
                try
                {
                    GeoLevelController controller = __instance.GeoLevel;

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {
                        controller.EventSystem.SetVariable(CyclopsBuiltVariable, 1);
                        GeoSite geoSite = (GeoSite)target.Actor;

                        controller.AlienFaction.AttackAncientSite(geoSite, 24);

                        GeoscapeEventContext context = new GeoscapeEventContext(controller.AlienFaction, controller.PhoenixFaction);
                        controller.EventSystem.TriggerGeoscapeEvent("Helena_Beast", context);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Prevents player from harvesting from Ancient site right after winning mission
        //Also triggers a bunch of things after story mission is completed

        [HarmonyPatch(typeof(GeoMission), "ApplyOutcomes")]
        public static class GeoMission_ModifyMissionData_CheckAncients_Patch
        {

            public static void Postfix(GeoMission __instance, FactionResult viewerFactionResult)
            {
                try
                {
                    GeoLevelController controller = __instance.Level;
                    GeoSite geoSite = __instance.Site;

                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {

                        MissionTypeTagDef ancientSiteDefense = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef");
                        if (__instance.MissionDef.SaveDefaultName == "AncientRuin" && !__instance.MissionDef.Tags.Contains(ancientSiteDefense))
                        {

                            controller.EventSystem.SetVariable(AncientsEncounterVariableName, controller.EventSystem.GetVariable(AncientsEncounterVariableName) + 1);
                            TFTVLogger.Always(AncientsEncounterVariableName + " is now " + controller.EventSystem.GetVariable(AncientsEncounterVariableName));

                            List<GeoVehicle> geoVehicles = __instance.Site.Vehicles.ToList();
                            foreach (GeoVehicle vehicle in geoVehicles)
                            {
                                vehicle.EndCollectingFromCurrentSite();

                            }
                        }
                        //if player wins the ancient defense mission, the variable triggering Yuggothian Entity research will be unlocked
                        if (__instance.MissionDef.Tags.Contains(ancientSiteDefense))
                        {
                            if (viewerFactionResult.State == TacFactionState.Won)
                            {
                                if (controller.EventSystem.GetVariable("Sphere") == 0)
                                {
                                    controller.EventSystem.SetVariable("Sphere", 1);
                                    //triggers Digitize my Dreams, the Cyclops said event
                                    GeoscapeEventContext context = new GeoscapeEventContext(controller.AlienFaction, controller.PhoenixFaction);
                                    controller.EventSystem.TriggerGeoscapeEvent("Cyclops_Dreams", context);
                                    AncientsCheckResearchState(controller);
                                }
                                GameTagDef lcGuardian = DefCache.GetDef<GameTagDef>("LivingCrystalGuardianGameTagDef");
                                GameTagDef oGuardian = DefCache.GetDef<GameTagDef>("OrichalcumGuardianGameTagDef");
                                GameTagDef pmGuardian = DefCache.GetDef<GameTagDef>("ProteanMutaneGuardianGameTagDef");
                                List<GameTagDef> guardianTags = new List<GameTagDef> { lcGuardian, oGuardian, pmGuardian };


                                foreach (GameTagDef gameTagDef in guardianTags)
                                {
                                    if (geoSite.GameTags.Contains(gameTagDef))
                                    {
                                        geoSite.GameTags.Remove(gameTagDef);

                                    }

                                }

                                TFTVCommonMethods.RemoveManuallySetObjective(controller, "BUILD_CYCLOPS_OBJECTIVE");

                            }
                            //if the player is defeated, the Cyclops variable will be reset so that the player may try again
                            else if (viewerFactionResult.State == TacFactionState.Defeated)
                            {
                                controller.EventSystem.SetVariable(CyclopsBuiltVariable, 0);

                            }

                            TFTVCommonMethods.RemoveManuallySetObjective(controller, "PROTECT_THE_CYCLOPS_OBJECTIVE_GEO_TITLE");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Method to check if Ancients (as a faction) are present in the mission
        public static bool CheckIfAncientsPresent(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                {
                    TFTVLogger.Always("Ancients present");
                    return true;

                }
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        //Adjusts deployment of Ancient Automata
        public static void AdjustAncientsOnDeployment(TacticalLevelController controller)
        {
            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
            try
            {
                if (LOTAReworkActive)
                {

                    TFTVLogger.Always("AdjustAncientsOnDeployment method invoked");
                    TacticalFaction ancients = controller.GetFactionByCommandName("anc");
                    CyclopsDefenseStatus.Multiplier = 0.5f;
                    List<TacticalActor> damagedGuardians = new List<TacticalActor>();
                    int countUndamagedGuardians = AncientsEncounterCounter + controller.Difficulty.Order;

                    foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                    {
                        // TFTVLogger.Always("Found tacticalactorbase");
                        if (tacticalActorBase is TacticalActor && !tacticalActorBase.HasGameTag(cyclopsTag))
                        {
                            //   TFTVLogger.Always("Found hoplite");
                            TacticalActor guardian = tacticalActorBase as TacticalActor;
                            if (damagedGuardians.Count() + countUndamagedGuardians < ancients.Actors.Count())
                            {
                                damagedGuardians.Add(guardian);
                            }
                            guardian.CharacterStats.WillPoints.Set(guardian.CharacterStats.WillPoints.IntMax / 3);
                            guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                            guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);

                        }
                        else if (tacticalActorBase is TacticalActor cyclops && tacticalActorBase.HasGameTag(cyclopsTag))
                        {
                            //  TFTVLogger.Always("Found cyclops");
                            tacticalActorBase.Status.ApplyStatus(CyclopsDefenseStatus);
                            cyclops.CharacterStats.WillPoints.Set(cyclops.CharacterStats.WillPoints.IntMax / 4);
                            cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                            cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                        }
                    }

                    foreach (TacticalActor tacticalActor in damagedGuardians)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(1, 101);
                        // TFTVLogger.Always("The roll is " + roll);


                        foreach (Equipment item in tacticalActor.Equipments.Equipments)
                        {
                            if (item.TacticalItemDef.Equals(BeamHead))
                            {
                                if (roll > 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                            {
                                if (roll <= 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                            {
                                if (roll + 10 * countUndamagedGuardians >= 65)
                                {
                                    item.DestroyAll();
                                }
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

        public static void AdjustHopliteAndCyclopsBeam()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");


                WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");


                WeaponDef cyclopsBeamVsMutants = DefCache.GetDef<WeaponDef>("CyclopsVSMutantsBeam");
                WeaponDef cyclopsBeamVsCyborgs = DefCache.GetDef<WeaponDef>("CyclopsVSCyborgs");

                if (CyclopsMolecularDamageBuff.Count() > 0)
                {

                    if (CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                    {


                        WeaponDef beamVsMutants = DefCache.GetDef<WeaponDef>("HopliteVSMutantsBeam");
                        WeaponDef beamVsCyborgs = DefCache.GetDef<WeaponDef>("HopliteVSCyborgs");

                        if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 1)
                        {
                            originalBeam.ViewElementDef = beamVsMutants.ViewElementDef;
                            originalBeam.DamagePayload = beamVsMutants.DamagePayload;
                            cyclopsLCBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                            cyclopsLCBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                            cyclopsOBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                            cyclopsOBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                            cyclopsPBeam.DamagePayload = cyclopsBeamVsMutants.DamagePayload;
                            cyclopsPBeam.ViewElementDef = cyclopsBeamVsMutants.ViewElementDef;
                            TFTVLogger.Always($"{originalBeam.name} is switching to vs mutants and aliens");
                        }
                        else if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 2)
                        {
                            originalBeam.ViewElementDef = beamVsCyborgs.ViewElementDef;
                            originalBeam.DamagePayload = beamVsCyborgs.DamagePayload;
                            cyclopsLCBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                            cyclopsLCBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;
                            cyclopsOBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                            cyclopsOBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;
                            cyclopsPBeam.DamagePayload = cyclopsBeamVsCyborgs.DamagePayload;
                            cyclopsPBeam.ViewElementDef = cyclopsBeamVsCyborgs.ViewElementDef;
                            TFTVLogger.Always($"{originalBeam.name} is switching to vs cyborgs and vehicles");
                        }
                    }
                    else
                    {

                        originalBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [HumanoidGuardian_Head_WeaponDef]");
                        originalBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                { Value = 70, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        cyclopsLCBeam.DamagePayload.DamageKeywords =
                           new List<DamageKeywordPair>()
                        { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                        };
                        cyclopsLCBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_LivingCrystal_WeaponDef]");
                        cyclopsOBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        cyclopsOBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_Orichalcum_WeaponDef]");
                        cyclopsPBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        cyclopsPBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_ProteanMutane_WeaponDef]");
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckCyclopsDefense()
        {
            try
            {
                if (LOTAReworkActive)
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (CheckIfAncientsPresent(controller))
                    {
                        List<TacticalActor> allHoplites = controller.GetFactionByCommandName("anc").TacticalActors.Where(ta => ta.HasGameTag(hopliteTag)).ToList();
                        int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                        float proportion = ((float)deadHoplites / (float)(allHoplites.Count));
                        CyclopsDefenseStatus.Multiplier = 0.5f + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                        TFTVLogger.Always($"There are {allHoplites.Count} hoplites in total, {deadHoplites} are dead. Proportion is {proportion}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");
                    }

                    AdjustHopliteAndCyclopsBeam();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        public static TacticalItem[] CheckGuardianBodyParts(TacticalActor actor)
        {
            try
            {
                TacticalItem[] equipment = new TacticalItem[3];

                foreach (Equipment item in actor.Equipments.Equipments)
                {
                    if (item.TacticalItemDef.Equals(BeamHead))
                    {
                        equipment[0] = item;
                    }
                    else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                    {
                        equipment[1] = item;

                    }
                    else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                    {
                        equipment[2] = item;
                    }
                }
                return equipment;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new TacticalItem[3];
            }
        }

        public static void AdjustAutomataStats(TacticalFaction faction)
        {
            try
            {

                foreach (TacticalActor tacticalActor in faction.TacticalActors)
                {
                    if (tacticalActor is TacticalActor guardian && tacticalActor.HasGameTag(hopliteTag) && !guardian.Status.HasStatus(AncientGuardianStealthStatus))
                    {
                        if (guardian.CharacterStats.WillPoints < 30)
                        {
                            if (guardian.CharacterStats.WillPoints > 25)
                            {
                                guardian.CharacterStats.WillPoints.Set(30);
                            }
                            else
                            {
                                guardian.CharacterStats.WillPoints.AddRestrictedToMax(5);

                            }
                        }

                        if (guardian.CharacterStats.WillPoints >= 30)
                        {
                            if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                            {
                                guardian.AddAbility(ancientsPowerUpAbility, guardian);
                                guardian.Status.ApplyStatus(ancientsPowerUpStatus);

                                TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, guardian, guardian);
                            }
                        }
                        else
                        {
                            if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                            {
                                guardian.RemoveAbility(ancientsPowerUpAbility);
                                guardian.Status.Statuses.Remove(guardian.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));

                            }

                        }
                        guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                        guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);
                    }
                    else if (tacticalActor is TacticalActor cyclops && tacticalActor.HasGameTag(cyclopsTag))
                    {
                        if (cyclops.HasStatus(AlertedStatus))
                        {
                            if (cyclops.CharacterStats.WillPoints < 40)
                            {
                                if (cyclops.CharacterStats.WillPoints > 35)
                                {
                                    cyclops.CharacterStats.WillPoints.Set(40);
                                }
                                else
                                {
                                    cyclops.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                }
                            }
                        }

                        if (cyclops.CharacterStats.WillPoints >= 40)
                        {
                            if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                            {
                                cyclops.AddAbility(ancientsPowerUpAbility, cyclops);
                                cyclops.Status.ApplyStatus(ancientsPowerUpStatus);

                                TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, cyclops, cyclops);
                            }
                        }
                        else
                        {
                            if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                            {
                                cyclops.RemoveAbility(ancientsPowerUpAbility);
                                cyclops.Status.Statuses.Remove(cyclops.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));

                            }
                        }

                        cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                        cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        [HarmonyPatch(typeof(ItemDef), "OnManufacture")]
        public static class TFTV_Ancients_ItemDef_OnManufacture
        {
            public static void Postfix(ItemDef __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (controller.EventSystem.GetVariable("ManufacturedImpossibleWeapon") == 0)
                    {
                        WeaponDef shardGun = DefCache.GetDef<WeaponDef>("AC_ShardGun_WeaponDef");
                        WeaponDef crystalCrossbow = DefCache.GetDef<WeaponDef>("AC_CrystalCrossbow_WeaponDef");
                        WeaponDef mattock = DefCache.GetDef<WeaponDef>("AC_Mattock_WeaponDef");
                        WeaponDef rebuke = DefCache.GetDef<WeaponDef>("AC_Rebuke_WeaponDef");
                        WeaponDef scorpion = DefCache.GetDef<WeaponDef>("AC_Scorpion_WeaponDef");
                        WeaponDef scyther = DefCache.GetDef<WeaponDef>("AC_Scyther_WeaponDef");


                        if (__instance as WeaponDef != null && (__instance as WeaponDef == shardGun || __instance as WeaponDef == crystalCrossbow || __instance as WeaponDef == mattock ||
                            __instance as WeaponDef == rebuke || __instance as WeaponDef == scorpion || __instance as WeaponDef == scyther))
                        {

                            controller.EventSystem.SetVariable("ManufacturedImpossibleWeapon", 1);
                            GeoscapeEventContext context = new GeoscapeEventContext(controller.PhoenixFaction, controller.PhoenixFaction);
                            controller.EventSystem.TriggerGeoscapeEvent("Alistair_Progress", context);

                        }

                    }




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }


        }

        //set resource cost of excavation (now exploration)
        [HarmonyPatch(typeof(ExcavateAbility), "GetResourceCost")]

        public static class TFTV_GeoAbility_GetResourceCost
        {
            public static void Postfix(ref ResourcePack __result)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();


                    if (controller.EventSystem.GetVariable("NewGameStarted") == 1)
                    {

                        __result = new ResourcePack() { new ResourceUnit(ResourceType.Materials, value: 20), new ResourceUnit(ResourceType.Tech, value: 5) };
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        //removes icon + text of resource requirement if resource is not required
        [HarmonyPatch(typeof(SiteContextualMenuDescriptionController), "SetResourcesText")]

        public static class TFTV_ResourceDisplayController_SetDisplayedResource
        {
            public static void Postfix(Text textField)
            {
                try
                {

                    if (textField.text == "0")
                    {
                        textField.transform.parent.gameObject.SetActive(value: false);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        /*  [HarmonyPatch(typeof(TacticalFaction), "RequestEndTurn")]
          public static class TacticalFaction_RequestEndTurn_AncientsSelfRepair_Patch
          {
              public static void Postfix(TacticalFaction __instance)
              {
                  try
                  {
                      if (LOTAReworkActive)
                      {
                          if (CheckIfAncientsPresent(__instance.TacticalLevel))
                          {
                              if (__instance.TacticalLevel.TurnNumber > 0 && __instance.Equals(__instance.TacticalLevel.GetFactionByCommandName("PX")))
                              {

                                  CheckRoboticSelfRepairStatus(__instance);
                                  CyclopsSelfHealing(__instance);
                                  CheckForAutoRepairAbility(__instance.TacticalLevel);
                                  AdjustAutomataStats(__instance.TacticalLevel);

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


        [HarmonyPatch(typeof(DamageKeyword), "ProcessKeywordDataInternal")]
        internal static class TFTV_DamageKeyword_ProcessKeywordDataInternal_DamageResistant_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {
                    if (LOTAReworkActive)
                    {

                        if (data.Target.GetActor() != null && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(AncientGuardianStealthStatus))
                        {
                            //  TFTVLogger.Always("Statis check passed");

                            float multiplier = 0.1f;

                            data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                            data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_Ancients_Patch
        {
            public static void Postfix(TacticalLevelController __instance, DeathReport deathReport)
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                try
                {
                    if (CheckIfAncientsPresent(__instance) && LOTAReworkActive)
                    {
                        TacticalFaction ancients = __instance.GetFactionByCommandName("anc");

                        if (deathReport.Actor is TacticalActor)
                        {
                            TacticalActor actor = deathReport.Actor as TacticalActor;
                            if (actor.TacticalFaction == ancients)
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in ancients.Actors)
                                {
                                    if (allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                    {
                                        TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                        float magnitude = 7;

                                        if ((actorAlly.Pos - actor.Pos).magnitude <= magnitude)
                                        {
                                            TFTVLogger.Always("Actor in range and will be receiving power from dead friendly");
                                            actorAlly.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                            if ((CheckGuardianBodyParts(actorAlly)[0] == null
                                            || CheckGuardianBodyParts(actorAlly)[1] == null
                                            || CheckGuardianBodyParts(actorAlly)[2] == null))
                                            {
                                                TFTVLogger.Always("Actor in range and missing bodyparts, getting spare parts");
                                                if (!actorAlly.HasStatus(AddAutoRepairStatusAbility) && !actorAlly.HasGameTag(cyclopsTag))
                                                {
                                                    actorAlly.Status.ApplyStatus(AddAutoRepairStatusAbility);
                                                    TFTVLogger.Always("AutoRepairStatus added to " + actorAlly.name);

                                                    /*   if (!actorAlly.HasGameTag(SelfRepairTag))
                                                       {
                                                           actorAlly.GameTags.Add(SelfRepairTag);
                                                       }*/
                                                    TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                                    tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);

                                                }

                                            }
                                            else
                                            {
                                                if (actorAlly.GetHealth() < actorAlly.TotalMaxHealth)
                                                {
                                                    if (actorAlly.GetHealth() + 50 >= actorAlly.TotalMaxHealth)
                                                    {
                                                        actorAlly.Health.Set(actorAlly.TotalMaxHealth);
                                                    }
                                                    else
                                                    {
                                                        actorAlly.Health.Set(actorAlly.GetHealth() + 50);
                                                    }

                                                }
                                                TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                                tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);
                                            }
                                        }
                                    }
                                }

                                if (actor.HasGameTag(hopliteTag))
                                {
                                    if (CyclopsDefenseStatus.Multiplier <= 0.99f)
                                    {
                                        List<TacticalActor> allHoplites = actor.TacticalFaction.TacticalActors.Where(ta => ta.HasGameTag(hopliteTag)).ToList();
                                        int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                                        float proportion = ((float)deadHoplites / (float)(allHoplites.Count));
                                        CyclopsDefenseStatus.Multiplier = 0.5f + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                                        TFTVLogger.Always($"There are {allHoplites.Count} hoplites in total, {deadHoplites} are dead. Proportion is {proportion}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");


                                        //  CyclopsDefenseStatus.Multiplier += 0.1f;
                                        TFTVLogger.Always("Hoplite killed, decreasing Cyclops defense. Cyclops defense now " + CyclopsDefenseStatus.Multiplier);
                                    }
                                    else
                                    {
                                        CyclopsDefenseStatus.Multiplier = 1;
                                        if (AutomataResearched)
                                        {
                                            foreach (TacticalActorBase allyTacticalActorBase in ancients.Actors)
                                            {
                                                if (allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                                {
                                                    TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                                    if (actorAlly.HasStatus(CyclopsDefenseStatus))
                                                    {
                                                        Status status = actorAlly.Status.GetStatusByName(CyclopsDefenseStatus.EffectName);
                                                        actorAlly.Status.Statuses.Remove(status);
                                                        TFTVLogger.Always("Cyclops defense removed from " + actorAlly.name);

                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // HoplitesKilled++;

                                    /*  if (AutomataResearched) 
                                      {
                                          string description = "Before any Hoplites are destroyed, the Cyclops has a 50% resistance to all damage. Destroying Hoplites reduces this resistance. Current resistance: " + (100 - (CyclopsDefenseStatus.Multiplier * 100)) + "%";
                                          CyclopsDefenseStatus.Visuals.Description = new LocalizedTextBind(description, true);
                                      }*/

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

        public static void CheckForAutoRepairAbility(TacticalFaction faction)
        {
            try
            {


                foreach (TacticalActor actor in faction.TacticalActors)
                {
                    if (actor.HasStatus(AddAutoRepairStatusAbility))
                    {
                        TacticalItem[] Bodyparts = CheckGuardianBodyParts(actor);

                        TFTVLogger.Always($"{actor.name} has spare parts, making repairs");

                        actor.Status.Statuses.Remove(actor.Status.GetStatusByName(AddAutoRepairStatusAbility.EffectName));

                        if (Bodyparts[0] == null)
                        {
                            actor.Equipments.AddItem(BeamHead);
                        }
                        else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftCrystalShield)
                        {
                            actor.Equipments.AddItem(RightDrill);
                        }
                        else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftShield)
                        {
                            actor.Equipments.AddItem(RightShield);
                        }
                        else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightDrill)
                        {
                            actor.Equipments.AddItem(LeftCrystalShield);
                        }
                        else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightShield)
                        {
                            actor.Equipments.AddItem(LeftShield);
                        }

                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        //Adapted Lucus solution to avoid Ancient Automata receiving WP penalty on ally death
        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_HumanEnemies_Patch
        {
            public static void Prefix(TacticalActor __instance, DeathReport death, out int __state)
            {


                __state = 0; //Set this to zero so that the method still works for other actors.
                if (LOTAReworkActive)
                {
                    //Postfix checks for relevant GameTags then saves and zeroes the WPWorth of the dying actor before main method is executed.

                    GameTagsList<GameTagDef> RelevantTags = new GameTagsList<GameTagDef> { cyclopsTag, hopliteTag };
                    if (__instance.TacticalFaction == death.Actor.TacticalFaction && death.Actor.HasGameTags(RelevantTags, false))
                    {
                        __state = death.Actor.TacticalActorBaseDef.WillPointWorth;
                        death.Actor.TacticalActorBaseDef.WillPointWorth = 0;
                    }
                }
            }

            public static void Postfix(TacticalActor __instance, DeathReport death, int __state)
            {
                if (LOTAReworkActive)
                {
                    //Postfix will remove necessary Willpoints from allies and restore WPWorth's value to the def of the dying actor.
                    if (__instance.TacticalFaction == death.Actor.TacticalFaction)
                    {
                        foreach (GameTagDef Tag in death.Actor.GameTags)
                        {
                            if (Tag == cyclopsTag || Tag == hopliteTag)
                            {
                                //Death has no effect on allies
                                death.Actor.TacticalActorBaseDef.WillPointWorth = __state;
                            }
                        }
                    }
                }

            }
        }
    }
}



