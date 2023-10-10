using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.Abilities 
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class AdjustAccessCostStatus : TacStatus
    {
        public AdjustAccessCostStatusDef AdjustAccessCostStatusDef
        {
            get
            {
                return this.Def<AdjustAccessCostStatusDef>();
            }
        }
    }
}