using Base.AI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// How much the AI wants to shoot a vehicle.
    ///
    /// Vanilla scores a shot by the damage it is expected to do (after armour, averaged over the target's parts),
    /// times the share of the target's parts it can see, times a threat weight (0.5 to 1, highest for whoever has
    /// hurt the faction most). The damage counts in points, whoever takes them. A soldier in cover shows only some
    /// of its parts; a vehicle can't take cover and is usually the busiest player unit. So rifle fire that takes 60
    /// of a buggy's 750 was preferred to a soldier for whom the same burst is half its health: in the first tester
    /// logs about three in four aimed enemy attacks went at the player's vehicle, which lost at most 40% of its
    /// health in a mission and came back repaired.
    ///
    /// A shot at a vehicle now scores sqrt(typical soldier health / the vehicle's current health) of what it did,
    /// capped at 1. "Typical" is the median maximum health of the vehicle's side's non-vehicle units on the map.
    /// A full buggy against 150-health soldiers is worth about 0.45 of before: rifles mostly turn back to the
    /// soldiers, heavy weapons (which do several times the damage) still take the vehicle on, and a vehicle that
    /// has been worn down draws fire again: at or below soldier health the factor is 1, so finishing a vehicle off
    /// scores as in vanilla.
    /// </summary>
    internal static class TFTVAIVehicleTargets
    {
        private const float FallbackSoldierHealth = 150f;

        private static GameTagDef _vehicleTag;
        private static bool _tagLooked;

        private static readonly Dictionary<TacticalFaction, float> SoldierHealthByFaction = new Dictionary<TacticalFaction, float>();
        private static int _healthFrame = -1;

        private static readonly HashSet<TacticalActorBase> LoggedThisTurn = new HashSet<TacticalActorBase>();
        private static int _scaledThisTurn;

        internal static void ClearTurn()
        {
            if (_scaledThisTurn > 0)
            {
                TFTVLogger.Always($"TFTV AI vehicle targets: {_scaledThisTurn} shot scores at vehicles scaled last turn");
            }

            _scaledThisTurn = 0;
            LoggedThisTurn.Clear();
            SoldierHealthByFaction.Clear();
        }

        private static bool IsVehicle(TacticalActorBase actor)
        {
            if (!_tagLooked)
            {
                _vehicleTag = CommonHelpers.GetSharedGameTags().VehicleTag;
                _tagLooked = true;
            }

            return _vehicleTag != null && actor.HasGameTag(_vehicleTag);
        }

        private static float SoldierHealth(TacticalFaction faction)
        {
            if (_healthFrame != Time.frameCount)
            {
                SoldierHealthByFaction.Clear();
                _healthFrame = Time.frameCount;
            }

            if (SoldierHealthByFaction.TryGetValue(faction, out float health))
            {
                return health;
            }

            List<float> healths = faction.TacticalActors
                .Where(a => a != null && a.IsAlive && !IsVehicle(a))
                .Select(a => (float)a.Health.Max)
                .Where(h => h > 0f)
                .OrderBy(h => h)
                .ToList();

            health = healths.Count > 0 ? healths[healths.Count / 2] : FallbackSoldierHealth;
            SoldierHealthByFaction[faction] = health;
            return health;
        }

        /// <summary>The multiplier for a shot at <paramref name="vehicle"/>: 1 for anything that isn't a vehicle.</summary>
        internal static float Factor(TacticalActorBase vehicle, out float soldierHealth, out float vehicleHealth)
        {
            soldierHealth = 0f;
            vehicleHealth = 0f;

            if (vehicle == null || vehicle.TacticalFaction == null || !IsVehicle(vehicle))
            {
                return 1f;
            }

            vehicleHealth = vehicle.Health;
            if (vehicleHealth <= 0f)
            {
                return 1f;
            }

            soldierHealth = SoldierHealth(vehicle.TacticalFaction);
            return Mathf.Min(1f, Mathf.Sqrt(soldierHealth / vehicleHealth));
        }

        [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithShootAbility")]
        internal static class AIAttackPositionConsideration_EvaluateWithShootAbility_VehicleTarget_patch
        {
            public static void Postfix(IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (__result <= 0f || !(target is TacAITarget tacAITarget) || tacAITarget.Actor == null)
                    {
                        return;
                    }

                    float factor = Factor(tacAITarget.Actor, out float soldierHealth, out float vehicleHealth);
                    if (factor >= 1f)
                    {
                        return;
                    }

                    __result *= factor;
                    _scaledThisTurn++;

                    if (LoggedThisTurn.Add(tacAITarget.Actor))
                    {
                        string shooter = actor is TacticalActor tacActor ? tacActor.DisplayName : "?";
                        TFTVLogger.Always($"TFTV AI vehicle targets: shots at {tacAITarget.Actor.DisplayName} ({vehicleHealth:0} health; its side's soldiers ~{soldierHealth:0}) score x{factor:0.00} this turn (first: {shooter})");
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
