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
using static UnityEngine.GraphicsBuffer;
using static UnityStandardAssets.Utility.TimedObjectActivator;

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

                if (coverInfoInDirection.CoverType >= CoverType.Low)
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

        [HarmonyPatch(typeof(AIActionMoveToPosition), "Execute")]
        public static class AIActionMoveToPosition_Execute_patch
        {
            private static void Prefix(AIActionMoveToPosition __instance, ref AIScoredTarget aiTarget)
            {
                try
                {
                    TacticalActor actor = (TacticalActor)aiTarget.Actor;
                    TacAITarget target = (TacAITarget)aiTarget.Target;

                    if (actor.NavigationComponent.AgentNavSettings.AgentRadius >= TacticalMap.HalfTileSize || actor.Status != null && actor.Status.HasStatus<PanicStatus>())
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
                        weapons = weapons.OrderBy(w => w.ApToUse).ToList();
                    }

                    Weapon weapon = weapons.First();

                    TFTVLogger.Always($"lowest AP ranged weapon is {weapon.DisplayName}, with {weapon.ApToUse}");

                    if (actor.CharacterStats.ActionPoints <= weapon.ApToUse)
                    {
                        TFTVLogger.Always($"{actor.name} has {actor.CharacterStats.ActionPoints} AP is at POS {actor.Pos}, current target POS {target.Pos}");

                        if (!Cover(actor, target))
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
                    || tacticalActor.IsEvacuated || tacticalActor.Equipments == null || tacticalActor.Equipments.GetWeapons() == null || !tacticalActor.Equipments.GetWeapons().Any(w => w.IsUsable && w.HasCharges))
                {
                    return;
                }

                List<Weapon> weapons = new List<Weapon>(tacticalActor.Equipments.GetWeapons().Where(
                    w => w.IsUsable && w.HasCharges && w.TacticalItemDef.Tags.Contains(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"))
                    && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                    ));

                //  TFTVLogger.Always($"got here for {tacticalActor.name}, weapons count: {weapons.Count}");

                if (weapons.Count == 0)
                {
                    return;
                }

                Weapon bestWeapon = weapons.OrderByDescending(w => w.WeaponDef.EffectiveRange).ToList().First();

                //   TFTVLogger.Always($"best weapon for {tacticalActor.name} is {bestWeapon}. currently selectedWeapon null? {tacticalActor.Equipments.SelectedWeapon==null} currently selected equipment null? {tacticalActor.Equipments.SelectedEquipment==null}");

                if (tacticalActor.Equipments.SelectedWeapon == null || tacticalActor.Equipments.SelectedWeapon != bestWeapon)
                {
                    // TFTVLogger.Always($"Applying GetBestWeaponForQA {tacticalActor.name} was holding {tacticalActor.Equipments.SelectedWeapon?.DisplayName}, switching to {bestWeapon.DisplayName}");
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
                            if (targetActor.Status.HasStatus<MindControlStatus>() || actor.Status.HasStatus<MindControlStatus>() ||
                                (
                                targetActor.HasGameTag(Shared.SharedGameTags.MutoidTag)
                                && !targetActor.Status.HasStatus<BleedStatus>()
                                && !targetActor.HasStatus(DefCache.GetDef<DamageOverTimeStatusDef>("Poison_DamageOverTimeStatusDef"))
                                && !targetActor.HasStatus(DefCache.GetDef<ParalysisDamageOverTimeStatusDef>("Paralysis_DamageOverTimeStatusDef")
                                )
                                ))
                            {
                                return false;
                            }

                            //unless actor is Aspida, don't heal actor in fire, unless the target is actor, don't heal actors with disabled hands unless they have ignore pain
                            if (actor.GetAbilityWithDef<HealAbility>(DefCache.GetDef<HealAbilityDef>("SY_FullRestoration_AbilityDef")) == null)
                            {
                                if ((targetActor.TacticalPerception.IsTouchingVoxel(TacticalVoxelType.Fire) && targetActor != actor)
                                    || !targetActor.Status.HasStatus<FreezeAspectStatsStatus>() && targetActor.HasLostHandStatus())
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
            internal class BadTargets
            {

                //This removes small critters from move related considerations of the Big and Med monsters.

                private static bool CullSmallCrittersForBigAndMedMonsters(AIClosestEnemyConsiderationDef closestEnemyConsiderationDef, IAIActor actor, IAITarget target, object context, ref float score)
                {
                    try
                    {
                        AIClosestEnemyConsiderationDef queenConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Queen_ClosestEnemy_AIConsiderationDef");
                        AIClosestEnemyConsiderationDef chironConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Chiron_ClosestEnemy_AIConsiderationDef");
                        AIClosestEnemyConsiderationDef acheronConsideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Acheron_ClosestLineToEnemy_AIConsiderationDef");

                        List<AIClosestEnemyConsiderationDef> aIClosestEnemyConsiderationDefs = new List<AIClosestEnemyConsiderationDef>() { queenConsideration, chironConsideration, acheronConsideration };

                        GameTagDef caterpillarDamage = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                        if (aIClosestEnemyConsiderationDefs.Contains(closestEnemyConsiderationDef))
                        {

                            TacticalActor tacticalActor = (TacticalActor)actor;
                            TacAITarget tacAITarget = (TacAITarget)target;
                            float num = 0f;
                            TacticalActorBase tacticalActorBase = null;

                            //  TFTVLogger.Always($"{tacticalActor.name} considering closest enemy");

                            foreach (TacticalActorBase enemy in tacticalActor.TacticalFaction.AIBlackboard.GetEnemies(tacticalActor.AIActor.GetEnemyMask(closestEnemyConsiderationDef.EnemyMask)).Where(ta => !ta.HasGameTag(caterpillarDamage)))
                            {

                                float num2 = 0f;
                                if (closestEnemyConsiderationDef.DistanceType == DistanceType.Line)
                                {
                                    num2 = (tacAITarget.Pos - enemy.Pos).magnitude;
                                }
                                else if (closestEnemyConsiderationDef.DistanceType == DistanceType.PathLength)
                                {
                                    float num3 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, tacticalActor);
                                    float num4 = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy);
                                    float destinationRadius = num3 + num4 + closestEnemyConsiderationDef.DestinationRadius;
                                    num2 = AIUtil.GetPathLength(tacticalActor, tacAITarget.Pos, enemy.Pos, useAStar: true, destinationRadius);
                                }

                                float num5 = num2.Clamp(closestEnemyConsiderationDef.MinDistance, closestEnemyConsiderationDef.MaxDistance);
                                float num6 = 1f - (num5 - closestEnemyConsiderationDef.MinDistance) / (closestEnemyConsiderationDef.MaxDistance - closestEnemyConsiderationDef.MinDistance);
                                if (closestEnemyConsiderationDef.ConsiderEnemyWeight)
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
                                score = closestEnemyConsiderationDef.NoEnemiesValue;
                                //  TFTVLogger.Always($"no enemies!");
                                return false;
                            }

                            if (closestEnemyConsiderationDef.SetEnemyAsTarget)
                            {
                                tacAITarget.Actor = tacticalActorBase;
                            }

                            num = Mathf.Clamp(num, 0f, 1f);
                            if (Utl.Equals(num, 0f, 0.01f))
                            {
                                num = 0.1f;
                            }

                            score = num;
                            //  TFTVLogger.Always($"the score is {num}");
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




                [HarmonyPatch(typeof(AIClosestEnemyConsideration), "Evaluate")]
                public static class AIClosestEnemyConsideration_Evaluate_patch
                {

                    public static bool Prefix(AIClosestEnemyConsideration __instance, IAIActor actor, IAITarget target, object context, ref float __result)
                    {
                        try
                        {
                            if (CullSmallCrittersForBigAndMedMonsters(__instance.BaseDef as AIClosestEnemyConsiderationDef, actor, target, context, ref __result))
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
                }
                /* [HarmonyPatch(typeof(AIClosestEnemyConsideration), "Evaluate")]
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
                 }*/

                //Patch to prevent Scylla from MELEE targeting tiny critters like worms and spider drones
                [HarmonyPatch(typeof(TacticalAbility), "GetTargetActors", new Type[] { typeof(TacticalTargetData), typeof(TacticalActorBase), typeof(Vector3) })]
                public static class TFTV_TacticalAbility_GetTargetActors_Scylla_Patch
                {
                    public static IEnumerable<TacticalAbilityTarget> Postfix(IEnumerable<TacticalAbilityTarget> results, TacticalActorBase sourceActor, TacticalAbility __instance)
                    {

                        foreach (TacticalAbilityTarget target in results)
                        {
                            if (IsValidTarget(__instance.TacticalActor, target, __instance.SelectedEquipment as Weapon)) // <- create a method to check the target
                            {
                                yield return target;
                            }
                        }

                    }
                }

                public static bool IsValidTarget(TacticalActor actor, TacticalAbilityTarget target, Weapon weapon = null)
                {
                    try
                    {
                        // bool isValid = true;

                        TacticalActor targetActor = target.GetTargetActor() as TacticalActor;

                        if (targetActor == null)
                        {
                            return true;
                        }

                        if (!CheckValidParalyzedTarget(actor, targetActor))
                        {
                            //  TFTVLogger.Always($"{targetActor.DisplayName} culled for {actor.DisplayName}");
                            return false;
                        }


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
                            if (targetActor.GameTags.Contains(caterpillarDamage) && targetActor.TacticalFaction != actor.TacticalFaction)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (targetActor.GameTags.Contains(caterpillarDamage) && (targetActor.Pos - target.Actor.Pos).magnitude > 8)
                            {
                                return false;
                            }
                        }

                        DamageKeywordDef[] excludeDamageDefs =
                        {
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ParalysingKeyword,
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ViralKeyword
                        };


                        if (weapon != null && weapon.GetDamagePayload().DamageKeywords.Any(damageKeyordPair => excludeDamageDefs.Contains(damageKeyordPair.DamageKeywordDef)))
                        {
                            if (targetActor.ActorDef.name.Equals("SpiderDrone_ActorDef") ||
                                    targetActor.ActorDef.name.Contains("Turret_ActorDef"))
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
                        //  TFTVLogger.Always($"Weapon.GetShootTargets {__instance.WeaponDef.name}");

                        foreach (TacticalAbilityTarget target in results)
                        {
                            //    TFTVLogger.Always($"target {target?.Actor?.name}");

                            if (IsValidTarget(__instance.TacticalActor, target, __instance)) // <- create a method to check the target
                            {
                                yield return target;
                            }

                        }
                    }
                }

                //method to cull fully paralyzed enemy targets from PX allies

                private static bool CheckValidParalyzedTarget(TacticalActorBase actor, TacticalActor target)
                {
                    try
                    {
                        TacticalFaction phoenix = actor.TacticalLevel.GetFactionByCommandName("px");


                        if (phoenix == null || actor.TacticalFaction!=null && (actor.TacticalFaction == phoenix || target.TacticalFaction == phoenix))
                        {
                            return true;
                        }

                        ParalysedStatusDef paralysedStatusDef = DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef");

                       // TFTVLogger.Always($"{target.HasStatus(paralysedStatusDef)} {actor.TacticalFaction.GetRelationTo(phoenix) == FactionRelation.Friend}");

                        if (target.HasStatus(paralysedStatusDef) && actor.TacticalFaction.GetRelationTo(phoenix) == FactionRelation.Friend)
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


            }

        }





    }
}
