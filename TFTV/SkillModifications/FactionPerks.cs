using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class FactionPerks
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            // Die Hard: When you take lethal damage there is 50% to survive with 1HP and have all negative effects cleared and limbs restored. Can only trigger once per combat.
            Create_DieHard();
            //OW Focus: Change icon to 'UI_AbilitiesIcon_EquipmentAbility_OverwatchFocus-2.png'
            Change_OWFocus();
            //Battle Hardened: Gain +2 to all primary stats, +10% to accuracy and +4 to perception
            Create_BattleHardened();
            //Takedown: Your bash and melee attacks gain +100 Shock value.
            Create_Takedown();
            //Shadowstep: No changes
            Change_Shadowstep();
            //Pain Chameleon: Maybe no change, to check if one of the ..._PainChameleon_AbilityDef will work
            Change_PainChameloen();
            // Saboteur: Reduce AP cost by 1 for non attack abilities and Spider Drone usage while unrevealed
            Create_Saboteur();
            //Sower of Change: Passive, Returns 10% of damage as Viral to the attacker within 10 tiles
            Create_SowerOfChange();
            //Breathe Mist: Adding progression def
            Change_BreatheMist();
            //Resurrect: 3AP 6WP, to check if the Mutoid_ResurrectAbilityDef will work, change to only allow 1 ressurect at one time (same as MC)
            Change_Resurrect();
            //Pepper Cloud: 1AP 2WP, to check if the Mutoid_PepperCloud_ApplyStatusAbilityDef will work, change range from 5 to 8 tiles
            Change_PepperCloud();
            // Punisher: When you kill an enemy, his allies lose 2 additional Will Points
            Create_Punisher();
            //AR Targeting: 2AP 2WP, Target ally gains +20% accuracy
            Create_AR_Targeting();
            //Endurance: Create new with 'Recover Restores 75% WP (instead of 50%)', check cloning from 'RecoverWill_AbilityDef', icon to LargeIcon from 'Reckless_AbilityDef'
            Create_Endurance();
        }

        private static void Create_DieHard()
        {
            string skillName = "DieHard_AbilityDef";
            Sprite icon = Helper.CreateSpriteFromImageFile("die_hard_2.png");
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
            PassiveModifierAbilityDef dieHard = Helper.CreateDefFromClone(
                source,
                "150776C9-CDE9-43A8-97C9-0676D1652736",
                skillName);
            dieHard.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "3BB8CED0-452C-456B-921D-33F3E57D31C9",
                skillName);
            dieHard.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "01CFDA81-0B2E-49A5-9A48-50BF0101977A",
                skillName);
            // reset all possible passive modifications, we need none, this ability is only to have something to chose and as flag for the below Harmony patch
            dieHard.StatModifications = new ItemStatModification[0];
            dieHard.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            dieHard.DamageKeywordPairs = new DamageKeywordPair[0];
            // Set necessary fields
            dieHard.CharacterProgressionData.RequiredSpeed = 0;
            dieHard.CharacterProgressionData.RequiredStrength = 0;
            dieHard.CharacterProgressionData.RequiredWill = 0;
            dieHard.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_DIE_HARD";
            dieHard.ViewElementDef.Description.LocalizationKey = "PR_BC_DIE_HARD_DESC";
            dieHard.ViewElementDef.LargeIcon = icon;
            dieHard.ViewElementDef.SmallIcon = icon;

            // Create a dummy status to have a persitent flag on the actor after Die Hard triggered to not have it trigger again in the current mission, see Harmony patch below
            TacStatusDef dieHardTriggeredStatus = Helper.CreateDefFromClone<TacStatusDef>(null, "11F6B0E4-5238-4478-910F-F868D3AD768E", $"DieHard_TriggeredStatus");
            dieHardTriggeredStatus.EffectName = "DieHard_TriggeredStatus";
            dieHardTriggeredStatus.ExpireOnEndOfTurn = false; // never expires, once it is applied to the actor it should stay the whole mission
            dieHardTriggeredStatus.ApplicationConditions = new EffectConditionDef[0];
            dieHardTriggeredStatus.Visuals = Helper.CreateDefFromClone(
                DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef").Visuals,
                "202879CB-09C1-4F92-B426-5E5B1DAB6D95",
                "E_Visuals [DieHard_TriggeredStatus]");
            dieHardTriggeredStatus.Visuals.DisplayName1.LocalizationKey = "PR_BC_DIE_HARD_TRIGGERED";
            dieHardTriggeredStatus.Visuals.Description.LocalizationKey = "PR_BC_DIE_HARD_TRIGGERED_DESC";
            dieHardTriggeredStatus.Visuals.LargeIcon = icon;
            dieHardTriggeredStatus.Visuals.SmallIcon = icon;
            dieHardTriggeredStatus.ShowNotification = true;
            dieHardTriggeredStatus.VisibleOnPassiveBar = true;
            dieHardTriggeredStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            dieHardTriggeredStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;

            // Create another dummy status to to make the actor invulnerable in the turn when Die Hard triggers, see Harmony patch below
            TacStatusDef dieHardKeepAliveStatus = Helper.CreateDefFromClone(
                dieHardTriggeredStatus,
                "B10CF045-D686-4A8D-A359-9636CA6EA8BA",
                $"DieHard_KeepAliveStatus");
            dieHardKeepAliveStatus.EffectName = "DieHard_KeepAliveStatus";
            dieHardKeepAliveStatus.DurationTurns = 0; // should only be active in the enemy turn when Die Hard triggered
            dieHardKeepAliveStatus.ExpireOnEndOfTurn = true; // ^^
            dieHardKeepAliveStatus.Visuals = Helper.CreateDefFromClone(
                dieHardTriggeredStatus.Visuals,
                "3433A8D1-DF3D-4FC3-92DA-3AF92EB835DC",
                "E_Visuals [DieHard_KeepAliveStatus]");
            dieHardKeepAliveStatus.Visuals.DisplayName1.LocalizationKey = "PR_BC_DIE_HARD_KEEPALIVE";
            dieHardKeepAliveStatus.Visuals.Description.LocalizationKey = "PR_BC_DIE_HARD_KEEPALIVE_DESC";
            dieHardKeepAliveStatus.ShowNotification = false;
            dieHardKeepAliveStatus.VisibleOnHealthbar = default;
            dieHardKeepAliveStatus.VisibleOnStatusScreen = default;

            // Set static variables for the patch
            TacticalActor_ApplyDamageInternal_Patch.DieHardAbilityDef = dieHard;
            TacticalActor_ApplyDamageInternal_Patch.DieHardTriggeredStatus = dieHardTriggeredStatus;
            TacticalActor_ApplyDamageInternal_Patch.DieHardKeepAliveStatus = dieHardKeepAliveStatus;
        }
        [HarmonyPatch(typeof(TacticalActor), "ApplyDamageInternal")]
        public static class TacticalActor_ApplyDamageInternal_Patch
        {
            internal static AbilityDef DieHardAbilityDef { get; set; }
            internal static TacStatusDef DieHardTriggeredStatus { get; set; }
            internal static TacStatusDef DieHardKeepAliveStatus { get; set; }
            internal static Shader Shader { get; } = DefCache.GetDef<StanceStatusDef>("E_VanishedStatus [Vanish_AbilityDef]").StanceShader;
            internal static List<TacticalActorBase> DieHardActorsToKeepAlive { get; } = new List<TacticalActorBase>();
            internal static bool OnFactionStartTurnSubscribed { get; set; } = false;
            internal static GameTagDef ExcludeFromAiBlackboard { get; } = DefCache.GetDef<GameTagDef>("ExcludeFromAiBlackboard_TagDef");
            internal static StatusDef[] StatusesToRemove = new StatusDef[]
            {
                        DieHardKeepAliveStatus,
                        DefCache.GetDef<StatusDef>("Bleed_StatusDef"),
                        DefCache.GetDef<StatusDef>("Poison_DamageOverTimeStatusDef"),
                        DefCache.GetDef<StatusDef>("Paralysis_DamageOverTimeStatusDef"),
                        DefCache.GetDef<StatusDef>("Fire_StatusDef"),
                        DefCache.GetDef<StatusDef>("Acid_StatusDef"),
                        DefCache.GetDef<StatusDef>("Infected_StatusDef"), // = virus applied
            };

            public static void Prefix(TacticalActor __instance, ref DamageResult damageResult)
            {
                try
                {
                    if ((damageResult.Source as TacticalActor)?.TacticalFaction == __instance.TacticalFaction
                        || damageResult.HealthDamage < __instance.Health
                        || __instance.GetAbilityWithDef<Ability>(DieHardAbilityDef) == null
                        || (__instance.HasStatus(DieHardTriggeredStatus) && !__instance.HasGameTag(ExcludeFromAiBlackboard)))
                    {
                        return;
                    }
                    if (!__instance.HasStatus(DieHardTriggeredStatus))
                    {
                        float overflowDamage = damageResult.HealthDamage - __instance.Health;
                        float lowcap = 0;
                        float highcap = 30;
                        float lowcapThreshold = lowcap + (highcap * 0.1f);
                        float highcapThreshold = highcap - (highcap * 0.1f);
                        // clamp overflow damage to lowcap+threshold and highcap-threshold
                        float clampedOverflowDamage = Mathf.Clamp(overflowDamage, lowcapThreshold, highcapThreshold);
                        // roll from lowcap to highcap
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        float roll = UnityEngine.Random.Range(0, highcap);
                        // => if clamped overlow damage is below lowcapThreshold DH will trigger with 90% chance
                        // => if clamped overlow damage is above highcapThreshold DH will trigger with 10% chance
                        if (clampedOverflowDamage > roll)
                        {
                            PRMLogger.Always($"Die Hard for {__instance}: Clamped overflow damage ({clampedOverflowDamage}) > RNG roll ({roll}), actor has to die :-(");
                            return; // Don't trigger Die Hard, RNG was against the actor
                        }
                        PRMLogger.Always($"Die Hard for {__instance}: Clamped overflow damage ({clampedOverflowDamage}) <= RNG roll ({roll}), trigger DH, actor get another chance :-)");
                        _ = __instance.Status.ApplyStatus(DieHardTriggeredStatus);
                        _ = __instance.Status.ApplyStatus(DieHardKeepAliveStatus);
                        _ = __instance.AddGameTags(new GameTagsList() { ExcludeFromAiBlackboard });
                        __instance.SetSpecialShader(Shader);
                        DieHardActorsToKeepAlive.Add(__instance);
                        if (!OnFactionStartTurnSubscribed)
                        {
                            __instance.TacticalFaction.StartTurnEvent += OnFactionStartTurn;
                            OnFactionStartTurnSubscribed = true;
                        }
                    }

                    PRMLogger.Always($"Die Hard for {__instance}: Original damage value = {damageResult.HealthDamage}, actor health = {__instance.Health.IntValue} ...");
                    // Set damage value to actors HP -1 so he has 1 HP left
                    damageResult.HealthDamage = __instance.Health - 1 < 0 ? 0 : __instance.Health - 1;

                    PRMLogger.Always($"Die Hard for {__instance}:  ... new damage value = {damageResult.HealthDamage}.");

                    // Clear all effects, statuses, stat modifier if set from the damage result
                    damageResult.ActorEffects?.Clear();
                    damageResult.ApplyStatuses?.Clear();
                    damageResult.StatModifications?.Clear();

                    // Unapply any existent DoT on the actor
                    //__instance.Status.UnapplyAllStatusesFiltered(status => StatusesToRemove.Contains(status.BaseDef));
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }

            private static void OnFactionStartTurn()
            {
                try
                {
                    PRMLogger.Always($"Die Hard OnFactionStartTurn() event handler called ...");
                    if (DieHardActorsToKeepAlive.IsEmpty())
                    {
                        PRMLogger.Always($"  ... no actors triggered Die Hard, early exit!");
                        return;
                    }
                    // Unapply any existent DoT on the actor
                    foreach (TacticalActorBase actor in DieHardActorsToKeepAlive)
                    {
                        PRMLogger.Always($"  ... cleaning DoTs and keep alive stuff from actor {actor}.");
                        actor.Status.UnapplyAllStatusesFiltered(status => StatusesToRemove.Contains(status.BaseDef));
                        (actor as TacticalActor)?.SetSpecialShader(null);
                        _ = actor.RemoveGameTags(new GameTagsList() { ExcludeFromAiBlackboard });
                        if (OnFactionStartTurnSubscribed)
                        {
                            actor.TacticalFaction.StartTurnEvent -= OnFactionStartTurn;
                            OnFactionStartTurnSubscribed = false;
                        }
                    }
                    DieHardActorsToKeepAlive.Clear();
                }
                catch (Exception e) 
                {
                    PRMLogger.Error(e);
                }
            }
        }

        private static void Create_Saboteur()
        {
            string skillName = "Saboteur_AbilityDef";

            ApplyStatusAbilityDef SneakAttack = DefCache.GetDef<ApplyStatusAbilityDef>("SneakAttack_AbilityDef");
            ApplyStatusAbilityDef Saboteur = Helper.CreateDefFromClone(
                SneakAttack,
                "F42B72D2-D1B8-4F9E-B0C5-FAAF81ED1234",
                skillName);
            Saboteur.CharacterProgressionData = Helper.CreateDefFromClone(
                SneakAttack.CharacterProgressionData,
                "D0D1FC34-9981-4E70-890E-9303F76CC91C",
                $"E_Preogression [{skillName}]");
            Saboteur.ViewElementDef = Helper.CreateDefFromClone(
                SneakAttack.ViewElementDef,
                "9080D03F-BB35-4B3E-A55B-BBAAC6DC163B",
                $"E_View [{skillName}]");
            Saboteur.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SABOTEUR";
            Saboteur.ViewElementDef.Description.LocalizationKey = "PR_BC_SABOTEUR_DESC";
            Sprite saboteurIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Thief.png");
            Saboteur.ViewElementDef.LargeIcon = saboteurIcon;
            Saboteur.ViewElementDef.SmallIcon = saboteurIcon;
            Saboteur.StatusDef = Helper.CreateDefFromClone(
                SneakAttack.StatusDef,
                "40B1D998-7F46-4AF4-B671-7E639967E4E0",
                $"E_Status [{skillName}]");

            ChangeAbilitiesCostStatusDef changeAbilitiesCostSource = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_MedkitAbilitiesCostChange [FastUse_AbilityDef]");
            
            ChangeAbilitiesCostStatusDef deployDroneChangeAbilitiesCostStatus = Helper.CreateDefFromClone(
                changeAbilitiesCostSource,
                "CA10F1C4-3138-4C13-95E0-07EA92CD199C",
                $"E_DeployDroneChangeAbilitiesCostStatus [{skillName}]");
            deployDroneChangeAbilitiesCostStatus.Visuals = Saboteur.ViewElementDef;
            deployDroneChangeAbilitiesCostStatus.AbilityCostModification.TargetAbilityTagDef = null;
            deployDroneChangeAbilitiesCostStatus.AbilityCostModification.SkillTagCullFilter = null;
            deployDroneChangeAbilitiesCostStatus.AbilityCostModification.EquipmentTagDef = DefCache.GetDef<GameTagDef>("DroneLauncherItem_TagDef");
            deployDroneChangeAbilitiesCostStatus.AbilityCostModification.AbilityCullFilter = null;

            // Drone Pack Tweak, it does not come from the drone launcher and only has the usual attack ability tag, needs a special tag to work
            SkillTagDef dronePackTag = Helper.CreateDefFromClone(
                DefCache.GetDef<SkillTagDef>("AttackAbility_SkillTagDef"),
                "C1B51EC8-4E0A-42EF-98C0-F15A62032981",
                "DronePack_SkillTagDef");
            TacticalAbilityDef deployDronePack = DefCache.GetDef<TacticalAbilityDef>("DeployDronePack_ShootAbilityDef");
            //TacticalAbilityDef deployDrone = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("DeployDrone_ShootAbilityDef"));
            //deployDrone.SkillTags = deployDrone.SkillTags.AddToArray(dronePackTag);
            deployDronePack.SkillTags = deployDronePack.SkillTags.AddToArray(dronePackTag);
            
            ChangeAbilitiesCostStatusDef deployDronePackChangeAbilitiesCostStatus = Helper.CreateDefFromClone(
                changeAbilitiesCostSource,
                "CAE456B2-F1EB-460E-B412-EFF745CA3071",
                $"E_DeployDronePackChangeAbilitiesCostStatus [{skillName}]");
            deployDronePackChangeAbilitiesCostStatus.Visuals = Saboteur.ViewElementDef;
            deployDronePackChangeAbilitiesCostStatus.AbilityCostModification.TargetAbilityTagDef = dronePackTag;
            deployDronePackChangeAbilitiesCostStatus.AbilityCostModification.SkillTagCullFilter = null;
            deployDronePackChangeAbilitiesCostStatus.AbilityCostModification.EquipmentTagDef = null;
            deployDronePackChangeAbilitiesCostStatus.AbilityCostModification.AbilityCullFilter = null;

            // List of all abilities that should not get AP reduction
            List<TacticalAbilityDef> excludedAbilities = new List<TacticalAbilityDef>()
            {
                DefCache.GetDef<TacticalAbilityDef>("Overwatch_AbilityDef"),
                //DefCache.GetDef<TacticalAbilityDef>("RecoverWill_AbilityDef")
            };

            ChangeAbilitiesCostStatusDef noneAttackChangeAbilitiesCostStatus = Helper.CreateDefFromClone(
                changeAbilitiesCostSource,
                "FD5DEF78-017D-4A9E-9DF5-9CA09EB82581",
                $"E_NoneAttackChangeAbilitiesCostStatus [{skillName}]");
            noneAttackChangeAbilitiesCostStatus.Visuals = Saboteur.ViewElementDef;
            noneAttackChangeAbilitiesCostStatus.AbilityCostModification.TargetAbilityTagDef = null;
            noneAttackChangeAbilitiesCostStatus.AbilityCostModification.SkillTagCullFilter = new SkillTagDef[] {
                DefCache.GetDef < SkillTagDef >("AttackAbility_SkillTagDef")
            };
            noneAttackChangeAbilitiesCostStatus.AbilityCostModification.EquipmentTagDef = null;
            noneAttackChangeAbilitiesCostStatus.AbilityCostModification.AbilityCullFilter = excludedAbilities;

            MultiStatusDef multiStatusDef = Helper.CreateDefFromClone(
                DefCache.GetDef<MultiStatusDef>("E_Status [FastUse_AbilityDef]"),
                "057B1342-B7AA-42F4-BD5C-578B19B1F5CA",
                $"E_MultiStatuses [{skillName}]");
            multiStatusDef.Statuses = new StatusDef[] {
                deployDroneChangeAbilitiesCostStatus,
                deployDronePackChangeAbilitiesCostStatus,
                noneAttackChangeAbilitiesCostStatus
            };

            FactionVisibilityConditionStatusDef factionVisibilityConditionStatus = (FactionVisibilityConditionStatusDef)Saboteur.StatusDef;
            factionVisibilityConditionStatus.RevealedStateStatusDef = null;
            factionVisibilityConditionStatus.LocatedStateStatusDef = multiStatusDef;
            factionVisibilityConditionStatus.HiddenStateStatusDef = multiStatusDef;
        }

        private static void Create_Punisher()
        {
            // Harmony patch RecoverWillAbility.GetWillpowerRecover
            // Adding an ability that get checked in the patched method (see below)
            string skillName = "Punisher_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
            PassiveModifierAbilityDef punisher = Helper.CreateDefFromClone(
                source,
                "5D896CB2-2472-48D8-9A3C-86F8B28435B9",
                skillName);
            punisher.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "6750E0C2-6044-4642-9811-ECF509EB97E4",
                skillName);
            punisher.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "7F38CD87-35A5-4C84-94AB-279392995A01",
                skillName);
            // reset all possible passive modifications, we need none, this ability is only to have something to chose and as flag for the Endurance Harmony patch
            punisher.StatModifications = new ItemStatModification[0];
            punisher.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            punisher.DamageKeywordPairs = new DamageKeywordPair[0];
            // Set necessary fields
            punisher.CharacterProgressionData.RequiredSpeed = 0;
            punisher.CharacterProgressionData.RequiredStrength = 0;
            punisher.CharacterProgressionData.RequiredWill = 0;
            punisher.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PUNISHER";
            punisher.ViewElementDef.Description.LocalizationKey = "PR_BC_PUNISHER_DESC";
            Sprite punisherIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_CharacterAbility_TargetLocation-2.png");
            punisher.ViewElementDef.LargeIcon = punisherIcon;
            punisher.ViewElementDef.SmallIcon = punisherIcon;
        }
        // Harmony patch to check if an actor got killed by someone with the Punisher ability
        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class BC_TacticalActor_OnAnotherActorDeath_Patch
        {
            public static void Postfix(TacticalActor __instance, DeathReport death)
            {
                try
                {
                    // Copy from original OnAnotherActorDeath method to catch some exceptions when this patch should also do nothing
                    if (death.Actor == __instance)
                    {
                        return;
                    }
                    if (__instance.Vehicle != null && __instance.Vehicle.Passengers.Contains(death.Actor))
                    {
                        MindControlStatusDef mindControlStatusDef = death.Actor.GetPreferredDieAbility().DieAbilityDef.StatusesToUnapplyFromMount.OfType<MindControlStatusDef>().FirstOrDefault();
                        if (mindControlStatusDef != null && __instance.Status.GetStatus<MindControlStatus>(mindControlStatusDef) != null)
                        {
                            return;
                        }
                    }
                    // end copy


                    if (death.Actor.TacticalActorBaseDef.WillPointWorth == 0)
                    {
                        return;
                    }
                    TacticalAbilityDef punisherAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Punisher_AbilityDef");
                    if (death.Killer != null && death.Killer.GetAbilityWithDef<TacticalAbility>(punisherAbilityDef) != null)
                    {
                        TacticalFaction tacticalFaction = death.Actor.TacticalFaction;
                        if (death.Actor.TacticalFaction == __instance.TacticalFaction && __instance.CharacterStats != null && __instance.CharacterStats.WillPoints > 0)
                        {
                            //TFTVLogger.Always($"Punisher -2 WP triggered for actor {__instance} from faction {__instance.TacticalFaction} because {death.Actor} from faction {death.Actor.TacticalFaction} got killed by {death.Killer}");
                            __instance.CharacterStats.WillPoints.Subtract(2);
                        }
                       
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        private static void Change_OWFocus()
        {
            OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");
            Sprite owSprite = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_EquipmentAbility_OverwatchFocus-2.png");
            overwatchFocus.ViewElementDef.LargeIcon = owSprite;
            overwatchFocus.ViewElementDef.SmallIcon = owSprite;
        }

        private static void Create_BattleHardened()
        {
            string skillName = "BattleHardened_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("EagleEyed_AbilityDef");
            PassiveModifierAbilityDef BattleHardened = Helper.CreateDefFromClone(
                source,
                "F25B325D-FBD3-4060-BF89-347DA0DF92C4",
                skillName);
            BattleHardened.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "E8E74771-5F65-4923-BF48-313823855C1A",
            skillName);
            BattleHardened.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "64C5ABA9-020B-4C8D-83E9-CC4124A72AB7",
                skillName);

            // Set necessary fields
            BattleHardened.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Speed,
                    Modification = StatModificationType.Add,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = 0.1f
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Perception,
                    Modification = StatModificationType.Add,
                    Value = 4
                }
            };
            BattleHardened.CharacterProgressionData.RequiredSpeed = 0;
            BattleHardened.CharacterProgressionData.RequiredStrength = 0;
            BattleHardened.CharacterProgressionData.RequiredWill = 0;
            BattleHardened.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_BATTLEHARDENED"; //new LocalizedTextBind("BATTLE HARDENED", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            BattleHardened.ViewElementDef.Description.LocalizationKey = "PR_BC_BATTLEHARDENED_DESC"; //new LocalizedTextBind("Gain +2 to all primary stats, +10% to accuracy and +4 to perception", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite BH_Icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_SpecOp-3.png");
            BattleHardened.ViewElementDef.LargeIcon = BH_Icon;
            BattleHardened.ViewElementDef.SmallIcon = BH_Icon;
        }

        private static void Create_Takedown()
        {
            string skillName = "BC_Takedown_AbilityDef";
            float bashDamage = 80f;
            float bashShock = 160f;
            //float meleeShockAddition = 100.0f;
            //LocalizedTextBind displayName = new LocalizedTextBind("TAKEDOWN", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind($"Deal {(int)bashDamage} damage and {(int)bashShock} shock damage to an adjacent target. Replaces Bash.", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite icon = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Brawler_AbilityDef]").LargeIcon;

            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("WeakSpot_AbilityDef");
            ApplyStatusAbilityDef takedown = Helper.CreateDefFromClone(
                source,
                "d2711bfc-b4cb-46dd-bb9f-599a88c1ebff",
                skillName);
            takedown.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "f7ce1c44-1447-41a3-8112-666c82451e25",
                skillName);
            takedown.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "0324925f-e318-40b6-ac8c-b68033823cd9",
                skillName);
            // Set usual fields for new created base ability (Takedown)
            takedown.CharacterProgressionData.RequiredStrength = 0;
            takedown.CharacterProgressionData.RequiredWill = 0;
            takedown.CharacterProgressionData.RequiredSpeed = 0;
            takedown.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_TAKEDOWN"; // displayName;
            takedown.ViewElementDef.Description.LocalizationKey = "PR_BC_TAKEDOWN_DESC"; // = description;
            takedown.ViewElementDef.LargeIcon = icon;
            takedown.ViewElementDef.SmallIcon = icon;

            // Create a new Bash ability by cloning from standard Bash with fixed damage and shock values
            BashAbilityDef bashToRemoveAbility = DefCache.GetDef<BashAbilityDef>("Bash_WithWhateverYouCan_AbilityDef");
            BashAbilityDef bashAbility = Helper.CreateDefFromClone(
                bashToRemoveAbility,
                "b2e1ecee-ad51-445f-afc4-6d2f629a8422",
                "Takedown_Bash_AbilityDef");
            SharedData Shared = GameUtl.GameComponent<SharedData>();
            bashAbility.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword,
                    Value = bashDamage
                },
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.ShockKeyword,
                    Value = bashShock
                }
            };
            bashAbility.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "e9617a5a-32ae-46a2-b9ca-538956470c0f",
                skillName);
            bashAbility.ViewElementDef.ShowInStatusScreen = false;
            bashAbility.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_TAKEDOWN"; // displayName;
            bashAbility.ViewElementDef.Description.LocalizationKey = "PR_BC_TAKEDOWN_DESC"; // = new LocalizedTextBind($"Deal {(int)bashDamage} damage and {(int)bashShock} shock damage to an adjacent target.", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            bashAbility.ViewElementDef.LargeIcon = icon;
            bashAbility.ViewElementDef.SmallIcon = icon;
            bashAbility.BashWith = BashAbilityDef.BashingWith.SelectedEquipmentOrBareHands;

            // Create a status to apply the bash ability to the actor
            AddAbilityStatusDef addNewBashAbiltyStatus = Helper.CreateDefFromClone( // Borrow status from Deplay Beacon (final mission)
                DefCache.GetDef<AddAbilityStatusDef>("E_AddAbilityStatus [DeployBeacon_StatusDef]"),
                "f084d230-9ad4-4315-a49d-d5e73c954254",
                $"E_ApplyNewBashAbilityEffect [{skillName}]");
            addNewBashAbiltyStatus.DurationTurns = -1;
            addNewBashAbiltyStatus.SingleInstance = true;
            addNewBashAbiltyStatus.ExpireOnEndOfTurn = false;
            addNewBashAbiltyStatus.AbilityDef = bashAbility;

            // Create an effect that removes the standard Bash from the actors abilities
            RemoveAbilityEffectDef removeRegularBashAbilityEffect = Helper.CreateDefFromClone(
                DefCache.GetDef<RemoveAbilityEffectDef>("RemoveAuraAbilities_EffectDef"),
                "b4bba4bf-f568-42b5-8baf-0169b7aa218a",
                $"E_RemoveRegularBashAbilityEffect [{skillName}]");
            removeRegularBashAbilityEffect.AbilityDefs = new AbilityDef[] { bashToRemoveAbility };

            // Create a status that applies the remove ability effect to the actor
            TacEffectStatusDef applyRemoveAbilityEffectStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<TacEffectStatusDef>("Mist_spawning_StatusDef"),
                "1a9ba75a-8075-4e07-8a13-b23798eda4a0",
                $"E_ApplyRemoveAbilityEffect [{skillName}]");
            applyRemoveAbilityEffectStatus.EffectName = "";
            applyRemoveAbilityEffectStatus.DurationTurns = -1;
            applyRemoveAbilityEffectStatus.ExpireOnEndOfTurn = false;
            applyRemoveAbilityEffectStatus.Visuals = null;
            applyRemoveAbilityEffectStatus.EffectDef = removeRegularBashAbilityEffect;
            applyRemoveAbilityEffectStatus.StatusAsEffectSource = false;
            applyRemoveAbilityEffectStatus.ApplyOnStatusApplication = true;
            applyRemoveAbilityEffectStatus.ApplyOnTurnStart = true;

            // Create a multi status to hold all statuses that Takedown applies to the actor
            MultiStatusDef multiStatus = Helper.CreateDefFromClone( // Borrow multi status from Rapid Clearance
                DefCache.GetDef<MultiStatusDef>("E_MultiStatus [RapidClearance_AbilityDef]"),
                "f4bc1190-c87c-4162-bf86-aa797c82d5d2",
                skillName);
            multiStatus.Statuses = new StatusDef[] { addNewBashAbiltyStatus, applyRemoveAbilityEffectStatus };

            takedown.StatusDef = multiStatus;

            //TacActorAimingAbilityAnimActionDef noWeaponBashAnim = Repo.GetAllDefs<TacActorAimingAbilityAnimActionDef>().FirstOrDefault(aa => aa.name.Equals("E_NoWeaponBash [Soldier_Utka_AnimActionsDef]"));
            //if (!noWeaponBashAnim.AbilityDefs.Contains(bashAbility))
            //{
            //    noWeaponBashAnim.AbilityDefs = noWeaponBashAnim.AbilityDefs.Append(bashAbility).ToArray();
            //}

            // Adding new bash ability to proper animations
            foreach (TacActorAimingAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorAimingAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(bashToRemoveAbility) && !animActionDef.AbilityDefs.Contains(bashAbility))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(bashAbility).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }

        private static void Change_Shadowstep()
        {
            PRMLogger.Debug("'" + MethodBase.GetCurrentMethod().DeclaringType.Name + "." + MethodBase.GetCurrentMethod().Name + "()' no changes implemented yet!");
            PRMLogger.Debug("----------------------------------------------------", false);
        }

        private static void Change_PainChameloen()
        {
            PRMLogger.Debug("'" + MethodBase.GetCurrentMethod().DeclaringType.Name + "." + MethodBase.GetCurrentMethod().Name + "()' no changes implemented yet!");
            PRMLogger.Debug("----------------------------------------------------", false);
        }

        private static void Create_SowerOfChange()
        {
            string skillName = "SowerOfChange_AbilityDef";
            //LocalizedTextBind name = new LocalizedTextBind("SOWER OF CHANGE", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Returns 25% of damage as Viral to the attacker within 10 tiles", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Sower_Of_Change_1-1.png");
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_ContactCorruption_ApplyStatusAbilityDef");
            ApplyStatusAbilityDef SowerOfChange = Helper.CreateDefFromClone(
                source,
                "40d9f907-a5a4-4f9a-bc12-e1a3f5459b3e",
                skillName);
            SowerOfChange.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "008272c9-2431-4681-a0a1-3bf61f3462bb",
                skillName);
            SowerOfChange.TargetingDataDef = Helper.CreateDefFromClone(
                source.TargetingDataDef,
                "1217a22e-0857-4094-a548-d224db6776a2",
                skillName);
            SowerOfChange.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "0441e1a3-47b5-4c31-9c33-5eb323f7e6a8",
                skillName);
            SowerOfChange.StatusDef = Helper.CreateDefFromClone(
                source.StatusDef,
                "1f5f7143-c6c3-440a-a7f5-0020f037d5cb",
                $"E_Status [{skillName}]");

            SowerOfChange.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SOWER_OF_CHANGE";
            SowerOfChange.ViewElementDef.Description.LocalizationKey = "PR_BC_SOWER_OF_CHANGE_DESC";
            SowerOfChange.ViewElementDef.LargeIcon = icon;
            SowerOfChange.ViewElementDef.SmallIcon = icon;
            //SowerOfChange.AnimType = -1;
            SharedData Shared = GameUtl.GameComponent<SharedData>();
            AddStatusDamageKeywordDataDef RawVirausDamageKeyword = Helper.CreateDefFromClone(
                Shared.SharedDamageKeywords.ViralKeyword,
                "c03aa65b-9ca2-4665-9370-67fa81144cf3",
                $"RawViral_DamageKeywordDataDef");
            RawVirausDamageKeyword.ApplyOnlyOnHealthDamage = false;

            DamagePayloadEffectDef DamageEffect = Helper.CreateDefFromClone(
                DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerPoisonExplosion_Die_AbilityDef]"),
                "d9870608-797c-428a-8b56-17c1bdadbe27",
                $"E_DamagePayloadEffectDef {skillName}");
            DamageEffect.DamagePayload = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Mutoid_ViralExplode_AbilityDef").DamagePayload;
            DamageEffect.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                //new DamageKeywordPair()
                //{
                //    DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword,
                //    Value = 5
                //},
                //new DamageKeywordPair()
                //{
                //    DamageKeywordDef = Shared.SharedDamageKeywords.PiercingKeyword,
                //    Value = 100
                //},
                //new DamageKeywordPair()
                //{
                //    DamageKeywordDef = Shared.SharedDamageKeywords.ViralKeyword,
                //    Value = 1
                //}
                new DamageKeywordPair()
                {
                    DamageKeywordDef = RawVirausDamageKeyword,
                    Value = 1
                }
            };
            DamageEffect.DamagePayload.DamageType = DefCache.GetDef<DamageTypeBaseEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef");
            DamageEffect.DamagePayload.DamageValue = 2333.0f;
            DamageEffect.DamagePayload.ArmourPiercing = 123.0f;
            DamageEffect.DamagePayload.Speed = 200.0f;
            DamageEffect.DamagePayload.BodyPartMultiplier = 0.0f;
            DamageEffect.DamagePayload.ObjectMultiplier = 0.0f;
            DamageEffect.DamagePayload.DamageDeliveryType = DamageDeliveryType.DirectLine;
            DamageEffect.DamagePayload.AoeRadius = 0.4f;
            DamageEffect.DamagePayload.ObjectToSpawnOnExplosion = null;
            DamageEffect.EffectPositionOffset = new Vector3(0, 0.2f, 0); // prevent to explode in the ground

            OnActorDamageReceivedStatusDef SocStatus = (OnActorDamageReceivedStatusDef)SowerOfChange.StatusDef;
            SocStatus.ApplicationConditions = new EffectConditionDef[0];
            SocStatus.DamageDeliveryTypeFilter = new List<DamageDeliveryType>();
            SocStatus.TargetApplicationConditions = new EffectConditionDef[]
            {
                DefCache.GetDef < EffectConditionDef >("NotOfPhoenixFaction_ApplicationCondition"),
                DefCache.GetDef < EffectConditionDef >("HasCombatantTag_ApplicationCondition")
            };

            SocStatus.EffectForAttacker = DamageEffect;
        }
        // Sower of Chage: Patching OnActorDamageReceivedStatus.OnActorDamageReceived() to handle the trigger effect preventing errors and to much slow motion
        [HarmonyPatch(typeof(OnActorDamageReceivedStatus), "OnActorDamageReceived")]
        internal static class SowerOfChange_OnActorDamageReceived_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(OnActorDamageReceivedStatus __instance, DamageResult damageResult)
            {
                try
                {
                    TacticalActor tacticalActor = __instance.TacticalActor;
                    TacticalAbility SowerOfChange = tacticalActor.GetAbilities<TacticalAbility>().FirstOrDefault(s => s.AbilityDef.name.Equals("SowerOfChange_AbilityDef"));
                    if (SowerOfChange == null)
                    {
                        return true;
                    }
                    PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                    PRMLogger.Debug($"OnActorDamageReceivedStatus.OnActorDamageReceived() called from '{SowerOfChange.AbilityDef.name}' ...");
                    PRMLogger.Debug($"Actor: {tacticalActor.DisplayName}");
                    PRMLogger.Debug($"Recieved HealthDamage: {damageResult.HealthDamage}");
                    if (damageResult.Source == null)
                    {
                        PRMLogger.Debug($"damageResult.Source is NULL, exit without apply effect!");
                        PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        return false;
                    }
                    if (!(damageResult.Source is IDamageDealer damageDealer))
                    {
                        PRMLogger.Debug($"damageResult.Source, type {damageResult.Source.GetType().Name}, is no IDamageDealer, exit without apply effect!");
                        PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        return false;
                    }
                    if (!__instance.OnActorDamageReceivedStatusDef.DamageDeliveryTypeFilter.IsEmpty()
                        && !__instance.OnActorDamageReceivedStatusDef.DamageDeliveryTypeFilter.Contains(damageDealer.GetDamagePayload().DamageDeliveryType))
                    {
                        PRMLogger.Debug($"DamageDeliveryType {damageDealer.GetDamagePayload().DamageDeliveryType} does not fit preset, exit without apply effect!");
                        PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        return false;
                    }
                    TacticalActorBase tacticalActorBase = damageDealer.GetTacticalActorBase();
                    if (tacticalActorBase == null)
                    {
                        PRMLogger.Debug($"damageDealer.GetTacticalActorBase() returned 'null', exit without apply effect!");
                        PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        return false;
                    }
                    PRMLogger.Debug($"TacticalActorBase of target: {tacticalActorBase}");
                    EffectConditionDef[] targetApplicationConditions = __instance.OnActorDamageReceivedStatusDef.TargetApplicationConditions;
                    for (int i = 0; i < targetApplicationConditions.Length; i++)
                    {
                        if (!targetApplicationConditions[i].ConditionMet(tacticalActorBase))
                        {
                            PRMLogger.Debug($"OnActorDamageReceivedStatusDef.TargetApplicationConditions not met, exit without apply effect!");
                            PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                            return false;
                        }
                    }
                    EffectTarget actorEffectTarget = TacUtil.GetActorEffectTarget(tacticalActorBase, null);
                    if (actorEffectTarget == null)
                    {
                        PRMLogger.Debug($"tacticalActorBase.GetActorEffectTarget() of target returned 'null', exit without apply effect!");
                        PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                        return false;
                    }
                    //GameObject effectTargetObject = actorEffectTarget.GameObject;
                    DamagePayloadEffectDef effectDef = (DamagePayloadEffectDef)__instance.OnActorDamageReceivedStatusDef.EffectForAttacker;
                    float viralDamage = 1;
                    //float blastDamage = 0;
                    float timingScale = 0.8f;
                    //blastDamage = effectDef.DamagePayload.DamageKeywords.Find(dk => dk.DamageKeywordDef == Shared.SharedDamageKeywords.BlastKeyword).Value;
                    viralDamage = damageResult.HealthDamage >= 4 ? damageResult.HealthDamage / 4 : 1.0f;
                    AddStatusDamageKeywordDataDef RawVirausDamageKeyword = DefCache.GetDef<AddStatusDamageKeywordDataDef>("RawViral_DamageKeywordDataDef");
                    effectDef.DamagePayload.DamageKeywords.Find(dk => dk.DamageKeywordDef == RawVirausDamageKeyword).Value = viralDamage;
                    //effectDef.DamagePayload.DamageKeywords.Find(dk => dk.DamageKeywordDef == Shared.SharedDamageKeywords.ViralKeyword).Value = viralDamage;
                    tacticalActor.Timing.Scale = timingScale;
                    tacticalActorBase.Timing.Scale = timingScale;
                    PRMLogger.Debug($"'{tacticalActor}' applies {viralDamage} viral damage on '{actorEffectTarget}', position '{actorEffectTarget.Position + effectDef.EffectPositionOffset}'");
                    //Logger.Always($"'{___TacticalActor}' applies {blastDamage} blast and {viralDamage} viral damage on '{effectTargetObject}', position '{actorEffectTarget.Position + effectDef.EffectPositionOffset}'");
                    Effect.Apply(__instance.Repo, effectDef, actorEffectTarget, tacticalActor);
                    tacticalActor.Timing.Scale = timingScale;
                    tacticalActorBase.Timing.Scale = timingScale;
                    PRMLogger.Debug($"Effect applied on {tacticalActorBase}");
                    PRMLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                    return false;
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                    return false;
                }
            }
        }
        private static void Change_BreatheMist()
        {
            // Breathe Mist adding progression def
            ApplyEffectAbilityDef mistBreather = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");
            AbilityCharacterProgressionDef mbProgressionDef = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "9eaf8809-01d9-4582-89e0-78c8596f5e7d",
                "MistBreather_AbilityDef");
            mbProgressionDef.RequiredStrength = 0;
            mbProgressionDef.RequiredWill = 0;
            mbProgressionDef.RequiredSpeed = 0;
            //Adjusting for Mutoids Voland
            mbProgressionDef.MutagenCost = 30;
            mistBreather.CharacterProgressionData = mbProgressionDef;
        }
        private static void Change_Resurrect()
        {
            ResurrectAbilityDef resurrect = DefCache.GetDef<ResurrectAbilityDef>("Mutoid_ResurrectAbilityDef");
            resurrect.ActionPointCost = 0.75f;
            resurrect.WillPointCost = 10;
            resurrect.UsesPerTurn = 1;
        }

        private static void Change_PepperCloud()
        {
            float pcRange = 8.0f;
            ApplyStatusAbilityDef pepperCloud = DefCache.GetDef<ApplyStatusAbilityDef>("Mutoid_PepperCloud_ApplyStatusAbilityDef");
            pepperCloud.TargetingDataDef.Origin.Range = pcRange;
            pepperCloud.ViewElementDef.Description.LocalizationKey = "PR_BC_PEPPER_CLOUD_DESC"; // new LocalizedTextBind($"Reduces Accuracy by 50% of all organic enemies within {pcRange} tiles for 1 turn.", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            // change Pepper Cloud to reduce accuracy and perception to 25%
            // DEACTIVATED: Pepper Cloud uses the Trembling status that is used by a couple of other abilities, needs to be cloned, also for Acherons PC
            //StatMultiplierStatusDef pepperCloudStatus = (StatMultiplierStatusDef)pepperCloud.StatusDef;
            //pepperCloudStatus.StatsMultipliers = new StatMultiplier[]
            //{
            //    new StatMultiplier()
            //    {
            //        StatName = "Accuracy",
            //        Multiplier = 0.25f,
            //    },
            //
            //    new StatMultiplier()
            //    {
            //        StatName = "Perception",
            //        Multiplier = 0.25f,
            //    },
            //};
        }

        private static void Create_AR_Targeting()
        {
            string skillName = "BC_ARTargeting_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("DeterminedAdvance_AbilityDef");
            ApplyStatusAbilityDef arTargeting = Helper.CreateDefFromClone(
                source,
                "ad95d7cb-b172-4e0d-acc5-e7e514fcb824",
                skillName);
            arTargeting.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "143a743b-e42e-4f65-9f83-f76bf42c733b",
                skillName);
            arTargeting.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "7019bd7f-d30d-4ce8-9c3d-0b6161bd4ee0",
                skillName);
            StanceStatusDef artStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<StanceStatusDef>("StomperLegs_StabilityStance_StatusDef"),
                "56b4ea0e-d0cc-4fc9-b6cf-26e45b2dc81c",
                "ARTargeting_Stance_StatusDef");
            artStatus.DurationTurns = 0;
            artStatus.SingleInstance = true;
            artStatus.Visuals = arTargeting.ViewElementDef;
            artStatus.StatModifications = new ItemStatModification[]
            { 
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = 0.3f
                }
            };

            arTargeting.StatusDef = artStatus;

            arTargeting.ActionPointCost = 0;
            arTargeting.WillPointCost = 2;
            arTargeting.UsesPerTurn = 8;

            arTargeting.CharacterProgressionData.RequiredSpeed = 0;
            arTargeting.CharacterProgressionData.RequiredStrength = 0;
            arTargeting.CharacterProgressionData.RequiredWill = 0;
            arTargeting.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_AR_TARGETTING"; // new LocalizedTextBind("AR TARGETING", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            arTargeting.ViewElementDef.Description.LocalizationKey = "PR_BC_AR_TARGETTING_DESC"; // new LocalizedTextBind("Target ally gains +30% accuracy", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite artIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_AR_Targeting_2-2.png");
            arTargeting.ViewElementDef.LargeIcon = artIcon;
            arTargeting.ViewElementDef.SmallIcon = artIcon;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(arTargeting))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(arTargeting).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }

        private static void Create_Endurance()
        {
            // Harmony patch RecoverWillAbility.GetWillpowerRecover
            // Adding an ability that get checked in the patched method (see below)
            string skillName = "Endurance_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
            PassiveModifierAbilityDef endurance = Helper.CreateDefFromClone(
                source,
                "4e9712b6-8a46-489d-9553-fdc1380c334a",
                skillName);
            endurance.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "ffc75f46-adf0-4683-b28c-a59e91a99843",
                skillName);
            endurance.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "75155fd6-7cef-40d8-a03d-28bdb3dc0929",
                skillName);
            // reset all possible passive modifications, we need none, this ability is only to have something to chose and as flag for the Endurance Harmony patch
            endurance.StatModifications = new ItemStatModification[0];
            endurance.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            endurance.DamageKeywordPairs = new DamageKeywordPair[0];
            // Set necessary fields
            endurance.CharacterProgressionData.RequiredSpeed = 0;
            endurance.CharacterProgressionData.RequiredStrength = 0;
            endurance.CharacterProgressionData.RequiredWill = 0;
            endurance.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_ENDURANCE"; // new LocalizedTextBind("ENDURANCE", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            endurance.ViewElementDef.Description.LocalizationKey = "PR_BC_ENDURANCE_DESC"; // new LocalizedTextBind("Recover restores 75% WP", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite enduranceIcon = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Reckless_AbilityDef]").LargeIcon;
            endurance.ViewElementDef.LargeIcon = enduranceIcon;
            endurance.ViewElementDef.SmallIcon = enduranceIcon;
        }
        // Endurance: Patching GetWillpowerRecover from active actor when he uses Recover to check if Endurance ability is active and return 75% WP to recover
        [HarmonyPatch(typeof(RecoverWillAbility), "GetWillpowerRecover")]
        internal static class RecoverWillAbility_GetWillpowerRecover_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(ref float __result, RecoverWillAbility __instance)
            {
                //TacticalActor ___TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacticalAbility), "TacticalActor").GetValue(__instance, null);
                TacticalActor tacticalActor = __instance.TacticalActor;
                TacticalAbility endurance = tacticalActor.GetAbilities<TacticalAbility>().FirstOrDefault(s => s.AbilityDef.name.Equals("Endurance_AbilityDef"));
                if (endurance != null)
                {
                    // Set amount of WP recovered to 75% of max WP
                    __result = Mathf.Ceil(tacticalActor.CharacterStats.WillPoints.Max * 75 / 100f);
                }
                else
                {
                    __result = Mathf.Ceil(tacticalActor.CharacterStats.WillPoints.Max * __instance.RecoverWillAbilityDef.WillPointsReturnedPerc / 100f);
                }
            }
        }
        [HarmonyPatch(typeof(RecoverWillAbility), "Activate")]
        internal static class RecoverWillAbility_Activate_Patch
        {
            public static void Postfix(RecoverWillAbility __instance)
            {
                TacticalActor tacticalActor = __instance.TacticalActor;
                TacticalAbility endurance = tacticalActor.GetAbilities<TacticalAbility>().FirstOrDefault(s => s.AbilityDef.name.Equals("Endurance_AbilityDef"));
                if (endurance != null)
                {
                    // Reduce Bleed and Poison by half and remove acid if existent on actor
                    List<string> effectNames = new List<string>() { "Acid", "Bleed", "Poison" };
                    tacticalActor.Status.UnapplyAllStatusesFiltered(s => s is TacStatus && effectNames.Contains((s as TacStatus).TacStatusDef.EffectName));
                }
            }
        }
    }
}
