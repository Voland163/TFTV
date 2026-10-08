using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Guarantees a minimum movement so a crippled operative can always limp to the evac zone.
    ///
    /// A turn's AP pool is the Speed stat (AP max = Max(1.3, Speed)), and one AP point buys
    /// TacticalNav.DistanceToAPFactor tiles. Disabled legs remove their armour's Speed bonus, which can
    /// push Speed below zero and leave 1.3 tiles for the whole turn. Raising the tiles per AP point
    /// (instead of AP max) changes movement only: abilities cost a fraction of AP max, so they are
    /// unaffected.
    ///
    /// - Not Slowed: at least 1 tile per AP (4 tiles per full turn).
    /// - Slowed: at least 1 tile for all 4 AP.
    ///
    /// Ancient guardians are excluded: TFTV ties their Speed to their WP on purpose.
    /// </summary>
    internal static class TFTVMinimumMovement
    {
        private const float MinTilesPerTurn = 4f;
        private const float MinTilesPerTurnWhenSlowed = 1f;

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static StatusDef _slowedStatus;
        private static ClassTagDef _hopliteTag;
        private static ClassTagDef _cyclopsTag;

        private static readonly Action<TacticalActor> CalculateEncumberanceFactor =
            AccessTools.MethodDelegate<Action<TacticalActor>>(AccessTools.Method(typeof(TacticalActor), "CalculateEncumberanceFactor"));

        private static void ApplyFloor(TacticalActor actor)
        {
            if (actor == null || actor.TacticalNav == null || actor.CharacterStats == null || actor.Status == null || actor.GetAbility<MoveAbility>() == null)
            {
                return;
            }

            if (_slowedStatus == null)
            {
                _slowedStatus = DefCache.GetDef<StatusDef>("Slowed_StatusDef");
                _hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                _cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
            }

            if (actor.HasGameTag(_hopliteTag) || actor.HasGameTag(_cyclopsTag))
            {
                return;
            }

            float apMax = actor.CharacterStats.ActionPoints.Max;

            if (apMax <= 0f)
            {
                return;
            }

            float minTiles = actor.Status.HasStatus(_slowedStatus) ? MinTilesPerTurnWhenSlowed : MinTilesPerTurn;

            if (apMax * actor.TacticalNav.DistanceToAPFactor < minTiles)
            {
                actor.TacticalNav.DistanceToAPFactor = minTiles / apMax;
            }
        }

        /// <summary>
        /// The only place vanilla recalculates the tiles per AP point; it runs on every stat update.
        /// </summary>
        [HarmonyPatch(typeof(TacticalActor), "CalculateEncumberanceFactor")]
        internal static class TacticalActor_CalculateEncumberanceFactor_Patch
        {
            public static void Postfix(TacticalActor __instance)
            {
                try
                {
                    ApplyFloor(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// Slowed changes Speed before it joins the status list, so the stat update above can't see it yet.
        /// Recalculate once the list has changed.
        /// </summary>
        [HarmonyPatch(typeof(StatusComponent), "OnStatusChanged")]
        internal static class StatusComponent_OnStatusChanged_Patch
        {
            public static void Postfix(StatusComponent __instance)
            {
                try
                {
                    TacticalActor actor = __instance.GetComponent<TacticalActor>();

                    if (actor != null && actor.TacticalNav != null && actor.CharacterStats != null)
                    {
                        CalculateEncumberanceFactor(actor);
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
