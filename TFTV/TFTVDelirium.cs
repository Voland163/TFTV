using Base;
using Base.Core;
using Base.Entities.Statuses;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
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
using Random = System.Random;

namespace TFTV
{
    internal class TFTVDelirium
    {
        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        // Patch to increase damage by 2% per mutated body part per Delirium point

        private static readonly TacticalAbilityDef InnerSight_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("InnerSight_AbilityDef");
        private static readonly TacticalAbilityDef Terror_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("Terror_AbilityDef");
        private static readonly TacticalAbilityDef feralDeliriumPerk = DefCache.GetDef<TacticalAbilityDef>("FeralNew_AbilityDef");
        private static readonly TacticalAbilityDef hyperalgesiaAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Hyperalgesia_AbilityDef");
        private static readonly TacticalAbilityDef feralAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Feral_AbilityDef");
        private static readonly TacticalAbilityDef bloodthirstyAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Bloodthirsty_AbilityDef");
        private static readonly TacticalAbilityDef fasterSynapsesDef = DefCache.GetDef<TacticalAbilityDef>("FasterSynapses_AbilityDef");
        private static readonly TacticalAbilityDef anxietyDef = DefCache.GetDef<TacticalAbilityDef>("AnxietyAbilityDef");
        private static readonly TacticalAbilityDef newAnxietyDef = DefCache.GetDef<TacticalAbilityDef>("NewAnxietyAbilityDef");
        private static readonly TacticalAbilityDef oneOfThemDef = DefCache.GetDef<TacticalAbilityDef>("OneOfThemPassive_AbilityDef");
        private static readonly TacticalAbilityDef wolverineDef = DefCache.GetDef<TacticalAbilityDef>("Wolverine_AbilityDef");
        private static readonly TacticalAbilityDef wolverineCuredDef = DefCache.GetDef<TacticalAbilityDef>("WolverineCured_AbilityDef");
        private static readonly TacticalAbilityDef derealizationDef = DefCache.GetDef<TacticalAbilityDef>("DerealizationIgnorePain_AbilityDef");
        private static readonly TacticalAbilityDef newDerealizationDef = DefCache.GetDef<TacticalAbilityDef>("Derealization_AbilityDef");

        private static readonly List<TacticalAbilityDef> DeliriumPerks = new List<TacticalAbilityDef>() {InnerSight_AbilityDef, Terror_AbilityDef, feralDeliriumPerk, hyperalgesiaAbilityDef,
        feralAbilityDef, bloodthirstyAbilityDef, fasterSynapsesDef, anxietyDef, newAnxietyDef, oneOfThemDef, wolverineDef, derealizationDef, newDerealizationDef};

        public static Dictionary<int, int> CharactersDeliriumPerksAndMissions = new Dictionary<int, int>();

        [HarmonyPatch(typeof(CorruptionStatus), "GetMultiplier")]
        internal static class TFTV_CorruptionStatus_GetMultiplier_Mutations_patch
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

