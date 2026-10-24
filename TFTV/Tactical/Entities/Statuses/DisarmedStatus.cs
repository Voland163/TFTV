using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Statuses;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// Hidden record on a humanoid enemy whose weapon was destroyed: which weapon, whether it has fled and is
    /// due back next turn, and whether it already came back once. The rules live in
    /// <see cref="TFTVHarshAttrition.DisarmedReturn"/>.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class DisarmedStatus : TacStatus
    {
        [SerializeMember]
        private string _weaponDefName;

        [SerializeMember]
        private bool _pendingReturn;

        [SerializeMember]
        private bool _returned;

        [SerializeMember]
        private float _exitX;

        [SerializeMember]
        private float _exitY;

        [SerializeMember]
        private float _exitZ;

        internal string WeaponDefName
        {
            get => _weaponDefName;
            set => _weaponDefName = value;
        }

        internal bool PendingReturn
        {
            get => _pendingReturn;
            set => _pendingReturn = value;
        }

        internal bool Returned
        {
            get => _returned;
            set => _returned = value;
        }

        /// <summary>Where it left the map; it comes back at the nearest unseen spot of its side's deploy zones.</summary>
        internal Vector3 ExitPosition
        {
            get => new Vector3(_exitX, _exitY, _exitZ);
            set
            {
                _exitX = value.x;
                _exitY = value.y;
                _exitZ = value.z;
            }
        }
    }
}
