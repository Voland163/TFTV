using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using System.Collections.Generic;

namespace TFTVVehicleRework.Armadillo
{
    public static class Mephistopheles
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;
        internal static SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;
        // "NJ_Armadillo_Mephistopheles_GroundVehicleWeaponDef"
        internal static readonly GroundVehicleWeaponDef Meph = (GroundVehicleWeaponDef)Repo.GetDef("49723d28-b373-3bc4-7918-21e87a72c585");

        public static void Change()
        {           
            Rebalance();
            ArmadilloMain.Update_Requirements(Meph);
        }

        private static void Rebalance()
        {
            Meph.ChargesMax = 8;
            Meph.DamagePayload.ConeRadius = 2f;
            
            //"NJ_Flamethrower_WeaponDef"
            WeaponDef Flamethrower = (WeaponDef)Repo.GetDef("b62efc91-6997-3064-7848-14299c6ddbc0");
            Meph.MainSwitch = Flamethrower.MainSwitch;
            //"FlameThrower_ShootAbilityDef"
            ShootAbilityDef FlamethrowerShoot = (ShootAbilityDef)Repo.GetDef("9cb530ee-14ad-11d4-2a31-312a93f799e9");
            Meph.Abilities = new AbilityDef[]
            {
                FlamethrowerShoot,
                AdaptiveWeapon()
            };
        }

        private static ApplyStatusAbilityDef AdaptiveWeapon()
        {
            //"Mutoid_Arm_GooGrenade_AdaptiveWeaponStatusDef"
            AdaptiveWeaponStatusDef GooArm = (AdaptiveWeaponStatusDef)Repo.GetDef("f3adf78e-86d3-0264-1930-ff5e2d6ea94b");
            AdaptiveWeaponStatusDef MephStatus = Repo.CreateDef<AdaptiveWeaponStatusDef>("c63d61b2-4afd-4809-ba29-fbf85bd3f270", GooArm);
            MephStatus.WeaponDef = Obliterator();            

            //"BloodLust_AbilityDef"
            ApplyStatusAbilityDef BloodLust = (ApplyStatusAbilityDef)Repo.GetDef("dfe93630-87f7-2774-1bc5-169deb082f7b");
            ApplyStatusAbilityDef CarrierAbility = Repo.CreateDef<ApplyStatusAbilityDef>("1ce5a6d4-f876-4492-a039-e0a899bdff78", BloodLust);
            CarrierAbility.name = "Obliterator_AdaptiveWeaponDef";
            CarrierAbility.ViewElementDef = ObliteratorVED(BloodLust.ViewElementDef);
            CarrierAbility.StatusDef = MephStatus;

            ShootingAnims(Obliterator());
            return CarrierAbility;
        }
        private static GroundVehicleWeaponDef Obliterator()
        {      
            GroundVehicleWeaponDef MephClone = (GroundVehicleWeaponDef)Repo.GetDef("ffb34012-b1fd-4b24-8236-ba2eb23db0b7");
            if (MephClone == null)
            {                               
                MephClone = (GroundVehicleWeaponDef)Repo.CreateDef("ffb34012-b1fd-4b24-8236-ba2eb23db0b7", Meph);
                MephClone.ChargesMax = 15;
                MephClone.DamagePayload.ObjectMultiplier = 1f;
                MephClone.DamagePayload.Speed = 200f;
                MephClone.DamagePayload.AutoFireShotCount = 1;
                MephClone.DamagePayload.ProjectilesPerShot = 5;

                //"PX_ShotgunRifle_WeaponDef"
                WeaponDef PXShotgun = (WeaponDef)Repo.GetDef("f7e8e44c-bfc4-4364-ca81-7b4b1cf57c15");

                MephClone.MainSwitch = PXShotgun.MainSwitch; 
                MephClone.DamagePayload.ProjectileVisuals = PXShotgun.DamagePayload.ProjectileVisuals;
                MephClone.VisualEffects = PXShotgun.VisualEffects;
                MephClone.SpreadDegrees = 30f;
                
                MephClone.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
                {
                    new DamageKeywordPair
                    {
                        DamageKeywordDef = keywords.BlastKeyword,
                        Value = 80f
                    },
                    new DamageKeywordPair
                    {
                        DamageKeywordDef = keywords.ShreddingKeyword,
                        Value = 10f
                    },
                };
            }
            return MephClone;
        }

        private static TacticalAbilityViewElementDef ObliteratorVED(TacticalAbilityViewElementDef template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("a04374fe-4d47-475a-854a-c07e97b3652f");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("a04374fe-4d47-475a-854a-c07e97b3652f", template);
                VED.name = "E_View [Obliterator_AdaptiveWeaponDef]";
                VED.DisplayName1 = new LocalizedTextBind("NJ_OBLITERATOR_NAME");
                VED.Description = new LocalizedTextBind("NJ_OBLITERATOR_DESC");
                VED.ShowInInventoryItemTooltip = true;
                //"E_View [Weapon_ShootAbilityDef]"
                TacticalAbilityViewElementDef ShootAbilityVED = (TacticalAbilityViewElementDef)Repo.GetDef("50c4bdf1-effe-0553-f2c4-d7a32d9d6a36");
                VED.SmallIcon = VED.LargeIcon = ShootAbilityVED.SmallIcon;
            }
            return VED;
        }

        private static void ShootingAnims(GroundVehicleWeaponDef weapon)
        {
            //"E_MephistophelesShooting [NJ_Armadillo_AnimActionsDef]"
            TacActorShootAnimActionDef MephShooting =  (TacActorShootAnimActionDef)Repo.GetDef("7ed75933-d69d-f850-c51b-3934d9388235");
            MephShooting.Equipments = MephShooting.Equipments.AddToArray(weapon);
        }
    }
}