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
        /// The entry discount that currently applies to this operative, or null.
        ///
        /// A TacticalAbilityCostModification carries no target of its own - AbilityQualifies()
        /// matches on the ability's skill tags - so once registered it discounts boarding any
        /// vehicle. The discount belongs to the vehicle carrying the module ("Entering the
        /// vehicle does not cost any Action Points"), which forces two regimes.
        /// </summary>
        private TacticalAbilityCostModification FindEntryCostModification()
        {
            // Standing on an entry point: the boarding target is settled, so price the exact
            // vehicle being boarded. This is the regime that governs whether the ability is
            // affordable and what Activate() actually charges, so a second vehicle parked
            // nearby never gets boarded on the Armadillo's discount.
            bool atEntryPoint = false;
            foreach (TacticalAbilityTarget target in base.GetTargets())
            {
                atEntryPoint = true;

                TacticalAbilityCostModification modification = GetEntryCostModification(target.Actor);
                if (modification != null)
                {
                    return modification;
                }
            }

            if (atEntryPoint)
            {
                return null;
            }

            // Out of position, so no boarding target exists yet. The entry tile marker is
            // priced right now - TacUtil runs the operative's move targets through
            // GetMaxMoveAndActRange(), which knows the ability but not the destination - so
            // the discount has to be registered for any boardable vehicle that grants one or
            // the tile is never highlighted at all. Planning only: by the time the operative
            // reaches an entry point the exact regime above takes over.
            TacticalFaction faction = this.TacticalActorBase.TacticalFaction;
            if (faction == null)
            {
                return null;
            }

            foreach (TacticalActor vehicleActor in faction.TacticalActors)
            {
                TacticalAbilityCostModification modification = GetEntryCostModification(vehicleActor);
                if (modification != null)
                {
                    return modification;
                }
            }

            return null;
        }

        /// <summary>
        /// The entry discount granted by one boardable vehicle, or null.
        /// </summary>
        private static TacticalAbilityCostModification GetEntryCostModification(TacticalActorBase vehicleActorBase)
        {
            VehicleComponent vehicle = (vehicleActorBase != null) ? vehicleActorBase.Vehicle : null;
            if (vehicle == null || vehicle.IsFull || !vehicleActorBase.IsAlive)
            {
                return null;
            }

            foreach (AdjustAccessCostStatus adjustAccessCost in vehicleActorBase.Status.GetStatuses<AdjustAccessCostStatus>())
            {
                if (adjustAccessCost.IsDefaultValue()
                    || adjustAccessCost.AdjustAccessCostStatusDef.AccessDirection != AdjustAccessCostStatusDef.Direction.Entry)
                {
                    continue;
                }

                return adjustAccessCost.AdjustAccessCostStatusDef.AccessCostModification;
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
