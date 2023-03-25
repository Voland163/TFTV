using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;
using TFTV.Tactical.Entities.DamageKeywords;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
    public class ArmourBreakStatusDef : TacStatusDef
    {
        public GameTagsList WeaponTagFilter = new GameTagsList();
        public DamageKeywordPair[] DamageKeywordPairs;
        public ItemStatModification[] StatModifications;
        public int NumberOfAttacks = 1;

        public bool ReduceDamage;
        public StatModificationType ModificationType;
        public DamageKeywordPair ShreddingKeywordPair;
        public float DamageMultiplier = 1;
        public bool DamageModEqualToShred;
    }
}
