using Base.Defs;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using System.Collections.Generic;
using PhoenixPoint.Tactical.Entities.Weapons;

namespace TFTVVehicleRework.Scarab
{
    public static class ScarabMain 
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;
        private static readonly SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;

        public enum HullModules {Front, Back, Left, Right, LFT, RFT, LBT, RBT}

        public static readonly Dictionary<HullModules, TacticalItemDef> DefaultHull = new Dictionary<HullModules, TacticalItemDef>
        {
            {HullModules.Front, (TacticalItemDef)Repo.GetDef("ed43834e-9480-ef84-1ae3-e6ecc44bb49b")},  //"PX_Scarab_Front_BodyPartDef"
            {HullModules.Back, (TacticalItemDef)Repo.GetDef("159f87f3-26e0-abf4-2a12-91af8f31eafc")},   //"PX_Scarab_Back_BodyPartDef"
            {HullModules.Left, (TacticalItemDef)Repo.GetDef("c35e0aff-f5ed-4e24-dab6-f96b6d21d389")},   //"PX_Scarab_Left_BodyPartDef"
            {HullModules.Right, (TacticalItemDef)Repo.GetDef("2a3b96b3-336e-89b4-fb51-84507954034a")},  //"PX_Scarab_Right_BodyPartDef"
            {HullModules.LFT, (TacticalItemDef)Repo.GetDef("c78bf90b-2a4f-a464-0b9b-a8e933bb2e2e")},    //"PX_Scarab_LeftFrontTyre_BodyPartDef"
            {HullModules.RFT, (TacticalItemDef)Repo.GetDef("e27f454e-f465-4284-1835-3710c08d9e18")},    //"PX_Scarab_RightFrontTyre_BodyPartDef"
            {HullModules.LBT, (TacticalItemDef)Repo.GetDef("73250440-0fc7-b274-4ada-50b3be1a132c")},    //"PX_Scarab_LeftBackTyre_BodyPartDef"
            {HullModules.RBT, (TacticalItemDef)Repo.GetDef("eac07fe7-a945-c674-08fa-982d111c8e3d")}     //"PX_Scarab_RightBackTyre_BodyPartDef"
        };

        public static void Change() 
        {
            Apply_GeneralChanges();
            Apply_WeaponChanges();      
            AmmunitionRacks.Change();
            DeploymentBay.Change();
            ReinforcedStabilisers.Change();
            LazarusShield.Change();  
        }

        private static void Apply_GeneralChanges()
        {
            //General HP/Armour/Speed changes for Scarab:
            ItemDef ScarabChassis = (ItemDef)Repo.GetDef("ac1b062d-5b12-83d4-f99d-95a8b25a56db"); //"_PX_Scarab_Chassis_ItemDef"
            foreach (AddonDef.SubaddonBind addon in ScarabChassis.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                
                if(BodyPart == DefaultHull[HullModules.Front])
                {
                    BodyPart.Armor = 40;
                    BodyPart.BodyPartAspectDef.Endurance = 24;
                }
                else if(BodyPart == DefaultHull[HullModules.Back])
                {
                    BodyPart.BodyPartAspectDef.Endurance = 12;
                }
                else if(BodyPart == DefaultHull[HullModules.Left] || BodyPart == DefaultHull[HullModules.Right])
                {
                    BodyPart.BodyPartAspectDef.Endurance = 17;
                }
                else if(BodyPart == DefaultHull[HullModules.LFT] ||
                        BodyPart == DefaultHull[HullModules.RFT] ||
                        BodyPart == DefaultHull[HullModules.LBT] ||
                        BodyPart == DefaultHull[HullModules.RBT] )
                {
                    BodyPart.Armor = 20;
                    BodyPart.BodyPartAspectDef.Speed = 9;
                }
            }
            //Update General Information to reflect new armour, HP and Speed
            GroundVehicleItemDef ScarabItem = (GroundVehicleItemDef)Repo.GetDef("986ffda3-f4a5-5824-0be8-55333b1a7264"); //"PX_Scarab_ItemDef"
            ScarabItem.DataDef.HitPoints = 700;
            ScarabItem.DataDef.Speed = 36;
            ScarabItem.DataDef.Armor = 25;
        }

        private static void Apply_WeaponChanges()
        {
            //Gemini only fires one shot per volley without module
            GroundVehicleWeaponDef Gemini = (GroundVehicleWeaponDef)Repo.GetDef("a598bebe-eaad-eb44-3870-92a861b1dc5e");
            Gemini.DamagePayload.AutoFireShotCount = 1;

            //ER 17->20 and Ammunition 4->8;
            GroundVehicleWeaponDef Taurus = (GroundVehicleWeaponDef)Repo.GetDef("d14af403-ec48-f954-48f3-7759a8fca9c2");
            Taurus.SpreadDegrees = (41f/20); //20 ER; ER = 41/Spread
            Taurus.ChargesMax = 8;
            Taurus.ManufactureTech = 30f;
            Taurus.ManufactureMaterials = 250f;
            Taurus.APToUsePerc = 75;

            //"PX_HeavyCannon_WeaponDef"
            WeaponDef Hell2 = (WeaponDef)Repo.GetDef("112a754d-413f-27f4-180c-b052cab71d70");
            Taurus.MainSwitch = Hell2.MainSwitch;
            Taurus.VisualEffects = Hell2.VisualEffects;
            Taurus.DamagePayload.ProjectileVisuals = Hell2.DamagePayload.ProjectileVisuals;

            //"PX_HelCannon_ResearchDef_ManufactureResearchRewardDef_0"
            ManufactureResearchRewardDef HellCannonResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("80d2b708-8ee3-228b-278e-acfa09815d64");
            HellCannonResearchReward.Items = HellCannonResearchReward.Items.AddToArray(Taurus);

            //Scorpio gets a whole new Damage Payload inc Virophage.
            GroundVehicleWeaponDef Scorpio = (GroundVehicleWeaponDef)Repo.GetDef("84f2634c-1da6-d114-e8dc-ddd923450c9d");
            Scorpio.HitPoints = 320;
            Scorpio.DamagePayload.AutoFireShotCount = 1;
            Scorpio.ManufactureTech = 70f;
            Scorpio.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
			{
				new DamageKeywordPair{DamageKeywordDef = keywords.BlastKeyword, Value = 60},
				new DamageKeywordPair{DamageKeywordDef = keywords.ShreddingKeyword, Value = 10},
                new DamageKeywordPair{DamageKeywordDef = (DamageKeywordDef)Repo.GetDef("c968f22f-392d-1964-68cd-edd14655082d"), Value = 90} //"Virophage_DamageKeywordDataDef"
			};

            //"PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0"
            ManufactureResearchRewardDef VirophageResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("02502242-0b31-a2b3-6d1b-2e78db0465e4");
            VirophageResearchReward.Items = VirophageResearchReward.Items.AddToArray(Scorpio);
        }
    }  
}