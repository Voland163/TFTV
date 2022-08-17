using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<string, int> DeadSoldiersDelirium = new Dictionary<string, int>();
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();
        public static TimeUnit timeOfMissionStart = 0;
        public static int RevenantCounter = 0;

        public static void CreateRevenantDefs()
        {
            try
            {
                CreateRevenantAbility();
                CreateRevenantStatusEffect();
                CreateRevenantGameTag();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_DeathRipper_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {

                    if (__instance.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId) 
                        && !__instance.TacticalGameParams.Statistics.DeadSoldiers.ContainsKey(deathReport.Actor.GeoUnitId) 
                        && !DeadSoldiersDelirium.ContainsKey(deathReport.Actor.DisplayName))
                    {
                        AddtoListOfDeadSoldiers(deathReport.Actor);
                        TFTVStamina.charactersWithBrokenLimbs.Remove(deathReport.Actor.GeoUnitId);
                        TFTVLogger.Always(deathReport.Actor.DisplayName + " died at" + timeOfMissionStart.DateTime.ToString() +
                            ". The deathlist now has " + DeadSoldiersDelirium.Count);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_RevenantGenerator_Patch
        {
            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {

                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.AlienFactionDef && DeadSoldiersDelirium.Count > 0 && RevenantCounter < 3)
                    {
                        TFTVLogger.Always("ActorEnteredPlay first if passed");

                        //First lets check time of death to create a first list of dead soldiers
                        List<GeoTacUnitId> allDeadSoldiers = __instance.TacticalGameParams.Statistics.DeadSoldiers.Keys.ToList();

                        //This list is after first crib re time they have been dead
                        List<GeoTacUnitId> deadLongEnoughSoldiers = new List<GeoTacUnitId>();

                        //These are class specific eligibility lists

                        List<GeoTacUnitId> eligibleForScylla = new List<GeoTacUnitId>();
                        List<GeoTacUnitId> eligibleForAcheron = new List<GeoTacUnitId>();
                        List<GeoTacUnitId> eligibleForChiron = new List<GeoTacUnitId>();
                        List<GeoTacUnitId> eligibleForSiren = new List<GeoTacUnitId>();
                        List<GeoTacUnitId> eligibleForTriton = new List<GeoTacUnitId>();
                        List<GeoTacUnitId> eligibleForArthron = new List<GeoTacUnitId>();

                        //first cribing
                        foreach (GeoTacUnitId candidate in allDeadSoldiers)
                        {
                            TimeUnit timeUnit = CheckTimerFromDeath(candidate, __instance);
                            TFTVLogger.Always("The time unit when character died is " + timeUnit.DateTime.ToString());
                            TFTVLogger.Always("Current time is " + timeOfMissionStart.DateTime.ToString());
                            TFTVLogger.Always((timeOfMissionStart.DateTime - timeUnit.DateTime).TotalHours.ToString());
                            if ((timeOfMissionStart.DateTime - timeUnit.DateTime).TotalHours >= 10)
                            {
                                deadLongEnoughSoldiers.Add(candidate);
                            }
                        }

                        //second class-specific eligibility cribing
                        foreach (GeoTacUnitId candidate in deadLongEnoughSoldiers)
                        {
                            int delirium = CheckDeliriumAtDeath(candidate, __instance);

                            if (delirium >= 10)
                            {
                                eligibleForScylla.Add(candidate);
                            }
                            else if (delirium == 9)
                            {
                                eligibleForAcheron.Add(candidate);
                            }
                            else if (delirium == 8)
                            {
                                eligibleForChiron.Add(candidate);
                            }
                            else if (delirium < 8 && delirium >= 6)
                            {
                                eligibleForSiren.Add(candidate);
                            }
                            else if (delirium < 6 && delirium >= 3)
                            {
                                eligibleForTriton.Add(candidate);
                            }
                            else if (delirium < 3) //&& delirium >= 2) for testing
                            {
                                eligibleForArthron.Add(candidate);
                            }
                        }


                        ClassTagDef crabTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Crabman_ClassTagDef"));
                        ClassTagDef fishmanTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Fishman_ClassTagDef"));
                        ClassTagDef sirenTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Siren_ClassTagDef"));
                        ClassTagDef chironTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Chiron_ClassTagDef"));
                        ClassTagDef acheronTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Acheron_ClassTagDef"));
                        ClassTagDef queenTag = Repo.GetAllDefs<ClassTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Queen_ClassTagDef"));
                      
                        if (actor.GameTags.Contains(crabTag) && eligibleForArthron.Count > 0 && !CheckForActorWithTag(crabTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForArthron.First();
                            TFTVLogger.Always("Here is an eligible crab: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                            TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }

                        if (actor.GameTags.Contains(fishmanTag) && eligibleForTriton.Count > 0 && !CheckForActorWithTag(fishmanTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForTriton.First();
                            TFTVLogger.Always("Here is an eligible fishman: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }

                        if (actor.GameTags.Contains(sirenTag) && eligibleForSiren.Count > 0 && !CheckForActorWithTag(sirenTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForSiren.First();
                            TFTVLogger.Always("Here is an eligible Siren: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }
                        if (actor.GameTags.Contains(chironTag) && eligibleForChiron.Count > 0 && !CheckForActorWithTag(chironTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForChiron.First();
                            TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }
                        if (actor.GameTags.Contains(acheronTag) && eligibleForAcheron.Count > 0 && !CheckForActorWithTag(acheronTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForAcheron.First();
                            TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }
                        if (actor.GameTags.Contains(queenTag) && eligibleForScylla.Count > 0 && !CheckForActorWithTag(queenTag, __instance))
                        {
                            GeoTacUnitId theChosen = eligibleForScylla.First();
                            TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                            TacticalActor tacticalActor = actor as TacticalActor;
                            AddRevenantStatusEffect(tacticalActor);
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            RevenantCounter++;
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalAbility), "GetAbilityDescription")]

        public static class TacticalAbility_DisplayCategory_ChangeDescriptionRevenantSkill_patch
        {

            public static void Postfix(TacticalAbility __instance, ref string __result)
            {
                try
                {
                    if (__instance.AbilityDef == Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(ged => ged.name.Equals("Revenant_AbilityDef")))
                    {
                        string actorName = __instance.TacticalActor.name;
                        __result = "This is your fallen comrade, " + actorName + ", returned as Pandoran monstrosity";

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
                    GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault
                            (ged => ged.name.Equals("Revenant_GameTagDef"));

                    if (__instance.GameTags.Contains(revenantTag))
                    {

                        string name = __instance.name + " Revenant";
                        __result = name;

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        public static bool CheckForActorWithTag(ClassTagDef classTagDef, TacticalLevelController level)
        {
            try
            {
                TacticalFaction alienFaction = level.GetTacticalFaction(sharedData.AlienFactionDef);
                GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Revenant_GameTagDef"));

                foreach (TacticalActor actor in alienFaction.TacticalActors)
                {
                    if (actor.tag.Contains(classTagDef.name) && actor.GameTags.Contains(revenantTag))
                    {
                        return true;

                    }
                }
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

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


        public static string GetDeadSoldiersNameFromID(GeoTacUnitId deadSoldier, TacticalLevelController level)
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

        public static GeoTacUnitId GetDeadSoldiersIdFromName(string name, TacticalLevelController level)
        {
            try
            {
                SoldierStats deadSoldier = level.TacticalGameParams.Statistics.DeadSoldiers.Values.Where(s => s.Name == name).First();

                List<GeoTacUnitId> deadSoldiersList = level.TacticalGameParams.Statistics.DeadSoldiers.Keys.ToList();

                foreach (GeoTacUnitId soldier in deadSoldiersList)
                {
                    if (level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(soldier, out deadSoldier))
                    {
                        return soldier;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();



        }




        public static int CheckDeliriumAtDeath(GeoTacUnitId deadSoldier, TacticalLevelController level)
        {
            try
            {

                int Delirium = DeadSoldiersDelirium.TryGetValue(GetDeadSoldiersNameFromID(deadSoldier, level), out Delirium) ? Delirium : 0;
                return Delirium;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void CreateRevenantGameTag()
        {
            string skillName = "Revenant_GameTagDef";
            GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
            GameTagDef Takashi = Helper.CreateDefFromClone(
                source,
                "1677F9F4-5B45-47FA-A119-83A76EF0EC70",
                skillName);
        }


        public static void AddtoListOfDeadSoldiers(TacticalActorBase deadSoldier)//, TacticalLevelController level)

        {

            try
            {

                TacticalActor tacticalActor = (TacticalActor)deadSoldier;
                int delirium = tacticalActor.CharacterStats.Corruption.IntValue;
                if (delirium > 0)
                {
                    DeadSoldiersDelirium.Add(deadSoldier.DisplayName, delirium);
                }
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

        public static void CreateRevenantAbility()
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
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("Nothing because fail", true);
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
                GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Revenant_GameTagDef"));
                tacticalActor.GameTags.Add((revenantTag), GameTagAddMode.ReplaceExistingExclusive);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

       

        }
    }


