using Base.Serialization.General;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using System.Linq;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacActorEffectConditionDef))]
    [CreateAssetMenu(fileName = "ActorHasBodypartTagEffectConditionDef", menuName = "Defs/Effects/Conditions/ActorHasBodypartTag")]
    public class ActorHasBodypartTagEffectConditionDef : TacActorEffectConditionDef
    {
        public GameTagDef VehicleTag;
        public GameTagDef RequiredBodyPartTag;
        public int RequiredCount = 1;

        protected override bool ActorChecks(TacticalActorBase actor)
        {
            TacticalActor tacticalActor = actor as TacticalActor;
            if (tacticalActor == null)
            {
                return false;
            }

            if (VehicleTag != null && tacticalActor.GameTags != null && tacticalActor.GameTags.Contains(VehicleTag))
            {
                return true;
            }

            if (RequiredBodyPartTag == null || tacticalActor.BodyState == null)
            {
                return false;
            }

            int requiredCount = RequiredCount < 1 ? 1 : RequiredCount;

            return tacticalActor.BodyState
                .GetArmourItems()
                .Count(item => item != null && item.GameTags != null && item.GameTags.Contains(RequiredBodyPartTag)) >= requiredCount;
        }
    }
}