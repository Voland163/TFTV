using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using UnityEngine;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class ShootOriginsTacticalVanillaFixes
    {
        /// <summary>
        /// A tactical save that could not be loaded.
        ///
        /// When a tactical level starts, the game pre-computes where every weapon on the map fires from,
        /// by posing a stand-in of the carrier and looking up the weapon's projectile-origin bone on it.
        /// It does not check the lookup: if the bone is not on the stand-in, CacheShootOrigins throws, and
        /// because it runs inside OnLevelStart the whole level start is aborted. Seen loading a save
        /// with an operative's riot shield deployed: the shield is re-attached to the shield point on load
        /// ("attaching ... for a second time"), and the lookup came back empty.
        ///
        /// The cache is only an optimisation - TacticalPerception.GetShotOrigin asks
        /// TryGetShootOriginFor first and works the origin out itself when nothing is cached - so a weapon
        /// that cannot be cached is skipped and logged rather than taking the level down with it.
        /// </summary>
        [HarmonyPatch(typeof(ShootOriginsCache), nameof(ShootOriginsCache.CacheShootOrigins))]
        internal static class ShootOriginsCache_CacheShootOrigins_Patch
        {
            private static Exception Finalizer(Exception __exception, Weapon weapon, GameObject rigPrefab)
            {
                if (__exception == null)
                {
                    return null;
                }

                try
                {
                    TFTVLogger.Always($"[ShootOrigins] Could not cache shoot origins for {weapon?.WeaponDef?.name} " +
                        $"carried by {weapon?.TacticalActor?.DisplayName} (rig {rigPrefab?.name}); skipped. " +
                        $"{__exception.GetType().Name}: {__exception.Message}");
                }
                catch
                {
                    // Logging must never be what breaks the level start.
                }

                return null;
            }
        }
    }
}
