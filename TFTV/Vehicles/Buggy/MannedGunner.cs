using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.UI;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PRMBetterClasses;
using System.Collections.Generic;
using TFTVVehicleRework.Abilities;
using TFTVVehicleRework.Misc;

namespace TFTVVehicleRework.KaosBuggy
{
    public static class MannedGunner
    {
        private static readonly DefRepository Repo = KaosBuggyMain.Repo;
        public static void Change()
        {
            //"KS_Kaos_Buggy_Revised_Armor_Plating_Hull_GroundVehicleModuleDef"
            GroundVehicleModuleDef RevisedPlating = (GroundVehicleModuleDef)Repo.GetDef("342b97d0-ac19-67a4-2a48-118b5a12980b");
            RevisedPlating.Armor = 0f;
            RevisedPlating.ViewElementDef.DisplayName1 = new LocalizedTextBind("KB_GUNNERMODULE_NAME");
            RevisedPlating.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_HULL");
            RevisedPlating.Abilities = new TacticalAbilityDef[]
            {
                MannedGunnerStatus(),
                BuggyExitCost(),
            };

            foreach (AddonDef.SubaddonBind addon in RevisedPlating.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                BodyPart.HitPoints = 150f;
                BodyPart.Armor = 20f;
                BodyPart.BodyPartAspectDef.Speed = 0f;
            }
            Adjust_Cost();
        }

        public static ApplyStatusAbilityDef BuggyExitCost()
        {
            ApplyStatusAbilityDef BuggyExit = (ApplyStatusAbilityDef)Repo.GetDef("232f5c03-2642-4b5d-bb82-08bfad4536d3");
			if (BuggyExit == null)
			{
				ApplyStatusAbilityDef Bloodlust = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b"); //"BloodLust_AbilityDef"

				//Make new ApplyStatusAbilityDef from the ability:
				BuggyExit = (ApplyStatusAbilityDef)Repo.CreateDef("232f5c03-2642-4b5d-bb82-08bfad4536d3", Bloodlust);
				BuggyExit.name = "BuggyExit_AbilityDef";

				//Make new StatusDef
				AdjustAccessCostStatusDef BuggyExitStatus = Repo.CreateDef<AdjustAccessCostStatusDef>("b6d947a1-0a1a-4ddf-9294-88c5eefb0e04");
				Helper.CopyFieldsByReflection(Bloodlust.StatusDef, BuggyExitStatus);		
				

				BuggyExitStatus.AccessCostModification.TargetAbilityTagDef = NewTags.get_ExitVehicleTag();
				BuggyExitStatus.AccessCostModification.SkillTagCullFilter = new SkillTagDef[0];
				BuggyExitStatus.AccessCostModification.EquipmentTagDef = null;
				BuggyExitStatus.AccessCostModification.AbilityCullFilter = new List<TacticalAbilityDef>{};
				BuggyExitStatus.AccessCostModification.RequiresProficientEquipment = false;
				BuggyExitStatus.AccessCostModification.ActionPointModType = TacticalAbilityModificationType.Set;
				BuggyExitStatus.AccessCostModification.ActionPointMod = 0.75f;
				BuggyExitStatus.name = "BuggyExit_StatusDef";
				BuggyExitStatus.AccessDirection = AdjustAccessCostStatusDef.Direction.Exit;

				BuggyExit.StatusDef = BuggyExitStatus;

                BuggyExit.ViewElementDef = Exit_VED();

			}
			return BuggyExit;
        }

        private static TacticalAbilityViewElementDef Exit_VED()
        {
            //"E_View [ExitVehicle_AbilityDef]"
            TacticalAbilityViewElementDef ExitVehicleVED = (TacticalAbilityViewElementDef)Repo.GetDef("bc2c18fc-feae-012c-91f6-484668db2d40");
            TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("6fd30edb-d469-4f84-988b-8a1c4d78c24a", ExitVehicleVED);
            VED.name = "E_View [BuggyExit_AbilityDef]";
            VED.DisplayName1 = new LocalizedTextBind("KB_EXITGUNNER_NAME");
            VED.Description = new LocalizedTextBind("KB_EXITGUNNER_DESC");
            VED.ShowInInventoryItemTooltip = true;
            return VED;
        }

