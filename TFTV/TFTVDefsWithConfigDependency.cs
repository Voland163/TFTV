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
using PhoenixPoint.Geoscape.Entities.Research.Reward;
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

        public static bool ChangesToCapturingPandoransImplemented = false;
        public static bool ChangesToFoodAndMutagenGenerationImplemented = false;   

        public static void ImplementConfigChoices()
        {
            try
            {
                ChangesToFoodAndMutagenGeneration();
                ChangesToPandoranCapture();
                EqualizeTrade();
                IncreaseHavenAlertCoolDown();
                TFTVBetterEnemies.ImplementBetterEnemies();
             //   ReverseEngineering();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

  /*      private static void ReverseEngineering()
        {
            try
            {

                TFTVConfig config = TFTVMain.Main.Config;

                if (config.ActivateReverseEngineeringResearch)
                {
                    TFTVReverseEngineering.ModifyReverseEngineering();
                    TFTVLogger.Always("Reverse Engineering changes to Defs injected");
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }*/

        private static void ChangeResourceRewardsForAutopsies()
        {
            try
            {
                DefCache.GetDef<ResearchDef>("PX_Alien_Mindfragger_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 75},
                    new ResourceUnit {Type = ResourceType.Mutagen, Value = 50}
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Poisonworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Fireworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };

                DefCache.GetDef<ResearchDef>("PX_AlienCrabman_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 50 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Fishman_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 75 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 100 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Siren_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 125 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Chiron_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 200 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 150 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Queen_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 300 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 250 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Swarmer_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 50 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_WormEgg_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_MindfraggerEgg_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_HatchingSentinel_ResearchDef").Resources = new ResourcePack()
                { 
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef").Resources = new ResourcePack()
                {
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_MistSentinel_ResearchDef").Resources = new ResourcePack()
                {
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };

                

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
                GeoHavenDef geoHavenDef = DefCache.GetDef<GeoHavenDef>("GeoHavenDef");

                if (config.LimitedRaiding)
                {

                    geoHavenDef.AlertStateCooldownDays = 7;
                   
                   
                }
                else 
                {
                    geoHavenDef.AlertStateCooldownDays = 3;
                
                
                }


                TFTVLogger.Always($"IncreaseHavenAlertCooldown set to {geoHavenDef.AlertStateCooldownDays}");

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

                GeoFactionDef anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                GeoFactionDef nj = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                GeoFactionDef syn = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");


                if (config.EqualizeTrade)
                {
                    List<TradingRatio> tradingRatios = new List<TradingRatio>
                    {
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 5, RecieveQuantity = 1, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 1, RecieveQuantity = 1, RecieveResource = ResourceType.Materials },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 1, RecieveQuantity = 1, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 5, RecieveQuantity = 1, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 1, RecieveQuantity = 5, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 1, RecieveQuantity = 5, RecieveResource = ResourceType.Materials }
                    };

                    anu.ResourceTradingRatios = tradingRatios;
                    nj.ResourceTradingRatios = tradingRatios;
                    syn.ResourceTradingRatios = tradingRatios;
            
                }
                else 
                {

                    List<TradingRatio> tradingRatiosAnu = new List<TradingRatio>
                    {
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 12, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 6, RecieveQuantity = 4, RecieveResource = ResourceType.Materials },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 6, RecieveQuantity = 4, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 10, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 10, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 9, RecieveResource = ResourceType.Materials }
                    };

                    List<TradingRatio> tradingRatiosNJ = new List<TradingRatio>
                    {
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 10, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 6, RecieveQuantity = 4, RecieveResource = ResourceType.Materials },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 6, RecieveQuantity = 4, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 12, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 9, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 10, RecieveResource = ResourceType.Materials }
                    };

                    List<TradingRatio> tradingRatiosSyn = new List<TradingRatio>
                    {
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 9, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Supplies, OfferQuantity = 5, RecieveQuantity = 4, RecieveResource = ResourceType.Materials },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 5, RecieveQuantity = 4, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Materials, OfferQuantity = 9, RecieveQuantity = 2, RecieveResource = ResourceType.Tech },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 8, RecieveResource = ResourceType.Supplies },
                        new TradingRatio() { OfferResource = ResourceType.Tech, OfferQuantity = 2, RecieveQuantity = 8, RecieveResource = ResourceType.Materials }
                    };              

                }

                TFTVLogger.Always($"EqualizeTrade Implemented? {config.EqualizeTrade}");

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

                if (!ChangesToFoodAndMutagenGenerationImplemented && TFTVNewGameOptions.LimitedHarvestingSetting)
                {
                    ModifyLocalizationKeys();
                    RemoveMutagenHarvestingResearch();
                    ChangeResourceRewardsForAutopsies();
                    ChangesToFoodAndMutagenGenerationImplemented = true;
                    TFTVLogger.Always($"ChangesToFoodAndMutagenGenerationImplemented");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void RemoveMutagenHarvestingResearch()
        {
            try
            {

                ResearchDef mutagenHarvesting = DefCache.GetDef<ResearchDef>("PX_MutagenHarvesting_ResearchDef");
                ExistingResearchRequirementDef researchRequirementDef = DefCache.GetDef<ExistingResearchRequirementDef>("PX_MutagenHarvesting_ResearchDef_ExistingResearchRequirementDef_2");

                DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(mutagenHarvesting);

                ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                List<ResearchRewardDef> mutationUnlocks = new List<ResearchRewardDef>(mutationTech.Unlocks)
                {
                    mutagenHarvesting.Unlocks[0]
                };

                mutationTech.Unlocks = mutationUnlocks.ToArray();



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

                if (!ChangesToCapturingPandoransImplemented && TFTVNewGameOptions.LimitedCaptureSetting)
                {
                    CreateCaptureModule();
                    ChangesToCapturingPandorans();
                    AdjustPandoranVolumes();
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
                DefCache.GetDef<ResearchViewElementDef>("ANU_MutationTech_ViewElementDef").CompleteText.LocalizationKey = "TFTV_MUTATIONTECH_RESEARCHDEF_COMPLETE_W_HARVESTING";
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
