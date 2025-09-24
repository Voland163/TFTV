using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static TFTV.TFTVDrills.DrillsDefs;



namespace TFTV.TFTVDrills
{
    internal class DrillsHarmony
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal class BulletHell
        {
            private static readonly GameTagDef AssaultRifleTag = DefCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef");
            private static readonly MethodInfo TacticalItemIsFunctionalGetter = AccessTools.PropertyGetter(typeof(TacticalItem), "IsFunctional");
            private static readonly MethodInfo TacticalItemIsDisabledGetter = AccessTools.PropertyGetter(typeof(TacticalItem), "IsDisabled");
            private static readonly PropertyInfo OwnerItemProperty = AccessTools.Property(typeof(TacticalItemAspectBase), "OwnerItem");
            private static readonly HashSet<int> ProcessedItems = new HashSet<int>();

            [HarmonyPatch(typeof(TacticalActor), "ShouldChangeAspectStats")]
            public static class TacticalActor_ShouldChangeAspectStats_BulletHell_Patch
            {
                public static void Postfix(TacticalActor __instance, TacticalItemAspectBase aspect)
                {
                    try
                    {
                        if (_bulletHell == null || aspect == null)
                        {
                            return;
                        }

                        if (!(aspect is BodyPartAspect))
                        {
                            return;
                        }

                        TacticalItem tacticalItem = aspect.OwnerItem;
                        if (tacticalItem == null)
                        {
                            return;
                        }

                        TacticalActor attacker = TacUtil.GetSourceTacticalActorBase(__instance.LastDamageSource) as TacticalActor;

                        if (attacker == null)
                        {
                            return;
                        }

                        if (attacker.Status == null || !attacker.Status.HasStatus(_bulletHellSlowStatus))
                        {
                            return;
                        }


                        Weapon weapon = attacker.Equipments?.SelectedWeapon;


                        if (weapon?.WeaponDef == null || !weapon.WeaponDef.Tags.Contains(AssaultRifleTag))
                        {
                            return;
                        }

                        ApplyEffects(attacker);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }



            private static void ApplyEffects(TacticalActor attacker)
            {
                try
                {
                    if (attacker?.Status == null)
                    {
                        return;
                    }


                    AddAttackBoostStatusDef rapidClearanceAttackBoostStatusDef = (AddAttackBoostStatusDef)Repo.GetDef("9385a73f-8d20-4022-acc1-9210e2e29b8f");
                    Status existingBulletHellStatus = attacker.Status.GetStatusByName(_bulletHellAttackBoostStatus.EffectName);
                    Status existingRapidClearanceStatus = attacker.Status.GetStatusByName(rapidClearanceAttackBoostStatusDef.EffectName);
                    Status existingBulletHellAPCostReduction = attacker.Status.GetStatusByName(_bulletHellAPCostReductionStatus.EffectName);

                    if (existingBulletHellStatus != null)
                    {
                        attacker.Status.UnapplyStatus(existingBulletHellStatus);
                    }

                    if(existingBulletHellAPCostReduction != null)
                    {
                        attacker.Status.UnapplyStatus(existingBulletHellAPCostReduction);
                    }


                    if (existingRapidClearanceStatus == null)
                    {
                        TFTVLogger.Always($"Applying Bullet Hell attack boost to {attacker.DisplayName} because no RC status present");
                        attacker.Status.ApplyStatus(_bulletHellAttackBoostStatus);
                    }
                   

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



        }

        internal class DesperateShot
        {


            [HarmonyPatch(typeof(ReloadAbility))]
            internal static class ReloadOthersReloadAbilityPatch
            {
                private static readonly MethodInfo GetReloadOthersWeaponTargetsMethod =
                    AccessTools.Method(
                        typeof(ReloadAbility),
                        "GetReloadOthersWeaponTargets",
                        new[] { typeof(TacticalTargetData), typeof(TacticalActorBase), typeof(Vector3) });

                private static readonly HashSet<TacticalItem> VirtualMagazines = new HashSet<TacticalItem>();

                [HarmonyPatch(nameof(ReloadAbility.GetTargets))]
                [HarmonyPostfix]
                private static void AllowReloadWithoutInventoryClips(
                    ReloadAbility __instance,
                    TacticalTargetData targetData,
                    TacticalActorBase sourceActor,
                    Vector3 sourcePosition,
                    ref IEnumerable<TacticalAbilityTarget> __result)
                {
                    if (__instance.ReloadAbilityDef != _ordnanceResupply)
                    {
                        return;
                    }

                    List<TacticalAbilityTarget> results = (__result ?? Enumerable.Empty<TacticalAbilityTarget>()).ToList();
                    HashSet<TacticalActorBase> actorsWithValidAmmo = results
                        .Where(t => t?.Actor != null && t?.TacticalItem?.CommonItemData?.CurrentCharges > 0)
                        .Select(t => t.Actor)
                        .ToHashSet();

                    List<TacticalAbilityTarget> candidateTargets = new List<TacticalAbilityTarget>();
                    candidateTargets.AddRange(InvokeReloadOthersWeaponTargets(
                        __instance,
                        targetData,
                        sourceActor,
                        sourcePosition));

                    foreach (TacticalAbilityTarget fallbackTarget in __instance.GetTargetActors(
                                 targetData,
                                 sourceActor,
                                 sourcePosition))
                    {
                        TacticalActorBase fallbackActor = fallbackTarget?.Actor ?? fallbackTarget?.GetTargetActor();
                        if (fallbackActor == null)
                        {
                            continue;
                        }

                        if (candidateTargets.Any(t => (t?.Actor ?? t?.GetTargetActor()) == fallbackActor))
                        {
                            continue;
                        }

                        candidateTargets.Add(fallbackTarget);
                    }

                    foreach (TacticalAbilityTarget candidate in candidateTargets)
                    {
                        TacticalActorBase actor = candidate?.Actor ?? candidate?.GetTargetActor();
                        if (actor == null || !actorsWithValidAmmo.Add(actor))
                        {
                            continue;
                        }

                        Weapon weapon = actor.AddonsManager?.RootAddon?.OfType<Weapon>()
                            .FirstOrDefault(w => !w.InfiniteCharges && w.CommonItemData.CurrentCharges < w.ChargesMax);

                        if (weapon == null)
                        {
                            continue;
                        }

                        TacticalAbilityTarget syntheticTarget = new TacticalAbilityTarget(candidate)
                        {
                            Equipment = weapon,
                            TacticalItem = CreateVirtualMagazine(weapon)
                        };

                        if (syntheticTarget.TacticalItem != null)
                        {
                            results.Add(syntheticTarget);
                        }
                    }

                    __result = results;
                }

                private static IEnumerable<TacticalAbilityTarget> InvokeReloadOthersWeaponTargets(
                    ReloadAbility instance,
                    TacticalTargetData targetData,
                    TacticalActorBase sourceActor,
                    Vector3 sourcePosition)
                {
                    if (GetReloadOthersWeaponTargetsMethod == null)
                    {
                        return Enumerable.Empty<TacticalAbilityTarget>();
                    }

                    object invocation = GetReloadOthersWeaponTargetsMethod.Invoke(
                        instance,
                        new object[] { targetData, sourceActor, sourcePosition });

                    return invocation as IEnumerable<TacticalAbilityTarget> ?? Enumerable.Empty<TacticalAbilityTarget>();
                }

                private static TacticalItem CreateVirtualMagazine(Weapon weapon)
                {
                    TacticalItemDef ammoDef = weapon?.WeaponDef?.CompatibleAmmunition?.FirstOrDefault();
                    if (ammoDef == null)
                    {
                        return null;
                    }

                    TacticalItem virtualClip = new TacticalItem();
                    virtualClip.Init(ammoDef, null);
                    VirtualMagazines.Add(virtualClip);
                    return virtualClip;
                }

                [HarmonyPatch(nameof(ReloadAbility.FractActionPointCost), MethodType.Getter)]
                [HarmonyPostfix]
                private static void EnsureActionPointCost(ReloadAbility __instance, ref float __result)
                {
                    if (__instance?.ReloadAbilityDef?.TargetingDataDef?.Origin?.TargetResult == TargetResult.Actor)
                    {
                        __result = __instance.ReloadAbilityDef.ActionPointCost;
                    }
                }

                [HarmonyPatch("Reload")]
                [HarmonyPostfix]
                private static void TopOffVirtualReloads(
                    ReloadAbility __instance,
                    Equipment equipment,
                    TacticalItem ammoClip)
                {
                    if (equipment == null || ammoClip == null)
                    {
                        return;
                    }

                    if (!VirtualMagazines.Remove(ammoClip))
                    {
                        return;
                    }

                    AmmoManager ammo = equipment.CommonItemData?.Ammo;
                    if (ammo == null)
                    {
                        return;
                    }

                    int missingCharges = equipment.ChargesMax - equipment.CommonItemData.CurrentCharges;
                    if (missingCharges <= 0)
                    {
                        return;
                    }

                    ammo.ReloadCharges(missingCharges, canCreateMagazines: true);

                    if (!equipment.TacticalActor.HasGameTag(OrdnanceResupplyTag))
                    {
                        equipment.TacticalActor.GameTags.Add(OrdnanceResupplyTag);
                    }
                }
            }


            //Allow activation even with insufficient AP (when our status says OK)
            [HarmonyPatch(typeof(TacticalAbility), "get_ActionPointRequirementSatisfied")]
            static class TacticalAbility_CanActivate_Desperate_Patch
            {
                static void Postfix(TacticalAbility __instance, ref bool __result)
                {
                    try
                    {
                        var actor = __instance?.TacticalActor;

                        if (__instance?.TacticalAbilityDef == _partingShot)
                        {
                            float fractCost = actor?.CharacterStats?.ActionPoints?.Max.EndValue * 0.25f ?? 0f;
                            float currentAp = actor?.CharacterStats?.ActionPoints?.Value.EndValue ?? 0f;

                            TFTVLogger.Always($"{actor.DisplayName} ap is {currentAp}, fractCost is {fractCost}");

                            if (currentAp > 0f && currentAp < fractCost)
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }

                            return;
                        }

                        if (__result) return; // already allowed

                        if (actor?.GetAbilityWithDef<PassiveModifierAbility>(_snapBrace) != null && __instance is DeployShieldAbility)
                        {
                            __result = true;
                            return;
                        }
                    }
                    catch (Exception e) { TFTVLogger.Always($"[TFTV] Desperate CanActivate patch failed: {e}"); }
                }



            }

            [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.Activate))]
            static class ShootAbility_Activate_PartingShot_Patch
            {
                static void Postfix(ShootAbility __instance)
                {
                    try
                    {
                        if (__instance?.TacticalAbilityDef != _partingShot)
                        {
                            return;
                        }

                        TacticalActor actor = __instance.TacticalActor;
                        if (actor?.CharacterStats?.ActionPoints != null)
                        {
                            actor.CharacterStats.ActionPoints.Set(0f);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Always($"[TFTV] Parting Shot AP clamp failed: {e}");
                    }
                }
            }
        }

        internal class MarkedWatch
        {


            [HarmonyPatch(typeof(TacticalLevelController), "TriggerOverwatch")]
            internal static class TacticalLevelController_TriggerOverwatch
            {
                static bool Prefix(TacticalLevelController __instance, TacticalActor target)
                {
                    try
                    {
                        if (target == null || !target.TacticalFaction.TacticalActors.Any(a => a.Status != null && a.Status.HasStatus(_markedwatchStatus)))
                        {
                            return true;
                        }




                        if (__instance.OverwatchTarget == null)
                        {
                            List<Status> listOfMarkedForOverwatch = (from actor in target.TacticalFaction.TacticalActors
                                                                     let status = actor.Status.GetStatusesByName(_markedwatchStatus.EffectName).FirstOrDefault()
                                                                     where status != null
                                                                     select status).ToList();


                            List<OverwatchStatus> listOfOverwatches = (from actor in __instance.Map.GetActors<TacticalActor>()
                                                                       let status = actor.Status.GetStatus<OverwatchStatus>()
                                                                       where status != null && !actor.DuringOwnTurn && actor.RelationTo(target) == FactionRelation.Enemy
                                                                       orderby (actor.Pos - target.Pos).sqrMagnitude
                                                                       select status).ToList();




                            if (target.Status.HasStatus(_markedwatchStatus) && listOfMarkedForOverwatch.Count() == 1)
                            {
                                // TFTVLogger.Always($"target.Status.HasStatus(_markedwatchStatus) && listOfMarkedForOverwatch.Count()==1");

                            }
                            else
                            {


                                List<TacticalActor> actorsWithMarkedForOverwatchTargets = (from actor in listOfMarkedForOverwatch
                                                                                           select actor.Source as TacticalActor).ToList();


                                if (!target.Status.HasStatus(_markedwatchStatus))
                                {

                                    listOfOverwatches.RemoveAll(s => actorsWithMarkedForOverwatchTargets.Contains(s.TacticalActor));
                                }
                                else
                                {
                                    TacStatus tacStatus = (TacStatus)target.Status.GetStatusesByName(_markedwatchStatus.EffectName).FirstOrDefault();
                                    TacticalActor sourceActor = tacStatus.Source as TacticalActor;
                                    if (sourceActor != null)
                                    {
                                        listOfOverwatches.RemoveAll(s => actorsWithMarkedForOverwatchTargets.Contains(s.TacticalActor) && s.TacticalActor != sourceActor);
                                    }
                                }
                            }

                            if (listOfOverwatches.Count > 0)
                            {
                                // TFTVLogger.Always($"before final check");

                                MethodInfo methodInfo = typeof(TacticalLevelController)
    .GetMethod("ExecuteOverwatch", BindingFlags.Instance | BindingFlags.NonPublic);

                                var enumerator = (IEnumerator<NextUpdate>)methodInfo.Invoke(
                                    __instance,
                                    new object[] { target, listOfOverwatches }
                                );

                                __instance.Timing.Start(enumerator, NextUpdate.ThisFrame);

                                //   TFTVLogger.Always($"final check cleared!");
                            }
                        }
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }
        }
        internal class DrawFire
        {



            [HarmonyPatch(typeof(AIUtil), nameof(AIUtil.GetEnemyWeight))]
            internal static class Patch_AIUtil_GetEnemyWeight
            {
                // Config
                private const float Multiplier = 100f;



                static void Postfix(AIBlackboard blackboard, TacticalActorBase enemy, ref float __result)
                {
                    try
                    {
                        if (HasTauntStatus(enemy))
                        {
                            TFTVLogger.Always($"{enemy?.DisplayName} initial score is {__result}");
                            __result *= Multiplier;
                            TFTVLogger.Always($"{enemy?.DisplayName} new score is {__result}");
                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }
            }

            private static bool HasTauntStatus(TacticalActorBase actor)
            {
                try
                {

                    if (actor.Status != null && actor.Status.HasStatus(_drawfireStatus))
                    {
                        TFTVLogger.Always($"{actor.DisplayName} has drawFireStatus! should be aggroed");
                        return true;

                    }

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }


            }
        }
        internal class MentorProtocol
        {
            public static bool CheckForMentorProtocolAbility(TacticalActor tacticalActor)
            {
                try
                {
                    return tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_mentorProtocol) != null;

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }

            }

        }
        internal class PounceProtocol
        {


        }



        internal class ViralPuppeteerToxicLink
        {


            [HarmonyPatch(typeof(TacticalAbility), "TargetFilterPredicate")]
            internal static class TacticalAbility_TargetFilterPredicate_Postfix
            {
                static void Postfix(
                    TacticalAbility __instance,
                    TacticalTargetData targetData,
                    TacticalActorBase sourceActor,
                    Vector3 sourcePosition,
                    TacticalActorBase targetActor,
                    Vector3 targetPosition,
                    ref bool __result)
                {
                    try
                    {
                        // If already valid, don’t touch it.
                        if (__result) return;

                        // Ability defs we care about
                        var mindControlAbilityDef = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");
                        var inducePanicAbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("InducePanic_AbilityDef");
                        var parasychosisAbilityDef = DefCache.GetDef<ApplyEffectAbilityDef>("Parasychosis_AbilityDef");

                        // Status defs / ability mods
                        var poisonStatusDef = DefCache.GetDef<DamageOverTimeStatusDef>("Poison_DamageOverTimeStatusDef");

                        var def = __instance != null ? __instance.TacticalAbilityDef : null;
                        if (def == null) return;

                        // Only proceed if we're on one of the three abilities
                        bool isMindOrPanic = (def == mindControlAbilityDef || def == inducePanicAbilityDef);
                        bool isParasychosis = (def == parasychosisAbilityDef);
                        if (!isMindOrPanic && !isParasychosis) return;

                        // --- Combo gates ---
                        bool comboA =
                            isMindOrPanic &&
                            HasInfected(targetActor) &&
                            HasPassive(sourceActor, _viralPuppeteer);

                        bool comboB =
                            isParasychosis &&
                            HasPoison(targetActor, poisonStatusDef) &&
                            HasPassive(sourceActor, _toxicLink);

                        if (!comboA && !comboB) return;

                        // Re-run the non-range/LOS parts of the predicate. If any of these fail, bail.
                        // 1) Interactable gate
                        if (!def.UsableOnNonInteractableActor && !(targetActor != null && targetActor.Interactable)) return;

                        // 2) Cull tags
                        if (targetData.CullTargetTags.Any() && targetActor.HasGameTags(targetData.CullTargetTags, false)) return;

                        // 3) Self-targeting rules
                        bool isSelf = sourceActor == targetActor;
                        if (isSelf && !targetData.TargetSelf) return;

                        // 4) Require target tags if any are specified
                        if (targetData.TargetTags.Any() && !targetActor.HasGameTags(targetData.TargetTags, false)) return;

                        // 5) Friend/Neutral/Enemy filter
                        bool friendOk = targetData.TargetFriendlies && sourceActor.RelationTo(targetActor) == FactionRelation.Friend;
                        bool neutralOk = targetData.TargetNeutrals && sourceActor.RelationTo(targetActor) == FactionRelation.Neutral;
                        bool enemyOk = targetData.TargetEnemies && sourceActor.RelationTo(targetActor) == FactionRelation.Enemy;
                        if (!friendOk && !neutralOk && !enemyOk) return;

                        // 6) Faction-visibility (NOT line-of-sight) check — preserve original intent
                        bool isRevealed = sourceActor.TacticalFaction.Vision.IsRevealed(targetActor);

                        // Ignore/Any -> leave as true

                        if (!isRevealed) return;

                        // If we reached here, the only reasons it could have failed are range or LOS.
                        // Grant an exception: ignore MinRange/MaxRange and LOS for this specific target.
                        __result = true;
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                    }
                }

                // --- Helpers ---

                // A: target must be infected
                private static bool HasInfected(TacticalActorBase actor)
                {
                    if (actor == null || actor.Status == null) return false;
                    // Original API used in your code:
                    return actor.Status.HasStatus<InfectedStatus>();
                }

                // B: target must be poisoned (by specific Poison DoT def)
                private static bool HasPoison(TacticalActorBase actor, DamageOverTimeStatusDef poisonDef)
                {
                    if (actor == null || actor.Status == null || poisonDef == null) return false;


                    // If this call exists in your version, it will work and is fastest.
                    return actor.Status.HasStatus(poisonDef);

                }

                // C: source must have the given passive ability def (works for Viral Puppeteer / Toxic Link)
                private static bool HasPassive(TacticalActorBase actor, TacticalAbilityDef passiveDef)
                {
                    if (actor == null || passiveDef == null) return false;

                    // Most passives of this type derive from PassiveModifierAbility.
                    var a = actor.GetAbilityWithDef<PassiveModifierAbility>(passiveDef);
                    return a != null;
                }
            }

        }

        internal class VirulentGrip
        {
            public static bool CheckForVirulentGripAbility(TacticalActor controllerActor, TacticalActor controlledActor)
            {
                try
                {
                    return controllerActor.GetAbilityWithDef<PassiveModifierAbility>(_virulentGrip) != null && controlledActor.Status.HasStatus<InfectedStatus>();

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

        }

        internal class ShieldedRiposte
        {
            private static bool _shieldRiposteDeployingShield = false;

            [HarmonyPatch(typeof(DeployShieldAbility), "Activate")]
            public static class Patch_DeployShieldAbility_Activate
            {
                public static void Postfix(DeployShieldAbility __instance)
                {
                    try
                    {
                        TacticalActor tacticalActor = __instance.TacticalActor;

                        if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_shieldedRiposte) != null)
                        {
                            _shieldRiposteDeployingShield = true;

                            TFTVLogger.Always($"tacticalActor?.DisplayName: {tacticalActor?.DisplayName} deploying shield, deployed status? " +
                                $"{tacticalActor.Status.HasStatus<ShieldDeployedStatus>()}");

                            List<Weapon> weapons = new List<Weapon>(tacticalActor.Equipments.GetWeapons().Where(
                            w => w.IsUsable && w.HasCharges && w.TacticalItemDef.Tags.Contains(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"))
                            && !w.WeaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("SpitterWeapon_TagDef"))
                            ));

                            if (weapons.Count == 0)
                            {
                                return;
                            }

                            Weapon bestWeapon = weapons.OrderByDescending(w => w.WeaponDef.EffectiveRange).ToList().First();

                            if (tacticalActor.Equipments.SelectedWeapon == null || tacticalActor.Equipments.SelectedWeapon != bestWeapon)
                            {
                                TFTVLogger.Always($"Getting ready for shield riposte {tacticalActor.name} was holding {tacticalActor.Equipments?.SelectedWeapon?.DisplayName}, switching to {bestWeapon.DisplayName}");
                                tacticalActor.Equipments.SetSelectedEquipment(bestWeapon);
                            }

                            _shieldRiposteDeployingShield = false;

                            // TFTVLogger.Always($"at the end, shield now deployed? {tacticalActor.Status.HasStatus<ShieldDeployedStatus>()}");

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw; // Run original
                    }
                }
            }

            [HarmonyPatch(typeof(EquipmentComponent), "SetSelectedEquipment")]
            public static class EquipmentComponent_SetSelectedEquipment_Patch
            {
                static bool IsRiotShield(Equipment equipment) =>
                    equipment != null && equipment.EquipmentDef == DefCache.GetDef<EquipmentDef>("FS_RiotShield_WeaponDef");

                static bool IsShieldDeployed(TacticalActor actor) =>
                    actor?.Status != null && actor.Status.HasStatus<ShieldDeployedStatus>();

                public static bool Prefix(EquipmentComponent __instance, Equipment equipment)
                {
                    var actor = __instance.TacticalActor;
                    var prev = __instance.SelectedEquipment;

                    //   TFTVLogger.Always($"Set selected equipment for {actor?.DisplayName}. Currently selected equipment: {prev?.ItemDef?.name}. Shield deployed? {_shieldRiposteDeployingShield}");

                    // Only intercept when switching away from a deployed riot shield
                    if (!(IsRiotShield(prev) && _shieldRiposteDeployingShield))
                        return true;

                    // TFTVLogger.Always($"passed the check; shield deployed and was wielding it");

                    // 1) Set SelectedEquipment via the property (non-public setter)
                    var selProp = AccessTools.Property(typeof(EquipmentComponent), "SelectedEquipment");
                    selProp?.SetValue(__instance, equipment, null);

                    // Unwire previous selection
                    if (prev != null)
                    {
                        var handlerMethod = AccessTools.Method(typeof(EquipmentComponent), "SelectedEquipmentIsDisabled");
                        var handler = (DamageReceiverImplementation.DamageReceiverStatusChanged)
                            Delegate.CreateDelegate(typeof(DamageReceiverImplementation.DamageReceiverStatusChanged), __instance, handlerMethod);

                        prev.DamageImplementation.ReachedZeroHealth -= handler;
                        prev.SetSelected(selected: false);

                        //TFTVLogger.Always($"Unwired previous equipment {prev.ItemDef.name}");


                    }

                    // Wire new selection
                    if (equipment != null)
                    {
                        var handlerMethod = AccessTools.Method(typeof(EquipmentComponent), "SelectedEquipmentIsDisabled");
                        var handler = (DamageReceiverImplementation.DamageReceiverStatusChanged)
                            Delegate.CreateDelegate(typeof(DamageReceiverImplementation.DamageReceiverStatusChanged), __instance, handlerMethod);

                        equipment.DamageImplementation.ReachedZeroHealth += handler;
                        equipment.SetSelected(selected: true);

                        // TFTVLogger.Always($"Wired new equipment {equipment.ItemDef.name}");
                    }

                    // DrawOut as normal if needed
                    if (equipment != null && equipment.HolsterSlot != null && equipment.EquipmentDef.HolsterWhenNotSelected)
                    {
                        AccessTools.Method(typeof(EquipmentComponent), "DrawOut")
                            ?.Invoke(__instance, new object[] { new AnimationEvent() });

                        TFTVLogger.Always($"Drew out new equipment {equipment.ItemDef.name}");
                    }

                    // 2) Invoke the event by fetching the PRIVATE backing field
                    //    (field-like events compile to a private field with the same name)
                    var evtField = AccessTools.Field(typeof(EquipmentComponent), "EquipmentChangedEvent");
                    var evt = (EquipmentComponent.EquipmentChangedHandler)evtField?.GetValue(__instance);

                    //    TFTVLogger.Always($"Invoking EquipmentChangedEvent, evtField==null: {evtField == null} evt==null: {evt == null}");

                    evt?.Invoke(equipment);

                    return false; // skip original
                }
            }


        }

        internal class MightMakesRight
        {

            public static float CheckForMightMakesRightDrill(TacticalActor tacticalActor)
            {
                try
                {
                    if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_mightMakesRight) != null)
                    {
                        return 1f + tacticalActor.CharacterStats.Endurance.Value.EndValue / 2 / 100;
                    }

                    return 0;

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

        }

        /*  internal class VeiledMarksman
          {
              [HarmonyPatch(typeof(ApplyStatusAbility), "OnActorMovedInNewTile")]
              public static class Patch_ApplyStatusAbility_OnActorMovedInNewTile
              {
                  public static bool Prefix(ApplyStatusAbility __instance, TacticalActorBase movedActor)
                  {
                      try
                      {
                          if (__instance.TacticalAbilityDef != _veiledMarksman) return true;

                          if (movedActor != __instance.TacticalActor) return false;

                          TacticalActor tacticalActor = __instance.TacticalActor;




                          IdleAbility idleAbility = tacticalActor.IdleAbility;
                          CoverType? coverType = (idleAbility != null) ? new CoverType?(idleAbility.ActivePose.CoverInfo.CoverType) : null;

                          TFTVLogger.Always($"{tacticalActor?.DisplayName} moving to a new tile. cover null: {coverType==null}");

                          if (coverType == null || coverType ==CoverType.None)
                          {
                              TFTVLogger.Always($"the position is not in cover");

                              Status status = tacticalActor.Status.GetStatusByName(_veiledMarksman.StatusDef.EffectName);
                              if (status != null)
                              {
                                  TFTVLogger.Always($"removing veiledMarksman status from {tacticalActor?.DisplayName}");
                                  movedActor.Status.UnapplyStatus(status);
                              }

                          }
                          else
                          {


                              TFTVLogger.Always($"the position is in cover");

                              if (tacticalActor.Status.GetStatusByName(_veiledMarksman.StatusDef.EffectName) == null)
                              {
                                  TFTVLogger.Always($"adding veiledMarksman status to {tacticalActor?.DisplayName}");
                                  movedActor.Status.ApplyStatus(_veiledMarksman.StatusDef);
                              }

                          }

                          return false;

                      }
                      catch (Exception ex)
                      {
                          TFTVLogger.Error(ex);
                          throw;
                      }
                  }

                  private static bool TryGetGroundPoint(TacticalActor actor, Vector3 approxPos, out Vector3 ground)
                  {
                      // Same pattern used elsewhere in the game (see IsValidStepOutPos):
                      var cast = actor.TacticalPerception.TacMap.CastFirstFloorAt(
                          approxPos + Vector3.up * actor.TacticalPerception.Height * 0.5f,
                          actor.TacticalNav.FloorLayers);

                      if (cast.HitIsValid)
                      {
                          ground = cast.Point;
                          return true;
                      }

                      ground = approxPos; // fall back
                      return false;
                  }

                  // Most robust “am I in ANY cover?” probe:
                  private static CoverType GetCoverAtTileRobust(TacticalActor actor, bool existingOnly = true)
                  {
                      var tp = actor.TacticalPerception;
                      if (tp == null || !tp.UsesCovers) return CoverType.None;

                      Vector3 pos = actor.Pos;

                      // 1) Snap to floor to avoid voxel/height tolerance issues
                      if (!TryGetGroundPoint(actor, pos, out var ground)) ground = pos;

                      // 2) Use the engine helper that inspects cover in the proper directions
                      //    (this is what many internal systems rely on).
                      var around = tp.TacMap.GetCoversAround(ground, tp.Height, existingOnly);

                      bool sawLow = false;
                      foreach (var c in around)
                      {
                          if (c.CoverType == CoverType.High) return CoverType.High;
                          if (c.CoverType == CoverType.Low) sawLow = true;
                      }

                      // 3) If nothing came back, do a defensive second-pass:
                      //    small back-off + orthogonal rays (handles being flush with the wall)
                      if (!sawLow)
                      {
                          const float EPS = 0.12f; // small local nudge (meters)
                          foreach (var dir in TacticalMap.OrthogonalGridDirections)
                          {
                              // back off slightly so the ray won’t start inside the collider
                              Vector3 sample = ground - dir.normalized * EPS;

                              var info = tp.TacMap.GetCoverInfoInDirection(sample, dir, tp.Height);

                              TFTVLogger.Always($"dir: {dir} cover: {info.CoverType} at pos {actor.Pos}");

                              if (info.CoverType == CoverType.High) return CoverType.High;
                              if (info.CoverType == CoverType.Low) sawLow = true;
                          }
                      }

                      return sawLow ? CoverType.Low : CoverType.None;
                  }



              }
          }*/

        internal class PackLoyalty
        {
            public static void CheckForPackLoyaltyDrill(TacticalLevelController controller)
            {
                try
                {




                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }

            }


        }

    }

}



















