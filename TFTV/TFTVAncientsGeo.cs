using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using HarmonyLib;
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
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Common.Entities.Items.ItemManufacturing;

namespace TFTV
{
    internal class TFTVAncientsGeo
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
      

        public static readonly string CyclopsBuiltVariable = "CyclopsBuiltVariable";
        //   public static bool LOTAReworkActive = false;
        public static bool AutomataResearched = false;

        //This is the number of previous encounters with Ancients. It is added to the Difficulty to determine the number of fully repaired MediumGuardians in battle
        public static int AncientsEncounterCounter = 0;
        public static string AncientsEncounterVariableName = "Ancients_Encounter_Global_Variable";

        public static Dictionary<int, int> CyclopsMolecularDamageBuff = new Dictionary<int, int> { }; //turn number + 0 = none, 1 = mutation, 2 = bionic

        public static List<string> AlertedHoplites = new List<string>();


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
                if (TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting)
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



        //Adjusts exotic resources received as reward
        [HarmonyPatch(typeof(RewardsController), "SetResources")]
        public static class RewardsController_SetResources_Patch
        {

            public static void Postfix(ResourcePack reward, RewardsController __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();


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

                    __result = false;

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

                    if (guardianTags.Any(tag => ancientSite.GameTags.Contains(tag)))
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


                    controller.EventSystem.SetVariable(CyclopsBuiltVariable, 1);
                    GeoSite geoSite = (GeoSite)target.Actor;

                    controller.AlienFaction.AttackAncientSite(geoSite, 8);

                    GeoscapeEventContext context = new GeoscapeEventContext(controller.AlienFaction, controller.PhoenixFaction);
                    controller.EventSystem.TriggerGeoscapeEvent("Helena_Beast", context);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Prevents player from harvesting from Ancient site right after winning mission
        //Also triggers a bunch of things after story mission is completed

        public static void EnsureNoHarvesting(GeoLevelController controller)
        {
            try
            {
                List<GeoVehicle> geoVehicles = controller.PhoenixFaction.Vehicles.ToList();
                foreach (GeoVehicle vehicle in geoVehicles)
                {
                    vehicle.EndCollectingFromCurrentSite();

                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
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

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
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


                    __result = new ResourcePack() { new ResourceUnit(ResourceType.Materials, value: 20), new ResourceUnit(ResourceType.Tech, value: 5) };

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

    }
}
