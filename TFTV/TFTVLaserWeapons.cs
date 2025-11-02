using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

/*internal class TFTVLaserWeapons
{
    /// <summary>
    /// Helper utilities that keep the shared laser ammo pools alive and
    /// provide easy access for Harmony patches.
    /// </summary>
    internal static class LaserAmmoShareHelper
    {
        /// <summary>
        /// Optional tag used to identify the weapons that should share ammunition.
        /// Populate this from your mod bootstrapper before the patches start firing.
        /// </summary>
        public static GameTagDef LaserWeaponTag; // may be left null

        /// <summary>
        /// Optional predicate override. If supplied it takes precedence over
        /// the <see cref="LaserWeaponTag"/> check and can be used for custom logic.
        /// </summary>
        public static Func<WeaponDef, bool> CustomLaserWeaponFilter; // may be left null

        private static readonly Dictionary<TacticalItemDef, SharedAmmoPool> PoolsByAmmo =
            new Dictionary<TacticalItemDef, SharedAmmoPool>();

        private static readonly Dictionary<AmmoManager, SharedAmmoPool> PoolsByManager =
            new Dictionary<AmmoManager, SharedAmmoPool>();

        /// <summary>
        /// Ensures that the provided item data is bound to the shared laser ammo manager when applicable.
        /// </summary>
        public static void TryAssignSharedAmmo(CommonItemData data)
        {
            if (data == null || data.OwnerItem == null)
            {
                return;
            }

            TacticalItemDef ammoDef;
            if (!TryGetLaserAmmoKey(data.OwnerItem, out ammoDef))
            {
                return;
            }

            var ammoManager = data.Ammo ?? new AmmoManager(data.OwnerItem);

            SharedAmmoPool pool;
            if (!PoolsByAmmo.TryGetValue(ammoDef, out pool))
            {
                pool = new SharedAmmoPool(ammoManager);
                PoolsByAmmo.Add(ammoDef, pool);
                PoolsByManager[ammoManager] = pool;
            }
            else
            {
                data.Ammo = pool.Manager;
                PoolsByManager[pool.Manager] = pool;
            }

            pool.Register(data.OwnerItem);
        }

        /// <summary>
        /// Returns a disposable scope that temporarily re-parents the shared ammo manager
        /// to the provided owner item while charges are being modified.
        /// </summary>
        public static ScopedAmmoUser PrepareForChargeMutation(CommonItemData data)
        {
            if (data == null || data.Ammo == null)
            {
                return default(ScopedAmmoUser);
            }

            SharedAmmoPool pool;
            if (!PoolsByManager.TryGetValue(data.Ammo, out pool))
            {
                return default(ScopedAmmoUser);
            }

            return pool.BeginUse(data.OwnerItem);
        }

        /// <summary>
        /// Determines whether the supplied data is currently using a shared ammo manager.
        /// </summary>
        public static bool IsUsingSharedManager(CommonItemData data)
        {
            return data != null && data.Ammo != null && PoolsByManager.ContainsKey(data.Ammo);
        }

        private static bool TryGetLaserAmmoKey(ICommonItem item, out TacticalItemDef ammoDef)
        {
            ammoDef = null; // reference type; may be left null when returning false

            var weaponDef = item != null ? item.ItemDef as WeaponDef : null;
            if (weaponDef == null)
            {
                return false;
            }

            if (!ShouldShareAmmo(weaponDef))
            {
                return false;
            }

            ammoDef = weaponDef.CompatibleAmmunition != null
                ? weaponDef.CompatibleAmmunition.FirstOrDefault()
                : null;

            return ammoDef != null;
        }

        private static bool ShouldShareAmmo(WeaponDef weaponDef)
        {
            if (CustomLaserWeaponFilter != null)
            {
                try
                {
                    if (CustomLaserWeaponFilter(weaponDef))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Custom logic failed, fall back to the tag based approach.
                    LoggerInstance.Error("CustomLaserWeaponFilter threw an exception: " + ex);
                }
            }

            if (LaserWeaponTag != null && weaponDef.Tags != null)
            {
                if (weaponDef.Tags.Contains(LaserWeaponTag))
                {
                    return true;
                }
            }

            return false;
        }

        internal sealed class SharedAmmoPool
        {
            private readonly List<WeakReference<ICommonItem>> _consumers =
                new List<WeakReference<ICommonItem>>();

            private WeakReference<ICommonItem> _primaryOwner;

            public SharedAmmoPool(AmmoManager manager)
            {
                if (manager == null) throw new ArgumentNullException("manager");
                Manager = manager;
                _primaryOwner = manager.ParentItem != null
                    ? new WeakReference<ICommonItem>(manager.ParentItem)
                    : null;
            }

            public AmmoManager Manager { get; private set; }

            public void Register(ICommonItem item)
            {
                if (item == null)
                {
                    return;
                }

                Cleanup();

                if (!_consumers.Any(reference =>
                {
                    ICommonItem existing;
                    return reference.TryGetTarget(out existing) && ReferenceEquals(existing, item);
                }))
                {
                    _consumers.Add(new WeakReference<ICommonItem>(item));
                }

                ICommonItem target;
                if (_primaryOwner == null || !_primaryOwner.TryGetTarget(out target) || target == null)
                {
                    _primaryOwner = new WeakReference<ICommonItem>(item);
                }

                if (Manager.ParentItem == null)
                {
                    Manager.ParentItem = item;
                }
            }

            public ScopedAmmoUser BeginUse(ICommonItem requestor)
            {
                var previous = Manager.ParentItem;
                if (requestor != null && !ReferenceEquals(previous, requestor))
                {
                    Manager.ParentItem = requestor;
                }

                return new ScopedAmmoUser(this, previous);
            }

            public void RestoreParent(ICommonItem previous)
            {
                var target = previous ?? GetFallbackOwner();
                if (target != null)
                {
                    Manager.ParentItem = target;
                }
            }

            private ICommonItem GetFallbackOwner()
            {
                ICommonItem owner;
                if (_primaryOwner != null && _primaryOwner.TryGetTarget(out owner) && owner != null)
                {
                    return owner;
                }

                foreach (var reference in _consumers)
                {
                    ICommonItem candidate;
                    if (reference.TryGetTarget(out candidate) && candidate != null)
                    {
                        _primaryOwner = new WeakReference<ICommonItem>(candidate);
                        return candidate;
                    }
                }

                return null;
            }

            private void Cleanup()
            {
                for (var i = _consumers.Count - 1; i >= 0; i--)
                {
                    ICommonItem target;
                    if (!_consumers[i].TryGetTarget(out target) || target == null)
                    {
                        _consumers.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Disposable scope that resets the ammo manager's parent when the current operation finishes.
        /// </summary>
        public readonly struct ScopedAmmoUser : IDisposable
        {
            private readonly SharedAmmoPool _pool;
            private readonly ICommonItem _previousParent;

            internal ScopedAmmoUser(SharedAmmoPool pool, ICommonItem previousParent)
            {
                _pool = pool;
                _previousParent = previousParent;
            }

            public void Dispose()
            {
                if (_pool != null)
                {
                    _pool.RestoreParent(_previousParent);
                }
            }
        }

        /// <summary>
        /// Minimal logger placeholder so the patches compile even if a logging infrastructure is not injected yet.
        /// Swap this implementation with your own if needed.
        /// </summary>
        internal static class LoggerInstance
        {
            public static void Error(string message)
            {
                // TODO: wire this up to the mod's logging solution.
            }
        }
    }

    /// <summary>
    /// Harmony patches that wire laser weapons to the shared ammo pool helper.
    /// </summary>
    [HarmonyPatch]
    internal static class SharedLaserAmmoPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommonItemData), nameof(CommonItemData.SetOwnerItem))]
        private static void BindSharedAmmoOnOwnerChange(CommonItemData __instance)
        {
            LaserAmmoShareHelper.TryAssignSharedAmmo(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommonItemData), MethodType.Constructor, new[]
        {
            typeof(ICommonItem),
            typeof(int),
            typeof(int),
            typeof(AmmoManager),
            typeof(int)
        })]
        private static void BindSharedAmmoAfterClone(CommonItemData __instance)
        {
            LaserAmmoShareHelper.TryAssignSharedAmmo(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommonItemData), MethodType.Constructor, new[]
        {
            typeof(ICommonItem),
            typeof(ItemData)
        })]
        private static void BindSharedAmmoAfterLoad(CommonItemData __instance)
        {
            LaserAmmoShareHelper.TryAssignSharedAmmo(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonItemData), nameof(CommonItemData.ModifyCharges))]
        private static void PrepareSharedAmmoUsage(CommonItemData __instance, out LaserAmmoShareHelper.ScopedAmmoUser __state)
        {
            __state = LaserAmmoShareHelper.PrepareForChargeMutation(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommonItemData), nameof(CommonItemData.ModifyCharges))]
        private static void RestoreSharedAmmoUsage(LaserAmmoShareHelper.ScopedAmmoUser __state)
        {
            __state.Dispose();
        }
    }
}*/

