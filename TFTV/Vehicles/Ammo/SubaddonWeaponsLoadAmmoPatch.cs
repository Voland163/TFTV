using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;

namespace TFTV.Vehicles.Ammo
{
    internal class SubaddonWeaponsLoadAmmoPatch
    {
        [HarmonyPatch(typeof(UIInventoryList), "TryLoadAmmo")]
        public static class UIInventoryListTryLoadAmmoLoggingPatch
        {
            public static bool Prefix(UIInventoryList __instance, ICommonItem item, UIInventorySlot itemSlot, UIInventoryList sourceList)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn) return true;

                    GroundVehicleModuleDef moduleDef = (item != null) ? (item.ItemDef as GroundVehicleModuleDef) : null;
                    if (moduleDef != null)
                    {
                        return TryLoadModuleAmmo(__instance, item, itemSlot, sourceList, moduleDef);
                    }

                    /*   EquipmentDef equipmentDef = (item != null) ? (item.ItemDef as EquipmentDef) : null;
                       int ammoCount = (equipmentDef != null && equipmentDef.CompatibleAmmunition != null)
                           ? equipmentDef.CompatibleAmmunition.Count()
                           : -1;

                       string message = string.Format(
                           "[TryLoadAmmo] list={0} item={1} itemDef={2} itemDefType={3} ammoCount={4} ammoManager={5} charges={6}/{7} slot={8} sourceList={9} sourceStacking={10} sourceSlots={11} sourceItems={12}",
                           GetObjectName(__instance),
                           GetItemName(item),
                           GetItemDefName(item),
                           (item != null && item.ItemDef != null) ? item.ItemDef.GetType().FullName : "null",
                           ammoCount,
                           (item != null && item.CommonItemData != null && item.CommonItemData.Ammo != null) ? "present" : "null",
                           (item != null && item.CommonItemData != null && item.CommonItemData.Ammo != null) ? item.CommonItemData.Ammo.CurrentCharges.ToString() : "null",
                           (item != null && item.ItemDef != null) ? item.ItemDef.ChargesMax.ToString() : "null",
                           GetObjectName(itemSlot),
                           GetObjectName(sourceList),
                           (sourceList != null) ? sourceList.AllowStacking.ToString() : "null",
                           (sourceList != null && sourceList.Slots != null) ? sourceList.Slots.Count.ToString() : "null",
                           (sourceList != null && sourceList.UnfilteredItems != null) ? sourceList.UnfilteredItems.Count.ToString() : "null");

                       TFTVLogger.Always(message);*/
                    return true;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Always("[TryLoadAmmo] Logging failed.");
                    TFTVLogger.Error(ex);
                    return true;
                }
            }

            private static bool TryLoadModuleAmmo(UIInventoryList list, ICommonItem item, UIInventorySlot itemSlot, UIInventoryList sourceList, GroundVehicleModuleDef moduleDef)
            {
                if (list == null || item == null || moduleDef == null || item.CommonItemData == null || item.CommonItemData.Ammo == null)
                {
                    return false;
                }

                if (sourceList == null || !sourceList.AllowStacking)
                {
                    return false;
                }

                List<WeaponDef> subWeapons = moduleDef.GetSubWeapons();
                if (subWeapons == null || subWeapons.Count == 0)
                {
                    return false;
                }

                HashSet<TacticalItemDef> ammoDefs = new HashSet<TacticalItemDef>();
                foreach (WeaponDef weaponDef in subWeapons)
                {
                    if (weaponDef == null || weaponDef.CompatibleAmmunition == null)
                    {
                        continue;
                    }

                    foreach (TacticalItemDef ammoDef in weaponDef.CompatibleAmmunition)
                    {
                        if (ammoDef != null)
                        {
                            ammoDefs.Add(ammoDef);
                        }
                    }
                }

                if (ammoDefs.Count == 0)
                {
                    return false;
                }

                foreach (TacticalItemDef ammoDef in ammoDefs)
                {
                    foreach (UIInventorySlot slot in sourceList.Slots)
                    {
                        if (slot.Empty || slot.Item == null || slot.Item.ItemDef != ammoDef)
                        {
                            continue;
                        }

                        if (list.TryLoadItemWithItem(item, slot.Item, slot))
                        {
                            if (itemSlot != null)
                            {
                                itemSlot.UpdateItem();
                            }
                            return false;
                        }
                    }

                    for (int i = 0; i < sourceList.UnfilteredItems.Count; i++)
                    {
                        ICommonItem commonItem = sourceList.UnfilteredItems[i];
                        if (commonItem == null || commonItem.ItemDef != ammoDef)
                        {
                            continue;
                        }

                        if (list.TryLoadItemWithItem(item, commonItem, null))
                        {
                            if (itemSlot != null)
                            {
                                itemSlot.UpdateItem();
                            }

                            if (commonItem.CommonItemData.IsEmpty())
                            {
                                sourceList.UnfilteredItems.RemoveAt(i);
                            }
                            return false;
                        }
                    }
                }

                return false;
            }

            private static string GetItemName(ICommonItem item)
            {
                if (item == null)
                {
                    return "null";
                }
                if (item.ItemDef != null)
                {
                    return item.ItemDef.name;
                }
                return item.GetType().Name;
            }

            private static string GetItemDefName(ICommonItem item)
            {
                if (item == null || item.ItemDef == null)
                {
                    return "null";
                }
                return item.ItemDef.name;
            }

            private static string GetObjectName(UnityEngine.Object obj)
            {
                return (obj != null) ? obj.name : "null";
            }
        }

    }
}
