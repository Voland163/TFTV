using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class ShieldDeployedTacticalVanillaFixes
    {
        /// <summary>
        /// A deployed shield retracting itself when a tactical save is loaded, leaving the soldier free to
        /// move with the shield in hand.
        ///
        /// ShieldDeployedStatus retracts on two signals: its shield being detached (OnDetached) and its
        /// carrier moving (OnActorMoved; it ignores exactly one move, the one it expects while being
        /// restored). Loading a save fires them while the status is being restored - the shield is
        /// re-attached to its shield point ("attaching ... for a second time") - so the status was
        /// unapplied in the same instant it was applied. With it gone nothing roots the soldier: the
        /// abilities bar, which with a shield deployed only offers Retrieve Shield and Stand By, is back
        /// to normal and the soldier can walk off.
        ///
        /// Neither signal is the player doing anything while a save is loading, so both are ignored then.
        /// </summary>
        private static bool IsLoading(ShieldDeployedStatus status)
        {
            return status?.TacticalActor?.TacticalLevel != null
                && status.TacticalActor.TacticalLevel.IsLoadingSavedGame;
        }

        private static void LogKept(ShieldDeployedStatus status, string signal)
        {
            TFTVLogger.Always($"[ShieldDeployed] Kept {status?.TacticalActor?.DisplayName}'s shield deployed: ignored '{signal}' while loading a save.");
        }

        [HarmonyPatch(typeof(ShieldDeployedStatus), "OnDetached")]
        internal static class ShieldDeployedStatus_OnDetached_Patch
        {
            private static bool Prefix(ShieldDeployedStatus __instance)
            {
                try
                {
                    if (!IsLoading(__instance))
                    {
                        return true;
                    }

                    LogKept(__instance, "shield detached");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(ShieldDeployedStatus), "OnActorMoved")]
        internal static class ShieldDeployedStatus_OnActorMoved_Patch
        {
            private static bool Prefix(ShieldDeployedStatus __instance, TacticalActorBase actor)
            {
                try
                {
                    if (actor == null || actor != __instance?.TacticalActor || !IsLoading(__instance))
                    {
                        return true;
                    }

                    LogKept(__instance, "carrier moved");
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        /// <summary>
        /// A soldier still holding the shield in one hand after retrieving it.
        ///
        /// Deploying makes the shield the selected equipment (TacticalAbility.Activate, or the AI action
        /// before it), but RetrieveShieldAbility never selects anything else: whatever is selected when
        /// the shield comes back stays selected. A player usually picked a weapon in between, and TFTV's
        /// AI turn-start weapon pick (GetBestWeaponForQA) happened to do the same for the AI, so it only
        /// showed when a unit deployed and retrieved without switching in between.
        ///
        /// The equipment a unit switches away from when it takes up a shield is remembered, and once the
        /// retrieve finishes, a unit still holding the shield gets it back (or its first usable weapon).
        /// </summary>
        private static readonly ConditionalWeakTable<EquipmentComponent, Equipment> EquipmentBeforeShield =
            new ConditionalWeakTable<EquipmentComponent, Equipment>();

        private static bool IsShield(Equipment equipment)
        {
            return equipment?.TacticalItemDef?.Abilities != null
                && equipment.TacticalItemDef.Abilities.Any(ability => ability is DeployShieldAbilityDef);
        }

        [HarmonyPatch(typeof(EquipmentComponent), nameof(EquipmentComponent.SetSelectedEquipment))]
        internal static class EquipmentComponent_SetSelectedEquipment_Patch
        {
            private static void Prefix(EquipmentComponent __instance, Equipment equipment)
            {
                try
                {
                    Equipment current = __instance.SelectedEquipment;

                    if (current == null || current == equipment || IsShield(current) || !IsShield(equipment))
                    {
                        return;
                    }

                    EquipmentBeforeShield.Remove(__instance);
                    EquipmentBeforeShield.Add(__instance, current);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch]
        internal static class RetrieveShieldAbility_RetrieveShield_Patch
        {
            private static MethodBase TargetMethod()
            {
                // The compiler-generated iterator of RetrieveShieldAbility.RetrieveShield (<RetrieveShield>d__5).
                Type iterator = typeof(RetrieveShieldAbility).GetNestedTypes(BindingFlags.NonPublic)
                    .First(type => type.Name.StartsWith("<RetrieveShield>"));
                return AccessTools.Method(iterator, "MoveNext");
            }

            private static void Postfix(object __instance, bool __result)
            {
                try
                {
                    if (__result)
                    {
                        return;
                    }

                    RetrieveShieldAbility ability = Traverse.Create(__instance).Field("<>4__this").GetValue<RetrieveShieldAbility>();
                    TacticalActor actor = ability?.TacticalActor;
                    EquipmentComponent equipments = actor?.Equipments;

                    if (equipments == null || actor.IsDead || !IsShield(equipments.SelectedEquipment))
                    {
                        return;
                    }

                    Equipment restore = null;

                    if (EquipmentBeforeShield.TryGetValue(equipments, out Equipment before)
                        && before != null && before.IsUsable && equipments.Equipments.Contains(before))
                    {
                        restore = before;
                    }

                    if (restore == null)
                    {
                        restore = equipments.GetWeapons().FirstOrDefault(weapon => weapon != null && weapon.IsUsable);
                    }

                    if (restore == null)
                    {
                        return;
                    }

                    TFTVLogger.Always($"[ShieldDeployed] {actor.DisplayName} retrieved the shield still holding it; switching back to {restore.DisplayName}.");
                    equipments.SetSelectedEquipment(restore);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
