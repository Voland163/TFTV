using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.View.ViewControllers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV
{
    internal class TFTVHarmonyTactical
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        [HarmonyPatch(typeof(TacticalFaction), "RequestEndTurn")]

        public static class TFTV_TacticalFactionn_RequestEndTurn_patch
        {
            public static void Postfix(TacticalFaction __instance)
            {
                try
                {
                    TFTVHumanEnemies.ImplementStartingVolleyHumanEnemiesTactic(__instance);
                    TFTVUmbra.ImplementCallReinforcementsTBTV(__instance);
                    TFTVPalaceMission.PalaceReinforcements(__instance);
                    TFTVPalaceMission.SpawnMistToHideReceptacleBody(__instance);
                    TFTVBaseDefenseTactical.ImplementBaseDefenseVsAliensPreAITurn(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalFaction), "GetSortedAIActors")]

        public static class TFTV_TacticalFactionn_GetSortedAIActors_patch
        {

            public static void Postfix(List<TacticalActor> __result, TacticalFaction __instance)
            {
                try
                {
                    if (__result.Count > 0)
                    {
                        TFTVArtOfCrab.SortOutAITurnOrder(__result);
                        __result.Sort((TacticalActor a, TacticalActor b) => a.AIActor.TurnOrderPriority - b.AIActor.TurnOrderPriority);
                        TFTVHumanEnemies.ApplyTactic(__instance.TacticalLevel);
                        TFTVLogger.Always("TFTV: Art of Crab: Sorted AI Turn Order");
                      //  TFTVPalaceMission.LogEnemyAP(__instance);
                    }

                    TFTVBaseDefenseTactical.ImplementBaseDefenseVsAliensPostAISortingOut(__instance);
                  //  TFTVPalaceMission.PalaceReinforcements(__instance);

                    //   TFTVPalaceMission.PalaceTacticalNewTurn(__instance);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(StatusComponent), "AddStatus")]
        public static class TFTV_StatusComponent_AddStatus_patch
        {
            public static void Postfix(StatusComponent __instance, Status status)
            {
                try
                {
                  //  TFTVLogger.Always($"the status is {status.Def.name}");

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    TFTVBaseDefenseTactical.BaseDefenseConsoleActivated(__instance, status, controller);

                    TFTVPalaceMission.PalaceConsoleActivated(__instance, status, controller);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

     



        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TFTV_TacticalLevelController_ActorEnteredPlay_Patch
        {
            public static void Prefix(TacticalActorBase actor)
            {
                try
                {
                    TFTVPalaceMission.RemoveVoiceFromSpecialCharactersByRemovingHumanTagOnEntryIntoPlay(actor);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {
                    TFTVDeliriumPerks.ImplementDeliriumPerks(actor, __instance);
                    TFTVExperimental.AddReinforcementTagToImplementNoDropsOption(actor, __instance);
                    TFTVHumanEnemies.GiveRankAndNameToHumaoidEnemy(actor, __instance);
                    TFTVUmbra.UmbraEverywhereVoidOmenImplementation(actor, __instance);
                    TFTVBaseDefenseTactical.AddScatterObjectiveTagForBaseDefense(actor, __instance);
                    TFTVSpecialDifficulties.AddSpecialDifficultiesBuffsAndVulnerabilities(actor, __instance);
                    TFTVPalaceMission.TryToTurnIntoRevenant(actor, __instance);
                    TFTVPalaceMission.CheckFinalMissionWinConditionWhereDeployingItem(actor, __instance);
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(SquadMemberScrollerController), "SetupProperPortrait")]
        public static class TFTV_SquadMemberScrollerController_SetupProperPortrait_Patch
        {
            public static void Prefix(TacticalActor actor)
            {
                try
                {
                  TFTVPalaceMission.ForceSpecialCharacterPortraitInSetupProperPortrait(actor);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

       

        [HarmonyPatch(typeof(TacticalFactionVision), "OnFactionStartTurn")]
        public static class TFTV_TacticalFactionVision_OnFactionStartTurn_Patch
        {
            public static void Postfix(TacticalFactionVision __instance)
            {
                try
                {
                    TFTVAncients.AncientsNewTurnCheck(__instance.Faction);
                    TFTVPalaceMission.PalaceTacticalNewTurn(__instance.Faction);
                    TFTVBaseDefenseTactical.PhoenixBaseDefenseVSAliensTurnStart(__instance.Faction.TacticalLevel, __instance.Faction);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TFTV_TacticalLevelController_ActorDied_Patch
        {
            public static void Prefix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    TFTVInfestationStory.CreateOutroInfestation(__instance, deathReport);
                    TFTVUmbra.TouchByTheVoidDeath(deathReport);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    TFTVRevenant.RecordPhoenixDeadForRevenantsAndOsiris(deathReport, __instance);
                    TFTVRevenant.RevenantKilled(deathReport, __instance);
                    TFTVAncients.AncientKilled(__instance, deathReport);
                    TFTVHumanEnemies.HumanEnemiesBloodRushTactic(deathReport);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacMission), "InitDeployZones")]

        public static class TFTV_TacMission_InitDeployZones_patch
        {
            public static void Postfix(TacMission __instance)
            {
                try
                {
                    TFTVBaseDefenseTactical.InitDeployZonesForBaseDefenseVsAliens(__instance.TacticalLevel);
                    TFTVPalaceMission.InitDeployZonesForPalaceMission(__instance.TacticalLevel);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

        public static class TFTV_TacticalActor_OnAbilityExecuteFinished_patch
        {
            public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
            {
                try
                {
                    TFTVBallistics.ClearBallisticInfoOnAbilityExecuteFinished();
                    TFTVArtOfCrab.ForceScyllaToUseCannonsAfterUsingHeadAttack(ability, __instance, parameter);
                    TFTVAncients.AddMindCrushEffectToCyclposScreamAndRedeployHopliteShieldsAfterMassShootAttack(ability, __instance, parameter);
                    TFTVFire.ActivateFireQuencherAbility();
                    TFTVBaseDefenseTactical.RemoveScatterRemainingAttackersTagFromEnemiesWithParasychosis(ability, parameter);
                    TFTVPalaceMission.CheckFinalMissionWinConditionForExalted(ability);
                    TFTVPalaceMission.CheckIfPlayerCloseToGate(__instance);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


    }
}
