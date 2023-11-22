using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.UI;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Eventus;
using PRMBetterClasses;
using TFTVVehicleRework.Effects;
using UnityEngine;

namespace TFTVVehicleRework.Aspida
{
    public static class ClericX2
    {
        private static readonly DefRepository Repo = AspidaMain.Repo;
        public static void Change()
        {
            // "SY_Aspida_Experimental_Thrusters_Technology_Engine_GroundVehicleModuleDef"
            GroundVehicleModuleDef ExpThrusters = (GroundVehicleModuleDef)Repo.GetDef("4a17f11d-9227-d064-d843-875e8b2bc296");
            ExpThrusters.Abilities = new AbilityDef[] 
            {
                CureSprayCloud(),
                RestoreArmour()
            };
            ExpThrusters.ViewElementDef.DisplayName1 = new LocalizedTextBind("SY_THRUSTERS_NAME");
        }

        public static ApplyEffectAbilityDef CureSprayCloud()
        {
            // "Acheron_CureCloud_ApplyEffectAbilityDef"
            ApplyEffectAbilityDef CureCloud = (ApplyEffectAbilityDef)Repo.GetDef("dba1a2a5-39de-2294-6877-1f4296038057");

            ApplyEffectAbilityDef CureSprayCloud = Repo.CreateDef<ApplyEffectAbilityDef>("2c50ada9-7f6b-462f-86c1-3f10e6db199b", CureCloud);
            CureSprayCloud.name = "Aspida_CureCloud_AbilityDef";
            CureSprayCloud.WillPointCost = 0f;
            CureSprayCloud.ActionPointCost = 0.25f;
            CureSprayCloud.UsesPerTurn = 1;
            CureSprayCloud.AnimType = -1;
            CureSprayCloud.EffectDef = CureEffects();
            CureSprayCloud.CheckApplicationConditions = true;
            CureSprayCloud.EventOnActivate = CureParticleEffects();
            
            TacticalTargetingDataDef CureSprayTargeting = Repo.CreateDef<TacticalTargetingDataDef>("1d0b2e39-ff71-4b86-b943-f73769b2f8ba", CureCloud.TargetingDataDef);
            CureSprayTargeting.Origin.LineOfSight = LineOfSightType.Ignore;
            CureSprayTargeting.Target.Range = 11.5f;

            CureSprayCloud.TargetingDataDef = CureSprayTargeting;
            CureSprayCloud.ViewElementDef = Cure_VED(CureCloud.ViewElementDef);
            CureSprayCloud.SceneViewElementDef = AspidaMain.AspidaSceneView();
            return CureSprayCloud;
        }

        private static ApplyEffectAbilityDef RestoreArmour()
        {
            //"Acheron_RestorePandoranArmor_AbilityDef"
            ApplyEffectAbilityDef RestoreArmour = (ApplyEffectAbilityDef)Repo.GetDef("65356940-ac77-01a4-690f-ea9e3aef7c03");

            ApplyEffectAbilityDef Aspida_RestoreArmour = (ApplyEffectAbilityDef)Repo.CreateDef("19b49bf8-2d85-43cb-be38-1b2694e7182b", RestoreArmour);
            Aspida_RestoreArmour.name = "Aspida_RestoreArmour_AbilityDef";
            Aspida_RestoreArmour.WillPointCost = 0;
            Aspida_RestoreArmour.ActionPointCost = 0.25f;
            Aspida_RestoreArmour.UsesPerTurn = 1;
            Aspida_RestoreArmour.AnimType = -1;
            Aspida_RestoreArmour.EffectDef = RestoreArmourEffect(RestoreArmour.EffectDef);
            Aspida_RestoreArmour.CheckApplicationConditions = true;

            TacticalTargetingDataDef Aspida_RA_Target = (TacticalTargetingDataDef)Repo.CreateDef("d8123e9e-f62d-41fd-9b16-b6e0262eedcf",RestoreArmour.TargetingDataDef);
            Aspida_RA_Target.name = "E_TargetingData [Aspida_RestoreArmour_AbilityDef]";
            Aspida_RA_Target.Origin.TargetTags = new GameTagsList{};
            Aspida_RA_Target.Origin.LineOfSight = LineOfSightType.Ignore;
            Aspida_RA_Target.Target.Range = 11.5f;

            Aspida_RestoreArmour.EventOnActivate = RestoreArmourParticleEffects();
            Aspida_RestoreArmour.TargetingDataDef = Aspida_RA_Target;
            Aspida_RestoreArmour.ViewElementDef = Armour_VED();
            Aspida_RestoreArmour.SceneViewElementDef = AspidaMain.AspidaSceneView();
            return Aspida_RestoreArmour;
        }

