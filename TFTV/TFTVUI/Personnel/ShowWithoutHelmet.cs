using Base;
using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Personnel
{
    internal class ShowWithoutHelmet
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private const string HelmetsOffPreferenceVariable = "TFTV_HelmetsOff";

        public static bool HelmetsOff;
        public static UIModuleSoldierCustomization uIModuleSoldierCustomization = null;

        private static GeoLevelController GetGeoLevelController()
        {
            return GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
        }

        private static Image GetHelmetToggleButtonIcon(PhoenixGeneralButton helmetToggleButton)
        {
            try
            {
                if (helmetToggleButton == null)
                {
                    return null;
                }

                return helmetToggleButton.transform.GetChildren().First().GetChildren()
                    .FirstOrDefault(t => t.name.Equals("UI_Icon"))?.GetComponent<Image>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        internal static void SyncCustomHelmetButtonIcon()
        {
            try
            {
                PhoenixGeneralButton helmetToggleButton = Loadouts.HelmetToggle;
                Image icon = GetHelmetToggleButtonIcon(helmetToggleButton);

                if (icon != null)
                {
                    icon.sprite = Helper.CreateSpriteFromImageFile(HelmetsOff
                        ? "TFTV_helmet_on_icon.png"
                        : "TFTV_helmet_off_icon.png");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool GetStoredHelmetPreference()
        {
            try
            {
                GeoLevelController controller = GetGeoLevelController();
                if (controller?.EventSystem == null)
                {
                    return HelmetsOff;
                }

                return controller.EventSystem.GetVariable(HelmetsOffPreferenceVariable) == 1;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return HelmetsOff;
            }
        }

        private static void SetStoredHelmetPreference(bool helmetsOff)
        {
            try
            {
                HelmetsOff = helmetsOff;

                GeoLevelController controller = GetGeoLevelController();
                if (controller?.EventSystem != null)
                {
                    controller.EventSystem.SetVariable(HelmetsOffPreferenceVariable, helmetsOff ? 1 : 0);
                }

                SyncCustomHelmetButtonIcon();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyStoredHelmetPreference(UIModuleSoldierCustomization module)
        {
            try
            {
                if (module?.HideHelmetToggle == null)
                {
                    return;
                }

                bool helmetsOff = GetStoredHelmetPreference();
                HelmetsOff = helmetsOff;
                module.HideHelmetToggle.isOn = helmetsOff;
                SyncCustomHelmetButtonIcon();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool HasAugmentedHead(GeoCharacter character, GameTagDef bionicalTag, GameTagDef mutationTag, ItemSlotDef headSlot)
        {
            try
            {
                if (character == null)
                {
                    return false;
                }

                foreach (GeoItem bionic in character.ArmourItems)
                {
                    if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                        && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        internal static void ToggleButtonClicked(PhoenixGeneralButton helmetToggleButton)
        {
            try
            {
                bool helmetsOff = !GetStoredHelmetPreference();
                SetStoredHelmetPreference(helmetsOff);

                Image icon = GetHelmetToggleButtonIcon(helmetToggleButton);

                if (icon != null)
                {
                    icon.sprite = Helper.CreateSpriteFromImageFile(helmetsOff
                        ? "TFTV_helmet_on_icon.png"
                        : "TFTV_helmet_off_icon.png");
                }

                if (uIModuleSoldierCustomization?.HideHelmetToggle != null && uIModuleSoldierCustomization.HideHelmetToggle.interactable)
                {
                    uIModuleSoldierCustomization.HideHelmetToggle.isOn = helmetsOff;
                }

                TFTVLogger.Always($"HelmetsOff is {HelmetsOff}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(UIModuleSoldierCustomization), nameof(uIModuleSoldierCustomization.OnNewCharacter))]
        internal static class TFTV_UI_UIModuleSoldierCustomization_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

            private static void Postfix(GeoCharacter newCharacter, UIModuleSoldierCustomization __instance)
            {
                try
                {
                    if (newCharacter == null || (!newCharacter.TemplateDef.IsHuman && !newCharacter.TemplateDef.IsMutoid))
                    {
                        return;
                    }

                    uIModuleSoldierCustomization = __instance;

                    bool allowHelmetToggle = newCharacter.TemplateDef.IsHuman
                        && !newCharacter.IsMutoid
                        && !HasAugmentedHead(newCharacter, bionicalTag, mutationTag, headSlot);

                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = allowHelmetToggle;

                    if (allowHelmetToggle)
                    {
                        ApplyStoredHelmetPreference(uIModuleSoldierCustomization);
                    }
                    else
                    {
                        uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                        HelmetsOff = GetStoredHelmetPreference();
                        SyncCustomHelmetButtonIcon();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "UpdateHelmetShown")] //VERIFIED
        internal static class TFTV_UIStateSoldierCustomization_UpdateHelmetShown_HelmetToggle_patch
        {
            public static void Postfix()
            {
                try
                {
                    if (uIModuleSoldierCustomization?.HideHelmetToggle == null)
                    {
                        return;
                    }

                    if (!uIModuleSoldierCustomization.HideHelmetToggle.interactable)
                    {
                        HelmetsOff = GetStoredHelmetPreference();
                        SyncCustomHelmetButtonIcon();
                        return;
                    }

                    SetStoredHelmetPreference(uIModuleSoldierCustomization.HideHelmetToggle.isOn);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")] //VERIFIED
        internal static class TFTV_UIStateSoldierCustomization_DisplaySoldier_HelmetToggle_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

            public static void Postfix()
            {
                try
                {
                    GeoCharacter geoCharacter = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.CurrentCharacter;

                    HelmetsOff = GetStoredHelmetPreference();
                    SyncCustomHelmetButtonIcon();

                    if (geoCharacter == null
                        || (!geoCharacter.TemplateDef.IsHuman && !geoCharacter.TemplateDef.IsMutoid)
                        || uIModuleSoldierCustomization?.HideHelmetToggle == null)
                    {
                        return;
                    }

                    bool allowHelmetToggle = geoCharacter.TemplateDef.IsHuman
                        && !geoCharacter.IsMutoid
                        && !HasAugmentedHead(geoCharacter, bionicalTag, mutationTag, headSlot);

                    uIModuleSoldierCustomization.HideHelmetToggle.interactable = allowHelmetToggle;

                    if (allowHelmetToggle)
                    {
                        ApplyStoredHelmetPreference(uIModuleSoldierCustomization);
                    }
                    else
                    {
                        uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                        SyncCustomHelmetButtonIcon();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIModuleActorCycle), nameof(UIModuleActorCycle.DisplaySoldier), new Type[] { typeof(GeoCharacter), typeof(bool), typeof(bool), typeof(bool) })]
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

                    if (character.CharacterStats.Corruption > 0f && character.Fatigue == null)
                    {
                        TFTVLogger.Always($"{character.DisplayName} had Delirium, but has no Stamina! Setting Delirium to 0");
                        character.CharacterStats.Corruption.Set(0f);
                    }

                    if (character.TemplateDef.IsMutog || character.TemplateDef.IsMutoid || character.TemplateDef.IsVehicle)
                    {
                        return true;
                    }

                    if (character != null && character.TemplateDef.IsHuman && !character.IsMutoid && !character.TemplateDef.IsMutog && !character.TemplateDef.IsVehicle)
                    {
                        bool hasAugmentedHead = HasAugmentedHead(character, bionicalTag, mutationTag, headSlot);

                        if (!hasAugmentedHead)
                        {
                            UnitDisplayData unitDisplayData = ____units.FirstOrDefault((UnitDisplayData u) => u.BaseObject == character);
                            if (unitDisplayData == null)
                            {
                                return true;
                            }

                            ____classWorldDisplay.SetDisplay(character.GetClassViewElementDefs(), (float)character.CharacterStats.Corruption > 0f);

                            HelmetsOff = GetStoredHelmetPreference();

                            if (HelmetsOff && __instance.CurrentState != UIModuleActorCycle.ActorCycleState.SubmenuSection)
                            {
                                __instance.DisplaySoldier(unitDisplayData, resetAnimation, addWeapon, showHelmet = false);
                                return false;
                            }

                            return true;
                        }

                        return true;
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
}
