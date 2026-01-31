using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV.LaserWeapons
{
    internal static class LaserWeaponsMain
    {
        internal static class LaserAmmoShareHelper
        {
            private const string LogPrefix = "[LaserWeapons]";

            internal static TacticalItemDef BatteryPackDef;

            internal sealed class WeaponEntry
            {
                internal WeaponDef WeaponDef;
                internal TacticalItemDef OriginalAmmoDef;
                internal int ReloadCost;
                internal int MagazineSize;

                internal int ShotsPerCharge => Mathf.Max(1, Mathf.CeilToInt(MagazineSize / (float)Math.Max(1, ReloadCost)));
            }

            private static readonly Dictionary<WeaponDef, WeaponEntry> WeaponEntries = new Dictionary<WeaponDef, WeaponEntry>();

            internal static void Log(string message)
            {
                TFTVLogger.Always($"{LogPrefix} {message}");
            }

            internal static void RegisterWeapon(WeaponDef weaponDef, TacticalItemDef originalAmmoDef, int reloadCost)
            {
                if (weaponDef == null || originalAmmoDef == null || reloadCost <= 0)
                {
                    Log($"Skipping registration for '{weaponDef?.name ?? "<null>"}' (ammo={originalAmmoDef?.name ?? "<null>"}, cost={reloadCost})");
                    return;
                }

                var entry = new WeaponEntry
                {
                    WeaponDef = weaponDef,
                    OriginalAmmoDef = originalAmmoDef,
                    ReloadCost = reloadCost,
                    // IMPORTANT: base per-charge math on the weapon's current magazine size, not the original ammo def.
                    MagazineSize = Mathf.Max(1, weaponDef.ChargesMax)
                };

                WeaponEntries[weaponDef] = entry;

                //  Log($"Registered {weaponDef.name}: cost={reloadCost}, magazine={entry.MagazineSize}, perCharge={entry.ShotsPerCharge}");
            }

            internal static bool TryGetEntry(WeaponDef weaponDef, out WeaponEntry entry)
            {
                entry = null;
                return weaponDef != null && WeaponEntries.TryGetValue(weaponDef, out entry);
            }

            private static IEnumerable<ICommonItem> EnumerateActorBatteryItems(TacticalActor actor)
            {
                if (actor == null || BatteryPackDef == null)
                {
                    yield break;
                }

                if (actor.Equipments != null)
                {
                    foreach (Item equipment in actor.Equipments.Items)
                    {
                        if (equipment is ICommonItem common && common.ItemDef == BatteryPackDef)
                        {
                            yield return common;
                        }
                    }
                }

                if (actor.Inventory != null)
                {
                    foreach (Item inventoryItem in actor.Inventory.Items)
                    {
                        if (inventoryItem is ICommonItem common && common.ItemDef == BatteryPackDef)
                        {
                            yield return common;
                        }
                    }
                }
            }

            private static int GetBatteryCharges(ICommonItem item)
            {
                return item?.CommonItemData != null ? Math.Max(0, item.CommonItemData.CurrentCharges) : 0;
            }

            internal static int CountAvailableCharges(TacticalActor actor)
            {
                if (actor == null)
                {
                    return 0;
                }

                int total = 0;
                foreach (ICommonItem item in EnumerateActorBatteryItems(actor))
                {
                    total += GetBatteryCharges(item);
                }

                Log($"Actor '{actor.name}' has {total} battery charge(s) available");
                return total;
            }

            private static int ConsumeFromItem(ICommonItem item, int amount)
            {
                if (item == null || item.ItemDef != BatteryPackDef || amount <= 0)
                {
                    return 0;
                }

                CommonItemData data = item.CommonItemData;
                if (data == null)
                {
                    return 0;
                }

                int current = Math.Max(0, data.CurrentCharges);
                int take = Math.Min(current, amount);
                if (take > 0)
                {
                    Log($"Consuming {take} charge(s) from '{item.ItemDef.name}' (before={current})");
                    data.ModifyCharges(-take, false);
                }

                return take;
            }

            internal static int ConsumeCharges(TacticalActor actor, ICommonItem preferredItem, int chargesToUse)
            {
                if (chargesToUse <= 0)
                {
                    return 0;
                }

                int consumed = 0;

                if (preferredItem != null && preferredItem.ItemDef == BatteryPackDef)
                {
                    consumed += ConsumeFromItem(preferredItem, chargesToUse);
                }

                if (consumed >= chargesToUse)
                {
                    Log($"Satisfied consumption from preferred item ({consumed}/{chargesToUse})");
                    return consumed;
                }

                if (actor != null)
                {
                    foreach (ICommonItem item in EnumerateActorBatteryItems(actor))
                    {
                        if (ReferenceEquals(item, preferredItem))
                        {
                            continue;
                        }

                        consumed += ConsumeFromItem(item, chargesToUse - consumed);
                        if (consumed >= chargesToUse)
                        {
                            break;
                        }
                    }
                }

                if (consumed < chargesToUse)
                {
                    Log($"Requested {chargesToUse} charge(s) but only consumed {consumed}");
                }

                return consumed;
            }

            private static TacticalItem CreateMagazine(WeaponEntry entry, int shotsToAdd)
            {
                TacticalItem magazine = new TacticalItem();
                magazine.Init(entry.OriginalAmmoDef, null);
                CommonItemData magazineData = magazine.CommonItemData;
                magazineData.ModifyCharges(-magazineData.CurrentCharges, false);
                magazineData.ModifyCharges(Mathf.Clamp(shotsToAdd, 0, entry.MagazineSize), false);
                return magazine;
            }

            internal static bool TryHandleTacticalReload(ReloadAbility ability, Equipment equipment, TacticalItem ammoClip)
            {
                WeaponDef weaponDef = equipment?.ItemDef as WeaponDef;
                if (ammoClip == null || ammoClip.ItemDef != BatteryPackDef || !TryGetEntry(weaponDef, out WeaponEntry entry))
                {
                    return false;
                }

                TacticalActor actor = equipment?.TacticalActor ?? ammoClip?.TacticalActor ?? ability?.TacticalActor;
                CommonItemData weaponData = equipment?.CommonItemData;
                if (weaponData == null)
                {
                    Log($"No CommonItemData for tactical reload on '{weaponDef?.name ?? "<null>"}'");
                    return true;
                }

                int missing = Math.Max(0, equipment.ChargesMax - weaponData.CurrentCharges);
                if (missing <= 0)
                {
                    Log($"'{weaponDef.name}' is already fully loaded ({weaponData.CurrentCharges}/{equipment.ChargesMax})");
                    return true;
                }

                int available = CountAvailableCharges(actor);
                if (available <= 0)
                {
                    Log($"No battery charges available for '{weaponDef.name}' (tactical)");
                    return true;
                }

                int perCharge = entry.ShotsPerCharge;
                int required = Mathf.CeilToInt(missing / (float)perCharge);
                int chargesToSpend = Math.Min(entry.ReloadCost, Math.Min(available, required));
                if (chargesToSpend <= 0)
                {
                    Log($"Unable to compute tactical charges for '{weaponDef.name}' (missing={missing}, perCharge={perCharge}, available={available})");
                    return true;
                }

                int shotsToAdd = Math.Min(missing, chargesToSpend * perCharge);
                Log($"Tactical reload '{weaponDef.name}': missing={missing}, available={available}, perCharge={perCharge}, chargesToSpend={chargesToSpend}, shotsToAdd={shotsToAdd}");

                TacticalItem magazine = CreateMagazine(entry, shotsToAdd);
                weaponData.Ammo.LoadMagazine(magazine);

                int consumed = ConsumeCharges(actor, ammoClip, chargesToSpend);
                Log($"Spent {consumed}/{chargesToSpend} battery charge(s) for '{weaponDef.name}' (tactical)");
                return true;
            }

            internal static bool TryHandleGeoReload(UIInventoryList list, ICommonItem item, ICommonItem ammoItem, UIInventorySlot ammoSlot, out bool result)
            {
                result = false;
                WeaponDef weaponDef = item?.ItemDef as WeaponDef;
                if (ammoItem == null || ammoItem.ItemDef != BatteryPackDef || !TryGetEntry(weaponDef, out WeaponEntry entry))
                {
                    return false;
                }

                CommonItemData weaponData = item.CommonItemData;
                if (weaponData == null)
                {
                    Log($"No CommonItemData for geoscape reload on '{weaponDef?.name ?? "<null>"}'");
                    return true;
                }

                int missing = Math.Max(0, item.ItemDef.ChargesMax - weaponData.CurrentCharges);
                if (missing <= 0)
                {
                    Log($"'{weaponDef.name}' is already fully loaded on geoscape");
                    result = true;
                    return true;
                }

                CommonItemData ammoData = ammoItem.CommonItemData;
                if (ammoData == null)
                {
                    Log("Selected battery has no CommonItemData (geoscape)");
                    return true;
                }

                int available = Math.Max(0, ammoData.CurrentCharges);
                if (available <= 0)
                {
                    Log($"Selected battery has no charges for '{weaponDef.name}' (geoscape)");
                    return true;
                }

                int perCharge = entry.ShotsPerCharge;
                int required = Mathf.CeilToInt(missing / (float)perCharge);
                int chargesToSpend = Math.Min(entry.ReloadCost, Math.Min(available, required));
                if (chargesToSpend <= 0)
                {
                    Log($"Unable to compute geoscape charges for '{weaponDef.name}' (missing={missing}, perCharge={perCharge}, available={available})");
                    return true;
                }

                int shotsToAdd = Math.Min(missing, chargesToSpend * perCharge);
                Log($"Geoscape reload '{weaponDef.name}': missing={missing}, available={available}, perCharge={perCharge}, chargesToSpend={chargesToSpend}, shotsToAdd={shotsToAdd}");

                int before = weaponData.CurrentCharges;

                ICommonItem magazine = item.Create(entry.OriginalAmmoDef);
                if (magazine?.CommonItemData != null)
                {
                    magazine.CommonItemData.ModifyCharges(-magazine.CommonItemData.CurrentCharges, false);
                    magazine.CommonItemData.ModifyCharges(shotsToAdd, false);
                    weaponData.Ammo.LoadMagazine(magazine);
                }

                Log($"Geoscape reload '{weaponDef.name}' applied: before={before}, now={weaponData.CurrentCharges}, desired={Mathf.Min(item.ItemDef.ChargesMax, before + shotsToAdd)}");

                ammoData.ModifyCharges(-chargesToSpend, false);
                if (ammoData.IsEmpty())
                {
                    list.RemoveItem(ammoItem, ammoSlot);
                }

                list.OnItemLoaded?.Invoke();
                result = true;
                return true;
            }

        }

        [HarmonyPatch(typeof(CommonItemData), nameof(CommonItemData.GetFullMagazinesCount))]
        private static class CommonItemData_GetFullMagazinesCount_Patch
        {
            private static bool Prefix(CommonItemData __instance, ref int __result)
            {
                if (__instance?.ItemDef is WeaponDef weaponDef && LaserAmmoShareHelper.TryGetEntry(weaponDef, out _))
                {
                    int count = __instance.Count;
                    __result = Math.Max(0, count - 1);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ReloadAbility), "Reload")] //VERIFIED
        private static class TacticalReloadPatch
        {
            private static bool Prefix(ReloadAbility __instance, Equipment equipment, TacticalItem ammoClip)
            {
                if (LaserAmmoShareHelper.BatteryPackDef == null)
                {
                    return true;
                }

                if (LaserAmmoShareHelper.TryHandleTacticalReload(__instance, equipment, ammoClip))
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), nameof(UIInventoryList.TryLoadItemWithItem))]
        private static class GeoscapeReloadPatch
        {
            private static bool Prefix(UIInventoryList __instance, ICommonItem item, ICommonItem ammoItem, UIInventorySlot ammoSlot, ref bool __result)
            {
                if (LaserAmmoShareHelper.BatteryPackDef == null)
                {
                    return true;
                }

                if (LaserAmmoShareHelper.TryHandleGeoReload(__instance, item, ammoItem, ammoSlot, out bool handled))
                {
                    __result = handled;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(AmmoManager), nameof(AmmoManager.UnloadMagazines))]
        private static class AmmoManager_UnloadMagazines_Patch
        {
            private static void Postfix(AmmoManager __instance, ref List<ICommonItem> __result)
            {
                if (LaserAmmoShareHelper.BatteryPackDef == null || __instance?.ParentItem == null)
                {
                    return;
                }

                if (!(__instance.ParentItem.ItemDef is WeaponDef weaponDef) || !LaserAmmoShareHelper.TryGetEntry(weaponDef, out var entry))
                {
                    return;
                }

                int totalShots = 0;
                foreach (ICommonItem magazine in __result)
                {
                    if (magazine?.CommonItemData == null)
                    {
                        continue;
                    }

                    if (magazine.ItemDef == entry.OriginalAmmoDef)
                    {
                        totalShots += Math.Max(0, magazine.CommonItemData.TotalCharges);
                    }
                    else if (magazine.ItemDef == LaserAmmoShareHelper.BatteryPackDef)
                    {
                        totalShots += Math.Max(0, magazine.CommonItemData.TotalCharges);
                    }
                }

                if (totalShots <= 0)
                {
                    __result = new List<ICommonItem>();
                    return;
                }

                int charges = Mathf.CeilToInt(totalShots / (float)entry.ShotsPerCharge);
                var converted = new List<ICommonItem>();
                int remaining = charges;
                while (remaining > 0)
                {
                    int chunk = Math.Min(remaining, Mathf.Max(1, LaserAmmoShareHelper.BatteryPackDef.ChargesMax));
                    ICommonItem battery = __instance.ParentItem.Create(LaserAmmoShareHelper.BatteryPackDef);
                    if (battery?.CommonItemData == null)
                    {
                        continue;
                    }

                    battery.CommonItemData.ModifyCharges(-battery.CommonItemData.CurrentCharges, false);
                    battery.CommonItemData.ModifyCharges(chunk, false);
                    converted.Add(battery);
                    remaining -= chunk;
                }

                __result = converted;
            }
        }

        [HarmonyPatch(typeof(UIInventorySlot), nameof(UIInventorySlot.UpdateItem))]
        private static class UIInventorySlot_UpdateItem_Patch
        {
            private static void Postfix(UIInventorySlot __instance)
            {
                if (LaserAmmoShareHelper.BatteryPackDef == null)
                {
                    return;
                }

                ICommonItem item = __instance?.Item;
                if (item?.ItemDef == LaserAmmoShareHelper.BatteryPackDef || item?.ItemDef.name == "junkerMinigun_AmmoClipDef")
                {
                    __instance.AmmoImageNode.gameObject.SetActive(false);
                }


            }
        }

    }
}