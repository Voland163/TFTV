using Base;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal class VehiclesAndMutogs
        {
            private static int CheckIfCharacterSpaceCostReduced(GeoVehicle geoVehicle, int occupancy)
            {
                try
                {
                    bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                    bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                    bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                    bool isHelios = geoVehicle.VehicleDef == helios;
                    bool aspidaResearchCompleted = HasCompletedResearch(geoVehicle, _aspidaResearchDef);

                    if (!hasHarness && !hasMutogPen && !isThunderbird && !(isHelios && aspidaResearchCompleted))
                    {
                        return occupancy;
                    }

                    List<GeoCharacter> geoCharacters = geoVehicle.Units.ToList();

                    int occupiedSpace = 0;

                    foreach (GeoCharacter geoCharacter in geoCharacters)
                    {
                        occupiedSpace += GetAdjustedCharacterSpace(geoCharacter, hasHarness, hasMutogPen, isThunderbird,
                             isHelios, aspidaResearchCompleted);

                    }
                   // TFTVLogger.Always($"CheckIfCharacterSpaceCostReduced for {geoVehicle.Name} {occupiedSpace}");
                    return occupiedSpace;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static int GetAdjustedCharacterSpace(GeoCharacter geoCharacter, bool hasHarness, bool hasMutogPen,
                      bool isThunderbird, bool isHelios, bool aspidaResearchCompleted)
            {
                try
                {
                    if (geoCharacter?.TemplateDef == null)
                    {
                        return 0;
                    }

                    int volume = geoCharacter.TemplateDef.Volume;

                    if (volume <= 0)
                    {
                        return volume;
                    }

                    bool isVehicle = geoCharacter.GameTags != null && geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag);
                    bool isMutog = geoCharacter.GameTags != null && geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag);

                    if (isMutog)
                    {
                        if (hasMutogPen)
                        {
                            return Math.Max(1, volume - 1);
                        }
                        return volume;
                    }

                    if (isVehicle)
                    {
                        int reducedVolume = volume;

                        if (isThunderbird)
                        {
                            reducedVolume -= _thunderbirdVehicleSpaceReduction;
                        }

                        if (hasHarness)
                        {
                            reducedVolume -= 1;
                        }

                        if (isHelios && aspidaResearchCompleted && geoCharacter.GameTags.Any(t => t == _aspidaClassTag))
                        {
                            reducedVolume -= 2;
                        }
                        return Math.Max(0, reducedVolume);
                    }
                    return volume;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static int GetAdjustedOccupiedSpace(GeoVehicle geoVehicle, int occupancy)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (config.VehicleAndMutogSize1)
                    {
                        List<GeoCharacter> geoCharacters = geoVehicle.Units.ToList();

                        int occupiedSpace = 0;

                        foreach (GeoCharacter geoCharacter in geoCharacters)
                        {
                            occupiedSpace += geoCharacter.OccupingSpace;
                        }

                        occupancy = occupiedSpace;
                    }

                    if (!AircraftReworkOn)
                    {
                        return occupancy;
                    }

                    return CheckIfCharacterSpaceCostReduced(geoVehicle, occupancy);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(GeoVehicle))]
            [HarmonyPatch("CurrentOccupiedSpace", MethodType.Getter)]
            public static class GeoVehicle_CurrentOccupiedSpace_Patch
            {
                public static void Postfix(GeoVehicle __instance, ref int __result)
                {
                    try
                    {
                        __result = GetAdjustedOccupiedSpace(__instance, __result);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(GeoVehicle), "get_UsedCharacterSpace")]
            public static class GeoVehicle_get_UsedCharacterSpace_Patch
            {
                public static void Postfix(GeoVehicle __instance, ref int __result)
                {
                    try
                    {
                        __result = GetAdjustedOccupiedSpace(__instance, __result);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(GeoVehicle), "get_FreeCharacterSpace")]
            public static class GeoVehicle_get_FreeCharacterSpace_Patch
            {
                public static void Postfix(GeoVehicle __instance, ref int __result)
                {
                    try
                    {
                        __result = __instance.MaxCharacterSpace - __instance.UsedCharacterSpace;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(GeoCharacter), "get_OccupingSpace")]
            public static class GeoCharacter_get_OccupingSpace_Patch
            {
                public static void Postfix(GeoCharacter __instance, ref int __result)
                {
                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (config.VehicleAndMutogSize1)
                        {
                            __result = 1;
                        }

                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        if (__result > 1)
                        {
                            GeoVehicle geoVehicle = TFTVCommonMethods.LocateSoldier(__instance);

                            if (geoVehicle == null)
                            {
                                return;
                            }

                            bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                            bool isHelios = geoVehicle.VehicleDef == helios;
                            bool aspidaResearchCompleted = HasCompletedResearch(geoVehicle, _aspidaResearchDef);

                            if (!hasHarness && !hasMutogPen && !isThunderbird && !(isHelios && aspidaResearchCompleted))
                            {
                                return;
                            }

                            __result = GetAdjustedCharacterSpace(__instance, hasHarness, hasMutogPen, isThunderbird,
                               isHelios, aspidaResearchCompleted);

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(TransferActionMenuElement), "Init")]
            public static class TransferActionMenuElement_Init_Patch
            {
                public static bool Prefix(TransferActionMenuElement __instance, IGeoCharacterContainer targetContainer, GeoRosterItem targetItem)
                {
                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (!AircraftReworkOn && !config.MultipleVehiclesInAircraftAllowed && !config.VehicleAndMutogSize1)
                        {
                            return true;
                        }

                        if (targetContainer is GeoVehicle)
                        {

                        }
                        else
                        {
                            return true;
                        }


                        if (targetItem.Character == null)
                        {
                            return true;
                        }

                        GeoVehicle geoVehicle = targetContainer as GeoVehicle;
                        GeoCharacter geoCharacter = targetItem.Character;

                        if (geoCharacter.TemplateDef.Volume < 2 && !config.VehicleAndMutogSize1)
                        {
                            return true;
                        }

                        int occupyingSpace = geoCharacter.TemplateDef.Volume;

                        if (config.VehicleAndMutogSize1)
                        {
                            occupyingSpace = 1;
                        }

                        PropertyInfo propertyInfo = typeof(TransferActionMenuElement).GetProperty("TargetContainer", BindingFlags.Public | BindingFlags.Instance);

                        propertyInfo.SetValue(__instance, targetContainer);

                        __instance.ContainerTextLabel.text = targetContainer.Name;
                        bool interactable = true;

                        bool hasHarness = false;
                        bool hasMutogPen = false;
                        bool isThunderbird = false;
                        bool isHelios = false;
                        bool aspidaResearchCompleted = false;


                        if (AircraftReworkOn)
                        {
                            hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                            hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                            isThunderbird = geoVehicle.VehicleDef == thunderbird;

                            isHelios = geoVehicle.VehicleDef == helios;
                            aspidaResearchCompleted = HasCompletedResearch(geoVehicle, _aspidaResearchDef);

                            if (!config.VehicleAndMutogSize1)
                            {
                                occupyingSpace = GetAdjustedCharacterSpace(geoCharacter, hasHarness, hasMutogPen, isThunderbird,
                                  isHelios, aspidaResearchCompleted);
                            }
                        }


                        __instance.ContainerTextLabel.text += $" [{targetContainer.CurrentOccupiedSpace}/{targetContainer.MaxCharacterSpace}]";
                        interactable = ((targetContainer.MaxCharacterSpace - targetContainer.CurrentOccupiedSpace >= occupyingSpace));

                        __instance.ErrorTooltip.gameObject.SetActive(value: false);

                        if (AircraftReworkOn)
                        {
                            if (!CheckVehicleMutogVehicleCapacity(geoVehicle, geoCharacter, isThunderbird, hasHarness, hasMutogPen))
                            {
                                __instance.ErrorTooltip.TipKey = __instance.VehicleErrorTextBind;
                                __instance.ErrorTooltip.gameObject.SetActive(value: true);
                                interactable = false;
                            }
                        }
                        else if (!config.MultipleVehiclesInAircraftAllowed && targetItem.Character.TemplateDef.Volume == 3)
                        {
                            foreach (GeoCharacter allCharacter in targetContainer.GetAllCharacters())
                            {
                                if (allCharacter.TemplateDef.Volume == targetItem.Character.TemplateDef.Volume)
                                {
                                    __instance.ErrorTooltip.TipKey = __instance.VehicleErrorTextBind;
                                    __instance.ErrorTooltip.gameObject.SetActive(value: true);
                                    interactable = false;
                                }
                            }
                        }


                        __instance.Button.interactable = interactable;

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static bool CheckVehicleMutogVehicleCapacity(GeoVehicle geoVehicle, GeoCharacter geoCharacter, bool thunderBird, bool hasHarness, bool mutogPen)
            {
                try
                {
                    bool isVehicle = geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag);
                    if (isVehicle && geoVehicle.VehicleDef == blimp)
                    {
                        return false;
                    }

                    int countVehicles = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag)).Count();
                    int countMutogs = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag)).Count();

                    // TFTVLogger.Always($"{geoVehicle.Name} has {countVehicles} vehicles, {geoCharacter.DisplayName}, has harness: {hasHarness} is thunderbird {thunderbird}");

                    if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag) && countVehicles > 0)
                    {
                        if (thunderBird && hasHarness && countVehicles < 2)
                        {
                            //       TFTVLogger.Always($"{geoCharacter.DisplayName} should return true");
                            return true;
                        }
                        return false;
                    }

                    if (geoCharacter.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag) && countMutogs > 0)
                    {
                        if (mutogPen && countMutogs < 2)
                        {
                            return true;
                        }
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
    }
}









