using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;


namespace PRMBetterClasses.VariousAdjustments
{
    internal class VariousAdjustments
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ApplyChanges()
        {
            SharedData shared = GameUtl.GameComponent<SharedData>();

            // Change Advanced Laser research to require advanced technician weapons
            Change_AdvLaserResearch();
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
            Change_MutoidWorms();
            // Screaming Head: Mind Control Immunity
            Change_PriestsHeadMutations();
            // Spider Drones: Armor down to 10 (from 30)
            Change_SpiderDrones();
            // Danchev MG: ER buff to 14 (up from 9)
            Change_VariousWeapons(shared);
            // Venom Torso: Add Weapon Tag to Poison Arm 
            Change_VenomTorso();
            // Haven Recruits: Come with Armour and Weapons on all difficulties
            Change_HavenRecruits();
            // Mech Arms: 200 emp damage
            Change_MechArms(shared);
            // Vengeance Torso: Attacks against enemies within 10 tiles deal 10% more damage
            Change_VengeanceTorso();
            // Shadow Legs: Electric Kick replace shock damage with Sonic damage (value 20)
            Change_ShadowLegs(shared);
            // Vidar GL - Increase Shred to 20 (from 10), Add Acid 10. Increase AP cost to 2 (from 1)
            Change_VidarGL(shared);
            // Destiny III - Give chance to fumble when non-proficient
            Change_Destiny();
        }

        private static void Change_AdvLaserResearch()
        {
            ExistingResearchRequirementDef advLaserResearchRequirement = Repo.GetAllDefs<ExistingResearchRequirementDef>().FirstOrDefault(rrd => rrd.name.Equals("PX_AdvancedLaserTech_ResearchDef_ExistingResearchRequirementDef_1"));
            advLaserResearchRequirement.ResearchID = "NJ_PRCRTechTurret_ResearchDef";
        }

        private static void Change_Stimpack()
        {
            EquipmentDef stimpack = Repo.GetAllDefs<EquipmentDef>().FirstOrDefault(ed => ed.name.Equals("Stimpack_EquipmentDef"));
            stimpack.ViewElementDef.Description.LocalizationKey = "PR_BC_STIMPACK_ITEM_DESC";

            HealAbilityDef stimpackAbility = stimpack.GetAbilityDef<HealAbilityDef>();
            stimpackAbility.ViewElementDef.Description.LocalizationKey = "PR_BC_STIMPACK_ABILITY_DESC";
            stimpackAbility.ActionPointCost = 0.25f;
            stimpackAbility.HealBodyParts = true;
            stimpackAbility.BodyPartHealAmount = 10.0f;
            //stimpackAbility.HealEffects.Add(Repo.GetAllDefs<HealAbilityDef>().FirstOrDefault(has => has.name.Equals("FieldMedic_AbilityDef")).HealEffects[0]);

            TacActorSimpleInteractionAnimActionDef healAnimActionDef =
                Repo.GetAllDefs<TacActorSimpleInteractionAnimActionDef>().FirstOrDefault(aa => aa.name.Equals("E_MedkitHeal [Soldier_Utka_AnimActionsDef]"));
            healAnimActionDef.Items = healAnimActionDef.Items.AddToArray(stimpack);
        }

        private static void Change_Poison()
        {
            DamageOverTimeStatusDef poisonDOT = Repo.GetAllDefs<DamageOverTimeStatusDef>().FirstOrDefault(dot => dot.name.Equals("Poison_DamageOverTimeStatusDef"));
            poisonDOT.Visuals.Description.LocalizationKey = "PR_BC_POISON_STATUS_DESC";
        }
        // Make Trembling status accessible for Harmony patches to avoid time critical Repo calls in them.
        public static StatMultiplierStatusDef trembling = Repo.GetAllDefs<StatMultiplierStatusDef>().FirstOrDefault(sms => sms.name.Equals("Trembling_StatusDef"));
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
            // Juggernaut Torso & Armadillo Legs: Speed -1 -> 0
            BodyPartAspectDef juggTorsoAspect = Repo.GetAllDefs<BodyPartAspectDef>().FirstOrDefault(bpa1 => bpa1.name.Equals("E_BodyPartAspect [NJ_Jugg_BIO_Torso_BodyPartDef]"));
            BodyPartAspectDef juggLegsAspect = Repo.GetAllDefs<BodyPartAspectDef>().FirstOrDefault(bpa2 => bpa2.name.Equals("E_BodyPartAspect [NJ_Jugg_BIO_Legs_ItemDef]"));
            juggTorsoAspect.Speed = juggLegsAspect.Speed = 0;

