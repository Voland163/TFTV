using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Objectives;
using System;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVNewPXCharacters
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        // public static readonly Sprite AlistairOffice = Helper.CreateSpriteFromImageFile("background_alistair_office.jpg");
        //  public static readonly Sprite OlenaOffice = Helper.CreateSpriteFromImageFile("insidebase.jpg");
        //  public static readonly Sprite HelenaOffice = Helper.CreateSpriteFromImageFile("background_office.jpg");
        //  public static readonly Sprite AncientBackground = Helper.CreateSpriteFromImageFile("background_ancients.jpg");

        private static readonly Sprite OlenaPic = Helper.CreateSpriteFromImageFile("BG_Olena_small.png");
        private static readonly Sprite AlistairPic = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
        private static readonly Sprite OlenaTired = Helper.CreateSpriteFromImageFile("BG_Olena_tired.png");
        private static readonly Sprite AlistairTired = Helper.CreateSpriteFromImageFile("BG_alistair_tired.png");
        private static readonly Sprite OlenaExhausted = Helper.CreateSpriteFromImageFile("BG_Olena_exhausted.png");
        private static readonly Sprite AlistairExhausted = Helper.CreateSpriteFromImageFile("BG_alistair_exhausted.png");
        private static readonly Sprite HelenaPic = Helper.CreateSpriteFromImageFile("helena.png");
        private static readonly Sprite HelenaTired = Helper.CreateSpriteFromImageFile("helena_tired.png");
        private static readonly Sprite HelenaExhausted = Helper.CreateSpriteFromImageFile("helena_exhausted.png");

        public static void PlayIntro(GeoLevelController level)
        {
            try
            {
                if (level.EventSystem.GetVariable("BG_Intro_Played") == 0)
                {
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(level.PhoenixFaction, level.ViewerFaction);
                    level.EventSystem.TriggerGeoscapeEvent("IntroBetterGeo_0", geoscapeEventContext);
                    level.EventSystem.SetVariable("BG_Intro_Played", 1);
                }
                if (level.EventSystem.GetVariable("BG_Intro_Played") == 1)
                {
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(level.PhoenixFaction, level.ViewerFaction);
                    level.EventSystem.TriggerGeoscapeEvent("IntroBetterGeo_1", geoscapeEventContext);
                    level.EventSystem.SetVariable("BG_Intro_Played", 2);
                }
                if (level.EventSystem.GetVariable("BG_Intro_Played") == 2)
                {
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(level.PhoenixFaction, level.ViewerFaction);
                    level.EventSystem.TriggerGeoscapeEvent("IntroBetterGeo_2", geoscapeEventContext);
                    level.EventSystem.SetVariable("BG_Intro_Played", 3);
                    
                    
                    level.EventSystem.SetVariable("NewConfigImplemented", 1);
                    //  TFTVBetaSaveGamesFixes.CheckNewLOTA(level);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(GeoFactionObjective), "GetIcon")]
        internal static class BG_GeoFactionObjective_GetIcon_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref Sprite __result, GeoFactionObjective __instance)
            {
                try
                {

                    if (__instance.Title != null && __instance.Title.LocalizationKey.Contains("VOID_OMEN_TITLE_"))
                    {
                        __result = TFTVDefsRequiringReinjection.VoidIcon;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //This is a patch to trigger events that introduce lines from characters;
        //needs to be done this way because if TriggerEncounter is assigned to only Outcome, that event is triggered before the Outcome! 

        [HarmonyPatch(typeof(GeoscapeEventSystem), "TriggerGeoscapeEvent")]

        public static class GeoscapeEventSystem_TriggerGeoscapeEvent_TriggerAdditionalEvent_patch
        {
            public static void Postfix(string eventId, GeoscapeEventSystem __instance, GeoscapeEventContext context)
            {
                try
                {
                    TFTVLogger.Always($"TriggerGeoscapeEvent triggered for event {eventId}");

                    if (eventId == "PROG_PX10_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("AlistairOnSymes1", context);
                    }
                    else if (eventId == "PROG_CH0_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("AlistairOnBarnabas", context);
                    }
                    else if (eventId == "PROG_PX1_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("AlistairOnSymes2", context);
                    }
                    else if (eventId == "PROG_LE0_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("HelenaOnOlena", context);
                    }
                    else if (eventId == "PROG_NJ1_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnWest", context);
                    }
                    else if (eventId == "PROG_AN6_WIN2")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnSynod", context);
                    }
                    else if (eventId == "PROG_LE_FINAL")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnAncients", context);
                    }
                    else if (eventId == "PROG_FS1_WIN")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnBehemoth", context);
                    }
                    else if (eventId == "AlistairOnSymes2")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnSymes", context);
                    }
                    else if (eventId == "Anu_Pissed2")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnBionicsLabSabotage", context);
                    }
                    else if (eventId == "NJ_Pissed2")
                    {
                        __instance.TriggerGeoscapeEvent("OlenaOnMutationsLabSabotage", context);
                    }
                    else if (eventId == "PROG_FS2_WIN")
                    {

                        GeoscapeEventDef afterWest = DefCache.GetDef<GeoscapeEventDef>("AlistairRoadsNoWest");
                        GeoscapeEventDef afterSynedrion = DefCache.GetDef<GeoscapeEventDef>("AlistairRoadsNoSynedrion");
                        GeoscapeEventDef afterAnu = DefCache.GetDef<GeoscapeEventDef>("AlistairRoadsNoAnu");
                        string answerAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_DESCRIPTION";
                        string questionAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_CHOICE";
                        string answerAboutHelena = "KEY_ALISTAIRONHELENA_DESCRIPTION";
                        string questionAboutHelena = "KEY_ALISTAIRONHELENA_CHOICE";


                        if (context.Level.EventSystem.GetVariable("SymesAlternativeCompleted") == 1)
                        {
                            afterWest.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutHelena;
                            afterWest.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutHelena;
                            afterSynedrion.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutHelena;
                            afterSynedrion.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutHelena;
                            afterAnu.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutHelena;
                            afterAnu.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutHelena;
                        }
                        else
                        {
                            afterWest.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutVirophage;
                            afterWest.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutVirophage;
                            afterSynedrion.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutVirophage;
                            afterSynedrion.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutVirophage;
                            afterAnu.GeoscapeEventData.Choices[3].Outcome.OutcomeText.General.LocalizationKey = answerAboutVirophage;
                            afterAnu.GeoscapeEventData.Choices[3].Text.LocalizationKey = questionAboutVirophage;
                        }

                        // TFTVThirdAct.ActivateFS3Event(context.Level);
                        TFTVVoidOmens.RemoveAllVoidOmens(context.Level);
                        __instance.TriggerGeoscapeEvent("AlistairRoads", context);
                        TFTVDelirium.RemoveDeliriumFromAllCharactersWithoutMutations(context.Level);
                    }
                    else if (eventId == "Helena_Oneiromancy")
                    {
                        __instance.TriggerGeoscapeEvent("Olena_Oneiromancy", context);
                    }


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(SiteEncountersArtCollectionDef), "GetEventArt")]
        public static class SiteEncountersArtCollectionDef_GetEventArt_InjectArt_patch
        {
            public static void Postfix(ref EncounterEventArt __result, GeoscapeEvent geoEvent)
            {
                try
                {
                   /* Sprite OlenaPic = Helper.CreateSpriteFromImageFile("BG_Olena_small.png");
                    Sprite AlistairPic = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
                    Sprite OlenaTired = Helper.CreateSpriteFromImageFile("BG_Olena_tired.png");
                    Sprite AlistairTired = Helper.CreateSpriteFromImageFile("BG_alistair_tired.png");
                    Sprite OlenaExhausted = Helper.CreateSpriteFromImageFile("BG_Olena_exhausted.png");
                    Sprite AlistairExhausted = Helper.CreateSpriteFromImageFile("BG_alistair_exhausted.png");
                    Sprite HelenaPic = Helper.CreateSpriteFromImageFile("helena.png");
                    Sprite HelenaTired = Helper.CreateSpriteFromImageFile("helena_tired.png");
                    Sprite HelenaExhausted = Helper.CreateSpriteFromImageFile("helena_exhausted.png");*/



                    GeoLevelController controller = geoEvent.Context.Level;
                    /* if (geoEvent.EventID.Equals("OlenaOnFirstFlyer") || geoEvent.EventID.Equals("OlenaOnFirstHavenTarget")) 
                     { 
                         __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_Olena_small.png");
                         //__result.EventBackground
                     }*/

                    /* if (geoEvent.EventID.Equals("PROG_LE2_WARN"))
                     {
                         __result.EventLeader = Helper.CreateSpriteFromImageFile("helena.png");
                     }*/

                    if (geoEvent.EventID.Equals("HelenaOnOlena"))
                    {

                        if (controller.EventSystem.GetEventRecord("PROG_LE0_MISS").SelectedChoice == 2)
                        {
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("Helena_peace.jpg");
                        }
                        else
                        {
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("Helena_fire2.jpg");
                        }

                        __result.EventLeader = HelenaPic;
                    }

                  /*  else if (geoEvent.EventID.Contains("SDI"))
                    {                      
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("ruins.png");//AlistairOffice;
                    }*/

                    else if (geoEvent.EventID.Equals("VoidOmen") || geoEvent.EventID == "PROG_FS10" || geoEvent.EventID.Contains("Alistair")
                            || geoEvent.EventID.Equals("PROG_LE3_WARN"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = AlistairExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = AlistairTired;
                        }
                        else
                        {
                            __result.EventLeader = AlistairPic;
                        }


                        __result.EventBackground = Helper.CreateSpriteFromImageFile("background_alistair_office.jpg");//AlistairOffice;
                    }



                    else if (geoEvent.EventID == "PROG_FS2" || geoEvent.EventID == "PROG_LE1_WARN" || (geoEvent.EventID.Contains("Olena") && !geoEvent.EventID.Contains("Helena")) ||
                        geoEvent.EventID == "PROG_LE1")
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = OlenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = OlenaTired;
                        }
                        else
                        {
                            __result.EventLeader = OlenaPic;
                        }
                        if (geoEvent.EventData.Description[0].General.LocalizationKey != "BASEDEFENSE_CONTAINMENTBREACH_TEXT")
                        {
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("insidebase.jpg");
                        }

                        
                    }

                    else if(geoEvent.EventID == "PROG_FS20")
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = OlenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = OlenaTired;
                        }
                        else
                        {
                            __result.EventLeader = OlenaPic;
                        }
                    }

                    else if (geoEvent.EventID == "PROG_FS9")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_HammerFallAlt.jpg");
                    }

                    else if (geoEvent.EventID.Contains("SDI"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = AlistairExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = AlistairTired;
                        }
                        else
                        {
                            __result.EventLeader = AlistairPic;
                        }
                    }

                    else if (geoEvent.EventID.Equals("PROG_FS0"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Hammerfall_impact2.jpg");
                    }

                    else if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_02"
                        || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_05" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_08"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_05.jpg");
                    }

                    else if (geoEvent.EventID.Equals("VoidOmen") && geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_11")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_11.jpg");
                    }

                    else if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_12" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_09"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_12.jpg");
                    }
                    else if (geoEvent.EventID.Equals("VoidOmen") && geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_13")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_13.jpg");
                    }
                    else if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_15" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_16"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_15.jpg");
                    }
                    else if (geoEvent.EventID.Equals("IntroBetterGeo_2"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("insidebase.jpg");
                        __result.EventLeader = OlenaPic;
                    }
                    else if (geoEvent.EventID.Equals("IntroBetterGeo_1"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                        __result.EventLeader = AlistairPic;
                    }
                    else if (geoEvent.EventID.Equals("IntroBetterGeo_0"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_0.jpg");
                    }
                    else if (geoEvent.EventID == "PROG_LW1_WIN")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("makeshift_lab.png");
                    }
                    else if (TFTVProjectOsiris.ProjectOsirisDeliveryEvents.Contains(geoEvent.EventID))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Bionics.png");

                        /* if (geoEvent.EventID == TFTVProjectOsiris.ProjectOsirisEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris.png");
                          //   __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
                         }
                         else if(geoEvent.EventID == TFTVProjectOsiris.RoboCopDeliveryEvent || geoEvent.EventID == TFTVProjectOsiris.RobocopEvent) 
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Juggernaut.png");

                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.FullMutantEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Mutation.png");

                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.HeavyMutantDeliveryEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Heavy_Mutant.png");

                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.WatcherMutantDeliveryEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Watcher.png");

                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.ShooterMutantDeliveryEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Shooter.png");

                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.ScoutDeliveryEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Exo.png");
                         }
                         else if (geoEvent.EventID == TFTVProjectOsiris.ShinobiDeliveryEvent)
                         {
                             __result.EventBackground = Helper.CreateSpriteFromImageFile("Project_Osiris_Shinobi.png");
                         }*/

                    }
                    else if (geoEvent.EventID.Equals("Helena_Echoes") || geoEvent.EventID.Equals("Helena_Oneiromancy") || geoEvent.EventID.Equals("Helena_Can_Build_Cyclops"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = HelenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = HelenaTired;
                        }
                        else
                        {
                            __result.EventLeader = HelenaPic;
                        }
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("background_office.jpg");//HelenaOffice;
                    }
                    else if (geoEvent.EventID.Equals("Helena_Beast"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = HelenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = HelenaTired;
                        }
                        else
                        {
                            __result.EventLeader = HelenaPic;
                        }
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("background_ancients.jpg");//AncientBackground;
                    }
                    else if (geoEvent.EventID.Equals("Cyclops_Dreams"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("background_cyclops.jpg");
                    }
                    else if (geoEvent.EventID.Equals("Helena_Virophage"))
                    {

                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = HelenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = HelenaTired;
                        }
                        else
                        {
                            __result.EventLeader = HelenaPic;
                        }

                        __result.EventBackground = Helper.CreateSpriteFromImageFile("background_cyclops.jpg");
                    }
                    else if (geoEvent.EventID.Equals("OlenaBaseDefense"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = OlenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = OlenaTired;
                        }
                        else
                        {
                            __result.EventLeader = OlenaPic;
                        }

                        if (geoEvent.EventData.Description[0].General.LocalizationKey != "BASEDEFENSE_CONTAINMENTBREACH_TEXT")
                        {
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("insidebase.jpg");
                        }
                    }

                    else if (geoEvent.EventID.Equals("PROG_FS3"))
                    {
                        if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                        {
                            __result.EventLeader = OlenaExhausted;
                        }
                        else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                        {
                            __result.EventLeader = OlenaTired;
                        }
                        else
                        {
                            __result.EventLeader = OlenaPic;
                        }
                      
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
