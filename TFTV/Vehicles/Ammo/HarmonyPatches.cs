using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TFTV;
using TFTV.Vehicles.Ammo;
using UnityEngine;
using UnityEngine.UI;

namespace PhoenixPoint.Modding
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
        private sealed class AmmoDefHolder
        {
            public TacticalItemDef AmmoDef;
        }

        private sealed class TooltipOverrideHolder
        {
            public string TipText;
        }

        private static readonly ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder> ReplenishAmmoDefs = new ConditionalWeakTable<GeoManufactureItem, AmmoDefHolder>();
        private static readonly ConditionalWeakTable<UIInventorySlotSideButton, TooltipOverrideHolder> SideButtonTooltipOverrides = new ConditionalWeakTable<UIInventorySlotSideButton, TooltipOverrideHolder>();

        private struct ModuleAmmoEntry
        {
            public TacticalItemDef AmmoDef;
            public int CurrentCharges;
            public int MaxCharges;
        }

        private static List<TacticalItemDef> GetModuleAmmoDefs(GroundVehicleModuleDef moduleDef)
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

        private static int GetAmmoChargesForDef(CommonItemData commonItemData, TacticalItemDef ammoDef)
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

        private static int GetAmmoCapacityForDef(GroundVehicleModuleDef moduleDef, TacticalItemDef ammoDef)
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

        private static bool EnsureModuleAmmo(CommonItemData commonItemData, GroundVehicleModuleDef moduleDef)
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

        private static TacticalItemDef GetPreferredAmmoDef(IReadOnlyList<ModuleAmmoEntry> ammoEntries)
        {
            if (ammoEntries == null || ammoEntries.Count == 0)
            {
                return null;
            }
            foreach (ModuleAmmoEntry moduleAmmoEntry in ammoEntries)
            {
                if (moduleAmmoEntry.CurrentCharges < moduleAmmoEntry.MaxCharges)
                {
                    return moduleAmmoEntry.AmmoDef;
                }
            }
            return ammoEntries[0].AmmoDef;
        }

        private static TacticalItemDef GetPreferredAmmoDef(IReadOnlyList<ModuleAmmoEntry> ammoEntries, UIModuleSoldierEquip parentModule)
        {
            if (ammoEntries == null || ammoEntries.Count == 0)
            {
                return null;
            }
            foreach (ModuleAmmoEntry moduleAmmoEntry in ammoEntries)
            {
                if (moduleAmmoEntry.CurrentCharges < moduleAmmoEntry.MaxCharges && HasAmmoAvailable(parentModule, moduleAmmoEntry.AmmoDef))
                {
                    return moduleAmmoEntry.AmmoDef;
                }
            }
            return GetPreferredAmmoDef(ammoEntries);
        }

        private static bool HasAmmoAvailable(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef)
        {
            if (parentModule == null || ammoDef == null)
            {
                return false;
            }
            if (parentModule.StorageList.UnfilteredItems.Any((ICommonItem item) => item.ItemDef == ammoDef))
            {
                return true;
            }
            if (parentModule.StorageList.PartialMagazines.Items.ContainsKey(ammoDef))
            {
                return true;
            }
            if (parentModule.InventoryList.UnfilteredItems.Any((ICommonItem item) => item.ItemDef == ammoDef))
            {
                return true;
            }
            if (!parentModule.IsVehicle && parentModule.ReadyList.UnfilteredItems.Any((ICommonItem item) => item.ItemDef == ammoDef))
            {
                return true;
            }
            return false;
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

        private static bool IsMarketplaceAmmoRelevantToScope(UIModuleSoldierEquip parentModule, ICommonItem owningItem)
        {
            try
            {
                if (parentModule == null || owningItem == null || owningItem.ItemDef == null)
                {
                    return false;
                }

                ItemDef itemDef = owningItem.ItemDef;

                if (itemDef.Tags != null && TFTVChangesToDLC5.TFTVKaosGuns._kGTag != null && itemDef.Tags.Contains(TFTVChangesToDLC5.TFTVKaosGuns._kGTag))
                {
                    return true;
                }

                if (itemDef.name != null && itemDef.name.StartsWith("KS_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return IsVehicleWeaponSoldInMarketplace(itemDef);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static bool IsVehicleWeaponSoldInMarketplace(ItemDef itemDef)
        {
            if (itemDef == null)
            {
                return false;
            }

            if(itemDef is GroundVehicleModuleDef groundVehicleModuleDef && groundVehicleModuleDef.GetSubWeapons().Any())
            {
                return true;
            }

            if(itemDef.Tags != null && VehiclesAmmoMain.MarketplaceGroundVehicleWeapon != null && itemDef.Tags.Contains(VehiclesAmmoMain.MarketplaceGroundVehicleWeapon))
            {
                return true;
            }

            return false;

           /* IVehicleEquipment vehicleEquipment = itemDef as IVehicleEquipment;
            return vehicleEquipment != null && vehicleEquipment.GetEquipmentType() == GroundVehicleEquipmentType.Weapon;*/
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

                    if (item.ItemDef is GroundVehicleModuleDef moduleDef)
                    {
                        if (!EnsureModuleAmmo(item.CommonItemData, moduleDef))
                        {
                            return true;
                        }

                        return TryComputeVehicleModuleState(__instance, parentModule, slot, item, moduleDef, ref __result);
                    }

                    return TryComputeNonModuleMarketplaceAmmoState(__instance, parentModule, slot, item, ref __result);
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

                bool canRepair = (bool)AccessTools.Method(typeof(UIInventorySlotSideButton), "CanRepair")
                    .Invoke(instance, new object[] { item.ItemDef });

                if (canRepair)
                {
                    float equippedItemHealth = parentModule.ModuleData.PrimarySoldierData.GetEquippedItemHealth(item.ItemDef);
                    if (equippedItemHealth >= 0f && equippedItemHealth < 1f)
                    {
                        result.Action = UIInventorySlotSideButton.SideButtonAction.Repair;
                        if (parentModule.ModuleData.Wallet != null)
                        {
                            ResourcePack repairCost = GeoCharacter.GetRepairCost(item.ItemDef, equippedItemHealth);
                            result.State = parentModule.ModuleData.Wallet.HasResources(repairCost)
                                ? UIInventorySlotSideButton.SideButtonState.ActionNeededAffordable
                                : UIInventorySlotSideButton.SideButtonState.ActionNeededUnaffordable;
                        }
                        return false;
                    }
                }

                if (item.CommonItemData.Ammo == null)
                {
                    return true;
                }

                List<ModuleAmmoEntry> moduleAmmoEntries = GetModuleAmmoEntries(item.CommonItemData, moduleDef);
                TacticalItemDef ammoDef = GetPreferredAmmoDef(moduleAmmoEntries, parentModule);
                if (ammoDef == null)
                {
                    return true;
                }

                ItemToProduce(instance) = ammoDef;

                ModuleAmmoEntry moduleAmmoEntry = moduleAmmoEntries.FirstOrDefault(entry => entry.AmmoDef == ammoDef);
                if (moduleAmmoEntry.MaxCharges > 0 && moduleAmmoEntry.CurrentCharges < moduleAmmoEntry.MaxCharges)
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

                if (destinationList != parentModule.StorageList &&
                    parentModule.StorageList.UnfilteredItems.Any(storageItem => storageItem.ItemDef == ammoDef))
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededFree;
                    return false;
                }

                if (IsVehicleWeaponSoldInMarketplace(moduleDef) && TrySetMarketplaceAmmoState(instance, parentModule, ammoDef, ref result))
                {
                    return false;
                }

                if (!parentModule.CanManufacture(ammoDef))
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededImpossible;
                    return false;
                }

                if (ammoDef.ManufacturePointsCost > 0f)
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededNeedsTime;
                    return false;
                }

                if (!parentModule.ModuleData.Wallet.HasResources(ammoDef.ManufacturePrice))
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededUnaffordable;
                    return false;
                }

                result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededAffordable;
                return false;
            }

            private static bool TryComputeNonModuleMarketplaceAmmoState(
               UIInventorySlotSideButton instance,
               UIModuleSoldierEquip parentModule,
               UIInventorySlot slot,
               ICommonItem item,
               ref UIInventorySlotSideButton.GeneralState result)
            {
                if (!IsMarketplaceAmmoRelevantToScope(parentModule, item))
                {
                    return true;
                }

                EquipmentDef equipmentDef = item.ItemDef as EquipmentDef;
                if (equipmentDef == null || equipmentDef.CompatibleAmmunition == null || equipmentDef.CompatibleAmmunition.Length == 0)
                {
                    return true;
                }

                TacticalItemDef ammoDef = equipmentDef.CompatibleAmmunition[0];
                if (ammoDef == null)
                {
                    return true;
                }

                result = default(UIInventorySlotSideButton.GeneralState);
                ItemToProduce(instance) = ammoDef;

                if (item.CommonItemData?.Ammo != null && item.CommonItemData.Ammo.CurrentCharges < item.ItemDef.ChargesMax)
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

                if (HasAmmoInStorage(parentModule, ammoDef))
                {
                    result.State = UIInventorySlotSideButton.SideButtonState.ActionNeededFree;
                    return false;
                }

                TrySetMarketplaceAmmoState(instance, parentModule, ammoDef, ref result);

                return false;
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

                string outOfStockText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_OUT_OF_STOCK");
                string ammoName = ammoDef.ViewElementDef != null ? ammoDef.ViewElementDef.DisplayName1.Localize() : ammoDef.name;

                int daysToRotation;
                if (!TryGetMarketplaceDaysToRotation(out daysToRotation))
                {
                    daysToRotation = 1;
                }

                outOfStockText = outOfStockText.Replace("{0}", ammoName);
                outOfStockText = outOfStockText.Replace("{1}", daysToRotation.ToString());

                SetTooltipOverride(instance, outOfStockText);
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

            string buyText = TFTVCommonMethods.ConvertKeyToString("TFTV_MARKETPLACE_AMMO_BUY");
            string ammoNameInStock = ammoDef.ViewElementDef != null ? ammoDef.ViewElementDef.DisplayName1.Localize() : ammoDef.name;

            int matsCost = mpCost.ByResourceType(ResourceType.Materials).RoundedValue;

            buyText = buyText.Replace("{0}", stockCount.ToString());
            buyText = buyText.Replace("{1}", ammoNameInStock);
            buyText = buyText.Replace("{2}", matsCost.ToString());

            SetTooltipOverride(instance, buyText);
            return true;
        }

        private static bool TryPlacePurchasedAmmoClip(UIModuleSoldierEquip parentModule, TacticalItemDef ammoDef, UIInventorySlotSideButton.SideButtonAction action)
        {
            if (parentModule == null || ammoDef == null)
            {
                return false;
            }

            GeoItem clip = new GeoItem(ammoDef, 1, -1, null, -100);

            if (action == UIInventorySlotSideButton.SideButtonAction.AddAmmo && parentModule.InventoryList != null)
            {
                parentModule.InventoryList.AddItem(clip, null, null);
                return true;
            }

            parentModule.StorageList.AddItem(clip);
            return true;
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

                    if (!IsMarketplaceAmmoRelevantToScope(parentModule, owningItem))
                    {
                        return true;
                    }

                    TacticalItemDef ammoDef = ItemToProduce(__instance) as TacticalItemDef;
                    if (ammoDef == null)
                    {
                        return true;
                    }

                    if (HasAmmoInStorage(parentModule, ammoDef))
                    {
                        return true;
                    }

                    if (!HasMarketplaceAmmoStock(ammoDef))
                    {
                        return true;
                    }

                    if (!TryBuyAmmoClipFromMarketplace(parentModule, ammoDef))
                    {
                        return true;
                    }

                    if (state.Action == UIInventorySlotSideButton.SideButtonAction.LoadAmmo)
                    {
                        if (TryReloadEquipmentFromPurchasedClip(owningItem))
                        {
                            parentModule.RefreshSideButtons();
                            return false;
                        }

                        return true;
                    }

                    if (!TryPlacePurchasedAmmoClip(parentModule, ammoDef, state.Action))
                    {
                        parentModule.StorageList.AddItem(new GeoItem(ammoDef, 1, -1, null, -100));
                    }

                    parentModule.RefreshSideButtons();
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        private static bool TryReloadEquipmentFromPurchasedClip(ICommonItem owningItem)
        {
            try
            {
                Equipment equipment = owningItem as Equipment;
                if (equipment == null || equipment.CommonItemData == null || equipment.CommonItemData.Ammo == null)
                {
                    return false;
                }

                int needed = equipment.ItemDef.ChargesMax - equipment.CommonItemData.CurrentCharges;
                if (needed <= 0)
                {
                    return false;
                }

                equipment.CommonItemData.Ammo.ReloadCharges(needed, true);
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static bool ReloadModuleAmmo(GeoItem item, TacticalItemDef ammoDef)
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
                if (moduleDef != null)
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
                int maxCharges = tacticalItemDef.ChargesMax;
                if (maxCharges <= 0)
                {
                    return true;
                }
                int currentCharges = GetAmmoChargesForDef(item.CommonItemData, tacticalItemDef);
                while (!ammoItem.CommonItemData.IsEmpty() && currentCharges < maxCharges)
                {
                    ICommonItem commonItem = ammoItem.GetSingleItem().Clone();
                    item.CommonItemData.Ammo.LoadMagazine(commonItem);
                    ammoItem.CommonItemData.Subtract(commonItem);
                    currentCharges += commonItem.CommonItemData.TotalCharges;
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

        [HarmonyPatch(typeof(UIModuleReplenish), "AddMissingAmmo")]
        public static class UIModuleReplenish_AddMissingAmmo_Patch
        {
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
                    ResourcePack repairCost = GeoCharacter.GetRepairCost(tacticalItemDef, num);
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
                    bool flag2 = geoPhoenixFaction.Wallet.HasResources(repairCost) && tacticalItemDef.Tags.Contains(manufacturableTag) && geoPhoenixFaction.Manufacture.Contains(tacticalItemDef);
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

        [HarmonyPatch(typeof(UIModuleReplenish), "SingleItemReloadAndRefresh")]
        public static class UIModuleReplenish_SingleItemReloadAndRefresh_Patch
        {
            public static bool Prefix(UIModuleReplenish __instance, GeoManufactureItem item)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn)
                {
                    return true;
                }

                if (item == null)
                {
                    return true;
                }
                AmmoDefHolder ammoDefHolder;
                if (!ReplenishAmmoDefs.TryGetValue(item, out ammoDefHolder))
                {
                    return true;
                }
                GeoItem item2 = item.GetComponent<ReplenishmentElementController>().Item;
                if (ReloadModuleAmmo(item2, ammoDefHolder.AmmoDef))
                {
                    AccessTools.Method(typeof(UIModuleReplenish), "RemoveFromList")?.Invoke(__instance, new object[] { item, true });
                    AccessTools.Method(typeof(UIModuleReplenish), "RefreshItemList")?.Invoke(__instance, null);
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