using Base.Defs;
using Base.Entities.Effects;
using Base.Serialization.General;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.Abilities 
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
	public class HealPassengersStatus : TacStatus
    {
        public HealPassengersStatusDef HealPassengersStatusDef
        {
            get
            {
                return this.Def<HealPassengersStatusDef>();
            }
        }

        public override void StartTurn()
        {
            base.StartTurn();
            // TestingMain.Main.Logger.LogInfo($"Vehicle has passenger: {this.TacticalActor.Vehicle.Passengers.Count > 0}");
            if(this.TacticalActor.Vehicle.Passengers.Count > 0)
            {
                foreach (TacticalActorBase passenger in this.TacticalActor.Vehicle.Passengers)
                {
                    // TestingMain.Main.Logger.LogInfo($"Passenger name: {passenger.DisplayName}");
                    if (passenger is TacticalActor tacticalactor)
                    {
                        tacticalactor.CharacterStats.Health.AddRestrictedToMax(this.HealPassengersStatusDef.RestoreHP);
                        tacticalactor.CharacterStats.WillPoints.AddRestrictedToMax(this.HealPassengersStatusDef.RestoreWP);
                        foreach(EffectDef effect in this.HealPassengersStatusDef.EffectsToApply)
                        {
                            Effect.Apply(base.Repo, effect, TacUtil.GetActorEffectTarget(passenger, null), null);
                        }
                    }
                }
                this.CallEvent();
            }
        }

        private void CallEvent()
        {
            if (this.HealPassengersStatusDef.EventOnStartTurn == null && base.TacticalActor != null)
            {
                return;
            }
            base.TacticalActor.TacActorEventusComponent.RaiseEvent(this.HealPassengersStatusDef.EventOnStartTurn, false, null);
        }
	}

}