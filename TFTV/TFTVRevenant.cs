using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
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
                CreateRevenantAbilityForAssault();
                CreateRevenantAbilityForBerserker();
                CreateRevenantAbilityForHeavy();
                CreateRevenantAbilityForInfiltrator();
                CreateRevenantAbilityForPriest();
                CreateRevenantAbilityForSniper();
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

                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.AlienFactionDef && DeadSoldiersDelirium.Count > 0 && RevenantCounter < 3
                        && !actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Revenant_GameTagDef"))))
                    {

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
                            TFTVLogger.Always((timeOfMissionStart - timeUnit).TimeSpan.Days.ToString());
                            if ((timeOfMissionStart - timeUnit).TimeSpan.Days >= 3)
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
                            else if (delirium < 3) //&& delirium >= 1 for testing
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
                            //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                            SetDeathTime(theChosen, __instance, timeOfMissionStart);
                            TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                            SetRevenantClassAbility(theChosen, __instance, tacticalActor);
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
                        string additionalDescription =
                            GetDescriptionOfRevenantClassAbility(actorName, __instance.TacticalActor.TacticalLevel);
                        __result = "This is your fallen comrade, " + actorName + ", returned as Pandoran monstrosity." + additionalDescription;

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
                //TFTVLogger.Always("the name is " + name);
                List<SoldierStats> deadSoldiersNames = level.TacticalGameParams.Statistics.DeadSoldiers.Values.Where(s => s.Name == name).ToList();
                SoldierStats deadSoldier = deadSoldiersNames.FirstOrDefault();
                //TFTVLogger.Always("the name of the soldier extracted from death list is " + deadSoldier.Name);
                List<GeoTacUnitId> deadSoldiersList = level.TacticalGameParams.Statistics.DeadSoldiers.Keys.ToList();

                foreach (GeoTacUnitId soldier in deadSoldiersList)
                {
                    if (level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(soldier, out deadSoldier))
                    {
                       // TFTVLogger.Always("The soldier is " + soldier);
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

        public static string GetDescriptionOfRevenantClassAbility(string name, TacticalLevelController level)
        {
            try
            {
                List<SoldierStats> deadSoldierStatsList = level.TacticalGameParams.Statistics.DeadSoldiers.Values.ToList();
                SoldierStats deadSoldierStats = new SoldierStats();

                foreach (SoldierStats soldierStats in deadSoldierStatsList)
                {
                    if (soldierStats.Name == name)
                    {
                        deadSoldierStats = soldierStats;
                    }
                }


                List<SpecializationDef> specializations = deadSoldierStats.ClassesSpecialized.ToList();

                string description = "";

                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("AssaultSpecializationDef"))))
                {
                    description += " Increased damage potential, speed and aggressiveness";
                }
                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("BerserkerSpecializationDef"))))
                {
                    description += " Fearless, fast, unstoppable...";
                }
                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("HeavySpecializationDef"))))
                {
                    description += " Bullet sponge";
                }
                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("InfiltratorSpecializationDef"))))
                {
                    description += " Scary quiet";
                }
                /*  if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("PriestSpecializationDef")))
                  {

                  }
                  if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("SniperSpecializationDef")))
                  {


                  }
                  else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("TechnicianSpecializationDef")))
                  {

                  }*/

                return description;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }
  
        public static void SetRevenantClassAbility(GeoTacUnitId deadSoldier, TacticalLevelController level, TacticalActor tacticalActor)
        {
            try
            {

                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;

                List<SpecializationDef> specializations = deadSoldierStats.ClassesSpecialized.ToList();

                foreach (SpecializationDef specialization in specializations)
                {
                    AddRevenantClassAbility(tacticalActor, specialization);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
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

        public static void CreateRevenantAbilityForAssault()
        {
            try
            {

                string skillName = "RevenantAssault_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "1045EB8D-1916-428F-92EF-A15FD2807818",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "7FF5A3CF-6BBD-4E4F-9E80-2DB7BDB29112",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "47BE3577-1D68-4FB2-BFA3-0A158FC710D9",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.25f},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Assault Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+25% Damage", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForBerserker()
        {
            try
            {

                string skillName = "RevenantBerserker_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "FD3FE516-25BA-44F2-9770-3AA4AD1DCB91",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "E2707CBD-3D99-4EA4-A48D-B8E6E14EFDFD",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3F74FAF1-1A87-4E2A-AEC2-CBB0BA5A14E0",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 5},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Berserker Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+5 Speed", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForHeavy()
        {
            try
            {

                string skillName = "RevenantHeavy_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "A8603522-3472-4A95-9ADF-F27E8B287D15",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "AA5F572B-D86B-4C00-B8B9-4D86EE5F7F4D",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "F8781E78-D106-44B3-A0E6-855BCAEB0A2F",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 100},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Heavy Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+10 Strength", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForInfiltrator()
        {
            try
            {

                string skillName = "RevenantInfiltrator_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "6C56E0F9-56BB-41D2-AFB1-08C8A49F69FA",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1F8B6D09-A2C5-4B3F-BBED-F59675301ABB",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6CAFD922-60C6-449E-A652-C2BD94386BE5",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.MultiplyMax, Value = 1.5f},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Infiltrator Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+50% Stealth", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForPriest()
        {
            try
            {

                string skillName = "RevenantPriest_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "0816E671-D396-4212-910F-87B5DEC6ADE2",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C1C7FBEA-2C0B-4930-A73C-15BF3A987784",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "460AAE12-0541-40AB-A4EE-E3E206A96FB4",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbilityForSniper()
        {
            try
            {

                string skillName = "RevenantSniper_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "4A2C53A3-D9DB-456A-8B88-AB2D90BE1DB5",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0D811905-8C70-4D46-9CF2-1A31C5E98ED1",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "7DCCCAAA-7245-4245-9033-F6320CCDA2AB",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 15},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Needs a different thing, actually
        public static void CreateRevenantAbilityForTechnician()
        {
            try
            {

                string skillName = "RevenantTechnician_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "04A284AC-545A-455F-8843-54056D68022E",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1A995634-EE80-4E72-A10F-F8389E8AEB50",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "19B35512-5C23-4046-B10D-2052CDEFB769",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Technician Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void AddRevenantClassAbility(TacticalActor tacticalActor, SpecializationDef specialization)

        {
            try
            {
                if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("AssaultSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Assault specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantAssault_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Pitcher_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Mutog_PrimalInstinct_AbilityDef")), tacticalActor);

                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("BerserkerSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Berserker specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantBerserker_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("BloodLust_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("IgnorePain_AbilityDef")), tacticalActor);
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("HeavySpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Heavy specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantHeavy_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Skirmisher_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("ShredResistant_DamageMultiplierAbilityDef")), tacticalActor);
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("InfiltratorSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Infiltrator specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantInfiltrator_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("WeakSpot_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("SurpriseAttack_AbilityDef")), tacticalActor);

                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("PriestSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Priest specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantPriest_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("MindSense_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("PsychicWard_AbilityDef")), tacticalActor);
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("SniperSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Sniper specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantSniper_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("OverwatchFocus_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("MasterMarksman_AbilityDef")), tacticalActor);

                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("TechnicianSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Technician specialization");
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantTechnician_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Stability_AbilityDef")), tacticalActor);
                    tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("AmplifyPain_AbilityDef")), tacticalActor);
                }


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


