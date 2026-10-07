using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Linq;
using TFTV;

namespace TFTVVehicleRework.HarmonyPatches
{
    /// <summary>
    /// Makes the fire tiles of an attack burn for the Burning value the attack actually carried.
    ///
    /// FireExplosionEffect.GetFireDamageValue only looks for the fire keyword in the weapon's own
    /// payload. When Burning comes from a bonus keyword on the shooter instead (the Purgatory's
    /// incendiary ammo is an AddAttackBoostStatus), the payload has none, so vanilla falls back to
    /// payload.DamageValue. The Purgatory's is 0, and a fire tile created with 0 damage takes
    /// Fire_DamageEffectDef.MaximumDamage instead of the ammo's value.
    ///
    /// Here the shooter's bonus Burning keyword stands in for the missing payload keyword, exactly
    /// as if the weapon had been given it while the ammo is loaded.
    /// </summary>
    [HarmonyPatch(typeof(FireExplosionEffect), "GetFireDamageValue")]
    internal static class FireExplosionEffect_GetFireDamageValue_Patch
    {
        private static void Postfix(FireExplosionEffect __instance, TacticalVoxelMatrix voxelMatrix, ref float __result)
        {
            try
            {
                if (!(__instance.Source is IDamageDealer dealer))
                {
                    return;
                }

                DamagePayload payload = dealer.GetDamagePayload();
                DamageTypeBaseEffectDef fireType = __instance.FireExplosionEffectDef?.DamageEffectDef?.DamageTypeDef;

                if (payload == null || fireType == null || payload.DamageKeywords.Any(k => k.DamageKeywordDef.DamageTypeDef == fireType))
                {
                    return;
                }

                if (!(dealer.GetTacticalActorBase() is TacticalActor actor) || !actor.HasBonusDamageKeywords)
                {
                    return;
                }

                DamageKeywordPair bonusFire = actor.GetBonusKeywords(null).FirstOrDefault(k => k.DamageKeywordDef.DamageTypeDef == fireType);

                if (bonusFire == null)
                {
                    return;
                }

                __result = bonusFire.GenerateDamageValue(null) * voxelMatrix.VoxelMatrixData.FireDamageMultiplier;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
