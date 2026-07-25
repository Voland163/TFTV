using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class WPtoMistLossTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(TacticalActor), "ApplyMistEffects")]
        internal static class SkipFirstTurnMistEffectsPatch
        {
            private static bool Prefix(TacticalActor __instance)
            {
                return __instance.TurnNumber > 1;
            }
        }
    }
}
