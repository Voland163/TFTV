using Base.AI.Defs;
using Base.Defs;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        #region 6. mist

        /// <summary>
        /// Pandorans like the Mist. Every move they make prefers a spot in it a little, and while Void Omen 21 heals
        /// them there, those that are hurt, have a disabled body part or can't use their weapons go into it on
        /// purpose (Go To Mist) and stay, instead of fleeing the map.
        /// </summary>
        internal static class MistSeeking
        {
            private const float GoToMistWeight = 600f;

            /// <summary>Outside the Mist a Pandoran's moves count this much; under VO21, less the more it needs healing.</summary>
            private const float OutsideMist = 0.85f;
            private const float OutsideMistPerNeed = 0.6f;
            private const float OutsideMistFloor = 0.2f;

            /// <summary>A step toward Mist out of reach this turn scores at most this share of reaching it.</summary>
            private const float ApproachShare = 0.4f;

            /// <summary>How the need to heal is counted for a disabled body part, and for weapons it can't use.</summary>
            private const float DisabledPartNeed = 0.6f;
            private const float DisarmedNeed = 1f;

            /// <summary>Mist further than this (in voxels, around the actor) is not looked for.</summary>
            private const int SearchRadius = 30;
            private const int SearchHeight = 3;

            internal static AIActionMoveToPositionDef GoToMistActionDef;

            private static AIHasEquipmentConsiderationDef _shouldGoToMistDef;
            private static AIHasEquipmentConsiderationDef _notHealingInMistDef;
            private static AIRoofAboveConsiderationDef _goToMistPositionDef;
            private static AIRoofAboveConsiderationDef _mistPreferenceDef;

            /// <summary>
            /// The Triton's defensive Mist (stock: spawned whenever its spot is unsafe and an enemy sees it) is skipped when
            /// the Triton already stands in Mist - a second spawn on the same tile changes nothing and cost a sniper its shot -
            /// and when the Mist would leave too little AP for a shot it has from where it stands. Otherwise Mist goes first:
            /// soldiers lose 5 m of sight per Mist blob, and return fire needs a visible line at half Perception
            /// (RFaLaCarte), so a Triton inside its Mist then shoots without drawing return fire.
            /// </summary>
            private static AIHasEquipmentConsiderationDef _tritonMistDef;
            private const string TritonMistAbility = "Fishman_SpawnMist_AbilityDef";
            private static readonly HashSet<string> TritonMistLogged = new HashSet<string>();

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, Vector3?>> NearestMistByFrame = new Dictionary<TacticalActor, KeyValuePair<int, Vector3?>>();

            private static bool HealingMist => TFTVVoidOmens.VoidOmensCheck != null && TFTVVoidOmens.VoidOmensCheck.Length > 21 && TFTVVoidOmens.VoidOmensCheck[21];

            internal static void CreateDefs()
            {
                try
                {
                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIRoofAboveConsiderationDef roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");

                    _shouldGoToMistDef = Helper.CreateDefFromClone(hasGun, "3a8f5d21-c46e-4b07-9e3d-71b0a2f8c954", "TFTV_ShouldGoToMist_AIConsiderationDef");
                    _notHealingInMistDef = Helper.CreateDefFromClone(hasGun, "b7d24e90-5f13-4a6c-8b81-0c9e3f75d2a8", "TFTV_NotHealingInMist_AIConsiderationDef");
                    _goToMistPositionDef = Helper.CreateDefFromClone(roof, "e1c6a83f-2d79-4f45-a0b2-9d48f6e17c03", "TFTV_GoToMistPosition_AIConsiderationDef");
                    _mistPreferenceDef = Helper.CreateDefFromClone(roof, "5d90b3e7-84a1-4c2f-b6d5-2e7f0a91c864", "TFTV_MistPreference_AIConsiderationDef");
                    _tritonMistDef = Helper.CreateDefFromClone(hasGun, "c4e81b2a-6f93-4d07-a5b8-1e2d9f70c3a6", "TFTV_TritonMistFirst_AIConsiderationDef");

                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("Fishman_MoveAndSpawnMistDefensive_AIActionDef"), _tritonMistDef);

                    CreateGoToMist();

                    // Under VO21 a Pandoran with no usable weapon goes to the Mist to mend instead of off the map.
                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveAndEscapeDef>("Flee_AIActionDef"), _notHealingInMistDef);

                    List<AIActionsTemplateDef> movers = TFTVMain.Repo.GetAllDefs<AIActionsTemplateDef>()
                        .Where(t => t.ActionDefs != null && t.ActionDefs.Any(a => a is AIActionMoveToPositionDef))
                        .ToList();

                    HashSet<AIActionMoveToPositionDef> moves = new HashSet<AIActionMoveToPositionDef>();

                    foreach (AIActionsTemplateDef template in movers)
                    {
                        moves.UnionWith(template.ActionDefs.OfType<AIActionMoveToPositionDef>());

                        if (!template.ActionDefs.Contains(GoToMistActionDef))
                        {
                            template.ActionDefs = template.ActionDefs.Append(GoToMistActionDef).ToArray();
                        }
                    }

                    moves.Remove(GoToMistActionDef);

                    int preferring = 0;

                    foreach (AIActionMoveToPositionDef move in moves)
                    {
                        foreach (AITargetEvaluation evaluation in move.Evaluations.Where(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef))
                        {
                            if (evaluation.Considerations.Any(c => c.Consideration == _mistPreferenceDef))
                            {
                                continue;
                            }

                            evaluation.Considerations = evaluation.Considerations.Append(new AIAdjustedConsideration
                            {
                                Consideration = _mistPreferenceDef,
                                ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                            }).ToArray();

                            preferring++;
                        }
                    }

                    TFTVLogger.Always($"TFTV AI: Go To Mist added to {movers.Count} templates; Mist preference added to {preferring} move evaluations");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateGoToMist()
            {
                GoToMistActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"),
                    "8c47e2b9-1d60-4f3a-a5e8-b93c06d71f42",
                    "TFTV_GoToMist_AIActionDef");

                GoToMistActionDef.Weight = GoToMistWeight;

                // With or without an enemy in view.
                RemoveConsiderations(GoToMistActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(GoToMistActionDef, _shouldGoToMistDef);

                AITargetEvaluation positions = GoToMistActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "f29a0c64-7b35-4e81-9d1c-48e5b7a03f6d",
                    "TFTV_GoToMistZone_AITargetGeneratorDef");

                zone.ZoneRange = 1f;
                zone.IncludeSelfActorPosition = false;
                positions.TargetGeneratorDef = zone;

                positions.Considerations = new AIAdjustedConsideration[]
                {
                    new AIAdjustedConsideration { Consideration = _goToMistPositionDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                    new AIAdjustedConsideration
                    {
                        Consideration = DefCache.GetDef<AILineOfSightToEnemiesConsiderationDef>("NoLineofSight_AIConsiderationDef"),
                        ScoreCurve = AnimationCurve.Linear(0f, 0.6f, 1f, 1f)
                    },
                    positions.Considerations.First(c => c.Consideration is AISpreadConsiderationDef)
                };
            }

            private static bool IsPandoran(TacticalActor actor)
            {
                return actor?.TacticalFaction?.Faction?.FactionDef?.MatchesShortName("aln") == true;
            }

            private static bool IsTouchingMist(TacticalActor actor)
            {
                return actor.TacticalPerceptionBase != null && actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist);
            }

            private static bool IsMist(TacticalVoxelMatrix matrix, Vector3 pos)
            {
                return matrix != null
                    && ((matrix.IsPositionInBounds(pos) && matrix.GetVoxel(pos)?.GetVoxelType() == TacticalVoxelType.Mist)
                        || (matrix.IsPositionInBounds(pos + Vector3.up) && matrix.GetVoxel(pos + Vector3.up)?.GetVoxelType() == TacticalVoxelType.Mist));
            }

            /// <summary>0 to 1: missing HP, a disabled body part, or no usable weapon, whichever weighs most.</summary>
            private static float Need(TacticalActor actor)
            {
                float health = actor.CharacterStats.Health;
                float max = actor.CharacterStats.Health.Max;
                float need = max > 0f ? Mathf.Clamp01(1f - health / max) : 0f;

                ItemSlot disabled = actor.BodyState?.GetHealthSlots()?.FirstOrDefault(IsDisabledPart);

                if (disabled != null)
                {
                    need = Mathf.Max(need, DisabledPartNeed);

                    if (DisabledPartLogged.Add($"{actor.name}|{disabled.ItemSlotDef?.name}"))
                    {
                        TFTVLogger.Always($"TFTV AI mist: {actor.DisplayName} counts {disabled.ItemSlotDef?.name} " +
                            $"({string.Join(", ", disabled.GetAllDirectItems(true).Select(i => i.ItemDef?.name))}) as a disabled part to mend");
                    }
                }

                if (actor.Equipments != null && !actor.Equipments.GetWeapons().Any(w => w.IsUsable))
                {
                    need = Mathf.Max(need, DisarmedNeed);
                }

                return need;
            }

            /// <summary>
            /// A slot with a body part in it whose HP is gone. Enabled is only "HP above zero", so a slot with nothing in it
            /// (or no HP at all) reads as disabled too, which sent unhurt Arthrons to the Mist.
            /// </summary>
            private static bool IsDisabledPart(ItemSlot slot)
            {
                return slot != null && !slot.Enabled && slot.GetHealth() != null && slot.GetHealth().Max > 0f
                    && slot.GetAllDirectItems(true).Any();
            }

            private static readonly HashSet<string> DisabledPartLogged = new HashSet<string>();

            /// <summary>How deep in the Mist a spot is, 0 to 1: the share of the spots 1 and 2 tiles around it (8 directions) that are Mist too.</summary>
            private static float MistDepth(TacticalVoxelMatrix matrix, Vector3 pos)
            {
                int frame = Time.frameCount;

                if (DepthFrame != frame)
                {
                    DepthFrame = frame;
                    DepthByTile.Clear();
                }

                Vector3Int key = Vector3Int.RoundToInt(pos * 2f);

                if (DepthByTile.TryGetValue(key, out float cached))
                {
                    return cached;
                }

                int mist = 0;

                foreach (Vector3 direction in DepthDirections)
                {
                    for (int step = 1; step <= DepthRings; step++)
                    {
                        if (IsMist(matrix, pos + direction * step))
                        {
                            mist++;
                        }
                    }
                }

                float depth = mist / (float)(DepthDirections.Length * DepthRings);
                DepthByTile[key] = depth;
                return depth;
            }

            private const int DepthRings = 2;
            private static readonly Vector3[] DepthDirections =
            {
                new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f),
                new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(-1f, 0f, -1f)
            };
            private static readonly Dictionary<Vector3Int, float> DepthByTile = new Dictionary<Vector3Int, float>();
            private static int DepthFrame = -1;

            /// <summary>In the Mist a move counts InMistEdge at its edge, up to 1 deep inside it; outside it counts OutsideMist or less.</summary>
            private const float InMistEdge = 0.9f;

            /// <summary>Go To Mist: a spot at the Mist's edge scores this share of one deep inside it.</summary>
            private const float GoToMistEdge = 0.7f;

            /// <summary>The nearest Mist around the actor, worked out once a frame; null if there is none in SearchRadius.</summary>
            private static Vector3? NearestMist(TacticalActor actor)
            {
                if (NearestMistByFrame.TryGetValue(actor, out KeyValuePair<int, Vector3?> cached) && cached.Key == Time.frameCount)
                {
                    return cached.Value;
                }

                Vector3? nearest = null;
                TacticalVoxelMatrix matrix = actor.TacticalLevel?.VoxelMatrix;

                // GetVoxels needs a voxel at the origin, and its circular search reads the position of voxels past
                // the map's edge without checking for null, so the square search is used: it skips them.
                if (matrix != null && matrix.IsPositionInBounds(actor.Pos) && matrix.GetVoxel(actor.Pos) != null)
                {
                    float best = float.MaxValue;

                    foreach (TacticalVoxel voxel in matrix.GetVoxels(actor.Pos, SearchRadius, SearchHeight, false))
                    {
                        if (voxel == null || voxel.GetVoxelType() != TacticalVoxelType.Mist)
                        {
                            continue;
                        }

                        Vector3 offset = voxel.Position - actor.Pos;
                        offset.y = 0f;
                        float distance = offset.sqrMagnitude;

                        if (distance < best)
                        {
                            best = distance;
                            nearest = voxel.Position;
                        }
                    }
                }

                NearestMistByFrame[actor] = new KeyValuePair<int, Vector3?>(Time.frameCount, nearest);
                return nearest;
            }

            /// <summary>The early exits are clones of HasGun; returns false for any other def.</summary>
            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (actor == null || def == null || GoToMistActionDef == null)
                {
                    return false;
                }

                if (def != _shouldGoToMistDef && def != _notHealingInMistDef && def != _tritonMistDef)
                {
                    return false;
                }

                try
                {
                    if (def == _tritonMistDef)
                    {
                        score = ShouldSpawnDefensiveMist(actor) ? 1f : 0f;
                    }
                    else if (def == _shouldGoToMistDef)
                    {
                        score = HealingMist && IsPandoran(actor) && actor.IsAlive && !IsTouchingMist(actor)
                            && Need(actor) > 0f && NearestMist(actor) != null ? 1f : 0f;
                    }
                    else
                    {
                        bool goesToMist = HealingMist && IsPandoran(actor) && NearestMist(actor) != null;
                        score = goesToMist ? 0f : 1f;
                    }
                }
                catch (Exception e)
                {
                    // Fail toward the stock behaviour: no Go To Mist, Flee as before.
                    LogOnce(e);
                    score = def == _shouldGoToMistDef ? 0f : 1f;
                }

                return true;
            }

            private static bool ShouldSpawnDefensiveMist(TacticalActor actor)
            {
                if (IsTouchingMist(actor))
                {
                    LogTritonMist(actor, "already stands in Mist: no second spawn");
                    return false;
                }

                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (weapon == null || !PeekAndFallBackAI.IsGun(weapon))
                {
                    return true;
                }

                TacticalAbility mist = actor.GetAbilities<TacticalAbility>().FirstOrDefault(a => a.AbilityDef?.name == TritonMistAbility);

                if (mist == null || !mist.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter))
                {
                    LogTritonMist(actor, $"cannot spawn Mist now ({(mist == null ? "no ability" : mist.GetDisabledState(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter).ToString())})");
                    return false;
                }

                float mistCost = mist?.ActionPointCost ?? 0f;
                float shotCost = actor.Equipments.GetAPToUseEquipment(weapon);
                float ap = actor.CharacterStats.ActionPoints;

                if (ap - mistCost >= shotCost)
                {
                    LogTritonMist(actor, $"may spawn Mist before its shot ({ap:0.#} AP: Mist {mistCost:0.#}, shot {shotCost:0.#}), then shoots from inside it");
                    return true;
                }

                TacticalActorBase target = Stances.ShotTargetFromWhereItStands(actor, weapon);

                if (target == null)
                {
                    return true;
                }

                LogTritonMist(actor, $"shoots {target.DisplayName} instead of spawning Mist ({ap:0.#} AP: Mist {mistCost:0.#} would leave too little for the {shotCost:0.#} AP shot)");
                return false;
            }

            private static void LogTritonMist(TacticalActor actor, string decision)
            {
                if (TritonMistLogged.Add($"{actor.name}|{decision}"))
                {
                    TFTVLogger.Always($"TFTV AI mist: {actor.DisplayName} {decision}");
                }
            }

            /// <summary>The position scores are clones of RoofAbove; returns false for any other def.</summary>
            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (actor == null || target == null || def == null || GoToMistActionDef == null)
                {
                    return false;
                }

                if (def != _mistPreferenceDef && def != _goToMistPositionDef)
                {
                    return false;
                }

                // An error here would stop the AI's turn (the scoring patch rethrows), so it counts as no preference.
                try
                {
                    score = def == _mistPreferenceDef ? MistPreference(actor, target.Pos) : GoToMistScore(actor, target);
                }
                catch (Exception e)
                {
                    LogOnce(e);
                    score = def == _mistPreferenceDef ? 1f : 0f;
                }

                return true;
            }

            private static bool _errorLogged;

            private static void LogOnce(Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    TFTVLogger.Error(e);
                }
            }

            private static float MistPreference(TacticalActor actor, Vector3 pos)
            {
                if (!IsPandoran(actor) || NearestMist(actor) == null)
                {
                    return 1f;
                }

                if (IsMist(actor.TacticalLevel.VoxelMatrix, pos))
                {
                    return InMistEdge + (1f - InMistEdge) * MistDepth(actor.TacticalLevel.VoxelMatrix, pos);
                }

                return HealingMist ? Mathf.Max(OutsideMistFloor, OutsideMist - OutsideMistPerNeed * Need(actor)) : OutsideMist;
            }

            /// <summary>Need times: 1 in the Mist (less the further the walk), else how much nearer the Mist it gets.</summary>
            private static float GoToMistScore(TacticalActor actor, TacAITarget target)
            {
                float need = Need(actor);
                Vector3? mist = NearestMist(actor);

                if (need <= 0f || mist == null)
                {
                    return 0f;
                }

                // A gunner with a shot from where it stands does not walk away from it: only Mist it can reach with AP
                // left for the shot counts, so it steps into nearby Mist and shoots from there, or shoots where it is.
                if (KeepsShotAP(actor, target, out float shotCost) == false)
                {
                    LogShotKept(actor, shotCost);
                    return 0f;
                }

                if (IsMist(actor.TacticalLevel.VoxelMatrix, target.Pos))
                {
                    float ap = Mathf.Max(1f, actor.CharacterStats.ActionPoints.Max);
                    float depth = GoToMistEdge + (1f - GoToMistEdge) * MistDepth(actor.TacticalLevel.VoxelMatrix, target.Pos);
                    return need * depth * (1f - 0.3f * Mathf.Clamp01(target.PathLength / ap));
                }

                Vector2 goal = new Vector2(mist.Value.x, mist.Value.z);
                float now = Vector2.Distance(new Vector2(actor.Pos.x, actor.Pos.z), goal);
                float then = Vector2.Distance(new Vector2(target.Pos.x, target.Pos.z), goal);

                if (now <= 0.5f || then >= now)
                {
                    return 0f;
                }

                return need * ApproachShare * Mathf.Clamp01((now - then) / now);
            }

            /// <summary>Null when the actor has no shot from here; else whether the walk to the target leaves AP for it.</summary>
            private static bool? KeepsShotAP(TacticalActor actor, TacAITarget target, out float shotCost)
            {
                shotCost = 0f;
                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (weapon == null || !weapon.IsUsable || !PeekAndFallBackAI.IsGun(weapon) || !Stances.HasShotFromHere(actor, weapon))
                {
                    return null;
                }

                shotCost = actor.Equipments.GetAPToUseEquipment(weapon);
                return actor.CharacterStats.ActionPoints - target.PathLength >= shotCost;
            }

            private static void LogShotKept(TacticalActor actor, float shotCost)
            {
                if (TritonMistLogged.Add($"{actor.name}|keeps shot"))
                {
                    TFTVLogger.Always($"TFTV AI mist: {actor.DisplayName} (need {Need(actor):0.00}) has a shot from where it stands: " +
                        $"Go To Mist only within reach that leaves the {shotCost:0.#} AP shot ({(float)actor.CharacterStats.ActionPoints:0.#} AP)");
                }
            }

            /// <summary>Called from TFTVArtOfCrab's move prefix, for the log.</summary>
            internal static bool RecordMove(AIActionDef actionDef, TacticalActor actor, TacAITarget target)
            {
                if (GoToMistActionDef == null || actionDef != GoToMistActionDef)
                {
                    return false;
                }

                bool inMist = IsMist(actor.TacticalLevel.VoxelMatrix, target.Pos);
                TFTVLogger.Always($"TFTV AI mist: {actor.DisplayName} (need {Need(actor):0.00}, HP {(float)actor.CharacterStats.Health:0} of {actor.CharacterStats.Health.Max:0}) " +
                    $"moves {Vector3.Distance(actor.Pos, target.Pos):0.0}m " +
                    $"{(inMist ? $"into the Mist (depth {MistDepth(actor.TacticalLevel.VoxelMatrix, target.Pos):0.00})" : "toward the Mist")} at {target.Pos}");

                return true;
            }

            internal static void ClearTurn()
            {
                NearestMistByFrame.Clear();
                TritonMistLogged.Clear();
                DepthByTile.Clear();
            }
        }

        #endregion
    }
}
