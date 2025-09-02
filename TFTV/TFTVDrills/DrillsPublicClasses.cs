using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal class DrillsPublicClasses
    {
        [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
        [CreateAssetMenu(fileName = "StaticArmorTacStatsStatusDef", menuName = "Defs/Statuses/StaticArmorTacStatsStatus")]
        public class StaticArmorTacStatsStatusDef : TacStatusDef
        {
            // Optional: show a popup per modifier when applied.
            public bool ShowPopups = false;

            // Optional: used if a specific modifier didn’t set its own PopupInfoMessageId.
            public string DefaultPopupInfoMessageId = null;
        }



        [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
        public class StaticArmorTacStatsStatus : TacStatus
        {
            public StaticArmorTacStatsStatusDef StaticDef => this.Def<StaticArmorTacStatsStatusDef>();

            // Keep exactly what we applied so we can remove it later.
            private readonly List<ItemStatModification> _applied = new List<ItemStatModification>();

            public override void OnApply(StatusComponent statusComponent)
            {
                base.OnApply(statusComponent);
                if (base.CurrentlyDeserializing)
                {
                    return;
                }

                StatsModifierPopup[] statsModifiers = GetStatModifiersFromArmor();

                for (int i = 0; i < statsModifiers.Length; i++)
                {
                    StatsModifierPopup statsModifierPopup = statsModifiers[i];
                    base.StatusComponent.AddStatModification(statsModifierPopup.StatModification);
                    if (!string.IsNullOrWhiteSpace(statsModifierPopup.PopupInfoMessageId))
                    {
                        base.TacticalActor.TacticalActorView.ShowNotification(statsModifierPopup.PopupInfoMessageId, statsModifierPopup.StatModification.Value);
                    }
                }
            }

            public override void OnUnapply()
            {
                base.OnUnapply();
                StatsModifierPopup[] statsModifiers = GetStatModifiersFromArmor();
                for (int i = 0; i < statsModifiers.Length; i++)
                {
                    StatsModifierPopup statsModifierPopup = statsModifiers[i];
                    base.StatusComponent.RemoveStatModification(statsModifierPopup.StatModification);
                }
            }


            private StatsModifierPopup[] GetStatModifiersFromArmor()
            {

                List<TacticalItem> armors = TacticalActor.BodyState.GetArmourItems().ToList();



                float accuracy = 0;
                float perception = 0;
                float stealth = 0;
                float speed = 0;

                foreach (TacticalItem tacticalItem in armors)
                {
                    BodyPartAspectDef bodyPartAspectDef = tacticalItem.TacticalItemDef.BodyPartAspectDef;

                    accuracy -= bodyPartAspectDef.Accuracy;
                    perception -= bodyPartAspectDef.Perception;
                    stealth -= bodyPartAspectDef.Stealth;
                    speed -= bodyPartAspectDef.Speed;
                }

                accuracy /= 2;
                perception /= 2;
                stealth /= 2;
                speed /= 2;



                return new StatsModifierPopup[]
                    {

                    new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Accuracy",
                        accuracy,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    },


                    new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Perception",
                        perception,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    },

                      new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Stealth",
                        stealth,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    },

                      new StatsModifierPopup
                        {
                        StatModification = new StatModification(
                        StatModificationType.Add,
                        "Speed",
                        speed,
                        null, // source argument required by constructor
                        0f    // applicationValue argument required by constructor
                    ),

                        PopupInfoMessageId = null
                    },

                    };
            }
        }


        [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
        public class TacStrengthDamageMultiplierStatus : TacStatus, IDamageMultiplier
        {
            private TacStrengthDamageMultiplierStatusDef MyDef => (TacStrengthDamageMultiplierStatusDef)TacStatusDef;

            public DamageMultiplierType MultiplierType =>
                MyDef.MultiplierType; // keep configurable (defaults to Outgoing in the Def)

            public bool AffectsDamageType(DamageTypeBaseEffectDef damageType)
            {
                // If none set, affect all; otherwise only the configured melee damage type.
                var meleeType = MyDef.MeleeStandardDamageType;
                return meleeType == null || damageType == meleeType;
            }

            public float GetMultiplier(object source, IDamageReceiver target)
            {
                if (UnapplyRequested || TacticalActor == null) return 1f;

                // 1f + Endurance / 2 / 100  ==  1 + Endurance / 200
                float endurance = TacticalActor.CharacterStats.Endurance.Value.EndValue;
                return 1f + endurance / 200f;
            }
        }

        [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
        public class TacStrengthDamageMultiplierStatusDef : TacStatusDef
        {
            // If set, the status only applies to this damage type (e.g., the game’s standard melee damage type).
            // Leave null to apply to all damage types.
            public DamageTypeBaseEffectDef MeleeStandardDamageType;

            // Keep this configurable; your current use is Outgoing.
            public DamageMultiplierType MultiplierType = DamageMultiplierType.Outgoing;
        }


        [SerializeType(InheritCustomCreateFrom = typeof(BaseDef))]
        [CreateAssetMenu(fileName = "ActorHasAtLeastItemsEffectConditionDef",
                     menuName = "Defs/Effects/Conditions/Actor Has At Least Items")]
        public class ActorHasAtLeastItemsEffectConditionDef : TacActorEffectConditionDef
        {
            [Tooltip("List of items that count toward the requirement.")]
            public List<TacticalItemDef> Items;

            [Tooltip("How many of the listed items are required.")]
            public int RequiredCount = 3;

            protected override bool ActorChecks(TacticalActorBase actor)
            {
                var tacticalActor = actor as TacticalActor;
                if (tacticalActor == null) return false;

                // Count how many of the actor’s armour items are in the list
                int count = tacticalActor.BodyState
                                         .GetArmourItems()
                                         .Count(x => Items.Contains(x.ItemDef));

                return count >= RequiredCount;
            }
        }


        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(TacEffectStatus))]
        public class LightStunStatus : TacEffectStatus
        {
            public LightStunStatusDef LightStunStatusDef => this.Def<LightStunStatusDef>();

            public override void OnApply(StatusComponent statusComponent)
            {
                bool applied = base.Applied;
                base.OnApply(statusComponent);
                if (!applied)
                {
                    ShowStatusNotification();
                    ApplyEffect();   // <-- make sure our AP reduction runs
                }
            }

            public override void ApplyEffect()
            {
                //TFTVLogger.Always($"apply effect: {UnapplyRequested} immune: {ActorIsImmune(TacticalActorBase)} tacticalactor null {TacticalActor == null}");
                if (UnapplyRequested || ActorIsImmune(TacticalActorBase)) return;

                base.ApplyEffect(); // spawns the EffectDef/particles/etc.

                if (TacticalActor != null)
                {
                    float cut = TacticalActor.CharacterStats.ActionPoints.Max * LightStunStatusDef.ActionPointsReduction;
                  //  TFTVLogger.Always($"AP cut (light stun): {cut}");
                    TacticalActor.CharacterStats.ActionPoints.Subtract(cut);
                }
            }
        }


        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(TacEffectStatusDef))]
        [CreateAssetMenu(fileName = "LightStunStatusDef", menuName = "Defs/Statuses/LightStunStatus")]
        public class LightStunStatusDef : TacEffectStatusDef
        {
            [Range(0f, 1f)]
            public float ActionPointsReduction = 0.5f;
        }



        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(StunStatusDef))]
        [CreateAssetMenu(fileName = "ConditionalStunStatusDef", menuName = "Defs/Statuses/ConditionalStunStatus")]
        public class ConditionalStunStatusDef : StunStatusDef
        {
            public PassiveModifierAbilityDef ResistAbility;
            public LightStunStatusDef AlternativeStunDef; // <- should be a NoDaze/low-AP variant
        }

        // Class implementing the conditional swap
        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(StunStatus))]
        public class ConditionalStunStatus : StunStatus
        {
            public ConditionalStunStatusDef ConditionalStunStatusDef => this.Def<ConditionalStunStatusDef>();

            public override void ApplyEffect()
            {
                if (UnapplyRequested || ActorIsImmune(TacticalActorBase))
                    return;

                var actor = TacticalActor;
                if (actor == null)
                    return;

                var resist = ConditionalStunStatusDef.ResistAbility;
                var altDef = ConditionalStunStatusDef.AlternativeStunDef;

                if (resist != null && actor.GetAbilityWithDef<PassiveModifierAbility>(resist) != null && altDef != null)
                {
                   // TFTVLogger.Always($"applying light stun");
                    StatusComponent.ApplyStatus(ConditionalStunStatusDef.AlternativeStunDef);
                    RequestUnapply(StatusComponent); // discard the strong stun instance
                    return;
                }

                // Otherwise, do vanilla Stun behavior
                base.ApplyEffect();
                
            }

            // Keep the standard AP reduction for the strong case
            public new void ReduceActionPoints() => base.ReduceActionPoints();
        }

        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(TacStatusDef))]
        [CreateAssetMenu(fileName = "DesperateUseStatusDef", menuName = "Defs/Statuses/DesperateUseStatus")]
        public class DesperateUseStatusDef : TacStatusDef
        {
            [Header("Desperate Use")]
            public TacticalAbilityDef[] AllowedAbilities;            // explicit allow-list (optional but recommended)
            public GameTagDef[] AllowedTags;                 // OR allow by tag
            public TacStatusDef[] RequiredStatusesAll;               // optional: must have ALL
            public bool RequiresAnyActionPoint = true;           // true = must have AP > 0  

            [Header("Penalty")]
            public TacStatusDef AccuracyPenaltyStatus;

        }

        [Serializable]
        [SerializeType(InheritCustomCreateFrom = typeof(TacStatus))]
        public class DesperateUseStatus : TacStatus
        {
            public DesperateUseStatusDef DefDesperate => this.Def<DesperateUseStatusDef>();

            // Set by our Activate() prefix right before a desperate activation starts
            internal bool DesperateActivationInProgress { get; set; }

            public override void OnApply(StatusComponent statusComponent)
            {
                base.OnApply(statusComponent);

                if (TacticalActor == null)
                {
                    RequestUnapply(statusComponent);
                    return;
                }

                // Apply the accuracy penalty immediately upon gaining this status
                ApplyPenalty();

                // Listen for ability execution so we can finalize (spend leftover AP and clear penalty)
                TacticalActor.AbilityExecutedEvent += OnAbilityExecuted;
            }

            public override void OnUnapply()
            {
                if (TacticalActor != null)
                {
                    TacticalActor.AbilityExecutedEvent -= OnAbilityExecuted;
                }

                // Always remove the penalty if it’s still around
                RemovePenalty();

                base.OnUnapply();
            }

            public override void EndTurn()
            {
                // If player ends turn without using the desperate shot:
                RemovePenalty();
                RequestUnapply(StatusComponent);    // expire the enabling status
                base.EndTurn();
            }

            private void OnAbilityExecuted(TacticalAbility ability)
            {
                if (TacticalActor == null) return;

                // Only finalize if this execution actually used the bypass
                if (!DesperateActivationInProgress) return;

                // Spend whatever AP was left: clamp to 0
                TacticalActor.CharacterStats.ActionPoints.Set(0f);

                // Clear the transient penalty
                RemovePenalty();

                // Consume the enabling status
                RequestUnapply(TacticalActor.Status);

                DesperateActivationInProgress = false;
            }

            private void ApplyPenalty()
            {
                if (DefDesperate.AccuracyPenaltyStatus == null || TacticalActor == null) return;

                // Avoid duping the penalty if status is somehow re-applied
                var existing = TacticalActor.Status.GetStatus<TacStatus>(DefDesperate.AccuracyPenaltyStatus);
                if (existing == null)
                {
                    TacticalActor.Status.ApplyStatus(DefDesperate.AccuracyPenaltyStatus, DefDesperate);
                }
            }

            private void RemovePenalty()
            {
                if (DefDesperate.AccuracyPenaltyStatus == null || TacticalActor == null) return;

                var penalty = TacticalActor.Status.GetStatus<TacStatus>(DefDesperate.AccuracyPenaltyStatus);
                // Ensure we only remove the one that came from this status (if you reuse the same penalty elsewhere)
                if (penalty != null)
                {
                    penalty.RequestUnapply(TacticalActor.Status);
                }
            }

            // === Helpers used by patches ===

            internal bool Matches(TacticalAbility ability)
            {
                var d = DefDesperate;
                var adef = ability?.TacticalAbilityDef;
                if (adef == null) return false;

                bool inList = d.AllowedAbilities == null || d.AllowedAbilities.Length == 0 || d.AllowedAbilities.Contains(adef);

                // If it’s a ShootAbility and you configured weapon tags, enforce them
                bool tagMatch = true;
                if (ability is ShootAbility shootAbility)
                {
                    tagMatch = (d.AllowedTags == null || d.AllowedTags.Length == 0)
                               || d.AllowedTags.Any(tag => shootAbility.Weapon?.WeaponDef?.Tags?.Contains(tag) == true);
                }

                return inList && tagMatch;
            }

            internal bool RequirementsMet()
            {
                var d = DefDesperate;
                if (d.RequiresAnyActionPoint && (TacticalActor?.CharacterStats.ActionPoints.Value ?? 0f) <= 0f)
                    return false;

                if (d.RequiredStatusesAll != null && d.RequiredStatusesAll.Length > 0)
                {
                    foreach (var req in d.RequiredStatusesAll)
                    {
                        if (TacticalActor.Status.GetStatus<TacStatus>(req) == null)
                            return false;
                    }
                }
                return true;
            }
        }


    }
}
