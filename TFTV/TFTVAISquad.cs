using Base.AI;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static TFTV.TFTVSuppression;

namespace TFTV
{
    /// <summary>
    /// Squad planning: a look at the whole AI side before anyone moves, so actors act on what their team mates are
    /// about to do instead of each one deciding alone.
    ///
    /// Runs once per AI faction turn, from the GetSortedAIActors postfix (TFTVHarmonyTactical), after Art of Crab has
    /// set the turn order. Each pass reads a shared <see cref="SquadContext"/> and writes into one <see cref="SquadPlan"/>:
    /// - <see cref="SquadOrder"/>: one actor told to do one thing to one target, for the benefit of others. The order
    ///   is acted on through the actor's own AI actions (a pass that adds a new kind of order also adds the action, or
    ///   the scoring hook, that carries it out), announced to the player when the actor starts acting, and dropped as
    ///   soon as it no longer makes sense.
    /// - <see cref="TargetWeight"/>: multiplies every attack score of an actor (or the whole side) against a target,
    ///   through the attack-position considerations (0 is a veto in TFTVAICombatAbilities' prefixes, anything else
    ///   a postfix below). Weights only ever lower a score: the scores are clamped to 1, so a boost would vanish.
    /// - Turn order: "A acts before B" pairs, applied to the faction's actor list before the turn starts.
    ///
    /// Passes marked NeedsSmartActors only involve smart actors: human leaders (Tier 1, 15 tiles) and lieutenants
    /// (Tier 2, 10 tiles), Sirens (15) and Scyllas (25), every AI actor within one of those ranges, and Tritons, and
    /// are skipped above <see cref="MaxAIActors"/> actors, so big base defences and Pandoran waves keep their turn
    /// time. One tile is taken as 1 m (a metre of path costs about one AP).
    ///
    /// Passes, in order:
    /// 1. Mind control hold (every turn, whole side): nobody attacks the soldier a mind controller is about to take.
    /// 2. Suppression: pin down overwatch that covers a smart attacker's approach.
    /// 3. Focus fire: share out the smart attackers' damage between targets for the most expected kills and harm,
    ///    re-planned each time one of them starts acting (TFTVAISquadFocus.cs).
    /// </summary>
    internal static partial class TFTVAISquadPlanner
    {
        /// <summary>
        /// Above this many AI actors the stage doesn't run. Evaluation measured at 80-330 ms per actor decision with
        /// turn time dominated by animation; the planning itself logs its cost on every run so this can be tuned.
        /// </summary>
        internal const int MaxAIActors = 16;

        internal enum OrderKind
        {
            Suppress,

            /// <summary>Announcement only: the plan's focus weights do the work.</summary>
            FocusFire,
        }

        internal sealed class SquadOrder
        {
            internal OrderKind Kind;
            internal TacticalActor Executor;

            /// <summary>Who gave the order: the executor's leader, the executor itself (a leader acting on its own call), or null.</summary>
            internal TacticalActor Issuer;
            internal TacticalActorBase Target;
            internal readonly List<TacticalActor> Beneficiaries = new List<TacticalActor>();
            internal string Reason;
            internal bool Announced;
            internal bool Done;
        }

        internal sealed class TargetWeight
        {
            /// <summary>Null for every actor of the planning faction.</summary>
            internal TacticalActor Executor;

            /// <summary>An actor the weight doesn't apply to (the mind controller itself).</summary>
            internal TacticalActor Exempt;
            internal TacticalActorBase Target;
            internal float Multiplier;
            internal string Reason;

            /// <summary>Which pass wrote it, so a pass can replace its own weights.</summary>
            internal string Source;

            /// <summary>Checked on every use; the weight stops counting once it returns false. Null = whole turn.</summary>
            internal Func<bool> StillApplies;
        }

        internal sealed class SquadContext
        {
            internal TacticalFaction Faction;
            internal List<TacticalActor> Actors;

            /// <summary>Revealed enemy actors.</summary>
            internal List<TacticalActor> Enemies;

            /// <summary>
            /// Every smart actor, mapped to the leader whose influence it is under: itself for a leader, null for an
            /// actor that is smart on its own (a Triton out of every leader's range).
            /// </summary>
            internal Dictionary<TacticalActor, TacticalActor> Commander;

