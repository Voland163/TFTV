using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.UI;
using Base.Utils;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Levels.Params;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Common.View.ViewModules.UIModuleModal;

namespace TFTV
{
    internal class TFTVBaseDefenseGeoscape
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static Dictionary<int, Dictionary<string, double>> PhoenixBasesUnderAttack = new Dictionary<int, Dictionary<string, double>>();
        public static Dictionary<int, int> PhoenixBasesUnderAttackSchedule = new Dictionary<int, int>(); //actually not only breaches, but any time other than 18 hours
        public static Dictionary<int, bool> ContainmentBreachSchedule = new Dictionary<int, bool>();
        public static Dictionary<int, List<string>> PandoransThatCanEscape = new Dictionary<int, List<string>>();

        private static readonly SharedData Shared = TFTVMain.Shared;
        public static List<int> PhoenixBasesInfested = new List<int>();
        public static Sprite VoidIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color brightRed = new Color32(255, 0, 0, 255);
        internal static GeoSite KludgeSite = null;

        private static GeoscapeEventDef _underAttackEventDef;
        private static GeoscapeEventDef _purgeContainmentEventDef;
        private static GeoscapeEventDef _containmentBreachEventDef;
        private static GeoscapeEventDef _scyllaIsLooseEventDef;

        internal static KillActorFactionObjectiveDef KillInfestation;
        internal static KillActorFactionObjectiveDef KillScylla;
        internal static KillActorFactionObjectiveDef KillSentinel;
        internal static SurviveTurnsFactionObjectiveDef SurviveFiveTurns;
        internal static SurviveTurnsFactionObjectiveDef SurviveThreeTurns;
        internal static KillActorFactionObjectiveDef ScatterEnemies;

        internal static GameTagDef KillInfestationTag;
        internal static GameTagDef ScatterEnemiesTag;


        internal class Defs
        {
            internal static readonly string Key_ContainmentBaseText = "BASEDEFENSE_PURGE_TEXT";
            internal static readonly string Key_UnderAttackBaseText = "BASEDEFENSE_EVENT_TEXT";

