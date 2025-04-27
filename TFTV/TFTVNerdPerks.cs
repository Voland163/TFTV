using Base.Defs;
using Base.Levels;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static UnityEngine.ParticleSystem;

namespace TFTV
{
    class TFTVNerdPerks
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;


        public static PassiveModifierAbilityDef [] recon = new PassiveModifierAbilityDef[] {};
        public static PassiveModifierAbilityDef [] vehicleOperator = new PassiveModifierAbilityDef[] { }; //dummy ability
        public static PassiveModifierAbilityDef[] dealMaker = new PassiveModifierAbilityDef[] { }; //dummy ability
        public static HealAbilityDef [] machineFixer = new HealAbilityDef[] { };
        public static HealAbilityDef[] fieldEngineer = new HealAbilityDef[] { };
        public static DamageMultiplierAbilityDef[] anomalyInvestigator = new DamageMultiplierAbilityDef[] { };
        public static ApplyStatusAbilityDef[] xenobiologist = new ApplyStatusAbilityDef[] { };
       

        /*
Types 	Geoscape 	Tactical 	
Recon 	Exploration 30min faster 	Gets 10% more healing 	//PassiveModifierAbilityDef Nurse = DefCache.GetDef<PassiveModifierAbilityDef>("Helpful_AbilityDef");
Vehicle Operator 	7% aircraft speed increase	Extra movement to ground vehicle if inside (+3)	
Machine Fixer	10% less geo vehicle maintenance	Can restore broken vehicle parts with +10 HP	
Anomaly Investigator	10% less penalty/10% more buff from flying in Mist	10% res to psychic scream (lowers damage) and MC (increases cost)	
Deal Maker	as if + 10 rep when interacting with haven leaders	(rank 1: Less time needed to convince NPC on tactical maps, rank 2: + 1 WP to all for extracting civilians, rank 3: + 2 WP to all for extracting civilians	
Xenobiologist	generates 1RP per hour	Extra +3% damage to vivisected Pandorans	
Field Engineer	generates 1MP per hour	Can repair 4 armor	
         */



