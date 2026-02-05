using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
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

                    return 0.5f * AircraftReworkGeoscape.Healing.GetRepairBionicsCostFactor(geoCharacter);

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
            [HarmonyPatch(typeof(UIModuleMutationSection), "SelectMutation")] //VERIFIED

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


            [HarmonyPatch(typeof(UIModuleMutationSection), "RepairItem")] //VERIFIED

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
                    TFTVLogger.Always($"Adding repairable item {itemDef?.name} for character {character?.DisplayName}");


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


            [HarmonyPatch(typeof(GeoCharacter), nameof(GeoCharacter.RepairItem), new Type[] { typeof(GeoItem), typeof(bool) })]

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


        /* [HarmonyPatch(typeof(UIModuleMutate), "OnNewCharacter")]//InitCharacterInfo")]
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
         }*/



        [HarmonyPatch(typeof(UIStateMutate), "EnterState")]//InitCharacterInfo")] //VERIFIED
        public static class UIStateMutate_EnterState_Patch
        {
            public static void Prefix(ref List<GeoCharacter> ____characters)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!config.MercsCanBeAugmented)
                    {
                        ____characters.RemoveAll(e => e.TemplateDef != null && e.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(UIStateBionics), "EnterState")]//InitCharacterInfo")] //VERIFIED
        public static class UIStateBionics_EnterState_Patch
        {
            public static void Prefix(ref List<GeoCharacter> ____characters)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!config.MercsCanBeAugmented)
                    {
                        ____characters.RemoveAll(e => e.TemplateDef != null && e.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }







        /*[HarmonyPatch(typeof(UIModuleBionics), "OnNewCharacter")]//InitCharacterInfo")]
        public static class UIModuleBionics_InitCharacterInfo_Patch
        {          
            public static void Postfix()
            {
                try
                {
                    if (all_units == null)
                    {

                        UIModuleActorCycle uIModuleActorCycle = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                        FieldInfo fieldInfoUnitList = typeof(UIModuleActorCycle).GetField("_units", BindingFlags.Instance | BindingFlags.NonPublic);

                        List<UnitDisplayData> units = (List<UnitDisplayData>)fieldInfoUnitList.GetValue(uIModuleActorCycle);

                        all_units = new List<UnitDisplayData>();
                        all_units.AddRange(units);

                        foreach (UnitDisplayData unit in all_units)
                        {
                            if (unit.BaseObject is GeoCharacter geoCharacter
                                && geoCharacter.TemplateDef != null
                                && geoCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag))
                            {
                                units.Remove(unit);
                                TFTVLogger.Always($"removing {geoCharacter.DisplayName}");
                            }
                        }

                        fieldInfoUnitList.SetValue(uIModuleActorCycle, units);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void Postfix()
            {
                try
                {
                    if (all_units != null)
                    {

                        UIModuleActorCycle uIModuleActorCycle = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule;
                        FieldInfo fieldInfoUnitList = typeof(UIModuleActorCycle).GetField("_units", BindingFlags.Instance | BindingFlags.NonPublic);



                        fieldInfoUnitList.SetValue(uIModuleActorCycle, all_units);

                        all_units = null;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/



        /* [HarmonyPatch(typeof(UIModuleBionics), "OnNewCharacter")]//InitCharacterInfo")]
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
         }*/




        [HarmonyPatch(typeof(UIModuleBionics), "InitPossibleMutations")] //VERIFIED
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



        [HarmonyPatch(typeof(EditUnitButtonsController), "CheckIsBionicsIsAvailable")] //VERIFIED
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


        [HarmonyPatch(typeof(EditUnitButtonsController), "CheckIsMutationIsAvailable")] //VERIFIED
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


        public static void AnuReactionToBionicsGeoAlienFactionUpdateFactionDaily(GeoAlienFaction geoAlienFaction)
        {
            try
            {
                int bionics = 0;
                GeoLevelController geoLevelController = geoAlienFaction.GeoLevel;
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(geoAlienFaction, geoLevelController.ViewerFaction);

                //check number of bionics player has
                GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
                foreach (GeoCharacter geoCharacter in geoAlienFaction.GeoLevel.PhoenixFaction.Soldiers)
                {
                    foreach (GeoItem bionic in geoCharacter.ArmourItems)
                    {
                        if (bionic.ItemDef.Tags.Contains(bionicalTag) && !bionic.ItemDef.Tags.Contains(TFTVChangesToDLC5.MercenaryTag))
                        {
                            bionics += 1;
                        }
                    }
                }
                if (bionics > 6 && geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Over_Bionics") == 0
                    && CheckForFacility(geoAlienFaction.GeoLevel, "KEY_BASE_FACILITY_BIONICSLAB_NAME"))
                {
                    geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed1", geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BG_Anu_Pissed_Over_Bionics", 1);
                }

                if (geoLevelController.EventSystem.GetVariable("BG_Anu_Pissed_Broke_Promise") == 1
                   && geoLevelController.EventSystem.GetVariable("BG_Anu_Really_Pissed_Over_Bionics") == 0)
                {
                    geoLevelController.EventSystem.TriggerGeoscapeEvent("Anu_Pissed2", geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BG_Anu_Really_Pissed_Over_Bionics", 1);
                    DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_BIONICSLAB_NAME", geoAlienFaction.GeoLevel);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void NJReactionToMutationsGeoAlienFactionUpdateFactionDaily(GeoAlienFaction geoAlienFaction)
        {
            try
            {
                int mutations = 0;
                GeoLevelController geoLevelController = geoAlienFaction.GeoLevel;
                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(geoAlienFaction, geoLevelController.ViewerFaction);

                //check number of mutations player has
                GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
                foreach (GeoCharacter geoCharacter in geoAlienFaction.GeoLevel.PhoenixFaction.Soldiers)
                {
                    foreach (GeoItem mutation in geoCharacter.ArmourItems)
                    {
                        if (mutation.ItemDef.Tags.Contains(mutationTag) && !mutation.ItemDef.name.Contains("Mutoid"))
                            mutations += 1;
                    }
                }
                if (mutations > 6 && geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Over_Mutations") == 0
                    && CheckForFacility(geoAlienFaction.GeoLevel, "KEY_BASE_FACILITY_MUTATION_LAB_NAME"))
                {
                    geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed1", geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BG_NJ_Pissed_Over_Mutations", 1);
                }
                if (geoLevelController.EventSystem.GetVariable("BG_NJ_Pissed_Broke_Promise") == 1
                   && geoLevelController.EventSystem.GetVariable("BG_NJ_Really_Pissed_Over_Mutations") == 0)
                {
                    geoLevelController.EventSystem.TriggerGeoscapeEvent("NJ_Pissed2", geoscapeEventContext);
                    geoLevelController.EventSystem.SetVariable("BG_NJ_Really_Pissed_Over_Mutations", 1);
                    DestroyFacilitiesOnPXBases("KEY_BASE_FACILITY_MUTATION_LAB_NAME", geoAlienFaction.GeoLevel);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        //Used for triggering NJ Pissed events 
        [HarmonyPatch(typeof(UIModuleMutationSection), "ApplyMutation")] //VERIFIED
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


        public static void CheckAnuPissedBionicsBrokenPromise(UIModuleBionics uIModuleBionics)
        {
            try
            {
                //check if player made promise to Anu not to apply more bionics
                if (uIModuleBionics.Context.Level.EventSystem.GetVariable("BG_Anu_Pissed_Made_Promise") == 1)
                {
                    uIModuleBionics.Context.Level.EventSystem.SetVariable("BG_Anu_Pissed_Broke_Promise", 1);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
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

