using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {
        internal class TFTVKaosGuns
        {

            internal static readonly WeaponDef _obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
            internal static readonly WeaponDef _subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
            internal static readonly WeaponDef _redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
            internal static readonly WeaponDef _devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
            internal static readonly WeaponDef _tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

            internal static Dictionary<GeoMarketplaceItemOptionDef, GeoMarketplaceItemOptionDef> _kGWeaponsAndAmmo = new Dictionary<GeoMarketplaceItemOptionDef, GeoMarketplaceItemOptionDef>();
            internal static GameTagDef _kGTag;

            [HarmonyPatch(typeof(GeoMission), "AddCratesToMissionData")] //VERIFIED
            public static class GeoMission_AddCratesToMissionData_patch
            {

                public static void Prefix(GeoMission __instance)
                {
                    try
                    {
                        if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeAmbush_MissionTagDef"))
                            ||
                            __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeScavenging_MissionTagDef")))
                        {

                            AmbushOrScavTemp = true;

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                public static void Postfix(GeoMission __instance)
                {
                    try
                    {
                        if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeAmbush_MissionTagDef"))
                            ||
                            __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeScavenging_MissionTagDef")))
                        {

                            AmbushOrScavTemp = false;

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }

            [HarmonyPatch(typeof(GeoLevelController), nameof(GeoLevelController.GetAvailableFactionEquipment))]
            public static class GeoLevelController_GetAvailableFactionEquipment_patch
            {


                public static void Postfix(GeoLevelController __instance, ref List<TacticalItemDef> __result)
                {
                    try
                    {
                        if (AmbushOrScavTemp)
                        {
                            TFTVLogger.Always($"It's an ambush or scavenging mission, adding KG ammo to GetAvailableFactionEquipment");

                            List<TacticalItemDef> kgAmmo = new List<TacticalItemDef>()
                        {
                            TFTVKaosGuns._subjector.CompatibleAmmunition[0],
                            TFTVKaosGuns._obliterator.CompatibleAmmunition[0],
                            TFTVKaosGuns._tormentor.CompatibleAmmunition[0],
                            TFTVKaosGuns._devastator.CompatibleAmmunition[0],
                            TFTVKaosGuns._redemptor.CompatibleAmmunition[0]

                        };

                            if (TFTVAircraftReworkMain.AircraftReworkOn)
                            {
                                List<TacticalItemDef> junkerAmmo = new List<TacticalItemDef>()

                                {
                                DefCache.GetDef<TacticalItemDef>("junkerMinigun_AmmoClipDef"),
                                DefCache.GetDef<TacticalItemDef>("vishnu_AmmoClipDef"),
                                DefCache.GetDef<TacticalItemDef>("fullstop_AmmoClipDef"),
                                DefCache.GetDef<TacticalItemDef>("screamer_AmmoClipDef"),
                                };

                                if (__instance.NewJerichoFaction.Research.HasCompleted("NJ_VehicleTech_ResearchDef"))
                                {
                                    __result.Add(DefCache.GetDef<TacticalItemDef>("purgatory_AmmoClipDef"));
                                }


                                __result.AddRange(junkerAmmo);
                            }


                            __result.AddRange(kgAmmo);


                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }


            private static void AdjustKGAndCreateAmmoForThem(WeaponDef weaponDef, int amount, int minPrice, string gUID0, string gUID1, string gUID2, string spriteFileName)
            {
                try
                {

                    FactionTagDef neutralFactionTag = DefCache.GetDef<FactionTagDef>("Neutral_FactionTagDef");
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");

                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{weaponDef.name}_AmmoClipDef";

                    ClassTagDef classTagDef = weaponDef.Tags.FirstOrDefault<ClassTagDef>();

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_KAOSGUNS_AMMO_{weaponDef.name}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_KAOSGUNS_AMMO_DESCRIPTION_{weaponDef.name}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);


                    newAmmo.ChargesMax = amount;
                    newAmmo.CrateSpawnWeight = 1000;
                    newAmmo.Tags.Remove(phoenixFactionTag);
                    newAmmo.Tags.Add(neutralFactionTag);
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"));
                    newAmmo.Tags.Add(classTagDef);
                    //  newAmmo.CombineWhenStacking = false;
                    newAmmo.ManufactureTech = 0;
                    newAmmo.ManufactureMaterials = minPrice;
                    weaponDef.ChargesMax = amount;
                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };

                    weaponDef.Tags.Add(_kGTag);
                    weaponDef.Tags.Add(Shared.SharedGameTags.ManufacturableTag);
                    GeoMarketplaceItemOptionDef newMarketplaceItem = Helper.CreateDefFromClone
                         (DefCache.GetDef<GeoMarketplaceItemOptionDef>("Obliterator_MarketplaceItemOptionDef"), gUID2, name);

                    newMarketplaceItem.MinPrice = minPrice;
                    newMarketplaceItem.MaxPrice = minPrice + minPrice * 1.25f;
                    newMarketplaceItem.ItemDef = newAmmo;
                    newMarketplaceItem.DisallowDuplicates = false;


                    TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");

                    List<GeoMarketplaceOptionDef> geoMarketplaceItemOptionDefs = marketplaceSettings.PossibleOptions.ToList();

                    geoMarketplaceItemOptionDefs.Add(newMarketplaceItem);


                    marketplaceSettings.PossibleOptions = geoMarketplaceItemOptionDefs.ToArray();

                    weaponDef.WeaponMalfunction = DefCache.GetDef<WeaponDef>("PX_AssaultRifle_WeaponDef").WeaponMalfunction;

                    if (weaponDef.Tags.Contains(DefCache.GetDef<ItemTypeTagDef>("AssaultRifleItem_TagDef")))
                    {
                        newAmmo.Tags.Add(Shared.SharedGameTags.MutoidClassTag);
                    }
                    if (weaponDef.Tags.Contains(DefCache.GetDef<ItemTypeTagDef>("HandgunItem_TagDef")))
                    {
                        newAmmo.Tags.Add(Shared.SharedGameTags.MutoidClassTag);
                        newAmmo.Tags.Add(TFTVMercenaries.berserkerTag);
                    }

                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    GeoMarketplaceItemOptionDef weaponMarketPlaceOption = (GeoMarketplaceItemOptionDef)geoMarketplaceItemOptionDefs.Find(o => o is GeoMarketplaceItemOptionDef marketOption && marketOption.ItemDef == weaponDef);

                    _kGWeaponsAndAmmo.Add(weaponMarketPlaceOption, newMarketplaceItem);

                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");

                    neutralFaction.StartingManufacturableItems = neutralFaction.StartingManufacturableItems.AddToArray(newAmmo);

                    InventoryComponentDef neutralCrateInventoryComponent = DefCache.GetDef<InventoryComponentDef>("Crate_PX_InventoryComponentDef");
                    neutralCrateInventoryComponent.ItemDefs = neutralCrateInventoryComponent.ItemDefs.AddToArray(newAmmo);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CreateKaosWeaponAmmo()
            {
                try
                {
                    _obliterator.ManufactureMaterials = 100;
                    _subjector.ManufactureMaterials = 100;
                    _redemptor.ManufactureMaterials = 100;
                    _devastator.ManufactureMaterials = 100;
                    _tormentor.ManufactureMaterials = 100;

                    _kGTag = TFTVCommonMethods.CreateNewTag("KaosGun", "{2DA3F33A-8D39-4DA6-8BA5-38C3114A21F7}");

                    //("Mutoid_ClassTagDef");

                    //KEY_KAOSGUNS_AMMO_
                    //KEY_KAOSGUNS_AMMO_DESCRIPTION_

                    AdjustKGAndCreateAmmoForThem(_tormentor, 8, 30, "e1875c26-0494-4d0f-9e5d-3c74a17c3b2d",
                    "79f6bb60-8ca3-4bbf-a0f1-c819f5ebf09e",
                    "ee89b5c3-6d06-4c5e-856b-96e7ff411c77", "KG_Pistol_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_subjector, 5, 30, "2e5be682-1f85-4610-bbb7-c2f2bf41d4c6",
                    "b03d78d4-c7e7-49c3-b097-3448e253a1e7",
                    "70a0a172-2b57-48d3-94c2-7cb4e428c3c4", "KG_Sniper_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_redemptor, 24, 30, "8f7ff5ca-4b8d-4677-86d3-7f21e41a3a70",
                    "d60e04a0-c873-4c16-9a83-2f9d6e1c163d",
                    "dc92d8ca-1b8d-4f85-9d90-d8eb9e63d5a3", "KG_Shotgun_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_devastator, 6, 30, "99aa40e5-5415-44b9-98ed-34d746a99b52",
                    "3b647fa3-1e06-4f2a-9d1c-82edf8a6dbff",
                    "605d3c8a-7b9c-481a-8c0d-7ff4be94901a", "KG_Cannon_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_obliterator, 32, 30, "2c86774f-4889-4c06-9f7a-8971e62ff267",
                    "587b1a5b-1665-48c9-8b9c-4156231712c1",
                    "1a1230fc-0e5d-4c4c-9be5-563879d2471f", "KG_Assault_Rifle_Ammo.png");


                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}


