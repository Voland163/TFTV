using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TFTV.Vehicles.Ammo.VehicleModuleAmmoHarmonyPatches;

namespace TFTV.Vehicles.Ammo
{
    internal class MissionEndReplenish
    {
        private sealed class AmmoDefHolder
        {
            public TacticalItemDef AmmoDef;
        }

        private static readonly ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder> ReplenishAmmoDefs = new ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder>();

        // Vanilla CanManufacture crashes (NRE on ManufacturableItem.ManufacturePrice) for any
        // ItemDef absent from the faction's manufacture list, because AddMissingItem and
        // ReplenishList both set ManufacturableItem = GetManufacturableItemByDef(def) without a
        // null guard before calling CanManufacture. This affects GroundVehicleModuleDef items AND
        // vehicle-specific ammo clips (e.g. hailstorm_AmmoClipDef) that end up in MissingInventory.
        // Fix: strip all non-manufacturable items from every missing-items list at the source, and
        // prune entries that become empty (prevents a ghost soldier header in the replenish UI).
        [HarmonyPatch]
        public static class PostmissionReplenishManager_GetMissingItems_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            static MethodBase TargetMethod()
            {
                return typeof(PostmissionReplenishManager).GetMethod(
                    "GetMissingItems",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
            }

            public static void Postfix(List<PostmissionReplenishManager.ReplenishableItems> __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                        return;

                    if (__result == null)
                        return;

                    GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    GeoPhoenixFaction phoenixFaction = controller?.PhoenixFaction;

                    if (phoenixFaction == null)
                        TFTVLogger.Always($"[ReplenishFix] PhoenixFaction unavailable; falling back to GroundVehicleModuleDef-only filter.");

                    // Runs on every refresh of the replenish screen, so it reports what it changed and
                    // nothing else - a per-character, per-item trace here buried the log.
                    int strippedTotal = 0;
                    int emptiedEntries = 0;

                    for (int i = __result.Count - 1; i >= 0; i--)
                    {
                        PostmissionReplenishManager.ReplenishableItems replenishable = __result[i];
                        if (replenishable == null)
                            continue;

                        int removed;
                        if (phoenixFaction != null)
                        {
                            removed = replenishable.MissingEquipmentItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                            removed += replenishable.MissingInventoryItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                            removed += replenishable.MissingArmourItems?.RemoveAll(def => phoenixFaction.Manufacture.GetManufacturableItemByDef(def) == null) ?? 0;
                        }
                        else
                        {
                            // Faction unavailable: narrow fallback keeps the game safe for module defs.
                            removed = replenishable.MissingEquipmentItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                            removed += replenishable.MissingInventoryItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                            removed += replenishable.MissingArmourItems?.RemoveAll(def => def is GroundVehicleModuleDef) ?? 0;
                        }

                        strippedTotal += removed;

                        // If nothing remains for this character, remove the entry entirely so no
                        // ghost soldier header appears in the replenish UI.
                        if (replenishable.IsEmpty())
                        {
                            emptiedEntries++;
                            __result.RemoveAt(i);
                        }
                    }

                    if (strippedTotal > 0 || emptiedEntries > 0)
                    {
                        TFTVLogger.Always($"[ReplenishFix] Stripped {strippedTotal} non-manufacturable item(s); " +
                            $"removed {emptiedEntries} now-empty entr(y/ies).");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // Final safety net: if anything slips through the Postfix filter, block it here rather
        // than letting vanilla CanManufacture crash on a null ManufacturableItem.
        [HarmonyPatch]
        public static class UIModuleReplenish_AddMissingItem_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoPhoenixFaction> FactionRef =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoPhoenixFaction>("_faction");

            static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UIModuleReplenish), "AddMissingItem");

