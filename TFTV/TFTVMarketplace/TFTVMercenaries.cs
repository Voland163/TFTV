using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Entities.Abilities;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV.TFTVBaseRework;
using UnityEngine;
using static PhoenixPoint.Geoscape.Entities.GeoUnitDescriptor;

namespace TFTV
{


    internal partial class TFTVChangesToDLC5
    {
        internal class TFTVMercenaries
        {
            internal static ClassTagDef assaultTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
            internal static ClassTagDef heavyTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
            internal static ClassTagDef infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");
            internal static ClassTagDef sniperTag = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
            internal static ClassTagDef priestTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
            internal static ClassTagDef technicianTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");
            internal static ClassTagDef berserkerTag = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");

            internal static TacticalItemDef goldAssaultHelmet = DefCache.GetDef<TacticalItemDef>("PX_Assault_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultLegs = DefCache.GetDef<TacticalItemDef>("PX_Assault_Legs_Gold_ItemDef");

            internal static List<TacticalItemDef> goldAssaultArmor = new List<TacticalItemDef>() { goldAssaultHelmet, goldAssaultTorso, goldAssaultLegs };

            internal static TacticalItemDef goldHeavyHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavyTorso = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavytLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Gold_ItemDef");
            internal static TacticalItemDef goldHeavytJetpack = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_JumpPack_Gold_BodyPartDef");

            internal static List<TacticalItemDef> goldHeavyArmor = new List<TacticalItemDef>() { goldHeavyHelmet, goldHeavytJetpack, goldHeavytLegs, goldHeavyTorso };

            internal static TacticalItemDef goldSniperHelmet = DefCache.GetDef<TacticalItemDef>("PX_Sniper_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldSniperTorso = DefCache.GetDef<TacticalItemDef>("PX_Sniper_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldSniperLegs = DefCache.GetDef<TacticalItemDef>("PX_Sniper_Legs_Gold_ItemDef");

            internal static List<TacticalItemDef> goldSniperArmor = new List<TacticalItemDef>() { goldSniperHelmet, goldSniperTorso, goldSniperLegs };

            internal static TacticalItemDef spyMasterTorso = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef");
            internal static TacticalItemDef spyMasterHelmet = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef");
            internal static TacticalItemDef spyMasterLegs = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef");

            internal static List<TacticalItemDef> spyMasterArmor = new List<TacticalItemDef>() { spyMasterHelmet, spyMasterLegs, spyMasterTorso };

            internal static TacticalItemDef sectarianHelmet = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_Viking_ItemDef");

            internal static List<TacticalItemDef> sectarianArmor = new List<TacticalItemDef>() { sectarianHelmet, sectarianLegs, sectarianTorso };

            internal static TacticalItemDef ghostTorso = DefCache.GetDef<TacticalItemDef>("SY_Assault_Torso_Neon_BodyPartDef");
            internal static TacticalItemDef ghostHelmet = DefCache.GetDef<TacticalItemDef>("SY_Assault_Helmet_Neon_BodyPartDef");
            internal static TacticalItemDef ghostLegs = DefCache.GetDef<TacticalItemDef>("SY_Assault_Legs_Neon_ItemDef");

            internal static List<TacticalItemDef> ghostArmor = new List<TacticalItemDef>() { ghostHelmet, ghostTorso, ghostLegs };

            internal static TacticalItemDef exileHelmet = DefCache.GetDef<TacticalItemDef>("SY_Assault_Helmet_WhiteNeon_BodyPartDef");
            internal static TacticalItemDef exileTorso = DefCache.GetDef<TacticalItemDef>("SY_Assault_Torso_WhiteNeon_BodyPartDef");
            internal static TacticalItemDef exileLegs = DefCache.GetDef<TacticalItemDef>("SY_Assault_Legs_WhiteNeon_ItemDef");

            internal static List<TacticalItemDef> exileArmor = new List<TacticalItemDef>() { exileHelmet, exileLegs, exileTorso };

            internal static TacticalItemDef slugHelmet = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Helmet_ALN_BodyPartDef");
            internal static TacticalItemDef slugLegs = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Legs_ALN_ItemDef");
            internal static TacticalItemDef slugTorso = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Torso_ALN_BodyPartDef");
            internal static TacticalItemDef slugMechArms = DefCache.GetDef<TacticalItemDef>("NJ_Technician_MechArms_ALN_WeaponDef");

            internal static List<TacticalItemDef> slugArmor = new List<TacticalItemDef>() { slugHelmet, slugLegs, slugTorso, slugMechArms };

            internal static TacticalItemDef doomLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Headhunter_ItemDef");
            internal static TacticalItemDef doomTorso = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomJetpack = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_JumpPack_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Headhunter_BodyPartDef");

            internal static List<TacticalItemDef> doomArmor = new List<TacticalItemDef>() { doomHelmet, doomTorso, doomLegs };

            internal static WeaponDef spyMasterXbow = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
            internal static WeaponDef ghostSniperRifle = DefCache.GetDef<WeaponDef>("NE_SniperRifle_WeaponDef");
            internal static WeaponDef slugPistol = DefCache.GetDef<WeaponDef>("NE_Pistol_WeaponDef");
            internal static WeaponDef exileAssaultRifle = DefCache.GetDef<WeaponDef>("NE_AssaultRifle_WeaponDef");
            internal static WeaponDef doomAC = DefCache.GetDef<WeaponDef>("PX_HeavyCannon_Headhunter_WeaponDef");
            internal static WeaponDef sectarianAxe = DefCache.GetDef<WeaponDef>("AN_Blade_Viking_WeaponDef");

            internal static HealAbilityDef SlugTechnicianRepair;
            internal static HealAbilityDef SlugTechnicianHeal;
            internal static HealAbilityDef SlugTechnicianRestore;
            internal static HealAbilityDef SlugFieldMedic;
            internal static RemoveFacehuggerAbilityDef SlugRemoveFaceHugger;
            internal static BashAbilityDef SlugTechnicianZap;

            internal static List<TacticalAbilityDef> SlugTacticalAbilities = new List<TacticalAbilityDef>()
            {
            SlugTechnicianRepair, SlugTechnicianHeal, SlugTechnicianRestore, SlugFieldMedic, SlugRemoveFaceHugger
            };


            internal static SpecializationDef SlugSpecialization;
            internal static ClassTagDef SlugClassTagDef;


            internal class Defs
            {
                public static void CreateMercenariesDefs()
                {
                    try
                    {
                        MakeSlugArmorNonRemovable();
                        CreateExpendableArchetypes();
                        AdjustMercenaryArmorsAndWeapons();
                        ChangeSlugArms();
                        CloneTechnicianSpec();
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static TacCharacterDef CreateTacCharaterDef(ClassTagDef classTagDef, string name, string gUID,
          WeaponDef weaponDef, List<TacticalItemDef> armorSlots, List<GameTagDef> tags, int level, int[] stats)
                {
                    try
                    {
                        //  GeoUnitDescriptor

                        TacCharacterDef characterSource = DefCache.GetDef<TacCharacterDef>("AN_Assault1_CharacterTemplateDef");
                        TacCharacterDef newCharacter = Helper.CreateDefFromClone(characterSource, gUID, name);

                        newCharacter.SpawnCommandId = name;
                        newCharacter.Data.Name = name;
                        newCharacter.Data.GameTags = tags != null ? new List<GameTagDef>(tags) { classTagDef }.ToArray() : new List<GameTagDef>() { classTagDef }.ToArray();
                        newCharacter.Data.EquipmentItems = new ItemDef[] { weaponDef };
                        newCharacter.Data.BodypartItems = armorSlots?.ToArray() ?? new ItemDef[] { };
                        newCharacter.Data.LevelProgression.SetLevel(level);

                        if (stats != null)
                        {
                            newCharacter.Data.Strength = stats[0];
                            newCharacter.Data.Will = stats[1];
                            newCharacter.Data.Speed = stats[2];
                        }

                        return newCharacter;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                private static void CloneTechnicianSpec()
                {
                    try
                    {
                        SlugClassTagDef = Helper.CreateDefFromClone(technicianTag, "{E513B9D9-D461-4274-B903-C24A98FD3B6B}", $"Slug_ClassTagDef");
                        SpecializationDef specializationDefSource = DefCache.GetDef<SpecializationDef>("TechnicianSpecializationDef");
                        SpecializationDef newSpec = Helper.CreateDefFromClone(specializationDefSource, "{B680BA89-C421-4D09-8ACC-F08CC7F19E24}", $"SlugSpecializationDef");
                        newSpec.ViewElementDef = Helper.CreateDefFromClone(specializationDefSource.ViewElementDef, "{7D5C1467-3DCC-4A33-95FA-91E4AE42F9CE}", $"{newSpec.name}");
                        newSpec.AbilityTrack = Helper.CreateDefFromClone(specializationDefSource.AbilityTrack, "{14CDBFDA-DE81-48BA-B8DD-AC2192858982}", $"{newSpec.name}");

                        newSpec.AbilityTrack.AbilitiesByLevel[5].Ability = SlugFieldMedic;

                        if (TFTVAircraftReworkMain.AircraftReworkOn
                           && newSpec.AbilityTrack?.AbilitiesByLevel != null
                           && newSpec.AbilityTrack.AbilitiesByLevel.Length > 6)
                        {
                            TacticalAbilityDef amplifyPain = DefCache.GetDef<TacticalAbilityDef>("AmplifyPain_AbilityDef");
                            if (amplifyPain != null)
                            {
                                AbilityTrackSlot slugLevelSeven = newSpec.AbilityTrack.AbilitiesByLevel[6];
                                slugLevelSeven.Ability = amplifyPain;
                                newSpec.AbilityTrack.AbilitiesByLevel[6] = slugLevelSeven;
                            }
                        }
                        // newSpec.ClassTag = SlugClassTagDef;

                        SlugSpecialization = newSpec;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustMercenaryArmorsAndWeapons()
                {
                    try
                    {
                        //From Belial's spreadsheet 

                        doomHelmet.ViewElementDef = Helper.CreateDefFromClone(doomHelmet.ViewElementDef, "{D618A564-1508-471E-94D8-518DBA2F3953}", doomHelmet.name);
                        doomHelmet.ViewElementDef.Description.LocalizationKey = $"DOOM_SLAYER_HELMET_NAME";
                        doomHelmet.ViewElementDef.DisplayName2.LocalizationKey = $"DOOM_SLAYER_HELMET_DESCRIPTION";

                        doomTorso.ViewElementDef = Helper.CreateDefFromClone(doomTorso.ViewElementDef, "{A3CCA23F-A1D6-4A0F-9576-083B52209D38}", doomTorso.name);
                        doomTorso.ViewElementDef.Description.LocalizationKey = $"DOOM_SLAYER_TORSO_NAME";
                        doomTorso.ViewElementDef.DisplayName2.LocalizationKey = $"DOOM_SLAYER_TORSO_DESCRIPTION";

                        doomLegs.ViewElementDef = Helper.CreateDefFromClone(doomLegs.ViewElementDef, "{6158C99A-6BAB-4D6D-B01F-6E087FD85ED0}", doomLegs.name);
                        doomLegs.ViewElementDef.Description.LocalizationKey = $"DOOM_SLAYER_LEGS_NAME";
                        doomLegs.ViewElementDef.DisplayName2.LocalizationKey = $"DOOM_SLAYER_LEGS_DESCRIPTION";

                        doomHelmet.Armor = 23;
                        doomTorso.Armor = 24;
                        DefCache.GetDef<TacticalItemDef>("PX_Heavy_LeftArm_Headhunter_BodyPartDef").Armor = 24;
                        DefCache.GetDef<TacticalItemDef>("PX_Heavy_RightArm_Headhunter_BodyPartDef").Armor = 24;


                        doomLegs.Armor = 22;
                        DefCache.GetDef<TacticalItemDef>("PX_Heavy_LeftLeg_Headhunter_BodyPartDef").Armor = 22;
                        DefCache.GetDef<TacticalItemDef>("PX_Heavy_RightLeg_Headhunter_BodyPartDef").Armor = 22;

                        doomHelmet.BodyPartAspectDef.Accuracy = -0.06f;
                        doomHelmet.BodyPartAspectDef.Perception = 0.0f;

                        doomTorso.BodyPartAspectDef.Accuracy = -0.04f;
                        doomLegs.BodyPartAspectDef.Speed = 0.0f;
                        doomLegs.BodyPartAspectDef.Accuracy = -0.04f;

                        sectarianHelmet.Armor = 14;
                        sectarianHelmet.Weight = 1;
                        sectarianHelmet.BodyPartAspectDef.WillPower = 1f;
                        sectarianHelmet.BodyPartAspectDef.Perception = -5f;
                        sectarianHelmet.BodyPartAspectDef.Stealth = -0.05f;

                        sectarianHelmet.ViewElementDef = Helper.CreateDefFromClone(sectarianHelmet.ViewElementDef, "{94121874-BC28-4E01-A986-36E28854BB5E}", sectarianHelmet.name);
                        sectarianHelmet.ViewElementDef.Description.LocalizationKey = $"SECTARIAN_HELMET_NAME";
                        sectarianHelmet.ViewElementDef.DisplayName2.LocalizationKey = $"SECTARIAN_HELMET_DESCRIPTION";

                        sectarianTorso.ViewElementDef = Helper.CreateDefFromClone(sectarianTorso.ViewElementDef, "{ED8A94B2-F867-4FB7-852A-624301A15F00}", sectarianTorso.name);
                        sectarianTorso.ViewElementDef.Description.LocalizationKey = $"SECTARIAN_TORSO_NAME";
                        sectarianTorso.ViewElementDef.DisplayName2.LocalizationKey = $"SECTARIAN_TORSO_DESCRIPTION";

                        sectarianLegs.ViewElementDef = Helper.CreateDefFromClone(sectarianLegs.ViewElementDef, "{820C3551-507A-4C54-B6E4-F32195AAA00C}", sectarianLegs.name);
                        sectarianLegs.ViewElementDef.Description.LocalizationKey = $"SECTARIAN_LEGS_NAME";
                        sectarianLegs.ViewElementDef.DisplayName2.LocalizationKey = $"SECTARIAN_LEGS_DESCRIPTION";

                        sectarianTorso.Armor = 18;
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_LeftArm_Viking_BodyPartDef").Armor = 18;
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_RightArm_Viking_BodyPartDef").Armor = 18;

                        sectarianTorso.Weight = 3;
                        sectarianTorso.BodyPartAspectDef.Endurance = 2;
                        sectarianTorso.BodyPartAspectDef.Speed = 0f;
                        sectarianTorso.BodyPartAspectDef.Stealth = -0.1f;
                        sectarianTorso.BodyPartAspectDef.Accuracy = -0.05f;

                        sectarianLegs.Armor = 14;
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_LeftLeg_Viking_BodyPartDef").Armor = 14;
                        DefCache.GetDef<TacticalItemDef>("AN_Berserker_RightLeg_Viking_BodyPartDef").Armor = 14;
                        sectarianLegs.Weight = 2;
                        sectarianLegs.BodyPartAspectDef.Endurance = 1;
                        sectarianLegs.BodyPartAspectDef.Speed = 1;
                        sectarianLegs.BodyPartAspectDef.Stealth = -0.1f;
                        sectarianLegs.BodyPartAspectDef.Accuracy = -0.05f;

                        ghostHelmet.ViewElementDef = Helper.CreateDefFromClone(ghostHelmet.ViewElementDef, "{9F9E3468-2771-4267-BF3B-8214DD908D78}", ghostHelmet.name);
                        ghostHelmet.ViewElementDef.Description.LocalizationKey = $"GHOST_HELMET_NAME";
                        ghostHelmet.ViewElementDef.DisplayName2.LocalizationKey = $"GHOST_HELMET_DESCRIPTION";

                        ghostLegs.ViewElementDef = Helper.CreateDefFromClone(ghostLegs.ViewElementDef, "{5AAFFFC7-E143-4FC4-86E1-B00752037483}", ghostLegs.name);
                        ghostLegs.ViewElementDef.Description.LocalizationKey = $"GHOST_LEGS_NAME";
                        ghostLegs.ViewElementDef.DisplayName2.LocalizationKey = $"GHOST_LEGS_DESCRIPTION";

                        ghostTorso.ViewElementDef = Helper.CreateDefFromClone(ghostTorso.ViewElementDef, "{69973138-4F25-49D8-8E30-167E53CC253B}", ghostTorso.name);
                        ghostTorso.ViewElementDef.Description.LocalizationKey = $"GHOST_TORSO_NAME";
                        ghostTorso.ViewElementDef.DisplayName2.LocalizationKey = $"GHOST_TORSO_DESCRIPTION";

                        ghostHelmet.Armor = 14;
                        ghostHelmet.BodyPartAspectDef.Stealth = 0.05f;

                        ghostTorso.Armor = 16;
                        DefCache.GetDef<TacticalItemDef>("SY_Assault_LeftArm_Neon_BodyPartDef").Armor = 16;
                        DefCache.GetDef<TacticalItemDef>("SY_Assault_RightArm_Neon_BodyPartDef").Armor = 16;
                        ghostTorso.BodyPartAspectDef.Stealth = 0.1f;

                        ghostLegs.Armor = 14;
                        DefCache.GetDef<TacticalItemDef>("SY_Assault_LeftLeg_Neon_BodyPartDef").Armor = 14;
                        DefCache.GetDef<TacticalItemDef>("SY_Assault_RightLeg_Neon_BodyPartDef").Armor = 14;
                        ghostLegs.BodyPartAspectDef.Stealth = 0.05f;

                        spyMasterHelmet.ViewElementDef = Helper.CreateDefFromClone(spyMasterHelmet.ViewElementDef, "{8CFFE705-55CA-4370-BDD7-7132423C9F4F}", spyMasterHelmet.name);
                        spyMasterHelmet.ViewElementDef.Description.LocalizationKey = $"SPYMASTER_HELMET_NAME";
                        spyMasterHelmet.ViewElementDef.DisplayName2.LocalizationKey = $"SPYMASTER_HELMET_DESCRIPTION";

                        spyMasterTorso.ViewElementDef = Helper.CreateDefFromClone(spyMasterTorso.ViewElementDef, "{ACCAB3BF-24B6-40E9-8D14-1095E4117FFF}", spyMasterTorso.name);
                        spyMasterTorso.ViewElementDef.Description.LocalizationKey = $"SPYMASTER_TORSO_NAME";
                        spyMasterTorso.ViewElementDef.DisplayName2.LocalizationKey = $"SPYMASTER_TORSO_DESCRIPTION";

                        spyMasterLegs.ViewElementDef = Helper.CreateDefFromClone(spyMasterLegs.ViewElementDef, "{40A39154-49E0-4F3F-9169-00732BD8F07F}", spyMasterLegs.name);
                        spyMasterLegs.ViewElementDef.Description.LocalizationKey = $"SPYMASTER_LEGS_NAME";
                        spyMasterLegs.ViewElementDef.DisplayName2.LocalizationKey = $"SPYMASTER_LEGS_DESCRIPTION";

                        spyMasterHelmet.Weight = 1;
                        spyMasterHelmet.ManufactureMaterials = 0;
                        spyMasterHelmet.ManufactureTech = 0;
                        spyMasterHelmet.BodyPartAspectDef.Perception = 4;

                        spyMasterTorso.Weight = 2;
                        spyMasterTorso.ForceShowInPortratis = true;
                        spyMasterTorso.BodyPartAspectDef.Speed = -1;
                        spyMasterTorso.ManufactureMaterials = 0;
                        spyMasterTorso.ManufactureTech = 0;

                        spyMasterLegs.Weight = 2;
                        spyMasterLegs.BodyPartAspectDef.Speed = -1;
                        spyMasterLegs.ManufactureMaterials = 0;
                        spyMasterLegs.ManufactureTech = 0;

                        spyMasterXbow.ManufactureTech = 0;
                        spyMasterXbow.ManufactureMaterials = 0;

                        slugHelmet.ViewElementDef = Helper.CreateDefFromClone(slugHelmet.ViewElementDef, "{9B73B5BD-FAD7-4451-949C-2D6F66968AAA}", slugHelmet.name);
                        slugHelmet.ViewElementDef.Description.LocalizationKey = $"SLUG_HELMET_NAME";
                        slugHelmet.ViewElementDef.DisplayName2.LocalizationKey = $"SLUG_HELMET_DESCRIPTION";

                        slugTorso.ViewElementDef = Helper.CreateDefFromClone(slugTorso.ViewElementDef, "{A14755F4-50EE-40F4-AF58-ECF449002E98}", slugTorso.name);
                        slugTorso.ViewElementDef.Description.LocalizationKey = $"SLUG_TORSO_NAME";
                        slugTorso.ViewElementDef.DisplayName2.LocalizationKey = $"SLUG_TORSO_DESCRIPTION";

                        slugLegs.ViewElementDef = Helper.CreateDefFromClone(slugLegs.ViewElementDef, "{E7B0B281-E919-4599-9E37-B5F7348BA2B2}", slugLegs.name);
                        slugLegs.ViewElementDef.Description.LocalizationKey = $"SLUG_LEGS_NAME";
                        slugLegs.ViewElementDef.DisplayName2.LocalizationKey = $"SLUG_LEGS_DESCRIPTION";

                        slugHelmet.Tags.Add(MercenaryTag);
                        slugHelmet.Tags.Add(Shared.SharedGameTags.BionicalTag);
                        slugHelmet.Armor = 20;
                        slugHelmet.Weight = 0;
                        slugHelmet.BodyPartAspectDef.Endurance = 1;
                        slugHelmet.BodyPartAspectDef.Accuracy = 0;
                        slugTorso.BodyPartAspectDef.Stealth = 0;

                        slugTorso.Tags.Add(MercenaryTag);
                        slugTorso.Tags.Add(Shared.SharedGameTags.BionicalTag);

                        slugTorso.Armor = 20;
                        DefCache.GetDef<TacticalItemDef>("NJ_Technician_LeftArm_ALN_BodyPartDef").Armor = 20;
                        DefCache.GetDef<TacticalItemDef>("NJ_Technician_RightArm_ALN_BodyPartDef").Armor = 20;
                        slugTorso.Weight = 0;
                        slugTorso.BodyPartAspectDef.Endurance = 1;
                        slugTorso.BodyPartAspectDef.Speed = 0;
                        slugTorso.BodyPartAspectDef.Accuracy = 0;
                        slugTorso.BodyPartAspectDef.Stealth = -0.1f;

                        slugLegs.Tags.Add(MercenaryTag);
                        slugLegs.Tags.Add(Shared.SharedGameTags.BionicalTag);

                        slugLegs.Armor = 20;
                        DefCache.GetDef<TacticalItemDef>("NJ_Technician_LeftLeg_ALN_BodyPartDef").Armor = 20;
                        DefCache.GetDef<TacticalItemDef>("NJ_Technician_RightLeg_ALN_BodyPartDef").Armor = 20;
                        slugLegs.Weight = 0;
                        slugLegs.BodyPartAspectDef.Endurance = 1;
                        slugLegs.BodyPartAspectDef.Speed = 0;
                        slugLegs.BodyPartAspectDef.Accuracy = 0;
                        slugLegs.BodyPartAspectDef.Stealth = -0.05f;

                        exileHelmet.ViewElementDef = Helper.CreateDefFromClone(exileHelmet.ViewElementDef, "{16AD85B5-7C8F-4EC8-9E74-CF23DBE3CE98}", exileHelmet.name);
                        //  exileHelmet.ViewElementDef.Description.LocalizationKey = "EXILE_HELMET_NAME";
                        exileHelmet.ViewElementDef.DisplayName2.LocalizationKey = "EXILE_HELMET_DESCRIPTION";

                        exileTorso.ViewElementDef = Helper.CreateDefFromClone(exileTorso.ViewElementDef, "{58D71B43-BCC4-41D0-BEC5-1ACCFADB9D63}", exileTorso.name);
                        //  exileTorso.ViewElementDef.Description.LocalizationKey = "EXILE_TORSO_NAME";
                        exileTorso.ViewElementDef.DisplayName2.LocalizationKey = "EXILE_TORSO_DESCRIPTION";

                        exileLegs.ViewElementDef = Helper.CreateDefFromClone(exileLegs.ViewElementDef, "{334A8591-4FCC-45FB-AB39-37B7AFBD4AB0}", exileLegs.name);
                        //  exileLegs.ViewElementDef.Description.LocalizationKey = "EXILE_LEGS_NAME";
                        exileLegs.ViewElementDef.DisplayName2.LocalizationKey = "EXILE_LEGS_DESCRIPTION";

                        doomAC.CompatibleAmmunition = new TacticalItemDef[] { DefCache.GetDef<TacticalItemDef>("FS_Autocannon_AmmoClip_ItemDef") };
                        doomAC.DamagePayload.DamageKeywords = new List<PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair>()
                    {
                    new PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair()
                    {
                    DamageKeywordDef=Shared.SharedDamageKeywords.DamageKeyword, Value = 90

                    },
                    new PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair()
                    {
                    DamageKeywordDef=Shared.SharedDamageKeywords.ShockKeyword, Value = 120
                    }
                    };
                        doomAC.ChargesMax = 12;
                        doomAC.DamagePayload.AutoFireShotCount = 2;

                        doomAC.ViewElementDef = Helper.CreateDefFromClone(doomAC.ViewElementDef, "{3973F99D-1CB9-4E14-9515-F37B9CC1C668}", doomAC.name);
                        doomAC.ViewElementDef.Description.LocalizationKey = $"DOOM_SLAYER_GROM_NAME";
                        doomAC.ViewElementDef.DisplayName1.LocalizationKey = $"DOOM_SLAYER_GROM_NAME";
                        doomAC.ViewElementDef.DisplayName2.LocalizationKey = $"DOOM_SLAYER_GROM_DESCRIPTION";

                        sectarianAxe.DamagePayload.DamageKeywords = new List<PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair>()
                    {
                    new PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair()
                    {
                    DamageKeywordDef=Shared.SharedDamageKeywords.DamageKeyword, Value = 160

                    },
                    new PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair()
                    {
                    DamageKeywordDef=Shared.SharedDamageKeywords.PiercingKeyword, Value = 20
                    },
                     new PhoenixPoint.Tactical.Entities.DamageKeywords.DamageKeywordPair()
                    {
                    DamageKeywordDef=Shared.SharedDamageKeywords.BleedingKeyword, Value = 20
                    }
                    };

                        sectarianAxe.ViewElementDef = Helper.CreateDefFromClone(sectarianAxe.ViewElementDef, "{C6DE2658-0012-4D8E-8813-EE77546DA64B}", sectarianAxe.name);
                        sectarianAxe.ViewElementDef.Description.LocalizationKey = $"SECTARIAN_AXE_NAME";
                        sectarianAxe.ViewElementDef.DisplayName1.LocalizationKey = $"SECTARIAN_AXE_NAME";
                        sectarianAxe.ViewElementDef.DisplayName2.LocalizationKey = $"SECTARIAN_AXE_DESCRIPTION";

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void NewTechnicianHealAndRepairForSlug()
                {

                    try
                    {
                        HealAbilityDef technicianHealSource = DefCache.GetDef<HealAbilityDef>("TechnicianHeal_AbilityDef");

                        HealAbilityDef slugTechnicianHeal = Helper.CreateDefFromClone(technicianHealSource, "{438EDB0E-72C3-4A4D-87CA-1AE252AC6B12}", $"SLUG_{technicianHealSource.name}");
                        slugTechnicianHeal.ViewElementDef = Helper.CreateDefFromClone(technicianHealSource.ViewElementDef, "{E03CA208-096D-4122-94F6-F221BF086034}", $"SLUG_{technicianHealSource.name}");
                        slugTechnicianHeal.CharacterProgressionData = Helper.CreateDefFromClone(technicianHealSource.CharacterProgressionData, "{236DF14B-4FAE-4371-83D2-67CACA2B226C}", $"SLUG_{technicianHealSource.name}");
                        //   slugTechnicianHeal.SceneViewElementDef = Helper.CreateDefFromClone(technicianHealSource.SceneViewElementDef, "{0E8F534F-F8A6-462D-A78F-C11433446121}", $"SLUG_{technicianHealSource.name}");


                        slugTechnicianHeal.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugTechnicianHeal.ViewElementDef.Description.LocalizationKey}";
                        slugTechnicianHeal.ConsumedCharges = 0;
                        slugTechnicianHeal.RequiredCharges = 0;


                        SlugTechnicianHeal = slugTechnicianHeal;

                        HealAbilityDef technicianRepairSource = DefCache.GetDef<HealAbilityDef>("TechnicianRepair_AbilityDef");
                        HealAbilityDef slugTechnicianRepair = Helper.CreateDefFromClone(technicianRepairSource, "{10A5D4D5-D9E4-4C5C-8C2F-A27385F4802C}", $"SLUG_{technicianRepairSource.name}");
                        slugTechnicianRepair.ViewElementDef = Helper.CreateDefFromClone(technicianRepairSource.ViewElementDef, "{C82EEDF3-69DB-43C0-B2E5-957CCB8CB4AC}", $"SLUG_{technicianRepairSource.name}");
                        slugTechnicianRepair.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugTechnicianRepair.ViewElementDef.Description.LocalizationKey}";
                        slugTechnicianRepair.CharacterProgressionData = Helper.CreateDefFromClone(technicianRepairSource.CharacterProgressionData, "{36244BF9-CCC6-4258-A8CB-9CF0972A627D}", $"SLUG_{technicianRepairSource.name}");
                        // slugTechnicianRepair.SceneViewElementDef = Helper.CreateDefFromClone(technicianRepairSource.SceneViewElementDef, "{79EA1AC2-6751-4BA4-9421-BF454D4669FE}", $"SLUG_{technicianRepairSource.name}");

                        slugTechnicianRepair.ConsumedCharges = 0;
                        slugTechnicianRepair.RequiredCharges = 0;

                        slugTechnicianRepair.SuppressHealingOnTargetTags.Add(Shared.SharedGameTags.HumanTag);

                        SlugTechnicianRepair = slugTechnicianRepair;

                        HealAbilityDef technicianRestoreSource = DefCache.GetDef<HealAbilityDef>("TechnicianRestoreBodyPart_AbilityDef");
                        HealAbilityDef slugTechnicianRestore = Helper.CreateDefFromClone(technicianRestoreSource, "{AC352394-2920-4F3D-B35B-98293759159C}", $"SLUG_{technicianRestoreSource.name}");
                        slugTechnicianRestore.ViewElementDef = Helper.CreateDefFromClone(technicianRestoreSource.ViewElementDef, "{2493766F-6981-46B0-9B50-5025E9D943F2}", $"SLUG_{technicianRestoreSource.name}");
                        slugTechnicianRestore.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugTechnicianRestore.ViewElementDef.Description.LocalizationKey}";
                        slugTechnicianRestore.CharacterProgressionData = Helper.CreateDefFromClone(technicianRestoreSource.CharacterProgressionData, "{655A5F2A-59D5-4139-83C9-0361A971955B}", $"SLUG_{technicianRestoreSource.name}");
                        //  slugTechnicianRestore.SceneViewElementDef = Helper.CreateDefFromClone(technicianRestoreSource.SceneViewElementDef, "{AF4F11AD-433F-4340-B46C-362CBB2B7DA7}", $"SLUG_{technicianRestoreSource.name}");
                        slugTechnicianRepair.ConsumedCharges = 0;
                        slugTechnicianRestore.RequiredCharges = 0;


                        SlugTechnicianRestore = slugTechnicianRestore;

                        HealAbilityDef technicianFieldMedicSource = DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef");
                        HealAbilityDef slugFieldMedic = Helper.CreateDefFromClone(technicianFieldMedicSource, "{E1E2A1D1-1124-4101-B7E2-B5892792536F}", $"SLUG_{technicianFieldMedicSource.name}");
                        slugFieldMedic.ViewElementDef = Helper.CreateDefFromClone(technicianFieldMedicSource.ViewElementDef, "{{9F83FB76-59D1-4294-B1E0-95F2204BAAAC}}", $"SLUG_{technicianFieldMedicSource.name}");
                        slugFieldMedic.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugFieldMedic.ViewElementDef.Description.LocalizationKey}";
                        slugFieldMedic.CharacterProgressionData = Helper.CreateDefFromClone(technicianFieldMedicSource.CharacterProgressionData, "{{2289C6C0-68FB-4892-815E-949CDF79C0CA}}", $"SLUG_{technicianFieldMedicSource.name}");
                        slugFieldMedic.ConsumedCharges = 0;
                        slugFieldMedic.RequiredCharges = 0;
                        slugFieldMedic.ContributionPointsOnUse = 600;

                        SlugFieldMedic = slugFieldMedic;

                        RemoveFacehuggerAbilityDef technicianRemoveFaceHuggerSource = DefCache.GetDef<RemoveFacehuggerAbilityDef>("TechnicianRemoveFacehugger_AbilityDef");
                        RemoveFacehuggerAbilityDef slugRemoveFaceHugger = Helper.CreateDefFromClone(technicianRemoveFaceHuggerSource, "{E33312E2-0844-453E-AEAB-23886992FDE5}", $"SLUG_{technicianRemoveFaceHuggerSource.name}");
                        slugRemoveFaceHugger.ViewElementDef = Helper.CreateDefFromClone(technicianRemoveFaceHuggerSource.ViewElementDef, "{4C0870A0-ABC9-4706-9832-AF10E32D17BC}", $"SLUG_{technicianRemoveFaceHuggerSource.name}");
                        slugRemoveFaceHugger.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugRemoveFaceHugger.ViewElementDef.Description.LocalizationKey}";
                        slugRemoveFaceHugger.RequiredCharges = 0;
                        slugRemoveFaceHugger.ContributionPointsOnUse = 750;

                        SlugRemoveFaceHugger = slugRemoveFaceHugger;

                        BashAbilityDef technicianZapSource = DefCache.GetDef<BashAbilityDef>("TechnicianBashStrike_AbilityDef");
                        BashAbilityDef slugZapAbility = Helper.CreateDefFromClone(technicianZapSource, "{04C76A21-CCA6-4F7D-94C8-496A2A8B4FA3}", $"SLUG_{technicianZapSource.name}");
                        slugZapAbility.ViewElementDef = Helper.CreateDefFromClone(technicianZapSource.ViewElementDef, "{4F13650B-DC48-40E8-8F74-95919014B130}", $"SLUG_{technicianZapSource.name}");
                        slugZapAbility.ViewElementDef.Description.LocalizationKey = $"SLUG_{slugZapAbility.ViewElementDef.Description.LocalizationKey}";
                        slugZapAbility.RequiredCharges = 0;
                        slugZapAbility.ConsumedCharges = 0;

                        SlugTechnicianZap = slugZapAbility;

                        TacActorSimpleInteractionAnimActionDef technicianHealAnimations = DefCache.GetDef<TacActorSimpleInteractionAnimActionDef>("E_TechnicianHeal [Soldier_Utka_AnimActionsDef]");
                        technicianHealAnimations.Abilities = new List<TacticalAbilityDef>(technicianHealAnimations.Abilities) { slugTechnicianHeal, slugTechnicianRestore, slugFieldMedic }.ToArray();

                        TacActorSimpleInteractionAnimActionDef technicianRepairAnimations = DefCache.GetDef<TacActorSimpleInteractionAnimActionDef>("E_TechnicianRepair [Soldier_Utka_AnimActionsDef]");
                        technicianRepairAnimations.Abilities = new List<TacticalAbilityDef>(technicianRepairAnimations.Abilities) { slugTechnicianRepair }.ToArray();

                        TacActorAimingAbilityAnimActionDef technicianZapAnimations = DefCache.GetDef<TacActorAimingAbilityAnimActionDef>("E_TechnicianBashStrike [Soldier_Utka_AnimActionsDef]");
                        technicianZapAnimations.AbilityDefs = new List<AbilityDef>(technicianZapAnimations.AbilityDefs) { slugZapAbility }.ToArray();

                        TacActorSimpleInteractionAnimActionDef technicianRemoveFHAnimations = DefCache.GetDef<TacActorSimpleInteractionAnimActionDef>("E_TechnicianRemoveFacehugger [Soldier_Utka_AnimActionsDef]");
                        technicianRemoveFHAnimations.Abilities = new List<TacticalAbilityDef>(technicianRemoveFHAnimations.Abilities) { slugRemoveFaceHugger }.ToArray();

                        TacItemSimpleInteractionAnimActionDef technicianHealArmsAnimations = (TacItemSimpleInteractionAnimActionDef)Repo.GetDef("dd0c72a0-1c28-89fc-6760-745127bc231d");

                        TacItemSimpleInteractionAnimActionDef technicianRepairArmsAnimations = (TacItemSimpleInteractionAnimActionDef)Repo.GetDef("eb0e026c-7fcf-89fc-ab10-7667c0df231d");

                        TacItemSimpleInteractionAnimActionDef technicianRemoveFHArmsAnimations = (TacItemSimpleInteractionAnimActionDef)Repo.GetDef("d8257790-45e6-89f2-5765-5d54e9e52d1d");

                        technicianHealArmsAnimations.Abilities = new List<TacticalAbilityDef>(technicianHealArmsAnimations.Abilities) { slugTechnicianHeal, slugFieldMedic, slugTechnicianRestore }.ToArray();
                        technicianRepairArmsAnimations.Abilities = new List<TacticalAbilityDef>(technicianRepairArmsAnimations.Abilities) { slugTechnicianRepair }.ToArray();
                        technicianRemoveFHArmsAnimations.Abilities = new List<TacticalAbilityDef>(technicianRepairArmsAnimations.Abilities) { slugRemoveFaceHugger }.ToArray();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void ChangeSlugArms()
                {
                    try
                    {

                        slugMechArms.Tags.Add(MercenaryTag);
                        slugMechArms.Tags.Add(Shared.SharedGameTags.BionicalTag);

                        WeaponDef slugArmsWeapon = slugMechArms as WeaponDef;

                        slugArmsWeapon.BehaviorOnDisable = EDisableBehavior.Disable;

                        slugMechArms.CompatibleAmmunition = new TacticalItemDef[] { };
                        slugMechArms.ChargesMax = -1;

                        NewTechnicianHealAndRepairForSlug();

                        HealAbilityDef technicianHealSource = DefCache.GetDef<HealAbilityDef>("TechnicianHeal_AbilityDef");
                        HealAbilityDef technicianRepairSource = DefCache.GetDef<HealAbilityDef>("TechnicianRepair_AbilityDef");

                        technicianRepairSource.SuppressHealingOnTargetTags.Add(Shared.SharedGameTags.HumanTag);

                        /* technicianRepairSource.TargetingDataDef.Origin.Range = 100;
                         technicianRepairSource.TargetingDataDef.Origin.LineOfSight = 0;
                         technicianRepairSource.TargetingDataDef.Origin.FactionVisibility = 0;*/

                        List<AbilityDef> slugArmsAbilities = slugMechArms.Abilities.ToList();

                        slugArmsAbilities.Remove(technicianHealSource);
                        slugArmsAbilities.Remove(technicianRepairSource);
                        slugArmsAbilities.Remove(DefCache.GetDef<ReloadAbilityDef>("Reload_AbilityDef"));
                        slugArmsAbilities.Remove(DefCache.GetDef<RemoveFacehuggerAbilityDef>("TechnicianRemoveFacehugger_AbilityDef"));
                        slugArmsAbilities.Remove(DefCache.GetDef<BashAbilityDef>("TechnicianBashStrike_AbilityDef"));

                        slugArmsAbilities.Add(SlugTechnicianHeal);
                        slugArmsAbilities.Add(SlugTechnicianRepair);
                        slugArmsAbilities.Add(SlugTechnicianZap);
                        slugArmsAbilities.Add(SlugRemoveFaceHugger);

                        slugArmsWeapon.ViewElementDef = Helper.CreateDefFromClone(
                            slugArmsWeapon.ViewElementDef,
                            "{329C56E7-FE67-49FD-8C0D-D713D09749A6}", $"SLUG_{slugArmsWeapon.name}");

                        slugArmsWeapon.ViewElementDef.DisplayName1.LocalizationKey = $"SLUG_{slugArmsWeapon.ViewElementDef.DisplayName1.LocalizationKey}";
                        slugArmsWeapon.ViewElementDef.DisplayName2.LocalizationKey = slugArmsWeapon.ViewElementDef.DisplayName1.LocalizationKey;
                        slugArmsWeapon.ViewElementDef.Description.LocalizationKey = "SLUG_KEY_MECH_ARMS_DESCRIPTION";

                        slugMechArms.Abilities = slugArmsAbilities.ToArray();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }


                }

                private static void MakeSlugArmorNonRemovable()
                {
                    try
                    {
                        foreach (TacticalItemDef itemDef in slugArmor)
                        {
                            itemDef.IsPermanentAugment = true;
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                private static void CreateExpendableArchetypes()
                {

                    try
                    {
                        GameTagDef mercenaryTag = TFTVCommonMethods.CreateNewTag("Mercenary", "{49BDADBC-A411-48B2-8773-533EE9247F4C}");
                        MercenaryTag = mercenaryTag;

                        int[] basicStats = new int[3] { 2, 2, 0 };
                        int[] oldStats = new int[3] { 2, 2, -2 };
                        int[] eliteGhostStats = new int[3] { 0, 4, 0 };

                        // New: Veteran stats (+4 to each value in the regular ones)
                        int[] basicStatsVet = new int[3] { basicStats[0] + 4, basicStats[1] + 4, basicStats[2] + 4 };
                        int[] oldStatsVet = new int[3] { oldStats[0] + 4, oldStats[1] + 4, oldStats[2] + 4 };
                        int[] eliteGhostStatsVet = new int[3] { eliteGhostStats[0] + 4, eliteGhostStats[1] + 4, eliteGhostStats[2] + 4 };

                        // Regular archetypes
                        TacCharacterDef ghost = CreateTacCharaterDef(priestTag, "Mercenary_Ghost", "{05C7ED24-1300-4336-94FB-82AE09CC45AF}",
                            ghostSniperRifle, ghostArmor, new List<GameTagDef>() { mercenaryTag }, 1, eliteGhostStats);

                        CreateMarketPlaceRecruit(ghost.name,
                            "{FA72C430-158D-4F44-99B4-08AF9BF2493F}", "{F2BBE15C-54D1-44D0-9299-D10E56E7314F}",
                            "{D01434F1-4A5E-4354-8272-58CF2CC1C41C}", "{65ACF823-241A-4EF5-890E-51F88FF0F6C6}",
                            "KEY_EXPENDABLE_ARCHETYPE_GHOST_NAME", "KEY_EXPENDABLE_ARCHETYPE_GHOST_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_GHOST_QUOTE",
                            ghost, 500, Helper.CreateSpriteFromImageFile("MERCENARY_GHOST.png"), 4);

                        TacCharacterDef doom = CreateTacCharaterDef(heavyTag, "Mercenary_Heavy", "{96628AFA-B8EF-4350-B451-72B24593993B}",
                            doomAC, doomArmor, new List<GameTagDef> { mercenaryTag }, 1, oldStats);

                        CreateMarketPlaceRecruit(doom.name, "{4FC1981D-C5B5-40E2-83D7-238486503215}", "{546E79A5-FFBE-45A2-852E-9D83E41FFA61}",
                            "{93FA4108-B4B3-45BA-9764-6069D1705228}", "{1ED716FA-EE9C-43C3-B68E-C851DB31BADF}",
                            "KEY_EXPENDABLE_ARCHETYPE_DOOM_NAME", "KEY_EXPENDABLE_ARCHETYPE_DOOM_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_DOOM_QUOTE",
                            doom, 500, Helper.CreateSpriteFromImageFile("MERCENARY_DOOM.png"), 0);

                        TacCharacterDef slug =
                            CreateTacCharaterDef(technicianTag, "Mercenary_Slug", "{BFB4540F-CE02-4934-ACDC-FF2CC5B02DA9}",
                            slugPistol, slugArmor, new List<GameTagDef>() { mercenaryTag }, 1, basicStats);

                        CreateMarketPlaceRecruit(slug.name, "{A8CBE9E4-7EA4-4AA9-93C0-09165C121F1F}", "{FC92AA97-F85A-46BD-9168-985578BF44B2}",
                            "{66D149CB-8ADA-46B5-BEAA-102C18B1F83D}", "{21084D35-84CE-4AD1-AA5A-70EF27C1A247}",
                            "KEY_EXPENDABLE_ARCHETYPE_SLUG_NAME", "KEY_EXPENDABLE_ARCHETYPE_SLUG_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_SLUG_QUOTE",
                            slug, 500, Helper.CreateSpriteFromImageFile("MERCENARY_SLUG.png"), 4);

                        TacCharacterDef spyMaster =
                             CreateTacCharaterDef(infiltratorTag, "Mercenary_Spymaster", "{BFB2B1E0-FA98-450E-83C0-F16EA953E7EB}",
                             spyMasterXbow, spyMasterArmor, new List<GameTagDef>() { mercenaryTag }, 1, basicStats);

                        CreateMarketPlaceRecruit(spyMaster.name, "{BD808894-2F9C-4490-ABF3-7EC8B3815589}", "{7316428E-394A-424D-92B9-1DF621B4AAC9}",
                            "{2FAD7A35-3444-4606-8951-6DB8A4BEA26E}", "{C145BEC5-CBCE-49C5-8BEA-ADDED4936E40}",
                            "KEY_EXPENDABLE_ARCHETYPE_SPYMASTER_NAME", "KEY_EXPENDABLE_ARCHETYPE_SPYMASTER_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_SPYMASTER_QUOTE",
                            spyMaster, 500, Helper.CreateSpriteFromImageFile("MERCENARY_SPYMASTER.png"), 3);

                        TacCharacterDef sectarian =
                            CreateTacCharaterDef(berserkerTag, "Mercenary_Sectarian", "{52C42AFC-F1A8-43FB-B1E1-DF1D68D71A7A}",
                            sectarianAxe, sectarianArmor, new List<GameTagDef>() { mercenaryTag }, 1, basicStats);

                        CreateMarketPlaceRecruit(sectarian.name, "{BE81203F-F0C7-4C42-A556-09DDE55ED15F}", "{CDA90EDA-9FA7-4C68-A593-DBD7140D6820}",
                            "{76EACAB3-3F2E-4A9C-AB3B-A6AEFDAB817D}", "{141EBDFD-7712-4357-8AEF-176F1C7DBD23}",
                            "KEY_EXPENDABLE_ARCHETYPE_SECTARIAN_NAME", "KEY_EXPENDABLE_ARCHETYPE_SECTARIAN_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_SECTARIAN_QUOTE",
                            sectarian, 500, Helper.CreateSpriteFromImageFile("MERCENARY_SECTARIAN.png"), 0);

                        TacCharacterDef exile =
                           CreateTacCharaterDef(assaultTag, "Mercenary_Exile", "{3FBC2BB0-0235-41C7-BB28-6848A74858AB}",
                           exileAssaultRifle, exileArmor, new List<GameTagDef>() { mercenaryTag }, 1, basicStats);

                        CreateMarketPlaceRecruit(exile.name, "{46D893B9-9DC7-4068-8348-6F66FBFF0AF7}", "{E93DFF70-669E-4699-B005-6A7F4FD42706}",
                            "{00F16431-56A8-418E-9E28-C1F55B3A7AF7}", "{241F3A70-43B2-4771-87A8-06F735F8C8F5}",
                            "KEY_EXPENDABLE_ARCHETYPE_EXILE_NAME", "KEY_EXPENDABLE_ARCHETYPE_EXILE_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_EXILE_QUOTE",
                            exile, 500, Helper.CreateSpriteFromImageFile("MERCENARY_EXILE.png"), 0);

                        // Veteran archetypes
                        TacCharacterDef ghostVet = CreateTacCharaterDef(priestTag, "Mercenary_Ghost_Vet", "{A1C7ED24-1300-4336-94FB-82AE09CC45AF}",
                            ghostSniperRifle, ghostArmor, new List<GameTagDef>() { mercenaryTag }, 5, eliteGhostStatsVet);

                        CreateMarketPlaceRecruit(ghostVet.name,
                            "{FA72C430-158D-4F44-99B4-08AF9BF2493E}", "{F2BBE15C-54D1-44D0-9299-D10E56E7314E}",
                            "{D01434F1-4A5E-4354-8272-58CF2CC1C41E}", "{65ACF823-241A-4EF5-890E-51F88FF0F6CE}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_GHOST_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_GHOST_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_GHOST_QUOTE",
                            ghostVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_GHOST_VET.png"), 8);

                        TacCharacterDef doomVet = CreateTacCharaterDef(heavyTag, "Mercenary_Heavy_Vet", "{B6628AFA-B8EF-4350-B451-72B24593993B}",
                            doomAC, doomArmor, new List<GameTagDef> { mercenaryTag }, 5, oldStatsVet);

                        CreateMarketPlaceRecruit(doomVet.name, "{4FC1981D-C5B5-40E2-83D7-238486503216}", "{546E79A5-FFBE-45A2-852E-9D83E41FFA62}",
                            "{93FA4108-B4B3-45BA-9764-6069D1705229}", "{1ED716FA-EE9C-43C3-B68E-C851DB31BAD0}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_DOOM_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_DOOM_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_DOOM_QUOTE",
                            doomVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_DOOM_VET.png"), 8);

                        TacCharacterDef slugVet =
                            CreateTacCharaterDef(technicianTag, "Mercenary_Slug_Vet", "{CFB4540F-CE02-4934-ACDC-FF2CC5B02DA9}",
                            slugPistol, slugArmor, new List<GameTagDef>() { mercenaryTag }, 5, basicStatsVet);

                        CreateMarketPlaceRecruit(slugVet.name, "{A8CBE9E4-7EA4-4AA9-93C0-09165C121F2F}", "{FC92AA97-F85A-46BD-9168-985578BF44B3}",
                            "{66D149CB-8ADA-46B5-BEAA-102C18B1F83E}", "{21084D35-84CE-4AD1-AA5A-70EF27C1A248}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SLUG_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SLUG_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SLUG_QUOTE",
                            slugVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_SLUG_VET.png"), 8);

                        TacCharacterDef spyMasterVet =
                             CreateTacCharaterDef(infiltratorTag, "Mercenary_Spymaster_Vet", "{CFB2B1E0-FA98-450E-83C0-F16EA953E7EB}",
                             spyMasterXbow, spyMasterArmor, new List<GameTagDef>() { mercenaryTag }, 5, basicStatsVet);

                        CreateMarketPlaceRecruit(spyMasterVet.name, "{BD808894-2F9C-4490-ABF3-7EC8B381558A}", "{7316428E-394A-424D-92B9-1DF621B4AAC8}",
                            "{2FAD7A35-3444-4606-8951-6DB8A4BEA26F}", "{C145BEC5-CBCE-49C5-8BEA-ADDED4936E41}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SPYMASTER_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SPYMASTER_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SPYMASTER_QUOTE",
                            spyMasterVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_SPYMASTER_VET.png"), 8);

                        TacCharacterDef sectarianVet =
                            CreateTacCharaterDef(berserkerTag, "Mercenary_Sectarian_Vet", "{62C42AFC-F1A8-43FB-B1E1-DF1D68D71A7A}",
                            sectarianAxe, sectarianArmor, new List<GameTagDef>() { mercenaryTag }, 5, basicStatsVet);

                        CreateMarketPlaceRecruit(sectarianVet.name, "{BE81203F-F0C7-4C42-A556-09DDE55ED15E}", "{CDA90EDA-9FA7-4C68-A593-DBD7140D6821}",
                            "{76EACAB3-3F2E-4A9C-AB3B-A6AEFDAB817E}", "{141EBDFD-7712-4357-8AEF-176F1C7DBD24}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SECTARIAN_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SECTARIAN_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_SECTARIAN_QUOTE",
                            sectarianVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_SECTARIAN_VET.png"), 8);

                        TacCharacterDef exileVet =
                           CreateTacCharaterDef(assaultTag, "Mercenary_Exile_Vet", "{4FBC2BB0-0235-41C7-BB28-6848A74858AB}",
                           exileAssaultRifle, exileArmor, new List<GameTagDef>() { mercenaryTag }, 5, basicStatsVet);

                        CreateMarketPlaceRecruit(exileVet.name, "{46D893B9-9DC7-4068-8348-6F66FBFF0AF8}", "{E93DFF70-669E-4699-B005-6A7F4FD42707}",
                            "{00F16431-56A8-418E-9E28-C1F55B3A7AF8}", "{241F3A70-43B2-4771-87A8-06F735F8C8F6}",
                            "KEY_EXPENDABLE_ARCHETYPE_VETERAN_EXILE_NAME", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_EXILE_DESCRIPTION", "KEY_EXPENDABLE_ARCHETYPE_VETERAN_EXILE_QUOTE",
                            exileVet, 900, Helper.CreateSpriteFromImageFile("MERCENARY_EXILE_VET.png"), 8);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void CreateMarketPlaceRecruit(string name, string gUID, string gUID2, string gUID3, string gUID4, string keyTitle, string keyDescription, string keyQuote, TacCharacterDef tacCharacterDef, int price, Sprite icon, int availability)
                {
                    try
                    {

                        GeoMarketplaceItemOptionDef sourceItemOption = DefCache.GetDef<GeoMarketplaceItemOptionDef>("KasoBuggy_MarketplaceItemOptionDef");
                        GeoMarketplaceItemOptionDef newOption = Helper.CreateDefFromClone(sourceItemOption, gUID, name);
                        GroundVehicleItemDef sourceVehicleItemDef = DefCache.GetDef<GroundVehicleItemDef>("KS_Kaos_Buggy_ItemDef");

                        GroundVehicleItemDef vehicleItemDef = Helper.CreateDefFromClone(sourceVehicleItemDef, gUID2, $"{name}_VehicleItemDef");
                        vehicleItemDef.ViewElementDef = Helper.CreateDefFromClone(sourceVehicleItemDef.ViewElementDef, gUID3, name);
                        vehicleItemDef.ViewElementDef.DisplayName1.LocalizationKey = keyTitle;
                        vehicleItemDef.ViewElementDef.Category.LocalizationKey = keyQuote;
                        vehicleItemDef.ViewElementDef.Description.LocalizationKey = keyDescription;
                        vehicleItemDef.DataDef = Helper.CreateDefFromClone(vehicleItemDef.DataDef, gUID4, name);
                        vehicleItemDef.Tags.Add(MercenaryTag);

                        vehicleItemDef.ViewElementDef.InventoryIcon = icon;

                        vehicleItemDef.VehicleTemplateDef = tacCharacterDef;

                        newOption.ItemDef = vehicleItemDef;
                        tacCharacterDef.Data.ViewElementDef = vehicleItemDef.ViewElementDef;
                        tacCharacterDef.ItemDef = vehicleItemDef;
                        newOption.MinPrice = price - price / 10;
                        newOption.MaxPrice = price + price / 10;
                        newOption.Availability = availability;

                        TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");
                        List<GeoMarketplaceOptionDef> geoMarketplaceItemOptionDefs = marketplaceSettings.PossibleOptions.ToList();
                        geoMarketplaceItemOptionDefs.Add(newOption);
                        marketplaceSettings.PossibleOptions = geoMarketplaceItemOptionDefs.ToArray();

                        // TFTVLogger.Always($"{name}null? {DefCache.GetDef<GroundVehicleItemDef>($"{name}_VehicleItemDef") == null}");

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
            internal class GeoRecruitment
            {

                [HarmonyPatch(typeof(GeoUnitDescriptor), "FinishInitCharacter")] //VERIFIED
                public static class GeoUnitDescriptor_FinishInitCharacter_patch
                {

                    public static void Prefix(GeoUnitDescriptor __instance)
                    {
                        try
                        {
                            ImplementPerkTossMercenariesOnHirePrefix(__instance);
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }


                    public static void Postfix(GeoUnitDescriptor __instance, GeoCharacter character)
                    {
                        try
                        {
                            AdjustmentsToMercernariesOnHire(character, __instance);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                private static void ImplementPerkTossMercenariesOnHirePrefix(GeoUnitDescriptor geoUnitDescriptor)
                {
                    try
                    {
                        PassiveModifierAbilityDef handGunsProf = DefCache.GetDef<PassiveModifierAbilityDef>("HandgunsTalent_AbilityDef");
                        PassiveModifierAbilityDef heavyWepProf = DefCache.GetDef<PassiveModifierAbilityDef>("HeavyWeaponsTalent_AbilityDef");
                        PassiveModifierAbilityDef meleeProf = DefCache.GetDef<PassiveModifierAbilityDef>("MeleeWeaponTalent_AbilityDef");
                        PassiveModifierAbilityDef pDWProf = DefCache.GetDef<PassiveModifierAbilityDef>("PDWTalent_AbilityDef");
                        PassiveModifierAbilityDef shotgunProf = DefCache.GetDef<PassiveModifierAbilityDef>("ShotgunTalent_AbilityDef");
                        PassiveModifierAbilityDef sniperProf = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
                        PassiveModifierAbilityDef assaultRiflesProf = DefCache.GetDef<PassiveModifierAbilityDef>("AssaultRiflesTalent_AbilityDef");

                        List<PassiveModifierAbilityDef> proficiencies = new List<PassiveModifierAbilityDef>()
                                { handGunsProf, heavyWepProf, meleeProf, pDWProf, shotgunProf, sniperProf, assaultRiflesProf};

                        if (geoUnitDescriptor.GetGameTags().Contains(MercenaryTag))
                        {
                            if (geoUnitDescriptor.ClassTag == priestTag)
                            {
                                // TFTVLogger.Always($"mercenary priest check");

                                if (geoUnitDescriptor.Progression.PersonalAbilities[3] == sniperProf)
                                {
                                    proficiencies.Remove(sniperProf);
                                    geoUnitDescriptor.Progression.PersonalAbilities[3] = proficiencies.GetRandomElement();
                                }

                                geoUnitDescriptor.Progression.PersonalAbilities[2] = DefCache.GetDef<RepositionAbilityDef>("Vanish_AbilityDef");
                                string njFactionAbility = TFTVAircraftReworkMain.AircraftReworkOn ? "AmplifyPain_AbilityDef" : "BC_ARTargeting_AbilityDef";
                                geoUnitDescriptor.Progression.PersonalAbilities[5] = DefCache.GetDef<ApplyStatusAbilityDef>(njFactionAbility);
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");


                            }
                            else if (geoUnitDescriptor.ClassTag == technicianTag)
                            {

                                if (geoUnitDescriptor.Progression.PersonalAbilities[3] == handGunsProf)
                                {
                                    proficiencies.Remove(handGunsProf);
                                    proficiencies.Remove(pDWProf);
                                    geoUnitDescriptor.Progression.PersonalAbilities[3] = proficiencies.GetRandomElement();
                                }

                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<ResurrectAbilityDef>("Mutoid_ResurrectAbilityDef");

                                // Get the FieldInfo for the MainSpecDef field
                                FieldInfo mainSpecDefField = typeof(ProgressionDescriptor).GetField("MainSpecDef", BindingFlags.Instance | BindingFlags.Public);

                                mainSpecDefField.SetValue(geoUnitDescriptor.Progression, SlugSpecialization);


                            }
                            else if (geoUnitDescriptor.ClassTag == infiltratorTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[2] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Biochemist_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<ApplyStatusAbilityDef>("Saboteur_AbilityDef");


                            }
                            else if (geoUnitDescriptor.ClassTag == berserkerTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<PassiveModifierAbilityDef>("DieHard_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");


                            }
                            else if (geoUnitDescriptor.ClassTag == heavyTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("BattleHardened_AbilityDef");


                            }
                            else if (geoUnitDescriptor.ClassTag == assaultTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<PassiveModifierAbilityDef>("DieHard_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");

                            }
                            TFTVLogger.Always($"Mercenary: {geoUnitDescriptor.ClassTag}");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustmentsToMercernariesOnHire(GeoCharacter character, GeoUnitDescriptor geoUnitDescriptor)
                {
                    try
                    {
                        if (geoUnitDescriptor.GetGameTags().Contains(MercenaryTag))
                        {
                            PersonnelRestrictions.EnsureJustAGrunt(character, "Mercenary hire");

                            AdjustMercenaryProficiencyPerks(character);

                            if (geoUnitDescriptor.ClassTag != berserkerTag)
                            {
                                GiveAmmoToMercenaryOnCreation(character);
                            }

                            AdjustNameOnHire(character);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustNameOnHire(GeoCharacter character)
                {
                    try
                    {
                        // Retrieve the female tag from the shared game tags
                        GenderTagDef femaleTag = Shared.SharedGameTags.Genders.FemaleTag;
                        string name = "";

                        // Ghosts (Priest tag)
                        if (character.GameTags.Contains(priestTag))
                        {
                            if (character.Identity.SexTag == femaleTag)
                            {
                                name = TFTVHumanEnemiesNames.syn_FemaleNames.GetRandomElement();
                            }
                            else
                            {
                                name = TFTVHumanEnemiesNames.syn_MaleNames.GetRandomElement();
                            }
                            name += $" \"{TFTVHumanEnemiesNames.ghostMonikers.GetRandomElement()}\"";
                        }
                        // Slugs (Technician tag)
                        else if (character.GameTags.Contains(technicianTag))
                        {
                            name = TFTVHumanEnemiesNames.slug_Adjectives.GetRandomElement();
                            name += $" {TFTVHumanEnemiesNames.slug_FirstNames.GetRandomElement()}";
                        }
                        // Spy Masters (Infiltrator tag)
                        else if (character.GameTags.Contains(infiltratorTag))
                        {
                            if (character.Identity.SexTag == femaleTag)
                            {
                                name = TFTVHumanEnemiesNames.spymasterFemaleNames.GetRandomElement();
                            }
                            else
                            {
                                name = TFTVHumanEnemiesNames.spymasterMaleNames.GetRandomElement();
                            }
                            name += $" \"{TFTVHumanEnemiesNames.spymasterMonikers.GetRandomElement()}\"";
                        }
                        // Old Hounds (Heavy tag)
                        else if (character.GameTags.Contains(heavyTag))
                        {
                            if (character.Identity.SexTag == femaleTag)
                            {
                                name = TFTVHumanEnemiesNames.nj_FemaleNames.GetRandomElement();
                            }
                            else
                            {
                                name = TFTVHumanEnemiesNames.nj_MaleNames.GetRandomElement();
                            }
                            name += $" \"{TFTVHumanEnemiesNames.oldHoundMonikers.GetRandomElement()}\"";
                        }
                        // Exiles (Assault tag)
                        else if (character.GameTags.Contains(assaultTag))
                        {
                            if (character.Identity.SexTag == femaleTag)
                            {
                                name = TFTVHumanEnemiesNames.syn_FemaleNames.GetRandomElement();
                            }
                            else
                            {
                                name = TFTVHumanEnemiesNames.syn_MaleNames.GetRandomElement();
                            }
                            name += $" \"{TFTVHumanEnemiesNames.exileMonikers.GetRandomElement()}\"";
                        }
                        // Sectarians (Berserker tag)
                        else if (character.GameTags.Contains(berserkerTag))
                        {
                            if (character.Identity.SexTag == femaleTag)
                            {
                                name = TFTVHumanEnemiesNames.sectarianFemaleNames.GetRandomElement();
                            }
                            else
                            {
                                name = TFTVHumanEnemiesNames.sectarianMaleNames.GetRandomElement();
                            }
                            name += $" \"{TFTVHumanEnemiesNames.sectarian_Monikers.GetRandomElement()}\"";
                        }

                        character.Identity.Name = name;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }



                private static void AdjustMercenaryProficiencyPerks(GeoCharacter character)
                {
                    try
                    {
                        PassiveModifierAbilityDef handGunsProf = DefCache.GetDef<PassiveModifierAbilityDef>("HandgunsTalent_AbilityDef");
                        PassiveModifierAbilityDef heavyWepProf = DefCache.GetDef<PassiveModifierAbilityDef>("HeavyWeaponsTalent_AbilityDef");
                        PassiveModifierAbilityDef meleeProf = DefCache.GetDef<PassiveModifierAbilityDef>("MeleeWeaponTalent_AbilityDef");
                        PassiveModifierAbilityDef pDWProf = DefCache.GetDef<PassiveModifierAbilityDef>("PDWTalent_AbilityDef");
                        PassiveModifierAbilityDef shotgunProf = DefCache.GetDef<PassiveModifierAbilityDef>("ShotgunTalent_AbilityDef");
                        PassiveModifierAbilityDef sniperProf = DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef");
                        PassiveModifierAbilityDef assaultRiflesProf = DefCache.GetDef<PassiveModifierAbilityDef>("AssaultRiflesTalent_AbilityDef");

                        List<PassiveModifierAbilityDef> proficiencies = new List<PassiveModifierAbilityDef>()
                                { handGunsProf, heavyWepProf, meleeProf, pDWProf, shotgunProf, sniperProf, assaultRiflesProf};


                        if (character.GameTags.Contains(priestTag))
                        {
                            character.Progression.AddAbility(sniperProf);
                        }

                        else if (character.GameTags.Contains(technicianTag))
                        {
                            character.Progression.AddAbility(handGunsProf);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static void GiveAmmoToMercenaryOnCreation(GeoCharacter character)
                {

                    try
                    {
                        if (character.TemplateDef.Data.EquipmentItems.Count() > 0)
                        {

                            ItemDef weapon = character.TemplateDef.Data.EquipmentItems[0];
                            ItemDef ammo = character.TemplateDef.Data.EquipmentItems[0].CompatibleAmmunition[0];

                            List<GeoItem> inventoryList = new List<GeoItem>();

                            if (character.ClassTag.Equals(infiltratorTag))
                            {
                                inventoryList = new List<GeoItem>()
                        {
                             new GeoItem(new ItemUnit { ItemDef = ammo, Quantity = 1 }),
                             new GeoItem(new ItemUnit { ItemDef = ammo, Quantity = 1 })
                        };
                            }

                            List<GeoItem> equipmentList = new List<GeoItem>()
                    {
                            new GeoItem(new ItemUnit { ItemDef = weapon, Quantity = 1}),
                            new GeoItem(new ItemUnit { ItemDef = ammo, Quantity = 1 }),
                            new GeoItem(new ItemUnit { ItemDef = ammo, Quantity = 1 })
                    };

                            character.SetItems(null, equipmentList, inventoryList, true);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }





            }
            internal class Tactical
            {

                /// <summary>
                /// Reduces Slug strength when it uses its Mech Arms
                /// </summary>
                /// <param name="tacticalAbility"></param>
                /// <param name="tacticalActor"></param>
                public static void SlugHealTraumaEffect(TacticalAbility tacticalAbility, TacticalActor tacticalActor)
                {
                    try
                    {
                        // TFTVLogger.Always($"Ability: {tacticalAbility.TacticalAbilityDef.name}");

                        List<TacticalAbilityDef> slugAbilities = new List<TacticalAbilityDef>() {
                    TFTVMercenaries.SlugTechnicianHeal, TFTVMercenaries.SlugTechnicianRepair, TFTVMercenaries.SlugTechnicianRestore, TFTVMercenaries.SlugFieldMedic,
                TFTVMercenaries.SlugRemoveFaceHugger, TFTVMercenaries.SlugTechnicianZap};

                        if (slugAbilities.Contains(tacticalAbility.TacticalAbilityDef))
                        {
                            int maxEndurance = tacticalActor.CharacterStats.Endurance.IntMax;

                            if (!TFTVRevenant.TFTVRevenantResearch.SlugOGStrength.ContainsKey(tacticalActor.GeoUnitId))
                            {
                                TFTVRevenant.TFTVRevenantResearch.SlugOGStrength.Add(tacticalActor.GeoUnitId, maxEndurance);
                            }

                            int enduranceToSubtract = (int)Math.Ceiling(maxEndurance * 0.15);

                            tacticalActor.CharacterStats.Endurance.Subtract(enduranceToSubtract);
                            tacticalActor.UpdateStats();



                            TFTVLogger.Always($"reducing endurance of {tacticalActor.DisplayName} by {enduranceToSubtract} from use of {tacticalAbility.TacticalAbilityDef.name}");
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }


                public static void AdjustMercItemsToBeRecoverable(bool revert)
                {
                    try
                    {
                        AdjustToBeDroppedOnDeath(doomArmor, revert);
                        AdjustToBeDroppedOnDeath(ghostArmor, revert);
                        AdjustToBeDroppedOnDeath(exileArmor, revert);
                        AdjustToBeDroppedOnDeath(sectarianArmor, revert);
                        AdjustToBeDroppedOnDeath(spyMasterArmor, revert);
                        AdjustToBeDroppedOnDeath(new List<TacticalItemDef>() { doomAC, spyMasterXbow, sectarianAxe }, revert);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static void AdjustToBeDroppedOnDeath(List<TacticalItemDef> list, bool revert = false)
                {
                    try
                    {
                        if (!revert)
                        {
                            foreach (TacticalItemDef item in list)
                            {
                                if (!item.Tags.Contains(Shared.SharedGameTags.ManufacturableTag))
                                {
                                    item.Tags.Add(Shared.SharedGameTags.ManufacturableTag);
                                }
                            }
                        }
                        else
                        {
                            foreach (TacticalItemDef item in list)
                            {
                                if (item.Tags.Contains(Shared.SharedGameTags.ManufacturableTag))
                                {
                                    item.Tags.Remove(Shared.SharedGameTags.ManufacturableTag);
                                }
                            }
                        }
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
}


