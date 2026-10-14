using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TFTV.Tactical.Entities.Statuses
{
    /// <summary>
    /// Fire burning on a character's body parts after an incendiary hit (the fire rework's "fire
    /// status"). Each burning part keeps its own fire value; the status itself only exists while at
    /// least one part burns. All the rules live in <see cref="TFTVFireRework"/>; this class holds
    /// the per-part values so they survive a tactical save.
    ///
    /// Unlike the vanilla FireStatus it replaces, nothing here is read from the fire tiles around the
    /// character: walking out of the fire does not put it out.
    /// </summary>
    [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
    public class BurningStatus : TacStatus
    {
        [SerializeMember]
        private List<string> _slotNames = new List<string>();

        [SerializeMember]
        private List<float> _values = new List<float>();

        public BurningStatusDef BurningStatusDef => BaseDef as BurningStatusDef;

        /// <summary>The highest fire value among the burning parts, which is what the chip shows.</summary>
        public override float Value => Values.Count == 0 ? 0f : Values.Max();

        private List<string> SlotNames => _slotNames ?? (_slotNames = new List<string>());

        private List<float> Values => _values ?? (_values = new List<float>());

        internal bool HasBurningParts => Values.Any(value => value > 0f);

        /// <summary>Slot name and fire value of every burning part.</summary>
        internal List<KeyValuePair<string, float>> GetParts()
        {
            List<KeyValuePair<string, float>> parts = new List<KeyValuePair<string, float>>();

            for (int i = 0; i < SlotNames.Count && i < Values.Count; i++)
            {
                if (Values[i] > 0f)
                {
                    parts.Add(new KeyValuePair<string, float>(SlotNames[i], Values[i]));
                }
            }

            return parts;
        }

        internal float GetPartValue(string slotName)
        {
            int index = SlotNames.IndexOf(slotName);
            return index >= 0 && index < Values.Count ? Values[index] : 0f;
        }

        /// <summary>Sets one part's fire value; 0 or less puts that part out.</summary>
        internal void SetPartValue(string slotName, float value)
        {
            int index = SlotNames.IndexOf(slotName);

            if (value <= 0f)
            {
                if (index >= 0)
                {
                    SlotNames.RemoveAt(index);
                    Values.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                Values[index] = value;
            }
            else
            {
                SlotNames.Add(slotName);
                Values.Add(value);
            }
        }

        internal void ClearParts()
        {
            SlotNames.Clear();
            Values.Clear();
        }

        /// <summary>Refreshes the chip, and removes the status once no part burns any more.</summary>
        internal void NotifyPartsChanged()
        {
            if (!HasBurningParts)
            {
                RequestUnapply(StatusComponent);
                return;
            }

            OnValueChanged();
        }

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);

            if (TacticalActor == null)
            {
                RequestUnapply(statusComponent);
                return;
            }

            if (CurrentlyDeserializing)
            {
                return;
            }

            if (TFTVFireRework.IsImmuneToBurning(TacticalActor))
            {
                RequestUnapply(statusComponent);
                return;
            }

            ShowStatusNotification(null);
        }

        public override void StartTurn()
        {
            base.StartTurn();
            TFTVFireRework.OnBurningStartTurn(this);
        }

        public override IEnumerable<string> GetTargetSlotsNames()
        {
            return GetParts().Select(part => part.Key);
        }

        /// <summary>The same per-size flames as the vanilla FireStatus.GetParticlePrefab.</summary>
        protected override GameObject GetParticlePrefab()
        {
            FireStatusDef vanilla = BurningStatusDef?.VanillaFireStatusDef;

            if (vanilla == null || TacticalActorBase?.NavigationComponent == null)
            {
                return base.GetParticlePrefab();
            }

            float radius = TacticalActorBase.NavigationComponent.AgentNavSettings.AgentRadius;

            if (radius <= 1f + 1E-05f)
            {
                return vanilla.HumanoidFireEffectPrefab;
            }

            if (radius <= 1.5f + 1E-05f)
            {
                return vanilla.MedMonsterFireEffectPrefab;
            }

            if (radius <= 2.5f + 1E-05f)
            {
                return vanilla.BigMonsterFireEffectPrefab;
            }

            return base.GetParticlePrefab();
        }
    }
}
