using Base.Levels.Nav;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using UnityEngine;

namespace TFTV
{
    internal class TFTVGoo
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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
         //           if (!DontUseGooNavigationPatch)
           //         {
                        TacStatsModifyStatusDef slowedStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                        GooDamageMultiplierAbilityDef gooImmunity = DefCache.GetDef<GooDamageMultiplierAbilityDef>("GooImmunity_AbilityDef");

                        TacticalVoxel voxel = ____actor.TacticalLevel.VoxelMatrix.GetVoxel(dstPos);

                  //      TacStatsModifyStatus status = ____actor.Status.GetStatus<TacStatsModifyStatus>(slowedStatus);

                       

                        float actorRadius = ____actor.NavigationComponent.AgentNavSettings.AgentRadius;

                        if (voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Goo && !____actor.HasStatus(slowedStatus) && actorRadius <= TacticalMap.HalfTileSize && ____actor.GetAbilityWithDef<GooDamageMultiplierAbility>(gooImmunity) == null)
                        {
                            __result = 2f;
                        }
             //       }

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
                GooDamageMultiplierAbilityDef gooImmunity = DefCache.GetDef<GooDamageMultiplierAbilityDef>("GooImmunity_AbilityDef");

                if (actor.GetAbilityWithDef<GooDamageMultiplierAbility>(gooImmunity) == null && actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Goo, pos))
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
