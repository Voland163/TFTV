using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();
        private static List<string> ExcludedEventsDiplomacyPenalty = new List<string> 
        { 
            "PROG_PU4_WIN", "PROG_SY7", "PROG_SY8", "PROG_AN3", "PROG_AN5", "PROG_NJ7", "PROG_NJ8",
        "PROG_AN2", "PROG_NJ1", "PROG_SY1", "PROG_AN4", "PROG_NJ2", "PROG_SY3_WIN", "PROG_AN6", "PROG_NJ3", "PROG_SY4_T", "PROG_SY4_P",
        "PROG_PU2_CHOICE3EVENT","PROG_PU4_WIN","PROG_PU2_CHOICE2EVENT","PROG_PU12WIN2", "PROG_PU12NewNJOption","PROG_LE1_WIN",
        "Anu_Pissed1", "Anu_Pissed2", "NJ_Pissed1", "NJ_Pissed2"
        };


        [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]

        public static class GeoEventChoiceOutcome_GenerateFactionReward_patch
        {
            private static readonly GeoFactionDef PhoenixFaction = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");

            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.DiplomaticPenalties;
            }

            public static void Prefix (GeoEventChoiceOutcome __instance, string eventID)
            {
                try 
                {
                    if (!ExcludedEventsDiplomacyPenalty.Contains(eventID) && __instance.Diplomacy.Count > 0)
                    {
                        for (int i = 0; i < __instance.Diplomacy.Count; i++)
                        {
                            if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value <= 0)
                            {
                                TFTVLogger.Always("The event is " + eventID + ". Original diplo penalty is " + __instance.Diplomacy[i].Value);
                                OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                diplomacyChange.Value *= 2;
                                __instance.Diplomacy[i] = diplomacyChange;
                                TFTVLogger.Always("GeoEvent " + eventID + " diplomacy change value is " + diplomacyChange.Value);
                            }
                        }
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }   
        }



        [HarmonyPatch(typeof(GeoFaction), "OnDiplomacyChanged")]
        public static class GeoBehemothActor_OnDiplomacyChanged_patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.DiplomaticPenalties;
            }

            public static void Postfix(GeoFaction __instance, PartyDiplomacy.Relation relation, int newValue)

            {
                try
                {
                     CheckPostponedFactionMissions(__instance, relation, newValue);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        public static void CheckPostponedFactionMissions(GeoFaction faction, PartyDiplomacy.Relation relation, int newValue)
        {
            try
            {
                GeoFaction targetFaction = faction.GeoLevel.GetFaction((PPFactionDef)relation.WithParty);
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(targetFaction, faction.GeoLevel.ViewerFaction);

                if (faction.GetParticipatingFaction() == faction.GeoLevel.AnuFaction
                       && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu")==1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 2)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);

                    }
                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 3)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);

                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 2)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);

                    }
                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 3)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") > 0)
                {
                    if (newValue == 24 && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 1)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }

                    else if (newValue == 74 && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 3)
                    {
                        if (faction.GeoLevel.EventSystem.GetVariable("Polyphonic") > faction.GeoLevel.EventSystem.GetVariable("Terraformers"))
                        {

                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_P", geoscapeEventContext);
                        }
                        else
                        {
                            faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                            faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY4_T", geoscapeEventContext);

                        }
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