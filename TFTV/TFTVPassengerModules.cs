using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
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
using static TFTV.TFTVNewGameMenu;
using System.Reflection;

namespace TFTV
{
    internal class TFTVPassengerModules
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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



                if (TFTVNewGameOptions.startingSquadCharacters == TFTVNewGameOptions.StartingSquadCharacters.UNBUFFED)
                {
                    startingTemplates = TFTVStarts.SetInitialSquadUnbuffed(levelController);
                }
                else if (TFTVNewGameOptions.startingSquadCharacters == TFTVNewGameOptions.StartingSquadCharacters.RANDOM)
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


        public static void ImplementFarMConfig(GeoLevelController controller)
        {
            try 
            {
               

                foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles) 
                {
                    MethodInfo methodInfo = typeof(GeoVehicle).GetMethod("UpdateVehicleBonusCache", BindingFlags.NonPublic | BindingFlags.Instance);

                //    TFTVLogger.Always($"method is null? {methodInfo==null}");
                    methodInfo.Invoke(geoVehicle, null);


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(GeoVehicle), "GetModuleBonusByType")]

        public static class TFTV_Experimental_GeoVehicle_GetModuleBonusByType_AdjustFARMRecuperationModule_patch
        {
            public static void Postfix(GeoVehicleModuleDef.GeoVehicleModuleBonusType type, ref float __result, GeoVehicle __instance)
            {
                try
                {
                    GeoVehicleEquipment hybernationPods = __instance.Modules?.FirstOrDefault(gve => gve != null && gve.ModuleDef != null && gve.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation);

                    if (hybernationPods != null && type == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation)
                    {
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (config.ActivateStaminaRecuperatonModule)
                        {
                           // TFTVLogger.Always($"geovehicle is {__instance.name}");
                            __result = 0.35f;

                        }
                        else
                        {

                            __result = 0.0f;

                        }

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
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
                  

                    List<ItemUnit> startingStorage = currentDifficultyLevel.StartingStorage.ToList();


                    if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.ANU)
                    {
                        startingStorage.Add(new ItemUnit(redeemerAmmo, 10));
                    }
                    else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.NJ)
                    {
                        startingStorage.Add(new ItemUnit(pdwAmmo, 10));
                        startingStorage.Add(new ItemUnit(mechArmsAmmo, 5));

                    }
                    else if (TFTVNewGameOptions.startingSquad == TFTVNewGameOptions.StartingSquadFaction.SYNEDRION)
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

            private static void Postfix(GeoVehicle __instance)//, Dictionary<GeoVehicleModuleDef.GeoVehicleModuleBonusType, float> ____vehicleModuleBonusCache)
            {
                try
                {
                 /*   TFTVLogger.Always($"{__instance.VehicleDef.ViewElement.Name}");

                    TFTVLogger.Always($"Modules present {__instance?.Modules?.Count()}");

                    foreach(GeoVehicleEquipment geoVehicleEquipment in __instance.Modules) 
                    {
                        TFTVLogger.Always($"{geoVehicleEquipment?.ModuleDef?.name}");
                    
                    
                    }*/


                    bool passengerModulePresent = __instance.Modules != null && __instance.Modules.Count()>0 && __instance.Modules.Any(m =>
                       m!=null && m.ModuleDef != null && (
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.SurvivalOdds ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation
                        )
                    );

                    string geoVehicle = __instance.VehicleDef.ViewElement.Name;

                  //  TFTVLogger.Always($"{__instance.VehicleDef.ViewElement.Name} {passengerModulePresent}");

                    switch (geoVehicle)
                    {
                        case "Geoscape Manticore":
                            {
                                if (passengerModulePresent)
                                {

                                    __instance.BaseDef = manticore6slots;

                                }
                                else
                                {

                                    __instance.BaseDef = manticore;

                                }

                                break;
                            }

                        case "Geoscape Helios":
                            {
                                if (passengerModulePresent)
                                {
                                    __instance.BaseDef = helios5slots;
                                }
                                else
                                {
                                    __instance.BaseDef = helios;

                                }

                                break;


                            }

                        case "Geoscape Thunderbird":
                            {
                                if (passengerModulePresent)
                                {
                                    __instance.BaseDef = thunderbird7slots;
                                }
                                else
                                {
                                    __instance.BaseDef = thunderbird;

                                }

                                break;


                            }
                        case "Geoscape Blimp":
                            {
                                if (passengerModulePresent)
                                {
                                    __instance.BaseDef = blimp12slots;
                                }
                                else
                                {
                                    __instance.BaseDef = blimp8slots;

                                }

                                break;

                            }

                        case "Geoscape Masked Manticore":
                            {
                                if (passengerModulePresent)
                                {
                                    __instance.BaseDef = maskedManticore8slots;
                                }
                                else
                                {
                                    __instance.BaseDef = maskedManticore;

                                }

                                break;

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
                    //      
                    //  TFTVLogger.Always($"Phoenix characters on site: {geoVehicle.CurrentSite.GetAllCharacters().Where(c => c.Faction == geoVehicle.Owner).Count()}");

                    //  if (__instance.EventSystem.IsEventTriggered("PROG_FS1_FAIL"))//geoVehicle.CurrentSite.GetAllCharacters().Any(c=>c.Faction==geoVehicle.Owner))//)
                    //  {
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

                 /*   }
                    else 
                    {

                        TFTVLogger.Always($"Failed the Hatching, so not getting your original craft back!");
                    
                    }*/


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}
