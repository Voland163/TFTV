using Base;
using Base.Defs;
using Base.Entities.Statuses;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Item = PhoenixPoint.Common.Entities.Items.Item;

namespace TFTV
{
    internal class TFTVVehicleFixes
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void CheckSquashing(TacticalAbility ability, TacticalActor tacticalActor)
        {
            try
            {

                if (ability == null || tacticalActor == null)
                {
                    return;
                }


                if (ability is CaterpillarMoveAbility caterpillarMoveAbility
                    && caterpillarMoveAbility.TacticalDemolition != null && caterpillarMoveAbility.TacticalDemolition.TacticalDemolitionComponentDef.DemolitionBodyShape == TacticalDemolitionComponentDef.DemolitionShape.Rectangle)
                {

                }
                else
                {
                    return;
                }


                Vector3 direction = tacticalActor.NavigationComponent.Direction;

                Vector3 frontcentre = tacticalActor.transform.position + direction;

                Type type = typeof(CaterpillarMoveAbility);

                // Get the field info for _actorBases
                FieldInfo fieldInfo = type.GetField("_actorBases", BindingFlags.NonPublic | BindingFlags.Instance);

                // Get the value of _actorBases
                HashSet<TacticalActorBase> actorBases = (HashSet<TacticalActorBase>)fieldInfo.GetValue(caterpillarMoveAbility);

                List<TacticalActor> nearbyActors = tacticalActor.TacticalLevel.Map.GetActors<TacticalActor>().
                    Where(a => a.IsAlive && a.HasGameTag(DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef"))
                    && (a.Pos - frontcentre).sqrMagnitude < 1)
                    .ToList();

                foreach (TacticalActor actor in nearbyActors)
                {
                    if (actorBases.Contains(actor))
                    {
                        continue;
                    }

                    TFTVLogger.Always($"squashing {actor.name}");

                    ApplyDamageEffectAbility ability1 = actor.GetAbility<ApplyDamageEffectAbility>();

                    if (ability1 != null)
                    {
                        ability1.Activate();
                        actorBases.Add(actor);
                        fieldInfo.SetValue(caterpillarMoveAbility, actorBases);
                    }
                    else
                    {
                        actor.Health.SetToMin();
                        actorBases.Add(actor);
                        fieldInfo.SetValue(caterpillarMoveAbility, actorBases);
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        [HarmonyPatch(typeof(AdaptiveWeaponStatus), nameof(AdaptiveWeaponStatus.OnApply))]
        public static class TFTV_AdaptiveWeaponStatus_OnApply
        {
            public static void Postfix(AdaptiveWeaponStatus __instance, StatusComponent statusComponent)
            {
                try
                {
                    if (__instance.AdaptiveWeaponStatusDef.Guid.Equals("c63d61b2-4afd-4809-ba29-fbf85bd3f270"))
                    {
                        TFTVLogger.Always($"{__instance.AdaptiveWeaponStatusDef.name} OnApply to {__instance.TacticalActor?.DisplayName}");

                        List<Equipment> obliterators = new List<Equipment>();

                        obliterators.AddRange(__instance.TacticalActor.Equipments.Equipments.Where(e => e.TacticalItemDef == __instance.AdaptiveWeaponStatusDef.WeaponDef).ToList());

                        if (obliterators.Count == 0)
                        {
                            __instance.TacticalActor.Equipments.AddItem(__instance.AdaptiveWeaponStatusDef.WeaponDef, __instance);
                        }
                        else
                        {
                            for (int x = 0; x < obliterators.Count; x++)
                            {
                                if (x == 0)
                                {
                                    continue;
                                }

                                Equipment equipment = obliterators[x];
                                __instance.TacticalActor.Equipments.RemoveItem(equipment);
                                TFTVLogger.Always($"{equipment?.DisplayName} {equipment?.TacticalItemDef?.name} removed");

                            }

                        }

                        List<Equipment> flameThrowers = new List<Equipment>();

                        WeaponDef armadilloFtDef = (WeaponDef)Repo.GetDef("49723d28-b373-3bc4-7918-21e87a72c585");

                        flameThrowers.AddRange(__instance.TacticalActor.Equipments.Equipments.Where(e => e.TacticalItemDef == armadilloFtDef).ToList());

                        if (flameThrowers.Count == 0)
                        {
                            __instance.TacticalActor.Equipments.AddItem(armadilloFtDef, __instance);
                        }
                        else
                        {
                            for (int x = 0; x < flameThrowers.Count; x++)
                            {
                                if (x == 0)
                                {
                                    continue;
                                }

                                Equipment equipment = flameThrowers[x];
                                __instance.TacticalActor.Equipments.RemoveItem(equipment);
                                TFTVLogger.Always($"{equipment?.DisplayName} {equipment?.TacticalItemDef?.name} removed");

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


        [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.GetDefaultShootAbility))]
        public static class TFTV_TacticalActor_GetDefaultShootAbility
        {
            public static void Postfix(TacticalActor __instance, ref ShootAbility __result)
            {
                try
                {
                    ShootAbilityDef stabilityTaurusShootAbilityDef = (ShootAbilityDef)Repo.GetDef("76ae9352-1343-4b95-964c-036341b6a0eb");
                    ShootAbilityDef stabilityMissileShootAbilityDef = (ShootAbilityDef)Repo.GetDef("df2e83d1-8688-4e47-8559-cc6a9f9906d1");

                    if (__instance.GetAbilityWithDef<ShootAbility>(stabilityTaurusShootAbilityDef) != null)
                    {
                        __result = __instance.GetAbilityWithDef<ShootAbility>(stabilityTaurusShootAbilityDef);
                    }
                    else if (__instance.GetAbilityWithDef<ShootAbility>(stabilityMissileShootAbilityDef) != null)
                    {
                        __result = __instance.GetAbilityWithDef<ShootAbility>(stabilityMissileShootAbilityDef);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(TacContextHelpManager), "OnStatusApplied")] //VERIFIED
        public static class TFTV_TacContextHelpManager_OnApply
        {
            public static bool Prefix(TacContextHelpManager __instance, Status status)
            {
                try
                {
                    if (!(status is TacStatus tacStatus) || tacStatus.StatusComponent == null)
                    {
                        // TFTVLogger.Always($"TacContextHelpManager.OnStatusApplied status null! returning to avoid softlock");
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
        }


        [HarmonyPatch(typeof(SlotStateStatus), nameof(SlotStateStatus.OnApply))]
        public static class TFTV_SlotStateStatus_OnApply
        {
            public static bool Prefix(SlotStateStatus __instance, StatusComponent statusComponent)
            {
                try
                {

                    if (__instance.Applied)
                    {
                        return true;
                    }

                    string targetSlotName = TacUtil.GetStatusTargetSlotName(__instance);

                    if (targetSlotName.IsNullOrEmpty())
                    {
                        TFTVLogger.Always($"{__instance.SlotStateStatusDef.name} status failed to apply to! Target slot not supplied! TFTV Kludge to prevent Softlock");
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
        }

        [HarmonyPatch(typeof(TacticalItem), nameof(TacticalItem.SetToDisabled))]
        public static class TFTV_TacticalItem_SetToDisabled
        {
            public static void Prefix(TacticalItem __instance)
            {
                try
                {
                    TacticalActor tacActor = __instance.TacticalActor;

                    GroundVehicleWeaponDef meph = (GroundVehicleWeaponDef)Repo.GetDef("49723d28-b373-3bc4-7918-21e87a72c585");
                    GroundVehicleWeaponDef obliterator = (GroundVehicleWeaponDef)Repo.GetDef("ffb34012-b1fd-4b24-8236-ba2eb23db0b7");

                    if (__instance.ItemDef == obliterator && tacActor != null)
                    {
                        TFTVLogger.Always($"it's the obliterator");

                        if (tacActor.Equipments.Equipments.Any(e => e.ItemDef == meph && e.Enabled))
                        {
                            TFTVLogger.Always($"Obliterator destroyed, removing meph");
                            tacActor.Equipments.RemoveItem(meph).Destroy();
                            TFTVLogger.Always($"should be destroyed");
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



        [HarmonyPatch(typeof(Addon), nameof(Addon.Destroy))]
        public static class TFTV_Addon_RemoveItem
        {
            public static bool Prefix(Addon __instance, ref List<Addon> __result)
            {
                try
                {
                    if (__instance == null)
                    {
                        __result = new List<Addon>();

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
        }


        [HarmonyPatch(typeof(InventoryComponent), "RemoveItem", new Type[] { typeof(ItemDef) })] //VERIFIED
        public static class TFTV_InventoryComponent_RemoveItem
        {
            public static bool Prefix(InventoryComponent __instance, ItemDef itemDef, ref Item __result)
            {
                try
                {
                    Item item = __instance.GetItem(itemDef);

                    TFTVLogger.Always($"item null? {item == null} {item?.ItemDef?.name}");

                    if (item != null)
                    {
                        __instance.RemoveItem(item);
                    }

                    __result = item;

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch] //VERIFIED
        public static class TFTV_PostmissionReplenishManager_RemoveExtra
        {
            static MethodBase TargetMethod()
            {
                return typeof(PostmissionReplenishManager).GetMethod("RemoveExtra", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(List<GeoItem>).MakeByRefType(), typeof(List<GeoItem>).MakeByRefType(), typeof(List<GeoItem>).MakeByRefType(), typeof(GeoCharacter) }, null);
            }

            public static void Prefix(PostmissionReplenishManager __instance, ref List<GeoItem> preferredItemsList, ref List<GeoItem> currentItems, ref List<GeoItem> extraItems, GeoCharacter debugRemoveLater)
            {
                try
                {
                    if (currentItems.Any(ei => ei.ItemDef.name.Equals("NJ_Armadillo_Mephistopheles_GroundVehicleWeaponDef(Clone)")))
                    {
                        TFTVLogger.Always($"Remove Extra for {debugRemoveLater?.DisplayName} item is NJ_Armadillo_Mephistopheles_GroundVehicleWeaponDef(Clone)");
                        currentItems.RemoveAll(ei => ei.ItemDef.name.Equals("NJ_Armadillo_Mephistopheles_GroundVehicleWeaponDef(Clone)"));
                    }



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }





        [HarmonyPatch(typeof(GeoMission), "ManageGear")] //VERIFIED
         public static class TFTV_GeoMission_ManageGear
         {
             public static bool Prefix(GeoMission __instance, TacMissionResult result, GeoSquad squad)
             {
                 try
                 {
                     MethodInfo methodInfoReloadItem = typeof(GeoMission).GetMethod("TryReloadItem", BindingFlags.NonPublic | BindingFlags.Instance);
                     MethodInfo methodInfoGetDeployedTurretItems = typeof(GeoMission).GetMethod("GetDeployedTurretItems", BindingFlags.NonPublic | BindingFlags.Instance);
                     MethodInfo methodInfoGetItemsOnTheGround = typeof(GeoMission).GetMethod("GetItemsOnTheGround", BindingFlags.NonPublic | BindingFlags.Instance);
                     MethodInfo methodInfoGetDeadSquadMembersArmour = typeof(GeoMission).GetMethod("GetDeadSquadMembersArmour", BindingFlags.NonPublic | BindingFlags.Instance);
                     MethodInfo methodInfoManageFreeReloads = typeof(GeoMission).GetMethod("ManageFreeReloads", BindingFlags.NonPublic | BindingFlags.Instance);
                     MethodInfo methodInfoManageAutosellItems = typeof(GeoMission).GetMethod("ManageAutosellItems", BindingFlags.NonPublic | BindingFlags.Instance);

                     TFTVLogger.Always($"methodInfoReloadItem {methodInfoReloadItem == null}\nmethodInfoGetDeployedTurretItems {methodInfoGetDeployedTurretItems == null}\n" +
                         $"methodInfoGetItemsOnTheGround {methodInfoGetItemsOnTheGround == null}\n methodInfoGetDeadSquadMembersArmour {methodInfoGetDeadSquadMembersArmour == null}\n" +
                         $"methodInfoGetDeadSquadMembersArmour {methodInfoGetDeadSquadMembersArmour == null}\n methodInfoManageFreeReloads {methodInfoManageFreeReloads == null}" +
                         $"methodInfoManageAutosellItems {methodInfoManageAutosellItems == null}");


                     GeoFaction viewerFaction = __instance.Site.GeoLevel.ViewerFaction;
                     FactionResult resultByFacionDef = result.GetResultByFacionDef(viewerFaction.Def.PPFactionDef);
                     bool num = resultByFacionDef.State == TacFactionState.Won;
                     IEnumerable<GeoCharacter> enumerable = squad.Units.Where((GeoCharacter s) => !s.IsAlive);
                     if (num)
                     {
                         foreach (GeoItem deployedTurretItem in (IEnumerable<GeoItem>)methodInfoGetDeployedTurretItems.Invoke(__instance, new object[] { resultByFacionDef }))
                         {
                             TFTVLogger.Always($"adding turret {deployedTurretItem.ItemDef.name}");
                             __instance.Reward.Items.AddItem(deployedTurretItem);
                         }

                         if (!__instance.MissionDef.DontRecoverItems)
                         {
                             foreach (GeoItem item in (IEnumerable<GeoItem>)methodInfoGetItemsOnTheGround.Invoke(__instance, new object[] { result }))
                             {
                                 TFTVLogger.Always($"adding item from the ground {item.ItemDef.name}");

                                 __instance.Reward.Items.AddItem(item);
                             }

                             foreach (GeoItem item2 in (IEnumerable<GeoItem>)methodInfoGetDeadSquadMembersArmour.Invoke(__instance, new object[] { result, squad }))
                             {
                                 TFTVLogger.Always($"adding armor from dead soldier {item2.ItemDef.name}");
                                 __instance.Reward.Items.AddItem(item2);
                             }

                             foreach (GeoCharacter item3 in enumerable)
                             {
                                 if (!item3.TemplateDef.IsVehicle)
                                 {
                                     foreach (GeoItem equipmentItem in item3.EquipmentItems)
                                     {
                                         TFTVLogger.Always($"adding equipmentItem from dead soldier {item3?.DisplayName} {equipmentItem.ItemDef.name}");
                                         __instance.Reward.Items.AddItem(equipmentItem);
                                     }
                                 }

                                 foreach (GeoItem inventoryItem in item3.InventoryItems)
                                 {
                                     TFTVLogger.Always($"adding inventoryItem from dead unit {item3?.DisplayName} {inventoryItem.ItemDef.name}");
                                     __instance.Reward.Items.AddItem(inventoryItem);
                                 }
                             }
                         }
                     }

                     if (viewerFaction is GeoPhoenixFaction geoPhoenixFaction)
                     {
                         geoPhoenixFaction.PostmissionReplenish(squad.Units, ref __instance.Reward.Items);
                     }

                     ItemStorage itemStorage = viewerFaction.GetItemStorage(__instance.Site);
                     foreach (GeoCharacter unit in squad.Units)
                     {
                         if (!unit.IsAlive)
                         {
                             continue;
                         }

                         foreach (GeoItem equipmentItem2 in unit.EquipmentItems)
                         {
                             TFTVLogger.Always($"{unit.DisplayName} reloading {equipmentItem2.ItemDef.name}");

                             if (!(bool)methodInfoReloadItem.Invoke(__instance, new object[] { equipmentItem2, __instance.Reward.Items, "mission items" }))
                             {
                                 methodInfoReloadItem.Invoke(__instance, new object[] { equipmentItem2, itemStorage, "faction storage" });
                             }
                         }

                         foreach (GeoItem inventoryItem2 in unit.InventoryItems)
                         {
                             TFTVLogger.Always($"{unit.DisplayName} reloading {inventoryItem2.ItemDef.name}");

                             if (!(bool)methodInfoReloadItem.Invoke(__instance, new object[] { inventoryItem2, __instance.Reward.Items, "mission items" }))
                             {
                                 methodInfoReloadItem.Invoke(__instance, new object[] { inventoryItem2, itemStorage, "faction storage" });
                             }

                         }

                         foreach (GeoItem armourItem in unit.ArmourItems)
                         {
                             TFTVLogger.Always($"{unit.DisplayName} reloading {armourItem.ItemDef.name}");

                             if (!(bool)methodInfoReloadItem.Invoke(__instance, new object[] { armourItem, __instance.Reward.Items, "mission items" }))
                             {
                                 methodInfoReloadItem.Invoke(__instance, new object[] { armourItem, itemStorage, "faction storage" });
                             }

                         }
                     }

                     methodInfoManageFreeReloads.Invoke(__instance, new object[] { result });
                     methodInfoManageAutosellItems.Invoke(__instance, new object[] { });


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
