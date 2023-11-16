using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using UnityEngine;

namespace TFTVVehicleRework.Misc
{
    public static class MiscChanges
    {
        private static readonly DefRepository Repo = VehiclesMain.Repo;
        public static void Apply() 
        {
            Change_Goo();
            Change_VehicleInventory();
            Give_VehiclesTrample();
            Update_LaunchMissileInfo();
            MarketplaceOptions.Remove_Options();
            RescueMissions.GenerateMissions();
            RescueMissions.Fix_BuggyDeploymentTemplate();
            SoldierMounting.Change();
            VehicleStatusTags.GiveTags();
        }

        // Gooed status now disables Caterpillar Tracks NavArea
        private static void Change_Goo()
        {
            GooedStatusDef Gooed = (GooedStatusDef)Repo.GetDef("9028b4fb-f1fe-f994-481f-9b983d15bf3b"); // "Gooed_StatusDef"
            Gooed.DisabledNavAreas = Gooed.DisabledNavAreas.AddToArray("WalkableArmadilloWorms");
        }
        private static void Change_VehicleInventory()
        {
            BackpackFilterDef VehicleInventory = (BackpackFilterDef)Repo.GetDef("89447e35-11a5-1154-bbfb-0ccced5df7f1"); // "VehicleBackpackFilterDef"
			VehicleInventory.MaxItems = 12;
        }

        private static void Update_LaunchMissileInfo()
        {
            // "E_ViewElement [LaunchMissiles_ShootAbilityDef]"
            TacticalAbilityViewElementDef LaunchMissilesVED = (TacticalAbilityViewElementDef)Repo.GetDef("72e07065-825f-b011-db35-30e4ec7e5a31");
            LaunchMissilesVED.Description = new LocalizedTextBind("UI_LAUNCHMISSILES_DESC");
        }

        // All Vehicles now have the Caterpillar Tracks Utility
        private static void Give_VehiclesTrample()
        {
            // get ActorDefs
            TacticalActorBaseDef[] VehicleActorDefs = new TacticalActorBaseDef[]
            {
                (TacticalActorBaseDef)Repo.GetDef("aaac8f86-772c-cf44-e8cd-0f07a8b6bf83"), // Armadillo
                (TacticalActorBaseDef)Repo.GetDef("16cd2345-36a9-a6c4-1afa-104e9c72833b"), // Aspida
                (TacticalActorBaseDef)Repo.GetDef("fc5539b5-5390-8324-5bf6-9d53a7ec092c"), // Buggy
                (TacticalActorBaseDef)Repo.GetDef("42a5c739-fa43-cf94-ea55-7c5d2e09527f")  // Scarab
            };

            //"CaterpillarMoveAbilityDef"
            CaterpillarMoveAbilityDef TrampleAbility = (CaterpillarMoveAbilityDef)Repo.GetDef("943eb31e-aae8-f134-cbd3-8b49a4fc896c");

            //"Mutog_CanLeap_AbilityDef"
            PassiveModifierAbilityDef MutogCanLeap = (PassiveModifierAbilityDef)Repo.GetDef("af29cfc4-9b22-b774-682f-1dabc6ac13d3");
            PassiveModifierAbilityDef DummyTrample = Repo.CreateDef<PassiveModifierAbilityDef>("a72fbfc4-17fe-4db1-aaf9-cec0eb350215", MutogCanLeap);
            DummyTrample.name = "Trample_DummyAbilityDef";
            DummyTrample.ViewElementDef = TrampleAbility.ViewElementDef;
            DummyTrample.ViewElementDef.DisplayName1 = new LocalizedTextBind("UI_TRAMPLE_NAME");
            DummyTrample.ViewElementDef.Description = new LocalizedTextBind("UI_TRAMPLE_DESC");

            //Change Trample's ViewElementDef to the same as regular moving abilities.
            TrampleAbility.ViewElementDef = (TacticalAbilityViewElementDef)Repo.GetDef("6333fa2e-6e95-8124-48ea-8f7a60a2e22c"); //"Move_AbilityViewDef"

            foreach(TacticalActorBaseDef ActorDef in VehicleActorDefs)
            {
                ActorDef.Abilities = ActorDef.Abilities.AddToArray(TrampleAbility);
                ActorDef.Abilities = ActorDef.Abilities.AddToArray(DummyTrample);
            }

			// "NJ_Armadillo_DemolitionComponentDef" -> Adjusting size of Armadillo Demolition Model so that it can crush worms;
            TacticalDemolitionComponentDef ArmadilloDemoComponentDef = (TacticalDemolitionComponentDef)Repo.GetDef("ca58e419-1c40-fba4-2922-b03e787f96c9");
            ArmadilloDemoComponentDef.RectangleSize = new Vector3
            {
                x = 2.5f,
                y = 2.6f,
                z = 2.9f,
            };

            //"SY_Aspida_DemolitionComponentDef"
            TacticalDemolitionComponentDef AspidaDemoComponentDef = (TacticalDemolitionComponentDef)Repo.GetDef("19e54d43-7eb6-ebf4-da14-abe67676b845");
            AspidaDemoComponentDef.CapsuleRadius = 0.95f;
        }
    }
}