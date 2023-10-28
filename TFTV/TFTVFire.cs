using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVFire
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static List<TacticalVoxel> VoxelsOnFire = new List<TacticalVoxel>();

        /// <summary>
        /// This method check on Geoscape start if the player has used fire weapons yet.
        /// Based on roll that scales with difficulty gives special FireQuencher ability to arthrons with non-poison head.
        /// As long as the head is not disabled, they are immune to fire and turn fire tiles that they come in contact with into Mist.
        /// </summary>
        /// <param name="controller"></param>

        private static void CheckUseFireWeaponsAndDifficulty(GeoLevelController controller)
        {
            try
            {
                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 0 && TFTVReleaseOnly.DifficultyOrderConverter(controller.CurrentDifficultyLevel.Order) > 1)
                {
                    // TFTVLogger.Always("Checking fire weapons usage to decide wether to add Fire Quenchers");

                    PhoenixStatisticsManager phoenixStatisticsManager = GameUtl.GameComponent<PhoenixGame>().GetComponent<PhoenixStatisticsManager>();
                 
                    if (phoenixStatisticsManager == null)
                    {
                        TFTVLogger.Always($"Failed to get stat manager in CheckUseFireWeaponsAndDifficulty");
                        return;
                    }

                    List<SoldierStats> allSoldierStats = new List<SoldierStats>(phoenixStatisticsManager.CurrentGameStats.LivingSoldiers.Values.ToList());
                    allSoldierStats.AddRange(phoenixStatisticsManager.CurrentGameStats.DeadSoldiers.Values.ToList());
                    List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                    int scoreFireDamage = 0;
                    StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");

                    foreach (SoldierStats stat in allSoldierStats)
                    {
                        if (stat.ItemsUsed.Count > 0)
                        {
                            usedWeapons.AddRange(stat.ItemsUsed);
                        }
                    }

                    if (usedWeapons.Count() > 0)
                    {
                        // TFTVLogger.Always("Checking use of each weapon... ");
                        foreach (UsedWeaponStat stat in usedWeapons)
                        {
                            //   TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());
                            if (Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Contains(stat.UsedItem.ToString())))
                            {
                                WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                                if (weaponDef != null && weaponDef.DamagePayload.DamageType == fireDamage)
                                {
                                    scoreFireDamage += stat.UsedCount;
                                }
                            }
                        }
                    }

                    // TFTVLogger.Always("Fire weapons used " + scoreFireDamage + " times");

                    if (scoreFireDamage > 1)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(0, 6) + TFTVReleaseOnly.DifficultyOrderConverter(controller.CurrentDifficultyLevel.Order) + scoreFireDamage;

                        //    TFTVLogger.Always("The roll is " + roll);
                        if (roll >= 10)
                        {
                            //    TFTVLogger.Always("The roll is passed!");
                            controller.EventSystem.SetVariable("FireQuenchersAdded", 1);
                        }

                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckForFireQuenchers(GeoLevelController controller)
        {
            try
            {
                CheckUseFireWeaponsAndDifficulty(controller);

                if (controller.EventSystem.GetVariable("FireQuenchersAdded") == 1)
                {
                    AddFireQuencherAbility();
                    TFTVLogger.Always("Fire Quenchers added!");
                }
                else
                {

                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };
                    DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = new TacticalAbilityDef[] { };

                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AddFireQuencherAbility()
        {
            try
            {
                ApplyStatusAbilityDef fireQuencherAbility = DefCache.GetDef<ApplyStatusAbilityDef>("FireQuencherAbility");
                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunityInvisibleAbility");
                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>() { fireQuencherAbility, fireImmunity };
               

                DefCache.GetDef<TacticalItemDef>("Crabman_Head_Humanoid_BodyPartDef").Abilities = abilities.ToArray();
                DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef").Abilities = abilities.ToArray();
               
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        /// <summary>
        /// This method is called every time the game checks if a character is touch some voxel (goo, fire, Mist).
        /// If the character has the FireQuencherStatus, given by the FireQuencherAbility, surrouding voxels are recorded in a list.
        /// </summary>
        /// <param name="tacticalPerceptionBase"></param>

        public static void CheckFireQuencherTouchingFire(TacticalPerceptionBase tacticalPerceptionBase)
        {

            try
            {
                DamageMultiplierStatusDef fireQuencherStatus = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");

                TacticalActorBase tacticalActorBase = tacticalPerceptionBase.TacActorBase;

                if (tacticalActorBase is TacticalActor && tacticalActorBase.GetActor().Status.HasStatus(fireQuencherStatus))
                {
                    foreach (TacticalVoxel voxel in tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxels(tacticalPerceptionBase.GetBounds()))
                    {
                        if (voxel.GetVoxelType() == TacticalVoxelType.Fire)
                        {
                            VoxelsOnFire.Add(voxel);
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, 0.5f)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, -0.5f)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-0.5f, 0, 0.5f)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(0.5f, 0, -0.5f)));

                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, 1)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, -1)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(1, 0, -1)));
                            VoxelsOnFire.Add(tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxel(voxel.Position + new Vector3(-1, 0, 1)));

                        }
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        /// <summary>
        /// This method is called every time an ability finishes executing.
        /// It checks if the VoxelsOnFire list has any voxels in it and if the voxel has fire on it,
        /// 1) in a first loop removes fire from all the voxels that have it, 
        /// 2) in a second loop, adds mist to the tiles that are empty,
        /// 3) clears the list
        /// Steps 1 and 2 are necessary because changing a fire voxel directly to a mist voxel causes issues.
        /// </summary>


        public static void ActivateFireQuencherAbility()
        {
            try
            {
                if (VoxelsOnFire != null)
                {

                    if (VoxelsOnFire.Count > 0)
                    {
                        //  TFTVLogger.Always("Voxels on fire count is " + TFTVExperimental.VoxelsOnFire.Count);
                        // List<TacticalVoxel> voxelsForMist = new List<TacticalVoxel>();
                        foreach (TacticalVoxel voxel in VoxelsOnFire)
                        {
                            if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Fire)
                            {
                                //    TFTVLogger.Always("Got past the if check");

                                voxel.SetVoxelType(TacticalVoxelType.Empty, 1);
                            }
                        }
                    }

                    if (VoxelsOnFire.Count > 0)
                    {
                        foreach (TacticalVoxel voxel in VoxelsOnFire)
                        {
                            if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Empty)
                            {
                                //TFTVLogger.Always("Got past the if check for Mist");
                                voxel.SetVoxelType(TacticalVoxelType.Mist, 2, 10);
                            }
                        }
                    }

                    VoxelsOnFire.Clear();
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



    }




}
