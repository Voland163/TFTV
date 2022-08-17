using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVSmallChanges
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void ChangesToMedbay()
        {
            try
            {
                HealFacilityComponentDef e_HealMedicalBay_PhoenixFacilityDe = Repo.GetAllDefs<HealFacilityComponentDef>().FirstOrDefault(ged => ged.name.Equals("E_Heal [MedicalBay_PhoenixFacilityDef]"));
                e_HealMedicalBay_PhoenixFacilityDe.BaseHeal = 16;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChangesToHD()
        {
            try
            {
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {

                    if (missionTypeDef.name.Contains("Haven") && !missionTypeDef.name.Contains("Infestation"))
                    {
                        TacCrateDataDef cratesNotResources = Repo.GetAllDefs<TacCrateDataDef>().FirstOrDefault(ged => ged.name.Equals("Default_TacCrateDataDef"));
                        if (missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[1].MutualRelation = FactionRelation.Enemy;
                        }
                        else if (!missionTypeDef.name.Contains("Civ"))
                        {
                            missionTypeDef.ParticipantsRelations[2].MutualRelation = FactionRelation.Enemy;
                        }
                        missionTypeDef.ParticipantsData[1].PredeterminedFactionEffects = missionTypeDef.ParticipantsData[0].PredeterminedFactionEffects;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Max = 2;
                        missionTypeDef.ParticipantsData[1].ReinforcementsTurns.Min = 2;
                        missionTypeDef.ParticipantsData[1].InfiniteReinforcements = true;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Max = 0.5f;
                        missionTypeDef.ParticipantsData[1].ReinforcementsDeploymentPart.Min = 0.5f;
                        missionTypeDef.MissionSpecificCrates = cratesNotResources;
                        missionTypeDef.FactionItemsRange.Min = 2;
                        missionTypeDef.FactionItemsRange.Max = 7;
                        missionTypeDef.CratesDeploymentPointsRange.Min = 20;
                        missionTypeDef.CratesDeploymentPointsRange.Max = 30;
                        missionTypeDef.DontRecoverItems = true;
                        

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
