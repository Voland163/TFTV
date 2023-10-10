using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Abilities;
using UnityEngine;

namespace TFTVVehicleRework.Abilities
{   
    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
    public class ApplyStatusAfterAbilityExecutedAbilityDef : TacticalAbilityDef
    {
        [Header("Apply Status")]
        public StatusDef StatusToApply;
    }
}