using Base.Defs;
using Base.Serialization.General;
using Base.Utils.Maths;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.Abilities
{
    [SerializeType(InheritCustomCreateFrom = typeof(DamageMultiplierStatus))]
    public class InvertedDamageMultiplierStatus : DamageMultiplierStatus
    {
        public InvertedDamageMultiplierStatusDef InvertedDamageMultiplierStatusDef
        {
            get
            {
                return this.Def<InvertedDamageMultiplierStatusDef>();
            }
        }

        protected override float GetIncomingMultiplier(object source)
        {
            if(Utl.LesserThan(this.InvertedDamageMultiplierStatusDef.Range, 0f, 1E-05f) || source == null)
            {
                return this.InvertedDamageMultiplierStatusDef.Multiplier;
            }
            TacticalActorBase damageSource = TacUtil.GetSourceTacticalActorBase(source);
            if (damageSource == null)
            {
                return this.InvertedDamageMultiplierStatusDef.Multiplier;
            }
            float range = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(this.InvertedDamageMultiplierStatusDef.Range, damageSource);
            //Inverts the Condition to require damage source to be from further away
            if (Utl.GreaterThanOrEqualTo((base.TacticalActorBase.Pos - damageSource.Pos).magnitude, range, 1E-05f))
            {
                return this.InvertedDamageMultiplierStatusDef.Multiplier;
            }
            return 1f;
        }

        protected override float GetOutgoingMultiplier(IDamageReceiver target)
        {
            if (target == null)
            {
                return this.InvertedDamageMultiplierStatusDef.Multiplier;
            }
            if (target.GetActor() == null)
            {
                return 1f;
            }
            if (target.GetActor().HasGameTags(this.InvertedDamageMultiplierStatusDef.OutgoingDamageTargetTags, false))
            {
                if(Utl.LesserThan(this.InvertedDamageMultiplierStatusDef.Range, 0f, 1E-05f))
                {
                    return this.InvertedDamageMultiplierStatusDef.Multiplier;
                }
                float range = TacticalNavigationComponent.ExtendRangeWithNavAgentRadius(this.InvertedDamageMultiplierStatusDef.Range, target.GetActor());
                if(Utl.GreaterThanOrEqualTo((base.TacticalActorBase.Pos - target.GetActor().Pos).magnitude, range, 1E-05f))
                {
                    return this.InvertedDamageMultiplierStatusDef.Multiplier;
                }
            }
            return 1f;
        }

    }
}