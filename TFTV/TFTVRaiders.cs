using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVRaiders
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal class Defs
        {
            //weak armors
            private static readonly TacticalItemDef neuAssaultJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuAssaultLegs = DefCache.GetDef<TacticalItemDef>("NEU_Assault_Legs_ItemDef");

            private static readonly TacticalItemDef neuHeavyBandana = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Helmet_BodyPartDef");
            private static readonly TacticalItemDef neuHeavyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuHeavyLegs = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_Legs_ItemDef");

            private static readonly TacticalItemDef neuSniperBandana = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Helmet_BodyPartDef");
            private static readonly TacticalItemDef neuSniperCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Torso_BodyPartDef");
            private static readonly TacticalItemDef neuSniperLegs = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_Legs_ItemDef");


            //indie armors
            private static readonly TacticalItemDef inAssaultHelmet = DefCache.GetDef<TacticalItemDef>("IN_Assault_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inAssaultTorso = DefCache.GetDef<TacticalItemDef>("IN_Assault_Torso_BodyPartDef");
            private static readonly TacticalItemDef inAssaultLegs = DefCache.GetDef<TacticalItemDef>("IN_Assault_Legs_ItemDef");

            private static readonly TacticalItemDef inHeavyHelmet = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inHeavyTorso = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Torso_BodyPartDef");
            private static readonly TacticalItemDef inHeavyLegs = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Legs_ItemDef");

            private static readonly TacticalItemDef inSniperHelmet = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Helmet_BodyPartDef");
            private static readonly TacticalItemDef inSniperTorsot = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Torso_BodyPartDef");
            private static readonly TacticalItemDef inSniperLegs = DefCache.GetDef<TacticalItemDef>("IN_Sniper_Legs_ItemDef");

            //acceptable merc armors
            internal static TacticalItemDef goldAssaultHelmet = DefCache.GetDef<TacticalItemDef>("PX_Assault_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldAssaultLegs = DefCache.GetDef<TacticalItemDef>("PX_Assault_Legs_Gold_ItemDef");

            internal static TacticalItemDef goldHeavyHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavyTorso = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_Gold_BodyPartDef");
            internal static TacticalItemDef goldHeavytLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Gold_ItemDef");

            internal static TacticalItemDef spyMasterTorso = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef");
            internal static TacticalItemDef spyMasterHelmet = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef");
            internal static TacticalItemDef spyMasterLegs = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef");

            internal static TacticalItemDef sectarianHelmet = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Helmet_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianTorso = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Torso_Viking_BodyPartDef");
            internal static TacticalItemDef sectarianLegs = DefCache.GetDef<TacticalItemDef>("AN_Berserker_Legs_Viking_ItemDef");

            internal static TacticalItemDef doomLegs = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Legs_Headhunter_ItemDef");
            internal static TacticalItemDef doomTorso = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomJetpack = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Torso_JumpPack_Headhunter_BodyPartDef");
            internal static TacticalItemDef doomHelmet = DefCache.GetDef<TacticalItemDef>("PX_Heavy_Helmet_Headhunter_BodyPartDef");

            internal static TacticalItemDef risingSunTorso = DefCache.GetDef<TacticalItemDef>("PX_Sniper_Torso_RisingSun_BodyPartDef");

            internal static WeaponDef doomAC = DefCache.GetDef<WeaponDef>("PX_HeavyCannon_Headhunter_WeaponDef");
            internal static WeaponDef sectarianAxe = DefCache.GetDef<WeaponDef>("AN_Blade_Viking_WeaponDef");
            internal static WeaponDef neXbow = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
            internal static WeaponDef neSniperRifle = DefCache.GetDef<WeaponDef>("NE_SniperRifle_WeaponDef");
            internal static WeaponDef nePistol = DefCache.GetDef<WeaponDef>("NE_Pistol_WeaponDef");
            internal static WeaponDef anuPistol = DefCache.GetDef<WeaponDef>("AN_HandCannon_WeaponDef");


            internal static WeaponDef neAssaultRifle = DefCache.GetDef<WeaponDef>("NE_AssaultRifle_WeaponDef");
            internal static WeaponDef neMachineGun = DefCache.GetDef<WeaponDef>("NE_MachineGun_WeaponDef");
            internal static WeaponDef grenade = DefCache.GetDef<WeaponDef>("PX_HandGrenade_WeaponDef");
            //acceptable faction wear
            internal static TacticalItemDef njAssaultHelmet = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Helmet_BodyPartDef");

            /* 
             ("NJ_Heavy_Helmet_BodyPartDef");
             ("PX_Assault_Helmet_BodyPartDef");
             ("PX_Heavy_Helmet_BodyPartDef");
             ("AN_Assault_Torso_BodyPartDef");
             ("");*/
            internal static ClassTagDef assaultTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
            internal static ClassTagDef heavyTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef");
            internal static ClassTagDef infiltratorTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef");
            internal static ClassTagDef sniperTag = DefCache.GetDef<ClassTagDef>("Sniper_ClassTagDef");
            internal static ClassTagDef priestTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef");
            internal static ClassTagDef technicianTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef");
            internal static ClassTagDef berserkerTag = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef");

            internal static readonly WeaponDef _obliterator = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
            internal static readonly WeaponDef _subjector = DefCache.GetDef<WeaponDef>("KS_Subjector_WeaponDef");
            internal static readonly WeaponDef _redemptor = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
            internal static readonly WeaponDef _devastator = DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef");
            internal static readonly WeaponDef _tormentor = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");

            private static TacticalItemDef _heavyTorsoNakedLeft;
            private static TacticalItemDef _heavyTorsoNakedRight;
            private static TacticalItemDef _heavyTorsoNakedBoth;

            private static TacticalItemDef _heavyLegsNakedRight;

            private static TacticalItemDef _assaultGoldNakedLeft;
            private static TacticalItemDef _assaultGoldNakedRight;
            private static TacticalItemDef _assaultGoldNakedBoth;

            private static TacticalItemDef _assaultInNakedLeft;
            private static TacticalItemDef _assaultInNakedRight;
            private static TacticalItemDef _assaultInNakedBoth;

            private static TacticalItemDef _heavyGoldNakedLeft;
            private static TacticalItemDef _heavyGoldNakedRight;
            private static TacticalItemDef _heavyGoldNakedBoth;

            private static TacticalItemDef _risingSunJacketBoth;
         

            public static void CreateRaiderDefs()
            {
                try
                {
                    CreateWeakArmors();
                    AdjustGeoFaction();
                    CreateTemplates();

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustGeoFaction()
            {
                try
                {
                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");

                    neutralFaction.Units = neutralFaction.Units.AddRangeToArray(new GeoFactionDef.CharacterSpawnData[]
                    {
                        new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = berserkerTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 3.0f
                        },
                         new GeoFactionDef.CharacterSpawnData()
                        {
                        IsEliteUnit = false,
                        ClassTag = infiltratorTag,
                        RecruitRandomWeight = 3.0f,
                        TacticalRandomWeight = 1.0f
                        },

                    });
                    neutralFaction.StartingUnits = new TacCharacterDef[] { };
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateTemplates()
            {
                try
                {
                    List<ItemDef> bersekerWeapons0 = new List<ItemDef>() { sectarianAxe, nePistol, nePistol.CompatibleAmmunition[0] };
                    List<TacticalItemDef> berserkerArmor0 = new List<TacticalItemDef>() { sectarianHelmet, neuHeavyTshirt, inAssaultLegs };

                    CreateTacCharaterDef(berserkerTag, "RoadKill", "{C5A4C006-3FF9-47A3-B64A-B32A8781F0A7}", bersekerWeapons0, berserkerArmor0, null, null, 1, 0);

                    List<ItemDef> bersekerWeapons1 = new List<ItemDef>() { sectarianAxe, anuPistol, anuPistol.CompatibleAmmunition[0] };
                    List<TacticalItemDef> berserkerArmor1 = new List<TacticalItemDef>() { inAssaultHelmet, neuAssaultJacket, doomLegs };

                    CreateTacCharaterDef(berserkerTag, "RoadWarrior", "{5C08A5A2-4AA7-4334-A216-1C1A6F85541E}", bersekerWeapons1, berserkerArmor1, null, null, 1, 1);

                    List<ItemDef> bersekerWeapons2 = new List<ItemDef>() { sectarianAxe, anuPistol, anuPistol.CompatibleAmmunition[0] };
                    List<TacticalItemDef> berserkerArmor2 = new List<TacticalItemDef>() { doomHelmet, neuSniperCoat, inAssaultLegs };

                    CreateTacCharaterDef(berserkerTag, "RoadBeast", "{2A77F7CB-9F49-44D9-B319-5A04C5FE2E24}", bersekerWeapons2, berserkerArmor2, null, null, 2, 2);

                    List<ItemDef> bersekerWeapons3 = new List<ItemDef>() { DefCache.GetDef<WeaponDef>("PX_StunRod_WeaponDef"), _tormentor, _tormentor.CompatibleAmmunition[0] };
                    List<TacticalItemDef> berserkerArmor3 = new List<TacticalItemDef>() { neuSniperBandana, _heavyGoldNakedBoth, _heavyLegsNakedRight};

                    CreateTacCharaterDef(berserkerTag, "RoadBoss", "{DC9A0366-6238-4EFA-B61C-BB41A62E418C}", bersekerWeapons3, berserkerArmor3, null, null, 3, 3);

                    List<ItemDef> assaultWeapons0 = new List<ItemDef>() { neAssaultRifle, neAssaultRifle.CompatibleAmmunition[0] };
                    List<TacticalItemDef> assaultArmor0 = new List<TacticalItemDef>() { neuAssaultJacket, neuAssaultLegs };

                    CreateTacCharaterDef(assaultTag, "Fodder", "{542B4D12-EB73-4A85-9B45-66A69357E367}", assaultWeapons0, assaultArmor0, null, null, 1, 0);

                    List<ItemDef> assaultWeapons1 = new List<ItemDef>() { neAssaultRifle, neAssaultRifle.CompatibleAmmunition[0] };
                    List<TacticalItemDef> assaultArmor1 = new List<TacticalItemDef>() { inAssaultHelmet, _risingSunJacketBoth, neuAssaultLegs };

                    CreateTacCharaterDef(assaultTag, "Doer", "{6166CDEF-63B5-4B24-B210-EDB585F09739}", assaultWeapons1, assaultArmor1, null, null, 1, 1);

                    List<ItemDef> assaultWeapons2 = new List<ItemDef>() { neAssaultRifle, neAssaultRifle.CompatibleAmmunition[0], grenade };
                    List<TacticalItemDef> assaultArmor2 = new List<TacticalItemDef>() { neuSniperBandana, _assaultGoldNakedBoth, spyMasterLegs };

                    CreateTacCharaterDef(assaultTag, "Freak", "{971F1025-505A-4DAD-AB48-80CA8A46A593}", assaultWeapons2, assaultArmor2, null, null, 2, 2);

                    List<ItemDef> assaultWeapons3 = new List<ItemDef>() { _obliterator, _obliterator.CompatibleAmmunition[0], grenade };
                    List<TacticalItemDef> assaultArmor3 = new List<TacticalItemDef>() { njAssaultHelmet, doomTorso, neuSniperLegs };

                    CreateTacCharaterDef(assaultTag, "CrazedOut", "{ED159572-24E1-45F9-85E5-33300BE706C8}", assaultWeapons3, assaultArmor3, null, null, 3, 3);

                    //Berserker
                    //archtype road kill: sectarian helmet weak torso and pants, + axe
                    //archtype road kill 2: olddog helmet, weak torso and pants, + axe
                    //archype road warrior: heavy helmet, coat, weak pants + axe
                    //archype knight: sectarian with NJ assault helmet and kg handgun

                    //Assault
                    //archtype cannon fodder: no helmet, weak torso and pants, + AR
                    //archtype cannon fodder 2: bandana, spymaster jacket and pants, + AR
                    //archtype basic; weak helmet, med torso and pants, + AR
                    //archtype sarge: nj assault helmet, olddog torso, 

                    //Sniper
                    //archtype hunter: no helmet, weak torso and pants, + indie SR
                    //archtype sniper: bandana, med torso and pants, + indie SR
                    //archtype killer (lvl6): indie helmet, med torso and pants, + kg SR

                    //Heavy
                    //archtype loco gunner: weak helmet, jacket, pants + grom
                    //archtype loco gunner 2: golden helmet, t-shirt, nj assault pants, + HMG
                    //archtype olddog
                    //archtype king: bandana, nj heavy armor + legs, and ft

                    //Infiltrator
                    //Spymaster

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static TacCharacterDef CreateTacCharaterDef(ClassTagDef classTagDef, string name, string gUID,
          List<ItemDef> equipmentSlots, List<TacticalItemDef> armorSlots, List<TacticalItemDef> inventorySlots, List<GameTagDef> tags, int level, int willStat)
            {
                try
                {
                    //  GeoUnitDescriptor

                    TacCharacterDef characterSource = DefCache.GetDef<TacCharacterDef>($"BAN_Assault{level}_CharacterTemplateDef");
                    TacCharacterDef newCharacter = Helper.CreateDefFromClone(characterSource, gUID, name);

                    newCharacter.SpawnCommandId = name;
                    newCharacter.Data.Name = name;
                    newCharacter.Data.GameTags = tags != null ? new List<GameTagDef>(tags) { classTagDef }.ToArray() : new List<GameTagDef>() { classTagDef }.ToArray();
                    newCharacter.Data.EquipmentItems = equipmentSlots?.ToArray() ?? new ItemDef[] { };
                    newCharacter.Data.BodypartItems = armorSlots?.ToArray() ?? new ItemDef[] { };
                    newCharacter.Data.InventoryItems = inventorySlots?.ToArray() ?? new ItemDef[] { };

                    // newCharacter.Data.LevelProgression.SetLevel(level);

                    if (willStat != 0)
                    {
                        newCharacter.Data.Will = willStat;
                    }

                    GeoFactionDef neutralFaction = DefCache.GetDef<GeoFactionDef>("Neutral_GeoFactionDef");
                    neutralFaction.StartingUnits = neutralFaction.StartingUnits.AddToArray(newCharacter);

                    TFTVLogger.Always($"{newCharacter.Data.Name} is now in the neutral faction? {neutralFaction.StartingUnits.Contains(newCharacter)}");

                    return newCharacter;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static TacticalItemDef CreateArmor(TacticalItemDef baseArmorPiece, string gUID, TacticalItemDef pieceToAdd, int position)
            {
                try 
                {
                    TacticalItemDef newPiece = Helper.CreateDefFromClone(baseArmorPiece, gUID, baseArmorPiece.name + pieceToAdd.name);
                    newPiece.SubAddons[position] = new PhoenixPoint.Common.Entities.Addons.AddonDef.SubaddonBind() { SubAddon  = pieceToAdd };
                    return newPiece;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            private static void CreateWeakArmors()
            {
                try
                {
                    List<BodyPartAspectDef> bodyPartAspectDefs = new List<BodyPartAspectDef>()
                    {
                    neuHeavyBandana.BodyPartAspectDef, neuHeavyTshirt.BodyPartAspectDef, neuHeavyLegs.BodyPartAspectDef,
                    neuSniperBandana.BodyPartAspectDef, neuSniperCoat.BodyPartAspectDef, neuSniperLegs.BodyPartAspectDef,
                    neuAssaultJacket.BodyPartAspectDef, neuAssaultLegs.BodyPartAspectDef
                    };

                    TacticalItemDef leftArmDirtyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftArm_BodyPartDef");
                    TacticalItemDef rightArmDirtyTshirt = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_RightArm_BodyPartDef");

                    TacticalItemDef leftArmJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_LeftArm_BodyPartDef");
                    TacticalItemDef rightArmJacket = DefCache.GetDef<TacticalItemDef>("NEU_Assault_RightArm_BodyPartDef");

                    TacticalItemDef leftArmIndAssault = DefCache.GetDef<TacticalItemDef>("IN_Assault_LeftArm_BodyPartDef");
                    TacticalItemDef rightArmIndAssault = DefCache.GetDef<TacticalItemDef>("IN_Assault_RightArm_BodyPartDef");

                    TacticalItemDef leftArmCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_LeftArm_BodyPartDef");
                    TacticalItemDef rightArmCoat = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_RightArm_BodyPartDef");

                    TacticalItemDef leftLegHeavyJeans = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftLeg_BodyPartDef");
                    TacticalItemDef rightLegHeavyJeans = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_RightLeg_BodyPartDef");

                    TacticalItemDef leftLegAssaultJeans = DefCache.GetDef<TacticalItemDef>("NEU_Assault_LeftLeg_BodyPartDef");
                    TacticalItemDef righLegAssaultJeans = DefCache.GetDef<TacticalItemDef>("NEU_Assault_RightLeg_BodyPartDef");

                    TacticalItemDef leftLegSniperJeans = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_LeftLeg_BodyPartDef");
                    TacticalItemDef rightLegSniperJeans = DefCache.GetDef<TacticalItemDef>("NEU_Sniper_RightLeg_BodyPartDef");

                    _heavyTorsoNakedLeft = CreateArmor(inHeavyTorso, "{F380E575-F7B5-468C-9B14-99DDFB564977}", leftArmDirtyTshirt, 0);
                    _heavyTorsoNakedRight = CreateArmor(inHeavyTorso, "{AFE24991-90C7-40C8-8F74-1846CBE257A1}", rightArmDirtyTshirt, 1);
                    _heavyTorsoNakedBoth = CreateArmor(_heavyTorsoNakedLeft, "{2C094B9A-2A34-402B-ABD7-06B98BB0454E}", rightArmDirtyTshirt, 1);

                    _heavyLegsNakedRight = CreateArmor(inHeavyLegs, "{A1288CBE-DFEC-451C-B4ED-161BFD8B85D1}", rightLegHeavyJeans, 1);

                    _assaultGoldNakedLeft = CreateArmor(goldAssaultTorso, "{93E304E0-40F0-4B85-A74B-93A6834ADBD2}", leftArmDirtyTshirt, 0);
                    _assaultGoldNakedRight = CreateArmor(goldAssaultTorso, "{160ED9D6-0DF4-465C-9871-6B3192301BB9}", rightArmDirtyTshirt, 1);
                    _assaultGoldNakedBoth = CreateArmor(_assaultGoldNakedLeft, "{3986B37E-104D-4B9C-9F87-7A911872457B}", rightArmDirtyTshirt, 1);

                    _heavyGoldNakedLeft = CreateArmor(goldHeavyTorso, "{04EC2DC1-D935-4632-AE2C-65C83628CD34}", leftArmDirtyTshirt, 0);
                    _heavyGoldNakedRight = CreateArmor(goldHeavyTorso, "{2510A7E3-9A9B-4374-8A6E-4E5911D0753A}", rightArmDirtyTshirt, 1);
                    _heavyGoldNakedBoth = CreateArmor(_heavyGoldNakedLeft, "{9A9EEE73-2E0D-4D9D-A565-194C983007EA}", rightArmDirtyTshirt, 1);

                    _risingSunJacketBoth = CreateArmor(CreateArmor(risingSunTorso, "{D434AF4D-98C4-4197-BCF8-54B213812E91}", leftArmIndAssault, 0), "{D9C7E352-83E0-4455-A2B1-FBFEB9AE2B60}", rightArmIndAssault, 1);



                    _assaultInNakedLeft = CreateArmor(inAssaultTorso, "{C7450FCD-C593-4F4C-B5B1-F0A42696E76C}", leftArmDirtyTshirt, 0);

         //   private static TacticalItemDef _assaultInNakedRight;
         //   private static TacticalItemDef _assaultInNakedBoth;


            //  DefCache.GetDef<TacticalItemDef>("IN_Heavy_LeftArm_BodyPartDef").SkinData = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftArm_BodyPartDef").SkinData;
            //  DefCache.GetDef<TacticalItemDef>("IN_Heavy_LeftArm_BodyPartDef").SubAddons = DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftArm_BodyPartDef").SubAddons;

            DefCache.GetDef<TacticalItemDef>("IN_Sniper_LeftArm_BodyPartDef");
                    DefCache.GetDef<TacticalItemDef>("NEU_Heavy_LeftHand_BodyPartDef");


                    foreach (BodyPartAspectDef bodyPartAspectDef in bodyPartAspectDefs)
                    {
                        bodyPartAspectDef.Speed = 0;
                        bodyPartAspectDef.Accuracy = 0;
                        bodyPartAspectDef.Endurance = 0;
                        bodyPartAspectDef.Perception = 0;
                        bodyPartAspectDef.Stealth = 0;
                        bodyPartAspectDef.WillPower = 0;
                    }

                    neuHeavyBandana.Armor = 0;
                    neuSniperBandana.Armor = 0;
                  
                    neuHeavyTshirt.Armor = 0;
                    leftArmDirtyTshirt.Armor = 0;
                    rightArmDirtyTshirt.Armor = 0;

                    neuAssaultJacket.Armor = 10;
                    leftArmJacket.Armor = 10;
                    rightArmJacket.Armor = 10;
                    
                    neuSniperCoat.Armor = 12;
                    leftArmCoat.Armor = 12;
                    rightArmCoat.Armor = 12;

                    neuHeavyLegs.Armor = 8;
                    leftLegHeavyJeans.Armor = 8;
                    rightLegHeavyJeans.Armor = 8;

                    neuAssaultLegs.Armor = 8;
                    leftLegAssaultJeans.Armor = 8;
                    righLegAssaultJeans.Armor = 8;

                    neuSniperLegs.Armor = 8;
                    leftLegSniperJeans.Armor = 8;
                    rightLegSniperJeans.Armor = 8;




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