        private static TacticalEventDef CureParticleEffects()
        {
            // "Acheron_SpawnCureCloudParticle_EventDef"
            TacticalEventDef CureCloudParticle = (TacticalEventDef)Repo.GetDef("5e88964b-c4bd-fa44-e92a-e5f61f01ac5a");

            TacticalEventDef AspidaCureParticle = Repo.CreateDef<TacticalEventDef>("a1691322-1826-4e12-8695-4aa5a9eae0b9", CureCloudParticle);

            //"E_Mist10 [Acheron_SpawnCureCloudParticle_EventDef]"
            ParticleEffectDef Mist10 = (ParticleEffectDef)Repo.GetDef("9524b306-f35b-3462-a40f-493df9368a94");

            ParticleEffectDef AspidaMist1 = (ParticleEffectDef)Repo.CreateDef("5d0037df-6e2c-4d54-919b-2616eb6d9c78", Mist10);
            ParticleEffectDef AspidaMist2 = (ParticleEffectDef)Repo.CreateDef("739b4edf-4c91-48f1-b60d-59ad39d3dff8", Mist10);
            ParticleEffectDef AspidaMist3 = (ParticleEffectDef)Repo.CreateDef("076ab56e-2977-4031-9efb-1f76e91697e5", Mist10);
            ParticleEffectDef AspidaMist4 = (ParticleEffectDef)Repo.CreateDef("85a863bf-388a-4326-9b89-b99aa435a501", Mist10);
            // ParticleEffectDef AspidaMist = (ParticleEffectDef)Repo.CreateDef("711aa121-f90c-4dd4-8995-6bf468656bb7", Mist10);
            AspidaMist1.AttachmentSocketName = "Hydraulics02_L";    // Top of hull left side, (47.6,-69.8,-15.5) rotation
            AspidaMist2.AttachmentSocketName = "Hydraulics02_R";    // Top of hull right side, (47.6, 69.8, 15.5) rotation
            AspidaMist3.AttachmentSocketName = "Pipes01_L";         // Left side, 0,0,0 rotation
            AspidaMist4.AttachmentSocketName = "Pipes01_R";         // Right side, 0,0,0 rotation
            
            AddedRotationParticleEffectDef AspidaMist5 = Repo.CreateDef<AddedRotationParticleEffectDef>("b27dd66b-35a9-484a-9d5b-75ac4b571a6c");
            Helper.CopyFieldsByReflection(AspidaMist3, AspidaMist5);
            AspidaMist5.RotationAxis = new Vector3(0,0,0);
            AspidaMist5.UseTransformOrientation = true;
            AspidaMist5.RotationDirection = AddedRotationParticleEffectDef.Direction.Y;
            AspidaMist5.RotationDegrees = 180f;

            AddedRotationParticleEffectDef AspidaMist6 = Repo.CreateDef<AddedRotationParticleEffectDef>("3425b214-d2eb-4bee-94e2-21fcaec8fbcc", AspidaMist5);
            AspidaMist6.AttachmentSocketName = "Pipes01_R";
            AddedRotationParticleEffectDef AspidaMist7 = Repo.CreateDef<AddedRotationParticleEffectDef>("26cfd7d0-3fb9-48c3-93fe-7351dba99edf",AspidaMist5);
            AspidaMist7.AttachmentSocketName = "Hydraulics02_L";
            AddedRotationParticleEffectDef AspidaMist8 = Repo.CreateDef<AddedRotationParticleEffectDef>("c0f25393-4552-41db-8271-815690be6acc",AspidaMist5);
            AspidaMist8.AttachmentSocketName = "Hydraulics02_R";

            AspidaCureParticle.EffectData.EffectDefs = new EffectDef[]
            {
                AspidaMist1,
                AspidaMist2,
                AspidaMist3,
                AspidaMist4,
                AspidaMist5,
                AspidaMist6,
                AspidaMist7,
                AspidaMist8
            };
            return AspidaCureParticle;
        }

