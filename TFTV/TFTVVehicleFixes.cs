using Base.Entities.Statuses;
using Base;
using HarmonyLib;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Base.Defs;
using PhoenixPoint.Common.Core;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using System.Reflection;
using UnityEngine;

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


        [HarmonyPatch(typeof(TacticalActor), "GetDefaultShootAbility")]
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



        [HarmonyPatch(typeof(TacContextHelpManager), "OnStatusApplied")]
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


        [HarmonyPatch(typeof(SlotStateStatus), "OnApply")]
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

        [HarmonyPatch(typeof(TacticalItem), "SetToDisabled")]
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



        [HarmonyPatch(typeof(Addon), "Destroy")]
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


        [HarmonyPatch(typeof(InventoryComponent), "RemoveItem", new Type[] { typeof(ItemDef) })]
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
    }
}
