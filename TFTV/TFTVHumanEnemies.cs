using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.UI;
using Base.Utils;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Tactical.View.ViewModules.UIModuleCharacterStatus;
using static PhoenixPoint.Tactical.View.ViewModules.UIModuleCharacterStatus.CharacterData;

namespace TFTV
{
    internal class TFTVHumanEnemies
    {


        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        private static MultiEffectDef _opticalShieldMultiStatusDef = null;

        public static GameTagDef HumanEnemyTier1GameTag = null;
        public static GameTagDef HumanEnemyTier2GameTag = null;
        public static GameTagDef HumanEnemyTier3GameTag = null;
        public static GameTagDef HumanEnemyTier4GameTag = null;
        public static GameTagDef humanEnemyTagDef = null;
        public static GameTagDef Subject24GameTag = null;


        private static readonly ApplyStatusAbilityDef regeneration = DefCache.GetDef<ApplyStatusAbilityDef>("Regeneration_Torso_Passive_AbilityDef");
        private static readonly HealthChangeStatusDef regenerationStatus = DefCache.GetDef<HealthChangeStatusDef>("Regeneration_Torso_Constant_StatusDef");
        private static AddAttackBoostStatusDef _startingVolleyStatusTactic = null;
        private static PassiveModifierAbilityDef _ambushTactic = null;
        private static ReturnFireAbilityDef _fireDisciplineTactic = null;
        private static readonly StatusDef frenzy = DefCache.GetDef<StatusDef>("Frenzy_StatusDef");
        private static readonly HitPenaltyStatusDef mFDStatus = DefCache.GetDef<HitPenaltyStatusDef>("E_PureDamageBonusStatus [MarkedForDeath_AbilityDef]");

        //Assault abilites
        private static readonly AbilityDef killAndRun = DefCache.GetDef<AbilityDef>("KillAndRun_AbilityDef");
        private static readonly AbilityDef quickAim = DefCache.GetDef<AbilityDef>("BC_QuickAim_AbilityDef");
        private static readonly AbilityDef onslaught = DefCache.GetDef<AbilityDef>("DeterminedAdvance_AbilityDef");
        private static readonly AbilityDef readyForAction = DefCache.GetDef<AbilityDef>("ReadyForAction_AbilityDef");
        private static readonly AbilityDef rapidClearance = DefCache.GetDef<AbilityDef>("RapidClearance_AbilityDef");

        //Berseker abilities
        private static readonly AbilityDef dash = DefCache.GetDef<AbilityDef>("Dash_AbilityDef");
        private static readonly AbilityDef cqc = DefCache.GetDef<AbilityDef>("CloseQuarters_AbilityDef");
        private static readonly AbilityDef bloodlust = DefCache.GetDef<AbilityDef>("BloodLust_AbilityDef");
        private static readonly AbilityDef ignorePain = DefCache.GetDef<AbilityDef>("IgnorePain_AbilityDef");
        private static readonly AbilityDef adrenalineRush = DefCache.GetDef<AbilityDef>("AdrenalineRush_AbilityDef");

        //Heavy
        private static readonly AbilityDef returnFire = DefCache.GetDef<AbilityDef>("ReturnFire_AbilityDef");
        private static readonly AbilityDef hunkerDown = DefCache.GetDef<AbilityDef>("HunkerDown_AbilityDef");
        private static readonly AbilityDef skirmisher = DefCache.GetDef<AbilityDef>("Skirmisher_AbilityDef");
        private static readonly AbilityDef shredResistant = DefCache.GetDef<AbilityDef>("ShredResistant_DamageMultiplierAbilityDef");
        private static readonly AbilityDef rageBurst = DefCache.GetDef<AbilityDef>("RageBurst_RageBurstInConeAbilityDef");

        //infiltrator
        private static readonly AbilityDef surpriseAttack = DefCache.GetDef<AbilityDef>("SurpriseAttack_AbilityDef");
        private static readonly AbilityDef decoy = DefCache.GetDef<AbilityDef>("Decoy_AbilityDef");
        private static readonly AbilityDef neuralFeedback = DefCache.GetDef<AbilityDef>("NeuralFeedback_AbilityDef");
        private static readonly AbilityDef jammingField = DefCache.GetDef<AbilityDef>("JammingFiled_AbilityDef");
        private static readonly AbilityDef parasychosis = DefCache.GetDef<AbilityDef>("Parasychosis_AbilityDef");

        //priest

        private static readonly AbilityDef mindControl = DefCache.GetDef<AbilityDef>("Priest_MindControl_AbilityDef");
        private static readonly AbilityDef inducePanic = DefCache.GetDef<AbilityDef>("InducePanic_AbilityDef");
        private static readonly AbilityDef mindSense = DefCache.GetDef<AbilityDef>("MindSense_AbilityDef");
        private static readonly AbilityDef psychicWard = DefCache.GetDef<AbilityDef>("PsychicWard_AbilityDef");
        private static readonly AbilityDef mindCrush = DefCache.GetDef<AbilityDef>("MindCrush_AbilityDef");

        //sniper
        private static readonly AbilityDef extremeFocus = DefCache.GetDef<AbilityDef>("ExtremeFocus_AbilityDef");
        private static readonly AbilityDef armorBreak = DefCache.GetDef<AbilityDef>("ArmourBreak_AbilityDef");
        private static readonly AbilityDef masterMarksman = DefCache.GetDef<AbilityDef>("MasterMarksman_AbilityDef");
        private static readonly AbilityDef inspire = DefCache.GetDef<AbilityDef>("Inspire_AbilityDef");
        private static readonly AbilityDef markedForDeath = DefCache.GetDef<AbilityDef>("MarkedForDeath_AbilityDef");

        //Technician
        private static readonly AbilityDef fastUse = DefCache.GetDef<AbilityDef>("FastUse_AbilityDef");
        private static readonly AbilityDef electricReinforcement = DefCache.GetDef<AbilityDef>("ElectricReinforcement_AbilityDef");
        private static readonly AbilityDef stability = DefCache.GetDef<AbilityDef>("Stability_AbilityDef");
        private static readonly AbilityDef fieldMedic = DefCache.GetDef<AbilityDef>("FieldMedic_AbilityDef");
        private static readonly AbilityDef amplifyPain = DefCache.GetDef<AbilityDef>("AmplifyPain_AbilityDef");



        public static List<string> HumanEnemiesGangNames = new List<string>();
        public static Dictionary<string, int> HumanEnemiesAndTactics = new Dictionary<string, int>();

        public static int RollCount = 0;
        public static List<ContextHelpHintDef> TacticsHint = new List<ContextHelpHintDef>();

