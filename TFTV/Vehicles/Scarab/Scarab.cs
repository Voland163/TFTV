using Base.Defs;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PRMBetterClasses;
using System.Collections.Generic;
using TFTVVehicleRework.Abilities;

namespace TFTVVehicleRework.Scarab
{
    public static class ScarabMain 
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;
        private static readonly SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;

        public enum HullModules {Front, Back, Left, Right, LFT, RFT, LBT, RBT}

        public static readonly Dictionary<HullModules, TacticalItemDef> DefaultHull = new Dictionary<HullModules, TacticalItemDef>
        {
            {HullModules.Front, (TacticalItemDef)Repo.GetDef("ed43834e-9480-ef84-1ae3-e6ecc44bb49b")},  //"PX_Scarab_Front_BodyPartDef"
            {HullModules.Back, (TacticalItemDef)Repo.GetDef("159f87f3-26e0-abf4-2a12-91af8f31eafc")},   //"PX_Scarab_Back_BodyPartDef"
            {HullModules.Left, (TacticalItemDef)Repo.GetDef("c35e0aff-f5ed-4e24-dab6-f96b6d21d389")},   //"PX_Scarab_Left_BodyPartDef"
            {HullModules.Right, (TacticalItemDef)Repo.GetDef("2a3b96b3-336e-89b4-fb51-84507954034a")},  //"PX_Scarab_Right_BodyPartDef"
            {HullModules.LFT, (TacticalItemDef)Repo.GetDef("c78bf90b-2a4f-a464-0b9b-a8e933bb2e2e")},    //"PX_Scarab_LeftFrontTyre_BodyPartDef"
            {HullModules.RFT, (TacticalItemDef)Repo.GetDef("e27f454e-f465-4284-1835-3710c08d9e18")},    //"PX_Scarab_RightFrontTyre_BodyPartDef"
            {HullModules.LBT, (TacticalItemDef)Repo.GetDef("73250440-0fc7-b274-4ada-50b3be1a132c")},    //"PX_Scarab_LeftBackTyre_BodyPartDef"
            {HullModules.RBT, (TacticalItemDef)Repo.GetDef("eac07fe7-a945-c674-08fa-982d111c8e3d")}     //"PX_Scarab_RightBackTyre_BodyPartDef"
        };

        public static void Change() 
        {
            Apply_GeneralChanges();
            Apply_GeminiChanges();
            Apply_TaurusChanges();
            Apply_ScorpioChanges(); 
            Fix_TurretSlot();     
            AmmunitionRacks.Change();
            DeploymentBay.Change();
            ReinforcedStabilisers.Change();
            LazarusShield.Change();  
        }

        private static void Apply_GeneralChanges()
        {
            //General HP/Armour/Speed changes for Scarab:
            ItemDef ScarabChassis = (ItemDef)Repo.GetDef("ac1b062d-5b12-83d4-f99d-95a8b25a56db"); //"_PX_Scarab_Chassis_ItemDef"
            foreach (AddonDef.SubaddonBind addon in ScarabChassis.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                
                if(BodyPart == DefaultHull[HullModules.Front])
                {
                    BodyPart.Armor = 40;
                    BodyPart.BodyPartAspectDef.Endurance = 24;
                }
                else if(BodyPart == DefaultHull[HullModules.Back])
                {
                    BodyPart.BodyPartAspectDef.Endurance = 12;
                }
                else if(BodyPart == DefaultHull[HullModules.Left] || BodyPart == DefaultHull[HullModules.Right])
                {
                    BodyPart.BodyPartAspectDef.Endurance = 17;
                }
                else if(BodyPart == DefaultHull[HullModules.LFT] ||
                        BodyPart == DefaultHull[HullModules.RFT] ||
                        BodyPart == DefaultHull[HullModules.LBT] ||
                        BodyPart == DefaultHull[HullModules.RBT] )
                {
                    BodyPart.Armor = 20;
                    BodyPart.BodyPartAspectDef.Speed = 9;
                }
            }
            //Update General Information to reflect new armour, HP and Speed
            GroundVehicleItemDef ScarabItem = (GroundVehicleItemDef)Repo.GetDef("986ffda3-f4a5-5824-0be8-55333b1a7264"); //"PX_Scarab_ItemDef"
            ScarabItem.DataDef.HitPoints = 700;
            ScarabItem.DataDef.Speed = 36;
            ScarabItem.DataDef.Armor = 25;
        }

