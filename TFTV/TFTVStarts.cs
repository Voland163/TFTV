using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVStarts
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ModifyIntroForSpecialStart(GeoFaction geoFaction, GeoSite site)
        {
            try
            {
                GeoscapeEventDef intro = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("IntroBetterGeo_0"));
                intro.GeoscapeEventData.Description[0].General = new LocalizedTextBind("After all these years, you finally got the call. It meant that Symes and his deputies were dead or unreachable. Phoenix Project had gone dark. " +
                    "You spent many years waiting, dreading for it to happen, leading a discrete existence at " + FindNearestHaven(geoFaction, site) + ", a " + geoFaction.Name.LocalizeEnglish()
                    + " haven.\n\nThe trek to Phoenix Point was long and dangerous, and you wouldn't have made it without " + geoFaction.GeoLevel.PhoenixFaction.Vehicles.First().Soldiers.Last().DisplayName + ", a " + geoFaction.Name.LocalizeEnglish()
                    + " " + geoFaction.GeoLevel.PhoenixFaction.Vehicles.First().Soldiers.Last().ClassTag.className + ".\n\nBut when you reached Phoenix Point, somebody was already home...", true);

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static string FindNearestHaven(GeoFaction geoFaction, GeoSite phoenixPoint)
        {
            try
            {


                IOrderedEnumerable<GeoSite> orderedEnumerable = from s in geoFaction.GeoLevel.Map.GetConnectedSitesOfType_Land(phoenixPoint, GeoSiteType.Haven, activeOnly: false)
                                                                orderby GeoMap.Distance(phoenixPoint, s)
                                                                select s;
                foreach (GeoSite geoHaven in orderedEnumerable)
                {
                    if (geoHaven.Owner == geoFaction)
                    {
                        geoHaven.RevealSite(geoFaction.GeoLevel.PhoenixFaction);
                        return geoHaven.Name;
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        //Adapted from MadSkunky's TutorialTweaks: https://github.com/MadSkunky/PP-Mods-TutorialTweaks
        public static List<TacCharacterDef> SetInitialSquadUnbuffed(GeoLevelController level)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
              
                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>();

                GameDifficultyLevelDef hardDifficultyLevel = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Hard_GameDifficultyLevelDef"));
                GameDifficultyLevelDef standardDifficultyLevel = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Standard_GameDifficultyLevelDef"));

                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Jacob_Tutorial2_TacCharacterDef"));
                TacCharacterDef Sophia2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Sophia_Tutorial2_TacCharacterDef"));

                TacCharacterDef assault = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_AssaultStarting_TacCharacterDef"));
                TacCharacterDef heavy = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_HeavyStarting_TacCharacterDef"));
                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));

                TacCharacterDef newJacob = Helper.CreateDefFromClone(Jacob2, "DDA13436-40BE-4096-9C69-19A3BF6658E6", "PX_Jacob_TFTV_TacCharacterDef");
                TacCharacterDef newSophia = Helper.CreateDefFromClone(Sophia2, "D9EC7144-6EB5-451C-9015-3E67F194AB1B", "PX_Sophia_TFTV_TacCharacterDef");
                
                newJacob.Data.ViewElementDef = Repo.GetAllDefs<ViewElementDef>().First(ved => ved.name.Equals("E_View [PX_Sniper_ActorViewDef]"));
                GameTagDef Sniper_CTD = Repo.GetAllDefs<GameTagDef>().First(gtd => gtd.name.Equals("Sniper_ClassTagDef"));
                for (int i = 0; i < newJacob.Data.GameTags.Length; i++)
                {
                    if (newJacob.Data.GameTags[i].GetType() == Sniper_CTD.GetType())
                    {
                        newJacob.Data.GameTags[i] = Sniper_CTD;
                    }
                }

                // Creating new arrays for Abilities, BodypartItems (armor), EquipmentItems (ready slots) and InventoryItems (backpack)
                // -> Overwrite old sets completely
                newJacob.Data.Abilites = new TacticalAbilityDef[] // abilities -> Class proficiency
                {
                Repo.GetAllDefs<ClassProficiencyAbilityDef>().First(cpad => cpad.name.Equals("Sniper_ClassProficiency_AbilityDef"))

                };
                newJacob.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Helmet_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Legs_ItemDef"))
                };


                newJacob.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_SniperRifle_WeaponDef")),
                    Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_Pistol_WeaponDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("Medkit_EquipmentDef"))
                };
                newJacob.Data.InventoryItems = new ItemDef[] // Backpack
                {
                newJacob.Data.EquipmentItems[0].CompatibleAmmunition[0],
                newJacob.Data.EquipmentItems[1].CompatibleAmmunition[0]
                };
               
                newJacob.Data.Strength = 0;
                newJacob.Data.Will = 0;
                newJacob.Data.Speed = 0;
                newJacob.Data.CurrentHealth = -1;

                newSophia.Data.Strength = 0;
                newSophia.Data.Will = 0;
                newSophia.Data.Speed = 0;
                newSophia.Data.CurrentHealth = -1;

                startingTemplates.Add(newSophia);
                startingTemplates.Add(newJacob);

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }

                int strengthBonus = 0;
                int willBonus = 0;

                if (level.CurrentDifficultyLevel.Order == 3)
                {
                    strengthBonus = 4;
                    willBonus = 1;
                }
                else if (level.CurrentDifficultyLevel.Order == 2)
                {
                    startingTemplates.Add(assault);
                    strengthBonus = 6;
                    willBonus = 2;
                }
                else if (level.CurrentDifficultyLevel.Order == 1)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                    strengthBonus = 8;
                    willBonus = 3;
                }

                newJacob.Data.Strength = strengthBonus;
                newJacob.Data.Will = willBonus;

                newSophia.Data.Strength = strengthBonus;
                newSophia.Data.Will = willBonus;

   
                return startingTemplates;


                /*
                TacCharacterDef Omar2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Omar_Tutorial2_TacCharacterDef"));
                Omar2.Data.Strength = 0;
                Omar2.Data.Will = 0;
                Omar2.Data.Speed = 0;
                Omar2.Data.CurrentHealth = -1;

                TacCharacterDef Takeshi3 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Takeshi_Tutorial3_TacCharacterDef"));
                Takeshi3.Data.Strength = 0;
                Takeshi3.Data.Will = 0;
                Takeshi3.Data.Speed = 0;
                Takeshi3.Data.CurrentHealth = -1;

                TacCharacterDef Irina3 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Irina_Tutorial3_TacCharacterDef"));
                Irina3.Data.Strength = 0;
                Irina3.Data.Will = 0;
                Irina3.Data.Speed = 0;
                Irina3.Data.CurrentHealth = -1;*/



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static List<TacCharacterDef> SetInitialSquadBuffed(GameDifficultyLevelDef difficultyLevel, GeoLevelController level)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                GameDifficultyLevelDef hardDifficultyLevel = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Hard_GameDifficultyLevelDef"));

                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>();
                startingTemplates.AddRange(hardDifficultyLevel.TutorialStartingSquadTemplate);

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }

                if (difficultyLevel.Order == 1)
                {
                    startingTemplates.Add(sniper);
                }

                return startingTemplates;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static List<TacCharacterDef> SetInitialSquadRandom(GameDifficultyLevelDef difficultyLevel, GeoLevelController level)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                TacCharacterDef assault = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_AssaultStarting_TacCharacterDef"));
                TacCharacterDef heavy = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_HeavyStarting_TacCharacterDef"));
                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>();
                startingTemplates.Add(heavy);
                startingTemplates.Add(assault);
                startingTemplates.Add(assault);
                startingTemplates.Add(sniper);

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }


                if (difficultyLevel.Order == 2)
                {
                    startingTemplates.Add(assault);
                }
                else if (difficultyLevel.Order == 1)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }

                return startingTemplates;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }







      
