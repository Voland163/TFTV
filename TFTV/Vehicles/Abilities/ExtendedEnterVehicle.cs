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
                this.UpdateAccessCostModification();
                return base.ShouldDisplay;
            }
        }

        /// <summary>
        /// Registers the entry AP discount granted by a vehicle module (the Armadillo's
        /// Lightweight Alloy, for one) on the operative.
        ///
        /// This can't wait for ShouldDisplay to become true. EnterVehicleAbility.ShouldDisplay
        /// only turns true once the operative already stands on the vehicle's entry point,
        /// whereas the entry tile marker is drawn earlier: TacUtil filters the operative's move
        /// targets through TacticalActor.GetMaxMoveAndActRange(), which prices this ability from
        /// the cost modifications registered on the operative at that moment. With the discount
        /// still unregistered the ability was priced at its full cost, so an operative with less
        /// than 1 AP got no marker on a tile they could in fact board from.
        /// </summary>
        private void UpdateAccessCostModification()
        {
            TacticalActor actor = this.TacticalActor;
            if (actor == null)
            {
                return;
            }

            TacticalAbilityCostModification modification = actor.IsMounted ? null : this.FindEntryCostModification();
            if (modification == this.APCostModification)
            {
                return;
            }

            if (this.APCostModification != null)
            {
                actor.RemoveAbilityCostModification(this.APCostModification);
                this.APCostModification = null;
            }

            if (modification != null)
            {
                this.APCostModification = modification;
                actor.AddAbilityCostModification(modification);
            }
        }

        /// <summary>
        /// The entry discount of the first boardable friendly vehicle that grants one, or null.
        /// </summary>
        private TacticalAbilityCostModification FindEntryCostModification()
        {
            TacticalFaction faction = this.TacticalActorBase.TacticalFaction;
            if (faction == null)
            {
                return null;
            }

            foreach (TacticalActor vehicleActor in faction.TacticalActors)
            {
                VehicleComponent vehicle = (vehicleActor != null) ? vehicleActor.Vehicle : null;
                if (vehicle == null || vehicle.IsFull || !vehicleActor.IsAlive)
                {
                    continue;
                }

                foreach (AdjustAccessCostStatus adjustAccessCost in vehicleActor.Status.GetStatuses<AdjustAccessCostStatus>())
                {
                    if (adjustAccessCost.IsDefaultValue()
                        || adjustAccessCost.AdjustAccessCostStatusDef.AccessDirection != AdjustAccessCostStatusDef.Direction.Entry)
                    {
                        continue;
                    }

                    return adjustAccessCost.AdjustAccessCostStatusDef.AccessCostModification;
                }
            }

            return null;
        }

        public override void Activate(object parameter = null)
        {
            base.Activate(parameter);
            if (this.APCostModification != null)
            {
                this.TacticalActor.RemoveAbilityCostModification(this.APCostModification);
                this.APCostModification = null;
            }
            if (this.ExtendedEnterVehicleAbilityDef.StealthStatus != null)
            {
                this.TacticalActor.Status.ApplyStatus(this.ExtendedEnterVehicleAbilityDef.StealthStatus);
            }
            //Hide actor upon entering vehicle:
            TacticalFactionVision.ForgetForAll(this.TacticalActorBase, true);
        }
    }
}
