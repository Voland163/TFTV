using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;

namespace TFTVVehicleRework.Misc 
{
    public static class RescueMissions
    {
        private static readonly DefRepository Repo = VehiclesMain.Repo;
        private static readonly ClassTagDef BuggyClassTag = (ClassTagDef)Repo.GetDef("27ca9059-9047-5df4-695b-9e4301407045"); //"KaosBuggy_ClassTagDef"

        public static void GenerateMissions()
        {
            //Standard map
            CustomMissionTypeDef ScavVRescueALN = (CustomMissionTypeDef)Repo.GetDef("8812521d-079c-b154-6b4a-bcd98638b439");
            CustomMissionTypeDef ScavVRescueAN = (CustomMissionTypeDef)Repo.GetDef("678600ce-ea8c-eaa4-4880-d85e0dae425c");
            CustomMissionTypeDef ScavVRescueBAN = (CustomMissionTypeDef)Repo.GetDef("ab2858c7-fb8a-4dd4-aa6c-378f6323422d");
            CustomMissionTypeDef ScavVRescueFSK = (CustomMissionTypeDef)Repo.GetDef("af99dc70-6cd0-f814-fbf2-bd4cfce998c4");
            CustomMissionTypeDef ScavVRescueNJ = (CustomMissionTypeDef)Repo.GetDef("7c5835e9-3e14-b214-da9b-fa7a49dc7185");
            CustomMissionTypeDef ScavVRescuePUR = (CustomMissionTypeDef)Repo.GetDef("c5fabf23-7f76-5304-48d4-0a8b17b5b7d6");
            CustomMissionTypeDef ScavVRescueSY = (CustomMissionTypeDef)Repo.GetDef("1ca759d2-7647-8184-d835-c3b57dd1799f");
            //Overgrown map
            CustomMissionTypeDef OScavVRescueALN = (CustomMissionTypeDef)Repo.GetDef("e6bad4b1-44e0-9614-1b26-d3ea2a70913f");
            CustomMissionTypeDef OScavVRescueAN = (CustomMissionTypeDef)Repo.GetDef("5ef6f8ea-2553-0984-18ae-4de69d36831e");
            CustomMissionTypeDef OScavVRescueBAN = (CustomMissionTypeDef)Repo.GetDef("53ad686e-30cd-4154-683f-e7d620020dd8");
            CustomMissionTypeDef OScavVRescueFSK = (CustomMissionTypeDef)Repo.GetDef("6b016bcd-b358-be54-a8b9-6e8af18f9de9");
            CustomMissionTypeDef OScavVRescueNJ = (CustomMissionTypeDef)Repo.GetDef("bd0f76dc-f9d0-7624-8965-f5abfb7edafa");
            CustomMissionTypeDef OScavVRescuePUR = (CustomMissionTypeDef)Repo.GetDef("83ac39bf-d36f-7f64-78a2-646dabc4f859");
            CustomMissionTypeDef OScavVRescueSY = (CustomMissionTypeDef)Repo.GetDef("7a6b2d8b-9da7-e724-1a6e-354affe2ad98");

            CustomMissionTypeDef BuggyRescueALN = Repo.CreateDef<CustomMissionTypeDef>("45705909-da42-4280-8e0e-aabdec4e9d6d",ScavVRescueALN);
            BuggyRescueALN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescueAN = Repo.CreateDef<CustomMissionTypeDef>("17dc6be1-d2a0-4906-9804-9b2f2c263396", ScavVRescueAN);
            BuggyRescueAN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescueBAN = Repo.CreateDef<CustomMissionTypeDef>("36cef83d-bd0e-4755-8861-1bf46cc46f90", ScavVRescueBAN);
            BuggyRescueBAN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescueFSK = Repo.CreateDef<CustomMissionTypeDef>("50f74682-d881-47c0-a774-ec7f773afc50", ScavVRescueFSK);
            BuggyRescueFSK.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescueNJ = Repo.CreateDef<CustomMissionTypeDef>("1072c7ba-0cdf-4419-8ff5-ba04fdfa2e59", ScavVRescueNJ);
            BuggyRescueNJ.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescuePUR = Repo.CreateDef<CustomMissionTypeDef>("be580c2c-42c8-4688-8267-3d10a28fa08e", ScavVRescuePUR);
            BuggyRescuePUR.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef BuggyRescueSY = Repo.CreateDef<CustomMissionTypeDef>("178c9a70-e48e-4253-8e37-a104bd3098e6", ScavVRescueSY);
            BuggyRescueSY.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;

            CustomMissionTypeDef OBuggyRescueALN = Repo.CreateDef<CustomMissionTypeDef>("018209be-541c-4d27-a364-0d6964c54ea7", OScavVRescueALN);
            OBuggyRescueALN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescueAN = Repo.CreateDef<CustomMissionTypeDef>("93c28a8b-2188-404b-8319-ad44c96c11bd", OScavVRescueAN);
            OBuggyRescueAN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescueBAN = Repo.CreateDef<CustomMissionTypeDef>("1ddcd08b-2ff1-4291-8e13-32f176aad7b4", OScavVRescueBAN);
            OBuggyRescueBAN.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescueFSK = Repo.CreateDef<CustomMissionTypeDef>("09139df4-af96-44bf-9492-113f07e46921", OScavVRescueFSK);
            OBuggyRescueFSK.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescueNJ = Repo.CreateDef<CustomMissionTypeDef>("c54a3658-e793-4e93-ba0d-a854840c5cef", OScavVRescueNJ);
            OBuggyRescueNJ.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescuePUR = Repo.CreateDef<CustomMissionTypeDef>("543cb2cc-0465-4e81-b5d5-0a3677b8a0b2", OScavVRescuePUR);
            OBuggyRescuePUR.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
            CustomMissionTypeDef OBuggyRescueSY = Repo.CreateDef<CustomMissionTypeDef>("c62af9be-3f6f-4ac9-b90e-99e6c95f5800", OScavVRescueSY);
            OBuggyRescueSY.ParticipantsData[1].ActorDeployParams[0].Limit.ActorTag = BuggyClassTag;
        }

        public static void Fix_BuggyDeploymentTemplate()
        {
            //"KS_Kaos_Buggy_CharacterTemplateDef"
            TacCharacterDef BuggyCharacterDef = (TacCharacterDef)Repo.GetDef("147c1dfa-411a-4114-4b3c-5a2e1cfcd1d2");
            BuggyCharacterDef.Data.Name = "JUNKER";
            BuggyCharacterDef.Data.GameTags = BuggyCharacterDef.Data.GameTags.AddToArray(BuggyClassTag);
            BuggyCharacterDef.DeploymentCost = 500;

            //"Neutral_GeoFactionDef"
            GeoFactionDef NeutralFaction = (GeoFactionDef)Repo.GetDef("df7a40b4-369b-a054-6af0-31b76dbaf312");
            NeutralFaction.StartingUnits = NeutralFaction.StartingUnits.AddToArray(BuggyCharacterDef);
        }
    }
}