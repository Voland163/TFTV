using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class WillPower
    {
      //  private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
          private static readonly DefRepository Repo = TFTVMain.Repo;

        //  private static readonly SharedData Shared = TFTVMain.Shared;
        public static void Change_WillPower()
        {
            foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Equals("Crabman12_EliteShielder_AlienMutationVariationDef") || a.name.Equals("Crabman12_EliteShielder2_AlienMutationVariationDef")
            || a.name.Equals("Crabman15_UltraShielder_AlienMutationVariationDef")))
            {
                character.Data.Will -= 4;
            }

            foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Equals("Crabman12_EliteShielder3_AlienMutationVariationDef")))
            {
                character.Data.Will -= 2;
            }

            foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && a.name.Contains("Pretorian")))
            {
                character.Data.Will -= 5;
            }

            foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Crabman") && (a.name.Contains("EliteViralCommando") || a.name.Contains("UltraViralCommando"))))
            {
                character.Data.Will -= 5;
            }

            foreach (TacCharacterDef crabMyr in Repo.GetAllDefs<TacCharacterDef>().Where(aad => aad.name.Contains("Crabman") && (aad.name.Contains("EliteRanger") || aad.name.Contains("UltraRanger"))))
            {
                crabMyr.Data.Will -= 5;
            }

            foreach (TacCharacterDef character in Repo.GetAllDefs<TacCharacterDef>().Where(a => a.name.Contains("Fishman")))
            {
                character.Data.Will -= 5;
            }
        }
    }
}
