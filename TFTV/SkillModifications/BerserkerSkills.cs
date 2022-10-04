using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using com.ootii.Collections;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses.SkillModifications
{
    internal class BerserkerSkills
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ApplyChanges()
        {
            // Dash: Move up to 13 tiles. Limited to 1 use per turn
            Change_Dash();

            // Ignore Pain: Remove MC immunity
            Change_IgnorePain();

            // Bloodlust: reduce buffs to max 25%
            Change_Bloodlust();

            // Adrenaline Rush: 1 AP for one handed weapons and skills, no WP restriction
            Change_AdrenalineRush();

            // Gun Kata 0AP 2WP Shoot your handgun for free. Limited to 2 uses per turn.
            Create_GunKata();

            // Exertion: 0AP 2WP Recover 1AP. Next turn you have -1 AP. Limited to 1 use per turn.
            Create_Exertion();
        }

        private static void Change_Bloodlust()
        {
            float maxBoost = 0.25f;
            ViewElementDef blView = Repo.GetAllDefs<ViewElementDef>().FirstOrDefault(ve => ve.name.Equals("E_ViewElement [BloodLust_AbilityDef]"));
            BloodLustStatusDef blStatus = Repo.GetAllDefs<BloodLustStatusDef>().FirstOrDefault(bl => bl.name.Equals("E_Status [BloodLust_AbilityDef]"));
            blStatus.MaxBoost = maxBoost;
            blView.Description.LocalizationKey = "PR_BC_BLOODLUST_DESC"; // new LocalizedTextBind($"Gain up to {maxBoost * 100}% Speed and Damage based on lost Health", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }

        private static void Change_Dash()
        {
            //float dashRange = 13f;
            //int dashUsesPerTurn = 1;
            //string dashDescription = $"Move up to {(int)dashRange} tiles. Limited to {dashUsesPerTurn} use per turn";
            Sprite dashIcon = Repo.GetAllDefs<TacticalAbilityViewElementDef>().FirstOrDefault(tave => tave.name.Equals("E_View [BodySlam_AbilityDef]")).LargeIcon;
            //
            RepositionAbilityDef dash = Repo.GetAllDefs<RepositionAbilityDef>().FirstOrDefault(r => r.name.Equals("Dash_AbilityDef"));
            //dash.TargetingDataDef.Origin.Range = dashRange;
            //dash.ViewElementDef.Description = new LocalizedTextBind(dashDescription, doNotLocalize);
            dash.ViewElementDef.LargeIcon = dashIcon;
            dash.ViewElementDef.SmallIcon = dashIcon;
            //dash.UsesPerTurn = dashUsesPerTurn;
            //dash.AmountOfMovementToUseAsRange = -1.0f;
        }

        private static void Change_IgnorePain()
        {
            PRMLogger.Debug("'" + MethodBase.GetCurrentMethod().DeclaringType.Name + "." + MethodBase.GetCurrentMethod().Name + "()' no changes implemented yet!");
            PRMLogger.Debug("----------------------------------------------------", false);
            //// Remove Ignore Pain from mind control application conditions
            //MindControlStatusDef mcStatus = Repo.GetAllDefs<MindControlStatusDef>().FirstOrDefault(mcs => mcs.name.Equals("MindControl_StatusDef"));
            //List<EffectConditionDef> mcApplicationConditions = mcStatus.ApplicationConditions.ToList();
            //if (mcApplicationConditions.Remove(Repo.GetAllDefs<EffectConditionDef>().FirstOrDefault(ec => ec.name.Contains("IgnorePain"))))
            //{
            //    mcStatus.ApplicationConditions = mcApplicationConditions.ToArray();
            //}
            //// Change description
            //ApplyStatusAbilityDef ignorePain = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(asa => asa.name.Equals("IgnorePain_AbilityDef"));
            //ignorePain.ViewElementDef.Description = new LocalizedTextBind("Disabled body parts remain functional and cannot Panic.", doNotLocalize);
        }

        private static void Change_AdrenalineRush()
        {
            ApplyStatusAbilityDef adrenalineRush = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(asa => asa.name.Equals("AdrenalineRush_AbilityDef"));
            adrenalineRush.StatusDef = Repo.GetAllDefs<ChangeAbilitiesCostStatusDef>().FirstOrDefault(sd => sd.name.Equals("E_SetAbilitiesTo1AP [AdrenalineRush_AbilityDef]"));
            adrenalineRush.ViewElementDef.Description.LocalizationKey = "PR_BC_ADRENALINE_DESC"; // new LocalizedTextBind("Until end of turn one-handed weapon and all non-weapon skills cost 1AP, except Recover.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
        }
        // Adrenaline Rush: Patching AbilityQualifies of ARs TacticalAbilityCostModification to determine which ability should get modified
        [HarmonyPatch(typeof(TacticalAbilityCostModification), "AbilityQualifies")]
        internal static class AR_AbilityQualifies_patch
        {
            internal static List<string> arExcludeList = new List<string>()
            {
                "RecoverWill_AbilityDef",
                "Overwatch_AbilityDef"
            };
            internal static SkillTagDef attackAbility_Tag = Repo.GetAllDefs<SkillTagDef>().FirstOrDefault(st => st.name.Equals("AttackAbility_SkillTagDef"));
            internal static ApplyStatusAbilityDef adrenalineRush = Repo.GetAllDefs<ApplyStatusAbilityDef>().FirstOrDefault(asa => asa.name.Equals("AdrenalineRush_AbilityDef"));
            internal static ChangeAbilitiesCostStatusDef arStatus = Repo.GetAllDefs<ChangeAbilitiesCostStatusDef>().FirstOrDefault(sd => sd.name.Equals("E_SetAbilitiesTo1AP [AdrenalineRush_AbilityDef]"));

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(TacticalAbilityCostModification __instance, ref bool __result, TacticalAbility ability)
            {
                try
                {
                    if (ability.TacticalActor.Status.HasStatus(arStatus) && __instance == arStatus.AbilityCostModification)
                    {
                        __result = ability.TacticalAbilityDef.SkillTags.Contains(attackAbility_Tag)
                            ? ability.Equipment == null || ability.Equipment.HandsToUse == 1
                            : !arExcludeList.Contains(ability.TacticalAbilityDef.name);
                    }
                }
                catch (Exception e)
                {
                    PRMLogger.Error(e);
                }
            }
        }

        private static void Create_GunKata()
        {
            int usesPerTurn = 2;
            float wpCost = 2.0f;
            float accMod = 1.0f;
            bool useFPC = true;
            string skillName = "GunKata_AbilityDef";
            //LocalizedTextBind name = new LocalizedTextBind("GUN KATA", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Shoot your handgun for free. Limited to 2 uses per turn.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            // Get some basics from repo
            ShootAbilityDef source = Repo.GetAllDefs<ShootAbilityDef>().FirstOrDefault(sa => sa.name.Equals("Gunslinger_AbilityDef"));
            GameTagDef handgunWeaponTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(gt => gt.name.Equals("HandgunItem_TagDef"));

            ShootAbilityDef GunKata = Helper.CreateDefFromClone(
                source,
                "f7d997ce-1272-4337-a55e-97ecab56d58e",
                skillName);
            GunKata.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "fe360ad7-fd39-432b-97c9-8354f1823dbd",
                skillName);
            GunKata.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "cf65b3b6-ec33-48ab-a08f-71a3cb44567a",
                skillName);
            GunKata.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_GUN_KATA"; // name;
            GunKata.ViewElementDef.Description.LocalizationKey = "PR_BC_GUN_KATA_DESC"; // description;
            GunKata.EquipmentTags = new GameTagDef[] { handgunWeaponTag };
            GunKata.UsesPerTurn = usesPerTurn;
            GunKata.WillPointCost = wpCost;
            GunKata.CanUseFirstPersonCam = useFPC;
            GunKata.ProjectileSpreadMultiplier = accMod;
        }

        private static void Create_Exertion()
        {
            int usesPerTurn = 1;
            float wpCost = 5.0f;
            float apMod = 50.0f;
            string skillName = "Exertion_AbilityDef";
            //LocalizedTextBind name = new LocalizedTextBind("EXERTION", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);
            //LocalizedTextBind description = new LocalizedTextBind("Recover 1AP. Limited to 1 use per turn.", TFTVMain.Main.Settings.DoNotLocalizeChangedTexts);

            // Get some basics from repo
            ExtraMoveAbilityDef source = Repo.GetAllDefs<ExtraMoveAbilityDef>().FirstOrDefault(asa => asa.name.Equals("ExtraMove_AbilityDef"));
            //ExtraMoveAbilityDef Exertion = Repo.GetAllDefs<ExtraMoveAbilityDef>().FirstOrDefault(asa => asa.name.Equals("ExtraMove_AbilityDef"));

            ExtraMoveAbilityDef Exertion = Helper.CreateDefFromClone(
                source,
                "790233f5-5aa5-4769-931c-c2f740271836",
                skillName);
            Exertion.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "f0bdbe30-2947-49f6-a1e7-276c6245861b",
                skillName);
            Exertion.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "d20f2149-a24b-4419-8a7f-b86bb7837a4d",
                skillName);
            Exertion.UsesPerTurn = usesPerTurn;
            Exertion.WillPointCost = wpCost;
            Exertion.ViewElementDef.DisplayName1.LocalizationKey = "PR_BC_EXERTION"; // name;
            Exertion.ViewElementDef.Description.LocalizationKey = "PR_BC_EXERTION_DESC"; // = description;
            Exertion.ActionPointsReturnedPerc = apMod;

            //AbilityDef animSource = Repo.GetAllDefs<AbilityDef>().FirstOrDefault(ad => ad.name.Equals("Priest_InstilFrenzy_AbilityDef"));
            //foreach (TacActorSimpleAbilityAnimActionDef animActionDef in Repo.GetAllDefs<TacActorSimpleAbilityAnimActionDef>().Where(aad => aad.name.Contains("Soldier_Utka_AnimActionsDef")))
            //{
            //    if (animActionDef.AbilityDefs != null && animActionDef.AbilityDefs.Contains(animSource) && !animActionDef.AbilityDefs.Contains(Exertion))
            //    {
            //        animActionDef.AbilityDefs = animActionDef.AbilityDefs.Append(Exertion).ToArray();
            //        PRMLogger.Debug("Anim Action '" + animActionDef.name + "' set for abilities:");
            //        foreach (AbilityDef ad in animActionDef.AbilityDefs)
            //        {
            //            PRMLogger.Debug("  " + ad.name);
            //        }
            //        PRMLogger.Debug("----------------------------------------------------", false);
            //    }
            //}
        }
    }
}
