using Base;
using Base.Defs;
using Base.Entities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Levels.Mist;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using TFTV.PortedAATweaks;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace TFTV
{
    internal class TFTVExperimental
    {

        internal static Color purple = new Color32(149, 23, 151, 255);
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static List<TacticalVoxel> VoxelsOnFire = new List<TacticalVoxel>();


        public static void CheckUseFireWeaponsAndDifficulty(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 0 && controller.CurrentDifficultyLevel.Order > 1)
                {
                    TFTVLogger.Always("Checking fire weapons usage to decide wether to add Fire Quenchers");

                    PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));

                    List<SoldierStats> allSoldierStats = new List<SoldierStats>(statisticsManager.CurrentGameStats.LivingSoldiers.Values.ToList());
                    allSoldierStats.AddRange(statisticsManager.CurrentGameStats.DeadSoldiers.Values.ToList());
                    List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                    int scoreFireDamage = 0;
                    StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");

                    foreach (SoldierStats stat in allSoldierStats)
                    {
                        if (stat.ItemsUsed.Count > 0)
                        {
                            usedWeapons.AddRange(stat.ItemsUsed);
                        }
                    }

                    if (usedWeapons.Count() > 0)
                    {
                        // TFTVLogger.Always("Checking use of each weapon... ");
                        foreach (UsedWeaponStat stat in usedWeapons)
                        {
                            //   TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());
                            if (Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Contains(stat.UsedItem.ToString())))
                            {
                                WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                                if (weaponDef != null && weaponDef.DamagePayload.DamageType == fireDamage)
                                {
                                    scoreFireDamage += stat.UsedCount;
                                }
                            }
                        }
                    }

                    TFTVLogger.Always("Fire weapons used " + scoreFireDamage + " times");

                    if (scoreFireDamage > 1)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(0, 6) + controller.CurrentDifficultyLevel.Order + scoreFireDamage;

                        TFTVLogger.Always("The roll is " + roll);

                        if (roll >= 10)
                        {
                            TFTVLogger.Always("The roll is passed!");
                            controller.EventSystem.SetVariable("FireQuenchersAdded", 1);
                        }

                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckForFireQuenchers(GeoLevelController controller)
        {
            try
            {
                CheckUseFireWeaponsAndDifficulty(controller);

                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 1)
                {
                    AddFireQuencherAbility();
                    TFTVLogger.Always("Fire Quenchers added!");
                }
                else
                {

                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };
                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };
                    
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void AddFireQuencherAbility()
        {
            try
            {
                ApplyStatusAbilityDef fireQuencherAbility = DefCache.GetDef<ApplyStatusAbilityDef>("FireQuencherAbility");
                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunityInvisibleAbility");
                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>() { fireQuencherAbility, fireImmunity };
                //  DefCache.GetDef<TacticalItemDef>("Crabman_Legs_Armoured_ItemDef").Abilities = abilities.ToArray();
                //  DefCache.GetDef<TacticalItemDef>("Crabman_Legs_EliteArmoured_ItemDef").Abilities = new TacticalAbilityDef[] { fireImmunity };
                //    DefCache.GetDef<TacCharacterDef>("Crabman9_Shielder_AlienMutationVariationDef").Data.Abilites = abilities.ToArray();

                DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = abilities.ToArray();
                DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = abilities.ToArray();
                // DefCache.GetDef<TacticalItemDef>("Crabman_LeftLeg_Armoured_BodyPartDef").Abilities = abilities.ToArray();
                //  DefCache.GetDef<TacticalItemDef>("Crabman_RightLeg_Armoured_BodyPartDef").Abilities = abilities.ToArray();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        [HarmonyPatch(typeof(TacticalPerceptionBase), "IsTouchingVoxel")]

        public static class TFTV_Experimental_Evaluate_Experiment_patch
        {
            public static void Postfix(TacticalPerceptionBase __instance)
            {
                try
                {
                    DamageMultiplierStatusDef fireQuencherStatus = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");
                    //   DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                    TacticalActorBase tacticalActorBase = __instance.TacActorBase;

                    //
                    if (tacticalActorBase is TacticalActor && tacticalActorBase.GetActor().Status.HasStatus(fireQuencherStatus)) //tacticalActorBase.GetActor().GetAbility<DamageMultiplierAbility>(fireImmunity)!=null 
                                                                                                                                 // && tacticalActorBase.GetActor().HasGameTag(DefCache.GetDef<GameTagDef>("Crabman_ClassTagDef")))
                    {
                        foreach (TacticalVoxel voxel in tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxels(__instance.GetBounds()))
                        {
                            if (voxel.GetVoxelType() == TacticalVoxelType.Fire)
                            {
                                VoxelsOnFire.Add(voxel);
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, 0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, -0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, 0.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, -0.5f)));
                                // VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1.5f, 0, 1.5f)));
                                //  VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1.5f, 0, -1.5f)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, 1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, -1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, -1)));
                                VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, 1)));
                                //  VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1.5f, 0, 0.5f)));
                                // VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1.5f, 0, -0.5f)));
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



        [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

        public static class TacticalActor_OnFinishedMovingActor_Experiment_patch
        {
            public static void Postfix()
            {
                try
                {
                    if (TFTVExperimental.VoxelsOnFire.Count > 0)
                    {
                      //  TFTVLogger.Always("Voxels on fire count is " + TFTVExperimental.VoxelsOnFire.Count);
                        // List<TacticalVoxel> voxelsForMist = new List<TacticalVoxel>();
                        foreach (TacticalVoxel voxel in TFTVExperimental.VoxelsOnFire)
                        {
                            if (voxel.GetVoxelType() == TacticalVoxelType.Fire)
                            {
                            //    TFTVLogger.Always("Got past the if check");

                                voxel.SetVoxelType(TacticalVoxelType.Empty, 1);
                            }
                        }
                    }

                    if (TFTVExperimental.VoxelsOnFire.Count > 0)
                    {
                        foreach (TacticalVoxel voxel in TFTVExperimental.VoxelsOnFire)
                        {
                            if (voxel.GetVoxelType() == TacticalVoxelType.Empty)
                            {
                                //TFTVLogger.Always("Got past the if check for Mist");
                                voxel.SetVoxelType(TacticalVoxelType.Mist, 2, 10);
                            }
                        }
                    }

                    TFTVExperimental.VoxelsOnFire.Clear();


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }




        /*   public static float Score = 0;
           public static List<float> ScoresBeforeCulling = new List<float>();
           public static int CounterAIActionsInfluencedBySafetyConsideration = 0;

           [HarmonyPatch(typeof(AIMultiplyConsiderationCombiner), "GetCombinedScore")]

           public static class AIConsiderationCombiner_GetCombinedScore_Experiment_patch
           {
               public static void Postfix(float __result, List<float> ____scores)
               {
                   try
                   {

                       if (Score != 0)
                       {
                           List<int> scoreList = new List<int>();
                           bool safetyConsiderationRelevant = true;
                           float scoreBeforeSafetyConsideration = 1f;

                           for (int x = 0; x < ____scores.Count()-1; x++)
                           {
                               if (____scores[x] == 0) 
                               {
                                   safetyConsiderationRelevant = false;

                               }
                               if (safetyConsiderationRelevant) 
                               {
                                   scoreBeforeSafetyConsideration *= ____scores[x];                          
                               }
                           }

                           if (safetyConsiderationRelevant && scoreBeforeSafetyConsideration > ____scores.Last()) 
                           {
                               ScoresBeforeCulling.Add(scoreBeforeSafetyConsideration);
                              TFTVLogger.Always("DefenseSafePosition consideration score was " + Score + " and combined score is " + __result + " so score was reduced from " + __result / Score + " checksum " + scoreBeforeSafetyConsideration);
                               foreach(float score in ____scores) 
                               {
                                   TFTVLogger.Always(score.ToString());

                               }

                           }

                           Score = 0;
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }
           }

           [HarmonyPatch(typeof(AISafePositionConsideration), "Evaluate")]

           public static class AISafePositionConsideration_Evaluate_Experiment_patch
           {
               public static void Postfix(AISafePositionConsideration __instance, float __result)
               {
                   try
                   {
                       if (__instance.BaseDef.name == "DefenseSafePosition_AIConsiderationDef" && __result != 1)
                       {

                           // TFTVLogger.Always("DefenseSafePosition_AIConsiderationDef " + __result);
                           Score = __result;
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }
           }



           [HarmonyPatch(typeof(AIFaction), "SelectTarget")]

           public static class AIFaction_SelectTarget_Experiment_patch
           {
               public static void Postfix(IList<AIScoredTarget> scoredTargets, AIScoredTarget __result, AIScoredTargetComparer ____aiScoredTargetComparer)
               {
                   try
                   {
                       if (__result != null)
                       {

                           IList<AIScoredTarget> list = null;
                           float num = 0f;
                           Dictionary<AIActionDef, MinHeap<AIScoredTarget>> dictionary = new Dictionary<AIActionDef, MinHeap<AIScoredTarget>>();
                           foreach (AIScoredTarget scoredTarget in scoredTargets)
                           {
                               AIActionDef actionDef = scoredTarget.Action.ActionDef;
                               if (!dictionary.ContainsKey(actionDef))
                               {
                                   dictionary[actionDef] = new MinHeap<AIScoredTarget>(____aiScoredTargetComparer);
                               }

                               dictionary[actionDef].Push(scoredTarget);
                               if (num < scoredTarget.Score)
                               {
                                   num = scoredTarget.Score;
                               }
                           }

                           list = new List<AIScoredTarget>();
                           foreach (KeyValuePair<AIActionDef, MinHeap<AIScoredTarget>> item in dictionary)
                           {
                               int i = 0;
                               MinHeap<AIScoredTarget> value = item.Value;
                               for (; i < 3; i++)
                               {
                                   if (value.Count <= 0)
                                   {
                                       break;
                                   }

                                   AIScoredTarget aIScoredTarget = value.Pop();
                                   if (aIScoredTarget.Score / num < 0.75)
                                   {
                                       break;
                                   }

                                   list.Add(aIScoredTarget);
                               }
                           }

                           foreach (AIScoredTarget aIScoredTarget in list)
                           {


                               TFTVLogger.Always
                                   ("Possible action is " + aIScoredTarget.Action.ActionDef.name + " with a score of " + aIScoredTarget.Score);

                           }
                           TFTVLogger.Always("Chosen action is " + __result.Action.ActionDef.name);
                           if (ScoresBeforeCulling.Count > 0) 
                           {
                               List<float> orderedCulledScores = ScoresBeforeCulling.OrderByDescending(s => s).ToList();

                               TFTVLogger.Always(ScoresBeforeCulling.Count() + " positions were culled by the SafetyConsideration, and the highest culled score was " + orderedCulledScores.First());

                               bool highCullScore = false;

                            //   foreach (AIScoredTarget aIScoredTarget in list)
                            //   {
                                   if (orderedCulledScores.First() >= list.Last().Score) 
                                   {
                                       highCullScore = true;

                                   }
                            //   }

                               if (highCullScore) 
                               {
                                   TFTVLogger.Always("Without the safety consideration, at least one of the culled positions could have been considered for chosen action");
                                   CounterAIActionsInfluencedBySafetyConsideration += 1;

                               }

                               ScoresBeforeCulling.Clear();
                           }
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }
           }*/


        //Method by Dimitar "Codemite" Evtimov from Snapshot Games
        public static void PatchInAllBaseDefenseDefs()
        {
            try
            {

                CustomMissionTypeDef alienDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                CustomMissionTypeDef anuDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAnu_CustomMissionTypeDef");
                CustomMissionTypeDef njDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseNJ_CustomMissionTypeDef");
                CustomMissionTypeDef syDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseSY_CustomMissionTypeDef");

                TacMissionTypeDef[] defenseMissions = { alienDef, anuDef, njDef, syDef };

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded)
                        continue;

                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetTransformsInChildrenStable())
                        {
                            var objActivator = transform.GetComponent<TacMissionObjectActivator>();
                            if (objActivator && objActivator.Missions.Length == 1 && objActivator.Missions.Contains(alienDef))
                            {
                                objActivator.Missions = defenseMissions;
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


        [HarmonyPatch(typeof(TacMission), "PrepareMissionActivators")]

        public static class TacMission_PrepareMissionActivators_Experiment_patch
        {
            public static void Prefix(TacMission __instance)
            {
                try
                {

                    TFTVLogger.Always("PrepareMissionActivators");
                    PatchInAllBaseDefenseDefs();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        public static TacticalActor mutoidReceivingHealing = null;

        [HarmonyPatch(typeof(HealAbility), "HealTargetCrt")]

        public static class HealAbility_HealTargetCrt_Mutoid_Patch
        {
            public static void Prefix(PlayingAction action)
            {
                try
                {
                    if ((TacticalActor)((TacticalAbilityTarget)action.Param).Actor != null)
                    {
                        TacticalActor actor = (TacticalActor)((TacticalAbilityTarget)action.Param).Actor;
                        if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef")))
                        {
                            mutoidReceivingHealing = actor;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "get_GeneralHealAmount")]

        public static class HealAbility_Mutoid_Patch
        {

            public static void Postfix(HealAbility __instance, ref float __result)
            {
                try
                {

                    if (mutoidReceivingHealing != null)
                    {
                        __result = 0;
                        mutoidReceivingHealing = null;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
        class DamageMultiplier_BugFix
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Div)
            };

                // insert after each of the first 3 divide opcodes
                int divs = 0;
                for (int index = 0; index < instructions.Count(); index++)
                {
                    if (listInstructions[index].opcode == OpCodes.Div)
                    {
                        listInstructions.InsertRange(index + 1, insert);
                        index += 2;
                        divs++;
                        if (divs == 3)
                        {
                            break;
                        }
                    }
                }

                if (divs != 3)
                {
                    return instructions; // didn't find three, function signature changed, abort
                }
                return listInstructions;
            }

        }

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
        public static class GeoMission_ModifyMissionData_VOObjectives_Patch
        {
            public static void Postfix(TacMissionData missionData, GeoMission __instance)
            {
                try
                {
                    TFTVLogger.Always("ModifyMissionData invoked");
                    GeoLevelController controller = __instance.Level;
                    List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                    List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                    // Remove faction objectives that correspond to void omens that are not in play
                    for (int i = listOfFactionObjectives.Count - 1; i >= 0; i--)
                    {
                        FactionObjectiveDef objective = listOfFactionObjectives[i];
                        if (objective.name.StartsWith("VOID_OMEN_TITLE_"))
                        {
                            int vo = int.Parse(objective.name.Substring("VOID_OMEN_TITLE_".Length));
                            if (!TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                            {
                                TFTVLogger.Always("Removing VO " + vo + " from faction objectives");
                                listOfFactionObjectives.RemoveAt(i);
                            }
                        }
                    }

                    // Add faction objectives for void omens that are in play
                    foreach (int vo in voidOmens)
                    {
                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                        {
                            if (!listOfFactionObjectives.Any(o => o.name == "VOID_OMEN_TITLE_" + vo))
                            {
                                TFTVLogger.Always("Adding VO " + vo + " to faction objectives");
                                listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                            }
                        }
                    }

                    missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




        /* [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
         public static class GeoMission_ModifyMissionData_AddVOObjectives_Patch
         {
             public static void Postfix(TacMissionData missionData, GeoMission __instance)
             {
                 try
                 {
                     TFTVLogger.Always("ModifyMissionData invoked");
                     GeoLevelController controller = __instance.Level;
                     List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                     foreach (int vo in voidOmens)
                     {
                         if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                         {
                             TFTVLogger.Always("VO " + vo + " found");
                             List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                             if (!listOfFactionObjectives.Contains(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo)))
                             {
                                 listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                                 missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                             }
                         }
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/

        //Patch to set VO objective test in uppercase to match other objectives
        [HarmonyPatch(typeof(ObjectivesManager), "Add")]
        public static class FactionObjective_ModifyObjectiveColor_Patch
        {

            public static void Postfix(ObjectivesManager __instance, FactionObjective objective)
            {
                try
                {
                    //  TFTVLogger.Always("FactionObjective Invoked");
                    if (objective.Description.LocalizationKey.Contains("VOID"))
                    {
                        objective.Description = new LocalizedTextBind(objective.Description.Localize().ToUpper(), true);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(WipeEnemyFactionObjective), "EvaluateObjective")]
        public static class TFTV_HavenDefendersHostileFactionObjective_EvaluateObjective_Patch
        {
            public static bool Prefix(FactionObjective __instance, ref FactionObjectiveState __result,
                List<TacticalFaction> ____enemyFactions, List<TacticalFactionDef> ____overrideFactions, bool ____ignoreDeployment)
            {
                try
                {
                    if (TFTVVoidOmens.VoidOmensCheck[5])
                    {
                        TacticalLevelController controller = __instance.Level;
                        string MissionType = controller.TacticalGameParams.MissionData.MissionType.SaveDefaultName;

                        if (MissionType == "HavenDefense")
                        {
                            if (!__instance.IsUiHidden)
                            {

                                //  TFTVLogger.Always("WipeEnemyFactionObjetive invoked");

                                if (!__instance.Faction.HasTacActorsThatCanWin() && !__instance.Faction.HasUndeployedTacActors())
                                {
                                    __result = FactionObjectiveState.Failed;
                                    TFTVLogger.Always("WipeEnemyFactionObjetive failed");
                                    return false; // skip original method
                                }

                                foreach (TacticalFaction enemyFaction in controller.Factions)
                                {
                                    if (enemyFaction.ParticipantKind == TacMissionParticipant.Intruder)
                                    {
                                        // TFTVLogger.Always("The faction is " + faction.TacticalFactionDef.name);
                                        if (!enemyFaction.HasTacActorsThatCanWin())
                                        {
                                            TFTVLogger.Always("HavenDefense with hostile defenders, no intruders alive, so mission should be a win");
                                            __result = FactionObjectiveState.Achieved;
                                            return false;
                                        }

                                    }
                                }


                            }
                            return true;
                        }
                        return true;
                    }
                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        //Patch to avoid triggering "failed" state for VO objectives when player loses a character
        [HarmonyPatch(typeof(KeepSoldiersAliveFactionObjective), "EvaluateObjective")]
        public static class KeepSoldiersAliveFactionObjective_EvaluateObjective_Patch
        {

            public static void Postfix(KeepSoldiersAliveFactionObjective __instance, ref FactionObjectiveState __result)
            {
                try
                {
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_3 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_3");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_5 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_5");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_7 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_7");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_10 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_10");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_15 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_15");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_16 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_16");
                    KeepSoldiersAliveFactionObjectiveDef VOID_OMEN_TITLE_19 = DefCache.GetDef<KeepSoldiersAliveFactionObjectiveDef>("VOID_OMEN_TITLE_19");

                    List<KeepSoldiersAliveFactionObjectiveDef> voidOmens = new List<KeepSoldiersAliveFactionObjectiveDef> { VOID_OMEN_TITLE_3, VOID_OMEN_TITLE_5, VOID_OMEN_TITLE_7, VOID_OMEN_TITLE_10, VOID_OMEN_TITLE_15, VOID_OMEN_TITLE_16, VOID_OMEN_TITLE_19 };

                    //  TFTVLogger.Always("FactionObjective Evaluate " + __instance.Description.ToString());
                    foreach (KeepSoldiersAliveFactionObjectiveDef keepSoldiersAliveFactionObjectiveDef in voidOmens)
                    {
                        // TFTVLogger.Always(keepSoldiersAliveFactionObjectiveDef.MissionObjectiveData.Description.LocalizeEnglish());
                        // TFTVLogger.Always(__instance.Description.LocalizationKey);

                        if (keepSoldiersAliveFactionObjectiveDef.MissionObjectiveData.Description.LocalizeEnglish().ToUpper() == __instance.Description.LocalizationKey)
                        {
                            // TFTVLogger.Always("FactionObjective check passed");
                            __result = FactionObjectiveState.InProgress;
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
    /* [HarmonyPatch(typeof(AIStrategicPositionConsideration), "Evaluate")]

        public static class AIStrategicPositionConsideration_Evaluate_Experiment_patch
        {
            public static void Postfix(AIStrategicPositionConsideration __instance, float __result)
            {
                try
                {
                    if (__instance.BaseDef.name == "StrategicPosition_AIConsiderationDef" && __result != 1)
                    {

                        TFTVLogger.Always("StrategicPosition_AIConsiderationDef " + __result);
                        Score = __result;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }*/

}


