using Base.Core;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Weapons;
using System.Collections.Generic;
using TFTV;

namespace PRMBetterClasses.VariousAdjustments
{
    internal class WeaponModifications
    {
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

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
            WeaponDef Hera = DefCache.GetDef<WeaponDef>("SY_NeuralPistol_WeaponDef");
            Hera.ChargesMax = 5;
            Hera.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.PiercingKeyword).Value = 20;
            ItemDef Hera_Ammo = DefCache.GetDef<ItemDef>("SY_NeuralPistol_AmmoClip_ItemDef");
            Hera_Ammo.ChargesMax = 5;
        }

        private static void Change_KaosWeapons()
        {
            SharedDamageKeywordsDataDef damageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;
            // Redeptor: ER 13, damage 40
            WeaponDef weaponDef = DefCache.GetDef<WeaponDef>("KS_Redemptor_WeaponDef");
            weaponDef.SpreadDegrees = 40.99f / 13;
            weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 40;
            // Obliterator: Damage 30
            weaponDef = DefCache.GetDef<WeaponDef>("KS_Obliterator_WeaponDef");
            weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 30;
            // Tormentor: Damage 40
            weaponDef = DefCache.GetDef<WeaponDef>("KS_Tormentor_WeaponDef");
            weaponDef.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == damageKeywords.DamageKeyword).Value = 40;
        }

        private static void Change_Ragnarok()
        {
            WeaponDef Ragnarok = DefCache.GetDef<WeaponDef>("PX_ShredingMissileLauncherPack_WeaponDef");
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

            ItemDef RagnarokAmmo = DefCache.GetDef<ItemDef>("PX_ShredingMissileLauncher_AmmoClip_ItemDef");
            RagnarokAmmo.ChargesMax = 8;
            RagnarokAmmo.ManufactureMaterials = 24;
            RagnarokAmmo.ManufactureTech = 62;
        }

        private static void Change_Iconoclast()
        {
            WeaponDef Iconoclast = DefCache.GetDef<WeaponDef>("AN_Shotgun_WeaponDef");
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
            WeaponDef NergalsWrath = DefCache.GetDef<WeaponDef>("AN_HandCannon_WeaponDef");
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
            WeaponDef ErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_WeaponDef");
            WeaponDef BonusErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
            ItemDef ErosCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Crossbow_AmmoClip_ItemDef");
            WeaponDef PsycheCrb = DefCache.GetDef<WeaponDef>("SY_Venombolt_WeaponDef");
            ItemDef PsycheCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Venombolt_AmmoClip_ItemDef");
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

            WeaponDef redeemer = DefCache.GetDef<WeaponDef>("AN_Redemptor_WeaponDef");
            WeaponDef subjector = DefCache.GetDef<WeaponDef>("AN_Subjector_WeaponDef");

            redeemer.DamagePayload.DamageKeywords[2].Value = redeemerViral;
            subjector.DamagePayload.DamageKeywords[2].Value = subjectorViral;
        }
    }
}
