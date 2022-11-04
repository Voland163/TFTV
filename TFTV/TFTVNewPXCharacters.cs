using Base.Defs;
using EnviroSamples;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVNewPXCharacters
    {
        private static readonly Sprite OlenaPic = Helper.CreateSpriteFromImageFile("BG_Olena_small.png");
        private static readonly Sprite AlistairPic = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");

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

        public static class GeoscapeEventSystem_TriggerGeoscapeEvent_patch
        {
            public static void Postfix(string eventId, GeoscapeEventSystem __instance, GeoscapeEventContext context)
            {
                try
                {
                    TFTVLogger.Always("TriggerGeoscapeEvent triggered for event " + eventId);
                                                  
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
                    else if(eventId == "PROG_LE0_WIN") 
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
                    else if(eventId == "PROG_LE_FINAL") 
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
                    else if (eventId== "PROG_FS2_WIN") 
                    {
                        // TFTVThirdAct.ActivateFS3Event(context.Level);
                        TFTVVoidOmens.RemoveAllVoidOmens(context.Level);
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


                    /* if (geoEvent.EventID.Equals("OlenaOnFirstFlyer") || geoEvent.EventID.Equals("OlenaOnFirstHavenTarget")) 
                     { 
                         __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_Olena_small.png");
                         //__result.EventBackground
                     }*/

                    if (geoEvent.EventID.Equals("PROG_LE2_WARN"))
                    {
                        __result.EventLeader = Helper.CreateSpriteFromImageFile("helena.png");
                    }

                    if (geoEvent.EventID.Equals("HelenaOnOlena"))
                    {
                        GeoLevelController controller = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));
                        if (controller.EventSystem.GetEventRecord("PROG_LE0_MISS").SelectedChoice == 2) 
                        {
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("Helena_peace.jpg");
                        }
                        else 
                        { 
                            __result.EventBackground = Helper.CreateSpriteFromImageFile("Helena_fire2.jpg");
                        }

                        __result.EventLeader = Helper.CreateSpriteFromImageFile("helena.png");                    
                    }

                    if (geoEvent.EventID.Equals("VoidOmen") || geoEvent.EventID == "PROG_FS10" || geoEvent.EventID.Contains("Alistair")
                            || geoEvent.EventID.Equals("PROG_LE3_WARN"))
                    {
                        __result.EventLeader = AlistairPic;
                    }

                    if (geoEvent.EventID == "PROG_FS2" || geoEvent.EventID == "PROG_LE1_WARN" || (geoEvent.EventID.Contains("Olena") && !geoEvent.EventID.Contains("Helena")))
                    {
                        __result.EventLeader = OlenaPic;
                    }

                    if (geoEvent.EventID == "PROG_FS20")
                    {
                        __result.EventLeader = OlenaPic;
                    }

                    if (geoEvent.EventID == "PROG_FS9")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_HammerFallAlt.jpg");
                    }

                    if (geoEvent.EventID.Contains("SDI"))
                    {
                        __result.EventLeader = AlistairPic;
                    }

                    if (geoEvent.EventID.Equals("PROG_FS0"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Hammerfall_impact2.jpg");
                    }

                    if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_02"
                        || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_05" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_08"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_05.jpg");
                    }

                    if (geoEvent.EventID.Equals("VoidOmen") && geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_11")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_11.jpg");
                    }

                    if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_12" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_09"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_12.jpg");
                    }
                    if (geoEvent.EventID.Equals("VoidOmen") && geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_13")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_13.jpg");
                    }
                    if (geoEvent.EventID.Equals("VoidOmen") && (geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_15" || geoEvent.EventData.Title.LocalizationKey == "VOID_OMEN_TITLE_16"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("VO_15.jpg");
                    }
                    if (geoEvent.EventID.Equals("IntroBetterGeo_2"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                        __result.EventLeader = OlenaPic;
                    }
                    if (geoEvent.EventID.Equals("IntroBetterGeo_1"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                        __result.EventLeader = AlistairPic;
                    }
                    if (geoEvent.EventID.Equals("IntroBetterGeo_0"))
                    {                      
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_0.jpg");                                      
                    }
                    if (geoEvent.EventID == "PROG_LW1_WIN")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("makeshift_lab.png");
                    }
                    if (TFTVProjectOsiris.ProjectOsirisDeliveryEvents.Contains(geoEvent.EventID)) 
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

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
