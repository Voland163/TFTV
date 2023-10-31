using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;

namespace TFTV
{
    internal class TFTVHarmonyGeoscape
    {




        [HarmonyPatch(typeof(GeoscapeEvent), "PostSerializationInit")]
        public static class GeoscapeEventSystem_PostSerializationInit_patch
        {
            public static void Prefix(GeoscapeEvent __instance)//GeoscapeEventData @event)
            {
                try
                {
                    TFTVDiplomacyPenalties.ImplementDiplomaticPenalties(null, __instance);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }


        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnEventTriggered")]
        public static class GeoscapeEventSystem_OnGeoscapeEvent_patch
        {
            public static bool Prefix(GeoscapeEventData @event)// @event)
            {
                try
                {
                    if (TFTVNewGameOptions.NoSecondChances && @event.EventID.Contains("FAIL") && @event.EventID!="PROG_FS1_FAIL")
                    {

                        TFTVLogger.Always($"Canceling event {@event.EventID} because No Second Chances is in effect!");

                        return false;
                    }

                    TFTVDiplomacyPenalties.ImplementDiplomaticPenalties(@event, null);


                    return true;

                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


        }

        [HarmonyPatch(typeof(GeoscapeEvent), "CompleteEvent")]

        public static class GeoscapeEvent_CompleteEvent_patch
        {
            public static void Postfix(GeoscapeEvent __instance)
            {
                try
                {
                    TFTVDiplomacyPenalties.RestoreStateDiplomaticPenalties(__instance);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }



        [HarmonyPatch(typeof(GeoSite), "CreateHavenDefenseMission")]
        public static class GeoSite_CreateHavenDefenseMission_RevealHD_Patch
        {

            public static void Postfix(GeoSite __instance, ref HavenAttacker attacker)
            {
                try
                {
                    TFTVCommonMethods.RevealHavenUnderAttack(__instance, __instance.GeoLevel);
                    TFTVVoidOmens.ImplementStrongerHavenDefenseVO(ref attacker);

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

            public static void Postfix(GeoFaction __instance, PartyDiplomacy.Relation relation, int newValue)

            {
                try
                {

                    TFTVDiplomacyPenalties.CheckPostponedFactionMissions(__instance, relation, newValue);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }


    }
}
