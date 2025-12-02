using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVRevertSGPatchBalance
    {
        internal static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static readonly DefRepository Repo = TFTVMain.Repo;
        internal static readonly SharedData Shared = TFTVMain.Shared;


        public static void RevertSGPatchBalanceChanges()
        {
            IncreaseAmmoCost();
            IncreaseConstructionCosts();
            DecreaseManticoresRange();
            IncreasePXResearchTimes();
            ReBuffBlastChirons();
        }


        //increases ammo cost by 25% from the patch values to revert 20% decrease 
        private static void IncreaseAmmoCost()
        {
            try
            {
                foreach (TacticalItemDef tacticalItemDef in Repo.GetAllDefs<TacticalItemDef>().Where(tid => tid.Tags.Any(t => t.name.Equals("AmmoItem_TagDef"))))
                {

                    tacticalItemDef.ManufactureMaterials *= 1.25f;
                    tacticalItemDef.ManufactureTech *= 1.25f;

                    TFTVLogger.Always($"[AmmoCost]{tacticalItemDef.name} mat cost {tacticalItemDef.ManufactureMaterials} tech cost {tacticalItemDef.ManufactureTech}", false);

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Patch increased range of Manticore and Masked Manticore from 2500 to 2600
        private static void DecreaseManticoresRange()
        {
            try
            {
                GeoVehicleDef manticore = DefCache.GetDef<GeoVehicleDef>("PP_Manticore_Def");
                GeoVehicleDef maskedManticore = DefCache.GetDef<GeoVehicleDef>("PP_MaskedManticore_Def");

                manticore.BaseStats.MaximumRange.Value = 2500;
                maskedManticore.BaseStats.MaximumRange.Value = 2500;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //increases construction costs by 25% from the patch values to revert 20% decrease; also fixes Access Lift cost and time
        private static void IncreaseConstructionCosts()
        {
            try
            {
                float increaseCostFactor = 1.25f;

                // Specific override for Access Lift
                PhoenixFacilityDef accessLiftFacility = DefCache.GetDef<PhoenixFacilityDef>("AccessLift_PhoenixFacilityDef");
                accessLiftFacility.ResourceCost = new ResourcePack()
                {
                    new ResourceUnit(ResourceType.Materials, 200),
                    new ResourceUnit(ResourceType.Tech, 10)
                };
                accessLiftFacility.ConstructionTimeDays = 2;

                // Apply same increase to all listed facility defs
                string[] facilityDefNames =
                {
                    "AlienContainment_PhoenixFacilityDef",
                    "ArcheologyLab_PhoenixFacilityDef",
                    "BionicsLab_PhoenixFacilityDef",
                    "EnergyGenerator_PhoenixFacilityDef",
                    "Entrance_PhoenixFacilityDef",
                    "FabricationPlant_PhoenixFacilityDef",
                    "FoodProduction_PhoenixFacilityDef",
                    "LivingQuarters_PhoenixFacilityDef",
                    "MedicalBay_PhoenixFacilityDef",
                    "MistRepeller_PhoenixFacilityDef",
                    "MutationLab_PhoenixFacilityDef",
                    "ResearchLab_PhoenixFacilityDef",
                    "SatelliteUplink_PhoenixFacilityDef",
                    "SecurityStation_PhoenixFacilityDef",
                    "Stores_PhoenixFacilityDef",
                    "TrainingFacility_PhoenixFacilityDef",
                    "VehicleBay_PhoenixFacilityDef"
                };

                foreach (string defName in facilityDefNames)
                {
                    PhoenixFacilityDef facility = DefCache.GetDef<PhoenixFacilityDef>(defName);
                    for (int i = 0; i < facility.ResourceCost.Count; i++)
                    {
                        ResourceUnit unit = facility.ResourceCost[i];
                        unit.Value *= increaseCostFactor;
                        facility.ResourceCost[i] = unit;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void IncreasePXResearchTimes()
        {
            try
            {
                float increaseTimeFactor = 1.25f;
                
                ResearchDbDef researchDbDef = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                foreach (ResearchDef researchDef in researchDbDef.Researches)
                {
                    float newDuration = researchDef.ResearchCost * increaseTimeFactor;
                    researchDef.ResearchCost = (int)newDuration;                    
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ReBuffBlastChirons()
        {
            try 
            {
                DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef").DamagePayload.Range = 40;

                WeaponDef crystalChiron = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Crystal_Mortar_WeaponDef");
                crystalChiron.DamagePayload.Range = 40;
                crystalChiron.HitPoints = 800;
                crystalChiron.BodyPartAspectDef.Endurance = 10;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
