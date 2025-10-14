using Base;
using Base.Core;
using Base.Defs;
using Base.Entities;
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
using PhoenixPoint.Geoscape.View;
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
using PhoenixPoint.Tactical.Entities.Equipments;
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
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal static Color yellow = new Color(255, 255, 0, 1.0f);
        internal static Color dark = new Color(52, 52, 61, 1.0f);

        public static bool HelmetsOff;

        private static void FixCarsWithDelirium(GeoCharacter character)
        {
            try
            {
                if (character.CharacterStats.Corruption > 0f && character.Fatigue == null)
                {
                    TFTVLogger.Always($"{character.DisplayName} had Delirium, but has no Stamina! Setting Delirium to 0");
                    character.CharacterStats.Corruption.Set(0f);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


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

                    public static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
                    {
                        try
                        {

                            FixCarsWithDelirium(____character);

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
                    private const string HeavyConditioningLocKey = "TFTV_DRILL_heavyconditioning_NAME";
                    private const string AksuSprintDrillLocKey = "TFTV_DRILL_aksusprintdrill_NAME";

                    private static readonly GameTagDef HeavyClassTag = DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef");
                    private static readonly GameTagDef BionicTag = DefCache.GetDef<GameTagDef>("Bionic_TagDef");

                    private static readonly HashSet<TacticalItemDef> AksuArmorPieces = new HashSet<TacticalItemDef>
    {
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_BodyPartDef"),
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_BodyPartDef"),
        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_ItemDef"),
    };

                    internal struct DrillBonuses
                    {
                        public float HeavyConditioningSpeedBonus;
                        public float HeavyConditioningAccuracyBonus;
                        public float HeavyConditioningPerceptionBonus;
                        public float HeavyConditioningStealthBonus;
                        public float AksuSprintSpeedBonus;

                        public bool HasHeavyConditioningBonus =>
                            !Mathf.Approximately(HeavyConditioningSpeedBonus, 0f) ||
                            !Mathf.Approximately(HeavyConditioningAccuracyBonus, 0f) ||
                            !Mathf.Approximately(HeavyConditioningPerceptionBonus, 0f) ||
                            !Mathf.Approximately(HeavyConditioningStealthBonus, 0f);

                        public bool HasAksuSprintBonus => !Mathf.Approximately(AksuSprintSpeedBonus, 0f);
                    }

                    internal static DrillBonuses CalculateDrillBonuses(GeoCharacter character)
                    {
                        DrillBonuses bonuses = default;

                        if (character == null)
                        {
                            return bonuses;
                        }

                        bool hasHeavyConditioning = false;
                        bool hasAksuSprint = false;

                        if (character.Progression != null)
                        {
                            foreach (TacticalAbilityDef ability in character.Progression.Abilities)
                            {
                                string abilityLocKey = ability?.ViewElementDef?.DisplayName1?.LocalizationKey;
                                if (abilityLocKey == HeavyConditioningLocKey)
                                {
                                    hasHeavyConditioning = true;
                                }
                                else if (abilityLocKey == AksuSprintDrillLocKey)
                                {
                                    hasAksuSprint = true;
                                }
                            }
                        }

                        if (!hasHeavyConditioning && !hasAksuSprint)
                        {
                            return bonuses;
                        }

                        int heavyArmorPiecesEquipped = 0;
                        float heavyArmorSpeedPenalty = 0f;
                        float heavyArmorAccuracyPenalty = 0f;
                        float heavyArmorPerceptionPenalty = 0f;
                        float heavyArmorStealthPenalty = 0f;

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

                            if (hasHeavyConditioning && tacticalItemDef.Tags.Contains(HeavyClassTag) && !tacticalItemDef.Tags.Contains(BionicTag))
                            {
                                heavyArmorPiecesEquipped++;

                                if (bodyPartAspectDef.Speed < 0f)
                                {
                                    heavyArmorSpeedPenalty += bodyPartAspectDef.Speed;
                                }

                                if (bodyPartAspectDef.Accuracy < 0f)
                                {
                                    heavyArmorAccuracyPenalty += bodyPartAspectDef.Accuracy;
                                }

                                if (bodyPartAspectDef.Perception < 0f)
                                {
                                    heavyArmorPerceptionPenalty += bodyPartAspectDef.Perception;
                                }

                                if (bodyPartAspectDef.Stealth < 0f)
                                {
                                    heavyArmorStealthPenalty += bodyPartAspectDef.Stealth;
                                }
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

                        if (hasHeavyConditioning && heavyArmorPiecesEquipped >= 3)
                        {
                            if (heavyArmorSpeedPenalty < 0f)
                            {
                                bonuses.HeavyConditioningSpeedBonus = -heavyArmorSpeedPenalty / 2f;
                            }

                            if (heavyArmorAccuracyPenalty < 0f)
                            {
                                bonuses.HeavyConditioningAccuracyBonus = -heavyArmorAccuracyPenalty / 2f;
                            }

                            if (heavyArmorPerceptionPenalty < 0f)
                            {
                                bonuses.HeavyConditioningPerceptionBonus = -heavyArmorPerceptionPenalty / 2f;
                            }

                            if (heavyArmorStealthPenalty < 0f)
                            {
                                bonuses.HeavyConditioningStealthBonus = -heavyArmorStealthPenalty / 2f;
                            }
                        }

                        if (hasAksuSprint && aksuArmorPiecesEquipped >= 3 && aksuArmorSpeedBonus > 0f)
                        {
                            bonuses.AksuSprintSpeedBonus = aksuArmorSpeedBonus;
                        }

                        return bonuses;
                    }

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

                            if (drillBonuses.HasHeavyConditioningBonus)
                            {
                                bonusSpeed += drillBonuses.HeavyConditioningSpeedBonus;
                            }

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



                [HarmonyPatch(typeof(UIModuleCharacterProgression), nameof(UIModuleCharacterProgression.RefreshStatusesPanel))]
                private static class TFTV_UIModuleCharacterProgression_RefreshStatusesPanel_patch
                {
                    private static readonly GameTagDef ArmorTag = Shared.SharedGameTags.ArmorTag;
                    private static readonly GameTagDef HeavyClassTag = DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef");
                    private static readonly GameTagDef BionicTag = DefCache.GetDef<GameTagDef>("Bionic_TagDef");

                    private static bool Prefix(UIModuleCharacterProgression __instance, GeoCharacter ____character, List<ICommonItem> armorItems)
                    {
                        try
                        {
                            if (____character == null)
                                return false;

                            bool hasHeavyConditioning = ____character.Progression?.Abilities?
                                .Any(a => a != null && a.name != null && a.name.IndexOf("heavyconditioning", StringComparison.OrdinalIgnoreCase) >= 0) ?? false;

                            if (!hasHeavyConditioning)
                                return true; // let original handle it

                            if (!armorItems.All(i => i.ItemDef.Tags.Contains(ArmorTag) && i.ItemDef.Tags.Contains(HeavyClassTag) && !i.ItemDef.Tags.Contains(BionicTag)))
                                return true; // let original handle it

                            float fPerception = 0f;
                            float fAccuracy = 0f;
                            float fStealth = 0f;
                            float fPerceptionMult = 1f;
                            float fAccuracyMult = 1f;
                            float fStealthMult = 1f;

                            PerceptionComponentDef componentDef = ____character.TemplateDef.ComponentSetDef.GetComponentDef<PerceptionComponentDef>();
                            if (componentDef != null)
                            {
                                fPerception += componentDef.PerceptionRange;
                            }

                            foreach (ICommonItem armorItem in armorItems)
                            {
                                TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                                if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                                {
                                    if (tacticalItemDef.BodyPartAspectDef.Perception > 0)
                                    {
                                        fPerception += tacticalItemDef.BodyPartAspectDef.Perception;
                                    }
                                    else
                                    {
                                        fPerception += tacticalItemDef.BodyPartAspectDef.Perception / 2;
                                    }

                                    if (tacticalItemDef.BodyPartAspectDef.Accuracy < 0)
                                    {
                                        fAccuracy += tacticalItemDef.BodyPartAspectDef.Accuracy / 2;
                                    }
                                    else
                                    {
                                        fAccuracy += tacticalItemDef.BodyPartAspectDef.Accuracy;
                                    }
                                    if (tacticalItemDef.BodyPartAspectDef.Stealth < 0)
                                    {
                                        fStealth += tacticalItemDef.BodyPartAspectDef.Stealth / 2;
                                    }
                                    else
                                    {
                                        fStealth += tacticalItemDef.BodyPartAspectDef.Stealth;
                                    }
                                }
                            }

                            MethodInfo methodInfo = typeof(UIModuleCharacterProgression).GetMethod("ApplyStatModification", BindingFlags.NonPublic | BindingFlags.Static);

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
                                            ApplyStatModification(statModifier, ref fPerception, ref fAccuracy, ref fStealth, ref fPerceptionMult, ref fAccuracyMult, ref fStealthMult);
                                        }
                                    }
                                }
                            }

                            foreach (PassiveModifierAbilityDef passiveModifier in ____character.PassiveModifiers)
                            {
                                ItemStatModification[] statModifications = passiveModifier.StatModifications;
                                foreach (ItemStatModification statModifier2 in statModifications)
                                {
                                    ApplyStatModification(statModifier2, ref fPerception, ref fAccuracy, ref fStealth, ref fPerceptionMult, ref fAccuracyMult, ref fStealthMult);
                                }
                            }

                            int num = (int)(fPerception * fPerceptionMult);
                            __instance.PerceptionStatText.text = $"+{num}";
                            if (fAccuracy * fAccuracyMult == 0f)
                            {
                                __instance.AccuracyStatText.text = "---";
                            }
                            else
                            {
                                __instance.AccuracyStatText.text = UIUtil.PercentageStat(fAccuracy * fAccuracyMult, "{0}%");
                            }

                            if (fStealth * fStealthMult == 0f)
                            {
                                __instance.StealthStatText.text = "---";
                            }
                            else
                            {
                                __instance.StealthStatText.text = UIUtil.PercentageStat(fStealth * fStealthMult, "{0}%");
                            }

                            return false; // skip original since we've updated the UI
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            return true; // on error, fall back to original
                        }
                    }

                    private static void ApplyStatModification(ItemStatModification statModifier, ref float fPerception, ref float fAccuracy, ref float fStealth, ref float fPerceptionMult, ref float fAccuracyMult, ref float fStealthMult)
                    {
                        switch (statModifier.TargetStat)
                        {
                            case StatModificationTarget.Perception:
                                if (statModifier.Modification == StatModificationType.Add)
                                {
                                    fPerception += statModifier.Value;
                                }
                                else if (statModifier.Modification == StatModificationType.Multiply)
                                {
                                    fPerceptionMult += statModifier.Value;
                                }

                                break;
                            case StatModificationTarget.Accuracy:
                                if (statModifier.Modification == StatModificationType.Add)
                                {
                                    fAccuracy += statModifier.Value;
                                }
                                else if (statModifier.Modification == StatModificationType.Multiply)
                                {
                                    fAccuracyMult += statModifier.Value;
                                }

                                break;
                            case StatModificationTarget.Stealth:
                                if (statModifier.Modification == StatModificationType.Add)
                                {
                                    fStealth += statModifier.Value;
                                }
                                else if (statModifier.Modification == StatModificationType.Multiply)
                                {
                                    fStealthMult += statModifier.Value;
                                }

                                break;
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

                      //  TFTVLogger.Always($"{geoCharacter?.DisplayName} has hc? {geoCharacter?.Progression?.Abilities?.Contains(TFTVDrills.DrillsDefs._heavyConditioning)}");

                        if (!data.Abilities.Any(ad => ad.Ability == TFTVDrills.DrillsDefs._heavyConditioning)) return;

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

                        // Highlight perception if the soldier exceeds a threshold.
                        if (fPerception<0)
                        {
                            __instance.PerceptionText.text = $"{data.Perception-fPerception+fPerception/2}";
                        }

                        // Accentuate accuracy for elite marksmen.
                        if (fAccuracy<0)
                        {
                            __instance.AccuracyText.text = $"{data.Accuracy-fAccuracy+fAccuracy/2}% ";
                        }

                        // Flag units that are exceptionally stealthy.
                        if (fStealth<0)
                        {
                            __instance.StealthText.text = $"{data.Stealth-fStealth+fStealth/2}%";
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
                private static readonly string armourItemsString = "ArmourItems";
                private static readonly string equipmentItemsString = "EquipmentItems";
                private static readonly string inventoryItemsString = "InventoryItems";

                //  private static bool _mutationBionicsShaded = false;

                private static void ShadeMutationBionics(UIModuleActorCycle uIModuleActorCycle)
                {
                    try
                    {
                        GeoCharacter geoCharacter = uIModuleActorCycle.CurrentCharacter;

                        if (geoCharacter == null) //|| geoCharacter.TemplateDef == null || !geoCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag) && !_mutationBionicsShaded)
                        {
                            return;
                        }

                        PhoenixGeneralButton mutationButton = uIModuleActorCycle.EditUnitButtonsController.MutationButton;
                        PhoenixGeneralButton bionicsButton = uIModuleActorCycle.EditUnitButtonsController.BionicsButton;

                        FieldInfo mutationAvailableFieldInfo = typeof(EditUnitButtonsController).GetField("_mutationAvailable", BindingFlags.NonPublic | BindingFlags.Instance);
                        FieldInfo bionicsAvailableFieldInfo = typeof(EditUnitButtonsController).GetField("_bionicsAvailable", BindingFlags.NonPublic | BindingFlags.Instance);

                        bool mutationAvailable = (bool)mutationAvailableFieldInfo.GetValue(uIModuleActorCycle.EditUnitButtonsController);
                        bool bionicsAvailable = (bool)bionicsAvailableFieldInfo.GetValue(uIModuleActorCycle.EditUnitButtonsController);

                        if (!mutationAvailable && !bionicsAvailable)
                        {
                            return;
                        }

                        Text bionicsText = null;
                        Text mutateText = null;

                        if (bionicsAvailable)
                        {
                            bionicsText = uIModuleActorCycle.EditUnitButtonsController.GetComponentsInChildren<Text>().FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_AUMGENTATION_ACTION"));

                            if (bionicsText != null)
                            {

                                bionicsButton.SetInteractable(true);
                                if (bionicsButton.gameObject.GetComponent<UITooltipText>() != null)
                                {
                                    bionicsButton.gameObject.GetComponent<UITooltipText>().enabled = false;
                                }

                                bionicsText.color = new Color(0.820f, 0.859f, 0.914f);

                            }


                        }

                        if (mutationAvailable)
                        {
                            mutateText = uIModuleActorCycle.EditUnitButtonsController.GetComponentsInChildren<Text>().FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_GEOSCAPE_MUTATE"));

                            if (mutateText != null)
                            {
                                mutationButton.SetInteractable(true);

                                if (mutationButton.gameObject.GetComponent<UITooltipText>() != null)
                                {
                                    mutationButton.gameObject.GetComponent<UITooltipText>().enabled = false;
                                }

                                mutateText.color = new Color(0.820f, 0.859f, 0.914f);
                            }

                        }

                        TFTVConfig config = TFTVMain.Main.Config;

                        if (geoCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag) && !config.MercsCanBeAugmented)
                        {
                            // _mutationBionicsShaded = true;

                            if (mutateText != null)
                            {
                                mutationButton.SetInteractable(false);

                                if (mutationButton.gameObject.GetComponent<UITooltipText>() != null)
                                {
                                    mutationButton.gameObject.GetComponent<UITooltipText>().enabled = true;

                                }
                                else
                                {
                                    mutationButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_ABILITY_NOAUGMENTATONS");
                                }
                                mutateText.color = Color.gray;

                            }

                            if (bionicsText != null)
                            {
                                bionicsButton.SetInteractable(false);

                                if (bionicsButton.gameObject.GetComponent<UITooltipText>() != null)
                                {
                                    bionicsButton.gameObject.GetComponent<UITooltipText>().enabled = true;

                                }
                                else
                                {
                                    bionicsButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_ABILITY_NOAUGMENTATONS");
                                }
                                bionicsText.color = Color.gray;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }


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

                                        ShadeMutationBionics(____parentModule);

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
                                    case UIModuleActorCycle.ActorCycleState.RecruitSection:
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
                                    case UIModuleActorCycle.ActorCycleState.Memorial:
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

                }


                private static bool _equipAllRunning = false;

                [HarmonyPatch(typeof(UIInventoryList), "GetTotalUsedStorage")]
                internal static class TFTV_UIInventoryList_GetTotalUsedStorage_patch
                {

                    public static void Postfix(UIInventoryList __instance, ref int __result)
                    {
                        try
                        {
                            if (_equipAllRunning)
                            {
                                __result = 0;
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }



                /*   [HarmonyPatch(typeof(UIInventoryList), "CanAcceptItem", new Type[] { typeof(ItemDef) })]
                   internal static class TFTV_UIInventoryList_CanAcceptItem_patch
                   {

                       public static bool Prefix(UIInventoryList __instance, ItemDef item, ref bool __result, bool ____isFiltering)
                       {
                           try
                           {
                               if (____isFiltering)
                               {
                                   TFTVLogger.Always($"{item.name} is filtering, returning true");
                                   __result = true;
                                   return false;
                               }

                               MethodInfo methodInfoCreateFilterData = typeof(UIInventoryList).GetMethod("CreateFilterData", BindingFlags.Instance | BindingFlags.NonPublic);

                               FilterData filterData = (FilterData)methodInfoCreateFilterData.Invoke(__instance, new object[] { item, null }); // __instance.CreateFilterData(item);

                               TFTVLogger.Always($"filterData.AddedItem: {filterData?.AddedItem}, " +
                                   $"filterData:RemovedItem: {filterData?.RemovedItem}, " +
                                   $"filterData.CurrentStorageUsed: {filterData?.CurrentStorageUsed}" +
                                   $"filterData?.CurrentItems?.Count(): {filterData?.CurrentItems?.Count()}");

                               if (!(__instance.InventoryListFilter == null) && !__instance.InventoryListFilter.CanAddItem(filterData, __instance.currentInventorySlotsBonus))
                               {
                                   __result = __instance.InventoryListFilter.CanSwapItems(filterData, __instance.currentInventorySlotsBonus);
                                   TFTVLogger.Always($"{item.name} {__result}");
                                   return false;
                               }
                               __result = true;
                               return false;

                           }
                           catch (Exception e)
                           {
                               TFTVLogger.Error(e);
                               throw;
                           }
                       }

                   }*/





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


                /// <summary>
                /// 1) cycle through the squad and hit loadoutbuttonclicked with showWarning false, and equipAllInvoked false,
                /// but add a new parameter making a list of missing things.
                /// 2) if _allMissingEquipment bigger than 0,
                /// cycle through all the other operatives, ordering by low stamina first, and make them drop the required items
                /// 3) cycle through the squad and load their loadouts, with showWarning true, and equipAllInvoked true,
                /// </summary>
                /// <param name="squad"></param>

                private static List<string> _allMissingEquipment = new List<string>();



                public static void EquipAll(List<GeoCharacter> squad)
                {
                    try
                    {
                        _equipAllRunning = true;

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        UIModuleActorCycle uIModuleActorCycle = controller.View.GeoscapeModules.ActorCycleModule;

                        UIModuleSoldierEquip uIModuleSoldierEquip = controller.View.GeoscapeModules.SoldierEquipModule;

                        MethodInfo methodInfo = typeof(UIModuleActorCycle).GetMethod("SelectSoldier", BindingFlags.Public | BindingFlags.Instance);

                        controller.View.ToEditUnitState(squad.First());

                        foreach (GeoCharacter geoCharacter in squad)
                        {
                            if (CharacterLoadouts.ContainsKey(geoCharacter.Id))
                            {
                                // TFTVLogger.Always($"first pass on {geoCharacter.DisplayName}");
                                object[] parameters = new object[] { geoCharacter, false };
                                methodInfo.Invoke(uIModuleActorCycle, parameters);
                                LoadLoadoutButtonClicked(false, false, true);
                            }
                        }

                        FieldInfo fieldInfo = typeof(GeoscapeView).GetField("_statesStack", BindingFlags.NonPublic | BindingFlags.Instance);
                        StateStack<GeoscapeViewContext> stateStack = (StateStack<GeoscapeViewContext>)fieldInfo.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);


                        if (_allMissingEquipment.Count == 0)     //All got equipped on the first pass! ending
                        {
                            //  TFTVLogger.Always($"and got here");
                            stateStack.SwitchToPreviousState();
                            _equipAllRunning = false;
                            return;
                        }



                        //Let's see if someone else has the stuff!

                        List<GeoCharacter> otherOperatives = controller.PhoenixFaction.Soldiers.Where
                            (s => !squad.Contains(s) &&
                            s.Fatigue != null).ToList();

                        otherOperatives = otherOperatives.OrderBy(o => o.Fatigue.Stamina.Value.EndValueInt).ToList();

                        foreach (GeoCharacter geoCharacter1 in otherOperatives)
                        {
                            // TFTVLogger.Always($"looking for missing equipment on {geoCharacter1.DisplayName}");
                            object[] parameters = new object[] { geoCharacter1, false };
                            methodInfo.Invoke(uIModuleActorCycle, parameters);
                            RemoveItemsFromCharacter(uIModuleSoldierEquip);

                            if (_allMissingEquipment.Count == 0)
                            {
                                break;
                            }
                        }

                        foreach (GeoCharacter geoCharacter in squad)
                        {
                            if (CharacterLoadouts.ContainsKey(geoCharacter.Id))
                            {
                                // TFTVLogger.Always($"second pass on {geoCharacter.DisplayName}");
                                object[] parameters = new object[] { geoCharacter, false };
                                methodInfo.Invoke(uIModuleActorCycle, parameters);
                                LoadLoadoutButtonClicked(true, true, false);
                            }
                        }

                        stateStack.SwitchToPreviousState();
                        _equipAllRunning = false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void RemoveItemsFromCharacter(UIModuleSoldierEquip uIModuleSoldierEquip)
                {
                    try
                    {
                        List<string> itemTypes = new List<string>() { armourItemsString, equipmentItemsString, inventoryItemsString };

                        List<UIInventoryList> inventoryLists = new List<UIInventoryList>()
                        {
                            uIModuleSoldierEquip.InventoryList,
                            uIModuleSoldierEquip.ArmorList,
                            uIModuleSoldierEquip.ReadyList
                        };

                        for (int x = 0; x < 3; x++)
                        {
                            foreach (UIInventorySlot slot in inventoryLists[x].Slots)
                            {
                                GeoItem item = (GeoItem)slot.Item;

                                if (item != null && _allMissingEquipment.Contains(item.ItemDef.Guid))
                                {
                                    // TFTVLogger.Always($"{item} should be removed");
                                    inventoryLists[x].RemoveItem(item, slot);
                                    // TFTVLogger.Always($"{item} got here 0 ");
                                    _allMissingEquipment.Remove(item.ItemDef.Guid);
                                    //  TFTVLogger.Always($"{item} got here 1");
                                    uIModuleSoldierEquip.StorageList.AddItem(item);
                                    //  TFTVLogger.Always($"{item} got here 2");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddItemsToCharacter(Dictionary<string, List<string>> itemsForCharacter, GeoCharacter character,
    UIModuleSoldierEquip uIModuleSoldierEquip, ref List<string> missingItems, bool recordMissingItems)
                {
                    try
                    {
                        AddItemsOfType(itemsForCharacter[armourItemsString], uIModuleSoldierEquip.StorageList, uIModuleSoldierEquip.ArmorList, ref missingItems, recordMissingItems);
                        AddItemsOfType(itemsForCharacter[equipmentItemsString], uIModuleSoldierEquip.StorageList, uIModuleSoldierEquip.ReadyList, ref missingItems, recordMissingItems);
                        AddItemsOfType(itemsForCharacter[inventoryItemsString], uIModuleSoldierEquip.StorageList, uIModuleSoldierEquip.InventoryList, ref missingItems, recordMissingItems);

                        if (!recordMissingItems)
                        {
                            _allMissingEquipment.Clear();
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddItemsOfType(List<string> itemGuids, UIInventoryList storage, UIInventoryList itemList,
                    ref List<string> missingItems, bool recordMissingItems)
                {
                    foreach (string guid in itemGuids)
                    {
                        ICommonItem item = storage.UnfilteredItems.Concat(storage.FilteredItems)
                            .FirstOrDefault(ufi => ufi.ItemDef.Guid == guid);

                        if (item == null)
                        {
                            missingItems.Add(guid);
                            if (recordMissingItems)
                            {
                                _allMissingEquipment.Add(guid);
                            }
                            continue;
                        }

                        for (int x = 0; x < itemList.Slots.Count(); x++)
                        {
                            if (itemList.Slots[x].Item != null)
                            {
                                continue;
                            }

                            if (itemList.CanAddItem(item, itemList.Slots[x]))
                            {
                                itemList.AddItem(item.GetSingleItem(), itemList.Slots[x], storage);
                                storage.RemoveItem(item.GetSingleItem(), null);
                                break;
                            }
                        }
                    }
                }


                private static void LoadLoadoutButtonClicked(bool showWarning = true, bool equipAllInvoked = false, bool recordMissingItems = false)
                {
                    try
                    {

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        UIModuleSoldierEquip uIModuleSoldierEquip = controller.View.GeoscapeModules.SoldierEquipModule;

                        GeoCharacter character = controller.View.GeoscapeModules.ActorCycleModule.CurrentCharacter;// hookToCharacter;

                        Dictionary<string, List<string>> itemsForCharacter = TryGetMissingLoadout(character, uIModuleSoldierEquip);

                        if (!CharacterLoadouts.ContainsKey(character.Id) || itemsForCharacter == null)
                        {
                            return;
                        }

                        List<string> missingItems = new List<string>();

                        AddItemsToCharacter(itemsForCharacter, character, uIModuleSoldierEquip, ref missingItems, recordMissingItems);

                        if (!showWarning)
                        {
                            return;
                        }

                        TryReplenish(missingItems, character, uIModuleSoldierEquip, controller, equipAllInvoked);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static string CreateMessageMissingItemsAndAmmo(List<string> missingItems, GeoCharacter character,
                    UIModuleSoldierEquip uIModuleSoldierEquip, List<ICommonItem> itemsMissingAmmo, ref List<string> missingInstantItems, ref ResourcePack totalCost)
                {
                    try
                    {
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        string message = $"{TFTVCommonMethods.ConvertKeyToString("KEY_UI_MISSING_LOADOUT_ITEMS_TFTV")}"; // $"Insufficient stocks to equip {} with:\n";
                        message = message.Replace("{0}", character.DisplayName);

                        Dictionary<string, int> instantManufactureItems = new Dictionary<string, int>();
                        Dictionary<string, int> otherItems = new Dictionary<string, int>();

                        string instantItemList = "";
                        string otherItemList = "";

                        for (int x = 0; x < missingItems.Count; x++)
                        {
                            TacticalItemDef itemDef = (TacticalItemDef)Repo.GetDef(missingItems[x]);
                            string itemName = itemDef.GetDisplayName().Localize();

                            if (itemDef.ManufacturePointsCost == 0 && phoenixFaction.Manufacture.Contains(itemDef))
                            {
                                // TFTVLogger.Always($"adding missing instant manufacture item {itemDef}");
                                missingInstantItems.Add(itemDef.Guid);

                                if (instantManufactureItems.ContainsKey(itemName))
                                {
                                    instantManufactureItems[itemName] += 1;
                                }
                                else
                                {
                                    instantManufactureItems.Add(itemName, 1);
                                }
                                // instantItemList += $"\n-{itemDef.GetDisplayName().Localize()}";
                            }
                            else
                            {
                                if (otherItems.ContainsKey(itemName))
                                {
                                    otherItems[itemName] += 1;
                                }
                                else
                                {
                                    otherItems.Add(itemName, 1);
                                }
                                //message += $"\n-{itemDef.GetDisplayName().Localize()}";
                            }

                        }

                        for (int x = 0; x < itemsMissingAmmo.Count; x++)
                        {
                            ICommonItem item = itemsMissingAmmo[x];

                            string itemName = "";
                            if (item.ItemDef.CompatibleAmmunition.Length > 0)
                            {
                                itemName = item.ItemDef.CompatibleAmmunition[0].GetDisplayName().Localize();
                            }
                            else
                            {
                                itemName = item.ItemDef.GetDisplayName().Localize();
                            }

                            if (phoenixFaction.Manufacture.Contains(item.ItemDef))
                            {
                                if (instantManufactureItems.ContainsKey(itemName))
                                {
                                    instantManufactureItems[itemName] += 1;
                                }
                                else
                                {
                                    instantManufactureItems.Add(itemName, 1);
                                }
                            }
                            else
                            {
                                if (otherItems.ContainsKey(itemName))
                                {
                                    otherItems[itemName] += 1;
                                }
                                else
                                {
                                    otherItems.Add(itemName, 1);
                                }
                            }

                            //  
                            // missingItems.Add(item.ItemDef.Guid);
                        }

                        foreach (string instantItem in instantManufactureItems.Keys)
                        {
                            instantItemList += $"\n-{instantManufactureItems[instantItem]} {instantItem}";
                        }

                        foreach (string otherItem in otherItems.Keys)
                        {
                            otherItemList += $"\n-{otherItems[otherItem]} {otherItem}";
                        }

                        message += otherItemList;
                        message += instantItemList;

                        if (instantManufactureItems.Keys.Count > 0)
                        {
                            List<ResourcePack> costs = new List<ResourcePack>();

                            foreach (string item in missingInstantItems)
                            {
                                ItemDef itemDef = (ItemDef)Repo.GetDef(item);
                                if (phoenixFaction.Manufacture.Contains(itemDef))
                                {
                                    costs.Add(itemDef.ManufacturePrice);
                                }
                            }

                            if (itemsMissingAmmo.Count > 0)
                            {
                                costs.AddRange(GetCostOfReloadingWeapons(uIModuleSoldierEquip));
                            }

                            totalCost = GetTotalCost(costs);

                            message += $"\n\n{TFTVCommonMethods.ConvertKeyToString("KEY_UI_REPLENISH_CONSUMABLES_TFTV")}";
                            if (otherItemList == "")
                            {
                                message = message.Replace("{0}", "");
                            }
                            else
                            {
                                message = message.Replace("{0}", instantItemList + $"\n");
                            }
                        }

                        return message;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static bool CheckItemEligibleForManufacture(ItemDef itemDef)
                {
                    try
                    {
                        GameTagDef manufacturableTag = Shared.SharedGameTags.ManufacturableTag;
                        GeoPhoenixFaction geoPhoenix = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        if (!itemDef.Tags.Contains(manufacturableTag))
                        {
                            return false;
                        }
                        if (!geoPhoenix.Manufacture.Contains(itemDef))
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


                private static void TryReplenish(List<string> missingItems, GeoCharacter character,
                    UIModuleSoldierEquip uIModuleSoldierEquip, GeoLevelController controller, bool equipAllInvoked = false)
                {

                    try
                    {
                        FieldInfo fieldInfo = typeof(GeoscapeView).GetField("_statesStack", BindingFlags.NonPublic | BindingFlags.Instance);
                        StateStack<GeoscapeViewContext> stateStack = (StateStack<GeoscapeViewContext>)fieldInfo.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);

                        List<ICommonItem> itemsMissingAmmo = new List<ICommonItem>();

                        itemsMissingAmmo.AddRange(uIModuleSoldierEquip.ReadyList.UnfilteredItems.Where(i => i.CommonItemData.CurrentCharges < i.ItemDef.ChargesMax && CheckItemEligibleForManufacture(i.ItemDef)));
                        itemsMissingAmmo.AddRange(uIModuleSoldierEquip.ArmorList.UnfilteredItems.Where(i => i.CommonItemData.CurrentCharges < i.ItemDef.ChargesMax && CheckItemEligibleForManufacture(i.ItemDef)));
                        itemsMissingAmmo.AddRange(uIModuleSoldierEquip.InventoryList.UnfilteredItems.Where(i => i.CommonItemData.CurrentCharges < i.ItemDef.ChargesMax && CheckItemEligibleForManufacture(i.ItemDef)));

                        List<string> missingInstantItems = new List<string>();

                        if (missingItems.Count == 0 && itemsMissingAmmo.Count == 0)
                        {
                            return;
                        }

                        ResourcePack totalCost = new ResourcePack();
                        string message = CreateMessageMissingItemsAndAmmo(missingItems, character, uIModuleSoldierEquip, itemsMissingAmmo, ref missingInstantItems, ref totalCost);

                        if (missingInstantItems.Count > 0 || itemsMissingAmmo.Count > 0)
                        {
                            if (uIModuleSoldierEquip.ModuleData.Wallet.HasResources(totalCost))
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(message.Replace("{1}", GetTotalPriceText(totalCost)), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnMissingEquipmentCallback));
                            }

                            void OnMissingEquipmentCallback(MessageBoxCallbackResult msgResult)
                            {
                                if (msgResult.DialogResult == MessageBoxResult.Yes)
                                {
                                    if (equipAllInvoked)
                                    {
                                        controller.View.ToEditUnitState(character);
                                    }

                                    ReloadWeapons(uIModuleSoldierEquip, totalCost);
                                    ManufactureMissingInstantItems(uIModuleSoldierEquip, missingInstantItems);


                                    if (equipAllInvoked)
                                    {
                                        stateStack.SwitchToPreviousState();
                                    }
                                }
                            }
                        }
                        else
                        {
                            GameUtl.GetMessageBox().ShowSimplePrompt(message, MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private static ResourcePack GetTotalCost(List<ResourcePack> prices)
                {
                    try
                    {

                        float matValue = 0;
                        float techValue = 0;
                        float suppliesValue = 0;
                        float mutagenValue = 0;

                        foreach (ResourcePack p in prices)
                        {
                            foreach (ResourceUnit resourceUnit in p)
                            {
                                ResourceType type = resourceUnit.Type;
                                switch (type)
                                {
                                    case ResourceType.Supplies:
                                        suppliesValue += resourceUnit.Value;
                                        break;
                                    case ResourceType.Materials:
                                        matValue += resourceUnit.Value;
                                        break;
                                    case ResourceType.Tech:
                                        techValue += resourceUnit.Value;
                                        break;
                                    case ResourceType.Mutagen:
                                        mutagenValue += resourceUnit.Value;
                                        break;
                                }
                            }
                        }


                        ResourcePack price = new ResourcePack() {
                            new ResourceUnit {Type= ResourceType.Materials, Value = matValue},
                            new ResourceUnit {Type = ResourceType.Tech, Value = techValue },
                            new ResourceUnit {Type = ResourceType.Supplies, Value = suppliesValue},
                            new ResourceUnit {Type = ResourceType.Mutagen, Value = mutagenValue},
                        };

                        return price;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static string GetTotalPriceText(ResourcePack price)
                {
                    try
                    {
                        UIModuleGeoscapeScreenUtils utilsModule = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeoscapeScreenUtilsModule;

                        string message = " ";

                        foreach (ResourceUnit resourceUnit in price)
                        {
                            if (resourceUnit.RoundedValue > 0)
                            {
                                string resourcesInfo = "";
                                ResourceType type = resourceUnit.Type;
                                switch (type)
                                {
                                    case ResourceType.Supplies:
                                        resourcesInfo = utilsModule.ScrapSuppliesResources.Localize(null);
                                        break;
                                    case ResourceType.Materials:
                                        resourcesInfo = utilsModule.ScrapMaterialsResources.Localize(null);
                                        break;
                                    case ResourceType.Tech:
                                        resourcesInfo = utilsModule.ScrapTechResources.Localize(null);
                                        break;
                                    case ResourceType.Mutagen:
                                        resourcesInfo = utilsModule.ScrapMutagenResources.Localize(null);
                                        break;
                                }
                                resourcesInfo = resourcesInfo.Replace("{0}", resourceUnit.RoundedValue.ToString());

                                TFTVLogger.Always($"{resourcesInfo}");

                                message += resourcesInfo;
                            }
                        }

                        return message;

                    }


                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void ReloadWeapon(GeoItem item)
                {
                    try
                    {
                        if (item != null && item.ItemDef.ChargesMax > 0 && item.CommonItemData.CurrentCharges < item.ItemDef.ChargesMax)
                        {
                            TFTVLogger.Always($"Reloading {item} {item.CommonItemData.CurrentCharges} {item.ItemDef.ChargesMax}");

                            item.ReloadForFree();

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static ResourcePack WeaponCost(GeoItem item)
                {
                    try
                    {
                        // TFTVLogger.Always($"Checking WeaponCost of {item} with charges max {item?.ItemDef?.ChargesMax} and current charges {item?.CommonItemData?.CurrentCharges}");

                        if (item != null && item.ItemDef.ChargesMax > 0 && item.CommonItemData.CurrentCharges < item.ItemDef.ChargesMax)
                        {
                            float ratio = item.CommonItemData.CurrentCharges / item.ItemDef.ChargesMax;
                            ResourcePack cost = new ResourcePack();
                            if (item.ItemDef.CompatibleAmmunition.FirstOrDefault() != null)
                            {
                                cost = item.ItemDef.CompatibleAmmunition.FirstOrDefault().ManufacturePrice * (1 - ratio);
                            }
                            else
                            {
                                cost = item.ItemDef.ManufacturePrice * (1 - ratio);
                            }

                            return cost;

                        }
                        return null;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static List<ResourcePack> GetCostOfReloadingWeapons(UIModuleSoldierEquip uIModuleSoldierEquip)
                {
                    try
                    {
                        List<ResourcePack> prices = new List<ResourcePack>();

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.ArmorList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ResourcePack cost = WeaponCost(item);
                            if (cost != null && phoenixFaction.Manufacture.Contains(item.ItemDef))
                            {
                                prices.Add(cost);
                            }
                        }

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.ReadyList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ResourcePack cost = WeaponCost(item);
                            if (cost != null && phoenixFaction.Manufacture.Contains(item.ItemDef))
                            {
                                prices.Add(cost);
                            }
                        }

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.InventoryList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ResourcePack cost = WeaponCost(item);
                            if (cost != null && phoenixFaction.Manufacture.Contains(item.ItemDef))
                            {
                                prices.Add(cost);
                            }
                        }
                        return prices;

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void ReloadWeapons(UIModuleSoldierEquip uIModuleSoldierEquip, ResourcePack price)
                {
                    try

                    {
                        Wallet wallet = uIModuleSoldierEquip.ModuleData.Wallet;

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.ArmorList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ReloadWeapon(item);
                            uIInventorySlot.UpdateItem();
                        }

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.ReadyList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ReloadWeapon(item);
                            uIInventorySlot.UpdateItem();
                        }

                        foreach (UIInventorySlot uIInventorySlot in uIModuleSoldierEquip.InventoryList.Slots)
                        {
                            GeoItem item = (GeoItem)uIInventorySlot.Item;
                            ReloadWeapon(item);
                            uIInventorySlot.UpdateItem();
                        }

                        wallet.Take(price, OperationReason.Purchase);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ManufactureMissingInstantItems(UIModuleSoldierEquip uIModuleSoldierEquip, List<string> items)
                {
                    try
                    {

                        foreach (string item in items)
                        {
                            ItemDef itemDef = (ItemDef)Repo.GetDef(item);

                            if (itemDef.CompatibleAmmunition.FirstOrDefault() != null)
                            {
                                itemDef = itemDef.CompatibleAmmunition.FirstOrDefault();
                            }

                            if (itemDef.ManufacturePointsCost != 0)
                            {
                                continue;
                            }

                            GeoItem geoItem = new GeoItem(itemDef);

                            uIModuleSoldierEquip.StorageList.AddItem(geoItem);
                        }

                        LoadLoadoutButtonClicked(false);
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static bool CheckEnoughStoresToReceiveWeight(Dictionary<string, List<GeoItem>> items)
                {
                    try
                    {
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                        int storageCapacity = phoenixFaction.GetTotalAvailableStorage();
                        int storageUsed = phoenixFaction.ItemStorage.GetStorageUsed();

                        int totalWeight = 0;

                        foreach (string list in items.Keys)
                        {
                            foreach (GeoItem geoItem in items[list])
                            {
                                totalWeight += geoItem.ItemDef.Weight;
                            }
                        }

                        if (totalWeight + storageUsed > storageCapacity)
                        {
                            string warning = TFTVCommonMethods.ConvertKeyToString("KEY_WARNING_STORAGE_EXCEEDED");

                            GameUtl.GetMessageBox().ShowSimplePrompt(warning, MessageBoxIcon.Stop, MessageBoxButtons.OK, null);
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

                private static bool TransferItemsToStore(Dictionary<string, List<GeoItem>> items, UIModuleSoldierEquip uIModuleSoldierEquip)
                {
                    try
                    {

                        if (items.Count == 0)
                        {
                            return true;
                        }

                        if (!CheckEnoughStoresToReceiveWeight(items))
                        {
                            return false;
                        }

                        foreach (string list in items.Keys)
                        {
                            foreach (GeoItem item in items[list])
                            {
                                if (item.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("MechArm"))
                                {
                                    continue;
                                }

                                if (list == inventoryItemsString)
                                {
                                    //  TFTVLogger.Always($"removing from character {item.ItemDef.name}, 0");
                                    uIModuleSoldierEquip.InventoryList.RemoveItem(item, null);
                                }
                                else if (list == equipmentItemsString)
                                {
                                    //  TFTVLogger.Always($"removing from character {item.ItemDef.name}, 1");
                                    uIModuleSoldierEquip.ReadyList.RemoveItem(item, null);
                                }
                                else
                                {
                                    //  TFTVLogger.Always($"removing from character {item.ItemDef.name}, 2");
                                    uIModuleSoldierEquip.ArmorList.RemoveItem(item, null);
                                }

                                // TFTVLogger.Always($"transferring {item.ItemDef.name}");
                                uIModuleSoldierEquip.StorageList.AddItem(item);
                            }

                        }

                        return true;

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static Dictionary<string, List<string>> TryGetMissingLoadout(GeoCharacter geoCharacter, UIModuleSoldierEquip uIModuleSoldierEquip)
                {
                    try
                    {
                        if (CharacterLoadouts == null || !CharacterLoadouts.ContainsKey(geoCharacter.Id))
                        {
                            return null;

                        }

                        Dictionary<string, List<string>> currentItems = GetCharacterItems(geoCharacter);

                        Dictionary<string, List<string>> characterLoadout = CharacterLoadouts[geoCharacter.Id];

                        Dictionary<string, List<string>> missingLoadout = new Dictionary<string, List<string>>
                    {
                        { armourItemsString, new List<string>() },
                        { equipmentItemsString, new List<string>() },
                        { inventoryItemsString, new List<string>() }
                            };

                        Dictionary<string, List<GeoItem>> characterItems = GetCharacterGeoItemList(geoCharacter, uIModuleSoldierEquip);

                        List<string> itemTypes = new List<string>() { armourItemsString, equipmentItemsString, inventoryItemsString };

                        Dictionary<string, List<GeoItem>> itemsToDrop = new Dictionary<string, List<GeoItem>>
                            {
                                { armourItemsString, new List<GeoItem>() },
                                { equipmentItemsString, new List<GeoItem>() },
                                { inventoryItemsString, new List<GeoItem>() }
                            };

                        foreach (string list in itemTypes)
                        {
                            if (currentItems.ContainsKey(list))
                            {
                                foreach (string item in currentItems[list])
                                {
                                    if (!characterLoadout[list].Contains(item))
                                    {
                                        itemsToDrop[list].Add(characterItems[list].FirstOrDefault(i => i.ItemDef.Guid == item));
                                    }
                                }
                            }
                            if (characterLoadout.ContainsKey(list))
                            {
                                foreach (string item in characterLoadout[list])
                                {
                                    if (currentItems.ContainsKey(list) && currentItems[list].Contains(item))
                                    {
                                        continue;
                                    }

                                    missingLoadout[list].Add(item);
                                }
                            }
                        }

                        if (TransferItemsToStore(itemsToDrop, uIModuleSoldierEquip))
                        {
                            return missingLoadout;
                        }

                        return null;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static Dictionary<string, List<string>> GetCharacterItems(GeoCharacter character)
                {
                    try
                    {
                        Dictionary<string, List<string>> characterItems = new Dictionary<string, List<string>>
                    {
                        { armourItemsString, new List<string>() },
                        { equipmentItemsString, new List<string>() },
                        { inventoryItemsString, new List<string>() }
                            };

                        foreach (GeoItem armourPiece in character.ArmourItems.Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                        {
                            characterItems[armourItemsString].Add(armourPiece.ItemDef.Guid);
                        }
                        foreach (GeoItem equipmentPiece in character.EquipmentItems)
                        {
                            characterItems[equipmentItemsString].Add(equipmentPiece.ItemDef.Guid);
                        }
                        foreach (GeoItem inventoryPiece in character.InventoryItems)
                        {
                            characterItems[inventoryItemsString].Add(inventoryPiece.ItemDef.Guid);
                        }

                        return characterItems;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static Dictionary<string, List<GeoItem>> GetCharacterGeoItemList(GeoCharacter character, UIModuleSoldierEquip uIModuleSoldierEquip)
                {
                    try
                    {
                        Dictionary<string, List<GeoItem>> characterItems = new Dictionary<string, List<GeoItem>>
                    {
                        { armourItemsString, new List<GeoItem>() },
                        { equipmentItemsString, new List<GeoItem>() },
                        { inventoryItemsString, new List<GeoItem>() }
                            };

                        foreach (GeoItem armourPiece in character.ArmourItems.Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                        {
                            characterItems[armourItemsString].Add(armourPiece);
                        }

                        foreach (GeoItem equipmentPiece in character.EquipmentItems)
                        {
                            characterItems[equipmentItemsString].Add(equipmentPiece);

                        }


                        foreach (GeoItem inventoryPiece in character.InventoryItems)
                        {
                            characterItems[inventoryItemsString].Add(inventoryPiece);
                        }


                        return characterItems;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private static void SaveLoadoutButtonClicked()
                {
                    try
                    {
                        GeoCharacter character = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;//hookToCharacter;

                        Dictionary<string, List<string>> characterItems = GetCharacterItems(character);

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

                public static void EquipBestCurrentTeam(GeoSite geoSite, UIModuleGeneralPersonelRoster uIModuleGeoRoster)
                {
                    try
                    {
                        TFTVLogger.Always($"EquipBestCurrentTeam running");

                        GeoPhoenixFaction phoenixFaction = geoSite.GeoLevel.PhoenixFaction;

                        List<GeoCharacter> charactersOnSite = new List<GeoCharacter>();

                        List<GeoRosterDeploymentItem> deploymentItems = (from g in uIModuleGeoRoster.Slots
                                                                         where g.gameObject.activeSelf
                                                                         select g into s
                                                                         select s.GetComponent<GeoRosterDeploymentItem>()).ToList();

                        foreach (GeoRosterDeploymentItem geoRosterItem in deploymentItems)
                        {
                            TFTVLogger.Always($"{geoRosterItem.Character.DisplayName}");

                            if (geoRosterItem.EnrollForDeployment)
                            {
                                TFTVLogger.Always($"{geoRosterItem.Character.DisplayName}");
                                charactersOnSite.Add(geoRosterItem.Character);
                            }
                        }

                        if (charactersOnSite != null && charactersOnSite.Count > 0)
                        {
                            GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.ToEditUnitState(charactersOnSite.FirstOrDefault());
                        }
                        else
                        {
                            return;
                        }

                        EquipAll(charactersOnSite);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void UnequipButtonClicked(bool droopAttachmentsSeparately = false)
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
                                            //  TFTVLogger.Always($"{geoItem.ItemDef} can go on {bionic.ItemDef}");
                                            attachments.Add(geoItem);

                                        }
                                    }
                                }
                            }

                            if (droopAttachmentsSeparately)
                            {
                                armorItems.AddRange(character.ArmourItems.
                                   Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                   Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)));

                            }
                            else
                            {
                                armorItems.AddRange(character.ArmourItems.
                                    Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                                    Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)).
                                    Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("Attachment")).
                                    Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("BackPack"))//.
                                                                                                                      // Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("LegsAttachment"))
                                                                                                                      //  Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("MechArm"))
                                    );
                            }

                            equipmentItems.AddRange(character.EquipmentItems);
                            inventoryItems.AddRange(character.InventoryItems);
                            armorItems.AddRange(attachments);

                            Dictionary<string, List<GeoItem>> allItems = new Dictionary<string, List<GeoItem>>
                                {
                                    { armourItemsString, armorItems },
                                    { inventoryItemsString, inventoryItems },
                                    { equipmentItemsString, equipmentItems }
                                };

                            TransferItemsToStore(allItems, uIModuleSoldierEquip);
                        }


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

            }

        }

        internal class MissionDeployment
        {
            private static void CreateBestEquipmentButton(GeoSite geoSite)
            {
                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    UIModuleDeploymentMissionBriefing uIModuleDeploymentMissionBriefing = controller.View.GeoscapeModules.DeploymentMissionBriefingModule;

                    UIModuleGeneralPersonelRoster uIModuleGeoRoster = controller.View.GeoscapeModules.GeneralPersonelRosterModule;

                    if (uIModuleDeploymentMissionBriefing.transform.GetComponentsInChildren<PhoenixGeneralButton>().FirstOrDefault(b => b.name == "EquipAllButton") != null)

                    {
                        PhoenixGeneralButton phoenixGeneralButton = uIModuleDeploymentMissionBriefing.transform.GetComponentsInChildren<PhoenixGeneralButton>().FirstOrDefault(b => b.name == "EquipAllButton");
                        //TFTVLogger.Always($"found button {phoenixGeneralButton.name} enabled? {phoenixGeneralButton.enabled} gameobject active? {phoenixGeneralButton.gameObject.activeSelf}");

                        phoenixGeneralButton.gameObject.SetActive(true);
                        phoenixGeneralButton.RemoveAllClickedDelegates();
                        phoenixGeneralButton.PointerClicked += () => EditScreen.LoadoutsAndHelmetToggle.EquipBestCurrentTeam(geoSite, uIModuleGeoRoster);
                        phoenixGeneralButton.ResetButtonAnimations();
                        return;
                    }

                    TFTVLogger.Always($"CreateBestEquipmentButton running");

                    Resolution resolution = Screen.currentResolution;
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    float resolutionFactorHeight = (float)resolution.height / 1080f;

                    if (resolution.width == 1920 && resolutionFactorHeight == 1200)
                    {
                        resolutionFactorHeight = 1;
                    }

                    EditUnitButtonsController editUnitButtonsController = controller.View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;

                    uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.gameObject.SetActive(true);

                    PhoenixGeneralButton useBestEquipmentButton = UnityEngine.Object.Instantiate(uIModuleDeploymentMissionBriefing.DeployButton, uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.transform);

                    uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.transform.position += new Vector3(105 * resolutionFactorWidth, 5 * resolutionFactorHeight, 0);
                    uIModuleDeploymentMissionBriefing.SquadSlotsUsedText.fontSize -= 10;

                    useBestEquipmentButton.name = "EquipAllButton";

                    Text text = useBestEquipmentButton.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "UIText3Big").GetComponent<Text>();

                    text.GetComponent<I2.Loc.Localize>().enabled = false;
                    text.text = TFTVCommonMethods.ConvertKeyToString("KEY_UI_LOADUP_TEXT").ToUpper();

                    Image image = useBestEquipmentButton.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Hotkey");


                    if (image == null)
                    {
                        TFTVLogger.Always($"image==null: {image == null}");
                        /*  GameObject iconObject = new GameObject("IconObject", typeof(Image), typeof(RectTransform));
                          iconObject.GetComponent<RectTransform>().SetParent(useBestEquipmentButton.transform);
                          image = iconObject.GetComponent<Image>();
                          image.preserveAspect = true;
                          image.SetNativeSize();*/
                    }
                    else
                    {
                        image.sprite = Helper.CreateSpriteFromImageFile("Lockers.png");
                    }

                    useBestEquipmentButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_UI_LOADUP_TIP");

                    useBestEquipmentButton.PointerClicked += () => EditScreen.LoadoutsAndHelmetToggle.EquipBestCurrentTeam(geoSite, uIModuleGeoRoster);
                    useBestEquipmentButton.transform.position += new Vector3(-373 * resolutionFactorWidth, 264 * resolutionFactorHeight, 0);

                    useBestEquipmentButton.SetInteractable(true);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


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

                        GeoCharacter geoCharacter = uIModuleActorCycle.CurrentCharacter;

                        if (geoCharacter != null)
                        {
                            foreach (GeoVehicle geoVehicle in controller.PhoenixFaction.Vehicles)
                            {
                                if (geoCharacter.GameTags.Contains(Shared.SharedGameTags.VehicleClassTag) && geoVehicle.GroundVehicles.Contains(geoCharacter) ||
                                    geoCharacter.GameTags.Contains(Shared.SharedGameTags.MutogTag)
                                    || geoVehicle.Soldiers.Contains(uIModuleActorCycle.CurrentCharacter))
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

                                CreateBestEquipmentButton(geoSite);
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
            private static float GetRepairCostMultiplier(GeoCharacter geoCharacter)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        return 0.5f;
                    }

                    return 0.5f * TFTVAircraftReworkMain.Modules.Geoscape.Healing.GetRepairBionicsCostFactor(geoCharacter);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

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
                            ResourcePack resourcePack = __instance.MutationUsed.ManufacturePrice * (1f - equippedItemHealth) * GetRepairCostMultiplier(____parentModule.CurrentCharacter);

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
                        ResourcePack resourcePack = itemDef.ManufacturePrice * (1f - equippedItemHealth) * GetRepairCostMultiplier(character);
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

                        ResourcePack pack = item.ItemDef.ManufacturePrice * (1f - equippedItemHealth) * GetRepairCostMultiplier(__instance);
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
                        FixCarsWithDelirium(character);

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

                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;
                        if (config.SkipMovies)
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
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            [HarmonyPatch(typeof(UIStateTacticalCutscene), "EnterState")]
            public static class TFTV_PhoenixGame_RunGameLevel_SkipLanding_Patch
            {
                public static void Postfix(UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
                {
                    try
                    {
                        TFTVConfig config = TFTVMain.Main.Config;
                        TFTVLogger.Always($"UIStateTacticalCutscene EnterState called");

                        if (config.SkipMovies)
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
