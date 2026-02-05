using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using TFTV.Vehicles.Ammo;
using UnityEngine;

namespace TFTV.LaserWeapons
{
    [HarmonyPatch]
    internal static class LaserWeaponsReplenishPatches
    {
        private static bool TryGetLaserEntry(GeoItem geoItem, out LaserWeaponsMain.LaserAmmoShareHelper.WeaponEntry entry)
        {
            entry = null;

            WeaponDef weaponDef = geoItem?.ItemDef as WeaponDef;
            if (weaponDef == null)
            {
                return false;
            }

            return LaserWeaponsMain.LaserAmmoShareHelper.TryGetEntry(weaponDef, out entry);
        }

        private static int GetMissingShots(GeoItem geoItem)
        {
            if (geoItem?.CommonItemData == null || geoItem.ItemDef == null)
            {
                return 0;
            }

            return Mathf.Max(0, geoItem.ItemDef.ChargesMax - geoItem.CommonItemData.CurrentCharges);
        }

        private static int GetChargesToSpendCapped(int missingShots, LaserWeaponsMain.LaserAmmoShareHelper.WeaponEntry entry)
        {
            if (entry == null || missingShots <= 0)
            {
                return 0;
            }

            int perCharge = Mathf.Max(1, entry.ShotsPerCharge);
            int required = Mathf.CeilToInt(missingShots / (float)perCharge);

            // Cap payment per replenish click.
            return Mathf.Clamp(required, 0, Mathf.Max(0, entry.ReloadCost));
        }

        private static ResourcePack GetBatteryPriceForCharges(int chargesToSpend)
        {
            if (chargesToSpend <= 0 || LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef == null)
            {
                return new ResourcePack();
            }

            TacticalItemDef battery = LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef;
            int max = Mathf.Max(1, battery.ChargesMax);

            // Price per charge, rounded up so we don't undercharge.
            int mats = Mathf.CeilToInt(battery.ManufactureMaterials * (chargesToSpend / (float)max));
            int tech = Mathf.CeilToInt(battery.ManufactureTech * (chargesToSpend / (float)max));

            return new ResourcePack
            {
                new ResourceUnit(ResourceType.Materials, mats),
                new ResourceUnit(ResourceType.Tech, tech)
            };
        }

