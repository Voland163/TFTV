using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Cameras.ExecutionNodes;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Cameras.Filters;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.KaosBuggy
{
    public static class Deathproof
   {
        private static readonly DefRepository Repo = KaosBuggyMain.Repo;

        //"Inspire_AbilityDef"
        internal static readonly ApplyStatusAbilityDef Inspire = (ApplyStatusAbilityDef)Repo.GetDef("faa8a477-3825-9304-6882-bd678aa4ce33");
        public static void Change()
        {
            //"KS_Kaos_Buggy_Experimental_Exhaust_System_Engine_GroundVehicleModuleDef"
            GroundVehicleModuleDef Exhaust = (GroundVehicleModuleDef)Repo.GetDef("2ed4297c-ccaa-0aa4-ab78-b56e53f9b074");
            Exhaust.ManufactureMaterials = 250f;
            Exhaust.ViewElementDef.DisplayName1 = new LocalizedTextBind("KB_DEATHPROOF_NAME");
            Exhaust.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_ENGINE");
            Exhaust.BodyPartAspectDef.Speed = -4f;
            Exhaust.Abilities = new AbilityDef[]
            {
                KillNRun(),
            };
            Update_ChaserCam();
            Adjust_Cost();
        }

        private static ApplyStatusAbilityDef KillNRun()
        {   
            ApplyStatusAbilityDef KNR = Repo.CreateDef<ApplyStatusAbilityDef>("f0dee62e-c24a-4c22-af5d-5896eddb2c3e", Inspire);
            KNR.name = "KillNRun_AbilityDef";
            KNR.ViewElementDef = KNR_VED();
            KNR.SkillTags = new SkillTagDef[]{};
            KNR.StatusDef = KNR_MultiStatus();
            KNR.StatusApplicationTrigger = StatusApplicationTrigger.StartTurn;   
            return KNR;
        }

        private static MultiStatusDef KNR_MultiStatus()
        {
            MultiStatusDef MultiStatus = Repo.CreateDef<MultiStatusDef>("31cabb79-6f8f-4274-88d1-10da300114e7");
            MultiStatus.EffectName = "E_MultiStatus [KillNRun_AbilityDef]";
            MultiStatus.Duration = float.PositiveInfinity;
            MultiStatus.Statuses = new StatusDef[]
            {
                KNR_Status(),
                KNR_OnActorDeathStatus(),
            };
            return MultiStatus;
        }

        private static OnActorDeathEffectStatusDef KNR_OnActorDeathStatus()
        {
            OnActorDeathEffectStatusDef DeathEffectStatus = (OnActorDeathEffectStatusDef)Repo.GetDef("ce2cac52-6416-47a0-ad9c-1d5a4f3a04f9");
            if (DeathEffectStatus == null)
            {
                DeathEffectStatus = Repo.CreateDef<OnActorDeathEffectStatusDef>("ce2cac52-6416-47a0-ad9c-1d5a4f3a04f9", Inspire.StatusDef as OnActorDeathEffectStatusDef);
                DeathEffectStatus.name = "E_DeathEffectStatus [KillNRun_AbilityDef]";
                DeathEffectStatus.EffectName = "KNR_TriggerListener";
                DeathEffectStatus.Visuals = KNR_VED();
                DeathEffectStatus.VisibleOnPassiveBar = true;
                DeathEffectStatus.DurationTurns = 0;
                DeathEffectStatus.EffectDef = Remove_TriggerListener();
            }
            return DeathEffectStatus;
        }


        private static AddAbilityStatusDef KNR_Status()
        {
            //"E_AddAbilityStatus [DeployBeacon_StatusDef]"
            AddAbilityStatusDef DeployBeacon = (AddAbilityStatusDef)Repo.GetDef("3f10fc10-23e8-45b1-2b01-51882ca7ac6f");
            AddAbilityStatusDef AddRunStatus = Repo.CreateDef<AddAbilityStatusDef>("5ed3339b-4200-4c4c-b574-e3126feacfdf", DeployBeacon);
            AddRunStatus.name = "E_AddAbilityStatus [KillNRun_AbilityDef]";
            AddRunStatus.DurationTurns = 0;
            AddRunStatus.SingleInstance = true;
            AddRunStatus.AbilityDef = Run_Ability();
            return AddRunStatus;
        }

        private static StatusRemoverEffectDef Remove_TriggerListener()
        {
            StatusRemoverEffectDef StatusRemover = Repo.CreateDef<StatusRemoverEffectDef>("2431fdbf-0f85-4456-b8c8-cc911ffbd9ac");
            StatusRemover.name = "E_StatusRemoverEffect [KillNRun_AbilityDef]";
            StatusRemover.ApplicationConditions = new EffectConditionDef[]{};
            StatusRemover.StatusToRemove = "KNR_TriggerListener";
            return StatusRemover;
        }

        private static TacticalAbilityViewElementDef KNR_VED()
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("96bc74df-12e0-42cb-a978-6ef5ae3d7462");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("96bc74df-12e0-42cb-a978-6ef5ae3d7462", Inspire.ViewElementDef);
                VED.DisplayName1 = new LocalizedTextBind("KB_KNR_NAME");
                VED.Description = new LocalizedTextBind("KB_KNR_DESC");
                VED.ShowInInventoryItemTooltip = true;
                VED.ShowInStatusScreen = true;
                VED.HideFromPassives = true;
            }
            return VED;
        }

        private static RepositionAbilityDef Run_Ability()
        {
            RepositionAbilityDef Run = (RepositionAbilityDef)Repo.GetDef("5bead478-305f-4c47-95dc-a718fb5900c4");
            if (Run == null)
            {
                //"Dash_AbilityDef"
                RepositionAbilityDef Dash = (RepositionAbilityDef)Repo.GetDef("1834ca7e-c667-8364-398f-3c0376f5f960");         

                Run = Repo.CreateDef<RepositionAbilityDef>("5bead478-305f-4c47-95dc-a718fb5900c4", Dash);
                Run.name = "E_RunAbility [KillNRun_AbilityDef]";
                Run.SuppressAutoStandBy = true;
                Run.DisablingStatuses = new StatusDef[]
                {
                    KNR_OnActorDeathStatus()
                };
                Run.UsesPerTurn = 1;
                Run.ActionPointCost = 0f;
                Run.WillPointCost = 0f;
                Run.SamePositionIsValidTarget = true;
                Run.AmountOfMovementToUseAsRange = -1f;

                Run.ViewElementDef = Repo.CreateDef<TacticalAbilityViewElementDef>("71fce055-1664-47dd-a996-84cd47bfc7ca", KNR_VED());
                Run.ViewElementDef.name = "E_ViewElementDef [E_RunAbility]";
                Run.ViewElementDef.ShowInStatusScreen = false;
                Run.ViewElementDef.ShouldFlash = true;

                Run.TargetingDataDef = Repo.CreateDef<TacticalTargetingDataDef>("fbc63b02-b59b-4bf4-92c5-30877e48bd59", Dash.TargetingDataDef);
                Run.TargetingDataDef.name = "E_TargetingData [E_RunAbility]";
                Run.TargetingDataDef.Origin.Range = 14f;
            }
            return Run;
        }

        private static void Update_ChaserCam()
        {
            //"E_DashCameraAbility [NoDieCamerasTacticalCameraDirectorDef]"
            FirstMatchExecutionDef DashCam = (FirstMatchExecutionDef)Repo.GetDef("9df769e7-0b75-2bb3-ea14-74aba0b502dd");
            FirstMatchExecutionDef NewDashCam = Repo.CreateDef<FirstMatchExecutionDef>("6550edb4-9db4-4c67-af25-b426e241bbfd", DashCam);

            //"E_DashAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]"
            TacCameraAbilityFilterDef DashFilter = (TacCameraAbilityFilterDef)Repo.GetDef("2131d78d-5b80-2bb1-80aa-b21755e500dd");
            TacCameraAbilityFilterDef NewDashFilter = Repo.CreateDef<TacCameraAbilityFilterDef>("f6c3a9af-b712-429e-8232-26a0912cd1e6", DashFilter);

            NewDashCam.FilterDef = NewDashFilter;
            NewDashFilter.TacticalAbilityDef = Run_Ability();            
        }

        private static void Adjust_Cost()
        {
            //"ExperimentalExhaustSystem_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef MarketOption = (GeoMarketplaceItemOptionDef)Repo.GetDef("4f63e91b-5bcb-b364-9903-083a98161a66");
            MarketOption.MaxPrice = 500f;
        }
    }
}