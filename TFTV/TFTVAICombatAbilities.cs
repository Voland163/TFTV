using Base.AI;
using Base.AI.Defs;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Three additions to the tactical AI:
    ///
    /// 1. Spider drone launcher. The launcher deals (next to) no damage - it drops friendly drones -
    ///    so AIAttackPositionConsideration always scores it 0 and no AI ever fires one. We give the
    ///    launcher its own MoveAndShoot-derived action with its own equipment target generator and
    ///    score the shot by what is standing around the impact point instead of by damage.
    ///
    /// 2. Bash. AIActionMoveAndAttack only reaches a BashAbility when the target weapon's payload is
    ///    Melee, so a humanoid holding a gun never bashes. We add a MoveAndStrike-derived action that
    ///    generates targets from the gun the actor is holding but executes the actor's own
    ///    "bash with whatever you can" ability - the same attack the player gets on the weapon.
    ///
    /// 3. Mind control reservations. At the start of an AI faction turn we predict which enemy each
    ///    mind controller is going to take, and stop the rest of the faction from spending its turn
    ///    shooting that enemy.
    ///
    /// See TFTVArtOfCrab for the rest of the AI work; this file only holds the three features above.
    /// </summary>
    internal class TFTVAICombatAbilities
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        /// <summary>Def name of the AI action template used by human and humanoid actors.</summary>
        private const string SoldierAITemplate = "AIActionsTemplateDef";

        /// <summary>Called once from TFTVDefsInjectedOnlyOnce.ChangesToAI.</summary>
        public static void CreateAIActionDefs()
        {
            SpiderDroneLauncherAI.CreateDefs();
            BashAI.CreateDefs();
        }

        #region shared def surgery helpers

        /// <summary>
        /// Adds an action to an AI actions template, unless it is already there.
        /// </summary>
        private static void AddActionToTemplate(string templateName, AIActionDef actionDef)
        {
            AIActionsTemplateDef template = DefCache.GetDef<AIActionsTemplateDef>(templateName);

            if (template.ActionDefs.Contains(actionDef))
            {
                return;
            }

            template.ActionDefs = template.ActionDefs.Append(actionDef).ToArray();
            TFTVLogger.Always($"TFTV AI: added {actionDef.name} (weight {actionDef.Weight}) to {templateName}");
        }

        /// <summary>
        /// Points every equipment target generator of an action at <paramref name="replacement"/>.
        /// The generator decides which of the actor's items the action gets to attack with, so this
        /// is what confines a cloned action to one weapon.
        /// </summary>
        private static void ReplaceEquipmentGenerator(AIActionDef actionDef, AIActorEquipmentTargetGeneratorDef replacement)
        {
            int replaced = 0;

            foreach (AITargetEvaluation evaluation in actionDef.Evaluations)
            {
                if (evaluation.TargetGeneratorDef is AIActorEquipmentTargetGeneratorDef)
                {
                    evaluation.TargetGeneratorDef = replacement;
                    replaced++;
                }

                if (evaluation.FallbackTargetGeneratorDef is AIActorEquipmentTargetGeneratorDef)
                {
                    evaluation.FallbackTargetGeneratorDef = replacement;
                    replaced++;
                }
            }

            if (replaced == 0)
            {
                TFTVLogger.Always($"TFTV AI WARNING: {actionDef.name} has no equipment target generator to replace with {replacement.name}");
            }
        }

        /// <summary>
        /// Swaps every AIAttackPositionConsiderationDef of an action for a private clone and returns it,
        /// so that the scoring patch below can tell our considerations apart from the shared ones.
        /// </summary>
        private static AIAttackPositionConsiderationDef ClonePrivateAttackPositionConsideration(AIActionDef actionDef, string guid, string name)
        {
            AIAttackPositionConsiderationDef clone = null;

            foreach (AITargetEvaluation evaluation in actionDef.Evaluations)
            {
                for (int i = 0; i < evaluation.Considerations.Length; i++)
                {
                    if (evaluation.Considerations[i].Consideration is AIAttackPositionConsiderationDef attackPosition)
                    {
                        if (clone == null)
                        {
                            clone = Helper.CreateDefFromClone(attackPosition, guid, name);
                        }

                        evaluation.Considerations[i].Consideration = clone;
                    }
                }
            }

            if (clone == null)
            {
                TFTVLogger.Always($"TFTV AI WARNING: {actionDef.name} has no attack position consideration to clone");
            }

            return clone;
        }

        /// <summary>
        /// Drops considerations of the given def types from an action, early exits included. Used to
        /// strip the melee-equipment gates off the cloned bash action.
        /// </summary>
        private static void RemoveConsiderations(AIActionDef actionDef, params Type[] considerationDefTypes)
        {
            bool Keep(AIAdjustedConsideration adjusted)
            {
                return adjusted.Consideration != null
                    && !considerationDefTypes.Any(t => t.IsInstanceOfType(adjusted.Consideration));
            }

            actionDef.EarlyExitConsiderations = actionDef.EarlyExitConsiderations.Where(Keep).ToArray();

            foreach (AITargetEvaluation evaluation in actionDef.Evaluations)
            {
                evaluation.Considerations = evaluation.Considerations.Where(Keep).ToArray();
            }
        }

        #endregion

        #region 1. spider drone launcher

        internal static class SpiderDroneLauncherAI
        {
            private const string LauncherWeaponDefName = "SY_SpiderDroneLauncher_WeaponDef";

            /// <summary>
            /// The drones land where the shot lands, and all they can do on the turn after that is walk
            /// up to something and detonate. Dropped in an enemy's lap they are just a free kill, so the
            /// shot is aimed short of the target - back along the line towards the shooter - leaving the
            /// drones this much room to work with. First distance that clears both this and
            /// MinShooterClearance wins.
            /// </summary>
            private static readonly float[] StandOffDistances = { 6f, 5f, 4f, 7f, 8f };

            /// <summary>
            /// Directions tried around the target, in degrees off the line back towards the shooter.
            /// Straight back is preferred - the drones land on our side of the gap and the shot is
            /// short - but once the shooter has closed in, that direction lands them at its own feet,
            /// and a drop out to the flank is the only one left.
            /// </summary>
            private static readonly float[] DropDirections = { 0f, 40f, -40f, 80f, -80f, 120f, -120f, 160f };

            /// <summary>No drone is dropped closer than this to any enemy, target or not.</summary>
            private const float MinStandOff = 4f;

            /// <summary>...and not in the shooter's own lap either.</summary>
            private const float MinShooterClearance = 3f;

            /// <summary>How far a drone is expected to travel before detonating on the following turn.</summary>
            private const float DroneStrikeRange = 10f;

            /// <summary>
            /// Score for dropping drones next to a single enemy, and what each further enemy in the
            /// blast adds. A stock shoot score is damage/MaxDamage, i.e. roughly 0.3-0.5 for a decent
            /// burst, so these keep the launcher below an ordinary shot at a lone target and ahead of a
            /// poor one at a crowd.
            /// </summary>
            private const float LoneEnemyScore = 0.25f;
            private const float ScorePerExtraEnemy = 0.15f;
            private const float MaxCoverageScore = 0.6f;

            internal static AIActionMoveAndAttackDef ActionDef;
            internal static AIAttackPositionConsiderationDef ConsiderationDef;

            private static WeaponDef _launcher;

            // Why the launcher went unused, tallied over one actor's turn and flushed by
            // LogTurnDiagnostics. Fired once and then three turns of nothing is very hard to explain
            // from the outside, so the AI says which check it tripped over.
            private static int _evaluations;
            private static int _outOfAmmo;
            private static int _abilityUnavailable;
            private static int _noDropPosition;
            private static int _noLineOfFire;
            private static float _bestScore;
            private static TacticalActor _diagnosticsActor;

            internal static void CreateDefs()
            {
                try
                {
                    _launcher = DefCache.GetDef<WeaponDef>(LauncherWeaponDefName);

                    // The launcher isn't reachable through the stock gun generator, and giving it
                    // GunWeapon_TagDef would drag it into overwatch/return fire/suppression weapon
                    // choice, so it gets a tag of its own.
                    GameTagDef launcherTag = TFTVCommonMethods.CreateNewTag(
                        "TFTV_SpiderDroneLauncher", "9c1f4a70-2b3e-4d51-8a6f-1e2c7b90d411");

                    if (!_launcher.Tags.Contains(launcherTag))
                    {
                        _launcher.Tags.Add(launcherTag);
                    }

                    AIActionMoveAndAttackDef moveAndShoot = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");

                    ActionDef = Helper.CreateDefFromClone(
                        moveAndShoot,
                        "3e8b5c21-77d4-4a0e-9b13-5f6a2d84c902",
                        "TFTV_MoveAndLaunchSpiderDrone_AIActionDef");

                    AIActorEquipmentTargetGeneratorDef launcherGenerator = Helper.CreateDefFromClone(
                        DefCache.GetDef<AIActorEquipmentTargetGeneratorDef>("Gun_AITargetGeneratorDef"),
                        "5a2d9f13-0c46-4e78-b2a1-8d3e7c15f460",
                        "TFTV_SpiderDroneLauncher_AITargetGeneratorDef");
                    launcherGenerator.EquipmentTags = new GameTagDef[] { launcherTag };

                    ReplaceEquipmentGenerator(ActionDef, launcherGenerator);

                    // The generator above already guarantees a usable launcher, and the gun-tag gates
                    // inherited from MoveAndShoot would veto the action for an actor carrying nothing else.
                    RemoveConsiderations(ActionDef, typeof(AICanUseEquipmentConsiderationDef), typeof(AIHasEquipmentConsiderationDef));

                    ConsiderationDef = ClonePrivateAttackPositionConsideration(
                        ActionDef,
                        "7b4e1c88-9a52-4f30-86d7-c1e0b93a2f55",
                        "TFTV_SpiderDroneLauncherAttackPosition_AIConsiderationDef");

                    AddActionToTemplate(SoldierAITemplate, ActionDef);

                    // Reload_AIActionDef is fed by the same gun-tagged equipment generator, so an
                    // infiltrator whose entire loadout is a launcher and one spare clip fires once and
                    // then has nothing to do for the rest of the mission. The reload machinery itself is
                    // weapon-agnostic; it just needs to be pointed at the launcher.
                    AIActionReloadDef reload = DefCache.GetDef<AIActionReloadDef>("Reload_AIActionDef");

                    AIActionReloadDef launcherReload = Helper.CreateDefFromClone(
                        reload,
                        "4c93a6e1-58bd-4207-9f3a-2e7c15d80b64",
                        "TFTV_ReloadSpiderDroneLauncher_AIActionDef");

                    ReplaceEquipmentGenerator(launcherReload, launcherGenerator);
                    RemoveConsiderations(launcherReload, typeof(AICanUseEquipmentConsiderationDef), typeof(AIHasEquipmentConsiderationDef));

                    AddActionToTemplate(SoldierAITemplate, launcherReload);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Damage-free replacement for AIAttackPositionConsideration.EvaluateWithShootAbility:
            /// the launcher is worth firing when the drones land in a crowd, not when they hurt.
            /// </summary>
            internal static float Score(TacticalActor actor, TacAITarget aiTarget)
            {
                try
                {
                    if (_launcher == null || !(aiTarget.Equipment is Weapon weapon) || weapon.WeaponDef != _launcher)
                    {
                        return 0f;
                    }

                    _evaluations++;
                    _diagnosticsActor = actor;

                    if (!weapon.IsUsable || !weapon.HasCharges)
                    {
                        _outOfAmmo++;
                        return 0f;
                    }

                    ShootAbility shootAbility = weapon.DefaultShootAbility;

                    if (shootAbility == null
                        || !shootAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints))
                    {
                        _abilityUnavailable++;
                        return 0f;
                    }

                    TacticalActorBase targetActor = aiTarget.Actor;

                    if (targetActor == null || !targetActor.IsAlive || actor.RelationTo(targetActor) != FactionRelation.Enemy)
                    {
                        return 0f;
                    }

                    // No room to land the drones anywhere near this enemy means no shot worth taking.
                    Vector3? dropPosition = FindDropPosition(actor, weapon, aiTarget.Pos, targetActor,
                        out TacticalAbilityTarget shootTarget, out bool anyCandidateStood);

                    if (dropPosition == null)
                    {
                        if (anyCandidateStood)
                        {
                            _noLineOfFire++;
                        }
                        else
                        {
                            _noDropPosition++;
                        }

                        return 0f;
                    }

                    // Hand the executing action the exact shot this score is for, the way
                    // AIAccurateWeaponAttackPositionConsideration does. Recomputing it at execution time
                    // instead let the two disagree: the drones went to whatever the vanilla fallback
                    // aimed at - the enemy - or the shot came out null and ended the actor's turn.
                    aiTarget.TacticalAbilityTarget = shootTarget;

                    int enemiesInReach = CountEnemiesWithin(actor, dropPosition.Value, DroneStrikeRange);
                    float coverage = Mathf.Clamp(LoneEnemyScore + ScorePerExtraEnemy * (enemiesInReach - 1), 0f, MaxCoverageScore);
                    float score = Mathf.Clamp(coverage * AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, targetActor), 0f, 1f);

                    if (score > _bestScore)
                    {
                        _bestScore = score;
                    }

                    return score;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// Reports why the launcher went unused this turn, then resets the tally. Called from the
            /// TacAIActor.EndTurn patch in TFTVArtOfCrab; silent for actors that carry no launcher.
            /// </summary>
            internal static void LogTurnDiagnostics(TacticalActor actor)
            {
                if (_evaluations == 0)
                {
                    return;
                }

                // The counters are global, so they belong to whoever was scoring, not to whichever actor
                // happens to be ending its turn.
                TFTVLogger.Always($"TFTV AI: {_diagnosticsActor?.DisplayName} launcher evaluated {_evaluations}x - " +
                    $"out of ammo {_outOfAmmo}, ability unavailable {_abilityUnavailable}, " +
                    $"no drop position {_noDropPosition}, no line of fire {_noLineOfFire}, best score {_bestScore:f2}");

                _evaluations = 0;
                _outOfAmmo = 0;
                _abilityUnavailable = 0;
                _noDropPosition = 0;
                _noLineOfFire = 0;
                _bestScore = 0f;
                _diagnosticsActor = null;
            }

            /// <summary>
            /// Where the drones should actually land: back along the line from the target towards the
            /// shooter, at the first stand-off distance that leaves room from every enemy, stays clear
            /// of the shooter, and lands somewhere a unit can stand. Null when the target is too close
            /// for a drone to fit in between.
            /// </summary>
            /// <summary>
            /// Raycast-and-shot checks the drop search may spend per firing position per enemy. Scoring
            /// runs it across the whole movement zone, so an unbounded ring search would be felt in the
            /// AI turn time. The first candidate is the one that normally answers.
            /// </summary>
            private const int MaxDropChecks = 4;

            /// <summary>
            /// The shot is aimed slightly above the drop point rather than at the floor itself, which
            /// the weapon's shoot-position check deals with poorly. SpawnActorEffect snaps the impact
            /// back down to the floor, and staying under its 0.4m "spawn in the air and fall" threshold
            /// keeps the drones landing on the spot rather than being dropped onto it.
            /// </summary>
            private const float DropAimHeight = 0.35f;
            /// <summary>
            /// Picks where the drones should land and the shot that puts them there, both worked out
            /// from the position the shot would be fired from. Whether the shot can reach the drop point
            /// is decided inside the search rather than after it - a point the weapon can't hit is not a
            /// candidate at all, it just means trying the next direction.
            ///
            /// Candidates are the ring around the target at each stand-off distance, straight back
            /// towards the shooter first, so the usual answer is the first one tried and the search
            /// costs one raycast and one shot check. The budget caps the pathological case.
            /// </summary>
            private static Vector3? FindDropPosition(TacticalActor actor, Weapon weapon, Vector3 shootFromPos,
                TacticalActorBase targetActor, out TacticalAbilityTarget shootTarget, out bool anyCandidateStood)
            {
                shootTarget = null;
                anyCandidateStood = false;

                Vector3 towardsShooter = shootFromPos - targetActor.Pos;
                towardsShooter.y = 0f;

                Vector3 baseDirection = towardsShooter.sqrMagnitude > 0.01f ? towardsShooter.normalized : Vector3.forward;
                TacticalMap map = actor.Map;
                int checksSpent = 0;

                foreach (float standOff in StandOffDistances)
                {
                    foreach (float degrees in DropDirections)
                    {
                        Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.up) * baseDirection;
                        Vector3 candidate = targetActor.Pos + direction * standOff;

                        // Cheap rejections before the raycast and the navmesh query.
                        if ((candidate - shootFromPos).magnitude < MinShooterClearance
                            || !IsClearOfEnemies(actor, candidate))
                        {
                            continue;
                        }

                        if (++checksSpent > MaxDropChecks)
                        {
                            return null;
                        }

                        candidate = map.SnapXYZ(TacticalMap.SnapXZToGrid(candidate),
                            UnityLayers.FloorAllMask, 0.5f, snapToBottomOnInvalidCast: true);

                        // A spider drone is smaller than the shooter, so anywhere the shooter fits does.
                        if (!actor.NavigationComponent.INavMesh.ValidPosition(
                            candidate, actor.NavigationComponent.NavAreas, 0.25f, ignoreObstacles: true))
                        {
                            continue;
                        }

                        anyCandidateStood = true;

                        // Let the weapon build the target, so range, line of fire, and ShootFromPos are
                        // all settled by the code that will actually fire the shot.
                        shootTarget = weapon.TryGetShootTarget(
                            new TacticalAbilityTarget(candidate + DropAimHeight * Vector3.up), shootFromPos);

                        if (shootTarget != null)
                        {
                            return candidate;
                        }
                    }
                }

                return null;
            }

            /// <summary>The drop point has to give the drones room from every enemy, not just the target.</summary>
            private static bool IsClearOfEnemies(TacticalActor actor, Vector3 position)
            {
                foreach (TacticalActorBase enemy in actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.All)))
                {
                    if (enemy != null && enemy.IsAlive && (enemy.Pos - position).magnitude < MinStandOff)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static int CountEnemiesWithin(TacticalActor actor, Vector3 position, float radius)
            {
                int count = 0;

                foreach (TacticalActorBase enemy in actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.All)))
                {
                    if (enemy != null && enemy.IsAlive && (enemy.Pos - position).magnitude <= radius)
                    {
                        count++;
                    }
                }

                return count;
            }

            /// <summary>
            /// Redirects the shot from the enemy to the drone drop point. Called from the
            /// AIActionMoveAndAttack.GetAttackTarget prefix in TFTVArtOfCrab, which owns that method.
            /// Returns null for anything that isn't our launch action, and also when no drop point can
            /// be found from where the actor ended up - the caller then aims at the enemy as usual.
            /// </summary>
            internal static TacticalAbilityTarget TryGetLaunchTarget(AIActionMoveAndAttack action, TacticalAbility ability, TacAITarget aiTarget)
            {
                try
                {
                    if (ActionDef == null || action.BaseDef != ActionDef
                        || !(ability is ShootAbility shootAbility) || aiTarget?.Actor == null)
                    {
                        return null;
                    }

                    TacticalActor actor = shootAbility.TacticalActor;

                    if (actor == null)
                    {
                        return null;
                    }

                    if (shootAbility.Weapon == null)
                    {
                        return null;
                    }

                    // Normally the shot the score was for, carried over from Score. The recompute is
                    // only for targets that reached execution without having been scored by us.
                    TacticalAbilityTarget launchTarget = aiTarget.TacticalAbilityTarget;

                    if (launchTarget == null)
                    {
                        FindDropPosition(actor, shootAbility.Weapon, actor.Pos, aiTarget.Actor, out launchTarget, out _);
                    }

                    if (launchTarget == null)
                    {
                        TFTVLogger.Always($"TFTV AI: {actor.DisplayName} has no drone drop point from {actor.Pos}; " +
                            $"falling back to aiming at {aiTarget.Actor.DisplayName}");
                        return null;
                    }

                    Vector3 dropPosition = launchTarget.GetWorkingPosition();

                    TFTVLogger.Always($"TFTV AI: {actor.DisplayName} is launching spider drones at " +
                        $"{dropPosition}, {(dropPosition - aiTarget.Actor.Pos).magnitude:f1} from {aiTarget.Actor.DisplayName}");

                    return launchTarget;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return null;
                }
            }
        }

        #endregion

        #region 2. bash

        internal static class BashAI
        {
            /// <summary>
            /// Bash is a fallback, not a plan: half the weight of shooting, so the AI only reaches for
            /// it when it can't shoot the target or the shot is worth very little.
            /// </summary>
            private const float WeightFractionOfShooting = 0.5f;

            /// <summary>
            /// Shock is most of a bash's payload and armour does not stop it, so counting it in full is
            /// what had infiltrators clubbing Armadillos for 0 HP + 0 ARM instead of firing. A dazed
            /// target is worth having, but nothing like a dead one.
            /// </summary>
            private const float StunScoreWeight = 0.25f;

            /// <summary>
            /// Below this the bash isn't worth an action point. Same cutoff vanilla applies to melee in
            /// AIAttackPositionConsideration.EvaluateWithBashAbility.
            /// </summary>
            private const float MinUsefulDamage = 10f;

            internal static AIActionMoveAndAttackDef ActionDef;
            internal static AIAttackPositionConsiderationDef ConsiderationDef;

            internal static void CreateDefs()
            {
                try
                {
                    AIActionMoveAndAttackDef moveAndStrike = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndStrike_AIActionDef");
                    AIActionMoveAndAttackDef moveAndShoot = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");

                    // MoveAndStrike is the closest thing to a bash the game has: same reach, same
                    // move-then-hit execution, and AIActionMoveAndAttack.GetAttackTarget already knows
                    // how to turn a BashAbility into a target.
                    ActionDef = Helper.CreateDefFromClone(
                        moveAndStrike,
                        "2f6a3d95-4c17-4b82-9e05-a7d81c460b37",
                        "TFTV_MoveAndBash_AIActionDef");

                    ActionDef.Weight = moveAndShoot.Weight * WeightFractionOfShooting;

                    // The stock melee chain runs the equipment generator first and the strike zone
                    // second, and AIActorRangeZoneTargetGenerator throws away any equipment whose
                    // WeaponDef carries no BashAbilityDef - that is every gun, so chained like that the
                    // action would never produce a single target. Bash doesn't need the AI target's
                    // equipment anyway (it hits with whatever is selected), so the action is rebuilt
                    // around the strike zone on its own, where the generator takes its prevGen == null
                    // path and just offers up the positions next to an enemy.
                    AITargetEvaluation sourceEvaluation = moveAndStrike.Evaluations.FirstOrDefault(
                        e => e.Considerations.Any(c => c.Consideration is AIAttackPositionConsiderationDef));

                    if (sourceEvaluation == null)
                    {
                        TFTVLogger.Always("TFTV AI WARNING: MoveAndStrike_AIActionDef has no attack position evaluation; bash AI not installed");
                        ActionDef = null;
                        return;
                    }

                    AIAdjustedConsideration sourceConsideration = sourceEvaluation.Considerations.First(
                        c => c.Consideration is AIAttackPositionConsiderationDef);

                    ConsiderationDef = Helper.CreateDefFromClone(
                        (AIAttackPositionConsiderationDef)sourceConsideration.Consideration,
                        "8d0c7e42-1b93-4a65-bf28-63e5a09d7c14",
                        "TFTV_BashAttackPosition_AIConsiderationDef");

                    ActionDef.Evaluations = new AITargetEvaluation[]
                    {
                        new AITargetEvaluation
                        {
                            TargetGeneratorDef = DefCache.GetDef<AITargetGeneratorDef>("StrikeAbilityZone_AITargetGeneratorDef"),
                            FallbackTargetGeneratorDef = null,
                            Considerations = new AIAdjustedConsideration[]
                            {
                                new AIAdjustedConsideration
                                {
                                    ScoreCurve = sourceConsideration.ScoreCurve,
                                    Consideration = ConsiderationDef
                                }
                            },
                            TopScoresToConsiderPerc = sourceEvaluation.TopScoresToConsiderPerc,
                            MinNumberOfTargetsPerc = sourceEvaluation.MinNumberOfTargetsPerc,
                            MaxNumberOfTargetsPerc = sourceEvaluation.MaxNumberOfTargetsPerc,
                            MinNumberOfTargets = sourceEvaluation.MinNumberOfTargets
                        }
                    };

                    // Without an equipment generator the melee-equipment early exits inherited from
                    // MoveAndStrike would veto the action outright.
                    RemoveConsiderations(ActionDef, typeof(AICanUseEquipmentConsiderationDef), typeof(AIHasEquipmentConsiderationDef));

                    AddActionToTemplate(SoldierAITemplate, ActionDef);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// The player's own bash: whatever is in hand, or bare hands when there is nothing.
            /// Picks up replacements too - BetterClasses' Takedown swaps the def but not the kind.
            /// </summary>
            internal static BashAbility GetBashAbility(TacticalActor actor)
            {
                return actor?.GetAbilities<BashAbility>().FirstOrDefault(
                    b => b.BashAbilityDef != null
                    && b.BashAbilityDef.BashWith == BashAbilityDef.BashingWith.SelectedEquipmentOrBareHands);
            }

            /// <summary>
            /// Scores the bash the way AIAttackPositionConsideration scores a melee weapon, but against
            /// the actor's bash ability rather than against the weapon in the AI target.
            /// </summary>
            internal static float Score(TacticalActor actor, TacAITarget aiTarget, AIAttackPositionConsiderationDef considerationDef)
            {
                try
                {
                    TacticalActorBase targetActor = aiTarget.Actor;

                    if (targetActor == null || !targetActor.IsAlive || actor.RelationTo(targetActor) != FactionRelation.Enemy)
                    {
                        return 0f;
                    }

                    BashAbility bashAbility = GetBashAbility(actor);

                    if (bashAbility == null
                        || !bashAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                    {
                        return 0f;
                    }

                    if (aiTarget.PathLength > actor.GetMaxMoveAndActRange(bashAbility, aiTarget.MoveAbility))
                    {
                        return 0f;
                    }

                    if (!bashAbility.GetTargetsAt(aiTarget.Pos).Any(t => t.Actor == targetActor))
                    {
                        return 0f;
                    }

                    float damage = EstimateDamage(bashAbility, targetActor);

                    if (damage < MinUsefulDamage)
                    {
                        return 0f;
                    }

                    float damageFactor = Mathf.Clamp01(damage / considerationDef.MaxDamage);

                    return Mathf.Clamp01(damageFactor * AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, targetActor));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            /// <summary>
            /// What the bash would actually do to this target. Unlike GetPayloadAiDamageScore, which
            /// adds up every keyword on the payload, CalculateDamageResultForAiTarget only counts the
            /// keywords marked for the AI vulnerability check and takes the target's best armour (less
            /// the payload's piercing) off the top - so a bash that armour swallows whole scores zero.
            /// </summary>
            private static float EstimateDamage(BashAbility bashAbility, TacticalActorBase targetActor)
            {
                DamageResult damageResult = default(DamageResult);

                bashAbility.GetDamagePayload().CalculateDamageResultForAiTarget(targetActor, targetActor, ref damageResult);

                return damageResult.HealthDamage + damageResult.ArmorDamage + StunScoreWeight * damageResult.StunValue;
            }

            /// <summary>
            /// AIActionMoveAndAttack derives the attack ability from the AI target's weapon, which for a
            /// gun is always the shoot ability. For our action it is the actor's bash instead.
            /// AIActionMoveAndAttack.GetAttackTarget already knows what to do with a BashAbility
            /// (see TFTVArtOfCrab.SingleAPWeaponsMultipleShots).
            /// </summary>
            [HarmonyPatch(typeof(AIActionMoveAndAttack), "GetAttackAbility")]
            internal static class TFTV_AIActionMoveAndAttack_GetAttackAbility_patch
            {
                public static bool Prefix(AIActionMoveAndAttack __instance, TacticalActorBase actor, TacAITarget aiTarget, ref TacticalAbility __result)
                {
                    try
                    {
                        if (ActionDef == null || __instance.BaseDef != ActionDef)
                        {
                            return true;
                        }

                        BashAbility bashAbility = GetBashAbility(actor as TacticalActor);
                        __result = bashAbility;

                        if (bashAbility != null && aiTarget?.Actor != null)
                        {
                            TFTVLogger.Always($"TFTV AI: {actor.DisplayName} is bashing {aiTarget.Actor.DisplayName}, " +
                                $"estimated {EstimateDamage(bashAbility, aiTarget.Actor):f0} damage past armour");
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        #endregion

        #region 3. don't shoot what we are about to mind control

        /// <summary>
        /// A mind controller acts somewhere in the middle of the faction's turn; everything that acts
        /// before it happily empties a magazine into the soldier it was going to take over. At the top
        /// of the turn we work out which enemy each controller is going to go for and take that enemy
        /// off the table for the rest of the faction until the controller has had its go.
        /// </summary>
        internal static class MindControlReservations
        {
            private class Reservation
            {
                internal TacticalActor Controller;
                internal TacticalActorBase Target;
            }

            private static readonly List<Reservation> _reservations = new List<Reservation>();

            internal static void Clear()
            {
                _reservations.Clear();
            }

            /// <summary>
            /// Called once per AI faction turn, from TFTVArtOfCrab.TurnOrder.SortOutAITurnOrder, with the
            /// faction's actors already in the order they will act in.
            /// </summary>
            internal static void PredictMindControlTargets(List<TacticalActor> sortedAIActors)
            {
                try
                {
                    _reservations.Clear();

                    if (sortedAIActors == null || sortedAIActors.Count == 0)
                    {
                        return;
                    }

                    AIBlackboard blackboard = sortedAIActors[0].TacticalFaction.AIBlackboard;
                    HashSet<TacticalActorBase> alreadyReserved = new HashSet<TacticalActorBase>();

                    foreach (TacticalActor controller in sortedAIActors)
                    {
                        MindControlAbility mindControl = controller.GetAbility<MindControlAbility>();

                        if (mindControl == null
                            || !mindControl.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                        {
                            continue;
                        }

                        TacticalActorBase predictedTarget = PredictTarget(controller, mindControl, blackboard, alreadyReserved);

                        if (predictedTarget == null)
                        {
                            continue;
                        }

                        alreadyReserved.Add(predictedTarget);
                        _reservations.Add(new Reservation { Controller = controller, Target = predictedTarget });

                        TFTVLogger.Always($"TFTV AI: {controller.DisplayName} is expected to mind control " +
                            $"{predictedTarget.DisplayName}; the rest of the faction will leave it alone until then");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>
            /// Mirrors the target the (TFTV-patched) AIMindControlPickAvailableTargetConsideration would
            /// settle on: highest enemy weight, scaled by the will point margin and by how battered the
            /// target's arms and legs are.
            /// </summary>
            private static TacticalActorBase PredictTarget(TacticalActor controller, MindControlAbility mindControl,
                AIBlackboard blackboard, HashSet<TacticalActorBase> alreadyReserved)
            {
                float reach = controller.GetMaxMoveAndActRange(mindControl);
                float bestScore = 0f;
                TacticalActorBase bestTarget = null;

                foreach (TacticalActorBase enemyBase in blackboard.GetEnemies(controller.AIActor.GetEnemyMask(ActorType.All)))
                {
                    if (!(enemyBase is TacticalActor enemy) || !enemy.IsAlive || enemy.IsDisabled || alreadyReserved.Contains(enemy))
                    {
                        continue;
                    }

                    if (!enemy.HasGameTags(mindControl.OriginTargetData.TargetTags))
                    {
                        continue;
                    }

                    if (!(enemy.Status.GetStatus<MindControlStatus>()?.MindControlStatusDef.CanBeOverridden ?? true))
                    {
                        continue;
                    }

                    if ((enemy.Pos - controller.Pos).magnitude > reach)
                    {
                        continue;
                    }

                    float willFactor = WillPointFactor(controller, enemy, mindControl);

                    if (willFactor <= 0f)
                    {
                        continue;
                    }

                    float score = AIUtil.GetEnemyWeight(blackboard, enemy)
                        * willFactor
                        * TFTVArtOfCrab.TargetCulling.MindControl.AIMindControlPickAvailableTargetConsideration_Evaluate_patch.CheckActorSuitability(enemy);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = enemy;
                    }
                }

                return bestTarget;
            }

            private static float WillPointFactor(TacticalActor controller, TacticalActor enemy, MindControlAbility mindControl)
            {
                int willpower = (int)(float)controller.CharacterStats.Willpower;

                if (willpower == 0)
                {
                    return mindControl.MindControlAbilityDef.CheckAgainstTargetWillPoints ? 0f : 1f;
                }

                return Mathf.Clamp(
                    ((float)controller.CharacterStats.WillPoints - (float)enemy.CharacterStats.WillPoints - enemy.TacticalActorDef.WillPointWorth) / willpower,
                    0f, 1f);
            }

            /// <summary>
            /// True while some other actor of <paramref name="actor"/>'s faction is still due to mind
            /// control <paramref name="target"/> this turn. The controller itself is not held back: if
            /// its mind control falls through, it should still be free to attack.
            /// </summary>
            internal static bool IsReservedForMindControl(TacticalActor actor, TacticalActorBase target)
            {
                if (_reservations.Count == 0 || actor == null || target == null)
                {
                    return false;
                }

                for (int i = _reservations.Count - 1; i >= 0; i--)
                {
                    Reservation reservation = _reservations[i];

                    if (!IsStillPending(reservation))
                    {
                        _reservations.RemoveAt(i);
                        continue;
                    }

                    if (reservation.Target == target
                        && reservation.Controller != actor
                        && reservation.Controller.TacticalFaction == actor.TacticalFaction)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// A reservation only lasts until the controller has taken its turn - or has been killed,
            /// disabled, or has spent its mind control on something else.
            /// </summary>
            private static bool IsStillPending(Reservation reservation)
            {
                TacticalActor controller = reservation.Controller;

                if (controller == null || !controller.IsAlive || controller.IsDisabled || !controller.IsActive)
                {
                    return false;
                }

                if (reservation.Target == null || !reservation.Target.IsAlive)
                {
                    return false;
                }

                MindControlAbility mindControl = controller.GetAbility<MindControlAbility>();

                if (mindControl == null || mindControl.UsesThisTurn > 0)
                {
                    return false;
                }

                EndTurnAbility endTurn = controller.GetAbility<EndTurnAbility>();

                return endTurn == null || endTurn.UsesThisTurn == 0;
            }
        }

        #endregion

        #region scoring patches

        /// <summary>
        /// One entry point for all three features: the reservation veto, and the two custom scores for
        /// the considerations cloned above. Everything else falls through to the stock evaluation.
        /// </summary>
        [HarmonyPatch(typeof(AIAttackPositionConsideration), nameof(AIAttackPositionConsideration.Evaluate))]
        internal static class TFTV_AIAttackPositionConsideration_Evaluate_patch
        {
            public static bool Prefix(AIAttackPositionConsideration __instance, IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    TacticalActor tacticalActor = actor as TacticalActor;
                    TacAITarget tacAITarget = target as TacAITarget;

                    if (tacticalActor == null || tacAITarget == null)
                    {
                        return true;
                    }

                    if (MindControlReservations.IsReservedForMindControl(tacticalActor, tacAITarget.Actor))
                    {
                        __result = 0f;
                        return false;
                    }

                    if (SpiderDroneLauncherAI.ConsiderationDef != null && __instance.BaseDef == SpiderDroneLauncherAI.ConsiderationDef)
                    {
                        __result = SpiderDroneLauncherAI.Score(tacticalActor, tacAITarget);
                        return false;
                    }

                    if (BashAI.ConsiderationDef != null && __instance.BaseDef == BashAI.ConsiderationDef)
                    {
                        __result = BashAI.Score(tacticalActor, tacAITarget, BashAI.ConsiderationDef);
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Paralysing and goo weapons score through their own consideration, so the reservation has to
        /// be honoured here too - paralysing the soldier we are about to take over is just as wasteful.
        /// </summary>
        [HarmonyPatch(typeof(AINonHealthDamageAttackPositionConsideration), nameof(AINonHealthDamageAttackPositionConsideration.Evaluate))]
        internal static class TFTV_AINonHealthDamageAttackPositionConsideration_Evaluate_patch
        {
            public static bool Prefix(IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (actor is TacticalActor tacticalActor && target is TacAITarget tacAITarget
                        && MindControlReservations.IsReservedForMindControl(tacticalActor, tacAITarget.Actor))
                    {
                        __result = 0f;
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        #endregion
    }
}
