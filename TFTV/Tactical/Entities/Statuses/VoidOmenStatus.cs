using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// Hidden status behind the tactical Void Omens. Stat modifications are sourced from the def, so they are saved
    /// with the stats and not put on again on load; this only keeps what the stats cannot, and gives the omens a turn
    /// start. The rules live in <see cref="TFTVVoidOmensExpansion.TacticalOmens"/>.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class VoidOmenStatus : TacStatus
    {
        /// <summary>VO36: this operative already gained its perk this mission.</summary>
        [SerializeMember]
        private bool _injuryPerkGranted;

        internal bool InjuryPerkGranted
        {
            get => _injuryPerkGranted;
            set => _injuryPerkGranted = value;
        }

        /// <summary>VO37: the stealth bonus currently on the stat, so it is only replaced when it changes.</summary>
        [SerializeMember]
        private float _stealthBonus;

        internal float StealthBonus
        {
            get => _stealthBonus;
            set => _stealthBonus = value;
        }

        private bool _listeningToWillPoints;

        // After OnApply: on load, OnApply runs before the stats are ready.
        public override void AfterApply()
        {
            base.AfterApply();

            if (TacticalActor?.CharacterStats?.WillPoints != null)
            {
                TacticalActor.CharacterStats.WillPoints.StatChangeEvent += OnWillPointsChanged;
                _listeningToWillPoints = true;
            }
        }

        public override void OnUnapply()
        {
            base.OnUnapply();

            if (_listeningToWillPoints)
            {
                TacticalActor.CharacterStats.WillPoints.StatChangeEvent -= OnWillPointsChanged;
                _listeningToWillPoints = false;
            }
        }

        public override void StartTurn()
        {
            base.StartTurn();
            TFTVVoidOmensExpansion.TacticalOmens.OnStatusStartTurn(this);
        }

        private void OnWillPointsChanged(BaseStat stat, StatChangeType change, float prevValue, float unclampedValue)
        {
            TFTVVoidOmensExpansion.TacticalOmens.RefreshStealth(this);
        }
    }
}
