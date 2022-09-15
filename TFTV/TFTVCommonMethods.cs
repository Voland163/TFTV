using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    internal class TFTVCommonMethods
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ClearInternalVariables()
        {
            try
            {
                TFTVAirCombat.targetsForBehemoth = new List<int>();
                //  TFTVAirCombat.targetsVisitedByBehemoth = new List<int>();
                TFTVAirCombat.flyersAndHavens = new Dictionary<int, List<int>>();
                TFTVAirCombat.checkHammerfall = false;
                TFTVRevenant.DeadSoldiersDelirium = new Dictionary<string, int>();
                TFTVVoidOmens.voidOmensCheck = new bool[18];
                //VO#1 is harder ambushes
                TFTVVoidOmens.VoidOmen1Active = false;
                //VO#3 is WP cost +50%
                TFTVVoidOmens.VoidOmen3Active = false;
                TFTVVoidOmens.VoidOmen4Active = false;
                //VO#5 is haven defenders hostile; this is needed for victory kludge
                TFTVVoidOmens.VoidOmen5Active = false;
                //VO#7 is more mist in missions
                TFTVVoidOmens.VoidOmen7Active = false;
                //VO#10 is no limit to Delirium
                TFTVVoidOmens.VoidOmen10Active = false;
                //VO#12 is +50% strength of alien attacks on Havens
                TFTVVoidOmens.VoidOmen12Active = false;
                //VO#15 is more Umbra
                TFTVVoidOmens.VoidOmen15Active = false;
                //VO#16 is Umbras can appear anywhere and attack anyone
                TFTVVoidOmens.VoidOmen16Active = false;
                TFTVUmbra.UmbraResearched = false;
                TFTVRevenant.daysRevenantLastSeen = 0;
                TFTVStamina.charactersWithBrokenLimbs = new List<int>();
                TFTVUI.hookToProgressionModule = null;
                TFTVUI.hookToCharacter = null;
                TFTVAirCombat.behemothScenicRoute = new List<int>();
                TFTVAirCombat.behemothTarget = 0;
                TFTVAirCombat.behemothWaitHours = 12;
                TFTVRevenant.revenantSpecialResistance = new List<string>();
                TFTVRevenant.revenantSpawned = false;
                TFTVRevenant.revenantCanSpawn = false;
                TFTVHumanEnemies.difficultyLevel = 0;
                TFTVHumanEnemies.HumanEnemiesAndTactics = new Dictionary<string, int>();
              //  TFTVRevenant.timeLastRevenantSpawned = new TimeSpan();
    }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void SetStaminaToZero(GeoCharacter __instance)
        {
            try
            {
                if (__instance.Fatigue != null && __instance.Fatigue.Stamina > 0 && (__instance.TemplateDef.IsHuman || __instance.TemplateDef.IsMutoid))
                {
                    __instance.Fatigue.Stamina.SetToMin();
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void GenerateGeoEventChoice(GeoscapeEventDef geoEvent, string choice, string outcome)
        {
            try
            {
                geoEvent.GeoscapeEventData.Choices.Add(new GeoEventChoice()

                {
                    Text = new LocalizedTextBind(choice),
                    Outcome = new GeoEventChoiceOutcome()
                    {
                        OutcomeText = new EventTextVariation()
                        {
                            General = new LocalizedTextBind(outcome)
                        }
                    }
                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static OutcomeDiplomacyChange GenerateDiplomacyOutcome(GeoFactionDef partyFaction, GeoFactionDef targetFaction, int value)
        {
            try
            {
                return new OutcomeDiplomacyChange()
                {
                    PartyFaction = partyFaction,
                    TargetFaction = targetFaction,
                    Value = value,
                    PartyType = (OutcomeDiplomacyChange.ChangeTarget)1,
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static OutcomeVariableChange GenerateVariableChange(string variableName, int value, bool isSet)
        {
            try
            {
                return new OutcomeVariableChange()
                {
                    VariableName = variableName,
                    Value = { Min = value, Max = value },
                    IsSetOperation = isSet,
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static GeoscapeEventDef CreateNewEvent(string name, string title, string description, string outcome)
        {
            try
            {

                string gUID = Guid.NewGuid().ToString();
                GeoscapeEventDef sourceLoseGeoEvent = Repo.GetAllDefs<GeoscapeEventDef>().FirstOrDefault(ged => ged.name.Equals("PROG_PU12_FAIL_GeoscapeEventDef"));
                GeoscapeEventDef newEvent = Helper.CreateDefFromClone(sourceLoseGeoEvent, gUID, name);
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReEneableEvent = false;
                newEvent.GeoscapeEventData.Choices[0].Outcome.ReactiveEncounters.Clear();
                newEvent.GeoscapeEventData.EventID = name;
                newEvent.GeoscapeEventData.Title.LocalizationKey = title;
                newEvent.GeoscapeEventData.Description[0].General.LocalizationKey = description;
                if (outcome != null)
                {
                    newEvent.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General.LocalizationKey = outcome;
                }
                return newEvent;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static ResearchDef CreateNewPXResearch(string id, int cost, string gUID, ResearchViewElementDef researchViewElementDef)

        {
            try
            {
                ResearchDef sourceResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_AtmosphericAnalysis_ResearchDef"));
                ResearchDef researchDef = Helper.CreateDefFromClone(sourceResearchDef, gUID, id);
                ResearchDef secondarySourceResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("PX_AlienGoo_ResearchDef"));

                ResearchDbDef researchDB = Repo.GetAllDefs<ResearchDbDef>().FirstOrDefault(ged => ged.name.Equals("pp_ResearchDB"));
                researchDef.Id = id;
                researchDef.InitialStates[0].State = ResearchState.Hidden;
                researchDef.ResearchCost = cost;
                researchDef.ViewElementDef = researchViewElementDef;
                researchDef.Unlocks = secondarySourceResearchDef.Unlocks;
                researchDef.Tags = secondarySourceResearchDef.Tags;
                researchDB.Researches.Add(researchDef);
                return researchDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static ResearchViewElementDef CreateNewResearchViewElement(string def, string gUID, string name, string reveal, string unlock, string complete)

        {
            try
            {

                ResearchViewElementDef sourceResearchViewDef = Repo.GetAllDefs<ResearchViewElementDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_CorruptionNode_ViewElementDef"));
                ResearchViewElementDef researchViewDef = Helper.CreateDefFromClone(sourceResearchViewDef, gUID, def);
                researchViewDef.DisplayName1.LocalizationKey = name;
                researchViewDef.RevealText.LocalizationKey = reveal;
                researchViewDef.UnlockText.LocalizationKey = unlock;
                researchViewDef.CompleteText.LocalizationKey = complete;
                return researchViewDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static ResearchViewElementDef CreateNewResearchViewElementNoKeys(string def, string gUID, string name, string reveal, string unlock, string complete)

        {
            try
            {

                ResearchViewElementDef sourceResearchViewDef = Repo.GetAllDefs<ResearchViewElementDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_CorruptionNode_ViewElementDef"));
                ResearchViewElementDef researchViewDef = Helper.CreateDefFromClone(sourceResearchViewDef, gUID, def);
                researchViewDef.DisplayName1 = new LocalizedTextBind (name, true);
                researchViewDef.RevealText = new LocalizedTextBind(reveal, true);
                researchViewDef.UnlockText = new LocalizedTextBind(unlock, true);
                researchViewDef.CompleteText = new LocalizedTextBind(complete, true);
                return researchViewDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }



        public static CaptureActorResearchRequirementDef CreateNewTagCaptureActorResearchRequirementDef(string gUID, string defName, string revealText)
        {
            try
            {
                CaptureActorResearchRequirementDef captureActorResearchRequirementDef 
                    = Repo.GetAllDefs<CaptureActorResearchRequirementDef>().FirstOrDefault(ged => ged.name.Equals("PX_Alien_EvolvedAliens_ResearchDef_CaptureActorResearchRequirementDef_0"));
                CaptureActorResearchRequirementDef newCaptureActorResearchRequirementDef = Helper.CreateDefFromClone(captureActorResearchRequirementDef, gUID, defName);
                newCaptureActorResearchRequirementDef.RequirementText = new LocalizedTextBind (revealText, true);
                return newCaptureActorResearchRequirementDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        public static EncounterVariableResearchRequirementDef CreateNewEncounterVariableResearchRequirementDef(string nameDef, string gUID, string variable, int value)
        {
            try
            {
                EncounterVariableResearchRequirementDef sourceVarResReq =
                      Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                      FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                
                EncounterVariableResearchRequirementDef newResReq = Helper.CreateDefFromClone(sourceVarResReq, gUID, nameDef);
                newResReq.VariableName = variable;
                newResReq.Value = value;
                return newResReq;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();

        }

        [HarmonyPatch(typeof(GeoSite), "CreateHavenDefenseMission")]
        public static class GeoSite_CreateHavenDefenseMission_RevealHD_Patch
        {
            public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.HavenSOS;
            }

            public static void Postfix(GeoSite __instance)
            {
                try
                {
                    // if (__instance.GetVisible(__instance.GeoLevel.PhoenixFaction)==false)
                    // {
                    __instance.RevealSite(__instance.GeoLevel.PhoenixFaction);

                    GeoscapeLogEntry entry = new GeoscapeLogEntry
                    {
                        Text = new LocalizedTextBind(__instance.Owner + " " + __instance.LocalizedSiteName + " is broadcasting an SOS, they are under attack!", true)
                    };
                    typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance.GeoLevel.Log, new object[] { entry, null });

                    __instance.GeoLevel.View.SetGamePauseState(true);
                    //  }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

    }
}

