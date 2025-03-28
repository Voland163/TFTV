using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.UI;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewControllers.SquadMemberScrollerController;



namespace TFTV
{
    internal class TFTVHarmonyTactical
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //   private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;




        [HarmonyPatch(typeof(TacticalVoxelMatrix), "StartTurn")]
        public static class TacticalVoxelMatrix_StartTurn_Patch
        {
            public static void Postfix(TacticalVoxelMatrix __instance, TacticalVoxel[] ____voxels)
            {

                try
                {
                    TFTVAircraftRework.Modules.Tactical.MistRepeller.ImplementMistRepellerTurnStart(__instance, ____voxels);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }



        [HarmonyPatch(typeof(AddAttackBoostStatus), "AbilityExecutedHandler")]
        public static class TFTV_AddAttackBoostStatus_AbilityExecutedHandler
        {
            public static bool Prefix(AddAttackBoostStatus __instance, TacticalAbility ability, ref int ____attacksBoosted)
            {
                try
                {
                    ApplyStatusAbilityDef killerInstinctAbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("KillerInstinct_AbiltyDef");

                  //  TFTVLogger.Always($"got here for {__instance.AddAttackBoostStatusDef.name}, tactical ability: {ability.TacticalAbilityDef.name}");

                    if (__instance.AddAttackBoostStatusDef!= killerInstinctAbilityDef.StatusDef)
                    {
                        return true;
                    }

                    

                    SkillTagDef[] skillTagCullFilter = __instance.AddAttackBoostStatusDef.SkillTagCullFilter;
                    foreach (SkillTagDef value in skillTagCullFilter)
                    {
                        if (ability.TacticalAbilityDef.SkillTags.Contains(value))
                        {
                            return false;
                        }
                    }

                    if (__instance.AddAttackBoostStatusDef.NumberOfAttacks < 0)
                    {
                        return false;
                    }

                    ____attacksBoosted++;
                    if (____attacksBoosted < __instance.AddAttackBoostStatusDef.NumberOfAttacks)
                    {
                        return false;
                    }

                    __instance.RequestUnapply(__instance.TacticalActor.Status);
                    TacStatusDef[] additionalStatusesToApply = __instance.AddAttackBoostStatusDef.AdditionalStatusesToApply;
                    foreach (TacStatusDef def in additionalStatusesToApply)
                    {
                        TacStatus status = __instance.TacticalActor.Status.GetStatus<TacStatus>(def);
                        if (status != null && __instance.AddAttackBoostStatusDef == status.Source as AddAttackBoostStatusDef)
                        {
                            status.RequestUnapply(__instance.TacticalActor.Status);
                        }
                    }

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalActorViewBase), "DoCameraChase")]
        public static class TFTV_TacticalActorViewBase_DoCameraChase
        {
            public static bool Prefix(TacticalActorViewBase __instance, bool chaseTransform, bool lockInput, bool instant, bool chaseOnlyOutsideFrame)
            {
                try
                {


                    if (TFTVVanillaFixes.UI.CheckIfEnemyActorTargeted() 
                        && TFTVAircraftRework.Modules.Tactical.GroundAttackWeapon.CheckForGroundAttackWeaponExplosions(__instance.ActorBase.TacticalLevel))
                    {
                        return true;
                    }

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        /* [HarmonyPatch(typeof(CorruptionStatus), "OnApply")]
         public static class CorruptionStatusPatch
         {
             // Postfix patch – after the original ActivateAbility runs.
             static bool Prefix(CorruptionStatus __instance, StatusComponent statusComponent)
             {
                 try
                 {
                     TacticalActor tacticalActor = __instance.TacticalActor;

                     if (tacticalActor == null)
                     {
                         return true;
                     }

                     if (tacticalActor.BodyState.GetArmourItems().All(a => a.GameTags.Contains(Shared.SharedGameTags.BionicalTag)))
                     {
                         TFTVLogger.Always($"not applying Delirium to {tacticalActor.DisplayName} because fully bionic");
                         return false; 
                     }


                     return true;
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/




        [HarmonyPatch(typeof(IdleAbility), "OnPlayingActionEnd")]
        public static class IdleAbility_OnPlayingActionEnd_patch
        {
            //  

            public static void Postfix(IdleAbility __instance)
            {
                try
                {

                    TFTVTimeScaling.EnsureStandardTimeScaleForIdleActor(__instance);
                    TFTVUITactical.ODITactical.HideODITooltipFailSafe();
                    // MissionQuips.RunPhoenixOverlayQuip(__instance.TacticalActor, "IdleAbilityActionEnd, I finished doing nothing!");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(SpottedTargetsElement), "SetActorClassIcon")]
        public static class SpottedTargetsElement_SetActorClassIcon_patch
        {


            public static void Postfix(SpottedTargetsElement __instance, GameObject obj, TacticalActorBase target)
            {
                try
                {
                    TFTVUITactical.ODITactical.ManageTBTVIconToSpottedEnemies(__instance, obj, target);
                    TFTVUITactical.Enemies.ManageRankIconToSpottedEnemies(__instance, obj, target);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        [HarmonyPatch(typeof(UIModuleObjectives), "Init")]
        public static class UIModuleObjectives_Init_patch
        {

            public static void Postfix(UIModuleObjectives __instance, TacticalViewContext Context)
            {
                try
                {
                    TFTVUITactical.CachePuristaSemiboldFont(__instance);
                    TFTVUITactical.ODITactical.CreateODITacticalWidget(__instance);
                    TFTVUITactical.CaptureTacticalWidget.CreateCaptureTacticalWidget(__instance);
                    TFTVUITactical.Enemies.ActivateOrAdjustLeaderWidgets();
                    //  MissionQuips.RunPhoenixOverlayQuip(Context.LevelController.GetFactionByCommandName("PX").TacticalActors.FirstOrDefault(), "oBJECTIVes Module Inited!");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalAbility), "GetTargetActors", new Type[] { typeof(TacticalTargetData), typeof(TacticalActorBase), typeof(Vector3) })]
        public static class TacticalAbility_GetTargetActors_Patch
        {
            public static void Postfix(TacticalAbility __instance, ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor,
                TacticalTargetData targetData, Vector3 sourcePosition)
            {
                try
                {
                    TFTVTouchedByTheVoid.Umbra.UmbraTactical.ImplementUmbraTargeting(ref __result, sourceActor);
                    TFTVVanillaFixes.Tactical.FixMeleeTooHighAttack(__instance, ref __result, sourceActor, targetData, sourcePosition);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalFaction), "RequestEndTurn")]
        public static class TFTV_TacticalFactionn_RequestEndTurn_patch
        {
            public static void Postfix(TacticalFaction __instance)
            {
                try
                {
                    // TFTVHumanEnemies.ImplementStartingVolleyHumanEnemiesTactic(__instance);
                    TFTVTouchedByTheVoid.TBTVCallReinforcements.ImplementCallReinforcementsTBTV(__instance);
                    TFTVPalaceMission.EnemyDeployments.Reinforcements.PalaceReinforcements(__instance);
                    TFTVPalaceMission.YuggothDefeat.SpawnMistToHideReceptacleBody(__instance);
                    TFTVBaseDefenseTactical.PandoranTurn.ImplementBaseDefenseVsAliensPreAITurn(__instance);
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
                        TFTVArtOfCrab.TurnOrder.SortOutAITurnOrder(__result);
                        __result.Sort((TacticalActor a, TacticalActor b) => a.AIActor.TurnOrderPriority - b.AIActor.TurnOrderPriority);
                        TFTVHumanEnemies.ApplyTactic(__instance.TacticalLevel);
                        TFTVLogger.Always("TFTV: Art of Crab: Sorted AI Turn Order");
                        //  TFTVPalaceMission.LogEnemyAP(__instance);
                    }

                    TFTVBaseDefenseTactical.PandoranTurn.ImplementBaseDefenseVsAliensPostAISortingOut(__instance);

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

                    TFTVBaseDefenseTactical.Map.Consoles.ActivateConsole.BaseDefenseConsoleActivated(__instance, status, controller);
                    TFTVPalaceMission.Consoles.PalaceConsoleActivated(__instance, status, controller);
                    TFTVRescueVIPMissions.TalkingPointConsoleActivated(__instance, status, controller);
                    TFTVNJQuestline.IntroMission.MissionQuips.Propaganda.StopPropaganda(controller, status);
                    // TFTVAncients.AncientDeployment.SmelterConsoleActivated(__instance, status, controller);
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

            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {
                    TFTVDeliriumPerks.ImplementDeliriumPerks(actor, __instance);
                    TFTVEconomyExploitsFixes.AddReinforcementTagToImplementNoDropsOption(actor, __instance);
                    TFTVHumanEnemies.GiveRankAndNameToHumaoidEnemy(actor, __instance);
                    TFTVTouchedByTheVoid.Umbra.UmbraTactical.UmbraEverywhereVoidOmenImplementation(actor, __instance);
                    TFTVBaseDefenseTactical.Objectives.AddScatterObjectiveTagForBaseDefense(actor, __instance);
                    TFTVSpecialDifficulties.OnTactical.AddSpecialDifficultiesBuffsAndVulnerabilities(actor, __instance);
                    TFTVPalaceMission.Revenants.TryToTurnIntoRevenant(actor, __instance);
                    TFTVPalaceMission.MissionObjectives.CheckFinalMissionWinConditionWhereDeployingItem(actor, __instance);
                    TFTVRevenant.Spawning.RevenentEntersPlayAfterLoad(actor);


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
            public static bool Prefix(TacticalActor actor, Dictionary<TacticalActor, PortraitSprites> ____soldierPortraits,
                SquadMemberScrollerController __instance, bool ____renderingInProgress)
            {
                try
                {
                    return TFTVCustomPortraits.NewCharacterPortraitInSetupProperPortrait(actor, ____soldierPortraits, __instance);
                    // return TFTVPalaceMission.MissionObjectives.ForceSpecialCharacterPortraitInSetupProperPortrait(actor, ____soldierPortraits, __instance, ____renderingInProgress);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
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
                    TFTVLogger.Always($"TacticalFactionVision.OnFactionStartTurn for faction " +
                        $"{__instance?.Faction?.Faction?.FactionDef?.GetName()}, turn: {__instance?.Faction?.TacticalLevel?.TurnNumber}");

                    TFTVVanillaFixes.Tactical.ParalysisDamage.ClearDataActorsParalysisDamage();
                    TFTVArtOfCrab.ClearData();
                    TFTVAncients.AncientsNewTurn.AncientsNewTurnCheck(__instance.Faction);
                    TFTVPalaceMission.PalaceTacticalNewTurn(__instance.Faction);
                    TFTVBaseDefenseTactical.PlayerTurn.PhoenixBaseDefenseVSAliensTurnStart(__instance.Faction.TacticalLevel, __instance.Faction);
                    TFTVTouchedByTheVoid.Umbra.UmbraTactical.CheckVO15(__instance.Faction.TacticalLevel, __instance.Faction);
                    TFTVHumanEnemies.ImplementStartingVolleyHumanEnemiesTactic(__instance.Faction);
                    TFTVRevenant.Resistance.ApplySpecialRevenantResistanceArmorStack(__instance.Faction.TacticalLevel, __instance.Faction);
                    PRMBetterClasses.SkillModifications.FactionPerks.DieHardOnFactionStartTurn(__instance.Faction);
                    TFTVAircraftRework.Modules.Tactical.EveryPhoenixTurn.ImplementModuleEffectsOnEveryPhoenixTurn(__instance.Faction);
                    // TFTVNJQuestline.IntroMission.MissionQuips.NJQuips.PopulateQuips(__instance.Faction);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




        [HarmonyPatch(typeof(TacticalActorBase), "ApplyDamage")]
        public static class TFTV_TacticalActorBase_ApplyDamage_Patch
        {
            public static void Postfix(TacticalActorBase __instance)
            {
                try
                {
                    TFTVBaseDefenseTactical.Map.Containment.CheckDamageContainment(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalLevelController), "ActorDamageDealt")]
        public static class TFTV_TacticalLevelController_ActorDamageDealt_Patch
        {
            public static void Prefix(TacticalActor actor, IDamageDealer damageDealer)
            {
                try
                {


                    TFTVTouchedByTheVoid.TBTVRolls.TBTVTriggeres.TBTVTriggerOnActorDamageDealt(actor, damageDealer);



                    //  string taunt = "Is that all you've got?";  // Example
                    //  FloatingTextManager.ShowFloatingText(actor, taunt, Color.white);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            public static void Postfix(TacticalActor actor, IDamageDealer damageDealer)
            {
                try
                {
                    TFTVAncients.HoplitesAbilities.HoplitesMolecularTargeting.CyclopsMolecularTargeting(actor, damageDealer);
                    TFTVBallistics.RemoveDCoy(actor, damageDealer);
                    TFTVHumanEnemies.HumanEnemiesRetributionTacticCheckOnActorDamageDealt(actor, damageDealer);

                    //  TFTVTauntsAndQuips.CollectQuips(actor, TFTVTauntsAndQuips.QuipTrigger.Damage, TFTVTauntsAndQuips.QuipContext.Self);

                    /* if(damageDealer != null && damageDealer.GetTacticalActorBase() != null && damageDealer.GetTacticalActorBase() is TacticalActor tacticalActor) 
                     {
                         TFTVTauntsAndQuips.CollectQuips(tacticalActor, TFTVTauntsAndQuips.QuipTrigger.Damage, TFTVTauntsAndQuips.QuipContext.Any);
                     }*/

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
                    TFTVInfestation.StoryFirstInfestedHaven.CreateOutroInfestation(__instance, deathReport);
                    TFTVTouchedByTheVoid.TBTVRolls.TBTVTriggeres.TouchByTheVoidDeath(deathReport);
                    TFTVRevenant.RecordUpkeep.RevenantKilled(deathReport, __instance);
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
                    TFTVRevenant.RecordUpkeep.RecordPhoenixDeadForRevenantsAndOsiris(deathReport, __instance);

                    TFTVAncients.CyclopsAbilities.CyclopsResistance.AncientKilled(__instance, deathReport);
                    TFTVHumanEnemies.HumanEnemiesTacticsOnDeath(deathReport);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Adapted Lucus solution to avoid Ancient Automata receiving WP penalty on ally death/also used for Human Enemies
        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_HumanEnemies_Patch
        {
            private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
            private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
            private static readonly GameTagDef HumanEnemyTier1GameTag = DefCache.GetDef<GameTagDef>("HumanEnemyTier_1_GameTagDef");
            private static readonly GameTagDef HumanEnemyTier2GameTag = DefCache.GetDef<GameTagDef>("HumanEnemyTier_2_GameTagDef");
            //  private static readonly GameTagDef HumanEnemyTier3GameTag = DefCache.GetDef<GameTagDef>("HumanEnemyTier_3_GameTagDef");
            private static readonly GameTagDef HumanEnemyTier4GameTag = DefCache.GetDef<GameTagDef>("HumanEnemyTier_4_GameTagDef");
            //  private static readonly GameTagDef humanEnemyTagDef = DefCache.GetDef<GameTagDef>("HumanEnemy_GameTagDef");


            public static void Prefix(TacticalActor __instance, DeathReport death, out int __state)
            {
                __state = 0; //Set this to zero so that the method still works for other actors.



                //Postfix checks for relevant GameTags then saves and zeroes the WPWorth of the dying actor before main method is executed.

                GameTagsList<GameTagDef> RelevantTags = new GameTagsList<GameTagDef> { cyclopsTag, hopliteTag, HumanEnemyTier4GameTag, HumanEnemyTier2GameTag, HumanEnemyTier1GameTag };
             
                if ((__instance.HasGameTag(hopliteTag) || __instance.HasGameTag(cyclopsTag))
                    || (__instance.TacticalFaction == death.Actor.TacticalFaction 
                    && (death.Actor.HasGameTags(RelevantTags, false) 
                    || death.Actor.Status != null && death.Actor.Status.HasStatus<MindControlStatus>())))
                {
                    __state = death.Actor.TacticalActorBaseDef.WillPointWorth;
                    death.Actor.TacticalActorBaseDef.WillPointWorth = 0;
                }             
            }

            public static void Postfix(TacticalActor __instance, DeathReport death, int __state)
            {

                //Postfix will remove necessary Willpoints from allies and restore WPWorth's value to the def of the dying actor.
                if (__instance.TacticalFaction == death.Actor.TacticalFaction)
                {
                    if (death.Actor.HasGameTag(HumanEnemyTier2GameTag))
                    {
                        //Allies lose 3WP
                        __instance.CharacterStats.WillPoints.Subtract((__state + 1));
                       
                    }
                    else if (death.Actor.HasGameTag(HumanEnemyTier1GameTag))
                    {
                        //Allies lose 6WP
                        __instance.CharacterStats.WillPoints.Subtract((__state * 3));                      
                    }
                }

                if(__state != 0)
                {
                    death.Actor.TacticalActorBaseDef.WillPointWorth = __state;
                }

            }
        }


        [HarmonyPatch(typeof(TacParticipantSpawn), "GetEligibleDeployZones")]
        public static class TFTV_TacParticipantSpawn_GetEligibleDeployZones_patch
        {
            public static IEnumerable<TacticalDeployZone> Postfix(IEnumerable<TacticalDeployZone> results, TacParticipantSpawn __instance, IEnumerable<TacticalDeployZone> zones, ActorDeployData deployData, int turnNumber, bool includeFutureTurns)
            {

                results = TFTVBaseDefenseTactical.StartingDeployment.PlayerDeployment.CullPlayerDeployZonesBaseDefense(results, deployData, turnNumber, __instance.TacMission.MissionData.MissionType, __instance.TacticalFaction.TacticalLevel);
                results = TFTVBehemothAndRaids.Behemoth.BehemothMission.CullPlayerDeployZonesBehemoth(results, deployData, turnNumber, __instance.TacMission.MissionData.MissionType);

                foreach (TacticalDeployZone zone in results)
                {
                    yield return zone;
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
                    TFTVBaseDefenseTactical.Map.DeploymentZones.InitDeployZonesForBaseDefenseVsAliens(__instance.TacticalLevel);
                    TFTVPalaceMission.EnemyDeployments.TacticalDeployZones.InitDeployZonesForPalaceMission(__instance.TacticalLevel);
                    TFTVNJQuestline.IntroMission.MissionStartChanges.ModifyDeployZones(__instance.TacticalLevel);
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
                    TFTVArtOfCrab.ScyllaBlasterAttack.ForceScyllaToUseCannonsAfterUsingHeadAttack(ability, __instance, parameter);
                    TFTVAncients.CyclopsAbilities.AddMindCrushEffectToCyclposScream(ability, __instance, parameter);
                    TFTVAncients.CyclopsAbilities.CyclopsCrossBeamShooting.RedeployHopliteShieldsAfterMassShootAttackAndRestoreTheirAP(ability, __instance, parameter);
                    TFTVVoxels.TFTVFire.ActivateFireQuencherAbility();
                    TFTVBaseDefenseTactical.Objectives.RemoveScatterRemainingAttackersTagFromEnemiesWithParasychosis(ability, parameter);
                    TFTVPalaceMission.MissionObjectives.CheckFinalMissionWinConditionForExalted(ability);
                    TFTVPalaceMission.Gates.CheckIfPlayerCloseToGate(__instance);
                    TFTVChangesToDLC5.TFTVMercenaries.Tactical.SlugHealTraumaEffect(ability, __instance);
                    TFTVArtOfCrab.GetBestWeaponForOWRF(__instance);
                    TFTVVehicleFixes.CheckSquashing(ability, __instance);
                    TFTVVoxels.TFTVGoo.ClearActorGooPositions();

                    //  TFTVTauntsAndQuips.SelectAndShowQuip(__instance);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(TacticalPerceptionBase), "IsTouchingVoxel")]

        public static class TFTV_TacticalPerceptionBase_IsTouchingVoxel_patch
        {
            public static void Postfix(TacticalPerceptionBase __instance)
            {
                try
                {
                    TFTVVoxels.TFTVFire.CheckFireQuencherTouchingFire(__instance);
                    TFTVVoxels.TFTVGoo.CheckActorTouchingGoo(__instance);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(DamageKeyword), "ProcessKeywordDataInternal")]
        internal static class TFTV_DamageKeyword_ProcessKeywordDataInternal_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {
                    TFTVAncients.HoplitesAbilities.ApplyDamageResistanceToHopliteInHiding(ref data);
                    // TFTVRevenant.Resistance.ApplyRevenantSpecialResistance(ref data);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActorBase), "get_DisplayName")]
        public static class TacticalActorBase_GetDisplayName_RevenantGenerator_Patch
        {
            public static void Postfix(TacticalActorBase __instance, ref string __result)
            {
                try
                {
                    string revenantName = TFTVRevenant.UIandFX.DisplayRevenantName(__instance);

                    if (revenantName != "")
                    {
                        __result = revenantName;
                    }

                    string escapedPandoranName = TFTVBaseDefenseTactical.DisplayEscapedPandoranName(__instance);

                    if (escapedPandoranName != "")
                    {
                        __result += escapedPandoranName;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Adapted from cfehunter old Modnix Mod. All hail cfehunter!
        /// </summary>
        [HarmonyPatch(typeof(ShootAbilitySceneViewElement), "DrawHoverMarker")]
        private static class WeaponSpreadPatch_DrawHoverMarker_Scatter
        {
            static GroundMarker scatterMarker = null;

            [HarmonyPostfix]
            static void Postfix(
                ShootAbilitySceneViewElement __instance,
                bool __result,
                TacticalViewContext context,
                Vector3 to,
                Vector3 from,
                TacticalAbilityTarget target)
            {
                try
                {


                    Weapon weapon = (Weapon)__instance.Ability.EquipmentSource;
                    //  TFTVLogger.Always($"ShootAbilitySceneViewElement.DrawHoverMarker running; result: {__result}, weapon null? {weapon==null}");
                    if (__result && weapon != null)
                    {
                        float spread = Mathf.Abs(weapon.GetWeaponSpread(AttackType.Regular, weapon.GetAllSpreadMultipliers(__instance.Ability.TacticalActor), (float)__instance.Ability.TacticalActor.CharacterStats.GetAccuracy(), (from - to).magnitude));
                        //  TFTVLogger.Always($"{__result} {weapon.DisplayName} {spread}, {weapon.WeaponDef.SpreadRadius}, {weapon.GetAllSpreadMultipliers(__instance.Ability.TacticalActor)} {(float)__instance.Ability.TacticalActor.CharacterStats.GetAccuracy()} {(from - to).magnitude}");

                        if (spread > float.Epsilon)
                        {
                            GroundMarkerType scatterType = GroundMarkerType.AreaOfEffectAura;//AttackRadiusInvalid;

                            if (scatterMarker == null || scatterMarker.Type != scatterType)
                            {
                                scatterMarker = new GroundMarker(scatterType, to, 0f);
                                scatterMarker.Areas = __instance.Ability.TacticalActor.TacticalNav.NavAreas;
                            }

                            context.View.Markers.AddGroundMarker(GroundMarkerGroup.HoverSelection, scatterMarker);
                            Utils.TiltForTerrain(context, scatterMarker, __instance.Ability.TacticalActor.TacticalNav.FloorLayers);

                            scatterMarker.VisualObject.transform.position = to;

                            scatterMarker.VisualObject.transform.localScale = spread * Vector3.one * 2;
                        }
                    }
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