        private static MultiEffectDef CureEffects()
        {
            MultiEffectDef Effects = (MultiEffectDef)Repo.GetDef("4f8c1afc-9df0-45fd-9885-ba1c0c7a9251");
            if (Effects == null)
            {
                //Cure_MultiEffectDef -> Used for CureSpray and Cure Cloud
                MultiEffectDef CureEffects = (MultiEffectDef)Repo.GetDef("d26406e9-2851-51c4-c9e0-a8df5079d622");
                Effects = Repo.CreateDef<MultiEffectDef>("4f8c1afc-9df0-45fd-9885-ba1c0c7a9251", CureEffects);
                Effects.name = "AspidaCure_MultiEffectDef";
                Effects.ApplicationConditions = new EffectConditionDef[]
                {
                    CureConditionsDef(),
                };
                Effects.PreconsiderApplicationConditions = false;
            }
            return Effects;
        }
        private static OrEffectConditionDef CureConditionsDef()
        {
            OrEffectConditionDef Conditions = (OrEffectConditionDef)Repo.GetDef("7183e145-4aff-4b25-bb84-d6224ea4428f");
            if (Conditions == null)
            {
                Conditions = Repo.CreateDef<OrEffectConditionDef>("7183e145-4aff-4b25-bb84-d6224ea4428f");
                Conditions.name = "E_ApplicationConditions [Aspida_CureCloud_AbilityDef]";
                Conditions.OrConditions = new EffectConditionDef[]
                {
                    (ActorHasStatusEffectConditionDef)Repo.GetDef("fa7ad091-29f3-57a4-9a1b-ca77b4e6a048"), //"HasParalysedStatus_ApplicationCondition"
                    (ActorHasStatusEffectConditionDef)Repo.GetDef("801f7d7d-a087-7994-c835-a13eefdf5e00"), //"HasParalysisStatus_ApplicationCondition"
                    (ActorHasStatusEffectConditionDef)Repo.GetDef("9adc999a-2528-99d4-5a44-d862a2492f27"), //"HasPoisonStatus_ApplicationCondition"
                    (ActorHasStatusEffectConditionDef)Repo.GetDef("29a6cfa8-44c7-2614-882a-e36c74a16b4e"), //"HasBleedStatus_ApplicationCondition"
                    (ActorHasStatusEffectConditionDef)Repo.GetDef("263cd4bf-6b9e-4d04-0b9b-9fd3db3580a3"), //"HasInfectedStatus_ApplicationCondition"
                    HasStrainedStatus(),
                    HasDazedStatus(),
                    HasSlowedStatus(),
                };
            }
            return Conditions;
        }

