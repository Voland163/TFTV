using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Linq;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class InfiltratorSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static ModLogger Logger = TFTVMain.Main.Logger;


        public static void ApplyChanges()
        {
            // Spider Drone Pack: 3 AP 3 WP, rest vanilla
            Change_DeployDronePack();

            // Sneak Attack: Direct fire and melee +60 damage while not spotted
            Change_SneakAttack();

            // Cautious: +10% stealth
            Change_Cautious();

            //Phantom Protocol: Gain +15% stealth and accuracy, provides immunity to Sentinels surveillance ability
            Create_BC2_PhantomProtocol();

            // Old Phantom Protocol BC1: like above, but active and without deny Sentinel detection
            // ### DEPRECTATED, KEPT FOR SAVE GAME COMPATIBILTY ###
            Create_PhantomProtocol_Old();
        }

        private static void Change_DeployDronePack()
        {
            float apCost = 0.75f;
            float wpCost = 3.0f;

            ShootAbilityDef DeployDronePack = Repo.GetAllDefs<ShootAbilityDef>().FirstOrDefault(s => s.name.Equals("DeployDronePack_ShootAbilityDef"));
            DeployDronePack.ActionPointCost = apCost;
            DeployDronePack.WillPointCost = wpCost;
        }

        private static void Change_SneakAttack()
        {
            float hiddenDamageMod = 1.5f;
            float locatedDamageMod = 1.25f;

            ApplyStatusAbilityDef SneakAttack = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(asa => asa.name.Equals("SneakAttack_AbilityDef"));
            FactionVisibilityConditionStatusDef factionVisibilityConditionStatus = (FactionVisibilityConditionStatusDef)SneakAttack.StatusDef;
            StanceStatusDef hiddenStateStatus = (StanceStatusDef)factionVisibilityConditionStatus.HiddenStateStatusDef;
            hiddenStateStatus.Visuals = Helper.CreateDefFromClone(
                SneakAttack.ViewElementDef,
                "8981e175-124a-48eb-8644-69f4ae77d454",
                $"E_HiddenStatusViewElement [{SneakAttack.name}]");
            StanceStatusDef locatedStateStatus = Helper.CreateDefFromClone(
                hiddenStateStatus,
                "8cc0375e-1f2d-4e4a-85df-d626dab2a92a",
                $"E_DetectedSneakAttackStatus [{SneakAttack.name}]");
            locatedStateStatus.Visuals = Helper.CreateDefFromClone(
                SneakAttack.ViewElementDef,
                "63353d9f-97c3-46fb-b40b-b9eb9d32f1ce",
                $"E_DetectedStatusViewElement [{SneakAttack.name}]");

            // Setting fields
            //SneakAttack.ViewElementDef.Description = new LocalizedTextBind($"Attacks while hidden deal +50% and while located +25% damage.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            SneakAttack.ViewElementDef.Description.LocalizationKey = "PR_BC_SNEAK_ATTACK_DESC";

            //hiddenStateStatus.Visuals.Description = new LocalizedTextBind($"HIDDEN: Deal +50% damage.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            hiddenStateStatus.Visuals.Description.LocalizationKey = "PR_BC_SA_HIDDEN_DESC";
            hiddenStateStatus.Visuals.Color = Color.green;
            hiddenStateStatus.StatModifications[0].Value = hiddenDamageMod;

            //locatedStateStatus.Visuals.Description = new LocalizedTextBind($"LOCATED: Deal +{(locatedDamageMod - 1) * 100}% damage.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            locatedStateStatus.Visuals.Description.LocalizationKey = "PR_BC_SA_LOCATED_DESC";
            locatedStateStatus.Visuals.Color = Color.yellow;
            locatedStateStatus.StatModifications[0].Value = locatedDamageMod;

            factionVisibilityConditionStatus.LocatedStateStatusDef = locatedStateStatus;
        }

        private static void Change_Cautious()
        {
            //float damageMod = 0.9f;
            float accuracyMod = 0.2f;
            float stealthMod = 0.1f;
            PassiveModifierAbilityDef cautious = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(asa => asa.name.Equals("Cautious_AbilityDef"));
            cautious.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                   TargetStat = StatModificationTarget.Accuracy,
                   Modification = StatModificationType.Add,
                   Value = accuracyMod
                },
                new ItemStatModification()
                {
                   TargetStat = StatModificationTarget.Stealth,
                   Modification = StatModificationType.Add,
                   Value = stealthMod
                }
            };
            cautious.ViewElementDef.Description.LocalizationKey = "PR_BC_CAUTIOUS_DESC"; // new LocalizedTextBind($"Gain {accuracyMod * 100}% accuracy and {stealthMod * 100}% stealth.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }

        public static PassiveModifierAbilityDef PhantomProtocolDef;
        private static void Create_BC2_PhantomProtocol()
        {
            float mod = 0.15f;
            string skillName = "BC2_PhantomProtocol_AbilityDef";

            PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("Cautious_AbilityDef"));
            PhantomProtocolDef = Helper.CreateDefFromClone(
                source,
                "B503271D-CB9F-41B2-82C4-60D4F73DA417",
                skillName);
            PhantomProtocolDef.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "6E2079D2-1FD2-4427-819D-A71A28E06422",
                skillName);
            PhantomProtocolDef.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "B7F6B435-D6C5-4219-A569-D103F49B4C26",
                skillName);

            // Set necessary fields
            PhantomProtocolDef.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = mod
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = mod
                }
            };

            // Change neccesary fields
            PhantomProtocolDef.CharacterProgressionData.RequiredSpeed = 0;
            PhantomProtocolDef.CharacterProgressionData.RequiredStrength = 0;
            PhantomProtocolDef.CharacterProgressionData.RequiredWill = 0;
            PhantomProtocolDef.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_PHANTOM_PROTOCOL";
            PhantomProtocolDef.ViewElementDef.Description.LocalizationKey = "PR_BC_PHANTOM_PROTOCOL_DESC";
            Sprite PP_Icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Phantom_Protocol_2-2.png");
            PhantomProtocolDef.ViewElementDef.LargeIcon = PP_Icon;
            PhantomProtocolDef.ViewElementDef.SmallIcon = PP_Icon;

            // Grant immunity to Sentinels surveillance ability
            GameTagDef surveillanceImmunity = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(gtd => gtd.name.Equals("InvisibleToSentinels_SkillTagDef"));
            PhantomProtocolDef.ActorTags = new GameTagDef[] { surveillanceImmunity };

            // Clear list of actors to rescue for rescue mission patch (see below)
            if (actorsToRescue != null && actorsToRescue.Count > 0)
            {
                actorsToRescue.Clear();
            }
        }
        // Patch to apply the SurveillanceImmunity_GameTagDef from BC_PhantomProtocol_AbilityDef.ActorTags to the actors game tags, vanilla does not do that
        [HarmonyPatch(typeof(PassiveModifierAbility), "AbilityAdded")]
        internal static class PassiveModifierAbility_AbilityAdded_Patch
        {
            public static void Postfix(PassiveModifierAbility __instance)
            {
                try
                {
                    if (__instance.PassiveModifierAbilityDef.name.Equals("BC2_PhantomProtocol_AbilityDef")
                        && !__instance.TacticalActorBase.HasGameTags(__instance.PassiveModifierAbilityDef.ActorTags))
                    {
                        __instance.TacticalActorBase.AddGameTags(new GameTagsList(__instance.PassiveModifierAbilityDef.ActorTags));
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }
        // Patch the evaluation of rescue mission.
        // The immunity to surveillance with Phantom Protocol caused the mission to fail in the moment an actor to rescue is activated by an Infiltrator with this perk.
        // List for actors to rescue, will be cleared when objective is achieved and on each level start (see above)
        private static readonly List<TacticalActor> actorsToRescue = new List<TacticalActor>();
        [HarmonyPatch(typeof(RescueSoldiersFactionObjective), "EvaluateObjective")]
        internal static class RescueSoldiersFactionObjective_EvaluateObjective_Patch
        {
            public static void Postfix(ref FactionObjectiveState __result, RescueSoldiersFactionObjective __instance)
            {
                try
                {
                    string LogPrefix = "RescueSoldiersFactionObjective_EvaluateObjective_Patch.Postfix(..):".ToUpper();
                    switch (__result)
                    {
                        case FactionObjectiveState.InProgress:
                            foreach (TacticalActor tacticalActor in __instance.Level.Map.GetActors<TacticalActor>(null))
                            {
                                if (tacticalActor.TacticalFaction.Faction.FactionDef == __instance.RescuedFaction)
                                {
                                    if (!(tacticalActor.IsDead || tacticalActor.Status.HasStatus<EvacuatedStatus>()))
                                    {
                                        if (!actorsToRescue.Contains(tacticalActor))
                                        {
                                            actorsToRescue.Add(tacticalActor);
                                            PRMLogger.Always($"{LogPrefix} {tacticalActor} added to rescue list, total actors in list: {actorsToRescue.Count}");
                                        }
                                    }
                                    else
                                    {
                                        if (actorsToRescue.Contains(tacticalActor))
                                        {
                                            actorsToRescue.Remove(tacticalActor);
                                            string reason = tacticalActor.IsDead ? "died" : "evaced";
                                            PRMLogger.Always($"{LogPrefix} {tacticalActor} removed from rescue list because he {reason}, total actors in list: {actorsToRescue.Count}");
                                        }
                                    }
                                }
                            }
                            break;
                        case FactionObjectiveState.Failed:
                            if (actorsToRescue.Count > 0)
                            {
                                string[] messages = new string[]
                                {
                                    $"{LogPrefix} Rescue soldiers objective is failed but rescue list still has {actorsToRescue.Count} actor(s) to rescue!",
                                    $"{LogPrefix} List of actor(s) to rescue: {actorsToRescue.Join()}",
                                    $"{LogPrefix} Set result back to 'InProgress' ..."
                                };
                                foreach (string message in messages)
                                {
                                    Logger.LogWarning(message);
                                    PRMLogger.Always(message);
                                }
                                __result = FactionObjectiveState.InProgress;
                            }
                            break;
                        case FactionObjectiveState.Achieved:
                            PRMLogger.Always($"{LogPrefix} Rescue soldiers objective achieved, current total actors in rescue list: {actorsToRescue.Count}, clearing list ...");
                            actorsToRescue.Clear();
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        // #############################################################################
        // OLD PHANTOM PROTOCOL, DEPRECATED AND UNUSED, KEPT FOR SAVE GAME COMPATIBILITY
        // #############################################################################
        private static void Create_PhantomProtocol_Old()
        {
            float mod = 0.25f;
            string skillName = "BC_PhantomProtocol_AbilityDef";
            ApplyStatusAbilityDef source = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(p => p.name.Equals("QuickAim_AbilityDef"));
            ApplyStatusAbilityDef phantomProtocol = Helper.CreateDefFromClone(
                source,
                "5f3e257c-aff7-4296-9992-f6728bfa8af8",
                skillName);
            phantomProtocol.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "08545868-2bed-47a4-8628-371bbce5f718",
                skillName);
            phantomProtocol.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "c312e7f4-3339-4ee8-9717-d1f9c8bd2b32",
                skillName);
            phantomProtocol.StatusDef = Helper.CreateDefFromClone(
                Repo.GetAllDefs<StanceStatusDef>().FirstOrDefault(sms => sms.name.Equals("E_VanishedStatus [Vanish_AbilityDef]")),
                "06ca77ea-223b-4ec0-a7e6-734e6b7fefe9",
                "E_AccAnd StealthMultiplier [BC_PhantomProtocol_AbilityDef]");

            phantomProtocol.ViewElementDef.DisplayName1 = new LocalizedTextBind("PHANTOM PROTOCOL", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            phantomProtocol.ViewElementDef.Description = new LocalizedTextBind("You gain +25% accuracy and stealth until next turn", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Phantom_Protocol_2-2.png");
            phantomProtocol.ViewElementDef.LargeIcon = icon;
            phantomProtocol.ViewElementDef.SmallIcon = icon;
            phantomProtocol.ActionPointCost = 0;
            phantomProtocol.WillPointCost = 3;

            StanceStatusDef ppModStatus = (StanceStatusDef)phantomProtocol.StatusDef;
            ppModStatus.Visuals = phantomProtocol.ViewElementDef;
            ppModStatus.EventOnApply = null;
            ppModStatus.EventOnUnapply = null;
            ppModStatus.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Accuracy,
                    Modification = StatModificationType.Add,
                    Value = mod
                },
                new ItemStatModification()
                {
                    TargetStat = StatModificationTarget.Stealth,
                    Modification = StatModificationType.Add,
                    Value = mod
                }
            };
            ppModStatus.EquipmentsStatModifications = new EquipmentItemTagStatModification[0];
            ppModStatus.StanceShader = null;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(phantomProtocol))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(phantomProtocol).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }
    }
}
