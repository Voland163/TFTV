using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.View.ViewControllers;
using PhoenixPoint.Tactical.View.ViewModules;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;


namespace PRMBetterClasses.VariousAdjustments
{
    internal class VariousAdjustments
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        public static void ApplyChanges()
        {
            SharedData shared = GameUtl.GameComponent<SharedData>();

            // Deny to demmolish the Access Lift
            Change_AccessLift();
            // Bash: Increase damage to weapon from 0,45 to 0,6
            Change_BashWeaponDamage();
            // Fix Regen Torso to also regenerate health in vehicles
            Fix_RegenTorso();
            // Fix for Triton Elite bloodsucker arms
            Fix_TritonElite();
            // Change Advanced Laser research to require advanced technician weapons
            Change_AdvancedResearches();
            // Change Stimpack: Restores 2AP, Heal 1HP to every body part. Disabled Body Parts are restored.
            Change_Stimpack();
            // Change Poison: -50% accuracy and -3 WP per turn
            Change_Poison();
            // Change various bionics
            Change_VariousBionics();
            // Turrets: Shoot at 1/2 burst but cost 2AP to shoot , maybe reduce armor of all by 10?
            Change_Turrets();
            // Stomp: Gain 50 blast damage
            Change_Stomp(shared);
            // Frenzy: Grant +8 SPD instead of 50% SPD
            Change_Frenzy();
            // Psychici resistance: fix effect and description to: Psychic Scream damage values are halved
            Change_PsychicResistance();
            // Mutoid Worms: limit each worm ability to 5 ammo (worms)
            Change_Mutoids();
            // Screaming Head: Mind Control Immunity
            Change_PriestsHeadMutations();
            // Spider Drones: Armor down to 10 (from 30)
            Change_SpiderDrones();
            // Various weapon changes
            Change_VariousWeapons(shared);
            // Venom Torso: Add Weapon Tag to Poison Arm 
            Change_VenomTorso();
            // Haven Recruits: Come with Armour and Weapons on all difficulties
            Change_HavenRecruits();
            // Mech Arms: 200 emp damage
            Change_MechArms(shared);
            // Shadow Legs: Electric Kick replace shock damage with Sonic damage (value 20)
            Change_ShadowLegs(shared);
            // Vidar GL - Increase Shred to 20 (from 10), Add Acid 10. Increase AP cost to 2 (from 1)
            Change_VidarGL(shared);
            // Destiny III - Give chance to fumble when non-proficient
            Change_Destiny();
        }

        private static void Change_AccessLift()
        {
            DefCache.GetDef<PhoenixFacilityDef>("AccessLift_PhoenixFacilityDef").CannotDemolish = true;
        }

        private static void Change_BashWeaponDamage()
        {
            MeleeBashDamageEffectDef meleeBashDamageEffect = (MeleeBashDamageEffectDef)Repo.GetDef("38767152-dc65-8be1-6d12-ad7ba889a88a"); // E_MeleeBashEffect [MeleeBash_StandardDamageTypeEffectDef]
            meleeBashDamageEffect.DamageAmountForTheEquipment = 0.6f;
        }

        private static void Fix_RegenTorso()
        {
            ApplyStatusAbilityDef regenTorsoAbility = DefCache.GetDef<ApplyStatusAbilityDef>("Regeneration_Torso_Passive_AbilityDef");
            regenTorsoAbility.CanApplyToOffMapTarget = true; // can apply the regeneration status also in vehicles (= off map)
        }

