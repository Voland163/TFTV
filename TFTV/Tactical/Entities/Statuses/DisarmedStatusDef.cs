using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>Def of <see cref="DisarmedStatus"/>, Harsh Attrition's record of a disarmed humanoid enemy.</summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "DisarmedStatusDef", menuName = "Defs/Statuses/DisarmedStatusDef")]
    public class DisarmedStatusDef : TacStatusDef
    {
    }
}
