using System;

namespace TFTV.BetterEnemies
{
    //This is copied and adapted from the original BetterEnemies mod by Dtony (all hail Dtony!)
    //https://github.com/dt22/BetterEnemies-Steam-Workshop (not last version)

    internal class BetterEnemiesMain
    {


        public static void Init()
        {
            try
            {
                AbilityChanges.Change_Abilities();
                AIActionDefs.Apply_AIActionDefs();
                ArthronsTritons.Change_ArthronsTritons();
                BetterAI.Change_AI();
                Missions.Change_Some_Missions();
                PerceptionAdjustments.Change_Perception(); 
                ResearchChanges.Create_ResearchRequirements();
                Scylla.Change_Queen();
                SirenChiron.Change_SirenChiron();
                SmallPandorans.Change_SmallCharactersAndSentinels();
                WillPower.Change_WillPower();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}

