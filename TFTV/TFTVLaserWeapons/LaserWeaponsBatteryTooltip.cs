using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TFTV.TFTVLaserWeapons
{
    internal class LaserWeaponsBatteryTooltip
    {
        private static string _laserBatteryPartialCharge = "TFTV_LASER_BATTERY_PARTIAL";
        private static string _laserBatteryMaxCharges = "TFTV_LASER_BATTERY_MAX_CHARGES";
        private static string _chargesPerPack = "TFTV_LASER_BATTERY_PER_PACK_CHARGES";

        private static void PopulateLocalizations()
        {
            try
            {
                if (_laserBatteryPartialCharge == null)
                {
                    _laserBatteryPartialCharge = TFTVCommonMethods.ConvertKeyToString("TFTV_LASER_BATTERY_PARTIAL");
                }
                if (_laserBatteryMaxCharges == null)
                {
                    _laserBatteryMaxCharges = TFTVCommonMethods.ConvertKeyToString("TFTV_LASER_BATTERY_MAX_CHARGES");
                }
                if (_chargesPerPack == null)
                {
                    _chargesPerPack = TFTVCommonMethods.ConvertKeyToString("TFTV_LASER_BATTERY_PER_PACK_CHARGES");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        [HarmonyPatch(typeof(UIInventoryTooltip), "AddAmmoStats")]
        public static class UIInventoryTooltip_AddAmmoStats_Patch
        {
            private const string LaserBatteryPackName = "TFTV_LaserBatteryPack_ItemDef";

            public static void Postfix(UIInventoryTooltip __instance, UIInventorySlot hoveredItem, List<ComparableData> itemStats)
            {
                TacticalItemDef tacItemDef = hoveredItem != null && hoveredItem.Item != null ? hoveredItem.Item.ItemDef as TacticalItemDef : null;
                if (tacItemDef == null)
                {
                    return;
                }
                if (tacItemDef.name != LaserBatteryPackName)
                {
                    return;
                }


                foreach (ComparableData comparableData in itemStats)
                {
                    if (comparableData != null && comparableData.localization == __instance.PartialMagazineAmmoStatName)
                    {
                        comparableData.localization = new LocalizedTextBind(_laserBatteryPartialCharge);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UIItemTooltip), "SetTacItemStats")]
        public static class UIItemTooltip_SetTacItemStats_Patch
        {
            private const string LaserBatteryPackName = "TFTV_LaserBatteryPack_ItemDef";

            public static void Postfix(UIItemTooltip __instance, TacticalItemDef tacItemDef)
            {
                if (tacItemDef == null || tacItemDef.name != LaserBatteryPackName)
                {
                    return;
                }



                OrderedDictionary orderedStats = AccessTools.Field(typeof(UIItemTooltip), "_orderedStatsDictionary").GetValue(__instance) as OrderedDictionary;
                if (orderedStats == null)
                {
                    return;
                }

                UpdateStat(orderedStats, __instance.AmmoCapacityStatName, _laserBatteryMaxCharges, null);
                UpdateStat(orderedStats, __instance.BurstsPerFullMagazineStatName, _chargesPerPack, "6/3/2/1");
            }

            private static void UpdateStat(OrderedDictionary orderedStats, LocalizedTextBind statName, string newLabel, string newValue)
            {
                if (statName == null || !orderedStats.Contains(statName.LocalizationKey))
                {
                    return;
                }

                ComparableData comparableData = orderedStats[statName.LocalizationKey] as ComparableData;
                if (comparableData == null)
                {
                    return;
                }

                comparableData.localization = new LocalizedTextBind(newLabel);
                if (!string.IsNullOrWhiteSpace(newValue))
                {
                    if (comparableData.primaryData != null)
                    {
                        comparableData.primaryData.value = newValue;
                    }
                    if (comparableData.comparisonData != null)
                    {
                        comparableData.comparisonData.value = newValue;
                    }
                }
            }
        }

    }
}