            internal bool IsSmart(TacticalActor actor)
            {
                return actor != null && Commander.ContainsKey(actor);
            }
        }

        internal sealed class SquadPlan
        {
            internal readonly TacticalFaction Faction;
            internal readonly List<SquadOrder> Orders = new List<SquadOrder>();
            internal readonly List<TargetWeight> Weights = new List<TargetWeight>();
            internal readonly List<KeyValuePair<TacticalActor, TacticalActor>> ActBefore = new List<KeyValuePair<TacticalActor, TacticalActor>>();
            internal SquadContext Context;

            internal SquadPlan(TacticalFaction faction)
            {
                Faction = faction;
            }

            /// <summary>Scales attacks on <paramref name="target"/> by <paramref name="multiplier"/> (0-1), for one actor or (null) the whole side.</summary>
            internal void AddWeight(TacticalActor executor, TacticalActorBase target, float multiplier, string reason,
                string source, TacticalActor exempt = null, Func<bool> stillApplies = null, bool log = true)
            {
                Weights.Add(new TargetWeight
                {
                    Executor = executor,
                    Exempt = exempt,
                    Target = target,
                    Multiplier = Mathf.Clamp01(multiplier),
                    Reason = reason,
                    Source = source,
                    StillApplies = stillApplies
                });

                if (log)
                {
                    TFTVLogger.Always($"[AI squad] weight x{multiplier:0.##} on {target?.DisplayName} for {executor?.DisplayName ?? "everyone"}" +
                        $"{(exempt != null ? " but " + exempt.DisplayName : "")}: {reason}");
                }
            }

            internal void RemoveWeights(string source)
            {
                Weights.RemoveAll(w => w.Source == source);
            }
        }

        internal interface ISquadPass
        {
            string Name { get; }
            bool IsEnabled { get; }

            /// <summary>Only runs with a smart actor on the side and at most MaxAIActors actors.</summary>
            bool NeedsSmartActors { get; }

            void Plan(SquadContext context, SquadPlan plan);
        }

        private static readonly ISquadPass[] Passes =
        {
            new MindControlHoldPass(),
            new SuppressionPass(),
            FocusPass,
        };

        private static SquadPlan _plan;

        /// <summary>The actor whose evaluations are running; a change means the next actor has started.</summary>
        private static TacticalActor _actingActor;

        #region leaders

        private static GameTagDef _sirenTag;
        private static GameTagDef _scyllaTag;
        private static GameTagDef _tritonTag;
        private static GameTagDef _humanTag;

        private static void ResolveTags()
        {
            if (_sirenTag == null)
            {
                DefCache defCache = TFTVMain.Main.DefCache;
                _sirenTag = defCache.GetDef<GameTagDef>("Siren_ClassTagDef");
                _scyllaTag = defCache.GetDef<GameTagDef>("Queen_ClassTagDef");
                _tritonTag = defCache.GetDef<GameTagDef>("Fishman_ClassTagDef");
                _humanTag = defCache.GetDef<GameTagDef>("Human_TagDef");
            }
        }

        /// <summary>Influence range in metres, 0 for an actor that is no leader.</summary>
        private static float LeaderRange(TacticalActor actor)
        {
            if (actor == null || !actor.IsAlive || actor.IsDisabled || actor.GameTags == null)
            {
                return 0f;
            }

            if (_scyllaTag != null && actor.GameTags.Contains(_scyllaTag))
            {
                return 25f;
            }

            if (TFTVHumanEnemies.HumanEnemyTier1GameTag != null && actor.GameTags.Contains(TFTVHumanEnemies.HumanEnemyTier1GameTag))
            {
                return 15f;
            }

            if (_sirenTag != null && actor.GameTags.Contains(_sirenTag))
            {
                return 15f;
            }

            if (TFTVHumanEnemies.HumanEnemyTier2GameTag != null && actor.GameTags.Contains(TFTVHumanEnemies.HumanEnemyTier2GameTag))
            {
                return 10f;
            }

            return 0f;
        }

        internal static bool IsHuman(TacticalActor actor)
        {
            ResolveTags();
            return actor?.GameTags != null && _humanTag != null && actor.GameTags.Contains(_humanTag);
        }

