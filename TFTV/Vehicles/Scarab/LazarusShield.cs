using Base.Defs;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Entities.Items.SkinData;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PRMBetterClasses;

namespace TFTVVehicleRework.Scarab
{
    public static class LazarusShield
    {
        private static readonly DefRepository Repo = ScarabMain.Repo;
        private static readonly RepositionAbilityDef VanishAbility = (RepositionAbilityDef)Repo.GetDef("36c30c9d-c1ae-3a74-d987-1b3dc9d3412a");
        private static readonly ApplyStatusAbilityDef StealthAbility = (ApplyStatusAbilityDef)Repo.GetDef("49aecd44-d46e-b314-1815-f373056bb822"); //"Stealth_AbilityDef"
        public static void Change()
        {
            GroundVehicleModuleDef FiberPlating = (GroundVehicleModuleDef)Repo.GetDef("983eb90b-29bf-15e4-fa76-d7f731069bd1"); //"PX_Scarab_Fiber_Plating_GroundVehicleModuleDef"
            FiberPlating.ViewElementDef.DisplayName1 = new LocalizedTextBind("PX_LAZARUS_NAME");
            FiberPlating.Armor = 0f;
            FiberPlating.BodyPartAspectDef.Stealth = 1f;
            FiberPlating.ManufactureTech = 30f;

            AdjustSubaddons(FiberPlating);
            FiberPlating.Abilities = new TacticalAbilityDef[]
            {
                ScarabVanish(),
                ScarabHiddenStatus()
            };

            Update_ResearchReqs(FiberPlating);
        }

        private static void AdjustSubaddons(GroundVehicleModuleDef module)
        {
            //Reset Carbon Plating HP/armour buff that it gets in vanilla;
            foreach (AddonDef.SubaddonBind addon in module.SubAddons)
            {
                TacticalItemDef BodyPart = (TacticalItemDef)addon.SubAddon;
                ((SimpleBodyPartSkinDataDef)BodyPart.SkinData).DisabledPrefab = new UnityEngine.AddressableAssets.AssetReferenceGameObject("");
                if (BodyPart.Guid == "c1935a9b-f2f7-88e4-fa88-5d471d653296") //"PX_Scarab_Fiber_Plating_Front_BodyPartDef"
                {
                    BodyPart.HitPoints = 300;
                    BodyPart.Armor = 40;
                }
                else if (BodyPart.Guid == "9886c434-73fc-8a64-2938-5dfce442a9f8") //"PX_Scarab_Fiber_Plating_Back_BodyPartDef"
                {
                    BodyPart.HitPoints = 220;
                    BodyPart.Armor = 20;
                }
                // "PX_Scarab_Fiber_Plating_Left_BodyPartDef" || "PX_Scarab_Fiber_Plating_Right_BodyPartDef"
                else if (BodyPart.Guid == "c6241ffb-2090-e6f4-5abb-545c3eb7b8d3" || BodyPart.Guid == "2529c93b-e199-b004-b972-ad0ca146cb76")
                {
                    BodyPart.HitPoints = 260;
                    BodyPart.Armor = 30;
                }
                else if (BodyPart.Guid == "46957959-6603-4e44-faca-58f8195eecd3" || //"PX_Scarab_Fiber_Plating_LeftFrontTyre_BodyPartDef"
                        BodyPart.Guid == "8bf1206d-b881-19b4-3b48-c26fe30988da" ||  //"PX_Scarab_Fiber_Plating_RightFrontTyre_BodyPartDef"
                        BodyPart.Guid == "a448a24d-37a2-6274-cbb8-8761ce4967d2" ||  //"PX_Scarab_Fiber_Plating_LeftBackTyre_BodyPartDef"
                        BodyPart.Guid == "ffc8e164-bafc-ecd4-9bfb-d96811b850f9")    //"PX_Scarab_Fiber_Plating_RightBackTyre_BodyPartDef"
                {
                    BodyPart.HitPoints = 200;
                    BodyPart.Armor = 20;
                }
            }
        }

