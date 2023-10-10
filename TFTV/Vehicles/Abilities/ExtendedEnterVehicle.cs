using Base;
using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System.Collections.Generic;

namespace TFTVVehicleRework.Abilities 
{
    [SerializeType(InheritCustomCreateFrom = typeof(EnterVehicleAbility))]
    public class ExtendedEnterVehicleAbility : EnterVehicleAbility
    {

        private TacticalAbilityCostModification APCostModification;

        public ExtendedEnterVehicleAbilityDef ExtendedEnterVehicleAbilityDef
        {
            get
            {
                return this.Def<ExtendedEnterVehicleAbilityDef>();
            }
        }

        public override bool ShouldDisplay
        {
            get
            {
                if (base.ShouldDisplay)
                {
                    IEnumerable<TacticalAbilityTarget> targets = base.GetTargets();
                    foreach (TacticalAbilityTarget tacticalAbilityTarget in targets)
                    {
                        VehicleComponent vehicle = tacticalAbilityTarget.Actor.Vehicle;
                        AdjustAccessCostStatus AdjustAccessCost = vehicle.TacticalActorBase.Status.GetStatus<AdjustAccessCostStatus>();
                        if (!AdjustAccessCost.IsDefaultValue() && AdjustAccessCost.AdjustAccessCostStatusDef.AccessDirection == AdjustAccessCostStatusDef.Direction.Entry)
                        {
                            if(this.APCostModification == null)
                            {   
                                this.APCostModification = AdjustAccessCost.AdjustAccessCostStatusDef.AccessCostModification;
                                this.TacticalActor.AddAbilityCostModification(this.APCostModification);
                            }
                        }
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
                return base.ShouldDisplay;
            }
        }

        public override void Activate(object parameter = null)
        {
            base.Activate(parameter);
            if (this.ExtendedEnterVehicleAbilityDef.StealthStatus != null)
            {
                this.TacticalActor.Status.ApplyStatus(this.ExtendedEnterVehicleAbilityDef.StealthStatus);
            }
            //Hide actor upon entering vehicle:
            TacticalFactionVision.ForgetForAll(this.TacticalActorBase, true);
        }
    }
}