using Base.Defs;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace TFTVVehicleRework.Armadillo
{
    public static class ArmadilloMain 
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;

        //"NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_1"
        private static readonly ManufactureResearchRewardDef ResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("d197bc9e-9a4d-a2d6-eabf-0bb7b6a0a9a7");
        public static void Change()
        {
            Adjust_Speed();
            Adjust_Capacity();
            Update_ItemStats();
            GasTanks.Change();
            GaussTurret.Change();
            ImprovedTraction.Change();
            LightweightAlloy.Change();
            Mephistopheles.Change();
            Purgatory.Change();
            ReinforcedPlating.Change();
            Give_VehicleEntity();
        }

        private static void Adjust_Speed()
        {
            // "_NJ_Armadillo_Chassis_ItemDef"
            TacticalItemDef Chassis = (TacticalItemDef)Repo.GetDef("43f902a8-fbdd-fb24-0a7b-72c5292591a6");
            foreach (AddonDef.SubaddonBind addon in Chassis.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                if(BodyPart.name.Contains("FrontTyre"))
                {
                    BodyPart.BodyPartAspectDef.Speed = 6f;
                }
                else if(BodyPart.name.Contains("BackTyre"))
                {
                    BodyPart.BodyPartAspectDef.Speed = 6f;
                }
            }
        }

        private static void Adjust_Capacity()
        {
            //"NJ_Armadillo_VehicleComponentDef"
            VehicleComponentDef Armadillo = (VehicleComponentDef)Repo.GetDef("90e70491-599d-59f4-8962-b0ff7823779b");
            Armadillo.PassengerCapacity = 3;
        }

        private static void Update_ItemStats()
        {
            //"NJ_Armadillo_ItemDef"
            GroundVehicleItemDef Armadillo = (GroundVehicleItemDef)Repo.GetDef("60f74fee-278b-8204-e8eb-3721c301292a");
            Armadillo.DataDef.Capacity = 3f;
            Armadillo.DataDef.Speed = 22f;
        }

        private static void Give_VehicleEntity()
        {
            // NJ_Armadillo_ActorDef
            TacticalActorDef ArmadilloActorDef = (TacticalActorDef)Repo.GetDef("fc5539b5-5390-8324-5bf6-9d53a7ec092c"); 
            // MachineEntity_ClassProficiencyAbilityDef
            ClassProficiencyAbilityDef MachineEntityProficiency = (ClassProficiencyAbilityDef)Repo.GetDef("8a02e039-e442-7774-8846-c497e257f25f");
            ArmadilloActorDef.Abilities = ArmadilloActorDef.Abilities.AddToArray(MachineEntityProficiency);
        }

        internal static void Update_Requirements(GroundVehicleModuleDef VehicleModule)
        {
           ResearchReward.Items = ResearchReward.Items.AddToArray(VehicleModule);
        }

        internal static void Update_Requirements(GroundVehicleWeaponDef VehicleWeapon)
        {
           ResearchReward.Items = ResearchReward.Items.AddToArray(VehicleWeapon);
        }

    }
}