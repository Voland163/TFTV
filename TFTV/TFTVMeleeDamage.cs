using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVMeleeDamage
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        public static StandardDamageTypeEffectDef MeleeStandardDamageType;
        public static DamageKeywordDef MeleeDamageKeywordDef;
        public static StandardDamageTypeEffectDef PsychicStandardDamageType;
        public static DamageKeywordDef PsychicStandardDamageKeywordDef;

        public static void AddMeleeDamageType()
        {
            CreateMeleeDamageType();
            CreatePsychicStandardDamageType();
            ReplaceCorruptionDamageTypes();
        }


        private static void ReplaceCorruptionDamageTypes()
        {
            try
            {
                CorruptionStatusDef corruptionStatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");
                corruptionStatusDef.DamageTypeDefs = new List<DamageTypeBaseEffectDef> { MeleeStandardDamageType, PsychicStandardDamageType };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreatePsychicStandardDamageType()
        {
            try
            {

                StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef newStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{14084440-1682-4620-BF4D-62774EBD644A}", "TFTV_PsychicStandard_damageType");

                DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{8AE7DE3C-72EA-4562-A221-2286AEB2ED20}", "TFTV_PsychicStandard_damageKeyword");

                newDamageKeywordDef.DamageTypeDef = newStandardDamageTypeEffectDef;

                PsychicStandardDamageKeywordDef = newDamageKeywordDef;
                PsychicStandardDamageType = newStandardDamageTypeEffectDef;

                DamageEffectDef mindCrushDamageEffectDef = DefCache.GetDef<DamageEffectDef>("E_Effect [MindCrush_AbilityDef]");
                mindCrushDamageEffectDef.DamageTypeDef = PsychicStandardDamageType;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateMeleeDamageType()
        {
            try
            {

                StandardDamageTypeEffectDef sourceStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef newMeleeStandardDamageTypeEffectDef = Helper.CreateDefFromClone(sourceStandardDamageTypeEffectDef, "{4BAEF84E-5985-47D2-897F-99C863C7E71D}", "TFTV_Melee_damageType");

                DamageKeywordDef sourceDamageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");
                DamageKeywordDef newDamageKeywordDef = Helper.CreateDefFromClone(sourceDamageKeywordDef, "{701EBF00-6BDB-48E2-9635-C854ABC63AFA}", "TFTV_Melee_damageKeyword");

                newDamageKeywordDef.DamageTypeDef = newMeleeStandardDamageTypeEffectDef;

                //"Mutog_HeadRamming_BodyPartDef"
                WeaponDef RammingHead = (WeaponDef)Repo.GetDef("c29d4fc0-cb86-0e54-383c-513f8926e6c1");
                RammingHead.Tags.Add(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef"));


                foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>())
                {
                    if (weaponDef.Tags.Contains(DefCache.GetDef<GameTagDef>("MeleeWeapon_TagDef")) && weaponDef.DamagePayload.DamageKeywords.Any(p => p.DamageKeywordDef == sourceDamageKeywordDef))
                    {
                        // TFTVLogger.Always($"{weaponDef.name} has melee weapon tag, replacing damage type");
                        foreach (DamageKeywordPair damageKeywordPair in weaponDef.DamagePayload.DamageKeywords)
                        {
                            if (damageKeywordPair.DamageKeywordDef == sourceDamageKeywordDef)
                            {
                                damageKeywordPair.DamageKeywordDef = newDamageKeywordDef;
                                // TFTVLogger.Always($"replaced");
                            }
                        }
                    }
                }



                MeleeStandardDamageType = newMeleeStandardDamageTypeEffectDef;
                MeleeDamageKeywordDef = newDamageKeywordDef;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

    }
}
