using Base.Core;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Lighting;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.TFTVVoidOmensExpansion
{
    /// <summary>
    /// The Void Omens that play out on the tactical map: Pandoran perception by day and night (VO20), healing in Mist
    /// (VO21), worms exploding on death (VO28), mind control that sticks (VO32), injured and delirious operatives gaining
    /// perks (VO36) and Pandoran stealth from Willpower (VO37).
    /// </summary>
    internal static class TacticalOmens
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private const string PandoranFactionCommandName = "aln";
        private const string PhoenixFactionCommandName = "px";

        private const int VO20PerceptionShift = 10;
        private const float VO21HealShareOfMaxHealth = 0.25f;

        /// <summary>A disabled body part the Mist mends comes back with this much HP.</summary>
        private const float VO21RestoredPartHealth = 10f;

        /// <summary>Damage of the claw an Arthron grows where its right arm was.</summary>
        private const float VO21WeakClawDamage = 50f;

        private const string ArthronRightArmSlotDefName = "Crabman_RightArm_SlotDef";
        private const string ArthronRightHandPrefix = "Crabman_RightHand_";

        /// <summary>The pincer clone an Arthron regrows in the Mist.</summary>
        private static WeaponDef WeakClawDef { get; set; }
        private const int VO32TurnsToSwitchSides = 2;
        private const float VO36HealthShare = 0.5f;
        private const float VO36DeliriumShareOfWillpower = 0.5f;
        private const float VO37StealthPerWillPoint = 0.02f;
        private const float VO37MaxStealthBonus = 0.5f;
        private const int VO37WillPointsLostPerTurn = 2;

        private static VoidOmenStatusDef StatusDef { get; set; }

        /// <summary>Each worm's own explosion, as an effect that can go off when it dies.</summary>
        private static readonly Dictionary<ApplyDamageEffectAbilityDef, DamagePayloadEffectDef> _wormExplosions = new Dictionary<ApplyDamageEffectAbilityDef, DamagePayloadEffectDef>();

        /// <summary>Worms that set off their own explosion: they die of it, and must not go off twice.</summary>
        private static readonly HashSet<TacticalActor> _detonatedWorms = new HashSet<TacticalActor>();

        internal static void CreateDefs()
        {
            try
            {
                FireStatusDef vanillaFire = DefCache.GetDef<FireStatusDef>("Fire_StatusDef");

                VoidOmenStatusDef status = Helper.CreateDefFromClone<VoidOmenStatusDef>(null, "{2E8C6A14-7B3D-4F09-A1D6-5C90E37B4F12}", "TFTV_VoidOmen_StatusDef");

                status.EffectName = "TFTV_VoidOmen";
                status.ApplicationConditions = new EffectConditionDef[0];
                status.DurationTurns = -1;
                status.ExpireOnEndOfTurn = false;
                status.DisablesActor = false;
                status.SingleInstance = true;
                status.ShowNotification = false;
                status.VisibleOnPassiveBar = false;
                status.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                status.VisibleOnStatusScreen = 0;
                status.HealthbarPriority = vanillaFire.HealthbarPriority;
                status.StackMultipleStatusesAsSingleIcon = false;
                status.ParticleEffectPrefab = null;
                status.EventOnApply = null;
                status.EventOnUnapply = null;
                status.Visuals = Helper.CreateDefFromClone(vanillaFire.Visuals, "{91B4D7C3-2F6E-4A58-8E03-C7A15D2E9B60}", "TFTV_VoidOmen_StatusDef");

                StatusDef = status;

                // An Acid Swarmer's death explosion is the pattern; the payload is the worm's own, shared rather than
                // copied so that what Stronger Pandorans does to it carries over.
                DamagePayloadEffectDef deathExplosion = DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerAcidExplosion_Die_AbilityDef]");

                CreateWormExplosion(deathExplosion, "AcidwormExplode_AbilityDef", "{5D19F0A2-8C47-4E3B-B6D1-34E7A0C96F25}");
                CreateWormExplosion(deathExplosion, "FirewormExplode_AbilityDef", "{A73E2B68-1D95-4C07-9F4A-E08B6D13C572}");
                CreateWormExplosion(deathExplosion, "PoisonwormExplode_AbilityDef", "{C84A06D9-3E72-4B1F-A5C8-17F2D9E60B3A}");

                CreateWeakClaw();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// VO21: a basic pincer that hits for VO21WeakClawDamage. Its damage payload is a copy, so the pincers
        /// Arthrons are born with keep theirs.
        /// </summary>
        private static void CreateWeakClaw()
        {
            WeaponDef pincer = DefCache.GetDef<WeaponDef>("Crabman_RightHand_Pincer_WeaponDef");
            WeaponDef claw = Helper.CreateDefFromClone(pincer, "{6B3E91D4-0A72-4F58-9C1E-D25A87F3B046}", "TFTV_VO21_MistClaw_WeaponDef");

            DamagePayload payload = new DamagePayload();
            Helper.CopyFieldsByReflection(pincer.DamagePayload, payload);

            DamageKeywordDef damage = Shared.SharedDamageKeywords.DamageKeyword;
            payload.DamageKeywords = pincer.DamagePayload.DamageKeywords
                .Select(k => new DamageKeywordPair { DamageKeywordDef = k.DamageKeywordDef, Value = k.DamageKeywordDef == damage ? VO21WeakClawDamage : k.Value })
                .ToList();

            claw.DamagePayload = payload;
            WeakClawDef = claw;

            // Animations are picked by equipment: without these the Arthron has no strike, idle or move pose
            // for the claw, the shot origin comes back null and the AI's turn never finishes.
            int lists = 0;
            int actions = 0;

            foreach (EquipmentListDef list in Repo.GetAllDefs<EquipmentListDef>().Where(l => l.Equipments != null && l.Equipments.Contains(pincer)))
            {
                list.Equipments = list.Equipments.Append(claw).ToArray();
                lists++;
            }

            foreach (TacActorAnimActionEquipmentFilteredDef animation in Repo.GetAllDefs<TacActorAnimActionEquipmentFilteredDef>())
            {
                bool added = false;

                if (animation.Equipments != null && animation.Equipments.Contains(pincer) && !animation.Equipments.Contains(claw))
                {
                    animation.Equipments = animation.Equipments.Append(claw).ToArray();
                    added = true;
                }

                if (animation.Bodyparts != null && animation.Bodyparts.Contains(pincer) && !animation.Bodyparts.Contains(claw))
                {
                    animation.Bodyparts = animation.Bodyparts.Append(claw).ToArray();
                    added = true;
                }

                if (added)
                {
                    actions++;
                }
            }

            TFTVLogger.Always($"[VO21] {claw.name} uses the pincer's animations: added to {lists} equipment lists and {actions} animation actions");
        }

        private static void CreateWormExplosion(DamagePayloadEffectDef source, string abilityName, string guid)
        {
            ApplyDamageEffectAbilityDef explodeAbility = DefCache.GetDef<ApplyDamageEffectAbilityDef>(abilityName);
            DamagePayloadEffectDef explosion = Helper.CreateDefFromClone(source, guid, $"TFTV_VO28_{abilityName}");
            explosion.DamagePayload = explodeAbility.DamagePayload;
            _wormExplosions[explodeAbility] = explosion;
        }

        private static bool IsPandoran(TacticalActor actor)
        {
            // Not GetFactionByCommandName: it throws on a mission without that faction.
            return actor.TacticalFaction?.Faction?.FactionDef?.MatchesShortName(PandoranFactionCommandName) == true;
        }

        private static bool HasMutations(TacticalActor actor)
        {
            return actor.BodyState?.GetArmourItems().Any(a => a.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag)) == true;
        }

        /// <summary>
        /// Night or day, read from the lighting the mission runs under; null for maps with no sky (Pandoran colonies,
        /// Phoenix bases). The lighting can be unset this early on a map, and then the local time of the site decides.
        /// </summary>
        private static bool? IsNightMission(TacticalLevelController controller)
        {
            LightingSettingsDef lighting = controller.TacticalGameParams.LightingSettingsDef;

            if (lighting == null)
            {
                lighting = GameUtl.GameComponent<LightingManager>()?.CurrentLightingSettingsDef;
            }

            string name = lighting != null ? lighting.name : null;

            if (string.IsNullOrEmpty(name))
            {
                int hour = controller.TacticalGameParams.GlobalTime.dateTime.Hour;
                return hour >= 20 || hour < 6;
            }

            if (name.Contains("Night") || name.Contains("Darkness"))
            {
                return true;
            }

            if (name.StartsWith("PX_") || (name.StartsWith("ALN_") && !name.Contains("Day")))
            {
                return null;
            }

            return false;
        }

        internal static void OnActorEnteredPlay(TacticalActorBase actorBase, TacticalLevelController controller)
        {
            try
            {
                if (!(actorBase is TacticalActor actor) || actor.Status == null || actor.CharacterStats == null)
                {
                    return;
                }

                bool[] omens = TFTVVoidOmens.VoidOmensCheck;

                if (!omens[20] && !omens[21] && !omens[37])
                {
                    return;
                }

                bool pandoran = IsPandoran(actor);

                if (!pandoran && !(omens[21] && HasMutations(actor)))
                {
                    return;
                }

                // A loaded actor brings the status and its modifications back.
                if (actor.Status.GetStatus<VoidOmenStatus>() != null)
                {
                    return;
                }

                actor.Status.ApplyStatus(StatusDef);

                if (omens[20] && pandoran)
                {
                    ApplyDayNightPerception(actor, controller);
                }

                if (omens[37] && pandoran)
                {
                    RefreshStealth(actor.Status.GetStatus<VoidOmenStatus>());
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyDayNightPerception(TacticalActor actor, TacticalLevelController controller)
        {
            bool? night = IsNightMission(controller);

            if (night == null)
            {
                return;
            }

            BaseStat perception = actor.CharacterStats.Perception;
            float current = perception;

            // Eggs and sentinels with no perception are left alone, and nothing is blinded outright.
            if (current <= 0f)
            {
                return;
            }

            float shift = night.Value ? VO20PerceptionShift : Mathf.Max(-VO20PerceptionShift, 1f - current);

            perception.AddStatModification(new StatModification(StatModificationType.Add, perception.Name, shift, StatusDef, shift), false);
            perception.ReapplyModifications();
        }

        internal static void OnStatusStartTurn(VoidOmenStatus status)
        {
            try
            {
                TacticalActor actor = status.TacticalActor;

                if (actor == null || !actor.IsAlive)
                {
                    return;
                }

                bool[] omens = TFTVVoidOmens.VoidOmensCheck;
                bool pandoran = IsPandoran(actor);

                if (omens[21] && (pandoran || HasMutations(actor)))
                {
                    HealInMist(actor);
                }

                if (omens[37] && pandoran)
                {
                    DrainWillPointsWhileUndetected(actor);
                    RefreshStealth(status);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// VO21, at the start of the actor's turn in the Mist: a quarter of its HP back, every disabled body part
        /// working again at VO21RestoredPartHealth, and an Arthron's lost right arm grown back as a weak claw.
        /// </summary>
        private static void HealInMist(TacticalActor actor)
        {
            if (actor.TacticalPerceptionBase == null || !actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist))
            {
                return;
            }

            StatusStat health = actor.CharacterStats.Health;
            float before = health;

            if (before < health.Max)
            {
                health.AddRestrictedToMax(Mathf.Ceil(health.Max * VO21HealShareOfMaxHealth));
            }

            List<string> mended = new List<string>();
            bool arthronArm = false;

            foreach (ItemSlot slot in actor.BodyState?.GetHealthSlots()?.Where(s => s != null && !s.Enabled).ToList() ?? new List<ItemSlot>())
            {
                StatusStat partHealth = slot.GetHealth();

                if (partHealth == null)
                {
                    continue;
                }

                partHealth.Set(Mathf.Min(VO21RestoredPartHealth, partHealth.Max), true);

                if (!slot.Enabled)
                {
                    slot.SetToEnabled();
                }

                mended.Add(slot.ItemSlotDef?.name ?? "?");
                arthronArm |= slot.ItemSlotDef?.name == ArthronRightArmSlotDefName;
            }

            // The claw itself can be shot off and destroyed while the arm stays: then there is no slot to mend.
            string claw = arthronArm || HasLostRightClaw(actor) ? GrowWeakClaw(actor) : null;

            if (mended.Count > 0)
            {
                actor.RecalculateUsableHands();
                actor.UpdateStats();
            }

            if (mended.Count > 0 || (float)health > before || claw != null)
            {
                TFTVLogger.Always($"[VO21] {actor.DisplayName} heals in the Mist: HP {before:0} -> {(float)health:0}" +
                    (mended.Count > 0 ? $", mended {string.Join(", ", mended)} at {VO21RestoredPartHealth:0} HP" : "") +
                    (claw != null ? $", {claw}" : ""));
            }
        }

        /// <summary>An Arthron with its right arm but nothing in that hand: its gun or pincer was destroyed.</summary>
        private static bool HasLostRightClaw(TacticalActor actor)
        {
            if (actor.Equipments == null || actor.BodyState == null)
            {
                return false;
            }

            ClassTagDef crabman = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");

            if (crabman == null || actor.GameTags == null || !actor.GameTags.Contains(crabman))
            {
                return false;
            }

            ItemSlot rightArm = actor.BodyState.GetHealthSlots()?.FirstOrDefault(s => s?.ItemSlotDef != null && s.ItemSlotDef.name == ArthronRightArmSlotDefName);

            return rightArm != null && rightArm.Enabled
                && !actor.Equipments.GetWeapons().Any(w => w?.WeaponDef != null
                    && (w.WeaponDef == WeakClawDef || w.WeaponDef.name.StartsWith(ArthronRightHandPrefix)));
        }

        /// <summary>Whatever the Arthron had in its right hand (a gun, a pincer) gives way to the weak claw.</summary>
        private static string GrowWeakClaw(TacticalActor actor)
        {
            if (WeakClawDef == null || actor.Equipments == null)
            {
                return null;
            }

            Weapon old = actor.Equipments.GetWeapons().FirstOrDefault(w => w?.WeaponDef != null
                && (w.WeaponDef == WeakClawDef || w.WeaponDef.name.StartsWith(ArthronRightHandPrefix)));

            if (old?.WeaponDef == WeakClawDef)
            {
                return null;
            }

            string oldName = old?.WeaponDef.name ?? "nothing";

            if (old != null)
            {
                actor.Equipments.RemoveItem(old);
                old.Destroy();
            }

            if (actor.Equipments.AddItem(WeakClawDef, null) is Equipment claw)
            {
                actor.Equipments.SetSelectedEquipment(claw);
            }
            else
            {
                TFTVLogger.Always($"[VO21] {actor.DisplayName}: the weak claw could not be added (had {oldName})");
                return null;
            }

            actor.RecalculateUsableHands();
            actor.UpdateStats();

            TFTVTauntsAndQuips.FloatingTextManager.ShowFloatingText(actor, TFTVCommonMethods.ConvertKeyToString("TFTV_CLAW_REGROWN"), Color.white);
            return $"right arm regrown as a weak claw ({oldName} -> {WeakClawDef.name}, {VO21WeakClawDamage:0} damage)";
        }

        private static void DrainWillPointsWhileUndetected(TacticalActor actor)
        {
            TacticalFaction phoenix = actor.TacticalLevel.GetFactionByCommandName(PhoenixFactionCommandName);

            if (phoenix == null || phoenix.Vision.IsRevealed(actor))
            {
                return;
            }

            StatusStat willPoints = actor.CharacterStats.WillPoints;

            if ((float)willPoints > 0f)
            {
                willPoints.Subtract(Mathf.Min(VO37WillPointsLostPerTurn, (float)willPoints));
            }
        }

        /// <summary>VO37: stealth follows Willpower, so a Pandoran that stays hidden long enough loses the bonus.</summary>
        internal static void RefreshStealth(VoidOmenStatus status)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[37] || status?.TacticalActor?.CharacterStats == null || !IsPandoran(status.TacticalActor))
                {
                    return;
                }

                TacticalActor actor = status.TacticalActor;
                float bonus = Mathf.Min((float)actor.CharacterStats.WillPoints * VO37StealthPerWillPoint, VO37MaxStealthBonus);

                if (Mathf.Approximately(bonus, status.StealthBonus))
                {
                    return;
                }

                BaseStat stealth = actor.CharacterStats.Stealth;

                stealth.RemoveStatModificationsWithSource(StatusDef, true);

                if (bonus > 0f)
                {
                    stealth.AddStatModification(new StatModification(StatModificationType.Add, stealth.Name, bonus, StatusDef, bonus), true);
                }

                status.StealthBonus = bonus;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>VO28. A worm that set off its own explosion has already gone off.</summary>
        internal static void OnActorDied(DeathReport deathReport)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[28] || !(deathReport.Actor is TacticalActor actor))
                {
                    return;
                }

                KeyValuePair<ApplyDamageEffectAbilityDef, DamagePayloadEffectDef> worm = _wormExplosions
                    .FirstOrDefault(w => actor.GetAbilityWithDef<ApplyDamageEffectAbility>(w.Key) != null);

                if (worm.Value == null || _detonatedWorms.Remove(actor))
                {
                    return;
                }

                TFTVLogger.Always($"VO28: {actor.DisplayName} explodes on death");
                Effect.Apply(Repo, worm.Value, TacUtil.GetActorEffectTarget(actor, null), actor);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(ApplyDamageEffectAbility), nameof(ApplyDamageEffectAbility.Activate))]
        internal static class ApplyDamageEffectAbility_Activate_VO28_Patch
        {
            private static void Prefix(ApplyDamageEffectAbility __instance)
            {
                try
                {
                    if (TFTVVoidOmens.VoidOmensCheck[28] && _wormExplosions.ContainsKey(__instance.ApplyDamageEffectAbilityDef))
                    {
                        _detonatedWorms.Add(__instance.TacticalActor);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// VO32. On the turn the time runs out the status comes off with the side the actor is on now as its "original"
        /// side, so unapplying it keeps the actor where it is, with no Willpower upkeep from then on.
        /// </summary>
        [HarmonyPatch(typeof(MindControlStatus), nameof(MindControlStatus.StartTurn))]
        internal static class MindControlStatus_StartTurn_VO32_Patch
        {
            private static void Postfix(MindControlStatus __instance)
            {
                try
                {
                    TacticalActor actor = __instance.TacticalActor;

                    if (!TFTVVoidOmens.VoidOmensCheck[32] || actor == null || !actor.IsAlive || __instance.UnapplyRequested
                        || __instance.TurnNumber - __instance.TurnApplied < VO32TurnsToSwitchSides)
                    {
                        return;
                    }

                    TFTVLogger.Always($"VO32: {actor.DisplayName} now belongs to {actor.TacticalFaction.TacticalFactionDef.name} for the rest of the mission");

                    __instance.OverridenMindControlStatus = null;
                    __instance.OriginalFaction = actor.TacticalFaction.TacticalFactionDef;
                    __instance.OriginalMissionParticipant = actor.MissionParticipant;
                    __instance.RequestUnapply(actor.Status);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>VO36: any operative with Delirium makes the perks possible.</summary>
        internal static bool DeliriumPerkGate(GeoLevelController controller)
        {
            return controller.PhoenixFaction.Soldiers.Any(s => (float)s.CharacterStats.Corruption > 0f);
        }

        /// <summary>VO36: one perk per operative, for the rest of the mission.</summary>
        internal static void OnActorHurt(TacticalActor actor, DamageResult damageResult)
        {
            try
            {
                if (!TFTVVoidOmens.VoidOmensCheck[36] || !actor.IsAlive || damageResult.HealthDamage < 1
                    || actor.TacticalFaction?.Faction?.BaseDef != Shared.PhoenixFactionDef || actor.CharacterStats == null)
                {
                    return;
                }

                float delirium = actor.CharacterStats.Corruption;
                StatusStat health = actor.CharacterStats.Health;

                if (delirium <= 0f || (float)health > (float)health.Max * VO36HealthShare
                    || delirium < (float)actor.CharacterStats.WillPoints.Max * VO36DeliriumShareOfWillpower)
                {
                    return;
                }

                VoidOmenStatus status = actor.Status.GetStatus<VoidOmenStatus>() ?? actor.Status.ApplyStatus(StatusDef) as VoidOmenStatus;

                if (status == null || status.InjuryPerkGranted)
                {
                    return;
                }

                status.InjuryPerkGranted = true;

                TacticalAbilityDef perk = TFTVDelirium.RollDeliriumPerk(actor.GetAbilities<TacticalAbility>().Select(a => a.TacticalAbilityDef));

                if (perk == null)
                {
                    return;
                }

                actor.AddAbility(perk, actor);
                TFTVDeliriumPerks.ApplyPerkGainedInMission(actor, perk);
                TFTVLogger.Always($"VO36: {actor.DisplayName} gains {perk.name} for the rest of the mission");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