        private static void Fix_TritonElite()
        {
            GameTagDef combinedWeaponBodyPart_Tag = (GameTagDef)Repo.GetDef("498a2ab2-cd1a-d104-f8fc-f37e875f76dc"); //"CombinedWeaponBodyPart_TagDef" (m_PathID 36683)
            List<TacticalItemDef> fishmanEliteUpperArms = new List<TacticalItemDef>()
            {
                (TacticalItemDef)Repo.GetDef("cb294fe3-a30b-5bc4-2ad0-3361cb1d0d84"), //"FishmanElite_UpperArms_BloodSucker_BodyPartDef"
                (TacticalItemDef)Repo.GetDef("ed323004-0282-a354-3ae0-053791ad17c6"), //"FishmanElite_Upper_LeftArm_BloodSucker_BodyPartDef"
                (TacticalItemDef)Repo.GetDef("b5361644-9ac1-9fd4-d931-b144c1c7d329"), //"FishmanElite_Upper_RightArm_BloodSucker_BodyPartDef"
                (TacticalItemDef)Repo.GetDef("32a6dd8e-0abb-3224-6b4b-33847fd67804"), //"FishmanElite_UpperArms_Paralyzing_BodyPartDef"
                (TacticalItemDef)Repo.GetDef("9b96a46e-8b84-7b64-fa18-71ee51afa0dd"), //"FishmanElite_Upper_LeftArm_Paralyzing_BodyPartDef"
                (TacticalItemDef)Repo.GetDef("3cedbaa9-1574-5f94-e8b9-1afec1f57903")  //"FishmanElite_Upper_RightArm_Paralyzing_BodyPartDef"
            };
            foreach (TacticalItemDef tacticalItemDef in fishmanEliteUpperArms)
            {
                if (tacticalItemDef.WeakAddon && tacticalItemDef.HandsToUse != 0) // base where the two arms are tied on is a WeakAddon ("..._UpperArms_...")
                {
                    tacticalItemDef.HandsToUse = 0;
                }
                if (!tacticalItemDef.Tags.Contains(combinedWeaponBodyPart_Tag))
                {
                    tacticalItemDef.Tags.Add(combinedWeaponBodyPart_Tag);
                }
            }
            /*
                        GameTagDef gameTag = (GameTagDef)Repo.GetDef("498a2ab2-cd1a-d104-f8fc-f37e875f76dc"); //DefCache.GetDef<GameTagDef>("CombinedWeaponBodyPart_TagDef");
                        TacticalItemDef EBloodsucker = (TacticalItemDef)Repo.GetDef("cb294fe3-a30b-5bc4-2ad0-3361cb1d0d84"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");
                        TacticalItemDef LEBloodsucker = (TacticalItemDef)Repo.GetDef("ed323004-0282-a354-3ae0-053791ad17c6"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_LeftArm_BloodSucker_BodyPartDef");
                        TacticalItemDef REBloodsucker = (TacticalItemDef)Repo.GetDef("b5361644-9ac1-9fd4-d931-b144c1c7d329"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_RightArm_BloodSucker_BodyPartDef");
                        EBloodsucker.HandsToUse = 0;
                        EBloodsucker.Tags.Add(gameTag);
                        LEBloodsucker.Tags.Add(gameTag);
                        REBloodsucker.Tags.Add(gameTag);
                        TacticalItemDef EParalysing = (TacticalItemDef)Repo.GetDef("32a6dd8e-0abb-3224-6b4b-33847fd67804"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_UpperArms_Paralyzing_BodyPartDef");
                        TacticalItemDef LEParalysing = (TacticalItemDef)Repo.GetDef("9b96a46e-8b84-7b64-fa18-71ee51afa0dd"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_LeftArm_Paralyzing_BodyPartDef");
                        TacticalItemDef REParalysing = (TacticalItemDef)Repo.GetDef("3cedbaa9-1574-5f94-e8b9-1afec1f57903"); //DefCache.GetDef<TacticalItemDef>("FishmanElite_Upper_RightArm_Paralyzing_BodyPartDef");
                        EParalysing.HandsToUse = 0;
                        EParalysing.Tags.Add(gameTag);
                        LEParalysing.Tags.Add(gameTag);
                        REParalysing.Tags.Add(gameTag);
            */
        }

        private static void Change_AdvancedResearches()
        {
            // Advanced laser tech
            ExistingResearchRequirementDef advLaserResearchRequirement0 = DefCache.GetDef<ExistingResearchRequirementDef>("PX_AdvancedLaserTech_ResearchDef_ExistingResearchRequirementDef_0");
            advLaserResearchRequirement0.ResearchID = "SYN_NightVision_ResearchDef";
            //ExistingResearchRequirementDef advLaserResearchRequirement1 = DefCache.GetDef<ExistingResearchRequirementDef>("PX_AdvancedLaserTech_ResearchDef_ExistingResearchRequirementDef_1");
            //advLaserResearchRequirement1.ResearchID = "NJ_PRCRTechTurret_ResearchDef";

            // Advanced melee tech (StunRod = Shock Lance)
            ExistingResearchRequirementDef advMeleeResearchRequirement0 = DefCache.GetDef<ExistingResearchRequirementDef>("PX_StunRodTech_ResearchDef_ExistingResearchRequirementDef_0");
            advMeleeResearchRequirement0.ResearchID = "ANU_AdvancedMeleeCombat_ResearchDef";
            ExistingResearchRequirementDef advMeleeResearchRequirement1 = DefCache.GetDef<ExistingResearchRequirementDef>("PX_StunRodTech_ResearchDef_ExistingResearchRequirementDef_1");
            advMeleeResearchRequirement1.ResearchID = "SYN_AdvancedDisableTech_ResearchDef";

            // Advanced acid tech, adding an additional requirement
            ExistingResearchRequirementDef advAcidResearchRequirement1 = Helper.CreateDefFromClone(
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_AdvancedAcidTech_ResearchDef_ExistingResearchRequirementDef_0"),
                "6C04D135-7609-40F6-AC08-09832817ED20",
                "PX_AdvancedAcidTech_ResearchDef_ExistingResearchRequirementDef_1");
            advAcidResearchRequirement1.ResearchID = "PX_HelCannon_ResearchDef";
            ResearchDef advAcidResearch = DefCache.GetDef<ResearchDef>("PX_AdvancedAcidTech_ResearchDef");
            advAcidResearch.RevealRequirements.Container[0].Requirements = advAcidResearch.RevealRequirements.Container[0].Requirements.AddToArray(advAcidResearchRequirement1);
        }

