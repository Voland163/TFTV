using Base;
using Base.Assets;
using Base.Cameras;
using Base.Core;
using Base.Defs;
using Base.Entities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Levels;
using Base.Utils.Maths;
using com.ootii.Helpers;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Levels.ActorDeployment;
using PhoenixPoint.Common.Levels.MapGeneration;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.ActorsInstance;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.StructuralTargets;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.ActorDeployment;
using PhoenixPoint.Tactical.Levels.Destruction;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.Levels.Mist;
using PhoenixPoint.Tactical.Prompts;
using SETUtil.Extend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static TFTV.TFTVBaseDefenseTactical.StartingDeployment;

namespace TFTV
{
    internal class TFTVBaseDefenseTactical
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //Serialized variables
        public static Dictionary<float, float> ConsolePositions = new Dictionary<float, float>();
        public static int StratToBeAnnounced = 0;
        public static int StratToBeImplemented = 0;
        public static float TimeLeft = 0;
        public static bool[] UsedStrats = new bool[5];
        internal static Dictionary<string, int> PandoransInContainment = new Dictionary<string, int>();
        internal static Dictionary<string, int> PandoransInContainmentThatEscaoed = new Dictionary<string, int>();
        internal static bool ScyllaLoose = false;
        internal static bool Breach = false;

        //Non-serialized variables
        public static bool VentingHintShown = false;

        //Common References
        private static readonly string ConsoleName = "BaseDefenseConsole";

        

        private static readonly ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
        private static readonly ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
        private static readonly ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");


