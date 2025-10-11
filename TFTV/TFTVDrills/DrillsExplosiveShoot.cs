using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal class DrillsExplosiveShoot
    {
        [CreateAssetMenu(fileName = "ExplosiveDisableShootAbilityDef", menuName = "Defs/Abilities/Tactical/Shoot/ExplosiveDisable")]
        [DefTarget(typeof(ExplosiveDisableShootAbility))]
        [SerializeType(InheritCustomCreateFrom = typeof(ShootAbilityDef))]
        public class ExplosiveDisableShootAbilityDef : ShootAbilityDef
        {
            public override void ValidateObject()
            {
                base.ValidateObject();
                if (ExplosionEffectDef == null)
                {
                    Debug.LogWarning("ExplosiveDisableShootAbilityDef is missing an ExplosionEffectDef reference.", this);
                }
            }

            [Header("Explosive Disable Settings")]
            [Tooltip("Optional tag that restricts the behaviour to weapons whose ItemClassTag matches this value.")]
            public GameTagDef RequiredWeaponClassTag;

            [Tooltip("Explosion will trigger when any disabled item carries at least one of these tags. Leave empty to trigger on all disabled items.")]
            public List<GameTagDef> TriggerItemTags = new List<GameTagDef>();

            [Tooltip("Effect that will be applied when a tagged item is disabled by this shot.")]
            public EffectDef ExplosionEffectDef;
        }

        public class ExplosiveDisableShootAbility : ShootAbility
        {
            public ExplosiveDisableShootAbilityDef ExplosiveDef => this.Def<ExplosiveDisableShootAbilityDef>();

            protected override IEnumerator<NextUpdate> Shoot(PlayingAction action)
            {
                bool entered = ExplosiveDisableShotContext.TryEnter(this);
                try
                {
                    yield return base.Timing.Call(base.Shoot(action), null);
                }
                finally
                {
                    if (entered)
                    {
                        ExplosiveDisableShotContext.Exit();
                    }
                }
            }

            internal bool ShouldTriggerOnDisabledItem(TacticalItem item)
            {
                if (item == null)
                {
                    return false;
                }

                ExplosiveDisableShootAbilityDef def = ExplosiveDef;
                if (def == null || def.ExplosionEffectDef == null)
                {
                    return false;
                }

                if (def.TriggerItemTags == null || def.TriggerItemTags.Count == 0)
                {
                    return true;
                }

                GameTagsList ownTags = item.OwnTags;
                foreach (GameTagDef tag in def.TriggerItemTags)
                {
                    if (tag != null && ownTags.Contains(tag))
                    {
                        return true;
                    }
                }

                return false;
            }

            internal void TriggerExplosionForDisabledItem(TacticalItem item)
            {
                ExplosiveDisableShootAbilityDef def = ExplosiveDef;
                if (def == null || def.ExplosionEffectDef == null)
                {
                    return;
                }

                EffectTarget effectTarget = CreateEffectTarget(item);
                if (effectTarget == null)
                {
                    return;
                }

                object effectSource = base.Source ?? this;
                Effect.Apply(base.Repo, def.ExplosionEffectDef, effectTarget, effectSource);
            }

            private EffectTarget CreateEffectTarget(TacticalItem item)
            {
                if (item == null)
                {
                    return null;
                }

                Vector3 position = Vector3.zero;
                bool hasPosition = false;
                try
                {
                    Transform aimPoint = item.GetAimPoint();
                    if (aimPoint != null)
                    {
                        position = aimPoint.position;
                        hasPosition = true;
                    }
                }
                catch (Exception)
                {
                    // In some contexts GetAimPoint can throw or log warnings if visuals are missing.
                    // We silently ignore those cases and fall back to actor based coordinates.
                }

                TacticalActorBase actor = item.TacticalActorBase;
                if (!hasPosition && actor != null)
                {
                    position = actor.Pos;
                    hasPosition = true;
                }

                if (!hasPosition)
                {
                    return null;
                }

                return new EffectTarget
                {
                    Object = item,
                    Position = position,
                    Param = item
                };
            }
        }

        internal static class ExplosiveDisableShotContext
        {
            [ThreadStatic]
            private static ExplosiveDisableShootAbility _currentAbility;

            [ThreadStatic]
            private static HashSet<TacticalItem> _processedItems;

            public static ExplosiveDisableShootAbility CurrentAbility => _currentAbility;

            public static HashSet<TacticalItem> ProcessedItems => _processedItems;

            public static bool TryEnter(ExplosiveDisableShootAbility ability)
            {
                if (ability == null)
                {
                    return false;
                }

                ExplosiveDisableShootAbilityDef def = ability.ExplosiveDef;
                if (def == null || def.ExplosionEffectDef == null)
                {
                    return false;
                }

                Weapon weapon = ability.Weapon;
                if (weapon == null)
                {
                    return false;
                }

                if (def.RequiredWeaponClassTag != null && weapon.ItemClassTag != def.RequiredWeaponClassTag)
                {
                    return false;
                }

                _currentAbility = ability;
                _processedItems = new HashSet<TacticalItem>();
                return true;
            }

            public static void Exit()
            {
                _currentAbility = null;
                _processedItems = null;
            }
        }

        [HarmonyPatch(typeof(TacticalAbilityReport))]
        internal static class ExplosiveDisableShootAbilityPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(TacticalAbilityReport.RaiseDamageEvents))]
            private static void ExplosiveDisable_OnRaiseDamageEvents(IEnumerable<TacticalAbilityReport> reports)
            {
                ExplosiveDisableShootAbility ability = ExplosiveDisableShotContext.CurrentAbility;
                if (ability == null)
                {
                    return;
                }

                HashSet<TacticalItem> processed = ExplosiveDisableShotContext.ProcessedItems;
                if (processed == null)
                {
                    return;
                }

                foreach (TacticalAbilityReport report in reports)
                {
                    if (report == null)
                    {
                        continue;
                    }

                    foreach (TacticalItem item in report.DisabledItems)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (!processed.Add(item))
                        {
                            continue;
                        }

                        if (!ability.ShouldTriggerOnDisabledItem(item))
                        {
                            continue;
                        }

                        ability.TriggerExplosionForDisabledItem(item);
                    }
                }
            }
        }

    }
}
