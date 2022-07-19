using Base.Defs;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using System;

namespace TFTV
{
    internal class TFTVResources
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static bool ApplyChangeReduceResources = true;
        public static void Apply_Changes(float resourceMultiplier)
        {
            try
            {
                if (ApplyChangeReduceResources)
                {

                    foreach (GeoscapeEventDef geoEvent in Repo.GetAllDefs<GeoscapeEventDef>())
                    {
                        foreach (GeoEventChoice choice in geoEvent.GeoscapeEventData.Choices)
                        {
                            if (choice.Outcome.Resources != null && !choice.Outcome.Resources.IsEmpty)
                            {
                                choice.Outcome.Resources *= resourceMultiplier;                                     
                            }
                        }
                    }
                    ApplyChangeReduceResources = false;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }

}

