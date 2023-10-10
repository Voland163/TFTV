using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace TFTVVehicleRework.KaosBuggy
{
    public static class Slaughterhouse
    {
        private static readonly DefRepository Repo = KaosBuggyMain.Repo;
        
        //"KS_Kaos_Buggy_Spiked_Armor_Plating_Hull_GroundVehicleModuleDef"
        internal static readonly GroundVehicleModuleDef SpikedHull = (GroundVehicleModuleDef)Repo.GetDef("387d72e4-a573-9e54-eb56-6198349753b8");
        public static void Change()
        {
            SpikedHull.Armor = 0f; 
            SpikedHull.ViewElementDef.DisplayName1 = new LocalizedTextBind("KB_SLAUGHTERHOUSE_NAME");
            SpikedHull.BodyPartAspectDef.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification
                {
                    TargetStat = PhoenixPoint.Common.Entities.StatModificationTarget.UnitsInside,
                    Modification = Base.Entities.Statuses.StatModificationType.Add,
                    Value = -1f
                }
            };
            SpikedHull.BodyPartAspectDef.Endurance = 35;
            Adjust_BodyParts();
            Fix_Spikes();
        }

        private static void Adjust_BodyParts()
        {
            foreach(AddonDef.SubaddonBind addon in SpikedHull.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                if (BodyPart.name == "KS_Kaos_Buggy_Spiked_Armor_Front_BodyPartDef")
                {
                    BodyPart.HitPoints = 250;
                    BodyPart.Armor = 40;
                }
                else if (BodyPart.name == "KS_Kaos_Buggy_Spiked_Armor_Left_BodyPartDef" || BodyPart.name == "KS_Kaos_Buggy_Spiked_Armor_Right_BodyPartDef")
                {
                    BodyPart.HitPoints = 200;
                    BodyPart.Armor = 30;
                }
                else if (BodyPart.name == "KS_Kaos_Buggy_Spiked_Armor_LeftBackTyre_BodyPartDef")
                {
                    BodyPart.HitPoints = 150;
                }
                else
                {
                    BodyPart.HitPoints = 150;
                }
            }
        }
        
        private static void Fix_Spikes()
        {
            TacticalReturnMeleeDamageDef SpikesRetaliation = (TacticalReturnMeleeDamageDef)Repo.GetDef("3193ad25-9a32-d0f4-bb8a-fde688328745");
			SpikesRetaliation.ApplyTraitsOnActivate = false;
			SpikesRetaliation.minDamage = 0;
			SpikesRetaliation.ViewElementDef.ShowInStatusScreen = true;
        }
    }
}
            