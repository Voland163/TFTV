using Base.AI;
using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        /// <summary>
        /// AI actions for the player class skills that enemies now get by level (TFTVHumanEnemies.GrantClassSkills).
        /// Passive skills (Ready for Action, Quarterback, Killer Instinct, Biochemist, Remote Deployment) need nothing
        /// here. Each active skill gets an action built from a stock pattern:
        ///
        /// - self buffs before a shot (Quick Aim, Armor Break, Boom Blast, Adrenaline Rush) and Hunker Down: an
        ///   ExecuteAbility on self, behind a gate that only opens when the buff pays off this turn;
        /// - special shots (Aimed Burst, Gunslinger, Gun Kata, Rage Burst): MoveAndShoot clones confined to the
        ///   weapons the skill uses, whose attack is the skill instead of the weapon's own shot;
        /// - targeted skills (Marked for Death, Induce Panic, Remote Control): MoveAndExecuteAbility / ExecuteAbility
        ///   on the skill's own targets, scored here;
        /// - free moves (Kill'n'Run's dash, Vanish): Dash clones whose spots are scored by cover;
        /// - jumps (jet jump, Jetpack Control): LeapAndAttack clones, jump to a spot and shoot.
        ///
        /// It also reworks Electric Reinforcement (only when two or more threatened allies lack it) and the AI's heal
        /// (pick the heal that suits the patient, count disabled body parts).
        /// </summary>
        internal static class ClassSkillsAI
        {
            private static readonly Dictionary<BaseDef, Func<TacticalActor, float>> Gates = new Dictionary<BaseDef, Func<TacticalActor, float>>();
            private static readonly Dictionary<BaseDef, Func<TacticalActor, TacAITarget, float>> Scores = new Dictionary<BaseDef, Func<TacticalActor, TacAITarget, float>>();
            private static readonly Dictionary<BaseDef, ShootAbilityDef> SpecialShots = new Dictionary<BaseDef, ShootAbilityDef>();
            private static readonly HashSet<BaseDef> CoverMoves = new HashSet<BaseDef>();

            private static AIHasEquipmentConsiderationDef _hasGun;
            private static AIRoofAboveConsiderationDef _roof;
            private static AIActionMoveAndExecuteAbilityDef _remoteControlAction;
            private static AIActionMoveAndExecuteAbilityDef _silenceAction;
            private static StatusDef _silencedStatus;
            private static GameTagDef _vehicleTag;

            /// <summary>Units that cast Neural Disruption this turn.</summary>
            private static readonly HashSet<TacticalActor> SilencedThisTurn = new HashSet<TacticalActor>();

            private const float DemolitionReach = 20f;

            private static ApplyStatusAbilityDef _electricReinforcement;
            private static StatusDef _markedForDeathStatus;
            private static GameTagDef _explosiveTag;
            private static GameTagDef _grenadeLauncherTag;

            /// <summary>A unit's AP the first time it is looked at in a turn: "has it acted yet".</summary>
            private static readonly Dictionary<TacticalActor, float> TurnStartAP = new Dictionary<TacticalActor, float>();

            /// <summary>Which heal ability the heal scoring picked for each candidate spot.</summary>
            private static readonly ConditionalWeakTable<TacAITarget, HealAbility> ChosenHeal = new ConditionalWeakTable<TacAITarget, HealAbility>();

            /// <summary>Set by the heal action's Execute prefix, read by the transpiled pick inside its coroutine.</summary>
            private static TacticalActor _healActor;
            private static TacAITarget _healTarget;

            private const float DisabledPartHealValue = 40f;
            private const float ArmourBreakMinArmour = 15f;
            private const float AdrenalineReach = 15f;
            private const float ThreatRange = 30f;

            #region defs

            internal static void CreateDefs()
            {
                try
                {
                    _hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    _roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");
                    _explosiveTag = DefCache.GetDef<GameTagDef>("ExplosiveWeapon_TagDef");
                    _grenadeLauncherTag = DefCache.GetDef<GameTagDef>("GrenadeLauncherItem_TagDef");
                    _vehicleTag = CommonHelpers.GetSharedGameTags().VehicleTag;

                    CreateSelfBuffs();
                    CreateSpecialShots();
                    CreateTargeted();
                    CreateMoves();
                    CreateJumps();
                    ReworkElectricReinforcement();
                    ReworkNeuralDisruption();
                    ReworkDemolitionMan();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static AIAdjustedConsideration Linear(AIConsiderationDef def)
            {
                return new AIAdjustedConsideration { Consideration = def, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) };
            }

            private static AIHasEquipmentConsiderationDef Gate(string name, string guid, Func<TacticalActor, float> score)
            {
                AIHasEquipmentConsiderationDef def = Helper.CreateDefFromClone(_hasGun, guid, name);
                Gates[def] = score;
                return def;
            }

            private static AIRoofAboveConsiderationDef Score(string name, string guid, Func<TacticalActor, TacAITarget, float> score)
            {
                AIRoofAboveConsiderationDef def = Helper.CreateDefFromClone(_roof, guid, name);
                Scores[def] = score;
                return def;
            }

            private static AITargetEvaluation Evaluation(AITargetGeneratorDef generator, AITargetEvaluation copyFrom, params AIConsiderationDef[] considerations)
            {
                return new AITargetEvaluation
                {
                    TargetGeneratorDef = generator,
                    FallbackTargetGeneratorDef = null,
                    Considerations = considerations.Select(Linear).ToArray(),
                    TopScoresToConsiderPerc = copyFrom.TopScoresToConsiderPerc,
                    MinNumberOfTargetsPerc = copyFrom.MinNumberOfTargetsPerc,
                    MaxNumberOfTargetsPerc = copyFrom.MaxNumberOfTargetsPerc,
                    MinNumberOfTargets = copyFrom.MinNumberOfTargets
                };
            }

            private static T Ability<T>(string name) where T : TacticalAbilityDef
            {
                T def = DefCache.GetDef<T>(name);

                if (def == null)
                {
                    TFTVLogger.Always($"[ClassSkills AI] WARNING: {name} not found");
                }

                return def;
            }

            /// <summary>Execute a skill on oneself, scored by <paramref name="value"/> once <paramref name="gate"/> opens.</summary>
            private static AIActionExecuteAbilityDef SelfAction(string name, string guid, string gateGuid, string valueGuid, TacticalAbilityDef ability,
                float weight, Func<TacticalActor, float> gate, Func<TacticalActor, TacAITarget, float> value = null)
            {
                AIActionExecuteAbilityDef source = DefCache.GetDef<AIActionExecuteAbilityDef>("InstilFrenzy_AIActionDef");
                AIActionExecuteAbilityDef action = Helper.CreateDefFromClone(source, guid, name);

                action.Weight = weight;
                action.AbilityDefs = new TacticalAbilityDef[] { ability };
                action.EarlyExitConsiderations = new[] { Linear(Gate(name + "_Gate", gateGuid, gate)) };
                action.Evaluations = new[]
                {
                    Evaluation(source.Evaluations[0].TargetGeneratorDef, source.Evaluations[0],
                        Score(name + "_Value", valueGuid, value ?? ((a, t) => 1f)))
                };

                AddActionToTemplate(SoldierAITemplate, action);
                return action;
            }

            private static void CreateSelfBuffs()
            {
                ApplyStatusAbilityDef quickAim = Ability<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef");
                ApplyStatusAbilityDef armourBreak = Ability<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef");
                ApplyStatusAbilityDef boomBlast = Ability<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
                ApplyStatusAbilityDef adrenalineRush = Ability<ApplyStatusAbilityDef>("AdrenalineRush_AbilityDef");
                ApplyStatusAbilityDef hunkerDown = Ability<ApplyStatusAbilityDef>("HunkerDown_AbilityDef");

                if (quickAim != null)
                {
                    SelfAction("TFTV_QuickAim_AIActionDef", "f36aaac3-2dd1-4f27-ae3c-c7e715f2dde3", "f8fbabcf-c37c-4e39-ab57-fcc78094baff", "23ed7e1d-c020-442c-9cf9-4b38f9aee551", quickAim, 500f, a => QuickAimPays(a, quickAim) ? 1f : 0f);
                }

                if (armourBreak != null)
                {
                    SelfAction("TFTV_ArmourBreak_AIActionDef", "21af3264-864c-443f-88b2-0925ed5bb06d", "86ff0705-070f-46c8-b3d3-7d715f2e3205", "65ad70c8-6646-4da3-81bc-09d63daf2ac5", armourBreak, 400f, a => ArmourBreakPays(a, armourBreak) ? 1f : 0f);
                }

                if (boomBlast != null)
                {
                    SelfAction("TFTV_BoomBlast_AIActionDef", "0f8973fd-991a-4782-93ed-fd4f6c5ae30a", "06d17b49-94b1-41a8-b309-38ecec42ad99", "1c6c21bf-ec26-4fd8-9edd-f74858dbe52e", boomBlast, 300f, a => BoomBlastPays(a, boomBlast) ? 1f : 0f);
                }

                if (adrenalineRush != null)
                {
                    SelfAction("TFTV_AdrenalineRush_AIActionDef", "628c3476-1f31-42f0-8d63-24d4da2e4e6a", "2413f72d-556a-4eb3-bbe7-8453e16537b0", "9b05a5bd-c3fc-470d-8294-faeaade2fed7", adrenalineRush, 300f, a => AdrenalineRushPays(a, adrenalineRush) ? 1f : 0f);
                }

                // Hunker Down ends the turn, so it only replaces the end of a turn spent exposed with nothing left to shoot.
                if (hunkerDown != null)
                {
                    SelfAction("TFTV_HunkerDown_AIActionDef", "3dbaa9ea-de8f-4c13-940c-bf434fbbdcc6", "c5140524-cd28-4453-aa46-c56804f46850", "cdf604ec-e999-4d41-a510-2422b11a2bca", hunkerDown, 0.9f, a => HunkerDownPays(a, hunkerDown) ? 1f : 0f);
                }
            }

            private static void CreateSpecialShots()
            {
                GameTagDef assaultRifle = DefCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef");
                GameTagDef handgun = DefCache.GetDef<GameTagDef>("HandgunItem_TagDef");
                GameTagDef gun = DefCache.GetDef<GameTagDef>("GunWeapon_TagDef");

                ShootAbilityDef aimedBurst = Ability<ShootAbilityDef>("AimedBurst_AbilityDef");
                ShootAbilityDef gunslinger = Ability<ShootAbilityDef>("BC_Gunslinger_AbilityDef");
                ShootAbilityDef gunKata = Ability<ShootAbilityDef>("GunKata_AbilityDef");
                RageBurstInConeAbilityDef rageBurst = Ability<RageBurstInConeAbilityDef>("RageBurst_RageBurstInConeAbilityDef");

                // Two bursts with an assault rifle for 3 AP and 4 WP, against one for the normal shot.
                if (aimedBurst != null)
                {
                    SpecialShot("TFTV_AimedBurst_AIActionDef", "685d1d38-12dd-4eb3-95cd-4cf2c4142097", "0e81966b-c6f3-497c-93fd-ac257436fc6f", "449b351c-6189-4228-93a8-9815d6a79956", "7fcb55bb-7fdd-4bf2-8857-bd8a0371705e", null, aimedBurst, 110f, new[] { assaultRifle }, false);
                }

                // Three handgun shots at half accuracy, about one and a half shots.
                if (gunslinger != null)
                {
                    SpecialShot("TFTV_Gunslinger_AIActionDef", "574f0693-8cef-4eb5-99e8-26a9680169e3", "45e1c51d-3da2-4fae-bdfa-a0a3c28444b2", "9488c02d-6ad2-4930-ac02-bc9826bd44b7", "634f7d87-8286-4dde-b81a-31bbc9dc7372", null, gunslinger, 110f, new[] { handgun }, false);
                }

                // Free handgun shots, twice a turn: from where the unit stands, so they never cost a move.
                if (gunKata != null)
                {
                    SpecialShot("TFTV_GunKata_AIActionDef", "5e287769-b45d-4ace-a148-5b5c0976f751", "fb38eba3-995b-42e1-aa54-685504287438", "95f8d7f0-f62e-45b0-b2ee-cc117ff4f670", "fcfd8251-0e9c-42d6-ad60-094cd94bef19", "dd2508b9-cf67-4052-b117-f6f02865bd7d", gunKata, 150f, new[] { handgun }, true);
                }

                // Five shots across a cone: only worth it with two or more enemies in the cone.
                if (rageBurst != null)
                {
                    GameTagDef[] tags = rageBurst.EquipmentTags != null && rageBurst.EquipmentTags.Length > 0 ? rageBurst.EquipmentTags : new[] { gun };
                    AIActionMoveAndAttackDef action = SpecialShot("TFTV_RageBurst_AIActionDef", "ee65f4ef-640f-4319-884b-7fc006af436a", "0efcc9e9-4aa0-43e9-986f-2cf1f857f5f6", "f1ac8629-f9bd-4b26-95ef-12b3317efc50", "e237bc23-25a8-4acf-b1e1-401666f7bf8e", null, rageBurst, 150f, tags, false);

                    AITargetEvaluation enemies = action.Evaluations.FirstOrDefault(e => e.Considerations.Any(c => c.Consideration is AIAttackPositionConsiderationDef));

                    if (enemies != null)
                    {
                        enemies.Considerations = enemies.Considerations.AddToArray(Linear(Score("TFTV_RageBurstCone_AIConsiderationDef", "608d543e-0487-4967-b6a7-fa177fa1ce8f",
                            (a, t) => RageBurstConeValue(a, t, rageBurst))));
                    }
                }
            }

            /// <summary>A MoveAndShoot clone whose attack is <paramref name="ability"/> with one of the weapons it allows.</summary>
            private static AIActionMoveAndAttackDef SpecialShot(string name, string guid, string generatorGuid, string gateGuid, string apGuid, string zoneGuid,
                ShootAbilityDef ability, float weight, GameTagDef[] weaponTags, bool ownTileOnly)
            {
                AIActionMoveAndAttackDef source = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");
                AIActionMoveAndAttackDef action = Helper.CreateDefFromClone(source, guid, name);
                action.Weight = weight;

                AIActorEquipmentTargetGeneratorDef weapons = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActorEquipmentTargetGeneratorDef>("Gun_AITargetGeneratorDef"), generatorGuid, name.Replace("_AIActionDef", "_AITargetGeneratorDef"));
                weapons.EquipmentTags = weaponTags.Where(t => t != null).ToArray();

                ReplaceEquipmentGenerator(action, weapons);
                RemoveConsiderations(action, typeof(AICanUseEquipmentConsiderationDef), typeof(AIHasEquipmentConsiderationDef));

                action.EarlyExitConsiderations = action.EarlyExitConsiderations.AddToArray(Linear(Gate(name + "_Gate", gateGuid,
                    a => SkillUsable(a, ability) && UsableWeapons(a, weaponTags).Any() ? 1f : 0f)));

                AITargetEvaluation positions = action.Evaluations.First(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);

                if (ownTileOnly)
                {
                    AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                        (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef, zoneGuid, name.Replace("_AIActionDef", "_Zone_AITargetGeneratorDef"));
                    zone.ZoneRange = 0f;
                    zone.IncludeSelfActorPosition = true;
                    positions.TargetGeneratorDef = zone;
                }

                positions.Considerations = positions.Considerations.AddToArray(Linear(Score(name + "_AP", apGuid,
                    (a, t) => (ownTileOnly && !Stances.IsOwnTile(a, t.Pos)) ? 0f : (APLeftAt(a, t) >= ShotCost(a, ability, t.Equipment as Weapon) - 0.01f ? 1f : 0f))));

                // MoveAndShoot already carries the holders' cover check (Stances), and the clone inherits it.
                SpecialShots[action] = ability;
                AddActionToTemplate(SoldierAITemplate, action);
                return action;
            }

            private static void CreateTargeted()
            {
                ApplyStatusAbilityDef markedForDeath = Ability<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef");
                ApplyStatusAbilityDef inducePanic = Ability<ApplyStatusAbilityDef>("InducePanic_AbilityDef");
                ApplyStatusAbilityDef remoteControl = Ability<ApplyStatusAbilityDef>("ManualControl_AbilityDef");

                // Mark the enemy the squad is about to shoot, from where the sniper stands, keeping the AP for its own shot.
                if (markedForDeath != null)
                {
                    _markedForDeathStatus = markedForDeath.StatusDef;

                    TargetedAction("TFTV_MarkedForDeath_AIActionDef", "a33d036b-e71e-4397-a200-62e31bb94e9e", "0061aedc-f117-4f0c-8130-602a666ebdcf", "c021b4ea-5193-4f97-8803-632bcda17a55", "04733b4e-950f-4bfb-af78-f8019f156823", "21bb514f-727f-4ff0-a315-ac02a4210cdc", "5a7ae798-5f77-43f0-a0fa-d8cf2508af5c", markedForDeath, 250f, true,
                        (a, t) => APLeftAt(a, t) >= AbilityCost(a, markedForDeath) + BestShotCost(a) - 0.01f ? 1f : 0f,
                        (a, t) => MarkValue(a, t));
                }

                // Panic whoever the priest out-wills, from a spot with some cover.
                if (inducePanic != null)
                {
                    TargetedAction("TFTV_InducePanic_AIActionDef", "92db5018-83a9-4e59-b8c3-8573984285d2", "877486fe-4a0a-47ee-81e6-08ffb4dda8c3", "1b0a50e6-27ed-4235-ac57-f63e0e6b0edd", "13473ac3-1139-4235-ba35-db7c6ebcdd8e", "b9f45bf4-0447-426d-b822-d26d59b63889", "64a1e79e-5674-45c8-8e5e-649ea44213a7", inducePanic, 120f, false,
                        (a, t) => APLeftAt(a, t) >= AbilityCost(a, inducePanic) - 0.01f && !Stances.CrossesOverwatch(a, t.Pos) ? Mathf.Max(0.4f, SpotCover(a, t.Pos)) : 0f,
                        (a, t) => PanicValue(a, t));
                }

                // Give an own turret with something to shoot a free extra attack, from where the technician stands.
                if (remoteControl != null)
                {
                    _remoteControlAction = TargetedAction("TFTV_RemoteControl_AIActionDef", "59888d5c-4936-4761-8e4f-e14889de86ce", "007700d4-706a-4afb-be61-3a0a65ac68d3", "444bac5e-cc13-4156-8346-086b26013d65", "82c74040-436a-4669-8e1a-f46600b79afe", "40349cfe-d5b1-4050-b9c0-cf4b5d3bdf1e", "77baa588-7106-4385-8260-b15a3e39b739", remoteControl, 200f, true,
                        (a, t) => APLeftAt(a, t) >= AbilityCost(a, remoteControl) - 0.01f ? 1f : 0f,
                        (a, t) => TurretShot(t.Actor as TacticalActor, out _) != null && t.Actor.TacticalFaction == a.TacticalFaction ? 1f : 0f);
                }
            }

            /// <summary>MoveAndDoSilence pattern: pick a spot (own tile, or within half a move), then one of the skill's targets from it.</summary>
            private static AIActionMoveAndExecuteAbilityDef TargetedAction(string name, string guid, string gateGuid, string zoneGuid, string targetsGuid,
                string positionGuid, string targetGuid, TacticalAbilityDef ability, float weight, bool ownTileOnly,
                Func<TacticalActor, TacAITarget, float> position, Func<TacticalActor, TacAITarget, float> target)
            {
                AIActionMoveAndExecuteAbilityDef source = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoSilence_AIActionDef");
                AIActionMoveAndExecuteAbilityDef action = Helper.CreateDefFromClone(source, guid, name);

                action.Weight = weight;
                action.AbilityToExecute = ability;
                action.AbilityTypeParameter = false;
                action.EarlyExitConsiderations = new[]
                {
                    source.EarlyExitConsiderations[0],
                    Linear(Gate(name + "_Gate", gateGuid, a => SkillUsable(a, ability) ? 1f : 0f))
                };

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)source.Evaluations[0].TargetGeneratorDef, zoneGuid, name.Replace("_AIActionDef", "_Zone_AITargetGeneratorDef"));
                zone.ZoneRange = ownTileOnly ? 0f : 0.5f;
                zone.IncludeSelfActorPosition = true;

                AIAbilityTargetsTargetGeneratorDef targets = Helper.CreateDefFromClone(
                    (AIAbilityTargetsTargetGeneratorDef)source.Evaluations[1].TargetGeneratorDef, targetsGuid, name.Replace("_AIActionDef", "_Targets_AITargetGeneratorDef"));
                targets.AbilityDef = ability;
                targets.AbilityTypeParameter = false;

                action.Evaluations = new[]
                {
                    Evaluation(zone, source.Evaluations[0], Score(name + "_Position", positionGuid,
                        (a, t) => ownTileOnly && !Stances.IsOwnTile(a, t.Pos) ? 0f : position(a, t))),
                    Evaluation(targets, source.Evaluations[1], Score(name + "_Target", targetGuid, target))
                };

                AddActionToTemplate(SoldierAITemplate, action);
                return action;
            }

            private static void CreateMoves()
            {
                RepositionAbilityDef knrDash = Ability<RepositionAbilityDef>("KillAndRun_Dash_AbilityDef");
                RepositionAbilityDef vanish = Ability<RepositionAbilityDef>("Vanish_AbilityDef");

                // The free move after a kill: back into cover once there is nothing left to shoot from here.
                if (knrDash != null)
                {
                    CoverMove("TFTV_KillAndRun_AIActionDef", "5ba831cb-99f4-4a4e-90b3-bb142a19d675", "aaf1a3a5-aee5-4da6-b47f-688b86718e29", "f7914b9b-d950-4e2c-b272-cd8045d20dad", "2669844a-14e8-4d3b-a790-e2cf7963457d", knrDash, 5f,
                        a => SkillUsable(a, knrDash) && (PeekAndFallBackAI.IsSeen(a) || !HasAnyShot(a)) ? 1f : 0f);
                }

                // Out of sight again once the enemy has seen the infiltrator.
                if (vanish != null)
                {
                    CoverMove("TFTV_Vanish_AIActionDef", "003ed302-7c74-4a43-8f8d-170b6114ebc1", "96e21308-18e3-4b37-bca7-242c8a0d5b49", "ca6e575f-7996-4418-925f-46a6ed01ade0", "a14bacb6-256d-473e-92b4-42884846b359", vanish, 6f,
                        a => SkillUsable(a, vanish) && PeekAndFallBackAI.IsSeen(a) ? 1f : 0f);
                }
            }

            private static void CoverMove(string name, string guid, string gateGuid, string zoneGuid, string scoreGuid,
                TacticalAbilityDef moveAbility, float weight, Func<TacticalActor, float> gate)
            {
                AIActionMoveToPositionDef source = DefCache.GetDef<AIActionMoveToPositionDef>("Dash_AIActionDef");
                AIActionMoveToPositionDef action = Helper.CreateDefFromClone(source, guid, name);
                action.Weight = weight;

                AITargetEvaluation sourcePositions = source.Evaluations.First(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);
                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)sourcePositions.TargetGeneratorDef, zoneGuid, name.Replace("_AIActionDef", "_Zone_AITargetGeneratorDef"));
                zone.MoveAbilityDef = moveAbility;
                zone.ZoneRange = 1f;
                zone.IncludeSelfActorPosition = false;

                action.EarlyExitConsiderations = new[] { Linear(Gate(name + "_Gate", gateGuid, gate)) };
                action.Evaluations = new[]
                {
                    Evaluation(zone, sourcePositions, Score(name + "_Spot", scoreGuid,
                        (a, t) => Stances.CrossesOverwatch(a, t.Pos) ? 0f : HiddenOrCovered(a, t.Pos)))
                };

                CoverMoves.Add(action);
                AddActionToTemplate(SoldierAITemplate, action);
            }

            private static void CreateJumps()
            {
                AIActionMoveAndAttackDef leapAndAttack = DefCache.GetDef<AIActionMoveAndAttackDef>("LeapAndAttack_AIActionDef");

                JetJumpAbilityDef jetJump = Ability<JetJumpAbilityDef>("JetJump_AbilityDef");
                JetJumpAbilityDef jetpackControl = Ability<JetJumpAbilityDef>("JetpackControl_AbilityDef");

                if (jetJump != null)
                {
                    JumpAndShoot("TFTV_JetJumpAndShoot_AIActionDef", "7f9ff2a6-1033-42fd-a610-7749eed9184d", "d0f1445e-2c1e-4a4d-86db-6ebb3c6541ff", "cdf1825b-096a-468b-91ee-f35a403a8539", leapAndAttack, jetJump);
                }

                if (jetpackControl != null)
                {
                    JumpAndShoot("TFTV_JetpackControlAndShoot_AIActionDef", "354e9a0c-df39-4668-b3f5-65c03b4856ce", "6ba12272-6b27-4934-ae1a-982b76d32396", "0487c7f2-6d25-4d87-af28-2905c5317380", leapAndAttack, jetpackControl);
                }

                // Bionic review: the Exo leap-and-shoot outweighed walking and shooting (100 against 75) though the leap
                // costs 3 WP, and took holders into the open. Now it wins only when the leap finds a clearly better shot.
                leapAndAttack.Weight = 60f;

                if (Stances.Enabled)
                {
                    Stances.AddCoveredWhenHolding(leapAndAttack);
                }
            }

            private static void JumpAndShoot(string name, string guid, string zoneGuid, string gateGuid, AIActionMoveAndAttackDef leapAndAttack, JetJumpAbilityDef jump)
            {
                AIActionMoveAndAttackDef action = Helper.CreateDefFromClone(leapAndAttack, guid, name);
                action.Weight = 60f;

                AITargetEvaluation positions = action.Evaluations.First(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);
                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef, zoneGuid, name.Replace("_AIActionDef", "_Zone_AITargetGeneratorDef"));
                zone.MoveAbilityDef = jump;
                positions.TargetGeneratorDef = zone;

                // The leap's own "can leap" gate becomes "can make this jump".
                action.EarlyExitConsiderations = action.EarlyExitConsiderations
                    .Where(c => !(c.Consideration is AIAbilityDisabledStateConsiderationDef))
                    .ToArray()
                    .AddToArray(Linear(Gate(name + "_Gate", gateGuid, a => SkillUsable(a, jump) ? 1f : 0f)));

                if (Stances.Enabled)
                {
                    Stances.AddCoveredWhenHolding(action);
                }

                AddActionToTemplate(SoldierAITemplate, action);
            }

            /// <summary>
            /// Electric Reinforcement was weighted 999 and fired whenever any ally stood within reach, every turn, by every
            /// technician. Now it needs two or more allies in reach who lack it and can be seen by a known enemy.
            /// </summary>
            private static void ReworkElectricReinforcement()
            {
                _electricReinforcement = Ability<ApplyStatusAbilityDef>("ElectricReinforcement_AbilityDef");
                AIActionExecuteAbilityDef action = DefCache.GetDef<AIActionExecuteAbilityDef>("ElectricReinforcement_AIActionDef");

                if (action == null || _electricReinforcement == null)
                {
                    return;
                }

                action.Weight = 150f;
                action.Evaluations[0].Considerations = new[]
                {
                    Linear(Score("TFTV_ElectricReinforcementValue_AIConsiderationDef", "efe66f8d-6ca7-4e81-be9e-2c3a767db284", (a, t) => ElectricReinforcementValue(a)))
                };
            }

            /// <summary>
            /// Neural Disruption (NJ Exo helmet: 1 AP, 3 WP, no per-turn limit) scored its targets by "active abilities
            /// costing 0 WP or more", which counts plain shooting, so every soldier looked like a prime target and the
            /// unit kept casting until its WP fell under half. Now: once per unit per turn, only on a target that is not
            /// already silenced, has WP abilities it can still pay for, and isn't the mind control's.
            /// </summary>
            private static void ReworkNeuralDisruption()
            {
                AIActionMoveAndExecuteAbilityDef silence = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoSilence_AIActionDef");
                _silencedStatus = DefCache.GetDef<StatusDef>("ActorSilenced_StatusDef");

                if (silence == null || silence.Evaluations.Length < 2)
                {
                    return;
                }

                _silenceAction = silence;

                silence.EarlyExitConsiderations = silence.EarlyExitConsiderations.AddToArray(Linear(Gate("TFTV_SilenceOncePerTurn_Gate", "dfcab077-f109-4795-8ec7-00cf8e74b234",
                    a => SilencedThisTurn.Contains(a) ? 0f : 1f)));

                silence.Evaluations[1].Considerations = new[]
                {
                    Linear(Score("TFTV_SilenceTargetValue_AIConsiderationDef", "3f2e365b-ee9d-417d-951d-8dd7c0d6042e", (a, t) => SilenceValue(a, t)))
                };
            }

            /// <summary>
            /// Demolition Man (Juggernaut torso: 2 WP, lasts to the end of the turn) went off at the start of every turn
            /// once alerted. Now only when the unit has no shot from where it stands and a known enemy close by is behind
            /// a wall from it: the turn it might actually walk through one.
            /// </summary>
            private static void ReworkDemolitionMan()
            {
                AIActionExecuteAbilityDef demolition = DefCache.GetDef<AIActionExecuteAbilityDef>("DemolitionMan_AIActionDef");

                if (demolition == null)
                {
                    return;
                }

                demolition.EarlyExitConsiderations = demolition.EarlyExitConsiderations.AddToArray(Linear(Gate("TFTV_DemolitionManWorthIt_Gate", "903b95ff-54c4-4c14-b7a1-516e6de3dedd",
                    a => DemolitionPays(a) ? 1f : 0f)));
            }

            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (def == null || actor == null || !Gates.TryGetValue(def, out Func<TacticalActor, float> gate))
                {
                    return false;
                }

                score = gate(actor);
                return true;
            }

            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 0f;

                if (def == null || actor == null || target == null || !Scores.TryGetValue(def, out Func<TacticalActor, TacAITarget, float> scorer))
                {
                    return false;
                }

                score = scorer(actor, target);
                return true;
            }

            internal static bool IsCoverMove(BaseDef def)
            {
                return def != null && CoverMoves.Contains(def);
            }

            internal static void ClearTurn()
            {
                TurnStartAP.Clear();
                SilencedThisTurn.Clear();
            }

            #endregion

            #region conditions

            private static TacticalAbility SkillOf(TacticalActor actor, AbilityDef def)
            {
                return def == null ? null : actor.GetAbilityWithDef<TacticalAbility>(def);
            }

            private static bool SkillUsable(TacticalActor actor, AbilityDef def)
            {
                TacticalAbility ability = SkillOf(actor, def);
                return ability != null && ability.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected);
            }

            private static float APMax(TacticalActor actor)
            {
                return Mathf.Max(1f, (float)actor.CharacterStats.ActionPoints.Max);
            }

            private static float AP(TacticalActor actor)
            {
                return actor.CharacterStats.ActionPoints;
            }

            private static float APLeftAt(TacticalActor actor, TacAITarget target)
            {
                return AP(actor) - (target?.PathLength ?? 0f);
            }

            private static float AbilityCost(TacticalActor actor, TacticalAbilityDef def)
            {
                TacticalAbility ability = SkillOf(actor, def);
                return ability != null ? ability.ActionPointCost : def.ActionPointCost * APMax(actor);
            }

            /// <summary>A shoot skill priced "−1" uses the weapon's own cost.</summary>
            private static float ShotCost(TacticalActor actor, ShootAbilityDef def, Weapon weapon)
            {
                if (def.ActionPointCost < 0f && weapon != null)
                {
                    return actor.Equipments.GetAPToUseEquipment(weapon);
                }

                return Mathf.Max(0f, def.ActionPointCost) * APMax(actor);
            }

            private static bool HasActedThisTurn(TacticalActor actor)
            {
                float ap = AP(actor);

                if (!TurnStartAP.TryGetValue(actor, out float start))
                {
                    TurnStartAP[actor] = ap;
                    return false;
                }

                return ap < start - 0.01f;
            }

            private static IEnumerable<Weapon> UsableWeapons(TacticalActor actor, GameTagDef[] tags)
            {
                return actor.Equipments.GetWeapons().Where(w => w != null && w.IsUsable && w.HasCharges
                    && (tags == null || tags.Length == 0 || tags.Any(t => t != null && w.WeaponDef.Tags.Contains(t))));
            }

            private static IEnumerable<Weapon> Guns(TacticalActor actor)
            {
                return actor.Equipments.GetWeapons().Where(w => w != null && w.IsUsable && w.HasCharges && PeekAndFallBackAI.IsGun(w));
            }

            private static bool HasAnyShot(TacticalActor actor)
            {
                return Guns(actor).Any(w => Stances.HasShotFromHere(actor, w));
            }

            private static float BestShotCost(TacticalActor actor)
            {
                List<float> costs = Guns(actor).Select(w => actor.Equipments.GetAPToUseEquipment(w)).ToList();
                return costs.Count == 0 ? 0f : costs.Min();
            }

            private static IEnumerable<TacticalActorBase> RevealedEnemies(TacticalActor actor)
            {
                return Stances.KnownEnemies(actor).Where(e => actor.TacticalFaction.Vision.IsRevealed(e));
            }

            /// <summary>The −1 AP lets the unit fire one more shot this turn than it otherwise could.</summary>
            private static bool QuickAimPays(TacticalActor actor, ApplyStatusAbilityDef quickAim)
            {
                if (!SkillUsable(actor, quickAim))
                {
                    return false;
                }

                float ap = AP(actor) - AbilityCost(actor, quickAim);
                float reduction = APMax(actor) / 4f;

                foreach (Weapon weapon in Guns(actor).Where(w => actor.IsProficientWithEquipment(w)))
                {
                    float cost = actor.Equipments.GetAPToUseEquipment(weapon);

                    if (cost - reduction <= 0.01f || !Stances.HasShotFromHere(actor, weapon))
                    {
                        continue;
                    }

                    if (Mathf.FloorToInt((ap + 0.01f) / (cost - reduction)) > Mathf.FloorToInt((AP(actor) + 0.01f) / cost))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>An armoured enemy in the line of fire from here, and the AP to shoot it after the skill.</summary>
            private static bool ArmourBreakPays(TacticalActor actor, ApplyStatusAbilityDef armourBreak)
            {
                if (!SkillUsable(actor, armourBreak))
                {
                    return false;
                }

                float ap = AP(actor) - AbilityCost(actor, armourBreak);

                foreach (Weapon weapon in Guns(actor))
                {
                    if (ap < actor.Equipments.GetAPToUseEquipment(weapon) - 0.01f)
                    {
                        continue;
                    }

                    if (Stances.ShotTargetsFromHere(actor, weapon).Any(t => Armour(t) >= ArmourBreakMinArmour))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static float Armour(TacticalActorBase target)
            {
                try
                {
                    return target is TacticalActor tacticalActor && tacticalActor.Status != null
                        ? (float)tacticalActor.Status.GetStat("Torso_Armor")
                        : 0f;
                }
                catch
                {
                    return 0f;
                }
            }

            private static float RangeOf(Weapon weapon)
            {
                float range = weapon.WeaponDef.DamagePayload != null ? weapon.WeaponDef.DamagePayload.Range : 0f;
                return range > 0f ? range : weapon.WeaponDef.EffectiveRange;
            }

            /// <summary>
            /// An explosive that only reaches the nearest known enemy with the +50% range, or a launcher that gets one more
            /// shot this turn from the −1 AP.
            /// </summary>
            private static bool BoomBlastPays(TacticalActor actor, ApplyStatusAbilityDef boomBlast)
            {
                if (!SkillUsable(actor, boomBlast))
                {
                    return false;
                }

                List<TacticalActorBase> enemies = RevealedEnemies(actor).ToList();

                if (enemies.Count == 0)
                {
                    return false;
                }

                float nearest = enemies.Min(e => Vector3.Distance(actor.Pos, e.Pos));
                float ap = AP(actor) - AbilityCost(actor, boomBlast);
                float reduction = APMax(actor) / 4f;

                foreach (Weapon weapon in UsableWeapons(actor, new[] { _explosiveTag, _grenadeLauncherTag }))
                {
                    float range = RangeOf(weapon);
                    float cost = actor.Equipments.GetAPToUseEquipment(weapon);
                    bool launcher = _grenadeLauncherTag != null && weapon.WeaponDef.Tags.Contains(_grenadeLauncherTag);
                    float costAfter = launcher ? Mathf.Max(0f, cost - reduction) : cost;

                    if (ap < costAfter - 0.01f)
                    {
                        continue;
                    }

                    if (nearest > range && nearest <= range * 1.5f)
                    {
                        return true;
                    }

                    if (launcher && nearest <= range && costAfter > 0.01f
                        && Mathf.FloorToInt((ap + 0.01f) / costAfter) > Mathf.FloorToInt((AP(actor) + 0.01f) / cost))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>At the start of the turn, with a one-handed weapon and an enemy within reach of a charge.</summary>
            private static bool AdrenalineRushPays(TacticalActor actor, ApplyStatusAbilityDef adrenalineRush)
            {
                if (!SkillUsable(actor, adrenalineRush) || AP(actor) < APMax(actor) * 0.75f)
                {
                    return false;
                }

                bool oneHanded = actor.Equipments.GetWeapons().Any(w => w != null && w.IsUsable && w.TacticalItemDef != null && w.TacticalItemDef.HandsToUse == 1);

                return oneHanded && RevealedEnemies(actor).Any(e => Vector3.Distance(actor.Pos, e.Pos) <= AdrenalineReach);
            }

            /// <summary>Exposed, out of shots from here, and already acted: dig in instead of just ending the turn.</summary>
            private static bool HunkerDownPays(TacticalActor actor, ApplyStatusAbilityDef hunkerDown)
            {
                return SkillUsable(actor, hunkerDown)
                    && PeekAndFallBackAI.IsRegularSoldier(actor)
                    && HasActedThisTurn(actor)
                    && PeekAndFallBackAI.IsSeen(actor)
                    && !HasAnyShot(actor);
            }

            /// <summary>Two or more enemies in the Rage Burst cone from the spot toward the target; 0 for a lone target.</summary>
            private static float RageBurstConeValue(TacticalActor actor, TacAITarget target, RageBurstInConeAbilityDef rageBurst)
            {
                if (target?.Actor == null)
                {
                    return 0f;
                }

                // A vehicle takes every shot of the burst on its own.
                if (_vehicleTag != null && target.Actor.HasGameTag(_vehicleTag))
                {
                    return 1f;
                }

                RageBurstInConeAbility ability = SkillOf(actor, rageBurst) as RageBurstInConeAbility;
                float halfAngle = ability != null ? Mathf.Max(10f, ability.GetConeSpreadDegrees() / 2f) : 30f;
                Weapon weapon = target.Equipment as Weapon;
                float range = weapon != null ? weapon.WeaponDef.EffectiveRange * 1.5f : 25f;

                Vector3 toTarget = target.Actor.Pos - target.Pos;
                toTarget.y = 0f;

                int inCone = RevealedEnemies(actor).Count(e =>
                {
                    Vector3 to = e.Pos - target.Pos;
                    to.y = 0f;
                    return to.magnitude <= range && Vector3.Angle(toTarget, to) <= halfAngle;
                });

                return inCone >= 3 ? 1f : inCone == 2 ? 0.7f : 0f;
            }

            /// <summary>Not silenced yet, not the mind control's, and holding WP abilities it can still pay for: more of them, more worth.</summary>
            private static float SilenceValue(TacticalActor actor, TacAITarget target)
            {
                TacticalActor enemy = target?.Actor as TacticalActor;

                if (enemy == null || !enemy.IsAlive || actor.RelationTo(enemy) != PhoenixPoint.Common.Core.FactionRelation.Enemy
                    || (_silencedStatus != null && enemy.HasStatus(_silencedStatus))
                    || TFTVAISquadPlanner.TargetMultiplier(actor, enemy) <= 0f)
                {
                    return 0f;
                }

                float willPoints = enemy.CharacterStats.WillPoints;

                int usable = enemy.GetAbilities<TacticalAbility>().Count(a => a.WillPointCost > 0f && a.WillPointCost <= willPoints
                    && a.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsEquipmentNotSelectedAndNotEnoughActionPoints));

                return usable == 0 ? 0f : Mathf.Min(1f, usable / 3f) * Mathf.Clamp01(AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, enemy));
            }

            /// <summary>No shot from here, and a known enemy within reach that it can't see past the walls.</summary>
            private static bool DemolitionPays(TacticalActor actor)
            {
                if (HasAnyShot(actor))
                {
                    return false;
                }

                return Stances.KnownEnemies(actor).Any(e => (e.Pos - actor.Pos).sqrMagnitude <= DemolitionReach * DemolitionReach
                    && !Stances.CanSee(e, actor, actor.Pos));
            }

            /// <summary>Worth marking: the squad's focus target, or an enemy many allies can see; never twice, never the mind control's.</summary>
            private static float MarkValue(TacticalActor actor, TacAITarget target)
            {
                TacticalActor enemy = target?.Actor as TacticalActor;

                if (enemy == null || !enemy.IsAlive || actor.RelationTo(enemy) != PhoenixPoint.Common.Core.FactionRelation.Enemy)
                {
                    return 0f;
                }

                if ((_markedForDeathStatus != null && enemy.HasStatus(_markedForDeathStatus)) || TFTVAISquadPlanner.TargetMultiplier(actor, enemy) <= 0f)
                {
                    return 0f;
                }

                int seenBy = actor.TacticalFaction.TacticalActors.Count(a => a != actor && a.IsAlive && Stances.CanSee(a, enemy, enemy.Pos));
                float worth = AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, enemy);
                float focus = TFTVAISquadPlanner.IsFocusTarget(enemy) ? 1f : 0.6f;

                return Mathf.Clamp01(worth * focus * (0.4f + 0.2f * Mathf.Min(3, seenBy)));
            }

            private static float PanicValue(TacticalActor actor, TacAITarget target)
            {
                TacticalActor enemy = target?.Actor as TacticalActor;

                if (enemy == null || !enemy.IsAlive || actor.RelationTo(enemy) != PhoenixPoint.Common.Core.FactionRelation.Enemy
                    || (enemy.Status != null && enemy.Status.HasStatus<PanicStatus>())
                    || (float)enemy.CharacterStats.WillPoints >= (float)actor.CharacterStats.WillPoints
                    || TFTVAISquadPlanner.TargetMultiplier(actor, enemy) <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(AIUtil.GetEnemyWeight(actor.TacticalFaction.AIBlackboard, enemy));
            }

            /// <summary>1 where no known enemy sees the spot; otherwise 0.25 plus credit for cover toward those who do.</summary>
            private static float HiddenOrCovered(TacticalActor actor, Vector3 pos)
            {
                List<TacticalActorBase> seers = Stances.KnownEnemies(actor).Where(e => Stances.CanSee(e, actor, pos)).ToList();

                return seers.Count == 0 ? 1f : 0.25f + 0.75f * seers.Average(e => Stances.CoverToward(actor, pos, e.Pos));
            }

            private static float SpotCover(TacticalActor actor, Vector3 pos)
            {
                List<TacticalActorBase> enemies = Stances.KnownEnemies(actor).OrderBy(e => (e.Pos - pos).sqrMagnitude).Take(3).ToList();
                return enemies.Count == 0 ? 1f : enemies.Average(e => Stances.CoverToward(actor, pos, e.Pos));
            }

            private static float ElectricReinforcementValue(TacticalActor actor)
            {
                TacticalAbility ability = SkillOf(actor, _electricReinforcement);

                if (ability == null)
                {
                    return 0f;
                }

                float range = _electricReinforcement.TargetingDataDef != null ? _electricReinforcement.TargetingDataDef.Origin.Range : 10f;
                List<TacticalActorBase> enemies = Stances.KnownEnemies(actor).Where(e => (e.Pos - actor.Pos).sqrMagnitude <= ThreatRange * ThreatRange * 4f).ToList();

                int threatened = actor.TacticalFaction.TacticalActors.Count(a => a.IsAlive
                    && (a.Pos - actor.Pos).sqrMagnitude <= range * range
                    && !a.HasStatus(_electricReinforcement.StatusDef)
                    && enemies.Any(e => (e.Pos - a.Pos).sqrMagnitude <= ThreatRange * ThreatRange && Stances.CanSee(e, a, a.Pos)));

                return threatened >= 2 ? Mathf.Min(1f, threatened / 4f) : 0f;
            }

            #endregion

            #region remote control

            /// <summary>The turret's shoot ability and its best target (by the AI's enemy weight), or null.</summary>
            private static TacticalAbilityTarget TurretShot(TacticalActor turret, out ShootAbility shoot)
            {
                shoot = null;

                if (turret == null || !turret.IsAlive || turret.Equipments == null)
                {
                    return null;
                }

                Weapon weapon = turret.Equipments.GetWeapons().FirstOrDefault(w => w != null && w.IsUsable && w.HasCharges);
                shoot = weapon?.DefaultShootAbility;

                if (shoot == null)
                {
                    return null;
                }

                AIBlackboard blackboard = turret.TacticalFaction.AIBlackboard;

                return shoot.GetTargets()
                    .Where(t => t.Actor != null && t.Actor.IsAlive && turret.RelationTo(t.Actor) == PhoenixPoint.Common.Core.FactionRelation.Enemy)
                    .OrderByDescending(t => AIUtil.GetEnemyWeight(blackboard, t.Actor))
                    .FirstOrDefault();
            }

            private static IEnumerator<NextUpdate> ThenTurretShoots(IEnumerator<NextUpdate> original, TacticalActor technician, TacticalActor turret)
            {
                while (original.MoveNext())
                {
                    yield return original.Current;
                }

                TacticalAbilityTarget target = null;
                ShootAbility shoot = null;

                try
                {
                    target = TurretShot(turret, out shoot);
                    TFTVLogger.Always($"[ClassSkills AI] {technician?.DisplayName} remote-controls {turret?.DisplayName}: " +
                        (target != null ? $"shoots {target.Actor?.DisplayName}" : "no target after all"));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                if (target != null && shoot != null && shoot.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                {
                    yield return shoot.ExecuteAndWait(target);
                }
            }

            [HarmonyPatch(typeof(AIActionMoveAndExecuteAbility), nameof(AIActionMoveAndExecuteAbility.Execute))]
            internal static class TFTV_AIActionMoveAndExecuteAbility_Execute_RemoteControl_patch
            {
                public static void Postfix(AIActionMoveAndExecuteAbility __instance, AIScoredTarget __0, ref IEnumerator<NextUpdate> __result)
                {
                    try
                    {
                        if (_silenceAction != null && __instance.BaseDef == _silenceAction && __0?.Actor is TacticalActor caster)
                        {
                            SilencedThisTurn.Add(caster);
                            return;
                        }

                        if (_remoteControlAction == null || __instance.BaseDef != _remoteControlAction)
                        {
                            return;
                        }

                        __result = ThenTurretShoots(__result, __0?.Actor as TacticalActor, (__0?.Target as TacAITarget)?.Actor as TacticalActor);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            #endregion

            #region special shots

            /// <summary>Our special shot actions attack with the skill, using the weapon the AI target picked.</summary>
            [HarmonyPatch(typeof(AIActionMoveAndAttack), "GetAttackAbility")]
            internal static class TFTV_AIActionMoveAndAttack_GetAttackAbility_ClassSkills_patch
            {
                public static bool Prefix(AIActionMoveAndAttack __instance, TacticalActorBase actor, TacAITarget aiTarget, ref TacticalAbility __result)
                {
                    try
                    {
                        if (!SpecialShots.TryGetValue(__instance.BaseDef, out ShootAbilityDef abilityDef) || !(actor is TacticalActor tacticalActor))
                        {
                            return true;
                        }

                        Weapon weapon = aiTarget?.Equipment as Weapon;

                        if (weapon != null && tacticalActor.Equipments.SelectedEquipment != weapon)
                        {
                            tacticalActor.Equipments.SetSelectedEquipment(weapon);
                        }

                        ShootAbility ability = tacticalActor.GetAbilities<ShootAbility>()
                            .Where(a => a.ShootAbilityDef == abilityDef)
                            .OrderByDescending(a => a.Weapon == weapon)
                            .FirstOrDefault();

                        if (ability == null)
                        {
                            return true;
                        }

                        TFTVLogger.Always($"[ClassSkills AI] {actor.DisplayName} uses {abilityDef.name.Replace("_AbilityDef", "")} with {weapon?.WeaponDef?.name} on {aiTarget?.Actor?.DisplayName}");
                        __result = ability;
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }
            }

            #endregion

            #region healing

            /// <summary>What a heal would do for this patient: HP it can restore, plus body parts it mends or re-enables.</summary>
            internal static float HealValue(HealAbility ability, TacticalActorBase patient)
            {
                if (ability == null || patient == null || patient.Health == null)
                {
                    return 0f;
                }

                float missing = Mathf.Max(0f, (float)patient.Health.Max - (float)patient.Health.Value);
                float value = Mathf.Min(Mathf.Max(0f, ability.GeneralHealAmount), missing);

                if (ability.BodyPartHealAmount > 0f && patient is TacticalActor tacticalActor && tacticalActor.BodyState != null)
                {
                    foreach (ItemSlot slot in tacticalActor.BodyState.GetHealthSlots())
                    {
                        if (slot?.GetHealth() == null || slot.GetHealth().Max <= 0f || !slot.GetAllDirectItems(true).Any())
                        {
                            continue;
                        }

                        float lost = (float)slot.GetHealth().Max - (float)slot.GetHealth().Value;

                        if (!slot.Enabled)
                        {
                            value += DisabledPartHealValue;
                        }
                        else if (lost > 0.5f)
                        {
                            value += Mathf.Min(lost, ability.BodyPartHealAmount);
                        }
                    }
                }

                return value;
            }

            /// <summary>Hurt, or with a disabled body part.</summary>
            internal static bool NeedsHealing(TacticalActorBase actor)
            {
                if (actor == null || !actor.IsAlive || actor.Health == null)
                {
                    return false;
                }

                if ((float)actor.Health.Value < (float)actor.Health.Max - 0.01f)
                {
                    return true;
                }

                return actor is TacticalActor tacticalActor && tacticalActor.BodyState != null
                    && tacticalActor.BodyState.GetHealthSlots().Any(s => s != null && !s.Enabled && s.GetHealth() != null && s.GetHealth().Max > 0f && s.GetAllDirectItems(true).Any());
            }

            internal static IEnumerable<HealAbility> UsableHeals(TacticalActor actor)
            {
                return actor.GetAbilities<HealAbility>().Where(h => h.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected));
            }

            internal static void RememberHeal(TacAITarget target, HealAbility ability)
            {
                if (target == null || ability == null)
                {
                    return;
                }

                ChosenHeal.Remove(target);
                ChosenHeal.Add(target, ability);
            }

            internal static HealAbility HealFor(TacAITarget target, TacticalActor actor)
            {
                return target != null && ChosenHeal.TryGetValue(target, out HealAbility ability) ? ability : actor.GetAbilities<HealAbility>().FirstOrDefault();
            }

            /// <summary>Replaces the heal coroutine's "first heal ability": the best one for the patient it is about to treat.</summary>
            public static HealAbility PickHeal(IEnumerable<HealAbility> abilities)
            {
                List<HealAbility> all = abilities?.ToList() ?? new List<HealAbility>();

                try
                {
                    TacticalActorBase patient = _healTarget?.Actor;

                    if (_healActor == null || patient == null)
                    {
                        return all.FirstOrDefault();
                    }

                    // The heal the scoring matched with this patient, if it can still be used.
                    if (ChosenHeal.TryGetValue(_healTarget, out HealAbility chosen) && all.Contains(chosen)
                        && chosen.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                    {
                        TFTVLogger.Always($"[ClassSkills AI] {_healActor.DisplayName} heals {patient.DisplayName} with {chosen.AbilityDef.name} (worth {HealValue(chosen, patient):0})");
                        return chosen;
                    }

                    HealAbility best = all
                        .Where(h => h.TacticalActor == _healActor && h.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsAndEquipmentNotSelected))
                        .Where(h => h.GetTargetsAt(_healTarget.Pos).Any(t => t.Actor == patient))
                        .OrderByDescending(h => HealValue(h, patient))
                        .FirstOrDefault();

                    if (best != null)
                    {
                        TFTVLogger.Always($"[ClassSkills AI] {_healActor.DisplayName} heals {patient.DisplayName} with {best.AbilityDef.name} (worth {HealValue(best, patient):0})");
                        return best;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

                return all.FirstOrDefault();
            }

            [HarmonyPatch(typeof(AIActionMoveAndHeal), nameof(AIActionMoveAndHeal.Execute))]
            internal static class TFTV_AIActionMoveAndHeal_Execute_patch
            {
                public static void Prefix(AIScoredTarget scoredTarget)
                {
                    _healActor = scoredTarget?.Actor as TacticalActor;
                    _healTarget = scoredTarget?.Target as TacAITarget;
                }
            }

            [HarmonyPatch]
            internal static class TFTV_AIActionMoveAndHeal_ExecuteCoroutine_patch
            {
                private static MethodBase TargetMethod()
                {
                    Type coroutine = typeof(AIActionMoveAndHeal).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
                        .First(t => t.Name.StartsWith("<Execute>"));

                    return coroutine.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }

                private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    MethodInfo firstOrDefault = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Length == 1)
                        .MakeGenericMethod(typeof(HealAbility));
                    MethodInfo pick = AccessTools.Method(typeof(ClassSkillsAI), nameof(PickHeal));
                    int replaced = 0;

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (instruction.Calls(firstOrDefault))
                        {
                            instruction.opcode = OpCodes.Call;
                            instruction.operand = pick;
                            replaced++;
                        }

                        yield return instruction;
                    }

                    if (replaced != 1)
                    {
                        TFTVLogger.Always($"[ClassSkills AI] WARNING: heal coroutine had {replaced} heal picks to replace (expected 1)");
                    }
                }
            }

            /// <summary>The heal action's medkit gates: any usable heal will do, and the spot score checks the AP.</summary>
            [HarmonyPatch(typeof(AIAbilityDisabledStateConsideration), nameof(AIAbilityDisabledStateConsideration.Evaluate))]
            internal static class TFTV_AIAbilityDisabledStateConsideration_Evaluate_Heal_patch
            {
                public static bool Prefix(AIAbilityDisabledStateConsideration __instance, IAIActor actor, ref float __result)
                {
                    if (__instance.BaseDef?.name != "HealAbilityEnabled_AIConsiderationDef" || !(actor is TacticalActor tacticalActor))
                    {
                        return true;
                    }

                    __result = UsableHeals(tacticalActor).Any() ? 1f : 0f;
                    return false;
                }
            }

            [HarmonyPatch(typeof(AIEnoughActionPointsForAbilityConsideration), nameof(AIEnoughActionPointsForAbilityConsideration.Evaluate))]
            internal static class TFTV_AIEnoughActionPointsForAbilityConsideration_Evaluate_Heal_patch
            {
                public static bool Prefix(AIEnoughActionPointsForAbilityConsideration __instance, IAIActor actor, ref float __result)
                {
                    if (__instance.BaseDef?.name != "EnoughAPToHeal_AIConsiderationDef" || !(actor is TacticalActor))
                    {
                        return true;
                    }

                    __result = 1f;
                    return false;
                }
            }

            #endregion
        }
    }
}
