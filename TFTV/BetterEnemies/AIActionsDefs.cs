using Base.AI.Defs;
using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class AIActionDefs
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
       
        public static void Apply_AIActionDefs()
        {
            Clone_PsychicScreamAI();  
            Clone_InstillFrenzyAI();
            Add_NewAIActionDefs();
            ChangeAI();
        }
        public static void Clone_PsychicScreamAI()
        {
            try
            {
                ApplyEffectAbilityDef MindCrush = DefCache.GetDef<ApplyEffectAbilityDef>("MindCrush_AbilityDef");

                string Name = "MoveAndDoMindCrush_AIActionDef";
                AIActionMoveAndExecuteAbilityDef source = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoPsychicScream_AIActionDef");
                AIActionMoveAndExecuteAbilityDef MindCrushAI = Helper.CreateDefFromClone(
                    source,
                    "45A50BBB-02A2-4CF7-A6A8-28D8DA8C7250",
                    Name);
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
            catch(Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void Clone_InstillFrenzyAI()
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
             
            catch(Exception e)
            {
                TFTVLogger.Error(e);
            }
}
        public static void Add_NewAIActionDefs()
        {
            try { 
            AIActionsTemplateDef DefaultAIActionsTemplateDef = DefCache.GetDef<AIActionsTemplateDef>("AIActionsTemplateDef");

            DefaultAIActionsTemplateDef.ActionDefs = new AIActionDef[]
            {
                DefaultAIActionsTemplateDef.ActionDefs[0],
                DefaultAIActionsTemplateDef.ActionDefs[1],
                DefaultAIActionsTemplateDef.ActionDefs[2],
                DefaultAIActionsTemplateDef.ActionDefs[3],
                DefaultAIActionsTemplateDef.ActionDefs[4],
                DefaultAIActionsTemplateDef.ActionDefs[5],
                DefaultAIActionsTemplateDef.ActionDefs[6],
                DefaultAIActionsTemplateDef.ActionDefs[7],
                DefaultAIActionsTemplateDef.ActionDefs[8],
                DefaultAIActionsTemplateDef.ActionDefs[9],
                DefaultAIActionsTemplateDef.ActionDefs[10],
                DefaultAIActionsTemplateDef.ActionDefs[11],
                DefaultAIActionsTemplateDef.ActionDefs[12],
                //DefaultAIActionsTemplateDef.ActionDefs[13],
                DefaultAIActionsTemplateDef.ActionDefs[14],
                DefaultAIActionsTemplateDef.ActionDefs[15],
                DefaultAIActionsTemplateDef.ActionDefs[16],
                DefaultAIActionsTemplateDef.ActionDefs[17],
                DefaultAIActionsTemplateDef.ActionDefs[18],
                DefaultAIActionsTemplateDef.ActionDefs[19],
                DefaultAIActionsTemplateDef.ActionDefs[20],
                DefaultAIActionsTemplateDef.ActionDefs[21],
                DefaultAIActionsTemplateDef.ActionDefs[22],
                DefaultAIActionsTemplateDef.ActionDefs[23],
                DefaultAIActionsTemplateDef.ActionDefs[25],
                DefaultAIActionsTemplateDef.ActionDefs[25],
                DefaultAIActionsTemplateDef.ActionDefs[26],
                DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoMindCrush_AIActionDef"),
                //DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>().FirstOrDefault(p => p.name.Equals("MoveAndDoBigBooms_AIActionDef")),
                DefCache.GetDef<AIActionExecuteAbilityDef>("ElectricReinforcement_AIActionDef"),
            };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void ChangeAI()
        {
            try 
            { 
            AIActionMoveAndExecuteAbilityDef NeuralDisruptAI = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoSilence_AIActionDef");

            NeuralDisruptAI.Weight = 32.5f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
