using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Equipments;
using UnityEngine;

namespace TFTVVehicleRework.Effects
{
    [SerializeType(InheritCustomCreateFrom = typeof(BaseDef))]
	[CreateAssetMenu(fileName = "ActorArmourThresholdEffectConditionDef", menuName = "Defs/Effects/Conditions/Actor Armour Threshold")]
    public class ActorArmourThresholdEffectConditionDef : StatThresholdEffectConditionDef
    {
        protected override bool ActorChecks(TacticalActorBase actor)
        {
            TacticalActor TacActor = actor as TacticalActor;
            if (TacActor == null || this.StatName != "Armour")
            {
                return false;
            }
            bool flag = false;
            foreach (ItemSlot slot in TacActor.BodyState.GetSlots())
            {
                StatusStat armor = slot.GetArmor();
                if (!ValueAsFractionOfMax)
                {
                    if(ThresholdSatisfied(this.ThresholdCondition, armor.Value, this.Value))
                    {
                        flag = true;
                    }
                }
                else
                {
                    if(ThresholdSatisfied(this.ThresholdCondition, armor.Value, this.Value*armor.Max))
                    {
                        flag = true;
                    }
                }
            }
            // IEnumerable<ItemSlot> slots = TacActor.BodyState.GetSlots();
            // bool flag = slots.Any((ItemSlot slot) => slot.GetArmor().Value < slot.GetArmor().Max);
            return flag;
        }
    }
}