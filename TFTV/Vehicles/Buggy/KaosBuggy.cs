using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using System;
using System.Collections.Generic;

namespace TFTVVehicleRework.KaosBuggy
{
    internal class KaosBuggyMain 
    {
        internal static readonly DefRepository Repo = VehiclesMain.Repo;
        private static readonly SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;
        internal enum KSWeapons {Fullstop, Screamer, Vishnu};

        internal static readonly Dictionary<KSWeapons, GroundVehicleModuleDef> BuggyGuns = new Dictionary<KSWeapons, GroundVehicleModuleDef>
        {
            {KSWeapons.Fullstop, (GroundVehicleModuleDef)Repo.GetDef("1a8003c9-508d-3504-3ad6-941ddf3801ea")}, //"KS_Buggy_Fullstop_GroundVehicleModuleDef"
            {KSWeapons.Screamer, (GroundVehicleModuleDef)Repo.GetDef("85cb5ca9-dd34-cff4-f832-697e9823a63e")}, //"KS_Buggy_The_Screamer_GroundVehicleModuleDef"
            {KSWeapons.Vishnu, (GroundVehicleModuleDef)Repo.GetDef("0a2e541a-8501-a894-9b6a-fc1e229d8979")} //"KS_Buggy_The_Vishnu_Gun_GroundVehicleModuleDef"
        };
        
        
        private static readonly TacticalEventDef ShootShotEvent = (TacticalEventDef)Repo.GetDef("fdb197bf-f1f4-b1e4-9be5-c455b8ec8caf"); //"ShootShot_EventDef"
        private static readonly WeaponDef Yat = (WeaponDef)Repo.GetDef("8f49ef31-b08a-1634-59d1-e21183d444ca"); //"NE_AssaultRifle_WeaponDef"
        private static readonly ShootAbilityDef LaunchGrenade = (ShootAbilityDef)Repo.GetDef("81fbb5db-1b12-b8f4-998e-6591f0771a2d"); //LaunchGrenade_ShootAbilityDef
        internal enum HullModules {Front, Back, Left, Right, Top, LFT, RFT, BT}

        internal static readonly Dictionary<HullModules, TacticalItemDef> DefaultHull = new Dictionary<HullModules, TacticalItemDef>
        {
            {HullModules.Front, (TacticalItemDef)Repo.GetDef("4d9379c6-20cf-bb54-58a7-ee9e01b80165")},  //"KS_Kaos_Buggy_Front_BodyPartDef"
            {HullModules.Back, (TacticalItemDef)Repo.GetDef("06302d51-cf0b-0d74-1b3a-81d2a01c834d")},   //"KS_Kaos_Buggy_Back_BodyPartDef"
            {HullModules.Left, (TacticalItemDef)Repo.GetDef("011fc059-7620-0b24-49d5-9658f82b6064")},   //"KS_Kaos_Buggy_Left_BodyPartDef"
            {HullModules.Right, (TacticalItemDef)Repo.GetDef("a05f5d42-cd3b-ef94-da7f-49ea8f1f77ae")},  //"KS_Kaos_Buggy_Right_BodyPartDef"
            {HullModules.Top, (TacticalItemDef)Repo.GetDef("0d6b3f5c-93ba-04e4-d99e-bcaba3db9bd6")},    //"KS_Kaos_Buggy_Top_BodyPartDef"
            {HullModules.LFT, (TacticalItemDef)Repo.GetDef("c1e01ee1-7d8f-13d4-697f-ebd44b17e158")},    //"KS_Kaos_Buggy_LeftFrontTyre_BodyPartDef"
            {HullModules.RFT, (TacticalItemDef)Repo.GetDef("3676d9f2-fdc8-1744-8a85-34fdc0afb655")},    //"KS_Kaos_Buggy_RightFrontTyre_BodyPartDef"
            {HullModules.BT, (TacticalItemDef)Repo.GetDef("7cf5dc60-24d0-c5c4-4a1d-d04eb57b3e9c")},     //"KS_Kaos_Buggy_RearTyre_BodyPartDef"
        };
        public static void Change()
        {
            Rebalance_Weapons();
            Rebalance_BodyParts();
            Fix_GeoTooltip();
            Update_ItemCost();
            Give_VehicleEntity();
            Adjust_WeaponPrices();
            Fix_WheelSlots();
            Fix_WeaponSlots();
            Replace_JunkerAI();
            Kamikaze.Change();
            Deathproof.Change();
            MannedGunner.Change();
            Slaughterhouse.Change();
        }
            
