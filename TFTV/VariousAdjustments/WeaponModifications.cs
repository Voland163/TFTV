using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;

namespace PRMBetterClasses.VariousAdjustments
{
    internal class WeaponModifications
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ApplyChanges()
        {
            Change_Hera();
            Change_KaosWeapons();
            Change_Ragnarok();
            Change_Iconoclast();
            Change_NergalsWrath();
            Change_Crossbows();
            Change_PriestWeapons();
        }

        private static void Change_Hera()
        {
            SharedDamageKeywordsDataDef damageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
            WeaponDef Hera = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("SY_NeuralPistol_WeaponDef"));
            Hera.ChargesMax = 5;
            Hera.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.PiercingKeyword).Value = 20;
            ItemDef Hera_Ammo = Repo.GetAllDefs<ItemDef>().FirstOrDefault(ec => ec.name.Equals("SY_NeuralPistol_AmmoClip_ItemDef"));
            Hera_Ammo.ChargesMax = 5;
        }

        private static void Change_KaosWeapons()
        {
            SharedDamageKeywordsDataDef damageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
            foreach (WeaponDef weapon in Repo.GetAllDefs<WeaponDef>())
            {
                switch (weapon.name)
                {
                    // Redeptor: ER 13, damage 40
                    case "KS_Redemptor_WeaponDef":
                        weapon.SpreadDegrees = 40.99f / 13;
                        weapon.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 40;
                        break;
                    // Obliterator: Damage 30
                    case "KS_Obliterator_WeaponDef":
                        weapon.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 30;
                        break;
                    // Tormentor: Damage 40
                    case "KS_Tormentor_WeaponDef":
                        weapon.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 40;
                        break;
                    default:
                        break;
                }
            }
        }

        private static void Change_Ragnarok()
        {
            WeaponDef Ragnarok = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("PX_ShredingMissileLauncherPack_WeaponDef"));
            Ragnarok.DamagePayload.Range = 35.0f;
            Ragnarok.DamagePayload.AoeRadius = 5.5f;
            SharedData Shared = GameUtl.GameComponent<SharedData>();

            // Easter egg for all testes :-)
            Ragnarok.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword,
                    Value = 40
                },
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword,
                    Value = 20
                }
            };
            Ragnarok.DamagePayload.ProjectilesPerShot = 4;
            Ragnarok.SpreadRadius = 5.5f;
            Ragnarok.ChargesMax = 8;

            ItemDef RagnarokAmmo = Repo.GetAllDefs<ItemDef>().FirstOrDefault(i => i.name.Equals("PX_ShredingMissileLauncher_AmmoClip_ItemDef"));
            RagnarokAmmo.ChargesMax = 8;
            RagnarokAmmo.ManufactureMaterials = 24;
            RagnarokAmmo.ManufactureTech = 62;
        }

        private static void Change_Iconoclast()
        {
            WeaponDef Iconoclast = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("AN_Shotgun_WeaponDef"));
            SharedData Shared = GameUtl.GameComponent<SharedData>();
            Iconoclast.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword,
                    Value = 30
                }
            };
            Iconoclast.SpreadDegrees = 40.99f / 13;
        }

        private static void Change_NergalsWrath()
        {
            WeaponDef NergalsWrath = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("AN_HandCannon_WeaponDef"));
            NergalsWrath.APToUsePerc = 25;
            SharedData Shared = GameUtl.GameComponent<SharedData>();
            NergalsWrath.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
            {
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword,
                    Value = 50
                },
                new DamageKeywordPair()
                {
                    DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword,
                    Value = 5
                }
            };
        }

        public static void Change_Crossbows()
        {
            WeaponDef ErosCrb = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("SY_Crossbow_WeaponDef"));
            WeaponDef BonusErosCrb = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("SY_Crossbow_Bonus_WeaponDef"));
            ItemDef ErosCrb_Ammo = Repo.GetAllDefs<ItemDef>().FirstOrDefault(ec => ec.name.Equals("SY_Crossbow_AmmoClip_ItemDef"));
            WeaponDef PsycheCrb = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(ec => ec.name.Equals("SY_Venombolt_WeaponDef"));
            ItemDef PsycheCrb_Ammo = Repo.GetAllDefs<ItemDef>().FirstOrDefault(ec => ec.name.Equals("SY_Venombolt_AmmoClip_ItemDef"));
            ErosCrb.ChargesMax = TFTVMain.Main.Settings.BaseCrossbow_Ammo;
            BonusErosCrb.ChargesMax = TFTVMain.Main.Settings.BaseCrossbow_Ammo;
            ErosCrb_Ammo.ChargesMax = TFTVMain.Main.Settings.BaseCrossbow_Ammo;
            PsycheCrb.ChargesMax = TFTVMain.Main.Settings.VenomCrossbow_Ammo;
            PsycheCrb_Ammo.ChargesMax = TFTVMain.Main.Settings.VenomCrossbow_Ammo;
        }

        public static void Change_PriestWeapons()
        {
            int redeemerViral = 4;
            int subjectorViral = 8;

            WeaponDef redeemer = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(a => a.name.Equals("AN_Redemptor_WeaponDef"));
            WeaponDef subjector = Repo.GetAllDefs<WeaponDef>().FirstOrDefault(a => a.name.Equals("AN_Subjector_WeaponDef"));

            redeemer.DamagePayload.DamageKeywords[2].Value = redeemerViral;
            subjector.DamagePayload.DamageKeywords[2].Value = subjectorViral;
        }
    }
}
