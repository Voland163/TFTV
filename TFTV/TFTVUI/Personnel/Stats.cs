using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class Stats
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly HashSet<TacticalItemDef> AksuArmorPieces = new HashSet<TacticalItemDef>
    {
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_BodyPartDef"),
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_BodyPartDef"),
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_ItemDef"),
    };

        internal struct DrillBonuses
        {
            public float AksuSprintSpeedBonus;
            public bool HasAksuSprintBonus => !Mathf.Approximately(AksuSprintSpeedBonus, 0f);
        }

        internal static DrillBonuses CalculateDrillBonuses(GeoCharacter character)
        {
            DrillBonuses bonuses = default;

            if (character == null)
            {
                return bonuses;
            }


            bool hasAksuSprint = false;

            if (character.Progression != null && character.Progression.Abilities.Any(a => a == TFTVDrills.DrillsDefs._aksuSprint))
            {
                hasAksuSprint = true;
            }

            if (!hasAksuSprint)
            {
                return bonuses;
            }

            int aksuArmorPiecesEquipped = 0;
            float aksuArmorSpeedBonus = 0f;

            foreach (ICommonItem armorItem in character.ArmourItems)
            {
                TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                if (tacticalItemDef == null)
                {
                    continue;
                }

                BodyPartAspectDef bodyPartAspectDef = tacticalItemDef.BodyPartAspectDef;
                if (bodyPartAspectDef == null)
                {
                    continue;
                }

                if (!tacticalItemDef.Tags.Contains(Shared.SharedGameTags.ArmorTag))
                {
                    continue;
                }

                if (hasAksuSprint && AksuArmorPieces.Contains(tacticalItemDef))
                {
                    aksuArmorPiecesEquipped++;
                    if (bodyPartAspectDef.Speed > 0f)
                    {
                        aksuArmorSpeedBonus += bodyPartAspectDef.Speed;
                    }
                }
            }


            if (hasAksuSprint && aksuArmorPiecesEquipped >= 3 && aksuArmorSpeedBonus > 0f)
            {
                bonuses.AksuSprintSpeedBonus = aksuArmorSpeedBonus;
            }

            return bonuses;
        }


        //This changes display of Delirium bar in personnel edit screen to show current Delirium value vs max delirium value the character can have
        // taking into account ODI level and bionics
        [HarmonyPatch(typeof(UIModuleCharacterProgression), nameof(UIModuleCharacterProgression.SetStatusesPanel))]
        internal static class BG_UIModuleCharacterProgression_SetStatusesPanel_patch
        {

            public static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
            {
                try
                {

                    if (____character.CharacterStats.Corruption > 0f && ____character.Fatigue == null)
                    {
                        TFTVLogger.Always($"{____character.DisplayName} had Delirium, but has no Stamina! Setting Delirium to 0");
                        ____character.CharacterStats.Corruption.Set(0f);
                    }


                    if (____character.CharacterStats.Corruption > 0f)
                    {
                        //____character.CharacterStats.Corruption.Set(Mathf.RoundToInt(____character.CharacterStats.Corruption));

                        float delirium = ____character.CharacterStats.Corruption;
                        if (TFTVDelirium.CalculateMaxCorruption(____character) < ____character.CharacterStats.Corruption)
                        {
                            delirium = (TFTVDelirium.CalculateMaxCorruption(____character));
                        }

                        __instance.CorruptionSlider.minValue = 0f;
                        __instance.CorruptionSlider.maxValue = Mathf.RoundToInt(TFTVDelirium.CalculateMaxCorruption(____character));
                        __instance.CorruptionSlider.value = delirium;

                        UITooltipText corruptionSliderTip = __instance.CorruptionSlider.gameObject.AddComponent<UITooltipText>();
                        corruptionSliderTip.TipText = $"{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DELIRIUM_EXPLANATION")} {TFTVDelirium.CurrentDeliriumLevel(____character.Faction.GeoLevel)}.";
                        __instance.CorruptionStatText.text = $"{Mathf.RoundToInt(delirium)}/{Mathf.RoundToInt(__instance.CorruptionSlider.maxValue)}";

                        int num = (int)(float)____character.Fatigue.Stamina;
                        int num2 = (int)(float)____character.Fatigue.Stamina.Max;
                        __instance.StaminaSlider.minValue = 0f;
                        __instance.StaminaSlider.maxValue = num2;
                        __instance.StaminaSlider.value = num;

                        //  UITooltipText staminaTextTip = new UITooltipText();
                        if (__instance.StaminaStatText.gameObject.GetComponent<UITooltipText>() == null)
                        {

                            __instance.StaminaStatText.gameObject.AddComponent<UITooltipText>();

                        }

                        __instance.StaminaStatText.gameObject.AddComponent<UITooltipText>();
                        if (num != num2)
                        {
                            string deliriumReducedStamina = "";
                            for (int i = 0; i < TFTVDelirium.CalculateStaminaEffectOnDelirium(____character); i++)
                            {
                                deliriumReducedStamina += "-";

                            }
                            __instance.StaminaStatText.text = $"<color=#da5be3>{deliriumReducedStamina}</color>" + num + "/" + num2;
                        }
                        else
                        {
                            __instance.StaminaStatText.text = "<color=#da5be3> ---- </color>" + num.ToString();

                        }

                        LocalizedTextBind localizedTextBind = new LocalizedTextBind("DELIRIUM_STAMINA_TOOLTIP", false);
                        string tipText = $"{localizedTextBind.Localize()} {TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}";


                        __instance.StaminaStatText.gameObject.GetComponent<UITooltipText>().TipKey = new LocalizedTextBind(tipText, true);


                        //   staminaTextTip.TipText = $"Character's current Stamina is reducing the effects of Delirium on Willpower by {TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}";

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Patch to show correct stats in Personnel Edit screen
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "GetStarBarValuesDisplayString")] //VERIFIED
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class TFTV_UIModuleCharacterProgression_RefreshStatPanel_patch
        {
            private static void Postfix(GeoCharacter ____character, ref string __result, CharacterBaseAttribute attribute, int currentAttributeValue, UIModuleCharacterProgression __instance)
            {
                try
                {
                    ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
                    float bonusSpeed = 0;
                    float bonusWillpower = 0;
                    float bonusStrength = 0;

                    foreach (ICommonItem armorItem in ____character.ArmourItems)
                    {
                        TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                        if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                        {
                            bonusSpeed += tacticalItemDef.BodyPartAspectDef.Speed;
                            bonusWillpower += tacticalItemDef.BodyPartAspectDef.WillPower;
                            bonusStrength += tacticalItemDef.BodyPartAspectDef.Endurance;
                        }
                    }

                    if (____character.Progression != null)
                    {
                        foreach (TacticalAbilityDef ability in ____character.Progression.Abilities)
                        {
                            PassiveModifierAbilityDef passiveModifierAbilityDef = ability as PassiveModifierAbilityDef;
                            if (!(passiveModifierAbilityDef == null))
                            {
                                ItemStatModification[] statModifications = passiveModifierAbilityDef.StatModifications;
                                foreach (ItemStatModification statModifier in statModifications)
                                {
                                    if (statModifier.TargetStat == StatModificationTarget.Endurance && statModifier.Modification == StatModificationType.AddMax)
                                    {
                                        bonusStrength += statModifier.Value;
                                    }
                                    else if (statModifier.TargetStat == StatModificationTarget.Willpower && statModifier.Modification == StatModificationType.AddMax)
                                    {
                                        bonusWillpower += statModifier.Value;
                                    }
                                    else if (statModifier.TargetStat == StatModificationTarget.Speed)
                                    {
                                        bonusSpeed += statModifier.Value;
                                    }
                                }
                            }

                            if (ability == derealization)
                            {
                                bonusStrength -= 5;
                            }
                        }

                        foreach (PassiveModifierAbilityDef passiveModifier in ____character.PassiveModifiers)
                        {
                            ItemStatModification[] statModifications = passiveModifier.StatModifications;
                            foreach (ItemStatModification statModifier2 in statModifications)
                            {
                                if (statModifier2.TargetStat == StatModificationTarget.Endurance)
                                {
                                    bonusStrength += statModifier2.Value;
                                }
                                else if (statModifier2.TargetStat == StatModificationTarget.Willpower)
                                {
                                    bonusWillpower += statModifier2.Value;
                                }
                                else if (statModifier2.TargetStat == StatModificationTarget.Speed)
                                {
                                    bonusSpeed += statModifier2.Value;
                                }
                            }
                        }
                    }

                    DrillBonuses drillBonuses = CalculateDrillBonuses(____character);

                    if (drillBonuses.HasAksuSprintBonus)
                    {
                        bonusSpeed += drillBonuses.AksuSprintSpeedBonus;
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Strength))
                    {
                        if (bonusStrength > 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#50c878>{currentAttributeValue + bonusStrength}</color>)";
                        }
                        else if (bonusStrength < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#cc0000>{currentAttributeValue + bonusStrength}</color>)";
                        }
                        else
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
                        }
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Speed))
                    {
                        if (bonusSpeed > 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#50c878>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else if (bonusSpeed < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#cc0000>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
                        }
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Will))
                    {
                        if (____character.CharacterStats.Corruption > TFTVDelirium.CalculateStaminaEffectOnDelirium(____character) && TFTVVoidOmens.VoidOmensCheck[3] == false)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}" +
                                $"<color=#da5be3> ({currentAttributeValue + bonusWillpower - ____character.CharacterStats.Corruption.Value + TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}</color>)";
                        }
                        else
                        {
                            if (bonusWillpower > 0)
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#50c878>{currentAttributeValue + bonusWillpower}</color>)";
                            }
                            else if (bonusWillpower < 0)
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)} (<color=#cc0000>{currentAttributeValue + bonusWillpower}</color>)";
                            }
                            else
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
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

        [HarmonyPatch(typeof(GeoRosterStatisticsController))]
        internal static class GeoRosterStatisticsControllerInitPatch
        {
            private static readonly GameTagDef ArmorTag = Shared.SharedGameTags.ArmorTag;
            private static readonly GameTagDef HeavyClassTag = DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef");
            private static readonly GameTagDef BionicTag = DefCache.GetDef<GameTagDef>("Bionic_TagDef");

            [HarmonyPostfix]
            [HarmonyPatch(nameof(GeoRosterStatisticsController.Init))]
            private static void Postfix(GeoRosterStatisticsController __instance, UnitStatisticsData data, bool hasServiceRecord)
            {
                if (data == null)
                {
                    return;
                }

                GeoCharacter geoCharacter = GameUtl.CurrentLevel().GetComponent<GeoLevelController>()?.View.GeoscapeModules.ActorCycleModule.CurrentCharacter;

                float maxCorruption = TFTVDelirium.CalculateMaxCorruption(geoCharacter);
                float delirium = geoCharacter.CharacterStats.Corruption;
                if (maxCorruption < delirium)
                {
                    delirium = maxCorruption;
                }

                __instance.CorruptionProgressBar.minValue = 0f;
                __instance.CorruptionProgressBar.maxValue = Mathf.RoundToInt(maxCorruption);
                __instance.CorruptionProgressBar.value = delirium;

                UITooltipText corruptionSliderTip = __instance.CorruptionProgressBar.gameObject.GetComponent<UITooltipText>() ?? __instance.CorruptionProgressBar.gameObject.AddComponent<UITooltipText>();
                corruptionSliderTip.TipText = $"{TFTVCommonMethods.ConvertKeyToString("KEY_UI_DELIRIUM_EXPLANATION")} {TFTVDelirium.CurrentDeliriumLevel(geoCharacter.Faction.GeoLevel)}.";
                __instance.CorruptionText.text = $"{Mathf.RoundToInt(delirium)}/{Mathf.RoundToInt(__instance.CorruptionProgressBar.maxValue)}";


                if (!TFTVNewGameOptions.IsReworkEnabled())
                {
                    return;
                }

                if (!data.Abilities.Any(ad => ad.Ability == TFTVDrills.DrillsDefs._aksuSprint)) return;

                DrillBonuses drillBonuses = CalculateDrillBonuses(geoCharacter);
                if (drillBonuses.HasAksuSprintBonus)
                {
                    __instance.SpeedText.text = $"{data.Speed + Mathf.RoundToInt(drillBonuses.AksuSprintSpeedBonus)}";
                }


                /*if (!data.Abilities.Any(ad => ad.Ability == TFTVDrills.DrillsDefs._heavyConditioning)) return;

                 if (!geoCharacter.ArmourItems.All(i => i.ItemDef.Tags.Contains(ArmorTag) && i.ItemDef.Tags.Contains(HeavyClassTag) && !i.ItemDef.Tags.Contains(BionicTag)))
                     return;

                 float fPerception = 0f;
                 float fAccuracy = 0f;
                 float fStealth = 0f;

                 foreach (GeoItem item in geoCharacter.ArmourItems)
                 {
                     if (item.ItemDef is TacticalItemDef tacticalItemDef && tacticalItemDef.BodyPartAspectDef != null)
                     {
                         if (tacticalItemDef.BodyPartAspectDef.Perception < 0)
                         {
                             fPerception += tacticalItemDef.BodyPartAspectDef.Perception;
                         }
                         if (tacticalItemDef.BodyPartAspectDef.Accuracy < 0)
                         {
                             fAccuracy += tacticalItemDef.BodyPartAspectDef.Accuracy;
                         }
                         if (tacticalItemDef.BodyPartAspectDef.Stealth < 0)
                         {
                             fStealth += tacticalItemDef.BodyPartAspectDef.Stealth;
                         }
                     }
                 }

                 fPerception *= 100;
                 fAccuracy *= 100;
                 fStealth *= 100;


                 //  PerceptionText.text = data.Perception.ToString();
                 //  AccuracyText.text = $"{data.Accuracy}%";
                 //  StealthText.text = $"{data.Stealth}%";


                 if (fPerception < 0)
                 {
                     __instance.PerceptionText.text = $"{Mathf.RoundToInt(data.Perception - fPerception + fPerception / 2)}";
                 }


                 if (fAccuracy < 0)
                 {
                     __instance.AccuracyText.text = $"{Mathf.RoundToInt(data.Accuracy - fAccuracy + fAccuracy / 2)}% ";
                 }


                 if (fStealth < 0)
                 {
                     __instance.StealthText.text = $"{Mathf.RoundToInt(data.Stealth - fStealth + fStealth / 2)}%";
                 }*/
            }
        }



        [HarmonyPatch(typeof(UIModuleSoldierEquip), "GetPrimaryWeight")] //VERIFIED
        internal static class TFTV_UIModuleSoldierEquip_GetPrimaryWeight_Patch
        {

            private static void Postfix(UIModuleSoldierEquip __instance, ref int __result)
            {
                try
                {
                    if (GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                    {
                        UIModuleCharacterProgression uIModuleCharacterProgression = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.CharacterProgressionModule;

                        FieldInfo characterField = typeof(UIModuleCharacterProgression).GetField("_character", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (characterField.GetValue(uIModuleCharacterProgression) is GeoCharacter geoCharacter && !__instance.IsVehicle && !geoCharacter.TemplateDef.IsMutog && uIModuleCharacterProgression != null)
                        {


                            int weightOfAugmentations = 0;

                            foreach (GeoItem armorpiece in geoCharacter.ArmourItems)
                            {
                                if (armorpiece.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag) || armorpiece.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag))
                                {
                                    weightOfAugmentations += armorpiece.ItemDef.Weight;

                                }
                            }

                            __result -= weightOfAugmentations;

                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Patch to show correct encumbrance
        [HarmonyPatch(typeof(UIModuleSoldierEquip), "RefreshWeightSlider")] //VERIFIED
        internal static class TFTV_UIModuleSoldierEquip_RefreshWeightSlider_Patch
        {
            private static readonly ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
            private static void Prefix(ref int maxWeight, UIModuleSoldierEquip __instance)
            {
                try

                {
                    if (GameUtl.CurrentLevel().GetComponent<GeoLevelController>() != null)
                    {

                        //  UIModuleSoldierEquipKludge = __instance;
                        //GeoCharacter geoCharacter = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;
                        UIModuleCharacterProgression uIModuleCharacterProgression = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.CharacterProgressionModule;

                        FieldInfo characterField = typeof(UIModuleCharacterProgression).GetField("_character", BindingFlags.NonPublic | BindingFlags.Instance);



                        if (characterField != null && uIModuleCharacterProgression != null && characterField.GetValue(uIModuleCharacterProgression) is GeoCharacter geoCharacter && !__instance.IsVehicle && !geoCharacter.TemplateDef.IsMutog)
                        {

                            float bonusStrength = 0;
                            float bonusToCarry = 1;

                            foreach (ICommonItem armorItem in geoCharacter.ArmourItems)
                            {
                                TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                                if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                                {
                                    bonusStrength += tacticalItemDef.BodyPartAspectDef.Endurance;
                                }
                            }

                            if (geoCharacter.Progression != null)
                            {
                                foreach (TacticalAbilityDef ability in geoCharacter.Progression.Abilities)
                                {
                                    PassiveModifierAbilityDef passiveModifierAbilityDef = ability as PassiveModifierAbilityDef;
                                    if (!(passiveModifierAbilityDef == null))
                                    {
                                        ItemStatModification[] statModifications = passiveModifierAbilityDef.StatModifications;
                                        foreach (ItemStatModification statModifier in statModifications)
                                        {
                                            if (statModifier.TargetStat == StatModificationTarget.Endurance && statModifier.Modification == StatModificationType.AddMax)
                                            {
                                                bonusStrength += statModifier.Value;
                                                // TFTVLogger.Always("The TacticalAbilityDef is " + ability.name + ". It modifies Endurance, giving " + statModifier.Value + ", " +
                                                //    "making the total bonus to Strength " + bonusStrength);
                                            }


                                            if (statModifier.TargetStat == StatModificationTarget.CarryWeight && statModifier.Modification == StatModificationType.MultiplyMax)
                                            {
                                                bonusToCarry += statModifier.Value - 1;
                                            }
                                        }
                                    }

                                    if (ability == derealization)
                                    {
                                        bonusStrength -= 5;

                                    }
                                }

                                foreach (PassiveModifierAbilityDef passiveModifier in geoCharacter.PassiveModifiers)
                                {
                                    ItemStatModification[] statModifications = passiveModifier.StatModifications;
                                    foreach (ItemStatModification statModifier2 in statModifications)
                                    {
                                        if (statModifier2.TargetStat == StatModificationTarget.Endurance)
                                        {
                                            bonusStrength += statModifier2.Value;
                                        }
                                        if (statModifier2.TargetStat == StatModificationTarget.CarryWeight)
                                        {
                                            bonusToCarry += statModifier2.Value;
                                        }

                                    }
                                }

                            }



                            maxWeight += (int)(bonusStrength * bonusToCarry);



                            uIModuleCharacterProgression?.StatChanged();//   hookToProgressionModule.StatChanged();
                                                                        //   hookToProgressionModule.RefreshStats();


                            //hookToProgressionModule.SetStatusesPanel();

                            uIModuleCharacterProgression?.RefreshStatPanel();//  hookToProgressionModule.RefreshStatPanel();
                                                                             //TFTVLogger.Always("Max weight is " + maxWeight + ". Bonus Strength is " + bonusStrength + ". Bonus to carry is " + bonusToCarry);
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        //Patch to keep characters animating in edit screen despite constant stat updates invoked by the other patches
        [HarmonyPatch(typeof(UIStateEditSoldier), "RequestRefreshCharacterData")] //VERIFIED
        internal static class TFTV_UIStateEditSoldier_RequestRefreshCharacterData_Patch
        {

            private static void Postfix(ref bool ____uiCharacterAnimationResetNeeded)
            {
                try
                {

                    ____uiCharacterAnimationResetNeeded = false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}