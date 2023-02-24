using Base.Defs;
using Base.Entities.Abilities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;

namespace TFTV.BetterEnemies
{
    internal class SirenChiron
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        //  private static readonly SharedData Shared = TFTVMain.Shared;

        public static void Change_SirenChiron()
        {
            try
            {

                TacticalItemDef sirenLegsHeavy = DefCache.GetDef<TacticalItemDef>("Siren_Legs_Heavy_BodyPartDef");
                TacticalItemDef sirenLegsAgile = DefCache.GetDef<TacticalItemDef>("Siren_Legs_Agile_BodyPartDef");
                TacticalItemDef sirenLegsOrichalcum = DefCache.GetDef<TacticalItemDef>("Siren_Legs_Orichalcum_BodyPartDef");
                TacticalItemDef sirenScremingHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Screamer_BodyPartDef");
                PsychicScreamAbilityDef sirenPsychicScream = DefCache.GetDef<PsychicScreamAbilityDef>("Siren_PsychicScream_AbilityDef");
                MindControlAbilityDef sirenMC = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");
                TacCharacterDef sirenBanshee = DefCache.GetDef<TacCharacterDef>("Siren3_InjectorBuffer_AlienMutationVariationDef");
                TacCharacterDef sirenHarbinger = DefCache.GetDef<TacCharacterDef>("Siren4_SlasherBuffer_AlienMutationVariationDef");
                TacticalPerceptionDef sirenPerception = DefCache.GetDef<TacticalPerceptionDef>("Siren_PerceptionDef");
                TacCharacterDef sirenArmis = DefCache.GetDef<TacCharacterDef>("Siren5_Orichalcum_AlienMutationVariationDef");
                WeaponDef sirenInjectorArms = DefCache.GetDef<WeaponDef>("Siren_Arms_Injector_WeaponDef");
                TacticalItemDef sirenArmisHead = DefCache.GetDef<TacticalItemDef>("Siren_Head_Orichalcum_BodyPartDef");
                WeaponDef sirenAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_AcidSpitter_WeaponDef");
                WeaponDef sirenArmisAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_Orichalcum_WeaponDef");
                ShootAbilityDef AcidSpray = DefCache.GetDef<ShootAbilityDef>("Siren_SpitAcid_AbilityDef");

                WeaponDef chironBlastMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Mortar_WeaponDef");
                WeaponDef chironCristalMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Crystal_Mortar_WeaponDef");
                WeaponDef chironAcidMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Acid_Mortar_WeaponDef");
                WeaponDef chironFireWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_FireWorm_Launcher_WeaponDef");
                WeaponDef chironAcidWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_AcidWorm_Launcher_WeaponDef");
                WeaponDef chironPoisonWormMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef");
                TacCharacterDef chironFireHeavy = DefCache.GetDef<TacCharacterDef>("Chiron2_FireWormHeavy_AlienMutationVariationDef");
                TacCharacterDef chironPoisonHeavy = DefCache.GetDef<TacCharacterDef>("Chiron4_PoisonWormHeavy_AlienMutationVariationDef");
                TacCharacterDef chironAcidHeavy = DefCache.GetDef<TacCharacterDef>("Chiron6_AcidWormHeavy_AlienMutationVariationDef");
                TacCharacterDef chironGooHeavy = DefCache.GetDef<TacCharacterDef>("Chiron8_GooHeavy_AlienMutationVariationDef");

                sirenPerception.PerceptionRange = 38;
                sirenBanshee.Data.Will = 14;
                sirenBanshee.Data.BodypartItems[0] = sirenScremingHead;
                sirenBanshee.Data.Speed += 5;
                sirenInjectorArms.DamagePayload.DamageKeywords[2].Value = 10;
                sirenLegsAgile.Armor = 30;
                sirenPsychicScream.ActionPointCost = 0.25f;
                sirenPsychicScream.UsesPerTurn = 1;
                sirenAcidTorso.APToUsePerc = 25;
                sirenArmisAcidTorso.APToUsePerc = 25;
                AcidSpray.UsesPerTurn = 1;

                sirenBanshee.Data.Abilites = new TacticalAbilityDef[]
                {

                DefCache.GetDef<TacticalAbilityDef>("Thief_AbilityDef"),
                DefCache.GetDef<TacticalAbilityDef>("StealthSpecialist_AbilityDef")
                };

                sirenArmis.Data.Abilites = new TacticalAbilityDef[]
                {
                sirenArmis.Data.Abilites[0],
                DefCache.GetDef<TacticalAbilityDef>("IgnorePain_AbilityDef"),
                };

                sirenArmisHead.Abilities = new AbilityDef[]
                {
                sirenArmisHead.Abilities[0],
                };

                chironFireHeavy.Data.Speed = 8;
                chironPoisonHeavy.Data.Speed = 8;
                chironAcidHeavy.Data.Speed = 8;
                chironGooHeavy.Data.Speed = 8;

                chironAcidMortar.ChargesMax = 18;
                chironFireWormMortar.DamagePayload.ProjectilesPerShot = 3;    // 3
                chironFireWormMortar.ChargesMax = 18;    // 15            
                chironAcidWormMortar.DamagePayload.ProjectilesPerShot = 3;    // 3
                chironAcidWormMortar.ChargesMax = 18;    // 15            
                chironPoisonWormMortar.DamagePayload.ProjectilesPerShot = 3;    // 3
                chironPoisonWormMortar.ChargesMax = 18;    // 15            
                chironBlastMortar.DamagePayload.ProjectilesPerShot = 3;    // 3
                chironBlastMortar.ChargesMax = 18;   // 12           
                chironCristalMortar.DamagePayload.ProjectilesPerShot = 3;    // 3
                chironCristalMortar.ChargesMax = 30;    // 12

                foreach (WeaponDef ChironWormLauncher in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Chiron_Abdomen_") && a.name.Contains("Worm_Launcher_WeaponDef")))
                {
                    ChironWormLauncher.DamagePayload.DamageKeywords[1].Value = 240;
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChironAcidBuff()
        {
            try
            {
              //  BetterEnemiesConfig Config = (BetterEnemiesConfig)BetterEnemiesMain.Main.Config;
                WeaponDef chironAcidMortar = DefCache.GetDef<WeaponDef>("Chiron_Abdomen_Acid_Mortar_WeaponDef");

              //  if (Config.ChironAcidBuff == true)
              //  {
                    chironAcidMortar.DamagePayload.DamageKeywords[0].Value = 20;
              //  }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
