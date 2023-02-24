using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.BetterEnemies
{
    internal class PerceptionAdjustments
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        public static void Change_Perception()
        {
            try
            {
              //  BetterEnemiesConfig Config = (BetterEnemiesConfig)BetterEnemiesMain.Main.Config;
                TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef");

               // if (Config.AdjustHumanPerception == true)
               // {
               //     tacticalPerceptionDef.PerceptionRange = Config.Human_Soldier_Perception;
              //  }

                BodyPartAspectDef bodyPartAspectDef = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef2 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Assault_Helmet_BodyPartDef]");
                bodyPartAspectDef2.Perception = 2f;
                BodyPartAspectDef bodyPartAspectDef3 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Helmet_BodyPartDef]");
                bodyPartAspectDef3.Perception = 5f;
                bodyPartAspectDef3.WillPower = 2f;
                BodyPartAspectDef bodyPartAspectDef4 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Helmet_Viking_BodyPartDef]");
                bodyPartAspectDef4.WillPower = 2f;
                BodyPartAspectDef bodyPartAspectDef5 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Priest_Legs_ItemDef]");
                bodyPartAspectDef5.Perception = 2f;
                BodyPartAspectDef bodyPartAspectDef6 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Priest_Torso_BodyPartDef]");
                bodyPartAspectDef6.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef7 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef7.Perception = -2f;
                BodyPartAspectDef bodyPartAspectDef8 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [PX_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef8.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef9 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Shinobi_BIO_Helmet_BodyPartDef]");
                bodyPartAspectDef9.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef10 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Sniper_Helmet_BodyPartDef]");
                bodyPartAspectDef10.Perception = 4f;
                BodyPartAspectDef bodyPartAspectDef11 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [PX_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef11.Perception = 0f;
                BodyPartAspectDef bodyPartAspectDef12 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [IN_Heavy_Helmet_BodyPartDef]");
                bodyPartAspectDef12.Perception = -2f;
                BodyPartAspectDef bodyPartAspectDef13 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Watcher_Helmet_BodyPartDef]");
                bodyPartAspectDef13.Perception = 8f;
                BodyPartAspectDef bodyPartAspectDef14 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [SY_Infiltrator_Helmet_BodyPartDef]");
                bodyPartAspectDef14.Perception = 5f;
                TacticalItemDef styxHelmet = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Helmet_BodyPartDef");
                styxHelmet.BodyPartAspectDef.Perception = 5f;
                BodyPartAspectDef bodyPartAspectDef15 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [AN_Berserker_Watcher_Torso_BodyPartDef]");
                bodyPartAspectDef15.Perception = 3f;
                BodyPartAspectDef bodyPartAspectDef16 = DefCache.GetDef<BodyPartAspectDef>("E_BodyPartAspect [NJ_Exo_BIO_Helmet_BodyPartDef]");
                bodyPartAspectDef16.Perception = 3f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}
