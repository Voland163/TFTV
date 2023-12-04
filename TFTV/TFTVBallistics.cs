using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System.Reflection;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.View.ViewControllers;

namespace TFTV
{
    internal class TFTVBallistics
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        //Make bullets go through Umbra and Decoy, method by Dimitar "Codemite" Evtimov from Snapshot Games

        public static IDamageReceiver GetDamageReceiver(DamagePredictor predictor, GameObject gameObject, Vector3 pos, Quaternion rot)
        {
            IDamageable damageableObject = gameObject.GetComponentInParent<IDamageable>();
            if (damageableObject == null)
            {
                return null;
            }

            IDamageReceiver recv = damageableObject.GetDamageReceiverForHit(pos, rot * Vector3.forward); ;
            if (predictor != null)
            {
                recv = predictor.GetPredictingReceiver(recv);
            }

            return recv;
        }

        public static Dictionary<Projectile, List<TacticalActor>> ProjectileActor = new Dictionary<Projectile, List<TacticalActor>>();
        public static Dictionary<Projectile, List<string>> ProjectileSlotName = new Dictionary<Projectile, List<string>>();

        //Also clear projectiles Dictionary, used to make bullets go through Umbra/Decoy

        public static void ClearBallisticInfoOnAbilityExecuteFinished() 
        {
            try
            {
                ProjectileActor.Clear();
                ProjectileSlotName.Clear();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(TacticalActor), "Die")]

