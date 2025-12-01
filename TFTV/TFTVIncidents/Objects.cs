using Base.Core;
using Base.Serialization.General;
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
                return true;
            }

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

            public GeoEventVariationConditionDef.ComparisonOperator PopulationComparison;

            public int PopulationThreshold;

            public GeoEventVariationConditionDef.ComparisonOperator LeaderRelationComparison;

            public int LeaderRelationThreshold;

            public string RequiredZoneKeyword;
         
            public string ForbiddenZoneKeyword;

            public GeoFactionDef RequiredFaction;
        }

    }
}
