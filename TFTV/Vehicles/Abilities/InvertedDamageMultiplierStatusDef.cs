using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTVVehicleRework.Abilities
{
    [SerializeType(InheritCustomCreateFrom = typeof(DamageMultiplierStatusDef))]
    public class InvertedDamageMultiplierStatusDef : DamageMultiplierStatusDef
    {
    }
}