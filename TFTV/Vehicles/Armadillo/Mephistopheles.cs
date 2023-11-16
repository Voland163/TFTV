using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities;
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
                MephClone.DamagePayload.ObjectMultiplier = 2f;
                MephClone.DamagePayload.Speed = 30f;
                MephClone.DamagePayload.AutoFireShotCount = 1;
                MephClone.DamagePayload.ProjectilesPerShot = 5;
                // MephClone.DamagePayload.ProjectilesPerShot = 3;
                MephClone.DamagePayload.Range = 20f;
                MephClone.DamagePayload.DamageDeliveryType = DamageDeliveryType.Sphere;
                MephClone.DamagePayload.ParabolaHeightToLengthRatio = 0.000001f;
                MephClone.DamagePayload.AoeRadius = 1.5f;
                MephClone.SpreadRadius = 5f;
                MephClone.SpreadRadiusDistanceModifier = ((GroundVehicleWeaponDef)Repo.GetDef("3986d735-5c23-ef24-6983-7d0132068f1b")).SpreadRadiusDistanceModifier; //Purgatory

                //"PX_ShotgunRifle_WeaponDef"
                // WeaponDef PXShotgun = (WeaponDef)Repo.GetDef("f7e8e44c-bfc4-4364-ca81-7b4b1cf57c15");
                //"PX_HeavyCannon_WeaponDef"
                WeaponDef Hell2 = (WeaponDef)Repo.GetDef("112a754d-413f-27f4-180c-b052cab71d70");

                // MephClone.MainSwitch = PXShotgun.MainSwitch; 
                MephClone.MainSwitch = Hell2.MainSwitch; 
                // MephClone.DamagePayload.ProjectileVisuals = PXShotgun.DamagePayload.ProjectileVisuals;
                MephClone.DamagePayload.ProjectileVisuals = Hell2.DamagePayload.ProjectileVisuals;
                // MephClone.VisualEffects = PXShotgun.VisualEffects;
                MephClone.VisualEffects = Hell2.VisualEffects;
                // MephClone.SpreadDegrees = 5f;
                
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

                MephClone.Abilities = new AbilityDef[]
                {
                    ObliteratorShoot()
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

        private static ShootAbilityDef ObliteratorShoot()
        {
            ShootAbilityDef Shoot = (ShootAbilityDef)Repo.GetDef("eb9d9c3d-77cd-4723-84ed-0284140f5eb3");
            if (Shoot == null)
            {
                // "LaunchGrenade_ShootAbilityDef"
                ShootAbilityDef LaunchGrenade = (ShootAbilityDef)Repo.GetDef("81fbb5db-1b12-b8f4-998e-6591f0771a2d");
                Shoot = Repo.CreateDef<ShootAbilityDef>("eb9d9c3d-77cd-4723-84ed-0284140f5eb3", LaunchGrenade);
                Shoot.name = "Obliterate_ShootAbilityDef";
                Shoot.CanShootOnEnemyBodyParts = true;
                Shoot.TargetingDataDef = LaunchGrenade.TargetingDataDef;
                Shoot.ViewElementDef = (TacticalAbilityViewElementDef)Repo.GetDef("50c4bdf1-effe-0553-f2c4-d7a32d9d6a36"); //"E_View [Weapon_ShootAbilityDef]"
                Shoot.SceneViewElementDef = LaunchGrenade.SceneViewElementDef;
            }
            return Shoot;
        }

        private static void ShootingAnims(GroundVehicleWeaponDef weapon)
        {
            //"E_MephistophelesShooting [NJ_Armadillo_AnimActionsDef]"
            TacActorShootAnimActionDef MephShooting =  (TacActorShootAnimActionDef)Repo.GetDef("7ed75933-d69d-f850-c51b-3934d9388235");
            MephShooting.Equipments = MephShooting.Equipments.AddToArray(weapon);
        }
    }
}