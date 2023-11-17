using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;

namespace TFTVVehicleRework.Armadillo
{
    public static class ImprovedTraction
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;
        public static void Change()
        {
            //"NJ_Armadillo_Bi-Turbo_Engine_GroundVehicleModuleDef"
            GroundVehicleModuleDef BiTurbo = (GroundVehicleModuleDef)Repo.GetDef("79f4699c-5ce9-2db4-982b-8bd30131a963");
            BiTurbo.HandsToUse = 0;
            BiTurbo.BodyPartAspectDef.Speed = -4f;
            BiTurbo.SubAddons = BiTurbo.SubAddons.AddRangeToArray(NewArmours());
            BiTurbo.ManufactureMaterials = 300f;

            BiTurbo.Abilities = new AbilityDef[]
            {
                ArmadilloGooImmunity(),
                BodyPartHPBuff()
            };

            ArmadilloMain.Update_Requirements(BiTurbo);
        }

        private static GooDamageMultiplierAbilityDef ArmadilloGooImmunity()
        {
            // "GooImmunity_AbilityDef"
            GooDamageMultiplierAbilityDef GooImmunity = (GooDamageMultiplierAbilityDef)Repo.GetDef("f7bfd2ad-6534-65f4-180b-479a7a63dfff");
            GooDamageMultiplierAbilityDef ArmadilloGooImmunity = Repo.CreateDef<GooDamageMultiplierAbilityDef>("46c257f4-ae90-4a01-9327-fe3f448937aa", GooImmunity);
            ArmadilloGooImmunity.name = "ArmadilloGooImmunity_AbilityDef";
            ArmadilloGooImmunity.NavAreas = new string[]{};
            return ArmadilloGooImmunity;
        }

        private static TacticalAbilityDef BodyPartHPBuff()
        {
            //"Mutog_CanLeap_AbilityDef"
            PassiveModifierAbilityDef MutogCanLeap = (PassiveModifierAbilityDef)Repo.GetDef("af29cfc4-9b22-b774-682f-1dabc6ac13d3");

            PassiveModifierAbilityDef BodyPartBuff = Repo.CreateDef<PassiveModifierAbilityDef>("08e7d926-cf82-435a-ac6c-8743d6f33531", MutogCanLeap);
            BodyPartBuff.name = "ImprovedTraction_BodyPartBuff_AbilityDef";
            BodyPartBuff.AnimType = -1;

            //"E_ViewElement [PsychicResistant_DamageMultiplierAbilityDef]"
            TacticalAbilityViewElementDef PierceResVED = (TacticalAbilityViewElementDef)Repo.GetDef("00431749-6f3f-d7e3-41a1-56e07706bd5a");
            TacticalAbilityViewElementDef BodyPartBuffVED = Repo.CreateDef<TacticalAbilityViewElementDef>("3db5a192-eb2b-487c-ba6c-ca4472e1f6b9", PierceResVED);
            BodyPartBuffVED.name = "E_View [ImprovedTraction_BodyPartBuff_AbilityDef]";
            BodyPartBuffVED.DisplayName1 = new LocalizedTextBind("NJ_TRACTION_NAME");
            BodyPartBuffVED.Description = new LocalizedTextBind("NJ_TRACTION_DESC");
            BodyPartBuffVED.SmallIcon = BodyPartBuffVED.LargeIcon = Helper.CreateSpriteFromImageFile("car_wheel.png");

            BodyPartBuff.ViewElementDef = BodyPartBuffVED;
            return BodyPartBuff;
        }
        private static AddonDef.SubaddonBind[] NewArmours()
        {
            // Clone wheels for +10 armour and +100HP
            TacticalItemDef LFT = (TacticalItemDef)Repo.GetDef("c8104a5d-c8f0-2634-796e-06bbc56483e2"); // LeftFrontTyre
            TacticalItemDef LBT = (TacticalItemDef)Repo.GetDef("bb2d73ac-5ad8-6b84-fae8-09a8d5ad66b4"); // LeftBackTyre
            TacticalItemDef RFT = (TacticalItemDef)Repo.GetDef("1d02cd5b-7618-a144-98a3-22c266e0ad47"); // RightFrontTyre
            TacticalItemDef RBT = (TacticalItemDef)Repo.GetDef("fc744614-d7aa-f0f4-3a08-39459fbf438a"); // RightBackTyre

            TacticalItemDef LF_Traction = Repo.CreateDef<TacticalItemDef>("0be0ed2a-d9fa-44fc-ba15-770b0cce21fc", LFT);
            TacticalItemDef LB_Traction = Repo.CreateDef<TacticalItemDef>("db89f124-6e28-4083-9bfd-fe2c0f04a69c", LBT);
            TacticalItemDef RF_Traction = Repo.CreateDef<TacticalItemDef>("ca52112b-d2c8-44c1-a0de-a214215aa18c", RFT);
            TacticalItemDef RB_Traction = Repo.CreateDef<TacticalItemDef>("d83939e9-dcd0-4dd8-8e07-c446808d3ed9", RBT);

            //Wheels stack armour and hitpoints
            LF_Traction.HitPoints = LB_Traction.HitPoints = RF_Traction.HitPoints = RB_Traction.HitPoints = 100f;
            LF_Traction.Armor = LB_Traction.Armor = RF_Traction.Armor = RB_Traction.Armor = 10f;

            LF_Traction.BodyPartAspectDef = Repo.CreateDef<BodyPartAspectDef>("bbfbda93-fac3-4c85-b641-f6d4914fa678", LFT.BodyPartAspectDef);
            LB_Traction.BodyPartAspectDef = Repo.CreateDef<BodyPartAspectDef>("eaab3ca3-1320-4a82-92e0-fe633fc6c3ae", LBT.BodyPartAspectDef);
            RF_Traction.BodyPartAspectDef = Repo.CreateDef<BodyPartAspectDef>("207046cf-1d6a-419c-a95a-10f1a5d755ff", RFT.BodyPartAspectDef);
            RB_Traction.BodyPartAspectDef = Repo.CreateDef<BodyPartAspectDef>("82ef41df-e74c-45b3-8679-dfd88c967983", RBT.BodyPartAspectDef);

            LF_Traction.BodyPartAspectDef.Speed = LB_Traction.BodyPartAspectDef.Speed = RF_Traction.BodyPartAspectDef.Speed = RB_Traction.BodyPartAspectDef.Speed = 0f;

            //Change Weakaddon setting so that the clones don't break mesh properties
            LFT.WeakAddon = LBT.WeakAddon = RFT.WeakAddon = RBT.WeakAddon = true;

            AddonDef.SubaddonBind[] Traction_Tires = new AddonDef.SubaddonBind[]
            {
                new AddonDef.SubaddonBind{SubAddon = LF_Traction, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = LB_Traction, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = RF_Traction, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = RB_Traction, AttachmentPointName = ""},
            };    
            return Traction_Tires;
        }
    }
}
