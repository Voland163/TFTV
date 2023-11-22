using Base.Defs;
using Base.UI;
using Base.Entities.Abilities;
using Base.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Eventus;

namespace TFTVVehicleRework.Aspida
{
    public static class PsychicJammer
    {
        private static readonly DefRepository Repo = AspidaMain.Repo;
        private static readonly SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;
        public static void Change()
        {
            // "SY_Aspida_Psyshic_Jammer_GroundVehicleModuleDef"
            GroundVehicleModuleDef Jammer = (GroundVehicleModuleDef)Repo.GetDef("e75548ae-e9e5-28a4-1a67-78e7a1486e47");
            Jammer.ViewElementDef.DisplayName1 = new LocalizedTextBind("SY_NAVMODULE_NAME");
            Jammer.Abilities = new AbilityDef[]
            {
                // JammerAbility(),
                LocatorAbility(),
                RepelMist(),
                MistAura()
            };
        }

        private static ApplyStatusAbilityDef LocatorAbility()
        {
            //"MotionDetection_AbilityDef" -> template ability
            ApplyStatusAbilityDef MotionDetection = (ApplyStatusAbilityDef)Repo.GetDef("636ba84c-7481-0544-79c1-822813b214cd");
            ApplyStatusAbilityDef AspidaMotionDetector = Repo.CreateDef<ApplyStatusAbilityDef>("2c011787-bba8-4eca-bbdf-73e352032b28", MotionDetection);
            AspidaMotionDetector.name = "AspidaMotionDetection_AbilityDef";

            TacticalTargetingDataDef DetectorTargeting = Repo.CreateDef<TacticalTargetingDataDef>("9df5838e-cd5e-4aa0-a315-c0de7b86c69c", MotionDetection.TargetingDataDef);
            DetectorTargeting.name = "E_TargetingDataDef [AspidaMotionDetection_AbilityDef]";
            DetectorTargeting.Origin.Range = 20f;

            TacticalAbilityViewElementDef DetectorVED = Repo.CreateDef<TacticalAbilityViewElementDef>("b6a3a3c9-0cca-4dfa-8193-92da3ba9f8d0", MotionDetection.ViewElementDef);
            DetectorVED.name = "E_ViewElement [AspidaMotionDetection_AbilityDef]";
            DetectorVED.DisplayName1 = new LocalizedTextBind("SY_DETECTION_NAME");
            DetectorVED.Description = new LocalizedTextBind("SY_DETECTION_DESC");

            AspidaMotionDetector.TargetingDataDef = DetectorTargeting;
            AspidaMotionDetector.ViewElementDef = DetectorVED;
            return AspidaMotionDetector;
        }

        private static ApplyEffectAbilityDef RepelMist()
        {
            //"MistRepeller_AbilityDef"
            ApplyEffectAbilityDef MistRepeller = (ApplyEffectAbilityDef)Repo.GetDef("f9349490-72e5-5784-0b76-af8f403162a5");        
            MistRepeller.EffectDef.ApplicationConditions = new EffectConditionDef[]
            {
                MistRepellerCondition()
            };
            MistRepeller.CheckApplicationConditions = true;

            ApplyEffectAbilityDef AspidaMistRepeller = Repo.CreateDef<ApplyEffectAbilityDef>("91b48569-27c8-4461-aabd-4aa10100a4dd", MistRepeller);
            AspidaMistRepeller.name = "AspidaMistRepeller_AbilityDef";
            AspidaMistRepeller.ApplyOnStartTurn = true;

            RemoveTacticalVoxelEffectDef RemoveMistEffect = Repo.CreateDef<RemoveTacticalVoxelEffectDef>("dbf7384c-2f45-44fa-a58e-084d13d9143f", MistRepeller.EffectDef);
            RemoveMistEffect.name = "E_RemoveMistEffect [AspidaMistRepeller_AbilityDef]";
            RemoveMistEffect.Radius = 5f;

            TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("54528190-d45c-433f-955f-ed9ba159d163", MistRepeller.ViewElementDef);
            VED.name = "E_ViewElementDef [AspidaMistRepeller_AbilityDef]";
            VED.DisplayName1 = new LocalizedTextBind("SY_MISTREPELLER_NAME");
            VED.Description = new LocalizedTextBind("SY_MISTREPELLER_DESC");

            AspidaMistRepeller.ViewElementDef = VED;
            AspidaMistRepeller.EffectDef = RemoveMistEffect;
            return AspidaMistRepeller;
        }

