﻿using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Missions;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static PhoenixPoint.Tactical.Entities.Abilities.CallReinforcementsAbilityDef;

namespace TFTV
{
    internal class TFTVTouchedByTheVoid
    {

        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static string TBTVVariableName = "Umbra_Encounter_Variable";
        public static int TBTVVariable = 0;
        public static bool UmbraResearched = false;

        public static readonly AddAbilityStatusDef hiddenTBTVAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("TBTV_Hidden_AddAbilityStatusDef");
        public static readonly DamageMultiplierStatusDef onAttackTBTVStatus = DefCache.GetDef<DamageMultiplierStatusDef>("TBTV_OnAttack_StatusDef");
        public static readonly DamageMultiplierStatusDef onTurnEndTBTVStatus = DefCache.GetDef<DamageMultiplierStatusDef>("TBTV_OnTurnEnd_StatusDef");
        public static readonly AddAbilityStatusDef oilCrabAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
        public static readonly AddAbilityStatusDef oilTritonAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilFish_AddAbilityStatusDef");

        public static readonly List <StatusDef> tbtvStatuses = new List <StatusDef>() 
        { 
        hiddenTBTVAddAbilityStatus, onAttackTBTVStatus, onTurnEndTBTVStatus,oilCrabAddAbilityStatus, oilTritonAddAbilityStatus        
        };

        private static readonly PassiveModifierAbilityDef acheronTributary = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Tributary_AbilityDef");

        private static readonly PassiveModifierAbilityDef hiddenTBTVAbilityDef = DefCache.GetDef<PassiveModifierAbilityDef>("TBTV_Hidden_AbilityDef");
        private static readonly PassiveModifierAbilityDef acheronHarbinger = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Harbinger_AbilityDef");
        private static readonly GameTagDef voidTouchedTag = DefCache.GetDef<GameTagDef>("VoidTouched_GameTagDef");
        private static readonly GameTagDef voidTouchedOnAttackTag = DefCache.GetDef<GameTagDef>("VoidTouchedOnAttack_GameTagDef");
        private static readonly GameTagDef voidTouchedOnTurnEndTag = DefCache.GetDef<GameTagDef>("VoidTouchedOnTurnEnd_GameTagDef");

        private static readonly HitPenaltyStatusDef mFDStatus = DefCache.GetDef<HitPenaltyStatusDef>("E_PureDamageBonusStatus [MarkedForDeath_AbilityDef]");
        public static WeaponDef umbraCrab = DefCache.GetDef<WeaponDef>("Oilcrab_Torso_BodyPartDef");
        public static BodyPartAspectDef umbraCrabBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Oilcrab_Torso_BodyPartDef]");
        public static WeaponDef umbraFish = DefCache.GetDef<WeaponDef>("Oilfish_Torso_BodyPartDef");
        public static BodyPartAspectDef umbraFishBodyAspect = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [Oilfish_Torso_BodyPartDef]");

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

        private static readonly DeathBelcherAbilityDef oilcrabDeathBelcherAbility = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef");
        private static readonly DeathBelcherAbilityDef oilfishDeathBelcherAbility = DefCache.GetDef<DeathBelcherAbilityDef>("Oilfish_Die_DeathBelcher_AbilityDef");

        private static readonly GameTagDef anyRevenantGameTag = TFTVRevenant.AnyRevenantGameTag; //DefCache.GetDef<GameTagDef>("Any_Revenant_TagDef");



        internal class TBTVRolls
        {
            
            public static int MakeTBTVRoll()
            {
                try
                {
                    int rollCap = 34;

                    TFTVLogger.Always("TBTV variable is " + TBTVVariable);

                    if (TBTVVariable >= 4)
                    {
                        rollCap = 101;
                    }
                    else if (TBTVVariable >= 2)
                    {
                        rollCap = 67;
                    }

                    TFTVLogger.Always("rollCap is " + rollCap);

                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(1, rollCap);
                    TFTVLogger.Always("The TBTV roll is " + roll);

                    return roll;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return 1;
                }
            }

            public static bool CheckForTBTVAbilities(TacticalActor tacticalActor)
            {
                try
                {
                    if (tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(oilcrabDeathBelcherAbility) != null ||
                        tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(oilfishDeathBelcherAbility) != null ||
                        tacticalActor.HasGameTag(voidTouchedOnAttackTag) ||
                        tacticalActor.HasGameTag(voidTouchedOnTurnEndTag))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return false;
                }
            }


           


