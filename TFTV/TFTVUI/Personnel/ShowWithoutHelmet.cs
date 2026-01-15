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

        public static bool HelmetsOff;
        private static bool toggleState = false;  // Initial toggle state
        public static UIModuleSoldierCustomization uIModuleSoldierCustomization = null;

        internal static void ToggleButtonClicked(PhoenixGeneralButton helmetToggleButton)
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



      


        [HarmonyPatch(typeof(UIStateSoldierCustomization), "UpdateHelmetShown")] //VERIFIED
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

}
