﻿using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;



namespace TFTV
{
    internal class TFTVBetaSaveGamesFixes
    {
        // public static bool LOTAapplied = false;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        // public static bool LOTAReworkGlobalCheck = false;

        private static readonly SharedData Shared = TFTVMain.Shared;

        public static void CorrrectPhoenixSaveManagerDifficulty()
        {
            try
            {
                PhoenixSaveManager phoenixSaveManager = GameUtl.GameComponent<PhoenixGame>().SaveManager;
                FieldInfo currentDifficultyField = typeof(PhoenixSaveManager).GetField("_currentDifficulty", BindingFlags.NonPublic | BindingFlags.Instance);

                GameDifficultyLevelDef difficulty = (GameDifficultyLevelDef)currentDifficultyField.GetValue(phoenixSaveManager);

                if (difficulty != null)
                {
                    TFTVLogger.Always($"difficulty is {difficulty}");
                }
                else
                {
                    TFTVLogger.Always($"No difficulty set as current difficulty!");


                    GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (geoLevelController != null)
                    {

                        currentDifficultyField.SetValue(phoenixSaveManager, geoLevelController.CurrentDifficultyLevel);

                        GameDifficultyLevelDef newDifficulty = (GameDifficultyLevelDef)currentDifficultyField.GetValue(phoenixSaveManager);


                        TFTVLogger.Always($"Current difficulty set to {newDifficulty?.name}");
                    }
                    else
                    {
                        GameDifficultyLevelDef gameDifficultyLevelDef = null;


                        if (TFTVNewGameOptions.InternalDifficultyCheck != 0)
                        {
                            DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef").Order = 2;
                            DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef").Order = 3;
                            DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef").Order = 4;
                            DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef").Order = 5;

                            switch (TFTVNewGameOptions.InternalDifficultyCheck)
                            {
                                case 1:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");
                                    break;

                                case 2:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");
                                    break;

                                case 3:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                                    break;

                                case 4:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                                    break;

                                case 5:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                                    break;

                                case 6:
                                    gameDifficultyLevelDef = DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");
                                    break;
                            }
                            currentDifficultyField.SetValue(phoenixSaveManager, geoLevelController.CurrentDifficultyLevel);

                            GameDifficultyLevelDef newDifficulty = (GameDifficultyLevelDef)currentDifficultyField.GetValue(phoenixSaveManager);


                            TFTVLogger.Always($"Current difficulty set to {newDifficulty?.name}");


                        }
                        else
                        {
                            string warning = $"Could not find difficulty! This is a tactical save made before Update# 36. Please load a Geoscape save before this mission; this save is doomed!";

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        public static void RemoveBadSlug(GeoLevelController controller)
        {
            try
            {
                TFTVLogger.Always($"Looking for bad slugs...");

                List<GeoCharacter> deathList = new List<GeoCharacter>();

                List<GeoItem> destroyList = new List<GeoItem>();

                ItemDef slugArmsDef = DefCache.GetDef<ItemDef>("NJ_Technician_MechArms_ALN_WeaponDef");
                ItemDef slugHelmet = DefCache.GetDef<ItemDef>("NJ_Technician_Helmet_ALN_BodyPartDef");
                ItemDef slugLegs = DefCache.GetDef<ItemDef>("NJ_Technician_Legs_ALN_ItemDef");
                ItemDef slugTorso = DefCache.GetDef<ItemDef>("NJ_Technician_Torso_ALN_BodyPartDef");


                foreach (ItemDef itemDef in controller.PhoenixFaction.ItemStorage.Items.Keys)
                {
                    if (itemDef == slugArmsDef || itemDef == slugHelmet || itemDef == slugTorso || itemDef == slugLegs)
                    {
                        destroyList.Add(controller.PhoenixFaction.ItemStorage.Items[slugArmsDef]);
                        TFTVLogger.Always($"found bad slug arms");
                    }
                }

                foreach (GeoItem item in destroyList)
                {
                    controller.PhoenixFaction.ItemStorage.RemoveItem(item);
                    TFTVLogger.Always($"destroyed bad slug arms");

                }



                foreach (GeoCharacter geoCharacter in controller.PhoenixFaction.Soldiers)
                {

                    //  if (geoCharacter.GameTags.Contains(TFTVChangesToDLC5.MercenaryTag)) 
                    //  {
                    //    TFTVLogger.Always($"found mercenary {geoCharacter.DisplayName}");

                    if (geoCharacter.ArmourItems.Any(i => i.ItemDef == DefCache.GetDef<ItemDef>("NJ_Technician_MechArms_ALN_WeaponDef")))
                    {

                        TFTVLogger.Always($"{geoCharacter.DisplayName} has aln tech arms");

                        foreach (GeoItem geoItem in geoCharacter.ArmourItems)
                        {
                            if (geoItem.ItemDef == slugArmsDef && geoItem.CommonItemData.Ammo != null)
                            {
                                TFTVLogger.Always($"{geoCharacter.DisplayName} has in armor {geoItem.ItemDef.name}, and it has {geoItem.CommonItemData.Ammo.CurrentCharges} charges ");
                                deathList.Add(geoCharacter);
                            }
                        }
                    }


                }

                if (deathList.Count > 0)
                {
                    foreach (GeoCharacter geoCharacter in deathList)
                    {
                        TFTVLogger.Always($"dismising bad slug {geoCharacter.DisplayName}");
                        controller.PhoenixFaction.KillCharacter(geoCharacter, CharacterDeathReason.Dismissed);

                    }


                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static void SetMarketPlaceRotations(GeoLevelController controller)
        {
            try
            {
                GeoMarketplace geoMarketplace = controller.Marketplace;

                DateTime cutoffDay = new DateTime(2047, 1, 5); // January 5th, 2047

                if (geoMarketplace.AllMissionsCompleted && controller.Timing.Now.DateTime > cutoffDay && controller.EventSystem.GetVariable("MarketPlaceRotations") == 0)
                {
                    // Calculate the number of days from the cutoff day to the current date and divide by 4
                    TimeSpan timeDifference = controller.Timing.Now.DateTime - cutoffDay;
                    int variable = (int)(timeDifference.TotalDays / 4);

                    // Set the "MarketPlaceRotations" variable
                    TFTVLogger.Always($"Adjusting old save to new Marketplace! today is {controller.Timing.Now.DateTime}, so setting MarketPlaceRotation to {variable}");

                    controller.EventSystem.SetVariable("MarketPlaceRotations", variable);
                    TFTVChangesToDLC5.ForceMarketPlaceUpdate();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        [HarmonyPatch(typeof(GeoMission), "TryReloadItem")]
        public static class GeoMission_TryReloadItem_patch
        {

            public static bool Prefix(GeoMission __instance, GeoItem item, ItemStorage storage, string storageName)
            {
                try
                {
                    // TFTVLogger.Always($"{item} storage {storage} storageName {storageName}");

                    WeaponDef Obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
                    WeaponDef Subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
                    WeaponDef Redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
                    WeaponDef Devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
                    WeaponDef Tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

                    List<WeaponDef> kaosGuns = new List<WeaponDef>() { Obliterator, Subjector, Redemptor, Devastator, Tormentor };


                    if (kaosGuns.Contains(item.ItemDef) && item.CommonItemData.Ammo == null)
                    {

                        TFTVLogger.Always($"trying to reload an old {item} that doesn't have compatible ammo! Canceling reload to avoid softlock");

                        return false;

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

        public static void CheckScyllaCaptureTechResearch(GeoLevelController controller)
        {
            try
            {
                ResearchDef scyllaCaptureModule = DefCache.GetDef<ResearchDef>("PX_Aircraft_EscapePods_ResearchDef");




                if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_Queen_ResearchDef") &&
                    (!controller.PhoenixFaction.Research.HasCompleted(scyllaCaptureModule.name) &&
                    !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == scyllaCaptureModule)))
                {
                    TFTVLogger.Always($"Player has completed Scylla autopsy research but didn't get the Scylla Capture Tech");
                    ResearchElement researchElement = controller.PhoenixFaction.Research.GetResearchById(scyllaCaptureModule.name);
                    researchElement.State = ResearchState.Unlocked;
                    TFTVLogger.Always($"{scyllaCaptureModule.name} available to PX? {researchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

        public static void CheckResearches(GeoLevelController controller)
        {
            try
            {

                ResearchDef terrorSentinelResearch = DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef");
                ResearchDef advNanotechRes = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");

                ResearchDef acidWormRes = DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef");
                ResearchDef fireWormRes = DefCache.GetDef<ResearchDef>("PX_Alien_Fireworm_ResearchDef");
                ResearchDef acidRes = DefCache.GetDef<ResearchDef>("PX_BlastResistanceVest_ResearchDef");

                ResearchDef mutationTech = DefCache.GetDef<ResearchDef>("ANU_MutationTech_ResearchDef");

                ResearchDef agileGLRes = DefCache.GetDef<ResearchDef>("PX_AGL_ResearchDef");

                if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_Acidworm_ResearchDef") &&
                    (!controller.PhoenixFaction.Research.HasCompleted(acidRes.name) &&
                    !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == acidRes)))
                {
                    TFTVLogger.Always($"Player has completed acidworm research but didn't get acid res tech");
                    ResearchElement researchElement = controller.PhoenixFaction.Research.GetResearchById(acidRes.name);
                    researchElement.State = ResearchState.Unlocked;
                    TFTVLogger.Always($"{acidRes.name} available to PX? {researchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                }

                if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_Fireworm_ResearchDef") &&
                   (!controller.PhoenixFaction.Research.HasCompleted(agileGLRes.name) &&
                   !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == agileGLRes)))
                {
                    TFTVLogger.Always($"Player has completed fireworm research but didn't get agile GL tech");
                    ResearchElement researchElement = controller.PhoenixFaction.Research.GetResearchById(agileGLRes.name);
                    researchElement.State = ResearchState.Unlocked;
                    TFTVLogger.Always($"{agileGLRes.name} available to PX? {researchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                }


                if (controller.PhoenixFaction.Research.HasCompleted("PX_Mutoid_ResearchDef") &&
                   (!controller.PhoenixFaction.Research.HasCompleted(mutationTech.name) &&
                   !controller.PhoenixFaction.Research.Researchable.Any(re => re.ResearchDef == mutationTech)))

                {

                    TFTVLogger.Always($"Player has completed Mutoids research but didn't get Mutation tech");

                    ResearchElement researchElement = controller.PhoenixFaction.Research.GetResearchById(mutationTech.name);
                    researchElement.State = ResearchState.Unlocked;
                    TFTVLogger.Always($"{mutationTech.name} available to PX? {researchElement.IsAvailableToFaction(controller.PhoenixFaction)}");

                }

                //  TFTVLogger.Always($"{controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech_ResearchDef")} {controller.PhoenixFaction.HarvestAliensForMutagensUnlocked}");

                if (controller.PhoenixFaction.Research.HasCompleted("ANU_MutationTech_ResearchDef") && !controller.PhoenixFaction.HarvestAliensForMutagensUnlocked)
                {

                    TFTVLogger.Always("Player researched Mutation, but hasn't unlocked Mutagen harvesting");
                    controller.PhoenixFaction.HarvestAliensForMutagensUnlocked = true;


                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void SpecialFixForTesting(GeoLevelController controller)
        {
            try
            {
                controller.EventSystem.SetVariable("NewGameStarted", 1);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void OpenBetaSaveGameFixes(GeoLevelController controller)
        {
            try
            {
                //  CheckNewLOTA(controller);
                FixScyllaCounter(controller);
                //  FixInfestedBase(controller);
                CheckSaveGameEventChoices(controller);
                CheckUmbraResearchVariable(controller);
                AddInteranlDifficultyCheckSaveData(controller);
                SetMarketPlaceRotations(controller);
                //  FixReactivateCyclopsMission(controller);
                //  SetStrongerPandoransOn();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void SetStrongerPandoransOn()
        {
            try
            {
                TFTVNewGameOptions.StrongerPandoransSetting = true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        private static void FixReactivateCyclopsMission(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetVariable(TFTVAncientsGeo.CyclopsBuiltVariable) == 1 && controller.Map.AllSites.Any(s => (s.Type == GeoSiteType.AncientHarvest || s.Type == GeoSiteType.AncientRefinery) && s.Owner == controller.AlienFaction))

                {
                    foreach (GeoSite geoSite in controller.Map.AllSites.Where(s => (s.Type == GeoSiteType.AncientHarvest || s.Type == GeoSiteType.AncientRefinery) && s.Owner == controller.AlienFaction))
                    {
                        TFTVLogger.Always($"{geoSite.LocalizedSiteName}, {geoSite.IsExcavated()}");

                    }

                    TFTVLogger.Always($"Player failed the Cyclops mission, need to clean up");
                    controller.EventSystem.SetVariable(TFTVAncientsGeo.CyclopsBuiltVariable, 0);
                    TFTVCommonMethods.RemoveManuallySetObjective(controller, "PROTECT_THE_CYCLOPS_OBJECTIVE_GEO_TITLE");
                    controller.Map.AllSites.FirstOrDefault(s => (s.Type == GeoSiteType.AncientHarvest || s.Type == GeoSiteType.AncientRefinery) && s.Owner == controller.AlienFaction).Owner = controller.PhoenixFaction;
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        private static void AddInteranlDifficultyCheckSaveData(GeoLevelController controller)
        {
            try
            {
                if (TFTVNewGameOptions.InternalDifficultyCheck == 0)
                {

                    TFTVNewGameOptions.InternalDifficultyCheck = controller.CurrentDifficultyLevel.Order;

                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void FixScyllaCounter(GeoLevelController controller)
        {
            try
            {
                GeoscapeEventSystem eventSystem = controller.EventSystem;

                int internalScyllaVariable = TFTVPandoranProgress.ScyllaCount;



                if (eventSystem.GetVariable("ScyllaCounter") == 0)
                {
                    if (internalScyllaVariable > 0)
                    {
                        eventSystem.SetVariable("ScyllaCounter", internalScyllaVariable);


                    }
                    else
                    {
                        int activeCitadels = controller.AlienFaction.Bases.Where(b => b.AlienBaseTypeDef.MonsterClassType != null).Count();
                        PhoenixStatisticsManager phoenixStatisticsManager = GameUtl.GameComponent<PhoenixGame>().GetComponent<PhoenixStatisticsManager>();

                        if (phoenixStatisticsManager == null)
                        {
                            TFTVLogger.Always($"Failed to get stat manager in FixScyllaCounter");
                            return;
                        }

                        int destroyedCitadels = phoenixStatisticsManager.CurrentGameStats.GeoscapeStats.DestroyedCitadels;

                        ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                        int scyllaActuallyKilled = 0;
                        foreach (SoldierStats soldierStats in phoenixStatisticsManager.CurrentGameStats.LivingSoldiers.Values)
                        {
                            if (soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count() > 0)
                            {
                                scyllaActuallyKilled += soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count();
                                TFTVLogger.Always($"{soldierStats.Name} killed {soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count()} scyllas!");
                            }


                        }
                        foreach (SoldierStats soldierStats in phoenixStatisticsManager.CurrentGameStats.DeadSoldiers.Values)
                        {
                            if (soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count() > 0)
                            {
                                scyllaActuallyKilled += soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count();
                                TFTVLogger.Always($"{soldierStats.Name} killed {soldierStats.EnemiesKilled.Where(ek => ek.Class == queenTag).Count()} scyllas!");
                            }

                        }
                        int totalScyllas = activeCitadels + scyllaActuallyKilled + destroyedCitadels;

                        TFTVLogger.Always($"active citadels# {activeCitadels}; destroyed citadels {destroyedCitadels}, scyllas recorded as killed {scyllaActuallyKilled}, so total Scyllas spawned # {totalScyllas}");

                        if (totalScyllas > 0)
                        {
                            eventSystem.SetVariable("ScyllaCounter", totalScyllas);

                        }

                    }

                }

                TFTVLogger.Always($"ScyllaCounter: {eventSystem.GetVariable("ScyllaCounter")}");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        internal static void FixInfestedBase(GeoLevelController controller)
        {
            try
            {
                FieldInfo basesField = AccessTools.Field(typeof(GeoPhoenixFaction), "_bases");
                List<GeoPhoenixBase> bases = (List<GeoPhoenixBase>)basesField.GetValue(controller.PhoenixFaction);

                foreach (GeoSite geoSite in controller.Map.AllSites)

                {
                    if (geoSite.GetComponent<GeoPhoenixBase>() != null && geoSite.Owner == controller.PhoenixFaction && !bases.Contains(geoSite.GetComponent<GeoPhoenixBase>()))
                    {

                        TFTVLogger.Always($"found base {geoSite.LocalizedSiteName} under Phoenix control but not in _bases list");
                        bases.Add(geoSite.GetComponent<GeoPhoenixBase>());
                        geoSite.RefreshVisuals();

                    }


                }





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        //now deprecated
        /* internal static void CheckNewLOTA(GeoLevelController controller)
         {
             try
             {
                 if (controller.EventSystem.GetVariable("NewGameStarted") == 1 && !LOTAapplied)
                 {
                     LOTAapplied = true;
                     TFTVDefsInjectedOnlyOnce.ChangesToLOTA2();
                     TFTVAncients.LOTAReworkActive = true;
                     LOTAReworkGlobalCheck = true;
                     TFTVLogger.Always("LOTA rework activated!");
                 }
                 CheckIfLOTAReworkActive();
             }

             catch (Exception e)
             {
                 TFTVLogger.Error(e);
             }
         }*/

        /*  public static void CheckNewLOTASavegame()
          {
              try
              {
                  if ((LOTAReworkGlobalCheck || TFTVAncients.LOTAReworkActive) && !LOTAapplied)
                  {
                      LOTAapplied = true;
                      TFTVDefsInjectedOnlyOnce.ChangesToLOTA2();
                      TFTVAncients.LOTAReworkActive = true;
                      TFTVLogger.Always("LOTA rework activated!");
                  }
                  CheckIfLOTAReworkActive();
              }

              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }
          }*/

        /*   internal static void CheckIfLOTAReworkActive()
           {
               try
               {

                   ContextHelpHintDef hintStory1 = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_STORY1");
                   ContextHelpHintDef hintCyclops = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPS");
                   ContextHelpHintDef hintCyclopsDefense = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_CYCLOPSDEFENSE");
                   ContextHelpHintDef hintHoplites = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITS");
                   ContextHelpHintDef hintHopliteRepair = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSREPAIR");
                   ContextHelpHintDef hintHopliteMaxPower = DefCache.GetDef<ContextHelpHintDef>("ANCIENTS_HOPLITSMAXPOWER");

                   ContextHelpHintDbDef alwaysDisplayedTacticalHintsDbDef = DefCache.GetDef<ContextHelpHintDbDef>("AlwaysDisplayedTacticalHintsDbDef");

                   ResearchDbDef researchDB = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                   ResearchDef ancientAutomataResearch = DefCache.GetDef<ResearchDef>("AncientAutomataResearch");
                   ResearchDef pX_LivingCrystalResearch = DefCache.GetDef<ResearchDef>("PX_LivingCrystalResearchDef");
                   ResearchDef pX_ProteanMutaneResearch = DefCache.GetDef<ResearchDef>("PX_ProteanMutaneResearchDef");


                   AncientSiteProbeItemDef ancientSiteProbeItemDef = DefCache.GetDef<AncientSiteProbeItemDef>("AncientSiteProbeItemDef");


                   if (TFTVAncients.LOTAReworkActive)
                   {
                       if (!researchDB.Researches.Contains(ancientAutomataResearch))
                       {
                           researchDB.Researches.Add(ancientAutomataResearch);
                       }
                       if (!researchDB.Researches.Contains(pX_LivingCrystalResearch))
                       {
                           researchDB.Researches.Add(pX_LivingCrystalResearch);
                       }
                       if (!researchDB.Researches.Contains(pX_ProteanMutaneResearch))
                       {
                           researchDB.Researches.Add(pX_ProteanMutaneResearch);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintStory1))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintStory1);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclops))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclops);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclopsDefense))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintCyclopsDefense);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHoplites))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHoplites);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteRepair))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteRepair);
                       }
                       if (!alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteMaxPower))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Add(hintHopliteMaxPower);
                       }
                   }
                   else
                   {
                       if (researchDB.Researches.Contains(ancientAutomataResearch))
                       {
                           researchDB.Researches.Remove(ancientAutomataResearch);
                       }
                       if (researchDB.Researches.Contains(pX_LivingCrystalResearch))
                       {
                           researchDB.Researches.Remove(pX_LivingCrystalResearch);
                       }
                       if (researchDB.Researches.Contains(pX_ProteanMutaneResearch))
                       {
                           researchDB.Researches.Remove(pX_ProteanMutaneResearch);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintStory1))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintStory1);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclops))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintCyclops);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintCyclopsDefense))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintCyclopsDefense);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHoplites))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHoplites);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteRepair))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHopliteRepair);
                       }
                       if (alwaysDisplayedTacticalHintsDbDef.Hints.Contains(hintHopliteMaxPower))
                       {
                           alwaysDisplayedTacticalHintsDbDef.Hints.Remove(hintHopliteMaxPower);
                       }
                       ancientSiteProbeItemDef.ManufactureTech = 25;
                       ancientSiteProbeItemDef.ManufactureMaterials = 75;
                   }
               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
               }


           }*/


        internal static void CheckSaveGameEventChoices(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == -1)
                {
                    controller.EventSystem.GetEventRecord("PROG_AN2").SelectChoice(0);
                    controller.EventSystem.GetEventRecord("PROG_AN2").Complete(controller.Timing.Now);
                }
                if (controller.EventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == -1)
                {
                    controller.EventSystem.GetEventRecord("PROG_AN4").SelectChoice(1);
                    controller.EventSystem.GetEventRecord("PROG_AN4").Complete(controller.Timing.Now);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CheckUmbraResearchVariable(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0) //|| controller.AlienFaction.EvolutionProgress>=4700)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 4);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }
                else if (controller.EventSystem.GetEventRecord("SDI_09")?.SelectedChoice == 0)// || controller.AlienFaction.EvolutionProgress >= 4230)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 3);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }
                else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)// || controller.AlienFaction.EvolutionProgress >= 2820)
                {
                    controller.EventSystem.SetVariable(TFTVUmbra.TBTVVariableName, 2);
                    TFTVLogger.Always(TFTVUmbra.TBTVVariableName + " is set to " + controller.EventSystem.GetVariable(TFTVUmbra.TBTVVariableName));
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
