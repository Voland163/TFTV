using Base;
using HarmonyLib;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TFTV
{
    internal class TFTVTacticalUtils
    {
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
                    
                /*    foreach(FixedDeployConditionData fixedDeployConditionData in tacticalDeployZone.FixedDeployment) 
                    {

                        TFTVLogger.Always($"{tacticalDeployZone.name} will spawn {fixedDeployConditionData.TacActorDef.name}");
                    
                    }*/
                    

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

    }
}
