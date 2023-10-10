using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Abilities;
using UnityEngine;

namespace TFTVVehicleRework.Abilities
{
    [CreateAssetMenu(fileName = "FreeReloadAbilityDef", menuName = "Defs/Abilities/Tactical/FreeReload")]
    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
    public class FreeReloadAbilityDef : TacticalAbilityDef
    {
    }
}