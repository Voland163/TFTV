using Base.Defs;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PRMBetterClasses;
using System.Linq;
using UnityEngine;

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
            
            // Check for shredding resistances and apply if present
            if (data.Target.GetActor() != null)
            {
                TacticalActorBase actor = data.Target.GetActor();
                
                // Revenant resistance
                DamageMultiplierStatusDef revenantResistanceStatus = TFTVMain.Main.DefCache.GetDef<DamageMultiplierStatusDef>("RevenantResistance_StatusDef");
                StandardDamageTypeEffectDef shredDamage = TFTVMain.Main.DefCache.GetDef<StandardDamageTypeEffectDef>("Shred_StandardDamageTypeEffectDef");
                if (actor.Status != null
                    && actor.Status.HasStatus(revenantResistanceStatus)
                    && revenantResistanceStatus.DamageTypeDefs[0] == shredDamage)
                {
                    data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * revenantResistanceStatus.Multiplier);
                }

                // ShredResistant_DamageMultiplierAbilityDef
                DamageMultiplierAbilityDef shredResistanceAbilityDef = TFTVMain.Main.DefCache.GetDef<DamageMultiplierAbilityDef>("ShredResistant_DamageMultiplierAbilityDef");
                if (actor.GetAbilityWithDef<DamageMultiplierAbility>(shredResistanceAbilityDef) != null)
                {
                    data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * shredResistanceAbilityDef.Multiplier);
                }
            }

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