            public static bool Prefix(UIModuleReplenish __instance, GeoCharacter character, ItemDef def, ref bool __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                        return true;

                    GeoPhoenixFaction faction = FactionRef(__instance);
                    if (faction != null && faction.Manufacture.GetManufacturableItemByDef(def) == null)
                    {
                        // A genuine safety net: reaching here means the postfix filter above missed
                        // something, which is worth knowing about, and it should be rare.
                        TFTVLogger.Always($"[ReplenishFix] AddMissingItem safety-net blocked '{def?.name}' ({def?.GetType()?.Name})" +
                            $" for '{character?.DisplayName}' - not in faction.Manufacture.");
                        __result = false;
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        // Intercept TryReloadItem for GroundVehicleModuleDef items, which vanilla does not handle.
        // Vanilla's non-equipment branch looks for the module def itself in storage (wrong),
        // and the equipment branch (CompatibleAmmunition) is only reached for EquipmentDef.
        // We handle each sub-weapon ammo def independently using our existing helpers.
        [HarmonyPatch(typeof(GeoMission), "TryReloadItem")]
        public static class GeoMission_TryReloadItem_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static bool Prefix(GeoItem item, ItemStorage storage, string storageName, ref bool __result)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    return true;

                GroundVehicleModuleDef moduleDef = item?.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null)
                    return true;

                if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    __result = true;
                    return false;
                }

                AmmoDiagnostics.Trace("PostMission", $"Reloading from {storageName}: {AmmoDiagnostics.DescribeAmmo(item)}");

                bool allFull = true;
                foreach (TacticalItemDef ammoDef in GetModuleAmmoDefs(moduleDef))
                {
                    int maxCharges = GetAmmoCapacityForDef(moduleDef, ammoDef);
                    if (maxCharges <= 0)
                        continue;

                    int currentCharges = GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                    if (currentCharges >= maxCharges)
                        continue;

                    if (!storage.Items.ContainsKey(ammoDef))
                    {
                        AmmoDiagnostics.Trace("PostMission",
                            $"{ammoDef.name} short at {currentCharges}/{maxCharges} but none in {storageName}; left as is.");
                        Debug.Log($"POSTMISSION RELOAD: Trying to reload {item} ({ammoDef.name}) but there is no ammo available in {storageName}!");
                        allFull = false;
                        continue;
                    }

                    GeoItem storageAmmo = storage.Items[ammoDef];
                    int clipSize = ammoDef.ChargesMax;

                    while (!storageAmmo.CommonItemData.IsEmpty())
                    {
                        int needed = maxCharges - GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                        if (needed <= 0)
                            break;

                        int toLoad = Mathf.Min(needed, clipSize);
                        GeoItem clip = new GeoItem(ammoDef, 1, -1, null, -100);
                        // Zero out and set to exactly toLoad charges (handles partial last clip)
                        clip.CommonItemData.ModifyCharges(-clip.CommonItemData.CurrentCharges, false);
                        clip.CommonItemData.ModifyCharges(toLoad, false);
                        item.CommonItemData.Ammo.LoadMagazine(clip);
                        storageAmmo.CommonItemData.Subtract(clip);
                    }

                    int finalCharges = GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                    AmmoDiagnostics.Trace("PostMission", $"{ammoDef.name} now {finalCharges}/{maxCharges} from {storageName}.");
                    Debug.Log($"POSTMISSION RELOAD: Reloaded {item} ({ammoDef.name}) now at {finalCharges}/{maxCharges} from {storageName}!");

                    if (storageAmmo.CommonItemData.IsEmpty())
                    {
                        storage.RemoveItem(storageAmmo);
                        Debug.Log($"POSTMISSION RELOAD: No more {ammoDef.name} left in {storageName}!");
                    }

                    if (finalCharges < maxCharges)
                        allFull = false;
                }