        private static ActorHasStatusEffectConditionDef HasStrainedStatus()
        {
            ActorHasStatusEffectConditionDef Condition = (ActorHasStatusEffectConditionDef)Repo.GetDef("351b8099-dfdd-444c-8c42-1f167f48164c");
            if (Condition == null)
            {
                Condition = Repo.CreateDef<ActorHasStatusEffectConditionDef>("351b8099-dfdd-444c-8c42-1f167f48164c");
                Condition.name = "HasStrainedStatus_ApplicationCondition";
                Condition.StatusDef = (TacStatusDef)Repo.GetDef("6bd0ce4c-882b-1d84-68f3-68d15ddf8ee8"); //"Strained_StatusDef"
                Condition.HasStatus = true;
            }
            return Condition;
        }
        private static ActorHasStatusEffectConditionDef HasDazedStatus()
        {
            ActorHasStatusEffectConditionDef Condition = (ActorHasStatusEffectConditionDef)Repo.GetDef("75d3765c-ed8c-46f5-b583-2ecff4ea8459");
            if (Condition == null)
            {
                Condition = Repo.CreateDef<ActorHasStatusEffectConditionDef>("75d3765c-ed8c-46f5-b583-2ecff4ea8459");
                Condition.name = "HasDazedStatus_ApplicationCondition";
                Condition.StatusDef = (TacStatusDef)Repo.GetDef("bc7c3977-a34f-0594-2aa0-6fc76c4351d4"); //"ActorStunned_StatusDef"
                Condition.HasStatus = true;
            }
            return Condition;
        }
        private static ActorHasStatusEffectConditionDef HasSlowedStatus()
        {
            ActorHasStatusEffectConditionDef Condition = (ActorHasStatusEffectConditionDef)Repo.GetDef("e7c9ce29-0430-4f75-9968-d171f93bedf4");
            if (Condition == null)
            {
                Condition = Repo.CreateDef<ActorHasStatusEffectConditionDef>("e7c9ce29-0430-4f75-9968-d171f93bedf4");
                Condition.name = "HasDazedStatus_ApplicationCondition";
                Condition.StatusDef = (TacStatusDef)Repo.GetDef("3191779e-4fa2-68f4-3a91-88ce97cc5c7c"); //"Slowed_StatusDef"
                Condition.HasStatus = true;
            }
            return Condition;
        }

        private static TacticalAbilityViewElementDef Cure_VED(TacticalAbilityViewElementDef Template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("579502ca-d99a-4f07-a595-f0f18ff8f730");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("579502ca-d99a-4f07-a595-f0f18ff8f730", Template);
                VED.name = "E_View [Aspida_CureCloud_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("SY_CURE_NAME");
                VED.Description = new LocalizedTextBind("SY_CURE_DESC");
                VED.ShowInInventoryItemTooltip = true;
            }
            return VED;
        }

        private static StatusEffectDef RestoreArmourEffect(EffectDef template)
        {
            StatusEffectDef Effect = (StatusEffectDef)Repo.GetDef("762715ae-ec04-44e9-a478-f51f55190e87");
            if (Effect == null)
            {
                Effect = Repo.CreateDef<StatusEffectDef>("762715ae-ec04-44e9-a478-f51f55190e87", (StatusEffectDef)template);
                Effect.name = "E_Effect [Aspida_RestoreArmour_AbilityDef]";
                Effect.ApplicationConditions = new EffectConditionDef[]
                {
                    RestoreArmourConditions()
                };
            }
            return Effect;
        }

