using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.Scarab
{
    public static class ReinforcedStabilisers
    {
        private static readonly DefRepository Repo = ScarabMain.Repo;

        //"Chiron_EnterStabilityStance_AbilityDef"
        internal static SwitchStanceAbilityDef Chiron_EnterStabilityStance = (SwitchStanceAbilityDef)Repo.GetDef("a23b9478-8821-6e14-d876-d9be712baf68");

        //"PX_Scarab_Reinforced_Caterpillar_Tracks_GroundVehicleModuleDef"
        internal static GroundVehicleModuleDef CaterpillarTracks = (GroundVehicleModuleDef)Repo.GetDef("60214c05-e326-e804-0a40-fc874506b313");
        public static void Change()
        {
            CaterpillarTracks.ViewElementDef.DisplayName1 = new LocalizedTextBind("PX_STABILITY_NAME");
            CaterpillarTracks.BodyPartAspectDef.Speed = 0f;
            CaterpillarTracks.ManufactureMaterials = 200f;
            CaterpillarTracks.Abilities = new AbilityDef[]
            {
                EnterStabilityStance(),
                ExitStabilityStance()
            };
            Update_TrampleAbility();
            Update_ManufactureRequirements();
        }

        internal static SwitchStanceAbilityDef EnterStabilityStance() 
        {
            SwitchStanceAbilityDef Scarab_EnterStabilityStance = (SwitchStanceAbilityDef)Repo.GetDef("44e2d750-09d1-4b51-96dc-c98d45451b1d");
            if (Scarab_EnterStabilityStance == null)
            {
                Scarab_EnterStabilityStance = (SwitchStanceAbilityDef)Repo.CreateDef("44e2d750-09d1-4b51-96dc-c98d45451b1d", Chiron_EnterStabilityStance);
                Scarab_EnterStabilityStance.name = "Scarab_EnterStabilityStance_AbilityDef";
                Scarab_EnterStabilityStance.AnimType = -1;

                Scarab_EnterStabilityStance.ViewElementDef = (TacticalAbilityViewElementDef)Repo.CreateDef("5447d0a5-9f49-434d-8165-69b304366249", Chiron_EnterStabilityStance.ViewElementDef);
                Scarab_EnterStabilityStance.ViewElementDef.name = "E_ViewElement [Scarab_EnterStabilityStance_AbilityDef]";
                Scarab_EnterStabilityStance.ViewElementDef.ShowInInventoryItemTooltip = true;

                Scarab_EnterStabilityStance.StabilityStanceIn = null;
                Scarab_EnterStabilityStance.StabilityStanceOut = null;

                Scarab_EnterStabilityStance.stanceStatusDef = StabilityStanceStatus();
            }
            return Scarab_EnterStabilityStance;
        }

        internal static SwitchStanceAbilityDef ExitStabilityStance()
        {
            SwitchStanceAbilityDef Scarab_ExitStabilityStance = (SwitchStanceAbilityDef)Repo.GetDef("370a68dc-5154-46f8-a7f6-2ece41ef4c36");
            if (Scarab_ExitStabilityStance == null)
            {
                SwitchStanceAbilityDef Chiron_ExitStabilityStance = (SwitchStanceAbilityDef)Repo.GetDef("72e3506f-34e5-7d54-8853-003745c7c150"); //"Chiron_ExitStabilityStance_AbilityDef"
                Scarab_ExitStabilityStance = (SwitchStanceAbilityDef)Repo.CreateDef("370a68dc-5154-46f8-a7f6-2ece41ef4c36", Chiron_ExitStabilityStance);

                Scarab_ExitStabilityStance.name = "Scarab_ExitStabilityStance_AbilityDef";
                Scarab_ExitStabilityStance.TargetingDataDef = Chiron_ExitStabilityStance.TargetingDataDef;
                Scarab_ExitStabilityStance.AnimType = -1;

                Scarab_ExitStabilityStance.StabilityStanceIn = null;
                Scarab_ExitStabilityStance.StabilityStanceOut = null;

                Scarab_ExitStabilityStance.ViewElementDef = (TacticalAbilityViewElementDef)Repo.CreateDef("b8734ee9-1136-4d5a-8f6a-3b06ef5f4635", Chiron_ExitStabilityStance.ViewElementDef);
                Scarab_ExitStabilityStance.ViewElementDef.name = "E_ViewElement [Scarab_ExitStabilityStance_AbilityDef]";
                Scarab_ExitStabilityStance.ViewElementDef.ShowInInventoryItemTooltip = true;
                Scarab_ExitStabilityStance.stanceStatusDef = StabilityStanceStatus();
            }
            return Scarab_ExitStabilityStance;
        }

        internal static StanceStatusDef StabilityStanceStatus()
        {
            StanceStatusDef StabilityStanceStatusDef = (StanceStatusDef)Repo.GetDef("323a39a1-cf40-42b8-aa43-e0e521454e95");
            if (StabilityStanceStatusDef == null)
            {
                StabilityStanceStatusDef = (StanceStatusDef)Repo.CreateDef("323a39a1-cf40-42b8-aa43-e0e521454e95", Chiron_EnterStabilityStance.stanceStatusDef);
                StabilityStanceStatusDef.name = "E_Status [Scarab_StabilityStance_AbilityDef]";
                StabilityStanceStatusDef.StanceAnimations = null;
                //Since we clone the Chiron's version that basically grants the same thing, all we need to do is change the number of bonus projectiles:
                StabilityStanceStatusDef.EquipmentsStatModifications[0].EquipmentStatModification.Value = 1;
            }

            return StabilityStanceStatusDef;
        }

        internal static void Update_TrampleAbility()
        {
            //"CaterpillarMoveAbilityDef"
            CaterpillarMoveAbilityDef TrampleAbility = (CaterpillarMoveAbilityDef)Repo.GetDef("943eb31e-aae8-f134-cbd3-8b49a4fc896c");
			TrampleAbility.DisablingStatuses = TrampleAbility.DisablingStatuses.AddToArray(StabilityStanceStatus());
        }

        internal static void Update_ManufactureRequirements()
        {
            //"PX_HelCannon_ResearchDef_ManufactureResearchRewardDef_0"
            ManufactureResearchRewardDef HelCannonReward = (ManufactureResearchRewardDef)Repo.GetDef("80d2b708-8ee3-228b-278e-acfa09815d64");
            ManufactureResearchRewardDef SwarmerRewardDef = (ManufactureResearchRewardDef)Repo.CreateDef("d6d85211-62ab-45fe-86db-bf94e5477ad1", HelCannonReward);
            SwarmerRewardDef.name = "PX_Alien_Swarmer_ResearchDef_ManufactureResearchRewardDef";
            SwarmerRewardDef.Items = new ItemDef[]
            {
                CaterpillarTracks
            };

            ResearchDef SwarmerResearch = (ResearchDef)Repo.GetDef("8e495706-5872-b7ea-296e-37f498573cf1"); //"PX_Alien_Swarmer_ResearchDef"
            SwarmerResearch.Unlocks = new ResearchRewardDef[]
            {
                SwarmerRewardDef
            };
        }
    }
}