        private static ActorHasStatusEffectConditionDef MistRepellerCondition()
        {
            ActorHasStatusEffectConditionDef Condition = Repo.CreateDef<ActorHasStatusEffectConditionDef>("41d5807b-cb97-4951-aa1a-fbc0c77ad668");
            Condition.name = "ActorIsOnMap_EffectConditionDef";
            Condition.StatusDef = (TacStatusDef)Repo.GetDef("d91ab356-3acc-4204-ca48-46da05874bb0"); //"OffMap_StatusDef"
            Condition.HasStatus = false;
            return Condition;
        }

        private static ApplyStatusAbilityDef MistAura()
        {
            //"ApplyStatus_MindControlImmunity_AbilityDef"
            ApplyStatusAbilityDef MCAura = (ApplyStatusAbilityDef)Repo.GetDef("cf5b7bba-e467-7aa4-88e4-1dd54d24f630");

            ApplyStatusAbilityDef MistAura = Repo.CreateDef<ApplyStatusAbilityDef>("67bf9486-82ed-4fb6-b642-991c84b976d6", MCAura);
            MistAura.name = "AspidaMistImmunityAura_AbilityDef";
            MistAura.CanApplyToOffMapTarget = true;
            MistAura.TargetApplicationConditions = new EffectConditionDef[]{};
            
            TacticalTargetingDataDef AuraTargeting = Repo.CreateDef<TacticalTargetingDataDef>("3fe70fec-de93-4135-b1af-ff12fcaf36d5", MCAura.TargetingDataDef);
            AuraTargeting.Origin.Range = 3.5f;
            AuraTargeting.Origin.TargetTags = new GameTagsList();
            AuraTargeting.Target.Range = 4.1f;

            TacticalAbilityViewElementDef AuraVED = Repo.CreateDef<TacticalAbilityViewElementDef>("043fde66-85cd-4db8-88db-4ae943536446", MCAura.ViewElementDef);
            AuraVED.name = "E_ViewElement [AspidaMistImmunityAura_AbilityDef]";
            AuraVED.DisplayName1 = new LocalizedTextBind("SY_MISTIMMUNITY_NAME");
            AuraVED.Description = new LocalizedTextBind("SY_MISTIMMUNITY_DESC");
            AuraVED.SmallIcon = AuraVED.LargeIcon = keywords.MistKeyword.Visuals.SmallIcon;
            AuraVED.ShowInInventoryItemTooltip = false;

            AreaOfEffectAbilitySceneViewDef AspidaScene = Repo.CreateDef<AreaOfEffectAbilitySceneViewDef>("5443d9fc-baca-4898-87c0-5614c5644c83", MCAura.SceneViewElementDef);
            AspidaScene.UseOriginData = false;

            //"MistResistance_StatusDef"
            DamageMultiplierStatusDef MistResistance = (DamageMultiplierStatusDef)Repo.GetDef("9126e1d4-3c31-8934-e90c-1c3d6ddabd92");
            DamageMultiplierStatusDef MistImmunity = Repo.CreateDef<DamageMultiplierStatusDef>("", MistResistance);
            MistImmunity.name = "MistImmunity_StatusDef";
            MistImmunity.EffectName = "MistImmunity";
            MistImmunity.Visuals = AuraVED;
            MistImmunity.Multiplier = 0.0f;
            MistImmunity.SingleInstance = true;
            MistImmunity.ShowNotification = true;
            MistImmunity.VisibleOnPassiveBar = true;
            MistImmunity.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;

            MistAura.TargetingDataDef = AuraTargeting;
            MistAura.StatusDef = MistImmunity;
            MistAura.SceneViewElementDef = AspidaScene;
            MistAura.ViewElementDef = AuraVED;
            return MistAura;
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
            // Aspida_PsychicJammer.EventOnActivate = (TacticalEventDef)Repo.GetDef("6253e5de-a558-73e4-f8a6-970f49af1dc1");

            //"Priest_PsychicScream_ParticleEventDef"
            TacticalEventDef PsyScream = (TacticalEventDef)Repo.GetDef("b09f4229-0538-2364-b9ae-e915011f2e8c");
            TacticalEventDef AspidaScream = Repo.CreateDef<TacticalEventDef>("7297fc24-035a-40e3-85fd-bf70a3834cd6", PsyScream);
            AspidaScream.EventTransformName = "Root";
            Aspida_PsychicJammer.EventOnActivate = AspidaScream;
            
            
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