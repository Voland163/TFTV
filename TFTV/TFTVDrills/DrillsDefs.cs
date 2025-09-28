using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TFTV.TFTVDrills.DrillsHarmony;
using static TFTV.TFTVDrills.DrillsPublicClasses;

namespace TFTV.TFTVDrills
{
    internal sealed class DrillClassLevelRequirement
    {
        public ClassTagDef ClassTag;
        public int MinimumLevel = 1;
        public bool RequireSelectedOperative; // TODO: confirm whether this should only check the viewing operative.
    }

    internal sealed class DrillUnlockCondition
    {
        public bool AlwaysAvailable;
        public List<string> RequiredResearchIds { get; } = new List<string>();
        public List<DrillClassLevelRequirement> ClassLevelRequirements { get; } = new List<DrillClassLevelRequirement>();
        public bool RequireAnyPhoenixOperative; // TODO: clarify how mixed class requirements should behave.

        public static DrillUnlockCondition AlwaysUnlocked()
        {
            return new DrillUnlockCondition { AlwaysAvailable = true };
        }
    }


    internal class DrillsDefs
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static readonly Dictionary<TacticalAbilityDef, DrillUnlockCondition> DrillUnlockConditions = new Dictionary<TacticalAbilityDef, DrillUnlockCondition>();

        internal static DamageMultiplierStatusDef _drawfireStatus;
        internal static DamageMultiplierStatusDef _markedwatchStatus;
        internal static GameTagDef OrdnanceResupplyTag;

        internal static PassiveModifierAbilityDef _causticJamming;
        internal static PassiveModifierAbilityDef _mentorProtocol;
        internal static PassiveModifierAbilityDef _virulentGrip;
        internal static PassiveModifierAbilityDef _viralPuppeteer;
        internal static PassiveModifierAbilityDef _toxicLink;
        internal static PassiveModifierAbilityDef _shieldedRiposte;
        internal static ApplyStatusAbilityDef _mightMakesRight;
        internal static PassiveModifierAbilityDef _packLoyalty;
        internal static PassiveModifierAbilityDef _shockDiscipline;
        internal static LightStunStatusDef _shockDisciplineStatus;
        internal static PassiveModifierAbilityDef _snapBrace;
        internal static ShootAbilityDef _partingShot;
        internal static ReloadAbilityDef _ordnanceResupply;
        internal static PassiveModifierAbilityDef _pinpointToss;


        internal static ApplyStatusAbilityDef _bulletHell;
        internal static AddAttackBoostStatusDef _bulletHellAttackBoostStatus;
        internal static TacStatsModifyStatusDef _bulletHellSlowStatus;
        internal static ChangeAbilitiesCostStatusDef _bulletHellAPCostReductionStatus;

        //  private static ApplyStatusAbilityDef _veiledMarksman;


        public static List<TacticalAbilityDef> Drills = new List<TacticalAbilityDef>();

        public static List<TacticalAbilityDef> GetAvailableDrills(GeoPhoenixFaction faction, GeoCharacter viewer)
        {
            var results = new List<TacticalAbilityDef>();
            if (Drills == null || Drills.Count == 0)
            {
                return results;
            }

            foreach (var ability in Drills)
            {
                if (ability == null)
                {
                    continue;
                }

                if (IsDrillUnlocked(faction, viewer, ability))
                {
                    results.Add(ability);
                }
            }

            return results;
        }

        public static bool IsDrillUnlocked(GeoPhoenixFaction faction, GeoCharacter viewer, TacticalAbilityDef ability)
        {
            if (ability == null)
            {
                return false;
            }

            if (!DrillUnlockConditions.TryGetValue(ability, out var condition) || condition == null)
            {
                return true;
            }

            if (condition.AlwaysAvailable)
            {
                return true;
            }

            if (!MeetsResearchRequirements(faction, condition))
            {
                return false;
            }

            if (!MeetsClassLevelRequirements(faction, viewer, condition))
            {
                return false;
            }

            return true;
        }

        public static void SetUnlockCondition(TacticalAbilityDef ability, DrillUnlockCondition condition)
        {
            if (ability == null)
            {
                return;
            }

            DrillUnlockConditions[ability] = condition ?? DrillUnlockCondition.AlwaysUnlocked();
        }

