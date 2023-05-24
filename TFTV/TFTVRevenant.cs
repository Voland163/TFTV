using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.ParticleSystems;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
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
using System.Diagnostics;
using System.Linq;
using UnityEngine;


namespace TFTV
{
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static Dictionary<int, int> DeadSoldiersDelirium = new Dictionary<int, int>();
        public static Dictionary<int, int> RevenantsKilled = new Dictionary<int, int>();


        public static int daysRevenantLastSeen = 0;
        // public static  timeLastRevenantSpawned = new TimeUnit();
        public static bool revenantCanSpawn = false;
        public static bool revenantSpawned = false;
        public static List<string> revenantSpecialResistance = new List<string>();
        public static int revenantID = 0;
        public static bool revenantResistanceHintCreated = false;

      //  private static bool revenantPresent = false;

        private static readonly GameTagDef revenantTier1GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_1_GameTagDef");
        private static readonly GameTagDef revenantTier2GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef");
        private static readonly GameTagDef revenantTier3GameTag = DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef");
        private static readonly GameTagDef anyRevenantGameTag = DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");
        private static readonly GameTagDef revenantResistanceGameTag = DefCache.GetDef<GameTagDef>("RevenantResistance_GameTagDef");

        private static readonly PassiveModifierAbilityDef revenantAssault = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantAssault_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantBerserker = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantBerserker_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantInfiltrator = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantInfiltrator_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantTechnician = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantTechnician_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantHeavy = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantHeavy_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantPriest = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantPriest_AbilityDef");
        private static readonly PassiveModifierAbilityDef revenantSniper = DefCache.GetDef<PassiveModifierAbilityDef>("RevenantSniper_AbilityDef");

        private static readonly SpecializationDef assaultSpecialization = DefCache.GetDef<SpecializationDef>("AssaultSpecializationDef");
        private static readonly SpecializationDef berserkerSpecialization = DefCache.GetDef<SpecializationDef>("BerserkerSpecializationDef");
        private static readonly SpecializationDef heavySpecialization = DefCache.GetDef<SpecializationDef>("HeavySpecializationDef");
        private static readonly SpecializationDef infiltratorSpecialization = DefCache.GetDef<SpecializationDef>("InfiltratorSpecializationDef");
        private static readonly SpecializationDef priestSpecialization = DefCache.GetDef<SpecializationDef>("PriestSpecializationDef");
        private static readonly SpecializationDef sniperSpecialization = DefCache.GetDef<SpecializationDef>("SniperSpecializationDef");
        private static readonly SpecializationDef technicianSpecialization = DefCache.GetDef<SpecializationDef>("TechnicianSpecializationDef");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
        private static readonly ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
        private static readonly ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
        private static readonly ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
        private static readonly ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");





        private static readonly AddAbilityStatusDef revenantStatusAbility = DefCache.GetDef<AddAbilityStatusDef>("Revenant_StatusEffectDef");
        private static readonly PassiveModifierAbilityDef revenantAbility = DefCache.GetDef<PassiveModifierAbilityDef>("Revenant_AbilityDef");
        private static readonly DamageMultiplierStatusDef revenantResistanceStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantResistance_StatusDef");
        //private static readonly DamageMultiplierAbilityDef revenantResistanceAbility = DefCache.GetDef<DamageMultiplierAbilityDef>("RevenantResistance_AbilityDef");



        private static readonly DamageOverTimeDamageTypeEffectDef virusDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Virus_DamageOverTimeDamageTypeEffectDef");
        private static readonly DamageOverTimeDamageTypeEffectDef acidDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");
        private static readonly AttenuatingDamageTypeEffectDef paralysisDamageWeaponDescription = DefCache.GetDef<AttenuatingDamageTypeEffectDef>("Electroshock_AttenuatingDamageTypeEffectDef");
        private static readonly DamageOverTimeDamageTypeEffectDef paralysisDamage = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
        // private static readonly DamageOverTimeDamageTypeEffectDef poisonDamage =DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Poison_DamageOverTimeDamageTypeEffectDef"));
        private static readonly StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef projectileDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef shredDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Shred_StandardDamageTypeEffectDef");
        private static readonly StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");
        // private static readonly StandardDamageTypeEffectDef bashDamage =DefCache.GetDef<StandardDamageTypeEffectDef>("Bash_StandardDamageTypeEffectDef"));

        //private static readonly DamageKeywordDef paralisingDamageKeywordDef =DefCache.GetDef<DamageKeywordDef>("Paralysing_DamageKeywordDataDef"));


        /*   private static readonly DamageMultiplierStatusDef RevenantAssaultStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantAssaultStatus");
           private static readonly DamageMultiplierStatusDef RevenantHeavyStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantHeavyStatus");
           private static readonly DamageMultiplierStatusDef RevenantBerserkerStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantBerserkerStatus");
           private static readonly DamageMultiplierStatusDef RevenantInfiltratorStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantInfiltratorStatus");
           private static readonly DamageMultiplierStatusDef RevenantSniperStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantSniperStatus");
           private static readonly DamageMultiplierStatusDef RevenantTechnician = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantTechnicianStatus");
           private static readonly DamageMultiplierStatusDef RevenantPriestStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RevenantPriestStatus");
           */

