using Base.Core;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;

namespace TFTV
{
    internal class TFTVDeliriumPerks
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static bool doNotLocalize = false;
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

      
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

       

        private static readonly StatusDef frenzy = DefCache.GetDef<StatusDef>("Frenzy_StatusDef");
        private static readonly StatusDef anxiety = DefCache.GetDef<StatusDef>("Anxiety_StatusDef");

        private static readonly StatusDef ignorePain = DefCache.GetDef<StatusDef>("IgnoreDisabledLimbs_StatusDef");

        // private static readonly StatusDef ignorePain = DefCache.GetDef<StatusDef>("IgnorePain_StatusDef");
        private static readonly StatusDef mistResistance = DefCache.GetDef<StatusDef>("MistResistance_StatusDef");
        private static readonly GameTagDef mistResistanceTag = DefCache.GetDef<GameTagDef>("OneOfUsMistResistance_GameTagDef");


        public static void ImplementDeliriumPerks(TacticalActorBase actor, TacticalLevelController controller)
        {

            try
            {
                if (actor.TacticalFaction.Faction.BaseDef == sharedData.PhoenixFactionDef && !controller.IsFromSaveGame)
                {
                    TacticalActor tacticalActor = actor as TacticalActor;

                    if (actor.GetAbilityWithDef<Ability>(fasterSynapsesDef) != null)
                    {
                        tacticalActor.Status.ApplyStatus(frenzy);
                        TFTVLogger.Always(actor.DisplayName + " with " + fasterSynapsesDef.name);
                    }

                    if ((actor.GetAbilityWithDef<Ability>(newAnxietyDef) != null || actor.GetAbilityWithDef<Ability>(anxietyDef) != null) && !actor.Status.HasStatus(anxiety))
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

      

        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")] //VERIFIED
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

        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")] //VERIFIED
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static void Postfix(TacticalActor __instance, DamageResult damageResult)
            {

                try
                {
                  
                    TacticalAbility hyperalgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                   
                    if (__instance.IsAlive && hyperalgesia != null && damageResult.HealthDamage>=1)
                    {
                        __instance.CharacterStats.WillPoints.Subtract(1);                      
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

