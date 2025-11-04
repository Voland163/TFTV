using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using System;
using System.Linq;
using static TFTV.TFTVAircraftReworkMain;
using Research = PhoenixPoint.Geoscape.Entities.Research.Research;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal class OverdriveRange
        {

            internal static float GetThunderbirdOverdriveRange(Research phoenixResearch)
            {
                try
                {


                    float rangeBuff = _thunderbirdRangeBuffPerLevel;

                    foreach (ResearchDef researchDef in _thunderbirdRangeBuffResearchDefs)
                    {
                        if (phoenixResearch.HasCompleted(researchDef.Id))
                        {
                            rangeBuff += _thunderbirdRangeBuffPerLevel;
                        }
                    }

                    return rangeBuff;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            [HarmonyPatch(typeof(GeoVehicle), "UpdateVehicleBonusCache")]
            internal static class GeoVehicle_UpdateVehicleBonusCache_patch
            {
                private static void Prefix(GeoVehicle __instance)
                {
                    try
                    {
                        if (!AircraftReworkOn)
                        {
                            return;
                        }

                        Research research = __instance?.GeoLevel?.PhoenixFaction?.Research;

                        if (__instance.Modules.Any(m => m != null && m.ModuleDef == _thunderbirdRangeModule) && research != null)
                        {
                            float range = GetThunderbirdOverdriveRange(research);
                            _thunderbirdRangeModule.GeoVehicleModuleBonusValue = range;
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