        private static bool MeetsResearchRequirements(GeoPhoenixFaction faction, DrillUnlockCondition condition)
        {
            if (condition?.RequiredResearchIds == null || condition.RequiredResearchIds.Count == 0)
            {
                return true;
            }

            if (faction?.Research == null)
            {
                return false;
            }

            foreach (var researchId in condition.RequiredResearchIds)
            {
                if (string.IsNullOrEmpty(researchId))
                {
                    // TODO: populate missing research identifiers.
                    continue;
                }

                if (!faction.Research.HasCompleted(researchId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MeetsClassLevelRequirements(GeoPhoenixFaction faction, GeoCharacter viewer, DrillUnlockCondition condition)
        {
            if (condition?.ClassLevelRequirements == null || condition.ClassLevelRequirements.Count == 0)
            {
                return true;
            }

            foreach (var requirement in condition.ClassLevelRequirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                bool satisfied = false;

                if (viewer != null && MeetsSingleClassRequirement(viewer, requirement))
                {
                    satisfied = true;
                }

                if (!satisfied)
                {
                    if (faction?.Soldiers != null)
                    {
                        satisfied = faction.Soldiers.Any(soldier => MeetsSingleClassRequirement(soldier, requirement));
                    }
                }

                if (!satisfied)
                {
                    // TODO: decide whether we should surface unmet requirements to the UI.
                    return false;
                }
            }

            return true;
        }

        private static bool MeetsSingleClassRequirement(GeoCharacter soldier, DrillClassLevelRequirement requirement)
        {
            if (soldier == null || requirement == null)
            {
                return false;
            }

            if (requirement.ClassTag != null && (soldier.ClassTags == null || !soldier.ClassTags.Contains(requirement.ClassTag)))
            {
                return false;
            }

            if (soldier.LevelProgression == null)
            {
                return false;
            }

            return soldier.LevelProgression.Level >= requirement.MinimumLevel;
        }

        private static void EnsureDefaultUnlockConditions()
        {
            if (Drills == null)
            {
                return;
            }

            foreach (var ability in Drills)
            {
                if (ability == null)
                {
                    continue;
                }

                if (!DrillUnlockConditions.ContainsKey(ability))
                {
                    DrillUnlockConditions[ability] = DrillUnlockCondition.AlwaysUnlocked();
                }
            }
        }

        private static void ConfigureUnlockConditions()
        {

            var ordnanceResupply = new DrillUnlockCondition();
            ordnanceResupply.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef"),
                MinimumLevel = 5,
                RequireSelectedOperative = true
            });
            SetUnlockCondition(_ordnanceResupply, ordnanceResupply);


            var toxicLink = new DrillUnlockCondition();
            toxicLink.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef"),
                MinimumLevel = 7,
                RequireSelectedOperative = true
            });
            toxicLink.RequiredResearchIds.Add("SYN_PoisonWeapons_ResearchDef");
            SetUnlockCondition(_toxicLink, toxicLink);


            var causticJamming = new DrillUnlockCondition();
            causticJamming.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef"),
                MinimumLevel = 5,
                RequireSelectedOperative = true
            });
            causticJamming.RequiredResearchIds.Add("SYN_PoisonWeapons_ResearchDef");
            SetUnlockCondition(_causticJamming, causticJamming);

            var mentorUnlock = new DrillUnlockCondition();
            mentorUnlock.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 7,
                RequireSelectedOperative = true
            });
            SetUnlockCondition(_mentorProtocol, mentorUnlock);

            var pintpointToss = new DrillUnlockCondition();
            pintpointToss.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 3,
                RequireSelectedOperative = true
            });
            SetUnlockCondition(_pinpointToss, pintpointToss);

            var oneHandedGrip = new DrillUnlockCondition();
            oneHandedGrip.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 5,
                RequireSelectedOperative = true
            });
            SetUnlockCondition(OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGrip, oneHandedGrip);

            var snapBrace = new DrillUnlockCondition();
            snapBrace.RequiredResearchIds.Add("PX_RiotShield_ResearchDef");
            SetUnlockCondition(_snapBrace, snapBrace);


            var shieldedRiposte = new DrillUnlockCondition();
            shieldedRiposte.RequiredResearchIds.Add("PX_RiotShield_ResearchDef");
            shieldedRiposte.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"),
                MinimumLevel = 2,
                RequireSelectedOperative = true
            });
            SetUnlockCondition(_shieldedRiposte, shieldedRiposte);

        }



        public static void CreateDefs()
        {
            try
            {

                CreateDrills();
                ReplaceStunStatusWithNewConditionalStatusDef();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void CreateDrills()
        {
            try
            {


                _causticJamming = CreateDrillNominalAbility("causticjamming", "8d4e5f60-9192-122b-d4e5-f60718293a4b", "b5c6d7e8-9010-ab1c-2d3e-4f506172839a", "c5d6e7f8-0910-a1b2-c3d4-e5f60718293a"); //done
                _mentorProtocol = CreateDrillNominalAbility("mentorprotocol", "a2b1f9c3-0c32-4d9f-9a7b-0c2d18ce6ab0", "9d2a3f7b-1a53-4a8c-a1ab-4b6d3e2f9a22", "7b1f8e9c-3d4a-4e8c-9b8a-2c5f7a9e0b31"); //done
                _pinpointToss = CreateDrillNominalAbility("pinpointtoss", "b59a3b5a-0b6e-4abf-9c7f-1db713e0b7a0", "c0e37c4a-4b1f-4f3e-8c2a-5f4e6d7c8a91", "e7f1a0b2-6d38-4d1e-9c3b-7a1d9e0f2b64"); //done

                GameTagDef grenadeTag = (GameTagDef)Repo.GetDef("318dd3ff-28f0-1bb4-98bc-39164b7292b6"); // GrenadeItem_TagDef

                _pinpointToss.ItemTagStatModifications = new EquipmentItemTagStatModification[]
                {
                        new EquipmentItemTagStatModification
                        {
                            ItemTag = grenadeTag,
                            EquipmentStatModification = new ItemStatModification
                            {
                                Modification = StatModificationType.Add,
                                TargetStat = StatModificationTarget.Accuracy,
                                Value = 0.5f
                            }
                        }
                };


                CreateDrillNominalAbility("shockdrop", "c1f7c2e4-9a2d-4b8c-ae3e-2c4b5d6e7f81", "f0a1b2c3-4d5e-6f70-8a91-b2c3d4e5f607", "0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9"); //pending



                OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGrip = CreateDrillNominalAbility("onehandedgrip", "16e7f809-2a3b-4c5d-d6e7-8f091a2b3c4d", "901a0b1c-2d3e-4f50-6172-839a4b5c6d7e", "0a0b1c2d-3e4f-5061-7283-9a4b5c6d7e8f"); //done
                OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGripAccPenalty = CreateOneHandedGripPenaltyStatus();

                CreateShockDisciplineStatus();
                _shockDiscipline = CreateDrillNominalAbility("shockdiscipline", "27f8091a-3b4c-5d6e-e7f8-091a2b3c4d5e", "1a0b1c2d-3e4f-5061-7283-9a4b5c6d7e8f", "2b1c2d3e-4f50-6172-839a-4b5c6d7e8f90"); //done

                _snapBrace = CreateDrillNominalAbility("snapbrace", "38091a2b-4c5d-6e7f-f809-1a2b3c4d5e6f", "2c3d4e5f-6172-839a-4b5c-6d7e8f9010ab", "3d4e5f61-7283-9a4b-5c6d-7e8f9010ab1c"); //done
                _shieldedRiposte = CreateDrillNominalAbility("shieldedriposte", "7c3d4e5f-8091-011a-c3d4-e5f60718293a", "9a4b5c6d-7e8f-9010-ab1c-2d3e4f506172", "a94b5c6d-7e8f-9010-ab1c-2d3e4f506173"); //pending fixing animation when deploying shield in different direction                

                _toxicLink = CreateDrillNominalAbility("toxiclink", "9e5f6071-a2a3-233c-e5f6-0718293a4b5c", "c6d7e8f9-1011-b2c3-d4e5-f60718293a4b", "d6e7f809-1112-c3d4-e5f6-0718293a4b5c"); //done
                CreateDrillNominalAbility("pounceprotocol", "af607182-b3b4-344d-f607-18293a4b5c6d", "d7e8f901-1213-c4d5-e6f7-08192a3b4c5d", "e7f80912-1314-d5e6-f7f8-192a3b4c5d6e"); //pending

                _ordnanceResupply = CreateOrdnanceResupplyAbility(); //done
                _viralPuppeteer = CreateDrillNominalAbility("viralpuppeteer", "e39a4b5c-f7f8-7881-0a11-2c3d4e5f6071", "23344556-2021-10f3-0405-8091a2b3c4d5", "33445566-2122-11f4-0506-91a2b3c4d5e6");
                _virulentGrip = CreateDrillNominalAbility("virulentgrip", "f4a5b6c7-0809-8992-1b22-3d4e5f607182", "34455667-2223-12f5-0607-a2b3c4d5e6f7", "45566778-2324-13f6-0708-b3c4d5e6f708");
                _packLoyalty = CreateDrillNominalAbility("packloyalty", "05b6c7d8-191a-9aa3-2c33-4e5f60718293", "45566789-2425-14f7-0809-c4d5e6f70819", "56677889-2526-15f8-0910-d5e6f708192a");




                CreateMightMakesRightAddStatusAbilityDef("mightmakesright", "d2a3b4c5-6e7f-4819-9a0b-1c2d3e4f5a60", "1e2f3a4b-5c6d-7081-92a3-b4c5d6e7f809", "2a3b4c5d-6e7f-8091-a2b3-c4d5e6f70819");

                Drills.Add(_mightMakesRight);
                Drills.Add(_ordnanceResupply);

                _drawfireStatus = CreateDummyStatus("drawfire", "{65B5A8AC-FBB0-42CC-BC2E-EB9DB7460FC8}", "{7557CA9F-DAB8-4AE1-AF1A-853261A4CF05}");
                Drills.Add(
                    CreateApplyStatusAbilityDef("drawfire", "8f7c0a6a-6b63-4b01-9d69-6f7e3d4a4b9a", "f2a5a2d1-0c1f-4c28-8a3a-2f4a0cc2fd3c", "3a0f4d0b-0a8f-4b8f-a8cc-1f4f4f3c3f9d", 2, 0, _drawfireStatus));





                CreateMarkedWatch();
                CreateOverride();
                CreateAksuSprint();
                CreateHeavyconditioning();

                CreatePartingShotAccuracyaMalusStatus();
                CreatePartingShootAbility();
                CreateBulletHellDrill();

                EnsureDefaultUnlockConditions();
                ConfigureUnlockConditions();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static StanceStatusDef CreateOneHandedGripPenaltyStatus()
        {
            try
            {

                //need to replace with StanceStatusDef, from Chiron_StabilityStance_StatusDef, for example
                string statusName = "TFTV_OneHandedGripPenalty_StatusDef";

                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_onehandedgrip.png");

                StanceStatusDef sourceStatus = DefCache.GetDef<StanceStatusDef>("Chiron_StabilityStance_StatusDef");



                StanceStatusDef newStatus = Helper.CreateDefFromClone(
                    sourceStatus,
                    "a4d84ce7-9c9f-4a6e-9190-5cde2b5cfef5",
                    statusName);

                newStatus.ShowNotification = false;
                newStatus.StanceAnimations = null;


                newStatus.Visuals = Helper.CreateDefFromClone(
                    sourceStatus.Visuals,
                    "f7e65c8b-0b82-46ad-83f8-33f4f55b73f4",
                    statusName);

                newStatus.Visuals.DisplayName1.LocalizationKey = "TFTV_DRILL_onehandedgrip_NAME";
                newStatus.Visuals.Description.LocalizationKey = "TFTV_DRILL_onehandedgrip_DESC";
                newStatus.Visuals.SmallIcon = icon;
                newStatus.Visuals.LargeIcon = icon;

                newStatus.StatModifications = new ItemStatModification[0];


                List<GameTagDef> affectedWeaponTags = new List<GameTagDef>
                    {
                    DefCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef"),
                        DefCache.GetDef<GameTagDef>("HeavyItem_TagDef"),
                        DefCache.GetDef<GameTagDef>("PDWItem_TagDef"),
                        DefCache.GetDef<GameTagDef>("SniperRifleItem_TagDef"),
                        DefCache.GetDef<GameTagDef>("ShotgunItem_TagDef"),
                         DefCache.GetDef<GameTagDef>("ViralItem_TagDef"),
                          DefCache.GetDef<GameTagDef>("CrossbowItem_TagDef")

                    };

                List<EquipmentItemTagStatModification> tagModifications = new List<EquipmentItemTagStatModification>();

                foreach (GameTagDef gameTag in affectedWeaponTags)
                {

                    tagModifications.Add(new EquipmentItemTagStatModification
                    {
                        ItemTag = gameTag,
                        EquipmentStatModification = new ItemStatModification
                        {
                            Modification = StatModificationType.Add,
                            TargetStat = StatModificationTarget.Accuracy,
                            Value = -0.25f
                        }
                    });

                }

                newStatus.EquipmentsStatModifications = tagModifications.ToArray();

                return newStatus;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        private static void CreateBulletHellDrill()
        {
            try
            {
                string name = "bullethell";
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                string guidAbility = "{0D3F97D1-5D0D-4B61-958D-53DB4CB4697E}";
                string guidProgression = "{E7C87624-7D12-4F8F-A7D6-AD3C5AE784A6}";
                string guidView = "{7ED1EE35-4B61-4F54-B6FC-1B69FDFBA4B6}";
                string guidAttackBoost = "{9B36B9AC-13F6-4BB6-8E19-C2086AC38C96}";
                string guidSlowStatus = "{0D53AD9F-52A1-4C6B-9C54-EA570074BC5E}";

                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ApplyStatusAbilityDef sourceAbility = DefCache.GetDef<ApplyStatusAbilityDef>("RapidClearance_AbilityDef");

                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                    sourceAbility,
                    guidAbility,
                    name);

                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    sourceAbility.CharacterProgressionData,
                    guidProgression,
                    name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    sourceAbility.ViewElementDef,
                    guidView,
                    name);

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;


                TacStatsModifyStatusDef slowSource = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                _bulletHellSlowStatus = Helper.CreateDefFromClone(
                    slowSource,
                    guidSlowStatus,
                    "BulletHell_SlowStatusDef");
                _bulletHellSlowStatus.EffectName = "BulletHellSlow";
                _bulletHellSlowStatus.SingleInstance = true;
                _bulletHellSlowStatus.DurationTurns = 0;
                _bulletHellSlowStatus.ExpireOnEndOfTurn = true;
                _bulletHellSlowStatus.StatsModifiers = new StatsModifierPopup[]
                {
                    new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Multiply,
                        "Speed",
                        0,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    }
                };

                AddAttackBoostStatusDef source = (AddAttackBoostStatusDef)Repo.GetDef("9385a73f-8d20-4022-acc1-9210e2e29b8f");

                _bulletHellAttackBoostStatus = Helper.CreateDefFromClone(
                    source,
                    guidAttackBoost,
                    "BulletHell_AttackBoostStatusDef");


                _bulletHellAPCostReductionStatus = Helper.CreateDefFromClone(
               (ChangeAbilitiesCostStatusDef)Repo.GetDef("e3062779-8f2f-4407-bc4f-a20f5c2d267b"),
               "{277B6FDB-A88C-452D-9F67-285FA3668AEE}",
               "E_AbilityCostModifier [BulletHell_AbilityDef]");

                _bulletHellAPCostReductionStatus.AbilityCostModification.ActionPointMod = -0.25f;
                _bulletHellAttackBoostStatus.EffectName = "BulletHellAttackBoost";

                _bulletHellAttackBoostStatus.AdditionalStatusesToApply = new TacStatusDef[]
                {
                    _bulletHellAPCostReductionStatus
                };

                newAbility.StatusDef = _bulletHellSlowStatus;

                _bulletHell = newAbility;

                Drills.Add(newAbility);


                TacticalAbilityDef animSource = DefCache.GetDef<TacticalAbilityDef>("RapidClearance_AbilityDef");
                foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                {
                    if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(newAbility))
                    {
                        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(newAbility).ToArray();
                        /* TFTVLogger.Always("Anim Action '" + animActionDef.name + "' set for abilities:");
                         foreach (AbilityDef ad in animActionDef.AbilityDefs)
                         {
                             TFTVLogger.Always("  " + ad.name);
                         }
                         TFTVLogger.Always("----------------------------------------------------", false);*/
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void CreateMightMakesRightAddStatusAbilityDef(string name, string guid0, string guid1, string guid2)
        {
            try
            {
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                var MightMakesRightStatusDef = CreateMightMakesRightStatus();


                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid0, name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid1, name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid2, name);
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.AnimType = -1;
                newAbility.StatusDef = MightMakesRightStatusDef;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { };


                _mightMakesRight = newAbility;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreateAksuSprint()
        {
            try
            {

                //ActorHasItemsEffectConditionDef

                string name = "aksusprintdrill";
                string guid0 = "{DE4F59B3-E04A-42EC-891A-E234A75D7F43}";
                string guid1 = "{AE8A4E57-571E-4916-9E30-81F3968325EC}";
                string guid2 = "05d6e7f8-192a-4b3c-c4d5-6e7f08192a3b";
                string guid3 = "7d8e901a-0b1c-2d3e-4f50-6172839a4b5c";
                string guid4 = "8e901a0b-1c2d-3e4f-5061-72839a4b5c6d";

                TacStatsModifyStatusDef sourceStatus = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");
                TacStatsModifyStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);

                List<TacticalItemDef> armourItems = new List<TacticalItemDef>()
                    {
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_BodyPartDef"),
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_BodyPartDef"),
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_ItemDef"),
                    };

                int requiredCount = 3;

                float bonusSpeed = armourItems.Sum(i => i.BodyPartAspectDef.Speed);

                newStatus.ApplicationConditions = new EffectConditionDef[] { CreateActorHasAtLeastItemsEffectConditionDef(name, guid1, armourItems, requiredCount) };

                newStatus.StatsModifiers = new StatsModifierPopup[]
                {
                    new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Speed",
                        6,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    }
                };

                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid2, name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid3, name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid4, name);
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.AnimType = -1;
                newAbility.StatusDef = newStatus;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { };

                Drills.Add(newAbility);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static ActorHasAtLeastItemsEffectConditionDef CreateActorHasAtLeastItemsEffectConditionDef(string name, string guid, List<TacticalItemDef> items, int requiredCount)
        {
            try
            {
                ActorHasAtLeastItemsEffectConditionDef newStatusApplicationCondition = Helper.CreateDefFromClone<ActorHasAtLeastItemsEffectConditionDef>(null, guid, name);
                newStatusApplicationCondition.Items = items;
                newStatusApplicationCondition.RequiredCount = requiredCount;

                return newStatusApplicationCondition;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }



        private static TacStrengthDamageMultiplierStatusDef CreateMightMakesRightStatus()
        {
            try
            {

                TacStrengthDamageMultiplierStatusDef def = Helper.CreateDefFromClone<TacStrengthDamageMultiplierStatusDef>(null, "{0CF1645E-B231-4A1F-BADB-A6F137790FCD}", "TFTV_TacStrengthDM_StatusDef");
                def.MultiplierType = DamageMultiplierType.Outgoing;
                def.MeleeStandardDamageType = DefCache.GetDef<DamageTypeBaseEffectDef>("Melee_Standard_DamageTypeDef");

                def.DurationTurns = -1;
                def.ExpireOnEndOfTurn = true;
                def.SingleInstance = true;
                def.ShowNotification = false;
                def.VisibleOnPassiveBar = false;
                def.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                def.ApplicationConditions = new EffectConditionDef[] { };
                def.Visuals = Helper.CreateDefFromClone(DefCache.GetDef<ViewElementDef>("E_ViewElement [ApplyStatus_MindControlImmunity_AbilityDef]"), "{A2DE7E41-09E3-4AF2-A49C-7E19A91972F4}", def.name);
                def.EventOnApply = null;
                def.EventOnUnapply = null;


                return def;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static ReloadAbilityDef CreateOrdnanceResupplyAbility()
        {
            try
            {
                string name = "ordnanceresupply";
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ReloadAbilityDef sourceAbility = DefCache.GetDef<ReloadAbilityDef>("ReloadTurret_AbilityDef");
                ReloadAbilityDef newAbility = Helper.CreateDefFromClone(
                    sourceAbility, "{4ED94518-3F90-4C78-961C-A46BBD45B474}", name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    sourceAbility.ViewElementDef, "{A35B328F-BDB9-4061-88A0-109C15F74595}", name);

                newAbility.SceneViewElementDef = Helper.CreateDefFromClone(
                    sourceAbility.SceneViewElementDef, "{D72E4101-58C8-4D79-BE1F-F8C945AD528E}", name);

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.ViewElementDef.LargeIcon = icon;

                newAbility.ViewElementDef.ConfirmationButtonKey.LocalizationKey = "Testing";

                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef").CharacterProgressionData,
                    "{77D0E030-1031-41E3-8863-7B746CEEBEE0}",
                    name);

                newAbility.TargetingDataDef = Helper.CreateDefFromClone(
                    sourceAbility.TargetingDataDef, "{BCFAF995-5FA6-4CF8-BCBD-D193FBA151B6}", name);

                newAbility.TargetingDataDef.Origin.TargetTags.Clear();
                newAbility.TargetingDataDef.Origin.TargetTags.Add(DefCache.GetDef<GameTagDef>("Vehicle_ClassTagDef"));

                // newAbility.TargetingDataDef.Origin.TargetTags.Add(Shared.SharedGameTags.VehicleTag);
                newAbility.TargetingDataDef.Origin.Range = 5;
                newAbility.RequiredCharges = 0;
                newAbility.ActionPointCost = 1f;
                newAbility.TargetingDataDef.Origin.TargetResult = TargetResult.Actor;

                GameTagDef resuppliedVehicleTag = Helper.CreateDefFromClone(DefCache.GetDef<GameTagDef>("Capturable_GameTagDef"), "{F1C2D3E4-5678-49A0-B1C2-D3E4F56789A0}", "ResuppliedVehicle_TagDef");

                newAbility.TargetingDataDef.Origin.CullTargetTags.Add(resuppliedVehicleTag);

                OrdnanceResupplyTag = resuppliedVehicleTag;

                //TFTVLogger.Always($"Created ability {newAbility.name} tags count {newAbility.TargetingDataDef.Origin.TargetTags.Count}, {newAbility.TargetingDataDef.Origin.TargetTags.First()?.name} ");


                TacticalAbilityDef animSource = DefCache.GetDef<TacticalAbilityDef>("ReloadTurret_AbilityDef");
                foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
                {
                    if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(newAbility))
                    {
                        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(newAbility).ToArray();
                        TFTVLogger.Always("Anim Action '" + animActionDef.name + "' set for abilities:");
                        foreach (AbilityDef ad in animActionDef.AbilityDefs)
                        {
                            TFTVLogger.Always("  " + ad.name);
                        }
                        TFTVLogger.Always("----------------------------------------------------", false);
                    }
                }

                return newAbility;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }




        private static void ReplaceStunStatusWithNewConditionalStatusDef()
        {
            try
            {
                string name = "TFTV_ConditionalStunStatus";

                StunStatusDef stunStatusDef = DefCache.GetDef<StunStatusDef>("ActorStunned_StatusDef");

                ConditionalStunStatusDef conditionalStunStatusDef = Helper.CreateDefFromClone<ConditionalStunStatusDef>(null, "{B688BC06-5562-48A3-91BB-1C54FDDB0A2A}", name);
                conditionalStunStatusDef.Visuals = Helper.CreateDefFromClone(stunStatusDef.Visuals, "{02081B04-3C32-4A70-B6E7-2F6AB2F6363D}", name);


                conditionalStunStatusDef.EffectName = "TFTVConditionalStun";
                conditionalStunStatusDef.ApplicationConditions = new EffectConditionDef[0];
                conditionalStunStatusDef.SingleInstance = true;
                conditionalStunStatusDef.ShowNotification = true;
                conditionalStunStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                conditionalStunStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                conditionalStunStatusDef.ResistAbility = _shockDiscipline;
                conditionalStunStatusDef.AlternativeStunDef = _shockDisciplineStatus;
                conditionalStunStatusDef.EffectDef = stunStatusDef.EffectDef;
                conditionalStunStatusDef.ActionPointsReduction = 0.75f;

                DefCache.GetDef<StunDamageEffectDef>("ConditionalStun_StunDamageEffectDef").StunStatusDef = conditionalStunStatusDef;
                DefCache.GetDef<StunDamageKeywordDataDef>("Shock_DamageKeywordDataDef").StatusDef = conditionalStunStatusDef;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateShockDisciplineStatus()
        {
            try


            {
                string name = "shockdiscipline";
                string guid0 = "{ACE8F8F1-B07B-43A4-A807-7700D5981AD4}";
                string guid1 = "{F91586BE-098B-4F5B-8577-F1F835EF4B96}";
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";


                StunStatusDef stunStatusDef = DefCache.GetDef<StunStatusDef>("ActorStunned_StatusDef");
                var light = Helper.CreateDefFromClone<LightStunStatusDef>(null, guid0, "shockdiscipline_light");
                light.Visuals = Helper.CreateDefFromClone(stunStatusDef.Visuals, guid1, name);

                light.Visuals.DisplayName1.LocalizationKey = locKeyName;
                light.Visuals.Description.LocalizationKey = locKeyDesc;

                light.ActionPointsReduction = 0.5f;

                light.EffectName = "ReducedStun";
                light.ApplicationConditions = new EffectConditionDef[0];
                light.SingleInstance = true;
                light.ShowNotification = true;
                light.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                light.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                light.EffectDef = stunStatusDef.EffectDef;


                _shockDisciplineStatus = light;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }






        private static void CreateHeavyconditioning()
        {
            try
            {
                string name = "heavyconditioning";
                string guid0 = "{7BE87D08-08EB-4906-97AD-132045854758}";
                string guid1 = "{5B384A78-B9BC-4618-8619-BAEA9BF8A94F}";
                string guid2 = "{26C6DF5A-5DB1-4197-A9D8-AEB1A6DAB49F}";
                string guid3 = "{15AD6ABA-F572-4BC8-B922-5DE586B5F92A}";
                string guid4 = "{DADA4422-287E-40D4-8D67-2990C61B4DDD}";

                StaticArmorTacStatsStatusDef newStatus = Helper.CreateDefFromClone<StaticArmorTacStatsStatusDef>(null, guid0, name);

                List<TacticalItemDef> armourItems = Repo.GetAllDefs<TacticalItemDef>().Where(i => i.Tags.Contains(Shared.SharedGameTags.ArmorTag)
               && i.Tags.Contains(DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef")) && !i.Tags.Contains(DefCache.GetDef<GameTagDef>("Bionic_TagDef"))).ToList();

                int requiredCount = 3;

                newStatus.ApplicationConditions = new EffectConditionDef[] { CreateActorHasAtLeastItemsEffectConditionDef(name, guid1, armourItems, requiredCount) };

                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid2, name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid3, name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid4, name);
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.AnimType = -1;
                newAbility.StatusDef = newStatus;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { };

                Drills.Add(newAbility);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static ApplyStatusAbilityDef CreateOverride()
        {
            try
            {

                string name = "override";
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                string guid0 = "f2711bfc-b4cb-46dd-bb9f-599a88c1ebff";
                string guid1 = "f7ce1c44-1447-41a3-8112-666c82451e25";
                string guid2 = "0324925f-e318-40b6-ac8c-b68033823cd9";
                string guid3 = "c3d9e8f0-3b4a-4c5d-e6f7-8091a2b3c4d5";
                string guid4 = "d4e5f607-1829-4a5b-6c7d-8e9f0a1b2c3d";
                string guid5 = "{8D86FE5B-8577-4DA6-B5B7-3D969B98C1A5}";





                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef");


                source.TargetingDataDef.Origin.TargetEnemies = true;


                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   guid0,
                    name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    guid1,
                    name);
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                   guid2,
                    name);
                newAbility.TargetingDataDef = Helper.CreateDefFromClone(
                    source.TargetingDataDef,
                    guid3,
                    name);

                newAbility.CharacterProgressionData.RequiredStrength = 0;
                newAbility.CharacterProgressionData.RequiredWill = 0;
                newAbility.CharacterProgressionData.RequiredSpeed = 0;
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName; // displayName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc; // = description;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { source.TargetApplicationConditions[0] };
                newAbility.TargetingDataDef.Origin.TargetEnemies = true;
                newAbility.TargetingDataDef.Origin.TargetFriendlies = false;
                newAbility.AnimType = 1;


                TacActorSimpleAbilityAnimActionDef animActions = DefCache.GetDef<TacActorSimpleAbilityAnimActionDef>("E_ManualControl [Soldier_Utka_AnimActionsDef]");
                animActions.AbilityDefs = animActions.AbilityDefs.AddToArray(newAbility);



                newAbility.StatusDef = Helper.CreateDefFromClone(
                    source.StatusDef,
                    guid4,
                    name);

                MindControlStatusDef newMindControlStatusDef = Helper.CreateDefFromClone(
                    DefCache.GetDef<MindControlStatusDef>("MindControl_StatusDef"),
                    guid5,
                    name
                    );

                newMindControlStatusDef.ControlFactionDef = DefCache.GetDef<PPFactionDef>("Phoenix_FactionDef");

                AddAttackBoostStatusDef addAttackBoostStatusDef = (AddAttackBoostStatusDef)newAbility.StatusDef;
                addAttackBoostStatusDef.AdditionalStatusesToApply = addAttackBoostStatusDef.AdditionalStatusesToApply.AddToArray(newMindControlStatusDef);


                Drills.Add(newAbility);

                return newAbility;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }
        private static DamageMultiplierStatusDef CreateDummyStatus(string name, string guid0, string guid1)
        {
            try
            {
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                DamageMultiplierStatusDef sourceStatus = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(sourceStatus, guid0, name);
                newStatus.Visuals = Helper.CreateDefFromClone(sourceStatus.Visuals, guid1, name);
                newStatus.Visuals.DisplayName1.LocalizationKey = locKeyName;
                newStatus.Visuals.Description.LocalizationKey = locKeyDesc;
                newStatus.Visuals.LargeIcon = icon;
                newStatus.Visuals.SmallIcon = icon;
                newStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                newStatus.DurationTurns = 1;

                return newStatus;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreatePartingShootAbility()
        {
            try
            {

                string name = $"partingshotpistol";
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                string abilityGuid = "490a1b2c-5d6e-7f80-980a-1b2c3d4e5f60";
                string progressionGuid = "e1446dca-3ec4-41e8-acf3-8c88871b024e";
                string viewGuid = "112f0608-435a-4d2e-9146-28d6f075c323";

                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ShootAbilityDef source = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                ShootAbilityDef newAbility = Helper.CreateDefFromClone(source, abilityGuid, name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, progressionGuid, name);
                newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, viewGuid, name);

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;

                newAbility.CharacterProgressionData.RequiredStrength = 0;
                newAbility.CharacterProgressionData.RequiredSpeed = 0;
                newAbility.CharacterProgressionData.RequiredWill = 0;

                newAbility.ActionPointCost = 0f;
                newAbility.WillPointCost = 0f;
                newAbility.EquipmentTags = new GameTagDef[] { DefCache.GetDef<GameTagDef>("HandgunItem_TagDef") };
                newAbility.ActorTags = Array.Empty<GameTagDef>();
                newAbility.ProjectileSpreadMultiplier = 4f / 3f; // -25% accuracy (1 / (4/3) = 0.75)
                newAbility.DisablingStatuses = new StatusDef[]
                {
            DefCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef").StatusDef,
                };

                _partingShot = newAbility;
                Drills.Add(newAbility);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        private static ApplyStatusAbilityDef CreateApplyStatusAbilityDef(string name, string guid0, string guid1, string guid2, int wpCost, int apCost, StatusDef statusDef)
        {
            try
            {
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(source, guid0, name);
                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(source.CharacterProgressionData, guid1, name);

                newAbility.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, guid2, name);
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newAbility.ViewElementDef.LargeIcon = icon;
                newAbility.ViewElementDef.SmallIcon = icon;
                newAbility.AnimType = -1;
                newAbility.StatusDef = statusDef;

                newAbility.WillPointCost = wpCost;
                newAbility.ActionPointCost = apCost;

                return newAbility;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreatePartingShotAccuracyaMalusStatus()
        {
            try
            {
                string name = "partingshot";

                StatMultiplierStatusDef newStatusDef = Helper.CreateDefFromClone(
            (StatMultiplierStatusDef)Repo.GetDef("4a6f7cc4-1bd6-45a5-b572-053963966b07"),
            "{6F7D17D0-477F-4371-BCCA-684E1F42376D}",
            name);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateMarkedWatch()
        {
            try
            {
                string name = "markedwatch";
                string guid0 = "83dbb6f5-6a07-4f66-8b24-88a9c2c3b2e2";
                string guid1 = "a7ce3b3d-3db5-4e52-b83e-1b6c1f1f9b22";
                string guid2 = "c90b0e5a-5e70-4bb8-96d0-8b7e8cc6d7f4";
                string guid3 = "{A986E625-0E3A-4569-AAF6-9DF759ADFF6F}";
                string guid4 = "{5E5D24F1-A230-45D8-9CBA-1C9C1D640468}";


                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";

                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                DamageMultiplierStatusDef newStatus = CreateDummyStatus(name, guid3, guid4);


                ApplyStatusAbilityDef sourceAbility = DefCache.GetDef<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef");


                ApplyStatusAbilityDef newTacticalAbility = Helper.CreateDefFromClone(
                    sourceAbility,
                    guid0,
                    name);

                newTacticalAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    sourceAbility.CharacterProgressionData,
                    guid1,
                    name);

                newTacticalAbility.ViewElementDef = Helper.CreateDefFromClone(
                    sourceAbility.ViewElementDef,
                    guid2,
                    name);


                newTacticalAbility.WillPointCost = 2;
                newTacticalAbility.StatusDef = newStatus;
                newTacticalAbility.TargetApplicationConditions = new EffectConditionDef[] { };
                newTacticalAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newTacticalAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newTacticalAbility.ViewElementDef.LargeIcon = icon;
                newTacticalAbility.ViewElementDef.SmallIcon = icon;



                Drills.Add(newTacticalAbility);
                _markedwatchStatus = newStatus;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static PassiveModifierAbilityDef CreateDrillNominalAbility(string name, string guid0, string guid1, string guid2)
        {
            try
            {
                string locKeyName = $"TFTV_DRILL_{name}_NAME";
                string locKeyDesc = $"TFTV_DRILL_{name}_DESC";
                Sprite icon = Helper.CreateSpriteFromImageFile($"Drill_{name}.png");

                PassiveModifierAbilityDef sourceAbility = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");


                PassiveModifierAbilityDef newTacticalAbility = Helper.CreateDefFromClone(
                    sourceAbility,
                    guid0,
                    name);

                newTacticalAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    sourceAbility.CharacterProgressionData,
                    guid1,
                    name);

                newTacticalAbility.ViewElementDef = Helper.CreateDefFromClone(
                    sourceAbility.ViewElementDef,
                    guid2,
                    name);


                newTacticalAbility.StatModifications = new ItemStatModification[0];
                newTacticalAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                newTacticalAbility.ViewElementDef.DisplayName1.LocalizationKey = locKeyName;
                newTacticalAbility.ViewElementDef.Description.LocalizationKey = locKeyDesc;
                newTacticalAbility.ViewElementDef.LargeIcon = icon;
                newTacticalAbility.ViewElementDef.SmallIcon = icon;



                Drills.Add(newTacticalAbility);

                return newTacticalAbility;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


    }


}

