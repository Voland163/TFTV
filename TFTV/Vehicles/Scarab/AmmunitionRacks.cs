using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;
using System.Collections.Generic;
using TFTVVehicleRework.Abilities;

namespace TFTVVehicleRework.Scarab
{
    public static class AmmunitionRacks
    {
        private static readonly DefRepository Repo = ScarabMain.Repo;

        public static readonly Dictionary<ScarabMain.HullModules, TacticalItemDef> DefaultHull = ScarabMain.DefaultHull;
        public static void Change()
        {
            //"PX_Scarab_Reinforced_Cargo_Racks_GroundVehicleModuleDef"
            GroundVehicleModuleDef CargoRacks = (GroundVehicleModuleDef)Repo.GetDef("038388bd-e3cf-2f54-5a5e-77a8351c1ec9");
            CargoRacks.ViewElementDef.DisplayName1 = new LocalizedTextBind("PX_AMMUNITION_NAME");
            CargoRacks.Armor = -10; //For Geo/Marketplace UI
            CargoRacks.BodyPartAspectDef.StatModifications = new ItemStatModification[]{}; //Resets vanilla inventory slot bonus
            CargoRacks.SubAddons = CargoRacks.SubAddons.AddRangeToArray(NewArmours());
            CargoRacks.Abilities = new TacticalAbilityDef[]
            {
                ScarabReload()
            };
        }
        
