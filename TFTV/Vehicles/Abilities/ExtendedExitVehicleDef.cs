using Base.Serialization.General;
using Base.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Abilities; 
using UnityEngine;

namespace TFTVVehicleRework.Abilities
{
    [CreateAssetMenu(fileName = "ExtendedExitVehicleAbilityDef", menuName = "Defs/Abilities/Tactical/ExitVehicleExtended")]
    [SerializeType(InheritCustomCreateFrom = typeof(ExitVehicleAbilityDef))]
    public class ExtendedExitVehicleAbilityDef : ExitVehicleAbilityDef
    {
        [Header("Remove Status that keeps actors hidden")]
        public StatusDef StealthStatus;
    }
}