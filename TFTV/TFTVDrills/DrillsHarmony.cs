using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.PathProcessors;
using PhoenixPoint.Tactical.UI.Abilities;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TFTV.TFTVDrills.DrillsDefs;
using static TFTV.TFTVDrills.DrillsPublicClasses;



namespace TFTV.TFTVDrills
{
    internal class DrillsHarmony
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        internal class Mutoids
        {
            [HarmonyPatch(typeof(GeoLevelController), nameof(GeoLevelController.CreateCharacterFromDescriptor))]
            internal static class GeoLevelController_CreateCharacterFromDescriptor_Patch
            {
                private static void Postfix(GeoLevelController __instance, GeoCharacter __result)
                {
                    //   TFTVLogger.Always($"GeoLevelController_CreateCharacterFromDescriptor: {__result.IsMutoid}");

                    if (TFTVNewGameOptions.IsReworkEnabled())
                    {
                        if (__result.IsMutoid)
                        {
                            var stamina = __result.Fatigue?.Stamina;
                            if (stamina != null && stamina.Value > 0f)
                            {
                                stamina.Set(0f, true);
                                // TFTVLogger.Always($"Stamina should be set 0 for new Mutoid");
                            }
                        }
                    }
                }
            }

        }

        internal static class ShockDrop
        {
            [HarmonyPatch(typeof(AbilitySummaryData), "ProcessDamageTypeFlowPayload")] //VERIFIED
            public static class AbilitySummaryData_ProcessDamageTypeFlowPayload_Patch
            {
                // Prefix replicates original implementation and prevents original from running.
                [HarmonyPrefix]
                public static bool Prefix(AbilitySummaryData __instance, TacticalActor tacticalActor, DamagePayload payload, int numActions)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return true;
                    }



                    if (payload == null)
                    {
                        return false; // nothing to do, skip original
                    }

                    if (tacticalActor.GetAbilityWithDef<BashAbility>(_shockDropBash) == null)
                    {
                        return true; // not our special bash, let original run
                    }

                    DamageEffectDef damageEffectDef = payload.TryGetDamageEffectDef();
                    if (damageEffectDef != null && damageEffectDef.DamageTypeDef != null)
                    {
                        KeywordData keywordData = new KeywordData
                        {
                            ViewElementDef = damageEffectDef.DamageTypeDef.Visuals,
                            Value = damageEffectDef.MaximumDamage,
                            NumActions = numActions
                        };
                        __instance.Keywords.Add(keywordData);

                        if (damageEffectDef is MeleeBashDamageEffectDef meleeBashDamageEffectDef)
                        {
                            // Recalculate bash damage value using actor and equipment
                            try
                            {
                                keywordData.Value = MeleeBashDamageEffect.GetDamageValue(
                                    tacticalActor,
                                    tacticalActor?.Equipments?.SelectedEquipment,
                                    meleeBashDamageEffectDef.EnduranceToDamageCoefficient,
                                    meleeBashDamageEffectDef.MeleeWeaponTagDef
                                );
                            }
                            catch
                            {
                                // If anything goes wrong during calculation, keep the original MaximumDamage value.
                            }

                            KeywordData item = new KeywordData
                            {
                                ViewElementDef = meleeBashDamageEffectDef.SecondaryDamageTypeDef.Visuals,
                                Value = keywordData.Value + _shockDropStatus.DamageKeywordPairs[0].Value,
                                NumActions = numActions
                            };
                            __instance.Keywords.Add(item);
                        }
                    }

