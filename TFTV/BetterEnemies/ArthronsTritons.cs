using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class ArthronsTritons
    {
          private static readonly DefRepository Repo = TFTVMain.Repo;
      //  private static readonly SharedData Shared = BetterEnemiesMain.Shared;

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void Change_ArthronsTritons()
        {
            try
            {
                TacticalItemDef crabmanHeavyHead = DefCache.GetDef<TacticalItemDef>("Crabman_Head_EliteHumanoid_BodyPartDef");
                TacCharacterDef crab9 = DefCache.GetDef<TacCharacterDef>("Crabman9_Shielder_AlienMutationVariationDef");
                TacCharacterDef crab10 = DefCache.GetDef<TacCharacterDef>("Crabman10_AdvancedShielder_AlienMutationVariationDef");
                TacCharacterDef crab11 = DefCache.GetDef<TacCharacterDef>("Crabman11_AdvancedShielder2_AlienMutationVariationDef");
                TacCharacterDef crab12 = DefCache.GetDef<TacCharacterDef>("Crabman12_EliteShielder_AlienMutationVariationDef");
                TacCharacterDef crab13 = DefCache.GetDef<TacCharacterDef>("Crabman13_EliteShielder2_AlienMutationVariationDef");
                TacCharacterDef crab14 = DefCache.GetDef<TacCharacterDef>("Crabman14_EliteShielder3_AlienMutationVariationDef");
                TacCharacterDef crab15 = DefCache.GetDef<TacCharacterDef>("Crabman15_UltraShielder_AlienMutationVariationDef");
                TacCharacterDef crab24 = DefCache.GetDef<TacCharacterDef>("Crabman24_Pretorian_AlienMutationVariationDef");
                TacCharacterDef crab25 = DefCache.GetDef<TacCharacterDef>("Crabman25_AdvancedPretorian_AlienMutationVariationDef");
                TacCharacterDef crab26 = DefCache.GetDef<TacCharacterDef>("Crabman26_AdvancedPretorian2_AlienMutationVariationDef");
                TacCharacterDef crab30 = DefCache.GetDef<TacCharacterDef>("Crabman30_UltraPretorian_AlienMutationVariationDef");
                TacCharacterDef crab38 = DefCache.GetDef<TacCharacterDef>("Crabman38_UltraAcidRanger_AlienMutationVariationDef");
                TacCharacterDef crab34 = DefCache.GetDef<TacCharacterDef>("Crabman34_UltraRanger_AlienMutationVariationDef");

                WeaponDef arthronGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Grenade_WeaponDef");
                WeaponDef arthronEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_EliteGrenade_WeaponDef");
                WeaponDef arthronAcidGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_Grenade_WeaponDef");
                WeaponDef arthronAcidEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_EliteGrenade_WeaponDef");

                WeaponDef fishArmsParalyze = DefCache.GetDef<WeaponDef>("Fishman_UpperArms_Paralyzing_BodyPartDef");
                WeaponDef fishArmsEliteParalyze = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_Paralyzing_BodyPartDef");

                TacCharacterDef fish7 = DefCache.GetDef<TacCharacterDef>("Fishman7_EliteStriker_AlienMutationVariationDef");
                TacCharacterDef fish8 = DefCache.GetDef<TacCharacterDef>("Fishman8_PiercerAssault_AlienMutationVariationDef");
                TacCharacterDef fish11 = DefCache.GetDef<TacCharacterDef>("Fishman11_Sniper_AlienMutationVariationDef");
                TacCharacterDef fish12 = DefCache.GetDef<TacCharacterDef>("Fishman12_FocusSniper_AlienMutationVariationDef");
                TacCharacterDef fish13 = DefCache.GetDef<TacCharacterDef>("Fishman13_AgroSniper_AlienMutationVariationDef");
                TacCharacterDef fish14 = DefCache.GetDef<TacCharacterDef>("Fishman14_PiercerSniper_AlienMutationVariationDef");
                TacCharacterDef fish15 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");
                TacCharacterDef fish17 = DefCache.GetDef<TacCharacterDef>("Fishman15_ViralAssault_AlienMutationVariationDef");
                TacCharacterDef fishSniper5 = DefCache.GetDef<TacCharacterDef>("FishmanElite_Shrowder_Sniper");
                TacCharacterDef fishSniper6 = DefCache.GetDef<TacCharacterDef>("Fishman_Shrowder_TacCharacterDef");

                RepositionAbilityDef dash = DefCache.GetDef<RepositionAbilityDef>("Dash_AbilityDef");
                ApplyStatusAbilityDef MasterMarksman = DefCache.GetDef<ApplyStatusAbilityDef>("MasterMarksman_AbilityDef");
                ApplyStatusAbilityDef ExtremeFocus = DefCache.GetDef<ApplyStatusAbilityDef>("ExtremeFocus_AbilityDef");
                PassiveModifierAbilityDef EnhancedVision = DefCache.GetDef<PassiveModifierAbilityDef>("EnhancedVision_AbilityDef");

                fishArmsParalyze.DamagePayload.DamageKeywords[1].Value = 8;
                fishArmsEliteParalyze.DamagePayload.DamageKeywords[1].Value = 16;

            
                WeaponDef EliteBloodSuckers = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");


                fish15.Data.BodypartItems[3] = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");
                fish17.Data.BodypartItems[3] = DefCache.GetDef<WeaponDef>("FishmanElite_UpperArms_BloodSucker_BodyPartDef");

              

                crab15.Data.BodypartItems[0] = crab34.Data.BodypartItems[0];

                foreach (TacCharacterDef TriotonSniper in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Fishman") && a.name.Contains("Sniper")))
                {
                    TriotonSniper.Data.Abilites = new TacticalAbilityDef[]
                    {
                    DefCache.GetDef<TacticalAbilityDef>("ExtremeFocus_AbilityDef"),
                    };
                }

                foreach (TacCharacterDef crab in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                {
                    crab.Data.Abilites = new TacticalAbilityDef[]
                    {
                    DefCache.GetDef<TacticalAbilityDef>("CloseQuarters_AbilityDef"),
                    };
                }
              

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && (aad.name.Contains("Pretorian") || aad.name.Contains("Tank"))))
                {
                    character.Data.Speed = 6;
                }

                foreach (TacCharacterDef crabShield in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && aad.name.Contains("Shielder")))
                {
                    crabShield.Data.Speed = 8;
                }

                foreach (WeaponDef crabmanGl in Repo.GetAllDefs<WeaponDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("LeftHand") && a.name.Contains("Grenade") && a.name.Contains("WeaponDef")))
                {
                    crabmanGl.DamagePayload.Range = 15;
                }

                foreach (TacCharacterDef commando in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("Commando")))
                {
                    commando.Data.Abilites = new TacticalAbilityDef[]
                    {
                    DefCache.GetDef<TacticalAbilityDef>("BloodLust_AbilityDef"),
                    };
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void ArthronAcidBuff()
        {
            try
            {
                //BetterEnemiesConfig Config = (BetterEnemiesConfig)BetterEnemiesMain.Main.Config;
                WeaponDef arthronAcidGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_Grenade_WeaponDef");
                WeaponDef arthronAcidEliteGL = DefCache.GetDef<WeaponDef>("Crabman_LeftHand_Acid_EliteGrenade_WeaponDef");

             //   if (Config.TritonAcidBuff == true)
             //   {
                    arthronAcidGL.DamagePayload.DamageKeywords[1].Value = 20;
                    arthronAcidEliteGL.DamagePayload.DamageKeywords[1].Value = 30;
             //   }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
