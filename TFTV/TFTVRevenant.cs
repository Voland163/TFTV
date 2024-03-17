using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.ParticleSystems;
using Base.UI;
using Epic.OnlineServices;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;


namespace TFTV
{
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static Dictionary<int, int> DeadSoldiersDelirium = new Dictionary<int, int>();
        public static Dictionary<int, int> RevenantsKilled = new Dictionary<int, int>();


        public static int daysRevenantLastSeen = 0;
        // public static  timeLastRevenantSpawned = new TimeUnit();
        public static bool revenantCanSpawn = false;
        public static bool revenantSpawned = false;
        public static List<string> revenantSpecialResistance = new List<string>();
        public static int revenantID = 0;
        public static string revenantResistanceHintGUID;

        //  private static bool revenantPresent = false;

        public static GameTagDef RevenantTier1GameTag;// = DefCache.GetDef<GameTagDef>("RevenantTier_1_GameTagDef");
        public static GameTagDef RevenantTier2GameTag;// = DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef");
        public static GameTagDef RevenantTier3GameTag;// = DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef");
        public static GameTagDef AnyRevenantGameTag; //= DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");
        public static GameTagDef RevenantResistanceGameTag;// = DefCache.GetDef<GameTagDef>("RevenantResistance_GameTagDef");

        private static PassiveModifierAbilityDef _revenantAssault;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantAssault_AbilityDef");
        private static PassiveModifierAbilityDef _revenantBerserker;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantBerserker_AbilityDef");
        private static PassiveModifierAbilityDef _revenantInfiltrator;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantInfiltrator_AbilityDef");
        private static PassiveModifierAbilityDef _revenantTechnician; //= DefCache.GetDef<PassiveModifierAbilityDef>("RevenantTechnician_AbilityDef");
        private static PassiveModifierAbilityDef _revenantHeavy;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantHeavy_AbilityDef");
        private static PassiveModifierAbilityDef _revenantPriest;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantPriest_AbilityDef");
        private static PassiveModifierAbilityDef _revenantSniper;// = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantSniper_AbilityDef");

        private static readonly SpecializationDef assaultSpecialization = DefCache.GetDef<SpecializationDef>("AssaultSpecializationDef");
        private static readonly SpecializationDef berserkerSpecialization = DefCache.GetDef<SpecializationDef>("BerserkerSpecializationDef");
        private static readonly SpecializationDef heavySpecialization = DefCache.GetDef<SpecializationDef>("HeavySpecializationDef");
        private static readonly SpecializationDef infiltratorSpecialization = DefCache.GetDef<SpecializationDef>("InfiltratorSpecializationDef");
        private static readonly SpecializationDef priestSpecialization = DefCache.GetDef<SpecializationDef>("PriestSpecializationDef");
        private static readonly SpecializationDef sniperSpecialization = DefCache.GetDef<SpecializationDef>("SniperSpecializationDef");
        private static readonly SpecializationDef technicianSpecialization = DefCache.GetDef<SpecializationDef>("TechnicianSpecializationDef");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
        private static readonly ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
        private static readonly ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
        private static readonly ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
        private static readonly ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");

        private static AddAbilityStatusDef _revenantStatusAbility; //= DefCache.GetDef<AddAbilityStatusDef>("Revenant_StatusEffectDef");
        private static PassiveModifierAbilityDef _revenantAbility; //= DefCache.GetDef<PassiveModifierAbilityDef>("Revenant_AbilityDef");
        private static DamageMultiplierStatusDef _revenantResistanceStatus; // = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantResistance_StatusDef");
        //private static readonly DamageMultiplierAbilityDef revenantResistanceAbility = DefCache.GetDef<DamageMultiplierAbilityDef>("RevenantResistance_AbilityDef");

        private static readonly DamageOverTimeDamageTypeEffectDef virusDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef");
        private static readonly DamageOverTimeDamageTypeEffectDef acidDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");
       // private static readonly AttenuatingDamageTypeEffectDef paralysisDamageWeaponDescription = DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Electroshock_AttenuatingDamageTypeEffectDef");
        private static readonly DamageOverTimeDamageTypeEffectDef paralysisDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
        // private static readonly DamageOverTimeDamageTypeEffectDef poisonDamage =DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Poison_DamageOverTimeDamageTypeEffectDef"));
        private static readonly StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef projectileDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef shredDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Shred_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");
        // private static readonly StandardDamageTypeEffectDef bashDamage =DefCache.GetDef<StandardDamageTypeEffectDef>("Bash_StandardDamageTypeEffectDef"));

        //private static readonly DamageKeywordDef paralisingDamageKeywordDef =DefCache.GetDef<DamageKeywordDef>("Paralysing_DamageKeywordDataDef"));

        internal class RevenantDefs 
        {
            private static Sprite VoidIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");

