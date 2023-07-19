using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVArtOfCrab
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
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
                          //    TFTVLogger.Always("Got here");
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

        //Force Scylla to use Cannons
        [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

        public static class TacticalActor_OnAbilityExecuteFinished_Scylla_Experiment_patch
        {
            public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
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
                        //    TFTVLogger.Always("Got here");
                        StartPreparingShootAbility startPreparingShootAbility = __instance.GetAbilityWithDef<StartPreparingShootAbility>(scyllaStartPreparing);

                        if (startPreparingShootAbility != null)
                        {
                            startPreparingShootAbility.Activate(parameter);
                        }

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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
        //NavMeshAreasDef


       


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
                        
                        // TFTVLogger.Always($"{tacticalActor.DisplayName} is looking for targets");
                        __result = CullTargetsLists(__result, sourceActor, __instance);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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
                    else if (actor.GameTags.Contains(queenTag) || actor.GameTags.Contains(acheronTag) || actor.GameTags.Contains(chironTag) || actor.GameTags.Contains(cyclopsTag))
                    {
                        if (target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.GameTags.Contains(caterpillarDamage) && tacticalActor.TacticalFaction!=actor.TacticalFaction)
                        {
                        //    TFTVLogger.Always($"{tacticalActor.name} culled");
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

                foreach (TacticalAbilityTarget tacticalAbilityTarget in culledList)
                {

                  //  TFTVLogger.Always($"target is {tacticalAbilityTarget.GetTargetActor()}");

                }

              return culledList;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



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



        [HarmonyPatch(typeof(TacticalFaction), "GetSortedAIActors")]

        public static class TFTV_TacticalFactionn_GetSortedAIActors_ArtOfCrab_patch
        {
            public static void Postfix(List<TacticalActor> __result, TacticalFaction __instance)
            {
                try
                {
                    if (__result.Count > 0)
                    {
                        SortOutAITurnOrder(__result);
                        __result.Sort((TacticalActor a, TacticalActor b) => a.AIActor.TurnOrderPriority - b.AIActor.TurnOrderPriority);
                        TFTVHumanEnemies.ApplyTactic(__instance.TacticalLevel);
                        TFTVLogger.Always("TFTV: Art of Crab: Sorted AI Turn Order");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }
    }
}
