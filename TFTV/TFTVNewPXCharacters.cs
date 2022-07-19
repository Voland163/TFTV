using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVNewPXCharacters
    {
        
        public static void CreateIntro()
        {
            try
            {
                string introEvent_0 = "IntroBetterGeo_0";
                string introEvent_1 = "IntroBetterGeo_1";
                string introEvent_2 = "IntroBetterGeo_2";
                GeoscapeEventDef intro0 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_0, "BG_INTRO_0_TITLE", "BG_INTRO_0_DESCRIPTION", null);
                GeoscapeEventDef intro1 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_1, "BG_INTRO_1_TITLE", "BG_INTRO_1_DESCRIPTION", null);
                // intro1.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = introEvent_0;
                GeoscapeEventDef intro2 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_2, "BG_INTRO_2_TITLE", "BG_INTRO_2_DESCRIPTION", null);
                // intro2.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = introEvent_1;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

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


        [HarmonyPatch(typeof(SiteEncountersArtCollectionDef), "GetEventArt")]
        public static class SiteEncountersArtCollectionDef_GetEventArt_InjectArt_patch
        {
            public static void Postfix(ref EncounterEventArt __result, GeoscapeEvent geoEvent)
            {
                try
                {
                    /* if (geoEvent.EventID.Equals("Anu_Pissed1"))
                     {
                         __result.EventBackground = Helper.CreateSpriteFromImageFile("combat.png");
                     }
                    */

                    if (geoEvent.EventID == "PROG_FS9")
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_HammerFallAlt.jpg");

                    }

                    if (geoEvent.EventID.Contains("SDI"))
                    {
                        __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
                    }

                    if (geoEvent.EventID.Equals("PROG_FS0"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("FesteringSkiesAfterHamerfall.png");
                    }

                    if (geoEvent.EventID.Equals("VoidOmen"))
                    {
                        __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
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


                    if (geoEvent.EventID.Equals("IntroBetterGeo_2"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                        __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_Olena.jpg");
                    }
                    if (geoEvent.EventID.Equals("IntroBetterGeo_1"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                        __result.EventLeader = Helper.CreateSpriteFromImageFile("BG_alistair_small.png");
                    }
                    if (geoEvent.EventID.Equals("IntroBetterGeo_0"))
                    {
                        __result.EventBackground = Helper.CreateSpriteFromImageFile("BG_Intro_0.jpg");
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
