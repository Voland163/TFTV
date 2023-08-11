using Base.Defs;
using Epic.OnlineServices;
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
          /*  public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.MoreAmbushes;
            }*/

            public static void Prefix(GeoscapeEventSystem __instance)
            {                
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.GetComponent<GeoLevelController>();

                    if (TFTVNewGameOptions.MoreAmbushesSetting)
                    {
                        __instance.AmbushExploredSitesProtection = 0;
                        __instance.StartingAmbushProtection = 0;
                        if (TFTVVoidOmens.VoidOmensCheck[1])
                        {
                            __instance.ExplorationAmbushChance = 100;

                        }
                        else
                        {
                            __instance.ExplorationAmbushChance = 70;
                        }
                    }
                    else 
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[1])
                        {
                            __instance.ExplorationAmbushChance = 100;

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

