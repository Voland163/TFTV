using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace PRMBetterClasses.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "ModifyDamageKeywordStatusDef", menuName = "Defs/Statuses/ModifyDamageKeywordStatus")]
    public class AddDependentDamageKeywordsStatusDef : TacStatusDef
    {
        public DamageKeywordDef[] DamageKeywordDefs;
        public float BonusDamagePerc = 0f;
    }
}
