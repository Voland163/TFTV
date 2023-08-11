using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
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

                    TFTVLogger.Always($"rolling for poisoning! chance {PoisoningChance}, roll {ProbabilityRoll}");

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

                ResourceGeneratorFacilityComponentDef foodProductionDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [FoodProduction_PhoenixFacilityDef]");

                UIAnimatedResourceController foodController = uIModuleInfoBar.FoodController;

                MethodInfo methodDisplayValue = typeof(UIAnimatedResourceController).GetMethod("DisplayValue", BindingFlags.NonPublic | BindingFlags.Instance);

                int foodProductionFacilitiesCount = CountFoodProductionFacilities(controller.PhoenixFaction);

                foodController.Income = (int)(foodProductionDef.BaseResourcesOutput[0].Value * 24 * foodProductionFacilitiesCount) + Mathf.Min((int)GetFarmOutputPerDay(), foodProductionFacilitiesCount * ProcessingCapacity) - controller.PhoenixFaction.Soldiers.Count();

                methodDisplayValue.Invoke(foodController, null);

                if (PandasForFoodProcessing > 0)
                {
                    UITooltipText tooltipText = uIModuleInfoBar.GetComponentsInChildren<UITooltipText>().FirstOrDefault(utt => utt.TipKey.LocalizeEnglish().Contains("FOOD") || utt.TipKey.LocalizeEnglish().Contains("Pandoran meat"));

                    string tipText = $"<b>Processing Pandoran meat</b>.\n-Meat to process: {(int)PandasForFoodProcessing}" +
                        $"\n-Max storage: {StorageCapacity * foodProductionFacilitiesCount}" +
                           $"\n-Max processing per day: {ProcessingCapacity * foodProductionFacilitiesCount}";

                    // TFTVLogger.Always($"{tipText}");

                    if (tooltipText != null)
                    {
                        tooltipText.TipKey = new LocalizedTextBind(tipText, true);
                        //  TFTVLogger.Always($"got here, {tipText}");
                    }

                }

                UIAnimatedResourceController mutagenController = uIModuleInfoBar.MutagensController;

                ResourceGeneratorFacilityComponentDef mutagenProductionDef = DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [MutationLab_PhoenixFacilityDef]");

                int mutationLabsCount = CountMutationLabs(controller.PhoenixFaction);

                int incomeFromLabs = (int)(mutagenProductionDef.BaseResourcesOutput[0].Value * 24 * mutationLabsCount);
                int incomeFromCapturedPandas = Mathf.Min((int)GetMutLabOutputPerDay(), mutationLabsCount * ProcessingCapacity);

                mutagenController.Income = incomeFromLabs + incomeFromCapturedPandas;
                methodDisplayValue.Invoke(mutagenController, null);
                if (mutagenController.Income > 0)
                {
                    string tipText = $"<b>Extracting mutagens from captured Pandorans</b>.\n-Mutagens from facilities: {incomeFromLabs}\n-Mutagens from Pandorans: {incomeFromCapturedPandas}\n-Max extraction per day: {50 * mutationLabsCount}";
                    UITooltipText tooltipText = uIModuleInfoBar.GetComponentsInChildren<UITooltipText>().FirstOrDefault(utt => utt.TipKey.LocalizationKey.Equals("KEY_MUTAGENS_RESOURCE_TT") || utt.TipKey.LocalizeEnglish().Contains("Extracting mutagens"));

                    //    TFTVLogger.Always($"{tipText}");

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


        [HarmonyPatch(typeof(UIAnimatedResourceController), "RefreshResourceText")]
        public static class UIAnimatedResourceController_RefreshResourceText_CapturePandorans_Patch
        {

            public static void Postfix(UIAnimatedResourceController __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
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



        [HarmonyPatch(typeof(UIStateRosterAliens), "OnDismantleForFood")]
        public static class UIStateRosterAliens_OnDismantleForFood_CapturePandorans_Patch
        {
            private static bool OnDismantleRun = false;

            public static bool Prefix(UIStateRosterAliens __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
                    {
                        UIModuleActorCycle actorCycleModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        int amount = phoenixFaction.GetHarvestingUnitResourceAmount(actorCycleModule.GetCurrent<GeoUnitDescriptor>(), ResourceType.Supplies);

                        int capacity = CountFoodProductionFacilities(phoenixFaction) * StorageCapacity;

                        if (PandasForFoodProcessing + amount > capacity && !OnDismantleRun)
                        {
                            string warning = $"Food processing facilities storage capacity almost reached! Rendering this Pandoran for food will waste {PandasForFoodProcessing + amount - capacity} units";

                            /*   MethodInfo methodInfo = typeof(UIStateRosterAliens).GetMethod("OnDismantpleForFoodDialogCallback", BindingFlags.Instance | BindingFlags.NonPublic);

                               MessageBoxCallbackResult msgResult = new MessageBoxCallbackResult {};
                               object[] parameters = new object[] { msgResult };*/

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                            OnDismantleRun = true;

                            return false;

                        }
                        OnDismantleRun = false;
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



        [HarmonyPatch(typeof(UIStateRosterAliens), "EnterState")]
        public static class UIStateRosterAliens_UpdateResourceInfo_CapturePandorans_Patch
        {

            public static void Postfix(UIStateRosterAliens __instance)
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
                            editUnitButtonsController.DismantleForFood.gameObject.SetActive(true);
                            editUnitButtonsController.DismantleForFoodResourcesText.gameObject.SetActive(true);
                            buttonContainer.GetComponentInChildren<Transform>().Find("Alien_Harvest_Food").gameObject.SetActive(true);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static float PandasForFoodProcessing = 0;

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionHourly")]
        public static class GeoAlienFaction_UpdateFactionHourly_CapturePandorans_Patch
        {

            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.LimitedHarvestingSetting)
                    {

                        GiveFood();
                        TriggerFoodPoisoning(__instance.GeoLevel);
                        GiveMutagens();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        private static void GiveMutagens()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                UIModuleInfoBar uIModuleInfoBar = controller.View.GeoscapeModules.ResourcesModule;

                ResourceUnit mutagens = new ResourceUnit { Type = ResourceType.Mutagen, Value = GetMutLabOutputPerDay() / 24 };

                controller.PhoenixFaction.Wallet.Give(mutagens, OperationReason.Production);

                //   TFTVLogger.Always($"mutagen income shown should be {uIModuleInfoBar.MutagensController.Income} + {mutagens.Value}");

                uIModuleInfoBar.MutagensController.RefreshResourceText(uIModuleInfoBar.MutagensController.Value, uIModuleInfoBar.MutagensController.Income + mutagens.Value, true);

                // TFTVLogger.Always($"giving mutagens {mutagens.Value}");

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

                UIModuleInfoBar uIModuleInfoBar = controller.View.GeoscapeModules.ResourcesModule;

                ResourceUnit supplies = new ResourceUnit { Type = ResourceType.Supplies, Value = GetFarmOutputPerDay() / 24 };


                controller.PhoenixFaction.Wallet.Give(supplies, OperationReason.Production);

                //  TFTVLogger.Always($"supplies income shown should be {uIModuleInfoBar.FoodController.Income} + {supplies.Value}");

                uIModuleInfoBar.FoodController.RefreshResourceText(uIModuleInfoBar.FoodController.Value, uIModuleInfoBar.FoodController.Income + supplies.Value, false);

                PandasForFoodProcessing -= supplies.Value;

                OnProductionUpdate(supplies.Value);


                //  TFTVLogger.Always($"giving supplies {supplies.Value}, reducing PandasForFoodProcessing to {PandasForFoodProcessing}");

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
                int capacity = CountFoodProductionFacilities(geoPhoenixFaction) * 100;

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

        [HarmonyPatch(typeof(GeoPhoenixFaction), "FinishHarvestingUnit")]
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
                            GeoFaction geoFaction = __instance;
                            //   GiveFood();
                            __instance.KillCapturedUnit(harvestingUnit);
                            UIModuleInfoBar uIModuleInfoBar = __instance.GeoLevel.View.GeoscapeModules.ResourcesModule;
                            uIModuleInfoBar.FoodController.RefreshResourceText(uIModuleInfoBar.FoodController.Value, uIModuleInfoBar.FoodController.Income + GetFarmOutputPerDay() * 24, false);


                            //  TFTVLogger.Always($"mutagen income shown should be {uIModuleInfoBar.MutagensController.Income} + {mutagens.Value}");

                            uIModuleInfoBar.MutagensController.RefreshResourceText(uIModuleInfoBar.MutagensController.Value, uIModuleInfoBar.MutagensController.Income + GetMutLabOutputPerDay() * 24, true);


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

        public static float GetFarmOutputPerDay()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                GeoPhoenixFaction pxFaction = controller.PhoenixFaction;
                if (pxFaction.HarvestAliensForSuppliesUnlocked)

                {

                    float suppliesPerDay = PandasForFoodProcessing;
                    int farms = CountFoodProductionFacilities(pxFaction);
                    int maxSuppliesPerDay = ProcessingCapacity * farms;

                    if (suppliesPerDay > maxSuppliesPerDay)
                    {
                        suppliesPerDay = maxSuppliesPerDay;

                    }

                    return suppliesPerDay;
                }

                return 0;

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
                GeoPhoenixFaction pxFaction = controller.PhoenixFaction;
                if (pxFaction.HarvestAliensForMutagensUnlocked)

                {
                    List<GeoUnitDescriptor> capturedUnits = pxFaction.CapturedUnits.ToList();

                    float mutagensPerDay = 0;
                    int mutationLabs = CountMutationLabs(pxFaction);
                    int maxMutagensPerDay = ProcessingCapacity * mutationLabs;

                    int divisor = 10;

                    foreach (GeoUnitDescriptor geoUnitDescriptor in capturedUnits)
                    {

                        mutagensPerDay += (float)pxFaction.GetHarvestingUnitResourceAmount(geoUnitDescriptor, ResourceType.Mutagen) / divisor;
                        /*   TFTVLogger.Always($"{geoUnitDescriptor?.GetName()} resource amount {(float)pxFaction.GetHarvestingUnitResourceAmount(geoUnitDescriptor, ResourceType.Mutagen)}, " +
                               $"divided by 10 = {(float)pxFaction.GetHarvestingUnitResourceAmount(geoUnitDescriptor, ResourceType.Mutagen) / divisor}" +
                               $"so mutagens per day {mutagensPerDay}");*/
                    }

                    if (mutagensPerDay > maxMutagensPerDay)
                    {
                        mutagensPerDay = maxMutagensPerDay;

                    }

                    if (maxMutagensPerDay > 0)
                    {
                        //   TFTVLogger.Always($"so mutagens per day {mutagensPerDay}");
                        return mutagensPerDay;

                    }


                }
                return 0;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

    }
}
