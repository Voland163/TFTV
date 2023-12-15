using Base;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

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
                TradeAndRaiding.ImplementTradeAndRaidingConfig();
                FoodAndMutagenGeneration.ChangesToFoodAndMutagenGeneration();
                PandoranCapture.ChangesToPandoranCapture();
                StrongerPandorans.ImplementStrongerPandorans();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal class TradeAndRaiding
        {
            internal static void ImplementTradeAndRaidingConfig()
            {
                try
                {
                    EqualizeTrade();
                    IncreaseHavenAlertCoolDown();
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
                        TFTVLogger.Always($"EqualizeTrade is on");
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
                        anu.ResourceTradingRatios = tradingRatiosAnu;
                        nj.ResourceTradingRatios = tradingRatiosNJ;
                        syn.ResourceTradingRatios = tradingRatiosSyn;

                        TFTVLogger.Always($"EqualizeTrade is off");
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class FoodAndMutagenGeneration
        {
            internal static void ChangesToFoodAndMutagenGeneration()
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!ChangesToFoodAndMutagenGenerationImplemented && TFTVNewGameOptions.LimitedHarvestingSetting)
                    {
                        ModifyHarvestingLocalizationKeys();
                        RemoveMutagenHarvestingResearch();
                        MoveFoodProductionFacilityToImmediatelyAvailable();
                        ChangesToFoodAndMutagenGenerationImplemented = true;
                        TFTVLogger.Always($"Limited Harvesting is on");
                        return;
                    }
                    else if (ChangesToFoodAndMutagenGenerationImplemented && !TFTVNewGameOptions.LimitedHarvestingSetting)
                    {
                        RestoreHarvestingLocalizationKeys();
                        RestoreMutagenHarvestingResearch();
                        RemoveFoodProductionFacilityToImmediatelyAvailable();
                        ChangesToFoodAndMutagenGenerationImplemented = false;
                        TFTVLogger.Always($"Limited Harvesting Setting reverted");
                    }
                    TFTVLogger.Always($"Limited Harvesting Setting is off");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ModifyHarvestingLocalizationKeys()
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


            private static void RestoreHarvestingLocalizationKeys()
            {
                try
                {
                    DefCache.GetDef<ViewElementDef>("E_ViewElement [FoodProduction_PhoenixFacilityDef]").Description.LocalizationKey = "KEY_BASE_FACILITY_FOOD_PRODUCTION_DESCRIPTION";
                    DefCache.GetDef<ViewElementDef>("E_ViewElement [MutationLab_PhoenixFacilityDef]").Description.LocalizationKey = "KEY_BASE_FACILITY_MUTATION_LAB_DESCRIPTION";
                    DefCache.GetDef<ResearchViewElementDef>("PX_MutagenHarvesting_ViewElementDef").CompleteText.LocalizationKey = "PX_MUTAGENHARVESTING_RESEARCHDEF_COMPLETE";
                    DefCache.GetDef<ResearchViewElementDef>("PX_FoodHavresting_ViewElementDef").BenefitsText.LocalizationKey = "PX_FOODHAVRESTING_RESEARCHDEF_BENEFITS";
                    DefCache.GetDef<ResearchViewElementDef>("PX_CaptureTech_ViewElementDef").CompleteText.LocalizationKey = "PX_CAPTURETECH_RESEARCHDEF_COMPLETE_TFTV";
                    DefCache.GetDef<ResearchViewElementDef>("ANU_MutationTech_ViewElementDef").CompleteText.LocalizationKey = "TFTV_MUTATIONTECH_RESEARCHDEF_COMPLETE";
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void MoveFoodProductionFacilityToImmediatelyAvailable()
            {
                try
                {
                    GeoPhoenixFactionDef geoPhoenixFactionDef = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");

                    List<PhoenixFacilityDef> phoenixFacilities = geoPhoenixFactionDef.StartingFacilities.ToList();
                    PhoenixFacilityDef foodProductionFacility = DefCache.GetDef<PhoenixFacilityDef>("FoodProduction_PhoenixFacilityDef");

                    phoenixFacilities.Add(foodProductionFacility);
                    geoPhoenixFactionDef.StartingFacilities = phoenixFacilities.ToArray();

                    ResearchDef fungusResearchDef = DefCache.GetDef<ResearchDef>("ANU_AnuFungusFood_ResearchDef");

                    FacilityBuffResearchRewardDef foodProductionBuff = Helper.CreateDefFromClone(DefCache.GetDef<FacilityBuffResearchRewardDef>("NJ_AutomatedFactories_ResearchDef_FacilityBuffResearchRewardDef_0"),
                        "{99DE6ED5-37E0-4CF5-8511-D59A51D0B5F0}",
                        "FoodProductionBuff");

                    foodProductionBuff.Facility = foodProductionFacility;
                    //  foodProductionBuff.Increase = 0.0f;
                    foodProductionBuff.ModificationType = GeoFactionFacilityBuff.FacilityComponentModificationType.Multiply;

                    fungusResearchDef.Unlocks = new ResearchRewardDef[] { fungusResearchDef.Unlocks[1], foodProductionBuff };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void RemoveFoodProductionFacilityToImmediatelyAvailable()
            {
                try
                {
                    GeoPhoenixFactionDef geoPhoenixFactionDef = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");

                    List<PhoenixFacilityDef> phoenixFacilities = geoPhoenixFactionDef.StartingFacilities.ToList();
                    PhoenixFacilityDef foodProductionFacility = DefCache.GetDef<PhoenixFacilityDef>("FoodProduction_PhoenixFacilityDef");

                    phoenixFacilities.Remove(foodProductionFacility);
                    geoPhoenixFactionDef.StartingFacilities = phoenixFacilities.ToArray();

                    ResearchDef fungusResearchDef = DefCache.GetDef<ResearchDef>("ANU_AnuFungusFood_ResearchDef");

                    ;

                    FacilityBuffResearchRewardDef foodProductionBuff = Helper.CreateDefFromClone(DefCache.GetDef<FacilityBuffResearchRewardDef>("NJ_AutomatedFactories_ResearchDef_FacilityBuffResearchRewardDef_0"),
                        "{99DE6ED5-37E0-4CF5-8511-D59A51D0B5F0}",
                        "FoodProductionBuff");

                    foodProductionBuff.Facility = foodProductionFacility;
                    //  foodProductionBuff.Increase = 0.0f;
                    foodProductionBuff.ModificationType = GeoFactionFacilityBuff.FacilityComponentModificationType.Multiply;

                    fungusResearchDef.Unlocks = new ResearchRewardDef[] { DefCache.GetDef<FacilityResearchRewardDef>("ANU_AnuFungusFood_ResearchDef_FacilityResearchRewardDef_0"), fungusResearchDef.Unlocks[1] };


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

                    DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(mutagenHarvesting);

                    ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");

                    List<ResearchRewardDef> mutationUnlocks = new List<ResearchRewardDef>(mutationTech.Unlocks)
                {
                    mutagenHarvesting.Unlocks[0]
                };

                    mutationTech.Unlocks = mutationUnlocks.ToArray();

                  //  TFTVLogger.Always($"PX_MutagenHarvesting_ResearchDef should have been removed; functionality added to MutationTech");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void RestoreMutagenHarvestingResearch()
            {
                try
                {
                    ResearchDef mutagenHarvesting = DefCache.GetDef<ResearchDef>("PX_MutagenHarvesting_ResearchDef");
                    DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Add(mutagenHarvesting);
                    ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");
                    ResearchRewardDef mutagenHarvestingResearchReward = mutagenHarvesting.Unlocks[0];

                    if (mutationTech.Unlocks.Contains(mutagenHarvestingResearchReward))
                    {
                      //  TFTVLogger.Always($"mutation tech contains {mutagenHarvestingResearchReward.name}");

                        List<ResearchRewardDef> mutationUnlocks = new List<ResearchRewardDef>(mutationTech.Unlocks);
                        mutationUnlocks.Remove(mutagenHarvestingResearchReward);
                        mutationTech.Unlocks = mutationUnlocks.ToArray();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class PandoranCapture
        {
            internal static void ChangesToPandoranCapture()
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!ChangesToCapturingPandoransImplemented && TFTVNewGameOptions.LimitedCaptureSetting)
                    {
                        MakeCaptureModuleResearchAvailable(TFTVNewGameOptions.LimitedCaptureSetting);
                        ChangesToCapturingPandoransImplemented = true;
                        TFTVLogger.Always($"Limited Capture is on");
                        return;
                    }
                    else if (ChangesToCapturingPandoransImplemented && !TFTVNewGameOptions.LimitedCaptureSetting)
                    {
                        MakeCaptureModuleResearchAvailable(TFTVNewGameOptions.LimitedCaptureSetting);
                        ChangesToCapturingPandoransImplemented = false;
                        TFTVLogger.Always($"Limited Capture reverted");
                    }

                    TFTVLogger.Always($"Limited Capture is off");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void MakeCaptureModuleResearchAvailable(bool limitedCapture)
            {
                try
                {
                    ResearchDef scyllaCaptureModule = DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef");
                    ResearchDbDef ppResearchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                    if (limitedCapture)
                    {
                        if (!ppResearchDB.Researches.Contains(scyllaCaptureModule))
                        {
                            ppResearchDB.Researches.Add(scyllaCaptureModule);
                        }
                    }

                    else
                    {
                        if (ppResearchDB.Researches.Contains(scyllaCaptureModule))
                        {
                            ppResearchDB.Researches.Remove(scyllaCaptureModule);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class StrongerPandorans
        {
            //Adapted from BetterEnemies by Dtony
            public static bool StrongerPandoransImplemented = false;
            internal static void ImplementStrongerPandorans()
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (TFTVNewGameOptions.StrongerPandoransSetting && !StrongerPandoransImplemented)
                    {
                        TFTVLogger.Always("Stronger Pandorans is on");
                        BEBuff_ArthronsTritons();
                        BEBuff_StartingEvolution();
                        BEBuff_Queen();
                        BEBUff_SirenChiron();
                        BEBuff_SmallCharactersAndSentinels();
                        StrongerPandoransImplemented = true;
                        return;
                    }
                    else if (!TFTVNewGameOptions.StrongerPandoransSetting && StrongerPandoransImplemented)
                    {

                        Revert_BEBuff_ArthronsTritons();
                        Revert_BEBuff_StartingEvolution();
                        Revert_BEBuff_Queen();
                        Revert_BEBUff_SirenChiron();
                        Revert_BEBuff_SmallCharactersAndSentinels();
                        StrongerPandoransImplemented = false;
                        TFTVLogger.Always("Stronger Pandorans reverted");
                    }

                    TFTVLogger.Always("Stronger Pandorans is off");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            public static void Revert_BEBuff_ArthronsTritons()
            {
                try
                {

                    TacCharacterDef crab15 = DefCache.GetDef<TacCharacterDef>("Crabman15_UltraShielder_AlienMutationVariationDef");

                    TacCharacterDef crab34 = DefCache.GetDef<TacCharacterDef>("Crabman34_UltraRanger_AlienMutationVariationDef");

                    WeaponDef arthronGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Grenade_WeaponDef");
                    WeaponDef arthronEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_EliteGrenade_WeaponDef");
                    WeaponDef arthronAcidGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_Grenade_WeaponDef");
                    WeaponDef arthronAcidEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_EliteGrenade_WeaponDef");

                    WeaponDef fishArmsParalyze = DefCache.GetDef<WeaponDef>("Fishman_UpperArms_Paralyzing_BodyPartDef");
                    WeaponDef fishArmsEliteParalyze = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_Paralyzing_BodyPartDef");

                    WeaponDef EliteBloodSuckers = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");

                    TacCharacterDef fish15 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");
                    TacCharacterDef fish17 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");


                    TacticalAbilityDef extremeFocus = DefCache.GetDef<TacticalAbilityDef>("ExtremeFocus_AbilityDef");
                    TacticalAbilityDef closeQuarters = DefCache.GetDef<TacticalAbilityDef>("CloseQuarters_AbilityDef");
                    TacticalAbilityDef bloodlust = DefCache.GetDef<TacticalAbilityDef>("BloodLust_AbilityDef");


                    fishArmsParalyze.DamagePayload.DamageKeywords[1].Value = 4;
                    fishArmsEliteParalyze.DamagePayload.DamageKeywords[1].Value = 6;

                    fish15.Data.BodypartItems[3] = fishArmsParalyze;
                    fish17.Data.BodypartItems[3] = fishArmsParalyze;

                    crab15.Data.BodypartItems[0] = DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef");

                    foreach (TacCharacterDef TriotonSniper in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Fishman") && a.name.Contains("Sniper")))
                    {
                        TriotonSniper.Data.Abilites = new TacticalAbilityDef[]
                        {

                        };
                    }

                    foreach (TacCharacterDef crab in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                    {
                        crab.Data.Abilites = new TacticalAbilityDef[]
                        {

                        };
                    }


                    foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && (aad.name.Contains("Pretorian") || aad.name.Contains("Tank"))))
                    {
                        character.Data.Speed = 2;
                    }

                    foreach (TacCharacterDef crabShield in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                    {
                        crabShield.Data.Speed = 4;
                    }

                    foreach (WeaponDef crabmanGl in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("LeftHand") && a.name.Contains("Grenade") && a.name.Contains("WeaponDef")))
                    {
                        crabmanGl.DamagePayload.Range = 11;
                    }

                    foreach (TacCharacterDef commando in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("Commando")))
                    {
                        commando.Data.Abilites = new TacticalAbilityDef[]
                        {

                        };
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void BEBuff_ArthronsTritons()
            {
                try
                {

                    TacCharacterDef crab15 = DefCache.GetDef<TacCharacterDef>("Crabman15_UltraShielder_AlienMutationVariationDef");

                    TacCharacterDef crab34 = DefCache.GetDef<TacCharacterDef>("Crabman34_UltraRanger_AlienMutationVariationDef");

                    WeaponDef arthronGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Grenade_WeaponDef");
                    WeaponDef arthronEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_EliteGrenade_WeaponDef");
                    WeaponDef arthronAcidGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_Grenade_WeaponDef");
                    WeaponDef arthronAcidEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_EliteGrenade_WeaponDef");

                    WeaponDef fishArmsParalyze = DefCache.GetDef<WeaponDef>("Fishman_UpperArms_Paralyzing_BodyPartDef");
                    WeaponDef fishArmsEliteParalyze = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_Paralyzing_BodyPartDef");

                    WeaponDef EliteBloodSuckers = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");

                    TacCharacterDef fish15 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");
                    TacCharacterDef fish17 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");


                    TacticalAbilityDef extremeFocus = DefCache.GetDef<TacticalAbilityDef>("ExtremeFocus_AbilityDef");
                    TacticalAbilityDef closeQuarters = DefCache.GetDef<TacticalAbilityDef>("CloseQuarters_AbilityDef");
                    TacticalAbilityDef bloodlust = DefCache.GetDef<TacticalAbilityDef>("BloodLust_AbilityDef");


                    fishArmsParalyze.DamagePayload.DamageKeywords[1].Value = 8;
                    fishArmsEliteParalyze.DamagePayload.DamageKeywords[1].Value = 16;

                    fish15.Data.BodypartItems[3] = EliteBloodSuckers;
                    fish17.Data.BodypartItems[3] = EliteBloodSuckers;

                    crab15.Data.BodypartItems[0] = crab34.Data.BodypartItems[0];

                    foreach (TacCharacterDef TriotonSniper in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Fishman") && a.name.Contains("Sniper")))
                    {
                        TriotonSniper.Data.Abilites = new TacticalAbilityDef[]
                        {
                   extremeFocus
                        };
                    }

                    foreach (TacCharacterDef crab in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                    {
                        crab.Data.Abilites = new TacticalAbilityDef[]
                        {
                    closeQuarters
                        };
                    }


                    foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && (aad.name.Contains("Pretorian") || aad.name.Contains("Tank"))))
                    {
                        character.Data.Speed = 6;
                    }

                    foreach (TacCharacterDef crabShield in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                    {
                        crabShield.Data.Speed = 8;
                    }

                    foreach (WeaponDef crabmanGl in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("LeftHand") && a.name.Contains("Grenade") && a.name.Contains("WeaponDef")))
                    {
                        crabmanGl.DamagePayload.Range = 15;
                    }

                    foreach (TacCharacterDef commando in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("Commando")))
                    {
                        commando.Data.Abilites = new TacticalAbilityDef[]
                        {
                    bloodlust
                        };
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void Revert_BEBuff_StartingEvolution()
            {
                try
                {
                    ResearchDef crabGunResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanGunner_ResearchDef");
                    ResearchDef fishWretchResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanSneaker_ResearchDef");
                    ResearchDef fishBasicResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanBasic_ResearchDef");
                    ResearchDef fishFootpadResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanAssault_ResearchDef");


                    crabGunResearch.InitialStates[4].State = ResearchState.Unlocked;
                    fishWretchResearch.InitialStates[4].State = ResearchState.Unlocked;
                    fishFootpadResearch.InitialStates[4].State = ResearchState.Unlocked;
                    fishBasicResearch.Unlocks = new ResearchRewardDef[] { DefCache.GetDef<UnitTemplateResearchRewardDef>("ALN_FishmanBasic_ResearchDef_UnitTemplateResearchRewardDef_0") };
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            public static void BEBuff_StartingEvolution()
            {
                try
                {
                    ResearchDef crabGunResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanGunner_ResearchDef");
                    ResearchDef fishWretchResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanSneaker_ResearchDef");
                    ResearchDef fishBasicResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanBasic_ResearchDef");
                    ResearchDef fishFootpadResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanAssault_ResearchDef");


                    crabGunResearch.InitialStates[4].State = ResearchState.Completed;
                    fishWretchResearch.InitialStates[4].State = ResearchState.Completed;
                    fishFootpadResearch.InitialStates[4].State = ResearchState.Completed;
                    fishBasicResearch.Unlocks = new ResearchRewardDef[0];
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            public static void Revert_BEBuff_Queen()
            {
                try
                {
                    TacticalItemDef queenSpawner = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Spawner_BodyPartDef");
                    TacticalItemDef queenBelcher = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
                    TacCharacterDef queenCrystal = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");

                    BodyPartAspectDef queenHeavyHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Heavy_BodyPartDef]");
                    BodyPartAspectDef queenSpitterHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Spitter_Goo_WeaponDef]");
                    BodyPartAspectDef queenSonicHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Sonic_WeaponDef]");

                    WeaponDef queenSmasher = DefCache.GetDef<WeaponDef>("Queen_Arms_Smashers_WeaponDef");

                    MindControlAbilityDef MindControl = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");


                    queenSpawner.Abilities = new AbilityDef[]
                    {
                queenSpawner.Abilities[0]
                    };

                    queenBelcher.Abilities = new AbilityDef[]
                    {
                queenBelcher.Abilities[0]
                    };

                    List<TacticalAbilityDef> scyllaAbilities = new List<TacticalAbilityDef>(queenCrystal.Data.Abilites);
                    scyllaAbilities.Remove(MindControl);
                    queenCrystal.Data.Abilites = scyllaAbilities.ToArray();

                    queenSmasher.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                queenSmasher.DamagePayload.DamageKeywords[0],
                queenSmasher.DamagePayload.DamageKeywords[1],


            };



                    queenSpawner.Armor = 30;
                    queenBelcher.Armor = 30;
                    queenHeavyHead.WillPower = 25;
                    queenSpitterHead.WillPower = 15;
                    queenSonicHead.WillPower = 20;

                    WeaponDef headSpitter = DefCache.GetDef<WeaponDef>("Queen_Head_Spitter_Goo_WeaponDef");
                    DamageKeywordDef acid = DefCache.GetDef<DamageKeywordDef>("Acid_DamageKeywordDataDef");
                    headSpitter.DamagePayload.DamageKeywords.RemoveLast();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void BEBuff_Queen()
            {
                try
                {
                    TacticalItemDef queenSpawner = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Spawner_BodyPartDef");
                    TacticalItemDef queenBelcher = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
                    TacCharacterDef queenCrystal = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");

                    BodyPartAspectDef queenHeavyHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Heavy_BodyPartDef]");
                    BodyPartAspectDef queenSpitterHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Spitter_Goo_WeaponDef]");
                    BodyPartAspectDef queenSonicHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Sonic_WeaponDef]");

                    WeaponDef queenSmasher = DefCache.GetDef<WeaponDef>("Queen_Arms_Smashers_WeaponDef");

                    MindControlAbilityDef MindControl = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");


                    queenSpawner.Abilities = new AbilityDef[]
                    {
                queenSpawner.Abilities[0],
                DefCache.GetDef<AbilityDef>("AcidResistant_DamageMultiplierAbilityDef"),
                    };

                    queenBelcher.Abilities = new AbilityDef[]
                    {
                queenBelcher.Abilities[0],
                DefCache.GetDef<AbilityDef>("AcidResistant_DamageMultiplierAbilityDef"),
                    };

                    List<TacticalAbilityDef> scyllaAbilities = new List<TacticalAbilityDef>(queenCrystal.Data.Abilites.ToList()) { MindControl };
                    queenCrystal.Data.Abilites = scyllaAbilities.ToArray();

                    foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Queen_AnimActionsDef")))
                    {
                        if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(MindControl))
                        {
                            animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(MindControl).ToArray();
                        }
                    }

                    queenSmasher.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                queenSmasher.DamagePayload.DamageKeywords[0],
                queenSmasher.DamagePayload.DamageKeywords[1],
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.ParalysingKeyword,
                    Value = 8,
                },
            };

                    queenSpawner.Armor = 60;
                    queenBelcher.Armor = 60;
                    queenHeavyHead.WillPower = 175;
                    queenSpitterHead.WillPower = 165;
                    queenSonicHead.WillPower = 170;

                    WeaponDef headSpitter = DefCache.GetDef<WeaponDef>("Queen_Head_Spitter_Goo_WeaponDef");
                    DamageKeywordDef acid = DefCache.GetDef<DamageKeywordDef>("Acid_DamageKeywordDataDef");
                    headSpitter.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = acid, Value = 30 });

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void Revert_BEBUff_SirenChiron()
            {
                try
                {


                    TacticalItemDef sirenLegsAgile = DefCache.GetDef<TacticalItemDef>("Siren_Legs_Agile_BodyPartDef");

                    TacticalItemDef sirenScremingHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Screamer_BodyPartDef");
                    PsychicScreamAbilityDef sirenPsychicScream = DefCache.GetDef<PsychicScreamAbilityDef>("Siren_PsychicScream_AbilityDef");

                    TacCharacterDef sirenBanshee = DefCache.GetDef<TacCharacterDef>("Siren3_InjectorBuffer_AlienMutationVariationDef");

                    TacticalPerceptionDef sirenPerception = DefCache.GetDef<TacticalPerceptionDef>("Siren_PerceptionDef");
                    TacCharacterDef sirenArmis = DefCache.GetDef<TacCharacterDef>("Siren5_Orichalcum_AlienMutationVariationDef");
                    WeaponDef sirenInjectorArms = DefCache.GetDef<WeaponDef>("Siren_Arms_Injector_WeaponDef");
                    TacticalItemDef sirenArmisHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Orichalcum_BodyPartDef");
                    WeaponDef sirenAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_AcidSpitter_WeaponDef");
                    WeaponDef sirenArmisAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_Orichalcum_WeaponDef");
                    ShootAbilityDef AcidSpray = DefCache.GetDef<ShootAbilityDef>("Siren_SpitAcid_AbilityDef");



                    sirenPerception.PerceptionRange = 30;

                    sirenBanshee.Data.Will = 8;
                    sirenBanshee.Data.BodypartItems[0] = DefCache.GetDef<TacticalItemDef>("Siren_Head_Buffer_BodyPartDef");
                    sirenBanshee.Data.Speed = 4;


                    sirenInjectorArms.DamagePayload.DamageKeywords[2].Value = 6;

                    sirenLegsAgile.Armor = 20;

                    sirenPsychicScream.ActionPointCost = 0.75f;
                    sirenPsychicScream.UsesPerTurn = 1;

                    sirenAcidTorso.APToUsePerc = 50;

                    sirenArmisAcidTorso.APToUsePerc = 50;

                    AcidSpray.UsesPerTurn = 1;

                    sirenBanshee.Data.Abilites = new TacticalAbilityDef[]
                    {

                    };

                    sirenArmis.Data.Abilites = new TacticalAbilityDef[]
                    {
                sirenArmis.Data.Abilites[0]

                    };

                    /*  sirenArmisHead.Abilities = new AbilityDef[]
                      {
                  sirenArmisHead.Abilities[0],
                      };*/

                    WeaponDef chironBlastMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef");
                    WeaponDef chironCristalMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Crystal_Mortar_WeaponDef");
                    WeaponDef chironAcidMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Acid_Mortar_WeaponDef");
                    WeaponDef chironFireWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_FireWorm_Launcher_WeaponDef");
                    WeaponDef chironAcidWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_AcidWorm_Launcher_WeaponDef");
                    WeaponDef chironPoisonWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef");
                    TacCharacterDef chironFireHeavy = DefCache.GetDef<TacCharacterDef>("Chiron2_FireWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironPoisonHeavy = DefCache.GetDef<TacCharacterDef>("Chiron4_PoisonWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironAcidHeavy = DefCache.GetDef<TacCharacterDef>("Chiron6_AcidWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironGooHeavy = DefCache.GetDef<TacCharacterDef>("Chiron8_GooHeavy_AlienMutationVariationDef");


                    chironFireHeavy.Data.Speed = 0;
                    chironPoisonHeavy.Data.Speed = 0;
                    chironAcidHeavy.Data.Speed = 0;
                    chironGooHeavy.Data.Speed = 0;

                    chironAcidMortar.ChargesMax = 15;

                    chironFireWormMortar.ChargesMax = 15;    // 15            

                    chironAcidWormMortar.ChargesMax = 15;    // 15            

                    chironPoisonWormMortar.ChargesMax = 15;    // 15            

                    chironBlastMortar.ChargesMax = 12;   // 12           

                    chironCristalMortar.ChargesMax = 12;    // 12

                    chironAcidMortar.DamagePayload.DamageKeywords[0].Value = 10;

                    foreach (WeaponDef ChironWormLauncher in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Chiron_Abdomen_") && a.name.Contains("Worm_Launcher_WeaponDef")))
                    {
                        ChironWormLauncher.DamagePayload.DamageKeywords[1].Value = 120;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void BEBUff_SirenChiron()
            {
                try
                {


                    TacticalItemDef sirenLegsAgile = DefCache.GetDef<TacticalItemDef>("Siren_Legs_Agile_BodyPartDef");

                    TacticalItemDef sirenScremingHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Screamer_BodyPartDef");
                    PsychicScreamAbilityDef sirenPsychicScream = DefCache.GetDef<PsychicScreamAbilityDef>("Siren_PsychicScream_AbilityDef");

                    TacCharacterDef sirenBanshee = DefCache.GetDef<TacCharacterDef>("Siren3_InjectorBuffer_AlienMutationVariationDef");

                    TacticalPerceptionDef sirenPerception = DefCache.GetDef<TacticalPerceptionDef>("Siren_PerceptionDef");
                    TacCharacterDef sirenArmis = DefCache.GetDef<TacCharacterDef>("Siren5_Orichalcum_AlienMutationVariationDef");
                    WeaponDef sirenInjectorArms = DefCache.GetDef<WeaponDef>("Siren_Arms_Injector_WeaponDef");
                    TacticalItemDef sirenArmisHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Orichalcum_BodyPartDef");
                    WeaponDef sirenAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_AcidSpitter_WeaponDef");
                    WeaponDef sirenArmisAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_Orichalcum_WeaponDef");
                    ShootAbilityDef AcidSpray = DefCache.GetDef<ShootAbilityDef>("Siren_SpitAcid_AbilityDef");

                    WeaponDef chironBlastMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef");
                    WeaponDef chironCristalMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Crystal_Mortar_WeaponDef");
                    WeaponDef chironAcidMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Acid_Mortar_WeaponDef");
                    WeaponDef chironFireWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_FireWorm_Launcher_WeaponDef");
                    WeaponDef chironAcidWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_AcidWorm_Launcher_WeaponDef");
                    WeaponDef chironPoisonWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef");
                    TacCharacterDef chironFireHeavy = DefCache.GetDef<TacCharacterDef>("Chiron2_FireWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironPoisonHeavy = DefCache.GetDef<TacCharacterDef>("Chiron4_PoisonWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironAcidHeavy = DefCache.GetDef<TacCharacterDef>("Chiron6_AcidWormHeavy_AlienMutationVariationDef");
                    TacCharacterDef chironGooHeavy = DefCache.GetDef<TacCharacterDef>("Chiron8_GooHeavy_AlienMutationVariationDef");

                    sirenPerception.PerceptionRange = 38;
                    sirenBanshee.Data.Will = 14;
                    sirenBanshee.Data.BodypartItems[0] = sirenScremingHead;
                    sirenBanshee.Data.Speed = 9;
                    sirenInjectorArms.DamagePayload.DamageKeywords[2].Value = 10;
                    sirenLegsAgile.Armor = 30;
                    sirenPsychicScream.ActionPointCost = 0.25f;
                    sirenPsychicScream.UsesPerTurn = 1;
                    sirenAcidTorso.APToUsePerc = 25;
                    sirenArmisAcidTorso.APToUsePerc = 25;
                    AcidSpray.UsesPerTurn = 1;

                    sirenBanshee.Data.Abilites = new TacticalAbilityDef[]
                    {

                DefCache.GetDef<TacticalAbilityDef>("Thief_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("StealthSpecialist_AbilityDef")
                    };

                    sirenArmis.Data.Abilites = new TacticalAbilityDef[]
                    {
                sirenArmis.Data.Abilites[0],
                DefCache.GetDef<TacticalAbilityDef>("IgnorePain_AbilityDef"),
                    };

                    /*   sirenArmisHead.Abilities = new AbilityDef[]
                       {
                   sirenArmisHead.Abilities[0],
                       };*/

                    chironFireHeavy.Data.Speed = 8;
                    chironPoisonHeavy.Data.Speed = 8;
                    chironAcidHeavy.Data.Speed = 8;
                    chironGooHeavy.Data.Speed = 8;

                    chironAcidMortar.ChargesMax = 18;

                    chironFireWormMortar.ChargesMax = 18;    // 15            

                    chironAcidWormMortar.ChargesMax = 18;    // 15            

                    chironPoisonWormMortar.ChargesMax = 18;    // 15            

                    chironBlastMortar.ChargesMax = 18;   // 12           

                    chironCristalMortar.ChargesMax = 30;    // 12

                    chironAcidMortar.DamagePayload.DamageKeywords[0].Value = 20;

                    foreach (WeaponDef ChironWormLauncher in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Chiron_Abdomen_") && a.name.Contains("Worm_Launcher_WeaponDef")))
                    {
                        ChironWormLauncher.DamagePayload.DamageKeywords[1].Value = 240;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void Revert_BEBuff_SmallCharactersAndSentinels()
            {
                try
                {
                    TacCharacterDef fireworm = DefCache.GetDef<TacCharacterDef>("Fireworm_AlienMutationVariationDef");
                    TacCharacterDef acidworm = DefCache.GetDef<TacCharacterDef>("Acidworm_AlienMutationVariationDef");
                    TacCharacterDef poisonworm = DefCache.GetDef<TacCharacterDef>("Poisonworm_AlienMutationVariationDef");
                    BodyPartAspectDef acidWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Acidworm_Torso_BodyPartDef]");
                    BodyPartAspectDef fireWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Fireworm_Torso_BodyPartDef]");
                    BodyPartAspectDef poisonWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Poisonworm_Torso_BodyPartDef]");
                    ApplyDamageEffectAbilityDef aWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("AcidwormExplode_AbilityDef");
                    ApplyDamageEffectAbilityDef fWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("FirewormExplode_AbilityDef");
                    ApplyDamageEffectAbilityDef pWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("PoisonwormExplode_AbilityDef");


                    TacticalPerceptionDef tacticalPerceptionHatchling = DefCache.GetDef<TacticalPerceptionDef>("SentinelHatching_PerceptionDef");
                    TacticalPerceptionDef tacticalPerceptionTerror = DefCache.GetDef<TacticalPerceptionDef>("SentinelTerror_PerceptionDef");


                    TacCharacterDef faceHuggerTac = DefCache.GetDef<TacCharacterDef>("Facehugger_TacCharacterDef");
                    TacCharacterDef faceHuggerVariation = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                    TacticalActorDef faceHugger = DefCache.GetDef<TacticalActorDef>("Facehugger_ActorDef");



                    RagdollDieAbilityDef FHDie = (RagdollDieAbilityDef)faceHugger.Abilities[2];
                    FHDie.DeathEffect = new EffectDef();



                    tacticalPerceptionTerror.PerceptionRange = 10;

                    tacticalPerceptionHatchling.PerceptionRange = 10;

                    foreach (SurveillanceAbilityDef eggSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Egg")))
                    {
                        eggSurv.TargetingDataDef.Origin.Range = 4;
                    }

                    foreach (SurveillanceAbilityDef sentinelSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Sentinel")))
                    {
                        sentinelSurv.TargetingDataDef.Origin.Range = 12;
                    }

                    int wormSpeed = 0;

                    int fWormFireDamage = 40;
                    int pWormBlastDamage = 25;
                    int pWormPoisonDamage = 50;

                    fireworm.DeploymentCost = 35;    // 35
                    acidworm.DeploymentCost = 35;    // 35
                    poisonworm.DeploymentCost = 35;  // 35


                    acidWorm.Speed = wormSpeed;
                    fireWorm.Speed = wormSpeed;
                    poisonWorm.Speed = wormSpeed;

                    aWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = 10 },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.AcidKeyword, Value = 20 },
                };

                    fWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BurningKeyword, Value = fWormFireDamage },

                };

                    pWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = pWormBlastDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.PoisonousKeyword, Value = pWormPoisonDamage },

                };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void BEBuff_SmallCharactersAndSentinels()
            {
                try
                {
                    TacCharacterDef fireworm = DefCache.GetDef<TacCharacterDef>("Fireworm_AlienMutationVariationDef");
                    TacCharacterDef acidworm = DefCache.GetDef<TacCharacterDef>("Acidworm_AlienMutationVariationDef");
                    TacCharacterDef poisonworm = DefCache.GetDef<TacCharacterDef>("Poisonworm_AlienMutationVariationDef");
                    BodyPartAspectDef acidWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Acidworm_Torso_BodyPartDef]");
                    BodyPartAspectDef fireWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Fireworm_Torso_BodyPartDef]");
                    BodyPartAspectDef poisonWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Poisonworm_Torso_BodyPartDef]");
                    ApplyDamageEffectAbilityDef aWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("AcidwormExplode_AbilityDef");
                    ApplyDamageEffectAbilityDef fWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("FirewormExplode_AbilityDef");
                    ApplyDamageEffectAbilityDef pWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("PoisonwormExplode_AbilityDef");


                    TacticalPerceptionDef tacticalPerceptionHatchling = DefCache.GetDef<TacticalPerceptionDef>("SentinelHatching_PerceptionDef");
                    TacticalPerceptionDef tacticalPerceptionTerror = DefCache.GetDef<TacticalPerceptionDef>("SentinelTerror_PerceptionDef");

                    TacCharacterDef faceHuggerTac = DefCache.GetDef<TacCharacterDef>("Facehugger_TacCharacterDef");
                    TacCharacterDef faceHuggerVariation = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                    TacticalActorDef faceHugger = DefCache.GetDef<TacticalActorDef>("Facehugger_ActorDef");

                    //  GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                    int faceHuggerBlastDamage = 1;
                    int faceHuggerAcidDamage = 10;
                    int faceHuggerAOERadius = 2;

                    string skillName = "BC_SwarmerAcidExplosion_Die_AbilityDef";
                    RagdollDieAbilityDef source = DefCache.GetDef<RagdollDieAbilityDef>("SwarmerAcidExplosion_Die_AbilityDef");
                    RagdollDieAbilityDef sAE = Helper.CreateDefFromClone(
                        source,
                        "1137345a-a18d-4800-b52e-b15d49f4dabf",
                        skillName);
                    sAE.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "10729876-f764-41b5-9b4e-c8cb98dca771",
                        skillName);
                    DamagePayloadEffectDef sAEEffect = Helper.CreateDefFromClone(
                        DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerAcidExplosion_Die_AbilityDef]"),
                        "ac9cd527-72d4-42d2-af32-5efbdf32812e",
                        "E_Element0 [BC_SwarmerAcidExplosion_Die_AbilityDef]");

                    sAE.DeathEffect = sAEEffect;
                    sAEEffect.DamagePayload.DamageKeywords[0].Value = faceHuggerBlastDamage;
                    sAEEffect.DamagePayload.DamageKeywords[1].Value = faceHuggerAcidDamage;
                    sAEEffect.DamagePayload.AoeRadius = faceHuggerAOERadius;

                    sAE.ViewElementDef.DisplayName1.LocalizationKey = "KEY_MINDFRAGGER_ACID_EXPLOSION";
                    sAE.ViewElementDef.Description.LocalizationKey = "KEY_MINDFRAGGER_ACID_EXPLOSION_DESCRIPTION";

                    RagdollDieAbilityDef FHDie = (RagdollDieAbilityDef)faceHugger.Abilities[2];
                    FHDie.DeathEffect = sAEEffect;


                    tacticalPerceptionTerror.PerceptionRange = 18;

                    tacticalPerceptionHatchling.PerceptionRange = 18;

                    foreach (SurveillanceAbilityDef eggSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Egg")))
                    {
                        eggSurv.TargetingDataDef.Origin.Range = 7;
                    }

                    foreach (SurveillanceAbilityDef sentinelSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Sentinel")))
                    {
                        sentinelSurv.TargetingDataDef.Origin.Range = 18;
                    }

                    int wormSpeed = 9;
                    int wormShredDamage = 3;
                    int aWormAcidDamage = 20; //nerfed on 13/11
                    int aWormBlastDamage = 10;
                    int fWormFireDamage = 40;
                    int pWormBlastDamage = 25;
                    int pWormPoisonDamage = 50;
                    fireworm.DeploymentCost = 10;    // 35
                    acidworm.DeploymentCost = 10;    // 35
                    poisonworm.DeploymentCost = 10;  // 35
                    acidWorm.Speed = wormSpeed;
                    fireWorm.Speed = wormSpeed;
                    poisonWorm.Speed = wormSpeed;

                    aWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = aWormBlastDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.AcidKeyword, Value = aWormAcidDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };

                    fWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BurningKeyword, Value = fWormFireDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };

                    pWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = pWormBlastDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.PoisonousKeyword, Value = pWormPoisonDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



















    }
}
