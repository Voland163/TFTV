using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Abilities;
using Base.Utils;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using static UITooltip;

namespace TFTV
{
    internal class TFTVScavengers
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static ClassTagDef _assaultTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");

        internal static ClassTagDef _heavyTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
        internal static ClassTagDef _infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");

        internal static ClassTagDef _sniperTag = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
        internal static ClassTagDef _priestTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
        internal static ClassTagDef _technicianTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");

        internal static ClassTagDef _assaultRaiderTag;
        internal static ClassTagDef _heavyRaiderTag;
        internal static ClassTagDef _sniperRaiderTag;
        internal static ClassTagDef _scumTag;

        private static Dictionary<UnitTemplateResearchRewardDef, List<TacCharacterDef>> _banditTemplatesTiedToPandoranUnlocks = new Dictionary<UnitTemplateResearchRewardDef, List<TacCharacterDef>>()
        {

        };


        [HarmonyPatch(typeof(UnitTemplateResearchReward), "GiveReward")]
        public static class UnitTemplateResearchReward_GiveReward_patch
        {
            public static void Postfix(UnitTemplateResearchReward __instance, GeoFaction faction)
            {
                try
                {
                    if (_banditTemplatesTiedToPandoranUnlocks.ContainsKey(__instance.RewardDef))
                    {
                        foreach (TacCharacterDef template in _banditTemplatesTiedToPandoranUnlocks[__instance.RewardDef])
                        {
                            string factionName = template.name.Split('_')[0];

                            GeoFaction geoFaction = faction.GeoLevel.Factions.FirstOrDefault(f => f.PPFactionDef.ShortNames.Contains(factionName)) ?? faction.GeoLevel.NeutralFaction;
                            if (!geoFaction.UnlockedUnitTemplates.Contains(template))
                            {
                                TFTVLogger.Always($"Adding {template.name} to {geoFaction.PPFactionDef.name} for {__instance.RewardDef.name}");
                                geoFaction.UnlockedUnitTemplates.Add(template);
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

       /* public static void ImplementScavengerSpecialMissions(TacticalLevelController controller)
        {
            try 
            {
                if (controller.TacMission.MissionData.MissionType.name.Equals("StoryPX13_CustomMissionTypeDef")) 
                {
                    TacticalDeployZone deployZone = TFTVTacticalUtils.FindTDZ("Deploy_Player_3x3_Vehicle");

                    TacticalDeployZoneDef deployZoneDef = DefCache.GetDef<TacticalDeployZoneDef>("Neutral_Bandits_DeployZoneDef");
                    TacticalDeployZone newDeployZone = ActorSpawner.SpawnActor<TacticalDeployZone>(deployZoneDef);


                   

                    TacActorData tacActorData = new TacActorData
                    {
                        ComponentSetTemplate = deployZoneDef.ComponentSet
                    };


                    StructuralTargetInstanceData structuralTargetInstanceData = tacActorData.GenerateInstanceData() as StructuralTargetInstanceData;
                    //  structuralTargetInstanceData.FacilityID = facilityID;
                    structuralTargetInstanceData.SourceTemplate = stdDef;
                    structuralTargetInstanceData.Source = tacActorData;


                    StructuralTarget structuralTarget = ActorSpawner.SpawnActor<StructuralTarget>(tacActorData.GenerateInstanceComponentSetDef(), structuralTargetInstanceData, callEnterPlayOnActor: false);
                    GameObject obj = structuralTarget.gameObject;
                    structuralTarget.name = name;
                    structuralTarget.Source = obj;

                    var ipCols = new GameObject("InteractionPointColliders");
                    ipCols.transform.SetParent(obj.transform);
                    ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                    ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                    var collider = ipCols.AddComponent<BoxCollider>();


                    structuralTarget.Initialize();
                    //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                    structuralTarget.DoEnterPlay();


                }
           
            }
            catch (Exception e) 
            {
                TFTVLogger.Error(e); 
            }    
        }*/
        

        internal class Defs
        {
            //weak armors
            private static readonly TacticalItemDef neuAssaultJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuAssaultLegs = DefCache.GetDef<TacticalItemDef>("NEU_Assault_Legs_ItemDef");

            private static readonly TacticalItemDef neuHeavyBandana = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Helmet_BodyPartDef");
            private static readonly TacticalItemDef neuHeavyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuHeavyLegs = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Legs_ItemDef");

            private static readonly TacticalItemDef neuSniperBandana = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Helmet_BodyPartDef");
            private static readonly TacticalItemDef neuSniperCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuSniperLegs = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Legs_ItemDef");

            //indie armors
            private static readonly TacticalItemDef inAssaultHelmet = DefCache.GetDef<TacticalItemDef>("IN_Assault_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inAssaultTorso = DefCache.GetDef<TacticalItemDef>("IN_Assault_Torso_BodyPartDef");
            private static readonly TacticalItemDef inAssaultLegs = DefCache.GetDef<TacticalItemDef>("IN_Assault_Legs_ItemDef");

            private static readonly TacticalItemDef inHeavyHelmet = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inHeavyTorso = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Torso_BodyPartDef");
            private static readonly TacticalItemDef inHeavyLegs = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Legs_ItemDef");

            private static readonly TacticalItemDef inSniperHelmet = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inSniperTorso = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Torso_BodyPartDef");
            private static readonly TacticalItemDef inSniperLegs = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Legs_ItemDef");

            //acceptable merc armors
            internal static TacticalItemDef goldAssaultHelmet = DefCache.GetDef<TacticalItemDef>("PX_Assault_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultLegs = DefCache.GetDef<TacticalItemDef>("PX_Assault_Legs_Gold_ItemDef");

            internal static TacticalItemDef goldHeavyHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavyTorso = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavytLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Gold_ItemDef");

            internal static TacticalItemDef spyMasterTorso = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef");
            internal static TacticalItemDef spyMasterHelmet = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef");
            internal static TacticalItemDef spyMasterLegs = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef");

            internal static TacticalItemDef sectarianHelmet = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_Viking_ItemDef");

            internal static TacticalItemDef doomLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Headhunter_ItemDef");
            internal static TacticalItemDef njAssaultTorso = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Torso_BodyPartDef");//PX_Heavy_Torso_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomJetpack = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_JumpPack_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Headhunter_BodyPartDef");

            internal static TacticalItemDef risingSunTorso = DefCache.GetDef<TacticalItemDef>("PX_Sniper_Torso_RisingSun_BodyPartDef");


            internal static WeaponDef sectarianAxe = DefCache.GetDef<WeaponDef>("AN_Blade_Viking_WeaponDef");
            internal static WeaponDef neXbow = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
            internal static WeaponDef anuPistol = DefCache.GetDef<WeaponDef>("AN_HandCannon_WeaponDef");

            internal static TacticalItemDef _heavyNakedArmLeft;
            internal static TacticalItemDef _heavyNakedArmRight;




            internal static WeaponDef grenade = DefCache.GetDef<WeaponDef>("PX_HandGrenade_WeaponDef");
           
            internal static SpecializationDef _scumSpecialization;
            internal static SpecializationDef _assaultRaiderSpecialization;
            internal static SpecializationDef _heavyRaiderSpecialization;
            internal static SpecializationDef _sniperRaiderSpecialization;

            internal static readonly WeaponDef _tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

            private static List<TacticalItemDef> _inHeavyTorsoArmorsNaked = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _assaultGoldTorsoArmorsNaked = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _heavyGoldTorsoArmorsNaked = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _doomNaked = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _inHeavyTorsoArmorsLight = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _assaultGoldTorsoArmorsLight = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _heavyGoldTorsoArmorsLight = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _doomLight = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _inHeavyTorsoArmorsMedium = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _assaultGoldTorsoArmorsMedium = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _heavyGoldTorsoArmorsMedium = new List<TacticalItemDef>(3);
            private static List<TacticalItemDef> _doomMedium = new List<TacticalItemDef>(3);

          
            internal class Armors 
            {
                internal static void ArmorDefs()
                {
                    try
                    {
                        CreateWeakArmors();
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static void CreateWeakArmors()
                {
                    try
                    {
                        List<BodyPartAspectDef> bodyPartAspectDefs = new List<BodyPartAspectDef>()
                    {
                    neuHeavyBandana.BodyPartAspectDef, neuHeavyTshirt.BodyPartAspectDef, neuHeavyLegs.BodyPartAspectDef,
                    neuSniperBandana.BodyPartAspectDef, neuSniperCoat.BodyPartAspectDef, neuSniperLegs.BodyPartAspectDef,
                    neuAssaultJacket.BodyPartAspectDef, neuAssaultLegs.BodyPartAspectDef
                    };

                        TacticalItemDef sourceNakedHeavyArmLeft = DefCache.GetDef<TacticalItemDef>("NJ_TobiasWest_LeftArm_BodyPartDef");
                        TacticalItemDef sourceNakedHeavyArmRight = DefCache.GetDef<TacticalItemDef>("NJ_TobiasWest_RightArm_BodyPartDef");

                        _heavyNakedArmLeft = Helper.CreateDefFromClone(sourceNakedHeavyArmLeft, "{D9D4FC78-B5F5-4D85-B3F2-3E16A0F1028A}", "HeavyNakedArmLeft");
                        _heavyNakedArmRight = Helper.CreateDefFromClone(sourceNakedHeavyArmRight, "{CC284239-492C-4EB7-9EAA-061268547F08}", "HeavyNakedArmRight");

                        TacticalItemDef leftArmJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_RightArm_BodyPartDef");

                        TacticalItemDef leftArmDirtyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmDirtyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_RightArm_BodyPartDef");

                        TacticalItemDef leftArmCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_RightArm_BodyPartDef");

                        TacticalItemDef leftLegHeavyJeans = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftLeg_BodyPartDef");
                        TacticalItemDef rightLegHeavyJeans = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_RightLeg_BodyPartDef");

                        TacticalItemDef leftLegAssaultJeans = DefCache.GetDef<TacticalItemDef>("NEU_Assault_LeftLeg_BodyPartDef");
                        TacticalItemDef righLegAssaultJeans = DefCache.GetDef<TacticalItemDef>("NEU_Assault_RightLeg_BodyPartDef");

                        TacticalItemDef leftLegSniperJeans = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_LeftLeg_BodyPartDef");
                        TacticalItemDef rightLegSniperJeans = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_RightLeg_BodyPartDef");

                        foreach (BodyPartAspectDef bodyPartAspectDef in bodyPartAspectDefs)
                        {
                            bodyPartAspectDef.Speed = 0;
                            bodyPartAspectDef.Accuracy = 0;
                            bodyPartAspectDef.Endurance = 0;
                            bodyPartAspectDef.Perception = 0;
                            bodyPartAspectDef.Stealth = 0;
                            bodyPartAspectDef.WillPower = 0;
                        }

                        _heavyNakedArmLeft.Armor = 8;
                        _heavyNakedArmRight.Armor = 8;

                        neuHeavyBandana.Armor = 0;
                        neuHeavyBandana.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Bandana.png");
                        neuSniperBandana.Armor = 0;
                        neuSniperBandana.ViewElementDef.DisplayName2.LocalizationKey = "KEY_TACTICAL_BANDANA_NAME2";
                        neuSniperBandana.ViewElementDef.Description.LocalizationKey = "KEY_WASTELAND_HEADGEAR_DESCRIPTION";
                        neuSniperBandana.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Bandana.png");

                        neuHeavyTshirt.Armor = 0;
                        neuHeavyTshirt.ViewElementDef.DisplayName2.LocalizationKey = "KEY_TSHIRT_NAME2";
                        neuHeavyTshirt.ViewElementDef.Description.LocalizationKey = "KEY_WASTELAND_BODY_ARMOR_DESCRIPTION";
                        neuHeavyTshirt.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Tshirt.png");
                        leftArmDirtyTshirt.Armor = 0;
                        rightArmDirtyTshirt.Armor = 0;

                        neuAssaultJacket.Armor = 10;
                        neuAssaultJacket.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Jacket.png");
                        leftArmJacket.Armor = 10;
                        rightArmJacket.Armor = 10;

                        neuSniperCoat.ViewElementDef.DisplayName2.LocalizationKey = "KEY_COAT_NAME2";
                        neuSniperCoat.ViewElementDef.Description.LocalizationKey = "KEY_WASTELAND_BODY_ARMOR_DESCRIPTION";
                        neuSniperCoat.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Coat.png");
                        neuSniperCoat.Armor = 10;
                        leftArmCoat.Armor = 10;
                        rightArmCoat.Armor = 10;

                        neuHeavyLegs.Armor = 8;
                        neuHeavyLegs.ViewElementDef.DisplayName2.LocalizationKey = "KEY_WASTELAND_LEGS_NAME2";
                        neuHeavyLegs.ViewElementDef.Description.LocalizationKey = "KEY_WASTELAND_LEGS_DESCRIPTION";
                        neuHeavyLegs.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Jeans.png");
                        leftLegHeavyJeans.Armor = 8;
                        rightLegHeavyJeans.Armor = 8;

                        neuAssaultLegs.Armor = 8;
                        neuAssaultLegs.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Jeans.png");
                        leftLegAssaultJeans.Armor = 8;
                        righLegAssaultJeans.Armor = 8;

                        neuSniperLegs.Armor = 8;
                        neuSniperLegs.ViewElementDef.DisplayName2.LocalizationKey = "KEY_WASTELAND_LEGS_NAME2";
                        neuSniperLegs.ViewElementDef.Description.LocalizationKey = "KEY_WASTELAND_LEGS_DESCRIPTION";
                        neuSniperLegs.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile("Raider_Icon_Jeans.png");
                        leftLegSniperJeans.Armor = 8;
                        rightLegSniperJeans.Armor = 8;

                        CreateMixedTorsoArmsArmors();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
              
                private static void CreateMixedTorsoArmsArmors()
                {
                    try
                    {
                        CreateNewArmorsAssault(
                            new List<List<TacticalItemDef>>() { _assaultGoldTorsoArmorsNaked, _assaultGoldTorsoArmorsLight, _assaultGoldTorsoArmorsMedium }
                       , goldAssaultTorso, new List<string>()
                       {
                       "3b6fcb7f-82b2-4bbe-8230-fca6ce9dc9ba","acf9d97d-b4e4-4c20-b9fb-39b856070e13","af6400f9-9171-4c4b-be0e-989f264512c2",
                       },
                            new List<string>()
                            {
                            "f41c9ab2-e545-4b86-9d36-b002f82e6647","73a34faa-702d-4c10-a533-9272f2dd05f8","f952b3bd-d9ad-4637-b95c-12eb4078af69"
                            },
                            new List<string>()
                            {
                            "129a9ce6-23c8-4ce0-9c45-b974d32fea5f", "c9388e4a-fe9b-44b4-b1a3-c7441db929b4", "dfe8ed7f-8808-4112-a9dc-55c4615c49df",
                            });

                        CreateNewArmorsAssault(
                          new List<List<TacticalItemDef>>() { _doomNaked, _doomLight, _doomMedium }
                     , njAssaultTorso, new List<string>()
                     {
                       "472a8fb4-2a54-4077-afad-f649f053ebb2",  "0e80968d-8adc-49b8-9e45-c182610a63ce", "499b2c8d-e230-4f8d-af92-25258c097f7b",
                     },
                          new List<string>()
                          {
                            "fa0e687a-9cba-47bf-8191-c5839fd71ef6",  "60a1c892-0a14-444a-8c3d-b5ae9173a65e", "88046d10-5296-40cc-8e57-048e547e19d7",
                          },
                          new List<string>()
                          {
                            "9e0ddc67-b280-4b1d-a7eb-d19092a3be73",  "e7f02362-c6f3-499c-b91e-1f536070a334",  "385e134e-771f-4c92-abb2-9775335a9aae",
                          });

                        CreateNewArmorsHeavy(
                            new List<List<TacticalItemDef>>() { _heavyGoldTorsoArmorsNaked, _heavyGoldTorsoArmorsLight, _heavyGoldTorsoArmorsMedium }
                       , goldHeavyTorso, new List<string>()
                       {
                       "0b6da268-6d42-4b6d-8d3a-78c83a00ceea", "972b0bcc-be55-4441-b9ef-fe27fcf771b4", "178ffc97-e5e0-4431-9d8c-05e9687507c6",
                       },
                            new List<string>()
                            {
                            "1e3de145-8eca-45a8-a3fb-6fe3af113e88", "29215417-ff23-4752-94f1-76d7526a5631",  "b3c92980-93ad-4190-b111-a2f19391fde3",
                            },
                            new List<string>()
                            {
                             "08c15a33-91ec-48c6-8639-699d1b4f49d8", "0993997c-ceef-4342-b6ab-145461c2bd5d",  "59eb294a-b0e6-4d3e-9ce1-f94bc3d8e8c0",
                            });


                        CreateNewArmorsHeavy(
                          new List<List<TacticalItemDef>>() { _inHeavyTorsoArmorsNaked, _inHeavyTorsoArmorsLight, _inHeavyTorsoArmorsMedium }
                     , inHeavyTorso, new List<string>()
                     {
                      "059b8c13-7dc3-41e9-954f-8be9ddf866ed", "7558c284-b7fc-4432-9415-09d82fe8121f", "12a017ff-f152-4c9e-9fc8-552232e90586",

                     },
                          new List<string>()
                          {
                           "a58dca44-03e2-4977-8932-ea2d2160cfac", "eed97fb1-9c6c-4cfa-91e1-10073f215483", "07c9eaf9-3ffd-4b09-9e3a-f3b5453a7024",
                          },
                          new List<string>()
                          {
                           "88a836d4-e50d-4ac7-9b10-92fecaa168d7", "cc5512aa-8973-413a-8c1a-7bcb8cf63958", "2163d21c-2e12-484c-830d-a5f7ceb84ec2",
                          });

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
                
                private static void CreateNewArmorsAssault(List<List<TacticalItemDef>> armorPiecesToModify, TacticalItemDef original, List<string> gUIDs0, List<string> gUIDs1, List<string> gUIDs2)
                {
                    try
                    {

                        List<AddonDef.SubaddonBind> subaddonBinds = new List<AddonDef.SubaddonBind>(original.SubAddons) { };
                        List<AddonDef.SubaddonBind> withoutJetpack = new List<AddonDef.SubaddonBind>(original.SubAddons) { };

                        if (subaddonBinds.Count() > 2)
                        {
                            withoutJetpack.Remove(original.SubAddons[2]);
                            original.SubAddons = withoutJetpack.ToArray();
                        }

                        //NEU_Sniper, NEU_Assault, NEU_Heavy too short
                        //SY_Leader_LeftArm_BodyPartDef and SY_Leader_RightArm_BodyPartDef for AssaultGoldNaked
                        //NJ_TobiasWest_LeftArm_BodyPartDef and NJ_TobiasWest_RightArm_BodyPartDef for HeavyGoldNaked
                        //SY_Infiltrator_Venom_ too thin for heavy armor, too short for assault armor
                        //PX_Heavy_RightArm_Headhunter_BodyPartDef too thin for heavy armor, works for assault armor
                        //Santa heavy uselss, for a change.
                        //AN_Assault_RightArm_BodyPartDef pretty good for assault

                        TacticalItemDef leftArmNakedAssault = DefCache.GetDef<TacticalItemDef>("SY_Leader_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmNakedAssault = DefCache.GetDef<TacticalItemDef>("SY_Leader_RightArm_BodyPartDef");

                        List<TacticalItemDef> nakedArmsAssault = new List<TacticalItemDef>() { leftArmNakedAssault, rightArmNakedAssault };
                        List<TacticalItemDef> nakedArmsHeavy = new List<TacticalItemDef>() { _heavyNakedArmLeft, _heavyNakedArmRight };

                        TacticalItemDef leftArmLight = DefCache.GetDef<TacticalItemDef>("IN_Sniper_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmLight = DefCache.GetDef<TacticalItemDef>("IN_Sniper_RightArm_BodyPartDef");

                        List<TacticalItemDef> ligthArms = new List<TacticalItemDef>() { leftArmLight, rightArmLight };

                        TacticalItemDef leftArmMedium = DefCache.GetDef<TacticalItemDef>("PX_Heavy_LeftArm_Headhunter_BodyPartDef");
                        TacticalItemDef rightArmMedium = DefCache.GetDef<TacticalItemDef>("PX_Heavy_RightArm_Headhunter_BodyPartDef");

                        List<TacticalItemDef> mediumArms = new List<TacticalItemDef>() { leftArmMedium, rightArmMedium };

                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[0].Add(Helper.CreateDefFromClone(original, gUIDs0[x], original.name + nakedArmsAssault[x].name));
                                armorPiecesToModify[0][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = nakedArmsAssault[x] };
                            }
                            else
                            {
                                armorPiecesToModify[0].Add(Helper.CreateDefFromClone(armorPiecesToModify[0][0], gUIDs0[x], original.name + "withNakedArms"));
                                armorPiecesToModify[0][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = nakedArmsAssault[1] };
                            }
                            //   TFTVLogger.Always($"armor {armorPiecesToModify[0][x].name} created");
                        }

                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[1].Add(Helper.CreateDefFromClone(original, gUIDs1[x], original.name + ligthArms[x].name));
                                armorPiecesToModify[1][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = ligthArms[x] };
                            }
                            else
                            {
                                armorPiecesToModify[1].Add(Helper.CreateDefFromClone(armorPiecesToModify[1][0], gUIDs1[x], original.name + "withLightArms"));
                                armorPiecesToModify[1][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = ligthArms[1] };
                            }
                            //   TFTVLogger.Always($"armor {armorPiecesToModify[1][x].name} created");
                        }

                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[2].Add(Helper.CreateDefFromClone(original, gUIDs2[x], original.name + mediumArms[x].name));
                                armorPiecesToModify[2][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = mediumArms[x] };
                            }
                            else
                            {
                                armorPiecesToModify[2].Add(Helper.CreateDefFromClone(armorPiecesToModify[2][0], gUIDs2[x], original.name + "withMediumArms"));
                                armorPiecesToModify[2][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = mediumArms[1] };
                            }
                            //  TFTVLogger.Always($"armor {armorPiecesToModify[2][x].name} created");
                        }

                        if (subaddonBinds.Count() > 2)
                        {
                            original.SubAddons = subaddonBinds.ToArray();
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                
                private static void CreateNewArmorsHeavy(List<List<TacticalItemDef>> armorPiecesToModify, TacticalItemDef original, List<string> gUIDs0, List<string> gUIDs1, List<string> gUIDs2)
                {
                    try
                    {

                        List<AddonDef.SubaddonBind> subaddonBinds = new List<AddonDef.SubaddonBind>(original.SubAddons) { };
                        List<AddonDef.SubaddonBind> withoutJetpack = new List<AddonDef.SubaddonBind>(original.SubAddons) { };
                        List<AbilityDef> savedAbilities = new List<AbilityDef>(original.Abilities);

                        if (subaddonBinds.Count() > 2)
                        {
                            withoutJetpack.Remove(original.SubAddons[2]);
                            original.SubAddons = withoutJetpack.ToArray();
                        }

                        original.Abilities = new AbilityDef[] { };


                        //NEU_Sniper, NEU_Assault, NEU_Heavy too short
                        //SY_Leader_LeftArm_BodyPartDef and SY_Leader_RightArm_BodyPartDef for AssaultGoldNaked
                        //NJ_TobiasWest_LeftArm_BodyPartDef and NJ_TobiasWest_RightArm_BodyPartDef for HeavyGoldNaked
                        //SY_Infiltrator_Venom_ too thin for heavy armor, too short for assault armor
                        //PX_Heavy_RightArm_Headhunter_BodyPartDef too thin for heavy armor, works for assault armor
                        //Santa heavy uselss, for a change.
                        //AN_Assault_RightArm_BodyPartDef pretty good for assault

                        TacticalItemDef leftArmNakedAssault = DefCache.GetDef<TacticalItemDef>("SY_Leader_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmNakedAssault = DefCache.GetDef<TacticalItemDef>("SY_Leader_RightArm_BodyPartDef");

                        List<TacticalItemDef> nakedArmsAssault = new List<TacticalItemDef>() { leftArmNakedAssault, rightArmNakedAssault };
                        List<TacticalItemDef> nakedArmsHeavy = new List<TacticalItemDef>() { _heavyNakedArmLeft, _heavyNakedArmRight };

                        TacticalItemDef leftArmLight = DefCache.GetDef<TacticalItemDef>("IN_Sniper_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmLight = DefCache.GetDef<TacticalItemDef>("IN_Sniper_RightArm_BodyPartDef");

                        List<TacticalItemDef> ligthArms = new List<TacticalItemDef>() { leftArmLight, rightArmLight };

                        TacticalItemDef leftArmMedium = DefCache.GetDef<TacticalItemDef>("IN_Assault_LeftArm_BodyPartDef");
                        TacticalItemDef rightArmMedium = DefCache.GetDef<TacticalItemDef>("IN_Assault_RightArm_BodyPartDef");

                        List<TacticalItemDef> mediumArms = new List<TacticalItemDef>() { leftArmMedium, rightArmMedium };



                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[0].Add(Helper.CreateDefFromClone(original, gUIDs0[x], original.name + nakedArmsHeavy[x].name));
                                armorPiecesToModify[0][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = nakedArmsHeavy[x] };
                            }
                            else
                            {
                                armorPiecesToModify[0].Add(Helper.CreateDefFromClone(armorPiecesToModify[0][0], gUIDs0[x], original.name + "withNakedArms"));
                                armorPiecesToModify[0][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = nakedArmsHeavy[1] };
                            }
                            //   TFTVLogger.Always($"armor {armorPiecesToModify[0][x].name} created");
                        }

                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[1].Add(Helper.CreateDefFromClone(original, gUIDs1[x], original.name + ligthArms[x].name));
                                armorPiecesToModify[1][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = ligthArms[x] };
                            }
                            else
                            {
                                armorPiecesToModify[1].Add(Helper.CreateDefFromClone(armorPiecesToModify[1][0], gUIDs1[x], original.name + "withLightArms"));
                                armorPiecesToModify[1][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = ligthArms[1] };
                            }
                            //   TFTVLogger.Always($"armor {armorPiecesToModify[1][x].name} created");
                        }

                        for (int x = 0; x < 3; x++)
                        {
                            if (x < 2)
                            {
                                armorPiecesToModify[2].Add(Helper.CreateDefFromClone(original, gUIDs2[x], original.name + mediumArms[x].name));
                                armorPiecesToModify[2][x].SubAddons[x] = new AddonDef.SubaddonBind() { SubAddon = mediumArms[x] };
                            }
                            else
                            {
                                armorPiecesToModify[2].Add(Helper.CreateDefFromClone(armorPiecesToModify[2][0], gUIDs2[x], original.name + "withMediumArms"));
                                armorPiecesToModify[2][x].SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = mediumArms[1] };
                            }
                            //  TFTVLogger.Always($"armor {armorPiecesToModify[2][x].name} created");
                        }

                        if (subaddonBinds.Count() > 2)
                        {
                            original.SubAddons = subaddonBinds.ToArray();
                        }

                        original.Abilities = savedAbilities.ToArray();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            internal class Templates 
            {

                private static List<TacticalItemDef> CreateArmorSets(TacticalItemDef helmet, TacticalItemDef torso, TacticalItemDef legs)
                {
                    try
                    {
                        return new List<TacticalItemDef>() { helmet, torso, legs };
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static List<ItemDef> CreateWeaponSet(ItemDef mainWeapon, ItemDef secondItem, ItemDef thirdItem)
                {
                    try
                    {
                        return new List<ItemDef>() { mainWeapon, secondItem, thirdItem };
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateHeavyTemplates()
                {
                    try
                    {
                        WeaponDef neMG = DefCache.GetDef<WeaponDef>("NE_MachineGun_WeaponDef");
                        WeaponDef njMG = DefCache.GetDef<WeaponDef>("NJ_Gauss_MachineGun_WeaponDef");
                        WeaponDef njFlamer = DefCache.GetDef<WeaponDef>("NJ_FlameThrower_WeaponDef");
                        WeaponDef pxGL = DefCache.GetDef<WeaponDef>("PX_GrenadeLauncher_WeaponDef");
                        WeaponDef devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
                        WeaponDef doomAC = DefCache.GetDef<WeaponDef>("PX_HeavyCannon_Headhunter_WeaponDef");

                        List<List<List<ItemDef>>> weapons = new List<List<List<ItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            weapons.Add(new List<List<ItemDef>>());
                        }
                        weapons[0].Add(CreateWeaponSet(neMG, neMG.CompatibleAmmunition[0], neMG.CompatibleAmmunition[0]));
                        weapons[0].Add(CreateWeaponSet(doomAC, doomAC.CompatibleAmmunition[0], doomAC.CompatibleAmmunition[0]));
                        weapons[1].Add(CreateWeaponSet(neMG, neMG.CompatibleAmmunition[0], neMG.CompatibleAmmunition[0]));
                        weapons[1].Add(CreateWeaponSet(doomAC, doomAC.CompatibleAmmunition[0], doomAC.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(njMG, njMG.CompatibleAmmunition[0], njMG.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(doomAC, doomAC.CompatibleAmmunition[0], doomAC.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(njMG, njMG.CompatibleAmmunition[0], njMG.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(neMG, neMG.CompatibleAmmunition[0], neMG.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(njFlamer, njFlamer.CompatibleAmmunition[0], njFlamer.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(njMG, njMG.CompatibleAmmunition[0], njMG.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(devastator, devastator.CompatibleAmmunition[0], devastator.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(pxGL, pxGL.CompatibleAmmunition[0], pxGL.CompatibleAmmunition[0]));


                        List<List<List<TacticalItemDef>>> armors = new List<List<List<TacticalItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            armors.Add(new List<List<TacticalItemDef>>());
                        }

                        armors[0].Add(CreateArmorSets(neuSniperBandana, neuAssaultJacket, inSniperLegs));
                        armors[0].Add(CreateArmorSets(neuSniperBandana, _assaultGoldTorsoArmorsNaked[2], neuSniperLegs));
                        armors[1].Add(CreateArmorSets(neuSniperBandana, _heavyGoldTorsoArmorsNaked[2], inSniperLegs));
                        armors[1].Add(CreateArmorSets(spyMasterHelmet, _doomNaked[2], neuAssaultLegs));
                        armors[2].Add(CreateArmorSets(neuSniperBandana, _heavyGoldTorsoArmorsNaked[1], spyMasterLegs));
                        armors[2].Add(CreateArmorSets(spyMasterHelmet, _inHeavyTorsoArmorsNaked[0], doomLegs));
                        armors[3].Add(CreateArmorSets(inAssaultHelmet, _heavyGoldTorsoArmorsLight[2], spyMasterLegs));
                        armors[3].Add(CreateArmorSets(inHeavyHelmet, _doomLight[2], inAssaultLegs));
                        armors[4].Add(CreateArmorSets(inAssaultHelmet, _heavyGoldTorsoArmorsMedium[2], doomLegs));
                        armors[4].Add(CreateArmorSets(inHeavyHelmet, _heavyGoldTorsoArmorsMedium[1], inHeavyLegs));
                        armors[5].Add(CreateArmorSets(goldHeavyHelmet, _doomMedium[0], inHeavyLegs));
                        armors[5].Add(CreateArmorSets(inHeavyHelmet, neuHeavyTshirt, inHeavyLegs));

                        List<string> gUIDS = new List<string>()
                    {
                         "c60855c0-3919-46b7-bc1b-c5761f9f48f6",
 "4a6e948d-0788-48a9-930f-127e70370c30",
 "7483496c-2bd9-4884-bca7-28d02cbd70f2",
 "8990dee4-4f8c-4eed-ab6a-17c8bd6d2cbb",
 "70845461-d420-4439-add1-1c275a4e1566",
 "6486bc23-f5de-4248-a937-d396e3e9a560",
 "5357b15c-5507-4454-adb4-049170404868",
 "de1a0919-06af-4fe2-b6c1-8eadcf74ce79",
 "77b1dc5b-63fe-45cc-9708-05046acd26e0",
 "a6e4a57a-2b74-4cf5-8846-6d7e0906eafc",
 "4b1a08b9-f7ed-4a69-8427-ecf22582f425",
 "cc277182-0585-468c-ab75-02b9bd58a01d",
 "2026144d-affd-4a75-9ab2-b545ca662e11",
 "7a68aa1c-9b19-427d-9a64-d6dd3782b2ad",
 "25dace1a-a682-4836-8329-222fb8ea0097",
 "2bdc31fb-504b-400b-b5a6-f586d14cb9aa",
 "dce80897-3ad9-4160-9073-56fc01c39d5e",
 "b94a65f1-f175-4878-bb77-b3c691e63e9d"


                    };


                        CreateRaiderTemplateSet(armors, weapons, _heavyRaiderTag, gUIDS);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
                private static void CreateSniperTemplates()
                {
                    try
                    {
                        WeaponDef nePistol = DefCache.GetDef<WeaponDef>("NE_Pistol_WeaponDef");
                        WeaponDef pxPistol = DefCache.GetDef<WeaponDef>("PX_Pistol_WeaponDef");
                        WeaponDef anuPistol = DefCache.GetDef<WeaponDef>("AN_HandCannon_WeaponDef");
                        WeaponDef njPistol = DefCache.GetDef<WeaponDef>("NJ_Gauss_HandGun_WeaponDef");
                        WeaponDef synPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                        WeaponDef kgSniperRifle = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
                        WeaponDef neSniperRifle = DefCache.GetDef<WeaponDef>("NE_SniperRifle_WeaponDef");
                        WeaponDef pxSniperRifle = DefCache.GetDef<WeaponDef>("PX_SniperRifle_RisingSun_WeaponDef");
                        WeaponDef njSniperRifle = DefCache.GetDef<WeaponDef>("NJ_Gauss_SniperRifle_WeaponDef");
                        WeaponDef sySniperRifle = DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef");

                        List<List<List<ItemDef>>> weapons = new List<List<List<ItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            weapons.Add(new List<List<ItemDef>>());
                        }
                        weapons[0].Add(CreateWeaponSet(neSniperRifle, nePistol, neSniperRifle.CompatibleAmmunition[0]));
                        weapons[0].Add(CreateWeaponSet(neSniperRifle, nePistol, neSniperRifle.CompatibleAmmunition[0]));
                        weapons[1].Add(CreateWeaponSet(neSniperRifle, nePistol, neSniperRifle.CompatibleAmmunition[0]));
                        weapons[1].Add(CreateWeaponSet(neSniperRifle, nePistol, neSniperRifle.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(pxSniperRifle, pxPistol, pxSniperRifle.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(pxSniperRifle, pxPistol, pxSniperRifle.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(njSniperRifle, njPistol, njSniperRifle.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(sySniperRifle, synPistol, sySniperRifle.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(njSniperRifle, njPistol, njSniperRifle.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(sySniperRifle, synPistol, sySniperRifle.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(kgSniperRifle, njPistol, kgSniperRifle.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(kgSniperRifle, synPistol, kgSniperRifle.CompatibleAmmunition[0]));

                        List<List<List<TacticalItemDef>>> armors = new List<List<List<TacticalItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            armors.Add(new List<List<TacticalItemDef>>());
                        }

                        armors[0].Add(CreateArmorSets(neuSniperBandana, neuAssaultJacket, neuAssaultLegs));
                        armors[0].Add(CreateArmorSets(neuSniperBandana, neuSniperCoat, neuSniperLegs));
                        armors[1].Add(CreateArmorSets(inSniperHelmet, neuSniperCoat, sectarianLegs));
                        armors[1].Add(CreateArmorSets(inAssaultHelmet, neuAssaultJacket, neuAssaultLegs));
                        armors[2].Add(CreateArmorSets(spyMasterHelmet, inSniperTorso, spyMasterLegs));
                        armors[2].Add(CreateArmorSets(neuSniperBandana, spyMasterTorso, inSniperLegs));
                        armors[3].Add(CreateArmorSets(spyMasterHelmet, inAssaultTorso, inSniperLegs));
                        armors[3].Add(CreateArmorSets(inAssaultHelmet, inSniperTorso, spyMasterLegs));
                        armors[4].Add(CreateArmorSets(neuSniperBandana, inAssaultTorso, inAssaultLegs));
                        armors[4].Add(CreateArmorSets(inSniperHelmet, sectarianTorso, doomLegs));
                        armors[5].Add(CreateArmorSets(goldAssaultHelmet, _assaultGoldTorsoArmorsNaked[2], inHeavyLegs));
                        armors[5].Add(CreateArmorSets(goldHeavyHelmet, spyMasterTorso, doomLegs));

                        List<string> gUIDS = new List<string>()
                    {
                         "cebd9bfb-40d1-412a-9d18-d55b7c80cb45",
 "08eaac65-d3dd-4a8b-9950-ab4b237bfdbc",
 "845fa2ae-d57f-4eba-b3aa-56edd771c47d",
 "99a70b8e-f453-40d7-9ef2-0bdf22f73e57",
 "bdf88925-12e7-4865-99cf-78bc582d5a41",
 "c2e9fa5b-7421-4200-8e34-7b32a5b46e1c",
 "f849a2d4-0686-4885-be93-231ee35957d6",
 "d53b8218-09de-499e-8ebb-5f85dcb78aa9",
 "0b42905a-b621-4037-987d-c82480532ff5",
 "5e79f7a4-72fc-4938-bd15-949c7266918b",
 "1c08b8af-37f1-4061-b0fd-247ca5011c35",
 "34dc7ff6-de56-4cf8-a0a2-6ad325b9d1be",
 "4870a3eb-5559-47d6-99a4-b4ee991e053a",
 "7ad83720-70b8-43c9-ba95-2cac4b0c83df",
 "f0a309c1-9e62-4973-94cb-f76dc42b8924",
 "59f16bd8-7ea2-4f6d-91e0-9fa2cb74275b",
 "435e1162-965c-4aaf-8ea3-4bec20811530",
 "d76d6308-2a25-4dd8-9a69-d47028a2f5f9",


                    };

                        //consider tying something to venomous myrmidons, because known template
                        List<UnitTemplateResearchRewardDef> researches = new List<UnitTemplateResearchRewardDef>()
                    {
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_FishmanSniper_ResearchDef_UnitTemplateResearchRewardDef_0"),
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_BasicSwarmer_ResearchDef_UnitTemplateResearchRewardDef_0"),
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_VenomousSwarmer_ResearchDef_UnitTemplateResearchRewardDef_0"),
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_VenomousSwarmer_ResearchDef_UnitTemplateResearchRewardDef_0"),
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_AcidSwarmer_ResearchDef_UnitTemplateResearchRewardDef_0"),
                    DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_FishmanPiercerSniper_ResearchDef_UnitTemplateResearchRewardDef_0"),

                    };

                        CreateRaiderTemplateSet(armors, weapons, _sniperRaiderTag, gUIDS, researches);



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
                private static void CreateAssaultTemplates()
                {
                    try
                    {
                        ItemDef acidNade = DefCache.GetDef<ItemDef>("AN_AcidGrenade_WeaponDef");
                        ItemDef fireNade = DefCache.GetDef<ItemDef>("NJ_IncindieryGrenade_WeaponDef");

                        ItemDef neRifle = DefCache.GetDef<ItemDef>("NE_AssaultRifle_WeaponDef");

                        ItemDef pxRifle = DefCache.GetDef<ItemDef>("PX_AssaultRifle_Gold_WeaponDef");

                        ItemDef synRifle = DefCache.GetDef<ItemDef>("SY_LaserAssaultRifle_WeaponDef");
                        ItemDef njRifle = DefCache.GetDef<ItemDef>("NJ_Gauss_AssaultRifle_WeaponDef");

                        ItemDef kgRifle = DefCache.GetDef<ItemDef>("KS_Obliterator_WeaponDef");

                        ItemDef pxShotgun = DefCache.GetDef<ItemDef>("PX_ShotgunRifle_WeaponDef");
                        ItemDef anShotgun = DefCache.GetDef<ItemDef>("AN_Shotgun_WeaponDef");

                        ItemDef kgShotgun = DefCache.GetDef<ItemDef>("KS_Redemptor_WeaponDef");

                        List<List<List<ItemDef>>> weapons = new List<List<List<ItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            weapons.Add(new List<List<ItemDef>>());
                        }



                        weapons[0].Add(CreateWeaponSet(neRifle, neRifle.CompatibleAmmunition[0], neRifle.CompatibleAmmunition[0]));
                        weapons[0].Add(CreateWeaponSet(neRifle, neRifle.CompatibleAmmunition[0], neRifle.CompatibleAmmunition[0]));
                        weapons[0].Add(CreateWeaponSet(neRifle, neRifle.CompatibleAmmunition[0], neRifle.CompatibleAmmunition[0]));
                        weapons[1].Add(CreateWeaponSet(neRifle, neRifle.CompatibleAmmunition[0], neRifle.CompatibleAmmunition[0]));

                        weapons[1].Add(CreateWeaponSet(pxRifle, pxRifle.CompatibleAmmunition[0], pxRifle.CompatibleAmmunition[0]));

                        weapons[1].Add(CreateWeaponSet(neRifle, grenade, grenade));
                        weapons[2].Add(CreateWeaponSet(pxShotgun, pxShotgun.CompatibleAmmunition[0], pxShotgun.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(pxShotgun, pxShotgun.CompatibleAmmunition[0], pxShotgun.CompatibleAmmunition[0]));
                        weapons[2].Add(CreateWeaponSet(pxRifle, pxRifle.CompatibleAmmunition[0], pxRifle.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(anShotgun, anShotgun.CompatibleAmmunition[0], anShotgun.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(njRifle, njRifle.CompatibleAmmunition[0], njRifle.CompatibleAmmunition[0]));
                        weapons[3].Add(CreateWeaponSet(synRifle, synRifle.CompatibleAmmunition[0], synRifle.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(anShotgun, anShotgun.CompatibleAmmunition[0], anShotgun.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(njRifle, njRifle.CompatibleAmmunition[0], njRifle.CompatibleAmmunition[0]));
                        weapons[4].Add(CreateWeaponSet(neRifle, fireNade, fireNade));

                        weapons[5].Add(CreateWeaponSet(kgShotgun, kgShotgun.CompatibleAmmunition[0], kgShotgun.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(kgRifle, kgRifle.CompatibleAmmunition[0], kgRifle.CompatibleAmmunition[0]));
                        weapons[5].Add(CreateWeaponSet(synRifle, acidNade, acidNade));

                        List<List<List<TacticalItemDef>>> armors = new List<List<List<TacticalItemDef>>>();

                        for (int i = 0; i < 6; i++)
                        {
                            armors.Add(new List<List<TacticalItemDef>>());
                        }

                        armors[0].Add(CreateArmorSets(neuSniperBandana, neuAssaultJacket, neuAssaultLegs));
                        armors[0].Add(CreateArmorSets(inAssaultHelmet, neuSniperCoat, neuSniperLegs));
                        armors[0].Add(CreateArmorSets(neuSniperBandana, _assaultGoldTorsoArmorsNaked[2], neuAssaultLegs));
                        armors[1].Add(CreateArmorSets(neuSniperBandana, _assaultGoldTorsoArmorsNaked[1], inSniperLegs));
                        armors[1].Add(CreateArmorSets(inAssaultHelmet, _assaultGoldTorsoArmorsNaked[0], neuSniperLegs));
                        armors[1].Add(CreateArmorSets(inHeavyHelmet, neuHeavyTshirt, inAssaultLegs));
                        armors[2].Add(CreateArmorSets(neuSniperBandana, inAssaultTorso, inSniperLegs));
                        armors[2].Add(CreateArmorSets(sectarianHelmet, _assaultGoldTorsoArmorsLight[2], neuSniperLegs));
                        armors[2].Add(CreateArmorSets(doomHelmet, _assaultGoldTorsoArmorsLight[1], spyMasterLegs));
                        armors[3].Add(CreateArmorSets(goldAssaultHelmet, spyMasterTorso, doomLegs));
                        armors[3].Add(CreateArmorSets(sectarianHelmet, _assaultGoldTorsoArmorsLight[0], goldAssaultLegs));
                        armors[3].Add(CreateArmorSets(inAssaultHelmet, _doomLight[1], spyMasterLegs));
                        armors[4].Add(CreateArmorSets(neuSniperBandana, sectarianTorso, doomLegs));
                        armors[4].Add(CreateArmorSets(goldAssaultHelmet, inAssaultTorso, neuAssaultLegs));
                        armors[4].Add(CreateArmorSets(goldHeavyHelmet, neuHeavyTshirt, sectarianLegs));
                        armors[5].Add(CreateArmorSets(neuSniperBandana, _assaultGoldTorsoArmorsLight[0], doomLegs));
                        armors[5].Add(CreateArmorSets(doomHelmet, _assaultGoldTorsoArmorsLight[1], inAssaultLegs));
                        armors[5].Add(CreateArmorSets(goldAssaultHelmet, neuHeavyTshirt, spyMasterLegs));

                        List<string> gUIDS = new List<string>()
                    {
                    "c8520d8c-05c6-44c9-9536-170e8de4c610",
 "c04a9d78-a3c6-4198-a9e5-030812015a8b",
 "5bb51682-57c8-4e39-9a6a-ef60116516e4",
 "dfed0875-6e6a-4a32-8240-a1603e3d3d80",
 "32837b37-ea74-465f-9410-895ce617e8d9",
 "ab666e7f-c2c8-4007-9fc9-5c42e4c000da",
 "964a69b4-2246-4b36-8a2c-4898fc37f4ae",
 "f7c8b8dc-befc-40e9-b95e-d7012555c041",
 "01bf60db-062e-45fb-9b56-acc6e109d61e",
 "77bed076-e6b9-4cad-82fe-8e03d3d75b84",
 "e9fc43f7-edea-48b5-bffe-48298adf2c9e",
 "3341f6ad-2000-4e9e-8105-56f49bb51330",
 "582899a6-4477-42ee-9e00-2fc54e13f449",
 "1545e343-f347-4abc-aae0-5b9b31e21d9f",
 "81F317CF-CE09-4FF6-B329-77BF5C4BFA56",
 "D404DAB0-8C98-4B52-963A-AEBD576CAEA7",
 "{CE97BA86-75E2-421A-BA4F-09807381DE3C}",
 "{7EE0BDBA-25D5-4E6C-A32C-5F880E0CE272}",
                    };


                        CreateRaiderTemplateSet(armors, weapons, _assaultRaiderTag, gUIDS);



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void CreateRaiderTemplateSet(List<List<List<TacticalItemDef>>> armors, List<List<List<ItemDef>>> weapons, ClassTagDef classTagDef, List<string> gUIDS, List<UnitTemplateResearchRewardDef> researches = null)
                {
                    try
                    {
                        for (int x = 0; x < armors.Count; x++)
                        {
                            for (int y = 0; y < armors[x].Count; y++)
                            {
                                // Check if researches is not null before accessing researches[x]
                                UnitTemplateResearchRewardDef research = (researches != null && x < researches.Count) ? researches[x] : null;

                                CreateTacCharaterDef(classTagDef, $"BAN_{classTagDef.className}_{x}{y}", gUIDS[0], weapons[x][y], armors[x][y], null, null, x + 1, x, research);
                                gUIDS.Remove(gUIDS[0]);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void CreateBerserkerTemplates()
                {
                    try
                    {
                        WeaponDef nePistol = DefCache.GetDef<WeaponDef>("NE_Pistol_WeaponDef");
                        WeaponDef pxPistol = DefCache.GetDef<WeaponDef>("PX_Pistol_WeaponDef");
                        WeaponDef anuPistol = DefCache.GetDef<WeaponDef>("AN_HandCannon_WeaponDef");
                        WeaponDef njPistol = DefCache.GetDef<WeaponDef>("NJ_Gauss_HandGun_WeaponDef");
                        WeaponDef synPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                        WeaponDef mardukFist = DefCache.GetDef<WeaponDef>("AN_Hammer_WeaponDef");
                        WeaponDef blade = DefCache.GetDef<WeaponDef>("AN_Blade_WeaponDef");
                        WeaponDef acidNade = DefCache.GetDef<WeaponDef>("AN_AcidGrenade_WeaponDef");
                        WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");

                        List<ItemDef> berserkerWeapons0 = new List<ItemDef>() { sectarianAxe };//, nePistol, nePistol.CompatibleAmmunition[0] };

                        List<TacticalItemDef> berserkerArmor0 = new List<TacticalItemDef>() { inSniperHelmet, neuSniperCoat, neuHeavyLegs };
                        List<TacticalItemDef> berserkerArmor0b = new List<TacticalItemDef>() { sectarianHelmet, neuAssaultJacket, neuSniperLegs };
                        List<TacticalItemDef> berserkerArmor0c = new List<TacticalItemDef>() { spyMasterHelmet, neuAssaultJacket, neuAssaultLegs };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_0", "{C5A4C006-3FF9-47A3-B64A-B32A8781F0A7}", berserkerWeapons0, berserkerArmor0, null, null, 1, 0);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_0b", "{8CFD7DDC-EC70-4E9E-90E0-FD27FB156A20}", berserkerWeapons0, berserkerArmor0b, null, null, 1, 0);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_0c", "{66B25F71-0CDD-4466-BD57-85FA9684869C}", berserkerWeapons0, berserkerArmor0c, null, null, 1, 0);

                        List<ItemDef> bersekerWeaponsGrenade1 = new List<ItemDef>() { sectarianAxe, grenade, grenade };

                        List<TacticalItemDef> berserkerArmor1 = new List<TacticalItemDef>() { spyMasterHelmet, _assaultGoldTorsoArmorsNaked[2], neuSniperLegs };
                        List<TacticalItemDef> berserkerArmor1grenade = new List<TacticalItemDef>() { sectarianHelmet, neuHeavyTshirt, inAssaultLegs };
                        List<TacticalItemDef> berserkerArmor1grenadeb = new List<TacticalItemDef>() { inAssaultHelmet, neuHeavyTshirt, inAssaultLegs };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_1", "{5C08A5A2-4AA7-4334-A216-1C1A6F85541E}", berserkerWeapons0, berserkerArmor1, null, null, 2, 1);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_1g", "{89F01E89-D796-4084-884D-75E78F88159B}", bersekerWeaponsGrenade1, berserkerArmor1grenade, null, null, 2, 1);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_1gb", "{AD21E7B7-80C1-4B9F-AFF0-104B3ACFF0EE}", bersekerWeaponsGrenade1, berserkerArmor1grenadeb, null, null, 2, 1);

                        List<ItemDef> berserkerWeapons2 = new List<ItemDef>() { sectarianAxe, pxPistol, pxPistol.CompatibleAmmunition[0] };

                        List<TacticalItemDef> berserkerArmor2 = new List<TacticalItemDef>() { doomHelmet, _heavyGoldTorsoArmorsLight[2], sectarianLegs };
                        List<TacticalItemDef> berserkerArmor2b = new List<TacticalItemDef>() { inHeavyHelmet, _doomLight[2], neuAssaultLegs };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_2", "{2A77F7CB-9F49-44D9-B319-5A04C5FE2E24}", berserkerWeapons0, berserkerArmor2, null, null, 3, 2);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_2b", "{5BE72413-B99F-43A8-AFE5-C2CE31B0F17D}", berserkerWeapons2, berserkerArmor2b, null, null, 3, 2);


                        List<ItemDef> berserkerWeapons3 = new List<ItemDef>() { mardukFist, njPistol, njPistol.CompatibleAmmunition[0] };
                        List<ItemDef> berserkerWeapons3b = new List<ItemDef>() { mardukFist, synPistol, synPistol.CompatibleAmmunition[0] };
                        List<ItemDef> berserkerWeaponsGrenade3 = new List<ItemDef>() { mardukFist, fireNade, fireNade };


                        List<TacticalItemDef> berserkerArmor3 = new List<TacticalItemDef>() { doomHelmet, _heavyGoldTorsoArmorsLight[0], neuSniperCoat };
                        List<TacticalItemDef> berserkerArmor3b = new List<TacticalItemDef>() { inHeavyHelmet, _assaultGoldTorsoArmorsLight[1], neuAssaultLegs };
                        List<TacticalItemDef> berserkerArmor3c = new List<TacticalItemDef>() { doomHelmet, _heavyGoldTorsoArmorsLight[1], neuSniperCoat };
                        List<TacticalItemDef> berserkerArmor3grenade = new List<TacticalItemDef>() { doomHelmet, neuHeavyTshirt, inHeavyLegs };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_3", "{DC9A0366-6238-4EFA-B61C-BB41A62E418C}", berserkerWeapons3, berserkerArmor3, null, null, 4, 3);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_3b", "{967BFAF4-88E4-4A6C-A05D-D78B58A157D7}", berserkerWeapons3b, berserkerArmor3b, null, null, 4, 3);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_3g", "{8CFADF47-FFBB-432D-BCB8-9E40DE88EB1C}", berserkerWeaponsGrenade3, berserkerArmor3grenade, null, null, 4, 3);

                        List<TacticalItemDef> berserkerArmor4 = new List<TacticalItemDef>() { doomHelmet, _heavyGoldTorsoArmorsMedium[2], inAssaultLegs };
                        List<TacticalItemDef> berserkerArmor4b = new List<TacticalItemDef>() { inHeavyHelmet, _doomMedium[2], neuAssaultLegs };
                        List<TacticalItemDef> berserkerArmor4c = new List<TacticalItemDef>() { doomHelmet, _assaultGoldTorsoArmorsMedium[2], inAssaultLegs };

                        List<ItemDef> berserkerWeapons4 = new List<ItemDef>() { blade, anuPistol, anuPistol.CompatibleAmmunition[0] };
                        List<ItemDef> berserkerWeapons4b = new List<ItemDef>() { blade, njPistol, njPistol.CompatibleAmmunition[0] };
                        List<ItemDef> berserkerWeapons4c = new List<ItemDef>() { sectarianAxe, synPistol, synPistol.CompatibleAmmunition[0] };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_4", "{40FF7508-01A4-4EC5-BD46-01FB93698E85}", berserkerWeapons4, berserkerArmor4, null, null, 5, 4);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_4b", "{D4874963-9758-4F89-877A-66070179EEAF}", berserkerWeapons4b, berserkerArmor4b, null, null, 5, 4);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_4c", "{C82A4D96-9346-4B5F-A378-8FBB97D932B8}", berserkerWeapons4c, berserkerArmor4c, null, null, 5, 4);


                        List<ItemDef> berserkerWeaponsGrenade5 = new List<ItemDef>() { mardukFist, acidNade, acidNade };
                        List<ItemDef> berserkerWeapons5 = new List<ItemDef>() { blade, _tormentor, _tormentor.CompatibleAmmunition[0] };
                        List<ItemDef> berserkerWeapons5b = new List<ItemDef>() { mardukFist, _tormentor, _tormentor.CompatibleAmmunition[0] };


                        List<TacticalItemDef> berserkerArmor5 = new List<TacticalItemDef>() { inHeavyHelmet, _heavyGoldTorsoArmorsMedium[0], inAssaultLegs };
                        List<TacticalItemDef> berserkerArmor5b = new List<TacticalItemDef>() { inHeavyHelmet, _heavyGoldTorsoArmorsMedium[1], inAssaultLegs };
                        List<TacticalItemDef> berserkerArmor5g = new List<TacticalItemDef>() { doomHelmet, neuHeavyTshirt, neuAssaultLegs };

                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_5", "{DFE295E7-9D07-4BD4-8FEE-8A5508C9C971}", berserkerWeapons5, berserkerArmor5, null, null, 6, 4);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_5b", "{4148377B-E788-4F75-934C-9D20785AD7FB}", berserkerWeapons5b, berserkerArmor5b, null, null, 6, 4);
                        CreateTacCharaterDef(_scumTag, "BAN_Berserker_5g", "{170CB4D6-0759-47AD-9285-0E06CC097FA5}", berserkerWeaponsGrenade5, berserkerArmor5g, null, null, 6, 4);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static void CreateTemplates()
                {
                    try
                    {
                        CreateBerserkerTemplates();
                        CreateAssaultTemplates();
                        CreateHeavyTemplates();
                        CreateSniperTemplates();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static TacCharacterDef CreateTacCharaterDef(ClassTagDef classTagDef, string name, string gUID,
              List<ItemDef> equipmentSlots, List<TacticalItemDef> armorSlots, List<TacticalItemDef> inventorySlots, List<GameTagDef> tags, int level, int willStat, UnitTemplateResearchRewardDef unitTemplateResearchRewardDef = null)
                {
                    try
                    {
                        //  GeoUnitDescriptor

                        TacCharacterDef characterSource = DefCache.GetDef<TacCharacterDef>($"BAN_Assault{level}_CharacterTemplateDef");
                        TacCharacterDef newCharacter = Helper.CreateDefFromClone(characterSource, gUID, name);

                        newCharacter.SpawnCommandId = name;
                        newCharacter.Data.Name = name;
                        newCharacter.Data.GameTags = tags != null ? new List<GameTagDef>(tags) { classTagDef }.ToArray() : new List<GameTagDef>() { classTagDef }.ToArray();
                        newCharacter.Data.EquipmentItems = equipmentSlots?.ToArray() ?? new ItemDef[] { };
                        newCharacter.Data.BodypartItems = armorSlots?.ToArray() ?? new ItemDef[] { };
                        newCharacter.Data.InventoryItems = inventorySlots?.ToArray() ?? new ItemDef[] { };

                        // newCharacter.Data.LevelProgression.SetLevel(level);

                        if (willStat != 0)
                        {
                            newCharacter.Data.Will = willStat;
                        }

                        float deploymentCost = level * 20;

                        if (classTagDef == _assaultRaiderTag)
                        {
                            deploymentCost += 70;
                        }
                        else if (classTagDef == _scumTag)
                        {
                            deploymentCost += 70;
                        }
                        else if (classTagDef == _heavyRaiderTag)
                        {
                            deploymentCost += 130;
                        }
                        else if (classTagDef == _sniperRaiderTag)
                        {
                            deploymentCost += 180;
                        }

                        newCharacter.DeploymentCost = (int)deploymentCost;

                        if (unitTemplateResearchRewardDef == null)
                        {
                            GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");
                            neutralFaction.StartingUnits = neutralFaction.StartingUnits.AddToArray(newCharacter);
                        }
                        else
                        {
                            if (_banditTemplatesTiedToPandoranUnlocks.ContainsKey(unitTemplateResearchRewardDef))
                            {
                                _banditTemplatesTiedToPandoranUnlocks[unitTemplateResearchRewardDef].Add(newCharacter);
                            }
                            else
                            {
                                _banditTemplatesTiedToPandoranUnlocks.Add(unitTemplateResearchRewardDef, new List<TacCharacterDef>() { newCharacter });
                            }
                        }
                        // TFTVLogger.Always($"{newCharacter.name} is now in the neutral faction, deployment cost is {newCharacter.DeploymentCost}");

                        return newCharacter;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


            }

            internal class ScavengerSpecializations 
            {

                internal static void CreateRaiderSpecs()
                {
                    try
                    {
                        CreateRaiderAssaultSpec();
                        CreateRaiderHeavySpec();
                        CreateScumSpec();
                        CreateRaiderSniperSpec();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateRaiderAssaultSpec()
                {
                    try
                    {
                        string name = "AssaultRaider";
                        _assaultRaiderTag = Helper.CreateDefFromClone(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"), "{C64FEF5F-351A-4D46-B129-3FDDF6DD0050}", $"{name}_ClassTagDef");
                        SpecializationDef specializationDefSource = DefCache.GetDef<SpecializationDef>("AssaultSpecializationDef");
                        SpecializationDef newSpec = Helper.CreateDefFromClone(specializationDefSource, "{3C44BE9A-6A22-4651-AD77-C6A1186E4D41}", name);
                        newSpec.ViewElementDef = Helper.CreateDefFromClone(specializationDefSource.ViewElementDef, "{CDDDC201-F141-4A26-A542-DD7C06507033}", $"{newSpec.name}");
                        newSpec.AbilityTrack = Helper.CreateDefFromClone(specializationDefSource.AbilityTrack, "{0875C025-2FAF-4E1D-B843-AA5A6965226F}", $"{newSpec.name}");

                        /*  for (int x = 1; x < newSpec.AbilityTrack.AbilitiesByLevel.Count(); x++)
                          {
                              newSpec.AbilityTrack.AbilitiesByLevel = new PhoenixPoint.Common.Entities.Characters.AbilityTrackSlot[]
                          {
                              new PhoenixPoint.Common.Entities.Characters.AbilityTrackSlot()
                              {Ability = specializationDefSource.AbilityTrack.AbilitiesByLevel[x].Ability, RequiresPrevAbility = false }

                          };

                          }*/



                        newSpec.ClassTag = _assaultRaiderTag;



                        _assaultRaiderSpecialization = newSpec;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateRaiderHeavySpec()
                {
                    try
                    {
                        string name = "HeavyRaider";
                        _heavyRaiderTag = Helper.CreateDefFromClone(DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"), "{8BB1BAA6-A69D-4ADE-9221-699F7CC2D0B9}", $"{name}_ClassTagDef");
                        SpecializationDef specializationDefSource = DefCache.GetDef<SpecializationDef>("HeavySpecializationDef");
                        SpecializationDef newSpec = Helper.CreateDefFromClone(specializationDefSource, "{F1F3A424-F443-46EE-AA79-EEB083F4482C}", name);
                        newSpec.ViewElementDef = Helper.CreateDefFromClone(specializationDefSource.ViewElementDef, "{48957D5C-CE49-4648-9A2A-22F571345ABD}", $"{newSpec.name}");
                        newSpec.AbilityTrack = Helper.CreateDefFromClone(specializationDefSource.AbilityTrack, "{476C1D3F-AC3F-46A5-BA05-48536692DAEE}", $"{newSpec.name}");
                        newSpec.ClassTag = _heavyRaiderTag;
                        _heavyRaiderSpecialization = newSpec;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void CreateRaiderSniperSpec()
                {
                    try
                    {
                        string name = "SniperRaider";
                        _sniperRaiderTag = Helper.CreateDefFromClone(DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef"), "{EF2A159C-E069-4770-BFD3-3DD0B6538299}", $"{name}_ClassTagDef");
                        SpecializationDef specializationDefSource = DefCache.GetDef<SpecializationDef>("SniperSpecializationDef");
                        SpecializationDef newSpec = Helper.CreateDefFromClone(specializationDefSource, "{11B394F0-CD4F-42B4-8154-5F52865D24C9}", name);
                        newSpec.ViewElementDef = Helper.CreateDefFromClone(specializationDefSource.ViewElementDef, "{9A4D95F4-A239-4035-931C-5C12B95BD2FC}", $"{newSpec.name}");
                        newSpec.AbilityTrack = Helper.CreateDefFromClone(specializationDefSource.AbilityTrack, "{516ACA79-A557-482F-9323-E58AC55AAC9C}", $"{newSpec.name}");
                        newSpec.ClassTag = _sniperRaiderTag;

                        _sniperRaiderSpecialization = newSpec;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private static void CreateScumSpec()
                {
                    try
                    {
                        _scumTag = Helper.CreateDefFromClone(DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef"), "{E85482C5-36C2-463E-8DC2-731542E0A154}", $"Scum_ClassTagDef");
                        SpecializationDef specializationDefSource = DefCache.GetDef<SpecializationDef>("BerserkerSpecializationDef");
                        SpecializationDef newSpec = Helper.CreateDefFromClone(specializationDefSource, "{BB8E9CD7-8D9D-485C-9A4E-5088C78D14AB}", $"ScumSpecializationDef");
                        newSpec.ViewElementDef = Helper.CreateDefFromClone(specializationDefSource.ViewElementDef, "{D061277C-28D2-4BC8-A79A-A321B00EB3F8}", $"{newSpec.name}");
                        newSpec.AbilityTrack = Helper.CreateDefFromClone(specializationDefSource.AbilityTrack, "{07E7B7F0-9381-4A0D-9F7E-E627DDD6FC33}", $"{newSpec.name}");

                        ClassProficiencyAbilityDef scumProficiency = Helper.CreateDefFromClone(DefCache.GetDef<ClassProficiencyAbilityDef>("Berserker_ClassProficiency_AbilityDef"),
                            "{0FCA4097-E538-4B77-ADF9-38855A165C26}", "ScumProficiency");

                        scumProficiency.ViewElementDef = Helper.CreateDefFromClone(DefCache.GetDef<ClassProficiencyAbilityDef>("Berserker_ClassProficiency_AbilityDef").ViewElementDef,
                            "{B29DD65F-7402-43C2-90DD-DFCC34737BB0}", "ScumProficiency");

                        Sprite icon = DefCache.GetDef<GeoFactionViewDef>("E_NEU_Bandits [NEU_Bandits_GeoSubFactionDef]").FactionIcon;

                        scumProficiency.ClassTags = new GameTagsList() { _scumTag, scumProficiency.ClassTags[1], scumProficiency.ClassTags[2] };
                        scumProficiency.ViewElementDef.SmallIcon = icon;
                        scumProficiency.ViewElementDef.LargeIcon = icon;
                        scumProficiency.ViewElementDef.DisplayName1.LocalizationKey = "KEY_WASTELAND_SCUM_NAME";
                        scumProficiency.ViewElementDef.DisplayName2.LocalizationKey = "KEY_WASTELAND_SCUM_NAME";
                        scumProficiency.ViewElementDef.Description.LocalizationKey = "KEY_BERSERKER_TRAINING_DESCRIPTION";

                        AbilityTrackSlot scumTrackSlot0 = new AbilityTrackSlot()
                        { Ability = scumProficiency, RequiresPrevAbility = false };

                        newSpec.AbilityTrack.AbilitiesByLevel[0] = scumTrackSlot0;

                        /*   for(int x =1; x< newSpec.AbilityTrack.AbilitiesByLevel.Count(); x++) 
                           {
                               newSpec.AbilityTrack.AbilitiesByLevel = new PhoenixPoint.Common.Entities.Characters.AbilityTrackSlot[]
                           {
                               new PhoenixPoint.Common.Entities.Characters.AbilityTrackSlot()
                               {Ability = specializationDefSource.AbilityTrack.AbilitiesByLevel[x].Ability, RequiresPrevAbility = false }

                           };

                           }*/


                        newSpec.ClassTag = _scumTag;
                        newSpec.IsDominantSpecialization = true;
                        newSpec.ViewElementDef.SmallIcon = icon;
                        newSpec.ViewElementDef.DisplayName1 = new Base.UI.LocalizedTextBind("testing displayName1", true);
                        newSpec.ViewElementDef.DisplayName2 = new Base.UI.LocalizedTextBind("testing displayName2", true);
                        newSpec.ViewElementDef.Description = new Base.UI.LocalizedTextBind("testing description", true);

                        _scumSpecialization = newSpec;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }






            }

            public static void CreateRaiderDefs()
            {
                try
                {
                    ScavengerSpecializations.CreateRaiderSpecs();
                    Armors.ArmorDefs();
                    AdjustGeoFaction();
                    Templates.CreateTemplates();
                    AdjustBanditMissions();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustGeoFaction()
            {
                try
                {
                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");

                    neutralFaction.InitialSpecializationDefs.Add(_scumSpecialization);
                    neutralFaction.InitialSpecializationDefs.Add(_assaultRaiderSpecialization);
                    neutralFaction.InitialSpecializationDefs.Add(_heavyRaiderSpecialization);
                    neutralFaction.InitialSpecializationDefs.Add(_sniperRaiderSpecialization);

                    neutralFaction.Units = neutralFaction.Units.AddRangeToArray(new GeoFactionDef.CharacterSpawnData[]
                    {
                        new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = _scumTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 3.0f
                        },
                         new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = _infiltratorTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 1.0f
                        },
                         new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = _assaultRaiderTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 3.0f
                        },
                         new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = _heavyRaiderTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 1.0f
                        },
                          new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = _sniperRaiderTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 1.0f
                        },
                    });
                    // neutralFaction.StartingUnits = new TacCharacterDef[] { };
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustBanditMissions()
            {
                try


                {

                    /*
                     * [TFTV @ 1/11/2024 12:36:11 PM] StoryAN1_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StoryLE1_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StoryNJ_Chain1_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StoryPX13_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StorySYN0_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StorySYN4_CustomMissionTypeDef has bandits in it, and they are participant 0? False
[TFTV @ 1/11/2024 12:36:11 PM] StorySYN5_CustomMissionTypeDef has bandits in it, and they are participant 0? False



[TFTV @ 1/11/2024 12:36:11 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
[TFTV @ 1/11/2024 12:36:11 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
[TFTV @ 1/11/2024 12:36:11 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
[TFTV @ 1/11/2024 12:36:11 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
[TFTV @ 1/11/2024 12:36:11 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
[TFTV @ 1/11/2024 12:36:11 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
                     * 
                     * 
                     * [TFTV @ 1/11/2024 12:56:00 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
d903cff7-c81d-32d4-b9da-bdd6dada4692
[TFTV @ 1/11/2024 12:56:00 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
4a45fa1b-314f-5034-9b77-28af6e188d01
[TFTV @ 1/11/2024 12:56:00 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
ee089de4-2262-1bf4-5824-7209bbd323ce
[TFTV @ 1/11/2024 12:56:00 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
889be02f-f83e-3d44-594c-7e8382982990
[TFTV @ 1/11/2024 12:56:00 PM] OScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
53ad686e-30cd-4154-683f-e7d620020dd8
[TFTV @ 1/11/2024 12:56:00 PM] ScavVRescueBAN_CustomMissionTypeDef has bandits in it, and they are participant 0? True
ab2858c7-fb8a-4dd4-aa6c-378f6323422d
                     * 
                     * 
                     * 
                     * 
                     */
                    TacCharacterDef BuggyCharacterDef = (TacCharacterDef)Repo.GetDef("147c1dfa-411a-4114-4b3c-5a2e1cfcd1d2");


                    List<CustomMissionTypeDef> banditStoryMissions = new List<CustomMissionTypeDef>()
                    {
                        DefCache.GetDef<CustomMissionTypeDef>("StoryAN1_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("StoryNJ_Chain1_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("StoryPX13_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("StorySYN4_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("StorySYN5_CustomMissionTypeDef"),
                    };

                    foreach (CustomMissionTypeDef customMissionTypeDef in banditStoryMissions)
                    {
                      //  customMissionTypeDef.ParticipantsData[1].DeploymentRule.DeploymentPoints = 500;
                        customMissionTypeDef.ParticipantsData[1].ActorDeployParams = new List<MissionDeployParams>() {
                     new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _assaultRaiderTag,
                        SpawnedCount = 0
                    },
                    Weight = 80
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _scumTag,
                        SpawnedCount = 0
                    },
                    Conditions = new List<PhoenixPoint.Geoscape.Entities.Requirement.MissionRequirementDef>(),
                    Weight = 100
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag =_heavyRaiderTag,
                SpawnedCount = 0
                    },
                    Weight = 50
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _sniperRaiderTag,
                        SpawnedCount = 0
                    },
                    Weight = 50
                }


                        };

                      /*  customMissionTypeDef.ParticipantsData[1].UniqueUnits = new TacMissionTypeParticipantData.UniqueChatarcterBind[]
                        {
                            new TacMissionTypeParticipantData.UniqueChatarcterBind
                            {
                            Amount=new RangeDataInt(){Max=1, Min=1},
                            Character = BuggyCharacterDef,
                            Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef")
                            }

                        };*/
                    };




                    List<CustomMissionTypeDef> banditAmbushAndScavengingMissions = new List<CustomMissionTypeDef>()
                    {
                        (CustomMissionTypeDef)Repo.GetDef("d903cff7-c81d-32d4-b9da-bdd6dada4692"),
                        (CustomMissionTypeDef)Repo.GetDef("4a45fa1b-314f-5034-9b77-28af6e188d01"),
                        (CustomMissionTypeDef)Repo.GetDef("ee089de4-2262-1bf4-5824-7209bbd323ce"),
                        (CustomMissionTypeDef)Repo.GetDef("889be02f-f83e-3d44-594c-7e8382982990"),
                        (CustomMissionTypeDef)Repo.GetDef("53ad686e-30cd-4154-683f-e7d620020dd8"),
                        (CustomMissionTypeDef)Repo.GetDef("ab2858c7-fb8a-4dd4-aa6c-378f6323422d"),
                        DefCache.GetDef<CustomMissionTypeDef>("AmbushBandits_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("OScavCratesBAN_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("ScavCratesBAN_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("OScavRescueBAN_CustomMissionTypeDef"),
                        DefCache.GetDef<CustomMissionTypeDef>("ScavRescueBAN_CustomMissionTypeDef"),
                    };

                    foreach (CustomMissionTypeDef missionTypeDef in banditAmbushAndScavengingMissions)
                    {
                        missionTypeDef.ParticipantsData[0].ActorDeployParams = new List<MissionDeployParams>() {
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _assaultRaiderTag,//DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"),
                        SpawnedCount = 0
                    },
                   // Conditions = new List<PhoenixPoint.Geoscape.Entities.Requirement.MissionRequirementDef>(),
                    Weight = 80
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _scumTag,//DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef"),
                        SpawnedCount = 0
                    },
                    Conditions = new List<PhoenixPoint.Geoscape.Entities.Requirement.MissionRequirementDef>(),
                    Weight = 100
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag =_heavyRaiderTag, //DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"),
                SpawnedCount = 0
                    },
                 //   Conditions = new List<PhoenixPoint.Geoscape.Entities.Requirement.MissionRequirementDef>(),
                    Weight = 50
                },
                new MissionDeployParams()
                {
                    Limit = new ActorDeployLimit()
                    {
                        ActorLimit = new RangeDataInt(0, 1000),
                        ActorTag = _sniperRaiderTag, //DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef"),
                        SpawnedCount = 0
                    },
                  //  Conditions = new List<PhoenixPoint.Geoscape.Entities.Requirement.MissionRequirementDef>(),
                    Weight = 50
                }

                };



                    }

                    TacCharacterDef sy0Infiltrator = DefCache.GetDef<TacCharacterDef>("S_IN_SuperThief_TacCharacterDef");
                    TacCharacterDef spyMaster = DefCache.GetDef<TacCharacterDef>("Mercenary_Spymaster");

                    sy0Infiltrator.Data.BodypartItems = spyMaster.Data.BodypartItems;

                    CustomMissionTypeDef synIntro = DefCache.GetDef<CustomMissionTypeDef>("StorySYN0_CustomMissionTypeDef");

                    TacMissionTypeParticipantData.UniqueChatarcterBind assaultBandit1 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Character = DefCache.GetDef<TacCharacterDef>($"BAN_AssaultRaider_00")
                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind assaultBandit2 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef"),
                        Character = DefCache.GetDef<TacCharacterDef>($"BAN_AssaultRaider_01")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind scummer1 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef"),
                        Character = DefCache.GetDef<TacCharacterDef>($"BAN_Berserker_0")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind scummer2 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef"),
                        Character = DefCache.GetDef<TacCharacterDef>($"BAN_Berserker_0b")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind sniper1 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef"),
                        Character = DefCache.GetDef<TacCharacterDef>($"BAN_SniperRaider_00")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind thief1 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Character = DefCache.GetDef<TacCharacterDef>("S_IN_Thief_TacCharacterDef")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind thief2 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Character = DefCache.GetDef<TacCharacterDef>("S_IN_Thief_TacCharacterDef")

                    };

                    TacMissionTypeParticipantData.UniqueChatarcterBind spyMaster1 = new TacMissionTypeParticipantData.UniqueChatarcterBind()
                    {
                        Amount = new RangeDataInt() { Max = 1, Min = 1 },
                        Difficulty = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef"),
                        Character = sy0Infiltrator

                    };


                    synIntro.ParticipantsData[1].UniqueUnits = new TacMissionTypeParticipantData.UniqueChatarcterBind[]
                    {
                        assaultBandit1, assaultBandit2, scummer1, scummer2, thief1, thief2, sniper1, spyMaster1
                    };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static TacticalItemDef CreateArmor(TacticalItemDef baseArmorPiece, string gUID, TacticalItemDef pieceToAdd, int position)
        {
            try
            {
                TacticalItemDef newPiece = Helper.CreateDefFromClone(baseArmorPiece, gUID, baseArmorPiece.name + pieceToAdd.name);
                newPiece.SubAddons[position] = new AddonDef.SubaddonBind() { SubAddon = pieceToAdd };
                return newPiece;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
    }
}