                    // Return false to skip the original method (we have replicated it)
                    return false;
                }
            }

            [HarmonyPatch(typeof(BashAbility), nameof(BashAbility.GetDamage))]
            internal static class BashAbilityShockPatch
            {
                private static readonly BashAbilityDef SpecialBashAbilityDef = _shockDropBash;

                private static void Postfix(
                    BashAbility __instance,
                    ref float __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }



                    if (__instance.BashAbilityDef == SpecialBashAbilityDef)
                    {
                        __result += _shockDropStatus.DamageKeywordPairs[0].Value;
                    }
                }

            }


            [HarmonyPatch(typeof(JetJumpAbility), nameof(JetJumpAbility.Activate))]
            public static class JetJumpAbility_Activate_ShockDrop_Patch
            {
                public static void Postfix(JetJumpAbility __instance)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }



                    try
                    {
                        if (_shockDrop == null || _shockDropStatus == null)
                        {
                            return;
                        }

                        TacticalActor actor = __instance?.TacticalActor;
                        if (actor == null || actor.Status == null)
                        {
                            return;
                        }

                        if (actor.GetAbilityWithDef<PassiveModifierAbility>(_shockDrop) == null)
                        {
                            return;
                        }

                        ShockDropStatus existingStatus = actor.Status.GetStatus<ShockDropStatus>(_shockDropStatus);
                        if (existingStatus != null)
                        {
                            actor.Status.UnapplyStatus(existingStatus);
                        }

                        actor.Status.ApplyStatus(_shockDropStatus);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal class OneHandedGrip
        {
            /// <summary>
            /// Provides shared helpers for attaching and detaching the one-handed accuracy penalty ability.
            /// The mod that defines the actual ability can set <see cref="OneHandedGrip"/> at runtime before
            /// the patches execute (for example during mod initialization).
            /// </summary>
            public static class OneHandedPenaltyAbilityManager
            {
                /// <summary>
                /// Gets or sets the tactical ability definition that should be granted while a pawn has exactly one disabled hand.
                /// This should be assigned by the mod bootstrap code once the ability definition is available.
                /// </summary>
                public static PassiveModifierAbilityDef OneHandedGrip { get; set; }
                public static StanceStatusDef OneHandedGripAccPenalty { get; set; }
                /// <summary>
                /// Internal token used as the source when adding the accuracy penalty ability.
                /// </summary>
                private static readonly object AbilitySource = new object();

                /// <summary>
                /// Adds the configured ability to the actor if needed.
                /// </summary>
                /// <param name="status">The status that triggered the check.</param>
                public static void TryAddStatus(TacticalActor tacticalActor)
                {


                    TFTVLogger.Always($"running TryAddAbility for {tacticalActor.DisplayName}");

                    if (tacticalActor.HasStatus(OneHandedGripAccPenalty))
                    {
                        return;
                    }

                    tacticalActor.Status.ApplyStatus(OneHandedGripAccPenalty);
                    TFTVLogger.Always($"{tacticalActor.DisplayName} has accpenalty status (should)? " +
                        $"{tacticalActor.HasStatus(OneHandedGripAccPenalty)}");
                }

                /// <summary>
                /// Removes the configured ability from the actor when no remaining statuses require it.
                /// </summary>
                /// <param name="status">The status that triggered the check.</param>
                public static void TryRemoveStatus(TacticalActor tacticalActor)
                {
                    try
                    {

                        TFTVLogger.Always($"running TryRemoveStatus for {tacticalActor.DisplayName}");


                        Status accPenaltyStatus = tacticalActor.Status.GetStatusesByName(OneHandedGripAccPenalty.EffectName).FirstOrDefault();

                        tacticalActor.Status.UnapplyStatus(accPenaltyStatus);
                        TFTVLogger.Always($"{tacticalActor.DisplayName} has accpenalty status (should not)? " +
                            $"{tacticalActor.HasStatus(OneHandedGripAccPenalty)}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }
            }


            [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.RecalculateUsableHands))]
            public static class TacticalActor_RecalculateUsableHands_Patch
            {
                public static bool Prefix(TacticalActor __instance, ref int ____usableHands)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return true;
                        }


                        if (__instance.GetAbilityWithDef<PassiveModifierAbility>(OneHandedPenaltyAbilityManager.OneHandedGrip) == null)
                        {
                            return true;
                        }

                        int num = 0;

                        foreach (UnusableHandStatus status in __instance.Status.GetStatuses<UnusableHandStatus>())
                        {
                            num += status.HandsDisabled;
                        }

                        int num2 = 0;
                        bool providesHandsIfDisabled = __instance.Status.HasStatus<FreezeAspectStatsStatus>();

                        foreach (ItemSlot slot in __instance.BodyState.GetSlots())
                        {
                            num2 += slot.GetHandsProvided(providesHandsIfDisabled);
                        }

                        if (num2 == 1)
                        {
                            TFTVLogger.Always($"{__instance.DisplayName} has 1 hand enabled and the OneHandedGrip ability, so number of usable hands should increase to 2");
                            num2++;
                            OneHandedPenaltyAbilityManager.TryAddStatus(__instance);

                        }
                        else if (num2 == 2 && __instance.HasStatus(OneHandedPenaltyAbilityManager.OneHandedGripAccPenalty))
                        {
                            TFTVLogger.Always($"{__instance.DisplayName} has 2 hand enabled and still has the acc penalty, so going to remove it");
                            OneHandedPenaltyAbilityManager.TryRemoveStatus(__instance);

                        }


                        int usableHands = ____usableHands;
                        ____usableHands = num2 - num;

                        if (____usableHands < 0)
                        {
                            ____usableHands = 0;
                        }

                        if (____usableHands < usableHands)
                        {
                            __instance.TacticalLevel.ActorUsableHandsDecreased(__instance);
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



        }

        internal static class NeuralLink
        {
            private const string PhoenixCommandName = "px";

            [HarmonyPatch(typeof(StatusComponent), nameof(StatusComponent.ApplyStatus), typeof(Status))]
            private static class StatusComponent_ApplyStatus_CommandOverlayPatch
            {
                public static void Postfix(StatusComponent __instance, Status status)
                {

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }


                    try
                    {
                        TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();


                        if (tacticalLevelController == null || !tacticalLevelController.TurnIsPlaying)
                        {
                            return;
                        }

                        if (status?.Def == _commandOverlayStatus)
                        {
                            RefreshNeuralLinkStatus();
                        }
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(StatusComponent), nameof(StatusComponent.UnapplyStatus), typeof(Status))]
            private static class StatusComponent_UnapplyStatus_CommandOverlayPatch
            {
                public static void Postfix(StatusComponent __instance, Status status)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return;
                        }

                        TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                        if (tacticalLevelController == null || !tacticalLevelController.TurnIsPlaying)
                        {
                            return;
                        }


                        if (status?.Def == _commandOverlayStatus)
                        {
                            RefreshNeuralLinkStatus();
                        }
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }
            }

            internal static void RefreshNeuralLinkStatus()
            {
                try
                {

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (controller == null || _neuralLink == null || _neuralLinkControlStatus == null || _commandOverlayStatus == null || _remoteControlAbilityDef == null)
                    {
                        return;
                    }

                    TacticalFaction phoenixFaction = controller.GetFactionByCommandName(PhoenixCommandName);
                    if (phoenixFaction == null)
                    {
                        return;
                    }

                    bool hasCommandOverlay = phoenixFaction.TacticalActors.Any(HasNeuralLinkAbility);

                    foreach (TacticalActor actor in phoenixFaction.TacticalActors)
                    {
                        if (actor?.Status == null || actor.TacticalActorDef != DefCache.GetDef<TacticalActorDef>("Soldier_ActorDef"))
                        {
                            continue;
                        }

                        AddAbilityStatus existingStatus = actor.Status.GetStatus<AddAbilityStatus>(_neuralLinkControlStatus);

                        if (!actor.IsAlive || actor.IsEvacuated)
                        {
                            if (existingStatus != null)
                            {
                                actor.Status.UnapplyStatus(existingStatus);
                            }
                            continue;
                        }

                        bool shouldHave = hasCommandOverlay
                            && actor.Status.HasStatus(_commandOverlayStatus)
                            && actor.GetAbilityWithDef<TacticalAbility>(_remoteControlAbilityDef) == null;

                        // TFTVLogger.Always($"{actor?.DisplayName} has {actor.Status.HasStatus(_commandOverlayStatus)}  {actor.GetAbilityWithDef<TacticalAbility>(_remoteControlAbilityDef) == null}");

                        if (shouldHave)
                        {
                            if (existingStatus == null)
                            {
                                // TFTVLogger.Always($"{actor?.DisplayName} should get neuralLinkControlStatus");
                                actor.Status.ApplyStatus(_neuralLinkControlStatus);
                            }
                        }
                        else if (existingStatus != null)
                        {
                            // TFTVLogger.Always($"{actor?.DisplayName} should lose neuralLinkControlStatus");
                            actor.Status.UnapplyStatus(existingStatus);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }

            }

            private static bool HasNeuralLinkAbility(TacticalActor actor)
            {
                return actor != null
                    && actor.IsAlive
                    && !actor.IsEvacuated
                    && actor.GetAbilityWithDef<PassiveModifierAbility>(_neuralLink) != null;
            }
        }

        internal class BulletHell
        {
            private static readonly GameTagDef AssaultRifleTag = DefCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef");


            public static void CheckForBulletHellDrillAplicationOnDisabledLimb(TacticalActor tacticalActor, TacticalItemAspectBase aspect) 
            {

                try
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }


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

                    TacticalActor attacker = TacUtil.GetSourceTacticalActorBase(tacticalActor.LastDamageSource) as TacticalActor;

                    TacticalActor selectedActor = tacticalActor?.TacticalLevel?.View?.SelectedActor;

                    TFTVLogger.Always($"attacker?.DisplayName: {attacker?.DisplayName} selectedActor?.DisplayName: {selectedActor?.DisplayName}");

                    if (attacker == null || selectedActor == null || attacker != selectedActor)
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

                    if (existingBulletHellAPCostReduction != null)
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

            [HarmonyPatch(typeof(EquipmentComponent))]
            internal static class EquipmentComponentPatches
            {
                [HarmonyPatch(nameof(EquipmentComponent.SetSelectedEquipment))]
                [HarmonyPrefix]
                private static bool PreventSwitchingWhenLocked(EquipmentComponent __instance, Equipment equipment)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return true;
                    }


                    TacticalActor actor = __instance.TacticalActor;
                    if (actor != null && equipment != null && equipment != __instance.SelectedEquipment && actor.Status.HasStatus(_bulletHellSlowStatus))
                    {
                        return false;
                    }

                    return true;
                }
            }

            [HarmonyPatch(typeof(TacticalView))]
            internal static class TacticalViewPatches
            {
                [HarmonyPatch(nameof(TacticalView.CanSwitchEquipment))]
                [HarmonyPostfix]
                private static void BlockUiSwitchingWhenLocked(TacticalActor actor, ref bool __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }


                    if (__result && actor != null && actor.Status.HasStatus(_bulletHellSlowStatus))
                    {
                        __result = false;
                    }
                }
            }

            [HarmonyPatch(typeof(ReloadAbility))]
            internal static class ReloadAbilityPatches
            {
                [HarmonyPatch("GetDisabledStateInternal")] //VERIFIED
                [HarmonyPostfix]
                private static void BlockReloadingWhenLocked(ReloadAbility __instance, ref AbilityDisabledState __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }



                    if (__result == AbilityDisabledState.NotDisabled && __instance?.TacticalActor?.Status != null && __instance.TacticalActor.Status.HasStatus(_bulletHellSlowStatus))
                    {
                        __result = AbilityDisabledState.BlockedByStatus;
                    }
                }
            }

        }

        internal class OrdenanceResupply
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
                    if (!TFTVNewGameOptions.IsReworkEnabled() || __instance.ReloadAbilityDef != _ordnanceResupply)
                    {
                        return;
                    }

                    List<TacticalAbilityTarget> results = (__result ?? Enumerable.Empty<TacticalAbilityTarget>()).ToList();
                    HashSet<Weapon> weaponsWithValidAmmo = new HashSet<Weapon>(
     results
         .Select(t => t?.Equipment as Weapon)
         .Where(w => w != null && w.CommonItemData?.CurrentCharges > 0)
 );

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
                        if (actor == null)
                        {
                            continue;
                        }

                        foreach (Weapon weapon in GetReloadableWeapons(actor))
                        {
                            if (!weaponsWithValidAmmo.Add(weapon))
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
                    }

                    __result = results;
                }

                private static IEnumerable<Weapon> GetReloadableWeapons(TacticalActorBase actor)
                {
                    IEnumerable<Weapon> weapons = Enumerable.Empty<Weapon>();

                    if (actor is TacticalActor tacticalActor && tacticalActor.Equipments != null)
                    {
                        weapons = tacticalActor.Equipments.Equipments.OfType<Weapon>();
                    }
                    else if (actor?.AddonsManager?.RootAddon != null)
                    {
                        weapons = actor.AddonsManager.RootAddon.OfType<Weapon>();
                    }

                    return weapons.Where(w => w != null && !w.InfiniteCharges && w.CommonItemData.CurrentCharges < w.ChargesMax);
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
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }


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
                    if (!TFTVNewGameOptions.IsReworkEnabled() || equipment == null || ammoClip == null)
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
                    if (missingCharges > 0)
                    {
                        ammo.ReloadCharges(missingCharges, canCreateMagazines: true);
                    }

                    TacticalActorBase owner = equipment.TacticalActorBase;
                    if (owner == null || !owner.HasGameTag(owner.SharedData.SharedGameTags.VehicleTag))
                    {
                        return;
                    }

                    foreach (Weapon otherWeapon in GetReloadableWeapons(owner))
                    {
                        if (otherWeapon == equipment)
                        {
                            continue;
                        }

                        AmmoManager otherAmmo = otherWeapon.CommonItemData?.Ammo;
                        int otherMissingCharges = otherWeapon.ChargesMax - otherWeapon.CommonItemData.CurrentCharges;
                        if (otherAmmo != null && otherMissingCharges > 0)
                        {
                            otherAmmo.ReloadCharges(otherMissingCharges, canCreateMagazines: true);
                        }
                    }

                    if (!owner.GameTags.Contains(OrdnanceResupplyTag))
                    {
                        owner.GameTags.Add(OrdnanceResupplyTag);
                    }
                }

            }

        }
        internal class DesperateShot
        {

            //Allow activation even with insufficient AP (when our status says OK)
            [HarmonyPatch(typeof(TacticalAbility), "get_ActionPointRequirementSatisfied")] //VERIFIED
            static class TacticalAbility_CanActivate_Desperate_Patch
            {


                static void Postfix(TacticalAbility __instance, ref bool __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }


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

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }

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


        //LOOKING FOR NULL
       /* internal class MarkedWatch
        {
            [HarmonyPatch]
            internal static class MarkedWatchOverwatchAccuracyPatch
            {

                private const string MarkedWatchEffectName = "markedwatch";
                private const float AccuracyBuff = 0.5f;

                private static readonly ConditionalWeakTable<ShootAbility, AppliedAccuracyModifier> ActiveAccuracyModifiers = new ConditionalWeakTable<ShootAbility, AppliedAccuracyModifier>();

                [HarmonyPatch(typeof(ShootAbility), nameof(ShootAbility.Activate))]
                private static class ShootAbility_Activate_MarkedWatchAccuracy
                {
                    public static void Postfix(ShootAbility __instance, object parameter)
                    {
                        try
                        {
                            ApplyAccuracyBonus(__instance, parameter);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            RemoveAccuracyBonus(__instance);
                        }
                    }
                }

                [HarmonyPatch(typeof(ShootAbility), "OnPlayingActionEnd")]
                private static class ShootAbility_OnPlayingActionEnd_MarkedWatchAccuracy
                {
                    public static void Prefix(ShootAbility __instance)
                    {
                        try
                        {
                            RemoveAccuracyBonus(__instance);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }

                private static void ApplyAccuracyBonus(ShootAbility ability, object parameter)
                {
                    if (ability == null || ability.TacticalActor == null)
                    {
                        return;
                    }

                    TacticalAbilityTarget tacticalAbilityTarget = parameter as TacticalAbilityTarget;
                    if (tacticalAbilityTarget == null || tacticalAbilityTarget.AttackType != AttackType.Overwatch)
                    {
                        return;
                    }

                    TacticalActor targetActor = tacticalAbilityTarget.Actor as TacticalActor;
                    if (targetActor?.Status == null)
                    {
                        return;
                    }

#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
                    TacStatus markedStatus = targetActor.Status
                        .GetStatusesByName(MarkedWatchEffectName)
                        .OfType<TacStatus>()
                        .FirstOrDefault(status => status.Source == ability.TacticalActor);
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast

                    if (markedStatus == null)
                    {
                        return;
                    }

                    BaseStat accuracyStat = ability.TacticalActor.CharacterStats?.TryGetStat(StatModificationTarget.Accuracy);
                    if (accuracyStat == null)
                    {
                        return;
                    }

                    RemoveAccuracyBonus(ability);

                    StatModification modifier = new StatModification(StatModificationType.Add, accuracyStat.Name, AccuracyBuff, ability, 0f);
                    accuracyStat.AddStatModification(modifier, true);

                    ActiveAccuracyModifiers.Add(ability, new AppliedAccuracyModifier
                    {
                        Modification = modifier,
                        BonusAmount = AccuracyBuff
                    });
                }

                private static void RemoveAccuracyBonus(ShootAbility ability)
                {
                    if (ability == null)
                    {
                        return;
                    }

                    if (ActiveAccuracyModifiers.TryGetValue(ability, out AppliedAccuracyModifier appliedModifier))
                    {
                        BaseStat accuracyStat = ability.TacticalActor?.CharacterStats?.TryGetStat(StatModificationTarget.Accuracy);
                        if (accuracyStat != null)
                        {
                            accuracyStat.RemoveStatModification(appliedModifier.Modification, true);
                        }

                        ActiveAccuracyModifiers.Remove(ability);
                    }
                }

                private sealed class AppliedAccuracyModifier
                {
                    public StatModification Modification;
                    public float BonusAmount;
                }
            }

            [HarmonyPatch(typeof(TacticalLevelController), "TriggerOverwatch")]
            internal static class TacticalLevelController_TriggerOverwatch
            {
                static bool Prefix(TacticalLevelController __instance, TacticalActor target)
                {

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return true;
                    }

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
        }*/
        internal class DrawFire
        {
            private const float Multiplier = 100f;

            private static readonly Stack<TacAIActor> _currentActors = new Stack<TacAIActor>();

            [HarmonyPatch(typeof(AIBlackboard), nameof(AIBlackboard.BeforeAIActorEvaluation))]
            private static class Patch_AIBlackboard_BeforeAIActorEvaluation
            {
                private static void Prefix(TacAIActor aiActor)
                {
                    _currentActors.Push(aiActor);
                }
            }

            [HarmonyPatch(typeof(AIBlackboard), nameof(AIBlackboard.AfterAIActorEvaluation))]
            private static class Patch_AIBlackboard_AfterAIActorEvaluation
            {
                private static void Postfix()
                {
                    if (_currentActors.Count > 0)
                    {
                        _currentActors.Pop();
                    }
                }
            }

            [HarmonyPatch(typeof(AIUtil), nameof(AIUtil.GetEnemyWeight))]
            private static class Patch_AIUtil_GetEnemyWeight
            {
                private static void Postfix(AIBlackboard blackboard, TacticalActorBase enemy, ref float __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }

                    try
                    {
                        if (!HasTauntStatus(enemy))
                        {
                            return;
                        }

                        TacAIActor currentAiActor = GetCurrentActor();
                        if (!ShouldApplyTauntMultiplier(enemy, currentAiActor))
                        {
                            TacticalActorBase actingActor = currentAiActor != null ? currentAiActor.TacticalActor : null;
                            string actorName = actingActor != null ? actingActor.DisplayName : "unknown actor";
                            TFTVLogger.Always($"Skipping taunt multiplier for {enemy?.DisplayName}; {actorName} cannot reach this turn.");
                            return;
                        }

                        TFTVLogger.Always($"{enemy?.DisplayName} initial score is {__result}");
                        __result *= Multiplier;
                        TFTVLogger.Always($"{enemy?.DisplayName} new score is {__result}");
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static TacAIActor GetCurrentActor()
            {
                return _currentActors.Count > 0 ? _currentActors.Peek() : null;
            }

            private static bool ShouldApplyTauntMultiplier(TacticalActorBase enemy, TacAIActor aiActor)
            {
                if (enemy == null)
                {
                    return false;
                }

                if (aiActor == null)
                {
                    return true;
                }

                TacticalActor tacticalActor = aiActor.TacticalActor;
                if (tacticalActor == null || tacticalActor.IsDead || !tacticalActor.InPlay)
                {
                    return false;
                }

                EquipmentComponent equipmentComponent = tacticalActor.Equipments;
                if (equipmentComponent == null)
                {
                    return false;
                }

                foreach (Equipment equipment in equipmentComponent.Equipments)
                {
                    Weapon weapon = equipment as Weapon;
                    if (weapon == null || !weapon.IsUsable)
                    {
                        continue;
                    }

                    TacticalAbility attackAbility = tacticalActor.GetDefaultAttackAbility(weapon);
                    if (attackAbility == null || !attackAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                    {
                        continue;
                    }

                    DamagePayload damagePayload = weapon.GetDamagePayload();
                    if (damagePayload == null)
                    {
                        continue;
                    }

                    if (damagePayload.DamageDeliveryType != DamageDeliveryType.Melee)
                    {
                        return true;
                    }

                    float moveAndActRange = tacticalActor.GetMaxMoveAndActRange(weapon, null);
                    if (moveAndActRange <= 0f)
                    {
                        continue;
                    }

                    float actorRadius = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, tacticalActor);
                    float enemyRadius = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(0f, enemy);
                    float destinationRadius = actorRadius + enemyRadius;
                    float pathLength = AIUtil.GetPathLength(tacticalActor, tacticalActor.Pos, enemy.Pos, true, destinationRadius);
                    if (!float.IsPositiveInfinity(pathLength) && Utl.LesserThanOrEqualTo(pathLength, moveAndActRange, 0.01f))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasTauntStatus(TacticalActorBase actor)
            {
                try
                {


                    if (actor != null && actor.Status != null && actor.Status.HasStatus(_drawfireStatus))
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

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return false;
                    }

                    return _mentorProtocol != null && tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_mentorProtocol) != null;

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
            private const string PhoenixCommandName = "px";


            [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")] //VERIFIED
            private static class TacticalLevelController_ActorEnteredPlay_PounceProtocolPatch
            {
                public static void Postfix(TacticalLevelController __instance)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return;
                        }

                        RefreshPounceProtocolStatus(__instance);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")] //VERIFIED
            private static class TacticalLevelController_ActorDied_PounceProtocolPatch
            {
                public static void Postfix(TacticalLevelController __instance)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return;
                        }

                        RefreshPounceProtocolStatus(__instance);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                        throw;
                    }
                }
            }

            private static void RefreshPounceProtocolStatus(TacticalLevelController controller)
            {
                try
                {
                    if (controller == null || _pounceProtocol == null || _pounceProtocolSpeedStatus == null)
                    {
                        return;
                    }

                    TacticalFaction phoenixFaction = controller.GetFactionByCommandName(PhoenixCommandName);
                    if (phoenixFaction == null)
                    {
                        return;
                    }

                    bool hasPounceOperative = phoenixFaction.TacticalActors.Any(HasPounceProtocolAbility);

                    foreach (TacticalActor drone in phoenixFaction.TacticalActors.Where(actor => IsPlayerSpiderDrone(actor, phoenixFaction)))
                    {
                        if (drone?.Status == null)
                        {
                            continue;
                        }

                        TacStatsModifyStatus existingStatus = drone.Status.GetStatus<TacStatsModifyStatus>(_pounceProtocolSpeedStatus);

                        if (hasPounceOperative)
                        {
                            if (existingStatus == null)
                            {
                                drone.Status.ApplyStatus(_pounceProtocolSpeedStatus);
                            }
                        }
                        else if (existingStatus != null)
                        {
                            drone.Status.UnapplyStatus(existingStatus);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

            private static bool HasPounceProtocolAbility(TacticalActor actor)
            {
                if (actor == null || !actor.IsAlive || actor.IsEvacuated)
                {
                    return false;
                }

                return actor.GetAbilityWithDef<PassiveModifierAbility>(_pounceProtocol) != null;
            }

            private static bool IsPlayerSpiderDrone(TacticalActor actor, TacticalFaction phoenixFaction)
            {
                if (actor == null || actor.TacticalFaction != phoenixFaction)
                {
                    return false;
                }

                if (!actor.IsAlive || actor.IsEvacuated)
                {
                    return false;
                }

                return actor.GameTags.Contains(DefCache.GetDef<GameTagDef>("SpiderDrone_ClassTagDef"));

            }

        }

        internal class ViralPuppeteerToxicLink
        {


            [HarmonyPatch(typeof(TacticalAbility), "TargetFilterPredicate")] //VERIFIED
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

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }

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
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return false;
                    }


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

            private static readonly System.Reflection.FieldInfo SourceEquipmentField =
                AccessTools.Field(typeof(ShieldDeployedStatus), "_sourceEquipment");

            [HarmonyPatch(typeof(ShieldDeployedStatus), nameof(ShieldDeployedStatus.OnUnapply))]
            private static class ShieldDeployedStatus_OnUnapply_Patch
            {
                public static void Postfix(ShieldDeployedStatus __instance)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }

                    if (__instance?.TacticalActor == null)
                    {
                        return;
                    }

                    TacticalItem sourceItem = SourceEquipmentField?.GetValue(__instance) as TacticalItem;
                    if (!(sourceItem is Equipment equipment))
                    {
                        return;
                    }

                    AddonSlot holsterSlot = equipment.HolsterSlot;
                    if (holsterSlot == null || equipment.ParentSlot == holsterSlot)
                    {
                        return;
                    }

                    equipment.ForceReattachMeTo(holsterSlot);
                    equipment.UpdateModelVisibility();
                }
            }



            private static bool _shieldRiposteDeployingShield = false;

            [HarmonyPatch(typeof(PathProcessorUtils), nameof(PathProcessorUtils.UsesTurnAnimations))]
            private static class PathProcessorUtils_UsesTurnAnimations_Patch
            {
                public static bool Prefix(TacticalActor actor, ref bool __result)
                {
                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return true;
                    }

                    if (actor == null)
                    {
                        return true;
                    }

                    if (!actor.ExecutingAbilities.Any(ability => ability is DeployShieldAbility))
                    {
                        return true;
                    }

                    __result = false;
                    return false;
                }
            }


            [HarmonyPatch(typeof(DeployShieldAbility), nameof(DeployShieldAbility.Activate))]
            public static class Patch_DeployShieldAbility_Activate
            {
                public static void Postfix(DeployShieldAbility __instance)
                {

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return;
                    }

                    try
                    {
                        TacticalActor tacticalActor = __instance.TacticalActor;

                        if (tacticalActor.GetAbilityWithDef<PassiveModifierAbility>(_shieldedRiposte) != null)
                        {
                            _shieldRiposteDeployingShield = true;

                            /* TFTVLogger.Always($"tacticalActor?.DisplayName: {tacticalActor?.DisplayName} deploying shield, deployed status? " +
                                 $"{tacticalActor.Status.HasStatus<ShieldDeployedStatus>()}");*/

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

            [HarmonyPatch(typeof(EquipmentComponent), nameof(EquipmentComponent.SetSelectedEquipment))]
            public static class EquipmentComponent_SetSelectedEquipment_Patch
            {
                static bool IsRiotShield(Equipment equipment) =>
                    equipment != null && equipment.EquipmentDef == DefCache.GetDef<EquipmentDef>("FS_RiotShield_WeaponDef");

                static bool IsShieldDeployed(TacticalActor actor) =>
                    actor?.Status != null && actor.Status.HasStatus<ShieldDeployedStatus>();

                public static bool Prefix(EquipmentComponent __instance, Equipment equipment)
                {

                    if (!TFTVNewGameOptions.IsReworkEnabled())
                    {
                        return true;
                    }

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

        internal class PackLoyalty
        {
            private static readonly Dictionary<TacticalActor, HashSet<DamageMultiplierStatus>> _activeMindWardLinks = new Dictionary<TacticalActor, HashSet<DamageMultiplierStatus>>();
            private static readonly object _packLoyaltyImmunitySource = new object();

            private static DamageMultiplierStatusDef _psychicWardStatusDef;
            private static TacStatusDef _mindControlImmunityStatusDef;

            private static DamageMultiplierStatusDef PsychicWardStatusDef
            {
                get
                {
                    if (_psychicWardStatusDef == null)
                    {
                        _psychicWardStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("PsychicWard_StatusDef");
                    }

                    return _psychicWardStatusDef;
                }
            }

            private static TacStatusDef MindControlImmunityStatusDef
            {
                get
                {
                    if (_mindControlImmunityStatusDef == null)
                    {
                        _mindControlImmunityStatusDef = DefCache.GetDef<TacStatusDef>("MindControlImmunity_StatusDef");
                    }

                    return _mindControlImmunityStatusDef;
                }
            }

            public static void CheckForPackLoyaltyDrill(TacticalLevelController controller)
            {
                try
                {
                    _activeMindWardLinks.Clear();
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

            private static bool TryGetPackLoyaltyParticipants(DamageMultiplierStatus status, out TacticalActor priest, out TacticalActor mutog)
            {
                priest = null;
                mutog = null;

                if (status == null)
                {
                    return false;
                }

                if (PsychicWardStatusDef == null || status.TacStatusDef != PsychicWardStatusDef)
                {
                    return false;
                }

                mutog = status.TacticalActor as TacticalActor;
                if (mutog == null)
                {
                    return false;
                }

                priest = ResolveSourceActor(status.Source);
                return priest != null;
            }

            private static TacticalActor ResolveSourceActor(object source)
            {
                switch (source)
                {
                    case TacticalActor actor:
                        return actor;
                    case TacticalActorBase actorBase:
                        return actorBase as TacticalActor;
                    case TacticalAbility ability:
                        return ability.TacticalActor;
                    default:
                        return null;
                }
            }

            private static bool ShouldApplyPackLoyalty(TacticalActor priest, TacticalActor mutog)
            {
                if (priest == null || mutog == null || MindControlImmunityStatusDef == null)
                {
                    return false;
                }

                if (!mutog.HasGameTag(Shared.SharedGameTags.MutogTag))
                {
                    return false;
                }

                if (priest.GetAbilityWithDef<PassiveModifierAbility>(_packLoyalty) == null)
                {
                    return false;
                }

                if (priest.RelationTo(mutog) != FactionRelation.Friend)
                {
                    return false;
                }

                return true;
            }

            private static void RegisterMindWard(DamageMultiplierStatus status, TacticalActor priest, TacticalActor mutog)
            {
                if (status == null || priest == null || mutog == null)
                {
                    return;
                }

                if (!_activeMindWardLinks.TryGetValue(mutog, out var links))
                {
                    links = new HashSet<DamageMultiplierStatus>();
                    _activeMindWardLinks[mutog] = links;
                }

                int previousCount = links.Count;
                links.Add(status);

                if (previousCount == 0 && links.Count > 0)
                {
                    EnsureImmunity(mutog);
                }
            }

            private static void EnsureImmunity(TacticalActor mutog)
            {
                try
                {
                    if (mutog?.Status == null)
                    {
                        return;
                    }

                    TacStatusDef immunityDef = MindControlImmunityStatusDef;
                    if (immunityDef == null)
                    {
                        return;
                    }

                    if (!HasPackLoyaltyImmunity(mutog))
                    {
                        mutog.Status.ApplyStatus(immunityDef, _packLoyaltyImmunitySource);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            private static bool HasPackLoyaltyImmunity(TacticalActor mutog)
            {
                TacStatusDef immunityDef = MindControlImmunityStatusDef;
                if (mutog?.Status == null || immunityDef == null)
                {
                    return false;
                }

                TacStatus status = mutog.Status.GetStatus<TacStatus>(immunityDef);
                return status != null && status.Source == _packLoyaltyImmunitySource;
            }

            private static void UnregisterMindWard(DamageMultiplierStatus status)
            {
                if (status == null)
                {
                    return;
                }

                TacticalActor mutog = status.TacticalActor as TacticalActor;
                if (mutog == null)
                {
                    return;
                }

                if (_activeMindWardLinks.TryGetValue(mutog, out var links))
                {
                    links.Remove(status);
                    if (links.Count == 0)
                    {
                        _activeMindWardLinks.Remove(mutog);
                        RemoveImmunityIfOwned(mutog);
                    }
                }
            }

            private static void RemoveImmunityIfOwned(TacticalActor mutog)
            {
                try
                {
                    TacStatusDef immunityDef = MindControlImmunityStatusDef;
                    if (mutog?.Status == null || immunityDef == null)
                    {
                        return;
                    }

                    TacStatus status = mutog.Status.GetStatus<TacStatus>(immunityDef);
                    if (status != null && status.Source == _packLoyaltyImmunitySource)
                    {
                        mutog.Status.UnapplyStatus(status);
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw;
                }
            }

            [HarmonyPatch(typeof(DamageMultiplierStatus), nameof(DamageMultiplierStatus.OnApply))]
            public static class DamageMultiplierStatus_OnApply_PackLoyalty_Patch
            {
                public static void Postfix(DamageMultiplierStatus __instance)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return;
                        }


                        if (!TryGetPackLoyaltyParticipants(__instance, out var priest, out var mutog))
                        {
                            return;
                        }

                        if (!ShouldApplyPackLoyalty(priest, mutog))
                        {
                            return;
                        }

                        RegisterMindWard(__instance, priest, mutog);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                    }
                }
            }

            [HarmonyPatch(typeof(TacStatus), nameof(TacStatus.OnUnapply))]
            public static class DamageMultiplierStatus_OnUnapply_PackLoyalty_Patch
            {
                public static void Postfix(DamageMultiplierStatus __instance)
                {
                    try
                    {
                        if (!TFTVNewGameOptions.IsReworkEnabled())
                        {
                            return;
                        }


                        if (PsychicWardStatusDef == null || __instance?.TacStatusDef != PsychicWardStatusDef)
                        {
                            return;
                        }

                        UnregisterMindWard(__instance);
                    }
                    catch (Exception ex)
                    {
                        TFTVLogger.Error(ex);
                    }
                }
            }
        }

    }

}



















