using Base;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Linq;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal class Healing
        {




            public static float GetRepairBionicsCostFactor(GeoCharacter geoCharacter)
            {
                try
                {
                    if (geoCharacter.Faction.Vehicles.Any(v => v.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule)
                    && v.Units.Contains(geoCharacter)))
                    {
                        int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs) - 1;
                        float repairCostFactor = _workshopBuffBionicRepairCostReduction * buffLevel;
                        if (buffLevel > 0)
                        {
                            return (1 - repairCostFactor);
                        }
                    }

                    return 1f;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            [HarmonyPatch(typeof(GeoPhoenixFaction), "UpdateCharactersInVehicles")]
            public static class GeoPhoenixFaction_UpdateCharactersInVehicles_Patch
            {
                static void Postfix(GeoPhoenixFaction __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        float healingFactor = _healingHPBase; //10
                                                              //  float buffPerlevel = _healingBuffPerLevel; //2

                        foreach (GeoVehicle geoVehicle in __instance.Vehicles)
                        {
                            if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _heliosPanaceaModule))
                            {
                                int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_heliosStatisChamberBuffResearchDefs) == 4 ? 1 : 0;
                                float healAmount = healingFactor + healingFactor * buffLevel; //10+10*(0 OR 1)*2, so 10 or 20
                                float staminaAmount = _heliosPanaceaModule.GeoVehicleModuleBonusValue +
                                    _heliosPanaceaModule.GeoVehicleModuleBonusValue * buffLevel; //0.35f + 0.35f * 2 * 1, so 0.35 or 0.7

                                foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                                {
                                    geoCharacter.Heal(healAmount);
                                    geoCharacter.Fatigue.Stamina.AddRestrictedToMax(staminaAmount);
                                }
                            }


                            if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdWorkshopModule))
                            {
                                int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_thunderbirdWorkshopBuffResearchDefs) - 1; //0-2
                                float healAmount = healingFactor + healingFactor * buffLevel; //10+10*(0-3), so 10, 20 or 30
                                float staminaAmount = _thunderbirdWorkshopModule.GeoVehicleModuleBonusValue
                                    + _thunderbirdWorkshopModule.GeoVehicleModuleBonusValue * buffLevel; // 0.35f + 0.35f * (0-2), so 0, 0.7, or 1.4

                                foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                {
                                    if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag))
                                        || geoCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                    {
                                        // TFTVLogger.Always($"{geoCharacter.DisplayName} in {geoVehicle.Name} has {geoCharacter.Health} HP, {geoCharacter.Fatigue?.Stamina} Stamina");
                                        geoCharacter.Heal(healAmount);
                                        geoCharacter.Fatigue?.Stamina?.AddRestrictedToMax(staminaAmount);
                                    }
                                }
                            }

                            if (geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutationLabModule))
                            {
                                int buffLevel = Tiers.GetBuffLevelFromResearchDefs(_blimpMutationLabModuleBuffResearches) - 1;
                                float healAmount = healingFactor + healingFactor * buffLevel; //10 + 10 * (0-2), so 10, 20 or 30
                                float staminaAmount = _blimpMutationLabModule.GeoVehicleModuleBonusValue +
                                    _blimpMutationLabModule.GeoVehicleModuleBonusValue * buffLevel;

                                foreach (GeoCharacter geoCharacter in geoVehicle.Units)
                                {
                                    /*  TFTVLogger.Always($"{geoCharacter.DisplayName}");

                                      foreach(GameTagDef gameTagDef in geoCharacter.GameTags) 
                                      {
                                          TFTVLogger.Always($"has {gameTagDef.name}", false);

                                      }*/


                                    if (geoCharacter.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                        || geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                                        || geoCharacter.GameTags.Contains(DefCache.GetDef<ClassTagDef>("Mutoid_ClassTagDef"))
                                        || geoCharacter.Progression.MainSpecDef == TFTVChangesToDLC5.TFTVMercenaries.SlugClassTagDef)
                                    {
                                        if (geoCharacter.IsAlive && geoCharacter.IsInjured)
                                        {
                                            geoCharacter.Health.AddRestrictedToMax(healAmount);
                                        }
                                        geoCharacter.Fatigue?.Stamina?.AddRestrictedToMax(staminaAmount);
                                        // TFTVLogger.Always($"{geoCharacter.DisplayName} getting {healAmount} healing, {extraStamina} stamina");
                                    }
                                }
                            }
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
}





