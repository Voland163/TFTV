using Base;
using Base.AI;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Utils.Maths;
using HarmonyLib;
using Mono.Cecil;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
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
using UnityEngine;

namespace TFTV
{
    internal class TFTVArtOfCrab
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

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
                            if (actor.GetAbilityWithDef<HealAbility>(DefCache.GetDef<HealAbilityDef>("SY_FullRestoration_AbilityDef")) != null)
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


                [HarmonyPatch(typeof(ShootAbility), "GetTargets")]
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
                }
                public static bool IsValidTarget(TacticalActor actor, TacticalAbilityTarget target, Weapon weapon)
                {
                    try
                    {
                        bool isValid = true;


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

                        AttenuatingDamageTypeEffectDef paralysisDamage = DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Electroshock_AttenuatingDamageTypeEffectDef");
                        DamageOverTimeDamageTypeEffectDef virusDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef");

                        if (weapon.WeaponDef.Tags.Contains(meleeTag) && !actor.GameTags.Contains(queenTag))
                        {

                        }
                        else if (actor.GameTags.Contains(queenTag) || actor.GameTags.Contains(acheronTag) || actor.GameTags.Contains(chironTag) || actor.GameTags.Contains(cyclopsTag))
                        {
                            if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && tacticalActor.TacticalFaction != actor.TacticalFaction)
                            {
                                isValid = false;
                            }
                        }
                        else
                        {
                            if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && (tacticalActor.Pos - target.Actor.Pos).magnitude > 8)
                            {
                                isValid = false;
                            }
                        }

                        return isValid;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

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
                                || actor.GameTags.Contains(acheronTag) || actor.GameTags.Contains(chironTag)) //|| actor.GameTags.Contains(cyclopsTag))
                            {
                                if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && tacticalActor.TacticalFaction != actor.TacticalFaction)
                                {
                                    // TFTVLogger.Always($"{tacticalActor.name} culled for {actor.name}");
                                    culledList.Remove(target);
                                }
                            }
                            else
                            {
                                if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && (tacticalActor.Pos - target.Actor.Pos).magnitude > 8)
                                {
                                    // TFTVLogger.Always($"{tacticalActor.name} culled");
                                    culledList.Remove(target);
                                }
                            }
                        }

                        DamageKeywordDef[] excludeDamageDefs =
                        {
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ParalysingKeyword,
                    GameUtl.GameComponent<SharedData>().SharedDamageKeywords.ViralKeyword
                };

                        if (ability.Equipment != null && ability.Equipment is Weapon weapon
                            && weapon.GetDamagePayload().DamageKeywords.Any(damageKeyordPair => excludeDamageDefs.Contains(damageKeyordPair.DamageKeywordDef)))
                        {
                            foreach (TacticalAbilityTarget target in targetList)
                            {
                                if (target.GetTargetActor() is TacticalActor tacticalActor && (tacticalActor.ActorDef.name.Equals("SpiderDrone_ActorDef") ||
                                    tacticalActor.ActorDef.name.Contains("Turret_ActorDef")) && culledList.Contains(target))
                                {
                                    //   TFTVLogger.Always($"{tacticalActor.name} culled");
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


        /* [HarmonyPatch(typeof(ApplyDamageEffectAbility), "GetTargetActors")]
         public static class TFTV_ApplyDamageEffectAbility_GetAttackActorTarget_ChironStomp_Patch
         {
             public static void Postfix(ref TacticalAbilityTarget __result, TacticalActorBase actor, AttackType attackType, ApplyDamageEffectAbility __instance)
             {
                 try
                 {
                     if (actor is TacticalActor tacticalActor && tacticalActor.IsControlledByAI)
                     {

                     }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/








        /*   [HarmonyPatch(typeof(SpawnActorAbility), "Activate")]
          internal static class TFTV_SpawnActorAbility_Activate_SpawnerySpawn_patch
          {

              public static void Postfix(SpawnActorAbility __instance, object parameter)
              {
                  try

                  {
                      TacticalAbilityTarget target = (TacticalAbilityTarget)parameter;

                      TFTVLogger.Always($"parameter is {target.PositionToApply}");

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);

                  }
              }
          }*/


        /*  [HarmonyPatch(typeof(TacticalActor), "StartTurn")]
          internal static class TFTV_TacticalActor_StartTurn_SpawnerySpawn_patch
          {

              public static void Postfix(TacticalActor __instance)
              {
                  try
                  {
                      if (__instance.ActorDef.name.Equals("SpawningPoolCrabman_ActorDef"))
                      {

                          SpawnActorAbilityDef spawnSpawneryAbilityDef = DefCache.GetDef<SpawnActorAbilityDef>("SpawnerySpawnAbility");
                          //    TFTVLogger.Always("");
                          SpawnActorAbility spawnSpawneryAbility = __instance.GetAbilityWithDef<SpawnActorAbility>(spawnSpawneryAbilityDef);

                          if (spawnSpawneryAbility != null)
                          {
                              TacticalAbilityTarget target = spawnSpawneryAbility.GetTargetDirections(__instance).First();
                              spawnSpawneryAbility.Activate(target);
                          }



                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);

                  }
              }
          }
        */

    }
}
