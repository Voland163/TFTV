using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV
{
    internal class TFTVRevenant
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static Dictionary<string, int> DeadSoldiersDelirium = new Dictionary<string, int>();

        public static void AddtoListOfDeadSoldiers(TacticalActor deadSoldier)

        {

            try
            {
                int delirium = deadSoldier.CharacterStats.Corruption.IntValue;
                string name = deadSoldier.GetDisplayName();

                DeadSoldiersDelirium.Add(name, delirium);


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateRevenantTemplate(string tacCharacterDef) 
        {
            try 
            {           
                TacCharacterDef templateDef = Repo.GetAllDefs<TacCharacterDef>().FirstOrDefault(gvw => gvw.name.Equals(tacCharacterDef));
                templateDef.Data.LocalizeName = false;
                templateDef.Data.Name = "Revenant";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateRevenantAbility(string name) 
        {
            try 
            {

                string skillName = "Revenant_AbilityDef";
                PassiveModifierAbilityDef source = Repo.GetAllDefs<PassiveModifierAbilityDef>().FirstOrDefault(p => p.name.Equals("SelfDefenseSpecialist_AbilityDef"));
                PassiveModifierAbilityDef hallucinating = Helper.CreateDefFromClone(
                    source,
                    "5d3421cb-9e22-4cdf-bcac-3beac61b2713",
                    skillName);
                hallucinating.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "92560850-084c-4d43-8c57-a4f5773e4a26",
                    skillName);
                hallucinating.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "b8c58fc2-c56e-4577-a187-c0922cba8468",
                    skillName);
                hallucinating.StatModifications = new ItemStatModification[0];
                hallucinating.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                hallucinating.ViewElementDef.DisplayName1 = new LocalizedTextBind("Revenant", true);
                hallucinating.ViewElementDef.Description = new LocalizedTextBind("This is your fallen comrade, " + name + ", returned as Pandoran monstrosity", true);
                Sprite icon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                hallucinating.ViewElementDef.LargeIcon = icon;
                hallucinating.ViewElementDef.SmallIcon = icon;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
      
    }

}