        public static string CurrentDeliriumLevel(GeoLevelController controller)
        {
            try
            {
                string currentDeliriumLevel = "";

                string maxDelirium = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_CAP_MAX" }.Localize();
                string medDelirium = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_CAP_MED" }.Localize();
                string lowDelirium = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_CAP_LOW" }.Localize();

                int currentODIlevel = controller.EventSystem.GetVariable("BC_SDI");
                // TFTVLogger.Always("CurrentODIlevel is " + currentODIlevel);
                int odiPerc = currentODIlevel * 100 / 20; //TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;

                if (TFTVVoidOmens.VoidOmensCheck[10] || odiPerc >= 45)
                {
                    currentDeliriumLevel = maxDelirium;
                }
                else if (odiPerc < 25)
                {
                    currentDeliriumLevel = medDelirium;
                }
                else if (odiPerc < 45)
                {
                    currentDeliriumLevel = lowDelirium;
                }


                return currentDeliriumLevel;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        //General method to calculate max Delirium a character on geo can have taking into account ODI and bionics
        public static float CalculateMaxCorruption(GeoCharacter character)
        {

            try
            {
                float bonusWillpower = 0;


                if (character.Progression != null)
                {

                    foreach (PassiveModifierAbilityDef passiveModifier in character.PassiveModifiers)
                    {
                        ItemStatModification[] statModifications = passiveModifier.StatModifications;
                        foreach (ItemStatModification statModifier2 in statModifications)
                        {
                            if (statModifier2.TargetStat == StatModificationTarget.Willpower)
                            {
                                bonusWillpower += statModifier2.Value;
                            }

                        }
                    }
                }

                float maxCorruption = 0;
                int bionics = 0;
                int currentODIlevel = character.Faction.GeoLevel.EventSystem.GetVariable("BC_SDI");
                // TFTVLogger.Always("CurrentODIlevel is " + currentODIlevel);
                int odiPerc = currentODIlevel * 100 / 20; //TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;
                //  TFTVLogger.Always("odiPerc is " + odiPerc);

                int actualWillpower = (int)(character.CharacterStats.Willpower.IntMax + bonusWillpower);

                foreach (GeoItem bionic in character.ArmourItems)
                {
                    if (bionic.ItemDef.Tags.Contains(bionicalTag) && !bionic.ItemDef.Tags.Contains(TFTVChangesToDLC5.MercenaryTag))

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
                        maxCorruption = actualWillpower / 3;

                        if (bionics == 1)
                        {
                            maxCorruption -= maxCorruption * 0.33f;
                        }

                        if (bionics == 2)
                        {
                            maxCorruption -= maxCorruption * 0.66f;
                        }

                    }
                    else
                    {
                        if (odiPerc < 45)
                        {
                            maxCorruption = actualWillpower * 1 / 2;

                            if (bionics == 1)
                            {
                                maxCorruption -= maxCorruption * 0.33f;
                            }

                            if (bionics == 2)
                            {
                                maxCorruption -= maxCorruption * 0.66f;
                            }

                        }
                        else // > 75%
                        {
                            maxCorruption = actualWillpower;

                            if (bionics == 1)
                            {
                                maxCorruption -= maxCorruption * 0.33f;
                            }

                            if (bionics == 2)
                            {
                                maxCorruption -= maxCorruption * 0.66f;
                            }

                        }
                    }
                }
                else
                {
                    maxCorruption = actualWillpower;

                    if (bionics == 1)
                    {
                        maxCorruption -= maxCorruption * 0.33f;
                    }

                    if (bionics == 2)
                    {
                        maxCorruption -= maxCorruption * 0.66f;
                    }



                }
                if (maxCorruption < character.CharacterStats.Corruption)
                {
                    character.CharacterStats.Corruption.Set(maxCorruption);

                }
                return maxCorruption;

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
                    int deliriumReduction = 0;

                    if (character.Fatigue != null && (character.TemplateDef.IsHuman || character.TemplateDef.IsMutoid))
                    {
                        // TFTVLogger.Always($"{character.DisplayName} has {character.Fatigue.Stamina}");


                        if (character.Fatigue.Stamina == 40)
                        {
                            deliriumReduction = 4;
                        }
                        else if (character.Fatigue.Stamina < 40 && character.Fatigue.Stamina >= 30)
                        {
                            deliriumReduction = 3;
                        }
                        else if (character.Fatigue.Stamina < 30 && character.Fatigue.Stamina >= 20)
                        {
                            deliriumReduction = 2;
                        }
                        else if (character.Fatigue.Stamina < 20 && character.Fatigue.Stamina >= 10)
                        {
                            deliriumReduction = 1;
                        }

                    }

                    //   TFTVLogger.Always($"so Delirium for {character.DisplayName} with {character.Fatigue.Stamina} should be reduced by {deliriumReduction}");


                    return deliriumReduction;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                throw new InvalidOperationException();
            }
        }


        //  AddAttackBoostStatus
        // ShootAbility


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
                    TacticalActor tacticalActor = __instance.TacticalActor; //(TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);

                    //Check for bionics
                    int numberOfBionics = 0;
                    float willFromArmor = 0;

                    foreach (TacticalItem armourItem in tacticalActor.BodyState.GetArmourItems())
                    {
                        if (armourItem.GameTags.Contains(bionicalTag) && !armourItem.GameTags.Contains(TFTVChangesToDLC5.MercenaryTag))
                        {
                            numberOfBionics++;
                        }

                        if (!armourItem.TacticalItemDef.IsPermanentAugment)
                        {
                            willFromArmor += armourItem.BodyPartAspect.BodyPartAspectDef.WillPower;

                        }
                    }

                    // Calculate the percentage of current ODI level, these two variables are globally set by our ODI event patches
                    int odiPerc = TFTVODIandVoidOmenRoll.CurrentODI_Level * 100 / 20; //TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;

                    int maxCorruption = tacticalActor.CharacterStats.Willpower.IntMax - (int)willFromArmor;

                    // Get max corruption dependent on max WP of the selected actor
                    if (!TFTVVoidOmens.VoidOmensCheck[10])
                    {
                        if (odiPerc < 25)
                        {
                            maxCorruption /= 3;

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
                                maxCorruption /= 2;

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
                                // maxCorruption = tacticalActor.CharacterStats.Willpower.IntMax;

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
                        // maxCorruption = tacticalActor.CharacterStats.Willpower.IntMax;

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
                        maxCorruption = 0;
                    }



                    // Like the original calculation, but adapted with 'maxCorruption'
                    // Also '__result' for 'return', '__instance' for 'this' and 'base_TacticalActor' for 'base.TacticalActor'

                    TacticalAbilityDef oneOfThemDef = DefCache.GetDef<TacticalAbilityDef>("OneOfThemPassive_AbilityDef");

                    if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(oneOfThemDef) != null)
                    {
                        __result = Mathf.Min(__instance.CorruptionStatusDef.ValueIncrement * 2, maxCorruption - tacticalActor.CharacterStats.Corruption.IntValue);
                        // TFTVLogger.Always($"Applying Delirium to {base_TacticalActor.DisplayName} with One of Them, {__result}");
                    }
                    else
                    {
                        __result = Mathf.Min(__instance.CorruptionStatusDef.ValueIncrement, maxCorruption - tacticalActor.CharacterStats.Corruption.IntValue);
                        // TFTVLogger.Always($"Applying Delirium to {base_TacticalActor.DisplayName}, {__result}");
                    }
                    // TFTVLogger.Always($"{base_TacticalActor.DisplayName} bionics: {numberOfBionics} odi {odiPerc} willpower max: {base_TacticalActor.CharacterStats.Willpower.IntMax}, max delirium {maxCorruption} " +
                    //  $"Delirium {base_TacticalActor.CharacterStats.Corruption.IntValue}, result: {__result} ");

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

        public static void RemoveDeliriumFromAllCharactersWithoutMutations(GeoLevelController controller)
        {
            try
            {
                GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                foreach (GeoCharacter character in controller.PhoenixFaction.HumanSoldiers)
                {


                    if (character.CharacterStats.Corruption != null
                        && character.CharacterStats.Corruption > 0
                        && !character.ArmourItems.Any(ai => ai.ItemDef.Tags.Contains(mutationTag)))
                    {
                        character.CharacterStats.Corruption.Set(0);

                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(GeoCharacter), "CureCorruption")]
        public static class GeoCharacter_CureCorruption_SetStaminaTo0_patch
        {
            private static readonly PassiveModifierAbilityDef InnerSight_AbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("InnerSight_AbilityDef");
            //  private static readonly PassiveModifierAbilityDef AnxietyAbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("AnxietyAbilityDef");
            private static readonly PassiveModifierAbilityDef Hyperalgesia_AbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("Hyperalgesia_AbilityDef");
            private static readonly PassiveModifierAbilityDef FasterSynapses_AbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("FasterSynapses_AbilityDef");
            private static readonly PassiveModifierAbilityDef Terror_AbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("Terror_AbilityDef");
            private static readonly ApplyStatusAbilityDef Wolverine_AbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("Wolverine_AbilityDef");
            //    private static readonly TacticalAbilityDef Derealization_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("DerealizationIgnorePain_AbilityDef");
            //    private static readonly ApplyStatusAbilityDef feral_AbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("Feral_AbilityDef");
            private static readonly PassiveModifierAbilityDef OneOfThem_AbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("OneOfThemPassive_AbilityDef");
            private static readonly ApplyStatusAbilityDef bloodthirsty_AbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("Bloodthirsty_AbilityDef");
            private static readonly PassiveModifierAbilityDef feralDeliriumPerk = DefCache.GetDef<PassiveModifierAbilityDef>("FeralNew_AbilityDef");
            private static readonly TacticalAbilityDef newAnxietyDef = DefCache.GetDef<TacticalAbilityDef>("NewAnxietyAbilityDef");
            private static readonly TacticalAbilityDef newDerealization_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("Derealization_AbilityDef");

            public static void Postfix(GeoCharacter __instance)
            {
                try
                {
                    List<TacticalAbilityDef> abilityList = new List<TacticalAbilityDef>
                    { Wolverine_AbilityDef, InnerSight_AbilityDef, newAnxietyDef, Hyperalgesia_AbilityDef, FasterSynapses_AbilityDef, Terror_AbilityDef,  newDerealization_AbilityDef,
                    OneOfThem_AbilityDef, bloodthirsty_AbilityDef, feralDeliriumPerk
                    };

                    if (__instance.GetBodyParts()
                        .Where(bp => bp?.RequiredSlotBinds.Count() > 0 && bp?.RequiredSlotBinds[0].RequiredSlot == DefCache.GetDef<ItemSlotDef>("Human_Torso_SlotDef")).
                        Any(bp => bp.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                    {
                        abilityList.Remove(Wolverine_AbilityDef);
                    }

                    if (__instance.GetTacticalAbilities().Contains(wolverineCuredDef) && abilityList.Contains(Wolverine_AbilityDef))
                    {
                        abilityList.Remove(Wolverine_AbilityDef);
                    }

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int num = UnityEngine.Random.Range(0, 200);
                    TFTVLogger.Always("Treatment rolled " + num);

                    if (num >= 0 && num <= 50)
                    {
                        List<TacticalAbilityDef> culledDeliriumPerksList = abilityList.Except(__instance.Progression.Abilities).ToList();

                        if (culledDeliriumPerksList.Count > 0)
                        {
                            //The mergele improvement
                            TacticalAbilityDef abilityToAdd = culledDeliriumPerksList.GetRandomElement(new Random((int)Stopwatch.GetTimestamp()));

                            TFTVLogger.Always("The randomly chosen ability is " + abilityToAdd.name);

                            __instance.Progression.AddAbility(abilityToAdd);

                            string afflictedWithConnector = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_PERK_PROMPT_CONNECTOR" }.Localize();
                            string afflictionRules = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_PERK_PROMPT_RULES" }.Localize();

                            string messagePrompt = $"{__instance.GetName()} {afflictedWithConnector} <b>{abilityToAdd.ViewElementDef.DisplayName1.LocalizeEnglish()}</b> {afflictionRules}"
                                + $"\n\n <i>{abilityToAdd.ViewElementDef.Description.LocalizeEnglish()}</i>";


                            GameUtl.GetMessageBox().ShowSimplePrompt(messagePrompt, MessageBoxIcon.None, MessageBoxButtons.OK, null);

                            TFTVLogger.Always("Added ability " + abilityToAdd.ViewElementDef.DisplayName1.LocalizeEnglish());

                            if (CharactersDeliriumPerksAndMissions == null)
                            {
                                CharactersDeliriumPerksAndMissions = new Dictionary<int, int>();
                            }

                            if (CharactersDeliriumPerksAndMissions.ContainsKey(__instance.Id))
                            {
                                CharactersDeliriumPerksAndMissions[__instance.Id] = 0;
                            }
                            else
                            {
                                CharactersDeliriumPerksAndMissions.Add(__instance.Id, 0);

                            }
                        }
                    }
                    else if (num > 50 && num <= 125)
                    {
                        TFTVCommonMethods.SetStaminaToZero(__instance);
                        string messagePromptNoPerk = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_PERK_PROMPT_NO_PERK" }.Localize();

                        GameUtl.GetMessageBox().ShowSimplePrompt($"{__instance.GetName()} {messagePromptNoPerk}", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoMission), "ApplyMissionResults")]
        public static class GeoMission_ManageGear_RollToRemoveDeliriumPerks_patch
        {
            public static void Postfix(GeoMission __instance, TacMissionResult result, GeoSquad squad)
            {
                try
                {
                    // TFTVLogger.Always($" {result.FactionResults.Find(r => r.FactionDef == Shared.PhoenixFactionDef).State}");

                    if (result.FactionResults.Find(r => r.FactionDef == Shared.PhoenixFactionDef).State == PhoenixPoint.Tactical.Levels.TacFactionState.Won)
                    {
                        //  TFTVLogger.Always($" passed the if");
                        RemoveDeliriumPerks(__instance.Site.GeoLevel, squad);
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static void RemoveDeliriumPerks(GeoLevelController controller, GeoSquad squad)
        {
            try
            {
                // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.CurrentDifficultyLevel.Order);

                if (CharactersDeliriumPerksAndMissions != null) //add difficulty/config option check
                {
                    // TFTVLogger.Always($"");

                    foreach (GeoCharacter geoCharacter in squad.Soldiers)
                    {
                        if (CharactersDeliriumPerksAndMissions.ContainsKey(geoCharacter.Id))
                        {
                            CharactersDeliriumPerksAndMissions[geoCharacter.Id] += 1;

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int sides = 100 + (10 * difficultyLevel) - (10 * CharactersDeliriumPerksAndMissions[geoCharacter.Id]);

                            if (sides <= 0)
                            {
                                sides = 1;

                            }

                            int num = UnityEngine.Random.Range(0, sides);

                            TFTVLogger.Always($"{geoCharacter.DisplayName} with {CharactersDeliriumPerksAndMissions[geoCharacter.Id]} missions rolls {num} on a {sides} sided dice to get rid of Delirium perks");

                            if (num <= 1)
                            {

                                foreach (TacticalAbilityDef tacticalAbilityDef in geoCharacter.GetTacticalAbilities().Where(ta => DeliriumPerks.Contains(ta)))
                                {
                                    List<TacticalAbilityDef> abilities = Traverse.Create(geoCharacter.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();
                                    TFTVLogger.Always($"removing {tacticalAbilityDef.name} from {geoCharacter.DisplayName}");

                                    abilities.Remove(tacticalAbilityDef); // Example modification

                                    if (tacticalAbilityDef == wolverineDef)
                                    {
                                        TFTVLogger.Always($"Adding cured wolverine ability");
                                        abilities.Add(wolverineCuredDef);


                                    }
                                }

                                CharactersDeliriumPerksAndMissions[geoCharacter.Id] = -1;
                            }                           
                        }
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(EditUnitButtonsController), "SetEditUnitButtonsBasedOnType")]
        internal static class TFTV_EditUnitButtonsController_SetEditUnitButtonsBasedOnType_DeliriumPerksCured_patch
        {
            public static void Postfix(UIModuleActorCycle ____parentModule)
            {
                try
                {
                    if (CharactersDeliriumPerksAndMissions != null && ____parentModule.CurrentCharacter != null && ____parentModule.CurrentCharacter.Id != null && CharactersDeliriumPerksAndMissions.ContainsKey(____parentModule.CurrentCharacter.Id) &&
                        CharactersDeliriumPerksAndMissions[____parentModule.CurrentCharacter.Id] == -1)
                    {
                        string messagePrompt = new LocalizedTextBind() { LocalizationKey = "KEY_DELIRIUM_PERK_RECOVERY" }.Localize();

                        GameUtl.GetMessageBox().ShowSimplePrompt($"{____parentModule.CurrentCharacter.GetName()} {messagePrompt}", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                        CharactersDeliriumPerksAndMissions.Remove(____parentModule.CurrentCharacter.Id);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //When getting an augment, each augment reduces corruption by a 1/3
        //And thanks to Mergele also Removes wolverine on installing a bionic torso
        [HarmonyPatch(typeof(UIModuleBionics), "OnAugmentApplied")]
        public static class UIModuleBionics_OnAugmentApplied_SetStaminaTo0_patch
        {
            public static void Postfix(UIModuleBionics __instance, ItemDef augment)
            {
                try
                {
                    if (__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33 >= 0)
                    {
                        __instance.CurrentCharacter.CharacterStats.Corruption.Set((float)(__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33));
                    }
                    else
                    {
                        __instance.CurrentCharacter.CharacterStats.Corruption.Set(0);
                    }

                    if (augment.RequiredSlotBinds[0].RequiredSlot == DefCache.GetDef<ItemSlotDef>("Human_Torso_SlotDef"))
                    {

                        List<TacticalAbilityDef> abilities = Traverse.Create(__instance.CurrentCharacter.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();

                        if (abilities.Contains(wolverineCuredDef))
                        {
                            abilities.Remove(wolverineCuredDef);

                        }
                        else if (abilities.Contains(wolverineDef))
                        {
                            abilities.Remove(wolverineDef);
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
