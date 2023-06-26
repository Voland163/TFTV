using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI.VideoPlayback;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
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
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVUI
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //This method changes how WP are displayed in the Edit personnel screen, to show effects of Delirium on WP
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //  public static Dictionary<int, List<string>> CurrentlyHiddenInv = new Dictionary<int, List<string>>();
        //  public static Dictionary<int, List<string>> CurrentlyAvailableInv = new Dictionary<int, List<string>>();



        public static UIModuleCharacterProgression hookToProgressionModule = null;
        public static GeoCharacter hookToCharacter = null;
        internal static bool moduleInfoBarAdjustmentsExecuted = false;
        // public static bool showFaceNotHelmet = true;

        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color blue = new Color32(62, 12, 224, 255);
        internal static Color green = new Color32(12, 224, 30, 255);
        internal static Color anu = new Color(0.9490196f, 0.0f, 1.0f, 1.0f);
        internal static Color nj = new Color(0.156862751f, 0.6156863f, 1.0f, 1.0f);
        internal static Color syn = new Color(0.160784319f, 0.8862745f, 0.145098045f, 1.0f);



        /*  [HarmonyPatch(typeof(ModManager), "SerializeModObject")]

          public static class TFTV_ModManager_SerializeModObject_patch
          {
              public static bool Prefix(ModMain mod, object data, ModManager __instance, ref ModInstanceData __result)
              {
                  try
                  {
                      if (data == null)
                      {
                         __result = null;
                      }

                      try
                      {
                          var settings = new JsonSerializerSettings
                          {
                              ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                          };

                          string jsonData = JsonConvert.SerializeObject(data, settings);
                          __result = new ModInstanceData
                          {
                              JsonData = jsonData,
                              TypeName = data.GetType().FullName
                          };
                      }
                      catch (Exception e)
                      {          
                          TFTVLogger.Error(e);
                      }


                      __result = null;

                      return false;
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/




        //EditUnitButtonsController for later attempts at adding toggle helmet on/off button

        /*[HarmonyPatch(typeof(EditUnitButtonsController))]
        [HarmonyPatch("Awake")]
        public static class EditUnitButtonsController_Awake_Patch
        {
            static void Postfix(EditUnitButtonsController __instance)
            {
                try
                {
                    // Use Traverse to access the UIModuleSoldierCustomization class
                    var uiModuleSoldierCustomizationType = AccessTools.TypeByName("UIModuleSoldierCustomization");
                    TFTVLogger.Always($"EditUnitButtonsController - Awake");
                    if (uiModuleSoldierCustomizationType != null)
                    {
                        TFTVLogger.Always($"{uiModuleSoldierCustomizationType.Name} got here");

                        // Use Traverse to access the HideHelmetToggle field in UIModuleSoldierCustomization
                        var hideHelmetToggleField = Traverse.Create(uiModuleSoldierCustomizationType).Field("HideHelmetToggle");

                        if (hideHelmetToggleField != null)
                        {
                            TFTVLogger.Always($"got here 2");

                            // Retrieve the HideHelmetToggle field value
                            Toggle hideHelmetToggle = hideHelmetToggleField.GetValue<Toggle>();


                            // Add the toggle button to the EditUnitButtonsController instance
                            __instance.gameObject.AddComponent(hideHelmetToggle.GetType());
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(EditUnitButtonsController))]
        [HarmonyPatch("Init")]
        public static class EditUnitButtonsController_Init_Patch
        {
            static void Postfix(EditUnitButtonsController __instance)
            {
                try
                {
                    Toggle hideHelmetToggle = __instance.gameObject.GetComponent<Toggle>();

                    hideHelmetToggle.interactable = true;

                   // hideHelmetToggle.transform.parent.gameObject.SetActive(true);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }
        }
        */


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

        /*  [HarmonyPatch(typeof(GeoCharacter), "RepairItem", new Type[] { typeof(ItemDef), typeof(bool) })]

          public static class TFTV_GeoCharacter_RepairItem_patch
          {

              public static void Postfix(GeoCharacter __instance, bool __result)
              {
                  try
                  {
                      TFTVLogger.Always("GeoCharacter RepairItem invoked and result is " + __result);

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }
          }*/

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
                            if (armourItem.GameTags.Contains(mutationTag))
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

        //UIStateBuyMutoid

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
                                recruit.Identity.Name = "Mutoid " + dictionary[classTag].DisplayName2.Localize();
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







        //Adapted from Mad´s Assorted Adjustments; this patch changes Geoescape UI

        [HarmonyPatch(typeof(UIModuleInfoBar), "Init")]
        public static class TFTV_UIModuleInfoBar_Init_GeoscapeUI_Patch
        {
            public static void Prefix(UIModuleInfoBar __instance, GeoscapeViewContext ____context)
            {
                try
                {
                    if (moduleInfoBarAdjustmentsExecuted)
                    {
                        return;
                    }

                    Resolution resolution = Screen.currentResolution;

                    // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;
                    //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                    // Declutter
                    Transform tInfoBar = __instance.PopulationBarRoot.transform.parent?.transform;

                    //Use this to catch the ToolTip
                    Transform[] thingsToUse = new Transform[2];

                    __instance.PopulationTooltip.enabled = false;

                    foreach (Transform t in tInfoBar.GetComponentsInChildren<Transform>())
                    {

                        if (t.name == "TooltipCatcher")
                        {
                            if (t.GetComponent<UITooltipText>().TipKey.LocalizeEnglish() == "Stores - used space / capacity of all stores facilities")
                            {
                                thingsToUse[0] = t;
                            }
                        }

                        // Hide useless icons at production and research
                        if (t.name == "UI_Clock")
                        {
                            t.gameObject.SetActive(false);
                        }
                        //Add Delirium and Pandoran evolution icons, as well as factions icons.
                        if (t.name == "Requirement_Icon")
                        {
                            Image icon = t.gameObject.GetComponent<Image>();
                            if (icon.sprite.name == "Geoscape_UICanvasIcons_Actions_EditSquad")
                            {
                                icon.sprite = Helper.CreateSpriteFromImageFile("Void-04P.png");
                                t.gameObject.name = "DeliriumIcon";
                                t.parent = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");
                                t.Translate(new Vector3(30f * resolutionFactorWidth, 0f, 0f));
                                t.localScale = new Vector3(1.3f, 1.3f, 1f);
                                t.gameObject.SetActive(false);
                                //  icon.color = purple;

                                //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}");

                                Transform pandoranEvolution = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                pandoranEvolution.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                                pandoranEvolution.gameObject.GetComponent<Image>().color = red;
                                pandoranEvolution.gameObject.name = "PandoranEvolutionIcon";
                                // pandoranEvolution.localScale = new Vector3(0.9f, 0.9f, 1);
                                pandoranEvolution.Translate(new Vector3(110f * resolutionFactorWidth, 0f, 0f));
                                // pandoranEvolution.Translate(80f*resolutionFactor, 0f, 0f, t);
                                pandoranEvolution.gameObject.SetActive(false);


                                Transform anuDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                anuDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                anuDiploInfoIcon.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().color = anu;
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Anu.png");
                                anuDiploInfoIcon.gameObject.name = "AnuIcon";
                                anuDiploInfoIcon.gameObject.SetActive(false);

                                Transform njDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                njDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                njDiploInfoIcon.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                                njDiploInfoIcon.gameObject.GetComponent<Image>().color = nj;
                                njDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_NewJericho.png");
                                njDiploInfoIcon.gameObject.name = "NJIcon";
                                njDiploInfoIcon.gameObject.SetActive(false);

                                Transform synDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                synDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                synDiploInfoIcon.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                                synDiploInfoIcon.gameObject.GetComponent<Image>().color = syn;
                                synDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Synedrion.png");
                                synDiploInfoIcon.gameObject.name = "SynIcon";
                                synDiploInfoIcon.gameObject.SetActive(false);
                                //  anuDiploInfo.gameObject.GetComponent<Image>().color = red;

                            }

                            // t.name = "ODI_icon";
                            // TFTVLogger.Always("Req_Icon name is " + icon.sprite.name);
                        }

                        if (t.name == "UI_underlight")
                        {
                            if (t.parent.name == "StoresRes")
                            {
                                thingsToUse[1] = t;
                            }


                            // TFTVLogger.Always("Parent of UI_underlight " + t.parent.name);


                            // separator.position = anuDiploInfoIcon.position - new Vector3(-100, 0, 0);
                        }

                        //Create separators to hold Delirium and Pandoran Evolution icons
                        if (t.name == "Separator")
                        {
                            Transform separator = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator.Translate(new Vector3(0f, 12f * resolutionFactorHeight, 0f));
                            separator.gameObject.name = "ODISeparator1";
                            separator.gameObject.SetActive(false);
                            // separator.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                            Transform separator2 = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator2.Translate(new Vector3(180f * resolutionFactorWidth, 12f * resolutionFactorHeight, 0f));
                            separator2.gameObject.name = "ODISeparator2";
                            separator2.gameObject.SetActive(false);
                            //  separator2.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                        }
                        // Remove skull icon
                        if (t.name == "skull")
                        {
                            t.gameObject.SetActive(false);
                        }

                        // Removed tiled gameover bar
                        if (t.name == "tiled_gameover")
                        {

                            t.gameObject.SetActive(false);
                        }

                        //Remove other bits and pieces of doomsday clock
                        if (t.name == "alive_mask" || t.name == "alive_animation" ||
                            t.name.Contains("alive_animated") || t.name == "dead" || t.name.Contains("death"))
                        {

                            t.gameObject.SetActive(false);
                        }

                        //    TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " root position " + "x: " + t.root.position.x);
                        //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " right " + "x: " + t.right.x);

                    }



                    Transform deliriumTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing Delirium tooltip";
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    deliriumTooltip.gameObject.name = "DeliriumTooltip";
                    deliriumTooltip.gameObject.SetActive(false);
                    //TFTVLogger.Always("Got here");

                    Transform evolutionTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing Pandoran Evolution tooltip";
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    evolutionTooltip.gameObject.name = "PandoranEvolutionTooltip";
                    evolutionTooltip.gameObject.SetActive(false);

                    Transform anuTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuIcon"));
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing Anu tooltip";
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    anuTooltip.gameObject.name = "AnuTooltip";
                    anuTooltip.gameObject.SetActive(false);

                    Transform njTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NJIcon"));
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing nj tooltip";
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    njTooltip.gameObject.name = "NJTooltip";
                    njTooltip.gameObject.SetActive(false);

                    Transform synTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynIcon"));
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing syn tooltip";
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    synTooltip.gameObject.name = "SynTooltip";
                    synTooltip.gameObject.SetActive(false);


                    //Create percentages next to each faction icon

                    Transform anuDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    anuDiploInfo.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                    anuDiploInfo.gameObject.name = "AnuPercentage";
                    anuDiploInfo.gameObject.SetActive(false);
                    // anuDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    // anuDiploInfo.gameObject.SetActive(false);

                    Transform njDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    njDiploInfo.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                    njDiploInfo.gameObject.name = "NjPercentage";
                    njDiploInfo.gameObject.SetActive(false);
                    njDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    synDiploInfo.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                    synDiploInfo.gameObject.name = "SynPercentage";
                    synDiploInfo.gameObject.SetActive(false);
                    //   synDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    //Create highlights for new elements

                    Transform deliriumIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    deliriumIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform PandoranEvolutionIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    PandoranEvolutionIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    PandoranEvolutionIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform anuDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuPercentage"));
                    anuDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));


                    Transform njDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NjPercentage"));
                    njDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // njDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynPercentage"));
                    synDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // synDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    __instance.PopulationPercentageText.gameObject.SetActive(false);

                    // Set a flag so that this whole stuff is only done ONCE
                    // Otherwise the visual transformations are repeated everytime leading to weird results
                    // This is reset on every level change (see below)
                    moduleInfoBarAdjustmentsExecuted = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Patch to ensure that patch above is only run once
        [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
        public static class PhoenixGame_RunGameLevel_Patch
        {
            public static void Prefix()
            {
                moduleInfoBarAdjustmentsExecuted = false;
            }
        }

        //Second patch to update Geoscape UI
        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdatePopulation")]
        public static class TFTV_ODI_meter_patch
        {
            public static void Postfix(UIModuleInfoBar __instance, GeoscapeViewContext ____context, LayoutGroup ____layoutGroup)
            {

                try
                {
                    //  TFTVLogger.Always("Running UpdatePopulation");

                    GeoLevelController controller = ____context.Level;

                    List<GeoAlienBase> listOfAlienBases = controller.AlienFaction.Bases.ToList();

                    int nests = 0;
                    int lairs = 0;
                    int citadels = 0;


                    foreach (GeoAlienBase alienBase in listOfAlienBases)
                    {
                        if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef")))
                        {
                            nests++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef")))
                        {
                            lairs++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef")))
                        {
                            citadels++;
                        }

                    }


                    int pEPerDay = nests + lairs * 2 + citadels * 3 + controller.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 2;
                    //max, not counting IH, is 3 + 6 + 9 = 18
                    //>=66%, evo high, so 12+
                    //<66% >33%, evo normal, 6+ 
                    //<33%, evo slow, else


                    Transform tInfoBar = __instance.PopulationBarRoot.transform.parent?.transform;
                    Transform populationBar = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");

                    //     TFTVLogger.Always("Got here");


                    Transform anuInfo = populationBar.GetComponent<Transform>().Find("AnuPercentage");
                    anuInfo.gameObject.GetComponent<Text>().text = $"<color=#f200ff>{____context.Level.AnuFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";


                    Transform njInfo = populationBar.GetComponent<Transform>().Find("NjPercentage");
                    njInfo.gameObject.GetComponent<Text>().text = $"<color=#289eff>{____context.Level.NewJerichoFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";


                    Transform synInfo = populationBar.GetComponent<Transform>().Find("SynPercentage");
                    synInfo.gameObject.GetComponent<Text>().text = $"<color=#28e225>{____context.Level.SynedrionFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";

                    Transform anuIcon = populationBar.GetComponent<Transform>().Find("AnuIcon");
                    Transform njIcon = populationBar.GetComponent<Transform>().Find("NJIcon");
                    Transform synIcon = populationBar.GetComponent<Transform>().Find("SynIcon");

                    //   TFTVLogger.Always("Got here 2");

                    Transform anuTooltip = populationBar.GetComponent<Transform>().Find("AnuIcon").GetComponent<Transform>().Find("AnuTooltip");
                    Transform njTooltip = populationBar.GetComponent<Transform>().Find("NJIcon").GetComponent<Transform>().Find("NJTooltip");
                    Transform synTooltip = populationBar.GetComponent<Transform>().Find("SynIcon").GetComponent<Transform>().Find("SynTooltip");


                    string anuToolTipText = "<b>The Disciples of Anu</b>";
                    string njToolTipText = "<b>New Jericho</b>";
                    string synToolTipText = "<b>Synedrion</b>";

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("AN_Discovered_DiplomacyStateTagDef")))
                    {
                        anuInfo.gameObject.SetActive(true);
                        anuIcon.gameObject.SetActive(true);
                        anuTooltip.gameObject.SetActive(true);

                        anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = anuToolTipText + "\n" + CreateTextForAnuTooltipText(controller);

                    }

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("NJ_Discovered_DiplomacyStateTagDef")))
                    {
                        njInfo.gameObject.SetActive(true);
                        njIcon.gameObject.SetActive(true);
                        njTooltip.gameObject.SetActive(true);

                        njTooltip.gameObject.GetComponent<UITooltipText>().TipText = njToolTipText + "\n" + CreateTextForNJTooltipText(controller);
                    }
                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("SY_Discovered_DiplomacyStateTagDef")))
                    {
                        synInfo.gameObject.SetActive(true);
                        synIcon.gameObject.SetActive(true);
                        synTooltip.gameObject.SetActive(true);

                        synTooltip.gameObject.GetComponent<UITooltipText>().TipText = synToolTipText + "\n" + CreateTextForSynTooltipText(controller);
                    }

                    //   TFTVLogger.Always("Got here 3");
                    Transform deliriumIconHolder = populationBar.GetComponent<Transform>().Find("DeliriumIcon");
                    Image deliriumIcon = deliriumIconHolder.GetComponent<Image>();
                    Transform separator = populationBar.GetComponent<Transform>().Find("ODISeparator1");

                    Transform separator2 = populationBar.GetComponent<Transform>().Find("ODISeparator2");

                    //    TFTVLogger.Always("Got here 4");

                    string deliriumToolTipText = "";
                    if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                    {
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        populationBar.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumIcon.sprite = TFTVDefsRequiringReinjection.VoidIcon;
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status equal to their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }
                    else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                    {
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Phalf.png");
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status of up to half of their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }
                    else if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0)
                    {
                        // TFTVLogger.Always("Got to SDI01");
                        deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Pthird.png");
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status of up to a third of their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }

                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                    {
                        deliriumToolTipText += "\n-<i>No limit to Delirium, regardless of ODI level</i> Void Omen is in effect.";
                    }

                    if (controller.EventSystem.GetEventRecord("SDI_09")?.SelectedChoice == 0)
                    {
                        deliriumToolTipText += "\n-Evolved Umbra sighted.";
                    }
                    else if (controller.EventSystem.GetVariable("UmbraResearched") == 1)
                    {
                        deliriumToolTipText += "\n-Sightings of Umbra have been reported";
                    }
                    if (controller.EventSystem.GetEventRecord("SDI_07")?.SelectedChoice == 0)
                    {
                        deliriumToolTipText += "\n-Havens in the Mist can become infested instead of destroyed when attacked by Pandorans. Infested havens accelerate Pandoran evolution.";
                    }


                    Transform deliriumTooltip = populationBar.GetComponent<Transform>().Find("DeliriumIcon").GetComponent<Transform>().Find("DeliriumTooltip");
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = deliriumToolTipText;
                    deliriumTooltip.gameObject.SetActive(true);
                    //TFTVLogger.Always("Got here");




                    /* if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0 && controller.EventSystem.GetEventRecord("PROG_FS2_WIN")?.SelectedChoice == 0)
                     {
                         deliriumIconHolder.gameObject.SetActive(false);
                     }*/

                    Transform evolutionIconHolder = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon");
                    Image evolutionIcon = evolutionIconHolder.GetComponent<Image>();

                    Transform evolutionTooltip = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon").GetComponent<Transform>().Find("PandoranEvolutionTooltip");
                    string evolutionToolTipText = "Based on reports and field observations, we estimate that the Pandorans are evolving ";
                    if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_EvolvedAliens_ResearchDef"))
                    {
                        // TFTVLogger.Always("Got here 5");
                        evolutionIconHolder.gameObject.SetActive(true);
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        evolutionTooltip.gameObject.SetActive(true);

                        if (pEPerDay >= 12)
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_fast.png");
                            evolutionToolTipText += "<b>very rapidly</b>. We must destroy Pandoran Colonies and Infested Havens before we are overwhelmed!";
                        }
                        else if (pEPerDay >= 6)
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_medium.png");
                            evolutionToolTipText += "<b>rapidly</b>. We must keep the number of Pandoran Colonies and Infested Havens in check.";
                        }
                        else
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                            evolutionToolTipText += ". We are monitoring the situation and will report any newly discovered Pandoran Colonies.";
                        }
                    }

                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = evolutionToolTipText;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }





        public static string CreateTextForAnuTooltipText(GeoLevelController controller)
        {
            try
            {
                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.AnuFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();


                if (controller.EventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 1 || controller.EventSystem.GetEventRecord("PROG_AN6_2")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the second special mission offered by this faction (will be offered again at 49%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_AN6_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_AN6_WIN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN4_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN2_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }


                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }


        public static string CreateTextForSynTooltipText(GeoLevelController controller)
        {
            try
            {
                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.SynedrionFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();
                int polyCounter = controller.EventSystem.GetVariable("Polyphonic");
                int terraCounter = controller.EventSystem.GetVariable("Terraformers");

                if (controller.EventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1 || controller.EventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }

                else if (controller.EventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice == 2)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_SY4_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_SY4_WIN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_SY3_WIN")?.SelectedChoice != null)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_SY1_WIN1")?.SelectedChoice != null || controller.EventSystem.GetEventRecord("PROG_SY1_WIN2")?.SelectedChoice != null)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }

                if (polyCounter > terraCounter)
                {
                    text += "\n-Through Phoenix Project influence, the Polyphonic tendency is currently ascendant in Synedrion";

                }
                else if (polyCounter < terraCounter)
                {
                    text += "\n-Through Phoenix Project influence, the Terraformers are currently ascendant in Synedrion";
                }



                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }




        public static string CreateTextForNJTooltipText(GeoLevelController controller)
        {
            try
            {
                // TFTVLogger.Always($"Checking NJ Diplo status {controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice}");

                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.NewJerichoFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();


                if (controller.EventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the second special mission offered by this faction (will be offered again at 49%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_NJ3_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 1)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ1_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }


                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
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



                    //   string forStrengthToolTip = "";


                    //  GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

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

        // public static UITooltipText StrengthToolTip = null;

        [HarmonyPatch(typeof(UIModuleCharacterProgression), "Awake")]

        internal static class TFTV_UIModuleCharacterProgression_Awake_Hook_patch
        {
            public static void Postfix(UIModuleCharacterProgression __instance)
            {
                try
                {
                    hookToProgressionModule = __instance;
                    //  StrengthToolTip = __instance.StrengthSlider.gameObject.GetComponent<UITooltipText>();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

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
                        TFTVLogger.Always("Actually here");

                        switch (____parentModule.CurrentState)
                        {
                            case UIModuleActorCycle.ActorCycleState.RosterSection:

                                HelmetToggle.gameObject.SetActive(false);
                                HelmetToggle.ResetButtonAnimations();
                                UnequipAll.gameObject.SetActive(false);
                                UnequipAll.ResetButtonAnimations();
                                SaveLoadout.gameObject.SetActive(false);
                                SaveLoadout.ResetButtonAnimations();
                                LoadLoadout.gameObject.SetActive(false);
                                LoadLoadout.ResetButtonAnimations();

                                break;

                            case UIModuleActorCycle.ActorCycleState.EditSoldierSection:

                                TFTVLogger.Always("And even here!");
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
                                HelmetToggle.gameObject.SetActive(false);
                                HelmetToggle.ResetButtonAnimations();
                                UnequipAll.gameObject.SetActive(false);
                                UnequipAll.ResetButtonAnimations();
                                SaveLoadout.gameObject.SetActive(false);
                                SaveLoadout.ResetButtonAnimations();
                                LoadLoadout.gameObject.SetActive(false);
                                LoadLoadout.ResetButtonAnimations();
                                break;
                            case UIModuleActorCycle.ActorCycleState.EditMutogSection:
                                HelmetToggle.gameObject.SetActive(false);
                                HelmetToggle.ResetButtonAnimations();
                                UnequipAll.gameObject.SetActive(false);
                                UnequipAll.ResetButtonAnimations();
                                SaveLoadout.gameObject.SetActive(false);
                                SaveLoadout.ResetButtonAnimations();
                                LoadLoadout.gameObject.SetActive(false);
                                LoadLoadout.ResetButtonAnimations();
                                break;
                            case UIModuleActorCycle.ActorCycleState.CapturedAlienSection:
                                HelmetToggle.gameObject.SetActive(false);
                                HelmetToggle.ResetButtonAnimations();
                                UnequipAll.gameObject.SetActive(false);
                                UnequipAll.ResetButtonAnimations();
                                SaveLoadout.gameObject.SetActive(false);
                                SaveLoadout.ResetButtonAnimations();
                                LoadLoadout.gameObject.SetActive(false);
                                LoadLoadout.ResetButtonAnimations();
                                break;


                        }

                        if (!____parentModule.EditUnitButtonsController.CustomizeButton.gameObject.activeInHierarchy)
                        {
                           

                            // TFTVLogger.Always($"Customize button enabled is {____parentModule.EditUnitButtonsController.CustomizeButton.enabled}");
                            HelmetToggle.gameObject.SetActive(false);
                            HelmetToggle.ResetButtonAnimations();
                            UnequipAll.gameObject.SetActive(false);
                            UnequipAll.ResetButtonAnimations();
                            SaveLoadout.gameObject.SetActive(false);
                            SaveLoadout.ResetButtonAnimations();
                            LoadLoadout.gameObject.SetActive(false);
                            LoadLoadout.ResetButtonAnimations();
                            // HelmetsOff = false;
                        }

                        if (____parentModule.CurrentCharacter!=null && (CharacterLoadouts==null || CharacterLoadouts != null && !CharacterLoadouts.ContainsKey(____parentModule.CurrentCharacter.Id)))
                        {
                          

                            LoadLoadout.gameObject.SetActive(false);
                            LoadLoadout.ResetButtonAnimations();
                        }
                       

                       



                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }



        // public static List<ICommonItem> ItemsEquippedFromStorage = new List<ICommonItem>();

        /*  public static List<ICommonItem> ConvertStringToICommonItem(List<string> itemDefNames)
           {
               try 
               {
                   List<ICommonItem> commonItems = new List<ICommonItem>();

                   foreach(string itemName in itemDefNames)  
                   { 
                   ItemDef itemDef = (ItemDef)Repo.GetDef(itemName);
                    GeoItem commonItem = new GeoItem();



                   }




                   return commonItems;

               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
                   throw;
               }
           }*/




        internal static int LocateSoldier(GeoCharacter geoCharacter)
        {
            try
            {
                int geoVehicleID = 0;
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                foreach (GeoVehicle aircraft in controller.PhoenixFaction.Vehicles)
                {
                    if (aircraft.GetAllCharacters().Contains(geoCharacter))
                    {

                        geoVehicleID = aircraft.VehicleID;
                        break;

                    }
                }


                return geoVehicleID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static List<int> LocateOtherVehicles(int id)
        {
            try
            {
                List<int> vehicleIDs = new List<int>();

                if (id != 0)
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    List<GeoVehicle> geoVehiclesAtSite = controller.PhoenixFaction?.Vehicles?.FirstOrDefault(v => v?.VehicleID == id)?.CurrentSite?.Vehicles?.Where(vs => vs?.Owner == controller.PhoenixFaction && vs?.VehicleID != id)?.ToList();

                    if (geoVehiclesAtSite != null && geoVehiclesAtSite.Count > 0)
                    {

                        foreach (GeoVehicle vehicle in geoVehiclesAtSite)
                        {
                            vehicleIDs.Add(vehicle.VehicleID);

                        }
                    }
                }

                return vehicleIDs;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }




        }



        //  public static GeoCharacter CharacterInventory = null;

        /*   [HarmonyPatch(typeof(UIStateEditSoldier), "CharacterChangedHandler")]
           internal static class TFTV_UIStateEditSoldier_UpdateSoldierEquipment_Patch
           {

               private static void Postfix(UIStateEditSoldier __instance, GeoCharacter lastCharacter, GeoCharacter newCharacter, bool initial)
               {
                   try
                   {
                       UIInventoryList storage = UIModuleSoldierEquipKludge.StorageList;





                       if (CurrentlyHiddenInv.Keys.Count > 0 || CurrentlyAvailableInv.Keys.Count > 0)
                       {
                           TFTVLogger.Always($"Looking at {newCharacter.DisplayName}");


                          //  UIInventoryList storage = UIModuleSoldierEquipKludge.StorageList;





                           CharacterInventory = newCharacter;
                           storage.Deinit();
                           storage.Init(storage.UnfilteredItems, UIModuleSoldierEquipKludge);

                           CharacterInventory = null;

                       }
                       else 
                       {
                           storage.Deinit();
                           storage.Init(storage.UnfilteredItems, UIModuleSoldierEquipKludge);


                       }



                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }

           }

   */


        /*   [HarmonyPatch(typeof(UIInventoryList), "Init")]
           internal static class TFTV_UIInventoryList_Update_InventoryExperiment_patch
           {
               public static void Prefix(ref IEnumerable<ICommonItem> items, UIModuleSoldierEquip parentModule, UIInventoryList __instance)
               {
                   try
                   {

                       if(__instance.IsStorage && CharacterInventory!=null)
                       {
                           GeoCharacter character = CharacterInventory;

                           int charactersAircraft = LocateSoldier(character);
                           List<int> otherVehiclesAtSameLocation = LocateOtherVehicles(charactersAircraft);

                           List<ICommonItem> commonItems = new List<ICommonItem>(items);

                        //   TFTVLogger.Always($"there are {commonItems.Count} items in the new list, vs the starting list {items.Count()}");

                           if (CurrentlyAvailableInv.Keys.Count > 0)
                           {
                               TFTVLogger.Always($"There are {CurrentlyAvailableInv.Keys.Count()} AircraftShowing inventories");
                            //   List<int> geoVehiclesElsewhere = new List<int>();

                               foreach (int geoVehicleAwayFromCharacter in CurrentlyAvailableInv.Keys)
                               {
                                   if (geoVehicleAwayFromCharacter != charactersAircraft &&
                                       (otherVehiclesAtSameLocation?.Count == 0
                                       || otherVehiclesAtSameLocation?.Count > 0 && !otherVehiclesAtSameLocation.Contains(geoVehicleAwayFromCharacter)))
                                   {
                                       TFTVLogger.Always($"{character?.DisplayName} is not in craft #{geoVehicleAwayFromCharacter} or at an aircraft at its location");

                                       if (!CurrentlyHiddenInv.ContainsKey(geoVehicleAwayFromCharacter))
                                       {
                                           CurrentlyHiddenInv.Add(geoVehicleAwayFromCharacter, new List<ICommonItem>());
                                           TFTVLogger.Always($"Creating new Hidden Inventory list for craft #{geoVehicleAwayFromCharacter}");
                                       }

                                       foreach (ICommonItem geoItem in CurrentlyAvailableInv[geoVehicleAwayFromCharacter])
                                       {

                                         //  ICommonItem commonItem = commonItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == geoItem).FirstOrDefault();

                                           TFTVLogger.Always($"removing {geoItem}");

                                           if (commonItems.Contains(geoItem))
                                           {

                                               commonItems.Remove(geoItem);
                                               CurrentlyHiddenInv[geoVehicleAwayFromCharacter].Add(geoItem);




                                               TFTVLogger.Always($"{geoItem} added to currently hidden list, count {CurrentlyHiddenInv[geoVehicleAwayFromCharacter].Count}");
                                           }
                                           else 
                                           {
                                               TFTVLogger.Always($"Item with guid {geoItem} not found!!!");
                                           }
                                       }

                                       foreach (ICommonItem geoItem in CurrentlyHiddenInv[geoVehicleAwayFromCharacter])
                                       {
                                           CurrentlyAvailableInv[geoVehicleAwayFromCharacter].Remove(geoItem);
                                       }

                                       //   geoVehiclesElsewhere.Add(geoVehicleAwayFromCharacter);
                                   }

                               }


                           }



                           if (charactersAircraft != 0 && CurrentlyHiddenInv.Keys.Count > 0)
                           {
                               if (CurrentlyHiddenInv.Keys.Contains(charactersAircraft))
                               {

                                   TFTVLogger.Always($"{character.DisplayName} is at craft #{charactersAircraft}; adding items to storage");

                                   if (!CurrentlyAvailableInv.ContainsKey(charactersAircraft))
                                   {
                                       CurrentlyAvailableInv.Add(charactersAircraft, new List<ICommonItem>());
                                       TFTVLogger.Always($"Creating new Available Inventory list for craft #{charactersAircraft}");
                                   }


                                   foreach (ICommonItem geoItem in CurrentlyHiddenInv[charactersAircraft])
                                   {
                                      // GeoItem geoItem1 = new GeoItem((ItemDef)Repo.GetDef(geoItem));
                                      // ICommonItem commonItem = geoItem1;//Repo.Instantiate<ICommonItem>(Repo.GetDef(geoItem));

                                       commonItems.Add(geoItem);
                                       CurrentlyAvailableInv[charactersAircraft].Add(geoItem);
                                       TFTVLogger.Always($"{geoItem} added to storage");

                                   }

                                   CurrentlyHiddenInv.Remove(charactersAircraft);



                               }

                               if (otherVehiclesAtSameLocation != null && otherVehiclesAtSameLocation.Count > 0)
                               {
                                   TFTVLogger.Always($"There are other vehicles at the same location");

                                   foreach (int geoVehicleAtSameLocation in otherVehiclesAtSameLocation)

                                   {
                                       if (CurrentlyHiddenInv.Keys.Contains(geoVehicleAtSameLocation))
                                       {
                                           if (!CurrentlyAvailableInv.ContainsKey(geoVehicleAtSameLocation))
                                           {
                                               CurrentlyAvailableInv.Add(geoVehicleAtSameLocation, new List<ICommonItem>());
                                               TFTVLogger.Always($"Creating new Available Inventory list for craft #{geoVehicleAtSameLocation}");
                                           }


                                           foreach (ICommonItem geoItem in CurrentlyHiddenInv[geoVehicleAtSameLocation])
                                           {
                                           //    GeoItem geoItem1 = new GeoItem((ItemDef)Repo.GetDef(geoItem));
                                           //    ICommonItem commonItem = geoItem1;//Repo.Instantiate<ICommonItem>(Repo.GetDef(geoItem));

                                               commonItems.Add(geoItem);
                                               CurrentlyAvailableInv[geoVehicleAtSameLocation].Add(geoItem);
                                               TFTVLogger.Always($"{geoItem} added to storage");


                                           }

                                           CurrentlyHiddenInv.Remove(geoVehicleAtSameLocation);
                                       }
                                   }
                               }
                           }

                           TFTVLogger.Always($"original list count vs old count {items.Count()} | {commonItems.Count()}");

                           items = commonItems;
                       }

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }

           }*/
        //  internal static List<ItemDef> CurrentlyShowingItems = new List<ItemDef>();
        /*  internal static Sprite TestPic = Helper.CreateSpriteFromImageFile("Aircraft_Inventory.png");




          [HarmonyPatch(typeof(UIInventorySlot), "Update")]
          internal static class TFTV_UIInventorySlot_Update_InventoryExperiment_patch
          {
              public static void Postfix(UIInventorySlot __instance, ICommonItem ____item)
              {
                  try
                  {


                      // TFTVLogger.Always($"default color is {__instance.Highlight.color.}");

                      if (CurrentlyAvailableInv.Count > 0)
                      {
                          foreach (List<ICommonItem> geoItemList in CurrentlyAvailableInv.Values)
                          {
                              foreach (ICommonItem geoItem in geoItemList)
                              {


                                  if (____item != null && __instance.ParentList.IsStorage && geoItem == ____item)
                                  {

                                      __instance.NotProficientNode.GetComponent<Image>().overrideSprite = TestPic;
                                      //  __instance.NotProficientNode.GetComponent<Image>().transform.localScale = new Vector3(2f, 2f, 2f); 
                                      //  __instance.NotProficientNode.enabled = true;
                                      __instance.NotProficientNode.gameObject.SetActive(true);

                                      if (__instance.NotProficientNode.gameObject.GetComponent<UITooltipText>() == null)

                                      {
                                          __instance.NotProficientNode.gameObject.AddComponent<UITooltipText>().TipText = "This item was unequipped by someone on a plane, and can only be equipped by someone at the same location";
                                          // __instance.gameObject.AddComponent<Text>().text = "just testing";
                                      }

                                      __instance.Highlight.color = red;
                                      // ColoredItems.Add(____item);
                                      return;
                                  }
                                  else
                                  {
                                      if (__instance.Highlight.color == red)
                                      {
                                          __instance.Highlight.color = new Color(1, 1, 1);
                                          __instance.NotProficientNode.GetComponent<Image>().overrideSprite = null;

                                      }

                                  }
                              }
                          }
                      }
                      else
                      {
                          if (__instance.Highlight.color == red)
                          {
                              __instance.Highlight.color = new Color(1, 1, 1);

                          }

                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }

          }*/

        internal static bool CheckPhoenixBasePresent(int vehicleID)
        {
            try
            {


                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (vehicleID == 0)
                {
                    return true;

                }

                if (controller.PhoenixFaction.Vehicles.FirstOrDefault(v => v.VehicleID == vehicleID)?.CurrentSite?.GetComponent<GeoPhoenixBase>() != null)
                {
                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }






        }

        /* [HarmonyPatch(typeof(UIInventoryList), "AddItem")]
         internal static class TFTV_UIInventoryList_AddItem_InventoryExperiment_patch
         {
             public static bool Prefix(UIInventoryList __instance, ICommonItem item, UIInventorySlot slot, UIInventoryList sourceList)
             {
                 try
                 {
                     GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                     GeoCharacter character = CharacterInventory ?? hookToCharacter;

                     int charactersAircraft = LocateSoldier(character);

                     // GeoItem geoItem = item as GeoItem;

                     if (charactersAircraft != 0 && !item.ItemDef.Tags.Contains(Shared.SharedGameTags.AmmoTag))
                     {
                         if (CheckPhoenixBasePresent(charactersAircraft))
                         {
                             TFTVLogger.Always($"{item.ItemDef.name} from {character.DisplayName} is going to be added to an inventory list. Is it storage? {__instance.IsStorage}");

                             return true;
                         }

                         if (__instance.IsStorage)
                         {
                             if (ItemsEquippedFromStorage.Contains(item))
                             {

                                 ItemsEquippedFromStorage.Remove(item);
                                 TFTVLogger.Always($"{item.ItemDef.name} came from storage, so it's not added to Aircraft Inventory. Count {ItemsEquippedFromStorage.Count}");

                                 return true;
                             }
                             else

                             {
                                 __instance.AllowStacking = false;
                                 MethodInfo TryStripAmmo = typeof(UIInventoryList).GetMethod("TryStripAmmo", BindingFlags.Instance | BindingFlags.NonPublic);
                                 MethodInfo AddRowIfNeeded = typeof(UIInventoryList).GetMethod("AddRowIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
                                 FieldInfo slotItemChangedEventField = AccessTools.Field(typeof(UIInventoryList), "OnSlotItemChanged");
                                 TryStripAmmo.Invoke(__instance, new object[] { item, null });

                                 if (slot == null)
                                 {
                                     slot = __instance.GetFirstAvailableSlot(item.ItemDef);
                                 }

                                 slot.Item = item;

                                 AddRowIfNeeded.Invoke(__instance, null);

                                 SlotItemChangedHandler slotItemChangedHandler = (SlotItemChangedHandler)slotItemChangedEventField.GetValue(__instance);
                                 slotItemChangedHandler?.Invoke(slot);

                                 if (CurrentlyAvailableInv.Keys.Contains(charactersAircraft))
                                 {
                                     CurrentlyAvailableInv[charactersAircraft].Add(item);
                                     TFTVLogger.Always($"craft #{charactersAircraft} not a PX base! {item.ItemDef.name} from {character.DisplayName} added to Currently Showing inv, count {CurrentlyAvailableInv[charactersAircraft].Count}");
                                 }
                                 else
                                 {
                                     TFTVLogger.Always($"craft #{charactersAircraft} not at a PX base! {item.ItemDef.name} from {character.DisplayName} added to Currently Showing inv. Creating new item list.");
                                     CurrentlyAvailableInv.Add(charactersAircraft, new List<ICommonItem>() { item});

                                 }
                                 //   MethodInfo TryStripAmmo = typeof(UIInventoryList).GetMethod("TryStripAmmo", BindingFlags.Instance | BindingFlags.NonPublic);
                                 //   TryStripAmmo.Invoke(__instance, new object[] { item, null });

                                // CurrentlyShowingItems.Add(item.ItemDef);

                                 return false;

                             }
                         }

                         if (!__instance.IsStorage && CurrentlyAvailableInv.Keys.Count > 0 && CurrentlyAvailableInv.Keys.Contains(charactersAircraft) && CurrentlyAvailableInv[charactersAircraft].Any(i => i == item))
                         {
                             CurrentlyAvailableInv[charactersAircraft].Remove(item);
                             TFTVLogger.Always($"{item.ItemDef.name} is coming from the craft #{charactersAircraft} inventory; current inventory count is {CurrentlyAvailableInv[charactersAircraft].Count} ");

                             return true;
                         }
                         else if (!__instance.IsStorage && CurrentlyAvailableInv.Keys.Count > 0)
                         {
                             List<int> otherVehiclesAtSameLocation = LocateOtherVehicles(charactersAircraft);

                             if (otherVehiclesAtSameLocation != null && otherVehiclesAtSameLocation.Count > 0)
                             {
                                 TFTVLogger.Always($"There are other vehicles at the same location");

                                 foreach (int geoVehicleAtSameLocation in otherVehiclesAtSameLocation)
                                 {
                                     if (CurrentlyAvailableInv.Keys.Contains(geoVehicleAtSameLocation))
                                     {
                                         if (CurrentlyAvailableInv[geoVehicleAtSameLocation].Any(i => i == item))
                                         {
                                             CurrentlyAvailableInv[geoVehicleAtSameLocation].Remove(item);
                                             TFTVLogger.Always($" craft #{geoVehicleAtSameLocation} is at the same location as craft #{charactersAircraft} and has {item.ItemDef.name}. Count {CurrentlyAvailableInv[geoVehicleAtSameLocation].Count}");
                                             return true;
                                         }
                                     }
                                 }
                             }
                         }

                         if (!__instance.IsStorage)
                         {

                             ItemsEquippedFromStorage.Add(item);
                             TFTVLogger.Always($"{item.ItemDef.name} coming from storage; saving to ItemEquippedFromStorage. Count{ItemsEquippedFromStorage.Count}");

                         }

                         return true;
                     }
                     TFTVLogger.Always($"{item.ItemDef.name} is going to be added to storage {__instance.IsStorage}");
                     return true;

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }*/






        public static Dictionary<int, Dictionary<string, List<string>>> CharacterLoadouts = new Dictionary<int, Dictionary<string, List<string>>>();

        public static bool HelmetsOff;

        public static PhoenixGeneralButton HelmetToggle = null;
        public static PhoenixGeneralButton UnequipAll = null;
        public static PhoenixGeneralButton SaveLoadout = null;
        public static PhoenixGeneralButton LoadLoadout = null;

        [HarmonyPatch(typeof(EditUnitButtonsController), "Awake")]
        internal static class TFTV_EditUnitButtonsController_Awake_ToggleHelmetButton_patch
        {
            private static bool toggleState = false;  // Initial toggle state
            private static readonly string armourItems = "ArmourItems";
            private static readonly string equipmentItems = "EquipmentItems";
            private static readonly string inventoryItems = "InventoryItems";

            public static void Postfix(EditUnitButtonsController __instance)
            {
                try
                {
                    TFTVLogger.Always($"checking");

                    PhoenixGeneralButton helmetToggleButton = UnityEngine.Object.Instantiate(__instance.EditButton, __instance.transform);
                    helmetToggleButton.gameObject.AddComponent<UITooltipText>().TipText = "Toggles helmet visibility on/off.";
                    // TFTVLogger.Always($"original icon position {newPhoenixGeneralButton.transform.position}, edit button position {__instance.EditButton.transform.position}");
                    helmetToggleButton.transform.position += new Vector3(-50, -35, 0);

                    // TFTVLogger.Always($"new icon position {newPhoenixGeneralButton.transform.position}");

                    PhoenixGeneralButton unequipAllPhoenixGeneralButton = UnityEngine.Object.Instantiate(__instance.EditButton, __instance.transform);
                    unequipAllPhoenixGeneralButton.gameObject.AddComponent<UITooltipText>().TipText = "Unequips all the items currently equipped by the operative.";
                    unequipAllPhoenixGeneralButton.transform.position = helmetToggleButton.transform.position + new Vector3(0, -100, 0);

                    PhoenixGeneralButton saveLoadout = UnityEngine.Object.Instantiate(__instance.EditButton, __instance.transform);
                    saveLoadout.transform.position = unequipAllPhoenixGeneralButton.transform.position + new Vector3(0, -100, 0);
                    saveLoadout.gameObject.AddComponent<UITooltipText>().TipText = "Saves the current loadout of the operative.";

                    PhoenixGeneralButton loadLoadout = UnityEngine.Object.Instantiate(__instance.EditButton, __instance.transform);
                    loadLoadout.transform.position = saveLoadout.transform.position + new Vector3(0, -100, 0);
                    loadLoadout.gameObject.AddComponent<UITooltipText>().TipText = "Loads the previously saved loadout for this operative.";


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
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void LoadLoadoutButtonClicked()
            {
                try
                {
                    GeoCharacter character = hookToCharacter;

                    if (!CharacterLoadouts.ContainsKey(character.Id)) 
                    {
                        return;
                    }


                    UnequipButtonClicked();
                    UIInventoryList storage = UIModuleSoldierEquipKludge.StorageList;

                    Predicate<TacticalItemDef> filter = null;


                    storage.SetFilter(filter);
                    //    UIModuleSoldierEquipKludge.RefreshSideButtons();


                    foreach (string armor in CharacterLoadouts[character.Id][armourItems])
                    {

                        ICommonItem item = storage.UnfilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == armor).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == armor).FirstOrDefault();
                        if (item != null && UIModuleSoldierEquipKludge.ArmorList.CanAddItem(item))
                        {

                            TFTVLogger.Always($"armor item is {item}");
                            UIModuleSoldierEquipKludge.ArmorList.AddItem(item.GetSingleItem());



                            /*     UIInventorySlot slot = UIModuleSoldierEquipKludge.ArmorList.Slots.FirstOrDefault(s => s.Item?.ItemDef == item.ItemDef);

                                 foreach(UIInventorySlot uIInventorySlot in UIModuleSoldierEquipKludge.ArmorList.Slots) 
                                 {
                                     if (uIInventorySlot.Item != null)
                                     {
                                         TFTVLogger.Always($"slot has {uIInventorySlot?.Item}");
                                     }
                                 }


                                 if (slot != null)
                                 {
                                     TFTVLogger.Always($"Found slot {slot.Item.ItemDef.name}");
                                 }

                                 UIModuleSoldierEquipKludge.ArmorList.TryLoadAmmo(item, slot, storage);*/
                            storage.RemoveItem(item.GetSingleItem(), null);


                            //storage.RemoveItem(, null);
                        }

                    }

                    foreach (string equipment in CharacterLoadouts[character.Id][equipmentItems])
                    {

                        ICommonItem item = storage.UnfilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == equipment).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == equipment).FirstOrDefault();
                        TFTVLogger.Always($"equipment item is {item}");

                        UIModuleSoldierEquipKludge.ReadyList.AddItem(item.GetSingleItem());
                        storage.RemoveItem(item.GetSingleItem(), null);
                       /* UIInventorySlot slot = UIModuleSoldierEquipKludge.ReadyList.Slots.FirstOrDefault(s => s.Item.ItemDef == item.ItemDef);

                        if (slot != null)
                        {
                            TFTVLogger.Always($"Found slot {slot.Item.ItemDef.name}");
                        }*/


                      //  if (item != null && UIModuleSoldierEquipKludge.ReadyList.CanAddItem(item))
                      //  {

                          /*  TacticalItemDef[] compatibleAmmunition = (item.ItemDef as EquipmentDef).CompatibleAmmunition;
                            foreach (TacticalItemDef tacticalItemDef in compatibleAmmunition)
                            {
                                TFTVLogger.Always("Got here");
                                bool foundSlot= false;
                                foreach (UIInventorySlot storageSlot in storage.Slots)
                                {
                                    if (!storageSlot.Empty && !(storageSlot.Item.ItemDef != tacticalItemDef) && storage.TryLoadItemWithItem(item, storageSlot.Item, storageSlot))
                                    {
                                        slot?.UpdateItem();
                                        TFTVLogger.Always("Got here2");
                                        foundSlot = true;
                                        break;
                                    }
                                }
                                TFTVLogger.Always("Got here b");

                                if (!foundSlot)
                                {

                                    foreach (ICommonItem unfilteredItem in storage.UnfilteredItems)
                                    {
                                        if (!(unfilteredItem.ItemDef != tacticalItemDef) && storage.TryLoadItemWithItem(item, unfilteredItem, null))
                                        {
                                            TFTVLogger.Always("Got here2b");

                                            slot?.UpdateItem();
                                            if (unfilteredItem.CommonItemData.IsEmpty())
                                            {
                                                storage.UnfilteredItems.Remove(unfilteredItem);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }*/

                         //   UIModuleSoldierEquipKludge.ReadyList.TryLoadAmmo(item, slot, storage);
                            //   TFTVLogger.Always($" ammo: {item?.CommonItemData?.Ammo == null} charge: {item?.CommonItemData?.Ammo?.CurrentCharges >= item?.ItemDef?.ChargesMax} storage: {storage == null} allowstacking: {!storage?.AllowStacking}");




                            //  TFTVLogger.Always("this worked too");
                      //  }

                    }

                    foreach (string inventory in CharacterLoadouts[character.Id][inventoryItems])
                    {



                        ICommonItem item = storage.UnfilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == inventory).FirstOrDefault() ?? storage.FilteredItems.Where((ICommonItem ufi) => ufi.ItemDef.Guid == inventory).FirstOrDefault();

                        if (item != null && UIModuleSoldierEquipKludge.InventoryList.CanAddItem(item))
                        {
                            TFTVLogger.Always($"inventory item is {item}");


                            UIModuleSoldierEquipKludge.InventoryList.AddItem(item.GetSingleItem());
                            UIInventorySlot slot = UIModuleSoldierEquipKludge.InventoryList.Slots.FirstOrDefault(s => s.Item == item);
                        //    UIModuleSoldierEquipKludge.InventoryList.TryLoadAmmo(item.GetSingleItem(), slot, storage);
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
                    GeoCharacter character = hookToCharacter;

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
                        /*  if (uIModuleSoldierCustomization != null)
                          {
                              uIModuleSoldierCustomization.HideHelmetToggle.isOn = true;

                          }*/
                        helmetToggleButton.transform.GetChildren().First().GetChildren().Where(t => t.name.Equals("UI_Icon")).FirstOrDefault().GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("TFTV_helmet_on_icon.png");
                        HelmetsOff = true;
                        // TFTVLogger.Always($"{uIModuleSoldierCustomization.HideHelmetToggle.isOn}");

                    }
                    else
                    {

                        /*  if (uIModuleSoldierCustomization != null)
                          {
                              uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                          }*/
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
                    if (UIModuleSoldierEquipKludge != null && hookToCharacter != null)
                    {
                        GeoCharacter character = hookToCharacter;

                        List<GeoItem> armorItems = new List<GeoItem>();
                        List<GeoItem> inventoryItems = new List<GeoItem>();
                        List<GeoItem> equipmentItems = new List<GeoItem>();

                        armorItems.AddRange(character.ArmourItems.
                            Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.AnuMutationTag)).
                            Where(a => !a.ItemDef.Tags.Contains(Shared.SharedGameTags.BionicalTag)).
                            Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("Attachment")).
                            Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("BackPack")).
                            Where(a => !a.ItemDef.RequiredSlotBinds[0].RequiredSlot.name.Contains("MechArm"))
                            );
                        equipmentItems.AddRange(character.EquipmentItems);
                        inventoryItems.AddRange(character.InventoryItems);

                        foreach (GeoItem item in inventoryItems)
                        {
                            // TFTVLogger.Always($"{item.ItemDef.name} in Inventory");
                            UIModuleSoldierEquipKludge.StorageList.AddItem(item);
                            UIModuleSoldierEquipKludge.InventoryList.RemoveItem(item, null);
                        }

                        foreach (GeoItem item in equipmentItems)
                        {
                            // TFTVLogger.Always($"{item.ItemDef.name} in Equipment");
                            UIModuleSoldierEquipKludge.StorageList.AddItem(item);
                            UIModuleSoldierEquipKludge.ReadyList.RemoveItem(item, null);
                        }

                        foreach (GeoItem item in armorItems)
                        {
                            //  TFTVLogger.Always($"{item.ItemDef.name} in Armor. {item.ItemDef?.RequiredSlotBinds[0].RequiredSlot?.name}");
                            UIModuleSoldierEquipKludge.StorageList.AddItem(item);
                            UIModuleSoldierEquipKludge.ArmorList.RemoveItem(item, null);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        public static UIModuleSoldierEquip UIModuleSoldierEquipKludge = null;

     /*   [HarmonyPatch(typeof(UIInventoryList), "TryLoadItemWithItem")]
        internal static class TFTV_UIInventoryList_TryLoadItemWithItem_Patch
        {
            private static readonly ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
            private static void Postfix(bool __result, ICommonItem item, ICommonItem ammoItem, UIInventorySlot ammoSlot)
            {
                try
                {
                    TFTVLogger.Always($"result is {__result}. item is {item} ammoItem is {ammoItem}. ammonslot is {ammoSlot?.name}");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/

    


    //Patch to show correct encumbrance
    [HarmonyPatch(typeof(UIModuleSoldierEquip), "RefreshWeightSlider")]
    internal static class TFTV_UIModuleSoldierEquip_RefreshWeightSlider_Patch
    {
        private static readonly ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
        private static void Prefix(ref int maxWeight, UIModuleSoldierEquip __instance)
        {
            try

            {
                UIModuleSoldierEquipKludge = __instance;

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

    /*[HarmonyPatch(typeof(CorruptionSettingsDef), "CalculateCorruptionShaderValue")]

    internal static class TFTV_UI_CorruptionSettingsDef_DeliriumFace_patch
    {
        private static void Prefix(float corruption01)
        {
            try
            {
                if (hookToCharacter != null)
                {
                    GeoCharacter geoCharacter = hookToCharacter;
                    if (geoCharacter.CharacterStats.Corruption > 0)
                    {
                        // corruption01 = ((geoCharacter.CharacterStats.Corruption-(geoCharacter.Fatigue.Stamina/10))/ geoCharacter.CharacterStats.WillPoints.IntMax)*0.25f;

                      //  TFTVLogger.Always("This character is " + geoCharacter.DisplayName + " has CorruptionProgressRel of " + geoCharacter.CharacterStats.CorruptionProgressRel
                      //      + " Delirium of " + geoCharacter.CharacterStats.Corruption + " and WP of " + geoCharacter.CharacterStats.WillPoints.IntMax + " and floatcorruption is " + corruption01);

                    }

                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }*/



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
                HelmetsOff = false;
                //  TFTVLogger.Always("Trying to set helmets off if character has mutated head");
                if (hookToCharacter != null && (hookToCharacter.TemplateDef.IsHuman || hookToCharacter.TemplateDef.IsMutoid))
                {
                    //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human or mutoid");
                    if (hookToCharacter != null && (!hookToCharacter.TemplateDef.IsHuman || hookToCharacter.IsMutoid))
                    {
                        //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is mutoid");
                        uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                        uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;

                    }
                    else if (hookToCharacter != null && hookToCharacter.TemplateDef.IsHuman)
                    {
                        //    TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human");
                        bool hasAugmentedHead = false;
                        foreach (GeoItem bionic in (hookToCharacter.ArmourItems))
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

                        if (HelmetsOff)
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



    //This changes display of Delirium bar in personnel edit screen to show current Delirium value vs max delirium value the character can have
    // taking into account ODI level and bionics
    [HarmonyPatch(typeof(UIModuleCharacterProgression), "SetStatusesPanel")]
    internal static class BG_UIModuleCharacterProgression_SetStatusesPanel_patch
    {

        private static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
        {
            try
            {
                hookToCharacter = ____character;

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
                    corruptionSliderTip.TipText = $"Delirium is gained in Tactical missions. Current max Delirium is {TFTVDelirium.CurrentDeliriumLevel(____character.Faction.GeoLevel)}.";
                    __instance.CorruptionStatText.text = $"{Mathf.RoundToInt(delirium)}/{Mathf.RoundToInt(__instance.CorruptionSlider.maxValue)}";

                    int num = (int)(float)____character.Fatigue.Stamina;
                    int num2 = (int)(float)____character.Fatigue.Stamina.Max;
                    __instance.StaminaSlider.minValue = 0f;
                    __instance.StaminaSlider.maxValue = num2;
                    __instance.StaminaSlider.value = num;


                    UITooltipText staminaTextTip = __instance.StaminaStatText.gameObject.AddComponent<UITooltipText>();
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

                    staminaTextTip.TipText = $"Character's current Stamina is reducing the effects of Delirium on Willpower by {TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}";

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }

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
            TFTVConfig config = TFTVMain.Main.Config;
            if (config.SkipMovies)
            {
                try
                {
                    if (____sourcePlaybackDef == null)
                    {
                        return;
                    }
                    if (____sourcePlaybackDef.ResourcePath.Contains("LandingSequences"))
                    {
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
