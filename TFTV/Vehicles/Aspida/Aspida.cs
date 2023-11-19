using Base.Defs;
using Base.Entities.Abilities;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace TFTVVehicleRework.Aspida
{
    public static class AspidaMain
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;
        private static readonly SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;

        //"SYN_Rover_ResearchDef_ManufactureResearchRewardDef_1"
        private static readonly ManufactureResearchRewardDef Rover = (ManufactureResearchRewardDef)Repo.GetDef("7590a85d-b8e1-5157-ffd6-20a635d0b29e");    

        public static void Change()
        {
            Rebalance_Weapons();
            Rebalance_Bodyparts();
            Update_ItemInfo();
            Give_LeapByDefault();
            StasisChamber.Change();
            PsychicJammer.Change();
            HermesX1.Change();
            ClericX2.Change();
        }
        
        private static void Rebalance_Weapons()
        {
            // "SY_Aspida_Arms_GroundVehicleWeaponDef"
            GroundVehicleWeaponDef Arms = (GroundVehicleWeaponDef)Repo.GetDef("e29f25fa-96cb-dac4-7983-88072fd2ab76");
            Arms.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.ParalysingKeyword).Value = 12f;
            
            // "SY_Aspida_Themis_GroundVehicleWeaponDef"
            GroundVehicleWeaponDef Themis = (GroundVehicleWeaponDef)Repo.GetDef("544551d2-6de2-7304-9af1-870037ec8e82");
            Themis.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.ParalysingKeyword).Value = 10f;
            // Themis.APToUsePerc = 75;
            // Themis.ChargesMax = 32;
            Themis.ChargesMax = 40;
            Themis.ManufacturePointsCost = 250f;
            Themis.Abilities = new AbilityDef[]
            {
                AspidaShootAbility()
            };
            Update_Requirements(Themis);

            // "SY_Aspida_Apollo_GroundVehicleWeaponDef"
            GroundVehicleWeaponDef Apollo = (GroundVehicleWeaponDef)Repo.GetDef("f2032edf-1890-7784-a974-6718e2000b16");
            Apollo.ChargesMax = 20;
            Apollo.Abilities = new AbilityDef[]
            {
                AspidaShootAbility()
            };
        }

        private static void Rebalance_Bodyparts()
        {
            //"_SY_Aspida_Chassis_ItemDef"
            ItemDef AspidaChassis = (ItemDef)Repo.GetDef("f938d6f1-c2ea-d224-fa3d-280f5c31e820");
            foreach (AddonDef.SubaddonBind addon in AspidaChassis.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                if(BodyPart.name == "SY_Aspida_Hull_BodyPartDef")
                {
                    continue;
                }
                if(BodyPart.name == "SY_Aspida_Body_BodyPartDef")
                {
                    BodyPart.Armor = 40f;
                }
                else
                {
                    BodyPart.BodyPartAspectDef.Speed = 8f;
                }
            }
        }

        private static void Update_ItemInfo()
        {
            // "SYN_Aspida_ItemDef"
            GroundVehicleItemDef AspidaItem = (GroundVehicleItemDef)Repo.GetDef("0b5a0322-338e-a324-b87f-40a0ef1d2075");
            AspidaItem.DataDef.Armor = 26f;
            AspidaItem.DataDef.Speed = 24f;
        }

        private static void Give_LeapByDefault()
        {
            JetJumpAbilityDef Aspida_Leap = (JetJumpAbilityDef)Repo.GetDef("d8651e61-ac45-0394-0bd5-f2e165446ae1");
            Aspida_Leap.AbilitiesRequired = new TacticalAbilityDef[] {};
            Aspida_Leap.ActionPointCost = 0.5f;
            Aspida_Leap.UsesPerTurn = 1;

            TacticalActorDef Aspida_ActorDef = (TacticalActorDef)Repo.GetDef("16cd2345-36a9-a6c4-1afa-104e9c72833b");
            Aspida_ActorDef.Abilities = Aspida_ActorDef.Abilities.AddToArray(Aspida_Leap);
        }

        internal static void Update_Requirements(GroundVehicleModuleDef VehicleModule)
        {
            Rover.Items = Rover.Items.AddToArray(VehicleModule);
        }

        internal static void Update_Requirements(GroundVehicleWeaponDef VehicleWeapon)
        {
            Rover.Items = Rover.Items.AddToArray(VehicleWeapon);
        }

        private static ShootAbilityDef AspidaShootAbility()
        {
            ShootAbilityDef AspidaShoot = (ShootAbilityDef)Repo.GetDef("3d138af1-1420-44e8-bfc8-44fff8f4e416");
            if (AspidaShoot == null)
            {
                // "LaserArray_ShootAbilityDef"
                ShootAbilityDef LaserArrayShoot = (ShootAbilityDef)Repo.GetDef("17d71e44-c07c-3e04-e977-1ff7eeb23a43");
                
                AspidaShoot = Repo.CreateDef<ShootAbilityDef>("3d138af1-1420-44e8-bfc8-44fff8f4e416", LaserArrayShoot);
                AspidaShoot.CanShootOnEnemyBodyParts = true;
                AspidaShoot.SnapToBodyparts = false;
                AspidaShoot.UsesPerTurn = -1;
            }
            return AspidaShoot;
        }
    }
}