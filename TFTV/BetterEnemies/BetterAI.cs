using Base.AI.Defs;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;

namespace TFTV.BetterEnemies
{
    internal class BetterAI
    {
        //private static readonly DefRepository Repo = TFTVMain.Repo;
        //  private static readonly SharedData Shared = BetterEnemiesMain.Shared;

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static void Change_AI()
        {
            try
            {

                AIActionsTemplateDef queenAITemplate = DefCache.GetDef<AIActionsTemplateDef>("Queen_AIActionsTemplateDef");
                AIActionsTemplateDef QueenAI = DefCache.GetDef<AIActionsTemplateDef>("Queen_AIActionsTemplateDef");

                AIActionsTemplateDef soldierAI = DefCache.GetDef<AIActionsTemplateDef>("AIActionsTemplateDef");


                AIActionsTemplateDef fishmanAI = DefCache.GetDef<AIActionsTemplateDef>("Fishman_AIActionsTemplateDef");


                AIActionsTemplateDef SirenAITemplate = DefCache.GetDef<AIActionsTemplateDef>("Siren_AIActionsTemplateDef");

                WeaponDef sirenArmisAcidTorso = DefCache.GetDef<WeaponDef>("Siren_Torso_Orichalcum_WeaponDef");

                AIActionsTemplateDef acheronAAI = DefCache.GetDef<AIActionsTemplateDef>("AcheronAggressive_AIActionsTemplateDef");

                AIActionMoveAndExecuteAbilityDef moveAndQuickAimAI = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndQuickAim_AIActionDef");

                AIActionMoveAndAttackDef mAShoot = Helper.CreateDefFromClone(
                        DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef"),
                        "3fd2dfd1-3cc0-4c71-b427-22afd020b45d",
                        "BC_MoveAndShoot_AIActionDef");
                AIActionMoveAndAttackDef mAStrike = Helper.CreateDefFromClone(
                    DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndStrike_AIActionDef"),
                    "78c28fb8-0573-467a-a1c3-94b40673ef47",
                    "VC_MoveAndStrike_AIActionDef");


                fishmanAI.ActionDefs[2] = mAShoot;
                fishmanAI.ActionDefs[3] = mAStrike;
                fishmanAI.ActionDefs[2].Weight = 500;
                mAShoot.Weight = 500;
                fishmanAI.ActionDefs[3].Weight = 300;

                moveAndQuickAimAI.Weight = 75;

                soldierAI.ActionDefs[7].Weight = 2;
                soldierAI.ActionDefs[26].Weight = 350;

                acheronAAI.ActionDefs[1].Weight = 250;

                QueenAI.ActionDefs[9].Weight = 0.01f;


                queenAITemplate.ActionDefs = new AIActionDef[]
                {
                queenAITemplate.ActionDefs[0],
                queenAITemplate.ActionDefs[1],
                queenAITemplate.ActionDefs[2],
                queenAITemplate.ActionDefs[3],
                queenAITemplate.ActionDefs[4],
                queenAITemplate.ActionDefs[5],
                queenAITemplate.ActionDefs[6],
                queenAITemplate.ActionDefs[7],
                queenAITemplate.ActionDefs[8],
                queenAITemplate.ActionDefs[9],
                queenAITemplate.ActionDefs[10],
                queenAITemplate.ActionDefs[12],
                queenAITemplate.ActionDefs[13],
                SirenAITemplate.ActionDefs[9],
                };

                SirenAITemplate.ActionDefs = new AIActionDef[]
                {
                SirenAITemplate.ActionDefs[0],
                SirenAITemplate.ActionDefs[1],
                SirenAITemplate.ActionDefs[2],
                SirenAITemplate.ActionDefs[3],
                SirenAITemplate.ActionDefs[4],
                SirenAITemplate.ActionDefs[5],
                SirenAITemplate.ActionDefs[6],
                SirenAITemplate.ActionDefs[7],
                SirenAITemplate.ActionDefs[8],
                SirenAITemplate.ActionDefs[9],
                mAShoot,
                };

                sirenArmisAcidTorso.Tags = new GameTagsList
            {
                sirenArmisAcidTorso.Tags[0],
                sirenArmisAcidTorso.Tags[1],
                sirenArmisAcidTorso.Tags[2],
                sirenArmisAcidTorso.Tags[3],
                sirenArmisAcidTorso.Tags[4],
                DefCache.GetDef<ItemClassificationTagDef>("GunWeapon_TagDef"),
            };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
