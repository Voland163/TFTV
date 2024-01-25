using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;

namespace TFTVVehicleRework.Mutog
{
    public static class MutogMain
    {
        private static readonly DefRepository Repo = VehiclesMain.Repo;
        internal static SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;
        public static void Change()
        {
            PawStrike();
            SlasherTail();
            BasherTail();
            RammingHead();
            VenomHead();
            AgileLegs();
            RegenLegs();
            Update_ItemInfo();
        }

        private static void PawStrike()
        {
            //"Mutog_RightPawBash_AbilityDef"
            BashAbilityDef RightBash = (BashAbilityDef)Repo.GetDef("79678240-9ab9-3a14-2aa3-7457e70fd69a");

            //"Mutog_LeftPawBash_AbilityDef"
            BashAbilityDef LeftBash = (BashAbilityDef)Repo.GetDef("68c7131e-f48d-2614-eb52-225fb1cdf67c");

            List<DamageKeywordPair> NewDamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.DamageKeyword,
                    Value = 100f,
                }
            };
            RightBash.DamagePayload.DamageKeywords = LeftBash.DamagePayload.DamageKeywords = NewDamageKeywords;
        }

        private static void SlasherTail()
        {
            //"Mutog_Tail_Bladed_WeaponDef"
            WeaponDef SlasherTail = (WeaponDef)Repo.GetDef("96ec5f05-d533-ea04-bbf0-d27bba250ded");

            List<DamageKeywordPair> NewDamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.DamageKeyword,
                    Value = 90f,
                },
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.PiercingKeyword,
                    Value = 20f,
                },
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.BleedingKeyword,
                    Value = 30f,
                },
            };
            SlasherTail.DamagePayload.DamageKeywords = NewDamageKeywords;
        }

        private static void BasherTail()
        {
            //"Mutog_Tail_Basher_WeaponDef"
            WeaponDef BasherTail = (WeaponDef)Repo.GetDef("5eb2c4ea-e15e-68e4-2b1f-960fab4d069c");
            BasherTail.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.DamageKeyword).Value = 80f;
            BasherTail.ViewElementDef.DisplayName1 = new Base.UI.LocalizedTextBind("AN_MUTOG_BASHER_NAME");
            BasherTail.ViewElementDef.Description = new Base.UI.LocalizedTextBind("AN_MUTOG_BASHER_DESC");
        }

        private static void RammingHead()
        {
            //"Mutog_HeadRamming_BodyPartDef"
            WeaponDef RammingHead = (WeaponDef)Repo.GetDef("c29d4fc0-cb86-0e54-383c-513f8926e6c1");
            RammingHead.DamagePayload.DamageKeywords.Find(dkp => dkp.DamageKeywordDef == keywords.DamageKeyword).Value = 170f;
        }

        private static void VenomHead()
        {
            //"Mutog_HeadPoison_WeaponDef"
            WeaponDef VenomHead = (WeaponDef)Repo.GetDef("f633a9ee-f8ac-2cf4-dbbd-ef1bf123169f");
            List<DamageKeywordPair> NewDamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.DamageKeyword,
                    Value = 80f,
                },
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.PoisonousKeyword,
                    Value = 80f,
                },
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.ParalysingKeyword,
                    Value = 8f,
                },
            };
            VenomHead.DamagePayload.DamageKeywords = NewDamageKeywords;
            VenomHead.SpreadDegrees = 42f/12;

            //"Mutog_TongueLash_AbilityDef"
            ShootAbilityDef TongueLash = (ShootAbilityDef)Repo.GetDef("af913686-46c1-a814-4812-b13781f6fe51");
            TongueLash.CanUseFirstPersonCam = true;
        }

        private static void AgileLegs()
        {
            //"CloseQuarters_AbilityDef"
            ApplyStatusAbilityDef CloseQuartersEvade = (ApplyStatusAbilityDef)Repo.GetDef("c2c5e9ac-45be-ea54-eadc-71c8a0b318e1");

            //"Mutog_Legs_Agile_ItemDef"
            TacticalItemDef AgileLegs = (TacticalItemDef)Repo.GetDef("5e192014-b4cd-2d04-c9b9-a5fc51193a48");
            foreach (AddonDef.SubaddonBind addon in AgileLegs.SubAddons)
            {
                TacticalItemDef Leg = (TacticalItemDef)addon.SubAddon;
                Leg.BodyPartAspectDef.Speed = 8f;
                Leg.Abilities = Leg.Abilities.AddToArray(CloseQuartersEvade);
            }
        }

        private static void RegenLegs()
        {
            //"Mutog_Legs_Regenerating_ItemDef"
            TacticalItemDef RegenLegs = (TacticalItemDef)Repo.GetDef("3be7b403-5a96-4a94-1ac3-23d3c85c080b");
            foreach (AddonDef.SubaddonBind addon in RegenLegs.SubAddons)
            {
                TacticalItemDef Leg = (TacticalItemDef)addon.SubAddon;
                Leg.Armor = 20f;
                Leg.HitPoints = 200f;
            }

            //"E_Status [Mutog_Regeneration_AbilityDef]"
            HealthChangeStatusDef MutogRegen = (HealthChangeStatusDef)Repo.GetDef("fae0ca58-9fe7-0b9c-da9d-e0ad6cf45a8e");
            MutogRegen.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
            MutogRegen.StackMultipleStatusesAsSingleIcon = true;
            MutogRegen.TargetFirstDamagedBodypart = false;
        }

        private static void Update_ItemInfo()
        {
            //"AN_Mutog_XYZ_ItemDef" where X = Poison/Ramming, Y = Agile/Regen, Z = Bashing/Bladed
            GroundVehicleItemDef PABa = (GroundVehicleItemDef)Repo.GetDef("7243c283-1856-7494-eb16-9fec8e6cfc12");
            GroundVehicleItemDef PABl = (GroundVehicleItemDef)Repo.GetDef("64e9d0e1-9c0d-5e54-7bbf-994ae575bb73");
            GroundVehicleItemDef PRBa = (GroundVehicleItemDef)Repo.GetDef("6f329bb2-db78-1964-4b6a-6f4b2b625567");
            GroundVehicleItemDef PRBl = (GroundVehicleItemDef)Repo.GetDef("3a0f80cd-ad54-cf14-7828-76ca00ee8906");
            GroundVehicleItemDef RABa = (GroundVehicleItemDef)Repo.GetDef("253d8478-b766-a074-bab6-d9085870c7d4");
            GroundVehicleItemDef RABl = (GroundVehicleItemDef)Repo.GetDef("1d2977b2-31b5-7144-2b0e-55b8b73b5952");
            GroundVehicleItemDef RRBa = (GroundVehicleItemDef)Repo.GetDef("92b6b1a6-6c35-3104-18b6-45113e6c889e");
            GroundVehicleItemDef RRBl = (GroundVehicleItemDef)Repo.GetDef("3faad453-c8d9-e4e4-8b01-135377a45488");

            PABa.DataDef.Speed = PABl.DataDef.Speed = RABa.DataDef.Speed = RABl.DataDef.Speed = 32f;
            PABa.DataDef.Armor = PABl.DataDef.Armor = RABa.DataDef.Armor = RABl.DataDef.Armor = 14f;
            PRBa.DataDef.Armor = PRBl.DataDef.Armor = RRBa.DataDef.Armor = RRBl.DataDef.Armor = 20f;
        }
    }
}