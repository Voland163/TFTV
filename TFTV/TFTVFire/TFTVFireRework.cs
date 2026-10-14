using Base.AI;
using Base.Core;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Levels;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// The fire rework. Fire hurts in two separate ways:
    ///
    ///  - Ground fire (the burning tiles) hurts only while a character is in it, and only Hit Points:
    ///    starting a turn on a burning tile ignores armour, moving through one is reduced by the
    ///    average armour of the character's body parts. It never sets anyone burning.
    ///  - Burning (<see cref="BurningStatus"/>) comes only from incendiary attacks and sticks to the
    ///    body parts they reach. Each part keeps its own fire value, which ticks once on application
    ///    and then at the start of each of the character's turns after dropping by 10 (20 with fire
    ///    resistance), until it reaches 0. Healing items put it out on every part at once.
    ///
    /// Burning hurts heavily armoured targets little by design; with the limb hit malus on, every fire
    /// tick also feeds the limb hit malus (<see cref="TFTVLimbHitMalus"/>) even through armour.
    ///
    /// Vanilla's actor-wide FireStatus, which was recalculated from the fire tiles every turn, is no
    /// longer applied at all.
    /// </summary>
    internal static class TFTVFireRework
    {
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal const float DecayPerTurn = 10f;
        internal const float ResistantDecayPerTurn = 20f;

        internal static BurningStatusDef BurningStatusDef { get; set; }

        private static TacticalVoxelMatrixDataDef VoxelData(TacticalActorBase actor)
        {
            return actor?.TacticalLevel?.VoxelMatrix?.VoxelMatrixData;
        }

        internal static DamageTypeBaseEffectDef GetFireDamageType(TacticalActorBase actor)
        {
            return VoxelData(actor)?.FireDamageTypeDef;
        }

        /// <summary>All of the character's fire damage multipliers, multiplied together (two 50% sources let 25% through).</summary>
        internal static float GetFireMultiplier(TacticalActor actor)
        {
            DamageTypeBaseEffectDef fireType = GetFireDamageType(actor);
            return fireType == null || actor == null ? 1f : actor.GetDamageMultiplierFor(fireType, null);
        }

        internal static bool HasFireResistance(TacticalActor actor)
        {
            return GetFireMultiplier(actor) < 1f - 1E-04f;
        }

        /// <summary>Fire resistance doubles how fast burning parts go out.</summary>
        internal static float GetDecay(TacticalActor actor)
        {
            return HasFireResistance(actor) ? ResistantDecayPerTurn : DecayPerTurn;
        }

        internal static bool IsImmuneToBurning(TacticalActor actor)
        {
            if (actor == null)
            {
                return true;
            }

            TacticalAbilityDef immunity = BurningStatusDef?.VanillaFireStatusDef?.FireImmunityAbilityDef;

            if (immunity != null && actor.GetAbilityWithDef<TacticalAbility>(immunity) != null)
            {
                return true;
            }

            return GetFireMultiplier(actor) <= 0f;
        }

        internal static BurningStatus GetBurning(TacticalActor actor)
        {
            return actor?.Status?.GetStatuses<BurningStatus>().FirstOrDefault(status => !status.UnapplyRequested);
        }

        private static BurningStatus GetOrApplyBurning(TacticalActor actor, object source)
        {
            BurningStatus status = GetBurning(actor);

            if (status != null || BurningStatusDef == null)
            {
                return status;
            }

            // The status outlives the attack and goes into saves, so it keeps only who started the
            // fire (for kill credit on later ticks), not the projectile or effect itself.
            status = actor.Status.ApplyStatus(BurningStatusDef, TacUtil.GetSourceTacticalActorBase(source)) as BurningStatus;

            return status == null || status.UnapplyRequested ? null : status;
        }

        #region burning

        /// <summary>
        /// An incendiary attack reached this character: every body part catches fire at the
        /// weapon's fire value, keeping the higher value where a part already burns, and every part
        /// the attack reached ticks once at its resulting value - even if it was already burning
        /// higher.
        ///
        /// The parts reached are all of the character's health slots: vanilla's fire damage covers
        /// every slot of a character standing in the new fire, for blasts and flamethrower cones
        /// alike, and this keeps that.
        /// </summary>
        internal static void ApplyIncendiary(TacticalActor actor, float fireValue, object source)
        {
            if (actor == null || !actor.IsAlive || actor.BodyState == null || actor.Status == null || IsImmuneToBurning(actor))
            {
                return;
            }

            TacticalVoxelMatrixDataDef voxelData = VoxelData(actor);
            FireStatusDef vanillaDef = BurningStatusDef?.VanillaFireStatusDef;

            // Damage-over-time resistance (TFTV's Nanotech) still takes its flat share off, as
            // FireExplosionEffect did before.
            float resistance = vanillaDef == null ? 0f : actor.GetDamageOverTimeStatusResistance(vanillaDef);
            float value = Mathf.Round(fireValue - resistance * (voxelData?.FireDamageMultiplier ?? 1f));

            if (resistance > 0f)
            {
                AircraftReworkHooks.OnFireResisted(actor);
            }

            if (value <= 0f)
            {
                return;
            }

            BurningStatus status = GetOrApplyBurning(actor, source);

            if (status == null)
            {
                return;
            }

            List<ItemSlot> slots = actor.BodyState.GetHealthSlots().ToList();

            foreach (ItemSlot slot in slots)
            {
                string slotName = slot.GetSlotName();
                status.SetPartValue(slotName, Mathf.Max(status.GetPartValue(slotName), value));
            }

            status.NotifyPartsChanged();
            Tick(actor, status, slots, source);
        }

        /// <summary>
        /// One tick on the listed burning parts. They go into one fire accumulation, as vanilla's
        /// fire did: each part takes its own fire value less its armour, and Hit Points take the
        /// average across the burning parts. Every tick also feeds the limb hit malus.
        /// </summary>
        private static void Tick(TacticalActor actor, BurningStatus status, IEnumerable<ItemSlot> slots, object source)
        {
            TacticalVoxelMatrixDataDef voxelData = VoxelData(actor);

            if (voxelData?.FireDamageEffectDef == null)
            {
                return;
            }

            DamageAccumulation accumulation = new DamageAccumulation(voxelData.FireDamageEffectDef, source ?? status);
            List<KeyValuePair<ItemSlot, float>> ticked = new List<KeyValuePair<ItemSlot, float>>();

            foreach (ItemSlot slot in slots)
            {
                float value = status.GetPartValue(slot.GetSlotName());

                if (value <= 0f)
                {
                    continue;
                }

                // DamageAccumulation reads Amount as each target is added, so every part gets its own value.
                accumulation.Amount = value;
                accumulation.AddTarget(slot, Vector3.zero, Vector3.zero, CastHit.Empty, null);
                ticked.Add(new KeyValuePair<ItemSlot, float>(slot, value));
            }

            if (ticked.Count == 0)
            {
                return;
            }

            accumulation.ApplyAddedDamage();

            float multiplier = GetFireMultiplier(actor);

            foreach (KeyValuePair<ItemSlot, float> part in ticked)
            {
                TFTVLimbHitMalus.OnFireTick(actor, part.Key, part.Value * multiplier);
            }
        }

        /// <summary>Start of the burning character's turn: every part drops, and those still burning tick.</summary>
        internal static void OnBurningStartTurn(BurningStatus status)
        {
            try
            {
                TacticalActor actor = status?.TacticalActor;

                if (actor == null || !actor.IsAlive || actor.BodyState == null)
                {
                    return;
                }

                float decay = GetDecay(actor);
                List<ItemSlot> stillBurning = new List<ItemSlot>();

                foreach (KeyValuePair<string, float> part in status.GetParts())
                {
                    float value = part.Value - decay;
                    status.SetPartValue(part.Key, value);

                    ItemSlot slot = actor.BodyState.GetSlot(part.Key);

                    if (value > 0f && slot != null)
                    {
                        stillBurning.Add(slot);
                    }
                }

                status.NotifyPartsChanged();

                if (stillBurning.Count > 0)
                {
                    Tick(actor, status, stillBurning, status.Source);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void Extinguish(TacticalActor actor)
        {
            BurningStatus status = GetBurning(actor);

            if (status == null)
            {
                return;
            }

            status.ClearParts();
            status.NotifyPartsChanged();
        }

        /// <summary>
        /// What the burning will still take from Hit Points over the coming turns if nothing puts
        /// it out: every turn each part drops, ticks for its value less its armour, and Hit Points
        /// take the average across the parts still burning.
        /// </summary>
        internal static float ExpectedRemainingHealthLoss(TacticalActor actor, BurningStatus status)
        {
            if (actor?.BodyState == null || status == null)
            {
                return 0f;
            }

            float decay = GetDecay(actor);
            float multiplier = GetFireMultiplier(actor);
            List<KeyValuePair<float, float>> parts = new List<KeyValuePair<float, float>>();

            foreach (KeyValuePair<string, float> part in status.GetParts())
            {
                ItemSlot slot = actor.BodyState.GetSlot(part.Key);
                float armour = slot == null ? 0f : (float)slot.GetArmor().Value;
                parts.Add(new KeyValuePair<float, float>(part.Value, armour));
            }

            float total = 0f;

            for (int turn = 1; turn < 20; turn++)
            {
                List<float> damages = parts
                    .Where(part => part.Key - turn * decay > 0f)
                    .Select(part => Mathf.Max(0f, part.Key - turn * decay - part.Value) * multiplier)
                    .ToList();

                if (damages.Count == 0)
                {
                    break;
                }

                total += damages.Average();
            }

            return total;
        }

        #endregion

        #region ground fire

        /// <summary>The fire value of the burning tiles the character is in (their average), or 0.</summary>
        internal static float GetGroundFireValue(TacticalActorBase actor, out TacticalVoxel voxel)
        {
            voxel = null;
            List<TacticalVoxel> touched = new List<TacticalVoxel>();

            if (actor?.TacticalPerceptionBase == null || !actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Fire, null, touched))
            {
                return 0f;
            }

            List<float> values = touched
                .Select(touchedVoxel => (touchedVoxel?.VoxelManager as FireVoxelManager)?.CurrentDamage ?? 0f)
                .ToList();

            if (values.Count == 0)
            {
                return 0f;
            }

            voxel = touched[0];
            return Mathf.Round(values.Average());
        }

        /// <summary>Ground fire takes Hit Points only, never a body part.</summary>
        private static void ApplyGroundFireDamage(TacticalActor actor, float damage, object source)
        {
            if (damage <= 0f || !actor.IsAlive)
            {
                return;
            }

            actor.ApplyDamage(new DamageResult
            {
                Source = source,
                HealthDamage = damage,
                DamageTypeDef = GetFireDamageType(actor),
            });
        }

        /// <summary>
        /// Starting the turn on a burning tile: the tile's fire value off Hit Points, armour ignored
        /// (heat soaks through it), fire resistance applied. Units that cannot move take this every
        /// turn, which is accepted.
        /// </summary>
        internal static void OnActorStartTurn(TacticalActor actor)
        {
            if (actor == null || !actor.IsAlive || !actor.IsActive || actor.IsOffMap)
            {
                return;
            }

            float value = GetGroundFireValue(actor, out TacticalVoxel voxel);

            if (value <= 0f)
            {
                return;
            }

            float damage = Mathf.Round(value * GetFireMultiplier(actor));

            ApplyGroundFireDamage(actor, damage, voxel);
            TFTVLimbHitMalus.OnGroundFireTurnStart(actor, damage);
        }

        /// <summary>Expected ground fire damage for ending a turn at this position (0 when it is not burning).</summary>
        internal static float GetGroundFireDamageAt(TacticalActor actor, Vector3 position)
        {
            TacticalVoxelMatrix matrix = actor?.TacticalLevel?.VoxelMatrix;
            TacticalVoxel voxel = matrix?.GetVoxel(position);

            if (voxel == null || voxel.GetVoxelType() != TacticalVoxelType.Fire)
            {
                return 0f;
            }

            float value = (voxel.VoxelManager as FireVoxelManager)?.CurrentDamage ?? 0f;
            return value * GetFireMultiplier(actor);
        }

        /// <summary>
        /// Entering a burning tile while moving: the tile's fire value less the average armour of
        /// the character's body parts, fire resistance applied, off Hit Points only. Once per
        /// burning tile entered (TacticalVoxelMatrix.OnActorMovedInNewTile calls this per tile).
        /// </summary>
        internal static bool OnMovedIntoFire(TacticalActorBase actorBase)
        {
            if (!(actorBase is TacticalActor actor) || actor.BodyState == null)
            {
                return false;
            }

            float value = GetGroundFireValue(actor, out TacticalVoxel voxel);

            if (value <= 0f)
            {
                return false;
            }

            List<ItemSlot> slots = actor.BodyState.GetHealthSlots().ToList();
            float averageArmour = slots.Count == 0 ? 0f : slots.Average(slot => (float)slot.GetArmor().Value);
            float damage = Mathf.Round(Mathf.Max(0f, value - averageArmour) * GetFireMultiplier(actor));

            ApplyGroundFireDamage(actor, damage, voxel);
            return true;
        }

        #endregion

        #region healing items

        /// <summary>
        /// Medkits and the other healing consumables: a heal ability run through an item that
        /// spends charges on it. Skills that heal, and item abilities that do not heal, do not count.
        /// </summary>
        internal static bool IsHealingItem(HealAbility ability)
        {
            HealAbilityDef def = ability?.HealAbilityDef;

            if (def == null || def.ConsumedCharges == 0 || ability.Equipment == null)
            {
                return false;
            }

            return def.GeneralHealAmount > 0f
                || def.BodyPartHealAmount > 0f
                || (def.HealEffects != null && def.HealEffects.Count > 0);
        }

        #endregion

        /// <summary>
        /// The aircraft rework's Nanotech is a one-shot damage-over-time resistance that is spent when
        /// a damage-over-time status lands; the fire rework applies fire without the vanilla FireStatus
        /// it used to catch, so it is spent here instead.
        /// </summary>
        internal static class AircraftReworkHooks
        {
            internal static void OnFireResisted(TacticalActor actor)
            {
                try
                {
                    if (!TFTVAircraftReworkMain.AircraftReworkOn)
                    {
                        return;
                    }

                    DamageOverTimeResistanceStatusDef nanotech = TFTVMain.Main.DefCache.GetDef<DamageOverTimeResistanceStatusDef>("NanoTech_StatusDef");

                    if (nanotech == null || !actor.HasStatus(nanotech))
                    {
                        return;
                    }

                    if (actor.Status.GetStatusByName(nanotech.EffectName) is DamageOverTimeResistanceStatus status)
                    {
                        actor.Status.UnapplyStatus(status);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch]
        internal static class Patches
        {
            /// <summary>
            /// An incendiary's new fire tiles: instead of vanilla's one-off fire damage to everyone in
            /// them, set them burning. The prediction pass (isSimulation) stays vanilla - it forecasts
            /// the same application tick.
            /// </summary>
            [HarmonyPatch(typeof(FireExplosionEffect), "ApplyFireDamageToAffectedActors")]
            internal static class FireExplosionEffect_ApplyFireDamageToAffectedActors_Patch
            {
                private static readonly MethodInfo GetFireDamageValue = AccessTools.Method(typeof(FireExplosionEffect), "GetFireDamageValue");

                private static bool Prefix(FireExplosionEffect __instance, TacticalVoxelMatrix voxelMatrix, IEnumerable<TacticalVoxel> affectedVoxels, bool isSimulation)
                {
                    try
                    {
                        if (isSimulation || voxelMatrix == null || affectedVoxels == null || GetFireDamageValue == null)
                        {
                            return true;
                        }

                        // Invoked through reflection so TFTV's own GetFireDamageValue postfix still applies.
                        float fireValue = (float)GetFireDamageValue.Invoke(__instance, new object[] { voxelMatrix });
                        HashSet<TacticalVoxel> fire = new HashSet<TacticalVoxel>(affectedVoxels.Where(voxel => voxel != null && voxel.GetVoxelType() == TacticalVoxelType.Fire));

                        if (fire.Count == 0)
                        {
                            return false;
                        }

                        List<TacticalActor> actors = voxelMatrix.TacticalLevel.Map.GetActors<TacticalActor>(null)
                            .Where(actor => actor.IsActive && !actor.IsOffMap && actor.TacticalPerceptionBase != null)
                            .ToList();

                        foreach (TacticalActor actor in actors)
                        {
                            if (voxelMatrix.GetVoxels(actor.TacticalPerceptionBase.GetBounds(null)).Any(fire.Contains))
                            {
                                ApplyIncendiary(actor, fireValue, __instance.Source);
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

            /// <summary>Moving through a burning tile (the only caller is TacticalVoxelMatrix.OnActorMovedInNewTile).</summary>
            [HarmonyPatch(typeof(FireVoxelManager), nameof(FireVoxelManager.ApplyFireDamageToActor), new Type[] { typeof(TacticalActorBase) })]
            internal static class FireVoxelManager_ApplyFireDamageToActor_Patch
            {
                private static bool Prefix(TacticalActorBase actor, ref bool __result)
                {
                    try
                    {
                        __result = OnMovedIntoFire(actor);
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            /// <summary>Ground fire never sets anyone burning: vanilla's tile-driven FireStatus is no longer applied.</summary>
            [HarmonyPatch(typeof(TacticalVoxelMatrix), "UpdateActorFireStatuses")]
            internal static class TacticalVoxelMatrix_UpdateActorFireStatuses_Patch
            {
                private static bool Prefix()
                {
                    return false;
                }
            }

            /// <summary>The other way vanilla applies its FireStatus is at the end of a move into fire (MoveAbility.Move).</summary>
            [HarmonyPatch(typeof(StatusComponent), nameof(StatusComponent.ApplyStatus), new Type[] { typeof(StatusDef), typeof(object), typeof(object) })]
            internal static class StatusComponent_ApplyStatus_Patch
            {
                private static bool Prefix(StatusDef def, ref Status __result)
                {
                    if (def is FireStatusDef)
                    {
                        __result = null;
                        return false;
                    }

                    return true;
                }
            }

            /// <summary>A vanilla FireStatus brought back by an older save goes out instead of ticking the old way.</summary>
            [HarmonyPatch(typeof(FireStatus), nameof(FireStatus.StartTurn))]
            internal static class FireStatus_StartTurn_Patch
            {
                private static bool Prefix(FireStatus __instance)
                {
                    try
                    {
                        __instance.RequestUnapply(__instance.StatusComponent);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                    return false;
                }
            }

            [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.StartTurn))]
            internal static class TacticalActor_StartTurn_GroundFire_Patch
            {
                private static void Postfix(TacticalActor __instance)
                {
                    try
                    {
                        OnActorStartTurn(__instance);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            /// <summary>Using a healing item on a character puts out every burning part.</summary>
            [HarmonyPatch(typeof(HealAbility), nameof(HealAbility.Activate))]
            internal static class HealAbility_Activate_Patch
            {
                private static void Postfix(HealAbility __instance, object parameter)
                {
                    try
                    {
                        if (!IsHealingItem(__instance))
                        {
                            return;
                        }

                        if ((parameter as TacticalAbilityTarget)?.Actor is TacticalActor target)
                        {
                            Extinguish(target);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }

        /// <summary>
        /// The AI's side of fire. Moving through a burning tile was already expensive to its
        /// pathfinder (TacticalNavCostFactorFuncs prices it, capped by FirePathCostTacticalVanillaFixes).
        /// What it lacked: ending a turn on one is now very dangerous, because start-of-turn ground fire
        /// ignores armour; and an incendiary is worth throwing at an armoured target for the limb hit
        /// malus even when the fire barely scratches it. Healing items for burning allies are scored in
        /// TFTVArtOfCrab's heal consideration.
        /// </summary>
        internal static class FireAI
        {
            /// <summary>Score left on a burning position at best; less as the fire approaches the actor's Hit Points.</summary>
            private const float BurningPositionScore = 0.15f;
            private const float LethalPositionScore = 0.01f;

            /// <summary>How much an incendiary's fire value counts as damage against a target armour shrugs it off from.</summary>
            private const float IncendiaryDebuffScore = 0.5f;

            internal static float GetPositionFactor(TacticalActor actor, Vector3 position)
            {
                float damage = GetGroundFireDamageAt(actor, position);

                if (damage <= 0f)
                {
                    return 1f;
                }

                float health = actor.Health == null ? 0f : (float)actor.Health;

                if (health <= damage)
                {
                    return LethalPositionScore;
                }

                return Mathf.Max(LethalPositionScore, BurningPositionScore * (1f - damage / Mathf.Max(1f, health)));
            }

            private static IEnumerator<NextUpdate> PenaliseBurningPositions(IEnumerator<NextUpdate> original, IAIActor aiActor, IList<AIScoredTarget> output)
            {
                while (original.MoveNext())
                {
                    yield return original.Current;
                }

                if (!(aiActor is TacticalActor actor) || output == null)
                {
                    yield break;
                }

                try
                {
                    foreach (AIScoredTarget scored in output)
                    {
                        if (scored?.Target is TacAITarget target && target.IsPosValid)
                        {
                            scored.Score *= GetPositionFactor(actor, target.Pos);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            [HarmonyPatch(typeof(AIActorMovementZoneTargetGenerator), nameof(AIActorMovementZoneTargetGenerator.FillTargets))]
            internal static class AIActorMovementZoneTargetGenerator_FillTargets_Patch
            {
                private static void Postfix(IAIActor actor, IList<AIScoredTarget> targets, ref IEnumerator<NextUpdate> __result)
                {
                    __result = PenaliseBurningPositions(__result, actor, targets);
                }
            }

            /// <summary>
            /// The AI scores an attack by the damage it predicts, and a fire that armour absorbs
            /// predicts next to none. With the limb hit malus on, the fire still debuffs the target, so
            /// part of the fire value is added back in proportion to how much armour would block.
            /// </summary>
            [HarmonyPatch(typeof(DamagePayload), nameof(DamagePayload.GetPayloadAiDamageScore))]
            internal static class DamagePayload_GetPayloadAiDamageScore_Fire_Patch
            {
                private static void Postfix(DamagePayload __instance, TacticalActorBase targetActorBase, ref float __result)
                {
                    try
                    {
                        if (!TFTVLimbHitMalus.IsEnabled || !(targetActorBase is TacticalActor actor) || __instance?.DamageKeywords == null)
                        {
                            return;
                        }

                        DamageKeywordPair fire = __instance.DamageKeywords.FirstOrDefault(pair => pair?.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword);

                        if (fire == null || !TFTVLimbHitMalus.IsEligible(actor) || IsImmuneToBurning(actor) || actor.BodyState == null)
                        {
                            return;
                        }

                        float value = fire.Value * GetFireMultiplier(actor);
                        List<ItemSlot> slots = actor.BodyState.GetHealthSlots().ToList();

                        if (value <= 0f || slots.Count == 0)
                        {
                            return;
                        }

                        float throughArmour = slots.Average(slot => Mathf.Max(0f, value - (float)slot.GetArmor().Value));
                        float blocked = 1f - Mathf.Clamp01(throughArmour / value);

                        __result += value * IncendiaryDebuffScore * blocked;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }
    }
}
