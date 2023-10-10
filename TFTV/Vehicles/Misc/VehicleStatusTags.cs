using Base.Defs;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Tactical.Entities;

namespace TFTVVehicleRework.Misc
{
    public static class VehicleStatusTags
    {
        private static readonly DefRepository Repo = VehiclesMain.Repo;
        public static void GiveTags()
        {
            //"Empty_VehicleStatusTagDef"
			VehicleStatusTagDef EmptyVehicleStatusTag = (VehicleStatusTagDef)Repo.GetDef("0f9b805c-6b93-c604-69f0-592cf925c506");
			//"HasPassengers_VehicleStatusTagDef"
			VehicleStatusTagDef HasPassengersStatusTag = (VehicleStatusTagDef)Repo.GetDef("49ba189e-950a-1cd4-d93e-6e64f8b23860");

			foreach (VehicleComponentDef VehicleComponent in Repo.GetAllDefs<VehicleComponentDef>())
			{
				if (VehicleComponent.HasDoor)
				{
					VehicleComponent.EmptyVehicleTag = EmptyVehicleStatusTag;
					VehicleComponent.HasPassengersTag = HasPassengersStatusTag;
				}
			}
        }
    }
}