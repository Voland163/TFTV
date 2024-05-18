using Base;
using Base.Core;
using Base.Defs;
using Epic.OnlineServices;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVSpecialDifficulties
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static bool DataForRookieSaved = false;
        private static Dictionary<string, int> AlienBodyPartsDictionary = new Dictionary<string, int>();
        private static List<TacticalItemDef> AlienBodyParts = new List<TacticalItemDef>();

        public static int CounterSpawned = 0;

        [HarmonyPatch(typeof(GeoMission), "ApplyTacticalMissionResult")]
        public class ApplyTacticalMissionResult
        {
            static bool Prefix(GeoMission __instance, out int __state)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.Level;
                    __state = controller.CurrentDifficultyLevel.Order;

                    if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                    {
                        TFTVLogger.Always($"Geoscape difficulty: {controller.CurrentDifficultyLevel.Name.Localize()}, Tactical Difficulty: {GetTacticalDifficulty().Name.Localize()}");
                        controller.CurrentDifficultyLevel = GetTacticalDifficulty();
                        
                        //  TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
                    }

                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            static void Postfix(GeoMission __instance, int __state)

            {
                try
                {
                    GeoLevelController controller = __instance.Level;
                    controller.CurrentDifficultyLevel = GetDifficultyFromOrder(__state);
                    TFTVPureAndForsaken.CheckMissionVSPure(__instance);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }




        }

        [HarmonyPatch(typeof(GeoMission), "PrepareTacticalGame")]
        public class PrepareTacticalGame
        {
            static bool Prefix(GeoMission __instance, out int __state)
            {

                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = __instance.Level;
                    __state = controller.CurrentDifficultyLevel.Order;

                    if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                    {
                        TFTVLogger.Always($"Geoscape difficulty: {controller.CurrentDifficultyLevel.Name.Localize()}, Tactical Difficulty: {GetTacticalDifficulty().Name.Localize()}");
                        controller.CurrentDifficultyLevel = GetTacticalDifficulty();
                        //  TFTVLogger.Always($"Checking that difficulty order level did not change: {__state}");
                    }

                  //  TFTVBaseDefenseGeoscape.ImplementSecurityStation(__instance.Site);

                    return true;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            static void Postfix(GeoMission __instance, int __state)
            {
                try
                {
                    GeoLevelController controller = __instance.Level;
                    controller.CurrentDifficultyLevel = GetDifficultyFromOrder(__state);

                  // TFTVBaseDefenseGeoscape.CleanUpSecurityStation(__instance.Site);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static GameDifficultyLevelDef GetDifficultyFromOrder(int order)
        {
            try
            {
                switch (order)
                {
                    case 1:

                        return DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");

                    case 2:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                    case 3:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");

                    case 4:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");

                    case 5:

                        return DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

                    case 6:

                        return DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");

                }

                return null;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static GameDifficultyLevelDef GetTacticalDifficulty()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                {
                    //  TFTVLogger.Always($"Difficulty level on Tactical is {config.difficultyOnTactical}");

                    switch (config.difficultyOnTactical)
                    {
                        case TFTVConfig.DifficultyOnTactical.STORY:
                            return DefCache.GetDef<GameDifficultyLevelDef>("StoryMode_DifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.ROOKIE:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.VETERAN:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.HERO:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.LEGEND:
                            return DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

                        case TFTVConfig.DifficultyOnTactical.ETERMES:
                            return DefCache.GetDef<GameDifficultyLevelDef>("Etermes_DifficultyLevelDef");
                    }
                }

                return null;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// This function is typically used for difficulty scaling and I reduced to 5 factors from 6 to avoid having something doubled from Story to Rookie,
        /// and because 5 is already a very big number, especially when something is multiplied by it.
        /// [and on this I take off this Math professor hat that I found somewhere]
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>

        public static int DifficultyOrderConverter(int order)
        {
            try
            {

                TFTVConfig config = TFTVMain.Main.Config;

                TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (tacticalLevelController != null && config.difficultyOnTactical != TFTVConfig.DifficultyOnTactical.GEOSCAPE)
                {
                    switch (config.difficultyOnTactical)
                    {
                        case TFTVConfig.DifficultyOnTactical.STORY:
                            return 1;

                        case TFTVConfig.DifficultyOnTactical.ROOKIE:
                            return 1;

                        case TFTVConfig.DifficultyOnTactical.VETERAN:
                            return 2;

                        case TFTVConfig.DifficultyOnTactical.HERO:
                            return 3;

                        case TFTVConfig.DifficultyOnTactical.LEGEND:
                            return 4;

                        case TFTVConfig.DifficultyOnTactical.ETERMES:
                            return 5;
                    }
                }

                if (order >= 2)
                {
                    return order - 1;
                }


                return order;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal class OnGeoscape
        {

            //Adjust diplo and resource reward from events based on Special Difficulties and VO2 & VO8 
            [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GenerateFactionReward")]

            public static class TFTV_GeoEventChoiceOutcome_GenerateFactionReward_SpecialDifficultiesAndVO2AndVO8_patch
            {
                private static readonly List<string> ExcludedEventsDiplomacyPenalty = new List<string>
        {
            "PROG_PU4_WIN", "PROG_SY7", "PROG_SY8", "PROG_AN3", "PROG_AN5", "PROG_NJ7", "PROG_NJ8",
        "PROG_AN2", "PROG_NJ1", "PROG_SY1", "PROG_AN4", "PROG_NJ2", "PROG_SY3_WIN", "PROG_AN6", "PROG_NJ3", "PROG_SY4_T", "PROG_SY4_P",
        "PROG_PU2_CHOICE3EVENT","PROG_PU4_WIN","PROG_PU2_CHOICE2EVENT","PROG_PU12WIN2", "PROG_PU12NewNJOption","PROG_LE1_WIN",
        "Anu_Pissed1", "Anu_Pissed2", "NJ_Pissed1", "NJ_Pissed2", "PROG_LE0_WIN"
        };

                private static List<OutcomeDiplomacyChange> outcomeDiplomacyChanges = new List<OutcomeDiplomacyChange>();


                private static readonly GeoFactionDef PhoenixFaction = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");

                public static void Prefix(GeoEventChoiceOutcome __instance, string eventID, out ResourcePack __state)
                {
                    outcomeDiplomacyChanges = new List<OutcomeDiplomacyChange>(__instance.Diplomacy);
                    __state = new ResourcePack(__instance.Resources);

                    try
                    {


                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        // TFTVConfig config = TFTVMain.Main.Config;

                        if (TFTVNewGameOptions.DiplomaticPenaltiesSetting)
                        {
                            if (!ExcludedEventsDiplomacyPenalty.Contains(eventID) && __instance.Diplomacy.Count > 0)
                            {
                                for (int i = 0; i < __instance.Diplomacy.Count; i++)
                                {
                                    if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value <= 0)
                                    {
                                        TFTVLogger.Always($"Harder diplomacy. The event is {eventID} Original diplo penalty is {__instance.Diplomacy[i].Value}. New diplomacy value is {__instance.Diplomacy[i].Value * 2}");
                                        OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                        diplomacyChange.Value *= 2;
                                        __instance.Diplomacy[i] = diplomacyChange;

                                    }
                                }
                            }
                        }

                        if (CheckGeoscapeSpecialDifficultySettings(controller) == 1)
                        {
                            if (__instance.Diplomacy.Count > 0)
                            {
                                for (int i = 0; i < __instance.Diplomacy.Count; i++)
                                {
                                    if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value >= 0)
                                    {
                                        TFTVLogger.Always($"Apply Easy Geoscape diplomacy. The event is {eventID} Original diplo penalty is {__instance.Diplomacy[i].Value}. New diplomacy value is {__instance.Diplomacy[i].Value * 2}");
                                        OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                        diplomacyChange.Value *= 2;
                                        __instance.Diplomacy[i] = diplomacyChange;

                                    }
                                }
                            }
                        }

                        /*     else if (CheckGeoscapeSpecialDifficultySettings(controller) == 2)
                             {
                                 if (__instance.Diplomacy.Count > 0)
                                 {
                                     for (int i = 0; i < __instance.Diplomacy.Count; i++)
                                     {
                                         if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value >= 0)
                                         {
                                             TFTVLogger.Always("Applying Etermes difficulty. The event is " + eventID + ". Original diplo reward is " + __instance.Diplomacy[i].Value +
                                                 ". New diplomacy value is " + __instance.Diplomacy[i].Value / 2);
                                             OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[i];
                                             diplomacyChange.Value /= 2;
                                             __instance.Diplomacy[i] = diplomacyChange;

                                         }
                                     }
                                 }
                             }*/



                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(2))
                        {
                            if (__instance.Diplomacy.Count > 0)
                            {
                                for (int t = 0; t < __instance.Diplomacy.Count; t++)
                                {
                                    if (__instance.Diplomacy[t].Value != 0)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                        TFTVLogger.Always($"VO#2. Original value was {diplomacyChange.Value}. New value is {diplomacyChange.Value * 0.5f}");
                                        diplomacyChange.Value = Mathf.CeilToInt(diplomacyChange.Value * 0.5f);
                                        __instance.Diplomacy[t] = diplomacyChange;

                                    }
                                }
                            }
                        }
                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(8))
                        {
                            if (__instance.Diplomacy.Count > 0)
                            {
                                for (int t = 0; t < __instance.Diplomacy.Count; t++)
                                {
                                    if (__instance.Diplomacy[t].Value <= 0 && __instance.Diplomacy[t].TargetFaction != PhoenixFaction)
                                    {
                                        OutcomeDiplomacyChange diplomacyChange = __instance.Diplomacy[t];
                                        TFTVLogger.Always($"VO#8. Original value was {diplomacyChange.Value} New value is {diplomacyChange.Value * 1.5f}");
                                        diplomacyChange.Value = Mathf.RoundToInt(diplomacyChange.Value * 1.5f);
                                        __instance.Diplomacy[t] = diplomacyChange;

                                    }
                                }
                            }
                        }


                        for (int i = 0; i < __instance.Resources.Count; i++)
                        {
                            ResourceUnit resources = __instance.Resources[i];
                            TFTVLogger.Always($"Setting to resource amount to Pre_Azozoth_base, from {__instance.Resources[i].Value} to {__instance.Resources[i].Value * 0.8f}");
                            resources.Value *= 0.8f;
                            __instance.Resources[i] = resources;

                        }

                        if (TFTVNewGameOptions.ResourceMultiplierSetting != 1)
                        {
                            for (int i = 0; i < __instance.Resources.Count; i++)
                            {
                                ResourceUnit resources = __instance.Resources[i];
                                TFTVLogger.Always($"Resource Multiplier changing resource reward from {__instance.Resources[i].Value} to {__instance.Resources[i].Value * TFTVNewGameOptions.ResourceMultiplierSetting}");
                                resources.Value *= TFTVNewGameOptions.ResourceMultiplierSetting;
                                __instance.Resources[i] = resources;

                            }
                        }
                        /*   else if (CheckGeoscapeSpecialDifficultySettings(controller) == 1)
                           {
                               for (int i = 0; i < __instance.Resources.Count; i++)
                               {
                                   ResourceUnit resources = __instance.Resources[i];
                                   TFTVLogger.Always($"Applying Easy difficulty. Resource Multiplier changing resource reward from {__instance.Resources[i].Value} to {__instance.Resources[i].Value * 2f}");
                                   resources.Value *= 2f;
                                   __instance.Resources[i] = resources;

                               }

                           }
                           else if (CheckGeoscapeSpecialDifficultySettings(controller) == 2)
                           {
                               for (int i = 0; i < __instance.Resources.Count; i++)
                               {
                                   ResourceUnit resources = __instance.Resources[i];
                                   TFTVLogger.Always($"Applying Etermes difficulty. Resource Multiplier changing resource reward from {__instance.Resources[i].Value} to {__instance.Resources[i].Value * 0.5f}");
                                   resources.Value *= 0.5f;
                                   __instance.Resources[i] = resources;

                               }

                           }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void Postfix(GeoEventChoiceOutcome __instance, string eventID, in ResourcePack __state)
                {
                    try
                    {
                        __instance.Diplomacy = new List<OutcomeDiplomacyChange>(outcomeDiplomacyChanges);

                        __instance.Resources = __state;

                        TFTVConfig config = TFTVMain.Main.Config;
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();


                        for (int i = 0; i < __instance.Diplomacy.Count; i++)
                        {
                            if (__instance.Diplomacy[i].TargetFaction == PhoenixFaction && __instance.Diplomacy[i].Value <= 0)
                            {
                                TFTVLogger.Always($"The event is {eventID},  diplo penalty now  {__instance.Diplomacy[i].Value}.");

                            }
                        }

                        for (int i = 0; i < __instance.Resources.Count; i++)
                        {

                            TFTVLogger.Always($"Resource value now {__instance.Resources[i].Value}");


                        }

                        outcomeDiplomacyChanges.Clear();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

            }



            //These patches modify resource rewards on special difficulties and for haven defenses when the VO18 is in play
            //Also reduces to 25% Mutagen reward from infested havens on ETERMES
            [HarmonyPatch(typeof(ResourceMissionOutcomeDef), "FillPotentialReward")]
            public static class TFTV_ResourceMissionOutcomeDef_FillPotentialReward_SpecialDifficultiesAndVO18_Patch

            {
                public static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription, ResourceMissionOutcomeDef __instance)
                {
                    try
                    {
                        MissionTagDef havenDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");

                        MissionTagDef infestedHavenTag = DefCache.GetDef<MissionTagDef>("HavenInfestation_MissionTypeTagDef");
                        GeoLevelController controller = mission.Site.GeoLevel;

                        if (mission.MissionDef.Tags.Contains(infestedHavenTag) && CheckGeoscapeSpecialDifficultySettings(controller) == 2)
                        {

                            ResourcePack resources = new ResourcePack(__instance.Resources);
                            float multiplier = 0.25f;


                            for (int i = 0; i < __instance.Resources.Count(); i++)
                            {
                                ResourceUnit resourceUnit = __instance.Resources[i];
                                resources[i] = new ResourceUnit(resourceUnit.Type, resourceUnit.Value * multiplier);
                            }

                            rewardDescription.Resources.Clear();
                            rewardDescription.Resources.AddRange(resources);
                            TFTVLogger.Always($"Resource reward from mission {mission.MissionName.LocalizeEnglish()} modified to {resources[0].Value}");
                        }


                        if (mission.MissionDef.Tags.Contains(havenDefenseTag))
                        {
                            ResourcePack resources = new ResourcePack(__instance.Resources);
                            float multiplier = TFTVNewGameOptions.ResourceMultiplierSetting;

                            if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(18) && __instance.name.Contains("Haven"))
                            {
                                multiplier *= 0.5f;
                            }


                            for (int i = 0; i < __instance.Resources.Count(); i++)
                            {
                                ResourceUnit resourceUnit = __instance.Resources[i];
                                resources[i] = new ResourceUnit(resourceUnit.Type, resourceUnit.Value * multiplier);
                            }

                            rewardDescription.Resources.Clear();
                            rewardDescription.Resources.AddRange(resources);
                            TFTVLogger.Always($"Resource reward from mission {mission.MissionName.LocalizeEnglish()} modified to {resources[0].Value}, {resources[1].Value} and {resources[2].Value}");

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            [HarmonyPatch(typeof(ResourceMissionOutcomeDef), "ApplyOutcome")]
            public static class TFTV_ResourceMissionOutcomeDef_ApplyOutcome_SpecialDifficultiesAndVO18_Patch
            {
                public static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription, ResourceMissionOutcomeDef __instance)
                {
                    try
                    {
                        MissionTagDef havenDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypeHavenDefense_MissionTagDef");
                        MissionTagDef proteanMutaneTag = DefCache.GetDef<MissionTagDef>("EnvAncientProteanMutane_MissionTagDef");
                        MissionTagDef livingCrystalTag = DefCache.GetDef<MissionTagDef>("EnvAncientLivingCrystal_MissionTagDef");
                        MissionTagDef orichalcumTag = DefCache.GetDef<MissionTagDef>("EnvAncientOrichalcum_MissionTagDef");
                        GeoLevelController controller = mission.Level;
                        List<MissionTagDef> list = new List<MissionTagDef>() { proteanMutaneTag, livingCrystalTag, orichalcumTag };

                        if (mission.MissionDef.Tags.Contains(havenDefenseTag))
                        {

                            ResourcePack resources = new ResourcePack(__instance.Resources);
                            float multiplier = TFTVNewGameOptions.ResourceMultiplierSetting;

                            if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(18) && __instance.name.Contains("Haven"))
                            {
                                multiplier *= 0.5f;
                            }


                            for (int i = 0; i < __instance.Resources.Count(); i++)
                            {
                                ResourceUnit resourceUnit = __instance.Resources[i];
                                resources[i] = new ResourceUnit(resourceUnit.Type, resourceUnit.Value * multiplier);

                            }
                            rewardDescription.Resources.Clear();
                            rewardDescription.Resources.AddRange(resources);
                            TFTVLogger.Always("Resource reward from mission " + mission.MissionName.LocalizeEnglish() + " modified to "
                               + resources[0].Value + ", " + resources[1].Value + " and " + resources[2].Value);

                        }


                        TFTVConfig config = TFTVMain.Main.Config;

                        float ResourceMultiplier = TFTVNewGameOptions.AmountOfExoticResourcesSetting;


                        float amountLivingCrystal = 150 * ResourceMultiplier;
                        float amountOrichalcum = 125 * ResourceMultiplier;
                        float amountProtean = 125 * ResourceMultiplier;

                        foreach (MissionTagDef tag in list)
                        {
                            if (mission.MissionDef.Tags.Contains(tag))
                            {
                                if (tag.Equals(orichalcumTag))
                                {
                                    ResourcePack resources = new ResourcePack(__instance.Resources);

                                    if (resources.Count > 0)
                                    {
                                        resources.Clear();
                                        resources.Add(new ResourceUnit(ResourceType.Orichalcum, amountOrichalcum));

                                    }
                                    else
                                    {
                                        resources.Add(new ResourceUnit(ResourceType.Orichalcum, amountOrichalcum));

                                    }
                                    rewardDescription.Resources.Clear();
                                    rewardDescription.Resources.AddRange(resources);

                                }
                                else if (tag.Equals(livingCrystalTag))
                                {
                                    ResourcePack resources = new ResourcePack(__instance.Resources);

                                    if (resources.Count > 0)
                                    {
                                        resources.Clear();
                                        resources.Add(new ResourceUnit(ResourceType.LivingCrystals, amountLivingCrystal));

                                    }
                                    else
                                    {
                                        resources.Add(new ResourceUnit(ResourceType.LivingCrystals, amountLivingCrystal));

                                    }
                                    rewardDescription.Resources.Clear();
                                    rewardDescription.Resources.AddRange(resources);

                                }
                                else if (tag.Equals(proteanMutaneTag))
                                {
                                    ResourcePack resources = new ResourcePack(__instance.Resources);

                                    if (resources.Count > 0)
                                    {
                                        resources.Clear();
                                        resources.Add(new ResourceUnit(ResourceType.ProteanMutane, amountProtean));

                                    }
                                    else
                                    {
                                        resources.Add(new ResourceUnit(ResourceType.ProteanMutane, amountProtean));

                                    }
                                    rewardDescription.Resources.Clear();
                                    rewardDescription.Resources.AddRange(resources);

                                }
                            }

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            //Patches to modify diplo rewards based on special difficulties or VO#2
            [HarmonyPatch(typeof(DiplomacyMissionOutcomeDef), "FillPotentialReward")]

            public static class TFTV_DiplomacyMissionOutcomeDef_FillPotentialReward_SpecialDifficultiesAndVO2_Patch

            {
                public static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription, DiplomacyMissionOutcomeDef __instance)
                {
                    try
                    {
                        GeoLevelController geoLevel = mission.Site.GeoLevel;


                        //This doubles diplomacy rewards when they are positive and halves them when they are negative when playing on Rookie, on Mission preview
                        if (CheckGeoscapeSpecialDifficultySettings(geoLevel) == 1)
                        {
                            if (__instance.DiplomacyToFaction.Min > 0)
                            {
                                GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.RandomValue() * 2f));
                                TFTVLogger.Always($"In preview, applying Easy Geoscape settings. Original diplo reward from mission {mission?.MissionDef?.name} was at the least" +
                                    $"{__instance.DiplomacyToFaction.Min} now it is at the least {__instance.DiplomacyToFaction.Min * 2f}");
                            }
                            else
                            {
                                GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.RandomValue() * 0.5f));
                                TFTVLogger.Always($"In preview, applying Easy Geoscape settings. Original diplo reward from mission {mission?.MissionDef?.name} was at the least " +
                                    $"{__instance.DiplomacyToFaction.Min} now it is at the least {__instance.DiplomacyToFaction.Min * 0.5f}");
                            }
                        }

                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(geoLevel).Contains(2))
                        {
                            GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                            GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                            rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.Min * 0.5f));
                            TFTVLogger.Always($"In preview, applying VO2. Original diplo reward from mission {mission?.MissionDef?.name} was " +
                                $"{__instance.DiplomacyToFaction.Min}; now it is {__instance.DiplomacyToFaction.Min * 0.5f}");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            [HarmonyPatch(typeof(DiplomacyMissionOutcomeDef), "ApplyOutcome")]

            public static class TFTV_DiplomacyMissionOutcomeDef_ApplyOutcome_SpecialDifficultiesAndVO2_Patch

            {
                public static void Postfix(GeoMission mission, ref MissionRewardDescription rewardDescription, DiplomacyMissionOutcomeDef __instance)
                {
                    try
                    {
                        GeoLevelController geoLevel = mission.Site.GeoLevel;


                        //This doubles diplomacy rewards when they are positive and halves them when they are negative when playing on Rookie
                        if (CheckGeoscapeSpecialDifficultySettings(geoLevel) == 1)
                        {
                            if (__instance.DiplomacyToFaction.Min > 0)
                            {
                                GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.RandomValue() * 2f));
                                TFTVLogger.Always("Applying Easy Geoscape settings. Original diplo reward from mission " + mission.MissionName.LocalizeEnglish() + " was at the least " + __instance.DiplomacyToFaction.Min
                                   + "; now it is at the least  " + __instance.DiplomacyToFaction.Min * 2f);
                            }
                            else
                            {
                                GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.Min * 0.5f));
                                TFTVLogger.Always("Applying Easy Geoscape settings. Original diplo reward from mission " + mission.MissionName.LocalizeEnglish() + " was at the least " + __instance.DiplomacyToFaction.Min
                                   + "; now it is at the least  " + __instance.DiplomacyToFaction.Min * 0.5f);
                            }
                        }
                        //removing the /2 on Etermes
                        /*   if (CheckGeoscapeSpecialDifficultySettings(geoLevel) == 2)
                           {
                               if (__instance.DiplomacyToFaction.Min > 0)
                               {
                                   GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                   GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                   rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.RandomValue() * 0.5f));
                                   TFTVLogger.Always("Applying Etermes settings. Original diplo reward from mission " + mission.MissionName.LocalizeEnglish() + " was at the least " + __instance.DiplomacyToFaction.Min
                                      + "; now it is at the least  " + __instance.DiplomacyToFaction.Min * 0.5f);
                               }
                               else
                               {
                                   GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                                   GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                                   rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.Min * 2f));
                                   TFTVLogger.Always("Applying Etermes settings. Original diplo reward from mission " + mission.MissionName.LocalizeEnglish() + " was at the least " + __instance.DiplomacyToFaction.Min
                                      + "; now it is at the least  " + __instance.DiplomacyToFaction.Min * 2f);
                               }
                           }*/

                        if (TFTVVoidOmens.CheckFordVoidOmensInPlay(geoLevel).Contains(2))
                        {
                            GeoFaction viewerFaction = mission.Site.GeoLevel.ViewerFaction;
                            GeoFaction faction = geoLevel.GetFaction(__instance.ToFaction);
                            rewardDescription.SetDiplomacyChange(faction, viewerFaction, Mathf.RoundToInt(__instance.DiplomacyToFaction.RandomValue() * 0.5f));
                            TFTVLogger.Always("Apply VO2. Original diplo reward from mission " + mission.MissionName.LocalizeEnglish() + " was at the least " + __instance.DiplomacyToFaction.Min
                                + "; now it is at the least  " + __instance.DiplomacyToFaction.Min * 0.5f);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            //For air missions
            [HarmonyPatch(typeof(GeoAirMission), "SetOutcomeAndReward")]
            public static class TFTV_GeoAirMission_SetOutcomeAndReward_SpecialDifficultiesAndVO2_Patch
            {
                public static void Prefix(GeoFactionReward reward)
                {
                    try
                    {
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();



                        if (reward.Resources != null && reward.Resources.Count > 0)
                        {
                            float multiplier = TFTVNewGameOptions.ResourceMultiplierSetting;

                            //  TFTVLogger.Always("Resource amount is " + reward.Resources[0].Value);
                            reward.Resources = new ResourcePack
                            { new ResourceUnit{

                                Type = reward.Resources[0].Type, Value = reward.Resources[0].Value * multiplier}

                            };
                            TFTVLogger.Always($"Applying multiplier of {multiplier}. Reward now {reward.Resources[0].Value}, from {reward.Resources[0].Value / multiplier}");
                        }

                        if (reward.Diplomacy != null && reward.Diplomacy.Count > 0)
                        {
                            float multiplier = 1f;
                            string difficulty = "";

                            if (CheckGeoscapeSpecialDifficultySettings(controller) == 1)
                            {

                                multiplier *= 2;
                                difficulty = "easy";

                            }

                            if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(2))
                            {

                                multiplier *= 0.5f;
                                difficulty += " VO2";

                            }
                            foreach (RewardDiplomacyChange change in reward.Diplomacy)
                            {
                                //  TFTVLogger.Always("Diplo reward is " + change.Value);
                                change.Value = Mathf.RoundToInt(multiplier * change.Value);
                                TFTVLogger.Always($"Applying {difficulty}. Diplo reward now {change.Value} from {change.Value / multiplier}");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }

        internal class OnTactical
        {
            /// <summary>
            /// Reinforcements will come with full AP on ETERMES!
            /// </summary>
            [HarmonyPatch(typeof(TacParticipantSpawn), "AdjustSpawned")]
            public static class TFTV_TacParticipantSpawn_AdjustSpawned_Patch
            {
                public static bool Prefix(TacParticipantSpawn __instance, TacticalLevelController tacticalLevel)
                {
                    try
                    {
                        
                        CounterSpawned++;

                        if (CheckTacticalSpecialDifficultySettings(tacticalLevel)==2 && CounterSpawned < 2)
                        {
                            TFTVLogger.Always($"first spawn on Etermes keeps full AP");
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


            //This method checks game difficulty and config options 
            //If playing on Rookie and difficulty override option not selected, easy tactical will apply
            //IF playing I AM ETERMES, extra difficulty will apply
            //Easy difficulty adds a special perk to all Phoenix Operatives that reduces damage from projecticle, fire, poison, paralysis, virus, acid  by 50%
            //and a special perk that makes all enemies more vulnerable to those damages, also by 50%
            //Etermes difficulty adds the same perks in reverse, and with a 25% 
            //Doesn't apply during Tutorial


            public static void AddSpecialDifficultiesBuffsAndVulnerabilities(TacticalActorBase actor, TacticalLevelController controller)
            {
                try
                {
                    if (!controller.TacMission.MissionData.MissionType.name.Contains("Tutorial"))
                    {
                        TFTVConfig config = TFTVMain.Main.Config;
                        DamageMultiplierStatusDef protectionStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RookieProtectionStatus");
                        DamageMultiplierStatusDef vulnerabilityStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RookieVulnerabilityStatus");
                        DamageMultiplierStatusDef protectionEtermesStatus = DefCache.GetDef<DamageMultiplierStatusDef>("EtermesProtectionStatus");
                        DamageMultiplierStatusDef vulnerabilityEtermesStatus = DefCache.GetDef<DamageMultiplierStatusDef>("EtermesVulnerabilityStatus");
                        DamageMultiplierStatusDef scyllaDamageResistance = DefCache.GetDef<DamageMultiplierStatusDef>("ScyllaDamageResistance");
                        ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                        if (CheckTacticalSpecialDifficultySettings(controller) == 1)
                        {
                            if (controller.GetFactionByCommandName("PX") != null)
                            {
                                TacticalFaction phoenixFaction = controller.GetFactionByCommandName("PX");

                                if (actor is TacticalActor tacticalActor && tacticalActor.TacticalFaction.GetRelationTo(phoenixFaction) == FactionRelation.Enemy)
                                {
                                    if (tacticalActor.IsActive && !tacticalActor.HasStatus(vulnerabilityStatus))
                                    {
                                        tacticalActor.Status.ApplyStatus(vulnerabilityStatus);
                                    }
                                }

                                if (actor is TacticalActor phoenixActor && phoenixActor.TacticalFaction == phoenixFaction)
                                {
                                    if (phoenixActor.IsActive && !phoenixActor.HasStatus(protectionStatus))
                                    {
                                        phoenixActor.Status.ApplyStatus(protectionStatus);
                                    }
                                }

                            }
                        }

                        if (TFTVNewGameOptions.EtermesResistanceAndVulnerability==1)
                        {
                            if (controller.GetFactionByCommandName("PX") != null)
                            {
                                TacticalFaction phoenixFaction = controller.GetFactionByCommandName("PX");

                                if (actor is TacticalActor tacticalActor && tacticalActor.TacticalFaction.GetRelationTo(phoenixFaction) == FactionRelation.Enemy)
                                {
                                    if (tacticalActor.IsActive && !tacticalActor.HasStatus(protectionEtermesStatus) && !tacticalActor.HasGameTag(cyclopsTag) 
                                        && !tacticalActor.HasGameTag(TFTVBaseDefenseTactical.Defs.SecurityGuardTag))
                                    {
                                        tacticalActor.Status.ApplyStatus(protectionEtermesStatus);
                                    }
                                }

                                if (actor is TacticalActor phoenixActor && phoenixActor.TacticalFaction == phoenixFaction)
                                {
                                    if (phoenixActor.IsActive && !phoenixActor.HasStatus(vulnerabilityEtermesStatus) && !phoenixActor.HasGameTag(TFTVBaseDefenseTactical.Defs.SecurityGuardTag))
                                    {
                                        phoenixActor.Status.ApplyStatus(vulnerabilityEtermesStatus);
                                    }
                                }

                            }
                        }
                        if (TFTVNewGameOptions.StrongerPandoransSetting && scyllaDamageResistance != null)
                        {
                            if (actor.ActorDef.name.Equals("Queen_ActorDef") && !actor.Status.HasStatus(scyllaDamageResistance))
                            {
                                actor.Status.ApplyStatus(scyllaDamageResistance);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class DefModifying
        {
            public static void CheckForSpecialDifficulties()
            {
                try
                {
                    SaveData();
                    ReducePandoranArmor();
                    NerfImpossibleWeapons();
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            //This method saves the relevant data from the defs before they are modified. 
            //This is needed in case player starts a new gamel/loads a game with a different difficulty setting
            public static void SaveData()
            {
                try 
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    GeoLevelController controllerGeo = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                    TacticalLevelController controllerTactical = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                    if ((controllerGeo != null && CheckOnGeoscapeSpecialDifficultySettingsForTactical(controllerGeo) == 1)
                        || (controllerTactical != null && CheckTacticalSpecialDifficultySettings(controllerTactical) == 1))
                    {

                        if (!DataForRookieSaved)
                        {
                            GameTagDef alienTag = DefCache.GetDef<GameTagDef>("Alien_RaceTagDef");

                            foreach (TacticalItemDef itemDef in Repo.GetAllDefs<TacticalItemDef>().Where(tif => tif.Tags.Contains(alienTag)))
                            {
                                if (itemDef.Armor > 20)
                                {
                                  //  TFTVLogger.Always(itemDef.name + " has " + itemDef.Armor + " armor");
                                    AlienBodyPartsDictionary.Add(itemDef.name, (int)itemDef.Armor);
                                    AlienBodyParts.Add(itemDef);
                                }
                            }
                            DataForRookieSaved = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


            public static void ReducePandoranArmor()
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controllerGeo = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (DataForRookieSaved)
                    {
                        foreach (TacticalItemDef itemDef1 in AlienBodyParts)
                        {
                            if ((controllerGeo != null && CheckOnGeoscapeSpecialDifficultySettingsForTactical(controllerGeo) == 1)
                        || (controllerTactical != null && CheckTacticalSpecialDifficultySettings(controllerTactical) == 1))
                            {
                                itemDef1.Armor = 20;

                            }
                            else
                            {
                                itemDef1.Armor = AlienBodyPartsDictionary[itemDef1.name];

                            }
                        }

                        BodyPartAspectDef CorruptionNode_Body_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [CorruptionNode_Body_BodyPartDef]");
                        BodyPartAspectDef Queen_Abdomen_Belcher_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Abdomen_Belcher_BodyPartDef]");
                        BodyPartAspectDef Queen_Abdomen_Spawner_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Abdomen_Spawner_BodyPartDef]");
                        BodyPartAspectDef Queen_Carapace_Heavy_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Carapace_Heavy_BodyPartDef]");
                        BodyPartAspectDef Queen_Torso_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Torso_BodyPartDef]");
                        BodyPartAspectDef MediumGuardian_Torso_LivingCrystal_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [MediumGuardian_Torso_LivingCrystal_BodyPartDef]");
                        BodyPartAspectDef MediumGuardian_Torso_Orichalcum_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [MediumGuardian_Torso_Orichalcum_BodyPartDef]");
                        BodyPartAspectDef MediumGuardian_Torso_ProteanMutane_BodyPartDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [MediumGuardian_Torso_ProteanMutane_BodyPartDef]");

                        List<BodyPartAspectDef> alienBodyPartAspects = new List<BodyPartAspectDef>()
                { CorruptionNode_Body_BodyPartDef, Queen_Abdomen_Belcher_BodyPartDef, Queen_Abdomen_Spawner_BodyPartDef,Queen_Carapace_Heavy_BodyPartDef, Queen_Torso_BodyPartDef,
                MediumGuardian_Torso_LivingCrystal_BodyPartDef, MediumGuardian_Torso_Orichalcum_BodyPartDef, MediumGuardian_Torso_ProteanMutane_BodyPartDef};


                        if ((controllerGeo != null && CheckOnGeoscapeSpecialDifficultySettingsForTactical(controllerGeo) == 1)
                        || (controllerTactical != null && CheckTacticalSpecialDifficultySettings(controllerTactical) == 1))
                        {
                            foreach (BodyPartAspectDef bodyPartAspect in alienBodyPartAspects)
                            {
                                bodyPartAspect.Endurance = 30;
                            }
                        }
                        else
                        {
                            CorruptionNode_Body_BodyPartDef.Endurance = 120;
                            Queen_Abdomen_Belcher_BodyPartDef.Endurance = 60;
                            Queen_Abdomen_Spawner_BodyPartDef.Endurance = 60;
                            Queen_Carapace_Heavy_BodyPartDef.Endurance = 50;
                            Queen_Torso_BodyPartDef.Endurance = 100;
                            MediumGuardian_Torso_LivingCrystal_BodyPartDef.Endurance = 150;
                            MediumGuardian_Torso_Orichalcum_BodyPartDef.Endurance = 150;
                            MediumGuardian_Torso_ProteanMutane_BodyPartDef.Endurance = 150;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static bool ImpossibleWeaponsAdjusted = false;
            public static void NerfImpossibleWeapons()
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    SharedData shared = GameUtl.GameComponent<SharedData>();
                    SharedDamageKeywordsDataDef damageKeywords = shared.SharedDamageKeywords;

                    GeoLevelController controllerGeo = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                 //   TFTVLogger.Always($". tac controller null? {controllerTactical == null}. IW adjustments on? {TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting}");

                    if ((controllerGeo != null && ApplyImpossibleWeaponsAdjustmentsOnGeoscape(controllerGeo) || controllerTactical != null && ApplyImpossibleWeaponsAdjustmentsOnTactical(controllerTactical)) && !ImpossibleWeaponsAdjusted)
                    {
                        foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
                        {
                            switch (weaponDef.Guid)
                            {
                                case "831be08f-d0d7-2764-4833-02ce83ff7277": // AC_Rebuke_WeaponDef
                                                                             // Remove shredding
                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                                    weaponDef.DamagePayload.ArmourShred = 0;
                                    weaponDef.DamagePayload.ArmourShredProbabilityPerc = 0;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.BurningKeyword, Value = 20 });
                                    break;

                                case "1fd630cb-c45f-cf14-8a4e-095ee3c672d1": //AC_ShardGun_WeaponDef

                                    weaponDef.DamagePayload.ProjectilesPerShot = 12;
                                    weaponDef.DamagePayload.DamageKeywords[1].Value = 10;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.PsychicKeyword, Value = 1 });
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_AC_SHOTGUN_NAME";
                                    DefCache.GetDef<ResearchViewElementDef>("PX_ShardGun_ViewElementDef").CompleteText.LocalizationKey = "TFTV_PX_SHARDGUN_RESEARCHDEF_COMPLETE";
                                    // DefCache.GetDef<ResearchViewElementDef>("ANU_AdvancedInfectionTech_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_ANU_ADVANCEDINFECTIONTECH_RESEARCHDEF_BENEFITS";

                                    break;

                                case "3489e0a7-2d5e-9704-0ada-ae332ebeed49": //AC_Mattock_WeaponDef


                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.ShockKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 110;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.SyphonKeyword, Value = 80 });
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_AC_MACE_NAME";
                                    // DefCache.GetDef<ResearchViewElementDef>("PX_MattockoftheAncients_ViewElementDef").CompleteText.LocalizationKey = "TFTV_PX_MATTOCKOFTHEANCIENTS_RESEARCHDEF_COMPLETE";

                                    break;
                                case "2cd06c4b-f1f5-a9b4-c9ff-afbad25be5d8"://AC_Scorpion_WeaponDef

                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.PiercingKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 140;
                                    weaponDef.DamagePayload.ArmourPiercing = 0;
                                    weaponDef.DamagePayload.StopOnFirstHit = false;
                                    weaponDef.DamagePayload.StopWhenNoRemainingDamage = false;
                                    // weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShreddingKeyword, Value = 10 });
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_AC_SNIPER_NAME";
                                    DefCache.GetDef<ResearchViewElementDef>("PX_Scorpion_ViewElementDef").CompleteText.LocalizationKey = "TFTV_PX_SCORPION_RESEARCHDEF_COMPLETE";
                                    // DefCache.GetDef<ResearchViewElementDef>("NJ_VehicleTech_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_NJ_VEHICLETECH_RESEARCHDEF_BENEFITS";
                                    break;
                                case "4d14021e-a8ce-7444-3a19-6f3dc9c44f8a"://AC_Scyther_WeaponDef

                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 180;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.BleedingKeyword, Value = 60 });
                                    weaponDef.DamagePayload.ArmourShred = 0;
                                    weaponDef.DamagePayload.ArmourShredProbabilityPerc = 0;
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_AC_SCYTHE_NAME";
                                    DefCache.GetDef<ResearchViewElementDef>("PX_Scyther_ViewElementDef").CompleteText.LocalizationKey = "TFTV_PX_SCYTHER_RESEARCHDEF_COMPLETE";
                                    //  DefCache.GetDef<ResearchViewElementDef>("SYN_Bionics3_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_SYN_BIONICS3_RESEARCHDEF_BENEFITS";
                                    break;
                            }
                            ImpossibleWeaponsAdjusted = true;
                        }

                    }
                    else if ((controllerGeo != null && !ApplyImpossibleWeaponsAdjustmentsOnGeoscape(controllerGeo) || controllerTactical != null && !ApplyImpossibleWeaponsAdjustmentsOnTactical(controllerTactical)) && ImpossibleWeaponsAdjusted)
                    {
                        foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
                        {

                            switch (weaponDef.Guid)
                            {
                                case "831be08f-d0d7-2764-4833-02ce83ff7277": // AC_Rebuke_WeaponDef
                                                                             // Remove shredding
                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.BurningKeyword);
                                    weaponDef.DamagePayload.ArmourShred = 10;
                                    weaponDef.DamagePayload.ArmourShredProbabilityPerc = 100;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShreddingKeyword, Value = 30 });
                                    break;

                                case "1fd630cb-c45f-cf14-8a4e-095ee3c672d1": //AC_ShardGun_WeaponDef

                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.PsychicKeyword);
                                    weaponDef.DamagePayload.ProjectilesPerShot = 15;
                                    weaponDef.DamagePayload.DamageKeywords[1].Value = 15;
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "KEY_AC_SHOTGUN_NAME";
                                    DefCache.GetDef<ResearchViewElementDef>("PX_ShardGun_ViewElementDef").CompleteText.LocalizationKey = "PX_SHARDGUN_RESEARCHDEF_COMPLETE";
                                    break;

                                case "3489e0a7-2d5e-9704-0ada-ae332ebeed49": //AC_Mattock_WeaponDef


                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.SyphonKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 220;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShockKeyword, Value = 440 });
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "KEY_AC_MACE_NAME";
                                    // DefCache.GetDef<ResearchViewElementDef>("PX_MattockoftheAncients_ViewElementDef").CompleteText.LocalizationKey = "TFTV_PX_MATTOCKOFTHEANCIENTS_RESEARCHDEF_COMPLETE";

                                    break;
                                case "2cd06c4b-f1f5-a9b4-c9ff-afbad25be5d8"://AC_Scorpion_WeaponDef

                                    //  _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 180;
                                    weaponDef.DamagePayload.ArmourPiercing = 50;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.PiercingKeyword, Value = 80 });
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "KEY_AC_SNIPER_NAME";
                                    weaponDef.DamagePayload.StopOnFirstHit = true;
                                    weaponDef.DamagePayload.StopWhenNoRemainingDamage = true;
                                    DefCache.GetDef<ResearchViewElementDef>("PX_Scorpion_ViewElementDef").CompleteText.LocalizationKey = "PX_SCORPION_RESEARCHDEF_COMPLETE";
                                    // DefCache.GetDef<ResearchViewElementDef>("NJ_VehicleTech_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_NJ_VEHICLETECH_RESEARCHDEF_BENEFITS";
                                    break;
                                case "4d14021e-a8ce-7444-3a19-6f3dc9c44f8a"://AC_Scyther_WeaponDef

                                    _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.BleedingKeyword);
                                    weaponDef.DamagePayload.DamageKeywords[0].Value = 300;
                                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShreddingKeyword, Value = 30 });
                                    weaponDef.DamagePayload.ArmourShred = 10;
                                    weaponDef.DamagePayload.ArmourShredProbabilityPerc = 0;
                                    weaponDef.ViewElementDef.DisplayName1.LocalizationKey = "KEY_AC_SCYTHE_NAME";
                                    DefCache.GetDef<ResearchViewElementDef>("PX_Scyther_ViewElementDef").CompleteText.LocalizationKey = "PX_SCYTHER_RESEARCHDEF_COMPLETE";
                                    //  DefCache.GetDef<ResearchViewElementDef>("SYN_Bionics3_ViewElementDef").BenefitsText.LocalizationKey = "TFTV_SYN_BIONICS3_RESEARCHDEF_BENEFITS";
                                    break;
                            }
                            ImpossibleWeaponsAdjusted = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// So, I'm not sure why I made such a mess of difficulties, but here it goes:
        /// 
        /// 1) there are 2 new difficulties, created in TFTVDefsInjectedOnlyOnce.SpecialDifficulties,
        ///
        /// StoryMode_DifficultyLevelDef.Order = 1;
        /// Etermes_DifficultyLevelDef.Order = 6;
        /// 
        /// 2) Each of Vanilla difficulty's Order field is changed (in the same method) so that:
        /// 
        /// DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef").Order = 2;
        /// DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef").Order = 3;           
        /// DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef").Order = 4;
        /// DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef").Order = 5;
        /// 
        /// 3) There is an internal variable to save difficulty. 
        /// 
        /// This is necessary for "fixing" Story Mode and Etermes saves as they are loaded;
        /// because the saves are accessed before the Mod is loaded, when the new difficulties don't "exist" yet,
        /// these saves have no difficulties when they are loaded just after launching the game. 
        /// The intenal variable is used to determine whether the save is Story Mode or Etermes.
        /// This is invoked for both Geoscape and Tactical saves.
        /// 
        /// 4) Tactical difficulty can be changed at any time in Settings. 
        /// 
        ///
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>

        public static int CheckGeoscapeSpecialDifficultySettings(GeoLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;
                if (controller.CurrentDifficultyLevel.Order == 1)
                {
                    return 1;

                }
                else if (controller.CurrentDifficultyLevel.Order == 6)//(config.EtermesMode)
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static int CheckOnGeoscapeSpecialDifficultySettingsForTactical(GeoLevelController controller)
        {
            try
            {
                //  TFTVLogger.Always($"is controller null? {controller == null}");

                TFTVConfig config = TFTVMain.Main.Config;


                //  TFTVLogger.Always($"is config null? {config == null}");

                //   TFTVLogger.Always($"controller.CurrentDifficultyLevel.Order {controller?.CurrentDifficultyLevel?.Order}");

                if (controller.CurrentDifficultyLevel.Order == 1 || GetTacticalDifficulty() != null && GetTacticalDifficulty().Order == 1)
                {
                    // TFTVLogger.Always($"Geoscape check re Tactical returns that it's Rookie!");
                    return 1;
                }
                else if (controller.CurrentDifficultyLevel.Order == 6)//(config.EtermesMode)
                {
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static int CheckTacticalSpecialDifficultySettings(TacticalLevelController controller = null)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (GetTacticalDifficulty() != null)
                {
                   // TFTVLogger.Always($"tactical difficulty not null; {GetTacticalDifficulty().Order}");

                    if (GetTacticalDifficulty().Order == 1)
                    {
                        return 1;
                    }
                    else if (GetTacticalDifficulty().Order == 6)
                    {
                        return 2;
                    }
                }
                else
                {
                    if (TFTVNewGameOptions.InternalDifficultyCheckTactical != 0)
                    {
                        // TFTVLogger.Always($"tactical difficulty is {TFTVNewGameOptions.InternalDifficultyCheckTactical}");

                        if (TFTVNewGameOptions.InternalDifficultyCheckTactical == 1)
                        {
                            return 1;
                        }
                        else if (TFTVNewGameOptions.InternalDifficultyCheckTactical == 6)
                        {
                            return 2;
                        }
                    }
                    else if(controller!=null)
                    {
                        if (controller.Difficulty.Order == 1)
                        {
                            return 1;

                        }
                        else if (controller.Difficulty.Order == 6)
                        {
                            return 2;
                        }
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


        public static bool ApplyImpossibleWeaponsAdjustmentsOnGeoscape(GeoLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting)
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

        public static bool ApplyImpossibleWeaponsAdjustmentsOnTactical(TacticalLevelController controller)
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting)
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
    }
}
