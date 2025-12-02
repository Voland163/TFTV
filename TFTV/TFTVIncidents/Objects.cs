using Base.Core;
using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events.Conditions;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVIncidents
{
    internal class Objects
    {
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
        }

        [SerializeType(SerializeMembersByDefault = SerializeMembersType.SerializeAll, Embedded = true)]
        public class GeoIncidentEligibilityCondition
        {
            // Token: 0x06005F59 RID: 24409 RVA: 0x00166D00 File Offset: 0x00164F00
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
                if (!string.IsNullOrEmpty(this.RequiredZoneKeyword) && !this.HasZoneWithKeyword(haven, this.RequiredZoneKeyword))
                {
                    return false;
                }
                if (!string.IsNullOrEmpty(this.ForbiddenZoneKeyword) && this.HasZoneWithKeyword(haven, this.ForbiddenZoneKeyword))
                {
                    return false;
                }
                if (this.NearbyHavenRange > EarthUnits.Zero && !this.HasNearbyEligibleHaven(haven, visitingFaction))
                {
                    return false;
                }
                return true;
            }

            // Token: 0x06005F5A RID: 24410 RVA: 0x00166E00 File Offset: 0x00165000
            private bool HasZoneWithKeyword(GeoHaven haven, string keyword)
            {
                foreach (GeoHavenZone geoHavenZone in haven.Zones)
                {
                    if (geoHavenZone.Def != null && geoHavenZone.Def.Keywords.Contains(keyword))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Token: 0x06005F5B RID: 24411 RVA: 0x00166E70 File Offset: 0x00165070
            private bool HasNearbyEligibleHaven(GeoHaven haven, GeoFaction visitingFaction)
            {
                GeoSite site = haven.Site;
                GeoLevelController geoLevelController = (site != null) ? site.GeoLevel : null;
                if (geoLevelController == null)
                {
                    return false;
                }
                IEnumerable<GeoIncidentEligibilityCondition> nearbyHavenConditions = this.NearbyHavenConditions;
                foreach (GeoSite geoSite in geoLevelController.Map.SitesByType[GeoSiteType.Haven])
                {
                    if (!(geoSite == site))
                    {
                        GeoHaven component = geoSite.GetComponent<GeoHaven>();
                        if (component != null)
                        {
                            bool flag = nearbyHavenConditions == null || nearbyHavenConditions.All((GeoIncidentEligibilityCondition c) => c.IsEligible(component, visitingFaction));
                            if (flag && GeoMap.Distance(site, geoSite) <= this.NearbyHavenRange)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            // Token: 0x04003DB2 RID: 15810
            public GeoEventVariationConditionDef.ComparisonOperator PopulationComparison;

            // Token: 0x04003DB3 RID: 15811
            public int PopulationThreshold;

            // Token: 0x04003DB4 RID: 15812
            public GeoEventVariationConditionDef.ComparisonOperator LeaderRelationComparison;

            // Token: 0x04003DB5 RID: 15813
            public int LeaderRelationThreshold;

            // Token: 0x04003DB6 RID: 15814
            public string RequiredZoneKeyword;

            // Token: 0x04003DB7 RID: 15815
            public string ForbiddenZoneKeyword;

            // Token: 0x04003DB8 RID: 15816
            public GeoFactionDef RequiredFaction;

            // Token: 0x04003DB9 RID: 15817
            public EarthUnits NearbyHavenRange;

            // Token: 0x04003DBA RID: 15818
            public List<GeoIncidentEligibilityCondition> NearbyHavenConditions = new List<GeoIncidentEligibilityCondition>();
        }

    }
}
