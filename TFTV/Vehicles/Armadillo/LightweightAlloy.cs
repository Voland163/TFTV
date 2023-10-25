using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;
using System.Collections.Generic;
using TFTVVehicleRework.Abilities;
using TFTVVehicleRework.Misc;

namespace TFTVVehicleRework.Armadillo
{
    public static class LightweightAlloy
    {
		private static readonly DefRepository Repo = ArmadilloMain.Repo;
        public static void Change()
        {
            GroundVehicleModuleDef LightweightAlloy = (GroundVehicleModuleDef)Repo.GetDef("32c107bb-d282-d2c4-fbae-9830a46a2e14");
            LightweightAlloy.BodyPartAspectDef.StatModifications = new ItemStatModification[]{};
            LightweightAlloy.Abilities = LightweightAlloy.Abilities.AddToArray(EnterVehicle());
        }

        private static ApplyStatusAbilityDef EnterVehicle()
        {
            ApplyStatusAbilityDef ArmadilloEntry =  (ApplyStatusAbilityDef)Repo.GetDef("a36eb5bd-4ec5-4d97-bd61-1a6d22e863b9");
			if (ArmadilloEntry == null)
			{
				//Take always active Ability and copy its values to ensure new status is always on.
				ApplyStatusAbilityDef Bloodlust = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b"); //"BloodLust_AbilityDef"

				//Make new ApplyStatusAbilityDef from the ability:
				ArmadilloEntry = (ApplyStatusAbilityDef)Repo.CreateDef("a36eb5bd-4ec5-4d97-bd61-1a6d22e863b9", Bloodlust);
				ArmadilloEntry.name = "ArmadilloEntry_AbilityDef";

				//Make new StatusDef
				AdjustAccessCostStatusDef ArmadilloEntryStatus = Repo.CreateDef<AdjustAccessCostStatusDef>("c6a38e70-52f5-4fba-938a-6941f4cf2c23");
				Helper.CopyFieldsByReflection(Bloodlust.StatusDef, ArmadilloEntryStatus);		
				
				ArmadilloEntryStatus.AccessCostModification.TargetAbilityTagDef = NewTags.get_EnterVehicleTag();
				ArmadilloEntryStatus.AccessCostModification.SkillTagCullFilter = new SkillTagDef[0];
				ArmadilloEntryStatus.AccessCostModification.EquipmentTagDef = null;
				ArmadilloEntryStatus.AccessCostModification.AbilityCullFilter = new List<TacticalAbilityDef>{};
				ArmadilloEntryStatus.AccessCostModification.RequiresProficientEquipment = false;
				ArmadilloEntryStatus.AccessCostModification.ActionPointModType = TacticalAbilityModificationType.Set;
				ArmadilloEntryStatus.AccessCostModification.ActionPointMod = 0f;
				ArmadilloEntryStatus.name = "E_Status [ArmadilloEntry_AbilityDef]";
				ArmadilloEntryStatus.AccessDirection = AdjustAccessCostStatusDef.Direction.Entry;

				ArmadilloEntry.StatusDef = ArmadilloEntryStatus;
				ArmadilloEntry.ViewElementDef = EntryVED();
			}
			return ArmadilloEntry;
        }

		private static TacticalAbilityViewElementDef EntryVED()
		{
			TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("c4977dcb-fbe8-4d1e-b205-da69ce0761cc");
			if (VED == null)
			{
				TacticalAbilityViewElementDef EnterVehicleViewElement = (TacticalAbilityViewElementDef)Repo.GetDef("1f9088c2-4873-9a83-d2c5-7b1e2e25cfb9");
				VED = Repo.CreateDef<TacticalAbilityViewElementDef>("c4977dcb-fbe8-4d1e-b205-da69ce0761cc", EnterVehicleViewElement);
				VED.name = "E_ViewElement [ArmadilloEntry_AbilityDef]";
				VED.Name = "ArmadilloEntry_AbilityDef";
				VED.DisplayName1 = new LocalizedTextBind("NJ_LIGHTWEIGHT_NAME");
				VED.Description = new LocalizedTextBind("NJ_LIGHTWEIGHT_DESC");
				VED.ShowInInventoryItemTooltip = true;
			}
			return VED;
		}
    }
}