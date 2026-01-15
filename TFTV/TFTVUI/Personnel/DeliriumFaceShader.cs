using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.View.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVUI.Personnel
{
    internal class DeliriumFaceShader
    {
        public static GeoCharacter HookToCharacterForDeliriumShader = null;

        //Patch to reduce Delirium visuals on faces of infected characters

        [HarmonyPatch(typeof(UIModuleActorCycle), "SetupFaceCorruptionShader")] //VERIFIED
        class TFTV_UIoduleActorCycle_SetupFaceCorruptionShader_Hook_Patch
        {
            private static void Prefix(UIModuleActorCycle __instance)
            {
                try
                {

                    HookToCharacterForDeliriumShader = __instance.CurrentCharacter;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void Postfix(UIModuleActorCycle __instance)
            {
                try
                {

                    HookToCharacterForDeliriumShader = null;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        public static TacticalActor HookCharacterStatsForDeliriumShader = null;


        [HarmonyPatch(typeof(SquadMemberScrollerController), "SetupFaceCorruptionShader")] //VERIFIED

        class TFTV_SquadMemberScrollerController_SetupFaceCorruptionShader
        {
            private static void Prefix(TacticalActor actor)
            {
                try
                {
                    HookCharacterStatsForDeliriumShader = actor;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static void Postfix()
            {
                try
                {
                    HookCharacterStatsForDeliriumShader = null;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(CharacterStats), "get_CorruptionProgressRel")] //VERIFIED
        internal static class TFTV_UI_CharacterStats_DeliriumFace_patch
        {
            private static void Postfix(ref float __result, CharacterStats __instance)
            {
                try
                {
                    // Type targetType = typeof(UIModuleActorCycle);
                    // FieldInfo geoCharacterField = targetType.GetField("GeoCharacter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (HookToCharacterForDeliriumShader != null)
                    {
                        GeoCharacter geoCharacter = HookToCharacterForDeliriumShader;

                        if (__instance.Corruption > 0 && geoCharacter != null)//hookToCharacter != null)
                        {

                            if (__instance.Corruption - TFTVDelirium.CalculateStaminaEffectOnDelirium(geoCharacter) > 0)
                            {
                                __result = ((geoCharacter.CharacterStats.Corruption - (TFTVDelirium.CalculateStaminaEffectOnDelirium(geoCharacter))) / 20);
                            }
                            else
                            {
                                __result = 0.05f;
                            }
                        }
                    }
                    if (HookCharacterStatsForDeliriumShader != null)
                    {
                        if (__instance == HookCharacterStatsForDeliriumShader.CharacterStats)
                        {
                            int stamina = 40;

                            if (TFTVDelirium.StaminaMap.ContainsKey(HookCharacterStatsForDeliriumShader.GeoUnitId))
                            {
                                stamina = TFTVDelirium.StaminaMap[HookCharacterStatsForDeliriumShader.GeoUnitId];
                            }


                            if (__instance.Corruption > 0)//hookToCharacter != null)
                            {

                                if (__instance.Corruption - stamina / 10 > 0)
                                {
                                    __result = ((__instance.Corruption - (stamina / 10)) / 20);
                                }
                                else
                                {
                                    __result = 0.05f;
                                }
                            }

                            //  TFTVLogger.Always($"corruption shader result is {__result}");
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
