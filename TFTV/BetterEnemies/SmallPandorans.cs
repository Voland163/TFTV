using Base.Core;
using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.BetterEnemies
{
    internal class SmallPandorans
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        //  private static readonly SharedData Shared = TFTVMain.Shared;


        public static void Change_SmallCharactersAndSentinels()
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

                GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

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
    }
}




