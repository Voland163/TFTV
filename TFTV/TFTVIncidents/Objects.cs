using Base.Core;
using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVIncidents
{
    internal class Objects
    {

        //Key structure: TFTV_INCIDENT_[ID]_[FACTION]_

        //Approaches: 
        /*
        Psycho-Sociology: P
        Exploration: E
        Occult: O
        Biotech: B
        Machinery: M
        Compute: C
        */

        //Title structure: TFTV_INCIDENT_[ID]_[FACTION]_TITLE
        //Description structure: TFTV_INCIDENT_[ID]_[FACTION]_DESC
        //Choice structure TFTV_INCIDENT_[ID]_[FACTION]_CHOICE_[0/1]_[APPROACH1_APPROACH2]/TFTV_INCIDENT_[ID]_[FACTION]_CHOICE_2 (Cancel)
        //Outcome structure: TFTV_INCIDENT_[ID]_[FACTION]_OUTCOME_[0/1]_[S/F]_[APPROACHA_APPROACHB] /TFTV_INCIDENT_[ID]_[FACTION]_OUTCOME_C (Cancel)

        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll, Embedded = true)]
        public class GeoIncidentDefinition
        {
            public bool IsEligibleFor(GeoHaven haven, GeoFaction visitingFaction)
            {
                return this.EligibilityConditions == null || this.EligibilityConditions.All((GeoIncidentEligibilityCondition c) => c.IsEligible(haven, visitingFaction));
            }

            public IEnumerable<GeoscapeEventDef> GetAllResolutionEvents()
            {
                yield return this.IntroEvent;
                yield return this.ChoiceAResolutionSuccess;
                yield return this.ChoiceAResolutionFailure;
                yield return this.ChoiceBResolutionSuccess;
                yield return this.ChoiceBResolutionFailure;
                yield break;
            }

            public int Id;

            public GeoscapeEventDef IntroEvent;

            public GeoscapeEventDef ChoiceAResolutionSuccess;

            public GeoscapeEventDef ChoiceAResolutionFailure;

            public GeoscapeEventDef ChoiceBResolutionSuccess;

            public GeoscapeEventDef ChoiceBResolutionFailure;

            public TimeUnit ResolutionTime;

            public List<GeoIncidentEligibilityCondition> EligibilityConditions = new List<GeoIncidentEligibilityCondition>();

            public int Priority;

            public GeoFactionDef FactionDef;
        }

        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll, Embedded = true)]
        public class GeoIncidentEligibilityCondition
        {
            public bool IsEligible(GeoHaven haven, GeoFaction visitingFaction)
            {
                if (haven == null)
                {
                    throw new ArgumentNullException("haven");
                }
                if (this.RequiredFaction != null)
                {
                    GeoFaction owner = (haven.Site != null) ? haven.Site.Owner : null;
                    if (owner == null || owner.Def != this.RequiredFaction)
                    {
                        return false;
                    }
                }
                if (this.PopulationComparison != GeoEventVariationConditionDef.ComparisonOperator.None && !GeoEventVariationConditionDef.Compare(haven.Population, this.PopulationThreshold, this.PopulationComparison))
                {
                    return false;
                }
                if (this.LeaderRelationComparison != GeoEventVariationConditionDef.ComparisonOperator.None)
                {
                    GeoFaction geoFaction = visitingFaction ?? ((haven.Site != null) ? haven.Site.GeoLevel.PhoenixFaction : null);
                    if (geoFaction == null)
                    {
                        return false;
                    }
                    GeoHavenLeader leader = haven.Leader;
                    if (leader == null || !GeoEventVariationConditionDef.Compare(leader.Diplomacy.GetDiplomacy(geoFaction), this.LeaderRelationThreshold, this.LeaderRelationComparison))
                    {
                        return false;
                    }
                }
                if (this.LeaderRelationToPhoenixComparison != GeoEventVariationConditionDef.ComparisonOperator.None)
                {
                    GeoFaction phoenixFaction = (haven.Site != null) ? haven.Site.GeoLevel.PhoenixFaction : null;
                    if (phoenixFaction == null)
                    {
                        return false;
                    }
                    GeoHavenLeader leader = haven.Leader;
                    if (leader == null || !GeoEventVariationConditionDef.Compare(leader.Diplomacy.GetDiplomacy(phoenixFaction), this.LeaderRelationToPhoenixThreshold, this.LeaderRelationToPhoenixComparison))
                    {
                        return false;
                    }
                }
                if (this.FactionRelationToPhoenixComparison != GeoEventVariationConditionDef.ComparisonOperator.None)
                {
                    GeoFaction phoenixFaction = (haven.Site != null) ? haven.Site.GeoLevel.PhoenixFaction : null;
                    GeoFaction owner = (haven.Site != null) ? haven.Site.Owner : null;
                    if (phoenixFaction == null || owner == null)
                    {
                        return false;
                    }
                    if (!GeoEventVariationConditionDef.Compare(owner.Diplomacy.GetDiplomacy(phoenixFaction), this.FactionRelationToPhoenixThreshold, this.FactionRelationToPhoenixComparison))
                    {
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(this.RequiredZoneDefName) && !this.HasZoneWithDefName(haven, this.RequiredZoneDefName))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(this.ForbiddenZoneDefName) && this.HasZoneWithDefName(haven, this.ForbiddenZoneDefName))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(this.RequiredSiteTag))
                {
                    GeoSite site = haven.Site;
                    if (site?.SiteTags == null || !site.SiteTags.Contains(this.RequiredSiteTag))
                    {
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(this.RequiredSiteTagPrefix))
                {
                    GeoSite site = haven.Site;
                    if (site?.SiteTags == null || !site.SiteTags.Any(t => t.StartsWith(this.RequiredSiteTagPrefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }
                if (this.RequireHavenInMist && !(haven?.Site?.IsInMist ?? false))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(this.RequiredResearchID))
                {
                    GeoLevelController geoLevel = (haven.Site != null) ? haven.Site.GeoLevel : null;
                    if (geoLevel == null || !this.HasCompletedResearch(geoLevel, this.RequiredResearchID))
                    {
                        return false;
                    }
                }
                if (this.VariableComparisonToVariable != GeoEventVariationConditionDef.ComparisonOperator.None)
                {
                    if (string.IsNullOrEmpty(this.RequiredVariableName) || string.IsNullOrEmpty(this.RequiredVariableNameB))
                    {
                        return false;
                    }
                    GeoLevelController geoLevel = (haven.Site != null) ? haven.Site.GeoLevel : null;
                    if (geoLevel == null)
                    {
                        return false;
                    }
                    int valueA = geoLevel.EventSystem.GetVariable(this.RequiredVariableName);
                    int valueB = geoLevel.EventSystem.GetVariable(this.RequiredVariableNameB);
                    if (!GeoEventVariationConditionDef.Compare(valueA, valueB, this.VariableComparisonToVariable))
                    {
                        return false;
                    }
                }
                if (this.VariableComparison != GeoEventVariationConditionDef.ComparisonOperator.None)
                {
                    if (string.IsNullOrEmpty(this.RequiredVariableName))
                    {
                        return false;
                    }
                    GeoLevelController geoLevel = (haven.Site != null) ? haven.Site.GeoLevel : null;
                    if (geoLevel == null || !GeoEventVariationConditionDef.Compare(geoLevel.EventSystem.GetVariable(this.RequiredVariableName), this.VariableThreshold, this.VariableComparison))
                    {
                        return false;
                    }
                }
                if (this.RequiredCharacterBackgroundFaction != null)
                {
                    GeoLevelController geoLevel = (haven.Site != null) ? haven.Site.GeoLevel : null;
                    if (geoLevel == null || !this.HasCharacterWithBackgroundFaction(geoLevel, this.RequiredCharacterBackgroundFaction))
                    {
                        return false;
                    }
                }
                if (this.RequireStarvingHaven && !this.IsStarvingHaven(haven))
                {
                    return false;
                }
                if (this.NearbyHavenRange > EarthUnits.Zero && !this.HasNearbyEligibleHaven(haven, visitingFaction))
                {
                    return false;
                }
                if (this.RequireDestroyedSite)
                {
                    GeoSite site = haven.Site;
                    if (site == null || site.State != GeoSiteState.Destroyed)
                    {
                        return false;
                    }
                }
                if (this.RequireNotDestroyed)
                {
                    GeoSite site = haven.Site;
                    if (site != null && site.State == GeoSiteState.Destroyed)
                    {
                        return false;
                    }
                }
                if (this.RequireNotInfested && haven.IsInfested)
                {
                    return false;
                }
                return true;
            }

            private bool IsStarvingHaven(GeoHaven haven)
            {
                if (haven?.ZonesStats == null)
                {
                    return false;
                }

                HavenZonesStats.HavenOnlyOutput output = haven.ZonesStats.GetTotalHavenOutput();
                return haven.GetPopulationChange(output) > 0;
            }

            private bool HasZoneWithDefName(GeoHaven haven, string zoneDefName)
            {
                // Possible GeoHavenZoneDef names:
                // Energy_GeoHavenZoneDef
                // Factory_GeoHavenZoneDef
                // FoodProduction_GeoHavenZoneDef
                // MissionaryCentre_GeoHavenZoneDef
                // Research_GeoHavenZoneDef
                // ResidentialElite_GeoHavenZoneDef
                // Residential_GeoHavenZoneDef
                // SatelliteUplink_GeoHavenZoneDef
                // TrainingElite_GeoHavenZoneDef
                // Training_GeoHavenZoneDef
                foreach (GeoHavenZone geoHavenZone in haven.Zones)
                {
                    if (geoHavenZone.Def != null && geoHavenZone.Def.name.Equals(zoneDefName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool HasCompletedResearch(GeoLevelController geoLevel, string researchId)
            {
                if (geoLevel == null || string.IsNullOrEmpty(researchId))
                {
                    return false;
                }

                foreach (GeoFaction faction in geoLevel.FactionsWithDiplomacy)
                {
                    if (faction != null && faction.Research != null && faction.Research.HasCompleted(researchId))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool HasCharacterWithBackgroundFaction(GeoLevelController geoLevel, GeoFactionDef backgroundFaction)
            {
                GeoPhoenixFaction phoenixFaction = geoLevel?.PhoenixFaction;
                if (phoenixFaction == null || backgroundFaction == null)
                {
                    return false;
                }

                return phoenixFaction.Soldiers.Any(c =>
                    c != null &&
                    c.OriginalFactionDef == backgroundFaction);
            }

            private bool HasNearbyEligibleHaven(GeoHaven haven, GeoFaction visitingFaction)
            {
                GeoSite site = haven.Site;
                GeoLevelController geoLevelController = (site != null) ? site.GeoLevel : null;
                if (geoLevelController == null || haven.Range == null)
                {
                    return false;
                }

                IEnumerable<GeoIncidentEligibilityCondition> nearbyHavenConditions = this.NearbyHavenConditions;
                EarthUnits range = haven.Range.Range;

                foreach (GeoSite geoSite in haven.Range.SitesInRange)
                {
                    if (!(geoSite == site))
                    {
                        GeoHaven component = geoSite.GetComponent<GeoHaven>();
                        if (component != null)
                        {
                            bool flag = nearbyHavenConditions == null || nearbyHavenConditions.All((GeoIncidentEligibilityCondition c) => c.IsEligible(component, visitingFaction));
                            if (flag && GeoMap.Distance(site, geoSite) <= range)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            public string RequiredSiteTag;

            public string RequiredSiteTagPrefix;

            public bool RequireHavenInMist;

            public GeoEventVariationConditionDef.ComparisonOperator PopulationComparison;

            public int PopulationThreshold;

            public GeoEventVariationConditionDef.ComparisonOperator LeaderRelationComparison;

            public int LeaderRelationThreshold;

            public GeoEventVariationConditionDef.ComparisonOperator LeaderRelationToPhoenixComparison;

            public int LeaderRelationToPhoenixThreshold;

            public GeoEventVariationConditionDef.ComparisonOperator FactionRelationToPhoenixComparison;

            public int FactionRelationToPhoenixThreshold;

            public string RequiredZoneDefName;

            public string ForbiddenZoneDefName;

            public GeoFactionDef RequiredFaction;

            public EarthUnits NearbyHavenRange;

            public List<GeoIncidentEligibilityCondition> NearbyHavenConditions = new List<GeoIncidentEligibilityCondition>();

            public string RequiredResearchID;

            public string RequiredVariableName;

            public string RequiredVariableNameB;

            public GeoEventVariationConditionDef.ComparisonOperator VariableComparison;

            public GeoEventVariationConditionDef.ComparisonOperator VariableComparisonToVariable;

            public int VariableThreshold;

            public GeoFactionDef RequiredCharacterBackgroundFaction;

            public bool RequireStarvingHaven;

            public bool RequireDestroyedSite;

            public bool RequireNotDestroyed;

            public bool RequireNotInfested;
        }

    }
}
