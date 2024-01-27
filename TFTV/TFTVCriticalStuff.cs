using Base.Core;
using Base.Defs;
using Base.Serialization;
using Base.UI.MessageBox;
using Epic.OnlineServices;
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
using UnityEngine;

namespace TFTV
{
    internal class TFTVCriticalStuff
    {
      
        
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        [HarmonyPatch(typeof(NamedValueStore))]
        internal class NamedValueStore_Fix
        {
            // TODO: use TFTV config to save the selected difficulty
            public static int TFTV_Difficulty = TFTVMain.Main.Config.Difficulty;

            [HarmonyPrefix]
            [HarmonyPatch(nameof(NamedValueStore.SetValue))]
            public static void NamedValueStore_SetValue_Prefix(string name, ref object val)
            {
                if (name.Equals("Options_NewGameDifficultyOption"))
                {
                    TFTVLogger.Always($"NamedValueStore_SetValue_Prefix() called ...");
                    TFTVLogger.Always($"In: {name} - {val}");

                    // TODO: save the selected difficulty in TFTV config
                    TFTV_Difficulty = (int)val;
                    int difficultyToSave = Mathf.Clamp(TFTV_Difficulty - 1, 0, 3);
                    val = difficultyToSave;
                    TFTVLogger.Always($"Out: {name} - {val}");
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(NamedValueStore.GetValue))]
            public static void NamedValueStore_GetValue_Postfix(string name, ref object __result)
            {
                if (name.Equals("Options_NewGameDifficultyOption"))
                {
                    TFTVLogger.Always($"NamedValueStore_GetValue_Postfix() called ...");
                    TFTVLogger.Always($"In: {name} - {__result}");
                    // TODO: read the selected difficulty from TFTV config
                    __result = TFTV_Difficulty;
                    TFTVLogger.Always($"Out: {name} - {__result}");
                }
            }
        }




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
