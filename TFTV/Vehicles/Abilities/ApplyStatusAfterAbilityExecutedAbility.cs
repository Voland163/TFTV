using Base.Defs;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Linq;

namespace TFTVVehicleRework.Abilities
{   
    [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbility))]
    public class ApplyStatusAfterAbilityExecutedAbility : TacticalAbility
    {

        public ApplyStatusAfterAbilityExecutedAbilityDef ApplyStatusAfterAbilityExecutedAbilityDef
        {
            get
            {
                return this.Def<ApplyStatusAfterAbilityExecutedAbilityDef>();
            }
        }

        public override void AbilityAdded()
        {
            base.AbilityAdded();
            base.TacticalActorBase.TacticalLevel.AbilityExecutedEvent += OnAbilityExecuted;
        }

        public override void AbilityRemovingStart()
        {
            base.AbilityRemovingStart();
            Status status = this.TryGetStatusFromActor(base.TacticalActorBase);
            if (status != null)
            {
                base.TacticalActorBase.Status.UnapplyStatus(status);
            }
            base.TacticalActorBase.TacticalLevel.AbilityExecutedEvent -= OnAbilityExecuted;
        }

        private void OnAbilityExecuted(TacticalAbility ability, object parameter)
        {
            if (base.TacticalActorBase == null)
            {
                return;
            }
            if (!base.TacticalActorBase.TacticalLevel.TurnIsPlaying)
            {
                return;
            }
            CheckApplicationConditions();
        }

        private void CheckApplicationConditions()
        {
            if (((TacStatusDef)ApplyStatusAfterAbilityExecutedAbilityDef.StatusToApply).ApplicationConditions.All((EffectConditionDef condition) => condition.ConditionMet(base.TacticalActorBase)))
            {
                ApplyStatusIfNotApplied(base.TacticalActorBase);
                return;
            }
            Status status = TryGetStatusFromActor(base.TacticalActorBase);
            if (status != null)
            {
                base.TacticalActorBase.Status.UnapplyStatus(status);
            }
        }

        private void ApplyStatusIfNotApplied(TacticalActorBase actor)
        {
            Status status = TryGetStatusFromActor(actor);
            if (status == null)
            {
                actor.Status.ApplyStatus(ApplyStatusAfterAbilityExecutedAbilityDef.StatusToApply);
            }
        }

        private Status TryGetStatusFromActor(TacticalActorBase actor)
        {
            return actor.Status.Statuses.FirstOrDefault((Status s) => s.Def == ApplyStatusAfterAbilityExecutedAbilityDef.StatusToApply);
        }
    }
}