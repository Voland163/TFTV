using Base;
using Base.Cameras.ExecutionNodes;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Input;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Cameras.Filters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV;
using UnityEngine;
using UnityEngine.UI;

namespace PRMBetterClasses.SkillModifications
{
    internal class AssaultSkills
    {
        // Get config, definition repository and shared data
        //private static readonly Settings Config = BetterClassesMain.Config;
        //private static readonly DefRepository Repo = BetterClassesMain.Repo;
        //private static readonly SharedData Shared = BetterClassesMain.Shared;

        //private static readonly bool doNotLocalize = BetterClassesMain.doNotLocalize;

        public static void ApplyChanges()
        {
            DefCache DefCache = TFTVMain.Main.DefCache;
        
            // Quick Aim: Adding accuracy modification
            Create_BC_QuickAim(DefCache);

            // Kill'n'Run: Recive one free Dash move when killing an enemy, once per turn
            Create_KillAndRun(DefCache);

            // Onslaught (DeterminedAdvance_AbilityDef): Receiver can get only 1 onslaught per turn.
            Change_Onslaught(DefCache);

            // Rapid Clearance: Until end of turn, after killing an enemy next attack cost -2AP
            Change_RapidClearance(DefCache);

            // AIMED BURST: 2 bursts with increased accuracy for 3 AP and 4 WP, AR only
            Create_AimedBurst(DefCache);
        }

        private static void Create_BC_QuickAim(DefCache defCache)
        {
            string skillName = "BC_QuickAim_AbilityDef";
            float qaAccMultiplier = 0.7f;
            int qaUsesPerTurn = 2;
            DefRepository Repo = TFTVMain.Repo;

            //LocalizedTextBind qaDescription = new LocalizedTextBind(
            //    $"The Action Point cost of the next shot with a proficient weapon is reduced by 1 with {(qaAccMultiplier * 100) - 100}% accuracy. Limited to {qaUsesPerTurn} uses per turn.",
            //    TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            ApplyStatusAbilityDef source = defCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef");
            ApplyStatusAbilityDef quickAim = Helper.CreateDefFromClone(
                source,
                "a92d0cab-60a8-4a42-aeed-b5415906b39d",
                skillName);
            quickAim.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "00c31653-0935-43ed-9dc7-683ab2012e8c",
                skillName);
            quickAim.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "8cdddcfd-a93c-4efa-a5a5-5dc099cb7ea5",
                skillName);
            quickAim.StatusDef = Helper.CreateDefFromClone(
                source.StatusDef,
                "c60511db-2785-4932-8654-086adc8e9e1b",
                skillName);
            StatMultiplierStatusDef qaAccMod = Helper.CreateDefFromClone(
                defCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef"),
                "4a6f7cc4-1bd6-45a5-b572-053963966b07",
                $"E AccuracyMultiplier [{skillName}]");

            quickAim.ViewElementDef.Description.LocalizationKey = "PR_BC_QUICK_AIM_DESC"; // qaDescription;
            quickAim.UsesPerTurn = qaUsesPerTurn;
            quickAim.DisablingStatuses = new StatusDef[]
            { 
                quickAim.StatusDef,
                defCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef").StatusDef
            };
            qaAccMod.EffectName = "";
            qaAccMod.ShowNotification = false;
            qaAccMod.VisibleOnHealthbar = 0;
            qaAccMod.VisibleOnStatusScreen = 0;
            qaAccMod.Visuals = null;
            qaAccMod.StatsMultipliers[0].StatName = "Accuracy";
            qaAccMod.StatsMultipliers[0].Multiplier = qaAccMultiplier;
            //(quickAim.StatusDef as AddAttackBoostStatusDef).DurationTurns = -1; // same as Master Marksman, not sure if neccessary, it worked this way ;-)
            (quickAim.StatusDef as AddAttackBoostStatusDef).Visuals = quickAim.ViewElementDef;
            TacStatusDef[] qaAddStatusesToApply = (quickAim.StatusDef as AddAttackBoostStatusDef).AdditionalStatusesToApply.Append(qaAccMod).ToArray();
            (quickAim.StatusDef as AddAttackBoostStatusDef).AdditionalStatusesToApply = qaAddStatusesToApply;

            foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            {
                if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(source) && !animActionDef.AbilityDefs.Contains(quickAim))
                {
                    animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(quickAim).ToArray();
                    PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
                    foreach (AbilityDef ad in animActionDef.AbilityDefs)
                    {
                        PRMLogger.Debug("  " + ad.name);
                    }
                    PRMLogger.Debug("----------------------------------------------------", false);
                }
            }
        }

        public static void Create_KillAndRun(DefCache defCache)
        {
            string skillName = "KillAndRun_AbilityDef";
            DefRepository Repo = TFTVMain.Repo;

            // Source to clone from for main ability: Inspire
            ApplyStatusAbilityDef inspireAbility = defCache.GetDef<ApplyStatusAbilityDef>("Inspire_AbilityDef");

            // Create Neccessary RuntimeDefs
            ApplyStatusAbilityDef killAndRunAbility = Helper.CreateDefFromClone(
                inspireAbility,
                "3e0e991e-e0bf-4630-b2ca-110e68790fb7",
                skillName);
            AbilityCharacterProgressionDef progression = Helper.CreateDefFromClone(
                inspireAbility.CharacterProgressionData,
                "e3f25d2a-7668-4223-bb82-73a3f2f926aa",
                skillName);
            TacticalAbilityViewElementDef viewElement = Helper.CreateDefFromClone(
                inspireAbility.ViewElementDef,
                "8a740c8d-43b6-4ef1-9b93-b2c329566f27",
                skillName);
            OnActorDeathEffectStatusDef onActorDeathEffectStatus = Helper.CreateDefFromClone(
                inspireAbility.StatusDef as OnActorDeathEffectStatusDef,
                "7cfcb266-6730-4642-88d5-8a212104b9cc",
                "E_KillListenerStatus [" + skillName + "]");
            RepositionAbilityDef dashAbility = Helper.CreateDefFromClone( // Create an own Dash ability from standard Dash
                defCache.GetDef<RepositionAbilityDef>("Dash_AbilityDef"),
                "de8cd8a9-f2eb-4b8a-a408-a2a1913930c4",
                "KillAndRun_Dash_AbilityDef");
            TacticalTargetingDataDef dashTargetingData = Helper.CreateDefFromClone( // ... and clone its targeting data
                defCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Dash_AbilityDef]"),
                "18e86a2b-6031-4c84-a2a0-cb6ad2423b56",
                "KillAndRun_Dash_AbilityDef");
            StatusRemoverEffectDef statusRemoverEffect = Helper.CreateDefFromClone( // Borrow effect from Manual Control
                defCache.GetDef<StatusRemoverEffectDef>("E_RemoveStandBy [ManualControlStatus]"),
                "77b65001-7b75-4fbc-a89e-cf3e3e8ca69f",
                "E_StatusRemoverEffect [" + skillName + "]");
            AddAbilityStatusDef addAbiltyStatus = Helper.CreateDefFromClone( // Borrow status from Deplay Beacon (final mission)
                defCache.GetDef<AddAbilityStatusDef>("E_AddAbilityStatus [DeployBeacon_StatusDef]"),
                "ac18e0d8-530d-4077-b372-71c9f82e2b88",
                skillName);
            MultiStatusDef multiStatus = Helper.CreateDefFromClone( // Borrow multi status from Rapid Clearance
                defCache.GetDef<MultiStatusDef>("E_MultiStatus [RapidClearance_AbilityDef]"),
                "be7115e5-ce6b-47da-bead-311f3978f242",
                skillName);
            FirstMatchExecutionDef cameraAbility = Helper.CreateDefFromClone(
                defCache.GetDef<FirstMatchExecutionDef>("E_DashCameraAbility [NoDieCamerasTacticalCameraDirectorDef]"),
                "75d8137e-06f7-4840-8156-23366c4daea7",
                "E_KnR_Dash_CameraAbility [NoDieCamerasTacticalCameraDirectorDef]");
            cameraAbility.FilterDef = Helper.CreateDefFromClone(
                defCache.GetDef<TacCameraAbilityFilterDef>("E_DashAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]"),
                "bf422b08-5b84-4b6a-a0cd-74ce1bfbc2fc",
                "E_KnR_Dash_CameraAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]");
            (cameraAbility.FilterDef as TacCameraAbilityFilterDef).TacticalAbilityDef = dashAbility;

            // Add new KnR Dash ability to animation action handler for dash (same animation)
            foreach (TacActorSimpleAbilityAnimActionDef def in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(b => b.name.Contains("Dash")))
            {
                if (!def.AbilityDefs.Contains(dashAbility))
                {
                    def.AbilityDefs = def.AbilityDefs.Append(dashAbility).ToArray();
                }
            }

            // Set fields
            killAndRunAbility.CharacterProgressionData = progression;
            killAndRunAbility.ViewElementDef = viewElement;
            killAndRunAbility.SkillTags = new SkillTagDef[0];
            killAndRunAbility.StatusDef = multiStatus;
            killAndRunAbility.StatusApplicationTrigger = StatusApplicationTrigger.StartTurn;

            bool doNotLocalize =  TFTVMain.Main.Settings.DoNotLocalizeChangedTexts;
            viewElement.DisplayName1.LocalizationKey = "PR_BC_KILL_N_RUN"; // new LocalizedTextBind("KILL'N'RUN", doNotLocalize);
            viewElement.Description.LocalizationKey = "PR_BC_KILL_N_RUN_DESC"; // new LocalizedTextBind("Once per turn, take a free move after killing an enemy.", doNotLocalize);
            Sprite knR_IconSprite = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_KillNRun.png");
            viewElement.LargeIcon = knR_IconSprite;
            viewElement.SmallIcon = knR_IconSprite;
            viewElement.ShowInStatusScreen = true;
            viewElement.HideFromPassives = true;

            dashAbility.TargetingDataDef = dashTargetingData;
            dashAbility.TargetingDataDef.Origin.Range = 7.0f;

            dashAbility.ViewElementDef = Helper.CreateDefFromClone(
                inspireAbility.ViewElementDef,
                "1ab98dd0-cb7c-4285-9aaf-b1770b5ebcb8",
                "KillAndRun_Dash_AbilityDef");
            dashAbility.ViewElementDef.DisplayName1 = viewElement.DisplayName1;
            dashAbility.ViewElementDef.Description = viewElement.Description;
            dashAbility.ViewElementDef.LargeIcon = knR_IconSprite;
            dashAbility.ViewElementDef.SmallIcon = knR_IconSprite;
            dashAbility.ViewElementDef.ShowInStatusScreen = false;
            dashAbility.ViewElementDef.HideFromPassives = true;
            dashAbility.ViewElementDef.ShouldFlash = true;

            dashAbility.SuppressAutoStandBy = true;
            dashAbility.DisablingStatuses = new StatusDef[] { onActorDeathEffectStatus };
            dashAbility.UsesPerTurn = 1;
            dashAbility.ActionPointCost = 0.0f;
            dashAbility.WillPointCost = 0.0f;
            dashAbility.SamePositionIsValidTarget = true;
            dashAbility.AmountOfMovementToUseAsRange = -1.0f;
            BC_TacticalAbility_get_ShouldDisplay_Patch.KnR_Dash_AbilityDef = dashAbility;

            multiStatus.Statuses = new StatusDef[] { onActorDeathEffectStatus, addAbiltyStatus };

            onActorDeathEffectStatus.EffectName = "KnR_KillTriggerListener";
            onActorDeathEffectStatus.Visuals = viewElement;
            onActorDeathEffectStatus.VisibleOnPassiveBar = true;
            onActorDeathEffectStatus.DurationTurns = 0;
            onActorDeathEffectStatus.EffectDef = statusRemoverEffect;

            statusRemoverEffect.StatusToRemove = "KnR_KillTriggerListener";

            addAbiltyStatus.DurationTurns = 0;
            addAbiltyStatus.SingleInstance = true;
            addAbiltyStatus.AbilityDef = dashAbility;
        }

        // Harmony Patch to display the KnR Dash ability after kill has been achieved
        [HarmonyPatch(typeof(TacticalAbility), "get_ShouldDisplay")]
        internal static class BC_TacticalAbility_get_ShouldDisplay_Patch
        {
            public static TacticalAbilityDef KnR_Dash_AbilityDef;
            public static void Postfix(TacticalAbility __instance, ref bool __result)
            {
                // Check if instance is KnR ability
                if (__instance.TacticalAbilityDef == KnR_Dash_AbilityDef)
                {
                    //  Set return value __result = true when ability is not disabled => show
                    __result = __instance.GetDisabledState() == AbilityDisabledState.NotDisabled;
                }
            }
        }
        
        private static void Change_Onslaught(DefCache defCache)
        {
            // This below works on the target but he can be targeted again from another Assault without any response => the Assault loses 2 AP and the target gets nothing
            // Looking for a solution, maybe MC fuctionality could be a solution (thx to Iko)
            // .... delayed ....
            //TacEffectStatusDef onslaughtStatus = Repo.GetAllDefs<TacEffectStatusDef>().FirstOrDefault(c => c.name.Equals("E_Status [DeterminedAdvance_AbilityDef]"));
            //onslaughtStatus.SingleInstance = true;
            PRMLogger.Debug("'" + MethodBase.GetCurrentMethod().DeclaringType.Name + "." + MethodBase.GetCurrentMethod().Name + "()' no changes implemented yet!");
            PRMLogger.Debug("----------------------------------------------------", false);
        }

        private static void Change_RapidClearance(DefCache defCache)
        {
            DefRepository Repo = TFTVMain.Repo;

            // Get Rapid Clearance ability def
            ApplyStatusAbilityDef rapidClearance = defCache.GetDef<ApplyStatusAbilityDef>("RapidClearance_AbilityDef");
            // Clone status apply effect from Vanish
            StatusEffectDef applyStatusEffect = Helper.CreateDefFromClone(
                defCache.GetDef<StatusEffectDef>("E_ApplyVanishStatusEffect [Vanish_AbilityDef]"),
                "8ea85920-588b-4e1d-a8e6-31ffbe9d3a02",
                "E_ApplyStatusEffect [RapidClearance_AbilityDef]");
            // Clone AP reduction statuses from QA
            AddAttackBoostStatusDef addAttackBoostStatus = Helper.CreateDefFromClone( // applies the AP reduction status only for the next attack
                defCache.GetDef<AddAttackBoostStatusDef>("E_Status [QuickAim_AbilityDef]"),
                "9385a73f-8d20-4022-acc1-9210e2e29b8f",
                "E_AttackBoostStatus [RapidClearance_AbilityDef]");
            ChangeAbilitiesCostStatusDef apReductionStatusEffect = Helper.CreateDefFromClone(
                defCache.GetDef<ChangeAbilitiesCostStatusDef>("E_AbilityCostModifier [QuickAim_AbilityDef]"),
                "e3062779-8f2f-4407-bc4f-a20f5c2d267b",
                "E_AbilityCostModifier [RapidClearance_AbilityDef]");
            // change properties and references
            apReductionStatusEffect.AbilityCostModification.RequiresProficientEquipment = false; // original QA true
            apReductionStatusEffect.AbilityCostModification.SkillTagCullFilter = new SkillTagDef[0]; // No restrictions, original QA disables melee and throwing grenades
            apReductionStatusEffect.AbilityCostModification.ActionPointMod = -0.5f; // -2 AP, original QA -1 AP
            addAttackBoostStatus.Visuals = rapidClearance.ViewElementDef;
            addAttackBoostStatus.SkillTagCullFilter = new SkillTagDef[0]; // No restrictions, original QA disables melee and throwing grenades
            addAttackBoostStatus.NumberOfAttacks = 2;
            addAttackBoostStatus.AdditionalStatusesToApply = new TacStatusDef[] { apReductionStatusEffect };
            applyStatusEffect.StatusDef = addAttackBoostStatus;
            rapidClearance.ViewElementDef.Description.LocalizationKey = "PR_BC_RAPID_CLEARANCE_DESC"; // new LocalizedTextBind("Until end of turn, after killing an enemy next attack cost -2AP", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            (rapidClearance.StatusDef as OnActorDeathEffectStatusDef).EffectDef = applyStatusEffect;
        }

        public static void Create_AimedBurst(DefCache defCache)
        {
            string skillName = "AimedBurst_AbilityDef";
            DefRepository Repo = TFTVMain.Repo;

            // Source to clone from
            //ShootAbilityDef source = defCache.GetDef<ShootAbilityDef>().FirstOrDefault(p => p.name.Equals("RageBurst_ShootAbilityDef"));
            ShootAbilityDef source = defCache.GetDef<ShootAbilityDef>("Gunslinger_AbilityDef");

            // Create Neccessary RuntimeDefs
            ShootAbilityDef aimedBurstAbility = Helper.CreateDefFromClone(
                source,
                "fc5f5cf1-1349-42ff-adc4-515d7ceddde4",
                skillName);
            AbilityCharacterProgressionDef progression = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "fa68ad15-a29b-4c66-b34a-fde332fc9d49",
                skillName);
            TacticalAbilityViewElementDef viewElement = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "13005fbc-2613-4a01-9355-0701ae350ca5",
                skillName);
            //SceneViewElementDef sceneView = Helper.CreateDefFromClone(
            //    source.SceneViewElementDef,
            //    "b1eefbc3-fb40-4733-a0cf-4efeecfc3af3",
            //    skillName);

            // Set fields
            aimedBurstAbility.CharacterProgressionData = progression;
            aimedBurstAbility.ViewElementDef = viewElement;
            //aimedBurstAbility.SceneViewElementDef = sceneView;
            //aimedBurstAbility.SkillTags = new SkillTagDef[] { rageBurst.SkillTags[0] };
            aimedBurstAbility.ActionPointCost = 0.75f;
            aimedBurstAbility.WillPointCost = 4.0f;
            aimedBurstAbility.ActorTags = new GameTagDef[] { defCache.GetDef<GameTagDef>("Assault_ClassTagDef") };
            aimedBurstAbility.EquipmentTags = new GameTagDef[] { defCache.GetDef<GameTagDef>("AssaultRifleItem_TagDef") };
            aimedBurstAbility.ExecutionsCount = 2;
            aimedBurstAbility.TraitsToApply = new string[] {
                "attack",
                "shoot",
                "ability"
            };
            //aimedBurstAbility.TargetsCount = 1;
            //aimedBurstAbility.ForceFirstPersonCam = false;
            aimedBurstAbility.ProjectileSpreadMultiplier = 0.7f;
            progression.RequiredStrength = 0;
            progression.RequiredWill = 0;
            progression.RequiredSpeed = 0;
            viewElement.DisplayName1.LocalizationKey = "PR_BC_AIMED_BURST";
            viewElement.Description.LocalizationKey = "PR_BC_AIMED_BURST_DESC";
            Sprite aimedBurst_IconSprite = defCache.GetDef<TacticalAbilityViewElementDef>("E_View [DeadlyDuo_ShootAbilityDef]").LargeIcon;
            viewElement.LargeIcon = aimedBurst_IconSprite;
            viewElement.SmallIcon = aimedBurst_IconSprite;
            aimedBurstAbility.DisablingStatuses = new StatusDef[]
            {
                defCache.GetDef<ApplyStatusAbilityDef>("ArmourBreak_AbilityDef").StatusDef,
            };
        }
    }
}
