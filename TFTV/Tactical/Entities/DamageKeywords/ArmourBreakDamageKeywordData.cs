using Base.Defs;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using PRMBetterClasses;
using System.Linq;

namespace TFTV.Tactical.Entities.DamageKeywords
{
    public class ArmourBreakDamageKeywordData : DamageKeyword
    {
		public ArmourBreakDamageKeywordDataDef ArmourBreakDamageKeywordDataDef
		{
			get
			{
                return this.Def<ArmourBreakDamageKeywordDataDef>();
			}
		}

        protected override bool ProcessKeywordDataInternal(ref DamageAccumulation.TargetData data)
        {
           // PRMLogger.Always($"ArmourBreakDamageKeywordData.ProcessKeywordDataInternal called ...");
            if (data == null)
			{
                data = GenerateTargetData();
            }
            float shredValue = CalculateDamageValue();
            data.DamageResult.ArmorDamage = ArmourBreakDamageKeywordDataDef.ShredIsAdditive ? data.DamageResult.ArmorDamage + shredValue : shredValue;
            return true;
        }

        protected override DamageAccumulation.TargetData GenerateTargetData()
        {
            PRMLogger.Always($"ArmourBreakDamageKeywordData.GenerateTargetData called ...");
            return new DamageAccumulation.TargetData(_recv, _accum.Source, null, null);
        }

        private float CalculateDamageValue()
        {
           // PRMLogger.Always($"ArmourBreakDamageKeywordData.CalculateDamageValue called ...");
            if (!(_accum.Source is IDamageDealer damageDealer))
            {
                return _value;
            }
            DamagePayload damagePayload = damageDealer.GetDamagePayload();
            return ArmourBreakDamageKeywordDataDef.CalculateDamageValue(damagePayload, _value);
        }
    }
}
