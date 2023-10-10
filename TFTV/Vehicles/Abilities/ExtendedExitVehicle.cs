using System.Linq;
using Base;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;

namespace TFTVVehicleRework.Abilities 
{
    [SerializeType(InheritCustomCreateFrom = typeof(ExitVehicleAbility))]
    public class ExtendedExitVehicleAbility : ExitVehicleAbility
    {

        private TacticalAbilityCostModification APCostModification;

        public ExtendedExitVehicleAbilityDef ExtendedExitVehicleAbilityDef
        {
            get
            {
                return this.Def<ExtendedExitVehicleAbilityDef>();
            }
        }

        public override bool ShouldDisplay
        {
            get
            {
                if (base.ShouldDisplay)
                {
                    AdjustAccessCostStatus AdjustAccessCost = this.TacticalActor.Mount.TacticalActorBase.Status.GetStatus<AdjustAccessCostStatus>();
                    if (!AdjustAccessCost.IsDefaultValue() && AdjustAccessCost.AdjustAccessCostStatusDef.AccessDirection == AdjustAccessCostStatusDef.Direction.Exit)
                    {
                        if(this.APCostModification == null)
                        {
                            this.APCostModification = AdjustAccessCost.AdjustAccessCostStatusDef.AccessCostModification;
                            this.TacticalActor.AddAbilityCostModification(this.APCostModification);
                        }
                    }
                    else
                    {
                        if (this.APCostModification != null)
                        {
                            this.TacticalActor.RemoveAbilityCostModification(this.APCostModification);
                            this.APCostModification = null;
                        }
                    }
                }
                return base.ShouldDisplay;
            }
        }

        public override void Activate(object parameter = null)
        {
            base.Activate(parameter);
            if (this.APCostModification != null)
            {
                this.TacticalActor.RemoveAbilityCostModification(this.APCostModification);
                this.APCostModification = null;
            }
            Status status = this.TryGetStatusFromActor(base.TacticalActorBase);
            if (status != null)
            {
                base.TacticalActorBase.Status.UnapplyStatus(status);
            }
        }

        private Status TryGetStatusFromActor(TacticalActorBase actor)
        {
            return actor.Status.Statuses.FirstOrDefault((Status s) => s.Def == ExtendedExitVehicleAbilityDef.StealthStatus);
        }

    }
}