
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using System;
using System.Linq;

namespace TFTV
{
    internal class TFTVReverseEngineering
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void Apply_Changes()
        {

            try
            {
                ResearchDef laserweapons = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_LaserWeapons_ResearchDef"));
                ExistingResearchRequirementDef sourceResearchReq = Repo.GetAllDefs<ExistingResearchRequirementDef>().FirstOrDefault(ged => ged.name.Equals("SYN_LaserWeapons_ResearchDef"));
                ExistingResearchRequirementDef pistolResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "9B0305C9-CB77-4854-9821-BFCF064EBD65", "LaserWeaponsResearchReqRE01");
                ExistingResearchRequirementDef arResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "CA372217-6853-4177-BE7D-6356B1CDE818", "PX_SY_LaserAssaultRifle_WeaponDef_ResearchDef");
                ExistingResearchRequirementDef srResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "2105AD1F-C837-42F0-B064-C6D120235E6F", "PX_SY_LaserSniperRifle_WeaponDef_ResearchDef");
                pistolResearchReq.ResearchID = "PX_SY_LaserPistol_WeaponDef_ResearchDef";
                arResearchReq.ResearchID = "PX_SY_LaserAssaultRifle_WeaponDef_ResearchDef";
                srResearchReq.ResearchID = "PX_SY_LaserSniperRifle_WeaponDef_ResearchDef";
                ReseachRequirementDefOpContainer[] defOpContainers = laserweapons.RevealRequirements.Container;
                for (int i = 0; i < defOpContainers.Length; i++)
                {
                    if (defOpContainers[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainers[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainers[i].Requirements.Contains(pistolResearchReq))
                    {
                        defOpContainers[i].Requirements = defOpContainers[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { pistolResearchReq, arResearchReq, srResearchReq });
                    }
                }

                ResearchDef synArmor = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_BattleTechArmor_ResearchDef"));
                ExistingResearchRequirementDef sourceResearchReqSynArmor = Repo.GetAllDefs<ExistingResearchRequirementDef>().FirstOrDefault(ged => ged.name.Equals("SYN_BattleTechArmor_ResearchDef"));
                ExistingResearchRequirementDef SynSniperHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "C51FEAF5-6695-4D1E-B3D2-94A0EFB5698D", "PX_SY_SHDefReq");
                ExistingResearchRequirementDef SynSniperTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "07FA6173-5756-4F21-918D-BB2DAFAF632", "PX_SY_STDefReq");
                ExistingResearchRequirementDef SynSniperLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "3B1CA569-05C4-4CFC-A7C0-9F19A9F7915E", "PX_SY_SLDefReq");
                ExistingResearchRequirementDef SynAssaultHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "DA04B1AA-5761-4FED-BD64-40846F5CF8E", "PX_SY_AHDefReq");
                ExistingResearchRequirementDef SynAssaultTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "4675D62B-3249-490C-9B63-BC84428E9681", "PX_SY_ATDefReq");
                ExistingResearchRequirementDef SynAssaultLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReqSynArmor, "AA58C0DE-E91B-40DA-AA3F-46C3607A0ED8", "PX_SY_ALDefReq");

                SynSniperHelmetResearchReq.ResearchID = "PX_SY_Sniper_Helmet_BodyPartDef_ResearchDef";
                SynSniperTorsoResearchReq.ResearchID = "PX_SY_Sniper_Torso_BodyPartDef_ResearchDef";
                SynSniperLegsResearchReq.ResearchID = "PX_SY_Sniper_Legs_ItemDef_ResearchDef";
                SynAssaultHelmetResearchReq.ResearchID = "PX_SY_Assault_Helmet_BodyPartDef_ResearchDef";
                SynAssaultTorsoResearchReq.ResearchID = "PX_SY_Assault_Torso_BodyPartDef_ResearchDef";
                SynAssaultLegsResearchReq.ResearchID = "PX_SY_Assault_Legs_ItemDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersSynArmor = synArmor.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersSynArmor.Length; i++)
                {
                    if (defOpContainersSynArmor[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersSynArmor[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersSynArmor[i].Requirements.Contains(SynSniperHelmetResearchReq))
                    {
                        defOpContainersSynArmor[i].Requirements = defOpContainersSynArmor[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { SynSniperHelmetResearchReq, SynSniperTorsoResearchReq, SynSniperLegsResearchReq, SynAssaultHelmetResearchReq, SynAssaultTorsoResearchReq, SynAssaultLegsResearchReq });
                    }
                }

                ResearchDef njArmor = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_CombatArmor_ResearchDef"));
                ExistingResearchRequirementDef sourceResearchReqNJArmor = Repo.GetAllDefs<ExistingResearchRequirementDef>().FirstOrDefault(ged => ged.name.Equals("NJ_CombatArmor_ResearchDef"));
                ExistingResearchRequirementDef NJSniperHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "A9B9700D-8E31-4426-8054-51B42BA81A5D", "PX_NJ_SHDefReq");
                ExistingResearchRequirementDef NJSniperTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "56FDF653-F993-447D-B9FE-61EEDFDCA856", "PX_NJ_STDefReq");
                ExistingResearchRequirementDef NJSniperLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "B53C57C0-7455-4643-92A1-B3AF8C5AF53C", "PX_NJ_SLDefReq");
                ExistingResearchRequirementDef NJAssaultHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "A1AFC756-20E7-421A-91E2-C6543E33987C", "PX_NJ_AHDefReq");
                ExistingResearchRequirementDef NJAssaultTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "7DB65DA5-6D09-475D-97B5-D69A3F41B951", "PX_NJ_ATDefReq");
                ExistingResearchRequirementDef NJAssaultLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "30E99FD6-41B1-4EDF-8E2B-C018D13DCBA4", "PX_NJ_ALDefReq");
                ExistingResearchRequirementDef NJHeavyHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "9B6F0100-8C0D-48E3-B50C-86C1C1F122E0", "PX_NJ_HHDefReq");
                ExistingResearchRequirementDef NJHeavyTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "44DFF3BA-D0DC-4B72-B6B9-FB7CCBD4EB10", "PX_NJ_HTDefReq");
                ExistingResearchRequirementDef NJHeavyLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReqNJArmor, "526E4DFC-A388-454F-9E09-6F66B2BED8D1", "PX_NJ_HLDefReq");

                NJSniperHelmetResearchReq.ResearchID = "PX_NJ_Sniper_Helmet_BodyPartDef_ResearchDef";
                NJSniperTorsoResearchReq.ResearchID = "PX_NJ_Sniper_Torso_BodyPartDef_ResearchDef";
                NJSniperLegsResearchReq.ResearchID = "PX_NJ_Sniper_Legs_ItemDef_ResearchDef";
                NJAssaultHelmetResearchReq.ResearchID = "PX_NJ_Assault_Helmet_BodyPartDef_ResearchDef";
                NJAssaultTorsoResearchReq.ResearchID = "PX_NJ_Assault_Torso_BodyPartDef_ResearchDef";
                NJAssaultLegsResearchReq.ResearchID = "PX_NJ_Assault_Legs_ItemDef_ResearchDef";
                NJHeavyHelmetResearchReq.ResearchID = "PX_NJ_Heavy_Helmet_BodyPartDef_ResearchDef";
                NJHeavyTorsoResearchReq.ResearchID = "PX_NJ_Heavy_Torso_BodyPartDef_ResearchDef";
                NJHeavyLegsResearchReq.ResearchID = "PX_NJ_Heavy_Legs_ItemDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersNJArmor = njArmor.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersNJArmor.Length; i++)
                {
                    if (defOpContainersNJArmor[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersNJArmor[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersNJArmor[i].Requirements.Contains(NJSniperHelmetResearchReq))
                    {
                        defOpContainersNJArmor[i].Requirements = defOpContainersNJArmor[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { NJSniperHelmetResearchReq, NJSniperTorsoResearchReq, NJSniperLegsResearchReq, NJAssaultHelmetResearchReq, NJAssaultTorsoResearchReq, NJAssaultLegsResearchReq, NJHeavyHelmetResearchReq, NJHeavyTorsoResearchReq, NJHeavyLegsResearchReq });
                    }
                }

                ResearchDef infiltrator = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_InfiltratorTech_ResearchDef"));
                ExistingResearchRequirementDef SY_crossbowResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "851851EA - FC36 - 4382 - 8D5A - DF42D49B2282", "PX_SY_CBDefReq");
                ExistingResearchRequirementDef SY_infHelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "F6F453CC - 7F73 - 4852 - 958A - 95B1CB1DB6EC", "PX_SY_IHDefReq");
                ExistingResearchRequirementDef SY_infTorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "00F866DA - 428D - 4DB1 - 9DBC - 139A9F59E2A2", "PX_SY_ITDefReq");
                ExistingResearchRequirementDef SY_infLegsResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "AD963F03 - 3BDA - 4F7B - 859C - 5B88B17362B8", "PX_SY_ILDefReq");
                ExistingResearchRequirementDef SY_spiderDroneResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "DBE24FE5-81E7-4CB0-8ECD-1A6D5D2485F7", "PX_SY_SDDefReq");
                SY_crossbowResearchReq.ResearchID = "PX_SY_Crossbow_WeaponDef_ResearchDef";
                SY_infHelmetResearchReq.ResearchID = "PX_SY_Infiltrator_Helmet_BodyPartDef_ResearchDef";
                SY_infTorsoResearchReq.ResearchID = "PX_SY_Infiltrator_Torso_BodyPartDef_ResearchDef";
                SY_infLegsResearchReq.ResearchID = "PX_SY_Infiltrator_Legs_ItemDef_ResearchDef";
                SY_spiderDroneResearchReq.ResearchID = "PX_SY_SpiderDroneLauncher_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersInfiltrator = infiltrator.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersInfiltrator.Length; i++)
                {
                    if (defOpContainersInfiltrator[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersInfiltrator[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersInfiltrator[i].Requirements.Contains(SY_crossbowResearchReq))
                    {
                        defOpContainersInfiltrator[i].Requirements = defOpContainersInfiltrator[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { SY_crossbowResearchReq, SY_infHelmetResearchReq, SY_infTorsoResearchReq, SY_infLegsResearchReq, SY_spiderDroneResearchReq });
                    }
                }

                ResearchDef neuralPistol = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_NeuralWeapons_ResearchDef"));
                ExistingResearchRequirementDef SY_neuralWResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "D1743E55-BD77-486C-A44E-F5867A09BEFE", "PX_SY_NPDefReq");
                SY_neuralWResearchReq.ResearchID = "PX_SY_NeuralPistol_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersNeuralWep = neuralPistol.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersNeuralWep.Length; i++)
                {
                    if (defOpContainersNeuralWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersNeuralWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersNeuralWep[i].Requirements.Contains(SY_neuralWResearchReq))
                    {
                        defOpContainersNeuralWep[i].Requirements = defOpContainersNeuralWep[i].Requirements.AddToArray(SY_neuralWResearchReq);
                    }
                }

                ResearchDef neuralRifle = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_AdvancedDisableTech_ResearchDef"));
                ExistingResearchRequirementDef SY_AdvNeuralWResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "54E81D5D-BF00-4E3F-9A66-AC5BA0274150", "PX_SY_NRDefReq");
                ExistingResearchRequirementDef SY_SonicGrenadeResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "0D336DCE-9373-4C5A-916C-E868D4AF9776", "PX_SY_SGDefReq");
                SY_AdvNeuralWResearchReq.ResearchID = "PX_SY_NeuralSniperRifle_WeaponDef_ResearchDef";
                SY_SonicGrenadeResearchReq.ResearchID = "PX_SY_SonicGrenade_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersNeuralAdvWep = neuralRifle.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersNeuralAdvWep.Length; i++)
                {
                    if (defOpContainersNeuralAdvWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersNeuralAdvWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersNeuralAdvWep[i].Requirements.Contains(SY_AdvNeuralWResearchReq))
                    {
                        defOpContainersNeuralAdvWep[i].Requirements = defOpContainersNeuralAdvWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { SY_AdvNeuralWResearchReq, SY_SonicGrenadeResearchReq });
                    }
                }

                ResearchDef poisonNade = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_PoisonWeapons_ResearchDef"));
                ExistingResearchRequirementDef SY_PoisonNadeResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "B50DB508-E2D6-4D51-B6AB-D106FF77347D", "PX_SY_PNDefReq");
                SY_PoisonNadeResearchReq.ResearchID = "PX_SY_PoisonGrenade_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersPoisonNade = poisonNade.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersPoisonNade.Length; i++)
                {
                    if (defOpContainersPoisonNade[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersPoisonNade[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersPoisonNade[i].Requirements.Contains(SY_PoisonNadeResearchReq))
                    {
                        defOpContainersPoisonNade[i].Requirements = defOpContainersPoisonNade[i].Requirements.AddToArray(SY_PoisonNadeResearchReq);
                    }
                }

                ResearchDef poisonBow = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("SYN_PoisonWeapons_ResearchDef"));
                ExistingResearchRequirementDef SY_PoisonBowResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "B0B38515-88AC-4E3E-96A3-E68E3241B3C2", "PX_SY_PBDefReq");
                SY_PoisonBowResearchReq.ResearchID = "PX_SY_Venombolt_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersPoisonBow = poisonBow.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersPoisonBow.Length; i++)
                {
                    if (defOpContainersPoisonBow[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersPoisonBow[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersPoisonBow[i].Requirements.Contains(SY_PoisonBowResearchReq))
                    {
                        defOpContainersPoisonBow[i].Requirements = defOpContainersPoisonBow[i].Requirements.AddToArray(SY_PoisonBowResearchReq);
                    }
                }

                ResearchDef technician = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_Technician_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_TechTurretResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "C20A3C2F-F01C-4345-A24E-77CF213E9D57", "PX_NJ_TTuDefReq");
                ExistingResearchRequirementDef PX_NJ_Technician_TorsoResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "527B4F64-47AE-4F53-BF83-EC9A6CB9972D", "PX_NJ_TTDefReq");
                ExistingResearchRequirementDef PX_NJ_Technician_MechArmsResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "7AF133F9-1B12-4AEF-8B6B-62EF9ACF2B61", "PX_NJ_MADefReq");
                ExistingResearchRequirementDef PX_NJ_Technician_LegsResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "112C0F46-8E4B-45CE-A18E-3EABCE0757E5", "PX_NJ_TLDefReq");
                ExistingResearchRequirementDef PX_NJ_Technician_HelmetResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "1D3A557C-C358-400A-B1AC-D1B26B3A64B6", "PX_NJ_THDefReq");
                ExistingResearchRequirementDef PX_NJ_Gauss_PDWResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "2DEB43B0-E259-4CFE-9879-FD29497956C2", "PX_NJ_GPDWDefReq");
                PX_NJ_TechTurretResearchReq.ResearchID = "PX_NJ_TechTurretItem_ItemDef_ResearchDef";
                PX_NJ_Technician_TorsoResearchReq.ResearchID = "PX_NJ_Technician_Torso_BodyPartDef_ResearchDef";
                PX_NJ_Technician_MechArmsResearchReq.ResearchID = "PX_NJ_Technician_MechArms_WeaponDef_ResearchDef";
                PX_NJ_Technician_LegsResearchReq.ResearchID = "PX_NJ_Technician_Legs_ItemDef_ResearchDef";
                PX_NJ_Technician_HelmetResearchReq.ResearchID = "PX_NJ_Technician_Helmet_BodyPartDef_ResearchDef";
                PX_NJ_Gauss_PDWResearchReq.ResearchID = "PX_NJ_Gauss_PDW_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersTechnician = technician.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersTechnician.Length; i++)
                {
                    if (defOpContainersTechnician[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersTechnician[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersTechnician[i].Requirements.Contains(PX_NJ_TechTurretResearchReq))
                    {
                        defOpContainersTechnician[i].Requirements = defOpContainersTechnician[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_NJ_TechTurretResearchReq, PX_NJ_Technician_TorsoResearchReq, PX_NJ_Technician_MechArmsResearchReq, PX_NJ_Technician_LegsResearchReq, PX_NJ_Technician_HelmetResearchReq, PX_NJ_Gauss_PDWResearchReq });
                    }
                }

                ResearchDef rocketLauncher = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_NeuralTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_RocketLauncherResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "FA17BF6E-EF5A-4996-8A93-D32E7BFE108D", "PX_NJ_RoLDefReq");
                PX_NJ_RocketLauncherResearchReq.ResearchID = "PX_NJ_RocketLauncherPack_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersRocketLauncher = rocketLauncher.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersRocketLauncher.Length; i++)
                {
                    if (defOpContainersRocketLauncher[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersRocketLauncher[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersRocketLauncher[i].Requirements.Contains(PX_NJ_RocketLauncherResearchReq))
                    {
                        defOpContainersRocketLauncher[i].Requirements = defOpContainersRocketLauncher[i].Requirements.AddToArray(PX_NJ_RocketLauncherResearchReq);
                    }
                }

                ResearchDef prcrTechTurret = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_PRCRTechTurret_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_PRCRTechTurretResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "A9C7BC41-88BA-4087-B676-BC63748D43D1", "PX_NJ_PRCRTTuDefReq");
                ExistingResearchRequirementDef PX_NJ_PRCRGauss_PDWResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "48D9DE30-9663-4556-843A-97EB050BB911", "PX_NJ_PRCRPDWDefReq");

                PX_NJ_PRCRTechTurretResearchReq.ResearchID = "PX_NJ_PRCRTechTurretItem_ItemDef_ResearchDef";
                PX_NJ_PRCRGauss_PDWResearchReq.ResearchID = "PX_NJ_PRCR_PDW_WeaponDef_ResearchDef";


                ReseachRequirementDefOpContainer[] defOpContainersPRCRTech = prcrTechTurret.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersPRCRTech.Length; i++)
                {
                    if (defOpContainersPRCRTech[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersPRCRTech[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersPRCRTech[i].Requirements.Contains(PX_NJ_PRCRTechTurretResearchReq))
                    {
                        defOpContainersPRCRTech[i].Requirements = defOpContainersPRCRTech[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_NJ_PRCRTechTurretResearchReq, PX_NJ_PRCRGauss_PDWResearchReq });
                    }
                }

                ResearchDef NJprcrWep = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_PiercerTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_PRCRSRResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "8C5318C7-761C-4AC9-A24C-168D598857EA", "PX_NJ_PRCRSRDefReq");
                ExistingResearchRequirementDef PX_NJ_PRCRARResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "97BB0118-37A5-4E38-965C-EADFED7B56DA", "PX_NJ_PRCRARDefReq");

                PX_NJ_PRCRSRResearchReq.ResearchID = "PX_NJ_PRCR_SniperRifle_WeaponDef_ResearchDef";
                PX_NJ_PRCRARResearchReq.ResearchID = "PX_NJ_PRCR_AssaultRifle_WeaponDef_ResearchDef";


                ReseachRequirementDefOpContainer[] defOpContainersPRCRTechWep = NJprcrWep.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersPRCRTechWep.Length; i++)
                {
                    if (defOpContainersPRCRTechWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersPRCRTechWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersPRCRTechWep[i].Requirements.Contains(PX_NJ_PRCRSRResearchReq))
                    {
                        defOpContainersPRCRTechWep[i].Requirements = defOpContainersPRCRTechWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_NJ_PRCRSRResearchReq, PX_NJ_PRCRARResearchReq });
                    }
                }

                ResearchDef NJfire = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_PurificationTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_firenadeResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "257F8670-1ED3-4542-B0FA-44095B3CA550", "PX_NJ_FireNDefReq");
                ExistingResearchRequirementDef PX_NJ_flamethrowerResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "3DC83619-97E1-42FE-B4AD-0AABB75812F5", "PX_NJ_FlameTDefReq");

                PX_NJ_firenadeResearchReq.ResearchID = "PX_NJ_IncindieryGrenade_WeaponDef_ResearchDef";
                PX_NJ_flamethrowerResearchReq.ResearchID = "PX_NJ_FlameThrower_WeaponDef_ResearchDef";


                ReseachRequirementDefOpContainer[] defOpContainersfireWep = NJfire.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersfireWep.Length; i++)
                {
                    if (defOpContainersfireWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersfireWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersfireWep[i].Requirements.Contains(PX_NJ_firenadeResearchReq))
                    {
                        defOpContainersfireWep[i].Requirements = defOpContainersfireWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_NJ_firenadeResearchReq, PX_NJ_flamethrowerResearchReq });
                    }
                }

                ResearchDef HrocketLauncher = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_ExplosiveTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_HRocketLauncherResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "159B5049-7BEA-483A-9253-3F10221FE14D", "PX_NJ_HRoLDefReq");
                PX_NJ_HRocketLauncherResearchReq.ResearchID = "PX_NJ_HeavyRocketLauncher_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersHRocketLauncher = HrocketLauncher.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersRocketLauncher.Length; i++)
                {
                    if (defOpContainersHRocketLauncher[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersHRocketLauncher[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersHRocketLauncher[i].Requirements.Contains(PX_NJ_HRocketLauncherResearchReq))
                    {
                        defOpContainersHRocketLauncher[i].Requirements = defOpContainersHRocketLauncher[i].Requirements.AddToArray(PX_NJ_HRocketLauncherResearchReq);
                    }
                }

                ResearchDef grocketLauncher = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_GuidanceTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_GRocketLauncherResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "D2AACC99-4EFF-48F7-B667-F04BE231953D", "PX_NJ_GRoLDefReq");
                PX_NJ_GRocketLauncherResearchReq.ResearchID = "PX_NJ_GuidedMissileLauncherPack_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersGRocketLauncher = grocketLauncher.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersGRocketLauncher.Length; i++)
                {
                    if (defOpContainersGRocketLauncher[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersGRocketLauncher[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersGRocketLauncher[i].Requirements.Contains(PX_NJ_GRocketLauncherResearchReq))
                    {
                        defOpContainersGRocketLauncher[i].Requirements = defOpContainersGRocketLauncher[i].Requirements.AddToArray(PX_NJ_GRocketLauncherResearchReq);
                    }
                }

                ResearchDef gaussWeapons = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("NJ_GaussTech_ResearchDef"));
                ExistingResearchRequirementDef PX_NJ_GaussMGResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "E6FEEE71-DD60-4419-873F-F4B6F0FFB5D4", "PX_NJ_GMGDefReq");
                ExistingResearchRequirementDef PX_NJ_GaussAR_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "52712514-31EC-4AE8-BCA3-49CE8E10167A", "PX_NJ_GARDefReq");
                ExistingResearchRequirementDef PX_NJ_GaussHG_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "3F83B7DD-C11C-4510-990C-75E574FAF212", "PX_NJ_GHGDefReq");
                ExistingResearchRequirementDef PX_NJ_GaussSR_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "D688CD9D-7E14-437A-A941-27A91EE69AD5", "PX_NJ_GSRDefReq");
                PX_NJ_GaussMGResearchReq.ResearchID = "PX_NJ_Gauss_MachineGun_WeaponDef_ResearchDef";
                PX_NJ_GaussAR_ResearchReq.ResearchID = "PX_NJ_Gauss_HandGun_WeaponDef_ResearchDef";
                PX_NJ_GaussHG_ResearchReq.ResearchID = "PX_NJ_Gauss_AssaultRifle_WeaponDef_ResearchDef";
                PX_NJ_GaussSR_ResearchReq.ResearchID = "PX_NJ_Gauss_SniperRifle_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersGaussWep = gaussWeapons.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersGaussWep.Length; i++)
                {
                    if (defOpContainersGaussWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersGaussWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersGaussWep[i].Requirements.Contains(PX_NJ_GaussMGResearchReq))
                    {
                        defOpContainersGaussWep[i].Requirements = defOpContainersGaussWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_NJ_GaussMGResearchReq, PX_NJ_GaussAR_ResearchReq, PX_NJ_GaussHG_ResearchReq, PX_NJ_GaussSR_ResearchReq });
                    }
                }

                ResearchDef anuWeapons = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_AnuWarfare_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_AssaultH_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "5EE15515-7004-4EA4-887B-33F0B3905BCC", "PX_AN_AHDefReq");
                ExistingResearchRequirementDef PX_AN_AssaulL_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "BB3284F7-548E-4B3D-8391-40FEE2787290", "PX_AN_ALDefReq");
                ExistingResearchRequirementDef PX_AN_AssaulT_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "679F9EA6-FA87-4D13-8227-8B3DABC54BB9", "PX_AN_ATDefReq");
                ExistingResearchRequirementDef PX_AN_AG_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "CD54E727-7E00-4122-B642-EF1C14F596BE", "PX_AN_SGDefReq");
                PX_AN_AssaultH_ResearchReq.ResearchID = "PX_AN_Assault_Helmet_BodyPartDef_ResearchDef";
                PX_AN_AssaulL_ResearchReq.ResearchID = "PX_AN_Assault_Legs_ItemDef_ResearchDef";
                PX_AN_AssaulT_ResearchReq.ResearchID = "PX_AN_Assault_Torso_BodyPartDef_ResearchDef";
                PX_AN_AG_ResearchReq.ResearchID = "PX_AN_Shotgun_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersAnuWep = anuWeapons.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersAnuWep.Length; i++)
                {
                    if (defOpContainersAnuWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersAnuWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersAnuWep[i].Requirements.Contains(PX_AN_AssaultH_ResearchReq))
                    {
                        defOpContainersAnuWep[i].Requirements = defOpContainersAnuWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_AN_AssaultH_ResearchReq, PX_AN_AssaulL_ResearchReq, PX_AN_AssaulT_ResearchReq, PX_AN_AG_ResearchReq });
                    }
                }

                ResearchDef priest = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_AnuPriest_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_Redemptor_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "FAF12E3B-00B3-4C07-8889-13190CE12508", "PX_AN_PRDefReq");
                ExistingResearchRequirementDef PX_AN_PriestL_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "D1D38027-684D-4E75-8DAA-EEB4A5677B80", "PX_AN_PLDefReq");
                ExistingResearchRequirementDef PX_AN_PriestT_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "E40B776D-4C66-46BE-83C5-F269E0D7BAD1", "PX_AN_PTDefReq");
                PX_AN_Redemptor_ResearchReq.ResearchID = "PX_AN_Redemptor_WeaponDef_ResearchDef";
                PX_AN_PriestL_ResearchReq.ResearchID = "PX_AN_Priest_Legs_ItemDef_ResearchDef";
                PX_AN_PriestT_ResearchReq.ResearchID = "PX_AN_Priest_Torso_BodyPartDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersPriest = priest.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersPriest.Length; i++)
                {
                    if (defOpContainersPriest[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersPriest[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersPriest[i].Requirements.Contains(PX_AN_Redemptor_ResearchReq))
                    {
                        defOpContainersPriest[i].Requirements = defOpContainersPriest[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_AN_Redemptor_ResearchReq, PX_AN_PriestL_ResearchReq, PX_AN_PriestT_ResearchReq });
                    }
                }

                ResearchDef berserker = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_Berserker_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_BerserkerH_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "A684B52E-E31A-47BD-AA4A-33A1BC5161F7", "PX_AN_BHDefReq");
                ExistingResearchRequirementDef PX_AN_BerserkerL_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "24531363-A7D4-41CF-A1A9-D3D5704608C8", "PX_AN_BLDefReq");
                ExistingResearchRequirementDef PX_AN_BerserkerT_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "DC6421F1-7B7E-4771-AD4D-47D2ED01DB83", "PX_AN_BTDefReq");
                ExistingResearchRequirementDef PX_AN_Hammer_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "470A56E0-E04B-4300-9078-1C5E8F2F2775", "PX_AN_HammerDefReq");
                ExistingResearchRequirementDef PX_AN_HC_ResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "B3B20735-F556-426A-B8E0-F4F990C03490", "PX_AN_HandCDefReq");
                PX_AN_BerserkerH_ResearchReq.ResearchID = "PX_AN_Berserker_Helmet_BodyPartDef_ResearchDef";
                PX_AN_BerserkerL_ResearchReq.ResearchID = "PX_AN_Berserker_Legs_ItemDef_ResearchDef";
                PX_AN_BerserkerT_ResearchReq.ResearchID = "PX_AN_Berserker_Torso_BodyPartDef_ResearchDef";
                PX_AN_Hammer_ResearchReq.ResearchID = "PX_AN_Hammer_WeaponDef_ResearchDef";
                PX_AN_HC_ResearchReq.ResearchID = "PX_AN_HandCannon_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersBerserker = berserker.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersBerserker.Length; i++)
                {
                    if (defOpContainersBerserker[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersBerserker[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersBerserker[i].Requirements.Contains(PX_AN_BerserkerH_ResearchReq))
                    {
                        defOpContainersBerserker[i].Requirements = defOpContainersBerserker[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_AN_BerserkerH_ResearchReq, PX_AN_BerserkerL_ResearchReq, PX_AN_BerserkerT_ResearchReq, PX_AN_Hammer_ResearchReq, PX_AN_HC_ResearchReq });
                    }
                }

                ResearchDef ANacid = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_AcidTech_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_acidnadeResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "50EC71CB-D3AD-43E7-866D-69AC818152A9", "PX_AN_AcidNDefReq");
                ExistingResearchRequirementDef PX_AN_acidGunResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "BF480655-C453-4CC6-92BE-58DFBC24F311", "PX_AN_AcidGDefReq");

                PX_AN_acidnadeResearchReq.ResearchID = "PX_AN_AcidGrenade_WeaponDef_ResearchDef";
                PX_AN_acidGunResearchReq.ResearchID = "PX_AN_AcidHandGun_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainerAcidWep = ANacid.RevealRequirements.Container;
                for (int i = 0; i < defOpContainerAcidWep.Length; i++)
                {
                    if (defOpContainerAcidWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainerAcidWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainerAcidWep[i].Requirements.Contains(PX_AN_acidnadeResearchReq))
                    {
                        defOpContainerAcidWep[i].Requirements = defOpContainerAcidWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_AN_acidnadeResearchReq, PX_AN_acidGunResearchReq });
                    }
                }

                ResearchDef ANadvmelee = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_AdvancedMeleeCombat_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_bladeResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "C5861228-B6CC-43F1-8131-499375E0855D", "PX_AN_bladeDefReq");
                ExistingResearchRequirementDef PX_AN_maceResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "0ECF6B49-51B1-410A-B2B1-96686CBB6A12", "PX_AN_maceDefReq");

                PX_AN_bladeResearchReq.ResearchID = "PX_AN_Blade_WeaponDef_ResearchDef";
                PX_AN_maceResearchReq.ResearchID = "PX_AN_Mace_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainerAdvMeleeWep = ANadvmelee.RevealRequirements.Container;
                for (int i = 0; i < defOpContainerAdvMeleeWep.Length; i++)
                {
                    if (defOpContainerAdvMeleeWep[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainerAdvMeleeWep[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainerAdvMeleeWep[i].Requirements.Contains(PX_AN_bladeResearchReq))
                    {
                        defOpContainerAdvMeleeWep[i].Requirements = defOpContainerAdvMeleeWep[i].Requirements.AddRangeToArray(new ResearchRequirementDef[] { PX_AN_bladeResearchReq, PX_AN_maceResearchReq });
                    }
                }

                ResearchDef anuShred = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_ShreddingTech_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_ShredSGResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "14731F2C-C583-46A9-8A93-9848E8C3D352", "PX_AN_SSGDefReq");
                PX_AN_ShredSGResearchReq.ResearchID = "PX_AN_ShreddingShotgun_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersShred = anuShred.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersShred.Length; i++)
                {
                    if (defOpContainersShred[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersShred[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersShred[i].Requirements.Contains(PX_AN_ShredSGResearchReq))
                    {
                        defOpContainersShred[i].Requirements = defOpContainersShred[i].Requirements.AddToArray(PX_AN_ShredSGResearchReq);
                    }
                }

                ResearchDef anAdvInf = Repo.GetAllDefs<ResearchDef>().FirstOrDefault(ged => ged.name.Equals("ANU_AdvancedInfectionTech_ResearchDef"));
                ExistingResearchRequirementDef PX_AN_VSRResearchReq = Helper.CreateDefFromClone(sourceResearchReq, "B376A8BE-D83A-48BB-8F44-8F121C2B59F9", "PX_AN_VSRDefReq");
                PX_AN_VSRResearchReq.ResearchID = "PX_AN_Subjector_WeaponDef_ResearchDef";

                ReseachRequirementDefOpContainer[] defOpContainersAdvInf = anAdvInf.RevealRequirements.Container;
                for (int i = 0; i < defOpContainersAdvInf.Length; i++)
                {
                    if (defOpContainersAdvInf[i].Operation != ResearchContainerOperation.ANY)
                    {
                        defOpContainersAdvInf[i].Operation = ResearchContainerOperation.ANY;
                    }
                    if (!defOpContainersAdvInf[i].Requirements.Contains(PX_AN_VSRResearchReq))
                    {
                        defOpContainersAdvInf[i].Requirements = defOpContainersAdvInf[i].Requirements.AddToArray(PX_AN_VSRResearchReq);
                    }
                }

                AdjustCosts();
            }

            

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AdjustCosts()
        {
            try
            {
                AdjustRPCostOfReverseEngineering("PX_SY_Laser", 300);
                AdjustRPCostOfReverseEngineering("PX_AN_Mace", 300);
                AdjustRPCostOfReverseEngineering("PX_AN_Berserker",100);
                AdjustRPCostOfReverseEngineering("PX_AN_Priest",500);
                AdjustRPCostOfReverseEngineering("PX_AN_Assault",100);
                AdjustRPCostOfReverseEngineering("PX_NJ_Gauss",200);
                AdjustRPCostOfReverseEngineering("PX_NJ_IncindieryGrenade",400);
                AdjustRPCostOfReverseEngineering("PX_NJ_PRCR_AssaultRifle", 600);
                AdjustRPCostOfReverseEngineering("PX_NJ_PRCRTechTurretItem",800);
                AdjustRPCostOfReverseEngineering("PX_NJ_TechTurretItem",250);
                AdjustRPCostOfReverseEngineering("PX_NJ_Technician",250);
                AdjustRPCostOfReverseEngineering("PX_SY_SonicGrenade_WeaponDef_ResearchDef",1300);
                AdjustRPCostOfReverseEngineering("PX_SY_Crossbow",800);
                AdjustRPCostOfReverseEngineering("PX_SY_Infiltrator",800);
                AdjustRPCostOfReverseEngineering("PX_NJ_Sniper",150);
                AdjustRPCostOfReverseEngineering("PX_NJ_Assault",150);
                AdjustRPCostOfReverseEngineering("PX_NJ_Heavy",150);
                AdjustRPCostOfReverseEngineering("PX_SY_Sniper",150);
                AdjustRPCostOfReverseEngineering("PX_SY_Assault",150);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        public static void AdjustRPCostOfReverseEngineering(string keyword, int cost) 
        {
            try 
            {
                foreach (ResearchDef research in Repo.GetAllDefs<ResearchDef>()) 
                {
                    if (research.Id.Contains(keyword)) 
                    { 
                    research.ResearchCost=cost;
                    
                    }              
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

    }
}
