using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
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

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        [SerializeType(InheritCustomCreateFrom = typeof(AddAttackBoostStatusDef))]
        public class ShockDropStatusDef : AddAttackBoostStatusDef
        {
            public BashAbilityDef DefaultBashAbility;

            public BashAbilityDef ReplacementBashAbility;
        }

        [SerializeType(InheritCustomCreateFrom = typeof(AddAttackBoostStatus))]
        public class ShockDropStatus : AddAttackBoostStatus
        {
            private TacticalAbility _pendingAttack;

            private bool _bashAbilityReplaced;

            private bool _removedDefaultBash;

            private ShockDropStatusDef ShockDropDef => BaseDef as ShockDropStatusDef;

            public override void OnApply(StatusComponent statusComponent)
            {
                base.OnApply(statusComponent);

                if (TacticalActor == null || TacticalLevel == null)
                {
                    return;
                }

                TacticalLevel.AbilityActivatingEvent += OnAbilityActivating;
                TacticalLevel.AbilityExecutedEvent += OnAbilityExecuted;

                ReplaceBashAbility();
            }

            public override void OnUnapply()
            {
                if (TacticalLevel != null)
                {
                    TacticalLevel.AbilityActivatingEvent -= OnAbilityActivating;
                    TacticalLevel.AbilityExecutedEvent -= OnAbilityExecuted;
                }

                RestoreBashAbility();

                _pendingAttack = null;

                base.OnUnapply();
            }

            private void OnAbilityActivating(TacticalAbility ability, object parameter)
            {
                if (!Applied || ability == null || ability.TacticalActor != TacticalActor)
                {
                    return;
                }

                if (ability is JetJumpAbility || ability is IdleAbility)
                {
                    return;
                }

                if (ability is BashAbility || ability.TacticalAbilityDef.SkillTags.Contains(DefCache.GetDef<SkillTagDef>("MeleeAbility_SkillTagDef")))
                {
                    _pendingAttack = ability;
                    return;
                }

                ForfeitBonus();
            }

            private void OnAbilityExecuted(TacticalAbility ability, object parameter)
            {
                if (!Applied || ability == null || ability.TacticalActor != TacticalActor)
                {
                    return;
                }

                if (ability is JetJumpAbility || ability is IdleAbility)
                {
                    return;
                }

                if (ability == _pendingAttack)
                {
                    _pendingAttack = null;
                    RestoreBashAbility();
                    return;
                }

                if (ability is BashAbility || ability.TacticalAbilityDef.SkillTags.Contains(DefCache.GetDef<SkillTagDef>("MeleeAbility_SkillTagDef")))
                {
                    return;
                }

                ForfeitBonus();
            }

            private void ForfeitBonus()
            {
                if (!Applied || TacticalActor == null)
                {
                    return;
                }

                RemoveBonusKeywords();
                RestoreBashAbility();
                _pendingAttack = null;

                if (TacticalActor?.Status != null)
                {
                    RequestUnapply(TacticalActor.Status);
                }
            }

            private void RemoveBonusKeywords()
            {
                if (TacticalActor == null)
                {
                    return;
                }

                DamageKeywordPair[] keywordPairs = ShockDropDef?.DamageKeywordPairs;
                if (keywordPairs == null)
                {
                    return;
                }

                foreach (DamageKeywordPair keywordPair in keywordPairs)
                {
                    TacticalActor.RemoveDamageKeywordPair(keywordPair);
                }
            }

            private void ReplaceBashAbility()
            {
                if (_bashAbilityReplaced || TacticalActor == null)
                {
                    return;
                }

                ShockDropStatusDef statusDef = ShockDropDef;
                if (statusDef?.ReplacementBashAbility == null)
                {
                    return;
                }

                if (statusDef.DefaultBashAbility != null &&
                    TacticalActor.GetAbilityWithDef<BashAbility>(statusDef.DefaultBashAbility) != null)
                {
                    TacticalActor.RemoveAbility(statusDef.DefaultBashAbility);
                    _removedDefaultBash = true;
                }

                if (TacticalActor.GetAbilityWithDef<BashAbility>(statusDef.ReplacementBashAbility) == null)
                {
                    TacticalActor.AddAbility(statusDef.ReplacementBashAbility, TacticalActor);
                }

                _bashAbilityReplaced = true;
            }

            private void RestoreBashAbility()
            {
                if (!_bashAbilityReplaced || TacticalActor == null)
                {
                    return;
                }

                ShockDropStatusDef statusDef = ShockDropDef;
                if (statusDef == null)
                {
                    return;
                }

                if (statusDef.ReplacementBashAbility != null &&
                    TacticalActor.GetAbilityWithDef<BashAbility>(statusDef.ReplacementBashAbility) != null)
                {
                    TacticalActor.RemoveAbility(statusDef.ReplacementBashAbility);
                }

                if (_removedDefaultBash && statusDef.DefaultBashAbility != null &&
                    TacticalActor.GetAbilityWithDef<BashAbility>(statusDef.DefaultBashAbility) == null)
                {
                    TacticalActor.AddAbility(statusDef.DefaultBashAbility, TacticalActor);
                }

                _removedDefaultBash = false;
                _bashAbilityReplaced = false;
            }
        }




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

                TFTVLogger.Always($"StaticArmorTacStatsStatus: {TacticalActor?.DisplayName}");

                for (int i = 0; i < statsModifiers.Length; i++)
                {
                    


                    StatsModifierPopup statsModifierPopup = statsModifiers[i];

                    TFTVLogger.Always($"statsModifierPopup {statsModifierPopup.StatModification.StatName} {statsModifierPopup.StatModification.Value}");

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
               
                if(speed>0 && speed < 1)
                {
                    speed = 1;
                }

                TFTVLogger.Always($"{TacticalActor?.DisplayName} speed={speed}");

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
                    var actionPoints = TacticalActor.CharacterStats.ActionPoints;
                    float max = actionPoints.Max;
                    float floor = max * (1f - LightStunStatusDef.ActionPointsReduction);
                    float clamped = Mathf.Min(actionPoints.Value, floor);
                    actionPoints.Set(clamped);
                }

                if (StatusComponent != null)
                {
                    RequestUnapply(StatusComponent);
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

      


    }
}
