using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDelirium
    {
      //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        // Patch to increase damage by 2% per mutated body part per Delirium point



        [HarmonyPatch(typeof(CorruptionStatus), "GetMultiplier")]
        internal static class BG_CorruptionStatus_GetMultiplier_Mutations_patch
        {

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref float __result, CorruptionStatus __instance)
            {
                try
                {
                    float numberOfMutations = 0;

                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);

                    foreach (TacticalItem armourItem in base_TacticalActor.BodyState.GetArmourItems())
                    {
                        if (armourItem.GameTags.Contains(mutationTag))
                        {
                            numberOfMutations++;
                        }
                    }

                    if (numberOfMutations > 0)
                    {
                        __result = 1f + (numberOfMutations * 2) / 100 * (float)base_TacticalActor.CharacterStats.Corruption;
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //General method to calculate max Delirium a character on geo can have taking into account ODI and bionics
        public static float CalculateMaxCorruption(GeoCharacter character)
        {

            try
            {
                float maxCorruption = 0;
                int bionics = 0;
                int currentODIlevel = character.Faction.GeoLevel.EventSystem.GetVariable("BC_SDI");
               // TFTVLogger.Always("CurrentODIlevel is " + currentODIlevel);
                int odiPerc = currentODIlevel * 100 / TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;
              //  TFTVLogger.Always("odiPerc is " + odiPerc);

                foreach (GeoItem bionic in character.ArmourItems)
                {
                    if (bionic.ItemDef.Tags.Contains(bionicalTag))

                        bionics += 1;
                }

                //For Project Osiris
                if (bionics == 3) 
                { 
                return maxCorruption;
                
                }

                if (!TFTVVoidOmens.VoidOmensCheck[10])
                {
                    if (odiPerc < 25)
                    {
                        maxCorruption = character.CharacterStats.Willpower.IntMax / 3;

                        if (bionics == 1)
                        {
                            return maxCorruption -= maxCorruption * 0.33f;
                        }

                        if (bionics == 2)
                        {
                            return maxCorruption -= maxCorruption * 0.66f;
                        }
                        else
                        {
                            return maxCorruption;
                        }
                    }
                    else
                    {
                        if (odiPerc < 45)
                        {
                            maxCorruption = character.CharacterStats.Willpower.IntMax * 1 / 2;

                            if (bionics == 1)
                            {
                                return maxCorruption -= maxCorruption * 0.33f;
                            }

                            if (bionics == 2)
                            {
                                return maxCorruption -= maxCorruption * 0.66f;
                            }
                            else
                            {
                                return maxCorruption;
                            }
                        }
                        else // > 75%
                        {
                            maxCorruption = character.CharacterStats.Willpower.IntMax;

                            if (bionics == 1)
                            {
                                return maxCorruption -= maxCorruption * 0.33f;
                            }

                            if (bionics == 2)
                            {
                                return maxCorruption -= maxCorruption * 0.66f;
                            }

                            else
                            {
                                return maxCorruption;
                            }
                        }
                    }
                }
                else
                {
                    maxCorruption = character.CharacterStats.Willpower.IntMax;

                    if (bionics == 1)
                    {
                        return maxCorruption -= maxCorruption * 0.33f;
                    }

                    if (bionics == 2)
                    {
                        return maxCorruption -= maxCorruption * 0.66f;
                    }

                    else
                    {
                        return maxCorruption;
                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            throw new InvalidOperationException();
        }

        // General method to calculate Stamina effect on Delirium, where each 10 stamina reduces Delirium effects by 1
        public static int CalculateStaminaEffectOnDelirium(GeoCharacter character)
        {
            {
                try
                {
                    if (character.Fatigue != null && (character.TemplateDef.IsHuman || character.TemplateDef.IsMutoid))
                    {

                        if (character.Fatigue.Stamina == 40)
                        {
                            return 4;
                        }
                        else if (character.Fatigue.Stamina < 40 && character.Fatigue.Stamina >= 30)
                        {
                            return 3;
                        }
                        else if (character.Fatigue.Stamina < 30 && character.Fatigue.Stamina >= 20)
                        {
                            return 2;
                        }
                        else if (character.Fatigue.Stamina < 20 && character.Fatigue.Stamina >= 10)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (System.Exception e)
                {
                    TFTVLogger.Error(e);
                }

                throw new InvalidOperationException();
            }
        }


        // Harmony patch to change the result of CorruptionStatus.CalculateValueIncrement() to be capped by ODI
        // When ODI is <25%, max corruption is 1/3, between 25 and 50% ODI, max corruption is 2/3, and ODI >50%, corruption can be 100%
        // Tell Harmony what original method in what class should get patched, the following class after this directive will be used to perform own code by injection
        [HarmonyPatch(typeof(CorruptionStatus), "CalculateValueIncrement")]

        // The class that holds the code we want to inject, the name can be anything, but the more accurate the better it is for bug hunting
        internal static class BC_CorruptionStatus_CalculateValueIncrement_patch
        {
            // This directive is only to prevent a VS message that the following method is never called (it will be called, but through Harmony and not our mod code)
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]

            // Finally the method that is called before (Prefix) or after (Postfix) the original method
            // In our case we use Postfix that is called after 'CalculateValueIncrement' was executed
            // The parameters are special variables with their names defined by Harmony:
            // 'ref int __result' is the return value of the original method 'CalculateValueIncrement' and with the prefix 'ref' we get write access to change it (without it would be readonly)
            // 'CorruptionStatus __instance' is status object that holds the original method, each character will have its own instance of this status and so we have access to their individual stats
            private static void Postfix(ref int __result, CorruptionStatus __instance)
            {
                // 'try ... catch' to make the code more stable, errors will most likely not result in game crashes or freezes but log an error message in the mods log file
                try
                {
                    // With Harmony patches we cannot directly access base.TacticalActor, Harmony's AccessTools uses Reflection to get it through the backdoor
                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);

                    //Check for bionics
                    int numberOfBionics = 0;

                    foreach (TacticalItem armourItem in base_TacticalActor.BodyState.GetArmourItems())
                    {
                        if (armourItem.GameTags.Contains(bionicalTag))
                        {
                            numberOfBionics++;
                        }
                    }

                    // Calculate the percentage of current ODI level, these two variables are globally set by our ODI event patches
                    int odiPerc = TFTVSDIandVoidOmenRoll.CurrentODI_Level * 100 / TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;
                  
                    int maxCorruption = 0;
                    // Get max corruption dependent on max WP of the selected actor
                    if (!TFTVVoidOmens.VoidOmensCheck[10])
                    {

                        if (odiPerc < 25)
                        {
                            maxCorruption = base_TacticalActor.CharacterStats.Willpower.IntMax / 3;

                            if (numberOfBionics == 1)
                            {
                                maxCorruption -= (int)(maxCorruption * 0.33);
                            }

                            if (numberOfBionics == 2)
                            {
                                maxCorruption -= (int)(maxCorruption * 0.66);
                            }

                        }
                        else
                        {
                            if (odiPerc < 45)
                            {
                                maxCorruption = base_TacticalActor.CharacterStats.Willpower.IntMax * 1 / 2;

                                if (numberOfBionics == 1)
                                {
                                    maxCorruption -= (int)(maxCorruption * 0.33);
                                }

                                if (numberOfBionics == 2)
                                {
                                    maxCorruption -= (int)(maxCorruption * 0.66);
                                }
                            }
                            else // > 50%
                            {
                                maxCorruption = base_TacticalActor.CharacterStats.Willpower.IntMax;

                                if (numberOfBionics == 1)
                                {
                                    maxCorruption -= (int)(maxCorruption * 0.33);
                                }

                                if (numberOfBionics == 2)
                                {
                                    maxCorruption -= (int)(maxCorruption * 0.66);
                                }

                            }
                        }
                    }
                    if (TFTVVoidOmens.VoidOmensCheck[10])
                    {
                        maxCorruption = base_TacticalActor.CharacterStats.Willpower.IntMax;

                        if (numberOfBionics == 1)
                        {
                            maxCorruption -= (int)(maxCorruption * 0.33);
                        }

                        if (numberOfBionics == 2)
                        {
                            maxCorruption -= (int)(maxCorruption * 0.66);
                        }
                    }

                    //For Project Osiris
                    if (numberOfBionics == 3)
                    {
                        maxCorruption=0;
                    }

                    // Like the original calculation, but adapted with 'maxCorruption'
                    // Also '__result' for 'return', '__instance' for 'this' and 'base_TacticalActor' for 'base.TacticalActor'
                    __result = Mathf.Min(__instance.CorruptionStatusDef.ValueIncrement, maxCorruption - base_TacticalActor.CharacterStats.Corruption.IntValue);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        // Dictionary to transfer the characters geoscape stamina to tactical level by actor ID
        public static Dictionary<GeoTacUnitId, int> StaminaMap = new Dictionary<GeoTacUnitId, int>();

        // Harmony patch to save the characters geoscape stamina by acor ID, this mehtod is called in the deployment phase before switching to tactical mode
        [HarmonyPatch(typeof(CharacterFatigue), "ApplyToTacticalInstance")]

        internal static class BC_CharacterFatigue_ApplyToTacticalInstance_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(CharacterFatigue __instance, TacCharacterData data)
            {
                try
                {
                    //Logger.Always($"BC_CharacterFatigue_ApplyToTacticalInstance_Patch.POSTFIX called, GeoUnitID {data.Id} with {__instance.Stamina.IntValue} stamina added to dictionary.", false);
                    if (StaminaMap.ContainsKey(data.Id))
                    {
                        StaminaMap[data.Id] = __instance.Stamina.IntValue;
                    }
                    else
                    {
                        StaminaMap.Add(data.Id, __instance.Stamina.IntValue);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        // Harmony patch to change the result of CorruptionStatus.GetStatModification() to take Stamina into account
        // Corruption application get reduced by 100% when Stamina is between 35-40, by 75% between 30-35, by 50% between 25-30.
        [HarmonyPatch(typeof(CorruptionStatus), "GetStatModification")]
        internal static class BC_CorruptionStatus_GetStatModification_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            // We use again Postfix that is called after 'GetStatModification' was executed
            // 'ref StatModification __result' is the return value of the original method 'GetStatModification'
            // 'CorruptionStatus __instance' again like above the status object that holds the original method for each character
            private static void Postfix(ref StatModification __result, CorruptionStatus __instance)
            {
                try
                {
                    // With Harmony patches we cannot directly access base.TacticalActor, Harmony's AccessTools uses Reflection to get it through the backdoor
                    TacticalActor tacticalActor = __instance.TacticalActor; //(TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);

                    // Get characters geoscape stamina by his actor ID

                    int stamina = 40;
                    if (StaminaMap.ContainsKey(tacticalActor.GeoUnitId))
                    {
                        stamina = StaminaMap[tacticalActor.GeoUnitId];
                    }

                    // Calculate WP reduction dependent on stamina
                    float wpReduction = tacticalActor.CharacterStats.Corruption;

                    if (TFTVVoidOmens.VoidOmensCheck[3])
                    {
                        wpReduction = 0;
                    }
                    else
                    {
                        wpReduction = tacticalActor.CharacterStats.Corruption; // stamina between 0 and 10

                        if (stamina == 40)
                        {
                            if (tacticalActor.CharacterStats.Corruption >= 4)
                            {
                                wpReduction = tacticalActor.CharacterStats.Corruption - 4;
                            }
                            else
                            {
                                wpReduction = 0;
                            }
                        }
                        else if (stamina >= 30 && stamina < 40)
                        {
                            if (tacticalActor.CharacterStats.Corruption >= 3)
                            {
                                wpReduction = tacticalActor.CharacterStats.Corruption - 3;
                            }
                            else
                            {
                                wpReduction = 0;
                            }
                        }
                        else if (stamina >= 20 && stamina < 30)
                        {
                            if (tacticalActor.CharacterStats.Corruption >= 2)
                            {
                                wpReduction = tacticalActor.CharacterStats.Corruption - 2;
                            }
                            else
                            {
                                wpReduction = 0;
                            }
                        }
                        else if (stamina >= 10 && stamina < 20)
                        {
                            if (tacticalActor.CharacterStats.Corruption >= 1)
                            {
                                wpReduction = tacticalActor.CharacterStats.Corruption - 1;
                            }
                            else
                            {
                                wpReduction = 0;
                            }

                        }
                    }

                    // Like the original calculation, but adapted with 'maxCorruption'
                    __result = new StatModification(StatModificationType.Add,
                                                    StatModificationTarget.Willpower.ToString(),
                                                    -wpReduction,
                                                    __instance.CorruptionStatusDef,
                                                    -wpReduction);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoCharacter), "CureCorruption")]
        public static class GeoCharacter_CureCorruption_SetStaminaTo0_patch
        {
            private static readonly PassiveModifierAbilityDef InnerSight_AbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("InnerSight_AbilityDef");
            private static readonly PassiveModifierAbilityDef AnxietyAbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("AnxietyAbilityDef");
            private static readonly PassiveModifierAbilityDef Hyperalgesia_AbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("Hyperalgesia_AbilityDef");
            private static readonly PassiveModifierAbilityDef FasterSynapses_AbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("FasterSynapses_AbilityDef");
            private static readonly PassiveModifierAbilityDef Terror_AbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("Terror_AbilityDef");
            private static readonly ApplyStatusAbilityDef Wolverine_AbilityDef =DefCache.GetDef<ApplyStatusAbilityDef>("Wolverine_AbilityDef");
            private static readonly TacticalAbilityDef Derealization_AbilityDef =DefCache.GetDef<TacticalAbilityDef>("DerealizationIgnorePain_AbilityDef");
            private static readonly ApplyStatusAbilityDef feral_AbilityDef =DefCache.GetDef<ApplyStatusAbilityDef>("Feral_AbilityDef");
            private static readonly PassiveModifierAbilityDef OneOfThem_AbilityDef =DefCache.GetDef<PassiveModifierAbilityDef>("OneOfThemPassive_AbilityDef");
            private static readonly ApplyStatusAbilityDef bloodthirsty_AbilityDef =DefCache.GetDef<ApplyStatusAbilityDef>("Bloodthirsty_AbilityDef");

            public static void Postfix(GeoCharacter __instance)
            {
                try
                {
                    List<TacticalAbilityDef> abilityList = new List<TacticalAbilityDef>
                    { InnerSight_AbilityDef, AnxietyAbilityDef, Hyperalgesia_AbilityDef, FasterSynapses_AbilityDef, Terror_AbilityDef, Wolverine_AbilityDef, Derealization_AbilityDef, feral_AbilityDef,
                    OneOfThem_AbilityDef, bloodthirsty_AbilityDef
                    };

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int num = UnityEngine.Random.Range(0, 200);
                    TFTVLogger.Always("Treatment rolled " + num);

                    if (num >= 0 && num <= 50)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            TacticalAbilityDef abilityToAdd = abilityList.GetRandomElement();
                            TFTVLogger.Always("The randomly chosen ability is " + abilityToAdd.name);
                            if (!__instance.Progression.Abilities.Contains(abilityToAdd))
                            {

                                __instance.Progression.AddAbility(abilityToAdd);
                                //__instance.Faction.GeoLevel.View.GeoscapeModules.TutorialModule.SetTutorialStep(test, false);
                                GameUtl.GetMessageBox().ShowSimplePrompt($"{__instance.GetName()}" + " appears to be afflicted with " + $"<b>{abilityToAdd.ViewElementDef.DisplayName1.LocalizeEnglish()}</b>"
                                    + " as a result of the experimental mutagen treatment. This condition is likely to be permanent."
                                    + "\n\n" + $"<i>{abilityToAdd.ViewElementDef.Description.LocalizeEnglish()}</i>", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                                TFTVLogger.Always("Added ability " + abilityToAdd.ViewElementDef.DisplayName1.LocalizeEnglish());
                                i = 100;
                            }
                        }
                    }
                    else if (num > 50 && num <= 125)
                    {
                        TFTVCommonMethods.SetStaminaToZero(__instance);
                        GameUtl.GetMessageBox().ShowSimplePrompt($"{__instance.GetName()}" + " did not suffer any lasting side effects, but had to be heavily sedated"
                                    + "\n\n" + $"<i>STAMINA reduced to zero</i>", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //When getting an augment, each augment reduces corruption by a 1/3
        [HarmonyPatch(typeof(UIModuleBionics), "OnAugmentApplied")]
        public static class UIModuleBionics_OnAugmentApplied_SetStaminaTo0_patch
        {
            public static void Postfix(UIModuleBionics __instance)
            {
                try
                {
                    //check number of augments the character has
                    
                   // int numberOfBionics = AugmentScreenUtilities.GetNumberOfBionicsAugmentations(__instance.CurrentCharacter);

                  //  for (int i = 0; i < numberOfBionics; i++)
                    //{
                        if (__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33 >= 0)
                        {
                            __instance.CurrentCharacter.CharacterStats.Corruption.Set((float)(__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33));
                        }
                        else
                        {
                            __instance.CurrentCharacter.CharacterStats.Corruption.Set(0);
                        }
                //    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


    }
}
