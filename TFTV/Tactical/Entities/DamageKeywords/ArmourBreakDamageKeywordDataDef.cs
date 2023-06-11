using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using UnityEngine;

namespace TFTV.Tactical.Entities.DamageKeywords
{
    [SerializeType(InheritCustomCreateFrom = typeof(BaseDef))]
    [CreateAssetMenu(fileName = "ArmourBreakDamageKeywordDataDef", menuName = "Defs/DamageKeywords/ArmourBreakDamageKeyword")]
    public class ArmourBreakDamageKeywordDataDef : DamageKeywordDef, IContextualKeywordDamageValue
    {
        public override void ApplyAiEvaluationEffect(TacticalActorBase actorBase, float keywordValue, float targetEffectiveArmor, ref DamageResult res)
        {
            res.ArmorDamage = Mathf.Min(targetEffectiveArmor, keywordValue);
        }

        public float CalculateDamageValue(DamagePayload payload, float shredValue)
        {
            //  PRMLogger.Always($"ArmourBreakDamageKeywordDataDef.CalculateDamageValue called ...");
            if (!DistributeShredAcrossBurst)
            {
                return shredValue;
            }
            int num = payload.AutoFireShotCount * payload.ProjectilesPerShot;
            return Mathf.Max(1.0f, Mathf.Floor(shredValue / num));
        }

        public bool DistributeShredAcrossBurst = false;
        public bool ShredIsAdditive = false;
    }
}
