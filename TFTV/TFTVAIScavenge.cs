using Base.AI.Defs;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        #region 7. scavenging

        /// <summary>
        /// A Triton or a human enemy left with nothing to fight with - no weapon it can use, or only guns that are
        /// empty with no spare magazine - picks up a weapon or a magazine lying on the ground. Only loose drops count
        /// (ItemContainer, as dead soldiers and dropped gear leave them), never mission crates.
        ///
        /// Two halves. Go For Gear is a move (weight 800, above Go To Mist) to a spot next to a useful drop that leaves
        /// the AP to pick it up; it vetoes Flee (weight 9999, fires when the actor can't use its weapons) while a drop
        /// is within reach. The pickup itself runs before each of the actor's AI evaluations: with a useful drop next
        /// to it and the AP, it takes the item, pays the Inventory ability's cost (1 of 4 AP), and the evaluation then
        /// sees an armed actor. Magazines go into the backpack and Reload_AIActionDef (added to the Triton template,
        /// which lacks it) loads them.
        /// </summary>
        internal static class Scavenging
        {
            private const float GoForGearWeight = 800f;

            /// <summary>The Inventory ability reaches 1.42 m (origin) / 1.3 m (target); a little slack for floor snapping.</summary>
            private const float PickupReach = 1.45f;

            /// <summary>Drops further than this (horizontally) are not looked at.</summary>
            private const float SearchRadius = 25f;

            internal static AIActionMoveToPositionDef GoForGearActionDef;

            private static AIHasEquipmentConsiderationDef _shouldGoForGearDef;
            private static AIHasEquipmentConsiderationDef _notGoingForGearDef;
            private static AIRoofAboveConsiderationDef _gearPositionDef;

            private static GameTagDef _humanTag;
            private static ClassTagDef _tritonTag;

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, List<KeyValuePair<ItemContainer, TacticalItem>>>> DropsByFrame =
                new Dictionary<TacticalActor, KeyValuePair<int, List<KeyValuePair<ItemContainer, TacticalItem>>>>();

            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> ReachableByFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

            private static readonly HashSet<string> Logged = new HashSet<string>();

            internal static void CreateDefs()
            {
                try
                {
                    _humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");
                    _tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIRoofAboveConsiderationDef roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");

                    _shouldGoForGearDef = Helper.CreateDefFromClone(hasGun, "6e2b9f41-0c7d-4a85-b3e6-d19a47c2f805", "TFTV_ShouldGoForGear_AIConsiderationDef");
                    _notGoingForGearDef = Helper.CreateDefFromClone(hasGun, "a35d7c08-91e4-4b2f-8f6a-5c0e2d9b71e4", "TFTV_NotGoingForGear_AIConsiderationDef");
                    _gearPositionDef = Helper.CreateDefFromClone(roof, "d81f4a6c-2b93-4e07-a5c8-7f0e1b3d92a6", "TFTV_GearPosition_AIConsiderationDef");

                    GoForGearActionDef = Helper.CreateDefFromClone(
                        DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"),
                        "3f7c0e92-5a41-4d6b-9e18-b2c4f8a07d53",
                        "TFTV_GoForGear_AIActionDef");

                    GoForGearActionDef.Weight = GoForGearWeight;

                    // With or without an enemy in view.
                    RemoveConsiderations(GoForGearActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                    PeekAndFallBackAI.AddEarlyExit(GoForGearActionDef, _shouldGoForGearDef);

                    AITargetEvaluation positions = GoForGearActionDef.Evaluations[0];

                    AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                        (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                        "9b05e7d3-6c28-4f91-8a4e-0d3c7b6f2e18",
                        "TFTV_GoForGearZone_AITargetGeneratorDef");

                    zone.ZoneRange = 1f;
                    // A drop at the actor's feet is picked up before the evaluation; a move to its own spot is invalid anyway.
                    zone.IncludeSelfActorPosition = false;
                    positions.TargetGeneratorDef = zone;

                    positions.Considerations = new AIAdjustedConsideration[]
                    {
                        new AIAdjustedConsideration { Consideration = _gearPositionDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                        new AIAdjustedConsideration
                        {
                            Consideration = DefCache.GetDef<AILineOfSightToEnemiesConsiderationDef>("NoLineofSight_AIConsiderationDef"),
                            ScoreCurve = AnimationCurve.Linear(0f, 0.7f, 1f, 1f)
                        }
                    };

                    AddActionToTemplate(SoldierAITemplate, GoForGearActionDef);
                    AddActionToTemplate("Fishman_AIActionsTemplateDef", GoForGearActionDef);

                    // Tritons have no Reload action, so a magazine they picked up would just sit in the backpack.
                    AddActionToTemplate("Fishman_AIActionsTemplateDef", DefCache.GetDef<AIActionReloadDef>("Reload_AIActionDef"));

                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveAndEscapeDef>("Flee_AIActionDef"), _notGoingForGearDef);

                    TFTVLogger.Always("TFTV AI: Go For Gear added to the soldier and Triton templates; Reload added to the Triton template");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static bool IsScavenger(TacticalActor actor)
            {
                return actor != null && actor.IsAlive && !actor.IsControlledByPlayer && actor.GameTags != null
                    && ((_humanTag != null && actor.GameTags.Contains(_humanTag)) || (_tritonTag != null && actor.GameTags.Contains(_tritonTag)))
                    && PeekAndFallBackAI.IsRegularSoldier(actor);
            }

            private static bool HasLoadedCharges(TacticalItem item)
            {
                return !item.HasCharges || item.InfiniteCharges || item.CommonItemData.CurrentCharges > 0;
            }

            private static IEnumerable<TacticalItem> CarriedItems(TacticalActor actor)
            {
                IEnumerable<Item> backpack = actor.Inventory?.Items ?? Enumerable.Empty<Item>();
                IEnumerable<Item> equipped = actor.Equipments?.Items ?? Enumerable.Empty<Item>();
                return backpack.Concat(equipped).OfType<TacticalItem>();
            }

            private static bool IsAmmoFor(Weapon weapon, TacticalItem item)
            {
                TacticalItemDef[] compatible = weapon.TacticalItemDef?.CompatibleAmmunition;
                return compatible != null && item.TacticalItemDef != null && compatible.Contains(item.TacticalItemDef) && HasLoadedCharges(item);
            }

            /// <summary>A weapon it holds that can attack now, or after a reload from what it carries.</summary>
            private static bool CanAttackWith(TacticalActor actor, Weapon weapon)
            {
                if (!weapon.IsUsable)
                {
                    return false;
                }

                return HasLoadedCharges(weapon) || CarriedItems(actor).Any(i => i != weapon && IsAmmoFor(weapon, i));
            }

            /// <summary>Nothing to fight with, or carries guns and every one of them is dry.</summary>
            internal static bool NeedsGear(TacticalActor actor)
            {
                if (!IsScavenger(actor) || actor.Equipments == null)
                {
                    return false;
                }

                List<Weapon> weapons = actor.Equipments.GetWeapons().ToList();

                if (!weapons.Any(w => CanAttackWith(actor, w)))
                {
                    return true;
                }

                List<Weapon> guns = weapons.Where(PeekAndFallBackAI.IsGun).ToList();
                return guns.Count > 0 && !guns.Any(w => CanAttackWith(actor, w));
            }

            /// <summary>A loaded weapon its hands can hold, or a magazine for a gun it holds and can use.</summary>
            private static bool IsUseful(TacticalActor actor, TacticalItem item)
            {
                if (item is Weapon weapon)
                {
                    return item.TacticalItemDef != null && item.TacticalItemDef.HandsToUse <= actor.GetUsableHands() && HasLoadedCharges(weapon);
                }

                return actor.Equipments.GetWeapons().Any(w => w.IsUsable && w.HasCharges && !w.InfiniteCharges && IsAmmoFor(w, item));
            }

            /// <summary>Useful drops around the actor, nearest first, worked out once a frame. Empty if it needs nothing.</summary>
            private static List<KeyValuePair<ItemContainer, TacticalItem>> UsefulDrops(TacticalActor actor)
            {
                if (DropsByFrame.TryGetValue(actor, out var cached) && cached.Key == Time.frameCount)
                {
                    return cached.Value;
                }

                List<KeyValuePair<ItemContainer, TacticalItem>> drops = new List<KeyValuePair<ItemContainer, TacticalItem>>();

                if (NeedsGear(actor) && actor.TacticalLevel?.Map != null)
                {
                    Vector2 here = new Vector2(actor.Pos.x, actor.Pos.z);

                    foreach (ItemContainer container in actor.TacticalLevel.Map.GetActors<ItemContainer>())
                    {
                        if (container == null || container is CrateItemContainer || container.Inventory?.Items == null)
                        {
                            continue;
                        }

                        if (Vector2.Distance(here, new Vector2(container.Pos.x, container.Pos.z)) > SearchRadius)
                        {
                            continue;
                        }

                        // A loaded weapon beats a magazine: it needs no reload.
                        TacticalItem best = container.Inventory.Items.OfType<TacticalItem>()
                            .Where(i => IsUseful(actor, i))
                            .OrderBy(i => i is Weapon ? 0 : 1)
                            .FirstOrDefault();

                        if (best != null)
                        {
                            drops.Add(new KeyValuePair<ItemContainer, TacticalItem>(container, best));
                        }
                    }

                    drops = drops.OrderBy(d => (d.Key.Pos - actor.Pos).sqrMagnitude).ToList();
                }

                DropsByFrame[actor] = new KeyValuePair<int, List<KeyValuePair<ItemContainer, TacticalItem>>>(Time.frameCount, drops);
                return drops;
            }

            private static float PickupCost(TacticalActor actor)
            {
                InventoryAbility inventory = actor.GetAbility<InventoryAbility>();
                return inventory != null ? inventory.ActionPointCost : actor.CharacterStats.ActionPoints.Max * 0.25f;
            }

            /// <summary>A useful drop it can walk to this turn and still pick up. Path lengths are in metres, about one AP each.</summary>
            private static bool DropReachable(TacticalActor actor)
            {
                if (ReachableByFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == Time.frameCount)
                {
                    return cached.Value;
                }

                bool reachable = false;
                float budget = actor.CharacterStats.ActionPoints - PickupCost(actor);

                if (budget >= 0f)
                {
                    foreach (KeyValuePair<ItemContainer, TacticalItem> drop in UsefulDrops(actor).Take(4))
                    {
                        if (Vector3.Distance(actor.Pos, drop.Key.Pos) <= PickupReach
                            || AIUtil.GetPathLength(actor, actor.Pos, drop.Key.Pos, true, PickupReach) <= budget)
                        {
                            reachable = true;
                            break;
                        }
                    }
                }

                ReachableByFrame[actor] = new KeyValuePair<int, bool>(Time.frameCount, reachable);
                return reachable;
            }

            /// <summary>The early exits are clones of HasGun; returns false for any other def.</summary>
            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (actor == null || def == null || GoForGearActionDef == null || (def != _shouldGoForGearDef && def != _notGoingForGearDef))
                {
                    return false;
                }

                try
                {
                    bool goes = UsefulDrops(actor).Count > 0 && actor.CharacterStats.ActionPoints >= PickupCost(actor)
                        && (def == _shouldGoForGearDef || DropReachable(actor));

                    if (def == _notGoingForGearDef && goes)
                    {
                        Log(actor, "flee", $"TFTV AI gear: {actor.DisplayName} stays to pick up {UsefulDrops(actor)[0].Value.TacticalItemDef?.name} instead of fleeing");
                    }

                    score = def == _shouldGoForGearDef ? (goes ? 1f : 0f) : (goes ? 0f : 1f);
                }
                catch (Exception e)
                {
                    // Fail toward the stock behaviour: no Go For Gear, Flee as before.
                    LogOnce(e);
                    score = def == _shouldGoForGearDef ? 0f : 1f;
                }

                return true;
            }

            /// <summary>The position score is a clone of RoofAbove; returns false for any other def.</summary>
            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 0f;

                if (actor == null || target == null || def == null || def != _gearPositionDef)
                {
                    return false;
                }

                try
                {
                    float left = actor.CharacterStats.ActionPoints - target.PathLength - PickupCost(actor);

                    if (left < 0f)
                    {
                        return true;
                    }

                    foreach (KeyValuePair<ItemContainer, TacticalItem> drop in UsefulDrops(actor))
                    {
                        if (Vector3.Distance(target.Pos, drop.Key.Pos) <= PickupReach)
                        {
                            // A weapon beats a magazine, and a shorter walk beats a longer one.
                            float ap = Mathf.Max(1f, actor.CharacterStats.ActionPoints.Max);
                            score = Mathf.Max(score, (drop.Value is Weapon ? 1f : 0.8f) * (1f - 0.3f * Mathf.Clamp01(target.PathLength / ap)));
                        }
                    }
                }
                catch (Exception e)
                {
                    LogOnce(e);
                    score = 0f;
                }

                return true;
            }

            /// <summary>Called from TFTVArtOfCrab's move prefix, for the log.</summary>
            internal static bool RecordMove(AIActionDef actionDef, TacticalActor actor, TacAITarget target)
            {
                if (GoForGearActionDef == null || actionDef != GoForGearActionDef)
                {
                    return false;
                }

                KeyValuePair<ItemContainer, TacticalItem> drop = UsefulDrops(actor).FirstOrDefault(d => Vector3.Distance(target.Pos, d.Key.Pos) <= PickupReach);
                TFTVLogger.Always($"TFTV AI gear: {actor.DisplayName} moves {Vector3.Distance(actor.Pos, target.Pos):0.0}m to pick up " +
                    $"{drop.Value?.TacticalItemDef?.name ?? "something"} at {target.Pos}");
                return true;
            }

            /// <summary>Before each of the actor's AI evaluations: takes a useful drop within reach, if it has the AP.</summary>
            internal static void TryPickUp(TacticalActor actor)
            {
                if (GoForGearActionDef == null || !IsScavenger(actor))
                {
                    return;
                }

                float cost = PickupCost(actor);

                if (actor.CharacterStats.ActionPoints < cost)
                {
                    return;
                }

                KeyValuePair<ItemContainer, TacticalItem> drop = UsefulDrops(actor).FirstOrDefault(d => Vector3.Distance(actor.Pos, d.Key.Pos) <= PickupReach);

                if (drop.Key == null)
                {
                    return;
                }

                ItemContainer container = drop.Key;
                TacticalItem item = drop.Value;
                string swapped = "";

                if (item is Weapon weapon)
                {
                    // Three equipment slots: an unusable or dry weapon goes on the ground to make room.
                    List<Equipment> equipped = actor.Equipments.Equipments.ToList();

                    if (equipped.Count >= 3)
                    {
                        Equipment old = equipped.FirstOrDefault(e => e is Weapon w && !CanAttackWith(actor, w)) ?? equipped.FirstOrDefault(e => !(e is Weapon));

                        if (old == null)
                        {
                            return;
                        }

                        actor.Equipments.RemoveItem(old);
                        container.Inventory.AddItem(old, null);
                        swapped = $", leaving {old.TacticalItemDef?.name}";
                    }

                    container.Inventory.RemoveItem(item);
                    actor.Equipments.AddItem(weapon, null);
                    actor.Equipments.SetSelectedEquipment(weapon);
                }
                else
                {
                    container.Inventory.RemoveItem(item);
                    actor.Inventory.AddItem(item, null);
                }

                actor.CharacterStats.ActionPoints.Subtract(cost);
                DropsByFrame.Remove(actor);
                ReachableByFrame.Remove(actor);

                TFTVLogger.Always($"TFTV AI gear: {actor.DisplayName} picks up {item.TacticalItemDef?.name} ({item.CommonItemData?.CurrentCharges} charges){swapped} " +
                    $"for {cost:0.#} AP, {(float)actor.CharacterStats.ActionPoints:0.#} left");
            }

            private static void Log(TacticalActor actor, string key, string line)
            {
                if (Logged.Add($"{actor.name}|{key}"))
                {
                    TFTVLogger.Always(line);
                }
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

            internal static void ClearTurn()
            {
                DropsByFrame.Clear();
                ReachableByFrame.Clear();
                Logged.Clear();
            }
        }

        [HarmonyPatch(typeof(AIBlackboard), nameof(AIBlackboard.BeforeAIActorEvaluation))]
        internal static class TFTV_AIBlackboard_BeforeAIActorEvaluation_Scavenge_patch
        {
            public static void Prefix(TacAIActor aiActor)
            {
                try
                {
                    Scavenging.TryPickUp(aiActor?.TacticalActor);
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
