using Base.Entities.Statuses;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class DecimalWPTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(StatusStat), "ApplyStatModification")] //VERIFIED
        public static class StatusStat_ApplyStatModification_patch
        {
            public static void Prefix(StatusStat __instance, ref StatModification statMod)
            {
                try
                {
                    //TFTVLogger.Always($"ApplyStatModification: {__instance.Name}");

                    if (__instance.Name == "WillPoints")
                    {
                        float roundedF = Mathf.CeilToInt(statMod.Value);

                        if (roundedF != statMod.Value)
                        {
                            TFTVLogger.Always($"ApplyStatModification: rounding WP from {statMod.Value} to {roundedF}");
                            statMod.Value = roundedF;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(StatusStat), nameof(StatusStat.Set))]
        public static class StatusStat_Set_patch
        {
            public static void Prefix(StatusStat __instance, ref float f)
            {
                try
                {
                    // TFTVLogger.Always($"{__instance.Name}");

                    if (__instance.Name == "WillPoints")
                    {
                        float roundedF = Mathf.CeilToInt(f);

                        if (roundedF != f)
                        {
                            TFTVLogger.Always($"rounding WP from {f} to {roundedF}");
                            f = roundedF;
                        }
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
