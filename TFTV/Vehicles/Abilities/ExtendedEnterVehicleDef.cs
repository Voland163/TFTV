using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Abilities; 
using UnityEngine;
using Base.Entities.Statuses;

namespace TFTVVehicleRework.Abilities
{
    [CreateAssetMenu(fileName = "ExtendedEnterVehicleAbilityDef", menuName = "Defs/Abilities/Tactical/EnterVehicleExtended")]
    [SerializeType(InheritCustomCreateFrom = typeof(EnterVehicleAbilityDef))]
    public class ExtendedEnterVehicleAbilityDef : EnterVehicleAbilityDef
    {
        [Header("Status that ensures actors are hidden")]
        public StatusDef StealthStatus;
    }
}