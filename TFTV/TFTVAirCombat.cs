using Base;
using Base.Core;
using Base.Defs;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Interception.Equipments;
using PhoenixPoint.Geoscape.Interception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static bool Prepare(TFTVMain main)
        {
            return main.Config.ActivateAirCombatChanges;
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

