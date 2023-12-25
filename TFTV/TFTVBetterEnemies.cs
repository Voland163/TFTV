using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
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
                // BodyPartAspectDef bodyPartAspectDef4 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Helmet_Viking_BodyPartDef]");
                // bodyPartAspectDef4.Perception = 5f;
                //  bodyPartAspectDef4.WillPower = 2f;
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


                AIAbilityNumberOfTargetsConsiderationDef Consideration2 = (AIAbilityNumberOfTargetsConsiderationDef)MindCrushAI.Evaluations[0].Considerations[1].Consideration;
                Consideration2.Ability = MindCrush;

 

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
