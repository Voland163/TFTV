using Base.Core;
using Base.Defs;
using Base.Serialization;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static class PhoenixGame_FinishLevelAndLoadGame_patch
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
                        TFTVLogger.Always($"setting to {gameData?.DifficultyDef}");
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(ModManager), "ProcessGeoscapeInstanceData")]
        public static class ModManager_ProcessGeoscapeInstanceData_patch
        {

            public static void Prefix(ModManager __instance, GeoLevelController controller, GeoLevelInstanceData instanceData, List<ModGeoscape> ____gsMods)
            {
                try
                {
                    TFTVLogger.Always($"Running ModManager.ProcessGeoscapeInstanceData. __instance.CanUseMods? {__instance.CanUseMods} ____gsMods.Count?{____gsMods.Count}");

                    foreach (ModGeoscape mod in ____gsMods)
                    {
                        TFTVLogger.Always($"looking at {mod.Main.Instance.Entry.LocalizedName}, version number: {mod.Main.Instance.Entry.MetaData.Version} ");

                        if (!instanceData.ModData.TryGetValue(mod.Main.Instance.ID, out var value))
                        {
                            TFTVLogger.Always($"if triggered for {mod.Main.Instance.Entry.LocalizedName} ");
                            continue;
                        }

                        MethodInfo deserializeMethod = typeof(ModManager).GetMethod("DeserializeModObject", BindingFlags.Instance | BindingFlags.NonPublic);

                        // Invoke the DeserializeModObject method
                        object[] parameters = { mod.Main, value };
                        object modData = deserializeMethod.Invoke(__instance, parameters);

                        TFTVLogger.Always($"modData null? {modData == null}");

                        if (modData == null && mod.Main.Instance.Entry.LocalizedName == "TFTV")
                        {
                            string warning = "TFTV save data is null! This save is borked! Please load an earlier save.";

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                        }

                        /*   if (modData != null)
                           {
                               __instance.TryInvokeModMethod(mod, delegate
                               {
                                   mod.ProcessGeoscapeInstanceData(modData);
                               }, "ProcessGeoscapeInstanceData");
                           }*/

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
