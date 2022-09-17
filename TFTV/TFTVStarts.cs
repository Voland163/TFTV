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


                if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                }
                else if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                }
                else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }
                else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
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

        public static List<TacCharacterDef> SetInitialSquadRandom(GeoLevelController level)
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
                    heavy,
                    assault,
                    assault,
                    sniper
                };

                if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }


                if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                }
                else if (level.CurrentDifficultyLevel.Order == 2 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                }
                else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad == TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }
                else if (level.CurrentDifficultyLevel.Order == 1 && config.startingSquad != TFTVConfig.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                }

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
