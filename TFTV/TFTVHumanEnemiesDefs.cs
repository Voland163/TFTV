using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
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
                string forsaken = "fo";
                string pure = "pu";

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
                    tagName + "Faction_" + anu + "_FactionGameTagDef");
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
                                                missionTypeDef.name == "SYN4_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN0_CustomMissionTypeDef")
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
        /* Don't need as will patch in to add the text 
        public static void CreateHumanEnemiesRanks()
        {
            try
            {
                string skillName = "HumanEnemy_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "B1DD1BB0-D504-4E96-BAAC-F99D53853231",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "44282C23-3546-4EB6-AFB6-85EB280973E5",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "F48DC754-728D-43EA-B69A-BE8F3F225513",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[0];
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("Nothing because fail", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("UI_StatusesIcons_CanBeRecruitedIntoPhoenix-2.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }*/

    }


}
