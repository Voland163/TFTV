using Base.Defs;
using Base.Entities.Abilities;
using Base.UI;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;

namespace TFTVVehicleRework.Armadillo
{
    public static class ReinforcedPlating
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;
        public static void Change()
        {
            //"NJ_Armadillo_Reinforced_Plating_Hull_GroundVehicleModuleDef"
            GroundVehicleModuleDef ReinforcedPlating = (GroundVehicleModuleDef)Repo.GetDef("c9b3af2a-4fb9-9984-8a51-b3987465d83d");
            ReinforcedPlating.HandsToUse = 0;
            ReinforcedPlating.BodyPartAspectDef.BleedValue = 0f; //Not particularly important
            ReinforcedPlating.BodyPartAspectDef.Endurance = 25f;

            //Adjust BodyPartHP: All Parts gain 50HP
            TacticalItemDef FrontPlating = (TacticalItemDef)Repo.GetDef("7c57e82d-6320-9bd4-b98c-7ac307872ef1");
            FrontPlating.HitPoints = 400f;
            TacticalItemDef TopPlating = (TacticalItemDef)Repo.GetDef("dfb1b04a-7f1e-0064-f96d-30108c1ddbd8");
            TopPlating.HitPoints = 300f;
            TacticalItemDef LeftPlating = (TacticalItemDef)Repo.GetDef("627e3a68-cef6-0f94-792c-10e196195cc4");
            TacticalItemDef RightPlating = (TacticalItemDef)Repo.GetDef("4c9c6881-fc29-9f74-5b31-e4e52b72587e");
            LeftPlating.HitPoints = RightPlating.HitPoints = 350f;

            //Resets vanilla's Armour Piercing resistance
            ReinforcedPlating.Abilities = new AbilityDef[]
            {
                BodyPartHPBuff(),
            };

            ArmadilloMain.Update_Requirements(ReinforcedPlating);
        }

        private static TacticalAbilityDef BodyPartHPBuff()
        {
            //"Mutog_CanLeap_AbilityDef"
            PassiveModifierAbilityDef MutogCanLeap = (PassiveModifierAbilityDef)Repo.GetDef("af29cfc4-9b22-b774-682f-1dabc6ac13d3");

            PassiveModifierAbilityDef BodyPartBuff = Repo.CreateDef<PassiveModifierAbilityDef>("dc2051fc-4171-44df-a0a9-10e94d0e4f39", MutogCanLeap);
            BodyPartBuff.name = "ReinforcedPlating_BodyPartBuff_AbilityDef";
            BodyPartBuff.AnimType = -1;

            //"E_ViewElement [PsychicResistant_DamageMultiplierAbilityDef]"
            TacticalAbilityViewElementDef PierceResVED = (TacticalAbilityViewElementDef)Repo.GetDef("00431749-6f3f-d7e3-41a1-56e07706bd5a");
            TacticalAbilityViewElementDef BodyPartBuffVED = Repo.CreateDef<TacticalAbilityViewElementDef>("3077fb74-736d-4e48-9b86-cff74d82e1d6", PierceResVED);
            BodyPartBuffVED.name = "E_View [ReinforcedPlating_BodyPartBuff_AbilityDef]";
            BodyPartBuffVED.DisplayName1 = new LocalizedTextBind("NJ_REINFORCED_NAME");
            BodyPartBuffVED.Description = new LocalizedTextBind("NJ_REINFORCED_DESC");

            BodyPartBuff.ViewElementDef = BodyPartBuffVED;
            return BodyPartBuff;
        }
    }
}