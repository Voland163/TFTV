using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
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


namespace TFTV
{
    internal class TFTVArtOfCrab
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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
                            TFTVLogger.Always("faction " + factionVision.Faction.Faction.FactionDef.name + " has revealed enemy " + tacticalActorBase.DisplayName);
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
            public static void Postfix(List<TacticalActor> __result)
            {
                try
                {
                    if (__result.Count > 0)
                    {
                        SortOutAITurnOrder(__result);
                        __result.Sort((TacticalActor a, TacticalActor b) => a.AIActor.TurnOrderPriority - b.AIActor.TurnOrderPriority);
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