        private static void Change_Stimpack()
        {
            EquipmentDef stimpack = DefCache.GetDef<EquipmentDef>("Stimpack_EquipmentDef");
            stimpack.ViewElementDef.Description.LocalizationKey = "PR_BC_STIMPACK_ITEM_DESC";

            HealAbilityDef stimpackAbility = stimpack.GetAbilityDef<HealAbilityDef>();
            stimpackAbility.ViewElementDef.Description.LocalizationKey = "PR_BC_STIMPACK_ABILITY_DESC";
            stimpackAbility.ActionPointCost = 0.25f;
            stimpackAbility.HealBodyParts = true;
            stimpackAbility.BodyPartHealAmount = 10.0f;
            //stimpackAbility.HealEffects.Add(Repo.GetAllDefs<HealAbilityDef>().FirstOrDefault(has => has.name.Equals("FieldMedic_AbilityDef")).HealEffects[0]);

            TacActorSimpleInteractionAnimActionDef healAnimActionDef =
                DefCache.GetDef<TacActorSimpleInteractionAnimActionDef>("E_MedkitHeal [Soldier_Utka_AnimActionsDef]");
            healAnimActionDef.Items = healAnimActionDef.Items.AddToArray(stimpack);
        }

        private static void Change_Poison()
        {
            DamageOverTimeStatusDef poisonDOT = DefCache.GetDef<DamageOverTimeStatusDef>("Poison_DamageOverTimeStatusDef");
            poisonDOT.Visuals.Description.LocalizationKey = "PR_BC_POISON_STATUS_DESC";
        }
        // Make Trembling status accessible for Harmony patches to avoid time critical Repo calls in them.
        public static StatMultiplierStatusDef trembling = DefCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef");
        // Harmony patch for Poison DOT to additionally apply -50% accuracy (Trembling status) and -3 WP per turn
        [HarmonyPatch(typeof(DamageOverTimeStatus), "ApplyEffect")]
        internal static class BC_DamageOverTimeStatus_ApplyEffect_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(DamageOverTimeStatus __instance)
            {
                if (__instance.DamageOverTimeStatusDef.name.Equals("Poison_DamageOverTimeStatusDef"))
                {
                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);
                    //StatusComponent statusComponent = (StatusComponent)AccessTools.Property(typeof(TacStatus), "StatusComponent").GetValue(__instance, null);
                    //StatMultiplierStatusDef trembling = Repo.GetAllDefs<StatMultiplierStatusDef>().FirstOrDefault(sms => sms.name.Equals("Trembling_StatusDef"));

                    if (__instance.IntValue <= 0 && base_TacticalActor.Status.HasStatus(trembling))
                    {
                        StatMultiplierStatus status = base_TacticalActor.Status.GetStatus<StatMultiplierStatus>(trembling);
                        status.RequestUnapply(status.StatusComponent);
                        return;
                    }

