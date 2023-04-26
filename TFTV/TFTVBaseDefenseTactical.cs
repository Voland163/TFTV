using Base;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Levels;
using Base.Utils.Maths;
using com.ootii.Geometry;
using HarmonyLib;
using hoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Destruction;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.Sequencer;
using PhoenixPoint.Tactical.UI;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using UnityEngine;

namespace TFTV
{
    internal class TFTVBaseDefenseTactical
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static float AttackProgress = 0;
        public static bool[] ConsoleInBaseDefense = new bool[3];

        public static int StratToBeAnnounced = 0;
        public static int StratToBeImplemented = 0;

        private static readonly string ConsoleName1 = "BaseDefenseConsole1";
        private static readonly string ConsoleName2 = "BaseDefenseConsole2";
        private static readonly string ConsoleName3 = "BaseDefenseConsole3";
        private static readonly DefRepository Repo = TFTVMain.Repo;
        // private static readonly GameTagDef InfestationFirstObjectiveTag = DefCache.GetDef<GameTagDef>("PhoenixBaseInfestation_GameTagDef");
        private static readonly GameTagDef InfestationSecondObjectiveTag = DefCache.GetDef<GameTagDef>("ScatterRemainingAttackers_GameTagDef");


        //Patch to add objective tag on Pandorans for the Scatter Attackers objective
        //Doesn't activate if Pandoran faction not present
        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_BaseDefense_Patch
        {
            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {
                    ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                    ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                    ClassTagDef SirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef AcheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");


                    // TFTVLogger.Always("ActorEnteredPlay invoked");
                    if (CheckIfBaseDefense(__instance))
                    {

                        if (__instance.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                        {
                            //   TFTVLogger.Always("found aln faction and checked that VO is in place");

                            if (actor.TacticalFaction.Faction.FactionDef.MatchesShortName("aln")
                                && actor is TacticalActor tacticalActor
                                && (actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishTag) || actor.GameTags.Contains(SirenTag) || actor.GameTags.Contains(AcheronTag))
                                && !actor.GameTags.Contains(InfestationSecondObjectiveTag)
                                )
                            {
                                actor.GameTags.Add(InfestationSecondObjectiveTag);
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

        //Runs method that does what needs to be done at start of Phoenix turn when defending vs Aliens
        [HarmonyPatch(typeof(TacticalFactionVision), "OnFactionStartTurn")]
        public static class TacticalFactionVision_OnFactionStartTurn_BaseDefense_Patch
        {
            public static void Postfix(TacticalFactionVision __instance)
            {
                try
                {
                    if (!__instance.Faction.TacticalLevel.IsLoadingSavedGame)
                    {
                        BaseDefenseTurnStartChecks(__instance.Faction.TacticalLevel, __instance.Faction);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void BaseDefenseTurnStartChecks(TacticalLevelController controller, TacticalFaction faction)
        {
            try
            {
                if (CheckIfBaseDefense(controller) && controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    //TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");


                    if (controller.TurnNumber > 0)
                    {
                        CheckForLossOfObjectiveTag(controller);
                    }
                    if (faction == phoenix && StratToBeImplemented != 0)
                    {
                        StratImplementer(controller);
                    }
                    if (faction == phoenix && StratToBeAnnounced != 0)
                    {
                        StratAnnouncer(controller);
                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CheckForLossOfObjectiveTag(TacticalLevelController controller)
        {
            try
            {
                List<TacticalActor> allPandorans = controller.GetFactionByCommandName("aln").TacticalActors.ToList();

                if (allPandorans.Count > 0)
                {

                    foreach (TacticalActor tacticalActor in allPandorans)
                    {
                        if (tacticalActor.GameTags.Contains(InfestationSecondObjectiveTag))
                        {
                            if (tacticalActor.IsDisabled || tacticalActor.IsEvacuated || tacticalActor.IsControlledByPlayer || tacticalActor.Status.CurrentStatuses.Any(s => s.Def.EffectName == "Panic"))
                            {
                                tacticalActor.GameTags.Remove(InfestationSecondObjectiveTag);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        //This method is NOT ONLY FOR BASE DEFENSE; also implements Void Omen objectives
        public static void ModifyObjectives(TacMissionTypeDef missionType)
        {
            try
            {
                TFTVLogger.Always("ModifyObjectives");
                GameTagDef baseDefense = DefCache.GetDef<GameTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");
                GameTagDef havenDefense = DefCache.GetDef<GameTagDef>("MissionTypeHavenDefense_MissionTagDef");

                List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                // Remove faction objectives that correspond to void omens that are not in play
                for (int i = listOfFactionObjectives.Count - 1; i >= 0; i--)
                {
                    FactionObjectiveDef objective = listOfFactionObjectives[i];
                    if (objective.name.StartsWith("VOID_OMEN_TITLE_"))
                    {
                        int vo = int.Parse(objective.name.Substring("VOID_OMEN_TITLE_".Length));
                        if (!TFTVVoidOmens.VoidOmensCheck[i])
                        {
                            TFTVLogger.Always("Removing VO " + vo + " from faction objectives");
                            listOfFactionObjectives.RemoveAt(i);
                        }
                        if (i == 5 && TFTVVoidOmens.VoidOmensCheck[i] && !missionType.Tags.Contains(havenDefense))
                        {
                            TFTVLogger.Always("Removing VO " + vo + " (hostile defenders) from faction objectives because not a haven defense mission");
                            listOfFactionObjectives.RemoveAt(i);
                        }
                    }
                }

                // Add faction objectives for void omens that are in play
                foreach (int vo in voidOmens)
                {
                    if (TFTVVoidOmens.VoidOmensCheck[vo])
                    {
                        if (vo != 5 || vo == 5 && missionType.Tags.Contains(havenDefense))

                            if (!listOfFactionObjectives.Any(o => o.name == "VOID_OMEN_TITLE_" + vo))
                            {
                                TFTVLogger.Always("Adding VO " + vo + " to faction objectives");
                                listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                            }
                    }
                }

                if (missionType.Tags.Contains(baseDefense) && missionType.ParticipantsData[0].FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef"))
                {

                    KillActorFactionObjectiveDef killSentinels = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [PhoenixBaseInfestation]");
                    KillActorFactionObjectiveDef scatterEnemies = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [ScatterRemainingAttackers]");
                    WipeEnemyFactionObjectiveDef killAllEnemies = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("E_DefeatEnemies [PhoenixBaseDefense_CustomMissionTypeDef]");
                    ProtectKeyStructuresFactionObjectiveDef protectFacilities = DefCache.GetDef<ProtectKeyStructuresFactionObjectiveDef>("E_ProtectKeyStructures [PhoenixBaseDefense_CustomMissionTypeDef]");

                    if (listOfFactionObjectives.Contains(killAllEnemies))
                    {

                        listOfFactionObjectives.Remove(killAllEnemies);

                    }
                    if (listOfFactionObjectives.Contains(protectFacilities))
                    {
                        listOfFactionObjectives.Remove(protectFacilities);

                    }

                    if (AttackProgress >= 0.3)
                    {
                        if (!listOfFactionObjectives.Contains(killSentinels))
                        {
                            listOfFactionObjectives.Add(killSentinels);

                        }

                    }
                    else
                    {
                        if (!listOfFactionObjectives.Contains(scatterEnemies))
                        {
                            listOfFactionObjectives.Add(scatterEnemies);

                        }



                    }
                }

                TFTVLogger.Always($"{listOfFactionObjectives[0].name} base defense objective");

                missionType.CustomObjectives = listOfFactionObjectives.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

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
                    //  TFTVLogger.Always($"ModifyMissionData invoked");
                    if (__instance.Site.Type == GeoSiteType.PhoenixBase && AttackProgress >= 0.3
                        && missionData.MissionType.ParticipantsData[0].FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef"))
                    {
                        PPFactionDef alienFaction = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                        int difficulty = __instance.GameController.CurrentDifficulty.Order;
                        // TFTVLogger.Always($"if passed");
                        foreach (TacMissionFactionData tacMissionFactionData in missionData.MissionParticipants)
                        {
                            TFTVLogger.Always($"{tacMissionFactionData.FactionDef} {tacMissionFactionData.InitialDeploymentPoints}");

                            if (tacMissionFactionData.FactionDef == alienFaction)
                            {
                                tacMissionFactionData.InitialDeploymentPoints *= 0.6f + (0.05f * difficulty);

                                TFTVLogger.Always($"Deployment points changed to {tacMissionFactionData.InitialDeploymentPoints}");

                            }

                        }

                        ContextHelpHintDef hintDef = DefCache.GetDef<ContextHelpHintDef>("TFTVBaseDefense");

                        if (AttackProgress < 0.3)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION";

                        }
                        else if (AttackProgress >= 0.3 && AttackProgress < 0.8)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_NESTING_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_NESTING_DESCRIPTION";
                        }
                        else
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_INFESTATION_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_INFESTATION_DESCRIPTION";

                        }


                    }

                    ModifyObjectives(missionData.MissionType);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        //Method to set situation at start of base defense; currently only for alien assault
        public static void StartingSitrep(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always($"Attack on base progress is {AttackProgress}");

                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    if (AttackProgress >= 0.8)
                    {
                        SetPlayerSpawnTunnels(controller);
                        InfestationStrat(controller);
                    }
                    else if (AttackProgress >= 0.3 && AttackProgress < 0.8)
                    {
                        SetPlayerSpawnTunnels(controller);
                        NestingStrat(controller);
                    }
                    else
                    {
                        SetPlayerSpawnTopsideAndCenter(controller);

                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void NestingStrat(TacticalLevelController controller)
        {
            try
            {
                GameTagDef infestationTag = DefCache.GetDef<GameTagDef>("PhoenixBaseInfestation_GameTagDef");
                TacCharacterDef acidWormEgg = DefCache.GetDef<TacCharacterDef>("Acidworm_Egg_AlienMutationVariationDef");
                // TacCharacterDef explosiveEgg = DefCache.GetDef<TacCharacterDef>("Explosive_Egg_TacCharacterDef");
                TacCharacterDef fraggerEgg = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef");
                TacCharacterDef fireWormEgg = DefCache.GetDef<TacCharacterDef>("Fireworm_Egg_AlienMutationVariationDef");
                TacCharacterDef poisonWormEgg = DefCache.GetDef<TacCharacterDef>("Poisonworm_Egg_AlienMutationVariationDef");
                TacCharacterDef swarmerEgg = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_TacCharacterDef");

                TacCharacterDef sentinelHatching = DefCache.GetDef<TacCharacterDef>("SentinelHatching_AlienMutationVariationDef");
                TacCharacterDef sentinelTerror = DefCache.GetDef<TacCharacterDef>("SentinelTerror_AlienMutationVariationDef");
                TacCharacterDef sentinelMist = DefCache.GetDef<TacCharacterDef>("SentinelMist_AlienMutationVariationDef");

                //   TacCharacterDef spawnery = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");

                List<TacCharacterDef> sentinels = new List<TacCharacterDef>() { sentinelMist, sentinelHatching, sentinelTerror };

                TacticalDeployZone centralZone = FindCentralDeployZone(controller);
                //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                ActorDeployData spawneryDeployData = sentinels.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp())).GenerateActorDeployData();
                spawneryDeployData.InitializeInstanceData();
                TacticalActorBase sentinel = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);

                sentinel.GameTags.Add(infestationTag);

                List<TacticalDeployZone> otherCentralZones = GetCenterSpaceDeployZones(controller);
                otherCentralZones.Remove(centralZone);

                List<TacCharacterDef> eggs = new List<TacCharacterDef>() { acidWormEgg, fraggerEgg, fireWormEgg, poisonWormEgg, swarmerEgg };


                List<TacCharacterDef> availableTemplatesOrdered =
                    new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                foreach (TacCharacterDef def in eggs)
                {
                    if (availableTemplatesOrdered.Contains(def))
                    {
                        availableEggs.Add(def);
                        // TFTVLogger.Always($"{def.name} added");

                    }
                }

                foreach (TacticalDeployZone tacticalDeployZone in otherCentralZones)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = UnityEngine.Random.Range(1, controller.Difficulty.Order);

                    for (int x = 0; x < roll; x++)
                    {
                        TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));


                        ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                        actorDeployData.InitializeInstanceData();
                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);


                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        internal static void InfestationStrat(TacticalLevelController controller)
        {
            try
            {
                GameTagDef infestationTag = DefCache.GetDef<GameTagDef>("PhoenixBaseInfestation_GameTagDef");
                TacCharacterDef acidWormEgg = DefCache.GetDef<TacCharacterDef>("Acidworm_Egg_AlienMutationVariationDef");
                // TacCharacterDef explosiveEgg = DefCache.GetDef<TacCharacterDef>("Explosive_Egg_TacCharacterDef");
                TacCharacterDef fraggerEgg = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef");
                TacCharacterDef fireWormEgg = DefCache.GetDef<TacCharacterDef>("Fireworm_Egg_AlienMutationVariationDef");
                TacCharacterDef poisonWormEgg = DefCache.GetDef<TacCharacterDef>("Poisonworm_Egg_AlienMutationVariationDef");
                TacCharacterDef swarmerEgg = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_TacCharacterDef");

                TacCharacterDef sentinelHatching = DefCache.GetDef<TacCharacterDef>("SentinelHatching_AlienMutationVariationDef");
                //  TacCharacterDef sentinelTerror = DefCache.GetDef<TacCharacterDef>("SentinelTerror_AlienMutationVariationDef");
                TacCharacterDef sentinelMist = DefCache.GetDef<TacCharacterDef>("SentinelMist_AlienMutationVariationDef");

                TacCharacterDef spawneryDef = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");

                List<TacCharacterDef> sentinels = new List<TacCharacterDef>() { sentinelMist, sentinelHatching, sentinelMist, sentinelHatching };

                TacticalDeployZone centralZone = FindCentralDeployZone(controller);
                //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                ActorDeployData spawneryDeployData = spawneryDef.GenerateActorDeployData();
                spawneryDeployData.InitializeInstanceData();
                TacticalActorBase spawnery = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);
                spawnery.GameTags.Add(infestationTag);

                List<TacticalDeployZone> otherCentralZones = GetCenterSpaceDeployZones(controller);
                otherCentralZones.Remove(centralZone);


                for (int i = 0; i < controller.Difficulty.Order; i++)
                {

                    ActorDeployData actorDeployData = sentinels[i].GenerateActorDeployData();
                    actorDeployData.InitializeInstanceData();
                    TacticalActorBase sentinel = otherCentralZones[i].SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, otherCentralZones[i].transform, true, otherCentralZones[i]);
                    sentinel.GameTags.Add(infestationTag);
                }



                List<TacCharacterDef> eggs = new List<TacCharacterDef>() { acidWormEgg, fraggerEgg, fireWormEgg, poisonWormEgg, swarmerEgg };


                List<TacCharacterDef> availableTemplatesOrdered =
                    new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                foreach (TacCharacterDef def in eggs)
                {
                    if (availableTemplatesOrdered.Contains(def))
                    {
                        availableEggs.Add(def);
                        // TFTVLogger.Always($"{def.name} added");

                    }
                }


                foreach (TacticalDeployZone tacticalDeployZone in GetEnemyDeployZones(controller))
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = UnityEngine.Random.Range(1, 11 + controller.Difficulty.Order);


                    if (roll > 6)
                    {
                        TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));


                        ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                        actorDeployData.InitializeInstanceData();
                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                    }
                }

                SpawnAdditionalEggs(controller);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void SetPlayerSpawnTunnels(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> playerDeployZones = GetTunnelDeployZones(controller);
                List<TacticalDeployZone> allPlayerDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).
                Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player));


                foreach (TacticalDeployZone deployZone in playerDeployZones)
                {
                    // TFTVLogger.Always($"{deployZone.name} at {deployZone.Pos} is {deployZone.IsDisabled}");


                    List<MissionDeployConditionData> missionDeployConditionDatas = GetTopsideDeployZones(controller).First().MissionDeployment;

                    deployZone.MissionDeployment.AddRange(missionDeployConditionDatas);
                }

                foreach (TacticalDeployZone zone in allPlayerDeployZones)
                {
                    if (!playerDeployZones.Contains(zone))
                    {
                        zone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                    }
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void SetPlayerSpawnTopsideAndCenter(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> playerDeployZones = GetTopsideDeployZones(controller);
                playerDeployZones.AddRange(GetCenterSpaceDeployZones(controller));

                List<TacticalDeployZone> allPlayerDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).
                Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player));


                foreach (TacticalDeployZone zone in allPlayerDeployZones)
                {
                    if (!playerDeployZones.Contains(zone))
                    {
                        zone.SetFaction(controller.GetFactionByCommandName("env"), TacMissionParticipant.Environment);
                    }
                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckConsoleSituation(TacticalLevelController controller)
        {
            try
            {
                if (CheckIfBaseDefense(controller))
                {

                    if (StratToBeImplemented != 0 && VentingHintShown == false)
                    {
                        VentingHintShown = true;
                        InteractionPointPlacement();

                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        public static bool VentingHintShown = false;

        public static void StratAnnouncer(TacticalLevelController controller)
        {
            try
            {
                if (StratToBeAnnounced != 0)
                {
                    TFTVLogger.Always($"strat for next turn is {StratToBeAnnounced}, so expecting a hint");
                    TacContextHelpManager hintManager = GameUtl.CurrentLevel().GetComponent<TacContextHelpManager>();
                    ContextHelpHintDef contextHelpHintDef = null;
                    FieldInfo hintsPendingDisplayField = typeof(ContextHelpManager).GetField("_hintsPendingDisplay", BindingFlags.NonPublic | BindingFlags.Instance);

                    switch (StratToBeAnnounced)
                    {
                        case 1:
                            {
                                // WormDropStrat(controller);
                                contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseWormsStrat");

                            }
                            break;

                        case 2:
                            {
                                //  GenerateSecondaryForce(controller);
                                contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseForce2Strat");

                            }
                            break;

                        case 3:
                            {
                                //  UmbraStrat(controller);
                                contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseUmbraStrat");

                            }
                            break;
                        case 4:
                            {
                                //   MyrmidonAssaultStrat(controller);
                                contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseWormsStrat");

                            }
                            break;
                    }


                    if (!hintManager.RegisterContextHelpHint(contextHelpHintDef, isMandatory: false, null))
                    {

                        ContextHelpHint item = new ContextHelpHint(contextHelpHintDef, isMandatory: false, null);

                        // Get the current value of _hintsPendingDisplay
                        List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);

                        // Add the new hint to _hintsPendingDisplay
                        hintsPendingDisplay.Add(item);

                        // Set the modified _hintsPendingDisplay value back to the hintManager instance
                        hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                    }

                    MethodInfo startLoadingHintAssetsMethod = typeof(TacContextHelpManager).GetMethod("StartLoadingHintAssets", BindingFlags.NonPublic | BindingFlags.Instance);

                    object[] args = new object[] { contextHelpHintDef }; // Replace hintDef with your desired argument value

                    // Invoke the StartLoadingHintAssets method using reflection
                    startLoadingHintAssetsMethod.Invoke(hintManager, args);

                    controller.View.TryShowContextHint();
                    StratToBeImplemented = StratToBeAnnounced;
                    StratToBeAnnounced = 0;

                    if (!VentingHintShown)
                    {
                        ContextHelpHintDef ventingHint = DefCache.GetDef<ContextHelpHintDef>("BaseDefenseVenting");

                        if (!hintManager.RegisterContextHelpHint(ventingHint, isMandatory: false, null))
                        {

                            ContextHelpHint item = new ContextHelpHint(ventingHint, isMandatory: false, null);

                            // Get the current value of _hintsPendingDisplay
                            List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);

                            // Add the new hint to _hintsPendingDisplay
                            hintsPendingDisplay.Add(item);

                            // Set the modified _hintsPendingDisplay value back to the hintManager instance
                            hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                        }

                        args = new object[] { ventingHint }; // Replace hintDef with your desired argument value

                        // Invoke the StartLoadingHintAssets method using reflection
                        startLoadingHintAssetsMethod.Invoke(hintManager, args);

                        controller.View.TryShowContextHint();
                        VentingHintShown = true;
                        InteractionPointPlacement();
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void StratImplementer(TacticalLevelController controller)
        {
            try
            {
                if (StratToBeImplemented != 0)
                {
                    switch (StratToBeImplemented)
                    {
                        case 1:
                            {
                                WormDropStrat(controller);

                            }
                            break;

                        case 2:
                            {
                                SpawnSecondaryForce(controller);

                            }
                            break;

                        case 3:
                            {
                                UmbraStrat(controller);

                            }
                            break;
                        case 4:
                            {
                                MyrmidonAssaultStrat(controller);

                            }
                            break;
                    }

                    StratToBeImplemented = 0;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        //Patch that invokes StratPicker method, and therefore sets the chain that will lead to announcement of strat, showing hint, spawning interaction points, etc
        [HarmonyPatch(typeof(TacticalFaction), "GetSortedAIActors")]
        public static class TFTV_TacticalFactionn_GetSortedAIActors_BaseDefense_patch
        {
            public static void Postfix(List<TacticalActor> __result, TacticalFaction __instance)
            {
                try
                {
                    if (__result.Count > 0)
                    {
                        if (CheckIfBaseDefense(__instance.TacticalLevel) && __instance.Equals(__instance.TacticalLevel.GetFactionByCommandName("aln")))
                        {
                            // if (__instance.TacticalLevel.TurnNumber > 0)
                            //  {

                            StratPicker(__instance.TacticalLevel);

                            //  }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        public static void StratPicker(TacticalLevelController controller)
        {
            try
            {
                if (CheckIfBaseDefense(controller)) //need to check for completion of objectives...
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = 7;//UnityEngine.Random.Range(1, 11 + controller.Difficulty.Order);

                    TFTVLogger.Always($"Picking strat for base defense, roll is {roll}");

                    if (roll == 5 || roll == 12)
                    {
                        StratToBeAnnounced = 1;

                        // WormDropStrat(controller);
                    }
                    else if (roll == 6 || roll == 13)
                    {
                        StratToBeAnnounced = 2;
                        // GenerateSecondaryForce(controller);
                    }
                    else if (roll == 7 || roll == 14)
                    {
                        StratToBeAnnounced = 3;
                        // UmbraStrat(controller);

                    }
                    else if (roll == 8 || roll == 10)
                    {
                        StratToBeAnnounced = 4;
                        // MyrmidonAssaultStrat(controller);
                    }
                    else if (roll == 9 || roll == 11)
                    {
                        if (AttackProgress >= 8)
                        {
                            SpawnAdditionalEggs(controller);
                        }
                        else
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll2 = UnityEngine.Random.Range(1, 7);
                            if (roll == 1)
                            {
                                StratToBeAnnounced = 1;

                                // WormDropStrat(controller);

                            }
                            else if (roll == 2)
                            {
                                StratToBeAnnounced = 2;
                                // GenerateSecondaryForce(controller);

                            }
                            else if (roll == 3)
                            {

                                StratToBeAnnounced = 3;
                                // UmbraStrat(controller);

                            }
                            else if (roll == 4)
                            {
                                StratToBeAnnounced = 4;
                                // MyrmidonAssaultStrat(controller);

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static TacticalDeployZone FindCentralDeployZone(TacticalLevelController controller)
        {
            try
            {
                TacticalDeployZone centralDeployZone = new TacticalDeployZone();

                List<TacticalDeployZone> centralDeployZones = GetCenterSpaceDeployZones(controller);



                foreach (TacticalDeployZone zone in centralDeployZones)
                {
                    int countChecks = 0;

                    for (int x = 0; x < centralDeployZones.Count(); x++)
                    {
                        float magnitude = (zone.Pos - centralDeployZones[x].Pos).HorizontalMagnitude();

                        if (magnitude < 10)
                        {
                            countChecks += 1;

                        }

                    }

                    if (countChecks == centralDeployZones.Count())
                    {
                        centralDeployZone = zone;

                    }

                }


                return centralDeployZone;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        internal static void SpawnAdditionalEggs(TacticalLevelController controller)
        {
            try
            {
                TacCharacterDef acidWormEgg = DefCache.GetDef<TacCharacterDef>("Acidworm_Egg_AlienMutationVariationDef");
                TacCharacterDef fraggerEgg = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef");
                TacCharacterDef fireWormEgg = DefCache.GetDef<TacCharacterDef>("Fireworm_Egg_AlienMutationVariationDef");
                TacCharacterDef poisonWormEgg = DefCache.GetDef<TacCharacterDef>("Poisonworm_Egg_AlienMutationVariationDef");
                TacCharacterDef swarmerEgg = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_TacCharacterDef");

                List<TacticalDeployZone> centralZones = GetCenterSpaceDeployZones(controller);


                List<TacCharacterDef> eggs = new List<TacCharacterDef>() { acidWormEgg, fraggerEgg, fireWormEgg, poisonWormEgg, swarmerEgg };


                List<TacCharacterDef> availableTemplatesOrdered =
                    new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                foreach (TacCharacterDef def in eggs)
                {
                    if (availableTemplatesOrdered.Contains(def))
                    {
                        availableEggs.Add(def);
                        // TFTVLogger.Always($"{def.name} added");

                    }
                }


                foreach (TacticalDeployZone tacticalDeployZone in centralZones)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = UnityEngine.Random.Range(1, 11 + controller.Difficulty.Order);


                    if (roll > 6)
                    {
                        TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));


                        ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                        actorDeployData.InitializeInstanceData();
                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                    }

                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }


        }
        internal static void GenerateExplosion(Vector3 position)
        {
            try
            {
                DelayedEffectDef explosion = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");

                Vector3 vector3 = new Vector3(position.x + UnityEngine.Random.Range(-4, 4), position.y, position.z + UnityEngine.Random.Range(-4, 4)); //for testing


                Effect.Apply(Repo, explosion, new EffectTarget
                {
                    Position = vector3
                }, null);

                //   TacticalLevelController controllerTactical = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));



                //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                  {
                      ChaseVector = position
                  };*/

                //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }
        internal static void GenerateFireExplosion(Vector3 position)
        {
            try
            {
                // FireExplosionEffectDef explosion = DefCache.GetDef<FireExplosionEffectDef>("E_FireExplosionEffect [Fire_StandardDamageTypeEffectDef]");
                SpawnTacticalVoxelEffectDef spawnFire = DefCache.GetDef<SpawnTacticalVoxelEffectDef>("FireVoxelSpawnerEffect");

                Vector3 vector3 = new Vector3(position.x + UnityEngine.Random.Range(-4, 4), position.y, position.z + UnityEngine.Random.Range(-4, 4));

                Effect.Apply(Repo, spawnFire, new EffectTarget
                {
                    Position = vector3
                }, null);

                //   TacticalLevelController controllerTactical = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));



                //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                  {
                      ChaseVector = position
                  };*/

                //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        internal static void GenerateFakeExplosion(Vector3 position)
        {
            try
            {
                DelayedEffectDef explosion = DefCache.GetDef<DelayedEffectDef>("FakeExplosion_ExplosionEffectDef");


                Effect.Apply(Repo, explosion, new EffectTarget
                {
                    Position = position
                }, null);

                //   TacticalLevelController controllerTactical = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));



                //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                  {
                      ChaseVector = position
                  };*/

                //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }


        }

        internal static void GenerateRandomExplosions()
        {
            try
            {

                TacticalLevelController controller = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));
                List<TacticalDeployZone> zones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null));
                zones.RemoveRange(GetTopsideDeployZones(controller));

                int explosions = 0;
                foreach (TacticalDeployZone zone in zones)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(0, 4);

                    if (roll == 0)
                    {
                        GenerateFireExplosion(zone.Pos);
                        explosions++;
                        TFTVLogger.Always($"explosion count {explosions}");
                    }
                    else if (roll == 1)
                    {
                        GenerateExplosion(zone.Pos);
                        explosions++;
                        TFTVLogger.Always($"explosion count {explosions}");


                    }


                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }


        [HarmonyPatch(typeof(StatusComponent), "AddStatus")]
        public static class StatusComponent_AddStatus_patch
        {
            public static void Postfix(StatusComponent __instance, Status status)
            {
                try
                {
                    TacticalLevelController controller = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));

                    //   TFTVLogger.Always($"Status {status.Def.name} applied to {__instance.transform.name}");
                    if (controller != null && CheckIfBaseDefense(controller))
                    {
                        if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                        {
                            StructuralTarget console = __instance.transform.GetComponent<StructuralTarget>();
                            List<StructuralTarget> generators = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().Where(b => b.name.StartsWith("PP_Cover_Generator")).ToList();

                            TFTVLogger.Always($"Console {console.name} activated");

                            if (console.name.Equals(ConsoleName1) && !ConsoleInBaseDefense[0])
                            {
                                //  TFTVLogger.Always($"Console {console.name} activation logged");
                                ConsoleInBaseDefense[0] = true;
                                StratToBeImplemented = 0;

                                if (generators.Count > 0)
                                {
                                    foreach (StructuralTarget structuralTarget in generators)
                                    {
                                        GenerateExplosion(structuralTarget.Pos);
                                    }

                                    GenerateRandomExplosions();
                                }

                            }
                            else if (console.name.Contains(ConsoleName2) && !ConsoleInBaseDefense[1])
                            {
                                //  TFTVLogger.Always($"Console {console.name} activation logged");
                                ConsoleInBaseDefense[1] = true;
                                StratToBeImplemented = 0;

                                if (generators.Count > 0)
                                {
                                    foreach (StructuralTarget structuralTarget in generators)
                                    {
                                        GenerateExplosion(structuralTarget.Pos);
                                    }

                                    GenerateRandomExplosions();
                                }
                            }
                            else if (console.name.Contains(ConsoleName3) && !ConsoleInBaseDefense[2])
                            {
                                //  TFTVLogger.Always($"Console {console.name} activation logged");
                                ConsoleInBaseDefense[2] = true;
                                StratToBeImplemented = 0;

                                if (generators.Count > 0)
                                {
                                    foreach (StructuralTarget structuralTarget in generators)
                                    {
                                        GenerateExplosion(structuralTarget.Pos);
                                    }

                                    GenerateRandomExplosions();
                                }
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

        internal static void DeactivateConsole(string name)
        {
            try
            {
                TFTVLogger.Always($"Looking for console with name {name}");

                StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                StructuralTarget console = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));
                TFTVLogger.Always($"Found console {console.name}");

                Status status = console.Status.GetStatusByName(activeConsoleStatusDef.EffectName);
                TFTVLogger.Always($"found status {status.Def.EffectName}");
                console.Status.UnapplyStatus(status);

                //KEY_ACTIVATE_OBJECTIVE_PROMPT
         

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }


        }

        internal static void CheckIfConsoleActivated(TacticalLevelController controller)
        {
            try
            {
                if (CheckIfBaseDefense(controller))
                {
                    for (int x = 0; x < ConsoleInBaseDefense.Count(); x++)
                    {
                        TFTVLogger.Always($"{ConsoleInBaseDefense[x]}");

                        if (ConsoleInBaseDefense[x] == true)
                        {
                            DeactivateConsole("BaseDefenseConsole" + (x + 1));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        internal static void SpawnInteractionPoint(Vector3 position, string name)
        {
            try
            {
                StructuralTargetDeploymentDef stdDef = DefCache.GetDef<StructuralTargetDeploymentDef>("HackableConsoleStructuralTargetDeploymentDef");

                TacActorData tacActorData = new TacActorData
                {
                    ComponentSetTemplate = stdDef.ComponentSet
                };


                StructuralTargetInstanceData structuralTargetInstanceData = tacActorData.GenerateInstanceData() as StructuralTargetInstanceData;
                //  structuralTargetInstanceData.FacilityID = facilityID;
                structuralTargetInstanceData.SourceTemplate = stdDef;
                structuralTargetInstanceData.Source = tacActorData;


                StructuralTarget structuralTarget = ActorSpawner.SpawnActor<StructuralTarget>(tacActorData.GenerateInstanceComponentSetDef(), structuralTargetInstanceData, callEnterPlayOnActor: false);
                GameObject obj = structuralTarget.gameObject;
                structuralTarget.name = name;
                structuralTarget.Source = obj;

                var ipCols = new GameObject("InteractionPointColliders");
                ipCols.transform.SetParent(obj.transform);
                ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                var collider = ipCols.AddComponent<BoxCollider>();


                structuralTarget.Initialize();
                structuralTarget.DoEnterPlay();


                StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                structuralTarget.Status.ApplyStatus(activeConsoleStatusDef);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        internal static void InteractionPointPlacement()
        {
            try
            {
                TacticalLevelController controller = (TacticalLevelController)UnityEngine.Object.FindObjectOfType(typeof(TacticalLevelController));

                if (CheckIfBaseDefense(controller))
                {

                    List<Breakable> consoles = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("NJR_LoCov_Console")).ToList();
                    Vector3[] position = new Vector3[3];

                    List<Breakable> consolesCulled = new List<Breakable>(consoles);

                    foreach (Breakable gameObject in consoles)
                    {
                        foreach (Breakable gameObject2 in consoles)
                        {
                            if (gameObject != gameObject2 && gameObject.transform.position.x == gameObject2.transform.position.x && gameObject.transform.position.z > gameObject2.transform.position.z)
                            {
                                position[0] = gameObject.transform.position + new Vector3(1, 0, 0);

                                TFTVLogger.Always($"{gameObject.name} is at position {gameObject.transform.position} and IPC will be placed at " +
                                    $"{position[0]}");
                                consolesCulled.Remove(gameObject);
                            }
                            else if (gameObject != gameObject2 && gameObject.transform.position.x == gameObject2.transform.position.x && gameObject.transform.position.z < gameObject2.transform.position.z)
                            {
                                position[2] = gameObject.transform.position + new Vector3(1, 0, 0);

                                TFTVLogger.Always($"{gameObject.name} is at position {gameObject.transform.position} and IPC will be placed at " +
                                    $"{position[2]}");
                                consolesCulled.Remove(gameObject);
                            }

                        }
                    }
                    position[1] = consolesCulled[0].transform.position + new Vector3(0, 0, 1);
                    TFTVLogger.Always($"{consolesCulled[0].gameObject.name} is at position {consolesCulled[0].transform.position} and IPC will be placed at " +
                    $"{position[1]}");


                    SpawnInteractionPoint(position[0], ConsoleName1);

                    SpawnInteractionPoint(position[1], ConsoleName2);

                    SpawnInteractionPoint(position[2], ConsoleName3);

                    CheckIfConsoleActivated(controller);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        [HarmonyPatch(typeof(TacMission), "InitDeployZones")]

        public static class TacMission_InitDeployZones_Experiment_patch
        {
            public static void Postfix(TacMission __instance)
            {
                try
                {
                    TacticalLevelController controller = __instance.TacticalLevel;

                    if (CheckIfBaseDefense(controller) && !controller.IsFromSaveGame)
                    {
                        TFTVLogger.Always("Initiating Deploy Zones for a base defense mission; not from save game");
                        StartingSitrep(controller);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        internal static void RevealAllSpawns(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> zones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null));

                //  TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");

              /*  TacticalDeployZone tacticalDeployZone1 = new TacticalDeployZone() { };
                tacticalDeployZone1 = zones.First();
                tacticalDeployZone1.SetPosition(zones.First().Pos + new Vector3(3, 0, 3));*/


                MethodInfo createVisuals = AccessTools.Method(typeof(TacticalDeployZone), "CreateVisuals");

                foreach (TacticalDeployZone tacticalDeployZone in zones)
                {
                    createVisuals.Invoke(tacticalDeployZone, null);
                }

             //   createVisuals.Invoke(tacticalDeployZone1, null);

                //  InfestationStrat(controller);

                //  GetCenterSpaceDeployZones(controller);
                //  GetTunnelDeployZones(controller);

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }


        internal static List<TacticalDeployZone> GetTopsideDeployZones(TacticalLevelController controller)
        {
            try
            {

                List<TacticalDeployZone> topsideDeployZones =
                    new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).Where(tdz => tdz.Pos.y > 4).ToList());

                return topsideDeployZones;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static List<TacticalDeployZone> GetEnemyDeployZones(TacticalLevelController controller)
        {
            try
            {

                List<TacticalDeployZone> enemyDeployZones =
                    new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).Where(tdz => tdz.MissionParticipant.Equals(TacMissionParticipant.Intruder)).ToList());

                return enemyDeployZones;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        internal static List<TacticalDeployZone> GetTunnelDeployZones(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> tunnelDeployZones = new List<TacticalDeployZone>();
                List<TacticalDeployZone> enemyDZs = GetEnemyDeployZones(controller);
                Dictionary<TacticalDeployZone, float> deployZonesAndDistance = new Dictionary<TacticalDeployZone, float>();

                foreach (TacticalDeployZone zone in GetAllBottomDeployZones(controller))
                {
                    foreach (TacticalDeployZone enemyDz in enemyDZs)
                    {
                        float magnitude = (zone.Pos - enemyDz.Pos).HorizontalMagnitude();
                        // TFTVLogger.Always($"{zone.Pos} - {enemyDz.Pos} has distance of {magnitude}");
                        if (!deployZonesAndDistance.ContainsKey(zone) && magnitude > 50)
                        {
                            deployZonesAndDistance.Add(zone, magnitude);
                        }
                        else if (magnitude > 50)
                        {
                            if (magnitude > deployZonesAndDistance[zone])
                            {
                                deployZonesAndDistance[zone] = magnitude;
                            }
                        }
                    }
                }

                Dictionary<TacticalDeployZone, float> sortedDeployZonesAndDistance = deployZonesAndDistance.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                int count = 0;
                foreach (KeyValuePair<TacticalDeployZone, float> entry in sortedDeployZonesAndDistance)
                {
                    tunnelDeployZones.Add(entry.Key);
                    count++;
                    if (count == 3)
                    {
                        break;
                    }
                }

                //   TFTVLogger.Always($"Tunnel deploy zone 1 is {tunnelDeployZones[0].Pos}");
                //   TFTVLogger.Always($"Tunnel deploy zone 2 is {tunnelDeployZones[1].Pos}");
                //   TFTVLogger.Always($"Tunnel deploy zone 3 is {tunnelDeployZones[2].Pos}");

                return tunnelDeployZones;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static List<TacticalDeployZone> GetCenterSpaceDeployZones(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> topsideDeployZones = GetTopsideDeployZones(controller);
                List<TacticalDeployZone> allBottomDeployZones = GetAllBottomDeployZones(controller);
                List<TacticalDeployZone> possibleCenterSpaceDeployZones = new List<TacticalDeployZone>();
                List<TacticalDeployZone> centerSpaceDeployZones = new List<TacticalDeployZone>();

                float maxDistancePrimaryCheck = 20;

                int requiredDistanceChecks = topsideDeployZones.Count();
                //  TFTVLogger.Always($"#Required distance checks is " + requiredDistanceChecks);

                foreach (TacticalDeployZone zone in allBottomDeployZones)
                {

                    int currentDistanceChecks = 0;


                    foreach (TacticalDeployZone topSideZone in topsideDeployZones)
                    {
                        float magnitude = (zone.Pos - topSideZone.Pos).HorizontalMagnitude();

                        if (magnitude <= maxDistancePrimaryCheck)
                        {
                            currentDistanceChecks += 1;

                        }

                        /*    TFTVLogger.Always($"{topSideZone.Pos} topside, compared to {zone.Pos}, magnitude is {magnitude}");
                            if (magnitude <= distance)
                            {
                                TFTVLogger.Always($"Check should be passed, count now {currentDistanceChecks}");
                            }    */
                    }

                    if (currentDistanceChecks >= requiredDistanceChecks)
                    {
                        possibleCenterSpaceDeployZones.Add(zone);

                    }

                }

                int requiredSecondaryChecks = possibleCenterSpaceDeployZones.Count();
                float maxDistanceSecondaryCheck = 16;

                foreach (TacticalDeployZone zone in possibleCenterSpaceDeployZones)
                {
                    int currentChecks = 0;

                    foreach (TacticalDeployZone tacticalDeployZone in possibleCenterSpaceDeployZones)
                    {
                        float magnitude = (zone.Pos - tacticalDeployZone.Pos).HorizontalMagnitude();
                        if (magnitude <= maxDistanceSecondaryCheck)
                        {
                            currentChecks += 1;
                        }

                    }
                    if (currentChecks >= requiredSecondaryChecks)
                    {
                        centerSpaceDeployZones.Add(zone);
                        // TFTVLogger.Always($"The zone at position {zone.Pos} is a center zone");
                    }
                }

                return centerSpaceDeployZones;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static List<TacticalDeployZone> GetAllBottomDeployZones(TacticalLevelController controller)
        {
            try
            {

                List<TacticalDeployZone> centerSpaceDeployZones =
                    new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).Where(tdz => tdz.Pos.y < 4).ToList());

                return centerSpaceDeployZones;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static bool CheckIfBaseDefense(TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.MissionData.MissionType.MissionTypeTag == DefCache.GetDef<MissionTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef"))
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

        internal static bool CheckAttackVectorForUmbra(TacticalActor tacticalActor, Vector3 pos)
        {
            try
            {
                bool canAttack = false;

                if (tacticalActor.Pos.y - pos.y < 2 && (tacticalActor.Pos - pos).magnitude < 15)
                {
                    TFTVLogger.Always($"{tacticalActor.DisplayName} is at {tacticalActor.Pos} and postion checked vs is {pos}");
                    canAttack = true;

                }

                return canAttack;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void UmbraStrat(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always("Umbra strat deploying");

                TacCharacterDef crabUmbra = DefCache.GetDef<TacCharacterDef>("Oilcrab_TacCharacterDef");
                TacCharacterDef fishUmbra = DefCache.GetDef<TacCharacterDef>("Oilfish_TacCharacterDef");

                List<TacCharacterDef> enemies = new List<TacCharacterDef>() { crabUmbra, fishUmbra };
                List<TacticalDeployZone> allDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null));
                List<TacticalActor> infectedPhoenixOperatives = new List<TacticalActor>();
                Dictionary<TacticalActor, TacticalDeployZone> targetablePhoenixOperatives = new Dictionary<TacticalActor, TacticalDeployZone>();

                foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("PX").TacticalActors)
                {
                    if ((tacticalActor.CharacterStats.Corruption != null && tacticalActor.CharacterStats.Corruption > 0)
                            || tacticalActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist))
                    {
                        infectedPhoenixOperatives.Add(tacticalActor);
                       // TFTVLogger.Always($"tactical actor added to list is {tacticalActor.DisplayName}");
                    }
                }



                if (infectedPhoenixOperatives.Count > 0)
                {
                    foreach (TacticalActor tacticalActor in infectedPhoenixOperatives)
                    {
                        foreach (TacticalDeployZone tacticalDeployZone in allDeployZones)
                        {
                            if (CheckAttackVectorForUmbra(tacticalActor, tacticalDeployZone.Pos))
                            {

                                if (!targetablePhoenixOperatives.ContainsKey(tacticalActor))
                                {
                                    targetablePhoenixOperatives.Add(tacticalActor, tacticalDeployZone);

                                }
                                else
                                {
                                    if ((tacticalActor.Pos - targetablePhoenixOperatives[tacticalActor].Pos).magnitude > (tacticalActor.Pos - tacticalDeployZone.Pos).magnitude)
                                    {
                                        targetablePhoenixOperatives[tacticalActor] = tacticalDeployZone;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (TacticalActor pXOperative in targetablePhoenixOperatives.Keys)
                {
                 

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                    int roll = UnityEngine.Random.Range(1, 11 + controller.Difficulty.Order);

                    TacticalDeployZone zone = targetablePhoenixOperatives[pXOperative];

                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                    Vector3 position = zone.Pos;
                    if (position.y <= 2 && position.y!=1.2) 
                    {
                        position.SetY(1.2f);  
                    }
                    else if (position.y>4 && position.y != 4.8) 
                    {
                        position.SetY(4.8f);
                    
                    }
                   

                    MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");
                    //spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Empty, zone.Pos + Vector3.up * -1.5f, 3, 1, false, true });

                    TFTVLogger.Always($"pXOperative to be ghosted {pXOperative.DisplayName} at pos {position}");
                    spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, position, 3, 1, false, true });

                    // SpawnBlob_Internal(TacticalVoxelType type, Vector3 pos, int horizontalRadius, int height, bool circular, bool updateMatrix = true)


                    if (roll > 6)
                    {


                        TacCharacterDef chosenEnemy = enemies.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                        zone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found deployzone and deploying " + chosenEnemy.name + $"; Position is y={zone.Pos.y} x={zone.Pos.x} z={zone.Pos.z}");
                        ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                        actorDeployData.InitializeInstanceData();
                        zone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zone);
                    }
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void MyrmidonAssaultStrat(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always("Myrmidon Assault Strat deploying");

                ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                List<TacticalDeployZone> tacticalDeployZones = GetTopsideDeployZones(controller);


                List<TacCharacterDef> myrmidons =
                    new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(myrmidonTag)));

                foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                {
                    int rollCap = controller.Difficulty.Order - 1;

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int myrmidonsToDeploy = UnityEngine.Random.Range(1, rollCap);

                    for (int x = 0; x < myrmidonsToDeploy; x++)
                    {

                        TacCharacterDef chosenMyrmidon = myrmidons.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found topside deployzone position and deploying " + chosenMyrmidon.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = chosenMyrmidon.GenerateActorDeployData();



                        actorDeployData.InitializeInstanceData();

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                    }

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void WormDropStrat(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always("WormDropStrat deploying");

                ClassTagDef wormTag = DefCache.GetDef<ClassTagDef>("Worm_ClassTagDef");
                List<TacticalDeployZone> tacticalDeployZones = GetTopsideDeployZones(controller);
                tacticalDeployZones.AddRange(GetCenterSpaceDeployZones(controller));


                List<TacCharacterDef> worms =
                    new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(wormTag)));

                foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                {
                    int rollCap = controller.Difficulty.Order - 1;

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int wormsToDeploy = UnityEngine.Random.Range(1, rollCap);
                    TacCharacterDef chosenWormType = worms.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                    for (int x = 0; x < wormsToDeploy; x++)
                    {
                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = chosenWormType.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                    }

                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void SpawnSecondaryForce(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always("Spwaning Secondary Force");

                List<TacticalDeployZone> tacticalDeployZones = GetAllBottomDeployZones(controller);
                List<TacticalActor> pxOperatives = controller.GetFactionByCommandName("px").TacticalActors.ToList();

                List<TacticalDeployZone> culledTacticalDeployZones = new List<TacticalDeployZone>();
                List<TacticalDeployZone> preferableDeploymentZone = new List<TacticalDeployZone>();
                TacticalDeployZone zoneToDeployAt = new TacticalDeployZone();

                foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                {
                    if (!CheckLOSToPlayer(controller, tacticalDeployZone.Pos))
                    {
                        culledTacticalDeployZones.Add(tacticalDeployZone);
                    }
                }

                if (culledTacticalDeployZones.Count > 0)
                {
                    foreach (TacticalDeployZone tunnelZone in GetTunnelDeployZones(controller))
                    {
                        if (culledTacticalDeployZones.Contains(tunnelZone))
                        {
                            preferableDeploymentZone.Add(tunnelZone);

                        }
                    }

                    if (preferableDeploymentZone.Count > 0)
                    {

                        zoneToDeployAt = preferableDeploymentZone.GetRandomElement();

                    }
                    else
                    {

                        zoneToDeployAt = culledTacticalDeployZones.GetRandomElement();


                    }
                }
                else
                {

                    zoneToDeployAt = tacticalDeployZones.GetRandomElement();


                }

                Dictionary<TacCharacterDef, int> secondaryForce = GenerateSecondaryForce(controller);

                GenerateFakeExplosion(zoneToDeployAt.Pos);

                foreach (TacCharacterDef tacCharacterDef in secondaryForce.Keys)
                {
                    for (int i = 0; i < secondaryForce[tacCharacterDef]; i++)
                    {

                        zoneToDeployAt.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();

                        zoneToDeployAt.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zoneToDeployAt);

                    }



                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        internal static Dictionary<TacCharacterDef, int> GenerateSecondaryForce(TacticalLevelController controller)
        {
            try
            {
                TFTVLogger.Always("Generating Secondary Force");

                Dictionary<ClassTagDef, int> reinforcements = TFTVUmbra.PickReinforcements(controller);
                Dictionary<TacCharacterDef, int> secondaryForce = new Dictionary<TacCharacterDef, int>();

                TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");

                int difficulty = controller.Difficulty.Order;

                List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                foreach (TacCharacterDef tacCharacterDef in availableTemplatesOrdered)
                {
                    if (tacCharacterDef.ClassTag != null && !secondaryForce.ContainsKey(tacCharacterDef)
                        && reinforcements.ContainsKey(tacCharacterDef.ClassTag) && reinforcements[tacCharacterDef.ClassTag] > 0)
                    {
                        secondaryForce.Add(tacCharacterDef, 1);
                        reinforcements[tacCharacterDef.ClassTag] -= 1;
                        //   TFTVLogger.Always("Added " + tacCharacterDef.name + " to the Seconday Force");

                    }
                }

                secondaryForce.Add(mindFragger, difficulty);

                return secondaryForce;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static bool CheckLOSToPlayer(TacticalLevelController contoller, Vector3 pos)
        {
            try
            {
                bool lOS = false;

                TacCharacterDef siren = DefCache.GetDef<TacCharacterDef>("Siren1_Basic_AlienMutationVariationDef");
                List<TacticalActor> phoenixOperatives = new List<TacticalActor>(contoller.GetFactionByCommandName("PX").TacticalActors);

                ActorDeployData actorDeployData = siren.GenerateActorDeployData();
                actorDeployData.InitializeInstanceData();

                foreach (TacticalActor actor in phoenixOperatives)
                {
                    TacticalActorBase actorBase = actor;

                    if (TacticalFactionVision.CheckVisibleLineBetweenActorsInTheory(actorBase, actorBase.Pos, actorDeployData.ComponentSetDef, pos) && (actor.Pos - pos).magnitude < 30)
                    {
                        lOS = true;
                        return lOS;
                    }
                }
                return lOS;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void CheckDepolyZones(TacticalLevelController controller)
        {
            TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
            TacCharacterDef basicMyrmidon = DefCache.GetDef<TacCharacterDef>("Swarmer_AlienMutationVariationDef");
            TacCharacterDef fireWorm = DefCache.GetDef<TacCharacterDef>("Fireworm_AlienMutationVariationDef");
            TacCharacterDef crystalScylla = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");
            TacCharacterDef poisonMyrmidon = DefCache.GetDef<TacCharacterDef>("SwarmerVenomous_AlienMutationVariationDef");
            TacCharacterDef meleeChiron = DefCache.GetDef<TacCharacterDef>("MeleeChiron");


            TFTVLogger.Always($"MissionDeployment.CheckDepolyZones() called ...");
            MissionDeployCondition missionDeployConditionToAdd = new MissionDeployCondition()
            {
                MissionData = new MissionDeployConditionData()
                {
                    ActivateOnTurn = 0,
                    DeactivateAfterTurn = 0,
                    ActorTagDef = TFTVMain.Main.DefCache.GetDef<ActorDeploymentTagDef>("Queen_DeploymentTagDef"),
                    ExcludeActor = false
                }
            };

            int numberOfSecondaryForces = controller.Difficulty.Order / 2;

            List<TacticalDeployZone> usedZones = new List<TacticalDeployZone>();

            TFTVLogger.Always($"The map has {controller.Map.GetActors<TacticalDeployZone>(null).ToList().Count} deploy zones");
            foreach (TacticalDeployZone tacticalDeployZone in controller.Map.GetActors<TacticalDeployZone>(null).ToList())
            {
                /*  TFTVLogger.Always($"Deployment zone {tacticalDeployZone} with Def '{tacticalDeployZone.TacticalDeployZoneDef}'");
                  TFTVLogger.Always($"Mission participant is {tacticalDeployZone.MissionParticipant}");
                  TFTVLogger.Always($"Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");*/


                if (tacticalDeployZone.MissionParticipant == TacMissionParticipant.Player && !usedZones.Contains(tacticalDeployZone))
                {

                    if (tacticalDeployZone.Pos.y > 4)
                    {
                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found topside deployzone position and deploying basic myrmidon; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = basicMyrmidon.GenerateActorDeployData();

                        actorDeployData.InitializeInstanceData();
                        usedZones.Add(tacticalDeployZone);
                        //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                    }
                    else if (tacticalDeployZone.Pos.y > 4)
                    {
                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found topside deployzone position and deploying mindfragger; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = mindFragger.GenerateActorDeployData();
                        usedZones.Add(tacticalDeployZone);

                        actorDeployData.InitializeInstanceData();

                        //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                    }
                    else if (tacticalDeployZone.Pos.y > 4)
                    {
                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found topside deployzone position and deploying fireworm; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = fireWorm.GenerateActorDeployData();
                        usedZones.Add(tacticalDeployZone);

                        actorDeployData.InitializeInstanceData();

                        //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                    }



                    if (tacticalDeployZone.Pos.y < 4)
                    {
                        tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                        TFTVLogger.Always($"Found bottom deployzone position and deploying mindfragger; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                        ActorDeployData actorDeployData = meleeChiron.GenerateActorDeployData();
                        usedZones.Add(tacticalDeployZone);
                        actorDeployData.InitializeInstanceData();

                        //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                        tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                    }
                }
            }
        }
    }
}
/*
internal static void AlternativeMistAndUmbraStrat(TacticalLevelController controller)
{
    try
    {
        TacCharacterDef crabUmbra = DefCache.GetDef<TacCharacterDef>("Oilcrab_TacCharacterDef");
        TacCharacterDef fishUmbra = DefCache.GetDef<TacCharacterDef>("Oilfish_TacCharacterDef");
        // TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");


        List<TacCharacterDef> enemies = new List<TacCharacterDef>() { crabUmbra, fishUmbra };

        List<TacticalDeployZone> topsideDeployZones = GetTopsideDeployZones(controller);


        int umbraCount = controller.Difficulty.Order * 2;
        List<TacticalActor> pandoransInValidPositions = new List<TacticalActor>();
        List<TacticalVoxel> tacticalVoxels = new List<TacticalVoxel>();

        foreach (TacticalDeployZone zone in topsideDeployZones)
        {

            TacticalVoxel startingVoxel = controller.VoxelMatrix.GetVoxel(zone.Pos);
            List<TacticalVoxel> voxels = new List<TacticalVoxel> { startingVoxel };
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(0.5f, 2, 0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-0.5f, 2, -0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-0.5f, 2, 0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(0.5f, 2, -0.5f)));

            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(1, 2, 1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-1, 2, -1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(1, 2, -1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-1, 2, 1)));

            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(0.5f, 2.5f, 0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-0.5f, 2.5f, -0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-0.5f, 2.5f, 0.5f)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(0.5f, 2.5f, -0.5f)));

            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(1, 2.5f, 1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-1, 2.5f, -1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(1, 2.5f, -1)));
            tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(startingVoxel.Position + new Vector3(-1, 2.5f, 1)));

        }

        foreach (TacticalVoxel tacticalVoxel in tacticalVoxels)
        {
            if (tacticalVoxel != null)
            {
                tacticalVoxel.SetVoxelType(TacticalVoxelType.Mist);

            }
        }

        //  TFTVLogger.Always("Got here");

        foreach (TacticalDeployZone deployZone in topsideDeployZones)
        {
            for (int x = 0; x < umbraCount / 3; x++)
            {

                TacCharacterDef chosenEnemy = enemies.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                deployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                TFTVLogger.Always($"Found deployzone and deploying " + chosenEnemy.name + $"; Position is y={deployZone.Pos.y} x={deployZone.Pos.x} z={deployZone.Pos.z}");
                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                actorDeployData.InitializeInstanceData();
                deployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, deployZone);
            }
        }
    }
    catch (Exception e)
    {
        TFTVLogger.Error(e);
    }


    
    internal static void MistAndUmbraStrat(TacticalLevelController controller)
    {
        try
        {
            TacCharacterDef crabUmbra = DefCache.GetDef<TacCharacterDef>("Oilcrab_TacCharacterDef");
            TacCharacterDef fishUmbra = DefCache.GetDef<TacCharacterDef>("Oilfish_TacCharacterDef");
            // TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");


            List<TacCharacterDef> enemies = new List<TacCharacterDef>() { crabUmbra, fishUmbra };

            TacticalDeployZone deployZone = controller.Map.GetActors<TacticalDeployZone>(null).Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player).ToList().First();

            List<TacticalActor> pandorans = new List<TacticalActor>(controller.GetFactionByCommandName("ALN").GetOwnedActors<TacticalActor>().Where(ta => ta.IsAlive).ToList());

            int umbraCount = controller.Difficulty.Order * 2;
            List<TacticalActor> pandoransInValidPositions = new List<TacticalActor>();
            List<TacticalVoxel> tacticalVoxels = new List<TacticalVoxel>();

            foreach (TacticalActor pandoran in pandorans)
            {

                List<TacticalVoxel> voxels = controller.VoxelMatrix.GetVoxels(pandoran.TacticalPerceptionBase.GetBounds()).ToList();
                foreach (TacticalVoxel voxel in voxels)
                {
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, 0.5f)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, -0.5f)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, 0.5f)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, -0.5f)));

                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, 1)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, -1)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, -1)));
                    tacticalVoxels.Add(controller.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, 1)));


                }
            }

            foreach (TacticalVoxel tacticalVoxel in tacticalVoxels)
            {
                if (tacticalVoxel != null)
                {
                    tacticalVoxel.SetVoxelType(TacticalVoxelType.Mist);

                }
            }

            //  TFTVLogger.Always("Got here");

            for (int x = 0; x <= umbraCount; x++)
            {
                TFTVLogger.Always("Spawning");
                TacticalActor tacticalActor = pandorans.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                TacCharacterDef chosenEnemy = enemies.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                deployZone.SetPosition(tacticalActor.Pos + new Vector3(0.5f, 0, 0.5f));
                deployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                TFTVLogger.Always($"Found deployzone and deploying " + chosenEnemy.name + $"; Position is y={deployZone.Pos.y} x={deployZone.Pos.x} z={deployZone.Pos.z}");
                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                actorDeployData.InitializeInstanceData();
                deployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, deployZone);
                umbraCount -= 1;
            }
        }
        catch (Exception e)
        {
            TFTVLogger.Error(e);
        }


    }*/

/* [HarmonyPatch(typeof(KillActorFactionObjective), "EvaluateObjective")]
 public static class TFTV_KillActorFactionObjective_EvaluateObjective_Patch
 {
     public static void Prefix(FactionObjective __instance, FactionObjectiveState __result)
     {
         try
         {
             TFTVLogger.Always($"{__instance.Description.LocalizationKey} {__result}");

             if (__instance.Description.LocalizationKey == "BASEDEFENSE_INFESTATION_OBJECTIVE" && __result == FactionObjectiveState.Achieved)
             {
                 TFTVLogger.Always("Got passed if check");

                 TacticalLevelController controller = __instance.Level;

                 GameTagDef gameTag = DefCache.GetDef<GameTagDef>("ScatterRemainingAttackers_GameTagDef");

                 foreach (TacticalActorBase tacticalActorBase in controller.GetFactionByCommandName("aln").Actors) 
                 { 

                     if(tacticalActorBase is TacticalActor tacticalActor && tacticalActor.CharacterStats.Endurance > 10) 
                     {

                         if (!tacticalActorBase.GameTags.Contains(gameTag)) 
                         {
                             tacticalActorBase.GameTags.Add(gameTag);
                             TFTVLogger.Always($"{tacticalActorBase.name} got the {gameTag.name}");

                         }

                     }

                 }


             }

         }

         catch (Exception e)
         {
             TFTVLogger.Error(e);
             throw;
         }
     }
 }*/


/*  [HarmonyPatch(typeof(TacticalDeployZone), "GetOrderedSpawnPositions")]

  public static class TacticalActor_OnFinishedMovingActor_Experiment_patch
  {
      public static void Postfix(ref List<Vector3> __result)
      {
          try
          {
              if (SpawningWorms)
              {
                  List<Vector3> spawnPositions = new List<Vector3>(__result);

                  foreach(Vector3 vector3 in spawnPositions) 
                  {
                      vector3.SetY(8);
                      TFTVLogger.Always("Checking y position " + vector3.y);
                  }

                  __result=spawnPositions;
              }
          }

          catch (Exception e)
          {
              TFTVLogger.Error(e);
          }

      }
  }*/

