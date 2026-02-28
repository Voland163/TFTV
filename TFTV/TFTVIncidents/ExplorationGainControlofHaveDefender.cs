using Base.Defs;
using Base.Serialization.General;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVIncidents
{
    internal class ExplorationGainControlofHaveDefender
    {
        [CreateAssetMenu(fileName = "HavenDefenseSupportAbilityDef", menuName = "Defs/Abilities/Tactical/HavenDefenseSupportAbility")]
        [SerializeType(InheritCustomCreateFrom = typeof(TacticalAbilityDef))]
        public class HavenDefenseSupportAbilityDef : TacticalAbilityDef
        {

            [Header("Haven Defense Support")]
            public int WillPointsOnCivilianExtract = 1;
        }

        public class HavenDefenseSupportAbility : TacticalAbility
        {
           
            public HavenDefenseSupportAbilityDef HavenDefenseSupportAbilityDef
            {
                get
                {
                    return this.Def<HavenDefenseSupportAbilityDef>();
                }
            }


            public override void AbilityAdded()
            {
                base.AbilityAdded();
                base.TacticalActorBase.TacticalLevel.NewTurnEvent += this.OnNewTurn;
                base.TacticalActorBase.TacticalLevel.ActorExitedPlayEvent += this.OnActorExitedPlay;
            }


            public override void AbilityRemovingStart()
            {
                base.AbilityRemovingStart();
                if (base.TacticalActorBase == null || base.TacticalActorBase.TacticalLevel == null)
                {
                    return;
                }
                base.TacticalActorBase.TacticalLevel.NewTurnEvent -= this.OnNewTurn;
                base.TacticalActorBase.TacticalLevel.ActorExitedPlayEvent -= this.OnActorExitedPlay;
            }


            private void OnNewTurn(TacticalFaction previousFaction, TacticalFaction nextFaction)
            {
                if (this._startBonusApplied || nextFaction.ParticipantKind != TacMissionParticipant.Player || !this.IsPrimaryProvider() || !this.IsHavenDefenseMission())
                {
                    return;
                }
                this._startBonusApplied = true;
                this.TakeControlOfHavenDefender(nextFaction);
            }


            private void OnActorExitedPlay(TacticalActorBase actor)
            {
                if (!this.IsPrimaryProvider() || !this.IsHavenDefenseMission() || !this.IsExtractedCivilian(actor))
                {
                    return;
                }
                this.GrantWillPointsToPlayerOperatives();
            }


            private void TakeControlOfHavenDefender(TacticalFaction playerFaction)
            {
                ClassTagDef civilianTag = CommonHelpers.GetSharedGameTags().CivilianTag;
                IEnumerable<TacticalActor> source = from a in base.TacticalActorBase.TacticalLevel.Map.GetActors<TacticalActor>(null)
                                                    where a.IsAlive && a.InPlay && a.TacticalFaction != null && a.TacticalFaction.ParticipantKind == TacMissionParticipant.Residents && !a.GameTags.Contains(civilianTag)
                                                    select a;
                TacticalActor tacticalActor = source.FirstOrDefault<TacticalActor>();
                if (tacticalActor == null)
                {
                    return;
                }
                tacticalActor.SetFaction(playerFaction, TacMissionParticipant.Player);
            }


            private void GrantWillPointsToPlayerOperatives()
            {
                TacticalFaction tacticalFaction = base.TacticalActorBase.TacticalLevel.GetFactionByCommandName("player");
                if (tacticalFaction == null)
                {
                    return;
                }
                foreach (TacticalActor tacticalActor in from a in base.TacticalActorBase.TacticalLevel.Map.GetActors<TacticalActor>(null)
                                                        where a.InPlay && a.IsAlive && a.TacticalFaction == tacticalFaction
                                                        select a)
                {
                    tacticalActor.CharacterStats.WillPoints.AddRestrictedToMax((float)this.HavenDefenseSupportAbilityDef.WillPointsOnCivilianExtract);
                }
            }


            private bool IsPrimaryProvider()
            {
                if (base.TacticalActorBase == null || base.TacticalActorBase.TacticalFaction == null)
                {
                    return false;
                }
                List<TacticalActor> list = (from a in base.TacticalActorBase.TacticalLevel.Map.GetActors<TacticalActor>(null)
                                            where a.TacticalFaction == base.TacticalActorBase.TacticalFaction && a.GetAbility<HavenDefenseSupportAbility>() != null
                                            orderby a.GetInstanceID()
                                            select a).ToList<TacticalActor>();
                return list.Count > 0 && list[0] == base.TacticalActor;
            }

            private bool IsHavenDefenseMission()
            {
                if (base.TacticalActorBase?.TacticalLevel?.TacMission?.MissionData?.MissionType == null)
                {
                    return false;
                }
                return base.TacticalActorBase.TacticalLevel.TacMission.MissionData.MissionType.MissionTypeTag == CommonHelpers.GetSharedGameTags().HavenDefenseMissionTag;
            }


            private bool IsExtractedCivilian(TacticalActorBase actor)
            {
                if (!(actor is TacticalActor tacticalActor) || !tacticalActor.IsEvacuated)
                {
                    return false;
                }
                ClassTagDef civilianTag = CommonHelpers.GetSharedGameTags().CivilianTag;
                return tacticalActor.GameTags.Contains(civilianTag);
            }

            private bool _startBonusApplied;
        }

    }
}
