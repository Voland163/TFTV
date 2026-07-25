using Base.Core;
using Base.Entities;
using Base.Entities.Effects;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class TurretAmmoRefillTacticalVanillaFixes
    {
        internal static class RetrieveTurretStateStore
        {
            private sealed class StoredTurretData
            {
                public ItemData WeaponData;
            }

            private static readonly ConditionalWeakTable<Equipment, StoredTurretData> StoredSnapshots =
                new ConditionalWeakTable<Equipment, StoredTurretData>();

            internal static void Store(Equipment equipment, ItemData weaponData)
            {
                if (equipment == null || weaponData == null)
                {
                    // TFTVLogger.Always($"RetrieveTurretStateStore.Store skipped. equipment null: {equipment == null}, weaponData null: {weaponData == null}.");
                    return;
                }

                ItemData clone = CloneItemData(weaponData);
                if (StoredSnapshots.TryGetValue(equipment, out _))
                {
                    StoredSnapshots.Remove(equipment);
                }

                StoredSnapshots.Add(equipment, new StoredTurretData { WeaponData = clone });

                // TFTVLogger.Always($"RetrieveTurretStateStore stored snapshot for {equipment.TacticalItemDef?.name ?? equipment.ToString()} with charges={clone?.Charges}.");
            }

            internal static bool TryApplyToActor(TacticalActor turretActor)
            {
                if (turretActor?.Inventory == null)
                {
                    // TFTVLogger.Always("RetrieveTurretStateStore.TryApplyToActor skipped due to null actor or inventory.");
                    return false;
                }

                foreach (Equipment equipment in turretActor.Inventory.Items.OfType<Equipment>())
                {
                    if (TryApply(equipment, turretActor))
                    {
                        return true;
                    }
                }

                // TFTVLogger.Always("RetrieveTurretStateStore.TryApplyToActor found no matching equipment snapshot.");
                return false;
            }

            private static bool TryApply(Equipment equipment, TacticalActor turretActor)
            {
                if (equipment == null || turretActor == null)
                {
                    return false;
                }

                if (!StoredSnapshots.TryGetValue(equipment, out StoredTurretData stored) || stored?.WeaponData == null)
                {
                    return false;
                }

                Weapon weapon = turretActor.AddonsManager?.RootAddon?.OfType<Weapon>().FirstOrDefault();
                if (weapon == null)
                {
                    // TFTVLogger.Always($"RetrieveTurretStateStore could not find weapon on turret actor {turretActor}.");
                    return false;
                }

                ItemData clone = CloneItemData(stored.WeaponData);
                weapon.CommonItemData.LoadFromData(clone);
                weapon.GetHealth().Set(clone.Health, true);
                weapon.GetArmor().Set(clone.Armor, true);
                weapon.InitMalfunctionStats(clone.Malfunction, clone.MalfunctionedLastUse);

                // TFTVLogger.Always($"RetrieveTurretStateStore reapplied snapshot to weapon {weapon.TacticalItemDef?.name ?? weapon.ToString()} Charges={clone?.Charges}.");

                StoredSnapshots.Remove(equipment);
                return true;
            }

            internal static ItemData CloneItemData(ItemData source)
            {
                if (source == null)
                {
                    return null;
                }

                ItemData clone = new ItemData
                {
                    ItemDef = source.ItemDef,
                    Charges = source.Charges,
                    Health = source.Health,
                    Armor = source.Armor,
                    Malfunction = source.Malfunction,
                    MalfunctionedLastUse = source.MalfunctionedLastUse,
                    OwnTags = source.OwnTags != null ? new List<GameTagDef>(source.OwnTags) : null
                };

                if (source.Ammo != null)
                {
                    clone.Ammo = new List<ItemData>(source.Ammo.Count);
                    for (int i = 0; i < source.Ammo.Count; i++)
                    {
                        clone.Ammo.Add(CloneItemData(source.Ammo[i]));
                    }
                }

                return clone;
            }
        }

        [HarmonyPatch(typeof(DeployTurretAbility), "OnActorSpawned")] //VERIFIED
        internal static class DeployTurretAbility_OnActorSpawned_Patch
        {
            public static void Postfix(DeployTurretAbility __instance, TacticalActor spawnedActor)
            {
                try
                {
                    if (spawnedActor == null)
                    {
                        // TFTVLogger.Always("DeployTurretAbility.OnActorSpawned postfix skipped due to null spawnedActor.");
                        return;
                    }

                    RetrieveTurretStateStore.TryApplyToActor(spawnedActor);
                    // TFTVLogger.Always($"DeployTurretAbility.OnActorSpawned postfix applied snapshot: {applied}.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(RetrieveDeployedItemAbility), "RetrieveTurretCrt")] //VERIFIED
        private static class RetrieveTurretCrtPrefix
        {
            private static bool Prefix(RetrieveDeployedItemAbility __instance, PlayingAction action, ref IEnumerator<NextUpdate> __result)
            {
                __result = RetrieveTurretCrtReplacement(__instance, action);
                return false;
            }
        }

        private static IEnumerator<NextUpdate> RetrieveTurretCrtReplacement(RetrieveDeployedItemAbility ability, PlayingAction action)
        {
            TacticalActorBase deployedItemActor = ((TacticalAbilityTarget)action.Param).Actor;
            // TFTVLogger.Always($"RetrieveTurretCrtReplacement started. Deployed actor: {deployedItemActor?.ToString() ?? "null"}. Collector: {ability?.TacticalActor?.ToString() ?? "null"}.");

            Vector3 normalized = (deployedItemActor.Pos - ability.TacticalActorBase.Pos).normalized;
            if (ability.RetrieveDeployedItemAbilityDef.PlayRetrievalAnimation)
            {
                ability.Timing.Start(deployedItemActor.NavigationComponent.Face(normalized, false, false), null);
            }

            yield return ability.Timing.Call(ability.TacticalActor.TacticalNav.Face(normalized, false), null);
            yield return ability.Timing.Call(ability.DoActionAnimation(true), null);

            if (ability.RetrieveDeployedItemAbilityDef.PlayRetrievalAnimation)
            {
                deployedItemActor.OverrideDefaultActionAnimationClip(ability);
                deployedItemActor.GetAbility<PlayActionAnimationAbility>().Activate(null);
            }

            deployedItemActor.Status.UnapplyAllStatuses();
            Equipment equipment = (Equipment)deployedItemActor.Inventory.Items.FirstOrDefault<Item>();
            // TFTVLogger.Always($"RetrieveTurretCrtReplacement found equipment: {(equipment != null ? equipment.TacticalItemDef?.name ?? equipment.ToString() : "null")}.");
            if (equipment != null)
            {
                deployedItemActor.Inventory.RemoveItem(equipment);
                equipment.GetHealth().Set(deployedItemActor.GetHealth(), true);
                GiveItemToCollector(ability, equipment, ability.TacticalActor);
                // TFTVLogger.Always($"Equipment transferred to collector. Health state: {equipment.GetHealth()?.ToString() ?? "null"}.");
            }

            Weapon weapon = deployedItemActor.AddonsManager.RootAddon.OfType<Weapon>().FirstOrDefault<Weapon>();
            // TFTVLogger.Always($"RetrieveTurretCrtReplacement weapon snapshot target: {(weapon != null ? weapon.TacticalItemDef?.name ?? weapon.ToString() : "null")}. CurrentCharges={(weapon != null ? weapon.CommonItemData?.CurrentCharges.ToString() : "n/a")} ChargesMax={(weapon != null ? weapon.ChargesMax.ToString() : "n/a")}.");

            InstanceDataHolderStatus instanceDataHolderStatus = ability.TacticalActor.Status.ApplyStatus<InstanceDataHolderStatus>(
                ability.RetrieveDeployedItemAbilityDef.InstanceDataHolderStatusDef,
                equipment != null ? equipment.TacticalItemDef : null);

            TacActorInstanceData tacActorInstanceData = deployedItemActor.SerializationData as TacActorInstanceData;
            if (tacActorInstanceData != null && instanceDataHolderStatus != null)
            {
                ItemData itemData = weapon != null ? weapon.ToItemData() : null;

                tacActorInstanceData.AbilityTraits = null;
                ReplaceOrAddEquipmentItem(tacActorInstanceData, itemData);
                RetrieveTurretStateStore.Store(equipment, itemData);
                instanceDataHolderStatus.ActorInstanceData = tacActorInstanceData;

                // TFTVLogger.Always("Stored updated TacActorInstanceData on InstanceDataHolderStatus.");
            }
            else
            {
                // TFTVLogger.Always($"Skipping instance data update. tacActorInstanceData null: {tacActorInstanceData == null}, status null: {instanceDataHolderStatus == null}.");
            }

            if (ability.RetrieveDeployedItemAbilityDef.PlayRetrievalAnimation)
            {
                yield return ability.Timing.Call(ability.WaitForActionAnimationEnd(), null);
            }

            EffectTarget actorEffectTarget = TacUtil.GetActorEffectTarget(deployedItemActor, null);
            Effect.Apply(ability.Repo, ability.RetrieveDeployedItemAbilityDef.RemoveActorEffect, actorEffectTarget, ability);
            yield break;
        }

        private static void ReplaceOrAddEquipmentItem(TacActorInstanceData instanceData, ItemData itemData)
        {
            if (instanceData == null || itemData == null || itemData.ItemDef == null)
            {
                // TFTVLogger.Always($"ReplaceOrAddEquipmentItem skipped. instanceData null: {instanceData == null}, itemData null: {itemData == null}, itemData.ItemDef null: {itemData?.ItemDef == null}.");
                return;
            }

            // TFTVLogger.Always($"ReplaceOrAddEquipmentItem processing ItemDef {itemData.ItemDef?.name ?? itemData.ItemDef?.ToString()}. CurrentCharges: {itemData.Charges}.");

            // Ensure the snapshot exists where the weapon is expected to be mounted.
            ReplaceItemDataInList(ref instanceData.EquipmentItems, itemData, true);
            ReplaceItemDataInList(ref instanceData.MountedEquipmentItems, itemData, true);
            ReplaceItemDataInList(ref instanceData.OneClipMissionEquipmentItems, itemData, true);
            ReplaceItemDataInList(ref instanceData.BodypartItems, itemData, true);
            ReplaceItemDataInList(ref instanceData.InventoryItems, itemData, false);
        }

        private static void ReplaceItemDataInList(ref List<ItemData> list, ItemData source, bool allowAddWhenMissing)
        {
            if (list == null)
            {
                if (!allowAddWhenMissing)
                {
                    // TFTVLogger.Always($"[List] list is null and additions are disabled. Skipping.");
                    return;
                }

                list = new List<ItemData>();
                // TFTVLogger.Always($"[List] created new list for stored items.");
            }

            int index = list.FindIndex(i => i != null && i.ItemDef == source.ItemDef);
            if (index >= 0)
            {
                list[index] = RetrieveTurretStateStore.CloneItemData(source);
            }
            else if (allowAddWhenMissing)
            {
                list.Add(RetrieveTurretStateStore.CloneItemData(source));
            }
        }

        private static void GiveItemToCollector(RetrieveDeployedItemAbility ability, Equipment item, TacticalActor collector)
        {
            if (ability == null || item == null || collector == null)
            {
                // TFTVLogger.Always($"GiveItemToCollector skipped. ability null: {ability == null}, item null: {item == null}, collector null: {collector == null}.");
                return;
            }

            TacticalView view = collector.TacticalLevel.View;
            EquipmentComponent equipments = collector.Equipments;
            InventoryComponent inventory = collector.Inventory;
            int tacticalEquipmentSize = view.TacticalEquipmentSize;
            int readyItemCount = equipments.Items.Count(x => view.CanStayInReadyItems(x.ItemDef));
            // TFTVLogger.Always($"GiveItemToCollector ready slots: {readyItemCount}/{tacticalEquipmentSize}.");

            if (readyItemCount >= tacticalEquipmentSize)
            {
                Item itemToMove = ability.SelectedEquipment;
                if (itemToMove == null || !view.CanStayInReadyItems(itemToMove.ItemDef))
                {
                    itemToMove = equipments.Items.FirstOrDefault(x => view.CanStayInReadyItems(x.ItemDef));
                }

                if (itemToMove != null)
                {
                    equipments.RemoveItem(itemToMove);
                    inventory.AddItem(itemToMove, ability);
                    // TFTVLogger.Always($"Moved item {itemToMove?.ItemDef?.name ?? itemToMove?.ToString()} from ready slot to inventory.");
                }
            }

            equipments.AddItem(item, ability);
            equipments.SetSelectedEquipment(item);
            // TFTVLogger.Always($"Collector received item {item?.TacticalItemDef?.name ?? item?.ToString()} and set as selected.");
        }
    }
}
