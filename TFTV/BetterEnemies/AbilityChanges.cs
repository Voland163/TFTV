using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;

namespace TFTV.BetterEnemies
{
    internal class AbilityChanges
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //  private static readonly DefRepository Repo = TFTVMain.Repo;

        //  private static readonly SharedData Shared = TFTVMain.Shared;
        public static void Change_Abilities()
        {
            Clone_GuardianBeam();

        }

        public static void Clone_GuardianBeam()
        {
            try
            {
                string skillName = "BE_Guardian_Beam_ShootAbilityDef";
                ShootAbilityDef source = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");
                ShootAbilityDef BEGB = Helper.CreateDefFromClone(
                    source,
                    "cfc8f607-2dac-40e3-bdfb-842f7e1ce71c",
                    skillName);
                BEGB.SceneViewElementDef = Helper.CreateDefFromClone(
                    source.SceneViewElementDef,
                   "0bdef0ee-7070-4d21-972e-b2d1f07710ae",
                   skillName);
                BEGB.TargetingDataDef = Helper.CreateDefFromClone(
                    source.TargetingDataDef,
                   "be53f499-9627-44b3-9cd8-87410b51f008",
                   skillName);


                BEGB.UsesPerTurn = 1;
                BEGB.TrackWithCamera = false;
                BEGB.ShownModeToTrack = PhoenixPoint.Tactical.Levels.KnownState.Revealed;
                ShootAbilitySceneViewDef guardianBeamSVE = (ShootAbilitySceneViewDef)BEGB.SceneViewElementDef;
                guardianBeamSVE.HoverMarkerInvalidTarget = PhoenixPoint.Tactical.View.GroundMarkerType.AttackConeNoTarget;
                guardianBeamSVE.LineToCursorInvalidTarget = PhoenixPoint.Tactical.View.GroundMarkerType.AttackLineNoTarget;
                guardianBeamSVE.HoverMarker = PhoenixPoint.Tactical.View.GroundMarkerType.AttackCone;
                BEGB.TargetingDataDef = DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Queen_GunsFire_ShootAbilityDef]");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
    }
}
