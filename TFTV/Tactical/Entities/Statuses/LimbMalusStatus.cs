using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// The limb hit malus on one character: per body part, how hard it has been hit (severity), how
    /// many of the character's turn ends that lasts, and how often the part has been restored after
    /// being disabled. The rules and the stat changes live in <see cref="TFTVLimbHitMalus"/>; this
    /// class only holds the per-part record so it survives a tactical save, and calls back into the
    /// rules at the right moments.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class LimbMalusStatus : TacStatus
    {
        [SerializeMember]
        private List<string> _slotNames = new List<string>();

        [SerializeMember]
        private List<float> _severities = new List<float>();

        /// <summary>How many of the character's turn ends the severity still lasts.</summary>
        [SerializeMember]
        private List<int> _endsLeft = new List<int>();

        [SerializeMember]
        private List<int> _restorations = new List<int>();

        /// <summary>Will Points the lower Willpower cut off, given back as the malus fades.</summary>
        [SerializeMember]
        private float _willPointsTrimmed;

        internal float WillPointsTrimmed
        {
            get => _willPointsTrimmed;
            set => _willPointsTrimmed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// The injured arms and their spread shares, as of the last refresh. Not saved: the refresh after a load
        /// (TFTVLimbHitMalus.RefreshAll) rebuilds it.
        /// </summary>
        internal List<TFTVLimbHitMalus.PartMalus> CachedArms;

        public LimbMalusStatusDef LimbMalusStatusDef => BaseDef as LimbMalusStatusDef;

        private List<string> SlotNames => _slotNames ?? (_slotNames = new List<string>());
        private List<float> Severities => _severities ?? (_severities = new List<float>());
        private List<int> EndsLeft => _endsLeft ?? (_endsLeft = new List<int>());
        private List<int> Restorations => _restorations ?? (_restorations = new List<int>());

        internal IEnumerable<string> TrackedSlots => SlotNames;

        private int IndexOf(string slotName)
        {
            int index = SlotNames.IndexOf(slotName);

            // Older or partly written records: pad the parallel lists so every index is valid.
            if (index >= 0)
            {
                while (Severities.Count <= index) { Severities.Add(0f); }
                while (EndsLeft.Count <= index) { EndsLeft.Add(0); }
                while (Restorations.Count <= index) { Restorations.Add(0); }
            }

            return index;
        }

        private int GetOrAdd(string slotName)
        {
            int index = IndexOf(slotName);

            if (index >= 0)
            {
                return index;
            }

            SlotNames.Add(slotName);
            Severities.Add(0f);
            EndsLeft.Add(0);
            Restorations.Add(0);

            return SlotNames.Count - 1;
        }

        internal float GetSeverity(string slotName)
        {
            int index = IndexOf(slotName);
            return index >= 0 ? Severities[index] : 0f;
        }

        internal int GetEndsLeft(string slotName)
        {
            int index = IndexOf(slotName);
            return index >= 0 ? EndsLeft[index] : 0;
        }

        internal int GetRestorations(string slotName)
        {
            int index = IndexOf(slotName);
            return index >= 0 ? Restorations[index] : 0;
        }

        internal void SetSeverity(string slotName, float severity, int endsLeft)
        {
            int index = GetOrAdd(slotName);
            Severities[index] = severity;
            EndsLeft[index] = endsLeft;
        }

        internal void ClearSeverity(string slotName)
        {
            int index = IndexOf(slotName);

            if (index >= 0)
            {
                Severities[index] = 0f;
                EndsLeft[index] = 0;
            }
        }

        /// <summary>A disabled part came back: one more step of residual malus, and a clean hit record.</summary>
        internal void AddRestoration(string slotName)
        {
            int index = GetOrAdd(slotName);
            Restorations[index]++;
            Severities[index] = 0f;
            EndsLeft[index] = 0;
        }

        /// <summary>Counts down one turn end for every part with severity; returns true when any expired.</summary>
        internal bool CountDownTurnEnd()
        {
            bool expired = false;

            for (int i = 0; i < SlotNames.Count; i++)
            {
                IndexOf(SlotNames[i]);

                if (Severities[i] <= 0f)
                {
                    continue;
                }

                EndsLeft[i]--;

                if (EndsLeft[i] <= 0)
                {
                    Severities[i] = 0f;
                    EndsLeft[i] = 0;
                    expired = true;
                }
            }

            return expired;
        }

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);

            if (TacticalActor == null)
            {
                RequestUnapply(statusComponent);
                return;
            }

            if (TacticalActor.Equipments != null)
            {
                TacticalActor.Equipments.EquipmentChangedEvent += OnEquipmentChanged;
            }

            // No refresh here: a new status is still empty (whoever applied it fills it in and
            // refreshes), and a loaded one is refreshed once the level is up
            // (TFTVLimbHitMalus.RefreshAll), when the character's stats and equipment are.
        }

        public override void OnUnapply()
        {
            base.OnUnapply();

            if (TacticalActor == null)
            {
                return;
            }

            if (TacticalActor.Equipments != null)
            {
                TacticalActor.Equipments.EquipmentChangedEvent -= OnEquipmentChanged;
            }

            TFTVLimbHitMalus.RemoveStats(TacticalActor, this);
        }

        public override void StartTurn()
        {
            base.StartTurn();
            TFTVLimbHitMalus.Refresh(this);
        }

        public override void EndTurn()
        {
            base.EndTurn();
            TFTVLimbHitMalus.OnEndTurn(this);
        }

        public override IEnumerable<string> GetTargetSlotsNames()
        {
            return TFTVLimbHitMalus.GetAffectedSlotNames(this);
        }

        private void OnEquipmentChanged(Equipment selectedEquipment)
        {
            TFTVLimbHitMalus.Refresh(this);
        }
    }
}
