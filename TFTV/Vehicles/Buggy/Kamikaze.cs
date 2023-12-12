using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using System.Collections.Generic;

namespace TFTVVehicleRework.KaosBuggy 
{
    public static class Kamikaze
    {
        private static readonly DefRepository Repo = KaosBuggyMain.Repo;

        private static readonly Dictionary<KaosBuggyMain.HullModules, TacticalItemDef> DefaultHull = KaosBuggyMain.DefaultHull;
        public static void Change()
        {
            // "KS_Kaos_Buggy_Dog_Ring_Gearbox_Engine_GroundVehicleModuleDef"
            GroundVehicleModuleDef DogRingGearBox = (GroundVehicleModuleDef)Repo.GetDef("8a9401fa-77fe-4844-2b7c-f33c9c796c1f");
            DogRingGearBox.Armor = 0f;
            DogRingGearBox.ManufactureMaterials = 200f;
            DogRingGearBox.ManufactureTech = 20f;
            DogRingGearBox.BodyPartAspectDef.Speed = 14f;
            // DogRingGearBox.SubAddons = DogRingGearBox.SubAddons.AddRangeToArray(NewArmours());
            DogRingGearBox.ChargesMax = 0;
            DogRingGearBox.Abilities = DogRingGearBox.Abilities.AddToArray(ExplodeOnDeath());

            DogRingGearBox.ViewElementDef.Description = new LocalizedTextBind("UI_JUNKER_ENGINE");
            Adjust_Cost();
        }

        public static AddonDef.SubaddonBind[] NewArmours()
        {
            //Clone default armours
            TacticalItemDef Kamikaze_Front_Armour = (TacticalItemDef)Repo.CreateDef("09564dd9-b2c0-4b73-87ec-efbadfb41e3d", DefaultHull[KaosBuggyMain.HullModules.Front]);
            TacticalItemDef Kamikaze_Back_Armour = (TacticalItemDef)Repo.CreateDef("e92416a0-7798-4405-b93a-4bc1b5ae20cb", DefaultHull[KaosBuggyMain.HullModules.Back]);
            TacticalItemDef Kamikaze_Left_Armour = (TacticalItemDef)Repo.CreateDef("2e52d585-0007-47ec-9309-4cea81c03076", DefaultHull[KaosBuggyMain.HullModules.Left]);
            TacticalItemDef Kamikaze_Right_Armour = (TacticalItemDef)Repo.CreateDef("1bf9a49c-b186-485b-acec-fe82636ba553", DefaultHull[KaosBuggyMain.HullModules.Right]);
            TacticalItemDef Kamikaze_Top_Armour = (TacticalItemDef)Repo.CreateDef("e93bf269-479a-467e-b5d3-465c976b091f", DefaultHull[KaosBuggyMain.HullModules.Top]);
            TacticalItemDef Kamikaze_LFT = (TacticalItemDef)Repo.CreateDef("90059d29-087c-4a9f-bbe5-a96ea9ee71fb", DefaultHull[KaosBuggyMain.HullModules.LFT]);
            TacticalItemDef Kamikaze_RFT = (TacticalItemDef)Repo.CreateDef("51ff052c-f3e1-4362-aac2-734a718a56b4", DefaultHull[KaosBuggyMain.HullModules.RFT]);
            TacticalItemDef Kamikaze_BT = (TacticalItemDef)Repo.CreateDef("36a2a07e-4514-47b6-93e5-560a3b21272d", DefaultHull[KaosBuggyMain.HullModules.BT]);

            //This bit is probably unnecessary overhead, but changing def names:
            Kamikaze_Front_Armour.name = "KaosBuggy_Kamikaze_Front_Armour_BodyPartDef";
            Kamikaze_Back_Armour.name = "KaosBuggy_Kamikaze_Back_Armour_BodyPartDef";
            Kamikaze_Left_Armour.name = "KaosBuggy_Kamikaze_Left_Armour_BodyPartDef";
            Kamikaze_Right_Armour.name = "KaosBuggy_Kamikaze_Right_Armour_BodyPartDef";
            Kamikaze_Top_Armour.name = "KaosBuggy_Kamikaze_Top_Armour_BodyPartDef";
            Kamikaze_LFT.name = "KaosBuggy_Kamikaze_LeftFrontTyre_BodyPartDef";
            Kamikaze_RFT.name = "KaosBuggy_Kamikaze_RightFrontTyre_BodyPartDef";
            Kamikaze_BT.name = "KaosBuggy_Kamikaze_BackTyre_BodyPartDef";

            //Adjust the armour on the new bodyparts
            Kamikaze_Front_Armour.Armor = 30f;
            Kamikaze_Back_Armour.Armor = Kamikaze_Left_Armour.Armor = Kamikaze_Right_Armour.Armor = Kamikaze_Top_Armour.Armor = 20f;
            Kamikaze_LFT.Armor = Kamikaze_RFT.Armor = Kamikaze_BT.Armor = 10f;

            //Need to clone the bodypartaspectdef of these parts and reduce Endurance to 0 on these clones:
            Kamikaze_Front_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("09d33b50-0e05-4114-835b-a94156b2e988", DefaultHull[KaosBuggyMain.HullModules.Front].BodyPartAspectDef);
            Kamikaze_Back_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("01630ac8-7262-4a2c-90ba-3674e6e138cb", DefaultHull[KaosBuggyMain.HullModules.Back].BodyPartAspectDef);
            Kamikaze_Left_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("0848e938-e039-4dc9-b768-a89f1870bd40", DefaultHull[KaosBuggyMain.HullModules.Left].BodyPartAspectDef);
            Kamikaze_Right_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("87c37b80-f977-4f97-9389-858e7af864fb", DefaultHull[KaosBuggyMain.HullModules.Right].BodyPartAspectDef);
            Kamikaze_Top_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("2d185772-cb1a-4788-bd05-d31fc1a7d8e5", DefaultHull[KaosBuggyMain.HullModules.Top].BodyPartAspectDef);
            Kamikaze_LFT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("3b335d51-a3d8-4e54-be77-d0163f9b3237", DefaultHull[KaosBuggyMain.HullModules.LFT].BodyPartAspectDef);
            Kamikaze_RFT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("add58bf4-d386-4138-a689-d6c464e68245", DefaultHull[KaosBuggyMain.HullModules.RFT].BodyPartAspectDef);
            Kamikaze_BT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("a5787a1f-5d6b-477f-9b46-e41645fd0608", DefaultHull[KaosBuggyMain.HullModules.BT].BodyPartAspectDef);

            Kamikaze_Front_Armour.BodyPartAspectDef.Endurance = 0f;
            Kamikaze_Back_Armour.BodyPartAspectDef.Endurance = 0f;
            Kamikaze_Left_Armour.BodyPartAspectDef.Endurance = 0f;
            Kamikaze_Right_Armour.BodyPartAspectDef.Endurance = 0f;
            Kamikaze_Top_Armour.BodyPartAspectDef.Endurance = 0f;
            Kamikaze_LFT.BodyPartAspectDef.Speed = Kamikaze_RFT.BodyPartAspectDef.Speed = Kamikaze_BT.BodyPartAspectDef.Speed = 0f;

            AddonDef.SubaddonBind[] Kamikaze_Armours = new AddonDef.SubaddonBind[]
            {
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_Front_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_Back_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_Left_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_Right_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_Top_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_LFT, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_RFT, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = Kamikaze_BT, AttachmentPointName = ""}
            };
            return Kamikaze_Armours;
        }

