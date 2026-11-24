using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVAISquadPlanner
    {
        #region pass: protect

        /// <summary>
        /// Protect the side's important units: its leaders (human leaders and lieutenants, Sirens, Scyllas) and its
        /// snipers (a gun of sniper range). A revealed soldier who could hit one of them on its next turn (a gun within
        /// effective range plus half a move, with a line of sight or close by; a melee weapon within a charge) is a
        /// threat. Every actor that can reach a threat this turn has its attacks on the soldiers that are no threat
        /// scaled by <see cref="NoThreatWeight"/>, and the focus pass counts a threat as worth
        /// <see cref="ThreatWorth"/> times as much.
        ///
        /// Runs every turn whatever the side's make-up (snipers are important without a leader about); it only costs
        /// important units x revealed soldiers distance tests and at most <see cref="MaxSightChecks"/> sight lines.
        /// </summary>
        private sealed class ProtectPass : ISquadPass
        {
            internal const string Source = "protect";

            private const float NoThreatWeight = 0.6f;
            private const float ThreatWorth = 1.5f;
            private const float NearEnoughWithoutSight = 8f;
            private const int MaxSightChecks = 40;

            private GameTagDef _meleeTag;

            public string Name => Source;

            public bool IsEnabled => true;

            public bool NeedsSmartActors => false;

            public void Plan(SquadContext context, SquadPlan plan)
            {
                if (context.Enemies.Count == 0)
                {
                    return;
                }

                if (_meleeTag == null)
                {
                    _meleeTag = TFTVMain.Main.DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");
                }

                List<TacticalActor> important = context.Actors
                    .Where(a => a != null && a.IsAlive && !a.IsDisabled
                        && (LeaderRange(a) > 0f || TFTVAICombatAbilities.NestAndAmbush.IsSniper(a)))
                    .ToList();

                if (important.Count == 0)
                {
                    return;
                }

                Dictionary<TacticalActor, List<TacticalActor>> threats = new Dictionary<TacticalActor, List<TacticalActor>>();
                int sightChecks = 0;

                foreach (TacticalActor enemy in context.Enemies)
                {
                    foreach (TacticalActor ally in important)
                    {
                        if (CanThreaten(enemy, ally, ref sightChecks))
                        {
                            if (!threats.TryGetValue(enemy, out List<TacticalActor> allies))
                            {
                                threats[enemy] = allies = new List<TacticalActor>();
                            }

                            allies.Add(ally);
                        }
                    }
                }

                if (threats.Count == 0)
                {
                    return;
                }

                foreach (TacticalActor threat in threats.Keys)
                {
                    plan.TargetWorth[threat] = (plan.TargetWorth.TryGetValue(threat, out float worth) ? worth : 1f) * ThreatWorth;
                }

                int weighted = 0;

                foreach (TacticalActor attacker in context.Actors)
                {
                    if (attacker == null || !attacker.IsAlive || attacker.IsDisabled)
                    {
                        continue;
                    }

                    List<TacticalActor> reachable = context.Enemies.Where(e => e.IsAlive && CanReach(attacker, e)).ToList();

                    if (!reachable.Any(threats.ContainsKey))
                    {
                        continue;
                    }

                    List<TacticalActor> reachableThreats = reachable.Where(threats.ContainsKey).ToList();

                    foreach (TacticalActor other in reachable.Where(e => !threats.ContainsKey(e)))
                    {
                        plan.AddWeight(attacker, other, NoThreatWeight,
                            $"{string.Join(", ", reachableThreats.Select(t => t.DisplayName))} threaten important allies first", Source,
                            stillApplies: () => reachableThreats.Any(t => t.IsAlive), log: false);
                        weighted++;
                    }
                }

                TFTVLogger.Always($"[AI squad] protect: " + string.Join("; ", threats.Select(t =>
                    $"{t.Key.DisplayName} threatens {string.Join(", ", t.Value.Select(a => a.DisplayName))}")) +
                    $"; {weighted} attacks on others scaled x{NoThreatWeight}, threats worth x{ThreatWorth} to focus fire ({sightChecks} sight checks)");
            }

            /// <summary>Whether the soldier could hit the ally on its next turn.</summary>
            private bool CanThreaten(TacticalActor enemy, TacticalActor ally, ref int sightChecks)
            {
                if (enemy == null || !enemy.IsAlive || enemy.Equipments == null)
                {
                    return false;
                }

                float distance = Vector3.Distance(enemy.Pos, ally.Pos);

                foreach (Weapon weapon in enemy.Equipments.GetWeapons())
                {
                    if (weapon?.WeaponDef == null || !weapon.IsUsable)
                    {
                        continue;
                    }

                    if (TFTVAICombatAbilities.PeekAndFallBackAI.IsGun(weapon))
                    {
                        if (distance > weapon.WeaponDef.EffectiveRange + enemy.MaxMoveRange * 0.5f)
                        {
                            continue;
                        }

                        if (distance <= NearEnoughWithoutSight)
                        {
                            return true;
                        }

                        if (sightChecks < MaxSightChecks)
                        {
                            sightChecks++;

                            if (TacticalFactionVision.CheckVisibleLineBetweenActors(enemy, enemy.Pos, ally, false, ally.Pos))
                            {
                                return true;
                            }
                        }
                    }
                    else if (_meleeTag != null && weapon.WeaponDef.Tags.Contains(_meleeTag)
                        && distance <= enemy.MaxMoveRange * 0.8f + 1.5f)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Rough reach this turn: a gun within effective range plus half a move, a melee weapon within a charge.</summary>
            private bool CanReach(TacticalActor attacker, TacticalActor target)
            {
                float distance = Vector3.Distance(attacker.Pos, target.Pos);

                foreach (Weapon weapon in attacker.Equipments.GetWeapons())
                {
                    if (TFTVAICombatAbilities.SuppressAI.CanFire(weapon))
                    {
                        if (distance <= weapon.WeaponDef.EffectiveRange + attacker.MaxMoveRange * 0.5f)
                        {
                            return true;
                        }
                    }
                    else if (weapon?.WeaponDef != null && weapon.IsUsable && _meleeTag != null && weapon.WeaponDef.Tags.Contains(_meleeTag)
                        && distance <= attacker.MaxMoveRange * 0.8f + 1.5f)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion
    }
}
