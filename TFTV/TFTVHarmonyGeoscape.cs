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
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TFTV
{
    internal class TFTVHarmonyGeoscape
    {

        [HarmonyPatch(typeof(DiplomaticGeoFactionObjective), "GetRelatedActors")]
        internal static class TFTV_DiplomaticGeoFactionObjective_GetRelatedActors_ExperimentPatch
        {
            public static void Postfix(DiplomaticGeoFactionObjective __instance, ref IEnumerable<GeoActor> __result, ref List<GeoSite> ____assignedSites)
            {
                try
                {
                    TFTVBaseDefenseGeoscape.GeoObjective.AddUnderAttackBaseToObjective(__instance, ref __result, ref ____assignedSites);
                    TFTVInfestation.ImplementLocateInfestedHavenOnObjectiveClick(__instance, ref __result, ref ____assignedSites);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionHourly")]
        public static class GeoAlienFaction_UpdateFactionHourly_CapturePandorans_Patch
        {

            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                  //  TFTVLogger.Always($"running UpdateFactionHourly {__instance.GeoLevel.Timing.Now}");

                    TFTVCapturePandoransGeoscape.LimitedHarvestingHourlyActions(__instance.GeoLevel);
                    TFTVBaseDefenseGeoscape.InitAttack.ContainmentBreach.HourlyCheckContainmentBreachDuringBaseDefense(__instance.GeoLevel);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



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
                    TFTVBaseDefenseGeoscape.Deployment.ModifyMissionDataBaseDefense(__instance, missionData);
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
            public static void Prefix(GeoscapeEventData @event, GeoscapeEventSystem __instance)// @event)
            {
                try
                {
                   
                    TFTVDiplomacyPenalties.ImplementDiplomaticPenalties(@event, null);
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
        public static class GeoFaction_OnDiplomacyChanged_patch
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
