using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVNewGameMenu
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

       /* [HarmonyPatch(typeof(GeoPhoenixFaction), "CreateInitialSquad")]
        internal static class BG_GeoPhoenixFaction_CreateInitialSquad_patch
        {
            private static readonly TacticalItemDef redeemerAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("AN_Redemptor_AmmoClip_ItemDef"));

            private static readonly TacticalItemDef pdwAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("NJ_Gauss_PDW_AmmoClip_ItemDef"));
            private static readonly TacticalItemDef mechArmsAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("MechArms_AmmoClip_ItemDef"));

            private static readonly TacticalItemDef boltsAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("SY_Crossbow_AmmoClip_ItemDef"));
            private static readonly TacticalItemDef spidersAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("SY_SpiderDroneLauncher_AmmoClip_ItemDef"));

            private static readonly TacCharacterDef jacob = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Jacob_TFTV_TacCharacterDef"));
            private static readonly TacCharacterDef sophia = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Sophia_TFTV_TacCharacterDef"));
            private static readonly TacCharacterDef assault = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_AssaultStarting_TacCharacterDef"));
            private static readonly TacCharacterDef heavy = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_HeavyStarting_TacCharacterDef"));
            private static readonly TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

            private static readonly TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
            private static readonly TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
            private static readonly TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));


            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(GeoPhoenixFaction __instance, GeoSite site)
            {
                try
                {

                    GeoVehicle geoVehicle = __instance.Vehicles.First();
                    geoVehicle.AddEquipment(hibernationModule);

                    TFTVConfig config = TFTVMain.Main.Config;


                    List<TacCharacterDef> chosenStartingTemplates = TFTVMain.startingTemplates;

                    GameDifficultyLevelDef currentDifficultyLevel = __instance.GeoLevel.CurrentDifficultyLevel;

                    TFTVStarts.ModifyStartingSquadStats(currentDifficultyLevel);

                    if (chosenStartingTemplates.Count > 6) //if buffed special start
                    {
                        if (currentDifficultyLevel.Order > 1) //if difficulty above Rookie
                        {
                            chosenStartingTemplates.Remove(sniper);
                        }
                    }
                    else
                    {
                        if (!chosenStartingTemplates.Contains(priest) && !chosenStartingTemplates.Contains(technician)
                            && !chosenStartingTemplates.Contains(infiltrator))  //if unbuffed or random PX start
                        {
                            if (currentDifficultyLevel.Order == 2)
                            {
                                chosenStartingTemplates.Remove(sniper);
                            }
                            else if (currentDifficultyLevel.Order > 2)
                            {
                                chosenStartingTemplates.Remove(sniper);
                                chosenStartingTemplates.Remove(assault);
                            }
                        }

                        else //if unbuffed or random special start
                        {
                            if (currentDifficultyLevel.Order == 2 || currentDifficultyLevel.Order == 3)
                            {
                                chosenStartingTemplates.Remove(sniper);
                                chosenStartingTemplates.Remove(assault);
                            }
                            else if (currentDifficultyLevel.Order == 4)
                            {
                                chosenStartingTemplates.Remove(sniper);
                                chosenStartingTemplates.Remove(assault);
                                chosenStartingTemplates.Remove(heavy);
                            }
                        }

                    }
                    foreach (TacCharacterDef template in chosenStartingTemplates)
                    {
                        GeoFaction geoFaction = new GeoFaction();

                        if (template.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.SynedrionFaction;
                            __instance.GeoLevel.EventSystem.SetVariable("BG_Start_Faction", 3);
                            TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.SynedrionFaction, site);
                        }
                        else if (template.name.Equals("PX_Starting_Priest_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.AnuFaction;
                            __instance.GeoLevel.EventSystem.SetVariable("BG_Start_Faction", 1);
                            TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.AnuFaction, site);
                        }
                        else if (template.name.Equals("PX_Starting_Technician_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.NewJerichoFaction;
                            __instance.GeoLevel.EventSystem.SetVariable("BG_Start_Faction", 2);
                            TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.NewJerichoFaction, site);
                        }
                        else
                        {
                            geoFaction = __instance;
                        }

                        if (!template.name.Contains("Buffed") && template != jacob && template != sophia)
                        {
                            GeoUnitDescriptor geoUnitDescriptor = geoFaction.GeoLevel.CharacterGenerator.GenerateUnit(geoFaction, template);
                            geoFaction.GeoLevel.CharacterGenerator.ApplyGenerationParameters(geoUnitDescriptor, currentDifficultyLevel.StartingSquadGenerationParams);
                            geoFaction.GeoLevel.CharacterGenerator.RandomizeIdentity(geoUnitDescriptor);

                            GeoCharacter character = geoUnitDescriptor.SpawnAsCharacter();
                            geoVehicle.AddCharacter(character);
                        }
                        else
                        {
                            GeoCharacter character = geoFaction.GeoLevel.CreateCharacterFromTemplate(template, __instance);
                            geoVehicle.AddCharacter(character);
                        }
                    }

                    List<ItemUnit> startingStorage = currentDifficultyLevel.StartingStorage.ToList();


                    if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                    {
                        startingStorage.Add(new ItemUnit(redeemerAmmo, 10));
                    }
                    else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                    {
                        startingStorage.Add(new ItemUnit(pdwAmmo, 10));
                        startingStorage.Add(new ItemUnit(mechArmsAmmo, 5));

                    }
                    else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                    {
                        startingStorage.Add(new ItemUnit(boltsAmmo, 20));
                        startingStorage.Add(new ItemUnit(spidersAmmo, 5));
                    }


                    foreach (ItemUnit itemUnit in startingStorage)
                    {
                        if (__instance.FactionDef.UseGlobalStorage)
                        {
                            __instance.ItemStorage.AddItem(new GeoItem(itemUnit));
                        }
                        else
                        {
                            site.ItemStorage.AddItem(new GeoItem(itemUnit));
                        }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return false;
            }
        }*/


        //You will need to edit scene hierarchy to add new objects under GameSettingsModule, it has a UIModuleGameSettings script
        //Class UIStateNewGeoscapeGameSettings is responsible for accepting selected settings and start the game, so you'll have to dig inside
        //for changing behaviour.
        /*   [HarmonyPatch(typeof(UIStateNewGeoscapeGameSettings), "EnterState")]
           internal static class UIStateNewGeoscapeGameSettings_EnterState_patch
           {
               private static void Postfix(UIStateNewGeoscapeGameSettings __instance)
               {
                   try
                   {



                   }



                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }


           }*/

        public static void ModifyStartingSquadStats(GameDifficultyLevelDef gameDifficulty)

        {
            try
            {
                TacCharacterDef newJacob = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_TFTV_TacCharacterDef"));
                TacCharacterDef newSophia = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Sophia_TFTV_TacCharacterDef"));

                int strengthBonus = 0;
                int willBonus = 0;

                if (gameDifficulty.Order == 3)
                {
                    strengthBonus = 4;
                    willBonus = 1;
                }
                else if (gameDifficulty.Order == 2)
                {
                    strengthBonus = 6;
                    willBonus = 2;
                }
                else if (gameDifficulty.Order == 1)
                {
                    strengthBonus = 8;
                    willBonus = 3;
                }

                newJacob.Data.Strength += strengthBonus;
                newJacob.Data.Will += willBonus;

                newSophia.Data.Strength += strengthBonus;
                newSophia.Data.Will += willBonus;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        //Adapted from MadSkunky's TutorialTweaks: https://github.com/MadSkunky/PP-Mods-TutorialTweaks
        public static List<TacCharacterDef> SetInitialSquadUnbuffed()
        {
            try
            {
                TacCharacterDef newJacob = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Jacob_TFTV_TacCharacterDef"));
                TacCharacterDef newSophia = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_Sophia_TFTV_TacCharacterDef"));
                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));

                TacCharacterDef assault = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_AssaultStarting_TacCharacterDef"));
                TacCharacterDef heavy = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_HeavyStarting_TacCharacterDef"));
                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

                TFTVConfig config = TFTVMain.Main.Config;

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>
                {
                    newSophia,
                    newJacob
                };

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    // level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    // level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    //  level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }


                /*
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

                newJacob.Data.Strength += strengthBonus;
                newJacob.Data.Will += willBonus;

                newSophia.Data.Strength += strengthBonus;
                newSophia.Data.Will += willBonus;

                */
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
        public static List<TacCharacterDef> SetInitialSquadBuffed()
        {
            try
            {
                TacCharacterDef Jacob2buffed = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_JacobBuffed_TFTV_TacCharacterDef"));
                TacCharacterDef Sophia2buffed = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_SophiaBuffed_TFTV_TacCharacterDef"));
                TacCharacterDef Omar3buffed = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_OmarBuffed_TFTV_TacCharacterDef"));
                TacCharacterDef Takeshi3buffed = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_TakeshiBuffed_TFTV_TacCharacterDef"));
                TacCharacterDef Irina3buffed = Repo.GetAllDefs<TacCharacterDef>().First(tcd => tcd.name.Equals("PX_IrinaBuffed_TFTV_TacCharacterDef"));

                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));

                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));

                TFTVConfig config = TFTVMain.Main.Config;

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef> { Jacob2buffed, Sophia2buffed, Omar3buffed, Takeshi3buffed, Irina3buffed };


                if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    // level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    //  level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    //   level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }

                startingTemplates.Add(sniper);

                return startingTemplates;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static List<TacCharacterDef> SetInitialSquadRandom()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                TacCharacterDef priest = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Priest_TacCharacterDef"));
                TacCharacterDef technician = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Technician_TacCharacterDef"));
                TacCharacterDef infiltrator = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"));
                TacCharacterDef assault = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_AssaultStarting_TacCharacterDef"));
                TacCharacterDef heavy = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_HeavyStarting_TacCharacterDef"));
                TacCharacterDef sniper = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(a => a.name.Equals("PX_SniperStarting_TacCharacterDef"));


                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>
                {
                    assault,
                    sniper
                };

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                    // level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {

                    startingTemplates.Add(technician);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                    // level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {

                    startingTemplates.Add(infiltrator);
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);

                    //  level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }
                else
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }


                /* if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                 {
                     startingTemplates.Add(assault);
                 }
                 else if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                 {
                     startingTemplates.Add(heavy);
                 }
                 else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                 {
                     
                 }
                 else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                 {
                     
                 }*/

                return startingTemplates;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }



    }
}
