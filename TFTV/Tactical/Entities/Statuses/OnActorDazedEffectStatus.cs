using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Serialization.General;
using Base.Utils.Maths;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class OnActorDazedEffectStatus : TacStatus
    {
        public OnActorDazedEffectStatusDef OnActorDazedEffectStatusDef => BaseDef as OnActorDazedEffectStatusDef;

        public Effect Effect { get; private set; }

        public override void Init(BaseDef def, DefRepository repo)
        {
            base.Init(def, repo);
            Effect = repo.Instantiate<Effect>(OnActorDazedEffectStatusDef.EffectDef, null, null, null, false);
            Effect.Source = Source ?? this;
        }

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);
            if (TacticalActor == null)
            {
                RequestUnapply(statusComponent);
                return;
            }
            TacticalLevel.ActorSuppressedEvent += OnActorDazed; // ActorSuppressedEvent => event handler when an actor is dazed
        }

        public override void OnUnapply()
        {
            base.OnUnapply();
            TacticalLevel.ActorSuppressedEvent -= OnActorDazed; // ActorSuppressedEvent => event handler when an actor is dazed
        }

        public void OnActorDazed(TacticalActor dazedActor)
        {
            if (ShouldApplyEffect(dazedActor))
            {
                ApplyEffect();
            }
        }

        private bool ShouldApplyEffect(TacticalActor dazedActor)
        {
            bool dazedActorIsApplicant = dazedActor == TacticalActor;
            if (OnActorDazedEffectStatusDef.IsApplicant && dazedActorIsApplicant)
            {
                return true;
            }
            if (dazedActorIsApplicant)
            {
                return false;
            }
            if (OnActorDazedEffectStatusDef.DazedByApplicant && TacUtil.GetSourceTacticalActorBase(dazedActor.LastDamageSource) != TacticalActorBase)
            {
                return false;
            }
            if (OnActorDazedEffectStatusDef.TriggerOnRelation != FactionRelation.All && TacticalActorBase.RelationTo(dazedActor).HasFlag(OnActorDazedEffectStatusDef.TriggerOnRelation))
            {
                return true;
            }
            if (!dazedActor.HasGameTags(OnActorDazedEffectStatusDef.RequiredActorTags, OnActorDazedEffectStatusDef.NeedAllTags))
            {
                return false;
            }
            float magnitude = (TacticalActorBase.Pos - dazedActor.Pos).magnitude;
            bool dazedActorIsInRange = Utl.LesserThanOrEqualTo(magnitude, TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(OnActorDazedEffectStatusDef.Range, TacticalActorBase), 1E-05f);
            return float.IsPositiveInfinity(OnActorDazedEffectStatusDef.Range) || dazedActorIsInRange;
        }

        private void ApplyEffect()
        {
            if (UnapplyRequested || ActorIsImmune(TacticalActorBase))
            {
                return;
            }
            EffectTarget actorEffectTarget = TacUtil.GetActorEffectTarget(TacticalActorBase);
            Effect.Apply(actorEffectTarget);
            if (OnActorDazedEffectStatusDef.EventOnSuccessfulTrigger && TacticalActor != null)
            {
                TacticalActor.TacActorEventusComponent.RaiseEvent(OnActorDazedEffectStatusDef.EventOnSuccessfulTrigger, false, null);
            }
        }
    }
}