        private static void Apply_GeminiChanges()
        {
            GroundVehicleWeaponDef Gemini = (GroundVehicleWeaponDef)Repo.GetDef("a598bebe-eaad-eb44-3870-92a861b1dc5e");
            Gemini.ChargesMax = 12;
            Gemini.DamagePayload.AutoFireShotCount = 1;
            Gemini.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair{DamageKeywordDef = keywords.BlastKeyword, Value = 70},
                new DamageKeywordPair{DamageKeywordDef = keywords.ShreddingKeyword, Value = 10},
            };
            Gemini.Abilities = new Base.Entities.Abilities.AbilityDef[]
            {
                DefaultSalvo(),
                StabilitySalvo()
            };
        }

        private static void Apply_TaurusChanges()
        {
            //ER 17->20 and Ammunition 4->8;
            GroundVehicleWeaponDef Taurus = (GroundVehicleWeaponDef)Repo.GetDef("d14af403-ec48-f954-48f3-7759a8fca9c2");
            Taurus.SpreadDegrees = (41f/20); //20 ER; ER = 41/Spread
            Taurus.ChargesMax = 18;
            Taurus.ManufactureTech = 30f;
            Taurus.ManufactureMaterials = 250f;

            //"PX_HeavyCannon_WeaponDef"
            WeaponDef Hell2 = (WeaponDef)Repo.GetDef("112a754d-413f-27f4-180c-b052cab71d70");
            Taurus.MainSwitch = Hell2.MainSwitch;
            Taurus.VisualEffects = Hell2.VisualEffects;
            Taurus.DamagePayload.ProjectileVisuals = Hell2.DamagePayload.ProjectileVisuals;
            Taurus.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair{DamageKeywordDef = keywords.DamageKeyword, Value = 200},
                new DamageKeywordPair{DamageKeywordDef = keywords.ShreddingKeyword, Value = 20},
                new DamageKeywordPair{DamageKeywordDef = keywords.ShockKeyword, Value = 280},
            };

            Taurus.Abilities = new Base.Entities.Abilities.AbilityDef[]
            {
                DefaultShoot(),
                StabilityShoot()
            };

            //"PX_HelCannon_ResearchDef_ManufactureResearchRewardDef_0"
            ManufactureResearchRewardDef HellCannonResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("80d2b708-8ee3-228b-278e-acfa09815d64");
            HellCannonResearchReward.Items = HellCannonResearchReward.Items.AddToArray(Taurus);
        }

        private static void Apply_ScorpioChanges()
        {
            //Scorpio gets a whole new Damage Payload inc Virophage.
            GroundVehicleWeaponDef Scorpio = (GroundVehicleWeaponDef)Repo.GetDef("84f2634c-1da6-d114-e8dc-ddd923450c9d");
            Scorpio.HitPoints = 320;
            Scorpio.ChargesMax = 12;
            Scorpio.DamagePayload.AutoFireShotCount = 1;
            Scorpio.ManufactureTech = 70f;
            Scorpio.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
			{
				new DamageKeywordPair{DamageKeywordDef = keywords.BlastKeyword, Value = 40},
				// new DamageKeywordPair{DamageKeywordDef = keywords.ShreddingKeyword, Value = 10},
                // new DamageKeywordPair{DamageKeywordDef = (DamageKeywordDef)Repo.GetDef("c968f22f-392d-1964-68cd-edd14655082d"), Value = 80} //"Virophage_DamageKeywordDataDef"
                new DamageKeywordPair{DamageKeywordDef = (DamageKeywordDef)Repo.GetDef("5bf85e8f-fdf2-a864-d8c0-22fc5f7ab613"), Value = 80} //"VirophageBlast_DamageKeywordDataDef"
			};
            Scorpio.Abilities = new Base.Entities.Abilities.AbilityDef[]
            {
                DefaultSalvo(),
                StabilitySalvo(),
            };

            //"PX_VirophageWeapons_ResearchDef_ManufactureResearchRewardDef_0"
            ManufactureResearchRewardDef VirophageResearchReward = (ManufactureResearchRewardDef)Repo.GetDef("02502242-0b31-a2b3-6d1b-2e78db0465e4");
            VirophageResearchReward.Items = VirophageResearchReward.Items.AddToArray(Scorpio);
        }

        private static void Fix_TurretSlot()
        {
            //"PX_Scarab_Turret_SlotDef"
            ItemSlotDef Turret = (ItemSlotDef)Repo.GetDef("46f4bb51-5d1d-63e4-db57-ba14672e2929");
            Turret.DamageHandler = DamageHandler.AttachedItem;
        }

        private static ShootAbilityDef DefaultSalvo()
        {
            ShootAbilityDef ScarabMissiles = (ShootAbilityDef)Repo.GetDef("3dae58da-1d59-47df-a0a1-b3ac52d67414");
            if(ScarabMissiles == null)
            {
                ShootAbilityDef LaunchMissiles = (ShootAbilityDef)Repo.GetDef("4da72135-1b6e-b184-8b64-77dbdde7cf30"); //"LaunchMissiles_ShootAbilityDef"

                ScarabMissiles = Repo.CreateDef<ShootAbilityDef>("3dae58da-1d59-47df-a0a1-b3ac52d67414", LaunchMissiles);
                ScarabMissiles.ExecutionsCount = 2;

                TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("3989911e-c5bf-4bde-be20-5bf0fea69265", LaunchMissiles.ViewElementDef);
                VED.ShowInInventoryItemTooltip = true;
                VED.Category = new Base.UI.LocalizedTextBind("UI_SCARAB_CATEGORY");
                VED.DisplayPriority = 2;
                ScarabMissiles.ViewElementDef = VED;
            }
            return ScarabMissiles;
        }

        private static ApplyStatusAfterAbilityExecutedAbilityDef StabilitySalvo()
        {
            ApplyStatusAfterAbilityExecutedAbilityDef CarrierAbility = (ApplyStatusAfterAbilityExecutedAbilityDef)Repo.GetDef("6f13f258-2993-466f-ac5a-7203653baca7");
            if (CarrierAbility == null)
            {
                ApplyStatusAbilityDef Bloodlust = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b"); //"BloodLust_AbilityDef"

                CarrierAbility = Repo.CreateDef<ApplyStatusAfterAbilityExecutedAbilityDef>("6f13f258-2993-466f-ac5a-7203653baca7");
                Helper.CopyFieldsByReflection(Bloodlust, CarrierAbility);
                CarrierAbility.name = "ScarabSalvo_AbilityDef";
                CarrierAbility.ViewElementDef = null;
                CarrierAbility.CharacterProgressionData = null;

                AddAbilityStatusDef MissileStatus = Repo.CreateDef<AddAbilityStatusDef>("a928dfd1-a4d7-4bc6-92cf-3db41b626b0a");
                Helper.CopyFieldsByReflection(Bloodlust.StatusDef, MissileStatus);
                MissileStatus.name = "E_Status [ScarabSalvo_AbilityDef]";
                MissileStatus.Visuals = null;
                MissileStatus.ApplicationConditions = new Base.Entities.Effects.ApplicationConditions.EffectConditionDef[]
                {
                    HasStabilityStatus(),
                };
                
                ShootAbilityDef LaunchMissiles = (ShootAbilityDef)Repo.GetDef("4da72135-1b6e-b184-8b64-77dbdde7cf30"); //"LaunchMissiles_ShootAbilityDef"
                ShootAbilityDef ScarabMissiles = Repo.CreateDef<ShootAbilityDef>("df2e83d1-8688-4e47-8559-cc6a9f9906d1", LaunchMissiles);
                ScarabMissiles.ExecutionsCount = 3;

                TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("3b4b01b4-1b14-4acd-a9c2-64040029700f", LaunchMissiles.ViewElementDef);
                VED.Description = new Base.UI.LocalizedTextBind("UI_SALVO_DESC");
                VED.Category = new Base.UI.LocalizedTextBind("UI_SCARAB_CATEGORY");
                VED.DisplayPriority = 1;
                ScarabMissiles.ViewElementDef = VED;

                
                MissileStatus.AbilityDef = ScarabMissiles;
                MissileStatus.UnapplyIfAbilityExists = false;
                CarrierAbility.StatusToApply = MissileStatus;
            }
            return CarrierAbility;
        }

        private static ShootAbilityDef DefaultShoot()
        {
            ShootAbilityDef ScarabShoot = (ShootAbilityDef)Repo.GetDef("82abe659-b5a0-459d-bf3c-9afbd9b24e52");
            if(ScarabShoot == null)
            {
                ShootAbilityDef WeaponShoot = (ShootAbilityDef)Repo.GetDef("d3e8b389-069f-04c4-8aca-fb204c74fd37"); //"Weapon_ShootAbilityDef"

                ScarabShoot = Repo.CreateDef<ShootAbilityDef>("82abe659-b5a0-459d-bf3c-9afbd9b24e52", WeaponShoot);
                ScarabShoot.ExecutionsCount = 2;

                TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("29770b92-1c73-478d-b14d-fe1f54bd28b7", WeaponShoot.ViewElementDef);
                VED.ShowInInventoryItemTooltip = true;
                VED.Description = new Base.UI.LocalizedTextBind("UI_DOUBLESHOOT_DESC");
                VED.Category = new Base.UI.LocalizedTextBind("UI_SCARAB_CATEGORY");
                VED.DisplayPriority = 2;
                ScarabShoot.ViewElementDef = VED;
            }
            return ScarabShoot;
        }

        private static ApplyStatusAfterAbilityExecutedAbilityDef StabilityShoot()
        {
            ApplyStatusAfterAbilityExecutedAbilityDef CarrierAbility = (ApplyStatusAfterAbilityExecutedAbilityDef)Repo.GetDef("392d907a-7131-4ebf-b17c-df4d0044d9cb");
            if (CarrierAbility == null)
            {
                ApplyStatusAbilityDef Bloodlust = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b"); //"BloodLust_AbilityDef"

                CarrierAbility = Repo.CreateDef<ApplyStatusAfterAbilityExecutedAbilityDef>("392d907a-7131-4ebf-b17c-df4d0044d9cb");
                Helper.CopyFieldsByReflection(Bloodlust, CarrierAbility);
                CarrierAbility.name = "ScarabShoot_AbilityDef";
                CarrierAbility.CharacterProgressionData = null;
                CarrierAbility.TargetingDataDef = (TacticalTargetingDataDef)Repo.GetDef("e1ac5f1b-c196-57c4-0a6b-223b33f7bca3"); //"_Self_TargetingDataDef"
                CarrierAbility.ViewElementDef = null;

                AddAbilityStatusDef ShootStatus = Repo.CreateDef<AddAbilityStatusDef>("152c314b-1163-4d64-b1ba-003f27a15851");
                Helper.CopyFieldsByReflection(Bloodlust.StatusDef, ShootStatus);
                ShootStatus.name = "E_Status [ScarabShoot_AbilityDef]";
                ShootStatus.Visuals = null;
                ShootStatus.ApplicationConditions = new Base.Entities.Effects.ApplicationConditions.EffectConditionDef[]
                {
                    HasStabilityStatus(),
                };
                
                ShootAbilityDef WeaponShoot = (ShootAbilityDef)Repo.GetDef("d3e8b389-069f-04c4-8aca-fb204c74fd37"); //"Weapon_ShootAbilityDef"
                ShootAbilityDef ScarabShoot = Repo.CreateDef<ShootAbilityDef>("76ae9352-1343-4b95-964c-036341b6a0eb", WeaponShoot);
                ScarabShoot.ExecutionsCount = 3;

                TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("320228c4-4e2b-49d2-9ff5-bdb2f5fcf2e9", WeaponShoot.ViewElementDef);
                VED.Description = new Base.UI.LocalizedTextBind("UI_TRIPLESHOOT_DESC");
                VED.Category = new Base.UI.LocalizedTextBind("UI_SCARAB_CATEGORY");
                VED.DisplayPriority = 1;
                ScarabShoot.ViewElementDef = VED;

                
                ShootStatus.AbilityDef = ScarabShoot;
                ShootStatus.UnapplyIfAbilityExists = false;
                CarrierAbility.StatusToApply = ShootStatus;
            }
            return CarrierAbility;
        }

        private static ActorHasStatusEffectConditionDef HasStabilityStatus()
        {
            ActorHasStatusEffectConditionDef HasStabilityStatus = (ActorHasStatusEffectConditionDef)Repo.GetDef("243d5a9f-32a2-47ac-a0c9-055e27ee7dfb");
            if (HasStabilityStatus == null)
            {
                HasStabilityStatus = Repo.CreateDef<ActorHasStatusEffectConditionDef>("243d5a9f-32a2-47ac-a0c9-055e27ee7dfb");
                HasStabilityStatus.StatusDef = ReinforcedStabilisers.StabilityStanceStatus();
                HasStabilityStatus.HasStatus = true;
            }
            return HasStabilityStatus;
        }
    }  
}