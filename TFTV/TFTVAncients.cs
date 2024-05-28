using Base.Core;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace TFTV
{
    internal class TFTVAncients
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        //private static readonly DefRepository Repo = TFTVMain.Repo;

        private static readonly DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");

        private static readonly WeaponDef RightDrill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
        private static readonly WeaponDef RightShield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
        private static readonly EquipmentDef LeftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
        private static readonly WeaponDef BeamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
        private static readonly EquipmentDef LeftCrystalShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_CrystalShield_EquipmentDef");

        private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
        private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

        private static readonly PassiveModifierAbilityDef ancientsPowerUpAbility = DefCache.GetDef<PassiveModifierAbilityDef>("AncientMaxPower_AbilityDef");
        private static readonly DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");
        private static readonly PassiveModifierAbilityDef SelfRepairAbility = DefCache.GetDef<PassiveModifierAbilityDef>("RoboticSelfRepair_AbilityDef");


        public static readonly string CyclopsBuiltVariable = "CyclopsBuiltVariable";
        //   public static bool LOTAReworkActive = false;
        public static bool AutomataResearched = false;

        //This is the number of previous encounters with Ancients. It is added to the Difficulty to determine the number of fully repaired MediumGuardians in battle
        private static int AncientsEncounterCounter = TFTVAncientsGeo.AncientsEncounterCounter;
        private static readonly AlertedStatusDef AlertedStatus = DefCache.GetDef<AlertedStatusDef>("Alerted_StatusDef");
        private static readonly DamageMultiplierStatusDef CyclopsDefenseStatus = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
        private static readonly StanceStatusDef AncientGuardianStealthStatus = DefCache.GetDef<StanceStatusDef>("AncientGuardianStealth_StatusDef");
        private static readonly DamageMultiplierStatusDef RoboticSelfRepairStatus = DefCache.GetDef<DamageMultiplierStatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
        // private static readonly GameTagDef SelfRepairTag = DefCache.GetDef<GameTagDef>("SelfRepair");
        // private static readonly GameTagDef MaxPowerTag = DefCache.GetDef<GameTagDef>("MaxPower");
        public static Dictionary<int, int> CyclopsMolecularDamageBuff = new Dictionary<int, int> { }; //turn number + 0 = none, 1 = mutation, 2 = bionic

        public static List<string> AlertedHoplites = new List<string>();

        //Method to check if Ancients (as a faction) are present in the mission
        public static bool CheckIfAncientsPresent(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")) || controller.TacMission.MissionData.MissionType.MaxPlayerUnits == 0)//MissionType.name.Contains("Attack_Alien_CustomMissionTypeDef"))
                {
                    TFTVLogger.Always("Ancients present");
                    return true;

                }
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }
        internal class CyclopsAbilities
        {
            public static void AddMindCrushEffectToCyclposScream(TacticalAbility ability, TacticalActor actor, object parameter)
            {
                try
                {
                    // TFTVLogger.Always($"aptouseperc is {DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef").APToUsePerc}");
                    if (ability.TacticalAbilityDef.name.Equals("CyclopsScream"))
                    {
                        SilencedStatusDef silencedStatusDef = DefCache.GetDef<SilencedStatusDef>("ActorSilenced_StatusDef");
                        DamageEffectDef mindCrushEffect = DefCache.GetDef<DamageEffectDef>("E_Effect [Cyclops_MindCrush]");

                        foreach (TacticalAbilityTarget target in ability.GetTargets())
                        {
                            if (target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor targetedTacticalActor)
                            {
                                targetedTacticalActor.ApplyDamage(new DamageResult
                                {
                                    ApplyStatuses = new List<StatusApplication>
                                { new StatusApplication
                                { StatusDef = silencedStatusDef, StatusSource = actor, StatusTarget = targetedTacticalActor } }
                                });
                                targetedTacticalActor.ApplyDamage(new DamageResult { ActorEffects = new List<EffectDef> { mindCrushEffect } });//, Source = __instance.Source });
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class CyclopsCrossBeamShooting
            {
                //Not used
                private static IEnumerator<NextUpdate> RaiseShield(TacticalActor shooterActor)//, Weapon weapon, TacticalAbilityTarget shootTarget)
                {

                    //  ShieldDeployedStatusDef shieldDeployed = DefCache.GetDef<ShieldDeployedStatusDef>("ShieldDeployed_StatusDef");
                    RetrieveShieldAbilityDef retrieveShieldAbilityDef = DefCache.GetDef<RetrieveShieldAbilityDef>("RetrieveShield_AbilityDef");
                    // Raise the shield

                    RetrieveShieldAbility retrieveShieldAbility = shooterActor.GetAbilityWithDef<RetrieveShieldAbility>(retrieveShieldAbilityDef);

                    TacticalAbilityTarget actorAsTargetForShieldRetrieval = new TacticalAbilityTarget
                    { GameObject = shooterActor.gameObject, PositionToApply = shooterActor.gameObject.transform.position };


                    // Wait for the shield animation to complete
                    yield return retrieveShieldAbility.ExecuteAndWait(actorAsTargetForShieldRetrieval);

                }

                internal static void ReDeployHopliteShield(TacticalAbility ability, TacticalActor tacticalActor, object parameter)
                {
                    try
                    {

                        // TFTVLogger.Always($"{tacticalActor.TacticalActorDef.name} with ability {ability.AbilityDef.name}");

                        if (tacticalActor.TacticalActorDef.name.Equals("HumanoidGuardian_ActorDef") && ability.AbilityDef.name.Equals("Guardian_Beam_ShootAbilityDef") && !tacticalActor.IsControlledByPlayer && tacticalActor.IsAlive)


                        {

                            TFTVLogger.Always($"{tacticalActor.name} should be redeploying shield");

                            DeployShieldAbilityDef deployShieldAbilityDef = DefCache.GetDef<DeployShieldAbilityDef>("DeployShield_Guardian_AbilityDef");

                            DeployShieldAbilityDef deployShieldAbilityDualDef = DefCache.GetDef<DeployShieldAbilityDef>("DeployShield_Guardian_Dual_AbilityDef");

                            DeployShieldAbility deployShieldAbility = null;

                            if (tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDef) != null)
                            {

                                deployShieldAbility = tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDef);


                            }
                            else if (tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDualDef) != null)
                            {

                                deployShieldAbility = tacticalActor.GetAbilityWithDef<DeployShieldAbility>(deployShieldAbilityDualDef);

                            }

                            if (deployShieldAbility != null)
                            {

                                TFTVLogger.Always($"{tacticalActor.name} found the ability to activate");
                                TacticalAbilityTarget targetOfTheAttack = parameter as TacticalAbilityTarget;

                                Vector3 directionShieldDeploy = tacticalActor.gameObject.transform.position + 2 * (targetOfTheAttack.ActorGridPosition - tacticalActor.gameObject.transform.position).normalized;
                                //  TFTVLogger.Always($"directShieldDeploy {directionShieldDeploy}, hoplite position {__instance.gameObject.transform.position} and target{targetOfTheAttack.ActorGridPosition}");
                                TacticalAbilityTarget tacticalAbilitytaret = new TacticalAbilityTarget
                                { GameObject = tacticalActor.gameObject, PositionToApply = directionShieldDeploy };

                                deployShieldAbility.Activate(tacticalAbilitytaret);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                [HarmonyPatch(typeof(MassShootTargetActorEffect), "OnApply")]

                public static class MassShootTargetActorEffect_OnApply_GuardiansCrossBeams_Patch
                {
                    public static bool Prefix(MassShootTargetActorEffect __instance, EffectTarget target)
                    {
                        try
                        {
                            MethodInfo tryGetShootTargetMethod = typeof(MassShootTargetActorEffect).GetMethod("TryGetShootTarget", BindingFlags.Instance | BindingFlags.NonPublic);

                            if (tryGetShootTargetMethod == null || target == null)
                            {
                                return false;
                            }

                            //  WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                            //   beamHead.APToUsePerc = 0;

                            TacticalAbilityTarget tacticalAbilityTarget = (TacticalAbilityTarget)tryGetShootTargetMethod.Invoke(__instance, new object[] { target });

                            if (tacticalAbilityTarget == null || tacticalAbilityTarget.Actor == null || tacticalAbilityTarget.Actor.IsDead)
                            {
                                return false;
                            }

                            TacticalActorBase sourceTacticalActorBase = TacUtil.GetSourceTacticalActorBase(__instance.Source);

                            if (sourceTacticalActorBase == null)
                            {
                                return false;

                            }

                            List<TacticalActor> list = sourceTacticalActorBase.TacticalFaction.TacticalActors.
                                Where((TacticalActor a) => a.TacticalActorBaseDef == __instance.MassShootTargetActorEffectDef.ShootersActorDef).
                                Where(ta => ta.IsAlive).
                                Where(ta => !ta.Status.HasStatus(AncientGuardianStealthStatus)).ToList();

                            TFTVLogger.Always($"Hoplites that can shoot in the cross-beam shooting {list.Count()}");

                            if (list.Count > 0)
                            {
                                using (new MultiForceTargetableLock(sourceTacticalActorBase.Map.GetActors<TacticalActor>()))
                                {
                                    foreach (TacticalActor hoplite in list)
                                    {
                                        ShieldDeployedStatusDef shieldDeployed = DefCache.GetDef<ShieldDeployedStatusDef>("ShieldDeployed_StatusDef");

                                        Weapon selectedWeapon = null;
                                        if (hoplite.Equipments != null)
                                        {
                                            foreach (Equipment equipment in hoplite.Equipments.Equipments)
                                            {
                                                if (equipment.TacticalItemDef.Equals(BeamHead) && equipment.IsUsable)
                                                {
                                                    selectedWeapon = equipment as Weapon;
                                                    TFTVLogger.Always($"{hoplite.name} has a beam weapon, check is null by any chance {selectedWeapon == null}");
                                                }
                                            }

                                            if (selectedWeapon != null)
                                            {
                                                TFTVLogger.Always($"{hoplite.name} can shoot");
                                                if (!hoplite.TacticalPerception.CheckFriendlyFire(selectedWeapon, hoplite.Pos, tacticalAbilityTarget, out TacticalActor hitFriend) && selectedWeapon.TryGetShootTarget(tacticalAbilityTarget) != null)
                                                {
                                                    TFTVLogger.Always($"{hoplite.name} won't hit a friendly");

                                                    if (hoplite.HasStatus(shieldDeployed))
                                                    {
                                                        TFTVLogger.Always($"{hoplite.name} has deployed shield");

                                                        //   Timing.Current.StartAndWaitFor(RaiseShield(hoplite));

                                                        hoplite.Equipments.SetSelectedEquipment(selectedWeapon);

                                                        //   TFTVLogger.Always($"selected weapon: {hoplite.Equipments.SelectedWeapon}");
                                                    }

                                                    MethodInfo faceAndShootAtTarget = typeof(MassShootTargetActorEffect).GetMethod("FaceAndShootAtTarget", BindingFlags.Instance | BindingFlags.NonPublic);

                                                    if (faceAndShootAtTarget != null)
                                                    {
                                                        Timing.Current.StartAndWaitFor((IEnumerator<NextUpdate>)faceAndShootAtTarget.Invoke(__instance, new object[] { hoplite, selectedWeapon, tacticalAbilityTarget }));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                TFTVLogger.Always($"{hoplite.name} can't shoot because selectedWeapon null? {selectedWeapon == null}");
                                            }
                                        }
                                    }
                                }
                            }


                            return false;

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }

                //  public static int HopliteBeamWeaponKludge = 0;

                public static Dictionary<TacticalActor, float> HopliteAPMassShoot = new Dictionary<TacticalActor, float>();

                [HarmonyPatch(typeof(MassShootTargetActorEffect), "FaceAndShootAtTarget")]

                public static class MassShootTargetActorEffect_FaceAndShootAtTarget_GuardiansCrossBeams_Patch
                {
                    public static void Postfix(TacticalActor shooterActor)
                    {
                        try
                        {
                            HopliteAPMassShoot.Add(shooterActor, shooterActor?.CharacterStats?.ActionPoints);


                            TFTVLogger.Always($"{shooterActor?.name} has {shooterActor?.CharacterStats?.ActionPoints} action points");

                            /*  WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                              beamHead.APToUsePerc = 0;
                              HopliteBeamWeaponKludge += 1;
                              TFTVLogger.Always($"MassShoot in effect, count {HopliteBeamWeaponKludge}");*/

                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }

                }

                public static void RedeployHopliteShieldsAfterMassShootAttackAndRestoreTheirAP(TacticalAbility ability, TacticalActor actor, object parameter)
                {
                    try
                    {

                        ReDeployHopliteShield(ability, actor, parameter);

                        if (HopliteAPMassShoot.Count > 0)
                        {
                            if (HopliteAPMassShoot.ContainsKey(actor))
                            {
                                TFTVLogger.Always($"{actor?.name} has {actor?.CharacterStats?.ActionPoints} ");
                                actor?.CharacterStats?.ActionPoints?.Set(HopliteAPMassShoot[actor]);
                                TFTVLogger.Always($"but now {actor?.name} has {actor?.CharacterStats?.ActionPoints} ");
                                HopliteAPMassShoot.Remove(actor);
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }



                }
            }

            internal class CyclopsResistance
            {
                public static void ResetCyclopsDefense()
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CheckIfAncientsPresent(controller))
                        {
                            float baseMultiplier = 0.5f;

                            /*  if (TFTVSpecialDifficulties.CheckTacticalSpecialDifficultySettings(controller) == 2)
                              {
                                  baseMultiplier = 0.25f; //adjusted on 22/12 from 0.0f
                              }*/

                            IEnumerable<TacticalActor> allHoplites = from x in controller.Map.GetActors<TacticalActor>()
                                                                     where x.HasGameTag(hopliteTag)
                                                                     where x.IsAlive
                                                                     select x;



                            int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                            float proportion = ((float)deadHoplites / (float)(allHoplites.Count()));

                            if (allHoplites.Count() == 0)
                            {
                                proportion = 1;
                            }

                            CyclopsDefenseStatus.Multiplier = baseMultiplier + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                            TFTVLogger.Always($"There are {allHoplites.Count()} hoplites in total, {deadHoplites} are dead. Proportion is {proportion} and base multiplier is {baseMultiplier}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void ReduceCyclopsResistance(TacticalFaction faction, TacticalLevelController controller, TacticalActor actor)
                {
                    try
                    {
                        if (CyclopsDefenseStatus.Multiplier <= 0.99f)
                        {
                            float baseMultiplier = 0.5f;

                            /*  if (TFTVSpecialDifficulties.CheckTacticalSpecialDifficultySettings(controller) == 2)
                              {
                                  baseMultiplier = 0.25f;
                              }*/

                            List<TacticalActor> allHoplites = actor.TacticalFaction.TacticalActors.Where(ta => ta.HasGameTag(hopliteTag)).ToList();
                            int deadHoplites = allHoplites.Where(h => h.IsDead).Count();
                            float proportion = ((float)deadHoplites / (float)(allHoplites.Count));
                            CyclopsDefenseStatus.Multiplier = baseMultiplier + proportion * 0.5f; //+ HoplitesKilled * 0.1f;
                            TFTVLogger.Always($"There are {allHoplites.Count} hoplites in total, {deadHoplites} are dead. Proportion is {proportion} and base multiplier is {baseMultiplier}. Cyclops Defense level is {CyclopsDefenseStatus.Multiplier}");


                            //  CyclopsDefenseStatus.Multiplier += 0.1f;
                            TFTVLogger.Always("Hoplite killed, decreasing Cyclops defense. Cyclops defense now " + CyclopsDefenseStatus.Multiplier);
                        }
                        else
                        {
                            CyclopsDefenseStatus.Multiplier = 1;
                            if (AutomataResearched)
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in faction.Actors)
                                {
                                    if (allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                    {
                                        TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                        if (actorAlly.HasStatus(CyclopsDefenseStatus))
                                        {
                                            Status status = actorAlly.Status.GetStatusByName(CyclopsDefenseStatus.EffectName);
                                            actorAlly.Status.Statuses.Remove(status);
                                            TFTVLogger.Always("Cyclops defense removed from " + actorAlly.name);

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

                public static void AncientKilled(TacticalLevelController controller, DeathReport deathReport)
                {
                    try
                    {
                        if (CheckIfAncientsPresent(controller))
                        {
                            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                            TacticalFaction faction = deathReport.Actor.TacticalFaction;

                            if (deathReport.Actor is TacticalActor)
                            {
                                TacticalActor actor = deathReport.Actor as TacticalActor;
                                if (actor.HasGameTag(hopliteTag))
                                {
                                    HoplitesAbilities.HoplitesAutoRepair.ApplyAutoRepairAbilityStatusOrHealNearbyHoplites(faction, actor);
                                    ReduceCyclopsResistance(faction, controller, actor);
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

        internal class HoplitesAbilities
        {
            public static void ApplyDamageResistanceToHopliteInHiding(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    if (data.Target.GetActor() != null && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(AncientGuardianStealthStatus))
                    {

                        float multiplier = 0.1f;
                        data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                        data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal class HoplitesMolecularTargeting
            {
                internal static void CyclopsMolecularTargeting(TacticalActor actor, IDamageDealer damageDealer)
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CyclopsMolecularDamageBuff.Count() == 0 || !CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                        {
                            ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");
                            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");

                            if (damageDealer != null && damageDealer.GetTacticalActorBase() != null && damageDealer.GetTacticalActorBase().GameTags.Contains(hopliteTag))
                            {
                                TacticalFaction tacticalFaction = damageDealer.GetTacticalActorBase().TacticalFaction;

                                bool cyclopsAlive = false;

                                foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                                {
                                    if (tacticalActor.IsAlive && tacticalActor.GameTags.Contains(cyclopsTag))
                                    {
                                        cyclopsAlive = true;

                                    }
                                }

                                if (cyclopsAlive)
                                {

                                    int bionics = 0;
                                    int mutations = 0;

                                    foreach (TacticalItem bodypart in actor.BodyState.GetArmourItems())
                                    {
                                        if (bodypart.GameTags.Contains(Shared.SharedGameTags.AnuMutationTag))
                                        {
                                            mutations += 1;
                                        }
                                        else if (bodypart.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                        {
                                            TFTVLogger.Always("bionics");

                                            bionics += 1;
                                        }
                                    }

                                    if (actor.TacticalActorDef.GameTags.Contains(Shared.SharedGameTags.VehicleTag))
                                    {
                                        bionics = 5;

                                    }


                                    if (bionics > mutations)
                                    {
                                        TFTVLogger.Always("more bionics");
                                        CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 2);
                                        BeamsVsCyborgs();

                                        TFTVLogger.Always($"{actor.DisplayName} is primarily bionic or a vehicle");

                                    }
                                    else if (bionics < mutations || actor.HasGameTag(Shared.SharedGameTags.AlienTag))
                                    {
                                        CyclopsMolecularDamageBuff.Add(controller.TurnNumber, 1);
                                        BeamsVsMutants();
                                        TFTVLogger.Always($"{actor.DisplayName} is primarily mutated or an Alien");
                                    }
                                    else
                                    {
                                        BeamOriginal();
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

                internal static void BeamsVsCyborgs()
                {
                    try
                    {
                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                        GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                        DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                        DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");

                        if (!originalBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsOBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (!cyclopsPBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Add(empDamage);
                        }
                        if (originalBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsOBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }
                        if (cyclopsPBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Remove(virophageDamage);
                        }

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }


                }

                internal static void BeamsVsMutants()
                {
                    try
                    {
                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                        GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                        DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                        DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");

                        if (!originalBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsOBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (!cyclopsPBeam.DamagePayload.DamageKeywords.Contains(virophageDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Add(virophageDamage);
                        }
                        if (originalBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            originalBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsLCBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsLCBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsOBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsOBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }
                        if (cyclopsPBeam.DamagePayload.DamageKeywords.Contains(empDamage))
                        {
                            cyclopsPBeam.DamagePayload.DamageKeywords.Remove(empDamage);
                        }



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static void BeamOriginal()
                {
                    try
                    {
                        //  TFTVLogger.Always($"Changing Ancient beam to original damage payload");

                        WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                        WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                        WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                        WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");


                        //   originalBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [HumanoidGuardian_Head_WeaponDef]");
                        originalBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                { Value = 70, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        cyclopsLCBeam.DamagePayload.DamageKeywords =
                           new List<DamageKeywordPair>()
                        { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                        };
                        //    cyclopsLCBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_LivingCrystal_WeaponDef]");
                        cyclopsOBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };
                        //   cyclopsOBeam.ViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [MediumGuardian_Head_Orichalcum_WeaponDef]");
                        cyclopsPBeam.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                                { new DamageKeywordPair
                                    { Value = 120, DamageKeywordDef = Shared.SharedDamageKeywords.DamageKeyword }
                                };


                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


            }

            internal class HoplitesAutoRepair
            {
                internal static void ApplyAutoRepairAbilityStatusOrHealNearbyHoplites(TacticalFaction faction, TacticalActor actor)
                {
                    try
                    {
                        foreach (TacticalActor actorAlly in faction.TacticalActors)
                        {
                            if (actorAlly != actor && (actorAlly.HasGameTag(hopliteTag) || actorAlly.HasGameTag(cyclopsTag)))
                            {
                                // TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                float magnitude = 7;

                                if ((actorAlly.Pos - actor.Pos).magnitude <= magnitude)
                                {
                                    TFTVLogger.Always("Actor in range and will be receiving power from dead friendly");
                                    actorAlly.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                    if ((CheckGuardianBodyParts(actorAlly)[0] == null
                                    || CheckGuardianBodyParts(actorAlly)[1] == null
                                    || CheckGuardianBodyParts(actorAlly)[2] == null))
                                    {
                                        TFTVLogger.Always("Actor in range and missing bodyparts, getting spare parts");
                                        if (!actorAlly.HasStatus(AddAutoRepairStatusAbility) && !actorAlly.HasGameTag(cyclopsTag))
                                        {
                                            actorAlly.Status.ApplyStatus(AddAutoRepairStatusAbility);
                                            TFTVLogger.Always("AutoRepairStatus added to " + actorAlly.name);


                                            TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                            tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);
                                        }
                                    }
                                    else
                                    {
                                        if (actorAlly.GetHealth() < actorAlly.TotalMaxHealth)
                                        {
                                            if (actorAlly.GetHealth() + 50 >= actorAlly.TotalMaxHealth)
                                            {
                                                actorAlly.Health.Set(actorAlly.TotalMaxHealth);
                                            }
                                            else
                                            {
                                                actorAlly.Health.Set(actorAlly.GetHealth() + 50);
                                            }

                                        }
                                        TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                        tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, actorAlly, actorAlly);
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

                public static TacticalItem[] CheckGuardianBodyParts(TacticalActor actor)
                {
                    try
                    {
                        TacticalItem[] equipment = new TacticalItem[3];

                        foreach (Equipment item in actor.Equipments.Equipments)
                        {
                            if (item.TacticalItemDef.Equals(BeamHead))
                            {
                                equipment[0] = item;
                            }
                            else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                            {
                                equipment[1] = item;

                            }
                            else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                            {
                                equipment[2] = item;
                            }
                        }
                        return equipment;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        return new TacticalItem[3];
                    }
                }

            }

        }

        internal class StuckHopliteVanillaFix
        {
            public static void CheckHopliteKillList(TacticalFaction tacticalFaction)
            {
                try
                {

                    if (!tacticalFaction.TacticalFactionDef.ShortNames.Contains("anc")) 
                    {
                        return;
                    }

                    TacticalLevelController controller = tacticalFaction.TacticalLevel;

                    if (controller.Map.GetActors<TacticalActor>().Any(ta => ta.HasGameTag(cyclopsTag) && ta.IsAlive))
                    {
                        return;

                    }

                    List<TacticalActor> aliveHoplites = new List<TacticalActor>(controller.Map.GetActors<TacticalActor>().Where(
                                                                ta => ta.HasGameTag(hopliteTag) && ta.IsAlive).ToList());
                    
                    if (aliveHoplites.Count() > 3)
                    {
                        return;
                    }

                    TFTVLogger.Always($"Cyclops is dead and no more than 3 hoplites alive. Destroying them.");

                    foreach (TacticalActor tacticalActor in aliveHoplites)
                    {
                        tacticalActor.ApplyDamage(new DamageResult { HealthDamage = 500, Source = tacticalActor });
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

        }

        internal class AncientsNewTurn
        {
            public static void AncientsNewTurnCheck(TacticalFaction tacticalFaction)
            {

                try
                {
                    if (!tacticalFaction.TacticalLevel.IsLoadingSavedGame && 
                        tacticalFaction.TacticalLevel.TacMission.MissionData.MissionType.MissionTags.Any(t=>t.name.Contains("MissionTypeAncientSite")))
                    {
                        TFTVLogger.Always($"starting turn {tacticalFaction.TurnNumber} for faction {tacticalFaction.Faction.FactionDef.name} in an Ancient Site map");
                        CheckRoboticSelfRepairStatus(tacticalFaction);
                        ApplyRoboticSelfHealingStatus(tacticalFaction);
                        StuckHopliteVanillaFix.CheckHopliteKillList(tacticalFaction);

                        if (tacticalFaction.TurnNumber > 0)
                        {
                            CheckForAutoRepairAbility(tacticalFaction);
                            AdjustAutomataStats(tacticalFaction);
                        }
                    }

                    AdjustHopliteAndCyclopsBeam();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void CheckRoboticSelfRepairStatus(TacticalFaction tacticalFaction)
            {
                try
                {
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors)
                    {
                        if (tacticalActor.HasStatus(RoboticSelfRepairStatus))
                        {
                            List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                            foreach (ItemSlot bodyPart in bodyPartAspects)
                            {
                                TFTVLogger.Always($"{tacticalActor.name} has disabled {bodyPart.DisplayName}. Adding {bodyPart.GetHealth().Max / 2} health ");
                                bodyPart.GetHealth().Add(bodyPart.GetHealth().Max / 2);
                                tacticalActor.CharacterStats.WillPoints.Subtract(5);
                            }

                            Status status = tacticalActor.Status.GetStatusByName(RoboticSelfRepairStatus.EffectName);

                            tacticalActor.Status.Statuses.Remove(status);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void ApplyRoboticSelfHealingStatus(TacticalFaction tacticalFaction)
            {
                try
                {
                    foreach (TacticalActor tacticalActor in tacticalFaction.TacticalActors.Where(ta => ta.GetAbilityWithDef<PassiveModifierAbility>(SelfRepairAbility) != null && !ta.IsDead))
                    {
                        List<ItemSlot> bodyPartAspects = tacticalActor.BodyState.GetHealthSlots().Where(hs => !hs.Enabled).ToList();

                        TFTVLogger.Always($"{tacticalActor.name} has {SelfRepairAbility.name} and {bodyPartAspects.Count} disabled body parts. Applying Robotic Self Repair");

                        if (bodyPartAspects.Count > 0)
                        {
                            tacticalActor.Status.ApplyStatus(RoboticSelfRepairStatus);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void CheckForAutoRepairAbility(TacticalFaction faction)
            {
                try
                {
                    foreach (TacticalActor actor in faction.TacticalActors)
                    {
                        if (actor.HasStatus(AddAutoRepairStatusAbility))
                        {
                            TacticalItem[] Bodyparts = HoplitesAbilities.HoplitesAutoRepair.CheckGuardianBodyParts(actor);

                            TFTVLogger.Always($"{actor.name} has spare parts, making repairs");

                            actor.Status.Statuses.Remove(actor.Status.GetStatusByName(AddAutoRepairStatusAbility.EffectName));

                            if (Bodyparts[0] == null)
                            {
                                actor.Equipments.AddItem(BeamHead);
                                TFTVLogger.Always($"adding head to {actor.name}");
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftCrystalShield)
                            {
                                actor.Equipments.AddItem(RightDrill);
                                TFTVLogger.Always($"adding drill to {actor.name}");
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] != null && Bodyparts[2].TacticalItemDef == LeftShield)
                            {
                                actor.Equipments.AddItem(RightShield);
                                TFTVLogger.Always($"adding right shield to {actor.name}");
                            }
                            else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightDrill)
                            {
                                actor.Equipments.AddItem(LeftCrystalShield);
                                TFTVLogger.Always($"adding crystal shield to {actor.name}");
                            }
                            else if (Bodyparts[2] == null && Bodyparts[1] != null && Bodyparts[1].TacticalItemDef == RightShield)
                            {
                                TFTVLogger.Always($"adding left shield to {actor.name}");
                                actor.Equipments.AddItem(LeftShield);
                            }
                            else if (Bodyparts[1] == null && Bodyparts[2] == null)
                            {
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                int num = UnityEngine.Random.Range(0, 2);

                                if (num == 0)
                                {
                                    actor.Equipments.AddItem(LeftCrystalShield);
                                    TFTVLogger.Always($"adding left crystal shield to {actor.name}");
                                }
                                else
                                {
                                    actor.Equipments.AddItem(LeftShield);
                                    TFTVLogger.Always($"adding left shield to {actor.name}");
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

            private static int DetermineHopliteSpeed(TacticalActor tacticalActor, int currentWP)
            {
                try 
                {
                    int divisor = 1;

                    if(tacticalActor.BodyState.GetSlot("RightLeg") != null && tacticalActor.BodyState.GetSlot("RightLeg").GetHealth() <1) 
                    {
                        divisor++;
                    
                    }
                    if (tacticalActor.BodyState.GetSlot("LeftLeg") != null && tacticalActor.BodyState.GetSlot("LeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }

                    return currentWP / divisor;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }

            private static int DetermineCyclopsSpeed(TacticalActor tacticalActor, int currentWP)
            {
                try
                {
                    int divisor = 1;

                    if (tacticalActor.BodyState.GetSlot("FrontRightLeg") != null && tacticalActor.BodyState.GetSlot("FrontRightLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("FrontLeftLeg") != null && tacticalActor.BodyState.GetSlot("FrontLeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("RearRightLeg") != null && tacticalActor.BodyState.GetSlot("RearRightLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }
                    if (tacticalActor.BodyState.GetSlot("RearLeftLeg") != null && tacticalActor.BodyState.GetSlot("RearLeftLeg").GetHealth() < 1)
                    {
                        divisor++;
                    }

                    return currentWP / divisor;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            public static void AdjustAutomataStats(TacticalFaction faction)
            {
                try
                {

                    foreach (TacticalActor tacticalActor in faction.TacticalActors)
                    {
                        if (tacticalActor is TacticalActor guardian && tacticalActor.HasGameTag(hopliteTag) && !guardian.Status.HasStatus(AncientGuardianStealthStatus))
                        {
                            if (guardian.CharacterStats.WillPoints < 30)
                            {
                                if (guardian.CharacterStats.WillPoints > 25)
                                {
                                    guardian.CharacterStats.WillPoints.Set(30);
                                }
                                else
                                {
                                    guardian.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                }
                            }

                            if (guardian.CharacterStats.WillPoints >= 30)
                            {
                                if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                                {
                                    guardian.AddAbility(ancientsPowerUpAbility, guardian);
                                    guardian.Status.ApplyStatus(ancientsPowerUpStatus);

                                    TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, guardian, guardian);
                                }
                            }
                            else
                            {
                                if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                                {
                                    guardian.RemoveAbility(ancientsPowerUpAbility);
                                    guardian.Status.Statuses.Remove(guardian.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));

                                }

                            }

                            int hopliteSpeed = DetermineHopliteSpeed(tacticalActor, guardian.CharacterStats.WillPoints.IntValue);

                            guardian.CharacterStats.Speed.SetMax(hopliteSpeed);
                            guardian.CharacterStats.Speed.Set(hopliteSpeed);
                        }
                        else if (tacticalActor is TacticalActor cyclops && tacticalActor.HasGameTag(cyclopsTag))
                        {
                            if (cyclops.HasStatus(AlertedStatus) || cyclops.IsControlledByPlayer)
                            {
                                if (cyclops.CharacterStats.WillPoints < 40)
                                {
                                    if (cyclops.CharacterStats.WillPoints > 35)
                                    {
                                        cyclops.CharacterStats.WillPoints.Set(40);
                                    }
                                    else
                                    {
                                        cyclops.CharacterStats.WillPoints.AddRestrictedToMax(5);

                                    }
                                }
                            }

                            if (cyclops.CharacterStats.WillPoints >= 40)
                            {
                                if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                                {
                                    cyclops.AddAbility(ancientsPowerUpAbility, cyclops);
                                    cyclops.Status.ApplyStatus(ancientsPowerUpStatus);

                                    TacContextHelpManager tacContextHelpManager = (TacContextHelpManager)UnityEngine.Object.FindObjectOfType(typeof(TacContextHelpManager));
                                    tacContextHelpManager.EventTypeTriggered(HintTrigger.ActorSeen, cyclops, cyclops);
                                }
                            }
                            else
                            {
                                if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                                {
                                    cyclops.RemoveAbility(ancientsPowerUpAbility);
                                    cyclops.Status.Statuses.Remove(cyclops.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));

                                }
                            }

                            int cyclopsSpeed = DetermineCyclopsSpeed(cyclops, cyclops.CharacterStats.WillPoints.IntValue);

                            cyclops.CharacterStats.Speed.SetMax(cyclopsSpeed);
                            cyclops.CharacterStats.Speed.Set(cyclopsSpeed);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }

            }

            public static void AdjustHopliteAndCyclopsBeam()
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    // TFTVLogger.Always($"AdjustingHopliteAndCyclopsBeams.CyclopsMolecularDamageBuff count {CyclopsMolecularDamageBuff.Count()}. Turn number is {controller.TurnNumber} ");

                    WeaponDef originalBeam = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");

                    GameTagDamageKeywordDataDef virophageDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                    GameTagDamageKeywordDataDef empDamageKeyword = DefCache.GetDef<GameTagDamageKeywordDataDef>("EMP_DamageKeywordDataDef");

                    DamageKeywordPair virophageDamage = new DamageKeywordPair { Value = 60, DamageKeywordDef = virophageDamageKeyword };
                    DamageKeywordPair empDamage = new DamageKeywordPair { Value = 40, DamageKeywordDef = empDamageKeyword };

                    WeaponDef cyclopsLCBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_LivingCrystal_WeaponDef");
                    WeaponDef cyclopsOBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_Orichalcum_WeaponDef");
                    WeaponDef cyclopsPBeam = DefCache.GetDef<WeaponDef>("MediumGuardian_Head_ProteanMutane_WeaponDef");


                    //  WeaponDef cyclopsBeamVsMutants = DefCache.GetDef<WeaponDef>("CyclopsVSMutantsBeam");
                    //   WeaponDef cyclopsBeamVsCyborgs = DefCache.GetDef<WeaponDef>("CyclopsVSCyborgs");

                    if (CyclopsMolecularDamageBuff.Count() > 0)
                    {

                        if (CyclopsMolecularDamageBuff.ContainsKey(controller.TurnNumber))
                        {
                            WeaponDef beamVsMutants = DefCache.GetDef<WeaponDef>("HopliteVSMutantsBeam");
                            WeaponDef beamVsCyborgs = DefCache.GetDef<WeaponDef>("HopliteVSCyborgs");

                            if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 1)
                            {

                                HoplitesAbilities.HoplitesMolecularTargeting.BeamsVsMutants();

                                TFTVLogger.Always($"{originalBeam.name} is switching to vs mutants and aliens");
                            }
                            else if (CyclopsMolecularDamageBuff[controller.TurnNumber] == 2)
                            {
                                HoplitesAbilities.HoplitesMolecularTargeting.BeamsVsCyborgs();


                                TFTVLogger.Always($"{originalBeam.name} is switching to vs cyborgs and vehicles");
                            }
                        }
                        else
                        {
                            HoplitesAbilities.HoplitesMolecularTargeting.BeamOriginal();
                            TFTVLogger.Always($"{originalBeam.name} is switching to neutral damage payload");
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }

        internal class AncientDeployment
        {
            //Adjusts deployment of Ancient Automata
            public static void AdjustAncientsOnDeployment(TacticalLevelController controller)
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                try
                {

                    TacticalFaction faction = new TacticalFaction();
                    int countUndamagedGuardians = 0;

                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                    {
                        faction = controller.GetFactionByCommandName("anc");
                        countUndamagedGuardians = AncientsEncounterCounter + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);
                    }
                    else
                    {
                        faction = controller.GetFactionByCommandName("px");
                        countUndamagedGuardians = 8 - TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);
                    }

                    CyclopsAbilities.CyclopsResistance.ResetCyclopsDefense();
                    //CyclopsDefenseStatus.Multiplier = 0.5f;

                    List<TacticalActor> damagedGuardians = new List<TacticalActor>();

                    TFTVLogger.Always($"AdjustAncientsOnDeployment, undamaged hoplites count is {countUndamagedGuardians}");

                    foreach (TacticalActor tacticalActor in faction.TacticalActors)
                    {
                        // TFTVLogger.Always("Found tacticalactorbase");
                        if (tacticalActor.HasGameTag(hopliteTag))
                        {
                            //   TFTVLogger.Always("Found hoplite");
                            TacticalActor guardian = tacticalActor;
                            if (damagedGuardians.Count() + countUndamagedGuardians < faction.TacticalActors.Count() - 1)
                            {
                                TFTVLogger.Always($"damagedGuardians.Count() + countUndamagedGuardians {damagedGuardians.Count() + countUndamagedGuardians}, faction.TacticalActors.Count(){faction.TacticalActors.Count()}");
                                damagedGuardians.Add(guardian);
                            }
                            guardian.CharacterStats.WillPoints.Set(guardian.CharacterStats.WillPoints.IntMax / 3);
                            guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                            guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);

                        }
                        else if (tacticalActor.HasGameTag(cyclopsTag))
                        {
                            //  TFTVLogger.Always("Found cyclops");
                            TacticalActor cyclops = tacticalActor;
                            cyclops.Status.ApplyStatus(CyclopsDefenseStatus);
                            cyclops.CharacterStats.WillPoints.Set(cyclops.CharacterStats.WillPoints.IntMax / 4);
                            cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                            cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                        }
                    }

                    foreach (TacticalActor tacticalActor in damagedGuardians)
                    {
                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(1, 101);
                        // TFTVLogger.Always("The roll is " + roll);


                        foreach (Equipment item in tacticalActor.Equipments.Equipments)
                        {
                            if (item.TacticalItemDef.Equals(BeamHead))
                            {
                                if (roll > 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(RightShield) || item.TacticalItemDef.Equals(RightDrill))
                            {
                                if (roll <= 45)
                                {
                                    item.DestroyAll();
                                }
                            }
                            else if (item.TacticalItemDef.Equals(LeftShield) || item.TacticalItemDef.Equals(LeftCrystalShield))
                            {
                                if (roll + 10 * countUndamagedGuardians >= 65)
                                {
                                    item.DestroyAll();
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
}