        private static readonly DelayedEffectStatusDef reinforcementStatusUnder1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatus1AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatus1AP]");
        private static readonly DelayedEffectStatusDef reinforcementStatusUnder2AP = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [ReinforcementStatusUnder2AP]");
        private static readonly MissionTagDef baseDefenseTag = DefCache.GetDef<MissionTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");

        //Run on Mission restart
        public static void ResetPandoransInContainment()
        {
            try
            {
                if (PandoransInContainmentThatEscaoed != null && PandoransInContainmentThatEscaoed.Count > 0)
                {
                    foreach (string key in PandoransInContainmentThatEscaoed.Keys)
                    {
                        if (PandoransInContainment.ContainsKey(key))
                        {
                            PandoransInContainment[key] += 1;
                        }
                        else
                        {
                            PandoransInContainment.Add(key, 1);
                        }
                    }
                }

                PandoransInContainmentThatEscaoed.Clear();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        //Displayed (Escaped) after escaped Pandoran name (run from [HarmonyPatch(typeof(TacticalActorBase), "get_DisplayName")])
        public static string DisplayEscapedPandoranName(TacticalActorBase tacticalActorBase)
        {
            try
            {
                string result = "";

                if (tacticalActorBase.HasGameTag(Defs.EscapedPandoran))
                {
                    result = " (Escaped)";
                }

                return result;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static bool CheckIfBaseDefenseVsAliens(TacticalLevelController controller)
        {
            try
            {
                if (controller.TacMission.MissionData.MissionType.MissionTypeTag == baseDefenseTag
                    && controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                {
                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        internal class Defs
        {
            internal static GameTagDef SecurityGuardTag;
            internal static TacCharacterDef SecurityGuard;
            internal static TacCharacterDef MFedSecurityGuard;
            private static ItemDef SecurityTorso;
            internal static TacCharacterDef ChironDigger;
            internal static GameTagDef EscapedPandoran;


            private static void CreateSecurityGuardTag()
            {
                try
                {
                    string name = "SecurityGuard";
                    GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                    SecurityGuardTag = Helper.CreateDefFromClone(
                         source,
                         "{A3F75FC4-EBDE-4A79-9829-748CC54B7255}",
                         name + "_" + "GameTagDef");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void CreateEscapedPandoranTag()
            {
                try
                {
                    string name = "EscapedPandoran";
                    GameTagDef source = DefCache.GetDef<GameTagDef>("Takeshi_Tutorial3_GameTagDef");
                    EscapedPandoran = Helper.CreateDefFromClone(
                         source,
                         "{12E7F229-EB06-4C17-891C-E3C2B22693CB}",
                         name + "_" + "GameTagDef");
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            //Not used atmo
            private static void CreateMeleeChiron()
            {
                try
                {
                    TacCharacterDef source = DefCache.GetDef<TacCharacterDef>("Chiron2_FireWormHeavy_AlienMutationVariationDef");
                    string name = "MeleeChiron";
                    string gUID = "{95AA563B-4EC8-4232-BB7D-A35765AD2055}";

                    TacCharacterDef newChiron = Helper.CreateDefFromClone(source, gUID, name);

                    newChiron.Data.Name = "KEY_CHIRON_DIGGER_NAME";
                    newChiron.SpawnCommandId = "MeleeChiron";
                    newChiron.Data.Speed = 10;
                    List<ItemDef> bodyParts = newChiron.Data.BodypartItems.ToList();
                    bodyParts.RemoveLast();

                    newChiron.Data.BodypartItems = bodyParts.ToArray();
                    ChironDigger = newChiron;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateSecurityArmor()
            {
                try
                {

                    TacticalItemDef sourceTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");

                    TacticalItemDef leftArmNakedAssault = CreateNakedArm(
                        DefCache.GetDef<TacticalItemDef>("Human_LeftArm_BodyPartDef"),
                        "NakedGoldShiftLeftArm_BodyPartDef",
                        "{B07248F5-08DC-4C9D-85AA-30840D90B404}",
                        "{43651E08-016F-4381-ADA4-143BA468B439}",
                        "{3E205B0D-66FD-4986-92E9-05484E3EB26B}",
                        "{BA7E7FB3-4FCB-48E3-B2BE-C5CBECB253B9}"
                        );
                    TacticalItemDef rightArmNakedAssault = CreateNakedArm(
                        DefCache.GetDef<TacticalItemDef>("Human_RightArm_BodyPartDef"),
                        "NakedGoldShiftRightArm_BodyPartDef",
                        "{04D68697-CE58-4039-AE95-091272143257}",
                        "{72A05522-B9C5-4520-9738-2BC74104B908}",
                        "{2091D133-645C-4959-8D75-2D1308A56DA0}",
                        "{C6DD6AF9-B897-42B8-81C1-78287A75A167}"
                        );

                    string name = "PX_Security_Toros_BodyParDef";
                    string gUID = "{38D8B5D1-8D55-4AFD-94E7-E68359A433F3}";

                    TacticalItemDef securityTorso = Helper.CreateDefFromClone(sourceTorso, gUID, name);
                    securityTorso.ViewElementDef = Helper.CreateDefFromClone(securityTorso.ViewElementDef, "{E1E14737-C984-411A-8C27-6262CCC64B0F}", name);
                    securityTorso.BodyPartAspectDef = Helper.CreateDefFromClone(securityTorso.BodyPartAspectDef, "{7C5C0FD7-7AB6-4EC8-A827-63E3B5C90631}", name);
                    securityTorso.SkinData = Helper.CreateDefFromClone(securityTorso.SkinData, "{5B1059E3-ACE7-4B54-8D5E-61020BBF8BAD}", name);
                    securityTorso.SubAddons[0] = new AddonDef.SubaddonBind() { SubAddon = leftArmNakedAssault };
                    securityTorso.SubAddons[1] = new AddonDef.SubaddonBind() { SubAddon = rightArmNakedAssault };

                    SecurityTorso = securityTorso;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static TacticalItemDef CreateNakedArm(TacticalItemDef source, string name, string gUID0, string gUID1, string gUID2, string gUID3)
            {
                try
                {
                    TacticalItemDef tacticalItemDef = Helper.CreateDefFromClone(source, gUID0, name);
                    tacticalItemDef.HitPoints = 0;
                    tacticalItemDef.ViewElementDef = Helper.CreateDefFromClone(source.ViewElementDef, gUID1, name);
                    tacticalItemDef.BodyPartAspectDef = Helper.CreateDefFromClone(source.BodyPartAspectDef, gUID2, name);
                    tacticalItemDef.SkinData = Helper.CreateDefFromClone(source.SkinData, gUID3, name);

                    return tacticalItemDef;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            private static void CreateSecurityGuard()
            {
                try
                {
                    string name = "Phoenix_Guard";
                    string gUID = "{AA2A57B0-C707-4BB0-AD13-8484F4349514}";



                    TacCharacterDef characterSource = DefCache.GetDef<TacCharacterDef>($"PX_Assault1_CharacterTemplateDef");
                    TacCharacterDef newCharacter = Helper.CreateDefFromClone(characterSource, gUID, name);

                    newCharacter.SpawnCommandId = name;
                    newCharacter.Data.Name = "Phoenix Guard";

                    TacticalItemDef sourceTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");
                    WeaponDef assaultRifle = DefCache.GetDef<WeaponDef>("PX_AssaultRifle_Gold_WeaponDef");
                    WeaponDef grenade = DefCache.GetDef<WeaponDef>("PX_HandGrenade_WeaponDef");
                    EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");

                    TacticalItemDef assaultHelmet = DefCache.GetDef<TacticalItemDef>("PX_Assault_Helmet_BodyPartDef");
                    TacticalItemDef assaultTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_BodyPartDef");
                    TacticalItemDef assaultLegs = DefCache.GetDef<TacticalItemDef>("PX_Assault_Legs_ItemDef");


                    TacticalItemDef assaultGoldHelmet = DefCache.GetDef<TacticalItemDef>("PX_Assault_Helmet_Gold_BodyPartDef");
                    TacticalItemDef assaultGoldTorso = DefCache.GetDef<TacticalItemDef>("PX_Assault_Torso_Gold_BodyPartDef");
                    TacticalItemDef assaultGoldLegs = DefCache.GetDef<TacticalItemDef>("PX_Assault_Legs_Gold_ItemDef");

                    newCharacter.Data.BodypartItems = new ItemDef[] { assaultGoldHelmet, SecurityTorso, assaultGoldLegs };

                    newCharacter.Data.EquipmentItems = new ItemDef[] {assaultRifle, assaultRifle.CompatibleAmmunition[0],grenade
,
                };

                    newCharacter.Data.InventoryItems = new ItemDef[] { assaultRifle.CompatibleAmmunition[0] };

                    SecurityGuard = newCharacter;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void CreateMindfraggedSecurityGuard()
            {
                try
                {
                    string name = "MFed_Phoenix_Guard";
                    string gUID = "{5ACCAA55-CAF9-48A6-9D46-756BC59BDC23}";

                    TacCharacterDef characterSource = SecurityGuard;
                    TacCharacterDef newCharacter = Helper.CreateDefFromClone(characterSource, gUID, name);

                    newCharacter.SpawnCommandId = name;
                    newCharacter.Data.Name = "Phoenix Guard";

                    newCharacter.Data.Abilites = newCharacter.Data.Abilites.AddToArray(DefCache.GetDef<ApplyStatusAbilityDef>("InfestedWithMindfragger_StatusAbilityDef"));
                    //  newCharacter.DefaultDeploymentTags = new ActorDeploymentTagDef[] {DefCache.GetDef<ActorDeploymentTagDef>("1x1_MindfraggedGrunt_DeploymentTagDef") };
                    //  newCharacter.Data.GameTags = newCharacter.Data.GameTags.AddToArray(DefCache.GetDef<ClassTagDef>("MindfraggedAssault_AN_ClassTagDef"));

                    MFedSecurityGuard = newCharacter;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            private static void ReinstateSecurityStation()
            {
                try
                {
                    PhoenixFacilityDef securityStation = DefCache.GetDef<PhoenixFacilityDef>("SecurityStation_PhoenixFacilityDef");
                    GeoPhoenixFactionDef phoenixFaction = DefCache.GetDef<GeoPhoenixFactionDef>("Phoenix_GeoPhoenixFactionDef");

                    phoenixFaction.StartingFacilities = phoenixFaction.StartingFacilities.AddToArray(securityStation);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
            public static void CreateDefsForBaseDefenseTactical()
            {
                try
                {
                    CreateSecurityArmor();
                    CreateSecurityGuard();
                    CreateMindfraggedSecurityGuard();
                    ReinstateSecurityStation();
                    CreateMeleeChiron();
                    CreateEscapedPandoranTag();
                    CreateSecurityGuardTag();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class Objectives
        {
            internal static readonly GameTagDef _scatterEnemiesObjectiveTag = TFTVBaseDefenseGeoscape.ScatterEnemiesTag;
            internal static readonly GameTagDef _killMainObjectiveTag = TFTVBaseDefenseGeoscape.KillInfestationTag;

            public static void OjectivesDebbuger(TacticalLevelController controller)
            {
                try
                {
                    IEnumerable<TacticalActorBase> allPandorans = from x in controller.Map.GetActors<TacticalActorBase>()
                                                                  where x.HasGameTag(_scatterEnemiesObjectiveTag)
                                                                  select x;
                    if (allPandorans.Count() > 0)
                    {
                        foreach (TacticalActorBase tacticalActor in allPandorans)
                        {
                            if (tacticalActor.IsOffMap || tacticalActor.TacticalFaction == controller.GetTacticalFaction(controller.TacticalLevelControllerDef.WildBeastFaction))
                            {
                                TFTVLogger.Always($"this Pandoran {tacticalActor.name} is OffMap or is wild. Removing KillObjective tag.");
                                tacticalActor.GameTags.Remove(_scatterEnemiesObjectiveTag);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            [HarmonyPatch(typeof(KillActorFactionObjective), "GetTargets")]
            public static class TFTV_KillActorFactionObjective_GetTargets_Patch
            {
                public static void Postfix(FactionObjective __instance, ref IEnumerable<TacticalActorBase> __result)
                {
                    try
                    {
                        if (__instance.Description.LocalizationKey == "BASEDEFENSE_INFESTATION_OBJECTIVE" && __result.Count() > 0)
                        {
                            //  TFTVLogger.Always("Got passed if check");

                            List<TacticalActorBase> actorsToKeep = new List<TacticalActorBase>();

                            foreach (TacticalActorBase tacticalActorBase in __result)
                            {
                                if (!tacticalActorBase.Status.HasStatus<MindControlStatus>())
                                {
                                    //  
                                    actorsToKeep.Add(tacticalActorBase);
                                }
                                else
                                {
                                    TFTVLogger.Always($"{tacticalActorBase.name} has mind controlled status!");

                                }
                            }

                            __result = actorsToKeep;

                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            public static void RemoveScatterRemainingAttackersTagFromEnemiesWithParasychosis(TacticalAbility ability, object parameter)
            {
                try
                {
                    ApplyEffectAbilityDef parasychosis = DefCache.GetDef<ApplyEffectAbilityDef>("Parasychosis_AbilityDef");

                    if (ability.TacticalAbilityDef == parasychosis && parameter is TacticalAbilityTarget target
                        && target.GetTargetActor() != null && target.GetTargetActor() is TacticalActor tacticalActor && tacticalActor.HasGameTag(_scatterEnemiesObjectiveTag))
                    {
                        //  TFTVLogger.Always($", target is {tacticalActor.name}");
                        tacticalActor.GameTags.Remove(_scatterEnemiesObjectiveTag);
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            [HarmonyPatch(typeof(TacticalFaction), "HasUndeployedTacActors")]
            public static class SurviveTurnsFactionObjective_HasUndeployedTacActors_BaseDefense_Patch
            {
                public static void Postfix(TacticalFaction __instance, ref bool __result)
                {
                    try
                    {
                        if (__instance.Faction.FactionDef == Shared.AlienFactionDef && __result == false)
                        {
                            FactionObjective survive5turns = __instance.TacticalLevel.GetFactionByCommandName("px").Objectives.FirstOrDefault(o => o.Description.LocalizationKey == "BASEDEFENSE_SURVIVE5_OBJECTIVE");

                            if (survive5turns != null && survive5turns.State != FactionObjectiveState.Achieved)

                            {
                                //   TFTVLogger.Always($"Survive 5 turns active, so result {__result} is being changed to true");
                                __result = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }


            //Method to add objective tag on Pandorans for the Scatter Attackers objective
            //Doesn't activate if Pandoran faction not present

            public static void AddScatterObjectiveTagForBaseDefense(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {

                    ClassTagDef AcheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");

                    // TFTVLogger.Always("ActorEnteredPlay invoked");
                    if (CheckIfBaseDefenseVsAliens(__instance))
                    {
                        if (actor.TacticalFaction.Faction.FactionDef.MatchesShortName("aln")
                            && actor is TacticalActor tacticalActor
                            && (actor.GameTags.Contains(crabTag) || actor.GameTags.Contains(fishmanTag) || actor.GameTags.Contains(sirenTag) || actor.GameTags.Contains(AcheronTag))
                            && !actor.GameTags.Contains(_scatterEnemiesObjectiveTag)
                            )
                        {
                            actor.GameTags.Add(_scatterEnemiesObjectiveTag);
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void AdjustReinforcements(TacMissionTypeDef missionType)
            {
                try
                {
                    float timeLeft = TimeLeft;
                    TFTVLogger.Always($"Adjusting regular reinforcements for base defense");
                    if (timeLeft > 12)
                    {
                        TFTVLogger.Always($"Because timeLeft is {timeLeft}, setting regular reinforcements to appear");
                        missionType.ParticipantsData[0].ReinforcementsDeploymentPart = new Base.Utils.RangeData() { Max = 0.3f, Min = 0.3f };
                        missionType.ParticipantsData[0].ReinforcementsTurns = new Base.Utils.RangeDataInt() { Max = 3, Min = 2 };
                    }
                    else
                    {
                        TFTVLogger.Always($"Because timeLeft is {timeLeft}, setting regular reinforcements to NOT appear");
                        missionType.ParticipantsData[0].ReinforcementsDeploymentPart = new Base.Utils.RangeData() { Max = 0.0f, Min = 0.0f };
                        missionType.ParticipantsData[0].ReinforcementsTurns = new Base.Utils.RangeDataInt() { Max = 0, Min = 0 };
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void ModifyBaseDefenseTacticalObjectives(TacMissionTypeDef missionType)
            {
                try
                {
                    GameTagDef baseDefense = DefCache.GetDef<GameTagDef>("MissionTypePhoenixBaseDefence_MissionTagDef");
                  
                    if (missionType.Tags.Contains(baseDefense) && missionType.ParticipantsData[0].FactionDef == DefCache.GetDef<PPFactionDef>("Alien_FactionDef"))
                    {

                        TFTVLogger.Always("ModifyBaseDefenseObjectives");
                        List<FactionObjectiveDef> listOfFactionObjectives = missionType.CustomObjectives.ToList();

                        KillActorFactionObjectiveDef killSpawnery = TFTVBaseDefenseGeoscape.KillInfestation;
                        KillActorFactionObjectiveDef killSentinel = TFTVBaseDefenseGeoscape.KillSentinel;
                        KillActorFactionObjectiveDef killScylla = TFTVBaseDefenseGeoscape.KillScylla;

                        SurviveTurnsFactionObjectiveDef survive3turns = TFTVBaseDefenseGeoscape.SurviveThreeTurns;
                        SurviveTurnsFactionObjectiveDef survive5turns = TFTVBaseDefenseGeoscape.SurviveFiveTurns;

                        KillActorFactionObjectiveDef scatterEnemies = TFTVBaseDefenseGeoscape.ScatterEnemies;
                        WipeEnemyFactionObjectiveDef killAllEnemies = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("E_DefeatEnemies [PhoenixBaseDefense_CustomMissionTypeDef]");
                        ProtectKeyStructuresFactionObjectiveDef protectFacilities = DefCache.GetDef<ProtectKeyStructuresFactionObjectiveDef>("E_ProtectKeyStructures [PhoenixBaseDefense_CustomMissionTypeDef]");

                        if (listOfFactionObjectives.Contains(killAllEnemies))
                        {
                            listOfFactionObjectives.Remove(killAllEnemies);
                        }
                        if (listOfFactionObjectives.Contains(protectFacilities))
                        {
                            listOfFactionObjectives.Remove(protectFacilities);
                        }

                        if (ScyllaLoose)
                        {
                            if (listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Remove(killSentinel);
                            }
                            if (listOfFactionObjectives.Contains(survive3turns))
                            {
                                listOfFactionObjectives.Remove(survive3turns);
                            }
                            if (listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Remove(killSpawnery);
                            }
                            if (listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Remove(survive5turns);
                            }
                            if (!listOfFactionObjectives.Contains(killScylla))
                            {
                                listOfFactionObjectives.Add(killScylla);
                            }
                        }
                        else if (TimeLeft < 6)
                        {
                            if (!listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Add(killSpawnery);
                            }
                            if (listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Remove(killSentinel);
                            }
                            if (listOfFactionObjectives.Contains(survive3turns))
                            {
                                listOfFactionObjectives.Remove(survive3turns);
                            }
                            if (listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Remove(survive5turns);
                            }

                        }
                        else if (TimeLeft < 12 && TimeLeft >= 6)
                        {
                            if (!listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Add(killSentinel);

                            }
                            if (listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Remove(killSpawnery);
                            }
                            if (listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Remove(survive5turns);
                            }
                        }
                        else
                        {
                            if (!listOfFactionObjectives.Contains(survive5turns))
                            {
                                listOfFactionObjectives.Add(survive5turns);
                            }
                            if (listOfFactionObjectives.Contains(survive3turns))
                            {
                                listOfFactionObjectives.Remove(survive3turns);
                            }
                            if (listOfFactionObjectives.Contains(killSentinel))
                            {
                                listOfFactionObjectives.Remove(killSentinel);
                            }
                            if (listOfFactionObjectives.Contains(killSpawnery))
                            {
                                listOfFactionObjectives.Remove(killSpawnery);
                            }
                        }

                        missionType.CustomObjectives = listOfFactionObjectives.ToArray();
                        AdjustReinforcements(missionType);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }




        }

        internal class Map
        {
            internal static Vector3 AccessLiftDeployPos;
            internal static Vector3 EntranceExitCentralPos;
            internal static Vector3 EntrancePhaseIPlayerSpawn;
            internal static List<Vector3> ContainmentBreachSpawn = new List<Vector3>();

            internal class Containment
            {
                public static void CheckDamageContainment(TacticalActorBase tacticalActorBase)
                {
                    try
                    {

                        if (tacticalActorBase != null && CheckIfBaseDefenseVsAliens(tacticalActorBase.TacticalLevel) && tacticalActorBase.name != null && tacticalActorBase.name.Contains("StructuralTarget"))
                        {

                        }
                        else
                        {
                            return;
                        }

                        if (PandoransInContainment != null && PandoransInContainment.Count > 0)
                        {
                            if (ContainmentBreachSpawn.Any(p => (p - tacticalActorBase.Pos).magnitude < 4))
                            {
                                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                                Vector3 vector3 = ContainmentBreachSpawn.FirstOrDefault(p => (p - tacticalActorBase.Pos).magnitude < 4);
                                PandoranDeployment.SetContainmentDynamicBreachSpawns(controller, vector3);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }
            }

            public static void FirstTurnBaseDefenseDeployment(TacticalLevelController controller)
            {
                try
                {
                    if (controller.TacMission.MissionData.MissionType.MissionTypeTag != baseDefenseTag)
                    {
                        return;
                    }

                    Consoles.GetConsoles();
                    GoldShiftSetup();
                 
                    if (Breach)
                    {
                        PandoranDeployment.SpawnEscapedPandoransInitialDeployment();
                    }

                }


                catch (Exception e)  
                {
                    TFTVLogger.Error(e);

                }
            }

            internal class Consoles
            {
                internal static void GetConsoles()
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (CheckIfBaseDefenseVsAliens(controller))
                        {
                            List<Breakable> consoles = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("NJR_LoCov_Console")).ToList();
                            Vector3[] position = new Vector3[consoles.Count];

                            consoles = consoles.OrderByDescending(c => c.transform.position.z).ToList();

                            for (int x = 0; x < consoles.Count; x++)
                            {
                                if (consoles[x].transform.rotation.y == 1.0f)
                                {
                                    ConsolePositions.Add(consoles[x].transform.position.z + 1, consoles[x].transform.position.x);
                                    TFTVLogger.Always($"there is a console at {consoles[x].transform.position} with rotation {consoles[x].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(x).Key} for z coordinate, and {ConsolePositions.ElementAt(x).Value} for x coordinate ");
                                }
                                else
                                {
                                    ConsolePositions.Add(consoles[x].transform.position.z, consoles[x].transform.position.x + 1);
                                    TFTVLogger.Always($"there is a console at {consoles[x].transform.position} with rotation {consoles[x].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(x).Key} for z coordinate, and {ConsolePositions.ElementAt(x).Value} for x coordinate ");
                                }
                            }

                            /*  ConsolePositions.Add(consoles[0].transform.position.z + 1, consoles[0].transform.position.x);
                              TFTVLogger.Always($"there is a console at {consoles[0].transform.position} with rotation {consoles[0].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(0).Key} for z coordinate, and {ConsolePositions.ElementAt(0).Value} for x coordinate ");
                              ConsolePositions.Add(consoles[1].transform.position.z, consoles[1].transform.position.x + 1);
                              TFTVLogger.Always($"there is a console at {consoles[1].transform.position} with rotation {consoles[1].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(1).Key} for z coordinate, and {ConsolePositions.ElementAt(1).Value} for x coordinate ");
                              ConsolePositions.Add(consoles[2].transform.position.z, consoles[2].transform.position.x + 1);
                              TFTVLogger.Always($"there is a console at {consoles[2].transform.position} with rotation {consoles[2].transform.rotation}, recording it in the dictionary as {ConsolePositions.ElementAt(2).Key} for z coordinate, and {ConsolePositions.ElementAt(2).Value} for x coordinate ");*/
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);

                    }
                }

                internal class SpawnConsoles
                {
                    public static void PlaceObjectives(TacticalLevelController controller)
                    {
                        try
                        {
                            if (CheckIfBaseDefenseVsAliens(controller))
                            {
                                if (StratToBeImplemented != 0 && VentingHintShown == false)
                                {
                                    VentingHintShown = true;
                                    InteractionPointPlacement();
                                }
                                TFTVLogger.Always($"Base defense:");
                                TFTVLogger.Always($"breach? {Breach}", false);
                                TFTVLogger.Always($"Scylla Loose? {ScyllaLoose}", false);
                                TFTVLogger.Always($"Pandorans in containment count: {PandoransInContainment.Count}", false);
                            }

                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    internal static void SpawnInteractionPoint(Vector3 position, string name)
                    {
                        try
                        {
                            StructuralTargetDeploymentDef stdDef = DefCache.GetDef<StructuralTargetDeploymentDef>("HackableConsoleStructuralTargetDeploymentDef");

                            TacActorData tacActorData = new TacActorData
                            {
                                ComponentSetTemplate = stdDef.ComponentSet
                            };


                            StructuralTargetInstanceData structuralTargetInstanceData = tacActorData.GenerateInstanceData() as StructuralTargetInstanceData;
                            //  structuralTargetInstanceData.FacilityID = facilityID;
                            structuralTargetInstanceData.SourceTemplate = stdDef;
                            structuralTargetInstanceData.Source = tacActorData;


                            StructuralTarget structuralTarget = ActorSpawner.SpawnActor<StructuralTarget>(tacActorData.GenerateInstanceComponentSetDef(), structuralTargetInstanceData, callEnterPlayOnActor: false);
                            GameObject obj = structuralTarget.gameObject;
                            structuralTarget.name = name;
                            structuralTarget.Source = obj;

                            var ipCols = new GameObject("InteractionPointColliders");
                            ipCols.transform.SetParent(obj.transform);
                            ipCols.tag = InteractWithObjectAbilityDef.ColliderTag;

                            ipCols.transform.SetPositionAndRotation(position, Quaternion.identity);
                            var collider = ipCols.AddComponent<BoxCollider>();


                            structuralTarget.Initialize();
                            //TFTVLogger.Always($"Spawning interaction point with name {name} at position {position}");
                            structuralTarget.DoEnterPlay();

                            //    TacticalActorBase

                            StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                            structuralTarget.Status.ApplyStatus(activeConsoleStatusDef);

                            TFTVLogger.Always($"{name} is at position {position}");
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    internal static void InteractionPointPlacement()
                    {
                        try
                        {
                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                            if (CheckIfBaseDefenseVsAliens(controller))
                            {
                                List<Breakable> consoles = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("NJR_LoCov_Console") && b.GetToughness() > 0).ToList();
                                Vector3[] culledConsolePositions = new Vector3[3];

                                consoles = consoles.OrderByDescending(c => c.transform.position.z).ToList();
                                TFTVLogger.Always($"{consoles.Count} consoles were found");

                                if (ConsolePositions.Count() == 0)
                                {
                                    TFTVLogger.Always($"Console Position {ConsolePositions.Count()}! Reacquiring to avoid errors");
                                    GetConsoles();
                                }

                                TFTVLogger.Always($"Console Positions count: {ConsolePositions.Count()}");

                                for (int x = 0; x < ConsolePositions.Count; x++)
                                {
                                    foreach (Breakable breakable in consoles)
                                    {
                                        //  TFTVLogger.Always($"here this breakable {breakable.name}");

                                        //  TFTVLogger.Always($"Console positions? {ConsolePositions.Count()}");
                                        TFTVLogger.Always($"difference in x coordinates is {Math.Abs((int)(ConsolePositions.ElementAt(x).Value - breakable?.transform?.position.x))} and in z coordinates {Math.Abs((int)(ConsolePositions.ElementAt(x).Key - breakable?.transform?.position.z))}");

                                        if (Math.Abs((int)(ConsolePositions.ElementAt(x).Value - breakable?.transform?.position.x)) < 5 && Math.Abs((int)(ConsolePositions.ElementAt(x).Key - breakable?.transform?.position.z)) < 5)
                                        {
                                            TFTVLogger.Always($"Found breakable at position {breakable.transform.position}, close to interaction point at {ConsolePositions.ElementAt(x)}");
                                            culledConsolePositions[x].y = breakable.transform.position.y;
                                            culledConsolePositions[x].x = ConsolePositions.ElementAt(x).Value;
                                            culledConsolePositions[x].z = ConsolePositions.ElementAt(x).Key;
                                        }
                                    }
                                }

                                for (int x = 0; x < culledConsolePositions.Count(); x++)
                                {
                                    if (culledConsolePositions[x] != null && culledConsolePositions[x] != new Vector3(0, 0, 0))
                                    {
                                        SpawnInteractionPoint(culledConsolePositions[x], ConsoleName + x);
                                    }
                                }

                                ActivateConsole.CheckIfConsoleActivated(controller);
                            }
                        }

                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                }

                internal class ActivateConsole
                {
                    public static void BaseDefenseConsoleActivated(StatusComponent statusComponent, Status status, TacticalLevelController controller)
                    {
                        try
                        {

                            if (controller != null && CheckIfBaseDefenseVsAliens(controller))
                            {
                                if (status.Def == DefCache.GetDef<StatusDef>("ConsoleActivated_StatusDef"))
                                {
                                    StructuralTarget console = statusComponent.transform.GetComponent<StructuralTarget>();
                                    List<StructuralTarget> generators = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().Where(st => st.Deployment != null).Where(st => st.Deployment.name.Equals("PP_Cover_Generator_2x2_A_StructuralTarget")).ToList();

                                    TFTVLogger.Always($"Console {console.name} activated. Generators count: {generators.Count}");

                                    for (int i = 0; i < 3; i++)
                                    {
                                        TFTVLogger.Always($"Console {console.name}  {ConsoleName + i}? {ConsolePositions.ElementAt(i).Value} ");

                                        if (console.name.Equals(ConsoleName + i) && ConsolePositions.ElementAt(i).Value != 1000)
                                        {
                                            //  TFTVLogger.Always($"Console {console.name} activation logged");
                                            float keyToChange = ConsolePositions.ElementAt(i).Key;
                                            ConsolePositions[keyToChange] = 1000;

                                            StratToBeImplemented = 0;

                                            if (generators.Count > 0)
                                            {
                                                foreach (StructuralTarget structuralTarget in generators)
                                                {

                                                    TFTVLogger.Always($"Applying damage to generators: current health is {structuralTarget.GetHealth()}, reducing it by {60}");
                                                    structuralTarget.Health.Subtract(60);
                                                    TFTVLogger.Always($"Current health is {structuralTarget.GetHealth()}");

                                                }

                                                Explosions.GenerateRandomExplosions();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    [HarmonyPatch(typeof(TacticalPrompt), "Show")]
                    public static class TacticalPrompt_AddStatus_patch
                    {
                        public static void Prefix(TacticalPrompt __instance)
                        {
                            try
                            {
                                // TFTVLogger.Always($"Showing prompt {__instance.PromptDef.name}");

                                TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                                TacticalPromptDef activateObjective = DefCache.GetDef<TacticalPromptDef>("ActivateObjectivePromptDef");
                                TacticalPromptDef consoleBaseDefenseObjective = DefCache.GetDef<TacticalPromptDef>("TFTVBaseDefensePrompt");

                                if (__instance.PromptDef == activateObjective && controller.TacMission.MissionData.UsePhoenixBaseLayout)
                                {
                                    //  TFTVLogger.Always("Got past the if on the prompt");
                                    __instance.PromptDef = consoleBaseDefenseObjective;
                                }
                            }
                            catch (Exception e)
                            {
                                TFTVLogger.Error(e);
                            }
                        }
                    }

                    internal static void DeactivateConsole(string name)
                    {
                        try
                        {
                            TFTVLogger.Always($"Looking for console with name {name}");

                            StatusDef activeConsoleStatusDef = DefCache.GetDef<StatusDef>("ActiveInteractableConsole_StatusDef");
                            StructuralTarget console = UnityEngine.Object.FindObjectsOfType<StructuralTarget>().FirstOrDefault(b => b.name.Equals(name));

                            if (console != null)
                            {
                                TFTVLogger.Always($"Found console {console.name}");

                                Status status = console?.Status?.GetStatusByName(activeConsoleStatusDef.EffectName);

                                if (status != null)
                                {
                                    TFTVLogger.Always($"found status {status.Def.EffectName}");
                                    console.Status.UnapplyStatus(status);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }

                    internal static void CheckIfConsoleActivated(TacticalLevelController controller)
                    {
                        try
                        {
                            if (CheckIfBaseDefenseVsAliens(controller))
                            {
                                for (int x = 0; x < ConsolePositions.Count(); x++)
                                {
                                    // TFTVLogger.Always($"{ConsoleInBaseDefense[x]}");

                                    if (ConsolePositions.ElementAt(x).Value == 1000)
                                    {
                                        DeactivateConsole(ConsoleName + x);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }


                }

                internal class Explosions
                {
                    internal static void GenerateExplosion(Vector3 position)
                    {
                        try
                        {
                            DelayedEffectDef explosion = DefCache.GetDef<DelayedEffectDef>("ExplodingBarrel_ExplosionEffectDef");

                            Vector3 vector3 = new Vector3(position.x + UnityEngine.Random.Range(-4, 4), position.y, position.z + UnityEngine.Random.Range(-4, 4));


                            Effect.Apply(Repo, explosion, new EffectTarget
                            {
                                Position = vector3
                            }, null);

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



                            //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                            /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                              {
                                  ChaseVector = position
                              };*/

                            //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                            //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                            //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }
                    internal static void GenerateFireExplosion(Vector3 position)
                    {
                        try
                        {
                            // FireExplosionEffectDef explosion = DefCache.GetDef<FireExplosionEffectDef>("E_FireExplosionEffect [Fire_StandardDamageTypeEffectDef]");
                            SpawnTacticalVoxelEffectDef spawnFire = DefCache.GetDef<SpawnTacticalVoxelEffectDef>("FireVoxelSpawnerEffect");

                            Vector3 vector3 = new Vector3(position.x + UnityEngine.Random.Range(-4, 4), position.y, position.z + UnityEngine.Random.Range(-4, 4));

                            Effect.Apply(Repo, spawnFire, new EffectTarget
                            {
                                Position = vector3
                            }, null);

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



                            //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                            /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                              {
                                  ChaseVector = position
                              };*/

                            //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                            //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                            //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }
                    internal static void GenerateFakeExplosion(Vector3 position)
                    {
                        try
                        {
                            DelayedEffectDef explosion = DefCache.GetDef<DelayedEffectDef>("FakeExplosion_ExplosionEffectDef");


                            Effect.Apply(Repo, explosion, new EffectTarget
                            {
                                Position = position
                            }, null);

                            //   TacticalLevelController controllerTactical = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();



                            //  PlanarScrollCamera camera = controllerTactical.View.CameraDirector.Manager.CurrentBehavior as PlanarScrollCamera;

                            /*  CameraChaseParams cameraChaseParams = new CameraChaseParams
                              {
                                  ChaseVector = position
                              };*/

                            //  camera.DoTemporaryChase(cameraChaseParams, cameraChaseParams);
                            //  CameraDirectorParams cameraParams = new CameraDirectorParams() {OriginPosition = camera.CenterWorldPos, TargetPosition=position }; 


                            //  controllerTactical.View.CameraDirector.Hint(CameraDirectorHint.EnterPlay, cameraParams);


                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }
                    internal static void GenerateRandomExplosions()
                    {
                        try
                        {

                            TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();
                            List<TacticalDeployZone> zones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>());
                            zones.RemoveRange(DeploymentZones.FindHangarTopsideDeployZones(controller));

                            int explosions = 0;
                            foreach (TacticalDeployZone zone in zones)
                            {
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                                int roll = UnityEngine.Random.Range(0, 4);

                                if (roll == 0)
                                {
                                    GenerateFireExplosion(zone.Pos);
                                    explosions++;
                                    TFTVLogger.Always($"explosion count {explosions}");
                                }
                                else if (roll == 1)
                                {
                                    GenerateExplosion(zone.Pos);
                                    explosions++;
                                    TFTVLogger.Always($"explosion count {explosions}");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);

                        }
                    }

                }

                [HarmonyPatch(typeof(Breakable), "Explode")]
                public static class Breakable_Explode_Experiment_patch
                {
                    public static void Postfix(Breakable __instance)
                    {
                        try
                        {
                            if (__instance.name.StartsWith("NJR_LoCov_Console"))
                            {
                                for (int x = 0; x < ConsolePositions.Count; x++)
                                {
                                    TFTVLogger.Always($"difference in x coordinates is " +
                                        $"{Math.Abs((int)(ConsolePositions.ElementAt(x).Value - __instance.transform?.position.x))} " +
                                        $"and in z coordinates {Math.Abs((int)(ConsolePositions.ElementAt(x).Key - __instance.transform?.position.z))}");

                                    if (Math.Abs(ConsolePositions.ElementAt(x).Value - __instance.transform.position.x) < 5
                                        && Math.Abs(ConsolePositions.ElementAt(x).Key - __instance.transform.position.z) < 5)
                                    {
                                        float keyToChange = ConsolePositions.ElementAt(x).Key;
                                        ConsolePositions[keyToChange] = 1000;
                                        TFTVLogger.Always($"{ConsoleName + x} exploded!");
                                        Map.Consoles.ActivateConsole.DeactivateConsole(ConsoleName + x);
                                        TFTVLogger.Always($"{ConsoleName + x} deactivated!");
                                    }
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

            internal class DeploymentZones
            {
                internal static TacticalDeployZone VehicleBayCentralDeployZone;
                internal static TacticalDeployZone SecondaryStrikeForceSpawn;
                internal static List<float> SecondaryStrikeForceVector;
                internal static List<TacticalDeployZone> VehicleBayCentralDeployZones;
                //   internal static List<TacticalDeployZone> TopSideHangarDeployZones;

                public static void InitDeployZonesForBaseDefenseVsAliens(TacticalLevelController controller)
                {
                    try
                    {
                        if (CheckIfBaseDefenseVsAliens(controller))
                        {
                            TFTVLogger.Always($"Initializing Deploy Zones for BD vs Aliens");

                            FindVehicleBayCentralDeployZone(controller);
                            FindCenterSpaceDeployZones(controller);
                            SetPlayerSpawnAccessLift(controller);
                            FindEntranceExitCentralPos();
                            FindEntrancePhaseIPlayerSpawn();
                            FindContainmentBreachPos();
                        }

                        if (controller.IsFromSaveGame)
                        {
                            return;
                        }


                        StartingDeployment.Init(controller);
                        //   FindHangarTopsideDeployZones(controller);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void SetPlayerSpawnAccessLift(TacticalLevelController controller)
                {
                    try
                    {
                        TFTVLogger.Always($"Setting up AccessLift", false);

                        MeshCollider playerAccessLift = UnityEngine.Object.FindObjectsOfType<MeshCollider>().FirstOrDefault(b => b.name.StartsWith("PP_Floor_AccessLift"));
                        AccessLiftDeployPos = playerAccessLift.bounds.center;

                        List<TacticalDeployZone> candidates = controller.Map.GetActors<TacticalDeployZone>().Where
                            (tdz =>
                            tdz.Pos.y > 4
                            && tdz.name.Contains("Deploy_Player_1x1_Elite_Grunt_Drone")
                            ).ToList();

                        List<TacticalDeployZone> playerAccessLiftSpawns = new List<TacticalDeployZone>() { candidates[0], candidates[1], candidates[2] };

                        //need this anyway for Triton infiltration team
                        int requiredTdz = 3;


                        /*_listLift.Count / 3;

                    if (_listLift.Count % 3 > 0)
                    {
                        requiredTdz++;
                    }

                    if (requiredTdz > 3) 
                    {
                        requiredTdz = 3;
                    }*/


                        //  TFTVLogger.Always($"Because list Lift has {_listLift.Count}, requiredTdz {requiredTdz}");

                        for (int x = 0; x <= requiredTdz - 1; x++)
                        {
                            int xVariation = 0;
                            int zVariation = 0;

                            if (x == 0)
                            {
                                xVariation = 3;
                            }
                            else if (x == 1)
                            {
                                xVariation = -3;
                            }
                            else if (x == 2)
                            {
                                zVariation = 3;
                            }

                            TacticalDeployZone deployZone = playerAccessLiftSpawns[x];
                            BoxCollider boxCollider = deployZone.GetComponent<BoxCollider>();

                            Vector3 oldPosition = deployZone.transform.position;
                            Vector3 newPosition = new Vector3(Map.AccessLiftDeployPos.x + xVariation, oldPosition.y, Map.AccessLiftDeployPos.z + zVariation);

                            deployZone.SetPosition(newPosition);

                            boxCollider.center = Vector3.zero; // Reset the center
                            boxCollider.size = Vector3.one; // Reset the size (if necessary)

                            // Calculate the new center position based on the GameObject's position
                            Vector3 newColliderCenter = deployZone.transform.InverseTransformPoint(newPosition);
                            boxCollider.center = newColliderCenter;

                            TFTVLogger.Always($"access lift: {deployZone.name} {deployZone.Pos}");// bounds center: {boxCollider.center}");

                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void FindAccessLiftCentralPos()
                {
                    try
                    {

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void FindEntranceExitCentralPos()
                {
                    try
                    {
                        BoxCollider entrancePlayerExitZone = UnityEngine.Object.FindObjectsOfType<BoxCollider>().FirstOrDefault(b => b.name.StartsWith("PlayerExitZone") && b.bounds.center.y < 4);
                        EntranceExitCentralPos = entrancePlayerExitZone.bounds.center;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void FindEntrancePhaseIPlayerSpawn()
                {
                    try
                    {
                        EntrancePhaseIPlayerSpawn = EntranceExitCentralPos + new Vector3(1.5f, 0, 19);

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                private static void FindContainmentBreachPos()
                {
                    try
                    {
                        List<Breakable> objectsOfInterestContainment = UnityEngine.Object.FindObjectsOfType<Breakable>()
                            .Where(b => b.name.StartsWith("PP_Cover_AlienCage"))
                            .ToList();

                        for (int x = 1; x < objectsOfInterestContainment.Count; x++)
                        {
                            Vector3 pos0 = objectsOfInterestContainment[x].transform.position;
                            Vector3 pos1 = objectsOfInterestContainment[x - 1].transform.position;

                            float distance = (pos0 - pos1).magnitude;
                            Vector3 offset = new Vector3(2, 0, 2);

                            if (distance < 8)
                            {
                                ContainmentBreachSpawn.Add(new Vector3(
                                    pos0.x > pos1.x ? pos1.x + offset.x : pos0.x + offset.x,
                                    pos0.y > pos1.y ? pos1.y : pos0.y,
                                    pos0.z > pos1.z ? pos1.z + offset.z : pos0.z + offset.z
                                ));
                            }
                        }

                        foreach (Vector3 vector3 in ContainmentBreachSpawn)
                        {
                            TFTVLogger.Always($"Added the following vectors for containment spawn: {vector3}");
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }



                internal static TacticalDeployZone FindVehicleBayCentralDeployZone(TacticalLevelController controller)
                {
                    try
                    {
                        MeshCollider vehicleBayMeshCollider = UnityEngine.Object.FindObjectsOfType<MeshCollider>().FirstOrDefault(b => b.name.StartsWith("PP_Floor_VehicleBay"));

                        TacticalDeployZone centralDeployZone = controller.Map.GetActors<TacticalDeployZone>(null).FirstOrDefault(tdz => (tdz.Pos - vehicleBayMeshCollider.bounds.center).magnitude < 2);

                        VehicleBayCentralDeployZone = centralDeployZone;

                        TFTVLogger.Always($"BD/DEPLOYMENT ZONES: VehicleBayCentralDeployZone is at {VehicleBayCentralDeployZone.Pos}");

                        return centralDeployZone;
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static List<TacticalDeployZone> FindCenterSpaceDeployZones(TacticalLevelController controller)
                {
                    try
                    {
                        TacticalDeployZone vehicleBayCentralDeployZone = VehicleBayCentralDeployZone;

                        List<TacticalDeployZone> centralDeployZones = controller.Map.GetActors<TacticalDeployZone>(null).Where(tdz => (tdz.Pos - vehicleBayCentralDeployZone.Pos).magnitude < 8 && tdz.Pos != vehicleBayCentralDeployZone.Pos).ToList();

                        VehicleBayCentralDeployZones = centralDeployZones;

                        TFTVLogger.Always($"BD/DEPLOYMENT ZONES: VehicleBayCentralDeployZones count {VehicleBayCentralDeployZones.Count()}");

                        return centralDeployZones;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static List<TacticalDeployZone> FindHangarTopsideDeployZones(TacticalLevelController controller)
                {
                    try
                    {
                        List<TacticalDeployZone> topsideDeployZones =
                            new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>().Where(tdz => tdz.Pos.y > 4 && VehicleBayCentralDeployZones.Any(cdz => (cdz.Pos - tdz.Pos).magnitude < 12)));

                        //TopSideHangarDeployZones = topsideDeployZones;

                        TFTVLogger.Always($"BD/DEPLOYMENT ZONES: TopSideHangarDeployZones count {topsideDeployZones.Count()}");

                        return topsideDeployZones;

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                private static BreachEntrance GetBreachEntrance(TacticalLevelController controller)
                {
                    try
                    {
                        List<BreachEntrance> breachEntrances = controller.Map.GetActors<BreachEntrance>().Where(b => b.Pos.z > 40).ToList();

                        if (breachEntrances.Count == 0)
                        {
                            breachEntrances = controller.Map.GetActors<BreachEntrance>().ToList();
                        }

                        breachEntrances = breachEntrances.OrderByDescending(tdz => (tdz.Pos - VehicleBayCentralDeployZone.Pos).magnitude).ToList();

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int roll = UnityEngine.Random.Range(0, Math.Max(breachEntrances.Count, 1));

                        TFTVLogger.Always($"number of eligible breachEntrances for secondary strike force: {breachEntrances.Count}, roll:{roll}");

                        return breachEntrances[roll];

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static BreachEntrance FindBreachEntrance(TacticalLevelController controller, Vector3 position)
                {
                    try
                    {
                        return controller.Map.GetActors<BreachEntrance>().FirstOrDefault(b => (b.Pos - position).magnitude < 8);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static TacticalDeployZone FindSecondaryStrikeDeployZone(TacticalLevelController controller)
                {
                    try
                    {
                        if (SecondaryStrikeForceSpawn == null && SecondaryStrikeForceVector == null)
                        {
                            BreachEntrance breach = GetBreachEntrance(controller);

                            Vector3 position = new Vector3();

                            if (EntranceUtils.GetEntranceObject(breach.transform, "TUNNEL", out Transform shutState) && EntranceUtils.GetEntranceObject(breach.transform, "WALL", out Transform shutState2))
                            {
                                TFTVLogger.Always($"shutState is null? {shutState == null} local scale: {shutState?.localScale}");
                                TFTVLogger.Always($"shutState2 is null? {shutState2 == null} local scale: {shutState2?.localScale}");

                                if (EntranceUtils.GetBounds(shutState, out Bounds result))
                                {
                                    TFTVLogger.Always($"result.center: {result.center}, max: {result.max}, min: {result.min}, size: {result.size}");
                                    position = result.center;
                                }
                            }

                            TFTVLogger.Always($"breach.Pos: {breach.Pos}, center: {position}");

                            TacticalDeployZone deployZone = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault(tdz => tdz.name.Contains("Deploy_Player_3x3_Vehicle"));

                            BoxCollider boxCollider = deployZone.GetComponent<BoxCollider>();

                            Vector3 newPosition = new Vector3(position.x, 4.8f, position.z);

                            deployZone.SetPosition(newPosition);

                            boxCollider.center = Vector3.zero; // Reset the center
                            boxCollider.size = Vector3.one; // Reset the size (if necessary)

                            // Calculate the new center position based on the GameObject's position
                            Vector3 newColliderCenter = deployZone.transform.InverseTransformPoint(newPosition);
                            boxCollider.center = newColliderCenter;
                            SecondaryStrikeForceSpawn = deployZone;
                            SecondaryStrikeForceVector = new List<float>() { deployZone.Pos.x, deployZone.Pos.y, deployZone.Pos.z };
                            TFTVLogger.Always($"secondary strike force position: {deployZone.name} {deployZone.Pos}");// bounds center: {boxCollider.center}");
                        }
                        else if (SecondaryStrikeForceVector != null)
                        {
                            TacticalDeployZone deployZone = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault(tdz => tdz.name.Contains("Deploy_Player_3x3_Vehicle"));

                            BoxCollider boxCollider = deployZone.GetComponent<BoxCollider>();
                            Vector3 newPosition = new Vector3(SecondaryStrikeForceVector[0], SecondaryStrikeForceVector[1], SecondaryStrikeForceVector[2]);
                            deployZone.SetPosition(newPosition);

                            boxCollider.center = Vector3.zero; // Reset the center
                            boxCollider.size = Vector3.one; // Reset the size (if necessary)

                            // Calculate the new center position based on the GameObject's position
                            Vector3 newColliderCenter = deployZone.transform.InverseTransformPoint(newPosition);
                            boxCollider.center = newColliderCenter;
                            SecondaryStrikeForceSpawn = deployZone;
                        }

                        TFTVLogger.Always($"{SecondaryStrikeForceSpawn.name} at {SecondaryStrikeForceSpawn.Pos}");

                        return SecondaryStrikeForceSpawn;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void CheckDepolyZones(TacticalLevelController controller)
                {
                    TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                    TacCharacterDef basicMyrmidon = DefCache.GetDef<TacCharacterDef>("Swarmer_AlienMutationVariationDef");
                    TacCharacterDef fireWorm = DefCache.GetDef<TacCharacterDef>("Fireworm_AlienMutationVariationDef");
                    TacCharacterDef crystalScylla = DefCache.GetDef<TacCharacterDef>("Scylla10_Crystal_AlienMutationVariationDef");
                    TacCharacterDef poisonMyrmidon = DefCache.GetDef<TacCharacterDef>("SwarmerVenomous_AlienMutationVariationDef");
                    TacCharacterDef meleeChiron = DefCache.GetDef<TacCharacterDef>("MeleeChiron");


                    TFTVLogger.Always($"MissionDeployment.CheckDepolyZones() called ...");
                    MissionDeployCondition missionDeployConditionToAdd = new MissionDeployCondition()
                    {
                        MissionData = new MissionDeployConditionData()
                        {
                            ActivateOnTurn = 0,
                            DeactivateAfterTurn = 0,
                            ActorTagDef = TFTVMain.Main.DefCache.GetDef<ActorDeploymentTagDef>("Queen_DeploymentTagDef"),
                            ExcludeActor = false
                        }
                    };

                    int numberOfSecondaryForces = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order) / 2;

                    List<TacticalDeployZone> usedZones = new List<TacticalDeployZone>();

                    TFTVLogger.Always($"The map has {controller.Map.GetActors<TacticalDeployZone>(null).ToList().Count} deploy zones");
                    foreach (TacticalDeployZone tacticalDeployZone in controller.Map.GetActors<TacticalDeployZone>(null).ToList())
                    {
                        /*  TFTVLogger.Always($"Deployment zone {tacticalDeployZone} with Def '{tacticalDeployZone.TacticalDeployZoneDef}'");
                          TFTVLogger.Always($"Mission participant is {tacticalDeployZone.MissionParticipant}");
                          TFTVLogger.Always($"Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");*/


                        if (tacticalDeployZone.MissionParticipant == TacMissionParticipant.Player && !usedZones.Contains(tacticalDeployZone))
                        {

                            if (tacticalDeployZone.Pos.y > 4)
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                                TFTVLogger.Always($"Found topside deployzone position and deploying basic myrmidon; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                                ActorDeployData actorDeployData = basicMyrmidon.GenerateActorDeployData();

                                actorDeployData.InitializeInstanceData();
                                usedZones.Add(tacticalDeployZone);
                                //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                            else if (tacticalDeployZone.Pos.y > 4)
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                                TFTVLogger.Always($"Found topside deployzone position and deploying mindfragger; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                                ActorDeployData actorDeployData = mindFragger.GenerateActorDeployData();
                                usedZones.Add(tacticalDeployZone);

                                actorDeployData.InitializeInstanceData();

                                //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                            else if (tacticalDeployZone.Pos.y > 4)
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                                TFTVLogger.Always($"Found topside deployzone position and deploying fireworm; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                                ActorDeployData actorDeployData = fireWorm.GenerateActorDeployData();
                                usedZones.Add(tacticalDeployZone);

                                actorDeployData.InitializeInstanceData();

                                //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }


                            if (tacticalDeployZone.Pos.y < 4)
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                                TFTVLogger.Always($"Found bottom deployzone position and deploying mindfragger; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                                ActorDeployData actorDeployData = meleeChiron.GenerateActorDeployData();
                                usedZones.Add(tacticalDeployZone);
                                actorDeployData.InitializeInstanceData();

                                //  TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, faction.TacticalFactionDef, faction.ParticipantKind, tacticalDeployZone.Pos, Quaternion.identity, null);

                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                        }
                    }
                }


                internal static List<TacticalDeployZone> GetEnemyDeployZones(TacticalLevelController controller)
                {
                    try
                    {

                        List<TacticalDeployZone> enemyDeployZones =
                            new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>().
                            Where(tdz => tdz.MissionParticipant.Equals(TacMissionParticipant.Intruder)
                            && tdz.MissionDeployment.Count() > 0)).ToList();

                        return enemyDeployZones;
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        }

        internal class StartingDeployment
        {
            private static void SpawnPXSecurityGuards(Breakable securityStation, TacticalLevelController controller)
            {
                try
                {
                    TacticalFactionDef phoenixFactionDef = DefCache.GetDef<TacticalFactionDef>("Phoenix_TacticalFactionDef");

                    List<Vector3> spawnPositions = new List<Vector3>();

                    if (_securityStationSpawnPositions.Count > 0)
                    {
                        TFTVLogger.Always($"Spawning {_numGuardsUnderPXControl} PX Guards for Phase I");

                        spawnPositions = _securityStationSpawnPositions;

                        void onLoadingCompletedForRegularSecurityGuard()
                        {
                            ActorDeployData actorDeployData = Defs.SecurityGuard.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            for (int x = 0; x < spawnPositions.Count; x++)
                            {
                                TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, phoenixFactionDef, TacMissionParticipant.Player, spawnPositions[x], securityStation.transform.rotation, null);
                                tacticalActorBase.Source = tacticalActorBase;
                                TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                                tacticalActor.Status.ApplyStatus(DefCache.GetDef<MindControlStatusDef>("UnderPhoenixControl_StatusDef"));
                                tacticalActor.ForceRestartTurn();
                            }

                            controller.SituationCache.Invalidate();
                            //  controller.View.ResetCharacterSelectedState();            
                        }

                        controller.AssetsLoader.StartLoadingRoots(Defs.SecurityGuard.AsEnumerable(), null, onLoadingCompletedForRegularSecurityGuard);
                    }
                    else
                    {


                        bool complete = false;

                        TFTVLogger.Always($"Going to spawn {_numGuardsUnderPXControl} PX Guards under PX control for Phase II/III, complete? {complete}");

                        void onLoadingCompletedForRegularSecurityGuardPhaseIIorIII()
                        {
                         
                            if (!complete)
                            {
                            
                                List<TacticalDeployZone> elegibleZones = controller.Map.GetActors<TacticalDeployZone>().Where(tdz =>
                               // tdz.TacticalFaction.TacticalFactionDef != phoenixFactionDef
                               (tdz.Pos - Map.DeploymentZones.VehicleBayCentralDeployZone.Pos).magnitude > 8 &&
                               (tdz.Pos - Map.AccessLiftDeployPos).magnitude > 5 &&
                               (tdz.Pos - Map.EntranceExitCentralPos).magnitude > 20 &&
                              !controller.Map.GetActors<TacticalActorBase>().Any(tab => tab != tdz && tab.Pos == tdz.Pos)
                              ).ToList();

                                ActorDeployData actorDeployData = Defs.SecurityGuard.GenerateActorDeployData();

                                actorDeployData.InitializeInstanceData();

                                TFTVLogger.Always($"There are {elegibleZones.Count} eligible zones");

                                if (elegibleZones.Count == 0)
                                {
                                    TFTVLogger.Always($"There are no eligible zones!");
                                }

                                for (int x = 0; x < _numGuardsUnderPXControl; x++)
                                {

                                    TacticalDeployZone randomlyChosenTDZ = elegibleZones.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                    TFTVLogger.Always($"randomlyChosenTDZ.Pos for PX Controlled Guard: {randomlyChosenTDZ.Pos}");

                                    elegibleZones.Remove(randomlyChosenTDZ);

                                    _alreadyUsedTDZForPXGuards.Add(randomlyChosenTDZ);

                                    randomlyChosenTDZ.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                                    TacticalActorBase tacticalActorBase = randomlyChosenTDZ.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, randomlyChosenTDZ);
                                    tacticalActorBase.Source = tacticalActorBase;

                                    TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                                    tacticalActor.Status.ApplyStatus(DefCache.GetDef<MindControlStatusDef>("UnderPhoenixControl_StatusDef"));
                                    tacticalActor.ForceRestartTurn();

                                }

                                complete = true;
                            }
                        }

                        AssetsReferencesLoader assetsLoader = controller.AssetsLoader;
                       
                        assetsLoader.StartLoadingRoots(Defs.SecurityGuard.AsEnumerable(), null, onLoadingCompletedForRegularSecurityGuardPhaseIIorIII);
  
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


            private static void SpawnALNSecurityGuards(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always($"Going to spawn {_numGuardsUnderALNControl} PX Guards under ALN control for Phase II/III");

                    bool complete = false;

                    void onLoadingCompletedForMFedSecurityGuard()
                    {
                        if (!complete)
                        {
                           // TFTVLogger.Always($"Loading assets complete; spawing");

                            List<TacticalDeployZone> elegibleZones = controller.Map.GetActors<TacticalDeployZone>().Where(tdz =>
                            // tdz.TacticalFaction.TacticalFactionDef != phoenixFactionDef
                            !_alreadyUsedTDZForPXGuards.Contains(tdz) &&
                            (tdz.Pos - Map.DeploymentZones.VehicleBayCentralDeployZone.Pos).magnitude > 8 &&
                            (tdz.Pos - Map.AccessLiftDeployPos).magnitude > 5 &&
                            (tdz.Pos - Map.EntranceExitCentralPos).magnitude > 20 &&
                           !controller.Map.GetActors<TacticalActorBase>().Any(tab => tab != tdz && tab.Pos == tdz.Pos)
                           ).ToList();

                            TFTVLogger.Always($"There are {elegibleZones.Count} eligible zones");

                            if (elegibleZones.Count == 0)
                            {
                                TFTVLogger.Always($"There are no eligible zones!");
                            }

                            ActorDeployData actorDeployData = Defs.MFedSecurityGuard.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            for (int x = 0; x < _numGuardsUnderALNControl; x++)
                            {
                                TacticalDeployZone randomlyChosenTDZ = elegibleZones.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                TFTVLogger.Always($"randomlyChosenTDZ.Pos for ALN controlled Guard: {randomlyChosenTDZ.Pos}");

                                elegibleZones.Remove(randomlyChosenTDZ);

                                randomlyChosenTDZ.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                                TacticalActorBase tacticalActorBase = randomlyChosenTDZ.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, randomlyChosenTDZ);
                                tacticalActorBase.Source = tacticalActorBase;
                            }

                            complete = true;

                        }
                    }

                    controller.AssetsLoader.StartLoadingRoots(Defs.MFedSecurityGuard.AsEnumerable(), null, onLoadingCompletedForMFedSecurityGuard);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }

            private static int _numGuardsUnderPXControl = 0;
            private static int _numGuardsUnderALNControl = 0;
            private static List<Vector3> _securityStationSpawnPositions = new List<Vector3>();
            private static List<TacticalDeployZone> _alreadyUsedTDZForPXGuards = new List<TacticalDeployZone>();


            private static List<Vector3> GetSecurityStationSpawns(TacticalLevelController controller)
            {
                try
                {
                    List<Breakable> security = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("PP_LoCov_SecurityRoom_Projector_3x3_A_StructuralTarget")).ToList();

                    TFTVLogger.Always($"Security Stations # {security.Count()}");

                    List<Vector3> spawnPositions = new List<Vector3>();

                    foreach (Breakable station in security)
                    {

                        List<Vector3> stationSpawnPositions = new List<Vector3>()
                              {
                            station.transform.position + new Vector3(-3, 0, 0),
                            station.transform.position + new Vector3(0, 0, -3),
                            station.transform.position + new Vector3(0, 0, 3)
                              };

                        spawnPositions.AddRange(stationSpawnPositions);
                    }

                    return spawnPositions;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static void GoldShiftSetup()
            {
                try
                {
                    TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                    _numGuardsUnderPXControl = 0;
                    _numGuardsUnderALNControl = 0;
                    _alreadyUsedTDZForPXGuards.Clear();
                    _securityStationSpawnPositions.Clear();

                    List<Breakable> security = UnityEngine.Object.FindObjectsOfType<Breakable>().Where(b => b.name.StartsWith("PP_LoCov_SecurityRoom_Projector_3x3_A_StructuralTarget")).ToList();

                    TFTVLogger.Always($"Security Stations # {security.Count()}");

                    if (security.Count == 0)
                    {
                        return;
                    }

                    TacticalFactionDef phoenixFactionDef = DefCache.GetDef<TacticalFactionDef>("Phoenix_TacticalFactionDef");
                    TacticalFactionDef alienFactionDef = DefCache.GetDef<TacticalFactionDef>("Alien_TacticalFactionDef");


                    if (TimeLeft >= 12 || !controller.Factions.Any(f => f.Faction.FactionDef==alienFactionDef))
                    {
                        foreach (Breakable station in security)
                        {
                            _numGuardsUnderPXControl += 3;

                            List<Vector3> spawnPositions = new List<Vector3>()
                              {
                            station.transform.position + new Vector3(-3, 0, 0),
                            station.transform.position + new Vector3(0, 0, -3),
                            station.transform.position + new Vector3(0, 0, 3)
                              };

                            _securityStationSpawnPositions.AddRange(spawnPositions);
                        }
                        TFTVLogger.Always($"Phase I Gold Shift Setup; PX: {_numGuardsUnderPXControl} ALN: {_numGuardsUnderALNControl}");
                    }
                    else
                    {
                        foreach (Breakable station in security)
                        {
                            _numGuardsUnderPXControl += 1;
                            _numGuardsUnderALNControl += 2;
                        }
                        TFTVLogger.Always($"Phase II / III Gold Shift Setup; PX: {_numGuardsUnderPXControl} ALN: {_numGuardsUnderALNControl}");
                    }

                    SpawnPXSecurityGuards(security.First(), controller);
                    SpawnALNSecurityGuards(controller);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public static void Init(TacticalLevelController controller)
            {
                try
                {
                    if (controller.TacMission.MissionData.MissionType.MissionTypeTag != baseDefenseTag)
                    {
                        return;
                    }

                    TFTVLogger.Always("Initiating Deploy Zones for a base defense mission; not from save game");

                    TFTVLogger.Always($"Attack on base progress is {TimeLeft}");

                    // TFTVLogger.Always($"Tutorial Base defense? {TutorialPhoenixBase}");

                    if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("aln")))
                    {
                        PlayerDeployment.SetUpDeployZones(controller);

                        if (ScyllaLoose)
                        {
                            PandoranDeployment.ScyllaLooseDeployment(controller);
                        }
                        else if (TimeLeft < 6)
                        {
                            PandoranDeployment.InfestationDeployment(controller);
                        }
                        else if (TimeLeft >= 6 && TimeLeft < 12)
                        {
                            PandoranDeployment.NestingDeployment(controller);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            internal class PandoranDeployment
            {

                private static readonly TacCharacterDef acidWormEgg = DefCache.GetDef<TacCharacterDef>("Acidworm_Egg_AlienMutationVariationDef");
                // TacCharacterDef explosiveEgg = DefCache.GetDef<TacCharacterDef>("Explosive_Egg_TacCharacterDef");
                private static readonly TacCharacterDef fraggerEgg = DefCache.GetDef<TacCharacterDef>("Facehugger_Egg_AlienMutationVariationDef");
                private static readonly TacCharacterDef fireWormEgg = DefCache.GetDef<TacCharacterDef>("Fireworm_Egg_AlienMutationVariationDef");
                private static readonly TacCharacterDef poisonWormEgg = DefCache.GetDef<TacCharacterDef>("Poisonworm_Egg_AlienMutationVariationDef");
                private static readonly TacCharacterDef swarmerEgg = DefCache.GetDef<TacCharacterDef>("Swarmer_Egg_TacCharacterDef");
                private static readonly TacCharacterDef sentinelHatching = DefCache.GetDef<TacCharacterDef>("SentinelHatching_AlienMutationVariationDef");
                private static readonly TacCharacterDef sentinelTerror = DefCache.GetDef<TacCharacterDef>("SentinelTerror_AlienMutationVariationDef");
                private static readonly TacCharacterDef sentinelMist = DefCache.GetDef<TacCharacterDef>("SentinelMist_AlienMutationVariationDef");
                private static readonly TacCharacterDef spawneryDef = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");

                private static readonly List<TacCharacterDef> eggs = new List<TacCharacterDef>() { acidWormEgg, fraggerEgg, fireWormEgg, poisonWormEgg, swarmerEgg };

                internal static void SpawnAdditionalEggs(TacticalLevelController controller)
                {
                    try
                    {
                        List<TacticalDeployZone> centralZones = Map.DeploymentZones.VehicleBayCentralDeployZones;

                        List<TacCharacterDef> availableTemplatesOrdered =
                            new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                        List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                        foreach (TacCharacterDef def in eggs)
                        {
                            if (availableTemplatesOrdered.Contains(def))
                            {
                                availableEggs.Add(def);
                                // TFTVLogger.Always($"{def.name} added");
                            }
                        }

                        foreach (TacticalDeployZone tacticalDeployZone in centralZones)
                        {
                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(1, 11 + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order));

                            if (roll > 6)
                            {
                                TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                                actorDeployData.InitializeInstanceData();
                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static void NestingDeployment(TacticalLevelController controller)
                {
                    try
                    {
                        //   TacCharacterDef spawnery = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");

                        List<TacCharacterDef> sentinels = new List<TacCharacterDef>() { sentinelMist, sentinelHatching, sentinelTerror };

                        TacticalDeployZone centralZone = Map.DeploymentZones.VehicleBayCentralDeployZone;
                        centralZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                        //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                        ActorDeployData spawneryDeployData = sentinels.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp())).GenerateActorDeployData();
                        spawneryDeployData.InitializeInstanceData();
                        TacticalActorBase sentinel = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);

                        sentinel.GameTags.Add(Objectives._killMainObjectiveTag);

                        List<TacticalDeployZone> otherCentralZones = Map.DeploymentZones.VehicleBayCentralDeployZones;
                        otherCentralZones.Remove(centralZone);

                        List<TacCharacterDef> availableTemplatesOrdered =
                            new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                        List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                        foreach (TacCharacterDef def in eggs)
                        {
                            if (availableTemplatesOrdered.Contains(def))
                            {
                                availableEggs.Add(def);
                                // TFTVLogger.Always($"{def.name} added");

                            }
                        }

                        foreach (TacticalDeployZone tacticalDeployZone in otherCentralZones)
                        {
                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(1, TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order));

                            for (int x = 0; x < roll; x++)
                            {
                                TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                                actorDeployData.InitializeInstanceData();
                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static List<TacCharacterDef> GetEscapedPandoransSelection(int escapees)
                {
                    try
                    {
                        List<TacCharacterDef> escapedPandorans = new List<TacCharacterDef>();

                        List<TacCharacterDef> selection = new List<TacCharacterDef>();

                        foreach (string item in PandoransInContainment.Keys.Where(k => PandoransInContainment[k] > 0))
                        {
                            int count = PandoransInContainment[item];

                            for (int x = 0; x < count; x++)
                            {
                                TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);

                                if (!tacCharacterDef.DefaultDeploymentTags.Any(ddt => ddt.name.Contains("3x3") || ddt.name.Contains("5x5")))
                                {
                                    TFTVLogger.Always($"Added {tacCharacterDef.name} to escaped Pandorans list");
                                    escapedPandorans.Add(tacCharacterDef);
                                }
                            }
                        }

                        escapedPandorans = escapedPandorans.OrderByDescending(tc => tc.DeploymentCost).ToList();

                        TFTVLogger.Always($"list is {escapedPandorans.Count} long");

                        if (escapedPandorans.Count < escapees)
                        {
                            selection = escapedPandorans;
                        }
                        else
                        {
                            for (int x = 0; x < escapees && x < escapedPandorans.Count; x++)
                            {
                                TFTVLogger.Always($"addding {escapedPandorans[x].name} to selection");
                                selection.Add(escapedPandorans[x]);
                            }
                        }

                        foreach (TacCharacterDef tacCharacter in selection)
                        {
                            PandoransInContainment[tacCharacter.Guid] -= 1;
                            if (PandoransInContainmentThatEscaoed.ContainsKey(tacCharacter.Guid))
                            {
                                PandoransInContainmentThatEscaoed[tacCharacter.Guid] += 1;
                            }
                            else
                            {
                                PandoransInContainmentThatEscaoed.Add(tacCharacter.Guid, 1);
                            }
                        }

                        return selection;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
                internal static void SpawnEscapedPandoransInitialDeployment()
                {
                    try
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (controller.TacMission.MissionData.MissionType.MissionTypeTag != baseDefenseTag)
                        {
                            return;
                        }

                        List<TacCharacterDef> escapedPandorans = GetEscapedPandoransSelection(10);

                        List<Vector3> containmentSpawnPos = Map.ContainmentBreachSpawn;

                        TacticalFactionDef alienFactionDef = DefCache.GetDef<TacticalFactionDef>("Alien_TacticalFactionDef");

                        foreach (Vector3 vector3 in containmentSpawnPos)
                        {
                            if (escapedPandorans.Count == 0)
                            {
                                return;
                            }

                            TacCharacterDef pandoran = escapedPandorans.First();

                            void onLoadingCompleted()
                            {
                                ActorDeployData actorDeployData = pandoran.GenerateActorDeployData();

                                actorDeployData.InitializeInstanceData();

                                TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, alienFactionDef, TacMissionParticipant.Intruder, vector3, new Quaternion(), null);
                                tacticalActorBase.Source = tacticalActorBase;
                                tacticalActorBase.GameTags.Add(Defs.EscapedPandoran);
                                //controller.SituationCache.Invalidate();
                                //  controller.View.ResetCharacterSelectedState();            
                            }
                            controller.AssetsLoader.StartLoadingRoots(pandoran.AsEnumerable(), null, onLoadingCompleted);
                            escapedPandorans.Remove(pandoran);
                        }

                        if (escapedPandorans.Count > 0)
                        {
                            List<TacticalDeployZone> allPandoranTdz = Map.DeploymentZones.GetEnemyDeployZones(controller).Where(t => !containmentSpawnPos.Contains(t.Pos)).ToList();

                            foreach (TacCharacterDef pandoran in escapedPandorans)
                            {
                                TacticalDeployZone tdz = allPandoranTdz.GetRandomElement();

                                void onLoadingCompleted()
                                {
                                    ActorDeployData actorDeployData = pandoran.GenerateActorDeployData();
                                    actorDeployData.InitializeInstanceData();

                                    TacticalActorBase tacticalActorBase = tdz.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tdz);
                                    tacticalActorBase.Source = tacticalActorBase;
                                    tacticalActorBase.GameTags.Add(Defs.EscapedPandoran);
                                  //  controller.SituationCache.Invalidate();
                                    //  controller.View.ResetCharacterSelectedState();            
                                }
                                controller.AssetsLoader.StartLoadingRoots(pandoran.AsEnumerable(), null, onLoadingCompleted);
                                TFTVLogger.Always($"deploying {pandoran?.name}");

                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static TacCharacterDef FindScylla()
                {
                    try
                    {
                        foreach (string item in PandoransInContainment.Keys)
                        {
                            TacCharacterDef tacCharacterDef = (TacCharacterDef)Repo.GetDef(item);

                            if (tacCharacterDef.ClassTag.className == "Queen")
                            {
                                return tacCharacterDef;
                            }
                        }

                        return null;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                internal static void ScyllaLooseDeployment(TacticalLevelController controller)
                {
                    try
                    {
                        TacCharacterDef scyllaDef = FindScylla();

                        List<TacCharacterDef> availableTemplatesOrdered =
                           new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                        List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                        foreach (TacCharacterDef def in eggs)
                        {
                            if (availableTemplatesOrdered.Contains(def))
                            {
                                availableEggs.Add(def);
                                // TFTVLogger.Always($"{def.name} added");
                            }
                        }

                        TacticalActorBase chosenDummy = new TacticalActorBase();

                        foreach (TacticalDeployZone tacticalDeployZone in Map.DeploymentZones.GetEnemyDeployZones(controller))
                        {
                            //tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(1, 11 + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order));

                            if (roll > 6)
                            {
                                TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                                actorDeployData.InitializeInstanceData();
                                TacticalActorBase egg = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                                if (chosenDummy == null)
                                {
                                    chosenDummy = egg;
                                    egg.GameTags.Add(Objectives._killMainObjectiveTag);
                                }
                            }
                        }

                        TacticalDeployZone centralZone = Map.DeploymentZones.VehicleBayCentralDeployZone;
                        centralZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                        void onLoadingCompleted()
                        {
                            ActorDeployData scyllaDeployData = scyllaDef.GenerateActorDeployData();
                            scyllaDeployData.InitializeInstanceData();

                            TacticalActorBase scylla = centralZone.SpawnActor(scyllaDeployData.ComponentSetDef, scyllaDeployData.InstanceData, scyllaDeployData.DeploymentTags, centralZone.transform, true, centralZone);
                            scylla.Source = scylla;
                            scylla.GameTags.Add(Objectives._killMainObjectiveTag);
                            scylla.GameTags.Add(Defs.EscapedPandoran);
                            controller.SituationCache.Invalidate();
                            chosenDummy.GameTags.Remove(Objectives._killMainObjectiveTag);
                            //  controller.View.ResetCharacterSelectedState();            
                        }
                        controller.AssetsLoader.StartLoadingRoots(scyllaDef.AsEnumerable(), null, onLoadingCompleted);

                        // SpawnAdditionalEggs(controller);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                internal static void InfestationDeployment(TacticalLevelController controller)
                {
                    try
                    {
                        List<TacCharacterDef> sentinels = new List<TacCharacterDef>() { sentinelMist, sentinelHatching, sentinelMist, sentinelHatching };

                        TacticalDeployZone centralZone = Map.DeploymentZones.VehicleBayCentralDeployZone;

                        centralZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                        //  TFTVLogger.Always($"central zone is at position{centralZone.Pos}");

                        ActorDeployData spawneryDeployData = spawneryDef.GenerateActorDeployData();
                        spawneryDeployData.InitializeInstanceData();
                        TacticalActorBase spawnery = centralZone.SpawnActor(spawneryDeployData.ComponentSetDef, spawneryDeployData.InstanceData, spawneryDeployData.DeploymentTags, centralZone.transform, true, centralZone);
                        spawnery.GameTags.Add(Objectives._killMainObjectiveTag);

                        List<TacticalDeployZone> otherCentralZones = Map.DeploymentZones.VehicleBayCentralDeployZones;

                        foreach (TacticalDeployZone tacticalDeploy in otherCentralZones)
                        {
                            tacticalDeploy.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                        }

                        for (int i = 0; i < Mathf.Min(TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order), 4); i++)
                        {

                            ActorDeployData actorDeployData = sentinels[i].GenerateActorDeployData();
                            actorDeployData.InitializeInstanceData();
                            TacticalActorBase sentinel = otherCentralZones[i].SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, otherCentralZones[i].transform, true, otherCentralZones[i]);
                            sentinel.GameTags.Add(Objectives._killMainObjectiveTag);
                        }

                        List<TacCharacterDef> availableTemplatesOrdered =
                            new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.OrderByDescending(tcd => tcd.DeploymentCost));

                        List<TacCharacterDef> availableEggs = new List<TacCharacterDef>() { };

                        foreach (TacCharacterDef def in eggs)
                        {
                            if (availableTemplatesOrdered.Contains(def))
                            {
                                availableEggs.Add(def);
                                // TFTVLogger.Always($"{def.name} added");
                            }
                        }

                        foreach (TacticalDeployZone tacticalDeployZone in Map.DeploymentZones.GetEnemyDeployZones(controller))
                        {
                            UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                            int roll = UnityEngine.Random.Range(1, 11 + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order));

                            if (roll > 6)
                            {
                                TacCharacterDef chosenEnemy = availableEggs.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                                ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                                actorDeployData.InitializeInstanceData();
                                tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);
                            }
                        }

                        SpawnAdditionalEggs(controller);

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void SetContainmentDynamicBreachSpawns(TacticalLevelController controller, Vector3 position)
                {
                    try
                    {
                        TFTVLogger.Always($"Setting up Containment Breach spawns", false);

                        List<TacticalDeployZone> candidates = controller.Map.GetActors<TacticalDeployZone>().Where
                            (tdz =>
                            tdz.Pos.y > 4
                            && tdz.name.Contains("Deploy_Player_1x1_Elite_Grunt_Drone")
                            ).ToList();


                        TacticalDeployZone deployZone = candidates.FirstOrDefault();
                        deployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);

                        deployZone.SetPosition(position);
                        BoxCollider boxCollider = deployZone.GetComponent<BoxCollider>();

                        boxCollider.center = Vector3.zero; // Reset the center
                        boxCollider.size = Vector3.one; // Reset the size (if necessary)

                        // Calculate the new center position based on the GameObject's position
                        Vector3 newColliderCenter = deployZone.transform.InverseTransformPoint(position);
                        boxCollider.center = newColliderCenter;

                        TFTVLogger.Always($"containment breach: {deployZone.name} {deployZone.Pos}");// bounds center: {boxCollider.center}");



                        List<TacCharacterDef> escapedPandorans = GetEscapedPandoransSelection(2);

                        foreach (TacCharacterDef pandoran in escapedPandorans)
                        {
                            void onLoadingCompleted()
                            {
                                ActorDeployData actorDeployData = pandoran.GenerateActorDeployData();
                                actorDeployData.InitializeInstanceData();

                                TacticalActorBase tacticalActorBase = deployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, deployZone);
                                tacticalActorBase.Source = tacticalActorBase;

                                controller.SituationCache.Invalidate();
                                //  controller.View.ResetCharacterSelectedState();            
                            }
                            controller.AssetsLoader.StartLoadingRoots(pandoran.AsEnumerable(), null, onLoadingCompleted);
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

            }
            internal class PlayerDeployment
            {
                private static List<int> _listEntrance = TFTVBaseDefenseGeoscape.Deployment.listEntrance;
                private static List<int> _listHangar = TFTVBaseDefenseGeoscape.Deployment.listHangar;
                private static List<int> _listLift = TFTVBaseDefenseGeoscape.Deployment.listLift;

                private static bool CheckTdzTeam(TacticalDeployZone zone, int geoId)
                {
                    try
                    {
                        /* if(_listEntrance.Contains(geoId) && _listHangar.Contains(geoId) && _listLift.Contains(geoId) && zone.Pos!=Map.AccessLiftDeployPos) 
                         {
                             return true;
                         }*/


                        if (geoId > 0)
                        {
                            if (_listEntrance.Contains(geoId) && ((zone.Pos - Map.EntrancePhaseIPlayerSpawn).magnitude < 10 || (zone.Pos - Map.EntranceExitCentralPos).magnitude < 20))
                            {
                                // TFTVLogger.Always($"{geoId} can deploy at {zone.name} {zone.Pos}");

                                return true;

                            }
                            else if (_listHangar.Contains(geoId) && (zone == Map.DeploymentZones.VehicleBayCentralDeployZone
                                || Map.DeploymentZones.VehicleBayCentralDeployZones.Contains(zone)))

                            {
                                //  TFTVLogger.Always($"{geoId} can deploy in Hangar");

                                return true;

                            }
                            else if (_listLift.Contains(geoId) && (zone.Pos.y > 4 && (zone.Pos - Map.AccessLiftDeployPos).magnitude < 4))
                            {
                                // TFTVLogger.Always($"{geoId} can deploy in AccessLift");

                                return true;

                            }

                        }

                        return false;

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }

                public static IEnumerable<TacticalDeployZone> CullPlayerDeployZonesBaseDefense(IEnumerable<TacticalDeployZone> results, ActorDeployData deployData, int turnNumber, TacMissionTypeDef missionTypeDef, TacticalLevelController controller)
                {
                    try
                    {
                        if (turnNumber != 0 || !missionTypeDef.Tags.Contains(baseDefenseTag) || !controller.Factions.Any(f => f.TacticalFactionDef.MatchesShortName("aln")))
                        {
                            return results;

                        }

                        List<TacticalDeployZone> culledList = new List<TacticalDeployZone>();

                        TacActorBaseInstanceData tacActorBaseInstanceData = (TacActorBaseInstanceData)deployData.InstanceData;

                        if (tacActorBaseInstanceData != null && tacActorBaseInstanceData.GeoUnitId != 0)
                        {
                            int actorId = tacActorBaseInstanceData.GeoUnitId;

                            foreach (TacticalDeployZone zone in results)
                            {
                                if (CheckTdzTeam(zone, actorId))
                                {
                                    culledList.Add(zone);
                                }
                            }

                            return culledList;
                        }
                        else
                        {
                            return results;
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }


                public static void SetUpDeployZones(TacticalLevelController controller)
                {
                    try
                    {
                        if (_listEntrance.Count > 0 && TimeLeft >= 12)
                        {
                            SetPlayerSpawnEntrancePhaseI(controller);
                        }
                        else if (_listEntrance.Count > 0 && TimeLeft < 12)
                        {
                            SetPlayerSpawnEntrancePhaseIIandPhaseIII(controller);
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }

                public static void SetPlayerSpawnEntrancePhaseI(TacticalLevelController controller)
                {
                    try
                    {
                        if (_listEntrance.Count == 0)
                        {
                            TFTVLogger.Always($"Nobody to deploy at the entrance");
                            return;
                        }

                        int requiredTdz = _listEntrance.Count / 3 + 1;

                        if (_listEntrance.Count % 3 > 0)
                        {
                            requiredTdz++;
                        }

                        if (requiredTdz > 4)
                        {
                            requiredTdz = 4;
                        }

                        TFTVLogger.Always($"Because entrance list has {_listEntrance.Count}, requiredTdz {requiredTdz}");

                        TFTVLogger.Always($"Setting up EntrancePhaseI", false);

                        TacticalDeployZone deployZoneVehicle = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault
                            (tdz =>
                            tdz.name.Contains("Deploy_Player_3x3_Vehicle")
                            && (tdz.Pos - Map.DeploymentZones.VehicleBayCentralDeployZone.Pos).magnitude > 5
                            && !Map.DeploymentZones.VehicleBayCentralDeployZones.Contains(tdz)
                            );

                        TacticalDeployZone deployZoneGrunt0 = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault
                        (tdz =>
                        tdz.name.Contains("Deploy_Player_1x1_Elite_Grunt_Drone")
                        && tdz.Pos.y > 4
                        && (tdz.Pos - Map.AccessLiftDeployPos).magnitude > 5
                        );

                        TacticalDeployZone deployZoneGrunt1 = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault
                      (tdz =>
                      tdz.name.Contains("Deploy_Player_3x3_Vehicle")
                      && tdz != deployZoneVehicle
                      && (tdz.Pos - Map.DeploymentZones.VehicleBayCentralDeployZone.Pos).magnitude > 5
                      && !Map.DeploymentZones.VehicleBayCentralDeployZones.Contains(tdz)
                      );

                        TacticalDeployZone deployZoneGrunt2 = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault
                      (tdz =>
                      tdz.name.Contains("Deploy_Player_3x3_Vehicle")
                      && tdz != deployZoneVehicle
                      && tdz != deployZoneGrunt1
                      && (tdz.Pos - Map.DeploymentZones.VehicleBayCentralDeployZone.Pos).magnitude > 5
                      && !Map.DeploymentZones.VehicleBayCentralDeployZones.Contains(tdz)
                      );

                        List<TacticalDeployZone> tacticalDeployZones = new List<TacticalDeployZone>() { deployZoneVehicle, deployZoneGrunt0, deployZoneGrunt1, deployZoneGrunt2, };

                        for (int x = 0; x < requiredTdz; x++)
                        {
                            int xVariation = 0;
                            float yVariation = 0;
                            int zVariation = 0;

                            BoxCollider boxCollider = tacticalDeployZones[x].GetComponent<BoxCollider>();

                            if (x == 0)
                            {
                                
                            }
                            else if (x == 1)
                            {
                                xVariation = 4;
                                zVariation = -2;
                                yVariation = 1.8f;
                            }
                            else if (x == 2)
                            {
                                tacticalDeployZones[x].MissionDeployment[0].ActorTagDef = deployZoneGrunt0.MissionDeployment[1].ActorTagDef;
                                //tacticalDeployZones[x].MissionDeployment[0].ExcludeActor = false;                                
                                xVariation = -2;
                                zVariation = -2;
                                yVariation = 1.8f;
                            }

                            else if (x == 3)
                            {
                                tacticalDeployZones[x].MissionDeployment[0].ActorTagDef = deployZoneGrunt0.MissionDeployment[1].ActorTagDef;
                                //tacticalDeployZones[x].MissionDeployment[0].ExcludeActor = false;
                                xVariation = 2;
                                zVariation = -2;
                                yVariation = 1.8f;
                            }

                            Vector3 oldPosition = tacticalDeployZones[0].transform.position;
                            Vector3 newPosition = new Vector3(Map.EntrancePhaseIPlayerSpawn.x + xVariation, oldPosition.y + yVariation, Map.EntrancePhaseIPlayerSpawn.z + zVariation);

                            tacticalDeployZones[x].SetPosition(newPosition);

                            boxCollider.center = Vector3.zero;
                            boxCollider.size = new Vector3(1.5f,1.5f,1.5f);

                            Vector3 newColliderCenter = tacticalDeployZones[x].transform.InverseTransformPoint(newPosition);
                            boxCollider.center = newColliderCenter;

                            TFTVLogger.Always($"phase1 entrance: {tacticalDeployZones[x].name} {tacticalDeployZones[x].Pos}, box collider extents: {boxCollider.bounds.extents}"); //bounds center: {boxCollider.center}");
                                                                                                                                                                                   //   TFTVLogger.Always($"{tacticalDeployZones[x].name} isMissionZone? {tacticalDeployZones[x].IsMissionZone} {tacticalDeployZones[x].MissionDeployment[0].ActorTagDef.name}");
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }



                public static void SetPlayerSpawnEntrancePhaseIIandPhaseIII(TacticalLevelController controller)
                {
                    try
                    {
                        List<TacticalDeployZone> allDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>());

                        foreach (TacticalDeployZone tacticalDeployZone in allDeployZones)
                        {

                            if (tacticalDeployZone.name.Contains("Reinforcement"))
                            {
                                //Disabling ordinary reinforcements
                                tacticalDeployZone.MissionDeployment.Clear();

                                /* if (tacticalDeployZone.name.Contains("Reinforcement_Intruder_3x3"))
                                 {
                                     tacticalDeployZone.enabled = false;
                                 }
                                 else
                                 {
                                     BoxCollider boxCollider = tacticalDeployZone.GetComponent<BoxCollider>();

                                     Vector3 oldPosition = tacticalDeployZone.transform.position;
                                     Vector3 newPosition = new Vector3(Map.AccessLiftDeployPos.x, 4.8f, Map.AccessLiftDeployPos.z);

                                     tacticalDeployZone.SetPosition(newPosition);

                                     boxCollider.center = Vector3.zero; // Reset the center
                                     boxCollider.size = Vector3.one; // Reset the size (if necessary)

                                     // Calculate the new center position based on the GameObject's position
                                     Vector3 newColliderCenter = tacticalDeployZone.transform.InverseTransformPoint(newPosition);
                                     boxCollider.center = newColliderCenter;
                                 }*/
                            }
                            else if ((tacticalDeployZone.Pos - Map.EntranceExitCentralPos).magnitude < 20)
                            {
                                //TFTVLogger.Always($"{tacticalDeployZone.name}: {tacticalDeployZone.Pos}, Map.EntranceExitCentralPos {Map.EntranceExitCentralPos}");

                                if (tacticalDeployZone.Pos.z - Map.EntranceExitCentralPos.z < 10 && tacticalDeployZone.Pos.x >= Map.EntranceExitCentralPos.x)
                                {
                                    if (tacticalDeployZone.name.Contains("1x1"))
                                    {

                                        //float reductionFactor = 0.5f;
                                        BoxCollider boxCollider = tacticalDeployZone.GetComponent<BoxCollider>();
                                        // TFTVLogger.Always($"before change: size: {boxCollider.size}, boxCollider.transform.localScale {boxCollider.transform.localScale} max: {boxCollider.bounds.max}, min: {boxCollider.bounds.min}, center: {boxCollider.bounds.center}", false);
                                        //    Vector3 newSize = boxCollider.size * reductionFactor;
                                        //  boxCollider.size = newSize;
                                        boxCollider.transform.localScale = new Vector3(4, 4, 4);

                                        // TFTVLogger.Always($"after change size: {boxCollider.size}, boxCollider.transform.localScale {boxCollider.transform.localScale} max: {boxCollider.bounds.max}, min: {boxCollider.bounds.min}, center: {boxCollider.bounds.center}", false);
                                    }
                                    //   Vector3 newSize = boxCollider.size - new Vector3(4, 0, 4); // Increase the size by 2 units in each direction
                                    //  boxCollider.size = newSize;

                                    tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);
                                    // TFTVLogger.Always($"{tacticalDeployZone.name} at {tacticalDeployZone.Pos} turned over to Player");
                                }
                                else
                                {
                                    tacticalDeployZone.MissionDeployment.Clear();

                                }

                            }
                            else if ((tacticalDeployZone.Pos - Map.AccessLiftDeployPos).magnitude > 4 || tacticalDeployZone.Pos.y < 4)
                            {
                                tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                                // TFTVLogger.Always($"{tacticalDeployZone.name} at {tacticalDeployZone.Pos} turned over to Pandorans");
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }


                /*  TFTVLogger.Always($"Tutorial base mission; setting up player to spawn at entrance. AllDeployZones Count: {allDeployZones.Count}");

                          List<Vector3> reinforcementSpawns = new List<Vector3>(ReinforcementSpawns);

                          foreach (TacticalDeployZone tacticalDeployZone in allDeployZones)
                          {
                              TFTVLogger.Always($"located {tacticalDeployZone.name} at {tacticalDeployZone.Pos}");

                              if (tacticalDeployZone.Pos.z == 0.5f || tacticalDeployZone.Pos.z == 1f)
                              {

                                  Vector3 vector3 = reinforcementSpawns.First();
                                  TFTVLogger.Always($"located tdz at {tacticalDeployZone.Pos}; changing it to position {vector3}");
                                  tacticalDeployZone.SetPosition(vector3);

                                  reinforcementSpawns.Remove(vector3);
                              }
                              else if (tacticalDeployZone.Pos == PlayerSpawn0 || tacticalDeployZone.Pos == PlayerSpawn1)
                              {
                                  TFTVLogger.Always($"Player spawn {tacticalDeployZone.name} at {tacticalDeployZone.Pos}");
                                  tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("px"), TacMissionParticipant.Player);
                                  //
                              }

                              else if (tacticalDeployZone.Pos.z <= 21.5)
                              {
                                  TFTVLogger.Always($"located tdz to be removed {tacticalDeployZone.Pos}");

                                  tacticalDeployZone.gameObject.SetActive(false);
                              }
                              else
                              {
                                  tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                              }
                          }
                      }
                      catch (Exception e)
                      {
                          TFTVLogger.Error(e);
                      }
                  }*/

                /*    public static void SetPlayerSpawnTunnels(TacticalLevelController controller)
                    {
                        try
                        {
                            List<TacticalDeployZone> playerDeployZones = Map.DeploymentZones.GetTunnelDeployZones(controller);
                            List<TacticalDeployZone> allPlayerDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).
                            Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player));


                            foreach (TacticalDeployZone deployZone in playerDeployZones)
                            {
                                // TFTVLogger.Always($"{deployZone.name} at {deployZone.Pos} is {deployZone.IsDisabled}");


                                List<MissionDeployConditionData> missionDeployConditionDatas = Map.DeploymentZones.GetTopsideDeployZones(controller).First().MissionDeployment;

                                deployZone.MissionDeployment.AddRange(missionDeployConditionDatas);
                            }

                            foreach (TacticalDeployZone zone in allPlayerDeployZones)
                            {
                                if (!playerDeployZones.Contains(zone))
                                {
                                    zone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                        }
                    }*/

                /*   public static void SetPlayerSpawnTopsideAndCenter(TacticalLevelController controller)
                   {
                       try
                       {
                           List<TacticalDeployZone> playerDeployZones = Map.DeploymentZones.GetTopsideDeployZones(controller);
                           playerDeployZones.AddRange(Map.DeploymentZones.GetCenterSpaceDeployZones(controller));

                           List<TacticalDeployZone> allPlayerDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>(null).
                           Where(tdz => tdz.MissionParticipant == TacMissionParticipant.Player));


                           foreach (TacticalDeployZone zone in allPlayerDeployZones)
                           {
                               if (!playerDeployZones.Contains(zone))
                               {
                                   zone.SetFaction(controller.GetFactionByCommandName("env"), TacMissionParticipant.Environment);
                               }
                           }
                       }
                       catch (Exception e)
                       {
                           TFTVLogger.Error(e);
                       }
                   }*/
            }

        }

        internal class PandoranTurn
        {
            public static void ImplementBaseDefenseVsAliensPreAITurn(TacticalFaction tacticalFaction)
            {
                try
                {
                    if (CheckIfBaseDefenseVsAliens(tacticalFaction.TacticalLevel))
                    {
                        // StratPicker(tacticalFaction.TacticalLevel);

                        //These strats get implemented before alien turn starts: triton infiltration team and secondary force

                        if (StratToBeImplemented >= 4)
                        {
                            StratImplementer(tacticalFaction.TacticalLevel);
                        }

                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void ImplementBaseDefenseVsAliensPostAISortingOut(TacticalFaction tacticalFaction)
            {
                try
                {

                    if (CheckIfBaseDefenseVsAliens(tacticalFaction.TacticalLevel) && tacticalFaction == tacticalFaction.TacticalLevel.GetFactionByCommandName("aln"))
                    {
                        StratPicker(tacticalFaction.TacticalLevel);

                        //These strats get implemented before alien turn starts: triton infiltration team and secondary force

                        if (StratToBeImplemented < 4)
                        {
                            StratImplementer(tacticalFaction.TacticalLevel);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
            public static void StratImplementer(TacticalLevelController controller)
            {
                try
                {
                    if (StratToBeImplemented != 0)
                    {
                        TFTVLogger.Always($"Strat to be implemented is {StratToBeImplemented}");

                        switch (StratToBeImplemented)
                        {
                            case 1:
                                {
                                    ReinforcementStrats.WormDropStrat(controller);
                                }
                                break;

                            case 2:
                                {
                                    ReinforcementStrats.MyrmidonAssaultStrat(controller);
                                }
                                break;

                            case 3:
                                {
                                    ReinforcementStrats.UmbraStrat(controller);
                                }
                                break;
                            case 4:
                                {
                                    ReinforcementStrats.SpawnSecondaryForce(controller);
                                }
                                break;
                            case 5:
                                {
                                    ReinforcementStrats.FishmanInfiltrationStrat(controller);
                                }
                                break;
                        }

                        StratToBeImplemented = 0;
                        TFTVLogger.Always($"Strat to be implemented now {StratToBeImplemented}, should be 0");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            //verified: working
            private static bool CheckUmbraStratEligibility(TacticalLevelController controller)
            {
                try
                {
                    //  TFTVLogger.Always($"Checking if Umbra strat is eligible");

                    List<TacticalActor> infectedPhoenixOperatives = new List<TacticalActor>();
                    List<TacticalDeployZone> deployZones = controller.Map.GetActors<TacticalDeployZone>().ToList();

                    //    TFTVLogger.Always($"deployZones is null? {deployZones==null} count? {deployZones?.Count}");

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("PX").TacticalActors)
                    {
                        if ((tacticalActor.CharacterStats.Corruption != null && tacticalActor.CharacterStats.Corruption > 0)
                                || tacticalActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist))
                        {
                            infectedPhoenixOperatives.Add(tacticalActor);
                            //  TFTVLogger.Always($"tactical actor added to list is {tacticalActor.DisplayName}");
                        }
                    }

                    if (infectedPhoenixOperatives.Count < 2)
                    {
                        TFTVLogger.Always($"Less than 2 infected operatives, can't use Umbra strat");
                        return false;
                    }

                    TacticalVoxelMatrix tacticalVoxelMatrix = controller.VoxelMatrix;

                    int eligibleOperativesCount = 0;
                    bool pxOperativeChecked = false;

                    foreach (TacticalActor pxOperative in infectedPhoenixOperatives)
                    {
                        //  TFTVLogger.Always($"considering {pxOperative.DisplayName}");
                        foreach (TacticalDeployZone tacticalDeployZone in deployZones)
                        {
                            if ((tacticalDeployZone.Pos - pxOperative.Pos).magnitude > 20)
                            {
                                continue;
                            }

                            foreach (TacticalVoxel voxel in tacticalVoxelMatrix.GetVoxels(tacticalDeployZone.GetComponent<BoxCollider>().bounds))
                            {
                                if (voxel.GetVoxelType() == TacticalVoxelType.Mist)
                                {
                                    eligibleOperativesCount++;
                                    pxOperativeChecked = true;
                                    TFTVLogger.Always($"{pxOperative.DisplayName} at {pxOperative.Pos} is close enough to a Mist voxel; increasing count to {eligibleOperativesCount}");
                                    break;
                                }
                            }

                            if (pxOperativeChecked)
                            {
                                break;
                            }
                        }
                    }

                    if (eligibleOperativesCount > 1)
                    {
                        return true;
                    }

                    TFTVLogger.Always($"No infected operatives close enough to deploy zone in Mist, can't use Umbra strat");

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static void PickStrat(TacticalLevelController controller, FactionObjective objective)
            {
                try
                {
                    List<int> availableStrats = new List<int>();

                    for (int x = 0; x < UsedStrats.Count(); x++)
                    {
                        TFTVLogger.Always($"strat {x + 1} recorded as used? {UsedStrats[x] == true}");
                        if (x + 1 == 3 && !CheckUmbraStratEligibility(controller))
                        {
                            continue;
                        }

                        if (UsedStrats[x] == false)
                        {
                            availableStrats.Add(x + 1);
                        }
                    }

                    TFTVLogger.Always($"available strat count: {availableStrats.Count}");

                    if (availableStrats.Count > 0)
                    {
                        StratToBeAnnounced = availableStrats.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                        UsedStrats[StratToBeAnnounced - 1] = true;
                        TFTVLogger.Always($"the objective is {objective.GetDescription()} and the strat picked is {StratToBeAnnounced} and it can't be used again");
                    }
                    else //this will give one turn pause, and then reset all strats, making them available again
                    {
                        UsedStrats = new bool[5];
                        Map.DeploymentZones.SecondaryStrikeForceSpawn = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }



            public static void StratPicker(TacticalLevelController controller)
            {
                try
                {
                    if (CheckIfBaseDefenseVsAliens(controller))
                    {
                        //need to check for completion of objectives...

                        ObjectivesManager phoenixObjectives = controller.GetFactionByCommandName("Px").Objectives;
                        int difficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);

                        foreach (FactionObjective objective in phoenixObjectives)
                        {
                            TFTVLogger.Always($"Checking objectives {objective.Description.LocalizationKey} {objective.GetCompletion()} " +
                                $"at turn number {controller.TurnNumber}");

                            if (objective.Description.LocalizationKey == "BASEDEFENSE_SURVIVE5_OBJECTIVE" && objective.GetCompletion() == 0)
                            {
                                TFTVLogger.Always($"the objective is {objective.GetDescription()}; completion is at {objective.GetCompletion()}");
                                if (controller.TurnNumber >= 5 - difficulty && controller.TurnNumber < 5)
                                {
                                    PickStrat(controller, objective);
                                }
                            }
                            else if ((objective.Description.LocalizationKey == "BASEDEFENSE_INFESTATION_OBJECTIVE"
                                || objective.Description.LocalizationKey == "BASEDEFENSE_SENTINEL_OBJECTIVE") && objective.GetCompletion() == 0)
                            {
                                TFTVLogger.Always($"the objective is {objective.GetDescription()}; completion is at {objective.GetCompletion()}");
                                UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());

                                int roll = UnityEngine.Random.Range(1, 11 + TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order));

                                if (roll >= 7)
                                {
                                    PickStrat(controller, objective);
                                }
                                else
                                {
                                    TFTVLogger.Always($"roll was {roll}, which is below 7! phew!");
                                    if (TimeLeft < 6)
                                    {
                                        StartingDeployment.PandoranDeployment.SpawnAdditionalEggs(controller);
                                        TFTVLogger.Always("But some more eggs should have spawned instead!");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        internal class PlayerTurn
        {
            public static void StratAnnouncer(TacticalLevelController controller)
            {
                try
                {
                    if (StratToBeAnnounced != 0)
                    {
                        TFTVLogger.Always($"strat for next turn is {StratToBeAnnounced}, so expecting a hint");

                        if (StratToBeAnnounced == 4)
                        {
                            TacticalDeployZone tacticalDeployZone = Map.DeploymentZones.FindSecondaryStrikeDeployZone(controller);

                            MethodInfo createVisuals = AccessTools.Method(typeof(TacticalDeployZone), "CreateVisuals");
                            createVisuals.Invoke(tacticalDeployZone, null);
                            // TFTVLogger.Always($"announcing secondary strike force! about to do a camera chase!");

                            tacticalDeployZone.CameraDirector.Hint(CameraHint.ChaseTarget, new CameraChaseParams
                            {
                                ChaseVector = tacticalDeployZone.Pos,
                                ChaseTransform = null,
                                ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                                LockCameraMovement = false,
                                Instant = true,
                                ChaseOnlyOutsideFrame = false,
                                SnapToFloorHeight = true

                            });

                            // TFTVLogger.Always($"announcing secondary strike force! should do a camera chase");
                        }

                        TacContextHelpManager hintManager = GameUtl.CurrentLevel().GetComponent<TacContextHelpManager>();
                        ContextHelpHintDef contextHelpHintDef = null;
                        FieldInfo hintsPendingDisplayField = typeof(ContextHelpManager).GetField("_hintsPendingDisplay", BindingFlags.NonPublic | BindingFlags.Instance);


                        switch (StratToBeAnnounced)
                        {
                            case 1:
                                {
                                    // WormDropStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseWormsStrat");

                                }
                                break;

                            case 2:
                                {
                                    //   MyrmidonAssaultStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseWormsStrat");
                                }
                                break;

                            case 3:
                                {
                                    //  UmbraStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseUmbraStrat");

                                }
                                break;
                            case 4:
                                {
                                    //  GenerateSecondaryForce(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseForce2Strat");
                                }
                                break;
                            case 5:
                                {
                                    //  UmbraStrat(controller);
                                    contextHelpHintDef = hintManager.HintDb.Hints.FirstOrDefault((ContextHelpHintDef h) => h.name == "BaseDefenseUmbraStrat");

                                }
                                break;
                        }


                        if (!hintManager.RegisterContextHelpHint(contextHelpHintDef, isMandatory: true, null))
                        {

                            ContextHelpHint item = new ContextHelpHint(contextHelpHintDef, isMandatory: true, null);

                            // Get the current value of _hintsPendingDisplay
                            List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);

                            // Add the new hint to _hintsPendingDisplay
                            hintsPendingDisplay.Add(item);

                            // Set the modified _hintsPendingDisplay value back to the hintManager instance
                            hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                        }

                        MethodInfo startLoadingHintAssetsMethod = typeof(TacContextHelpManager).GetMethod("StartLoadingHintAssets", BindingFlags.NonPublic | BindingFlags.Instance);

                        object[] args = new object[] { contextHelpHintDef }; // Replace hintDef with your desired argument value

                        // Invoke the StartLoadingHintAssets method using reflection
                        startLoadingHintAssetsMethod.Invoke(hintManager, args);

                        controller.View.TryShowContextHint();
                        StratToBeImplemented = StratToBeAnnounced;
                        StratToBeAnnounced = 0;

                        if (!VentingHintShown)
                        {
                            ContextHelpHintDef ventingHint = DefCache.GetDef<ContextHelpHintDef>("BaseDefenseVenting");

                            if (!hintManager.RegisterContextHelpHint(ventingHint, isMandatory: true, null))
                            {

                                ContextHelpHint item = new ContextHelpHint(ventingHint, isMandatory: true, null);

                                // Get the current value of _hintsPendingDisplay
                                List<ContextHelpHint> hintsPendingDisplay = (List<ContextHelpHint>)hintsPendingDisplayField.GetValue(hintManager);

                                // Add the new hint to _hintsPendingDisplay
                                hintsPendingDisplay.Add(item);

                                // Set the modified _hintsPendingDisplay value back to the hintManager instance
                                hintsPendingDisplayField.SetValue(hintManager, hintsPendingDisplay);
                            }

                            args = new object[] { ventingHint }; // Replace hintDef with your desired argument value

                            // Invoke the StartLoadingHintAssets method using reflection
                            startLoadingHintAssetsMethod.Invoke(hintManager, args);

                            controller.View.TryShowContextHint();
                            VentingHintShown = true;
                            Map.Consoles.SpawnConsoles.InteractionPointPlacement();
                        }

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            //Method that does what needs to be done at start of Phoenix turn when defending vs Aliens
            public static void PhoenixBaseDefenseVSAliensTurnStart(TacticalLevelController controller, TacticalFaction tacticalFaction)
            {
                try
                {
                    if (!controller.IsLoadingSavedGame)
                    {
                        if (CheckIfBaseDefenseVsAliens(controller))
                        {
                            TacticalFaction phoenix = controller.GetFactionByCommandName("px");
                            Objectives.OjectivesDebbuger(controller);

                            if (tacticalFaction == phoenix && StratToBeAnnounced != 0)
                            {
                                StratAnnouncer(controller);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        internal class ReinforcementStrats
        {
            internal static bool CheckAttackVectorForUmbra(TacticalActor tacticalActor, TacticalDeployZone tacticalDeployZone)
            {
                try
                {

                    if ((tacticalDeployZone.Pos - tacticalActor.Pos).magnitude > 20)
                    {
                        return false;
                    }

                    TacticalVoxelMatrix tacticalVoxelMatrix = tacticalActor.TacticalLevel.VoxelMatrix;


                    foreach (TacticalVoxel voxel in tacticalVoxelMatrix.GetVoxels(tacticalDeployZone.GetComponent<BoxCollider>().bounds))
                    {
                        if (voxel.GetVoxelType() == TacticalVoxelType.Mist)
                        {
                            TFTVLogger.Always($"{tacticalActor.DisplayName} at {tacticalActor.Pos} is close enough to a Mist voxel; Umbra strat is eligible");
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static void UmbraStrat(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Umbra strat deploying");



                    TacCharacterDef crabUmbra = DefCache.GetDef<TacCharacterDef>("Oilcrab_TacCharacterDef");
                    TacCharacterDef fishUmbra = DefCache.GetDef<TacCharacterDef>("Oilfish_TacCharacterDef");

                    List<TacCharacterDef> enemies = new List<TacCharacterDef>() { crabUmbra, fishUmbra };
                    List<TacticalDeployZone> allDeployZones = new List<TacticalDeployZone>(controller.Map.GetActors<TacticalDeployZone>());
                    List<TacticalActor> infectedPhoenixOperatives = new List<TacticalActor>();
                    Dictionary<TacticalActor, TacticalDeployZone> targetablePhoenixOperatives = new Dictionary<TacticalActor, TacticalDeployZone>();

                    foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("PX").TacticalActors)
                    {
                        if ((tacticalActor.CharacterStats.Corruption != null && tacticalActor.CharacterStats.Corruption > 0)
                                || tacticalActor.TacticalPerceptionBase.IsTouchingVoxel(TacticalVoxelType.Mist))
                        {
                            infectedPhoenixOperatives.Add(tacticalActor);
                            // TFTVLogger.Always($"tactical actor added to list is {tacticalActor.DisplayName}");
                        }
                    }

                    if (infectedPhoenixOperatives.Count == 0)
                    {
                        TFTVLogger.Always("No infected operatives! Can't delploy Umbra strat!");
                        return;
                    }

                    foreach (TacticalActor tacticalActor in infectedPhoenixOperatives)
                    {
                        foreach (TacticalDeployZone tacticalDeployZone in allDeployZones)
                        {
                            if (CheckAttackVectorForUmbra(tacticalActor, tacticalDeployZone))
                            {
                                if (!targetablePhoenixOperatives.ContainsKey(tacticalActor))
                                {
                                    targetablePhoenixOperatives.Add(tacticalActor, tacticalDeployZone);
                                }
                                else
                                {
                                    if ((tacticalActor.Pos - targetablePhoenixOperatives[tacticalActor].Pos).magnitude > (tacticalActor.Pos - tacticalDeployZone.Pos).magnitude)
                                    {
                                        targetablePhoenixOperatives[tacticalActor] = tacticalDeployZone;
                                    }
                                }
                            }
                        }
                    }

                    int maxUmbra = Math.Max(TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order), 3);


                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();

                    for (int x = 0; x < targetablePhoenixOperatives.Keys.Count(); x++)
                    {

                        if (x == maxUmbra)
                        {
                            break;
                        }

                        TacticalActor pXOperative = targetablePhoenixOperatives.Keys.ElementAt(x);

                        TacticalDeployZone zone = targetablePhoenixOperatives[pXOperative]; //get the zone to spawn

                        //Choose type of Umbra and generate necessary data
                        TacCharacterDef chosenEnemy = enemies.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));
                        ActorDeployData actorDeployData = chosenEnemy.GenerateActorDeployData();
                        actorDeployData.InitializeInstanceData();



                        List<Vector3> orderedSpawnPositions = zone.GetOrderedSpawnPositions();

                        List<Vector3> mistCoveredSpawnPositions = new List<Vector3>();

                        foreach (Vector3 vector3 in orderedSpawnPositions)
                        {
                            if (tacticalVoxelMatrix.GetVoxel(vector3).GetVoxelType() == TacticalVoxelType.Mist)
                            {
                                mistCoveredSpawnPositions.Add(vector3);
                            }
                        }

                        IReadOnlyCollection<Vector3> validSpawnPosition = zone.GetValidSpawnPosition(actorDeployData.ComponentSetDef, actorDeployData.DeploymentTags, mistCoveredSpawnPositions);

                        if (validSpawnPosition.Count == 0)
                        {
                            TFTVLogger.Always($"No valid positions to spawn Umbra near {pXOperative.DisplayName}!");
                            continue;
                        }

                        /*    Vector3 position = zone.Pos;
                            //  TFTVLogger.Always($"position before adjustmment is {position}");
                            if (position.y <= 2 && position.y != 1.0)
                            {
                                //  TFTVLogger.Always($"position should be adjusted to 1.2");
                                position.y = 1.0f;
                            }
                            else if (position.y > 4 && position.y != 4.8)
                            {
                                //    TFTVLogger.Always($"position should be adjusted to 4.8");
                                position.SetY(4.8f);
                            }

                            MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");
                            //spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Empty, zone.Pos + Vector3.up * -1.5f, 3, 1, false, true });
                            // TFTVLogger.Always($"pXOperative to be ghosted {pXOperative.DisplayName} at pos {position}");
                            spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, position, 3, 1, false, true });

                            // SpawnBlob_Internal(TacticalVoxelType type, Vector3 pos, int horizontalRadius, int height, bool circular, bool updateMatrix = true)*/

                        zone.SetFaction(controller.GetFactionByCommandName("aln"), TacMissionParticipant.Intruder);
                        //  TFTVLogger.Always($"Found deployzone and deploying " + chosenEnemy.name + $"; Position is y={zone.Pos.y} x={zone.Pos.x} z={zone.Pos.z}");

                        TacticalActorBase tacticalActorBase = TacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, controller.GetFactionByCommandName("aln").TacticalFactionDef, TacMissionParticipant.Intruder, validSpawnPosition.First(), zone.transform.rotation, zone);

                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                        tacticalActor?.TacticalActorView.DoCameraChase();

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void MyrmidonAssaultStrat(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Myrmidon Assault Strat deploying");

                    ClassTagDef myrmidonTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.FindHangarTopsideDeployZones(controller);

                    if (tacticalDeployZones.Count < 4)
                    {
                        for (int x = 0; x < 4 - tacticalDeployZones.Count; x++)
                        {
                            if (Map.DeploymentZones.VehicleBayCentralDeployZones[x] == null)
                            {
                                break;
                            }

                            tacticalDeployZones.Add(Map.DeploymentZones.VehicleBayCentralDeployZones[x]);
                        }
                    }

                    List<TacCharacterDef> myrmidons =
                        new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(myrmidonTag)));

                    foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                    {
                        int rollCap = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order) - 1;

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int myrmidonsToDeploy = UnityEngine.Random.Range(1, rollCap);

                        for (int x = 0; x < myrmidonsToDeploy; x++)
                        {
                            TacCharacterDef chosenMyrmidon = myrmidons.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found topside deployzone position and deploying " + chosenMyrmidon.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = chosenMyrmidon.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (x == 0 && tacticalActor != null)
                            {
                                tacticalActor.TacticalActorView.DoCameraChase();
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static void WormDropStrat(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("WormDropStrat deploying");

                    ClassTagDef wormTag = DefCache.GetDef<ClassTagDef>("Worm_ClassTagDef");
                    List<TacticalDeployZone> tacticalDeployZones = Map.DeploymentZones.FindHangarTopsideDeployZones(controller);
                    tacticalDeployZones.AddRange(Map.DeploymentZones.VehicleBayCentralDeployZones);

                    List<TacCharacterDef> worms =
                        new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(ua => ua.ClassTags.Contains(wormTag)));

                    foreach (TacticalDeployZone tacticalDeployZone in tacticalDeployZones)
                    {
                        int rollCap = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order) - 1;

                        UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                        int wormsToDeploy = UnityEngine.Random.Range(1, rollCap);
                        TacCharacterDef chosenWormType = worms.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                        for (int x = 0; x < wormsToDeploy; x++)
                        {
                            tacticalDeployZone.SetFaction(controller.GetFactionByCommandName("AlN"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = chosenWormType.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();

                            TacticalActorBase tacticalActorBase = tacticalDeployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, tacticalDeployZone);

                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                            if (x == 0 && tacticalActor != null)
                            {
                                tacticalActor.TacticalActorView.DoCameraChase();
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void ApplyReinforcementStatus(TacticalActor tacticalActor, TacCharacterDef tacCharacterDef)
            {
                try
                {
                    if (tacticalActor == null)
                    {
                        TFTVLogger.Always($"couldn't spawn {tacCharacterDef.name} for some reason!");

                        return;

                    }


                    if (tacticalActor.HasGameTag(crabTag) || tacticalActor.HasGameTag(fishmanTag))
                    {
                        if (TFTVArtOfCrab.Has1APWeapon(tacCharacterDef))
                        {
                            tacticalActor.Status.ApplyStatus(reinforcementStatus1AP);
                        }
                        else
                        {
                            tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                            TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");
                        }
                    }
                    else if (tacticalActor.HasGameTag(sirenTag))
                    {
                        tacticalActor.Status.ApplyStatus(reinforcementStatusUnder1AP);
                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder1AP.EffectName}");
                    }
                    else
                    {
                        tacticalActor.Status.ApplyStatus(reinforcementStatusUnder2AP);
                        TFTVLogger.Always($"{tacticalActor.name} receiving {reinforcementStatusUnder2AP.EffectName}");
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            private static void CreateBreachEntrance(BreachEntrance breachEntrance)
            {
                try
                {

                    TFTVLogger.Always($"{breachEntrance.name} at {breachEntrance.Pos}");
                    FieldInfo fieldInfoBounds = typeof(BreachEntrance).GetField("_navBounds", BindingFlags.NonPublic | BindingFlags.Instance);

                    fieldInfoBounds.SetValue(breachEntrance, default(Bounds));
                    if (EntranceUtils.GetEntranceObject(breachEntrance.transform, "TUNNEL", out Transform shutState) && EntranceUtils.GetEntranceObject(breachEntrance.transform, "WALL", out Transform shutState2))
                    {

                        shutState.gameObject.SetActive(value: true);
                        shutState2.gameObject.SetActive(value: true);
                        if (EntranceUtils.GetBounds(shutState2, out Bounds result))
                        {

                            shutState.gameObject.SetActive(value: false);
                            shutState2.gameObject.SetActive(value: false);
                            fieldInfoBounds.SetValue(breachEntrance, result);
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }

            }


            internal static void SpawnSecondaryForce(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Spawning Secondary Force");

                    TacticalDeployZone deployZone = Map.DeploymentZones.FindSecondaryStrikeDeployZone(controller);

                    Dictionary<TacCharacterDef, int> secondaryForce = GenerateSecondaryForce(controller);

                    deployZone.CameraDirector.Hint(CameraHint.ChaseTarget, new CameraChaseParams
                    {
                        ChaseVector = new Vector3(deployZone.Pos.x, 0.0f, deployZone.Pos.z),
                        ChaseTransform = null,
                        ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LockCameraMovement = false,
                        Instant = true,
                        ChaseOnlyOutsideFrame = false,
                        SnapToFloorHeight = true

                    });

                    Map.Consoles.Explosions.GenerateFakeExplosion(deployZone.Pos);
                    CreateBreachEntrance(Map.DeploymentZones.FindBreachEntrance(controller, deployZone.Pos));


                    TFTVLogger.Always($"Explosion preceding secondary force deployment at {deployZone.Pos}");

                    deployZone.SetFaction(controller.GetFactionByCommandName("ALN"), TacMissionParticipant.Intruder);
                    TFTVLogger.Always($"Changed deployzone to Alien and Intruder");

                    foreach (TacCharacterDef tacCharacterDef in secondaryForce.Keys)
                    {
                        for (int i = 0; i < secondaryForce[tacCharacterDef]; i++)
                        {

                            if (tacCharacterDef != Defs.ChironDigger)
                            {

                                TFTVLogger.Always($"going to generate actorDeployedData from {tacCharacterDef.name}");
                                ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                                actorDeployData.InitializeInstanceData();

                                TacticalActorBase tacticalActorBase = deployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, deployZone);
                                tacticalActorBase.Source = tacticalActorBase;

                                controller.SituationCache.Invalidate();

                                TFTVLogger.Always($"tacticalActorBase is null? {tacticalActorBase == null}");

                                if (tacticalActorBase != null)
                                {
                                    TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                                    ApplyReinforcementStatus(tacticalActor, tacCharacterDef);
                                }
                            }
                            else
                            {
                                void onLoadingCompleted()
                                {
                                    TFTVLogger.Always($"going to generate actorDeployedData from {tacCharacterDef.name}");
                                    ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                                    actorDeployData.InitializeInstanceData();

                                    TacticalActorBase tacticalActorBase = deployZone.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, deployZone);
                                    tacticalActorBase.Source = tacticalActorBase;

                                    controller.SituationCache.Invalidate();

                                    TFTVLogger.Always($"tacticalActorBase is null? {tacticalActorBase == null}");

                                    if (tacticalActorBase != null)
                                    {
                                        TacticalActor tacticalActor = tacticalActorBase as TacticalActor;

                                        ApplyReinforcementStatus(tacticalActor, tacCharacterDef);
                                    }
                                }
                                controller.AssetsLoader.StartLoadingRoots(Defs.SecurityGuard.AsEnumerable(), null, onLoadingCompleted);
                            }
                            TFTVLogger.Always($"{tacCharacterDef.name} spawned");
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }


            internal static void FishmanInfiltrationStrat(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Spawning Triton Infiltration team");
                  
                    TacticalDeployZone zoneToDeployAt = controller.Map.GetActors<TacticalDeployZone>().FirstOrDefault(tdz => tdz.Pos.y > 4 && (tdz.Pos - Map.AccessLiftDeployPos).magnitude < 5);

                    if (TimeLeft < 12 && UnityEngine.Random.Range(0, 2) > 0)
                    {
                        List<TacticalDeployZone> tacticalDeploys = controller.Map.GetActors<TacticalDeployZone>().Where(tdz => tdz.Pos.z < 5).ToList();
                        zoneToDeployAt = tacticalDeploys.GetRandomElement();
                    }

                    zoneToDeployAt.CameraDirector.Hint(CameraHint.ChaseTarget, new CameraChaseParams
                    {
                        ChaseVector = zoneToDeployAt.Pos,
                        ChaseTransform = null,
                        ChaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                        LockCameraMovement = false,
                        Instant = true,
                        ChaseOnlyOutsideFrame = false,
                        SnapToFloorHeight = true

                    });

                    Dictionary<TacCharacterDef, int> tritonInfiltratonTeam = GenerateTritonInfiltrationForce(controller);

                    Level level = controller.Level;
                    TacticalVoxelMatrix tacticalVoxelMatrix = level?.GetComponent<TacticalVoxelMatrix>();
                    Vector3 position = zoneToDeployAt.Pos;

                    //  TFTVLogger.Always($"position before adjustmment is {position}");
                    if (position.y <= 2 && position.y != 1.0)
                    {
                        //  TFTVLogger.Always($"position should be adjusted to 1.2");
                        position.y = 1.0f;
                    }
                    else if (position.y > 4 && position.y != 4.8)
                    {
                        //    TFTVLogger.Always($"position should be adjusted to 4.8");
                        position.SetY(4.8f);
                    }

                    TFTVLogger.Always($"Mist will spawn at position {position}");

                    MethodInfo spawnBlob = AccessTools.Method(typeof(TacticalVoxelMatrix), "SpawnBlob_Internal");

                    spawnBlob.Invoke(tacticalVoxelMatrix, new object[] { TacticalVoxelType.Mist, position, 3, 1, false, true });


                    foreach (TacCharacterDef tacCharacterDef in tritonInfiltratonTeam.Keys)
                    {
                        for (int i = 0; i < tritonInfiltratonTeam[tacCharacterDef]; i++)
                        {

                            zoneToDeployAt.SetFaction(controller.GetFactionByCommandName("Aln"), TacMissionParticipant.Intruder);
                            //  TFTVLogger.Always($"Found center deployzone position and deploying " + chosenWormType.name + $"; Position is y={tacticalDeployZone.Pos.y} x={tacticalDeployZone.Pos.x} z={tacticalDeployZone.Pos.z}");
                            ActorDeployData actorDeployData = tacCharacterDef.GenerateActorDeployData();

                            actorDeployData.InitializeInstanceData();
                            TacticalActorBase tacticalActorBase = zoneToDeployAt.SpawnActor(actorDeployData.ComponentSetDef, actorDeployData.InstanceData, actorDeployData.DeploymentTags, null, true, zoneToDeployAt);
                            TacticalActor tacticalActor = tacticalActorBase as TacticalActor;
                            TFTVLogger.Always($"deploying {tacticalActor.name}");
                            ApplyReinforcementStatus(tacticalActor, tacCharacterDef);
                        }
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            internal static Dictionary<TacCharacterDef, int> GenerateTritonInfiltrationForce(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Generating Triton Infiltration Team Force");

                    ClassTagDef fishmanTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");

                    List<TacCharacterDef> researchedTritons = new List<TacCharacterDef>();

                    Dictionary<TacCharacterDef, int> infiltrationTeam = new Dictionary<TacCharacterDef, int>();

                    int difficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);

                    List<TacCharacterDef> unlockedTritons = controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Where(tcd => tcd.ClassTag == fishmanTag).ToList();

                    foreach (TacCharacterDef tacCharacterDef in unlockedTritons)
                    {
                        if (!researchedTritons.Contains(tacCharacterDef))
                        {
                            researchedTritons.Add(tacCharacterDef);
                        }
                    }

                    for (int x = 0; x < Math.Max(difficulty, 2); x++)
                    {
                        TacCharacterDef tritonTypeToAdd = researchedTritons.GetRandomElement(new System.Random((int)Stopwatch.GetTimestamp()));

                        if (infiltrationTeam.ContainsKey(tritonTypeToAdd))
                        {
                            infiltrationTeam[tritonTypeToAdd] += 1;
                        }
                        else
                        {
                            infiltrationTeam.Add(tritonTypeToAdd, 1);
                        }
                    }

                    return infiltrationTeam;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static Dictionary<TacCharacterDef, int> GenerateSecondaryForce(TacticalLevelController controller)
            {
                try
                {
                    TFTVLogger.Always("Generating Secondary Force");
                    TacCharacterDef mindFragger = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                    int difficulty = TFTVSpecialDifficulties.DifficultyOrderConverter(controller.Difficulty.Order);

                    // UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    //  int coinToss = UnityEngine.Random.Range(0, 2);

                    Dictionary<ClassTagDef, int> reinforcements = new Dictionary<ClassTagDef, int>
                    {
                        { crabTag, 3 },
                        {sirenTag, 1 }
                    };

                    // TFTVLogger.Always("2");
                    Dictionary<TacCharacterDef, int> secondaryForce = new Dictionary<TacCharacterDef, int>() { { mindFragger, difficulty } };

                    /*   if (coinToss == 0)
                       {
                           reinforcements[crabTag] = 3;
                           reinforcements.Add(sirenTag, 1);
                           secondaryForce.Add(mindFragger, difficulty);
                       }
                       else
                       {
                           secondaryForce.Add(Defs.ChironDigger, 1);
                           secondaryForce.Add(mindFragger, 2);
                       }*/

                    List<TacCharacterDef> availableTemplatesOrdered = new List<TacCharacterDef>(controller.TacMission.MissionData.UnlockedAlienTacCharacterDefs.Shuffle());

                    foreach (TacCharacterDef tacCharacterDef in availableTemplatesOrdered)
                    {
                        if (tacCharacterDef.ClassTag != null && !secondaryForce.ContainsKey(tacCharacterDef)
                            && reinforcements.ContainsKey(tacCharacterDef.ClassTag) && reinforcements[tacCharacterDef.ClassTag] > 0)
                        {
                            secondaryForce.Add(tacCharacterDef, 1);
                            reinforcements[tacCharacterDef.ClassTag] -= 1;
                            //   TFTVLogger.Always("Added " + tacCharacterDef.name + " to the Seconday Force");
                        }
                    }
                    //   TFTVLogger.Always("3");


                    //    TFTVLogger.Always("4");
                    return secondaryForce;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            internal static bool CheckLOSToPlayer(TacticalLevelController contoller, Vector3 pos)
            {
                try
                {
                    bool lOS = false;

                    TacCharacterDef siren = DefCache.GetDef<TacCharacterDef>("Siren1_Basic_AlienMutationVariationDef");
                    List<TacticalActor> phoenixOperatives = new List<TacticalActor>(contoller.GetFactionByCommandName("PX").TacticalActors);

                    ActorDeployData actorDeployData = siren.GenerateActorDeployData();
                    actorDeployData.InitializeInstanceData();

                    foreach (TacticalActor actor in phoenixOperatives)
                    {
                        TacticalActorBase actorBase = actor;

                        if (TacticalFactionVision.CheckVisibleLineBetweenActorsInTheory(actorBase, actorBase.Pos, actorDeployData.ComponentSetDef, pos) && (actor.Pos - pos).magnitude < 30)
                        {
                            lOS = true;
                            return lOS;
                        }
                    }
                    return lOS;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

    }
}

