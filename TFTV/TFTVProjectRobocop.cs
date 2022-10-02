using Base.Defs;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVProjectRobocop
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly BCSettings bCSettings = TFTVMain.Main.Settings;

        private static readonly string RobocopEvent = "RoboCopDeliveryEvent";

        private static readonly TacCharacterDef characterDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(p => p.name.Equals("NJ_Jugg_TacCharacterDef"));

        private static readonly TacticalItemDef juggHead = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Jugg_BIO_Helmet_BodyPartDef"));
        private static readonly TacticalItemDef juggLegs = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Jugg_BIO_Legs_ItemDef"));
        private static readonly TacticalItemDef juggTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Jugg_BIO_Torso_BodyPartDef"));
        private static readonly TacticalItemDef exoHead = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Exo_BIO_Helmet_BodyPartDef"));
        private static readonly TacticalItemDef exoLegs = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Exo_BIO_Legs_ItemDef"));
        private static readonly TacticalItemDef exoTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("NJ_Exo_BIO_Torso_BodyPartDef"));
        private static readonly TacticalItemDef shinobiHead = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("SY_Shinobi_BIO_Helmet_BodyPartDef"));
        private static readonly TacticalItemDef shinobiLegs = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("SY_Shinobi_BIO_Legs_ItemDef"));
        private static readonly TacticalItemDef shinobiTorso = Repo.GetAllDefs<TacticalItemDef>().FirstOrDefault(p => p.name.Equals("SY_Shinobi_BIO_Torso_BodyPartDef"));


        public static void CreateRoboCopDef()
        {
            try
            {

                TacCharacterDef roboCopDef = Helper.CreateDefFromClone(characterDef, "36B01740-2E05-4D5A-9C2E-24D506C6F584", "RoboCop");
                //   roboCopDef.name = "RoboCop";
                //   roboCopDef.SpawnCommandId = "DeadOrAliveYouAreComingWithMe";
                roboCopDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopDef.Data.InventoryItems = new ItemDef[] { };
                roboCopDef.Data.Abilites = new TacticalAbilityDef[] { };
                //   roboCopDef.Data.GameTags = new GameTagDef[] { };
                //  roboCopDef.Data.BodypartItems = new ItemDef[] { juggHead, juggTorso, juggLegs };

                TacCharacterDef roboCopScoutDef = Helper.CreateDefFromClone(characterDef, "BF7FDA63-18BA-4B1B-9AD6-A17643471530", "RoboCopScoutDef");
                roboCopScoutDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopScoutDef.Data.InventoryItems = new ItemDef[] { };
                roboCopScoutDef.Data.Abilites = new TacticalAbilityDef[] { };
                roboCopScoutDef.Data.GameTags = new GameTagDef[] { };
                roboCopScoutDef.Data.BodypartItems = new ItemDef[] { exoHead, exoTorso, exoLegs };

                TacCharacterDef roboCopShinobiDef = Helper.CreateDefFromClone(characterDef, "90C338B9-BA40-44AE-8CAA-2F988D300E08", "RoboCopShinobiDef");
                roboCopShinobiDef.Data.EquipmentItems = new ItemDef[] { };
                roboCopShinobiDef.Data.InventoryItems = new ItemDef[] { };
                roboCopShinobiDef.Data.Abilites = new TacticalAbilityDef[] { };
                roboCopShinobiDef.Data.GameTags = new GameTagDef[] { };
                roboCopShinobiDef.Data.BodypartItems = new ItemDef[] { shinobiHead, shinobiTorso, shinobiLegs };

                TFTVLogger.Always("RoboCop Defs created");


                //need to change name, and class (roboCopDef.Data.Abilites and roboCopDef.Data.GameTags)
                //to add different equipment sets roboCopDef.Data.BodypartItems



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRoboCopDeliveryEvent()
        {
            try
            {
                TFTVLogger.Always("Robocop def real " + Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(p => p.name.Equals("RoboCop")).name);



                TFTVLogger.Always("Check that Murphy is in the list " + bCSettings.SpecialCharacterPersonalSkills.Keys.Contains("Murphy"));
                //NJ_Exo_BIO_Helmet_BodyPartDef
                //NJ_Exo_BIO_Legs_ItemDef
                //NJ_Exo_BIO_Torso_BodyPartDef


                //SY_Shinobi_BIO_Helmet_BodyPartDef
                //SY_Shinobi_BIO_Legs_ItemDef
                //SY_Shinobi_BIO_Torso_BodyPartDef


                GeoscapeEventDef roboCopDeliveryEvent = TFTVCommonMethods.CreateNewEvent(RobocopEvent, "ROBOCOP_DELIVERY_TITLE", "ROBOCOP_DELIVERY_TEXT", "ROBOCOP_DELIVERY_OUTCOME");
                TacCharacterDef roboCopDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(p => p.name.Equals("RoboCop"));


                // characterDef.Data.Name = "Murphy";
                roboCopDeliveryEvent.GeoscapeEventData.Choices[0].Outcome.CustomCharacters.Add(roboCopDef);



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void CreateBionicMan(GeoLevelController levelController)
        {
            try
            {
                if (TFTVRevenant.RevenantsKilled.Keys.Count > 0) 
                { 
                int idChosen = 0;

                foreach (int id in TFTVRevenant.RevenantsKilled.Keys)
                {
                    if (TFTVRevenant.RevenantsKilled[id] == 0)
                    {
                        idChosen = id;

                    }
                }

                GeoTacUnitId bionicMan = new GeoTacUnitId();
                if (idChosen != 0)
                {
                    foreach (GeoTacUnitId deadSoldier in levelController.DeadSoldiers.Keys)
                    {
                        if (deadSoldier == idChosen)
                        {
                            TFTVLogger.Always("The chosen dead soldier is " + levelController.DeadSoldiers[deadSoldier].GetName());
                            bionicMan = deadSoldier;
                        }
                    }
                }

                    if (bionicMan != null)
                    {

                        bCSettings.SpecialCharacterPersonalSkills.Add(levelController.DeadSoldiers[bionicMan].Identity.Name, new Dictionary<int, string>()
                {
                    {0,"Priest_MindControl_AbilityDef"},
                    // ...
                });

                        TacCharacterDef roboCopDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(p => p.name.Equals("RoboCop"));
                        roboCopDef.Data.Name = levelController.DeadSoldiers[bionicMan].Identity.Name;
                        roboCopDef.Data.LocalizeName = false;




                        GeoscapeEventDef roboCopDeliveryEvent = levelController.EventSystem.GetEventByID(RobocopEvent);

                        if (levelController.PhoenixFaction.Research.HasCompleted("NJ_Bionics2_ResearchDef"))
                        {
                            TFTVCommonMethods.GenerateGeoEventChoice(roboCopDeliveryEvent, "ROBOCOP_DELIVERY_SCOUT", "ROBOCOP_DELIVERY_OUTCOME");

                        }
                        if (levelController.PhoenixFaction.Research.HasCompleted("SYN_Bionics3_ResearchDef"))
                        {
                            TFTVCommonMethods.GenerateGeoEventChoice(roboCopDeliveryEvent, "ROBOCOP_DELIVERY_SHINOBI", "ROBOCOP_DELIVERY_OUTCOME");
                        }





                        GeoSite phoenixBase = levelController.PhoenixFaction.Bases.First().Site;
                        GeoscapeEventContext context = new GeoscapeEventContext(phoenixBase, levelController.PhoenixFaction);
                        levelController.EventSystem.TriggerGeoscapeEvent(RobocopEvent, context);
                    }
                }
                // bionicManDef.Data.Name = levelController.DeadSoldiers[bionicMan].GetName();
                // bionicManDef.Data.CharacterProgression.AddSkillPoints(levelController.DeadSoldiers[bionicMan].Level * 20);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

    }
}
