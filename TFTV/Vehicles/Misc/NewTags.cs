using Base.Defs;
using PhoenixPoint.Common.Entities.GameTagsTypes;

namespace TFTVVehicleRework.Misc
{
    public static class NewTags
    {
		private static readonly DefRepository Repo = VehiclesMain.Repo;
        public static SkillTagDef get_EnterVehicleTag()
        {
            SkillTagDef EnterVehicleSkillTag = (SkillTagDef)Repo.GetDef("26ff2dd8-4e57-480d-bc22-1ac64f4f3872");
			if (EnterVehicleSkillTag == null)
			{
				EnterVehicleSkillTag = Repo.CreateDef<SkillTagDef>("26ff2dd8-4e57-480d-bc22-1ac64f4f3872");
				EnterVehicleSkillTag.name = "EnterVehicle_SkillTagDef";
				EnterVehicleSkillTag.ResourcePath = "Defs/GameTags/Actors/Skills/EnterVehicle_SkillTagDef";
			} 
			return EnterVehicleSkillTag;
        }

        public static SkillTagDef get_ExitVehicleTag()
        {
            SkillTagDef ExitVehicleSkillTag = (SkillTagDef)Repo.GetDef("e17308a6-dd9c-45c4-a623-9e74bb82f589");
			if (ExitVehicleSkillTag == null)
			{
				ExitVehicleSkillTag = Repo.CreateDef<SkillTagDef>("e17308a6-dd9c-45c4-a623-9e74bb82f589");
				ExitVehicleSkillTag.name = "ExitVehicle_SkillTagDef";
				ExitVehicleSkillTag.ResourcePath = "Defs/GameTags/Actors/Skills/ExitVehicle_SkillTagDef";
			} 
			return ExitVehicleSkillTag;
        }
    }
}