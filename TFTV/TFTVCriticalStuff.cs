using Base.Defs;
using Base.Serialization;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVCriticalStuff
    {
      
        
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        /// <summary>
        /// Because we create new difficulties once the mod is loaded, and the games are loaded before the mod, 
        /// when loading a save from the title menu the savegame might not have a difficulty assigned to it.
        /// Massive craptastic poo cyclon without this.
        /// 
        /// Of course, now go on and remove it to see what happens!
        /// </summary>


        [HarmonyPatch(typeof(PhoenixGame), "FinishLevelAndLoadGame")]
        public static class DieAbility_LoadGame_patch
        {

            public static void Prefix(PPSavegameMetaData gameData)
            {
                try
                {
                    if (gameData.DifficultyDef != null)
                    {
                        TFTVLogger.Always($"{gameData?.DifficultyDef}");
                    }
                    else
                    {
                        gameData.DifficultyDef = DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");
                        TFTVLogger.Always($"{gameData?.DifficultyDef}");
                    }

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
