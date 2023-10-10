using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Abilities; 
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTVVehicleRework.Abilities
{
    [CreateAssetMenu(fileName = "AdjustAccessCostStatusDef", menuName = "Defs/Abilities/Tactical/AdjustAccessCostStatus")]
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    public class AdjustAccessCostStatusDef : TacStatusDef
    {
        public TacticalAbilityCostModification AccessCostModification = new TacticalAbilityCostModification();

        public Direction AccessDirection;

        public enum Direction
        {
            Entry,
            Exit
        }
    }
}