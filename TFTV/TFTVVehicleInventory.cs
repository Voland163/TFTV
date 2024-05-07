using Base;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVVehicleInventory
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        //Patch to allow trading inventory with vehicle while inside vehicle
        [HarmonyPatch(typeof(TacUtil), "CanTradeWith")]
        public static class TacUtil_EnterState_patch
        {
            public static bool Prefix(TacticalActor buyer, TacticalActor seller, ref bool __result)
            {
                try
                {
                    __result = true;

                    if (seller == null)
                    {
                        __result = false;
                    }

                    if (seller.IsDead)
                    {
                        __result = false;
                    }

                    if (seller.HasEndedTurn)
                    {
                        __result = false;
                    }

                    if (seller == buyer)
                    {
                        __result = false;

                    }

                    if (seller == buyer.Status.GetStatus<MountedStatus>()?.Vehicle.TacticalActorBase)
                    {
                        // TFTVLogger.Always($"buyer has mounted status, switching to true");
                        // __result = true;
                    }

                    if (seller.RelationTo(buyer) != FactionRelation.Friend)
                    {
                        __result = false;
                    }

                    if (seller.Vehicle != null && seller.Vehicle.VehicleComponentDef.HasDoor && buyer.Vehicle != null && buyer.Vehicle.VehicleComponentDef.HasDoor)
                    {
                        //  TFTVLogger.Always($"seller is vehicle, switching to true");
                        __result = false;
                    }

                    //  TFTVLogger.Always($"result is {__result}");
                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(UIStateInventory), "EnterState")]
        public static class UIStateInventory_EnterState_patch
        {
            public static void Postfix(UIStateInventory __instance, ref InventoryComponent ____groundInventory)
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    UIModuleSoldierEquip uIModuleSoldierEquip = controller.View.TacticalModules.TacticalSoldierEquipModule;


                    if (__instance.PrimaryActor.Mount != null && ____groundInventory == __instance.PrimaryActor.Mount.TacticalActorBase.Inventory || __instance.PrimaryActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                    {
                      //  uIModuleSoldierEquip.StorageLabelTextComp.rectTransform.GetComponentsInParent<RectTransform>().FirstOrDefault(t => t.name.Equals("StoresPanel")).GetComponentsInChildren<Transform>().FirstOrDefault(t2=>t2.name.Equals("UIScrollView"));

                        //  UIInventoryList inventoryList = uIModuleSoldierEquip.StorageList;

                       foreach (RectTransform transform in uIModuleSoldierEquip.StorageLabelTextComp.rectTransform.GetComponentsInParent<RectTransform>())
                        {

                            if (transform.name == "StoresPanel")
                            {
                                foreach (Transform transform1 in transform.GetComponentInChildren<Transform>())
                                {
                                 //   TFTVLogger.Always($"child is {transform1}");

                                    if (transform1.name == "UIScrollView")
                                    {
                                        transform1.gameObject.SetActive(false);
                                    }

                                }

                            }

                        }

                     //   TFTVLogger.Always($" {controller.View.TacticalModules.TacticalSoldierEquipModule.StorageList.name} ");*/

                    }
                    else
                    {
                      //  uIModuleSoldierEquip.StorageLabelTextComp.rectTransform.GetComponentsInParent<RectTransform>().FirstOrDefault(t => t.name.Equals("StoresPanel")).GetComponentsInChildren<Transform>().FirstOrDefault(t2 => t2.name.Equals("UIScrollView"));

                         foreach (RectTransform transform in uIModuleSoldierEquip.StorageLabelTextComp.rectTransform.GetComponentsInParent<RectTransform>())
                         {

                             if (transform.name == "StoresPanel")
                             {
                                 foreach (Transform transform1 in transform.GetComponentInChildren<Transform>())
                                 {
                                   //  TFTVLogger.Always($"child is {transform1}");

                                     if (transform1.name == "UIScrollView")
                                     {
                                         transform1.gameObject.SetActive(true);
                                     }

                                 }

                             }


                             //uIModuleSoldierEquip.StorageLabelTextComp.rectTransform.GetComponentsInParent<Transform>().FirstOrDefault(t => t.name == "StoresPanel").gameObject.SetActive(true);
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




        [HarmonyPatch(typeof(UIStateInventory), "GetGroundItems")]
        public static class UIStateInventory_GetGroundItems_patch
        {
            public static bool Prefix(UIStateInventory __instance, IEnumerable<ItemContainer> itemContainers, ref HashSet<TacticalItem> __result)
            {
                try
                {


                    MethodInfo method = typeof(UIStateInventory).GetMethod("GetItemsInItemContainers", BindingFlags.NonPublic | BindingFlags.Instance);
                    IEnumerable<Item> result = (IEnumerable<Item>)method.Invoke(__instance, new object[] { itemContainers });

                    HashSet<TacticalItem> hashSet = new HashSet<TacticalItem>();

                    hashSet.AddRange(from item in result.OfType<TacticalItem>()
                                     where item.IsPickable
                                     select item);


                    __result = hashSet;

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
}
