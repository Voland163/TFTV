using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class DamageTacticalVanillaFixes
    {
        /// <summary>
        /// This replaces the original Vanilla method, which contained several bugs. 
        /// The bugs resulted from not considering how damage multipliers / armor stack multipliers (in TFTV, the special revenant resistance, in Vanilla, the Orichalcum shielding)
        /// reduced incoming damage to limbs.
        /// </summary>
        [HarmonyPatch(typeof(DamageAccumulation), nameof(DamageAccumulation.GenerateStandardDamageTargetData))]
        public static class TFTV_DamageAccumulation_GenerateStandardDamageTargetData
        {

            public static bool Prefix(DamageAccumulation __instance, ref DamageAccumulation.TargetData __result,
                IDamageReceiver target, Vector3 damageOrigin, Vector3 impactForce, CastHit impactHit, out float damageAmountLeft)
            {
                try
                {

                    MethodInfo GetPureDamageBonusForMethod = typeof(DamageAccumulation).GetMethod("GetPureDamageBonusFor", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetSourceDamageMultiplierMethod = typeof(DamageAccumulation).GetMethod("GetSourceDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetEffectiveArmorMethod = typeof(DamageAccumulation).GetMethod("GetEffectiveArmor", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo GetArmorStackMultiplierMethod = typeof(DamageAccumulation).GetMethod("GetArmorStackMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);

                    float armorStackMultiplier = (float)GetArmorStackMultiplierMethod.Invoke(__instance, new object[] { target as IDamageBlocker, __instance.Source });

                    float amount = __instance.Amount;
                    damageAmountLeft = __instance.Amount;
                    float num = amount + (float)GetPureDamageBonusForMethod.Invoke(__instance, new object[] { target });
                    float num2 = __instance.Amount * (float)GetSourceDamageMultiplierMethod.Invoke(__instance, new object[] { __instance.DamageTypeDef, target }) - __instance.Amount;//12
                    float damageMultiplierFor = target.GetDamageMultiplierFor(__instance.DamageTypeDef, __instance.Source);
                    float damageMultiplier = __instance.GetDamageMultiplier(target.GetApplicationType());
                    float effectiveArmor = (float)GetEffectiveArmorMethod.Invoke(__instance, new object[] { target });

                    float totalDamageMultiplier = damageMultiplier * damageMultiplierFor * armorStackMultiplier;

                    float num3 = effectiveArmor / totalDamageMultiplier;

                    float num4 = Mathf.Max(0f, num - num3) + num2; //max amount of damage that can be dealt by the attack. missing damageMultiplierFor?
                    float num5 = target.GetHealth() / totalDamageMultiplier; //max amount of damage target can take
                    float num6 = Mathf.Min(num4, num5); //choosing lower of the two
                    if (!__instance.IsFireDamageType)
                    {
                        float num7 = Mathf.Min(b: ((float)target.GetHealth().Max + effectiveArmor) / totalDamageMultiplier, a: damageAmountLeft);//50
                        damageAmountLeft -= num7;

                    }

                    if (num5 < 1E-05f && target.IsAccessoryBodyPart())
                    {
                        num4 = 0f;
                    }

                    float num8 = num6 * totalDamageMultiplier;

                    __result = new DamageAccumulation.TargetData
                    {
                        Target = target,
                        AmountApplied = num4,
                        DamageResult = new DamageResult
                        {
                            Source = __instance.Source,
                            ArmorDamage = __instance.ArmorShred,
                            ArmorMitigatedDamage = Mathf.Min(amount, num3),
                            HealthDamage = num8,
                            ImpactForce = impactForce,
                            ImpactHit = impactHit,
                            DamageOrigin = damageOrigin,
                            DamageTypeDef = __instance.DamageTypeDef,
                            RelatedDamageTypeDefs = ((__instance.DamageKeywords != null) ? __instance.DamageKeywords.Select((DamageKeywordPair x) => x.DamageKeywordDef.DamageTypeDef).ToList() : null)
                        }
                    };


                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

    }
}