        private static TacticalEventDef RestoreArmourParticleEffects()
        {
            //"Acheron_SpawnCorrosiveCloudParticle_EventDef"
            TacticalEventDef CorrosiveCloudParticles = (TacticalEventDef)Repo.GetDef("57b0f4b2-0d01-51a4-bbb6-870da8b291e3");
            
            TacticalEventDef AspidaRestoreArmourParticles = Repo.CreateDef<TacticalEventDef>("9b1f72ec-ebba-4911-ba99-42cd7a275862", CorrosiveCloudParticles);

            //"E_Mist10 [Acheron_SpawnCorrosiveCloudParticle_EventDef]"
            ParticleEffectDef Mist10 = (ParticleEffectDef)Repo.GetDef("a20e5fc4-f1e3-5b33-cd1d-39f84a4e06e9");

            ParticleEffectDef AspidaMist1 = Repo.CreateDef<ParticleEffectDef>("c6a66926-760a-486f-b42d-fafbcae447a9", Mist10);
            ParticleEffectDef AspidaMist2 = Repo.CreateDef<ParticleEffectDef>("9232e72c-edaa-4a56-a721-c7b73f296075", Mist10);
            ParticleEffectDef AspidaMist3 = Repo.CreateDef<ParticleEffectDef>("bd9540f2-3d4c-4b38-a03e-75614889e543", Mist10);
            ParticleEffectDef AspidaMist4 = Repo.CreateDef<ParticleEffectDef>("64a50684-b166-43fa-bec8-00396823be37", Mist10);
            AspidaMist1.AttachmentSocketName = "Hydraulics02_L";    // Top of hull left side, (47.6,-69.8,-15.5) rotation
            AspidaMist2.AttachmentSocketName = "Hydraulics02_R";    // Top of hull right side, (47.6, 69.8, 15.5) rotation
            AspidaMist3.AttachmentSocketName = "Pipes01_L";         // Left side, 0,0,0 rotation
            AspidaMist4.AttachmentSocketName = "Pipes01_R";         // Right side, 0,0,0 rotation

            AddedRotationParticleEffectDef AspidaMist5 = Repo.CreateDef<AddedRotationParticleEffectDef>("f9a01ec3-1a5b-434a-a80f-c2124e92ae7d");
            Helper.CopyFieldsByReflection(AspidaMist3, AspidaMist5);
            AspidaMist5.RotationAxis = new Vector3(0,0,0);
            AspidaMist5.UseTransformOrientation = true;
            AspidaMist5.RotationDirection = AddedRotationParticleEffectDef.Direction.Y;
            AspidaMist5.RotationDegrees = 180f;

            AddedRotationParticleEffectDef AspidaMist6 = Repo.CreateDef<AddedRotationParticleEffectDef>("a0199153-c920-4482-ae19-a98a75f42b27", AspidaMist5);
            AspidaMist6.AttachmentSocketName = "Pipes01_R";
            AddedRotationParticleEffectDef AspidaMist7 = Repo.CreateDef<AddedRotationParticleEffectDef>("1cfffdbc-416f-440f-aa0a-a646ecc1cd86",AspidaMist5);
            AspidaMist7.AttachmentSocketName = "Hydraulics02_L";
            AddedRotationParticleEffectDef AspidaMist8 = Repo.CreateDef<AddedRotationParticleEffectDef>("dbc7d2b5-65d3-4783-9c8e-486da76970e5",AspidaMist5);
            AspidaMist8.AttachmentSocketName = "Hydraulics02_R";

            AspidaRestoreArmourParticles.EffectData.EffectDefs = new EffectDef[]
            {
                AspidaMist1,
                AspidaMist2,
                AspidaMist3,
                AspidaMist4,
                AspidaMist5,
                AspidaMist6,
                AspidaMist7,
                AspidaMist8
            };
            return AspidaRestoreArmourParticles;
        }
        private static ActorArmourThresholdEffectConditionDef RestoreArmourConditions()
        {
               ActorArmourThresholdEffectConditionDef Condition = (ActorArmourThresholdEffectConditionDef)Repo.GetDef("801b5018-9682-48b3-bc60-db9d41c3af10");
               if (Condition == null)
               {
                    Condition = Repo.CreateDef<ActorArmourThresholdEffectConditionDef>("801b5018-9682-48b3-bc60-db9d41c3af10");
                    Condition.name = "E_ApplicationConditions [Aspida_RestoreArmour_AbilityDef]";
                    Condition.ThresholdCondition = ThresholdCondition.LesserThan;
                    Condition.StatName = "Armour";
                    Condition.Value = 1f;
                    Condition.ValueAsFractionOfMax = true;
               }
               return Condition;
        }

        private static TacticalAbilityViewElementDef Armour_VED()
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("3cda60ec-d0ee-4a53-98c9-4b305ddc8862");
            if (VED == null)
            {
                //E_View [ElectricReinforcement_AbilityDef]
                TacticalAbilityViewElementDef ElectricReinfVED = (TacticalAbilityViewElementDef)Repo.GetDef("3c31041c-edb9-4e53-c219-4e272c84848c");
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("3cda60ec-d0ee-4a53-98c9-4b305ddc8862", ElectricReinfVED);
                VED.name = "E_View [Aspida_RestoreArmour_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("SY_ARMOUR_NAME");
                VED.Description = new LocalizedTextBind("SY_ARMOUR_DESC");
                VED.ShowInInventoryItemTooltip = true;
            }
            return VED;
        }
    }
}