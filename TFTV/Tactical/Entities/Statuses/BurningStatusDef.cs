using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// Def of <see cref="BurningStatus"/>, the per-body-part fire status of the fire rework.
    /// Built from the vanilla Fire_StatusDef so it looks and reads the same; the vanilla def is kept
    /// for its per-size flame prefabs and its immunity ability.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "BurningStatusDef", menuName = "Defs/Statuses/BurningStatusDef")]
    public class BurningStatusDef : TacStatusDef
    {
        public FireStatusDef VanillaFireStatusDef;
    }
}
