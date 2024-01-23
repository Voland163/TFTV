using Base;
using Base.Core;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI;
using Base.UI.MessageBox;
using Base.UI.VideoPlayback;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Tactical.View.ViewControllers.SoldierResultElement;

namespace TFTV
{
    internal class TFTVUI
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static Color yellow = new Color(255, 255, 0, 1.0f);
        internal static Color dark = new Color(52, 52, 61, 1.0f);

        public static bool HelmetsOff;

        [HarmonyPatch(typeof(SoldierResultElement), "SetStatus", new Type[] { typeof(SoldierStatus), typeof(object[]) })]
        public static class SoldierResultElement_SetStatus_patch
        {

            public static void Postfix(SoldierResultElement __instance)
            {
                try
                {
                    if (TFTVStamina.charactersWithDisabledBodyParts.ContainsKey(__instance.Actor.GeoUnitId))
                    {
                        string badlyInjuredText = TFTVCommonMethods.ConvertKeyToString("KEY_BADLY_INJURED_OPERATIVE");

                        __instance.Status.text = badlyInjuredText;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIItemTooltip), "SetTacItemStats")]
        public static class UIItemTooltip_SetTacItemStats_patch
        {

            public static void Postfix(UIItemTooltip __instance, TacticalItemDef tacItemDef, bool secondObject, int subItemIndex = -1)
            {
                try
                {
                    if (tacItemDef == null)
                    {
                        return;
                    }

                    if (tacItemDef is GroundVehicleModuleDef || tacItemDef is ItemDef itemDef &&
                        (itemDef is GeoVehicleEquipmentDef || itemDef is VehicleItemDef || itemDef is GroundVehicleWeaponDef))
                    {
                        //TFTVLogger.Always($"{tacItemDef.name}");
                        return;

                    }

                    //  TFTVLogger.Always($"is not GroundVehicleModuleDef or GeoVehicleEquipmentDef");

                    BodyPartAspectDef bodyPartAspectDef = tacItemDef.BodyPartAspectDef;
                    if (bodyPartAspectDef != null)
                    {
                        if (bodyPartAspectDef.Endurance > 0)
                        {
                            MethodInfo methodInfo = typeof(UIItemTooltip).GetMethod("SetStat", BindingFlags.NonPublic | BindingFlags.Instance);
                            object[] parameters = { new LocalizedTextBind("KEY_PROGRESSION_STRENGTH"), secondObject, UIUtil.StatsWithSign(bodyPartAspectDef.Endurance), bodyPartAspectDef.Endurance, null, subItemIndex };

                            methodInfo.Invoke(__instance, parameters);
                        }

                        if (bodyPartAspectDef.WillPower > 0)
                        {
                            MethodInfo methodInfo = typeof(UIItemTooltip).GetMethod("SetStat", BindingFlags.NonPublic | BindingFlags.Instance);
                            object[] parameters = { new LocalizedTextBind("KEY_PROGRESSION_WILLPOWER"), secondObject, UIUtil.StatsWithSign(bodyPartAspectDef.WillPower), bodyPartAspectDef.WillPower, null, subItemIndex };

                            methodInfo.Invoke(__instance, parameters);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        internal class EditScreen
        {
            internal class Stats
            {
                //This changes display of Delirium bar in personnel edit screen to show current Delirium value vs max delirium value the character can have
                // taking into account ODI level and bionics
                [HarmonyPatch(typeof(UIModuleCharacterProgression), "SetStatusesPanel")]
                internal static class BG_UIModuleCharacterProgression_SetStatusesPanel_patch
                {

                    private static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
                    {
                        try
                        {
                            //hookToCharacter = ____character;

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
                [HarmonyPatch(typeof(UIModuleCharacterProgression), "GetStarBarValuesDisplayString")]
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
                                                //  forStrengthToolTip += $"+{statModifier.Value} from {passiveModifierAbilityDef.ViewElementDef.DisplayName1.Localize()}";
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
                                            //   forStrengthToolTip += $"+{statModifier2.Value} from {passiveModifier?.ViewElementDef?.DisplayName1?.Localize()}";
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

                            //  StrengthToolTip.TipText += forStrengthToolTip;

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
                                if (____character.CharacterStats.Corruption > TFTVDelirium.CalculateStaminaEffectOnDelirium(____character) && TFTVVoidOmens.VoidOmensCheck[3] == false)
                                {
                                    // TFTVLogger.Always($"current Delirium value is {____character.CharacterStats.Corruption.Value}; effect of Stamina is {TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}, " +
                                    //     $"current attribute value is {currentAttributeValue}, bonusWillpower is {bonusWillpower}, max {____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}");

                                    __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}" +
                                        $"<color=#da5be3> ({currentAttributeValue + bonusWillpower - ____character.CharacterStats.Corruption.Value + TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}</color>)";
                                }
                                else
                                {
                                    //  TFTVLogger.Always($"current Delirium value is {____character.CharacterStats.Corruption.Value}; effect of Stamina is {TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}, " +
                                    //      $"current attribute value is {currentAttributeValue}, bonusWillpower is {bonusWillpower}, TFTVVoidOmens.VoidOmensCheck[3] {TFTVVoidOmens.VoidOmensCheck[3]}");

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

               

                [HarmonyPatch(typeof(UIModuleSoldierEquip), "GetPrimaryWeight")]
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
                [HarmonyPatch(typeof(UIModuleSoldierEquip), "RefreshWeightSlider")]
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

                                if (characterField.GetValue(uIModuleCharacterProgression) is GeoCharacter geoCharacter && !__instance.IsVehicle && !geoCharacter.TemplateDef.IsMutog && uIModuleCharacterProgression != null)
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
                                    //  TFTVLogger.Always($"got all the way to here");
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
                [HarmonyPatch(typeof(UIStateEditSoldier), "RequestRefreshCharacterData")]
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

           // PhoenixPoint.Geoscape.View.ViewStates.UIStateEditSoldier.SoldierSlotItemChangedHandler(PhoenixPoint.Common.View.ViewControllers.Inventory.UIInventorySlot slot)

          //  PhoenixPoint.Common.View.ViewControllers.Inventory.UIInventorySlotSideButton.OnSideButtonPressed


            internal class LoadoutsAndHelmetToggle
            {
                /// <summary>
                /// Patches to add toggle helment and loadouts buttons
                /// </summary>      
                /// 

                public static PhoenixGeneralButton HelmetToggle = null;
                public static PhoenixGeneralButton UnequipAll = null;
                public static PhoenixGeneralButton SaveLoadout = null;
                public static PhoenixGeneralButton LoadLoadout = null;

                public static Dictionary<int, Dictionary<string, List<string>>> CharacterLoadouts = new Dictionary<int, Dictionary<string, List<string>>>();

                private static bool toggleState = false;  // Initial toggle state
                private static readonly string armourItems = "ArmourItems";
                private static readonly string equipmentItems = "EquipmentItems";
                private static readonly string inventoryItems = "InventoryItems";


                [HarmonyPatch(typeof(EditUnitButtonsController), "SetEditUnitButtonsBasedOnType")]
                internal static class TFTV_EditUnitButtonsController_SetEditUnitButtonsBasedOnType_ToggleHelmetButton_patch
                {
                    public static void Prefix(EditUnitButtonsController __instance, UIModuleActorCycle ____parentModule)
                    {
                        try
                        {
                            if (____parentModule.CurrentUnit != null)
                            {
                                //  TFTVLogger.Always($"Actually here; {____parentModule.CurrentState}");

                                switch (____parentModule.CurrentState)
                                {
                                    case UIModuleActorCycle.ActorCycleState.RosterSection:

                                        if (HelmetToggle != null)
                                        {
                                            HelmetToggle.gameObject.SetActive(false);
                                            HelmetToggle.ResetButtonAnimations();
                                            UnequipAll.gameObject.SetActive(false);
                                            UnequipAll.ResetButtonAnimations();
                                            SaveLoadout.gameObject.SetActive(false);
                                            SaveLoadout.ResetButtonAnimations();
                                            LoadLoadout.gameObject.SetActive(false);
                                            LoadLoadout.ResetButtonAnimations();
                                        }

                                        break;

                                    case UIModuleActorCycle.ActorCycleState.EditSoldierSection:


                                        //  HelmetToggle.gameObject.SetActive(true);
                                        //  HelmetToggle.ResetButtonAnimations();
                                        UnequipAll.gameObject.SetActive(true);
                                        UnequipAll.ResetButtonAnimations();
                                        SaveLoadout.gameObject.SetActive(true);
                                        SaveLoadout.ResetButtonAnimations();
                                        LoadLoadout.gameObject.SetActive(true);
                                        LoadLoadout.ResetButtonAnimations();

                                        bool hasAugmentedHead = false;
                                        ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");
                                        foreach (GeoItem bionic in ____parentModule?.CurrentCharacter?.ArmourItems)
                                        {
                                            if ((bionic.CommonItemData.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                            && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                                            {
                                                hasAugmentedHead = true;
                                            }
                                        }

                                        if (hasAugmentedHead)
                                        {
                                            HelmetToggle.gameObject.SetActive(false);
                                            HelmetToggle.ResetButtonAnimations();
                                        }
                                        else
                                        {
                                            HelmetToggle.gameObject.SetActive(true);
                                            HelmetToggle.ResetButtonAnimations();
                                        }


                                        break;
                                    case UIModuleActorCycle.ActorCycleState.EditVehicleSection:
                                        if (HelmetToggle != null)
                                        {
                                            HelmetToggle.gameObject.SetActive(false);
                                            HelmetToggle.ResetButtonAnimations();
                                            UnequipAll.gameObject.SetActive(false);
                                            UnequipAll.ResetButtonAnimations();
                                            SaveLoadout.gameObject.SetActive(false);
                                            SaveLoadout.ResetButtonAnimations();
                                            LoadLoadout.gameObject.SetActive(false);
                                            LoadLoadout.ResetButtonAnimations();
                                        }
                                        break;
                                    case UIModuleActorCycle.ActorCycleState.EditMutogSection:
                                        if (HelmetToggle != null)
                                        {
                                            HelmetToggle.gameObject.SetActive(false);
                                            HelmetToggle.ResetButtonAnimations();
                                            UnequipAll.gameObject.SetActive(false);
                                            UnequipAll.ResetButtonAnimations();
                                            SaveLoadout.gameObject.SetActive(false);
                                            SaveLoadout.ResetButtonAnimations();
                                            LoadLoadout.gameObject.SetActive(false);
                                            LoadLoadout.ResetButtonAnimations();
                                        }
                                        break;
                                    case UIModuleActorCycle.ActorCycleState.CapturedAlienSection:
                                        if (HelmetToggle != null)
                                        {
                                            HelmetToggle.gameObject.SetActive(false);
                                            HelmetToggle.ResetButtonAnimations();
                                            UnequipAll.gameObject.SetActive(false);
                                            UnequipAll.ResetButtonAnimations();
                                            SaveLoadout.gameObject.SetActive(false);
                                            SaveLoadout.ResetButtonAnimations();
                                            LoadLoadout.gameObject.SetActive(false);
                                            LoadLoadout.ResetButtonAnimations();
                                        }
                                        break;
                                }

                                if (____parentModule.CurrentState == UIModuleActorCycle.ActorCycleState.SubmenuSection)//EditUnitButtonsController.CustomizeButton.gameObject.activeInHierarchy)
                                {

                                    // TFTVLogger.Always($"Customize button enabled is {____parentModule.EditUnitButtonsController.CustomizeButton.enabled}");
                                    if (HelmetToggle != null)
                                    {
                                        HelmetToggle.gameObject.SetActive(false);
                                        HelmetToggle.ResetButtonAnimations();
                                        UnequipAll.gameObject.SetActive(false);
                                        UnequipAll.ResetButtonAnimations();
                                        SaveLoadout.gameObject.SetActive(false);
                                        SaveLoadout.ResetButtonAnimations();
                                        LoadLoadout.gameObject.SetActive(false);
                                        LoadLoadout.ResetButtonAnimations();
                                    }
                                    // HelmetsOff = false;
                                }

                                if (____parentModule.CurrentCharacter != null && (CharacterLoadouts == null || CharacterLoadouts != null && !CharacterLoadouts.ContainsKey(____parentModule.CurrentCharacter.Id)))
                                {
                                    if (HelmetToggle != null)
                                    {

                                        LoadLoadout.gameObject.SetActive(false);
                                        LoadLoadout.ResetButtonAnimations();
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

                [HarmonyPatch(typeof(EditUnitButtonsController), "Awake")]
                internal static class TFTV_EditUnitButtonsController_Awake_ToggleHelmetButton_patch
                {

                    public static void Postfix(EditUnitButtonsController __instance)
                    {
                        try
                        {
                            CreateAdditionalButtonsForUIEditScreen(__instance);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void CreateAdditionalButtonsForUIEditScreen(EditUnitButtonsController editUnitButtonsController)
                    {
                        try
                        {
                            if (HelmetToggle == null)
                            {

                                Resolution resolution = Screen.currentResolution;

                                // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                                float resolutionFactorWidth = (float)resolution.width / 1920f;
                                //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                                float resolutionFactorHeight = (float)resolution.height / 1080f;
                                //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                                // TFTVLogger.Always($"checking");

                                PhoenixGeneralButton helmetToggleButton = UnityEngine.Object.Instantiate(editUnitButtonsController.EditButton, editUnitButtonsController.transform);
                                helmetToggleButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_TOGGLEHELMET_TIP");// "Toggles helmet visibility on/off.";
                                                                                                                                                                                  // TFTVLogger.Always($"original icon position {newPhoenixGeneralButton.transform.position}, edit button position {__instance.EditButton.transform.position}");
                                helmetToggleButton.transform.position += new Vector3(-50 * resolutionFactorWidth, -35 * resolutionFactorHeight, 0);

                                // TFTVLogger.Always($"new icon position {newPhoenixGeneralButton.transform.position}");

                                PhoenixGeneralButton unequipAllPhoenixGeneralButton = UnityEngine.Object.Instantiate(editUnitButtonsController.EditButton, editUnitButtonsController.transform);
                                unequipAllPhoenixGeneralButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_UNEQUIP_TIP");// "Unequips all the items currently equipped by the operative.";
                                unequipAllPhoenixGeneralButton.transform.position = helmetToggleButton.transform.position + new Vector3(0, -100 * resolutionFactorHeight, 0);

                                PhoenixGeneralButton saveLoadout = UnityEngine.Object.Instantiate(editUnitButtonsController.EditButton, editUnitButtonsController.transform);
                                saveLoadout.transform.position = unequipAllPhoenixGeneralButton.transform.position + new Vector3(0, -100 * resolutionFactorHeight, 0);
                                saveLoadout.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_SAVELOAD_TIP");//"Saves the current loadout of the operative.";

                                PhoenixGeneralButton loadLoadout = UnityEngine.Object.Instantiate(editUnitButtonsController.EditButton, editUnitButtonsController.transform);
                                loadLoadout.transform.position = saveLoadout.transform.position + new Vector3(0, -100 * resolutionFactorHeight, 0);
                                loadLoadout.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_EDIT_SCREEN_LOADLOAD_TIP");//"Loads the previously saved loadout for this operative.";


                                helmetToggleButton.PointerClicked += () => ToggleButtonClicked(helmetToggleButton);
                                unequipAllPhoenixGeneralButton.PointerClicked += () => UnequipButtonClicked();
                                saveLoadout.PointerClicked += () => SaveLoadoutButtonClicked();
                                loadLoadout.PointerClicked += () => LoadLoadoutButtonClicked();

                                helmetToggleButton.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("TFTV_helmet_off_icon.png");
                                unequipAllPhoenixGeneralButton.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("lockers.png");
                                saveLoadout.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("loadout_load.png");
                                loadLoadout.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("loadout_save.png");

                                HelmetToggle = helmetToggleButton;
                                UnequipAll = unequipAllPhoenixGeneralButton;
                                SaveLoadout = saveLoadout;
                                LoadLoadout = loadLoadout;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void LoadLoadoutButtonClicked()
                    {
                        try
                        {
                            GeoCharacter character = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;// hookToCharacter;

                            if (!CharacterLoadouts.ContainsKey(character.Id))
                            {
                                return;
                            }

                            UIModuleSoldierEquip uIModuleSoldierEquip = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.SoldierEquipModule;

                            UnequipButtonClicked();
                            UIInventoryList storage = uIModuleSoldierEquip.StorageList;

                            Predicate<TacticalItemDef> filter = null;

                            storage.SetFilter(filter);

                            foreach (string armorPiece in CharacterLoadouts[character.Id][armourItems])
                            {
                                ICommonItem item = storage.UnfilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == armorPiece).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == armorPiece).FirstOrDefault();

                                for (int x = 0; x < uIModuleSoldierEquip.ArmorList.Slots.Count(); x++)
                                {
                                    //   TFTVLogger.Always($"looking at slot{uIModuleSoldierEquip.ArmorList.Slots[x].name} for {item}");

                                    if (item != null && uIModuleSoldierEquip.ArmorList.CanAddItem(item, uIModuleSoldierEquip.ArmorList.Slots[x]))
                                    {
                                        //  TFTVLogger.Always($"found slot {uIModuleSoldierEquip.ArmorList.Slots[x].name} for armor item {item}");
                                        uIModuleSoldierEquip.ArmorList.AddItem(item.GetSingleItem(), uIModuleSoldierEquip.ArmorList.Slots[x], storage);

                                        storage.RemoveItem(item.GetSingleItem(), null);
                                    }
                                }
                            }

                            for (int x = 0; x < CharacterLoadouts[character.Id][equipmentItems].Count(); x++)
                            {
                                string equipment = CharacterLoadouts[character.Id][equipmentItems][x];
                                ICommonItem item = storage.UnfilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == equipment).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == equipment).FirstOrDefault();
                                //  TFTVLogger.Always($"equipment item is {item}");

                                if (item != null && uIModuleSoldierEquip.ReadyList.CanAddItem(item))
                                {
                                    //  TFTVLogger.Always($"equipment item is {item}");
                                    uIModuleSoldierEquip.ReadyList.AddItem(item.GetSingleItem(), uIModuleSoldierEquip.ReadyList.Slots[x], storage);
                                    storage.RemoveItem(item.GetSingleItem(), null);
                                }
                            }

                            for (int x = 0; x < CharacterLoadouts[character.Id][inventoryItems].Count(); x++)
                            {
                                string inventory = CharacterLoadouts[character.Id][inventoryItems][x];
                                ICommonItem item = storage.UnfilteredItems.Where(
                                    (ICommonItem ufi) => ufi.ItemDef.Guid == inventory).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == inventory).FirstOrDefault();

                                if (item != null && uIModuleSoldierEquip.InventoryList.CanAddItem(item))
                                {
                                    uIModuleSoldierEquip.InventoryList.AddItem(item.GetSingleItem(), uIModuleSoldierEquip.InventoryList.Slots[x], storage);
                                    storage.RemoveItem(item.GetSingleItem(), null);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void SaveLoadoutButtonClicked()
                    {
                        try
                        {
                            GeoCharacter character = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;//hookToCharacter;

                            Dictionary<string, List<string>> characterItems = new Dictionary<string, List<string>>
                    {
                        { armourItems, new List<string>() },
                        { equipmentItems, new List<string>() },
                        { inventoryItems, new List<string>() }
                    };

                            foreach (GeoItem armourPiece in character.ArmourItems.Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                    Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                            {

                                characterItems[armourItems].Add(armourPiece.ItemDef.Guid);

                            }
                            foreach (GeoItem equipmentPiece in character.EquipmentItems)
                            {
                                characterItems[equipmentItems].Add(equipmentPiece.ItemDef.Guid);

                            }
                            foreach (GeoItem inventoryPiece in character.InventoryItems)
                            {
                                characterItems[inventoryItems].Add(inventoryPiece.ItemDef.Guid);

                            }

                            if (CharacterLoadouts == null)
                            {
                                CharacterLoadouts = new Dictionary<int, Dictionary<string, List<string>>>();
                            }

                            if (!CharacterLoadouts.ContainsKey(character.Id))
                            {
                                CharacterLoadouts.Add(character.Id, characterItems);
                            }
                            else
                            {
                                CharacterLoadouts[character.Id].Clear();
                                CharacterLoadouts[character.Id].AddRange(characterItems);
                            }

                            LoadLoadout.gameObject.SetActive(true);
                            LoadLoadout.ResetButtonAnimations();
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void ToggleButtonClicked(PhoenixGeneralButton helmetToggleButton)
                    {
                        try
                        {
                            toggleState = !toggleState;  // Flip the toggle state

                            // Perform any actions based on the toggle state
                            if (toggleState)
                            {
                                //  if (uIModuleSoldierCustomization != null)
                                //  {
                                //      uIModuleSoldierCustomization.HideHelmetToggle.isOn = true;

                                //  }
                                helmetToggleButton.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("TFTV_helmet_on_icon.png");
                                HelmetsOff = true;
                                // TFTVLogger.Always($"{uIModuleSoldierCustomization.HideHelmetToggle.isOn}");

                            }
                            else
                            {

                                //                    if (uIModuleSoldierCustomization != null)
                                //                  {
                                //                    uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                //              }

                                helmetToggleButton.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("TFTV_helmet_off_icon.png");
                                HelmetsOff = false;


                            }
                            TFTVLogger.Always($"HelmetsOff is {HelmetsOff}");
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    private static void UnequipButtonClicked()
                    {
                        try
                        {
                            GeoCharacter geoCharacter = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;
                            UIModuleSoldierEquip uIModuleSoldierEquip = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.SoldierEquipModule;

                            if (uIModuleSoldierEquip != null && geoCharacter != null)
                            {
                                GeoCharacter character = geoCharacter;

                                List<GeoItem> armorItems = new List<GeoItem>();
                                List<GeoItem> inventoryItems = new List<GeoItem>();
                                List<GeoItem> equipmentItems = new List<GeoItem>();

                                List<GeoItem> attachments = new List<GeoItem>();

                                if (character.ArmourItems.Any(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                                {
                                    foreach (GeoItem bionic in character.ArmourItems.
                                     Where(a => a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                                    {
                                        foreach (GeoItem geoItem in character.ArmourItems.
                                     Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                                        {
                                            if (geoItem.ItemDef.RequiredSlotBinds[0].IsCompatibleWith(bionic.ItemDef))
                                            {
                                                // TFTVLogger.Always($"{geoItem.ItemDef} can go on {bionic.ItemDef}");
                                                attachments.Add(geoItem);

                                            }
                                        }
                                    }
                                }

                                armorItems.AddRange(character.ArmourItems.
                                    Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                    Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)).
                                    Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("Attachment")).
                                    Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("BackPack"))//.
                                                                                                                      // Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("LegsAttachment"))
                                                                                                                      //  Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("MechArm"))
                                    );

                                equipmentItems.AddRange(character.EquipmentItems);
                                inventoryItems.AddRange(character.InventoryItems);

                                armorItems.AddRange(attachments);

                                GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                                int storageCapacity = phoenixFaction.GetTotalAvailableStorage();
                                int storageUsed = phoenixFaction.ItemStorage.GetStorageUsed();

                                if (equipmentItems.Count + inventoryItems.Count + armorItems.Count + storageUsed > storageCapacity)
                                {
                                    string warning = TFTVCommonMethods.ConvertKeyToString("KEY_WARNING_STORAGE_EXCEEDED");

                                    GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Stop, MessageBoxButtons.OK, null);
                                    return;
                                }


                                foreach (GeoItem item in inventoryItems)
                                {
                                    // TFTVLogger.Always($"{item.ItemDef.name} in Inventory");
                                    uIModuleSoldierEquip.StorageList.AddItem(item);
                                    uIModuleSoldierEquip.InventoryList.RemoveItem(item, null);
                                }

                                foreach (GeoItem item in equipmentItems)
                                {
                                    // TFTVLogger.Always($"{item.ItemDef.name} in Equipment");
                                    uIModuleSoldierEquip.StorageList.AddItem(item);
                                    uIModuleSoldierEquip.ReadyList.RemoveItem(item, null);
                                }

                                foreach (GeoItem item in armorItems)
                                {
                                    //  TFTVLogger.Always($"{item.ItemDef.name} in Armor. {item.ItemDef?.RequiredSlotBinds[0].RequiredSlot?.name}");
                                    uIModuleSoldierEquip.StorageList.AddItem(item);
                                    uIModuleSoldierEquip.ArmorList.RemoveItem(item, null);
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

        internal class MissionDeployment
        {
            ///Patches to show mission light conditions
            [HarmonyPatch(typeof(UIStateRosterDeployment), "EnterState")]
            public static class TFTV_UIStateRosterDeployment_EnterState_patch
            {
                public static void Postfix(UIStateRosterDeployment __instance)
                {
                    try
                    {
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        GeoSite geoSite = null;

                        UIModuleActorCycle uIModuleActorCycle = controller.View.GeoscapeModules.ActorCycleModule;
                        UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                        if (uIModuleActorCycle.CurrentCharacter != null && !uIModuleActorCycle.CurrentCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleClassTag))
                        {
                            foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                            {
                                if (geoVehicle.Soldiers.Contains(uIModuleActorCycle.CurrentCharacter))
                                {
                                    geoSite = geoVehicle.CurrentSite;
                                    break;
                                }
                            }

                            if (geoSite != null)
                            {

                                int hourOfTheDay = geoSite.LocalTime.DateTime.Hour;
                                int minuteOfTheHour = geoSite.LocalTime.DateTime.Minute;
                                bool dayTimeMission = hourOfTheDay >= 6 && hourOfTheDay <= 20;

                                TFTVLogger.Always($"LocalTime: {hourOfTheDay:00}:{minuteOfTheHour:00}");

                                Transform objectives = uIModuleDeploymentMissionBriefing.ObjectivesTextContainer.transform;
                                Transform lootContainer = uIModuleDeploymentMissionBriefing.AutolootContainer.transform;

                                Transform newIcon = UnityEngine.Object.Instantiate(lootContainer.GetComponent<Transform>().GetComponentInChildren<Image>().transform, uIModuleDeploymentMissionBriefing.MissionNameText.transform);

                                Sprite lightConditions = Helper.CreateSpriteFromImageFile(dayTimeMission ? "light_conditions_sun.png" : "light_conditions_moon.png");
                                Color color = dayTimeMission ? yellow : dark;

                                newIcon.GetComponentInChildren<Image>().sprite = lightConditions;
                                newIcon.GetComponentInChildren<Image>().color = color;

                                string text = $"Local time is {hourOfTheDay:00}:{minuteOfTheHour:00}";
                                newIcon.gameObject.AddComponent<UITooltipText>().TipText = text;

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(UIStateRosterDeployment), "ExitState")]
            public static class TFTV_UIStateRosterDeployment_ExitState_patch
            {
                public static void Postfix(UIStateRosterDeployment __instance)
                {
                    try
                    {

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                        uIModuleDeploymentMissionBriefing.MissionNameText.transform.DestroyChildren();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }




        }

        internal class RepairingBionics
        {
            /// <summary>
            /// Patches to fix repairing bionics
            /// </summary>
            [HarmonyPatch(typeof(UIModuleMutationSection), "SelectMutation")]

            public static class TFTV_UIModuleMutationSection_SelectMutation_patch
            {
                public static void Postfix(UIModuleMutationSection __instance, IAugmentationUIModule ____parentModule)
                {
                    try
                    {
                        if (__instance.RepairButton.isActiveAndEnabled)
                        {
                            float equippedItemHealth = ____parentModule.CurrentCharacter.GetEquippedItemHealth(__instance.MutationUsed);
                            ResourcePack resourcePack = __instance.MutationUsed.ManufacturePrice * (1f - equippedItemHealth) * 0.5f;

                            bool interactable = ____parentModule.Context.ViewerFaction.Wallet.HasResources(resourcePack);
                            __instance.RepairButtonCost.Init(resourcePack);
                            __instance.RepairButton.SetEnabled(interactable);
                            __instance.RepairButton.SetInteractable(interactable);
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleMutationSection), "RepairItem")]

            public static class TFTV_UIModuleMutationSection_RepairItem_patch
            {

                public static void Postfix(UIModuleMutationSection __instance, IAugmentationUIModule ____parentModule)
                {
                    try
                    {
                        // TFTVLogger.Always("RepairItem invoked");

                        if (!(____parentModule.CurrentCharacter.GetEquippedItemHealth(__instance.MutationUsed) >= 1f) && ____parentModule.CurrentCharacter.RepairItem(__instance.MutationUsed))
                        {
                            ____parentModule.RequestViewRefresh();

                            typeof(UIModuleMutationSection).GetMethod("RefreshContainerSlots", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);

                            __instance.RepairButton.gameObject.SetActive(value: false);
                            __instance.MutateButton.gameObject.SetActive(value: false);

                            UIModuleActorCycle controller = (UIModuleActorCycle)UnityEngine.Object.FindObjectOfType(typeof(UIModuleActorCycle));

                            controller.DisplaySoldier(____parentModule.CurrentCharacter, true);


                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleReplenish), "AddRepairableItem")]

            public static class TFTV_UIModuleReplenish_AddRepairableItem_patch
            {

                public static bool Prefix(UIModuleReplenish __instance, GeoCharacter character, ItemDef itemDef, ref int materialsCost, ref int techCost, ref bool __result)
                {
                    try
                    {
                        GeoFaction faction = character.Faction;


                        MethodInfo onEnterSlotMethodInfo = typeof(UIModuleReplenish).GetMethod("OnEnterSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo onExitSlotMethodInfo = typeof(UIModuleReplenish).GetMethod("OnExitSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate onEnterSlotDelegate = Delegate.CreateDelegate(typeof(InteractHandler), __instance, onEnterSlotMethodInfo);
                        Delegate onExitSlotDelegate = Delegate.CreateDelegate(typeof(InteractHandler), __instance, onExitSlotMethodInfo);

                        MethodInfo singleItemRepairMethodInfo = typeof(UIModuleReplenish).GetMethod("SingleItemRepair", BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate singleItemRepairDelegate = Delegate.CreateDelegate(typeof(Action<GeoManufactureItem>), __instance, singleItemRepairMethodInfo);

                        float equippedItemHealth = character.GetEquippedItemHealth(itemDef);
                        ResourcePack resourcePack = itemDef.ManufacturePrice * (1f - equippedItemHealth) * 0.5f;
                        materialsCost += resourcePack.ByResourceType(ResourceType.Materials).RoundedValue;
                        techCost += resourcePack.ByResourceType(ResourceType.Tech).RoundedValue;
                        GeoManufactureItem geoManufactureItem = UnityEngine.Object.Instantiate(__instance.ItemListPrefab, __instance.ItemListContainer);
                        ReplenishmentElementController.CreateAndAdd(geoManufactureItem.gameObject, ReplenishmentType.Repair, character, geoManufactureItem.ItemDef);
                        geoManufactureItem.OnEnter = (InteractHandler)Delegate.Combine(geoManufactureItem.OnEnter, onEnterSlotDelegate);
                        geoManufactureItem.OnExit = (InteractHandler)Delegate.Combine(geoManufactureItem.OnExit, onExitSlotDelegate);


                        geoManufactureItem.OnSelected = (Action<GeoManufactureItem>)Delegate.Combine(geoManufactureItem.OnSelected, singleItemRepairDelegate);
                        geoManufactureItem.Init(itemDef, faction, resourcePack, repairMode: true);
                        PhoenixGeneralButton component = geoManufactureItem.AddToQueueButton.GetComponent<PhoenixGeneralButton>();
                        if (component != null && equippedItemHealth == 1f)
                        {
                            component.SetEnabled(isEnabled: false);
                        }

                        __instance.RepairableItems.Add(geoManufactureItem);
                        __result = faction.Wallet.HasResources(resourcePack);
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(GeoCharacter), "RepairItem", new Type[] { typeof(GeoItem), typeof(bool) })]

            public static class TFTV_GeoCharacter_RepairItem_GeoItem_patch
            {

                public static bool Prefix(GeoCharacter __instance, ref bool __result, GeoItem item, bool payCost = true)
                {
                    try
                    {
                        _ = item.ItemDef;
                        float equippedItemHealth = __instance.GetEquippedItemHealth(item);
                        if (equippedItemHealth >= 1f)
                        {
                            __result = false;
                            return false;
                        }

                        ResourcePack pack = item.ItemDef.ManufacturePrice * (1f - equippedItemHealth) * 0.5f;
                        if (!__instance.Faction.Wallet.HasResources(pack) && payCost)
                        {
                            __result = false;
                            return false;
                        }

                        if (payCost)
                        {
                            __instance.Faction.Wallet.Take(pack, OperationReason.ItemRepair);
                        }

                        __instance.RestoreBodyPart(item);
                        __result = true;
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

        internal class DamagePrediction
        {
            //Patch to show correct damage prediction with mutations and Delirium 
            [HarmonyPatch(typeof(PhoenixPoint.Tactical.UI.Utils), "GetDamageKeywordValue")]
            public static class TFTV_Utils_GetDamageKeywordValue_DamagePredictionMutations_Patch
            {
                public static void Postfix(DamagePayload payload, DamageKeywordDef damageKeyword, TacticalActor tacticalActor, ref float __result)
                {
                    try
                    {
                        SharedData shared = GameUtl.GameComponent<SharedData>();
                        SharedDamageKeywordsDataDef damageKeywords = shared.SharedDamageKeywords;
                        StandardDamageTypeEffectDef projectileDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                        StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                        if (tacticalActor != null && (damageKeyword.DamageTypeDef == projectileDamage || damageKeyword.DamageTypeDef == blastDamage) && damageKeyword != damageKeywords.SyphonKeyword) //&& damageKeyword is PiercingDamageKeywordDataDef == false) 
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

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }







        }

        internal class Mutoids
        {
            /// <summary>
            /// Patches to show class icons on Mutoids
            /// </summary>

            [HarmonyPatch(typeof(GeoCharacter), "GetClassViewElementDefs")]
            internal static class TFTV_GeoCharacter_GetClassViewElementDefs_patch
            {
                public static void Postfix(ref ICollection<ViewElementDef> __result, GeoCharacter __instance)
                {
                    try
                    {
                        if (__instance.IsMutoid)
                        {

                            ClassTagDef assault = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                            ClassTagDef heavy = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
                            ClassTagDef sniper = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
                            ClassTagDef berserker = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");
                            ClassTagDef priest = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
                            ClassTagDef technician = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");
                            ClassTagDef infiltrator = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");

                            ViewElementDef assaultVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Assault_ClassProficiency_AbilityDef]");
                            ViewElementDef heavyVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Heavy_ClassProficiency_AbilityDef]");
                            ViewElementDef sniperVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Sniper_ClassProficiency_AbilityDef]");
                            ViewElementDef berserkerVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Berserker_ClassProficiency_AbilityDef]");
                            ViewElementDef priestVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Priest_ClassProficiency_AbilityDef]");
                            ViewElementDef technicianVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Technician_ClassProficiency_AbilityDef]");
                            ViewElementDef infiltratorVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Infiltrator_ClassProficiency_AbilityDef]");

                            ViewElementDef mutoidVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [MutoidSpecializationDef]");

                            Dictionary<ClassTagDef, ViewElementDef> dictionary = new Dictionary<ClassTagDef, ViewElementDef>(){
                            { assault, assaultVE },
                            { heavy, heavyVE },
                            { sniper, sniperVE },
                            { berserker, berserkerVE },
                            { priest, priestVE },
                            { technician, technicianVE },
                            { infiltrator, infiltratorVE }
                        };

                            foreach (ClassTagDef classTag in dictionary.Keys)
                            {
                                if (__instance.ClassTags.Contains(classTag))
                                {
                                    __result = new ViewElementDef[2] { mutoidVE, dictionary[classTag] };
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



            [HarmonyPatch(typeof(GeoPhoenixFaction), "AddRecruitToContainerFinal")]

            internal static class TFTV_GeoPhoenixFaction_AddRecruitToContainerFinal_patch
            {
                public static void Prefix(ref GeoCharacter recruit)
                {
                    try
                    {
                        if (recruit.IsMutoid)
                        {

                            ClassTagDef assault = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                            ClassTagDef heavy = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
                            ClassTagDef sniper = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
                            ClassTagDef berserker = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");
                            ClassTagDef priest = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
                            ClassTagDef technician = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");
                            ClassTagDef infiltrator = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");

                            ViewElementDef assaultVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Assault_ClassProficiency_AbilityDef]");
                            ViewElementDef heavyVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Heavy_ClassProficiency_AbilityDef]");
                            ViewElementDef sniperVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Sniper_ClassProficiency_AbilityDef]");
                            ViewElementDef berserkerVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Berserker_ClassProficiency_AbilityDef]");
                            ViewElementDef priestVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Priest_ClassProficiency_AbilityDef]");
                            ViewElementDef technicianVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Technician_ClassProficiency_AbilityDef]");
                            ViewElementDef infiltratorVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Infiltrator_ClassProficiency_AbilityDef]");

                            Dictionary<ClassTagDef, ViewElementDef> dictionary = new Dictionary<ClassTagDef, ViewElementDef>(){
                            { assault, assaultVE },
                            { heavy, heavyVE },
                            { sniper, sniperVE },
                            { berserker, berserkerVE },
                            { priest, priestVE },
                            { technician, technicianVE },
                            { infiltrator, infiltratorVE }
                        };

                            foreach (ClassTagDef classTag in dictionary.Keys)
                            {
                                if (recruit.ClassTags.Contains(classTag))
                                {
                                    recruit.Identity.Name = $"{TFTVCommonMethods.ConvertKeyToString("KEY_MUTOID_CLASS")} {dictionary[classTag].DisplayName2.Localize()}";
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



            [HarmonyPatch(typeof(TacticalActorBase), "UpdateClassViewElementDefs")]

            internal static class TFTV_TacticalActorBase_UpdateClassViewElementDefs_patch
            {
                public static void Postfix(TacticalActorBase __instance, ref List<ViewElementDef> ____classViewElementDefs)
                {
                    try

                    {
                        GameTagDef mutoidTag = DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef");


                        if (__instance is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(mutoidTag))
                        {

                            //  TFTVLogger.Always($"{tacticalActor.DisplayName}");
                            ClassTagDef assault = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                            ClassTagDef heavy = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
                            ClassTagDef sniper = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
                            ClassTagDef berserker = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");
                            ClassTagDef priest = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
                            ClassTagDef technician = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");
                            ClassTagDef infiltrator = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");

                            ViewElementDef assaultVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Assault_ClassProficiency_AbilityDef]");
                            ViewElementDef heavyVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Heavy_ClassProficiency_AbilityDef]");
                            ViewElementDef sniperVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Sniper_ClassProficiency_AbilityDef]");
                            ViewElementDef berserkerVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Berserker_ClassProficiency_AbilityDef]");
                            ViewElementDef priestVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Priest_ClassProficiency_AbilityDef]");
                            ViewElementDef technicianVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Technician_ClassProficiency_AbilityDef]");
                            ViewElementDef infiltratorVE = DefCache.GetDef<ViewElementDef>("E_ViewElement [Infiltrator_ClassProficiency_AbilityDef]");

                            TacticalAbilityViewElementDef mutoidVE = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Mutoid_ClassProficiency_AbilityDef]");

                            Dictionary<ClassTagDef, ViewElementDef> dictionary = new Dictionary<ClassTagDef, ViewElementDef>(){
                            { assault, assaultVE },
                            { heavy, heavyVE },
                            { sniper, sniperVE },
                            { berserker, berserkerVE },
                            { priest, priestVE },
                            { technician, technicianVE },
                            { infiltrator, infiltratorVE }
                        };

                            foreach (ClassTagDef classTag in dictionary.Keys)
                            {
                                if (tacticalActor.GameTags.Contains(classTag))
                                {

                                    ____classViewElementDefs = new List<ViewElementDef> { mutoidVE, dictionary[classTag] };
                                    //  TFTVLogger.Always("Here we are");

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






        }

        internal class DeliriumFaceShader
        {
            public static GeoCharacter HookToCharacterForDeliriumShader = null;

            //Patch to reduce Delirium visuals on faces of infected characters

            [HarmonyPatch(typeof(UIModuleActorCycle), "SetupFaceCorruptionShader")]
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


            [HarmonyPatch(typeof(SquadMemberScrollerController), "SetupFaceCorruptionShader")]

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


            [HarmonyPatch(typeof(CharacterStats), "get_CorruptionProgressRel")]
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

        internal class ShowWithoutHelmet
        {
            [HarmonyPatch(typeof(UIModuleSoldierCustomization), "OnNewCharacter")]

            internal static class TFTV_UI_UIModuleSoldierCustomization_patch
            {
                private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

                private static void Postfix(GeoCharacter newCharacter, UIModuleSoldierCustomization __instance)
                {
                    try
                    {
                        //  TFTVLogger.Always("Checking that OnNewCharacter is launched");
                        if (newCharacter != null && (newCharacter.TemplateDef.IsHuman || newCharacter.TemplateDef.IsMutoid))
                        {
                            //    TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is human or mutoid");

                            UIModuleSoldierCustomization uIModuleSoldierCustomizationLocal = __instance;//(UIModuleSoldierCustomization)UnityEngine.Object.FindObjectOfType(typeof(UIModuleSoldierCustomization));
                            uIModuleSoldierCustomization = uIModuleSoldierCustomizationLocal;
                            uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;

                            if (newCharacter != null && (!newCharacter.TemplateDef.IsHuman || newCharacter.IsMutoid))
                            {

                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                //  TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is mutoid");

                            }
                            else if (newCharacter != null && newCharacter.TemplateDef.IsHuman)
                            {
                                // TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is human");
                                bool hasAugmentedHead = false;
                                foreach (GeoItem bionic in (newCharacter.ArmourItems))
                                {
                                    if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                                    && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                                    {
                                        hasAugmentedHead = true;
                                    }
                                }

                                if (hasAugmentedHead)
                                {
                                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                    uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                    //   TFTVLogger.Always("character is " + newCharacter.DisplayName + " and has augmented head");
                                }
                                else
                                {
                                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;
                                    //   TFTVLogger.Always("character is " + newCharacter.DisplayName + " and does not have an augmented head");
                                }
                            }
                            /* else
                             {
                                 uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;
                             }*/

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


            }



            public static UIModuleSoldierCustomization uIModuleSoldierCustomization = null;


            [HarmonyPatch(typeof(UIStateSoldierCustomization), "UpdateHelmetShown")]
            internal static class TFTV_UIStateSoldierCustomization_UpdateHelmetShown_HelmetToggle_patch
            {
                public static void Postfix()
                {
                    try
                    {

                        HelmetsOff = !uIModuleSoldierCustomization.HideHelmetToggle.isOn;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

            }
            [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")]
            internal static class TFTV_UIStateSoldierCustomization_DisplaySoldier_HelmetToggle_patch
            {
                private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

                public static void Postfix()
                {
                    try
                    {
                        GeoCharacter geoCharacter = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;//

                        HelmetsOff = false;
                        //  TFTVLogger.Always("Trying to set helmets off if character has mutated head");
                        if (geoCharacter != null && (geoCharacter.TemplateDef.IsHuman || geoCharacter.TemplateDef.IsMutoid))
                        {
                            //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human or mutoid");
                            if (geoCharacter != null && (!geoCharacter.TemplateDef.IsHuman || geoCharacter.IsMutoid))
                            {
                                //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is mutoid");
                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;

                            }
                            else if (geoCharacter != null && geoCharacter.TemplateDef.IsHuman)
                            {
                                //    TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human");
                                bool hasAugmentedHead = false;
                                foreach (GeoItem bionic in (geoCharacter.ArmourItems))
                                {
                                    if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                                    && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                                    {
                                        hasAugmentedHead = true;
                                    }
                                }

                                if (hasAugmentedHead)
                                {
                                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                    uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                    //   TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and has augmented head");
                                }
                                else
                                {
                                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;

                                    //    TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and does not have an augmented head");
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


            [HarmonyPatch(typeof(UIModuleActorCycle), "DisplaySoldier", new Type[] { typeof(GeoCharacter), typeof(bool), typeof(bool), typeof(bool) })]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            internal static class BG_UIModuleActorCycle_DisplaySoldier_patch
            {
                private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

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
                                    && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
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

                                if (HelmetsOff && __instance.CurrentState != UIModuleActorCycle.ActorCycleState.SubmenuSection)
                                {

                                    // if (uIModuleSoldierCustomization == null && HelmetsOff || uIModuleSoldierCustomization.HideHelmetToggle.isOn)
                                    // {
                                    __instance.DisplaySoldier(unitDisplayData, resetAnimation, addWeapon, showHelmet = false);
                                    return false;
                                }

                                else
                                {
                                    return true;

                                }
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








        }

        internal class CutscenesAndSplashscreens
        {
            //Adapted from Mad's Assorted Adjustments, all hail the Great Mad!
            [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
            public static class TFTV_PhoenixGame_RunGameLevel_SkipLogos_Patch
            {
                public static bool Prefix(PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result)
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    try
                    {
                        if (config.SkipMovies)
                        {

                            if (levelSceneBinding == __instance.Def.IntroLevelSceneDef.Binding)
                            {
                                __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
                                return false;
                            }

                            return true;
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            [HarmonyPatch(typeof(UIStateHomeScreenCutscene), "EnterState")]
            public static class TFTV_PhoenixGame_RunGameLevel_SkipIntro_Patch
            {
                public static void Postfix(UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
                {
                    TFTVConfig config = TFTVMain.Main.Config;
                    if (config.SkipMovies)
                    {
                        try
                        {
                            if (____sourcePlaybackDef == null)
                            {
                                return;
                            }

                            if (____sourcePlaybackDef.ResourcePath.Contains("Game_Intro_Cutscene"))
                            {
                                typeof(UIStateHomeScreenCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(UIStateTacticalCutscene), "EnterState")]
            public static class TFTV_PhoenixGame_RunGameLevel_SkipLanding_Patch
            {
                public static void Postfix(UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
                {
                    TFTVLogger.Always($"EnterState called");

                    TFTVConfig config = TFTVMain.Main.Config;
                    if (config.SkipMovies)
                    {

                        try
                        {
                            //  TFTVLogger.Always($"Skip Movies check passed");

                            if (____sourcePlaybackDef == null)
                            {
                                return;
                            }
                            if (____sourcePlaybackDef.ResourcePath.Contains("LandingSequences"))
                            {
                                // TFTVLogger.Always($"LandingSequence getting canceled");
                                typeof(UIStateTacticalCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(__instance, null);
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
    }
}
