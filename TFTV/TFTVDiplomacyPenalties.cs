using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVDiplomacyPenalties
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
       


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
                       && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedAnu") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN2", geoscapeEventContext);
                    }
                    else if (newValue == 49)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN4", geoscapeEventContext);

                    }
                    else if (newValue == 74)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedAnu", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_AN6", geoscapeEventContext);

                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.NewJerichoFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedNewJericho") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ1", geoscapeEventContext);
                    }
                    else if (newValue == 49)
                    {

                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ2", geoscapeEventContext);

                    }
                    else if (newValue == 74)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedNewJericho", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_NJ3", geoscapeEventContext);
                    }
                }
                else if (faction.GetParticipatingFaction() == faction.GeoLevel.SynedrionFaction
                      && targetFaction == faction.GeoLevel.PhoenixFaction && faction.GeoLevel.EventSystem.GetVariable("RefusedSynedrion") == 1)
                {
                    if (newValue == 24)
                    {
                        faction.GeoLevel.EventSystem.SetVariable("RefusedSynedrion", 0);
                        faction.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_SY1", geoscapeEventContext);
                    }

                    else if (newValue == 74)
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