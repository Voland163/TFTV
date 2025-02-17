using Base;
using Base.Core;
using Base.Defs;
using Base.Input;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Manufacturing;
using PhoenixPoint.Geoscape.View.ViewControllers.PhoenixBase;
using PhoenixPoint.Geoscape.View.ViewControllers.Research;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewControllers.VehicleRoster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Levels.GeoMap;
using Text = UnityEngine.UI.Text;

namespace TFTV
{
    internal class TFTVDragandDropFunctionality
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal class Research
        {
            [HarmonyPatch(typeof(UIModuleResearch))]
            public static class UIModuleResearchPatch
            {
                // Patch the Start method to add drag-and-drop event handlers

                [HarmonyPostfix]
                [HarmonyPatch("Awake")]
                public static void Awake_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        __instance.QueueScrollRect.AdditionalVisibleRows = 50;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                [HarmonyPostfix]
                [HarmonyPatch("Init")]
                public static void Init_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                [HarmonyPostfix]
                [HarmonyPatch("AddToQueue")]
                public static void AddToQueue_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                // Patch the SetupQueue method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("SetupQueue")]
                public static void SetupQueue_Postfix(UIModuleResearch __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void AddDragHandlers(ResearchQueueItem item, UIModuleResearch instance)
                {
                    try
                    {
                        if (item.gameObject.GetComponent<DragHandler>() != null)
                        {
                            // DragHandler handler = item.gameObject.GetComponent<DragHandler>();
                            // UnityEngine.Object.Destroy(handler);
                            return;
                        }

                        var dragHandler = item.gameObject.AddComponent<DragHandler>();
                        dragHandler.Init(item, instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private ResearchQueueItem _item;
                private UIModuleResearch _roster;
                private RectTransform _originalParent;
                private int _originalIndex;
                private ScrollRect _scrollRect;

                public void Init(ResearchQueueItem item, UIModuleResearch roster)
                {
                    try
                    {
                        _roster = roster;
                        _item = item;
                        _scrollRect = _roster.QueueScrollRect.Scroll;
                        _roster.QueueScrollRect.AdditionalVisibleRows = 50;

                        Debug.Log($"DragHandler initialized for slot: {item.ResearchName.text}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        // TFTVLogger.Always($"OnBeginDrag called for {_item.ResearchName.text}");
                        _originalParent = transform.parent as RectTransform;
                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<ResearchElement> researchElementQueue = phoenixFaction.Research.ResearchQueue;

                        for (int i = 0; i < researchElementQueue.Count; i++)
                        {
                            ResearchElement manufactureQueueItem = researchElementQueue[i];

                            if (manufactureQueueItem == _item.Research)
                            {
                                _originalIndex = i;
                                break;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent);
                        transform.SetParent(_originalParent.parent);

                        // Disable the ScrollRect to prevent it from interfering with the drag
                        /*  if (_scrollRect != null)
                          {
                              _scrollRect.enabled = false;
                          }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }



                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {

                        // TFTVLogger.Always($"OnDrag for {_item.ResearchName.text}");
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        //   TFTVLogger.Always($"OnEndDrag called for {_item.ResearchName.text}");

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        // TFTVLogger.Always($"_originalIndex: {_originalIndex} new index {newIndex}, phoenixFaction.Manufacture.Queue.Count {phoenixFaction.Manufacture.Queue.Count}");

                        if (newIndex != _originalIndex)
                        {
                            ResearchElement item = _item.Research;
                            phoenixFaction.Research.ResearchQueue.Remove(item);
                            phoenixFaction.Research.ResearchQueue.Insert(newIndex, item);

                            // TFTVLogger.Always($"Moved {item.ResearchID} from index {_originalIndex} to {newIndex}");
                        }

                        UIModuleResearch uIModuleResearch = phoenixFaction.GeoLevel.View.GeoscapeModules.ResearchModule;

                        MethodInfo method = uIModuleResearch.GetType().GetMethod("SetupQueue", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(uIModuleResearch, null);

                        // Re-enable the ScrollRect after the drag ends
                        /* if (_scrollRect != null)
                         {
                             _scrollRect.enabled = true;
                         }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);
                    GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<ResearchElement> researchQueue = phoenixFaction.Research.ResearchQueue;

                    // TFTVLogger.Always($"There {researchQueue.Count} items in Manufacturing queue");

                    UIModuleResearch uIModuleResearch = phoenixFaction.GeoLevel.View.GeoscapeModules.ResearchModule;

                    //  TFTVLogger.Always($"localPosition: {localPosition}");

                    for (int i = 0; i < researchQueue.Count; i++)
                    {
                        ResearchElement researchElement = researchQueue[i];

                        foreach (var child in uIModuleResearch.QueueScrollRect.Scroll.content.GetComponentsInChildren<ResearchQueueItem>())
                        {
                            if (child.Research == researchElement && child != _item)
                            {
                                /* TFTVLogger.Always($"item: {researchElement.ResearchID}, pos: {i}, " +
                                     $"found child {child.ResearchName.text}, at pos {child.transform}, " +
                                     $"RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position): {RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position)}" +
                                     $"\nlocalPosition.y > child.transform.localPosition.y {localPosition.y > child.transform.localPosition.y}");*/

                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position))
                                {
                                    return i;
                                }
                            }
                        }
                    }

                    return _originalIndex;
                }
            }



        }



        internal class Manufacturing
        {
          /*  public static void ClearInternalData()
            {
                try 
                { 
                
                
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }*/

            private static readonly string _amountInInventoryTextObject = "AmountInInventory";
            private static readonly string _techCostTextObject = "TextContainerTechCost";
            private static readonly string _matsCostTextObject = "TextContainerMatsCost";
            private static readonly string _matsIconObject = "IconTech";
            private static readonly string _techIconObject = "IconMats";

            private static readonly ViewElementDef _matsViewDef = DefCache.GetDef<ViewElementDef>("Materials_ResourceDef");
            private static readonly ViewElementDef _techViewDef = DefCache.GetDef<ViewElementDef>("Tech_ResourceDef");

           // private static bool CheckedManufacturingForNewItems = false;
            
            [HarmonyPatch(typeof(GeoPhoenixFaction), "OnNewManufacturableItemsAdded")]
            public static class GeoPhoenixFaction_OnNewManufacturableItemsAdded_Patch
            {
                public static bool Prefix(GeoPhoenixFaction __instance, ManufacturableItem item)
                {
                    try
                    {
                        if (item.Tags.Contains(DefCache.GetDef<GameTagDef>("AmmoItem_TagDef"))) 
                        {
                         //   TFTVLogger.Always($"New manufacturable Ammo item: {item.Name.Localize()}");
                            return false;
                        }

                       // CheckedManufacturingForNewItems = false;

                       // TFTVLogger.Always($"New manufacturable item: {item.Name.Localize()}");
                        return true;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

           

           




            private static GameObject AddManufactureAmmoButton(GeoManufactureItem geoManufactureItem,
                GeoFaction faction, ItemDef ammoItemDef)
            {
                try
                {
                    ResourcePack cost = ammoItemDef.ManufacturePrice;
                    bool enoughMats = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Materials));
                    bool enoughTech = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Tech));

                    GameObject originalButton = geoManufactureItem.AddToQueueButton.gameObject;
                    Transform transform = geoManufactureItem.CurrentlyOwnedQuantityText.transform;
                    GameObject manufactureAmmoButton = UnityEngine.Object.Instantiate(originalButton, transform);

                    manufactureAmmoButton.name = "ManufactureAmmoButton";


                    // Set the position slightly to the left
                    RectTransform rectTransform = manufactureAmmoButton.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(125, 100);
                    rectTransform.anchoredPosition += new Vector2(-135, -50); // Adjust the offset as needed

                    manufactureAmmoButton.GetComponent<HorizontalLayoutGroup>().enabled = false;
                    manufactureAmmoButton.GetComponent<ContentSizeFitter>().enabled = false;
                  //  manufactureAmmoButton.GetComponent<UITooltipText>().UpdateText("MANUFACTURE AMMO OR CHARGES FOR THIS ITEM");

                    GameObject textAmountInventoryObject = new GameObject(_amountInInventoryTextObject, typeof(Text), typeof(RectTransform));
                    Text textInventoryAmount = textAmountInventoryObject.GetComponent<Text>();
                    RectTransform textRect = textAmountInventoryObject.GetComponent<RectTransform>();
                    textRect.anchoredPosition = new Vector2(-10, 30);

                    textInventoryAmount.color = Color.white;
                    textInventoryAmount.font = geoManufactureItem.CurrentlyOwnedQuantityText.font;
                    textInventoryAmount.fontSize = geoManufactureItem.CurrentlyOwnedQuantityText.fontSize - 4;
                    textInventoryAmount.text = ammoItemDef.GetQuantity(faction, true).ToString();
                    textAmountInventoryObject.transform.SetParent(rectTransform, false);

                    GameObject iconTechContainer = new GameObject(_techIconObject, typeof(Image), typeof(RectTransform));
                    Image imageTech = iconTechContainer.GetComponent<Image>();
                    imageTech.sprite = _techViewDef.SmallIcon;
                    imageTech.color = _techViewDef.Color;

                    RectTransform iconTechRect = iconTechContainer.GetComponent<RectTransform>();
                    iconTechRect.sizeDelta = new Vector2(30, 30);
                    iconTechRect.anchoredPosition = new Vector2(-50, -60);
                    iconTechContainer.transform.SetParent(rectTransform, false);

                    GameObject textContainerTech = new GameObject(_techCostTextObject, typeof(Text), typeof(RectTransform));
                    Text textTech = textContainerTech.GetComponent<Text>();
                    RectTransform textTechRect = textContainerTech.GetComponent<RectTransform>();
                    textTechRect.anchoredPosition = new Vector2(20, -85);
                    //  textTechRect.sizeDelta = new Vector2(40, 40);

                    if (enoughTech)
                    {   
                        textTech.color = _techViewDef.Color;
                    }
                    else
                    {
                        textTech.color = Color.red;
                    }

                    textTech.font = geoManufactureItem.CurrentlyOwnedQuantityText.font;
                    textTech.fontSize = geoManufactureItem.CurrentlyOwnedQuantityText.fontSize - 10;
                    textTech.text = ammoItemDef.ManufacturePrice.FirstOrDefault(ru => ru.Type == ResourceType.Tech).Value.ToString();
                    // textTech.alignment = TextAnchor.MiddleRight;
                    textContainerTech.transform.SetParent(rectTransform, false);

                    GameObject iconMatsContainer = new GameObject(_matsIconObject, typeof(Image), typeof(RectTransform));
                    Image imageMats = iconMatsContainer.GetComponent<Image>(); 
                    imageMats.sprite = _matsViewDef.SmallIcon;
                    imageMats.color = _matsViewDef.Color;

                    RectTransform iconMatsRect = iconMatsContainer.GetComponent<RectTransform>();
                    iconMatsRect.sizeDelta = new Vector2(28, 30);
                    iconMatsRect.anchoredPosition = new Vector2(25, -60);
                    iconMatsContainer.transform.SetParent(rectTransform, false);

                    GameObject textContainerMats = new GameObject(_matsCostTextObject, typeof(Text), typeof(RectTransform));
                    Text textMats = textContainerMats.GetComponent<Text>();
                    RectTransform textMatsRect = textContainerMats.GetComponent<RectTransform>();
                    textMatsRect.anchoredPosition = new Vector2(87, -85);
                    //   textMatsRect.sizeDelta = new Vector2(40, 40);   


                    textMats.font = geoManufactureItem.CurrentlyOwnedQuantityText.font;
                    textMats.fontSize = geoManufactureItem.CurrentlyOwnedQuantityText.fontSize - 10;
                    textMats.text = ammoItemDef.ManufacturePrice.FirstOrDefault(ru => ru.Type == ResourceType.Materials).Value.ToString();
                    //   textMats.alignment = TextAnchor.MiddleRight;
                    textContainerMats.transform.SetParent(rectTransform, false);

                    if (enoughMats)
                    {
                       
                        textMats.color = _matsViewDef.Color;
                    }
                    else
                    {
                        textMats.color = Color.red;
                    }

                    //  GameObject textContainer = UnityEngine.Object.Instantiate(__instance.CurrentlyOwnedQuantityTextContainer, clonedButton.transform);
                    //  textContainer.GetComponentInChildren<Text>().text = ammoItemDef.GetQuantity(____faction, true).ToString();

                    // Set a different sprite
                    Image actionIcon = manufactureAmmoButton.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Icon");
                    actionIcon.SetNativeSize();
                    actionIcon.sprite = ammoItemDef.ViewElementDef.InventoryIcon; // Set to a different sprite

                
                    return manufactureAmmoButton;

                }
                catch (Exception e)
                {

                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void UpdateManufactureButton(GameObject manufactureAmmoButton, GeoFaction faction, ItemDef ammoItemDef)
            {
                try
                {

                    ResourcePack cost = ammoItemDef.ManufacturePrice;
                    bool enoughMats = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Materials));
                    bool enoughTech = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Tech));

                    manufactureAmmoButton.SetActive(true);
                    Image actionIcon = manufactureAmmoButton.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Icon");
                    actionIcon.sprite = ammoItemDef.ViewElementDef.InventoryIcon;
                    Text techCostText = manufactureAmmoButton.transform.Find(_techCostTextObject).GetComponent<Text>();
                    techCostText.text = ammoItemDef.ManufacturePrice.FirstOrDefault(ru => ru.Type == ResourceType.Tech).Value.ToString();

                    if (enoughTech)
                    {
                        techCostText.color = _techViewDef.Color;
                    }
                    else
                    {
                        techCostText.color = Color.red;
                    }

                    Text matsCostText = manufactureAmmoButton.transform.Find(_matsCostTextObject).GetComponent<Text>();
                    matsCostText.text = ammoItemDef.ManufacturePrice.FirstOrDefault(ru => ru.Type == ResourceType.Materials).Value.ToString();

                    if (enoughMats)
                    {
                        matsCostText.color = _matsViewDef.Color;
                    }
                    else
                    {
                        matsCostText.color = Color.red;
                    }

                    Text amountInventory = manufactureAmmoButton.transform.Find(_amountInInventoryTextObject).GetComponent<Text>();
                    amountInventory.text = ammoItemDef.GetQuantity(faction, true).ToString();

                  //  geoManufactureItem.NewElementMarker.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 0);
                }
                catch (Exception e)
                {

                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static Vector2 _ogPositionNewItemElement = new Vector2(0, 0);
            private static Vector2 _newPositionNewItemElement = new Vector2(_ogPositionNewItemElement.x-275, -5);


            private static void AddorUpdateAmmoButton(GeoManufactureItem geoManufactureItem, GeoFaction faction)
            {
                try
                {
                    //   TFTVLogger.Always($"looking at item {__instance.ItemName.text}");

                    if(_ogPositionNewItemElement == Vector2.zero) 
                    {

                        _ogPositionNewItemElement = geoManufactureItem.NewElementMarker.GetComponent<RectTransform>().anchoredPosition;
                       // TFTVLogger.Always($"_ogPositionNewItemElement: {_ogPositionNewItemElement}");
                    }

                    Transform transform = geoManufactureItem.CurrentlyOwnedQuantityText.transform;
                    GameObject manufactureAmmoButton = transform.Find("ManufactureAmmoButton")?.gameObject;

                    if (geoManufactureItem.ItemDef.CompatibleAmmunition == null 
                        || geoManufactureItem.ItemDef.CompatibleAmmunition.Count() == 0 
                        || geoManufactureItem.ItemDef.CompatibleAmmunition.Count()>0 && geoManufactureItem.ItemDef.CompatibleAmmunition[0] == DefCache.GetDef<ItemDef>("SharedFreeReload_AmmoClip_ItemDef"))
                    {
                        if (manufactureAmmoButton != null)
                        {
                            manufactureAmmoButton?.SetActive(false);
                            
                            geoManufactureItem.NewElementMarker.GetComponent<RectTransform>().anchoredPosition = _ogPositionNewItemElement;
                        }
                        return;
                    }

                    UIModuleManufacturing uIModuleManufacturing = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ManufacturingModule;

                    if(uIModuleManufacturing.Mode != UIModuleManufacturing.UIMode.Manufacture) 
                    {
                        manufactureAmmoButton?.SetActive(false);
                        return;
                    }

                    ItemDef ammoItemDef = geoManufactureItem.ItemDef.CompatibleAmmunition[0];
                    
                    if (manufactureAmmoButton == null)
                    {
                        manufactureAmmoButton = AddManufactureAmmoButton(geoManufactureItem, faction, ammoItemDef);
                    }
                    else
                    {
                        UpdateManufactureButton(manufactureAmmoButton, faction, ammoItemDef);
                    }
                    // Add click event

                    geoManufactureItem.NewElementMarker.GetComponent<RectTransform>().anchoredPosition = _newPositionNewItemElement;

                    Button clonedButtonComponent = manufactureAmmoButton.GetComponent<Button>();

                    clonedButtonComponent.onClick.RemoveAllListeners();


                    clonedButtonComponent.onClick.AddListener(() =>
                    {
                        try
                        {
                            ResourcePack cost = ammoItemDef.ManufacturePrice;
                            bool enoughMats = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Materials));
                            bool enoughTech = faction.Wallet.HasResources(cost.FirstOrDefault(ru => ru.Type == ResourceType.Tech));

                            if (enoughMats && enoughTech && faction.ItemStorage.GetStorageUsed()+ammoItemDef.Weight<faction.GetTotalAvailableStorage())
                            {
                                // UIModuleManufacturing uIModuleManufacturing = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ManufacturingModule;
                                GeoItem geoItem = new GeoItem(ammoItemDef);
                                faction.ItemStorage.AddItem(geoItem);
                                faction.Wallet.Take(cost, OperationReason.Purchase);
                                geoManufactureItem.UpdateItemData();
                                geoManufactureItem.UpdateCostData();
                                UpdateManufactureButton(manufactureAmmoButton, faction, ammoItemDef);
                                //MethodInfo methodInfoAddToQueue = typeof(UIModuleManufacturing).GetMethod("AddToQueue", BindingFlags.Instance|BindingFlags.NonPublic);

                                // Define what happens when the cloned button is clicked
                            }
                           // TFTVLogger.Always("Cloned button clicked!");
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    });

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }


            [HarmonyPatch(typeof(GeoManufactureItem), "Init",
                typeof(ItemDef), typeof(GeoFaction), typeof(UIModuleManufacturing.UIMode), typeof(ItemStorage), typeof(VehicleEquipmentStorage), typeof(bool))]
            public static class GeoManufactureItem_Init_Patch
            {
                public static void Postfix(GeoManufactureItem __instance, GeoFaction ____faction)
                {
                    try
                    {
                        AddorUpdateAmmoButton(__instance, ____faction);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            private static bool _scrapFromSoldierEquip = false;

            [HarmonyPatch(typeof(UIModuleSoldierEquip), "AreaEndDragHandler")]
            public static class UIModuleSoldierEquip_AreaEndDragHandler_Patch
            {
                public static void Prefix(UIModuleSoldierEquip __instance, UIInventorySlot sourceSlot, UIInventoryDropArea destinationArea)
                {
                    try
                    {
                        if (destinationArea == __instance.ManufactureItemArea)
                        {
                            //TFTVLogger.Always($"setting _scrapFromSoldierEquip to true");
                            _scrapFromSoldierEquip = true;
                        }

                      
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            [HarmonyPatch(typeof(UIModuleManufacturing))]
            public static class UIModuleManufacturingPatch2
            {
                [HarmonyPatch("RefreshItemList")]
                [HarmonyPrefix]
                public static void RefreshItemListPrefix(UIModuleManufacturing __instance, ref IEnumerable<ItemDef> availableItemRecipes)
                {
                    try
                    {
                        if (__instance.Mode == UIModuleManufacturing.UIMode.Manufacture && !_scrapFromSoldierEquip)
                        {

                            List<ItemDef> itemDefs = new List<ItemDef>();

                            foreach (ItemDef itemDef in availableItemRecipes)
                            {
                                if (!itemDef.Tags.Contains(DefCache.GetDef<GameTagDef>("AmmoItem_TagDef")))
                                {
                                    itemDefs.Add(itemDef);
                                }
                            }

                            availableItemRecipes = itemDefs.ToArray();
                        }

                        if (_scrapFromSoldierEquip) 
                        {
                            TFTVLogger.Always($"setting _scrapFromSoldierEquip to false");
                           _scrapFromSoldierEquip = false;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            

            [HarmonyPatch(typeof(UIModuleManufacturing))]
            public static class UIModuleManufacturingPatch
            {
                // Patch the Start method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("Init")]
                public static void Init_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                [HarmonyPostfix]
                [HarmonyPatch("Start")]
                public static void Start_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                // Patch the SetupQueue method to add drag-and-drop event handlers
                [HarmonyPostfix]
                [HarmonyPatch("SetupQueue")]
                public static void SetupQueue_Postfix(UIModuleManufacturing __instance)
                {
                    try
                    {
                        foreach (var item in __instance.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            AddDragHandlers(item, __instance);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }





                /*    [HarmonyPatch(typeof(UIModuleManufacturing), "CancelItem")]
                    public static class UIModuleManufacturing_CancelItem_patch
                    {

                        public static void Prefix(UIModuleManufacturing __instance, GeoManufactureQueueItem item)
                        {
                            try
                            {
                                TFTVLogger.Always($"Cancel item: {item.ItemDef.name} Index: {item.transform.GetSiblingIndex() + 1}, transform: {item.transform.name}");


                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                                throw;
                            }
                        }
                    }*/

                private static void AddDragHandlers(GeoManufactureQueueItem item, UIModuleManufacturing instance)
                {
                    try
                    {
                        if (item.gameObject.GetComponent<DragHandler>() != null)
                        {
                            // DragHandler handler = item.gameObject.GetComponent<DragHandler>();
                            // UnityEngine.Object.Destroy(handler);
                            return;
                        }

                        var dragHandler = item.gameObject.AddComponent<DragHandler>();
                        dragHandler.Init(item, instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /*  [HarmonyPatch(typeof(ItemManufacturing), "Cancel")]
              public static class ItemManufacturing_CancelItem_patch
              {

                  public static void Prefix(UIModuleManufacturing __instance, int index, List<ItemManufacturing.ManufactureQueueItem> ____queue)
                  {
                      try
                      {
                          TFTVLogger.Always($"{index}");

                          foreach (ItemManufacturing.ManufactureQueueItem manufactureQueueItem in ____queue)
                          {
                              TFTVLogger.Always($"{manufactureQueueItem.ManufacturableItem.Name.Localize()} index: {____queue.IndexOf(manufactureQueueItem)}");

                          }

                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                          throw;
                      }
                  }
              }*/



            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private GeoManufactureQueueItem _item;
                private UIModuleManufacturing _roster;
                private RectTransform _originalParent;
                private int _originalIndex;
                private ScrollRect _scrollRect;

                public void Init(GeoManufactureQueueItem item, UIModuleManufacturing roster)
                {
                    try
                    {
                        _roster = roster;
                        _item = item;
                        _scrollRect = _roster.QueueScroller.Scroll;
                        _roster.QueueScroller.AdditionalVisibleRows = 50;
                        // Debug.Log($"DragHandler initialized for slot: {item.ItemName.text}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        //TFTVLogger.Always($"OnBeginDrag called for {_item.ItemName.text}");
                        _originalParent = transform.parent as RectTransform;

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        List<ItemManufacturing.ManufactureQueueItem> manufactureQueueItems = phoenixFaction.Manufacture.Queue;

                        for (int i = 0; i < manufactureQueueItems.Count; i++)
                        {
                            ItemManufacturing.ManufactureQueueItem manufactureQueueItem = manufactureQueueItems[i];

                            if (manufactureQueueItem == _item.QueueElement)
                            {
                                _originalIndex = i;
                                break;
                            }
                        }

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent);
                        transform.SetParent(_originalParent.parent);

                        // Disable the ScrollRect to prevent it from interfering with the drag
                        /*  if (_scrollRect != null)
                          {
                              _scrollRect.enabled = false;
                          }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {
                        //TFTVLogger.Always($"OnDrag for {_item.ItemName.text}");
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        // TFTVLogger.Always($"OnEndDrag called for {_item.ItemName.text}");

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        //  TFTVLogger.Always($"_originalIndex: {_originalIndex} new index {newIndex}, phoenixFaction.Manufacture.Queue.Count {phoenixFaction.Manufacture.Queue.Count}");

                        if (newIndex != _originalIndex)
                        {
                            ItemManufacturing.ManufactureQueueItem item = _item.QueueElement;
                            phoenixFaction.Manufacture.Queue.Remove(item);
                            phoenixFaction.Manufacture.Queue.Insert(newIndex, item);
                            _item.transform.SetSiblingIndex(newIndex);
                            // TFTVLogger.Always($"Moved {item.ManufacturableItem.Name.Localize()} from index {_originalIndex} to {newIndex}");
                        }

                        UIModuleManufacturing uIModuleManufacturing = phoenixFaction.GeoLevel.View.GeoscapeModules.ManufacturingModule;

                        /* foreach (ItemManufacturing.ManufactureQueueItem manufactureQueueItem in phoenixFaction.Manufacture.Queue)
                         {

                             TFTVLogger.Always($"{manufactureQueueItem.ManufacturableItem.Name.Localize()} {phoenixFaction.Manufacture.Queue.IndexOf(manufactureQueueItem)}");

                         }*/
                        MethodInfo method = uIModuleManufacturing.GetType().GetMethod("SetupQueue", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(uIModuleManufacturing, null);

                        /* foreach (ItemManufacturing.ManufactureQueueItem manufactureQueueItem in phoenixFaction.Manufacture.Queue)
                         {

                             TFTVLogger.Always($"{manufactureQueueItem.ManufacturableItem.Name.Localize()} {phoenixFaction.Manufacture.Queue.IndexOf(manufactureQueueItem)}");

                         }*/

                        // Re-enable the ScrollRect after the drag ends
                        /* if (_scrollRect != null)
                         {
                             _scrollRect.enabled = true;
                         }*/
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);
                    GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;

                    List<ItemManufacturing.ManufactureQueueItem> manufactureQueueItems = phoenixFaction.Manufacture.Queue;

                    //  TFTVLogger.Always($"There {manufactureQueueItems.Count} items in Manufacturing queue");

                    UIModuleManufacturing uIModuleManufacturing = phoenixFaction.GeoLevel.View.GeoscapeModules.ManufacturingModule;

                    //  TFTVLogger.Always($"localPosition: {localPosition}");

                    for (int i = 0; i < manufactureQueueItems.Count; i++)
                    {
                        ItemManufacturing.ManufactureQueueItem manufactureQueueItem = manufactureQueueItems[i];

                        // TFTVLogger.Always($"in the loop: {manufactureQueueItem.ManufacturableItem.Name} {i}");

                        foreach (var child in uIModuleManufacturing.QueueScroller.Scroll.content.GetComponentsInChildren<GeoManufactureQueueItem>())
                        {
                            if (child.QueueElement == manufactureQueueItem && child != _item)
                            {
                                /* TFTVLogger.Always($"item: {manufactureQueueItem.ManufacturableItem.Name.Localize()}, pos: {i}, " +
                                     $"found child {child.ItemName.text}, at pos {child.transform.localPosition}, " +
                                     $"RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position): {RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position)}" +
                                     $"\nlocalPosition.y > child.transform.localPosition.y {localPosition.y > child.transform.localPosition.y}");*/

                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)child.transform, position))
                                {
                                    // TFTVLogger.Always($"returning: {manufactureQueueItem.ManufacturableItem.Name.Localize()} {i}");

                                    return i;
                                }
                            }
                        }
                    }

                    return _originalIndex;
                }
            }
        }



        internal class VehicleRoster
        {

            public static List<InputAction> ActionsAircraftHotkeys = new List<InputAction>();
            public static bool AircraftHotkeysBindingsApplied = false;



            [HarmonyPatch(typeof(UIStateVehicleSelected), "OnInputEvent")]
            public static class UIStateVehicleSelected_OnInputEvent_patch
            {

                public static void Postfix(UIStateVehicleSelected __instance, InputEvent ev)
                {
                    try
                    {
                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        MethodInfo method = typeof(UIStateVehicleSelected).GetMethod("SelectVehicle", BindingFlags.NonPublic | BindingFlags.Instance);

                        GeoscapeViewContext geoscapeViewContext = (GeoscapeViewContext)typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller.View);

                        InputController inputController = geoscapeViewContext.Input;

                        FieldInfo field = inputController.GetType().GetField("_activeActionsMap", BindingFlags.NonPublic | BindingFlags.Instance);

                        InputAction[] inputActions = (InputAction[])field.GetValue(inputController);

                        if (!AircraftHotkeysBindingsApplied)
                        {
                            foreach (InputAction inputAction in ActionsAircraftHotkeys.Where(ia => !inputActions.Contains(ia)))
                            {
                                // TFTVLogger.Always($"{inputAction.Name} not found! adding to the list");
                                inputController.ApplyKeybinding(inputAction);

                            }
                            AircraftHotkeysBindingsApplied = true;
                        }


                        if (ev.Type == InputEventType.Pressed)
                        {
                            // TFTVLogger.Always($"evName: {ev.Name}");

                            if (ActionsAircraftHotkeys.Any(a => a.Name == ev.Name))
                            {
                                int id = int.Parse(ActionsAircraftHotkeys.FirstOrDefault(a => a.Name == ev.Name).Chords[0].Keys[0].Name);
                                GeoVehicle vehicle = controller.PhoenixFaction.Vehicles.FirstOrDefault(v => v.VehicleID == id);
                                if (vehicle != null)
                                {
                                    method.Invoke(__instance, new object[] { vehicle, true });
                                }
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

          /*  [HarmonyPatch(typeof(GeoRosterItem), "UpdateLocations")]
            public static class GeoRosterItem_UpdateLocations_patch
            {

                public static void Postfix(GeoRosterItem __instance, IGeoCharacterContainer ____container)
                {
                    try
                    {
                        //TFTVLogger.Always($"UpdateLocations Running");

                        if (____container is GeoVehicle)
                        {
                            GeoVehicle geoVehicle = (GeoVehicle)____container;

                            if (geoVehicle != null)
                            {
                                // TFTVLogger.Always($"looking at {geoVehicle.Name} {geoVehicle.VehicleID}");
                                __instance.VehicleNumberText.text = (geoVehicle.VehicleID + 1).ToString();
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

            [HarmonyPatch(typeof(FacilityRosterSlot), "UpdateLocation")]
            public static class FacilityRosterSlot_UpdateLocations_patch
            {

                public static void Postfix(FacilityRosterSlot __instance, UseSoldiersFacilityComponent ____soldierComponent)
                {
                    try
                    {
                        // TFTVLogger.Always($"UpdateLocation Running");
                        GeoPhoenixBase pxBase = ____soldierComponent.Context.Facility.PxBase;
                        GeoVehicle geoVehicle = pxBase.VehiclesAtBase.FirstOrDefault((GeoVehicle p) => p.Units.Contains(__instance.Character));

                        if (geoVehicle != null)
                        {
                            //   TFTVLogger.Always($"looking at {geoVehicle.Name} {geoVehicle.VehicleID}");
                            Text text = __instance.ThirdLocationSpotText;
                            text.text = (geoVehicle.VehicleID + 1).ToString();

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleActorCycle), "UpdateLocations")]
            public static class UIModuleActorCycle_UpdateLocations_patch
            {

                public static void Postfix(UIModuleActorCycle __instance, UnitDisplayData ____currentUnit)
                {
                    try
                    {
                        GeoCharacter character = ____currentUnit.BaseObject as GeoCharacter;
                        if (character == null)
                        {
                            return;
                        }

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;
                        IGeoCharacterContainer geoCharacterContainer = phoenixFaction.Sites.Concat<IGeoCharacterContainer>(phoenixFaction.Vehicles).FirstOrDefault((IGeoCharacterContainer c) => c.GetAllCharacters().Contains(character));

                        if (geoCharacterContainer == null)
                        {
                            return;
                        }

                        if (geoCharacterContainer is GeoVehicle geoVehicle)
                        {
                            if (geoVehicle != null)
                            {
                                __instance.VehicleNumberText.text = (geoVehicle.VehicleID + 1).ToString();

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


            [HarmonyPatch(typeof(GeoVehicle), "get_Name")]
            public static class GeoVehicle_get_Name_patch
            {

                public static bool Prefix(GeoVehicle __instance, ref string __result, string ____vehicleName)
                {
                    try
                    {
                        //TFTVLogger.Always($"{__instance.VehicleDef.ViewElement.DisplayName1.Localize(null)} {__instance.VehicleID}");

                        if (string.IsNullOrWhiteSpace(____vehicleName))
                        {
                            __result = string.Format(__instance.VehicleDef.ViewElement.DisplayName1.Localize(null), __instance.VehicleID + 1);
                        }
                        else
                        {
                            __result = ____vehicleName;
                        }

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/


            public static List<int> PlayerVehicles = new List<int>();

            public static void RecordVehicleOrder(GeoLevelController controller)
            {
                try
                {
                    PlayerVehicles.Clear();
                    GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                    FieldInfo fieldInfo = controller.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);
                    FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);
                    List<GeoVehicle> vehicles = factionActorCache.Cache[phoenixFaction];
                    for (int x = 0; x < vehicles.Count; x++)
                    {
                        vehicles[x].VehicleID = x+1;
                        PlayerVehicles.Add(vehicles[x].VehicleID);
                        TFTVLogger.Always($"Recording {vehicles[x].Name} {vehicles[x].VehicleID}");
                    }
                    
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }

            public static void RestoreVehicleOrder(GeoLevelController controller)
            {
                try
                {
                   

                    if (PlayerVehicles != null && PlayerVehicles.Count > 0)
                    {
                        GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                        FieldInfo fieldInfo = phoenixFaction.GeoLevel.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);

                        FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);

                        List<GeoVehicle> vehicles = new List<GeoVehicle>();

                      /*  if(phoenixFaction.Vehicles.Any(vehicle => vehicle.VehicleID == 0)) 
                        {
                            foreach (GeoVehicle geoVehicle in phoenixFaction.Vehicles)
                            {
                                geoVehicle.VehicleID += 1;
                            }
                        }*/

                        for (int x = 1; x<= PlayerVehicles.Count; x++)
                        {
                            GeoVehicle geoVehicle = phoenixFaction.Vehicles.FirstOrDefault(vehicle => vehicle.VehicleID == PlayerVehicles[x-1] && !vehicles.Contains(vehicle));
                            geoVehicle.VehicleID = x;
                            vehicles.Add(geoVehicle);
                            TFTVLogger.Always($"Restoring {geoVehicle.Name} {geoVehicle.VehicleID}");
                        }

                        factionActorCache.Cache[phoenixFaction] = vehicles;
                        fieldInfo.SetValue(phoenixFaction.GeoLevel.Map, factionActorCache);

                        //  TFTVLogger.Always($"got here");

                        //   phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Uninit();
                        /*  FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                          GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(controller.View);
                          phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Init(context);*/
                    }

                

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }





            [HarmonyPatch(typeof(UIModuleVehicleRoster))]
            public static class TFTVDragandDropFunctionality
            {
                [HarmonyPatch("InitSlots")]
                [HarmonyPostfix]
                public static void InitSlotsPostfix(UIModuleVehicleRoster __instance)
                {
                    try
                    {
                        Debug.Log($"Initializing DragHandlers for {__instance.Slots.Count} slots.");

                        foreach (var slot in __instance.Slots)
                        {
                            if (slot == null)
                            {
                                Debug.LogWarning("Slot is null during initialization!");
                                continue;
                            }

                            var dragHandler = slot.GetComponent<DragHandler>();
                            if (dragHandler == null)
                            {
                                dragHandler = slot.gameObject.AddComponent<DragHandler>();
                                dragHandler.Init(__instance, slot);
                                Debug.Log($"Added DragHandler to slot: {slot.name}");
                            }
                            else
                            {
                                Debug.Log($"DragHandler already exists for slot: {slot.name}");
                            }

                            //  EnableRaycastTargets(slot.gameObject);

                        }

                        ScrapeButtonFunctionality(__instance);



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void EnableRaycastTargets(GameObject obj)
                {
                    foreach (var graphic in obj.GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.raycastTarget = true;
                        Debug.Log($"Raycast target enabled for: {graphic.name}");
                    }
                }

                private static List<VehicleItemDef> _vehicleDefs;

                private static void PopulateInternalVehicleDefsList()
                {
                    try
                    {
                        if (_vehicleDefs == null || _vehicleDefs.Count == 0)
                        {
                            _vehicleDefs = new List<VehicleItemDef>();
                            _vehicleDefs.AddRange(GameUtl.GameComponent<DefRepository>().GetAllDefs<VehicleItemDef>().ToList());

                            foreach (VehicleItemDef vehicleItemDef in _vehicleDefs)
                            {
                                TFTVLogger.Always($"VehicleItemDef: {vehicleItemDef.name}, {vehicleItemDef.ViewElementDef.name}");



                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static PhoenixGeneralButton _scrapButton = null;

                //taken & adjusted from Mad's Assorted Adjustments. All hail Mad! https://github.com/Mad-Mods-Phoenix-Point/AssortedAdjustments/blob/main/Source/AssortedAdjustments/Patches/EnableScrapAircraft.cs
                private static void ScrapeButtonFunctionality(UIModuleVehicleRoster uIModuleVehicleRoster)
                {
                    try
                    {
                        PopulateInternalVehicleDefsList();


                        if (_scrapButton != null)
                        {

                        }
                        else
                        {

                            //   TFTVLogger.Always($"checking");
                            Resolution resolution = Screen.currentResolution;

                            // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                            float resolutionFactorHeight = (float)resolution.height / 1080f;
                            //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                            EditUnitButtonsController editUnitButtonsController = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ActorCycleModule.EditUnitButtonsController;
                            PhoenixGeneralButton checkButton = UnityEngine.Object.Instantiate(editUnitButtonsController.DismissButton, uIModuleVehicleRoster.transform);
                            checkButton.gameObject.AddComponent<UITooltipText>().TipText = TFTVCommonMethods.ConvertKeyToString("KEY_SCRAP_AIRCRAFT");// "Toggles helmet visibility on/off.";

                            checkButton.transform.position += new Vector3(90 * resolutionFactorWidth, 130 * resolutionFactorHeight);
                            checkButton.PointerClicked += () => OnScrapAircraftClick();
                            _scrapButton = checkButton;
                        }

                        void OnScrapAircraftClick()
                        {
                            GeoVehicle aircraftToScrap = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                            //   TFTVLogger.Always($"aircraftToScrap?.name: {aircraftToScrap?.name} vehicleDef {aircraftToScrap.VehicleDef.name} {aircraftToScrap.VehicleDef.ViewElement.name}"); 

                            UIModuleGeoscapeScreenUtils uIModuleGeoscapeScreenUtils = aircraftToScrap.GeoLevel.View.GeoscapeModules.GeoscapeScreenUtilsModule;

                            string messageBoxText = uIModuleGeoscapeScreenUtils.DismissVehiclePrompt.Localize(null);
                            VehicleItemDef aircraftItemDef = _vehicleDefs.Where(viDef => viDef.ViewElementDef == aircraftToScrap.VehicleDef.ViewElement).FirstOrDefault();

                            //  TFTVLogger.Always($"aircraftItemDef?.name: {aircraftItemDef?.name}");

                            if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                            {
                                messageBoxText = messageBoxText + "\n" + uIModuleGeoscapeScreenUtils.ScrapResourcesBack.Localize(null) + "\n \n";
                                foreach (ResourceUnit resourceUnit in aircraftItemDef.ScrapPrice)
                                {
                                    if (resourceUnit.RoundedValue > 0)
                                    {
                                        string resourcesInfo = "";
                                        ResourceType type = resourceUnit.Type;
                                        switch (type)
                                        {
                                            case ResourceType.Supplies:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapSuppliesResources.Localize(null);
                                                break;
                                            case ResourceType.Materials:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapMaterialsResources.Localize(null);
                                                break;
                                            case (ResourceType)3:
                                                break;
                                            case ResourceType.Tech:
                                                resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapTechResources.Localize(null);
                                                break;
                                            default:
                                                if (type == ResourceType.Mutagen)
                                                {
                                                    resourcesInfo = uIModuleGeoscapeScreenUtils.ScrapMutagenResources.Localize(null);
                                                }
                                                break;
                                        }
                                        resourcesInfo = resourcesInfo.Replace("{0}", resourceUnit.RoundedValue.ToString());
                                        messageBoxText += resourcesInfo;
                                    }
                                }
                            }


                            // Safety check as the game's UI fails hard if there's NO GeoVehicle left at all
                            if (aircraftToScrap.Owner.Vehicles.Count() <= 1)
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_LAST_AIRCRAFT"), MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                            }
                            else if (aircraftToScrap.Travelling)
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_IN_TRANSIT_AIRCRAFT"), MessageBoxIcon.Error, MessageBoxButtons.OK, new MessageBox.MessageBoxCallback(OnScrapAircraftImpossibleCallback), null, null);
                            }
                            else
                            {
                                GameUtl.GetMessageBox().ShowSimplePrompt(string.Format(messageBoxText, aircraftToScrap.Name), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, new MessageBox.MessageBoxCallback(OnScrapAircraftCallback), null, aircraftToScrap);
                            }
                        }

                        void OnScrapAircraftImpossibleCallback(MessageBoxCallbackResult msgResult)
                        {
                            // Nothing
                        }

                        void OnScrapAircraftCallback(MessageBoxCallbackResult msgResult)
                        {
                            if (msgResult.DialogResult == MessageBoxResult.Yes)
                            {

                                GeoVehicle aircraftToScrap = uIModuleVehicleRoster.SelectedSlot.Vehicle.GetBaseObject<GeoVehicle>();

                                if (aircraftToScrap != null)
                                {
                                    // Unset vehicle.CurrentSite and trigger site.VehicleLeft
                                    aircraftToScrap.Travelling = true;

                                    RemoveEquipmentFromScrappedVehicle(aircraftToScrap);
                                    uIModuleVehicleRoster.UpdateSelectedVehicleEquipments();

                                    // Away with it!
                                    aircraftToScrap.Destroy();

                                    // Add resources
                                    VehicleItemDef aircraftItemDef = _vehicleDefs.Where(viDef => viDef.ComponentSetDef.Components.Contains(aircraftToScrap.VehicleDef)).FirstOrDefault();

                                    MethodInfo updateResourcInfoMethodInfo = typeof(UIModuleInfoBar).GetMethod("UpdateResourceInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                                    UIModuleInfoBar uIModuleInfoBar = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.ResourcesModule;

                                    if (aircraftItemDef != null && !aircraftItemDef.ScrapPrice.IsEmpty)
                                    {
                                        aircraftToScrap.Owner.Wallet.Give(aircraftItemDef.ScrapPrice, OperationReason.Scrap);
                                        updateResourcInfoMethodInfo.Invoke(uIModuleInfoBar, new object[] { aircraftToScrap.Owner, true });
                                    }

                                    FieldInfo fieldInfo = typeof(UIModuleVehicleRoster).GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance);
                                    List<VehicleDisplayData> vehicles = (List<VehicleDisplayData>)fieldInfo.GetValue(uIModuleVehicleRoster);
                                    vehicles.Remove(uIModuleVehicleRoster.SelectedSlot.Vehicle);
                                    fieldInfo.SetValue(uIModuleVehicleRoster, vehicles);

                                    uIModuleVehicleRoster.RosterList.DestroyChildren();

                                    MethodInfo methodInfo = typeof(UIModuleVehicleRoster).GetMethod("InitSlots", BindingFlags.NonPublic | BindingFlags.Instance);
                                    methodInfo.Invoke(uIModuleVehicleRoster, null);

                                    uIModuleVehicleRoster.SetSelectSlot(vehicles.First(), true);

                                    UIModuleVehicleCycle uIModuleVehicleCycle = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.VehicleCycleModule;
                                    uIModuleVehicleCycle.SelectVehicle(vehicles.FirstOrDefault());
                                }



                                /*uIModuleVehicleRoster.SelectedSlot.WeaponSlot01.ResetItem();
                                uIModuleVehicleRoster.SelectedSlot.WeaponSlot02.ResetItem();
                                uIModuleVehicleRoster.SelectedSlot.ModuleSlot.ResetItem();*/

                            }

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void RemoveEquipmentFromScrappedVehicle(GeoVehicle aircraftToScrap)
                {
                    try
                    {
                        GeoLevelController controller = aircraftToScrap.GeoLevel;

                        GeoFaction geoFaction = controller.PhoenixFaction;

                        List<GeoVehicleEquipment> vehicleEquipmentToRemove = new List<GeoVehicleEquipment>();

                        foreach (GeoVehicleEquipment geoVehicleEquipment in aircraftToScrap.Equipments)
                        {
                            if (geoVehicleEquipment != null)
                            {
                                // TFTVLogger.Always($"{geoVehicleEquipment} being added ");
                                // GeoVehicleEquipmentUIData geoVehicleEquipmentUIData = geoVehicleEquipment.CreateUIData();
                                geoFaction.AircraftItemStorage.AddItem(geoVehicleEquipment);
                                vehicleEquipmentToRemove.Add(geoVehicleEquipment);
                                // vehicleEquipModule.StorageList.AddItem(geoVehicleEquipmentUIData);
                            }
                        }

                        foreach (GeoVehicleEquipment vehicleEquipment in vehicleEquipmentToRemove)
                        {
                            aircraftToScrap.RemoveEquipment(vehicleEquipment);

                            TFTVLogger.Always($"removing {vehicleEquipment.EquipmentDef.name} from scrapped vehicle");
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
            {
                private UIModuleVehicleRoster _roster;
                private GeoVehicleRosterSlot _slot;
                private Transform _originalParent;
                private int _originalIndex;

                public void Init(UIModuleVehicleRoster roster, GeoVehicleRosterSlot slot)
                {
                    try
                    {
                        _roster = roster;
                        _slot = slot;
                        Debug.Log($"DragHandler initialized for slot: {slot.name}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnBeginDrag(PointerEventData eventData)
                {
                    try
                    {
                        Debug.Log($"OnBeginDrag called for {_slot.name}");
                        _originalParent = transform.parent;
                        _originalIndex = transform.GetSiblingIndex();

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent.GetComponent<RectTransform>());
                        transform.SetParent(_originalParent.parent);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnDrag(PointerEventData eventData)
                {
                    try
                    {
                        transform.position = eventData.position;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                    try
                    {
                        Debug.Log($"OnEndDrag called for {_slot.name}");
                        transform.SetParent(_originalParent);
                        int newIndex = GetNewIndex(eventData.position);

                        transform.SetSiblingIndex(newIndex);

                        int oldIndex = _roster.Slots.IndexOf(_slot);

                        if (newIndex != oldIndex)
                        {
                            Debug.Log($"Slot moved: Old Index: {oldIndex}, New Index: {newIndex}");
                            _roster.Slots.RemoveAt(oldIndex);
                            _roster.Slots.Insert(newIndex, _slot);
                            UpdateVehicleOrder();
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private int GetNewIndex(Vector3 position)
                {
                    Vector3 localPosition = _originalParent.InverseTransformPoint(position);

                    for (int i = 0; i < _originalParent.childCount; i++)
                    {
                        RectTransform child = _originalParent.GetChild(i) as RectTransform;

                        if (child != null)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint(child, position))
                            {
                                return i;
                            }

                            if (localPosition.y > child.localPosition.y)
                            {
                                return i;
                            }
                        }
                    }
                    return _originalParent.childCount - 1;
                }

                private void UpdateVehicleOrder()
                {
                    try
                    {
                        Debug.Log("Updating vehicle order...");

                        List<GeoVehicleRosterSlot> geoVehicleRosterSlots = _roster.Slots.Where(s => s.Vehicle != null).ToList();

                        _roster.GetType().GetField("_vehicles", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.SetValue(_roster, geoVehicleRosterSlots.Select(slot => slot.Vehicle).ToList());

                        List<GeoVehicle> vehicles = geoVehicleRosterSlots.Select(slot => (GeoVehicle)slot.Vehicle.BaseObject).ToList();

                        GeoPhoenixFaction phoenixFaction = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().PhoenixFaction;


                        FieldInfo fieldInfo = phoenixFaction.GeoLevel.Map.GetType().GetField("_factionVehiclesCache", BindingFlags.NonPublic | BindingFlags.Instance);

                        FactionActorCache<GeoVehicle> factionActorCache = (FactionActorCache<GeoVehicle>)fieldInfo.GetValue(phoenixFaction.GeoLevel.Map);
                        factionActorCache.Cache[phoenixFaction] = vehicles;
                        fieldInfo.SetValue(phoenixFaction.GeoLevel.Map, factionActorCache);


                        //  phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Uninit();
                        RecordVehicleOrder(phoenixFaction.GeoLevel);



                        FieldInfo fieldInfo_context = typeof(GeoscapeView).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
                        GeoscapeViewContext context = (GeoscapeViewContext)fieldInfo_context.GetValue(GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View);
                        phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.Init(context);
                        // TFTVLogger.Always($"TESTING got here END");

                        //   MethodInfo method = phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                        //  method.Invoke(phoenixFaction.GeoLevel.View.GeoscapeModules.VehicleSelectionModule, null);
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
