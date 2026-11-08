using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV.TFTVVoidOmensExpansion
{
    /// <summary>
    /// The Void Omens that play out across the world map: fallen havens (VO25), factions at war in missions (VO26), raiding
    /// against trade (VO27), allies at points of interest (VO29), exploring in Mist (VO31) and haven defence rewards (VO35).
    /// </summary>
    internal static class WorldOmens
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        // VO25: a state per fallen haven, kept in an event variable so it needs no save data of its own.
        private const string VO25SiteVariable = "VO25_FallenHaven_";
        private const int VO25MissionPending = 1;
        private const int VO25MissionOpen = 2;
        private const int VO25DiplomacyCost = -6;

        private const float VO26ChanceOfFactionAtWar = 0.5f;

        private const float VO27InfiltrationFactionMultiplier = 2f;
        private const float VO27InfiltrationLeaderMultiplier = 3f;
        private const int VO27MinLeaderAttitude = 50;
        private const int VO27HavenGivesMultiplier = 3;
        private const int VO27HavenTakesMultiplier = 2;

        private const float VO29DailyChancePerFaction = 0.1f;
        private const int VO29MinResources = 50;
        private const int VO29MaxResources = 150;

        private const float VO31ExplorationTimeMultiplier = 1.5f;
        private const float VO31RewardMultiplier = 1.5f;

        private const float VO35RewardLostPerCivilian = 0.05f;
        private const float VO35RewardLostPerStructure = 0.1f;
        private const float VO35MinimumReward = 0.25f;

        private static void AddLogEntry(GeoLevelController controller, string text)
        {
            GeoscapeLogEntry entry = new GeoscapeLogEntry { Text = new LocalizedTextBind(text, true) };

            typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller.Log, new object[] { entry, null });
        }

        private static IEnumerable<GeoFaction> GetMajorFactions(GeoLevelController controller)
        {
            return new GeoFaction[] { controller.NewJerichoFaction, controller.AnuFaction, controller.SynedrionFaction };
        }

        internal static void OnDailyUpdate(GeoLevelController controller)
        {
            try
            {
                OpenFallenHavenMissions(controller);
                LetAlliesExplore(controller);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // ---- VO25 ----

        [HarmonyPatch(typeof(GeoSite), nameof(GeoSite.DestroySite))]
        internal static class GeoSite_DestroySite_VO25_Patch
        {
            private static void Postfix(GeoSite __instance)
            {
                try
                {
                    // Only a haven that really fell: a Prefix elsewhere turns some destructions into infestations.
                    if (TFTVVoidOmens.VoidOmensCheck[25] && __instance.Type == GeoSiteType.Haven && __instance.State == GeoSiteState.Destroyed
                        && __instance.GetComponent<GeoHaven>() != null)
                    {
                        __instance.GeoLevel.EventSystem.SetVariable(VO25SiteVariable + __instance.SiteId, VO25MissionPending);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>Daily, not at the destruction: the defence mission that destroyed the haven is still ending then.</summary>
        private static void OpenFallenHavenMissions(GeoLevelController controller)
        {
            foreach (GeoSite site in controller.Map.AllSites.Where(s => s.Type == GeoSiteType.Haven && s.State == GeoSiteState.Destroyed).ToList())
            {
                string variable = VO25SiteVariable + site.SiteId;

                if (controller.EventSystem.GetVariable(variable) != VO25MissionPending || site.ActiveMission != null)
                {
                    continue;
                }

                site.CreateScavengingMission(null, null);

                if (site.ActiveMission == null)
                {
                    continue;
                }

                controller.EventSystem.SetVariable(variable, VO25MissionOpen);
                site.RevealSite(controller.PhoenixFaction);
                site.RefreshVisuals();
                AddLogEntry(controller, TFTVCommonMethods.FormatKey("KEY_VOID_OMEN_25_SITE", site.LocalizedSiteName));
                TFTVLogger.Always($"VO25: scavenging mission at the ruins of {site.LocalizedSiteName}");
            }
        }

        /// <summary>The cost is paid once, when the mission is launched, even if the omen has ended since it opened.</summary>
        [HarmonyPatch(typeof(GeoMission), "PrepareTacticalGame")]
        internal static class GeoMission_PrepareTacticalGame_VO25_Patch
        {
            private static void Prefix(GeoMission __instance)
            {
                try
                {
                    GeoSite site = __instance.Site;

                    if (!(__instance is GeoScavengingMission) || site == null)
                    {
                        return;
                    }

                    string variable = VO25SiteVariable + site.SiteId;

                    if (site.GeoLevel.EventSystem.GetVariable(variable) != VO25MissionOpen)
                    {
                        return;
                    }

                    site.GeoLevel.EventSystem.SetVariable(variable, 0);

                    GeoFaction formerOwner = site.GetComponent<GeoHaven>()?.OriginalOwner ?? site.Owner;

                    if (GetMajorFactions(site.GeoLevel).Contains(formerOwner))
                    {
                        formerOwner.Diplomacy.ModifyDiplomacy(site.GeoLevel.PhoenixFaction, VO25DiplomacyCost);
                        TFTVLogger.Always($"VO25: {VO25DiplomacyCost} diplomacy with {formerOwner.Name.LocalizeEnglish()} for scavenging {site.LocalizedSiteName}");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // ---- VO26 ----

        private static List<GeoFaction> GetFactionsAtWar(GeoLevelController controller)
        {
            return GetMajorFactions(controller).Where(f => f.Diplomacy.GetDiplomacyState(controller.PhoenixFaction) == PartyDiplomacyState.Conflict).ToList();
        }

        internal static bool AtWarGate(GeoLevelController controller) => GetFactionsAtWar(controller).Count > 0;

        /// <summary>A faction at war, if the roll comes up and it has a mission of this kind; otherwise the game picks as usual.</summary>
        private static IGeoFactionMissionParticipant PickFactionAtWar(GeoLevelController controller, Func<GeoMissionGenerator.ParticipantFilter, TacMissionTypeDef> findMission)
        {
            if (!TFTVVoidOmens.VoidOmensCheck[26] || UnityEngine.Random.value >= VO26ChanceOfFactionAtWar)
            {
                return null;
            }

            return GetFactionsAtWar(controller)
                .OrderBy(_ => UnityEngine.Random.value)
                .FirstOrDefault(f => findMission(new GeoMissionGenerator.ParticipantFilter { Faction = f.PPFactionDef, ParticipantType = TacMissionParticipant.Intruder }) != null);
        }

        [HarmonyPatch(typeof(GeoSite), nameof(GeoSite.CreateScavengingMission))]
        internal static class GeoSite_CreateScavengingMission_VO26_Patch
        {
            private static void Prefix(GeoSite __instance, ref IGeoFactionMissionParticipant __0, ICollection<MissionTagDef> __1)
            {
                try
                {
                    if (__0 != null)
                    {
                        return;
                    }

                    GeoLevelController controller = __instance.GeoLevel;

                    __0 = PickFactionAtWar(controller, filter => __1 != null && __1.Count > 0
                        ? controller.MissionGenerator.GetRandomMission(__1, filter)
                        : controller.MissionGenerator.GetRandomMission(controller.SharedData.SharedGameTags.ScavengingMissionTag, filter));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoSite), nameof(GeoSite.CreateAmbushMission))]
        internal static class GeoSite_CreateAmbushMission_VO26_Patch
        {
            private static void Prefix(GeoSite __instance, ref IGeoFactionMissionParticipant __0)
            {
                try
                {
                    if (__0 != null)
                    {
                        return;
                    }

                    GeoLevelController controller = __instance.GeoLevel;

                    __0 = PickFactionAtWar(controller, filter => controller.MissionGenerator.GetRandomMission(controller.SharedData.SharedGameTags.AmbushMissionTag, filter));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // ---- VO27 ----

        internal static void ApplyVO27(GeoLevelController controller) => TFTVVoidOmens.SetInfiltrationMultipliers(VO27InfiltrationFactionMultiplier, VO27InfiltrationLeaderMultiplier);
        internal static void RevertVO27(GeoLevelController controller) => TFTVVoidOmens.SetInfiltrationMultipliers(1f, 1.5f);

        [HarmonyPatch(typeof(GeoHaven), nameof(GeoHaven.GetResourceTradingData))]
        internal static class GeoHaven_GetResourceTradingData_VO27_Patch
        {
            private static void Postfix(GeoHaven __instance, TradeModuleData __result)
            {
                try
                {
                    if (!TFTVVoidOmens.VoidOmensCheck[27] || __instance.Leader == null
                        || __instance.Leader.Diplomacy.GetDiplomacy(__instance.Site.GeoLevel.PhoenixFaction) < VO27MinLeaderAttitude)
                    {
                        return;
                    }

                    foreach (PurchaseOptions options in __result.ResourceToPurchaseOptionsDict.Values)
                    {
                        foreach (ResourceType resource in options.TradeRatiosByResource.Keys.ToList())
                        {
                            TradeRatio ratio = options.TradeRatiosByResource[resource];
                            ratio.HavenGiveRatio *= VO27HavenGivesMultiplier;
                            ratio.HavenTakeRatio *= VO27HavenTakesMultiplier;
                            options.TradeRatiosByResource[resource] = ratio;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        // ---- VO29 ----

        internal static bool AlignedGate(GeoLevelController controller)
        {
            return GetMajorFactions(controller).Any(f => f.Diplomacy.GetDiplomacyState(controller.PhoenixFaction) >= PartyDiplomacyState.Pact);
        }

        private static void LetAlliesExplore(GeoLevelController controller)
        {
            if (!TFTVVoidOmens.VoidOmensCheck[29])
            {
                return;
            }

            foreach (GeoFaction faction in GetMajorFactions(controller).Where(f => f.Diplomacy.GetDiplomacyState(controller.PhoenixFaction) >= PartyDiplomacyState.Pact))
            {
                if (UnityEngine.Random.value >= VO29DailyChancePerFaction)
                {
                    continue;
                }

                GeoSite site = GetNearestUnexploredSite(controller, faction);

                if (site != null)
                {
                    ExploreForPhoenix(controller, faction, site);
                }
            }
        }

        private static GeoSite GetNearestUnexploredSite(GeoLevelController controller, GeoFaction faction)
        {
            List<Vector3> havens = faction.Havens.Where(h => h?.Site != null).Select(h => h.Site.WorldPosition).ToList();

            if (havens.Count == 0)
            {
                return null;
            }

            GeoPhoenixFaction phoenix = controller.PhoenixFaction;

            return controller.Map.AllSites
                .Where(s => s.Type == GeoSiteType.Exploration && s.State == GeoSiteState.Functioning && s.GetVisible(phoenix) && !s.GetInspected(phoenix)
                    && !s.HasActiveMission && !s.IsStoryProtected && !s.IsObjectiveProtected && !s.IsArcheologySite
                    && !s.Vehicles.Any(v => v.Owner == phoenix))
                .OrderBy(s => havens.Min(h => Vector3.Distance(h, s.WorldPosition)))
                .FirstOrDefault();
        }

        private static void ExploreForPhoenix(GeoLevelController controller, GeoFaction faction, GeoSite site)
        {
            ResourceType[] types = { ResourceType.Supplies, ResourceType.Materials, ResourceType.Tech };
            ResourceType type = types[UnityEngine.Random.Range(0, types.Length)];
            int amount = UnityEngine.Random.Range(VO29MinResources, VO29MaxResources + 1);

            site.EncounterID = null;
            site.SetInspected(controller.PhoenixFaction, true);
            site.SetVisited(controller.PhoenixFaction, true);
            site.RefreshVisuals();

            controller.PhoenixFaction.Wallet.Give(new ResourceUnit(type, amount), OperationReason.Gift);

            AddLogEntry(controller, TFTVCommonMethods.FormatKey("KEY_VOID_OMEN_29_EXPLORED", faction.Name.Localize(), site.LocalizedSiteName, amount.ToString(), type.ToString()));
            TFTVLogger.Always($"VO29: {faction.Name.LocalizeEnglish()} explored {site.LocalizedSiteName}, Phoenix gets {amount} {type}");
        }

        // ---- VO31 ----

        [HarmonyPatch(typeof(GeoSite), "get_ExplorationTime")]
        internal static class GeoSite_get_ExplorationTime_VO31_Patch
        {
            private static void Postfix(GeoSite __instance, ref TimeUnit __result)
            {
                try
                {
                    if (TFTVVoidOmens.VoidOmensCheck[31] && __instance.Type == GeoSiteType.Exploration && __instance.IsInMist)
                    {
                        __result = TimeUnit.FromHours((float)__result.TimeSpan.TotalHours * VO31ExplorationTimeMultiplier);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal static void RaiseMistExplorationRewards(GeoscapeEventContext context, GeoFactionReward reward)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[31] || reward == null || context?.Site == null
                    || context.Site.Type != GeoSiteType.Exploration || !context.Site.IsInMist)
                {
                    return;
                }

                ResourcePack raised = new ResourcePack();

                foreach (ResourceUnit resource in reward.Resources)
                {
                    raised.Add(new ResourceUnit(resource.Type, resource.Value > 0f ? resource.Value * VO31RewardMultiplier : resource.Value));
                }

                reward.Resources = raised;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // ---- VO35 ----

        /// <summary>From what the mission ended with: civilians in any faction's results and structures destroyed.</summary>
        internal static void ReduceHavenDefenseReward(GeoMission mission, ResourcePack resources)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[35] || mission.Result == null)
                {
                    return;
                }

                GameTagDef civilianTag = DefCache.GetDef<GameTagDef>("Civilian_ClassTagDef");
                List<IUnitResultData> results = mission.Result.FactionResults.SelectMany(f => f.UnitResults).Select(u => u.Data).ToList();

                int civiliansLost = results.OfType<TacActorUnitResult>().Count(u => !u.IsAlive && u.HasTag(civilianTag));
                int structuresLost = results.OfType<StructuralTargetResult>().Count(s => !s.IsAlive);

                float multiplier = Mathf.Max(VO35MinimumReward, 1f - civiliansLost * VO35RewardLostPerCivilian - structuresLost * VO35RewardLostPerStructure);

                for (int i = 0; i < resources.Count; i++)
                {
                    ResourceUnit resource = resources[i];
                    resources[i] = new ResourceUnit(resource.Type, resource.Value * multiplier);
                }

                TFTVLogger.Always($"VO35: {civiliansLost} civilians and {structuresLost} structures lost, haven defence reward x{multiplier}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
