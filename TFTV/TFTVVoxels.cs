using Base.Core;
using Base.Defs;
using Base.Levels.Nav;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVVoxels
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static bool GooVoxelSpawnAlreadyChecked = false;
        public static bool FireVoxelSpawnAlreadyChecked = false;

        [HarmonyPatch(typeof(TacticalVoxelMatrix), "VoxelSpawned")]
        public static class TacticalVoxelMatrix_VoxelSpawned_patch
        {
            public static void Postfix(TacticalVoxelMatrix __instance, TacticalVoxel voxel)
            {
                try
                {
                    if (!GooVoxelSpawnAlreadyChecked && __instance.HadGoo)
                    {
                        PropertyInfo propertyInfo = typeof(TacticalVoxelMatrix).GetProperty("HadGoo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        if (!__instance.TacticalLevel.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                        {
                            propertyInfo.SetValue(__instance, false);
                            return;
                        }
                        else
                        {
                            if (__instance.TacticalLevel.CurrentFaction == __instance.TacticalLevel.GetFactionByCommandName("aln"))
                            {
                                TFTVLogger.Always($"Pandorans used goo!");
                                GooVoxelSpawnAlreadyChecked = true;
                            }
                            else
                            {
                                propertyInfo.SetValue(__instance, false);
                                // TFTVLogger.Always($"Someone else used goo! HadGoo is {(bool)propertyInfo.GetValue(__instance)}");
                            }
                        }
                    }

                    if (!FireVoxelSpawnAlreadyChecked && voxel.GetVoxelType() == TacticalVoxelType.Fire
                       && __instance.TacticalLevel.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        TFTVLogger.Always($"Player used fire vs Pandorans! (lets assume)");
                        FireVoxelSpawnAlreadyChecked = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        internal class TFTVFire
        {
            public static List<TacticalVoxel> VoxelsOnFire = new List<TacticalVoxel>();

            /// <summary>
            /// This method check on Geoscape start if the player has used fire weapons yet.
            /// Based on roll that scales with difficulty gives special FireQuencher ability to arthrons with non-poison head.
            /// As long as the head is not disabled, they are immune to fire and turn fire tiles that they come in contact with into Mist.
            /// </summary>
            /// <param name="controller"></param>

            private static bool FireUseThresholdPassed(GeoLevelController controller)
            {
                try
                {
                    int difficulty = controller.CurrentDifficultyLevel.Order;
                    int fireUsedInMissions = controller.EventSystem.GetVariable("FireQuenchersAdded");

                    TFTVLogger.Always($"fire used in missions: {fireUsedInMissions}");

                    if (FireVoxelSpawnAlreadyChecked)
                    {
                        FireVoxelSpawnAlreadyChecked = false;

                        if (fireUsedInMissions > 10 - difficulty)
                        {
                            TFTVLogger.Always($"FireQuenchers should be already implemented; fire has been used in {fireUsedInMissions} missions vs Pandorans");

                            return true;
                        }
                        else
                        {
                            fireUsedInMissions++;
                            controller.EventSystem.SetVariable("FireQuenchersAdded", fireUsedInMissions);
                        }
                    }

                    if (fireUsedInMissions > 10 - difficulty)
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

            public static void CheckForFireQuenchers(GeoLevelController controller)
            {
                try
                {
                    if (FireUseThresholdPassed(controller))
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


        internal class TFTVGoo
        {

            /// <summary>
            /// Code to change goo navigation behaviour, implementation idea from Codemite (all hail Codemite!)
            /// </summary>
            /// 

            // public static bool DontUseGooNavigationPatch = false;

            [HarmonyPatch(typeof(TacticalNavCostFactorFuncs), "CostFactorFunc")]
            public static class TacticalNavCostFactorFuncs_CostFactorFunc_patch
            {

                public static void Postfix(Vector3 srcPos, Vector3 dstPos, NavAreas areas, ref float __result, TacticalActor ____actor)
                {
                    try
                    {
                        TacStatsModifyStatusDef slowedStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");

                        TacticalVoxel voxel = ____actor.TacticalLevel.VoxelMatrix.GetVoxel(dstPos);

                        float actorRadius = ____actor.NavigationComponent.AgentNavSettings.AgentRadius;

                        if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Goo &&
                            !____actor.HasStatus(slowedStatus) &&
                            ____actor.GetAbility<GooDamageMultiplierAbility>() == null)
                        {
                            __result = 20f;//2f;
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            [HarmonyPatch(typeof(GooVoxelManager), "StartTurn")]
            public static class GooVoxelManager_StartTurn_patch
            {

                public static void Postfix(TacticalVoxel ____voxel, GooVoxelManager __instance)
                {
                    try
                    {

                        __instance.TurnNumberSpawned = __instance.TurnNumber;

                        foreach (TacticalActor actor in ____voxel.Matrix.TacticalLevel.Map.GetActors<TacticalActor>())
                        {
                            AddGooedStatus(actor, actor.Pos);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(GooVoxelManager), "OnSpawn")]
            public static class GooVoxelManager_OnSpawn_patch
            {

                public static bool Prefix(TacticalVoxel ____voxel, GooVoxelManager __instance)
                {
                    try
                    {

                        __instance.TurnNumberSpawned = __instance.TurnNumber;

                        foreach (TacticalActor actor in ____voxel.Matrix.TacticalLevel.Map.GetActors<TacticalActor>())
                        {
                            AddGooedStatus(actor, actor.Pos);
                        }

                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void AddGooedStatus(TacticalActor actor, Vector3 pos)
            {
                try
                {
                    if (actor.Status == null || actor.TacticalPerceptionBase == null)
                    {

                        return;

                    }
                    TacStatsModifyStatusDef slowedStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                    TacStatsModifyStatus status = actor.Status.GetStatus<TacStatsModifyStatus>(slowedStatus);
                    // GooDamageMultiplierAbilityDef gooImmunity = DefCache.GetDef<GooDamageMultiplierAbilityDef>("GooImmunity_AbilityDef");

                    if (actor.GetAbility<GooDamageMultiplierAbility>() == null && actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Goo, pos))
                    {
                        if (status == null)
                        {
                            actor.Status.ApplyStatus(slowedStatus);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            public static void CheckActorTouchingGoo(TacticalPerceptionBase tacticalPerceptionBase)
            {
                try
                {
                    TacticalActorBase tacticalActorBase = tacticalPerceptionBase.TacActorBase;
                    TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                    TacStatsModifyStatusDef slowedStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();           

                    if (tacticalActor == null
                        || controller.CurrentFaction != tacticalActor.TacticalFaction
                        || tacticalActor.GetAbility<GooDamageMultiplierAbility>() != null
                        || tacticalActor.Status.HasStatus(slowedStatus))
                    {
                        return;
                    }

                    foreach (TacticalVoxel voxel in tacticalActorBase.TacticalLevel.VoxelMatrix.GetVoxels(tacticalPerceptionBase.GetBounds()))
                    {
                        if (voxel.GetVoxelType() == TacticalVoxelType.Goo)
                        {
                            TacticalNavigationComponent navComponent = tacticalActor.TacticalNav;
                            float apToSubtract = 1 / navComponent.DistanceToAPFactor;

                            tacticalActor.CharacterStats.ActionPoints.Subtract(apToSubtract);

                           // TFTVLogger.Always($"subtracting {apToSubtract} from {tacticalActor.DisplayName} because traversing Goo");
                        }
                    }     
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }



            [HarmonyPatch(typeof(TacticalVoxelMatrix), "UpdateGooedStatus")]
            public static class TacticalVoxelMatrix_UpdateGooedStatus_patch
            {
                public static bool Prefix()
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
        }

    }




}
