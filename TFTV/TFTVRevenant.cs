using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<int, int> DeadSoldiersDelirium = new Dictionary<int, int>();
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();
        public static bool flag = false;
        public static TacticalActorBase actorRevenant = null;
        public static TimeUnit timeOfMissionStart = 0;

        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_DeathRipper_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {

                    if (__instance.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId))
                    {

                        AddtoListOfDeadSoldiers(deathReport.Actor);
                        TFTVStamina.charactersWithBrokenLimbs.Remove(deathReport.Actor.GeoUnitId);
                        TFTVLogger.Always("The deathlist now has " + DeadSoldiersDelirium.Count);

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

/*
        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_RevenantGenerator_Patch
        {
            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {
                    TFTVLogger.Always("Count of dead soldiers in list " + DeadSoldiersDelirium.Count);

                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.AlienFactionDef) //&& DeadSoldiersDelirium.Count > 0)
                    {
                        //  if (DeadSoldiersDelirium.Values.Contains(1)) 
                        //  {

                              List<GeoTacUnitId> candidateList = __instance.TacticalGameParams.Statistics.DeadSoldiers.Keys.ToList();
                              foreach(GeoTacUnitId candidate in candidateList) 
                              {
                                  TimeUnit timeUnit = CheckTimerFromDeath(candidate, __instance);


                                    

                                  TFTVLogger.Always("The time unit when character died is " + timeUnit.DateTime.ToString());
                                  TFTVLogger.Always("Current time is " + timeOfMissionStart.DateTime.ToString());
                                  TFTVLogger.Always((timeOfMissionStart.DateTime - timeUnit.DateTime).TotalHours.ToString());
                            SetDeathTime(candidate, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(candidate, __instance).DateTime.ToString());


                            //  if (timeUnit.TimeSpan.CompareTo(__instance.GameTiming.Now)>1)




                        }

                         

                        ClassTagDef crabTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Crabman_ClassTagDef"));
                        if (actor.GameTags.Contains(crabTag))
                        {

                            if (!flag)
                            {
                                CreateRevenantAbility(GetDeadSoldiersName(__instance.TacticalGameParams.Statistics.DeadSoldiers.Keys.First(), __instance));
                                CreateRevenantStatusEffect();
                                flag = true;

                            }
                            
                            TFTVLogger.Always("Here is an eligible crab: " + actor.GetDisplayName());
                            actorRevenant = actor;
                            TacticalActor tacticalActor = actor as TacticalActor;

                            AddRevenantStatusEffect(tacticalActor);
                            //   typeof(TacticalActorBase).GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] {  });
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(TacticalActorBase), "get_DisplayName")]
        public static class TacticalActorBase_GetDisplayName_RevenantGenerator_Patch
        {
            public static void Postfix(TacticalActorBase __instance, ref string __result)

            {
                try

                {

                    ClassTagDef crabTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Crabman_ClassTagDef"));


                    if (__instance.GameTags.Contains(crabTag))
                    {

                        string name = GetDeadSoldiersName(__instance.TacticalLevel.TacticalGameParams.Statistics.DeadSoldiers.Keys.First(), __instance.TacticalLevel) + " Revenant";
                        __result = name;

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }
*/
        public static void SetDeathTime(GeoTacUnitId deadSoldier, TacticalLevelController level, TimeUnit currentTime)
        {
            try
            {
                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;
                deadSoldierStats.DeathCause.DateOfDeath = currentTime;
               

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static TimeUnit CheckTimerFromDeath(GeoTacUnitId deadSoldier, TacticalLevelController level)
        {
            try
            {
                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;
                return deadSoldierStats.DeathCause.DateOfDeath;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static string GetDeadSoldiersName(GeoTacUnitId deadSoldier, TacticalLevelController level)
        {
            try
            {

                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;
                return deadSoldierStats.Name;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }




        public static int CheckDeliriumAtDeath(TacticalActorBase deadSoldier, TacticalLevelController level)
        {
            try
            {

                int Delirium = DeadSoldiersDelirium.TryGetValue(deadSoldier.GeoUnitId, out Delirium) ? Delirium : 0;
                return Delirium;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
        /*
                public static List<GeoTacUnitId> CheckDeadSoldiersWithDelirium(int delirium)
                {
                    try
                    {
                        List<int> AllDeadSoldiers = DeadSoldiersDelirium.Keys.ToList();
                        List<GeoTacUnitId> geoTacUnitIds = AllDeadSoldiers as List<int>;
                        List<GeoTacUnitId> EligibleDeadSoldiers = new List<GeoTacUnitId>();
                        foreach(GeoTacUnitId soldier in AllDeadSoldiers) 
                        {

                            if (DeadSoldiersDelirium.TryGetValue(soldier, out delirium)) 
                            { 
                            EligibleDeadSoldiers.Add(soldier);

                            }

                        }

                        return EligibleDeadSoldiers;


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                    throw new InvalidOperationException();
                }

                */
        public static void AddtoListOfDeadSoldiers(TacticalActorBase deadSoldier)//, TacticalLevelController level)

        {

            try
            {

                TacticalActor tacticalActor = (TacticalActor)deadSoldier;
                int delirium = tacticalActor.CharacterStats.Corruption.IntValue;
                DeadSoldiersDelirium.Add(deadSoldier.GeoUnitId, delirium);

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateRevenantStatusEffect()
        {
            try
            {
                AddAbilityStatusDef sourceAbilityStatusDef =
                     Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                     (ged => ged.name.Equals("OilCrab_AddAbilityStatusDef"));
                PassiveModifierAbilityDef Revenant_Ability = 
                    Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Revenant_AbilityDef"));
                AddAbilityStatusDef newAbilityStatusDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "68EE5958-D977-4BD4-9018-CAE03C5A6579", "Revenant_StatusEffectDef");
                newAbilityStatusDef.AbilityDef = Revenant_Ability;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    public static void CreateRevenantAbility(string name)
        {
            try
            {

                string skillName = "Revenant_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "8A62302E-9C2D-4AFA-AFF3-2F526BF82252",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "FECD4DD8-5E1A-4A0F-BC3A-C2F0AA30E41F",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "75B1017A-0455-4B44-91F0-3E1446899B42",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[0];
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("This is your fallen comrade, " + name + ", returned as Pandoran monstrosity", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddRevenantStatusEffect(TacticalActor tacticalActor)

        {
            try
            {
                AddAbilityStatusDef revenantAbility =
                     Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                     (ged => ged.name.Equals("Revenant_StatusEffectDef"));
                tacticalActor.Status.ApplyStatus(revenantAbility);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /*
        public static void AddRevenantAbility(TacticalActor tacticalActor)

        {
            try
            {
                AbilityDef revenantAbility =
                     Repo.GetAllDefs<AbilityDef>().FirstOrDefault
                     (ged => ged.name.Equals("Revenant_AbilityDef"));
                tacticalActor.AddAbility(revenantAbility, tacticalActor);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }*/

        /*
        public static void CreateRevenantTemplate(string tacCharacterDef, string name)
        {
            try
            {
                TacCharacterDef templateDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(gvw => gvw.name.Equals(tacCharacterDef));
                templateDef.Data.LocalizeName = false;
                templateDef.Data.Name = name + " Revenant";
                PassiveModifierAbilityDef Revenant_Ability = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Revenant_AbilityDef"));

                //  templateDef.(Revenant_Ability);



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }*/

    }

}

