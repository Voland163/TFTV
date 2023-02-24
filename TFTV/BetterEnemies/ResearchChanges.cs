using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Entities.Research;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class ResearchChanges
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static void Create_ResearchRequirements()
            {
            try
            {
                ResearchDef crabGunResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanGunner_ResearchDef");
                ResearchDef crabBasicResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef");
                ResearchDef fishWretchResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanSneaker_ResearchDef");
                ResearchDef fishBasicResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanBasic_ResearchDef");
                ResearchDef fishFootpadResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanAssault_ResearchDef");
                ResearchDef fishPiercerAssault = DefCache.GetDef<ResearchDef>("ALN_FishmanPiercerAssault_ResearchDef");
                ResearchDef fishPiercerSniper = DefCache.GetDef<ResearchDef>("ALN_FishmanPiercerSniper_ResearchDef");
                ResearchDef FishThugAlpha = DefCache.GetDef<ResearchDef>("ALN_FishmanEliteStriker_ResearchDef");

                ResearchDef Chiron8 = DefCache.GetDef<ResearchDef>("ALN_Chiron8_ResearchDef");
                ResearchDef Chiron13 = DefCache.GetDef<ResearchDef>("ALN_Chiron13_ResearchDef");
                ResearchDef siren5 = DefCache.GetDef<ResearchDef>("ALN_Siren5_ResearchDef");

                crabGunResearch.InitialStates[4].State = ResearchState.Completed;
                fishWretchResearch.InitialStates[4].State = ResearchState.Completed;
                fishFootpadResearch.InitialStates[4].State = ResearchState.Completed;
                fishBasicResearch.Unlocks = new ResearchRewardDef[0];
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}
