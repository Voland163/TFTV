using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.View;
using System;
using System.Linq;
using System.Reflection;

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

        [HarmonyPatch(typeof(GeoscapeView), "SetSelectedActor")]
        internal static class GeoscapeViewSelectionPatch
        {
            private static readonly FieldInfo ContextField = AccessTools.Field(typeof(GeoscapeView), "_context");

            private static void Prefix(GeoscapeView __instance, ref GeoActor actor)
            {
                if (!TFTVAircraftReworkMain.AircraftReworkOn) 
                {
                    return;
                }

                if (actor is GeoVehicle geoVehicle && !geoVehicle.IsOwnedByViewer)
                {
                    GeoscapeViewContext context = ContextField?.GetValue(__instance) as GeoscapeViewContext;
                    GeoVehicle fallbackVehicle = context?.ViewerFaction?.Vehicles?.FirstOrDefault();
                    actor = fallbackVehicle;
                }
            }
        }

    }
}