        private static Dictionary<TacticalActor, TacticalActor> FindSmartActors(List<TacticalActor> actors)
        {
            Dictionary<TacticalActor, TacticalActor> commander = new Dictionary<TacticalActor, TacticalActor>();
            List<KeyValuePair<TacticalActor, float>> leaders = new List<KeyValuePair<TacticalActor, float>>();

            foreach (TacticalActor actor in actors)
            {
                float range = LeaderRange(actor);

                if (range > 0f)
                {
                    leaders.Add(new KeyValuePair<TacticalActor, float>(actor, range));
                }
            }

            foreach (TacticalActor actor in actors)
            {
                if (actor == null || !actor.IsAlive)
                {
                    continue;
                }

                if (leaders.Any(l => l.Key == actor))
                {
                    commander[actor] = actor;
                    continue;
                }

                // The widest influence wins; between equals, the nearest leader.
                TacticalActor best = null;
                float bestRange = 0f;
                float bestDistance = float.MaxValue;

                foreach (KeyValuePair<TacticalActor, float> leader in leaders)
                {
                    float distance = Vector3.Distance(actor.Pos, leader.Key.Pos);

                    if (distance > leader.Value)
                    {
                        continue;
                    }

                    if (leader.Value > bestRange || (Mathf.Approximately(leader.Value, bestRange) && distance < bestDistance))
                    {
                        best = leader.Key;
                        bestRange = leader.Value;
                        bestDistance = distance;
                    }
                }

                if (best != null)
                {
                    commander[actor] = best;
                }
                else if (_tritonTag != null && actor.GameTags != null && actor.GameTags.Contains(_tritonTag))
                {
                    commander[actor] = null;
                }
            }

            return commander;
        }

        #endregion

        #region planning

