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

      //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        public static string variableUmbraALNResReq = "Umbra_Encounter_Variable";
        public static bool UmbraResearched = false;
        public static RandomValueEffectConditionDef randomValueFishUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralFishmen_FactionEffectDef]");
        public static RandomValueEffectConditionDef randomValueCrabUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralCrabmen_FactionEffectDef]");
        private static readonly AddAbilityStatusDef oilCrabAbility = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
        private static readonly AddAbilityStatusDef oilTritonAbility = DefCache.GetDef<AddAbilityStatusDef>("OilFish_AddAbilityStatusDef");

        public static WeaponDef umbraCrab = DefCache.GetDef<WeaponDef>("Oilcrab_Torso_BodyPartDef");

        public static BodyPartAspectDef umbraCrabBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Oilcrab_Torso_BodyPartDef]");

        public static WeaponDef umbraFish = DefCache.GetDef<WeaponDef>("Oilfish_Torso_BodyPartDef");

        public static BodyPartAspectDef umbraFishBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Oilfish_Torso_BodyPartDef]");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

        private static readonly DeathBelcherAbilityDef oilcrabDeathBelcherAbility = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef");
        private static readonly  DeathBelcherAbilityDef oilfishDeathBelcherAbility = DefCache.GetDef<DeathBelcherAbilityDef>("Oilfish_Die_DeathBelcher_AbilityDef");

        private static readonly GameTagDef anyRevenantGameTag = DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");
        
        

        public static void SetUmbraRandomValue(float value)
        {
            try
            {

                randomValueFishUmbra.ThresholdValue = value;

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
                
                tacticalActor.Status.ApplyStatus(oilTritonAbility);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void UmbraEvolution(int healthPoints, int standardDamageAttack, int pierceDamageAttack)
        {
            
            umbraCrab.HitPoints = healthPoints;
            umbraCrab.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
            umbraCrab.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;
            
            umbraCrabBodyAspect.Endurance = (healthPoints / 10);
           
            umbraFish.HitPoints = healthPoints;
            umbraFish.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
            umbraFish.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;          
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
                                    && !actor.name.Contains("Oilcrab") && !actor.GameTags.Contains(anyRevenantGameTag))

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
                                    && !actor.name.Contains("Oilfish") && !actor.GameTags.Contains(anyRevenantGameTag))
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
                   
                    TFTVLogger.Always("The random Crab Umbra value is " + randomValueCrabUmbra.ThresholdValue);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
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

