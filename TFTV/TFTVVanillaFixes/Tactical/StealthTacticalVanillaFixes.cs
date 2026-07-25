using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class StealthTacticalVanillaFixes
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static bool _usingEchoHead = false;

        //Prevents targeting body parts with Destiny and similar of unrevealed characters.

        [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.GetShootTarget))]
        public static class ShootAbility_GetShootTarget_Patch
        {
            public static void Postfix(ShootAbility __instance,
                TacticalAbilityTarget target, ref TacticalAbilityTarget __result)// Vector3? sourcePosition = null, TacticalTargetData targetData = null, )
            {
                try
                {
                    if (__instance.ShootAbilityDef.SnapToBodyparts)
                    {
                        TacticalActor tacticalActor = target.Actor as TacticalActor;
                        if (tacticalActor != null && !tacticalActor.IsRevealedToViewer)
                        {
                            __result = null;

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


        [HarmonyPatch(typeof(TacticalAbility), "get_EquipmentWithTags")] //VERIFIED
        public static class TFTV_TacticalAbility_get_EquipmentWithTags
        {
            public static void Postfix(TacticalAbility __instance, ref Equipment __result)
            {
                try
                {
                    if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                    {
                        if (__instance.SelectedEquipment != null && __instance.SelectedEquipment.GameTags.Contains(DefCache.GetDef<GameTagDef>("SilencedWeapon_TagDef")))
                        {
                            __result = null;
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


        [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.Activate))]
        public static class TFTV_ShootAbility_Activate
        {
            public static void Prefix(ShootAbility __instance)
            {
                try
                {
                    if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                    {
                        _usingEchoHead = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(Weapon), nameof(Weapon.IsAttackSilent))]
        public static class TFTV_Weapon_IsAttackSilent
        {
            public static void Postfix(Weapon __instance, ref bool __result)
            {
                try
                {
                    if (_usingEchoHead)
                    {
                        __result = true;
                        _usingEchoHead = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalFactionVision), "LocateRandomEnemyIfNeeded")] //VERIFIED
        public static class TFTV_TacticalFactionVision_LocateRandomEnemyIfNeeded
        {
            public static bool Prefix(TacticalFactionVision __instance)
            {
                try
                {
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //Prevents AI from consindering unseen enemies when evaluating attacks with explosives/cone weapons
        private static bool CheckVisibility(TacticalActorBase tacticalActorBase, TacticalActor tacticalActor, DamagePayload damagePayload)
        {
            try
            {
                if (damagePayload.DamageDeliveryType == DamageDeliveryType.Sphere || damagePayload.DamageDeliveryType == DamageDeliveryType.Cone)
                {
                    if (tacticalActor.TacticalFaction == tacticalActorBase.TacticalFaction)
                    {
                        return true;
                    }

                    if (tacticalActor.TacticalFaction.GetAllAliveFriendlyActors<TacticalActorBase>(tacticalActor).Contains(tacticalActorBase))
                    {
                        return true;
                    }

                    if (tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(ActorType.All, true).Contains(tacticalActorBase))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
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

        [HarmonyPatch(typeof(AIUtil), nameof(AIUtil.GetAffectedTargetsByShooting))]
        public static class TFTV_AIUtil_GetAffectedTargetsByShooting_patch
        {
            private static IEnumerable<TacticalActorBase> Postfix(IEnumerable<TacticalActorBase> results, Vector3 shootPos, TacticalActor sourceActor, Weapon sourceWeapon, TacticalAbilityTarget target, ShootAbilityDef shootAbility = null)
            {

                DamagePayload damagePayload = sourceWeapon.GetDamagePayload();

                foreach (TacticalActorBase actorBase in results)
                {
                    if (CheckVisibility(actorBase, sourceActor, damagePayload))
                    {
                        yield return actorBase;
                    }

                }
            }

        }
    }
}
