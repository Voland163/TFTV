using Base;
using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    internal class TFTVTacticalUtils
    {

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
                  //  TFTVLogger.Always($"{tacticalDeployZone.name} at position {tacticalDeployZone.Pos}, belongs to {tacticalDeployZone.MissionParticipant.GetName()}");
                    
                    foreach(FixedDeployConditionData fixedDeployConditionData in tacticalDeployZone.FixedDeployment) 
                    {
                        TFTVLogger.Always($"{tacticalDeployZone.name} will spawn {fixedDeployConditionData.TacActorDef.name}");
                    
                    }


                    foreach (MissionDeployConditionData fixedDeployConditionData in tacticalDeployZone.MissionDeployment)
                    {
                        TFTVLogger.Always($"{tacticalDeployZone.name} at {tacticalDeployZone.Pos} activates on turn {fixedDeployConditionData.ActivateOnTurn}, tag: {fixedDeployConditionData.ActorTagDef}, " +
                            $"deactivate after turn: {fixedDeployConditionData.DeactivateAfterTurn}");

                    }

                    TFTVLogger.Always($"{tacticalDeployZone.DeployConditions}");
                    

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
