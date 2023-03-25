using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "FumbleChanceStatusDef", menuName = "Defs/Statuses/FumbleChanceStatusDef")]
    public class FumbleChanceStatusDef : TacStatusDef
    {
        public int FumbleChancePerc = 50;
        public TacticalAbilityDef[] AbilitiesToFumble;
        public DamageDeliveryType RestrictedDeliveryType = default;
    }
}
