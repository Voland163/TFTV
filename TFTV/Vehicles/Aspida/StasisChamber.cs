using Base.Defs;
using Base.UI;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Eventus;
using TFTVVehicleRework.Abilities;
using PRMBetterClasses;

namespace TFTVVehicleRework.Aspida
{   
    public static class StasisChamber
    {
        private static readonly DefRepository Repo = AspidaMain.Repo;
        public static void Change()
        {
            //"SY_Aspida_Improved_Chassis_GroundVehicleModuleDef"
            GroundVehicleModuleDef ImpChassis = (GroundVehicleModuleDef)Repo.GetDef("5343205a-3b48-0674-ba2a-cdbf2ad3c0a4");
            ImpChassis.Abilities = new AbilityDef[]
            {
                AspidaHealAbility(),
            };
            AspidaMain.Update_Requirements(ImpChassis);
        }

        private static ApplyStatusAbilityDef AspidaHealAbility()
        {
            ApplyStatusAbilityDef BloodLustAbility = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b");

            ApplyStatusAbilityDef AspidaHealAbility = (ApplyStatusAbilityDef)Repo.CreateDef("82f16105-7a18-4d44-bb24-062379279ed3", BloodLustAbility);
        
            HealPassengersStatusDef AspidaHealStatus = Repo.CreateDef<HealPassengersStatusDef>("695228f7-9635-4c8f-9fee-56027fdd4dbe");
            Helper.CopyFieldsByReflection(BloodLustAbility.StatusDef, AspidaHealStatus);
            AspidaHealStatus.name = "AspidaHealPassengers_StatusDef";
            AspidaHealStatus.ShowNotification = true;
            AspidaHealStatus.RestoreHP = 80f;
            AspidaHealStatus.RestoreWP = 5f;
            AspidaHealStatus.EffectsToApply = new EffectDef[]
            {
                (StatusRemoverEffectDef)Repo.GetDef("e10ae455-7042-0be4-4899-969be2197c7f"), //"BleedRemover_EffectDef"
                (StatusRemoverEffectDef)Repo.GetDef("216fc588-a16f-f5c4-abe8-044dd50d6589"), //"PoisonRemover_EffectDef"
            } ;
            AspidaHealStatus.EventOnStartTurn = (TacticalEventDef)Repo.GetDef("f59bbc65-0bc3-40c4-9b5a-adf38e6d0b21"); //"Regenerate_TargetEffect_EventDef"

            AspidaHealAbility.StatusDef = AspidaHealStatus;

            //"E_View [TechnicianHeal_AbilityDef]"
            TacticalAbilityViewElementDef TechnicianHealVED = (TacticalAbilityViewElementDef)Repo.GetDef("b63fd1e9-d061-da52-6e14-9c69d4ebe023");
            TacticalAbilityViewElementDef AspidaHealVED = Repo.CreateDef<TacticalAbilityViewElementDef>("90c54554-9986-419d-a23e-8b3aca352d03", TechnicianHealVED);
            AspidaHealVED.name = "E_View [AspidaHealPassengers_StatusDef]";
            AspidaHealVED.DisplayName1 = new LocalizedTextBind("SY_STASIS_NAME");
            AspidaHealVED.Description = new LocalizedTextBind("SY_STASIS_DESC");

            AspidaHealAbility.ViewElementDef = AspidaHealVED;
            AspidaHealStatus.Visuals = AspidaHealVED;

            return AspidaHealAbility;
        }

    }
}