        internal class Defs
        {
            public static void CreateHumanEnemiesDefs()
            {
                try
                {
                    CreateAmbushAbility();
                    CreateHumanEnemiesTags();
                    CreateSubject24();
                    CreateOpticalShieldStatus();
                    CreateStartingVolleyStatus();
                    CreateReturnFireCloneForHumanTactics();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateReturnFireCloneForHumanTactics()
            {
                try
                {
                    string name = "FireDisciplineAbility";
                    ReturnFireAbilityDef returnFireAbilityDefSource = DefCache.GetDef<ReturnFireAbilityDef>("ReturnFire_AbilityDef");
                    ReturnFireAbilityDef newRF = Helper.CreateDefFromClone(returnFireAbilityDefSource, "{AD13E6CD-9D7E-4A5A-8CC0-6AF71DB83B42}", name);
                    newRF.CharacterProgressionData = Helper.CreateDefFromClone(returnFireAbilityDefSource.CharacterProgressionData, "{AF54FD23-931E-43BC-82AD-4471D1881A8C}", name);
                    newRF.ViewElementDef = Helper.CreateDefFromClone(returnFireAbilityDefSource.ViewElementDef, "{AD23518F-1E14-47E3-BF47-C421F7C95D33}", name);
                    _fireDisciplineTactic = newRF;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateStartingVolleyStatus()
            {
                try
                {
                    string name = "StartingVolley";

                    AddAttackBoostStatusDef sourceQA = DefCache.GetDef<AddAttackBoostStatusDef>("E_Status [BC_QuickAim_AbilityDef]");
                    AddAttackBoostStatusDef startingVolley = Helper.CreateDefFromClone(sourceQA, "{E1B4C902-5C37-432A-BA4C-E95EDDDAAFC9}", name);

                    startingVolley.Visuals = Helper.CreateDefFromClone(sourceQA.Visuals, "{A7049D39-6709-4941-9298-ED50699C836A}", name);
                    //startingVolley.Visuals.DisplayName1.LocalizationKey = "KEY_STARTING_VOLLEY_STATUS_NAME";
                    //startingVolley.Visuals.Description.LocalizationKey = "KEY_STARTING_VOLLEY_STATUS_DESCRIPTION";

                    startingVolley.AdditionalStatusesToApply[0] = Helper.CreateDefFromClone(sourceQA.AdditionalStatusesToApply[0], "{98C2606E-AEF4-4A32-9C0A-31600E6F942E}", name);
                    startingVolley.AdditionalStatusesToApply[1] = Helper.CreateDefFromClone(sourceQA.AdditionalStatusesToApply[1], "{2643FFB2-7433-4532-8F01-AF180429B864}", name);

                    ChangeAbilitiesCostStatusDef changeAbilitiesCostStatusDef = (ChangeAbilitiesCostStatusDef)startingVolley.AdditionalStatusesToApply[0];
                    changeAbilitiesCostStatusDef.AbilityCostModification.ActionPointMod = -0.5f;
                    _startingVolleyStatusTactic = startingVolley;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateOpticalShieldStatus()
            {
                try
                {
                    string name = "OpticalShield";

                    MultiEffectDef sourceMultiEffect = (MultiEffectDef)Repo.GetDef("cb989636-ca35-2fb2-c568-604714f19d95");//("E_MultiEffect [PainChameleon_AbilityDef]");
                    MultiEffectDef newMultiEffect = Helper.CreateDefFromClone(sourceMultiEffect, "{250ACA8F-F137-47F5-9B4F-DCD4741DBA4A}", name);
                    TacStatusEffectDef newStatusEffectHoldingVanishEffect = Helper.CreateDefFromClone((TacStatusEffectDef)sourceMultiEffect.EffectDefs[0], "{8890A518-B196-4CF2-97E0-310D8B074108}", name);// ("E_ApplyVanishStatusEffect [PainChameleon_AbilityDef]");

                    StanceStatusDef vanishedStatusSource = (StanceStatusDef)Repo.GetDef("8dbf3262-686d-2fb2-91cc-47014c539d95");

                    StanceStatusDef newVanishedStatus = Helper.CreateDefFromClone(vanishedStatusSource, "{AF8D634C-3712-4F17-B256-7B8FB051A43F}", name); // ("E_VanishedStatus [PainChameleon_AbilityDef]");

                    newVanishedStatus.Visuals = Helper.CreateDefFromClone(vanishedStatusSource.Visuals, "{3FC367B1-A4FF-43D6-A18D-922DF3EA528D}", name);

                    newStatusEffectHoldingVanishEffect.StatusDef = newVanishedStatus;
                    newVanishedStatus.Visuals.DisplayName1.LocalizationKey = "KEY_ACTIVE_CAMO_DISPLAY_NAME";
                    newVanishedStatus.Visuals.Description.LocalizationKey = "KEY_ACTIVE_CAMO_DESCRIPTION";
                    _opticalShieldMultiStatusDef = newMultiEffect;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateHumanEnemiesTags()
            {
                try
                {
                    string tagName = "HumanEnemy";
                    string anu = "anu";
                    string bandit = "ban";
                    string newJericho = "nj";
                    string synedrion = "syn";
                    string forsaken = "FallenOnes";
                    string pure = "Purists";

                    GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                    HumanEnemyTier1GameTag = Helper.CreateDefFromClone(
                        source,
                        "11F227E3-A45A-44EE-8B93-94E59D8C7B53",
                        tagName + "Tier_1_" + "GameTagDef");
                    HumanEnemyTier2GameTag = Helper.CreateDefFromClone(
                        source,
                        "CE88CFDB-B010-40A7-A86A-C842DF5F35CF",
                        tagName + "Tier_2_" + "GameTagDef");
                    HumanEnemyTier3GameTag = Helper.CreateDefFromClone(
                        source,
                        "D4E764C5-3978-40C3-8CED-AFAF81B40BF8",
                        tagName + "Tier_3_" + "GameTagDef");
                    HumanEnemyTier4GameTag = Helper.CreateDefFromClone(
                        source,
                        "21D065AC-432F-4D29-92AF-5355EF972E38",
                        tagName + "Tier_4_" + "GameTagDef");
                    GameTagDef anuGameTag = Helper.CreateDefFromClone(
                        source,
                        "1C8EC6EF-CE51-4AC5-B799-128FDE6ABF14",
                        tagName + "Faction_" + anu + "_GameTagDef");
                    GameTagDef banditGameTag = Helper.CreateDefFromClone(
                        source,
                        "78993F15-9233-4C49-B8C3-13144156E438",
                        tagName + "Faction_" + bandit + "_GameTagDef");
                    GameTagDef newJerichoGameTag = Helper.CreateDefFromClone(
                        source,
                        "62980A28-8E7A-4F0D-A01C-B58C4D085677",
                        tagName + "Faction_" + newJericho + "_GameTagDef");
                    GameTagDef SynedrionGameTag = Helper.CreateDefFromClone(
                        source,
                        "B29CEA3A-6C24-4872-9773-02E2FC21F645",
                        tagName + "Faction_" + synedrion + "_GameTagDef");
                    GameTagDef forsakenGameTag = Helper.CreateDefFromClone(
                        source,
                        "133FA2A8-C93D-43A9-BEFB-E5FAAAC43AFF",
                        tagName + "Faction_" + forsaken + "_GameTagDef");
                    GameTagDef pureGameTag = Helper.CreateDefFromClone(
                        source,
                        "DDDAB7AC-1317-4B37-AB18-1E57F8D30147",
                        tagName + "Faction_" + pure + "_GameTagDef");
                    humanEnemyTagDef = Helper.CreateDefFromClone(
                        source,
                        "BF6F6546-AE38-47E0-B581-FDB8F8F5171D",
                        tagName + "_GameTagDef");


                    //    TFTVLogger.Always("Human Enemy Tags created");

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            private static void CreateAmbushAbility()
            {
                try
                {

                    string skillName = "HumanEnemiesTacticsAmbush_AbilityDef";
                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                    PassiveModifierAbilityDef ambushAbility = Helper.CreateDefFromClone(
                        source,
                        "31785839-0687-4065-ACFB-255C1A1CE63D",
                        skillName);
                    ambushAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                        source.CharacterProgressionData,
                        "136290BA-D672-4EEF-822E-F3B8FF27496C",
                        skillName);
                    ambushAbility.ViewElementDef = Helper.CreateDefFromClone(
                        source.ViewElementDef,
                        "6D47E347-35DE-4E8E-B6FF-9B9DF0598175",
                        skillName);
                    ambushAbility.StatModifications = new ItemStatModification[]
                    { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                    };
                    ambushAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                    ambushAbility.ViewElementDef.DisplayName1.LocalizationKey = "HUMAN_ENEMIES_KEY_AMBUSH";
                    ambushAbility.ViewElementDef.Description.LocalizationKey = "HUMAN_ENEMIES_KEY_AMBUSH_DESCRIPTION";

                    Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_TacticalAnalyst.png");
                    ambushAbility.ViewElementDef.LargeIcon = icon;
                    ambushAbility.ViewElementDef.SmallIcon = icon;
                    _ambushTactic = ambushAbility;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateSubject24()
            {
                try
                {
                    string nameDef = "Subject24_TacCharacerDef";

                    TacCharacterDef subject24 = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Jugg_TacCharacterDef"), "A4F0335E-BF41-4175-8C28-7B0DE5352224", nameDef);
                    subject24.Data.Name = "Subject 24";


                    TacticalItemDef juggBionicLeft = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_LeftArm_BodyPartDef");
                    TacticalItemDef juggBionicRight = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_RightArm_BodyPartDef");
                    TacticalItemDef sourceJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_Torso_BodyPartDef");

                    TacticalItemDef juggTorso = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");

                    TacticalItemDef subject24jacket = Helper.CreateDefFromClone(sourceJacket, "{1E1723AD-09B0-49FD-8FCF-C338AD22EE4D}", "Subject24_jacket");
                    subject24jacket.ViewElementDef = Helper.CreateDefFromClone(sourceJacket.ViewElementDef, "{18A8F78D-D16A-4B98-A22B-B9C7DAC11A90}", "Subject24_jacket");
                    subject24jacket.ViewElementDef.DisplayName1 = juggTorso.ViewElementDef.DisplayName1;
                    subject24jacket.ViewElementDef.DisplayName2 = juggTorso.ViewElementDef.DisplayName2;
                    subject24jacket.ViewElementDef.Description = juggTorso.ViewElementDef.Description;
                    subject24jacket.ViewElementDef.Category = juggTorso.ViewElementDef.Category;
                    subject24jacket.ViewElementDef.LargeIcon = juggTorso.ViewElementDef.LargeIcon;
                    subject24jacket.ViewElementDef.InventoryIcon = juggTorso.ViewElementDef.InventoryIcon;
                    subject24jacket.ViewElementDef.RosterIcon = juggTorso.ViewElementDef.RosterIcon;
                    subject24jacket.Armor = juggTorso.Armor;
                    subject24jacket.IsPermanentAugment = true;

                    subject24jacket.SubAddons[0] = new AddonDef.SubaddonBind() { SubAddon = juggBionicLeft };
                    subject24jacket.SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = juggBionicRight };
                    subject24jacket.BodyPartAspectDef = juggTorso.BodyPartAspectDef;

                    subject24.Data.BodypartItems = new ItemDef[] { subject24jacket,
                       DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef"), DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Legs_ItemDef")
                   };


                    // CustomizationColorTagDef_10 green
                    // CustomizationColorTagDef_14 pink
                    // CustomizationColorTagDef_0 grey
                    // CustomizationColorTagDef_7 red

                    CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                    GameTagDef subject24Tag = TFTVCommonMethods.CreateNewTag("Subject24Tag", "{DFFCBF3B-AECE-4543-A8DC-D41CC67B9FFB}");

                    Subject24GameTag = subject24Tag;

                    List<GameTagDef> gameTags = subject24.Data.GameTags.ToList();
                    gameTags.Add(blackColor);
                    gameTags.Add(TFTVDefsInjectedOnlyOnce.AlwaysDeployTag);
                    gameTags.Add(subject24Tag);
                    subject24.SpawnCommandId = "Subject24TFTV";
                    subject24.Data.GameTags = gameTags.ToArray();

                    List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("StoryPU14_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits.ToList();
                    TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                    {
                        Character = subject24,
                        Amount = new RangeDataInt { Max = 1, Min = 1 },
                    };
                    tacCharacterDefs.Add(uniqueChatarcterBind);
                    DefCache.GetDef<CustomMissionTypeDef>("StoryPU14_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits = tacCharacterDefs.ToArray();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        public static void RollTactic(string nameOfFaction)
        {
            try
            {
                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                int roll = UnityEngine.Random.Range(1, 10);

                TFTVLogger.Always("The tactics roll is " + roll);


                if (!HumanEnemiesAndTactics.ContainsKey(nameOfFaction))
                {
                    HumanEnemiesAndTactics.Add(nameOfFaction, roll);
                    RollCount++;
                }

                TFTVLogger.Always("Tactics have been rolled " + RollCount + " times, and there are currently " + HumanEnemiesAndTactics.Count + " tactics in play.");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static string GenerateGangName(TacticalFaction faction)
        {
            try
            {
                string nameOfFaction = faction.Faction.FactionDef.ShortName;
                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                string name = TFTVHumanEnemiesNames.GetSquadName(nameOfFaction);
                TFTVLogger.Always("The gang names is" + name);
                return name;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static string[] GetTacticNameAndDescription(int roll, TacticalFaction enemyHumanFaction)
        {
            try
            {
                string description = "";
                string tactic = "";

                if (roll == 1)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FEARSOME_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FEARSOME_TITLE");
                }
                else if (roll == 2)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_VOLLEY_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_VOLLEY_TITLE");

                }
                else if (roll == 3)
                {
                    if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("Purists"))
                    {

                        description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SELFREPAIR_TEXT");
                        tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SELFREPAIR_TITLE");
                    }
                    else
                    {

                        description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DRUGS_TEXT");
                        tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DRUGS_TITLE");

                    }
                }
                else if (roll == 4)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_CAMO_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_CAMO_TITLE");
                }
                else if (roll == 5)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DISCIPLINE_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DISCIPLINE_TITLE");
                }
                else if (roll == 6)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FRENZY_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FRENZY_TITLE");
                }
                else if (roll == 7)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_RETRIBUTION_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_RETRIBUTION_TITLE");
                }
                else if (roll == 8)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_AMBUSH_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_AMBUSH_TITLE");
                }
                else if (roll == 9)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_TARGETING_TEXT");
                    tactic = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_TARGETING_TITLE");
                }

                return new string[] { tactic, description };


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static string[] GetTypeOfEnemyUnit(TacticalFaction enemyHumanFaction)
        {
            try
            {
                string unitType = "";
                string fileNameSquadPic = "";

                if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("ban"))
                {
                    unitType = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_UNIT_TYPE_GANG");
                    //  factionTag= "NEU_Bandits_TacticalFactionDef";
                    fileNameSquadPic = "ban_squad.png";
                }
                else if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("nj") || enemyHumanFaction.Faction.FactionDef.ShortName.Equals("anu") || enemyHumanFaction.Faction.FactionDef.ShortName.Equals("syn"))
                {

                    string factionName = "";
                    if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("nj"))
                    {
                        factionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_NJ");
                        //  factionTag = "NewJericho_TacticalFactionDef";
                        fileNameSquadPic = "nj_squad.jpg";
                    }
                    else if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("anu"))
                    {
                        factionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_ANU");
                        //  factionTag = "Anu_TacticalFactionDef";
                        fileNameSquadPic = "anu_squad.jpg";
                    }
                    else
                    {
                        factionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SYN");
                        //  factionTag = "Synedrion_TacticalFactionDef";                        
                        fileNameSquadPic = "syn_squad.jpg";
                    }

                    unitType = $"{TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_INDEFINITEARTICLE")}{factionName} {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_UNIT_TYPE_SQUAD")}";
                }
                else if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("FallenOnes"))
                {
                    unitType = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_UNIT_TYPE_PACK");
                    // factionTag = "AN_FallenOnes_TacticalFactionDef";
                    fileNameSquadPic = "fo_squad.jpg";
                }
                else if (enemyHumanFaction.TacticalFactionDef.ShortName.Equals("Purists"))
                {
                    unitType = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_UNIT_TYPE_ARRAY");
                    // factionTag = "NJ_Purists_TacticalFactionDef";

                    if (fileNameSquadPic == "")
                    {
                        fileNameSquadPic = "pu_squad.jpg";
                    }
                }

                return new string[] { unitType, fileNameSquadPic };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static void GenerateHumanEnemyUnit(TacticalActor leader, TacticalFaction enemyHumanFaction, string nameOfLeader, int roll)
        {
            try
            {
                string nameOfGang = "";

                string[] unitTypeData = GetTypeOfEnemyUnit(enemyHumanFaction);

                string unitType = unitTypeData[0];
                string fileNameSquadPic = unitTypeData[1];

                if (nameOfLeader != "Subject 24")
                {
                    nameOfGang = GenerateGangName(enemyHumanFaction);
                }
                else
                {
                    nameOfGang = "Subject 24";
                    fileNameSquadPic = "subject24_squad.jpg";
                }


                string[] tacticAndDescription = GetTacticNameAndDescription(roll, enemyHumanFaction);


                string nameOfTactic = tacticAndDescription[0];
                string descriptionOfTactic = tacticAndDescription[1];

                string descriptionHint = "";

                string youAreFacing = TFTVCommonMethods.ConvertKeyToString("KEY_HUMAN_ENEMIES_FACING");
                string called = TFTVCommonMethods.ConvertKeyToString("KEY_HUMAN_ENEMIES_GANG_CALLED");
                string leaderIs = TFTVCommonMethods.ConvertKeyToString("KEY_HUMAN_ENEMIES_LEADER_IS");
                string usingTactic = TFTVCommonMethods.ConvertKeyToString("KEY_HUMAN_ENEMIES_USING_TACTIC");

                string pureArrayDescription = TFTVCommonMethods.ConvertKeyToString("KEY_HUMAN_ENEMIES_SUBJECT24_ARRAY_DESCRIPTION");

                if (nameOfLeader != "Subject 24")
                {
                    descriptionHint = $"{youAreFacing} {unitType}{called} {nameOfGang}{leaderIs} {nameOfLeader}{usingTactic} {nameOfTactic}: {descriptionOfTactic}";
                }
                else
                {
                    descriptionHint = $"{pureArrayDescription} {nameOfTactic}: {descriptionOfTactic}";
                }

                ContextHelpHintDef humanEnemySightedHint = TFTVHints.HintDefs.DynamicallyCreatedHints.CreateNewTacticalHintForHumanEnemies(nameOfGang, HintTrigger.ActorSeen, "HumanEnemyFaction_" + enemyHumanFaction.TacticalFactionDef.ShortName + "_GameTagDef", nameOfGang, descriptionHint, fileNameSquadPic);

                HumanEnemiesGangNames.Add(nameOfGang);

                TacticsHint.Add(humanEnemySightedHint);


            }
            catch (Exception e)
            {

                TFTVLogger.Error(e);
            }
        }

        public static void ImplementHumanEnemies(TacticalLevelController controller)
        {
            try
            {
                foreach (TacticalFaction faction in GetHumanEnemyFactions(controller))
                {
                    AssignHumanEnemiesTags(faction, controller);

                }
            }
            catch (Exception e)
            {

                TFTVLogger.Error(e);
            }

        }

        private static void GetGangerReady(GameTagDef rankTag, string factionName, TacticalActor tacticalActor, GameTagDef factionTag = null)
        {
            try
            {
                tacticalActor.GameTags.Add(rankTag, GameTagAddMode.ReplaceExistingExclusive);

                tacticalActor.GameTags.Add(humanEnemyTagDef);
                if (factionTag != null)
                {
                    tacticalActor.GameTags.Add(factionTag);
                }

                GenderTagDef genderTagDef = tacticalActor.GameTags.FirstOrDefault(x => x is GenderTagDef) as GenderTagDef;

                if (!tacticalActor.HasGameTag(Subject24GameTag))
                {
                    tacticalActor.name = TFTVHumanEnemiesNames.GetName(factionName, rankTag, genderTagDef);
                }
                AdjustStatsAndSkills(tacticalActor);

                ActorClassIconElement actorClassIconElement = tacticalActor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                TFTVUITactical.Enemies.ChangeHealthBarIcon(actorClassIconElement, tacticalActor);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void AssignHumanEnemiesTags(TacticalFaction faction, TacticalLevelController controller, bool isCombatUnit = true)
        {
            try
            {
                TacticalFaction phoenix = controller.GetFactionByCommandName("PX");
                int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);

                TacticalActor leader = null;

                List<TacticalActor> listOfHumansEnemies = faction.TacticalActors.Where(tacticalActor => tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay).ToList();

                if (listOfHumansEnemies.Count == 0)
                {
                    return;
                }

                TFTVLogger.Always($"Subject24GameTag {Subject24GameTag == null}");

                if (listOfHumansEnemies.Any(t => t.HasGameTag(Subject24GameTag)))
                {
                    leader = listOfHumansEnemies.FirstOrDefault(tacticalActor => tacticalActor.HasGameTag(Subject24GameTag));
                    leader.name = TFTVCommonMethods.ConvertKeyToString("KEY_LORE_TITLE_SUBJECT24");
                    listOfHumansEnemies.Remove(leader);

                    TFTVLogger.Always($"Found subject 24 {leader.name}");
                }

                TFTVLogger.Always($"There are {listOfHumansEnemies.Count()} human enemies in {faction.Faction?.FactionDef?.GetName()}");
                List<TacticalActor> orderedListOfHumanEnemies = listOfHumansEnemies.OrderByDescending(e => e.LevelProgression.Level).ToList();

                if (listOfHumansEnemies[0].LevelProgression.Level == listOfHumansEnemies[listOfHumansEnemies.Count - 1].LevelProgression.Level)
                {
                    // TFTVLogger.Always("All enemies are of the same level");
                    orderedListOfHumanEnemies = listOfHumansEnemies.OrderByDescending(e => e.CharacterStats.Willpower.IntValue).ToList();
                }

                if (leader == null && isCombatUnit)
                {
                    leader = orderedListOfHumanEnemies[0];
                    orderedListOfHumanEnemies.Remove(leader);
                }

                int champs = Mathf.FloorToInt(orderedListOfHumanEnemies.Count / (5 - (difficultyLevel / 2)));
                // TFTVLogger.Always("There is space for " + champs + " champs");
                int gangers = Mathf.FloorToInt((orderedListOfHumanEnemies.Count - champs) / (4 - (difficultyLevel / 2)));
                //  TFTVLogger.Always("There is space for " + gangers + " gangers");
                int juves = orderedListOfHumanEnemies.Count - champs - gangers;
                //  TFTVLogger.Always("There is space for " + juves + " juves");

                //  TacticalActorBase leaderBase = leader;
                string nameOfFaction = faction.Faction.FactionDef.ShortName;

                //  TFTVLogger.Always("The short name of the faction is " + nameOfFaction);

                GameTagDef humanEnemyFactionTag = null;

                // if (isCombatUnit)
                //  {
                humanEnemyFactionTag = DefCache.GetDef<GameTagDef>("HumanEnemyFaction_" + nameOfFaction + "_GameTagDef");
                //  }



                // TFTVLogger.Always("gameTagDef found");
                //List<string> factionNames = TFTVHumanEnemiesNames.names.GetValueSafe(nameOfFaction);

                if (leader != null && !leader.GameTags.Contains(HumanEnemyTier1GameTag))
                {
                    GetGangerReady(HumanEnemyTier1GameTag, nameOfFaction, leader, humanEnemyFactionTag);
                }

                if (isCombatUnit)
                {
                    RollTactic(nameOfFaction);
                    GenerateHumanEnemyUnit(leader, faction, leader.name, HumanEnemiesAndTactics[nameOfFaction]);
                }

                for (int i = 0; i < champs; i++)
                {
                    TacticalActor champ = orderedListOfHumanEnemies[i];

                    if (champ.HasGameTag(humanEnemyTagDef))
                    {
                        continue;
                    }

                    GetGangerReady(HumanEnemyTier2GameTag, nameOfFaction, champ, humanEnemyFactionTag);

                    TFTVLogger.Always("This " + champ.name + " is now a champ");
                }

                for (int i = champs; i < champs + gangers; i++)
                {
                    TacticalActor ganger = orderedListOfHumanEnemies[i];

                    if (ganger.HasGameTag(humanEnemyTagDef))
                    {
                        continue;
                    }

                    GetGangerReady(HumanEnemyTier3GameTag, nameOfFaction, ganger, humanEnemyFactionTag);

                    TFTVLogger.Always("This " + ganger.name + " is now a ganger");

                }

                for (int i = champs + gangers; i < champs + gangers + juves; i++)
                {
                    TacticalActor juve = orderedListOfHumanEnemies[i];

                    if (juve.HasGameTag(humanEnemyTagDef))
                    {
                        continue;
                    }

                    GetGangerReady(HumanEnemyTier4GameTag, nameOfFaction, juve, humanEnemyFactionTag);

                    TFTVLogger.Always("This " + juve.name + " is now a juve");


                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static List<TacticalFaction> GetHumanEnemyFactions(TacticalLevelController controller)
        {
            try
            {
                List<TacticalFaction> enemyHumanFactions = new List<TacticalFaction>();

                TacticalFaction phoenix = controller.GetFactionByCommandName("PX");

                foreach (TacticalFaction faction in controller.Factions)
                {
                    if (faction.GetRelationTo(phoenix) == FactionRelation.Enemy)
                    {
                        if (faction.Faction.FactionDef.ShortName.Equals("ban")
                                || faction.Faction.FactionDef.ShortName.Equals("nj") || faction.Faction.FactionDef.ShortName.Equals("anu")
                                || faction.Faction.FactionDef.ShortName.Equals("syn") || faction.Faction.FactionDef.ShortName.Equals("Purists")
                                || faction.Faction.FactionDef.ShortName.Equals("FallenOnes"))
                        {
                            TFTVLogger.Always("The short name of the faction is " + faction.Faction.FactionDef.ShortName);

                            enemyHumanFactions.Add(faction);
                        }
                    }
                }

                return enemyHumanFactions;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        [HarmonyPatch(typeof(TacticalActorBase), "get_DisplayName")]
        public static class TacticalActorBase_GetDisplayName_HumanEnemiesGenerator_Patch
        {
            public static void Postfix(TacticalActorBase __instance, ref string __result)
            {
                try
                {
                    if (GetFactionTierAndClassTags(__instance.GameTags.ToList())[0] != null)
                    {
                        __result = __instance.name + GetRankName(GetFactionTierAndClassTags(__instance.GameTags.ToList()));
                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }
        public static GameTagDef[] GetFactionTierAndClassTags(List<GameTagDef> data)
        {
            try
            {
                GameTagDef[] factionTierAndClass = new GameTagDef[3];
                GameTagDef factionGameTagDef = null;
                GameTagDef tierGameTagDef = null;
                GameTagDef classTagDef = null;
                foreach (GameTagDef tag in data)
                {
                    //  TFTVLogger.Always("Here is tag with name " + tag.name);
                    if (tag.name.Contains("HumanEnemyFaction"))
                    {
                        factionGameTagDef = tag;
                    }
                    // TFTVLogger.Always("The character has the tag " + tag.name);

                    //  TFTVLogger.Always("Here is tag with name " + tag.name);
                    else if (tag.name.Contains("HumanEnemyTier"))
                    {
                        tierGameTagDef = tag;
                        // TFTVLogger.Always("The character has the tag " + tag.name);
                    }
                    else if (tag.name.Contains("Class"))
                    {
                        classTagDef = tag;
                        //  TFTVLogger.Always("The character has the tag " + tag.name);
                    }
                }
                factionTierAndClass[0] = factionGameTagDef;
                factionTierAndClass[1] = tierGameTagDef;
                factionTierAndClass[2] = classTagDef;

                return factionTierAndClass;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static string GetRankName(GameTagDef[] gameTags)
        {
            try
            {

                string factionName = gameTags[0].name.Split('_')[1];
                // TFTVLogger.Always("Faction name is " + factionName);
                List<string> nameRank = TFTVHumanEnemiesNames.ranks.GetValueSafe(factionName);
                string rank = gameTags[1].name.Split('_')[1];
                int rankOrder = int.Parse(rank);
                //  TFTVLogger.Always("Rank order is " + rankOrder);
                string rankName = " " + "(" + nameRank[rankOrder - 1] + ")";

                return rankName;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        private static Color _regularIconColor = new Color(0, 0, 0, 0);

        [HarmonyPatch(typeof(UIModuleCharacterStatus), "SetData")]
        public static class UIModuleCharacterStatus_SetData_AdjustLevel_Patch
        {
            public static void Postfix(CharacterData data, UIModuleCharacterStatus __instance, ListControl ____abilitiesList)
            {
                try
                {

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    if (controller == null)
                    {
                        return;
                    }

                    UIStateCharacterStatus uIStateCharacterStatus = (UIStateCharacterStatus)controller.View.CurrentState;

                    // TFTVLogger.Always($"uIStateCharacterStatus null? {uIStateCharacterStatus==null}");


                    TacticalActor tacticalActor = typeof(UIStateCharacterStatus).GetField("_character", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(uIStateCharacterStatus) as TacticalActor;

                    bool enemy = TFTVUITactical.Enemies.IsReallyEnemy(tacticalActor);

                    // TFTVLogger.Always($"SetData: tacticalActor null? {tacticalActor==null}");

                    if (_regularIconColor == new Color(0, 0, 0, 0))
                    {
                        _regularIconColor = __instance.ClassIcon.MainClassIcon.color;
                    }

                    ActorClassIconElement actorClassIconElement = __instance.ClassIcon;

                    if (enemy)
                    {
                        if (tacticalActor.HasGameTag(TFTVRevenant.AnyRevenantGameTag))
                        {
                            actorClassIconElement.MainClassIcon.color = TFTVUITactical.LeaderColor;
                        }
                        else
                        {
                            actorClassIconElement.MainClassIcon.color = TFTVUITactical.NegativeColor;
                        }
                    }
                    else
                    {
                        actorClassIconElement.MainClassIcon.color = _regularIconColor;
                    }

                    //  __instance.ClassIcon.MainClassIcon.color = _regularIconColor;

                    GameTagDef[] factionAndTier = GetFactionTierAndClassTags(data.Tags.ToList());
                    TFTVUITactical.Enemies.RemoveRankFromInfoPanel(actorClassIconElement);

                    if (factionAndTier[0] != null)
                    {
                        string factionName = factionAndTier[0].name.Split('_')[1];

                        bool friendly = controller.GetFactionByCommandName(factionName).GetRelationTo(controller.GetFactionByCommandName("PX")) != FactionRelation.Enemy;

                        ____abilitiesList.AddRow<CharacterStatusAbilityRowController>
                                (__instance.AbilitiesListAbilityPrototype).SetData(ApplyTextChanges(factionAndTier[0], factionAndTier[1]));

                        TFTVUITactical.Enemies.AdjustIconInfoPanel(actorClassIconElement, factionAndTier[1], friendly);

                        if (factionAndTier[1] == HumanEnemyTier1GameTag)
                        {
                            if (data.Level < 6)
                            {
                                __instance.CharacterLevel.text = "6";
                            }

                            if (HumanEnemiesAndTactics.Count > 0)
                            {
                                int roll = HumanEnemiesAndTactics[factionName];
                                TFTVLogger.Always("factionName is " + factionName + " and the roll is " + roll);
                                ____abilitiesList.AddRow<CharacterStatusAbilityRowController>
                                   (__instance.AbilitiesListAbilityPrototype).SetData(AddTacticsDescription(roll));
                            }
                        }
                        else if (factionAndTier[1] == HumanEnemyTier2GameTag)
                        {
                            if (data.Level < 5)
                            {
                                __instance.CharacterLevel.text = "5";
                            }
                            else if (data.Level > 6)
                            {
                                __instance.CharacterLevel.text = "6";

                            }
                        }
                        else if (factionAndTier[1] == HumanEnemyTier3GameTag)
                        {
                            if (data.Level < 3)
                            {
                                __instance.CharacterLevel.text = "3";
                            }
                            else if (data.Level > 4)
                            {
                                __instance.CharacterLevel.text = "4";

                            }
                        }
                        else if (factionAndTier[1] == HumanEnemyTier4GameTag)
                        {
                            if (data.Level > 2)
                            {
                                __instance.CharacterLevel.text = "2";
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        public static AbilityData ApplyTextChanges(GameTagDef factionTag, GameTagDef tierTag)
        {

            try
            {
                string factionName = factionTag.name.Split('_')[1];
                // TFTVLogger.Always("Faction name is " + factionName);
                List<string> nameRank = TFTVHumanEnemiesNames.ranks.GetValueSafe(factionName);
                string rank = tierTag.name.Split('_')[1];
                int rankOrder = int.Parse(rank);
                // TFTVLogger.Always("Rank order is " + rankOrder);

                string name = nameRank[rankOrder - 1];
                string description = TFTVHumanEnemiesNames.tierDescriptions[rankOrder - 1];
                AbilityData abilityData = new AbilityData
                {
                    Name = new LocalizedTextBind(name, true),
                    LocalizedDescription = description,
                    Icon = Helper.CreateSpriteFromImageFile("UI_StatusesIcons_CanBeRecruitedIntoPhoenix-2.png")
                };
                return abilityData;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        public static AbilityData AddTacticsDescription(int roll)
        {
            try
            {
                string name = TFTVCommonMethods.ConvertKeyToString("KEY_TACTIC");
                string tactic = "";
                string description = "";

                if (roll == 1)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FEARSOME_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FEARSOME_TITLE")}";
                }
                else if (roll == 2)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_VOLLEY_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_VOLLEY_TITLE")}";

                }
                else if (roll == 3)
                {

                    if (HumanEnemiesAndTactics.ContainsKey("pu") || HumanEnemiesAndTactics.ContainsKey("Purists"))
                    {

                        description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SELFREPAIR_TEXT");
                        tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SELFREPAIR_TITLE")}";
                    }
                    else
                    {

                        description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DRUGS_TEXT");
                        tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DRUGS_TITLE")}";

                    }
                }
                else if (roll == 4)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_CAMO_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_CAMO_TITLE")}";
                }
                else if (roll == 5)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DISCIPLINE_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_DISCIPLINE_TITLE")}";
                }
                else if (roll == 6)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FRENZY_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_FRENZY_TITLE")}";
                }
                else if (roll == 7)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_RETRIBUTION_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_RETRIBUTION_TITLE")}";
                }
                else if (roll == 8)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_AMBUSH_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_AMBUSH_TITLE")}";
                }
                else if (roll == 9)
                {
                    description = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_TARGETING_TEXT");
                    tactic = $"- {TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_TARGETING_TITLE")}";
                }

                AbilityData abilityData = new AbilityData
                {
                    Name = new LocalizedTextBind(name + tactic, true),
                    LocalizedDescription = description,
                    Icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_SpecOp-1.png")
                };

                return abilityData;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static int GetAdjustedLevel(TacticalActor tacticalActor)
        {
            try
            {
                int startingLevel = tacticalActor.LevelProgression.Level;
                TFTVLogger.Always("Starting level is " + startingLevel);
                GameTagDef tierTagDef = GetFactionTierAndClassTags(tacticalActor.GameTags.ToList())[1];

                string rank = tierTagDef.name.Split('_')[1];
                int rankOrder = int.Parse(rank);
                TFTVLogger.Always("The rank is " + rankOrder);

                if (rankOrder == 1)
                {
                    if (startingLevel == 7)
                    {
                        return 7;
                    }
                    else
                    {
                        return 6;

                    }

                }
                else if (rankOrder == 2)
                {
                    if (startingLevel <= 5)
                    {
                        return 5;
                    }
                    else
                    {
                        return 6;

                    }
                }
                else if (rankOrder == 3)
                {
                    if (startingLevel <= 3)
                    {
                        return 3;
                    }
                    else
                    {
                        return 4;
                    }
                }
                else if (rankOrder == 4)
                {
                    if (startingLevel >= 2)
                    {
                        return 2;
                    }
                    else
                    {
                        return 1;
                    }
                }

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        private static int[] BuffFromClass(GameTagDef classTagDef, int difficulty)
        {
            try
            {
                ClassTagDef assaultTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                ClassTagDef berserkerTag = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");
                ClassTagDef heavyTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
                ClassTagDef infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");

                ClassTagDef sniperTag = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
                ClassTagDef priestTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
                ClassTagDef technicianTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");

                ClassTagDef assaultRaiderTag = TFTVScavengers._assaultRaiderTag;
                ClassTagDef heavyRaiderTag = TFTVScavengers._heavyRaiderTag;
                ClassTagDef sniperRaiderTag = TFTVScavengers._sniperRaiderTag;
                ClassTagDef scumTag = TFTVScavengers._scumTag;

                int[] stats = new int[3];

                if (classTagDef == assaultTag || classTagDef == assaultRaiderTag)
                {
                    stats[0] = 1;
                    stats[1] = 0;
                    stats[2] = 2;
                }
                else if (classTagDef == berserkerTag || classTagDef == infiltratorTag || classTagDef == scumTag)
                {
                    stats[0] = 0;
                    stats[1] = 0;
                    stats[2] = Mathf.FloorToInt(difficulty / 2);
                }
                else if (classTagDef == priestTag || classTagDef == technicianTag)
                {
                    stats[0] = 0;
                    stats[1] = difficulty;
                    stats[2] = 0;
                }
                else if (classTagDef == heavyTag || classTagDef == heavyRaiderTag)
                {
                    stats[0] = difficulty + 2;
                    stats[1] = 0;
                    stats[2] = 0;
                }

                return stats;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void AdjustStats(TacticalActor tacticalActor, GameTagDef classTagDef)
        {
            int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(tacticalActor.TacticalLevel.Difficulty.Order);

            try
            {
                int[] classBuff = BuffFromClass(classTagDef, difficultyLevel);

                int level = tacticalActor.LevelProgression.Level;

                //  TFTVLogger.Always($"ta: {tacticalActor?.DisplayName} level: {level} ");

                float[] equipmentBuff = new float[3];

                foreach (TacticalItem tacticalItem in tacticalActor.BodyState.GetArmourItems())
                {
                    equipmentBuff[0] += tacticalItem.BodyPartAspect.BodyPartAspectDef.Endurance;
                    equipmentBuff[1] += tacticalItem.BodyPartAspect.BodyPartAspectDef.WillPower;
                    equipmentBuff[2] += tacticalItem.BodyPartAspect.BodyPartAspectDef.Speed;
                }

                // TFTVLogger.Always($"{tacticalActor.name} has buffs from equipment {equipmentBuff[0]}, {equipmentBuff[1]}, {equipmentBuff[2]}");

                //11+4+14+6 MAX STR = 35 + armor
                tacticalActor.CharacterStats.Endurance.SetMax(11 + difficultyLevel + level * 2 + classBuff[0] + equipmentBuff[0]);
                tacticalActor.CharacterStats.Endurance.Set(11 + difficultyLevel + level * 2 + classBuff[0] + equipmentBuff[0]);
                tacticalActor.CharacterStats.Health.SetMax(120 + difficultyLevel * 10 + level * 2 * 10 + (classBuff[0] + equipmentBuff[0]) * 10);
                tacticalActor.CharacterStats.Health.SetToMax();


                //5+2+7+6+5 MAX WP = 25 + armor
                tacticalActor.CharacterStats.Willpower.SetMax(5 + Mathf.FloorToInt(difficultyLevel / 2) + level + GetStatBuffForTier(tacticalActor) / 2 + classBuff[1] + equipmentBuff[1]);
                tacticalActor.CharacterStats.Willpower.Set(5 + Mathf.FloorToInt(difficultyLevel / 2) + level + GetStatBuffForTier(tacticalActor) / 2 + classBuff[1] + equipmentBuff[1]);


                //14+3+4+4 MAX SPD = 25 + armor OLD
                //NEW

                //Rookie/Story Mode: 14 + 1 = 15 + armor
                //Veteran: 14 + 2 + 1 = 17 + armor
                //Hero: 14 + 3 + 1 = 18 + armor
                //Legend: 14 + 4 + 2 = 20 + arnor
                //ETERMES: 14 + 5 + 2 = 21 + armor
                tacticalActor.CharacterStats.Speed.SetMax(14 + GetStatBuffForTier(tacticalActor) / 3 + classBuff[2] + equipmentBuff[2]);
                tacticalActor.CharacterStats.Speed.Set(14 + GetStatBuffForTier(tacticalActor) / 3 + classBuff[2] + equipmentBuff[2]);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AdjustStatsAndSkills(TacticalActor tacticalActor)
        {
            int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(tacticalActor.TacticalLevel.Difficulty.Order);

            try
            {

                List<AbilityDef> assaultAbilities = new List<AbilityDef> { quickAim, killAndRun, onslaught, readyForAction, rapidClearance };
                List<AbilityDef> berserkerAbilities = new List<AbilityDef> { dash, cqc, bloodlust, ignorePain, adrenalineRush };
                List<AbilityDef> heavyAbilities = new List<AbilityDef> { returnFire, hunkerDown, skirmisher, shredResistant, rageBurst };
                List<AbilityDef> infiltratorAbilities = new List<AbilityDef> { surpriseAttack, decoy, neuralFeedback, jammingField, parasychosis };
                List<AbilityDef> priestAbilities = new List<AbilityDef> { mindControl, inducePanic, mindSense, psychicWard, mindCrush };
                List<AbilityDef> sniperAbilities = new List<AbilityDef> { extremeFocus, armorBreak, masterMarksman, inspire, markedForDeath };
                List<AbilityDef> technicianAbilities = new List<AbilityDef> { fastUse, electricReinforcement, stability, fieldMedic, amplifyPain };

                List<AbilityDef> allAbilities = new List<AbilityDef>();

                allAbilities.AddRange(assaultAbilities);
                allAbilities.AddRange(berserkerAbilities);
                allAbilities.AddRange(heavyAbilities);
                allAbilities.AddRange(infiltratorAbilities);
                allAbilities.AddRange(priestAbilities);
                allAbilities.AddRange(sniperAbilities);
                allAbilities.AddRange(technicianAbilities);

                List<AbilityDef> discardedAbilities = new List<AbilityDef>(allAbilities);
                //   discardedAbilities.Remove(quickAim);
                discardedAbilities.Remove(dash);
                discardedAbilities.Remove(cqc);
                discardedAbilities.Remove(bloodlust);
                discardedAbilities.Remove(ignorePain);
                discardedAbilities.Remove(returnFire);
                discardedAbilities.Remove(skirmisher);
                discardedAbilities.Remove(shredResistant);
                discardedAbilities.Remove(surpriseAttack);
                discardedAbilities.Remove(neuralFeedback);
                discardedAbilities.Remove(parasychosis);
                discardedAbilities.Remove(mindControl);
                discardedAbilities.Remove(mindSense);
                discardedAbilities.Remove(mindCrush);
                discardedAbilities.Remove(psychicWard);
                discardedAbilities.Remove(extremeFocus);
                discardedAbilities.Remove(masterMarksman);
                discardedAbilities.Remove(inspire);
                discardedAbilities.Remove(fastUse);
                discardedAbilities.Remove(electricReinforcement);
                discardedAbilities.Remove(stability);
                discardedAbilities.Remove(fieldMedic);

                if (tacticalActor.HasGameTag(TFTVScavengers._scumTag))
                {
                    FreezeAspectStatsStatus ignorePain = tacticalActor.Status.GetStatus<FreezeAspectStatsStatus>();

                    if (ignorePain != null)
                    {
                        TFTVLogger.Always($"{tacticalActor.name} has {ignorePain.Def.EffectName} status; removing it");
                        tacticalActor.Status.Statuses.Remove(ignorePain);
                    }

                    foreach (AbilityDef ability in allAbilities)
                    {
                        if (tacticalActor.GetAbilityWithDef<Ability>(ability) != null)
                        {
                            tacticalActor.RemoveAbility(ability);
                        }
                    }
                }
                else
                {
                    foreach (AbilityDef ability in discardedAbilities)
                    {
                        if (tacticalActor.GetAbilityWithDef<Ability>(ability) != null)
                        {
                            tacticalActor.RemoveAbility(ability);
                        }
                    }
                }

                int level = GetAdjustedLevel(tacticalActor);
                GameTagDef classTagDef = GetFactionTierAndClassTags(tacticalActor.GameTags.ToList())[2];

                AdjustStats(tacticalActor, classTagDef);


                if (classTagDef.name.Contains("Assault"))
                {
                    if (tacticalActor.GetAbilityWithDef<Ability>(quickAim) != null)
                    {
                        tacticalActor.RemoveAbility(quickAim);
                        tacticalActor.UpdateStats();
                    }
                    // }
                }


                if (classTagDef.name.Contains("Berserker"))
                {
                    if (level >= 6)
                    {
                        tacticalActor.AddAbility(dash, tacticalActor);
                        tacticalActor.AddAbility(cqc, tacticalActor);
                        tacticalActor.AddAbility(bloodlust, tacticalActor);
                        tacticalActor.AddAbility(ignorePain, tacticalActor);
                    }
                    else if (level == 5)
                    {
                        tacticalActor.AddAbility(dash, tacticalActor);
                        tacticalActor.AddAbility(cqc, tacticalActor);
                        tacticalActor.AddAbility(bloodlust, tacticalActor);
                    }
                    else if (level >= 3)
                    {
                        tacticalActor.AddAbility(dash, tacticalActor);
                        tacticalActor.AddAbility(cqc, tacticalActor);
                    }
                    else if (level == 2)
                    {
                        tacticalActor.AddAbility(dash, tacticalActor);
                    }
                }
                if (classTagDef.name.Contains("Heavy"))
                {
                    if (level >= 6)
                    {
                        tacticalActor.AddAbility(returnFire, tacticalActor);
                        tacticalActor.AddAbility(shredResistant, tacticalActor);
                        tacticalActor.AddAbility(skirmisher, tacticalActor);
                    }
                    else if (level == 5)
                    {
                        tacticalActor.AddAbility(returnFire, tacticalActor);
                        tacticalActor.AddAbility(skirmisher, tacticalActor);

                    }
                    else if (level >= 2)
                    {
                        tacticalActor.AddAbility(returnFire, tacticalActor);
                    }
                }
                if (classTagDef.name.Contains("Infiltrator"))
                {
                    if (level >= 6)
                    {
                        tacticalActor.AddAbility(jammingField, tacticalActor);
                        tacticalActor.AddAbility(neuralFeedback, tacticalActor);
                        tacticalActor.AddAbility(surpriseAttack, tacticalActor);
                    }
                    else if (level >= 5)
                    {
                        tacticalActor.AddAbility(neuralFeedback, tacticalActor);
                        tacticalActor.AddAbility(surpriseAttack, tacticalActor);

                    }
                    else if (level >= 2)
                    {
                        tacticalActor.AddAbility(surpriseAttack, tacticalActor);
                    }
                }
                if (classTagDef.name.Contains("Priest"))
                {
                    if (level == 7)
                    {
                        tacticalActor.AddAbility(mindCrush, tacticalActor);
                        tacticalActor.AddAbility(psychicWard, tacticalActor);
                        tacticalActor.AddAbility(mindSense, tacticalActor);
                        tacticalActor.AddAbility(mindControl, tacticalActor);

                    }
                    else if (level == 6)
                    {
                        tacticalActor.AddAbility(psychicWard, tacticalActor);
                        tacticalActor.AddAbility(mindSense, tacticalActor);
                        tacticalActor.AddAbility(mindControl, tacticalActor);

                    }
                    else if (level == 5)
                    {
                        tacticalActor.AddAbility(mindSense, tacticalActor);
                        tacticalActor.AddAbility(mindControl, tacticalActor);
                    }
                    else if (level >= 2)
                    {
                        tacticalActor.AddAbility(mindControl, tacticalActor);
                    }
                }

                if (classTagDef.name.Contains("Sniper"))
                {
                    if (level >= 6)
                    {
                        tacticalActor.AddAbility(inspire, tacticalActor);
                        tacticalActor.AddAbility(masterMarksman, tacticalActor);
                        tacticalActor.AddAbility(extremeFocus, tacticalActor);
                    }
                    else if (level == 5)
                    {
                        tacticalActor.AddAbility(masterMarksman, tacticalActor);
                        tacticalActor.AddAbility(extremeFocus, tacticalActor);

                    }
                    else if (level >= 2)
                    {
                        tacticalActor.AddAbility(extremeFocus, tacticalActor);
                    }
                }
                if (classTagDef.name.Contains("Technician"))
                {
                    if (level >= 6)
                    {
                        tacticalActor.AddAbility(electricReinforcement, tacticalActor);
                        tacticalActor.AddAbility(fastUse, tacticalActor);
                        tacticalActor.AddAbility(stability, tacticalActor);
                        tacticalActor.AddAbility(fieldMedic, tacticalActor);
                    }
                    else if (level == 5)
                    {
                        tacticalActor.AddAbility(electricReinforcement, tacticalActor);
                        tacticalActor.AddAbility(fastUse, tacticalActor);
                        tacticalActor.AddAbility(stability, tacticalActor);
                    }
                    else if (level >= 3)
                    {
                        tacticalActor.AddAbility(electricReinforcement, tacticalActor);
                        tacticalActor.AddAbility(fastUse, tacticalActor);
                    }
                    else if (level == 2)
                    {
                        tacticalActor.AddAbility(fastUse, tacticalActor);
                    }
                }

                tacticalActor.UpdateStats();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static int GetStatBuffForTier(TacticalActor tacticalActor)
        {
            int difficultyLevel = TFTVSpecialDifficulties.DifficultyOrderConverter(tacticalActor.TacticalLevel.Difficulty.Order);

            try
            {
                GameTagDef tierTagDef = GetFactionTierAndClassTags(tacticalActor.GameTags.ToList())[1];
                string rank = tierTagDef.name.Split('_')[1];
                int rankOrder = int.Parse(rank);

                int buff = (4 - rankOrder) * (difficultyLevel);

                return buff;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void GiveRankAndNameToHumaoidEnemy(TacticalActorBase actor, TacticalLevelController __instance)
        {
            try
            {
                if (HumanEnemiesAndTactics != null && actor.BaseDef.name == "Soldier_ActorDef" && actor.InPlay
                    && __instance.CurrentFaction != __instance.GetFactionByCommandName("PX"))
                {
                    TFTVLogger.Always("The turn number is " + __instance.TurnNumber);
                    if (GetHumanEnemyFactions(__instance) != null)
                    {
                        foreach (TacticalFaction faction in GetHumanEnemyFactions(__instance))
                        {
                            string nameOfFaction = faction.Faction.FactionDef.ShortName;
                            GameTagDef gameTagDef = DefCache.GetDef<GameTagDef>("HumanEnemyFaction_" + nameOfFaction + "_GameTagDef");


                            if (faction.Actors.Contains(actor))
                            {
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                int rankNumber = UnityEngine.Random.Range(1, 7);
                                GameTagDef rank = HumanEnemyTier4GameTag;
                                GenderTagDef genderTagDef = actor.GameTags.FirstOrDefault(x => x is GenderTagDef) as GenderTagDef;

                                if (rankNumber == 6)
                                {
                                    rank = HumanEnemyTier2GameTag;
                                    TFTVLogger.Always(actor.name + " is now a champ");
                                }
                                else if (rankNumber >= 4 && rankNumber < 6)
                                {
                                    rank = HumanEnemyTier3GameTag;
                                    TFTVLogger.Always(actor.name + " is now a ganger");

                                }

                                actor.GameTags.Add(rank);
                                actor.GameTags.Add(humanEnemyTagDef, GameTagAddMode.ReplaceExistingExclusive);
                                actor.GameTags.Add(gameTagDef);
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                actor.name = TFTVHumanEnemiesNames.GetName(nameOfFaction, rank, genderTagDef);
                                TFTVLogger.Always("Name of new enemy is " + actor.name);
                                TacticalActor tacticalActor = actor as TacticalActor;
                                AdjustStatsAndSkills(tacticalActor);

                                ActorClassIconElement actorClassIconElement = actor.TacticalActorViewBase.UIActorElement.GetComponent<HealthbarUIActorElement>().ActorClassIconElement;
                                TFTVUITactical.Enemies.ChangeHealthBarIcon(actorClassIconElement, actor);

                            }

                        }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChampRecoverWPAura(TacticalLevelController controller)
        {
            try
            {
                List<TacticalFaction> enemyHumanFactions = GetHumanEnemyFactions(controller);
                if (enemyHumanFactions.Count != 0)
                {
                    foreach (TacticalFaction faction in enemyHumanFactions)
                    {
                        foreach (TacticalActor tacticalActor in faction.TacticalActors)
                        {

                            if (tacticalActor.HasGameTag(HumanEnemyTier2GameTag) && tacticalActor.IsAlive && !tacticalActor.IsEvacuated)
                            {
                                foreach (TacticalActor allyTacticalActor in faction.TacticalActors)
                                {
                                    if (allyTacticalActor.BaseDef.name == "Soldier_ActorDef" && allyTacticalActor.InPlay)
                                    {
                                        float magnitude = allyTacticalActor.GetAdjustedPerceptionValue();

                                        if ((allyTacticalActor.Pos - tacticalActor.Pos).magnitude < magnitude
                                            && TacticalFactionVision.CheckVisibleLineBetweenActors(allyTacticalActor, allyTacticalActor.Pos, tacticalActor, true))
                                        {
                                            // TFTVLogger.Always("Actor in range and has LoS");
                                            allyTacticalActor.CharacterStats.WillPoints.AddRestrictedToMax(1);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ApplyTacticStartOfPlayerTurn(TacticalLevelController controller)
        {
            try
            {
                if (controller.CurrentFaction != controller.GetFactionByCommandName("Px"))
                {
                    return;
                }

                if (HumanEnemiesAndTactics.Keys.Count > 0)
                {
                    TFTVUITactical.Enemies.ActivateOrAdjustLeaderWidgets();
                }

                foreach (string faction in HumanEnemiesAndTactics.Keys)
                {

                    if (HumanEnemiesAndTactics.GetValueSafe(faction) == 4)
                    {
                        TFTVLogger.Always("Applying tactic Optical Shield");
                        OpticalShield(controller, faction);
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 3)
                    {
                        if (HumanEnemiesAndTactics.ContainsKey("pu") || HumanEnemiesAndTactics.ContainsKey("Purists"))
                        {

                        }
                        else
                        {
                            TFTVLogger.Always("Applying tactic Experimental Drugs");
                            ExperimentalDrugs(controller, faction);
                        }
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 5)
                    {
                        TFTVLogger.Always("Applying tactic Fire Discipline");
                        FireDiscipline(controller, faction);
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ApplyTactic(TacticalLevelController controller)
        {
            try
            {
                foreach (string faction in HumanEnemiesAndTactics.Keys)
                {
                    if (HumanEnemiesAndTactics.GetValueSafe(faction) == 1)
                    {
                        TFTVLogger.Always("Applying tactic Terrifying Aura");
                        TerrifyingAura(controller, faction);
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 2)
                    {
                        TFTVLogger.Always("Tactic Starting Volley");
                        // StartingVolley(controller);
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 3)
                    {
                        if (HumanEnemiesAndTactics.ContainsKey("pu") || HumanEnemiesAndTactics.ContainsKey("Purists"))
                        {
                            TFTVLogger.Always("Applying tactic Pure self-repair");
                            PureSelfRepairAbility();
                        }
                        else
                        {
                            TFTVLogger.Always("Applying tactic Experimental Drugs");
                            ExperimentalDrugs(controller, faction);
                        }
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 5)
                    {
                        TFTVLogger.Always("Applying tactic Fire Discipline");
                        FireDiscipline(controller, faction);
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 6)
                    {
                        TFTVLogger.Always("Tactic Blood Frenzy");
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 7)
                    {
                        TFTVLogger.Always("Tactic Retribution");
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 8)
                    {
                        TFTVLogger.Always("Applying tactic Ambush");
                        Ambush(controller, faction);
                    }
                    else if (HumanEnemiesAndTactics.GetValueSafe(faction) == 9)
                    {
                        TFTVLogger.Always("Applying tactic Assisted Targeting");
                        AssistedTargeting(controller, faction);
                    }
                }

                if (HumanEnemiesAndTactics.Keys.Count > 0)
                {
                    TFTVUITactical.Enemies.ActivateOrAdjustLeaderWidgets();
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Special faction tactics

        internal static void EmmissariesOfTheVoid()
        {
            try
            {
                //Give Foresaken Umbra

                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                TacticalFaction tacticalFaction = controller.GetFactionByCommandName("pu");
                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.IsAlive).Where(ta => ta.GetAbilityWithDef<PassiveModifierAbility>(SelfRepairAbility) == null))
                {
                    tacticalActor.AddAbility(SelfRepairAbility, tacticalActor);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void RapidReactionForce()
        {
            try
            {
                //NJ reinforcements if leader has arm disabled but alive at start of turn


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void BeastMaster()
        {
            try
            {
                //Spawns ANU Priest + Mutog 


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        internal static void AssassinationTeam()
        {
            try
            {
                //Spawns SY infiltator reinforcements at player spawn locations


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }







        private static readonly PassiveModifierAbilityDef SelfRepairAbility = DefCache.GetDef<PassiveModifierAbilityDef>("RoboticSelfRepair_AbilityDef");
        //  private static readonly DamageMultiplierStatusDef RoboticSelfRepairStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RoboticSelfRepair_AddAbilityStatusDef");

        internal static void PureSelfRepairAbility()
        {
            try
            {
                if (HumanEnemiesAndTactics.Count > 0 && (HumanEnemiesAndTactics.ContainsKey("pu") || HumanEnemiesAndTactics.ContainsKey("Purists")) && HumanEnemiesAndTactics.ContainsValue(3))
                {

                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                    TacticalFaction tacticalFaction = controller.GetFactionByCommandName("pu");
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.IsAlive && ta.HasGameTag(HumanEnemyTier2GameTag)).Where(ta => ta.GetAbilityWithDef<PassiveModifierAbility>(SelfRepairAbility) == null))
                    {
                        tacticalActor.AddAbility(SelfRepairAbility, tacticalActor);
                    }
                    TFTVAncients.AncientsNewTurn.CheckRoboticSelfRepairStatus(tacticalFaction);
                    TFTVAncients.AncientsNewTurn.ApplyRoboticSelfHealingStatus(tacticalFaction);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void TerrifyingAura(TacticalLevelController controller, string factionName)
        {
            try
            {
                TacticalActor leader = GetLeader(controller, factionName);

                if (leader == null)
                {
                    TFTVLogger.Always($"no leader for {factionName}! must be dead, Terrifying Aura will not be applied");
                    return;
                }


                foreach (TacticalActorBase enemy in leader.TacticalFaction.GetAllAliveEnemyActors<TacticalActorBase>(false))
                {
                    if (enemy.BaseDef.name == "Soldier_ActorDef" && enemy is TacticalActor actor)
                    {

                        float magnitude = actor.GetAdjustedPerceptionValue();

                        if ((actor.Pos - leader.Pos).magnitude < magnitude
                            && TacticalFactionVision.CheckVisibleLineBetweenActors(actor, actor.Pos, leader, true))
                        {
                            TFTVLogger.Always($"{actor.GetDisplayName()} is within perception range and has LoS on {leader.name}");
                            actor.CharacterStats?.WillPoints.Subtract(2);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ExperimentalDrugs(TacticalLevelController controller, string factionName)
        {
            try
            {
                List<TacticalFaction> enemyHumanFactions = GetHumanEnemyFactions(controller);
                if (enemyHumanFactions.Count != 0)
                {
                    foreach (TacticalFaction faction in enemyHumanFactions)
                    {
                        if (faction.Faction.FactionDef.ShortName == factionName)
                        {
                            foreach (TacticalActor tacticalActor in faction.TacticalActors)
                            {
                                if (tacticalActor.HasGameTag(HumanEnemyTier4GameTag))
                                {

                                    if (tacticalActor.GetAbilityWithDef<Ability>(regeneration) == null
                                        && !tacticalActor.HasStatus(regenerationStatus))
                                    {

                                        TFTVLogger.Always($"{tacticalActor.name} is getting the experimental drugs");
                                        tacticalActor.AddAbility(regeneration, tacticalActor);
                                        tacticalActor.Status.ApplyStatus(regenerationStatus);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void StartingVolley(TacticalLevelController controller, string factionName)
        {
            try
            {
                TacticalActor leader = GetLeader(controller, factionName);

                if (leader == null)
                {
                    TFTVLogger.Always($"No leader for {factionName} found! must be dead, Starting Volley will not apply.");
                    return;
                }


                foreach (TacticalActor tacticalActor in leader.TacticalFaction.TacticalActors)
                {
                    if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay && tacticalActor.IsAlive)
                    {
                        TFTVArtOfCrab.GetBestWeaponForQA(tacticalActor);

                        if (tacticalActor.Equipments.SelectedWeapon == null)
                        {
                            continue;
                        }

                        float SelectedWeaponRange = tacticalActor.Equipments.SelectedWeapon.WeaponDef.EffectiveRange;
                        TFTVLogger.Always($"{tacticalActor.name} selected weapon is {tacticalActor.Equipments.SelectedWeapon.DisplayName} and its maximum range is {tacticalActor.Equipments.SelectedWeapon.WeaponDef.EffectiveRange}");

                        /*  foreach (TacticalActorBase enemy in leader.TacticalFaction.GetAllAliveEnemyActors<TacticalActorBase>(false))
                          {
                              if ((enemy.Pos - tacticalActor.Pos).magnitude < SelectedWeaponRange / 2
                              && TacticalFactionVision.CheckVisibleLineBetweenActors(enemy, enemy.Pos, tacticalActor, true)
                              && !tacticalActor.Status.HasStatus(startingVolleyStatus))
                              {
                                 */ //TFTVLogger.Always($"{tacticalActor.name} is getting quick aim status because close enough to {enemy.name}");

                        tacticalActor.Status.ApplyStatus(_startingVolleyStatusTactic);


                        //    }
                        //  }
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static TacticalActor GetLeader(TacticalLevelController controller, string factionName)
        {
            try
            {
                return controller.Factions.FirstOrDefault(f => f.Faction.FactionDef.ShortNames.Contains(factionName)).TacticalActors.FirstOrDefault(ta => ta.HasGameTag(HumanEnemyTier1GameTag) && ta.IsAlive && !ta.IsEvacuated);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static TacticalActor GetChampion(TacticalLevelController controller, string factionName)
        {
            try
            {
                return controller.Factions.FirstOrDefault(f => f.Faction.FactionDef.ShortNames.Contains(factionName)).TacticalActors.FirstOrDefault(ta => ta.HasGameTag(HumanEnemyTier2GameTag) && ta.IsAlive && !ta.IsEvacuated);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        public static void OpticalShield(TacticalLevelController controller, string factionName)
        {
            try
            {
                TacticalActor leader = GetLeader(controller, factionName);

                if (leader == null)
                {
                    TFTVLogger.Always($"No leader for {factionName} found! must be dead, Optical Shield will not apply.");
                    return;
                }

                foreach (TacticalActor tacticalActor in leader.TacticalFaction.TacticalActors)
                {
                    if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay && leader != tacticalActor)
                    {
                        float magnitude = 16;

                        if ((tacticalActor.Pos - leader.Pos).magnitude < magnitude)
                        {
                            /* RepositionAbilityDef painC = DefCache.GetDef<RepositionAbilityDef>("PainChameleon_AbilityDef");

                             tacticalActor.AddAbility(painC, tacticalActor);
                             tacticalActor.GetAbilityWithDef<RepositionAbility>(painC).Activate();
                             tacticalActor.RemoveAbility(painC);*/
                            TFTVLogger.Always($"{tacticalActor.name} in range of {leader.name} acquiring optical shield");
                            tacticalActor.ApplyDamage(new DamageResult { ActorEffects = new List<EffectDef>() { _opticalShieldMultiStatusDef } });

                            //  tacticalActor.Status.ApplyStatus((StanceStatusDef)Repo.GetDef("8dbf3262-686d-2fb2-91cc-47014c539d95"));
                            //  TacticalFactionVision.ForgetForAll(tacticalActor, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AssistedTargeting(TacticalLevelController controller, string factionName)
        {
            try
            {
                StatModification accuracyBuff1 = new StatModification() { Modification = StatModificationType.Add, Value = 0.15f, StatName = "Accuracy" };
                StatModification accuracyBuff2 = new StatModification() { Modification = StatModificationType.Add, Value = 0.3f, StatName = "Accuracy" };
                StatModification accuracyBuff3 = new StatModification() { Modification = StatModificationType.Add, Value = 0.45f, StatName = "Accuracy" };
                StatModification accuracyBuff4 = new StatModification() { Modification = StatModificationType.Add, Value = 0.6f, StatName = "Accuracy" };

                List<StatModification> accuracyBuffs = new List<StatModification>()
                {
                accuracyBuff1, accuracyBuff2, accuracyBuff3, accuracyBuff4

                };

                foreach (TacticalActor tacticalActor in controller.Factions.FirstOrDefault(f => f.Faction.FactionDef.ShortNames.Contains(factionName)).TacticalActors)
                {
                    if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay)
                    {
                        foreach (StatModification accuracyBuff in accuracyBuffs)
                        {
                            if (tacticalActor.CharacterStats.Accuracy.Modifications.Contains(accuracyBuff))
                            {
                                tacticalActor.CharacterStats.Accuracy.RemoveStatModification(accuracyBuff);
                            }
                        }
                    }
                }

                TacticalActor leader = GetLeader(controller, factionName);

                if (leader == null)
                {
                    TFTVLogger.Always($"No leader for {factionName} found! must be dead, Assisted Targeting will not apply.");
                    return;
                }

                foreach (TacticalActor tacticalActor in leader.TacticalFaction.TacticalActors)
                {
                    float magnitude = 12;
                    int numberOAssists = 0;

                    foreach (TacticalActor assist in leader.TacticalFaction.TacticalActors)
                    {
                        if ((tacticalActor.Pos - assist.Pos).magnitude <= magnitude
                            && tacticalActor != assist)
                        {
                            numberOAssists++;
                        }
                    }


                    if (numberOAssists > 0)
                    {
                        int pos = Math.Min(3, numberOAssists - 1);
                        tacticalActor.CharacterStats.Accuracy.AddStatModification(accuracyBuffs[pos]);
                        TFTVLogger.Always($"{tacticalActor.name} has {numberOAssists} assists, so adding {accuracyBuffs[pos]} accuracy");
                    }
                    ;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void FireDiscipline(TacticalLevelController controller, string factionName)
        {
            try
            {
                TacticalActor leader = GetLeader(controller, factionName);

                ReturnFireAbilityDef returnFireFireDiscipline = _fireDisciplineTactic;

                foreach (TacticalFaction tacticalFaction in controller.Factions)
                {
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                    {
                        if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay)
                        {
                            if (tacticalActor.GetAbilityWithDef<Ability>(returnFireFireDiscipline) != null)
                            {
                                tacticalActor.RemoveAbility(returnFireFireDiscipline);
                                TFTVLogger.Always($"Removing Fire Discipline RF from {tacticalActor.name}");
                            }
                        }
                    }
                }

                if (leader == null)
                {
                    TFTVLogger.Always($"No leader for {factionName} found! must be dead, Fire Discipline will not apply.");
                    return;
                }

                foreach (TacticalActor tacticalActor in leader.TacticalFaction.TacticalActors)
                {
                    if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay
                        && tacticalActor.GetAbilityWithDef<Ability>(returnFire) == null && tacticalActor.GetAbilityWithDef<Ability>(returnFireFireDiscipline) == null && tacticalActor != leader)
                    {
                        float magnitude = 20;

                        if ((tacticalActor.Pos - leader.Pos).magnitude < magnitude)
                        {
                            TFTVLogger.Always($"{tacticalActor.name} is in range of {leader.name}, adding return fire");
                            tacticalActor.AddAbility(returnFireFireDiscipline, tacticalActor);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void Ambush(TacticalLevelController controller, string factionName)
        {
            try
            {
                foreach (TacticalActor tacticalActor in controller.Factions.FirstOrDefault(f => f.Faction.FactionDef.ShortNames.Contains(factionName)).TacticalActors)
                {
                    if (tacticalActor.GetAbilityWithDef<Ability>(_ambushTactic) != null)
                    {
                        TFTVLogger.Always($"Removing Ambush ability from {tacticalActor.name}");
                        tacticalActor.RemoveAbility(_ambushTactic);
                    }
                }

                TacticalActor leader = GetLeader(controller, factionName);

                if (leader == null)
                {
                    TFTVLogger.Always($"No leader for {factionName} found! must be dead, Ambush will not apply.");
                    return;
                }

                List<TacticalActorBase> enemies = new List<TacticalActorBase>(leader.TacticalFaction.GetAllAliveEnemyActors<TacticalActorBase>(false).ToList());

                foreach (TacticalActorBase tacticalActor in leader.TacticalFaction.TacticalActors)
                {
                    if (tacticalActor.BaseDef.name == "Soldier_ActorDef" && tacticalActor.InPlay)
                    {
                        float magnitude = 10;

                        if (enemies.Any(e => (tacticalActor.Pos - e.Pos).magnitude < magnitude))
                        {
                        }
                        else
                        {
                            TFTVLogger.Always($"{tacticalActor.name} receiving the ambush ability");
                            tacticalActor.AddAbility(_ambushTactic, tacticalActor);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void HumanEnemiesTacticsOnDeath(DeathReport deathReport)
        {
            try
            {
                if (HumanEnemiesAndTactics.ContainsKey(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName)
                    && HumanEnemiesAndTactics.GetValueSafe(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName) == 6)
                {


                    if (deathReport.Actor.HasGameTag(HumanEnemyTier2GameTag) || deathReport.Actor.HasGameTag(HumanEnemyTier1GameTag))
                    {
                        foreach (TacticalActorBase allyTacticalActorBase in deathReport.Actor.TacticalFaction.Actors)
                        {
                            TacticalActor tacticalActor = allyTacticalActorBase as TacticalActor;

                            if (allyTacticalActorBase.BaseDef.name == "Soldier_ActorDef" && allyTacticalActorBase.InPlay
                                && !tacticalActor.HasStatus(frenzy))
                            {
                                allyTacticalActorBase.Status.ApplyStatus(frenzy);

                            }
                        }
                    }

                }
                if (HumanEnemiesAndTactics.ContainsKey(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName)
                   && HumanEnemiesAndTactics.GetValueSafe(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName) == 4)
                {

                    if (deathReport.Actor.HasGameTag(HumanEnemyTier1GameTag))
                    {
                        TacStatusEffectDef statusEffectDef = (TacStatusEffectDef)_opticalShieldMultiStatusDef.EffectDefs[0];
                        StanceStatusDef opticalShieldStealth = (StanceStatusDef)statusEffectDef.StatusDef;
                        StanceStatusDef stealthStatus = DefCache.GetDef<StanceStatusDef>("Stealth_StatusDef");

                        foreach (TacticalActorBase allyTacticalActorBase in deathReport.Actor.TacticalFaction.Actors)
                        {
                            TacticalActor tacticalActor = allyTacticalActorBase as TacticalActor;


                            if (allyTacticalActorBase.BaseDef.name == "Soldier_ActorDef" && allyTacticalActorBase.InPlay
                                && tacticalActor.HasStatus(opticalShieldStealth))
                            {
                                TFTVLogger.Always($"Unapply optical shield status from {allyTacticalActorBase.name} because leader is dead!");
                                allyTacticalActorBase.Status.UnapplyStatus(allyTacticalActorBase.Status.GetStatusByName(opticalShieldStealth.EffectName));

                                if (tacticalActor.HasStatus(stealthStatus))
                                {
                                    tacticalActor.Status.UnapplyStatus(allyTacticalActorBase.Status.GetStatusByName(stealthStatus.EffectName));

                                }

                                /*  foreach(TacticalActorBase tacticalActorBase in deathReport.Actor.TacticalFaction.GetAllAliveEnemyActors<TacticalActorBase>(false)) 
                                  { 
                                  if(tacticalActorBase is TacticalActor actor) 
                                      { 

                                      }

                                  }*/
                            }
                        }
                    }

                }
                if (HumanEnemiesAndTactics.ContainsKey(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName)
                   && HumanEnemiesAndTactics.GetValueSafe(deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName) == 5)
                {
                    FireDiscipline(deathReport.Actor.TacticalLevel, deathReport.Actor.TacticalFaction.Faction.FactionDef.ShortName);
                }

                if (HumanEnemiesAndTactics.Keys.Count > 0)
                {
                    TFTVUITactical.Enemies.ActivateOrAdjustLeaderWidgets();
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void HumanEnemiesRetributionTacticCheckOnActorDamageDealt(TacticalActor actor, IDamageDealer damageDealer)
        {
            try
            {
                try
                {

                    if ((actor.HasGameTag(HumanEnemyTier1GameTag) || actor.HasGameTag(HumanEnemyTier2GameTag)) && damageDealer != null && HumanEnemiesAndTactics.ContainsKey(actor.TacticalFaction.Faction.FactionDef.ShortName)
                    && HumanEnemiesAndTactics.GetValueSafe(actor.TacticalFaction.Faction.FactionDef.ShortName) == 7)
                    {
                        TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                        TacticalActor attacker = attackerBase as TacticalActor;

                        if (!attacker.Status.HasStatus(mFDStatus) && actor.TacticalFaction != attacker.TacticalFaction)
                        {
                            attacker.Status.ApplyStatus(mFDStatus);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ImplementStartingVolleyHumanEnemiesTactic(TacticalFaction tacticalFaction)
        {
            try
            {
                if (HumanEnemiesAndTactics.ContainsKey(tacticalFaction.Faction.FactionDef.ShortName)
                    && HumanEnemiesAndTactics.GetValueSafe(tacticalFaction.Faction.FactionDef.ShortName) == 2
                    && tacticalFaction.TacticalLevel.TurnNumber > 0)
                {
                    StartingVolley(tacticalFaction.TacticalLevel, tacticalFaction.Faction.FactionDef.ShortName);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


    }
}
