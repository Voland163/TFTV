using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;

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
        /// <summary>
        /// The stats were rolled. The status can go on before that (studied or hurt on the frame it spawned), so
        /// having it does not mean the roll is done.
        /// </summary>
        [SerializeMember]
        private bool _rolled;

        internal bool Rolled
        {
            get => _rolled;
            set => _rolled = value;
        }

        /// <summary>
        /// Added to every strike of its melee weapons. A payload belongs to the weapon's def, shared by every creature
        /// carrying it, so this cannot go on the weapon the way the other rolls go on the stats.
        /// </summary>
        [SerializeMember]
        private int _meleeDamageRoll;

        internal int MeleeDamageRoll
        {
            get => _meleeDamageRoll;
            set => _meleeDamageRoll = value;
        }

        /// <summary>
        /// Species and body-part defs it spawned with (TFTVHarshAttrition.PandoranVariance.ComputeTemplate): what the
        /// player has to have killed or captured before for its Hit Points to be known.
        /// </summary>
        [SerializeMember]
        private string _template;

        internal string Template
        {
            get => _template;
            set => _template = value;
        }

        /// <summary>The player passed the perception check on this individual: exact numbers for the rest of the mission.</summary>
        [SerializeMember]
        private bool _studied;

        internal bool Studied
        {
            get => _studied;
            set => _studied = value;
        }

        /// <summary>
        /// Slots (their def names) of the body parts a hit has got through to: the damage numbers give their armour away,
        /// so it shows as it is. Kept rather than read off the stats, so healing a part does not hide it again.
        /// </summary>
        [SerializeMember]
        private List<string> _hurtParts;

        internal List<string> HurtParts
        {
            get
            {
                if (_hurtParts == null)
                {
                    _hurtParts = new List<string>();
                }

                return _hurtParts;
            }
        }

        /// <summary>
        /// Damage roll of each weapon it has carried, as "WeaponDefName=1.0734" (TFTVHarshAttrition.PandoranVariance
        /// weapon rolls). A payload belongs to the shared weapon def, so the roll is kept here, per weapon def.
        /// </summary>
        [SerializeMember]
        private List<string> _weaponRolls;

        internal List<string> WeaponRolls
        {
            get
            {
                if (_weaponRolls == null)
                {
                    _weaponRolls = new List<string>();
                }

                return _weaponRolls;
            }
        }

        /// <summary>Weapon defs the player has seen this individual attack with: their tooltips show the rolled values.</summary>
        [SerializeMember]
        private List<string> _revealedWeapons;

        internal List<string> RevealedWeapons
        {
            get
            {
                if (_revealedWeapons == null)
                {
                    _revealedWeapons = new List<string>();
                }

                return _revealedWeapons;
            }
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
