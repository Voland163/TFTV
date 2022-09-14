using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVUmbra
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static string variableUmbraALNResReq = "Umbra_Encounter_Variable";
        public static bool UmbraResearched = false;

        public static void ChangeUmbra()

        {
            try
            {
                //Need to change to take account of VoidOmen!!!:
                SetUmbraRandomValue(0);
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));
                //Changing Umbra Crab and Triton to appear after SDI event 3;
                ResearchDef umbraCrabResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_CrabmanUmbra_ResearchDef"));

                //Creating new Research Requirement, requiring a variable to be triggered  
                EncounterVariableResearchRequirementDef variableResReqUmbra = Helper.CreateDefFromClone(sourceVarResReq, "0CCC30E0-4DB1-44CD-9A60-C1C8F6588C8A", "UmbraResReqDef");
                variableResReqUmbra.VariableName = variableUmbraALNResReq;
                // This changes the Umbra reserach so that 2 conditions have to be fulfilled: 1) a) nest has to be researched, or b) exotic material has to be found
                // (because 1)a) is fufilled at start of the game, b)) is redundant but harmless), and 2) a special variable has to be triggered, assigned to event sdi3
                umbraCrabResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraCrabResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraCrabResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Now same thing for Triton Umbra, but it will use same variable because we want them to appear at the same time
                ResearchDef umbraFishResearch = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ALN_FishmanUmbra_ResearchDef"));
                umbraFishResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraFishResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraFishResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Because Triton research has 2 requirements in the second container, we set them to any
                umbraFishResearch.RevealRequirements.Container[1].Operation = ResearchContainerOperation.ANY;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void SetUmbraRandomValue(float value)
        {
            try
            {
                RandomValueEffectConditionDef randomValueFishUmbra = Repo.GetAllDefs<RandomValueEffectConditionDef>().
                FirstOrDefault(ged => ged.name.Equals("E_RandomValue [UmbralFishmen_FactionEffectDef]"));
                randomValueFishUmbra.ThresholdValue = value;
                RandomValueEffectConditionDef randomValueCrabUmbra = Repo.GetAllDefs<RandomValueEffectConditionDef>().
                FirstOrDefault(ged => ged.name.Equals("E_RandomValue [UmbralCrabmen_FactionEffectDef]"));
                randomValueCrabUmbra.ThresholdValue = value;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void AddArthronUmbraDeathBelcherAbility(TacticalActor tacticalActor)

        {
            try
            {
                AddAbilityStatusDef oilCrabAbility =
                      Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                      (ged => ged.name.Equals("OilCrab_AddAbilityStatusDef"));
                tacticalActor.Status.ApplyStatus(oilCrabAbility);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddTritonUmbraDeathBelcherAbility(TacticalActor tacticalActor)

        {
            try
            {
                AddAbilityStatusDef oilTritonAbility =
                     Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                     (ged => ged.name.Equals("OilFish_AddAbilityStatusDef"));
                tacticalActor.Status.ApplyStatus(oilTritonAbility);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void UmbraEvolution(int healthPoints, int standardDamageAttack, int pierceDamageAttack)
        {
            WeaponDef umbraCrab = Repo.GetAllDefs<WeaponDef>().
            FirstOrDefault(ged => ged.name.Equals("Oilcrab_Torso_BodyPartDef"));
            umbraCrab.HitPoints = healthPoints;
            umbraCrab.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
            umbraCrab.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;
            BodyPartAspectDef umbraCrabBodyAspect = Repo.GetAllDefs<BodyPartAspectDef>().
            FirstOrDefault(ged => ged.name.Equals("E_BodyPartAspect [Oilcrab_Torso_BodyPartDef]"));
            umbraCrabBodyAspect.Endurance = (healthPoints / 10);
            WeaponDef umbraFish = Repo.GetAllDefs<WeaponDef>().
            FirstOrDefault(ged => ged.name.Equals("Oilfish_Torso_BodyPartDef"));
            umbraFish.HitPoints = healthPoints;
            umbraFish.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
            umbraFish.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;
            BodyPartAspectDef umbraFishBodyAspect = Repo.GetAllDefs<BodyPartAspectDef>().
            FirstOrDefault(ged => ged.name.Equals("E_BodyPartAspect [Oilfish_Torso_BodyPartDef]"));
            umbraFishBodyAspect.Endurance = (healthPoints / 10);
        }

        public static void SetUmbraEvolution(GeoLevelController level)
        {

            if (level.EventSystem.GetVariable(variableUmbraALNResReq) == 2)
            {
                UmbraEvolution(125 * level.CurrentDifficultyLevel.Order, 20 * level.CurrentDifficultyLevel.Order, 20);
            }
            else if (level.EventSystem.GetVariable(variableUmbraALNResReq) == 1)
            {
                UmbraEvolution(80 * level.CurrentDifficultyLevel.Order, 20 * level.CurrentDifficultyLevel.Order, 0);
            }
        }

        public static int totalCharactersWithDelirium;
        public static int totalDeliriumOnMission;


        public static void SpawnUmbra(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    if (UmbraResearched)
                    {
                        if (!TFTVVoidOmens.VoidOmen16Active)
                        {
                            ClassTagDef crabTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                           (ged => ged.name.Equals("Crabman_ClassTagDef"));
                            ClassTagDef fishTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                           (ged => ged.name.Equals("Fishman_ClassTagDef"));

                            DeathBelcherAbilityDef oilcrabDeathBelcherAbility =
                           Repo.GetAllDefs<DeathBelcherAbilityDef>().FirstOrDefault
                           (ged => ged.name.Equals("Oilcrab_Die_DeathBelcher_AbilityDef"));
                            DeathBelcherAbilityDef oilfishDeathBelcherAbility =
                           Repo.GetAllDefs<DeathBelcherAbilityDef>().FirstOrDefault
                           (ged => ged.name.Equals("Oilfish_Die_DeathBelcher_AbilityDef"));

                            TacticalFaction phoenix = controller.GetFactionByCommandName("px");
                            TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                            totalCharactersWithDelirium = 0;
                            totalDeliriumOnMission = 0;

                            foreach (TacticalActor actor in phoenix.TacticalActors)
                            {
                                if (actor.CharacterStats.Corruption.Value > 0)
                                {
                                    totalCharactersWithDelirium++;
                                    totalDeliriumOnMission += (int)actor.CharacterStats.Corruption.Value.BaseValue;

                                }
                            }

                            TFTVLogger.Always("Total Delirium on mission is " + totalDeliriumOnMission);
                            TFTVLogger.Always("Number of characters with Delirium is " + totalCharactersWithDelirium);

                            foreach (TacticalActor actor in pandorans.TacticalActors)
                            {

                                TFTVLogger.Always("The actor is " + actor.name);
                                if (actor.GameTags.Contains(crabTag) && actor.GetAbilityWithDef<DeathBelcherAbility>(oilcrabDeathBelcherAbility) == null
                                    && !actor.name.Contains("Oilcrab") && !actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Revenant_GameTagDef"))))

                                {
                                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                                    int roll = UnityEngine.Random.Range(0, 100);
                                    if (TFTVVoidOmens.VoidOmen15Active && roll <= totalDeliriumOnMission)
                                    {
                                        TFTVLogger.Always("This Arthron here " + actor + ", got past the crabtag and the blecher ability check!");
                                        AddArthronUmbraDeathBelcherAbility(actor);
                                    }
                                    else if (!TFTVVoidOmens.VoidOmen15Active && roll <= totalDeliriumOnMission / 2)
                                    {
                                        TFTVLogger.Always("This Arthron here " + actor + ", got past the crabtag and the blecher ability check!");
                                        AddArthronUmbraDeathBelcherAbility(actor);
                                    }

                                }
                                if (actor.GameTags.Contains(fishTag) && actor.GetAbilityWithDef<DeathBelcherAbility>(oilfishDeathBelcherAbility) == null
                                    && !actor.name.Contains("Oilfish") && !actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Revenant_GameTagDef"))))
                                {
                                    UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                                    int roll = UnityEngine.Random.Range(0, 100);
                                    if (TFTVVoidOmens.VoidOmen15Active && roll <= totalDeliriumOnMission)
                                    {
                                        TFTVLogger.Always("This Triton here " + actor + ", got past the crabtag and the blecher ability check!");
                                        AddTritonUmbraDeathBelcherAbility(actor);
                                    }
                                    else if (!TFTVVoidOmens.VoidOmen15Active && roll <= totalDeliriumOnMission / 2)
                                    {
                                        TFTVLogger.Always("This Triton here " + actor + ", got past the crabtag and the blecher ability check!");
                                        AddTritonUmbraDeathBelcherAbility(actor);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    RandomValueEffectConditionDef randomValueCrabUmbra = Repo.GetAllDefs<RandomValueEffectConditionDef>().
                    FirstOrDefault(ged => ged.name.Equals("E_RandomValue [UmbralCrabmen_FactionEffectDef]"));
                    TFTVLogger.Always("The randon Crab Umbra value is " + randomValueCrabUmbra.ThresholdValue);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(Research), "CompleteResearch")]
        public static class Research_NewTurnEvent_CalculateDelirium_Patch
        {
            public static void Postfix(ResearchElement research)
            {
                try
                {
                    TFTVLogger.Always("Research completed " + research.ResearchID);

                    if (research.ResearchID == "ALN_CrabmanUmbra_ResearchDef")
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("UmbraResearched", 1);
                        TFTVLogger.Always("Umbra Researched variable is set to " + research.Faction.GeoLevel.EventSystem.GetVariable("UmbraResearched"));
                    }
                    else if (research.ResearchID == "ANU_AnuPriest_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 1)
                    {
                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);
                    }
                    else if (research.ResearchID == "NJ_Technician_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 2)
                    {
                        TFTVLogger.Always("Research completed " + research.ResearchID + " and corresponding flag triggered");
                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);
                    }
                    else if (research.ResearchID == "SYN_InfiltratorTech_ResearchDef" && research.Faction.GeoLevel.EventSystem.GetVariable("BG_Start_Faction") == 3)
                    {
                        research.Faction.GeoLevel.PhoenixFaction.Research.GiveResearch(research, true);
                    }
                    //To trigger change of rate in Pandoran Evolution
                    else if (research.ResearchID == "ALN_Citadel_ResearchDef") 
                    {
                        research.Faction.GeoLevel.EventSystem.SetVariable("Pandorans_Researched_Citadel", 1);
                        research.Faction.GeoLevel.AlienFaction.SpawnNewAlienBase();
                        GeoAlienBase citadel = research.Faction.GeoLevel.AlienFaction.Bases.FirstOrDefault(ab => ab.AlienBaseTypeDef.name == "Citadel_GeoAlienBaseTypeDef");
                        citadel.SpawnMonster(Repo.GetAllDefs<ClassTagDef>().FirstOrDefault(ctf => ctf.name.Equals("Queen_ClassTagDef")), true);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        public static void CheckForUmbraResearch(GeoLevelController level)
        {
            try
            {
                if (level.EventSystem.GetVariable("UmbraResearched") == 1)
                {
                    UmbraResearched = true;

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Patch to prevent Umbras from attacking characters without Delirium
        [HarmonyPatch(typeof(TacticalAbility), "GetTargetActors", new Type[] { typeof(TacticalTargetData), typeof(TacticalActorBase), typeof(Vector3) })]
        public static class TacticalAbility_GetTargetActors_Patch
        {
            public static void Postfix(ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor)
            {
                try
                {
                    if (!TFTVVoidOmens.VoidOmen16Active)
                    {
                        if (sourceActor.ActorDef.name.Equals("Oilcrab_ActorDef") || sourceActor.ActorDef.name.Equals("Oilfish_ActorDef"))
                        {
                            List<TacticalAbilityTarget> list = new List<TacticalAbilityTarget>(); // = __result.ToList();
                                                                                                  //list.RemoveWhere(adilityTarget => (adilityTarget.Actor as TacticalActor)?.CharacterStats.Corruption <= 0);
                            foreach (TacticalAbilityTarget source in __result)
                            {
                                if (source.Actor is TacticalActor && (source.Actor as TacticalActor).CharacterStats.Corruption > 0)
                                {
                                    list.Add(source);
                                }
                            }
                            __result = list;
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

}