        private static ApplyEffectAbilityDef ScarabVanish()
        {
            ApplyEffectAbilityDef Scarab_Vanish = Repo.CreateDef<ApplyEffectAbilityDef>("ecebe31a-e951-4bb4-a46f-a61eb02cd294");
            Helper.CopyFieldsByReflection(VanishAbility, Scarab_Vanish);
            Scarab_Vanish.name = "ScarabVanish_AbilityDef";
            //"_Self_TargetingDataDef"
            Scarab_Vanish.TargetingDataDef = (TacticalTargetingDataDef)Repo.GetDef("e1ac5f1b-c196-57c4-0a6b-223b33f7bca3");
            Scarab_Vanish.ViewElementDef = VanishVED(VanishAbility.ViewElementDef);
            Scarab_Vanish.SceneViewElementDef = null;
            Scarab_Vanish.UsesPerTurn = 1;
            Scarab_Vanish.WillPointCost = 0f;
            Scarab_Vanish.ActionPointCost = 0.5f;
            Scarab_Vanish.EffectDef = (HideEffectDef)Repo.GetDef("88c6e025-39b9-04d4-39a3-14ac659afdb1"); //"HideActorFromOtherFactions_EffectDef"
            // Scarab_Vanish.EffectDef = VanishEffects();
            // Scarab_Vanish.EffectDef = VanishAbility.PreparationActorEffectDef;
            Scarab_Vanish.DisablingStatuses = new StatusDef[]{HiddenStance()};
            Scarab_Vanish.ApplyOnStartTurn = false;
            Scarab_Vanish.ApplyToAllTargets = true;
            Scarab_Vanish.ApplyOnMove = false;
            Scarab_Vanish.CheckApplicationConditions = false;
            Scarab_Vanish.SimulatesDamage = false;
            Scarab_Vanish.MultipleTargetSimulation = false;

            return Scarab_Vanish;            
        }

        private static ApplyStatusAbilityDef ScarabHiddenStatus()
        {
            ApplyStatusAbilityDef ScarabStealth = (ApplyStatusAbilityDef)Repo.GetDef("44bab08f-444c-45ff-ae4f-0f73dd1dc255");
            if (ScarabStealth == null)
            {
                ScarabStealth = Repo.CreateDef<ApplyStatusAbilityDef>("44bab08f-444c-45ff-ae4f-0f73dd1dc255", StealthAbility);
                ScarabStealth.name = "ScarabStealth_AbilityDef";
                ScarabStealth.ViewElementDef = HiddenVED();
                ScarabStealth.StatusDef = FactionVisibilityStatus();
            }
            return ScarabStealth;
        }

        private static FactionVisibilityConditionStatusDef FactionVisibilityStatus()
        {   
            FactionVisibilityConditionStatusDef VisibilityStatus = (FactionVisibilityConditionStatusDef)Repo.GetDef("2c712822-130e-4bdb-9d93-d895060aab42");
            if (VisibilityStatus == null)
            {
                //"StealthVisibilityCondition_StatusDef"
                FactionVisibilityConditionStatusDef VisibilityCondition = (FactionVisibilityConditionStatusDef)Repo.GetDef("9eefa0d4-4834-b834-2944-d3cccf5c9b3d");
                VisibilityStatus = Repo.CreateDef<FactionVisibilityConditionStatusDef>("2c712822-130e-4bdb-9d93-d895060aab42", VisibilityCondition);
                VisibilityStatus.name = "Scarab_StealthVisibilityCondition_StatusDef";
                VisibilityStatus.HiddenStateStatusDef = VisibilityStatus.LocatedStateStatusDef = HiddenStance();
            }
            return VisibilityStatus;
        }

        private static StanceStatusDef HiddenStance()
        {
            StanceStatusDef HiddenStance = (StanceStatusDef)Repo.GetDef("bd37d91a-5905-4316-9843-eb4f5ae568a0");
            if (HiddenStance == null)
            {
                // "E_VanishedStatus [Vanish_AbilityDef]"
                StanceStatusDef VanishedStatus = (StanceStatusDef)Repo.GetDef("dd27cb97-d80e-3be2-d340-ffd669cad72b");
                HiddenStance = Repo.CreateDef<StanceStatusDef>("bd37d91a-5905-4316-9843-eb4f5ae568a0", VanishedStatus);
                HiddenStance.DurationTurns = -1;
                HiddenStance.ExpireOnEndOfTurn = false;
                HiddenStance.StatModifications = new ItemStatModification[]{};
                HiddenStance.EquipmentsStatModifications = new EquipmentItemTagStatModification[]{};
                HiddenStance.Visuals = HiddenVED();
            }
            return HiddenStance;
        }

