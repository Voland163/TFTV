using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>Def of <see cref="PandoranVarianceStatus"/>, Harsh Attrition's hidden Pandoran stat rolls.</summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "PandoranVarianceStatusDef", menuName = "Defs/Statuses/PandoranVarianceStatusDef")]
    public class PandoranVarianceStatusDef : TacStatusDef
    {
    }
}
