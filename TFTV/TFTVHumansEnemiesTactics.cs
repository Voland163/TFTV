using Base.Defs;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVHumansEnemiesTactics
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void ChampRecoverWPAura(TacticalLevelController controller)
        {
            try
            {
                List<TacticalFaction> enemyHumanFactions = TFTVHumanEnemies.GetHumanEnemyFactions(controller);
                if (enemyHumanFactions.Count != 0)
                {
                    foreach (TacticalFaction faction in enemyHumanFactions)
                    {
                        foreach (TacticalActorBase tacticalActorBase in faction.Actors)
                        {
                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (tacticalActorBase.HasGameTag(Repo.GetAllDefs<GameTagDef>().FirstOrDefault(p => p.name.Equals("HumanEnemyTier_2_GameTagDef"))))
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in faction.Actors)
                                {
                                    if (allyTacticalActorBase.BaseDef.name == "Soldier_ActorDef" && allyTacticalActorBase.InPlay)
                                    {
                                        TacticalActor actor = allyTacticalActorBase as TacticalActor;
                                        float magnitude = actor.GetAdjustedPerceptionValue();

                                        if ((allyTacticalActorBase.Pos - tacticalActorBase.Pos).magnitude < magnitude
                                            && TacticalFactionVision.CheckVisibleLineBetweenActors(allyTacticalActorBase, allyTacticalActorBase.Pos, tacticalActor, true))
                                        {
                                            TFTVLogger.Always("Actor in range and has LoS");
                                            actor.CharacterStats.WillPoints.AddRestrictedToMax(1);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}
