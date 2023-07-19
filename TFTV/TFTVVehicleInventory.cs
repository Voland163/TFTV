using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View.ViewStates;
using System.Reflection;
using PhoenixPoint.Common.Entities.Items;
using Base;

namespace TFTV
{
    internal class TFTVVehicleInventory
    {

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
