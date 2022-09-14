using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.ParticleSystems;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
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
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<string, int> DeadSoldiersDelirium = new Dictionary<string, int>();
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();
        public static int daysRevenantLastSeen = 0;
        // public static  timeLastRevenantSpawned = new TimeUnit();
        public static bool revenantCanSpawn = false;
        public static bool revenantSpawned = false;
        public static List<string> revenantSpecialResistance = new List<string>();

        public static void CreateRevenantDefs()
        {
            try
            {
                TFTVRevenantDefs.CreateRevenantAbility();
                TFTVRevenantDefs.CreateRevenantStatusEffect();
                TFTVRevenantDefs.CreateRevenantGameTags();
                TFTVRevenantDefs.CreateRevenantAbilityForAssault();
                TFTVRevenantDefs.CreateRevenantAbilityForBerserker();
                TFTVRevenantDefs.CreateRevenantAbilityForHeavy();
                TFTVRevenantDefs.CreateRevenantAbilityForInfiltrator();
                TFTVRevenantDefs.CreateRevenantAbilityForPriest();
                TFTVRevenantDefs.CreateRevenantAbilityForSniper();
                TFTVRevenantDefs.CreateRevenantAbilityForTechnician();
                TFTVRevenantDefs.CreateRevenantResistanceAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CheckRevenantTime(GeoLevelController controller)

        {
            try
            {
                TFTVLogger.Always("Last time a Revenant was seen was on day " + daysRevenantLastSeen + ", and now it is day " + controller.Timing.Now.TimeSpan.Days);
                if (DeadSoldiersDelirium.Count > 0 && (daysRevenantLastSeen == 0 || controller.Timing.Now.TimeSpan.Days - daysRevenantLastSeen >= 3)) //+ UnityEngine.Random.Range(-1, 3))) 
                { 
                   revenantCanSpawn = true;
                   TFTVLogger.Always("Therefore, a Revenant can spawn is " + revenantCanSpawn);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void UpdateRevenantTimer(GeoLevelController controller) 
        {
            try 
            {
                if (revenantSpawned) 
                { 
                daysRevenantLastSeen = controller.Timing.Now.TimeSpan.Days;
                controller.EventSystem.SetVariable("Revenant_Spotted", controller.EventSystem.GetVariable("Revenant_Spotted")+1);
                revenantSpawned = false;
                TFTVLogger.Always("Last time a Revenant was seen was on day " + daysRevenantLastSeen + ", and now it is day " + controller.Timing.Now.TimeSpan.Days);
                TFTVLogger.Always("# Revenants spotted " + controller.EventSystem.GetVariable("Revenant_Spotted"));
                }        
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void RevenantCheckAndSpawn(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    if (!revenantSpawned && revenantCanSpawn) //&& (timeLastRevenantSpawned == 0 || )
                    {
                        TFTVLogger.Always("RevenantCheckAndSpawn invoked");
                        TryToSpawnRevenant(controller);

                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /*        public static void RevenantResistanceCheck(TacticalLevelController controller)
                {
                    try
                    {

                        List<TacticalActorBase> pandorans = controller.GetFactionByCommandName("aln").Actors.ToList();
                        bool revenantPresent = false;

                        foreach (TacticalActorBase actor in pandorans)
                        {
                            if (actor.HasGameTag(TFTVMain.Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant"))))
                            {
                                revenantPresent = true;
                                TFTVLogger.Always("On new turn, revenant is found");
                            }
                        }

                        if (revenantPresent == true)
                        {


                            foreach (TacticalActorBase actor in pandorans)
                            {
                                if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(actor.GeoUnitId)
                                     && !actor.GameTags.Contains(TFTVMain.Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant")))
                                     && actor.GetAbilityWithDef<DamageMultiplierAbility>(TFTVMain.Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("RevenantResistance_AbilityDef"))) == null)

                                    AddRevenantResistanceAbility(actor);
                                    TFTVLogger.Always(actor.name + " received the revenant resistance ability.");

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }*/

        public static GeoTacUnitId RevenantRoll(TacticalLevelController controller)
        {
            try
            {
               
                List<string> allDeadSoldiers = DeadSoldiersDelirium.Keys.ToList();
                List<GeoTacUnitId> candidates = new List<GeoTacUnitId>();

                foreach (string deadSoldier in allDeadSoldiers)
                {                    
                    candidates.Add(GetDeadSoldiersIdFromName(deadSoldier, controller));
                    TFTVLogger.Always("deadSoldier " + deadSoldier + " with GeoTacUnitId " 
                        + GetDeadSoldiersIdFromName(deadSoldier, controller) +  "is added to the list of Revenant candidates");
                }
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                int roll = UnityEngine.Random.Range(0, candidates.Count());
                TFTVLogger.Always("The total number of candidates is " + candidates.Count() + " and the roll is " + roll);

                GeoTacUnitId theChosen = candidates[roll];
                TFTVLogger.Always("The Chosen is " + GetDeadSoldiersNameFromID(theChosen, controller));
                return theChosen;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static ClassTagDef GetRevenantClassTag(TacticalLevelController controller, GeoTacUnitId theChosen)
        {
            try
            {
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

                int delirium = DeadSoldiersDelirium[GetDeadSoldiersNameFromID(theChosen, controller)];
                TFTVLogger.Always("The Chosen, " + GetDeadSoldiersNameFromID(theChosen, controller) + ", has " + delirium + " Delirium");
                if (delirium >= 10)
                {
                    return queenTag;
                }
                else if (delirium == 9)
                {
                    return acheronTag;
                }
                else if (delirium == 8)
                {
                    return chironTag;
                }
                else if (delirium < 8 && delirium >= 6)
                {
                    return sirenTag;
                }
                else if (delirium < 6 && delirium >= 3)
                {
                    return fishmanTag;
                }
                else //&& delirium >= 1 for testing
                {
                    return crabTag;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }

        public static void TryToSpawnRevenant(TacticalLevelController controller)

        {
            try
            {

                if (controller.GetFactionByCommandName("aln") != null)
                {
                    TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                    GeoTacUnitId theChosen = RevenantRoll(controller);
                    ClassTagDef classTagDef = GetRevenantClassTag(controller, theChosen);
                    List<TacticalActorBase> candidates = new List<TacticalActorBase>();
                    TacticalActorBase actor = new TacticalActorBase();

                    foreach (TacticalActorBase tacticalActorBase in pandorans.Actors.Where(tab => tab.GameTags.Contains(classTagDef)))
                    {
                        candidates.Add(tacticalActorBase);
                    }

                    if (candidates.Count > 0)
                    {
                        actor = candidates.First();
                    }
                    else
                    {
                        return;
                    }

                    TFTVLogger.Always("Here is an eligible Pandoran to be a Revenant: " + actor.GetDisplayName());
                    TacticalActor tacticalActor = actor as TacticalActor;
                    AddRevenantStatusEffect(actor);
                    SetRevenantTierTag(theChosen, actor, controller);
                    actor.name = GetDeadSoldiersNameFromID(theChosen, controller);
                    //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                    // SetDeathTime(theChosen, __instance, timeOfMissionStart);
                    DeadSoldiersDelirium[GetDeadSoldiersNameFromID(theChosen, controller)] += 1;
                    TFTVLogger.Always("The accumulated delirium for  " + GetDeadSoldiersNameFromID(theChosen, controller) 
                        + " is now " + DeadSoldiersDelirium[GetDeadSoldiersNameFromID(theChosen, controller)]);
                    SetRevenantClassAbility(theChosen, controller, tacticalActor);
                    AddRevenantResistanceAbility(actor);
                    //  SpreadResistance(__instance);
                    actor.UpdateStats();
                    revenantCanSpawn = false;

                    foreach (TacticalActorBase pandoran in pandorans.Actors)
                    {
                        if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(pandoran.GeoUnitId)
                             && !pandoran.GameTags.Contains(TFTVMain.Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant")))
                             && pandoran.GetAbilityWithDef<DamageMultiplierAbility>(TFTVMain.Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("RevenantResistance_AbilityDef"))) == null)

                            AddRevenantResistanceAbility(pandoran);
                        TFTVLogger.Always(pandoran.name + " received the revenant resistance ability.");
                    }
                    //   GameTagDef anyRevenantGameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Any_Revenant_TagDef"));
                    //   GameTagDef revenantTier1GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_1_GameTagDef"));

                    // TFTVLogger.Always("Actor has tag any revenant? " + actor.HasGameTag(anyRevenantGameTag));
                    // TFTVLogger.Always("Actor has tag tier 1 revenant? " + actor.HasGameTag(revenantTier1GameTag));
                }
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
                        TFTVLogger.Always(deathReport.Actor.DisplayName + " died at. The deathlist now has " + DeadSoldiersDelirium.Count);
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
                    GameTagDef revenantTier1GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_1_GameTagDef"));
                    GameTagDef revenantTier2GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"));
                    GameTagDef revenantTier3GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"));

                    if (__instance.GameTags.Contains(revenantTier1GameTag))
                    {
                        string name = __instance.name + " Mimic";
                        __result = name;
                    }
                    else if (__instance.GameTags.Contains(revenantTier2GameTag))
                    {
                        string name = __instance.name + " Dybbuk";
                        __result = name;
                    }
                    else if (__instance.GameTags.Contains(revenantTier3GameTag))
                    {
                        string name = __instance.name + " Nemesis";
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
                // GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant"));

                foreach (TacticalActor actor in alienFaction.TacticalActors)
                {
                    if (actor.tag.Contains(classTagDef.name) && actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant"))))
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

        public static void SetRevenantTierTag(GeoTacUnitId deadSoldier, TacticalActorBase actor, TacticalLevelController level)
        {
            try
            {
                GameTagDef revenantTier1GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_1_GameTagDef"));
                GameTagDef revenantTier2GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"));
                GameTagDef revenantTier3GameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"));
                GameTagDef anyRevenantGameTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Any_Revenant_TagDef"));


                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers.TryGetValue(deadSoldier, out deadSoldierStats) ? deadSoldierStats : null;
                int numMissions = deadSoldierStats.MissionsParticipated;
                int enemiesKilled = deadSoldierStats.EnemiesKilled.Count;
                int soldierLevel = deadSoldierStats.Level;
                int score = (numMissions + enemiesKilled + soldierLevel) / 2;

                GameTagDef tag = new GameTagDef();

                if (score >= 30)
                {
                    tag = revenantTier3GameTag;
                }
                else if (score <= 30 && score >20)
                {
                    tag = revenantTier2GameTag;
                }
                else if (score <= 10)
                {
                    tag = revenantTier1GameTag;
                }
                             
                actor.GameTags.Add(tag, GameTagAddMode.ReplaceExistingExclusive);
                actor.GameTags.Add(anyRevenantGameTag);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
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
                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("PriestSpecializationDef"))))
                {
                    description += " Power overflowing";
                }
                if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("SniperSpecializationDef"))))
                {
                    description += " All seeing";
                }
                else if (specializations.Contains(Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("TechnicianSpecializationDef"))))
                {
                    description += " Surge!";
                }

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

        public static void AddtoListOfDeadSoldiers(TacticalActorBase deadSoldier)//, TacticalLevelController level)
        {

            try
            {

                TacticalActor tacticalActor = (TacticalActor)deadSoldier;
                int delirium = tacticalActor.CharacterStats.Corruption.IntValue;
                TFTVLogger.Always("The character that died has " + delirium + " Delirium");
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



        public static DamageTypeBaseEffectDef GetPreferredDamageType(TacticalLevelController controller)
        {
            try
            {
                List<SoldierStats> allSoldierStats = controller.TacticalGameParams.Statistics.LivingSoldiers.Values.ToList();
                allSoldierStats.AddRange(controller.TacticalGameParams.Statistics.DeadSoldiers.Values.ToList());
                List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                DamageOverTimeDamageTypeEffectDef virusDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Virus_DamageOverTimeDamageTypeEffectDef"));
                DamageOverTimeDamageTypeEffectDef acidDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Acid_DamageOverTimeDamageTypeEffectDef"));
                DamageOverTimeDamageTypeEffectDef paralysisDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Paralysis_DamageOverTimeDamageTypeEffectDef"));
                DamageOverTimeDamageTypeEffectDef poisonDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Poison_DamageOverTimeDamageTypeEffectDef"));
                StandardDamageTypeEffectDef fireDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Fire_StandardDamageTypeEffectDef"));
                StandardDamageTypeEffectDef projectileDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Projectile_StandardDamageTypeEffectDef"));
                StandardDamageTypeEffectDef shredDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Shred_StandardDamageTypeEffectDef"));
                StandardDamageTypeEffectDef blastDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Blast_StandardDamageTypeEffectDef"));
                StandardDamageTypeEffectDef bashDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Bash_StandardDamageTypeEffectDef"));

                DamageKeywordDef paralisingDamageKeywordDef = Repo.GetAllDefs<DamageKeywordDef>().FirstOrDefault(p => p.name.Equals("Paralysing_DamageKeywordDataDef"));


                int scoreFireDamage = 0;
                //  int scoreShredDamage = 0;
                int scoreBlastDamage = 0;
                int scoreAcidDamage = 0;
                int scoreHighDamage = 0;
                int scoreBurstDamage = 0;
                //    int scoreBashDamage = 0;

                foreach (SoldierStats stat in allSoldierStats)
                {
                    if (stat.ItemsUsed.Count > 0)
                    {
                        usedWeapons.AddRange(stat.ItemsUsed);

                    }
                }
              //  TFTVLogger.Always("Number of times weapons or other items were used " + usedWeapons.Count());
                if (usedWeapons.Count() > 0)
                {
                   // TFTVLogger.Always("Checking use of each weapon... ");
                    foreach (UsedWeaponStat stat in usedWeapons)
                    {
                       // TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());
                        if (Repo.GetAllDefs<WeaponDef>().FirstOrDefault(p => p.name.Contains(stat.UsedItem.ToString())))
                        {
                            WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                           // TFTVLogger.Always("This item, as weapon is  " + weaponDef.ViewElementDef.DisplayName1.LocalizeEnglish());

                            if (weaponDef != null && weaponDef.DamagePayload.DamageType == fireDamage)
                            {
                                scoreFireDamage += stat.UsedCount;
                            }
                            /*  if (weaponDef.DamagePayload.DamageType == bashDamage)
                              {
                                  TFTVLogger.Always("This weapon is considered melee damage  " + weaponDef.ViewElementDef.DisplayName1.LocalizeEnglish());
                                  scoreBashDamage += stat.UsedCount;
                              }*/
                            if (weaponDef != null && weaponDef.DamagePayload.DamageType == blastDamage)
                            {
                                scoreBlastDamage += stat.UsedCount;
                            }
                            if (weaponDef != null && weaponDef.DamagePayload.DamageType == acidDamage)
                            {
                                scoreAcidDamage += stat.UsedCount;
                            }
                            if (weaponDef != null && weaponDef.DamagePayload.DamageKeywords[0].Value >= 70)
                            {

                               // TFTVLogger.Always("This weapon is considered high damage  " + weaponDef.ViewElementDef.DisplayName1.LocalizeEnglish());
                                scoreHighDamage += stat.UsedCount;


                            }
                            if (weaponDef != null && weaponDef.DamagePayload.DamageType == projectileDamage && (weaponDef.DamagePayload.ProjectilesPerShot >= 2 || weaponDef.DamagePayload.AutoFireShotCount >= 3))
                            {
                              //  TFTVLogger.Always("This weapon is considered high burst  " + weaponDef.ViewElementDef.DisplayName1.LocalizeEnglish());
                                scoreBurstDamage += stat.UsedCount;
                            }


                        }
                    }
                    TFTVLogger.Always("Number of fire weapons used " + scoreFireDamage);
                    TFTVLogger.Always("Number of blast weapons used " + scoreBlastDamage);
                    TFTVLogger.Always("Number of acid weapons used " + scoreAcidDamage);
                    //  TFTVLogger.Always("Number of shred weapons used " + scoreShredDamage);
                    TFTVLogger.Always("Number of high damage per hit weapons used " + scoreHighDamage);
                    TFTVLogger.Always("Number of burst weapons used " + scoreBurstDamage);


                    //    scoreShredDamage = (int)(scoreShredDamage * 0.25);
                    //    TFTVLogger.Always("Number of shred weapons used after adjustment  " + scoreShredDamage);
                    scoreHighDamage = (int)(scoreHighDamage * 0.25); //for testing
                    TFTVLogger.Always("Number of high damage weapons used after adjustment  " + scoreHighDamage);
                    scoreBurstDamage = (int)(scoreBurstDamage * 0.10);
                    TFTVLogger.Always("Number of burst weapons used after adjustment  " + scoreBurstDamage);
                    /*    scoreBashDamage = (int)(scoreBurstDamage * 100); //for testing
                        TFTVLogger.Always("Number of melee weapons used after adjustment  " + scoreBashDamage);*/

                    if (scoreAcidDamage > 0 || scoreFireDamage > 0 || scoreBlastDamage > 0 || scoreHighDamage > 0 || scoreBurstDamage > 0)
                    {
                        List<int> scoreList = new List<int> { scoreFireDamage, scoreAcidDamage, scoreBlastDamage, scoreBurstDamage, scoreHighDamage };
                        int winner = scoreList.Max();
                        TFTVLogger.Always("The highest score is " + winner);

                        DamageTypeBaseEffectDef damageTypeDef = new DamageTypeBaseEffectDef();
                        if (winner == scoreFireDamage)
                        {
                            damageTypeDef = fireDamage;
                        }
                        if (winner == scoreAcidDamage)
                        {
                            damageTypeDef = acidDamage;
                        }
                        if (winner == scoreBlastDamage)
                        {
                            damageTypeDef = blastDamage;
                        }
                        if (winner == scoreBurstDamage)
                        {
                            damageTypeDef = shredDamage;
                        }
                        if (winner == scoreHighDamage)
                        {
                            damageTypeDef = null; //projectileDamage;
                        }

                        return damageTypeDef;
                    }
                    else
                    {
                        return projectileDamage;

                    }
                }
                else
                {
                    return projectileDamage;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            throw new InvalidOperationException();
        }


        public static void ModifyRevenantResistanceAbility(TacticalLevelController controller)
        {
            DamageMultiplierAbilityDef revenantResistanceAbilityDef = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("RevenantResistance_AbilityDef"));
            revenantResistanceAbilityDef.DamageTypeDef = GetPreferredDamageType(controller);

            DamageOverTimeDamageTypeEffectDef virusDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Virus_DamageOverTimeDamageTypeEffectDef"));
            DamageOverTimeDamageTypeEffectDef acidDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Acid_DamageOverTimeDamageTypeEffectDef"));
            DamageOverTimeDamageTypeEffectDef paralysisDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Paralysis_DamageOverTimeDamageTypeEffectDef"));
            DamageOverTimeDamageTypeEffectDef poisonDamage = Repo.GetAllDefs<DamageOverTimeDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Poison_DamageOverTimeDamageTypeEffectDef"));
            StandardDamageTypeEffectDef fireDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Fire_StandardDamageTypeEffectDef"));
            StandardDamageTypeEffectDef projectileDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Projectile_StandardDamageTypeEffectDef"));
            StandardDamageTypeEffectDef shredDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Shred_StandardDamageTypeEffectDef"));
            StandardDamageTypeEffectDef blastDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Blast_StandardDamageTypeEffectDef"));
            StandardDamageTypeEffectDef bashDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Bash_StandardDamageTypeEffectDef"));

            string descriptionDamage = "";

            if (revenantResistanceAbilityDef.DamageTypeDef == acidDamage)
            {
                descriptionDamage = "<b>acid damage</b>";
            }
            else if (revenantResistanceAbilityDef.DamageTypeDef == blastDamage)
            {
                descriptionDamage = "<b>blast damage</b>";
            }
            else if (revenantResistanceAbilityDef.DamageTypeDef == fireDamage)
            {
                descriptionDamage = "<b>fire damage</b>";
            }
            else if (revenantResistanceAbilityDef.DamageTypeDef == shredDamage)
            {
                descriptionDamage = "<b>shred damage</b>";
            }

            else if (revenantResistanceAbilityDef.DamageTypeDef == null)
            {
                descriptionDamage = "<b>high damage attacks </b>";
                revenantResistanceAbilityDef.Multiplier = 1f;
            }
            /*   else if (revenantResistanceAbilityDef.DamageTypeDef == projectileDamage)
               {
                   descriptionDamage = "";
                   revenantResistanceAbilityDef.Multiplier = 1f;
               }*/

            revenantResistanceAbilityDef.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant Resistance", true);
            revenantResistanceAbilityDef.ViewElementDef.Description = new LocalizedTextBind((1 - revenantResistanceAbilityDef.Multiplier) * 100 + "%" + " resistance gained to " + descriptionDamage + " from knowledge of Phoenix ways", true);

            if (revenantResistanceAbilityDef.DamageTypeDef == null)
            {
                revenantResistanceAbilityDef.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant Resistance", true);
                revenantResistanceAbilityDef.ViewElementDef.Description = new LocalizedTextBind("This Pandoran has developed a unique active armor protection: <b>damage from first hit in a turn is reduced by 75%</b> " +
                    "as a response to Phoenix Project overwhelming use of weapons with high damage per projectile/strike", true);
            }
        }

        public static void AddRevenantResistanceAbility(TacticalActorBase tacticalActor)
        {
            tacticalActor.AddAbility(Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("RevenantResistance_AbilityDef")), tacticalActor);

        }

        public static void AddRevenantClassAbility(TacticalActor tacticalActor, SpecializationDef specialization)

        {
            try
            {
                if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("AssaultSpecializationDef")))
                {

                    PassiveModifierAbilityDef assaultAbility = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantAssault_AbilityDef"));
                    TFTVLogger.Always("Deceased had Assault specialization");

                    tacticalActor.AddAbility(assaultAbility, tacticalActor);

                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Pitcher_AbilityDef")), tacticalActor);
                        assaultAbility.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.15f},
                        };
                        assaultAbility.ViewElementDef.Description = new LocalizedTextBind("+15% Damage", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Pitcher_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Mutog_PrimalInstinct_AbilityDef")), tacticalActor);
                        assaultAbility.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.25f},
                        };
                        assaultAbility.ViewElementDef.Description = new LocalizedTextBind("+25% Damage", true);
                    }
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("BerserkerSpecializationDef")))
                {
                    PassiveModifierAbilityDef berserkerAbility = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantBerserker_AbilityDef"));

                    TFTVLogger.Always("Deceased had Berserker specialization");
                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("BloodLust_AbilityDef")), tacticalActor);
                        berserkerAbility.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 6},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 6},
                        };

                        berserkerAbility.ViewElementDef.Description = new LocalizedTextBind("+6 Speed", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("BloodLust_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("CloseQuarters_AbilityDef")), tacticalActor);
                        berserkerAbility.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 10},
                        };

                        berserkerAbility.ViewElementDef.Description = new LocalizedTextBind("+10 Speed", true);
                    }
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("HeavySpecializationDef")))
                {
                    PassiveModifierAbilityDef heavy = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantHeavy_AbilityDef"));

                    TFTVLogger.Always("Deceased had Heavy specialization");
                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("ReturnFire_AbilityDef")), tacticalActor);
                        heavy.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 20},
                        };

                        heavy.ViewElementDef.Description = new LocalizedTextBind("+20 Strength", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("ReturnFire_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Skirmisher_AbilityDef")), tacticalActor);
                        heavy.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 30},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 30},
                        };

                        heavy.ViewElementDef.Description = new LocalizedTextBind("+30 Strength", true);
                    }
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("InfiltratorSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Infiltrator specialization");

                    PassiveModifierAbilityDef infiltrator = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantInfiltrator_AbilityDef"));

                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("SurpriseAttack_AbilityDef")), tacticalActor);
                        infiltrator.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.AddMax, Value = 30},
                        };

                        infiltrator.ViewElementDef.Description = new LocalizedTextBind("+30% Stealth", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("SurpriseAttack_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("WeakSpot_AbilityDef")), tacticalActor);
                        infiltrator.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 30},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 30},
                        };

                        infiltrator.ViewElementDef.Description = new LocalizedTextBind("+50% Stealth", true);
                    }
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("PriestSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Priest specialization");
                    PassiveModifierAbilityDef priest = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantPriest_AbilityDef"));

                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("MindSense_AbilityDef")), tacticalActor);
                        priest.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 20},
                        };

                        priest.ViewElementDef.Description = new LocalizedTextBind("+20 Willpower", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("MindSense_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("PsychicWard_AbilityDef")), tacticalActor);
                        priest.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 40},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 40},
                        };

                        priest.ViewElementDef.Description = new LocalizedTextBind("+40 Willpower", true);
                    }
                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("SniperSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Sniper specialization");

                    PassiveModifierAbilityDef sniper = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantSniper_AbilityDef"));

                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("OverwatchFocus_AbilityDef")), tacticalActor);
                        sniper.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 15},
                        };

                        sniper.ViewElementDef.Description = new LocalizedTextBind("+15 Perception", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("OverwatchFocus_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("MasterMarksman_AbilityDef")), tacticalActor);
                        sniper.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 20 },
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.Add, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.AddMax, Value = 20},
                        };

                        sniper.ViewElementDef.Description = new LocalizedTextBind("+20 Perception, +20% Accuracy", true);
                    }

                }
                else if (specialization == Repo.GetAllDefs<SpecializationDef>().FirstOrDefault(p => p.name.Equals("TechnicianSpecializationDef")))
                {
                    TFTVLogger.Always("Deceased had Technician specialization");

                    PassiveModifierAbilityDef technician = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(sd => sd.name.Equals("RevenantTechnician_AbilityDef"));

                    if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_2_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Stability_AbilityDef")), tacticalActor);
                        technician.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                        };

                        technician.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +10 Willpower", true);
                    }
                    else if (tacticalActor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("RevenantTier_3_GameTagDef"))))
                    {
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("Stability_AbilityDef")), tacticalActor);
                        tacticalActor.AddAbility(Repo.GetAllDefs<AbilityDef>().FirstOrDefault(sd => sd.name.Equals("BioChemist_AbilityDef")), tacticalActor);
                        technician.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                        };

                        technician.ViewElementDef.Description = new LocalizedTextBind("+10 Strength, +10 Willpower", true);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void SpreadResistance(TacticalLevelController level)
        {
            try
            {
                TFTVLogger.Always("Spread Resistance invoked ");
                List<TacticalFaction> factions = level.Factions.ToList();
                foreach (TacticalFaction faction in factions)
                {
                    if (faction.Faction.FactionDef.Equals(sharedData.AlienFactionDef))
                    {
                        foreach (TacticalActorBase actor in faction.Actors.ToList())
                        {
                            TFTVLogger.Always("looking at actor " + actor.name);

                            if (!level.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(actor.GeoUnitId)
                            && !actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant"))))
                            {
                                TFTVLogger.Always("Got passed the if checks");
                                AddRevenantResistanceAbility(actor);
                                TFTVLogger.Always("revenantSpecialResistance now contains " + revenantSpecialResistance.Count);
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

        public static void AddRevenantStatusEffect(TacticalActorBase actor)

        {
            try
            {
                AddAbilityStatusDef revenantAbility =
                     Repo.GetAllDefs<AddAbilityStatusDef>().FirstOrDefault
                     (ged => ged.name.Equals("Revenant_StatusEffectDef"));

                actor.Status.ApplyStatus(revenantAbility);

                Action delayedAction = () => ModifyMistBreath(actor, Color.red, Color.red);
                actor.Timing.Start(RunOnNextFrame(delayedAction));

                // Difference between Timing.Start and Timing.Call:
                //      -Timing.Start will queue up the coroutine in the timing scheduler and then continue with the current method.
                //      -Timing.Call will start the coroutine and WAIT until it is complete.
                // Generally Start is used in methods which are not coroutines while Call is used within other coroutines 

                // So we can safely afford to modify the visual parts by starting another coroutine from this synchronous method by using Timing.Start.

                actor.Timing.Start(DiscoBreath(actor));

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static IEnumerator<NextUpdate> RunOnNextFrame(Action action)
        {
            yield return NextUpdate.NextFrame;
            action.Invoke();
        }

        /// <summary>
        /// Execute a list of actions via coroutine
        /// </summary>
        /// <param name="actions">List of actions to be played in sequential order</param>
        /// <param name="secondsBetweenActions">How many seconds to wait between actions</param>
        /// <param name="startDelay">Delay the start. In seconds.</param>
        /// <param name="repetitions">Count of repetitions. Negative numbers means infinite</param>
        /// <returns></returns>
        private static IEnumerator<NextUpdate> PlayActions(List<Action> actions, float secondsBetweenActions, float startDelay = 0f, int repetitions = 0)
        {


            yield return NextUpdate.Seconds(startDelay);

            do
            {
                foreach (var action in actions)
                {
                    action.Invoke();
                    yield return NextUpdate.Seconds(secondsBetweenActions);
                }

                if (repetitions == 0)
                {
                    break;
                }

                if (repetitions > 0)
                {
                    repetitions--;
                }
            }
            while (repetitions != 0);


        }

        private static IEnumerator<NextUpdate> DiscoBreath(TacticalActorBase actor)
        {
            List<Action> changeColorActions = new List<Action>() {
                () => ModifyMistBreath(actor, Color.red, Color.clear),
                () => ModifyMistBreath(actor, Color.clear, Color.black),
                () => ModifyMistBreath(actor, Color.black, Color.red),
            };

            yield return actor.Timing.Call(PlayActions(
                actions: changeColorActions,
                secondsBetweenActions: 2f,
                startDelay: 1f,
                repetitions: -1
            ));
        }

        private static void ModifyMistBreath(TacticalActorBase actor, Color from, Color to)
        {
            string targetVfxName = "VFX_OilCrabman_Breath";
            //string targetVfxName = tacStatus.TacStatusDef.ParticleEffectPrefab.GetComponent<ParticleSpawnSettings>().name;

            var pssArray = actor.AddonsManager
                .RigRoot.GetComponentsInChildren<ParticleSpawnSettings>()
                .Where(pss => pss.name.StartsWith(targetVfxName));

            var particleSystems = pssArray
                .SelectMany(pss => pss.GetComponentsInChildren<UnityEngine.ParticleSystem>());

            foreach (var ps in particleSystems)
            {
                var mainModule = ps.main;
                UnityEngine.ParticleSystem.MinMaxGradient minMaxGradient = mainModule.startColor;
                minMaxGradient.mode = ParticleSystemGradientMode.Color;
                minMaxGradient.colorMin = from;
                minMaxGradient.colorMax = to;
                mainModule.startColor = minMaxGradient;
            }
        }

        // Adopted from MadSkunky BetterClasses. Harmony Patch to calcualte shred resistance, vanilla has no implementation for this
        [HarmonyPatch(typeof(ShreddingDamageKeywordData), "ProcessKeywordDataInternal")]
        internal static class BC_ShreddingDamageKeywordData_ProcessKeywordDataInternal_ShredResistant_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                TacticalActorBase actor = data.Target.GetActor();
                DamageMultiplierAbilityDef revenantResistanceAbilityDef = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(dma => dma.name.Equals("RevenantResistance_AbilityDef"));
                StandardDamageTypeEffectDef shredDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Shred_StandardDamageTypeEffectDef"));

                if (actor != null && actor.GetAbilityWithDef<DamageMultiplierAbility>(revenantResistanceAbilityDef) != null && revenantResistanceAbilityDef.DamageTypeDef == shredDamage)
                {
                    data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * revenantResistanceAbilityDef.Multiplier);
                }
            }
        }


        [HarmonyPatch(typeof(TacticalActorBase), "ApplyDamageInternal")]
        internal static class TacticalActorBase_ApplyDamage_DamageResistant_patch
        {
            public static void Postfix(TacticalActorBase __instance)
            {
                try
                {
                    //  TFTVLogger.Always("Actor who has received damage is " + __instance.name);

                    DamageMultiplierAbilityDef searchedAbilityDef = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(dma => dma.name.Equals("RevenantResistance_AbilityDef"));
                    StandardDamageTypeEffectDef projectileDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Projectile_StandardDamageTypeEffectDef"));

                    if (searchedAbilityDef.DamageTypeDef == null && __instance.GetAbilityWithDef<DamageMultiplierAbility>(searchedAbilityDef) != null && !revenantSpecialResistance.Contains(__instance.name))
                    {
                        TFTVLogger.Always(__instance.name + " has the Revenant Resistance ability and it's the first time it is triggered");
                        revenantSpecialResistance.Add(__instance.name);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        // Harmony Patch to calculate damage resistance
        [HarmonyPatch(typeof(DamageKeyword), "ProcessKeywordDataInternal")]
        internal static class BC_DamageKeyword_ProcessKeywordDataInternal_DamageResistant_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    float multiplier = 0.25f;
                    TacticalActorBase actor = data.Target.GetActor();

                    DamageMultiplierAbilityDef searchedAbilityDef = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(dma => dma.name.Equals("RevenantResistance_AbilityDef"));
                    StandardDamageTypeEffectDef projectileDamage = Repo.GetAllDefs<StandardDamageTypeEffectDef>().FirstOrDefault(p => p.name.Equals("Projectile_StandardDamageTypeEffectDef"));

                    if (searchedAbilityDef.DamageTypeDef == null && actor != null && actor.GetAbilityWithDef<DamageMultiplierAbility>(searchedAbilityDef) != null && !revenantSpecialResistance.Contains(actor.name))
                    {
                        //  TFTVLogger.Always("This check was passed");
                        data.DamageResult.HealthDamage = Mathf.Floor(data.DamageResult.HealthDamage * multiplier);
                        data.AmountApplied = Mathf.Floor(data.AmountApplied * multiplier);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_Revenant_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    GameTagDef revenantTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Any_Revenant_TagDef"));

                    if (deathReport.Actor.HasGameTag(revenantTag))
                        {
                        revenantSpawned = true;

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
                            if (revenantSpawned == false)
                            {

                                if (actor.TacticalFaction.Faction.BaseDef == sharedData.AlienFactionDef && DeadSoldiersDelirium.Count > 0
                                    && !actor.GameTags.Contains(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Contains("Revenant"))))
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
                                        if ((timeOfMissionStart - timeUnit).TimeSpan.Days >= 1)
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

                                    if (actor.GameTags.Contains(crabTag) && eligibleForArthron.Count > 0 && !CheckForActorWithTag(crabTag, __instance)
                                        && RevenantCounter[0] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForArthron.First();
                                        TFTVLogger.Always("Here is an eligible crab: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //  SpreadResistance(__instance);
                                        actor.UpdateStats();

                                        RevenantCounter[0] = 1;
                                    }

                                    if (actor.GameTags.Contains(fishmanTag) && eligibleForTriton.Count > 0 && !CheckForActorWithTag(fishmanTag, __instance)
                                        && RevenantCounter[1] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForTriton.First();
                                        TFTVLogger.Always("Here is an eligible fishman: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //   SpreadResistance(__instance);
                                        actor.UpdateStats();

                                        RevenantCounter[1] = 1;
                                    }

                                    if (actor.GameTags.Contains(sirenTag) && eligibleForSiren.Count > 0 && !CheckForActorWithTag(sirenTag, __instance)
                                        && RevenantCounter[2] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForSiren.First();
                                        TFTVLogger.Always("Here is an eligible Siren: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //   SpreadResistance(__instance);
                                        actor.UpdateStats();


                                        RevenantCounter[2] = 1;
                                    }
                                    if (actor.GameTags.Contains(chironTag) && eligibleForChiron.Count > 0 && !CheckForActorWithTag(chironTag, __instance)
                                        && RevenantCounter[3] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForChiron.First();
                                        TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //   SpreadResistance(__instance);
                                        actor.UpdateStats();


                                        RevenantCounter[3] = 1;
                                    }
                                    if (actor.GameTags.Contains(acheronTag) && eligibleForAcheron.Count > 0 && !CheckForActorWithTag(acheronTag, __instance)
                                        && RevenantCounter[4] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForAcheron.First();
                                        TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //    SpreadResistance(__instance);
                                        actor.UpdateStats();
                                        RevenantCounter[4] = 1;
                                    }
                                    if (actor.GameTags.Contains(queenTag) && eligibleForScylla.Count > 0 && !CheckForActorWithTag(queenTag, __instance)
                                        && RevenantCounter[5] == 0)
                                    {
                                        GeoTacUnitId theChosen = eligibleForScylla.First();
                                        TFTVLogger.Always("Here is an eligible Chiron: " + actor.GetDisplayName());
                                        TacticalActor tacticalActor = actor as TacticalActor;
                                        AddRevenantStatusEffect(actor);
                                        SetRevenantTierTag(theChosen, actor, __instance);
                                        actor.name = GetDeadSoldiersNameFromID(theChosen, __instance);
                                        DeadSoldiersDelirium[actor.name] += 1;
                                        SetDeathTime(theChosen, __instance, timeOfMissionStart);
                                        TFTVLogger.Always("The time of death has been reset to " + CheckTimerFromDeath(theChosen, __instance).DateTime.ToString());
                                        SetRevenantClassAbility(theChosen, __instance, tacticalActor);
                                        AddRevenantResistanceAbility(actor);
                                        //   SpreadResistance(__instance);
                                        actor.UpdateStats();
                                        RevenantCounter[5] = 1;
                                    }

                                }
                            }
                            revenantSpawned = true;
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }*/

    }
}


