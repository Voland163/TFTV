using Base;
using Base.Cameras;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVTacticalUtils
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly string _sheKey = "KEY_GRAMMAR_PRONOUNS_SHE";
        private static readonly string _heKey = "KEY_GRAMMAR_PRONOUNS_HE";
        private static readonly string _herKey = "KEY_GRAMMAR_PRONOUNS_HER";
        private static readonly string _himKey = "KEY_GRAMMAR_PRONOUNS_HIM";

        public static string ShortenName(string fullName, int maxLength)
        {
            try
            {
                if (fullName.Length <= maxLength)
                    return fullName;

                string[] words = fullName.Split(' ');
                if (words.Length < 2)
                    return fullName; // If there's no last name, return as is

                string firstInitial = words[0][0] + ".";
                string lastWord = words[words.Length - 1];

                string shortenedName = firstInitial + " " + lastWord;

                return shortenedName;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        public static List<TacticalActor> GetRevealedMindControlledByPhoenixEnemy()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                return controller.Map.GetTacActors<TacticalActor>(controller.GetFactionByCommandName("px"), FactionRelation.Enemy).Where(ta => ta.IsAlive && ta.IsRevealedToViewer && ta.Status.Statuses.Any(s => s is MindControlStatus)).ToList();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static List<TacticalActor> GetRevealedNeutralTacticalActors()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                return controller.Map.GetTacActors<TacticalActor>(controller.GetFactionByCommandName("px"), FactionRelation.Neutral).Where(ta => ta.IsAlive && ta.IsRevealedToViewer).ToList();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static TacticalActor GetEnemyActorWithClassTag(ClassTagDef gameTag)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                List<TacticalActor> enemyActors = controller.Map.GetTacActors<TacticalActor>(controller.GetFactionByCommandName("px"), FactionRelation.Enemy).ToList();

                if (enemyActors.Count > 0 && enemyActors.Any(ta => CheckActorCanQuip(ta) && ta.HasGameTag(gameTag)))
                {
                    return enemyActors.FirstOrDefault(ta => CheckActorCanQuip(ta) && ta.HasGameTag(gameTag));
                }

                return null;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        private static bool CheckActorCanQuip(TacticalActor tacticalActor)
        {
            try
            {
                if (!tacticalActor.IsAlive || tacticalActor.IsEvacuated || tacticalActor.IsDisabled)
                {
                    return false;
                }

                if (tacticalActor.GameTags.Contains(Shared.SharedGameTags.VehicleTag)
                    || tacticalActor.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                    || tacticalActor.GameTags.Contains(Shared.SharedGameTags.MutoidTag))
                {
                    return false;
                }

                if (tacticalActor.Status.HasStatus<MindControlStatus>() || tacticalActor.Status.HasStatus<ParalysedStatus>() || tacticalActor.Status.HasStatus<PanicStatus>())
                {
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

        public static List<TacticalActor> GetEligibleForQuipsPhoenixActors()
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                return controller.Map.GetTacActors<TacticalActor>(controller.GetFactionByCommandName("px")).
                    Where(ta => CheckActorCanQuip(ta)).ToList();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        public static string AdjustTextForGender(string text, bool male = true)
        {
            try
            {
                if (male)
                {
                    if (text.Contains("[he/she]"))
                    {
                        text = text.Replace("[he/she]", TFTVCommonMethods.ConvertKeyToString(_heKey));
                    }

                    if (text.Contains("[him/her]"))
                    {
                        text = text.Replace("[him/her]", TFTVCommonMethods.ConvertKeyToString(_himKey));
                    }
                }
                else
                {

                    if (text.Contains("[he/she]"))
                    {
                        text = text.Replace("[he/she]", TFTVCommonMethods.ConvertKeyToString(_sheKey));
                    }

                    if (text.Contains("[him/her]"))
                    {
                        text = text.Replace("[him/her]", TFTVCommonMethods.ConvertKeyToString(_herKey));
                    }
                }

                return text;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();


        }


        public static string GetCharacterLastName(string characterName)
        {
            try
            {
                if (characterName.Split().Count() > 1)
                {

                    return characterName.Split()[1];
                }
                else
                {

                    return characterName;
                }


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static List<TacticalActor> GetTacticalActorsPhoenix(TacticalLevelController level)
        {
            try
            {
                TacticalFaction phoenix = level.GetFactionByCommandName("PX");

                GameTagDef mutoidTag = DefCache.GetDef<GameTagDef>("Mutoid_TagDef");

                List<TacticalActor> operatives = new List<TacticalActor>();

                foreach (TacticalActorBase tacticalActorBase in phoenix.Actors)
                {
                    TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                    if (tacticalActorBase.BaseDef.name == "Soldier_ActorDef" && tacticalActorBase.InPlay && !tacticalActorBase.HasGameTag(mutoidTag)
                        && tacticalActorBase.IsAlive && tacticalActor.GeoUnitId!=null && tacticalActor.GeoUnitId != 0 && level.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(tacticalActor.GeoUnitId))
                    {
                        operatives.Add(tacticalActor);
                    }
                }

                if (operatives.Count == 0)
                {
                    return null;
                }

                TFTVLogger.Always("There are " + operatives.Count() + " phoenix operatives");
                List<TacticalActor> orderedOperatives = operatives.OrderByDescending(e => GetNumberOfMissions(e)).ToList();
                for (int i = 0; i < operatives.Count; i++)
                {
                    TFTVLogger.Always("TacticalActor is " + orderedOperatives[i].DisplayName + " and # of missions " + GetNumberOfMissions(orderedOperatives[i]));
                }
                return orderedOperatives;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static int GetNumberOfMissions(TacticalActor tacticalActor)
        {
            try
            {
                TacticalLevelController level = tacticalActor.TacticalFaction.TacticalLevel;

                int numberOfMission = level.TacticalGameParams.Statistics.LivingSoldiers[tacticalActor.GeoUnitId].MissionsParticipated;

                return numberOfMission;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }






        internal static TacticalDeployZone FindTDZ(string name)
        {
            try
            {
                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                TacticalDeployZone tacticalDeployZone = controller.Map.GetActors<TacticalDeployZone>(null).FirstOrDefault(tdz => tdz.name == name);

                return tacticalDeployZone;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void RevealExfilPoint(TacticalLevelController controller, int turnNumber)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!config.ShowAmbushExfil)
                {
                    return;
                }

                if (!controller.TacMission.MissionData.MissionType.MissionTags.Contains(DefCache.GetDef<MissionTypeTagDef>("MissionTypeAmbush_MissionTagDef")))
                {
                    return;
                }

                if (turnNumber != 1 || !controller.CurrentFaction.IsControlledByPlayer)
                {
                    return;
                }

                TacticalExitZone tacticalExitZone = controller.Map.GetActors<TacticalExitZone>().FirstOrDefault(a => a.TacticalFaction == controller.GetFactionByCommandName("px"));
                TFTVLogger.Always($"found tez? {tacticalExitZone != null}");

                MethodInfo createVisuals = AccessTools.Method(typeof(TacticalExitZone), "CreateVisuals");

                if (tacticalExitZone != null)
                {
                    createVisuals.Invoke(tacticalExitZone, null);

                    tacticalExitZone.CameraDirector.Hint(CameraHint.ChaseTarget, new CameraChaseParams
                    {
                        ChaseVector = tacticalExitZone.Pos,
                        ChaseTransform = null,
                        ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LockCameraMovement = false,
                        Instant = true,
                        ChaseOnlyOutsideFrame = false,
                        SnapToFloorHeight = true

                    });

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        internal static void RevealAllSpawns(TacticalLevelController controller)
        {
            try
            {
                List<TacticalDeployZone> zones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>());
                List<TacticalExitZone> exitZone = new List<TacticalExitZone>(controller.Map.GetActors<TacticalExitZone>());
                //  TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");

                /*  TacticalDeployZone tacticalDeployZone1 = new TacticalDeployZone() { };
                  tacticalDeployZone1 = zones.First();
                  tacticalDeployZone1.SetPosition(zones.First().Pos + new Vector3(3, 0, 3));*/




                MethodInfo createVisuals = AccessTools.Method(typeof(TacticalDeployZone), "CreateVisuals");

                foreach (TacticalDeployZone tacticalDeployZone in zones)
                {
                    createVisuals.Invoke(tacticalDeployZone, null);
                    //  TFTVLogger.Always($"{tacticalDeployZone.name} at position {tacticalDeployZone.Pos}, belongs to {tacticalDeployZone.MissionParticipant.GetName()}");

                    //   TFTVLogger.Always($"{tacticalDeployZone.name} has perception base? {tacticalDeployZone.TacticalPerceptionBase!=null}");

                    foreach (FixedDeployConditionData fixedDeployConditionData in tacticalDeployZone.FixedDeployment)
                    {
                        TFTVLogger.Always($"FixedDeployConditionData: {tacticalDeployZone.name} at {tacticalDeployZone.Pos} will spawn {fixedDeployConditionData.TacActorDef.name}");
                    }

                    foreach (MissionDeployConditionData fixedDeployConditionData in tacticalDeployZone.MissionDeployment)
                    {
                        TFTVLogger.Always($"{tacticalDeployZone.name} at {tacticalDeployZone.Pos} activates on turn {fixedDeployConditionData.ActivateOnTurn}, tag: {fixedDeployConditionData?.ActorTagDef?.name}, " +
                            $"deactivate after turn: {fixedDeployConditionData.DeactivateAfterTurn}", false);

                    }

                    //    TFTVLogger.Always($"{tacticalDeployZone.DeployConditions}");
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





        /*
        [HarmonyPatch(typeof(WipeEnemyFactionObjective), "EvaluateObjective")]
        public static class WipeEnemyFactionObjective_EvaluateObjective_BaseDefense_Patch
        {
            public static void Prefix(WipeEnemyFactionObjective __instance, FactionObjectiveState __result)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (__result == FactionObjectiveState.Failed && __instance.Faction!= controller.GetFactionByCommandName("px"))
                    { 
                        FactionObjective baseDefenseSurvive5turns = controller.GetFactionByCommandName("px").Objectives.FirstOrDefault(o => o.Description.LocalizationKey == "BASEDEFENSE_SURVIVE5_OBJECTIVE");

                        if (baseDefenseSurvive5turns != null && baseDefenseSurvive5turns.State != FactionObjectiveState.Achieved)
                        {
                            baseDefenseSurvive5turns.Evaluate();
                            
                            TFTVLogger.Always($"Wipe enemy objective {__instance.Faction} state is {__result}, baseDefenseSurvive objective is {baseDefenseSurvive5turns.State}");
                           


                        }


                    
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/

        /* [HarmonyPatch(typeof(KillActorFactionObjective), "EvaluateObjective")]
          public static class KillActorFactionObjective_EvaluateObjective_BaseDefense_Patch
          {
              public static void Postfix(KillActorFactionObjective __instance, FactionObjectiveState __result)
              {
                  try
                  {
                      TFTVLogger.Always($"Kill actor objective {__instance.GetDescription()} state is {__result}");
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/



    }
}
