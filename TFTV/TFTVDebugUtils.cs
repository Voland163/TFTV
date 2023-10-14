using Base;
using Base.Core;
using Base.Defs;
using Base.Serialization;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{



    internal class TFTVDebugUtils
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


      /*  [HarmonyPatch(typeof(GeoFaction), "ScheduleAttackOnSite")]
        public static class PhoenixGame_ScheduleAttackOnSite_patch
        {

            public static void Postfix(GeoSite site, TimeUnit attackAfter)
            {
                try
                {
                    TFTVLogger.Always($"scheduleAttackOnSite run {site.LocalizedSiteName} {attackAfter}");



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }*/


        private static void CheckSaves()
        {
            try
            {



                PhoenixSaveManager phoenixSaveManager = GameUtl.GameComponent<PhoenixGame>().SaveManager;

                List<SavegameMetaData> saves = phoenixSaveManager.GetSaves().ToList();

                foreach (SavegameMetaData save in saves)
                {
                    PPSavegameMetaData ppSave = save as PPSavegameMetaData;

                    TFTVLogger.Always($"{ppSave.Name} has difficulty {ppSave.DifficultyDef.name}");

                }



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        internal static void Print()
        {
            try
            {


                foreach (TacticalItemDef item in Repo.GetAllDefs<TacticalItemDef>()
                    .Where(ti => ti.name.Contains("Torso")).Where(ti => !ti.name.Contains("BIO"))

                .Where(ti => ti.name.StartsWith("AN_") || ti.name.StartsWith("SY_") || ti.name.StartsWith("NJ_") || ti.name.StartsWith("NEU") || ti.name.StartsWith("PX_") || ti.name.StartsWith("IN_")))
                {



                    TFTVLogger.Always($"{item} might need a tag");

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
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
