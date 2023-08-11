using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVChangesToDLC5Events
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly WeaponDef Obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
        private static readonly WeaponDef Subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
        private static readonly WeaponDef Redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
        private static readonly WeaponDef Devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
        private static readonly WeaponDef Tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

        public static void ChangesToDLC5Defs()
        {
            try
            {
                foreach (GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef in Repo.GetAllDefs<GeoMarketplaceItemOptionDef>())
                {
                    if (!geoMarketplaceItemOptionDef.DisallowDuplicates)
                    {
                        geoMarketplaceItemOptionDef.DisallowDuplicates = true;

                    }
                }


                GeoMarketplaceResearchOptionDef randomMarketResearch = DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef");
                randomMarketResearch.DisallowDuplicates = true;
                randomMarketResearch.Availability = 8;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static List<string> CheckForKaosWepaons(GeoLevelController level)
        {
            try
            {

                List<string> kaosWeapons = new List<string>();

                List<GeoItem> phoenixStorageItems = level.PhoenixFaction.ItemStorage.ToList();
                List<GeoCharacter> phoenixSoldiers = level.PhoenixFaction.Soldiers.ToList();
                List<GeoCharacter> phoenixVehicles = level.PhoenixFaction.GroundVehicles.ToList();

                GameTagDef dlc5Tag = GameUtl.GameComponent<SharedData>().SharedGameTags.Dlc5TagDef;

                foreach (GeoItem item in phoenixStorageItems)
                {
                    if (item.ItemDef.Tags.Contains(dlc5Tag))
                    {
                        //TFTVLogger.Always(item.CommonItemData.GetDisplayName());



                        if (item.ItemDef.Equals(Obliterator))//item.CommonItemData.GetDisplayName() == "Obliterator")
                        {
                            kaosWeapons.Add("KEY_KS_OBLITERATOR");
                        }
                        else if (item.ItemDef.Equals(Subjector))//item.CommonItemData.GetDisplayName() == "Subjector")
                        {
                            kaosWeapons.Add("KEY_KS_SUBJECTOR");
                        }
                        else if (item.ItemDef.Equals(Redemptor))//item.CommonItemData.GetDisplayName() == "Redemptor")
                        {
                            kaosWeapons.Add("KEY_KS_REDEMPTOR");
                        }
                        else if (item.ItemDef.Equals(Devastator))//item.CommonItemData.GetDisplayName() == "Devastator")
                        {
                            kaosWeapons.Add("KEY_KS_DEVASTATOR");
                        }
                        else if (item.ItemDef.Equals(Tormentor))//item.CommonItemData.GetDisplayName() == "Tormentor")
                        {
                            kaosWeapons.Add("KEY_KS_TORMENTOR");
                        }

                    }
                }

                foreach (GeoCharacter geoCharacter in phoenixVehicles)
                {
                    foreach (GeoItem inventoryItem in geoCharacter.InventoryItems)
                    {
                        if (inventoryItem.ItemDef.Tags.Contains(dlc5Tag))
                        {
                            //TFTVLogger.Always(inventoryItem.CommonItemData.GetDisplayName());
                            if (inventoryItem.ItemDef.Equals(Obliterator))//inventoryItem.CommonItemData.GetDisplayName() == "Obliterator")
                            {
                                kaosWeapons.Add("KEY_KS_OBLITERATOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Subjector))//inventoryItem.CommonItemData.GetDisplayName() == "Subjector")
                            {
                                kaosWeapons.Add("KEY_KS_SUBJECTOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Redemptor)) //inventoryItem.CommonItemData.GetDisplayName() == "Redemptor")
                            {
                                kaosWeapons.Add("KEY_KS_REDEMPTOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Devastator)) //inventoryItem.CommonItemData.GetDisplayName() == "Devastator")
                            {
                                kaosWeapons.Add("KEY_KS_DEVASTATOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Tormentor)) //inventoryItem.CommonItemData.GetDisplayName() == "Tormentor")
                            {
                                kaosWeapons.Add("KEY_KS_TORMENTOR");
                            }
                        }
                    }
                }


                foreach (GeoCharacter geoCharacter in level.PhoenixFaction.Soldiers)
                {
                    foreach (GeoItem inventoryItem in geoCharacter.InventoryItems)
                    {
                        if (inventoryItem.ItemDef.Tags.Contains(dlc5Tag))
                        {
                            //TFTVLogger.Always(inventoryItem.CommonItemData.GetDisplayName());
                            if (inventoryItem.ItemDef.Equals(Obliterator))//inventoryItem.CommonItemData.GetDisplayName() == "Obliterator")
                            {
                                kaosWeapons.Add("KEY_KS_OBLITERATOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Subjector))//inventoryItem.CommonItemData.GetDisplayName() == "Subjector")
                            {
                                kaosWeapons.Add("KEY_KS_SUBJECTOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Redemptor)) //inventoryItem.CommonItemData.GetDisplayName() == "Redemptor")
                            {
                                kaosWeapons.Add("KEY_KS_REDEMPTOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Devastator)) //inventoryItem.CommonItemData.GetDisplayName() == "Devastator")
                            {
                                kaosWeapons.Add("KEY_KS_DEVASTATOR");
                            }
                            else if (inventoryItem.ItemDef.Equals(Tormentor)) //inventoryItem.CommonItemData.GetDisplayName() == "Tormentor")
                            {
                                kaosWeapons.Add("KEY_KS_TORMENTOR");
                            }
                        }
                    }
                    foreach (GeoItem equipmentItem in geoCharacter.EquipmentItems)
                    {
                        if (equipmentItem.ItemDef.Tags.Contains(dlc5Tag))
                        {
                            // TFTVLogger.Always(equipmentItem.CommonItemData.GetDisplayName());

                            if (equipmentItem.ItemDef.Equals(Obliterator))// equipmentItem.CommonItemData.GetDisplayName() == "Obliterator")
                            {
                                kaosWeapons.Add("KEY_KS_OBLITERATOR");
                            }
                            else if (equipmentItem.ItemDef.Equals(Subjector)) //equipmentItem.CommonItemData.GetDisplayName() == "Subjector")
                            {
                                kaosWeapons.Add("KEY_KS_SUBJECTOR");
                            }
                            else if (equipmentItem.ItemDef.Equals(Redemptor)) //equipmentItem.CommonItemData.GetDisplayName() == "Redemptor")
                            {
                                kaosWeapons.Add("KEY_KS_REDEMPTOR");
                            }
                            else if (equipmentItem.ItemDef.Equals(Devastator)) //equipmentItem.CommonItemData.GetDisplayName() == "Devastator")
                            {
                                kaosWeapons.Add("KEY_KS_DEVASTATOR");
                            }
                            else if (equipmentItem.ItemDef.Equals(Tormentor)) //equipmentItem.CommonItemData.GetDisplayName() == "Tormentor")
                            {
                                kaosWeapons.Add("KEY_KS_TORMENTOR");
                            }
                        }
                    }
                }

                return kaosWeapons;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        [HarmonyPatch(typeof(GeoMarketplace), "UpdateOptions", new Type[] { })]
        public static class GeoMarketplace_UpdateOptions_MarketPlace_patch
        {

            private static readonly List<GeoMarketplaceItemOptionDef> geoMarketplaceItemOptionDefs = Repo.GetAllDefs<GeoMarketplaceItemOptionDef>().ToList();

        /*    public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateKERework;
            }*/



            public static void Prefix(GeoLevelController ____level)
            {

                try
                {
                    List<string> kaosWeapons = CheckForKaosWepaons(____level);

                    foreach (GeoMarketplaceItemOptionDef geoMarketplaceItemOptionDef in geoMarketplaceItemOptionDefs)
                    {
                        // TFTVLogger.Always(geoMarketplaceItemOptionDef.ItemDef.GetDisplayName().LocalizationKey);

                        if (kaosWeapons.Contains(geoMarketplaceItemOptionDef.ItemDef.GetDisplayName().LocalizationKey))
                        {
                            geoMarketplaceItemOptionDef.Availability = 10;
                        }
                        else
                        {
                            geoMarketplaceItemOptionDef.Availability = 0;
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }


        [HarmonyPatch(typeof(GeoMarketplace), "OnSiteVisited")]
        public static class GeoMarketplace_OnSiteVisited_MarketPlace_patch
        {
       /*     public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.ActivateKERework;
            }*/

            public static void Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings)
            {
                try
                {
                    if (____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) == 0)
                    {
                        ____level.EventSystem.SetVariable(____settings.NumberOfDLC5MissionsCompletedVariable, 4);
                        ____level.EventSystem.SetVariable(____settings.DLC5IntroCompletedVariable, 1);
                        ____level.EventSystem.SetVariable(____settings.DLC5FinalMovieCompletedVariable, 1);
                        __instance.UpdateOptions(____level.Timing);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}

