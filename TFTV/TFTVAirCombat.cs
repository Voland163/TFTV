using Base;
using Base.Core;
using Base.Defs;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Interception;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVAirCombat
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void ModifyAirCombatDefs()
        {
            try
            {
                //implementing Belial's proposal: 

                // ALN_VoidChamber_VehicleWeaponDef  Fire rate increased 20s-> 10s, Damage decreased 400-> 200
                // ALN_Spikes_VehicleWeaponDef	Changed to Psychic Guidance (from Visual Guidance)
                // ALN_Ram_VehicleWeaponDef Changed to Psychic Guidance(from Visual Guidance), HP 250-> 350

                // PX_Afterburner_GeoVehicleModuleDef Charges 5-> 3
                // PX_Flares_GeoVehicleModuleDef 5-> 3
                //  AN_ECMJammer_GeoVehicleModuleDef Charges 5-> 3

                //PX_ElectrolaserThunderboltHC9_VehicleWeaponDef Accuracy 95 % -> 85 %
                // PX_BasicMissileNomadAAM_VehicleWeaponDef 80 % -> 70 %
                // NJ_RailgunMaradeurAC4_VehicleWeaponDef 80 % -> 70 %
                //SY_LaserGunArtemisMkI_VehicleWeaponDef Artemis Accuracy 95 % -> 85 %


                GeoVehicleWeaponDef voidChamberWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_VoidChamber_VehicleWeaponDef"));
                GeoVehicleWeaponDef spikesWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Spikes_VehicleWeaponDef"));
                GeoVehicleWeaponDef ramWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Ram_VehicleWeaponDef"));
                GeoVehicleWeaponDef thunderboltWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_ElectrolaserThunderboltHC9_VehicleWeaponDef"));
                GeoVehicleWeaponDef nomadWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_BasicMissileNomadAAM_VehicleWeaponDef"));
                GeoVehicleWeaponDef railGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("NJ_RailgunMaradeurAC4_VehicleWeaponDef"));
                GeoVehicleWeaponDef laserGunWDef = Repo.GetAllDefs<GeoVehicleWeaponDef>().FirstOrDefault(gvw => gvw.name.Equals("SY_LaserGunArtemisMkI_VehicleWeaponDef"));

                GeoVehicleModuleDef afterburnerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Afterburner_GeoVehicleModuleDef"));
                GeoVehicleModuleDef flaresMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("PX_Flares_GeoVehicleModuleDef"));
                GeoVehicleModuleDef jammerMDef = Repo.GetAllDefs<GeoVehicleModuleDef>().FirstOrDefault(gvw => gvw.name.Equals("AN_ECMJammer_GeoVehicleModuleDef"));

                voidChamberWDef.ChargeTime = 10.0f;
                var voidDamagePayload = voidChamberWDef.DamagePayloads[0].Damage;
                voidChamberWDef.DamagePayloads[0] = new GeoWeaponDamagePayload { Damage = voidDamagePayload, Amount = 200 };

                spikesWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.Guidence = GeoVehicleWeaponGuidence.Psychic;
                ramWDef.HitPoints = 350;
                thunderboltWDef.Accuracy = 85;
                nomadWDef.Accuracy = 70;
                railGunWDef.Accuracy = 70;
                laserGunWDef.Accuracy = 85;

                afterburnerMDef.AmmoCount = 3;
                flaresMDef.AmmoCount = 3;
                jammerMDef.AmmoCount = 3;

                //Changing ALN Abbadon research so they appear only in Third Act, or After ODI reaches apex
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   Repo.GetAllDefs<EncounterVariableResearchRequirementDef>().
                   FirstOrDefault(ged => ged.name.Equals("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0"));

                //Creating new Research Requirements, each requiring a variable to be triggered  
                EncounterVariableResearchRequirementDef variableResReqAbbadon = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqDef");
                variableResReqAbbadon.VariableName = "ThirdActStarted";
                EncounterVariableResearchRequirementDef variableResReqAbbadonAlt = Helper.CreateDefFromClone(sourceVarResReq, "F8D9463A-69C5-47B1-B52A-061D898CEEF8", "AbbadonResReqAltDef");
                variableResReqAbbadonAlt.VariableName = "ODI_Complete";
                //Altering researchDef, requiring Third Act to have started and adding an alternative way of revealing research if ODI is completed 
                ResearchDef aLN_Large_Flyer_ResearchDef = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(gvw => gvw.name.Equals("ALN_Large_Flyer_ResearchDef"));
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Operation = ResearchContainerOperation.ANY;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container[0].Requirements.AddItem(variableResReqAbbadon);
                ReseachRequirementDefOpContainer[] reseachRequirementDefOpContainers = new ReseachRequirementDefOpContainer[1];
                ResearchRequirementDef[] researchRequirementDefs = new ResearchRequirementDef[1];
                researchRequirementDefs[0] = variableResReqAbbadonAlt;

                reseachRequirementDefOpContainers[0].Requirements = researchRequirementDefs;
                aLN_Large_Flyer_ResearchDef.RevealRequirements.Container.AddRangeToArray(reseachRequirementDefOpContainers);
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
    [HarmonyPatch(typeof(InterceptionGameController), "DisengagePlayer")]
    public static class InterceptionGameController_DisengagePlayer_DisengageDestroyRandomWeapon_patch
    {
        public static bool Prepare()
        {
            TFTVConfig Config = new TFTVConfig();
            return Config.ActivateAirCombatChanges;
        }


        public static void Postfix(InterceptionGameController __instance)
        {
            try
            {
                int numberOfActiveWeaponsEnemy = 0;

                for (int i = 0; i < __instance.EnemyAircraft.Weapons.Count(); i++)
                {
                    InterceptionAircraftWeapon enemyWeapon = __instance.EnemyAircraft.GetWeapon(i);
                    if (enemyWeapon != null && !enemyWeapon.IsDisabled)
                    {
                        TFTVLogger.Always("Weapon " + i + "is " + enemyWeapon.WeaponDef.GetDisplayName().LocalizeEnglish());
                        numberOfActiveWeaponsEnemy++;
                    }
                }

                TFTVLogger.Always("Number of active enemy weapons: " + numberOfActiveWeaponsEnemy);

                int num = UnityEngine.Random.Range(0, 100 + 25 * numberOfActiveWeaponsEnemy);
                TFTVLogger.Always("Rol: " + num);
                if (num > 100)
                {
                    GeoVehicle playerCraft = __instance.CurrentMission.PlayerAircraft.Vehicle;
                    GeoVehicleEquipment randomWeapon = playerCraft.Weapons.ToList().GetRandomElement();
                    playerCraft.RemoveEquipment(randomWeapon);
                    GameUtl.GetMessageBox().ShowSimplePrompt($"<b>{randomWeapon.EquipmentDef.GetDisplayName().LocalizeEnglish()}</b>" + " was destroyed "
                                    + " during " + $"{playerCraft.Name}" + "'s" + " disengagement maneuvers.", MessageBoxIcon.None, MessageBoxButtons.OK, null);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }


}

