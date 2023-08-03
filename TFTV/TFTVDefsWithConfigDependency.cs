using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using static TFTV.TFTVCapturePandorans;

namespace TFTV
{
    internal class TFTVDefsWithConfigDependency
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static readonly ResearchTagDef CriticalResearchTag = DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef");

        private static bool ChangesToCapturingPandoransImplemented = false;
        private static bool ChangesToFoodAndMutagenGenerationImplemented = false;
        private static bool EqualizeTradeImplemented = false;
        private static bool IncreaseHavenAlertCooldownImplemented = false;

        public static void ImplementConfigChoices()
        {
            try
            {
                ChangesToFoodAndMutagenGeneration();
                ChangesToPandoranCapture();
                EqualizeTrade();
                IncreaseHavenAlertCoolDown();
                TFTVBetterEnemies.ImplementBetterEnemies();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

        private static void IncreaseHavenAlertCoolDown()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!IncreaseHavenAlertCooldownImplemented && config.LimitedRaiding)
                {

                    DefCache.GetDef<GeoHavenDef>("GeoHavenDef").AlertStateCooldownDays = 7;

                    IncreaseHavenAlertCooldownImplemented = true;
                    TFTVLogger.Always($"IncreaseHavenAlertCooldownImplemented");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        private static void EqualizeTrade()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!EqualizeTradeImplemented && config.EqualizeTrade)
                {

                    GeoFactionDef anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                    GeoFactionDef nj = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                    GeoFactionDef syn = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                    List<TradingRatio> tradingRatios = new List<TradingRatio>();
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 5, RecieveQuantity = 1, RecieveResource = ResourceType.Tech });
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 1, RecieveQuantity = 1, RecieveResource = ResourceType.Materials });
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 1, RecieveQuantity = 1, RecieveResource = ResourceType.Supplies });
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 5, RecieveQuantity = 1, RecieveResource = ResourceType.Tech });
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 1, RecieveQuantity = 5, RecieveResource = ResourceType.Supplies });
                    tradingRatios.Add(new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 1, RecieveQuantity = 5, RecieveResource = ResourceType.Materials });

                    anu.ResourceTradingRatios = tradingRatios;
                    nj.ResourceTradingRatios = tradingRatios;
                    syn.ResourceTradingRatios = tradingRatios;

                    EqualizeTradeImplemented = true;
                    TFTVLogger.Always($"EqualizeTradeImplemented");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }



        private static void ChangesToFoodAndMutagenGeneration()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!ChangesToFoodAndMutagenGenerationImplemented && config.LimitedHarvesting)
                {
                    ModifyLocalizationKeys();

                    ChangesToFoodAndMutagenGenerationImplemented = true;
                    TFTVLogger.Always($"ChangesToFoodAndMutagenGenerationImplemented");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void ChangesToPandoranCapture()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!ChangesToCapturingPandoransImplemented && config.LimitedCapture)
                {
                    CreateCaptureModule();
                    ChangesToCapturingPandorans();
                    AdjustPandoranVolumes();
                    CreateObjectiveCaptureCapacity();

                    ChangesToCapturingPandoransImplemented = true;
                    TFTVLogger.Always($"ChangesToCapturingPandoransImplemented");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ModifyLocalizationKeys()
        {
            try
            {
                DefCache.GetDef<ViewElementDef>("E_ViewElement [FoodProduction_PhoenixFacilityDef]").Description.LocalizationKey = "KEY_BASE_FACILITY_FOOD_PRODUCTION_DESCRIPTION_TFTV";
                DefCache.GetDef<ViewElementDef>("E_ViewElement [MutationLab_PhoenixFacilityDef]").Description.LocalizationKey = "KEY_BASE_FACILITY_MUTATION_LAB_DESCRIPTION_TFTV";
                DefCache.GetDef<ResearchViewElementDef>("PX_MutagenHarvesting_ViewElementDef").CompleteText.LocalizationKey = "PX_MUTAGENHARVESTING_RESEARCHDEF_COMPLETE_TFTV";
                DefCache.GetDef<ResearchViewElementDef>("PX_FoodHavresting_ViewElementDef").BenefitsText.LocalizationKey = "PX_FOODHAVRESTING_RESEARCHDEF_BENEFITS_TFTV";
                DefCache.GetDef<ResearchViewElementDef>("PX_CaptureTech_ViewElementDef").CompleteText.LocalizationKey = "PX_CAPTURETECH_RESEARCHDEF_COMPLETE_TFTV";
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void AdjustPandoranVolumes()
        {
            try
            {
                ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
                ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                ClassTagDef wormTag = DefCache.GetDef<ClassTagDef>("Worm_ClassTagDef");
                ClassTagDef facehuggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");
                ClassTagDef swarmerTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");


                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>().Where(tcd => tcd.IsAlien))
                {
                    if (tacCharacterDef.Data.GameTags.Contains(swarmerTag) || tacCharacterDef.Data.GameTags.Contains(facehuggerTag) || tacCharacterDef.Data.GameTags.Contains(wormTag))
                    {

                        tacCharacterDef.Volume = 1;

                    }
                    else if (tacCharacterDef.Data.GameTags.Contains(sirenTag))
                    {

                        tacCharacterDef.Volume = 3;

                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateObjectiveCaptureCapacity()
        {
            try
            {
                TFTVCommonMethods.CreateObjectiveReminder("{25590AE4-872B-4679-A15C-300C3DC48A53}", "CAPTURE_CAPACITY_AIRCRAFT", 0);
                TFTVCommonMethods.CreateObjectiveReminder("{4EB4A290-8FE7-45CC-BF8B-914C52441EF4}", "CAPTURE_CAPACITY_BASE", 0);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateCaptureModule()
        {
            try
            {

                ResearchDef scyllaCaptureModule = DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef");
                ExistingResearchRequirementDef existingResearchRequirementDef = DefCache.GetDef<ExistingResearchRequirementDef>("PX_Aircraft_EscapePods_ResearchDef_ExistingResearchRequirementDef_1");
                existingResearchRequirementDef.ResearchID = "PX_Alien_Queen_ResearchDef";

                scyllaCaptureModule.Tags = new ResearchTagDef[] { CriticalResearchTag };
                scyllaCaptureModule.RevealRequirements.Container =
                    new ReseachRequirementDefOpContainer[] { new ReseachRequirementDefOpContainer()
                    { Operation = ResearchContainerOperation.ANY, Requirements = new ResearchRequirementDef[] { existingResearchRequirementDef } } };
                scyllaCaptureModule.ResearchCost = 500;

                GeoVehicleModuleDef captureModule = DefCache.GetDef<GeoVehicleModuleDef>("PX_EscapePods_GeoVehicleModuleDef");
                captureModule.ManufactureMaterials = 600;
                captureModule.ManufactureTech = 75;
                captureModule.ManufacturePointsCost = 505;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangesToCapturingPandorans()
        {
            try
            {
                DefCache.GetDef<PrisonFacilityComponentDef>("E_Prison [AlienContainment_PhoenixFacilityDef]").ContaimentCapacity = 25;


                string captureAbilityName = "CapturePandoran_Ability";
                ApplyStatusAbilityDef applyStatusAbilitySource = DefCache.GetDef<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef");
                ApplyStatusAbilityDef newCaptureAbility = Helper.CreateDefFromClone(applyStatusAbilitySource, "{8850B4B0-5545-4FCE-852A-E56AFA19DED6}", captureAbilityName);

                string removeCaptureAbilityName = "RemoveCapturePandoran_Ability";
                ApplyStatusAbilityDef removeCaptureStatusAbility = Helper.CreateDefFromClone(applyStatusAbilitySource, "{1D24098D-5C9A-4698-8062-5BAF974ADE35}", removeCaptureAbilityName);
                removeCaptureStatusAbility.ViewElementDef = Helper.CreateDefFromClone(applyStatusAbilitySource.ViewElementDef, "{19FF369F-868B-4DFA-90AE-E72D4075B868}", removeCaptureAbilityName);
                removeCaptureStatusAbility.ViewElementDef.DisplayName1.LocalizationKey = "CANCEL_CAPTURE_NAME";
                removeCaptureStatusAbility.ViewElementDef.Description.LocalizationKey = "CANCEL_CAPTURE_DESCRIPTION";

                newCaptureAbility.ViewElementDef = Helper.CreateDefFromClone(applyStatusAbilitySource.ViewElementDef, "{C740EF09-6068-4ADB-9E38-7F6F504ACC07}", captureAbilityName);
                newCaptureAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ability_capture.png");
                newCaptureAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ability_capture_small.png");
                newCaptureAbility.ViewElementDef.DisplayName1.LocalizationKey = "CAPTURE_ABILITY_NAME";
                newCaptureAbility.ViewElementDef.Description.LocalizationKey = "CAPTURE_ABILITY_DESCRIPTION";

                newCaptureAbility.TargetingDataDef = Helper.CreateDefFromClone(applyStatusAbilitySource.TargetingDataDef, "{AB7A060C-2CB1-4DD6-A21F-A018BC8B0600}", captureAbilityName);
                newCaptureAbility.WillPointCost = 0;

                string captureStatusName = "CapturePandoran_Status";
                ParalysedStatusDef paralysedStatusDef = DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef");
                ReadyForCapturesStatusDef newCapturedStatus = Helper.CreateDefFromClone<ReadyForCapturesStatusDef>(null, "{96B40C5A-7FF2-4C67-83DA-ACEF0BE7D2E8}", captureStatusName);
                newCapturedStatus.EffectName = "ReadyForCapture";
                newCapturedStatus.Duration = paralysedStatusDef.Duration;
                newCapturedStatus.DurationTurns = -1;
                newCapturedStatus.ExpireOnEndOfTurn = false;
                newCapturedStatus.ApplicationConditions = paralysedStatusDef.ApplicationConditions;
                newCapturedStatus.DisablesActor = true;
                newCapturedStatus.SingleInstance = false;
                newCapturedStatus.ShowNotification = true;
                newCapturedStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newCapturedStatus.VisibleOnPassiveBar = true;
                newCapturedStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newCapturedStatus.HealthbarPriority = 300;
                newCapturedStatus.StackMultipleStatusesAsSingleIcon = true;
                newCapturedStatus.Visuals = Helper.CreateDefFromClone(paralysedStatusDef.Visuals, "{4305BE38-4408-4565-A440-A989C07467A0}", captureStatusName);
                newCapturedStatus.EventOnApply = paralysedStatusDef.EventOnApply;

                newCapturedStatus.Visuals.LargeIcon = newCaptureAbility.ViewElementDef.LargeIcon;
                newCapturedStatus.Visuals.SmallIcon = newCaptureAbility.ViewElementDef.SmallIcon;
                newCapturedStatus.Visuals.DisplayName1.LocalizationKey = "CAPTURE_STATUS_NAME";
                newCapturedStatus.Visuals.Description.LocalizationKey = "CAPTURE_STATUS_DESCRIPTION";

                ActorHasStatusEffectConditionDef actorIsParalyzedEffectCondition = TFTVCommonMethods.CreateNewStatusEffectCondition("{C9422E7A-B17E-4DFE-A2FD-D91311119B3B}", paralysedStatusDef, true);
                ActorHasStatusEffectConditionDef actorIsNotReadyForCaptureEffectCondition = TFTVCommonMethods.CreateNewStatusEffectCondition("{B89D9D5F-436E-47C8-8BF5-853E1721DFCF}", newCapturedStatus, false);

                newCaptureAbility.StatusDef = newCapturedStatus;
                newCaptureAbility.TargetApplicationConditions = new EffectConditionDef[] { actorIsParalyzedEffectCondition, actorIsNotReadyForCaptureEffectCondition };
                newCaptureAbility.UsableOnDisabledActor = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

    }
}
