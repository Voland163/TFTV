using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVBetterEnemies
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //Adapted from BetterEnemies by Dtony
        internal static bool CheckIfBEActive()
        {
            try
            {
                AIActionMoveAndAttackDef existingMAShoot = (AIActionMoveAndAttackDef)Repo.GetDef("3fd2dfd1-3cc0-4c71-b427-22afd020b45d");     
                  
                if (existingMAShoot != null)
                {
                    return true;
                }
                else
                {
                    return false;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void RevertScyllaAIFromBE()
        {
            try
            {
                WeaponDef queenLeftBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_LeftArmGun_WeaponDef");
                WeaponDef queenRightBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_RightArmGun_WeaponDef");
                WeaponDef queenBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_Arms_Gun_WeaponDef");

                queenBlastWeapon.DamagePayload.DamageKeywords[0].Value = 80;
                queenBlastWeapon.DamagePayload.DamageKeywords[1].Value = 30;
                queenLeftBlastWeapon.DamagePayload.DamageKeywords[0].Value = 80;
                queenLeftBlastWeapon.DamagePayload.DamageKeywords[1].Value = 30;
                queenRightBlastWeapon.DamagePayload.DamageKeywords[0].Value = 80;
                queenRightBlastWeapon.DamagePayload.DamageKeywords[1].Value = 30;


                AdditionalEffectShootAbilityDef scyllaGunsShootAbilityDef = DefCache.GetDef<AdditionalEffectShootAbilityDef>("Queen_GunsFire_ShootAbilityDef");
                StartPreparingShootAbilityDef startPreparingShootAbilityDef = DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef");

                queenLeftBlastWeapon.Abilities = new AbilityDef[]
            {
                scyllaGunsShootAbilityDef, startPreparingShootAbilityDef
            };

                queenRightBlastWeapon.Abilities = new AbilityDef[]
                {
                scyllaGunsShootAbilityDef, startPreparingShootAbilityDef
                };

                queenBlastWeapon.Abilities = new AbilityDef[]
                {
                scyllaGunsShootAbilityDef, startPreparingShootAbilityDef
                };

             
                AIActionsTemplateDef QueenAI = DefCache.GetDef<AIActionsTemplateDef>("Queen_AIActionsTemplateDef");
                AIActionMoveAndExecuteAbilityDef moveAndPrepareShooting = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("Queen_MoveAndPrepareShooting_AIActionDef");
                List<AIActionDef> QueenAIActions = new List<AIActionDef>(QueenAI.ActionDefs)
                {
                    moveAndPrepareShooting
                };

                QueenAI.ActionDefs = QueenAIActions.ToArray();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        internal static void ImplementBetterEnemies()
        {
            try
            {
                TFTVConfig config = TFTVMain.Main.Config;

                if (!CheckIfBEActive())
                {
                    TFTVLogger.Always("BetterEnemies mod not found");

                    BECreateAIActionDefs();
                  //  TFTVLogger.Always("BE AIActionDefs created");
                    BEFixesToAI();
                 //   TFTVLogger.Always("BE Fixes to AI applied");
                    BEChange_Perception();
                    BEFixCaterpillarTracksDamage();
                    BEReducePandoranWillpower();
                    if (config.BetterEnemiesOn)
                    {
                        TFTVLogger.Always("More challenging Pandorans from BetterEnemies on!");
                        BEBuff_ArthronsTritons();
                        BEBuff_StartingEvolution();
                        BEBuff_Queen();
                        BEBUff_SirenChiron();
                        BEBuff_SmallCharactersAndSentinels();
                    }
                }
                else
                {
                    TFTVLogger.Always("BetterEnemies mod found, reverting changes to Scylla");
                    RevertScyllaAIFromBE();


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        public static void BEBuff_SmallCharactersAndSentinels()
        {
            try
            {

                DefRepository Repo = GameUtl.GameComponent<DefRepository>();
                SharedData Shared = GameUtl.GameComponent<SharedData>();

                TacCharacterDef fireworm = DefCache.GetDef<TacCharacterDef>("Fireworm_AlienMutationVariationDef");
                TacCharacterDef acidworm = DefCache.GetDef<TacCharacterDef>("Acidworm_AlienMutationVariationDef");
                TacCharacterDef poisonworm = DefCache.GetDef<TacCharacterDef>("Poisonworm_AlienMutationVariationDef");
                BodyPartAspectDef acidWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Acidworm_Torso_BodyPartDef]");
                BodyPartAspectDef fireWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Fireworm_Torso_BodyPartDef]");
                BodyPartAspectDef poisonWorm = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Poisonworm_Torso_BodyPartDef]");
                ApplyDamageEffectAbilityDef aWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("AcidwormExplode_AbilityDef");
                ApplyDamageEffectAbilityDef fWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("FirewormExplode_AbilityDef");
                ApplyDamageEffectAbilityDef pWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("PoisonwormExplode_AbilityDef");

                TacticalPerceptionDef tacticalPerceptionEgg = DefCache.GetDef<TacticalPerceptionDef>("Fireworm_Egg_PerceptionDef");
                TacticalPerceptionDef tacticalPerceptionHatchling = DefCache.GetDef<TacticalPerceptionDef>("SentinelHatching_PerceptionDef");
                TacticalPerceptionDef tacticalPerceptionTerror = DefCache.GetDef<TacticalPerceptionDef>("SentinelTerror_PerceptionDef");
                TacticalPerceptionDef tacticalPerceptionMindFraggerEgg = DefCache.GetDef<TacticalPerceptionDef>("EggFacehugger_PerceptionDef");

                TacCharacterDef faceHuggerTac = DefCache.GetDef<TacCharacterDef>("Facehugger_TacCharacterDef");
                TacCharacterDef faceHuggerVariation = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                TacticalActorDef faceHugger = DefCache.GetDef<TacticalActorDef>("Facehugger_ActorDef");

                //  GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                int faceHuggerBlastDamage = 1;
                int faceHuggerAcidDamage = 10;
                int faceHuggerAOERadius = 2;

                string skillName = "BC_SwarmerAcidExplosion_Die_AbilityDef";
                RagdollDieAbilityDef source = DefCache.GetDef<RagdollDieAbilityDef>("SwarmerAcidExplosion_Die_AbilityDef");
                RagdollDieAbilityDef sAE = Helper.CreateDefFromClone(
                    source,
                    "1137345a-a18d-4800-b52e-b15d49f4dabf",
                    skillName);
                sAE.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "10729876-f764-41b5-9b4e-c8cb98dca771",
                    skillName);
                DamagePayloadEffectDef sAEEffect = Helper.CreateDefFromClone(
                    DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerAcidExplosion_Die_AbilityDef]"),
                    "ac9cd527-72d4-42d2-af32-5efbdf32812e",
                    "E_Element0 [BC_SwarmerAcidExplosion_Die_AbilityDef]");

                sAE.DeathEffect = sAEEffect;
                sAEEffect.DamagePayload.DamageKeywords[0].Value = faceHuggerBlastDamage;
                sAEEffect.DamagePayload.DamageKeywords[1].Value = faceHuggerAcidDamage;
                sAEEffect.DamagePayload.AoeRadius = faceHuggerAOERadius;

                sAE.ViewElementDef.DisplayName1 = new LocalizedTextBind("ACID EXPLOSION");
                sAE.ViewElementDef.Description = new LocalizedTextBind("Upon death, the mindfragger bursts in an acid explosion damaging nearby targets");

                RagdollDieAbilityDef FHDie = (RagdollDieAbilityDef)faceHugger.Abilities[2];
                FHDie.DeathEffect = sAEEffect;


                tacticalPerceptionMindFraggerEgg.PerceptionRange = 7;
                tacticalPerceptionTerror.PerceptionRange = 18;
                tacticalPerceptionEgg.PerceptionRange = 7;
                tacticalPerceptionHatchling.PerceptionRange = 18;

                foreach (SurveillanceAbilityDef eggSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Egg")))
                {
                    eggSurv.TargetingDataDef.Origin.Range = 7;
                }

                foreach (SurveillanceAbilityDef sentinelSurv in Repo.GetAllDefs<SurveillanceAbilityDef>().Where(p => p.name.Contains("Sentinel")))
                {
                    sentinelSurv.TargetingDataDef.Origin.Range = 18;
                }

                int wormSpeed = 9;
                int wormShredDamage = 3;
                int aWormAcidDamage = 30;
                int aWormBlastDamage = 10;
                int fWormFireDamage = 40;
                int pWormBlastDamage = 25;
                int pWormPoisonDamage = 50;
                fireworm.DeploymentCost = 10;    // 35
                acidworm.DeploymentCost = 10;    // 35
                poisonworm.DeploymentCost = 10;  // 35
                acidWorm.Speed = wormSpeed;
                fireWorm.Speed = wormSpeed;
                poisonWorm.Speed = wormSpeed;

                aWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = aWormBlastDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.AcidKeyword, Value = aWormAcidDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };

                fWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BurningKeyword, Value = fWormFireDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };

                pWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = pWormBlastDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.PoisonousKeyword, Value = pWormPoisonDamage },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.ShreddingKeyword, Value = wormShredDamage },
                };

                /*   foreach (TacticalActorDef actor in Repo.GetAllDefs<TacticalActorDef>().Where(a => a.name.Contains("worm") || a.name.Contains("SpiderDrone")))
                   {
                       actor.GameTags.Add(damagedByCaterpillar);
                   }*/ //included in base mod
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void BEBUff_SirenChiron()
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

                chironAcidMortar.DamagePayload.DamageKeywords[0].Value = 20;

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


        public static void BEBuff_Queen()
        {
            try
            {
                TacticalItemDef queenSpawner = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Spawner_BodyPartDef");
                TacticalItemDef queenBelcher = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
                TacCharacterDef queenCrystal = DefCache.GetDef<TacCharacterDef>("Queen_Crystal_TacCharacterDef");

                BodyPartAspectDef queenHeavyHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Heavy_BodyPartDef]");
                BodyPartAspectDef queenSpitterHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Spitter_Goo_WeaponDef]");
                BodyPartAspectDef queenSonicHead = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Queen_Head_Sonic_WeaponDef]");

                WeaponDef queenSmasher = DefCache.GetDef<WeaponDef>("Queen_Arms_Smashers_WeaponDef");

                MindControlAbilityDef MindControl = DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef");


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



                queenSpawner.Armor = 60;
                queenBelcher.Armor = 60;
                queenHeavyHead.WillPower = 175;
                queenSpitterHead.WillPower = 165;
                queenSonicHead.WillPower = 170;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void BEBuff_StartingEvolution()
        {
            try
            {
                ResearchDef crabGunResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanGunner_ResearchDef");
                //    ResearchDef crabBasicResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef");
                ResearchDef fishWretchResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanSneaker_ResearchDef");
                ResearchDef fishBasicResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanBasic_ResearchDef");
                ResearchDef fishFootpadResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanAssault_ResearchDef");
                //   ResearchDef fishPiercerAssault = DefCache.GetDef<ResearchDef>("ALN_FishmanPiercerAssault_ResearchDef");
                //   ResearchDef fishPiercerSniper = DefCache.GetDef<ResearchDef>("ALN_FishmanPiercerSniper_ResearchDef");
                //   ResearchDef FishThugAlpha = DefCache.GetDef<ResearchDef>("ALN_FishmanEliteStriker_ResearchDef");

                //  ResearchDef Chiron8 = DefCache.GetDef<ResearchDef>("ALN_Chiron8_ResearchDef");
                //  ResearchDef Chiron13 = DefCache.GetDef<ResearchDef>("ALN_Chiron13_ResearchDef");
                //  ResearchDef siren5 = DefCache.GetDef<ResearchDef>("ALN_Siren5_ResearchDef");

                crabGunResearch.InitialStates[4].State = ResearchState.Completed;
                fishWretchResearch.InitialStates[4].State = ResearchState.Completed;
                fishFootpadResearch.InitialStates[4].State = ResearchState.Completed;
                fishBasicResearch.Unlocks = new ResearchRewardDef[0];
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void BEBuff_ArthronsTritons()
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

                arthronAcidGL.DamagePayload.DamageKeywords[1].Value = 20;
                arthronAcidEliteGL.DamagePayload.DamageKeywords[1].Value = 30;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void BEReducePandoranWillpower()
        {
            try
            {
                TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef");

                tacticalPerceptionDef.PerceptionRange = 30;

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Equals("Crabman12_EliteShielder_AlienMutationVariationDef") || a.name.Equals("Crabman12_EliteShielder2_AlienMutationVariationDef")
                || a.name.Equals("Crabman15_UltraShielder_AlienMutationVariationDef")))
                {
                    character.Data.Will -= 4;
                }

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Equals("Crabman12_EliteShielder3_AlienMutationVariationDef")))
                {
                    character.Data.Will -= 2;
                }

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("Pretorian")))
                {
                    character.Data.Will -= 5;
                }

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && (a.name.Contains("EliteViralCommando") || a.name.Contains("UltraViralCommando"))))
                {
                    character.Data.Will -= 5;
                }

                foreach (TacCharacterDef crabMyr in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && (aad.name.Contains("EliteRanger") || aad.name.Contains("UltraRanger"))))
                {
                    crabMyr.Data.Will -= 5;
                }

                foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Fishman")))
                {
                    character.Data.Will -= 5;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }


        }



        internal static void BEFixCaterpillarTracksDamage()
        {
            try
            {
                GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                foreach (TacticalActorDef actor in Repo.GetAllDefs<TacticalActorDef>().Where(a => a.name.Contains("worm") || a.name.Contains("SpiderDrone")))
                {
                    actor.GameTags.Add(damagedByCaterpillar);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }


        internal static void BEChange_Perception()
        {
            try
            {

                TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef");

                // if (Config.AdjustHumanPerception == true)
                // {
                //     tacticalPerceptionDef.PerceptionRange = Config.Human_Soldier_Perception;
                //  }

                BodyPartAspectDef bodyPartAspectDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef2 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Assault_Helmet_BodyPartDef]");
                bodyPartAspectDef2.Perception = 2f;
                BodyPartAspectDef bodyPartAspectDef3 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Helmet_BodyPartDef]");
                bodyPartAspectDef3.Perception = 5f;
                bodyPartAspectDef3.WillPower = 2f;
                BodyPartAspectDef bodyPartAspectDef4 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Helmet_Viking_BodyPartDef]");
                bodyPartAspectDef4.Perception = 5f;
                bodyPartAspectDef4.WillPower = 2f;
                BodyPartAspectDef bodyPartAspectDef5 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Priest_Legs_ItemDef]");
                bodyPartAspectDef5.Perception = 2f;
                BodyPartAspectDef bodyPartAspectDef6 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Priest_Torso_BodyPartDef]");
                bodyPartAspectDef6.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef7 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef7.Perception = -2f;
                BodyPartAspectDef bodyPartAspectDef8 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [PX_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef8.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef9 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Shinobi_BIO_Helmet_BodyPartDef]");
                bodyPartAspectDef9.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef10 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef10.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef11 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [PX_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef11.Perception = 0f;
                BodyPartAspectDef bodyPartAspectDef12 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [IN_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef12.Perception = -2f;
                BodyPartAspectDef bodyPartAspectDef13 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Watcher_Helmet_BodyPartDef]");
                bodyPartAspectDef13.Perception = 8f;
                BodyPartAspectDef bodyPartAspectDef14 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Infiltrator_Helmet_BodyPartDef]");
                bodyPartAspectDef14.Perception = 5f;
                TacticalItemDef styxHelmet = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Helmet_BodyPartDef");
                styxHelmet.BodyPartAspectDef.Perception = 5f;
                BodyPartAspectDef bodyPartAspectDef15 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Watcher_Torso_BodyPartDef]");
                bodyPartAspectDef15.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef16 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Exo_BIO_Helmet_BodyPartDef]");
                bodyPartAspectDef16.Perception = 3f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void BECreateAIActionDefs()
        {
            BEClone_PsychicScreamAI();
            BEClone_InstillFrenzyAI();
        }
        public static void BEClone_PsychicScreamAI()
        {
            try
            {
                ApplyEffectAbilityDef MindCrush = DefCache.GetDef<ApplyEffectAbilityDef>("MindCrush_AbilityDef");

                string mindCrushName = "MoveAndDoMindCrush_AIActionDef";
                AIActionMoveAndExecuteAbilityDef source = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoPsychicScream_AIActionDef");
                AIActionMoveAndExecuteAbilityDef MindCrushAI = Helper.CreateDefFromClone(
                    source,
                    "45A50BBB-02A2-4CF7-A6A8-28D8DA8C7250",
                    mindCrushName);
                MindCrushAI.EarlyExitConsiderations[1].Consideration = Helper.CreateDefFromClone(
                    source.EarlyExitConsiderations[1].Consideration,
                    "C5054388-18F5-4AD6-BB30-85C27749ECD7",
                    "MindCrushAbilityEnabled_AIConsiderationDef");
                MindCrushAI.Evaluations[0].Considerations[0].Consideration = Helper.CreateDefFromClone(
                    source.Evaluations[0].Considerations[0].Consideration,
                    "88464571-E231-4D3E-9F86-F18A759FA9EA",
                    "MindCrushProximityToTargets_AIConsiderationDef");
                MindCrushAI.Evaluations[0].Considerations[1].Consideration = Helper.CreateDefFromClone(
                    source.Evaluations[0].Considerations[1].Consideration,
                    "53546688-659F-4550-927A-2A0EBA143E3D",
                    "MindCrushNumberOfEnemiesInRange_AIConsiderationDef");
                MindCrushAI.Evaluations[0].Considerations[2].Consideration = Helper.CreateDefFromClone(
                    source.Evaluations[0].Considerations[2].Consideration,
                    "BC9C2BA8-9D13-4503-AE19-DF91B7278321",
                    "WillpointsLeftAfterMindCrush_AIConsiderationDef");

                MindCrushAI.Weight = 999;
                MindCrushAI.AbilityToExecute = MindCrush;
                AIAbilityDisabledStateConsiderationDef EarlyExitConsideration1 = (AIAbilityDisabledStateConsiderationDef)MindCrushAI.EarlyExitConsiderations[1].Consideration;
                EarlyExitConsideration1.Ability = MindCrush;
                AIProximityToEnemiesConsiderationDef Consideration1 = (AIProximityToEnemiesConsiderationDef)MindCrushAI.Evaluations[0].Considerations[0].Consideration;
                Consideration1.MaxRange = 10;
                AINumberOfEnemiesInRangeConsiderationDef Consideration2 = (AINumberOfEnemiesInRangeConsiderationDef)MindCrushAI.Evaluations[0].Considerations[1].Consideration;
                Consideration2.MaxEnemies = 5;
                Consideration2.MaxRange = 10;
                AIWillpointsLeftAfterAbilityConsiderationDef Consideration3 = (AIWillpointsLeftAfterAbilityConsiderationDef)MindCrushAI.Evaluations[0].Considerations[2].Consideration;
                Consideration3.Ability = MindCrush;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void BEClone_InstillFrenzyAI()
        {
            try
            {
                ApplyStatusAbilityDef ElectricReinforcement = DefCache.GetDef<ApplyStatusAbilityDef>("ElectricReinforcement_AbilityDef");

                string Name = "ElectricReinforcement_AIActionDef";
                AIActionExecuteAbilityDef source = DefCache.GetDef<AIActionExecuteAbilityDef>("InstilFrenzy_AIActionDef");
                AIActionExecuteAbilityDef ElectricReinforcementAI = Helper.CreateDefFromClone(
                    source,
                    "A8211067-3261-4AF6-B459-8E3C468965AD",
                    Name);
                ElectricReinforcementAI.EarlyExitConsiderations[1].Consideration = Helper.CreateDefFromClone(
                    source.EarlyExitConsiderations[1].Consideration,
                    "051874DC-67D7-4656-A823-C896B1A80F2B",
                    "ElectricReinforcementEnabled_AIConsiderationDef");
                ElectricReinforcementAI.Evaluations[0].Considerations[0].Consideration = Helper.CreateDefFromClone(
                    source.Evaluations[0].Considerations[0].Consideration,
                    "8ACA689A-C0CB-490F-B243-BC5598CD7F7A",
                    "ElectricReinforcementNumberOfTargets_AIConsiderationDef");

                ElectricReinforcementAI.Weight = 999;
                ElectricReinforcementAI.AbilityDefs[0] = ElectricReinforcement;
                AIAbilityDisabledStateConsiderationDef EarlyExitConsideration1 = (AIAbilityDisabledStateConsiderationDef)ElectricReinforcementAI.EarlyExitConsiderations[1].Consideration;
                EarlyExitConsideration1.Ability = ElectricReinforcement;
                AIAbilityNumberOfTargetsConsiderationDef Consideration1 = (AIAbilityNumberOfTargetsConsiderationDef)ElectricReinforcementAI.Evaluations[0].Considerations[0].Consideration;
                Consideration1.Ability = ElectricReinforcement;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void BEFixesToAI()
        {
            try
            {

                //Make Tritons shoot more, strike less
                AIActionMoveAndAttackDef mAShoot = Helper.CreateDefFromClone(
                        DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef"),
                        "3fd2dfd1-3cc0-4c71-b427-22afd020b45d",
                        "BC_MoveAndShoot_AIActionDef");
                /*    AIActionMoveAndAttackDef mAStrike = Helper.CreateDefFromClone(
                        DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndStrike_AIActionDef"),
                        "78c28fb8-0573-467a-a1c3-94b40673ef47",
                        "VC_MoveAndStrike_AIActionDef");*/

                AIActionsTemplateDef fishmanAI = DefCache.GetDef<AIActionsTemplateDef>("Fishman_AIActionsTemplateDef");
                fishmanAI.ActionDefs[2] = mAShoot;
                // fishmanAI.ActionDefs[3] = mAStrike;
                mAShoot.Weight = 500;
                // mAStrike.Weight = 300;

                //Adding Acid Torso attack for Sirens
                AIActionsTemplateDef SirenAITemplate = DefCache.GetDef<AIActionsTemplateDef>("Siren_AIActionsTemplateDef");
                WeaponDef sirenArmisAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_Orichalcum_WeaponDef");

                sirenArmisAcidTorso.Tags.Add(DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"));
                List<AIActionDef> sirenAIActions = new List<AIActionDef>(SirenAITemplate.ActionDefs.ToList())
                {
                    mAShoot
                };
                SirenAITemplate.ActionDefs = sirenAIActions.ToArray();
              //  TFTVLogger.Always("SirenAITemplate");
                //reduce weight for neuralDisrupt AI action
                AIActionMoveAndExecuteAbilityDef NeuralDisruptAI = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoSilence_AIActionDef");
                NeuralDisruptAI.Weight = 32.5f;

                //Reduce healing, increase dash and strike, remove stomp, add mindcrush and electric reinforcement
                AIActionsTemplateDef soldierAI = DefCache.GetDef<AIActionsTemplateDef>("AIActionsTemplateDef");

                AIActionMoveAndHealDef healAIAction = DefCache.GetDef<AIActionMoveAndHealDef>("MoveAndHeal_AIActionDef");
                AIActionMoveAndAttackDef dashaAndStikeAIAction = DefCache.GetDef<AIActionMoveAndAttackDef>("DashAndStrike_AIActionDef");
                AIActionMoveAndExecuteAbilityDef moveAndStompAIAction = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndStomp_AIActionDef");

                healAIAction.Weight = 2;
                dashaAndStikeAIAction.Weight = 350;

                List<AIActionDef> soldierAIActionDefs = new List<AIActionDef>(soldierAI.ActionDefs.ToList());
                soldierAIActionDefs.Remove(moveAndStompAIAction);
                soldierAIActionDefs.Add(DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoMindCrush_AIActionDef"));
                soldierAIActionDefs.Add(DefCache.GetDef<AIActionExecuteAbilityDef>("ElectricReinforcement_AIActionDef"));

                soldierAI.ActionDefs = soldierAIActionDefs.ToArray();
             //   TFTVLogger.Always("SoldierAITemplate");

                //Reduce weight for Acheron recover
                AIActionExecuteAbilityDef acheronRecover = DefCache.GetDef<AIActionExecuteAbilityDef>("Acheron_Recover_AIActionDef");
                acheronRecover.Weight = 250;

                //Add mindcontrol to Scylla; it's used by buffed scylla, but doesn't hurt anyway
                AIActionMoveAndExecuteAbilityDef moveAndDoMC = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("Siren_MoveAndDoMindControl_AIActionDef");
                AIActionsTemplateDef queenAITemplate = DefCache.GetDef<AIActionsTemplateDef>("Queen_AIActionsTemplateDef");
                List<AIActionDef> scyllaActionDefs = new List<AIActionDef>(queenAITemplate.ActionDefs.ToList())
                {
                    moveAndDoMC
                };
                queenAITemplate.ActionDefs = scyllaActionDefs.ToArray();
              //  TFTVLogger.Always("QueenAITemplate");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}
