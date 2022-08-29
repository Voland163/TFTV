using Base.Core;
using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVRevenantDefs
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<string, int> DeadSoldiersDelirium = new Dictionary<string, int>();
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

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
                newAbilityStatusDef.ApplicationConditions = new EffectConditionDef[] { };
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
                PassiveModifierAbilityDef revenantAssault = Helper.CreateDefFromClone(
                    source,
                    "1045EB8D-1916-428F-92EF-A15FD2807818",
                    skillName);
                revenantAssault.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "7FF5A3CF-6BBD-4E4F-9E80-2DB7BDB29112",
                    skillName);
                revenantAssault.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "47BE3577-1D68-4FB2-BFA3-0A158FC710D9",
                    skillName);
                revenantAssault.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                };
                revenantAssault.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantAssault.ViewElementDef.DisplayName1 = new LocalizedTextBind("Assault Revenant", true);
                revenantAssault.ViewElementDef.Description = new LocalizedTextBind("+10% Damage", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantAssault.ViewElementDef.LargeIcon = icon;
                revenantAssault.ViewElementDef.SmallIcon = icon;

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
                PassiveModifierAbilityDef revenantBerserker = Helper.CreateDefFromClone(
                    source,
                    "FD3FE516-25BA-44F2-9770-3AA4AD1DCB91",
                    skillName);
                revenantBerserker.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "E2707CBD-3D99-4EA4-A48D-B8E6E14EFDFD",
                    skillName);
                revenantBerserker.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "3F74FAF1-1A87-4E2A-AEC2-CBB0BA5A14E0",
                    skillName);
                revenantBerserker.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.AddMax, Value = 4},
                new ItemStatModification {TargetStat = StatModificationTarget.Speed, Modification = StatModificationType.Add, Value = 4},
                };
                revenantBerserker.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                revenantBerserker.ViewElementDef.DisplayName1 = new LocalizedTextBind("Berserker Revenant", true);
                revenantBerserker.ViewElementDef.Description = new LocalizedTextBind("+4 Speed", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantBerserker.ViewElementDef.LargeIcon = icon;
                revenantBerserker.ViewElementDef.SmallIcon = icon;

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
                PassiveModifierAbilityDef heavy = Helper.CreateDefFromClone(
                    source,
                    "A8603522-3472-4A95-9ADF-F27E8B287D15",
                    skillName);
                heavy.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "AA5F572B-D86B-4C00-B8B9-4D86EE5F7F4D",
                    skillName);
                heavy.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "F8781E78-D106-44B3-A0E6-855BCAEB0A2F",
                    skillName);
                heavy.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.AddMax, Value = 10},
                  new ItemStatModification {TargetStat = StatModificationTarget.Health, Modification = StatModificationType.Add, Value = 100},
                };
                heavy.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                heavy.ViewElementDef.DisplayName1 = new LocalizedTextBind("Heavy Revenant", true);
                heavy.ViewElementDef.Description = new LocalizedTextBind("+10 Strength", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                heavy.ViewElementDef.LargeIcon = icon;
                heavy.ViewElementDef.SmallIcon = icon;

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
                PassiveModifierAbilityDef infiltrator = Helper.CreateDefFromClone(
                    source,
                    "6C56E0F9-56BB-41D2-AFB1-08C8A49F69FA",
                    skillName);
                infiltrator.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "1F8B6D09-A2C5-4B3F-BBED-F59675301ABB",
                    skillName);
                infiltrator.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6CAFD922-60C6-449E-A652-C2BD94386BE5",
                    skillName);
                infiltrator.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Stealth, Modification = StatModificationType.MultiplyMax, Value = 1.15f},
                };
                infiltrator.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                infiltrator.ViewElementDef.DisplayName1 = new LocalizedTextBind("Infiltrator Revenant", true);
                infiltrator.ViewElementDef.Description = new LocalizedTextBind("+15% Stealth", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                infiltrator.ViewElementDef.LargeIcon = icon;
                infiltrator.ViewElementDef.SmallIcon = icon;

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
                PassiveModifierAbilityDef priest = Helper.CreateDefFromClone(
                    source,
                    "0816E671-D396-4212-910F-87B5DEC6ADE2",
                    skillName);
                priest.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C1C7FBEA-2C0B-4930-A73C-15BF3A987784",
                    skillName);
                priest.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "460AAE12-0541-40AB-A4EE-E3E206A96FB4",
                    skillName);
                priest.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 10},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 10},
                };
                priest.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                priest.ViewElementDef.DisplayName1 = new LocalizedTextBind("Priest Revenant", true);
                priest.ViewElementDef.Description = new LocalizedTextBind("+10 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                priest.ViewElementDef.LargeIcon = icon;
                priest.ViewElementDef.SmallIcon = icon;

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
                PassiveModifierAbilityDef sniper = Helper.CreateDefFromClone(
                    source,
                    "4A2C53A3-D9DB-456A-8B88-AB2D90BE1DB5",
                    skillName);
                sniper.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0D811905-8C70-4D46-9CF2-1A31C5E98ED1",
                    skillName);
                sniper.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "7DCCCAAA-7245-4245-9033-F6320CCDA2AB",
                    skillName);
                sniper.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.Perception, Modification = StatModificationType.Add, Value = 10},
                };
                sniper.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                sniper.ViewElementDef.DisplayName1 = new LocalizedTextBind("Sniper Revenant", true);
                sniper.ViewElementDef.Description = new LocalizedTextBind("+10 Perception", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                sniper.ViewElementDef.LargeIcon = icon;
                sniper.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

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
                { new ItemStatModification {TargetStat = StatModificationTarget.Endurance, Modification = StatModificationType.Add, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Endurance,Modification = StatModificationType.AddMax, Value = 5},
                new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.Add, Value = 5},
                 new ItemStatModification {TargetStat = StatModificationTarget.Willpower, Modification = StatModificationType.AddMax, Value = 5}
                };
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Technician Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("+5 Strength, +5 Willpower", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateRevenantResistanceAbility()
        {
            try
            {
                string skillName = "RevenantResistance_AbilityDef";
                DamageMultiplierAbilityDef source = Repo.GetAllDefs<DamageMultiplierAbilityDef>().FirstOrDefault(p => p.name.Equals("FireResistant_DamageMultiplierAbilityDef"));
                DamageMultiplierAbilityDef revenantResistance = Helper.CreateDefFromClone(
                    source,
                    "A7F8113B-B281-4ECD-99FE-3125FCE029C4",
                    skillName);
                revenantResistance.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "C298F900-A7D5-4EEC-96E1-50D017614396",
                    skillName);
                revenantResistance.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "B737C223-52D0-413B-B48F-978AD5D5BB33",
                    skillName);

                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                revenantResistance.ViewElementDef.LargeIcon = icon;
                revenantResistance.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantGameTags()
        {
            string skillName = "RevenantTier";
            GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
            GameTagDef revenantTier1GameTag = Helper.CreateDefFromClone(
                source,
                "1677F9F4-5B45-47FA-A119-83A76EF0EC70",
                skillName + "_1_" + "GameTagDef");
            GameTagDef revenantTier2GameTag = Helper.CreateDefFromClone(
                source,
                "9A807A62-D51D-404E-ADCF-ABB4A888202E",
                skillName + "_2_" + "GameTagDef");
            GameTagDef revenantTier3GameTag = Helper.CreateDefFromClone(
                source,
                "B4BD3091-8522-4F3C-8A0F-9EE522E0E6B4",
                skillName + "_3_" + "GameTagDef");
        }

    }
}
