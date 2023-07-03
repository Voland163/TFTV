using Base.Core;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVDeliriumPerks
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
     
        internal static bool doNotLocalize = false;
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

        private static readonly TacticalAbilityDef InnerSight_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("InnerSight_AbilityDef");
        private static readonly TacticalAbilityDef Terror_AbilityDef = DefCache.GetDef<TacticalAbilityDef>("Terror_AbilityDef");
        private static readonly TacticalAbilityDef feralDeliriumPerk = DefCache.GetDef<TacticalAbilityDef>("FeralNew_AbilityDef");
        private static readonly TacticalAbilityDef hyperalgesiaAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Hyperalgesia_AbilityDef");
        private static readonly TacticalAbilityDef feralAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Feral_AbilityDef");
        private static readonly TacticalAbilityDef bloodthirstyAbilityDef = DefCache.GetDef<TacticalAbilityDef>("Bloodthirsty_AbilityDef");
        private static readonly TacticalAbilityDef fasterSynapsesDef = DefCache.GetDef<TacticalAbilityDef>("FasterSynapses_AbilityDef");
        private static readonly TacticalAbilityDef anxietyDef = DefCache.GetDef<TacticalAbilityDef>("AnxietyAbilityDef");
        private static readonly TacticalAbilityDef newAnxietyDef = DefCache.GetDef<TacticalAbilityDef>("NewAnxietyAbilityDef");
        private static readonly TacticalAbilityDef oneOfThemDef = DefCache.GetDef<TacticalAbilityDef>("OneOfThemPassive_AbilityDef");
        private static readonly TacticalAbilityDef wolverineDef = DefCache.GetDef<TacticalAbilityDef>("Wolverine_AbilityDef");
        private static readonly TacticalAbilityDef derealizationDef = DefCache.GetDef<TacticalAbilityDef>("DerealizationIgnorePain_AbilityDef");
        private static readonly TacticalAbilityDef newDerealizationDef = DefCache.GetDef<TacticalAbilityDef>("Derealization_AbilityDef");

        private static readonly StatMultiplierStatusDef wolverinePassiveStatus = DefCache.GetDef<StatMultiplierStatusDef>("WolverinePassive_StatusDef");

        private static readonly List<TacticalAbilityDef> DeliriumPerks = new List<TacticalAbilityDef>() {InnerSight_AbilityDef, Terror_AbilityDef, feralDeliriumPerk, hyperalgesiaAbilityDef,
        feralAbilityDef, bloodthirstyAbilityDef, fasterSynapsesDef, anxietyDef, newAnxietyDef, oneOfThemDef, wolverineDef, derealizationDef, newDerealizationDef};

        private static readonly StatusDef frenzy = DefCache.GetDef<StatusDef>("Frenzy_StatusDef");
        private static readonly StatusDef anxiety = DefCache.GetDef<StatusDef>("Anxiety_StatusDef");
        private static readonly StatusDef ignorePain = DefCache.GetDef<StatusDef>("IgnorePain_StatusDef");
        private static readonly StatusDef mistResistance = DefCache.GetDef<StatusDef>("MistResistance_StatusDef");
        private static readonly GameTagDef mistResistanceTag = DefCache.GetDef<GameTagDef>("OneOfUsMistResistance_GameTagDef");




        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_DeliriumPerks_Patch
        {

            public static void Postfix(TacticalActorBase actor)
            {
                try
                {
                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.PhoenixFactionDef)
                    {
                        TacticalActor tacticalActor = actor as TacticalActor;

                        if (actor.GetAbilityWithDef<Ability>(fasterSynapsesDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(frenzy);
                            TFTVLogger.Always(actor.DisplayName + " with " + fasterSynapsesDef.name);
                        }

                        if ((actor.GetAbilityWithDef<Ability>(newAnxietyDef) != null || actor.GetAbilityWithDef<Ability>(anxietyDef)!=null)&&!actor.Status.HasStatus(anxiety))
                        {
                            tacticalActor.Status.ApplyStatus(anxiety);
                            TFTVLogger.Always(actor.DisplayName + " with " + newAnxietyDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(oneOfThemDef) != null && !actor.HasGameTag(mistResistanceTag))
                        {
                            tacticalActor.Status.ApplyStatus(mistResistance);
                            tacticalActor.GameTags.Add(mistResistanceTag, GameTagAddMode.ReplaceExistingExclusive);
                            TFTVLogger.Always(actor.DisplayName + " with " + oneOfThemDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(wolverineDef) != null && !actor.Status.HasStatus(wolverinePassiveStatus))
                        {
                            TFTVLogger.Always($"{actor.DisplayName} has accuracy of {tacticalActor.CharacterStats.Accuracy}");

                            tacticalActor.Status.ApplyStatus(wolverinePassiveStatus);
                        
                            //tacticalActor.AddAbility(wolverineDef, actor);
                            TFTVLogger.Always($"{actor.DisplayName} with {wolverineDef.name}, has accuracy of {tacticalActor.CharacterStats.Accuracy}");
                        }

                        if (actor.GetAbilityWithDef<Ability>(derealizationDef) != null)
                        {

                            tacticalActor.CharacterStats.Endurance.Value.ModificationValue -= 5;
                            tacticalActor.CharacterStats.Endurance.Max.ModificationValue -= 5;
                            tacticalActor.UpdateStats();


                            // tacticalActor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>("IgnorePain_AbilityDef")), actor);

                            TFTVLogger.Always(actor.DisplayName + " with " + derealizationDef.name);

                        }

                        if (actor.GetAbilityWithDef<Ability>(newDerealizationDef) != null && !actor.Status.HasStatus(ignorePain))
                        {

                            actor.Status.ApplyStatus(ignorePain);
                          

                            // tacticalActor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>("IgnorePain_AbilityDef")), actor);

                            TFTVLogger.Always($"{actor.DisplayName} should have ignore pain status");

                        }

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalAbility), "FumbleActionCheck")]
        public static class TacticalAbility_FumbleActionCheck_Patch
        {
            private static readonly TacticalAbilityDef feral = DefCache.GetDef<TacticalAbilityDef>("Feral_AbilityDef");
            public static void Postfix(TacticalAbility __instance, ref bool __result)
            {
                try
                {
                    if (__instance.TacticalActor.GetAbilityWithDef<TacticalAbility>(feral) != null && __instance.Source is Equipment)
                    {
                        __result = __result || UnityEngine.Random.Range(0, 100) < 20;
                        TFTVLogger.Always("The fumble action is " + __instance.GetAbilityDescription() + " and the fumble result is " + __result);
                    }
                    
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        

        /*
                [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
                public static class TacticalLevelController_ActorDied_HumanEnemiesTactics_BloodRush_Patch
                {
                    private static readonly TacticalAbilityDef feral =DefCache.GetDef<TacticalAbilityDef>("Feral_AbilityDef"));

                    public static void Postfix(DeathReport deathReport)
                    {
                        try
                        {
                            TacticalActorBase killer = deathReport.Killer;

                            if(killer.GetAbilityWithDef<TacticalAbility>(feral) != null) 
                            {
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                int roll = UnityEngine.Random.Range(0, 100);
                                TFTVLogger.Always("FumbleActionCheck roll is " + roll);

                                if (roll < 10) 
                                {
                                    TacticalActor tacticalActor = killer as TacticalActor;
                                    TFTVLogger.Always("Max action points are " + tacticalActor.CharacterStats.ActionPoints.Max);
                                    float maxActionPoints = tacticalActor.CharacterStats.ActionPoints.Max;
                                    tacticalActor.CharacterStats.ActionPoints.Subtract((maxActionPoints/4)*2);
                                    TFTVLogger.Always("Action points now " + tacticalActor.CharacterStats.ActionPoints.Value.EndValue);
                                }


                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }
        */
        /*
        [HarmonyPatch(typeof(TacticalAbility), "get_FumbledAction")]
        public static class TacticalAbility_FumbleActionCheck_Patch
        {
           private static readonly TacticalAbilityDef feral =DefCache.GetDef<TacticalAbilityDef>("Feral_AbilityDef"));

            public static bool Prefix(TacticalAbility __instance, ref bool __result)
            {
               
                try
                {
                   
                    TFTVLogger.Always("get_FumbledAction " + __instance.Source);

                    TFTVLogger.Always("The actor is " + __instance.TacticalActor.DisplayName + " and the ability is " + __instance.GetAbilityDescription());

                    if (__instance.TacticalActor.GetAbilityWithDef<TacticalAbility>(feral) != null && __instance.Source is Equipment)
                    {
                        
                         UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                          int roll = UnityEngine.Random.Range(0, 100);
                        TFTVLogger.Always("FumbleActionCheck roll is " + roll);

                        if (roll > 10)
                        {
                            // typeof(TacticalAbility).GetMethod("FumbleAction", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { __instance.ActionComponent. });
                            __result = true;
                            return false;
                        }
                        
                        return true;

                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
            }
        }
        */
        //Dtony's Delirium perks patch
        /*    [HarmonyPatch(typeof(RecruitsListElementController), "SetRecruitElement")]
            public static class RecruitsListElementController_SetRecruitElement_Patch
            {
                public static bool Prefix(RecruitsListElementController __instance, RecruitsListEntryData entryData, List<RowIconTextController> ____abilityIcons)
                {
                    try
                    {
                        if (____abilityIcons == null)
                        {
                            ____abilityIcons = new List<RowIconTextController>();
                            if (__instance.PersonalTrackRoot.transform.childCount < entryData.PersonalTrackAbilities.Count())
                            {
                                RectTransform parent = __instance.PersonalTrackRoot.GetComponent<RectTransform>();
                                RowIconTextController source = parent.GetComponentInChildren<RowIconTextController>();
                                parent.DetachChildren();
                                source.Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);
                                for (int i = 0; i < entryData.PersonalTrackAbilities.Count(); i++)
                                {
                                    RowIconTextController entry = UnityEngine.Object.Instantiate(source, parent, true);
                                }
                            }
                            UIUtil.GetComponentsFromContainer(__instance.PersonalTrackRoot.transform, ____abilityIcons);
                        }
                        __instance.RecruitData = entryData;
                        __instance.RecruitName.SetSoldierData(entryData.Recruit);
                        BC_SetAbilityIcons(entryData.PersonalTrackAbilities.ToList(), ____abilityIcons);
                        if (entryData.SuppliesCost != null && __instance.CostText != null && __instance.CostColorController != null)
                        {
                            __instance.CostText.text = entryData.SuppliesCost.ByResourceType(ResourceType.Supplies).RoundedValue.ToString();
                            __instance.CostColorController.SetWarningActive(!entryData.IsAffordable, true);
                        }
                        __instance.NavHolder.RefreshNavigation();
                        return false;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return true;
                    }
                }


                private static void BC_SetAbilityIcons(List<TacticalAbilityViewElementDef> abilities, List<RowIconTextController> abilityIcons)
                {
                    foreach (RowIconTextController rowIconTextController in abilityIcons)
                    {
                        rowIconTextController.gameObject.SetActive(false);
                    }
                    for (int i = 0; i < abilities.Count; i++)
                    {
                        abilityIcons[i].gameObject.SetActive(true);
                        abilityIcons[i].SetController(abilities[i].LargeIcon, abilities[i].DisplayName1, abilities[i].Description);
                    }
                }
            }*/

        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_Patch
        {

            public static void Postfix(TacticalActor __instance, DeathReport death)
            {

                try
                {

                    TacticalAbility hyperAlgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                    TacticalAbility feral = __instance.GetAbilityWithDef<TacticalAbility>(feralAbilityDef);
                    TacticalAbility bloodthirsty = __instance.GetAbilityWithDef<TacticalAbility>(bloodthirstyAbilityDef);

                    if (hyperAlgesia != null)
                    {
                        TacticalFaction tacticalFaction = death.Actor.TacticalFaction;
                        int willPointWorth = death.Actor.TacticalActorBaseDef.WillPointWorth;
                        if (death.Actor.TacticalFaction == __instance.TacticalFaction)
                        {
                            __instance.CharacterStats.WillPoints.Add(willPointWorth);
                        }
                    }
                    if (feral != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.ActionPoints.Add(__instance.CharacterStats.ActionPoints.Max / 4);
                    }
                    if (bloodthirsty != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.Health.AddRestrictedToMax(death.Actor.Health.Max / 2);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static void Postfix(TacticalActor __instance)
            {


                try
                {
                    bool receivedDamage = false;

                    TacticalAbility hyperalgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                    //  TacticalAbility derealization = __instance.GetAbilityWithDef<TacticalAbility>(derealizationDef);
                    if (__instance.IsAlive && hyperalgesia != null && !receivedDamage)
                    {
                        __instance.CharacterStats.WillPoints.Subtract(1);
                        receivedDamage = true;
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

