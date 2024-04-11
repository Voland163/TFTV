using Base;
using Base.AI;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Utils.Maths;
using Epic.OnlineServices;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    internal class TFTVArtOfCrab
    {
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        private static bool Cover(TacticalActor tacticalActor, TacAITarget tacAITarget)
        {
            try 
            {
                float adjustedPerception = tacticalActor.GetAdjustedPerceptionValue();
                float perceptionRange = adjustedPerception * adjustedPerception;

                Dictionary<TacticalActorBase, Vector3> relevantEnemies = new Dictionary<TacticalActorBase, Vector3>();
            
                foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(ActorType.Combatant)))
                {
                 
                    if (!((tacAITarget.Pos - enemy.Pos).sqrMagnitude < adjustedPerception 
                        || !TacticalFactionVision.CheckVisibleLineBetweenActors(enemy, enemy.Pos, tacticalActor, checkAllPoints: true, tacAITarget.Pos)))
                    {
                        continue;
                    }

                    Vector3 vector = enemy.Pos - tacAITarget.Pos;
                    if (Utl.Equals(vector.x, 0f) && Utl.Equals(vector.z, 0f))
                    {
                        
                        continue;
                    }

                    relevantEnemies.Add(enemy, vector); 
                }

                if (relevantEnemies.Count == 0) 
                {

                    return false;
                }

                relevantEnemies.OrderBy(kvp => kvp.Value);

                TacticalActorBase closestRelevantEnemy = relevantEnemies.Keys.FirstOrDefault();

                Vector3 coverDirection = tacticalActor.TacticalPerception.GetCoverDirection(tacAITarget.Pos, relevantEnemies[closestRelevantEnemy]);
               
                CoverInfo coverInfoInDirection = tacticalActor.Map.GetCoverInfoInDirection(tacAITarget.Pos, coverDirection, tacticalActor.TacticalPerception.Height);
                   
                if(coverInfoInDirection.CoverType >= CoverType.Low) 
                {
                    return true;               
                }
                else 
                {
                    return false;            
                }
                
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


       // AIActorMovementZoneTargetGenerator

        //Prevents step out

        [HarmonyPatch(typeof(AIActionMoveToPosition), "Execute")]
        public static class AIActionMoveToPosition_Execute_patch
        {
            private static void Prefix(AIActionMoveToPosition __instance, ref AIScoredTarget aiTarget)
            {
                try
                {
                    TacticalActor actor = (TacticalActor)aiTarget.Actor;
                    TacAITarget target = (TacAITarget)aiTarget.Target;

                    if (actor.NavigationComponent.AgentNavSettings.AgentRadius >= TacticalMap.HalfTileSize || actor.Status!=null && actor.Status.HasStatus<PanicStatus>()) 
                    {
                        return;    
                    }

                    List<Weapon> weapons = new List<Weapon>(actor.Equipments.GetWeapons().Where(
                    w => w.IsUsable 
                    && w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("GunWeapon_TagDef")) 
                    && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                    ));

                    if (weapons.Count == 0)
                    {
                      return;   
                    }
                    else 
                    { 
                    weapons = weapons.OrderBy(w=>w.ApToUse).ToList(); 
                    }

                    Weapon weapon = weapons.First();

                    TFTVLogger.Always($"lowest AP ranged weapon is {weapon.DisplayName}, with {weapon.ApToUse}");
                   
                    if (actor.CharacterStats.ActionPoints <= weapon.ApToUse)
                    {
                        TFTVLogger.Always($"{actor.name} has {actor.CharacterStats.ActionPoints} AP is at POS {actor.Pos}, current target POS {target.Pos}");

                        if(!Cover(actor, target)) 
                        {
                            TFTVLogger.Always($"Position has no cover!");
                            target.Pos = actor.Pos;
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


        // TacticalActorBase

        // Def
        //AIActorEquipmentTargetGeneratorDef

        // AIStrategicPositionConsiderationDef
        //AILineOfSightToEnemiesConsiderationDef

        //AIEnemyTargetGeneratorDef
        
        public static bool Has1APWeapon(TacCharacterDef tacCharacterDef)
        {
            try
            {
                DelayedEffectStatusDef reinforcementStatusUnder1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder1AP]");
                DelayedEffectStatusDef reinforcementStatus1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatus1AP]");
                DelayedEffectStatusDef reinforcementStatusUnder2AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder2AP]");


                foreach (ItemDef itemDef in tacCharacterDef.Data.BodypartItems)
                {
                    // TFTVLogger.Always($"{itemDef.name}");

                    if (itemDef.name.Contains("Pincer") || itemDef.name.Contains("Pistol"))
                    {
                        TFTVLogger.Always($"reinforcement {tacCharacterDef.name} has {itemDef.name}, so should get {reinforcementStatus1AP.EffectName}");
                        return true;
                    }
                }

                TFTVLogger.Always($"reinforcement {tacCharacterDef.name} has no 1AP weapon, so applying {reinforcementStatusUnder2AP.EffectName}");
                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        //Culls dash evaluation so that it is only considered when character has at least 3 AP

        [HarmonyPatch(typeof(AIHasEnemiesInRangeConsideration), "Evaluate")]
        public static class AIHasEnemiesInRangeConsideration_Evaluate_patch
        {
            private static void Postfix(AIHasEnemiesInRangeConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
            {
                try
                {
                    if (__instance.Def.name.Equals("Dash_RangeClearanceConsiderationDef"))
                    {
                        TacticalActor tacticalActor = (TacticalActor)actor;

                        //  TFTVLogger.Always($"{tacticalActor.name} so far has {__result} in Dash_RangeClearanceConsiderationDef");

                        if (tacticalActor.CharacterStats.ActionPoints < tacticalActor.CharacterStats.ActionPoints.Max * 0.75f)
                        {
                            //  TFTVLogger.Always($"{tacticalActor.name} has {tacticalActor.CharacterStats.ActionPoints} AP, less than max {tacticalActor.CharacterStats.ActionPoints.Max * 0.75f}, so failing eval for dash");
                            __result = 1;
                        }



                        /*  float actorRange = tacticalActor.CharacterStats.Speed * 1.5f;

                          TFTVLogger.Always($"{tacticalActor.name} speed: {tacticalActor.CharacterStats.Speed} ap: {tacticalActor.CharacterStats.ActionPoints}" +
                              $"actorRange: {actorRange}");

                          foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask)))
                          {
                              if ((enemy.Pos - tacticalActor.Pos).magnitude <= actorRange)
                              {
                                  __result = 1;
                               //   TFTVLogger.Always($"found enemy {enemy.DisplayName} within range, but returning 0 to see what happens");
                                  return false;
                              }
                          }

                          __result = 0;
                          return false;*/


                    }

                    // return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

        }

        public static void GetBestWeaponForOWRF(TacticalActor tacticalActor)
        {
            try
            {
                if (!tacticalActor.IsControlledByAI || !tacticalActor.TacticalActorDef.name.Equals("Soldier_ActorDef") || tacticalActor.IsDead || tacticalActor.IsDisabled || tacticalActor.IsEvacuated
                    || tacticalActor.Equipments == null || tacticalActor.Equipments.GetWeapons() == null)
                {
                    return;
                }

                if (tacticalActor.GetAbility<ReturnFireAbility>("ReturnFire_AbilityDef") == null && tacticalActor.GetAbility<ApplyStatusAbility>("ExtremeFocus_AbilityDef") == null)
                {
                    return;
                }

                List<Weapon> weapons = new List<Weapon>(tacticalActor.Equipments.GetWeapons().Where(
                    w => w.IsUsable && w.HasCharges && w.TacticalItemDef.Tags.Contains(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"))
                    && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                    ));

                if (weapons.Count == 0)
                {
                    return;
                }


                Weapon bestWeapon = weapons.OrderByDescending(w => w.WeaponDef.EffectiveRange).ToList().First();

                if (tacticalActor.Equipments.SelectedWeapon != bestWeapon)
                {
                    TFTVLogger.Always($"Applying GetBestWeaponForOWRF {tacticalActor.name} was holding {tacticalActor.Equipments.SelectedWeapon.DisplayName}, switching to {bestWeapon.DisplayName}");
                    tacticalActor.Equipments.SetSelectedEquipment(bestWeapon);
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void GetBestWeaponForQA(TacticalActor tacticalActor)
        {
            try
            {
                if (!tacticalActor.IsControlledByAI || !tacticalActor.TacticalActorDef.name.Equals("Soldier_ActorDef") || tacticalActor.IsDead || tacticalActor.IsDisabled
                    || tacticalActor.IsEvacuated || tacticalActor.Equipments == null || tacticalActor.Equipments.GetWeapons() == null ||  !tacticalActor.Equipments.GetWeapons().Any(w=>w.IsUsable && w.HasCharges))
                {
                    return;
                }

                List<Weapon> weapons = new List<Weapon>(tacticalActor.Equipments.GetWeapons().Where(
                    w => w.IsUsable && w.HasCharges && w.TacticalItemDef.Tags.Contains(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"))
                    && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                    ));

                if (weapons.Count == 0)
                {
                    return;
                }

                Weapon bestWeapon = weapons.OrderByDescending(w => w.WeaponDef.EffectiveRange).ToList().First();

                if (tacticalActor.Equipments.SelectedWeapon != bestWeapon)
                {
                    TFTVLogger.Always($"Applying GetBestWeaponForQA {tacticalActor.name} was holding {tacticalActor.Equipments.SelectedWeapon.DisplayName}, switching to {bestWeapon.DisplayName}");
                    tacticalActor.Equipments.SetSelectedEquipment(bestWeapon);
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithShootAbility")]
        internal static class AIAttackPositionConsideration_EvaluateWithShootAbility_patch
        {

            public static void Postfix(AIAttackPositionConsideration __instance, IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    FumbleChanceStatusDef jammingField = DefCache.GetDef<FumbleChanceStatusDef>("E_FumbleChanceStatus [JammingFiled_AbilityDef]");

                    if (__result > 0)
                    {
                        TacticalActor tacActor = (TacticalActor)actor;
                        if (tacActor != null && tacActor.Status.HasStatus(jammingField))
                        {

                            __result *= 0.1f;
                            //  TFTVLogger.Always($"{tacActor.name} result {__result} because jinxed");
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

        internal class TurnOrder
        {



            public static void SortOutAITurnOrder(List<TacticalActor> tacticalActors)
            {
                try
                {


                    TacticalFactionVision factionVision = tacticalActors[0].TacticalFaction.Vision;
                    // TFTVLogger.Always("test 1");
                    List<TacticalActorBase> enemyBases =
                          new List<TacticalActorBase>(factionVision.GetKnownActors(KnownState.Revealed, PhoenixPoint.Common.Core.FactionRelation.Enemy));

                    List<TacticalActor> enemies = new List<TacticalActor>();

                    int ScoutCounter = 0;


                    if (enemyBases.Count > 0)
                    {
                        foreach (TacticalActorBase tacticalActorBase in enemyBases)
                        {
                            if (tacticalActorBase is TacticalActor)
                            {
                                enemies.Add(tacticalActorBase as TacticalActor);
                                //  TFTVLogger.Always("faction " + factionVision.Faction.Faction.FactionDef.name + " has revealed enemy " + tacticalActorBase.DisplayName);
                            }
                        }
                    }


                    //  TFTVLogger.Always("test 2");
                    foreach (TacticalActor tacticalActor in tacticalActors)
                    {


                        /*   if (enemies.Count > 0)
                           {
                               if (CheckForMeleeTargets(tacticalActor, enemies))
                               {
                                   tacticalActor.AIActor.SetTurnOrderPriorityOverride(-900);
                                   // TFTVLogger.Always(tacticalActor.DisplayName + " should act third");
                               }
                           }*/
                        if (enemies.Count > 0)
                        {
                            if (CheckWorms(tacticalActor, enemies))//, tacticalActors)) 
                            {
                                tacticalActor.AIActor.SetTurnOrderPriorityOverride(-950);
                                //  TFTVLogger.Always(tacticalActor.DisplayName + " should act second");
                            }
                        }
                        if (CheckUmbra(tacticalActor))
                        {
                            TFTVLogger.Always("Setting Umbra to act last");
                            tacticalActor.AIActor.SetTurnOrderPriorityOverride(1000);

                        }



                        if (CheckForShred(tacticalActor))
                        {
                            tacticalActor.AIActor.SetTurnOrderPriorityOverride(-900);

                        }

                        if (enemies.Count <= 2 && ScoutCounter < 2)
                        {
                            TFTVLogger.Always("Two or fewer enemies detected. Giving turn priority order to scouts");

                            if (CheckScouts(tacticalActor, tacticalActors))
                            {
                                tacticalActor.AIActor.SetTurnOrderPriorityOverride(-940);
                                ScoutCounter += 1;
                            }
                        }

                        if (tacticalActor.GetAbility<InstilFrenzyAbility>() != null)
                        {
                            if (CheckForFrenzyTargets(tacticalActor, tacticalActors))
                            {
                                TFTVLogger.Always(tacticalActor.DisplayName + " can cast frenzy and has at least 3 friendlies nearby who are not frenzied yet");
                                tacticalActor.AIActor.SetTurnOrderPriorityOverride(-1000);
                                //  TFTVLogger.Always(tacticalActor.DisplayName + " has highest priority");
                            }
                        }

                        if (CheckForFireQuenchers(tacticalActor))
                        {
                            TFTVLogger.Always("Found a fire quenecher and a fire to quench!");

                            tacticalActor.AIActor.SetTurnOrderPriorityOverride(-950);
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static bool CheckForFireQuenchers(TacticalActor tacticalActor)
            {
                try
                {
                    DamageMultiplierStatusDef fireQuencherStatus = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");

                    if (tacticalActor.Status.HasStatus(fireQuencherStatus))
                    {

                        float range = tacticalActor.MaxMoveRange;
                        TacticalLevelController controller = tacticalActor.TacticalLevel;
                        List<TacticalVoxelEntity> litVoxelEntities = controller.VoxelMatrix.GetEntities(TacticalVoxelType.Fire).ToList();

                        foreach (TacticalVoxelEntity tacticalVoxelEntity in litVoxelEntities)
                        {
                            foreach (TacticalVoxel voxel in tacticalVoxelEntity.Voxels)
                            {
                                if ((voxel.Position - tacticalActor.Pos).magnitude < range)
                                {

                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static bool CheckForShred(TacticalActor tacticalActor)
            {
                try
                {
                    StandardDamageTypeEffectDef shredDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Shred_StandardDamageTypeEffectDef");

                    foreach (Weapon weapon in tacticalActor.Equipments.GetWeapons())
                    {
                        foreach (DamageKeywordPair damageKeywordPair in weapon.GetDamagePayload().DamageKeywords)
                        {
                            if (damageKeywordPair.DamageKeywordDef == shredDamage)
                            {
                                TFTVLogger.Always(tacticalActor.DisplayName + " has a shredding weapon " + weapon.DisplayName + " it should move first, to shred some armor");
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static bool CheckScouts(TacticalActor tacticalActor, List<TacticalActor> friendlies)
            {
                try
                {
                    List<TacticalActor> allFactionActorsOrderedByPerception = new List<TacticalActor>(friendlies.OrderByDescending(a => a.GetAdjustedPerceptionValue()).ToList());

                    foreach (TacticalActor actor in allFactionActorsOrderedByPerception)
                    {
                        if (actor.Equipments.SelectedWeapon != null)
                        {
                            if ((!actor.Equipments.SelectedWeapon.IsUsable || actor.Equipments.SelectedWeapon.WeaponDef.APToUsePerc < 75) && actor == tacticalActor)
                            {
                                TFTVLogger.Always(actor.DisplayName + " has perception of " + actor.GetAdjustedPerceptionValue() + " and no 3AP weapon, so would make for a good scout");
                                return true;

                            }
                        }
                        else
                        {
                            foreach (Weapon weapon in actor.Equipments.GetWeapons())
                            {
                                //  TFTVLogger.Always("the weapon is " + weapon.DisplayName);

                                if (weapon.WeaponDef.APToUsePerc < 75)
                                {
                                    TFTVLogger.Always(actor.DisplayName + " has perception of " + actor.GetAdjustedPerceptionValue() + " and no 3AP weapon, so would make for a good scout");
                                    return true;
                                }
                            }

                        }
                    }
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            public static bool CheckUmbra(TacticalActor tacticalActor)
            {
                try
                {
                    if (tacticalActor.name.Contains("Oilcrab") || tacticalActor.name.Contains("Oilfish"))
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

            public static bool CheckWorms(TacticalActor actor, List<TacticalActor> enemies)//, List<TacticalActor> friendlies)
            {
                try
                {
                    GameTagDef acidWormTag = DefCache.GetDef<GameTagDef>("Acidworm_ClassTagDef");
                    GameTagDef poisonWormTag = DefCache.GetDef<GameTagDef>("Poisonworm_ClassTagDef");
                    float range = actor.MaxMoveRange + 2.5f;

                    if (actor.HasGameTag(acidWormTag) || actor.HasGameTag(poisonWormTag))
                    {
                        foreach (TacticalActor enemy in enemies)
                        {
                            if (CheckDistance(actor, enemy, range))
                            {
                                TFTVLogger.Always(enemy.DisplayName + " is within range of " + actor.DisplayName);

                                /* foreach (TacticalActor friendly in friendlies)
                                 {
                                     if (CheckDistance(friendly, enemy, 2) && friendly.GameTags != actor.GameTags)
                                     {
                                         TFTVLogger.Always(friendly.DisplayName + " not same kind of worm, is within range of " + enemy.DisplayName);
                                         return true;

                                     }
                                 }*/
                                return true;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static bool CheckDistance(TacticalActor actor, TacticalActor target, float range)
            {
                try
                {
                    if ((target.Pos - actor.Pos).magnitude < range)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }




            public static bool CheckForMeleeTargets(TacticalActor actor, List<TacticalActor> enemies)
            {
                try
                {
                    GameTagDef meleeTag = DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef");
                    Weapon meleeWeapon = null;
                    //  TacticalFactionVision factionVision = actor.TacticalFaction.Vision;

                    foreach (Weapon weapon in actor.Equipments.GetWeapons())
                    {
                        //  TFTVLogger.Always("the weapon is " + weapon.DisplayName);

                        if (weapon.WeaponDef.Tags.Contains(meleeTag))
                        {
                            meleeWeapon = weapon;
                        }
                    }

                    if (meleeWeapon != null)
                    {
                        TFTVLogger.Always("The melee weapon is " + meleeWeapon.DisplayName);
                        float range = actor.GetMaxMoveAndActRange(meleeWeapon);
                        TFTVLogger.Always("The actor " + actor.DisplayName + " has a melee weapon " + meleeWeapon.DisplayName + " and has maximum move and act range of " + range);

                        foreach (TacticalActor enemy in enemies)
                        {
                            if (CheckDistance(actor, enemy, range))
                            {
                                TFTVLogger.Always(enemy.DisplayName + " is within range of " + actor.DisplayName);
                                return true;
                            }

                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            public static bool CheckForFrenzyTargets(TacticalActor actor, List<TacticalActor> tacticalActors)
            {
                try
                {
                    FrenzyStatusDef frenzyStatusDef = DefCache.GetDef<FrenzyStatusDef>("Frenzy_StatusDef");
                    GameTagDef chironTag = DefCache.GetDef<GameTagDef>("Chiron_ClassTagDef");

                    float range = actor.GetAbility<InstilFrenzyAbility>().InstilFrenzyAbilityDef.TargetingDataDef.Origin.Range;
                    TFTVLogger.Always("The actor " + actor.DisplayName + " has a frenzy ability and has maximum range of " + range);
                    int counter = 0;

                    foreach (TacticalActor targetActor in tacticalActors)
                    {
                        if (!targetActor.HasStatus(frenzyStatusDef) && targetActor != actor && CheckDistance(actor, targetActor, range) && !targetActor.HasGameTag(chironTag))
                        {
                            counter++;
                        }
                    }

                    if (counter >= 3)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }



        }
        internal class ScyllaBlasterAttack
        {



            [HarmonyPatch(typeof(AISlowTargetsInConeConsideration), "Evaluate")]
            public static class AISlowTargetsInConeConsideration_GetMovementDataInRange_patch
            {
                private static bool Prefix(AISlowTargetsInConeConsideration __instance, ref float __result, IAIActor actor, IAITarget target)
                {
                    try
                    {

                        MethodInfo method = typeof(AISlowTargetsInConeConsideration).GetMethod("GetEnemiesInCone", BindingFlags.NonPublic | BindingFlags.Instance);

                        TacticalActor tacticalActor = (TacticalActor)actor;
                        TacAITarget tacAITarget = (TacAITarget)target;
                        List<TacticalActorBase> list = tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask)).ToList();
                        if (list.Count == 0)
                        {
                            __result = 0f;
                            return false;
                        }

                        float num = 0f;
                        foreach (Vector3 allGridDirection in TacticalMap.AllGridDirections)
                        {
                            Vector3 zero = Vector3.zero;
                            float num2 = 0f;

                            Vector3 actorPos = tacAITarget.Pos;
                            Vector3 actorDir = tacAITarget.Pos;

                            IEnumerable<TacticalActorBase> enemies = list;

                            // Invoke the method on the instance
                            IEnumerable<TacticalActorBase> enemiesInCone = (IEnumerable<TacticalActorBase>)method.Invoke(__instance, new object[] { actorPos, actorDir, enemies });

                            List<TacticalActorBase> toRemoveBecauseCompletelyBlocked = new List<TacticalActorBase>();

                            foreach (TacticalActorBase tacticalActorBase in list)
                            {
                                if (tacticalActorBase is TacticalActor tacticalActor1)
                                {
                                    if (!TacticalFactionVision.CheckVisibleLineBetweenActorsInTheory(tacticalActor, actorPos, DefCache.GetDef<ComponentSetDef>("Queen_ComponentSetDef"), tacticalActor1.Pos))
                                    {
                                        //  TFTVLogger.Always($"can't see {tacticalActor1.DisplayName} from {actorPos}");
                                        toRemoveBecauseCompletelyBlocked.Add(tacticalActorBase);
                                    }
                                }
                            }

                            List<TacticalActorBase> culledList = new List<TacticalActorBase>(enemiesInCone);

                            culledList.RemoveRange(toRemoveBecauseCompletelyBlocked);


                            foreach (TacticalActorBase item in culledList)
                            {
                                // TFTVLogger.Always($"{item.name}");

                                /*  TacticalActor tacticalActor2 = item as TacticalActor;
                                  int num3 = 0;
                                  if (tacticalActor2 != null)
                                  {
                                      //TFTVLogger.Always($"{tacticalActor2.name}");
                                      num3 = tacticalActor2.CharacterStats.Speed.IntValue;
                                  }

                                  float num4 = Mathf.Min(1f - Mathf.Min((float)num3 / 20f, 1f), 1f);
                                  zero += (item.Pos - tacAITarget.Pos).normalized;
                                  num2 += num4;*/
                                zero += (item.Pos - tacAITarget.Pos).normalized;
                                num2 += 1;
                            }

                            if (num < num2)
                            {
                                num = num2;
                                tacAITarget.Direction = zero.normalized;
                                tacAITarget.AngleInRadians = __instance.Def.FieldOfView / 2f * ((float)Math.PI / 180f);
                                tacAITarget.TacticalAbilityTarget = new TacticalAbilityTarget(tacAITarget.Pos + tacAITarget.Direction * 30f);
                            }
                        }

                        __result = Mathf.Min(num / (float)__instance.Def.NumberOfTartgetsToConsider, 1f);
                        // TFTVLogger.Always($"result is {__result}");
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }

            public static void ForceScyllaToUseCannonsAfterUsingHeadAttack(TacticalAbility ability, TacticalActor actor, object parameter)
            {
                try
                {
                    //    TacticalAbilityTarget target = parameter as TacticalAbilityTarget;
                    //  TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName} and the TacticalAbilityTarget position to apply is {target.PositionToApply} ");

                    ShootAbilityDef scyllaSpit = DefCache.GetDef<ShootAbilityDef>("GooSpit_ShootAbilityDef");
                    ShootAbilityDef scyllaScream = DefCache.GetDef<ShootAbilityDef>("SonicBlast_ShootAbilityDef");

                    //   TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");
                    if (ability.TacticalAbilityDef == scyllaSpit || ability.TacticalAbilityDef == scyllaScream)
                    {
                        StartPreparingShootAbilityDef scyllaStartPreparing = DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef");
                        //    TFTVLogger.Always("");
                        StartPreparingShootAbility startPreparingShootAbility = actor.GetAbilityWithDef<StartPreparingShootAbility>(scyllaStartPreparing);

                        startPreparingShootAbility?.Activate(parameter);
                    }


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        internal class SquashCrittersForScylllaCyclopsChiron
        {
            [HarmonyPatch(typeof(CaterpillarMoveAbility), "AbilityAdded")]
            internal static class TFTV_CaterpillarMoveAbility_AbilityAdded_ScyllaCaterpillar_patch
            {

                public static void Postfix(CaterpillarMoveAbility __instance)
                {
                    try
                    {
                        if (__instance.TacticalActor.ActorDef.name.Equals("Queen_ActorDef"))
                        {

                            TacticalNavigationComponent component = __instance.TacticalActor.GetComponent<TacticalNavigationComponent>();
                            string[] dilloNavAreas = new string[] { "WalkableArmadilloWorms" };
                            string[] extraNavAreas = new string[] { "WalkableBigMonster" };


                            __instance.TacticalActor.TacticalNav.RemoveNavAreas(dilloNavAreas);
                            __instance.TacticalActor.TacticalNav.AddNavAreas(extraNavAreas);
                            //  TFTVLogger.Always($"{__instance.TacticalActor.DisplayName} has {component.NavAreas.GetAreaCount()} navigation areas, " +
                            //       $"navcomp agent is {component.AgentTypeName}");

                        }
                        else if (__instance.TacticalActor.ActorDef.name.Equals("MediumGuardian_ActorDef"))
                        {
                            //add Scylla navAreas to Cyclops so that it is less constrained by scenery
                            TacticalNavigationComponent component = __instance.TacticalActor.GetComponent<TacticalNavigationComponent>();
                            string[] extraNavAreas = new string[] { "WalkableBigMonster" };
                            __instance.TacticalActor.TacticalNav.AddNavAreas(extraNavAreas);

                            // TFTVLogger.Always($"{__instance.TacticalActor.DisplayName} has {component.NavAreas.GetAreaCount()} navigation areas, " +
                            //     $"navcomp agent is {component.AgentTypeName}");


                            //Refresh cache> Codemite trick
                            component.CurrentPath = component.CreatePathRequest();

                        }
                        else if (__instance.TacticalActor.ActorDef.name.Equals("Chiron_ActorDef"))
                        {

                            TacticalNavigationComponent component = __instance.TacticalActor.GetComponent<TacticalNavigationComponent>();

                            /*    string[] extraNavAreas = new string[] { "WalkableMedMonster" };

                                __instance.TacticalActor.TacticalNav.AddNavAreas(extraNavAreas);*/

                            /*   TFTVLogger.Always($"{__instance.TacticalActor.DisplayName} has {component.NavAreas.GetAreaCount()} navigation areas, " +
                                   $"navcomp agent is {component.AgentTypeName}");


                               TacticalDemolitionComponent demo = __instance.TacticalActor.GetComponent<TacticalDemolitionComponent>();
                               demo.TacticalDemolitionComponentDef.RectangleCenter = new Vector3(0f, 1.5f, 0f);*/
                            component.CurrentPath = component.CreatePathRequest();
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }
            }

            [HarmonyPatch(typeof(CaterpillarMoveAbility), "Activate")]
            internal static class TFTV_CaterpillarMoveAbility_Activate_ScyllaCaterpillar_patch
            {

                public static void Postfix(CaterpillarMoveAbility __instance)
                {
                    try
                    {
                        if (__instance.TacticalActor.ActorDef.name.Equals("Queen_ActorDef")
                            || __instance.TacticalActor.ActorDef.name.Equals("MediumGuardian_ActorDef")
                            || __instance.TacticalActor.ActorDef.name.Equals("Chiron_ActorDef"))
                        {
                            EffectTarget effectTarget = new EffectTarget() { Object = __instance.TacticalActor.gameObject };

                            TacStatusEffectDef scyllaImmunity = DefCache.GetDef<TacStatusEffectDef>("E_MakeImmuneToBlastDamageEffect [ScyllaSquisher]");
                            Effect.Apply(Repo, scyllaImmunity, effectTarget, __instance.TacticalActor);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }
            }

            [HarmonyPatch(typeof(TacticalActor), "OnFinishedMovingActor")]
            public static class TacticalActor_OnFinishedMovingActor_Scylla_Experiment_patch
            {
                public static void Postfix(TacticalActor __instance)
                {
                    try
                    {
                        //   CaterpillarMoveAbilityDef caterpillarAbility = DefCache.GetDef<CaterpillarMoveAbilityDef>("CaterpillarMoveAbilityDef");

                        //  if (ability.TacticalAbilityDef == caterpillarAbility)
                        //  {

                        if (__instance.ActorDef.name.Equals("Queen_ActorDef")
                          || __instance.ActorDef.name.Equals("MediumGuardian_ActorDef")
                          || __instance.ActorDef.name.Equals("Chiron_ActorDef"))
                        {
                            DamageMultiplierStatusDef scyllaImmunity = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [ScyllaSquisher]");

                            //   TFTVLogger.Always($"{__instance.DisplayName} moved");

                            if (__instance.HasStatus(scyllaImmunity))
                            {
                                TFTVLogger.Always($"{__instance.DisplayName} has {scyllaImmunity.name}");
                                Status status = __instance.Status.GetStatusByName(scyllaImmunity.EffectName);
                                __instance.Status.Statuses.Remove(status);
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
        internal class PreventDamageFromSomeExplosions
        {
            [HarmonyPatch(typeof(DieAbility), "SpawnDeathEffect")]
            internal static class TFTV_DieAbility_SpawnDeathEffect_patch
            {

                public static bool Prefix(DieAbility __instance)
                {
                    try
                    {
                        //  TFTVLogger.Always($"running SpawnDeathEffect for {__instance.TacticalAbilityDef.name}.");
                        TacticalAbility tacticalAbility = TacUtil.GetSourceOfType<TacticalAbility>(__instance.TacticalActorBase.LastDamageSource);

                        if (tacticalAbility != null)
                        {

                            // TFTVLogger.Always($"tactical ability def is {tacticalAbility.TacticalAbilityDef.name}");
                            if (tacticalAbility.TacticalAbilityDef is RemoveFacehuggerAbilityDef)
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
            }

            [HarmonyPatch(typeof(ApplyDamageEffectAbility), "GetCharactersToIgnore")]
            public static class ApplyDamageEffectAbility_Activate_Scylla_Caterpillar_patch
            {
                public static void Postfix(ApplyDamageEffectAbility __instance, ref IEnumerable<TacticalActorBase> __result)
                {
                    try
                    {
                        //  TFTVLogger.Always($"applying damage effect ability {__instance.AbilityDef.name}");
                        RagdollDieAbilityDef mindFraggerExplosion = DefCache.GetDef<RagdollDieAbilityDef>("BC_SwarmerAcidExplosion_Die_AbilityDef");

                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");
                        if (__instance.TacticalActor.HasGameTag(damagedByCaterpillar) && __instance.TacticalActor.TacticalFaction != controller.CurrentFaction)
                        {
                            CaterpillarMoveAbilityDef scyllaSquisher = DefCache.GetDef<CaterpillarMoveAbilityDef>("ScyllaSquisher");

                            List<TacticalActorBase> additionalCharactersToIgnore = new List<TacticalActorBase>(__result);

                            additionalCharactersToIgnore.AddRange(

                                from t in __instance.TacticalActorBase.Map.GetActors<TacticalActor>()
                                where t.GetAbilityWithDef<CaterpillarMoveAbility>(scyllaSquisher) != null
                                select t

                            );

                            __result = additionalCharactersToIgnore;

                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }
        internal class TargetCulling
        {
            internal class Umbra
            {
                //Need to patch to check if enemy has Delirium, is in Mist or VO in effect
                //  AIVisibleEnemiesConsiderationDef aIVisibleEnemiesConsiderationDef = DefCache.GetDef<AIVisibleEnemiesConsiderationDef>("AnyFactionVisibleEnemy_AIConsiderationDef");

                //Need to patch to check if enemy has Delirium, is in Mist or VO in effect, so it mirrors anyFactionVisibleEnemyConsideration
                //   AIVisibleEnemiesConsiderationDef NOaIVisibleEnemiesConsiderationDef = DefCache.GetDef<AIVisibleEnemiesConsiderationDef>("NoFactionVisibleEnemy_AIConsiderationDef");

                private static bool CheckIfUmbra(TacticalActor actor)
                {
                    try
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[16])
                        {
                            return false;
                        }

                        if (actor == null)
                        {
                            return false;
                        }

                        if (actor.ActorDef.name.Equals("Oilcrab_ActorDef") || actor.ActorDef.name.Equals("Oilfish_ActorDef"))
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

                private static bool CheckValidTarget(TacticalActor targetActor)
                {
                    try
                    {
                        return targetActor.CharacterStats.Corruption > 0 || targetActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                [HarmonyPatch(typeof(AIVisibleEnemiesConsideration), "Evaluate")]
                public static class AIVisibleEnemiesConsideration_Evaluate_patch
                {
                    public static bool Prefix(AIVisibleEnemiesConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {

                            TacticalActor tacticalActor = (TacticalActor)actor;

                            if (!CheckIfUmbra(tacticalActor))
                            {
                                return true;
                            }

                            MethodInfo enemiesToConsiderMethod = typeof(AIVisibleEnemiesConsideration).GetMethod("EnemiesToConsider", BindingFlags.Instance | BindingFlags.NonPublic);
                            TacAITarget tacTarget = target as TacAITarget;


                            if (__instance.Def.VisibilityType == VisibilityType.Faction)
                            {
                                IEnumerable<TacticalActorBase> source = (IEnumerable<TacticalActorBase>)enemiesToConsiderMethod.Invoke(__instance, new object[] { tacTarget, tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask)) });
                                IEnumerable<TacticalActorBase> culledSource = source.Where(tab => tab is TacticalActor tacActor && CheckValidTarget(tacActor));

                                if (__instance.Def.Visible)
                                {
                                    if (!culledSource.Any())
                                    {
                                        __result = 0f;
                                        return false;
                                    }

                                    __result = 1f;
                                    return false;
                                }

                                if (!culledSource.Any())
                                {
                                    __result = 1f;
                                    return false;
                                }

                                __result = 0f;
                                return false;
                            }

                            __result = 0f;

                            return false;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                [HarmonyPatch(typeof(AIClosestEnemyConsideration), "Evaluate")]
                public static class AIClosestEnemyConsideration_Evaluate_patch
                {
                    public static bool Prefix(AIClosestEnemyConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {
                            AIClosestEnemyConsiderationDef aIClosestEnemyConsiderationDef = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Umbra_ClosestPathToEnemy_AIConsiderationDef");

                            if (__instance.BaseDef != aIClosestEnemyConsiderationDef)
                            {
                                return true;
                            }

                            TacticalActor tacticalActor = (TacticalActor)actor;

                            if (!CheckIfUmbra(tacticalActor))
                            {
                                return true;
                            }

                            TacAITarget tacAITarget = (TacAITarget)target;
                            float num = 0f;
                            TacticalActorBase tacticalActorBase = null;
                            foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(aIClosestEnemyConsiderationDef.EnemyMask)))
                            {
                                if (enemy is TacticalActor targetActor && CheckValidTarget(targetActor))
                                {
                                    float num2 = 0f;
                                    float num3 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, tacticalActor);
                                    float num4 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy);
                                    float destinationRadius = num3 + num4 + aIClosestEnemyConsiderationDef.DestinationRadius;
                                    num2 = AIUtil.GetPathLength(tacticalActor, tacAITarget.Pos, enemy.Pos, useAStar: true, destinationRadius);

                                    float num5 = num2.Clamp(aIClosestEnemyConsiderationDef.MinDistance, aIClosestEnemyConsiderationDef.MaxDistance);
                                    float num6 = 1f - (num5 - aIClosestEnemyConsiderationDef.MinDistance) / (aIClosestEnemyConsiderationDef.MaxDistance - aIClosestEnemyConsiderationDef.MinDistance);

                                    if (num6 > num)
                                    {
                                        num = num6;
                                        tacticalActorBase = enemy;
                                    }
                                }
                            }

                            if (tacticalActorBase == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            num = Mathf.Clamp(num, 0f, 1f);
                            if (Utl.Equals(num, 0f, 0.01f))
                            {
                                num = 0.1f;
                            }

                            __result = num;
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
            internal class Healing
            {

                [HarmonyPatch(typeof(AIHealPositionConsideration), "Evaluate")]
                public static class AIHealPositionConsideration_Evaluate_patch
                {
                    private static bool CheckActorSuitability(TacticalActor targetActor, TacticalActor actor)
                    {
                        try
                        {
                            //if target or actor is MCed, don't heal
                            if (targetActor.Status.HasStatus<MindControlStatus>() || actor.Status.HasStatus<MindControlStatus>())
                            {
                                return false;
                            }

                            //unless actor is Aspida, don't heal actor in fire, unless the target is actor, don't heal actors with disabled hands unless they have ignore pain
                            if (actor.GetAbilityWithDef<HealAbility>(DefCache.GetDef<HealAbilityDef>("SY_FullRestoration_AbilityDef")) == null)
                            {
                                if ((targetActor.TacticalPerception.IsTouchingVoxel(TacticalVoxelType.Fire) && targetActor != actor)
                                    || !actor.Status.HasStatus<FreezeAspectStatsStatus>() && actor.HasLostHandStatus())
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


                    public static bool Prefix(AIHealPositionConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {

                            TacticalActor obj = (TacticalActor)actor;
                            TacAITarget tacAITarget = (TacAITarget)target;
                            HealAbility healAbility = obj.GetAbilities<HealAbility>().FirstOrDefault();
                            if (healAbility == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            IEnumerable<TacticalAbilityTarget> targetsAt = healAbility.GetTargetsAt(tacAITarget.Pos);
                            TacticalAbilityTarget tacticalAbilityTarget = null;
                            float num = 0f;
                            foreach (TacticalAbilityTarget item in targetsAt)
                            {
                                float num2 = item.Actor.Health.Value;
                                float num3 = (float)item.Actor.Health.Max - num2;

                                //adding check if actor or target are mindcontrolled, etc.
                                if (item.Actor is TacticalActor tacticalActor && !CheckActorSuitability(tacticalActor, obj))
                                {
                                    num3 = 0;
                                }

                                if (num3 > num)
                                {
                                    tacticalAbilityTarget = item;
                                    num = num3;
                                }
                            }

                            if (num > 0f)
                            {
                                tacAITarget.Actor = tacticalAbilityTarget.Actor;
                                __result = num / (float)tacAITarget.Actor.Health.Max;
                                return false;
                            }

                            __result = 0f;

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
            internal class MindControl
            {

                [HarmonyPatch(typeof(AIMindControlPickAvailableTargetConsideration), "Evaluate")]
                public static class AIMindControlPickAvailableTargetConsideration_Evaluate_patch
                {

                    private static float CheckActorSuitability(TacticalActor actor)
                    {
                        try
                        {
                            float score = 1f;
                            int hands = 2;
                            int legs = 2;

                            if (actor.Status.HasStatus<FreezeAspectStatsStatus>())
                            {
                                return score;
                            }

                            foreach (ItemSlot bodyPart in actor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled))
                            {
                                if (bodyPart.ItemSlotDef.ProvidesHand)
                                {
                                    hands--;
                                }
                                else if (bodyPart.ItemSlotDef.SlotName == "LeftLeg" || bodyPart.ItemSlotDef.SlotName == "RightLeg")
                                {
                                    legs--;
                                }
                            }

                            if (hands == 0 || legs == 0)
                            {
                                score = 0f;
                            }
                            else
                            {
                                score -= 0.25f * (2 - hands);
                                score -= 0.25f * (2 - legs);
                            }

                            return score;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    public static bool Prefix(AIMindControlPickAvailableTargetConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {

                            TacticalActor tacticalActor = (TacticalActor)actor;
                            TacAITarget tacAITarget = (TacAITarget)target;
                            MindControlAbility mindControlAbility = __instance.Def.GetAbility(tacticalActor, tacAITarget) as MindControlAbility;
                            if (mindControlAbility == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            IEnumerable<TacticalAbilityTarget> abilityTargets = null;
                            if (tacAITarget.IsPosValid)
                            {
                                abilityTargets = mindControlAbility.GetTargetsAt(tacAITarget.Pos);
                            }
                            else
                            {
                                abilityTargets = mindControlAbility.GetTargets();
                            }

                            abilityTargets = abilityTargets.Where((TacticalAbilityTarget t) => t.Actor != null && !t.Actor.IsDisabled || !t.Actor.IsDeadNextTurn()); //added checks IsDisabled and IsDeadNextTurn
                            IEnumerable<TacticalActorBase> enumerable = from a in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(__instance.Def.EnemyMask))
                                                                        where abilityTargets.Any((TacticalAbilityTarget t) => t.Actor == a)
                                                                        select a;
                            float num = -1f;
                            TacticalActorBase enemyWithMaxWeight = null;
                            foreach (TacticalActorBase item in enumerable)
                            {
                                float enemyWeight = AIUtil.GetEnemyWeight(tacticalActor.TacticalFaction.AIBlackboard, item);
                                float num2 = 1f;
                                if ((int)(float)tacticalActor.CharacterStats.Willpower != 0)
                                {
                                    num2 = Mathf.Clamp(((float)tacticalActor.CharacterStats.WillPoints - (float)((TacticalActor)item).CharacterStats.WillPoints - (float)((TacticalActor)item).TacticalActorDef.WillPointWorth) / (float)tacticalActor.CharacterStats.Willpower, 0f, 1f);
                                }
                                else if (mindControlAbility.MindControlAbilityDef.CheckAgainstTargetWillPoints)
                                {
                                    __result = 0f;
                                    return false;
                                }

                                //Adding to make targets touching fire less appealing
                                if (item != null)
                                {
                                    if (item.TacticalPerceptionBase != null && item.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Fire))
                                    {
                                        num2 *= 0.5f;
                                    }

                                    //setting score to 0 if both arms or both legs disabled, reducing by 0.25f per disabled arm or leg otherwise
                                    if (item is TacticalActor targetTacticalActor)
                                    {
                                        num2 *= CheckActorSuitability(targetTacticalActor);
                                    }
                                }
                                enemyWeight *= num2;
                                if (enemyWeight > num)
                                {
                                    num = enemyWeight;
                                    enemyWithMaxWeight = item;
                                }
                            }

                            TacticalAbilityTarget tacticalAbilityTarget = abilityTargets.FirstOrDefault((TacticalAbilityTarget t) => t.Actor == enemyWithMaxWeight);
                            if (tacticalAbilityTarget == null)
                            {
                                __result = 0f;
                                return false;
                            }

                            tacAITarget.Actor = tacticalAbilityTarget.Actor;
                            __result = Mathf.Clamp(num, 0f, 1f);


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
            internal class SmallCritters
            {

                //This removes small critters from move related considerations of the Big and Med monsters.

                [HarmonyPatch(typeof(AIClosestEnemyConsideration), "Evaluate")]
                public static class AIClosestEnemyConsideration_Evaluate_patch
                {

                    public static bool Prefix(AIClosestEnemyConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {

                            AIClosestEnemyConsiderationDef queenConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Queen_ClosestEnemy_AIConsiderationDef");
                            AIClosestEnemyConsiderationDef chironConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Chiron_ClosestEnemy_AIConsiderationDef");
                            AIClosestEnemyConsiderationDef acheronConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Acheron_ClosestLineToEnemy_AIConsiderationDef");

                            List<AIClosestEnemyConsiderationDef> aIClosestEnemyConsiderationDefs = new List<AIClosestEnemyConsiderationDef>() { queenConsideration, chironConsideration, acheronConsideration };

                            GameTagDef caterpillarDamage = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                            if (aIClosestEnemyConsiderationDefs.Contains(__instance.BaseDef))
                            {

                                AIClosestEnemyConsiderationDef Def = (AIClosestEnemyConsiderationDef)__instance.BaseDef;
                                TacticalActor tacticalActor = (TacticalActor)actor;
                                TacAITarget tacAITarget = (TacAITarget)target;
                                float num = 0f;
                                TacticalActorBase tacticalActorBase = null;

                                //  TFTVLogger.Always($"{tacticalActor.name} considering closest enemy");

                                foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(Def.EnemyMask)).Where(ta => !ta.HasGameTag(caterpillarDamage)))
                                {

                                    float num2 = 0f;
                                    if (Def.DistanceType == DistanceType.Line)
                                    {
                                        num2 = (tacAITarget.Pos - enemy.Pos).magnitude;
                                    }
                                    else if (Def.DistanceType == DistanceType.PathLength)
                                    {
                                        float num3 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, tacticalActor);
                                        float num4 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy);
                                        float destinationRadius = num3 + num4 + Def.DestinationRadius;
                                        num2 = AIUtil.GetPathLength(tacticalActor, tacAITarget.Pos, enemy.Pos, useAStar: true, destinationRadius);
                                    }

                                    float num5 = num2.Clamp(Def.MinDistance, Def.MaxDistance);
                                    float num6 = 1f - (num5 - Def.MinDistance) / (Def.MaxDistance - Def.MinDistance);
                                    if (Def.ConsiderEnemyWeight)
                                    {
                                        num6 *= AIUtil.GetEnemyWeight(tacticalActor.TacticalFaction.AIBlackboard, enemy);
                                    }

                                    if (num6 > num)
                                    {
                                        num = num6;
                                        tacticalActorBase = enemy;
                                    }

                                    // TFTVLogger.Always($"the enemy is {enemy.name} and their score is {num6}");
                                }

                                if (tacticalActorBase == null)
                                {
                                    __result = Def.NoEnemiesValue;
                                    //  TFTVLogger.Always($"no enemies!");
                                    return false;
                                }

                                if (Def.SetEnemyAsTarget)
                                {
                                    tacAITarget.Actor = tacticalActorBase;
                                }

                                num = Mathf.Clamp(num, 0f, 1f);
                                if (Utl.Equals(num, 0f, 0.01f))
                                {
                                    num = 0.1f;
                                }

                                __result = num;
                                //  TFTVLogger.Always($"the score is {num}");
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






                //Patch to prevent Scylla from targeting tiny critters like worms and spider drones
                [HarmonyPatch(typeof(TacticalAbility), "GetTargetActors", new Type[] { typeof(TacticalTargetData), typeof(TacticalActorBase), typeof(Vector3) })]
                public static class TFTV_TacticalAbility_GetTargetActors_Scylla_Patch
                {
                    public static void Postfix(ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor, TacticalAbility __instance)
                    {
                        try
                        {
                            if (sourceActor is TacticalActor tacticalActor && tacticalActor.IsControlledByAI)
                            {
                                //  TFTVLogger.Always($"{tacticalActor.DisplayName} is looking for targets, before culling has {__result.Count()} targets");

                                // TFTVLogger.Always($"{tacticalActor.DisplayName} is looking for targets");
                                __result = CullTargetsLists(__result, sourceActor, __instance);

                                //  TFTVLogger.Always($"{tacticalActor.DisplayName} after culling has {__result.Count()} targets");
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }

                //shouldn't be necessary because it invokes Weapon.GetShootTargets, patched below.
                /*  [HarmonyPatch(typeof(ShootAbility), "GetTargets")]
                  public static class TFTV_ShootAbility_GetTargets_Scylla_Patch
                  {
                      public static void Postfix(ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor, ShootAbility __instance)
                      {
                          try
                          {
                              if (sourceActor is TacticalActor tacticalActor && tacticalActor.IsControlledByAI)
                              {
                                  // if (tacticalActor.HasGameTag(DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef")))
                                  // {
                                  //   TFTVLogger.Always($"{tacticalActor.DisplayName} is looking for targets for ability {__instance.TacticalAbilityDef.name}, before culling has {__result.Count()} targets");
                                  __result = CullTargetsLists(__result, sourceActor, __instance);
                                  //   TFTVLogger.Always($"{tacticalActor.DisplayName} after culling has {__result.Count()} targets");
                                  // }
                              }
                          }
                          catch (Exception e)
                          {
                              TFTVLogger.Error(e);
                          }
                      }
                  }*/
                public static bool IsValidTarget(TacticalActor actor, TacticalAbilityTarget target, Weapon weapon)
                {
                    try
                    {
                        // bool isValid = true;


                        ClassTagDef swarmerTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                        ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                        ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                        ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                        ClassTagDef tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                        ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                        ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
                        ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                        GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");
                        GameTagDef caterpillarDamage = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");
                        ItemClassificationTagDef meleeTag = DefCache.GetDef<ItemClassificationTagDef>("MeleeWeapon_TagDef");

                        if (actor.GameTags.Contains(queenTag) || actor.GameTags.Contains(acheronTag) || actor.GameTags.Contains(chironTag) || actor.GameTags.Contains(cyclopsTag))
                        {
                            if (target.GetTargetActor() is TacticalActor targetActor && targetActor.GameTags.Contains(caterpillarDamage) && targetActor.TacticalFaction != actor.TacticalFaction)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (target.GetTargetActor() is TacticalActor targetActor && targetActor.GameTags.Contains(caterpillarDamage) && (targetActor.Pos - target.Actor.Pos).magnitude > 8)
                            {
                                return false;
                            }
                        }

                        DamageKeywordDef[] excludeDamageDefs =
                        {
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ParalysingKeyword,
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ViralKeyword
                        };


                        if (weapon.GetDamagePayload().DamageKeywords.Any(damageKeyordPair => excludeDamageDefs.Contains(damageKeyordPair.DamageKeywordDef)))
                        {
                            if (target.GetTargetActor() is TacticalActor targetActor && (targetActor.ActorDef.name.Equals("SpiderDrone_ActorDef") ||
                                    targetActor.ActorDef.name.Contains("Turret_ActorDef")))
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


                //IMPORTANT! METHOD FROM MADSKUNKY ON IEnumerable Harmony Patch!!!
                [HarmonyPatch(typeof(Weapon), "GetShootTargets")]
                public static class TFTV_Weapon_GetShootTargets_Patch
                {
                    public static IEnumerable<TacticalAbilityTarget> Postfix(IEnumerable<TacticalAbilityTarget> results, Weapon __instance)
                    {
                        foreach (TacticalAbilityTarget target in results)
                        {
                            if (IsValidTarget(__instance.TacticalActor, target, __instance)) // <- create a method to check the target
                            {
                                yield return target;
                            }

                        }
                    }
                }


                // AIActionMoveAndAttack

                //implement method to cull small target
                public static IEnumerable<TacticalAbilityTarget> CullTargetsLists(IEnumerable<TacticalAbilityTarget> targetList, TacticalActorBase actor, TacticalAbility ability)
                {
                    try
                    {
                        //   TFTVLogger.Always($"{actor.name} using {ability?.TacticalAbilityDef?.name} and target list has {targetList.Count()} elements");



                        List<TacticalAbilityTarget> culledList = new List<TacticalAbilityTarget>(targetList);

                        ClassTagDef swarmerTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                        ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                        ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                        ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                        ClassTagDef tritonTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                        ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                        ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
                        ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");


                        GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");
                        GameTagDef caterpillarDamage = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");
                        SkillTagDef meleeSkillTag = DefCache.GetDef<SkillTagDef>("MeleeAbility_SkillTagDef");


                        AttenuatingDamageTypeEffectDef paralysisDamage = DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Electroshock_AttenuatingDamageTypeEffectDef");
                        DamageOverTimeDamageTypeEffectDef virusDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef");
                        foreach (TacticalAbilityTarget target in targetList)
                        {
                            if (actor.GameTags.Contains(swarmerTag) || (ability.TacticalAbilityDef.SkillTags.Contains(meleeSkillTag) && actor.GameTags.Contains(crabTag)))
                            {

                            }
                            else if (actor.GameTags.Contains(queenTag) //&& actor.GetAbility<CaterpillarMoveAbility>() != null
                                || actor.GameTags.Contains(acheronTag) || actor.GameTags.Contains(chironTag) || actor.GameTags.Contains(cyclopsTag))
                            {
                                if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && tacticalActor.TacticalFaction != actor.TacticalFaction)
                                {
                                    // TFTVLogger.Always($"{tacticalActor.name} culled for {actor.name}");
                                    culledList.Remove(target);
                                }
                            }
                        }

                        return culledList;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }
        }



        /* [HarmonyPatch(typeof(AIAbilityConsiderationDef), "GetAbility")]
       internal static class AIAbilityConsiderationDef_GetAbility_patch
       {


           public static bool Prefix(AIAbilityConsiderationDef __instance, TacticalActorBase actor, TacAITarget target, ref TacticalAbility __result)
           {
               try
               {
                   if (__instance.UseWeaponDefaultShootAbility)
                   {
                       TFTVLogger.Always($"getting default shoot ability");
                       __result = (target.Equipment as Weapon).DefaultShootAbility;
                       return false;
                   }

                   if (__instance.AbilityTypeParameter)
                   {
                       Type abilityType = __instance.Ability.GetType();
                       if (target.Equipment == null)
                       {
                           TFTVLogger.Always($"target.Equipment == null");
                           __result = actor.GetAbilityFiltered<TacticalAbility>((Ability x) => x.AbilityDef.GetType() == abilityType);
                           return false;
                       }

                       AbilityDef abilityDef = target.Equipment.EquipmentDef.Abilities.FirstOrDefault((AbilityDef x) => x.GetType() == abilityType);
                       if (abilityDef != null)
                       {
                           TFTVLogger.Always($"abilityDef != null");
                           __result = actor.GetAbilityWithDef<TacticalAbility>(abilityDef);
                           return false;
                       }
                   }
                   TFTVLogger.Always($"trying to get it with GetGetAbilityWithDef");
                   __result = actor.GetAbilityWithDef<TacticalAbility>(__instance.Ability);
                   TFTVLogger.Always($"__result is null? {__result}");
                   return false;
               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
                   throw;
               }
           }
       }*/

        


        /*  [HarmonyPatch(typeof(TacticalAbility), "GetDisabledStateDefaults")]
      internal static class TacticalAbility_GetDisabledStateDefaults_patch
      {

          public static bool Prefix(TacticalAbility __instance, IgnoredAbilityDisabledStatesFilter filter, ref AbilityDisabledState __result)
          {
              try
              {
                  if(__instance.TacticalAbilityDef == DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef")) 
                  {

                      if (!__instance.ActorTagsSatisfied)
                      {
                          TFTVLogger.Always($"1");
                          __result = AbilityDisabledState.IncompatibleWithActor;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.ActorIsDisabled) && !__instance.UsableOnDisabledActor && __instance.TacticalActorBase.IsDisabled)
                      {
                          TFTVLogger.Always($"2");
                          __result = AbilityDisabledState.ActorIsDisabled;
                          return false;
                      }

                      Equipment equipment2 = __instance.OverrideEquipment;
                      if (equipment2 == null)
                      {
                          equipment2 = __instance.EquipmentSource;
                      }

                      if (equipment2 != null && !filter.IsStateIgnored(AbilityDisabledState.NotEnoughHands) && !equipment2.HasEnoughHandsToUse())
                      {
                          __result = AbilityDisabledState.NotEnoughHands;
                      }

                      if (equipment2 == null && __instance.HasEquipmentTagsRequirement)
                      {
                          equipment2 = __instance.EquipmentWithTags;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.EquipmentIsDisabled) && equipment2 != null && !equipment2.IsUsable)
                      {
                          TFTVLogger.Always($"3");
                          __result = AbilityDisabledState.EquipmentIsDisabled;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.EquipmentNotActive) && equipment2 != null && !equipment2.IsActive)
                      {
                          TFTVLogger.Always($"4");
                          __result = AbilityDisabledState.EquipmentNotActive;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.EquipmentNotSelected) && equipment2 != null && !isEquipmentOfSelectedGroup(equipment2) && !__instance.UsableOnNonSelectedEquipment)
                      {TFTVLogger.Always($"5");
                          __result = AbilityDisabledState.EquipmentNotSelected;
                          return false;
                      }

                      int num = Math.Max(1, __instance.GetRequiredCharges());
                      if (!filter.IsStateIgnored(AbilityDisabledState.NoMoreCharges) && equipment2 != null && !equipment2.InfiniteCharges && equipment2.CommonItemData.CurrentCharges < num && !__instance.UsableOnEquipmentWithInsufficientCharges)
                      {TFTVLogger.Always($"6");
                          __result = AbilityDisabledState.NoMoreCharges;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.OtherAbilityNeeded) && __instance.TacticalAbilityDef.AbilitiesRequired.Length != 0 && __instance.TacticalAbilityDef.AbilitiesRequired.Any((TacticalAbilityDef def) => __instance.TacticalActorBase.GetAbilityWithDef<TacticalAbility>(def) == null))
                      {
                          TFTVLogger.Always($"6");
                          __result = AbilityDisabledState.OtherAbilityNeeded;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.NoMoreUsesThisTurn) && !__instance.CanUseThisTurn)
                      {
                          TFTVLogger.Always($"7");
                          __result = AbilityDisabledState.NoMoreUsesThisTurn;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.NotEnoughActionPoints) && !__instance.ActionPointRequirementSatisfied)
                      {
                          TFTVLogger.Always($"8");
                          __result = AbilityDisabledState.NotEnoughActionPoints;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.NotEnoughWillPoints) && !__instance.WillPointRequirementSatisfied)
                      {
                          TFTVLogger.Always($"9");
                          __result = AbilityDisabledState.NotEnoughWillPoints;
                          return false;
                      }

                      if (__instance.IsTacticalActor && !filter.IsStateIgnored(AbilityDisabledState.RequirementsNotMet) && !__instance.TraitsTagsRequirementSatisfied)
                      {
                          TFTVLogger.Always($"10");
                          __result = AbilityDisabledState.RequirementsNotMet;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.NoValidTarget) && !__instance.HasValidTargets)
                      {
                          TFTVLogger.Always($"11");
                          __result = AbilityDisabledState.NoValidTarget;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.OffMap) && __instance.TacticalActorBase.IsOffMap)
                      {
                          TFTVLogger.Always($"12");
                          __result = AbilityDisabledState.OffMap;
                          return false;
                      }

                      if (!filter.IsStateIgnored(AbilityDisabledState.ActorStunned) && __instance.UsesWillPoints && (__instance.TacticalActorBase.IsStunned || __instance.TacticalActorBase.IsSilenced))
                      {
                          TFTVLogger.Always($"13");
                          __result = AbilityDisabledState.ActorStunned;
                          return false;
                      }

                      if (__instance.TacticalActorBase.Status.GetStatuses<Status>().Any((Status x) => __instance.DisabledByStatuses.Contains(x.Def)) && !filter.IsStateIgnored(AbilityDisabledState.BlockedByStatus))
                      {
                          TFTVLogger.Always($"14");
                          __result = AbilityDisabledState.BlockedByStatus;
                          return false;
                      }

                      __result = AbilityDisabledState.NotDisabled;

                      bool isEquipmentOfSelectedGroup(Equipment equipment)
                      {
                          if (equipment.EquipmentComponent == null)
                          {
                              return false;
                          }

                          Equipment selectedEquipment = equipment.EquipmentComponent.SelectedEquipment;
                          if (selectedEquipment == null)
                          {
                              return false;
                          }

                          IEnumerable<Equipment> source = Global.Enumerate<Equipment>(selectedEquipment);
                          EquipmentGroupTagDef equipmentGroupTag = selectedEquipment.GameTags.OfType<EquipmentGroupTagDef>().FirstOrDefault();
                          if (equipmentGroupTag != null)
                          {
                              source = equipment.EquipmentComponent.Equipments.Where((Equipment x) => x.GameTags.Contains(equipmentGroupTag));
                          }

                          return source.Contains(equipment);
                      }

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
      }*/


        /* */
        //AIActionMoveAndExecuteAbilityDef
        //  MoveAndQuickAim_AIActionDef

        /*
        [HarmonyPatch(typeof(AIActorMovementZoneTargetGenerator), "FillTargets")]
            internal static class AIActorMovementZoneTargetGenerator_FillTargets_patch
        {

            public static void Prefix(
                AIActorMovementZoneTargetGenerator __instance, AIFaction faction, IAIActor actor, object context, AITargetGenerator prevGen, IList<AIScoredTarget> prevGenTargets, IList<AIScoredTarget> targets, IEnumerator<NextUpdate> __result, IgnoredAbilityDisabledStatesFilter ____ignoredDisabledStatesFilter)
            {
                try
                {
                    TacticalActor tacticalActor = (TacticalActor)actor;
                    TFTVLogger.Always($"{tacticalActor.name} {__instance.Def.name} targets {targets.Count}");

                    TacticalActor tacActor = (TacticalActor)actor;
                    IMoveAbility moveAbility;
                    if (__instance.Def.MoveAbilityDef != null)
                    {
                        TacticalAbility abilityWithDef = tacActor.GetAbilityWithDef<TacticalAbility>(__instance.Def.MoveAbilityDef);
                        if (abilityWithDef == null)
                        {
                            throw new InvalidOperationException(tacActor.name + " doesn't have " + __instance.Def.MoveAbilityDef.name);
                        }

                        moveAbility = abilityWithDef as IMoveAbility;
                        if (moveAbility == null)
                        {
                            throw new InvalidOperationException(__instance.Def.MoveAbilityDef.name + " is not an IMoveAbility");
                        }
                    }
                    else
                    {
                        moveAbility = tacActor.GetAbility<MoveAbility>();
                    }

                    if (!moveAbility.IsEnabled(____ignoredDisabledStatesFilter))
                    {
                        yield break;
                    }

                    int runsCount = ((prevGen == null) ? 1 : prevGenTargets.Count);
                    IEnumerable<MoveAbilityTargetData> targetsData = Enumerable.Empty<MoveAbilityTargetData>();
                    if (prevGen != null)
                    {
                        float num = tacActor.MaxActionPoints;
                        bool flag = false;
                        for (int j = 0; j < runsCount; j++)
                        {
                            AIScoredTarget aIScoredTarget = prevGenTargets[j];
                            Weapon weapon;
                            if (aIScoredTarget != null && aIScoredTarget.Target != null && (weapon = (aIScoredTarget.Target as TacAITarget)?.Equipment as Weapon) != null)
                            {
                                flag = true;
                                float predictionActionPointCost = weapon.DefaultShootAbility.PredictionActionPointCost;
                                if (predictionActionPointCost < num)
                                {
                                    num = predictionActionPointCost;
                                }
                            }
                        }

                        if (!flag)
                        {
                            targetsData = moveAbility.GetTargetsData();
                        }
                        else
                        {
                            float num2 = tacActor.MaxMoveRange - num;
                            if (!(num2 >= 0f))
                            {
                                yield break;
                            }

                            float num3 = num2 - ((TacticalAbility)moveAbility).PredictionActionPointCost;
                            if (num3 >= 0f)
                            {
                                TacticalPathRequest aiPathRequest = tacActor.TacticalLevel.GetAiPathRequest(tacActor, num3);
                                targetsData = moveAbility.GetTargetsData(aiPathRequest);
                            }
                        }
                    }
                    else
                    {
                        targetsData = moveAbility.GetTargetsData();
                    }

                    float normalizedZoneRange = moveAbility.GetNormalizedZoneRange(__instance.Def.ZoneRange);
                    float maxMoveAndActRange = ((!__instance.Def.PositionsWithCoverOnly || !Utl.LesserThan(normalizedZoneRange, 1f, 0.01f)) ? (moveAbility.MaxActionPointCost * normalizedZoneRange) : (moveAbility.MaxActionPointCost * GetMinNormalizedZoneRange(tacActor)));
                    int i = 0;
                    while (i < runsCount)
                    {
                        AIScoredTarget prevScoredTarget = ((prevGen != null) ? prevGenTargets[i] : null);
                        Weapon weapon2;
                        if (prevScoredTarget != null && prevScoredTarget.Target != null && (weapon2 = (prevScoredTarget.Target as TacAITarget).Equipment as Weapon) != null)
                        {
                            maxMoveAndActRange = tacActor.GetMaxMoveAndActRange(weapon2.DefaultShootAbility, moveAbility, IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected);
                        }

                        if (__instance.Def.DiscardEverySecondPosition)
                        {
                            targetsData = from t in targetsData
                                          orderby t.Position.x, t.Position.z
                                          select t;
                        }

                        float moveAbilityCost = ((TacticalAbility)moveAbility).PredictionActionPointCost;
                        bool hasMinDistance = __instance.Def.MinValidMovementDistance > 0f;
                        int count = -1;
                        foreach (MoveAbilityTargetData target in targetsData)
                        {
                            count++;
                            if (__instance.Def.DiscardEverySecondPosition && count % 2 != 0)
                            {
                                continue;
                            }

                            if (faction.TimeOff)
                            {
                                yield return faction.TimeYield();
                                faction.TimeRestart();
                            }

                            float num4 = target.PathLength;
                            if (moveAbilityCost > 0f)
                            {
                                num4 = moveAbilityCost;
                            }

                            if (!Utl.GreaterThan(num4, maxMoveAndActRange, 0.01f) && (!hasMinDistance || !(__instance.Def.UsePathDistance ? Utl.GreaterThan(__instance.Def.MinValidMovementDistance, num4) : Utl.GreaterThan(Def.MinValidMovementDistance, (target.Position - tacActor.Pos).magnitude))) && (!__instance.Def.PositionsWithCoverOnly || __instance.HasCoversAround(tacActor, target.Position)))
                            {
                                TacAITarget tacAITarget = new TacAITarget
                                {
                                    Pos = target.Position,
                                    PathLength = target.PathLength,
                                    MoveAbility = moveAbility
                                };
                                AIScoredTarget item;
                                if (prevScoredTarget != null)
                                {
                                    tacAITarget.MergeWith(prevScoredTarget.Target);
                                    item = new AIScoredTarget
                                    {
                                        Target = tacAITarget,
                                        Score = prevScoredTarget.Score
                                    };
                                }
                                else
                                {
                                    item = new AIScoredTarget
                                    {
                                        Target = tacAITarget,
                                        Score = 1f
                                    };
                                }

                                targets.Add(item);
                            }
                        }

                        if (__instance.Def.IncludeSelfActorPosition && (!__instance.Def.PositionsWithCoverOnly || HasCoversAround(tacActor, tacActor.Pos)))
                        {
                            TacAITarget tacAITarget2 = new TacAITarget
                            {
                                Pos = tacActor.Pos,
                                PathLength = 0f,
                                MoveAbility = moveAbility
                            };
                            AIScoredTarget item2;
                            if (prevScoredTarget != null)
                            {
                                tacAITarget2.MergeWith(prevScoredTarget.Target);
                                item2 = new AIScoredTarget
                                {
                                    Target = tacAITarget2,
                                    Score = prevScoredTarget.Score
                                };
                            }
                            else
                            {
                                item2 = new AIScoredTarget
                                {
                                    Target = tacAITarget2,
                                    Score = 1f
                                };
                            }

                            targets.Add(item2);
                        }

                        int num5 = i + 1;
                        i = num5;
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

        /*  [HarmonyPatch(typeof(Weapon), "GetShootTargets")]
          public static class TFTV_Weapon_GetShootTargets_Patch
          {
              public static IEnumerable<TacticalAbilityTarget> Postfix(IEnumerable<TacticalAbilityTarget> results, Weapon __instance)
              {
                  foreach (TacticalAbilityTarget target in results)
                  {
                      if (IsValidTarget(__instance.TacticalActor, target, __instance)) // <- create a method to check the target
                      {
                          yield return target;
                      }

                  }
              }
          }*/


        /*  [HarmonyPatch(typeof(AIActorEquipmentTargetGenerator), "FillTargets")]
           internal static class AIActorEquipmentTargetGenerator_FillTargets_patch
          {

              public static void Postfix (AIActorEquipmentTargetGenerator __instance, 
                  AIFaction faction, IAIActor actor, object context, AITargetGenerator prevGen, 
                  IList<AIScoredTarget> prevGenTargets, IList<AIScoredTarget> targets)
              {
                  try
                  {
                      AIActorEquipmentTargetGeneratorDef Def = (AIActorEquipmentTargetGeneratorDef)__instance.BaseDef;

                      if (!Def.name.Equals("Gun_AITargetGeneratorDef")) 
                      {
                          return;         
                      }

                      TFTVLogger.Always($"{Def.name}");

                      TacticalActor tacActor = (TacticalActor)actor;
                      int num = ((prevGen == null) ? 1 : prevGenTargets.Count);
                      for (int i = 0; i < num; i++)
                      {
                          AIScoredTarget aIScoredTarget = ((prevGen != null) ? prevGenTargets[i] : null);
                          foreach (Equipment equipment in tacActor.Equipments.Equipments)
                          {
                              TFTVLogger.Always($"Looking at {equipment?.DisplayName}");

                              if ((Def.EquipmentTags.Length == 0 || equipment.GameTags.Any((GameTagDef tag) => Def.EquipmentTags.Contains(tag))) && isEquipmentUsable(equipment))
                              {
                                  TFTVLogger.Always($"Got passed the if check for {equipment?.DisplayName}");

                                  TacAITarget tacAITarget = new TacAITarget
                                  {
                                      Equipment = equipment
                                  };
                                  AIScoredTarget item;
                                  if (aIScoredTarget != null)
                                  {
                                      tacAITarget.MergeWith(aIScoredTarget.Target);
                                      item = new AIScoredTarget
                                      {
                                          Target = tacAITarget,
                                          Score = aIScoredTarget.Score
                                      };
                                  }
                                  else
                                  {
                                      item = new AIScoredTarget
                                      {
                                          Target = tacAITarget,
                                          Score = 1f
                                      };
                                  }

                                  TFTVLogger.Always($"item.Actor==null? {item.Actor==null}");

                                  targets.Add(item);
                                  TFTVLogger.Always($"tagets count {targets.Count()}");

                              }
                          }
                      }

                      bool isEquipmentUsable(Equipment equipment)
                      {
                          if (!equipment.IsUsable)
                          {
                              return false;
                          }

                          if (!equipment.IsBodyPart)
                          {
                              return true;
                          }

                          ItemSlot parentSlot = equipment.ParentItemSlot;
                          if (parentSlot == null)
                          {
                              return true;
                          }

                          return tacActor.Status.GetStatuses<SlotStateStatus>().FirstOrDefault((SlotStateStatus x) => x.GetTargetSlotsNames().Contains(parentSlot.GetSlotName()))?.SlotStateStatusDef.BodypartsEnabled ?? true;
                      }

                    //  yield break;

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/


        /*  [HarmonyPatch(typeof(AIAttackPositionConsideration), "EvaluateWithShootAbility")]
           internal static class AIAttackPositionConsideration_EvaluateWithShootAbility_patch
          {

              public static bool Prefix(AIAttackPositionConsideration __instance, ref float __result, IAIActor actor, IAITarget target)
              {
                  try

                  {

                     // TFTVLogger.Always($"{__instance.Def.name}");
                      if (!__instance.Def.name.Equals("QuickAimAttackPosition_AIConsiderationDef"))
                      {
                          return true;
                      }
                      MethodInfo methodInfo = typeof(AIAttackPositionConsideration).GetMethod("GetPayloadMaxDamage", BindingFlags.Instance | BindingFlags.NonPublic);

                      TacticalActor tacActor = (TacticalActor)actor;
                          TacAITarget tacAITarget = (TacAITarget)target;
                          Weapon weapon = (Weapon)tacAITarget.Equipment;
                          ShootAbility shootAbility = weapon?.DefaultShootAbility;
                          if (weapon == null || shootAbility == null)
                          {
                              Debug.LogError($"{__instance.Def.name} has invalid target weapon {weapon} for {tacActor}", tacActor);
                          __result = 0f;
                          TFTVLogger.Always($"1");
                          return false;

                          }

                          IgnoredAbilityDisabledStatesFilter ignoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints = IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints;
                          if (!shootAbility.IsEnabled(ignoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints))
                          {
                          __result = 0f;
                          TFTVLogger.Always($"2");
                          return false;
                      }

                          if (__instance.Def.IsOverwatch)
                          {
                              OverwatchAbility ability = tacActor.GetAbility<OverwatchAbility>(weapon);
                              if (ability == null || !ability.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreEquipmentNotSelected))
                              {
                              __result = 0f;
                              TFTVLogger.Always($"3");
                              return false;
                          }

                              tacAITarget.AngleInRadians = __instance.Def.OverwatchFOV / 2f * ((float)Math.PI / 180f);
                          }

                          TacticalAbilityTarget tacticalAbilityTarget = new TacticalAbilityTarget();
                          List<TacticalActorBase> list = new List<TacticalActorBase>(5);
                          float num = 1f;
                          if (tacAITarget.Actor == null)
                          {
                          TFTVLogger.Always($"4");
                          __result = 0.0f;
                          return false;
                      }

                          tacticalAbilityTarget.Actor = tacAITarget.Actor;
                          tacticalAbilityTarget.ActorGridPosition = tacAITarget.Actor.Pos;
                          int num2 = 1;
                          if (tacAITarget.Actor is TacticalActor)
                          {
                              num2 = ((TacticalActor)tacAITarget.Actor).BodyState.GetHealthSlots().Count();
                          }

                          list.Clear();
                          List<KeyValuePair<TacticalAbilityTarget, float>> shootTargetsWithScores = tacActor.TacticalFaction.AIBlackboard.GetShootTargetsWithScores(weapon, tacticalAbilityTarget, tacAITarget.Pos);
                          TacticalAbilityTarget key = shootTargetsWithScores.FirstOrDefault().Key;
                          if (key != null)
                          {
                              list.AddRange(AIUtil.GetAffectedTargetsByShooting(key.ShootFromPos, tacActor, weapon, key));
                              if (list.Count == 0)
                              {
                              TFTVLogger.Always($"5");
                              __result = 0f;
                              return false;
                          }

                              int num3 = 0;
                              foreach (TacticalActorBase item in list)
                              {
                                  if (tacActor.RelationTo(item) == FactionRelation.Friend)
                                  {
                                      num *= __instance.Def.FriendlyHitScoreMultiplier;
                                  }
                                  else if (tacActor.RelationTo(item) == FactionRelation.Neutral)
                                  {
                                      num *= __instance.Def.NeutralHitScoreMultiplier;
                                  }
                                  else if (tacActor.RelationTo(item) == FactionRelation.Enemy)
                                  {
                                      num3++;
                                  }
                              }

                              if (num < Mathf.Epsilon || num3 == 0)
                              {
                              TFTVLogger.Always($"6");
                              __result = 0f;
                              return false;
                          }

                          float temp = (float)methodInfo.Invoke
                          (__instance, new object[] { tacAITarget.Pos, weapon.GetDamagePayload(), list.Where((TacticalActorBase ac) => tacActor.RelationTo(ac) == FactionRelation.Enemy), weapon });

                              float num4 = temp.ClampHigh(__instance.Def.MaxDamage);
                              num *= num4 / __instance.Def.MaxDamage;
                              if (num < Mathf.Epsilon)
                              {
                              TFTVLogger.Always($"7");
                              __result = 0f;
                              return false;
                          }

                              DamageDeliveryType damageDeliveryType = weapon.GetDamagePayload().DamageDeliveryType;
                              if (damageDeliveryType != DamageDeliveryType.Parabola && damageDeliveryType != DamageDeliveryType.Sphere && AIUtil.CheckActorType(tacAITarget.Actor, ActorType.Civilian | ActorType.Combatant))
                              {
                                  Vector3 vector = key.ShootFromPos - tacAITarget.Actor.Pos;
                                  if ((!vector.x.IsZero() || !vector.z.IsZero()) && !__instance.Def.IsOverwatch)
                                  {
                                      num *= Mathf.Clamp((float)shootTargetsWithScores.Count / (float)num2, 0f, 1f);
                                  }
                              }

                              num *= AIUtil.GetEnemyWeight(tacActor.TacticalFaction.AIBlackboard, tacAITarget.Actor);
                              __result = Mathf.Clamp(num, 0f, 1f);
                          TFTVLogger.Always($"8");
                          return false;
                      }
                      TFTVLogger.Always($"9");
                      __result = 0f;
                      return false;

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /*   */



        // AIActorMovementZoneTargetGeneratorDef
        //AIEnoughActionPointsForAbilityConsiderationDef


        /*  [HarmonyPatch(typeof(AIEnoughActionPointsForAbilityConsideration), "Evaluate")]
          public static class AIEnoughActionPointsForAbilityConsideration_GetMovementDataInRange_patch
          {
              private static void Postfix(AIEnoughActionPointsForAbilityConsideration __instance, float __result, IAIActor actor)
              {
                  try
                  {
                      if (__instance.BaseDef.name.Equals("Queen_CanUsePrepareShoot_AIConsiderationDef"))
                      {


                          TacticalActor tacActor = actor as TacticalActor;


                          TFTVLogger.Always($"{tacActor.name} {__instance.BaseDef.name} running Evaluate, result is {__result}");

                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }

          }*/




        //  AILineOfSightToEnemiesConsiderationDef



        /*  ("DashMovementZoneNoSelfPosition_AITargetGeneratorDef");
              AIActorMovementZoneTargetGeneratorDef

              [HarmonyPatch(typeof(AIActorMovementZoneTargetGenerator), "FillTargets")]
              internal static class AIActorMovementZoneTargetGenerator_FillTargets_patch
          {

              public static void Prefix(
                  AIActorMovementZoneTargetGenerator __instance, AIFaction faction, IAIActor actor, object context, AITargetGenerator prevGen, IList<AIScoredTarget> prevGenTargets, IList<AIScoredTarget> targets, IEnumerator<NextUpdate> __result, IgnoredAbilityDisabledStatesFilter ____ignoredDisabledStatesFilter)
              {
                  try
                  {
                      TacticalActor tacticalActor = (TacticalActor)actor;
                      TFTVLogger.Always($"{tacticalActor.name} {__instance.Def.name} targets {targets.Count}");

                      TacticalActor tacActor = (TacticalActor)actor;
                      IMoveAbility moveAbility;
                      if (__instance.Def.MoveAbilityDef != null)
                      {
                          TacticalAbility abilityWithDef = tacActor.GetAbilityWithDef<TacticalAbility>(__instance.Def.MoveAbilityDef);
                          if (abilityWithDef == null)
                          {
                              throw new InvalidOperationException(tacActor.name + " doesn't have " + __instance.Def.MoveAbilityDef.name);
                          }

                          moveAbility = abilityWithDef as IMoveAbility;
                          if (moveAbility == null)
                          {
                              throw new InvalidOperationException(__instance.Def.MoveAbilityDef.name + " is not an IMoveAbility");
                          }
                      }
                      else
                      {
                          moveAbility = tacActor.GetAbility<MoveAbility>();
                      }

                      if (!moveAbility.IsEnabled(____ignoredDisabledStatesFilter))
                      {
                          yield break;
                      }

                      int runsCount = ((prevGen == null) ? 1 : prevGenTargets.Count);
                      IEnumerable<MoveAbilityTargetData> targetsData = Enumerable.Empty<MoveAbilityTargetData>();
                      if (prevGen != null)
                      {
                          float num = tacActor.MaxActionPoints;
                          bool flag = false;
                          for (int j = 0; j < runsCount; j++)
                          {
                              AIScoredTarget aIScoredTarget = prevGenTargets[j];
                              Weapon weapon;
                              if (aIScoredTarget != null && aIScoredTarget.Target != null && (weapon = (aIScoredTarget.Target as TacAITarget)?.Equipment as Weapon) != null)
                              {
                                  flag = true;
                                  float predictionActionPointCost = weapon.DefaultShootAbility.PredictionActionPointCost;
                                  if (predictionActionPointCost < num)
                                  {
                                      num = predictionActionPointCost;
                                  }
                              }
                          }

                          if (!flag)
                          {
                              targetsData = moveAbility.GetTargetsData();
                          }
                          else
                          {
                              float num2 = tacActor.MaxMoveRange - num;
                              if (!(num2 >= 0f))
                              {
                                  yield break;
                              }

                              float num3 = num2 - ((TacticalAbility)moveAbility).PredictionActionPointCost;
                              if (num3 >= 0f)
                              {
                                  TacticalPathRequest aiPathRequest = tacActor.TacticalLevel.GetAiPathRequest(tacActor, num3);
                                  targetsData = moveAbility.GetTargetsData(aiPathRequest);
                              }
                          }
                      }
                      else
                      {
                          targetsData = moveAbility.GetTargetsData();
                      }

                      float normalizedZoneRange = moveAbility.GetNormalizedZoneRange(__instance.Def.ZoneRange);
                      float maxMoveAndActRange = ((!__instance.Def.PositionsWithCoverOnly || !Utl.LesserThan(normalizedZoneRange, 1f, 0.01f)) ? (moveAbility.MaxActionPointCost * normalizedZoneRange) : (moveAbility.MaxActionPointCost * GetMinNormalizedZoneRange(tacActor)));
                      int i = 0;
                      while (i < runsCount)
                      {
                          AIScoredTarget prevScoredTarget = ((prevGen != null) ? prevGenTargets[i] : null);
                          Weapon weapon2;
                          if (prevScoredTarget != null && prevScoredTarget.Target != null && (weapon2 = (prevScoredTarget.Target as TacAITarget).Equipment as Weapon) != null)
                          {
                              maxMoveAndActRange = tacActor.GetMaxMoveAndActRange(weapon2.DefaultShootAbility, moveAbility, IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected);
                          }

                          if (__instance.Def.DiscardEverySecondPosition)
                          {
                              targetsData = from t in targetsData
                                            orderby t.Position.x, t.Position.z
                                            select t;
                          }

                          float moveAbilityCost = ((TacticalAbility)moveAbility).PredictionActionPointCost;
                          bool hasMinDistance = __instance.Def.MinValidMovementDistance > 0f;
                          int count = -1;
                          foreach (MoveAbilityTargetData target in targetsData)
                          {
                              count++;
                              if (__instance.Def.DiscardEverySecondPosition && count % 2 != 0)
                              {
                                  continue;
                              }

                              if (faction.TimeOff)
                              {
                                  yield return faction.TimeYield();
                                  faction.TimeRestart();
                              }

                              float num4 = target.PathLength;
                              if (moveAbilityCost > 0f)
                              {
                                  num4 = moveAbilityCost;
                              }

                              if (!Utl.GreaterThan(num4, maxMoveAndActRange, 0.01f) && (!hasMinDistance || !(__instance.Def.UsePathDistance ? Utl.GreaterThan(__instance.Def.MinValidMovementDistance, num4) : Utl.GreaterThan(Def.MinValidMovementDistance, (target.Position - tacActor.Pos).magnitude))) && (!__instance.Def.PositionsWithCoverOnly || __instance.HasCoversAround(tacActor, target.Position)))
                              {
                                  TacAITarget tacAITarget = new TacAITarget
                                  {
                                      Pos = target.Position,
                                      PathLength = target.PathLength,
                                      MoveAbility = moveAbility
                                  };
                                  AIScoredTarget item;
                                  if (prevScoredTarget != null)
                                  {
                                      tacAITarget.MergeWith(prevScoredTarget.Target);
                                      item = new AIScoredTarget
                                      {
                                          Target = tacAITarget,
                                          Score = prevScoredTarget.Score
                                      };
                                  }
                                  else
                                  {
                                      item = new AIScoredTarget
                                      {
                                          Target = tacAITarget,
                                          Score = 1f
                                      };
                                  }

                                  targets.Add(item);
                              }
                          }

                          if (__instance.Def.IncludeSelfActorPosition && (!__instance.Def.PositionsWithCoverOnly || HasCoversAround(tacActor, tacActor.Pos)))
                          {
                              TacAITarget tacAITarget2 = new TacAITarget
                              {
                                  Pos = tacActor.Pos,
                                  PathLength = 0f,
                                  MoveAbility = moveAbility
                              };
                              AIScoredTarget item2;
                              if (prevScoredTarget != null)
                              {
                                  tacAITarget2.MergeWith(prevScoredTarget.Target);
                                  item2 = new AIScoredTarget
                                  {
                                      Target = tacAITarget2,
                                      Score = prevScoredTarget.Score
                                  };
                              }
                              else
                              {
                                  item2 = new AIScoredTarget
                                  {
                                      Target = tacAITarget2,
                                      Score = 1f
                                  };
                              }

                              targets.Add(item2);
                          }

                          int num5 = i + 1;
                          i = num5;
                      }



                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /* [HarmonyPatch(typeof(AIAbilityDisabledStateConsideration), "Evaluate")]
         internal static class AIAbilityDisabledStateConsideration_Evaluate_patch
         {

             public static bool Prefix(AIAbilityDisabledStateConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {
                     TacticalActorBase actor2 = (TacticalActorBase)actor;
                     TacAITarget target2 = target as TacAITarget;
                     TacticalAbility ability = __instance.Def.GetAbility(actor2, target2);


                     TFTVLogger.Always($"{__instance.Def.name}");

                     if (__instance.Def.name.Equals("QuickAimAbilityEnabled_AIConsiderationDef"))
                     {
                         if (ability == null)
                         {
                             TFTVLogger.Always($"ability is null!");
                             __result = 0f;
                             return false;
                         }

                         IgnoredAbilityDisabledStatesFilter filter = new IgnoredAbilityDisabledStatesFilter(__instance.Def.IgnoredStates.Select((string x) => new AbilityDisabledState(x)).ToArray());
                         //  TFTVLogger.Always($"filter is {filter.}");

                         if (!(ability.GetDisabledState(filter).Key == __instance.Def.AbilityDisabledState))
                         {
                             TFTVLogger.Always($"{ability.AbilityDef.name} is disabled!");
                             __result = 0f;
                             return false;
                         }
                         TFTVLogger.Always($"ability enabled!");
                         __result = 1f;

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
         }*/


        /*  [HarmonyPatch(typeof(AIVisibleEnemiesConsideration), "Evaluate")]
          public static class AIVisibleEnemiesConsideration_GetMovementDataInRange_patch
          {
              private static void Postfix(AIVisibleEnemiesConsideration __instance, float __result, IAIActor actor)
              {
                  try
                  {
                      TFTVLogger.Always($"{__instance.Def.name} {__result}");

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }

          }*/




        /*  [HarmonyPatch(typeof(AICanUseEquipmentConsideration), "Evaluate")]
          public static class AICanUseEquipmentConsiderationn_GetMovementDataInRange_patch
          {
              private static void Postfix(AICanUseEquipmentConsideration __instance, float __result, IAIActor actor)
              {
                  try
                  {
                      TFTVLogger.Always($"{__instance.Def.name} {__result}");

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }
              }

          }*/


        /*   [HarmonyPatch(typeof(AIAbilityDisabledStateConsideration), "Evaluate")]
           internal static class AIAbilityDisabledStateConsideration_Evaluate_patch
           {

               public static void Postfix(AIAbilityDisabledStateConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
               {
                   try
                   {

                       TFTVLogger.Always($"{__instance.Def.name} {__result}");

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/


        /*   [HarmonyPatch(typeof(AIActorRangeZoneTargetGenerator), "GetMovementDataInRange")]
           public static class AIActorRangeZoneTargetGenerator_GetMovementDataInRange_patch
           {
               private static void Prefix(AIActorRangeZoneTargetGenerator __instance, IEnumerable<TacAITarget> __result, IList<AIScoredTarget> prevGenTargets)
               {
                   try
                   {
                       if (__instance.BaseDef == DefCache.GetDef<AIActorRangeZoneTargetGeneratorDef>("Dash_StrikeAbilityZone_AITargetGeneratorDef"))
                       {
                           TFTVLogger.Always($"Prefix GetMovementDataInRange for Dash_StrikeAbilityZone_AITargetGeneratorDef IList<AIScoredTarget> prevGenTargets count: {prevGenTargets.Count()}");
                       }
                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }
               }

               private static void Postfix(AIActorRangeZoneTargetGenerator __instance, IEnumerable<TacAITarget> __result)
               {
                   try
                   {
                       TFTVLogger.Always($"{__instance.Def.name} running GetMovementDataInRange");

                       if (__instance.BaseDef == DefCache.GetDef<AIActorRangeZoneTargetGeneratorDef>("Dash_StrikeAbilityZone_AITargetGeneratorDef"))
                       {
                           TFTVLogger.Always($"GetMovementDataInRange for Dash_StrikeAbilityZone_AITargetGeneratorDef Result count: {__result.Count()}");
                       }
                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }
               }

           }*/

        // ("MinTravelDistance_AIConsiderationDef"); AITravelDistanceConsiderationDef
        // ("Spread_AIConsiderationDef") AISpreadConsiderationDef
        //("Worm_ClosestPathToEnemy_AIConsiderationDef") AIClosestEnemyConsiderationDef
        //NoLineofSight_AIConsiderationDef  AILineOfSightToEnemiesConsiderationDef
        //AIWillpointsLeftAfterAbilityConsiderationDef

        /* [HarmonyPatch(typeof(AIWillpointsLeftAfterAbilityConsideration), "Evaluate")]
         public static class AIWillpointsLeftAfterAbilityConsideration_Evaluate_patch
         {
             private static void Postfix(AIWillpointsLeftAfterAbilityConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {

                     TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }


         [HarmonyPatch(typeof(AILineOfSightToEnemiesConsideration), "Evaluate")]
         public static class AILineOfSightToEnemiesConsideration_Evaluate_patch
         {
             private static void Postfix(AILineOfSightToEnemiesConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {

                     TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }

         [HarmonyPatch(typeof(AITravelDistanceConsideration), "Evaluate")]
         public static class AITravelDistanceConsideration_Evaluate_patch
         {
             private static void Postfix(AITravelDistanceConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {

                     TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }





         [HarmonyPatch(typeof(AISpreadConsideration), "Evaluate")]
         public static class AISpreadConsideration_Evaluate_patch
         {
             private static void Postfix(AISpreadConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {

                     TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }



         [HarmonyPatch(typeof(AIClosestEnemyConsideration), "Evaluate")]
         public static class AIClosestEnemyConsideration_Evaluate_patch
         {
             private static void Postfix(AIClosestEnemyConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
             {
                 try
                 {

                     TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }

         }
        */


        /*
     [HarmonyPatch(typeof(AIStrategicPositionConsideration), "Evaluate")]
     public static class AIStrategicPositionConsideration_Evaluate_patch
     {
         private static void Postfix(AIStrategicPositionConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
         {
             try
             {
                 AIStrategicPositionConsiderationDef def = DefCache.GetDef<AIStrategicPositionConsiderationDef>("StrategicPositionOff_AIConsiderationDef");
                 TFTVLogger.Always($"{__instance.BaseDef.name} {__result}");

             }
             catch (Exception e)
             {
                 TFTVLogger.Error(e);
                 throw;
             }
         }

     }








     [HarmonyPatch(typeof(AIActorRangeZoneTargetGenerator), "FillTargets")]
     public static class AIActorRangeZoneTargetGenerator_FillTargets_patch
     {
         private static void Postfix(AIActorRangeZoneTargetGenerator __instance, IList<AIScoredTarget> targets)
         {
             try
             {
                 TFTVLogger.Always($"{__instance.Def.name} running FillTargets");

                 if (__instance.BaseDef == DefCache.GetDef<AIActorRangeZoneTargetGeneratorDef>("Dash_StrikeAbilityZone_AITargetGeneratorDef"))
                 {
                     TFTVLogger.Always($"filltargets Dash_StrikeAbilityZone_AITargetGeneratorDef targets count {targets.Count} ");
                     foreach (AIScoredTarget aIScoredTarget in targets)
                     {
                         TFTVLogger.Always($"{aIScoredTarget?.Actor} {aIScoredTarget?.Target} {aIScoredTarget.Score}");
                     }

                 }
             }
             catch (Exception e)
             {
                 TFTVLogger.Error(e);
             }
         }

     }*/





    }
}