            public static void CreateRevenantDefs()
            {
                try
                {
                    CreateRevenantAbility();
                    CreateRevenantStatusEffect();
                    CreateRevenantGameTags();
                    CreateRevenantAbilityForAssault();
                    CreateRevenantAbilityForBerserker();
                    CreateRevenantAbilityForHeavy();
                    CreateRevenantAbilityForInfiltrator();
                    CreateRevenantAbilityForPriest();
                    CreateRevenantAbilityForSniper();
                    CreateRevenantAbilityForTechnician();
                    CreateRevenantResistanceStatus();
                    //  CreateRevenantClassStatusEffects();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }




            public static void CreateRevenantStatusEffect()
            {
                try
                {


                    AddAbilityStatusDef sourceAbilityStatusDef = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
                    PassiveModifierAbilityDef Revenant_Ability = DefCache.GetDef<PassiveModifierAbilityDef>("Revenant_AbilityDef");

                    AddAbilityStatusDef newAbilityStatusDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "68EE5958-D977-4BD4-9018-CAE03C5A6579", "Revenant_StatusEffectDef");
                    newAbilityStatusDef.AbilityDef = Revenant_Ability;
                    newAbilityStatusDef.ApplicationConditions = new EffectConditionDef[] { };
                    _revenantStatusAbility = newAbilityStatusDef;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbility()
            {
                try
                {

                    string skillName = "Revenant_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef revenantAbility = Helper.CreateDefFromClone(
                        source,
                        "8A62302E-9C2D-4AFA-AFF3-2F526BF82252",
                        skillName);
                    revenantAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "FECD4DD8-5E1A-4A0F-BC3A-C2F0AA30E41F",
                        skillName);
                    revenantAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "75B1017A-0455-4B44-91F0-3E1446899B42",
                        skillName);
                    revenantAbility.StatModifications = new ItemStatModification[0];
                    revenantAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    revenantAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_ABILITY_REVENANT";
                    revenantAbility.ViewElementDef.Description.LocalizationKey = "KEY_ABILITY_REVENANT_DESCRIPTION";
                    revenantAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ODI_Skull.png");
                    revenantAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ODI_Skull.png");
                    _revenantAbility = revenantAbility;
                    
                    // revenantAbility.ViewElementDef.ShowInStatusScreen = false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForAssault()
            {
                try
                {

                    string skillName = "RevenantAssault_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef revenantAssault = Helper.CreateDefFromClone(
                        source,
                        "1045EB8D-1916-428F-92EF-A15FD2807818",
                        skillName);
                    revenantAssault.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "7FF5A3CF-6BBD-4E4F-9E80-2DB7BDB29112",
                        skillName);
                    revenantAssault.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "47BE3577-1D68-4FB2-BFA3-0A158FC710D9",
                        skillName);
                    revenantAssault.StatModifications = new ItemStatModification[]
                    {
                    new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.05f},
                    new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 2},
                    new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 2},
                    };
                    revenantAssault.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    revenantAssault.ViewElementDef.DisplayName1 = new LocalizedTextBind("Assault Revenant", true);
                    revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+5% Damage", true);

                    revenantAssault.ViewElementDef.LargeIcon = VoidIcon;
                    revenantAssault.ViewElementDef.SmallIcon = VoidIcon;
                    revenantAssault.ViewElementDef.ShowInStatusScreen = false;
                    _revenantAssault = revenantAssault;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForBerserker()
            {
                try
                {

                    string skillName = "RevenantBerserker_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef revenantBerserker = Helper.CreateDefFromClone(
                        source,
                        "FD3FE516-25BA-44F2-9770-3AA4AD1DCB91",
                        skillName);
                    revenantBerserker.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "E2707CBD-3D99-4EA4-A48D-B8E6E14EFDFD",
                        skillName);
                    revenantBerserker.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "3F74FAF1-1A87-4E2A-AEC2-CBB0BA5A14E0",
                        skillName);
                    revenantBerserker.StatModifications = new ItemStatModification[]
                    {
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                    };
                    revenantBerserker.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    revenantBerserker.ViewElementDef.DisplayName1 = new LocalizedTextBind("Berserker Revenant", true);
                    revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+4 Speed", true);

                    revenantBerserker.ViewElementDef.LargeIcon = VoidIcon;
                    revenantBerserker.ViewElementDef.SmallIcon = VoidIcon;
                    revenantBerserker.ViewElementDef.ShowInStatusScreen = false;
                    _revenantBerserker = revenantBerserker;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForHeavy()
            {
                try
                {

                    string skillName = "RevenantHeavy_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef heavy = Helper.CreateDefFromClone(
                        source,
                        "A8603522-3472-4A95-9ADF-F27E8B287D15",
                        skillName);
                    heavy.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "AA5F572B-D86B-4C00-B8B9-4D86EE5F7F4D",
                        skillName);
                    heavy.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "F8781E78-D106-44B3-A0E6-855BCAEB0A2F",
                        skillName);
                    heavy.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 100},
                    };
                    heavy.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    heavy.ViewElementDef.DisplayName1 = new LocalizedTextBind("Heavy Revenant", true);
                    heavy.ViewElementDef.Description = new LocalizedTextBind("+5 Strength", true);

                    heavy.ViewElementDef.LargeIcon = VoidIcon;
                    heavy.ViewElementDef.SmallIcon = VoidIcon;
                    heavy.ViewElementDef.ShowInStatusScreen = false;
                    _revenantHeavy = heavy;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForInfiltrator()
            {
                try
                {

                    string skillName = "RevenantInfiltrator_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef infiltrator = Helper.CreateDefFromClone(
                        source,
                        "6C56E0F9-56BB-41D2-AFB1-08C8A49F69FA",
                        skillName);
                    infiltrator.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "1F8B6D09-A2C5-4B3F-BBED-F59675301ABB",
                        skillName);
                    infiltrator.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "6CAFD922-60C6-449E-A652-C2BD94386BE5",
                        skillName);
                    infiltrator.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.15f},
                    };
                    infiltrator.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    infiltrator.ViewElementDef.DisplayName1 = new LocalizedTextBind("Infiltrator Revenant", true);
                    infiltrator.ViewElementDef.Description = new LocalizedTextBind("+15% Stealth", true);

                    infiltrator.ViewElementDef.LargeIcon = VoidIcon;
                    infiltrator.ViewElementDef.SmallIcon = VoidIcon;
                    _revenantInfiltrator = infiltrator;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForPriest()
            {
                try
                {

                    string skillName = "RevenantPriest_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef priest = Helper.CreateDefFromClone(
                        source,
                        "0816E671-D396-4212-910F-87B5DEC6ADE2",
                        skillName);
                    priest.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "C1C7FBEA-2C0B-4930-A73C-15BF3A987784",
                        skillName);
                    priest.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "460AAE12-0541-40AB-A4EE-E3E206A96FB4",
                        skillName);
                    priest.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10},
                    };
                    priest.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    priest.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                    priest.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);

                    priest.ViewElementDef.LargeIcon = VoidIcon;
                    priest.ViewElementDef.SmallIcon = VoidIcon;
                    priest.ViewElementDef.ShowInStatusScreen = false;
                    _revenantPriest = priest;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForSniper()
            {
                try
                {

                    string skillName = "RevenantSniper_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef sniper = Helper.CreateDefFromClone(
                        source,
                        "4A2C53A3-D9DB-456A-8B88-AB2D90BE1DB5",
                        skillName);
                    sniper.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "0D811905-8C70-4D46-9CF2-1A31C5E98ED1",
                        skillName);
                    sniper.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "7DCCCAAA-7245-4245-9033-F6320CCDA2AB",
                        skillName);
                    sniper.ViewElementDef.ShowInStatusScreen = false;
                    sniper.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 10},
                    };
                    sniper.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    sniper.ViewElementDef.DisplayName1 = new LocalizedTextBind("Sniper Revenant", true);
                    sniper.ViewElementDef.Description = new LocalizedTextBind("+10 Perception", true);

                    sniper.ViewElementDef.LargeIcon = VoidIcon;
                    sniper.ViewElementDef.SmallIcon = VoidIcon;
                    _revenantSniper = sniper;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantAbilityForTechnician()
            {
                try
                {

                    string skillName = "RevenantTechnician_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef technician = Helper.CreateDefFromClone(
                        source,
                        "04A284AC-545A-455F-8843-54056D68022E",
                        skillName);
                    technician.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "1A995634-EE80-4E72-A10F-F8389E8AEB50",
                        skillName);
                    technician.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "19B35512-5C23-4046-B10D-2052CDEFB769",
                        skillName);
                    technician.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 5},
                 new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 5}
                    };
                    technician.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    technician.ViewElementDef.DisplayName1 = new LocalizedTextBind("Technician Revenant", true);
                    technician.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +5 Willpower", true);

                    technician.ViewElementDef.LargeIcon = VoidIcon;
                    technician.ViewElementDef.SmallIcon = VoidIcon;
                    technician.ViewElementDef.ShowInStatusScreen = false;
                    _revenantTechnician = technician;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantResistanceStatus()
            {
                try
                {
                    string skillName = "RevenantResistance_StatusDef";
                    DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                    DamageMultiplierStatusDef revenantResistance = Helper.CreateDefFromClone(
                        source,
                        "A7F8113B-B281-4ECD-99FE-3125FCE029C4",
                        skillName);
                    revenantResistance.EffectName = "RevenantResistance";
                    revenantResistance.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                    revenantResistance.VisibleOnPassiveBar = true;
                    revenantResistance.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;

                    //  revenantResistance.CharacterProgressionData = Helper.CreateDefFromClone(
                    //      source.CharacterProgressionData,
                    //      "C298F900-A7D5-4EEC-96E1-50D017614396",
                    //     skillName);
                    revenantResistance.Visuals = Helper.CreateDefFromClone(
                        source.Visuals,
                        "B737C223-52D0-413B-B48F-978AD5D5BB33",
                        skillName);
                    revenantResistance.DamageTypeDefs = new DamageTypeBaseEffectDef[1];

                    revenantResistance.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_RevenantResistance.png");
                    revenantResistance.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_RevenantResistance.png");
                    _revenantResistanceStatus = revenantResistance;
                   
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CreateRevenantGameTags()
            {
                string skillName = "RevenantTier";
                GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
               RevenantTier1GameTag =  Helper.CreateDefFromClone(
                    source,
                    "1677F9F4-5B45-47FA-A119-83A76EF0EC70",
                    skillName + "_1_" + "GameTagDef");
                RevenantTier2GameTag = Helper.CreateDefFromClone(
                    source,
                    "9A807A62-D51D-404E-ADCF-ABB4A888202E",
                    skillName + "_2_" + "GameTagDef");
                RevenantTier3GameTag = Helper.CreateDefFromClone(
                    source,
                    "B4BD3091-8522-4F3C-8A0F-9EE522E0E6B4",
                    skillName + "_3_" + "GameTagDef");
                AnyRevenantGameTag = Helper.CreateDefFromClone(
                       source,
                       "D2904A22-FE23-45B3-8879-9236E389C9E4",
                       "Any_Revenant_TagDef");
                string tagName = "RevenantResistance";
               RevenantResistanceGameTag = Helper.CreateDefFromClone(
                    source,
                    "D424B077-6731-40AD-BFA8-7020BD3A9F9A",
                    tagName + "_GameTagDef");
            }






        }

        internal class PrespawnChecks
        {
            public static void CheckForNotDeadSoldiers(TacticalLevelController level)
            {
                try
                {
                    List<int> soldiersReallyDead = new List<int>();
                    List<int> soldiersNotReallyDead = new List<int>();
                    SoldierStats soldierStats = new SoldierStats();

                    TFTVLogger.Always("Soldiers in the DeadSoldiersDelirium are " + DeadSoldiersDelirium.Count());

                    foreach (GeoTacUnitId reallyDeadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                    {
                        TFTVLogger.Always("This one is really dead: " + reallyDeadSoldier);
                        soldiersReallyDead.Add(reallyDeadSoldier);

                    }

                    foreach (int id in DeadSoldiersDelirium.Keys)
                    {
                        if (!soldiersReallyDead.Contains(id))
                        {
                            TFTVLogger.Always(id + " ? This one ain't dead!");
                            soldiersNotReallyDead.Add(id);
                        }
                    }

                    foreach (int id in soldiersNotReallyDead)
                    {
                        DeadSoldiersDelirium.Remove(id);
                    }

                    TFTVLogger.Always("Soldiers in the DeadSoldiersDelirium after cleanup are " + DeadSoldiersDelirium.Count());
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void CheckRevenantTime(GeoLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("DeadSoldierDeliriumCount is " + DeadSoldiersDelirium.Count + " and last time a Revenant was seen was on day " + daysRevenantLastSeen + ", and now it is day " + controller.Timing.Now.TimeSpan.Days);
                    if (DeadSoldiersDelirium.Count > 0 && (daysRevenantLastSeen == 0 || controller.Timing.Now.TimeSpan.Days - daysRevenantLastSeen >= 3)) //UnityEngine.Random.Range(-1, 3))) 
                    {
                        revenantCanSpawn = true;
                        TFTVLogger.Always("Therefore, a Revenant can spawn is " + revenantCanSpawn);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class StatsAndClasses 
        {

            public static void SetRevenantClassAbility(GeoTacUnitId deadSoldier, TacticalLevelController level, TacticalActor tacticalActor)
            {
                try
                {

                    SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;

                    List<SpecializationDef> specializations = deadSoldierStats.ClassesSpecialized.ToList();

                    foreach (SpecializationDef specialization in specializations)
                    {
                        AddRevenantClassAbility(tacticalActor, specialization);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void IncreaseRevenantHP(TacticalActor tacticalActor)
            {
                try
                {


                    int currentEndurance = (int)(tacticalActor.CharacterStats.Endurance);
                    int newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.15f);
                    if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                    {

                        newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.2f);

                    }
                    else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                    {
                        newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.25f);

                    }
                    tacticalActor.CharacterStats.Endurance.SetMax(newEndurance);
                    tacticalActor.CharacterStats.Endurance.SetToMax();
                    tacticalActor.UpdateStats();
                    tacticalActor.CharacterStats.Health.SetToMax();
                    tacticalActor.UpdateStats();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }


            }
            public static void AddRevenantClassAbility(TacticalActor tacticalActor, SpecializationDef specialization)
            {
                try
                {

                    if (specialization == assaultSpecialization)
                    {
                        TFTVLogger.Always("Deceased had Assault specialization");
                        // tacticalActor.Status.ApplyStatus(RevenantAssaultStatus);

                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {
                            _revenantAssault.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                            };
                            // revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+10% Damage", true);
                        }
                        else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                        {

                            _revenantAssault.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.15f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 6},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 6},
                            };
                            //  revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+15% Damage", true);
                        }
                        tacticalActor.AddAbility(_revenantAssault, tacticalActor);


                    }
                    else if (specialization == berserkerSpecialization)
                    {
                        TFTVLogger.Always("Deceased had Berserker specialization");
                        // tacticalActor.Status.ApplyStatus(RevenantBerserkerStatus);


                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {

                            _revenantBerserker.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 6},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 6},
                            };

                            // revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+6 Speed", true);
                        }
                        else if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef")))
                        {

                            _revenantBerserker.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 10},
                            };

                            //  revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+10 Speed", true);
                        }
                        tacticalActor.AddAbility(_revenantBerserker, tacticalActor);
                    }
                    else if (specialization == heavySpecialization)
                    {
                        TFTVLogger.Always("Deceased had Heavy specialization");
                        //    tacticalActor.Status.ApplyStatus(RevenantHeavyStatus);

                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {

                            _revenantHeavy.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 150}
                            };

                            // revenantHeavy.ViewElementDef.Description = new LocalizedTextBind("+10 Strength", true);
                        }
                        else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                        {

                            _revenantHeavy.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 200}
                            };

                            //  revenantHeavy.ViewElementDef.Description = new LocalizedTextBind("+15 Strength", true);
                        }
                        tacticalActor.AddAbility(_revenantHeavy, tacticalActor);
                    }
                    else if (specialization == infiltratorSpecialization)
                    {
                        TFTVLogger.Always("Deceased had Infiltrator specialization");
                        //   tacticalActor.Status.ApplyStatus(RevenantInfiltratorStatus);


                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {

                            _revenantInfiltrator.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.3f},
                            };

                            //  revenantInfiltrator.ViewElementDef.Description = new LocalizedTextBind("+30% Stealth", true);
                        }
                        else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                        {

                            _revenantInfiltrator.StatModifications = new ItemStatModification[]
                            {
                          // new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 30},
                           new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.5f},
                            };

                            //   revenantInfiltrator.ViewElementDef.Description = new LocalizedTextBind("+50% Stealth", true);
                        }
                        tacticalActor.AddAbility(_revenantInfiltrator, tacticalActor);
                    }
                    else if (specialization == priestSpecialization)
                    {
                        TFTVLogger.Always("Deceased had Priest specialization");
                        //     tacticalActor.Status.ApplyStatus(RevenantPriestStatus);

                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {
                            _revenantPriest.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 20},
                            };

                            // revenantPriest.ViewElementDef.Description = new LocalizedTextBind("+20 Willpower", true);
                        }
                        else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                        {

                            _revenantPriest.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 30},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 30},
                            };

                            //  revenantPriest.ViewElementDef.Description = new LocalizedTextBind("+30 Willpower", true);
                        }
                        tacticalActor.AddAbility(_revenantPriest, tacticalActor);
                    }

                    else if (specialization == sniperSpecialization)
                    {

                        TFTVLogger.Always("Deceased had Sniper specialization");
                        //    tacticalActor.Status.ApplyStatus(RevenantSniperStatus);



                        if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef")))
                        {
                            _revenantSniper.StatModifications = new ItemStatModification[]
                            {
                           // new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 0.15f},
                            };


                        }
                        else if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef")))
                        {

                            _revenantSniper.StatModifications = new ItemStatModification[]
                            {
                         //   new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 20 },
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 0.2f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.Add, Value = 0.2f},
                                //  new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.AddMax, Value = 20},
                            };


                            //      RevenantSniperStatus.Visuals.Description = new LocalizedTextBind("Nemesis, +20 Perception, +20% Accuracy, +20% HP", true);

                        }

                        tacticalActor.AddAbility(_revenantSniper, tacticalActor);
                    }
                    else if (specialization == technicianSpecialization)
                    {
                        TFTVLogger.Always("Deceased had Technician specialization");
                        //   tacticalActor.Status.ApplyStatus(RevenantTechnician);
                        tacticalActor.AddAbility(_revenantTechnician, tacticalActor);


                        if (tacticalActor.GameTags.Contains(RevenantTier2GameTag))
                        {

                            _revenantTechnician.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                            };

                            _revenantTechnician.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +10 Willpower", true);
                        }
                        else if (tacticalActor.GameTags.Contains(RevenantTier3GameTag))
                        {

                            _revenantTechnician.StatModifications = new ItemStatModification[]
                            {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                            };

                            _revenantTechnician.ViewElementDef.Description = new LocalizedTextBind("+10 Strength, +10 Willpower", true);
                        }
                    }

                    IncreaseRevenantHP(tacticalActor);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class Resistance
        {
            public static void CheckIfRevenantPresent(TacticalLevelController controller)
            {
                try
                {
                    if (controller.IsFromSaveGame && revenantID != 0)
                    {
                        TFTVLogger.Always("Game is loading and Revenant is present; adjusting revenant resistance check");
                        AdjustRevenantResistanceStatusDescription();

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void ApplyRevenantSpecialResistance(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    if (data.Target.GetActor() != null && _revenantResistanceStatus.DamageTypeDefs[0] == null
                        && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(_revenantResistanceStatus))
                    {
                        float multiplier = 0.5f;

                        if (!revenantSpecialResistance.Contains(data.Target.GetActor().name))
                        {
                            //  TFTVLogger.Always("This check was passed");
                            data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                            data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                            if (!data.Target.IsBodyPart())
                            {
                                revenantSpecialResistance.Add(data.Target.GetActor().name);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            // Adopted from MadSkunky BetterClasses. Harmony Patch to calculate shred resistance, vanilla has no implementation for this
            [HarmonyPatch(typeof(ShreddingDamageKeywordData), "ProcessKeywordDataInternal")]
            internal static class BC_ShreddingDamageKeywordData_ProcessKeywordDataInternal_ShredResistant_patch
            {

                public static void Postfix(ref DamageAccumulation.TargetData data)
                {

                    if (data.Target.GetActor() != null && data.Target.GetActor().Status != null)
                    {
                        TacticalActorBase actor = data.Target.GetActor();

                        if (actor.Status.HasStatus(_revenantResistanceStatus) && _revenantResistanceStatus.DamageTypeDefs[0] == shredDamage)
                        {
                            data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * _revenantResistanceStatus.Multiplier);
                        }

                        DamageMultiplierAbilityDef shredResistanceAbilityDef = DefCache.GetDef<DamageMultiplierAbilityDef>("ShredResistant_DamageMultiplierAbilityDef");

                        if (actor.GetAbilityWithDef<DamageMultiplierAbility>(shredResistanceAbilityDef) != null)
                        {
                            data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * shredResistanceAbilityDef.Multiplier);
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(TacticalActorBase), "ApplyDamageInternal")]
            internal static class TacticalActorBase_ApplyDamage_DamageResistant_patch
            {
                public static void Postfix(TacticalActorBase __instance)
                {
                    try
                    {
                        //  TFTVLogger.Always("Actor who has received damage is " + __instance.name);

                        if (_revenantResistanceStatus != null
                            && _revenantResistanceStatus.DamageTypeDefs != null
                            && _revenantResistanceStatus.DamageTypeDefs.Count() > 0
                            && _revenantResistanceStatus.DamageTypeDefs[0] == null
                            && __instance.Status != null
                            && __instance.Status.HasStatus(_revenantResistanceStatus)
                            && revenantSpecialResistance != null
                            && !revenantSpecialResistance.Contains(__instance.name))
                        {
                            //  TFTVLogger.Always(__instance.name + " has the Revenant Resistance ability and it's the first time it is triggered");
                            revenantSpecialResistance.Add(__instance.name);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            public static DamageTypeBaseEffectDef GetPreferredDamageType(TacticalLevelController controller)
            {
                try
                {
                    List<SoldierStats> allSoldierStats = controller.TacticalGameParams.Statistics.LivingSoldiers.Values.ToList();
                    TFTVLogger.Always("AllSoldierStats count is " + allSoldierStats.Count());
                    allSoldierStats.AddRange(controller.TacticalGameParams.Statistics.DeadSoldiers.Values.ToList());
                    TFTVLogger.Always("AllSoldierStats, including dead soldiers, count is " + allSoldierStats.Count());
                    List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                    float scoreFireDamage = 0;
                    float scoreParalysisDamage = 0;
                    float scoreVirusDamage = 0;
                    //  int scoreShredDamage = 0;
                    float scoreBlastDamage = 0;
                    float scoreAcidDamage = 0;
                    float scoreHighDamage = 0;
                    float scoreBurstDamage = 0;
                    //    int scoreBashDamage = 0;

                    foreach (SoldierStats stat in allSoldierStats)
                    {
                        if (stat.ItemsUsed.Count > 0)
                        {
                            usedWeapons.AddRange(stat.ItemsUsed);
                        }
                    }

                    TFTVLogger.Always("Number of weapons or other items used " + usedWeapons.Count());

                    if (usedWeapons.Count() > 0)
                    {
                        // TFTVLogger.Always("Checking use of each weapon... ");
                        foreach (UsedWeaponStat stat in usedWeapons)
                        {
                            WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                            //   TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());

                            if (weaponDef != null)
                            {
                                //   TFTVLogger.Always($"weapon is {weaponDef.name}");

                                foreach (DamageKeywordPair damageKeywordPair in weaponDef.DamagePayload.DamageKeywords)
                                {
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword)
                                    {
                                        scoreFireDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BlastKeyword)
                                    {
                                        scoreBlastDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                                    {
                                        scoreAcidDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.ParalysingKeyword)
                                    {
                                        scoreParalysisDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.ViralKeyword)
                                    {
                                        scoreVirusDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.Value >= 70 && damageKeywordPair.DamageKeywordDef != Shared.SharedDamageKeywords.ShockKeyword)
                                    {
                                        // TFTVLogger.Always($"{weaponDef.name} is counted as high damage weapon");
                                        scoreHighDamage += stat.UsedCount;
                                    }
                                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.DamageKeyword && (weaponDef.DamagePayload.ProjectilesPerShot >= 2 || weaponDef.DamagePayload.AutoFireShotCount >= 3))
                                    {
                                        scoreBurstDamage += stat.UsedCount;
                                    }
                                }
                            }
                        }
                        TFTVLogger.Always("Number of fire weapons used " + scoreFireDamage);
                        TFTVLogger.Always("Number of blast weapons used " + scoreBlastDamage);
                        TFTVLogger.Always("Number of acid weapons used " + scoreAcidDamage);
                        TFTVLogger.Always("Number of virus weapons used " + scoreVirusDamage);
                        TFTVLogger.Always("Number of paralysis weapons used " + scoreParalysisDamage);

                        //  TFTVLogger.Always("Number of shred weapons used " + scoreShredDamage);
                        TFTVLogger.Always("Number of high damage per hit weapons used " + scoreHighDamage);
                        TFTVLogger.Always("Number of burst weapons used " + scoreBurstDamage);

                        //Scores have to be adjusted

                        //fireWeapons 2 modified by AP cost: 4
                        //acidWeapons 4 modified by AP cost: 3 + 2 + 2 + 1 = 8
                        //paralyzingWeapons 3 modified by AP cost::  3 + 3 + 1 = 7
                        //virusWeapons 3 modified by AP cost: 2 + 2 + 1 = 5
                        //blastWeapons 7 + 1 Arachni + 6 grenades 14 total  = 14 + 9 + 3 = 26
                        //highDamageWeapons Slamstrike + 8 HWs + 9 Sniper Rifles + 6 Melee weapons 23 total
                        // modified by AP cost: 2 + 8 + 9 + 12 = 31
                        //highBurstWeapons 12 ARs + SGs + 6 HWs + 1 Sanctifier + 1 Tormentor + 1 Redeemer + 3 PDWs 24 total
                        //modified by AP cost: 24 + 6 + 3 + 3 + 2 + 9 = 47  

                        scoreAcidDamage *= 47f / 4f;
                        scoreVirusDamage *= 47f / 5f;
                        scoreParalysisDamage *= 47f / 7f;
                        scoreFireDamage *= 47f / 4f;
                        scoreBlastDamage *= 47f / 26f;
                        scoreHighDamage *= 47f / 31f;

                        TFTVLogger.Always("Number of acid damage weapons used after adjustment  " + scoreAcidDamage);
                        TFTVLogger.Always("Number of virus damage weapons used after adjustment  " + scoreVirusDamage);
                        TFTVLogger.Always("Number of paralysis damage weapons used after adjustment  " + scoreParalysisDamage);
                        TFTVLogger.Always("Number of fire damage weapons used after adjustment  " + scoreFireDamage);
                        TFTVLogger.Always("Number of high damage weapons used after adjustment  " + scoreHighDamage);
                        TFTVLogger.Always("Number of blast damage weapons used after adjustment  " + scoreBlastDamage);
                        TFTVLogger.Always("Number of burst weapons used (no adjustment) " + scoreBurstDamage);


                        if (scoreAcidDamage > 0 || scoreFireDamage > 0 || scoreBlastDamage > 0 || scoreHighDamage > 0 || scoreBurstDamage > 0
                            || scoreVirusDamage > 0 || scoreParalysisDamage > 0)
                        {
                            List<float> scoreList = new List<float> { scoreFireDamage, scoreAcidDamage, scoreBlastDamage, scoreBurstDamage, scoreHighDamage, scoreParalysisDamage, scoreVirusDamage };
                            scoreList = scoreList.OrderByDescending(x => x).ToList();
                            int options = 0;

                            if (scoreList.Count > 2)
                            {
                                options = 3;
                            }
                            else
                            {
                                options = scoreList.Count;
                            }

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(0, options);

                            float winner = scoreList[roll];
                            TFTVLogger.Always("The roll is " + roll + ", so the highest score is " + winner);

                            DamageTypeBaseEffectDef damageTypeDef = new DamageTypeBaseEffectDef();
                            if (winner == scoreFireDamage)
                            {
                                damageTypeDef = fireDamage;
                            }
                            if (winner == scoreAcidDamage)
                            {
                                damageTypeDef = acidDamage;
                            }
                            if (winner == scoreBlastDamage)
                            {
                                damageTypeDef = blastDamage;
                            }
                            if (winner == scoreBurstDamage)
                            {
                                damageTypeDef = shredDamage;
                            }
                            if (winner == scoreVirusDamage)
                            {
                                damageTypeDef = virusDamage;
                            }
                            if (winner == scoreParalysisDamage)
                            {
                                damageTypeDef = paralysisDamage;
                            }
                            if (winner == scoreHighDamage)
                            {
                                damageTypeDef = null; //projectileDamage;
                            }
                            return damageTypeDef;
                        }
                        else
                        {
                            return projectileDamage;
                        }
                    }

                    else
                    {
                        return projectileDamage;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }

            public static void AdjustRevenantResistanceStatusDescription()
            {
                try
                {
                    _revenantResistanceStatus.Multiplier = 0.5f;

                    string descriptionDamage = "";

                    if (_revenantResistanceStatus.DamageTypeDefs[0] == acidDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_ACID_NAME")}</b>";//"<b>acid damage</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == blastDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_BLAST_NAME")}</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == fireDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_FIRE_NAME")}</b>"; //"<b>fire damage</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == shredDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_SHRED_NAME")}</b>"; //"<b>shred damage</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == virusDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_VIRUS_NAME")}</b>"; //"<b>virus damage</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == paralysisDamage)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_PHOENIXPEDIA_GUIDE_PARALYSE_NAME")}</b>"; //"<b>paralysis damage</b>";
                    }
                    else if (_revenantResistanceStatus.DamageTypeDefs[0] == null)
                    {
                        descriptionDamage = $"<b>{TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_HIGH_DAMAGE")}</b>"; //high damage attacks
                        _revenantResistanceStatus.Multiplier = 1f;
                    }

                    _revenantResistanceStatus.Visuals.DisplayName1 = new LocalizedTextBind($"{TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE0")} - {descriptionDamage.ToUpper()}", true);
                    _revenantResistanceStatus.Visuals.Description = new LocalizedTextBind($"{(1 - _revenantResistanceStatus.Multiplier) * 100}% {TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE1")} {descriptionDamage} {TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE2")}", true);

                    if (_revenantResistanceStatus.DamageTypeDefs[0] == null)
                    {
                        _revenantResistanceStatus.Visuals.DisplayName1 = new LocalizedTextBind($"{TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE0")} - {descriptionDamage.ToUpper()}", true);
                        _revenantResistanceStatus.Visuals.Description = new LocalizedTextBind($"{TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE3")}", true);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            public static void ModifyRevenantResistanceAbility(TacticalLevelController controller)
            {
                try
                {
                    _revenantResistanceStatus.DamageTypeDefs[0] = GetPreferredDamageType(controller);
                    AdjustRevenantResistanceStatusDescription();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void AddRevenantResistanceStatus(TacticalActorBase tacticalActorBase)
            {
                TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                if (!tacticalActor.Status.HasStatus(_revenantResistanceStatus) && !tacticalActor.GameTags.Last().name.Contains("Mindfragged"))
                {
                    tacticalActor.Status.ApplyStatus<DamageMultiplierStatus>(_revenantResistanceStatus);
                }

                if (!tacticalActorBase.HasGameTag(RevenantResistanceGameTag) && !tacticalActor.GameTags.Last().name.Contains("Mindfragged"))
                {
                    tacticalActorBase.GameTags.Add(RevenantResistanceGameTag);
                }
            }

            public static void ImplementVO19(TacticalLevelController controller)
            {
                try
                {
                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) && TFTVVoidOmens.VoidOmensCheck[19] && revenantID == 0)
                    {
                        TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                        foreach (TacticalActorBase pandoranBase in pandorans.Actors)
                        {
                            TacticalActor pandoran = pandoranBase as TacticalActor;
                            if (pandoran != null)
                            {
                                TFTVLogger.Always("The Pandoran is " + pandoran.DisplayName);

                                if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(pandoran.GeoUnitId)
                                     && !pandoran.GameTags.Contains(AnyRevenantGameTag)
                                     && !pandoran.Status.HasStatus(_revenantResistanceStatus))
                                {
                                    AddRevenantResistanceStatus(pandoran);
                                    TFTVLogger.Always(pandoran.name + " received the revenant resistance ability.");
                                }
                            }
                        }
                        string newGuid = Guid.NewGuid().ToString();
                        string hintDescription = _revenantResistanceStatus.Visuals.Description.Localize();
                        string hintTitle = TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE_VO19_TITLE");

                        // TFTVLogger.Always("Got to before hint");

                        TFTVHints.HintDefs.DynamicallyCreatedHints.CreateNewTacticalHintForRevenantResistance("RevenantResistanceSighted", HintTrigger.ActorSeen, "RevenantResistance_GameTagDef", hintTitle, hintDescription);
                      
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class Spawning
        {
            public static void RevenantCheckAndSpawn(TacticalLevelController controller)
            {
                try
                {
                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) && DeadSoldiersDelirium.Count > 0 && !controller.TacMission.IsFinalMission)
                    {
                        if (!revenantSpawned && revenantCanSpawn) //&& (timeLastRevenantSpawned == 0 || )
                        {
                            TFTVLogger.Always("RevenantCheckAndSpawn invoked");
                            TryToSpawnRevenant(controller);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static GeoTacUnitId RevenantRoll(TacticalLevelController controller)
            {
                try
                {
                    List<int> allDeadSoldiers = DeadSoldiersDelirium.Keys.ToList();
                    List<GeoTacUnitId> candidates = new List<GeoTacUnitId>();
                    TFTVLogger.Always("Total count in DeadSoldiersDelirium is " + allDeadSoldiers.Count);

                    foreach (int deadSoldier in allDeadSoldiers)
                    {
                        candidates.Add(GetDeadSoldiersIdFromInt(deadSoldier, controller));
                        TFTVLogger.Always("deadSoldier " + deadSoldier + " with GeoTacUnitId "
                            + GetDeadSoldiersIdFromInt(deadSoldier, controller) + " is added to the list of Revenant candidates");
                    }

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(0, candidates.Count());
                    TFTVLogger.Always("The total number of candidates is " + candidates.Count() + " and the roll is " + roll);

                    GeoTacUnitId theChosen = candidates[roll];
                    TFTVLogger.Always("The Chosen is " + GetDeadSoldiersNameFromID(theChosen, controller));
                    return theChosen;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
            public static List<ClassTagDef> GetRevenantClassTag(TacticalLevelController controller, GeoTacUnitId theChosen)
            {
                try
                {
                    int delirium = DeadSoldiersDelirium[theChosen];
                    List<ClassTagDef> possibleTags = new List<ClassTagDef> { crabTag };

                    TFTVLogger.Always("The Chosen, " + GetDeadSoldiersNameFromID(theChosen, controller) + ", has " + delirium + " Delirium");

                    if (delirium > 2)
                    {
                        possibleTags.Add(fishmanTag);
                    }
                    if (delirium > 5)
                    {
                        possibleTags.Add(sirenTag);
                    }
                    if (delirium > 6)
                    {
                        possibleTags.Add(chironTag);
                    }
                    if (delirium > 8)
                    {
                        possibleTags.Add(acheronTag);
                    }
                    if (delirium > 9)
                    {
                        possibleTags.Add(queenTag);

                    }

                    return possibleTags;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
            public static void TryToSpawnRevenant(TacticalLevelController controller)
            {
                try
                {
                    if (controller.GetFactionByCommandName("aln") != null)
                    {
                        TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                        GeoTacUnitId theChosen = RevenantRoll(controller);
                        List<ClassTagDef> possibleClasses = GetRevenantClassTag(controller, theChosen);
                        List<TacticalActorBase> candidates = new List<TacticalActorBase>();
                        TacticalActorBase actor = new TacticalActorBase();
                        int availableTags = possibleClasses.Count;


                        for (int i = 0; i < availableTags; i++)
                        {

                            foreach (TacticalActorBase tacticalActorBase in pandorans.Actors.Where(tab => tab.GameTags.Contains(possibleClasses.Last())))
                            {
                                candidates.Add(tacticalActorBase);

                            }
                            candidates = candidates.OrderByDescending(tab => tab.DeploymentCost).ToList();



                            if (candidates.Count > 0)
                            {
                                actor = candidates.First();
                                i = availableTags;
                            }
                            else
                            {
                                TFTVLogger.Always(possibleClasses.Last() + " no actor with this tag, removing from list");
                                possibleClasses.Remove(possibleClasses.Last());

                                if (i == availableTags - 1)
                                {
                                    TFTVLogger.Always("No eligible Pandoran found, no Revenant will spawn this time");
                                    return;
                                }
                            }
                        }


                        TFTVLogger.Always("Here is an eligible Pandoran to be a Revenant: " + actor.GetDisplayName());
                        TacticalActor tacticalActor = actor as TacticalActor;
                        UIandFX.Breath.AddRevenantStatusEffect(actor);
                        SetRevenantTierTag(theChosen, actor, controller);
                        actor.name = GetDeadSoldiersNameFromID(theChosen, controller);
                        // SetDeathTime(theChosen, __instance, timeOfMissionStart);
                        revenantID = theChosen;
                        StatsAndClasses.SetRevenantClassAbility(theChosen, controller, tacticalActor);
                        Resistance.AddRevenantResistanceStatus(actor);
                        //  SpreadResistance(__instance);
                        actor.UpdateStats();
                        //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                        // revenantCanSpawn = false;

                        foreach (TacticalActorBase pandoranBase in pandorans.Actors)
                        {
                            TacticalActor pandoran = pandoranBase as TacticalActor;
                            if (pandoran != null)
                            {
                                TFTVLogger.Always("The Pandoran is " + pandoran.DisplayName);

                                if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(pandoran.GeoUnitId)
                                     && !pandoran.GameTags.Contains(AnyRevenantGameTag)
                                     && !pandoran.Status.HasStatus(_revenantResistanceStatus))
                                {

                                    Resistance.AddRevenantResistanceStatus(pandoran);
                                    TFTVLogger.Always(pandoran.name + " received the revenant resistance ability.");
                                }
                            }
                        }
                        string newGuid = Guid.NewGuid().ToString();



                        string hintDescription = $"{_revenantResistanceStatus.Visuals.Description.Localize()}\n{TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE_HINT")}";
                        string hintTitle = TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_RESISTANCE_TITLE");

                        // TFTVLogger.Always("Got to before hint");

                        TFTVHints.HintDefs.DynamicallyCreatedHints.CreateNewTacticalHintForRevenantResistance("RevenantResistanceSighted", HintTrigger.ActorSeen, "RevenantResistance_GameTagDef", hintTitle, hintDescription);
                        
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static string GetDeadSoldiersNameFromID(GeoTacUnitId deadSoldier, TacticalLevelController level)
            {
                try
                {
                    string name = level.TacticalGameParams.Statistics.DeadSoldiers[deadSoldier].Name;
                    return name;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
            public static void SetRevenantTierTag(GeoTacUnitId deadSoldier, TacticalActorBase actor, TacticalLevelController level)
            {
                try
                {
                    SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers[deadSoldier];
                    int numMissions = deadSoldierStats.MissionsParticipated;
                    int enemiesKilled = deadSoldierStats.EnemiesKilled.Count;
                    int soldierLevel = deadSoldierStats.Level;
                    int score = (numMissions + enemiesKilled + soldierLevel) / 2;
                    TFTVLogger.Always("#of Missions: " + numMissions + ". #enemies killed: " + enemiesKilled + ". level: " + soldierLevel + ". The score is " + score);
                    GameTagDef tag = new GameTagDef();

                    if (score >= 30)
                    {
                        tag = RevenantTier3GameTag;
                    }
                    else if (score <= 30 && score > 10)
                    {
                        tag = RevenantTier2GameTag;
                    }
                    else if (score <= 10)
                    {
                        tag = RevenantTier1GameTag;
                    }
                    if (!actor.HasGameTag(tag))
                    {
                        actor.GameTags.Add(tag);
                    }
                    if (!actor.HasGameTag(AnyRevenantGameTag))
                    {
                        actor.GameTags.Add(AnyRevenantGameTag);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static GeoTacUnitId GetDeadSoldiersIdFromInt(int id, TacticalLevelController level)
            {
                try
                {
                    foreach (GeoTacUnitId deadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                    {
                        if (deadSoldier == id)
                        {
                            return deadSoldier;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
            public static GeoTacUnitId GetDeadSoldiersIdFromName(string name, TacticalLevelController level)
            {
                try
                {
                    foreach (SoldierStats deadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Values)
                    {
                        if (deadSoldier.Name == name)
                        {
                            foreach (GeoTacUnitId idOfDeadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                            {

                                if (level.TacticalGameParams.Statistics.DeadSoldiers.GetValueSafe(idOfDeadSoldier) == deadSoldier)
                                {
                                    TFTVLogger.Always("The soldier is " + deadSoldier.Name + " with GeoTacUnitID " + idOfDeadSoldier.ToString());
                                    return idOfDeadSoldier;
                                }

                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }
        }

        internal class RecordUpkeep
        {
           
                public static void UpdateRevenantTimer(GeoLevelController controller)
                {
                    try
                    {
                        if (revenantSpawned)
                        {
                            daysRevenantLastSeen = controller.Timing.Now.TimeSpan.Days;
                            controller.EventSystem.SetVariable("Revenant_Spotted", controller.EventSystem.GetVariable("Revenant_Spotted") + 1);
                            revenantSpawned = false;
                            TFTVLogger.Always("Last time a Revenant was seen was on day " + daysRevenantLastSeen + ", and now it is day " + controller.Timing.Now.TimeSpan.Days);
                            TFTVLogger.Always("# Revenants spotted " + controller.EventSystem.GetVariable("Revenant_Spotted"));
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            public static void RecordPhoenixDeadForRevenantsAndOsiris(DeathReport deathReport, TacticalLevelController controller)
            {
                try
                {
                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("px")) && deathReport.Actor.TacticalFaction == controller.GetFactionByCommandName("PX")
                        && !controller.TacMission.MissionData.MissionType.name.Contains("Tutorial") && !controller.TacMission.IsFinalMission)
                    {

                        if (TFTVRevenantResearch.ProjectOsiris)
                        {
                            ClassTagDef mutoidTag = DefCache.GetDef<ClassTagDef>("Mutoid_ClassTagDef");

                            if (controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !controller.TacticalGameParams.Statistics.DeadSoldiers.ContainsKey(deathReport.Actor.GeoUnitId) && !deathReport.Actor.GameTags.Contains(mutoidTag))

                                TFTVRevenantResearch.RecordStatsOfDeadSoldier(deathReport.Actor);
                        }


                        if (controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !controller.TacticalGameParams.Statistics.DeadSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !DeadSoldiersDelirium.ContainsKey(deathReport.Actor.GeoUnitId))
                        {
                            AddtoListOfDeadSoldiers(deathReport.Actor);
                            TFTVStamina.charactersWithDisabledBodyParts.Remove(deathReport.Actor.GeoUnitId);
                            TFTVLogger.Always(deathReport.Actor.DisplayName + " died at. The deathlist now has " + DeadSoldiersDelirium.Count);
                            if (deathReport.Actor.DisplayName != controller.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name)
                            {
                                TFTVLogger.Always("Dead actor " + deathReport.Actor.DisplayName + " is " + controller.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name +
                                    " in the files. Files will be updated");

                                PhoenixStatisticsManager phoenixStatisticsManager = GameUtl.GameComponent<PhoenixGame>().GetComponent<PhoenixStatisticsManager>();

                                if (phoenixStatisticsManager == null)
                                {
                                    TFTVLogger.Always($"Failed to get stat manager in RecordPhoenixDeadForRevenantAndOsiris");
                                    return;
                                }
                                //  PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));
                                phoenixStatisticsManager.CurrentGameStats.DeadSoldiers[deathReport.Actor.GeoUnitId].Name = deathReport.Actor.DisplayName;
                                controller.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name = deathReport.Actor.DisplayName;
                                TFTVLogger.Always("Name in files of Living Soldiers changed to " + controller.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name);
                                TFTVLogger.Always("Name in files of currentstats changed to " + phoenixStatisticsManager.CurrentGameStats.DeadSoldiers[deathReport.Actor.GeoUnitId].Name);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void AddtoListOfDeadSoldiers(TacticalActorBase deadSoldier)//, TacticalLevelController level)
            {

                try
                {

                    TacticalActor tacticalActor = (TacticalActor)deadSoldier;
                    int delirium = tacticalActor.CharacterStats.Corruption.IntValue;
                    TFTVLogger.Always("The character that died has " + delirium + " Delirium");
                    if (delirium > 0)
                    {
                        DeadSoldiersDelirium.Add(deadSoldier.GeoUnitId, delirium);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            public static void RevenantKilled(DeathReport deathReport, TacticalLevelController controller)
            {
                try
                {
                    if (deathReport.Actor.HasGameTag(AnyRevenantGameTag))
                    {
                        revenantSpawned = true;
                        TFTVLogger.Always("Revenant was killed, so revenantSpawned is now " + revenantSpawned);
                        if (!RevenantsKilled.Keys.Contains(revenantID))
                        {
                            RevenantsKilled.Add(revenantID, 0);
                        }

                        if (deathReport.Actor.HasGameTag(RevenantTier1GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 1;
                            //  TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                            // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(2);
                        }
                        else if (deathReport.Actor.HasGameTag(RevenantTier2GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 5;
                            //  TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                            // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(4);
                        }
                        else if (deathReport.Actor.HasGameTag(RevenantTier3GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 10;
                            // TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                            // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(6);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            public static bool SkillPointsForRevenantKillAwarded = false;

            [HarmonyPatch(typeof(TacticalFaction), "get_Skillpoints")]
            public static class TacticalFaction_GiveExperienceForObjectives_Revenant_Patch
            {
                public static void Postfix(TacticalFaction __instance, ref int __result)
                {
                    try
                    {
                        // TFTVLogger.Always("get_Skillpoints");

                        if (__instance == __instance.TacticalLevel.GetFactionByCommandName("PX") && !SkillPointsForRevenantKillAwarded)
                        {
                            TacticalFaction phoenix = __instance;

                            if (TFTVRevenantResearch.RevenantPoints == 10)
                            {
                                __result += 6;
                                // TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                                SkillPointsForRevenantKillAwarded = true;
                            }
                            else if (TFTVRevenantResearch.RevenantPoints == 5)
                            {
                                __result += 4;
                                //    TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                                SkillPointsForRevenantKillAwarded = true;
                            }
                            else if (TFTVRevenantResearch.RevenantPoints == 1)
                            {
                                __result += 2;
                                //   TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                                SkillPointsForRevenantKillAwarded = true;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

        }

        internal class UIandFX 
        {
            internal class Breath 
            {
                public static void AddRevenantStatusEffect(TacticalActorBase actor)
                {
                    try
                    {
                        actor.Status.ApplyStatus(_revenantStatusAbility);

                        Action delayedAction = () => ModifyMistBreath(actor, Color.red, Color.red);
                        actor.Timing.Start(RunOnNextFrame(delayedAction));

                        // Difference between Timing.Start and Timing.Call:
                        //      -Timing.Start will queue up the coroutine in the timing scheduler and then continue with the current method.
                        //      -Timing.Call will start the coroutine and WAIT until it is complete.
                        // Generally Start is used in methods which are not coroutines while Call is used within other coroutines 

                        // So we can safely afford to modify the visual parts by starting another coroutine from this synchronous method by using Timing.Start.

                        actor.Timing.Start(DiscoBreath(actor));

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
                private static IEnumerator<NextUpdate> RunOnNextFrame(Action action)
                {
                    yield return NextUpdate.NextFrame;
                    action.Invoke();
                }

                /// <summary>
                /// Execute a list of actions via coroutine
                /// </summary>
                /// <param name="actions">List of actions to be played in sequential order</param>
                /// <param name="secondsBetweenActions">How many seconds to wait between actions</param>
                /// <param name="startDelay">Delay the start. In seconds.</param>
                /// <param name="repetitions">Count of repetitions. Negative numbers means infinite</param>
                /// <returns></returns>
                private static IEnumerator<NextUpdate> PlayActions(List<Action> actions, float secondsBetweenActions, float startDelay = 0f, int repetitions = 0)
                {


                    yield return NextUpdate.Seconds(startDelay);

                    do
                    {
                        foreach (var action in actions)
                        {
                            action.Invoke();
                            yield return NextUpdate.Seconds(secondsBetweenActions);
                        }

                        if (repetitions == 0)
                        {
                            break;
                        }

                        if (repetitions > 0)
                        {
                            repetitions--;
                        }
                    }
                    while (repetitions != 0);


                }

                private static IEnumerator<NextUpdate> DiscoBreath(TacticalActorBase actor)
                {
                    List<Action> changeColorActions = new List<Action>() {
                () => ModifyMistBreath(actor, Color.red, Color.clear),
                () => ModifyMistBreath(actor, Color.clear, Color.black),
                () => ModifyMistBreath(actor, Color.black, Color.red),
            };

                    yield return actor.Timing.Call(PlayActions(
                        actions: changeColorActions,
                        secondsBetweenActions: 2f,
                        startDelay: 1f,
                        repetitions: -1
                    ));
                }

                private static void ModifyMistBreath(TacticalActorBase actor, Color from, Color to)
                {
                    string targetVfxName = "VFX_OilCrabman_Breath";
                    //string targetVfxName = tacStatus.TacStatusDef.ParticleEffectPrefab.GetComponent<ParticleSpawnSettings>().name;

                    var pssArray = actor.AddonsManager
                        .RigRoot.GetComponentsInChildren<ParticleSpawnSettings>()
                        .Where(pss => pss.name.StartsWith(targetVfxName));

                    var particleSystems = pssArray
                        .SelectMany(pss => pss.GetComponentsInChildren<UnityEngine.ParticleSystem>());

                    foreach (var ps in particleSystems)
                    {
                        var mainModule = ps.main;
                        UnityEngine.ParticleSystem.MinMaxGradient minMaxGradient = mainModule.startColor;
                        minMaxGradient.mode = ParticleSystemGradientMode.Color;
                        minMaxGradient.colorMin = from;
                        minMaxGradient.colorMax = to;
                        mainModule.startColor = minMaxGradient;
                    }
                }

            }


            [HarmonyPatch(typeof(TacticalAbility), "GetAbilityDescription")]
            public static class TacticalAbility_DisplayCategory_ChangeDescriptionRevenantSkill_patch
            {
                public static void Postfix(TacticalAbility __instance, ref string __result)
                {
                    try
                    {
                        if (__instance.AbilityDef == _revenantAbility)
                        {
                            string actorName = __instance.TacticalActor.name;
                            string additionalDescription =
                                GetDescriptionOfRevenantClassAbility(actorName, __instance.TacticalActor.TacticalLevel);

                            string fallenComrade0 = TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_ABILITY_DESCRIPION0");
                            string fallenComrade1 = TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_ABILITY_DESCRIPION1");

                            __result = $"{fallenComrade0} {actorName} {fallenComrade1} {additionalDescription}";

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }
            }

            public static string GetDescriptionOfRevenantClassAbility(string name, TacticalLevelController level)
            {
                try
                {
                    List<SoldierStats> deadSoldierStatsList = level.TacticalGameParams.Statistics.DeadSoldiers.Values.ToList();
                    SoldierStats deadSoldierStats = new SoldierStats();

                    foreach (SoldierStats soldierStats in deadSoldierStatsList)
                    {
                        if (soldierStats.Name == name)
                        {
                            deadSoldierStats = soldierStats;
                        }
                    }


                    List<SpecializationDef> specializations = deadSoldierStats.ClassesSpecialized.ToList();

                    string description = "";

                    if (specializations.Contains(assaultSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_ASSAULT");// "Increased damage potential and speed.";
                    }
                    if (specializations.Contains(berserkerSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_SERKER");//"Fearless, fast, unstoppable...";
                    }
                    if (specializations.Contains(heavySpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_HEAVY");//"Bullet sponge.";
                    }
                    if (specializations.Contains(infiltratorSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_INFILTRATOR");//"Scary quiet.";
                    }
                    if (specializations.Contains(priestSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_PRIEST");//"Power overflowing.";
                    }
                    if (specializations.Contains(sniperSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_SNIPER");//"All seeing.";
                    }
                    else if (specializations.Contains(technicianSpecialization))
                    {
                        description += TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DESCRIPTION_TECH");//"Surge!";
                    }

                    return description;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                throw new InvalidOperationException();
            }

            public static string DisplayRevenantName(TacticalActorBase tacticalActorBase)
            {
                try 
                {
                    string result = "";

                    if (tacticalActorBase.GameTags.Contains(RevenantTier1GameTag))
                    {
                        string name = $"{tacticalActorBase.name} {TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_MIMIC")}"; //" Mimic";
                        result = name;
                    }
                    else if (tacticalActorBase.GameTags.Contains(RevenantTier2GameTag))
                    {
                        string name = $"{tacticalActorBase.name} {TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_DYBBUK")}"; // __instance.name + " Dybbuk";
                        result = name;
                    }
                    else if (tacticalActorBase.GameTags.Contains(RevenantTier3GameTag))
                    {
                        string name = $"{tacticalActorBase.name} {TFTVCommonMethods.ConvertKeyToString("KEY_REVENANT_NEMESIS")}"; //__instance.name + " Nemesis";
                        result = name;
                    }

                    return result;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }


            }

           

        }


    }
}


