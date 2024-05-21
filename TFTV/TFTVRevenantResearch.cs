using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVRevenantResearch
    {

        private static readonly DefCache DefCache = new DefCache();
        //   private static readonly DefRepository Repo = TFTVMain.Repo;

        public static bool RevenantCaptured = false;

        private static readonly GameTagDef revenantTier1GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_1_GameTagDef");
        private static readonly GameTagDef revenantTier2GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef");
        private static readonly GameTagDef revenantTier3GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef");
        private static readonly GameTagDef anyRevenantGameTag = TFTVRevenant.AnyRevenantGameTag; //DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");
        private static readonly string RevenantCapturedVariable = "RevenantCapturedVariable";
        private static readonly string RevenantsDestroyed = "RevenantsDestroyed";
        public static string NameOfCapturedRevenant = "";
        public static int RevenantPoints = 0;
        public static bool ProjectOsiris = false;
        public static Dictionary<int, int[]> ProjectOsirisStats = new Dictionary<int, int[]>();

        public static void CreateRevenantRewardsDefs()
        {
            CreateProjectOsirisResearch();
            CreateRevenantLiveResearch();
        }

        public static void RecordStatsOfDeadSoldier(TacticalActorBase deadSoldier)
        {

            try
            {
                TacticalActor actor = deadSoldier as TacticalActor;
                int endurance = actor.CharacterStats.Endurance.Value.BaseValueInt;
                int willpower = actor.CharacterStats.Willpower.Value.BaseValueInt;
                int speed = actor.CharacterStats.Speed.Value.BaseValueInt;

                ProjectOsirisStats.Add(deadSoldier.GeoUnitId, new int[] { endurance, willpower, speed });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckProjectOsiris(GeoLevelController controller)
        {
            try
            {
                if (controller.PhoenixFaction.Research.HasCompleted("PX_Project_Osiris_Research"))
                {
                    ProjectOsiris = true;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateRevenantLiveResearch()
        {
            try
            {
                string defName = "PX_Revenant_Live_Research";

                string title = "PX_REVENANT_LIVE_RESEARCH_TITLE";
                string reveal = "";
                string unlock = "PX_REVENANT_LIVE_RESEARCH_REVEAL";
                string complete = "PX_REVENANT_LIVE_RESEARCH_COMPLETE";
                string benefits = "PX_REVENANT_LIVE_RESEARCH_BENEFITS";
                string gUID = "B94BF4EC-4227-4D87-900D-48AB3B970DC1";
                string gUID2 = "B5CC42DE-016F-4151-ACFA-8604C9C4CCCF";
                int cost = 200;

                ResearchDef revenantCaptureResearch =
                    TFTVCommonMethods.CreateNewPXResearch(defName, cost, gUID, gUID2, title, reveal, unlock, complete, benefits, null);

                string revenantVariable = RevenantCapturedVariable;

                EncounterVariableResearchRequirementDef revenantEncounterVariableResearch =
                    TFTVCommonMethods.CreateNewEncounterVariableResearchRequirementDef(defName + "EncounterVariableResearchReq", "2857133D-C201-4BF8-B505-AF80863BA4EE",
                    revenantVariable, 1);

                ReseachRequirementDefOpContainer[] revenantReseachRevealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] revenantRevealResearchRequirementDefs = new ResearchRequirementDef[1];
                revenantRevealResearchRequirementDefs[0] = revenantEncounterVariableResearch; //small box
                revenantReseachRevealRequirementContainer[0].Requirements = revenantRevealResearchRequirementDefs; //medium box
                revenantCaptureResearch.RevealRequirements.Container = revenantReseachRevealRequirementContainer;
                revenantCaptureResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateProjectOsirisResearch()
        {
            try
            {
                string defName = "PX_Project_Osiris_Research";


                string title = "PX_PROJECT_OSIRIS_TITLE";
                string reveal = "";
                string complete = "PX_PROJECT_OSIRIS_COMPLETE";
                string unlock = "PX_PROJECT_OSIRIS_REVEAL";
                string benefits = "PX_PROJECT_OSIRIS_BENEFITS";
                string gUID = "E91914A2-B077-40F0-AB98-6560537A89C8";
                string gUID2 = "040593DB-C61F-4C2A-A908-1B84C62424AF";
                int cost = 400;

                ResearchDef enoughRevenantsKilledResearch =
                    TFTVCommonMethods.CreateNewPXResearch(defName, cost, gUID, gUID2, title, reveal, unlock, complete, benefits, null);

                string revenantVariable = RevenantsDestroyed;

                EncounterVariableResearchRequirementDef revenantEncounterVariableResearch =
                    TFTVCommonMethods.CreateNewEncounterVariableResearchRequirementDef(defName + "EncounterVariableResearchReq", "009E4EC9-94ED-488A-A00D-536BFA750CEB",
                    revenantVariable, 10);

                ReseachRequirementDefOpContainer[] revenantReseachRevealRequirementContainer = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] revenantRevealResearchRequirementDefs = new ResearchRequirementDef[1];
                revenantRevealResearchRequirementDefs[0] = revenantEncounterVariableResearch; //small box
                revenantReseachRevealRequirementContainer[0].Requirements = revenantRevealResearchRequirementDefs; //medium box
                enoughRevenantsKilledResearch.RevealRequirements.Container = revenantReseachRevealRequirementContainer;
                enoughRevenantsKilledResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckRevenantResearchRequirements(GeoLevelController controller)
        {
            try
            {
                TFTVLogger.Always($"Passing Revenant Points to Geoscape variable {RevenantPoints}");

                if (RevenantCaptured && controller.EventSystem.GetVariable(RevenantCapturedVariable) == 0)
                {
                    controller.EventSystem.SetVariable(RevenantCapturedVariable, 1);
                    RevenantCaptured = false;
                    TFTVLogger.Always("Id of captured revenant " + TFTVRevenant.revenantID);

                    foreach (GeoTacUnitId geoTacUnitId in controller.DeadSoldiers.Keys)
                    {
                        if (geoTacUnitId == TFTVRevenant.revenantID)
                        {
                            NameOfCapturedRevenant = controller.DeadSoldiers[geoTacUnitId].Identity.Name;
                        }
                    }
                    TFTVLogger.Always("Name of captured revenant " + NameOfCapturedRevenant);

                    /* string viewElementName = "Captured Revenant";
                     string revenantVariable = RevenantCapturedVariable;

                     ResearchViewElementDef revenantResearchViewElement = Repo.GetAllDefs<ResearchViewElementDef>().FirstOrDefault(rve => rve.name.Equals("PX_Revenant_Live_Research_ViewElement_Def"));
                     revenantResearchViewElement.DisplayName1 = new LocalizedTextBind(viewElementName, true);
                     revenantResearchViewElement.RevealText = new LocalizedTextBind("The operatives who captured this creature claim that it is "+ NameOfCapturedRevenant
                         + ", a former comrade, returned as a Pandoran monstrosity. We should examine it as soon as possible.", true);
                     revenantResearchViewElement.CompleteText = new LocalizedTextBind("There is no room for doubt: this creature is, or used to be " + NameOfCapturedRevenant + ". What has been done to our friend and how..." +
                         " I would rather not try to imagine it. I wanted to hope that there was no shred of consciousness of the former self left in that monstrosity, but... I could swear it recognized me." +
                         "\n\nThere is nothing we can do for our comrade now, and I'm afraid that killing it won't provide any release.We might encounter it again and again: the Pandorans splice and clone their victims ad infinitum..." +
                         "\n\nWe have to defeat the Pandoravirus once and for all. It's the only way to be sure.", true);*/

                }
                else if (controller.PhoenixFaction.Research.HasCompleted("PX_Revenant_Live_Research") && RevenantPoints > 0)
                {
                    TFTVLogger.Always($"Adding {RevenantPoints} to {RevenantsDestroyed}");
                    RevenantCaptured = false;
                    controller.EventSystem.SetVariable(RevenantsDestroyed, controller.EventSystem.GetVariable(RevenantsDestroyed) + RevenantPoints);

                }
                else
                {
                    RevenantCaptured = false;
                }

                if (controller.EventSystem.GetVariable(RevenantsDestroyed) > 10)
                {
                    controller.EventSystem.SetVariable(RevenantsDestroyed, 10);
                }

                RevenantPoints = 0;
                TFTVRevenant.RecordUpkeep.SkillPointsForRevenantKillAwarded = false;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CheckRevenantCapturedOrKilled(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {

                    if (TFTVRevenant.revenantID != 0 && controller.GetFactionByCommandName("PX").State == TacFactionState.Won)
                    {
                        foreach (TacticalActorBase pandoranActorBase in controller.GetFactionByCommandName("ALN").Actors)
                        {
                            TacticalActor pandoranActor = pandoranActorBase as TacticalActor;

                            if (pandoranActorBase.HasGameTag(anyRevenantGameTag) && pandoranActor.Status.GetStatus<ParalysedStatus>(DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef")) != null)
                            {
                                TFTVRevenant.revenantSpawned = true;
                                TFTVLogger.Always("Revenant was captured, so revenantSpawned is now " + TFTVRevenant.revenantSpawned);

                                RevenantCaptured = true;
                                if (pandoranActor.HasGameTag(revenantTier1GameTag))
                                {
                                    RevenantPoints = 1;
                                }
                                else if (pandoranActor.HasGameTag(revenantTier2GameTag))
                                {
                                    RevenantPoints = 5;
                                }
                                else if (pandoranActor.HasGameTag(revenantTier3GameTag))
                                {
                                    RevenantPoints = 10;
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
