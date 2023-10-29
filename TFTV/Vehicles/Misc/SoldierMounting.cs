using Base.Entities.Abilities;
using HarmonyLib;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;
using System.Linq;
using TFTVVehicleRework.Abilities;
using PRMBetterClasses;
using Base.Defs;
using Base;

namespace TFTVVehicleRework.Misc
{
    public static class SoldierMounting
    {
		private static readonly DefRepository Repo = VehiclesMain.Repo;

        //"EnterVehicle_AbilityDef"
        internal static readonly EnterVehicleAbilityDef EnterVehicleAbilityDef = (EnterVehicleAbilityDef)Repo.GetDef("fdc80b78-dba5-9b14-6846-23fcf8b658b8");

        //"ExitVehicle_AbilityDef"
        internal static readonly ExitVehicleAbilityDef ExitVehicleAbilityDef = (ExitVehicleAbilityDef)Repo.GetDef("acab64f4-c00b-00b4-998a-cf56cde5b541");
        
        public static void Change()
        {
			List<AbilityDef> AbilitiesToRemove = new List<AbilityDef>
			{
				EnterVehicleAbilityDef,
				ExitVehicleAbilityDef,
			};
			List<AbilityDef> AbilitiesToAdd = new List<AbilityDef>
			{
				get_EnterVehicleAbility(),
				get_ExitVehicleAbility(),
			};

			TacticalActorDef Soldier_ActorDef = (TacticalActorDef)Repo.GetDef("958fa60e-4ee3-5d74-b90d-9acd1fab332d"); //"Soldier_ActorDef"

			List<AbilityDef> soldierAbilities = new List<AbilityDef>(Soldier_ActorDef.Abilities);
			soldierAbilities.RemoveRange(AbilitiesToRemove);
			soldierAbilities.AddRange(AbilitiesToAdd);

			Soldier_ActorDef.Abilities = soldierAbilities.ToArray();

		//	Soldier_ActorDef.Abilities.RemoveRange(AbilitiesToRemove);
		//	Soldier_ActorDef.Abilities.AddRange(AbilitiesToAdd);

            TacticalActorDef Civilian_ActorDef = (TacticalActorDef)Repo.GetDef("28d0e424-280d-fa34-c8c4-778b92d26bc5"); //"Civilian_ActorDef"

            List<AbilityDef> civilianAbilities = new List<AbilityDef>(Civilian_ActorDef.Abilities);
            civilianAbilities.RemoveRange(AbilitiesToRemove);
            civilianAbilities.AddRange(AbilitiesToAdd);

            Civilian_ActorDef.Abilities = civilianAbilities.ToArray();


           // Civilian_ActorDef.Abilities.RemoveRange(AbilitiesToRemove);
		//	Civilian_ActorDef.Abilities.AddRange(AbilitiesToAdd);
        }

        public static ExtendedEnterVehicleAbilityDef get_EnterVehicleAbility()
        {
            ExtendedEnterVehicleAbilityDef ExtendedEnterVehicle = (ExtendedEnterVehicleAbilityDef)Repo.GetDef("7d47b409-b7ad-4f0e-8416-4e78114b1450");
			if (ExtendedEnterVehicle == null)
			{
				ExtendedEnterVehicle  = Repo.CreateDef<ExtendedEnterVehicleAbilityDef>("7d47b409-b7ad-4f0e-8416-4e78114b1450");			
				Helper.CopyFieldsByReflection(EnterVehicleAbilityDef, ExtendedEnterVehicle);				
				ExtendedEnterVehicle.name = "ExtendedEnterVehicle_AbilityDef";
				ExtendedEnterVehicle.ActionPointCost = 0.25f;

				ExtendedEnterVehicle.SkillTags = ExtendedEnterVehicle.SkillTags.AddToArray(NewTags.get_EnterVehicleTag());
				ExtendedEnterVehicle.StealthStatus = HidePassenger();
			}
			return ExtendedEnterVehicle;
        }

        public static ExtendedExitVehicleAbilityDef get_ExitVehicleAbility()
        {
            ExtendedExitVehicleAbilityDef ExtendedExitVehicle = (ExtendedExitVehicleAbilityDef)Repo.GetDef("6bd88bea-78c2-4ccc-945b-078233783cdd");
			if(ExtendedExitVehicle == null)
			{
				ExtendedExitVehicle = Repo.CreateDef<ExtendedExitVehicleAbilityDef>("6bd88bea-78c2-4ccc-945b-078233783cdd");
				Helper.CopyFieldsByReflection(ExitVehicleAbilityDef, ExtendedExitVehicle);
				ExtendedExitVehicle.name = "ExtendedExitVehicle_AbilityDef";
				ExtendedExitVehicle.ActionPointCost = 0.5f;

				ExtendedExitVehicle.SkillTags = ExtendedExitVehicle.SkillTags.AddToArray(NewTags.get_ExitVehicleTag());
				ExtendedExitVehicle.StealthStatus = HidePassenger();
			}
			return ExtendedExitVehicle;
        }

		private static StanceStatusDef HidePassenger()
		{

			StanceStatusDef StealthStatus = (StanceStatusDef)Repo.GetDef("ecdacbff-b6b4-456e-aef5-c298e46909fa");
			if (StealthStatus == null)
			{
				//"E_VanishedStatus [Vanish_AbilityDef]"
				StanceStatusDef VanishStatus = (StanceStatusDef)Repo.GetDef("dd27cb97-d80e-3be2-d340-ffd669cad72b");

				StealthStatus = Repo.CreateDef<StanceStatusDef>("ecdacbff-b6b4-456e-aef5-c298e46909fa",VanishStatus);
				StealthStatus.name = "HiddenPassenger_StatusDef";
				StealthStatus.DurationTurns = -1;
				StealthStatus.EventOnApply = null;
				StealthStatus.EventOnUnapply = null;
				StealthStatus.StanceShader = null;
			}
			return StealthStatus;
		}
    }
}