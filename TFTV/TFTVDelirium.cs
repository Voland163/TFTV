using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDelirium
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        // Patch to increase damage by 2% per mutated body part per Delirium point

        public static void ApplyChanges()
        {
            try
            {
                CorruptionStatusDef corruption_StatusDef = Repo.GetAllDefs<CorruptionStatusDef>().FirstOrDefault(ged => ged.name.Equals("Corruption_StatusDef"));
                corruption_StatusDef.Multiplier = 0.0f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        [HarmonyPatch(typeof(CorruptionStatus), "GetMultiplier")]
        internal static class BG_CorruptionStatus_GetMultiplier_Mutations_patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref float __result, CorruptionStatus __instance)
            {
                try
                {
                    float numberOfMutations = 0;
                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);


                    foreach (TacticalItem armourItem in base_TacticalActor.BodyState.GetArmourItems())
                    {
                        if (armourItem.GameTags.Contains(bionicalTag))
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
                int odiPerc = currentODIlevel * 100 / TFTVSDIandVoidOmenRoll.ODI_EventIDs.Length;

                GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                foreach (GeoItem bionic in character.ArmourItems)
                {
                    if (bionic.ItemDef.Tags.Contains(bionicalTag))

                        bionics += 1;
                }

                if (!TFTVVoidOmens.VoidOmen10Active)
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
                        if (odiPerc < 50)
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
                if (TFTVVoidOmens.VoidOmen10Active)
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
            catch (System.Exception e)
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
                    /* string stamina40 = "<color=#18f005>-4 to Delirium effect(WP loss)</color>";
                     string stamina30to39 = "<color=#c1f005>-3 to Delirium effect(WP loss)</color>";
                     string stamina20to29 = "<color=#f0e805>-2 to Delirium effect(WP loss)</color>";
                     string stamina10to19 = "<color=##f07b05>-1 to Delirium effect(WP loss)</color>";
                     string stamina0to9= "<color=#f00505>Delirium has full effect</color>";*/

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
                catch (System.Exception e)
                {
                    TFTVLogger.Error(e);
                }

                throw new InvalidOperationException();
            }
        }

        //This method assigns Delirium Perks to operatives who rolled them when treating Delirium
        public static void DeliriumPerksOnTactical(TacticalLevelController level)
        {
            DefRepository Repo = GameUtl.GameComponent<DefRepository>();
            try
            {
                foreach (TacticalFaction faction in level.Factions)
                {
                    if (faction.IsViewerFaction)
                    {
                        foreach (TacticalActor actor in faction.TacticalActors)
                        {
                            TacticalAbilityDef abilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("AngerIssues_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef) != null)
                            {
                                actor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Frenzy_StatusDef")));
                                TFTVLogger.Always(actor.name + " with " + abilityDef.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());
                            }

                            TacticalAbilityDef abilityDef1 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Hallucinating_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef1) != null)
                            {
                                actor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Hallucinating_StatusDef")));
                                TFTVLogger.Always(actor.name + " with " + abilityDef1.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());
                            }

                            TacticalAbilityDef abilityDef2 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("FleshEater_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef2) != null)
                            {
                                actor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("FleshEaterHP_AbilityDef")), actor);
                                TFTVLogger.Always(actor.name + " with " + abilityDef2.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());
                            }
                            TacticalAbilityDef abilityDef3 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("OneOfUsPassive_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef3) != null)
                            {
                                actor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("MistResistance_StatusDef")));
                                actor.GameTags.Add(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(sd => sd.name.Equals("Takashi_GameTagDef")), GameTagAddMode.ReplaceExistingExclusive);
                                TFTVLogger.Always(actor.name + " with " + abilityDef3.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());
                            }

                            TacticalAbilityDef abilityDef5 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Nails_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef5) != null)
                            {
                                actor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("NailsPassive_AbilityDef")), actor);
                                TFTVLogger.Always(actor.name + " with " + abilityDef5.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());
                            }

                            TacticalAbilityDef abilityDef7 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Immortality_AbilityDef"));
                            if (actor.GetAbilityWithDef<Ability>(abilityDef7) != null)
                            {
                                actor.Status.ApplyStatus(Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("ArmorBuffStatus_StatusDef")));
                                TFTVLogger.Always(actor.name + " with " + abilityDef7.name + " has the following statuses: " + actor.Status.CurrentStatuses.ToString());

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

        
        //This method changes how WP are displayed in the Edit personnel screen, to show effects of Delirium on WP

        [HarmonyPatch(typeof(UIModuleCharacterProgression), "GetStarBarValuesDisplayString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleCharacterProgression_RefreshStatPanel_patch
        {

            private static void Postfix(GeoCharacter ____character, ref string __result, CharacterBaseAttribute attribute, int currentAttributeValue)
            {
                try
                {
                    if (____character.CharacterStats.Corruption > CalculateStaminaEffectOnDelirium(____character) && attribute.Equals(CharacterBaseAttribute.Will))
                    {
                        __result = $"<color=#da5be3>{currentAttributeValue - ____character.CharacterStats.Corruption.Value + CalculateStaminaEffectOnDelirium(____character)}</color>" + $"({currentAttributeValue}) / " +
                        $"{____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}";
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //This changes display of Delirium bar in personnel edit screen to show current Delirium value vs max delirium value the character can have
        // taking into account ODI level and bionics
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "SetStatusesPanel")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleCharacterProgression_SetStatusesPanel_patch
        {

            private static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
            {
                try
                {
                    if (____character.CharacterStats.Corruption > 0f)

                    {
                        __instance.CorruptionSlider.minValue = 0f;
                        __instance.CorruptionSlider.maxValue = CalculateMaxCorruption(____character);
                        __instance.CorruptionSlider.value = ____character.CharacterStats.Corruption.IntValue;
                        __instance.CorruptionStatText.text = $"{____character.CharacterStats.Corruption.IntValue}/{Mathf.RoundToInt(__instance.CorruptionSlider.maxValue)}";

                        int num = (int)(float)____character.Fatigue.Stamina;
                        int num2 = (int)(float)____character.Fatigue.Stamina.Max;
                        __instance.StaminaSlider.minValue = 0f;
                        __instance.StaminaSlider.maxValue = num2;
                        __instance.StaminaSlider.value = num;
                        if (num != num2)
                        {
                            string deliriumReducedStamina = "";
                            for (int i = 0; i < CalculateStaminaEffectOnDelirium(____character); i++)
                            {
                                deliriumReducedStamina += "-";

                            }
                            __instance.StaminaStatText.text = $"<color=#da5be3>{deliriumReducedStamina}</color>" + num + "/" + num2;
                        }
                        else
                        {
                            __instance.StaminaStatText.text = "<color=#da5be3> ---- </color>" + num.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
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
                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;

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
                    if (!TFTVVoidOmens.VoidOmen10Active)
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
                            if (odiPerc < 50)
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
                            else // > 75%
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
                    if (TFTVVoidOmens.VoidOmen10Active)
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
                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);

                    // Get characters geoscape stamina by his actor ID

                    int stamina = 40;
                    if (StaminaMap.ContainsKey(base_TacticalActor.GeoUnitId))
                    {
                        stamina = StaminaMap[base_TacticalActor.GeoUnitId];
                    }

                    // Calculate WP reduction dependent on stamina
                    float wpReduction = base_TacticalActor.CharacterStats.Corruption;

                    if (TFTVVoidOmens.VoidOmen3Active)
                    {
                        wpReduction = 0;
                    }
                    else
                    {
                        wpReduction = base_TacticalActor.CharacterStats.Corruption; // stamina between 0 and 10

                        if (stamina == 40)
                        {
                            wpReduction = base_TacticalActor.CharacterStats.Corruption - 4;
                        }
                        else if (stamina >= 30 && stamina < 40)
                        {
                            wpReduction = base_TacticalActor.CharacterStats.Corruption - 3;
                        }
                        else if (stamina >= 20 && stamina < 30)
                        {
                            wpReduction = base_TacticalActor.CharacterStats.Corruption - 2;
                        }
                        else if (stamina >= 10 && stamina < 20)
                        {
                            wpReduction = base_TacticalActor.CharacterStats.Corruption - 1;
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
            public static void Postfix(GeoCharacter __instance)
            {

                try
                {

                    PassiveModifierAbilityDef shutEye_Ability = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("ShutEye_AbilityDef"));

                    PassiveModifierAbilityDef hallucinating_AbilityDef = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Hallucinating_AbilityDef"));

                    PassiveModifierAbilityDef solipsism_AbilityDef = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Solipsism_AbilityDef"));

                    PassiveModifierAbilityDef angerIssues_AbilityDef = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("AngerIssues_AbilityDef"));

                    PassiveModifierAbilityDef photophobia_AbilityDef = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Photophobia_AbilityDef"));

                    ApplyStatusAbilityDef nails_AbilityDef = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Nails_AbilityDef"));

                    PassiveModifierAbilityDef immortality_AbilityDef = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Immortality_AbilityDef"));

                    ApplyStatusAbilityDef feral_AbilityDef = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Feral_AbilityDef"));

                    DamageMultiplierAbilityDef oneOfUs_AbilityDef = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("OneOfUs_AbilityDef"));

                    ApplyStatusAbilityDef fleshEater_AbilityDef = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(ged => ged.name.Equals("FleshEater_AbilityDef"));


                    List<TacticalAbilityDef> abilityList = new List<TacticalAbilityDef>
                    { shutEye_Ability, hallucinating_AbilityDef, solipsism_AbilityDef, angerIssues_AbilityDef, photophobia_AbilityDef, nails_AbilityDef, immortality_AbilityDef, feral_AbilityDef,
                    oneOfUs_AbilityDef, fleshEater_AbilityDef
                    };

                    int num = UnityEngine.Random.Range(0, 200);
                    // GeoscapeTutorialStepsDef stepTest = Repo.GetAllDefs<GeoscapeTutorialStepsDef>().FirstOrDefault(ged => ged.name.Equals("GeoscapeTutorialStepsDef"));
                    // GeoscapeTutorialStep test = new GeoscapeTutorialStep();
                    // test.Title.LocalizationKey = $"test";
                    // test.Description.LocalizationKey = $"testing";
                    TFTVLogger.Always("Treatment rolled " + num);

                    if (num >= 0 && num <= 50)
                    {
                        for (int i = 0; i < 100; i++)
                        {
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

        [HarmonyPatch(typeof(TacticalAbility), "FumbleActionCheck")]
        public static class TacticalAbility_FumbleActionCheck_Patch
        {
            public static void Postfix(TacticalAbility __instance, ref bool __result)
            {
                DefRepository Repo = GameUtl.GameComponent<DefRepository>();

                try
                {
                    TacticalAbilityDef abilityDef9 = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Feral_AbilityDef"));
                    if (__instance.TacticalActor.GetAbilityWithDef<TacticalAbility>(abilityDef9) != null && __instance.Source is Equipment)
                    {
                        __result = UnityEngine.Random.Range(0, 100) < 10;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        //Dtony's Delirium perks patch
        [HarmonyPatch(typeof(RecruitsListElementController), "SetRecruitElement")]
        public static class RecruitsListElementController_SetRecruitElement_Patch
        {
            public static bool Prefix(RecruitsListElementController __instance, RecruitsListEntryData entryData, List<RowIconTextController> ____abilityIcons)
            {
                try
                {
                    if (____abilityIcons == null)
                    {
                        ____abilityIcons = new List<RowIconTextController>();
                        if (__instance.PersonalTrackRoot.transform.childCount < entryData.PersonalTrackAbilities.Count())
                        {
                            RectTransform parent = __instance.PersonalTrackRoot.GetComponent<RectTransform>();
                            RowIconTextController source = parent.GetComponentInChildren<RowIconTextController>();
                            parent.DetachChildren();
                            source.Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);
                            for (int i = 0; i < entryData.PersonalTrackAbilities.Count(); i++)
                            {
                                RowIconTextController entry = UnityEngine.Object.Instantiate(source, parent, true);
                            }
                        }
                        UIUtil.GetComponentsFromContainer(__instance.PersonalTrackRoot.transform, ____abilityIcons);
                    }
                    __instance.RecruitData = entryData;
                    __instance.RecruitName.SetSoldierData(entryData.Recruit);
                    BC_SetAbilityIcons(entryData.PersonalTrackAbilities.ToList(), ____abilityIcons);
                    if (entryData.SuppliesCost != null && __instance.CostText != null && __instance.CostColorController != null)
                    {
                        __instance.CostText.text = entryData.SuppliesCost.ByResourceType(ResourceType.Supplies).RoundedValue.ToString();
                        __instance.CostColorController.SetWarningActive(!entryData.IsAffordable, true);
                    }
                    __instance.NavHolder.RefreshNavigation();
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }


            private static void BC_SetAbilityIcons(List<TacticalAbilityViewElementDef> abilities, List<RowIconTextController> abilityIcons)
            {
                foreach (RowIconTextController rowIconTextController in abilityIcons)
                {
                    rowIconTextController.gameObject.SetActive(false);
                }
                for (int i = 0; i < abilities.Count; i++)
                {
                    abilityIcons[i].gameObject.SetActive(true);
                    abilityIcons[i].SetController(abilities[i].LargeIcon, abilities[i].DisplayName1, abilities[i].Description);
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
                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                    int numberOfBionics = AugmentScreenUtilities.GetNumberOfBionicsAugmentations(__instance.CurrentCharacter);

                    for (int i = 0; i < numberOfBionics; i++)
                    {
                        if (__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33 >= 0)
                        {
                            __instance.CurrentCharacter.CharacterStats.Corruption.Set((float)(__instance.CurrentCharacter.CharacterStats.Corruption - __instance.CurrentCharacter.CharacterStats.Willpower * 0.33));
                        }
                        else
                        {
                            __instance.CurrentCharacter.CharacterStats.Corruption.Set(0);
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
