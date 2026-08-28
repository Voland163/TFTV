using Base;
using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using UnityEngine;

namespace TFTVVehicleRework.Abilities 
{
    [SerializeType(InheritCustomCreateFrom = typeof(EnterVehicleAbility))]
    public class ExtendedEnterVehicleAbility : EnterVehicleAbility
    {

        private TacticalAbilityCostModification APCostModification;

        // While an activation is in flight the boarding target is known, so the target-based
        // decision made in Activate() must not be overwritten by the position-based one.
        private bool _resolvingActivation;

        /// <summary>The discount currently registered on the operative by this ability, or null.</summary>
        internal TacticalAbilityCostModification RegisteredAccessCostModification
        {
            get { return this.APCostModification; }
        }

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
                // Activating fires events that refresh the ability bar, which lands back here.
                // ActionPointCost is read live when ApplyCosts() runs, so re-registering by
                // position at that moment would silently undo Activate()'s target-based choice
                // and hand the operative a free boarding of whatever else is parked alongside.
                if (!this._resolvingActivation)
                {
                    this.UpdateAccessCostModification();
                }

                return base.ShouldDisplay;
            }
        }

        /// <summary>
        /// Registers the entry AP discount granted by the vehicle the operative is in position
        /// to board (the Armadillo's Lightweight Alloy, for one).
        ///
        /// A TacticalAbilityCostModification carries no target of its own - AbilityQualifies()
        /// matches on the ability's skill tags - so any registered discount applies to every
        /// vehicle the operative boards. Keeping it tied to what is boardable from the
        /// operative's own tile is therefore the only thing stopping a second, module-less
        /// vehicle from being boarded for free.
        ///
        /// The entry tile marker is priced before the operative is in position and is handled
        /// separately, per candidate tile, by MoveAbilityTargetData_IsActorInActionRange_Patch.
        /// </summary>
        private void UpdateAccessCostModification()
        {
            TacticalActor actor = this.TacticalActor;
            if (actor == null)
            {
                return;
            }

            this.SetAccessCostModification(
                actor.IsMounted ? null : this.GetEntryCostModificationAt(actor.Pos));
        }

        private void SetAccessCostModification(TacticalAbilityCostModification modification)
        {
            TacticalActor actor = this.TacticalActor;
            if (actor == null || modification == this.APCostModification)
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
        /// The entry discount granted by a vehicle boardable from <paramref name="position"/>,
        /// or null. Only vehicles this operative could actually board from that exact tile are
        /// consulted, so the discount never reaches a vehicle without the module.
        /// </summary>
        internal TacticalAbilityCostModification GetEntryCostModificationAt(Vector3 position)
        {
            foreach (TacticalAbilityTarget target in base.GetTargetsAt(position))
            {
                TacticalAbilityCostModification modification = GetEntryCostModification(target.Actor);
                if (modification != null)
                {
                    return modification;
                }
            }

            return null;
        }

        /// <summary>
        /// True when any boardable friendly vehicle grants an entry discount, wherever it stands.
        ///
        /// Deliberately not position-aware: this answers only "is there somewhere worth walking
        /// to", which is what SceneViewElement.IsValid() needs in order to let the marker pass
        /// run at all. Which tiles actually light up is decided per tile afterwards, by
        /// MoveAbilityTargetData_IsActorInActionRange_Patch.
        /// </summary>
        internal bool HasEntryDiscountSomewhere()
        {
            TacticalActor actor = this.TacticalActor;
            if (actor == null || actor.IsMounted)
            {
                return false;
            }

            TacticalFaction faction = this.TacticalActorBase.TacticalFaction;
            if (faction == null)
            {
                return false;
            }

            foreach (TacticalActor vehicleActor in faction.TacticalActors)
            {
                if (GetEntryCostModification(vehicleActor) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The entry discount granted by one vehicle, or null.
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
            // base.Activate() charges the AP, so settle the discount against the vehicle
            // actually being boarded first. One tile can be an entry point for two vehicles,
            // and the cost modification carries no target that could tell them apart.
            TacticalAbilityTarget target = parameter as TacticalAbilityTarget;

            this._resolvingActivation = true;
            try
            {
                this.SetAccessCostModification(
                    (target != null) ? GetEntryCostModification(target.Actor) : null);

                base.Activate(parameter);
            }
            finally
            {
                this._resolvingActivation = false;
                this.SetAccessCostModification(null);
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
