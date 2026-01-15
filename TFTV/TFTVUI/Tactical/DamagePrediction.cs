using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Linq;
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


        [HarmonyPatch(typeof(UIStateShoot), "GetMinMaxPossibleDamage")] //VERIFIED
        public static class UIStateShoot_GetMinMaxPossibleDamage_patch
        {
            public static bool Prefix(UIStateShoot __instance)
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
