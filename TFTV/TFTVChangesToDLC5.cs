using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static PhoenixPoint.Geoscape.Entities.GeoUnitDescriptor;

namespace TFTV
{


    internal class TFTVChangesToDLC5
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static GameTagDef MercenaryTag;

        private static bool AmbushOrScavTemp = false;



        /// <summary>
        /// Allows to buy in Marketplace without an aircraft at the site.
        /// </summary>

        [HarmonyPatch(typeof(MarketplaceAbility), "GetTargetDisabledStateInternal")]
        public static class MarketplaceAbility_GetTargetDisabledStateInternal_patch
        {

            public static void Postfix(ref GeoAbilityTargetDisabledState __result, MarketplaceAbility __instance)
            {
                try
                {
                    if (__instance.GeoLevel.EventSystem.GetVariable("NumberOfDLC5MissionsCompletedVariable") > 0)
                    {

                        __result = GeoAbilityTargetDisabledState.NotDisabled;

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



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
                        doomLegs.Armor = 22;

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
                        sectarianTorso.Weight = 3;
                        sectarianTorso.BodyPartAspectDef.Endurance = 2;
                        sectarianTorso.BodyPartAspectDef.Speed = 0f;
                        sectarianTorso.BodyPartAspectDef.Stealth = -0.1f;
                        sectarianTorso.BodyPartAspectDef.Accuracy = -0.05f;

                        sectarianLegs.Armor = 14;
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
                        ghostTorso.BodyPartAspectDef.Stealth = 0.1f;

                        ghostLegs.Armor = 14;
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
                        slugTorso.Weight = 0;
                        slugTorso.BodyPartAspectDef.Endurance = 1;
                        slugTorso.BodyPartAspectDef.Speed = 0;
                        slugTorso.BodyPartAspectDef.Accuracy = 0;
                        slugTorso.BodyPartAspectDef.Stealth = -0.1f;

                        slugLegs.Tags.Add(MercenaryTag);
                        slugLegs.Tags.Add(Shared.SharedGameTags.BionicalTag);

                        slugLegs.Armor = 20;
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



                [HarmonyPatch(typeof(GeoUnitDescriptor), "FinishInitCharacter")]
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

                                if (geoUnitDescriptor.Progression.PersonalAbilities[3] == sniperProf)
                                {
                                    proficiencies.Remove(sniperProf);
                                    geoUnitDescriptor.Progression.PersonalAbilities[3] = proficiencies.GetRandomElement();
                                }

                                geoUnitDescriptor.Progression.PersonalAbilities[2] = DefCache.GetDef<RepositionAbilityDef>("Vanish_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[5] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_ARTargeting_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");

                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
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

                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
                            }
                            else if (geoUnitDescriptor.ClassTag == infiltratorTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[2] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Biochemist_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<ApplyStatusAbilityDef>("Saboteur_AbilityDef");

                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
                            }
                            else if (geoUnitDescriptor.ClassTag == berserkerTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<PassiveModifierAbilityDef>("DieHard_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");

                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
                            }
                            else if (geoUnitDescriptor.ClassTag == heavyTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("BattleHardened_AbilityDef");

                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
                            }
                            else if (geoUnitDescriptor.ClassTag == assaultTag)
                            {
                                geoUnitDescriptor.Progression.PersonalAbilities[1] = DefCache.GetDef<PassiveModifierAbilityDef>("DieHard_AbilityDef");
                                geoUnitDescriptor.Progression.PersonalAbilities[6] = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");
                                TFTVLogger.Always($"{geoUnitDescriptor.ClassTag}");
                            }
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
                            AdjustMercenaryProficiencyPerks(character);

                            if (geoUnitDescriptor.ClassTag != berserkerTag)
                            {
                                GiveAmmoToMercenaryOnCreation(character);
                            }
                        }
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
                            int enduranceToSubtract = (int)Math.Ceiling(tacticalActor.CharacterStats.Endurance.IntMax * 0.15);

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

                [HarmonyPatch(typeof(GeoMission), "ApplyMissionResults")]
                public static class GeoMission_ApplyMissionResults_patch
                {
                    public static void Prefix()
                    {
                        try
                        {
                            AdjustMercItemsToBeRecoverable(false);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                    public static void Postfix()
                    {
                        try
                        {
                            AdjustMercItemsToBeRecoverable(true);

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }

                private static void AdjustMercItemsToBeRecoverable(bool revert)
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

        internal class TFTVKaosGuns
        {

            internal static readonly WeaponDef _obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
            internal static readonly WeaponDef _subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
            internal static readonly WeaponDef _redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
            internal static readonly WeaponDef _devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
            internal static readonly WeaponDef _tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

            internal static Dictionary<GeoMarketplaceItemOptionDef, GeoMarketplaceItemOptionDef> _kGWeaponsAndAmmo = new Dictionary<GeoMarketplaceItemOptionDef, GeoMarketplaceItemOptionDef>();
            internal static GameTagDef _kGTag;

            [HarmonyPatch(typeof(GeoMission), "AddCratesToMissionData")]
            public static class GeoMission_AddCratesToMissionData_patch
            {

                public static void Prefix(GeoMission __instance)
                {
                    try
                    {
                        if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeAmbush_MissionTagDef"))
                            ||
                            __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeScavenging_MissionTagDef")))
                        {

                            AmbushOrScavTemp = true;

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                public static void Postfix(GeoMission __instance)
                {
                    try
                    {
                        if (__instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeAmbush_MissionTagDef"))
                            ||
                            __instance.MissionDef.Tags.Contains(DefCache.GetDef<MissionTagDef>("MissionTypeScavenging_MissionTagDef")))
                        {

                            AmbushOrScavTemp = false;

                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }

            [HarmonyPatch(typeof(GeoLevelController), "GetAvailableFactionEquipment")]
            public static class GeoLevelController_GetAvailableFactionEquipment_patch
            {


                public static void Postfix(ref List<TacticalItemDef> __result)
                {
                    try
                    {
                        if (AmbushOrScavTemp)
                        {
                            TFTVLogger.Always($"It's an ambush or scavenging mission, adding KG ammo to GetAvailableFactionEquipment");

                            List<TacticalItemDef> kgAmmo = new List<TacticalItemDef>()
                        {
                            TFTVKaosGuns._subjector.CompatibleAmmunition[0],
                            TFTVKaosGuns._obliterator.CompatibleAmmunition[0],
                            TFTVKaosGuns._tormentor.CompatibleAmmunition[0],
                            TFTVKaosGuns._devastator.CompatibleAmmunition[0],
                            TFTVKaosGuns._redemptor.CompatibleAmmunition[0]

                        };

                            __result.AddRange(kgAmmo

                            );


                        }


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

            }


            private static void AdjustKGAndCreateAmmoForThem(WeaponDef weaponDef, int amount, int minPrice, string gUID0, string gUID1, string gUID2, string spriteFileName)
            {
                try
                {

                    FactionTagDef neutralFactionTag = DefCache.GetDef<FactionTagDef>("Neutral_FactionTagDef");
                    FactionTagDef phoenixFactionTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");

                    TacticalItemDef sourceAmmo = DefCache.GetDef<TacticalItemDef>("PX_AssaultRifle_AmmoClip_ItemDef");
                    string name = $"{weaponDef.name}_AmmoClipDef";

                    ClassTagDef classTagDef = weaponDef.Tags.FirstOrDefault<ClassTagDef>();

                    TacticalItemDef newAmmo = Helper.CreateDefFromClone(sourceAmmo, gUID0, name);
                    newAmmo.ViewElementDef = Helper.CreateDefFromClone(sourceAmmo.ViewElementDef, gUID1, name);
                    newAmmo.ViewElementDef.DisplayName1.LocalizationKey = $"KEY_KAOSGUNS_AMMO_{weaponDef.name}";
                    newAmmo.ViewElementDef.Description.LocalizationKey = $"KEY_KAOSGUNS_AMMO_DESCRIPTION_{weaponDef.name}";
                    newAmmo.ViewElementDef.InventoryIcon = Helper.CreateSpriteFromImageFile(spriteFileName);


                    newAmmo.ChargesMax = amount;
                    newAmmo.CrateSpawnWeight = 1000;
                    newAmmo.Tags.Remove(phoenixFactionTag);
                    newAmmo.Tags.Add(neutralFactionTag);
                    newAmmo.Tags.Remove(DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef"));
                    newAmmo.Tags.Add(classTagDef);
                    //  newAmmo.CombineWhenStacking = false;
                    newAmmo.ManufactureTech = 0;
                    newAmmo.ManufactureMaterials = minPrice;
                    weaponDef.ChargesMax = amount;
                    weaponDef.CompatibleAmmunition = new TacticalItemDef[] { newAmmo };

                    weaponDef.Tags.Add(_kGTag);

                    GeoMarketplaceItemOptionDef newMarketplaceItem = Helper.CreateDefFromClone
                         (DefCache.GetDef<GeoMarketplaceItemOptionDef>("Obliterator_MarketplaceItemOptionDef"), gUID2, name);

                    newMarketplaceItem.MinPrice = minPrice;
                    newMarketplaceItem.MaxPrice = minPrice + minPrice * 1.25f;
                    newMarketplaceItem.ItemDef = newAmmo;
                    newMarketplaceItem.DisallowDuplicates = false;


                    TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");

                    List<GeoMarketplaceOptionDef> geoMarketplaceItemOptionDefs = marketplaceSettings.PossibleOptions.ToList();

                    geoMarketplaceItemOptionDefs.Add(newMarketplaceItem);


                    marketplaceSettings.PossibleOptions = geoMarketplaceItemOptionDefs.ToArray();

                    weaponDef.WeaponMalfunction = DefCache.GetDef<WeaponDef>("PX_AssaultRifle_WeaponDef").WeaponMalfunction;

                    if (weaponDef.Tags.Contains(DefCache.GetDef<ItemTypeTagDef>("AssaultRifleItem_TagDef")))
                    {
                        newAmmo.Tags.Add(Shared.SharedGameTags.MutoidClassTag);
                    }
                    if (weaponDef.Tags.Contains(DefCache.GetDef<ItemTypeTagDef>("HandgunItem_TagDef")))
                    {
                        newAmmo.Tags.Add(Shared.SharedGameTags.MutoidClassTag);
                        newAmmo.Tags.Add(TFTVMercenaries.berserkerTag);
                    }

                    AmmoWeaponDatabase.AmmoToWeaponDictionary.Add(newAmmo, new List<TacticalItemDef>() { weaponDef });
                    GeoMarketplaceItemOptionDef weaponMarketPlaceOption = (GeoMarketplaceItemOptionDef)geoMarketplaceItemOptionDefs.Find(o => o is GeoMarketplaceItemOptionDef marketOption && marketOption.ItemDef == weaponDef);

                    _kGWeaponsAndAmmo.Add(weaponMarketPlaceOption, newMarketplaceItem);

                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");

                    neutralFaction.StartingManufacturableItems = neutralFaction.StartingManufacturableItems.AddToArray(newAmmo);

                    InventoryComponentDef neutralCrateInventoryComponent = DefCache.GetDef<InventoryComponentDef>("Crate_PX_InventoryComponentDef");
                    neutralCrateInventoryComponent.ItemDefs = neutralCrateInventoryComponent.ItemDefs.AddToArray(newAmmo);



                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }



            }

            public static void CreateKaosWeaponAmmo()
            {
                try
                {
                    _obliterator.ManufactureMaterials = 100;
                    _subjector.ManufactureMaterials = 100;
                    _redemptor.ManufactureMaterials = 100;
                    _devastator.ManufactureMaterials = 100;
                    _tormentor.ManufactureMaterials = 100;

                    _kGTag = TFTVCommonMethods.CreateNewTag("KaosGun", "{2DA3F33A-8D39-4DA6-8BA5-38C3114A21F7}");

                    //("Mutoid_ClassTagDef");

                    //KEY_KAOSGUNS_AMMO_
                    //KEY_KAOSGUNS_AMMO_DESCRIPTION_

                    AdjustKGAndCreateAmmoForThem(_tormentor, 8, 30, "e1875c26-0494-4d0f-9e5d-3c74a17c3b2d",
                    "79f6bb60-8ca3-4bbf-a0f1-c819f5ebf09e",
                    "ee89b5c3-6d06-4c5e-856b-96e7ff411c77", "KG_Pistol_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_subjector, 5, 30, "2e5be682-1f85-4610-bbb7-c2f2bf41d4c6",
                    "b03d78d4-c7e7-49c3-b097-3448e253a1e7",
                    "70a0a172-2b57-48d3-94c2-7cb4e428c3c4", "KG_Sniper_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_redemptor, 24, 30, "8f7ff5ca-4b8d-4677-86d3-7f21e41a3a70",
                    "d60e04a0-c873-4c16-9a83-2f9d6e1c163d",
                    "dc92d8ca-1b8d-4f85-9d90-d8eb9e63d5a3", "KG_Shotgun_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_devastator, 6, 30, "99aa40e5-5415-44b9-98ed-34d746a99b52",
                    "3b647fa3-1e06-4f2a-9d1c-82edf8a6dbff",
                    "605d3c8a-7b9c-481a-8c0d-7ff4be94901a", "KG_Cannon_Ammo.png");
                    AdjustKGAndCreateAmmoForThem(_obliterator, 32, 30, "2c86774f-4889-4c06-9f7a-8971e62ff267",
                    "587b1a5b-1665-48c9-8b9c-4156231712c1",
                    "1a1230fc-0e5d-4c4c-9be5-563879d2471f", "KG_Assault_Rifle_Ammo.png");


                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class TFTVMarketPlaceItems
        {
            public static void AdjustMarketPlaceOptions()
            {
                try
                {
                    TheMarketplaceSettingsDef marketplaceSettings = DefCache.GetDef<TheMarketplaceSettingsDef>("TheMarketplaceSettingsDef");

                    List<GeoMarketplaceOptionDef> geoMarketplaceOptionDefs = new List<GeoMarketplaceOptionDef>(marketplaceSettings.PossibleOptions.ToList());

                    geoMarketplaceOptionDefs.Remove(DefCache.GetDef<GeoMarketplaceResearchOptionDef>("Random_MarketplaceResearchOptionDef"));

                    marketplaceSettings.PossibleOptions = geoMarketplaceOptionDefs.ToArray();

                    DefCache.GetDef<GeoMarketplaceOptionDef>("Redemptor_MarketplaceItemOptionDef").Availability = 3;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Subjector_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Tormentor_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("Devastator_Redemptor_MarketplaceItemOptionDef").Availability = 2;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("JetBoosters_MarketplaceItemOptionDef").Availability = 1;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("TheFullstop_MarketplaceItemOptionDef").Availability = 5;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("TheScreamer_MarketplaceItemOptionDef").Availability = 3;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("AdvancedEngineMappingModule_MarketplaceItemOptionDef").Availability = 0;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("SpikedArmorPlating_MarketplaceItemOptionDef").Availability = 2;
                    DefCache.GetDef<GeoMarketplaceOptionDef>("ReinforcedCargoRacks_MarketplaceItemOptionDef").Availability = 3;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class TFTVMarketPlaceGenerateOffers
        {
            private static readonly ClassTagDef _vehicle_ClassTagDef = DefCache.GetDef<ClassTagDef>("Vehicle_ClassTagDef");

            private static readonly string _marketPlaceStockRotated = "MarketPlaceRotations";
            private static string _currentMarketPlaceSpecial;
            private static readonly string _vehicleMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_VEHICLES";
            private static readonly string _weaponsMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_WEAPONS";
            private static readonly string _mercenaryMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_MERCENARY";
            private static readonly string _researchMarketPlaceSpecial = "KEY_MARKETPLACE_SPECIAL_RESEARCH";
            private static readonly string[] _marketPlaceSpecials = new string[] { _vehicleMarketPlaceSpecial, _researchMarketPlaceSpecial, _mercenaryMarketPlaceSpecial, _weaponsMarketPlaceSpecial };


            /// <summary>
            /// Can't hire mercenaries if Living Quarters are full and can't buy tech that has already been researched
            /// </summary>

            [HarmonyPatch(typeof(GeoEventChoice), "PassRequirements")]
            public static class GeoEventChoice_PassRequirements_patch
            {
                public static void Postfix(GeoEventChoice __instance, GeoFaction faction, ref bool __result)
                {
                    try
                    {
                        RemoveBadChoices(__instance, faction, ref __result);


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void RemoveBadChoices(GeoEventChoice eventChoice, GeoFaction faction, ref bool result)
            {
                try
                {



                    if (eventChoice.Outcome != null && eventChoice.Outcome.GiveResearches != null && eventChoice.Outcome.GiveResearches.Count > 0 &&
                        eventChoice.Text == faction.Research.GetResearchById(eventChoice.Outcome.GiveResearches[0]).ResearchDef.ViewElementDef.ResearchName &&
                        faction.Research.HasCompleted(eventChoice.Outcome.GiveResearches[0]))
                    {
                        TFTVLogger.Always($"Phoenix Porject has already completed {eventChoice.Text}");

                        result = false;

                    }

                    if (eventChoice.Outcome != null && eventChoice.Outcome.Units != null && eventChoice.Outcome.Units.Count > 0 && eventChoice.Outcome.Units[0].Data.GameTags.Contains(MercenaryTag) && faction is GeoPhoenixFaction phoenixFaction && phoenixFaction.LivingQuarterFull)
                    {
                        // TFTVLogger.Always($"Living Quarters are full! Can't recruit Mercenary");
                        result = false;
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }



            }



            private static GeoEventChoice GenerateResearchChoice(ResearchDef researchDef, float price)
            {
                try
                {
                    GeoEventChoice geoEventChoice = GenerateChoice(price);
                    geoEventChoice.Outcome.GiveResearches.Add(researchDef.Id);
                    geoEventChoice.Text = researchDef.ViewElementDef?.ResearchName;
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GeoEventChoice GenerateItemChoice(ItemDef itemDef, float price)
            {
                try
                {
                    // TFTVLogger.Always($"item def is {itemDef.name}");

                    GeoEventChoice geoEventChoice = GenerateChoice(price);
                    if (itemDef is GroundVehicleItemDef groundVehicleItemDef)
                    {
                        geoEventChoice.Outcome.Units.Add(groundVehicleItemDef.VehicleTemplateDef);
                    }
                    else
                    {
                        geoEventChoice.Outcome.Items.Add(new ItemUnit(itemDef, 1));
                    }

                    geoEventChoice.Text = itemDef.GetDisplayName();
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static GeoEventChoice GenerateChoice(float price)
            {
                try
                {
                    GeoEventChoice geoEventChoice = new GeoEventChoice
                    {
                        Requirments = new GeoEventChoiceRequirements(),
                        Outcome = new GeoEventChoiceOutcome()
                    };
                    geoEventChoice.Requirments.Resources.Add(new ResourceUnit(ResourceType.Materials, price));
                    geoEventChoice.Outcome.ReEneableEvent = true;
                    return geoEventChoice;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static List<GeoMarketplaceOptionDef> GetOptionsByType(List<GeoMarketplaceOptionDef> currentlyPossibleOptions, GameTagDef itemTypeTag)
            {
                try
                {
                    return
                        new List<GeoMarketplaceOptionDef>(currentlyPossibleOptions).Where(o => o is GeoMarketplaceItemOptionDef item && item.ItemDef.Tags.Contains(itemTypeTag)).ToList();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static GeoMarketplaceItemOptionDef[] NewJericho_Items()
            {
                GeoMarketplaceItemOptionDef[] Options = new GeoMarketplaceItemOptionDef[]
                {
                (GeoMarketplaceItemOptionDef)Repo.GetDef("a5833903-97b1-71f4-9b7c-b0755e8decf7"), //Purgatory
                (GeoMarketplaceItemOptionDef)Repo.GetDef("03ebb7ca-08d7-36a4-2bf6-851b47682476"), //Lightweight Alloy
                (GeoMarketplaceItemOptionDef)Repo.GetDef("46a57a6d-7163-8ef4-99b3-8167efb46edc"), //Supercharger
                };
                return Options;
            }

            private static GeoMarketplaceItemOptionDef[] Synedrion_Items()
            {
                GeoMarketplaceItemOptionDef[] Options = new GeoMarketplaceItemOptionDef[]
                {
                (GeoMarketplaceItemOptionDef)Repo.GetDef("017b69c2-8a8f-e784-6b36-70cc804ece5d"), //Apollo
                (GeoMarketplaceItemOptionDef)Repo.GetDef("456bf1a1-82ce-2f54-9a0a-27600107d5b4"), //Psychic Jammer
                (GeoMarketplaceItemOptionDef)Repo.GetDef("3e192929-51ba-29e4-7ac1-e9ab2836f076"), //Experimental Thrusters
                };
                return Options;
            }


            /// <summary>
            /// When MarketPlace is discovered, 
            /// 
            /// 1) NumberOfDLC5MissionsCompletedVariable is set to 4 (to remove everything connected to DLC5 mission generation).
            /// 
            /// 2) _updateOptionsNextTime is set to now and updateOptionsWithRespectToTime is forcefully run
            ///  
            /// When UpdateOptionsWithRespectToTime is run, it checks whether _updateOptionsNextTime is past now. 
            /// 
            /// If it is, UpdateOptions(Timing) is run. 
            /// </summary>


            [HarmonyPatch(typeof(GeoMarketplace), "UpdateOptionsWithRespectToTime")]
            public static class GeoMarketplace_UpdateOptionsWithRespectToTime_patch
            {
                public static bool Prefix(ref TimeUnit ____updateOptionsNextTime, GeoLevelController ____level, GeoMarketplace __instance)
                {
                    try
                    {
                        TFTVLogger.Always($"UpdateOptionsWithRespectToTime: ____updateOptionsNextTime is {____updateOptionsNextTime.DateTime}, ____level.Timing.Now is {____level.Timing.Now.DateTime} ");

                        if (____level.Timing.Now < ____updateOptionsNextTime)
                        {

                        }
                        else
                        {
                            /*  UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                              int hours = UnityEngine.Random.Range(65, 90);


                              ____updateOptionsNextTime = TimeUtils.GetNextTimeInHours(____level.Timing, hours);*/

                            //TFTVLogger.Always($"After trigger: UpdateOptionsWithRespectToTime: ____updateOptionsNextTime is {____updateOptionsNextTime.DateTime}, ____level.Timing.Now is {____level.Timing.Now.DateTime} ");
                            __instance.UpdateOptions(____level.Timing);


                        }
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            /// <summary>
            /// When UpdateOptions(Timing) is run, if 
            /// 
            /// 1) NumberOfDLC5MissionsCompletedVariable > 0 (that is, if MarketPlace has been explored) and
            /// 2) current time passed _updateOptionsNextTime
            /// 
            /// Then
            /// 
            /// 1) UpdateOptions is run;
            /// 2) _updateOptionsNextTime is to 65-90 hours from now
            /// 3) LogEntry is created
            /// 4) MarketRotation variable is increased by 1
            /// 
            /// </summary>

            [HarmonyPatch(typeof(GeoMarketplace), "UpdateOptions", new Type[] { typeof(Timing) })]
            public static class GeoMarketplace_UpdateOptionsTiming_patch
            {
                public static bool Prefix(ref TimeUnit ____updateOptionsNextTime, GeoLevelController ____level, Timing timing, GeoMarketplace __instance, TheMarketplaceSettingsDef ____settings)
                {
                    try
                    {

                        // TFTVLogger.Always($"UpdateOptions(Timing) is called (Prefix) Current time: {____level.Timing.Now.DateTime}. Next update: {____updateOptionsNextTime.DateTime}");

                        if (timing.Now >= ____updateOptionsNextTime && ____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) > 0)
                        {
                            MethodInfo updateOptionsMethod = typeof(GeoMarketplace).GetMethod("UpdateOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                            updateOptionsMethod.Invoke(__instance, null);
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int hours = UnityEngine.Random.Range(65, 90);
                            ____updateOptionsNextTime = TimeUtils.GetNextTimeInHours(____level.Timing, hours);

                            CreateLogEntryAndRollSpecialsMarketplaceUpdated(____level);

                            ____level.EventSystem.SetVariable(_marketPlaceStockRotated, ____level.EventSystem.GetVariable(_marketPlaceStockRotated) + 1);
                            TFTVLogger.Always($"number of stock rotations is {____level.EventSystem.GetVariable(_marketPlaceStockRotated)}");
                        }


                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                /*     public static void Postfix(TimeUnit ____updateOptionsNextTime, GeoLevelController ____level)
                     {
                         try
                         {
                           //  TFTVLogger.Always($"UpdateOptions(Timing) Postfix: Current time: {____level.Timing.Now.DateTime}. Next update: {____updateOptionsNextTime.DateTime}");
                         }
                         catch (Exception e)
                         {
                             TFTVLogger.Error(e);
                             throw;
                         }
                     }*/


            }


            [HarmonyPatch(typeof(GeoMarketplace), "UpdateOptions", new Type[] { })]

            public static class GeoMarketplace_UpdateOptions_MarketPlace_patch
            {
                public static bool Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings, TimeUnit ____updateOptionsNextTime)
                {
                    try
                    {
                        GenerateMarketPlaceOptionsOnUpdateOptions(__instance, ____level, ____settings, ____updateOptionsNextTime);

                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void GenerateMarketPlaceOptionsOnUpdateOptions(GeoMarketplace geoMarketPlace, GeoLevelController controller, TheMarketplaceSettingsDef marketPlaceSettings, TimeUnit updateOptionsNextTime)
            {
                try
                {
                    TFTVLogger.Always($"Updating marketplace options. Current time: {controller.Timing.Now.DateTime}. Next update: {updateOptionsNextTime.DateTime}");

                    if (controller.EventSystem.GetVariable(_marketPlaceStockRotated) > 2)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int diceRoll = UnityEngine.Random.Range(1, 11);
                        if (diceRoll <= 3)
                        {
                            _currentMarketPlaceSpecial = _marketPlaceSpecials.GetRandomElement();
                        }
                    }


                    geoMarketPlace.MarketplaceChoices.Clear();

                    int numberOfStockRotations = controller.EventSystem.GetVariable(_marketPlaceStockRotated);

                    TFTVLogger.Always($"number of stock rotations is {controller.EventSystem.GetVariable(_marketPlaceStockRotated)}");

                    int numberOfOffers = Math.Min(8 + numberOfStockRotations * 4, 40);

                    TFTVLogger.Always($"Number of offers is {numberOfOffers}; divided by 4 {numberOfOffers / 4}");

                    List<GeoMarketplaceOptionDef> currentlyPossibleOptions = new List<GeoMarketplaceOptionDef>();

                    foreach (GeoMarketplaceOptionDef geoMarketplaceOptionDef in marketPlaceSettings.PossibleOptions)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int coinToss = UnityEngine.Random.Range(0, 2);

                        if (geoMarketplaceOptionDef.Availability - coinToss <= numberOfStockRotations)
                        {
                            currentlyPossibleOptions.Add(geoMarketplaceOptionDef);
                        }
                    }

                    currentlyPossibleOptions = CullAvailableOptionsBasedOnExternals(controller, currentlyPossibleOptions);

                    float voPriceMultiplier = TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(19) ? 0.5f : 1;

                    if (_currentMarketPlaceSpecial != null)
                    {
                        TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}");

                        if (_currentMarketPlaceSpecial == _weaponsMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more weapon choices");
                            GenerateWeaponChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 5), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                        else if (_currentMarketPlaceSpecial == _vehicleMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more vehicle choices");
                            GenerateVehicleChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 20), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                        else if (_currentMarketPlaceSpecial == _mercenaryMarketPlaceSpecial)
                        {
                            TFTVLogger.Always($"Marketspecial is {_currentMarketPlaceSpecial}, so generating more merc choices");
                            GenerateMercenaryChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 6), geoMarketPlace, voPriceMultiplier * 0.75f);
                        }
                    }
                    GenerateWeaponChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 5), geoMarketPlace, voPriceMultiplier);
                    GenerateVehicleChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 10), geoMarketPlace, voPriceMultiplier);
                    GenerateMercenaryChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 6), geoMarketPlace, voPriceMultiplier);
                    GenerateResearchChoices(currentlyPossibleOptions, Math.Min(numberOfOffers / 4, 8), geoMarketPlace, voPriceMultiplier);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoMarketplaceOptionDef> CullAvailableOptionsBasedOnExternals(GeoLevelController controller, List<GeoMarketplaceOptionDef> options)
            {
                try
                {

                    if (controller != null && controller.NewJerichoFaction != null && controller.SynedrionFaction != null)
                    {
                        if (controller.NewJerichoFaction.Research.HasCompleted("NJ_VehicleTech_ResearchDef"))
                        {
                            //If complete, add more options
                            //   num += 3;
                        }
                        else
                        {
                            //Otherwise we remove NJ items from being rolled by GenerateRandomChoiceTFTV
                            options.RemoveRange(NewJericho_Items());
                        }
                        if (controller.SynedrionFaction.Research.HasCompleted("SYN_Rover_ResearchDef"))
                        {
                            // num += 3;
                        }
                        else
                        {
                            options.RemoveRange(Synedrion_Items());
                        }
                    }

                    return options;


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static List<GeoEventChoice> GenerateWeaponChoices(List<GeoMarketplaceOptionDef> availableOptions,
                int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    List<GeoMarketplaceOptionDef> weaponsAvailable = GetOptionsByType(availableOptions, TFTVKaosGuns._kGTag);

                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (weaponsAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef weaponOffer = (GeoMarketplaceItemOptionDef)weaponsAvailable.GetRandomElement();

                        // TFTVLogger.Always($"weaponOffer is {weaponOffer.name}");

                        weaponsAvailable.Remove(weaponOffer);

                        int price = (int)(UnityEngine.Random.Range(weaponOffer.MinPrice, weaponOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(weaponOffer.ItemDef, price);
                        GeoMarketplaceItemOptionDef ammoOffer = TFTVKaosGuns._kGWeaponsAndAmmo[weaponOffer];

                        int ammoPrice = (int)(UnityEngine.Random.Range(ammoOffer.MinPrice, ammoOffer.MaxPrice) * priceModifier);

                        List<GeoEventChoice> ammo = new List<GeoEventChoice>()
                    {
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                        GenerateItemChoice(ammoOffer.ItemDef, ammoPrice),
                    };

                        geoMarketplace.MarketplaceChoices.Add(item);
                        geoMarketplace.MarketplaceChoices.AddRange(ammo);
                        //  TFTVLogger.Always($"should have added {weaponOffer.name} and 3 ammo for it");
                    }

                    return list;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoEventChoice> GenerateVehicleChoices(List<GeoMarketplaceOptionDef> availableOptions,
                int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    List<GeoMarketplaceOptionDef> vehicleItemsAvailable = GetOptionsByType(availableOptions, _vehicle_ClassTagDef);

                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (vehicleItemsAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef vehicleItemToOffer;
                        if (x == 0)
                        {

                            vehicleItemToOffer = DefCache.GetDef<GeoMarketplaceItemOptionDef>("KasoBuggy_MarketplaceItemOptionDef");

                        }
                        else
                        {
                            vehicleItemToOffer = (GeoMarketplaceItemOptionDef)vehicleItemsAvailable.GetRandomElement();
                        }

                        vehicleItemsAvailable.Remove(vehicleItemToOffer);

                        int price = (int)(UnityEngine.Random.Range(vehicleItemToOffer.MinPrice, vehicleItemToOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(vehicleItemToOffer.ItemDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);

                        // TFTVLogger.Always($"should have added {vehicleItemToOffer.name}");
                    }

                    return list;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<GeoEventChoice> GenerateMercenaryChoices(List<GeoMarketplaceOptionDef> availableOptions,
               int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoMarketplaceOptionDef> mercernariesAvailable = GetOptionsByType(availableOptions, MercenaryTag);

                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (mercernariesAvailable.Count() == 0)
                        {
                            break;
                        }

                        GeoMarketplaceItemOptionDef mercenaryToOffer = (GeoMarketplaceItemOptionDef)mercernariesAvailable.GetRandomElement();

                        mercernariesAvailable.Remove(mercenaryToOffer);

                        int price = (int)(UnityEngine.Random.Range(mercenaryToOffer.MinPrice, mercenaryToOffer.MaxPrice) * priceModifier);

                        GeoEventChoice item = GenerateItemChoice(mercenaryToOffer.ItemDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);
                        // TFTVLogger.Always($"should have added {mercenaryToOffer.name}");
                    }

                    return list;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void GenerateResearchChoices(List<GeoMarketplaceOptionDef> availableOptions,
              int numberToGenerate, GeoMarketplace geoMarketplace, float priceModifier)
            {
                try
                {
                    List<GeoEventChoice> list = new List<GeoEventChoice>();

                    List<GeoMarketplaceOptionDef> researchOptions = availableOptions.Where(o => o is GeoMarketplaceResearchOptionDef).ToList();

                    if (_currentMarketPlaceSpecial != null && _currentMarketPlaceSpecial == _researchMarketPlaceSpecial)
                    {
                        TFTVLogger.Always($"research special!");
                        numberToGenerate = 8;
                        priceModifier *= 0.5f;

                    }

                    if (researchOptions.Count == 0)
                    {
                        return;

                    }


                    for (int x = 0; x < numberToGenerate; x++)
                    {
                        if (researchOptions.Count() == 0)
                        {
                            break;

                        }

                        GeoMarketplaceResearchOptionDef researchToOffer = (GeoMarketplaceResearchOptionDef)researchOptions.GetRandomElement();

                        researchOptions.Remove(researchToOffer);

                        int price = (int)(UnityEngine.Random.Range(researchToOffer.MinPrice, researchToOffer.MaxPrice) * priceModifier);

                        ResearchDef researchDef = researchToOffer.GetResearch();

                        if (researchDef == null)
                        {
                            break;
                        }


                        GeoEventChoice item = GenerateResearchChoice(researchDef, price);

                        geoMarketplace.MarketplaceChoices.Add(item);
                        //  TFTVLogger.Always($"should have added {researchDef.Id}");
                    }

                    _researchesAlreadyRolled.Clear();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static List<ResearchDef> _researchesAlreadyRolled = new List<ResearchDef>();

            [HarmonyPatch(typeof(GeoMarketplaceResearchOptionDef), "GetRandomResearch")]
            public static class GeoMarketplaceResearchOptionDef_GetRandomResearch_MarketPlace_patch
            {
                public static bool Prefix(ref ResearchDef __result)
                {
                    try
                    {
                        GeoLevelController level = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                        List<ResearchElement> list = new List<ResearchElement>();

                        for (int x = 0; x < level.FactionsWithDiplomacy.Count(); x++)
                        {
                            list.AddRange(level.FactionsWithDiplomacy.ElementAt(x).Research.Completed.Where((ResearchElement r) => r.IsAvailableToFaction(level.PhoenixFaction)).ToList());
                        }

                        if (list.Count == 0)
                        {
                            TFTVLogger.Always($"No researches! Player knows all!");
                            return false;
                        }

                        List<ResearchElement> phoenixFactionCompletedResearches = level.PhoenixFaction.Research.RevealedAndCompleted.ToList();
                        list.RemoveAll((ResearchElement research) => phoenixFactionCompletedResearches.Any((ResearchElement phoenixResearch) => research.ResearchID == phoenixResearch.ResearchID));

                        // TFTVLogger.Always($"_researchesAlreadyRolled has any elements in it? {_researchesAlreadyRolled.Count > 0}");

                        if (_researchesAlreadyRolled.Count > 0)
                        {
                            list.RemoveAll(e => _researchesAlreadyRolled.Contains(e.ResearchDef));
                            //  TFTVLogger.Always($"removing already rolled researches from pool");
                        }

                        if (list.Count != 0)
                        {
                            // TFTVLogger.Always($"There are {list.Count} researches that could be offered to the player in the Marketplace");
                            __result = list.ElementAt(UnityEngine.Random.Range(0, list.Count)).ResearchDef;
                            _researchesAlreadyRolled.Add(__result);
                        }
                        else
                        {
                            __result = null;
                        }
                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            private static void CreateLogEntryAndRollSpecialsMarketplaceUpdated(GeoLevelController controller)
            {
                try
                {


                    string textToDisplay = $"{TFTVCommonMethods.ConvertKeyToString("KEY_MARKETPLACE_NEW_STOCK")} {TFTVCommonMethods.ConvertKeyToString(_currentMarketPlaceSpecial)} ";

                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                    {
                        Text = new LocalizedTextBind(textToDisplay, true)
                    };
                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller.Log, new object[] { entry, null });
                    controller.View.SetGamePauseState(true);

                    _currentMarketPlaceSpecial = null;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            [HarmonyPatch(typeof(GeoMarketplace), "AfterMissionComplete")]
            public static class GeoMarketplace_AfterMissionComplete_patch
            {
                public static bool Prefix()
                {
                    try
                    {
                        TFTVLogger.Always($"Canceling GeoMarketPlace AfterMissionComplete");
                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


        }

        internal class TFTVMarketPlaceUI
        {



            private static void FakeResearchOptionToSetupCharacterSale(UIModuleTheMarketplace uIModuleTheMarketplace)
            {
                try
                {
                    // GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                    uIModuleTheMarketplace.ResearchRoot.SetActive(value: true);
                    uIModuleTheMarketplace.ItemsRoot.SetActive(value: false);
                    ResearchDef researchById = uIModuleTheMarketplace.Context.Level.GetResearchById("PX_Synedrion_ResearchDef");
                    ResearchElement researchElement = new ResearchElement(researchById);
                    researchElement.Init(uIModuleTheMarketplace.Context.ViewerFaction, researchById);
                    researchElement.State = ResearchState.Revealed;
                    uIModuleTheMarketplace.ResearchInfo.Init(researchElement);


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void ResearchInfoForCharacterKludge(GeoEventChoice choice, UIModuleTheMarketplace uIModuleTheMarketplace)
            {
                try
                {


                    if (choice != null && choice.Outcome != null && choice.Outcome.Units != null && choice.Outcome.Units.Count > 0
                        && choice.Outcome.Units[0] is TacCharacterDef tacCharacterDef && tacCharacterDef.Data.GameTags.Contains(MercenaryTag))
                    {
                        FakeResearchOptionToSetupCharacterSale(uIModuleTheMarketplace);

                        uIModuleTheMarketplace.ResearchRoot.SetActive(false);
                        uIModuleTheMarketplace.ResearchRoot.SetActive(true);

                        uIModuleTheMarketplace.ResearchInfo.Title.text = TFTVCommonMethods.ConvertKeyToString(tacCharacterDef.Data.ViewElementDef.DisplayName1.LocalizationKey);
                        /*  uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta =
                              new Vector2(uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta.x * 2, uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta.y);
                          uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMaxSize = 48;*/

                        /*   TFTVLogger.Always($"font size: {uIModuleTheMarketplace.ResearchInfo.Title.fontSize}; " +
                               $"size of rectransfrom {uIModuleTheMarketplace.ResearchInfo.Title.rectTransform.sizeDelta}; " +
                               $"resize text max size: {uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMaxSize};" +
                               $"resize text min size:{uIModuleTheMarketplace.ResearchInfo.Title.resizeTextMinSize}" +
                               $"resize text for best fit: {uIModuleTheMarketplace.ResearchInfo.Title.resizeTextForBestFit}");*/

                        uIModuleTheMarketplace.ResearchInfo.Description.text = TFTVCommonMethods.ConvertKeyToString(tacCharacterDef.Data.ViewElementDef.Description.LocalizationKey);
                        uIModuleTheMarketplace.ResearchInfo.BenefitsContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.ResourceContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.RequirementsContainer.SetActive(false);
                        uIModuleTheMarketplace.ResearchInfo.Icon.sprite = tacCharacterDef.Data.ViewElementDef.InventoryIcon;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            [HarmonyPatch(typeof(UIModuleTheMarketplace), "SetupChoiceInfoBlock")]
            public static class UIModuleTheMarketplace_SetupChoiceInfoBlock_patch
            {
                public static void Postfix(UIModuleTheMarketplace __instance, GeoEventChoice choice)
                {
                    try
                    {

                        ResearchInfoForCharacterKludge(choice, __instance);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleTheMarketplace), "UpdateVisuals")]
            public static class UIModuleTheMarketplace_UpdateVisuals_patch
            {
                public static void Postfix(UIModuleTheMarketplace __instance)
                {
                    try
                    {
                        Text timeToRestock = __instance.transform.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name.Equals("OffersHint"));
                        string text = TFTVCommonMethods.ConvertKeyToString("DLC5/KEY_MARKETPLACE_UPDATE_DESCRIPTION");

                        GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                        GeoMarketplace geoMarketplace = controller.Marketplace;

                        FieldInfo fieldInfo_updateOptionsNextTime = typeof(GeoMarketplace).GetField("_updateOptionsNextTime", BindingFlags.Instance | BindingFlags.NonPublic);

                        TimeUnit updateTime = (TimeUnit)fieldInfo_updateOptionsNextTime.GetValue(geoMarketplace);
                        TimeUnit currentTime = controller.Timing.Now;

                        int daysToRotation = Mathf.Max(updateTime.DateTime.Day - currentTime.DateTime.Day, 1);

                        string suffix = TFTVCommonMethods.ConvertKeyToString("KEY_DAYS");

                        if (daysToRotation == 1)
                        {
                            suffix = TFTVCommonMethods.ConvertKeyToString("KEY_DAY");
                        }

                        timeToRestock.text = $"{text} {daysToRotation} {suffix.ToUpper()}";

                        __instance.NoOffersAvailableHint.SetActive(false);

                        //   TFTVLogger.Always($"Running UpdateVisuals");

                        CreateItemFilter();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(UIModuleTheMarketplace), "OnChoiceSelected")]
            public static class UIModuleTheMarketplace_OnChoiceSelected_patch
            {

                public static void Postfix(UIModuleTheMarketplace __instance, GeoEventChoice choice)
                {
                    try
                    {

                        //  TFTVLogger.Always($"Running OnChoiceSelected");
                        if (MPGeoEventChoices != null && MPGeoEventChoices.Contains(choice))
                        {
                            //    TFTVLogger.Always($"Removing choice from internally saved list");

                            MPGeoEventChoices.Remove(choice);

                        }

                        if (choice.Outcome.Units.Count > 0 && choice.Outcome.Units[0] is TacCharacterDef tacCharacterDef && tacCharacterDef.Data.GameTags.Contains(MercenaryTag))
                        {


                            //  TFTVLogger.Always($"got to this if here");

                            __instance.Loca_AllMissionsFinishedDesc.LocalizationKey = tacCharacterDef.Data.ViewElementDef.Category.LocalizationKey;
                            __instance.UpdateVisuals();
                        }

                        CheckSecretMPCounter();

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            [HarmonyPatch(typeof(UIStateMarketplaceGeoscapeEvent), "ExitState")]
            public static class UIStateMarketplaceGeoscapeEvent_ExitState_patch
            {

                public static void Postfix()
                {
                    try
                    {
                        //   TFTVLogger.Always($"Running ExitState marketplace");
                        GeoMarketplace geoMarketplace = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().Marketplace;
                        if (MPGeoEventChoices != null && MPGeoEventChoices.Count > 0)
                        {
                            PropertyInfo propertyInfo = typeof(GeoMarketplace).GetProperty("MarketplaceChoices", BindingFlags.Instance | BindingFlags.Public);

                            // TFTVLogger.Always($"before manually transferring the MarketChoices {propertyInfo.GetValue(geoMarketplace)}");                
                            propertyInfo.SetValue(geoMarketplace, new List<GeoEventChoice>(MPGeoEventChoices));
                            //  TFTVLogger.Always($"after manually transferring the MarketChoices {propertyInfo.GetValue(geoMarketplace)}");
                            MPGeoEventChoices = null;
                            //  TFTVLogger.Always($"after clearing the internal MarketChoices list {propertyInfo.GetValue(geoMarketplace)}");
                            UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;
                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("UI_KaosMarket_Image_uinomipmaps.jpg");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }



            public static PhoenixGeneralButton MarketToggleButton = null;

            private static void CreateItemFilter()
            {
                try
                {
                    if (MarketToggleButton == null)
                    {

                        UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;

                        marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                        marketplaceUI.MissionRewardDescriptionText.gameObject.SetActive(true);

                        marketplaceUI.MissionRewardHeaderText.text = "";
                        marketplaceUI.MissionRewardDescriptionText.text = "";

                        Resolution resolution = Screen.currentResolution;



                        // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                        float resolutionFactorWidth = (float)resolution.width / 1920f;
                        //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                        float resolutionFactorHeight = (float)resolution.height / 1080f;
                        //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);
                        bool ultrawideresolution = resolutionFactorWidth / resolutionFactorHeight > 2;
                        //  marketplaceUI.MissionRewardHeaderText.gameObject.SetActive(true);
                        PhoenixGeneralButton allToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton vehicleToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton equipmentToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);
                        PhoenixGeneralButton otherToggle = UnityEngine.Object.Instantiate(marketplaceUI.LocateMissionButton, marketplaceUI.MissionRewardDescriptionText.transform);

                        allToggle.gameObject.AddComponent<UITooltipText>().TipText = "ALL";
                        allToggle.gameObject.SetActive(true);
                        allToggle.PointerClicked += () => ToggleButtonClicked(0);
                        allToggle.transform.GetComponentInChildren<Text>().text = "ALL";
                        //  allToggle.transform.localScale *= 0.6f;

                        if (!ultrawideresolution)
                        {
                            allToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }


                        allToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x * 0.65f, allToggle.GetComponent<RectTransform>().sizeDelta.y);


                        vehicleToggle.gameObject.AddComponent<UITooltipText>().TipText = "VEHICLES";
                        vehicleToggle.gameObject.SetActive(true);
                        vehicleToggle.PointerClicked += () => ToggleButtonClicked(1);
                        vehicleToggle.transform.GetComponentInChildren<Text>().text = "VEHICLES";
                        vehicleToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("UI_Vehicle_FilterIcon.png");

                        if (!ultrawideresolution)
                        {
                            vehicleToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 0, 0); //new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }

                        vehicleToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);


                        equipmentToggle.gameObject.AddComponent<UITooltipText>().TipText = "EQUIPMENT";
                        equipmentToggle.gameObject.SetActive(true);
                        equipmentToggle.PointerClicked += () => ToggleButtonClicked(2);
                        equipmentToggle.transform.GetComponentInChildren<Text>().text = "EQUIPMENT";
                        //    equipmentToggle.transform.localScale *= 0.5f;

                        if (!ultrawideresolution)
                        {
                            equipmentToggle.transform.position -= new Vector3(-150 * resolutionFactorWidth, 0, 0);
                        }
                        equipmentToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);
                        equipmentToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("MP_UI_Choices_Equipment.png");

                        otherToggle.gameObject.AddComponent<UITooltipText>().TipText = "OTHER";
                        otherToggle.gameObject.SetActive(true);
                        otherToggle.PointerClicked += () => ToggleButtonClicked(3);
                        otherToggle.transform.GetComponentInChildren<Text>().text = "OTHER";
                        //   otherToggle.transform.localScale *= 0.5f;
                        if (!ultrawideresolution)
                        {
                            otherToggle.transform.position -= new Vector3(150 * resolutionFactorWidth, 100 * resolutionFactorHeight, 0);
                        }
                        otherToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(allToggle.GetComponent<RectTransform>().sizeDelta.x, allToggle.GetComponent<RectTransform>().sizeDelta.y);
                        otherToggle.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name == "Icon").sprite = Helper.CreateSpriteFromImageFile("Geoscape_Icon_Research.png");

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static List<GeoEventChoice> MPGeoEventChoices = null;


            private static bool CheckIfMarketChoiceVehicle(GeoEventChoice choice)
            {
                try
                {

                    if (choice.Outcome.Items.Count > 0 && choice.Outcome.Items[0].ItemDef.name.Contains("GroundVehicle")
                        || choice.Outcome.Units.Count > 0 && choice.Outcome.Units[0].name.Contains("KS_Kaos_Buggy")) //&& choice.Outcome.Units[0].name.Contains("KS_Kaos_Buggy")))
                    {
                        return true;
                    }
                    else return false;
                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool CheckIfMarketChoiceWeaponOrAmmo(GeoEventChoice choice)
            {
                try
                {
                    if (choice.Outcome != null && choice.Outcome.Items != null
                                        && choice.Outcome.Items.Count > 0 && choice.Outcome.Items[0].ItemDef != null
                                        && (choice.Outcome.Items[0].ItemDef.name.Contains("WeaponDef") || choice.Outcome.Items[0].ItemDef.name.Contains("AmmoClip"))
                                        && !choice.Outcome.Items[0].ItemDef.name.Contains("GroundVehicle"))
                    {

                        return true;

                    }
                    else return false;


                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            public static void FilterMarketPlaceOptions(GeoMarketplace geoMarketplace, int filter)
            {
                try
                {
                    PropertyInfo propertyInfoMarketPlaceChoicesGeoEventChoice = typeof(GeoMarketplace).GetProperty("MarketplaceChoices", BindingFlags.Instance | BindingFlags.Public);

                    if (MPGeoEventChoices == null)
                    {
                        // TFTVLogger.Always($"saving all Choices to internal list, count is {geoMarketplace.MarketplaceChoices.Count}");
                        MPGeoEventChoices = geoMarketplace.MarketplaceChoices;
                    }
                    else
                    {
                        // TFTVLogger.Always($"passing all Choices from internal list, count {MPGeoEventChoices.Count}, to proper list, count {geoMarketplace.MarketplaceChoices.Count}");
                        propertyInfoMarketPlaceChoicesGeoEventChoice?.SetValue(geoMarketplace, MPGeoEventChoices);

                    }

                    List<GeoEventChoice> choicesToShow = new List<GeoEventChoice>();

                    if (filter != 0)
                    {
                        if (filter == 1)
                        {
                            // TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (CheckIfMarketChoiceVehicle(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    //TFTVLogger.Always($"the vehicle equipment choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }

                            propertyInfoMarketPlaceChoicesGeoEventChoice.SetValue(geoMarketplace, choicesToShow);

                        }
                        else if (filter == 2)
                        {
                            //   TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (CheckIfMarketChoiceWeaponOrAmmo(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    // TFTVLogger.Always($"the weapon or ammo choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }

                        }
                        else if (filter == 3)
                        {
                            //   TFTVLogger.Always($"There are {geoMarketplace.MarketplaceChoices.Count} choices");

                            for (int i = 0; i < geoMarketplace.MarketplaceChoices.Count; i++)
                            {
                                if (!CheckIfMarketChoiceWeaponOrAmmo(geoMarketplace.MarketplaceChoices[i]) && !CheckIfMarketChoiceVehicle(geoMarketplace.MarketplaceChoices[i]))
                                {
                                    // TFTVLogger.Always($"the other choice number {i} is {geoMarketplace.MarketplaceChoices[i].Outcome.Items[0].ItemDef.name}");
                                    choicesToShow.Add(geoMarketplace.MarketplaceChoices[i]);
                                }
                            }
                        }

                        propertyInfoMarketPlaceChoicesGeoEventChoice.SetValue(geoMarketplace, choicesToShow);
                    }
                    //  TFTVLogger.Always($"Count of proper list (that will be shown) is {geoMarketplace.MarketplaceChoices.Count}");

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ToggleButtonClicked(int filter)
            {
                try
                {
                    GeoMarketplace geoMarketplace = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().Marketplace;
                    UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;
                    FieldInfo fieldInfoGeoEventGeoscapeEvent = typeof(UIModuleTheMarketplace).GetField("_geoEvent", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo methodInfoUpdateList = typeof(UIModuleTheMarketplace).GetMethod("UpdateList", BindingFlags.NonPublic | BindingFlags.Instance);


                    switch (filter)
                    {
                        case 0:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_All.jpg");
                            break;

                        case 1:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("loading_screen26.jpg");
                            break;

                        case 2:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_Equipment.jpg");
                            break;

                        case 3:

                            marketplaceUI.transform.GetComponentsInChildren<Image>().FirstOrDefault(c => c.name.Equals("Picture")).sprite = Helper.CreateSpriteFromImageFile("MP_Choices_Other.jpg");
                            break;

                    }

                    marketplaceUI.ListScrollRect.ScrollToElement(0);
                    FilterMarketPlaceOptions(geoMarketplace, filter);

                    methodInfoUpdateList.Invoke(marketplaceUI, new object[] { fieldInfoGeoEventGeoscapeEvent.GetValue(marketplaceUI) });


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }



            /// <summary>
            /// Easter egg conversation in the Marketplace after player makes a lot of purchases
            /// </summary>


            public static int SecretMPCounter;
            private static void CheckSecretMPCounter()
            {
                try
                {
                    SecretMPCounter++;

                    UIModuleTheMarketplace marketplaceUI = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.TheMarketplaceModule;

                    if (SecretMPCounter >= 20 && SecretMPCounter <= 41)
                    {

                        TFTVLogger.Always("Should trigger MP EE");
                        // TFTVLogger.Always($"{marketplaceUI.MissionDescriptionText.text}");

                        marketplaceUI.Loca_AllMissionsFinishedDesc.LocalizationKey = "KEY_SECRET_MARKETPLACE_TEXT" + (SecretMPCounter - 20);

                        // marketplaceUI.MissionDescriptionText.text = TFTVCommonMethods.ConvertKeyToString("KEY_SECRET_MARKETPLACE_TEXT0");// + );


                    }

                    if (SecretMPCounter >= 40)
                    {
                        SecretMPCounter = 0;
                        marketplaceUI.Loca_AllMissionsFinishedDesc.LocalizationKey = "KEY_MARKETPLACE_DESCRIPTION_5";
                    }

                }


                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        /// <summary>
        /// Ensures that rescue vehicle missions will not contain faction vehicles if they haven't been researched by the faction yet.
        /// commented out 2/1 because 1) less variety in vehicles gained, and 2) introduced weird meta to wait until vehicles researched
        /// </summary>

        /*   [HarmonyPatch(typeof(GeoMissionGenerator), "GetRandomMission", new Type[] { typeof(IEnumerable<MissionTagDef>), typeof(ParticipantFilter), typeof(Func<TacMissionTypeDef, bool>) })]
           public static class GeoMissionGenerator_GetRandomMission_patch
           {

               public static void Prefix(IEnumerable<MissionTagDef> tags, out List<CustomMissionTypeDef> __state, GeoLevelController ____level)
               {
                   try
                   {
                       ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                       ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");

                       MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                       __state = new List<CustomMissionTypeDef>();


                       if (tags.Contains(requiresVehicle) && ____level != null)
                       {
                           TFTVLogger.Always($"Generating rescue Vehicle scav; checking if factions have researched Aspida/Armadillo");
                           GeoLevelController controller = ____level;

                           if (controller.NewJerichoFaction.Research != null && !controller.NewJerichoFaction.Research.HasCompleted("NJ_VehicleTech_ResearchDef"))
                           {
                               TFTVLogger.Always($"Armadillo not researched by New Jericho");

                               foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                               {
                                   if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == armadillo)
                                   {
                                       __state.Add(customMissionTypeDef);
                                   }
                               }
                           }
                           if (controller.SynedrionFaction.Research != null && !controller.SynedrionFaction.Research.HasCompleted("SYN_Rover_ResearchDef"))
                           {
                               TFTVLogger.Always($"Aspida not researched by Synedrion");

                               foreach (CustomMissionTypeDef customMissionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>().Where(m => m.Tags.Contains(requiresVehicle)))
                               {
                                   if (customMissionTypeDef.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag == aspida)
                                   {
                                       __state.Add(customMissionTypeDef);
                                   }
                               }
                           }

                           if (__state.Count > 0)
                           {
                               TFTVLogger.Always($"Removing rescue vehicle missions with not researched vehicles from generation pool");

                               foreach (CustomMissionTypeDef mission in __state)
                               {
                                   mission.Tags.Remove(requiresVehicle);
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


               public static void Postfix(IEnumerable<MissionTagDef> tags, in List<CustomMissionTypeDef> __state)
               {
                   try
                   {
                       ClassTagDef aspida = DefCache.GetDef<ClassTagDef>("Aspida_ClassTagDef");
                       ClassTagDef armadillo = DefCache.GetDef<ClassTagDef>("Armadillo_ClassTagDef");


                       MissionTagDef requiresVehicle = DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef");

                       if (tags.Contains(DefCache.GetDef<MissionTagDef>("Contains_RescueVehicle_MissionTagDef")) && __state.Count > 0)
                       {
                           TFTVLogger.Always($"Adding back missions that were removed from the pool");

                           foreach (CustomMissionTypeDef mission in __state)
                           {

                               if (!mission.Tags.Contains(requiresVehicle))
                               {
                                   mission.Tags.Add(requiresVehicle);
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
           }*/


        public static void ForceMarketPlaceUpdate()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();

                GeoMarketplace geoMarketplace = controller.Marketplace;
                MethodInfo updateOptionsWithRespectToTimeMethod = typeof(GeoMarketplace).GetMethod("UpdateOptionsWithRespectToTime", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo updateOptionsNextTimeField = typeof(GeoMarketplace).GetField("_updateOptionsNextTime", BindingFlags.NonPublic | BindingFlags.Instance);

                updateOptionsNextTimeField.SetValue(geoMarketplace, controller.Timing.Now);
                TFTVLogger.Always($"Forced Marketplace options update; changing next update time to now, {controller.Timing.Now.DateTime}");

                updateOptionsWithRespectToTimeMethod.Invoke(geoMarketplace, null);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        [HarmonyPatch(typeof(GeoMarketplace), "OnSiteVisited")]
        public static class GeoMarketplace_OnSiteVisited_MarketPlace_patch
        {
            public static void Prefix(GeoMarketplace __instance, GeoLevelController ____level, TheMarketplaceSettingsDef ____settings)
            {
                try
                {
                    if (____level.EventSystem.GetVariable(____settings.NumberOfDLC5MissionsCompletedVariable) == 0)
                    {
                        TFTVLogger.Always($"Marketplace visited for the first time");

                        ____level.EventSystem.SetVariable(____settings.NumberOfDLC5MissionsCompletedVariable, 4);
                        ____level.EventSystem.SetVariable(____settings.DLC5IntroCompletedVariable, 1);
                        ____level.EventSystem.SetVariable(____settings.DLC5FinalMovieCompletedVariable, 1);
                        ForceMarketPlaceUpdate();
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }





    }
}