            // Give mounted weapon slot to Juggernaut Torso 
            TacticalItemDef juggTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(ti1 => ti1.name.Equals("NJ_Jugg_BIO_Torso_BodyPartDef"));
            TacticalItemDef neuralTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(ti2 => ti2.name.Equals("NJ_Exo_BIO_Torso_BodyPartDef"));
            juggTorso.ProvidedSlots = neuralTorso.ProvidedSlots;

            // shield deploy AP cost 1 -> 0
            TacticalAbilityDef deployJuggShield = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("DeployShield_Bionic_AbilityDef"));
            deployJuggShield.ActionPointCost = 0;

            // Neural Torso: Grants Mounted Weapons and Tech Arms Proficiency (MountedWeaponTalent_AbilityDef = MountedItem_TagDef = proficiency with all mounted equipment)
            // First fix name and description of given mounted weapon talent that in fact gives mounted item proficiency also for robotic arms
            PassiveModifierAbilityDef mountedItemsProficiency = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(pma => pma.name.Equals("MountedWeaponTalent_AbilityDef"));
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
            BodyPartAspectDef mirageLegsAspect = Repo.GetAllDefs<BodyPartAspectDef>().FirstOrDefault(bpa2 => bpa2.name.Equals("E_BodyPartAspect [SY_Shinobi_BIO_Legs_ItemDef]"));
            mirageLegsAspect.Speed = 2;
        }

        public static void Change_Turrets()
        {
            int turretAPToUsePerc = 50;
            int turretArmor = 10;
            int turretAutoFireShotCount = 4;

            WeaponDef turret = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("NJ_TechTurretGun_WeaponDef"));
            turret.APToUsePerc = turretAPToUsePerc;
            turret.Armor = turretArmor;
            turret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;

            WeaponDef prcrTurret = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("NJ_PRCRTechTurretGun_WeaponDef"));
            prcrTurret.APToUsePerc = turretAPToUsePerc;
            prcrTurret.Armor = turretArmor;
            prcrTurret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;

            WeaponDef laserTurret = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("PX_LaserTechTurretGun_WeaponDef"));
            laserTurret.APToUsePerc = turretAPToUsePerc;
            laserTurret.Armor = turretArmor;
            laserTurret.DamagePayload.AutoFireShotCount = turretAutoFireShotCount;
        }
        public static void Change_Stomp(SharedData shared)
        {
            int StompShockValue = 200;
            int StompBlastValue = 50;

            ApplyDamageEffectAbilityDef stomp = Repo.GetAllDefs<ApplyDamageEffectAbilityDef>().FirstOrDefault(p => p.name.Equals("StomperLegs_Stomp_AbilityDef"));
            stomp.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShockKeyword, Value = StompShockValue },
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.BlastKeyword, Value = StompBlastValue },
                };
        }
        public static void Change_Frenzy()
        {
            float frenzySpeed = 0.33f;

            FrenzyStatusDef frenzy = Repo.GetAllDefs<FrenzyStatusDef>().FirstOrDefault(p => p.name.Equals("Frenzy_StatusDef"));
            frenzy.SpeedCoefficient = frenzySpeed;
            //LocalizedTextBind description = new LocalizedTextBind("", PRMBetterClassesMain.Main.Settings.DoNotLocalizeChangedTexts);
            foreach (ViewElementDef visuals in Repo.GetAllDefs<ViewElementDef>().Where(tav => tav.name.Contains("Frenzy_")))
            {
                visuals.Description.LocalizationKey = visuals.name.Contains("Status") ? "PR_BC_FRENZY_STATUS_DESC" : "PR_BC_FRENZY_DESC";
            }
        }
        public static void Change_PsychicResistance()
        {

        }
        public static void Change_MutoidWorms()
        {
            int mutoidWormCharges = 5;
            float range = 25.0f;

            WeaponDef mAWorm = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("Mutoid_Arm_AcidWorm_WeaponDef"));
            WeaponDef mFWorm = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("Mutoid_Arm_FireWorm_WeaponDef"));
            WeaponDef mPWorm = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("Mutoid_Arm_PoisonWorm_WeaponDef"));

            mAWorm.ChargesMax = mutoidWormCharges;
            mAWorm.DamagePayload.Range = range;
            mFWorm.ChargesMax = mutoidWormCharges;
            mFWorm.DamagePayload.Range = range;
            mPWorm.ChargesMax = mutoidWormCharges;
            mPWorm.DamagePayload.Range = range;
        }
        public static void Change_PriestsHeadMutations()
        {
            foreach (TacticalItemDef tacticalItem in Repo.GetAllDefs<TacticalItemDef>())
            {
                // Screaming Head mutation
                if (tacticalItem.name.Equals("AN_Priest_Head03_BodyPartDef"))
                {
                    tacticalItem.BodyPartAspectDef.WillPower = 8;
                    tacticalItem.Abilities = new AbilityDef[]
                    {
                        tacticalItem.Abilities[0],
                        Repo.GetAllDefs<AbilityDef>().FirstOrDefault(p => p.name.Equals("MindControlImmunity_AbilityDef"))
                    };
                }
                // Judgement Head mutation
                if (tacticalItem.name.Equals("AN_Priest_Head02_BodyPartDef"))
                {
                    tacticalItem.BodyPartAspectDef.WillPower = 4;
                }
            }
        }
        public static void Change_SpiderDrones()
        {
            int spiderDroneArmor = 10;

            TacticalItemDef spiderDrone = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("SpiderDrone_Torso_BodyPartDef"));
            spiderDrone.Armor = spiderDroneArmor;
        }
        public static void Change_VariousWeapons(SharedData shared)
        {
            SharedDamageKeywordsDataDef damageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
            foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
            {
                // Danchev MG
                if (weaponDef.name.Equals("PX_PoisonMachineGun_WeaponDef"))
                {
                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = shared.SharedDamageKeywords.ShreddingKeyword, Value = 3 });
                    weaponDef.SpreadDegrees = 40.99f / 17;
                }
                // Danchev AR
                if (weaponDef.name.Equals("PX_AcidAssaultRifle_WeaponDef"))
                {
                    weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = shared.SharedDamageKeywords.ShreddingKeyword, Value = 1 });
                    weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == shared.SharedDamageKeywords.AcidKeyword).Value = 10;
                }
                // Slamstrike Shotgun
                if (weaponDef.name.Equals("FS_SlamstrikeShotgun_WeaponDef"))
                {
                    weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == shared.SharedDamageKeywords.ShockKeyword).Value = 180;
                }
                // Grenades
                if (weaponDef.name.EndsWith("Grenade_WeaponDef") && weaponDef.Tags.Contains(shared.SharedGameTags.StandaloneTag))
                {
                    // Manufature intantly
                    weaponDef.ManufacturePointsCost = 0;
                    // Imhullu Acid grenade
                    if (weaponDef.name.Equals("AN_AcidGrenade_WeaponDef"))
                    {
                        weaponDef.DamagePayload.DamageKeywords.Add(new DamageKeywordPair { DamageKeywordDef = damageKeywords.ShockKeyword, Value = 240 });
                    }
                    // Fire grenade
                    if (weaponDef.name.Equals("NJ_IncindieryGrenade_WeaponDef"))
                    {
                        weaponDef.ManufactureMaterials = 26;
                        weaponDef.ManufactureTech = 10;
                        DamageKeywordPair damageKeywordPair = weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.ShreddingKeyword);
                        _ = weaponDef.DamagePayload.DamageKeywords.Remove(damageKeywordPair);
                    }
                }
            }
        }
        public static void Change_VenomTorso()
        {
            WeaponDef venomTorso = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("AN_Berserker_Shooter_LeftArm_WeaponDef"));

            venomTorso.Tags = new GameTagsList()
            {
                venomTorso.Tags[0],
                venomTorso.Tags[1],
                venomTorso.Tags[2],
                Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("GunWeapon_TagDef"))
            };
        }
        public static void Change_HavenRecruits()
        {
            bool hasArmor = true;
            bool hasWeapon = true;

            GameDifficultyLevelDef easy = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Easy_GameDifficultyLevelDef"));
            GameDifficultyLevelDef standard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Standard_GameDifficultyLevelDef"));
            GameDifficultyLevelDef hard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("Hard_GameDifficultyLevelDef"));
            GameDifficultyLevelDef veryhard = Repo.GetAllDefs<GameDifficultyLevelDef>().FirstOrDefault(a => a.name.Equals("VeryHard_GameDifficultyLevelDef"));

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

            WeaponDef mechArms = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("NJ_Technician_MechArms_WeaponDef"));
            DamageKeywordDef emp = Repo.GetAllDefs<DamageKeywordDef>().FirstOrDefault(p => p.name.Equals("EMP_DamageKeywordDataDef"));
            mechArms.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShockKeyword, Value = mechArmsShockDamage },
                new DamageKeywordPair{DamageKeywordDef = emp, Value = mechArmsEMPDamage }
            };
            ShootAbilityDef techArmStrike = Repo.GetAllDefs<ShootAbilityDef>().FirstOrDefault(s => s.name.Equals("TechnicianStrike_ShootAbilityDef"));
            techArmStrike.UsesPerTurn = usesPerTurn;
            // Change ammo cost for MechArms
            TacticalItemDef mechArmsAmmo = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(a => a.name.Equals("MechArms_AmmoClip_ItemDef"));
            mechArmsAmmo.ManufactureMaterials = 55;
            mechArmsAmmo.ManufactureTech = 15;
        }
        public static void Change_VengeanceTorso()
        {
            TacticalItemDef vTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("SY_Shinobi_BIO_Torso_BodyPartDef"));

            vTorso.Abilities[1] = Repo.GetAllDefs<AbilityDef>().FirstOrDefault(p => p.name.Equals("BattleFocus_AbilityDef"));
        }
        public static void Change_ShadowLegs(SharedData shared)
        {
            int shadowLegsSonicDamage = 20;

            BashAbilityDef shadowLegs = Repo.GetAllDefs<BashAbilityDef>().FirstOrDefault(p => p.name.Equals("ElectricKick_AbilityDef"));

            shadowLegs.DamagePayload.DamageKeywords[0].DamageKeywordDef = shared.SharedDamageKeywords.SonicKeyword;
            shadowLegs.DamagePayload.DamageKeywords[0].Value = shadowLegsSonicDamage;

            BodyPartAspectDef shadowLegsAspectDef = Repo.GetAllDefs<BodyPartAspectDef>().FirstOrDefault(bpa => bpa.name.Equals("E_BodyPartAspect [AN_Berserker_Shooter_Legs_ItemDef]"));
            shadowLegsAspectDef.Speed = 1;
        }
        public static void Change_VidarGL(SharedData shared)
        {
            int vGLNormal = 50;
            int vGLShred = 20;
            int vGLAcid = 10;
            int vGlAPCost = 50;

            WeaponDef vGL = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("FS_AssaultGrenadeLauncher_WeaponDef"));

            vGL.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.BlastKeyword, Value = vGLNormal },
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.ShreddingKeyword, Value = vGLShred },
                new DamageKeywordPair{DamageKeywordDef = shared.SharedDamageKeywords.AcidKeyword, Value = vGLAcid },
            };

            vGL.APToUsePerc = vGlAPCost;

        }
        public static void Change_Destiny()
        {
            WeaponDef destiny3 = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Equals("PX_LaserArrayPack_WeaponDef"));
            destiny3.FumblePerc = 50;
        }
    }
}
