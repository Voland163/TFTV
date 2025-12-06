using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.LaserWeapons
{
    internal static class LaserWeaponsInit
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        internal static TacticalItemDef LaserBatteryPackDef;

        internal static List<TacticalItemDef> OriginalLaserAmmo = new List<TacticalItemDef>();

        private sealed class WeaponSetup
        {
            internal WeaponDef WeaponDef;
            internal TacticalItemDef OriginalAmmoDef;
            internal TacticalItemDef[] OriginalCompat;
            internal int ReloadCost;
        }

        private static void AdjustAllDefs()
        {
            foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>())
            {
                if (tacCharacterDef.Data.EquipmentItems.Any(i => i != null && OriginalLaserAmmo.Contains(i)))
                {
                    //TFTVLogger.Always($"[LaserWeaponsInit] {tacCharacterDef.name} has laser ammo in default equipment");
                    tacCharacterDef.Data.EquipmentItems = tacCharacterDef.Data.EquipmentItems.Select(i =>
                    {
                        if (i != null && OriginalLaserAmmo.Contains(i))
                        {
                            return LaserBatteryPackDef;
                        }
                        return i;
                    }).ToArray();

                }

                if (tacCharacterDef.Data.InventoryItems.Any(i => i != null && OriginalLaserAmmo.Contains(i)))
                {
                    //TFTVLogger.Always($"[LaserWeaponsInit] {tacCharacterDef.name} has laser ammo in default equipment");
                    tacCharacterDef.Data.InventoryItems = tacCharacterDef.Data.InventoryItems.Select(i =>
                    {
                        if (i != null && OriginalLaserAmmo.Contains(i))
                        {
                            return LaserBatteryPackDef;
                        }
                        return i;
                    }).ToArray();
                }
            }

            foreach (ManufactureResearchRewardDef manufactureResearchRewardDef in Repo.GetAllDefs<ManufactureResearchRewardDef>())
            {
                if (manufactureResearchRewardDef.Items.Any(i => i != null && OriginalLaserAmmo.Contains(i)))
                {
                    //TFTVLogger.Always($"[LaserWeaponsInit] {manufactureResearchRewardDef.name} has laser ammo in manufacture rewards");
                    manufactureResearchRewardDef.Items = manufactureResearchRewardDef.Items.Select(i =>
                    {
                        if (i != null && OriginalLaserAmmo.Contains(i))
                        {
                            return LaserBatteryPackDef;
                        }
                        return i;
                    }).ToArray();
                }
            }

        }

        private static void AdjustWeaponMaxAmmo()
        {
            try
            {
                DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef").ChargesMax = 10;
                DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_WeaponDef").ChargesMax = 36;
                DefCache.GetDef<WeaponDef>("PX_LaserPDW_WeaponDef").ChargesMax = 24;
                DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef").ChargesMax = 8;
                DefCache.GetDef<WeaponDef>("PX_LaserArrayPack_WeaponDef").ChargesMax = 9;
                DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_Neon_WeaponDef").ChargesMax = 36;
                DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_WhiteNeon_WeaponDef").ChargesMax = 36;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }


        }

        public static void Init()
        {
            try
            {
                var weaponDefs = new Dictionary<string, int>
                {
                    { "SY_LaserPistol_WeaponDef", 1 },
                    { "SY_LaserAssaultRifle_WeaponDef", 2 },
                    {"SY_LaserAssaultRifle_Neon_WeaponDef", 2 },
                    {"SY_LaserAssaultRifle_WhiteNeon_WeaponDef", 2 },
                    { "PX_LaserPDW_WeaponDef", 2 },
                    { "SY_LaserSniperRifle_WeaponDef", 3 },
                    { "PX_LaserArrayPack_WeaponDef", 3 }
                };

                var setups = new List<WeaponSetup>();

                foreach (var kvp in weaponDefs)
                {
                    WeaponDef weaponDef = DefCache.GetDef<WeaponDef>(kvp.Key);
                    if (weaponDef == null)
                    {
                        //TFTVLogger.Always($"[LaserWeaponsInit] Missing weapon def '{kvp.Key}'");
                        continue;
                    }

                    TacticalItemDef[] originalCompat = weaponDef.CompatibleAmmunition?.Where(a => a != null).ToArray() ?? Array.Empty<TacticalItemDef>();
                    TacticalItemDef originalAmmo = originalCompat.FirstOrDefault();
                    if (originalAmmo == null)
                    {
                        //TFTVLogger.Always($"[LaserWeaponsInit] '{weaponDef.name}' has no compatible ammo to clone");
                        continue;
                    }

                    if (!OriginalLaserAmmo.Contains(originalAmmo))
                    {
                        OriginalLaserAmmo.Add(originalAmmo);
                    }

                    setups.Add(new WeaponSetup
                    {
                        WeaponDef = weaponDef,
                        OriginalAmmoDef = originalAmmo,
                        OriginalCompat = originalCompat,
                        ReloadCost = kvp.Value
                    });
                }

                if (setups.Count == 0)
                {
                    //TFTVLogger.Always("[LaserWeaponsInit] No laser weapons registered");
                    return;
                }

                LaserBatteryPackDef = CreateOrGetBatteryPack(setups.Select(s => s.OriginalAmmoDef).FirstOrDefault());

                ManufactureResearchRewardDef manufactureResearchRewardDef = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_LaserWeapons_ResearchDef_ManufactureResearchRewardDef_0");
                manufactureResearchRewardDef.Items = manufactureResearchRewardDef.Items.AddToArray(LaserBatteryPackDef);

                LaserWeaponsMain.LaserAmmoShareHelper.BatteryPackDef = LaserBatteryPackDef;
                //TFTVLogger.Always($"[LaserWeaponsInit] Battery pack def: {LaserBatteryPackDef?.name ?? "<null>"}");

                foreach (WeaponSetup setup in setups)
                {
                    LaserWeaponsMain.LaserAmmoShareHelper.RegisterWeapon(setup.WeaponDef, setup.OriginalAmmoDef, setup.ReloadCost);

                    var compatibility = new List<TacticalItemDef> { LaserBatteryPackDef };
                    foreach (TacticalItemDef ammo in setup.OriginalCompat)
                    {
                        if (ammo != null && !ReferenceEquals(ammo, LaserBatteryPackDef))
                        {
                            compatibility.Add(ammo);
                        }
                    }

                    setup.WeaponDef.CompatibleAmmunition = compatibility.ToArray();
                    //TFTVLogger.Always($"[LaserWeaponsInit] Updated compatible ammo for {setup.WeaponDef.name}");
                }

                AdjustWeaponMaxAmmo();
                AdjustAllDefs();
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private static TacticalItemDef CreateOrGetBatteryPack(TacticalItemDef sourceAmmo)
        {
            if (LaserBatteryPackDef != null)
            {
                return LaserBatteryPackDef;
            }

            if (sourceAmmo == null)
            {
                throw new InvalidOperationException("Unable to find source ammo for laser battery pack clone.");
            }

            var battery = Helper.CreateDefFromClone(
                sourceAmmo,
                guid: "{60ED2CAB-4DC4-4AA9-8F91-E9F0CF82F104}",
                name: "TFTV_LaserBatteryPack_ItemDef");

            battery.ChargesMax = 6;
            battery.DestroyWhenUsed = false;
            battery.DestroyAtZeroCharges = true;
            battery.Weight = 1;
            battery.ManufactureMaterials = 40f;
            battery.ManufactureTech = 10f;
            battery.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_LASER_BATTERY_NAME";
            battery.ViewElementDef.Description.LocalizationKey = "TFTV_LASER_BATTERY_DESC";


            return battery;
        }
    }
}