        /// <summary>Called once per AI faction turn with the actors in the order they will act; may reorder the list.</summary>
        internal static void PlanTurn(TacticalFaction faction, List<TacticalActor> sortedActors)
        {
            Clear();

            try
            {
                if (faction == null || faction.IsControlledByPlayer || sortedActors == null || sortedActors.Count == 0
                    || !Passes.Any(p => p.IsEnabled))
                {
                    return;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                ResolveTags();

                SquadContext context = new SquadContext
                {
                    Faction = faction,
                    Actors = sortedActors,
                    Enemies = faction.Vision.GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false)
                        .OfType<TacticalActor>().Where(a => a.IsAlive).ToList(),
                    Commander = FindSmartActors(sortedActors)
                };

                bool overCap = sortedActors.Count > MaxAIActors;
                bool smartPasses = !overCap && context.Commander.Count > 0 && context.Enemies.Count > 0;

                if (overCap)
                {
                    TFTVLogger.Always($"[AI squad] {faction.Faction?.FactionDef?.name}: {sortedActors.Count} actors, over {MaxAIActors}; only the whole-side passes run");
                }

                SquadPlan plan = new SquadPlan(faction) { Context = context };

                // Live while the passes run, so a later pass sees an earlier one's orders and weights.
                _plan = plan;

                foreach (ISquadPass pass in Passes)
                {
                    if (!pass.IsEnabled || (pass.NeedsSmartActors && !smartPasses))
                    {
                        continue;
                    }

                    try
                    {
                        pass.Plan(context, plan);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Always($"[AI squad] pass {pass.Name} failed");
                        TFTVLogger.Error(e);
                    }
                }

                ApplyTurnOrder(sortedActors, plan);

                stopwatch.Stop();
                TFTVLogger.Always($"[AI squad] {faction.Faction?.FactionDef?.name}: {sortedActors.Count} actors, {context.Commander.Count} smart, " +
                    $"{context.Enemies.Count} enemies seen; {plan.Orders.Count} orders, {plan.Weights.Count} target weights; took {stopwatch.Elapsed.TotalMilliseconds:0.0} ms");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>Moves each "acts first" actor to just ahead of the actor it has to act before, if it isn't already.</summary>
        private static void ApplyTurnOrder(List<TacticalActor> actors, SquadPlan plan)
        {
            foreach (KeyValuePair<TacticalActor, TacticalActor> pair in plan.ActBefore)
            {
                int first = actors.IndexOf(pair.Key);
                int then = actors.IndexOf(pair.Value);

                if (first < 0 || then < 0 || first < then)
                {
                    continue;
                }

                actors.RemoveAt(first);
                actors.Insert(then, pair.Key);
                TFTVLogger.Always($"[AI squad] {pair.Key.DisplayName} now acts before {pair.Value.DisplayName}");
            }
        }

        internal static void Clear()
        {
            _plan = null;
            _actingActor = null;
            FocusPass.Clear();
        }

        #endregion

        #region queries from the AI

        /// <summary>The live suppress order <paramref name="actor"/> is carrying out, if any.</summary>
        internal static SquadOrder GetSuppressOrder(TacticalActor actor)
        {
            if (_plan == null || actor == null || actor.TacticalFaction != _plan.Faction)
            {
                return null;
            }

            foreach (SquadOrder order in _plan.Orders)
            {
                if (order.Kind == OrderKind.Suppress && order.Executor == actor && IsLive(order))
                {
                    return order;
                }
            }

            return null;
        }

        /// <summary>Product of the plan's weights for this actor against this target; 1 when there are none.</summary>
        internal static float TargetMultiplier(TacticalActor actor, TacticalActorBase target)
        {
            if (_plan == null || _plan.Weights.Count == 0 || actor == null || target == null || actor.TacticalFaction != _plan.Faction)
            {
                return 1f;
            }

            float multiplier = 1f;

            foreach (TargetWeight weight in _plan.Weights)
            {
                if (weight.Target == target && (weight.Executor == null || weight.Executor == actor) && weight.Exempt != actor
                    && (weight.StillApplies == null || weight.StillApplies()))
                {
                    multiplier *= weight.Multiplier;
                }
            }

            return multiplier;
        }

        private static bool IsLive(SquadOrder order)
        {
            if (order.Done)
            {
                return false;
            }

            string why = null;

            if (order.Executor == null || !order.Executor.IsAlive || order.Executor.IsDisabled)
            {
                why = "the executor can't act";
            }
            else if (order.Target == null || !order.Target.IsAlive)
            {
                why = "the target is down";
            }
            else if (order.Kind == OrderKind.Suppress)
            {
                TacticalActor target = order.Target as TacticalActor;

                if (target == null || SuppressionRuntime.BlocksReactionFire(target))
                {
                    why = "the target is pinned down";
                }
                else if (!target.Status.HasStatus<OverwatchStatus>())
                {
                    why = "the target is off overwatch";
                }
            }
            else if (order.Kind == OrderKind.FocusFire && !IsFocusAssignment(order.Executor, order.Target))
            {
                why = "the plan has moved on";
            }

            if (why == null)
            {
                return true;
            }

            order.Done = true;
            TFTVLogger.Always($"[AI squad] order done: {order.Executor?.DisplayName} {order.Kind} {order.Target?.DisplayName} ({why})");
            return false;
        }

        #endregion

        #region showing the player

        /// <summary>Line keys are TFTV_SQUAD_{kind}_{ORDER|REPLY|SELF}_{0-2}.</summary>
        private static string HumanKey(OrderKind kind, string part, int line)
        {
            return $"TFTV_SQUAD_{(kind == OrderKind.FocusFire ? "FOCUS" : "SUPPRESS")}_{part}_{line}";
        }

        private const int HumanLines = 3;
        private const string PandoranCallKey = "TFTV_SQUAD_PANDORAN_CALL";
        private const string PandoranReplyKey = "TFTV_SQUAD_PANDORAN_REPLY";

        private const float ReplyDelay = 1.5f;

        private static TacticalEventDef _callVoice;
        private static TacticalEventDef _replyVoice;

        /// <summary>Before each of an actor's AI evaluations: announce any order it is about to carry out.</summary>
        internal static void OnBeforeActorEvaluation(TacticalActor actor)
        {
            if (_plan == null || actor == null || actor.TacticalFaction != _plan.Faction)
            {
                return;
            }

            if (actor != _actingActor)
            {
                _actingActor = actor;
                FocusPass.Replan(_plan, actor);
            }

            foreach (SquadOrder order in _plan.Orders)
            {
                if (order.Kind != OrderKind.FocusFire && order.Executor == actor && !order.Announced && IsLive(order))
                {
                    order.Announced = true;
                    Announce(order);
                }
            }
        }

        /// <summary>
        /// An AI shot or strike is starting (ShootAbility.Activate). A focus target is called out by the first of its
        /// assigned attackers to actually attack it; that attacker's leader gives the call.
        /// </summary>
        internal static void OnAttack(TacticalActor attacker, TacticalAbilityTarget abilityTarget)
        {
            if (_plan == null || attacker == null || abilityTarget == null || attacker.TacticalFaction != _plan.Faction
                || !attacker.TacticalFaction.IsPlayingTurn)
            {
                return;
            }

            TacticalActorBase target = abilityTarget.GetTargetActor() ?? abilityTarget.Actor;

            if (target == null || !IsFocusAssignment(attacker, target))
            {
                return;
            }

            foreach (SquadOrder order in _plan.Orders)
            {
                if (order.Kind == OrderKind.FocusFire && order.Target == target && !order.Announced)
                {
                    order.Announced = true;

                    if (order.Executor != attacker)
                    {
                        order.Beneficiaries.Remove(attacker);
                        order.Beneficiaries.Add(order.Executor);
                        order.Executor = attacker;
                        _plan.Context.Commander.TryGetValue(attacker, out order.Issuer);
                    }

                    if (IsLive(order))
                    {
                        Announce(order);
                    }
                }
            }
        }

        /// <summary>
        /// The issuer calls, the executor answers: floating text over each (words for humans, a described sound for
        /// Pandorans) and the creature's own voice. Only what the player can see is shown.
        /// </summary>
        private static void Announce(SquadOrder order)
        {
            TacticalActor executor = order.Executor;
            TacticalActor issuer = order.Issuer;
            TacticalFaction player = executor.TacticalLevel?.Factions?.FirstOrDefault(f => f.IsControlledByPlayer);

            bool Seen(TacticalActor actor)
            {
                return actor != null && actor.IsAlive && player != null && player.Vision.IsRevealed(actor);
            }

            int line = UnityEngine.Random.Range(0, HumanLines);

            if (issuer != null && issuer != executor)
            {
                if (Seen(issuer))
                {
                    Say(issuer, IsHuman(issuer) ? HumanKey(order.Kind, "ORDER", line) : PandoranCallKey, 0f, call: true);
                }

                if (Seen(executor))
                {
                    Say(executor, IsHuman(executor) ? HumanKey(order.Kind, "REPLY", line) : PandoranReplyKey, Seen(issuer) ? ReplyDelay : 0f, call: false);
                }
            }
            else if (Seen(executor))
            {
                Say(executor, IsHuman(executor) ? HumanKey(order.Kind, "SELF", line) : PandoranCallKey, 0f, call: true);
            }

            TFTVLogger.Always($"[AI squad] announcing: {issuer?.DisplayName ?? "(no issuer)"} -> {executor.DisplayName}: {order.Reason}; " +
                $"issuer seen {Seen(issuer)}, executor seen {Seen(executor)}");
        }

        private static void Say(TacticalActor actor, string key, float delay, bool call)
        {
            void Show()
            {
                try
                {
                    if (actor == null || !actor.IsAlive)
                    {
                        return;
                    }

                    TFTVTauntsAndQuips.FloatingTextManager.ShowFloatingText(actor, TFTVCommonMethods.ConvertKeyToString(key), Color.white);
                    RaiseVoice(actor, call);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            if (delay <= 0f)
            {
                Show();
            }
            else
            {
                TFTVNJQuestline.DelayedInvoker.Run(delay, Show);
            }
        }

        /// <summary>Generic voice events: each creature plays its own sound for them, if its voice set has one.</summary>
        private static void RaiseVoice(TacticalActor actor, bool call)
        {
            if (_callVoice == null)
            {
                _callVoice = TFTVMain.Main.DefCache.GetDef<TacticalEventDef>("RallyVoice_EventDef");
                _replyVoice = TFTVMain.Main.DefCache.GetDef<TacticalEventDef>("GruntVoice_EventDef");
            }

            TacticalEventDef voice = call ? _callVoice : _replyVoice;

            if (voice != null && actor.TacActorEventusComponent != null)
            {
                actor.TacActorEventusComponent.RaiseEvent(voice, false, null);
            }
        }

        #endregion

        #region pass: suppression

        /// <summary>
        /// Overwatch in the way of an attack: a smart actor that has to close in (a melee fighter, or a mind controller
        /// with a predicted target out of reach) would cross a soldier's overwatch cone on its way. One or two smart
        /// gunners who between them can pin that soldier down (Moderate suppression ends its overwatch) are told to,
        /// and act before the attacker.
        ///
        /// Cost: approaches x overwatching soldiers cone tests (point-in-cone along the path, a line-of-sight ray only
        /// for a point inside), then gunners x blockers cached shot checks. A handful of each per turn.
        /// </summary>
        private sealed class SuppressionPass : ISquadPass
        {
            private const float MeleeStopDistance = 1.5f;
            private const float SampleSpacing = 1.5f;
            private const int MaxSamples = 24;
            private const int MaxSightChecks = 3;
            private const int MaxSuppressorsPerBlocker = 2;

            /// <summary>Expected points have to cover this share of what is needed, or nobody is sent.</summary>
            private const float MinCoverOfNeed = 0.9f;

            /// <summary>A gunner that would have to move first is expected to fire one burst fewer and counts for less.</summary>
            private const float MustMovePenalty = 0.7f;

            public string Name => "suppression";

            public bool IsEnabled => SuppressionRuntime.IsSuppressionEnabled;

            public bool NeedsSmartActors => true;

            private sealed class Approach
            {
                internal TacticalActor Actor;
                internal TacticalActorBase Target;
                internal Vector3 From;
                internal Vector3 To;
            }

            public void Plan(SquadContext context, SquadPlan plan)
            {
                List<TacticalActor> blockers = context.Enemies.Where(TFTVAICombatAbilities.SuppressAI.IsOverwatchToPin).ToList();

                if (blockers.Count == 0)
                {
                    return;
                }

                List<KeyValuePair<TacticalActor, TacticalActorBase>> mindControl = TFTVAICombatAbilities.MindControlReservations.Pending();
                List<Approach> approaches = FindApproaches(context, mindControl);

                if (approaches.Count == 0)
                {
                    return;
                }

                // Which soldier's overwatch covers which approach.
                Dictionary<TacticalActor, List<Approach>> covered = new Dictionary<TacticalActor, List<Approach>>();

                foreach (TacticalActor blocker in blockers)
                {
                    OverwatchStatus overwatch = blocker.Status.GetStatus<OverwatchStatus>();

                    foreach (Approach approach in approaches)
                    {
                        if (CrossesOverwatch(blocker, overwatch, approach))
                        {
                            if (!covered.TryGetValue(blocker, out List<Approach> list))
                            {
                                covered[blocker] = list = new List<Approach>();
                            }

                            list.Add(approach);
                        }
                    }
                }

                if (covered.Count == 0)
                {
                    TFTVLogger.Always($"[AI squad] suppression: {approaches.Count} approaches, {blockers.Count} soldiers on overwatch, none in the way");
                    return;
                }

                HashSet<TacticalActor> busy = new HashSet<TacticalActor>(approaches.Select(a => a.Actor));
                busy.UnionWith(mindControl.Select(r => r.Key));

                foreach (KeyValuePair<TacticalActor, List<Approach>> entry in covered.OrderByDescending(e => e.Value.Count))
                {
                    // Bursts would hurt a soldier a mind controller is about to take over, and the reservation
                    // vetoes shooting it anyway.
                    if (mindControl.Any(r => r.Value == entry.Key))
                    {
                        TFTVLogger.Always($"[AI squad] suppression: {entry.Key.DisplayName}'s overwatch is in the way, but it is reserved for mind control");
                        continue;
                    }

                    AssignSuppressors(context, plan, entry.Key, entry.Value, busy);
                }
            }

            private static List<Approach> FindApproaches(SquadContext context, List<KeyValuePair<TacticalActor, TacticalActorBase>> mindControl)
            {
                List<Approach> approaches = new List<Approach>();

                foreach (TacticalActor actor in context.Actors)
                {
                    // Stance holders (TFTVAIStances) stay put: they won't be crossing anyone's overwatch.
                    if (!context.IsSmart(actor) || !actor.IsAlive || actor.IsDisabled || SuppressionRuntime.IsSuppressionExcluded(actor)
                        || TFTVAICombatAbilities.Stances.HoldsPosition(actor))
                    {
                        continue;
                    }

                    TacticalActorBase target = null;
                    float stopDistance = MeleeStopDistance;
                    KeyValuePair<TacticalActor, TacticalActorBase> reservation = mindControl.FirstOrDefault(r => r.Key == actor);

                    if (reservation.Key != null)
                    {
                        MindControlAbility mindControlAbility = actor.GetAbility<MindControlAbility>();
                        target = reservation.Value;
                        stopDistance = mindControlAbility != null ? Mathf.Max(MeleeStopDistance, mindControlAbility.OriginTargetData.Range) : MeleeStopDistance;
                    }
                    // Leaders (Sirens, Scyllas) only count as closing in when they go for a mind control.
                    else if (LeaderRange(actor) <= 0f && actor.MaxMoveRange > 1f
                        && !actor.Equipments.GetWeapons().Any(TFTVAICombatAbilities.SuppressAI.CanFire))
                    {
                        target = Nearest(actor, context.Enemies, actor.MaxMoveRange + MeleeStopDistance);
                    }

                    if (target == null)
                    {
                        continue;
                    }

                    Vector3 offset = actor.Pos - target.Pos;

                    if (offset.magnitude <= stopDistance)
                    {
                        continue;
                    }

                    approaches.Add(new Approach
                    {
                        Actor = actor,
                        Target = target,
                        From = actor.Pos,
                        To = target.Pos + offset.normalized * stopDistance
                    });
                }

                return approaches;
            }

            private static TacticalActor Nearest(TacticalActor actor, List<TacticalActor> enemies, float maxDistance)
            {
                TacticalActor best = null;
                float bestDistance = maxDistance;

                foreach (TacticalActor enemy in enemies)
                {
                    float distance = Vector3.Distance(actor.Pos, enemy.Pos);

                    if (enemy.IsAlive && distance <= bestDistance)
                    {
                        best = enemy;
                        bestDistance = distance;
                    }
                }

                return best;
            }

            /// <summary>Whether the straight line of the approach passes through the cone where the soldier can see it.</summary>
            private static bool CrossesOverwatch(TacticalActor blocker, OverwatchStatus overwatch, Approach approach)
            {
                if (overwatch == null)
                {
                    return false;
                }

                Base.Utils.Maths.Cone cone = overwatch.GetCone();
                Vector3 path = approach.To - approach.From;
                int samples = Mathf.Clamp(Mathf.CeilToInt(path.magnitude / SampleSpacing), 1, MaxSamples);
                int sightChecks = 0;

                for (int i = 0; i <= samples; i++)
                {
                    Vector3 point = approach.From + path * ((float)i / samples);

                    if (!cone.Contains(point + Vector3.up))
                    {
                        continue;
                    }

                    if (TacticalFactionVision.CheckVisibleLineBetweenActors(blocker, blocker.Pos, approach.Actor, false, point))
                    {
                        return true;
                    }

                    if (++sightChecks >= MaxSightChecks)
                    {
                        return false;
                    }
                }

                return false;
            }

            private static void AssignSuppressors(SquadContext context, SquadPlan plan, TacticalActor blocker, List<Approach> approaches, HashSet<TacticalActor> busy)
            {
                float need = SuppressionRuntime.PointsToModerate(blocker);

                if (need <= 0f || float.IsInfinity(need))
                {
                    return;
                }

                List<KeyValuePair<TacticalActor, float>> candidates = new List<KeyValuePair<TacticalActor, float>>();

                foreach (TacticalActor gunner in context.Actors)
                {
                    if (busy.Contains(gunner) || !context.IsSmart(gunner) || !gunner.IsAlive || gunner.IsDisabled)
                    {
                        continue;
                    }

                    float expected = ExpectedSuppression(gunner, blocker);

                    if (expected > 0f)
                    {
                        candidates.Add(new KeyValuePair<TacticalActor, float>(gunner, expected));
                    }
                }

                List<KeyValuePair<TacticalActor, float>> chosen = new List<KeyValuePair<TacticalActor, float>>();
                float total = 0f;

                foreach (KeyValuePair<TacticalActor, float> candidate in candidates.OrderByDescending(c => c.Value))
                {
                    if (total >= need || chosen.Count >= MaxSuppressorsPerBlocker)
                    {
                        break;
                    }

                    chosen.Add(candidate);
                    total += candidate.Value;
                }

                string attackers = string.Join(", ", approaches.Select(a => $"{a.Actor.DisplayName} -> {a.Target.DisplayName}"));

                if (chosen.Count == 0 || total < need * MinCoverOfNeed)
                {
                    TFTVLogger.Always($"[AI squad] suppression: {blocker.DisplayName}'s overwatch covers {attackers}, but nobody can pin it " +
                        $"(needs {need:0.#} points, best {total:0.#} from {chosen.Count} gunners of {candidates.Count})");
                    return;
                }

                foreach (KeyValuePair<TacticalActor, float> pick in chosen)
                {
                    TacticalActor gunner = pick.Key;
                    context.Commander.TryGetValue(gunner, out TacticalActor leader);

                    SquadOrder order = new SquadOrder
                    {
                        Kind = OrderKind.Suppress,
                        Executor = gunner,
                        Issuer = leader,
                        Target = blocker,
                        Reason = $"suppress {blocker.DisplayName} (overwatch) so {attackers} can get through; " +
                            $"expects {pick.Value:0.#} of {need:0.#} points"
                    };

                    foreach (Approach approach in approaches)
                    {
                        order.Beneficiaries.Add(approach.Actor);
                        plan.ActBefore.Add(new KeyValuePair<TacticalActor, TacticalActor>(gunner, approach.Actor));
                    }

                    plan.Orders.Add(order);
                    busy.Add(gunner);

                    TFTVLogger.Always($"[AI squad] {leader?.DisplayName ?? "(own call)"} -> {gunner.DisplayName}: {order.Reason}");
                }
            }

            /// <summary>
            /// Points the gunner's best gun should land on the blocker this turn, or 0 when it can't fire or can't get
            /// to a shot. With no shot from where it stands, it has to be within move-and-shoot reach.
            /// </summary>
            private static float ExpectedSuppression(TacticalActor gunner, TacticalActor blocker)
            {
                float best = 0f;
                float distance = Vector3.Distance(gunner.Pos, blocker.Pos);

                foreach (Weapon weapon in gunner.Equipments.GetWeapons())
                {
                    if (!TFTVAICombatAbilities.SuppressAI.CanFire(weapon))
                    {
                        continue;
                    }

                    int bursts = TFTVAICombatAbilities.SuppressAI.AffordableBursts(gunner, weapon, gunner.CharacterStats.ActionPoints);

                    if (bursts == 0)
                    {
                        continue;
                    }

                    float factor = 1f;

                    if (!TFTVAICombatAbilities.SuppressAI.HasShot(gunner, weapon, blocker, gunner.Pos))
                    {
                        if (distance > weapon.WeaponDef.EffectiveRange + gunner.MaxMoveRange * 0.5f)
                        {
                            continue;
                        }

                        bursts = Mathf.Max(1, bursts - 1);
                        factor = MustMovePenalty;
                    }

                    best = Mathf.Max(best, TFTVAICombatAbilities.SuppressAI.ExpectedPointsPerBurst(weapon) * bursts * factor);
                }

                return best;
            }
        }

        #endregion

        #region patches

        /// <summary>Applies the plan's target weights to every attack score, after any custom scoring.</summary>
        [HarmonyPatch(typeof(AIAttackPositionConsideration), nameof(AIAttackPositionConsideration.Evaluate))]
        internal static class TFTV_AIAttackPositionConsideration_Evaluate_SquadWeights_patch
        {
            public static void Postfix(IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (__result > 0f && _plan != null && _plan.Weights.Count > 0
                        && actor is TacticalActor tacticalActor && target is TacAITarget tacAITarget)
                    {
                        __result *= TargetMultiplier(tacticalActor, tacAITarget.Actor);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>Same for paralysing and goo weapons, which score through their own consideration.</summary>
        [HarmonyPatch(typeof(AINonHealthDamageAttackPositionConsideration), nameof(AINonHealthDamageAttackPositionConsideration.Evaluate))]
        internal static class TFTV_AINonHealthDamageAttackPositionConsideration_Evaluate_SquadWeights_patch
        {
            public static void Postfix(IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (__result > 0f && _plan != null && _plan.Weights.Count > 0
                        && actor is TacticalActor tacticalActor && target is TacAITarget tacAITarget)
                    {
                        __result *= TargetMultiplier(tacticalActor, tacAITarget.Actor);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AIBlackboard), nameof(AIBlackboard.BeforeAIActorEvaluation))]
        internal static class TFTV_AIBlackboard_BeforeAIActorEvaluation_Squad_patch
        {
            public static void Prefix(TacAIActor aiActor)
            {
                try
                {
                    OnBeforeActorEvaluation(aiActor?.TacticalActor);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        #endregion
    }
}
