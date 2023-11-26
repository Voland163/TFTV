using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    internal class TFTVAugmentations
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;



        [HarmonyPatch(typeof(UIModuleMutate), "OnNewCharacter")]//InitCharacterInfo")]
        public static class UIModuleMutate_InitCharacterInfo_Patch
        {
            public static void Postfix(Dictionary<AddonSlotDef, UIModuleMutationSection> ____augmentSections, GeoCharacter newCharacter)
            {
                try
                {
                    if (newCharacter.TemplateDef != null && newCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag))
                    {
                       // TFTVLogger.Always($"current character is {newCharacter.DisplayName} and it has mercenary tag? {newCharacter.TemplateDef.GetGameTags().Contains(MercenaryTag)}");

                        foreach (KeyValuePair<AddonSlotDef, UIModuleMutationSection> augmentSection in ____augmentSections)
                        {
                            augmentSection.Value.ResetContainer(AugumentSlotState.BlockedByPermenantAugument, "KEY_ABILITY_NOAUGMENTATONS");
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(UIModuleBionics), "OnNewCharacter")]//InitCharacterInfo")]
        public static class UIModuleBionics_InitCharacterInfo_Patch
        {
            public static void Postfix(Dictionary<AddonSlotDef, UIModuleMutationSection> ____augmentSections, GeoCharacter newCharacter)
            {
                try
                {
                    if (newCharacter.TemplateDef != null && newCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag))
                    {
                       // TFTVLogger.Always($"current character is {newCharacter.DisplayName} and it has mercenary tag? {newCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag)}");

                        foreach (KeyValuePair<AddonSlotDef, UIModuleMutationSection> augmentSection in ____augmentSections)
                        {
                            augmentSection.Value.ResetContainer(AugumentSlotState.BlockedByPermenantAugument, "KEY_ABILITY_NOAUGMENTATONS");
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }




        [HarmonyPatch(typeof(UIModuleBionics), "InitPossibleMutations")]
        public static class UIModuleBionics_InitPossibleMutations_patch
        {
            public static bool Prefix(UIModuleBionics __instance, Dictionary<AddonSlotDef, UIModuleMutationSection> ____augmentSections)
            {
                try
                {
                    if (____augmentSections.Any())
                    {
                        ____augmentSections.Clear();
                    }

                    UIModuleMutationSection[] componentsInChildren = __instance.GetComponentsInChildren<UIModuleMutationSection>();
                    UIModuleMutationSection[] array = componentsInChildren;
                    foreach (UIModuleMutationSection uIModuleMutationSection in array)
                    {
                        ____augmentSections[uIModuleMutationSection.SlotForMutation] = uIModuleMutationSection;
                        ____augmentSections[uIModuleMutationSection.SlotForMutation].PossibleMutations.Clear();
                    }

                    foreach (ItemDef item in (from p in GameUtl.GameComponent<DefRepository>().GetAllDefs<ItemDef>()
                                              where p.Tags.Contains(Shared.SharedGameTags.BionicalTag) && (!p.Tags.Contains(TFTVChangesToDLC5.MercenaryTag)) && p.ViewElementDef != null
                                              select p).ToList())
                    {
                        AddonDef.RequiredSlotBind[] requiredSlotBinds = item.RequiredSlotBinds;
                        for (int i = 0; i < requiredSlotBinds.Length; i++)
                        {
                            AddonDef.RequiredSlotBind requiredSlotBind = requiredSlotBinds[i];
                            if (____augmentSections.ContainsKey(requiredSlotBind.RequiredSlot))
                            {
                                ____augmentSections[requiredSlotBind.RequiredSlot].PossibleMutations.Add(item);
                            }
                        }
                    }

                    array = componentsInChildren;
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].InitView(__instance);
                    }

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(EditUnitButtonsController), "CheckIsBionicsIsAvailable")]
        public static class EditUnitButtonsController_CheckIsBionicsIsAvailable_Bionics_patch
        {
            public static void Postfix(GeoPhoenixFaction phoenixFaction, ref bool ____bionicsAvailable, 
                EditUnitButtonsController __instance, UIModuleActorCycle ____parentModule)
            {
                try
                {

                    bool flag = false;
                    foreach (GeoPhoenixBase basis in phoenixFaction.Bases)
                    {
                        foreach (GeoPhoenixFacility facility in basis.Layout.Facilities)
                        {
                            if (!(facility.Def != __instance.BionicLab) && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }

                  //  PassiveModifierAbilityDef noAugAbility = DefCache.GetDef<PassiveModifierAbilityDef>("NoAug_AbilityDef");

                    if (flag) //&& ____parentModule != null && ____parentModule.CurrentCharacter != null && !____parentModule.CurrentCharacter.GetTacticalAbilities().Contains(noAugAbility))
                    {
                        

                    }
                    else
                    {
                        ____bionicsAvailable = false;
                        MethodInfo methodInfo = typeof(EditUnitButtonsController).GetMethod("SetCircularButtonVisibility", BindingFlags.NonPublic | BindingFlags.Instance);

                        methodInfo.Invoke(__instance, new object[] { __instance.BionicsButton, ____bionicsAvailable });
                    }
                   
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(EditUnitButtonsController), "CheckIsMutationIsAvailable")]
        public static class EditUnitButtonsController_CheckIsMutationIsAvailable_Mutations_patch
        {                   
            public static void Postfix(GeoPhoenixFaction phoenixFaction, ref bool ____mutationAvailable, EditUnitButtonsController __instance, UIModuleActorCycle ____parentModule)
            {
                try
                {

                    bool flag = false;
                    foreach (GeoPhoenixBase basis in phoenixFaction.Bases)
                    {
                        foreach (GeoPhoenixFacility facility in basis.Layout.Facilities)
                        {
                            if (!(facility.Def != __instance.MutationLab) && facility.State == GeoPhoenixFacility.FacilityState.Functioning && facility.IsPowered)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }

                   // PassiveModifierAbilityDef noAugAbility = DefCache.GetDef<PassiveModifierAbilityDef>("NoAug_AbilityDef");

                    if (flag) //&& ____parentModule !=null && ____parentModule.CurrentCharacter!=null && !____parentModule.CurrentCharacter.GetTacticalAbilities().Contains(noAugAbility))
                    {


                    }
                    else
                    {
                        ____mutationAvailable = false;
                        MethodInfo methodInfo = typeof(EditUnitButtonsController).GetMethod("SetCircularButtonVisibility", BindingFlags.NonPublic | BindingFlags.Instance);

                        methodInfo.Invoke(__instance, new object[] { __instance.MutationButton, ____mutationAvailable });
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        public static class PhoenixStatisticsManager_UpdateGeoscapeStats_AnuPissedAtBionics_Patch
        {
            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                    int bionics = 0;
                    GeoLevelController geoLevelController = __instance.GeoLevel;
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance, geoLevelController.ViewerFaction);

                    //check number of bionics player has
                    GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                    foreach (GeoCharacter geoCharacter in __instance.GeoLevel.PhoenixFaction.Soldiers)
                    {
                        foreach (GeoItem bionic in geoCharacter.ArmourItems)
                        {
                            if (bionic.ItemDef.Tags.Contains(bionicalTag) && !bionic.ItemDef.Tags.Contains(TFTVChangesToDLC5.MercenaryTag))

                                bionics += 1;
                        }
                    }
                    if (bionics > 6 && geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Over_Bionics") == 0
                        && CheckForFacility(__instance.GeoLevel, "KEY_BASE_FACILITY_BIONICSLAB_NAME"))
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed1", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_Anu_Pissed_Over_Bionics", 1);
                    }

                    if (geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Broke_Promise") == 1
                       && geoLevelController.EventSystem.GetVariable("BG_Anu_Really_Pissed_Over_Bionics") == 0)
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed2", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_Anu_Really_Pissed_Over_Bionics", 1);
                        DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_BIONICSLAB_NAME", __instance.GeoLevel);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoAlienFaction), "UpdateFactionDaily")]
        public static class PhoenixStatisticsManager_UpdateGeoscapeStats_NJPissedAtMutations_Patch
        {
            public static void Postfix(GeoAlienFaction __instance)
            {
                try
                {
                    int mutations = 0;
                    GeoLevelController geoLevelController = __instance.GeoLevel;
                    GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance, geoLevelController.ViewerFaction);

                    //check number of mutations player has
                    GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                    foreach (GeoCharacter geoCharacter in __instance.GeoLevel.PhoenixFaction.Soldiers)
                    {
                        foreach (GeoItem mutation in geoCharacter.ArmourItems)
                        {
                            if (mutation.ItemDef.Tags.Contains(mutationTag) && !mutation.ItemDef.name.Contains("Mutoid"))
                                mutations += 1;
                        }
                    }
                    if (mutations > 6 && geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Over_Mutations") == 0
                        && CheckForFacility(__instance.GeoLevel, "KEY_BASE_FACILITY_MUTATION_LAB_NAME"))
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed1", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_NJ_Pissed_Over_Mutations", 1);
                    }
                    if (geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Broke_Promise") == 1
                       && geoLevelController.EventSystem.GetVariable("BG_NJ_Really_Pissed_Over_Mutations") == 0)
                    {
                        geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed2", geoscapeEventContext);
                        geoLevelController.EventSystem.SetVariable("BG_NJ_Really_Pissed_Over_Mutations", 1);
                        DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_MUTATION_LAB_NAME", __instance.GeoLevel);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Used for triggering NJ Pissed events 
        [HarmonyPatch(typeof(UIModuleMutationSection), "ApplyMutation")]
        public static class UIModuleMutationSection_ApplyMutation_PissedEvents_patch
        {
            private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
            private static readonly GeoFactionDef newJerico = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;

            public static void Postfix(UIModuleMutationSection __instance, IAugmentationUIModule ____parentModule)
            {
                try
                {


                    //check if player made promise to New Jericho not to apply more mutations
                    if (____parentModule.Context.Level.EventSystem.GetVariable("BG_NJ_Pissed_Made_Promise") == 1
                        && ____parentModule.CurrentCharacter.OriginalFactionDef == newJerico && __instance.MutationUsed.Tags.Contains(mutationTag))
                    {
                        ____parentModule.Context.Level.EventSystem.SetVariable("BG_NJ_Pissed_Broke_Promise", 1);

                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Used for triggering Anu pissed events
        [HarmonyPatch(typeof(UIModuleBionics), "OnAugmentApplied")]
        public static class UIModuleBionics_OnAugmentApplied_SetStaminaTo0_patch
        {
            public static void Postfix(UIModuleBionics __instance)
            {
                try
                {
                    //check if player made promise to Anu not to apply more bionics
                    if (__instance.Context.Level.EventSystem.GetVariable("BG_Anu_Pissed_Made_Promise") == 1)
                    {
                        __instance.Context.Level.EventSystem.SetVariable("BG_Anu_Pissed_Broke_Promise", 1);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static bool CheckForFacility(GeoLevelController level, string facilityName)
        {
            try
            {
                List<GeoPhoenixBase> phoenixBases = level.PhoenixFaction.Bases.ToList();

                foreach (GeoPhoenixBase pxBase in phoenixBases)
                {

                    List<GeoPhoenixFacility> facilities = pxBase.Layout.Facilities.ToList();

                    foreach (GeoPhoenixFacility facility in facilities)
                    {
                        if (facility.ViewElementDef.DisplayName1.LocalizationKey == facilityName)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void DestroyFacilitiesOnPXBases(string nameOfFacility, GeoLevelController level)
        {
            try
            {
                List<GeoPhoenixBase> phoenixBases = level.PhoenixFaction.Bases.ToList();

                foreach (GeoPhoenixBase pxBase in phoenixBases)
                {

                    List<GeoPhoenixFacility> facilities = pxBase.Layout.Facilities.ToList();

                    foreach (GeoPhoenixFacility facility in facilities)
                    {
                        if (facility.ViewElementDef.DisplayName1.LocalizationKey == nameOfFacility)

                        {
                            facility.DestroyFacility();

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

