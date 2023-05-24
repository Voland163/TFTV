using Base.Serialization.General;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
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
        public GameTagDef[] WeaponTagCullFilter;
    }
}