        public static void Rebalance_Weapons()
        {
            foreach (KSWeapons Module in BuggyGuns.Keys)
            {
                BuggyGuns[Module].BodyPartAspectDef.Endurance = 0; //Prevents random bonus HP because of weapons
                WeaponDef Minigun = (WeaponDef)BuggyGuns[Module].SubAddons[0].SubAddon; //Reference to the three miniguns
                Minigun.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_MINIGUN");
                Minigun.ShootingEvent = ShootShotEvent;
                Minigun.MainSwitch = Yat.MainSwitch;
                Minigun.ChargesMax = 80;
                Minigun.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.DamageKeyword).Value = 35;
                Minigun.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.ShreddingKeyword).Value = 2;
                Minigun.DamagePayload.AutoFireShotCount = 10;
                Minigun.SpreadDegrees = (40.99f/19f); //= 19 effective range; ER = 41/Spread
                switch(Module)
                {
                    case KSWeapons.Fullstop:
                        BuggyGuns[Module].ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_GOOGUN");
                        WeaponDef GooGun = (WeaponDef)BuggyGuns[Module].SubAddons[1].SubAddon; 
                        GooGun.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_GOOGUN");
                        GooGun.ChargesMax = 6;
                        // GooGun.ChargesMax = 8;
                        GooGun.Abilities = new AbilityDef[]
                        {
                            LaunchGrenade
                        };
                        GooGun.DamagePayload.DamageKeywords.Add(new DamageKeywordPair{DamageKeywordDef = keywords.BlastKeyword, Value = 10f});
                        GooGun.DamagePayload.DamageKeywords.Add(new DamageKeywordPair{DamageKeywordDef = keywords.AcidKeyword, Value = 20f});
                        break;
                    case KSWeapons.Screamer:
                        BuggyGuns[Module].ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_SCREAMER");
                        WeaponDef Screamer = (WeaponDef)BuggyGuns[Module].SubAddons[1].SubAddon;
                        Screamer.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_SCREAMER");
                        Screamer.ChargesMax = 6;
                        // Screamer.ChargesMax = 8;
                        Screamer.DamagePayload.DamageDeliveryType = DamageDeliveryType.Sphere;
                        Screamer.DamagePayload.ParabolaHeightToLengthRatio = 0.00000001f;
                        Screamer.DamagePayload.AoeRadius = 1.5f;
                        Change_ScreamerSceneView();
                        break;
                    case KSWeapons.Vishnu:
                        BuggyGuns[Module].ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_TITAN");
                        WeaponDef TitanGL = (WeaponDef)BuggyGuns[Module].SubAddons[1].SubAddon;
                        TitanGL.Tags.Add((GameTagDef)Repo.GetDef("d947049a-4e90-1804-1bdf-25d9acc7b11a")); //"ExplosiveWeapon_TagDef"
                        TitanGL.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_TITAN");
                        TitanGL.APToUsePerc = 50;
                        TitanGL.Abilities = new AbilityDef[]
                        {
                            LaunchGrenade
                        };
                        TitanGL.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.BlastKeyword).Value = 50;
                        TitanGL.DamagePayload.AoeRadius = 2.5f;
                        TitanGL.DamagePayload.Range = 24f;
                        break;
                    default: //Catches anything unexpected
                        break;
                }
            }
        }

        public static void Rebalance_BodyParts()
        {
            // Base hull properties: Speed 21->28; HP 1480 -> 750; Various nerfs to Hull HP/Armour
            ItemDef BuggyChassis  = (ItemDef)Repo.GetDef("cbf9ba5d-5178-6204-d987-dbca1bec838c"); //"KS_Kaos_Buggy_Chassis_ItemDef"
            BuggyChassis.ManufactureMaterials = 450f;
            BuggyChassis.ManufactureTech = 50f;
            foreach (AddonDef.SubaddonBind addon in BuggyChassis.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                if (BodyPart == DefaultHull[HullModules.Front])
                {
                    BodyPart.HitPoints = 250f;
                    BodyPart.Armor = 40f;
                    BodyPart.BodyPartAspectDef.Endurance = 20f;
                }
                else if (BodyPart == DefaultHull[HullModules.Back])
                {
                    BodyPart.HitPoints = 200f;
                    BodyPart.BodyPartAspectDef.Endurance = 13f;
                }
                else if (BodyPart == DefaultHull[HullModules.Left] || BodyPart == DefaultHull[HullModules.Right])
                {
                    BodyPart.HitPoints = 200f;
                    BodyPart.Armor = 30f;
                    BodyPart.BodyPartAspectDef.Endurance = 16f;
                }
                else if (BodyPart == DefaultHull[HullModules.Top])
                {
                    BodyPart.BodyPartAspectDef.Endurance = 10f;
                }
                else if(BodyPart == DefaultHull[HullModules.BT])
                {
                    BodyPart.HitPoints = 150f;
                    BodyPart.BodyPartAspectDef.Speed = 10f;
                    BodyPart.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_WHEEL");
                }
                else //Last case are the remaining two tyres:
                {
                    BodyPart.HitPoints = 150f;
                    BodyPart.BodyPartAspectDef.Speed = 9f;
                    BodyPart.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_WHEEL");
                }
            }
        }

        private static void Give_VehicleEntity()
        {
            // KS_Kaos_Buggy_ActorDef
            TacticalActorDef BuggyActorDef = (TacticalActorDef)Repo.GetDef("aaac8f86-772c-cf44-e8cd-0f07a8b6bf83"); 
            // MachineEntity_ClassProficiencyAbilityDef
            ClassProficiencyAbilityDef MachineEntityProficiency = (ClassProficiencyAbilityDef)Repo.GetDef("8a02e039-e442-7774-8846-c497e257f25f");
            BuggyActorDef.Abilities = BuggyActorDef.Abilities.AddToArray(MachineEntityProficiency);
        }

        public static void Fix_GeoTooltip()
        {
            GroundVehicleItemDef KaosBuggy  = (GroundVehicleItemDef)Repo.GetDef("311b122c-8d53-f3b4-99cd-1b56d3852ef7"); //"KS_Kaos_Buggy_ItemDef"
            KaosBuggy.DataDef.HitPoints = 750f;
            KaosBuggy.DataDef.Capacity = 2f;
            KaosBuggy.DataDef.Armor = 28f;

            KaosBuggy.ViewElementDef.DisplayName1 = new LocalizedTextBind("UI_JUNKER_NAME");
        }

        private static void Update_ItemCost()
        {
            //"KasoBuggy_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef KaosBuggy = (GeoMarketplaceItemOptionDef)Repo.GetDef("52f7984c-ce31-a844-4a2c-82f00563eb91");
            KaosBuggy.MinPrice = 550f;
            KaosBuggy.MaxPrice = 750f;
        }

        private static void Adjust_WeaponPrices()
        {
            // "TheScreamer_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef Screamer = (GeoMarketplaceItemOptionDef)Repo.GetDef("cdd843f2-374f-4f04-abf6-2d438be2b454");
            // "TheFullstop_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef Fullstop = (GeoMarketplaceItemOptionDef)Repo.GetDef("1a4ccc7f-3b33-e8c4-898a-95c143f9be92");
            Screamer.MinPrice = Fullstop.MinPrice = 200f;
            Screamer.MaxPrice = Fullstop.MaxPrice = 300f;
        }

        private static void Fix_WheelSlots()
        {
            //"Kaos_LeftBackWheel_SlotDef"
            ItemSlotDef BW = (ItemSlotDef)Repo.GetDef("00fa8a15-3b55-fc24-3834-3061c6abbd48");
            BW.DisplayName = new LocalizedTextBind("UI_JUNKER_WHEEL");
            //"Kaos_LeftFrontWheel_SlotDef"
            ItemSlotDef LFW = (ItemSlotDef)Repo.GetDef("f5aa29b3-18fe-1554-9a3c-6b31683b1df4");
            LFW.DisplayName = new LocalizedTextBind("UI_JUNKER_WHEEL");
            //"Kaos_RightFrontWheel_SlotDef"
            ItemSlotDef RFW = (ItemSlotDef)Repo.GetDef("d674a04a-7aac-d014-ab3f-b48ead1995c5");
            RFW.DisplayName = new LocalizedTextBind("UI_JUNKER_WHEEL");
            
            //Setting to true means that armour is overridden by higher layers
            BW.DontStackArmorAndHealth = LFW.DontStackArmorAndHealth = RFW.DontStackArmorAndHealth = true;
        }

        private static void Fix_WeaponSlots()
        {
            //"KS_Cannon_SlotDef"
            ItemSlotDef Cannon = (ItemSlotDef)Repo.GetDef("038c9014-5c6a-d804-9a76-e207e1de9a7b");
            Cannon.DamageHandler = DamageHandler.AttachedItem;
            //"KS_Minigun_SlotDef"
            ItemSlotDef Minigun = (ItemSlotDef)Repo.GetDef("8eae5413-09d9-5e14-6a00-b445cb370e26");          
            Minigun.DamageHandler = DamageHandler.AttachedItem;
        }
        
        private static void Change_ScreamerSceneView()
        {
            //"E_Element0 [KS_Buggy_Screamer_WeaponDef]"
            ShootAbilityDef ScreamerShooting = (ShootAbilityDef)Repo.GetDef("e2dc5d29-f46b-ef62-f6bc-d8c15c42fa28");
            ScreamerShooting.SceneViewElementDef = LaunchGrenade.SceneViewElementDef;
            ScreamerShooting.UpdatePrediction = true;
        }
        
        private static void Replace_JunkerAI()
        {
            TacAIActorDef ArmadilloAI = (TacAIActorDef)Repo.GetDef("67f797a7-611b-0df4-9876-9adb3c2d20ec"); //"Armadillo_AIActorDef"
            TacAIActorDef NewAI = Repo.CreateDef<TacAIActorDef>("3b24e4e5-12da-4b85-8aac-16f6a76aed98", ArmadilloAI);
            NewAI.name = "Junker_AIActorDef";

            ComponentSetDef Buggy_CSD = (ComponentSetDef)Repo.GetDef("badec04c-b5f5-c834-4a43-f6fdda3e1f09"); //"KS_Kaos_Buggy_ComponentSetDef"
            TacAIActorDef DefaultAI = (TacAIActorDef)Repo.GetDef("40fa77e4-078c-f224-4a13-961be07c561f"); //"AIActorDef"
            Buggy_CSD.Components[Array.FindIndex(Buggy_CSD.Components, component => component == DefaultAI)] = NewAI;
        }
    }
}
