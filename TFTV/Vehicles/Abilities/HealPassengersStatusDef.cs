using Base.Serialization.General;
using Base.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Eventus;
using UnityEngine;

namespace TFTVVehicleRework.Abilities
{
    [CreateAssetMenu(fileName = "HealPassengersStatusDef", menuName = "Defs/Abilities/Tactical/HealPassengersStatus")]
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    public class HealPassengersStatusDef : TacStatusDef
    {
		[Min(0f)]
		[Header("Increase Passenger HP by quantity:")]
		public float RestoreHP;

		[Min(0f)]
		[Header("Increase Passenger WillPoints by quantity:")]
		public float RestoreWP;

    [Header("Apply effects to Passenger:")]
    public EffectDef[] EffectsToApply;

    [Header("Event Data")]
		public TacticalEventDef EventOnStartTurn;

    }
}