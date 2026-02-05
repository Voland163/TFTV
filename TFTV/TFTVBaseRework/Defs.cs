using Base.Defs;
using Mono.Cecil;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVBaseRework
{
    internal class Defs
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        public static void CreateAndModifyDefs() 
        {
            try 
            {
                
                if(!BaseReworkUtils.BaseReworkEnabled)
                {
                    return;
                }
               
              
                DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [BionicsLab_PhoenixFacilityDef]").BaseResourcesOutput.Values = new List <ResourceUnit>
                {
                    new ResourceUnit()
                    {
                        Type = PhoenixPoint.Common.Core.ResourceType.Research,
                        Value = 2
                    },
                    
                };
                DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [ResearchLab_PhoenixFacilityDef]").BaseResourcesOutput.Values = new List<ResourceUnit>
                {
                    new ResourceUnit()
                    {
                        Type = PhoenixPoint.Common.Core.ResourceType.Research,
                        Value = 2
                    },

                };

                DefCache.GetDef<ResourceGeneratorFacilityComponentDef>("E_ResourceGenerator [FabricationPlant_PhoenixFacilityDef]").BaseResourcesOutput.Values = new List<ResourceUnit>
                {
                    new ResourceUnit()
                    {
                        Type = PhoenixPoint.Common.Core.ResourceType.Production,
                        Value = 2
                    },

                };

                ResearchDef havenRecruitingResearchDef = DefCache.GetDef<ResearchDef>("PX_HavenRecruits_ResearchDef");

                List <ResearchRewardDef> havenRecruitingUnlocks = havenRecruitingResearchDef.Unlocks.ToList();
                havenRecruitingUnlocks.Remove(
                    DefCache.GetDef<UnlockFunctionalityResearchRewardDef>("PX_HavenRecruits_ResearchDef_UnlockFunctionalityResearchRewardDef_0"));

                havenRecruitingResearchDef.Unlocks = havenRecruitingUnlocks.ToArray();

                DefCache.GetDef<ActivateBaseAbilityDef>("ActivateBaseAbilityDef").Cost = new ResourcePack() { };

                GeoscapeEventDef synFreeBaseEvent = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");
                foreach(GeoEventChoice geoEventChoice in synFreeBaseEvent.GeoscapeEventData.Choices)
                {
                    geoEventChoice.Outcome.ConvertSiteToPhoenixBase = false;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

    }
}
