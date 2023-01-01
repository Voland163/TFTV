using Base;
using Base.Defs;
using Base.Entities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using hoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVExperimental
    {

        internal static Color purple = new Color32(149, 23, 151, 255);
        //  private static readonly DefRepository Repo = TFTVMain.Repo;

        //Unlock Project Glory when player activates 3rd base. Commented out for release #12
       /* [HarmonyPatch(typeof(GeoPhoenixFaction), "ActivatePhoenixBase")]
          public static class GeoPhoenixFaction_ActivatePhoenixBase_GiveGlory_Patch
          {
              public static void Postfix(GeoPhoenixFaction __instance)
              {
                  try
                  {
                      if (__instance.GeoLevel.EventSystem.GetVariable("Photographer")!=1 && __instance.Bases.Count()>2) 
                      {
                        GeoscapeEventContext eventContext = new GeoscapeEventContext(__instance.GeoLevel.ViewerFaction, __instance);
                        __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaLotaStart", eventContext);
                          __instance.GeoLevel.EventSystem.SetVariable("Photographer", 1);
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }
       */

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
                            listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_"+vo));
                            missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                        }
                    }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }

        /*public static void CheckObjectives()
        {
            try 
            {
                List<ObjectiveElement> objectiveElementsList = UnityEngine.Object.FindObjectsOfType<ObjectiveElement>().ToList();
                if (objectiveElementsList.Count > 0)
                {
                    TFTVLogger.Always("There are elements in the list");

                }
                foreach (ObjectiveElement objectiveElement in objectiveElementsList)
                {
                    TFTVLogger.Always("ObjectiveElement name " + objectiveElement.name);
                    TFTVLogger.Always("ObjectiveElement " + objectiveElement.StatusText.text);

                    if (objectiveElement.Description.text.Contains("VOID"))
                    {
                        TFTVLogger.Always("FactionObjective check passed");
                        objectiveElement.Description.color = purple;

                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }*/


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
                        objective.Description = new LocalizedTextBind (objective.Description.Localize().ToUpper(), true);
                    }
                  
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        

        [HarmonyPatch(typeof(KeepSoldiersAliveFactionObjective), "EvaluateObjective")]
        public static class KeepSoldiersAliveFactionObjective_EvaluateObjective_Patch
        {

            public static void Postfix(KeepSoldiersAliveFactionObjective __instance,  ref FactionObjectiveState __result)
            {
                try
                {
                    //TFTVLogger.Always("FactionObjective Evaluate");
                    if (__instance.Description.LocalizationKey.Contains("VOID"))
                    {
                       __result=FactionObjectiveState.InProgress;

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

