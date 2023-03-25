using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
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
        // commented out for release #13
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");

        private static readonly WeaponDef rightDrill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
        private static readonly WeaponDef rightShield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
        private static readonly EquipmentDef leftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
        private static readonly WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
        private static readonly EquipmentDef leftCrystalShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_CrystalShield_EquipmentDef");

        private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
        private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

        private static readonly PassiveModifierAbilityDef ancientsPowerUpAbility = DefCache.GetDef<PassiveModifierAbilityDef>("AncientMaxPower_AbilityDef");
        private static readonly DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

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
        // private static readonly GameTagDef SelfRepairTag = DefCache.GetDef<GameTagDef>("SelfRepair");
        // private static readonly GameTagDef MaxPowerTag = DefCache.GetDef<GameTagDef>("MaxPower");


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

        public static void RemoveManuallySetObjective(GeoLevelController controller, string title)
        {
            try
            {
                List<GeoFactionObjective> listOfObjectives = controller.PhoenixFaction.Objectives.ToList();

                foreach (GeoFactionObjective objective1 in listOfObjectives)
                {
                    if (objective1.Title == null)
                    {
                        TFTVLogger.Always("objective1.Title is missing!");
                    }
                    else
                    {
                        if (objective1.Title.LocalizationKey == null)
                        {
                            TFTVLogger.Always("objective1.Title.LocalizationKey is missing!");
                        }
                        else
                        {
                            TFTVLogger.Always("objective1.Title.LocalizationKey is " + objective1.Title.LocalizationKey);

                            if (objective1.Title.LocalizationKey == title)
                            {
                                controller.PhoenixFaction.RemoveObjective(objective1);
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

                //AutoRepair_AddAbilityStatusDef  AncientsPoweredUp AncientMaxPower_AbilityDef
                // DamageMultiplierStatusDef sourceAbilityStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                // 

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CheckResearchStateOnGeoscapeEnd(GeoLevelController controller)
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

                AbilityDef stunStatusImmunity = DefCache.GetDef<AbilityDef>("StunStatusImmunity_AbilityDef");
                //AbilityDef empImmunity = DefCache.GetDef<AbilityDef>("EMPImmunity_DamageMultiplierAbilityDef");

                DamageMultiplierStatusDef cyclopsDefense_StatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
                DamageMultiplierStatusDef selfRepair = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                DamageMultiplierStatusDef poweredUp = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                List<AbilityDef> abilitiesToRemove = new List<AbilityDef>() { poisonResistance };
                List<AbilityDef> abilitiesToAdd = new List<AbilityDef>() { poisonImmunity, paralysisImmunity };

                if (controller.PhoenixFaction.Research.HasCompleted("AncientAutomataResearch"))
                {
                    TFTVLogger.Always("Ancient Automata Research Completed");

                    if (!abilitiesToRemove.Contains(stunStatusImmunity))
                    {
                        abilitiesToRemove.Add(stunStatusImmunity);
                    }
                    /*  if (!abilitiesToRemove.Contains(eMPResistant))
                      {
                          abilitiesToRemove.Add(eMPResistant);
                      }*/

                    AutomataResearched = true;

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
                    /*    if (!abilitiesToAdd.Contains(eMPResistant))
                        {
                            abilitiesToAdd.Add(eMPResistant);
                        }*/

                    AutomataResearched = false;

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

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
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
                //  AbilityDef psychicImmunity = DefCache.GetDef<AbilityDef>("PsychicImmunity_DamageMultiplierAbilityDef");
                AbilityDef paralysisImmunity = DefCache.GetDef<AbilityDef>("ParalysisNotShockImmunity_DamageMultiplierAbilityDef");

                AbilityDef stunStatusImmunity = DefCache.GetDef<AbilityDef>("StunStatusImmunity_AbilityDef");
                //AbilityDef empImmunity = DefCache.GetDef<AbilityDef>("EMPImmunity_DamageMultiplierAbilityDef");

                DamageMultiplierStatusDef cyclopsDefense_StatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
                DamageMultiplierStatusDef selfRepair = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");
                DamageMultiplierStatusDef poweredUp = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");

                List<AbilityDef> abilitiesToRemove = new List<AbilityDef>() { poisonResistance };
                List<AbilityDef> abilitiesToAdd = new List<AbilityDef>() { poisonImmunity, paralysisImmunity };

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

        [HarmonyPatch(typeof(RewardsController), "SetResources")]
        public static class RewardsController_SetResources_Patch
        {

            public static void Postfix(ResourcePack reward, RewardsController __instance)
            {
                try
                {
                    GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));

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
                                RemoveManuallySetObjective(controller, "OBTAIN_PM_OBJECTIVE");
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
                                RemoveManuallySetObjective(controller, "OBTAIN_LC_OBJECTIVE");

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

                                RemoveManuallySetObjective(controller, "BUILD_CYCLOPS_OBJECTIVE");

                            }
                            //if the player is defeated, the Cyclops variable will be reset so that the player may try again
                            else if (viewerFactionResult.State == TacFactionState.Defeated)
                            {
                                controller.EventSystem.SetVariable(CyclopsBuiltVariable, 0);

                            }

                            RemoveManuallySetObjective(controller, "PROTECT_THE_CYCLOPS_OBJECTIVE_GEO_TITLE");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

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
                            if (item.TacticalItemDef.Equals(beamHead))
                            {
                                if (roll > 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(rightShield) || item.TacticalItemDef.Equals(rightDrill))
                            {
                                if (roll <= 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(leftShield) || item.TacticalItemDef.Equals(leftCrystalShield))
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

        public static void CheckCyclopsDefense()
        {
            try
            {
                if (LOTAReworkActive)
                {
                    CyclopsDefenseStatus.Multiplier = 0.5f + HoplitesKilled * 0.1f;
                    TFTVLogger.Always("Cyclops Defense level is " + CyclopsDefenseStatus.Multiplier);
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
                    if (item.TacticalItemDef.Equals(beamHead))
                    {
                        equipment[0] = item;
                    }
                    else if (item.TacticalItemDef.Equals(rightShield) || item.TacticalItemDef.Equals(rightDrill))
                    {
                        equipment[1] = item;

                    }
                    else if (item.TacticalItemDef.Equals(leftShield) || item.TacticalItemDef.Equals(leftCrystalShield))
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

        public static void AdjustAutomataStats(TacticalLevelController controller)
        {

            try
            {
                TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                {
                    if (tacticalActorBase is TacticalActor guardian && tacticalActorBase.HasGameTag(hopliteTag) && !guardian.Status.HasStatus(AncientGuardianStealthStatus))
                    {
                        if (guardian.CharacterStats.WillPoints < 30)
                        {
                            if (guardian.CharacterStats.WillPoints > 25)
                            {
                                guardian.CharacterStats.WillPoints.Set(30);
                            }
                            else
                            {
                                guardian.CharacterStats.WillPoints.Add(5);

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
                    else if (tacticalActorBase is TacticalActor cyclops && tacticalActorBase.HasGameTag(cyclopsTag))
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
                                    cyclops.CharacterStats.WillPoints.Add(5);

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
                    GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));

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
                    GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));


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

        [HarmonyPatch(typeof(TacticalFaction), "RequestEndTurn")]
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
        }


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
                                            actorAlly.CharacterStats.WillPoints.Add(5);

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
                                    if (CyclopsDefenseStatus.Multiplier <= 0.9f)
                                    {

                                        CyclopsDefenseStatus.Multiplier += 0.1f;
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
                                    HoplitesKilled++;

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

        public static void CheckForAutoRepairAbility(TacticalLevelController controller)
        {
            try
            {

                TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                {
                    if (tacticalActorBase is TacticalActor)
                    {
                        TacticalActor actor = tacticalActorBase as TacticalActor;

                        if (actor.HasStatus(AddAutoRepairStatusAbility))
                        {
                            Weapon drill = new Weapon();
                            Weapon shield = new Weapon();
                            Equipment livingShield = new Equipment();
                            Equipment orichalcumShield = new Equipment();

                            foreach (Equipment item in actor.Equipments.Equipments)
                            {
                                if (item.TacticalItemDef == rightDrill)
                                {
                                    drill = item as Weapon;

                                }
                                else if (item.TacticalItemDef == rightShield)
                                {
                                    shield = item as Weapon;

                                }
                                else if (item.TacticalItemDef == leftCrystalShield)
                                {
                                    livingShield = item;

                                }
                                else if (item.TacticalItemDef == leftShield)
                                {
                                    orichalcumShield = item;
                                }
                            }

                            TFTVLogger.Always("Actor has spare parts, making repairs");
                            actor.Status.Statuses.Remove(actor.Status.GetStatusByName(AddAutoRepairStatusAbility.EffectName));
                            if (CheckGuardianBodyParts(actor)[0] == null)
                            {
                                actor.Equipments.AddItem(beamHead);
                            }
                            else if (CheckGuardianBodyParts(actor)[1] == null && livingShield != null)
                            {
                                actor.Equipments.AddItem(rightDrill);
                            }
                            else if (CheckGuardianBodyParts(actor)[1] == null && orichalcumShield != null)
                            {
                                actor.Equipments.AddItem(rightShield);
                            }
                            else if (CheckGuardianBodyParts(actor)[2] == null && drill != null)
                            {
                                actor.Equipments.AddItem(leftCrystalShield);
                            }
                            else if (CheckGuardianBodyParts(actor)[2] == null && shield != null)
                            {
                                actor.Equipments.AddItem(leftShield);
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



