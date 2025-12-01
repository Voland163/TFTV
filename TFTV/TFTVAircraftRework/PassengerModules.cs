using Base;
using com.ootii.Helpers;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.View.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using static TFTV.TFTVAircraftReworkMain;
using static TFTV.AircraftReworkHelpers;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal class PassengerModules
        {
            private static bool CheckForPassengerModule(GeoVehicle geoVehicle)
            {
                try
                {
                    return geoVehicle.Modules != null && geoVehicle.Modules.Count() > 0 && geoVehicle.Modules.Any(m =>
                       m != null && m.ModuleDef != null && (
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Speed ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.SurvivalOdds ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Range ||
                            m.ModuleDef.BonusType == GeoVehicleModuleDef.GeoVehicleModuleBonusType.Recuperation
                        )
                    );


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

          
            private static int GetAdditionalMaxSpaceGeoVehicle(GeoVehicle geoVehicle)
            {
                try
                {
                    int additionalMaxSpace = 0;

                    if (AircraftReworkOn)
                    {
                        if (geoVehicle.Modules.Any(m => m?.ModuleDef == _basicPassengerModule))
                        {
                            additionalMaxSpace += 2;
                        }

                        if (geoVehicle.VehicleDef == helios && HasCompletedResearch(geoVehicle, _heliosMoonMissionResearchDef))
                        {
                            additionalMaxSpace += 2;
                        }     
                    }
                    else
                    {
                        if (CheckForPassengerModule(geoVehicle))
                        {
                            additionalMaxSpace += 4;
                        }
                    }

                    return additionalMaxSpace;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void AdjustMaxCharacterSpacePassengerModules(GeoVehicle geoVehicle, ref int maxSpace)
            {
                try
                {
                    if (AircraftReworkOn)
                    {
                        int additionalSpace = GetAdditionalMaxSpaceGeoVehicle(geoVehicle);

                        maxSpace += additionalSpace;
                    }
                    else
                    {
                        if (CheckForPassengerModule(geoVehicle))
                        {
                            maxSpace += 4;
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void AdjustAircraftInfoPassengerModules(GeoVehicle geoVehicle, ref AircraftInfoData aircraftInfo)
            {
                try
                {
                    if (AircraftReworkOn)
                    {
                        aircraftInfo.CurrentCrew = GetAdjustedPassengerManifestAircraftRework(geoVehicle);
                    }
                    else
                    {
                        if (CheckForPassengerModule(geoVehicle))
                        {
                            aircraftInfo.MaxCrew += 4;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CheckAircraftNewPassengerCapacity(GeoVehicle geoVehicle)
            {
                try
                {
                    if (geoVehicle.CurrentSite != null && geoVehicle.CurrentSite.Type == GeoSiteType.PhoenixBase)
                    {

                        if (geoVehicle.UsedCharacterSpace > geoVehicle.MaxCharacterSpace)
                        {
                            if (AircraftReworkOn)
                            {
                                RemoveExtraVehicleOrMutog(geoVehicle);

                            }
                            else
                            {
                                List<GeoCharacter> list = new List<GeoCharacter>(from u in geoVehicle.Units orderby u.OccupingSpace descending select u);
                                foreach (GeoCharacter character in list)
                                {
                                    if (geoVehicle.FreeCharacterSpace >= 0)
                                    {
                                        break;
                                    }
                                    geoVehicle.RemoveCharacter(character);
                                    geoVehicle.CurrentSite.AddCharacter(character);
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

            private static void RemoveExtraVehicleOrMutog(GeoVehicle geoVehicle)
            {
                try

                {
                    int countVehicles = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag)).Count();
                    int countMutogs = geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag)).Count();

                    bool hasHarness = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _vehicleHarnessModule);
                    bool hasMutogPen = geoVehicle.Modules.Any(m => m != null && m.ModuleDef == _blimpMutogPenModule);
                    bool isThunderbird = geoVehicle.VehicleDef == thunderbird;

                    bool isHelios = geoVehicle.VehicleDef == helios;
                    bool aspidaResearchCompleted = HasCompletedResearch(geoVehicle, _aspidaResearchDef);

                    // TFTVLogger.Always($"{geoVehicle.Name} has {countVehicles} vehicles, {geoCharacter.DisplayName}, has harness: {hasHarness} is thunderbird {thunderbird}");

                    if (AircraftReworkOn && geoVehicle.VehicleDef == blimp && countVehicles > 0)
                    {
                        foreach (GeoCharacter vehicle in geoVehicle.Units.Where(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag)).ToList())
                        {
                            geoVehicle.RemoveCharacter(vehicle);

                            geoVehicle.CurrentSite?.AddCharacter(vehicle);
                        }
                    }
                    else if (countVehicles > 1)
                    {
                        if (isThunderbird && hasHarness && countVehicles < 3)
                        {

                        }
                        else
                        {
                            GeoCharacter geoCharacter = geoVehicle.Units.FirstOrDefault(c => c.GameTags.Any(t => t == Shared.SharedGameTags.VehicleTag));
                            geoVehicle.RemoveCharacter(geoCharacter);
                            geoVehicle.CurrentSite.AddCharacter(geoCharacter);

                        }
                    }

                    if (countMutogs > 1)
                    {
                        if (hasMutogPen && countMutogs < 2)
                        {

                        }
                        else
                        {

                            GeoCharacter geoCharacter = geoVehicle.Units.FirstOrDefault(c => c.GameTags.Any(t => t == Shared.SharedGameTags.MutogTag));
                            geoVehicle.RemoveCharacter(geoCharacter);
                            geoVehicle.CurrentSite.AddCharacter(geoCharacter);

                        }
                    }

                    if (geoVehicle.FreeCharacterSpace < 0)
                    {
                        List<GeoCharacter> list = new List<GeoCharacter>(from u in geoVehicle.Units orderby u.OccupingSpace descending select u);

                        foreach (GeoCharacter character in list)
                        {
                            if (geoVehicle.FreeCharacterSpace >= 0)
                            {
                                break;
                            }

                            geoVehicle.RemoveCharacter(character);
                            geoVehicle.CurrentSite?.AddCharacter(character);
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





