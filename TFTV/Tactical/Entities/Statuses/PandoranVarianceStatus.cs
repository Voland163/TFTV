using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// Hidden marker on a Pandoran whose HP and armour were rolled by Harsh Attrition. The rolls themselves
    /// live on the stats, as AddMax modifications whose source is the def (stat modifications are saved
    /// with their source), so this only records what the stats cannot: whether the player has studied
    /// this individual up close, and how much damage it has taken. The rules live in <see cref="TFTVHarshAttrition.PandoranVariance"/>.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class PandoranVarianceStatus : TacStatus
    {
        /// <summary>The player passed the perception check on this individual: exact numbers for the rest of the mission.</summary>
        [SerializeMember]
        private bool _studied;

        internal bool Studied
        {
            get => _studied;
            set => _studied = value;
        }

        /// <summary>Hit Points lost so far, all sources: shown in place of health while the species is unknown.</summary>
        [SerializeMember]
        private float _damageTaken;

        internal float DamageTaken
        {
            get => _damageTaken;
            set => _damageTaken = value;
        }
    }
}
