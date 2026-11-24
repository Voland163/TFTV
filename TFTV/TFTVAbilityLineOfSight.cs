using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Whether a player unit can target someone with an ability that needs line of sight (Induce Panic, Mind Control,
    /// healing...) flickered while nobody moved. TacticalAbility.CheckVisibleLineBetweenActors asks
    /// TacticalFactionVision.CheckVisibleLineBetweenActors without a target position, and then the line is traced to
    /// the target's live head and aim-point bones, which its idle animation keeps moving: a target at the edge of
    /// sight went in and out of the ability's targets from the same spot. Given a position, the same vanilla check
    /// uses the vision point for that position and a target dummy in its idle pose instead (what the AI uses for
    /// hypothetical positions). Player units now always pass the target's own position, so the answer holds still.
    /// </summary>
    internal static class TFTVAbilityLineOfSight
    {
        [HarmonyPatch(typeof(TacticalAbility), "CheckVisibleLineBetweenActors")]
        internal static class TacticalAbility_CheckVisibleLineBetweenActors_StableLineOfSight_Patch
        {
            private static bool Prefix(TacticalActorBase __0, Vector3 __1, TacticalActorBase __2, bool __3, ref bool __result)
            {
                try
                {
                    if (!(__0 is TacticalActor source) || !source.IsControlledByPlayer
                        || !(__2 is TacticalActor target) || target == source || !target.IsAlive || !target.InPlay || target.TargetDummy == null)
                    {
                        return true;
                    }

                    __result = TacticalFactionVision.CheckVisibleLineBetweenActors(__0, __1, __2, __3, target.Pos, 1f, null);
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
