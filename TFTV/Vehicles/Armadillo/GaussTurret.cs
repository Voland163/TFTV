using Base.Defs;
using Base.UI;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using TFTVVehicleRework.Abilities;
using PRMBetterClasses;

namespace TFTVVehicleRework.Armadillo
{
    public static class GaussTurret
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;
        public static void Change()
        {
            // "NJ_Armadillo_Gauss_Turret_GroundVehicleWeaponDef"
            GroundVehicleWeaponDef GaussTurret = (GroundVehicleWeaponDef)Repo.GetDef("510bf01c-22d0-4b04-597b-c4eaff0b0de9");
            GaussTurret.ChargesMax = 32;
            GaussTurret.SpreadDegrees = 40.99f/19f;
            GaussTurret.HitPoints = 360f;
            // GaussTurret.BodyPartAspectDef.Perception = 4;

            //"Reload_AbilityDef"
            ReloadAbilityDef Reload = (ReloadAbilityDef)Repo.GetDef("3d6a71c7-c27b-5374-5a51-0ba31db93d41");
            
            //"ReturnFire_AbilityDef"
            ReturnFireAbilityDef ReturnFire = (ReturnFireAbilityDef)Repo.GetDef("a87efee7-0b84-f9d4-3bcb-56b3e479714c");
            ReturnFire.ActorTags = new GameTagDef[0]; // remove ReturnFire_SkillTag requirement for RF to work
            ReturnFire.ViewElementDef.ShowInInventoryItemTooltip = true; //Shows ability in the Geo UI

            FreeReloadAbilityDef ArmadilloReload = Repo.CreateDef<FreeReloadAbilityDef>("5f16a5b4-e6c5-49ca-95e3-4ba345bbd31d");
            Helper.CopyFieldsByReflection(Reload, ArmadilloReload);
            ArmadilloReload.name = "FreeReload_AbilityDef";
            ArmadilloReload.ResourcePath = "Defs/Tactical/Actors/_Common/Abilities/FreeReload_AbilityDef";
            ArmadilloReload.InputAction = "";
            ArmadilloReload.ActionPointCost = 0.5f;

            ArmadilloReload.ViewElementDef = (TacticalAbilityViewElementDef)Repo.CreateDef("f66de850-07d9-4339-9ee8-9f5839fd66c7", Reload.ViewElementDef);
            ArmadilloReload.ViewElementDef.name = "E_View [FreeReload_AbilityDef]";
            ArmadilloReload.ViewElementDef.Description = new LocalizedTextBind("UI_RELOAD_DESC");
            ArmadilloReload.ViewElementDef.ShowInInventoryItemTooltip = true;

            GaussTurret.Abilities = GaussTurret.Abilities.AddToArray(ArmadilloReload);
            GaussTurret.Abilities = GaussTurret.Abilities.AddToArray(ReturnFire);
        }
    }
}