                __result = allFull;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIModuleReplenish), "AddMissingAmmo")]
        public static class UIModuleReplenish_AddMissingAmmo_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoscapeViewContext> ReplenishContext =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoscapeViewContext>("_context");
            private static readonly AccessTools.FieldRef<UIModuleReplenish, GeoPhoenixFaction> ReplenishFaction =
                AccessTools.FieldRefAccess<UIModuleReplenish, GeoPhoenixFaction>("_faction");

            private static InteractHandler GetInteractHandler(UIModuleReplenish instance, string methodName)
            {
                var methodInfo = AccessTools.Method(typeof(UIModuleReplenish), methodName);
                if (methodInfo == null)
                {
                    return null;
                }
                return (InteractHandler)Delegate.CreateDelegate(typeof(InteractHandler), instance, methodInfo);
            }

            private static Action<GeoManufactureItem> GetManufactureItemHandler(UIModuleReplenish instance, string methodName)
            {
                var methodInfo = AccessTools.Method(typeof(UIModuleReplenish), methodName);
                if (methodInfo == null)
                {
                    return null;
                }
                return (Action<GeoManufactureItem>)Delegate.CreateDelegate(typeof(Action<GeoManufactureItem>), instance, methodInfo);
            }

            public static bool Prefix(UIModuleReplenish __instance, GeoCharacter character, GeoItem item, ref int materialsCost, ref int techCost, ref bool __result)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                var moduleDef = (item != null) ? (item.ItemDef as GroundVehicleModuleDef) : null;
                if (moduleDef == null)
                {
                    return true;
                }

                if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    __result = false;
                    return false;
                }
                bool flag = false;
                foreach (TacticalItemDef tacticalItemDef in GetModuleAmmoDefs(moduleDef))
                {
                    int maxCharges = GetAmmoCapacityForDef(moduleDef, tacticalItemDef);
                    if (maxCharges <= 0)
                    {
                        continue;
                    }
                    int currentCharges = GetAmmoChargesForDef(item.CommonItemData, tacticalItemDef);

                    if (currentCharges >= maxCharges)
                    {
                        continue;
                    }
                    float num = (float)currentCharges / (float)maxCharges;

                    // Buy-only ammunition is priced at a whole magazine from the marketplace; anything
                    // the player can manufacture keeps vanilla's proportional repair cost.
                    bool marketplaceOnly = IsMarketplaceOnlyAmmo(tacticalItemDef);
                    ResourcePack marketplaceCost = null;
                    bool magazineOnSale = marketplaceOnly &&
                        CanBuyMagazine(ReplenishFaction(__instance), tacticalItemDef, out marketplaceCost);

                    ResourcePack repairCost = marketplaceOnly
                        ? (marketplaceCost ?? MarketplacePrice(0))
                        : GeoCharacter.GetRepairCost(tacticalItemDef, num);

                    GeoManufactureItem geoManufactureItem = UnityEngine.Object.Instantiate<GeoManufactureItem>(__instance.ItemListPrefab, __instance.ItemListContainer);
                    GeoManufactureItem geoManufactureItem2 = geoManufactureItem;
                    InteractHandler interactHandler = GetInteractHandler(__instance, "OnEnterSlot");
                    if (interactHandler != null)
                    {
                        geoManufactureItem2.OnEnter = (InteractHandler)Delegate.Combine(geoManufactureItem2.OnEnter, interactHandler);
                    }
                    GeoManufactureItem geoManufactureItem3 = geoManufactureItem;
                    InteractHandler interactHandler2 = GetInteractHandler(__instance, "OnExitSlot");
                    if (interactHandler2 != null)
                    {
                        geoManufactureItem3.OnExit = (InteractHandler)Delegate.Combine(geoManufactureItem3.OnExit, interactHandler2);
                    }
                    GeoManufactureItem geoManufactureItem4 = geoManufactureItem;
                    Action<GeoManufactureItem> action = GetManufactureItemHandler(__instance, "SingleItemReloadAndRefresh");
                    if (action != null)
                    {
                        geoManufactureItem4.OnSelected = (Action<GeoManufactureItem>)Delegate.Combine(geoManufactureItem4.OnSelected, action);
                    }
                    GeoscapeViewContext geoscapeViewContext = ReplenishContext(__instance);
                    geoManufactureItem.Init(tacticalItemDef, geoscapeViewContext.ViewerFaction, repairCost, false);
                    geoManufactureItem.CanCraftQuantityText.transform.parent.gameObject.SetActive(false);
                    ReplenishmentElementController.CreateAndAdd(geoManufactureItem.gameObject, ReplenishmentType.Reload, character, item.ItemDef, item);
                    ReplenishAmmoDefs.Remove(geoManufactureItem);
                    ReplenishAmmoDefs.Add(geoManufactureItem, new AmmoDefHolder
                    {
                        AmmoDef = tacticalItemDef
                    });
                    __instance.Items.Add(geoManufactureItem);
                    GameTagDef manufacturableTag = GameUtl.GameComponent<SharedData>().SharedGameTags.ManufacturableTag;
                    GeoPhoenixFaction geoPhoenixFaction = ReplenishFaction(__instance);

                    // Buy-only ammunition is never in the faction's manufacture list, so asking whether
                    // it can be built would disable every one of these rows. What gates it instead is
                    // whether a magazine is actually on sale and affordable.
                    bool flag2 = marketplaceOnly
                        ? magazineOnSale
                        : geoPhoenixFaction.Wallet.HasResources(repairCost)
                            && tacticalItemDef.Tags.Contains(manufacturableTag)
                            && geoPhoenixFaction.Manufacture.Contains(tacticalItemDef);

                    AmmoDiagnostics.Trace("ReplenishScreen",
                        $"Row for {tacticalItemDef.name} on {moduleDef.name} ({currentCharges}/{maxCharges}), " +
                        $"{(marketplaceOnly ? "marketplace" : "manufacture")} priced " +
                        $"{repairCost.ByResourceType(ResourceType.Materials).RoundedValue} materials, " +
                        $"{(flag2 ? "buyable" : "not buyable")}.");
                    PhoenixGeneralButton component = geoManufactureItem.AddToQueueButton.GetComponent<PhoenixGeneralButton>();
                    if (component != null)
                    {
                        component.SetEnabled(flag2);
                    }
                    geoManufactureItem.AddToQueueButton.SetInteractable(flag2);
                    if (flag2)
                    {
                        materialsCost += repairCost.ByResourceType(ResourceType.Materials).RoundedValue;
                        techCost += repairCost.ByResourceType(ResourceType.Tech).RoundedValue;
                        flag = true;
                    }
                }
                __result = flag;
                return false;
            }
        }

        #region Buying module ammunition from the marketplace

        // Ground vehicle module ammunition is Kaos kit: it is never manufacturable, so the replenish
        // screen's usual "pay the repair cost, conjure the clip" route does not apply to it. A module
        // magazine is bought from the marketplace at the going rate for a whole magazine, and whatever
        // the weapon cannot hold goes into faction storage rather than being thrown away.

        /// <summary>
        /// The cheapest magazine of this ammunition currently on sale, or null when the marketplace is
        /// not offering any. Deliberately has no fallback price: ammunition that is not on sale cannot
        /// be bought, and the replenish row stays disabled until the next restock.
        /// </summary>
        private static GeoEventChoice FindCheapestListing(GeoMarketplace marketplace, TacticalItemDef ammoDef, out int price)
        {
            price = 0;
            if (marketplace?.MarketplaceChoices == null || ammoDef == null)
            {
                return null;
            }

            GeoEventChoice cheapest = null;
            int cheapestPrice = int.MaxValue;

            foreach (GeoEventChoice choice in marketplace.MarketplaceChoices)
            {
                if (choice?.Outcome?.Items == null || choice.Outcome.Items.Count == 0) continue;
                if (choice.Outcome.Items[0].ItemDef != ammoDef) continue;
                if (choice.Requirments?.Resources == null) continue;

                int choicePrice = choice.Requirments.Resources.ByResourceType(ResourceType.Materials).RoundedValue;
                if (choicePrice < cheapestPrice)
                {
                    cheapestPrice = choicePrice;
                    cheapest = choice;
                }
            }

            if (cheapest == null)
            {
                return null;
            }

            price = cheapestPrice;
            return cheapest;
        }

        /// <summary>
        /// Ammunition that can only ever be bought, never built - the Junker's magazines and the
        /// Purgatory's. Everything else a vehicle fires is Phoenix, New Jericho or Synedrion kit that
        /// the manufacture-cost route already prices correctly, and is left alone.
        /// </summary>
        internal static bool IsMarketplaceOnlyAmmo(TacticalItemDef ammoDef)
        {
            return ammoDef != null && VehiclesAmmoMain.MarketplaceAmmoDefsAndOptions.ContainsKey(ammoDef);
        }

        internal static ResourcePack MarketplacePrice(int price)
        {
            return new ResourcePack(new ResourceUnit(ResourceType.Materials, price));
        }

        /// <summary>
        /// Whether a magazine of this ammunition could be bought right now: one is on sale and the
        /// faction can afford the cheapest.
        /// </summary>
        internal static bool CanBuyMagazine(GeoPhoenixFaction faction, TacticalItemDef ammoDef, out ResourcePack cost)
        {
            cost = null;

            GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            if (controller == null || faction == null) return false;

            int price;
            if (FindCheapestListing(controller.Marketplace, ammoDef, out price) == null) return false;

            cost = MarketplacePrice(price);
            return faction.Wallet.HasResources(cost);
        }

        /// <summary>
        /// Buys one whole magazine of this ammunition at the cheapest price on sale, loads the module
        /// with as much of it as the weapon can hold, and banks the rest in faction storage.
        ///
        /// The listing is consumed, so a single cheap magazine cannot be used to refill a whole fleet -
        /// buying it here is the same transaction as buying it at the marketplace, and removes it from
        /// the offers for the same reason.
        /// </summary>
        private static bool TryBuyMagazineAndLoad(GeoItem moduleItem, TacticalItemDef ammoDef)
        {
            try
            {
                GroundVehicleModuleDef moduleDef = moduleItem?.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null || ammoDef == null || ammoDef.ChargesMax <= 0) return false;
                if (!EnsureModuleAmmo(moduleItem.CommonItemData, moduleDef)) return false;

                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                GeoPhoenixFaction faction = controller?.PhoenixFaction;
                if (faction == null) return false;

                int needed = GetAmmoCapacityForDef(moduleDef, ammoDef) - GetAmmoChargesForDef(moduleItem.CommonItemData, ammoDef);
                if (needed <= 0)
                {
                    AmmoDiagnostics.Trace("Purchase", $"{ammoDef.name} already full on {moduleDef.name}; nothing bought.");
                    return true;
                }

                int price;
                GeoEventChoice listing = FindCheapestListing(controller.Marketplace, ammoDef, out price);
                if (listing == null)
                {
                    TFTVLogger.Always($"[ModuleAmmo] No {ammoDef.name} on sale; cannot replenish {moduleDef.name}.");
                    return false;
                }

                ResourcePack cost = MarketplacePrice(price);
                if (!faction.Wallet.HasResources(cost))
                {
                    AmmoDiagnostics.Trace("Purchase",
                        $"Cheapest {ammoDef.name} is {price} materials; faction cannot afford it.");
                    return false;
                }

                faction.Wallet.Take(cost, OperationReason.Purchase);
                controller.Marketplace.MarketplaceChoices.Remove(listing);

                // The magazine is bought whole: the weapon takes what it has room for and the balance
                // is banked, so nothing the player paid for is lost.
                int loaded = Math.Min(needed, ammoDef.ChargesMax);
                int leftover = ammoDef.ChargesMax - loaded;

                GeoItem magazine = new GeoItem(ammoDef, 1, -1, null, -100);
                magazine.CommonItemData.ModifyCharges(-magazine.CommonItemData.CurrentCharges, false);
                magazine.CommonItemData.ModifyCharges(loaded, false);
                moduleItem.CommonItemData.Ammo.LoadMagazine(magazine);

                if (leftover > 0)
                {
                    GeoItem remainder = new GeoItem(ammoDef, 1, -1, null, -100);
                    remainder.CommonItemData.ModifyCharges(-remainder.CommonItemData.CurrentCharges, false);
                    remainder.CommonItemData.ModifyCharges(leftover, false);
                    faction.ItemStorage.AddItem(remainder);
                }

                TFTVLogger.Always($"[ModuleAmmo] Bought a {ammoDef.name} magazine for {price} materials; " +
                    $"{loaded} into {moduleDef.name}, {leftover} to storage.");

                AmmoDiagnostics.Trace("Purchase",
                    $"Listing consumed; marketplace now holds " +
                    $"{controller.Marketplace.MarketplaceChoices.Count} choice(s). Module: {AmmoDiagnostics.DescribeAmmo(moduleItem)}");

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// The row-level buy button. Each row stands for one of the module's ammunition types, and
        /// only that type is bought.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleReplenish), "SingleItemReloadAndRefresh")]
        public static class UIModuleReplenish_SingleItemReloadAndRefresh_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static bool Prefix(UIModuleReplenish __instance, GeoManufactureItem item)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn || item == null) return true;

                    AmmoDefHolder holder;
                    if (!ReplenishAmmoDefs.TryGetValue(item, out holder)) return true;

                    GeoItem moduleItem = item.GetComponent<ReplenishmentElementController>()?.Item;
                    if (moduleItem == null) return true;

                    AmmoDiagnostics.Trace("ReplenishScreen",
                        $"Row clicked for {holder.AmmoDef?.name}: {AmmoDiagnostics.DescribeAmmo(moduleItem)}");

                    if (TryBuyMagazineAndLoad(moduleItem, holder.AmmoDef))
                    {
                        AccessTools.Method(typeof(UIModuleReplenish), "RemoveFromList")?.Invoke(__instance, new object[] { item, true });
                        AccessTools.Method(typeof(UIModuleReplenish), "RefreshItemList")?.Invoke(__instance, null);
                    }

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
        /// Replenish All routes every reloadable item through SingleItemReload, which cannot read a
        /// module - vanilla resolves CompatibleAmmunition on the module def, finds none, and gives up.
        /// A module is reloaded here instead, buying a magazine for each of its ammunition types.
        /// </summary>
        [HarmonyPatch(typeof(UIModuleReplenish), "SingleItemReload")]
        public static class UIModuleReplenish_SingleItemReload_ModuleAmmo_Patch
        {
            static bool Prepare() => TFTVAircraftReworkMain.AircraftReworkOn;
            public static bool Prefix(GeoItem geoItem, ref bool __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn) return true;

                    GroundVehicleModuleDef moduleDef = geoItem?.ItemDef as GroundVehicleModuleDef;
                    if (moduleDef == null) return true;

                    AmmoDiagnostics.Trace("ReplenishAll", $"Reloading {AmmoDiagnostics.DescribeAmmo(geoItem)}");

                    bool allFull = true;
                    foreach (TacticalItemDef ammoDef in GetModuleAmmoDefs(moduleDef))
                    {
                        int missing = GetAmmoCapacityForDef(moduleDef, ammoDef) - GetAmmoChargesForDef(geoItem.CommonItemData, ammoDef);
                        if (missing <= 0) continue;

                        if (!TryBuyMagazineAndLoad(geoItem, ammoDef))
                        {
                            allFull = false;
                        }
                    }

                    // Only reports success when the module is genuinely full, so a row for ammunition
                    // that could not be bought stays on the list instead of quietly disappearing.
                    __result = allFull;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        #endregion
    }
}