using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>Def of <see cref="LimbMalusStatus"/>, the limb hit malus (Config.LimbHitMalus).</summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "LimbMalusStatusDef", menuName = "Defs/Statuses/LimbMalusStatusDef")]
    public class LimbMalusStatusDef : TacStatusDef
    {
    }
}
