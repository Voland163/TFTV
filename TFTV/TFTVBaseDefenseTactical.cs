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
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Destruction;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.Prompts;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVBaseDefenseTactical
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static float TimeLeft = 0;

        public static Dictionary<float, float> ConsolePositions = new Dictionary<float, float>();


        public static int StratToBeAnnounced = 0;
        public static int StratToBeImplemented = 0;

        private static readonly string ConsoleName = "BaseDefenseConsole";

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly GameTagDef InfestationSecondObjectiveTag = DefCache.GetDef<GameTagDef>("ScatterRemainingAttackers_GameTagDef");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
        private static readonly ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");


        private static readonly DelayedEffectStatusDef reinforcementStatusUnder1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatus1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatus1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatusUnder2AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder2AP]");

        public static bool TutorialPhoenixBase = false;
        //Method to set situation at start of base defense; currently only for alien assault

        private static Vector3 PlayerSpawn0 = new Vector3(-15.5f, 1.2f, 13.5f);
        private static Vector3 PlayerSpawn1 = new Vector3(-12f, 1.2f, 9.5f);

        private static Vector3 ReinforcementSpawn0 = new Vector3(-9.5f, 4.8f, 60.5f);
        private static Vector3 ReinforcementSpawn1 = new Vector3(-9.5f, 4.8f, 59.5f);
        private static Vector3 ReinforcementSpawn2 = new Vector3(-9.5f, 4.8f, 58.5f);

        // private static List<Vector3> PlayerSpawns = new List<Vector3>() { PlayerSpawn1, PlayerSpawn3 };
        private static readonly List<Vector3> ReinforcementSpawns = new List<Vector3>() { ReinforcementSpawn0, ReinforcementSpawn1, ReinforcementSpawn2 };

        public static bool VentingHintShown = false;

        //Patch that invokes StratPicker method, and therefore sets the chain that will lead to announcement of strat, showing hint, spawning interaction points, etc
        //also invokes strat implementer, which will actually materialize the reinforcements

        public static bool[] UsedStrats = new bool[5];
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

        internal class Objectives
        {
            public static void OjectivesDebbuger(TacticalLevelController controller)
            {
                try
                {
                    IEnumerable<TacticalActorBase> allPandorans = from x in controller.Map.GetActors<TacticalActorBase>()
                                                                  where x.HasGameTag(InfestationSecondObjectiveTag)
                                                                  select x;
                    if (allPandorans.Count() > 0)
                    {
                        foreach (TacticalActorBase tacticalActor in allPandorans)
                        {
                            if (tacticalActor.IsOffMap)
                            {
                                TFTVLogger.Always($"this Pandoran {tacticalActor.name} is OffMap. Removing KillObjective tag.");
                                tacticalActor.GameTags.Remove(InfestationSecondObjectiveTag);

                            }

                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            [HarmonyPatch(typeof(KillActorFactionObjective), "GetTargets")]
            public static class TFTV_KillActorFactionObjective_GetTargets_Patch
            {
                public static void Postfix(FactionObjective __instance, ref IEnumerable<TacticalActorBase> __result)
                {
                    try
                    {
                        if (__instance.Description.LocalizationKey == "BASEDEFENSE_INFESTATION_OBJECTIVE" && __result.Count() > 0)
                        {
                            //  TFTVLogger.Always("Got passed if check");

                            List<TacticalActorBase> actorsToKeep = new List<TacticalActorBase>();

                            foreach (TacticalActorBase tacticalActorBase in __result)
                            {
                                if (!tacticalActorBase.Status.HasStatus<MindControlStatus>())
                                {
                                    //  
                                    actorsToKeep.Add(tacticalActorBase);
                                }
                                else
                                {
                                    TFTVLogger.Always($"{tacticalActorBase.name} has mind controlled status!");

                                }
                            }

                            __result = actorsToKeep;

                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            public static void RemoveScatterRemainingAttackersTagFromEnemiesWithParasychosis(TacticalAbility ability, object parameter)
            {
                try
                {

                    ApplyEffectAbilityDef parasychosis = DefCache.GetDef<ApplyEffectAbilityDef>("Parasychosis_AbilityDef");
                    GameTagDef infestationSecondObjectiveTag = DefCache.GetDef<GameTagDef>("ScatterRemainingAttackers_GameTagDef");

                    if (ability.TacticalAbilityDef == parasychosis && parameter is TacticalAbilityTarget target
                        && target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.HasGameTag(infestationSecondObjectiveTag))
                    {
                        //  TFTVLogger.Always($", target is {tacticalActor.name}");
                        tacticalActor.GameTags.Remove(infestationSecondObjectiveTag);

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }



            [HarmonyPatch(typeof(TacticalFaction), "HasUndeployedTacActors")]
            public static class SurviveTurnsFactionObjective_HasUndeployedTacActors_BaseDefense_Patch
            {
                public static void Postfix(TacticalFaction __instance, ref bool __result)
                {
                    try
                    {
                        if (__instance.Faction.FactionDef == Shared.AlienFactionDef && __result == false)
                        {
                            FactionObjective survive5turns = __instance.TacticalLevel.GetFactionByCommandName("px").Objectives.FirstOrDefault(o => o.Description.LocalizationKey == "BASEDEFENSE_SURVIVE5_OBJECTIVE");

                            if (survive5turns != null && survive5turns.State != FactionObjectiveState.Achieved)

                            {
                                //   TFTVLogger.Always($"Survive 5 turns active, so result {__result} is being changed to true");
                                __result = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            //Method to add objective tag on Pandorans for the Scatter Attackers objective
            //Doesn't activate if Pandoran faction not present

            public static void AddScatterObjectiveTagForBaseDefense(TacticalActorBase actor, TacticalLevelController __instance)
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

            public static void ModifyBaseDefenseTacticalObjectives(TacMissionTypeDef missionType)
            {
                try
                {

                    GameTagDef baseDefense = DefCache.GetDef<GameTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");
                    GameTagDef havenDefense = DefCache.GetDef<GameTagDef>("MissionTypeHavenDefense_MissionTagDef");

                    if (missionType.Tags.Contains(baseDefense) && missionType.ParticipantsData[0].FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef"))
                    {

                        TFTVLogger.Always("ModifyBaseDefenseObjectives");
                        List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                        KillActorFactionObjectiveDef killSpawnery = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [PhoenixBaseInfestation]");
                        KillActorFactionObjectiveDef killSentinel = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [PhoenixBaseDestroySentinel]");
                        SurviveTurnsFactionObjectiveDef survive3turns = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveThreeTurns");
                        SurviveTurnsFactionObjectiveDef survive5turns = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveFiveTurns");

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

                        if (TimeLeft < 6)
                        {
                            if (!listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Add(killSpawnery);
                            }
                            if (listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Remove(killSentinel);
                            }
                            if (listOfFactionObjectives.Contains(survive3turns))
                            {
                                listOfFactionObjectives.Remove(survive3turns);
                            }

                        }
                        else if (TimeLeft < 12  && TimeLeft >= 6)
                        {
                            if (!listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Add(killSentinel);

                            }
                            if (listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Remove(killSpawnery);
                            }
                            if (!listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Remove(survive5turns);
                            }
                        }
                        else
                        {
                            if (!listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Add(survive5turns);
                            }
                            if (listOfFactionObjectives.Contains(survive3turns))
                            {
                                listOfFactionObjectives.Remove(survive3turns);
                            }
                            if (listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Remove(killSentinel);
                            }
                            if (listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Remove(killSpawnery);
                            }
                        }

                        missionType.CustomObjectives = listOfFactionObjectives.ToArray();
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void ModifyMissionDataBaseDefense(GeoMission geoMission, TacMissionData missionData)
            {
                try
                {
                    if (TFTVBaseDefenseGeoscape.PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId) || TFTVBaseDefenseGeoscape.PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                    {

                        PPFactionDef alienFaction = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                        int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(geoMission.GameController.CurrentDifficulty.Order);

                        ContextHelpHintDef hintDef = DefCache.GetDef<ContextHelpHintDef>("TFTVBaseDefense");

                        int timer = 0;

                        if (geoMission.Site.ExpiringTimerAt != 0)
                        {
                            timer = (geoMission.Site.ExpiringTimerAt.DateTime - geoMission.Level.Timing.Now.DateTime).Hours;
                        }

                       /* float timeToCompleteAttack = 18;

                        if (TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach.ContainsKey(geoMission.Site.SiteId))
                        {
                            timeToCompleteAttack = TFTVBaseDefenseGeoscape.PhoenixBasesContainmentBreach[geoMission.Site.SiteId];
                        }

                        float progress = 1f - timer / timeToCompleteAttack;

                        if (timeToCompleteAttack != 18 && progress < 0.3)
                        {
                            progress = 0.3f;
                        }*/

                        TFTVLogger.Always($"When modifying mission data, timer is {timer}");

                        string spriteFileName = "base_defense_hint.jpg";

                        if (timer < 12)
                        {
                            foreach (TacMissionFactionData tacMissionFactionData in missionData.MissionParticipants)
                            {
                                TFTVLogger.Always($"{tacMissionFactionData.FactionDef} {tacMissionFactionData.InitialDeploymentPoints}");

                                if (tacMissionFactionData.FactionDef == alienFaction)
                                {
                                    tacMissionFactionData.InitialDeploymentPoints *= 0.6f + (0.05f * difficulty);

                                    TFTVLogger.Always($"Deployment points changed to {tacMissionFactionData.InitialDeploymentPoints}");
                                }
                            }
                        }

                        if (timer >= 12)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION";
                            spriteFileName = "base_defense_hint.jpg";
                        }
                        else if (timer <12 && timer >= 6)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_NESTING_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_NESTING_DESCRIPTION";
                            spriteFileName = "base_defense_hint_nesting.jpg";
                        }
                        else
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_INFESTATION_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_INFESTATION_DESCRIPTION";
                            spriteFileName = "base_defense_hint_infestation.jpg";
                        }
                        TFTVHints._hintDefSpriteFileNameDictionary[hintDef] = spriteFileName;

                        TimeLeft = timer;
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }






            public static void AuxiliaryCheckForMissionAccomplished(TacticalLevelController controller)
            {
                try
                {

                    if (CheckIfBaseDefense(controller))
                    {
                        //need to check for completion of objectives...

                        ObjectivesManager phoenixObjectives = controller.GetFactionByCommandName("Px").Objectives;
                        // int difficulty = controller.Difficulty.Order;

                        // bool otherObjectivesCompleted = false;

                        foreach (FactionObjective objective in phoenixObjectives)
                        {
                            if (objective.Description.LocalizationKey != "BASEDEFENSE_SECOND_OBJECTIVE" && objective.GetCompletion() == 0)
                            {

                                TFTVLogger.Always($"the Phoenix objective is {objective.GetDescription()}; completion is at {objective.GetCompletion()}");

                                IEnumerable<TacticalActorBase> allPandorans = from x in controller.Map.GetActors<TacticalActorBase>()
                                                                              where x.HasGameTag(InfestationSecondObjectiveTag)
                                                                              select x;

                                if (allPandorans.Count() > 0)
                                {
                                    foreach (TacticalActorBase tacticalActor in allPandorans)
                                    {
                                        if (!tacticalActor.Status.HasStatus<ParalysedStatus>() && tacticalActor.IsAlive)
                                        {
                                            return;
                                        }
                                    }
                                }
                                else
                                {


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

        internal class Map
        {
            internal class Consoles
            {
                internal static void GetConsoles()
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CheckIfBaseDefense(controller))
                        {
                            List<Breakable> consoles = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("NJR_LoCov_Console")).ToList();
                            Vector3[] position = new Vector3[consoles.Count];

                            consoles = consoles.OrderByDescending(c => c.transform.position.z).ToList();

                            for (int x=0; x<consoles.Count; x++) 
                            {
                                if (consoles[x].transform.rotation.y == 1.0f) 
                                {
                                    ConsolePositions.Add(consoles[x].transform.position.z + 1, consoles[x].transform.position.x);
                                    TFTVLogger.Always($"there is a console at {consoles[x].transform.position} with rotation {consoles[x].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(x).Key} for z coordinate, and {ConsolePositions.ElementAt(x).Value} for x coordinate ");
                                }
                                else 
                                {
                                    ConsolePositions.Add(consoles[x].transform.position.z, consoles[x].transform.position.x+1);
                                    TFTVLogger.Always($"there is a console at {consoles[x].transform.position} with rotation {consoles[x].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(x).Key} for z coordinate, and {ConsolePositions.ElementAt(x).Value} for x coordinate ");
                                }      
                            }

                          /*  ConsolePositions.Add(consoles[0].transform.position.z + 1, consoles[0].transform.position.x);
                            TFTVLogger.Always($"there is a console at {consoles[0].transform.position} with rotation {consoles[0].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(0).Key} for z coordinate, and {ConsolePositions.ElementAt(0).Value} for x coordinate ");
                            ConsolePositions.Add(consoles[1].transform.position.z, consoles[1].transform.position.x + 1);
                            TFTVLogger.Always($"there is a console at {consoles[1].transform.position} with rotation {consoles[1].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(1).Key} for z coordinate, and {ConsolePositions.ElementAt(1).Value} for x coordinate ");
                            ConsolePositions.Add(consoles[2].transform.position.z, consoles[2].transform.position.x + 1);
                            TFTVLogger.Always($"there is a console at {consoles[2].transform.position} with rotation {consoles[2].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(2).Key} for z coordinate, and {ConsolePositions.ElementAt(2).Value} for x coordinate ");*/
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }

                internal class SpawnConsoles
                {
                    public static void PlaceObjectives(TacticalLevelController controller)
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
                            //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                            structuralTarget.DoEnterPlay();

                            //    TacticalActorBase

                            StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                            structuralTarget.Status.ApplyStatus(activeConsoleStatusDef);

                            TFTVLogger.Always($"{name} is at position {position}");
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
                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                            if (CheckIfBaseDefense(controller))
                            {
                                List<Breakable> consoles = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("NJR_LoCov_Console") && b.GetToughness()>0).ToList();
                                Vector3[] culledConsolePositions = new Vector3[3];

                                consoles = consoles.OrderByDescending(c => c.transform.position.z).ToList();
                                TFTVLogger.Always($"{consoles.Count} consoles were found");

                                if (ConsolePositions.Count() == 0)
                                {
                                    TFTVLogger.Always($"Console Position {ConsolePositions.Count()}! Reacquiring to avoid errors");
                                    GetConsoles();
                                }

                                TFTVLogger.Always($"Console Positions count: {ConsolePositions.Count()}");

                                for (int x = 0; x < ConsolePositions.Count; x++)
                                {
                                    foreach (Breakable breakable in consoles)
                                    {
                                        //  TFTVLogger.Always($"here this breakable {breakable.name}");

                                        //  TFTVLogger.Always($"Console positions? {ConsolePositions.Count()}");
                                        TFTVLogger.Always($"difference in x coordinates is {Math.Abs((int)(ConsolePositions.ElementAt(x).Value - breakable?.transform?.position.x))} and in z coordinates {Math.Abs((int)(ConsolePositions.ElementAt(x).Key - breakable?.transform?.position.z))}");

                                        if (Math.Abs((int)(ConsolePositions.ElementAt(x).Value - breakable?.transform?.position.x)) < 5 && Math.Abs((int)(ConsolePositions.ElementAt(x).Key - breakable?.transform?.position.z)) < 5)
                                        {
                                            TFTVLogger.Always($"Found breakable at position {breakable.transform.position}, close to interaction point at {ConsolePositions.ElementAt(x)}");
                                            culledConsolePositions[x].y = breakable.transform.position.y;
                                            culledConsolePositions[x].x = ConsolePositions.ElementAt(x).Value;
                                            culledConsolePositions[x].z = ConsolePositions.ElementAt(x).Key;
                                        }
                                    }
                                }

                                for (int x = 0; x < culledConsolePositions.Count(); x++)
                                {
                                    if (culledConsolePositions[x] != null && culledConsolePositions[x] != new Vector3(0, 0, 0))
                                    {
                                        SpawnInteractionPoint(culledConsolePositions[x], ConsoleName + x);
                                    }
                                }

                                ActivateConsole.CheckIfConsoleActivated(controller);
                            }
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }


                }

                internal class ActivateConsole
                {
                    public static void BaseDefenseConsoleActivated(StatusComponent statusComponent, Status status, TacticalLevelController controller)
                    {
                        try
                        {

                            if (controller != null && CheckIfBaseDefense(controller))
                            {
                                if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                                {
                                    StructuralTarget console = statusComponent.transform.GetComponent<StructuralTarget>();
                                    List<StructuralTarget> generators = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().Where(st => st.Deployment != null).Where(st => st.Deployment.name.Equals("PP_Cover_Generator_2x2_A_StructuralTarget")).ToList();

                                    TFTVLogger.Always($"Console {console.name} activated. Generators count: {generators.Count}");

                                    for (int i = 0; i < 3; i++)
                                    {
                                        TFTVLogger.Always($"Console {console.name}  {ConsoleName + i}? {ConsolePositions.ElementAt(i).Value} ");

                                        if (console.name.Equals(ConsoleName + i) && ConsolePositions.ElementAt(i).Value != 1000)
                                        {
                                            //  TFTVLogger.Always($"Console {console.name} activation logged");
                                            float keyToChange = ConsolePositions.ElementAt(i).Key;
                                            ConsolePositions[keyToChange] = 1000;

                                            StratToBeImplemented = 0;

                                            if (generators.Count > 0)
                                            {
                                                foreach (StructuralTarget structuralTarget in generators)
                                                {

                                                    TFTVLogger.Always($"Applying damage to generators: current health is {structuralTarget.GetHealth()}, reducing it by {60}");
                                                    structuralTarget.Health.Subtract(60);
                                                    TFTVLogger.Always($"Current health is {structuralTarget.GetHealth()}");

                                                }

                                                Explosions.GenerateRandomExplosions();
                                            }
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

                    [HarmonyPatch(typeof(TacticalPrompt), "Show")]
                    public static class TacticalPrompt_AddStatus_patch
                    {
                        public static void Prefix(TacticalPrompt __instance)
                        {
                            try
                            {
                                // TFTVLogger.Always($"Showing prompt {__instance.PromptDef.name}");

                                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                                TacticalPromptDef activateObjective = DefCache.GetDef<TacticalPromptDef>("ActivateObjectivePromptDef");
                                TacticalPromptDef consoleBaseDefenseObjective = DefCache.GetDef<TacticalPromptDef>("TFTVBaseDefensePrompt");

                                if (__instance.PromptDef == activateObjective && controller.TacMission.MissionData.UsePhoenixBaseLayout)
                                {
                                    //  TFTVLogger.Always("Got past the if on the prompt");
                                    __instance.PromptDef = consoleBaseDefenseObjective;
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

                            if (console != null)
                            {
                                TFTVLogger.Always($"Found console {console.name}");

                                Status status = console?.Status?.GetStatusByName(activeConsoleStatusDef.EffectName);

                                if (status != null)
                                {
                                    TFTVLogger.Always($"found status {status.Def.EffectName}");
                                    console.Status.UnapplyStatus(status);
                                }
                            }
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
                                for (int x = 0; x < ConsolePositions.Count(); x++)
                                {
                                    // TFTVLogger.Always($"{ConsoleInBaseDefense[x]}");

                                    if (ConsolePositions.ElementAt(x).Value == 1000)
                                    {
                                        DeactivateConsole(ConsoleName + x);
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

                internal class Explosions
                {
                    internal static void GenerateExplosion(Vector3 position)
                    {
                        try
                        {
                            DelayedEffectDef explosion = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");

                            Vector3 vector3 = new Vector3(position.x + UnityEngine.Random.Range(-4, 4), position.y, position.z + UnityEngine.Random.Range(-4, 4));


                            Effect.Apply(Repo, explosion, new EffectTarget
                            {
                                Position = vector3
                            }, null);

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



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

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



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

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



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

                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            List<TacticalDeployZone> zones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null));
                            zones.RemoveRange(Map.DeploymentZones.GetTopsideDeployZones(controller));

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

                }

                [HarmonyPatch(typeof(Breakable), "Explode")]
                public static class Breakable_Explode_Experiment_patch
                {
                    public static void Postfix(Breakable __instance)
                    {
                        try
                        {
                            if (__instance.name.StartsWith("NJR_LoCov_Console"))
                            {
                                for (int x = 0; x < ConsolePositions.Count; x++)
                                {
                                    TFTVLogger.Always($"difference in x coordinates is " +
                                        $"{Math.Abs((int)(ConsolePositions.ElementAt(x).Value - __instance.transform?.position.x))} " +
                                        $"and in z coordinates {Math.Abs((int)(ConsolePositions.ElementAt(x).Key - __instance.transform?.position.z))}");

                                    if (Math.Abs(ConsolePositions.ElementAt(x).Value - __instance.transform.position.x) < 5
                                        && Math.Abs(ConsolePositions.ElementAt(x).Key - __instance.transform.position.z) < 5)
                                    {
                                        float keyToChange = ConsolePositions.ElementAt(x).Key;
                                        ConsolePositions[keyToChange] = 1000;
                                        TFTVLogger.Always($"{ConsoleName + x} exploded!");
                                        Map.Consoles.ActivateConsole.DeactivateConsole(ConsoleName + x);
                                        TFTVLogger.Always($"{ConsoleName + x} deactivated!");
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

            internal class DeploymentZones
            {
                public static void InitDeployZonesForBaseDefenseVsAliens(TacticalLevelController controller)
                {
                    try
                    {
                        if (CheckIfBaseDefense(controller) && !controller.IsFromSaveGame)
                        {
                            TFTVLogger.Always("Initiating Deploy Zones for a base defense mission; not from save game");
                            StartingDeployment.Init(controller);
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

                    int numberOfSecondaryForces = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order) / 2;

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
            }

        }

        internal class StartingDeployment
        {
            public static void Init(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always($"Attack on base progress is {TimeLeft}");

                    TFTVLogger.Always($"Tutorial Base defense? {TutorialPhoenixBase}");

                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        if (TimeLeft < 6)
                        {
                            if (TutorialPhoenixBase)
                            {
                                PlayerDeployment.SetPlayerSpawnEntrance(controller);
                            }
                            else
                            {
                                PlayerDeployment.SetPlayerSpawnTunnels(controller);
                            }
                            PandoranDeployment.InfestationDeployment(controller);
                        }
                        else if (TimeLeft >= 6 && TimeLeft < 12)
                        {
                            if (TutorialPhoenixBase)
                            {
                                PlayerDeployment.SetPlayerSpawnEntrance(controller);
                            }
                            else
                            {
                                PlayerDeployment.SetPlayerSpawnTunnels(controller);
                            }
                            PandoranDeployment.NestingDeployment(controller);
                        }
                        else
                        {
                            PlayerDeployment.SetPlayerSpawnTopsideAndCenter(controller);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            internal class PandoranDeployment
            {
                internal static void SpawnAdditionalEggs(TacticalLevelController controller)
                {
                    try
                    {
                        TacCharacterDef acidWormEgg = DefCache.GetDef<TacCharacterDef>("Acidworm_Egg_AlienMutationVariationDef");
                        TacCharacterDef fraggerEgg = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef");
                        TacCharacterDef fireWormEgg = DefCache.GetDef<TacCharacterDef>("Fireworm_Egg_AlienMutationVariationDef");
                        TacCharacterDef poisonWormEgg = DefCache.GetDef<TacCharacterDef>("Poisonworm_Egg_AlienMutationVariationDef");
                        TacCharacterDef swarmerEgg = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_TacCharacterDef");

                        List<TacticalDeployZone> centralZones = Map.DeploymentZones.GetCenterSpaceDeployZones(controller);


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

                            int roll = UnityEngine.Random.Range(1, 11 + TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order));

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

                internal static void NestingDeployment(TacticalLevelController controller)
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

                        TacticalDeployZone centralZone = Map.DeploymentZones.FindCentralDeployZone(controller);
                        //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                        ActorDeployData spawneryDeployData = sentinels.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp())).GenerateActorDeployData();
                        spawneryDeployData.InitializeInstanceData();
                        TacticalActorBase sentinel = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);

                        sentinel.GameTags.Add(infestationTag);

                        List<TacticalDeployZone> otherCentralZones = Map.DeploymentZones.GetCenterSpaceDeployZones(controller);
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

                            int roll = UnityEngine.Random.Range(1, TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order));

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

                internal static void InfestationDeployment(TacticalLevelController controller)
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

                        TacticalDeployZone centralZone = Map.DeploymentZones.FindCentralDeployZone(controller);
                        //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                        ActorDeployData spawneryDeployData = spawneryDef.GenerateActorDeployData();
                        spawneryDeployData.InitializeInstanceData();
                        TacticalActorBase spawnery = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);
                        spawnery.GameTags.Add(infestationTag);

                        List<TacticalDeployZone> otherCentralZones = Map.DeploymentZones.GetCenterSpaceDeployZones(controller);
                        otherCentralZones.Remove(centralZone);


                        for (int i = 0; i < Mathf.Min(TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order), 4); i++)
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

                        foreach (TacticalDeployZone tacticalDeployZone in Map.DeploymentZones.GetEnemyDeployZones(controller))
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(1, 11 + TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order));


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

            }
            internal class PlayerDeployment
            {
                public static void SetPlayerSpawnEntrance(TacticalLevelController controller)
                {
                    try
                    {
                        List<TacticalDeployZone> allDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>());

                        TFTVLogger.Always($"Tutorial base mission; setting up player to spawn at entrance. AllDeployZones Count: {allDeployZones.Count}");

                        List<Vector3> reinforcementSpawns = new List<Vector3>(ReinforcementSpawns);

                        foreach (TacticalDeployZone tacticalDeployZone in allDeployZones)
                        {
                            TFTVLogger.Always($"located {tacticalDeployZone.name} at {tacticalDeployZone.Pos}");

                            if (tacticalDeployZone.Pos.z == 0.5f || tacticalDeployZone.Pos.z == 1f)
                            {

                                Vector3 vector3 = reinforcementSpawns.First();
                                TFTVLogger.Always($"located tdz at {tacticalDeployZone.Pos}; changing it to position {vector3}");
                                tacticalDeployZone.SetPosition(vector3);

                                reinforcementSpawns.Remove(vector3);
                            }
                            else if (tacticalDeployZone.Pos == PlayerSpawn0 || tacticalDeployZone.Pos == PlayerSpawn1)
                            {
                                TFTVLogger.Always($"Player spawn {tacticalDeployZone.name} at {tacticalDeployZone.Pos}");
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);
                                //
                            }

                            else if (tacticalDeployZone.Pos.z <= 21.5)
                            {
                                TFTVLogger.Always($"located tdz to be removed {tacticalDeployZone.Pos}");

                                tacticalDeployZone.gameObject.SetActive(false);

                            }
                            else
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                            }
                        }
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
                        List<TacticalDeployZone> playerDeployZones = Map.DeploymentZones.GetTunnelDeployZones(controller);
                        List<TacticalDeployZone> allPlayerDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).
                        Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player));


                        foreach (TacticalDeployZone deployZone in playerDeployZones)
                        {
                            // TFTVLogger.Always($"{deployZone.name} at {deployZone.Pos} is {deployZone.IsDisabled}");


                            List<MissionDeployConditionData> missionDeployConditionDatas = Map.DeploymentZones.GetTopsideDeployZones(controller).First().MissionDeployment;

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
                        List<TacticalDeployZone> playerDeployZones = Map.DeploymentZones.GetTopsideDeployZones(controller);
                        playerDeployZones.AddRange(Map.DeploymentZones.GetCenterSpaceDeployZones(controller));

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
            }

        }

        internal class PandoranTurn
        {
            public static void ImplementBaseDefenseVsAliensPreAITurn(TacticalFaction tacticalFaction)
            {
                try
                {
                    if (tacticalFaction.TacticalLevel.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        if (CheckIfBaseDefense(tacticalFaction.TacticalLevel) && tacticalFaction.Equals(tacticalFaction.TacticalLevel.GetFactionByCommandName("px")))
                        {
                            // StratPicker(tacticalFaction.TacticalLevel);

                            //These strats get implemented before alien turn starts: triton infiltration team and secondary force

                            if (StratToBeImplemented >= 4)
                            {
                                StratImplementer(tacticalFaction.TacticalLevel);
                            }

                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void ImplementBaseDefenseVsAliensPostAISortingOut(TacticalFaction tacticalFaction)
            {
                try
                {
                    if (tacticalFaction.TacticalLevel.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        if (CheckIfBaseDefense(tacticalFaction.TacticalLevel) && tacticalFaction.Equals(tacticalFaction.TacticalLevel.GetFactionByCommandName("aln")))
                        {
                            StratPicker(tacticalFaction.TacticalLevel);

                            //These strats get implemented before alien turn starts: triton infiltration team and secondary force

                            if (StratToBeImplemented < 4)
                            {
                                StratImplementer(tacticalFaction.TacticalLevel);
                            }
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
                        TFTVLogger.Always($"Strat to be implemented is {StratToBeImplemented}");

                        switch (StratToBeImplemented)
                        {
                            case 1:
                                {
                                    ReinforcementStrats.WormDropStrat(controller);
                                }
                                break;

                            case 2:
                                {
                                    ReinforcementStrats.MyrmidonAssaultStrat(controller);
                                }
                                break;

                            case 3:
                                {
                                    ReinforcementStrats.UmbraStrat(controller);
                                }
                                break;
                            case 4:
                                {
                                    ReinforcementStrats.SpawnSecondaryForce(controller);
                                }
                                break;
                            case 5:
                                {
                                    ReinforcementStrats.FishmanInfiltrationStrat(controller);
                                }
                                break;
                        }

                        StratToBeImplemented = 0;
                        TFTVLogger.Always($"Strat to be implemented now {StratToBeImplemented}, should be 0");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void StratPicker(TacticalLevelController controller)
            {
                try
                {
                    if (CheckIfBaseDefense(controller))
                    {
                        //need to check for completion of objectives...

                        ObjectivesManager phoenixObjectives = controller.GetFactionByCommandName("Px").Objectives;
                        int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order);

                        foreach (FactionObjective objective in phoenixObjectives)
                        {
                            TFTVLogger.Always($"Checking objectives {objective.Description.LocalizationKey} {objective.GetCompletion()} " +
                                $"at turn number {controller.TurnNumber}");

                            if (objective.Description.LocalizationKey == "BASEDEFENSE_SURVIVE5_OBJECTIVE" && objective.GetCompletion() == 0)
                            {
                                TFTVLogger.Always($"the objective is {objective.GetDescription()}; completion is at {objective.GetCompletion()}");
                                if (controller.TurnNumber >= 5 - difficulty && controller.TurnNumber < 5)
                                {
                                    List<int> availableStrats = new List<int>();

                                    for (int x = 0; x < UsedStrats.Count(); x++)
                                    {
                                        TFTVLogger.Always($"strat {x + 1} recorded as used? {UsedStrats[x] == true}");

                                        if (UsedStrats[x] == false)
                                        {
                                            availableStrats.Add(x + 1);
                                        }
                                    }

                                    TFTVLogger.Always($"available strat count: {availableStrats.Count}");

                                    if (availableStrats.Count > 0)
                                    {
                                        StratToBeAnnounced = availableStrats.GetRandomElement();
                                        UsedStrats[StratToBeAnnounced - 1] = true;
                                        TFTVLogger.Always($"the objective is {objective.GetDescription()} and the strat picked is {StratToBeAnnounced} and it can't be used again");
                                    }
                                    else //this will give one turn pause, and then reset all strats, making them available again
                                    {
                                        UsedStrats = new bool[5];
                                    }
                                }
                            }
                            else if ((objective.Description.LocalizationKey == "BASEDEFENSE_INFESTATION_OBJECTIVE"
                                || objective.Description.LocalizationKey == "BASEDEFENSE_SENTINEL_OBJECTIVE") && objective.GetCompletion() == 0)
                            {
                                TFTVLogger.Always($"the objective is {objective.GetDescription()}; completion is at {objective.GetCompletion()}");
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());



                                int roll = UnityEngine.Random.Range(1, 11 + TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order));


                                if (roll >= 7)
                                {
                                    List<int> availableStrats = new List<int>();

                                    for (int x = 0; x < UsedStrats.Count(); x++)
                                    {
                                        if (UsedStrats[x] == false)
                                        {
                                            availableStrats.Add(x + 1);

                                        }
                                    }

                                    if (availableStrats.Count > 0)
                                    {

                                        StratToBeAnnounced = availableStrats.GetRandomElement();

                                        UsedStrats[StratToBeAnnounced - 1] = true;
                                        TFTVLogger.Always($"the objective is {objective.GetDescription()} and the strat picked is {StratToBeAnnounced} and it can't be used again");

                                    }
                                    else//this will give one turn pause, and then reset all strats, making them available again
                                    {
                                        UsedStrats = new bool[5];
                                    }
                                }
                                else
                                {
                                    TFTVLogger.Always($"roll was {roll}, which is below 7! phew!");
                                    if (TimeLeft < 6)
                                    {
                                        StartingDeployment.PandoranDeployment.SpawnAdditionalEggs(controller);
                                        TFTVLogger.Always("But some more eggs should have spawned instead!");
                                    }
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

        internal class PlayerTurn
        {

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
                                    //   MyrmidonAssaultStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseWormsStrat");
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
                                    //  GenerateSecondaryForce(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseForce2Strat");
                                }
                                break;
                            case 5:
                                {
                                    //  UmbraStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseUmbraStrat");

                                }
                                break;
                        }


                        if (!hintManager.RegisterContextHelpHint(contextHelpHintDef, isMandatory: true, null))
                        {

                            ContextHelpHint item = new ContextHelpHint(contextHelpHintDef, isMandatory: true, null);

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

                            if (!hintManager.RegisterContextHelpHint(ventingHint, isMandatory: true, null))
                            {

                                ContextHelpHint item = new ContextHelpHint(ventingHint, isMandatory: true, null);

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
                            Map.Consoles.SpawnConsoles.InteractionPointPlacement();
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            //Method that does what needs to be done at start of Phoenix turn when defending vs Aliens
            public static void PhoenixBaseDefenseVSAliensTurnStart(TacticalLevelController controller, TacticalFaction tacticalFaction)
            {
                try
                {
                    if (!controller.IsLoadingSavedGame)
                    {
                        if (CheckIfBaseDefense(controller) && controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                        {
                            TacticalFaction alienFaction = controller.GetFactionByCommandName("aln");
                            TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                            Objectives.OjectivesDebbuger(controller);

                            if (tacticalFaction == phoenix && StratToBeAnnounced != 0)
                            {
                                StratAnnouncer(controller);
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

        internal class ReinforcementStrats
        {
            internal static bool CheckAttackVectorForUmbra(TacticalActor tacticalActor, Vector3 pos)
            {
                try
                {
                    bool canAttack = false;

                    if (tacticalActor.Pos.y - pos.y < 2 && (tacticalActor.Pos - pos).magnitude < 15)
                    {
                        //  TFTVLogger.Always($"{tacticalActor.DisplayName} is at {tacticalActor.Pos} and postion checked vs is {pos}");
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

                    for (int x = 0; x < targetablePhoenixOperatives.Keys.Count(); x++)
                    {
                        TacticalActor pXOperative = targetablePhoenixOperatives.Keys.ElementAt(x);

                        int deliriumScale = Math.Max((int)pXOperative.CharacterStats.Corruption / 2, 2) + 2 * x;

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                        int roll = UnityEngine.Random.Range(TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order), Math.Max(deliriumScale, controller.Difficulty.Order));

                        TFTVLogger.Always($"{pXOperative.DisplayName} has {deliriumScale} deliriumScale, and the roll is {roll}");

                        TacticalDeployZone zone = targetablePhoenixOperatives[pXOperative];

                        Level level = controller.Level;
                        TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                        Vector3 position = zone.Pos;
                        //  TFTVLogger.Always($"position before adjustmment is {position}");
                        if (position.y <= 2 && position.y != 1.0)
                        {
                            //  TFTVLogger.Always($"position should be adjusted to 1.2");
                            position.y = 1.0f;
                        }
                        else if (position.y > 4 && position.y != 4.8)
                        {
                            //    TFTVLogger.Always($"position should be adjusted to 4.8");
                            position.SetY(4.8f);
                        }

                        MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");
                        //spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Empty, zone.Pos + Vector3.up * -1.5f, 3, 1, false, true });
                        // TFTVLogger.Always($"pXOperative to be ghosted {pXOperative.DisplayName} at pos {position}");
                        spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, position, 3, 1, false, true });

                        // SpawnBlob_Internal(TacticalVoxelType type, Vector3 pos, int horizontalRadius, int height, bool circular, bool updateMatrix = true)

                        if (roll > 6)
                        {
                            TacCharacterDef chosenEnemy = enemies.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                            zone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found deployzone and deploying " + chosenEnemy.name + $"; Position is y={zone.Pos.y} x={zone.Pos.x} z={zone.Pos.z}");
                            ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                            actorDeployData.InitializeInstanceData();
                            TacticalActorBase tacticalActorBase = zone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zone);

                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            tacticalActor?.TacticalActorView.DoCameraChase();
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
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.GetTopsideDeployZones(controller);

                    List<TacCharacterDef> myrmidons =
                        new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(myrmidonTag)));

                    foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                    {
                        int rollCap = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order) - 1;

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int myrmidonsToDeploy = UnityEngine.Random.Range(1, rollCap);

                        for (int x = 0; x < myrmidonsToDeploy; x++)
                        {
                            TacCharacterDef chosenMyrmidon = myrmidons.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found topside deployzone position and deploying " + chosenMyrmidon.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = chosenMyrmidon.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);


                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (x == 0 && tacticalActor != null)
                            {
                                tacticalActor.TacticalActorView.DoCameraChase();
                            }
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
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.GetTopsideDeployZones(controller);
                    tacticalDeployZones.AddRange(Map.DeploymentZones.GetCenterSpaceDeployZones(controller));

                    List<TacCharacterDef> worms =
                        new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(wormTag)));

                    foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                    {
                        int rollCap = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order) - 1;

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int wormsToDeploy = UnityEngine.Random.Range(1, rollCap);
                        TacCharacterDef chosenWormType = worms.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                        for (int x = 0; x < wormsToDeploy; x++)
                        {
                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = chosenWormType.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (x == 0 && tacticalActor != null)
                            {
                                tacticalActor.TacticalActorView.DoCameraChase();
                            }
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
                    TFTVLogger.Always("Spawning Secondary Force");
                    int difficulty = controller.Difficulty.Order;
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.GetAllBottomDeployZones(controller);
                    TFTVLogger.Always($"there are {tacticalDeployZones.Count()} bottom deploy zones");

                    List<TacticalActor> pxOperatives = controller.GetFactionByCommandName("px").TacticalActors.ToList();

                    List<TacticalDeployZone> culledTacticalDeployZones = new List<TacticalDeployZone>();
                    List<TacticalDeployZone> preferableDeploymentZone = new List<TacticalDeployZone>();
                    TacticalDeployZone zoneToDeployAt = new TacticalDeployZone();

                    foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                    {
                        if (!CheckLOSToPlayer(controller, tacticalDeployZone.Pos))
                        {
                            TFTVLogger.Always($"Found culled tactical deploy zone at {tacticalDeployZone.Pos} ");

                            culledTacticalDeployZones.Add(tacticalDeployZone);
                        }
                    }

                    if (culledTacticalDeployZones.Count > 0)
                    {
                        foreach (TacticalDeployZone tunnelZone in Map.DeploymentZones.GetTunnelDeployZones(controller))
                        {
                            if (culledTacticalDeployZones.Contains(tunnelZone))
                            {
                                TFTVLogger.Always($"Found preferable tactical deploy zone at {tunnelZone.Pos} ");
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
                            TFTVLogger.Always($"getting random zoneToDeployAt from culled options");
                            TFTVLogger.Always($"position is {zoneToDeployAt?.Pos}");
                        }
                    }
                    else
                    {
                        TFTVLogger.Always($"getting random zoneToDeployAt");

                        zoneToDeployAt = tacticalDeployZones.GetRandomElement();
                        TFTVLogger.Always($"the zoneToDeployAt is {zoneToDeployAt?.Pos}");
                    }

                    Dictionary<TacCharacterDef, int> secondaryForce = GenerateSecondaryForce(controller);

                    Map.Consoles.Explosions.GenerateFakeExplosion(zoneToDeployAt.Pos);

                    TFTVLogger.Always($"Explosion preceding secondary force deployment at {zoneToDeployAt.Pos}");

                    zoneToDeployAt.SetFaction(controller.GetFactionByCommandName("ALN"), TacMissionParticipant.Intruder);
                    TFTVLogger.Always($"Changed deployzone to Alien and Intruder");

                    foreach (TacCharacterDef tacCharacterDef in secondaryForce.Keys)
                    {
                        for (int i = 0; i < secondaryForce[tacCharacterDef]; i++)
                        {

                            TFTVLogger.Always($"going to generate actorDeployedData from {tacCharacterDef.name}");
                            ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();
                            TFTVLogger.Always($"generated deployData");
                            actorDeployData.InitializeInstanceData();
                            TFTVLogger.Always($"data initialized");
                            TacticalActorBase tacticalActorBase = zoneToDeployAt.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zoneToDeployAt);
                            TFTVLogger.Always($"actor spawned");

                            if (tacticalActorBase != null)
                            {

                                TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                                if (difficulty < 6)
                                {
                                    if (tacticalActor != null)
                                    {
                                        if (tacticalActor.HasGameTag(crabTag) || tacticalActor.HasGameTag(fishmanTag))
                                        {
                                            if (TFTVArtOfCrab.Has1APWeapon(tacCharacterDef))
                                            {

                                                tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);

                                            }
                                            else
                                            {

                                                tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                                TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");

                                            }
                                        }
                                        else if (tacticalActor.HasGameTag(sirenTag))
                                        {
                                            tacticalActor.Status.ApplyStatus(reinforcementStatusUnder1AP);
                                            TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder1AP.EffectName}");
                                        }
                                        else
                                        {

                                            tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                            TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");

                                        }
                                    }
                                }

                                if (i == 0)
                                {
                                    tacticalActor.TacticalActorView.DoCameraChase();
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

            internal static void FishmanInfiltrationStrat(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Spawning Triton Infiltration team");
                    int difficulty = controller.Difficulty.Order;
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.GetAllBottomDeployZones(controller);
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
                        foreach (TacticalDeployZone tunnelZone in Map.DeploymentZones.GetTunnelDeployZones(controller))
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

                    Dictionary<TacCharacterDef, int> tritonInfiltratonTeam = GenerateTritonInfiltrationForce(controller);

                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                    Vector3 position = zoneToDeployAt.Pos;


                    //  TFTVLogger.Always($"position before adjustmment is {position}");
                    if (position.y <= 2 && position.y != 1.0)
                    {
                        //  TFTVLogger.Always($"position should be adjusted to 1.2");
                        position.y = 1.0f;
                    }
                    else if (position.y > 4 && position.y != 4.8)
                    {
                        //    TFTVLogger.Always($"position should be adjusted to 4.8");
                        position.SetY(4.8f);

                    }

                    TFTVLogger.Always($"Mist will spawn at position {position}");

                    MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");

                    spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, position, 3, 1, false, true });


                    foreach (TacCharacterDef tacCharacterDef in tritonInfiltratonTeam.Keys)
                    {
                        for (int i = 0; i < tritonInfiltratonTeam[tacCharacterDef]; i++)
                        {

                            zoneToDeployAt.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = zoneToDeployAt.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zoneToDeployAt);

                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (difficulty < 6)
                            {
                                if (tacticalActor != null)
                                {

                                    if (TFTVArtOfCrab.Has1APWeapon(tacCharacterDef))
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);

                                    }
                                    else
                                    {

                                        tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");
                                    }
                                }
                            }

                            if (i == 0)
                            {
                                tacticalActor.TacticalActorView.DoCameraChase();
                            }
                            //  TFTVLogger.Always($"{tacticalActor.DisplayName} spawned, has {tacticalActor.CharacterStats.ActionPoints} actions points");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static Dictionary<TacCharacterDef, int> GenerateTritonInfiltrationForce(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Generating Triton Infiltration Team Force");

                    ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

                    List<TacCharacterDef> researchedTritons = new List<TacCharacterDef>();

                    Dictionary<TacCharacterDef, int> infiltrationTeam = new Dictionary<TacCharacterDef, int>();

                    int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order);

                    foreach (TacCharacterDef tacCharacterDef in controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs)
                    {
                        if (tacCharacterDef.ClassTag != null && tacCharacterDef.ClassTag == fishmanTag && !researchedTritons.Contains(tacCharacterDef))
                        {
                            researchedTritons.Add(tacCharacterDef);

                        }
                    }

                    for (int x = 0; x < difficulty + 2; x++)
                    {
                        TacCharacterDef tritonTypeToAdd = researchedTritons.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                        if (infiltrationTeam.ContainsKey(tritonTypeToAdd))
                        {
                            infiltrationTeam[tritonTypeToAdd] += 1;
                        }
                        else
                        {
                            infiltrationTeam.Add(tritonTypeToAdd, 1);
                        }
                    }

                    return infiltrationTeam;
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

                    Dictionary<ClassTagDef, int> reinforcements = TFTVTouchedByTheVoid.TBTVCallReinforcements.PickReinforcements(controller);

                    // TFTVLogger.Always("2");
                    Dictionary<TacCharacterDef, int> secondaryForce = new Dictionary<TacCharacterDef, int>();

                    TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");

                    int difficulty = TFTVReleaseOnly.DifficultyOrderConverter(controller.Difficulty.Order);

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
                    //   TFTVLogger.Always("3");
                    secondaryForce.Add(mindFragger, difficulty);
                    //    TFTVLogger.Always("4");
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
        }

    }
}

