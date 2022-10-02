
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Saves;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVUI
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        //This method changes how WP are displayed in the Edit personnel screen, to show effects of Delirium on WP


        public static UIModuleCharacterProgression hookToProgressionModule = null;
        public static GeoCharacter hookToCharacter = null;

        
        

        [HarmonyPatch(typeof(UIModuleCharacterProgression), "GetStarBarValuesDisplayString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleCharacterProgression_RefreshStatPanel_patch
        {
            private static readonly ApplyStatusAbilityDef derealization = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("DerealizationIgnorePain_AbilityDef"));
            private static void Postfix(GeoCharacter ____character, ref string __result, CharacterBaseAttribute attribute, int currentAttributeValue)
            {
                try
                {
                    float bonusSpeed = 0;
                    float bonusWillpower = 0;
                    float bonusStrength = 0;

                    //  GeoLevelController level = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));

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

                    if (attribute.Equals(CharacterBaseAttribute.Strength))
                    {
                        if (bonusStrength > 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#50c878>{currentAttributeValue + bonusStrength}</color>)";
                        }
                        else if (bonusStrength < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#cc0000>{currentAttributeValue + bonusStrength}</color>)";
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
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#50c878>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else if (bonusSpeed < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#cc0000>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
                        }
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Will))
                    {
                        if (____character.CharacterStats.Corruption > TFTVDelirium.CalculateStaminaEffectOnDelirium(____character) && TFTVVoidOmens.voidOmensCheck[3] == false)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}" +
                                $"<color=#da5be3> ({currentAttributeValue + bonusWillpower - ____character.CharacterStats.Corruption.Value + TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}</color>)";
                        }
                        else
                        {
                            if (bonusWillpower > 0)
                            {

                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                            $" (<color=#50c878>{currentAttributeValue + bonusWillpower}</color>)";

                            }
                            else if (bonusWillpower < 0)
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                        $" (<color=#cc0000>{currentAttributeValue + bonusWillpower}</color>)";
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
        
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "Awake")]

        internal static class UIModuleCharacterProgression_Awake_patch

        {
            public static void Postfix(UIModuleCharacterProgression __instance)
            {
                try
                {
                    hookToProgressionModule = __instance;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(UIModuleSoldierEquip), "RefreshWeightSlider")]
        internal static class UIModuleSoldierEquip_RefreshWeightSlider_Patch
        {
           private static readonly ApplyStatusAbilityDef derealization = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("DerealizationIgnorePain_AbilityDef"));
            private static void Prefix(ref int maxWeight, UIModuleSoldierEquip __instance)
            {
                try
                {
                 

                    if (hookToCharacter != null && !__instance.IsVehicle && !hookToCharacter.TemplateDef.IsMutog)
                    {

                        float bonusStrength = 0;
                        float bonusToCarry = 1;

                        foreach (ICommonItem armorItem in hookToCharacter.ArmourItems)
                        {
                            TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                            if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                            {
                                bonusStrength += tacticalItemDef.BodyPartAspectDef.Endurance;
                            }
                        }

                      if (hookToCharacter.Progression != null)
                        {
                            foreach (TacticalAbilityDef ability in hookToCharacter.Progression.Abilities)
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

                            foreach (PassiveModifierAbilityDef passiveModifier in hookToCharacter.PassiveModifiers)
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
                        hookToProgressionModule.StatChanged();
                     //   hookToProgressionModule.RefreshStats();
                        //hookToProgressionModule.SetStatusesPanel();
                       hookToProgressionModule.RefreshStatPanel();
                        //TFTVLogger.Always("Max weight is " + maxWeight + ". Bonus Strength is " + bonusStrength + ". Bonus to carry is " + bonusToCarry);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleActorCycle), "DisplaySoldier", new Type[] { typeof(GeoCharacter), typeof(bool), typeof(bool), typeof(bool) })]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleActorCycle_DisplaySoldier_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = Repo.GetAllDefs<ItemSlotDef>().FirstOrDefault(ged => ged.name.Equals("Human_Head_SlotDef"));


            private static bool Prefix(UIModuleActorCycle __instance, List<UnitDisplayData> ____units,
                CharacterClassWorldDisplay ____classWorldDisplay,
                GeoCharacter character, bool showHelmet, bool resetAnimation, bool addWeapon)
            {
                try
                { 
                    if (character.TemplateDef.IsMutog || character.TemplateDef.IsMutoid || character.TemplateDef.IsVehicle) 
                    {
                        return true;
                    }


                    if (character != null && character.TemplateDef.IsHuman && !character.IsMutoid && !character.TemplateDef.IsMutog && !character.TemplateDef.IsVehicle)
                    {
                        
                        bool hasAugmentedHead = false;

                       foreach (GeoItem bionic in character.ArmourItems)
                        {
                           
                            
                            if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag)) 
                                && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot==headSlot)
                            {
                                hasAugmentedHead = true;
                                
                            }
                        }

                        if (!hasAugmentedHead)
                        {
                            UnitDisplayData unitDisplayData = ____units.FirstOrDefault((UnitDisplayData u) => u.BaseObject == character);
                            if (unitDisplayData == null)
                            {
                                return true;
                            }


                            ____classWorldDisplay.SetDisplay(character.GetClassViewElementDefs(), (float)character.CharacterStats.Corruption > 0f);

                            __instance.DisplaySoldier(unitDisplayData, resetAnimation, addWeapon, showHelmet = false);
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
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
                    hookToCharacter = ____character;

                    if (____character.CharacterStats.Corruption > 0f)

                    {
                        float delirium = ____character.CharacterStats.Corruption.IntValue;
                        if(TFTVDelirium.CalculateMaxCorruption(____character) < ____character.CharacterStats.Corruption.IntValue) 
                        {
                            delirium = (TFTVDelirium.CalculateMaxCorruption(____character));
                        }

                        __instance.CorruptionSlider.minValue = 0f;
                        __instance.CorruptionSlider.maxValue = TFTVDelirium.CalculateMaxCorruption(____character);
                        __instance.CorruptionSlider.value = delirium;
                        __instance.CorruptionStatText.text = $"{delirium}/{Mathf.RoundToInt(__instance.CorruptionSlider.maxValue)}";

                        int num = (int)(float)____character.Fatigue.Stamina;
                        int num2 = (int)(float)____character.Fatigue.Stamina.Max;
                        __instance.StaminaSlider.minValue = 0f;
                        __instance.StaminaSlider.maxValue = num2;
                        __instance.StaminaSlider.value = num;
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
