using Base.Defs;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities;

namespace TFTVVehicleRework.Armadillo
{
    public static class ArmadilloMain 
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;

        //"NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_1"
        private static readonly ManufactureResearchRewardDef ResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("d197bc9e-9a4d-a2d6-eabf-0bb7b6a0a9a7");
        public static void Change()
        {
            GasTanks.Change();
            GaussTurret.Change();
            ImprovedTraction.Change();
            LightweightAlloy.Change();
            Mephistopheles.Change();
            Purgatory.Change();
            ReinforcedPlating.Change();
            Give_VehicleEntity();
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