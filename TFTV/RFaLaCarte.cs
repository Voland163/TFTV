using Base.Entities.Statuses;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
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
            public static bool Prefix(
                TacticalLevelController __instance,
                ref List<ReturnFireAbility> __result,
                TacticalActor shooter, Weapon weapon, TacticalAbilityTarget target,
                bool getOnlyPossibleTargets = false, List<TacticalActor> casualties = null)
            {
                // (unchanged header/early returns...)

                List<ReturnFireAbility> list;
                IEnumerable<TacticalActor> actors = __instance.Map.GetActors<TacticalActor>(null);
                using (MultiForceDummyTargetableLock multiForceDummyTargetableLock = new MultiForceDummyTargetableLock(actors))
                {
                    list = actors
                        .Where(actor => actor.IsAlive && actor.RelationTo(shooter) == FactionRelation.Enemy)
                        .SelectMany(actor =>
                            from a in actor.GetAbilities<ReturnFireAbility>()
                            orderby a.ReturnFireDef.ReturnFirePriority
                            select new { actor, ability = a })
                        .Where(pair =>
                        {
                            var ability = pair.ability;
                            var actor = ability.TacticalActor;

                            // Skip if already disabled by status (panic/stun/etc.)
                            if (actor.Status != null && actor.Status.GetStatuses<Status>()
                                    .Any(s => ability.DisabledByStatuses.Contains(s.Def)))
                                return false;

                            return ability.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)
                                   && ability.IsValidTarget(shooter);
                        })
                        .GroupBy(pair => pair.actor, pair => pair.ability)
                        .Select(group => new { actorReturns = group, actorAbility = group.First() })
                        .OrderByDescending(group => group.actorAbility.TacticalActor == target.GetTargetActor())
                        .Select(group => group.actorAbility)
                        .Where(returnFireAbility =>
                        {
                            // (rest of your existing filters: counters, bash riposte, angle, LoS, etc.)
                            // ... unchanged code ...
                            return true;
                        })
                        .ToList();
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
