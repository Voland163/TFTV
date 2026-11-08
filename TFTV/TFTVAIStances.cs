using Base.AI;
using Base.AI.Defs;
using Base.Defs;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.PathProcessors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal partial class TFTVAICombatAbilities
    {
        #region 5. stances

        /// <summary>
        /// Faction-wide stances for human enemies, worked out again whenever the AI looks at them.
        ///
        /// A faction the player has to wipe out, that gets no reinforcements and calls none, starts
        /// Cautious: one or two low-rank, sharp-eyed scouts carrying melee, a shotgun or a rifle go
        /// looking, and everyone else takes a position near where they spawned, preferring spots with
        /// a roof or walls around them. Once any of its soldiers has seen a player unit it turns
        /// Defensive: the scouts come back to the group and everyone holds. Gunners hold at firing
        /// posts (behind high cover with a one-tile step-out, or over low cover, with a clear line
        /// toward the nearest known enemy) and keep the AP to set Overwatch; melee holders hide.
        /// Shots and Overwatch are taken from the spot, with a peek, or from another firing post
        /// within half a move. A holder only moves when it is seen or another spot is clearly better,
        /// and with AP left and nothing to shoot it watches the nearest known enemy's approach, if a
        /// shot that way can come out from its spot. It goes Offensive only while it has at least 1.5 times the player's
        /// living units and the player has already lost someone, and drops back to Defensive for good
        /// once half its units or its leader are dead.
        ///
        /// A Defensive faction that loses the exchange of fire over a round (see OnFactionStartTurn)
        /// and has no clear shots changes tactics: Infiltration sends its stealthiest soldiers round
        /// a flank unseen, Counterattack sends its short-range and grenade carriers forward, and
        /// everyone else keeps holding. Still losing a round later, it tries the other.
        ///
        /// Every other faction is Offensive throughout, which is the stock behaviour, except that
        /// long-range gunners use a shorter, cover-seeking advance that stops at shooting distance.
        ///
        /// Only actors whose template carries the hold action are affected, so Arthrons, Tritons,
        /// vehicles and soldiers behind a deployed shield keep their own behaviour.
        /// </summary>
        internal static class Stances
        {
            internal enum Stance { Offensive, Cautious, Defensive }

            /// <summary>What a Defensive faction tries when it is losing the exchange of fire.</summary>
            internal enum Tactic { None, Counterattack, Infiltration }

            /// <summary>Losing the exchange: since the last change, this share of its HP gone (or a death), while the player lost under half as much.</summary>
            private const float LosingShare = 0.1f;
            private const float LosingRatio = 2f;

            /// <summary>This many soldiers with a shot at an enemy without high cover toward them is a clear way to deal damage: no change of tactic.</summary>
            private const int ClearShotsToStay = 2;

            /// <summary>Infiltrators need this much stealth, and the faction this many of them.</summary>
            private const float InfiltratorStealth = 0.1f;
            private const int MinInfiltrators = 2;
            private const int ThreeInfiltratorsFromSquadSize = 8;

            /// <summary>The flank point lies this far to one side of the player's centre.</summary>
            private const float FlankDistance = 12f;

            /// <summary>An infiltrator this near the flank point attacks.</summary>
            private const float FlankArrived = 4f;

            private const float InfiltrateZoneRange = 0.5f;

            /// <summary>How far toward its squad's centre the leader's anchor moves each turn.</summary>
            private const float LeaderCatchUp = 8f;

            /// <summary>Above Overwatch (18), below any shot, so holders shoot first when they can.</summary>
            private const float HoldWeight = 20f;

            private const float ScoutZoneRange = 0.75f;
            private const float CautiousAdvanceZoneRange = 0.5f;

            /// <summary>Distance at which the cautious advance stops closing in. Shared by all its users.</summary>
            private const float CautiousAdvanceMinDistance = 14f;

            private const float OffensiveStrengthRatio = 1.5f;

            /// <summary>A stance holds at least this many of the faction's turns, unless its leader dies.</summary>
            private const int MinStanceTurns = 2;

            /// <summary>Winning the exchange: since the last change, dealt at least this many times what it lost.</summary>
            private const float WinningRatio = 2f;

            /// <summary>This many soldiers with clear shots at once is a reason to press on.</summary>
            private const int ClearShotsToAttack = 3;

            /// <summary>A leader below this share of its HP is a reason to pull back.</summary>
            private const float LeaderWeak = 0.5f;

            /// <summary>With half the squad or the leader lost, going Offensive is this much less likely.</summary>
            private const float CrippledOffensiveFactor = 0.4f;

            /// <summary>Holders pick freely within this distance of their anchor, and not beyond the second.</summary>
            private const float AnchorRadius = 6f;
            private const float AnchorMaxRadius = 14f;

            /// <summary>Applied to every spot but the current one: another spot has to be a quarter better to be worth the walk.</summary>
            private const float MoveAwayFactor = 0.75f;

            /// <summary>The same, when the current spot's cover toward the enemy has fallen below PoorCover.</summary>
            private const float PoorCoverMoveAwayFactor = 0.95f;
            private const float PoorCover = 0.6f;

            /// <summary>Applied to the current spot while a known enemy can see it.</summary>
            private const float SeenHereFactor = 0.5f;

            private const float HoldZoneRange = 0.5f;

            /// <summary>Holders may move to shoot or overwatch only to a firing post this good: a step-out from high cover, or a lane over low cover.</summary>
            private const float MinPostToMoveTo = 0.85f;

            /// <summary>
            /// A firing post needs this much clear line toward the nearest known enemy (less if the enemy
            /// is nearer), from the spot itself or from its step-out.
            /// </summary>
            private const float LaneLength = 10f;

            /// <summary>How a firing post scores by the cover between it and the enemy.</summary>
            private const float StepOutPost = 1f;
            private const float LowCoverPost = 0.9f;
            private const float OpenPost = 0.5f;

            /// <summary>Score of a spot a known enemy can see, before any credit for cover toward it.</summary>
            private const float SeenSpot = 0.4f;

            /// <summary>Cover is judged toward this many of the nearest known enemies.</summary>
            private const int CoverEnemies = 3;

            /// <summary>The watch prefers the side where the known enemies are: share of them within this angle of its line.</summary>
            private const float WatchClusterAngle = 25f;

            /// <summary>Below hold, so holders take their spot first; above ending the turn.</summary>
            private const float WatchWeight = 10f;

            /// <summary>Watch for enemies up to this far beyond the cone's reach: they can walk into it.</summary>
            private const float WatchApproachMargin = 10f;

            private const int TwoScoutsFromSquadSize = 6;

            internal static AIActionMoveToPositionDef HoldActionDef;
            internal static AIActionMoveToPositionDef ScoutActionDef;
            internal static AIActionMoveToPositionDef CautiousAdvanceActionDef;
            internal static AIActionOverwatchDef WatchActionDef;
            internal static AIActionMoveToPositionDef InfiltrateActionDef;
            private static AIHasEquipmentConsiderationDef _shouldInfiltrateDef;
            private static AIRoofAboveConsiderationDef _flankPointDef;
            private static GameTagDef _grenadeTag;

            private static AIHasEquipmentConsiderationDef _shouldHoldDef;
            private static AIHasEquipmentConsiderationDef _shouldWatchDef;
            private static AIAttackPositionConsiderationDef _watchTargetDef;
            private static AIHasEquipmentConsiderationDef _shouldScoutDef;
            private static AIHasEquipmentConsiderationDef _notHoldingDef;
            private static AIHasEquipmentConsiderationDef _notLongGunnerDef;
            private static AIHasEquipmentConsiderationDef _isLongGunnerDef;

            private static AIRoofAboveConsiderationDef _wallsDef;

            /// <summary>Fall back's cover: like _wallsDef, but a spot with less cover than where the actor stands (and under low cover) is ruled out.</summary>
            private static AIRoofAboveConsiderationDef _fallBackCoverDef;

            /// <summary>A fall back must reach at least low cover, or at least the cover the actor already has if that is less.</summary>
            private const float FallBackMinCover = 0.5f;

            /// <summary>Cover where each actor stands, per frame: the fall back compares every candidate spot with it.</summary>
            private static readonly Dictionary<TacticalActor, KeyValuePair<int, float>> CoverHereByFrame = new Dictionary<TacticalActor, KeyValuePair<int, float>>();
            private static AIRoofAboveConsiderationDef _anchorDef;
            private static AIRoofAboveConsiderationDef _coveredWhenHoldingDef;
            private static AIRoofAboveConsiderationDef _firingPostDef;
            private static AIRoofAboveConsiderationDef _canStillWatchDef;
            private static AIRoofAboveConsiderationDef _ownTileDef;
            private static AIRoofAboveConsiderationDef _exposureDef;
            private static AIRoofAboveConsiderationDef _notBleedingVampireDef;

            /// <summary>A Triton below this share of its HP, with a working blood-sucking arm, would rather bite.</summary>
            private const float WoundedVampireHealth = 0.5f;

            /// <summary>Its shots count for this much then, so a strike in reach beats them.</summary>
            private const float WoundedVampireShotFactor = 0.2f;

            private static readonly HashSet<TacticalActor> WoundedVampireLogged = new HashSet<TacticalActor>();

            private static GameTagDef[] _scoutWeaponTags;
            private static GameTagDef[] _tierTags;

            private static readonly Vector3[] GridDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            private class FactionState
            {
                internal TacticalFaction Faction;
                internal bool Managed;

                /// <summary>Haven defenders: Defensive from start to end, against whoever attacks the haven.</summary>
                internal bool Forced;
                internal Temperament Temperament;
                internal int StartCount;
                internal bool ScoutsRecalled;
                internal bool Announced;
                internal Stance Current = Stance.Cautious;
                internal int TurnsInStance;
                internal int ComputedFrame = -1;
                internal readonly List<TacticalActor> Scouts = new List<TacticalActor>();
                internal readonly HashSet<TacticalActor> EverScouted = new HashSet<TacticalActor>();
                internal readonly Dictionary<TacticalActor, Vector3> Anchors = new Dictionary<TacticalActor, Vector3>();

                internal Tactic Tactic = Tactic.None;
                internal readonly List<TacticalActor> Attackers = new List<TacticalActor>();
                internal readonly List<TacticalActor> Infiltrators = new List<TacticalActor>();
                internal readonly HashSet<TacticalActor> Engaged = new HashSet<TacticalActor>();
                internal float FlankSide;
                internal Vector3 FlankPoint;

                // Both sides' HP at the start of the faction's previous turn.
                internal bool HasSnapshot;
                internal float OwnHP;
                internal int OwnAlive;
                internal float OpponentHP;
                internal int OpponentAlive;
                internal bool LeaderAlive;

                // The exchange adds up from turn to turn until the stance or tactic changes or the
                // faction is no longer behind; a clear shot excuses a change only once in that time.
                internal float WindowStartHP;
                internal float WindowStartOpponentHP;
                internal float WindowLost;
                internal float WindowDealt;
                internal bool WindowDeath;
                internal bool WindowKill;
                internal bool GraceUsed;
                internal bool AllIn;
            }

            /// <summary>
            /// How a faction leans: the chance it follows a reason to go Offensive or Defensive, the
            /// chance it switches anyway after a while (a feint), and the tactic it tries first.
            /// </summary>
            private class Temperament
            {
                internal string Name;
                internal float ToOffensive;
                internal float ToDefensive;
                internal float Feint;
                internal Tactic FirstTactic = Tactic.Infiltration;
                internal int MinScouts;
            }

            private static readonly Temperament Balanced = new Temperament { Name = "balanced", ToOffensive = 0.7f, ToDefensive = 0.7f, Feint = 0.1f };

            /// <summary>By faction def name, first match wins.</summary>
            private static readonly KeyValuePair<string, Temperament>[] Temperaments =
            {
                new KeyValuePair<string, Temperament>("NEU_Bandits", new Temperament { Name = "reckless", ToOffensive = 0.85f, ToDefensive = 0.6f, Feint = 0.15f }),
                new KeyValuePair<string, Temperament>("NJ_Purists", new Temperament { Name = "aggressive", ToOffensive = 0.85f, ToDefensive = 0.55f, Feint = 0.1f }),
                new KeyValuePair<string, Temperament>("NewJericho", new Temperament { Name = "disciplined", ToOffensive = 0.55f, ToDefensive = 0.85f, Feint = 0.05f, FirstTactic = Tactic.Counterattack }),
                new KeyValuePair<string, Temperament>("Synedrion", new Temperament { Name = "cautious", ToOffensive = 0.55f, ToDefensive = 0.85f, Feint = 0.08f, MinScouts = 2 }),
                new KeyValuePair<string, Temperament>("AN_FallenOnes", new Temperament { Name = "ambushers", ToOffensive = 0.6f, ToDefensive = 0.8f, Feint = 0.12f }),
                new KeyValuePair<string, Temperament>("Anu", new Temperament { Name = "zealous", ToOffensive = 0.85f, ToDefensive = 0.55f, Feint = 0.1f }),
                new KeyValuePair<string, Temperament>("Alien", new Temperament { Name = "predatory", ToOffensive = 0.85f, ToDefensive = 0.7f, Feint = 0.1f }),
            };

            private static Temperament TemperamentOf(TacticalFaction faction)
            {
                string name = FactionName(faction);
                return Temperaments.FirstOrDefault(t => name.Contains(t.Key)).Value ?? Balanced;
            }

            private static readonly Dictionary<TacticalFaction, FactionState> States = new Dictionary<TacticalFaction, FactionState>();
            private static readonly HashSet<string> ContactFactions = new HashSet<string>();
            private static readonly Dictionary<AIActionsTemplateDef, bool> StanceTemplates = new Dictionary<AIActionsTemplateDef, bool>();

            private static readonly HashSet<TacticalActor> HeldThisTurn = new HashSet<TacticalActor>();
            private static readonly HashSet<TacticalActor> ScoutedThisTurn = new HashSet<TacticalActor>();

            /// <summary>AP each actor had when first looked at this turn; suppression and wounds can start it below max.</summary>
            private static readonly Dictionary<TacticalActor, float> TurnStartAP = new Dictionary<TacticalActor, float>();
            private static readonly HashSet<TacticalActor> ShootFirstLogged = new HashSet<TacticalActor>();
            private static readonly HashSet<TacticalActor> InfiltratedThisTurn = new HashSet<TacticalActor>();
            private static readonly Dictionary<TacticalActor, KeyValuePair<int, bool>> ShotFromHereFrame = new Dictionary<TacticalActor, KeyValuePair<int, bool>>();

            /// <summary>Tactic records from a save, applied when their faction's state is first made.</summary>
            private static readonly Dictionary<string, string[]> PendingTactics = new Dictionary<string, string[]>();

            #region defs

            internal static void CreateDefs()
            {
                try
                {
                    AIHasEquipmentConsiderationDef hasGun = DefCache.GetDef<AIHasEquipmentConsiderationDef>("HasGun_AIConsiderationDef");
                    AIRoofAboveConsiderationDef roof = DefCache.GetDef<AIRoofAboveConsiderationDef>("RoofAbove_AIConsiderationDef");

                    _shouldHoldDef = Helper.CreateDefFromClone(hasGun, "5b0f3c1e-6d2a-4b7e-9f41-2c8a7d5e0b13", "TFTV_ShouldHold_AIConsiderationDef");
                    _shouldScoutDef = Helper.CreateDefFromClone(hasGun, "a3e9d27c-1f84-4c56-b0d9-7e62f15a8c40", "TFTV_ShouldScout_AIConsiderationDef");
                    _notHoldingDef = Helper.CreateDefFromClone(hasGun, "c81b4f06-3e9d-4a27-8b5c-d409e6f2a171", "TFTV_NotHolding_AIConsiderationDef");
                    _notLongGunnerDef = Helper.CreateDefFromClone(hasGun, "e46d0a9b-7c35-4f18-a2e6-91b3c8d75f02", "TFTV_NotLongGunner_AIConsiderationDef");
                    _isLongGunnerDef = Helper.CreateDefFromClone(hasGun, "17f5c2d8-9a46-4e0b-8d73-b5a10e4c6f29", "TFTV_IsLongGunner_AIConsiderationDef");

                    _wallsDef = Helper.CreateDefFromClone(roof, "6d2e8b41-0c57-4f93-a1b8-3f7d9e25c604", "TFTV_WallsAround_AIConsiderationDef");
                    _fallBackCoverDef = Helper.CreateDefFromClone(roof, "4f1c8e27-9b36-4a05-8d72-e3a6b0c95d18", "TFTV_FallBackCover_AIConsiderationDef");
                    _anchorDef = Helper.CreateDefFromClone(roof, "b905e3f7-4d18-4c62-9e0a-57c1a8d3b2e6", "TFTV_NearAnchor_AIConsiderationDef");
                    _coveredWhenHoldingDef = Helper.CreateDefFromClone(roof, "2a7c9f50-e3b1-4d84-b6f2-0d95e8a1c737", "TFTV_CoveredWhenHolding_AIConsiderationDef");
                    _shouldWatchDef = Helper.CreateDefFromClone(hasGun, "0e7d4b92-56a1-4c38-9f0b-c2a8e7d61f45", "TFTV_ShouldWatch_AIConsiderationDef");
                    _firingPostDef = Helper.CreateDefFromClone(roof, "9c4e1a7f-3b62-4d05-a8e9-61f0d2b7c4a3", "TFTV_FiringPost_AIConsiderationDef");
                    _canStillWatchDef = Helper.CreateDefFromClone(roof, "d5a8f2c6-7e13-4b90-9c41-0e6b3a8d2f57", "TFTV_CanStillWatch_AIConsiderationDef");
                    _ownTileDef = Helper.CreateDefFromClone(roof, "47b2e9d0-c815-4a6f-b3d7-8f5a1c0e9b62", "TFTV_OwnTile_AIConsiderationDef");
                    _exposureDef = Helper.CreateDefFromClone(roof, "b3f1c7e2-58a9-4d06-9e4b-2c7d0a91f8e5", "TFTV_Exposure_AIConsiderationDef");
                    _notBleedingVampireDef = Helper.CreateDefFromClone(roof, "6f2d9a4c-1e87-4b35-a0c9-d47e83b51f26", "TFTV_NotBleedingVampire_AIConsiderationDef");
                    _shouldInfiltrateDef = Helper.CreateDefFromClone(hasGun, "c7f4e0b2-1a95-4d68-8e3c-29b5d16a0f73", "TFTV_ShouldInfiltrate_AIConsiderationDef");
                    _flankPointDef = Helper.CreateDefFromClone(roof, "84b1d6e9-f20c-4a37-95d8-0e6c3a7f1b25", "TFTV_TowardFlank_AIConsiderationDef");
                    _grenadeTag = DefCache.GetDef<GameTagDef>("GrenadeItem_TagDef");

                    _scoutWeaponTags = new[]
                    {
                        DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef"),
                        DefCache.GetDef<GameTagDef>("ShotgunItem_TagDef"),
                        DefCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef"),
                    };

                    CreateHold(roof);
                    CreateScout();
                    CreateCautiousAdvance();
                    CreateWatch();
                    CreateInfiltrate();

                    AIActionsTemplateDef template = DefCache.GetDef<AIActionsTemplateDef>(SoldierAITemplate);

                    // Holders, scouts and infiltrators get no plain move; scouts and infiltrators have their own.
                    foreach (AIActionMoveToPositionDef move in template.ActionDefs.OfType<AIActionMoveToPositionDef>())
                    {
                        if (move != HoldActionDef && move != ScoutActionDef && move != InfiltrateActionDef && move != PeekAndFallBackAI.FallBackActionDef)
                        {
                            PeekAndFallBackAI.AddEarlyExit(move, _notHoldingDef);
                        }
                    }

                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveAndAttackDef>("DashAndShoot_AIActionDef"), _notHoldingDef);

                    // Long-range gunners advance through CautiousAdvance instead.
                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef"), _notLongGunnerDef);
                    PeekAndFallBackAI.AddEarlyExit(DefCache.GetDef<AIActionMoveToPositionDef>("Dash_AIActionDef"), _notLongGunnerDef);

                    // Holders shoot or overwatch from where they stand, or from a walled-in spot within half a move.
                    AddCoveredWhenHolding(DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef"));
                    AddCoveredWhenHolding(DefCache.GetDef<AIActionOverwatchDef>("Overwatch_AIActionDef"));

                    // A holder walked into the open to patch up an ally and was shot to pieces there.
                    AddCoveredWhenHolding(DefCache.GetDef<AIActionMoveAndHealDef>("MoveAndHeal_AIActionDef"));

                    // Tritons and Sirens fight like soldiers, keeping their own mist and psychic abilities.
                    // Their plain moves (Advance_Normal, MoveToRandomWaypoint) are the soldiers' own defs,
                    // so the holding gates above already apply to them.
                    foreach (string templateName in new[] { "Fishman_AIActionsTemplateDef", "Siren_AIActionsTemplateDef" })
                    {
                        GiveSoldierActions(DefCache.GetDef<AIActionsTemplateDef>(templateName));
                    }

                    // Their shot is TFTVBetterEnemies' heavier clone of MoveAndShoot.
                    AddCoveredWhenHolding(DefCache.GetDef<AIActionMoveAndAttackDef>("BC_MoveAndShoot_AIActionDef"));

                    // A badly hurt Triton with a blood-sucking arm bites to heal rather than shoots. The shot
                    // weighs 600 against the strike's 350 and nothing values the heal, so the shots are scaled down.
                    foreach (AIActionDef shot in new AIActionDef[] { DefCache.GetDef<AIActionMoveAndAttackDef>("BC_MoveAndShoot_AIActionDef"), PeekAndFallBackAI.PeekActionDef })
                    {
                        AITargetEvaluation positions = shot.Evaluations.First(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);
                        positions.Considerations = positions.Considerations.AddToArray(new AIAdjustedConsideration
                        {
                            Consideration = _notBleedingVampireDef,
                            ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                        });
                    }

                    // Falling back means getting behind something, not just out of sight: a spot worse covered than
                    // where the actor stands scores 0, so with nowhere better in reach it stays put.
                    AITargetEvaluation fallBack = PeekAndFallBackAI.FallBackActionDef.Evaluations[0];
                    fallBack.Considerations = fallBack.Considerations.AddToArray(new AIAdjustedConsideration
                    {
                        Consideration = _fallBackCoverDef,
                        ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                    });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void GiveSoldierActions(AIActionsTemplateDef template)
            {
                AIActionDef[] soldierActions =
                {
                    PeekAndFallBackAI.PeekActionDef, PeekAndFallBackAI.FallBackActionDef,
                    HoldActionDef, ScoutActionDef, CautiousAdvanceActionDef, WatchActionDef, InfiltrateActionDef
                };

                template.ActionDefs = template.ActionDefs.Concat(soldierActions.Where(a => a != null && !template.ActionDefs.Contains(a))).ToArray();
                TFTVLogger.Always($"TFTV AI: gave {template.name} the soldiers' stance, peek and fall back actions");
            }

            private static void AddCoveredWhenHolding(AIActionDef actionDef)
            {
                AITargetEvaluation positions = actionDef.Evaluations.First(e =>
                    e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef || e.TargetGeneratorDef is AIActorRangeZoneTargetGeneratorDef);

                positions.Considerations = positions.Considerations.AddToArray(new AIAdjustedConsideration
                {
                    Consideration = _coveredWhenHoldingDef,
                    ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                });
            }

            /// <summary>
            /// Overwatch on the spot toward the nearest enemy the faction knows of, seen or not, the way
            /// a player sets one up behind cover to cover an approach. Stock Overwatch only targets an
            /// enemy it could shoot right now.
            /// </summary>
            private static void CreateWatch()
            {
                AIActionOverwatchDef overwatch = DefCache.GetDef<AIActionOverwatchDef>("Overwatch_AIActionDef");

                WatchActionDef = Helper.CreateDefFromClone(overwatch, "7a1e9c35-d4b8-4f62-8e07-b35f2a9d0c18", "TFTV_WatchFromCover_AIActionDef");
                WatchActionDef.Weight = WatchWeight;

                RemoveConsiderations(WatchActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(WatchActionDef, _shouldWatchDef);

                AITargetEvaluation positions = WatchActionDef.Evaluations.First(e => e.TargetGeneratorDef is AIActorMovementZoneTargetGeneratorDef);

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "c5f02d8e-91a7-4b3c-a6d4-e8b07f2c3915",
                    "TFTV_WatchZone_AITargetGeneratorDef");

                // A zero range still yields the neighbouring tiles, so the own tile is enforced by a score.
                zone.ZoneRange = 0f;
                zone.IncludeSelfActorPosition = true;
                positions.TargetGeneratorDef = zone;
                positions.Considerations = new AIAdjustedConsideration[]
                {
                    new AIAdjustedConsideration { Consideration = _ownTileDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) }
                };

                AITargetEvaluation enemies = WatchActionDef.Evaluations.First(e => e.TargetGeneratorDef is AIEnemyTargetGeneratorDef);

                AIEnemyTargetGeneratorDef known = Helper.CreateDefFromClone(
                    (AIEnemyTargetGeneratorDef)enemies.TargetGeneratorDef,
                    "e83b6f14-2c09-4d75-b1a8-6f9d0e47c2b3",
                    "TFTV_WatchEnemies_AITargetGeneratorDef");

                // Every enemy; the score below keeps the ones the faction knows of.
                known.CheckVision = false;
                enemies.TargetGeneratorDef = known;

                _watchTargetDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIAttackPositionConsiderationDef>("OverwatchAttackPosition_AIConsiderationDef"),
                    "3d6c8a07-f1e5-4b92-a074-9c2e5b8d1f60",
                    "TFTV_WatchTarget_AIConsiderationDef");

                enemies.Considerations = new AIAdjustedConsideration[]
                {
                    new AIAdjustedConsideration { Consideration = _watchTargetDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) }
                };

                AddActionToTemplate(SoldierAITemplate, WatchActionDef);
            }

            private static void CreateHold(AIRoofAboveConsiderationDef roof)
            {
                HoldActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("MoveToSafePosition_AIActionDef"),
                    "f0c64a2b-8e17-4d39-95b6-a7d23c18e5f4",
                    "TFTV_HoldPosition_AIActionDef");

                HoldActionDef.Weight = HoldWeight;

                // Holders act whether or not an enemy is in view.
                RemoveConsiderations(HoldActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(HoldActionDef, _shouldHoldDef);

                AITargetEvaluation positions = HoldActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "f6a3d1c8-2b74-4e09-8d5f-a1c7e93b0d26",
                    "TFTV_HoldZone_AITargetGeneratorDef");

                zone.ZoneRange = HoldZoneRange;
                zone.IncludeSelfActorPosition = true;
                positions.TargetGeneratorDef = zone;

                positions.Considerations = new AIAdjustedConsideration[]
                {
                    new AIAdjustedConsideration { Consideration = roof, ScoreCurve = AnimationCurve.Linear(0f, 0.7f, 1f, 1f) },
                    new AIAdjustedConsideration { Consideration = _wallsDef, ScoreCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f) },
                    new AIAdjustedConsideration { Consideration = _anchorDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                    // Gunners only; 1 for everyone else, who keep hiding.
                    new AIAdjustedConsideration { Consideration = _firingPostDef, ScoreCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 1f) },
                    new AIAdjustedConsideration { Consideration = _canStillWatchDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                    // In place of stock NoLineofSight: a gunner seen from behind cover is where it should be.
                    new AIAdjustedConsideration { Consideration = _exposureDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                    positions.Considerations.First(c => c.Consideration is AISpreadConsiderationDef)
                };

                AddActionToTemplate(SoldierAITemplate, HoldActionDef);
            }

            private static void CreateScout()
            {
                ScoutActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef"),
                    "8e3b17d4-c65a-4f20-b9d1-6a04f2e7c58b",
                    "TFTV_Scout_AIActionDef");

                // Scouts go looking whether or not an enemy is in view.
                RemoveConsiderations(ScoutActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(ScoutActionDef, _shouldScoutDef);
                PeekAndFallBackAI.AddEarlyExit(ScoutActionDef, PeekAndFallBackAI.HasNotFallenBackConsiderationDef);

                AITargetEvaluation positions = ScoutActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "d27a5e91-0b43-4c8f-a6e5-3c19b7f04d82",
                    "TFTV_ScoutZone_AITargetGeneratorDef");

                zone.ZoneRange = ScoutZoneRange;
                zone.IncludeSelfActorPosition = false;
                positions.TargetGeneratorDef = zone;

                SetLineOfSightCurve(positions, AnimationCurve.Linear(0f, 0.4f, 1f, 1f));

                AddActionToTemplate(SoldierAITemplate, ScoutActionDef);
            }

            /// <summary>
            /// Infiltrators work their way to the flank point half a move at a time, keeping AP to
            /// shoot, by spots the player can't see and never through an Overwatch cone.
            /// </summary>
            private static void CreateInfiltrate()
            {
                InfiltrateActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef"),
                    "5e8c2a41-9d07-4b3f-a6e2-7c1f0d84b95a",
                    "TFTV_Infiltrate_AIActionDef");

                RemoveConsiderations(InfiltrateActionDef, typeof(AIVisibleEnemiesConsiderationDef));
                PeekAndFallBackAI.AddEarlyExit(InfiltrateActionDef, _shouldInfiltrateDef);

                AITargetEvaluation positions = InfiltrateActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "a2d71f96-3c48-4e05-b9a1-6f3e2c0d87b4",
                    "TFTV_InfiltrateZone_AITargetGeneratorDef");

                zone.ZoneRange = InfiltrateZoneRange;
                zone.IncludeSelfActorPosition = false;
                positions.TargetGeneratorDef = zone;

                List<AIAdjustedConsideration> considerations = new List<AIAdjustedConsideration>
                {
                    new AIAdjustedConsideration { Consideration = _flankPointDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                    new AIAdjustedConsideration { Consideration = _exposureDef, ScoreCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f) },
                };

                AIAdjustedConsideration spread = positions.Considerations.FirstOrDefault(c => c.Consideration is AISpreadConsiderationDef);

                if (spread != null)
                {
                    considerations.Add(spread);
                }

                positions.Considerations = considerations.ToArray();

                AddActionToTemplate(SoldierAITemplate, InfiltrateActionDef);
            }

            private static void CreateCautiousAdvance()
            {
                CautiousAdvanceActionDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef"),
                    "4c9f0e62-a7d3-4b15-8e2c-f1b6d09a3e75",
                    "TFTV_CautiousAdvance_AIActionDef");

                PeekAndFallBackAI.AddEarlyExit(CautiousAdvanceActionDef, _isLongGunnerDef);
                PeekAndFallBackAI.AddEarlyExit(CautiousAdvanceActionDef, PeekAndFallBackAI.HasNotFallenBackConsiderationDef);

                AITargetEvaluation positions = CautiousAdvanceActionDef.Evaluations[0];

                AIActorMovementZoneTargetGeneratorDef zone = Helper.CreateDefFromClone(
                    (AIActorMovementZoneTargetGeneratorDef)positions.TargetGeneratorDef,
                    "93b6d1a8-5f20-4e7c-b4d9-0a8e2c65f13d",
                    "TFTV_CautiousAdvanceZone_AITargetGeneratorDef");

                zone.ZoneRange = CautiousAdvanceZoneRange;
                positions.TargetGeneratorDef = zone;

                AIClosestEnemyConsiderationDef closest = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIClosestEnemyConsiderationDef>("ClosestLineToEnemy_AIConsiderationDef"),
                    "e1a84c37-2d6b-4f95-8c0e-b7f3d5a29e61",
                    "TFTV_ShootingDistanceToEnemy_AIConsiderationDef");

                closest.MinDistance = CautiousAdvanceMinDistance;

                for (int i = 0; i < positions.Considerations.Length; i++)
                {
                    if (positions.Considerations[i].Consideration is AIClosestEnemyConsiderationDef)
                    {
                        positions.Considerations[i] = new AIAdjustedConsideration { Consideration = closest, ScoreCurve = positions.Considerations[i].ScoreCurve };
                    }
                }

                SetLineOfSightCurve(positions, AnimationCurve.Linear(0f, 0.3f, 1f, 1f));

                AddActionToTemplate(SoldierAITemplate, CautiousAdvanceActionDef);
            }

            /// <summary>The clones may share AIAdjustedConsideration objects with Advance_Normal, so swap rather than edit.</summary>
            private static void SetLineOfSightCurve(AITargetEvaluation evaluation, AnimationCurve curve)
            {
                for (int i = 0; i < evaluation.Considerations.Length; i++)
                {
                    if (evaluation.Considerations[i].Consideration is AILineOfSightToEnemiesConsiderationDef lineOfSight)
                    {
                        evaluation.Considerations[i] = new AIAdjustedConsideration { Consideration = lineOfSight, ScoreCurve = curve };
                    }
                }
            }

            #endregion

            #region stance

            internal static Stance GetStance(TacticalFaction faction)
            {
                FactionState state = GetState(faction);

                if (state == null || !state.Managed)
                {
                    return Stance.Offensive;
                }

                if (state.ComputedFrame != Time.frameCount)
                {
                    state.ComputedFrame = Time.frameCount;
                    Update(state);
                }

                return state.Current;
            }

            private static FactionState GetState(TacticalFaction faction)
            {
                if (faction == null)
                {
                    return null;
                }

                if (!States.TryGetValue(faction, out FactionState state))
                {
                    state = new FactionState { Faction = faction };
                    States.Add(faction, state);
                    Classify(state);
                    RestoreTactic(state);
                }

                return state;
            }

            private static void Classify(FactionState state)
            {
                TacticalFaction faction = state.Faction;
                TacticalLevelController level = faction.TacticalLevel;
                string name = FactionName(faction);

                state.StartCount = faction.TacticalActors.Count();
                state.Temperament = TemperamentOf(faction);

                if (faction.IsControlledByPlayer || !faction.TacticalActors.Any(UsesStanceTemplate))
                {
                    return;
                }

                // The human AI's actions skip anything wider than a soldier; say so for the types it was given.
                foreach (TacticalActor big in faction.TacticalActors.Where(a => UsesStanceTemplate(a) && !PeekAndFallBackAI.IsRegularSoldier(a)))
                {
                    TFTVLogger.Always($"TFTV AI stance: {big.DisplayName} of {name} is too big for the human AI (agent radius {big.NavigationComponent.AgentNavSettings.AgentRadius:0.00}) and keeps its own behaviour");
                }

                TacMissionTypeDef missionType = level.TacMission?.MissionData?.MissionType;
                bool havenDefense = IsMissionOfType(missionType, "MissionTypeHavenDefense_MissionTagDef");
                bool baseDefense = IsMissionOfType(missionType, "MissionTypePhoenixBaseDefence_MissionTagDef");

                if (havenDefense && faction.ParticipantKind == TacMissionParticipant.Residents)
                {
                    state.Managed = true;
                    state.Forced = true;
                    state.Current = Stance.Defensive;
                    TFTVLogger.Always($"TFTV AI stance: {name} ({state.StartCount} units) defends the haven: Defensive throughout");
                    return;
                }

                if (havenDefense || baseDefense)
                {
                    TFTVLogger.Always($"TFTV AI stance: {name} ({state.StartCount} units) is attacking a {(havenDefense ? "haven" : "Phoenix base")}: stays Offensive");
                    return;
                }

                TacticalFaction player = PlayerFaction(level);

                if (player == null)
                {
                    TFTVLogger.Always($"TFTV AI stance: {name} stays Offensive, no player faction found");
                    return;
                }

                List<FactionObjective> objectives = player.Objectives.ToList();

                // A wipe-out can come later in the chain, e.g. activate a console, then kill everyone.
                bool mustBeWipedOut = objectives.Any(o => LeadsToWipeOut(o, faction, player));

                List<string> otherVictoryObjectives = objectives
                    .Where(o => o.IsVictoryObjective && !LeadsToWipeOut(o, faction, player))
                    .Select(o => o.GetType().Name)
                    .ToList();

                TacMissionFactionData missionData = level.TacMission?.GetMissionFactionData(faction.TacticalFactionDef);
                TacMissionTypeParticipantData participantData = missionType?.GetParticipantData(faction.ParticipantKind);

                float reinforcementPoints = missionData?.ReinforcementDeploymentPoints ?? 0f;
                bool infiniteReinforcements = participantData != null && participantData.InfiniteReinforcements;

                // Reinforcements no longer rule stances out: they arrive into whatever stance the faction is in.
                state.Managed = mustBeWipedOut && otherVictoryObjectives.Count == 0;

                TFTVLogger.Always($"TFTV AI stance: {name} ({state.StartCount} units, {state.Temperament.Name}) {(state.Managed ? "uses stances" : "stays Offensive")}. " +
                    $"Player objectives: {string.Join(", ", objectives.Select(o => o.GetType().Name + (o.IsVictoryObjective ? "*" : "")))}; " +
                    $"must be wiped out {mustBeWipedOut}, other victory objectives {otherVictoryObjectives.Count}, " +
                    $"reinforcement points {reinforcementPoints}, infinite {infiniteReinforcements}");
            }

            private static bool IsMissionOfType(TacMissionTypeDef missionType, string tagName)
            {
                MissionTagDef tag = DefCache.GetDef<MissionTagDef>(tagName);

                return missionType != null && tag != null
                    && (missionType.MissionTypeTag == tag || (missionType.MissionTags != null && missionType.MissionTags.Contains(tag)));
            }

            /// <summary>Who the faction fights: the player, or for haven defenders whoever attacks the haven (and the player, under Void Omen 5).</summary>
            private static List<TacticalFaction> Opponents(FactionState state)
            {
                TacticalFaction faction = state.Faction;

                if (!state.Forced)
                {
                    TacticalFaction player = PlayerFaction(faction.TacticalLevel);
                    return player != null ? new List<TacticalFaction> { player } : new List<TacticalFaction>();
                }

                return faction.TacticalLevel.Factions
                    .Where(f => f != faction && !f.Faction.FactionDef.MatchesShortName("env") && faction.GetRelationTo(f) == FactionRelation.Enemy)
                    .ToList();
            }

            /// <summary>
            /// Same choice as WipeEnemyFactionObjective.EvaluateObjective: the override factions if
            /// any are set, otherwise every faction the objective's owner is at war with.
            /// (IsInterestedInActor is no help, it always returns false.)
            /// </summary>
            private static bool Targets(WipeEnemyFactionObjective objective, TacticalFaction faction, TacticalFaction owner)
            {
                if (OverrideFactions(objective) is List<TacticalFactionDef> overrides && overrides.Count > 0)
                {
                    return overrides.Contains(faction.TacticalFactionDef);
                }

                // Objectives further down the chain have no faction until they are added.
                TacticalFaction objectiveFaction = objective.Faction ?? owner;
                return objectiveFaction != null && objectiveFaction.GetRelationTo(faction) == FactionRelation.Enemy;
            }

            /// <summary>The objective is a wipe-out of the faction, or one follows it on success.</summary>
            private static bool LeadsToWipeOut(FactionObjective objective, TacticalFaction faction, TacticalFaction owner, int depth = 0)
            {
                if (objective == null || depth > 8)
                {
                    return false;
                }

                if (objective is WipeEnemyFactionObjective wipe && Targets(wipe, faction, owner))
                {
                    return true;
                }

                return objective.NextOnSuccess != null && objective.NextOnSuccess.Any(o => LeadsToWipeOut(o, faction, owner, depth + 1));
            }

            private static readonly AccessTools.FieldRef<WipeEnemyFactionObjective, List<TacticalFactionDef>> OverrideFactions =
                AccessTools.FieldRefAccess<WipeEnemyFactionObjective, List<TacticalFactionDef>>("_overrideFactions");

            private static void Update(FactionState state)
            {
                TacticalFaction faction = state.Faction;
                string name = FactionName(faction);

                if (!state.Forced && !ContactFactions.Contains(name))
                {
                    TacticalActor seen = Opponents(state)
                        .SelectMany(f => f.TacticalActors)
                        .FirstOrDefault(a => a.IsAlive && faction.Vision.IsRevealed(a));

                    if (seen != null)
                    {
                        ContactFactions.Add(name);
                        TFTVLogger.Always($"TFTV AI stance: {name} has seen {seen.DisplayName}");
                    }
                }

                bool contact = state.Forced || ContactFactions.Contains(name);
                state.StartCount = Math.Max(state.StartCount, faction.TacticalActors.Count());

                // After first contact the stance only changes at the start of the faction's turn (OnFactionStartTurn).
                Stance next;
                string reason;

                if (state.Forced)
                {
                    next = Stance.Defensive;
                    reason = "defends the haven";
                }
                else if (!contact)
                {
                    next = Stance.Cautious;
                    reason = "no contact yet";
                }
                else if (state.Current == Stance.Cautious)
                {
                    next = Stance.Defensive;
                    reason = "first contact";
                    state.TurnsInStance = 0;
                }
                else
                {
                    next = state.Current;
                    reason = "as decided at the start of its turn";
                }

                if (next != state.Current || !state.Announced)
                {
                    TFTVLogger.Always($"TFTV AI stance: {name} turn {faction.TurnNumber}: {state.Current} -> {next} ({reason})");
                }

                state.Announced = true;
                state.Current = next;

                if (state.Tactic != Tactic.None && next != Stance.Defensive)
                {
                    EndTactic(state, $"now {next}");
                }
                else if (state.Tactic == Tactic.Infiltration && faction.IsPlayingTurn)
                {
                    CheckEngaged(state);
                }

                if (next == Stance.Cautious)
                {
                    RefreshScouts(state);
                }
                else if (!state.ScoutsRecalled && state.EverScouted.Count > 0)
                {
                    RecallScouts(state);
                }
            }

            private static void RefreshScouts(FactionState state)
            {
                state.Scouts.RemoveAll(a => a == null || !a.IsAlive);

                List<TacticalActor> alive = state.Faction.TacticalActors.Where(a => a.IsAlive).ToList();
                int wanted = Math.Max(alive.Count >= TwoScoutsFromSquadSize ? 2 : 1, state.Temperament?.MinScouts ?? 0);

                if (state.Scouts.Count >= wanted)
                {
                    return;
                }

                foreach (TacticalActor scout in alive
                    .Where(a => !state.Scouts.Contains(a) && CanScout(a))
                    .OrderBy(RankOrder)
                    .ThenByDescending(a => a.GetAdjustedPerceptionValue())
                    .Take(wanted - state.Scouts.Count)
                    .ToList())
                {
                    state.Scouts.Add(scout);
                    state.EverScouted.Add(scout);
                    TFTVLogger.Always($"TFTV AI stance: {scout.DisplayName} scouts for {FactionName(state.Faction)} (rank order {RankOrder(scout)}, perception {scout.GetAdjustedPerceptionValue()})");
                }
            }

            /// <summary>Scouts hold near the middle of everyone else from now on.</summary>
            private static void RecallScouts(FactionState state)
            {
                state.ScoutsRecalled = true;
                state.Scouts.Clear();

                List<TacticalActor> rest = state.Faction.TacticalActors.Where(a => a.IsAlive && !state.EverScouted.Contains(a)).ToList();

                if (rest.Count == 0)
                {
                    return;
                }

                Vector3 centre = rest.Aggregate(Vector3.zero, (sum, a) => sum + a.Pos) / rest.Count;

                foreach (TacticalActor scout in state.EverScouted.Where(a => a != null && a.IsAlive))
                {
                    state.Anchors[scout] = centre;
                    TFTVLogger.Always($"TFTV AI stance: {scout.DisplayName} falls back to the group at {centre}");
                }
            }

            private static bool CanScout(TacticalActor actor)
            {
                return UsesStanceTemplate(actor)
                    && PeekAndFallBackAI.IsRegularSoldier(actor)
                    && !IsLeader(actor)
                    && actor.Equipments.GetWeapons().Any(w => _scoutWeaponTags.Any(t => w.WeaponDef.Tags.Contains(t)));
            }

            /// <summary>Juves first, then tier 3, then soldiers without a gang rank, then tier 2. Leaders never scout.</summary>
            private static int RankOrder(TacticalActor actor)
            {
                GameTagDef[] tiers = TierTags();

                if (tiers[3] != null && actor.GameTags.Contains(tiers[3])) return 0;
                if (tiers[2] != null && actor.GameTags.Contains(tiers[2])) return 1;
                if (tiers[1] != null && actor.GameTags.Contains(tiers[1])) return 3;
                return 2;
            }

            private static bool IsLeader(TacticalActor actor)
            {
                GameTagDef leader = TierTags()[0];
                return leader != null && actor.GameTags.Contains(leader);
            }

            /// <summary>The rank tags are made by TFTVHumanEnemies, possibly after our defs, so look them up on first use.</summary>
            private static GameTagDef[] TierTags()
            {
                if (_tierTags == null)
                {
                    _tierTags = Enumerable.Range(1, 4)
                        .Select(i => (GameTagDef)DefCache.GetDef<GameTagDef>($"HumanEnemyTier_{i}_GameTagDef"))
                        .ToArray();
                }

                return _tierTags;
            }

            private static TacticalFaction PlayerFaction(TacticalLevelController level)
            {
                return level?.Factions.FirstOrDefault(f => f.IsControlledByPlayer);
            }

            private static string FactionName(TacticalFaction faction)
            {
                return faction.TacticalFactionDef.name;
            }

            private static bool UsesStanceTemplate(TacticalActorBase actor)
            {
                AIActionsTemplateDef template = actor?.ActionsTemplateDef;

                if (template == null || HoldActionDef == null)
                {
                    return false;
                }

                if (!StanceTemplates.TryGetValue(template, out bool uses))
                {
                    uses = template.ActionDefs.Contains(HoldActionDef);
                    StanceTemplates.Add(template, uses);
                }

                return uses;
            }

            #endregion

            #region tactics

            /// <summary>
            /// Called at the start of every faction's turn. A Defensive faction weighs the HP both
            /// sides lost since its previous turn began. If it is losing the exchange and has no clear
            /// way to deal damage this turn, it counterattacks or infiltrates; if it is still losing a
            /// turn later, it tries the other.
            /// </summary>
            internal static void OnFactionStartTurn(TacticalFaction faction)
            {
                try
                {
                    if (faction == null || faction.IsControlledByPlayer || HoldActionDef == null)
                    {
                        return;
                    }

                    FactionState state = GetState(faction);

                    if (state == null || !state.Managed)
                    {
                        return;
                    }

                    TacticalFaction player = PlayerFaction(faction.TacticalLevel);
                    List<TacticalFaction> opponents = Opponents(state);
                    string name = FactionName(faction);
                    Stance stance = GetStance(faction);

                    float ownHP = TotalHP(faction);
                    int ownAlive = faction.TacticalActors.Count(a => a.IsAlive);
                    float opponentHP = opponents.Sum(TotalHP);
                    int opponentAlive = opponents.Sum(f => f.TacticalActors.Count(a => a.IsAlive));
                    TacticalActor leader = faction.TacticalActors.FirstOrDefault(IsLeader);
                    bool leaderAlive = leader != null && leader.IsAlive;

                    if (stance != Stance.Cautious && state.HasSnapshot)
                    {
                        float lost = Mathf.Max(0f, state.OwnHP - ownHP);
                        float dealt = Mathf.Max(0f, state.OpponentHP - opponentHP);
                        bool death = ownAlive < state.OwnAlive;
                        bool kill = opponentAlive < state.OpponentAlive;

                        if (state.WindowStartHP <= 0f)
                        {
                            state.WindowStartHP = state.OwnHP;
                            state.WindowStartOpponentHP = state.OpponentHP;
                        }

                        state.WindowLost += lost;
                        state.WindowDealt += dealt;
                        state.WindowDeath |= death;
                        state.WindowKill |= kill;

                        bool losing = (state.WindowLost >= LosingShare * state.WindowStartHP || state.WindowDeath)
                            && state.WindowDealt < state.WindowLost / LosingRatio;

                        bool winning = state.WindowDealt > 0f && state.WindowDealt >= WinningRatio * state.WindowLost
                            && (state.WindowKill || state.WindowDealt >= LosingShare * state.WindowStartOpponentHP);

                        TFTVLogger.Always($"TFTV AI tactics: {name} turn {faction.TurnNumber} ({stance}, {state.Temperament.Name}): since its last turn lost {lost:0} HP" +
                            (death ? $" ({state.OwnAlive - ownAlive} dead)" : "") +
                            $", dealt {dealt:0}" + (kill ? $" ({state.OpponentAlive - opponentAlive} killed)" : "") +
                            $"; since the last change lost {state.WindowLost:0} of {state.WindowStartHP:0}, dealt {state.WindowDealt:0}: " +
                            $"{(losing ? "losing the exchange" : winning ? "winning the exchange" : "even")}; tactic {state.Tactic}");

                        bool switched = !state.Forced && DecideStance(state, losing, winning, ownAlive, opponentAlive, opponents, leader, state.LeaderAlive && !leaderAlive);

                        if (switched)
                        {
                            ResetWindow(state, ownHP, opponentHP);
                        }
                        else if (!state.Forced && state.Current == Stance.Defensive && losing && player != null)
                        {
                            int clearShots = CountClearShots(faction, out string who);

                            if (clearShots >= ClearShotsToStay && !state.GraceUsed)
                            {
                                state.GraceUsed = true;
                                TFTVLogger.Always($"TFTV AI tactics: {name} has {clearShots} clear shots this turn ({who}), so it gives {state.Tactic} one more turn");
                            }
                            else
                            {
                                if (clearShots >= ClearShotsToStay)
                                {
                                    TFTVLogger.Always($"TFTV AI tactics: {name} has {clearShots} clear shots ({who}), but it already waited once");
                                }

                                ChangeTactic(state, player);
                                ResetWindow(state, ownHP, opponentHP);
                            }
                        }
                        else if (state.WindowDealt >= state.WindowLost)
                        {
                            // Holding its own: start counting afresh.
                            ResetWindow(state, ownHP, opponentHP);
                        }
                    }

                    if (state.Current == Stance.Defensive)
                    {
                        KeepLeaderWithSquad(state);
                    }

                    if (state.Tactic == Tactic.Infiltration && player != null)
                    {
                        UpdateFlankPoint(state, player);
                    }

                    state.HasSnapshot = true;
                    state.OwnHP = ownHP;
                    state.OwnAlive = ownAlive;
                    state.OpponentHP = opponentHP;
                    state.OpponentAlive = opponentAlive;
                    state.LeaderAlive = leaderAlive;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ResetWindow(FactionState state, float ownHP, float opponentHP)
            {
                state.WindowStartHP = ownHP;
                state.WindowStartOpponentHP = opponentHP;
                state.WindowLost = 0f;
                state.WindowDealt = 0f;
                state.WindowDeath = false;
                state.WindowKill = false;
                state.GraceUsed = false;
            }

            /// <summary>
            /// Offensive or Defensive for the coming turn. Losing the exchange, a hurt or dead leader,
            /// or half the squad gone are reasons to pull back; winning the exchange, the numbers, or
            /// several clear shots are reasons to press on. A reason is followed with the temperament's
            /// chance, and with no reason the faction may switch anyway (a feint). A stance holds for
            /// MinStanceTurns unless the leader has just died. Returns whether the stance changed.
            /// </summary>
            private static bool DecideStance(FactionState state, bool losing, bool winning, int ownAlive, int opponentAlive,
                List<TacticalFaction> opponents, TacticalActor leader, bool leaderJustDied)
            {
                TacticalFaction faction = state.Faction;
                Temperament temperament = state.Temperament;
                Stance current = state.Current;

                if (current == Stance.Cautious)
                {
                    return false;
                }

                state.TurnsInStance++;

                bool leaderDead = leader != null && !leader.IsAlive;
                bool leaderWeak = leader != null && leader.IsAlive
                    && (float)leader.CharacterStats.Health < LeaderWeak * leader.CharacterStats.Health.Max;
                bool halfLost = (state.StartCount - ownAlive) * 2 >= state.StartCount;
                bool crippled = halfLost || leaderDead;

                List<string> reasons = new List<string>();
                Stance toward;

                if (current == Stance.Offensive)
                {
                    toward = Stance.Defensive;

                    if (losing) reasons.Add("losing the exchange");
                    if (leaderDead) reasons.Add("leader dead");
                    else if (leaderWeak) reasons.Add("leader badly hurt");
                    if (halfLost) reasons.Add($"lost {state.StartCount - ownAlive} of {state.StartCount}");
                }
                else
                {
                    toward = Stance.Offensive;

                    bool opponentCasualty = opponents.Any(f => f.TacticalActors.Any(a => a.IsDead));

                    if (winning) reasons.Add("winning the exchange");
                    if (opponentCasualty && ownAlive >= OffensiveStrengthRatio * opponentAlive) reasons.Add($"{ownAlive} against {opponentAlive}");

                    int clearShots = CountClearShots(faction, out string who);

                    if (clearShots >= ClearShotsToAttack) reasons.Add($"{clearShots} clear shots ({who})");
                }

                float chance;
                string why;

                if (reasons.Count > 0)
                {
                    chance = toward == Stance.Offensive ? temperament.ToOffensive : temperament.ToDefensive;
                    why = string.Join(", ", reasons);
                }
                else
                {
                    chance = temperament.Feint;
                    why = "feint";
                }

                if (toward == Stance.Offensive && crippled)
                {
                    chance *= CrippledOffensiveFactor;
                }

                bool emergency = leaderJustDied && current == Stance.Offensive;
                string name = FactionName(faction);

                if (emergency)
                {
                    chance = 1f;
                }
                else if (state.TurnsInStance < MinStanceTurns)
                {
                    if (reasons.Count > 0)
                    {
                        TFTVLogger.Always($"TFTV AI stance: {name} would consider {toward} ({why}) but has been {current} for only {state.TurnsInStance} turn(s)");
                    }

                    return false;
                }

                float roll = UnityEngine.Random.value;
                bool switching = roll < chance;

                TFTVLogger.Always($"TFTV AI stance: {name} turn {faction.TurnNumber}: {current}, {(reasons.Count > 0 ? "reasons to go" : "may feint")} {toward} ({why}); " +
                    $"chance {chance:0.00} ({temperament.Name}), roll {roll:0.00}: {(switching ? "switches" : "stays")}");

                if (!switching)
                {
                    return false;
                }

                state.Current = toward;
                state.TurnsInStance = 0;

                if (toward != Stance.Defensive && state.Tactic != Tactic.None)
                {
                    EndTactic(state, $"now {toward}");
                }

                return true;
            }

            /// <summary>
            /// The leader holds near the middle of the rest of its squad, rather than near where it
            /// spawned, so it doesn't sit at the back while the fight moves away from it.
            /// </summary>
            private static void KeepLeaderWithSquad(FactionState state)
            {
                TacticalActor leader = state.Faction.TacticalActors.FirstOrDefault(a => a.IsAlive && IsLeader(a));
                List<TacticalActor> squad = state.Faction.TacticalActors.Where(a => a.IsAlive && !IsLeader(a) && UsesStanceTemplate(a)).ToList();

                if (leader == null || squad.Count == 0)
                {
                    return;
                }

                Vector3 centre = squad.Aggregate(Vector3.zero, (sum, a) => sum + a.Pos) / squad.Count;
                float distance = FlatDistance(leader.Pos, centre);

                if (distance <= AnchorRadius)
                {
                    return;
                }

                // A step at a time: the anchor scores fall to nothing beyond AnchorMaxRadius, so an anchor
                // further than that would leave the leader no spot worth holding.
                Vector3 anchor = Vector3.MoveTowards(leader.Pos, centre, LeaderCatchUp);
                state.Anchors[leader] = anchor;
                TFTVLogger.Always($"TFTV AI tactics: {leader.DisplayName} (leader) is {distance:0}m from its squad's centre, holds toward it around {anchor}");
            }

            private static float TotalHP(TacticalFaction faction)
            {
                return faction.TacticalActors.Where(a => a.IsAlive).Sum(a => (float)a.CharacterStats.Health);
            }

            /// <summary>
            /// Soldiers with the AP for a shot the engine's targeting allows from where they stand, at
            /// an enemy within the weapon's effective range who has no high cover toward them; or the
            /// AP for a grenade, with an enemy they can see within throwing range. Fills in who and at whom, for the log.
            /// </summary>
            private static int CountClearShots(TacticalFaction faction, out string who)
            {
                List<string> shots = new List<string>();

                foreach (TacticalActor actor in faction.TacticalActors.Where(a => a.IsAlive && UsesStanceTemplate(a) && PeekAndFallBackAI.IsRegularSoldier(a)))
                {
                    float ap = actor.CharacterStats.ActionPoints;
                    Weapon weapon = actor.Equipments?.SelectedWeapon;
                    TacticalActorBase target = null;

                    if (weapon != null && PeekAndFallBackAI.IsGun(weapon) && ap >= actor.Equipments.GetAPToUseEquipment(weapon))
                    {
                        target = ShotTargetsFromHere(actor, weapon).FirstOrDefault(e =>
                            FlatDistance(actor.Pos, e.Pos) <= weapon.WeaponDef.EffectiveRange
                            && CoverToward(actor, e.Pos, actor.Pos) < 1f);
                    }

                    if (target != null)
                    {
                        shots.Add($"{actor.DisplayName} at {target.DisplayName} {FlatDistance(actor.Pos, target.Pos):0}m");
                        continue;
                    }

                    foreach (Weapon grenade in Grenades(actor).Where(g => ap >= actor.Equipments.GetAPToUseEquipment(g)))
                    {
                        float reach = grenade.GetDamagePayload().Range;

                        target = KnownEnemies(actor).FirstOrDefault(e =>
                            faction.Vision.IsRevealed(e)
                            && FlatDistance(actor.Pos, e.Pos) <= reach
                            && TacticalFactionVision.CheckVisibleLineBetweenActors(actor, actor.Pos, e, checkAllPoints: true, e.Pos));

                        if (target != null)
                        {
                            shots.Add($"{actor.DisplayName} grenade at {target.DisplayName} {FlatDistance(actor.Pos, target.Pos):0}m");
                            break;
                        }
                    }
                }

                who = string.Join(", ", shots);
                return shots.Count;
            }

            private static IEnumerable<Weapon> Grenades(TacticalActor actor)
            {
                return actor.Equipments?.GetWeapons().Where(w => w.WeaponDef.Tags.Contains(_grenadeTag)) ?? Enumerable.Empty<Weapon>();
            }

            /// <summary>
            /// Infiltration first if the faction has the stealthy soldiers for it, then Counterattack,
            /// and if that fails too, a Counterattack by everyone but the leader.
            /// </summary>
            private static void ChangeTactic(FactionState state, TacticalFaction player)
            {
                bool canInfiltrate = PickInfiltrators(state.Faction).Count >= MinInfiltrators;
                bool canCounterattack = PickAttackers(state.Faction, false).Count > 0;

                if (state.Tactic == Tactic.None && canCounterattack && state.Temperament?.FirstTactic == Tactic.Counterattack)
                {
                    StartTactic(state, Tactic.Counterattack, player);
                }
                else if (state.Tactic == Tactic.None && canInfiltrate)
                {
                    StartTactic(state, Tactic.Infiltration, player);
                }
                else if (state.Tactic != Tactic.Counterattack && canCounterattack)
                {
                    StartTactic(state, Tactic.Counterattack, player);
                }
                else if (!state.AllIn && PickAttackers(state.Faction, true).Count > 0)
                {
                    state.AllIn = true;
                    StartTactic(state, Tactic.Counterattack, player);
                }
                else
                {
                    TFTVLogger.Always($"TFTV AI tactics: {FactionName(state.Faction)} is still losing, nothing else to try; keeps to {state.Tactic}");
                }
            }

            private static void StartTactic(FactionState state, Tactic tactic, TacticalFaction player, bool restored = false)
            {
                state.Tactic = tactic;
                state.Attackers.Clear();
                state.Infiltrators.Clear();
                state.Engaged.Clear();

                List<TacticalActor> units;

                if (tactic == Tactic.Counterattack)
                {
                    units = PickAttackers(state.Faction, state.AllIn);
                    state.Attackers.AddRange(units);
                }
                else if (tactic == Tactic.Infiltration)
                {
                    units = PickInfiltrators(state.Faction);
                    state.Infiltrators.AddRange(units);

                    if (!restored)
                    {
                        state.FlankSide = 0f;
                    }

                    UpdateFlankPoint(state, player);
                }
                else
                {
                    units = new List<TacticalActor>();
                }

                TFTVLogger.Always($"TFTV AI tactics: {FactionName(state.Faction)} {(restored ? "resumes" : "switches to")} {tactic}{(tactic == Tactic.Counterattack && state.AllIn ? " (everyone but the leader)" : "")}" +
                    (units.Count > 0 ? $" with {string.Join(", ", units.Select(u => u.DisplayName))}" : "") +
                    (tactic == Tactic.Infiltration ? $", flank point {state.FlankPoint}" : ""));
            }

            private static void EndTactic(FactionState state, string reason)
            {
                TFTVLogger.Always($"TFTV AI tactics: {FactionName(state.Faction)} ends {state.Tactic} ({reason})");
                state.Tactic = Tactic.None;
                state.Attackers.Clear();
                state.Infiltrators.Clear();
                state.Engaged.Clear();
            }

            private static bool CanTakeRole(TacticalActor actor)
            {
                return actor.IsAlive && UsesStanceTemplate(actor) && PeekAndFallBackAI.IsRegularSoldier(actor) && !IsLeader(actor);
            }

            /// <summary>Everyone but the leader whose weapon has an effective range under 17, or who carries grenades; with allIn, everyone but the leader.</summary>
            private static List<TacticalActor> PickAttackers(TacticalFaction faction, bool allIn)
            {
                return faction.TacticalActors.Where(a =>
                {
                    if (!CanTakeRole(a))
                    {
                        return false;
                    }

                    Weapon weapon = a.Equipments?.SelectedWeapon;
                    return allIn || weapon == null || !PeekAndFallBackAI.IsLongRangeGun(weapon) || Grenades(a).Any();
                }).ToList();
            }

            /// <summary>The stealthiest soldiers with at least +10%, two of them (three from eight units up).</summary>
            private static List<TacticalActor> PickInfiltrators(TacticalFaction faction)
            {
                int wanted = faction.TacticalActors.Count(a => a.IsAlive) >= ThreeInfiltratorsFromSquadSize ? 3 : 2;

                return faction.TacticalActors
                    .Where(a => CanTakeRole(a) && a.CharacterStats.Stealth.Value.EndValue >= InfiltratorStealth)
                    .OrderByDescending(a => a.CharacterStats.Stealth.Value.EndValue)
                    .Take(wanted)
                    .ToList();
            }

            /// <summary>
            /// FlankDistance to one side of the player's centre, square to the line between the two
            /// groups. The side is chosen once, where the player's soldiers have the least cover
            /// facing it; the point follows the player's centre every turn.
            /// </summary>
            private static void UpdateFlankPoint(FactionState state, TacticalFaction player)
            {
                TacticalFaction faction = state.Faction;
                List<TacticalActor> own = faction.TacticalActors.Where(a => a.IsAlive).ToList();
                List<TacticalActor> enemies = player.TacticalActors.Where(a => a.IsAlive && (faction.Vision.IsRevealed(a) || faction.Vision.IsLocated(a))).ToList();

                if (enemies.Count == 0)
                {
                    enemies = player.TacticalActors.Where(a => a.IsAlive).ToList();
                }

                if (own.Count == 0 || enemies.Count == 0)
                {
                    return;
                }

                Vector3 enemyCentre = enemies.Aggregate(Vector3.zero, (sum, a) => sum + a.Pos) / enemies.Count;
                Vector3 ownCentre = own.Aggregate(Vector3.zero, (sum, a) => sum + a.Pos) / own.Count;
                Vector3 axis = enemyCentre - ownCentre;
                axis.y = 0f;
                axis = axis.sqrMagnitude < 0.01f ? Vector3.forward : axis.normalized;
                Vector3 across = Vector3.Cross(Vector3.up, axis);
                TacticalActor probe = own[0];

                if (state.FlankSide == 0f)
                {
                    float Weakness(float side)
                    {
                        Vector3 flank = enemyCentre + across * side * FlankDistance;
                        return enemies.Average(e => 1f - CoverToward(probe, e.Pos, flank));
                    }

                    state.FlankSide = Weakness(-1f) > Weakness(1f) ? -1f : 1f;
                }

                state.FlankPoint = enemyCentre + across * state.FlankSide * FlankDistance;
            }

            /// <summary>An infiltrator attacks once it reaches the flank point, or once it has a shot at an enemy with no high cover toward it.</summary>
            private static void CheckEngaged(FactionState state)
            {
                foreach (TacticalActor infiltrator in state.Infiltrators)
                {
                    if (!infiltrator.IsAlive || state.Engaged.Contains(infiltrator))
                    {
                        continue;
                    }

                    string reason = null;

                    if (FlatDistance(infiltrator.Pos, state.FlankPoint) <= FlankArrived)
                    {
                        reason = "reached the flank point";
                    }
                    else
                    {
                        Weapon weapon = infiltrator.Equipments?.SelectedWeapon;
                        float range = weapon != null ? weapon.GetDamagePayload().Range : 0f;

                        TacticalActorBase target = KnownEnemies(infiltrator).FirstOrDefault(e =>
                            state.Faction.Vision.IsRevealed(e)
                            && Vector3.Distance(infiltrator.Pos, e.Pos) <= range
                            && CoverToward(infiltrator, e.Pos, infiltrator.Pos) < 1f
                            && HasLineOfFire(infiltrator, infiltrator.Pos, e.Pos));

                        if (target != null)
                        {
                            reason = $"has {target.DisplayName} without high cover toward it";
                        }
                    }

                    if (reason != null)
                    {
                        state.Engaged.Add(infiltrator);
                        TFTVLogger.Always($"TFTV AI tactics: {infiltrator.DisplayName} attacks, {reason}");
                    }
                }

                if (state.Infiltrators.Count > 0 && state.Infiltrators.All(a => !a.IsAlive))
                {
                    EndTactic(state, "all infiltrators dead");
                }
            }

            private const int TacticRecordParts = 20;

            /// <summary>Faction name, tactic, flank side, the HP snapshot, the running exchange and the stance.</summary>
            internal static List<string> ExportTactics()
            {
                return States.Values
                    .Where(s => s.Managed)
                    .Select(s => string.Join("|",
                        FactionName(s.Faction),
                        s.Tactic.ToString(),
                        s.FlankSide.ToString(CultureInfo.InvariantCulture),
                        s.HasSnapshot.ToString(),
                        s.OwnHP.ToString(CultureInfo.InvariantCulture),
                        s.OwnAlive.ToString(CultureInfo.InvariantCulture),
                        s.OpponentHP.ToString(CultureInfo.InvariantCulture),
                        s.WindowStartHP.ToString(CultureInfo.InvariantCulture),
                        s.WindowLost.ToString(CultureInfo.InvariantCulture),
                        s.WindowDealt.ToString(CultureInfo.InvariantCulture),
                        s.WindowDeath.ToString(),
                        s.GraceUsed.ToString(),
                        s.AllIn.ToString(),
                        s.Current.ToString(),
                        s.TurnsInStance.ToString(CultureInfo.InvariantCulture),
                        s.OpponentAlive.ToString(CultureInfo.InvariantCulture),
                        s.LeaderAlive.ToString(),
                        s.WindowStartOpponentHP.ToString(CultureInfo.InvariantCulture),
                        s.WindowKill.ToString(),
                        s.Announced.ToString()))
                    .ToList();
            }

            internal static void ImportTactics(List<string> tactics)
            {
                PendingTactics.Clear();

                foreach (string record in tactics ?? new List<string>())
                {
                    string[] parts = record.Split('|');

                    if (parts.Length == TacticRecordParts)
                    {
                        PendingTactics[parts[0]] = parts;
                    }
                }
            }

            private static void RestoreTactic(FactionState state)
            {
                if (!PendingTactics.TryGetValue(FactionName(state.Faction), out string[] parts))
                {
                    return;
                }

                PendingTactics.Remove(parts[0]);

                try
                {
                    state.FlankSide = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    state.HasSnapshot = bool.Parse(parts[3]);
                    state.OwnHP = float.Parse(parts[4], CultureInfo.InvariantCulture);
                    state.OwnAlive = int.Parse(parts[5], CultureInfo.InvariantCulture);
                    state.OpponentHP = float.Parse(parts[6], CultureInfo.InvariantCulture);
                    state.WindowStartHP = float.Parse(parts[7], CultureInfo.InvariantCulture);
                    state.WindowLost = float.Parse(parts[8], CultureInfo.InvariantCulture);
                    state.WindowDealt = float.Parse(parts[9], CultureInfo.InvariantCulture);
                    state.WindowDeath = bool.Parse(parts[10]);
                    state.GraceUsed = bool.Parse(parts[11]);
                    state.AllIn = bool.Parse(parts[12]);

                    // Haven defenders are Defensive whatever was saved.
                    if (!state.Forced)
                    {
                        state.Current = (Stance)Enum.Parse(typeof(Stance), parts[13]);
                    }

                    state.TurnsInStance = int.Parse(parts[14], CultureInfo.InvariantCulture);
                    state.OpponentAlive = int.Parse(parts[15], CultureInfo.InvariantCulture);
                    state.LeaderAlive = bool.Parse(parts[16]);
                    state.WindowStartOpponentHP = float.Parse(parts[17], CultureInfo.InvariantCulture);
                    state.WindowKill = bool.Parse(parts[18]);
                    state.Announced = bool.Parse(parts[19]);

                    Tactic tactic = (Tactic)Enum.Parse(typeof(Tactic), parts[1]);
                    TacticalFaction player = PlayerFaction(state.Faction.TacticalLevel);

                    // Roles are picked again by the same rules; infiltrators re-check their flank on their next turn.
                    if (tactic != Tactic.None && player != null)
                    {
                        StartTactic(state, tactic, player, restored: true);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            #endregion

            #region scoring

            /// <summary>For the squad planner: a holder shoots from where it stands (or a post within half a move) and doesn't charge.</summary>
            internal static bool HoldsPosition(TacticalActor actor)
            {
                return actor != null && IsHolding(actor);
            }

            private static bool IsHolding(TacticalActor actor)
            {
                return UsesStanceTemplate(actor)
                    && PeekAndFallBackAI.IsRegularSoldier(actor)
                    && GetStance(actor.TacticalFaction) != Stance.Offensive
                    && !IsAttacker(actor)
                    && !IsInfiltrating(actor);
            }

            /// <summary>Goes on the offensive for its faction's tactic: a counterattacker, or an infiltrator that has reached its flank.</summary>
            private static bool IsAttacker(TacticalActor actor)
            {
                FactionState state = GetState(actor.TacticalFaction);

                if (state == null || state.Tactic == Tactic.None || GetStance(actor.TacticalFaction) != Stance.Defensive)
                {
                    return false;
                }

                return state.Tactic == Tactic.Counterattack ? state.Attackers.Contains(actor) : state.Engaged.Contains(actor);
            }

            /// <summary>An infiltrator still on its way to the flank.</summary>
            private static bool IsInfiltrating(TacticalActor actor)
            {
                FactionState state = GetState(actor.TacticalFaction);

                return state != null
                    && state.Tactic == Tactic.Infiltration
                    && GetStance(actor.TacticalFaction) == Stance.Defensive
                    && state.Infiltrators.Contains(actor)
                    && !state.Engaged.Contains(actor);
            }

            private static bool IsActiveScout(TacticalActor actor)
            {
                return GetStance(actor.TacticalFaction) == Stance.Cautious && GetState(actor.TacticalFaction).Scouts.Contains(actor);
            }

            private static bool HasLongGun(TacticalActor actor)
            {
                Weapon weapon = actor.Equipments?.SelectedWeapon;
                return weapon != null && PeekAndFallBackAI.IsLongRangeGun(weapon) && PeekAndFallBackAI.IsRegularSoldier(actor);
            }

            /// <summary>The early exits are clones of HasGun; returns false for any other def.</summary>
            internal static bool TryScoreGate(BaseDef def, TacticalActor actor, out float score)
            {
                score = 0f;

                if (actor == null || def == null || HoldActionDef == null)
                {
                    return false;
                }

                if (def == _shouldHoldDef)
                {
                    score = IsHolding(actor)
                        && !IsActiveScout(actor)
                        && !HeldThisTurn.Contains(actor)
                        && !HasActedThisTurn(actor) ? 1f : 0f;
                    return true;
                }

                if (def == _shouldScoutDef)
                {
                    score = UsesStanceTemplate(actor)
                        && PeekAndFallBackAI.IsRegularSoldier(actor)
                        && IsActiveScout(actor)
                        && !ScoutedThisTurn.Contains(actor) ? 1f : 0f;
                    return true;
                }

                if (def == _shouldWatchDef)
                {
                    // A soldier should move or hold first; holders whose hold kept them in place count as done.
                    bool actedThisTurn = HasActedThisTurn(actor) || HeldThisTurn.Contains(actor);

                    score = UsesStanceTemplate(actor)
                        && PeekAndFallBackAI.IsRegularSoldier(actor)
                        && (IsHolding(actor) || HasLongGun(actor))
                        && !IsActiveScout(actor)
                        && actedThisTurn
                        && !ShouldShootFirst(actor) ? 1f : 0f;
                    return true;
                }

                if (def == _notHoldingDef)
                {
                    score = IsHolding(actor) || IsInfiltrating(actor) ? 0f : 1f;
                    return true;
                }

                if (def == _shouldInfiltrateDef)
                {
                    score = PeekAndFallBackAI.IsRegularSoldier(actor)
                        && IsInfiltrating(actor)
                        && !InfiltratedThisTurn.Contains(actor) ? 1f : 0f;
                    return true;
                }

                if (def == _notLongGunnerDef)
                {
                    score = UsesStanceTemplate(actor) && HasLongGun(actor) ? 0f : 1f;
                    return true;
                }

                if (def == _isLongGunnerDef)
                {
                    score = HasLongGun(actor) ? 1f : 0f;
                    return true;
                }

                return false;
            }

            /// <summary>The position scores are clones of RoofAbove; returns false for any other def.</summary>
            internal static bool TryScorePosition(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 1f;

                if (actor == null || target == null || def == null || HoldActionDef == null)
                {
                    return false;
                }

                if (def == _wallsDef)
                {
                    score = CrossesOverwatch(actor, target.Pos) ? 0f : CoverAt(actor, target.Pos);
                    return true;
                }

                if (def == _fallBackCoverDef)
                {
                    // Must not walk through an Overwatch cone either.
                    if (CrossesOverwatch(actor, target.Pos))
                    {
                        score = 0f;
                        return true;
                    }

                    float there = CoverAt(actor, target.Pos);
                    score = there >= Mathf.Min(FallBackMinCover, CoverHere(actor)) - 0.01f ? Mathf.Max(0.1f, there) : 0f;
                    return true;
                }

                if (def == _anchorDef)
                {
                    score = CrossesOverwatch(actor, target.Pos) ? 0f : NearAnchor(actor, target.Pos);
                    return true;
                }

                if (def == _coveredWhenHoldingDef)
                {
                    if (!IsHolding(actor) || IsOwnTile(actor, target.Pos))
                    {
                        score = 1f;
                    }
                    else if (CrossesOverwatch(actor, target.Pos))
                    {
                        score = 0f;
                    }
                    else
                    {
                        float post = FiringPostTowardNearest(actor, target.Pos, out _);
                        score = post >= MinPostToMoveTo ? post : 0f;
                    }

                    return true;
                }

                if (def == _notBleedingVampireDef)
                {
                    score = IsWoundedVampire(actor) ? WoundedVampireShotFactor : 1f;
                    return true;
                }

                if (def == _exposureDef)
                {
                    score = Exposure(actor, target.Pos);
                    return true;
                }

                if (def == _flankPointDef)
                {
                    score = CrossesOverwatch(actor, target.Pos) ? 0f : TowardFlank(actor, target.Pos);
                    return true;
                }

                if (def == _firingPostDef)
                {
                    score = IsGunner(actor) ? FiringPostTowardNearest(actor, target.Pos, out _) : 1f;
                    return true;
                }

                if (def == _canStillWatchDef)
                {
                    score = CanStillWatchFrom(actor, target) ? 1f : 0f;
                    return true;
                }

                if (def == _ownTileDef)
                {
                    score = IsOwnTile(actor, target.Pos) ? 1f : 0f;
                    return true;
                }

                return false;
            }

            private static bool IsOwnTile(TacticalActor actor, Vector3 pos)
            {
                return (pos - actor.Pos).sqrMagnitude <= 0.25f;
            }

            /// <summary>Records the AP an actor starts its turn with, the first time it is looked at.</summary>
            private static bool HasActedThisTurn(TacticalActor actor)
            {
                float ap = actor.CharacterStats.ActionPoints;

                if (!TurnStartAP.TryGetValue(actor, out float start))
                {
                    TurnStartAP.Add(actor, ap);
                    return false;
                }

                return ap < start - 0.01f;
            }

            /// <summary>
            /// A gunner with a known enemy in its line of fire, and the AP to shoot and still set
            /// Overwatch afterwards, shoots first: the watch waits until the shot is taken.
            /// </summary>
            private static bool ShouldShootFirst(TacticalActor actor)
            {
                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (weapon == null || !PeekAndFallBackAI.IsGun(weapon))
                {
                    return false;
                }

                OverwatchAbility overwatch = actor.GetAbility<OverwatchAbility>(weapon);

                if (overwatch == null)
                {
                    return false;
                }

                float shotCost = actor.Equipments.GetAPToUseEquipment(weapon);

                if (actor.CharacterStats.ActionPoints < shotCost + overwatch.ActionPointCost)
                {
                    return false;
                }

                // The engine's own targets: a guessed line of fire once left the leader blocked from
                // watching with no shot the AI would take, so it ended its turn doing nothing.
                TacticalActorBase target = ShotTargetFromHere(actor, weapon);

                if (target != null && ShootFirstLogged.Add(actor))
                {
                    TFTVLogger.Always($"TFTV AI stance: {actor.DisplayName} has a shot at {target.DisplayName} " +
                        $"({Vector3.Distance(actor.Pos, target.Pos):0.0}m) and AP for it plus Overwatch " +
                        $"({(float)actor.CharacterStats.ActionPoints:0.#} >= {shotCost:0.#} + {overwatch.ActionPointCost:0.#}), so it shoots before watching");
                }

                return target != null;
            }

            /// <summary>A clear line from the spot, or from a step-out of the high cover in front of it, to the enemy.</summary>
            private static bool HasLineOfFire(TacticalActor actor, Vector3 pos, Vector3 enemyPos)
            {
                float distance = Vector3.Distance(pos, enemyPos);
                return FiringPost(actor, pos, enemyPos, out _, Mathf.Max(1f, distance - 1.5f)) > 0f;
            }

            private static IEnumerable<TacticalActorBase> KnownEnemies(TacticalActor actor)
            {
                return actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.Combatant), true)
                    .Where(e => e != null && e.IsAlive);
            }

            /// <summary>
            /// Before contact, high walls around the spot. After, the cover between the spot and the
            /// nearest known enemies, so a corner open toward them scores nothing however many walls it has.
            /// </summary>
            private static float CoverHere(TacticalActor actor)
            {
                int frame = Time.frameCount;

                if (!CoverHereByFrame.TryGetValue(actor, out KeyValuePair<int, float> cached) || cached.Key != frame)
                {
                    cached = new KeyValuePair<int, float>(frame, CoverAt(actor, actor.Pos));
                    CoverHereByFrame[actor] = cached;
                }

                return cached.Value;
            }

            private static float CoverAt(TacticalActor actor, Vector3 pos)
            {
                List<TacticalActorBase> enemies = KnownEnemies(actor).OrderBy(e => (e.Pos - pos).sqrMagnitude).Take(CoverEnemies).ToList();

                if (enemies.Count == 0)
                {
                    return WallsAround(actor, pos);
                }

                return enemies.Average(e => CoverToward(actor, pos, e.Pos));
            }

            /// <summary>1 at the flank point, falling to 0 at ten metres further away than the infiltrator is now.</summary>
            private static float TowardFlank(TacticalActor actor, Vector3 pos)
            {
                FactionState state = GetState(actor.TacticalFaction);

                if (state == null)
                {
                    return 0f;
                }

                float now = FlatDistance(actor.Pos, state.FlankPoint);
                float there = FlatDistance(pos, state.FlankPoint);

                return Mathf.Clamp01(1f - there / (now + 10f));
            }

            private static float FlatDistance(Vector3 a, Vector3 b)
            {
                return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            }

            /// <summary>1 behind high cover toward the enemy, 0.5 behind low cover, 0 in the open; diagonals share between the two sides.</summary>
            private static float CoverToward(TacticalActor actor, Vector3 pos, Vector3 enemyPos)
            {
                Vector3 direction = enemyPos - pos;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                {
                    return 0f;
                }

                direction.Normalize();
                float height = actor.TacticalPerception.Height;
                float covered = 0f;
                float total = 0f;

                foreach (Vector3 side in GridDirections)
                {
                    float weight = Vector3.Dot(side, direction);

                    if (weight < 0.3f)
                    {
                        continue;
                    }

                    CoverType cover = actor.Map.GetCoverInfoInDirection(pos, side, height).CoverType;
                    covered += weight * (cover == CoverType.High ? 1f : cover == CoverType.Low ? 0.5f : 0f);
                    total += weight;
                }

                return total > 0f ? covered / total : 0f;
            }

            /// <summary>
            /// 1 where no known enemy can see the spot. Seen, it scores SeenSpot, and a gunner gets
            /// credit for the cover between it and the enemies who see it: that is a firing post.
            /// </summary>
            private static float Exposure(TacticalActor actor, Vector3 pos)
            {
                List<TacticalActorBase> seers = KnownEnemies(actor).Where(e => CanSee(e, actor, pos)).ToList();

                if (seers.Count == 0)
                {
                    return 1f;
                }

                if (!IsGunner(actor))
                {
                    return SeenSpot;
                }

                return SeenSpot + (1f - SeenSpot) * seers.Average(e => CoverToward(actor, pos, e.Pos));
            }

            private static bool CanSee(TacticalActorBase enemy, TacticalActor actor, Vector3 pos)
            {
                float perception = enemy is TacticalActor seer ? seer.GetAdjustedPerceptionValue() : 30f;

                return (enemy.Pos - pos).sqrMagnitude < perception * perception
                    && TacticalFactionVision.CheckVisibleLineBetweenActors(enemy, enemy.Pos, actor, checkAllPoints: true, pos);
            }

            /// <summary>Walking straight from the actor to pos passes through an enemy's Overwatch cone.</summary>
            private static bool CrossesOverwatch(TacticalActor actor, Vector3 pos)
            {
                if (IsOwnTile(actor, pos))
                {
                    return false;
                }

                List<Cone> cones = actor.Map.GetActors<TacticalActor>()
                    .Where(a => a.IsAlive && a.RelationTo(actor) == FactionRelation.Enemy)
                    .Select(a => a.Status?.GetStatus<OverwatchStatus>())
                    .Where(s => s != null)
                    .Select(s => s.GetCone())
                    .ToList();

                if (cones.Count == 0)
                {
                    return false;
                }

                Vector3 start = actor.Pos + Vector3.up;
                Vector3 end = pos + Vector3.up;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / 0.5f));

                for (int i = 1; i <= steps; i++)
                {
                    Vector3 point = Vector3.Lerp(start, end, (float)i / steps);

                    if (cones.Any(c => c.Contains(point)))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>Below WoundedVampireHealth of its HP, with a blood-sucking arm it can use (a regenerating torso can mend a disabled one).</summary>
            private static bool IsWoundedVampire(TacticalActor actor)
            {
                if ((float)actor.CharacterStats.Health >= WoundedVampireHealth * actor.CharacterStats.Health.Max)
                {
                    return false;
                }

                Weapon sucker = actor.Equipments?.GetWeapons().FirstOrDefault(w => w.WeaponDef.name.Contains("BloodSucker") && w.IsUsable);

                if (sucker == null)
                {
                    return false;
                }

                if (WoundedVampireLogged.Add(actor))
                {
                    TFTVLogger.Always($"TFTV AI: {actor.DisplayName} is badly hurt ({(float)actor.CharacterStats.Health:0} of {actor.CharacterStats.Health.Max:0} HP) " +
                        $"and has {sucker.WeaponDef.name}: its shots count for {WoundedVampireShotFactor:0.0}, it would rather bite to heal");
                }

                return true;
            }

            private static bool IsGunner(TacticalActor actor)
            {
                Weapon weapon = actor.Equipments?.SelectedWeapon;
                return weapon != null && PeekAndFallBackAI.IsGun(weapon);
            }

            /// <summary>
            /// A gunner's hold may only spend the AP it can spare and still set Overwatch afterwards,
            /// so a sniper (3 AP) stays put or shifts a tile, where it used to walk off and then had no
            /// AP left to watch.
            /// </summary>
            private static bool CanStillWatchFrom(TacticalActor actor, TacAITarget target)
            {
                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (weapon == null || !PeekAndFallBackAI.IsGun(weapon) || IsOwnTile(actor, target.Pos))
                {
                    return true;
                }

                OverwatchAbility overwatch = actor.GetAbility<OverwatchAbility>(weapon);

                if (overwatch == null)
                {
                    return true;
                }

                float needed = overwatch.ActionPointCost;

                // With a shot on offer from here, the walk must not cost that shot: a sniper short on AP shoots instead of shuffling.
                if (HasShotFromHere(actor, weapon))
                {
                    needed = Mathf.Max(needed, actor.Equipments.GetAPToUseEquipment(weapon));
                }

                return actor.CharacterStats.ActionPoints - target.PathLength >= needed;
            }

            /// <summary>A revealed enemy in range and in the line of fire from where the actor stands, and the AP to shoot it. Cached per frame.</summary>
            internal static bool HasShotFromHere(TacticalActor actor, Weapon weapon)
            {
                if (ShotFromHereFrame.TryGetValue(actor, out KeyValuePair<int, bool> cached) && cached.Key == Time.frameCount)
                {
                    return cached.Value;
                }

                bool shot = actor.CharacterStats.ActionPoints >= actor.Equipments.GetAPToUseEquipment(weapon)
                    && ShotTargetFromHere(actor, weapon) != null;

                ShotFromHereFrame[actor] = new KeyValuePair<int, bool>(Time.frameCount, shot);
                return shot;
            }

            /// <summary>
            /// An enemy the weapon's own shoot ability can target from where the actor stands: the
            /// engine's line-of-fire and step-out rules, as the player's targeting uses them.
            /// </summary>
            /// <summary>The first enemy the weapon could shoot from where the actor stands, by the engine's rules; null if none.</summary>
            internal static TacticalActorBase ShotTargetFromWhereItStands(TacticalActor actor, Weapon weapon)
            {
                return ShotTargetFromHere(actor, weapon);
            }

            private static TacticalActorBase ShotTargetFromHere(TacticalActor actor, Weapon weapon)
            {
                return ShotTargetsFromHere(actor, weapon).FirstOrDefault();
            }

            private static IEnumerable<TacticalActorBase> ShotTargetsFromHere(TacticalActor actor, Weapon weapon)
            {
                ShootAbility shoot = actor.GetAbilities<ShootAbility>().FirstOrDefault(a => a.Weapon == weapon && a.IsDefault)
                    ?? actor.GetAbilities<ShootAbility>().FirstOrDefault(a => a.Weapon == weapon);

                if (shoot == null)
                {
                    return Enumerable.Empty<TacticalActorBase>();
                }

                return shoot.GetTargetsAt(actor.Pos)
                    .Select(t => t.GetTargetActor())
                    .Where(e => e != null && e.IsAlive && e.RelationTo(actor) == FactionRelation.Enemy
                        && actor.TacticalFaction.Vision.IsRevealed(e))
                    .Distinct();
            }

            /// <summary>
            /// Score of the watch action's target: an enemy the faction knows of, near enough to walk
            /// into a cone as long as perception and weapon range allow. Nearer is better.
            /// </summary>
            internal static bool TryScoreWatchTarget(BaseDef def, TacticalActor actor, TacAITarget target, out float score)
            {
                score = 0f;

                if (_watchTargetDef == null || def != _watchTargetDef)
                {
                    return false;
                }

                if (!(target.Equipment is Weapon weapon) || target.Actor == null || target.Actor.IsDead)
                {
                    return true;
                }

                OverwatchAbility overwatch = actor.GetAbility<OverwatchAbility>(weapon);

                if (overwatch == null || !overwatch.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreEquipmentNotSelected))
                {
                    return true;
                }

                if (!actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.Combatant), true).Contains(target.Actor))
                {
                    return true;
                }

                float reach = Mathf.Min(actor.GetAdjustedPerceptionValue(), weapon.GetDamagePayload().Range) + WatchApproachMargin;
                float distance = Vector2.Distance(new Vector2(target.Pos.x, target.Pos.z), new Vector2(target.Actor.Pos.x, target.Actor.Pos.z));

                score = distance > reach ? 0f : 0.5f + 0.5f * (1f - distance / reach);

                // No Overwatch at a wall: only where a shot toward the enemy can actually come out.
                if (score > 0f)
                {
                    score *= FiringPost(actor, target.Pos, target.Actor.Pos, out _);
                }

                // Face where the known enemies are rather than a lone one off to the side.
                if (score > 0f)
                {
                    score *= 0.5f + 0.5f * ShareInDirection(actor, target.Pos, target.Actor.Pos, reach);
                }

                return true;
            }

            /// <summary>Share of the known enemies within reach that lie within WatchClusterAngle of the line from pos toward enemyPos.</summary>
            private static float ShareInDirection(TacticalActor actor, Vector3 pos, Vector3 enemyPos, float reach)
            {
                Vector3 line = enemyPos - pos;
                line.y = 0f;
                int near = 0;
                int along = 0;

                foreach (TacticalActorBase enemy in KnownEnemies(actor))
                {
                    Vector3 toEnemy = enemy.Pos - pos;
                    toEnemy.y = 0f;

                    if (toEnemy.magnitude > reach)
                    {
                        continue;
                    }

                    near++;

                    if (Vector3.Angle(line, toEnemy) <= WatchClusterAngle)
                    {
                        along++;
                    }
                }

                return near > 0 ? (float)along / near : 1f;
            }

            /// <summary>1 with high cover on three or more sides; low cover counts a quarter.</summary>
            private static float WallsAround(TacticalActor actor, Vector3 pos)
            {
                TacticalMap map = actor.Map;
                float height = actor.TacticalPerception.Height;
                float sides = 0f;

                foreach (Vector3 direction in GridDirections)
                {
                    CoverType cover = map.GetCoverInfoInDirection(pos, direction, height).CoverType;

                    if (cover == CoverType.High)
                    {
                        sides += 1f;
                    }
                    else if (cover == CoverType.Low)
                    {
                        sides += 0.25f;
                    }
                }

                return Mathf.Clamp01(sides / 3f);
            }

            private delegate bool StepOutCheck(TacticalPerception perception, TacticalAbilityTarget target, Vector3 actorPos, ref Vector3 shootPos);

            private static StepOutCheck _isValidStepOut;

            /// <summary>The engine's own test (the target argument is unused), so the AI steps out exactly where shots can.</summary>
            private static bool IsValidStepOut(TacticalPerception perception, Vector3 pos, ref Vector3 stepOut)
            {
                if (_isValidStepOut == null)
                {
                    _isValidStepOut = AccessTools.MethodDelegate<StepOutCheck>(AccessTools.Method(typeof(TacticalPerception), "IsValidStepOutPos"));
                }

                return _isValidStepOut(perception, null, pos, ref stepOut);
            }

            private static TacticalActorBase NearestKnownEnemy(TacticalActor actor, Vector3 pos)
            {
                TacticalActorBase nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (TacticalActorBase enemy in actor.TacticalFaction.AIBlackboard.GetEnemies(actor.AIActor.GetEnemyMask(ActorType.Combatant), true))
                {
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    float distance = (enemy.Pos - pos).sqrMagnitude;

                    if (distance < nearestDistance)
                    {
                        nearest = enemy;
                        nearestDistance = distance;
                    }
                }

                return nearest;
            }

            /// <summary>1 when no enemy is known yet: there's nothing to face, so the other scores decide.</summary>
            private static float FiringPostTowardNearest(TacticalActor actor, Vector3 pos, out string kind)
            {
                TacticalActorBase enemy = NearestKnownEnemy(actor, pos);

                if (enemy == null)
                {
                    kind = "no known enemy";
                    return 1f;
                }

                return FiringPost(actor, pos, enemy.Pos, out kind);
            }

            /// <summary>
            /// How well a gunner standing at pos covers the way to an enemy, by the player's rules for
            /// shooting from cover. Behind high cover facing the enemy, the shot steps out one tile to
            /// either side: the spot is a post if that step is allowed (the engine's own test) and the
            /// line from there is clear. Otherwise the line from the spot itself has to be clear, and it
            /// scores less over low cover and less again in the open. A corner whose step-out runs into
            /// the other wall, or whose cover goes on for two tiles, scores 0.
            /// </summary>
            private static float FiringPost(TacticalActor actor, Vector3 pos, Vector3 enemyPos, out string kind, float lane = LaneLength)
            {
                Vector3 toEnemy = enemyPos - pos;
                toEnemy.y = 0f;
                float distance = toEnemy.magnitude;

                if (distance < 3f)
                {
                    kind = "enemy next to it";
                    return StepOutPost;
                }

                Vector3 direction = toEnemy / distance;
                lane = Mathf.Min(lane, distance - 1.5f);
                TacticalPerception perception = actor.TacticalPerception;
                Vector3 facing = GridDirections.OrderByDescending(d => Vector3.Dot(d, direction)).First();
                CoverType cover = actor.Map.GetCoverInfoInDirection(pos, facing, perception.Height).CoverType;

                if (cover == CoverType.High)
                {
                    Weapon weapon = actor.Equipments?.SelectedWeapon;
                    Vector3 side = Vector3.Cross(Vector3.up, facing).normalized;
                    float best = 0f;

                    if (weapon != null)
                    {
                        foreach (bool right in new[] { true, false })
                        {
                            float step = StepOutPathProcessor.GetStepoutDistance(actor, weapon, right);

                            if (float.IsInfinity(step))
                            {
                                continue;
                            }

                            Vector3 stepOut = right ? pos + side * step : pos - side * step;

                            if (IsValidStepOut(perception, pos, ref stepOut))
                            {
                                best = Mathf.Max(best, LaneScore(perception.GetVisionPointAt(stepOut), enemyPos, lane));
                            }
                        }
                    }

                    kind = best > 0f ? "steps out of high cover" : "blind behind high cover";
                    return best * StepOutPost;
                }

                float open = LaneScore(perception.GetVisionPointAt(pos), enemyPos, lane);
                kind = open <= 0f ? "blind" : cover == CoverType.Low ? "over low cover" : "in the open";
                return open * (cover == CoverType.Low ? LowCoverPost : OpenPost);
            }

            /// <summary>Half-angle of the side rays, in degrees: about the narrowest Overwatch cone.</summary>
            private const float LaneSpread = 10f;

            /// <summary>0 if the straight line toward the enemy is blocked within the lane, else 0.6 plus 0.2 for each clear side ray.</summary>
            private static float LaneScore(Vector3 eye, Vector3 enemyPos, float lane)
            {
                Vector3 direction = (enemyPos + Vector3.up - eye).normalized;

                if (Physics.Raycast(eye, direction, lane, UnityLayers.BlockingAll))
                {
                    return 0f;
                }

                float score = 0.6f;

                foreach (float angle in new[] { LaneSpread, -LaneSpread })
                {
                    if (!Physics.Raycast(eye, Quaternion.AngleAxis(angle, Vector3.up) * direction, lane, UnityLayers.BlockingAll))
                    {
                        score += 0.2f;
                    }
                }

                return score;
            }

            /// <summary>
            /// For the logs, when a gunner of a faction with stances ends its turn with AP left
            /// and no Overwatch: why it neither shot nor watched.
            /// </summary>
            internal static void LogEndTurn(TacticalActor actor)
            {
                if (HoldActionDef == null || actor?.TacticalFaction == null || !UsesStanceTemplate(actor)
                    || GetState(actor.TacticalFaction)?.Managed != true || actor.Status?.HasStatus<OverwatchStatus>() == true)
                {
                    return;
                }

                Weapon weapon = actor.Equipments?.SelectedWeapon;

                if (weapon == null || !PeekAndFallBackAI.IsGun(weapon) || actor.CharacterStats.ActionPoints <= 0f)
                {
                    return;
                }

                OverwatchAbility overwatch = actor.GetAbility<OverwatchAbility>(weapon);
                string watch = overwatch == null
                    ? "no Overwatch ability"
                    : $"Overwatch {overwatch.ActionPointCost:0.#} AP, {(overwatch.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreEquipmentNotSelected) ? "enabled" : "disabled: " + overwatch.GetDisabledState(IgnoredAbilityDisabledStatesFilter.IgnoreEquipmentNotSelected))}";

                TryScoreGate(_shouldWatchDef, actor, out float gate);

                float reach = Mathf.Min(actor.GetAdjustedPerceptionValue(), weapon.GetDamagePayload().Range) + WatchApproachMargin;
                string best = "no known enemy";
                float bestScore = -1f;

                foreach (TacticalActorBase enemy in KnownEnemies(actor))
                {
                    TacAITarget target = new TacAITarget { Pos = actor.Pos, Actor = enemy, Equipment = weapon };
                    TryScoreWatchTarget(_watchTargetDef, actor, target, out float score);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = $"best watch target {enemy.DisplayName} {FlatDistance(actor.Pos, enemy.Pos):0.0}m (reach {reach:0}), score {score:0.00}, {DescribePost(actor, enemy.Pos)}";
                    }
                }

                TFTVLogger.Always($"TFTV AI stance: {actor.DisplayName} ends its turn with {(float)actor.CharacterStats.ActionPoints:0.#} of {actor.CharacterStats.ActionPoints.Max:0.#} AP: " +
                    $"shot {actor.Equipments.GetAPToUseEquipment(weapon):0.#} AP, {watch}; holding {IsHolding(actor)}, watch gate {gate:0}; {best}");
            }

            /// <summary>For the logs.</summary>
            internal static string DescribeCover(TacticalActor actor, Vector3 pos)
            {
                return $"{CoverAt(actor, pos):0.00}";
            }

            /// <summary>For the logs.</summary>
            internal static string DescribePost(TacticalActor actor, Vector3 enemyPos)
            {
                float post = FiringPost(actor, actor.Pos, enemyPos, out string kind);
                return $"post {post:0.00}, {kind}";
            }

            private static float NearAnchor(TacticalActor actor, Vector3 pos)
            {
                FactionState state = GetState(actor.TacticalFaction);

                if (!state.Anchors.TryGetValue(actor, out Vector3 anchor))
                {
                    anchor = actor.Pos;
                    state.Anchors.Add(actor, anchor);
                }

                float distance = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(anchor.x, anchor.z));
                float score = distance <= AnchorRadius ? 1f : Mathf.Clamp01(1f - (distance - AnchorRadius) / (AnchorMaxRadius - AnchorRadius));

                if ((pos - actor.Pos).sqrMagnitude > 0.25f)
                {
                    // Once the enemy has moved round so that the spot no longer covers, leaving it costs little.
                    score *= CoverAt(actor, actor.Pos) < PoorCover ? PoorCoverMoveAwayFactor : MoveAwayFactor;
                }
                else if (PeekAndFallBackAI.IsSeen(actor) && !(IsGunner(actor) && FiringPostTowardNearest(actor, pos, out _) >= MinPostToMoveTo))
                {
                    // A gunner seen at a good firing post stays: being seen from there is the point.
                    score *= SeenHereFactor;
                }

                return score;
            }

            #endregion

            #region bookkeeping

            /// <summary>
            /// Called from TFTVArtOfCrab's move prefix. Returns true for the hold and scout moves, which
            /// StepOutOfCoverFix must leave alone: both go to spots nobody sees on purpose.
            /// </summary>
            internal static bool RecordMove(AIActionDef actionDef, TacticalActor actor, TacAITarget target)
            {
                if (HoldActionDef == null || (actionDef != HoldActionDef && actionDef != ScoutActionDef && actionDef != InfiltrateActionDef))
                {
                    return false;
                }

                if (actionDef == InfiltrateActionDef)
                {
                    InfiltratedThisTurn.Add(actor);
                    FactionState state = GetState(actor.TacticalFaction);

                    TFTVLogger.Always($"TFTV AI tactics: {actor.DisplayName} infiltrates, moving {Vector3.Distance(actor.Pos, target.Pos):0.0}m to {target.Pos}, " +
                        $"{FlatDistance(target.Pos, state.FlankPoint):0.0}m from the flank point, exposure {Exposure(actor, target.Pos):0.00}");

                    return true;
                }

                bool hold = actionDef == HoldActionDef;
                (hold ? HeldThisTurn : ScoutedThisTurn).Add(actor);

                // A holder's new spot is its home from now on, so it doesn't drift back toward where it spawned.
                if (hold)
                {
                    GetState(actor.TacticalFaction).Anchors[actor] = target.Pos;
                }

                float moved = Vector3.Distance(actor.Pos, target.Pos);
                string post = "";

                if (hold && IsGunner(actor))
                {
                    float postScore = FiringPostTowardNearest(actor, target.Pos, out string kind);
                    post = $", post {postScore:0.00} ({kind})";
                }

                TFTVLogger.Always($"TFTV AI stance: {actor.DisplayName} {(hold ? "holds" : "scouts")}, moving {moved:0.0}m to {target.Pos}" +
                    (hold ? $", cover {CoverAt(actor, target.Pos):0.00}, exposure {Exposure(actor, target.Pos):0.00}{post}" : ""));

                return true;
            }

            /// <summary>Called at the start of every faction's turn.</summary>
            internal static void ClearTurn()
            {
                HeldThisTurn.Clear();
                ScoutedThisTurn.Clear();
                CoverHereByFrame.Clear();
                TurnStartAP.Clear();
                ShootFirstLogged.Clear();
                InfiltratedThisTurn.Clear();
                WoundedVampireLogged.Clear();
                ShotFromHereFrame.Clear();
            }

            /// <summary>Called on every level change and load, before the save data comes back.</summary>
            internal static void ClearMission()
            {
                States.Clear();
                ContactFactions.Clear();
                PendingTactics.Clear();
                ClearTurn();
            }

            internal static List<string> ExportContacts()
            {
                return ContactFactions.ToList();
            }

            internal static void ImportContacts(List<string> contacts)
            {
                ContactFactions.Clear();

                if (contacts != null)
                {
                    ContactFactions.UnionWith(contacts);
                }
            }

            #endregion
        }

        /// <summary>
        /// AI Overwatch cones follow the player's rules: the weapon class's default angle (by the share
        /// of AP its shot costs) and as long as perception and weapon range allow. Stock AI asks for a
        /// fixed 60 degree cone one effective range long.
        /// </summary>
        [HarmonyPatch(typeof(OverwatchAbility), nameof(OverwatchAbility.GetAbilityTargetCone))]
        internal static class TFTV_OverwatchAbility_GetAbilityTargetCone_patch
        {
            /// <summary>Far enough that GetAbilityTargetCone always clamps to perception or weapon range.</summary>
            private const float FarAway = 1000f;

            public static void Prefix(OverwatchAbility __instance, ref Vector3 targetPosition, ref float spreadAngle)
            {
                try
                {
                    TacticalActor actor = __instance.TacticalActor;

                    if (actor?.TacticalFaction == null || !actor.TacticalFaction.IsControlledByAI || __instance.OverwatchWeapon == null)
                    {
                        return;
                    }

                    Vector3 direction = targetPosition - actor.Pos;
                    direction.y = 0f;

                    if (direction.sqrMagnitude < 0.01f)
                    {
                        return;
                    }

                    targetPosition = actor.Pos + direction.normalized * FarAway;
                    spreadAngle = __instance.GetConeSpreadSettings().DefaultValue * Mathf.Deg2Rad;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AIActionOverwatch), nameof(AIActionOverwatch.Execute))]
        internal static class TFTV_AIActionOverwatch_Execute_patch
        {
            public static void Prefix(AIActionOverwatch __instance, AIScoredTarget aiTarget)
            {
                try
                {
                    if (Stances.WatchActionDef != null && __instance.ActionDef == Stances.WatchActionDef
                        && aiTarget.Actor is TacticalActor actor && aiTarget.Target is TacAITarget target)
                    {
                        TFTVLogger.Always($"TFTV AI stance: {actor.DisplayName} watches from cover toward {target.Actor?.DisplayName} " +
                            $"({Vector3.Distance(actor.Pos, target.Actor?.Pos ?? actor.Pos):0.0}m away" +
                            (target.Actor != null ? $", {Stances.DescribePost(actor, target.Actor.Pos)})" : ")"));
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AIActionEndCharacterTurn), nameof(AIActionEndCharacterTurn.Execute))]
        internal static class TFTV_AIActionEndCharacterTurn_Execute_patch
        {
            public static void Prefix(AIScoredTarget aiTarget)
            {
                try
                {
                    Stances.LogEndTurn(aiTarget?.Actor as TacticalActor);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AIRoofAboveConsideration), nameof(AIRoofAboveConsideration.Evaluate))]
        internal static class TFTV_AIRoofAboveConsideration_Evaluate_patch
        {
            public static bool Prefix(AIRoofAboveConsideration __instance, IAIActor actor, IAITarget target, ref float __result)
            {
                try
                {
                    if (Stances.TryScorePosition(__instance.BaseDef, actor as TacticalActor, target as TacAITarget, out float score)
                        || MistSeeking.TryScorePosition(__instance.BaseDef, actor as TacticalActor, target as TacAITarget, out score)
                        || Scavenging.TryScorePosition(__instance.BaseDef, actor as TacticalActor, target as TacAITarget, out score))
                    {
                        __result = score;
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        #endregion
    }
}
