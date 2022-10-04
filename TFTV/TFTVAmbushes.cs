using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVAmbushes
    {
      
        [HarmonyPatch(typeof(GeoscapeEventSystem), "OnLevelStart")]

        public static class GeoscapeEventSystem_PhoenixFaction_OnLevelStart_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.MoreAmbushes;
            }

            public static void Prefix(GeoscapeEventSystem __instance)
            {                
                try
                {                    
                    __instance.AmbushExploredSitesProtection = 0;
                    __instance.StartingAmbushProtection = 0;
                    if (TFTVVoidOmens.VoidOmen1Active)
                    {
                        __instance.ExplorationAmbushChance = 100;

                    }
                    else
                    {
                        __instance.ExplorationAmbushChance = 70;
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

