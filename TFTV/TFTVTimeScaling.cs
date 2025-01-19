using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using PhoenixPoint.Common.Entities.GameTagsTypes;

namespace TFTV
{
    internal class TFTVTimeScaling
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void EnsureStandardTimeScaleForIdleActor(IdleAbility ability)
        {
            try
            {

                TacticalActor tacticalActor = ability.TacticalActor;

                if (tacticalActor != null)
                {
                    OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();

                    //  TFTVLogger.Always($"ending IdleAbility for {tacticalActor.DisplayName}");
                    tacticalActor.TimingScale.Timing.Scale = optionsManager.CurrentGameplayOptions.AnimationSpeedLevel; //1.1f;
                    TacTimeScaleRegulator tacTimeScaleRegulator = tacticalActor.TacticalLevel.GetComponent<TacTimeScaleRegulator>();
                    MethodInfo methodInfoApplyScaleToActor = typeof(TacTimeScaleRegulator).GetMethod("ApplyScaleToActor", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo methodInfoUpdateCurrentTimeScale = typeof(TacTimeScaleRegulator).GetMethod("UpdateCurrentTimeScale", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfoUpdateCurrentTimeScale.Invoke(tacTimeScaleRegulator, new object[] { });
                    methodInfoApplyScaleToActor.Invoke(tacTimeScaleRegulator, new object[] { tacticalActor });
                }

                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(TacTimeScaleRegulator), "UpdateCurrentTimeScale")]
        public static class TacTimeScaleRegulator_UpdateCurrentTimeScale_patch
        {
            public static void Postfix(TacTimeScaleRegulator __instance)
            {
                try
                {

                    OptionsManager optionsManager = GameUtl.GameComponent<OptionsManager>();
                    TacticalLevelController controller = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();
                    if (controller != null)
                    {

                        float speedMultiplier = optionsManager.CurrentGameplayOptions.AnimationSpeedLevel;
                        //  TFTVLogger.Always($"speedmultiplier: {speedMultiplier}, speedMultiplier+1/4: {(1+speedMultiplier) / 4}");
                        controller.OverwatchTimeScale = 0.1f * ((1 + speedMultiplier) / 4);
                        //  TFTVLogger.Always($"OverwatchTimeScale: {controller.OverwatchTimeScale}");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(TacTimeScaleRegulator), "ApplyScaleToActor")]
        public static class TacTimeScaleRegulator_ApplyScaleToActor_patch
        {
            public static bool Prefix(TacTimeScaleRegulator __instance, TacticalActor tacActor)
            {
                try
                {
                    if (!tacActor.IsAlive)
                    {
                        return false;
                    }

                    if (tacActor.IdleAbility.IsExecuting) //&& !tacActor.HasGameTag(DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef")))//tacActor.TimingScale.Timing.Scale == 1f)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(IdleAbility), "Activate")]
        public static class IdleAbility_Activate_patch
        {
            public static void Prefix(IdleAbility __instance)
            {
                try
                {

                    TacticalActor tacticalActor = __instance.TacticalActor;

                    if (tacticalActor != null)
                    {
                        // TFTVLogger.Always($"activating IdleAbility for {tacticalActor.DisplayName}");
                        TacTimeScaleRegulator tacTimeScaleRegulator = tacticalActor.TacticalLevel.GetComponent<TacTimeScaleRegulator>();
                        tacticalActor.TimingScale.RemoveScale(tacTimeScaleRegulator);
                        float num = 1;
                        tacticalActor.TimingScale.AddScale(num, tacTimeScaleRegulator);
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
