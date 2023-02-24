using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class Missions
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static void Change_Some_Missions()
        {
            try
            {
                CustomMissionTypeDef px14 = DefCache.GetDef<CustomMissionTypeDef>("StoryPX14_CustomMissionTypeDef");
                CustomMissionTypeDef px1 = DefCache.GetDef<CustomMissionTypeDef>("StoryPX1_CustomMissionTypeDef");
                CustomMissionTypeDef px15 = DefCache.GetDef<CustomMissionTypeDef>("StoryPX15_CustomMissionTypeDef");
                ApplyStatusAbilityDef coCorruption = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_CoCorruption_AbilityDef");
                TacCharacterDef pool = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_TacCharacterDef");
                TacCharacterDef node = DefCache.GetDef<TacCharacterDef>("CorruptionNode_TacCharacterDef");


                pool.Data.Abilites = new TacticalAbilityDef[]
                {
                coCorruption,
                };

                node.Data.Abilites = new TacticalAbilityDef[]
                {
                coCorruption,
                };

                px14.IsAiAlertedInitially = true;
                px1.IsAiAlertedInitially = true;
                px15.IsAiAlertedInitially = true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
    }
}
