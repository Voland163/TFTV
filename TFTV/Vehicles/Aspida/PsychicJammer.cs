using Base.Defs;
using Base.UI;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Eventus;

namespace TFTVVehicleRework.Aspida
{
    public static class PsychicJammer
    {
        private static readonly DefRepository Repo = AspidaMain.Repo;
        public static void Change()
        {
            // "SY_Aspida_Psyshic_Jammer_GroundVehicleModuleDef"
            GroundVehicleModuleDef Jammer = (GroundVehicleModuleDef)Repo.GetDef("e75548ae-e9e5-28a4-1a67-78e7a1486e47");
            Jammer.Abilities = new AbilityDef[]
            {
                JammerAbility(),
            };
        }

        private static ApplyStatusAbilityDef JammerAbility()
        {
            //"NeuralDisruption_AbilityDef"
            ApplyStatusAbilityDef NeuralDisruption = (ApplyStatusAbilityDef)Repo.GetDef("e2ca912b-33e8-0eb4-e8ec-0e63126d9ac0");

            // E_TargetingData [MindCrush_AbilityDef]
            TacticalTargetingDataDef MC_Targeting = (TacticalTargetingDataDef)Repo.GetDef("7e7f3f7d-45f6-0a53-d82d-f4c5fdb6ac42");

            ApplyStatusAbilityDef Aspida_PsychicJammer = (ApplyStatusAbilityDef)Repo.CreateDef("5c0ba263-32e3-4300-b142-5ea2eeeb9417", NeuralDisruption);
            Aspida_PsychicJammer.name = "Aspida_PsychicJammer_AbilityDef";
            Aspida_PsychicJammer.CharacterProgressionData = null;
            Aspida_PsychicJammer.AnimType = -1;
            Aspida_PsychicJammer.WillPointCost = 0;
            Aspida_PsychicJammer.ActionPointCost = 0.5f;
            Aspida_PsychicJammer.ApplyStatusToAllTargets = true;
            Aspida_PsychicJammer.ShowNotificationOnUse = false;

            //"_Generic_AreaOfEffectSceneViewElementDef"
            SceneViewElementDef AOEScene = (SceneViewElementDef)Repo.GetDef("391d3adb-7329-8334-1bf2-8970f24b6184");
            
            //"ActorSilenced_StatusDef"
            TacStatusDef SilencedStatusDef = (TacStatusDef)Repo.GetDef("5bddb2fc-b1a4-c7c4-495f-4a544fd3dbcd");

            ActorHasStatusEffectConditionDef Aspida_NDCheck = Repo.CreateDef<ActorHasStatusEffectConditionDef>("c4d48488-2d8c-4345-ac94-d2c63e474b19");
            Aspida_NDCheck.StatusDef = SilencedStatusDef;
            Aspida_NDCheck.HasStatus = false;

            TacticalTargetingDataDef Jammer_Targeting = (TacticalTargetingDataDef)Repo.CreateDef("4d51542c-d8e4-4685-ba98-ca6ec5d2fa54", MC_Targeting);
            Jammer_Targeting.name = "TargetingData [Aspida_PsychicJammer_AbilityDef]";
            Jammer_Targeting.Origin.Range = 12f;

            Aspida_PsychicJammer.TargetingDataDef = Jammer_Targeting;
            Aspida_PsychicJammer.SceneViewElementDef = AOEScene;
            Aspida_PsychicJammer.TargetApplicationConditions = new EffectConditionDef[]
            {
                Aspida_NDCheck,
            };

            //"Acheron_RestoreArmorCast_ParticleEventDef"
            Aspida_PsychicJammer.EventOnActivate = (TacticalEventDef)Repo.GetDef("6253e5de-a558-73e4-f8a6-970f49af1dc1");
            
            Aspida_PsychicJammer.ViewElementDef = VED(NeuralDisruption.ViewElementDef);
            return Aspida_PsychicJammer;
        }

        private static TacticalAbilityViewElementDef VED(TacticalAbilityViewElementDef template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("ae5b3209-d195-455a-bfc2-aa3eaabfd4bb");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("ae5b3209-d195-455a-bfc2-aa3eaabfd4bb", template);
                VED.name = "E_View [Aspida_PsychicJammer_AbilityDef]";
                VED.Description = new LocalizedTextBind("SY_JAMMER_DESC");
                VED.ShowInInventoryItemTooltip = true;
            }
            return VED;
        }
    }
}