        public static void CheckIfRevenantPresent(TacticalLevelController controller)
        {
            try 
            {
                if (controller.IsFromSaveGame && revenantID != 0) 
                {
                    TFTVLogger.Always("Game is loading and Revenant is present; adjusting revenant resistance check");
                    AdjustRevenantResistanceStatusDescription();
                
                }



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CheckForNotDeadSoldiers(TacticalLevelController level)
        {
            try
            {
                List<int> soldiersReallyDead = new List<int>();
                List<int> soldiersNotReallyDead = new List<int>();
                SoldierStats soldierStats = new SoldierStats();

                TFTVLogger.Always("Soldiers in the DeadSoldiersDelirium are " + DeadSoldiersDelirium.Count());

                foreach (GeoTacUnitId reallyDeadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                {
                    TFTVLogger.Always("This one is really dead: " + reallyDeadSoldier);
                    soldiersReallyDead.Add(reallyDeadSoldier);

                }

                foreach (int id in DeadSoldiersDelirium.Keys)
                {
                    if (!soldiersReallyDead.Contains(id))
                    {
                        TFTVLogger.Always(id + " ? This one ain't dead!");
                        soldiersNotReallyDead.Add(id);
                    }
                }

                foreach (int id in soldiersNotReallyDead)
                {
                    DeadSoldiersDelirium.Remove(id);
                }

                TFTVLogger.Always("Soldiers in the DeadSoldiersDelirium after cleanup are " + DeadSoldiersDelirium.Count());
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
                TFTVLogger.Always("DeadSoldierDeliriumCount is " + DeadSoldiersDelirium.Count + " and last time a Revenant was seen was on day " + daysRevenantLastSeen + ", and now it is day " + controller.Timing.Now.TimeSpan.Days);
                if (DeadSoldiersDelirium.Count > 0 && (daysRevenantLastSeen == 0 || controller.Timing.Now.TimeSpan.Days - daysRevenantLastSeen >= 3)) //UnityEngine.Random.Range(-1, 3))) 
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
                    controller.EventSystem.SetVariable("Revenant_Spotted", controller.EventSystem.GetVariable("Revenant_Spotted") + 1);
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
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) && DeadSoldiersDelirium.Count > 0)
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
        public static GeoTacUnitId RevenantRoll(TacticalLevelController controller)
        {
            try
            {

                List<int> allDeadSoldiers = DeadSoldiersDelirium.Keys.ToList();
                List<GeoTacUnitId> candidates = new List<GeoTacUnitId>();
                TFTVLogger.Always("Total count in DeadSoldiersDelirium is " + allDeadSoldiers.Count);

                foreach (int deadSoldier in allDeadSoldiers)
                {
                    candidates.Add(GetDeadSoldiersIdFromInt(deadSoldier, controller));
                    TFTVLogger.Always("deadSoldier " + deadSoldier + " with GeoTacUnitId "
                        + GetDeadSoldiersIdFromInt(deadSoldier, controller) + " is added to the list of Revenant candidates");
                }

                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
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
        public static List<ClassTagDef> GetRevenantClassTag(TacticalLevelController controller, GeoTacUnitId theChosen)
        {
            try
            {
                int delirium = DeadSoldiersDelirium[theChosen];
                List<ClassTagDef> possibleTags = new List<ClassTagDef> { crabTag };

                TFTVLogger.Always("The Chosen, " + GetDeadSoldiersNameFromID(theChosen, controller) + ", has " + delirium + " Delirium");

                if (delirium > 2)
                {
                    possibleTags.Add(fishmanTag);
                }
                if (delirium > 5)
                {
                    possibleTags.Add(sirenTag);
                }
                if (delirium > 6)
                {
                    possibleTags.Add(chironTag);
                }
                if (delirium > 8)
                {
                    possibleTags.Add(acheronTag);
                }
                if (delirium > 9)
                {
                    possibleTags.Add(queenTag);

                }

                return possibleTags;


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
                    List<ClassTagDef> possibleClasses = GetRevenantClassTag(controller, theChosen);
                    List<TacticalActorBase> candidates = new List<TacticalActorBase>();
                    TacticalActorBase actor = new TacticalActorBase();
                    int availableTags = possibleClasses.Count;


                    for (int i = 0; i < availableTags; i++)
                    {

                        foreach (TacticalActorBase tacticalActorBase in pandorans.Actors.Where(tab => tab.GameTags.Contains(possibleClasses.Last())))
                        {
                            candidates.Add(tacticalActorBase);

                        }
                        candidates = candidates.OrderByDescending(tab => tab.DeploymentCost).ToList();



                        if (candidates.Count > 0)
                        {
                            actor = candidates.First();
                            i = availableTags;
                        }
                        else
                        {
                            TFTVLogger.Always(possibleClasses.Last() + " no actor with this tag, removing from list");
                            possibleClasses.Remove(possibleClasses.Last());

                            if (i == availableTags - 1)
                            {
                                TFTVLogger.Always("No eligible Pandoran found, no Revenant will spawn this time");
                                return;
                            }
                        }
                    }


                    TFTVLogger.Always("Here is an eligible Pandoran to be a Revenant: " + actor.GetDisplayName());
                    TacticalActor tacticalActor = actor as TacticalActor;
                    AddRevenantStatusEffect(actor);
                    SetRevenantTierTag(theChosen, actor, controller);
                    actor.name = GetDeadSoldiersNameFromID(theChosen, controller);
                    // SetDeathTime(theChosen, __instance, timeOfMissionStart);
                    revenantID = theChosen;
                    SetRevenantClassAbility(theChosen, controller, tacticalActor);
                    AddRevenantResistanceStatus(actor);
                    //  SpreadResistance(__instance);
                    actor.UpdateStats();
                    //  TFTVLogger.Always("Crab's name has been changed to " + actor.GetDisplayName());
                    // revenantCanSpawn = false;

                    foreach (TacticalActorBase pandoranBase in pandorans.Actors)
                    {
                        TacticalActor pandoran = pandoranBase as TacticalActor;
                        if (pandoran != null)
                        {
                            TFTVLogger.Always("The Pandoran is " + pandoran.DisplayName);

                            if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(pandoran.GeoUnitId)
                                 && !pandoran.GameTags.Contains(anyRevenantGameTag)
                                 && !pandoran.Status.HasStatus(revenantResistanceStatus))
                            {

                                AddRevenantResistanceStatus(pandoran);
                                TFTVLogger.Always(pandoran.name + " received the revenant resistance ability.");
                            }
                        }
                    }
                    string newGuid = Guid.NewGuid().ToString();
                    string hintDescription = revenantResistanceStatus.Visuals.Description.LocalizeEnglish() +
                        ".\nKilling the Revenant will not remove this resistance from any Pandoran that already has it." +
                        "\nPandorans arriving as reinforcements will not receive the resistance.";

                    // TFTVLogger.Always("Got to before hint");

                    TFTVTutorialAndStory.CreateNewTacticalHintForRevenantResistance("RevenantResistanceSighted", HintTrigger.ActorSeen, "RevenantResistance_GameTagDef", "Revenant resistance", hintDescription);
                    revenantResistanceHintCreated = true;
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ImplementVO19(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) && TFTVVoidOmens.VoidOmensCheck[19])
                {
                    TacticalFaction pandorans = controller.GetFactionByCommandName("aln");
                    foreach (TacticalActorBase pandoranBase in pandorans.Actors)
                    {
                        TacticalActor pandoran = pandoranBase as TacticalActor;
                        if (pandoran != null)
                        {
                            TFTVLogger.Always("The Pandoran is " + pandoran.DisplayName);

                            if (!controller.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(pandoran.GeoUnitId)
                                 && !pandoran.GameTags.Contains(anyRevenantGameTag)
                                 && !pandoran.Status.HasStatus(revenantResistanceStatus))
                            {

                                AddRevenantResistanceStatus(pandoran);
                                TFTVLogger.Always(pandoran.name + " received the revenant resistance ability.");
                            }
                        }
                    }
                    string newGuid = Guid.NewGuid().ToString();
                    string hintDescription = revenantResistanceStatus.Visuals.Description.LocalizeEnglish();

                    // TFTVLogger.Always("Got to before hint");

                    TFTVTutorialAndStory.CreateNewTacticalHintForRevenantResistance("RevenantResistanceSighted", HintTrigger.ActorSeen, "RevenantResistance_GameTagDef", "Revenant resistance", hintDescription);
                    revenantResistanceHintCreated = true;

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
                    if (__instance.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("px")) && deathReport.Actor.TacticalFaction == __instance.GetFactionByCommandName("PX")
                        && !__instance.TacMission.MissionData.MissionType.name.Contains("Tutorial"))
                    {

                        if (TFTVRevenantResearch.ProjectOsiris)
                        {
                            GameTagDef mutoidTag = DefCache.GetDef<GameTagDef>("Mutoid_TagDef");

                            if (__instance.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !__instance.TacticalGameParams.Statistics.DeadSoldiers.ContainsKey(deathReport.Actor.GeoUnitId) && !deathReport.Actor.GameTags.Contains(mutoidTag))

                                TFTVRevenantResearch.RecordStatsOfDeadSoldier(deathReport.Actor);
                        }


                        if (__instance.TacticalGameParams.Statistics.LivingSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !__instance.TacticalGameParams.Statistics.DeadSoldiers.ContainsKey(deathReport.Actor.GeoUnitId)
                            && !DeadSoldiersDelirium.ContainsKey(deathReport.Actor.GeoUnitId))
                        {
                            AddtoListOfDeadSoldiers(deathReport.Actor);
                            TFTVStamina.charactersWithDisabledBodyParts.Remove(deathReport.Actor.GeoUnitId);
                            TFTVLogger.Always(deathReport.Actor.DisplayName + " died at. The deathlist now has " + DeadSoldiersDelirium.Count);
                            if (deathReport.Actor.DisplayName != __instance.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name)
                            {
                                TFTVLogger.Always("Dead actor " + deathReport.Actor.DisplayName + " is " + __instance.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name +
                                    " in the files. Files will be updated");
                                PhoenixStatisticsManager statisticsManager = (PhoenixStatisticsManager)UnityEngine.Object.FindObjectOfType(typeof(PhoenixStatisticsManager));
                                statisticsManager.CurrentGameStats.DeadSoldiers[deathReport.Actor.GeoUnitId].Name = deathReport.Actor.DisplayName;
                                __instance.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name = deathReport.Actor.DisplayName;
                                TFTVLogger.Always("Name in files of Living Soldiers changed to " + __instance.TacticalGameParams.Statistics.LivingSoldiers[deathReport.Actor.GeoUnitId].Name);
                                TFTVLogger.Always("Name in files of currentstats changed to " + statisticsManager.CurrentGameStats.DeadSoldiers[deathReport.Actor.GeoUnitId].Name);
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

        [HarmonyPatch(typeof(TacticalAbility), "GetAbilityDescription")]
        public static class TacticalAbility_DisplayCategory_ChangeDescriptionRevenantSkill_patch
        {
            public static void Postfix(TacticalAbility __instance, ref string __result)
            {
                try
                {
                    if (__instance.AbilityDef == revenantAbility)
                    {
                        string actorName = __instance.TacticalActor.name;
                        string additionalDescription =
                            GetDescriptionOfRevenantClassAbility(actorName, __instance.TacticalActor.TacticalLevel);
                        __result = "This is your fallen comrade, " + actorName + ", returned as Pandoran monstrosity. " + additionalDescription;

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

        public static string GetDeadSoldiersNameFromID(GeoTacUnitId deadSoldier, TacticalLevelController level)
        {
            try
            {
                string name = level.TacticalGameParams.Statistics.DeadSoldiers[deadSoldier].Name;
                return name;
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
                SoldierStats deadSoldierStats = level.TacticalGameParams.Statistics.DeadSoldiers[deadSoldier];
                int numMissions = deadSoldierStats.MissionsParticipated;
                int enemiesKilled = deadSoldierStats.EnemiesKilled.Count;
                int soldierLevel = deadSoldierStats.Level;
                int score = (numMissions + enemiesKilled + soldierLevel) / 2;
                TFTVLogger.Always("#of Missions: " + numMissions + ". #enemies killed: " + enemiesKilled + ". level: " + soldierLevel + ". The score is " + score);
                GameTagDef tag = new GameTagDef();

                if (score >= 30)
                {
                    tag = revenantTier3GameTag;
                }
                else if (score <= 30 && score > 10)
                {
                    tag = revenantTier2GameTag;
                }
                else if (score <= 10)
                {
                    tag = revenantTier1GameTag;
                }
                if (!actor.HasGameTag(tag))
                {
                    actor.GameTags.Add(tag);
                }
                if (!actor.HasGameTag(anyRevenantGameTag))
                {
                    actor.GameTags.Add(anyRevenantGameTag);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static GeoTacUnitId GetDeadSoldiersIdFromInt(int id, TacticalLevelController level)
        {
            try
            {
                foreach (GeoTacUnitId deadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                {
                    if (deadSoldier == id)
                    {
                        return deadSoldier;
                    }
                }
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
                foreach (SoldierStats deadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Values)
                {
                    if (deadSoldier.Name == name)
                    {
                        foreach (GeoTacUnitId idOfDeadSoldier in level.TacticalGameParams.Statistics.DeadSoldiers.Keys)
                        {

                            if (level.TacticalGameParams.Statistics.DeadSoldiers.GetValueSafe(idOfDeadSoldier) == deadSoldier)
                            {
                                TFTVLogger.Always("The soldier is " + deadSoldier.Name + " with GeoTacUnitID " + idOfDeadSoldier.ToString());
                                return idOfDeadSoldier;
                            }

                        }

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

                if (specializations.Contains(assaultSpecialization))
                {
                    description += "Increased damage potential and speed.";
                }
                if (specializations.Contains(berserkerSpecialization))
                {
                    description += "Fearless, fast, unstoppable...";
                }
                if (specializations.Contains(heavySpecialization))
                {
                    description += "Bullet sponge.";
                }
                if (specializations.Contains(infiltratorSpecialization))
                {
                    description += "Scary quiet.";
                }
                if (specializations.Contains(priestSpecialization))
                {
                    description += "Power overflowing.";
                }
                if (specializations.Contains(sniperSpecialization))
                {
                    description += "All seeing.";
                }
                else if (specializations.Contains(technicianSpecialization))
                {
                    description += "Surge!";
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
        public static void AddtoListOfDeadSoldiers(TacticalActorBase deadSoldier)//, TacticalLevelController level)
        {

            try
            {

                TacticalActor tacticalActor = (TacticalActor)deadSoldier;
                int delirium = tacticalActor.CharacterStats.Corruption.IntValue;
                TFTVLogger.Always("The character that died has " + delirium + " Delirium");
                if (delirium > 0)
                {
                    DeadSoldiersDelirium.Add(deadSoldier.GeoUnitId, delirium);
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
                TFTVLogger.Always("AllSoldierStats count is " + allSoldierStats.Count());
                allSoldierStats.AddRange(controller.TacticalGameParams.Statistics.DeadSoldiers.Values.ToList());
                TFTVLogger.Always("AllSoldierStats, including dead soldiers, count is " + allSoldierStats.Count());
                List<UsedWeaponStat> usedWeapons = new List<UsedWeaponStat>();

                float scoreFireDamage = 0;
                float scoreParalysisDamage = 0;
                float scoreVirusDamage = 0;
                //  int scoreShredDamage = 0;
                float scoreBlastDamage = 0;
                float scoreAcidDamage = 0;
                float scoreHighDamage = 0;
                float scoreBurstDamage = 0;
                //    int scoreBashDamage = 0;

                foreach (SoldierStats stat in allSoldierStats)
                {
                    if (stat.ItemsUsed.Count > 0)
                    {
                        usedWeapons.AddRange(stat.ItemsUsed);
                    }
                }

                TFTVLogger.Always("Number of weapons or other items used " + usedWeapons.Count());

                if (usedWeapons.Count() > 0)
                {
                    // TFTVLogger.Always("Checking use of each weapon... ");
                    foreach (UsedWeaponStat stat in usedWeapons)
                    {
                        WeaponDef weaponDef = stat.UsedItem as WeaponDef;
                        //   TFTVLogger.Always("This item is  " + stat.UsedItem.ViewElementDef.DisplayName1.LocalizeEnglish());

                        if (weaponDef != null)
                        {
                         //   TFTVLogger.Always($"weapon is {weaponDef.name}");

                            foreach (DamageKeywordPair damageKeywordPair in weaponDef.DamagePayload.DamageKeywords)
                            {
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword)
                                {
                                    scoreFireDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BlastKeyword)
                                {
                                    scoreBlastDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                                {
                                    scoreAcidDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.ParalysingKeyword)
                                {
                                    scoreParalysisDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.ViralKeyword)
                                {
                                    scoreVirusDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.Value >= 70 && damageKeywordPair.DamageKeywordDef!=Shared.SharedDamageKeywords.ShockKeyword)
                                {
                                   // TFTVLogger.Always($"{weaponDef.name} is counted as high damage weapon");
                                    scoreHighDamage += stat.UsedCount;
                                }
                                if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.DamageKeyword && (weaponDef.DamagePayload.ProjectilesPerShot >= 2 || weaponDef.DamagePayload.AutoFireShotCount >= 3))
                                {
                                    scoreBurstDamage += stat.UsedCount;
                                }
                            }

                        }
                    }
                    TFTVLogger.Always("Number of fire weapons used " + scoreFireDamage);
                    TFTVLogger.Always("Number of blast weapons used " + scoreBlastDamage);
                    TFTVLogger.Always("Number of acid weapons used " + scoreAcidDamage);
                    TFTVLogger.Always("Number of virus weapons used " + scoreVirusDamage);
                    TFTVLogger.Always("Number of paralysis weapons used " + scoreParalysisDamage);

                    //  TFTVLogger.Always("Number of shred weapons used " + scoreShredDamage);
                    TFTVLogger.Always("Number of high damage per hit weapons used " + scoreHighDamage);
                    TFTVLogger.Always("Number of burst weapons used " + scoreBurstDamage);

                    //Scores have to be adjusted

                    //fireWeapons 2 modified by AP cost: 4
                    //acidWeapons 4 modified by AP cost: 3 + 2 + 2 + 1 = 8
                    //paralyzingWeapons 3 modified by AP cost::  3 + 3 + 1 = 7
                    //virusWeapons 3 modified by AP cost: 2 + 2 + 1 = 5
                    //blastWeapons 7 + 1 Arachni + 6 grenades 14 total  = 14 + 9 + 3 = 26
                    //highDamageWeapons Slamstrike + 8 HWs + 9 Sniper Rifles + 6 Melee weapons 23 total
                    // modified by AP cost: 2 + 8 + 9 + 12 = 31
                    //highBurstWeapons 12 ARs + SGs + 6 HWs + 1 Sanctifier + 1 Tormentor + 1 Redeemer + 3 PDWs 24 total
                    //modified by AP cost: 24 + 6 + 3 + 3 + 2 + 9 = 47  

                    scoreAcidDamage *= 47f / 4f;
                    scoreVirusDamage *= 47f / 5f;
                    scoreParalysisDamage *= 47f / 7f;
                    scoreFireDamage *= 47f / 4f;
                    scoreBlastDamage *= 47f / 26f;
                    scoreHighDamage *= 47f / 31f;

                    TFTVLogger.Always("Number of acid damage weapons used after adjustment  " + scoreAcidDamage);
                    TFTVLogger.Always("Number of virus damage weapons used after adjustment  " + scoreVirusDamage);
                    TFTVLogger.Always("Number of paralysis damage weapons used after adjustment  " + scoreParalysisDamage);
                    TFTVLogger.Always("Number of fire damage weapons used after adjustment  " + scoreFireDamage);
                    TFTVLogger.Always("Number of high damage weapons used after adjustment  " + scoreHighDamage);
                    TFTVLogger.Always("Number of blast damage weapons used after adjustment  " + scoreBlastDamage);
                    TFTVLogger.Always("Number of burst weapons used (no adjustment) " + scoreBurstDamage);


                    if (scoreAcidDamage > 0 || scoreFireDamage > 0 || scoreBlastDamage > 0 || scoreHighDamage > 0 || scoreBurstDamage > 0
                        || scoreVirusDamage > 0 || scoreParalysisDamage > 0)
                    {
                        List<float> scoreList = new List<float> { scoreFireDamage, scoreAcidDamage, scoreBlastDamage, scoreBurstDamage, scoreHighDamage, scoreParalysisDamage, scoreVirusDamage };
                        scoreList = scoreList.OrderByDescending(x => x).ToList();
                        int options = 0;

                        if (scoreList.Count > 2)
                        {
                            options = 3;
                        }
                        else
                        {
                            options = scoreList.Count;
                        }

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                        int roll = UnityEngine.Random.Range(0, options);

                        float winner = scoreList[roll];
                        TFTVLogger.Always("The roll is " + roll + ", so the highest score is " + winner);

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
                        if (winner == scoreVirusDamage)
                        {
                            damageTypeDef = virusDamage;
                        }
                        if (winner == scoreParalysisDamage)
                        {
                            damageTypeDef = paralysisDamage;
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

        public static void AdjustRevenantResistanceStatusDescription()
        {
            try 
            {
                revenantResistanceStatus.Multiplier = 0.5f;

                string descriptionDamage = "";

                if (revenantResistanceStatus.DamageTypeDefs[0] == acidDamage)
                {
                    descriptionDamage = "<b>acid damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == blastDamage)
                {
                    descriptionDamage = "<b>blast damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == fireDamage)
                {
                    descriptionDamage = "<b>fire damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == shredDamage)
                {
                    descriptionDamage = "<b>shred damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == virusDamage)
                {
                    descriptionDamage = "<b>virus damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == paralysisDamage)
                {
                    descriptionDamage = "<b>paralysis damage</b>";
                }
                else if (revenantResistanceStatus.DamageTypeDefs[0] == null)
                {
                    descriptionDamage = "<b>high damage attacks </b>";
                    revenantResistanceStatus.Multiplier = 1f;
                }

                revenantResistanceStatus.Visuals.DisplayName1 = new LocalizedTextBind("REVENANT RESISTANCE - " + descriptionDamage.ToUpper(), true);
                revenantResistanceStatus.Visuals.Description = new LocalizedTextBind((1 - revenantResistanceStatus.Multiplier) * 100 + "%" + " resistance gained to " + descriptionDamage + " from knowledge of Phoenix ways", true);

                if (revenantResistanceStatus.DamageTypeDefs[0] == null)
                {
                    revenantResistanceStatus.Visuals.DisplayName1 = new LocalizedTextBind("REVENANT RESISTANCE - " + descriptionDamage.ToUpper(), true);
                    revenantResistanceStatus.Visuals.Description = new LocalizedTextBind("<b>Damage from first hit in a turn is reduced by 75%</b>, " +
                        "an evolutionary response to Phoenix Project overwhelming use of weapons with high damage per projectile/strike", true);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ModifyRevenantResistanceAbility(TacticalLevelController controller)
        {
            try
            {
                revenantResistanceStatus.DamageTypeDefs[0] = GetPreferredDamageType(controller);
                AdjustRevenantResistanceStatusDescription();
                
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void AddRevenantResistanceStatus(TacticalActorBase tacticalActorBase)
        {
            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

            if (!tacticalActor.Status.HasStatus(revenantResistanceStatus) && !tacticalActor.GameTags.Last().name.Contains("Mindfragged"))
            {
                tacticalActor.Status.ApplyStatus<DamageMultiplierStatus>(revenantResistanceStatus);
            }

            if (!tacticalActorBase.HasGameTag(revenantResistanceGameTag) && !tacticalActor.GameTags.Last().name.Contains("Mindfragged"))
            {
                tacticalActorBase.GameTags.Add(revenantResistanceGameTag);
            }
        }

        public static void IncreaseRevenantHP(TacticalActor tacticalActor)
        {
            try
            {


                int currentEndurance = (int)(tacticalActor.CharacterStats.Endurance);
                int newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.15f);
                if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                {

                    newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.2f);

                }
                else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                {
                    newEndurance = (int)(tacticalActor.CharacterStats.Endurance * 1.25f);

                }
                tacticalActor.CharacterStats.Endurance.SetMax(newEndurance);
                tacticalActor.CharacterStats.Endurance.SetToMax();
                tacticalActor.UpdateStats();
                tacticalActor.CharacterStats.Health.SetToMax();
                tacticalActor.UpdateStats();
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

                if (specialization == assaultSpecialization)
                {
                    TFTVLogger.Always("Deceased had Assault specialization");
                    // tacticalActor.Status.ApplyStatus(RevenantAssaultStatus);

                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {
                        revenantAssault.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                        };
                        // revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+10% Damage", true);
                    }
                    else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                    {

                        revenantAssault.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.15f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 6},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 6},
                        };
                        //  revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+15% Damage", true);
                    }
                    tacticalActor.AddAbility(revenantAssault, tacticalActor);


                }
                else if (specialization == berserkerSpecialization)
                {
                    TFTVLogger.Always("Deceased had Berserker specialization");
                    // tacticalActor.Status.ApplyStatus(RevenantBerserkerStatus);


                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {

                        revenantBerserker.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 6},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 6},
                        };

                        // revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+6 Speed", true);
                    }
                    else if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef")))
                    {

                        revenantBerserker.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 10},
                        };

                        //  revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+10 Speed", true);
                    }
                    tacticalActor.AddAbility(revenantBerserker, tacticalActor);
                }
                else if (specialization == heavySpecialization)
                {
                    TFTVLogger.Always("Deceased had Heavy specialization");
                    //    tacticalActor.Status.ApplyStatus(RevenantHeavyStatus);

                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {

                        revenantHeavy.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 150}
                        };

                        // revenantHeavy.ViewElementDef.Description = new LocalizedTextBind("+10 Strength", true);
                    }
                    else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                    {

                        revenantHeavy.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 200}
                        };

                        //  revenantHeavy.ViewElementDef.Description = new LocalizedTextBind("+15 Strength", true);
                    }
                    tacticalActor.AddAbility(revenantHeavy, tacticalActor);
                }
                else if (specialization == infiltratorSpecialization)
                {
                    TFTVLogger.Always("Deceased had Infiltrator specialization");
                    //   tacticalActor.Status.ApplyStatus(RevenantInfiltratorStatus);


                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {

                        revenantInfiltrator.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.3f},
                        };

                        //  revenantInfiltrator.ViewElementDef.Description = new LocalizedTextBind("+30% Stealth", true);
                    }
                    else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                    {

                        revenantInfiltrator.StatModifications = new ItemStatModification[]
                        {
                          // new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 30},
                           new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.Add, Value = 0.5f},
                        };

                        //   revenantInfiltrator.ViewElementDef.Description = new LocalizedTextBind("+50% Stealth", true);
                    }
                    tacticalActor.AddAbility(revenantInfiltrator, tacticalActor);
                }
                else if (specialization == priestSpecialization)
                {
                    TFTVLogger.Always("Deceased had Priest specialization");
                    //     tacticalActor.Status.ApplyStatus(RevenantPriestStatus);

                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {
                        revenantPriest.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 20},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 20},
                        };

                        // revenantPriest.ViewElementDef.Description = new LocalizedTextBind("+20 Willpower", true);
                    }
                    else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                    {

                        revenantPriest.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 30},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 30},
                        };

                        //  revenantPriest.ViewElementDef.Description = new LocalizedTextBind("+30 Willpower", true);
                    }
                    tacticalActor.AddAbility(revenantPriest, tacticalActor);
                }

                else if (specialization == sniperSpecialization)
                {

                    TFTVLogger.Always("Deceased had Sniper specialization");
                    //    tacticalActor.Status.ApplyStatus(RevenantSniperStatus);



                    if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_2_GameTagDef")))
                    {
                        revenantSniper.StatModifications = new ItemStatModification[]
                        {
                           // new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 15},
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 0.15f},
                        };


                    }
                    else if (tacticalActor.GameTags.Contains(DefCache.GetDef<GameTagDef>("RevenantTier_3_GameTagDef")))
                    {

                        revenantSniper.StatModifications = new ItemStatModification[]
                        {
                         //   new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.AddMax, Value = 20 },
                            new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 0.2f},
                            new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.Add, Value = 0.2f},
                          //  new ItemStatModification {TargetStat = StatModificationTarget.Accuracy, Modification = StatModificationType.AddMax, Value = 20},
                        };


                        //      RevenantSniperStatus.Visuals.Description = new LocalizedTextBind("Nemesis, +20 Perception, +20% Accuracy, +20% HP", true);

                    }

                    tacticalActor.AddAbility(revenantSniper, tacticalActor);
                }
                else if (specialization == technicianSpecialization)
                {
                    TFTVLogger.Always("Deceased had Technician specialization");
                    //   tacticalActor.Status.ApplyStatus(RevenantTechnician);
                    tacticalActor.AddAbility(revenantTechnician, tacticalActor);


                    if (tacticalActor.GameTags.Contains(revenantTier2GameTag))
                    {

                        revenantTechnician.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                        };

                        revenantTechnician.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +10 Willpower", true);
                    }
                    else if (tacticalActor.GameTags.Contains(revenantTier3GameTag))
                    {

                        revenantTechnician.StatModifications = new ItemStatModification[]
                        {
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                            new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10}
                        };

                        revenantTechnician.ViewElementDef.Description = new LocalizedTextBind("+10 Strength, +10 Willpower", true);
                    }
                }

                IncreaseRevenantHP(tacticalActor);
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


                actor.Status.ApplyStatus(revenantStatusAbility);

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

      /*  public static void CheckForRevenantResistance(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    TFTVLogger.Always("Aliens are present");

                    TacticalFaction aliens = controller.GetFactionByCommandName("aln");
                    TFTVLogger.Always("Alien faction is " + aliens.Faction.FactionDef.name);

                    foreach (TacticalActorBase actorBase in aliens.Actors)
                    {
                        //  TacticalActor tacticalActor = actorBase as TacticalActor;
                        if (actorBase.Status.HasStatus(revenantResistanceStatus))
                        {
                            revenantPresent = true;
                        }
                        if (revenantPresent)
                        {
                            return;
                        }
                    }
                    return;
                }
                else
                {
                    TFTVLogger.Always("Alien faction is not present");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }*/

        // Adopted from MadSkunky BetterClasses. Harmony Patch to calcualte shred resistance, vanilla has no implementation for this
        [HarmonyPatch(typeof(ShreddingDamageKeywordData), "ProcessKeywordDataInternal")]
        internal static class BC_ShreddingDamageKeywordData_ProcessKeywordDataInternal_ShredResistant_patch
        {

            public static void Postfix(ref DamageAccumulation.TargetData data)
            {

                if (data.Target.GetActor() != null && data.Target.GetActor().Status != null)
                {
                    TacticalActorBase actor = data.Target.GetActor();

                    if (actor.Status.HasStatus(revenantResistanceStatus) && revenantResistanceStatus.DamageTypeDefs[0] == shredDamage)
                    {
                        data.DamageResult.ArmorDamage = Mathf.Floor(data.DamageResult.ArmorDamage * revenantResistanceStatus.Multiplier);
                    }
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

                    if (revenantResistanceStatus != null
                        && revenantResistanceStatus.DamageTypeDefs != null
                        && revenantResistanceStatus.DamageTypeDefs.Count() > 0
                        && revenantResistanceStatus.DamageTypeDefs[0] == null
                        && __instance.Status != null
                        && __instance.Status.HasStatus(revenantResistanceStatus)
                        && revenantSpecialResistance != null
                        && !revenantSpecialResistance.Contains(__instance.name))
                    {
                        //  TFTVLogger.Always(__instance.name + " has the Revenant Resistance ability and it's the first time it is triggered");
                        revenantSpecialResistance.Add(__instance.name);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /*
        [HarmonyPatch(typeof(DamageMultiplierStatus), "GetIncomingMultiplier")]

        internal static class TFTV_DamageMultiplierStatus_GetIncomingMultiplier_DamageResistant_patch
        {
            public static void Postfix(ref float __result, DamageMultiplierStatus __instance)
            {
                try
                {
                    
           
                    if (__instance.TacticalActor != null && revenantResistanceStatus.DamageTypeDefs[0] == null
                        && __instance.TacticalActor.Status.HasStatus(revenantResistanceStatus) && 
                        !revenantSpecialResistance.Contains(__instance.TacticalActor.name))
                    {
                       
                      // revenantSpecialResistance.Add(__instance.TacticalActor.name);
                        __result = 0.25f;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }*/


        // Harmony Patch to calculate damage resistance
        [HarmonyPatch(typeof(DamageKeyword), "ProcessKeywordDataInternal")]
        internal static class TFTV_DamageKeyword_ProcessKeywordDataInternal_DamageResistant_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    if (data.Target.GetActor() != null && revenantResistanceStatus.DamageTypeDefs[0] == null
                        && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(revenantResistanceStatus))
                    {
                        float multiplier = 0.25f;

                        if (!revenantSpecialResistance.Contains(data.Target.GetActor().name))
                        {
                            //  TFTVLogger.Always("This check was passed");
                            data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                            data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                            if (!data.Target.IsBodyPart())
                            {
                                revenantSpecialResistance.Add(data.Target.GetActor().name);
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

        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_Revenant_Patch
        {
            public static void Postfix(DeathReport deathReport, TacticalLevelController __instance)
            {
                try
                {
                    if (deathReport.Actor.HasGameTag(anyRevenantGameTag))
                    {
                        revenantSpawned = true;
                        TFTVLogger.Always("Revenant was killed, so revenantSpawned is now " + revenantSpawned);
                        if (!RevenantsKilled.Keys.Contains(revenantID))
                        {
                            RevenantsKilled.Add(revenantID, 0);
                        }

                        if (deathReport.Actor.HasGameTag(revenantTier1GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 1; // testing 1
                                                                     //  TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                                                                     // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(2);
                        }
                        else if (deathReport.Actor.HasGameTag(revenantTier2GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 5; // testing 5
                                                                     //  TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                                                                     // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(4);
                        }
                        else if (deathReport.Actor.HasGameTag(revenantTier3GameTag))
                        {
                            TFTVRevenantResearch.RevenantPoints = 10;
                            // TFTVLogger.Always("StartingSkill points " + __instance.GetFactionByCommandName("PX").StartingSkillpoints);
                            // __instance.GetFactionByCommandName("PX").SetStartingSkillPoints(6);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static bool SkillPointsForRevenantKillAwarded = false;

        [HarmonyPatch(typeof(TacticalFaction), "get_Skillpoints")]
        public static class TacticalFaction_GiveExperienceForObjectives_Revenant_Patch
        {
            public static void Postfix(TacticalFaction __instance, ref int __result)
            {
                try
                {
                    // TFTVLogger.Always("get_Skillpoints");

                    if (__instance == __instance.TacticalLevel.GetFactionByCommandName("PX") && !SkillPointsForRevenantKillAwarded)
                    {
                        TacticalFaction phoenix = __instance;

                        if (TFTVRevenantResearch.RevenantPoints == 10)
                        {
                            __result += 6;
                            // TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                            SkillPointsForRevenantKillAwarded = true;
                        }
                        else if (TFTVRevenantResearch.RevenantPoints == 5)
                        {
                            __result += 4;
                            //    TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                            SkillPointsForRevenantKillAwarded = true;
                        }
                        else if (TFTVRevenantResearch.RevenantPoints == 1)
                        {
                            __result += 2;
                            //   TFTVLogger.Always(__instance.Skillpoints + " awarded for killing Revenant");
                            SkillPointsForRevenantKillAwarded = true;
                        }
                        else
                        {
                            return;
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


