using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>Def of <see cref="VoidOmenStatus"/>, the hidden status behind the tactical Void Omens.</summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "VoidOmenStatusDef", menuName = "Defs/Statuses/VoidOmenStatusDef")]
    public class VoidOmenStatusDef : TacStatusDef
    {
    }
}
