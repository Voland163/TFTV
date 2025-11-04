using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using System;

namespace TFTV
{

    internal partial class AircraftReworkGeoscape
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static void AddAbilityToGeoVehicle(GeoVehicle geoVehicle, GeoAbilityDef geoAbility)
        {
            try
            {

                geoVehicle.AddAbility(geoAbility, geoVehicle);
                TFTVLogger.Always($"Added {geoAbility.name} to {geoVehicle.Name}");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void RemoveAbilityFromVehicle(GeoVehicle geoVehicle, GeoAbilityDef geoAbility)
        {
            try
            {
                geoVehicle.RemoveAbility(geoAbility);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}