            internal static void CreateNewBaseDefense()
            {
                try
                {
                    ChangeBaseDefense();
                    CreateObjectivesBaseDefense();
                    CreateBaseDefenseEvents();
                    CreateCosmeticExplosion();
                    CreateFireExplosion();
                    CreateFakeFacilityToFixBadBaseDefenseMaps();
                    ReduceDamageFromInfestation();
                    CreateConsolePromptBaseDefense();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void CreateFireExplosion()
            {
                try
                {
                    SpawnTacticalVoxelEffectDef spawnFire = Helper.CreateDefFromClone<SpawnTacticalVoxelEffectDef>(null, "{96C92F1C-CA61-4FB3-8147-809ED0E70108}", "FireVoxelSpawnerEffect");
                    spawnFire.ApplicationConditions = new EffectConditionDef[] { };
                    spawnFire.SpawnDelay = 2f;
                    spawnFire.Radius = 2;
                    spawnFire.TacticalVoxelType = PhoenixPoint.Tactical.Levels.Mist.TacticalVoxelType.Fire;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void CreateCosmeticExplosion()
            {
                try
                {

                    string name = "FakeExplosion_ExplosionEffectDef";
                    string gUIDDelayedEffect = "{82F49470-B14D-4C73-8B91-9D3EEE7CCB44}";
                    DelayedEffectDef sourceDelayedEffect = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");
                    DelayedEffectDef newDelayedEffect = Helper.CreateDefFromClone(sourceDelayedEffect, gUIDDelayedEffect, name);
                    newDelayedEffect.SecondsDelay = 0.0f;

                    string gUIDExplosionEffect = "{8054419B-6410-47A4-8BD5-C2CC5A4B8B62}";
                    ExplosionEffectDef sourceExplosionEffect = DefCache.GetDef<ExplosionEffectDef>("E_ShrapnelExplosion [ExplodingBarrel_ExplosionEffectDef]");
                    ExplosionEffectDef newExplosionEffect = Helper.CreateDefFromClone(sourceExplosionEffect, gUIDExplosionEffect, name);


                    //  SpawnVoxelDamageTypeEffectDef mistDamage = DefCache.GetDef<SpawnVoxelDamageTypeEffectDef>("Goo_SpawnVoxelDamageTypeEffectDef");

                    string gUIDDamageEffect = "{CD3D8BC8-C90D-40A6-BBA3-0FD7FE629F15}";
                    DamageEffectDef sourceDamageEffect = DefCache.GetDef<DamageEffectDef>("E_DamageEffect [ExplodingBarrel_ExplosionEffectDef]");
                    DamageEffectDef newDamageEffect = Helper.CreateDefFromClone(sourceDamageEffect, gUIDDamageEffect, name);
                    newDamageEffect.MinimumDamage = 1;
                    newDamageEffect.MaximumDamage = 1;
                    newDamageEffect.ObjectMultiplier = 100;
                    newDamageEffect.ArmourShred = 0;
                    newDamageEffect.ArmourShredProbabilityPerc = 0;
                    //  newDamageEffect.DamageTypeDef = mistDamage;
                    newExplosionEffect.DamageEffect = newDamageEffect;
                    newDelayedEffect.EffectDef = newExplosionEffect;

                    newDelayedEffect.SecondsDelay = 0.0f;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CreateBaseDefenseEvents()
            {
                try
                {   //BASEDEFENSE_EVENT_TITLE: A Phoenix Project base is under attack!
                    //BASEDEFENSE_EVENT_TEXT: Director! We are under attack! Our automated defenses and bullheads will buy us some time,
                    //but we need to deploy a squad to secure the base as soon as possible.
                    //The longer we take, the more time the enemy will have to achieve its objectives.

                    //+optional:
                    //BASEDEFENSE_CAPTIVE_SIREN_TEXT There is something else.
                    //We suspect that a Siren held in containment at the base was instrumental in directing the Pandorans to the base!
                    //I should have guessed that its telepathic prowess could be a security risk.
                    //I recommend that we enforce a perimeter free of Pandoran colonies of at least 2,000 km
                    //around our bases with containment facilities if we are going to hold specimens with telepathic abilities. 
                    // BASEDEFENSE_CAPTIVE_SCYLLA_TEXT It was the Scylla!The Scylla that we captured has been acting as a beacon to all
                    // the Pandorans within the region. I recommend that we terminate the creature as soon as our researchers are done with it.
                    //BASEDEFENSE_NJWALLS_TEXT	Thankfully the New Jericho fortifications will buy us some extra time.

                    _underAttackEventDef = TFTVCommonMethods.CreateNewEvent("OlenaBaseDefense", "BASEDEFENSE_EVENT_TITLE", Key_UnderAttackBaseText, null);


                    //BASEDEFENSE_PURGE_TITLE: Purge containment?
                    //BASEDEFENSE_PURGE_TEXT: Director, what shall do we with the captured Pandorans held at the base? They might break free during the attack! Do you authorize the purge? Here is the manifest:
                    //BASEDEFENSE_PURGE_CHOICE_O: Purge them!
                    //BASEDEFENSE_PURGE_CHOICE_1: No, they are too valuable. We will risk it.

                    GeoscapeEventDef purgeEvent = TFTVCommonMethods.CreateNewEvent("OlenaPurge", "BASEDEFENSE_PURGE_TITLE", Key_ContainmentBaseText, null);
                    purgeEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BASEDEFENSE_PURGE_CHOICE_O";
                    purgeEvent.GeoscapeEventData.Choices.Add(new GeoEventChoice() { Text = new LocalizedTextBind("BASEDEFENSE_PURGE_CHOICE_1") });
                    _purgeContainmentEventDef = purgeEvent;


                    //CONTAINMENTBREACH_EVENT_TITLE:		CONTAINMENT BREACH!
                    //CONTAINMENTBREACH_EVENT_TEXT:  We are not sure how it happened, but everything that was <i>in</i>, got <i>out</i>.
                    //Director, this base is already almost taken over by the Pandorans!

                    _containmentBreachEventDef = TFTVCommonMethods.CreateNewEvent("OlenaContainmentBreach", "CONTAINMENTBREACH_EVENT_TITLE", "CONTAINMENTBREACH_EVENT_TEXT", null);


                    //LOOSE_SCYLLA_EVENT_TITLE:		SCYLLA ON THE LOOSE!
                    //LOOSE_SCYLLA_EVENT_TEXT:      Director, the Scylla escaped! It is taking over our base! If we don't act fast, it might turn it into a Pandoran colony.

                    _scyllaIsLooseEventDef = TFTVCommonMethods.CreateNewEvent("OlenaLooseScylla", "LOOSE_SCYLLA_EVENT_TITLE", "LOOSE_SCYLLA_EVENT_TEXT", null);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


            private static void ReduceDamageFromInfestation()
            {
                try
                {
                    DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef").FacilityDamageOnBaseAbandoned = new RangeDataInt() { Min = 20, Max = 40 };


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateFakeFacilityToFixBadBaseDefenseMaps()
            {
                try
                {
                    PhoenixFacilityDef storesFacility = DefCache.GetDef<PhoenixFacilityDef>("Stores_PhoenixFacilityDef");

                    string fakeFacilityName = "FakeFacility";

                    PhoenixFacilityDef newFakeFacility = Helper.CreateDefFromClone(storesFacility, "{FC1CF7B3-7355-4E28-BFA2-57B1D5A83576}", fakeFacilityName);
                    newFakeFacility.ViewElementDef = Helper.CreateDefFromClone(storesFacility.ViewElementDef, "{DA2A6489-117C-49D9-BA4F-A01A47A021B2}", fakeFacilityName);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CreateObjectivesBaseDefense()
            {
                try
                {
                    KillActorFactionObjectiveDef killActorFactionObjectiveSource = DefCache.GetDef<KillActorFactionObjectiveDef>("E_KillSentinels [Nest_AlienBase_CustomMissionTypeDef]");

                    string nameObjectiveDestroySpawnery = "PhoenixBaseInfestation";
                    GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                    GameTagDef gameTagMainObjective = Helper.CreateDefFromClone(
                        source,
                        "{B42E4079-EDC6-4E7A-9720-8F8839FCD3CE}",
                        nameObjectiveDestroySpawnery + "_GameTagDef");

                    KillActorFactionObjectiveDef killInfestation = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "5BDA1D39-80A8-4EB8-A34F-92FB08AF2CB5", nameObjectiveDestroySpawnery);
                    killInfestation.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_INFESTATION_OBJECTIVE";
                    killInfestation.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_INFESTATION_OBJECTIVE";
                    killInfestation.KillTargetGameTag = gameTagMainObjective;
                    killInfestation.IsVictoryObjective = false;
                    KillInfestation = killInfestation;

                    KillInfestationTag = gameTagMainObjective;


                    string nameObjectiveKillScylla = "PhoenixBaseKillScylla";
                    KillActorFactionObjectiveDef killScylla = Helper.CreateDefFromClone(killInfestation, "{6B331F65-7F9B-454E-B67B-A313F065615F}", nameObjectiveKillScylla);
                    killScylla.MissionObjectiveData.Description.LocalizationKey = "KEY_OBJECTIVE_KILL_QUEEN";
                    killScylla.MissionObjectiveData.Summary.LocalizationKey = "KEY_OBJECTIVE_KILL_QUEEN";
                    KillScylla = killScylla;

                    string nameObjectiveDestroySentinel = "PhoenixBaseDestroySentinel";

                    KillActorFactionObjectiveDef killSentinel = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "{97745084-836A-4D5C-A1F5-052EDEC307A5}", nameObjectiveDestroySentinel);
                    killSentinel.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SENTINEL_OBJECTIVE";
                    killSentinel.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SENTINEL_OBJECTIVE";
                    killSentinel.KillTargetGameTag = gameTagMainObjective;
                    killSentinel.IsVictoryObjective = false;
                    KillSentinel = killSentinel;

                    string nameObjectiveScatterEnemies = "ScatterRemainingAttackers";
                    GameTagDef gameTagSecondObjective = Helper.CreateDefFromClone(
                        source,
                        "{ADACF6A2-A969-4518-AD36-C94D1A1C6A82}",
                        nameObjectiveScatterEnemies + "_GameTagDef");
                    KillActorFactionObjectiveDef scatterEnemies = Helper.CreateDefFromClone(killActorFactionObjectiveSource, "{B7BB4BFF-E7DC-4FD1-A307-FF348FC87946}", nameObjectiveScatterEnemies);
                    scatterEnemies.KillTargetGameTag = gameTagSecondObjective;
                    scatterEnemies.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SECOND_OBJECTIVE";
                    scatterEnemies.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SECOND_OBJECTIVE";
                    scatterEnemies.ParalysedCounts = true;
                    scatterEnemies.AchievedWhenEnemiesAreDefeated = true;
                    ScatterEnemies = scatterEnemies;
                    ScatterEnemiesTag = gameTagSecondObjective;
                    //secondKillAll.IsDefeatObjective = false;

                    //infestation BD mission, destroy Spawnery, then scatter attackers
                    killInfestation.NextOnSuccess = new FactionObjectiveDef[] { scatterEnemies };
                    killScylla.NextOnSuccess = new FactionObjectiveDef[] { scatterEnemies };

                    SurviveTurnsFactionObjectiveDef sourceSurviveObjective = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveAmbush_CustomMissionObjective");
                    string nameObjective = "SurviveFiveTurns";

                    SurviveTurnsFactionObjectiveDef surviveFiveTurns = Helper.CreateDefFromClone(sourceSurviveObjective, "{EC7E94DD-199B-41BF-B6D7-7933CE40E0C1}", nameObjective);
                    surviveFiveTurns.SurviveTurns = 5;
                    surviveFiveTurns.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SURVIVE5_OBJECTIVE";
                    surviveFiveTurns.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SURVIVE_COMPLETE";
                    surviveFiveTurns.IsDefeatObjective = false;
                    SurviveFiveTurns = surviveFiveTurns;

                    //early BD mission, survive 5 turns, then scatter attackers
                    surviveFiveTurns.NextOnSuccess = new FactionObjectiveDef[] { scatterEnemies };



                    string nameObjectivSurviveThreeTurns = "SurviveThreeTurns";

                    SurviveTurnsFactionObjectiveDef surviveThreeTurns = Helper.CreateDefFromClone(sourceSurviveObjective, "{B817A3CD-482B-472F-85EC-7259451E8F88}", nameObjectivSurviveThreeTurns);
                    surviveThreeTurns.SurviveTurns = 3;
                    surviveThreeTurns.MissionObjectiveData.Description.LocalizationKey = "BASEDEFENSE_SURVIVE3_OBJECTIVE";
                    surviveThreeTurns.MissionObjectiveData.Summary.LocalizationKey = "BASEDEFENSE_SURVIVE_COMPLETE";
                    surviveThreeTurns.NextOnSuccess = new FactionObjectiveDef[] { scatterEnemies };
                    surviveThreeTurns.IsDefeatObjective = false;
                    SurviveThreeTurns = surviveThreeTurns;

                    //Mid BD mission, first kill Sentinel, then Survive 3 turns, then scatter attackers //changed to survive 5 turns, to make it harder then first scenario
                    killSentinel.NextOnSuccess = new FactionObjectiveDef[] { surviveFiveTurns };

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void ChangeBaseDefense()
            {
                try
                {
                    CustomMissionTypeDef baseDefenseMissionTypeDef = DefCache.GetDef<CustomMissionTypeDef>("PXBaseAlien_CustomMissionTypeDef");
                    baseDefenseMissionTypeDef.MandatoryMission = false;
                    baseDefenseMissionTypeDef.SkipDeploymentSelection = false;
                    baseDefenseMissionTypeDef.MaxPlayerUnits = 9;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            internal static void CreateConsolePromptBaseDefense()
            {
                try
                {
                    string gUID = "{444AE91B-2FA4-4296-914A-72F0310D8D46}";
                    string name = "TFTVBaseDefensePrompt";
                    TacticalPromptDef source = DefCache.GetDef<TacticalPromptDef>("ActivateObjectivePromptDef");
                    TacticalPromptDef newPrompt = Helper.CreateDefFromClone(source, gUID, name);

                    newPrompt.PromptText.LocalizationKey = "BASEDEFENSE_VENTING_PROMPT";
                    newPrompt.PromptIcon = Base.UI.MessageBox.MessageBoxIcon.Warning;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }


        }

        //Returns time remaining for attack to be completed only for purposes of preparing and implementing tactical mission (including deployment).
        public static float CalculateBaseAttackProgress(GeoSite phoenixBase)
        {
            try
            {

                double timeSpanHoursTimer = phoenixBase.ExpiringTimerAt.TimeSpan.TotalHours;
                double timeSpanHoursNow = phoenixBase.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                return (float)superClockTimer;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }





        [HarmonyPatch(typeof(GeoAlienFaction), "PhoenixBaseAttackCheck")]
        public static class GeoAlienFaction_PhoenixBaseAttackCheck_patch
        {

            public static bool Prefix(GeoAlienFaction __instance)
            {
                try
                {
                    foreach (SiteAttackSchedule item in __instance.PhoenixBaseAttackSchedule)
                    {
                        if (item.HasAttackScheduled)
                        {
                            continue;
                        }

                        GeoSite pxBase = item.Site;
                        if (pxBase.State != GeoSiteState.Functioning || !pxBase.GetInspected(__instance)
                            || PhoenixBasesUnderAttack != null && PhoenixBasesUnderAttack.ContainsKey(pxBase.SiteId)
                            || PhoenixBasesInfested != null && PhoenixBasesInfested.Contains(pxBase.SiteId))
                        {
                            continue;
                        }

                        foreach (GeoAlienBase colony in __instance.Bases)
                        {
                            if (colony.SitesInRange.Contains(pxBase))
                            {
                                GeoLevelController controller = __instance.GeoLevel;
                                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                                float distance = Vector3.Distance(colony.Site.WorldPosition, pxBase.WorldPosition);
                                bool pxBaseInMist = pxBase.IsInMist;
                                bool hasTelepathicCaptive =
                                    phoenixFaction.ContaimentUsage > 0 &&
                                    pxBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(f => f.GetComponent<PrisonFacilityComponent>() != null) &&
                                    phoenixFaction.CapturedUnits.Any(gud => gud.ClassTag == DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef")
                                    || gud.ClassTag == DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef"));

                                //Nest 5 per day
                                //Lair 10 per day
                                //Citadel 20 per day
                                //Palace 0 per day

                                // bool researchedWalls = phoenixFaction.Research.HasCompleted("NJ_WallsOfJericho_ResearchDef");
                                bool researchedDomovoy = phoenixFaction.Research.HasCompleted("SYN_SafeZoneProject_ResearchDef");

                                int colonyCounter = colony.AlienBaseTypeDef.PhoenixBaseAttackCounterPerDay;
                                float multiplier = 1;

                                if (distance < 2)
                                {
                                    multiplier += 2 - distance;
                                }

                                if (pxBaseInMist)
                                {
                                    multiplier *= 1.5f;
                                }

                                if (hasTelepathicCaptive)
                                {
                                    multiplier *= 3f; //changed from 1.5f for update 53
                                }

                                if (researchedDomovoy)
                                {
                                    multiplier *= 0.5f;
                                }

                                int adjustedCounter = (int)(colonyCounter * multiplier);

                                item.Counter += adjustedCounter;

                                TFTVLogger.Always($"{colony.AlienBaseTypeDef.Name.LocalizeEnglish()} " +
                                 $"is {distance} from {pxBase.LocalizedSiteName} in mist? {pxBaseInMist} with a telepathic captive? {hasTelepathicCaptive}" +
                                 $" player researched Project Domovoy? {researchedDomovoy}, colonyCounter is {colonyCounter}, multiplier is {multiplier}, so adjusted counter per day is {adjustedCounter}," +
                                 $"and accumulated counter is {item.Counter}");
                            }
                        }

                        if (item.Counter >= __instance.FactionDef.PhoenixBaseAttackMissionCounter && !item.Site.HasActiveMission && __instance.Research.HasCompleted("ALN_Lair_ResearchDef"))
                        {
                            (from b in __instance.Bases
                             where b.SitesInRange.Contains(pxBase)
                             select b.Site).ToList();
                            __instance.AttackPhoenixBase(pxBase);
                        }
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

        internal class InitAttack
        {

            internal class ContainmentBreach
            {
                private static readonly PhoenixFacilityDef _containment = DefCache.GetDef<PhoenixFacilityDef>("AlienContainment_PhoenixFacilityDef");
                private static readonly PrisonFacilityComponentDef _prisonComponent = DefCache.GetDef<PrisonFacilityComponentDef>("E_Prison [AlienContainment_PhoenixFacilityDef]");

                internal static bool scyllaPresent = false;
                internal static bool sirenPresent = false;
                internal static bool wallsOfJericho = false;

                public static void HourlyCheckContainmentBreachDuringBaseDefense(GeoLevelController controller)
                {
                    try
                    {
                        if (PhoenixBasesUnderAttack == null || PhoenixBasesUnderAttack.Count == 0)
                        {
                            return;
                        }

                        GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                        List<GeoPhoenixBase> phoenixBasesUnderAttack = phoenixFaction.Bases.Where(pb => PhoenixBasesUnderAttack.ContainsKey(pb.Site.SiteId)).ToList();

                        foreach (GeoPhoenixBase phoenixBase in phoenixBasesUnderAttack)
                        {
                            

                            float timer = CalculateBaseAttackProgress(phoenixBase.Site);
                            int siteID = phoenixBase.Site.SiteId;

                            if (HasUndamagedContainment(phoenixBase) && RollContainmentBreach(phoenixBase.Site, timer))
                            {
                                timer = Math.Max(timer - 12, 4);
                                phoenixBase.Site.ExpiringTimerAt = TimeUnit.FromSeconds((float)(3600 * Math.Max(PhoenixBasesUnderAttack[siteID].First().Value - 12, 4)));
                                string faction = PhoenixBasesUnderAttack[siteID].First().Key;
                                PhoenixBasesUnderAttack[siteID][faction] = Math.Max(PhoenixBasesUnderAttack[siteID][faction] - 12, 4);
                                phoenixBase.Site.RefreshVisuals();
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void ImplementContaimentBreach(GeoSite geoSite)
                {
                    try
                    {
                        if (PandoransThatCanEscape == null || PandoransThatCanEscape.Count() == 0 || !PandoransThatCanEscape.ContainsKey(geoSite.SiteId))
                        {
                            return;
                        }

                        ContainmentBreachSchedule.Add(geoSite.SiteId, false);
                        ContainmentBreachInProgress = true;
                        CaptiveEscapeContainment(geoSite.SiteId, geoSite.GeoLevel.PhoenixFaction);
                        ContainmentBreachInProgress = false;
                        DamageContainmentFacilities(geoSite);
                        GeoscapeEventContext context = new GeoscapeEventContext(geoSite, geoSite.GeoLevel.PhoenixFaction);
                        foreach (string item in PandoransThatCanEscape[geoSite.SiteId])
                        {
                            TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);
                            if (tacCharacterDef.ClassTag.className == "Queen")
                            {
                                geoSite.GeoLevel.EventSystem.TriggerGeoscapeEvent(_scyllaIsLooseEventDef.EventID, context);
                                ContainmentBreachSchedule[geoSite.SiteId] = true;
                                return;
                            }
                        }

                        geoSite.GeoLevel.EventSystem.TriggerGeoscapeEvent(_containmentBreachEventDef.EventID, context);

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                internal static void CheckOnCaptiveDestroyed(GeoUnitDescriptor geoUnitDescriptor)
                {
                    try
                    {
                        // TFTVLogger.Always($"CheckOnCaptiveDestroyed running for {geoUnitDescriptor.GetName()}");

                        if (PandoransThatCanEscape.Keys.Count == 0)
                        {
                            return;
                        }

                        if (ContainmentBreachInProgress)
                        {
                            return;
                        }

                        int baseId = 0;
                        string itemToRemove = "";


                        foreach (int phoenixBase in PandoransThatCanEscape.Keys)
                        {
                            foreach (string item in PandoransThatCanEscape[phoenixBase])
                            {
                                TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);

                                if (tacCharacterDef == geoUnitDescriptor.UnitType.TemplateDef)
                                {
                                    itemToRemove = item;
                                    baseId = phoenixBase;
                                    break;
                                }
                            }
                        }

                        PandoransThatCanEscape[baseId].Remove(itemToRemove);
                        if (PandoransThatCanEscape[baseId].Count == 0)
                        {
                            PandoransThatCanEscape.Remove(baseId);
                            return;
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void CheckPurgeContainmentEvent(GeoEventChoice choice, GeoscapeEvent @event)
                {
                    try
                    {
                        if (@event.EventID == _purgeContainmentEventDef.EventID && choice == _purgeContainmentEventDef.GeoscapeEventData.Choices[0])
                        {
                            TFTVLogger.Always($"Elected purge!");
                            GeoSite geoSite = @event.Context.Site;

                            if (geoSite != null && PandoransThatCanEscape.ContainsKey(geoSite.SiteId))
                            {
                                TFTVLogger.Always($"Found the site");
                                PurgeContainment(geoSite.SiteId, geoSite.GeoLevel.PhoenixFaction);
                                PandoransThatCanEscape.Remove(geoSite.SiteId);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }

                internal static bool HasUndamagedContainment(GeoPhoenixBase phoenixBase) //checks if breach already happened
                {
                    try
                    {
                        if (phoenixBase.Layout.Facilities.Any(f => f.GetComponent<PrisonFacilityComponent>() != null && !f.IsWorking))
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

                public static bool ContainmentBreachInProgress = false;

                internal static bool RollContainmentBreach(GeoSite geoSite, float timer)
                {
                    try
                    {
                        if (PandoransThatCanEscape == null || PandoransThatCanEscape != null && PandoransThatCanEscape.Count == 0)
                        {
                            return false;
                        }

                        if (timer >= 16)
                        {
                            return false;
                        }

                        float deployValue = 0;

                        foreach (string item in PandoransThatCanEscape[geoSite.SiteId])
                        {
                            TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);
                            deployValue += tacCharacterDef.DeploymentCost;
                        }

                        int deployRollChance = (int)(deployValue / 100) + 18 - (int)timer;

                        GeoPhoenixBase phoenixBase = geoSite.GetComponent<GeoPhoenixBase>();
                        PhoenixFacilityDef securityStationDef = DefCache.GetDef<PhoenixFacilityDef>("SecurityStation_PhoenixFacilityDef");
                        int securityValue = phoenixBase.SoldiersInBase.Count();

                        if (phoenixBase.Layout.Facilities.Any(f => f.Def == securityStationDef && f.IsWorking))
                        {
                            securityValue += phoenixBase.Layout.Facilities.Where(f => f.Def == securityStationDef && f.IsWorking).Count() * 3;
                        }

                        int roll = UnityEngine.Random.Range(0, Math.Max(deployRollChance - securityValue, 20));

                        TFTVLogger.Always($"Rolling for containment breach: deployRollChance:{deployRollChance}; securityValue: {securityValue}; roll: {roll}");

                        if (roll > 18)
                        {
                            ImplementContaimentBreach(geoSite);
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

                internal static void CaptiveEscapeContainment(int baseId, GeoPhoenixFaction phoenixFaction)
                {
                    try
                    {
                        List<TacCharacterDef> templates = new List<TacCharacterDef>();

                        foreach (string item in PandoransThatCanEscape[baseId])
                        {
                            templates.Add((TacCharacterDef)Repo.GetDef(item));
                        }

                        for (int x = 0; x < templates.Count; x++)
                        {
                            if (phoenixFaction.CapturedUnits.Any(c => c.UnitType.TemplateDef == templates[x]))
                            {
                                phoenixFaction.KillCapturedUnit(phoenixFaction.CapturedUnits.FirstOrDefault(cu => cu.UnitType.TemplateDef == templates[x]));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void PurgeContainment(int baseId, GeoPhoenixFaction phoenixFaction)
                {
                    try
                    {
                        List<TacCharacterDef> templates = new List<TacCharacterDef>();

                        foreach (string item in PandoransThatCanEscape[baseId])
                        {
                            templates.Add((TacCharacterDef)Repo.GetDef(item));
                        }

                        for (int x = 0; x < templates.Count; x++)
                        {
                            if (phoenixFaction.CapturedUnits.Any(c => c.UnitType.TemplateDef == templates[x]))
                            {
                                if (phoenixFaction.HarvestAliensForSuppliesUnlocked)
                                {
                                    phoenixFaction.HarvestCapturedUnit(phoenixFaction.CapturedUnits.FirstOrDefault(cu => cu.UnitType.TemplateDef == templates[x]), ResourceType.Supplies);
                                }
                                else
                                {
                                    phoenixFaction.KillCapturedUnit(phoenixFaction.CapturedUnits.FirstOrDefault(cu => cu.UnitType.TemplateDef == templates[x]));
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

                internal static void DamageContainmentFacilities(GeoSite geoSite)
                {
                    try
                    {
                        foreach (GeoPhoenixFacility geoPhoenixFacility in geoSite.GetComponent<GeoPhoenixBase>().Layout.Facilities)
                        {

                            if (geoPhoenixFacility.GetComponent<PrisonFacilityComponent>() != null)
                            {
                                geoPhoenixFacility.DamageFacility(50);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static bool BaseCanHaveContainmentBreach(GeoSite geoSite)
                {
                    try
                    {
                        if (geoSite.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(f => f.Def == _containment && f.IsWorking) && geoSite.GeoLevel.PhoenixFaction.CapturedUnits.Count() > 0)
                        {

                            GenerateCapturedUnitsList(geoSite.GeoLevel.PhoenixFaction, geoSite);
                            if (PandoransThatCanEscape[geoSite.SiteId].Count > 0)
                            {
                                TFTVLogger.Always($"{geoSite.LocalizedSiteName} can have containment breach!");
                                return true;
                            }

                        }
                        TFTVLogger.Always($"{geoSite.LocalizedSiteName} can't have containment breach.");
                        return false;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static int CountContainmentFacilities(GeoPhoenixFaction phoenixFaction)
                {
                    try
                    {
                        PrisonFacilityComponentDef containmentDef = DefCache.GetDef<PrisonFacilityComponentDef>("E_Prison [AlienContainment_PhoenixFacilityDef]");

                        int count = 0;

                        foreach (GeoPhoenixBase bases in phoenixFaction.Bases)
                        {
                            foreach (GeoPhoenixFacility facility in bases.Layout.Facilities)
                            {
                                if (facility.GetComponent<PrisonFacilityComponent>() is PrisonFacilityComponent containmentFacility
                                    && containmentFacility.ComponentDef == containmentDef && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
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

                internal static void GenerateCapturedUnitsList(GeoPhoenixFaction phoenixFaction, GeoSite phoenixBase)
                {
                    try
                    {
                        TFTVLogger.Always($"GenerateCapturedUnitsList invoked");

                        List<GeoUnitDescriptor> capturedUnits = phoenixFaction.CapturedUnits.ToList();

                        TFTVLogger.Always($"capturedUnits count: {capturedUnits.Count()}");

                        List<string> capturedUnitsTacCharacterGUID = new List<string>();
                        int containmentFaciltiesAtBase = phoenixBase.GetComponent<GeoPhoenixBase>().Layout.Facilities.Where(f => f.Def == _containment && f.IsWorking).Count();
                        int containmentFacilitiesOutsideBase = CountContainmentFacilities(phoenixFaction) - containmentFaciltiesAtBase;

                        TFTVLogger.Always($"containmentFacilitiesOutsideBase count {containmentFacilitiesOutsideBase}");

                        if (containmentFacilitiesOutsideBase == 0)
                        {
                            foreach (GeoUnitDescriptor item in capturedUnits)
                            {
                                capturedUnitsTacCharacterGUID.Add(item.UnitType.TemplateDef.Guid);

                                if (item.ClassTag.className == "Queen")
                                {
                                    scyllaPresent = true;
                                }
                                else if (item.ClassTag.className == "Siren")
                                {
                                    sirenPresent = true;
                                }
                            }
                        }
                        else
                        {
                            //List<GeoUnitDescriptor> capturedUnitsSelection = new List<GeoUnitDescriptor>();
                            int totalVolumeAtBase = containmentFaciltiesAtBase * _prisonComponent.ContaimentCapacity;
                            int totalVolumeOutSideBase = containmentFacilitiesOutsideBase * _prisonComponent.ContaimentCapacity;
                            int totalVolumeUsed = 0;

                            foreach (GeoUnitDescriptor capturedUnit in capturedUnits)
                            {
                                totalVolumeUsed += capturedUnit.Volume;
                                if (capturedUnit.ClassTag.className == "Queen")
                                {
                                    scyllaPresent = true;
                                }
                                else if (capturedUnit.ClassTag.className == "Siren")
                                {
                                    sirenPresent = true;
                                }
                            }

                            int estimateVolumeUsedAtBase = (totalVolumeUsed / (containmentFacilitiesOutsideBase + containmentFaciltiesAtBase)) * containmentFaciltiesAtBase;

                            TFTVLogger.Always($"estimatedVolumeUsedAtBase: {estimateVolumeUsedAtBase}");

                            int meter = 0;

                            capturedUnits = capturedUnits.Shuffle().ToList();

                            foreach (GeoUnitDescriptor geoUnitDescriptor in capturedUnits)
                            {
                                TFTVLogger.Always($"{geoUnitDescriptor.GetName()} {geoUnitDescriptor.Volume}", false);
                            }


                            for (int i = 0; meter < estimateVolumeUsedAtBase && i < capturedUnits.Count; i++)
                            {
                                meter += capturedUnits[i].Volume;
                                capturedUnitsTacCharacterGUID.Add(capturedUnits[i].UnitType.TemplateDef.Guid);

                                TFTVLogger.Always($"added {capturedUnits[i].UnitType.TemplateDef.name} to the list");

                            }

                        }

                        if (capturedUnitsTacCharacterGUID.Count == 0)
                        {
                            TFTVLogger.Always($"No captured Pandas in the list!");
                            return;
                        }

                        PandoransThatCanEscape.Add(phoenixBase.SiteId, capturedUnitsTacCharacterGUID);

                        TFTVLogger.Always($"count of items in _pandoransThatCanEscape[phoenixBase.SiteId]: {PandoransThatCanEscape[phoenixBase.SiteId].Count()}");

                        AdjustPurgeEvent(PandoransThatCanEscape[phoenixBase.SiteId]);

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void AdjustUnderAttackEvent()
                {
                    try
                    {
                        _underAttackEventDef.GeoscapeEventData.Description[0].General = new LocalizedTextBind(Defs.Key_UnderAttackBaseText);

                        string text = TFTVCommonMethods.ConvertKeyToString(Defs.Key_UnderAttackBaseText);


                        if (scyllaPresent)
                        {
                            text += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_CAPTIVE_SCYLLA_TEXT")}\n";
                        }
                        else if (sirenPresent)
                        {
                            text += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_CAPTIVE_SIREN_TEXT")}\n";
                        }

                        if (wallsOfJericho)
                        {
                            text += $"\n{TFTVCommonMethods.ConvertKeyToString("BASEDEFENSE_NJWALLS_TEXT")}";
                        }

                        scyllaPresent = false;
                        sirenPresent = false;
                        wallsOfJericho = false;

                        _underAttackEventDef.GeoscapeEventData.Description[0].General = new LocalizedTextBind(text, true);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustPurgeEvent(List<string> templateCharacterGUIDS)
                {
                    try
                    {
                        _purgeContainmentEventDef.GeoscapeEventData.Description[0].General = new LocalizedTextBind(Defs.Key_ContainmentBaseText);

                        Dictionary<TacCharacterDef, int> captures = new Dictionary<TacCharacterDef, int>();

                        foreach (string item in templateCharacterGUIDS)
                        {
                            TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);

                            TFTVLogger.Always($"{tacCharacterDef.name}");

                            if (captures.Keys.Contains(tacCharacterDef))
                            {
                                captures[tacCharacterDef] += 1;
                            }
                            else
                            {
                                captures.Add(tacCharacterDef, 1);
                            }
                        }

                        string text = TFTVCommonMethods.ConvertKeyToString(Defs.Key_ContainmentBaseText);

                        foreach (TacCharacterDef tacCharacterDef in captures.Keys)
                        {
                            TFTVLogger.Always($"{captures[tacCharacterDef]} {tacCharacterDef.Data.Name}");
                            text += $"\n{captures[tacCharacterDef]} {TFTVCommonMethods.ConvertKeyToString(tacCharacterDef.Data.Name)}";
                        }

                        _purgeContainmentEventDef.GeoscapeEventData.Description[0].General = new LocalizedTextBind(text, true);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static GeoSite FindPhoenixBase(int id, GeoLevelController controller)
            {
                try
                {
                    List<GeoPhoenixBase> allPhoenixBases = controller.PhoenixFaction.Bases.ToList();

                    GeoSite targetPhoenixBase = null;

                    foreach (GeoPhoenixBase phoenixBase in allPhoenixBases)
                    {
                        if (phoenixBase.Site.SiteId == id)
                        {
                            targetPhoenixBase = phoenixBase.Site;
                        }
                    }

                    return targetPhoenixBase;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void AddToTFTVAttackSchedule(GeoSite phoenixBase, GeoLevelController controller, GeoFaction attacker, int hours)
            {
                try
                {
                    TimeUnit timeForAttack = TimeUnit.FromHours(hours);
                    TimeUnit timer = controller.Timing.Now + timeForAttack;
                    PhoenixBasesUnderAttack.Add(
                        phoenixBase.SiteId,
                        new Dictionary<string, double>()
                        {
                { attacker.GetPPName(), timer.TimeSpan.TotalHours}
                        });
                    phoenixBase.RefreshVisuals();
                    TFTVLogger.Always($"{phoenixBase.LocalizedSiteName} was added to the list of Phoenix bases under attack by {attacker}. " +
                        $"Attack will be completed successfully by {timer}");

                    if (hours != 18)
                    {
                        if (PhoenixBasesUnderAttackSchedule.ContainsKey(phoenixBase.SiteId))
                        {
                            PhoenixBasesUnderAttackSchedule.Remove(phoenixBase.SiteId);
                            TFTVLogger.Always($"{phoenixBase.LocalizedSiteName} somehow already in breach list (which is not a breach list, remember)! Removing before re-adding");
                        }

                        PhoenixBasesUnderAttackSchedule.Add(phoenixBase.SiteId, hours);
                    }

                    //For implementing base defense as proper objective:
                    DiplomaticGeoFactionObjective protectBase = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                    {
                        Title = new LocalizedTextBind($"<color=#FF0000>Phoenix base {phoenixBase.LocalizedSiteName} is under attack!</color>", true)
                    };

                    controller.PhoenixFaction.AddObjective(protectBase);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void StartPandoranAttackOnBase(GeoLevelController controller, PPFactionDef factionDef, GeoSite phoenixBase)
            {
                try
                {
                    GeoFaction attacker = controller.GetFaction(factionDef);
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;

                    TFTVLogger.Always($"Phoenix base under attack? {PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId)} Phoenix base infested? {PhoenixBasesInfested.Contains(phoenixBase.SiteId)} ");

                    if (phoenixBase.Type == GeoSiteType.PhoenixBase && !PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId) && !PhoenixBasesInfested.Contains(phoenixBase.SiteId))
                    {

                        int hoursToCompleteAttack = 18;

                        GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, controller.PhoenixFaction);

                        if (controller.PhoenixFaction.Research.HasCompleted("NJ_WallsOfJericho_ResearchDef"))
                        {
                            TFTVLogger.Always($"Player has researched Walls of Jericho! Getting extra 6 hours.");
                            hoursToCompleteAttack += 6;
                            ContainmentBreach.wallsOfJericho = true;
                        }

                        ContainmentBreach.BaseCanHaveContainmentBreach(phoenixBase);
                        ContainmentBreach.AdjustUnderAttackEvent();
                        controller.EventSystem.TriggerGeoscapeEvent(_underAttackEventDef.EventID, context);



                        if (PandoransThatCanEscape.Count > 0)
                        {
                            TFTVLogger.Always($"ContainmentBreach._pandoransThatCanEscape.Count > 0");
                            controller.EventSystem.TriggerGeoscapeEvent(_purgeContainmentEventDef.EventID, context);

                        }

                        TFTVHints.GeoscapeHints.TriggerBaseDefenseHint(controller);
                        AddToTFTVAttackSchedule(phoenixBase, controller, attacker, hoursToCompleteAttack);

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            //Patch to add base to TFTV schedule
            [HarmonyPatch(typeof(SiteAttackSchedule), "StartAttack")]
            public static class SiteAttackSchedule_StartAttack_Experiment_patch
            {
                public static void Prefix(SiteAttackSchedule __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"StartAttack invoked for {__instance.Site.LocalizedSiteName}");
                        GeoSite phoenixBase = __instance.Site;
                        GeoLevelController controller = __instance.Site.GeoLevel;
                        PPFactionDef factionDef = __instance.Attacker;

                        if (factionDef == Shared.AlienFactionDef)
                        {
                            StartPandoranAttackOnBase(controller, factionDef, phoenixBase);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            //Patch to avoid showing attack in log
            [HarmonyPatch(typeof(GeoscapeLog), "OnFactionSiteAttackScheduled")]
            internal static class TFTV_GeoscapeLog_OnFactionSiteAttackScheduled_HideAttackInLogger
            {
                public static bool Prefix()
                {
                    try
                    {
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

        internal class GeoObjective
        {
            internal static void RemoveBaseDefenseObjective(string baseName)
            {
                try
                {
                    GeoPhoenixFaction geoPhoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<GeoFactionObjective> listOfObjectives = geoPhoenixFaction.Objectives.ToList();

                    foreach (GeoFactionObjective objective in listOfObjectives)
                    {
                        if (objective.GetTitle() == null)
                        {
                            TFTVLogger.Always("objective.GetTitle() returns null!");
                        }
                        else
                        {
                            foreach (GeoSite geoSite in geoPhoenixFaction.Sites)
                            {
                                if (objective.GetTitle().Contains(baseName))
                                {
                                    geoPhoenixFaction.RemoveObjective(objective);
                                }
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

            public static void AddUnderAttackBaseToObjective(DiplomaticGeoFactionObjective objective, ref IEnumerable<GeoActor> __result, ref List<GeoSite> ____assignedSites)
            {

                try
                {
                    //  TFTVLogger.Always($"GetRelatedActorsInvoked objective {__instance.GetTitle()}");

                    GeoLevelController geoLevelController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    foreach (GeoSite geoSite in geoLevelController.PhoenixFaction.Sites)
                    {
                        if (objective.GetTitle().Contains(geoSite.LocalizedSiteName))
                        {
                            ____assignedSites.Add(geoSite);

                            __result = ____assignedSites;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            [HarmonyPatch(typeof(GeoFaction), "get_Objectives")]
            internal static class TFTV_GeoFaction_get_Objectives_ExperimentPatch
            {
                public static void Postfix(GeoFaction __instance, ref IReadOnlyList<GeoFactionObjective> __result)
                {

                    try
                    {
                        List<GeoFactionObjective> reOrderedObjectiveList = new List<GeoFactionObjective>();


                        if (__result != null && GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                        {
                            foreach (GeoFactionObjective objective in __result)
                            {
                                if (objective.GetTitle() != null && objective.GetTitle().Contains("Phoenix base") && objective.GetTitle().Contains("is under attack!"))
                                {
                                    //  TFTVLogger.Always($"Found base under attack objective");
                                    reOrderedObjectiveList.Add(objective);

                                }
                            }
                        }


                        if (__result != null && GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                        {
                            foreach (GeoFactionObjective objective in __result)
                            {
                                string localizationKey = objective.Title?.LocalizationKey;

                                if (localizationKey != null && localizationKey.Contains("VOID_OMEN_TITLE_"))
                                {
                                    //  TFTVLogger.Always($"Found VO objective");
                                    reOrderedObjectiveList.Add(objective);
                                }
                            }
                        }


                        if (reOrderedObjectiveList.Count > 0)
                        {

                            reOrderedObjectiveList.AddRange(__result.Where(o => !reOrderedObjectiveList.Contains(o)));

                            __result = reOrderedObjectiveList;

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleGeoObjectives), "InitObjective")]
            internal class TFTV_UIModuleGeoObjectives_SetObjective_ExperimentPatch
            {

                public static bool Prefix(UIModuleGeoObjectives __instance, GeoObjectiveElementController element,
                    GeoFactionObjective objective, GeoLevelController ____level, IEnumerable<GeoFactionObjective> ____rawObjectives,
                   ref int ____objectiveClickIndex, ref GeoObjectiveElementController ____lastObjectiveClicked)
                {
                    try
                    {


                        string warningMessage = "<color=#FF0000> !!! WARNING !!! A PHOENIX BASE IS UNDER ATTACK</color>";
                        Text objectivesHeaderText = __instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>();
                        string objectivesRegularHeader = new LocalizedTextBind() { LocalizationKey = "KEY_OBJECTIVES" }.Localize();


                        if (PhoenixBasesUnderAttack.Count > 0)
                        {
                            objectivesHeaderText.text += warningMessage;
                        }
                        else
                        {
                            if (__instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>().text.Contains(warningMessage))
                            {
                                objectivesHeaderText.text = objectivesRegularHeader;


                            }
                        }

                        if (objective.GetTitle().Contains("Phoenix base") && objective.GetTitle().Contains("is under attack!"))
                        {

                            //   MethodInfo getObjectiveTooltipMethod = __instance.GetType().GetMethod("GetObjectiveTooltip", BindingFlags.NonPublic | BindingFlags.Instance);

                            if (____level == null)
                            {
                                ____level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            }

                            Color factionColor = brightRed;


                            EventGeoFactionObjective eventGeoFactionObjective = objective as EventGeoFactionObjective;
                            TimeUnit endTime = TimeUnit.Invalid;
                            if (eventGeoFactionObjective != null)
                            {
                                string timerID = ____level.EventSystem.GetEventByID(eventGeoFactionObjective.EventID).GeoscapeEventData.TimerID;
                                if (!string.IsNullOrWhiteSpace(timerID))
                                {
                                    GeoEventTimer timerById = ____level.EventSystem.GetTimerById(timerID);
                                    if (timerById != null)
                                    {
                                        endTime = timerById.EndAt;
                                    }
                                }
                            }


                            bool isLocationObjective = true;//objective.GetRelatedActors().Any((GeoActor x) => objective.IsActorFocusableByObjective(x));
                            LocalizedTextBind objectiveTooltip = new LocalizedTextBind();//(LocalizedTextBind)getObjectiveTooltipMethod.Invoke(__instance, new object[] { objective });
                            element.SetObjective(objective.GetIcon(), factionColor, objective.GetTitle(), ____rawObjectives.IndexOf(objective), isLocationObjective, true, objectiveTooltip, endTime, ____level.Timing);

                            if (____lastObjectiveClicked != element)
                            {
                                ____lastObjectiveClicked = element;
                                ____objectiveClickIndex = -1;
                            }

                            ____objectiveClickIndex++;

                            MethodInfo onElementClickedMethod = __instance.GetType().GetMethod("OnElementClicked", BindingFlags.NonPublic | BindingFlags.Instance);

                            element.OnElementClicked = (Action<GeoObjectiveElementController>)Delegate.CreateDelegate(typeof(Action<GeoObjectiveElementController>), __instance, onElementClickedMethod);

                            return false;

                        }

                        string localizationKey = objective.Title?.LocalizationKey;

                        if (localizationKey != null && localizationKey.Contains("VOID_OMEN_TITLE_"))
                        {
                            MethodInfo onElementClickedMethod = __instance.GetType().GetMethod("OnElementClicked", BindingFlags.NonPublic | BindingFlags.Instance);
                            MethodInfo getObjectiveTooltipMethod = __instance.GetType().GetMethod("GetObjectiveTooltip", BindingFlags.NonPublic | BindingFlags.Instance);


                            if (____level == null)
                            {
                                ____level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                            }

                            Color factionColor = purple;
                            EventGeoFactionObjective eventGeoFactionObjective = objective as EventGeoFactionObjective;
                            TimeUnit endTime = TimeUnit.Invalid;
                            if (eventGeoFactionObjective != null)
                            {
                                string timerID = ____level.EventSystem.GetEventByID(eventGeoFactionObjective.EventID).GeoscapeEventData.TimerID;
                                if (!string.IsNullOrWhiteSpace(timerID))
                                {
                                    GeoEventTimer timerById = ____level.EventSystem.GetTimerById(timerID);
                                    if (timerById != null)
                                    {
                                        endTime = timerById.EndAt;
                                    }
                                }
                            }


                            bool isLocationObjective = objective.GetRelatedActors().Any((GeoActor x) => objective.IsActorFocusableByObjective(x));
                            LocalizedTextBind objectiveTooltip = (LocalizedTextBind)getObjectiveTooltipMethod.Invoke(__instance, new object[] { objective });
                            element.SetObjective(objective.GetIcon(), factionColor, objective.GetTitle(), ____rawObjectives.IndexOf(objective), isLocationObjective, false, objectiveTooltip, endTime, ____level.Timing);

                            element.OnElementClicked = (Action<GeoObjectiveElementController>)onElementClickedMethod.Invoke(__instance, new object[] { element });

                            //  __instance.ObjectivesHeader.transform.Find("ObjectivesLabel").GetComponent<Text>().color = purple;

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

        }

        internal class Briefing
        {

            internal static Sprite _defaultPicForBaseDefense = null;

            //Patch to change briefing depending on attack progress
            [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]
            public static class PhoenixBaseDefenseDataBind_ModalShowHandler_DontCancelMission_patch
            {
                public static void Postfix(PhoenixBaseDefenseDataBind __instance, UIModal modal)
                {
                    try
                    {

                        GeoMission geoMission = (GeoMission)modal.Data;
                        GeoLevelController controller = geoMission.Level;

                        if (_defaultPicForBaseDefense == null)
                        {
                            _defaultPicForBaseDefense = __instance.Background.sprite;
                        }

                        if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId) || PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                        {
                            GeoSite phoenixBase = geoMission.Site;

                            double timeSpanHoursTimer = phoenixBase.ExpiringTimerAt.TimeSpan.TotalHours;
                            double timeSpanHoursNow = phoenixBase.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                            TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                            TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                            double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                            float totalTimeForAttack = 18;

                            if (PhoenixBasesUnderAttackSchedule.ContainsKey(phoenixBase.SiteId))
                            {
                                totalTimeForAttack = PhoenixBasesUnderAttackSchedule[phoenixBase.SiteId];
                            }

                            float timer = (float)superClockTimer;
                            TFTVLogger.Always($"DontCancelMission: timer is {timer}");

                            FactionInfoMapping factionInfo = __instance.Resources.GetFactionInfo(geoMission.GetEnemyFaction());
                            LocalizedTextBind text = new LocalizedTextBind();
                            LocalizedTextBind objectivesText = new LocalizedTextBind();
                            Sprite sprite;

                            if (timer > 12)//progress < 0.3)
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_TACTICALADVANTAGE" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_SIMPLE" };
                            }
                            else if (PhoenixBasesInfested.Contains(geoMission.Site.SiteId) || timer < 6) //progress >= 0.8)
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint_infestation.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_INFESTATION" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_DOUBLE" };
                            }
                            else
                            {
                                sprite = Helper.CreateSpriteFromImageFile("base_defense_hint_nesting.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_NESTING" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_OBJECTIVES_DOUBLE" };
                            }

                            if (ContainmentBreachSchedule.ContainsKey(geoMission.Site.SiteId) && ContainmentBreachSchedule[geoMission.Site.SiteId])
                            {
                                sprite = Helper.CreateSpriteFromImageFile("BD_LooseScylla_briefing.jpg");
                                text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_BRIEFING_LOOSE_SCYLLA" };
                                objectivesText = new LocalizedTextBind() { LocalizationKey = "KEY_OBJECTIVE_KILL_QUEEN" };
                            }

                            __instance.Background.sprite = sprite;
                            __instance.Warning.SetWarning(text, factionInfo.Name, geoMission.Site.SiteName);
                            Text description = __instance.GetComponentInChildren<ObjectivesController>().Objectives;
                            description.GetComponent<I2.Loc.Localize>().enabled = false;
                            description.text = objectivesText.Localize();
                        }
                        else
                        {
                            __instance.Background.sprite = _defaultPicForBaseDefense;
                            Text description = __instance.GetComponentInChildren<ObjectivesController>().Objectives;
                            description.GetComponent<I2.Loc.Localize>().enabled = true;
                            description.text = TFTVCommonMethods.ConvertKeyToString("KEY_MISSION_PX_BASE_DEFENCE_DESCRIPTION");

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

        }

        internal class Deployment
        {
            private static readonly Sprite iconEntrance = Helper.CreateSpriteFromImageFile("BD_EntranceIcon.png");
            private static readonly Sprite iconHangar = Helper.CreateSpriteFromImageFile("BD_HangarIcon.png");
            private static readonly Sprite iconLift = Helper.CreateSpriteFromImageFile("BD_LiftIcon.png");

            internal static List<int> listEntrance = new List<int>();
            internal static List<int> listHangar = new List<int>();
            internal static List<int> listLift = new List<int>();

            private static float _timer = 0f;
            private static bool _accessLift = false;

            internal class UI
            {
                internal static void CreateCheckButton(GeoRosterDeploymentItem geoRosterDeploymentItem)
                {
                    try
                    {
                        float timer = _timer;
                        TFTVLogger.Always($"BaseDefense timer: {_timer}");

                        bool accessLift = _accessLift;
                        bool isVehicle = geoRosterDeploymentItem.Character.GameTags.Contains(Shared.SharedGameTags.VehicleClassTag)
                            || geoRosterDeploymentItem.Character.GameTags.Contains(Shared.SharedGameTags.MutogTag);

                        //  TFTVLogger.Always($"{geoRosterDeploymentItem.Character.DisplayName} vehicle? {isVehicle}");

                        Resolution resolution = Screen.currentResolution;

                        // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                        float resolutionFactorHeight = (float)resolution.height / 1080f;
                        //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                        // TFTVLogger.Always($"checking");

                        PhoenixGeneralButton checkButton = UnityEngine.Object.Instantiate(geoRosterDeploymentItem.CheckButton, geoRosterDeploymentItem.transform);
                        checkButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_DEPLOYMENT_ZONE_TIP");// "Toggles helmet visibility on/off.";

                        UIButtonIconController uIButtonIconController = checkButton.GetComponent<UIButtonIconController>();

                        uIButtonIconController.Icon.gameObject.SetActive(true);

                        uIButtonIconController.Icon.sprite = iconEntrance;

                        checkButton.transform.position += new Vector3(-100 * resolutionFactorWidth, 0);
                        checkButton.PointerClicked += () => ToggleButtonClicked(checkButton, geoRosterDeploymentItem);
                        AssignTeam(checkButton, geoRosterDeploymentItem);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AssignTeam(PhoenixGeneralButton checkButton, GeoRosterDeploymentItem geoRosterDeploymentItem)
                {
                    try
                    {
                        GeoCharacter geoCharacter = geoRosterDeploymentItem.Character;


                        if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconHangar && geoRosterDeploymentItem.EnrollForDeployment)
                        {
                            if (!listHangar.Contains(geoCharacter.Id))
                            {
                                listHangar.Add(geoCharacter.Id);
                            }

                            if (listEntrance.Contains(geoCharacter.Id))
                            {
                                listEntrance.Remove(geoCharacter.Id);
                            }

                            if (listLift.Contains(geoCharacter.Id))
                            {
                                listLift.Remove(geoCharacter.Id);
                            }
                        }
                        else if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconEntrance && geoRosterDeploymentItem.EnrollForDeployment)
                        {
                            if (!listEntrance.Contains(geoCharacter.Id))
                            {
                                listEntrance.Add(geoCharacter.Id);
                            }

                            if (listHangar.Contains(geoCharacter.Id))
                            {
                                listHangar.Remove(geoCharacter.Id);
                            }

                            if (listLift.Contains(geoCharacter.Id))
                            {
                                listLift.Remove(geoCharacter.Id);
                            }
                        }
                        else if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconLift && geoRosterDeploymentItem.EnrollForDeployment)
                        {
                            if (!listLift.Contains(geoCharacter.Id))
                            {
                                listLift.Add(geoCharacter.Id);
                            }

                            if (listEntrance.Contains(geoCharacter.Id))
                            {
                                listEntrance.Remove(geoCharacter.Id);
                            }

                            if (listHangar.Contains(geoCharacter.Id))
                            {
                                listHangar.Remove(geoCharacter.Id);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ToggleButtonClicked(PhoenixGeneralButton checkButton, GeoRosterDeploymentItem geoRosterDeploymentItem)
                {
                    try
                    {
                        bool accessLift = _accessLift;
                        bool isVehicle = geoRosterDeploymentItem.Character.GameTags.Contains(Shared.SharedGameTags.VehicleClassTag)
                            || geoRosterDeploymentItem.Character.GameTags.Contains(Shared.SharedGameTags.MutogTag);

                        List<int> possibleLocations = new List<int> { 0 };

                        if (_timer > 12)
                        {
                            possibleLocations.Add(1);
                        }

                        if (accessLift && !isVehicle)
                        {
                            possibleLocations.Add(2);
                        }

                        int currentLocation = 0;

                        if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconEntrance)
                        {
                            currentLocation = 0;
                        }
                        else if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconHangar)
                        {
                            currentLocation = 1;
                        }
                        else if (checkButton.GetComponent<UIButtonIconController>().Icon.sprite == iconLift)
                        {
                            currentLocation = 2;
                        }

                        // Increment currentLocation within the bounds of possibleLocations
                        //   currentLocation = (currentLocation + 1) % possibleLocations.Count;

                        if (possibleLocations.Contains(currentLocation + 1))
                        {
                            currentLocation++;
                        }
                        else if (possibleLocations.Contains(currentLocation + 2))
                        {
                            currentLocation += 2;
                        }
                        else
                        {
                            currentLocation = 0;
                        }

                        switch (currentLocation)
                        {
                            case 0:
                                checkButton.GetComponent<UIButtonIconController>().Icon.sprite = iconEntrance;
                                break;
                            case 1:
                                checkButton.GetComponent<UIButtonIconController>().Icon.sprite = iconHangar;
                                break;
                            case 2:
                                checkButton.GetComponent<UIButtonIconController>().Icon.sprite = iconLift;
                                break;
                        }

                        AssignTeam(checkButton, geoRosterDeploymentItem);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void ModifyForBaseDefense(UIStateRosterDeployment uIStateRosterDeployment, List<GeoRosterDeploymentItem> deploymentItems)
                {
                    try
                    {
                        if (!PhoenixBasesUnderAttack.ContainsKey(uIStateRosterDeployment.Mission.Site.SiteId) && !PhoenixBasesInfested.Contains(uIStateRosterDeployment.Mission.Site.SiteId))
                        {
                            return;
                        }

                        SetBaseDefenseSitrep(uIStateRosterDeployment.Mission);

                        foreach (GeoRosterDeploymentItem geoRosterDeploymentItem in deploymentItems)
                        {
                            CreateCheckButton(geoRosterDeploymentItem);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static void SetBaseDefenseSitrep(GeoMission mission)
            {
                try
                {
                    _timer = CalculateBaseAttackProgress(mission.Site);
                    _accessLift = mission.Site.GetComponent<GeoPhoenixBase>().Layout.Facilities.Any(
                        f => f.Def.name.Equals("AccessLift_PhoenixFacilityDef") && !f.IsDamaged);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void ModifyMissionDataBaseDefense(GeoMission geoMission, TacMissionData missionData)
            {
                try
                {
                    if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId) || PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                    {

                        PPFactionDef alienFaction = DefCache.GetDef<PPFactionDef>("Alien_FactionDef");
                        int difficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(geoMission.GameController.CurrentDifficulty.Order);

                        ContextHelpHintDef hintDef = DefCache.GetDef<ContextHelpHintDef>("TFTVBaseDefense");

                        int timer = (int)_timer;


                        if (PandoransThatCanEscape.ContainsKey(geoMission.Site.SiteId))
                        {
                            foreach (string item in PandoransThatCanEscape[geoMission.Site.SiteId])
                            {
                                if (TFTVBaseDefenseTactical.PandoransInContainment.ContainsKey(item))
                                {
                                    TFTVBaseDefenseTactical.PandoransInContainment[item] += 1;
                                }
                                else
                                {
                                    TFTVBaseDefenseTactical.PandoransInContainment.Add(item, 1);
                                }
                            };
                        }

                        if (ContainmentBreachSchedule != null
                            && ContainmentBreachSchedule.ContainsKey(geoMission.Site.SiteId))
                        {
                            TFTVBaseDefenseTactical.Breach = true;

                            /*   var keysToModify = new List<string>(PandoransInContainment.Keys);
                               foreach (string key in keysToModify)
                               {
                                   PandoransInContainment[key] = true;
                               }*/

                            if (ContainmentBreachSchedule[geoMission.Site.SiteId])
                            {
                                TFTVBaseDefenseTactical.ScyllaLoose = true;
                            }
                        }

                        TFTVLogger.Always($"When modifying mission data, timer is {timer}");

                        string spriteFileName = "base_defense_hint.jpg";

                        if (timer < 12)
                        {
                            foreach (TacMissionFactionData tacMissionFactionData in missionData.MissionParticipants)
                            {
                                TFTVLogger.Always($"{tacMissionFactionData.FactionDef} {tacMissionFactionData.InitialDeploymentPoints}");

                                if (tacMissionFactionData.FactionDef == alienFaction)
                                {
                                    tacMissionFactionData.InitialDeploymentPoints *= 0.5f + (0.05f * difficulty);

                                    TFTVLogger.Always($"Deployment points changed to {tacMissionFactionData.InitialDeploymentPoints}");
                                }
                            }
                        }

                        if (TFTVBaseDefenseTactical.ScyllaLoose)
                        {
                            hintDef.Title.LocalizationKey = "BASEDEFENSE_SCYLLA_LOOSE_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_SCYLLA_LOOSE_DESCRIPTION";
                            spriteFileName = "BD_LooseScylla_hint.jpg";
                            //Add text + pic for Scylla Loose
                        }
                        else if (timer >= 12)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_TACTICAL_ADVANTAGE_DESCRIPTION";
                            spriteFileName = "base_defense_hint_small.jpg";
                        }
                        else if (timer < 12 && timer >= 6)
                        {

                            hintDef.Title.LocalizationKey = "BASEDEFENSE_NESTING_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_NESTING_DESCRIPTION";
                            spriteFileName = "base_defense_hint_nesting.jpg";
                        }
                        else
                        {
                            hintDef.Title.LocalizationKey = "BASEDEFENSE_INFESTATION_TITLE";
                            hintDef.Text.LocalizationKey = "BASEDEFENSE_INFESTATION_DESCRIPTION";
                            spriteFileName = "base_defense_hint_infestation.jpg";
                        }
                        TFTVHints._hintDefSpriteFileNameDictionary[hintDef] = spriteFileName;

                        TFTVBaseDefenseTactical.TimeLeft = timer;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class BaseFacilities
        {

            internal class EnsureCorrectLayout
            {

                //Patch and methods for demolition PhoenixFacilityConfirmationDialogue
                internal static List<Vector2Int> CheckAdjacency(GeoPhoenixFacility geoPhoenixFacility)
                {
                    try
                    {

                        List<Vector2Int> positionsFacilityOccupies = GetVectorsOccupiedByFacility(geoPhoenixFacility);

                        List<Vector2Int> adjacentTiles = new List<Vector2Int>();

                        foreach (Vector2Int position in positionsFacilityOccupies)
                        {

                            if (position.x + 1 <= 4)
                            {
                                if (!positionsFacilityOccupies.Contains(position + Vector2Int.right))
                                {
                                    adjacentTiles.Add(position + Vector2Int.right);
                                }
                                //     TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {position}, right is {position + Vector2Int.right}", false);

                            }
                            if (position.x - 1 >= 0)
                            {
                                if (!positionsFacilityOccupies.Contains(position + Vector2Int.left))
                                {
                                    adjacentTiles.Add(position + Vector2Int.left);
                                }
                                //  TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {position}, left is {position + Vector2Int.left}", false); 
                            }
                            if (position.y + 1 <= 4)
                            {
                                if (!positionsFacilityOccupies.Contains(position + Vector2Int.up))
                                {
                                    adjacentTiles.Add(position + Vector2Int.up);
                                }
                                //    TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {position}, down is {position + Vector2Int.up}", false);

                            }
                            if (position.y - 1 >= 0)
                            {
                                if (!positionsFacilityOccupies.Contains(position + Vector2Int.down))
                                {
                                    adjacentTiles.Add(position + Vector2Int.down);
                                }
                                //  TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {position}, up is {position + Vector2Int.down}", false);
                            }
                        }
                        return adjacentTiles;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static GeoPhoenixFacility GetFacilityAtPositionIncludingHangar(Vector2Int position, GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        foreach (GeoPhoenixFacility phoenixFacility in layout.Facilities)
                        {
                            if (GetVectorsOccupiedByFacility(phoenixFacility).Contains(position))
                            {
                                return phoenixFacility;
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


                internal static bool CheckConnectionToEntrance(GeoPhoenixBaseLayout layout, List<Vector2Int> tileToExclude, GeoPhoenixFacility baseFacility)
                {
                    try
                    {
                        PhoenixFacilityDef entranceFacilityDef = DefCache.GetDef<PhoenixFacilityDef>("Entrance_PhoenixFacilityDef");

                        GeoPhoenixFacility entrance = layout.Facilities.FirstOrDefault(bf => bf.Def == entranceFacilityDef);

                        List<Vector2Int> adjacentTiles = CheckAdjacency(baseFacility);

                        foreach (Vector2Int adjacentTile in adjacentTiles)
                        {
                            if (entrance.FacilityTiles.Contains(adjacentTile))
                            {
                                return true;
                            }
                            else if (GetFacilityAtPositionIncludingHangar(adjacentTile, layout) != null && !tileToExclude.Contains(adjacentTile))
                            {
                                // TFTVLogger.Always($"0, facility: {layout.GetFacilityAtPosition(adjacentTile).Def.name}");

                                foreach (Vector2Int connectedTile in CheckAdjacency(GetFacilityAtPositionIncludingHangar(adjacentTile, layout)))
                                {
                                    if (entrance.FacilityTiles.Contains(connectedTile))
                                    {
                                        //       TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");
                                        return true;

                                    }
                                    else if (GetFacilityAtPositionIncludingHangar(connectedTile, layout) != null && !tileToExclude.Contains(connectedTile))
                                    {
                                        //     TFTVLogger.Always($"1, facility: {layout.GetFacilityAtPosition(connectedTile).Def.name}");

                                        foreach (Vector2Int connectedTile2 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile, layout)))
                                        {
                                            if (entrance.FacilityTiles.Contains(connectedTile2))
                                            {
                                                // TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");
                                                return true;

                                            }
                                            else if (GetFacilityAtPositionIncludingHangar(connectedTile2, layout) != null && !tileToExclude.Contains(connectedTile2))
                                            {
                                                // TFTVLogger.Always($"2, facility: {layout.GetFacilityAtPosition(connectedTile2).Def.name}");

                                                foreach (Vector2Int connectedTile3 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile2, layout)))
                                                {
                                                    if (entrance.FacilityTiles.Contains(connectedTile3))
                                                    {
                                                        // TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");
                                                        return true;

                                                    }
                                                    else if (GetFacilityAtPositionIncludingHangar(connectedTile3, layout) != null && !tileToExclude.Contains(connectedTile3))
                                                    {
                                                        // TFTVLogger.Always($"3, facility: {layout.GetFacilityAtPosition(connectedTile3).Def.name}");
                                                        foreach (Vector2Int connectedTile4 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile3, layout)))
                                                        {
                                                            if (entrance.FacilityTiles.Contains(connectedTile4))
                                                            {
                                                                //       TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");
                                                                return true;

                                                            }
                                                            else if (GetFacilityAtPositionIncludingHangar(connectedTile4, layout) != null && !tileToExclude.Contains(connectedTile4))
                                                            {
                                                                // TFTVLogger.Always($"4, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                foreach (Vector2Int connectedTile5 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile4, layout)))
                                                                {
                                                                    if (entrance.FacilityTiles.Contains(connectedTile5))
                                                                    {
                                                                        //       TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                        return true;

                                                                    }
                                                                    else if (GetFacilityAtPositionIncludingHangar(connectedTile5, layout) != null && !tileToExclude.Contains(connectedTile5))
                                                                    {
                                                                        //      TFTVLogger.Always($"5, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                        foreach (Vector2Int connectedTile6 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile5, layout)))
                                                                        {
                                                                            if (entrance.FacilityTiles.Contains(connectedTile6))
                                                                            {
                                                                                //         TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                return true;

                                                                            }
                                                                            else if (GetFacilityAtPositionIncludingHangar(connectedTile6, layout) != null && !tileToExclude.Contains(connectedTile6))
                                                                            {
                                                                                //         TFTVLogger.Always($"6, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                foreach (Vector2Int connectedTile7 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile6, layout)))
                                                                                {
                                                                                    if (entrance.FacilityTiles.Contains(connectedTile7))
                                                                                    {
                                                                                        //              TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                        return true;

                                                                                    }
                                                                                    else if (GetFacilityAtPositionIncludingHangar(connectedTile7, layout) != null && !tileToExclude.Contains(connectedTile7))
                                                                                    {
                                                                                        //            TFTVLogger.Always($"7, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                        foreach (Vector2Int connectedTile8 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile7, layout)))
                                                                                        {
                                                                                            if (entrance.FacilityTiles.Contains(connectedTile8))
                                                                                            {
                                                                                                //              TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                                return true;

                                                                                            }
                                                                                            else if (GetFacilityAtPositionIncludingHangar(connectedTile8, layout) != null && !tileToExclude.Contains(connectedTile8))
                                                                                            {
                                                                                                //         TFTVLogger.Always($"8, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                                foreach (Vector2Int connectedTile9 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile8, layout)))
                                                                                                {
                                                                                                    if (entrance.FacilityTiles.Contains(connectedTile9))
                                                                                                    {
                                                                                                        //              TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                                        return true;

                                                                                                    }
                                                                                                    else if (GetFacilityAtPositionIncludingHangar(connectedTile9, layout) != null && !tileToExclude.Contains(connectedTile9))
                                                                                                    {
                                                                                                        //           TFTVLogger.Always($"9, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                                        foreach (Vector2Int connectedTile10 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile9, layout)))
                                                                                                        {
                                                                                                            if (entrance.FacilityTiles.Contains(connectedTile10))
                                                                                                            {
                                                                                                                //                   TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                                                return true;

                                                                                                            }
                                                                                                            else if (GetFacilityAtPositionIncludingHangar(connectedTile10, layout) != null && !tileToExclude.Contains(connectedTile10))
                                                                                                            {
                                                                                                                //               TFTVLogger.Always($"10, facility: {layout.GetFacilityAtPosition(connectedTile4).Def.name}");
                                                                                                                foreach (Vector2Int connectedTile11 in CheckAdjacency(GetFacilityAtPositionIncludingHangar(connectedTile10, layout)))
                                                                                                                {
                                                                                                                    if (entrance.FacilityTiles.Contains(connectedTile11))
                                                                                                                    {
                                                                                                                        //                      TFTVLogger.Always($" final, {baseFacility.Def.name} is connected to {entrance.Def.name}");

                                                                                                                        return true;

                                                                                                                    }
                                                                                                                }
                                                                                                            }
                                                                                                        }
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static List<Vector2Int> GetVectorsOccupiedByFacility(GeoPhoenixFacility facility)
                {
                    try
                    {
                        List<Vector2Int> positionsFacilityOccupies = new List<Vector2Int>() { facility.GridPosition };

                        if (facility.FacilityTiles.Count == 4)
                        {
                            positionsFacilityOccupies.Add(facility.GridPosition + Vector2Int.right);
                            positionsFacilityOccupies.Add(facility.GridPosition + Vector2Int.up);
                            positionsFacilityOccupies.Add(facility.GridPosition + Vector2Int.right + Vector2Int.up);
                        }

                        return positionsFacilityOccupies;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                [HarmonyPatch(typeof(UIFacilityInfoPopup), "Show")]
                public static class UIFacilityInfoPopup_Show_PreventBadDemolition_patch
                {

                    public static void Postfix(UIFacilityInfoPopup __instance, GeoPhoenixFacility facility)
                    {
                        try
                        {
                            // TFTVLogger.Always($"facility is {facility.ViewElementDef.name} at {facility.GridPosition} and it can't be demolished? {facility.CannotDemolish}");

                            PhoenixFacilityDef entranceFacilityDef = DefCache.GetDef<PhoenixFacilityDef>("Entrance_PhoenixFacilityDef");

                            List<Vector2Int> positionsToExclude = GetVectorsOccupiedByFacility(facility);

                            List<Vector2Int> adjacentTiles = CheckAdjacency(facility);

                            GeoPhoenixBaseLayout layout = facility.PxBase.Layout;

                            foreach (GeoPhoenixFacility baseFacility in layout.Facilities.Where(bf => bf != facility && bf.Def != entranceFacilityDef))
                            {
                                //  TFTVLogger.Always($"considering {baseFacility.Def.name}");

                                if (CheckConnectionToEntrance(layout, positionsToExclude, baseFacility))
                                {
                                    // TFTVLogger.Always($"{baseFacility.Def.name} at {baseFacility.GridPosition} is connected to Hangar");
                                }
                                else
                                {
                                    // connectionToHangarLoss = true;
                                    //    TFTVLogger.Always($"if {facility.Def.name} at {facility.GridPosition} is demolished, {baseFacility.Def.name} at {baseFacility.GridPosition} will lose connection to Hangar");
                                    __instance.DemolishFacilityBtn.SetInteractable(false);
                                    //  __instance.CanDemolish = false;
                                    __instance.Description.text += $"\n\n{TFTVCommonMethods.ConvertKeyToString("KEY_CANT_DEMOLISH")}";

                                    return;
                                }
                            }
                            if (!facility.CannotDemolish)
                            {
                                __instance.DemolishFacilityBtn.SetInteractable(true);
                            }

                            // TFTVLogger.Always($"{facility.Def.name} is demolished, some facility will lose connection to Hangar is {connectionToHangarLoss}");
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }

            }

            internal class AddressBadLayout
            {
                private static void RemoveFakeFacilities(GeoPhoenixBaseLayout layout)
                {

                    try
                    {
                        PhoenixFacilityDef fakeFacility = DefCache.GetDef<PhoenixFacilityDef>("FakeFacility");

                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.Where(f => f.Def == fakeFacility).ToList();

                        if (geoPhoenixFacilities.Count > 0)
                        {
                            for (int x = 0; x < geoPhoenixFacilities.Count(); x++)
                            {
                                layout.RemoveFacility(geoPhoenixFacilities[x]);
                            }

                            TFTVLogger.Always($"removing fake facilities");
                        }

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static List<GeoPhoenixFacility> SetFacilitiesUnderConstructionToCompleted(GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.Where(f => f.State == GeoPhoenixFacility.FacilityState.UnderContstruction).ToList();

                        PropertyInfo stateProperty = typeof(GeoPhoenixFacility).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);


                        if (geoPhoenixFacilities.Count > 0)
                        {
                            TFTVLogger.Always($"Facilities under construction at base under attack detected; setting to damaged");

                            foreach (GeoPhoenixFacility facility in geoPhoenixFacilities)
                            {
                                stateProperty.SetValue(facility, GeoPhoenixFacility.FacilityState.Damaged);
                            }


                        }

                        return geoPhoenixFacilities;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void RestoreFacilityState(List<GeoPhoenixFacility> geoPhoenixFacilities)
                {
                    try
                    {

                        PropertyInfo stateProperty = typeof(GeoPhoenixFacility).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (geoPhoenixFacilities.Count > 0)
                        {
                            TFTVLogger.Always($"Restoring faciliites");

                            foreach (GeoPhoenixFacility facility in geoPhoenixFacilities)
                            {
                                stateProperty.SetValue(facility, GeoPhoenixFacility.FacilityState.UnderContstruction);
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                [HarmonyPatch(typeof(GeoPhoenixBaseLayout), "ModifyMissionData")]
                public static class TFTV_GeoPhoenixBaseLayout_ModifyMissionData_patch
                {

                    public static void Prefix(GeoPhoenixBaseLayout __instance, GeoMission mission, TacMissionData missionData, out List<GeoPhoenixFacility> __state)
                    {
                        try
                        {
                            //this is still necessary

                            __state = SetFacilitiesUnderConstructionToCompleted(__instance);


                            //This should not be necessary anymore

                            /*     TFTVLogger.Always("ModifyMissionData");
                                 if ((PhoenixBasesUnderAttack.ContainsKey(mission.Site.SiteId) || PhoenixBasesInfested.Contains(mission.Site.SiteId)) && !CheckIfBaseLayoutOK(__instance))
                                 {
                                     TFTVLogger.Always("Bad layout!");
                                     FixBadLayout(__instance);
                                 }*/

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                    public static void Postfix(GeoPhoenixBaseLayout __instance, GeoMission mission, TacMissionData missionData, in List<GeoPhoenixFacility> __state)
                    {
                        try
                        {
                            RestoreFacilityState(__state);
                            //  RemoveFakeFacilities(mission.Site.GetComponent<GeoPhoenixBase>().Layout);
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                /* private static bool CheckIfBaseLayoutOK(GeoPhoenixBaseLayout layout)
                 {
                     try
                     {
                         List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.ToList();
                         GeoPhoenixFacility hangar = layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);

                         GeoPhoenixFacility powerGenerator = geoPhoenixFacilities.FirstOrDefault(f => f.GetComponent<PowerFacilityComponent>() != null);

                         if (powerGenerator != null && powerGenerator.HealthPercentage == 0)
                         {
                             FieldInfo fieldInfo = typeof(GeoPhoenixFacility).GetField("_health", BindingFlags.NonPublic | BindingFlags.Instance);
                             if (fieldInfo != null)
                             {
                                 fieldInfo.SetValue(powerGenerator, 50);
                                 TFTVLogger.Always($"{powerGenerator.HealthPercentage}");
                             }
                         }



                         foreach (GeoPhoenixFacility geoPhoenixFacility in geoPhoenixFacilities)
                         {
                             TFTVLogger.Always($"{geoPhoenixFacility.Def.name} at {geoPhoenixFacility.GridPosition}");

                         }

                         if (hangar.GridPosition.y == 0)
                         {
                             TFTVBaseDefenseTactical.TutorialPhoenixBase = true;

                             return true;
                         }


                         if (layout.GetFacilityAtPosition(hangar.GridPosition - new Vector2Int(0, 1)) != null || layout.GetFacilityAtPosition(hangar.GridPosition - new Vector2Int(-1, 1)) != null)
                         {
                             return true;

                         }
                         else
                         {

                             TFTVLogger.Always($"there are no facilities at grid {hangar.GridPosition - new Vector2Int(0, 1)} or {hangar.GridPosition - new Vector2Int(-1, 1)}");
                             return false;

                         }

                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }*/

                private static void FixBadLayout(GeoPhoenixBaseLayout layout)
                {
                    try
                    {
                        PhoenixFacilityDef fakeFacility = DefCache.GetDef<PhoenixFacilityDef>("FakeFacility");

                        List<GeoPhoenixFacility> geoPhoenixFacilities = layout.Facilities.ToList();
                        GeoPhoenixFacility hangar = layout.BasicFacilities.FirstOrDefault(bf => bf.FacilityTiles.Count > 1);

                        if (layout.CanPlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(0, 1)))
                        {
                            layout.PlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(0, 1), false);
                        }

                        if (layout.CanPlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(-1, 1)))
                        {
                            layout.PlaceFacility(fakeFacility, hangar.GridPosition - new Vector2Int(-1, 1), false);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



            }

            public static void DisableFacilitiesAtBase(GeoPhoenixBase phoenixBase)
            {
                try
                {
                    foreach (GeoPhoenixFacility facility in phoenixBase.Layout.Facilities)
                    {
                        if (facility.GetComponent<HealFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
                        }
                        if (facility.GetComponent<ExperienceFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
                        }
                        if (facility.GetComponent<VehicleSlotFacilityComponent>() != null)
                        {
                            facility.SetPowered(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class Infestation
        {
            //Patch to replace infestation mission with base defense mission
            [HarmonyPatch(typeof(GeoSite), "CreatePhoenixBaseInfestationMission")]
            public static class GeoSite_CreatePhoenixBaseInfestationMission_Experiment_patch
            {
                public static bool Prefix(GeoSite __instance)
                {
                    try
                    {
                        if (PhoenixBasesInfested.Contains(__instance.SiteId))
                        {
                            TFTVLogger.Always($"GeoSite.CreatePhoenixBaseInfestationMission");

                            GeoMissionGenerator.ParticipantFilter participantFilter = new GeoMissionGenerator.ParticipantFilter { Faction = __instance.GeoLevel.SharedData.AlienFactionDef, ParticipantType = TacMissionParticipant.Intruder };
                            TacMissionTypeDef mission = __instance.GeoLevel.MissionGenerator.GetRandomMission(__instance.GeoLevel.SharedData.SharedGameTags.BaseDefenseMissionTag, participantFilter);

                            GeoPhoenixBaseInfestationMission activeMission = new GeoPhoenixBaseInfestationMission(mission, __instance);
                            __instance.SetActiveMission(activeMission);

                            FieldInfo basesField = AccessTools.Field(typeof(GeoPhoenixFaction), "_bases");
                            List<GeoPhoenixBase> bases = (List<GeoPhoenixBase>)basesField.GetValue(__instance.GeoLevel.PhoenixFaction);
                            bases.Remove(__instance.GetComponent<GeoPhoenixBase>());

                            __instance.RefreshVisuals();

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

        }

        internal class Debriefing
        {

            [HarmonyPatch(typeof(PhoenixBaseDefenseOutcomeDataBind), "ModalShowHandler")]
            public static class PhoenixBaseDefenseOutcomeDataBind_ModalShowHandler_Experiment_patch
            {
                public static bool Prefix(UIModal modal, ref bool ____shown, ref UIModal ____modal, PhoenixBaseDefenseOutcomeDataBind __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"Defense mission outcome showing.");
                        MissionTagDef pxBaseInfestationTag = DefCache.GetDef<MissionTagDef>("MissionTypeBaseInfestation_MissionTagDef");

                        GeoMission geoMission = (GeoMission)modal.Data;

                        if (geoMission.GetEnemyFaction().PPFactionDef != Shared.AlienFactionDef)
                        {
                            return true;
                        }

                        if (!geoMission.MissionDef.MissionTags.Contains(pxBaseInfestationTag))
                        {

                            if (!____shown)
                            {

                                if (____modal == null)
                                {
                                    ____modal = modal;
                                    ____modal.OnModalHide += __instance.ModalHideHandler;
                                }

                                ____shown = true;

                                if (geoMission.GetMissionOutcomeState() == TacFactionState.Won)
                                {
                                    TFTVLogger.Always("Defense mission won");

                                    __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;
                                    __instance.Background.sprite = Helper.CreateSpriteFromImageFile("BG_Intro_1.jpg");
                                    __instance.Rewards.SetReward(geoMission.Reward);



                                    if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId))
                                    {
                                        PhoenixBasesUnderAttack.Remove(geoMission.Site.SiteId);

                                        if (PhoenixBasesUnderAttackSchedule.ContainsKey(geoMission.Site.SiteId))
                                        {
                                            PhoenixBasesUnderAttackSchedule.Remove(geoMission.Site.SiteId);
                                        }

                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                    }

                                    if (PandoransThatCanEscape.ContainsKey(geoMission.Site.SiteId))
                                    {
                                        PandoransThatCanEscape.Remove(geoMission.Site.SiteId);
                                    }

                                    if (PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                                    {

                                        PhoenixBasesInfested.Remove(geoMission.Site.SiteId);
                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                        //  geoMission.Level.PhoenixFaction.ActivatePhoenixBase(geoMission.Site, true);



                                        FieldInfo basesField = AccessTools.Field(typeof(GeoPhoenixFaction), "_bases");
                                        List<GeoPhoenixBase> bases = (List<GeoPhoenixBase>)basesField.GetValue(geoMission.Level.PhoenixFaction);
                                        bases.Add(geoMission.Site.GetComponent<GeoPhoenixBase>());
                                        geoMission.Site.RefreshVisuals();


                                    }

                                    geoMission.Site.RefreshVisuals();
                                    TFTVVanillaFixes.CheckFacilitesNotWorking(geoMission.Site.GetComponent<GeoPhoenixBase>());


                                }
                                else
                                {
                                    TFTVLogger.Always("Defense mission vs aliens lost");

                                    InitAttack.ContainmentBreach.ImplementContaimentBreach(geoMission.Site);

                                    if (!PhoenixBasesInfested.Contains(geoMission.Site.SiteId))
                                    {
                                        TFTVLogger.Always($"{geoMission.Site.SiteId} should get added to infested bases");
                                        PhoenixBasesInfested.Add(geoMission.Site.SiteId);
                                    }
                                    if (PhoenixBasesUnderAttack.ContainsKey(geoMission.Site.SiteId))
                                    {
                                        PhoenixBasesUnderAttack.Remove(geoMission.Site.SiteId);

                                        if (PhoenixBasesUnderAttackSchedule.ContainsKey(geoMission.Site.SiteId))
                                        {
                                            PhoenixBasesUnderAttackSchedule.Remove(geoMission.Site.SiteId);
                                        }

                                        GeoObjective.RemoveBaseDefenseObjective(geoMission.Site.LocalizedSiteName);
                                    }
                                    __instance.TopBar.Subtitle.text = geoMission.Site.LocalizedSiteName;
                                    __instance.Background.sprite = Helper.CreateSpriteFromImageFile("base_defense_lost.jpg");
                                    __instance.Rewards.SetReward(geoMission.Reward);

                                    GeoPhoenixBase geoPhoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();
                                    geoMission.Site.Owner = geoMission.Site.GeoLevel.AlienFaction;
                                    geoMission.Site.CreatePhoenixBaseInfestationMission();

                                    Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                                    description.GetComponent<I2.Loc.Localize>().enabled = false;
                                    LocalizedTextBind descriptionText = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TEXT" };
                                    description.text = descriptionText.Localize();
                                    Text title = __instance.TopBar.Title;
                                    title.GetComponent<I2.Loc.Localize>().enabled = false;
                                    title.text = new LocalizedTextBind() { LocalizationKey = "BASEDEFENSE_DEFEAT_TITLE" }.Localize();

                                    if (geoMission.Site.CharactersCount > 0)
                                    {
                                        List<GeoSite> phoenixBases = new List<GeoSite>();

                                        foreach (GeoPhoenixBase phoenixBase in geoMission.Site.GeoLevel.PhoenixFaction.Bases)
                                        {
                                            // TFTVLogger.Always($"Phoenix base is {phoenixBase.Site.LocalizedSiteName}");
                                            phoenixBases.Add(phoenixBase.Site);

                                        }

                                        phoenixBases.Remove(geoMission.Site);

                                        phoenixBases = phoenixBases.OrderBy(b => (b.WorldPosition - geoMission.Site.WorldPosition).magnitude).ToList();

                                        List<GeoCharacter> charactersToMove = new List<GeoCharacter>();

                                        foreach (GeoCharacter character in geoMission.Site.GetAllCharacters())
                                        {
                                            charactersToMove.Add(character);
                                            // TFTVLogger.Always($"character to move {character.DisplayName}");

                                        }

                                        foreach (GeoCharacter geoCharacter in charactersToMove)
                                        {
                                            geoMission.Site.RemoveCharacter(geoCharacter);
                                            phoenixBases.First().AddCharacter(geoCharacter);
                                            //  TFTVLogger.Always($"{geoCharacter.DisplayName} moved to {phoenixBases.First().LocalizedSiteName}");
                                            //  description.text += $" {geoCharacter.DisplayName} escaped to {phoenixBases.First().LocalizedSiteName}.";
                                        }

                                        for (int x = 0; x < charactersToMove.Count; x++)
                                        {
                                            if (x < charactersToMove.Count - 2)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName},";
                                            }
                                            else if (x == charactersToMove.Count - 2)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName} and";
                                            }
                                            else if (x == charactersToMove.Count - 1)
                                            {
                                                description.text += $" {charactersToMove[x].DisplayName} ";
                                            }

                                        }

                                        description.text += $"escaped to {phoenixBases.First().LocalizedSiteName}.";
                                    }

                                }
                            }
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



        }

        internal class Visuals
        {
            //Patch to create/manage visuals of attack on Phoenix base
            public static GeoUpdatedableMissionVisualsController GetBaseAttackVisuals(GeoSite phoenixBase)
            {
                try
                {
                    GeoUpdatedableMissionVisualsController missionVisualsController = null;


                    // Get the FieldInfo object representing the _visuals field
                    FieldInfo visualsField = typeof(GeoSite).GetField("_visuals", BindingFlags.Instance | BindingFlags.NonPublic);

                    // Get the value of the _visuals field using reflection
                    GeoSiteVisualsController visuals = (GeoSiteVisualsController)visualsField.GetValue(phoenixBase);


                    GeoUpdatedableMissionVisualsController[] visualsControllers = visuals.VisualsContainer.GetComponentsInChildren<GeoUpdatedableMissionVisualsController>();
                    missionVisualsController = visualsControllers.FirstOrDefault(vc => vc.gameObject.name == "kludge");

                    return missionVisualsController;
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            public static void RefreshBaseDefenseVisuals(GeoSiteVisualsController geoSiteVisualsController, GeoSite site)
            {
                try
                {
                    if (KludgeSite != null)
                    {
                        KludgeSite = null;

                    }

                    if (site.Type == GeoSiteType.PhoenixBase)
                    {
                        GeoUpdatedableMissionVisualsController missionVisualsController = GetBaseAttackVisuals(site);

                        if (missionVisualsController != null || PhoenixBasesUnderAttack.ContainsKey(site.SiteId))
                        {

                            if (PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController == null)
                            {
                                geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(true);
                                Color baseAttackTrackerColor = new Color32(192, 32, 32, 255);
                                geoSiteVisualsController.BaseIDText.gameObject.SetActive(false);

                                site.ExpiringTimerAt = TimeUnit.FromSeconds((float)(3600 * PhoenixBasesUnderAttack[site.SiteId].First().Value));
                                // TFTVLogger.Always($"saved time value is {TimeUnit.FromHours((float)PhoenixBasesUnderAttack[site.SiteId].First().Value)}");

                                GeoUpdatedableMissionVisualsController missionPrefab = GeoSiteVisualsDefs.Instance.HavenDefenseVisualsPrefab;
                                missionVisualsController = UnityEngine.Object.Instantiate(missionPrefab, geoSiteVisualsController.VisualsContainer);
                                missionVisualsController.name = "kludge";

                                double timeSpanHoursTimer = site.ExpiringTimerAt.TimeSpan.TotalHours;
                                double timeSpanHoursNow = site.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                                TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                                TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                                double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                                float totalTimeForAttack = 18;

                                if (PhoenixBasesUnderAttackSchedule.ContainsKey(site.SiteId))
                                {
                                    totalTimeForAttack = PhoenixBasesUnderAttackSchedule[site.SiteId];
                                }

                                float timer = (float)superClockTimer; //(site.ExpiringTimerAt.DateTime - site.GeoLevel.Timing.Now.DateTime).Hours;
                                                                      //   TFTVLogger.Always($"timer: {timer}");

                                if (timer > totalTimeForAttack)
                                {
                                    TFTVLogger.Always($"timer {timer} is higher than totalTimeForAttack ({totalTimeForAttack}), setting timer to totalTimeForAttack");
                                    timer = totalTimeForAttack;
                                }

                                float progress = 1f - timer / totalTimeForAttack;
                                //   TFTVLogger.Always($"timeToCompleteAttack is {timer}, total time for attack is {totalTimeForAttack} progress is {progress}");

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);
                                progressRenderer.gameObject.SetChildrenVisibility(true);

                                //  TFTVLogger.Always($"");

                                IGeoFactionMissionParticipant factionMissionParticipant = site.ActiveMission.GetEnemyFaction();

                                //   TFTVLogger.Always($"2");

                                IGeoFactionMissionParticipant owner = site.Owner;


                                //  TFTVLogger.Always($"3");
                                progressRenderer.material.SetColor("_SecondColor", factionMissionParticipant.ParticipantViewDef.FactionColor);
                                progressRenderer.material.SetColor("_FirstColor", site.GeoLevel.PhoenixFaction.FactionDef.FactionColor);
                                progressRenderer.material.SetFloat("_Progress", progress);
                                // TFTVBaseDefenseTactical.AttackProgress = progress;

                            }
                            else if (!PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {
                                TFTVLogger.Always("missionVisualsController found, though it's not active");
                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);


                                geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                                geoSiteVisualsController.BaseIDText.gameObject.SetActive(true);
                                missionVisualsController.gameObject.SetActive(false);

                            }
                            else if (PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController != null && missionVisualsController.name == "kludge")
                            {

                                var accessor = AccessTools.Field(typeof(GeoUpdatedableMissionVisualsController), "_progressRenderer");
                                MeshRenderer progressRenderer = (MeshRenderer)accessor.GetValue(missionVisualsController);
                                double timeSpanHoursTimer = site.ExpiringTimerAt.TimeSpan.TotalHours;
                                double timeSpanHoursNow = site.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                                //  TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                                //  TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                                double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                                float totalTimeForAttack = 18;

                                if (PhoenixBasesUnderAttackSchedule.ContainsKey(site.SiteId))
                                {
                                    totalTimeForAttack = PhoenixBasesUnderAttackSchedule[site.SiteId];
                                }

                                float timer = (float)superClockTimer; //(site.ExpiringTimerAt.DateTime - site.GeoLevel.Timing.Now.DateTime).Hours;
                                                                      //   TFTVLogger.Always($"timer: {timer}");

                                if (timer > totalTimeForAttack)
                                {
                                    TFTVLogger.Always($"timer {timer} is higher than totalTimeForAttack ({totalTimeForAttack}), setting timer to totalTimeForAttack");
                                    timer = totalTimeForAttack;
                                }

                                float progress = 1f - timer / totalTimeForAttack;
                                //  TFTVLogger.Always($"timeToCompleteAttack is {timer}, total time for attack is {totalTimeForAttack} progress is {progress}");

                                progressRenderer.material.SetFloat("_Progress", progress);

                                if (progress >= 1)
                                {
                                    KludgeSite = site;
                                    TFTVLogger.Always("Progress 1 reached!");
                                    MethodInfo registerMission = typeof(GeoSite).GetMethod("RegisterMission", BindingFlags.NonPublic | BindingFlags.Instance);

                                    TFTVLogger.Always($"registerMission null?? {registerMission == null} site has no active mission?? {site.ActiveMission == null}");

                                    if (registerMission != null && site.ActiveMission != null)
                                    {
                                        registerMission.Invoke(site, new object[] { site.ActiveMission });
                                    }

                                    geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                                    geoSiteVisualsController.BaseIDText.gameObject.SetActive(true);
                                    missionVisualsController.gameObject.SetActive(false);

                                }
                                else if (timer < 12)//progress >= 0.3 || totalTimeForAttack < 12)
                                {
                                    // TFTVLogger.Always($"Deactivating facilities at base {site.LocalizedSiteName}");

                                    BaseFacilities.DisableFacilitiesAtBase(site.GetComponent<GeoPhoenixBase>());
                                }


                                // TFTVBaseDefenseTactical.AttackProgress = progress;
                            }
                        }
                        if (site.ExpiringTimerAt != null && !PhoenixBasesUnderAttack.ContainsKey(site.SiteId) && missionVisualsController == null)
                        {
                            geoSiteVisualsController.TimerController.gameObject.SetChildrenVisibility(false);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleSiteContextualMenu), "SetMenuItems")]
        public static class UIModuleSiteContextualMenu_SetMenuItems_patch
        {
            public static void Postfix(GeoSite site, List<GeoAbility> rawAbilities, Vector3 position, UIModuleSiteContextualMenu __instance)
            {
                try
                {
                    if (site.GetComponent<GeoPhoenixBase>() != null && site.ActiveMission != null && site.CharactersCount > 0 &&
                        (site.Vehicles.Count() == 0 || !site.Vehicles.Any(v => v.GetCharacterCount() > 0)))
                    {

                        FieldInfo fieldInfoListSiteContextualMenuItem = typeof(UIModuleSiteContextualMenu).GetField("_menuItems", BindingFlags.NonPublic | BindingFlags.Instance);
                        List<SiteContextualMenuItem> menuItems = fieldInfoListSiteContextualMenuItem.GetValue(__instance) as List<SiteContextualMenuItem>;

                        foreach (SiteContextualMenuItem menuItem in menuItems)
                        {
                            // TFTVLogger.Always($"menu item: {menuItem?.ItemText?.text}");
                            if (menuItem.ItemText.text == DefCache.GetDef<EnterBaseAbilityDef>("EnterBaseAbilityDef").ViewElementDef.DisplayName1.Localize())
                            {
                                menuItem.ItemText.text = TFTVCommonMethods.ConvertKeyToString("KEY_DEPLOY_BASE_DEFENSE_TEXT");
                                menuItem.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_DEPLOY_BASE_DEFENSE_TIP");
                                // TFTVLogger.Always($"menu item: {menuItem?.ItemText?.text}");
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
        }

        [HarmonyPatch(typeof(EnterBaseAbility), "ActivateInternal")]
        public static class EnterBaseAbility_ActivateInternal_patch
        {
            public static bool Prefix(EnterBaseAbility __instance, GeoAbilityTarget target)
            {
                try
                {
                    TFTVLogger.Always($"ActivateInternalfor ability {__instance.GeoscapeAbilityDef.name}");

                    if (target.Actor is GeoSite site && site.ActiveMission != null && site.CharactersCount > 0
                        &&
                        (site.Vehicles.Count() == 0 || !site.Vehicles.Any(v => v.GetCharacterCount() > 0)))
                    {
                        TFTVLogger.Always($"Deploying Base Defense");
                        GeoSite geoSite = (GeoSite)target.Actor;
                        //  GeoVehicle initialContainer = (GeoVehicle)base.Actor;


                        __instance.GeoLevel.View.LaunchMission(geoSite.ActiveMission);
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

        //These patches cancel the listeners added by the Register Mission method used in the TFTVAAExperiment.cs to force the base defense
        //mission when timer expires
        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionCancel_Patch
        {
            [HarmonyPatch("add_OnMissionCancel")]
            [HarmonyPrefix]
            public static bool OnMissionCancelAdded(GeoMission __instance, Action<GeoMission> value)
            {
                try
                {

                    if (KludgeSite == __instance.Site) //&& TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionCancel invoked");

                        return false;
                        // ____missionCancelled = false;
                    }
                    //  }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }  // Your patch code here
            }
        }

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionActivated_Patch
        {
            [HarmonyPatch("add_OnMissionActivated")]
            [HarmonyPrefix]
            public static bool OnMissionActivatedAdded(GeoMission __instance, Action<GeoMission, PlayTacticalGameLevelResult> value)
            {
                try
                {
                    if (KludgeSite == __instance.Site)// && TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionActivated invoked");

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

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionPreApplyResult_Patch
        {
            [HarmonyPatch("add_OnMissionPreApplyResult")]
            [HarmonyPrefix]
            public static bool OnMissionPreApplyResultAdded(GeoMission __instance, Action<GeoMission> value)
            {
                try
                {


                    if (KludgeSite == __instance.Site) //&& TFTVAAExperiment.KludgeCheck)
                    {
                        TFTVLogger.Always("add_OnMissionPreApplyResult invoked");

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

        [HarmonyPatch(typeof(GeoMission))]
        public static class GeoMission_add_OnMissionCompleted_Patch
        {
            [HarmonyPatch("add_OnMissionCompleted")]
            [HarmonyPrefix]
            public static bool OnMissionCompletedAdded(GeoMission __instance, Action<GeoMission, GeoFactionReward> value)
            {
                try
                {
                    if (KludgeSite == __instance.Site)
                    {
                        TFTVLogger.Always("add_OnMissionCompleted invoked");
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


        //Patches to close deployment screen if no characters present
        [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
        public static class TFTV_UIStateRosterDeployment_EnterState_BaseDefenseGeo_patch
        {
            public static bool Prefix(UIStateRosterDeployment __instance)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    MissionTypeTagDef ancientSiteDefense = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef");

                    if (__instance.Mission != null)
                    {
                        GeoMission geoMission = __instance.Mission;
                        GeoSite geoSite = __instance.Mission.Site;

                        if ((geoSite.Type == GeoSiteType.AncientHarvest || geoSite.Type == GeoSiteType.AncientRefinery) && geoMission.MissionDef.MissionTags.Contains(ancientSiteDefense))
                        {
                            if (geoSite.ActiveMission != null)
                            {
                                geoSite.ActiveMission.Launch(new GeoSquad() { });
                                return false;
                            }
                        }
                    }

                    return true;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;

                }
            }


            public static void Postfix(UIStateRosterDeployment __instance)
            {
                try
                {


                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();



                    UIModuleActorCycle uIModuleActorCycle = controller.View.GeoscapeModules.ActorCycleModule;
                    UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                    if (uIModuleActorCycle.CurrentCharacter == null)
                    {
                        Type uiStateType = typeof(UIStateRosterDeployment);
                        MethodInfo exitStateMethod = uiStateType.GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance);
                        exitStateMethod.Invoke(__instance, null);
                        TFTVLogger.Always("OnCancel exitState");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoMission), "get_SkipDeploymentSelection")]
        public static class GeoMission_get_SkipDeploymentSelection_BaseDefenseProgressOne_patch
        {
            public static void Postfix(GeoMission __instance)
            {
                try
                {
                    MissionTagDef pxBaseDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");


                    if (__instance.MissionDef.MissionTags.Contains(pxBaseDefenseTag))
                    {
                        GeoSite phoenixBase = __instance.Site;

                        if (!PhoenixBasesUnderAttackSchedule.ContainsKey(phoenixBase.SiteId))
                        {
                            return;

                        }

                        double timeSpanHoursTimer = phoenixBase.ExpiringTimerAt.TimeSpan.TotalHours;
                        double timeSpanHoursNow = phoenixBase.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                        //  TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                        //  TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                        double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                        float totalTimeForAttack = 18;

                        if (PhoenixBasesUnderAttackSchedule.ContainsKey(phoenixBase.SiteId))
                        {
                            totalTimeForAttack = PhoenixBasesUnderAttackSchedule[phoenixBase.SiteId];
                        }

                        float timer = (float)superClockTimer; //(site.ExpiringTimerAt.DateTime - site.GeoLevel.Timing.Now.DateTime).Hours;
                        TFTVLogger.Always($"timer: {timer}");

                        float progress = 1f - timer / totalTimeForAttack;

                        TFTVLogger.Always($"attack progress is {progress}");

                        List<IGeoCharacterContainer> characterContainers = __instance.GetDeploymentSources(__instance.Site.Owner);

                        IEnumerable<GeoCharacter> deployment = characterContainers.SelectMany((IGeoCharacterContainer s) => s.GetAllCharacters());

                        if (deployment.Count() == 0 && progress >= 1)
                        {
                            __instance.Cancel();
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModal), "Cancel")]
        public static class UIModal_Cancel_BaseInfestation_patch
        {
            public static bool Prefix(UIModal __instance, DialogCallback ____handler)
            {
                try
                {

                    //TFTVLogger.Always($"Showing modal {__instance.name}");

                    if (__instance.Data is GeoMission geoMission && __instance.name.Contains("Brief"))
                    {
                        //  TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission.GetMissionOutcomeState()}");
                        GeoSite geoSite = geoMission.Site;
                        //  TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method invoked.");
                        if (PhoenixBasesInfested.Contains(geoSite.SiteId))
                        {
                            TFTVLogger.Always("GeoPhoenixBaseDefense(Infestation)Mission Cancel method canceled.");

                            ____handler(ModalResult.Close);

                            return false;
                        }
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

        [HarmonyPatch(typeof(GeoMission), "Cancel")]
        public static class GeoMission_Cancel_BaseInfestation_patch
        {
            public static bool Prefix(GeoMission __instance)
            {
                try
                {
                    GeoSite geoSite = __instance.Site;


                    //  TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method invoked.");
                    if (PhoenixBasesInfested.Contains(geoSite.SiteId))
                    {
                        TFTVLogger.Always("GeoPhoenixBaseDefense(Infestation)Mission Cancel method canceled.");

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

        //Patch not to remove base defense mission when it is canceled

        [HarmonyPatch(typeof(GeoPhoenixBaseDefenseMission), "Cancel")]
        public static class GeoPhoenixBaseDefenseMission_Cancel_Experiment_patch
        {
            public static bool Prefix(GeoPhoenixBaseDefenseMission __instance)
            {
                try
                {

                    GeoSite phoenixBase = __instance.Site;

                    double timeSpanHoursTimer = phoenixBase.ExpiringTimerAt.TimeSpan.TotalHours;
                    double timeSpanHoursNow = phoenixBase.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                    //  TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                    //  TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                    double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                    float totalTimeForAttack = 18;

                    if (PhoenixBasesUnderAttackSchedule.ContainsKey(phoenixBase.SiteId))
                    {
                        totalTimeForAttack = PhoenixBasesUnderAttackSchedule[phoenixBase.SiteId];
                    }

                    float timer = (float)superClockTimer; //(site.ExpiringTimerAt.DateTime - site.GeoLevel.Timing.Now.DateTime).Hours;
                    TFTVLogger.Always($"timer: {timer}");

                    float progress = 1f - timer / totalTimeForAttack;

                    TFTVLogger.Always($"GeoPhoenixBaseDefenseMission Cancel method invoked. Progress is {progress}");

                    if (PhoenixBasesUnderAttack.ContainsKey(phoenixBase.SiteId) && progress < 1)
                    {
                        TFTVLogger.Always("GeoPhoenixBaseDefenseMission Cancel method canceled.");

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


        //Patches to prevent recruiting to a base under attack
        [HarmonyPatch(typeof(RecruitsBaseDeployElementController), "SetBaseElement")]
        public static class RecruitsBaseDeployElementController_SetBasesForDeployment_BaseDefense_patch
        {
            public static void Postfix(bool isBaseUnderAttack, RecruitsBaseDeployElementController __instance)
            {
                try
                {
                    if (isBaseUnderAttack)
                    {
                        __instance.BaseButton.SetInteractable(false);
                    }
                    else if (!__instance.BaseButton.IsInteractable())
                    {
                        __instance.BaseButton.SetInteractable(true);

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(RecruitsBaseDeployData), "get_IsOperational")]
        public static class RecruitsBaseDeployData_SetBasesForDeployment_BaseDefense_patch
        {
            public static void Postfix(ref bool __result, RecruitsBaseDeployData __instance)
            {
                try
                {

                    if (PhoenixBasesUnderAttack.ContainsKey(__instance.PhoenixBase.Site.SiteId)
                        || PhoenixBasesInfested.Contains(__instance.PhoenixBase.Site.SiteId))
                    {
                        __result = false;

                    }
                    else
                    {
                        __result = true;

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleGeoAssetDeployment), "ShowDeployDialog")]
        public static class UIModuleGeoAssetDeployment_ShowDeployDialog_BaseDefense_patch
        {


            public static void Prefix(UIModuleGeoAssetDeployment __instance, ref IEnumerable<GeoSite> sites)
            {
                try
                {
                    // Create a modified collection of sites without the ones under attack
                    List<GeoSite> modifiedSites = new List<GeoSite>(sites);
                    modifiedSites.RemoveAll(site => PhoenixBasesUnderAttack.ContainsKey(site.SiteId));
                    modifiedSites.RemoveAll(site => PhoenixBasesInfested.Contains(site.SiteId));

                    sites = modifiedSites;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }


        private static bool AbilityBought = false;


       /* [HarmonyPatch(typeof(UIModuleCharacterProgression), "BuyAbility")]
        public static class UIModuleCharacterProgression_BuyAbility_patch
        {
            public static void Postfix(UIStateEditSoldier __instance)
            {
                try
                {
                        AbilityBought = true;
                    TFTVLogger.Always($"UIModuleCharacterProgression.BuyAbility; ability bought: {AbilityBought}");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }*/


       

        //Patch to close mission briefings for missions not in play (the Vanilla double mission bug)
        //and also to close Deploy screen if there are no characters to deploy to mission
        [HarmonyPatch(typeof(UIModal), "Show")]
        public static class UIModal_Show_DoubleMissionVanillaFixAndBaseDefense_patch
        {

            public static void Postfix(UIModal __instance, object data)
            {
                try
                {
                    GeoscapeView geoscapeView = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View;

                    UIModuleModal uIModuleModal = geoscapeView.GeoscapeModules.ModalModule;


                    TFTVLogger.Always($"Showing modal {__instance.name}, " +
                        $"{__instance.GetComponent<ModalType>().GetName()}, ability bought:{AbilityBought}, {geoscapeView.MainUILayer.ActiveState.Name}");



                    if (__instance.name.Contains("Confirm_CharacterProgression") && __instance.GetComponent<ModalType>() == ModalType.GeoHavenAttackBrief)
                    {
                       
                     //  AbilityBought = true;
                      TFTVLogger.Always($"Character Progression in GeoHavenAttack; ability bought: {AbilityBought}, {data?.GetType()?.Name}");
                    }


                    if (data is GeoMission geoMission && __instance.name.Contains("Brief"))
                    {
                        TFTVLogger.Always($"data is GeoMission and the mission state is {geoMission.GetMissionOutcomeState()}");
                        GeoSite geoSite = geoMission.Site;

                        if (geoMission.GetMissionOutcomeState() != TacFactionState.Playing)
                        {
                            __instance.Close();
                            TFTVLogger.Always("Closing modal because mission is not in play");
                        }

                        if (AbilityBought && __instance.GetComponent<HavenDefenceBriefDataBind>() != null)
                        {

                            TFTVLogger.Always($"must be coming from character progression");
                            __instance.Cancel();
                            AbilityBought = false;
                        }

                        AbilityBought = false;

                        if (PhoenixBasesUnderAttack.ContainsKey(geoSite.SiteId) && geoSite.CharactersCount == 0)
                        {

                            double timeSpanHoursTimer = geoSite.ExpiringTimerAt.TimeSpan.TotalHours;
                            double timeSpanHoursNow = geoSite.GeoLevel.Timing.Now.TimeSpan.TotalHours;

                            //  TFTVLogger.Always($"timeSpanHoursTimer {timeSpanHoursTimer}");
                            //  TFTVLogger.Always($"timeSpanHoursNow {timeSpanHoursNow}");

                            double superClockTimer = timeSpanHoursTimer - timeSpanHoursNow;

                            float totalTimeForAttack = 18;

                            if (PhoenixBasesUnderAttackSchedule.ContainsKey(geoSite.SiteId))
                            {
                                totalTimeForAttack = PhoenixBasesUnderAttackSchedule[geoSite.SiteId];
                            }

                            float timer = (float)superClockTimer; //(site.ExpiringTimerAt.DateTime - site.GeoLevel.Timing.Now.DateTime).Hours;
                            TFTVLogger.Always($"timer: {timer}");

                            float progress = 1f - timer / totalTimeForAttack;

                            List<IGeoCharacterContainer> characterContainers = geoMission.GetDeploymentSources(geoSite.Owner);

                            IEnumerable<GeoCharacter> deployment = characterContainers.SelectMany((IGeoCharacterContainer s) => s.GetAllCharacters());


                            if (deployment.Count() == 0 && progress < 1)
                            {
                                __instance.Close();

                                TFTVLogger.Always("Closing modal because no troops to deploy in mission.");
                            }
                        }

                        MissionTypeTagDef ancientSiteDefense = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef");

                        if (geoMission.MissionDef.MissionTags.Contains(ancientSiteDefense))
                        {
                            Sprite sprite = Helper.CreateSpriteFromImageFile("cyclopsmission.jpg");
                            __instance.transform.GetComponentInChildren<Image>().sprite = sprite;

                            Text description = __instance.GetComponentInChildren<ObjectivesController>().Objectives;
                            description.GetComponent<I2.Loc.Localize>().enabled = false;
                            description.text = TFTVCommonMethods.ConvertKeyToString("PROTECT_THE_CYCLOPS");

                            Text subtitle = __instance.GetComponentInChildren<TopBarController>().Subtitle;
                            subtitle.GetComponent<I2.Loc.Localize>().enabled = false;
                            subtitle.text = geoSite.LocalizedSiteName;

                            CommonMissionDataController commonMissionDataController = __instance.GetComponentInChildren<CommonMissionDataController>();
                            Text enemyText = commonMissionDataController.EnemyText;
                            enemyText.GetComponent<I2.Loc.Localize>().enabled = false;
                            enemyText.text = geoSite.GeoLevel.AlienFaction.Name.Localize();

                            commonMissionDataController.InfectedAreaGroup.gameObject.SetActive(false);
                        }




                        /*  MissionTypeTagDef ancientSiteDefense = DefCache.GetDef<MissionTypeTagDef>("MissionTypeAncientSiteDefense_MissionTagDef");
                          if ((geoSite.Type == GeoSiteType.AncientHarvest || geoSite.Type == GeoSiteType.AncientRefinery) && geoMission.MissionDef.MissionTags.Contains(ancientSiteDefense))
                          {
                              if (geoSite.ActiveMission != null)
                              {
                                  geoSite.ActiveMission.Launch(new GeoSquad() { });
                              }
                              else 
                              {
                                  geoMission.Cancel();
                                  geoSite.Owner = geoSite.GeoLevel.PhoenixFaction;
                                  __instance.Close();
                                  TFTVLogger.Always("Closing modal because no Active Mission on Site.");
                              }
                          }*/

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Patch to create base defense mission even when no characters present at the base
        [HarmonyPatch(typeof(GeoPhoenixBase), "get_CanCreateBaseDefense")]
        public static class GeoPhoenixBase_get_CanCreateBaseDefense_Patch
        {
            public static void Postfix(GeoPhoenixBase __instance, ref bool __result)
            {
                try
                {
                    TFTVLogger.Always("get_CanCreateBaseDefense invoked");

                    if (PhoenixBasesUnderAttack.ContainsKey(__instance.Site.SiteId))
                    {
                        __result = true;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/
        /******STUFF BELOW NOT USED!!!********/


        /*  [HarmonyPatch(typeof(UIModal), "Show")]
          public static class PhoenixBaseDefenseDataBind_ModalShowHandler_Experiment_patch
          {
              private static bool previousConditionFulfilled = false;
              private static Vector3 originalCancelPos = new Vector3();
              private static string originalCancelText = "";
              private static string newStartMissionName = "StartButtonHidden";
              private static string newCancelName = "CancelButtonChanged";

              public static void Postfix(UIModal __instance, object data)
              {

                  try
                  {

                      if (data is GeoMission geoMission && __instance.name.Contains("Brief_PhoenixBaseDefense"))
                      {

                          GeoPhoenixBase phoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();

                          if (phoenixBase.SoldiersInBase.Count() == 0 && !geoMission.Site.Vehicles.Any(v => v.Soldiers.Count() > 0))
                          {

                              TFTVLogger.Always("");

                              PhoenixGeneralButton startMission = FindButtonByText("START MISSION");
                              PhoenixGeneralButton cancel = FindButtonByText("CANCEL");

                              if (startMission != null)
                              {
                                  startMission.gameObject.SetActive(false);
                                  startMission.gameObject.name = newStartMissionName;
                              }

                              if (cancel != null)
                              {
                                  originalCancelPos = cancel.gameObject.transform.position;
                                  originalCancelText = cancel.gameObject.GetComponentInChildren<Text>().text;
                                  cancel.gameObject.transform.position = startMission.gameObject.transform.position;
                                  cancel.gameObject.GetComponentInChildren<Text>().text = "Alrighty, will be back later";
                                  cancel.gameObject.name = newCancelName;
                                  previousConditionFulfilled = true;
                              }
                          }
                          else if (previousConditionFulfilled)
                          {
                              TFTVLogger.Always(", past previousConditionFulfilled");

                              PhoenixGeneralButton startMission = FindButtonByName(newStartMissionName);
                              PhoenixGeneralButton cancel = FindButtonByName(newCancelName);

                              TFTVLogger.Always($"buttons");

                              if (startMission != null)
                              {
                                  startMission.gameObject.SetActive(true);
                              }

                              if (cancel != null)
                              {
                                  cancel.gameObject.transform.position = originalCancelPos;
                                  cancel.gameObject.GetComponentInChildren<Text>().text = originalCancelText;
                              }

                              previousConditionFulfilled = false;
                          }
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }


              }

          }*/

        internal static PhoenixGeneralButton FindButtonByName(string name)
        {
            try
            {

                PhoenixGeneralButton result = null;

                PhoenixGeneralButton[] buttons = UnityEngine.Object.FindObjectsOfType<PhoenixGeneralButton>();

                foreach (PhoenixGeneralButton button in buttons)
                {

                    if (button.gameObject.name == name)
                    {

                        result = button;


                    }
                }

                return result;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }
        internal static PhoenixGeneralButton FindButtonByText(string ButtonText)
        {
            try
            {
                PhoenixGeneralButton result = null;

                PhoenixGeneralButton[] buttons = UnityEngine.Object.FindObjectsOfType<PhoenixGeneralButton>();

                foreach (PhoenixGeneralButton button in buttons)
                {
                    Text text = button.gameObject.GetComponentInChildren<Text>();

                    if (text != null)
                    {
                        TFTVLogger.Always($"text is {text.text}");
                        if (text.text == ButtonText)
                        {
                            // text.text = "SOMETHING NEW";
                            //button.gameObject.SetActive(false);
                            result = button;
                        }

                    }

                    // Do something with the PhoenixGeneralButton component
                }

                return result;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        /*  [HarmonyPatch(typeof(PhoenixBaseDefenseDataBind), "ModalShowHandler")]
          public static class PhoenixBaseDefenseDataBind_ModalShowHandler_Experiment_patch
          {
              public static void Postfix(UIModal modal, PhoenixBaseDefenseDataBind __instance)
              {
                  try
                  {
                      GeoMission geoMission = (GeoMission)modal.Data;
                      GeoPhoenixBase phoenixBase = geoMission.Site.GetComponent<GeoPhoenixBase>();

                      if (phoenixBase.SoldiersInBase.Count()==0 && !geoMission.Site.Vehicles.Any(v=>v.Soldiers.Count()>0))
                      {
                          TFTVLogger.Always("");

                          Button startMission = UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.name.Equals("UIMainButton_HPriority")).First();

                          Button cancel = UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.name.Equals("UI_Button_Back")).First();

                          cancel.gameObject.transform.position = startMission.gameObject.transform.position;
                          startMission.gameObject.SetActive(false);
                          cancel.gameObject.GetComponentInChildren<Text>().text = "Alrighty, will be back later";
                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/


        /*  foreach (Button button in buttons)
          {
              TFTVLogger.Always($"{button.name}");

              Component[] components = button.gameObject.GetComponentsInChildren<PhoenixGeneralButton>();

              foreach (Component component in components)
              {
                  PhoenixGeneralButton pgButton = component as PhoenixGeneralButton;
                  if (pgButton != null)
                  {
                      TFTVLogger.Always($"Found PhoenixGeneralButton component in {button.name}");

                      Text text = pgButton.gameObject.GetComponentInChildren<Text>();

                      if (text != null)
                      {
                          TFTVLogger.Always($"text is {text.text}");
                          if (text.text == "START MISSION")
                          {
                             // text.text = "SOMETHING NEW";
                              //button.gameObject.SetActive(false);
                              startMission = button;
                          }
                          if (text.text == "CANCEL")
                          {
                              //   text.text = "SOMETHING NEW";
                              //button.gameObject.SetActive(false);
                              cancel = button;
                          }
                      }

                      // Do something with the PhoenixGeneralButton component
                  }
              }
          }*/



    }
}