        public static class TacticalActor_Die_patch
        {
            public static bool Prefix(TacticalActor __instance)
            {
                try
                {
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");

                    if (!__instance.IsObject() && __instance.ActorDef == dcoy)
                    {
                        // TFTVLogger.Always("It's a decoy!");
                        __instance.gameObject.SetActive(false);
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

        


        [HarmonyPatch(typeof(ProjectileLogic), "OnProjectileHit")]

        public static class ProjectileLogic_OnProjectileHit_Umbra_Patch
        {
            public static bool Prefix(ProjectileLogic __instance, ref bool __result, DamageAccumulation ____damageAccum, CastHit hit, Vector3 dir)
            {
                try
                {

                    Vector3 pos = hit.Point;
                    Quaternion rot = Quaternion.LookRotation(dir);
                    IDamageReceiver receiver = GetDamageReceiver(__instance.Predictor, hit.Collider.gameObject, pos, rot);


                    ClassTagDef umbraClassTag = DefCache.GetDef<ClassTagDef>("Umbra_ClassTagDef");
                    SpawnedActorTagDef decoy = DefCache.GetDef<SpawnedActorTagDef>("Decoy_SpawnedActorTagDef");


                    if (__instance.Predictor != null)
                    {
                        receiver = __instance.Predictor.GetPredictingReceiver(receiver);
                    }

                    TacticalActor hitActor = receiver?.GetActor() as TacticalActor;
                    string slotName = receiver?.GetSlotName();

                    if (hitActor != null && __instance.Projectile != null)
                    {
                        if (hitActor.HasGameTag(umbraClassTag) || hitActor.HasGameTag(decoy))
                        {

                            __result = false;

                            if (ProjectileActor.ContainsKey(__instance.Projectile) && ProjectileActor[__instance.Projectile] != null && ProjectileActor[__instance.Projectile].Contains(hitActor))
                            {

                                __instance.Projectile.OnProjectileHit(hit);
                                ____damageAccum?.ResetToInitalAmount();

                            }
                            else
                            {


                                __instance.Projectile.OnProjectileHit(hit);


                                MethodInfo affectTargetMethod = typeof(ProjectileLogic).GetMethod("AffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                                affectTargetMethod.Invoke(__instance, new object[] { hit, dir });

                                ____damageAccum?.ResetToInitalAmount();

                                if (ProjectileActor.ContainsKey(__instance.Projectile))
                                {
                                    ProjectileActor[__instance.Projectile].Add(hitActor);
                                }
                                else
                                {
                                    ProjectileActor.Add(__instance.Projectile, new List<TacticalActor> { hitActor });
                                }


                            }
                            return false;
                        }
                        else if (__instance.Projectile.TryGetWeapon().WeaponDef == DefCache.GetDef<WeaponDef>("AC_Scorpion_WeaponDef") && slotName != "")
                        {
                            // TFTVLogger.Always("here we are!!");

                            __result = false;

                            if (ProjectileSlotName.ContainsKey(__instance.Projectile) && ProjectileSlotName[__instance.Projectile] != null && ProjectileSlotName[__instance.Projectile].Contains(slotName))
                            {

                                __instance.Projectile.OnProjectileHit(hit);
                                ____damageAccum?.ResetToInitalAmount();

                            }
                            else
                            {


                                __instance.Projectile.OnProjectileHit(hit);


                                MethodInfo affectTargetMethod = typeof(ProjectileLogic).GetMethod("AffectTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                                affectTargetMethod.Invoke(__instance, new object[] { hit, dir });

                                ____damageAccum?.ResetToInitalAmount();

                                if (ProjectileSlotName.ContainsKey(__instance.Projectile))
                                {
                                    ProjectileSlotName[__instance.Projectile].Add(slotName);
                                }
                                else
                                {
                                    ProjectileSlotName.Add(__instance.Projectile, new List<string> { slotName });
                                }


                            }
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
        }

        //Makes D-Coy disappear instead of dying

        [HarmonyPatch(typeof(DieAbility), "Activate")]
        public static class DieAbility_Activate_Decoy_Patch
        {
            public static bool Prefix(DieAbility __instance)
            {
                try
                {


                    // TFTVLogger.Always("DieTriggered");
                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");

                    if (!__instance.TacticalActorBase.IsObject() && __instance.TacticalActorBase.ActorDef == dcoy)
                    {
                        // TFTVLogger.Always("It's a decoy!");
                        __instance.TacticalActor.gameObject.SetActive(false);
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

        //D-Coy patch to remove if attacked by "smart" enemy
        public static void RemoveDCoy(TacticalActor actor, IDamageDealer damageDealer) 
        {

            try
            {
                if (damageDealer != null)
                {

                    TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");
                    ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                    ClassTagDef tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                    ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                    ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                    ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                    GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");

                    /* TFTVLogger.Always($"actor is null? {actor == null}");
                     TFTVLogger.Always($"actor is {actor.name}");
                     TFTVLogger.Always($"damagedealer null? {damageDealer == null}");
                     TFTVLogger.Always($"damagePayload null? {damageDealer.GetDamagePayload() == null}");*/

                    if (damageDealer.GetDamagePayload() == null || actor == null) //|| damageDealer.GetDamagePayload().DamageKeywords.Count==0) 
                    {
                        return;
                    }

                    if (damageDealer.GetDamagePayload().DamageKeywords.Count() == 0)
                    {
                        return;

                    }

                    //TFTVLogger.Always($"running ActorDamageDealt damage: {damageDealer.GetDamagePayload()?.DamageKeywords?.First()?.DamageKeywordDef.name}");

                    if (actor.IsAlive)
                    {
                        if (actor.TacticalActorDef == dcoy && damageDealer != null && damageDealer != null && !damageDealer.GetDamagePayload().DamageKeywords.First().DamageKeywordDef.Equals(Shared.SharedDamageKeywords.ShockKeyword) && damageDealer.GetTacticalActorBase() != null)
                        {
                            TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                            TacticalActor attacker = attackerBase as TacticalActor;

                            if (!attacker.IsControlledByPlayer)
                            {
                                //Decoy despawned if attacked by Siren or Scylla
                                if (attacker.GameTags.Contains(sirenTag) || attacker.GameTags.Contains(queenTag)
                                    || attacker.GameTags.Contains(hopliteTag) || attacker.GameTags.Contains(cyclopsTag))
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                    TFTVHints.TacticalHints.ShowStoryPanel(actor.TacticalLevel, "HintDecoyDiscovered");
                                }
                                //Decoy despawned if attacked within 5 tiles by human, triton or acheron
                                else if ((attacker.GameTags.Contains(tritonTag)
                                    || attacker.GameTags.Contains(humanTag)
                                    || attacker.GameTags.Contains(acheronTag))
                                    && (actor.Pos - attacker.Pos).magnitude <= 5)
                                {
                                    actor.ApplyDamage(new DamageResult() { HealthDamage = actor.Health });
                                    //  TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    //  tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorHurt, actor, actor);
                                }
                            }
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
