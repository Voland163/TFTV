using Base.Entities.Effects;
using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Eventus;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    [CreateAssetMenu(fileName = "OnActorDazedEffectStatusDef", menuName = "Defs/Statuses/OnActorDazedEffectStatusDef")]
    public class OnActorDazedEffectStatusDef : TacStatusDef
    {
        public bool IsApplicant;
        public FactionRelation TriggerOnRelation = FactionRelation.Enemy;
        public GameTagDef[] RequiredActorTags = new GameTagDef[0];
        public bool NeedAllTags;
        public bool DazedByApplicant = true;
        public float Range = float.PositiveInfinity;
        //public EffectDef EffectDef;
        public float RestoreActionPointsFraction;
        public TacticalEventDef EventOnSuccessfulTrigger;
        public int TriggerCount = 1;
    }
}
