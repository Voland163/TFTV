using HarmonyLib;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class InventoryDupeTacticalVanillaFixes
    {
        [HarmonyPatch(typeof(UIStateInventory), "InitListUpdateDictionary")] //VERIFIED
        private static class InitListUpdateDictionary_Patch
        {
            private static readonly AccessTools.FieldRef<UIStateInventory, Dictionary<UIInventoryList, Func<Item, InventoryComponent>>> RemoveMapRef =
                AccessTools.FieldRefAccess<UIStateInventory, Dictionary<UIInventoryList, Func<Item, InventoryComponent>>>("OnListRemoveItems");

            // We keep these getters (they exist in UIStateInventory)
            private static readonly Func<UIStateInventory, IEnumerable<InventoryComponent>> LinkedInventoriesGetter =
                AccessTools.MethodDelegate<Func<UIStateInventory, IEnumerable<InventoryComponent>>>(AccessTools.Method(typeof(UIStateInventory), "GetLinkedInventoriesOfInventoryQueries"));

            private static readonly Func<UIStateInventory, IEnumerable<ItemContainer>> ItemContainersGetter =
                AccessTools.MethodDelegate<Func<UIStateInventory, IEnumerable<ItemContainer>>>(AccessTools.Method(typeof(UIStateInventory), "GetItemContainers"));

            public static void Postfix(UIStateInventory __instance, InventoryComponent ____groundInventory)
            {
                var removeMap = RemoveMapRef(__instance);
                if (removeMap == null)
                {
                    return;
                }

                UIModuleSoldierEquip module = __instance.PrimaryActor?.TacticalLevel?.View?.TacticalModules?.TacticalSoldierEquipModule;
                UIInventoryList storageList = module?.StorageList;
                if (storageList == null)
                {
                    return;
                }

                // Critical: resolve storage removals from storage sources only (ground + item containers),
                // never from actors, to avoid "not picked up" when adds happen before removes.
                removeMap[storageList] = item => ResolveStorageRemovalTarget(__instance, item, ____groundInventory);
            }

            private static InventoryComponent ResolveStorageRemovalTarget(UIStateInventory state, Item item, InventoryComponent groundInventory)
            {
                if (item == null)
                {
                    return null;
                }

                // 1) Prefer actual storage inventories (ground + item containers) that currently contain the item in their queries
                foreach (InventoryComponent inv in EnumerateStorageInventories(state, groundInventory))
                {
                    if (InventoryQueryContainsItem(inv, item))
                    {
                        return inv;
                    }
                }

                // 2) Fallback to the item's owning inventory if it is a storage inventory (common when the item originated in a crate/ground)
                InventoryComponent owner = item.InventoryComponent;
                if (IsStorageInventory(state, owner, groundInventory))
                {
                    return owner;
                }

                // 3) Final fallback: ground if available, else owner
                return groundInventory ?? owner;
            }

            private static IEnumerable<InventoryComponent> EnumerateStorageInventories(UIStateInventory state, InventoryComponent groundInventory)
            {
                // Ground inventory (drop container or vehicle mount)
                if (groundInventory != null)
                {
                    yield return groundInventory;
                }

                // All open item containers (crates, chests, etc.)
                if (ItemContainersGetter != null)
                {
                    foreach (ItemContainer container in ItemContainersGetter(state) ?? Enumerable.Empty<ItemContainer>())
                    {
                        InventoryComponent inv = container?.Inventory;
                        if (inv != null)
                        {
                            yield return inv;
                        }
                    }
                }

                // NOTE: Do NOT enumerate linked actor inventories here.
                // This prevents removing from destination inventories when adds were processed earlier in the same frame.
            }

            private static bool IsStorageInventory(UIStateInventory state, InventoryComponent inv, InventoryComponent groundInventory)
            {
                if (inv == null)
                {
                    return false;
                }

                if (inv == groundInventory)
                {
                    return true;
                }

                if (ItemContainersGetter == null)
                {
                    return false;
                }

                foreach (ItemContainer container in ItemContainersGetter(state) ?? Enumerable.Empty<ItemContainer>())
                {
                    if (container?.Inventory == inv)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool InventoryQueryContainsItem(InventoryComponent inventory, Item item)
            {
                if (inventory == null || item == null)
                {
                    return false;
                }

                try
                {
                    InventoryQuery query = inventory.GetInventoryQuery();
                    return query != null && query.Items.Contains(item);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }
        }

    }
}
