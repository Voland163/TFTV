using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.PathProcessors;
using System.Linq;
using TFTV;
using UnityEngine;
using static PhoenixPoint.Tactical.Levels.PathProcessors.ClimbPathProcessor;

namespace PRMBetterClasses.SkillModifications
{
    internal class BackgroundPerks
    {
        // Get config, definition repository and shared data
        //private static readonly Settings Config = TFTVMain.Main.Config;
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //private static readonly SharedData Shared = BetterClassesMain.Shared;

        //private static readonly bool doNotLocalize = BetterClassesMain.doNotLocalize;

        public static void ApplyChanges()
        {
            // Survivor, WP +4, but - 4 STR
            Create_Survivor();
            // Nurse, WP + 2, Healing + 30 %, ex Healer
            Change_Nurse();
            // Scav, WP + 2, Perception + 4
            Change_Scav();
            // CorpseDisposer, Fire Resistance, STR + 2
            Create_CorpseDisposer();
            // HardLabor, STR +4, Perception - 10
            Change_HardLabor();
            // SQUATTER, STR + 2, Carry Weight +25 %
            Change_Squatter();
            // VOLUNTEERED, Nightvision, +2 SPD, -2WP
            Create_Volunteered();
            // CondoRaider, "Jump", +1 SPD
            Create_CondoRaider();
            // TunnelRat, Stealth + 15 %, SPD + 1
            Change_TunnelRat();
            // DESK JOCKEY, Accuracy + 10 %
            Create_Hunter();
            // Troublemaker, Grenades deal +10 % damage
            Create_Troublemaker();
            // Attentive, Perception +4, Hearing Range +10
            Create_Paranoid();
            // TRUE GRIT, Melee Resistance 10 %
            Create_TrueGrit();
            // Privileged	SPD +2, Perception +4, Carry Weight -25%	You were riding out the Apocalypse in First Class and letting others carry your load for you. It didn't last, and the run for your life that followed gave you a new awareness.
            Create_Privileged();
            // A HISTORY OF VIOLENCE, Damage + 10 %, Willpower - 3
            Create_AHistoryOfViolence();
            // DAREDEVIL, Damage +10 %, Accuracy - 10 %
            Create_Daredevil();
            // Psychic, Gain "Psychic Scream"
            Create_DamagedAmygdala();
            // SANITATION EXPERT, Immune to goo
            Create_SanitationExpert();
            // LAB ASSISTANT, Acid Resistance
            Create_LabAssistant();
            // ROCKETEER, Jetpack Proficiency
            Create_Rockteer();
        }
        private static void Create_Survivor()
        {
            string skillName = "Survivor_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
            PassiveModifierAbilityDef Survivor = Helper.CreateDefFromClone(
                source,
                "8e907b1f-f94e-4047-b27a-4de7022868b9",
                skillName);
            Survivor.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "90919b90-e7a6-47fc-9bd1-609e254f53eb",
                skillName);
            Survivor.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "160dfb40-c5cf-414d-a04f-5ba23bcd761b",
                skillName);