            public static void GiveTBTVAbility(TacticalActor tacticalActor, int roll)
            {
                try
                {
                    //1-30 Umbra, 31-36 dud,37-66 MfD, 67-96 Reinforcements, 97-100 dud  

                    if (roll > 37 && roll <= 66)
                    {
                        if (!tacticalActor.HasGameTag(voidTouchedOnAttackTag) && !tacticalActor.HasGameTag(voidTouchedOnTurnEndTag))
                        {
                            tacticalActor.Status.ApplyStatus(onAttackTBTVStatus);
                            tacticalActor.GameTags.Add(voidTouchedOnAttackTag);

                            TFTVLogger.Always("MfD status applied " + tacticalActor.name);
                        }
                    }
                    else if (roll > 66 && roll <= 96)
                    {
                        if (!tacticalActor.HasGameTag(voidTouchedOnAttackTag) && !tacticalActor.HasGameTag(voidTouchedOnTurnEndTag))
                        {
                            tacticalActor.Status.ApplyStatus(onTurnEndTBTVStatus);
                            tacticalActor.GameTags.Add(voidTouchedOnTurnEndTag);

                            TacticalFactionVision.IncrementKnownCounterToAll(tacticalActor, KnownState.Revealed, 1, true);

                            TFTVLogger.Always($"CallReinforcements status applied {tacticalActor.name}, should be revealed to all!");
                        }
                    }
                    else if ((roll >= 31 && roll <= 36) || roll >= 97)
                    {
                        TFTVLogger.Always(tacticalActor.name + " had a dud TBTV!");
                        return;
                    }
                    else
                    {
                        if (!tacticalActor.HasGameTag(voidTouchedOnAttackTag) && !tacticalActor.HasGameTag(voidTouchedOnTurnEndTag))
                        {
                            if (tacticalActor.HasGameTag(crabTag))
                            {
                                tacticalActor.Status.ApplyStatus(oilCrabAddAbilityStatus);
                                TFTVLogger.Always("Spawn Umbra crab status applied " + tacticalActor.name);
                            }
                            else if (tacticalActor.HasGameTag(fishTag))
                            {
                                tacticalActor.Status.ApplyStatus(oilTritonAddAbilityStatus);
                                TFTVLogger.Always("Spawn Umbra fish status applied " + tacticalActor.name);
                            }
                            else
                            {
                                int roll2 = UnityEngine.Random.Range(1, 11);
                                if (roll2 <= 5)
                                {
                                    tacticalActor.Status.ApplyStatus(oilTritonAddAbilityStatus);
                                    TFTVLogger.Always("Spawn Umbra fish status applied " + tacticalActor.name);
                                }
                                else
                                {
                                    tacticalActor.Status.ApplyStatus(oilCrabAddAbilityStatus);
                                    TFTVLogger.Always("Spawn Umbra crab status applied " + tacticalActor.name);
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


            internal class TBTVTriggeres
            {
                public static void TouchByTheVoidDeath(DeathReport deathReport)
                {
                    try
                    {
                        if (deathReport.Actor != null && deathReport.Actor is TacticalActor)
                        {
                            TacticalActor actor = deathReport.Actor as TacticalActor;

                            if (actor.GameTags.Contains(voidTouchedTag))
                            {
                                actor.RemoveAbility(hiddenTBTVAbilityDef);
                                actor.GameTags.Remove(voidTouchedTag);
                                int roll = MakeTBTVRoll();

                                if (roll > 30)
                                {
                                    TFTVLogger.Always($"{actor.name} getting TBTV on death");
                                    RemoveDeathBelcherAbilities(actor);
                                    GiveTBTVAbility(actor, roll);
                                }
                            }

                            if (actor.GetAbilityWithDef<PassiveModifierAbility>(acheronTributary) != null)
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in actor.TacticalFaction.Actors)
                                {
                                    if (allyTacticalActorBase.InPlay && allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                    {
                                        TacticalActor tacticalActor = allyTacticalActorBase as TacticalActor;
                                        float magnitude = 10;

                                        if ((allyTacticalActorBase.Pos - actor.Pos).magnitude <= magnitude)
                                        {
                                            if (tacticalActor.GameTags.Contains(crabTag) && !tacticalActor.GameTags.Contains(voidTouchedTag)
                                                && !tacticalActor.name.Contains("Oilcrab") && !tacticalActor.GameTags.Contains(anyRevenantGameTag)
                                                && !tacticalActor.GameTags.Contains(voidTouchedOnTurnEndTag) && !tacticalActor.GameTags.Contains(voidTouchedOnAttackTag)
                                                && !tacticalActor.HasStatus(oilCrabAddAbilityStatus))
                                            {
                                                tacticalActor.Status.ApplyStatus(hiddenTBTVAddAbilityStatus);
                                                if (!tacticalActor.HasGameTag(voidTouchedTag))
                                                {
                                                    tacticalActor.GameTags.Add(voidTouchedTag);
                                                    tacticalActor.AddAbility(oilcrabDeathBelcherAbility, tacticalActor);
                                                }
                                                TFTVLogger.Always("The actor who will receive TBTV from the Tributary is " + tacticalActor.name);
                                            }

                                            else if (tacticalActor.GameTags.Contains(fishTag) && tacticalActor.GameTags.Contains(voidTouchedTag)
                                                && !tacticalActor.name.Contains("Oilfish") && !actor.GameTags.Contains(anyRevenantGameTag)
                                                 && !tacticalActor.GameTags.Contains(voidTouchedOnTurnEndTag) && !tacticalActor.GameTags.Contains(voidTouchedOnAttackTag)
                                                && !tacticalActor.HasStatus(oilTritonAddAbilityStatus))
                                            {

                                                tacticalActor.Status.ApplyStatus(hiddenTBTVAddAbilityStatus);
                                                if (!tacticalActor.HasGameTag(voidTouchedTag))
                                                {
                                                    tacticalActor.GameTags.Add(voidTouchedTag);
                                                    tacticalActor.AddAbility(oilfishDeathBelcherAbility, tacticalActor);
                                                }
                                                TFTVLogger.Always("The actor who will receive TBTV from the Tributary is " + tacticalActor.name);
                                            }
                                            else if (!tacticalActor.GameTags.Contains(voidTouchedTag)
                                                && !tacticalActor.name.Contains("Oilfish") && !tacticalActor.name.Contains("Oilcrab") && !tacticalActor.GameTags.Contains(anyRevenantGameTag)
                                                 && !tacticalActor.GameTags.Contains(voidTouchedOnTurnEndTag) && !tacticalActor.GameTags.Contains(voidTouchedOnAttackTag)
                                                && !tacticalActor.HasStatus(oilCrabAddAbilityStatus) && !tacticalActor.HasStatus(oilTritonAddAbilityStatus))
                                            {

                                                tacticalActor.Status.ApplyStatus(hiddenTBTVAddAbilityStatus);
                                                if (!tacticalActor.HasGameTag(voidTouchedTag))
                                                {
                                                    tacticalActor.GameTags.Add(voidTouchedTag);
                                                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                                    int roll = UnityEngine.Random.Range(1, 11);
                                                    if (roll <= 5)
                                                    {
                                                        tacticalActor.AddAbility(oilfishDeathBelcherAbility, tacticalActor);
                                                    }
                                                    else
                                                    {
                                                        tacticalActor.AddAbility(oilcrabDeathBelcherAbility, tacticalActor);
                                                    }
                                                    TFTVLogger.Always("The actor who will receive TBTV from the Tributary is " + tacticalActor.name);
                                                }

                                            }
                                        }
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

                [HarmonyPatch(typeof(TacticalActor), "OnEquipmentHealthChange")]
                public static class TacticalActor_OnEquipmentHealthChange_TBTV_Patch
                {
                    public static void Prefix(TacticalActor __instance)
                    {
                        try
                        {
                            if (__instance.GameTags.Contains(voidTouchedTag))
                            {
                                TFTVLogger.Always($"{__instance.name} getting TBTV ability on equipment damaged");

                                RemoveTouchedByTheVoid(__instance);
                                int roll = MakeTBTVRoll();
                                GiveTBTVAbility(__instance, roll);
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }

                public static void TBTVTriggerOnActorDamageDealt(TacticalActor actor, IDamageDealer damageDealer)
                {
                    try
                    {
                        if (actor == null) 
                        {
                            return;
                        }

                        if (actor.GameTags!=null && actor.GameTags.Contains(voidTouchedTag))
                        {
                            TFTVLogger.Always($"{actor.name} getting TBTV on getting damaged");
                            RemoveTouchedByTheVoid(actor);
                            int roll = MakeTBTVRoll();
                            GiveTBTVAbility(actor, roll);
                        }

                        if (actor.Status!=null && actor.HasStatus(onAttackTBTVStatus) && damageDealer != null)
                        {
                            TacticalActorBase attackerBase = damageDealer.GetTacticalActorBase();
                            TacticalActor attacker = attackerBase as TacticalActor;

                            if (!attacker.Status.HasStatus(mFDStatus) && actor.TacticalFaction != attacker.TacticalFaction)
                            {
                                attacker.Status.ApplyStatus(mFDStatus);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                private static void RemoveTouchedByTheVoid(TacticalActor actor)
                {
                    try 
                    {
                        RemoveDeathBelcherAbilities(actor);
                        actor.RemoveAbility(hiddenTBTVAbilityDef);

                        Status hiddenTBTVStatus = actor.Status.GetStatusByName(hiddenTBTVAddAbilityStatus.EffectName);

                        if (hiddenTBTVAddAbilityStatus != null)
                        {
                            actor.Status.UnapplyStatus(hiddenTBTVStatus);
                        }

                        actor.GameTags.Remove(voidTouchedTag);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }

                }

                private static void RemoveDeathBelcherAbilities(TacticalActor tacticalActor)
                {
                    try
                    {
                        if (tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(oilcrabDeathBelcherAbility) != null)
                        {
                            tacticalActor.RemoveAbility(oilcrabDeathBelcherAbility);
                        }
                        else if (tacticalActor.GetAbilityWithDef<DeathBelcherAbility>(oilfishDeathBelcherAbility) != null)
                        {
                            tacticalActor.RemoveAbility(oilfishDeathBelcherAbility);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }
            }
        }

        internal class Umbra
        {



            internal class UmbraGeoscape
            {
                public static void CheckForUmbraResearch(GeoLevelController level)
                {
                    try
                    {
                        if (level.EventSystem.GetVariable("UmbraResearched") > 0)
                        {
                            UmbraResearched = true;
                        }
                        TBTVVariable = level.EventSystem.GetVariable(TBTVVariableName);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void ImplementUmbraEvolution(int healthPoints, int standardDamageAttack, int pierceDamageAttack)
                {
                    try
                    {
                        umbraCrab.HitPoints = healthPoints;
                        umbraCrab.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
                        umbraCrab.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;

                        umbraCrabBodyAspect.Endurance = (healthPoints / 10);
                        umbraCrabBodyAspect.Speed = 20;

                        umbraFish.HitPoints = healthPoints * 0.75f;
                        umbraFish.DamagePayload.DamageKeywords[0].Value = standardDamageAttack;
                        umbraFish.DamagePayload.DamageKeywords[1].Value = pierceDamageAttack;
                        umbraFishBodyAspect.Endurance = (healthPoints * 0.75f / 10);
                        umbraFishBodyAspect.Speed = 25;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void SetUmbraEvolution(GeoLevelController level)
                {
                    try
                    {
                        if (level.EventSystem.GetVariable(TBTVVariableName) == 3 || level.EventSystem.GetVariable(TBTVVariableName) == 4)
                        {
                            ImplementUmbraEvolution(125 * TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order), 20 * TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order), 20);
                        }
                        else if (level.EventSystem.GetVariable(TBTVVariableName) == 1 || level.EventSystem.GetVariable(TBTVVariableName) == 2)
                        {
                            ImplementUmbraEvolution(80 * TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order), 20 * TFTVSpecialDifficulties.DifficultyOrderConverter(level.CurrentDifficultyLevel.Order), 0);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }

            internal class UmbraTactical
            {
                public static void CheckVO15(TacticalLevelController controller, TacticalFaction faction)
                {
                    try
                    {
                        if (TFTVVoidOmens.VoidOmensCheck[15] &&
                            controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) &&
                            controller.GetFactionByCommandName("Px") == faction && !controller.IsLoadingSavedGame)
                        {
                            TFTVLogger.Always($"More Umbras VO in effect; killing Umbra at start of player's turn");

                            foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("Aln").
                                     TacticalActors.Where(ta => ta.IsAlive && !ta.Status.HasStatus<PreparingStatus>() && (ta.ActorDef.name.Equals("Oilcrab_ActorDef") || ta.ActorDef.name.Equals("Oilfish_ActorDef"))))
                            {
                                tacticalActor.ApplyDamage(new DamageResult { HealthDamage = 1000 });
                                TFTVLogger.Always($"More Umbras VO in effect; killing {tacticalActor.name} at start of player's turn");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                public static int GetTouchedByTheVoidChances(TacticalLevelController controller)
                {
                    try
                    {
                        TacticalFaction phoenix = controller.GetFactionByCommandName("px");

                        int totalDeliriumOnMission = 0;

                        if (TFTVVoidOmens.VoidOmensCheck[16])
                        {
                            int baseChance = 16;

                            if (TFTVVoidOmens.VoidOmensCheck[15])
                            {
                             baseChance = 32;
                            }

                           
                                totalDeliriumOnMission = baseChance + CheckForAcheronHarbingers(controller) * 10;
                          
                        }
                        else
                        {
                            totalDeliriumOnMission = 0;

                            foreach (TacticalActor actor in phoenix.TacticalActors)
                            {
                                if (actor.CharacterStats.Corruption.Value > 0)
                                {
                                    totalDeliriumOnMission += (int)actor.CharacterStats.Corruption.Value.BaseValue;
                                }
                            }

                            if (!TFTVVoidOmens.VoidOmensCheck[15])
                            {
                                totalDeliriumOnMission /= 2;
                            }
                           
                                totalDeliriumOnMission += CheckForAcheronHarbingers(controller) * 10;
                            
                           
                        }

                        return totalDeliriumOnMission;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }

                }

                public static void RollTouchByTheVoid(TacticalLevelController controller)
                {
                    try
                    {
                        if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                        {
                            if (UmbraResearched)
                            {
                                if (!TFTVVoidOmens.VoidOmensCheck[16])
                                {
                                    TacticalFaction phoenix = controller.GetFactionByCommandName("px");
                                    TacticalFaction pandorans = controller.GetFactionByCommandName("aln");

                                    int totalDeliriumOnMission=GetTouchedByTheVoidChances(controller);

                                    foreach (TacticalActor actor in pandorans.TacticalActors)
                                    {

                                        // TFTVLogger.Always("The actor is " + actor.name);
                                        if ((actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishTag)) && !actor.GameTags.Contains(voidTouchedTag)
                                            && !actor.name.Contains("Oilcrab") && !actor.name.Contains("Oilfish") && !actor.GameTags.Contains(anyRevenantGameTag) && !TBTVRolls.CheckForTBTVAbilities(actor))

                                        {
                                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                            int roll = UnityEngine.Random.Range(1, 101);
                                            if (roll <= totalDeliriumOnMission)
                                            {
                                                TFTVLogger.Always("This Arthron here " + actor + ", got past the TBTV check!");
                                                actor.Status.ApplyStatus(hiddenTBTVAddAbilityStatus);
                                                actor.GameTags.Add(voidTouchedTag);
                                                actor.AddAbility(oilcrabDeathBelcherAbility, actor);
                                            }
                                        }

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

                public static int CheckForAcheronHarbingers(TacticalLevelController controller, bool checkVisibleOnly = false)
                {
                    try
                    {
                        if (!controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln"))) 
                        {
                            return 0;
                        }

                            TacticalFaction pandoranFaction = controller.GetFactionByCommandName("aln");
                        int harbingers = 0;

                        List<TacticalActor> pandorans = pandoranFaction.TacticalActors.ToList();

                        if (checkVisibleOnly)
                        {

                            IEnumerable<TacticalActor> tacticalActors = from a in controller.GetFactionByCommandName("px").Vision.GetKnownActors(KnownState.Revealed, FactionRelation.Enemy, false).OfType<TacticalActor>()
                                                                        where a.InPlay && a.Interactable
                                                                        select a;

                            pandorans = tacticalActors.ToList();
                        }

                        foreach (TacticalActor actor in pandorans)
                        {
                            if (actor.IsAlive && actor.GetAbilityWithDef<PassiveModifierAbility>(acheronHarbinger) != null)
                            {
                               // TFTVLogger.Always("This harbinger is " + actor.name);
                                harbingers++;
                            }

                        }
                        return harbingers;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return 0;
                    }
                }


                //To prevent Umbras from attacking characters without Delirium
                public static void ImplementUmbraTargeting(ref IEnumerable<TacticalAbilityTarget> __result, TacticalActorBase sourceActor)
                {
                    try
                    {
                        //Design choice to allow decoys to be targeted by Umbra if decoy is in mist 
                        //  SpawnedActorTagDef decoyTag = DefCache.GetDef<SpawnedActorTagDef>("Decoy_SpawnedActorTagDef");

                        if (!TFTVVoidOmens.VoidOmensCheck[16])
                        {
                            if (sourceActor.ActorDef.name.Equals("Oilcrab_ActorDef") || sourceActor.ActorDef.name.Equals("Oilfish_ActorDef"))
                            {
                                List<TacticalAbilityTarget> list = new List<TacticalAbilityTarget>(); // = __result.ToList();
                                                                                                      //list.RemoveWhere(adilityTarget => (adilityTarget.Actor as TacticalActor)?.CharacterStats.Corruption <= 0);
                                foreach (TacticalAbilityTarget target in __result)
                                {
                                    if (target.Actor is TacticalActor && ((target.Actor as TacticalActor).CharacterStats.Corruption > 0 || (target.Actor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist)))) //&& !source.Actor.HasGameTag(decoyTag))))
                                    {
                                        list.Add(target);
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


                public static void UmbraEverywhereVoidOmenImplementation(TacticalActorBase actor, TacticalLevelController controller)
                {

                    try
                    {
                        // TFTVLogger.Always("ActorEnteredPlay invoked");
                        if (UmbraResearched)
                        {
                            if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")) && TFTVVoidOmens.VoidOmensCheck[16])
                            {

                                int totalDeliriumOnMission = GetTouchedByTheVoidChances(controller);

                                //   TFTVLogger.Always("found aln faction and checked that VO is in place");

                                if ((actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishTag)) && !actor.GameTags.Contains(voidTouchedTag)
                                && !actor.name.Contains("Oilcrab") && !actor.name.Contains("Oilfish") && !actor.GameTags.Contains(anyRevenantGameTag)
                                && actor.TacticalFaction.Faction.FactionDef.MatchesShortName("aln") && !TBTVRolls.CheckForTBTVAbilities(actor as TacticalActor))

                                {
                                    TacticalActor tacticalActor = actor as TacticalActor;

                                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                    int roll = UnityEngine.Random.Range(1, 101);
                                    // TFTVLogger.Always("The roll is " + roll);
                                    if (roll <= totalDeliriumOnMission)
                                    {
                                        TFTVLogger.Always("VO16+VO15 This Arthron here " + actor + ", got past the TBTV check!");
                                        tacticalActor.Status.ApplyStatus(hiddenTBTVAddAbilityStatus);

                                        actor.GameTags.Add(voidTouchedTag);
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
            }
        }

        internal class TBTVCallReinforcements
        {
            public static void ImplementCallReinforcementsTBTV(TacticalFaction tacticalFaction)
            {
                try
                {
                    if (tacticalFaction.TacticalLevel.TurnNumber > 0 && tacticalFaction.TacticalLevel.GetFactionByCommandName("PX") == tacticalFaction)
                    {
                        ActivateReinforcementAbility(tacticalFaction.TacticalLevel);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static Dictionary<ClassTagDef, int> PickReinforcements(TacticalLevelController controller)
            {
                try
                {
                    ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                    ClassTagDef mindfraggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");
                    Dictionary<ClassTagDef, int> reinforcements = new Dictionary<ClassTagDef, int>();
                    List<ClassTagDef> eligibleClassTagDefs = new List<ClassTagDef>();


                    int difficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);

                    foreach (TacCharacterDef tacCharacterDef in controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs)
                    {
                        if (tacCharacterDef.ClassTag != null && !eligibleClassTagDefs.Contains(tacCharacterDef.ClassTag))

                        {
                            //  TFTVLogger.Always("ClassTag " + tacCharacterDef.ClassTag.className + " added");
                            eligibleClassTagDefs.Add(tacCharacterDef.ClassTag);

                        }
                    }

                    if (difficulty > 0)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(1, 2);

                        if (roll == 1)
                        {
                            reinforcements.Add(crabTag, 1);
                        }
                        else
                        {
                            reinforcements.Add(fishTag, 1);
                        }
                    }

                    if (difficulty > 2)
                    {
                        if (eligibleClassTagDefs.Contains(myrmidonTag))
                        {
                            reinforcements.Add(myrmidonTag, 1);

                        }
                        else
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int roll = UnityEngine.Random.Range(1, 2);

                            if (roll == 1)
                            {
                                if (reinforcements.ContainsKey(crabTag))
                                {
                                    reinforcements[crabTag] += 1;
                                }
                                else
                                {
                                    reinforcements.Add(crabTag, 1);
                                }
                            }
                            else
                            {
                                if (reinforcements.ContainsKey(fishTag))
                                {
                                    reinforcements[fishTag] += 1;
                                }
                                else
                                {
                                    reinforcements.Add(fishTag, 1);
                                }
                            }
                        }
                    }

                    if (difficulty > 3)
                    {
                        if (eligibleClassTagDefs.Contains(sirenTag) && eligibleClassTagDefs.Contains(myrmidonTag))
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int roll = UnityEngine.Random.Range(1, 2);

                            if (roll == 1)
                            {
                                reinforcements.Add(sirenTag, 1);
                            }
                            else
                            {
                                if (reinforcements.ContainsKey(myrmidonTag))
                                {
                                    reinforcements[myrmidonTag] += 1;
                                }
                                else
                                {
                                    reinforcements.Add(myrmidonTag, 1);
                                }
                            }

                        }
                        else if (eligibleClassTagDefs.Contains(sirenTag) && !eligibleClassTagDefs.Contains(myrmidonTag))
                        {

                            reinforcements.Add(sirenTag, 1);

                        }
                        else if (!eligibleClassTagDefs.Contains(sirenTag) && eligibleClassTagDefs.Contains(myrmidonTag))
                        {
                            if (reinforcements.ContainsKey(myrmidonTag))
                            {
                                reinforcements[myrmidonTag] += 1;
                            }
                            else
                            {
                                reinforcements.Add(myrmidonTag, 1);
                            }

                        }
                        else
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                            int roll = UnityEngine.Random.Range(1, 2);

                            if (roll == 1)
                            {
                                if (reinforcements.ContainsKey(crabTag))
                                {
                                    reinforcements[crabTag] += 1;
                                }
                                else
                                {
                                    reinforcements.Add(crabTag, 1);
                                }
                            }
                            else
                            {
                                if (reinforcements.ContainsKey(fishTag))
                                {
                                    reinforcements[fishTag] += 1;
                                }
                                else
                                {
                                    reinforcements.Add(fishTag, 1);
                                }
                            }
                        }
                    }

                    return reinforcements;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return new Dictionary<ClassTagDef, int>() { { crabTag, 1 } };
                }

            }

            public static void ActivateReinforcementAbility(TacticalLevelController controller)
            {
                try
                {

                    ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                    ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");

                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        TacticalFaction pandoranFaction = controller.GetFactionByCommandName("aln");
                        TacticalFaction wildFaction = controller.GetFactionByCommandName("wild");
                        List<TacticalActor> pandoransCallingReinforcements = new List<TacticalActor>();

                        foreach (TacticalActor actor in pandoranFaction.TacticalActors)
                        {

                            //    TFTVLogger.Always("The actor is " + actor.name);
                            if ((actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishTag)) &&
                                actor.HasStatus(onTurnEndTBTVStatus) && actor.IsAlive)
                            {
                                pandoransCallingReinforcements.Add(actor);
                            }
                        }

                        foreach (TacticalActor actor in wildFaction.TacticalActors)
                        {

                            //    TFTVLogger.Always("The actor is " + actor.name);
                            if ((actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishTag)) &&
                                actor.HasStatus(onTurnEndTBTVStatus) && actor.IsAlive)
                            {
                                pandoransCallingReinforcements.Add(actor);
                            }
                        }


                        if (pandoransCallingReinforcements.Count > 0)
                        {
                            foreach (TacticalActor actor in pandoransCallingReinforcements)
                            {
                                Dictionary<ClassTagDef, int> reinforcements = PickReinforcements(controller);

                                foreach (ClassTagDef classTag in reinforcements.Keys)
                                {

                                    ReinforcementSettings reinforcement = new ReinforcementSettings()
                                    {
                                        CharacterTagDef = classTag,
                                        NumberOfReinforcements = new Base.Utils.RangeDataInt()
                                        { Min = reinforcements[classTag], Max = reinforcements[classTag] }
                                    };

                                    // TFTVLogger.Always("Reinforcements should be called, with classTag " + classTag.className);
                                    CallReinforcementsAbility callReinforcementsAbility = new CallReinforcementsAbility();


                                    //  actor.GetAbilityWithDef<CallReinforcementsAbility>(callReinforcementsTBTVAbilityDef);
                                    TacParticipantSpawn participantSpawn = controller.TacMission.ParticipantSpawns.First((TacParticipantSpawn ps) => ps.TacticalFaction == pandoranFaction); //ps.ParticipantKind == actor.MissionParticipant);
                                                                                                                                                                                             // ReinforcementSettings[] reinforcementsSettings = callReinforcementsAbility.Def<CallReinforcementsAbilityDef>().ReinforcementsSettings;

                                    MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(CallReinforcementsAbility), "DeployReinforcement");
                                    method_GenerateTargetData.Invoke(callReinforcementsAbility, new object[] { participantSpawn, reinforcement });
                                }

                                actor.Status.UnapplyStatus(actor.Status.GetStatusByName(onTurnEndTBTVStatus.EffectName));

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























        /* public static void CheckForActorsInMist(TacticalLevelController controller)
         {
             try
             {
                 foreach (TacticalFaction tacticalFaction in controller.Factions)
                 {
                     if (tacticalFaction != controller.GetFactionByCommandName("aln"))
                     {
                         foreach (TacticalActorBase tacticalActorBase in tacticalFaction.Actors)
                         {
                             if (tacticalActorBase is TacticalActor)
                             {
                                 TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                                 if (tacticalActorBase.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist))
                                 {

                                     if (!tacticalActor.HasStatus(umbraTargetStatusDef))
                                     {
                                         TFTVLogger.Always("The target is " + tacticalActor.name);
                                         tacticalActor.Status.ApplyStatus(umbraTargetStatusDef);
                                     }
                                 }
                                 else 
                                 {
                                     if (tacticalActor.HasStatus(umbraTargetStatusDef))
                                     {
                                         TFTVLogger.Always("The target is " + tacticalActor.name);
                                         Status status = tacticalActor.Status.GetStatus<Status>(umbraTargetStatusDef);
                                         tacticalActor.Status.Statuses.Remove(status);

                                     }
                                 }
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
        */

        /* [HarmonyPatch(typeof(CallReinforcementsAbility), "CallReinforcementsCrt")]

         public static class CallReinforcementsAbility_CallReinforcementsCrt_TBTV_Patch
         {
             public static bool Prefix(CallReinforcementsAbility __instance)
             {
                 try
                 {
                     if (__instance.ActorTags.Contains(DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef")))
                     {
                         return true;

                     }
                     else
                     {
                         TacParticipantSpawn participantSpawn = __instance.TacticalActor.TacticalLevel.TacMission.ParticipantSpawns.First((TacParticipantSpawn ps) => ps.ParticipantKind == __instance.TacticalActor.MissionParticipant);
                         CallReinforcementsAbilityDef.ReinforcementSettings[] reinforcementsSettings = __instance.Def<CallReinforcementsAbilityDef>().ReinforcementsSettings;
                         foreach (CallReinforcementsAbilityDef.ReinforcementSettings reinforcementSettings in reinforcementsSettings)
                         {
                             MethodInfo method_GenerateTargetData = AccessTools.Method(typeof(CallReinforcementsAbility), "DeployReinforcement");
                             method_GenerateTargetData.Invoke(__instance, new object[] { participantSpawn, reinforcementSettings });
                         }
                         return false;
                     }

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     return true;
                 }



             }
         }*/

        //need to exclude curespray

        /*
        [HarmonyPatch(typeof(ApplyEffectAbility), "ApplyEffectCrt")]
        internal static class ApplyEffectAbility_ApplyStatusCrt_AcheronTBTV_patch
        {

            public static void Postfix(PlayingAction action, ApplyEffectAbility __instance)
            {
                try
                {
                    ApplyEffectAbilityDef cureCloud = DefCache.GetDef<ApplyEffectAbilityDef>("Acheron_CureCloud_ApplyEffectAbilityDef");
                    ApplyEffectAbilityDef restoreArmor = DefCache.GetDef<ApplyEffectAbilityDef>("Acheron_RestorePandoranArmor_AbilityDef");

                    if (__instance.TacticalActor != null && __instance.TacticalActor.HasGameTag(acheronTag) && !__instance.AbilityDef.Equals(cureCloud) && !__instance.AbilityDef.Equals(restoreArmor))
                    {
                        TFTVLogger.Always("Acheron attack");

                        TacticalAbilityTarget abilityTarget = (TacticalAbilityTarget)action.Param;

                        if (__instance.ApplyEffectAbilityDef.ApplyToAllTargets)
                        {
                            foreach (TacticalAbilityTarget targetActor in __instance.GetTargetActors(__instance.OriginTargetData))
                            {
                                if (targetActor.Actor is TacticalActor)
                                {
                                    TFTVLogger.Always("Actor is " + targetActor.Actor);
                                    TacticalActor tacticalActor = targetActor.Actor as TacticalActor;

                                    if (!tacticalActor.HasStatus(umbraTargetStatusDef))
                                    {
                                        TFTVLogger.Always("The target is " + tacticalActor.name);
                                        tacticalActor.Status.ApplyStatus(umbraTargetStatusDef);

                                    }

                                }
                            }
                        }
                        else
                        {
                            abilityTarget.GetTargetActor().Status.ApplyStatus(umbraTargetStatusDef);

                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }





        [HarmonyPatch(typeof(ShootAbility), "OnPlayingActionEnd")]

        internal static class ShootAbility_PlayAction_AcheronTBTV_patch
        {

            public static void Postfix(ShootAbility __instance)
            {
                try
                {
                    AdditionalEffectShootAbilityDef cureSpray = DefCache.GetDef<AdditionalEffectShootAbilityDef>("Acheron_CureSpray_AbilityDef");

                    if (__instance.TacticalActor != null && __instance.TacticalActor.HasGameTag(acheronTag) && !__instance.AbilityDef.Equals(cureSpray))
                    {
                        TFTVLogger.Always("Acheron ShootAbility attack");

                        TacticalAbilityTarget abilityTarget = __instance.LastAbilityTarget;

                        if (__instance.LastAbilityTarget != null && __instance.LastAbilityTarget.Actor is TacticalActor)
                        {
                            //   TFTVLogger.Always("Actor is " + __instance.LastAbilityTarget.Actor);
                            TacticalActor tacticalActor = __instance.LastAbilityTarget.Actor as TacticalActor;

                            if (!tacticalActor.HasStatus(umbraTargetStatusDef))
                            {
                                TFTVLogger.Always("The target is " + tacticalActor.name);
                                tacticalActor.Status.ApplyStatus(umbraTargetStatusDef);

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

        [HarmonyPatch(typeof(ApplyDamageEffectAbility), "ApplyDamageEffectCrt")]
        internal static class ApplyDamageEffectAbility_ApplyDamageEffectCrt_AcheronTBTV_patch
        {
            public static void Postfix(PlayingAction action, ApplyDamageEffectAbility __instance)
            {
                try
                {
                    if (__instance.TacticalActor != null && __instance.TacticalActor.HasGameTag(acheronTag))
                    {
                        TFTVLogger.Always("Acheron ApplyDamageEffectAbility attack");

                        TacticalAbilityTarget abilityTarget = (TacticalAbilityTarget)action.Param;

                        foreach (TacticalAbilityTarget targetActor in __instance.GetTargetActors(__instance.OriginTargetData))
                        {
                            if (targetActor.Actor is TacticalActor)
                            {
                                // TFTVLogger.Always("Actor is " + targetActor.Actor);
                                TacticalActor tacticalActor = targetActor.Actor as TacticalActor;

                                if (!tacticalActor.HasStatus(umbraTargetStatusDef))
                                {
                                    TFTVLogger.Always("The target is " + tacticalActor.name);
                                    tacticalActor.Status.ApplyStatus(umbraTargetStatusDef);

                                }
                                ParticleEffectDef pepperCloudParticleEffect = DefCache.GetDef<ParticleEffectDef>("E_Mist10 [Acheron_SpawnPepperCloudParticle_EventDef]");
                                Effect pepperCloud = new Effect() { BaseDef = pepperCloudParticleEffect };
                                EffectTarget effectTarget = new EffectTarget { GameObject = tacticalActor.gameObject };
                                pepperCloud.Apply(effectTarget);
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

        [HarmonyPatch(typeof(ApplyStatusAbility), "ApplyStatusCrt")]
        internal static class ApplyStatusAbility_ApplyStatusCrt_AcheronTBTV_patch
        {

            public static void Postfix(PlayingAction action, ApplyStatusAbility __instance)
            {
                try
                {
                    if (__instance.TacticalActor != null && __instance.TacticalActor.HasGameTag(acheronTag))
                    {
                        TFTVLogger.Always("Acheron ApplyStatus attack");

                        TacticalAbilityTarget abilityTarget = (TacticalAbilityTarget)action.Param;

                        if (__instance.ApplyStatusAbilityDef.ApplyStatusToAllTargets)
                        {
                            foreach (TacticalAbilityTarget targetActor in __instance.GetTargetActors(__instance.OriginTargetData))
                            {
                                if (targetActor.Actor is TacticalActor)
                                {
                                    // TFTVLogger.Always("Actor is " + targetActor.Actor);
                                    TacticalActor tacticalActor = targetActor.Actor as TacticalActor;

                                    if (!tacticalActor.HasStatus(umbraTargetStatusDef))
                                    {
                                        TFTVLogger.Always("The target is " + tacticalActor.name);
                                        tacticalActor.Status.ApplyStatus(umbraTargetStatusDef);

                                    }

                                }
                            }
                        }
                        else
                        {
                            abilityTarget.GetTargetActor().Status.ApplyStatus(umbraTargetStatusDef);

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

    }

}

