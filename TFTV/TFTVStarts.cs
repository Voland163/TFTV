using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVStarts
    {

       // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ModifyIntroForSpecialStart(GeoFaction geoFaction, GeoSite site)
        {
            try
            {
                GeoscapeEventDef intro =DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_0");

                string factionStartIntroText0 = TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_START_INTROTEXT0");
                string factionStartIntroText1 = TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_START_INTROTEXT1");
                string a = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_INDEFINITEARTICLE");
                string factionStartIntroText2 = TFTVCommonMethods.ConvertKeyToString("KEY_FACTION_START_INTROTEXT2");

                intro.GeoscapeEventData.Description[0].General = new LocalizedTextBind($"{factionStartIntroText0} " +
                    $"{FindNearestHaven(geoFaction, site)},{a}{geoFaction.Name.LocalizeEnglish()} {factionStartIntroText1}" +
                    $"{geoFaction.GeoLevel.PhoenixFaction.Vehicles.First().Soldiers.Last().DisplayName}{a}{geoFaction.Name.LocalizeEnglish()} " +
                    $"{geoFaction.GeoLevel.PhoenixFaction.Vehicles.First().Soldiers.Last().ClassTag.className}." +
                    $"\n\n{factionStartIntroText2}", true);

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void RevertIntroToNormalStart()
        {
            try
            {
     
                GeoscapeEventDef intro =DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_0");

                intro.GeoscapeEventData.Description[0].General = new LocalizedTextBind(TFTVCommonMethods.ConvertKeyToString("BG_INTRO_0_DESCRIPTION"), true);


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


                IOrderedEnumerable<GeoSite> orderedEnumerable = from s in geoFaction.GeoLevel.Map.AllSites
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
                TacCharacterDef newJacob =DefCache.GetDef<TacCharacterDef>("PX_Jacob_TFTV_TacCharacterDef");
                TacCharacterDef newSophia =DefCache.GetDef<TacCharacterDef>("PX_Sophia_TFTV_TacCharacterDef");
                TacCharacterDef priest =DefCache.GetDef<TacCharacterDef>("PX_Starting_Priest_TacCharacterDef");
                TacCharacterDef technician =DefCache.GetDef<TacCharacterDef>("PX_Starting_Technician_TacCharacterDef");
                TacCharacterDef infiltrator =DefCache.GetDef<TacCharacterDef>("PX_Starting_Infiltrator_TacCharacterDef");

                TacCharacterDef assault =DefCache.GetDef<TacCharacterDef>("PX_AssaultStarting_TacCharacterDef");
                TacCharacterDef heavy =DefCache.GetDef<TacCharacterDef>("PX_HeavyStarting_TacCharacterDef");
                TacCharacterDef sniper =DefCache.GetDef<TacCharacterDef>("PX_SniperStarting_TacCharacterDef");

                TFTVConfig config = TFTVMain.Main.Config;

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>
                {
                    newSophia,
                    newJacob
                };

                if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 2 && TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 2 && TFTVNewGameOptions.startingSquad != TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 1 && TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 1 && TFTVNewGameOptions.startingSquad != TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                }


                if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
                }


                int strengthBonus = 0;
                int willBonus = 0;

                if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 3)
                {
                    strengthBonus = 4;
                    willBonus = 1;
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 2)
                {
                    strengthBonus = 6;
                    willBonus = 2;
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 1)
                {
                    strengthBonus = 8;
                    willBonus = 3;
                }

                newJacob.Data.Strength = assault.Data.Strength + strengthBonus;
                newJacob.Data.Will = assault.Data.Will + willBonus;

                newSophia.Data.Strength = assault.Data.Strength + strengthBonus;
                newSophia.Data.Will = assault.Data.Will + willBonus;


                return startingTemplates;


                /*
                TacCharacterDef Omar2 =DefCache.GetDef<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Omar_Tutorial2_TacCharacterDef"));
                Omar2.Data.Strength = 0;
                Omar2.Data.Will = 0;
                Omar2.Data.Speed = 0;
                Omar2.Data.CurrentHealth = -1;
                TacCharacterDef Takeshi3 =DefCache.GetDef<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Takeshi_Tutorial3_TacCharacterDef"));
                Takeshi3.Data.Strength = 0;
                Takeshi3.Data.Will = 0;
                Takeshi3.Data.Speed = 0;
                Takeshi3.Data.CurrentHealth = -1;
                TacCharacterDef Irina3 =DefCache.GetDef<TacCharacterDef>().First(tcd => tcd.name.Contains("PX_Irina_Tutorial3_TacCharacterDef"));
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
                TacCharacterDef Jacob2buffed =DefCache.GetDef<TacCharacterDef>("PX_JacobBuffed_TFTV_TacCharacterDef");
                TacCharacterDef Sophia2buffed =DefCache.GetDef<TacCharacterDef>("PX_SophiaBuffed_TFTV_TacCharacterDef");
                TacCharacterDef Omar3buffed =DefCache.GetDef<TacCharacterDef>("PX_OmarBuffed_TFTV_TacCharacterDef");
                TacCharacterDef Takeshi3buffed =DefCache.GetDef<TacCharacterDef>("PX_TakeshiBuffed_TFTV_TacCharacterDef");
                TacCharacterDef Irina3buffed =DefCache.GetDef<TacCharacterDef>("PX_IrinaBuffed_TFTV_TacCharacterDef");

                TacCharacterDef priest =DefCache.GetDef<TacCharacterDef>("PX_Starting_Priest_TacCharacterDef");
                TacCharacterDef technician =DefCache.GetDef<TacCharacterDef>("PX_Starting_Technician_TacCharacterDef");
                TacCharacterDef infiltrator =DefCache.GetDef<TacCharacterDef>("PX_Starting_Infiltrator_TacCharacterDef");

                TacCharacterDef sniper =DefCache.GetDef<TacCharacterDef>("PX_SniperStarting_TacCharacterDef");

                TFTVConfig config = TFTVMain.Main.Config;

                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef> { Jacob2buffed, Sophia2buffed, Omar3buffed, Takeshi3buffed, Irina3buffed };

                if (TFTVSpecialDifficulties.DifficultyOrderConverter(difficultyLevel.Order) == 1)
                {
                    startingTemplates.Add(sniper);
                }

                if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.ANU)
                {
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.NJ)
                {
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
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

                TacCharacterDef priest =DefCache.GetDef<TacCharacterDef>("PX_Starting_Priest_TacCharacterDef");
                TacCharacterDef technician =DefCache.GetDef<TacCharacterDef>("PX_Starting_Technician_TacCharacterDef");
                TacCharacterDef infiltrator =DefCache.GetDef<TacCharacterDef>("PX_Starting_Infiltrator_TacCharacterDef");
                TacCharacterDef assault =DefCache.GetDef<TacCharacterDef>("PX_AssaultStarting_TacCharacterDef");
                TacCharacterDef heavy =DefCache.GetDef<TacCharacterDef>("PX_HeavyStarting_TacCharacterDef");
                TacCharacterDef sniper =DefCache.GetDef<TacCharacterDef>("PX_SniperStarting_TacCharacterDef");


                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>
                {
                    heavy,
                    assault,
                    assault,
                    sniper
                };


                if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 2 && TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 2 && TFTVNewGameOptions.startingSquad != TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 1 && TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(assault);
                    startingTemplates.Add(sniper);
                }
                else if (TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order) == 1 && TFTVNewGameOptions.startingSquad != TFTVNewGameOptions.StartingSquadFaction.PHOENIX)
                {
                    startingTemplates.Add(heavy);
                    startingTemplates.Add(assault);
                }


                if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.ANU)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(priest);
                    level.EventSystem.SetVariable("BG_Start_Faction", 1);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.NJ)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(technician);
                    level.EventSystem.SetVariable("BG_Start_Faction", 2);
                }
                else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.SYNEDRION)
                {
                    startingTemplates.Remove(heavy);
                    startingTemplates.Remove(assault);
                    startingTemplates.Add(infiltrator);
                    level.EventSystem.SetVariable("BG_Start_Faction", 3);
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