        public static ApplyStatusAfterAbilityExecutedAbilityDef MannedGunnerStatus()
        {
            //"AttackAbility_SkillTagDef" -> Influencing tag
            SkillTagDef AttackAbilitySkillTagDef = (SkillTagDef)Repo.GetDef("b29f3c31-b7e0-3214-6bdc-9d692d527f2c");
            //"HasPassengers_VehicleStatusTagDef" -> Required Tag
			VehicleStatusTagDef HasPassengersStatusTag = (VehicleStatusTagDef)Repo.GetDef("49ba189e-950a-1cd4-d93e-6e64f8b23860");

            //"FastUse_AbilityDef" -> Status always applied
            ApplyStatusAbilityDef FastUse = (ApplyStatusAbilityDef)Repo.GetDef("3f8b32f5-6084-f544-aba0-c98af7db93c3");

            ApplyStatusAfterAbilityExecutedAbilityDef KBStatusCarrier = Repo.CreateDef<ApplyStatusAfterAbilityExecutedAbilityDef>("487f04b1-d3fc-402b-89ce-7da237d5444f"); 
            Helper.CopyFieldsByReflection(FastUse,KBStatusCarrier);
            KBStatusCarrier.name = "KaosBuggy_QuickAttack_AbilityDef";
            KBStatusCarrier.CharacterProgressionData = null;

            //"E_MedkitAbilitiesCostChange [FastUse_AbilityDef]" -> Effect always applied
            ChangeAbilitiesCostStatusDef FastUseStatus = (ChangeAbilitiesCostStatusDef)Repo.GetDef("c4ce3155-c9fa-f4d1-0ba3-8c71897206c2");

            ChangeAbilitiesCostStatusDef BuggyQuickAttackStatus = Repo.CreateDef<ChangeAbilitiesCostStatusDef>("187e3e40-a53e-4c1d-af21-34856ecf7659", FastUseStatus);
            BuggyQuickAttackStatus.name = "KaosBuggy_QuickAttack_StatusDef";
            BuggyQuickAttackStatus.AbilityCostModification.TargetAbilityTagDef = AttackAbilitySkillTagDef;
            BuggyQuickAttackStatus.AbilityCostModification.EquipmentTagDef = null;
            
            TacticalAbilityViewElementDef BuggyQuickAttackViewElementDef = (TacticalAbilityViewElementDef)Repo.CreateDef("bd9161af-4cf4-4a7b-8e23-b37505f5611a", FastUse.ViewElementDef);
            BuggyQuickAttackViewElementDef.DisplayName1 = new LocalizedTextBind("KB_MANNEDGUNNER_NAME");
            BuggyQuickAttackViewElementDef.Description = new LocalizedTextBind("KB_MANNEDGUNNER_DESC");
            BuggyQuickAttackViewElementDef.ShowInInventoryItemTooltip = true;
            //"E_ViewElement [QuickAim_AbilityDef]"
            TacticalAbilityViewElementDef QuickAim_VED = (TacticalAbilityViewElementDef)Repo.GetDef("0bf95ee1-4d33-3743-4e84-f15dbb937544");
            BuggyQuickAttackViewElementDef.SmallIcon = BuggyQuickAttackViewElementDef.LargeIcon = QuickAim_VED.SmallIcon;

            ActorHasTagEffectConditionDef BuggyQuickAttackCondition = Repo.CreateDef<ActorHasTagEffectConditionDef>("6b944fdd-96e2-4680-94c5-fab4e0290a82");
            BuggyQuickAttackCondition.name = "HasPassengersTag_ApplicationCondition";
            BuggyQuickAttackCondition.ResourcePath = "Defs/Tactical/Actors/_Common/Effects_Statuses/ApplicationConditions/HasPassengersTag_ApplicationCondition";
            BuggyQuickAttackCondition.GameTag = HasPassengersStatusTag;
            BuggyQuickAttackCondition.HasTag = true;

            BuggyQuickAttackStatus.ApplicationConditions = new EffectConditionDef[]
            {
                BuggyQuickAttackCondition
            };
            
            KBStatusCarrier.ViewElementDef = BuggyQuickAttackViewElementDef;
            KBStatusCarrier.StatusToApply = BuggyQuickAttackStatus;

            return KBStatusCarrier;
        }

        private static void Adjust_Cost()
        {
            //"RevisedArmorPlating_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef MarketOption = (GeoMarketplaceItemOptionDef)Repo.GetDef("d1a7a199-6cbc-0534-592c-66076d905d37");
            MarketOption.MinPrice = 350f;
            MarketOption.MaxPrice = 550f;
        }
    }
}