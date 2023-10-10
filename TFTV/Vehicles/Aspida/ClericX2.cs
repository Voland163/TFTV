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
            CureSprayCloud.ActionPointCost = 0.5f;
            CureSprayCloud.AnimType = -1;
        
            //Cure_MultiEffectDef -> Used for CureSpray and Cure Cloud
            MultiEffectDef CureEffects = (MultiEffectDef)Repo.GetDef("d26406e9-2851-51c4-c9e0-a8df5079d622");
            CureSprayCloud.EffectDef = CureEffects;

            CureSprayCloud.EventOnActivate = CureParticleEffects();
            CureSprayCloud.ViewElementDef = Cure_VED(CureCloud.ViewElementDef);

            return CureSprayCloud;
        }

        private static ApplyEffectAbilityDef RestoreArmour()
        {
            //"Acheron_RestorePandoranArmor_AbilityDef"
            ApplyEffectAbilityDef RestoreArmour = (ApplyEffectAbilityDef)Repo.GetDef("65356940-ac77-01a4-690f-ea9e3aef7c03");

            ApplyEffectAbilityDef Aspida_RestoreArmour = (ApplyEffectAbilityDef)Repo.CreateDef("19b49bf8-2d85-43cb-be38-1b2694e7182b", RestoreArmour);
            Aspida_RestoreArmour.name = "Aspida_RestoreArmour_AbilityDef";
            Aspida_RestoreArmour.WillPointCost = 0;
            Aspida_RestoreArmour.ActionPointCost = 0.5f;
            Aspida_RestoreArmour.AnimType = -1;

            TacticalTargetingDataDef Aspida_RA_Target = (TacticalTargetingDataDef)Repo.CreateDef("d8123e9e-f62d-41fd-9b16-b6e0262eedcf",RestoreArmour.TargetingDataDef);
            Aspida_RA_Target.name = "E_TargetingData [Aspida_RestoreArmour_AbilityDef]";
            Aspida_RA_Target.Origin.TargetTags = new GameTagsList{};

            //"Acheron_RestoreArmorCast_ParticleEventDef"
            Aspida_RestoreArmour.EventOnActivate = (TacticalEventDef)Repo.GetDef("6253e5de-a558-73e4-f8a6-970f49af1dc1");
            Aspida_RestoreArmour.TargetingDataDef = Aspida_RA_Target;
            Aspida_RestoreArmour.ViewElementDef = Armour_VED();
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

            AspidaCureParticle.EffectData.EffectDefs = new EffectDef[]
            {
                AspidaMist1,
                AspidaMist2,
                AspidaMist3,
                AspidaMist4
            };
            return AspidaCureParticle;
        }
        
        // private static TacticalEventDef RestoreArmourEffects()
        // {
        //     return
        // }

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