using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVVanillaFixes.Geoscape
{
    internal class UIGeoscapeVanillaFixes
    {

        //Patch to fix Vanilla perception multipliers application
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "ApplyStatModification")]
        public static class Patch_ApplyStatModification_MultiplyFix
        {
            public static bool Prefix(
                ItemStatModification statModifier,
                ref float fPerception,
                ref float fAccuracy,
                ref float fStealth,
                ref float fPerceptionMult,
                ref float fAccuracyMult,
                ref float fStealthMult)
            {
                switch (statModifier.TargetStat)
                {
                    case StatModificationTarget.Perception:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fPerception += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fPerceptionMult *= statModifier.Value; // Option A
                        }
                        break;

                    case StatModificationTarget.Accuracy:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fAccuracy += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fAccuracyMult *= statModifier.Value; // Option A
                        }
                        break;

                    case StatModificationTarget.Stealth:
                        if (statModifier.Modification == StatModificationType.Add)
                        {
                            fStealth += statModifier.Value;
                        }
                        else if (statModifier.Modification == StatModificationType.Multiply)
                        {
                            fStealthMult *= statModifier.Value; // Option A
                        }
                        break;
                }

                // Skip original ApplyStatModification
                return false;
            }
        }





        //Fixes scanner showing colony detected for Palace
        [HarmonyPatch(typeof(SiteSurroundingsScanner), nameof(SiteSurroundingsScanner.AlienBasesAvailableInRange))]
        public static class SiteSurroundingsScanner_AlienBasesAvailableInRange_patch
        {

            public static void Postfix(SiteSurroundingsScanner __instance, GeoSite ____site, ref bool __result)
            {
                try
                {
                    Func<GeoSite, bool> querry = (GeoSite s) => s.GetComponent<GeoAlienBase>() != null && !s.GetComponent<GeoAlienBase>().IsPalace && s.GetInspected(____site.Owner) && s.State == GeoSiteState.Functioning;
                    MethodInfo methodInfo = typeof(SiteSurroundingsScanner).GetMethod("QuerryForAlienBases", BindingFlags.NonPublic | BindingFlags.Instance);
                    IEnumerable<GeoSite> eligibleSites = (IEnumerable<GeoSite>)methodInfo.Invoke(__instance, new object[] { querry });

                    __result = eligibleSites.Any();

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

