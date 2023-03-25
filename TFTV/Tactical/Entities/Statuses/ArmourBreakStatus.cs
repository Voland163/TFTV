using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Linq;
using TFTV.Tactical.Entities.DamageKeywords;

namespace TFTV.Tactical.Entities.Statuses
{
    [SerializeType(InheritCustomCreateFrom = typeof(Status))]
    public class ArmourBreakStatus : TacStatus
    {
        private int _attacksBoosted;

        public ArmourBreakStatusDef ArmourBreakStatusDef => BaseDef as ArmourBreakStatusDef;
        private bool LimitedAttacks => ArmourBreakStatusDef.NumberOfAttacks > 0;

        public override void OnApply(StatusComponent statusComponent)
        {
            base.OnApply(statusComponent);
            if (TacticalActor == null)
            {
                RequestUnapply(statusComponent);
                return;
            }
            if (LimitedAttacks)
            {
                TacticalActor.AbilityExecutedEvent += AbilityExecutedHandler;
            }
            if (ArmourBreakStatusDef.WeaponTagFilter != null && ArmourBreakStatusDef.WeaponTagFilter.Count > 0)
            {
                TacticalActor.Equipments.EquipmentChangedEvent += OnEquipmentChanged;
                OnEquipmentChanged(TacticalActor.Equipments.SelectedEquipment);
            }
            else
            {
                AddArmourBreak(TacticalActor.Equipments.SelectedWeapon);
            }
        }

        public override void OnUnapply()
        {
            base.OnUnapply();
            if (TacticalActor == null)
            {
                return;
            }
            if (ArmourBreakStatusDef.WeaponTagFilter != null)
            {
                TacticalActor.Equipments.EquipmentChangedEvent -= OnEquipmentChanged;
            }
            if (LimitedAttacks)
            {
                TacticalActor.AbilityExecutedEvent -= AbilityExecutedHandler;
            }
            RemoveArmourBreak(TacticalActor.Equipments.SelectedWeapon);
        }

        private void AbilityExecutedHandler(TacticalAbility ability)
        {
            if (!(ability is IAttackAbility) && !(ability is IDamageDealer))
            {
                return;
            }
            if (ArmourBreakStatusDef.NumberOfAttacks < 0)
            {
                return;
            }
            _attacksBoosted++;
            if (_attacksBoosted < ArmourBreakStatusDef.NumberOfAttacks)
            {
                return;
            }
            RequestUnapply(TacticalActor.Status);
        }

        private void OnEquipmentChanged(Equipment selectedEquipment)
        {
            Weapon weapon = selectedEquipment as Weapon;
            RemoveArmourBreak(weapon);
            if (weapon != null
                && ArmourBreakStatusDef.WeaponTagFilter.Any(tagDef => weapon.GameTags.Contains(tagDef)))
            {
                AddArmourBreak(weapon);
            }
        }

        private void AddArmourBreak(Weapon weapon)
        {
            TacticalActor.AddDamageKeywordPair(ArmourBreakStatusDef.ShreddingKeywordPair);
            if (ArmourBreakStatusDef.ReduceDamage)
            {
                TacticalActor.Status.AddStatModification(GetDamageModification(weapon, ArmourBreakStatusDef.ShreddingKeywordPair));
            }
        }

        private void RemoveArmourBreak(Weapon weapon)
        {
            TacticalActor.RemoveDamageKeywordPair(ArmourBreakStatusDef.ShreddingKeywordPair);
            if (ArmourBreakStatusDef.ReduceDamage && weapon != null)
            {
                TacticalActor.Status.RemoveStatModification(GetDamageModification(weapon, ArmourBreakStatusDef.ShreddingKeywordPair));
            }
        }

        private StatModification GetDamageModification(Weapon weapon, DamageKeywordPair shreddingKeywordPair)
        {
            StatModificationType statModificationType = StatModificationType.Multiply;
            float value = ArmourBreakStatusDef.DamageMultiplier;
            if (ArmourBreakStatusDef.DamageModEqualToShred && weapon != null)
            {
                statModificationType = StatModificationType.Add;
                ArmourBreakDamageKeywordDataDef armourBreakDamageKeyword = (ArmourBreakDamageKeywordDataDef)shreddingKeywordPair.DamageKeywordDef;
                value = armourBreakDamageKeyword.CalculateDamageValue(weapon.GetDamagePayload(), shreddingKeywordPair.Value);
            }
            StatModification damageMod = new StatModification(
                                    statModificationType,
                                    StatModificationTarget.BonusAttackDamage.ToString(),
                                    value,
                                    this,
                                    0f);
            return damageMod;
        }
    }
}
