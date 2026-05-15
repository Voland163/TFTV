using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVBaseRework;
using UnityEngine;

namespace TFTV
{
    internal class TFTVCapturePandoransGeoscape
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly string GeoVariableFoodPoisoning = "FoodPoisoning";


        //LucusTheDestroyer algorithm for food poisoning from harvesting Pandoran meat

        public static float ToxinsInCirculation = 0;

        internal static int StorageCapacity = 250;
        internal static int ProcessingCapacity = 50;

        private static bool ShouldUseExplicitFoodAndMutagenGeneration()
        {
            return BaseReworkCheck.BaseReworkEnabled || TFTVNewGameOptions.LimitedHarvestingSetting;
        }

        private static bool ShouldUseHarvestProcessing()
        {
            return TFTVNewGameOptions.LimitedHarvestingSetting;
        }

        public static void OnProductionUpdate(float currentProduction)
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                float SafeProductionAmount = 4 - (controller.CurrentDifficultyLevel.Order / 2);

                ToxinsInCirculation = Mathf.Max(0, ToxinsInCirculation + ((currentProduction - SafeProductionAmount) / 100));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void TriggerFoodPoisoning(GeoLevelController controller)
        {
            try
            {
                if (UnityEngine.Random.Range(1, 7) == 6)
                {
                    float PoisoningChance = ToxinsInCirculation * ToxinsInCirculation; //x^2 function

                    float ProbabilityRoll = UnityEngine.Random.Range(0f, 100f);

                    //  TFTVLogger.Always($"rolling for poisoning! chance {PoisoningChance}, roll {ProbabilityRoll}");

                    if (ProbabilityRoll < PoisoningChance)
                    {
                        ToxinsInCirculation = 0;
                        FoodPoisoningEffects(controller);

                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void FoodPoisoningEffects(GeoLevelController controller)
        {
            try
            {
                controller.EventSystem.SetVariable(GeoVariableFoodPoisoning, controller.EventSystem.GetVariable(GeoVariableFoodPoisoning) + 1);

                int severity = Math.Min(controller.EventSystem.GetVariable(GeoVariableFoodPoisoning), 3);
                string eventID = $"FoodPoisoning{severity}";

                GeoscapeEventContext context = new GeoscapeEventContext(controller.PhoenixFaction, controller.PhoenixFaction);

                controller.EventSystem.TriggerGeoscapeEvent(eventID, context);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void RefreshFoodAndMutagenProductionTooltupUI()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                UIModuleInfoBar uIModuleInfoBar = controller.View.GeoscapeModules.ResourcesModule;

                UIAnimatedResourceController foodController = uIModuleInfoBar.FoodController;
                MethodInfo methodDisplayValue = typeof(UIAnimatedResourceController).GetMethod("DisplayValue", BindingFlags.NonPublic | BindingFlags.Instance);

                int foodProductionFacilitiesCount = CountFoodProductionFacilities(controller.PhoenixFaction);
                int facilityFoodPerDay = Mathf.RoundToInt(GetFoodFacilityOutputPerDay(controller.PhoenixFaction));
                int processedFoodPerDay = Mathf.RoundToInt(GetProcessedFoodOutputPerDay(controller.PhoenixFaction));

                int foodConsumptionPerDay = BaseReworkCheck.BaseReworkEnabled
                    ? FoodAndLivingSpacePolicy.GetTotalFoodConsumptionPerDay(controller.PhoenixFaction)
                    : controller.PhoenixFaction.Characters.Count(character => character?.Fatigue != null);

                foodController.Income = facilityFoodPerDay + processedFoodPerDay - foodConsumptionPerDay;

               /* string consumptionLog = BaseReworkCheck.BaseReworkEnabled
                    ? FoodAndLivingSpacePolicy.GetBreakdownForLog(controller.PhoenixFaction)
                    : $"mode=VanillaFatigueCharacters total={foodConsumptionPerDay}";
               */
              //  TFTVLogger.Always(
                //    $"income {foodController.Income}, from {facilityFoodPerDay} from {foodProductionFacilitiesCount} facilities plus {processedFoodPerDay} minus {foodConsumptionPerDay}; {consumptionLog}");

                methodDisplayValue.Invoke(foodController, null);

                if (PandasForFoodProcessing > 0)
                {
                    UITooltipText tooltipText = uIModuleInfoBar.GetComponentsInChildren<UITooltipText>()
                        .FirstOrDefault(utt =>
                            utt.TipKey != null
                            && utt.TipKey.LocalizationKey != null
                            && (utt.TipKey.LocalizeEnglish().Contains("FOOD") || utt.TipKey.LocalizeEnglish().Contains("Pandoran meat")));

                    string processingPandoranMeat = new LocalizedTextBind() { LocalizationKey = "KEY_PANDORAN_MEAT_PROCESSING" }.Localize();
                    string pandoranMeatToProcess = new LocalizedTextBind() { LocalizationKey = "KEY_PANDORAN_MEAT_TO_PROCESS" }.Localize();
                    string pandoranMeatStorage = new LocalizedTextBind() { LocalizationKey = "KEY_PANDORAN_MEAT_STORAGE" }.Localize();
                    string pandoranMeatProcessingCapacity = new LocalizedTextBind() { LocalizationKey = "KEY_PANDORAN_MEAT_MAX_PROCESS" }.Localize();

                    string tipText = $"{processingPandoranMeat} " +
                        $"\n{pandoranMeatToProcess} {(int)PandasForFoodProcessing}" +
                        $"\n{pandoranMeatStorage} {StorageCapacity * foodProductionFacilitiesCount}" +
                        $"\n{pandoranMeatProcessingCapacity} {ProcessingCapacity * foodProductionFacilitiesCount}";

                    if (tooltipText != null)
                    {
                        tooltipText.TipKey = new LocalizedTextBind(tipText, true);
                    }
                }

                UIAnimatedResourceController mutagenController = uIModuleInfoBar.MutagensController;

                int mutationLabsCount = CountMutationLabs(controller.PhoenixFaction);
                int incomeFromLabs = Mathf.RoundToInt(GetMutagenFacilityOutputPerDay(controller.PhoenixFaction));
                int incomeFromCapturedPandas = Mathf.RoundToInt(GetProcessedMutagenOutputPerDay(controller.PhoenixFaction));

                mutagenController.Income = incomeFromLabs + incomeFromCapturedPandas;
                methodDisplayValue.Invoke(mutagenController, null);

                if (mutagenController.Income > 0)
                {
                    string extractingMutagens = new LocalizedTextBind() { LocalizationKey = "KEY_EXTRACTING_MUTAGENS_TFTV" }.Localize();
                    string mutagenFromFacilities = new LocalizedTextBind() { LocalizationKey = "KEY_MUTAGENS_FROM_FACILITIES_TFTV" }.Localize();
                    string mutagenFromPandorans = new LocalizedTextBind() { LocalizationKey = "KEY_MUTAGENS_FROM_PANDORANS_TFTV" }.Localize();
                    string maxExtractionPerDay = new LocalizedTextBind() { LocalizationKey = "KEY_MAX_EXTRACTION_PER_DAY_TFTV" }.Localize();

                    string tipText = $"{extractingMutagens}.\n{mutagenFromFacilities} {incomeFromLabs}\n{mutagenFromPandorans} {incomeFromCapturedPandas}\n{maxExtractionPerDay} {50 * mutationLabsCount}";
                    UITooltipText tooltipText = uIModuleInfoBar.GetComponentsInChildren<UITooltipText>()
                        .FirstOrDefault(utt =>
                            utt.TipKey != null
                            && utt.TipKey.LocalizationKey != null
                            && (utt.TipKey.LocalizationKey.Equals("KEY_MUTAGENS_RESOURCE_TT") || utt.TipKey.LocalizeEnglish().Contains("Extracting mutagens")));

                    if (tooltipText != null)
                    {
                        tooltipText.TipKey = new LocalizedTextBind(tipText, true);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(UIAnimatedResourceController), nameof(UIAnimatedResourceController.RefreshResourceText))]
        public static class UIAnimatedResourceController_RefreshResourceText_CapturePandorans_Patch
        {
            public static void Postfix(UIAnimatedResourceController __instance)
            {
                try
                {
                    if (ShouldUseExplicitFoodAndMutagenGeneration())
                    {
                        RefreshFoodAndMutagenProductionTooltupUI();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static string LimitedCapturingFoodHarvesting(UIStateRosterAliens uIStateRosterAliens)
        {
            try
            {

                UIModuleActorCycle actorCycleModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                int amount = phoenixFaction.GetHarvestingUnitResourceAmount(actorCycleModule.GetCurrent<GeoUnitDescriptor>(), ResourceType.Supplies);

                int capacity = CountFoodProductionFacilities(phoenixFaction) * StorageCapacity;

                if (PandasForFoodProcessing + amount > capacity)
                {
                    string warningText = new LocalizedTextBind() { LocalizationKey = "KEY_FOOD_PROCESSING_WARNING_TFTV" }.Localize();

                    string warning = $"{warningText} {PandasForFoodProcessing + amount - capacity}";

                    return warning;
                }

                return "";
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        internal static void UpdateResourceInfo(UIStateRosterAliens uIStateRosterAliens)
        {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
                    {

                        // TFTVLogger.Always($"running EnterState RosterAliens");
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        EditUnitButtonsController editUnitButtonsController = controller.View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;
                        GameObject buttonContainer = editUnitButtonsController.AlienContainmentButtonContainer;

                        buttonContainer.GetComponentInChildren<Transform>().Find("Alien_Harvest_Mutagen").gameObject.SetActive(false);

                        editUnitButtonsController.DismantleForMutagens.gameObject.SetActive(false);
                        editUnitButtonsController.DismantleForMutagensResourcesText.gameObject.SetActive(false);


                        if (CountFoodProductionFacilities(controller.PhoenixFaction) == 0 || PandasForFoodProcessing == StorageCapacity * CountFoodProductionFacilities(controller.PhoenixFaction))
                        {
                            editUnitButtonsController.DismantleForFood.gameObject.SetActive(false);
                            editUnitButtonsController.DismantleForFoodResourcesText.gameObject.SetActive(false);
                            buttonContainer.GetComponentInChildren<Transform>().Find("Alien_Harvest_Food").gameObject.SetActive(false);
                        }
                        else
                        {
                            if (controller.PhoenixFaction.HarvestAliensForSuppliesUnlocked)
                            {

                                editUnitButtonsController.DismantleForFood.gameObject.SetActive(true);
                                editUnitButtonsController.DismantleForFoodResourcesText.gameObject.SetActive(true);
                                buttonContainer.GetComponentInChildren<Transform>().Find("Alien_Harvest_Food").gameObject.SetActive(true);
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        

        public static float PandasForFoodProcessing = 0;

        public static void LimitedHarvestingHourlyActions(GeoLevelController controller)
        {
            try
            {
                if (!ShouldUseExplicitFoodAndMutagenGeneration())
                {
                    return;
                }

                GiveFood();
                GiveMutagens();

                if (ShouldUseHarvestProcessing())
                {
                    TriggerFoodPoisoning(controller);
                }

                RefreshFoodAndMutagenProductionTooltupUI();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void GiveFood()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                float facilityFoodPerHour = GetFoodFacilityOutputPerDay(phoenixFaction) / 24f;
                float processedFoodPerHour = GetProcessedFoodOutputPerDay(phoenixFaction) / 24f;
                float totalFoodPerHour = facilityFoodPerHour + processedFoodPerHour;

                if (totalFoodPerHour <= 0f)
                {
                    return;
                }

                phoenixFaction.Wallet.Give(new ResourceUnit(ResourceType.Supplies, totalFoodPerHour), OperationReason.Production);

                if (processedFoodPerHour > 0f)
                {
                    PandasForFoodProcessing = Mathf.Max(0f, PandasForFoodProcessing - processedFoodPerHour);
                    OnProductionUpdate(processedFoodPerHour);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void GiveMutagens()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                float totalMutagensPerHour = GetTotalMutagenOutputPerDay(phoenixFaction) / 24f;
                if (totalMutagensPerHour <= 0f)
                {
                    return;
                }

                phoenixFaction.Wallet.Give(new ResourceUnit(ResourceType.Mutagen, totalMutagensPerHour), OperationReason.Production);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AddToMeatProcessingBank(GeoPhoenixFaction geoPhoenixFaction, int amount)
        {
            try
            {
                int capacity = CountFoodProductionFacilities(geoPhoenixFaction) * 250;

                if (amount + PandasForFoodProcessing > capacity)
                {
                    PandasForFoodProcessing = capacity;

                }
                else
                {
                    PandasForFoodProcessing += amount;

                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(GeoPhoenixFaction), "FinishHarvestingUnit")] //VERIFIED
        public static class GeoPhoenixFaction_FinishHarvestingUnit_CapturePandorans_Patch
        {

            public static bool Prefix(GeoPhoenixFaction __instance, ResourceType returnResource)
            {
                try
                {

                    //TFTVLogger.Always($"running FinishHarvestingUnit");

                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
                    {
                        if (returnResource == ResourceType.Supplies)
                        {
                            GeoUnitDescriptor harvestingUnit = __instance.HarvestingUnit;
                            int harvestingUnitResourceAmount = __instance.GetHarvestingUnitResourceAmount(harvestingUnit, returnResource);

                            AddToMeatProcessingBank(__instance, harvestingUnitResourceAmount);
                            __instance.KillCapturedUnit(harvestingUnit);
                            RefreshFoodAndMutagenProductionTooltupUI();

                            TFTVLogger.Always($"running FinishHarvestingUnit; Pandas for fp now {PandasForFoodProcessing}");

                            return false;

                        }
                        return true;
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

        private static int CountMutationLabs(GeoPhoenixFaction phoenixFaction)
        {
            try
            {
                ResourceGeneratorFacilityComponentDef mutagenGeneratorDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [MutationLab_PhoenixFacilityDef]");

                int count = 0;

                foreach (GeoPhoenixBase bases in phoenixFaction.Bases)
                {
                    foreach (GeoPhoenixFacility facility in bases.Layout.Facilities)
                    {
                        if (facility.GetComponent<ResourceGeneratorFacilityComponent>() is ResourceGeneratorFacilityComponent resourceGenerator
                            && resourceGenerator.ComponentDef == mutagenGeneratorDef && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
                        {
                            count += 1;

                        }
                    }
                }

                return count;


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int CountFoodProductionFacilities(GeoPhoenixFaction phoenixFaction)
        {
            try
            {
                ResourceGeneratorFacilityComponentDef foodGeneratorDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [FoodProduction_PhoenixFacilityDef]");

                int count = 0;

                foreach (GeoPhoenixBase bases in phoenixFaction.Bases)
                {
                    foreach (GeoPhoenixFacility facility in bases.Layout.Facilities)
                    {
                        if (facility.GetComponent<ResourceGeneratorFacilityComponent>() is ResourceGeneratorFacilityComponent resourceGenerator
                            && resourceGenerator.ComponentDef == foodGeneratorDef && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
                        {
                            count += 1;

                        }
                    }
                }

                return count;


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetFoodFacilityOutputPerDay(GeoPhoenixFaction pxFaction)
        {
            try
            {
                if (pxFaction == null)
                {
                    return 0f;
                }

                PhoenixFacilityDef foodProductionFacility = DefCache.GetDef<PhoenixFacilityDef>("FoodProduction_PhoenixFacilityDef");
                ResourceGeneratorFacilityComponentDef foodProductionDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [FoodProduction_PhoenixFacilityDef]");

                int farms = CountFoodProductionFacilities(pxFaction);
                if (farms <= 0)
                {
                    return 0f;
                }

                float baseHourlyOutput = foodProductionDef.BaseResourcesOutput.ByResourceType(ResourceType.Supplies).Value;
              //  TFTVLogger.Always($"base hourly output per food production facility: {baseHourlyOutput}");
                float buffedHourlyOutputPerFacility = pxFaction.FacilityBuffs.GetValue(foodProductionFacility, foodProductionDef, baseHourlyOutput);

               
                
               // TFTVLogger.Always($"buffed hourly output per food production facility: {buffedHourlyOutputPerFacility}");
                return buffedHourlyOutputPerFacility * 24f * farms;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetProcessedFoodOutputPerDay(GeoPhoenixFaction pxFaction)
        {
            try
            {
                if (!ShouldUseHarvestProcessing() || pxFaction == null || !pxFaction.HarvestAliensForSuppliesUnlocked)
                {
                    return 0f;
                }

                float suppliesPerDay = PandasForFoodProcessing;
                int farms = CountFoodProductionFacilities(pxFaction);
                int maxSuppliesPerDay = ProcessingCapacity * farms;

                if (suppliesPerDay > maxSuppliesPerDay)
                {
                    suppliesPerDay = maxSuppliesPerDay;
                }

                return suppliesPerDay;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetMutagenFacilityOutputPerDay(GeoPhoenixFaction pxFaction)
        {
            try
            {
                if (pxFaction == null)
                {
                    return 0f;
                }

                PhoenixFacilityDef mutationLabFacility = DefCache.GetDef<PhoenixFacilityDef>("MutationLab_PhoenixFacilityDef");
                ResourceGeneratorFacilityComponentDef mutagenProductionDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [MutationLab_PhoenixFacilityDef]");

                int labs = CountMutationLabs(pxFaction);
                if (labs <= 0)
                {
                    return 0f;
                }

                float baseHourlyOutput = mutagenProductionDef.BaseResourcesOutput.ByResourceType(ResourceType.Mutagen).Value;
                float buffedHourlyOutputPerFacility = pxFaction.FacilityBuffs.GetValue(mutationLabFacility, mutagenProductionDef, baseHourlyOutput);

                return buffedHourlyOutputPerFacility * 24f * labs;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetProcessedMutagenOutputPerDay(GeoPhoenixFaction pxFaction)
        {
            try
            {
                if (!ShouldUseHarvestProcessing() || pxFaction == null || !pxFaction.HarvestAliensForMutagensUnlocked)
                {
                    return 0f;
                }

                List<GeoUnitDescriptor> capturedUnits = pxFaction.CapturedUnits.ToList();

                float mutagensPerDay = 0f;
                int mutationLabs = CountMutationLabs(pxFaction);
                int maxMutagensPerDay = ProcessingCapacity * mutationLabs;
                int divisor = 10;

                foreach (GeoUnitDescriptor geoUnitDescriptor in capturedUnits)
                {
                    mutagensPerDay += (float)pxFaction.GetHarvestingUnitResourceAmount(geoUnitDescriptor, ResourceType.Mutagen) / divisor;
                }

                if (mutagensPerDay > maxMutagensPerDay)
                {
                    mutagensPerDay = maxMutagensPerDay;
                }

                return maxMutagensPerDay > 0 ? mutagensPerDay : 0f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static float GetTotalMutagenOutputPerDay(GeoPhoenixFaction pxFaction)
        {
            try
            {
                return GetMutagenFacilityOutputPerDay(pxFaction) + GetProcessedMutagenOutputPerDay(pxFaction);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static float GetMutLabOutputPerDay()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                return GetProcessedMutagenOutputPerDay(controller.PhoenixFaction);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

    }
}
