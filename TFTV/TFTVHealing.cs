using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using System.Reflection;
using PhoenixPoint.Tactical.View.ViewModules;

namespace TFTV
{
    internal class TFTVHealing
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static TacticalActor mutoidReceivingHealing = null;

        [HarmonyPatch(typeof(HealAbility), "HealTargetCrt")]

        public static class HealAbility_HealTargetCrt_Mutoid_Patch
        {
            public static void Prefix(PlayingAction action)
            {
                try
                {
                    if ((TacticalActor)((TacticalAbilityTarget)action.Param).Actor != null)
                    {
                        TacticalActor actor = (TacticalActor)((TacticalAbilityTarget)action.Param).Actor;
                        if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef")))
                        {
                            mutoidReceivingHealing = actor;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "get_GeneralHealAmount")]

        public static class HealAbility_Mutoid_Patch
        {

            public static void Postfix(HealAbility __instance, ref float __result)
            {
                try
                {

                    if (mutoidReceivingHealing != null)
                    {
                        __result = 0;
                        mutoidReceivingHealing = null;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "ShouldReturnTarget")]

        public static class TFTV_HealAbility_ShouldReturnTarget_NanoKit_Patch
        {
            public static void Postfix(HealAbility __instance, TacticalActor healer, TacticalActor targetActor, ref bool __result)
            {
                try
                {
                   // TFTVLogger.Always($"HealAbility {__instance.HealAbilityDef.name}");

                    if (__instance.HealAbilityDef.name.Equals("DoTMedkit"))
                    {
                        __result = false;

                        MethodInfo methodInfo = typeof(HealAbility).GetMethod("HealEffectConditionsMet", BindingFlags.NonPublic | BindingFlags.Static);

                        foreach (HealAbilityDef.ConditionalHealEffect healEffect in __instance.HealAbilityDef.HealEffects)
                        {
                            bool result = (bool)methodInfo.Invoke(__instance, new object[] { healEffect, healer, targetActor });

                            if (result)
                            {
                                __result = true;
                            }
                        }                       
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //DoT Medkit UI adjustments to prevent 0 healing appearing when selecting ability
        [HarmonyPatch(typeof(UIModuleAbilityConfirmationButton), "SetAbility")]
        public static class UIModuleAbilityConfirmationButton_SetAbility_patch
        {
            public static void Postfix(UIModuleAbilityConfirmationButton __instance, TacticalAbility ability)
            {
                try
                {
                    // EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");


                    if (ability.AbilityDef.name == "DoTMedkit")
                    {
                        //   TFTVLogger.Always($"the ability is {ability.AbilityDef.name}");                    
                        __instance.DamageTypeTemplateShort.gameObject.SetActive(false);
                        __instance.DamageTypeTemplateExtended.gameObject.SetActive(false);


                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleWeaponSelection), "SetHealAmount")]
        public static class UIModuleWeaponSelection_SetHealAmount_patch
        {
            public static void Prefix(UIModuleWeaponSelection __instance, ref float amount)
            {
                try
                {
                    if (amount <= 1)
                    {

                        amount = 0;
                    }



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
