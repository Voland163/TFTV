using Base;
using Base.Defs;
using Base.Entities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Missions;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFTV
{
    internal class TFTVExperimental
    {

        internal static Color purple = new Color32(149, 23, 151, 255);
        private static readonly DefRepository Repo = TFTVMain.Repo;


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

}


