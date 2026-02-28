using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions.Modifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class PsychoSociologyAddHavenDefenders
    {
        [HarmonyPatch(typeof(GeoHaven), "GetDeployment")]
        public static class GeoHaven_GetDeployment_HavenDefenseExtraDefenders_Patch
        {
            private const string HavenDefenseExtraDefendersModifierKey = "HavenDefenseExtraDefenders";

            public static void Postfix(GeoHaven __instance, ref int __result)
            {
                GeoFactionStatModifiers factionStatModifiers = __instance.Site.Owner.FactionStatModifiers;
                if (factionStatModifiers == null)
                {
                    return;
                }
                float num;
                if (!factionStatModifiers.FactionGameplayMultipliers.TryGetValue(HavenDefenseExtraDefendersModifierKey, out num))
                {
                    return;
                }
                int num2 = Mathf.Clamp(Mathf.RoundToInt(num), 0, 3);
                if (num2 <= 0)
                {
                    return;
                }
                __result += num2 * 100;
            }
        }
    }
}
