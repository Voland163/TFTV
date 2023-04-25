using Base.Defs;
using Base.Entities.Abilities;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.BetterEnemies
{
    internal class Scylla
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static void Change_Queen()
        {
            try
            {
                TacticalItemDef queenSpawner = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Spawner_BodyPartDef");
                TacticalItemDef queenBelcher = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
                TacCharacterDef queenCrystal = DefCache.GetDef<TacCharacterDef>("Queen_Crystal_TacCharacterDef");

                BodyPartAspectDef queenHeavyHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Heavy_BodyPartDef]");
                BodyPartAspectDef queenSpitterHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Spitter_Goo_WeaponDef]");
                BodyPartAspectDef queenSonicHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Sonic_WeaponDef]");

                WeaponDef queenLeftBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_LeftArmGun_WeaponDef");
                WeaponDef queenRightBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_RightArmGun_WeaponDef");
                WeaponDef queenBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_Arms_Gun_WeaponDef");
                WeaponDef queenSmasher = DefCache.GetDef<WeaponDef>("Queen_Arms_Smashers_WeaponDef");

                AdditionalEffectShootAbilityDef queenBlast = DefCache.GetDef<AdditionalEffectShootAbilityDef>("Queen_GunsFire_ShootAbilityDef");
                ShootAbilityDef guardianBeam = DefCache.GetDef<ShootAbilityDef>("BE_Guardian_Beam_ShootAbilityDef");
                MindControlAbilityDef MindControl = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");
                //   BetterEnemiesConfig Config = (BetterEnemiesConfig)BetterEnemiesMain.Main.Config;

                //   if (Config.ScyllaBuff == true)
                //  {
                queenLeftBlastWeapon.Abilities = new AbilityDef[]
            {
                guardianBeam,
            };

                queenRightBlastWeapon.Abilities = new AbilityDef[]
                {
                guardianBeam,
                };

                queenBlastWeapon.Abilities = new AbilityDef[]
                {
                guardianBeam,
                };

                queenSpawner.Abilities = new AbilityDef[]
                {
                queenSpawner.Abilities[0],
                DefCache.GetDef<AbilityDef>("AcidResistant_DamageMultiplierAbilityDef"),
                };

                queenBelcher.Abilities = new AbilityDef[]
                {
                queenBelcher.Abilities[0],
                DefCache.GetDef<AbilityDef>("AcidResistant_DamageMultiplierAbilityDef"),
                };

                queenBlastWeapon.Tags = new GameTagsList
            {
                queenBlastWeapon.Tags[0],
                queenBlastWeapon.Tags[1],
                queenBlastWeapon.Tags[2],
                queenBlastWeapon.Tags[3],
                DefCache.GetDef<ItemClassificationTagDef>("ExplosiveWeapon_TagDef")
            };

                queenLeftBlastWeapon.Tags = new GameTagsList
            {
                queenLeftBlastWeapon.Tags[0],
                queenLeftBlastWeapon.Tags[1],
                queenLeftBlastWeapon.Tags[2],
                DefCache.GetDef<ItemClassificationTagDef>("ExplosiveWeapon_TagDef")
            };

                queenBlastWeapon.Tags = new GameTagsList
            {
                queenRightBlastWeapon.Tags[0],
                queenRightBlastWeapon.Tags[1],
                queenRightBlastWeapon.Tags[2],
                DefCache.GetDef<ItemClassificationTagDef>("ExplosiveWeapon_TagDef")
            };

                queenCrystal.Data.Abilites = new TacticalAbilityDef[]
                {
                DefCache.GetDef<TacticalAbilityDef>("CaterpillarMoveAbilityDef"),
                MindControl,
                };

                foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Queen_AnimActionsDef")))
                {
                    if (animActionDef.AbilityDefs != null && !animActionDef.AbilityDefs.Contains(MindControl))
                    {
                        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(MindControl).ToArray();
                    }
                }

                queenSmasher.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                queenSmasher.DamagePayload.DamageKeywords[0],
                queenSmasher.DamagePayload.DamageKeywords[1],
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.ParalysingKeyword,
                    Value = 8,
                },
            };

                queenBlastWeapon.DamagePayload.DamageKeywords[0].Value = 40;
                queenBlastWeapon.DamagePayload.DamageKeywords[1].Value = 3;
                queenLeftBlastWeapon.DamagePayload.DamageKeywords[0].Value = 40;
                queenLeftBlastWeapon.DamagePayload.DamageKeywords[1].Value = 3;
                queenRightBlastWeapon.DamagePayload.DamageKeywords[0].Value = 40;
                queenRightBlastWeapon.DamagePayload.DamageKeywords[1].Value = 3;

                queenSpawner.Armor = 60;
                queenBelcher.Armor = 60;
                queenHeavyHead.WillPower = 175;
                queenSpitterHead.WillPower = 165;
                queenSonicHead.WillPower = 170;
                //   }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
