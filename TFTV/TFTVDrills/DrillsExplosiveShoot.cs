using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Serialization.General;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal class DrillsExplosiveShoot
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

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
                if (def == null)
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

            internal void TriggerExplosionForDisabledItem(TacticalItem item, TacticalActorBase fallbackActor = null)
            {
                EffectTarget effectTarget = CreateEffectTarget(item, fallbackActor);

                if (effectTarget == null)
                {
                    return;
                }

                ExplosiveDisableShootAbilityDef def = ExplosiveDef;

                if (def == null || def.ExplosionEffectDef == null)
                {
                    return;
                }

                object effectSource = base.Source ?? this;

                DamagePayloadEffectDef explosionEffect = (DamagePayloadEffectDef)def.ExplosionEffectDef;

                explosionEffect.DamagePayload.DamageKeywords = new List<DamageKeywordPair>() 
                { 
                    explosionEffect.DamagePayload.DamageKeywords[0], 
                };

                explosionEffect.DamagePayload.ObjectToSpawnOnExplosion = DefCache.GetDef<ExplosionEffectDef>("E_ShrapnelExplosion [ExplodingBarrel_ExplosionEffectDef]").ObjectToSpawn;

                float radius = 2.5f;
                float damage = 50;

                if (item is Weapon weapon)
                {
                    WeaponDef weaponDef = weapon.WeaponDef;

                    DamagePayload weaponPayload = weapon.GetDamagePayload();

                    List<DamageKeywordPair> damageKeywordPairs = new List<DamageKeywordPair>();

                    foreach (DamageKeywordPair damageKeywordPair in weaponPayload.DamageKeywords)
                    {
                        TFTVLogger.Always($"damageKeywordPair: {damageKeywordPair?.DamageKeywordDef?.name} {damageKeywordPair?.Value}");
                       
                        if (damageKeywordPair?.DamageKeywordDef == Shared.SharedDamageKeywords.BlastKeyword)
                        {
                            damage = damageKeywordPair.Value;
                            TFTVLogger.Always($"setting blast damage to... {damage}");
                        }

                        if (damageKeywordPair?.DamageKeywordDef == Shared.SharedDamageKeywords.ShreddingKeyword)
                        {
                            TFTVLogger.Always($"{weaponDef.name} does shred damage");
                            explosionEffect.DamagePayload.DamageKeywords.Add(damageKeywordPair);
                        }
                        
                        if(damageKeywordPair?.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                            {
                                TFTVLogger.Always($"{weaponDef.name} does acid damage");
                                 explosionEffect.DamagePayload.DamageKeywords.Add(damageKeywordPair);
                            }
                        if (damageKeywordPair?.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword)
                        {
                            TFTVLogger.Always($"{weaponDef.name} does fire damage");
                            explosionEffect.DamagePayload.DamageKeywords.Add(damageKeywordPair);
                        }
                    }

                    if (weaponPayload.ObjectToSpawnOnExplosion != null)
                    {

                        TFTVLogger.Always($"weaponPayload.ObjectToSpawnOnExplosion: {weaponPayload.ObjectToSpawnOnExplosion.name}");
                       // explosionEffect.DamagePayload.ObjectToSpawnOnExplosion = weaponPayload.ObjectToSpawnOnExplosion;
                    }
                   
                }

                radius *= 1.5f; // +50% radius
                damage *= 2f;   // 2x damage


                explosionEffect.DamagePayload.AoeRadius = radius;
                explosionEffect.DamagePayload.DamageKeywords[0].Value = damage;

                Effect.Apply(base.Repo, explosionEffect, effectTarget, effectSource);
            }

            private EffectTarget CreateEffectTarget(TacticalItem item, TacticalActorBase fallbackActor)
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

                if (!hasPosition && fallbackActor != null)
                {
                    position = fallbackActor.Pos;
                    hasPosition = true;
                    actor = fallbackActor;
                }

                if (!hasPosition)
                {
                    return null;
                }

                return new EffectTarget
                {
                    Object = item != null ? (object)item : actor,
                    GameObject = actor?.gameObject,
                    Position = position,
                    Rotation = Quaternion.identity,
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
                if (def == null)
                {
                    return false;
                }

                Weapon weapon = ability.Weapon;
                if (weapon == null)
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

            public static void TryProcessDisabledItem(TacticalItem item, TacticalActorBase fallbackActor)
            {
                ExplosiveDisableShootAbility ability = _currentAbility;
                HashSet<TacticalItem> processed = _processedItems;
                if (ability == null || processed == null || item == null)
                {
                    return;
                }

                if (!processed.Add(item))
                {
                    return;
                }

                if (!ability.ShouldTriggerOnDisabledItem(item))
                {
                    return;
                }

                ability.TriggerExplosionForDisabledItem(item, fallbackActor);
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

               // TFTVLogger.Always($"ExplosiveDisable_OnRaiseDamageEvents: ability {ability == null}");

                if (ability == null)
                {
                    return;
                }

                HashSet<TacticalItem> processed = ExplosiveDisableShotContext.ProcessedItems;

              //  TFTVLogger.Always($"processed: {processed == null}");

                if (processed == null)
                {
                    return;
                }

            //    TFTVLogger.Always($"reports count: {reports.Count()}");

                foreach (TacticalAbilityReport report in reports)
                {
                 //   TFTVLogger.Always($"report: damaged actor: {report?.DamagedActor?.name} disabled parts: {report?.DisabledItems?.Count()} ");

                    if (report == null)
                    {
                        continue;
                    }

                    TacticalActorBase damagedActor = report.DamagedActor;

                    foreach (TacticalItem item in report.DisabledItems)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        ExplosiveDisableShotContext.TryProcessDisabledItem(item, damagedActor);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TacticalItem))]
        internal static class ExplosiveDisableShootAbilityItemPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(TacticalItem.SetToDisabled))]
            private static void ExplosiveDisable_CacheOwner(TacticalItem __instance, ref TacticalActorBase __state)
            {
               // TFTVLogger.Always($"disabledItem {__instance?.DisplayName}");

                __state = __instance != null ? __instance.TacticalActorBase : null;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(TacticalItem.SetToDisabled))]
            private static void ExplosiveDisable_OnItemDisabled(TacticalItem __instance, TacticalActorBase __state)
            {
                if (__instance == null)
                {
                    return;
                }

                ExplosiveDisableShotContext.TryProcessDisabledItem(__instance, __state);
            }
        }

        [HarmonyPatch(typeof(UIModuleFreeFirstPersonShooting))]
        internal static class ExplosiveDisableShootAbilityUIModulePatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(UIModuleFreeFirstPersonShooting.OnNewDamageReceiverSelected))]
            private static void ExplosiveDisable_OnNewDamageReceiverSelected(UIModuleFreeFirstPersonShooting __instance, IDamageReceiver damageReceiver, bool inRange)
            {
                ExplosiveDisableTargetHighlighter.Update(__instance != null ? (__instance.SelectedAbility as ExplosiveDisableShootAbility) : null, damageReceiver);
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(UIModuleFreeFirstPersonShooting.ClearFirstPersonShootingLayout))]
            private static void ExplosiveDisable_OnClearFirstPersonLayout()
            {
                ExplosiveDisableTargetHighlighter.Clear();
            }
        }

        internal static class ExplosiveDisableTargetHighlighter
        {
            private static readonly HashSet<IHighlightable> _highlighted = new HashSet<IHighlightable>();

            public static void Update(ExplosiveDisableShootAbility ability, IDamageReceiver damageReceiver)
            {
                if (ability == null)
                {
                    Clear();
                    return;
                }

                TacticalActorBase tacticalActorBase = damageReceiver != null ? damageReceiver.GetActor() : null;
                if (tacticalActorBase == null)
                {
                    Clear();
                    return;
                }

                HashSet<IHighlightable> next = GatherHighlightTargets(ability, tacticalActorBase);
                Apply(next);
            }

            public static void Clear()
            {
                if (_highlighted.Count == 0)
                {
                    return;
                }

                foreach (IHighlightable highlightable in _highlighted)
                {
                    highlightable?.Highlight(false, false, true);
                }

                _highlighted.Clear();
            }

            private static HashSet<IHighlightable> GatherHighlightTargets(ExplosiveDisableShootAbility ability, TacticalActorBase actor)
            {
                HashSet<IHighlightable> hashSet = new HashSet<IHighlightable>();
                TacticalActor tacticalActor = actor as TacticalActor;
                if (tacticalActor == null || tacticalActor.BodyState == null)
                {
                    return hashSet;
                }

                foreach (ItemSlot itemSlot in tacticalActor.BodyState.GetSlots())
                {
                    bool flag = false;
                    foreach (TacticalItem tacticalItem in itemSlot.GetAllDirectItems(false))
                    {
                        if (tacticalItem != null && ability.ShouldTriggerOnDisabledItem(tacticalItem))
                        {
                            hashSet.Add(tacticalItem);
                            flag = true;
                        }
                    }

                    if (flag)
                    {
                        hashSet.Add(itemSlot);
                    }
                }

                if (tacticalActor.Equipments != null)
                {
                    foreach (TacticalItem tacticalItem2 in tacticalActor.Equipments.Items.OfType<TacticalItem>())
                    {
                        if (tacticalItem2 != null && ability.ShouldTriggerOnDisabledItem(tacticalItem2))
                        {
                            hashSet.Add(tacticalItem2);
                            ItemSlot parentItemSlot = tacticalItem2.ParentItemSlot;
                            if (parentItemSlot != null)
                            {
                                hashSet.Add(parentItemSlot);
                            }
                        }
                    }
                }

                return hashSet;
            }

            private static void Apply(HashSet<IHighlightable> next)
            {
                if (next.Count == 0)
                {
                    Clear();
                    return;
                }

                foreach (IHighlightable highlightable in _highlighted)
                {
                    if (highlightable == null || !next.Contains(highlightable))
                    {
                        highlightable?.Highlight(false, false, true);
                    }
                }

                foreach (IHighlightable highlightable2 in next)
                {
                    if (highlightable2 != null && !_highlighted.Contains(highlightable2))
                    {
                        highlightable2.Highlight(true, false, true);
                    }
                }

                _highlighted.Clear();
                _highlighted.UnionWith(next);
            }
        }

        
    }
}
