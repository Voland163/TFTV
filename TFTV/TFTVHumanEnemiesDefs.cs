using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV
{
    internal class TFTVHumanEnemiesDefs
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
   
        public static void CreateHumanEnemiesTags()
        {
            try
            {


                string tagName = "HumanEnemy";
                string anu = "anu";
                string bandit = "ban";
                string newJericho = "nj";
                string synedrion = "syn";
                string forsaken = "FallenOnes";
                string pure = "Purists";

                GameTagDef source = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("Takeshi_Tutorial3_GameTagDef"));
                GameTagDef tier1GameTag = Helper.CreateDefFromClone(
                    source,
                    "11F227E3-A45A-44EE-8B93-94E59D8C7B53",
                    tagName + "Tier_1_" + "GameTagDef");
                GameTagDef tier2GameTag = Helper.CreateDefFromClone(
                    source,
                    "CE88CFDB-B010-40A7-A86A-C842DF5F35CF",
                    tagName + "Tier_2_" + "GameTagDef");
                GameTagDef tier3GameTag = Helper.CreateDefFromClone(
                    source,
                    "D4E764C5-3978-40C3-8CED-AFAF81B40BF8",
                    tagName + "Tier_3_" + "GameTagDef");
                GameTagDef tier4GameTag = Helper.CreateDefFromClone(
                    source,
                    "21D065AC-432F-4D29-92AF-5355EF972E38",
                    tagName + "Tier_4_" + "GameTagDef");
                GameTagDef anuGameTag = Helper.CreateDefFromClone(
                    source,
                    "1C8EC6EF-CE51-4AC5-B799-128FDE6ABF14",
                    tagName + "Faction_" + anu + "_GameTagDef");
                GameTagDef banditGameTag = Helper.CreateDefFromClone(
                    source,
                    "78993F15-9233-4C49-B8C3-13144156E438",
                    tagName + "Faction_" + bandit + "_GameTagDef");
                GameTagDef newJerichoGameTag = Helper.CreateDefFromClone(
                    source,
                    "62980A28-8E7A-4F0D-A01C-B58C4D085677",
                    tagName + "Faction_" + newJericho + "_GameTagDef");
                GameTagDef SynedrionGameTag = Helper.CreateDefFromClone(
                    source,
                    "B29CEA3A-6C24-4872-9773-02E2FC21F645",
                    tagName + "Faction_" + synedrion + "_GameTagDef");
                GameTagDef forsakenGameTag = Helper.CreateDefFromClone(
                    source,
                    "133FA2A8-C93D-43A9-BEFB-E5FAAAC43AFF",
                    tagName + "Faction_" + forsaken + "_GameTagDef");
                GameTagDef pureGameTag = Helper.CreateDefFromClone(
                    source,
                    "DDDAB7AC-1317-4B37-AB18-1E57F8D30147",
                    tagName + "Faction_" + pure + "_GameTagDef");
                GameTagDef humanEnemyTag = Helper.CreateDefFromClone(
                    source,
                    "BF6F6546-AE38-47E0-B581-FDB8F8F5171D",
                    tagName + "_GameTagDef");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        public static void ModifyMissionDefsToReplaceNeutralWithBandit()
        {
            try 
            {
                PPFactionDef banditFaction = Repo.GetAllDefs<PPFactionDef>().FirstOrDefault(p => p.name.Equals("NEU_Bandits_FactionDef"));

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    // TFTVLogger.Always("The first foreach went ok");


                    foreach (MutualParticipantsRelations relations in missionTypeDef.ParticipantsRelations)
                    {
                        // TFTVLogger.Always("The second foreach went ok");
                        if (relations.FirstParticipant == TacMissionParticipant.Player && relations.MutualRelation == FactionRelation.Enemy)
                        {
                            //   TFTVLogger.Always("The if inside the second foreach went ok");

                            if (missionTypeDef.ParticipantsData != null)
                            {
                                foreach (TacMissionTypeParticipantData data in missionTypeDef.ParticipantsData)
                                {
                                    //TFTVLogger.Always("The third foreach went Ok");

                                    if (data.ParticipantKind == relations.SecondParticipant)
                                    {
                                        // TFTVLogger.Always("The if inside the third foreach went ok");
                                        if (data.FactionDef != null)
                                        {
                                            if (missionTypeDef.name == "StoryAN1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryNJ_Chain1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryPX13_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN0_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN4_CustomMissionTypeDef")
                                            {
                                                data.FactionDef = banditFaction;
                                                TFTVLogger.Always("In mission " + missionTypeDef.name + " the enemy faction is " + data.FactionDef.name);
                                            }
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
        public static void CreateAmbushAbility()
        {
            try
            {

                string skillName = "HumanEnemiesTacticsAmbush_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef ambushAbility = Helper.CreateDefFromClone(
                    source,
                    "31785839-0687-4065-ACFB-255C1A1CE63D",
                    skillName);
                ambushAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "136290BA-D672-4EEF-822E-F3B8FF27496C",
                    skillName);
                ambushAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "6D47E347-35DE-4E8E-B6FF-9B9DF0598175",
                    skillName);
                ambushAbility.StatModifications = new ItemStatModification[]
                { new ItemStatModification {TargetStat = StatModificationTarget.BonusAttackDamage, Modification = StatModificationType.Multiply, Value = 1.10f},
                };
                ambushAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                ambushAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind("Ambush (Tactics)", true);
                ambushAbility.ViewElementDef.Description = new LocalizedTextBind
                    ("+10% damage. Received ability because Leader was alive and there were no enemies in sight within 10 tiles at the start of the turn.", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_TacticalAnalyst.png");
                ambushAbility.ViewElementDef.LargeIcon = icon;
                ambushAbility.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }


}