        internal class Defs 
        { 
            public static void CreateDefs()
            {
                try 
                {
                    CreateNerdAbilities();  
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateNerdAbilities() 
            {
                try 
                {
                    CreateReconAbility();
                    CreateVehicleOperatorAbility();
                    CreateDealMakerAbility();
                    CreateMachineFixerAbility();
                    CreateFieldEngineerAbility();
                    CreateAnomalyInvestigatorAbility();
                    CreateXenobiologistAbility();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateReconAbility() 
            {
                try
                {
                    string name = "RECON";
                    List<string> guids = new List<string>()
{
    "f3b27157-4d84-4e8d-8c4b-1a2d3f2e0457",
    "2a14f859-99f7-49ef-9023-3cd5eaa87372",
    "9b7c3fa0-61b7-4c6a-8c6c-9c52d49c0372",
    "6d178f13-dfd4-40f2-bb0e-40b2e9f9c142",
    "4fe9dc6d-5c17-44cc-91b5-2e9165781dc0",
    "edc26cb5-f47a-4370-b7cb-058d4033c68a",
    "5a1a5d35-812a-42f3-a7cf-84a36a40e9df",
    "ad1b476e-b327-4d7b-b65d-5d13e07e1a08",
    "e3760e87-054d-43d8-8794-13c63aafae35"
};

                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.StatModifications = new ItemStatModification[]
                        {
                        new ItemStatModification(){
                            TargetStat = StatModificationTarget.BonusHealValue,
                            Value = 1.1f*x,
                            Modification = Base.Entities.Statuses.StatModificationType.Multiply
                        }

                        };

                        recon = recon.AddToArray(newAbility);
                    }
                 
                
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateDealMakerAbility()
            {
                try
                {
                    string name = "DEAL_MAKER";
                    List<string> guids = new List<string>()
{
    "d842e157-6f1f-4261-a78c-56b6b0e3cc48",
    "9cf4c0b2-2ec6-4ea4-92a3-221e7e55f19b",
    "f7600f5c-2440-48e2-906b-84c3b9d56160",
    "cb11d6bc-765a-4b8e-89a7-d7fc594c1b95",
    "33b30994-f0e6-4c87-bdf9-7858cd252b1e",
    "8ee76e52-fd1a-4de1-8e82-b229f46b2453",
    "b06a6a04-6aa1-48ec-bf4e-d90a36a83163",
    "4e7ed943-5ae6-4c17-87f2-91e9cc4ac66d",
    "10fc3720-42c2-4308-a0a1-03fdc1e340b1"
};



                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.StatModifications = new ItemStatModification[] { };
                        

                        dealMaker = dealMaker.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateVehicleOperatorAbility()
            {
                try
                {
                    string name = "VEHICLE_OPERATOR";
                    List<string> guids = new List<string>()
{
    "b7d92c4d-4df3-4d94-bc4c-2dcf7e5ecb4e",
    "03c1d587-5a71-4f44-bfc3-2a1a64b23c0f",
    "e453d31a-9303-4b7f-86b5-90a0b5df3f71",
    "7806b8df-fb29-4e26-b5fc-07db7ef1fa84",
    "15b8b14f-f8f0-493f-872e-86568e4a7536",
    "91b2b60e-f248-4cd2-bc1c-e64c81e2a70a",
    "6e88e419-8c6a-46cc-89b3-5be9791b0f4c",
    "fc47cf45-29a9-43a6-9ef6-d9e211731bd6",
    "54c4f9bb-f0a5-4297-982c-f4bb502c5672"
};


                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        PassiveModifierAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.StatModifications = new ItemStatModification[] { };


                        vehicleOperator = vehicleOperator.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateMachineFixerAbility()
            {
                try
                {
                    string name = "MACHINE_FIXER";
                    List<string> guids = new List<string>()
{
    "66d913f3-d315-4cc4-8610-b4e7ab1e92e0",
    "ca93c4cf-9f0e-4b93-8662-c78dfc41f37d",
    "de0cbd62-7b7f-42e4-81a2-4cc34e40c9d2",
    "798354de-764f-48ed-9826-e50e6b7d6db5",
    "3f639b18-1cc1-41a2-b19e-5158180faadc",
    "1d0fa8ef-13f0-4e37-88e1-60184fe7348f",
    "a23ab037-c162-45f6-a9fb-45fbc72a4b93",
    "58c8d013-46e2-4dd3-9731-7076c5fe4696",
    "a4074419-e80e-4989-a5aa-b59a1551f2d9"
};



                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    HealAbilityDef source = DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        HealAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.ActorTags = new GameTagDef[] { };
                        newAbility.EquipmentTags = new GameTagDef[] { };
                       


                        machineFixer = machineFixer.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateFieldEngineerAbility()
            {
                try
                {
                    string name = "FIELD_ENGINEER";
                    List<string> guids = new List<string>()
{
    "cd5f8124-bc59-40c4-9dbe-6b8b099e62dc",
    "2f3e24e1-43d4-45fa-8eaf-32dc94cba9fc",
    "ea388f4f-2ac6-4204-8bc9-1a255e2f4a7b",
    "9b7f8a1e-1302-4221-bf3f-43a78ef22e5a",
    "bdfc2e3a-f46c-4cfc-a154-7b88e50d3a62",
    "81f9f473-b33b-44ad-8b9b-68914de7e95a",
    "1e4b2d3b-eebb-4c95-85dc-f6f7d514bce4",
    "6e74c0b6-4ed1-4de9-8547-f947a6a0578a",
    "3b998180-8914-493b-a55f-3e380e07c730"
};




                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    HealAbilityDef source = DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        HealAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.ActorTags = new GameTagDef[] { };
                        newAbility.EquipmentTags = new GameTagDef[] { };



                        fieldEngineer = fieldEngineer.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateAnomalyInvestigatorAbility()
            {
                try
                {
                    string name = "ANOMALY_INVESTIGATOR";
                    List<string> guids = new List<string>()
{
    "47cf9f78-1899-4b39-b70a-38c5d4b0b5b1",
    "c0f16efb-d4f2-4f4c-9bfa-03e8ad6c0f3f",
    "ef3a12c1-e260-4c90-b144-672b10e26a7c",
    "bd6c2fcb-9d68-4f48-9e1e-cb6a73f3fd38",
    "2d2d88c5-9f4e-47a4-8fa9-6c3edca0b4a6",
    "9d1be87a-4e09-4dd2-8894-ecb9e63783df",
    "01ecf0b3-4e86-4f36-b5c4-fb93ac1e4744",
    "ae28c4c1-ff1a-4a64-85cf-9eb00fcb7c78",
    "72462d60-36fa-4c1a-b17c-39f5eac04831"
};


                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("FieldMedic_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        DamageMultiplierAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.ActorTags = new GameTagDef[] { };
                        newAbility.EquipmentTags = new GameTagDef[] { };

                        anomalyInvestigator = anomalyInvestigator.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateXenobiologistAbility()
            {
                try
                {
                    string name = "XENOBIOLOGIST";
                    List<string> guids = new List<string>()
{
    "58f760b3-3ef7-4cf4-9ec6-527c17608d6c",
    "0a2a7682-45bc-4cf2-bb9a-ecbd76086b6e",
    "b1c6a579-0556-4e65-bcfd-015cf8a053a3",
    "34a2d2ed-6a91-4d58-b1a2-22fdbdf416e2",
    "d32f4b5a-019c-4adf-ae49-2f94e279d9c4",
    "e944db5a-9f3c-4e29-a7c1-999f235d82b1",
    "f3e9d4fd-746e-4989-a91f-f3cecb2a4a1f",
    "a5c823b0-56d4-4a65-a4a6-1dd7f44c86e5",
    "8a63ed4d-21a0-4e91-a64f-21fba0e0c2b3"
};



                    string keyTitle = $"KEY_NERD_PERK_TITLE_{name}";
                    string keyDescription = $"KEY_NERD_PERK_DESCRIPTION_{name}";


                    ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("FieldMedic_AbilityDef");

                    for (int x = 0; x < 3; x++)
                    {
                        ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                            source,
                            guids[x],
                            name + x.ToString());

                        newAbility.ViewElementDef = Helper.CreateDefFromClone(
                            source.ViewElementDef,
                            guids[3 + x],
                            name + x.ToString());

                        newAbility.ViewElementDef.DisplayName1.LocalizationKey = keyTitle + x.ToString();
                        newAbility.ViewElementDef.Description.LocalizationKey = keyDescription + x.ToString();
                        newAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile($"UI_NerdAbilitiesIcon_{name}_{x}.png");
                        newAbility.ViewElementDef.SmallIcon = newAbility.ViewElementDef.LargeIcon;

                        newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                            source.CharacterProgressionData,
                            guids[6 + x],
                            name + x.ToString());

                        newAbility.ActorTags = new GameTagDef[] { };
                        newAbility.EquipmentTags = new GameTagDef[] { };

                        xenobiologist = xenobiologist.AddToArray(newAbility);
                    }


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }


    }
}