                    if (__instance.IntValue > 0)
                    {
                        if (!base_TacticalActor.Status.HasStatus(trembling))
                        {
                            _ = base_TacticalActor.Status.ApplyStatus(trembling);
                        }
                        float newWP = Mathf.Max(base_TacticalActor.CharacterStats.WillPoints.Min, base_TacticalActor.CharacterStats.WillPoints - 3.0f);
                        base_TacticalActor.CharacterStats.WillPoints.Set(newWP);
                    }
                }
            }
        }
        // Harmony patch to unapply trembling when poison status is unapplied
        [HarmonyPatch(typeof(TacEffectStatus), "OnUnapply")]
        internal static class BC_TacEffectStatus_OnUnapply_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(TacEffectStatus __instance)
            {
                if (__instance.TacEffectStatusDef.name.Equals("Poison_DamageOverTimeStatusDef"))
                {
                    TacticalActor base_TacticalActor = (TacticalActor)AccessTools.Property(typeof(TacStatus), "TacticalActor").GetValue(__instance, null);
                    //StatMultiplierStatusDef trembling = Repo.GetAllDefs<StatMultiplierStatusDef>().FirstOrDefault(sms => sms.name.Equals("Trembling_StatusDef"));
                    if (base_TacticalActor.Status.HasStatus(trembling))
                    {
                        StatMultiplierStatus status = base_TacticalActor.Status.GetStatus<StatMultiplierStatus>(trembling);
                        status.RequestUnapply(status.StatusComponent);
                        return;
                    }
                }
            }
        }

        private static void Change_VariousBionics()
        {
            // Restrict Rocket Leap ability to 2 uses per turn
            TacticalAbilityDef rocketLeap = DefCache.GetDef<TacticalAbilityDef>("Exo_Leap_AbilityDef");
            rocketLeap.UsesPerTurn = 2;
            rocketLeap.WillPointCost = 3f;
            //rocketLeap.TargetingDataDef.Origin.Range = 12;

            // Juggernaut Torso & Armadillo Legs: Speed -1 -> 0
            BodyPartAspectDef juggTorsoAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Jugg_BIO_Torso_BodyPartDef]");
            BodyPartAspectDef juggLegsAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Jugg_BIO_Legs_ItemDef]");
            juggTorsoAspect.Speed = juggLegsAspect.Speed = 0;

            // Give mounted weapon slot to Juggernaut Torso 
            TacticalItemDef juggTorso = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Torso_BodyPartDef");
            TacticalItemDef neuralTorso = DefCache.GetDef<TacticalItemDef>("NJ_Exo_BIO_Torso_BodyPartDef");
            juggTorso.ProvidedSlots = neuralTorso.ProvidedSlots;

            // shield deploy AP cost 1 -> 0
            TacticalAbilityDef deployJuggShield = DefCache.GetDef<TacticalAbilityDef>("DeployShield_Bionic_AbilityDef");
            deployJuggShield.ActionPointCost = 0;

            // Neural Torso: Grants Mounted Weapons and Tech Arms Proficiency (MountedWeaponTalent_AbilityDef = MountedItem_TagDef = proficiency with all mounted equipment)
            // First fix name and description of given mounted weapon talent that in fact gives mounted item proficiency also for robotic arms
            PassiveModifierAbilityDef mountedItemsProficiency = DefCache.GetDef<PassiveModifierAbilityDef>("MountedWeaponTalent_AbilityDef");
            mountedItemsProficiency.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_MOUNTED_ITEMS_PROF";
            mountedItemsProficiency.ViewElementDef.Description = new LocalizedTextBind("PR_BC_MOUNTED_ITEMS_PROF_DESC");
            //Sprite icon = Repo.GetAllDefs<ViewElementDef>().FirstOrDefault(ve => ve.name.Equals("E_View [NJ_Technician_MechArms_WeaponDef]")).LargeIcon;
            //mountedItemsProficiency.ViewElementDef.LargeIcon = icon;
            //mountedItemsProficiency.ViewElementDef.SmallIcon = icon;
            mountedItemsProficiency.ViewElementDef.ShowInInventoryItemTooltip = true;
            // Add proficiency ability to Neural Torso
            if (!neuralTorso.Abilities.Contains(mountedItemsProficiency))
            {
                neuralTorso.Abilities = neuralTorso.Abilities.AddToArray(mountedItemsProficiency);
            }

            // Mirage Legs: Speed +1 -> +2
            BodyPartAspectDef mirageLegsAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Shinobi_BIO_Legs_ItemDef]");
            mirageLegsAspect.Speed = 2;

            // Vengeance Torso: Attacks against enemies within 10 tiles deal 10% more damage (Battle Focus ability)
            TacticalItemDef vTorso = DefCache.GetDef<TacticalItemDef>("SY_Shinobi_BIO_Torso_BodyPartDef");
            vTorso.Abilities[1] = DefCache.GetDef<AbilityDef>("BattleFocus_AbilityDef");

            // Echo Head: Remove "Silent Echo", Stealth +10% -> +15%, Accuracy 0% -> 5% --- DELAYED AMYBE EVEN NOT NECESSARY
            //TacticalItemDef echoHead = (TacticalItemDef)Repo.GetDef("bacfc1a6-f043-ff64-8bee-5bbdea13970f"); // SY_Shinobi_BIO_Helmet_BodyPartDef
            //echoHead.Abilities = new AbilityDef[] // set abilities new without Silent Echo, aka remove it
            //{
            //    (AbilityDef)Repo.GetDef("94b5e1df-83c8-9c74-fa4f-104708cd017c"), // EnhancedVision_AbilityDef (Night Vision)
            //    (AbilityDef)Repo.GetDef("7cb66494-acd9-fa74-b93d-15e5f50e5b40")  // BionicDamageMultipliers_AbilityDef
            //};
            //echoHead.BodyPartAspectDef.Stealth = 0.15f;
            //echoHead.BodyPartAspectDef.Accuracy = 0.05f;
        }

        public static void Change_Turrets()
        {
            int turretAPToUsePerc = 50;
            int turretArmor = 10;
            int turretAutoFireShotCount = 4;

            WeaponDef turret = DefCache.GetDef<WeaponDef>("NJ_TechTurretGun_WeaponDef");
            turret.APToUsePerc = turretAPToUsePerc;
            turret.Armor = turretArmor;
            turret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;

            WeaponDef prcrTurret = DefCache.GetDef<WeaponDef>("NJ_PRCRTechTurretGun_WeaponDef");
            prcrTurret.APToUsePerc = turretAPToUsePerc;
            prcrTurret.Armor = turretArmor;
            prcrTurret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;

            WeaponDef laserTurret = DefCache.GetDef<WeaponDef>("PX_LaserTechTurretGun_WeaponDef");
            laserTurret.APToUsePerc = turretAPToUsePerc;
            laserTurret.Armor = turretArmor;
            laserTurret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;
        }
        public static void Change_Stomp(SharedData shared)
        {
            int StompShockValue = 200;
            int StompBlastValue = 50;

            ApplyDamageEffectAbilityDef stomp = DefCache.GetDef<ApplyDamageEffectAbilityDef>("StomperLegs_Stomp_AbilityDef");
            stomp.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShockKeyword, Value = StompShockValue },
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.BlastKeyword, Value = StompBlastValue },
                };
        }
        public static void Change_Frenzy()
        {
            float frenzySpeed = 0.33f;

            FrenzyStatusDef frenzy = DefCache.GetDef<FrenzyStatusDef>("Frenzy_StatusDef");
            frenzy.SpeedCoefficient = frenzySpeed;
            //LocalizedTextBind description = new LocalizedTextBind("", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            foreach (ViewElementDef visuals in Repo.GetAllDefs<ViewElementDef>().Where(tav => tav.name.Contains("Frenzy_")))
            {
                visuals.Description.LocalizationKey = visuals.name.Contains("Status") ? "PR_BC_FRENZY_STATUS_DESC" : "PR_BC_FRENZY_DESC";
            }
        }
        public static void Change_PsychicResistance()
        {
            DamageMultiplierAbilityDef psychicResistant = DefCache.GetDef<DamageMultiplierAbilityDef>("PsychicResistant_DamageMultiplierAbilityDef");
            psychicResistant.Multiplier = 0.0f; // Set to 0.0 because 0.5 = resistance don't work.
            // Burrow view element aka name, description and icon from psychic immunity
            DamageMultiplierAbilityDef psychicImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("PsychicImmunity_DamageMultiplierAbilityDef");
            psychicResistant.ViewElementDef = psychicImmunity.ViewElementDef;
        }
        public static void Change_Mutoids()
        {
            // Mutoids gain Pistol and Assault Rifle proficiency -> adding Mutoid_ClassTagDef ("cd38a996-0565-1694-bb91-b4479a65950e") to their weapon tags
            // -> see Change_VariousWeapons(..) below

            // Worm launcher, limited ammo and range
            List<WeaponDef> mutoidWormLauncher = new List<WeaponDef>()
            {
                (WeaponDef)Repo.GetDef("8ca45ea2-3f17-c0f4-1ab6-464bbe6a6acb"), // Mutoid_Arm_AcidWorm_WeaponDef
                (WeaponDef)Repo.GetDef("f5a7eb15-269d-db74-9bc1-38d96f785cef"), // Mutoid_Arm_FireWorm_WeaponDef
                (WeaponDef)Repo.GetDef("3412c7e3-1d20-bd24-daac-93f3c245ce77")  // Mutoid_Arm_PoisonWorm_WeaponDef
            };
            TacticalItemDef sharedFreeReloadAmmo = (TacticalItemDef)Repo.GetDef("e296cd3b-74a9-2184-1900-f122ac9c50fc"); // SharedFreeReload_AmmoClip_ItemDef
            foreach (WeaponDef launcher in mutoidWormLauncher)
            {
                launcher.ChargesMax = 5;
                launcher.DamagePayload.Range = 25f;
                launcher.CompatibleAmmunition = new TacticalItemDef[] { sharedFreeReloadAmmo };
            }
        }
        public static void Change_VariousWeapons(SharedData shared)
        {
            // defining variables
            SharedDamageKeywordsDataDef damageKeywords = shared.SharedDamageKeywords;
            GameTagDef grenadeTag = (GameTagDef)Repo.GetDef("318dd3ff-28f0-1bb4-98bc-39164b7292b6"); // GrenadeItem_TagDef
            GameTagDef mutoidClassTag = (GameTagDef)Repo.GetDef("cd38a996-0565-1694-bb91-b4479a65950e"); // Mutoid_ClassTagDef
            GameTagDef pistolTag = (GameTagDef)Repo.GetDef("7a8a0a76-deb6-c004-3b5b-712eae0ad4a5"); // HandgunItem_TagDef
            GameTagDef assaultRifleTag = (GameTagDef)Repo.GetDef("d98ff229-5459-9224-ea2e-2dbca60bae1d"); // AssaultRifleItem_TagDef
            TFTVConfig config = TFTVMain.Main.Config;
            // loop over all weapon defs in the repo
            foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
            {
                // All hand thrown grenades (only these weapon defs ends with "Grenade_WeaponDef" <- checked by tag)
                if (weaponDef.Tags.Contains(grenadeTag)) // weaponDef.name.EndsWith("Grenade_WeaponDef") && 
                {
                    // Manufature intantly
                    weaponDef.ManufacturePointsCost = 0;
                }
                // Mutoids gain Pistol and Assault Rifle proficiency -> adding Mutoid_ClassTagDef ("cd38a996-0565-1694-bb91-b4479a65950e") to their weapon tags
                if ((weaponDef.Tags.Contains(pistolTag) || weaponDef.Tags.Contains(assaultRifleTag)) && !weaponDef.Tags.Contains(mutoidClassTag))
                {
                    weaponDef.Tags.Add(mutoidClassTag);
                }
                // Various changes dependend on weapon def guids (safer than using the def names)
                switch (weaponDef.Guid)
                {
                    // Hawk light sniper rifle, switch to one handed ...
                    //case "f72e9df8-2c13-6ba4-e9b8-444dfda1b19a": // FS_LightSniperRifle_WeaponDef
                    //    weaponDef.HandsToUse = 1;
                    //    break;
                    // Rebuke, add piercing scrap shred

                    case "831be08f-d0d7-2764-4833-02ce83ff7277": // AC_Rebuke_WeaponDef
                        if (TFTVNewGameOptions.ImpossibleWeaponsAdjustmentsSetting)
                        {
                            //weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword).Value = 1;
                            // Remove shredding
                            _ = weaponDef.DamagePayload.DamageKeywords.RemoveAll(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                            weaponDef.DamagePayload.ArmourShred = 0;
                            weaponDef.DamagePayload.ArmourShredProbabilityPerc = 0;
                            //weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.BlastKeyword).Value = 10;
                            //weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.PiercingKeyword, Value = 25 });
                            //weaponDef.DamagePayload.ProjectilesPerShot = 10;
                            //weaponDef.DamagePayload.ParabolaHeightToLengthRatio = 0.5f;
                            //weaponDef.DamagePayload.AoeRadius = 3f;
                            //weaponDef.DamagePayload.Range = 30.0f;
                            //weaponDef.DamagePayload.BodyPartMultiplier = 2;
                            //weaponDef.DamagePayload.ObjectMultiplier = 10;
                            //weaponDef.SpreadRadius = 6f;
                        }
                        break;

                    // Danchev MG
                    case "434c4004-580f-10a4-995a-c5a64e6998dc": // PX_PoisonMachineGun_WeaponDef
                        weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShreddingKeyword, Value = 3 });
                        weaponDef.SpreadDegrees = 40.99f / 17;
                        break;
                    // Danchev AR
                    case "f3b83418-1363-18d4-4858-143901ea2d8e": // PX_AcidAssaultRifle_WeaponDef
                        weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShreddingKeyword, Value = 1 });
                        weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.AcidKeyword).Value = 10;
                        break;
                    // Slamstrike Shotgun
                    case "38ddbbc0-3bd4-5834-5a75-19bde3df3ab6": // FS_SlamstrikeShotgun_WeaponDef
                        weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.ShockKeyword).Value = 180;
                        break;
                    // Imhullu Acid grenade
                    case "4291806f-cc74-5b24-2ace-644a17b65bd9": // AN_AcidGrenade_WeaponDef
                        weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShockKeyword, Value = 240 });
                        break;
                    // Fire grenade
                    case "3880461b-4419-0cc4-5986-64bda2224e51": // NJ_IncindieryGrenade_WeaponDef
                        weaponDef.ManufactureMaterials = 26;
                        weaponDef.ManufactureTech = 10;
                        DamageKeywordPair damageKeywordPair2 = weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                        _ = weaponDef.DamagePayload.DamageKeywords.Remove(damageKeywordPair2);
                        break;
                    // Yggdrasil Virophage grenade
                    case "06825a27-029e-a414-d806-49c513dfce53": // PX_VirophageGrenade_WeaponDef
                        weaponDef.ManufactureMutagen = 10;
                        break;
                    default:
                        break;
                }
            }
        }
        public static void Change_PriestsHeadMutations()
        {
            // Screaming Head mutation
            TacticalItemDef tactcalItemDef = (TacticalItemDef)Repo.GetDef("c9a03d9e-0cf7-5e84-9ba6-cbb94e5eb0e1"); // AN_Priest_Head03_BodyPartDef
            tactcalItemDef.BodyPartAspectDef.WillPower = 8;
            List<AbilityDef> temp = tactcalItemDef.Abilities.ToList();
            temp.Add((AbilityDef)Repo.GetDef("758fd670-ceb2-e474-1b01-8388d47cd8e1")); // MindControlImmunity_AbilityDef
            tactcalItemDef.Abilities = temp.ToArray();

            // Judgement Head mutation
            tactcalItemDef = (TacticalItemDef)Repo.GetDef("4804dff5-a2f9-7dd4-3bb9-6f5ed215ccbc"); // AN_Priest_Head02_BodyPartDef
            tactcalItemDef.BodyPartAspectDef.WillPower = 4;
        }
        public static void Change_SpiderDrones()
        {
            int spiderDroneArmor = 10;

            TacticalItemDef spiderDrone = DefCache.GetDef<TacticalItemDef>("SpiderDrone_Torso_BodyPartDef");
            spiderDrone.Armor = spiderDroneArmor;
        }
        public static void Change_VenomTorso()
        {
            // Get Venom Torso def
            TacticalItemDef venomTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Shooter_Torso_BodyPartDef");
            // Set accuracy buff to 0, we don't want this when we can use 2 handed weapons!
            /*venomTorso.BodyPartAspectDef.Accuracy = 0;*/


            // Get poison spike weapon def
            WeaponDef poisonSpikeWeapon = DefCache.GetDef<WeaponDef>("AN_Berserker_Shooter_LeftArm_WeaponDef");
            // Add handgun item tag def for proficiency check

            GameTagDef handGunTag = DefCache.GetDef<GameTagDef>("HandgunItem_TagDef");

            if (!poisonSpikeWeapon.Tags.Contains(handGunTag))
            {
                poisonSpikeWeapon.Tags.Add(handGunTag);
            }
            // Add handgun prficiency to venom torso, makes the venom spikes to be a weapon with proficiency for several skills
            PassiveModifierAbilityDef handgunsProficiency = DefCache.GetDef<PassiveModifierAbilityDef>("HandgunsTalent_AbilityDef");
            if (!venomTorso.Abilities.Contains(handgunsProficiency))
            {
                venomTorso.Abilities = venomTorso.Abilities.AddToArray(handgunsProficiency);
            }

            // Add GunWeapon tag for ... idk ;-)
            poisonSpikeWeapon.Tags.Add(DefCache.GetDef<GameTagDef>("GunWeapon_TagDef"));
            // Set range to infinity (As all other direct line weapons), vanilla is set to 12.0
            poisonSpikeWeapon.DamagePayload.Range = float.PositiveInfinity;
            // Set ammo to unlimited (ChargesMax = 0)
            poisonSpikeWeapon.ChargesMax = 0;
            // Make 2 handed weapns usable => remove "UnusableLeftHand_AbilityDef"
            // => set it new to "ShootPoisonSpike_ShootAbilityDef"
            // add Overwatch just because :-)
            /*poisonSpikeWeapon.Abilities = new AbilityDef[]
            {
                DefCache.GetDef<AbilityDef>("ShootPoisonSpike_ShootAbilityDef"),
                DefCache.GetDef<AbilityDef>("Overwatch_AbilityDef")
            };*/
            // Buff accuracy to compensate the lost acc buff from torso 
            /*poisonSpikeWeapon.SpreadDegrees = 1.2f;*/ // default 1.8
        }
        public static void Change_HavenRecruits()
        {
            bool hasArmor = true;
            bool hasWeapon = true;

            GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");
            GameDifficultyLevelDef standard = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
            GameDifficultyLevelDef hard = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
            GameDifficultyLevelDef veryhard = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");

            easy.RecruitsGenerationParams.HasArmor = hasArmor;
            easy.RecruitsGenerationParams.HasWeapons = hasWeapon;
            standard.RecruitsGenerationParams.HasArmor = hasArmor;
            standard.RecruitsGenerationParams.HasWeapons = hasWeapon;
            hard.RecruitsGenerationParams.HasArmor = hasArmor;
            hard.RecruitsGenerationParams.HasWeapons = hasWeapon;
            veryhard.RecruitsGenerationParams.HasArmor = hasArmor;
            veryhard.RecruitsGenerationParams.HasWeapons = hasWeapon;
        }
        public static void Change_MechArms(SharedData shared)
        {
            int mechArmsShockDamage = 180;
            int mechArmsEMPDamage = 200;
            int usesPerTurn = 1;

            WeaponDef mechArms = DefCache.GetDef<WeaponDef>("NJ_Technician_MechArms_WeaponDef");
            // Bonus mech arms (has the same visuals, no need to change them)
            WeaponDef bonusMechArms = DefCache.GetDef<WeaponDef>("NJ_Technician_MechArms_ALN_WeaponDef");
            DamageKeywordDef emp = DefCache.GetDef<DamageKeywordDef>("EMP_DamageKeywordDataDef");
            mechArms.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShockKeyword, Value = mechArmsShockDamage },
                new DamageKeywordPair{DamageKeywordDef = emp, Value = mechArmsEMPDamage }
            };
            bonusMechArms.DamagePayload.DamageKeywords = mechArms.DamagePayload.DamageKeywords;
            // Set ability description and usage per turn TechnicianStrike_ShootAbilityDef
            BashAbilityDef techArmBashStrike = DefCache.GetDef<BashAbilityDef>("TechnicianBashStrike_AbilityDef");
            //techArmBashStrike.ViewElementDef.Description = new LocalizedTextBind("PR_BC_ELECTRIC_STRIKE_DESCRIPTION");
            //TFTVMain.Main.Logger.LogInfo($"TechnicianBashStrike_AbilityDef description: '{techArmBashStrike.ViewElementDef.Description.Localize()}'");
            //TFTVMain.Main.Logger.LogInfo($"TechnicianBashStrike_AbilityDef category: '{techArmBashStrike.ViewElementDef.Description.}'");
            techArmBashStrike.UsesPerTurn = usesPerTurn;
            // Change ammo cost for MechArms
            TacticalItemDef mechArmsAmmo = DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef");
            mechArmsAmmo.ManufactureMaterials = 55;
            mechArmsAmmo.ManufactureTech = 15;
        }
        public static void Change_ShadowLegs(SharedData shared)
        {
            int shadowLegsSonicDamage = 20;

            BashAbilityDef shadowLegs = DefCache.GetDef<BashAbilityDef>("ElectricKick_AbilityDef");

            shadowLegs.DamagePayload.DamageKeywords[0].DamageKeywordDef = shared.SharedDamageKeywords.SonicKeyword;
            shadowLegs.DamagePayload.DamageKeywords[0].Value = shadowLegsSonicDamage;

            BodyPartAspectDef shadowLegsAspectDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Watcher_Legs_ItemDef]");
            shadowLegsAspectDef.Speed = 1;
        }
        public static void Change_VidarGL(SharedData shared)
        {
            //int vGLNormal = 50;
            //int vGLShred = 20;
            //int vGLAcid = 10;
            int vGlAPCost = 50;

            WeaponDef vGL = DefCache.GetDef<WeaponDef>("FS_AssaultGrenadeLauncher_WeaponDef");

           
            //vGL.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            //{
            //    new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.BlastKeyword, Value = vGLNormal },
            //    new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShreddingKeyword, Value = vGLShred },
            //    new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.AcidKeyword, Value = vGLAcid },
            //};

            vGL.APToUsePerc = vGlAPCost;
            vGL.ChargesMax = 3;

            ItemDef vGLammo = DefCache.GetDef<ItemDef>("FS_AssaultGrenadeLauncher_AmmoClip_ItemDef");
            vGLammo.ChargesMax = 3;

            vGLammo.ManufactureMaterials = 34;
            vGLammo.ManufactureTech = 4;
            Sprite vGLammoIcon = Helper.CreateSpriteFromImageFile("Vidar_Ammo_3x_v3.png");
            vGLammo.ViewElementDef.InventoryIcon = vGLammoIcon;
            vGLammo.ViewElementDef.RosterIcon = vGLammoIcon;
        }
        public static void Change_Destiny()
        {
            WeaponDef destiny3 = DefCache.GetDef<WeaponDef>("PX_LaserArrayPack_WeaponDef");
            destiny3.FumblePerc = 50;
        }
    }

    /// <summary>
    /// Harmony Patch to fix a bug when ability buttons in tactical missions does not show on certain circumstances.
    /// 
    /// The original tacticalAbilityRowController.AbilitiesListMaxElements is set to 8 while the AbilitiesBarMaxElements is 14.
    /// The abilities are ordered in 3 groups (common, advanced and slot abilities) for each row to show in the UI.
    /// If one group has more than 8 members in any row than the UI bugs out and does not show the button plus the flash does colorize another button orange.
    /// This patch sets the AbilitiesListMaxElements equal to AbilitiesBarMaxElements so there are defenitely enough elements in the precreated UI element to not bug out.
    /// </summary>
    [HarmonyPatch(typeof(UIModuleAbilities), "Awake")]
    internal static class UIModuleAbilities_Awake_Patch
    {
        public static void Prefix(UIModuleAbilities __instance)
        {
            foreach (TacticalAbilityRowController tacticalAbilityRowController in __instance.AbilitiesBars)
            {
                tacticalAbilityRowController.AbilitiesListMaxElements = __instance.AbilitiesBarMaxElements;
            }
        }
    }
}
