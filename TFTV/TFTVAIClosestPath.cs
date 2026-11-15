using Base.AI;
using Base.AI.Defs;
using Base.Utils.Maths;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Cheaper "closest enemy by path" scoring (AIClosestEnemyConsideration with DistanceType.PathLength).
    ///
    /// Vanilla runs one A* from the scored tile to every known enemy. Dash scores every tile in its move zone this
    /// way (TFTV swapped its straight-line check for Worm_ClosestPathToEnemy), so a few hundred tiles against eight
    /// soldiers meant thousands of pathfinds per evaluation.
    ///
    /// A walk is never shorter than the straight line, so enemies are tried nearest first by straight line, and the
    /// search stops as soon as the next one's straight-line distance can't beat the best walk found (or is past
    /// MaxDistance, where the score is 0 anyway). The score is the same as vanilla's with one exception: vanilla
    /// counts an enemy it finds no path to as 8 m away, and a skipped far enemy can't get that phantom 8 m.
    ///
    /// Only defs that weigh enemies equally and don't set the enemy as the target take this route; the rest stay
    /// vanilla.
    ///
    /// It also gives melee units walking distance on the plain advances (see <see cref="PlainAdvances"/>).
    /// </summary>
    internal static class TFTVAIClosestPath
    {
        /// <summary>Path length vanilla's GetPathLength returns when it finds no path.</summary>
        private const float NoPathLength = 8f;

        /// <summary>
        /// Margin under the straight line: a path runs between tile centres, so its start and end can sit a little
        /// closer together than the scored position and the enemy.
        /// </summary>
        private const float Slack = 1f;

        private static readonly List<KeyValuePair<float, TacticalActorBase>> Enemies = new List<KeyValuePair<float, TacticalActorBase>>();
        private static readonly Comparison<KeyValuePair<float, TacticalActorBase>> ByBound = (a, b) => a.Key.CompareTo(b.Key);

        private static int _tiles;
        private static int _paths;
        private static int _vanillaPaths;
        private static int _underBound;

        /// <summary>
        /// Plain advances that measure the distance to the nearest enemy in a straight line. Within 7 m every spot
        /// scores the same, so a melee unit on an upper floor above the fight has nowhere better to go and never
        /// looks for the stairs (tester log 2026-11-14: an Arthron shuffled along a third floor for four turns).
        /// Their check becomes a walking-distance clone; units with a gun they can fire keep the straight line,
        /// since they only need to get within range, and it costs no pathfinding.
        /// </summary>
        private static readonly string[] PlainAdvances =
        {
            "Crabman_Advance_Normal_WithoutShield_AIActionDef",
            "Advance_Normal_AIActionDef",
            "Advance_Normal_WithoutShield_AIActionDef",
            "Advance_UnderCover_AIActionDef",
            "Fishman_Advance_Normal_AIActionDef",
            "Siren_Advance_AIActionDef"
        };

        /// <summary>Straight-line check => GUID of its walking clone.</summary>
        private static readonly Dictionary<string, string> WalkingCloneGuids = new Dictionary<string, string>
        {
            { "Crabman_ClosestLineToEnemy_AIConsiderationDef", "b0999fd7-5dbf-4fff-97ab-3b08348ad54d" },
            { "ClosestLineToEnemy_AIConsiderationDef", "da3defea-658f-42fa-a4c4-ba28bebd653e" },
            { "Fishman_ClosestLineToEnemy_AIConsiderationDef", "cc447475-2581-4709-a13c-77f569e99e01" },
            { "Siren_ClosestEnemy_AIConsiderationDef", "725cffd4-92c8-4e97-9abb-310c2bd9cea8" }
        };

        /// <summary>Walks are longer than straight lines: the far end of the score scale stretches by this much.</summary>
        private const float WalkingMaxDistanceFactor = 1.5f;

        /// <summary>Reach of the stock walking checks (ClosestPathToEnemy and friends).</summary>
        private const float WalkingDestinationRadius = 1.7f;

        /// <summary>Walking clone => the straight-line check it replaced, for units that aren't melee only.</summary>
        private static readonly Dictionary<AIClosestEnemyConsiderationDef, AIClosestEnemyConsiderationDef> LineFor =
            new Dictionary<AIClosestEnemyConsiderationDef, AIClosestEnemyConsiderationDef>();

        private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> MeleeOnlyByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

        /// <summary>Swaps the plain advances' straight-line check for a walking one. Called once, after the other AI defs are made.</summary>
        internal static void CreateDefs()
        {
            try
            {
                Dictionary<AIClosestEnemyConsiderationDef, AIClosestEnemyConsiderationDef> clones =
                    new Dictionary<AIClosestEnemyConsiderationDef, AIClosestEnemyConsiderationDef>();
                List<string> swapped = new List<string>();

                DefCache defCache = TFTVMain.Main.DefCache;

                foreach (string name in PlainAdvances)
                {
                    AIActionDef action = defCache.GetDef<AIActionDef>(name);

                    if (action?.Evaluations == null)
                    {
                        TFTVLogger.Always($"TFTV AI walking advance: {name} not found");
                        continue;
                    }

                    foreach (AITargetEvaluation evaluation in action.Evaluations)
                    {
                        if (evaluation?.Considerations == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < evaluation.Considerations.Length; i++)
                        {
                            AIAdjustedConsideration adjusted = evaluation.Considerations[i];

                            if (!(adjusted?.Consideration is AIClosestEnemyConsiderationDef line) || line.DistanceType != DistanceType.Line
                                || !WalkingCloneGuids.TryGetValue(line.name, out string guid))
                            {
                                continue;
                            }

                            if (!clones.TryGetValue(line, out AIClosestEnemyConsiderationDef walking))
                            {
                                walking = Helper.CreateDefFromClone(line, guid, "TFTV_Walking_" + line.name);
                                walking.DistanceType = DistanceType.PathLength;
                                walking.DestinationRadius = WalkingDestinationRadius;
                                walking.MaxDistance = line.MaxDistance * WalkingMaxDistanceFactor;
                                clones[line] = walking;
                                LineFor[walking] = line;
                            }

                            // A new array and a new entry: other actions cloned from this one may share either.
                            AIAdjustedConsideration[] considerations = evaluation.Considerations.ToArray();
                            considerations[i] = new AIAdjustedConsideration { Consideration = walking, ScoreCurve = adjusted.ScoreCurve };
                            evaluation.Considerations = considerations;
                            swapped.Add(name);
                        }
                    }
                }

                TFTVLogger.Always($"TFTV AI walking advance: melee units measure by walking distance on {string.Join(", ", swapped.Distinct())}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>No gun it can fire, and a melee weapon. Cached per frame.</summary>
        private static bool IsMeleeOnly(TacticalActor actor)
        {
            int frame = Time.frameCount;

            if (MeleeOnlyByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == frame)
            {
                return cached.Value;
            }

            bool meleeOnly = actor.Equipments != null
                && !actor.Equipments.GetWeapons().Any(TFTVAICombatAbilities.SuppressAI.CanFire)
                && actor.Equipments.GetWeapons().Any(TFTVAICombatAbilities.MeleeReach.IsMelee);

            MeleeOnlyByFrame[actor] = new KeyValuePair<int, bool>(frame, meleeOnly);
            return meleeOnly;
        }

        /// <summary>Vanilla's straight-line score, with the replaced check's numbers.</summary>
        private static float ScoreByLine(AIClosestEnemyConsiderationDef def, TacticalActor actor, TacAITarget target)
        {
            float best = 0f;
            bool found = false;

            foreach (TacticalActorBase enemy in actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(def.EnemyMask), true))
            {
                float distance = (target.Pos - enemy.Pos).magnitude;
                float score = 1f - (Mathf.Clamp(distance, def.MinDistance, def.MaxDistance) - def.MinDistance) / (def.MaxDistance - def.MinDistance);

                if (def.ConsiderEnemyWeight)
                {
                    score *= AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, enemy);
                }

                if (score > best)
                {
                    best = score;
                    found = true;
                }
            }

            if (!found)
            {
                return def.NoEnemiesValue;
            }

            best = Mathf.Clamp(best, 0f, 1f);
            return Utl.Equals(best, 0f, 0.01f) ? 0.1f : best;
        }

        /// <summary>Scores the consideration if it qualifies; false leaves it to vanilla.</summary>
        internal static bool TryScore(AIClosestEnemyConsideration consideration, IAIActor actor, IAITarget target, ref float result)
        {
            AIClosestEnemyConsiderationDef def = consideration?.BaseDef as AIClosestEnemyConsiderationDef;

            if (def != null && LineFor.TryGetValue(def, out AIClosestEnemyConsiderationDef lineDef)
                && actor is TacticalActor gunner && target is TacAITarget gunnerTarget
                && gunner.TacticalFaction?.AIBlackboard != null && gunner.AIActor != null && !IsMeleeOnly(gunner))
            {
                result = ScoreByLine(lineDef, gunner, gunnerTarget);
                return true;
            }

            if (def == null || def.DistanceType != DistanceType.PathLength || def.ConsiderEnemyWeight || def.SetEnemyAsTarget
                || def.MaxDistance <= def.MinDistance || !(actor is TacticalActor tacticalActor) || !(target is TacAITarget tacAITarget)
                || tacticalActor.TacticalFaction?.AIBlackboard == null || tacticalActor.AIActor == null)
            {
                return false;
            }

            Vector3 from = tacAITarget.Pos;
            float ownRadius = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, tacticalActor);

            Enemies.Clear();

            foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(def.EnemyMask), true))
            {
                float radius = ownRadius + TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy) + def.DestinationRadius;
                Enemies.Add(new KeyValuePair<float, TacticalActorBase>((from - enemy.Pos).magnitude - radius, enemy));
            }

            Enemies.Sort(ByBound);

            float best = 0f;
            float bestLength = float.MaxValue;
            bool found = false;

            foreach (KeyValuePair<float, TacticalActorBase> entry in Enemies)
            {
                float bound = entry.Key - Slack;

                // Nobody further on can score above 0 (past MaxDistance) or beat the best walk (a walk >= its bound).
                if (bound >= def.MaxDistance || (found && bound >= bestLength))
                {
                    break;
                }

                TacticalActorBase enemy = entry.Value;
                float radius = ownRadius + TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy) + def.DestinationRadius;
                float length = AIUtil.GetPathLength(tacticalActor, from, enemy.Pos, true, radius);
                _paths++;

                if (length < bound && length != NoPathLength)
                {
                    _underBound++;
                }

                float score = 1f - (Mathf.Clamp(length, def.MinDistance, def.MaxDistance) - def.MinDistance) / (def.MaxDistance - def.MinDistance);

                if (score > best)
                {
                    best = score;
                    bestLength = length;
                    found = true;

                    // Already at the top score: nothing can beat it.
                    if (length <= def.MinDistance)
                    {
                        break;
                    }
                }
            }

            _tiles++;
            _vanillaPaths += Enemies.Count;
            Enemies.Clear();

            if (!found)
            {
                result = def.NoEnemiesValue;
                return true;
            }

            best = Mathf.Clamp(best, 0f, 1f);
            result = Utl.Equals(best, 0f, 0.01f) ? 0.1f : best;
            return true;
        }

        /// <summary>Logs what the last turn saved and starts counting afresh. Called at the start of every faction's turn.</summary>
        internal static void ClearTurn()
        {
            if (_tiles > 0)
            {
                TFTVLogger.Always($"TFTV AI closest path: {_tiles} positions scored with {_paths} pathfinds instead of {_vanillaPaths}" +
                    $"{(_underBound > 0 ? $"; {_underBound} walks came out shorter than their straight-line bound" : "")}");
            }

            _tiles = 0;
            _paths = 0;
            _vanillaPaths = 0;
            _underBound = 0;
            MeleeOnlyByFrame.Clear();
        }
    }
}
