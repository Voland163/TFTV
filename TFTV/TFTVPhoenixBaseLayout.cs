using Base;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PhoenixPoint.Geoscape.Entities.PhoenixBases.GeoPhoenixBaseTemplate;

namespace TFTV
{
    internal class TFTVPhoenixBaseLayout
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static bool CheckLayout(GeoPhoenixBaseLayout layout)
        {
            try
            {
                if (layout.Facilities.Any(f => f.Def.Size == 2))
                {
                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static GeoPhoenixBaseLayout GenerateBaseLayout(GeoPhoenixBaseTemplate template, GeoPhoenixBaseLayoutDef layoutDef, int randomSeed)
        {
            try
            {
                PhoenixFacilityDef accessLift = DefCache.GetDef<PhoenixFacilityDef>("AccessLift_PhoenixFacilityDef");

                UnityEngine.Random.InitState(randomSeed);
                PhoenixBaseLayoutEdge entrance = PhoenixBaseLayoutEdge.Bottom;
                GeoPhoenixBaseLayout geoPhoenixBaseLayout = new GeoPhoenixBaseLayout(layoutDef, entrance);
                Vector2Int randomElement = geoPhoenixBaseLayout.GetBuildableTiles().GetRandomElement();
                geoPhoenixBaseLayout.PlaceBaseEntrance(randomElement);
                IList<PhoenixFacilityData> list = template.FacilityData.ToList().Shuffle();
                List<Vector2Int> list2 = new List<Vector2Int>();

                int facilitiesCount = list.Count;

                //  TFTVLogger.Always($"Checking facilities in list for base {template.Name.Localize()}");

                foreach (PhoenixFacilityData facilityData in list)
                {
                    TFTVLogger.Always($"{facilityData.FacilityDef.name}", false);
                }

                while (list.Count > 0)
                {
                    PhoenixFacilityData phoenixFacilityData = null;

                    if (list.Count == list.Count - 2) //place hangar 3rd
                    {
                        IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef.Size == 2);
                        phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());
                        //   TFTVLogger.Always($"Facility to be placed: {phoenixFacilityData?.FacilityDef?.name}");

                    }
                    else if (list.Count == list.Count - 4) //place access lift with at least one space to hangar
                    {
                        IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef == accessLift);
                        phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());
                        //   TFTVLogger.Always($"Facility to be placed: {phoenixFacilityData?.FacilityDef?.name}");
                    }
                    else //place anything except access lift or hangar in between
                    {
                        IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef.Size == 1 && f.FacilityDef != accessLift);
                        phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());
                        //  TFTVLogger.Always($"Facility to be placed: {phoenixFacilityData?.FacilityDef?.name}");
                    }

                    list2.Clear();
                    geoPhoenixBaseLayout.GetBuildableTilesForFacility(phoenixFacilityData.FacilityDef, list2);
                    if (list2.Count == 0)
                    {
                        Debug.LogError("No tiles available for placing facility " + phoenixFacilityData.FacilityDef.name + "!");
                    }
                    else
                    {
                        GeoPhoenixFacility geoPhoenixFacility = geoPhoenixBaseLayout.PlaceFacility(position: list2.GetRandomElement(), facilityDef: phoenixFacilityData.FacilityDef, dontPlaceCorridors: false);
                        if (template.ApplyFullDamageOnFacilities)
                        {
                            geoPhoenixFacility.ApplyFullDamageOnFacility();
                        }
                        else if (phoenixFacilityData.StartingHealth < 100)
                        {
                            geoPhoenixFacility.DamageFacility(100 - phoenixFacilityData.StartingHealth);
                        }

                        TFTVLogger.Always($"Facility placed: {geoPhoenixFacility.Def.name}");
                    }

                    if (0 == 0)
                    {
                        list.Remove(phoenixFacilityData);
                    }
                }

                int num = template.BlockedTiles.RandomValue();
                for (int i = 0; i < num; i++)
                {
                    ICollection<Vector2Int> rockPlacableTiles = geoPhoenixBaseLayout.GetRockPlacableTiles();
                    if (rockPlacableTiles.Count() == 0)
                    {
                        Debug.LogWarning("No place left to place rock in phoenix base!");
                        break;
                    }

                    Vector2Int randomElement3 = rockPlacableTiles.GetRandomElement();
                    geoPhoenixBaseLayout.PlaceRock(randomElement3);
                }

                return geoPhoenixBaseLayout;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }


        [HarmonyPatch(typeof(GeoPhoenixBaseTemplate), "CreateBaseLayout")]
        public static class TFTV_GeoPhoenixBaseTemplate_CreateBaseLayout_patch
        {

            public static bool Prefix(GeoPhoenixBaseTemplate __instance, ref GeoPhoenixBaseLayout __result, GeoPhoenixBaseLayoutDef layoutDef, int randomSeed)
            {
                try
                {


                    GeoPhoenixBaseLayout layout = GenerateBaseLayout(__instance, layoutDef, randomSeed);

                    if (CheckLayout(layout))
                    {
                        __result = layout;
                        return false;
                    }

                    TFTVLogger.Always($"Failed to generate TFTV layout! Allowing to generate regular layout");

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