        private static void Update_ResearchReqs(GroundVehicleModuleDef module)
        {
            // "PX_Alien_MistSentinel_ResearchDef"
            ResearchDef MistSentinel = (ResearchDef)Repo.GetDef("93451a57-bd6b-d200-7823-3be981b2d694");
            
            //"NJ_VehicleTech_ResearchDef_ManufactureResearchRewardDef_1"
            ManufactureResearchRewardDef NJVehicleTech = (ManufactureResearchRewardDef)Repo.GetDef("d197bc9e-9a4d-a2d6-eabf-0bb7b6a0a9a7");

            ManufactureResearchRewardDef Sentinel_ResearchReward = Repo.CreateDef<ManufactureResearchRewardDef>("6921285e-0a0e-41ca-9864-4f9958be9ca2", NJVehicleTech);
            Sentinel_ResearchReward.name = "MistSentinel_ManufactureResearchRewardDef";
            Sentinel_ResearchReward.Items = new ItemDef[]
            {
                module
            };
            MistSentinel.Unlocks = MistSentinel.Unlocks.AddToArray(Sentinel_ResearchReward);
        }

        // private static EffectDef VanishEffects()
        // {
        //     MultiEffectDef Effects = Repo.CreateDef<MultiEffectDef>("9753181e-7eff-46b0-9947-4ea552fefa44", VanishAbility.PreparationActorEffectDef);
        //     Effects.name = "E_MultiEffect [ScarabVanish_AbilityDef]";
        //     Effects.EffectDefs[0] = ScarabVanishStatus(Effects.EffectDefs[0]);
        //     return Effects;
        // }

        // private static StatusEffectDef ScarabVanishStatus(EffectDef template)
        // {
        //     StatusEffectDef VanishStatusEffect = Repo.CreateDef<StatusEffectDef>("2559fe10-9958-472f-b88c-b34a719cf83e", template);
        //     VanishStatusEffect.name = "E_ApplyVanishStatusEffect [ScarabVanish_AbilityDef]";
        //     VanishStatusEffect.StatusDef = Repo.CreateDef<StanceStatusDef>("bd37d91a-5905-4316-9843-eb4f5ae568a0",(template as StatusEffectDef).StatusDef);
        //     VanishStatusEffect.StatusDef.name = "E_VanishedStatus [ScarabVanish_AbilityDef]";
        //     (VanishStatusEffect.StatusDef as StanceStatusDef).DurationTurns = -1;
        //     (VanishStatusEffect.StatusDef as StanceStatusDef).ExpireOnEndOfTurn = false;
        //     (VanishStatusEffect.StatusDef as StanceStatusDef).StatModifications[0].Value = 0.1f;
        //     (VanishStatusEffect.StatusDef as StanceStatusDef).Visuals = VanishVED(VanishAbility.ViewElementDef);
        //     return VanishStatusEffect;
        // }
        private static TacticalAbilityViewElementDef VanishVED(TacticalAbilityViewElementDef template)
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("bbde0400-452d-4dc6-9c63-d6d574f7012b");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("bbde0400-452d-4dc6-9c63-d6d574f7012b", template);
                VED.name = "E_View [ScarabVanish_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("PX_VANISH_NAME");
                VED.Description = new LocalizedTextBind("PX_VANISH_DESC");
                VED.HideConfirmationButton = false;
                VED.ShowInInventoryItemTooltip = true;
            }
            return VED;
        }

        private static TacticalAbilityViewElementDef HiddenVED()
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("4257cc44-ff99-403f-b8b7-923e27e8ca6f");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("4257cc44-ff99-403f-b8b7-923e27e8ca6f", StealthAbility.ViewElementDef);
                VED.name = "E_View [ScarabHidden_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("PX_HIDDEN_NAME");
                VED.Description = new LocalizedTextBind("PX_HIDDEN_DESC");
                VED.HideFromPassives = true;
                VED.ShowInStatusScreen = true;
            }
            return VED;
        }
    }
}