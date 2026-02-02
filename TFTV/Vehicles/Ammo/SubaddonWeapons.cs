using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVChangesToDLC5;

namespace TFTV.Vehicles.Ammo
{
    [HarmonyPatch(typeof(UIStateEditVehicle), "SoldierSlotItemChangedHandler")]
    public static class UIStateEditVehicle_SoldierSlotItemChangedHandler_patch
    {
        public static bool Prefix(UIStateEditVehicle __instance, UIInventorySlot slot)
        {
            try
            {
                if (slot == null)
                {
                    return false;
                }

                return true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
    }

    [HarmonyPatch]
    public static class VehicleModuleAmmoHarmonyPatches
    {
       

        private sealed class TooltipOverrideHolder
        {
            public string TipText;
        }

        private sealed class MarketplacePromptHolder
        {
            public List<TacticalItemDef> AmmoDefs = new List<TacticalItemDef>();
        }

        
        private static readonly ConditionalWeakTable<UIInventorySlotSideButton, TooltipOverrideHolder> SideButtonTooltipOverrides = new ConditionalWeakTable<UIInventorySlotSideButton, TooltipOverrideHolder>();
        private static readonly ConditionalWeakTable<UIInventorySlotSideButton, MarketplacePromptHolder> MarketplacePromptOverrides = new ConditionalWeakTable<UIInventorySlotSideButton, MarketplacePromptHolder>();

        private struct ModuleAmmoEntry
        {
            public TacticalItemDef AmmoDef;
            public int CurrentCharges;
            public int MaxCharges;
        }

        internal static List<TacticalItemDef> GetModuleAmmoDefs(GroundVehicleModuleDef moduleDef)
        {
            if (moduleDef == null)
            {
                return new List<TacticalItemDef>();
            }
            List<TacticalItemDef> list = new List<TacticalItemDef>();
            foreach (WeaponDef weaponDef in moduleDef.GetSubWeapons())
            {
                TacticalItemDef tacticalItemDef = weaponDef.CompatibleAmmunition.FirstOrDefault();
                if (tacticalItemDef != null && !list.Contains(tacticalItemDef))
                {
                    list.Add(tacticalItemDef);
                }
            }
            return list;
        }

        private static List<ModuleAmmoEntry> GetModuleAmmoEntries(CommonItemData commonItemData, GroundVehicleModuleDef moduleDef)
        {
            List<TacticalItemDef> moduleAmmoDefs = GetModuleAmmoDefs(moduleDef);
            List<ModuleAmmoEntry> list = new List<ModuleAmmoEntry>();
            foreach (TacticalItemDef tacticalItemDef in moduleAmmoDefs)
            {
                int num = GetAmmoChargesForDef(commonItemData, tacticalItemDef);
                list.Add(new ModuleAmmoEntry
                {
                    AmmoDef = tacticalItemDef,
                    CurrentCharges = num,
                    MaxCharges = GetAmmoCapacityForDef(moduleDef, tacticalItemDef)
                });
            }
            return list;
        }

        internal static int GetAmmoChargesForDef(CommonItemData commonItemData, TacticalItemDef ammoDef)
        {
            if (commonItemData == null || commonItemData.Ammo == null || ammoDef == null)
            {
                return 0;
            }
            int num = 0;
            foreach (ICommonItem commonItem in commonItemData.Ammo.LoadedMagazines)
            {
                if (commonItem.ItemDef == ammoDef)
                {
                    num += commonItem.CommonItemData.TotalCharges;
                }
            }
            return num;
        }

        internal static int GetAmmoCapacityForDef(GroundVehicleModuleDef moduleDef, TacticalItemDef ammoDef)
        {
            if (moduleDef == null || ammoDef == null || ammoDef.ChargesMax <= 0)
            {
                return 0;
            }
            int num = 0;
            foreach (WeaponDef weaponDef in moduleDef.GetSubWeapons())
            {
                if (weaponDef.CompatibleAmmunition.Contains(ammoDef))
                {
                    num++;
                }
            }
            return num * ammoDef.ChargesMax;
        }

        private static void MoveModuleAmmoToSubweapons(GroundVehicleModule module, IEnumerable<Weapon> subWeapons)
        {
            if (module == null || module.CommonItemData == null || module.CommonItemData.Ammo == null)
            {
                return;
            }
            List<ICommonItem> loadedMagazines = module.CommonItemData.Ammo.LoadedMagazines;
            if (loadedMagazines == null || loadedMagazines.Count == 0)
            {
                return;
            }
            foreach (Weapon weapon in subWeapons)
            {
                TacticalItemDef tacticalItemDef = weapon.WeaponDef.CompatibleAmmunition.FirstOrDefault();
                if (tacticalItemDef == null)
                {
                    continue;
                }
                List<ICommonItem> list = loadedMagazines.Where((ICommonItem item) => item.ItemDef == tacticalItemDef).ToList<ICommonItem>();
                if (list.Count == 0)
                {
                    continue;
                }
                if (weapon.CommonItemData.Ammo == null)
                {
                    weapon.CommonItemData.Ammo = new AmmoManager(weapon);
                }
                foreach (ICommonItem commonItem in list)
                {
                    weapon.CommonItemData.Ammo.LoadMagazine(commonItem);
                    loadedMagazines.Remove(commonItem);
                }
            }
        }

        private static void MoveSubweaponAmmoToModule(GroundVehicleModule module, IEnumerable<Weapon> subWeapons)
        {
            if (module == null || module.CommonItemData == null)
            {
                return;
            }
            if (module.CommonItemData.Ammo == null)
            {
                module.CommonItemData.Ammo = new AmmoManager(module);
            }
            foreach (Weapon weapon in subWeapons)
            {
                if (weapon.CommonItemData == null || weapon.CommonItemData.Ammo == null)
                {
                    continue;
                }
                List<ICommonItem> list = weapon.CommonItemData.Ammo.UnloadMagazines();
                foreach (ICommonItem commonItem in list)
                {
                    module.CommonItemData.Ammo.LoadMagazine(commonItem);
                }
            }
        }

        internal static bool EnsureModuleAmmo(CommonItemData commonItemData, GroundVehicleModuleDef moduleDef)
        {
            if (commonItemData == null || moduleDef == null)
            {
                return false;
            }
            if (GetModuleAmmoDefs(moduleDef).Count == 0)
            {
                return false;
            }
            if (commonItemData.Ammo == null)
            {
                commonItemData.Ammo = new AmmoManager(commonItemData.OwnerItem);
            }
            return true;
        }

        private static bool HasAmmoInInventory(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (parentModule == null || ammoDef == null || parentModule.InventoryList == null)
            {
                return false;
            }

            return parentModule.InventoryList.UnfilteredItems.Any(item => item.ItemDef == ammoDef);
        }

        private static bool HasAmmoInStorage(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (parentModule == null || ammoDef == null)
            {
                return false;
            }

            if (parentModule.StorageList.UnfilteredItems.Any((ICommonItem item) => item.ItemDef == ammoDef))
            {
                return true;
            }

            return parentModule.StorageList.PartialMagazines.Items.ContainsKey(ammoDef);
        }

        private static bool HasInventorySpace(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (parentModule == null || ammoDef == null || parentModule.InventoryList == null)
            {
                return false;
            }

            return parentModule.InventoryList.GetFirstAvailableSlot(ammoDef, false) != null;
        }

        private static void ClearTooltipOverride(UIInventorySlotSideButton button)
        {
            if (button == null)
            {
                return;
            }

            SideButtonTooltipOverrides.Remove(button);
        }

        private static void SetTooltipOverride(UIInventorySlotSideButton button, string tooltipText)
        {
            if (button == null)
            {
                return;
            }

            SideButtonTooltipOverrides.Remove(button);
            SideButtonTooltipOverrides.Add(button, new TooltipOverrideHolder { TipText = tooltipText });
        }

        private static void ClearMarketplacePromptOverride(UIInventorySlotSideButton button)
        {
            if (button == null)
            {
                return;
            }

            MarketplacePromptOverrides.Remove(button);
        }

        private static void SetMarketplacePromptOverride(UIInventorySlotSideButton button, IEnumerable<TacticalItemDef> ammoDefs)
        {
            if (button == null)
            {
                return;
            }

            ClearMarketplacePromptOverride(button);

            MarketplacePromptOverrides.Add(button, new MarketplacePromptHolder
            {
                AmmoDefs = ammoDefs.Where(def => def != null).ToList()
            });
        }

        private static bool IsItemMissingAmmo(ICommonItem owningItem, TacticalItemDef ammoDef)
        {
            if (owningItem?.CommonItemData == null || ammoDef == null)
            {
                return false;
            }

            if (owningItem.CommonItemData.Ammo == null)
            {
                // Treat missing ammo manager as empty / needs ammo
                return true;
            }

            int currentCharges = GetAmmoChargesForDef(owningItem.CommonItemData, ammoDef);
            return currentCharges < ammoDef.ChargesMax;
        }

        private static bool TryLoadWeaponFromClipInInventoryOrStorage(
            UIModuleSoldierEquip parentModule,
            ICommonItem owningItem,
            TacticalItemDef ammoDef)
        {
            if (parentModule == null || owningItem == null || ammoDef == null)
            {
                return false;
            }

            ICommonItem ammoItem = null;
            UIInventoryList sourceList = null;

            // Prefer storage first (matches module priority in GetPreferredModuleAmmoEntry)
            if (HasAmmoInStorage(parentModule, ammoDef))
            {
                ammoItem = parentModule.StorageList.UnfilteredItems
                    .FirstOrDefault(i => i.ItemDef == ammoDef)?.GetSingleItem();
                sourceList = parentModule.StorageList;

                if (ammoItem == null)
                {
                    // fallback: partial magazines are in storage list, take via helper
                    ICommonItem partial;
                    if (TryTakeAmmoFromStorageList(parentModule, ammoDef, out partial))
                    {
                        ammoItem = partial;
                        sourceList = null; // already removed from storage by TryTakeAmmoFromStorageList
                    }
                }
            }
            else if (HasAmmoInInventory(parentModule, ammoDef))
            {
                ammoItem = parentModule.InventoryList.UnfilteredItems.FirstOrDefault(i => i.ItemDef == ammoDef);
                sourceList = parentModule.InventoryList;
            }

            if (ammoItem == null)
            {
                return false;
            }

            // Ensure ammo manager exists
            if (owningItem.CommonItemData.Ammo == null)
            {
                owningItem.CommonItemData.Ammo = new AmmoManager(owningItem.CommonItemData.OwnerItem);
            }

            int maxCharges = ammoDef.ChargesMax;
            int currentCharges = GetAmmoChargesForDef(owningItem.CommonItemData, ammoDef);

            // Load from the picked clip into the weapon (partial load supported)
            while (!ammoItem.CommonItemData.IsEmpty() && currentCharges < maxCharges)
            {
                int neededCharges = maxCharges - currentCharges;

                ICommonItem single = ammoItem.GetSingleItem().Clone();
                int availableCharges = single.CommonItemData.TotalCharges;
                int chargesToLoad = Math.Min(neededCharges, availableCharges);

                if (chargesToLoad < availableCharges)
                {
                    single.CommonItemData.ModifyCharges(-single.CommonItemData.CurrentCharges, false);
                    single.CommonItemData.ModifyCharges(chargesToLoad, false);
                }

                owningItem.CommonItemData.Ammo.LoadMagazine(single);

                if (chargesToLoad >= availableCharges)
                {
                    ammoItem.CommonItemData.Subtract(single);
                }
                else
                {
                    ammoItem.CommonItemData.ModifyCharges(-chargesToLoad, false);
                }

                currentCharges += chargesToLoad;
            }

            // If we consumed the clip fully and it was from a list, remove it
            if (ammoItem.CommonItemData.IsEmpty() && sourceList != null)
            {
                sourceList.RemoveItem(ammoItem, null);
            }
            else if (!ammoItem.CommonItemData.IsEmpty() && sourceList == parentModule.StorageList)
            {
                // If it was from storage and still has charges, put it back (matches module flow)
                sourceList.AddItem(ammoItem, null, null);
            }

            return true;
        }

        private static bool TryReloadWeaponFromPurchasedClip(ICommonItem owningItem, TacticalItemDef ammoDef, UIModuleSoldierEquip parentModule)
        {
            try
            {
                if (owningItem == null || ammoDef == null || parentModule == null)
                {
                    return false;
                }

                WeaponDef weaponDef = owningItem.ItemDef as WeaponDef;
                if (weaponDef == null || weaponDef.CompatibleAmmunition == null || !weaponDef.CompatibleAmmunition.Contains(ammoDef))
                {
                    return false;
                }

                if (owningItem.CommonItemData.Ammo == null)
                {
                    owningItem.CommonItemData.Ammo = new AmmoManager(owningItem.CommonItemData.OwnerItem);
                }

                int maxCharges = ammoDef.ChargesMax;
                int currentCharges = GetAmmoChargesForDef(owningItem.CommonItemData, ammoDef);
                int needed = maxCharges - currentCharges;

                if (needed <= 0)
                {
                    return false;
                }

                int chargesToLoad = Mathf.Min(needed, ammoDef.ChargesMax);
                int remainingCharges = Mathf.Max(ammoDef.ChargesMax - chargesToLoad, 0);

                GeoItem geoItem = new GeoItem(ammoDef, 1, -1, null, -100);
                geoItem.CommonItemData.ModifyCharges(-geoItem.CommonItemData.CurrentCharges, false);
                geoItem.CommonItemData.ModifyCharges(chargesToLoad, false);

                owningItem.CommonItemData.Ammo.LoadMagazine(geoItem);

                // leftover goes to storage (consistent with module purchase behavior)
                TryPlacePurchasedAmmoClip(parentModule, ammoDef, remainingCharges);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

       

        private static bool IsWeaponWithCompatibleAmmo(ItemDef itemDef, out WeaponDef weaponDef, out TacticalItemDef ammoDef)
        {
            weaponDef = null;
            ammoDef = null;

            weaponDef = itemDef as WeaponDef;
            if (weaponDef == null)
            {
                return false;
            }

            if (weaponDef.CompatibleAmmunition == null || weaponDef.CompatibleAmmunition.Length == 0)
            {
                return false;
            }

            ammoDef = weaponDef.CompatibleAmmunition[0];
            return ammoDef != null;
        }

        private static GeoEventChoice GetMarketplaceAmmoChoice(TacticalItemDef ammoDef)
        {
            if (ammoDef == null)
            {
                return null;
            }

            GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            GeoMarketplace marketplace = controller?.Marketplace;
            if (marketplace == null || marketplace.MarketplaceChoices == null)
            {
                return null;
            }

            return marketplace.MarketplaceChoices.FirstOrDefault(c =>
                c != null &&
                c.Outcome != null &&
                c.Outcome.Items != null &&
                c.Outcome.Items.Count > 0 &&
                c.Outcome.Items[0].ItemDef == ammoDef);
        }

        private static bool TryGetMarketplaceAmmoCost(TacticalItemDef ammoDef, out ResourcePack cost)
        {
            cost = null;

            GeoEventChoice choice = GetMarketplaceAmmoChoice(ammoDef);
            if (choice == null)
            {
                return false;
            }

            cost = choice.Requirments?.Resources;
            return cost != null;
        }

        private static bool TryBuyAmmoClipFromMarketplace(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            try
            {
                if (parentModule == null || ammoDef == null || parentModule.ModuleData == null || parentModule.ModuleData.Wallet == null)
                {
                    return false;
                }

                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                GeoMarketplace marketplace = controller?.Marketplace;
                if (marketplace == null || marketplace.MarketplaceChoices == null || marketplace.MarketplaceChoices.Count == 0)
                {
                    return false;
                }

                GeoEventChoice choice = GetMarketplaceAmmoChoice(ammoDef);
                if (choice == null)
                {
                    return false;
                }

                ResourcePack cost = choice.Requirments?.Resources;
                if (cost == null)
                {
                    return false;
                }

                if (!parentModule.ModuleData.Wallet.HasResources(cost))
                {
                    return false;
                }

                parentModule.ModuleData.Wallet.Take(cost, OperationReason.Purchase);
                marketplace.MarketplaceChoices.Remove(choice);

                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static bool IsKaosGunWeapon(ItemDef itemDef, out WeaponDef weaponDef, out TacticalItemDef ammoDef)
        {
            weaponDef = null;
            ammoDef = null;

            if (!IsWeaponWithCompatibleAmmo(itemDef, out weaponDef, out ammoDef))
            {
                return false;
            }

            // KG ammo is created in TFTVChangesToDLC5.cs and the weapon gets tagged there.
            // We gate the KG sidebutton behavior to that tag so normal weapons keep vanilla behavior.
            if (TFTVChangesToDLC5.TFTVKaosGuns._kGTag == null)
            {
                return false;
            }

            return weaponDef.Tags != null && weaponDef.Tags.Contains(TFTVChangesToDLC5.TFTVKaosGuns._kGTag);
        }

        [HarmonyPatch(typeof(UIInventorySlotSideButton), "GetState")]
        public static class UIInventorySlotSideButton_GetState_Patch
        {
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, bool> ButtonPossible =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, bool>("_buttonPossible");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIInventorySlot> OwningSlot =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIInventorySlot>("_owningSlot");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIModuleSoldierEquip> ParentModule =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIModuleSoldierEquip>("_parentModule");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, ItemDef> ItemToProduce =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, ItemDef>("_itemToProduce");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIInventoryList> DestinationList =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIInventoryList>("_destinationList");

            public static bool Prefix(UIInventorySlotSideButton __instance, ref UIInventorySlotSideButton.GeneralState __result)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn || __instance == null)
                    {
                        return true;
                    }

                    UIInventorySlot slot = OwningSlot(__instance);
                    if (slot == null || slot.Empty)
                    {
                        return true;
                    }

                    ICommonItem item = slot.Item;
                    if (item == null || item.ItemDef == null)
                    {
                        return true;
                    }

                    UIModuleSoldierEquip parentModule = ParentModule(__instance);
                    if (parentModule == null)
                    {
                        __result.State = UIInventorySlotSideButton.SideButtonState.Hidden;
                        return false;
                    }

                    if (!ButtonPossible(__instance))
                    {
                        return true;
                    }

                    ClearTooltipOverride(__instance);
                    ClearMarketplacePromptOverride(__instance);

                    if (item.ItemDef is GroundVehicleModuleDef moduleDef && moduleDef.GetSubWeapons().Count() > 0)
                    {
                        if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                        {
                            return true;
                        }

                        return TryComputeVehicleModuleState(__instance, parentModule, slot, item, moduleDef, ref __result);
                    }

                    if (IsKaosGunWeapon(item.ItemDef, out WeaponDef weaponDef, out TacticalItemDef weaponAmmoDef))
                    {
                        // IMPORTANT: do NOT force our own tooltip/state when storage/inventory already has ammo;
                        // vanilla behavior should apply then.
                        if (HasAmmoInStorage(parentModule, weaponAmmoDef)) //|| HasAmmoInInventory(parentModule, weaponAmmoDef))
                        {
                            return true;
                        }

                        // Only now do we show marketplace/out-of-stock tooltip
                        if (!HasMarketplaceAmmoStock(weaponAmmoDef))
                        {
                            __result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;
                            SetTooltipOverride(__instance, BuildMarketplaceOutOfStockText(weaponAmmoDef));
                            return false;
                        }

                        ItemToProduce(__instance) = weaponAmmoDef;
                        __result.Action = UIInventorySlotSideButton.SideButtonAction.AddAmmo;
                        DestinationList(__instance) = parentModule.InventoryList;

                        TrySetMarketplaceAmmoState(__instance, parentModule, weaponAmmoDef, ref __result);
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

            private static bool TryComputeVehicleModuleState(
                UIInventorySlotSideButton instance,
                UIModuleSoldierEquip parentModule,
                UIInventorySlot slot,
                ICommonItem item,
                GroundVehicleModuleDef moduleDef,
                ref UIInventorySlotSideButton.GeneralState result)
            {
                result = default(UIInventorySlotSideButton.GeneralState);


                if (item.CommonItemData.Ammo == null)
                {
                    return true;
                }

                List<ModuleAmmoEntry> moduleAmmoEntries = GetModuleAmmoEntries(item.CommonItemData, moduleDef);
                if (moduleAmmoEntries.Count == 0)
                {
                    return true;
                }

                List<ModuleAmmoEntry> missingEntries = moduleAmmoEntries
                    .Where(entry => entry.MaxCharges > 0 && entry.CurrentCharges < entry.MaxCharges)
                    .ToList();

                if (missingEntries.Count == 0)
                {
                    return TryComputeFullModuleState(instance, parentModule, slot, moduleAmmoEntries, ref result);
                }

                ModuleAmmoEntry? preferredEntry = GetPreferredModuleAmmoEntry(parentModule, missingEntries, moduleAmmoEntries);
                if (preferredEntry == null)
                {
                    return true;
                }

                TacticalItemDef ammoDef = preferredEntry.Value.AmmoDef;
                ItemToProduce(instance) = ammoDef;

                if (preferredEntry.Value.MaxCharges > 0 && preferredEntry.Value.CurrentCharges < preferredEntry.Value.MaxCharges)
                {
                    result.Action = UIInventorySlotSideButton.SideButtonAction.LoadAmmo;
                    DestinationList(instance) = slot.ParentList;
                }
                else
                {
                    result.Action = UIInventorySlotSideButton.SideButtonAction.AddAmmo;
                    DestinationList(instance) = (UIInventoryList)AccessTools.Method(typeof(UIInventorySlotSideButton), "GetDestinationList")
                        .Invoke(instance, new object[] { ammoDef });
                }

                UIInventoryList destinationList = DestinationList(instance);
                if (destinationList == null)
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededNoStorage;
                    return false;
                }

                if (missingEntries.Count > 0)
                {
                    if (missingEntries.Any(entry => HasAmmoInStorage(parentModule, entry.AmmoDef)) ||
                        missingEntries.Any(entry => HasAmmoInInventory(parentModule, entry.AmmoDef)))
                    {
                        result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededFree;
                        return false;
                    }

                    if (missingEntries.Count == 1)
                    {
                        if (TrySetMarketplaceAmmoState(instance, parentModule, ammoDef, ref result))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        TacticalItemDef first = missingEntries[0].AmmoDef;
                        TacticalItemDef second = missingEntries[1].AmmoDef;
                        if (TrySetMarketplaceAmmoState(instance, parentModule, new List<TacticalItemDef> { first, second }, ref result))
                        {
                            SetMarketplacePromptOverride(instance, new[] { first, second });
                            return false;
                        }
                    }
                }

                result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededAffordable;
                return false;
            }

            private static bool TryComputeFullModuleState(
                UIInventorySlotSideButton instance,
                UIModuleSoldierEquip parentModule,
                UIInventorySlot slot,
                List<ModuleAmmoEntry> moduleAmmoEntries,
                ref UIInventorySlotSideButton.GeneralState result)
            {
                if (parentModule == null || moduleAmmoEntries == null || moduleAmmoEntries.Count == 0)
                {
                    return true;
                }

                List<TacticalItemDef> ammoDefs = moduleAmmoEntries.Select(entry => entry.AmmoDef).Where(def => def != null).ToList();
                TacticalItemDef storageAmmo = ammoDefs.FirstOrDefault(def => HasAmmoInStorage(parentModule, def) && HasInventorySpace(parentModule, def));
                if (storageAmmo != null)
                {
                    ItemToProduce(instance) = storageAmmo;
                    result.Action = UIInventorySlotSideButton.SideButtonAction.AddAmmo;
                    DestinationList(instance) = parentModule.InventoryList;
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededFree;
                    return false;
                }

                List<TacticalItemDef> marketplaceAmmo = ammoDefs.Where(def => HasMarketplaceAmmoStock(def)).ToList();
                if (marketplaceAmmo.Count == 0)
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;

                    // Avoid the generic "can't be manufactured" tooltip by always showing the
                    // marketplace rotation info for each compatible ammo type.
                    var tooltipLines = ammoDefs
                        .Where(def => def != null)
                        .Select(def => BuildMarketplaceOutOfStockText(def))
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    if (tooltipLines.Count > 0)
                    {
                        SetTooltipOverride(instance, string.Join("\n", tooltipLines));
                    }

                    return false;
                }

                TacticalItemDef firstMarketAmmo = marketplaceAmmo[0];
                if (!HasInventorySpace(parentModule, firstMarketAmmo))
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededNoStorage;
                    return false;
                }

                ItemToProduce(instance) = firstMarketAmmo;
                result.Action = UIInventorySlotSideButton.SideButtonAction.AddAmmo;
                DestinationList(instance) = parentModule.InventoryList;

                if (marketplaceAmmo.Count == 1)
                {
                    TrySetMarketplaceAmmoState(instance, parentModule, firstMarketAmmo, ref result);
                    return false;
                }

                TacticalItemDef secondMarketAmmo = marketplaceAmmo[1];
                TrySetMarketplaceAmmoState(instance, parentModule, new List<TacticalItemDef> { firstMarketAmmo, secondMarketAmmo }, ref result);
                SetMarketplacePromptOverride(instance, new[] { firstMarketAmmo, secondMarketAmmo });
                return false;
            }

            private static ModuleAmmoEntry? GetPreferredModuleAmmoEntry(
                UIModuleSoldierEquip parentModule,
                List<ModuleAmmoEntry> missingEntries,
                List<ModuleAmmoEntry> allEntries)
            {
                if (missingEntries != null && missingEntries.Count > 0)
                {
                    ModuleAmmoEntry entryFromStorage = missingEntries.FirstOrDefault(entry => HasAmmoInStorage(parentModule, entry.AmmoDef));
                    if (entryFromStorage.AmmoDef != null)
                    {
                        return entryFromStorage;
                    }

                    ModuleAmmoEntry entryFromInventory = missingEntries.FirstOrDefault(entry => HasAmmoInInventory(parentModule, entry.AmmoDef));
                    if (entryFromInventory.AmmoDef != null)
                    {
                        return entryFromInventory;
                    }

                    return missingEntries[0];
                }

                if (allEntries == null || allEntries.Count == 0)
                {
                    return null;
                }

                return allEntries[0];
            }
        }

        [HarmonyPatch(typeof(UIInventorySlotSideButton), "RefreshState")]
        public static class UIInventorySlotSideButton_RefreshState_MarketplaceTooltipOverride_Patch
        {
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIInventorySlotSideButton.GeneralState> CurrentState =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIInventorySlotSideButton.GeneralState>("_currentState");

            public static void Postfix(UIInventorySlotSideButton __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return;
                    }

                    TooltipOverrideHolder holder;
                    if (!SideButtonTooltipOverrides.TryGetValue(__instance, out holder) || holder == null || string.IsNullOrWhiteSpace(holder.TipText))
                    {
                        return;
                    }

                    var state = CurrentState(__instance);
                    if (state.State == UIInventorySlotSideButton.SideButtonState.Hidden)
                    {
                        return;
                    }

                    AccessTools.Method(typeof(UIInventorySlotSideButton), "SetTooltipText")?.Invoke(__instance, new object[] { holder.TipText });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        private static bool HasMarketplaceAmmoStock(TacticalItemDef ammoDef)
        {
            return GetMarketplaceAmmoChoice(ammoDef) != null;
        }

        private static int GetMarketplaceAmmoStockCount(TacticalItemDef ammoDef)
        {
            if (ammoDef == null)
            {
                return 0;
            }

            GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            GeoMarketplace marketplace = controller?.Marketplace;
            if (marketplace == null || marketplace.MarketplaceChoices == null)
            {
                return 0;
            }

            return marketplace.MarketplaceChoices.Count(c =>
                c != null &&
                c.Outcome != null &&
                c.Outcome.Items != null &&
                c.Outcome.Items.Count > 0 &&
                c.Outcome.Items[0].ItemDef == ammoDef);
        }

        private static bool TryGetMarketplaceDaysToRotation(out int daysToRotation)
        {
            daysToRotation = 1;

            GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            GeoMarketplace geoMarketplace = controller?.Marketplace;
            if (controller == null || geoMarketplace == null)
            {
                return false;
            }

            FieldInfo fieldInfoUpdateOptionsNextTime = typeof(GeoMarketplace)
                .GetField("_updateOptionsNextTime", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfoUpdateOptionsNextTime == null)
            {
                return false;
            }

            TimeUnit updateTime = (TimeUnit)fieldInfoUpdateOptionsNextTime.GetValue(geoMarketplace);
            TimeUnit currentTime = controller.Timing.Now;

            daysToRotation = Mathf.Max(updateTime.DateTime.Day - currentTime.DateTime.Day, 1);
            return true;
        }

        private static string BuildMarketplaceOutOfStockText(TacticalItemDef ammoDef)
        {
            string outOfStockText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_OUT_OF_STOCK");
            string ammoName = ammoDef.ViewElementDef != null ? ammoDef.ViewElementDef.DisplayName1.Localize() : ammoDef.name;

            int daysToRotation;
            if (!TryGetMarketplaceDaysToRotation(out daysToRotation))
            {
                daysToRotation = 1;
            }

            outOfStockText = outOfStockText.Replace("{0}", ammoName);
            outOfStockText = outOfStockText.Replace("{1}", daysToRotation.ToString());
            return outOfStockText;
        }

        private static string BuildMarketplaceBuyText(TacticalItemDef ammoDef, int stockCount, ResourcePack mpCost)
        {
            string buyText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_BUY");
            string ammoNameInStock = ammoDef.ViewElementDef != null ? ammoDef.ViewElementDef.DisplayName1.Localize() : ammoDef.name;

            int matsCost = mpCost.ByResourceType(ResourceType.Materials).RoundedValue;

            buyText = buyText.Replace("{0}", stockCount.ToString());
            buyText = buyText.Replace("{1}", ammoNameInStock);
            buyText = buyText.Replace("{2}", matsCost.ToString());

            return buyText;
        }

        private static bool TrySetMarketplaceAmmoState(
            UIInventorySlotSideButton instance,
            UIModuleSoldierEquip parentModule,
            TacticalItemDef ammoDef,
            ref UIInventorySlotSideButton.GeneralState result)
        {
            int stockCount = GetMarketplaceAmmoStockCount(ammoDef);
            if (stockCount <= 0)
            {
                result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;
                SetTooltipOverride(instance, BuildMarketplaceOutOfStockText(ammoDef));
                return true;
            }

            ResourcePack mpCost;
            if (!TryGetMarketplaceAmmoCost(ammoDef, out mpCost))
            {
                result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;
                return true;
            }

            bool canAfford = parentModule.ModuleData?.Wallet != null &&
                             parentModule.ModuleData.Wallet.HasResources(mpCost);

            result.State = canAfford
                ? UIInventorySlotSideButton.SideButtonState.ActionNeededAffordable
                : UIInventorySlotSideButton.SideButtonState.ActionNeededUnaffordable;

            SetTooltipOverride(instance, BuildMarketplaceBuyText(ammoDef, stockCount, mpCost));
            return true;
        }

        private static bool TrySetMarketplaceAmmoState(
            UIInventorySlotSideButton instance,
            UIModuleSoldierEquip parentModule,
            List<TacticalItemDef> ammoDefs,
            ref UIInventorySlotSideButton.GeneralState result)
        {
            if (ammoDefs == null || ammoDefs.Count == 0)
            {
                return false;
            }

            bool anyStock = false;
            bool anyAffordable = false;
            List<string> tooltipLines = new List<string>();

            foreach (TacticalItemDef ammoDef in ammoDefs)
            {
                if (ammoDef == null)
                {
                    continue;
                }

                int stockCount = GetMarketplaceAmmoStockCount(ammoDef);
                if (stockCount <= 0)
                {
                    tooltipLines.Add(BuildMarketplaceOutOfStockText(ammoDef));
                    continue;
                }

                anyStock = true;
                ResourcePack mpCost;
                if (!TryGetMarketplaceAmmoCost(ammoDef, out mpCost))
                {
                    tooltipLines.Add(BuildMarketplaceOutOfStockText(ammoDef));
                    continue;
                }

                if (parentModule.ModuleData?.Wallet != null && parentModule.ModuleData.Wallet.HasResources(mpCost))
                {
                    anyAffordable = true;
                }

                tooltipLines.Add(BuildMarketplaceBuyText(ammoDef, stockCount, mpCost));
            }

            if (!anyStock)
            {
                result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;
                SetTooltipOverride(instance, string.Join("\n", tooltipLines));
                return true;
            }

            result.State = anyAffordable
                ? UIInventorySlotSideButton.SideButtonState.ActionNeededAffordable
                : UIInventorySlotSideButton.SideButtonState.ActionNeededUnaffordable;

            SetTooltipOverride(instance, string.Join("\n", tooltipLines));
            return true;
        }

        private static bool TryPlacePurchasedAmmoClip(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef, int remainingCharges)
        {
            if (parentModule == null || ammoDef == null)
            {
                return false;
            }

            if (remainingCharges <= 0)
            {
                return true;
            }

            GeoItem clip = new GeoItem(ammoDef, 1, -1, null, -100);
            clip.CommonItemData.ModifyCharges(-clip.CommonItemData.CurrentCharges, false);
            clip.CommonItemData.ModifyCharges(remainingCharges, false);

            parentModule.StorageList.AddItem(clip);
            return true;
        }

        private static bool TryPlacePurchasedAmmoClipInInventory(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (parentModule == null || ammoDef == null || parentModule.InventoryList == null)
            {
                return false;
            }

            if (!HasInventorySpace(parentModule, ammoDef))
            {
                return false;
            }

            GeoItem clip = new GeoItem(ammoDef, 1, -1, null, -100);
            parentModule.InventoryList.AddItem(clip, null, null);
            return true;
        }

        private static bool TryTakeAmmoFromStorageList(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef, out ICommonItem ammoItem)
        {
            ammoItem = null;

            if (parentModule == null || ammoDef == null)
            {
                return false;
            }

            GeoItem partialItem;
            if (parentModule.StorageList.PartialMagazines.Items.TryGetValue(ammoDef, out partialItem))
            {
                ICommonItem singleItem = partialItem.GetSingleItem();
                parentModule.StorageList.PartialMagazines.RemoveItem((GeoItem)singleItem);
                ammoItem = singleItem;
                return true;
            }

            ICommonItem storedItem = parentModule.StorageList.UnfilteredItems.FirstOrDefault(item => item.ItemDef == ammoDef);
            if (storedItem == null)
            {
                return false;
            }

            ammoItem = storedItem.GetSingleItem();
            parentModule.StorageList.RemoveItem(ammoItem, null);
            return true;
        }

        private static bool TryReloadModuleFromPurchasedClip(ICommonItem owningItem, TacticalItemDef ammoDef, UIModuleSoldierEquip parentModule)
        {
            try
            {
                if (owningItem == null || ammoDef == null || parentModule == null)
                {
                    return false;
                }

                GroundVehicleModuleDef moduleDef = owningItem.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null || !EnsureModuleAmmo(owningItem.CommonItemData, moduleDef))
                {
                    return false;
                }

                int maxCharges = GetAmmoCapacityForDef(moduleDef, ammoDef);
                int currentCharges = GetAmmoChargesForDef(owningItem.CommonItemData, ammoDef);
                int needed = maxCharges - currentCharges;
                if (needed <= 0)
                {
                    return false;
                }

                int clipCharges = ammoDef.ChargesMax;
                int chargesToLoad = Mathf.Min(needed, clipCharges);
                int remainingCharges = Mathf.Max(clipCharges - chargesToLoad, 0);

                GeoItem geoItem = new GeoItem(ammoDef, 1, -1, null, -100);
                geoItem.CommonItemData.ModifyCharges(-geoItem.CommonItemData.CurrentCharges, false);
                geoItem.CommonItemData.ModifyCharges(chargesToLoad, false);
                owningItem.CommonItemData.Ammo.LoadMagazine(geoItem);

                TryPlacePurchasedAmmoClip(parentModule, ammoDef, remainingCharges);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        [HarmonyPatch(typeof(UIInventorySlotSideButton), "OnSideButtonPressed")]
        public static class UIInventorySlotSideButton_OnSideButtonPressed_MarketplaceAmmoFallback_Patch
        {
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIInventorySlotSideButton.GeneralState> CurrentState =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIInventorySlotSideButton.GeneralState>("_currentState");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIModuleSoldierEquip> ParentModule =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIModuleSoldierEquip>("_parentModule");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, ItemDef> ItemToProduce =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, ItemDef>("_itemToProduce");
            private static readonly AccessTools.FieldRef<UIInventorySlotSideButton, UIInventorySlot> OwningSlot =
                AccessTools.FieldRefAccess<UIInventorySlotSideButton, UIInventorySlot>("_owningSlot");

            public static bool Prefix(UIInventorySlotSideButton __instance)
            {
                try
                {
                    if (__instance == null)
                    {
                        return true;
                    }

                    UIInventorySlotSideButton.GeneralState state = CurrentState(__instance);
                    if (state.Action != UIInventorySlotSideButton.SideButtonAction.LoadAmmo &&
                        state.Action != UIInventorySlotSideButton.SideButtonAction.AddAmmo)
                    {
                        return true;
                    }

                    UIModuleSoldierEquip parentModule = ParentModule(__instance);
                    UIInventorySlot owningSlot = OwningSlot(__instance);
                    ICommonItem owningItem = owningSlot?.Item;

                    WeaponDef weaponDef;
                    TacticalItemDef weaponAmmoDef;
                    if (owningItem != null && IsKaosGunWeapon(owningItem.ItemDef, out weaponDef, out weaponAmmoDef))
                    {
                        if (weaponAmmoDef == null || parentModule == null)
                        {
                            return true;
                        }

                        // 1) Empty/partial KG: use inventory/storage clips first (load into KG)
                        if (IsItemMissingAmmo(owningItem, weaponAmmoDef))
                        {
                            if (HasAmmoInStorage(parentModule, weaponAmmoDef) || HasAmmoInInventory(parentModule, weaponAmmoDef))
                            {
                                if (TryLoadWeaponFromClipInInventoryOrStorage(parentModule, owningItem, weaponAmmoDef))
                                {
                                    owningSlot.UpdateItem();
                                    parentModule.RefreshSideButtons();
                                    return false;
                                }
                            }

                            // 2) No clips available: marketplace fallback (buy 1 and load into KG)
                            if (!HasMarketplaceAmmoStock(weaponAmmoDef))
                            {
                                return true;
                            }

                            if (!TryBuyAmmoClipFromMarketplace(parentModule, weaponAmmoDef))
                            {
                                return true;
                            }

                            if (TryReloadWeaponFromPurchasedClip(owningItem, weaponAmmoDef, parentModule))
                            {
                                owningSlot.UpdateItem();
                                parentModule.RefreshSideButtons();
                                return false;
                            }

                            return true;
                        }

                        // 3) Full KG: move storage clip -> inventory first (one at a time), else marketplace into inventory
                        if (HasAmmoInStorage(parentModule, weaponAmmoDef) && HasInventorySpace(parentModule, weaponAmmoDef))
                        {
                            ICommonItem ammoItem;
                            if (TryTakeAmmoFromStorageList(parentModule, weaponAmmoDef, out ammoItem))
                            {
                                parentModule.InventoryList.AddItem(ammoItem, null, null);
                                parentModule.RefreshSideButtons();
                                return false;
                            }
                        }

                        if (!HasMarketplaceAmmoStock(weaponAmmoDef))
                        {
                            return true;
                        }

                        if (!TryBuyAmmoClipFromMarketplace(parentModule, weaponAmmoDef))
                        {
                            return true;
                        }

                        if (TryPlacePurchasedAmmoClipInInventory(parentModule, weaponAmmoDef))
                        {
                            parentModule.RefreshSideButtons();
                            return false;
                        }

                        return true;
                    }

                    GroundVehicleModuleDef moduleDef = owningItem?.ItemDef as GroundVehicleModuleDef;
                    if (moduleDef == null || !EnsureModuleAmmo(owningItem.CommonItemData, moduleDef))
                    {
                        return true;
                    }

                    List<ModuleAmmoEntry> moduleAmmoEntries = GetModuleAmmoEntries(owningItem.CommonItemData, moduleDef);
                    List<ModuleAmmoEntry> missingEntries = moduleAmmoEntries
                        .Where(entry => entry.MaxCharges > 0 && entry.CurrentCharges < entry.MaxCharges)
                        .ToList();

                    if (missingEntries.Count == 0)
                    {
                        return HandleFullModuleSideButton(parentModule, owningItem, __instance);
                    }

                    ModuleAmmoEntry selectedEntry = missingEntries
                        .FirstOrDefault(entry => HasAmmoInStorage(parentModule, entry.AmmoDef));

                    bool useStorage = selectedEntry.AmmoDef != null;

                    if (!useStorage)
                    {
                        selectedEntry = missingEntries.FirstOrDefault(entry => HasAmmoInInventory(parentModule, entry.AmmoDef));
                    }

                    if (selectedEntry.AmmoDef != null)
                    {
                        TacticalItemDef ammoDef = selectedEntry.AmmoDef;
                        ICommonItem ammoItem = null;
                        UIInventoryList sourceList = null;

                        if (HasAmmoInStorage(parentModule, ammoDef))
                        {
                            ammoItem = parentModule.StorageList.UnfilteredItems
                                .FirstOrDefault(item => item.ItemDef == ammoDef)?.GetSingleItem();
                            sourceList = parentModule.StorageList;
                        }
                        else if (HasAmmoInInventory(parentModule, ammoDef))
                        {
                            ammoItem = parentModule.InventoryList.UnfilteredItems
                                .FirstOrDefault(item => item.ItemDef == ammoDef);
                            sourceList = parentModule.InventoryList;
                        }

                        if (ammoItem != null && sourceList != null)
                        {
                            if (sourceList == parentModule.InventoryList)
                            {
                                parentModule.StorageList.TryLoadItemWithItem(owningItem, ammoItem, null);
                                if (ammoItem.CommonItemData.IsEmpty())
                                {
                                    sourceList.RemoveItem(ammoItem, null);
                                }
                            }
                            else
                            {
                                sourceList.RemoveItem(ammoItem, null);
                                parentModule.StorageList.TryLoadItemWithItem(owningItem, ammoItem, null);

                                if (!ammoItem.CommonItemData.IsEmpty())
                                {
                                    sourceList.AddItem(ammoItem, null, null);
                                }
                            }

                            owningSlot.UpdateItem();
                            parentModule.RefreshSideButtons();
                            return false;
                        }
                    }

                    TacticalItemDef requestedAmmoDef = ItemToProduce(__instance) as TacticalItemDef;
                    if (requestedAmmoDef == null)
                    {
                        return true;
                    }

                    if (missingEntries.Count == 1)
                    {
                        if (!HasMarketplaceAmmoStock(requestedAmmoDef))
                        {
                            return true;
                        }

                        if (!TryBuyAmmoClipFromMarketplace(parentModule, requestedAmmoDef))
                        {
                            return true;
                        }

                        if (TryReloadModuleFromPurchasedClip(owningItem, requestedAmmoDef, parentModule))
                        {
                            parentModule.RefreshSideButtons();
                            return false;
                        }

                        return true;
                    }

                    MarketplacePromptHolder promptHolder;
                    if (!MarketplacePromptOverrides.TryGetValue(__instance, out promptHolder) || promptHolder == null || promptHolder.AmmoDefs.Count < 2)
                    {
                        return true;
                    }

                    ShowMarketplaceAmmoChoicePrompt(parentModule, owningItem, promptHolder.AmmoDefs[0], promptHolder.AmmoDefs[1]);
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }

            private static bool HandleFullModuleSideButton(UIModuleSoldierEquip parentModule, ICommonItem owningItem, UIInventorySlotSideButton instance)
            {
                if (parentModule == null || owningItem == null || instance == null)
                {
                    return true;
                }

                TacticalItemDef ammoDef = ItemToProduce(instance) as TacticalItemDef;
                if (ammoDef == null)
                {
                    return true;
                }

                if (HasAmmoInStorage(parentModule, ammoDef) && HasInventorySpace(parentModule, ammoDef))
                {
                    ICommonItem ammoItem;
                    if (TryTakeAmmoFromStorageList(parentModule, ammoDef, out ammoItem))
                    {
                        parentModule.InventoryList.AddItem(ammoItem, null, null);
                        parentModule.RefreshSideButtons();
                        return false;
                    }
                }

                if (!HasMarketplaceAmmoStock(ammoDef))
                {
                    return true;
                }

                MarketplacePromptHolder promptHolder;
                if (MarketplacePromptOverrides.TryGetValue(instance, out promptHolder) && promptHolder != null && promptHolder.AmmoDefs.Count >= 2)
                {
                    ShowMarketplaceAmmoChoicePromptForInventory(parentModule, promptHolder.AmmoDefs[0], promptHolder.AmmoDefs[1]);
                    return false;
                }

                if (!TryBuyAmmoClipFromMarketplace(parentModule, ammoDef))
                {
                    return true;
                }

                if (TryPlacePurchasedAmmoClipInInventory(parentModule, ammoDef))
                {
                    parentModule.RefreshSideButtons();
                    return false;
                }

                return true;
            }
        }

        private static string LimitToTwoWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string[] words = text
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 2)
            {
                return text.Trim();
            }

            return words[0] + " " + words[1];
        }

        private static void ShowMarketplaceAmmoChoicePrompt(
            UIModuleSoldierEquip parentModule,
            ICommonItem owningItem,
            TacticalItemDef firstAmmo,
            TacticalItemDef secondAmmo)
        {
            string ammoName1 = firstAmmo?.ViewElementDef != null ? firstAmmo.ViewElementDef.DisplayName1.Localize() : firstAmmo?.name;
            string ammoName2 = secondAmmo?.ViewElementDef != null ? secondAmmo.ViewElementDef.DisplayName1.Localize() : secondAmmo?.name;

            ammoName1 = LimitToTwoWords(ammoName1);
            ammoName2 = LimitToTwoWords(ammoName2);

            if (string.IsNullOrWhiteSpace(ammoName1) || string.IsNullOrWhiteSpace(ammoName2))
            {
                return;
            }

            /* string promptText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_CHOOSE");
             if (string.IsNullOrWhiteSpace(promptText) || promptText == "TFTV_MARKETPLACE_AMMO_CHOOSE")
             {
                 promptText = "Choose ammo to purchase:";
             }*/

            Dictionary<MessageBoxButtons, string> buttonLabels = new Dictionary<MessageBoxButtons, string>
            {
                { MessageBoxButtons.Yes, ammoName1 },
                { MessageBoxButtons.No, ammoName2 },
                { MessageBoxButtons.Cancel, "CANCEL" }
            };

            GameUtl.GetMessageBox().ShowSimplePrompt(
                "",
                MessageBoxIcon.Question,
                MessageBoxButtons.YesNoCancel,
                buttonLabels,
                result =>
                {
                    try
                    {
                        if (result.DialogResult == MessageBoxResult.Yes)
                        {
                            TryBuyAndReloadAmmoChoice(parentModule, owningItem, firstAmmo);
                        }
                        else if (result.DialogResult == MessageBoxResult.No)
                        {
                            TryBuyAndReloadAmmoChoice(parentModule, owningItem, secondAmmo);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                });
        }

        private static void ShowMarketplaceAmmoChoicePromptForInventory(
            UIModuleSoldierEquip parentModule,
            TacticalItemDef firstAmmo,
            TacticalItemDef secondAmmo)
        {
            string ammoName1 = firstAmmo?.ViewElementDef != null ? firstAmmo.ViewElementDef.DisplayName1.Localize() : firstAmmo?.name;
            string ammoName2 = secondAmmo?.ViewElementDef != null ? secondAmmo.ViewElementDef.DisplayName1.Localize() : secondAmmo?.name;

            ammoName1 = LimitToTwoWords(ammoName1);
            ammoName2 = LimitToTwoWords(ammoName2);

            if (string.IsNullOrWhiteSpace(ammoName1) || string.IsNullOrWhiteSpace(ammoName2))
            {
                return;
            }

            /* string promptText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_CHOOSE");
             if (string.IsNullOrWhiteSpace(promptText) || promptText == "TFTV_MARKETPLACE_AMMO_CHOOSE")
             {
                 promptText = "Choose ammo to purchase:";
             }*/

            Dictionary<MessageBoxButtons, string> buttonLabels = new Dictionary<MessageBoxButtons, string>
            {
                { MessageBoxButtons.Yes, ammoName1 },
                { MessageBoxButtons.No, ammoName2 },
                { MessageBoxButtons.Cancel, "CANCEL" }
            };

            GameUtl.GetMessageBox().ShowSimplePrompt(
                "",
                MessageBoxIcon.Question,
                MessageBoxButtons.YesNoCancel,
                buttonLabels,
                result =>
                {
                    try
                    {
                        if (result.DialogResult == MessageBoxResult.Yes)
                        {
                            TryBuyAndStoreAmmoChoice(parentModule, firstAmmo);
                        }
                        else if (result.DialogResult == MessageBoxResult.No)
                        {
                            TryBuyAndStoreAmmoChoice(parentModule, secondAmmo);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                });
        }

        private static void TryBuyAndReloadAmmoChoice(UIModuleSoldierEquip parentModule, ICommonItem owningItem, TacticalItemDef ammoDef)
        {
            if (ammoDef == null || parentModule == null || owningItem == null)
            {
                return;
            }

            if (!HasMarketplaceAmmoStock(ammoDef))
            {
                return;
            }

            if (!TryBuyAmmoClipFromMarketplace(parentModule, ammoDef))
            {
                return;
            }

            if (TryReloadModuleFromPurchasedClip(owningItem, ammoDef, parentModule))
            {
                parentModule.RefreshSideButtons();
            }
        }

        private static void TryBuyAndStoreAmmoChoice(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (ammoDef == null || parentModule == null)
            {
                return;
            }

            if (!HasMarketplaceAmmoStock(ammoDef))
            {
                return;
            }

            if (!TryBuyAmmoClipFromMarketplace(parentModule, ammoDef))
            {
                return;
            }

            if (TryPlacePurchasedAmmoClipInInventory(parentModule, ammoDef))
            {
                parentModule.RefreshSideButtons();
            }
        }

        public static bool ReloadModuleAmmo(GeoItem item, TacticalItemDef ammoDef)
        {
            if (item == null || ammoDef == null)
            {
                return false;
            }
            var moduleDef = item.ItemDef as GroundVehicleModuleDef;
            if (moduleDef == null || !EnsureModuleAmmo(item.CommonItemData, moduleDef))
            {
                return false;
            }
            int maxCharges = GetAmmoCapacityForDef(moduleDef, ammoDef);
            int currentCharges = GetAmmoChargesForDef(item.CommonItemData, ammoDef);
            int chargesDelta = maxCharges - currentCharges;
            if (chargesDelta <= 0 || ammoDef.ChargesMax <= 0)
            {
                return false;
            }
            while (chargesDelta > 0)
            {
                GeoItem geoItem = new GeoItem(ammoDef, 1, -1, null, -100);
                geoItem.CommonItemData.ModifyCharges(-geoItem.CommonItemData.CurrentCharges, false);
                int num = Math.Min(ammoDef.ChargesMax, chargesDelta);
                geoItem.CommonItemData.ModifyCharges(num, false);
                item.CommonItemData.Ammo.LoadMagazine(geoItem);
                chargesDelta -= num;
            }
            return true;
        }

        [HarmonyPatch(typeof(CommonItemData), "SetOwnerItem")]
        public static class CommonItemData_SetOwnerItem_Patch
        {
            public static void Postfix(CommonItemData __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                if (__instance == null)
                {
                    return;
                }

                var moduleDef = __instance.ItemDef as GroundVehicleModuleDef;
                if (moduleDef != null && moduleDef.GetSubWeapons().Count() > 0)
                {
                    EnsureModuleAmmo(__instance, moduleDef);
                }
            }
        }

        [HarmonyPatch(typeof(UIInventorySlot), "UpdateItem")]
        public static class UIInventorySlot_UpdateItem_Patch
        {
            public static void Postfix(UIInventorySlot __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                if (__instance == null)
                {
                    return;
                }
                ICommonItem item = __instance.Item;
                if (item == null)
                {
                    return;
                }
                var moduleDef = item.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null || !EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    return;
                }

                GroundVehicleModule tacticalModule = item as GroundVehicleModule;
                List<Weapon> subWeapons = null;
                if (tacticalModule != null)
                {
                    subWeapons = tacticalModule.GetAllSubaddons().OfType<Weapon>().ToList();
                    if (subWeapons.Count > 0)
                    {
                        MoveSubweaponAmmoToModule(tacticalModule, subWeapons);
                    }
                }

                List<ModuleAmmoEntry> moduleAmmoEntries = GetModuleAmmoEntries(item.CommonItemData, moduleDef);
                if (moduleAmmoEntries.Count == 0)
                {
                    if (tacticalModule != null && subWeapons != null && subWeapons.Count > 0)
                    {
                        MoveModuleAmmoToSubweapons(tacticalModule, subWeapons);
                    }
                    return;
                }
                __instance.AmmoInfoRoot.SetActive(true);

                Image image = __instance.AmmoInfoRoot.GetComponentInChildren<Image>();

                if (image != null)
                {
                    image.enabled = false;
                }

                __instance.ExtraAmmoInfoRoot.SetActive(false);

                __instance.AmmoInfoText.text = string.Join(" ", moduleAmmoEntries.Select((ModuleAmmoEntry entry) => string.Format("{0}/<color=#{1}>{2}</color>", entry.CurrentCharges, ColorUtility.ToHtmlStringRGBA(__instance.PartialWeaponAmmoColor), entry.MaxCharges)).ToArray<string>());

                if (__instance.IsActorInventory)
                {
                    AccessTools.Method(typeof(UIInventorySlot), "HighlightItemWithAmmo")?.Invoke(__instance, null);
                }

                if (tacticalModule != null && subWeapons != null && subWeapons.Count > 0)
                {
                    MoveModuleAmmoToSubweapons(tacticalModule, subWeapons);
                }
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), "TryLoadItemWithItem")]
        public static class UIInventoryList_TryLoadItemWithItem_Patch
        {
            public static bool Prefix(UIInventoryList __instance, ICommonItem item, ICommonItem ammoItem, UIInventorySlot ammoSlot, ref bool __result)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                var moduleDef = (item != null) ? (item.ItemDef as GroundVehicleModuleDef) : null;
                TacticalItemDef tacticalItemDef = (ammoItem != null) ? (ammoItem.ItemDef as TacticalItemDef) : null;
                if (moduleDef == null || tacticalItemDef == null)
                {
                    return true;
                }
                if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                {
                    return true;
                }
                int maxCharges = GetAmmoCapacityForDef(moduleDef, tacticalItemDef);
                if (maxCharges <= 0)
                {
                    return true;
                }
                int currentCharges = GetAmmoChargesForDef(item.CommonItemData, tacticalItemDef);
                while (!ammoItem.CommonItemData.IsEmpty() && currentCharges < maxCharges)
                {
                    int neededCharges = maxCharges - currentCharges;
                    ICommonItem commonItem = ammoItem.GetSingleItem().Clone();
                    int availableCharges = commonItem.CommonItemData.TotalCharges;
                    int chargesToLoad = Math.Min(neededCharges, availableCharges);
                    if (chargesToLoad < availableCharges)
                    {
                        commonItem.CommonItemData.ModifyCharges(-commonItem.CommonItemData.CurrentCharges, false);
                        commonItem.CommonItemData.ModifyCharges(chargesToLoad, false);
                    }
                    item.CommonItemData.Ammo.LoadMagazine(commonItem);
                    if (chargesToLoad >= availableCharges)
                    {
                        ammoItem.CommonItemData.Subtract(commonItem);
                    }
                    else
                    {
                        ammoItem.CommonItemData.ModifyCharges(-chargesToLoad, false);
                    }
                    currentCharges += chargesToLoad;
                    Action onItemLoaded = __instance.OnItemLoaded;
                    if (onItemLoaded != null)
                    {
                        onItemLoaded();
                    }
                }
                if (ammoItem.CommonItemData.IsEmpty() && ammoSlot != null)
                {
                    ammoSlot.ParentList.RemoveItem(ammoItem, ammoSlot);
                }
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIInventoryList), "TryStripAmmo")]
        public static class UIInventoryList_TryStripAmmo_Patch
        {
            public static bool Prefix(UIInventoryList __instance, ICommonItem item, UIInventorySlot itemSlot)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                var moduleDef = (item != null) ? (item.ItemDef as GroundVehicleModuleDef) : null;
                if (moduleDef == null || item.CommonItemData.Ammo == null)
                {
                    return true;
                }
                List<ICommonItem> list = item.CommonItemData.Ammo.UnloadMagazines();
                foreach (ICommonItem commonItem in list)
                {
                    if (__instance.ShouldHidePartialMagazines && commonItem.CommonItemData.CurrentCharges != commonItem.ItemDef.ChargesMax)
                    {
                        GeoItem geoItem = commonItem as GeoItem;
                        if (geoItem == null)
                        {
                            continue;
                        }
                        __instance.PartialMagazines.AddItem(geoItem);
                        if (__instance.PartialMagazines.Items[geoItem.ItemDef].CommonItemData.Count > 1 || __instance.PartialMagazines.Items[geoItem.ItemDef].CommonItemData.CurrentCharges == commonItem.ItemDef.ChargesMax)
                        {
                            ICommonItem singleItem = __instance.PartialMagazines.Items[geoItem.ItemDef].GetSingleItem();
                            __instance.PartialMagazines.Items[geoItem.ItemDef].CommonItemData.Subtract(singleItem);
                            __instance.AddItem(singleItem, null, null);
                        }
                    }
                    else
                    {
                        __instance.AddItem(commonItem, null, null);
                    }
                }
                if (list.Any<ICommonItem>())
                {
                    Action onItemUnloaded = __instance.OnItemUnloaded;
                    if (onItemUnloaded != null)
                    {
                        onItemUnloaded();
                    }
                }
                if (itemSlot != null)
                {
                    itemSlot.UpdateItem();
                }
                return false;
            }
        }



        [HarmonyPatch(typeof(TacticalItem), "ToItemData")]
        public static class TacticalItem_ToItemData_Patch
        {
            public static void Prefix(TacticalItem __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                var module = __instance as GroundVehicleModule;
                if (module == null)
                {
                    return;
                }
                List<Weapon> list = module.GetAllSubaddons().OfType<Weapon>().ToList<Weapon>();
                if (list.Count == 0)
                {
                    return;
                }
                MoveSubweaponAmmoToModule(module, list);
            }

            public static void Postfix(TacticalItem __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                var module = __instance as GroundVehicleModule;
                if (module == null)
                {
                    return;
                }
                List<Weapon> list = module.GetAllSubaddons().OfType<Weapon>().ToList<Weapon>();
                if (list.Count == 0)
                {
                    return;
                }
                MoveModuleAmmoToSubweapons(module, list);
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "ProcessInstanceData")]
        public static class TacticalActor_ProcessInstanceData_Patch
        {
            public static void Postfix(TacticalActor __instance)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return;
                }

                if (__instance == null || __instance.AddonsManager == null || __instance.AddonsManager.RootAddon == null)
                {
                    return;
                }
                foreach (GroundVehicleModule groundVehicleModule in __instance.AddonsManager.RootAddon.OfType<GroundVehicleModule>())
                {
                    List<Weapon> list = groundVehicleModule.GetAllSubaddons().OfType<Weapon>().ToList<Weapon>();
                    if (list.Count == 0)
                    {
                        continue;
                    }
                    MoveModuleAmmoToSubweapons(groundVehicleModule, list);
                }
            }
        }
    }
}