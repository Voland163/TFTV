using Base.Defs;
using HarmonyLib;
using hoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewControllers.SoldierResultElement;
using static TFTV.TFTVUI.Tactical.Data;

namespace TFTV.TFTVUI.Tactical
{
    internal class Various
    {
        [HarmonyPatch(typeof(SoldierResultElement), "SetStatus", new Type[] { typeof(SoldierStatus), typeof(object[]) })] //VERIFIED
        public static class SoldierResultElement_SetStatus_patch
        {

            public static void Postfix(SoldierResultElement __instance)
            {
                try
                {
                    if (TFTVStamina.charactersWithDisabledBodyParts.ContainsKey(__instance.Actor.GeoUnitId))
                    {
                        string badlyInjuredText = TFTVCommonMethods.ConvertKeyToString("KEY_BADLY_INJURED_OPERATIVE");

                        __instance.Status.text = badlyInjuredText;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class TFTVTacticalObjectives
        {
            private static readonly TFTVConfig Config = new TFTVConfig();
            private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
            private static readonly DefRepository Repo = TFTVMain.Repo;
            private static readonly SharedData sharedData = TFTVMain.Shared;





            [HarmonyPatch(typeof(UIModuleBattleSummary), nameof(UIModuleBattleSummary.Initialize))]
            public static class UIModuleBattleSummary_Initialize_patch
            {
                public static void Prefix(UIModuleBattleSummary __instance)
                {
                    try
                    {
                        RectTransform rectTransformToCopy = null;

                        foreach (Component component in __instance.ObjectivesResultContainer)
                        {
                            if (component is RectTransform transform)
                            {
                                rectTransformToCopy = transform;
                                break;
                            }
                        }

                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                        UnityEngine.Object.Instantiate(rectTransformToCopy, __instance.ObjectivesResultContainer.transform);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            [HarmonyPatch(typeof(ObjectiveElement), nameof(ObjectiveElement.SetObjective), new Type[] { typeof(FactionObjective) })]
            public static class ObjectiveElement_SetObjective_patch
            {


                public static void Postfix(FactionObjective objective, ObjectiveElement __instance)
                {
                    try
                    {
                        if (objective.State == FactionObjectiveState.Achieved)
                        {
                            __instance.Description.color = Color.green;
                        }
                        else if (__instance.Description.color == Color.green && objective.State != FactionObjectiveState.Achieved)
                        {
                            __instance.Description.color = WhiteColor;
                        }

                        SecondaryObjectivesTactical.AdjustSecondaryObjective(objective, __instance);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }
            }




            //Adjusted for all haven defenses because parapsychosis bug. Check if necessary for other WipeEnemyFactionObjective missions
            [HarmonyPatch(typeof(WipeEnemyFactionObjective), "EvaluateObjective")] //VERIFIED
            public static class TFTV_HavenDefendersHostileFactionObjective_EvaluateObjective_Patch
            {
                public static bool Prefix(FactionObjective __instance, ref FactionObjectiveState __result,
                    List<TacticalFaction> ____enemyFactions, List<TacticalFactionDef> ____overrideFactions, bool ____ignoreDeployment)
                {
                    try
                    {
                        //  TFTVLogger.Always($"evaluating {__instance.GetDescription()} and the result is {__result}");

                        //   if (VoidOmensCheck[5])
                        //   {
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
                                    //  TFTVLogger.Always("WipeEnemyFactionObjetive failed");
                                    return false; // skip original method
                                }

                                foreach (TacticalFaction enemyFaction in controller.Factions)
                                {
                                    if (enemyFaction.ParticipantKind == TacMissionParticipant.Intruder)
                                    {
                                        // TFTVLogger.Always("The faction is " + faction.TacticalFactionDef.name);
                                        if (!enemyFaction.HasTacActorsThatCanWin())
                                        {
                                            //  TFTVLogger.Always("HavenDefense, no intruders alive, so mission should be a win");
                                            __result = FactionObjectiveState.Achieved;
                                            return false;
                                        }

                                    }
                                }


                            }
                            return true;
                        }
                        return true;
                        //  }
                        //  return true;
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
}
