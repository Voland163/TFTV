using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TFTV.TFTVBaseRework.BaseActivation;

namespace TFTV
{
    /// <summary>
    /// Keeps Pandoran colonies where they have something to attack.
    ///
    /// A colony can only reach havens and Phoenix bases within its type's MaximumRange (the size its range
    /// grows to). Vanilla places colonies on any free mist site, and a colony with nothing in reach just
    /// sits there and holds one of the concurrent-colony slots, so no useful colony can replace it.
    ///
    /// - A new colony is only placed on a site with at least one haven or active Phoenix base within the
    ///   maximum range of its type.
    /// - A colony that has had nothing in reach for 4 days is abandoned. Abandoned colonies stop counting
    ///   against the limits (GetBasesOfType counts Functioning sites only), so new ones can spawn.
    ///
    /// The Palace is left alone by both rules.
    /// </summary>
    internal static class TFTVPandoranColonyRange
    {
        private const int HoursWithoutTargetsBeforeAbandoning = 4 * 24;
        private const string NoTargetsSinceVariablePrefix = "TFTV_COLONY_NO_TARGETS_SINCE_";

        private static bool IsExempt(GeoLevelController level, GeoAlienBaseTypeDef baseType)
        {
            return baseType == null || baseType == level.SharedData.PalaceBaseTypeDef;
        }

        /// <summary>
        /// True if a haven or an active Phoenix base is within the maximum range of a colony of this type
        /// placed at this position.
        /// </summary>
        internal static bool HasTargetInRange(GeoLevelController level, Vector3 position, GeoAlienBaseTypeDef baseType)
        {
            float maxRangeMeters = baseType.MaximumRange.InMeters;

            bool InRange(GeoSite site) => GeoMap.Distance(position, site.WorldPosition).InMeters <= maxRangeMeters;

            foreach (GeoSite haven in level.Map.SitesByType[GeoSiteType.Haven])
            {
                if (haven.State == GeoSiteState.Functioning
                    && haven.Owner != null && !haven.Owner.IsAlienFaction && !haven.Owner.IsNeutralFaction
                    && InRange(haven))
                {
                    return true;
                }
            }

            foreach (GeoPhoenixBase phoenixBase in level.PhoenixFaction.Bases)
            {
                GeoSite site = phoenixBase.Site;

                // Outposts are not attacked by colonies (see TFTVBaseDefenseGeoscape), so they don't count.
                if (site != null && site.State == GeoSiteState.Functioning
                    && !site.SiteTags.Contains(PhoenixBaseReworkState.OutpostTag)
                    && InRange(site))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called once a day: starts or clears each colony's no-target clock and abandons the colonies whose
        /// clock has run for 4 days.
        /// </summary>
        internal static void AbandonColoniesWithoutTargets(GeoAlienFaction alienFaction)
        {
            try
            {
                GeoLevelController level = alienFaction.GeoLevel;
                int hoursNow = (int)(level.Timing.Now - level.Timing.StartTime).TimeSpan.TotalHours + 1;

                foreach (GeoAlienBase colony in alienFaction.Bases.ToList())
                {
                    GeoSite site = colony.Site;

                    if (site == null || site.State != GeoSiteState.Functioning || IsExempt(level, colony.AlienBaseTypeDef))
                    {
                        continue;
                    }

                    string variable = NoTargetsSinceVariablePrefix + site.SiteId;
                    int noTargetsSince = level.EventSystem.GetVariable(variable);

                    if (HasTargetInRange(level, site.WorldPosition, colony.AlienBaseTypeDef))
                    {
                        if (noTargetsSince != 0)
                        {
                            level.EventSystem.SetVariable(variable, 0);
                        }
                        continue;
                    }

                    if (noTargetsSince == 0)
                    {
                        level.EventSystem.SetVariable(variable, hoursNow);
                        TFTVLogger.Always($"[ColonyRange] {site.LocalizedSiteName} (site {site.SiteId}) has no haven or Phoenix base in reach; it will be abandoned in {HoursWithoutTargetsBeforeAbandoning / 24} days unless one appears.");
                        continue;
                    }

                    if (hoursNow - noTargetsSince < HoursWithoutTargetsBeforeAbandoning)
                    {
                        continue;
                    }

                    // Don't pull the colony away from under an assault the player is setting up.
                    if (site.GetPlayerVehiclesOnSite().Any())
                    {
                        continue;
                    }

                    level.EventSystem.SetVariable(variable, 0);
                    site.AbandonSite();
                    site.RefreshVisuals();
                    TFTVLogger.Always($"[ColonyRange] Abandoned {site.LocalizedSiteName} (site {site.SiteId}): nothing in reach for {HoursWithoutTargetsBeforeAbandoning / 24} days.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Vanilla's SpawnNewAlienBase(limits), with one more condition on the candidate sites: a haven or
        /// active Phoenix base must be within the maximum range of the colony type.
        /// </summary>
        [HarmonyPatch(typeof(GeoAlienFaction), nameof(GeoAlienFaction.SpawnNewAlienBase), new Type[] { typeof(GeoAlienBaseLimits) })]
        internal static class GeoAlienFaction_SpawnNewAlienBase_Patch
        {
            public static bool Prefix(GeoAlienFaction __instance, GeoAlienBaseLimits updatable, ref GeoSite __result)
            {
                try
                {
                    GeoLevelController level = __instance.GeoLevel;
                    GeoAlienBaseTypeDef baseType = updatable.BaseLimitation.BaseType;

                    if (IsExempt(level, baseType))
                    {
                        return true;
                    }

                    GeoMap map = level.Map;
                    List<GeoSite> existingBases = __instance.GetBasesOfType(baseType);
                    float minDistanceKm = updatable.BaseLimitation.MinDistanceBetweenKm;

                    List<GeoSite> candidates = map.ActiveSites
                        .Where(s => s.State == GeoSiteState.None && s.Type == GeoSiteType.AlienBase && map.IsInMist(s))
                        .Where(s => existingBases.All(b => GeoMap.Distance(s.WorldPosition, b.WorldPosition).InMeters / 1000f >= minDistanceKm))
                        .ToList();

                    List<GeoSite> withTargets = candidates.Where(s => HasTargetInRange(level, s.WorldPosition, baseType)).ToList();

                    if (withTargets.Count < candidates.Count)
                    {
                        TFTVLogger.Always($"[ColonyRange] {baseType.name}: {candidates.Count - withTargets.Count} of {candidates.Count} candidate sites have no haven or Phoenix base in reach and were skipped.");
                    }

                    if (withTargets.Count == 0)
                    {
                        __result = null;
                        return false;
                    }

                    GeoSite site = withTargets[UnityEngine.Random.Range(0, withTargets.Count)];
                    __instance.SpawnNewAlienBase(site, baseType);
                    updatable.TotalBases++;
                    __result = site;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }

        /// <summary>
        /// Vanilla treats an abandoned colony like a destroyed one, including raising the Behemoth's
        /// disruption. Nobody destroyed an abandoned colony, so it gives no disruption.
        /// </summary>
        [HarmonyPatch(typeof(GeoAlienBase), "OnBaseDestroyed")]
        internal static class GeoAlienBase_OnBaseDestroyed_Patch
        {
            public static bool Prefix(GeoAlienBase __instance)
            {
                return __instance.Site == null || __instance.Site.State != GeoSiteState.Abandoned;
            }
        }
    }
}
