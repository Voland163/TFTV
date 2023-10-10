using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PRMBetterClasses;
using System.Collections.Generic;
using TFTVVehicleRework.Abilities;
using TFTVVehicleRework.Misc;

namespace TFTVVehicleRework.Scarab
{
    public static class DeploymentBay
    {
        private static readonly DefRepository Repo = ScarabMain.Repo;
        public static void Change()
        {
            //"PX_Scarab_Advanced_Engine_Mapping_Module_Engine_GroundVehicleModuleDef"
            GroundVehicleModuleDef Engine = (GroundVehicleModuleDef)Repo.GetDef("9ff261a4-a88c-22a4-0a74-bb48ce6a890a");
            Engine.ViewElementDef.DisplayName1 = new LocalizedTextBind("PX_DEPLOY_NAME");
            Engine.BodyPartAspectDef.Speed = 0f;

            Engine.Abilities = Engine.Abilities.AddToArray(ScarabExit());
            Engine.ViewElementDef.DisplayName1 = new LocalizedTextBind("PX_DEPLOY_NAME");
        }

        public static ApplyStatusAbilityDef ScarabExit()
        {
            ApplyStatusAbilityDef ScarabExit = (ApplyStatusAbilityDef)Repo.GetDef("76d89195-76e0-4493-ba3b-9b826825c5ed");
            if (ScarabExit == null)
            {
                ApplyStatusAbilityDef QuickAim = (ApplyStatusAbilityDef)Repo.GetDef("2d74a8e7-4284-36d4-4872-7c7b0c9ce245");

                // Make clone of Quickaim
                ScarabExit = (ApplyStatusAbilityDef)Repo.CreateDef("76d89195-76e0-4493-ba3b-9b826825c5ed", QuickAim);
                ScarabExit.name = "ScarabExit_AbilityDef";
                ScarabExit.ActionPointCost = 0.5f;
                ScarabExit.WillPointCost = 0f;
                ScarabExit.AnimType = -1; //Stops game from freezing

                TacticalAbilityViewElementDef ScarabExitViewElement = (TacticalAbilityViewElementDef)Repo.CreateDef("9693e26a-1ee4-4db3-8cbf-60c97470268a", QuickAim.ViewElementDef);
                ScarabExitViewElement.DisplayName1 = new LocalizedTextBind("PX_EJECT_NAME");
                ScarabExitViewElement.Description = new LocalizedTextBind("PX_EJECT_DESC");
                ScarabExitViewElement.ShowInInventoryItemTooltip = true;
                ScarabExitViewElement.ShowInStatusScreen = true;

                TacticalAbilityViewElementDef ExitVehicleViewElement = (TacticalAbilityViewElementDef)Repo.GetDef("bc2c18fc-feae-012c-91f6-484668db2d40");
                ScarabExitViewElement.SmallIcon = ScarabExitViewElement.LargeIcon = ExitVehicleViewElement.SmallIcon;

                AdjustAccessCostStatusDef ScarabExitStatus = Repo.CreateDef<AdjustAccessCostStatusDef>("3a756587-9013-44c6-87f2-c533f004dc6e");
                Helper.CopyFieldsByReflection(QuickAim.StatusDef, ScarabExitStatus);
                ScarabExitStatus.EventOnApply = null;
                ScarabExitStatus.name = "ScarabExit_StatusDef";
                ScarabExitStatus.ShowNotification = true;
                ScarabExitStatus.Visuals = ScarabExitViewElement;

                ScarabExitStatus.AccessCostModification.TargetAbilityTagDef = NewTags.get_ExitVehicleTag();
                ScarabExitStatus.AccessCostModification.SkillTagCullFilter = new SkillTagDef[0];
                ScarabExitStatus.AccessCostModification.EquipmentTagDef = null;
                ScarabExitStatus.AccessCostModification.AbilityCullFilter = new List<TacticalAbilityDef>{};
                ScarabExitStatus.AccessCostModification.RequiresProficientEquipment = false;
                ScarabExitStatus.AccessCostModification.ActionPointModType = TacticalAbilityModificationType.Set;
                ScarabExitStatus.AccessCostModification.ActionPointMod = 0f;
                ScarabExitStatus.AccessDirection = AdjustAccessCostStatusDef.Direction.Exit;

                ScarabExit.ViewElementDef = ScarabExitViewElement;
                ScarabExit.StatusDef = ScarabExitStatus;
                ScarabExit.DisablingStatuses = new StatusDef[]
                {
                    ScarabExitStatus
                };
            }
            return ScarabExit;
        }
    }
}