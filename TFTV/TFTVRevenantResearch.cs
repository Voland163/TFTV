using Base.Defs;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVRevenantResearch
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void CreateDefs()
        {
            try
            {
                string nameRevenantCaptureResearch = "PX_Revenant_Live_Research";
                string revenantVariable = "Revenant_Encountered_Variable";
                string viewElementName = "Captured Revenant";
                string viewElementReveal = "We have to capture a Revenant alive";
                string viewElementUnlock = "The operatives who captured this creature claim that it is {}, a former comrade, returned as a Pandoran monstrosity. We should examine it as soon as possible.";
                string viewElementComplete = "There is no room for doubt: this creature is, or used to be {}. What has been done to our friend and how... I would rather not try to imagine it. I wanted to hope that there was no shred of consciousness of the former self left in that monstrosity, but... I could swear it recognized me." +
                    "\n\nThere is nothing we can do for our comrade now, and I'm afraid that killing it won't provide any release.We might encounter it again and again: the Pandorans splice and clone their victims ad infinitum..." +
                    "\n\nWe have to defeat the Pandoravirus once and for all. It's the only way to be sure.";

                GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Any_Revenant_TagDef"));

                ResearchViewElementDef revenantResearchViewElement =
                    TFTVCommonMethods.CreateNewResearchViewElementNoKeys(nameRevenantCaptureResearch + "ViewElement_Def", "B94BF4EC-4227-4D87-900D-48AB3B970DC1",
                    viewElementName, viewElementReveal, viewElementUnlock, viewElementComplete);
                ResearchDef revenantCaptureResearch =
                    TFTVCommonMethods.CreateNewPXResearch(nameRevenantCaptureResearch, 200, "B5CC42DE-016F-4151-ACFA-8604C9C4CCCF", revenantResearchViewElement);

                EncounterVariableResearchRequirementDef revenantEncounterVariableResearch =
                    TFTVCommonMethods.CreateNewEncounterVariableResearchRequirementDef(nameRevenantCaptureResearch + "EncounterVariableResearchReq", "2857133D-C201-4BF8-B505-AF80863BA4EE",
                    revenantVariable, 1);

                ReseachRequirementDefOpContainer[] revenantReseachRevealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] revenantRevealResearchRequirementDefs = new ResearchRequirementDef[1];
                revenantRevealResearchRequirementDefs[0] = revenantEncounterVariableResearch; //small box
                revenantReseachRevealRequirementContainer[0].Requirements = revenantRevealResearchRequirementDefs; //medium box
                

                CaptureActorResearchRequirementDef captureRevenantResearchRequirementDef =
                TFTVCommonMethods.CreateNewTagCaptureActorResearchRequirementDef( "0A57C449-BC81-4768-9AE9-61BE1443F278", nameRevenantCaptureResearch + "_CaptureRequirementDef", viewElementReveal);
                captureRevenantResearchRequirementDef.Tag = revenantTag;
                ReseachRequirementDefOpContainer[] revenantReseachUnlockRequirementContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] revenantUnlockResearchRequirementDefs = new ResearchRequirementDef[1];
                revenantUnlockResearchRequirementDefs[0] = captureRevenantResearchRequirementDef; //small box
                revenantReseachRevealRequirementContainer[0].Requirements = revenantUnlockResearchRequirementDefs; //medium box*/

                revenantCaptureResearch.RevealRequirements.Container = revenantReseachUnlockRequirementContainer;
                revenantCaptureResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                revenantCaptureResearch.UnlockRequirements.Container = revenantReseachUnlockRequirementContainer;
                revenantCaptureResearch.UnlockRequirements.Operation = ResearchContainerOperation.ALL;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
