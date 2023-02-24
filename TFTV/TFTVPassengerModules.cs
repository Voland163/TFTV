using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVPassengerModules
    {
        
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

      /*  private static readonly GeoVehicleEquipmentDef hibernationModule = Repo.GetAllDefs<GeoVehicleEquipmentDef>().FirstOrDefault(gve => gve.name.Equals("SY_HibernationPods_GeoVehicleModuleDef"));

        private static readonly GeoVehicleDef manticore6slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_Manticore_Def_6_Slots"));
        private static readonly GeoVehicleDef manticore = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_Manticore_Def"));
        private static readonly GeoVehicleDef helios5slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("SYN_Helios_Def_5_Slots"));
        private static readonly GeoVehicleDef helios = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("SYN_Helios_Def"));
        private static readonly GeoVehicleDef thunderbird7slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Thunderbird_Def_7_Slots"));
        private static readonly GeoVehicleDef thunderbird = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Thunderbird_Def"));
        private static readonly GeoVehicleDef blimp12slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("ANU_Blimp_Def_12_Slots"));
        private static readonly GeoVehicleDef blimp8slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("ANU_Blimp_Def"));
        private static readonly GeoVehicleDef maskedManticore8slots = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_ManticoreMasked_Def_8_Slots"));
        private static readonly GeoVehicleDef maskedManticore = Repo.GetAllDefs<GeoVehicleDef>().FirstOrDefault(ged => ged.name.Equals("PP_MaskedManticore_Def"));*/


        
        private static readonly GeoVehicleEquipmentDef hibernationModule = DefCache.GetDef<GeoVehicleEquipmentDef>("SY_HibernationPods_GeoVehicleModuleDef");

        private static readonly GeoVehicleDef manticore6slots = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def_6_Slots");
        private static readonly GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
        private static readonly GeoVehicleDef helios5slots = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def_5_Slots");
        private static readonly GeoVehicleDef helios = DefCache.GetDef<GeoVehicleDef>("SYN_Helios_Def");
        private static readonly GeoVehicleDef thunderbird7slots = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def_7_Slots");
        private static readonly GeoVehicleDef thunderbird = DefCache.GetDef<GeoVehicleDef>("NJ_Thunderbird_Def");
        private static readonly GeoVehicleDef blimp12slots = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def_12_Slots");
        private static readonly GeoVehicleDef blimp8slots = DefCache.GetDef<GeoVehicleDef>("ANU_Blimp_Def");
        private static readonly GeoVehicleDef maskedManticore8slots = DefCache.GetDef<GeoVehicleDef>("PP_ManticoreMasked_Def_8_Slots");
        private static readonly GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");


        public static List<TacCharacterDef> CreateStartingSquad(GeoLevelController levelController)
        {
            try
            {
                List<TacCharacterDef> startingTemplates = new List<TacCharacterDef>();
                TFTVConfig config = TFTVMain.Main.Config;
                GameDifficultyLevelDef currentDifficultyLevel = levelController.CurrentDifficultyLevel;



                if (config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.UNBUFFED)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadUnbuffed(levelController);
                }
                else if (config.tutorialCharacters == TFTVConfig.StartingSquadCharacters.RANDOM)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadRandom(levelController);
                }
                else //if buffed
                {
                    startingTemplates = TFTVStarts.SetInitialSquadBuffed(currentDifficultyLevel, levelController);
                }

                return startingTemplates;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }




        [HarmonyPatch(typeof(GeoPhoenixFaction), "CreateInitialSquad")]
        internal static class BG_GeoPhoenixFaction_CreateInitialSquad_patch
        {
           

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(GeoPhoenixFaction __instance, GeoSite site)
            {
                try
                {
                    


                    TacticalItemDef redeemerAmmo = DefCache.GetDef<TacticalItemDef>("AN_Redemptor_AmmoClip_ItemDef");

                    TacticalItemDef pdwAmmo = DefCache.GetDef<TacticalItemDef>("NJ_Gauss_PDW_AmmoClip_ItemDef");
                    TacticalItemDef mechArmsAmmo = DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef");

                    TacticalItemDef boltsAmmo = DefCache.GetDef<TacticalItemDef>("SY_Crossbow_AmmoClip_ItemDef");
                    TacticalItemDef spidersAmmo = DefCache.GetDef<TacticalItemDef>("SY_SpiderDroneLauncher_AmmoClip_ItemDef");

                    TacCharacterDef jacob = DefCache.GetDef<TacCharacterDef>("PX_Jacob_TFTV_TacCharacterDef");
                    TacCharacterDef sophia = DefCache.GetDef<TacCharacterDef>("PX_Sophia_TFTV_TacCharacterDef");


                    GeoVehicle geoVehicle = __instance.Vehicles.First();
                    geoVehicle.AddEquipment(hibernationModule);

                    TFTVConfig config = TFTVMain.Main.Config;

                    List<TacCharacterDef> startingTemplates = CreateStartingSquad(__instance.GeoLevel);

                    GameDifficultyLevelDef currentDifficultyLevel = __instance.GeoLevel.CurrentDifficultyLevel;


                    foreach (TacCharacterDef template in startingTemplates)
                    {
                        GeoFaction geoFaction = new GeoFaction();

                        if (template.name.Equals("PX_Starting_Infiltrator_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.SynedrionFaction;
                        }
                        else if (template.name.Equals("PX_Starting_Priest_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.AnuFaction;
                        }
                        else if (template.name.Equals("PX_Starting_Technician_TacCharacterDef"))
                        {
                            geoFaction = __instance.GeoLevel.NewJerichoFaction;
                        }
                        else
                        {
                            geoFaction = __instance;
                        }

                        if (!template.name.Contains("Buffed") && template != jacob && template != sophia)
                        {
                            GeoUnitDescriptor geoUnitDescriptor = geoFaction.GeoLevel.CharacterGenerator.GenerateUnit(geoFaction, template);
                            geoFaction.GeoLevel.CharacterGenerator.ApplyGenerationParameters(geoUnitDescriptor, currentDifficultyLevel.StartingSquadGenerationParams);
                            geoFaction.GeoLevel.CharacterGenerator.RandomizeIdentity(geoUnitDescriptor);

                            GeoCharacter character = geoUnitDescriptor.SpawnAsCharacter();
                            geoVehicle.AddCharacter(character);
                        }
                        else
                        {
                            GeoCharacter character = geoFaction.GeoLevel.CreateCharacterFromTemplate(template, __instance);
                            geoVehicle.AddCharacter(character);
                        }
                    }

                    if (__instance.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 1)
                    {
                        TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.AnuFaction, site);
                    }
                    else if (__instance.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 2)
                    {
                        TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.NewJerichoFaction, site);
                    }
                    else if (__instance.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 3)
                    {
                        TFTVStarts.ModifyIntroForSpecialStart(__instance.GeoLevel.SynedrionFaction, site);
                    }
                    else
                    {
                        TFTVStarts.RevertIntroToNormalStart();
                    }

                    List<ItemUnit> startingStorage = currentDifficultyLevel.StartingStorage.ToList();


                    if (config.startingSquad == TFTVConfig.StartingSquadFaction.ANU)
                    {
                        startingStorage.Add(new ItemUnit(redeemerAmmo, 10));
                    }
                    else if (config.startingSquad == TFTVConfig.StartingSquadFaction.NJ)
                    {
                        startingStorage.Add(new ItemUnit(pdwAmmo, 10));
                        startingStorage.Add(new ItemUnit(mechArmsAmmo, 5));

                    }
                    else if (config.startingSquad == TFTVConfig.StartingSquadFaction.SYNEDRION)
                    {
                        startingStorage.Add(new ItemUnit(boltsAmmo, 20));
                        startingStorage.Add(new ItemUnit(spidersAmmo, 5));
                    }


                    foreach (ItemUnit itemUnit in startingStorage)
                    {
                        if (__instance.FactionDef.UseGlobalStorage)
                        {
                            __instance.ItemStorage.AddItem(new GeoItem(itemUnit));
                        }
                        else
                        {
                            site.ItemStorage.AddItem(new GeoItem(itemUnit));
                        }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), "ReplaceEquipments")]
        internal static class BG_GeoVehicle_ReplaceEquipments_RemoveExcessPassengers_patch
        {
            private static void Postfix(GeoVehicle __instance)
            {
                try
                {
                    if (__instance.CurrentSite != null && __instance.CurrentSite.Type == GeoSiteType.PhoenixBase)
                    {
                        if (!__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation)
                            || !__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed)
                            || !__instance.HasModuleBonusTo(GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range))
                        {
                            if (__instance.UsedCharacterSpace > __instance.MaxCharacterSpace)
                            {
                                List<GeoCharacter> list = new List<GeoCharacter>(from u in __instance.Units orderby u.OccupingSpace descending select u);
                                foreach (GeoCharacter character in list)
                                {
                                    if (__instance.FreeCharacterSpace >= 0)
                                    {
                                        break;
                                    }
                                    __instance.RemoveCharacter(character);
                                    __instance.CurrentSite.AddCharacter(character);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), "UpdateVehicleBonusCache")]
        internal static class BG_GeoVehicle_UpdateVehicleBonusCache_PassengerModulesIncreaseSpaceForUnits_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]

            private static void Postfix(GeoVehicle __instance)
            {
                try
                {

                    GeoVehicleEquipment hybernationPods = __instance.Modules?.FirstOrDefault(gve => gve != null && gve.ModuleDef != null && gve.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation);
                    GeoVehicleEquipment fuelTank = __instance.Modules?.FirstOrDefault(gve => gve != null && gve.ModuleDef != null && gve.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range);
                    GeoVehicleEquipment cruiseControl = __instance.Modules?.FirstOrDefault(gve => gve != null && gve.ModuleDef != null && gve.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed);
                    string geoVehicle = __instance.VehicleDef.ViewElement.Name;

                    //if hybernation pods are present, take the stats of the new defs with increased capacity
                    if (geoVehicle == "Geoscape Manticore")
                    {
                        if (hybernationPods != null || cruiseControl != null || fuelTank != null)
                        {
                            __instance.BaseDef = manticore6slots;
                        }
                        else
                        {
                            __instance.BaseDef = manticore;

                        }
                    }
                    if (geoVehicle == "Geoscape Helios")
                    {
                        if (hybernationPods != null || cruiseControl != null || fuelTank != null)
                        {
                            __instance.BaseDef = helios5slots;
                        }
                        else
                        {
                            __instance.BaseDef = helios;

                        }
                    }
                    if (geoVehicle == "Geoscape Thunderbird")
                    {
                        if (hybernationPods != null || cruiseControl != null || fuelTank != null)
                        {
                            __instance.BaseDef = thunderbird7slots;
                        }
                        else
                        {
                            __instance.BaseDef = thunderbird;

                        }
                    }
                    if (geoVehicle == "Geoscape Blimp")
                    {
                        if (hybernationPods != null || cruiseControl != null || fuelTank != null)
                        {
                            __instance.BaseDef = blimp12slots;
                        }
                        else
                        {
                            __instance.BaseDef = blimp8slots;

                        }
                    }
                    if (geoVehicle == "Geoscape Masked Manticore")
                    {
                        if (hybernationPods != null || cruiseControl != null || fuelTank != null)
                        {
                            __instance.BaseDef = maskedManticore8slots;
                        }
                        else
                        {
                            __instance.BaseDef = maskedManticore;

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoLevelController), "RunInterceptionTutorial")]
        public static class GeoLevelController_DontDestroyAircraft_Gift
        {
            private static GeoVehicle clonedAircraft;

            public static void Prefix(GeoLevelController __instance)
            {
                try
                {

                    GeoVehicle geoVehicle = __instance.View.SelectedActor as GeoVehicle;
                    string componentName = "PP_Manticore";
                    if (geoVehicle.VehicleDef.name.Contains("ANU_Blimp"))
                    {
                        componentName = "ANU_Blimp";
                    }
                    else if (geoVehicle.VehicleDef.name.Contains("NJ_Thunderbird"))
                    {
                        componentName = "NJ_Thunderbird";
                    }
                    else if (geoVehicle.VehicleDef.name.Contains("SYN_Helios"))
                    {
                        componentName = "SYN_Helios";
                    }
                    ComponentSetDef sourceAircraftComponentDef = DefCache.GetDef<ComponentSetDef>(componentName);
                    clonedAircraft = __instance.PhoenixFaction.CreateVehicle(geoVehicle.CurrentSite, sourceAircraftComponentDef);
                    clonedAircraft.RenameVehicle(geoVehicle.Name);
                    foreach (GeoVehicleEquipment equipment in geoVehicle.Equipments)
                    {
                        clonedAircraft.AddEquipment(equipment);
                    }

                    GeoSite geoSite = (from p in __instance.Map.SitesByType[GeoSiteType.PhoenixBase]
                                       where p.Owner == __instance.PhoenixFaction && p.State == GeoSiteState.Functioning
                                       select p into d
                                       orderby GeoMap.Distance(d, clonedAircraft.CurrentSite)
                                       select d).FirstOrDefault();
                    clonedAircraft.TeleportToSite(geoSite);
                    clonedAircraft.ReloadAllEquipments();


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}
