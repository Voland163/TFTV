using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TFTV
{
    internal class TFTVHarmonyGeoscape
    {
        //Invokes changes to MissionObjectives always, and if base defense vs aliens changes deployment and hint
        [HarmonyPatch(typeof(GeoMission), "ModifyMissionData")]
        public static class GeoMission_ModifyMissionData_patch
        {
            public static void Postfix(GeoMission __instance, TacMissionData missionData)
            {
                try
                {
                    TFTVLogger.Always($"GeoMission.ModifyMissionData invoked.");

                    TFTVCapturePandorans.CheckCaptureCapability(__instance);
                    TFTVBaseDefenseTactical.Objectives.ModifyMissionDataBaseDefense(__instance, missionData);
                    TFTVVoidOmens.ModifyVoidOmenTacticalObjectives(missionData.MissionType);
                    TFTVCapturePandorans.ModifyCapturePandoransTacticalObjectives(missionData.MissionType);
                    TFTVBaseDefenseTactical.Objectives.ModifyBaseDefenseTacticalObjectives(missionData.MissionType);

                    // __instance.GameController.SaveManager.IsSaveEnabled = true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }


        [HarmonyPatch(typeof(GeoMission), "ApplyOutcomes")]
        public static class GeoMission_ModifyMissionData_Patch
        {

            public static void Postfix(GeoMission __instance, FactionResult viewerFactionResult)
            {
                try
                {
                    TFTVAncientsGeo.AncientSites.OnTakingAncientSiteFromAncients(__instance);
                    TFTVAncientsGeo.DefendCyclopsStoryMission.OnCompletingDefendCyclopsMission(__instance, viewerFactionResult);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoscapeEvent), "PostSerializationInit")]
        public static class GeoscapeEvent_PostSerializationInit_patch
        {
            public static void Prefix(GeoscapeEvent __instance)//GeoscapeEventData @event)
            {
                try
                {
                    /*   TFTVLogger.Always($"trying to load event");
                       TFTVLogger.Always($"event is {__instance.EventID} ");

                       if (__instance.EventID.Contains("VoidOmen")) 
                       {
                           GeoLevelController component = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                           TFTVODIandVoidOmenRoll.GenerateVoidOmenEvent(component, TFTVODIandVoidOmenRoll.GenerateReportData(component), true, "", 15);   
                       }*/

                    TFTVDiplomacyPenalties.ImplementDiplomaticPenalties(null, __instance);


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        private static readonly List<string> failEventsNoSecondChances = new List<string>()
        {
"PROG_AN0_FAIL","PROG_AN2_FAIL","PROG_AN4_FAIL","PROG_AN6_FAIL1","PROG_AN6_FAIL2","PROG_LE0_FAIL","PROG_KE2_FAIL","PROG_NJ0_FAIL","PROG_NJ1_FAIL1",
"PROG_NJ2_FAIL","PROG_PU12_FAIL","PROG_PU14_FAIL","PROG_PU8_FAIL","PROG_PX1_FAIL","PROG_PX13_FAIL","PROG_PX14_FAIL","PROG_PX15_FAIL","PROG_SY0_FAIL",
"PROG_SY1_FAIL1","PROG_SY1_FAIL2", "PROG_SY2_FAIL", "PROG_SY3_FAIL","PROG_SY4_FAIL1","PROG_SY4_FAIL2"
        };

        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnEventTriggered")]
        public static class GeoscapeEventSystem_OnGeoscapeEvent_patch
        {
            public static bool Prefix(GeoscapeEventData @event, GeoscapeEventSystem __instance)// @event)
            {
                try
                {
                    //PROG_AN0_FAIL_GeoscapeEventDef
                    //PROG_AN2_FAIL_GeoscapeEventDef
                    //PROG_AN4_FAIL_GeoscapeEventDef
                    //PROG_AN6_FAIL1_GeoscapeEventDef
                    //PROG_AN6_FAIL2_GeoscapeEventDef

                    //PROG_LE0_FAIL_GeoscapeEventDef
                    //PROG_KE2_FAIL_GeoscapeEventDef
                    //PROG_NJ0_FAIL_GeoscapeEventDef
                    //PROG_NJ1_FAIL_GeoscapeEventDef
                    //PROG_NJ2_FAIL_GeoscapeEventDef

                    //PROG_PU12_FAIL_GeoscapeEventDef //has a timer
                    //PROG_PU14_FAIL_GeoscapeEventDef

                    //PROG_PU8_FAIL_GeoscapeEventDef //has a timer

                    //PROG_PX1_FAIL_GeoscapeEventDef
                    //PROG_PU14_FAIL_GeoscapeEventDef
                    //PROG_PX13_FAIL_GeoscapeEventDef
                    //PROG_PX14_FAIL_GeoscapeEventDef
                    //PROG_PX15_FAIL_GeoscapeEventDef

                    //PROG_SY0_FAIL_GeoscapeEventDef
                    //PROG_SY1_FAIL1_GeoscapeEventDef
                    //PROG_SY1_FAIL2_GeoscapeEventDef
                    //PROG_SY2_FAIL_GeoscapeEventDef
                    //PROG_SY3_FAIL_GeoscapeEventDef
                    //PROG_SY4_FAIL1_GeoscapeEventDef
                    //PROG_SY4_FAIL2_GeoscapeEventDef




                    /*  if (TFTVNewGameOptions.NoSecondChances && failEventsNoSecondChances.Contains(@event.EventID))
                      {
                          TFTVLogger.Always($"Canceling event {@event.EventID} because No Second Chances is in effect!");

                          return false;
                      }*/

                    TFTVDiplomacyPenalties.ImplementDiplomaticPenalties(@event, null);


                    return true; //TFTVInfestation.ScienceOfMadness.CancelProgFS3IfTrappedInMistAlreadyTriggered(@event, __instance);

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
            public static void Postfix(GeoscapeEvent __instance, GeoFaction faction, GeoEventChoice choice)
            {
                try
                {
                    TFTVLogger.Always($"GeoscapeEvent.CompleteEvent for {__instance.EventID}");

                    TFTVDiplomacyPenalties.RestoreStateDiplomaticPenalties(__instance);
                    TFTVBaseDefenseGeoscape.InitAttack.ContainmentBreach.CheckPurgeContainmentEvent(choice, __instance);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }


        [HarmonyPatch(typeof(GeoPhoenixFaction), "KillCapturedUnit")]
        public static class GeoPhoenixFaction_KillCapturedUnit_patch
        {
            public static void Postfix(GeoPhoenixFaction __instance, GeoUnitDescriptor unit)
            {
                try
                {
                    //Removes from the list 
                    TFTVBaseDefenseGeoscape.InitAttack.ContainmentBreach.CheckOnCaptiveDestroyed(unit);
                    TFTVResearch.Vivisections.ResetResearch(unit, __instance);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }
        }


        [HarmonyPatch(typeof(UIStateRosterAliens), "OnDismantleForFood")]
        public static class UIStateRosterAliens_OnDismantleForFood_CapturePandorans_Patch
        {
            public static bool Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;


                    UIModuleActorCycle actorCycleModule = controller.View.GeoscapeModules.ActorCycleModule;
                    GeoUnitDescriptor unit = actorCycleModule.GetCurrent<GeoUnitDescriptor>();
                    UIModuleGeneralPersonelRoster geoRosterModule = controller.View.GeoscapeModules.GeneralPersonelRosterModule;

                    string warningText = "";

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
                    {
                        warningText = TFTVCapturePandoransGeoscape.LimitedCapturingFoodHarvesting(__instance);
                    }

                    if (TFTVResearch.Vivisections.IsPandoranVivisectionRelevant(unit, phoenixFaction) && TFTVResearch.Vivisections.CountPandoranType(unit, phoenixFaction) == 1)
                    {
                        warningText = TFTVCommonMethods.ConvertKeyToString("KEY_VIVISECTION_WARNING");
                    }

                    if (warningText == "")
                    {
                        return true;
                    }

                    GameUtl.GetMessageBox().ShowSimplePrompt(warningText, MessageBoxIcon.Warning, MessageBoxButtons.YesNo, (msgResult) =>
                    {
                        // Invoke the callback method with the chosen result
                        MethodInfo methodInfo = typeof(UIStateRosterAliens).GetMethod("OnDismantpleForFoodDialogCallback", BindingFlags.Instance | BindingFlags.NonPublic);
                        methodInfo.Invoke(__instance, new object[] { msgResult });

                    });

                    return false;
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
