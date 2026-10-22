using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVDrills.DrillsPublicClasses;

namespace TFTV.TFTVUI.Tactical
{
    internal class DamagePrediction
    {

        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;


        //Patch to show correct damage prediction with mutations and Delirium 
        [HarmonyPatch(typeof(PhoenixPoint.Tactical.UI.Utils), "GetDamageKeywordValue")] //VERIFIED
        public static class TFTV_Utils_GetDamageKeywordValue_DamagePredictionMutations_Patch
        {
            public static void Postfix(DamagePayload payload, DamageKeywordDef damageKeyword, TacticalActor tacticalActor, ref float __result)
            {
                try
                {
                    SharedData shared = GameUtl.GameComponent<SharedData>();
                    SharedDamageKeywordsDataDef damageKeywords = shared.SharedDamageKeywords;

                    CorruptionStatusDef corruptionStatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");


                    if (tacticalActor != null && corruptionStatusDef.DamageTypeDefs.Contains(damageKeyword.DamageTypeDef) && damageKeyword != damageKeywords.SyphonKeyword) //&& damageKeyword is PiercingDamageKeywordDataDef == false) 
                    {

                        float numberOfMutations = 0;

                        //   TFTVLogger.Always("GetDamageKeywordValue check passed");

                        foreach (TacticalItem armourItem in tacticalActor.BodyState.GetArmourItems())
                        {
                            if (armourItem.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag))
                            {
                                numberOfMutations++;
                            }
                        }

                        if (numberOfMutations > 0)
                        {
                            // TFTVLogger.Always("damage value is " + payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage));

                            __result = payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage) * (1f + (numberOfMutations * 2) / 100 * (float)tacticalActor.CharacterStats.Corruption);
                            // TFTVLogger.Always($"GetDamageKeywordValue invoked for {tacticalActor.DisplayName} and result is {__result}");
                            //  TFTVLogger.Always("result is " + __result +", damage increase is " + (1f + (((numberOfMutations * 2) / 100) * (float)tacticalActor.CharacterStats.Corruption)));
                        }

                        if (tacticalActor.Status != null && tacticalActor.Status.HasStatus<TacStrengthDamageMultiplierStatus>())
                        {

                            float endurance = tacticalActor.CharacterStats.Endurance.Value.EndValue;
                            __result += payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage) * endurance / 200f;

                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }








        /// <summary>
        /// This method is run on Tactical Start and it removes the damage prediction bar when aiming, because it's inaccurate
        /// </summary>

        public static void RemoveDamagePredictionBar()
        {
            try
            {
                if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null)
                {
                    UIModuleShootTargetHealthbar uIModuleShootTargetHealthbar = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>().View.TacticalModules.ShootTargetHealthBar;
                    uIModuleShootTargetHealthbar.ModulePanel.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("DamagePrediction")).gameObject.SetActive(false);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /// <summary>
        /// The aiming prediction simulates the weapon on its own (no ability is passed down to the
        /// DamagePredictor), so it fires one execution: abilities that shoot several bursts, such as
        /// Aimed Burst (2) or Gunslinger, showed the damage of only the first. While the shoot states
        /// run their prediction, Weapon.GetNumberOfShots is asked for the ability's execution count.
        /// </summary>
        private static int _predictionExecutions;

        private static Func<UIStateShoot, ShootAbility> _getShootAbility;

        private static void BeginPrediction(UIStateShoot state)
        {
            try
            {
                if (_getShootAbility == null)
                {
                    _getShootAbility = AccessTools.MethodDelegate<Func<UIStateShoot, ShootAbility>>(AccessTools.PropertyGetter(typeof(UIStateShoot), "_shootAbility"));
                }

                _predictionExecutions = _getShootAbility(state)?.ShootAbilityDef?.ExecutionsCount ?? 0;
            }
            catch (Exception e)
            {
                _predictionExecutions = 0;
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIStateShoot), "SimulateDamagePrediction", new Type[] { typeof(Weapon), typeof(Vector3), typeof(TacticalAbilityTarget), typeof(bool) })]
        public static class UIStateShoot_SimulateDamagePrediction_Executions_Patch
        {
            public static void Prefix(UIStateShoot __instance) => BeginPrediction(__instance);
            public static void Finalizer() => _predictionExecutions = 0;
        }

        [HarmonyPatch(typeof(UIStateFreeCam), "SimulateDamagePrediction", new Type[] { typeof(Weapon), typeof(Vector3), typeof(TacticalAbilityTarget), typeof(IDamageReceiver) })]
        public static class UIStateFreeCam_SimulateDamagePrediction_Executions_Patch
        {
            public static void Prefix(UIStateFreeCam __instance) => BeginPrediction(__instance);
            public static void Finalizer() => _predictionExecutions = 0;
        }

        [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetNumberOfShots))]
        public static class Weapon_GetNumberOfShots_PredictionExecutions_Patch
        {
            public static void Prefix(ref int executionsCount)
            {
                if (_predictionExecutions > 1 && executionsCount == 1)
                {
                    executionsCount = _predictionExecutions;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateShoot), "GetMinMaxPossibleDamage")] //VERIFIED
        public static class UIStateShoot_GetMinMaxPossibleDamage_patch
        {
            public static bool Prefix() // GetMinMaxPossibleDamage is static: no __instance
            {
                try
                {

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
