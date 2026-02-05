using Base;
using Base.Core;
using Base.Defs;
using Base.UI;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Personnel
{
    internal class Loadouts
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        /// <summary>
        /// Patches to add toggle helment and loadouts buttons
        /// </summary>      
        /// 

        public static PhoenixGeneralButton HelmetToggle = null;
        public static PhoenixGeneralButton UnequipAll = null;
        public static PhoenixGeneralButton SaveLoadout = null;
        public static PhoenixGeneralButton LoadLoadout = null;

        public static Dictionary<int, Dictionary<string, List<string>>> CharacterLoadouts = new Dictionary<int, Dictionary<string, List<string>>>();

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

        public static void ShowAndHideHelmetAndLoadoutButtons(UIModuleActorCycle uIModuleActorCycle)
        {
            try
            {
                if (uIModuleActorCycle.CurrentUnit != null)
                {
                    //  TFTVLogger.Always($"Actually here; {____parentModule.CurrentState}");

                    switch (uIModuleActorCycle.CurrentState)
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
                            foreach (GeoItem bionic in uIModuleActorCycle?.CurrentCharacter?.ArmourItems)
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

                            ShadeMutationBionics(uIModuleActorCycle);

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

                    if (uIModuleActorCycle.CurrentState == UIModuleActorCycle.ActorCycleState.SubmenuSection)//EditUnitButtonsController.CustomizeButton.gameObject.activeInHierarchy)
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

                    if (uIModuleActorCycle.CurrentCharacter != null && (CharacterLoadouts == null || CharacterLoadouts != null && !CharacterLoadouts.ContainsKey(uIModuleActorCycle.CurrentCharacter.Id)))
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



        [HarmonyPatch(typeof(EditUnitButtonsController), nameof(EditUnitButtonsController.Awake))]
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

        [HarmonyPatch(typeof(UIInventoryList), nameof(UIInventoryList.GetTotalUsedStorage))]
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


                    helmetToggleButton.PointerClicked += () => ShowWithoutHelmet.ToggleButtonClicked(helmetToggleButton);
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

            //LoadoutService.EquipAllFromButton(charactersOnSite, CharacterLoadouts);

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
    }
}