        internal static AddonDef.SubaddonBind[] NewArmours()
        {
            //Clone default armours
            TacticalItemDef AmmoRack_Front_Armour = (TacticalItemDef)Repo.CreateDef("20e93a8f-1f84-4942-a370-dedd480466ec", DefaultHull[ScarabMain.HullModules.Front]);
            TacticalItemDef AmmoRack_Back_Armour = (TacticalItemDef)Repo.CreateDef("b0aecadc-241f-4e86-a56a-b7af26ac1b12", DefaultHull[ScarabMain.HullModules.Back]);
            TacticalItemDef AmmoRack_Left_Armour = (TacticalItemDef)Repo.CreateDef("d5569c47-b26c-4b2f-9529-ae0375e81bd2", DefaultHull[ScarabMain.HullModules.Left]);
            TacticalItemDef AmmoRack_Right_Armour = (TacticalItemDef)Repo.CreateDef("c6433fdb-4758-41e8-b166-8a56e19a43de", DefaultHull[ScarabMain.HullModules.Right]);
            TacticalItemDef AmmoRack_LFT = (TacticalItemDef)Repo.CreateDef("64370279-15bd-45b1-b328-930d4fdcc4f5", DefaultHull[ScarabMain.HullModules.LFT]);
            TacticalItemDef AmmoRack_RFT = (TacticalItemDef)Repo.CreateDef("2d928f80-4e3e-4516-95a7-88d60a617587", DefaultHull[ScarabMain.HullModules.RFT]);
            TacticalItemDef AmmoRack_LBT = (TacticalItemDef)Repo.CreateDef("7a57f81a-2ca4-409d-b704-bbf8f34d5ea2", DefaultHull[ScarabMain.HullModules.LBT]);
            TacticalItemDef AmmoRack_RBT = (TacticalItemDef)Repo.CreateDef("e0cbe9e3-cdef-4274-a00a-f0f1e5b21cc9", DefaultHull[ScarabMain.HullModules.RBT]);

            //This bit is probably unnecessary overhead, but changing def names:
            AmmoRack_Front_Armour.name = "PX_Scarab_AmmoRack_Front_Armour_BodyPartDef";
            AmmoRack_Back_Armour.name = "PX_Scarab_AmmoRack_Back_Armour_BodyPartDef";
            AmmoRack_Left_Armour.name = "PX_Scarab_AmmoRack_Left_Armour_BodyPartDef";
            AmmoRack_Right_Armour.name = "PX_Scarab_AmmoRack_Right_Armour_BodyPartDef";
            AmmoRack_LFT.name = "PX_Scarab_AmmoRack_LeftFrontTyre_BodyPartDef";
            AmmoRack_RFT.name = "PX_Scarab_AmmoRack_RightFrontTyre_BodyPartDef";
            AmmoRack_LBT.name = "PX_Scarab_AmmoRack_LeftBackTyre_BodyPartDef";
            AmmoRack_RBT.name = "PX_Scarab_AmmoRack_RightBackTyre_BodyPartDef";

            //Adjust the armour on the new bodyparts
            AmmoRack_Front_Armour.Armor = 30;
            AmmoRack_Back_Armour.Armor = 10;
            AmmoRack_Left_Armour.Armor = AmmoRack_Right_Armour.Armor = 20;
            AmmoRack_LFT.Armor = AmmoRack_LBT.Armor = AmmoRack_RFT.Armor = AmmoRack_RBT.Armor = 10;
            
            //New armours need to set WeakAddons to false to avoid Softlocking when used with Stability Module. Likely due to Stability Module not hiding subaddons.
            AmmoRack_Front_Armour.WeakAddon = AmmoRack_Back_Armour.WeakAddon = AmmoRack_Left_Armour.WeakAddon = AmmoRack_Right_Armour.WeakAddon = false;
            AmmoRack_LFT.WeakAddon = AmmoRack_RFT.WeakAddon = AmmoRack_LBT.WeakAddon = AmmoRack_RBT.WeakAddon = false;
            
            //Need to clone the bodypartaspectdef of these parts and reduce Endurance to 0 on these clones:
            AmmoRack_Front_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("5b54fd09-522d-475c-9c04-0d9fd410e2e6", DefaultHull[ScarabMain.HullModules.Front].BodyPartAspectDef);
            AmmoRack_Back_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("aa7971fa-99d6-4fe4-b9f7-97ef99d2093f", DefaultHull[ScarabMain.HullModules.Back].BodyPartAspectDef);
            AmmoRack_Left_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("ebbe3645-9529-4e51-b432-b8e93361a401", DefaultHull[ScarabMain.HullModules.Left].BodyPartAspectDef);
            AmmoRack_Right_Armour.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("5ff86aff-f288-4cbe-b92d-24a0277e6ccf", DefaultHull[ScarabMain.HullModules.Right].BodyPartAspectDef);
            AmmoRack_LFT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("1478a149-453f-478b-ad66-f13f64266e9c", DefaultHull[ScarabMain.HullModules.LFT].BodyPartAspectDef);
            AmmoRack_LBT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("b7ce5a6a-1326-436f-b784-50ecbfd7291a", DefaultHull[ScarabMain.HullModules.LBT].BodyPartAspectDef);
            AmmoRack_RFT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("dbf1242b-357d-4fc4-9225-f2a481415fe3", DefaultHull[ScarabMain.HullModules.RFT].BodyPartAspectDef);
            AmmoRack_RBT.BodyPartAspectDef = (BodyPartAspectDef)Repo.CreateDef("afacf3d3-a258-4358-9d4f-cda8038875ec", DefaultHull[ScarabMain.HullModules.RBT].BodyPartAspectDef);

            //Ensure that clones do not add any stats to the vehicle
            AmmoRack_Front_Armour.BodyPartAspectDef.Endurance = 0;
            AmmoRack_Back_Armour.BodyPartAspectDef.Endurance = 0;
            AmmoRack_Left_Armour.BodyPartAspectDef.Endurance = 0;
            AmmoRack_Right_Armour.BodyPartAspectDef.Endurance = 0;
            AmmoRack_LFT.BodyPartAspectDef.Speed = 0;
            AmmoRack_LBT.BodyPartAspectDef.Speed = 0;
            AmmoRack_RFT.BodyPartAspectDef.Speed = 0;
            AmmoRack_RBT.BodyPartAspectDef.Speed = 0;

            //Notes: 
            // - No need to change required slots or provided subaddon slots for the armours. 
            // - Possibly doesn't need to change any of the "Weakaddons" settings either.
            
            //Adding new armours to the module's Subaddons
            AddonDef.SubaddonBind[] AmmoRack_Armours = new AddonDef.SubaddonBind[]
            {
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_Front_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_Back_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_Left_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_Right_Armour, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_LFT, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_RFT, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_LBT, AttachmentPointName = ""},
                new AddonDef.SubaddonBind{SubAddon = AmmoRack_RBT, AttachmentPointName = ""}
            };
            return AmmoRack_Armours;
        }

        internal static FreeReloadAbilityDef ScarabReload()
        {
            ReloadAbilityDef Reload = (ReloadAbilityDef)Repo.GetDef("3d6a71c7-c27b-5374-5a51-0ba31db93d41");
    
            FreeReloadAbilityDef ScarabReload = Repo.CreateDef<FreeReloadAbilityDef>("9461314d-7d6e-48c8-8298-9752e39d0f0e");
            Helper.CopyFieldsByReflection(Reload, ScarabReload);
            ScarabReload.name = "ScarabFreeReload_AbilityDef";
            ScarabReload.ResourcePath = "Defs/Tactical/Actors/_Common/Abilities/FreeReload_AbilityDef";
            ScarabReload.InputAction = "";
            ScarabReload.ActionPointCost = 0.5f;

            ScarabReload.ViewElementDef = (TacticalAbilityViewElementDef)Repo.CreateDef("e91bdf96-62c5-4f5d-b85b-b21ac077fef2", Reload.ViewElementDef);
            ScarabReload.ViewElementDef.name = "E_View [ScarabFreeReload_AbilityDef]";
            ScarabReload.ViewElementDef.Description = new LocalizedTextBind("UI_RELOAD_DESC");
            ScarabReload.ViewElementDef.ShowInInventoryItemTooltip = true;

            return ScarabReload;
        }
    }
}