            // Set necessary fields
            float strength = -2.0f;
            float willpower = 4.0f;
            Survivor.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.Add,
                    Value = strength
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Endurance,
                    Modification = StatModificationType.AddMax,
                    Value = strength
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = willpower
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = willpower
                },
                //new ItemStatModification()
                //{
                //    TargetStat = StatModificationTarget.WillPoints,
                //    Modification = StatModificationType.Add,
                //    Value = willpower
                //},
                //new ItemStatModification()
                //{
                //    TargetStat = StatModificationTarget.WillPoints,
                //    Modification = StatModificationType.AddMax,
                //    Value = willpower
                //}
            };
            Survivor.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            Survivor.DamageKeywordPairs = new DamageKeywordPair[0];
            Survivor.CharacterProgressionData.RequiredSpeed = 0;
            Survivor.CharacterProgressionData.RequiredStrength = 0;
            Survivor.CharacterProgressionData.RequiredWill = 0;
            Survivor.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SURVIVOR"; //new LocalizedTextBind("SURVIVOR", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Survivor.ViewElementDef.Description.LocalizationKey = "PR_BC_SURVIVOR_DESC"; // = new LocalizedTextBind("<b>-4 Strength, +4 Willpower</b>\n<i>You have had it tougher than most, and that's saying a lot. Your body took a toll, but your mind grew stronger.</i>", doNotLocalize);
            Sprite SurvivorIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Survivor_3-1.png");
            Survivor.ViewElementDef.LargeIcon = SurvivorIcon;
            Survivor.ViewElementDef.SmallIcon = SurvivorIcon;
        }
        private static void Change_Nurse()
        {
            PassiveModifierAbilityDef Nurse = DefCache.GetDef<PassiveModifierAbilityDef>("Helpful_AbilityDef");
            for (int i = 0; i < Nurse.StatModifications.Length; i++)
            {
                if (Nurse.StatModifications[i].TargetStat == StatModificationTarget.BonusHealValue)
                {
                    Nurse.StatModifications[i].Value = 1.3f;
                }
                if (Nurse.StatModifications[i].TargetStat == StatModificationTarget.WillPoints
                    || Nurse.StatModifications[i].TargetStat == StatModificationTarget.Willpower)
                {
                    Nurse.StatModifications[i].Value = 2.0f;
                }
            }
            Nurse.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_NURSE";
            Nurse.ViewElementDef.Description.LocalizationKey = "PR_BC_NURSE_DESC";
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_ExpertHealer-2.png");
            Nurse.ViewElementDef.LargeIcon = icon;
            Nurse.ViewElementDef.SmallIcon = icon;
        }
        private static void Change_Scav()
        {
            PassiveModifierAbilityDef scav = DefCache.GetDef<PassiveModifierAbilityDef>("Brainiac_AbilityDef");
            for (int i = 0; i < scav.StatModifications.Length; i++)
            {
                if (scav.StatModifications[i].TargetStat == StatModificationTarget.Perception)
                {
                    scav.StatModifications[i].Value = 4.0f;
                }
            }
            scav.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SCAV"; //new LocalizedTextBind("SCAV", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            scav.ViewElementDef.Description.LocalizationKey = "PR_BC_SCAV_DESC"; //new LocalizedTextBind("<b>+2 Willpower, +4 Perception</b>\n<i>The Old World left behind all sorts of interesting things. It takes patience and a keen eye to find them.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        private static void Create_CorpseDisposer()
        {
            string skillName = "CorpseDisposer_AbilityDef";
            DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("FireResistant_DamageMultiplierAbilityDef");
            DamageMultiplierAbilityDef CorpseDisposer = Helper.CreateDefFromClone(
                source,
                "8647a3e3-1fb0-44ca-9d6d-352613068070",
                skillName);
            CorpseDisposer.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "7a96d706-9771-406b-bbfa-4705eaf3cb1c",
                skillName);
            CorpseDisposer.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "b77223a3-c109-4a3e-8b59-a7d028f181f2",
                skillName);
            // Set necessary fields
            // TODO: Finding a way to add strength modifier, the below don't work, the base ability has no StatModifications
            //CorpseDisposer.StatModifications = new ItemStatModification[]
            //  {
            //    new ItemStatModification()
            //    {
            //        TargetStat = StatModificationTarget.Endurance,
            //        Modification = StatModificationType.Add,
            //        Value = 2
            //    },
            //  };
            //CorpseDisposer.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            //CorpseDisposer.DamageKeywordPairs = new DamageKeywordPair[0];
            CorpseDisposer.CharacterProgressionData.RequiredSpeed = 0;
            CorpseDisposer.CharacterProgressionData.RequiredStrength = 0;
            CorpseDisposer.CharacterProgressionData.RequiredWill = 0;
            CorpseDisposer.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_CORPSE_DISPOSER"; //new LocalizedTextBind("CORPSE DISPOSER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            CorpseDisposer.ViewElementDef.Description.LocalizationKey = "PR_BC_CORPSE_DISPOSER_DESC"; //new LocalizedTextBind("<b>50% Fire Resistance</b>\n<i>There were too many to bury, so you had to burn them. Day after day, week after week. It's not the fire that scares you.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        private static void Change_HardLabor()
        {
            PassiveModifierAbilityDef HardLabor = DefCache.GetDef<PassiveModifierAbilityDef>("Strongman_AbilityDef");
            for (int i = 0; i < HardLabor.StatModifications.Length; i++)
            {
                if (HardLabor.StatModifications[i].TargetStat == StatModificationTarget.Perception)
                {
                    HardLabor.StatModifications[i].Value = -4.0f;
                }
                if (HardLabor.StatModifications[i].TargetStat == StatModificationTarget.Endurance)
                {
                    HardLabor.StatModifications[i].Value = 4.0f;
                }
            }
            HardLabor.ItemTagStatModifications = new EquipmentItemTagStatModification[0]; // delete weapon buff and proficiency
            HardLabor.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_HARD_LABOR"; //new LocalizedTextBind("HARD LABOR", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            HardLabor.ViewElementDef.Description.LocalizationKey = "PR_BC_HARD_LABOR_DESC"; //new LocalizedTextBind("<b>+2 Strength, -5 Perception</b>\n<i>The New World didn't just build itself. There was lot of digging, lifting and carrying, and dust and noise everywhere.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        private static void Change_Squatter()
        {
            PassiveModifierAbilityDef Squatter = DefCache.GetDef<PassiveModifierAbilityDef>("Resourceful_AbilityDef");
            Squatter.StatModifications = DefCache.GetDef<PassiveModifierAbilityDef>("Resourceful_AbilityDef").StatModifications;
            Squatter.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SQUATTER"; //new LocalizedTextBind("SQUATTER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Squatter.ViewElementDef.Description.LocalizationKey = "PR_BC_SQUATTER_DESC"; //new LocalizedTextBind("<b>+2 Strength, +25% carry weight</b>\n<i>In the camps you carried everything on your person at all times. And sometimes you had to take things from other people.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite SquatterIcon = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [ExpertPacker_AbilityDef]").LargeIcon;
            Squatter.ViewElementDef.LargeIcon = SquatterIcon;
            Squatter.ViewElementDef.SmallIcon = SquatterIcon;
        }
        private static void Create_Volunteered()
        {
            string skillName = "Volunteered_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("EnhancedVision_AbilityDef");
            PassiveModifierAbilityDef Volunteered = Helper.CreateDefFromClone(
                source,
                "9ca95f13-49d9-49fd-90bc-f1f59c99003b",
                skillName);
            Volunteered.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "ff169374-0ec6-481d-a071-2e8abd407755",
                skillName);
            Volunteered.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "da7ef36f-c51b-4a4a-9650-502ce52de24e",
                skillName);
            // reset all passive modifications we don't need
            Volunteered.DamageKeywordPairs = new DamageKeywordPair[0];
            // Set necessary fields
            float willpower = -2.0f;
            float speed = 2.0f;
            Volunteered.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification() // reduce current WP
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = willpower
                },
                new ItemStatModification() // reduce max WP, otherwise the operative could gain the 2 WP back in mission
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = willpower
                },
                //new ItemStatModification() // same for WillPoints
                //{
                //    TargetStat = StatModificationTarget.WillPoints,
                //    Modification = StatModificationType.Add,
                //    Value = willpower
                //},
                //new ItemStatModification() // and WillPoints max
                //{
                //    TargetStat = StatModificationTarget.WillPoints,
                //    Modification = StatModificationType.AddMax,
                //    Value = willpower
                //},
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Speed,
                    Modification = StatModificationType.Add,
                    Value = speed
                }
            };
            for (int i = 0; i < Volunteered.ItemTagStatModifications.Length; i++)
            {
                if (Volunteered.ItemTagStatModifications[i].EquipmentStatModification.Value > 0.0f)
                {
                    Volunteered.ItemTagStatModifications[i].EquipmentStatModification.Value = 0.0f;
                }
            }
            Volunteered.CharacterProgressionData.RequiredSpeed = 0;
            Volunteered.CharacterProgressionData.RequiredStrength = 0;
            Volunteered.CharacterProgressionData.RequiredWill = 0;
            Volunteered.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_VOLUNTEERED"; //new LocalizedTextBind("VOLUNTEERED", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Volunteered.ViewElementDef.Description.LocalizationKey = "PR_BC_VOLUNTEERED_DESC"; //new LocalizedTextBind("<b>Night vision, +2 Speed, -2 Willpower</b>\n<i>They gave you cat eyes and reflex enhancers, and nothing for the headaches that came after.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite VolunteeredIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Volunteered_1-1.png");
            Volunteered.ViewElementDef.LargeIcon = VolunteeredIcon;
            Volunteered.ViewElementDef.SmallIcon = VolunteeredIcon;
        }
       
        private static void Create_CondoRaider()
        {
            string skillName = "CondoRaider_AbilityDef";
            AddNavAreasAbilityDef source = DefCache.GetDef<AddNavAreasAbilityDef>("Humanoid_HighJump_AbilityDef");
            AddNavAreasAbilityDef CondoRaider = Helper.CreateDefFromClone(
                source,
                "5e2e7ad9-164d-4ac0-ae6f-23570bcfa525",
                skillName);
            CondoRaider.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "dab39934-f4b1-4348-beec-e281ce0fb807",
                skillName);
            CondoRaider.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "2b6ea7c4-beaa-405e-99ab-d6e6bf304f8d",
                skillName);
            // Set necessary fields
            // TODO: Finding a way to add speed modifier, the below don't work, the base ability has no StatModifications
            //CondoRaider.StatModifications = new ItemStatModification[]
            //{
            //    new ItemStatModification()
            //    {
            //        TargetStat = StatModificationTarget.Speed,
            //        Modification = StatModificationType.Add,
            //        Value = 1
            //    },
            //};
            //CondoRaider.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            //CondoRaider.DamageKeywordPairs = new DamageKeywordPair[0];
            CondoRaider.CharacterProgressionData.RequiredSpeed = 0;
            CondoRaider.CharacterProgressionData.RequiredStrength = 0;
            CondoRaider.CharacterProgressionData.RequiredWill = 0;
            CondoRaider.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_CONDO_RAIDER"; // new LocalizedTextBind("CONDO RAIDER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            CondoRaider.ViewElementDef.Description.LocalizationKey = "PR_BC_CONDO_RAIDER_DESC"; // new LocalizedTextBind("<b>Jump</b>\n<i>The world is full of broken elevators and collapsed staircases, and the pros in this business don't use ladders.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //Sprite CondoRaiderIcon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(t => t.name.Equals("E_ViewElement [ExpertRunner_AbilityDef]")).LargeIcon;
            //CondoRaider.ViewElementDef.LargeIcon = CondoRaiderIcon;
            //CondoRaider.ViewElementDef.SmallIcon = CondoRaiderIcon;
        }
        private static void Change_TunnelRat()
        {
            PassiveModifierAbilityDef TunnelRat = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
            for (int i = 0; i < TunnelRat.StatModifications.Length; i++)
            {
                if (TunnelRat.StatModifications[i].TargetStat == StatModificationTarget.Stealth)
                {
                    TunnelRat.StatModifications[i].Value = 0.15f;
                }
            }
            TunnelRat.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_TUNNEL_RAT"; // new LocalizedTextBind("TUNNEL RAT", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            TunnelRat.ViewElementDef.Description.LocalizationKey = "PR_BC_TUNNEL_RAT_DESC"; // new LocalizedTextBind("<b>+15% Stealth, +1 Speed</b>\n<i>You spent a good spell in the sewers. You had to be quiet, real quiet... And then run like hell!</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        private static void Create_Hunter()
        {
            string skillName = "Hunter_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("EagleEyed_AbilityDef");
            PassiveModifierAbilityDef Hunter = Helper.CreateDefFromClone(
                source,
                "15150fb6-0088-4124-bf7c-31146a2006ed",
                skillName);
            Hunter.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "ebc8f83b-b4a1-42e9-8bdb-03d34abca012",
                skillName);
            Hunter.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "001c6300-51f6-44a4-890d-b9f1a6587f53",
                skillName);

            // Set necessary fields
            for (int i = 0; i < Hunter.StatModifications.Length; i++)
            {
                if (Hunter.StatModifications[i].TargetStat == StatModificationTarget.Accuracy)
                {
                    Hunter.StatModifications[i].Value = 0.1f;
                }
            }
            Hunter.CharacterProgressionData.RequiredSpeed = 0;
            Hunter.CharacterProgressionData.RequiredStrength = 0;
            Hunter.CharacterProgressionData.RequiredWill = 0;
            Hunter.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_HUNTER"; // new LocalizedTextBind("HUNTER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Hunter.ViewElementDef.Description.LocalizationKey = "PR_BC_HUNTER_DESC"; // new LocalizedTextBind("<b>+10% Accuracy</b>\n<i>There used to be game and hunger around these parts.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite DeskJockeyIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Hunter_2-3.png");
            Hunter.ViewElementDef.LargeIcon = DeskJockeyIcon;
            Hunter.ViewElementDef.SmallIcon = DeskJockeyIcon;
        }
        private static void Create_Troublemaker()
        {
            string skillName = "Troublemaker_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("ExpertThrower_AbilityDef");
            PassiveModifierAbilityDef Troublemaker = Helper.CreateDefFromClone(
                source,
                "a35b7814-1b64-4ce6-ab31-ab4a70ad1732",
                skillName);
            Troublemaker.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "b752b25c-305d-4a01-aa3f-cf34d91c13ef",
                skillName);
            Troublemaker.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "5feb11bf-72f0-4bee-8cf5-c27b84efc5b3",
                skillName);

            // Set necessary fields
            //Troublemaker.StatModifications = new ItemStatModification[0]; // <- not necessary, is already null
            Troublemaker.ItemTagStatModifications[0].EquipmentStatModification.TargetStat = StatModificationTarget.BonusAttackDamage;
            Troublemaker.ItemTagStatModifications[0].EquipmentStatModification.Modification = StatModificationType.Multiply;
            Troublemaker.ItemTagStatModifications[0].EquipmentStatModification.Value = 1.1f;
            //Troublemaker.DamageKeywordPairs = new DamageKeywordPair[0]; // <- not necessary, is already null
            Troublemaker.CharacterProgressionData.RequiredSpeed = 0;
            Troublemaker.CharacterProgressionData.RequiredStrength = 0;
            Troublemaker.CharacterProgressionData.RequiredWill = 0;
            Troublemaker.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_TROUBLEMAKER"; //new LocalizedTextBind("TROUBLEMAKER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Troublemaker.ViewElementDef.Description.LocalizationKey = "PR_BC_TROUBLEMAKER_DESC"; // new LocalizedTextBind("<b>+10% Grenade Damage</b>\n<i>You have a knack for irritating the authorities. Maybe that's why you are here? Think about it.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite TroublemakerIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Guerilla.png");
            Troublemaker.ViewElementDef.LargeIcon = TroublemakerIcon;
            Troublemaker.ViewElementDef.SmallIcon = TroublemakerIcon;
        }
        private static void Create_Paranoid()
        {
            string skillName = "Paranoid_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
            PassiveModifierAbilityDef paranoid = Helper.CreateDefFromClone(
                source,
                "7da2f9bc-8175-4b62-81ef-af66c6cd8a58",
                skillName);
            paranoid.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "447fced9-3f37-4fce-8b81-e484e2a433b2",
                skillName);
            paranoid.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "c0988f23-cd33-435d-a8a0-56a28168d98b",
                skillName);
            paranoid.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Perception,
                    Modification = StatModificationType.Add,
                    Value = 8
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.HearingRange,
                    Modification = StatModificationType.Add,
                    Value = 10
                },
              };
            paranoid.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            paranoid.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PARANOID"; // new LocalizedTextBind("PARANOID", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            paranoid.ViewElementDef.Description.LocalizationKey = "PR_BC_PARANOID_DESC"; // new LocalizedTextBind("<b>+4 Perception, +10 Hearing Range</b>\n<i>But you are not, because the monsters are real!</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Paranoid_1-1.png");
            paranoid.ViewElementDef.LargeIcon = icon;
            paranoid.ViewElementDef.SmallIcon = icon;
        }
        private static void Create_Privileged()
        {
            string skillName = "Privileged_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
            PassiveModifierAbilityDef Privileged = Helper.CreateDefFromClone(
                source,
                "d2c9867d-faea-4b69-a503-3a00928d1ba7",
                skillName);
            Privileged.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "7bff46bc-f344-4078-9ee7-dd3fd6df14f9",
                skillName);
            Privileged.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "53085210-555a-4dda-bcee-bd8a361aa244",
                skillName);
            Privileged.StatModifications = new ItemStatModification[]
              {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Speed,
                    Modification = StatModificationType.Add,
                    Value = 2
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Perception,
                    Modification = StatModificationType.Add,
                    Value = 4
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.CarryWeight,
                    Modification = StatModificationType.MultiplyMax,
                    Value = 0.75f
                },
              };
            Privileged.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            Privileged.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PRIVILEGED";
            Privileged.ViewElementDef.Description.LocalizationKey = "PR_BC_PRIVILEGED_DESC";
            Sprite pIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Privileged_2-2.png");
            Privileged.ViewElementDef.LargeIcon = pIcon;
            Privileged.ViewElementDef.SmallIcon = pIcon;
        }
        private static void Create_AHistoryOfViolence()
        {
            string skillName = "AHistoryOfViolence_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Focused_AbilityDef");
            PassiveModifierAbilityDef aHistoryOfViolence = Helper.CreateDefFromClone(
                source,
                "0d856da5-b098-4e88-a09b-480f71e9470e",
                skillName);
            aHistoryOfViolence.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "285336cf-7c14-4b04-8ea0-e284b9bc1e7d",
                skillName);
            aHistoryOfViolence.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "181083f5-7fa7-490e-afdc-30a39d313dbe",
                skillName);

            // Set necessary fields
            float willpower = -2.0f;
            float bonusDamage = 1.1f;
            aHistoryOfViolence.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.Add,
                    Value = willpower
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Willpower,
                    Modification = StatModificationType.AddMax,
                    Value = willpower
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.BonusAttackDamage,
                    Modification = StatModificationType.Multiply,
                    Value = bonusDamage
                },
            };
            aHistoryOfViolence.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
            aHistoryOfViolence.DamageKeywordPairs = new DamageKeywordPair[0];
            aHistoryOfViolence.CharacterProgressionData.RequiredSpeed = 0;
            aHistoryOfViolence.CharacterProgressionData.RequiredStrength = 0;
            aHistoryOfViolence.CharacterProgressionData.RequiredWill = 0;
            aHistoryOfViolence.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_HISTORY_OF_VIOLENCE"; // new LocalizedTextBind("A HISTORY OF VIOLENCE", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            aHistoryOfViolence.ViewElementDef.Description.LocalizationKey = "PR_BC_HISTORY_OF_VIOLENCE_DESC"; // new LocalizedTextBind("<b>+10% Damage, -3 Willpower</b>\n<i>In the past, you hurt some people. They probably deserved it.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite ahofIcon = DefCache.GetDef<PassiveModifierAbilityDef>("SilencedWeaponTalent_AbilityDef").ViewElementDef.LargeIcon;
            aHistoryOfViolence.ViewElementDef.LargeIcon = ahofIcon;
            aHistoryOfViolence.ViewElementDef.SmallIcon = ahofIcon;
        }
        private static void Create_Daredevil()
        {
            string skillName = "Daredevil_AbilityDef";
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Reckless_AbilityDef");
            PassiveModifierAbilityDef daredevil = Helper.CreateDefFromClone(
                source,
                "a1bb97ba-862f-4c0d-98b7-efbcd6f9021d",
                skillName);
            daredevil.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "211ed239-6be7-42c4-8a85-0d9c9c73c645",
                skillName);
            daredevil.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "e95de50c-8c04-4a2c-b219-296c816def67",
                skillName);
            //daredevil.StatModifications[0].Value = 1.1f; // no change, not necessary
            //daredevil.StatModifications[1].Value = -0.1f; // no change, not necessary
            daredevil.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_DAREDEVIL"; // new LocalizedTextBind("DAREDEVIL", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            daredevil.ViewElementDef.Description.LocalizationKey = "PR_BC_DAREDEVIL_DESC"; // new LocalizedTextBind("<b>+10% Damage, -10% Accuracy</b>\n<i>In the world you were born to, you have to live dangerously if at all. Strike first, strike hard, aim not.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite ddIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Heartless.png");
            daredevil.ViewElementDef.LargeIcon = ddIcon;
            daredevil.ViewElementDef.SmallIcon = ddIcon;
        }
        private static void Create_DamagedAmygdala()
        {
            string skillName = "DamagedAmygdala_AbilityDef";
            DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("PsychicImmunity_DamageMultiplierAbilityDef");
            DamageMultiplierAbilityDef damagedAmygdala = Helper.CreateDefFromClone(
                source,
                "5fe50c69-3081-4502-98bf-1ba9d6911c99",
                skillName);
            damagedAmygdala.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "ed634d13-34ec-43ef-9940-04400369535f",
                skillName);
            damagedAmygdala.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "e853a40d-7117-46bb-9504-7c0dea1fff97",
                skillName);
            PRMLogger.Debug("---------------------------------------------------------------", false);
            // Set necessary fields
            damagedAmygdala.CharacterProgressionData.RequiredSpeed = 0;
            damagedAmygdala.CharacterProgressionData.RequiredStrength = 0;
            damagedAmygdala.CharacterProgressionData.RequiredWill = 0;
            damagedAmygdala.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_DAMAGED_AMYGDALA";
            damagedAmygdala.ViewElementDef.Description.LocalizationKey = "PR_BC_DAMAGED_AMYGDALA_DESC";
        }
        private static void Create_SanitationExpert()
        {
            string skillName = "SanitationExpert_AbilityDef";
            GooDamageMultiplierAbilityDef source = DefCache.GetDef<GooDamageMultiplierAbilityDef>("GooImmunity_AbilityDef");
            GooDamageMultiplierAbilityDef sanitationExpert = Helper.CreateDefFromClone(
                source,
                "f3aa9070-fbba-4fe7-8909-4b098e53187c",
                skillName);
            sanitationExpert.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "17382410-b088-442f-bd45-31aa43461fb6",
                skillName);
            sanitationExpert.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "533d07ea-4928-43ba-a4af-91bb2be260fe",
                skillName);
            sanitationExpert.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_SANITATION_EXPERT"; // new LocalizedTextBind("SANITATION EXPERT", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            sanitationExpert.ViewElementDef.Description.LocalizationKey = "PR_BC_SANITATION_EXPERT_DESC"; // new LocalizedTextBind("<b>Goo Immunity</b>\n<i>Robots can't do everything. Grime, dirt, slime, ooze, yuck, sludge: you have been through it all and come on the other side.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite seIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Plumber.png");
            sanitationExpert.ViewElementDef.LargeIcon = seIcon;
            sanitationExpert.ViewElementDef.SmallIcon = seIcon;
        }
        private static void Create_LabAssistant()
        {
            string skillName = "LabAssistant_AbilityDef";
            DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef");
            DamageMultiplierAbilityDef labAssistant = Helper.CreateDefFromClone(
                source,
                "610c2c16-3572-4c5b-b75d-a05f2520266e",
                skillName);
            labAssistant.CharacterProgressionData = Helper.CreateDefFromClone(
                DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef").CharacterProgressionData,
                "fe81789d-940a-497e-bc2e-847a2ecadc05",
                skillName);
            labAssistant.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "7a0f28dd-ee19-4c52-a2af-ab894bf70845",
                skillName);
            labAssistant.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_LAB_ASSISTANT"; // new LocalizedTextBind("LAB ASSISTANT", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            labAssistant.ViewElementDef.Description.LocalizationKey = "PR_BC_LAB_ASSISTANT_DESC"; // new LocalizedTextBind("<b>Acid Resistance</b>\n<i>All those little accidents throughout the years have taught you a lot about safely dealing with acid spills.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        private static void Create_Rockteer()
        {
            string skillName = "Rocketeer_AbilityDef";
            ClassProficiencyAbilityDef source = DefCache.GetDef<ClassProficiencyAbilityDef>("UseAttachedEquipment_AbilityDef");
            ClassProficiencyAbilityDef rocketeer = Helper.CreateDefFromClone(
                source,
                "52a59fad-179c-4126-a28e-2de988137a78",
                skillName);
            rocketeer.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "30779905-64da-4a23-8176-716d00beb8c8",
                skillName);
            rocketeer.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "c89a96a1-aaf3-4413-b355-bcb4646f263e",
                skillName);
            // Set necessary fields
            rocketeer.CharacterProgressionData.RequiredSpeed = 0;
            rocketeer.CharacterProgressionData.RequiredStrength = 0;
            rocketeer.CharacterProgressionData.RequiredWill = 0;
            rocketeer.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_ROCKETEER"; // new LocalizedTextBind("ROCKETEER", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            rocketeer.ViewElementDef.Description.LocalizationKey = "PR_BC_ROCKETEER_DESC"; // new LocalizedTextBind("<b>Jetpack Proficiency</b>\n<i>Since you were a child you dreamt of flying rockets. Perhaps now you can!</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite rocketeerIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_CharacterAbility_LaunchMissile-2.png");
            rocketeer.ViewElementDef.LargeIcon = rocketeerIcon;
            rocketeer.ViewElementDef.SmallIcon = rocketeerIcon;
        }
        private static void Create_TrueGrit()
        {
            // Set all melee weapon defs to the a melee damage type
            // Don't used! It messes up different melee weapons with additional status damages
            // Create a new array with some melee damage types
            //DamageTypeBaseEffectDef[] meleeDamageTypes = new DamageTypeBaseEffectDef[]
            //{
            //    Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Slash_StandardDamageTypeEffectDef")),
            //    Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("MeleeBash_StandardDamageTypeEffectDef")),
            //    Repo.GetAllDefs<DamageTypeBaseEffectDef>().FirstOrDefault(dtb => dtb.name.Equals("Bash_StandardDamageTypeEffectDef"))
            //};
            //Fix_MeleeWeaponDamageType(meleeDamageTypes);

            string skillName = "TrueGrit_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");
            ApplyStatusAbilityDef trueGrit = Helper.CreateDefFromClone(
                source,
                "1a688a8d-96df-41f4-83ea-c554de05a7a4",
                skillName);
            trueGrit.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "f3115a62-c8a9-4d4f-ab95-78bb1a0ada4e",
                skillName);
            trueGrit.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "b387840f-2407-44cf-a2d8-cc219f4525d7",
                skillName);
            trueGrit.StatusDef = Helper.CreateDefFromClone(
               source.StatusDef,
                "c6daf964-6de7-4fab-8fa5-0a8f598fcaec",
                skillName);
            trueGrit.CharacterProgressionData.RequiredSpeed = 0;
            trueGrit.CharacterProgressionData.RequiredStrength = 0;
            trueGrit.CharacterProgressionData.RequiredWill = 0;
            trueGrit.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_TRUE_GRIT"; // new LocalizedTextBind("TRUE GRIT", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            trueGrit.ViewElementDef.Description.LocalizationKey = "PR_BC_TRUE_GRIT_DESC"; // new LocalizedTextBind("<b>-10% damage from adjacent enemies</b>\n<i>The end of civilization wasn't kind to you, but when things get up close and personal, you just suck it up.</i>", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite tgIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_TentacularBody_MeleeAttackRetaliation-2.png");
            trueGrit.ViewElementDef.LargeIcon = tgIcon;
            trueGrit.ViewElementDef.SmallIcon = tgIcon;
            DamageMultiplierStatusDef tgStatus = (DamageMultiplierStatusDef)trueGrit.StatusDef;
            tgStatus.Multiplier = 0.8f;
            tgStatus.Range = 1.5f;
            tgStatus.Visuals = trueGrit.ViewElementDef;
            tgStatus.VisibleOnStatusScreen = 0;
        }
        //private static void Fix_MeleeWeaponDamageType(DamageTypeBaseEffectDef[] meleeDamageTypes)
        //{
        //    Logger.Always("Fix_MeleeWeaponDamageType called, fixed melee weapons:", false);
        //    int count = 0;
        //    foreach (WeaponDef weapon in Repo.GetAllDefs<WeaponDef>())
        //    {
        //        if (weapon.DamagePayload.DamageDeliveryType == DamageDeliveryType.Melee && !meleeDamageTypes.Contains(weapon.DamagePayload.DamageType))
        //        {
        //            Logger.Always("   <" + weapon.name + "> with damage type <" + weapon.DamagePayload.DamageType.name + "> set to <" + meleeDamageTypes[0].name + ">");
        //            weapon.DamagePayload.DamageType = meleeDamageTypes[0];
        //            count++;
        //        }
        //    }
        //    if (count == 0)
        //    {
        //        Logger.Always("  None");
        //    }
        //    Logger.Always("------------------------------------------------------------", false);
        //}
    }
}