/*
        public static void MakeJacobSniper()
        {
            try
            {
                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_Tutorial2_TacCharacterDef"));
                

                // Get Jacobs definition for the 1st part of the tutorial
                TacCharacterDef Jacob1 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_Tutorial_TacCharacterDef"));
                // Set class related definition for actor view
                
                // Switch the given Assault ClassTagDef in Jacobs GameTags to Sniper (keep both ClassTagDefs would make him dual classed rigth from scratch)
                
                
                
                
                GameTagDef Sniper_CTD = Repo.GetAllDefs<GameTagDef>().First(gtd => gtd.name.Equals("Sniper_ClassTagDef"));
                for (int i = 0; i < Jacob1.Data.GameTags.Length; i++)
                {
                    if (Jacob1.Data.GameTags[i].GetType() == Sniper_CTD.GetType())
                    {
                        Jacob1.Data.GameTags[i] = Sniper_CTD;
                    }
                }

                // Creating new arrays for Abilities, BodypartItems (armor), EquipmentItems (ready slots) and InventoryItems (backpack)
                // -> Overwrite old sets completely
                Jacob1.Data.Abilites = new TacticalAbilityDef[] // abilities -> Class proficiency
                {
                Repo.GetAllDefs<ClassProficiencyAbilityDef>().First(cpad => cpad.name.Equals("Sniper_ClassProficiency_AbilityDef"))

                };
                Jacob1.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Helmet_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("PX_Sniper_Legs_ItemDef"))
                };


                Jacob1.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_SniperRifle_WeaponDef")),
                    Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Equals("PX_Pistol_WeaponDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Equals("Medkit_EquipmentDef"))
                };
                Jacob1.Data.InventoryItems = new ItemDef[] // Backpack
                {
                Jacob1.Data.EquipmentItems[0].CompatibleAmmunition[0],
                Jacob1.Data.EquipmentItems[1].CompatibleAmmunition[0]
                };
                // Get Jacobs definition for the 2nd and following parts of the tutorial
                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_Tutorial2_TacCharacterDef"));
                // Copy changes from Jabobs 1st to his 2nd definition
                Jacob2.Data.ViewElementDef = Jacob1.Data.ViewElementDef;
                Jacob2.Data.GameTags = Jacob1.Data.GameTags;
                Jacob2.Data.Abilites = Jacob1.Data.Abilites;
                Jacob2.Data.BodypartItems = Jacob1.Data.BodypartItems;
                Jacob2.Data.EquipmentItems = Jacob1.Data.EquipmentItems;
                Jacob2.Data.InventoryItems = Jacob1.Data.InventoryItems;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
*/

        public static void AdjustStatsDifficulty(GameDifficultyLevelDef gameDifficultyLevel)
        {
            try
            {
               
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


       

        public static void CreateInitialInfiltrator()
        {
            try
            {
                TacCharacterDef sourceInfiltrator = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("S_SY_Infiltrator_TacCharacterDef"));
                TacCharacterDef startingInfiltrator = Helper.CreateDefFromClone(sourceInfiltrator, "8835621B-CFCA-41EF-B480-241D506BD742", "PX_Starting_Infiltrator_TacCharacterDef");
                startingInfiltrator.Data.Strength = 0;
                startingInfiltrator.Data.Will = 0;

                startingInfiltrator.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Helmet_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("SY_Infiltrator_Bonus_Legs_ItemDef"))
                };

                /*
                startingInfiltrator.Data.EquipmentItems = new ItemDef[] // Ready slots
                                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("SY_Crossbow_Bonus_WeaponDef")),
                                    Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("SY_SpiderDroneLauncher_WeaponDef")),
                                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("Medkit_EquipmentDef"))
                                };
                startingInfiltrator.Data.InventoryItems = new ItemDef[] // Backpack
                                {
                                startingInfiltrator.Data.EquipmentItems[0].CompatibleAmmunition[0],
                                startingInfiltrator.Data.EquipmentItems[1].CompatibleAmmunition[0],

                                 };
                */
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateInitialPriest()
        {
            try
            {
                TacCharacterDef sourcePriest = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("S_AN_Priest_TacCharacterDef"));
                TacCharacterDef startingPriest = Helper.CreateDefFromClone(sourcePriest, "B1C9385B-05D1-453D-8665-4102CCBA77BE", "PX_Starting_Priest_TacCharacterDef");
                startingPriest.Data.Strength = 0;
                startingPriest.Data.Will = 0;

                startingPriest.Data.BodypartItems = new ItemDef[] // Armour
                {
              //  Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Head02_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("AN_Priest_Legs_ItemDef"))
                };
                //    TFTVLogger.Always(startingPriest.Data.EquipmentItems.Count().ToString());

                /*  ItemDef[] inventoryList = new ItemDef[]

                  { 
                  startingPriest.Data.EquipmentItems[0].CompatibleAmmunition[0],
                  startingPriest.Data.EquipmentItems[1].CompatibleAmmunition[0]
                  };*/

                startingPriest.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("AN_Redemptor_WeaponDef")),
                  //  Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("Medkit_EquipmentDef"))
                };
                // startingPriest.Data.InventoryItems = inventoryList;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateInitialTechnician()
        {
            try
            {
                TacCharacterDef sourceTechnician = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("NJ_Technician_TacCharacterDef"));
                TacCharacterDef startingTechnician = Helper.CreateDefFromClone(sourceTechnician, "1D0463F9-6684-4CE1-82CA-386FC2CE18E3", "PX_Starting_Technician_TacCharacterDef");
                startingTechnician.Data.Strength = 0;
                startingTechnician.Data.Will = 0;
                startingTechnician.Data.LevelProgression.Experience = 0;
                /*
                startingTechnician.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("NJ_Technician_Helmet_ALN_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("NJ_Technician_Torso_ALN_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("NJ_Technician_Legs_ALN_ItemDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("NJ_Technician_MechArms_ALN_WeaponDef"))
                };*/

                /*
                startingTechnician.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("NJ_Gauss_PDW_WeaponDef")),
                  Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("NJ_TechTurretGun_WeaponDef")),
                  Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("NJ_TechTurretGun_WeaponDef"))
                };
                startingTechnician.Data.InventoryItems = new ItemDef[] // Backpack
                {
                startingTechnician.Data.EquipmentItems[0].CompatibleAmmunition[0],
                startingTechnician.Data.EquipmentItems[1].CompatibleAmmunition[0]
                };*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
