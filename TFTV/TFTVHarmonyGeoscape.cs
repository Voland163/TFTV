using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVHarmonyGeoscape
    {

        [HarmonyPatch(typeof(GeoEventChoiceOutcome), nameof(GeoEventChoiceOutcome.GenerateFactionReward))]
        public static class GeoEventChoiceOutcome_GenerateFactionReward_Postfix
        {           
            public static void Postfix(GeoEventChoiceOutcome __instance, GeoscapeEventContext context, string eventID, GeoFaction faction, ref GeoFactionReward __result, in ResourcePack __state)
            {
                TFTVVanillaFixes.Geoscape.ApplyGenerateFactionReward(__instance, eventID, ref __result);
                TFTVAircraftReworkMain.MarketPlace.ApplyGenerateFactionReward(__instance, faction);
            }
        }

        [HarmonyPatch(typeof(UIStateRosterAliens), "EnterState")]
        public static class UIStateRosterAliens_EnterState_Postfix
        {
            public static void Postfix(UIStateRosterAliens __instance)
            {
                TFTVBackgrounds.ContainmentScreen.GetInfoAboutAlien();
                TFTVCapturePandoransGeoscape.UpdateResourceInfo(__instance);
            }
        }


        [HarmonyPatch(typeof(GeoLevelController), "DailyUpdate")]
        public static class GeoLevelController_DailyUpdate_Postfix
        {
            public static void Postfix(GeoLevelController __instance)
            {
                TFTVVanillaFixes.Geoscape.ApplyDailyUpdate(__instance);
                TFTVBaseRework.PersonnelData.DailyUpdatePersonnelPool(__instance);
                TFTVBaseRework.TrainingFacilityRework.DailyUpdateTraining(__instance);
                TFTVBaseRework.TrainingFacilityRework.DailyUpdateTrainingDeferredFallback(__instance);
            }
        }




        [HarmonyPatch(typeof(GeoPhoenixFaction), "AddRecruitToContainerFinal")] //VERIFIED  
        internal static class TFTV_GeoPhoenixFaction_AddRecruitToContainerFinal_patch
        {
            public static void Prefix(ref GeoCharacter recruit)
            {
                try
                {
                   TFTVUI.Personnel.Mutoids.AddMutoidToNewRecruitGeoPhoenixFactionAddRecruitToContainerFinal(ref recruit);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void Postfix(GeoPhoenixFaction __instance, GeoCharacter recruit)
            {
                try
                {
                    TFTVProjectOsiris.AddProjectOsirisRecruitGeoPhoenixFactionAddRecruitToContainerFinal(__instance, recruit);
                    TFTVBaseRework.TrainingFacilityRework.TryApplyDeferredTrainingStats(recruit);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }



        [HarmonyPatch(typeof(GeoMission), "ApplyMissionResults")] //VERIFIED
        public static class GeoMission_ApplyMissionResults_patch
        {
            public static void Prefix()
            {
                try
                {
                    TFTVChangesToDLC5.TFTVMercenaries.Tactical.AdjustMercItemsToBeRecoverable(false);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void Postfix(GeoMission __instance, TacMissionResult result, GeoSquad squad)
            {
                try
                {
                    TFTVChangesToDLC5.TFTVMercenaries.Tactical.AdjustMercItemsToBeRecoverable(true);
                    TFTVDelirium.RemoveDeliriumPerksGeoMissionApplyMissionResults(__instance, result, squad);
                    AircraftReworkMaintenance.AfterLosingAmbushGeoMissionApplyMissionResults(__instance, result, squad);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(GeoAlienFaction), nameof(GeoAlienFaction.UpdateFactionDaily))]
        public static class PhoenixStatisticsManager_UpdateGeoscapeStats_AnuPissedAtBionics_Patch
        {
            public static void Postfix(GeoAlienFaction __instance, int ____evolutionProgress)
            {
                try
                {
                    TFTVAugmentations.AnuReactionToBionicsGeoAlienFactionUpdateFactionDaily(__instance);
                    TFTVAugmentations.NJReactionToMutationsGeoAlienFactionUpdateFactionDaily(__instance);
                    TFTVODIandVoidOmenRoll.ApplyNewODIGeoAlienFactionFactionDailyUpdate(__instance, ____evolutionProgress);
                    TFTVPandoranProgress.AddPandoranEvolutionPointsGeoAlienFactionUpdateFactionDaily(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(GeoVehicle), "get_MaxCharacterSpace")]
        internal static class BG_GeoVehicle_get_MaxCharacterSpace_patch
        {
            public static void Postfix(GeoVehicle __instance, ref int __result)
            {
                try
                {
                    AircraftReworkGeoscape.PassengerModules.AdjustMaxCharacterSpacePassengerModules(__instance, ref __result);
                    // TFTVLogger.Always($"get_MaxCharacterSpace postfix executed for {__instance.Name} __result: {__result}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), "ReplaceEquipments")]
        internal static class BG_GeoVehicle_ReplaceEquipments_RemoveExcessPassengers_patch
        {
            public static void Postfix(GeoVehicle __instance)
            {
                try
                {
                    AircraftReworkGeoscape.Scanning.CheckAircraftScannerAbility(__instance);
                    AircraftReworkGeoscape.PassengerModules.CheckAircraftNewPassengerCapacity(__instance);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




        [HarmonyPatch(typeof(GeoVehicle), "GetAircraftInfo")]
        internal static class GeoVehicle_GetAircraftInfo_patch
        {
            public static void Postfix(GeoVehicle __instance, ref AircraftInfoData __result)
            {
                try
                {
                    AircraftReworkGeoscape.PassengerModules.AdjustAircraftInfoPassengerModules(__instance, ref __result);
                    //  TFTVLogger.Always($"GetAircraftInfo postfix executed for {__instance.Name} __result.CurrentCrew: {__result.CurrentCrew} __result.MaxCrew: {__result.MaxCrew}");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



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
                    TFTVChangesToDLC4Events.SoldierReachesFiveDelirium(__instance.GeoLevel);
                    AircraftReworkMaintenance.MaintenanceToll(__instance.GeoLevel);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(GeoMission), "Launch")]
        public static class GeoMission_Launch_InfestationStory_Patch
        {
            public static void Postfix(GeoMission __instance, GeoSquad squad)
            {
                try
                {
                    TFTVInfestation.StoryFirstInfestedHaven.InfestationStoryMission(__instance, squad);
                    TFTVNJQuestline.IntroMission.Geoscape.RecordHavenName(__instance);
                    AircraftReworkTacticalModules.CheckTacticallyRelevantModulesOnVehicle(__instance.GetLocalAircraft(squad), __instance);

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
                    TFTVBaseDefenseTactical.Objectives.ModifyBaseDefenseTacticalObjectives(missionData.MissionType);
                    TFTVUI.Tactical.SecondaryObjectivesTactical.PopulateAvailableObjectives(__instance.Site.GeoLevel);
                    TFTVUI.Tactical.SecondaryObjectivesTactical.AddAllAvailableSecondaryObjectivesToMission(missionData.MissionType);

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
                    TFTVEconomyExploitsFixes.ClearTimerNoSecondChances(__instance);

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


        public static TacticalAbilityDef StarvedAbility;
        /// <summary>
        /// Adds/removes starved ability, to let player know when soldiers are starving.
        /// </summary>
        [HarmonyPatch(typeof(GeoPhoenixFaction), "FeedSoldiers")]
        public static class TFTV_GeoPhoenixFactione_AddHunger_patch
        {

            public static void Postfix(GeoPhoenixFaction __instance)
            {
                try
                {
                    bool starvingCharacter = false;

                    foreach (GeoCharacter geoCharacter in __instance.Characters.Where(c => c.Fatigue != null))
                    {
                        if (geoCharacter.Fatigue.Hunger > 0 && !geoCharacter.GetTacticalAbilities().Contains(StarvedAbility))
                        {
                            TFTVLogger.Always($"Adding {StarvedAbility.name} to {geoCharacter.DisplayName}");
                            geoCharacter.Progression.AddAbility(StarvedAbility);
                            starvingCharacter = true;
                        }
                        if (geoCharacter.Fatigue.Hunger <= 0 && geoCharacter.GetTacticalAbilities().Contains(StarvedAbility))
                        {
                            List<TacticalAbilityDef> abilities = Traverse.Create(geoCharacter.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();
                            TFTVLogger.Always($"Removing {StarvedAbility.name} from {geoCharacter.DisplayName}");
                            abilities.Remove(StarvedAbility);
                        }
                    }

                    if (starvingCharacter)
                    {
                        __instance.GeoLevel.View.RequestGamePause();
                        string messageText = TFTVCommonMethods.ConvertKeyToString("TFTV_STARVATION_MECHANIC_DESCRIPTION");
                        // $"One or more of your operatives is now STARVING. This happens when your food stocks are insufficient to feed your operatives. A STARVING operative has reduced STRENGTH and WILLPOWER in Tactical Missions. Each day the character spends without eating will worsen the condition, eventually resulting in DEATH. The condition will improve until it disappears every day the character is fed.";

                        GameUtl.GetMessageBox().ShowSimplePrompt(messageText, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(AlienBaseOutcomeDataBind), "ModalShowHandler")]
        public static class TFTV_AlienBaseOutcomeDataBind_ModalShowHandler_patch
        {

            public static void Postfix(AlienBaseOutcomeDataBind __instance, UIModal modal, bool ____shown, UIModal ____modal)
            {
                try
                {


                    __instance.Rewards.transform.gameObject.SetActive(true);

                    Transform rewardContainer = __instance.Rewards.GetComponentInChildren<Transform>().Find("Rewards");

                    rewardContainer.gameObject.SetActive(true);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

    }
}
