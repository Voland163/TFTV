using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PRMBetterClasses
{
    class RFaLaCarte
    {
        private static int shotLimit = 1;
        private static int turretsShotLimit = 1;
        private static float perceptionRatio = 0.5f;
        private static float turretsPerceptionRatio = 0.5f;
        private static bool allowBashRiposte = true;
        private static bool targetCanRetaliate = true;
        private static bool casualtiesCanRetaliate = true;
        private static bool bystandersCanRetaliate = true;
        private static bool checkFriendlyFire = true;
        private static float reactionAngleCos = 360;
        private static float turretsReactionAngleCos = 360;

        private static Dictionary<TacticalActor, int> returnFireCounter = new Dictionary<TacticalActor, int>();

        // ******************************************************************************************************************
        // ******************************************************************************************************************
        // Patched methods
        // ******************************************************************************************************************
        // ******************************************************************************************************************

        [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.PlayTurnCrt))]
        public static class TacticalFaction_PlayTurnCrt_Patch
        {
            public static void Prefix(TacticalFaction __instance)
            {
                // Keep in the map only the actors not belonging to the faction that is starting its turn
                returnFireCounter = returnFireCounter
                    .Where(actor => actor.Key.TacticalFaction != __instance)
                    .ToDictionary(actor => actor.Key, actor => actor.Value);
            }
        }

        [HarmonyPatch(typeof(TacticalLevelController), nameof(TacticalLevelController.GetReturnFireAbilities))]
        public static class TacticalLevelController_GetReturnFireAbilities_Patch
        {
            public static bool Prefix(TacticalLevelController __instance, ref List<ReturnFireAbility> __result,
                                      TacticalActor shooter, Weapon weapon, TacticalAbilityTarget target,
                                      bool getOnlyPossibleTargets = false, List<TacticalActor> casualties = null)
            {
                // No return fire for the following attacks
                WeaponDef weaponDef = weapon?.WeaponDef;
                if (target.AttackType == AttackType.ReturnFire
                    || target.AttackType == AttackType.Overwatch
                    || target.AttackType == AttackType.Synced
                    || target.AttackType == AttackType.ZoneControl
                    || weaponDef != null && weaponDef.NoReturnFireFromTargets)
                {
                    __result = null;
                    return false;
                }

                List<ReturnFireAbility> list;
                IEnumerable<TacticalActor> actors = __instance.Map.GetActors<TacticalActor>(null);
                using (MultiForceDummyTargetableLock multiForceDummyTargetableLock = new MultiForceDummyTargetableLock(actors))
                {
                    list = actors
                        // Get alive enemies for the shooter
                        .Where((TacticalActor actor) => {
                            return actor.IsAlive && actor.RelationTo(shooter) == FactionRelation.Enemy;
                        })
                        // Select the ones that have the return fire ability, ordered by priority
                        // Rmq: it is possible to have an actor twice if he has multiple RF abilities
                        .SelectMany((TacticalActor actor) =>
                            from a in actor.GetAbilities<ReturnFireAbility>()
                            orderby a.ReturnFireDef.ReturnFirePriority
                            select a, (TacticalActor actor, ReturnFireAbility ability) =>
                                new { actor = actor, ability = ability }
                        )
                        // Check if shooter is a valid target for each actor/ability
                        .Where((actorAbilities) =>
                            actorAbilities.ability.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)
                                && actorAbilities.ability.IsValidTarget(shooter)
                        )
                        // Group by actor and keep only first valid ability
                        .GroupBy((actorAbilities) => actorAbilities.actor, (actorAbilities) => actorAbilities.ability)
                        .Select((IGrouping<TacticalActor, ReturnFireAbility> actorAbilities) =>
                            new { actorReturns = actorAbilities, actorAbility = actorAbilities.First() }
                        )
                        // Make sure the target of the attack is the first one to retaliate
                        .OrderByDescending((actorAbilities) => actorAbilities.actorAbility.TacticalActor == target.GetTargetActor())
                        .Select((actorAbilities) => actorAbilities.actorAbility)
                        .Where((ReturnFireAbility returnFireAbility) => {
                            TacticalActor tacticalActor = returnFireAbility.TacticalActor;
                        // Check that he has not retaliated too much already
                        int actorShotLimit = tacticalActor.IsMetallic ? turretsShotLimit : shotLimit;
                            if (actorShotLimit > 0)
                            {
                                returnFireCounter.TryGetValue(tacticalActor, out var currentCount);
                                if (currentCount >= actorShotLimit)
                                {
                                    return false;
                                }
                            }
                        // Always allow bash riposte
                        if (returnFireAbility.ReturnFireDef.RiposteWithBashAbility)
                            {
                                return allowBashRiposte;
                            }
                        // Checks if the target is allowed to retaliate
                        // Rmq: Skipped when doing predictions on who will return fire (getOnlyPossibleTargets == false)
                        if (getOnlyPossibleTargets)
                            {
                                if (target.Actor == tacticalActor
                                    || target.MultiAbilityTargets != null && target.MultiAbilityTargets.Any((TacticalAbilityTarget mat) => mat.Actor == tacticalActor))
                                {
                                // The actor was one of the targets
                                if (!targetCanRetaliate) return false;
                                }
                                else if (casualties != null && casualties.Contains(tacticalActor))
                                {
                                // The actor was one of the casualties (not necessarily the target)
                                if (!casualtiesCanRetaliate) return false;
                                }
                                else
                                {
                                    if (!bystandersCanRetaliate) return false;
                                }
                            }
                            float actorReactionAngleCos = tacticalActor.IsMetallic ? turretsReactionAngleCos : reactionAngleCos;
                            if (!IsAngleOK(shooter, tacticalActor, actorReactionAngleCos))
                            {
                                return false;
                            }
                        // Check that target won't need to move to retaliate
                        ShootAbility defaultShootAbility = returnFireAbility.GetDefaultShootAbility();
                            TacticalAbilityTarget attackActorTarget = defaultShootAbility.GetAttackActorTarget(shooter, AttackType.ReturnFire);
                            if (attackActorTarget == null || !Utl.Equals(attackActorTarget.ShootFromPos, defaultShootAbility.Actor.Pos, 1E-05f))
                            {
                                return false;
                            }
                            TacticalActor tacticalActor1 = null;
                        // Prevent friendly fire
                        if (checkFriendlyFire && returnFireAbility.TacticalActor.TacticalPerception.CheckFriendlyFire(returnFireAbility.Weapon, attackActorTarget.ShootFromPos, attackActorTarget, out tacticalActor1, FactionRelation.Neutral | FactionRelation.Friend))
                            {
                                return false;
                            }
                            if (!returnFireAbility.TacticalActor.TacticalPerception.HasFloorSupportAt(returnFireAbility.TacticalActor.Pos))
                            {
                                return false;
                            }
                        // Check that we have a line of sight between both actors at a perception ratio (including stealth stuff)
                        float actorPerceptionRatio = tacticalActor.IsMetallic ? turretsPerceptionRatio : perceptionRatio;
                            if (!TacticalFactionVision.CheckVisibleLineBetweenActors(returnFireAbility.TacticalActor, returnFireAbility.TacticalActor.Pos,
                                shooter, false, null, actorPerceptionRatio))
                            {
                                return false;
                            }
                            return true;
                        }).ToList();
                }
                __result = list;
                return false;
            }
        }

        [HarmonyPatch(typeof(TacticalLevelController), nameof(TacticalLevelController.FireWeaponAtTargetCrt))]
        public static class TacticalLevelController_FireWeaponAtTargetCrt_Patch
        {
            public static void Prefix(Weapon weapon, TacticalAbilityTarget abilityTarget)
            {
                if (abilityTarget.AttackType == AttackType.ReturnFire)
                {
                    TacticalActor tacticalActor = weapon.TacticalActor;
                    returnFireCounter.TryGetValue(tacticalActor, out var currentCount);
                    returnFireCounter[tacticalActor] = currentCount + 1;
                }
            }
        }

        private static bool IsAngleOK(TacticalActor shooter, TacticalActorBase target, float reactionAngleCos)
        {
            if (reactionAngleCos > 0.99)
            {
                return true;
            }
            Vector3 targetForward = target.transform.TransformDirection(Vector3.forward);
            Vector3 targetToShooter = (shooter.Pos - target.Pos).normalized;
            float angleCos = Vector3.Dot(targetForward, targetToShooter);
            return Utl.GreaterThanOrEqualTo(angleCos, reactionAngleCos);
        }
    }
}
