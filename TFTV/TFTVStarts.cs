using Base.Defs;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVStarts
    {
        // PX_Jacob_Tutorial2_TacCharacterDef replace [3], with hard starting squad [1]
        // PX_Sophia_Tutorial2_TacCharacterDef replace [1], with hard starting squad [2]
        private static readonly DefRepository Repo = TFTVMain.Repo;

        //Adapted from MadSkunky's TutorialTweaks: https://github.com/MadSkunky/PP-Mods-TutorialTweaks
        public static void MakeJacobIntoSniper()
        {
            try
            {
                
                

                CreateInitialInfiltrator();
                CreateInitialPriest();
                CreateInitialTechnician();

                // Get Jacobs definition for the 1st part of the tutorial
                TacCharacterDef Jacob1 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Jacob_Tutorial_TacCharacterDef"));
                // Set class related definition for actor view
                Jacob1.Data.ViewElementDef = Repo.GetAllDefs<ViewElementDef>().First(ved => ved.name.Contains("E_View [PX_Sniper_ActorViewDef]"));
                // Switch the given Assault ClassTagDef in Jacobs GameTags to Sniper (keep both ClassTagDefs would make him dual classed rigth from scratch)
                GameTagDef Sniper_CTD = Repo.GetAllDefs<GameTagDef>().First(gtd => gtd.name.Contains("Sniper_ClassTagDef"));
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
                Repo.GetAllDefs<ClassProficiencyAbilityDef>().First(cpad => cpad.name.Contains("Sniper_ClassProficiency_AbilityDef"))
                                                                                                                                   
                };
                Jacob1.Data.BodypartItems = new ItemDef[] // Armour
                {
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("PX_Sniper_Helmet_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("PX_Sniper_Torso_BodyPartDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("PX_Sniper_Legs_ItemDef"))
                };


                Jacob1.Data.EquipmentItems = new ItemDef[] // Ready slots
                { Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("PX_SniperRifle_WeaponDef")),
                    Repo.GetAllDefs<WeaponDef>().First(wd => wd.name.Contains("PX_Pistol_WeaponDef")),
                Repo.GetAllDefs<TacticalItemDef>().First(tad => tad.name.Contains("Medkit_EquipmentDef"))
                };
                Jacob1.Data.InventoryItems = new ItemDef[] // Backpack
                {
                Jacob1.Data.EquipmentItems[0].CompatibleAmmunition[0],
                Jacob1.Data.EquipmentItems[1].CompatibleAmmunition[0]
                };
                // Get Jacobs definition for the 2nd and following parts of the tutorial
                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Jacob_Tutorial2_TacCharacterDef"));
                // Copy changes from Jabobs 1st to his 2nd definition
                Jacob2.Data.ViewElementDef = Jacob1.Data.ViewElementDef;
                Jacob2.Data.GameTags = Jacob1.Data.GameTags;
                Jacob2.Data.Abilites = Jacob1.Data.Abilites;
                Jacob2.Data.BodypartItems = Jacob1.Data.BodypartItems;
                Jacob2.Data.EquipmentItems = Jacob1.Data.EquipmentItems;
                Jacob2.Data.InventoryItems = Jacob1.Data.InventoryItems;
                Jacob2.Data.Strength = 0;
                Jacob2.Data.Will = 0;
                Jacob2.Data.Speed = 0;
                Jacob2.Data.CurrentHealth = -1;

                TacCharacterDef Sophia2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Sophia_Tutorial2_TacCharacterDef"));
                Sophia2.Data.Strength = 0;
                Sophia2.Data.Will = 0;
                Sophia2.Data.Speed = 0;
                Sophia2.Data.CurrentHealth = -1;

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
                Irina3.Data.CurrentHealth = -1;

                

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AdjustStatsDifficulty(GeoLevelController level)
        {
            try 
            {
                int strengthBonus = 0;
                int willBonus = 0;

                if (level.CurrentDifficultyLevel.Order == 3)
                {
                    strengthBonus = 4;
                    willBonus = 1;
                }
                else if (level.CurrentDifficultyLevel.Order == 2)
                {
                    strengthBonus = 6;
                    willBonus = 2;
                }
                else if (level.CurrentDifficultyLevel.Order == 1)
                {
                    strengthBonus = 8;
                    willBonus = 3;
                }

                TacCharacterDef Jacob2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Jacob_Tutorial2_TacCharacterDef"));
                TacCharacterDef Sophia2 = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Sophia_Tutorial2_TacCharacterDef"));

                Jacob2.Data.Strength = strengthBonus;
                Jacob2.Data.Will = willBonus;

                Sophia2.Data.Strength = strengthBonus;
                Sophia2.Data.Will = willBonus;
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
                TFTVLogger.Always(startingPriest.Data.EquipmentItems.Count().ToString());

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
