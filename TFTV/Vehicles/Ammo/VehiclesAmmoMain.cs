using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using Base.Entities.Abilities;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTVVehicleRework.Abilities;

namespace TFTV.Vehicles.Ammo
{
    internal class VehiclesAmmoMain
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal static Dictionary<ItemDef, GeoMarketplaceItemOptionDef> MarketplaceWeaponsAndAmmo = new Dictionary<ItemDef, GeoMarketplaceItemOptionDef>();
        internal static GameTagDef MarketplaceGroundVehicleWeapon;
        private static ReloadAbilityDef _reloadVehicleAbility3APDef;
        private static ReloadAbilityDef _reloadAbilityDef;


        internal class Defs
        {
            public static void CreateDefs()
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        TFTVLogger.Always("[VehiclesAmmoMain] Aircraft rework is disabled, skipping CreateDefs.");
                        return;
                    }
                    CreateVehicleReloadAbility3AP();
                    CreateAmmoForScarab();
                    CreateAmmoForAspida();
                    CreateAmmoForArmadillo();
                    CreateAmmoForJunker();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AddFreeStartingAmmo(TacCharacterDef vehicle, TacticalItemDef ammo, int amount = 1)
            {
                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        vehicle.Data.InventoryItems = vehicle.Data.InventoryItems.AddToArray(ammo);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateVehicleReloadAbility3AP()
            {
                try
                {
                    ReloadAbilityDef reload = DefCache.GetDef<ReloadAbilityDef>("Reload_AbilityDef");
                    _reloadVehicleAbility3APDef = Helper.CreateDefFromClone(reload, "{D4E5F6A7-B8C9-4D0E-9123-4567890ABCDE}", "VehicleReload_AbilityDef");
                    _reloadVehicleAbility3APDef.ActionPointCost = 0.75f;
                    _reloadAbilityDef = reload;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void AddReloadAbilityIfMissing(WeaponDef weaponDef, ReloadAbilityDef reload = null)
            {
                try
                {
                    if (reload == null)
                    {
                        reload = _reloadVehicleAbility3APDef;
                    }

                    if (weaponDef.Abilities == null)
                    {
                        weaponDef.Abilities = new AbilityDef[0];
                    }



                    if (weaponDef.Abilities.Any(a => a is FreeReloadAbilityDef))
                    {
                        TFTVLogger.Always($"[VehiclesAmmoMain] Removing existing FreeReloadAbilityDef from {weaponDef.name}.");

                        FreeReloadAbilityDef freeReload = (FreeReloadAbilityDef)weaponDef.Abilities.FirstOrDefault(a => a is FreeReloadAbilityDef);

                        List<AbilityDef> abilitiesList = weaponDef.Abilities.ToList();
                        abilitiesList.Remove(freeReload);

                        weaponDef.Abilities = abilitiesList.ToArray();
                    }


                    if (!weaponDef.Abilities.Contains(reload))
                    {
                        weaponDef.Abilities = weaponDef.Abilities.AddToArray(reload);
                    }

                    // Optional: show it in Geo inventory tooltip (matches existing pattern in other files)
                    if (reload.ViewElementDef != null)
                    {
                        reload.ViewElementDef.ShowInInventoryItemTooltip = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateAmmoForScarab()
            {
                try
                {
                    TacCharacterDef scarab = DefCache.GetDef<TacCharacterDef>("PX_Scarab_CharacterTemplateDef");

                    GroundVehicleWeaponDef scarabGeminiWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("PX_Scarab_Missile_Turret_GroundVehicleWeaponDef");
                    scarabGeminiWeapon.FreeReloadOnMissionEnd = false;

                    AddReloadAbilityIfMissing(scarabGeminiWeapon);
                    TacticalItemDef geminiWeaponAmmo = CreateAmmoForScarabWeapon(
                        "gemini",
                        scarabGeminiWeapon,
                        32,
                        96,
                        "{D3C1E2F1-4F1A-4C2B-8C6E-1F3B2A7D8E90}",
                        "{A2B3C4D5-E6F7-4812-3456-7890ABCDEF12}",
                        "{B1C2D3E4-F5A6-4789-0123-4567890ABCDE}",
                        "vehicles_ammo_scarab_gemini.png");

                    AddFreeStartingAmmo(scarab, geminiWeaponAmmo);

                    GeoPhoenixFactionDef phoenixFaction = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");
                    phoenixFaction.StartingManufacturableItems = phoenixFaction.StartingManufacturableItems.AddToArray(geminiWeaponAmmo);

                    // Taurus
                    GroundVehicleWeaponDef scarabTaurusWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("PX_Scarab_Taurus_GroundVehicleWeaponDef");
                    scarabTaurusWeapon.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(scarabTaurusWeapon);
                    TacticalItemDef taurusWeaponAmmo = CreateAmmoForScarabWeapon(
                        "taurus",
                        scarabTaurusWeapon,
                        12,
                        57,
                        "{C8D13A2F-8A3E-4A55-9A1E-1B1B83C5B7F1}",
                        "{4C43D65A-4C32-4D44-9A5D-5A0D3B7B2472}",
                        "{F1A2C3D4-E5F6-47A8-9B0C-1D2E3F4A5B6C}",
                        "vehicles_ammo_scarab_taurus.png");

                    ManufactureResearchRewardDef helCannonReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_HelCannon_ResearchDef_ManufactureResearchRewardDef_0");
                    helCannonReward.Items = helCannonReward.Items.AddToArray(taurusWeaponAmmo);

                    // Scorpio
                    GroundVehicleWeaponDef scarabScorpioWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("PX_Scarab_Scorpio_GroundVehicleWeaponDef");
                    scarabScorpioWeapon.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(scarabScorpioWeapon);
                    TacticalItemDef scorpioWeaponAmmo = CreateAmmoForScarabWeapon(
                        "scorpio",
                        scarabScorpioWeapon,
                        48,
                        154,
                        "{9B8A7C6D-5E4F-4321-9ABC-DEF012345678}",
                        "{12345678-90AB-CDEF-1234-567890ABCDEF}",
                        "{0FEDCBA9-8765-4321-0FED-CBA987654321}",
                        "vehicles_ammo_scarab_scorpio.png");

                    ManufactureResearchRewardDef virophageReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0");
                    virophageReward.Items = virophageReward.Items.AddToArray(scorpioWeaponAmmo);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateAmmoForAspida()
            {
                try
                {
                    TacCharacterDef aspida = DefCache.GetDef<TacCharacterDef>("SY_Aspida_CharacterTemplateDef");


                    ManufactureResearchRewardDef roverReward = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_Rover_ResearchDef_ManufactureResearchRewardDef_1");

                    // Arms
                    GroundVehicleWeaponDef aspidaArmsWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("SY_Aspida_Arms_GroundVehicleWeaponDef");
                    aspidaArmsWeapon.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(aspidaArmsWeapon);
                    TacticalItemDef aspidaArmsAmmo = CreateAmmoForAspidaWeapon(
                        "aspida_arms",
                        aspidaArmsWeapon,
                        45,
                        165,
                        "{B86B7B4F-0E9A-4D8B-A2AD-9C7E0A6FD1C2}",
                        "{C9DD2A6B-7F35-4C5F-9F22-0B9E741D7E2E}",
                        "vehicles_ammo_aspida_arms.png");

                    roverReward.Items = roverReward.Items.AddToArray(aspidaArmsAmmo);
                    AddFreeStartingAmmo(aspida, aspidaArmsAmmo);

                    ManufactureResearchRewardDef neuralRifleReward = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_AdvancedDisableTech_ResearchDef_ManufactureResearchRewardDef_0");

                    // Themis
                    GroundVehicleWeaponDef aspidaThemisWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("SY_Aspida_Themis_GroundVehicleWeaponDef");
                    aspidaThemisWeapon.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(aspidaThemisWeapon);
                    TacticalItemDef aspidaThemisAmmo = CreateAmmoForAspidaWeapon(
                        "aspida_themis",
                        aspidaThemisWeapon,
                        18,
                        38,
                        "{7B3E6F4A-12B5-4C7C-9E3C-7F94FBE0B28A}",
                        "{D2F1E8C7-5B7A-4C3E-9A1B-0C2D3E4F5A6B}",
                        "vehicles_ammo_aspida_themis.png");

                    neuralRifleReward.Items = neuralRifleReward.Items.AddToArray(aspidaThemisWeapon);
                    neuralRifleReward.Items = neuralRifleReward.Items.AddToArray(aspidaThemisAmmo);

                    GroundVehicleWeaponDef aspidaLaserCannons = DefCache.GetDef<GroundVehicleWeaponDef>("SY_Aspida_Apollo_GroundVehicleWeaponDef");
                    aspidaLaserCannons.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(aspidaLaserCannons);
                    //   AmmoWeaponDatabase.AmmoToWeaponDictionary[aspidaLaserCannons.CompatibleAmmunition[0]].Add(aspidaLaserCannons);
                    //  aspidaLaserCannons.CompatibleAmmunition[0].Tags.Add(DefCache.GetDef<GameTagDef>("Vehicle_TagDef"));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static TacticalItemDef CreateAmmoForAspidaWeapon(string ammoName, GroundVehicleWeaponDef weaponDef, int techPrice, int matPrice, string gUID0, string gUID1, string spriteFileName)
            {
                try
                {
                    GameTagDef vehicleTag = DefCache.GetDef<GameTagDef>("Vehicle_TagDef"); //used to assign to Manufacturing
                    FactionTagDef synedrionFactionTag = DefCache.GetDef<FactionTagDef>("Synedrion_FactionTagDef");
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");


                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{ammoName}_AmmoClipDef";

                    ClassTagDef classTagDef = weaponDef.Tags.FirstOrDefault<ClassTagDef>();

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_VEHICLE_AMMO_{ammoName}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_VEHICLE_AMMO_DESCRIPTION_{ammoName}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);

                    newAmmo.RequiredSlotBinds[0] = weaponDef.RequiredSlotBinds[0];
                    newAmmo.Weight = 5;
                    newAmmo.CrateSpawnWeight = 1000;

                    // Ensure vehicle-class tagging
                    newAmmo.Tags.Remove(phoenixFactionTag);
                    if (!newAmmo.Tags.Contains(synedrionFactionTag))
                    {
                        newAmmo.Tags.Add(synedrionFactionTag);
                    }
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"));
                    newAmmo.Tags.Add(DefCache.GetDef<ClassTagDef>("Vehicle_ClassTagDef"));
                    if (classTagDef != null)
                    {
                        newAmmo.Tags.Add(classTagDef);
                    }
                    newAmmo.Tags.Add(vehicleTag);

                    newAmmo.ManufactureTech = techPrice;
                    newAmmo.ManufactureMaterials = matPrice;
                    newAmmo.ChargesMax = weaponDef.ChargesMax;

                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };
                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    return newAmmo;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static TacticalItemDef CreateAmmoForScarabWeapon(string ammoName, GroundVehicleWeaponDef weaponDef, int techPrice, int matPrice, string gUID0, string gUID1, string gUID2, string spriteFileName)
            {
                try
                {
                    GameTagDef vehicleTag = DefCache.GetDef<GameTagDef>("Vehicle_TagDef"); //used to assign to Manufacturing
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");

                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{ammoName}_AmmoClipDef";

                    ClassTagDef classTagDef = weaponDef.Tags.FirstOrDefault<ClassTagDef>();

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_VEHICLE_AMMO_{ammoName}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_VEHICLE_AMMO_DESCRIPTION_{ammoName}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);

                    newAmmo.RequiredSlotBinds[0] = weaponDef.RequiredSlotBinds[0];
                    newAmmo.Weight = 5;

                    newAmmo.CrateSpawnWeight = 1000;
                    newAmmo.Tags.Remove(DefCache.GetDef<FactionTagDef>("NewJerico_FactionTagDef"));
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"));
                    newAmmo.Tags.Add(classTagDef);
                    newAmmo.Tags.Add(vehicleTag);

                    newAmmo.ManufactureTech = techPrice;
                    newAmmo.ManufactureMaterials = matPrice;
                    newAmmo.ChargesMax = weaponDef.ChargesMax;

                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };
                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    return newAmmo;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateAmmoForJunker()
            {
                try
                {
                    CreateJunkerTag();

                    TacCharacterDef junker = DefCache.GetDef<TacCharacterDef>("KS_Kaos_Buggy_CharacterTemplateDef");
                    TacCharacterDef junker2 = DefCache.GetDef<TacCharacterDef>("KS_Kaos_Buggy_TacCharacterDef");

                    WeaponDef junkerMinigun = DefCache.GetDef<WeaponDef>("KS_Buggy_Minigun_Vishnu_WeaponDef");
                    TacticalItemDef junkerMinigunAmmo = CreateAmmoForJunkerWeapon(
                        "junkerMinigun",
                        junkerMinigun,
                        50,
                        "{E4F5A6B7-C8D9-4E0F-9123-4567890ABCDE}",
                        "{F1E2D3C4-B5A6-4789-0123-4567890ABCDE}",
                        "{0A1B2C3D-4E5F-6789-0123-4567890ABCDE}"
                        //DefCache.GetDef<GroundVehicleModuleDef>("KS_Buggy_Minigun_Vishnu_ModuleDef")
                        );

                    AddFreeStartingAmmo(junker, junkerMinigunAmmo, 2);
                    AddFreeStartingAmmo(junker2, junkerMinigunAmmo, 2);

                    WeaponDef junkerVishnu = DefCache.GetDef<WeaponDef>("KS_Buggy_The_Vishnu_Gun_Cannon_WeaponDef");
                    TacticalItemDef junkerVishnuAmmo = CreateAmmoForJunkerWeapon(
                        "vishnu",
                        junkerVishnu,
                        100,
                        "{1B2C3D4E-5F60-7890-1234-567890ABCDEF}",
                        "{2A3B4C5D-6E7F-8901-2345-67890ABCDEF1}",
                        "{3C4D5E6F-7081-2345-6789-0ABCDEF12345}"
                        );

                    AddFreeStartingAmmo(junker, junkerVishnuAmmo, 1);
                    AddFreeStartingAmmo(junker2, junkerVishnuAmmo, 1);

                    WeaponDef junkerMinigunFullstop = DefCache.GetDef<WeaponDef>("KS_Buggy_Minigun_Fullstop_WeaponDef");
                    junkerMinigunFullstop.CompatibleAmmunition = new TacticalItemDef[] { junkerMinigun.CompatibleAmmunition[0] };
                    junkerMinigunFullstop.FreeReloadOnMissionEnd = false;
                    AmmoWeaponDatabase.AmmoToWeaponDictionary[junkerMinigun.CompatibleAmmunition[0]].Add(junkerMinigunFullstop);

                    WeaponDef junkerMinigunScreamer = DefCache.GetDef<WeaponDef>("KS_Buggy_Minigun_Screamer_WeaponDef");
                    junkerMinigunScreamer.CompatibleAmmunition = new TacticalItemDef[] { junkerMinigun.CompatibleAmmunition[0] };
                    junkerMinigunScreamer.FreeReloadOnMissionEnd = false;
                    AmmoWeaponDatabase.AmmoToWeaponDictionary[junkerMinigun.CompatibleAmmunition[0]].Add(junkerMinigunScreamer);



                    WeaponDef junkerFullStop = DefCache.GetDef<WeaponDef>("KS_Buggy_Fullstop_WeaponDef");
                    CreateAmmoForJunkerWeapon(
                        "fullstop",
                        junkerFullStop,
                        100,
                        "{4D5E6F70-8192-3456-7890-ABCDEF123456}",
                        "{5E6F7081-9234-5678-90AB-CDEF12345678}",
                        "{6F708192-3456-7890-ABCD-EF1234567890}"
                        );


                    WeaponDef junkerScreamer = DefCache.GetDef<WeaponDef>("KS_Buggy_Screamer_WeaponDef");
                    CreateAmmoForJunkerWeapon(
                        "screamer",
                        junkerScreamer,
                        100,
                        "{7F809192-3456-7890-ABCD-EF1234567890}",
                        "{80919234-5678-90AB-CDEF-1234567890AB}",
                        "{90123456-7890-ABCD-EF12-34567890ABCD}"
                        );


                    // Purgatory
                    GroundVehicleWeaponDef purgatoryWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("NJ_Armadillo_Purgatory_GroundVehicleWeaponDef");
                    CreateAmmoForJunkerWeapon(
                        "purgatory",
                        purgatoryWeapon,
                        280,
                        "{F0A6A10A-7E76-43DC-A2B4-C3A9B2E4C7A8}",
                        "{2D7B8A87-8E66-4A8C-9CB7-0C1BCB2F11E4}",
                        "{EF57AD6A-683B-4648-928A-C602FBEC4691}"
                        );
                    // "vehicles_ammo_purgatory.png"



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateJunkerTag()
            {
                try
                {
                    string gUID = "{6318BE3A-A5E1-42C5-A25A-339126F3DBB0}";
                    MarketplaceGroundVehicleWeapon = Helper.CreateDefFromClone<GameTagDef>(DefCache.GetDef<GameTagDef>("GunWeapon_TagDef"), gUID, "Junker_WeaponTagDef");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static TacticalItemDef CreateAmmoForJunkerWeapon(string ammoName, WeaponDef weaponDef, int minPrice, string gUID0, string gUID1, string gUID2)
            {
                try
                {

                    FactionTagDef neutralFactionTag = DefCache.GetDef<FactionTagDef>("Neutral_FactionTagDef");
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");

                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{ammoName}_AmmoClipDef";
                    string spriteFileName = $"vehicles_ammo_{ammoName}.png";

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_VEHICLE_AMMO_{ammoName}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_VEHICLE_AMMO_DESCRIPTION_{ammoName}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);

                    newAmmo.Weight = 5;
                    newAmmo.ChargesMax = weaponDef.ChargesMax;
                    newAmmo.CrateSpawnWeight = 1000;
                    newAmmo.Tags.Remove(phoenixFactionTag);
                    newAmmo.Tags.Add(neutralFactionTag);
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"));
                    newAmmo.Tags.Add(DefCache.GetDef<ClassTagDef>("Vehicle_ClassTagDef"));
                    //  newAmmo.CombineWhenStacking = false;
                    newAmmo.ManufactureTech = 0;
                    newAmmo.ManufactureMaterials = minPrice;
                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };

                    weaponDef.Tags.Add(MarketplaceGroundVehicleWeapon);
                    weaponDef.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(weaponDef);
                    GeoMarketplaceItemOptionDef newMarketplaceItem = Helper.CreateDefFromClone
                         (DefCache.GetDef<GeoMarketplaceItemOptionDef>("Obliterator_MarketplaceItemOptionDef"), gUID2, name);

                    newMarketplaceItem.MinPrice = minPrice*0.8f;
                    newMarketplaceItem.MaxPrice = minPrice + minPrice*0.5f;
                    newMarketplaceItem.ItemDef = newAmmo;
                    newMarketplaceItem.DisallowDuplicates = false;
                    //  newMarketplaceItem.Availability = availability;

                    /* TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");

                     List<GeoMarketplaceOptionDef> geoMarketplaceItemOptionDefs = marketplaceSettings.PossibleOptions.ToList();

                     geoMarketplaceItemOptionDefs.Add(newMarketplaceItem);

                     marketplaceSettings.PossibleOptions = geoMarketplaceItemOptionDefs.ToArray();*/

                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    /*  GeoMarketplaceItemOptionDef weaponMarketPlaceOption = (GeoMarketplaceItemOptionDef)geoMarketplaceItemOptionDefs.Find(o => o is GeoMarketplaceItemOptionDef marketOption && marketOption.ItemDef == groundVehicleModuleDef);

                       _junkerWeaponsAndAmmo.Add(weaponMarketPlaceOption, newMarketplaceItem);*/

                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");

                    neutralFaction.StartingManufacturableItems = neutralFaction.StartingManufacturableItems.AddToArray(newAmmo);

                    InventoryComponentDef neutralCrateInventoryComponent = DefCache.GetDef<InventoryComponentDef>("Crate_PX_InventoryComponentDef");
                    neutralCrateInventoryComponent.ItemDefs = neutralCrateInventoryComponent.ItemDefs.AddToArray(newAmmo);

                    MarketplaceWeaponsAndAmmo.Add(weaponDef, newMarketplaceItem);

                    return newAmmo;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void CreateAmmoForArmadillo()
            {
                try
                {
                    TacCharacterDef armadillo = DefCache.GetDef<TacCharacterDef>("NJ_Armadillo_CharacterTemplateDef");

                    ManufactureResearchRewardDef njVehicleTechReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_1");

                    // Hailstorm (Gauss turret)
                    GroundVehicleWeaponDef hailstormWeapon = DefCache.GetDef<GroundVehicleWeaponDef>("NJ_Armadillo_Gauss_Turret_GroundVehicleWeaponDef");
                    hailstormWeapon.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(hailstormWeapon, _reloadAbilityDef);
                    TacticalItemDef hailstormAmmo = CreateAmmoForNJVehicleWeapon(
                        "hailstorm",
                        hailstormWeapon,
                        3,
                        22,
                        "{D55CD4B6-7851-4CC1-A0B9-2A5C65C69DF7}",
                        "{1E7B3C2D-9F41-4A3B-9D60-DB5D6D9A3CD1}",
                        "vehicles_ammo_hailstorm.png");

                    njVehicleTechReward.Items = njVehicleTechReward.Items.AddToArray(hailstormAmmo);
                    AddFreeStartingAmmo(armadillo, hailstormAmmo, 3);

                    ManufactureResearchRewardDef njFireTechReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_PurificationTech_ResearchDef_ManufactureResearchRewardDef_0");

                    GroundVehicleWeaponDef flamethrower = DefCache.GetDef<GroundVehicleWeaponDef>("NJ_Armadillo_Mephistopheles_GroundVehicleWeaponDef");
                    flamethrower.FreeReloadOnMissionEnd = false;
                    AddReloadAbilityIfMissing(flamethrower);
                    TacticalItemDef flamethrowerAmmo = CreateAmmoForNJVehicleWeapon(
                        "ft",
                        flamethrower,
                        35,
                        168,
                        "{A1B2C3D4-E5F6-4789-0123-4567890ABCDE}",
                        "{3E4F5A6B-7C8D-9E0F-1234-567890ABCDEF}",
                        "vehicles_ammo_ft.png");

                    njFireTechReward.Items = njFireTechReward.Items.AddToArray(flamethrower);
                    njFireTechReward.Items = njFireTechReward.Items.AddToArray(flamethrowerAmmo);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            // NJ ammo: ensure NJ faction tag, remove PX tag (and keep vehicle class tag)
            private static TacticalItemDef CreateAmmoForNJVehicleWeapon(string ammoName, GroundVehicleWeaponDef weaponDef, int techPrice, int matPrice, string gUID0, string gUID1, string spriteFileName)
            {
                try
                {
                    GameTagDef vehicleTag = DefCache.GetDef<GameTagDef>("Vehicle_TagDef"); //used to assign to Manufacturing
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");
                    FactionTagDef newJerichoFactionTag = DefCache.GetDef<FactionTagDef>("NewJerico_FactionTagDef");

                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{ammoName}_AmmoClipDef";

                    ClassTagDef classTagDef = weaponDef.Tags.FirstOrDefault<ClassTagDef>();

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_VEHICLE_AMMO_{ammoName}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_VEHICLE_AMMO_DESCRIPTION_{ammoName}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);

                    newAmmo.RequiredSlotBinds[0] = weaponDef.RequiredSlotBinds[0];
                    newAmmo.Weight = 5;
                    newAmmo.CrateSpawnWeight = 1000;

                    // Faction tagging: NJ yes, PX no
                    newAmmo.Tags.Remove(phoenixFactionTag);
                    if (!newAmmo.Tags.Contains(newJerichoFactionTag))
                    {
                        newAmmo.Tags.Add(newJerichoFactionTag);
                    }

                    // Ensure vehicle class tag; also add weapon's class tag if present
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"));
                    newAmmo.Tags.Add(DefCache.GetDef<ClassTagDef>("Vehicle_ClassTagDef"));
                    if (classTagDef != null && !newAmmo.Tags.Contains(classTagDef))
                    {
                        newAmmo.Tags.Add(classTagDef);
                    }
                    newAmmo.Tags.Add(vehicleTag);

                    newAmmo.ManufactureTech = techPrice;
                    newAmmo.ManufactureMaterials = matPrice;
                    newAmmo.ChargesMax = weaponDef.ChargesMax;

                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };
                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    return newAmmo;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
    }
}
