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
            TacticalActorBaseDef Aspida = (TacticalActorBaseDef)Repo.GetDef("16cd2345-36a9-a6c4-1afa-104e9c72833b");
            TacticalActorBaseDef Armadillo = (TacticalActorBaseDef)Repo.GetDef("aaac8f86-772c-cf44-e8cd-0f07a8b6bf83");
            TacticalActorBaseDef Buggy = (TacticalActorBaseDef)Repo.GetDef("fc5539b5-5390-8324-5bf6-9d53a7ec092c");
            TacticalActorBaseDef Scarab = (TacticalActorBaseDef)Repo.GetDef("42a5c739-fa43-cf94-ea55-7c5d2e09527f");

            //"CaterpillarMoveAbilityDef"
            CaterpillarMoveAbilityDef TrampleAbility = (CaterpillarMoveAbilityDef)Repo.GetDef("943eb31e-aae8-f134-cbd3-8b49a4fc896c");
            TrampleAbility.ViewElementDef.DisplayName1 = new LocalizedTextBind("UI_TRAMPLE_NAME");
            TrampleAbility.ViewElementDef.Description = new LocalizedTextBind("UI_TRAMPLE_DESC");

            Aspida.Abilities = Aspida.Abilities.AddToArray(TrampleAbility);
            Armadillo.Abilities = Armadillo.Abilities.AddToArray(TrampleAbility);
            Buggy.Abilities = Buggy.Abilities.AddToArray(TrampleAbility);
            Scarab.Abilities = Scarab.Abilities.AddToArray(TrampleAbility);

			// "NJ_Armadillo_DemolitionComponentDef" -> Adjusting size of Armadillo Demolition Model so that it can crush worms;
            TacticalDemolitionComponentDef ArmadilloDemoComponentDef = (TacticalDemolitionComponentDef)Repo.GetDef("ca58e419-1c40-fba4-2922-b03e787f96c9");
            ArmadilloDemoComponentDef.RectangleSize = new Vector3
            {
                x = 2.5f,
                y = 2.6f,
                z = 2.9f,
            };
        }
    }
}