        private static void TopUpPartialMagazines(IEnumerable<PostmissionReplenishManager.ReplenishableItems> missingItems)
        {
            if (missingItems == null)
            {
                return;
            }

            foreach (PostmissionReplenishManager.ReplenishableItems replenishableItems in missingItems)
            {
                if (replenishableItems?.ReloadableItems == null)
                {
                    continue;
                }

                foreach (GeoItem geoItem in replenishableItems.ReloadableItems)
                {
                    if (!TryGetLaserEntry(geoItem, out var entry) || geoItem?.CommonItemData == null)
                    {
                        continue;
                    }

                    int perCharge = Mathf.Max(1, entry.ShotsPerCharge);
                    int current = Mathf.Max(0, geoItem.CommonItemData.CurrentCharges);
                    int max = Mathf.Max(0, geoItem.ItemDef.ChargesMax);

                    if (current <= 0 || current >= max)
                    {
                        continue;
                    }

                    int remainder = current % perCharge;
                    if (remainder == 0)
                    {
                        continue;
                    }

                    int topUp = Mathf.Min(max - current, perCharge - remainder);
                    if (topUp <= 0)
                    {
                        continue;
                    }

                    geoItem.CommonItemData.ModifyCharges(topUp, canCreateMagazines: true);
                    LaserWeaponsMain.LaserAmmoShareHelper.Log($"Post-mission top-up: '{geoItem.ItemDef.name}' {current}/{max} -> {current + topUp}/{max}");
                }
            }
        }

      
        [HarmonyPatch(typeof(UIModuleReplenish), nameof(UIModuleReplenish.Init))]
        private static class UIModuleReplenish_Init_LaserBatteryTopUp_Patch
        {
            private static void Prefix(List<PostmissionReplenishManager.ReplenishableItems> missingItems)
            {
                try
                {
                    if (LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef != null)
                    {
                        TopUpPartialMagazines(missingItems);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleReplenish), "AddMissingAmmo")]
        private static class UIModuleReplenish_AddMissingAmmo_LaserBattery_Patch
        {
            public static bool Prefix(
                UIModuleReplenish __instance,
                GeoCharacter character,
                GeoItem item,
                ref int materialsCost,
                ref int techCost,
                ref bool __result)
            {
                try
                {
                    if (!TryGetLaserEntry(item, out var entry))
                    {
                        return true;
                    }

                    int missingShots = GetMissingShots(item);
                    if (missingShots <= 0)
                    {
                        __result = false;
                        return false;
                    }

                    int chargesToSpend = GetChargesToSpendCapped(missingShots, entry);
                    if (chargesToSpend <= 0)
                    {
                        __result = false;
                        return false;
                    }

                    ResourcePack cost = GetBatteryPriceForCharges(chargesToSpend);

                    GeoPhoenixFaction faction = AccessTools.Field(typeof(UIModuleReplenish), "_faction")?.GetValue(__instance) as GeoPhoenixFaction;
                    if (faction == null)
                    {
                        __result = false;
                        return false;
                    }

                    GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;
                    TacticalItemDef battery = LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef;

                    bool canPay =
                        battery != null &&
                        battery.Tags.Contains(manufacturableTag) &&
                        faction.Manufacture.Contains(battery) &&
                        faction.Wallet.HasResources(cost);

                    // Build the UI element but force it to use our computed cost and enablement.
                    GeoManufactureItem ui = UnityEngine.Object.Instantiate(__instance.ItemListPrefab, __instance.ItemListContainer);
                    ui.OnEnter += (InteractHandler)Delegate.CreateDelegate(typeof(InteractHandler), __instance, AccessTools.Method(typeof(UIModuleReplenish), "OnEnterSlot"));
                    ui.OnExit += (InteractHandler)Delegate.CreateDelegate(typeof(InteractHandler), __instance, AccessTools.Method(typeof(UIModuleReplenish), "OnExitSlot"));
                    ui.OnSelected += (Action<GeoManufactureItem>)Delegate.CreateDelegate(typeof(Action<GeoManufactureItem>), __instance, AccessTools.Method(typeof(UIModuleReplenish), "SingleItemReloadAndRefresh"));

                    // Show battery as the replenished "thing", but priced correctly.
                    ui.Init(battery, faction, cost, repairMode: false);
                    ui.CanCraftQuantityText.transform.parent.gameObject.SetActive(false);

                    ReplenishmentElementController.CreateAndAdd(ui.gameObject, ReplenishmentType.Reload, character, item.ItemDef, item);

                    PhoenixGeneralButton btn = ui.AddToQueueButton.GetComponent<PhoenixGeneralButton>();
                    if (btn != null)
                    {
                        btn.SetEnabled(canPay);
                    }
                    ui.AddToQueueButton.SetInteractable(canPay);

                    __instance.Items.Add(ui);

                    if (canPay)
                    {
                        materialsCost += cost.ByResourceType(ResourceType.Materials).RoundedValue;
                        techCost += cost.ByResourceType(ResourceType.Tech).RoundedValue;
                    }

                    __result = canPay;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleReplenish), "SingleItemReload")]
        private static class UIModuleReplenish_SingleItemReload_LaserBattery_Patch
        {
            public static bool Prefix(UIModuleReplenish __instance, GeoItem geoItem, ref bool __result)
            {
                try
                {
                    if (!TryGetLaserEntry(geoItem, out var entry))
                    {
                        return true;
                    }

                    GeoPhoenixFaction faction = AccessTools.Field(typeof(UIModuleReplenish), "_faction")?.GetValue(__instance) as GeoPhoenixFaction;
                    if (faction == null)
                    {
                        __result = false;
                        return false;
                    }

                    int missingShots = GetMissingShots(geoItem);
                    if (missingShots <= 0)
                    {
                        __result = true;
                        return false;
                    }

                    int chargesToSpend = GetChargesToSpendCapped(missingShots, entry);
                    if (chargesToSpend <= 0)
                    {
                        __result = false;
                        return false;
                    }

                    ResourcePack cost = GetBatteryPriceForCharges(chargesToSpend);

                    TacticalItemDef battery = LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef;
                    GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;

                    if (battery == null ||
                        !battery.Tags.Contains(manufacturableTag) ||
                        !faction.Manufacture.Contains(battery) ||
                        !faction.Wallet.HasResources(cost))
                    {
                        __result = false;
                        return false;
                    }

                    faction.Wallet.Take(cost, OperationReason.Purchase);

                    int shotsToAdd = Mathf.Min(missingShots, chargesToSpend * Mathf.Max(1, entry.ShotsPerCharge));
                    geoItem.CommonItemData.ModifyCharges(shotsToAdd, canCreateMagazines: true);

                    __result = true;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}