        public static RagdollDieAbilityDef ExplodeOnDeath()
        {
            //"SwarmerPoisonExplosion_Die_AbilityDef"
            RagdollDieAbilityDef PoisonSwarmerExplosion = (RagdollDieAbilityDef)Repo.GetDef("c1fbc786-4c79-ad84-7b74-1f44fecdb5bc");

            RagdollDieAbilityDef BuggyExplode = (RagdollDieAbilityDef)Repo.CreateDef("2b6349bc-7719-4767-ae27-ed0d2174dcfd", PoisonSwarmerExplosion); 
            BuggyExplode.name = "KaosBuggyExplosion_DieAbilityDef";
            BuggyExplode.AnimType = -1;

            //"Explode_EventDef"
            TacticalEventDef Explode_EventDef = (TacticalEventDef)Repo.GetDef("ac153f3d-a326-4684-fa3e-73291df8566c");
            BuggyExplode.EventOnActivate = Explode_EventDef;

            // DelayedEffectDef "ExplodingBarrel_ExplosionEffectDef" NEW -> MultiEffectDef "E_MultiEffect [ExplodingBarrel_ExplosionEffectDef]" NEW
            // Multi -> RemoveTacticalVoxelEffectDef "E_RemoveVoxels [ExplodingBarrel_ExplosionEffectDef]" NEW? && ExplosionEffectDef "E_ShrapnelExplosion [ExplodingBarrel_ExplosionEffectDef]" CLONE
            // Explosion -> DamageEffectDef "E_DamageEffect [ExplodingBarrel_ExplosionEffectDef]"
            
            ExplosionEffectDef BarrelShrapnel = (ExplosionEffectDef)Repo.GetDef("6a8f4097-9229-20a1-fcfc-2c6091428946");
            ExplosionEffectDef BuggyShrapnel = Repo.CreateDef<ExplosionEffectDef>("bb308f21-62c4-442d-97b4-3fdae397490a", BarrelShrapnel);
            BuggyShrapnel.name = "E_ShrapnelExplosion [KaosBuggyExplosion_DieAbilityDef]";
            BuggyShrapnel.Radius = 8f;

            //"E_ProjectileVisuals [PX_Scarab_Scorpio_GroundVehicleWeaponDef]"
            BuggyShrapnel.ObjectToSpawn = ((ProjectileDef)Repo.GetDef("d75d2f07-5f3b-336d-a390-728abe07757f")).HitEffect.EffectPrefab;

            DamageEffectDef BuggyDamageEffect = Repo.CreateDef<DamageEffectDef>("82a4a9ff-2d82-4832-b585-dd25bc4a7df2", BarrelShrapnel.DamageEffect);
            BuggyDamageEffect.name = "E_DamageEffect [KaosBuggyExplosion_DieAbilityDef]";
            BuggyDamageEffect.MinimumDamage = 100f;
            BuggyDamageEffect.MaximumDamage = 120f;
            BuggyDamageEffect.ObjectMultiplier = 3f;
            BuggyDamageEffect.ArmourShred = 20f;

            BuggyShrapnel.DamageEffect = BuggyDamageEffect;

            RemoveTacticalVoxelEffectDef BuggyRemoveVoxels = Repo.CreateDef<RemoveTacticalVoxelEffectDef>("a8a01616-b8d1-4b7e-a498-850da07b78b3");
            BuggyRemoveVoxels.name = "E_RemoveVoxels [KaosBuggyExplosion_DieAbilityDef]";
            BuggyRemoveVoxels.ApplicationConditions = new EffectConditionDef[]{};
            BuggyRemoveVoxels.VoxelsToRemove = PhoenixPoint.Tactical.Levels.Mist.TacticalVoxelType.All;
            BuggyRemoveVoxels.Radius = 8f;

            MultiEffectDef BuggyMultiEffect = Repo.CreateDef<MultiEffectDef>("fd248836-15bf-47a6-9037-3d6a38777e24");
            BuggyMultiEffect.name = "E_MultiEffect [KaosBuggyExplosion_DieAbilityDef]";
            BuggyMultiEffect.ApplicationConditions = new EffectConditionDef[]{};
            BuggyMultiEffect.PreconsiderApplicationConditions = false;
            BuggyMultiEffect.EffectDefs = new EffectDef[]
            {
                BuggyShrapnel,
                BuggyRemoveVoxels
            };

            DelayedEffectDef BuggyExplosionEffect = Repo.CreateDef<DelayedEffectDef>("54b49102-837c-47e0-be23-1c4f4a39340d");
            BuggyExplosionEffect.name = "KaosBuggyExplosion_ExplosionEffectDef";
            BuggyExplosionEffect.ApplicationConditions = new EffectConditionDef[]{};
            BuggyExplosionEffect.EffectDef = BuggyMultiEffect;

            BuggyExplode.DeathEffect = BuggyExplosionEffect;
            BuggyExplode.ViewElementDef = VED(PoisonSwarmerExplosion);
            return BuggyExplode;
        }
        
        private static TacticalAbilityViewElementDef VED(RagdollDieAbilityDef Template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("b52d7044-ea99-4b70-bd50-49e2f5ef7b42");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("b52d7044-ea99-4b70-bd50-49e2f5ef7b42", Template.ViewElementDef);
                //Taking Icon from "E_View [FirewormExplode_AbilityDef]"
                VED.SmallIcon = VED.LargeIcon = ((TacticalAbilityViewElementDef)Repo.GetDef("ac4a6676-fe5f-bc1c-0a63-1baf0859d67c")).SmallIcon;
                VED.name = "E_View [KaosBuggyExplosion_DieAbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("KB_KAMIKAZE_NAME");
                VED.Description = new LocalizedTextBind("KB_KAMIKAZE_DESC");
            }
            return VED;
        }

        private static void Adjust_Cost()
        {
            //"JetBoosters_MarketplaceItemOptionDef"
            GeoMarketplaceItemOptionDef MarketOption = (GeoMarketplaceItemOptionDef)Repo.GetDef("4b3f066b-0e75-1cc4-797b-c50912666690");
            MarketOption.MinPrice = 250f;
            MarketOption.MaxPrice = 400f;
        }
    }
}