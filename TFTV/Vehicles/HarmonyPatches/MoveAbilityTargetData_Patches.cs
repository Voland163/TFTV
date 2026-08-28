using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using TFTVVehicleRework.Abilities;
using UnityEngine;

namespace TFTVVehicleRework.HarmonyPatches
{
    /// <summary>
    /// Prices the boarding marker against the vehicle that would actually be boarded.
    ///
    /// TacUtil draws an ability's ground markers by running the operative's move targets
    /// through MoveAbilityTargetData.IsActorInActionRange(actor, ability) and handing the
    /// survivors to SceneViewElement.GetPositionMarkers(), which keeps only tiles where
    /// GetTargetsAt() finds something to board. So the marker is already correct about
    /// "can this operative board from here" - the filter above it is what was wrong.
    ///
    /// That filter prices the ability through GetMaxMoveAndActRange(), which knows the
    /// ability but not the destination, so it read the entry cost from whatever discount
    /// happened to be registered on the operative. Registering the discount up front made
    /// the marker appear but let it apply to any vehicle; not registering it priced boarding
    /// at full cost and hid the marker from an operative with less than 1 AP.
    ///
    /// The destination is known here, so the discount granted by the vehicles boardable from
    /// that exact tile is registered just for this one evaluation.
    /// </summary>
    [HarmonyPatch(typeof(MoveAbilityTargetData), nameof(MoveAbilityTargetData.IsActorInActionRange),
        new Type[] { typeof(TacticalActor), typeof(TacticalAbility) })]
    internal static class MoveAbilityTargetData_IsActorInActionRange_Patch
    {
        private static bool Prefix(
            MoveAbilityTargetData __instance,
            TacticalActor actor,
            TacticalAbility ability,
            ref bool __result)
        {
            try
            {
                ExtendedEnterVehicleAbility enterVehicle = ability as ExtendedEnterVehicleAbility;
                if (enterVehicle == null || actor == null || actor.IsMounted)
                {
                    return true;
                }

                // GetTargetsAt() casts for line of sight, and this runs once per candidate
                // tile, so only ask about tiles close enough to a vehicle to be one of its
                // entry points.
                if (!IsNearAVehicle(actor, __instance.Position))
                {
                    return true;
                }

                TacticalAbilityCostModification modification =
                    enterVehicle.GetEntryCostModificationAt(__instance.Position);

                if (modification == null)
                {
                    return true;
                }

                actor.AddAbilityCostModification(modification);
                try
                {
                    __result = __instance.IsPositionInRange(actor.GetMaxMoveAndActRange(ability));
                }
                finally
                {
                    actor.RemoveAbilityCostModification(modification);
                }

                return false;
            }
            catch (Exception e)
            {
                TFTV.TFTVLogger.Error(e);
                return true;
            }
        }

        private const float MaxEntryPointDistance = 6f;

        private static bool IsNearAVehicle(TacticalActor actor, Vector3 position)
        {
            TacticalFaction faction = actor.TacticalFaction;
            if (faction == null)
            {
                return false;
            }

            foreach (TacticalActor vehicleActor in faction.TacticalActors)
            {
                if (vehicleActor == null || vehicleActor.Vehicle == null)
                {
                    continue;
                }

                if ((vehicleActor.Pos - position).sqrMagnitude
                    <= MaxEntryPointDistance * MaxEntryPointDistance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
