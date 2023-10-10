using System;
using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using Base.Core;
using Base.Levels;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using Base.Utils;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Modding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PRMBetterClasses;

namespace TFTVVehicleRework.Aspida
{
    public static class HermesX1
    {
        private static readonly DefRepository Repo = AspidaMain.Repo;
        public static void Change()
        {
            // "SY_Aspida_Hybrid_Engine_Technology_GroundVehicleModuleDef"
            GroundVehicleModuleDef HybridEngine = (GroundVehicleModuleDef)Repo.GetDef("9cbd1428-53c6-a0b4-c90a-843e592b0092");
            HybridEngine.Abilities = new AbilityDef[]
            {
                StimSprayCloud()
            };
            HybridEngine.ViewElementDef.DisplayName1 = new LocalizedTextBind("SY_HYBRID_NAME");
            AspidaMain.Update_Requirements(HybridEngine);
        }

        public static ApplyEffectAbilityDef StimSprayCloud()
        {
            // "DeterminedAdvance_AbilityDef"
            ApplyStatusAbilityDef DetAdv = (ApplyStatusAbilityDef)Repo.GetDef("175744da-5e1d-d1d4-58fb-b08d226b58f6");

            // ApplyStatusAbilityDef StimSpray = Repo.CreateDef<ApplyStatusAbilityDef>("316b7244-69ab-46fa-a435-0861c7446bae", DetAdv);
            ApplyEffectAbilityDef StimSpray = Repo.CreateDef<ApplyEffectAbilityDef>("316b7244-69ab-46fa-a435-0861c7446bae");
            Helper.CopyFieldsByReflection(DetAdv, StimSpray);
            // StimSpray.ApplyStatusToAllTargets = true;
            // StimSpray.ShowNotificationOnUse = true;
            StimSpray.AnimType = -1;
            StimSpray.WillPointCost = 0f;
            StimSpray.ActionPointCost = 0.75f;

            StatusEffectDef StimSprayStatusEffect = Repo.CreateDef<StatusEffectDef>("4cd27b0c-af38-44e9-adfc-fe425efd9491");
            StimSprayStatusEffect.ApplicationConditions = new EffectConditionDef[]{};
            StimSprayStatusEffect.StatusDef = DetAdv.StatusDef;
            
            //"E_Effect [DeterminedAdvance_AbilityDef]"
            // StimSpray.EffectDef = (ModifyStatusStatRatioEffectDef)Repo.GetDef("58b5a010-67ea-d04c-921f-52c2d552c0f7");
            StimSpray.EffectDef = StimSprayStatusEffect;

            StimSpray.ApplyOnStartTurn = false;
            StimSpray.ApplyToAllTargets = true;
            StimSpray.ApplyOnMove = false;
            StimSpray.CheckApplicationConditions = false;
            StimSpray.SimulatesDamage = false;
            StimSpray.MultipleTargetSimulation = false;

            // "Acheron_CureCloud_ApplyEffectAbilityDef"
            ApplyEffectAbilityDef CureCloud = (ApplyEffectAbilityDef)Repo.GetDef("dba1a2a5-39de-2294-6877-1f4296038057");
            TacticalTargetingDataDef StimSprayTargeting = Repo.CreateDef<TacticalTargetingDataDef>("e41969a2-7b86-4466-b83d-fb33e5a35ef3", CureCloud.TargetingDataDef);
            StimSprayTargeting.Origin.Range = 5f;            

            StimSpray.TargetingDataDef = StimSprayTargeting;
            StimSpray.ViewElementDef = VED(DetAdv);
            StimSpray.EventOnActivate = StimParticleEffects();

            return StimSpray;
        }

        private static TacticalEventDef StimParticleEffects()
        {
            // "Acheron_SpawnParalyticCloudParticle_EventDef"
            TacticalEventDef ParalyticCloudParticle = (TacticalEventDef)Repo.GetDef("b3c8e730-f992-b5f4-9a52-019c55dc00b7");

            TacticalEventDef AspidaStimParticle = Repo.CreateDef<TacticalEventDef>("b71b19ac-6f43-4613-9a00-954a8a593470", ParalyticCloudParticle);

            // "E_Mist10 [Acheron_SpawnParalyticCloudParticle_EventDef]"
            ParticleEffectDef Mist10 = (ParticleEffectDef)Repo.GetDef("461518ed-40ee-5791-47ad-dc6929656555");

            ParticleEffectDef AspidaMist1 = (ParticleEffectDef)Repo.CreateDef("241000c8-a381-492f-bffc-6e580a10085d", Mist10);
            ParticleEffectDef AspidaMist2 = (ParticleEffectDef)Repo.CreateDef("da07b1c7-7177-48ba-ba1e-91873bdc1b78", Mist10);
            ParticleEffectDef AspidaMist3 = (ParticleEffectDef)Repo.CreateDef("5c9df931-f7e0-476b-b975-c33dc76a95d0", Mist10);
            ParticleEffectDef AspidaMist4 = (ParticleEffectDef)Repo.CreateDef("159090f7-a48e-4405-bbf7-156ab626288c", Mist10);
            // ParticleEffectDef AspidaMist = (ParticleEffectDef)Repo.CreateDef("711aa121-f90c-4dd4-8995-6bf468656bb7", Mist10);
            AspidaMist1.AttachmentSocketName = "Hydraulics02_L";    // Top of hull left side, (47.6,-69.8,-15.5) rotation
            AspidaMist2.AttachmentSocketName = "Hydraulics02_R";    // Top of hull right side, (47.6, 69.8, 15.5) rotation
            AspidaMist3.AttachmentSocketName = "Pipes01_L";         // Left side, 0,0,0 rotation
            AspidaMist4.AttachmentSocketName = "Pipes01_R";         // Right side, 0,0,0 rotation

            AspidaStimParticle.EffectData.EffectDefs = new EffectDef[]
            {
                AspidaMist1,
                AspidaMist2,
                AspidaMist3,
                AspidaMist4
            };
            return AspidaStimParticle;
        }

        private static TacticalAbilityViewElementDef VED(ApplyStatusAbilityDef Template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("9e6fcb1a-14b8-490a-86f0-b93b75fa1c8c");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("9e6fcb1a-14b8-490a-86f0-b93b75fa1c8c", Template.ViewElementDef);
                VED.DisplayName1 = new LocalizedTextBind("SY_STIM_NAME");
                VED.Description = new LocalizedTextBind("SY_STIM_DESC");
                VED.ShowInInventoryItemTooltip = true;
            }
            return VED;
        }
    }
}