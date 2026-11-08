using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVAISquadPlanner
    {
        #region pass: mind control hold

        /// <summary>
        /// Predicts which soldier each mind controller is going to take (MindControlReservations) and holds the whole
        /// side's fire on that soldier until the controller has had its go, been stopped, or spent its mind control.
        /// Runs every turn whatever the side's size, as the reservations did before the planner existed.
        /// </summary>
        private sealed class MindControlHoldPass : ISquadPass
        {
            internal const string Source = "mind control hold";

            public string Name => Source;

            public bool IsEnabled => true;

            public bool NeedsSmartActors => false;

            public void Plan(SquadContext context, SquadPlan plan)
            {
                TFTVAICombatAbilities.MindControlReservations.PredictMindControlTargets(context.Actors);

                foreach (KeyValuePair<TacticalActor, TacticalActorBase> reservation in TFTVAICombatAbilities.MindControlReservations.Pending())
                {
                    TacticalActor controller = reservation.Key;
                    TacticalActorBase target = reservation.Value;

                    plan.AddWeight(null, target, 0f, $"{controller.DisplayName} is going to mind control it", Source,
                        exempt: controller,
                        stillApplies: () => TFTVAICombatAbilities.MindControlReservations.IsPending(controller, target));
                }
            }
        }

        #endregion

        #region pass: focus fire

        // A property, not a field initializer: Passes (in TFTVAISquad.cs) needs it, and initializer order across
        // the files of a partial class is not guaranteed.
        private static FocusFirePass _focusPass;
        private static FocusFirePass FocusPass => _focusPass ?? (_focusPass = new FocusFirePass());

        internal static bool IsFocusAssignment(TacticalActor attacker, TacticalActorBase target)
        {
            return attacker != null && FocusPass.Assignment.TryGetValue(attacker, out TacticalActorBase assigned) && assigned == target;
        }

        /// <summary>
        /// Shares the smart attackers' damage out between the soldiers they can reach this turn.
        ///
        /// Each attacker's expected damage on each target is worked out once per turn, from where it stands: its best
        /// weapon's per-round damage after armour (DamagePayload.CalculateDamageResultForAiTarget, as the AI's own
        /// shot scoring does) times rounds per burst, bursts it can afford, and a rough chance to hit. A gun counts a
        /// target it has a shot at from here (the AI's cached shot check) or can walk into effective range of; a melee
        /// weapon counts what it can walk up to.
        ///
        /// The assignment maximises the summed value of all targets: a target is worth its enemy weight times the
        /// share of its health taken off, plus a bonus when the damage on it should kill it. Attackers with the fewest
        /// options are placed first (an attacker that can only reach one target takes it), each where it adds the
        /// most, then single reassignments are tried until none helps. That way two attackers finish one soldier when
        /// together they should kill it, and split up when neither kill is in reach.
        ///
        /// The result is written as weights on each attacker: 1 on its assigned target, <see cref="OffPlanWeight"/>
        /// on the others it could reach, so the AI's own scoring still decides when the plan is clearly wrong from
        /// where it ends up standing. Re-planned, with fresh health, each time one of the attackers starts acting, so
        /// misses and kills earlier in the turn are taken into account; that costs only arithmetic, the damage
        /// figures are kept from the first plan (actors still to act haven't moved).
        /// </summary>
        private sealed class FocusFirePass : ISquadPass
        {
            internal const string Source = "focus fire";

            private const float OffPlanWeight = 0.5f;

            /// <summary>Expected damage has to beat the target's health by this much to count as a kill.</summary>
            private const float KillMargin = 1.15f;

            /// <summary>Value of a kill, on top of the health taken off (which is worth 1 at most).</summary>
            private const float KillBonus = 1f;

            private const float GunHitChance = 0.65f;
            private const float GunHitChanceBeyondRange = 0.4f;
            private const float MeleeHitChance = 0.9f;
            private const float MeleeReach = 1.5f;

            /// <summary>Paths aren't straight: a melee attacker is counted as reaching this share of its move in a line.</summary>
            private const float MeleePathFactor = 0.8f;

            /// <summary>A holder strikes only what is (nearly) next to it already.</summary>
            private const float HolderMeleeReach = MeleeReach + 1f;

            /// <summary>Damage worth less than this share of the target's health isn't an option at all.</summary>
            private const float MinUsefulShare = 0.05f;

            private const int ImprovementRounds = 4;

            public string Name => Source;

            public bool IsEnabled => true;

            public bool NeedsSmartActors => true;

            internal readonly Dictionary<TacticalActor, TacticalActorBase> Assignment = new Dictionary<TacticalActor, TacticalActorBase>();

            /// <summary>Expected damage per attacker and target, worked out once per turn.</summary>
            private readonly Dictionary<TacticalActor, Dictionary<TacticalActor, float>> _damage = new Dictionary<TacticalActor, Dictionary<TacticalActor, float>>();

            private string _lastSummary;
            private bool _active;
            private GameTagDef _meleeTag;

            internal void Clear()
            {
                Assignment.Clear();
                _damage.Clear();
                _lastSummary = null;
                _active = false;
            }

            public void Plan(SquadContext context, SquadPlan plan)
            {
                _active = true;
                Run(plan, null, initial: true);
            }

            /// <summary>Called when <paramref name="actingActor"/> starts acting: share out again among those still to act.</summary>
            internal void Replan(SquadPlan plan, TacticalActor actingActor)
            {
                if (!_active || plan?.Context == null)
                {
                    return;
                }

                try
                {
                    Run(plan, actingActor, initial: false);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private void Run(SquadPlan plan, TacticalActor actingActor, bool initial)
            {
                SquadContext context = plan.Context;
                plan.RemoveWeights(Source);
                Assignment.Clear();

                List<TacticalActor> targets = context.Enemies
                    .Where(t => t != null && t.IsAlive && !IsHeld(plan, t))
                    .ToList();

                List<TacticalActor> attackers = StillToAct(context, actingActor).Where(a => IsFreeAttacker(context, a)).ToList();

                if (targets.Count == 0 || attackers.Count == 0)
                {
                    return;
                }

                Dictionary<TacticalActor, float> health = targets.ToDictionary(t => t, t => Mathf.Max(1f, (float)t.CharacterStats.Health));
                Dictionary<TacticalActor, float> worth = targets.ToDictionary(t => t, t => Mathf.Max(0.1f,
                    AIUtil.GetEnemyWeight(context.Faction.AIBlackboard, t)));

                // Options per attacker: targets it can do meaningful damage to.
                Dictionary<TacticalActor, List<KeyValuePair<TacticalActor, float>>> options = new Dictionary<TacticalActor, List<KeyValuePair<TacticalActor, float>>>();

                foreach (TacticalActor attacker in attackers)
                {
                    Dictionary<TacticalActor, float> damage = DamageTable(attacker, targets);
                    List<KeyValuePair<TacticalActor, float>> list = damage
                        .Where(d => health.ContainsKey(d.Key) && d.Value >= health[d.Key] * MinUsefulShare)
                        .ToList();

                    if (list.Count > 0)
                    {
                        options[attacker] = list;
                    }
                }

                // Sharing out needs two attackers; a lone one is left to the AI's own choice.
                if (options.Count < 2)
                {
                    return;
                }

                Dictionary<TacticalActor, float> load = targets.ToDictionary(t => t, t => 0f);
                Dictionary<TacticalActor, KeyValuePair<TacticalActor, float>> chosen = new Dictionary<TacticalActor, KeyValuePair<TacticalActor, float>>();

                float Value(TacticalActor target, float damage)
                {
                    float hp = health[target];
                    return worth[target] * (Mathf.Min(damage, hp) / hp + (damage >= hp * KillMargin ? KillBonus : 0f));
                }

                // Fewest options first, then the hardest hitters.
                foreach (TacticalActor attacker in options.Keys.OrderBy(a => options[a].Count).ThenByDescending(a => options[a].Max(o => o.Value)))
                {
                    float bestGain = 0f;
                    KeyValuePair<TacticalActor, float> best = default(KeyValuePair<TacticalActor, float>);

                    foreach (KeyValuePair<TacticalActor, float> option in options[attacker])
                    {
                        float gain = Value(option.Key, load[option.Key] + option.Value) - Value(option.Key, load[option.Key]);

                        if (gain > bestGain)
                        {
                            bestGain = gain;
                            best = option;
                        }
                    }

                    if (best.Key != null)
                    {
                        chosen[attacker] = best;
                        load[best.Key] += best.Value;
                    }
                }

                // Single moves until none improves the total.
                for (int round = 0; round < ImprovementRounds; round++)
                {
                    bool improved = false;

                    foreach (TacticalActor attacker in chosen.Keys.ToList())
                    {
                        KeyValuePair<TacticalActor, float> current = chosen[attacker];
                        float loss = Value(current.Key, load[current.Key]) - Value(current.Key, load[current.Key] - current.Value);
                        float bestDelta = 0.0001f;
                        KeyValuePair<TacticalActor, float> best = default(KeyValuePair<TacticalActor, float>);

                        foreach (KeyValuePair<TacticalActor, float> option in options[attacker])
                        {
                            if (option.Key == current.Key)
                            {
                                continue;
                            }

                            float delta = Value(option.Key, load[option.Key] + option.Value) - Value(option.Key, load[option.Key]) - loss;

                            if (delta > bestDelta)
                            {
                                bestDelta = delta;
                                best = option;
                            }
                        }

                        if (best.Key != null)
                        {
                            load[current.Key] -= current.Value;
                            load[best.Key] += best.Value;
                            chosen[attacker] = best;
                            improved = true;
                        }
                    }

                    if (!improved)
                    {
                        break;
                    }
                }

                foreach (KeyValuePair<TacticalActor, KeyValuePair<TacticalActor, float>> entry in chosen)
                {
                    TacticalActor attacker = entry.Key;
                    Assignment[attacker] = entry.Value.Key;

                    foreach (KeyValuePair<TacticalActor, float> option in options[attacker])
                    {
                        if (option.Key != entry.Value.Key)
                        {
                            plan.AddWeight(attacker, option.Key, OffPlanWeight, $"assigned to {entry.Value.Key.DisplayName}", Source, log: false);
                        }
                    }
                }

                LogAssignment(chosen, health, load);

                if (initial)
                {
                    AddAnnouncements(context, plan, chosen, health, load);
                }
            }

            /// <summary>Actors of the side that haven't acted yet: from the one starting now to the end of the turn order.</summary>
            private static IEnumerable<TacticalActor> StillToAct(SquadContext context, TacticalActor actingActor)
            {
                int start = actingActor != null ? Mathf.Max(0, context.Actors.IndexOf(actingActor)) : 0;

                for (int i = start; i < context.Actors.Count; i++)
                {
                    TacticalActor actor = context.Actors[i];
                    EndTurnAbility endTurn = actor?.GetAbility<EndTurnAbility>();

                    if (actor != null && actor.IsAlive && actor.IsActive && (endTurn == null || endTurn.UsesThisTurn == 0))
                    {
                        yield return actor;
                    }
                }
            }

            /// <summary>Smart, able to act, and not already busy: no live suppress order, no mind control due.</summary>
            private static bool IsFreeAttacker(SquadContext context, TacticalActor actor)
            {
                if (!context.IsSmart(actor) || actor.IsDisabled || GetSuppressOrder(actor) != null)
                {
                    return false;
                }

                return !TFTVAICombatAbilities.MindControlReservations.Pending().Any(r => r.Key == actor);
            }

            /// <summary>A target the whole side holds fire on (mind control).</summary>
            private static bool IsHeld(SquadPlan plan, TacticalActor target)
            {
                return plan.Weights.Any(w => w.Executor == null && w.Target == target && w.Multiplier <= 0f
                    && (w.StillApplies == null || w.StillApplies()));
            }

            private Dictionary<TacticalActor, float> DamageTable(TacticalActor attacker, List<TacticalActor> targets)
            {
                if (!_damage.TryGetValue(attacker, out Dictionary<TacticalActor, float> table))
                {
                    _damage[attacker] = table = new Dictionary<TacticalActor, float>();
                }

                foreach (TacticalActor target in targets)
                {
                    if (!table.ContainsKey(target))
                    {
                        table[target] = ExpectedDamage(attacker, target);
                    }
                }

                return table;
            }

            /// <summary>Health damage the attacker's best weapon should do to the target this turn; 0 if out of reach.</summary>
            private float ExpectedDamage(TacticalActor attacker, TacticalActor target)
            {
                if (_meleeTag == null)
                {
                    _meleeTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");
                }

                float best = 0f;
                float distance = Vector3.Distance(attacker.Pos, target.Pos);
                float actionPoints = attacker.CharacterStats.ActionPoints;

                // Stance holders (TFTVAIStances) don't walk out to attack: only shots from here, only strikes next to them.
                bool holding = TFTVAICombatAbilities.Stances.HoldsPosition(attacker);

                foreach (Weapon weapon in attacker.Equipments.GetWeapons())
                {
                    if (weapon?.WeaponDef == null || !weapon.IsUsable)
                    {
                        continue;
                    }

                    bool gun = TFTVAICombatAbilities.SuppressAI.CanFire(weapon);
                    bool melee = !gun && _meleeTag != null && weapon.WeaponDef.Tags.Contains(_meleeTag);

                    if (!gun && !melee)
                    {
                        continue;
                    }

                    int attacks = TFTVAICombatAbilities.SuppressAI.AffordableBursts(attacker, weapon, actionPoints);
                    float hitChance;

                    if (gun)
                    {
                        if (TFTVAICombatAbilities.SuppressAI.HasShot(attacker, weapon, target, attacker.Pos))
                        {
                            hitChance = distance <= weapon.WeaponDef.EffectiveRange ? GunHitChance : GunHitChanceBeyondRange;
                        }
                        else if (!holding && distance <= weapon.WeaponDef.EffectiveRange + attacker.MaxMoveRange * 0.5f)
                        {
                            hitChance = GunHitChance;
                            attacks = Mathf.Max(1, attacks - 1);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (distance > (holding ? HolderMeleeReach : attacker.MaxMoveRange * MeleePathFactor + MeleeReach))
                        {
                            continue;
                        }

                        hitChance = MeleeHitChance;

                        if (distance > MeleeReach)
                        {
                            attacks = Mathf.Max(1, attacks - 1);
                        }
                    }

                    if (attacks <= 0)
                    {
                        continue;
                    }

                    DamageResult perRound = default(DamageResult);
                    weapon.GetDamagePayload().CalculateDamageResultForAiTarget(target, target, ref perRound);

                    DamagePayload payload = weapon.WeaponDef.DamagePayload;
                    float rounds = Mathf.Max(1, payload.AutoFireShotCount) * Mathf.Max(1, payload.ProjectilesPerShot);

                    best = Mathf.Max(best, perRound.HealthDamage * rounds * hitChance * attacks);
                }

                return best;
            }

            private void LogAssignment(Dictionary<TacticalActor, KeyValuePair<TacticalActor, float>> chosen,
                Dictionary<TacticalActor, float> health, Dictionary<TacticalActor, float> load)
            {
                string summary = string.Join("; ", chosen
                    .GroupBy(c => c.Value.Key)
                    .Select(g => $"{g.Key.DisplayName} (hp {health[g.Key]:0}) <- " +
                        string.Join(", ", g.Select(c => $"{c.Key.DisplayName} {c.Value.Value:0}")) +
                        (load[g.Key] >= health[g.Key] * KillMargin ? ": expected kill" : "")));

                if (summary != _lastSummary)
                {
                    _lastSummary = summary;
                    TFTVLogger.Always($"[AI squad] focus: {(summary.Length > 0 ? summary : "nobody assigned")}");
                }
            }

            /// <summary>
            /// A called target: two or more attackers on one soldier they should kill. Announced when one of them actually
            /// attacks it (OnAttack), not when the first starts acting: a plan that falls through is never called out.
            /// </summary>
            private static void AddAnnouncements(SquadContext context, SquadPlan plan,
                Dictionary<TacticalActor, KeyValuePair<TacticalActor, float>> chosen,
                Dictionary<TacticalActor, float> health, Dictionary<TacticalActor, float> load)
            {
                foreach (IGrouping<TacticalActor, TacticalActor> group in chosen.GroupBy(c => c.Value.Key, c => c.Key))
                {
                    if (group.Count() < 2 || load[group.Key] < health[group.Key] * KillMargin)
                    {
                        continue;
                    }

                    TacticalActor first = group.OrderBy(a => context.Actors.IndexOf(a)).First();
                    context.Commander.TryGetValue(first, out TacticalActor leader);

                    SquadOrder order = new SquadOrder
                    {
                        Kind = OrderKind.FocusFire,
                        Executor = first,
                        Issuer = leader,
                        Target = group.Key,
                        Reason = $"focus fire on {group.Key.DisplayName} with {string.Join(", ", group.Select(a => a.DisplayName))}"
                    };

                    order.Beneficiaries.AddRange(group.Where(a => a != first));
                    plan.Orders.Add(order);
                }
            }
        }

        #endregion
    }
}
