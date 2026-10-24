using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    internal static partial class TFTVHarshAttrition
    {
        /// <summary>
        /// A humanoid enemy (a human soldier or a Triton) whose weapon was destroyed and who escapes the map comes
        /// back at the start of its side's next turn with a fresh copy of that weapon, if it still has more than
        /// half its HP, the mission is still on and its side still has someone on the map. Once per mission.
        ///
        /// Escaping only applies EvacuatedStatus (hidden, forgotten by every side), so the same actor returns, with
        /// its name, rank and wounds. It comes back at the spot of its side's deploy zones nearest where it left
        /// that no player unit can see, with the reinforcement Action Point limit.
        /// </summary>
        internal static class DisarmedReturn
        {
            internal static DisarmedStatusDef StatusDef { get; set; }

            internal const float MinHealthShare = 0.5f;

            private const string SoldierActorDefName = "Soldier_ActorDef";
            private const string PlayerFactionCommandName = "px";
            private const string ReturnedKey = "TFTV_DISARMED_RETURNED";

            private static ClassTagDef _fishmanTag;
            private static ClassTagDef _civilianTag;

            private static ClassTagDef FishmanTag => _fishmanTag ?? (_fishmanTag = TFTVMain.Main.DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef"));
            private static ClassTagDef CivilianTag => _civilianTag ?? (_civilianTag = TFTVMain.Main.DefCache.GetDef<ClassTagDef>("Civilian_ClassTagDef"));

            private static bool IsCandidate(TacticalActor actor)
            {
                if (actor?.TacticalLevel == null || actor.GameTags == null || actor.IsControlledByPlayer)
                {
                    return false;
                }

                if (actor.TacticalFaction == actor.TacticalLevel.GetFactionByCommandName(PlayerFactionCommandName))
                {
                    return false;
                }

                if (actor.HasGameTag(TFTVMain.Shared.SharedGameTags.VehicleTag) || (CivilianTag != null && actor.GameTags.Contains(CivilianTag)))
                {
                    return false;
                }

                return actor.TacticalActorDef?.name == SoldierActorDefName || (FishmanTag != null && actor.GameTags.Contains(FishmanTag));
            }

            private static bool HasUsableWeapon(TacticalActor actor)
            {
                return actor.Equipments?.GetWeapons()?.Any(weapon => weapon != null && weapon.IsUsable && weapon.HasCharges) == true;
            }

            #region disarmed and escaping

            private static void OnWeaponDestroyed(TacticalItem item)
            {
                if (!IsEnabled || StatusDef == null || !(item is Weapon weapon) || weapon.WeaponDef == null)
                {
                    return;
                }

                TacticalActor actor = weapon.TacticalActor;

                if (actor == null || !actor.IsAlive || actor.Status == null || !IsCandidate(actor))
                {
                    return;
                }

                DisarmedStatus status = actor.Status.GetStatus<DisarmedStatus>() ?? actor.Status.ApplyStatus(StatusDef) as DisarmedStatus;

                // The first weapon lost is the one it comes back with: normally its main gun.
                if (status != null && string.IsNullOrEmpty(status.WeaponDefName))
                {
                    status.WeaponDefName = weapon.WeaponDef.name;
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} lost its {weapon.WeaponDef.name}");
                }
            }

            /// <summary>What an escaping actor is due back with, read before the escape strips its statuses.</summary>
            private sealed class Escape
            {
                internal TacticalActor Actor;
                internal string WeaponDefName;
                internal Vector3 Position;
            }

            private static Escape _escaping;

            private static void BeforeExit(TacticalActor actor)
            {
                _escaping = null;

                if (!IsEnabled || actor?.Status == null)
                {
                    return;
                }

                DisarmedStatus status = actor.Status.GetStatus<DisarmedStatus>();

                if (status == null || status.Returned || string.IsNullOrEmpty(status.WeaponDefName) || HasUsableWeapon(actor))
                {
                    return;
                }

                _escaping = new Escape { Actor = actor, WeaponDefName = status.WeaponDefName, Position = actor.Pos };
            }

            /// <summary>ExitMissionAbility removes every status but EvacuatedStatus; the record goes back on afterwards.</summary>
            private static void AfterExit(TacticalActor actor)
            {
                Escape escape = _escaping;
                _escaping = null;

                if (escape == null || escape.Actor != actor || StatusDef == null || !actor.IsEvacuated)
                {
                    return;
                }

                if (actor.Status.ApplyStatus(StatusDef) is DisarmedStatus status)
                {
                    status.WeaponDefName = escape.WeaponDefName;
                    status.PendingReturn = true;
                    status.ExitPosition = escape.Position;
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} fled disarmed; due back next turn if above {MinHealthShare:P0} HP");
                }
            }

            #endregion

            #region coming back

            private static void OnFactionStartTurn(TacticalFaction faction)
            {
                TacticalLevelController level = faction?.TacticalLevel;

                if (!IsEnabled || level?.Map == null)
                {
                    return;
                }

                foreach (TacticalActor actor in level.Map.GetActors<TacticalActor>(null).Where(actor => actor != null && actor.TacticalFaction == faction).ToList())
                {
                    DisarmedStatus status = actor.Status?.GetStatus<DisarmedStatus>();

                    if (status != null && status.PendingReturn)
                    {
                        try
                        {
                            TryReturn(level, actor, status);
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }
            }

            private static void TryReturn(TacticalLevelController level, TacticalActor actor, DisarmedStatus status)
            {
                // One chance: whatever happens now, it is not due back again.
                status.PendingReturn = false;

                float maxHealth = (float)actor.CharacterStats.Health.Max;
                float health = actor.CharacterStats.Health;

                if (!actor.IsAlive || !actor.IsEvacuated || maxHealth <= 0f || health / maxHealth <= MinHealthShare)
                {
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} stays gone: {health}/{maxHealth} HP");
                    return;
                }

                if (!level.Map.GetActors<TacticalActor>(null).Any(other => other != actor && other.TacticalFaction == actor.TacticalFaction && other.IsAlive && !other.IsEvacuated))
                {
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} stays gone: nobody of its side is left on the map");
                    return;
                }

                WeaponDef weaponDef = TFTVMain.Main.DefCache.GetDef<WeaponDef>(status.WeaponDefName);
                TacticalFaction player = level.GetFactionByCommandName(PlayerFactionCommandName);

                if (weaponDef == null || player == null || !TryFindSpot(level, actor, player, status.ExitPosition, out Vector3 spot))
                {
                    TFTVLogger.Always($"[HarshAttrition] {actor.name} stays gone: no weapon def or no free spot");
                    return;
                }

                // Back on the map: the reverse of EvacuatedStatus.HideActor.
                EvacuatedStatus evacuated = actor.Status.GetStatus<EvacuatedStatus>();

                if (evacuated != null)
                {
                    actor.Status.UnapplyStatus(evacuated);
                }

                actor.TacticalActorView?.ClearForceHidden();

                if (actor.Animator != null)
                {
                    actor.Animator.enabled = true;
                }

                TFTVPlayerDeploymentPhase.MoveActor(level, actor, spot);
                level.SituationCache.Invalidate();

                // The destroyed copy out, a fresh one in.
                foreach (Weapon old in actor.Equipments.GetWeapons().Where(weapon => weapon.WeaponDef == weaponDef && !weapon.IsUsable).ToList())
                {
                    actor.Equipments.RemoveItem(old);
                    old.Destroy();
                }

                if (actor.Equipments.AddItem(weaponDef, null) is Equipment newWeapon)
                {
                    actor.Equipments.SetSelectedEquipment(newWeapon);
                }

                status.Returned = true;

                ApplyReinforcementActionPoints(actor, weaponDef);

                TFTVTauntsAndQuips.FloatingTextManager.ShowFloatingText(actor, TFTVCommonMethods.ConvertKeyToString(ReturnedKey), Color.white);
                TFTVLogger.Always($"[HarshAttrition] {actor.name} returned at {spot} with a new {weaponDef.name}");
            }

            /// <summary>The same Action Point limit TFTV gives reinforcements (TFTVArtOfCrab.Has1APWeapon picks which).</summary>
            private static void ApplyReinforcementActionPoints(TacticalActor actor, WeaponDef weaponDef)
            {
                string statusName = weaponDef.name.Contains("Pistol") || weaponDef.name.Contains("Pincer")
                    ? "E_Status [ReinforcementStatus1AP]"
                    : "E_Status [ReinforcementStatusUnder2AP]";

                DelayedEffectStatusDef limit = TFTVMain.Main.DefCache.GetDef<DelayedEffectStatusDef>(statusName);

                if (limit != null)
                {
                    actor.Status.ApplyStatus(limit);
                }
            }

            /// <summary>
            /// A free spot in its side's deploy zones, nearest where it left first, that no player unit's
            /// perception reaches; failing that, the free spot farthest from the player's units.
            /// </summary>
            private static bool TryFindSpot(TacticalLevelController level, TacticalActor actor, TacticalFaction player, Vector3 exit, out Vector3 spot)
            {
                spot = Vector3.zero;

                List<TacticalDeployZone> zones = level.Map.GetActors<TacticalDeployZone>(null).Where(zone => zone.MissionParticipant == actor.TacticalFaction.ParticipantKind).ToList();

                if (zones.Count == 0)
                {
                    zones = level.Map.GetActors<TacticalDeployZone>(null).Where(zone => zone.MissionParticipant != TacMissionParticipant.Player).ToList();
                }

                List<TacticalActor> watchers = level.Map.GetActors<TacticalActor>(null)
                    .Where(other => other.TacticalFaction == player && other.IsAlive && !other.IsEvacuated)
                    .ToList();

                bool found = false;
                float farthest = float.MinValue;

                foreach (TacticalDeployZone zone in zones.OrderBy(zone => (zone.Pos - exit).sqrMagnitude))
                {
                    foreach (Vector3 pos in zone.GetOrderedSpawnPositions(false))
                    {
                        if (TFTVPlayerDeploymentPhase.FindBlocker(level, actor, pos) != null)
                        {
                            continue;
                        }

                        if (!watchers.Any(watcher => (watcher.Pos - pos).magnitude <= watcher.GetAdjustedPerceptionValue()))
                        {
                            spot = pos;
                            return true;
                        }

                        float nearestWatcher = watchers.Count == 0 ? float.MaxValue : watchers.Min(watcher => (watcher.Pos - pos).magnitude);

                        if (nearestWatcher > farthest)
                        {
                            farthest = nearestWatcher;
                            spot = pos;
                            found = true;
                        }
                    }
                }

                return found;
            }

            #endregion

            #region patches

            /// <summary>Equipment overrides TacticalItem's version without calling it, so weapons only go through this one.</summary>
            [HarmonyPatch(typeof(Equipment), "OnHealthReachedZero")]
            internal static class Equipment_OnHealthReachedZero_DisarmedReturn_Patch
            {
                private static void Postfix(Equipment __instance)
                {
                    try
                    {
                        OnWeaponDestroyed(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(ExitMissionAbility), nameof(ExitMissionAbility.Activate))]
            internal static class ExitMissionAbility_Activate_DisarmedReturn_Patch
            {
                private static void Prefix(ExitMissionAbility __instance)
                {
                    try
                    {
                        BeforeExit(__instance.TacticalActor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void Postfix(ExitMissionAbility __instance)
                {
                    try
                    {
                        AfterExit(__instance.TacticalActor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalFaction), nameof(TacticalFaction.StartTurn))]
            internal static class TacticalFaction_StartTurn_DisarmedReturn_Patch
            {
                private static void Postfix(TacticalFaction __instance)
                {
                    try
                    {
                        OnFactionStartTurn(__instance);
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
}
