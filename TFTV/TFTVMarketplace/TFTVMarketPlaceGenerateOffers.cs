using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using Base.UI;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TFTV.Vehicles.Ammo;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {
        internal class TFTVMarketPlaceGenerateOffers
        {
            private static readonly ClassTagDef _vehicle_ClassTagDef = DefCache.GetDef<ClassTagDef>("Vehicle_ClassTagDef");

            private static readonly string _marketPlaceStockRotated = "MarketPlaceRotations";
            private static string _currentMarketPlaceSpecial;
            private static readonly string _vehicleMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_VEHICLES";
            private static readonly string _weaponsMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_WEAPONS";
            private static readonly string _mercenaryMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_MERCENARY";
            private static readonly string _researchMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_RESEARCH";
            private static readonly string[] _marketPlaceSpecials = new string[] { _vehicleMarketPlaceSpecial, _researchMarketPlaceSpecial, _mercenaryMarketPlaceSpecial, _weaponsMarketPlaceSpecial };


            /// <summary>
            /// Can't hire mercenaries if Living Quarters are full and can't buy tech that has already been researched
            /// </summary>

            [HarmonyPatch(typeof(GeoEventChoice), nameof(GeoEventChoice.PassRequirements))]
            public static class GeoEventChoice_PassRequirements_patch
            {
                public static void Postfix(GeoEventChoice __instance, GeoFaction faction, ref bool __result)
                {
                    try
                    {
                        RemoveBadChoices(__instance, faction, ref __result);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void RemoveBadChoices(GeoEventChoice eventChoice, GeoFaction faction, ref bool result)
            {
                try
                {



                    if (eventChoice.Outcome != null && eventChoice.Outcome.GiveResearches != null && eventChoice.Outcome.GiveResearches.Count > 0 &&
                        eventChoice.Text == faction.Research.GetResearchById(eventChoice.Outcome.GiveResearches[0]).ResearchDef.ViewElementDef.ResearchName &&
                        faction.Research.HasCompleted(eventChoice.Outcome.GiveResearches[0]))
                    {
                        TFTVLogger.Always($"PX has already completed {eventChoice.Text.Localize()}");

                        result = false;

                    }

                    if (eventChoice.Outcome != null && eventChoice.Outcome.Units != null && eventChoice.Outcome.Units.Count > 0 && eventChoice.Outcome.Units[0].Data.GameTags.Contains(MercenaryTag) && faction is GeoPhoenixFaction phoenixFaction && phoenixFaction.LivingQuarterFull)
                    {
                        // TFTVLogger.Always($"Living Quarters are full! Can't recruit Mercenary");
                        result = false;
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }



            }



            private static GeoEventChoice GenerateResearchChoice(ResearchDef researchDef, float price)
            {
                try
                {
                    GeoEventChoice geoEventChoice = GenerateChoice(price);
                    geoEventChoice.Outcome.GiveResearches.Add(researchDef.Id);
                    geoEventChoice.Text = researchDef.ViewElementDef?.ResearchName;
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static GeoEventChoice GenerateItemChoice(ItemDef itemDef, float price)
            {
                try
                {
                    // TFTVLogger.Always($"item def is {itemDef.name}, display: {itemDef.GetDisplayName().Localize()}");

                    GeoEventChoice geoEventChoice = GenerateChoice(price);
                    if (itemDef is GroundVehicleItemDef groundVehicleItemDef)
                    {
                        geoEventChoice.Outcome.Units.Add(groundVehicleItemDef.VehicleTemplateDef);
                    }
                    else
                    {
                        geoEventChoice.Outcome.Items.Add(new ItemUnit(itemDef, 1));
                    }

                    geoEventChoice.Text = itemDef.GetDisplayName();
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GeoEventChoice GenerateChoice(float price)
            {
                try
                {
                    GeoEventChoice geoEventChoice = new GeoEventChoice
                    {
                        Requirments = new GeoEventChoiceRequirements(),
                        Outcome = new GeoEventChoiceOutcome()
                    };
                    geoEventChoice.Requirments.Resources.Add(new ResourceUnit(ResourceType.Materials, price));
                    geoEventChoice.Outcome.ReEneableEvent = true;
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static List<GeoMarketplaceOptionDef> GetOptionsByType(List<GeoMarketplaceOptionDef> currentlyPossibleOptions, GameTagDef itemTypeTag)
            {
                try
                {
                    return
                        new List<GeoMarketplaceOptionDef>(currentlyPossibleOptions).Where(o => o is GeoMarketplaceItemOptionDef item && item.ItemDef.Tags.Contains(itemTypeTag)).ToList();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static GeoMarketplaceItemOptionDef[] NewJericho_Items()
            {
                GeoMarketplaceItemOptionDef[] Options = new GeoMarketplaceItemOptionDef[]
                {
                (GeoMarketplaceItemOptionDef)Repo.GetDef("a5833903-97b1-71f4-9b7c-b0755e8decf7"), //Purgatory
                (GeoMarketplaceItemOptionDef)Repo.GetDef("03ebb7ca-08d7-36a4-2bf6-851b47682476"), //Lightweight Alloy
                (GeoMarketplaceItemOptionDef)Repo.GetDef("46a57a6d-7163-8ef4-99b3-8167efb46edc"), //Supercharger
                };
                return Options;
            }

            private static GeoMarketplaceItemOptionDef[] Synedrion_Items()
            {
                GeoMarketplaceItemOptionDef[] Options = new GeoMarketplaceItemOptionDef[]
                {
                (GeoMarketplaceItemOptionDef)Repo.GetDef("017b69c2-8a8f-e784-6b36-70cc804ece5d"), //Apollo
                (GeoMarketplaceItemOptionDef)Repo.GetDef("456bf1a1-82ce-2f54-9a0a-27600107d5b4"), //Psychic Jammer
                (GeoMarketplaceItemOptionDef)Repo.GetDef("3e192929-51ba-29e4-7ac1-e9ab2836f076"), //Experimental Thrusters
                };
                return Options;
            }


            /// <summary>
            /// When MarketPlace is discovered, 
            /// 
            /// 1) NumberOfDLC5MissionsCompletedVariable is set to 4 (to remove everything connected to DLC5 mission generation).
            /// 
            /// 2) _updateOptionsNextTime is set to now and updateOptionsWithRespectToTime is forcefully run
            ///  
            /// When UpdateOptionsWithRespectToTime is run, it checks whether _updateOptionsNextTime is past now. 
            /// 
            /// If it is, UpdateOptions(Timing) is run. 
            /// </summary>


            [HarmonyPatch(typeof(GeoMarketplace), "UpdateOptionsWithRespectToTime")] //VERIFIED
            public static class GeoMarketplace_UpdateOptionsWithRespectToTime_patch
            {
                public static bool Prefix(ref TimeUnit ____updateOptionsNextTime, GeoLevelController ____level, GeoMarketplace __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"UpdateOptionsWithRespectToTime: ____updateOptionsNextTime is {____updateOptionsNextTime.DateTime}, ____level.Timing.Now is {____level.Timing.Now.DateTime} ");

                        if (____level.Timing.Now < ____updateOptionsNextTime)
                        {

                        }
                        else
                        {
                            /*  UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                              int hours = UnityEngine.Random.Range(65, 90);


                              ____updateOptionsNextTime = TimeUtils.GetNextTimeInHours(____level.Timing, hours);*/

                            //TFTVLogger.Always($"After trigger: UpdateOptionsWithRespectToTime: ____updateOptionsNextTime is {____updateOptionsNextTime.DateTime}, ____level.Timing.Now is {____level.Timing.Now.DateTime} ");
                            __instance.UpdateOptions(____level.Timing);


                        }
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /// <summary>
            /// When UpdateOptions(Timing) is run, if 
            /// 
            /// 1) NumberOfDLC5MissionsCompletedVariable > 0 (that is, if MarketPlace has been explored) and
            /// 2) current time passed _updateOptionsNextTime
            /// 
            /// Then
            /// 
            /// 1) UpdateOptions is run;
            /// 2) _updateOptionsNextTime is to 65-90 hours from now
            /// 3) LogEntry is created
            /// 4) MarketRotation variable is increased by 1
            /// 
            /// </summary>

            [HarmonyPatch(typeof(GeoMarketplace), nameof(GeoMarketplace.UpdateOptions), new Type[] { typeof(Timing) })]
            public static class GeoMarketplace_UpdateOptionsTiming_patch
            {
                public static bool Prefix(ref TimeUnit ____updateOptionsNextTime, GeoLevelController ____level, Timing timing, GeoMarketplace __instance, TheMarketplaceSettingsDef ____settings)
                {
                    try
                    {

                        // TFTVLogger.Always($"UpdateOptions(Timing) is called (Prefix) Current time: {____level.Timing.Now.DateTime}. Next update: {____updateOptionsNextTime.DateTime}");

                        if (timing.Now >= ____updateOptionsNextTime && ____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) > 0)
                        {
                            MethodInfo updateOptionsMethod = typeof(GeoMarketplace).GetMethod("UpdateOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                            updateOptionsMethod.Invoke(__instance, null);
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int hours = UnityEngine.Random.Range(65, 90);
                            ____updateOptionsNextTime = TimeUtils.GetNextTimeInHours(____level.Timing, hours);

                            CreateLogEntryAndRollSpecialsMarketplaceUpdated(____level);

                            ____level.EventSystem.SetVariable(_marketPlaceStockRotated, ____level.EventSystem.GetVariable(_marketPlaceStockRotated) + 1);
                            TFTVLogger.Always($"number of stock rotations is {____level.EventSystem.GetVariable(_marketPlaceStockRotated)}");
                        }


                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                /*     public static void Postfix(TimeUnit ____updateOptionsNextTime, GeoLevelController ____level)
                     {
                         try
                         {
                           //  TFTVLogger.Always($"UpdateOptions(Timing) Postfix: Current time: {____level.Timing.Now.DateTime}. Next update: {____updateOptionsNextTime.DateTime}");
                         }
                         catch (Exception e)
                         {
                             TFTVLogger.Error(e);
                             throw;
                         }
                     }*/


            }


            [HarmonyPatch(typeof(GeoMarketplace), nameof(GeoMarketplace.UpdateOptions), new Type[] { })]

            public static class GeoMarketplace_UpdateOptions_MarketPlace_patch
            {
                public static bool Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings, TimeUnit ____updateOptionsNextTime)
                {
                    try
                    {
                        GenerateMarketPlaceOptionsOnUpdateOptions(__instance, ____level, ____settings, ____updateOptionsNextTime);

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void GenerateMarketPlaceOptionsOnUpdateOptions(GeoMarketplace geoMarketPlace, GeoLevelController controller, TheMarketplaceSettingsDef marketPlaceSettings, TimeUnit updateOptionsNextTime)
            {
                try
                {
                    TFTVLogger.Always($"Updating marketplace options. Current time: {controller.Timing.Now.DateTime}. Next update: {updateOptionsNextTime.DateTime}");

                    if (controller.EventSystem.GetVariable(_marketPlaceStockRotated) > 2)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int diceRoll = UnityEngine.Random.Range(1, 11);
                        if (diceRoll <= 3)
                        {
                            _currentMarketPlaceSpecial = _marketPlaceSpecials.GetRandomElement();
                        }
                    }


                    geoMarketPlace.MarketplaceChoices.Clear();

                    int numberOfStockRotations = controller.EventSystem.GetVariable(_marketPlaceStockRotated);

                    TFTVLogger.Always($"number of stock rotations is {controller.EventSystem.GetVariable(_marketPlaceStockRotated)}");

                    int numberOfOffers = Math.Min(8 + numberOfStockRotations * 4, 40);

                    TFTVLogger.Always($"Number of offers is {numberOfOffers}; divided by 4 {numberOfOffers / 4}");

                    List<GeoMarketplaceOptionDef> currentlyPossibleOptions = new List<GeoMarketplaceOptionDef>();

                    foreach (GeoMarketplaceOptionDef geoMarketplaceOptionDef in marketPlaceSettings.PossibleOptions)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int coinToss = UnityEngine.Random.Range(0, 2);

                        if (geoMarketplaceOptionDef.Availability - coinToss <= numberOfStockRotations)
                        {
                            currentlyPossibleOptions.Add(geoMarketplaceOptionDef);
                        }
                    }

                    currentlyPossibleOptions = CullAvailableOptionsBasedOnExternals(controller, currentlyPossibleOptions);

                    float voPriceMultiplier = TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(19) ? 0.5f : 1;

                    if (_currentMarketPlaceSpecial != null)
                    {
                        TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}");

                        if (_currentMarketPlaceSpecial == _weaponsMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more weapon choices");
                            GenerateWeaponChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 5), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                        else if (_currentMarketPlaceSpecial == _vehicleMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more vehicle choices");
                            GenerateVehicleChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 20), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                        else if (_currentMarketPlaceSpecial == _mercenaryMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more merc choices");
                            GenerateMercenaryChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 6), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                    }
                    GenerateWeaponChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 5), geoMarketPlace, voPriceMultiplier);
                    GenerateVehicleChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 10), geoMarketPlace, voPriceMultiplier);
                    GenerateMercenaryChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 6), geoMarketPlace, voPriceMultiplier);
                    GenerateResearchChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 8), geoMarketPlace, voPriceMultiplier);

                    if (TFTVAircraftReworkMain.AircraftReworkOn) //&& controller.EventSystem.GetVariable(_marketPlaceStockRotated) > 3)
                    {
                        TFTVAircraftReworkMain.MarketPlace.GenerateMarketPlaceModules(geoMarketPlace, voPriceMultiplier);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoMarketplaceOptionDef> CullAvailableOptionsBasedOnExternals(GeoLevelController controller, List<GeoMarketplaceOptionDef> options)
            {
                try
                {

                    if (controller != null && controller.NewJerichoFaction != null && controller.SynedrionFaction != null)
                    {
                        if (controller.NewJerichoFaction.Research.HasCompleted("NJ_VehicleTech_ResearchDef"))
                        {
                            //If complete, add more options
                            //   num += 3;
                        }
                        else
                        {
                            //Otherwise we remove NJ items from being rolled by GenerateRandomChoiceTFTV
                            options.RemoveRange(NewJericho_Items());
                        }
                        if (controller.SynedrionFaction.Research.HasCompleted("SYN_Rover_ResearchDef"))
                        {
                            // num += 3;
                        }
                        else
                        {
                            options.RemoveRange(Synedrion_Items());
                        }
                    }

                    return options;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static List<GeoEventChoice> GenerateWeaponChoices(List<GeoMarketplaceOptionDef> availableOptions,
                int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    List<GeoMarketplaceOptionDef> weaponsAvailable = GetOptionsByType(availableOptions, TFTVKaosGuns._kGTag);

                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (weaponsAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef weaponOffer = (GeoMarketplaceItemOptionDef)weaponsAvailable.GetRandomElement();

                        // TFTVLogger.Always($"weaponOffer is {weaponOffer.name}");

                        weaponsAvailable.Remove(weaponOffer);

                        int price = (int)(UnityEngine.Random.Range(weaponOffer.MinPrice, weaponOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(weaponOffer.ItemDef, price);
                        GeoMarketplaceItemOptionDef ammoOffer = TFTVKaosGuns._kGWeaponsAndAmmo[weaponOffer];

                        int ammoPrice = (int)(UnityEngine.Random.Range(ammoOffer.MinPrice, ammoOffer.MaxPrice) * priceModifier);

                        List<GeoEventChoice> ammo = new List<GeoEventChoice>()
                    {
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                    };

                        geoMarketplace.MarketplaceChoices.Add(item);
                        geoMarketplace.MarketplaceChoices.AddRange(ammo);
                        //  TFTVLogger.Always($"should have added {weaponOffer.name} and 3 ammo for it");
                    }

                    return list;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void GenerateVehicleChoices(List<GeoMarketplaceOptionDef> availableOptions,
                int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    // TFTVLogger.Always($"[GenerateVehicleChoices] Generating {numberToGenerate}");

                    List<GeoMarketplaceOptionDef> vehicleItemsAvailable = GetOptionsByType(availableOptions, _vehicle_ClassTagDef);

                    //  TFTVLogger.Always($"[GenerateVehicleChoices] available options {vehicleItemsAvailable}");


                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (vehicleItemsAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef vehicleItemToOffer;
                        if (x == 0)
                        {

                            vehicleItemToOffer = DefCache.GetDef<GeoMarketplaceItemOptionDef>("KasoBuggy_MarketplaceItemOptionDef");

                        }
                        else
                        {
                            vehicleItemToOffer = (GeoMarketplaceItemOptionDef)vehicleItemsAvailable.GetRandomElement();
                        }

                        // TFTVLogger.Always($"[GenerateVehicleChoices] vehicleItemToOffer is {vehicleItemToOffer.name}");

                        vehicleItemsAvailable.Remove(vehicleItemToOffer);

                        int price = (int)(UnityEngine.Random.Range(vehicleItemToOffer.MinPrice, vehicleItemToOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(vehicleItemToOffer.ItemDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);

                        if (TFTVAircraftReworkMain.AircraftReworkOn)
                        {
                            List<GeoMarketplaceItemOptionDef> ammoOffers = GetMarketplaceAmmoOffersForVehicleItem(vehicleItemToOffer);

                            foreach (GeoMarketplaceItemOptionDef ammoOffer in ammoOffers)
                            {
                                AddMarketplaceAmmoChoices(geoMarketplace, ammoOffer, priceModifier);
                            }
                        }

                        // TFTVLogger.Always($"should have added {vehicleItemToOffer.name}");
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void AddMarketplaceAmmoOfferForWeapon(WeaponDef weaponDef, List<GeoMarketplaceItemOptionDef> ammoOffers)
            {
                try
                {
                    if (weaponDef == null || ammoOffers == null)
                    {
                        return;
                    }

                    GeoMarketplaceItemOptionDef ammoOffer;
                    if (VehiclesAmmoMain.MarketplaceWeaponsAndAmmo.TryGetValue(weaponDef, out ammoOffer) &&
                        ammoOffer != null &&
                        !ammoOffers.Contains(ammoOffer))
                    {
                        ammoOffers.Add(ammoOffer);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void AddMarketplaceAmmoOfferForAmmoDef(TacticalItemDef ammoDef, List<GeoMarketplaceItemOptionDef> ammoOffers)
            {
                try
                {
                    if (ammoDef == null || ammoOffers == null)
                    {
                        return;
                    }

                    GeoMarketplaceItemOptionDef ammoOffer;
                    if (VehiclesAmmoMain.MarketplaceAmmoDefsAndOptions.TryGetValue(ammoDef, out ammoOffer) &&
                        ammoOffer != null &&
                        !ammoOffers.Contains(ammoOffer))
                    {
                        ammoOffers.Add(ammoOffer);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoMarketplaceItemOptionDef> GetMarketplaceAmmoOffersForVehicleItem(GeoMarketplaceItemOptionDef vehicleItemToOffer)
            {
                try
                {
                    List<GeoMarketplaceItemOptionDef> ammoOffers = new List<GeoMarketplaceItemOptionDef>();

                    if (vehicleItemToOffer == null || vehicleItemToOffer.ItemDef == null)
                    {
                        return ammoOffers;
                    }

                    WeaponDef weaponDef = vehicleItemToOffer.ItemDef as WeaponDef;
                    if (weaponDef != null)
                    {
                        AddMarketplaceAmmoOfferForWeapon(weaponDef, ammoOffers);
                    }

                    GroundVehicleModuleDef moduleDef = vehicleItemToOffer.ItemDef as GroundVehicleModuleDef;
                    if (moduleDef != null)
                    {
                        foreach (TacticalItemDef ammoDef in VehicleModuleAmmoHarmonyPatches.GetModuleAmmoDefs(moduleDef))
                        {
                            AddMarketplaceAmmoOfferForAmmoDef(ammoDef, ammoOffers);
                        }
                    }

                    if (vehicleItemToOffer == DefCache.GetDef<GeoMarketplaceItemOptionDef>("KasoBuggy_MarketplaceItemOptionDef"))
                    {
                        AddMarketplaceAmmoOfferForWeapon(DefCache.GetDef<WeaponDef>("KS_Buggy_Minigun_Vishnu_WeaponDef"), ammoOffers);
                        AddMarketplaceAmmoOfferForWeapon(DefCache.GetDef<WeaponDef>("KS_Buggy_The_Vishnu_Gun_Cannon_WeaponDef"), ammoOffers);
                    }

                    return ammoOffers;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoEventChoice> GenerateMercenaryChoices(List<GeoMarketplaceOptionDef> availableOptions,
               int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoMarketplaceOptionDef> mercernariesAvailable = GetOptionsByType(availableOptions, MercenaryTag);

                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (mercernariesAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef mercenaryToOffer = (GeoMarketplaceItemOptionDef)mercernariesAvailable.GetRandomElement();

                        mercernariesAvailable.Remove(mercenaryToOffer);

                        int price = (int)(UnityEngine.Random.Range(mercenaryToOffer.MinPrice, mercenaryToOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(mercenaryToOffer.ItemDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);
                        // TFTVLogger.Always($"should have added {mercenaryToOffer.name}");
                    }

                    return list;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void GenerateResearchChoices(List<GeoMarketplaceOptionDef> availableOptions,
              int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    List<GeoMarketplaceOptionDef> researchOptions = availableOptions.Where(o => o is GeoMarketplaceResearchOptionDef).ToList();

                    if (_currentMarketPlaceSpecial != null && _currentMarketPlaceSpecial == _researchMarketPlaceSpecial)
                    {
                        TFTVLogger.Always($"research special!");
                        numberToGenerate = 8;
                        priceModifier *= 0.5f;

                    }

                    if (researchOptions.Count == 0)
                    {
                        return;

                    }


                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (researchOptions.Count() == 0)
                        {
                            break;

                        }

                        GeoMarketplaceResearchOptionDef researchToOffer = (GeoMarketplaceResearchOptionDef)researchOptions.GetRandomElement();

                        researchOptions.Remove(researchToOffer);

                        int price = (int)(UnityEngine.Random.Range(researchToOffer.MinPrice, researchToOffer.MaxPrice) * priceModifier);

                        ResearchDef researchDef = researchToOffer.GetResearch();

                        if (researchDef == null)
                        {
                            break;
                        }


                        GeoEventChoice item = GenerateResearchChoice(researchDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);
                        //  TFTVLogger.Always($"should have added {researchDef.Id}");
                    }

                    _researchesAlreadyRolled.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<ResearchDef> _researchesAlreadyRolled = new List<ResearchDef>();

            [HarmonyPatch(typeof(GeoMarketplaceResearchOptionDef), "GetRandomResearch")] //VERIFIED
            public static class GeoMarketplaceResearchOptionDef_GetRandomResearch_MarketPlace_patch
            {
                public static bool Prefix(ref ResearchDef __result)
                {
                    try
                    {
                        GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        List<ResearchElement> list = new List<ResearchElement>();

                        for (int x = 0; x < level.FactionsWithDiplomacy.Count(); x++)
                        {
                            list.AddRange(level.FactionsWithDiplomacy.ElementAt(x).Research.Completed.Where((ResearchElement r) => r.IsAvailableToFaction(level.PhoenixFaction)).ToList());
                        }

                        if (list.Count == 0)
                        {
                            TFTVLogger.Always($"No researches! Player knows all!");
                            return false;
                        }

                        List<ResearchElement> phoenixFactionCompletedResearches = level.PhoenixFaction.Research.RevealedAndCompleted.ToList();
                        list.RemoveAll((ResearchElement research) => phoenixFactionCompletedResearches.Any((ResearchElement phoenixResearch) => research.ResearchID == phoenixResearch.ResearchID));

                        // TFTVLogger.Always($"_researchesAlreadyRolled has any elements in it? {_researchesAlreadyRolled.Count > 0}");

                        if (_researchesAlreadyRolled.Count > 0)
                        {
                            list.RemoveAll(e => _researchesAlreadyRolled.Contains(e.ResearchDef));
                            //  TFTVLogger.Always($"removing already rolled researches from pool");
                        }

                        if (list.Count != 0)
                        {
                            // TFTVLogger.Always($"There are {list.Count} researches that could be offered to the player in the Marketplace");
                            __result = list.ElementAt(UnityEngine.Random.Range(0, list.Count)).ResearchDef;
                            _researchesAlreadyRolled.Add(__result);
                        }
                        else
                        {
                            __result = null;
                        }
                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void CreateLogEntryAndRollSpecialsMarketplaceUpdated(GeoLevelController controller)
            {
                try
                {
                    // TFTVLogger.Always($"controller.Timing.Now {controller.Timing.Now}");

                    string textToDisplay = $"{TFTVCommonMethods.ConvertKeyToString("KEY_MARKETPLACE_NEW_STOCK")} {TFTVCommonMethods.ConvertKeyToString(_currentMarketPlaceSpecial)} ";

                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                    {
                        Text = new LocalizedTextBind(textToDisplay, true),
                        EventDate = controller.Timing.Now,
                    };

                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller.Log, new object[] { entry, null });
                    controller.View.SetGamePauseState(true);

                    _currentMarketPlaceSpecial = null;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            [HarmonyPatch(typeof(GeoMarketplace), nameof(GeoMarketplace.AfterMissionComplete))]
            public static class GeoMarketplace_AfterMissionComplete_patch
            {
                public static bool Prefix()
                {
                    try
                    {
                        TFTVLogger.Always($"Canceling GeoMarketPlace AfterMissionComplete");
                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static int GetMarketplaceChoiceCountForItem(GeoMarketplace geoMarketplace, ItemDef itemDef)
            {
                try
                {
                    if (geoMarketplace == null || itemDef == null || geoMarketplace.MarketplaceChoices == null)
                    {
                        return 0;
                    }

                    return geoMarketplace.MarketplaceChoices.Count(choice =>
                        choice != null &&
                        choice.Outcome != null &&
                        choice.Outcome.Items != null &&
                        choice.Outcome.Items.Count > 0 &&
                        choice.Outcome.Items[0].ItemDef == itemDef);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static int GetMarketplaceAmmoChoiceCap(GeoMarketplaceItemOptionDef ammoOffer)
            {
                try
                {
                    if (ammoOffer == null || ammoOffer.ItemDef == null)
                    {
                        return 0;
                    }

                    TacticalItemDef minigunAmmoDef = DefCache.GetDef<TacticalItemDef>("junkerMinigun_AmmoClipDef");

                    if (ammoOffer.ItemDef == minigunAmmoDef)
                    {
                        return 3;
                    }

                    return 2;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void AddMarketplaceAmmoChoices(GeoMarketplace geoMarketplace, GeoMarketplaceItemOptionDef ammoOffer, float priceModifier)
            {
                try
                {
                    if (geoMarketplace == null || ammoOffer == null || ammoOffer.ItemDef == null)
                    {
                        return;
                    }

                    int cap = GetMarketplaceAmmoChoiceCap(ammoOffer);
                    if (cap <= 0)
                    {
                        return;
                    }

                    int existingChoices = GetMarketplaceChoiceCountForItem(geoMarketplace, ammoOffer.ItemDef);
                    int choicesToAdd = Math.Max(0, cap - existingChoices);

                    for (int i = 0; i < choicesToAdd; i++)
                    {
                        int ammoPrice = (int)(UnityEngine.Random.Range(ammoOffer.MinPrice, ammoOffer.MaxPrice) * priceModifier);
                        geoMarketplace.MarketplaceChoices.Add(GenerateItemChoice(ammoOffer.ItemDef, ammoPrice));
                    }
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


