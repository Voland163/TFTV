using Base;
using Base.AI.Defs;
using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.Input;
using Base.UI;
using Base.Utils;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.ContextHelp.HintConditions;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.Entities.RedeemableCodes;
using PhoenixPoint.Common.Levels.Missions;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Actions;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.AI.TargetGenerators;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.ApplicationConditions;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TFTV.LaserWeapons;
using UnityEngine;
using static PhoenixPoint.Common.Entities.Addons.AddonDef;
using static PhoenixPoint.Tactical.Entities.Abilities.HealAbilityDef;
using static PhoenixPoint.Tactical.Entities.Statuses.ItemSlotStatsModifyStatusDef;
using static TFTV.TFTVCapturePandorans;
using ResourceType = PhoenixPoint.Common.Core.ResourceType;

namespace TFTV
{
    internal class TFTVDefsInjectedOnlyOnce
    {


        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static GameTagDef AlwaysDeployTag;



        //  private static readonly ResearchTagDef CriticalResearchTag = DefCache.GetDef<ResearchTagDef>("CriticalPath_ResearchTagDef");


        // ResurrectAbilityRulesDef to mess with later

        internal static void Experimental()
        {
            try
            {
                TFTVNJQuestline.NewNJIntroMission = true;

                // TFTVTauntsAndQuips.PopulateQuipList();
                TFTVNJQuestline.IntroMission.Defs.ModifyIntroMissionDefs();

                //  CharacterBuilderViewParametersDef defaultCharacters = DefCache.GetDef<CharacterBuilderViewParametersDef>("DefaultCharBuilderViewParametersDef");

                //  defaultCharacters.ObjectScale = new Vector3(1, 1, 1);


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        internal static void Print()
        {
            try
            {
                foreach (WeaponDef weaponDef in Repo.GetAllDefs<WeaponDef>().Where(w => w.Tags.Contains(DefCache.GetDef<GameTagDef>("GunWeapon_TagDef"))))
                {
                    TFTVLogger.Always($"WeaponDef has GunWeapon_TagDef {weaponDef.name}");
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void DisplayTimerProperties()
        {
            try
            {
                // Display the timer frequency and resolution.
                if (Stopwatch.IsHighResolution)
                {
                    TFTVLogger.Always("Operations timed using the system's high-resolution performance counter.");
                }
                else
                {
                    TFTVLogger.Always("Operations timed using the DateTime class.");
                }

                long frequency = Stopwatch.Frequency;
                TFTVLogger.Always($"Timer frequency in ticks per second = {frequency}");
                long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
                TFTVLogger.Always($"Timer is accurate within {nanosecPerTick} nanoseconds");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void InjectDefsInjectedOnlyOnceBatch1()
        {
            try
            {


                CreateAlwaysDeployTag();

                VanillaFixes();

                GeoscapeEvents();

                CreateRoboticSelfRestoreAbility();

                LoadingScreensAndLore();

                TFTVAncients.Defs.ChangesToLOTA();

                //  CreateVoidOmenRemindersInTactical();

                CreateFireQuenchers();

                TFTVBaseDefenseGeoscape.Defs.CreateNewBaseDefense();

                ScyllaAcheronsChironsAndCyclops();

                TFTVChangesToDLC4Events.Defs.ChangeOrCreateDefs();

                ChangeUmbra();

                ChangesModulesAndAcid();

                ChangePalaceMissions();

                CreateAndAdjustDefsForLimitedCapture();

                TFTVRevenant.RevenantDefs.CreateRevenantDefs();

                TFTVHints.HintDefs.CreateHints();

                Marketplace();

                ChangesToAI();

                SpecialDifficulties();

                VariousMinorAdjustments();

                TFTVScavengers.Defs.CreateRaiderDefs();

                TFTVPureAndForsaken.Defs.InitDefs();

                TFTVTacticalDeploymentEnemies.PopulateLimitsForUndesirables();

                AddAlwaysDeployTagToUniqueDeployments();

                CreateBackgrounds();

                TFTVBackgrounds.LoadTFTVBackgrounds();

                TFTVUITactical.Enemies.PopulateFactionViewElementDictionary();

                CreateHotkeys();

                DisplayTimerProperties();

                VirophageDamage();

                AddViralBodyPartTagToEliteViralArthronGun();
                //  TFTVTacticalObjectives.Defs.CreateHumanTacticsRemindersInTactical();
                ShockDamagePriority();

                TFTVHumanEnemies.Defs.CreateHumanEnemiesDefs();

                ChangeRenderedPortraitsParam();

                AircraftReworkDefs.CreateAndModifyDefs();

                TFTVMeleeDamage.AddMeleeDamageType();

                TFTVDrills.DrillsDefs.CreateDefs();

                // Experimental();

                //   Print();

                //  ChangeScyllaSounds();
                CreateSuppressionStatusDefs();
                AddMissingViewElementDefs();
              //  LaserWeaponsInit.Init();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AddMissingViewElementDefs()
        {
            try
            {
                ViewElementDef source = DefCache.GetDef<ViewElementDef>("E_View [Mutog_Tail_Basher_WeaponDef]");

                TacticalItemDef mutogAgileLegs = DefCache.GetDef<TacticalItemDef>("Mutog_Legs_Agile_ItemDef");
                TacticalItemDef mutogRegenerativeLegs = DefCache.GetDef<TacticalItemDef>("Mutog_Legs_Regenerating_ItemDef");

                mutogAgileLegs.ViewElementDef = Helper.CreateDefFromClone(source, "{8B943E30-736F-4954-9F1A-72503EEDF6FF}", mutogAgileLegs.name);
                mutogRegenerativeLegs.ViewElementDef = Helper.CreateDefFromClone(source, "{8DDE42AE-3112-4A74-BAD8-D5A20EA82204}", mutogRegenerativeLegs.name);

                List<ViewElementDef> viewElementDefs = new List<ViewElementDef>()
                {
                    mutogAgileLegs.ViewElementDef,
                    mutogRegenerativeLegs.ViewElementDef
                };

                mutogAgileLegs.ViewElementDef.DisplayName1.LocalizationKey = "MUTOG_AGILE_LEGS_NAME";
                mutogRegenerativeLegs.ViewElementDef.DisplayName1.LocalizationKey = "MUTOG_REGENERATIVE_LEGS_NAME";

                foreach(ViewElementDef viewElementDef in viewElementDefs) 
                {
                    viewElementDef.SmallIcon = null;
                    viewElementDef.LargeIcon = null;
                    viewElementDef.InventoryIcon = null;
                    viewElementDef.DeselectIcon = null;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateSuppressionStatusDefs()
        {
            try
            {
                TacStatusDef suppression0 = TFTVSuppression.SuppressionStatuses.LightSuppressionStatus;
                TacStatusDef suppression1 = TFTVSuppression.SuppressionStatuses.ModerateSuppressionStatus;
                TacStatusDef suppression2 = TFTVSuppression.SuppressionStatuses.HeavySuppressionStatus;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        /*  private static void ChangeScyllaSounds()
          {
              try 
              {
                  TacticalEventDef tacticalEventDef = DefCache.GetDef<TacticalEventDef>("Queen_MistHurt_EventDef");

                  string filePath = Path.Combine(TFTVMain.ModDirectory, "Assets", "test_audio.wav");

                  TFTVAudio.ExternalAudioInjector.RegisterClipFromFile(tacticalEventDef, filePath, 1, false, 1, null, AudioType.WAV);

              }
              catch (Exception e)
              {
                  TFTVLogger.Error(e);
              }

          }*/



        private static void AddAIActorStatsConditionsConsideration()
        {
            try
            {
                //DemolitionManMinimalWillPoints_AIConsiderationsDef

                AIActorStatsConditionsConsiderationDef demolitionStateMinimalWPAIConsideration = DefCache.GetDef<AIActorStatsConditionsConsiderationDef>("DemolitionManMinimalWillPoints_AIConsiderationsDef");

                string silenceWPConsiderationName = "TFTV_SilenceMinimalWillPoints_AIConsiderationsDef";
                string electricReinforcementWPConsiderationName = "TFTV_ElectricReinforcementMinimalWillPoints_AIConsiderationsDef";

                //need to create an AIActorStatsConditionsConsiderationDef and add it to desired AIActionDefs.
                //desired ActionDefs: electric reinforcement, MoveAndDoSilence_AIActionDef, DemolitionMan_AIActionDef

                AIActorStatsConditionsConsiderationDef newSilenceConsideration =
                    Helper.CreateDefFromClone(demolitionStateMinimalWPAIConsideration, "{B0F1A2D6-3C8B-4E5A-9F7C-1D3F5B6E7A8B}", silenceWPConsiderationName);

                AIActorStatsConditionsConsiderationDef newElectricReinforcementConsideration =
                    Helper.CreateDefFromClone(demolitionStateMinimalWPAIConsideration, "{C1D2E3F4-5A6B-7C8D-9E0F-1A2B3C4D5E6F}", electricReinforcementWPConsiderationName);

                newElectricReinforcementConsideration.Conditions[0].Quantity = 0.5f;
                newSilenceConsideration.Conditions[0].Quantity = 0.5f;

                AIActionDef moveAndDoSilence = DefCache.GetDef<AIActionDef>("MoveAndDoSilence_AIActionDef");
                AIActionExecuteAbilityDef electricReinforcement = TFTVBetterEnemies.electricReinforcement;

                AIAdjustedConsideration newElectricReinforcementAdjustedConsideration = new AIAdjustedConsideration
                {
                    Consideration = newElectricReinforcementConsideration,
                    ScoreCurve = electricReinforcement.EarlyExitConsiderations[0].ScoreCurve
                };

                electricReinforcement.EarlyExitConsiderations = electricReinforcement.EarlyExitConsiderations.AddToArray(newElectricReinforcementAdjustedConsideration);

                moveAndDoSilence.EarlyExitConsiderations = moveAndDoSilence.EarlyExitConsiderations.AddToArray(new AIAdjustedConsideration
                {
                    Consideration = newSilenceConsideration,
                    ScoreCurve = moveAndDoSilence.EarlyExitConsiderations[0].ScoreCurve
                });

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void ChangeRenderedPortraitsParam()
        {
            try
            {

                SquadPortraitsDef squadPortraitsDef = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                for (int x = 0; x < squadPortraitsDef.RenderParamsList.Count(); x++)
                {
                    squadPortraitsDef.RenderParamsList[x].CameraDistance = 0.5f;
                    squadPortraitsDef.RenderParamsList[x].CameraFoV = 35f;
                    squadPortraitsDef.RenderParamsList[x].RenderedPortraitsResolution = new Vector2Int(1024, 819);

                    if (x == 0)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0;
                    }

                    if (x == 1)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0.05f;
                    }

                    if (x == 2)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = -0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = -0.05f;
                    }

                    if (x == 3)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = -0.05f;
                    }

                    if (x == 4)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0.05f;
                    }

                    if (x == 5)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0f;
                    }

                    if (x == 6)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = -0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0f;
                    }

                    if (x == 7)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = -0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = 0.05f;
                    }

                    if (x == 8)
                    {
                        squadPortraitsDef.RenderParamsList[x].CameraHeight = 0.05f;
                        squadPortraitsDef.RenderParamsList[x].CameraSide = -0.05f;
                    }

                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void ShockDamagePriority()
        {
            try
            {
                //StunDamageKeywordData
                StunDamageKeywordDataDef shockDamageKeywordDef = DefCache.GetDef<StunDamageKeywordDataDef>("Shock_DamageKeywordDataDef");
                DamageKeywordDef damageKeywordDef = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");

                shockDamageKeywordDef.KeywordApplicationPriority = -1000;



                List<WeaponDef> weapons = new List<WeaponDef>()
                {
                DefCache.GetDef<WeaponDef>("AN_Hammer_WeaponDef"),
                DefCache.GetDef<WeaponDef>("KS_Devastator_WeaponDef"),
                DefCache.GetDef<WeaponDef>("FS_Autocannon_WeaponDef"),
                DefCache.GetDef<WeaponDef>("FS_SlamstrikeShotgun_WeaponDef"),
                DefCache.GetDef<WeaponDef>("PX_HeavyCannon_WeaponDef"),
              //  DefCache.GetDef<WeaponDef>("PX_Scarab_Taurus_GroundVehicleWeaponDef"),
                DefCache.GetDef<WeaponDef>("PX_HeavyCannon_Headhunter_WeaponDef"),
                DefCache.GetDef<WeaponDef>("Mutog_HeadRamming_BodyPartDef"),
                DefCache.GetDef<WeaponDef>("Mutog_Tail_Basher_WeaponDef")
                };

                foreach (WeaponDef weaponDef in weapons)
                {
                    float standardDamageAdjustment = weaponDef.DamagePayload.DamageKeywords.FirstOrDefault(dk => dk.DamageKeywordDef == damageKeywordDef).Value - 30;
                    weaponDef.DamagePayload.DamageKeywords.FirstOrDefault(dk => dk.DamageKeywordDef == shockDamageKeywordDef).Value += standardDamageAdjustment;
                }

                //    DefCache.GetDef<ApplyDamageEffectAbilityDef>("StomperLegs_Stomp_AbilityDef").DamagePayload.DamageKeywords.FirstOrDefault(dk => dk.DamageKeywordDef == damageKeywordDef).Value+=20;

                //  DefCache.GetDef<BashAbilityDef>("Takedown_Bash_AbilityDef").DamagePayload.DamageKeywords.FirstOrDefault(dk => dk.DamageKeywordDef == damageKeywordDef).Value += 50;

                /*  foreach (ApplyDamageEffectAbilityDef ApplyDamageEffectAbilityDef in 
                      Repo.GetAllDefs<ApplyDamageEffectAbilityDef>().Where(a=>a.DamagePayload.DamageKeywords.Any(k=>k.DamageKeywordDef== shockDamageKeywordDef)))
                      {
                      TFTVLogger.Always($"{ApplyDamageEffectAbilityDef.name} does shock damage; {ApplyDamageEffectAbilityDef.DamagePayload.DamageKeywords.FirstOrDefault(dk => dk.DamageKeywordDef == shockDamageKeywordDef).Value}");
                  }*/

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AddViralBodyPartTagToEliteViralArthronGun()
        {
            try
            {
                DefCache.GetDef<WeaponDef>("Crabman_RightHand_Viral_EliteGun_WeaponDef").Tags.Add(DefCache.GetDef<ItemTypeTagDef>("ViralBodypart_TagDef"));
                DefCache.GetDef<WeaponDef>("Crabman_RightHand_Viral_Gun_WeaponDef").Tags.Add(DefCache.GetDef<ItemTypeTagDef>("ViralBodypart_TagDef"));


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void VirophageDamage()
        {
            try
            {
                GameTagDamageKeywordDataDef virophageDamageKeyWordDataDef = DefCache.GetDef<GameTagDamageKeywordDataDef>("Virophage_DamageKeywordDataDef");
                StandardDamageTypeEffectDef projectileStandardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                DamageEffectDef genericDamageEffectDef = DefCache.GetDef<DamageEffectDef>("Generic_DamageEffectDef");

                DamageEffectDef newViroDamageEffectDef = Helper.CreateDefFromClone(genericDamageEffectDef, "{088D8BAC-4A0F-4D2A-813A-D7AF951B3C41}", $"TFTV_Viro_{genericDamageEffectDef.name}");
                newViroDamageEffectDef.ArmourPiercing = 200;
                StandardDamageTypeEffectDef newViroStandardDamageTypeEffectDef
                    = Helper.CreateDefFromClone(projectileStandardDamageTypeEffectDef, "{47979269-E470-4176-AC1E-DBD23076E90D}", $"TFTV_Viro_{projectileStandardDamageTypeEffectDef.name}");
                newViroStandardDamageTypeEffectDef.FormulaEffect = newViroDamageEffectDef;
                virophageDamageKeyWordDataDef.DamageTypeDef = newViroStandardDamageTypeEffectDef;
                virophageDamageKeyWordDataDef.RequiredGameTagDefs = virophageDamageKeyWordDataDef.RequiredGameTagDefs.AddToArray(Shared.SharedGameTags.AnuMutationTag);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static InputAction CreateHotkey(KeyCode keyCode, string keyName, string actionName, InputAction.ActionCategory actionCategory, int hash, InputSetDef inputSetDef)
        {
            try
            {
                InputKey inputKey = new InputKey
                {
                    Name = keyName,
                    Hash = (int)keyCode,
                    InputSource = InputSource.Key,
                    DeadzoneOverride = -1.0f

                };

                // Create a new InputChord using the "V" key
                InputChord inputChord = new InputChord
                {
                    OverridingBehavior = InputChord.ActionOverriding.OverridingHidden,
                    Keys = new InputKey[] { inputKey }
                };

                InputAction inputAction = new InputAction
                {
                    Name = actionName,
                    ActionSection = actionCategory,
                    ActionDisplayText = new LocalizedTextBind(),
                    Chords = new InputChord[] { inputChord },
                    Hash = hash
                };

                InputMapDef inputMapDef = DefCache.GetDef<InputMapDef>("PhoenixInput");
                inputMapDef.Actions = inputMapDef.Actions.AddToArray(inputAction);
                inputSetDef.ActionNames = inputSetDef.ActionNames.AddToArray(inputAction.Name);

                return inputAction;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreateAircraftHotkeys()
        {
            try
            {
                InputMapDef inputMapDef = DefCache.GetDef<InputMapDef>("PhoenixInput");


                for (int x = 1; x < 10; x++)
                {

                    int hash = inputMapDef.Actions.Last().Hash + x;
                    KeyCode keyCode = (KeyCode)62 + x;



                    //   TFTVLogger.Always($"SelectAircraft{x} hash: {hash}, keycode = {keyCode}, {keyCode.GetHashCode()}");



                    InputAction inputAction = CreateHotkey(keyCode, x.ToString(), $"SelectAircraft{x}", InputAction.ActionCategory.Geoscape, hash, DefCache.GetDef<InputSetDef>("SetVehicleSelectedControls"));
                    TFTVDragandDropFunctionality.VehicleRoster.ActionsAircraftHotkeys.Add(inputAction);
                }




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreateHotkeys()
        {
            try
            {
                CreateAircraftHotkeys();

                InputMapDef inputMapDef = DefCache.GetDef<InputMapDef>("PhoenixInput");

                InputAction inputAction = CreateHotkey((KeyCode)46, "v", "DisplayPerceptionCircles", InputAction.ActionCategory.Tactical,
                    inputMapDef.Actions.Last().Hash + 1, DefCache.GetDef<InputSetDef>("SetCharacterSelectedControls"));
                TFTVVanillaFixes.UI.ShowPerceptionCircles = inputAction;



                /*  foreach (InputAction inputAction1 in inputMapDef.Actions)
                  {
                      foreach (InputChord inputChord in inputAction1.Chords)
                      {
                          foreach (InputKey inputKey in inputChord.Keys)
                          {
                              TFTVLogger.Always($"{inputAction1?.Name} {inputAction1?.Hash} {inputKey?.Name} {inputKey?.Hash}");
                          }
                      }
                  }*/

                /* foreach (string inputAction in inputSetDef.ActionNames)
                 {
                     TFTVLogger.Always($"{inputAction}");
                 }*/



                //SelectAircraftHotkeys();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateBackgrounds()
        {
            try
            {
                //TFTVBackgrounds.LoadTFTVBackgrounds();

                ChangeBuilderViewParams();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void AdjustColorDefs()
        {
            try
            {
                Color voidColor = TFTVUITactical.VoidColor;
                Color negativeColor = TFTVUITactical.NegativeColor;

                DefCache.GetDef<UIColorDef>("UIColorDef_Corruption").Color = voidColor;

                Color alphaColor = new Color(voidColor.r, voidColor.g, voidColor.b, 0.5f);

                DefCache.GetDef<UIColorDef>("UIColorDef_Corruption_Alpha").Color = alphaColor;

                DefCache.GetDef<UIColorDef>("UIColorDef_UIColor_Negative").Color = negativeColor;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void ChangeBuilderViewParams()
        {
            try
            {
                CharacterBuilderViewParametersDef smallCharacters = DefCache.GetDef<CharacterBuilderViewParametersDef>("SmallCharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef defaultCharacters = DefCache.GetDef<CharacterBuilderViewParametersDef>("DefaultCharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef bigCharacters = DefCache.GetDef<CharacterBuilderViewParametersDef>("3x3CharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef hugeCharacters = DefCache.GetDef<CharacterBuilderViewParametersDef>("5x5CharBuilderViewParametersDef");

                CharacterBuilderViewParametersDef newDefault = Helper.CreateDefFromClone(defaultCharacters, "{89E6A4EB-7F0D-43FD-9C2D-88C80D9AE190}", "PandoranDefaultCharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef newSmall = Helper.CreateDefFromClone(smallCharacters, "{00822DEA-B807-4A22-9176-709ABCA42561}", "PandoranSmallCharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef newBig = Helper.CreateDefFromClone(bigCharacters, "{8EA294A4-648E-40B8-AF85-0FC906144C01}", "PandoranBigCharBuilderViewParametersDef");
                CharacterBuilderViewParametersDef newHuge = Helper.CreateDefFromClone(hugeCharacters, "{4ABB0218-790D-4C37-AB9A-ECC9AB0A8EB0}", "PandoranHugeCharBuilderViewParametersDef");

                newDefault.ObjectWorldPosition.z -= 0.45f;
                newSmall.ObjectWorldPosition.z -= 0.45f;
                newSmall.ObjectWorldPosition.y -= 0.3f;
                newBig.ObjectWorldPosition.z -= 0.45f;
                newBig.ObjectScale = new Vector3(0.80f, 0.80f, 0.80f);
                newBig.ObjectWorldPosition.y -= 0.5f;
                newHuge.ObjectWorldPosition.z -= 0.45f;





                //Default size
                List<ViewElementDef> defaultSize = new List<ViewElementDef>()
                {
                DefCache.GetDef<ViewElementDef>("E_View [Crabman_ActorViewDef]"),
                DefCache.GetDef<ViewElementDef>("E_View [Fishman_ActorViewDef]"),
                DefCache.GetDef<ViewElementDef>("E_View [Siren_ActorViewDef]")
                };

                foreach (var viewElementDef in defaultSize)
                {
                    viewElementDef.BuilderViewParamDef = newDefault;
                }



                List<ViewElementDef> smallSize = new List<ViewElementDef>()
                { DefCache.GetDef<ViewElementDef>("E_View [Acidworm_ActorViewDef]"),
                DefCache.GetDef<ViewElementDef>("E_View [Facehugger_ActorViewDef]"),
                DefCache.GetDef<ViewElementDef>("E_View [Poisonworm_ActorViewDef]"),
                (ViewElementDef)Repo.GetDef("832a0ad2-507a-ab61-ee68-5afb4da8d982") //E_View [Fireworm_ActorViewDef]
                };

                foreach (var viewElementDef in smallSize)
                {
                    viewElementDef.BuilderViewParamDef = newSmall;
                }



                List<ViewElementDef> bigSize = new List<ViewElementDef>()
                {
                DefCache.GetDef<ViewElementDef>("E_View [Acheron_ActorViewDef]"),
                DefCache.GetDef<ViewElementDef>("E_View [Chiron_ActorViewDef]"),

                };

                foreach (var viewElementDef in bigSize)
                {
                    viewElementDef.BuilderViewParamDef = newBig;
                }


                DefCache.GetDef<ViewElementDef>("E_ViewElement [Queen_ActorViewDef]").BuilderViewParamDef = newHuge;




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        private static void AddAlwaysDeployTagToUniqueDeployments()
        {
            try
            {

                List<TacCharacterDef> specialTemplates = new List<TacCharacterDef>()
{
    DefCache.GetDef<TacCharacterDef>("S_NEU_Sniper_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_NEU_Heavy_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_IN_Assault_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_NJ_Heavy_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_NJ_Sniper_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_SY_Infiltrator_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_AN_BerserkerHeavy_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_AN_Priest_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_AN_BerserkerWatcher_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_SirenSuper_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_IN_Madman_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_AN_Puppeteer_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_AN_Berserker_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_IN_SuperThief_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_Chiron_Mortar_TacCharacterDef"),
    DefCache.GetDef<TacCharacterDef>("S_Chiron_FireWorm_TacCharacterDef")
};

                foreach (TacCharacterDef characterDef in specialTemplates)
                {
                    if (characterDef.Data.GameTags.Contains(AlwaysDeployTag))
                    {
                        characterDef.Data.GameTags = characterDef.Data.GameTags.AddToArray(AlwaysDeployTag);
                    }
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateAlwaysDeployTag()
        {
            try
            {
                AlwaysDeployTag = TFTVCommonMethods.CreateNewTag("AlwaysDeployTag", "{34603F61-05E3-424C-ABDB-99B9D63BDAD5}");
                TFTVTacticalDeploymentEnemies.AlwaysDeployTag = AlwaysDeployTag;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AdjustMistSentinelDetection()
        {
            try
            {
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [SentinelMist_Surveillance_AbilityDef]").Origin.Range = 15;

                // DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("TriggerSurveillance_ZoneOfControlStatusDef").Range = 15;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void ChangeBehemothStomp()
        {
            try
            {

                DamagePayloadEffectDef damagePayloadEffectDef = DefCache.GetDef<DamagePayloadEffectDef>("BehemothMassStomp_Electroshock_DamagePayloadEffectDef");

                StatusEffectDef newStatusEffectDef = Helper.CreateDefFromClone(DefCache.GetDef<StatusEffectDef>("Stun_StatusEffectDef"),
                    "{E6E67870-F514-46FB-8629-C08C47C817C9}", "Silenced_StatusEffectDef");

                newStatusEffectDef.StatusDef = DefCache.GetDef<StatusDef>("ActorSilenced_StatusDef");

                damagePayloadEffectDef.DamagePayload.CustomEffect = newStatusEffectDef;

                damagePayloadEffectDef.DamagePayload.DamageKeywords[0].Value = 1;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }



        private static void AdjustDashAI()
        {
            try
            {
                // AIClosestEnemyConsiderationDef
                AIActionMoveAndAttackDef dashAndStrike = DefCache.GetDef<AIActionMoveAndAttackDef>("DashAndStrike_AIActionDef");
                AIActionMoveAndAttackDef moveAndStrike = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndStrike_AIActionDef");
                AIActionMoveAndAttackDef dashAndShoot = DefCache.GetDef<AIActionMoveAndAttackDef>("DashAndShoot_AIActionDef");
                AIActionMoveToPositionDef dashAI = DefCache.GetDef<AIActionMoveToPositionDef>("Dash_AIActionDef");

                AIHasEnemiesInRangeConsiderationDef newClearanceRangeConsideration = Helper.CreateDefFromClone(

                    DefCache.GetDef<AIHasEnemiesInRangeConsiderationDef>("ClearanceRange_AIConsiderationDef"),
                    "{44AD1B5B-C284-41AB-854B-49356CA7373E}",
                    "Dash_RangeClearanceConsiderationDef"
                );

                newClearanceRangeConsideration.Reverse = false;
                newClearanceRangeConsideration.MaxRange = 5;

                moveAndStrike.Weight = 350;
                dashAndStrike.Weight = 300;
                dashAI.Weight = 10;
                dashAndShoot.Weight = 50;

                dashAI.EarlyExitConsiderations[3].Consideration = newClearanceRangeConsideration;
                //dashAI.EarlyExitConsiderations = new AIAdjustedConsideration[] { dashAI.EarlyExitConsiderations[0], dashAI.EarlyExitConsiderations[1], dashAI.EarlyExitConsiderations[2],  };

                dashAI.Evaluations[0].Considerations[0].Consideration = DefCache.GetDef<AIStrategicPositionConsiderationDef>("StrategicPositionOff_AIConsiderationDef");
                dashAI.Evaluations[0].Considerations[2].Consideration = DefCache.GetDef<AILineOfSightToEnemiesConsiderationDef>("LineofSight_AIConsiderationDef");
                dashAI.Evaluations[0].Considerations[3].Consideration = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Worm_ClosestPathToEnemy_AIConsiderationDef");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }








        private static void LoadingScreensAndLore()
        {
            try
            {
                AddLoadingScreens();
                AddTips();
                AddLoreEntries();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void GeoscapeEvents()
        {
            try
            {
                AugmentationEventsDefs();
                CreateFoodPoisoningEvents();
                CreateIntro();
                Create_VoidOmen_Events();
                InjectAlistairAhsbyLines();
                InjectOlenaKimLines();
                TFTVChangesToDLC1andDLC2Events.ChangesToDLC1andDLC2Defs();
                TFTVChangesToDLC3Events.ChangesToDLC3Defs();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void Marketplace()
        {
            try
            {
                TFTVChangesToDLC5.TFTVMercenaries.Defs.CreateMercenariesDefs();
                TFTVChangesToDLC5.TFTVKaosGuns.CreateKaosWeaponAmmo();
                TFTVChangesToDLC5.TFTVMarketPlaceItems.AdjustMarketPlaceOptions();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void SpecialDifficulties()
        {
            try
            {
                ModifyPandoranProgress();
                CreateRookieVulnerability();
                CreateRookieProtectionStatus();
                CreateEtermesStatuses();
                CreateETERMESDifficultyLevel();
                CreateStoryModeDifficultyLevel();
                ModifyVanillaDifficultiesOrder();
                CreateScyllaDamageResistanceForStrongerPandorans();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void VariousMinorAdjustments()
        {
            try
            {
                // LimitCoDeliriumAttack();

                ModifyDecoyAbility();
                RemoveCorruptionDamageBuff();
                ModifyCratesToAddArmor();
                TFTVReverseEngineering.ModifyReverseEngineering();
                TFTVInfestation.Defs.ImplementInfestationDefs();
                ChangesAmbushMissions();
                RemoveCensusResearch();
                AllowMedkitsToTargetMutoidsAndChangesToMutoidSkillSet();
                MistOnAllMissions();
                RemoveScyllaAndNodeResearches();
                ChangeMyrmidonAndFirewormResearchRewards();
                ModifyMissionDefsToReplaceNeutralWithBandit();
                CreateReinforcementTag();
                Change_Crossbows();
                MakeMindControlImmunityRemovable();
                ReducePromoSkins();
                ChangeFireNadeCostAndDamage();
                ChangesToMedbay();
                RestrictCanBeRecruitedIntoPhoenix();
                RemoveMindControlImmunityVFX();
                RemovePirateKing();
                ChangeVehicleInventorySlots();
                StealAircraftMissionsNoItemRecovery();
                AddMissingElectronicTags();
                AddContributionPointsToPriestAndTech();
                ModifyRecruitsCost();
                ModifyRescueCiviliansMissions();
                TFTVBetterEnemies.BEChange_Perception();
                TFTVBetterEnemies.BEReducePandoranWillpower();
                BringBackArmisAndCrystalChiron();
                LimitCoDeliriumAttack();
                AdjustAlienAmbushChance();
                IncreaseBionicLabCost();
                ReduceEffectOfMistOnPerception();
                ChangeBehemothStomp();
                TFTVBaseDefenseTactical.Defs.CreateDefsForBaseDefenseTactical();
                RemoveTerrorSentinelCitadel();
                RemoveOrganicConditionForSlowedStatusAndMakeSingleApplication();
                AdjustMistSentinelDetection();
                Create_StarvedAbility();
                TFTVUITactical.SecondaryObjectivesTactical.Defs.CreateDefs();
                AdjustColorDefs();


                //  ChangeBuilderViewParams();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void Create_StarvedAbility()
        {
            try
            {
                string skillName = "StarvedAbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef starved = Helper.CreateDefFromClone(
                    source,
                    "{1BAF6AB9-DC78-4267-BF26-65C9AC9C1BE2}",
                    skillName);
                starved.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "{1D4C2FC3-DEDD-4A40-AA8F-067CD02FA858}",
                    skillName);
                starved.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "{89E99192-8363-4E3D-A699-75702EDEE674}",
                    skillName);
                starved.StatModifications = new ItemStatModification[0];
                starved.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                starved.ViewElementDef.DisplayName1.LocalizationKey = "TFTV_STARVED_NAME";
                starved.ViewElementDef.Description.LocalizationKey = "TFTV_STARVED_DESCRIPTION";
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_Starved.png");
                starved.ViewElementDef.LargeIcon = icon;
                starved.ViewElementDef.SmallIcon = icon;
                TFTVHarmonyGeoscape.StarvedAbility = starved;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void RemoveMindControlImmunityVFX()
        {
            try
            {
                DefCache.GetDef<TacStatusDef>("MindControlImmunity_StatusDef").ParticleEffectPrefab = new GameObject() { };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        private static void RemoveOrganicConditionForSlowedStatusAndMakeSingleApplication()
        {
            try
            {
                TacStatsModifyStatusDef slowedStatusDef = DefCache.GetDef<TacStatsModifyStatusDef>("Slowed_StatusDef");

                slowedStatusDef.ApplicationConditions = new EffectConditionDef[] { };
                slowedStatusDef.SingleInstance = true;

                TacStatusEffectDef confusionCloudEffect = DefCache.GetDef<TacStatusEffectDef>("E_ApplySlowedStatus [Acheron_ParalyticCloud_AbilityDef]");

                confusionCloudEffect.ApplicationConditions = new EffectConditionDef[]
                {
                DefCache.GetDef<ActorHasTagEffectConditionDef>("E_HasOrganicTag [Corruption_StatusDef]")
                };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void RemoveTerrorSentinelCitadel()
        {
            try
            {
                CustomMissionTypeDef citadelMission = DefCache.GetDef<CustomMissionTypeDef>("CitadelAlien_CustomMissionTypeDef");
                citadelMission.ParticipantsData[0].ActorDeployParams.RemoveAt(1);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void ReduceEffectOfMistOnPerception()
        {
            try
            {
                DefCache.GetDef<TacticalPerceptionDef>("Soldier_PerceptionDef").MistBlobPerceptionRangeCost = 5;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AdjustAlienAmbushChance()
        {
            try
            {
                GeoAlienFactionDef geoAlienFactionDef = DefCache.GetDef<GeoAlienFactionDef>("Alien_GeoAlienFactionDef");
                geoAlienFactionDef.ScavengingAmbushBaseWeight = 60;
                geoAlienFactionDef.ScavengingAmbushSitesRange.Value = 2000;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void BringBackArmisAndCrystalChiron()
        {
            try
            {
                ResearchDef siren5 = DefCache.GetDef<ResearchDef>("ALN_Siren5_ResearchDef");
                siren5.ResearchCost = 200;
                DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Siren5_ResearchDef_ExistingResearchRequirementDef_1").ResearchID = "ALN_Acheron6_ResearchDef";

                ResearchDef chiron13 = DefCache.GetDef<ResearchDef>("ALN_Chiron13_ResearchDef");
                chiron13.ResearchCost = 200;
                DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Chiron13_ResearchDef_ExistingResearchRequirementDef_1").ResearchID = "ALN_Chiron12_ResearchDef";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }

        private static void IncreaseBionicLabCost()
        {
            try
            {
                DefCache.GetDef<PhoenixFacilityDef>("BionicsLab_PhoenixFacilityDef").ResourceCost = new ResourcePack()
                {
                    new ResourceUnit(){ Type = ResourceType.Materials, Value = 300},
                    new ResourceUnit(){ Type = ResourceType.Tech, Value = 225},
                };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void VanillaFixes()
        {
            try
            {
                FixUnarmedAspida();
                FixPriestScream();
                SyphonAttackFix();
                FixMyrmidonFlee();
                FixBionic3ResearchNotGivingAccessToFacility();
                FixNoXPCaptureAcheron();
                FixSpikeShootingArmShootingWhenDisabled();
                FixSilentInfiltrators();
                FixResearchRequirements();
                FixMindControlImmunityNotRestoredWhenHeadReenabled();
                FixChironStompIgnoringFriendlies();
                FixMindWard();
                FixAcheronAiming();
                FixInstilFrenzySound();
                FixMutoidDazeImmunity();
                FixMutagenCostBadAcidWorm();
                FixNotCapturableFaceHuggers();
                FixFactionLevel1Templates();
                FixByzantiumAutoRecover();

                // FixUmbraFire(); doesn't work because status removed before check - implemented differently elsewhere
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void FixByzantiumAutoRecover()
        {
            try
            {
                DefCache.GetDef<CustomMissionTypeDef>("StoryNJ_Chain1_CustomMissionTypeDef").DontRecoverItems = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixFactionLevel1Templates()
        {
            try
            {

                /* foreach (UnitTemplateResearchRewardDef unitTemplateResearchRewardDef in Repo.GetAllDefs<UnitTemplateResearchRewardDef>().
                     Where(t => t.Add == true && t.Template.Data.LevelProgression.Level == 1 && !t.name.Contains("ALN")))
                 {
                     TacCharacterDef tacCharacterDef = unitTemplateResearchRewardDef.Template;

                     TFTVLogger.Always($"{tacCharacterDef.name} added in {unitTemplateResearchRewardDef.name}");

                     if (Repo.GetAllDefs<UnitTemplateResearchRewardDef>().Any(t => t.Template == tacCharacterDef && t.Add == false))
                     {
                         TFTVLogger.Always($"{tacCharacterDef.name} removed in {Repo.GetAllDefs<UnitTemplateResearchRewardDef>().FirstOrDefault(t => t.Template == tacCharacterDef && t.Add == false).name}", false);
                     }
                     else
                     {
                         TFTVLogger.Always($"{tacCharacterDef.name} never removed", false);
                     }
                 }*/

                ResearchDef gaussResearch = DefCache.GetDef<ResearchDef>("NJ_GaussTech_ResearchDef");
                ResearchDef newJerichoResearch = DefCache.GetDef<ResearchDef>("PX_NewJericho_ResearchDef");
                gaussResearch.Unlocks = gaussResearch.Unlocks.AddRangeToArray(newJerichoResearch.Unlocks);
                newJerichoResearch.Unlocks = new ResearchRewardDef[] { };

                ResearchDef laserResearch = DefCache.GetDef<ResearchDef>("SYN_LaserWeapons_ResearchDef");
                ResearchDef synedrionResearch = DefCache.GetDef<ResearchDef>("PX_Synedrion_ResearchDef");
                laserResearch.Unlocks = laserResearch.Unlocks.AddRangeToArray(synedrionResearch.Unlocks);
                synedrionResearch.Unlocks = new ResearchRewardDef[] { };

                ResearchDef anuWarfareResearch = DefCache.GetDef<ResearchDef>("ANU_AnuWarfare_ResearchDef");
                ResearchDef anuResearch = DefCache.GetDef<ResearchDef>("PX_DisciplesOfAnu_ResearchDef");
                anuWarfareResearch.Unlocks = anuWarfareResearch.Unlocks.AddRangeToArray(anuResearch.Unlocks);
                anuResearch.Unlocks = new ResearchRewardDef[] { };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }







        private static void FixNotCapturableFaceHuggers()
        {
            try
            {
                TacCharacterDef faceHuggerNormal = DefCache.GetDef<TacCharacterDef>("Facehugger_AlienMutationVariationDef");
                TacCharacterDef faceHuggerOther = DefCache.GetDef<TacCharacterDef>("Facehugger_TacCharacterDef");

                faceHuggerOther.Data.GameTags = faceHuggerNormal.Data.GameTags;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixMutagenCostBadAcidWorm()
        {
            try
            {
                TacCharacterDef badAcidWorm = DefCache.GetDef<TacCharacterDef>("AcidwormTest_AlienMutationVariationDef");
                TacCharacterDef regularAcidWorm = DefCache.GetDef<TacCharacterDef>("Acidworm_AlienMutationVariationDef");
                badAcidWorm.DeploymentCost = regularAcidWorm.DeploymentCost;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixMutoidDazeImmunity()
        {
            try
            {
                ApplyStatusAbilityDef applyStatusAbilityDef = DefCache.GetDef<ApplyStatusAbilityDef>("MutoidDazeImmunity_AbilityDef");

                AbilityTrackDef scyllaAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [ScyllaSpecializationDef]");



                string name = "MutoidStunImmunity_AbilityDef";

                StatusImmunityAbilityDef statusImmunityAbilityDef = DefCache.GetDef<StatusImmunityAbilityDef>("StunStatusImmunity_AbilityDef");



                StatusImmunityAbilityDef newStatusImmunityAbility = Helper.CreateDefFromClone(
                        statusImmunityAbilityDef,
                        "{30D6D912-BAFB-4B9C-B611-453B96DCB302}",
                        name);

                newStatusImmunityAbility.ViewElementDef = Helper.CreateDefFromClone(
                    applyStatusAbilityDef.ViewElementDef,
                    "{58E9DD2E-EF55-476E-8D30-9D7330F22E22}",
                    name);

                newStatusImmunityAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    applyStatusAbilityDef.CharacterProgressionData,
                    "{3E0E7423-877A-4281-BDB5-5CD01084E342}",
                    name);


                scyllaAbilityTrack.AbilitiesByLevel[2].Ability = newStatusImmunityAbility;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixInstilFrenzySound()
        {
            try
            {
                DefCache.GetDef<TacticalEventDef>("FrenzyStatus_TargetEffect_EventDef").AudioData.Mute = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void FixAcheronAiming()
        {
            try
            {
                TacticalPerceptionDef tacticalPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Acheron_PerceptionDef");
                tacticalPerceptionDef.DefaultAimSlot = DefCache.GetDef<ItemSlotDef>("Acheron_Husk_SlotDef");

                List<TacticalItemDef> tacticalItemDefs = new List<TacticalItemDef>()
                {
DefCache.GetDef<TacticalItemDef>("Acheron_Husk_BodyPartDef"),
DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Husk_BodyPartDef"),
DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_Husk_BodyPartDef"),
DefCache.GetDef<TacticalItemDef>("AcheronAsclepius_Husk_BodyPartDef"),
DefCache.GetDef<TacticalItemDef>("AcheronAsclepiusChampion_Husk_BodyPartDef"),
DefCache.GetDef<TacticalItemDef>("AcheronPrime_Husk_BodyPartDef")

                };

                // DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Torso_BodyPartDef").Armor = 20;
                //  DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Husk_BodyPartDef").Armor = 5;
                foreach (TacticalItemDef tacticalItemDef in tacticalItemDefs)
                {
                    int armorRemoved = (int)tacticalItemDef.Armor;

                    tacticalItemDef.Armor = 0;
                    tacticalItemDef.HitPoints += 10 * armorRemoved;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void FixMindWard()
        {
            try
            {
                DefCache.GetDef<DamageMultiplierStatusDef>("PsychicWard_StatusDef").ApplicationConditions = new EffectConditionDef[] { };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixChironStompIgnoringFriendlies()
        {
            try
            {
                DefCache.GetDef<AIAttackPositionConsiderationDef>("Chiron_StompAttackPosition_AIConsiderationDef").FriendlyHitScoreMultiplier = 0.7f;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixMindControlImmunityNotRestoredWhenHeadReenabled()
        {
            try
            {
                DefCache.GetDef<ApplyStatusAbilityDef>("MindControlImmunity_AbilityDef").StatusApplicationTrigger = StatusApplicationTrigger.AbilityAdded;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixResearchRequirements()
        {
            try
            {
                DefCache.GetDef<CaptureActorResearchRequirementDef>("PX_GooRepeller_ResearchDef_CaptureActorResearchRequirementDef_0").IsRetroactive = true;
                DefCache.GetDef<CaptureActorResearchRequirementDef>("PX_AlienVirusInfection_ResearchDef_CaptureActorResearchRequirementDef_0").IsRetroactive = true;
                DefCache.GetDef<CaptureActorResearchRequirementDef>("PX_PyschicAttack_ResearchDef_CaptureActorResearchRequirementDef_0").IsRetroactive = true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void FixSilentInfiltrators()
        {
            try
            {

                GameTagDef silentWeaponTag = DefCache.GetDef<GameTagDef>("SilencedWeapon_TagDef");
                GameTagDef silentSkillTag = DefCache.GetDef<GameTagDef>("Silent_SkillTagDef");

                WeaponDef crystalCrossbow = DefCache.GetDef<WeaponDef>("AC_CrystalCrossbow_WeaponDef");
                WeaponDef bonusCrossbow = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
                WeaponDef basicCrossbow = DefCache.GetDef<WeaponDef>("SY_Crossbow_WeaponDef");
                WeaponDef venomCrossbow = DefCache.GetDef<WeaponDef>("SY_Venombolt_WeaponDef");
                WeaponDef spiderDroneLauncher = DefCache.GetDef<WeaponDef>("SY_SpiderDroneLauncher_WeaponDef");

                List<WeaponDef> crossbowWeapons = new List<WeaponDef>() { crystalCrossbow, basicCrossbow, bonusCrossbow, venomCrossbow, spiderDroneLauncher };

                foreach (WeaponDef weaponDef in crossbowWeapons)
                {
                    if (!weaponDef.Tags.Contains(silentWeaponTag))
                    {
                        weaponDef.Tags.Add(silentWeaponTag);
                    }
                }

                Shared.SharedGameTags.SilentTags = new GameTagDef[] { silentWeaponTag, silentSkillTag };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangesToAI()
        {
            try
            {
                TFTVBetterEnemies.BECreateAIActionDefs();
                TFTVBetterEnemies.BEFixesToAI();
                IncreaseRangeClosestEnemyConsideration();
                ModifyChironWormAndAoETargeting();
                GiveNewActorAIToUmbra();
                AdjustDashAI();
                AddAIActorStatsConditionsConsideration();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void GiveNewActorAIToUmbra()
        {
            try
            {
                TacAIActorDef newAI = NewUmbraAI();

                ComponentSetDef oilCrabComponentSetDef = DefCache.GetDef<ComponentSetDef>("Oilcrab_ComponentSetDef");
                ComponentSetDef oilFishComponentSetDef = DefCache.GetDef<ComponentSetDef>("Oilfish_ComponentSetDef");

                oilCrabComponentSetDef.Components[10] = newAI;
                oilFishComponentSetDef.Components[10] = newAI;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static TacAIActorDef NewUmbraAI()
        {
            try
            {
                TacAIActorDef source = DefCache.GetDef<TacAIActorDef>("CrabmanBrawler_AIActorDef");
                string name = "Umbra_AIActor";
                TacAIActorDef newTacAIActor = Helper.CreateDefFromClone(source, "{1D65840C-D754-44AA-8C2F-A294BF4A55DF}", name);

                newTacAIActor.AIActorData.IsAlerted = true;
                newTacAIActor.AIActionsTemplateDef = Helper.CreateDefFromClone(source.AIActionsTemplateDef, "{2FD1975C-2AC5-44CA-BE56-9B01FBD2A736}", name);

                //EndCharacterTurn_AIActionDef [0]
                //Advance_Aggressive_AIActionDef [1]
                //MoveAndStrike_AIActionDef [2]
                //DeployShield_AIActionDef [3] - NO COPY
                //MoveToRandomWaypoint_AIActionDef [4]
                //MoveToSafePosition_AIActionDef [5] - NO COPY

                //Early exit consideration requires an enemy to be visible by the faction - used by [1] and [2]
                //Need to patch to check if enemy has Delirium, is in Mist or VO in effect
                AIVisibleEnemiesConsiderationDef aIVisibleEnemiesConsiderationDef = DefCache.GetDef<AIVisibleEnemiesConsiderationDef>("AnyFactionVisibleEnemy_AIConsiderationDef");

                //This is set to 32 tiles, should probably increase to 100. [1].Evaluations[1]
                //Need to patch to check if enemy has Delirium, is in Mist or VO in effect
                AIClosestEnemyConsiderationDef aIClosestEnemyConsiderationDef = DefCache.GetDef<AIClosestEnemyConsiderationDef>("Worm_ClosestPathToEnemy_AIConsiderationDef");
                aIClosestEnemyConsiderationDef.DistanceType = DistanceType.PathLength;

                string umbraAIClosestEnemyConsiderationName = "Umbra_ClosestPathToEnemy_AIConsiderationDef";

                newTacAIActor.AIActionsTemplateDef.ActionDefs[1].Evaluations[0].Considerations[1].Consideration = Helper.CreateDefFromClone(
                    aIClosestEnemyConsiderationDef, "{28943F5A-9432-496F-9415-81087C686C9F}", umbraAIClosestEnemyConsiderationName);
                AIClosestEnemyConsiderationDef umbraClosestEnemyConsiderationDef = (AIClosestEnemyConsiderationDef)newTacAIActor.AIActionsTemplateDef.ActionDefs[1].Evaluations[0].Considerations[1].Consideration;
                umbraClosestEnemyConsiderationDef.MaxDistance = 100;

                AIActionMoveAndAttackDef moveAndStrikeAIActionDef = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndStrike_AIActionDef");
                string umbraMoveAndStrikeActionName = "Umbra_MoveAndStrike_AIActionDef";
                AIActionMoveAndAttackDef umbraMoveAndStrikeActionDef = Helper.CreateDefFromClone(moveAndStrikeAIActionDef, "{87D7753B-E778-45D8-82E8-965B3B8D9380}", umbraMoveAndStrikeActionName);

                //used by [2], consider removing consideartions [0] and [1]
                // AIStrategicPositionConsiderationDef aIStrategicPositionConsiderationDef = DefCache.GetDef<AIStrategicPositionConsiderationDef>("StrategicPositionOff_AIConsiderationDef");
                // AILineOfSightToEnemiesConsiderationDef aILineOfSightToEnemiesConsiderationDef = DefCache.GetDef<AILineOfSightToEnemiesConsiderationDef>("NoLineofSight_AIConsiderationDef");

                List<AITargetEvaluation> aITargetEvaluationsMoveAndStrike = umbraMoveAndStrikeActionDef.Evaluations.ToList();

                /*
                 * 
                 * [TFTV @ 12/28/2023 4:21:41 PM] There are Base.AI.Defs.AIAdjustedConsideration[] evaluations
[TFTV @ 12/28/2023 4:21:41 PM] There are Base.AI.Defs.AIAdjustedConsideration[] evaluations now
[TFTV @ 12/28/2023 4:21:41 PM] Checking considerations in umbraMoveAndStrikeActionDef evaluations, count Base.AI.Defs.AITargetEvaluation[]
[TFTV @ 12/28/2023 4:21:41 PM] StrategicPositionOff_AIConsiderationDef
[TFTV @ 12/28/2023 4:21:41 PM] NoLineofSight_AIConsiderationDef
[TFTV @ 12/28/2023 4:21:41 PM] AttackPosition_AIConsiderationDef
[TFTV @ 12/28/2023 4:21:41 PM] NumberOfAttacks_AIConsiderationDef
                 */


                /*   TFTVLogger.Always($"There are {umbraMoveAndStrikeActionDef.Evaluations.Count()} evaluations");
                   foreach (AIAdjustedConsideration aITargetEvaluation in umbraMoveAndStrikeActionDef.Evaluations[1].Considerations)
                   {
                       TFTVLogger.Always($"{aITargetEvaluation.Consideration.name}");
                   }*/


                List<AIAdjustedConsideration> adjustedConsiderations = aITargetEvaluationsMoveAndStrike[1].Considerations.ToList();

                adjustedConsiderations.Remove(aITargetEvaluationsMoveAndStrike[1].Considerations[0]);
                adjustedConsiderations.Remove(aITargetEvaluationsMoveAndStrike[1].Considerations[1]);

                // TFTVLogger.Always($"There are {aITargetEvaluationsMoveAndStrike[1].Considerations} evaluations now");

                aITargetEvaluationsMoveAndStrike[1].Considerations = adjustedConsiderations.ToArray();
                umbraMoveAndStrikeActionDef.Evaluations = aITargetEvaluationsMoveAndStrike.ToArray();

                /*     TFTVLogger.Always($"Checking considerations in umbraMoveAndStrikeActionDef evaluations, count {umbraMoveAndStrikeActionDef.Evaluations.Count()}");
                     foreach (AIAdjustedConsideration aITargetEvaluation in umbraMoveAndStrikeActionDef.Evaluations[1].Considerations)
                     {
                         TFTVLogger.Always($"{aITargetEvaluation.Consideration.name}");
                     }*/

                //consideration [2], using bash, this already takes into account that target has to have Delirium, be in Mist or VO must be active, because it gets Targets given by bash ability
                //However, to make targets with more Delirium more attractive, would need to patch
                AIAttackPositionConsiderationDef aIAttackPositionConsiderationDef = DefCache.GetDef<AIAttackPositionConsiderationDef>("AttackPosition_AIConsiderationDef");

                //consideration [3], AINumberOfAttacksConsiderationDef can be left as is.

                //used by [4], Need to patch to check if enemy has Delirium, is in Mist or VO in effect, so it mirrors anyFactionVisibleEnemyConsideration
                AIVisibleEnemiesConsiderationDef NOaIVisibleEnemiesConsiderationDef = DefCache.GetDef<AIVisibleEnemiesConsiderationDef>("NoFactionVisibleEnemy_AIConsiderationDef");

                List<AIActionDef> aIActionDefs = new List<AIActionDef>() {
                    source.AIActionsTemplateDef.ActionDefs[0],
                    source.AIActionsTemplateDef.ActionDefs[1],
                    umbraMoveAndStrikeActionDef,
                    source.AIActionsTemplateDef.ActionDefs[4],
                };

                newTacAIActor.AIActionsTemplateDef.ActionDefs = aIActionDefs.ToArray();

                return newTacAIActor;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        //NEU_Assault_Torso_BodyPartDef
        //NEU_Assault_Legs_ItemDef
        //NEU_Sniper_Helmet_BodyPartDef
        //NEU_Sniper_Torso_BodyPartDef
        //NEU_Sniper_Legs_ItemDef

        private static void LimitCoDeliriumAttack()
        {
            try
            {
                AddAttackBoostStatusDef codelirium = DefCache.GetDef<AddAttackBoostStatusDef>("CorruptionAttack_StatusDef");

                codelirium.WeaponTagFilter = DefCache.GetDef<ItemClassificationTagDef>("MeleeWeapon_TagDef");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);


            }
        }

        private static void Change_Crossbows()
        {
            try
            {
                WeaponDef ErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_WeaponDef");
                WeaponDef BonusErosCrb = DefCache.GetDef<WeaponDef>("SY_Crossbow_Bonus_WeaponDef");
                ItemDef ErosCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Crossbow_AmmoClip_ItemDef");
                WeaponDef PsycheCrb = DefCache.GetDef<WeaponDef>("SY_Venombolt_WeaponDef");
                ItemDef PsycheCrb_Ammo = DefCache.GetDef<ItemDef>("SY_Venombolt_AmmoClip_ItemDef");
                ErosCrb.ChargesMax = 5;
                BonusErosCrb.ChargesMax = 5;
                ErosCrb_Ammo.ChargesMax = 5;
                PsycheCrb.ChargesMax = 4;
                PsycheCrb_Ammo.ChargesMax = 4;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void IncreaseRangeClosestEnemyConsideration()
        {
            try
            {
                DefCache.GetDef<AIClosestEnemyConsiderationDef>("Queen_ClosestEnemy_AIConsiderationDef").MaxDistance = 100;
                DefCache.GetDef<AIClosestEnemyConsiderationDef>("Chiron_ClosestEnemy_AIConsiderationDef").MaxDistance = 100;
                DefCache.GetDef<AIClosestEnemyConsiderationDef>("Acheron_ClosestLineToEnemy_AIConsiderationDef").MaxDistance = 100;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ModifyVanillaDifficultiesOrder()
        {
            try
            {
                DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef").Order = 2;
                DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef").Order = 3;
                DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef").Order = 4;
                DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef").Order = 5;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateETERMESDifficultyLevel()
        {
            try
            {
                GameDifficultyLevelDef sourceDef = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                GameDifficultyLevelDef newDifficulty = Helper.CreateDefFromClone(sourceDef, "{F713C90F-5D7D-4F95-B71A-CE094A7DA6AE}", "Etermes_DifficultyLevelDef");
                newDifficulty.Order = 6;
                newDifficulty.Name.LocalizationKey = "TFTV_DIFFICULTY_ETERMES_TITLE";
                newDifficulty.Description.LocalizationKey = "TFTV_DIFFICULTY_ETERMES_DESCRIPTION";

                newDifficulty.RecruitCostPerLevelMultiplier = 0.5f;
                newDifficulty.RecruitmentPriceModifier = 1.3f;
                newDifficulty.NestLimitations.MaxNumber = 4;
                newDifficulty.NestLimitations.HoursBuildTime = 73;
                newDifficulty.LairLimitations.MaxNumber = 4;
                newDifficulty.LairLimitations.MaxConcurrent = 4;
                newDifficulty.LairLimitations.HoursBuildTime = 80;
                newDifficulty.CitadelLimitations.HoursBuildTime = 144;

                newDifficulty.InitialDeploymentPoints = 812;
                newDifficulty.FinalDeploymentPoints = 3125;
                newDifficulty.DaysToReachFinalDeployment = 72;

                List<GameDifficultyLevelDef> difficultyLevelDefs = new List<GameDifficultyLevelDef>(Shared.DifficultyLevels) { newDifficulty };

                Shared.DifficultyLevels = difficultyLevelDefs.ToArray();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateStoryModeDifficultyLevel()
        {
            try
            {
                GameDifficultyLevelDef sourceDef = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");
                GameDifficultyLevelDef newDifficulty = Helper.CreateDefFromClone(sourceDef, "{B10E3C8C-1398-4398-B1A6-A93DB0C48781}", "StoryMode_DifficultyLevelDef");
                newDifficulty.Order = 1;
                newDifficulty.Name.LocalizationKey = "TFTV_DIFFICULTY_ROOKIE_TITLE";
                newDifficulty.Description.LocalizationKey = "TFTV_DIFFICULTY_ROOKIE_DESCRIPTION";

                List<GameDifficultyLevelDef> difficultyLevelDefs = new List<GameDifficultyLevelDef>(Shared.DifficultyLevels);
                difficultyLevelDefs.Insert(0, newDifficulty);

                Shared.DifficultyLevels = difficultyLevelDefs.ToArray();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void MakeMindControlImmunityRemovable()
        {
            try
            {
                DefCache.GetDef<ApplyStatusAbilityDef>("MindControlImmunity_AbilityDef").RemoveStatusOnAbilityRemoving = true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangeStoryAN4_CustomMissionTypeDef()
        {
            try
            {
                WipeEnemyFactionObjectiveDef sourceWipeEnemyObjective = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("300WipeEnemy_CustomMissionObjective");

                WipeEnemyFactionObjectiveDef newWipeEnemyObjective = Helper.CreateDefFromClone(sourceWipeEnemyObjective, "{C8E9CA43-D615-4A57-A123-C1082D718702}", "newWipeEnemyObjective");

                newWipeEnemyObjective.IsUiHidden = true;



                CustomMissionTypeDef anStory4 = DefCache.GetDef<CustomMissionTypeDef>("StoryAN4_CustomMissionTypeDef");

                anStory4.CustomObjectives = anStory4.CustomObjectives.AddToArray(newWipeEnemyObjective);

                foreach (FactionObjectiveDef factionObjective in anStory4.CustomObjectives)
                {
                    TFTVLogger.Always($"{factionObjective.name}");


                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void CreateAndAdjustDefsForLimitedCapture()
        {
            try
            {
                // CreateObjectiveCaptureCapacity(); Removed because now in Capture Widget.
                ChangeResourceRewardsForAutopsies();
                AdjustPandoranVolumes();
                ChangesToCapturingPandorans();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void ChangesToCapturingPandorans()
        {
            try
            {

                string captureAbilityName = "CapturePandoran_Ability";
                ApplyStatusAbilityDef applyStatusAbilitySource = DefCache.GetDef<ApplyStatusAbilityDef>("MarkedForDeath_AbilityDef");
                ApplyStatusAbilityDef newCaptureAbility = Helper.CreateDefFromClone(applyStatusAbilitySource, "{8850B4B0-5545-4FCE-852A-E56AFA19DED6}", captureAbilityName);

                string removeCaptureAbilityName = "RemoveCapturePandoran_Ability";
                ApplyStatusAbilityDef removeCaptureStatusAbility = Helper.CreateDefFromClone(applyStatusAbilitySource, "{1D24098D-5C9A-4698-8062-5BAF974ADE35}", removeCaptureAbilityName);
                removeCaptureStatusAbility.ViewElementDef = Helper.CreateDefFromClone(applyStatusAbilitySource.ViewElementDef, "{19FF369F-868B-4DFA-90AE-E72D4075B868}", removeCaptureAbilityName);
                removeCaptureStatusAbility.ViewElementDef.DisplayName1.LocalizationKey = "CANCEL_CAPTURE_NAME";
                removeCaptureStatusAbility.ViewElementDef.Description.LocalizationKey = "CANCEL_CAPTURE_DESCRIPTION";
                removeCaptureStatusAbility.TargetingDataDef = Helper.CreateDefFromClone(applyStatusAbilitySource.TargetingDataDef, "{910F0071-BDD3-4FA2-9CD4-17199D938637}", removeCaptureAbilityName);
                removeCaptureStatusAbility.TargetingDataDef.Origin.LineOfSight = LineOfSightType.Ignore;

                newCaptureAbility.ViewElementDef = Helper.CreateDefFromClone(applyStatusAbilitySource.ViewElementDef, "{C740EF09-6068-4ADB-9E38-7F6F504ACC07}", captureAbilityName);
                newCaptureAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ability_capture.png");
                newCaptureAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ability_capture_small.png");
                newCaptureAbility.ViewElementDef.DisplayName1.LocalizationKey = "CAPTURE_ABILITY_NAME";
                newCaptureAbility.ViewElementDef.Description.LocalizationKey = "CAPTURE_ABILITY_DESCRIPTION";


                newCaptureAbility.TargetingDataDef = Helper.CreateDefFromClone(applyStatusAbilitySource.TargetingDataDef, "{AB7A060C-2CB1-4DD6-A21F-A018BC8B0600}", captureAbilityName);
                newCaptureAbility.TargetingDataDef.Origin.LineOfSight = LineOfSightType.Ignore;
                newCaptureAbility.WillPointCost = 0;

                string captureStatusName = "CapturePandoran_Status";
                ParalysedStatusDef paralysedStatusDef = DefCache.GetDef<ParalysedStatusDef>("Paralysed_StatusDef");
                ReadyForCapturesStatusDef newCapturedStatus = Helper.CreateDefFromClone<ReadyForCapturesStatusDef>(null, "{96B40C5A-7FF2-4C67-83DA-ACEF0BE7D2E8}", captureStatusName);
                newCapturedStatus.EffectName = "ReadyForCapture";
                newCapturedStatus.Duration = paralysedStatusDef.Duration;
                newCapturedStatus.DurationTurns = -1;
                newCapturedStatus.ExpireOnEndOfTurn = false;
                newCapturedStatus.ApplicationConditions = paralysedStatusDef.ApplicationConditions;
                newCapturedStatus.DisablesActor = true;
                newCapturedStatus.SingleInstance = false;
                newCapturedStatus.ShowNotification = true;
                newCapturedStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newCapturedStatus.VisibleOnPassiveBar = true;
                newCapturedStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newCapturedStatus.HealthbarPriority = 300;
                newCapturedStatus.StackMultipleStatusesAsSingleIcon = true;
                newCapturedStatus.Visuals = Helper.CreateDefFromClone(paralysedStatusDef.Visuals, "{4305BE38-4408-4565-A440-A989C07467A0}", captureStatusName);
                newCapturedStatus.EventOnApply = paralysedStatusDef.EventOnApply;

                newCapturedStatus.Visuals.LargeIcon = newCaptureAbility.ViewElementDef.LargeIcon;
                newCapturedStatus.Visuals.SmallIcon = newCaptureAbility.ViewElementDef.SmallIcon;
                newCapturedStatus.Visuals.DisplayName1.LocalizationKey = "CAPTURE_STATUS_NAME";
                newCapturedStatus.Visuals.Description.LocalizationKey = "CAPTURE_STATUS_DESCRIPTION";

                ActorHasStatusEffectConditionDef actorIsParalyzedEffectCondition = TFTVCommonMethods.CreateNewStatusEffectCondition("{C9422E7A-B17E-4DFE-A2FD-D91311119B3B}", paralysedStatusDef, true);
                ActorHasStatusEffectConditionDef actorIsNotReadyForCaptureEffectCondition = TFTVCommonMethods.CreateNewStatusEffectCondition("{B89D9D5F-436E-47C8-8BF5-853E1721DFCF}", newCapturedStatus, false);

                newCaptureAbility.StatusDef = newCapturedStatus;
                newCaptureAbility.TargetApplicationConditions = new EffectConditionDef[] { actorIsParalyzedEffectCondition, actorIsNotReadyForCaptureEffectCondition };
                newCaptureAbility.UsableOnDisabledActor = true;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        private static void AdjustPandoranVolumes()
        {
            try
            {
                ClassTagDef crabTag = DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef");
                ClassTagDef fishTag = DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef");
                ClassTagDef sirenTag = DefCache.GetDef<ClassTagDef>("Siren_ClassTagDef");
                ClassTagDef chironTag = DefCache.GetDef<ClassTagDef>("Chiron_ClassTagDef");
                ClassTagDef acheronTag = DefCache.GetDef<ClassTagDef>("Acheron_ClassTagDef");
                ClassTagDef queenTag = DefCache.GetDef<ClassTagDef>("Queen_ClassTagDef");
                ClassTagDef wormTag = DefCache.GetDef<ClassTagDef>("Worm_ClassTagDef");
                ClassTagDef facehuggerTag = DefCache.GetDef<ClassTagDef>("Facehugger_ClassTagDef");
                ClassTagDef swarmerTag = DefCache.GetDef<ClassTagDef>("Swarmer_ClassTagDef");

                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>().Where(tcd => tcd.IsAlien))
                {
                    if (tacCharacterDef.Data.GameTags.Contains(swarmerTag) || tacCharacterDef.Data.GameTags.Contains(facehuggerTag) || tacCharacterDef.Data.GameTags.Contains(wormTag))
                    {
                        tacCharacterDef.Volume = 1;
                    }
                    else if (tacCharacterDef.Data.GameTags.Contains(sirenTag))
                    {
                        tacCharacterDef.Volume = 3;
                    }
                }

                DefCache.GetDef<PrisonFacilityComponentDef>("E_Prison [AlienContainment_PhoenixFacilityDef]").ContaimentCapacity = 25;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangeResourceRewardsForAutopsies()
        {
            try
            {
                DefCache.GetDef<ResearchDef>("PX_Alien_Mindfragger_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 75},
                    new ResourceUnit {Type = ResourceType.Mutagen, Value = 50}
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Poisonworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Fireworm_ResearchDef").Resources = new ResourcePack()
                {
                        new ResourceUnit { Type = ResourceType.Materials, Value = 25},
                     new ResourceUnit {Type = ResourceType.Mutagen, Value = 25}
                };

                DefCache.GetDef<ResearchDef>("PX_AlienCrabman_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 50 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Fishman_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 75 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 100 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Siren_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 125 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_Chiron_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 200 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 150 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Queen_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 300 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 250 }
                };

                DefCache.GetDef<ResearchDef>("PX_Alien_Swarmer_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 50 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_WormEgg_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_MindfraggerEgg_ResearchDef").Resources = new ResourcePack()
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 100 },
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_HatchingSentinel_ResearchDef").Resources = new ResourcePack()
                {
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef").Resources = new ResourcePack()
                {
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };
                DefCache.GetDef<ResearchDef>("PX_Alien_MistSentinel_ResearchDef").Resources = new ResourcePack()
                {
                     new ResourceUnit { Type = ResourceType.Mutagen, Value = 75 }
                };



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void ReducePromoSkins()
        {
            try
            {
                RedeemableCodeDef redeemableCodePriestTechnicianDef = DefCache.GetDef<RedeemableCodeDef>("CompleteEdition_RedeemableCodeDef");
                RedeemableCodeDef redeemableCodeDoomSlayer = DefCache.GetDef<RedeemableCodeDef>("HeadhunterSet_RedeemableCodeDef");
                RedeemableCodeDef redeemableCodeInfiltrator = DefCache.GetDef<RedeemableCodeDef>("Infiltrator_RedeemableCodeDef");
                RedeemableCodeDef redeemableCodeViking = DefCache.GetDef<RedeemableCodeDef>("Viking_RedeemableCodeDef");
                RedeemableCodeDef redeemableCodeWhiteSet = DefCache.GetDef<RedeemableCodeDef>("WhiteSet_RedeemableCodeDef");
                RedeemableCodeDef redeemableCodeNeoSet = DefCache.GetDef<RedeemableCodeDef>("NeonSet_RedeemableCodeDef");

                redeemableCodePriestTechnicianDef.GiftedItems.RemoveWhere(i => i.name.Contains("ALN") || i.name.Contains("MechArms"));

                List<RedeemableCodeDef> redeemableCodeDefs = new List<RedeemableCodeDef>()
                {
                redeemableCodeDoomSlayer, redeemableCodeInfiltrator, redeemableCodeViking, redeemableCodeWhiteSet, redeemableCodeNeoSet

                };

                foreach (RedeemableCodeDef redeemableCodeDef in redeemableCodeDefs)
                {
                    redeemableCodeDef.AutoRedeem = false;
                    redeemableCodeDef.RedeemableCode = "noskinforyou";
                    redeemableCodeDef.Allowed = false;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void CreateConvinceCivilianStatus()
        {
            try
            {


                Sprite talkSprite = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Warcry.png");
                Sprite cancelTalkSprite = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_Warcry_Cancel.png");

                //how hacking works:
                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective
                //
                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]
                //
                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef, which
                //1) gives the ability Hacking_Cancel_AbilityDef
                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]
                //
                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef

                //First create all the abilities

                //sources for new abilities
                InteractWithObjectAbilityDef startHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Start_AbilityDef");
                ApplyEffectAbilityDef cancelHackingDef = DefCache.GetDef<ApplyEffectAbilityDef>("Hacking_Cancel_AbilityDef");
                InteractWithObjectAbilityDef finishHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Finish_AbilityDef");

                startHackingDef.EndsTurn = true;

                //new abilities
                string convinceCivilianAbilityName = "ConvinceCivilianToJoinAbility";
                string cancelConvinceCivilianAbilityName = "CancelConvinceCivilianToJoinAbility";
                string finishConvinceCivilianAbilityName = "FinishConvinceCivilianToJoinAbility";

                InteractWithObjectAbilityDef newConvinceCivilianAbility = Helper.CreateDefFromClone(
                    startHackingDef,
                    "{8F36E864-4C97-4CFD-A13E-3CFA59A47A43}",
                    convinceCivilianAbilityName
                );
                InteractWithObjectAbilityDef newFinishConvinceCivilianAbility = Helper.CreateDefFromClone(
                    finishHackingDef,
                    "{29B0EEF2-1AB5-4011-9553-0C93799FB271}",
                    finishConvinceCivilianAbilityName
                );

                ApplyEffectAbilityDef newCancelConvinceCivilianAbility = Helper.CreateDefFromClone(
                    cancelHackingDef,
                    "{9CFD91E8-F98F-458C-B03C-0B92C72E8439}",
                    cancelConvinceCivilianAbilityName
                );

                newConvinceCivilianAbility.ViewElementDef = Helper.CreateDefFromClone(
                    startHackingDef.ViewElementDef,
                    "{F2E38123-B8CD-409F-95B2-23AE232472D8}",
                    convinceCivilianAbilityName
                );
                newFinishConvinceCivilianAbility.ViewElementDef = Helper.CreateDefFromClone(
                    finishHackingDef.ViewElementDef,
                    "{1FD84A13-321A-4BA8-880A-ABC787BA2636}",
                    finishConvinceCivilianAbilityName
                );
                newCancelConvinceCivilianAbility.ViewElementDef = Helper.CreateDefFromClone(
                    cancelHackingDef.ViewElementDef,
                    "{3489BA68-A8ED-49D5-A6C6-89B2C1444E94}",
                    cancelConvinceCivilianAbilityName
                );

                newConvinceCivilianAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_CONVINCE_ABILITY";
                newConvinceCivilianAbility.ViewElementDef.Description.LocalizationKey = "KEY_CONVINCE_ABILITY_DESCRIPTION";
                newConvinceCivilianAbility.ViewElementDef.LargeIcon = talkSprite;
                newConvinceCivilianAbility.ViewElementDef.SmallIcon = talkSprite;

                newCancelConvinceCivilianAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_CANCEL_CONVINCE_ABILITY";
                newCancelConvinceCivilianAbility.ViewElementDef.Description.LocalizationKey = "KEY_CANCEL_CONVINCE_ABILITY_DESCRIPTION";
                newCancelConvinceCivilianAbility.ViewElementDef.SmallIcon = cancelTalkSprite;
                newCancelConvinceCivilianAbility.ViewElementDef.LargeIcon = cancelTalkSprite;
                //Then create the statuses

                //sources for new statuses
                TacStatusDef activateHackableChannelingStatus = DefCache.GetDef<TacStatusDef>("ActiveHackableChannelingConsole_StatusDef"); //status on console, this is just a tag of sorts
                ActorBridgeStatusDef actorToConsoleBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ActorToConsoleBridge_StatusDef");

                AddAbilityStatusDef hackingStatusDef = DefCache.GetDef<AddAbilityStatusDef>("Hacking_Channeling_StatusDef"); //status on actor
                ActorBridgeStatusDef consoleToActorBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ConsoleToActorBridge_StatusDef");

                string statusOnObjectiveName = "ConvinceCivilianOnObjectiveStatus";
                string objectiveToActorBridgeStatusName = "ConvinceCivilianObjectiveToActorBridgeStatus";
                string actorToObjectiveBridgeStatusName = "ConvinceCivilianActorToObjectiveBridgeStatus";
                string statusOnActorName = "ConvinceCivilianOnActorStatus";

                TacStatusDef newStatusOnObjective = Helper.CreateDefFromClone(
                    activateHackableChannelingStatus,
                    "{546FF54E-1422-40F6-9C55-134E780F3E2C}",
                    statusOnObjectiveName
                );
                ActorBridgeStatusDef newActorToObjectiveStatus = Helper.CreateDefFromClone(
                    actorToConsoleBridgingStatusDef,
                    "{169D6712-9055-4D47-8585-5F832BBBFD47}",
                    actorToObjectiveBridgeStatusName
                );

                AddAbilityStatusDef newStatusOnActor = Helper.CreateDefFromClone(
                    hackingStatusDef,
                    "{998B630E-46C1-4BDB-BC4A-86F26B4651FB}",
                    statusOnActorName
                );
                ActorBridgeStatusDef newObjectiveToActorStatus = Helper.CreateDefFromClone(
                    consoleToActorBridgingStatusDef,
                    "{E090EF24-826D-442A-9BB8-D713EBE200A4}",
                    objectiveToActorBridgeStatusName
                );

                //need to create visuals for the new statuses

                newStatusOnObjective.Visuals = Helper.CreateDefFromClone(
                    activateHackableChannelingStatus.Visuals,
                    "{D9251544-3E29-4DC7-9963-74E445F46E7B}",
                    statusOnObjectiveName
                );
                newActorToObjectiveStatus.Visuals = Helper.CreateDefFromClone(
                    actorToConsoleBridgingStatusDef.Visuals,
                    "{FBA2DC77-2FFC-45CD-813C-2E244CA701CB}",
                    actorToObjectiveBridgeStatusName
                );
                newObjectiveToActorStatus.Visuals = Helper.CreateDefFromClone(
                    consoleToActorBridgingStatusDef.Visuals,
                    "{5756C65B-053B-4AFF-ADEF-514B980ECA02}",
                    objectiveToActorBridgeStatusName
                );
                newStatusOnActor.Visuals = Helper.CreateDefFromClone(
                    hackingStatusDef.Visuals,
                    "{AF4B1C45-70AD-49F9-85EA-6878B9CBB527}",
                    statusOnActorName
                );

                newActorToObjectiveStatus.Visuals.DisplayName1.LocalizationKey = "KEY_CONVINCE_STATUS";
                newActorToObjectiveStatus.Visuals.Description.LocalizationKey = "KEY_CONVINCE_STATUS_DESCRIPTION";
                newActorToObjectiveStatus.Visuals.SmallIcon = talkSprite;


                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective

                //Force Gate ability
                newConvinceCivilianAbility.ActiveInteractableConsoleStatusDef = newStatusOnObjective; //status on the objective
                newConvinceCivilianAbility.ActivatedConsoleStatusDef = newObjectiveToActorStatus; //bridge status from objective to Actor
                                                                                                  //we don't change newForceGateAbility.StatusesBlockingActivation because we keep using Console_ActivatedStatusDef unchanged, for now

                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef

                //so let's pair the new bridging statuses
                newActorToObjectiveStatus.PairedStatusDef = newObjectiveToActorStatus;
                newObjectiveToActorStatus.PairedStatusDef = newActorToObjectiveStatus;

                //and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]

                TacticalEventDef newEventOnApplyConvinceCvilian = Helper.CreateDefFromClone(
                    DefCache.GetDef<TacticalEventDef>("E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]"),
                    "{5C6A2F85-5F7F-48C0-AA14-A3D9EC435198}",
                    statusOnActorName
                );

                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef

                TacStatusEffectDef newEffectToApplyActiveStatusOnActor = Helper.CreateDefFromClone(
                    DefCache.GetDef<TacStatusEffectDef>("E_ApplyHackingChannelingStatus [Hacking_ActorToConsoleBridge_StatusDef]"),
                    "{E94A91FB-F8ED-4C09-9012-91F6B20549DA}",
                    statusOnObjectiveName
                );
                newEffectToApplyActiveStatusOnActor.StatusDef = newStatusOnActor;
                newEventOnApplyConvinceCvilian.EffectData.EffectDefs = new EffectDef[] { newEffectToApplyActiveStatusOnActor };

                newActorToObjectiveStatus.EventOnApply = newEventOnApplyConvinceCvilian;

                //which
                //1) gives the ability Hacking_Cancel_AbilityDef
                newStatusOnActor.AbilityDef = newCancelConvinceCivilianAbility; //the status gives the actor the ability to cancel the hacking/forcing the gate
                                                                                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]

                //we need to create a new event for when the effect is unapplied, to apply 2 new effects:
                //1) finish executing the ability (original E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]),
                //2) remove bridge effect from ActorToObjective, and we don't change the original because the effect name hasn't been changed E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef]
                TacticalEventDef newEventOnUnApplyConvinceCivilian = Helper.CreateDefFromClone(
                    DefCache.GetDef<TacticalEventDef>("E_EventOnUnapply [Hacking_Channeling_StatusDef]"),
                    "{A85B34A6-73F9-4500-BA48-2B9C463C838D}",
                    statusOnActorName
                );
                ActivateAbilityEffectDef newActivateFinishConvincingCivilianEffect = Helper.CreateDefFromClone(
                    DefCache.GetDef<ActivateAbilityEffectDef>("E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]"),
                    "{5F617C39-F133-43BB-8294-80205964DA49}",
                    statusOnActorName
                );
                newEventOnUnApplyConvinceCivilian.EffectData.EffectDefs = new EffectDef[] { newActivateFinishConvincingCivilianEffect, newEventOnUnApplyConvinceCivilian.EffectData.EffectDefs[1] };

                newStatusOnActor.EventOnUnapply = newEventOnUnApplyConvinceCivilian;

                //we start by changing the ability our clone is pointing at
                newActivateFinishConvincingCivilianEffect.AbilityDef = newFinishConvinceCivilianAbility;

                //but it has also 2 application conditions:
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef], we can probably keep it as it is
                //2) "E_StatusElapsedInTurns", and this one we have to replace because it is pointing at ActorToConsole bridge, and we want it pointing at our new ActorToObjective bridge
                MinStatusDurationInTurnsEffectConditionDef newTurnDurationCondition = Helper.CreateDefFromClone(
                    DefCache.GetDef<MinStatusDurationInTurnsEffectConditionDef>("E_StatusElapsedInTurns"),
                    "{258124D4-C85A-4084-840E-5015CD100123}",
                    newFinishConvinceCivilianAbility + "ElapsedTurnsCondition"
                );

                newTurnDurationCondition.TacStatusDef = newActorToObjectiveStatus;
                newActivateFinishConvincingCivilianEffect.ApplicationConditions = new EffectConditionDef[] { newActivateFinishConvincingCivilianEffect.ApplicationConditions[0], newTurnDurationCondition };

                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef

                //Force Gate Cancel ability shouldn't require changing, as the effect in RemoveActorHackingStatuses_EffectDef is still called "HackingChannel"

                //Force Gate Finish ability activatedConsoleStatus is the same, for now, Console_ActivatedStatusDef,  but we need to change ActiveInteractableConsoleStatusDef to the new objective to actor Bridge
                newFinishConvinceCivilianAbility.ActiveInteractableConsoleStatusDef = newObjectiveToActorStatus;

                //We need to add the forcegateability to the actor template
                //and apparently the finishgateability too
                TacticalActorDef soldierActorDef = DefCache.GetDef<TacticalActorDef>("Soldier_ActorDef");

                List<AbilityDef> abilityDefs = new List<AbilityDef>(soldierActorDef.Abilities) { newConvinceCivilianAbility, newFinishConvinceCivilianAbility };
                soldierActorDef.Abilities = abilityDefs.ToArray();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ModifyRescueCiviliansMissions()
        {
            try
            {
                PPFactionDef neutralFaction = DefCache.GetDef<PPFactionDef>("Neutral_FactionDef");

                //  CustomMissionTypeDef rescueHelenaMisson = DefCache.GetDef<CustomMissionTypeDef>("StoryLE0_CustomMissionTypeDef");

                CustomMissionTypeDef rescueSparkMisson = DefCache.GetDef<CustomMissionTypeDef>("Bcr5_CustomMissionTypeDef");
                CustomMissionTypeDef rescueFelipeMisson = DefCache.GetDef<CustomMissionTypeDef>("Bcr7_CustomMissionTypeDef");
                CustomMissionTypeDef rescueHelenaMisson = DefCache.GetDef<CustomMissionTypeDef>("StoryLE0_CustomMissionTypeDef");

                CustomMissionTypeDef rescueCalendarMisson = DefCache.GetDef<CustomMissionTypeDef>("Bcr1_CustomMissionTypeDef");
                //  TacMissionTypeParticipantData sourceEnvironmentParcipantData = rescueHelenaMisson.ParticipantsData[2];

                TacMissionTypeParticipantData newEnvironmentParcipantData = new TacMissionTypeParticipantData
                {
                    ParticipantKind = TacMissionParticipant.Environment,
                    ActorDeployParams = new List<MissionDeployParams>() { },
                    FactionDef = neutralFaction,
                    GenerateGeoCharacters = false,
                    PredeterminedFactionEffects = new EffectDef[] { },
                    ReinforcementsDeploymentPart = new RangeData() { Min = 0, Max = 0 },
                    ReinforcementsTurns = new RangeDataInt() { Min = 0, Max = 0 },
                    InfiniteReinforcements = false,
                    UniqueUnits = new TacMissionTypeParticipantData.UniqueChatarcterBind[] { },
                    DeploymentRule = new TacMissionTypeParticipantData.DeploymentRuleData()
                    {
                        IncludeNearbyFactionSites = false,
                        DeploymentPoints = 0,
                        MinDeployment = 0,
                        MaxDeployment = 1000000,
                        DeploymentPercentage = 100,
                        DeploymentType = 0,
                        OverrideUnitDeployment = new List<TacMissionTypeParticipantData.DeploymentRuleData.UnitDeploymentOverride>() { }
                    }
                };

                rescueSparkMisson.ParticipantsData.Add(newEnvironmentParcipantData);
                rescueFelipeMisson.ParticipantsData.Add(newEnvironmentParcipantData);
                rescueCalendarMisson.ParticipantsData.Add(newEnvironmentParcipantData);

                CreateConvinceCivilianStatus();

                string nameTalkingPointConsoleTag = "TalkingPointConsoleTag";

                StructuralTargetTypeTagDef interactableConsoleTag = Helper.CreateDefFromClone
                    (DefCache.GetDef<StructuralTargetTypeTagDef>("InteractableConsole_StructuralTargetTypeTagDef"),
                    "{E39FE24B-468B-46EA-B1D1-65908A4F550F}", nameTalkingPointConsoleTag);


                rescueSparkMisson.CustomObjectives[1] = CreateNewActivateConsoleObjective("ConvinceCivilianObjectiveSpark", "{75C311B3-B34C-4460-BB8C-95D1963E6F90}", "{EADCDE10-1B4F-4956-AC44-42FDF959F069}", "KEY_OBJECTIVE_CONVINCE_SPARKS");
                rescueFelipeMisson.CustomObjectives[1] = CreateNewActivateConsoleObjective("ConvinceCivilianObjectiveFelipe", "{12780334-2607-48DF-8F93-16B1665078F0}", "{D19D79E5-EA2F-44C7-B05F-F19D9B58A462}", "KEY_OBJECTIVE_CONVINCE_FELIPE");
                rescueCalendarMisson.CustomObjectives[1] = CreateNewActivateConsoleObjective("ConvinceCivilianObjectiveCalendar", "{84DFB63C-A79A-49DA-94DE-5C401FE2B7FD}", "{A663C9EF-2A06-457F-999D-B80657863503}", "KEY_OBJECTIVE_CONVINCE_CALENDAR");
                rescueHelenaMisson.CustomObjectives[1] = CreateNewActivateConsoleObjective("ConvinceCivilianObjectiveHelena", "{AC2A9633-6D6F-4261-8315-06899D4A47BF}", "{6E492142-7500-4C71-B6E2-0B16BF2C4AE6}", "KEY_OBJECTIVE_CONVINCE_HELENA");

                rescueFelipeMisson.ParticipantsRelations = rescueFelipeMisson.ParticipantsRelations.AddToArray(new MutualParticipantsRelations()
                { FirstParticipant = TacMissionParticipant.Player, SecondParticipant = TacMissionParticipant.Environment, MutualRelation = FactionRelation.Friend });

                rescueSparkMisson.ParticipantsRelations = rescueSparkMisson.ParticipantsRelations.AddToArray(new MutualParticipantsRelations()
                { FirstParticipant = TacMissionParticipant.Player, SecondParticipant = TacMissionParticipant.Environment, MutualRelation = FactionRelation.Friend });

                rescueCalendarMisson.ParticipantsRelations = rescueCalendarMisson.ParticipantsRelations.AddToArray(new MutualParticipantsRelations()
                { FirstParticipant = TacMissionParticipant.Player, SecondParticipant = TacMissionParticipant.Environment, MutualRelation = FactionRelation.Friend });


                //Add inifinite reinforcements to Helena
                rescueHelenaMisson.ParticipantsData[1].InfiniteReinforcements = true;
                rescueHelenaMisson.ParticipantsData[1].ReinforcementsTurns = new RangeDataInt() { Min = 0, Max = 1 };
                rescueHelenaMisson.ParticipantsData[1].ReinforcementsDeploymentPart = new RangeData() { Min = 0.1f, Max = 0.1f };
                rescueHelenaMisson.DontRecoverItems = true;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static WipeEnemyFactionObjectiveDef CreateNewActivateConsoleObjective(string name, string gUID, string gUID1, string key)
        {
            try
            {
                StructuralTargetTypeTagDef interactableConsoleTag = DefCache.GetDef<StructuralTargetTypeTagDef>("TalkingPointConsoleTag");

                WipeEnemyFactionObjectiveDef sourceWipeEnemyFactionObjective = DefCache.GetDef<WipeEnemyFactionObjectiveDef>("WipeEnemy_CustomMissionObjective");
                WipeEnemyFactionObjectiveDef newDummyObjective = Helper.CreateDefFromClone(sourceWipeEnemyFactionObjective, gUID, "DummyObjective");
                newDummyObjective.MissionObjectiveData.ExperienceReward = 0;
                newDummyObjective.IsUiHidden = true;
                newDummyObjective.IsDefeatObjective = false;
                newDummyObjective.IsVictoryObjective = false;

                ActivateConsoleFactionObjectiveDef sourceActivateFactionObjective = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("StealResearch_HackConsole_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef newObjective = Helper.CreateDefFromClone(sourceActivateFactionObjective, name, gUID1);
                newObjective.ObjectiveData.ActiveInteractables = -1;
                newObjective.ObjectiveData.InteractablesToActivate = -1;
                newObjective.ObjectiveData.InteractableTagDef = interactableConsoleTag;

                newObjective.IsDefeatObjective = true;//false; TESTING
                newObjective.MissionObjectiveData.Summary.LocalizationKey = key;
                newObjective.MissionObjectiveData.Description.LocalizationKey = key;

                TacStatusDef activateHackableChannelingStatus = DefCache.GetDef<TacStatusDef>("ConvinceCivilianOnObjectiveStatus"); //status on console, this is just a tag of sorts

                newObjective.ObjectiveData.InteractableStatusDef = activateHackableChannelingStatus;

                newDummyObjective.NextOnSuccess = new FactionObjectiveDef[] { newObjective };
                newDummyObjective.NextOnFail = new FactionObjectiveDef[] { newObjective };
                //  newObjective.ObjectiveData.ActivatedInteractableStatusDef = 


                return newDummyObjective;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreateAcidDisabledStatus()
        {
            try
            {

                SlotStateStatusDef disabledSource = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicSlot_StatusDef");
                string name = "DisabledElectronicSlotFromAcid_StatusDef";
                SlotStateStatusDef newDisabled = Helper.CreateDefFromClone(disabledSource, "{1C5E47B5-6CE1-4A41-A711-07652506A901}", name);
                newDisabled.DurationTurns = 0;
                newDisabled.Visuals = Helper.CreateDefFromClone(disabledSource.Visuals, "{606C38AD-8AA7-4D7E-B031-733BCB7D2C42}", name);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static void ChangeFireNadeCostAndDamage()
        {
            try
            {
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");

                //change fire damage to 30 from 40
                foreach (DamageKeywordPair damageKeywordPair in fireNade.DamagePayload.DamageKeywords)
                {
                    if (damageKeywordPair.DamageKeywordDef == Shared.SharedDamageKeywords.BurningKeyword)
                    {
                        damageKeywordPair.Value = 30;
                    }
                }

                fireNade.ManufactureTech = 5;
                fireNade.ManufactureMaterials = 20;

                WeaponDef healNade = DefCache.GetDef<WeaponDef>("PX_HealGrenade_WeaponDef");

                healNade.ManufactureTech = 6;
                healNade.ManufactureMaterials = 28;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }






        private static void FixBionic3ResearchNotGivingAccessToFacility()
        {
            try
            {
                ResearchDef researchDef = DefCache.GetDef<ResearchDef>("SYN_Bionics3_ResearchDef");
                FacilityResearchRewardDef facilityRewardDef = DefCache.GetDef<FacilityResearchRewardDef>("NJ_Bionics2_ResearchDef_FacilityResearchRewardDef_0");
                List<ResearchRewardDef> rewards = new List<ResearchRewardDef>(researchDef.Unlocks) { facilityRewardDef };
                researchDef.Unlocks = rewards.ToArray();

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangePalaceMissions()
        {
            try
            {
                CreateForceYuggothianReceptacleGatesAbilityAndStatus();
                CreateNewStatusOnDisablingYugothianEyes();
                AdjustYuggothianEntity();
                ChangePalaceMissionDefs();
                CreateCharactersForPalaceMission();
                CreateReinforcementStatuses();
                ChangeIconForMarkOfTheVoid();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangeIconForMarkOfTheVoid()
        {
            try
            {
                TacticalAbilityViewElementDef markOfTheVoidViewElement = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Yuggothian_StatusAttack_AbilityDef]");
                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_MoV.png");
                markOfTheVoidViewElement.LargeIcon = icon;
                markOfTheVoidViewElement.SmallIcon = icon;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void CreateNewStatusOnDisablingYugothianEyes()
        {
            try
            {

                //Status to be applied when YR is disrupted, causing shields to be lowered.

                string statusName = "YR_Disrupted";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatusDef = Helper.CreateDefFromClone(
                    source,
                    "{6DA5667A-5890-4746-AA2A-182EA82D0E4C}",
                    statusName);
                newStatusDef.EffectName = statusName;
                newStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newStatusDef.VisibleOnPassiveBar = true;
                newStatusDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatusDef.DurationTurns = 2;

                newStatusDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatusDef.VisibleOnPassiveBar = false;


                newStatusDef.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    "{42FF81F8-B4D7-494D-A651-010DD8807EFF}",
                    statusName);
                newStatusDef.Multiplier = 1;
                newStatusDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };

                newStatusDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("cracked-shield.png");
                newStatusDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("cracked-shield.png");

                newStatusDef.Visuals.DisplayName1.LocalizationKey = "YR_DEFENSE_BROKEN_NAME";
                newStatusDef.Visuals.Description.LocalizationKey = "YR_DEFENSE_BROKEN_DESCRIPTION";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }


        private static void CreateForceYuggothianReceptacleGatesAbilityAndStatus()
        {
            try
            {


                //how hacking works:
                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective
                //
                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]
                //
                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef, which
                //1) gives the ability Hacking_Cancel_AbilityDef
                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]
                //
                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef


                //First create all the abilities

                //sources for new abilities
                InteractWithObjectAbilityDef startHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Start_AbilityDef");
                ApplyEffectAbilityDef cancelHackingDef = DefCache.GetDef<ApplyEffectAbilityDef>("Hacking_Cancel_AbilityDef");
                InteractWithObjectAbilityDef finishHackingDef = DefCache.GetDef<InteractWithObjectAbilityDef>("Hacking_Finish_AbilityDef");

                startHackingDef.EndsTurn = true;

                //new abilities
                string forceGateAbilityName = "ForceYuggothianGateAbility";
                string cancelGateAbilityName = "CancelYuggothianGateAbility";
                string finishGateAbilityName = "FinishYuggothianGateAbility";

                InteractWithObjectAbilityDef newForceGateAbility = Helper.CreateDefFromClone
                    (startHackingDef,
                    "{AB869306-7AA4-417F-93E4-8A6CE63FFE45}", forceGateAbilityName);
                InteractWithObjectAbilityDef newFinishGateAbility = Helper.CreateDefFromClone(
                    finishHackingDef,
                    "{3E702D44-02EE-4BCC-9943-466441FAD3AF}", finishGateAbilityName);

                ApplyEffectAbilityDef newCancelGateAbility = Helper.CreateDefFromClone(
                    cancelHackingDef,
                    "{A020E779-FA4C-4D44-AA32-AF3D424B8324}", cancelGateAbilityName);


                newForceGateAbility.ViewElementDef = Helper.CreateDefFromClone(startHackingDef.ViewElementDef, "{BEAD489E-9B4D-4DF9-9B76-BCE653FF9F6D}", forceGateAbilityName);
                newFinishGateAbility.ViewElementDef = Helper.CreateDefFromClone(finishHackingDef.ViewElementDef, "{BE486198-CB7E-47D9-8041-64F747D9548A}", finishGateAbilityName);
                newCancelGateAbility.ViewElementDef = Helper.CreateDefFromClone(cancelHackingDef.ViewElementDef, "{{3309F86B-45F2-4C7A-A639-F12E1B17B5FD}}", cancelGateAbilityName);

                newForceGateAbility.ViewElementDef.DisplayName1.LocalizationKey = "FORCE_GATE_ABILITY";
                newForceGateAbility.ViewElementDef.Description.LocalizationKey = "FORCE_GATE_ABILITY_DESCRIPTION";
                newCancelGateAbility.ViewElementDef.DisplayName1.LocalizationKey = "CANCEL_FORCE_GATE_ABILITY";
                newCancelGateAbility.ViewElementDef.Description.LocalizationKey = "CANCEL_FORCE_GATE_ABILITY_DESCRIPTION";
                //  TFTVLogger.Always($"");

                //Then create the statuses

                //sources for new statuses 
                TacStatusDef activateHackableChannelingStatus = DefCache.GetDef<TacStatusDef>("ActiveHackableChannelingConsole_StatusDef"); //status on console, this is just a tag of sorts
                ActorBridgeStatusDef actorToConsoleBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ActorToConsoleBridge_StatusDef");

                AddAbilityStatusDef hackingStatusDef = DefCache.GetDef<AddAbilityStatusDef>("Hacking_Channeling_StatusDef"); //status on actor
                ActorBridgeStatusDef consoleToActorBridgingStatusDef = DefCache.GetDef<ActorBridgeStatusDef>("Hacking_ConsoleToActorBridge_StatusDef");


                string statusOnObjectiveName = "ForceGateOnObjectiveStatus";
                string objectiveToActorBridgeStatusName = "ObjectiveToActorBridgeStatus";
                string actorToObjectiveBridgeStatusName = "ActorToObjectiveBridgeStatus";
                string statusOnActorName = "ForcingGateOnActorStatus";

                TacStatusDef newStatusOnObjective = Helper.CreateDefFromClone(activateHackableChannelingStatus, "{6A31787B-14AD-4143-AD57-C3AF04AF1E2B}", statusOnObjectiveName);
                ActorBridgeStatusDef newActorToObjectiveStatus = Helper.CreateDefFromClone(actorToConsoleBridgingStatusDef, "{D288280F-603D-4556-804A-9B8B63646C96}", actorToObjectiveBridgeStatusName);
                newActorToObjectiveStatus.SingleInstance = true;

                AddAbilityStatusDef newStatusOnActor = Helper.CreateDefFromClone(hackingStatusDef, "{23143337-5CF8-4AE8-8B7D-B5D0650CD629}", statusOnActorName);
                ActorBridgeStatusDef newObjectiveToActorStatus = Helper.CreateDefFromClone(consoleToActorBridgingStatusDef, "{897F88CC-2BB0-4E04-A0CD-AFF62463C199}", objectiveToActorBridgeStatusName);
                newObjectiveToActorStatus.SingleInstance = true;

                //need to create visuals for the new statuses

                newStatusOnObjective.Visuals = Helper.CreateDefFromClone(activateHackableChannelingStatus.Visuals, "{6934146B-0F91-4C34-8B58-EB115748B915}", statusOnObjectiveName);
                newActorToObjectiveStatus.Visuals = Helper.CreateDefFromClone(actorToConsoleBridgingStatusDef.Visuals, "{6B002C8D-F28D-4A61-83BB-81E06BFF51FE}", actorToObjectiveBridgeStatusName);
                newObjectiveToActorStatus.Visuals = Helper.CreateDefFromClone(consoleToActorBridgingStatusDef.Visuals, "{75E47B2A-6598-4635-882C-C763681E2C6D}", objectiveToActorBridgeStatusName);
                newStatusOnActor.Visuals = Helper.CreateDefFromClone(hackingStatusDef.Visuals, "{A315B3DF-7F7C-4887-B875-007EB58DB61F}", statusOnActorName);
                newStatusOnActor.SingleInstance = true;
                //   TFTVLogger.Always($"2");

                newActorToObjectiveStatus.Visuals.DisplayName1.LocalizationKey = "FORCE_GATE_STATUS";
                newActorToObjectiveStatus.Visuals.Description.LocalizationKey = "FORCE_GATE_STATUS_DESCRIPTION";

                // TFTVLogger.Always($"3");
                //Hacking_Start_AbilityDef is conditioned on Objective not having ConsoleActivated_StatusDef and it applies
                //1) ActiveHackableChannelingConsole_StatusDef to the Console (this is just a tag)
                //2) Hacking_ConsoleToActorBridge_StatusDef to the Objective


                //Force Gate ability
                newForceGateAbility.ActiveInteractableConsoleStatusDef = newStatusOnObjective; //status on the objective
                newForceGateAbility.ActivatedConsoleStatusDef = newObjectiveToActorStatus; //bridge status from objective to Actor
                                                                                           //we don't change newForceGateAbility.StatusesBlockingActivation because we keep using Console_ActivatedStatusDef unchanged, for now

                //Hacking_ActorToConsoleBridge_StatusDef is paired with Hacking_ConsoleToActorBridge_StatusDef
                //
                //
                //so let's pair the new bridging statuses
                newActorToObjectiveStatus.PairedStatusDef = newObjectiveToActorStatus;
                newObjectiveToActorStatus.PairedStatusDef = newActorToObjectiveStatus;


                //and it triggers an event when it is applied
                //This is event is E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]
                //
                TacticalEventDef newEventOnApplyForcingGate = Helper.CreateDefFromClone(DefCache.GetDef<TacticalEventDef>("E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef]"), "{AD224294-3CFB-4003-90A5-5BE83755D171}", statusOnActorName);

                //E_EventOnApply [Hacking_ActorToConsoleBridge_StatusDef], provided that a game is not being loaded, applies
                //Hacking_Channeling_StatusDef,
                //
                TacStatusEffectDef newEffectToApplyActiveStatusOnActor = Helper.CreateDefFromClone(DefCache.GetDef<TacStatusEffectDef>("E_ApplyHackingChannelingStatus [Hacking_ActorToConsoleBridge_StatusDef]"), "{133AEE94-FAB8-44BF-B796-1A6A4A367745}", statusOnObjectiveName);
                newEffectToApplyActiveStatusOnActor.StatusDef = newStatusOnActor;
                newEventOnApplyForcingGate.EffectData.EffectDefs = new EffectDef[] { newEffectToApplyActiveStatusOnActor };

                newActorToObjectiveStatus.EventOnApply = newEventOnApplyForcingGate;
                //
                //
                //which
                //1) gives the ability Hacking_Cancel_AbilityDef
                newStatusOnActor.AbilityDef = newCancelGateAbility; //the status gives the actor the ability to cancel the hacking/forcing the gate
                //2) on UnApply triggers the event E_EventOnUnapply [Hacking_Channeling_StatusDef]


                //we need to create a new event for when the effect is unapplied, to apply 2 new effects:
                //1) finish executing the ability (original E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]),
                //2) remove bridge effect from ActorToObjective, and we don't change the original because the effect name hasn't been changed E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef]
                TacticalEventDef newEventOnUnApplyForcingGate = Helper.CreateDefFromClone(DefCache.GetDef<TacticalEventDef>("E_EventOnUnapply [Hacking_Channeling_StatusDef]"), "{1739F6E1-21C2-45B5-9944-0B1A042DD9C4}", statusOnActorName);
                ActivateAbilityEffectDef newActivateFinishForcingGateEffect = Helper.CreateDefFromClone(DefCache.GetDef<ActivateAbilityEffectDef>("E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]"), "{CA7D54EE-2086-4FF7-9F5C-EBE66A92D67A}", statusOnActorName);
                newEventOnUnApplyForcingGate.EffectData.EffectDefs = new EffectDef[] { newActivateFinishForcingGateEffect, newEventOnUnApplyForcingGate.EffectData.EffectDefs[1] };

                newStatusOnActor.EventOnUnapply = newEventOnUnApplyForcingGate;

                //we start by changing the ability our clone is pointing at
                newActivateFinishForcingGateEffect.AbilityDef = newFinishGateAbility;

                //but it has also 2 application conditions:
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef], we can probably keep it as it is
                //2) "E_StatusElapsedInTurns", and this one we have to replace because it is pointing at ActorToConsole bridge, and we want it pointing at our new ActorToObjective bridge
                MinStatusDurationInTurnsEffectConditionDef newTurnDurationCondition = Helper.CreateDefFromClone
                    (DefCache.GetDef<MinStatusDurationInTurnsEffectConditionDef>("E_StatusElapsedInTurns"), "{9D190470-2C5A-45BD-B95D-2E96A8723E49}", newFinishGateAbility + "ElapsedTurnsCondition");

                newTurnDurationCondition.TacStatusDef = newActorToObjectiveStatus;
                newActivateFinishForcingGateEffect.ApplicationConditions = new EffectConditionDef[] { newActivateFinishForcingGateEffect.ApplicationConditions[0], newTurnDurationCondition };

                //
                //Hacking_Cancel_AbilityDef has the effect RemoveActorHackingStatuses_EffectDef, which removes status with the effectname HackingChannel (Hacking_Channeling_StatusDef)
                //
                //E_EventOnUnapply [Hacking_Channeling_StatusDef] triggers 2 effects:
                //1) E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef]
                //2) E_RemoveBridgeStatusEffect [Hacking_Channeling_StatusDef], which removes the status with the effectname ActorToConsoleBridge (Hacking_ActorToConsoleBridge_StatusDef)
                //
                //E_ActivateHackingFinishAbility [Hacking_Channeling_StatusDef], provided that 
                //1) E_ActorIsAlive [Hacking_Channeling_StatusDef]
                //2) E_StatusElapsedInTurns for status Hacking_ActorToConsoleBridge_StatusDef is 2
                //will activate the ability Hacking_Finish_AbilityDef, which
                //1) looks at the status Hacking_ConsoleToActorBridge_StatusDef
                //2) and triggers the status ConsoleActivated_StatusDef


                //Force Gate Cancel ability shouldn't require changing, as the effect in RemoveActorHackingStatuses_EffectDef is still called "HackingChannel"

                //Force Gate Finish ability activatedConsoleStatus is the same, for now, Console_ActivatedStatusDef,  but we need to change ActiveInteractableConsoleStatusDef to the new objective to actor Bridge
                newFinishGateAbility.ActiveInteractableConsoleStatusDef = newObjectiveToActorStatus;




                //We need to add the forcegateability to the actor template
                //and apparently the finishgateability too
                TacticalActorDef soldierActorDef = DefCache.GetDef<TacticalActorDef>("Soldier_ActorDef");

                List<AbilityDef> abilityDefs = new List<AbilityDef>(soldierActorDef.Abilities) { newForceGateAbility, newFinishGateAbility };
                soldierActorDef.Abilities = abilityDefs.ToArray();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        private static void RestrictCanBeRecruitedIntoPhoenix()
        {
            try
            {
                TriggerAbilityZoneOfControlStatusDef canBeRecruited1x1 = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_1x1_StatusDef");
                TriggerAbilityZoneOfControlStatusDef canBeRecruited3x3 = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_3x3_StatusDef");

                TriggerAbilityZoneOfControlStatusDef canBeRecruited3x3_disabled = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_3x3_Disabled_StatusDef");
                TriggerAbilityZoneOfControlStatusDef canBeRecruited1x1_disabled = DefCache.GetDef<TriggerAbilityZoneOfControlStatusDef>("CanBeRecruitedIntoPhoenix_1x1_Disabled_StatusDef");

                List<EffectConditionDef> effectConditionDefs1x1 = canBeRecruited1x1.TriggerConditions.ToList();
                List<EffectConditionDef> effectConditionDefs3x3 = canBeRecruited3x3.TriggerConditions.ToList();
                List<EffectConditionDef> effectConditionDefs3x3_disabled = canBeRecruited3x3_disabled.TriggerConditions.ToList();
                List<EffectConditionDef> effectConditionDefs1x1_disabled = canBeRecruited1x1_disabled.TriggerConditions.ToList();
                ActorHasTagEffectConditionDef source = DefCache.GetDef<ActorHasTagEffectConditionDef>("HasCombatantTag_ApplicationCondition");
                ActorHasTagEffectConditionDef notDroneCondition = Helper.CreateDefFromClone(source, "{87709AA5-4B10-44A7-9810-1E0502726A48}", "NotADroneEffectConditionDef");

                notDroneCondition.HasTag = false;
                notDroneCondition.GameTag = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                ActorHasTagEffectConditionDef notAlienCondition = Helper.CreateDefFromClone(source, "{5EDDD493-F5BF-4942-BD12-594B76CFE0EF}", "NotAlienEffectConditionDef");
                notAlienCondition.HasTag = false;
                notAlienCondition.GameTag = DefCache.GetDef<GameTagDef>("Alien_RaceTagDef");

                effectConditionDefs1x1.Add(notAlienCondition);
                effectConditionDefs1x1.Add(notDroneCondition);

                effectConditionDefs3x3.Add(notAlienCondition);
                effectConditionDefs3x3.Add(notDroneCondition);

                effectConditionDefs3x3_disabled.Add(notAlienCondition);
                effectConditionDefs3x3_disabled.Add(notDroneCondition);

                effectConditionDefs1x1_disabled.Add(notAlienCondition);
                effectConditionDefs1x1_disabled.Add(notDroneCondition);

                canBeRecruited1x1.TriggerConditions = effectConditionDefs1x1.ToArray();
                canBeRecruited3x3.TriggerConditions = effectConditionDefs3x3.ToArray();
                canBeRecruited3x3_disabled.TriggerConditions = effectConditionDefs3x3_disabled.ToArray();
                canBeRecruited1x1_disabled.TriggerConditions = effectConditionDefs1x1_disabled.ToArray();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void AdjustYuggothianEntity()
        {
            try
            {

                TacticalPerceptionDef yugothianPercpetion = DefCache.GetDef<TacticalPerceptionDef>("Yugothian_PerceptionDef");
                yugothianPercpetion.SizeSpottingMultiplier = 1.0f;
                // yugothianPercpetion.PermanentReveal = false;
                yugothianPercpetion.AlwaysVisible = false;

                DefCache.GetDef<TacticalActorYuggothDef>("Yugothian_ActorDef").EnduranceToHealthMultiplier = 100;

                DefCache.GetDef<TacticalItemDef>("Yugothian_Head_BodyPartDef").HitPoints = 900000;
                DefCache.GetDef<TacticalItemDef>("Yugothian_Roots_BodyPartDef").HitPoints = 900000;

                DefCache.GetDef<SpawnActorAbilityDef>("DeployInjectorBomb2_AbilityDef");

                DefCache.GetDef<TacCharacterDef>("YugothianMain_TacCharacterDef").Data.Will = 500;

                // ActionCamDef deployCam = DefCache.GetDef<ActionCamDef>("DeployInjectorBombCamDef");

                // deployCam.PositionOffset.x = -5;
                //  DefCache.GetDef<CameraAnyFilterDef>("E_AnyDeployInjectorBombAbilityFilter [NoDieCamerasTacticalCameraDirectorDef]").Conditions.Clear();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }

        private static void CreateReinforcementStatuses()
        {
            try
            {
                StatsModifyEffectDef modifyAPEffect = DefCache.GetDef<StatsModifyEffectDef>("ModifyAP_EffectDef");
                modifyAPEffect.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.2f,
                    StatName = "ActionPoints"

                }

                };

                string reinforcementStatusUnder1AP = "ReinforcementStatusUnder1AP";
                string reinforcementStatus1AP = "ReinforcementStatus1AP";
                string reinforcementStatusUnder2AP = "ReinforcementStatusUnder2AP";


                StatsModifyEffectDef newEffect1AP = Helper.CreateDefFromClone(modifyAPEffect, "{A52F2DD5-92F4-4D31-B4E1-32454D67435A}", reinforcementStatus1AP);
                StatsModifyEffectDef newEffectUnder2AP = Helper.CreateDefFromClone(modifyAPEffect, "{D6090754-5A2C-45E3-888D-60E825CB619F}", reinforcementStatusUnder2AP);
                newEffect1AP.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.25f,
                    StatName = "ActionPoints"

                }

                };

                newEffectUnder2AP.StatModifications = new List<StatModification>()
                {new StatModification()
                {
                    Modification = StatModificationType.MultiplyRestrictedToBounds,
                    Value = 0.4f,
                    StatName = "ActionPoints"

                }

                };
                DelayedEffectStatusDef source = DefCache.GetDef<DelayedEffectStatusDef>("E_Status [WarCry_AbilityDef]");

                DelayedEffectStatusDef newReinforcementStatusUnder1APStatus = Helper.CreateDefFromClone(source, "{60D48AD5-CCC5-4D99-9B59-C5B7041B5818}", reinforcementStatusUnder1AP);

                TacticalAbilityViewElementDef viewElementSource = DefCache.GetDef<TacticalAbilityViewElementDef>("E_ViewElement [Acheron_CallReinforcements_AbilityDef]");

                newReinforcementStatusUnder1APStatus.EffectName = "RecentReinforcementUnder1AP";
                newReinforcementStatusUnder1APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{4E808CF0-7E73-4CC9-B642-E8CEFE663FA6}", reinforcementStatusUnder1AP);
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatusUnder1APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatusUnder1APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatusUnder1APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatusUnder1APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatusUnder1APStatus.EffectDef = modifyAPEffect;
                newReinforcementStatusUnder1APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatusUnder1APStatus.ShowNotification = false;
                newReinforcementStatusUnder1APStatus.ShowNotificationOnUnApply = false;


                DelayedEffectStatusDef newReinforcementStatus1APStatus = Helper.CreateDefFromClone(source, "{D32F42E3-97F5-4EE4-BDAC-36A07767593B}", reinforcementStatus1AP);

                newReinforcementStatus1APStatus.EffectName = "RecentReinforcement1AP";
                newReinforcementStatus1APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{49715088-BD6C-4104-A7D0-A08796A517DD}", reinforcementStatus1AP);
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatus1APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatus1APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatus1APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatus1APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatus1APStatus.EffectDef = newEffect1AP;
                newReinforcementStatus1APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatus1APStatus.ShowNotification = false;
                newReinforcementStatus1APStatus.ShowNotificationOnUnApply = false;
                //     newReinforcementStatus1APStatus.EventOnApply = new TacticalEventDef();


                DelayedEffectStatusDef newReinforcementStatusUnder2APStatus = Helper.CreateDefFromClone(source, "{C3AB59A4-0579-4B3C-89FA-2370BB982071}", reinforcementStatusUnder2AP);

                newReinforcementStatusUnder2APStatus.EffectName = "RecentReinforcementUnder2AP";
                newReinforcementStatusUnder2APStatus.Visuals = Helper.CreateDefFromClone(source.Visuals, "{{466FAEDC-0CEE-4ADB-8A58-089B1B783348}}", reinforcementStatusUnder2AP);
                newReinforcementStatusUnder2APStatus.EffectDef = newEffectUnder2AP;
                //  Sprite icon = Helper.CreateSpriteFromImageFile("TBTV_CallReinforcements.png");

                newReinforcementStatusUnder2APStatus.Visuals.SmallIcon = viewElementSource.SmallIcon;
                newReinforcementStatusUnder2APStatus.Visuals.LargeIcon = viewElementSource.LargeIcon;
                newReinforcementStatusUnder2APStatus.Visuals.DisplayName1 = viewElementSource.DisplayName1; //for testing, adjust later
                newReinforcementStatusUnder2APStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newReinforcementStatusUnder2APStatus.EventOnApply = new TacticalEventDef();
                newReinforcementStatusUnder2APStatus.ShowNotification = false;
                newReinforcementStatusUnder2APStatus.ShowNotificationOnUnApply = false;
                //   newReinforcementStatusUnder2APStatus.EventOnApply = new TacticalEventDef();



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }




        }


        private static void CreateCharactersForPalaceMission()
        {

            /*Nikolai
    Stas
    Zhara
    Sophia_Villanova
    Colonel_Jack_Harlson
    Captain_Richter*/
            try
            {
                CreateHarlson();
                CreateRichter();
                CreateSofia();

                CreateZhara();
                CreateStas();
                CreateNikolai();

                CreateTaxiarchNergal();
                ChangeExalted();



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }







        }
        private static void ChangeExalted()
        {


            try
            {

                TacCharacterDef exalted = DefCache.GetDef<TacCharacterDef>("AN_Exalted_TacCharacterDef");

                List<TacticalAbilityDef> tacticalAbilities = exalted.Data.Abilites.ToList();



                ApplyStatusAbilityDef sowerOfChange = DefCache.GetDef<ApplyStatusAbilityDef>("SowerOfChange_AbilityDef");
                ApplyStatusAbilityDef bioChemist = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Biochemist_AbilityDef");
                ApplyEffectAbilityDef layWaste = DefCache.GetDef<ApplyEffectAbilityDef>("LayWaste_AbilityDef");

                tacticalAbilities.Add(sowerOfChange);
                tacticalAbilities.Add(bioChemist);
                tacticalAbilities.Add(layWaste);

                exalted.Data.Abilites = tacticalAbilities.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateHarlson()
        {
            try
            {

                JetJumpAbilityDef jetpackControl = DefCache.GetDef<JetJumpAbilityDef>("JetpackControl_AbilityDef");
                ApplyStatusAbilityDef boomBlast = DefCache.GetDef<ApplyStatusAbilityDef>("BigBooms_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                PassiveModifierAbilityDef punisher = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");



                string nameDef = "Harlson_TacCharacterDef";

                TacCharacterDef harlson = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Heavy7_CharacterTemplateDef"), "{88465F1E-64E1-4EAC-BCB2-A42CC8F915A8}", nameDef);
                harlson.Data.Name = "Colonel_Jack_Harlson";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                jetpackControl, boomBlast, takedown, punisher


                };

                harlson.Data.Abilites = abilities.ToArray();


                WeaponDef archangel = DefCache.GetDef<WeaponDef>("NJ_HeavyRocketLauncher_WeaponDef");
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");
                WeaponDef deceptor = DefCache.GetDef<WeaponDef>("NJ_Gauss_MachineGun_WeaponDef");

                WeaponDef guidedMissileLauncher = DefCache.GetDef<WeaponDef>("NJ_GuidedMissileLauncherPack_WeaponDef");

                TacticalItemDef hmgAmmo = DefCache.GetDef<TacticalItemDef>("NJ_Gauss_MachineGun_AmmoClip_ItemDef");
                TacticalItemDef hrAmmo = DefCache.GetDef<TacticalItemDef>("NJ_HeavyRocketLauncher_AmmoClip_ItemDef");
                TacticalItemDef gmAmmo = DefCache.GetDef<TacticalItemDef>("NJ_GuidedMissileLauncher_AmmoClip_ItemDef");


                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");


                harlson.Data.EquipmentItems = new ItemDef[] { archangel, fireNade, medkit };
                harlson.Data.InventoryItems = new ItemDef[] { gmAmmo, gmAmmo, hrAmmo, hrAmmo, hrAmmo, medkit };

                harlson.Data.LevelProgression.SetLevel(7);
                harlson.Data.Strength = 20;
                harlson.Data.Will = 14;
                harlson.Data.Speed = 10;

                GameTagDef characterTag = TFTVCommonMethods.CreateNewTag(nameDef, "{8AF3B063-8B77-4B3C-94BC-93A3D90B18C7}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                CustomizationPrimaryColorTagDef greyPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_0");

                CustomizationPatternTagDef noPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_0");
                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = harlson.Data.GameTags.ToList();
                gameTags.Add(greyPrimaryColor);
                gameTags.Add(secondaryBlackColor);
                gameTags.Add(noPattern);
                gameTags.Add(maleGenderTag);
                gameTags.Add(characterTag);

                harlson.SpawnCommandId = "HarlsonTFTV";
                harlson.Data.GameTags = gameTags.ToArray();
                harlson.CustomizationParams.KeepExistingCustomizationTags = true;



                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{D5A73379-CAA3-4C49-B9E3-FE37F4A2DD9A}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{879D3FB4-BCDF-4E79-BF27-E5100B60ECCC}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{99281C28-6764-444A-B06E-458B4374ED3B}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Torso_BodyPartDef");

                harlson.Data.BodypartItems = new ItemDef[] { newHead, legs, torso, guidedMissileLauncher };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Jack.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = harlson,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static void CreateRichter()
        {
            try
            {
                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                PassiveModifierAbilityDef punisher = DefCache.GetDef<PassiveModifierAbilityDef>("Punisher_AbilityDef");

                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");

                TacticalItemDef apARAmmo = DefCache.GetDef<TacticalItemDef>("NJ_PRCR_AssaultRifle_AmmoClip_ItemDef");

                string nameDef = "Richter_TacCharacterDef";

                TacCharacterDef richter = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Assault7_CharacterTemplateDef"), "{A275168C-03EA-4734-8B6D-A373E988C19B}", nameDef);
                richter.Data.Name = "Captain_Richter";
                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                aimedBurst, quarterback, takedown, punisher


                };

                richter.Data.Abilites = abilities.ToArray();

                richter.Data.LevelProgression.SetLevel(7);
                richter.Data.Strength = 16;
                richter.Data.Will = 14;
                richter.Data.Speed = 14;

                GameTagDef characterTag = TFTVCommonMethods.CreateNewTag(nameDef, "{AFCAF5E5-1E97-4564-9249-370AF8170756}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                FacialHairTagDef beard = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Ind1");

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");


                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                CustomizationHairColorTagDef whiteFacialHair = DefCache.GetDef<CustomizationHairColorTagDef>("CustomizationHairColorTagDef_6");
                RaceTagDef caucasian = DefCache.GetDef<RaceTagDef>("Caucasian_RaceTagDef");

                FaceTagDef face3 = DefCache.GetDef<FaceTagDef>("3_FaceTagDef");


                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = richter.Data.GameTags.ToList();
                gameTags.Add(caucasian);
                gameTags.Add(face3);
                gameTags.Add(beard);
                gameTags.Add(pattern9);
                gameTags.Add(whitePrimaryColor);
                gameTags.Add(secondaryBlueColor);
                gameTags.Add(whiteFacialHair);
                gameTags.Add(maleGenderTag);
                gameTags.Add(characterTag);

                richter.SpawnCommandId = "RichterTFTV";
                richter.Data.GameTags = gameTags.ToArray();
                richter.CustomizationParams.KeepExistingCustomizationTags = true;



                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{9DC824DF-50CE-408C-9804-E37B9ECFD74C}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{346BF292-8F76-417F-B30E-83709F592A84}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{89A1F3F2-DB35-45D8-AE6E-C1C9C3F33704}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Assault_Torso_BodyPartDef");

                richter.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };
                richter.Data.InventoryItems = new ItemDef[] { medkit, medkit, apARAmmo, apARAmmo, apARAmmo };

                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Richter.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = richter,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateSofia()
        {
            try
            {
                ApplyStatusAbilityDef manualControl = DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef");
                PassiveModifierAbilityDef remoteDeployment = DefCache.GetDef<PassiveModifierAbilityDef>("RemoteDeployment_AbilityDef");
                ApplyStatusAbilityDef takedown = DefCache.GetDef<ApplyStatusAbilityDef>("BC_Takedown_AbilityDef");
                ApplyStatusAbilityDef arTargeting = TFTVAircraftReworkMain.AircraftReworkOn
                    ? DefCache.GetDef<ApplyStatusAbilityDef>("AmplifyPain_AbilityDef")
                    : DefCache.GetDef<ApplyStatusAbilityDef>("BC_ARTargeting_AbilityDef");

                string nameDef = "Sofia_TacCharacterDef";

                TacCharacterDef sofia = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("NJ_Technician7_CharacterTemplateDef"), "{033AA4BB-AA41-45AF-B84B-CFD3F1C76014}", nameDef);
                sofia.Data.Name = "Sophia_Villanova";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                  manualControl,
                    remoteDeployment,
                    takedown,
                    arTargeting
                };

                sofia.Data.Abilites = abilities.ToArray();


                WeaponDef scorcher = DefCache.GetDef<WeaponDef>("PX_LaserPDW_WeaponDef");
                WeaponDef mechArms = DefCache.GetDef<WeaponDef>("NJ_Technician_MechArms_WeaponDef");

                TacticalItemDef laserAmmo = DefCache.GetDef<TacticalItemDef>("PX_LaserPDW_AmmoClip_ItemDef");
                TacticalItemDef mechArmsAmmo = DefCache.GetDef<TacticalItemDef>("MechArms_AmmoClip_ItemDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef fireNade = DefCache.GetDef<WeaponDef>("NJ_IncindieryGrenade_WeaponDef");
                TacticalItemDef laserTurret = DefCache.GetDef<TacticalItemDef>("PX_LaserTechTurretItem_ItemDef");

                sofia.Data.EquipmentItems = new ItemDef[] { scorcher, laserTurret, laserTurret };
                sofia.Data.InventoryItems = new ItemDef[] { laserAmmo, mechArmsAmmo, mechArmsAmmo, laserAmmo, medkit, medkit };

                sofia.Data.LevelProgression.SetLevel(7);
                sofia.Data.Strength = 16;
                sofia.Data.Will = 14;
                sofia.Data.Speed = 14;

                GameTagDef sofiaTag = TFTVCommonMethods.CreateNewTag(nameDef, "{1B969433-9925-454D-9EF5-15AC081EC607}");
                GenderTagDef femaleGenderTag = DefCache.GetDef<GenderTagDef>("Female_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");
                CustomizationSecondaryColorTagDef secondaryBlueColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_2");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");
                CustomizationPrimaryColorTagDef blackPrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");

                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");
                CustomizationPatternTagDef pattern9 = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_8");

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = sofia.Data.GameTags.ToList();
                gameTags.Add(blackPrimaryColor);
                gameTags.Add(secondaryBlueColor);
                gameTags.Add(pattern9);
                gameTags.Add(femaleGenderTag);
                gameTags.Add(sofiaTag);

                sofia.SpawnCommandId = "SofiaTFTV";
                sofia.Data.GameTags = gameTags.ToArray();
                sofia.CustomizationParams.KeepExistingCustomizationTags = true;

                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{354840D7-6543-4381-854E-472B5B126CE7}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{358AB930-0194-419B-BE25-2AADFFE8E97E}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{34155BEC-A605-4A0A-91A6-E1723606F118}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("NJ_Technician_Torso_BodyPartDef");

                sofia.Data.BodypartItems = new ItemDef[] { newHead, legs, torso, mechArms };

                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Sofia.png") });

                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = sofia,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateStas()
        {
            try
            {
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");
                ApplyStatusAbilityDef saboteur = DefCache.GetDef<ApplyStatusAbilityDef>("Saboteur_AbilityDef");
                RepositionAbilityDef vanish = DefCache.GetDef<RepositionAbilityDef>("Vanish_AbilityDef");
                ShootAbilityDef deployDronePack = DefCache.GetDef<ShootAbilityDef>("DeployDronePack_ShootAbilityDef");

                string nameDef = "Stas_TacCharacterDef";

                TacCharacterDef stas = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Infiltrator7_CharacterTemplateDef"), "{FBB2FE80-E86B-4C0F-9B02-19E52FF1F745}", nameDef);
                stas.Data.Name = "Stas";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                saboteur, overwatchFocus, vanish, deployDronePack


                };

                stas.Data.Abilites = abilities.ToArray();

                WeaponDef venomBolt = DefCache.GetDef<WeaponDef>("SY_Venombolt_WeaponDef");
                WeaponDef arachlauncher = DefCache.GetDef<WeaponDef>("SY_SpiderDroneLauncher_WeaponDef");
                WeaponDef laserSniper = DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef");
                WeaponDef laserPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef arachAmmo = DefCache.GetDef<TacticalItemDef>("SY_SpiderDroneLauncher_AmmoClip_ItemDef");
                TacticalItemDef venomBoltAmmo = DefCache.GetDef<TacticalItemDef>("SY_Venombolt_AmmoClip_ItemDef");

                TacticalItemDef laserRifleAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef");
                TacticalItemDef laserPistolAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef");


                stas.Data.EquipmentItems = new ItemDef[] { venomBolt, arachlauncher, sonicGrenade };
                stas.Data.InventoryItems = new ItemDef[] { venomBoltAmmo, venomBoltAmmo, arachAmmo, venomBoltAmmo, medkit, medkit };

                stas.Data.LevelProgression.SetLevel(7);
                stas.Data.Strength = 16;
                stas.Data.Will = 14;
                stas.Data.Speed = 14;

                GameTagDef stasTag = TFTVCommonMethods.CreateNewTag(nameDef, "{17647EF3-1D4D-4F9C-8525-6F8C3ADD9B5A}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                //CustomizationPatternTagDef_11
                //CustomizationSecondaryColorTagDef_9



                List<GameTagDef> gameTags = stas.Data.GameTags.ToList();

                gameTags.Add(maleGenderTag);
                gameTags.Add(stasTag);

                stas.SpawnCommandId = "StasTFTV";
                stas.Data.GameTags = gameTags.ToArray();
                stas.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{734C5B3A-DA43-4045-B10D-E3799866D98D}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{BDC0706A-8A86-4E20-B479-CAA65856E4FC}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{F4D611AB-B89D-40E4-AAD6-6382BCE5D74B}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("SY_Infiltrator_Bonus_Torso_BodyPartDef");

                stas.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Stas.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = stas,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateNikolai()
        {
            try
            {
                PassiveModifierAbilityDef endurance = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");

                ShootAbilityDef gunslinger = DefCache.GetDef<ShootAbilityDef>("BC_Gunslinger_AbilityDef");
                PassiveModifierAbilityDef killzone = DefCache.GetDef<PassiveModifierAbilityDef>("KillZone_AbilityDef");

                List<TacticalAbilityDef> abilitiesToAdd = new List<TacticalAbilityDef>()
                {
                endurance, overwatchFocus, gunslinger, killzone

                };

                string nameDef = "Nikolai_TacCharacterDef";

                TacCharacterDef nikolai = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Sniper7_CharacterTemplateDef"), "{99DA6A62-BF24-471C-B966-1954C6F5A9E1}", nameDef);
                nikolai.Data.Name = "Nikolai";

                nikolai.Data.Abilites = abilitiesToAdd.ToArray();


                WeaponDef laserSniper = DefCache.GetDef<WeaponDef>("SY_LaserSniperRifle_WeaponDef");
                WeaponDef laserPistol = DefCache.GetDef<WeaponDef>("SY_LaserPistol_WeaponDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserRifleAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserSniperRifle_AmmoClip_ItemDef");
                TacticalItemDef laserPistolAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserPistol_AmmoClip_ItemDef");


                nikolai.Data.EquipmentItems = new ItemDef[] { laserSniper, laserPistol, medkit };
                nikolai.Data.InventoryItems = new ItemDef[] { laserRifleAmmo, laserRifleAmmo, laserPistolAmmo, laserPistolAmmo, medkit };

                nikolai.Data.LevelProgression.SetLevel(7);
                nikolai.Data.Strength = 16;
                nikolai.Data.Will = 14;
                nikolai.Data.Speed = 14;

                GameTagDef nikolaiTag = TFTVCommonMethods.CreateNewTag(nameDef, "{E9013ABC-E6C3-4F43-876D-B1DE64053F75}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                //CustomizationPatternTagDef_11
                //CustomizationSecondaryColorTagDef_9

                CustomizationSecondaryColorTagDef secondaryBlackColor = DefCache.GetDef<CustomizationSecondaryColorTagDef>("CustomizationSecondaryColorTagDef_9");

                CustomizationPrimaryColorTagDef whitePrimaryColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_5");

                CustomizationPatternTagDef linesPattern = DefCache.GetDef<CustomizationPatternTagDef>("CustomizationPatternTagDef_11");

                List<GameTagDef> gameTags = nikolai.Data.GameTags.ToList();
                gameTags.Add(secondaryBlackColor);
                gameTags.Add(whitePrimaryColor);
                gameTags.Add(linesPattern);
                gameTags.Add(maleGenderTag);
                gameTags.Add(nikolaiTag);

                nikolai.SpawnCommandId = "NikolaiTFTV";
                nikolai.Data.GameTags = gameTags.ToArray();
                nikolai.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef head = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef newHead = Helper.CreateDefFromClone(head, "{FF4FF18F-B701-4CE5-94F6-DF513A349072}", nameDef);
                newHead.ViewElementDef = Helper.CreateDefFromClone(head.ViewElementDef, "{9F811161-BC19-45F6-BA4B-B17910101CA7}", nameDef);
                newHead.BodyPartAspectDef = Helper.CreateDefFromClone(head.BodyPartAspectDef, "{06D4E1A5-B036-4683-9B5B-DE2864F2D4A9}", nameDef);

                TacticalItemDef legs = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Legs_ItemDef");
                TacticalItemDef torso = DefCache.GetDef<TacticalItemDef>("SY_Sniper_Torso_BodyPartDef");

                nikolai.Data.BodypartItems = new ItemDef[] { newHead, legs, torso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = newHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Nikolai.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = nikolai,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();






            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateZhara()
        {
            try
            {

                PassiveModifierAbilityDef endurance = DefCache.GetDef<PassiveModifierAbilityDef>("Endurance_AbilityDef");
                OverwatchFocusAbilityDef overwatchFocus = DefCache.GetDef<OverwatchFocusAbilityDef>("OverwatchFocus_AbilityDef");

                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");

                string nameDef = "Zhara_TacCharacterDef";

                TacCharacterDef zhara = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("SY_Assault7_CharacterTemplateDef"), "{CBC16AB7-7469-4251-AF06-35122B4412DD}", nameDef);
                zhara.Data.Name = "Zhara";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                endurance, overwatchFocus, aimedBurst, quarterback


                };

                zhara.Data.Abilites = abilities.ToArray();

                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");
                WeaponDef deimosWhite = DefCache.GetDef<WeaponDef>("SY_LaserAssaultRifle_WhiteNeon_WeaponDef");
                WeaponDef poisonGrenade = DefCache.GetDef<WeaponDef>("SY_PoisonGrenade_WeaponDef");
                WeaponDef sonicGrenade = DefCache.GetDef<WeaponDef>("SY_SonicGrenade_WeaponDef");

                TacticalItemDef laserAmmo = DefCache.GetDef<TacticalItemDef>("SY_LaserAssaultRifle_AmmoClip_ItemDef");

                zhara.Data.EquipmentItems = new ItemDef[] { deimosWhite, poisonGrenade, sonicGrenade };
                zhara.Data.InventoryItems = new ItemDef[] { laserAmmo, laserAmmo, laserAmmo, medkit, medkit };

                zhara.Data.LevelProgression.SetLevel(7);
                zhara.Data.Strength = 16;
                zhara.Data.Will = 14;
                zhara.Data.Speed = 14;

                GameTagDef zharaTag = TFTVCommonMethods.CreateNewTag(nameDef, "{24DB53A2-3710-4900-A15B-D1B673BED535}");
                GenderTagDef femaleGenderTag = DefCache.GetDef<GenderTagDef>("Female_GenderTagDef");
                // FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                // CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = zhara.Data.GameTags.ToList();
                //   gameTags.Add(blackColor);
                gameTags.Add(femaleGenderTag);
                gameTags.Add(zharaTag);

                zhara.SpawnCommandId = "ZharaTFTV";
                zhara.Data.GameTags = gameTags.ToArray();
                zhara.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef assaultHead = DefCache.GetDef<TacticalItemDef>("SY_Assault_Helmet_WhiteNeon_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef zharaHead = Helper.CreateDefFromClone(assaultHead, "{D583A19E-2238-431D-BD70-4A058E2B46EC}", "ZharaHead_ItemDef");
                zharaHead.ViewElementDef = Helper.CreateDefFromClone(assaultHead.ViewElementDef, "{3ADA66FA-2307-4D48-96CB-959882176617}", "ZharaHead_ItemDef");
                zharaHead.BodyPartAspectDef = Helper.CreateDefFromClone(assaultHead.BodyPartAspectDef, "{B1160987-6DD3-410E-B6D9-536274CC0645}", "ZharaHead_ItemDef");

                TacticalItemDef assaultLegs = DefCache.GetDef<TacticalItemDef>("SY_Assault_Legs_WhiteNeon_ItemDef");
                TacticalItemDef assaultTorso = DefCache.GetDef<TacticalItemDef>("SY_Assault_Torso_WhiteNeon_BodyPartDef");

                zhara.Data.BodypartItems = new ItemDef[] { zharaHead, assaultLegs, assaultTorso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = zharaHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Zhara.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = zhara,
                    Amount = new RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();
                DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }
        private static void CreateTaxiarchNergal()
        {
            try
            {
                // ApplyEffectAbilityDef LayWaste_AbilityDef
                //ApplyStatusAbilityDef BC_Biochemist_AbilityDef

                ApplyEffectAbilityDef mistBreather = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");
                ApplyStatusAbilityDef sowerOfChange = DefCache.GetDef<ApplyStatusAbilityDef>("SowerOfChange_AbilityDef");


                ShootAbilityDef aimedBurst = DefCache.GetDef<ShootAbilityDef>("AimedBurst_AbilityDef");
                PassiveModifierAbilityDef quarterback = DefCache.GetDef<PassiveModifierAbilityDef>("Pitcher_AbilityDef");

                string nameDef = "TaxiarchNergal_TacCharacterDef";

                TacCharacterDef taxiarchNergal = Helper.CreateDefFromClone(DefCache.GetDef<TacCharacterDef>("AN_Assault7_CharacterTemplateDef"), "{3AA9BBC1-FCE2-4274-AEA1-7CD00E3677DC}", nameDef);
                taxiarchNergal.Data.Name = "Taxiarch_Nergal";

                List<TacticalAbilityDef> abilities = new List<TacticalAbilityDef>()
                {
                mistBreather, sowerOfChange, aimedBurst, quarterback


                };

                taxiarchNergal.Data.Abilites = abilities.ToArray();

                WeaponDef shreddingShotgun = DefCache.GetDef<WeaponDef>("AN_ShreddingShotgun_WeaponDef");
                WeaponDef acidGrenade = DefCache.GetDef<WeaponDef>("AN_AcidGrenade_WeaponDef");
                TacticalItemDef shreddingAmmo = DefCache.GetDef<TacticalItemDef>("AN_ShreddingShotgun_AmmoClip_ItemDef");
                EquipmentDef medkit = DefCache.GetDef<EquipmentDef>("Medkit_EquipmentDef");

                taxiarchNergal.Data.EquipmentItems = new ItemDef[] { shreddingShotgun, acidGrenade, medkit };
                taxiarchNergal.Data.InventoryItems = new ItemDef[] { shreddingAmmo, shreddingAmmo, shreddingAmmo, shreddingAmmo, medkit, acidGrenade };

                taxiarchNergal.Data.LevelProgression.SetLevel(7);
                taxiarchNergal.Data.Strength = 16;
                taxiarchNergal.Data.Will = 14;
                taxiarchNergal.Data.Speed = 14;

                GameTagDef taxiarchTag = TFTVCommonMethods.CreateNewTag(nameDef, "{AD9711B0-2A39-4E82-BF9C-BDB8111C3697}");
                GenderTagDef maleGenderTag = DefCache.GetDef<GenderTagDef>("Male_GenderTagDef");
                FacialHairTagDef noFacialHairTag = DefCache.GetDef<FacialHairTagDef>("FacialHairTagDef_Empty");

                //  VoiceProfileTagDef newEmptyVoiceTag = Helper.CreateDefFromClone<VoiceProfileTagDef>(DefCache.GetDef<VoiceProfileTagDef>("1_VoiceProfileTagDef"), "{6935EA8D-95AB-4035-AB9B-B7390138733F}", "EmptyVoiceTag");

                // CustomizationColorTagDef_10 green
                // CustomizationColorTagDef_14 pink
                // CustomizationColorTagDef_0 grey
                // CustomizationColorTagDef_7 red

                CustomizationPrimaryColorTagDef blackColor = DefCache.GetDef<CustomizationPrimaryColorTagDef>("CustomizationColorTagDef_9");
                List<GameTagDef> gameTags = taxiarchNergal.Data.GameTags.ToList();
                gameTags.Add(blackColor);
                gameTags.Add(maleGenderTag);
                gameTags.Add(taxiarchTag);
                gameTags.Add(noFacialHairTag);
                //   gameTags.Add(newEmptyVoiceTag);
                taxiarchNergal.SpawnCommandId = "TaxiarchNergalTFTV";
                taxiarchNergal.Data.GameTags = gameTags.ToArray();
                taxiarchNergal.CustomizationParams.KeepExistingCustomizationTags = true;


                TacticalItemDef assaultHead = DefCache.GetDef<TacticalItemDef>("AN_Assault_Helmet_BodyPartDef");

                SquadPortraitsDef squadPortraits = DefCache.GetDef<SquadPortraitsDef>("SquadPortraitsDef");

                TacticalItemDef taxiarchNergalHead = Helper.CreateDefFromClone(assaultHead, "{6BA24E77-F104-4979-A8CC-720B988AB344}", "TaxiarchNergalHead_ItemDef");
                taxiarchNergalHead.ViewElementDef = Helper.CreateDefFromClone(assaultHead.ViewElementDef, "{064E1B24-E796-4E6D-97CF-00EF59BF1FC6}", "TaxiarchNergalHead_ItemDef");

                taxiarchNergalHead.BodyPartAspectDef = Helper.CreateDefFromClone(assaultHead.BodyPartAspectDef, "{A7FAAFE1-3EF6-4DB7-A5B1-43FC3DE2A335}", "TaxiarchNergalHead_ItemDef");

                TacticalItemDef assaultLegs = DefCache.GetDef<TacticalItemDef>("AN_Assault_Legs_ItemDef");
                TacticalItemDef assaultTorso = DefCache.GetDef<TacticalItemDef>("AN_Assault_Torso_BodyPartDef");

                taxiarchNergal.Data.BodypartItems = new ItemDef[] { taxiarchNergalHead, assaultLegs, assaultTorso };


                squadPortraits.ManualPortraits.Add(new SquadPortraitsDef.ManualPortrait { HeadPart = taxiarchNergalHead, Portrait = Helper.CreateSpriteFromImageFile("PM_Taxiarch_Nergal.png") });




                List<TacMissionTypeParticipantData.UniqueChatarcterBind> tacCharacterDefs = DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef").ParticipantsData[1].UniqueUnits.ToList();
                TacMissionTypeParticipantData.UniqueChatarcterBind uniqueChatarcterBind = new TacMissionTypeParticipantData.UniqueChatarcterBind
                {
                    Character = taxiarchNergal,
                    Amount = new Base.Utils.RangeDataInt { Max = 1, Min = 1 },
                };
                tacCharacterDefs.Add(uniqueChatarcterBind);
                DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef").ParticipantsData[0].UniqueUnits = tacCharacterDefs.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }





        }
        private static void ChangePalaceMissionDefs()
        {
            try
            {

                string newActivatedStatusName = "YuggothianThingyActivated";

                TacStatusDef tacStatusDef = DefCache.GetDef<TacStatusDef>("ActiveHackableChannelingConsole_StatusDef");
                TacStatusDef newActivatedStatusDef = Helper.CreateDefFromClone(tacStatusDef, "{813BC5B3-143C-4B0A-B449-6AFBAA3B3792}", newActivatedStatusName);
                newActivatedStatusDef.EffectName = newActivatedStatusName;

                ActivateConsoleFactionObjectiveDef interactWithYRObjectivePX = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianPX_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveNJ = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianBeacon_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveAnu = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianExalted_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveSyPoly = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianPoly_CustomMissionObjective");
                ActivateConsoleFactionObjectiveDef interactWithYRObjectiveSyTerra = DefCache.GetDef<ActivateConsoleFactionObjectiveDef>("InteractWithYuggothianTerra_CustomMissionObjective");

                List<ActivateConsoleFactionObjectiveDef> victoryMissionInteractObjectives = new List<ActivateConsoleFactionObjectiveDef>()
                    {
                        interactWithYRObjectivePX,
                        interactWithYRObjectiveAnu,
                        interactWithYRObjectiveNJ,
                        interactWithYRObjectiveSyPoly,
                        interactWithYRObjectiveSyTerra
                    };

                foreach (ActivateConsoleFactionObjectiveDef activateConsoleFactionObjectiveDef in victoryMissionInteractObjectives)
                {
                    activateConsoleFactionObjectiveDef.ObjectiveData.ActivatedInteractableStatusDef = newActivatedStatusDef;
                    activateConsoleFactionObjectiveDef.IsDefeatObjective = false;

                }

                CustomMissionTypeDef pxPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("PXVictory_CustomMissionTypeDef");

                pxPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("PXPalace", "{0CF66B9B-2E8F-4195-A688-A52DECD1982A}"));

                CustomMissionTypeDef njPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("NJVictory_CustomMissionTypeDef");
                njPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("NJPalace", "{5D7A9365-7BC2-4CAA-9D0E-2B6A06FA67A3}"));
                njPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef anuPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("ANVictory_CustomMissionTypeDef");
                anuPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("ANPalace", "{AAFC6643-110D-48AB-8730-AC7A86C6B8F3}"));
                anuPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef syPolyPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("SYPolyVictory_CustomMissionTypeDef");
                syPolyPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("SYPolyPalace", "{B8156DBC-5188-436C-A6B1-B00EA5362A11}"));
                syPolyPalaceMissionDef.MaxPlayerUnits = 7;

                CustomMissionTypeDef syTerraPalaceMissionDef = DefCache.GetDef<CustomMissionTypeDef>("SYTerraVictory_CustomMissionTypeDef");
                syTerraPalaceMissionDef.Tags.Add(TFTVCommonMethods.CreateNewMissionTag("SYTerraPalace", "{D2049387-C2C7-426A-82DB-E367851B5437}"));
                syTerraPalaceMissionDef.MaxPlayerUnits = 7;

                List<CustomMissionTypeDef> victoryMissions = new List<CustomMissionTypeDef>()
                    {
                    pxPalaceMissionDef,
                    njPalaceMissionDef,
                   // anuPalaceMissionDef,
                    syPolyPalaceMissionDef,
                    syTerraPalaceMissionDef
                    };

                anuPalaceMissionDef.ParticipantsData[1].ActorDeployParams.Clear();
                anuPalaceMissionDef.CustomObjectives = new FactionObjectiveDef[] { anuPalaceMissionDef.CustomObjectives[0].NextOnSuccess[0].NextOnSuccess[0], anuPalaceMissionDef.CustomObjectives[1] };

                foreach (CustomMissionTypeDef customMissionTypeDef in victoryMissions)
                {
                    // customMissionTypeDef.ParticipantsData[1].ActorDeployParams.Clear();
                    customMissionTypeDef.CustomObjectives = new FactionObjectiveDef[] { customMissionTypeDef.CustomObjectives[0], customMissionTypeDef.CustomObjectives[1], customMissionTypeDef.CustomObjectives[2].NextOnSuccess[0].NextOnSuccess[0] };



                    //  pxPalaceMissionDef.ParticipantsData[1].ActorDeployParams.Clear();
                    //  pxPalaceMissionDef.CustomObjectives = new FactionObjectiveDef[] { pxPalaceMissionDef.CustomObjectives[0], pxPalaceMissionDef.CustomObjectives[1], interactWithYRObjectivePX };
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        /// <summary>
        /// This DR is only used when Stronger Pandorans is switched on. However, it has to be created always in case a tactical save is loaded
        /// straight from title screen; otherwise the game will never finish loading.
        /// </summary>

        private static void CreateScyllaDamageResistanceForStrongerPandorans()
        {

            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "ScyllaDamageResistance";
                string gUID = "{CE61D05C-5A75-4354-BEC8-73EC0357F971}";
                string gUIDVisuals = "{6272B177-49AA-4F81-9C05-9CB9026A26C5}";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                //   TFTVLogger.Always($"{source.DamageTypeDefs.Count()}");

                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.75f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                newStatus.DamageTypeDefs = source.DamageTypeDefs;

                //   TFTVLogger.Always($"{newStatus.DamageTypeDefs.Count()}");

                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);
                damageTypeBaseEffectDefs.Add(TFTVMeleeDamage.MeleeStandardDamageType);

                //     TFTVLogger.Always($"damageTypeBaseEffectDefs {damageTypeBaseEffectDefs.Count()}");

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                //  TFTVLogger.Always($"{newStatus.DamageTypeDefs.Count()}");

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "SCYLLA_DAMAGERESISTANCE_NAME";
                newStatus.Visuals.Description.LocalizationKey = "SCYLLA_DAMAGERESISTANCE_DESCRIPTION";



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        private static void CreateObjectiveCaptureCapacity()
        {
            try
            {
                TFTVCommonMethods.CreateObjectiveReminder("{25590AE4-872B-4679-A15C-300C3DC48A53}", "CAPTURE_CAPACITY_AIRCRAFT", 0);
                TFTVCommonMethods.CreateObjectiveReminder("{4EB4A290-8FE7-45CC-BF8B-914C52441EF4}", "CAPTURE_CAPACITY_BASE", 0);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void StealAircraftMissionsNoItemRecovery()
        {
            try
            {
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftAN_CustomMissionTypeDef").DontRecoverItems = true;
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftNJ_CustomMissionTypeDef").DontRecoverItems = true;
                DefCache.GetDef<CustomMissionTypeDef>("StealAircraftSY_CustomMissionTypeDef").DontRecoverItems = true;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void CreateFoodPoisoningEvents()
        {
            try
            {
                string event1Name = "FoodPoisoning1";
                string event1Title = "FOOD_POISONING_TITLE_1";
                string event1Description = "FOOD_POISONING_DESCRIPTION_1";
                //  string event1Outcome = "FOOD_POISONING_OUTCOME_1";

                string event2Name = "FoodPoisoning2";
                string event2Title = "FOOD_POISONING_TITLE_2";
                string event2Description = "FOOD_POISONING_DESCRIPTION_2";
                //  string event2Outcome = "FOOD_POISONING_OUTCOME_2";

                string event3Name = "FoodPoisoning3";
                string event3Title = "FOOD_POISONING_TITLE_3";
                string event3Description = "FOOD_POISONING_DESCRIPTION_3";
                //  string event3Outcome = "FOOD_POISONING_OUTCOME_3";

                GeoscapeEventDef foodPoisoning1 = TFTVCommonMethods.CreateNewEvent(event1Name, event1Title, event1Description, null);
                foodPoisoning1.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 20;
                foodPoisoning1.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 10;


                GeoscapeEventDef foodPoisoning2 = TFTVCommonMethods.CreateNewEvent(event2Name, event2Title, event2Description, null);
                foodPoisoning2.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 40;
                foodPoisoning2.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 20;

                GeoscapeEventDef foodPoisoning3 = TFTVCommonMethods.CreateNewEvent(event3Name, event3Title, event3Description, null);
                foodPoisoning3.GeoscapeEventData.Choices[0].Outcome.DamageAllSoldiers = 80;
                foodPoisoning3.GeoscapeEventData.Choices[0].Outcome.TireAllSoldiers = 40;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        private static void CreateReinforcementTag()
        {
            try
            {
                TFTVCommonMethods.CreateNewTag("ReinforcementTag", "{19762255-93FC-4A7B-877D-914A3BD152C9}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ChangeVehicleInventorySlots()
        {
            try
            {
                DefCache.GetDef<BackpackFilterDef>("VehicleBackpackFilterDef").MaxItems = 12;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void MakeVestsOnlyForOrganicMeatbags()
        {
            try
            {


                GameTagDef organicMeatbagTorsoTag = TFTVCommonMethods.CreateNewTag("MeatBagTorso", "{8D13AAD6-BA65-4907-B3C8-C977B819BF48}");

                GameTagDef rocketMountTorsoTag = TFTVCommonMethods.CreateNewTag("RocketMountTorso", "{04ABD5E5-6666-4E8E-AF19-EF958315CDE1}");

                foreach (TacticalItemDef item in Repo.GetAllDefs<TacticalItemDef>()

                    .Where(ti => ti.name.Contains("Torso"))

                    .Where(ti => ti.name.StartsWith("AN_") || ti.name.StartsWith("SY_") || ti.name.StartsWith("NJ_")
                    || ti.name.StartsWith("NEU") || ti.name.StartsWith("PX_") || ti.name.StartsWith("IN_")))

                {
                    if (!item.Tags.Contains(organicMeatbagTorsoTag) && !item.name.Contains("BIO"))
                    {
                        item.Tags.Add(organicMeatbagTorsoTag);
                        //  TFTVLogger.Always($"adding organicMeatbagTorsoTag to {item.name}");
                    }

                    if (!item.Tags.Contains(rocketMountTorsoTag) &&
                        item.Tags.Contains(DefCache.GetDef<GameTagDef>("Heavy_ClassTagDef"))
                        && !item.name.StartsWith("IN_"))
                    {
                        item.Tags.Add(rocketMountTorsoTag);
                        //  TFTVLogger.Always($"adding rocketMountTorsoTag to {item.name}");
                    }
                }

                WeaponDef fury = DefCache.GetDef<WeaponDef>("NJ_RocketLauncherPack_WeaponDef");
                WeaponDef thor = DefCache.GetDef<WeaponDef>("NJ_GuidedMissileLauncherPack_WeaponDef");
                WeaponDef destiny = DefCache.GetDef<WeaponDef>("PX_LaserArrayPack_WeaponDef");
                WeaponDef ragnarok = DefCache.GetDef<WeaponDef>("PX_ShredingMissileLauncherPack_WeaponDef");

                fury.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                thor.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                destiny.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;
                ragnarok.RequiredSlotBinds[0].GameTagFilter = rocketMountTorsoTag;

                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                TacticalItemDef nanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                blastVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                poisonVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                fireVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;
                nanoVest.RequiredSlotBinds[0].GameTagFilter = organicMeatbagTorsoTag;

                TacticalItemDef indepHeavyArmor = DefCache.GetDef<TacticalItemDef>("IN_Heavy_Torso_BodyPartDef");
                TacticalItemDef njHeavyArmor = DefCache.GetDef<TacticalItemDef>("NJ_Heavy_Torso_BodyPartDef");
                indepHeavyArmor.ProvidedSlots = new ProvidedSlotBind[] { indepHeavyArmor.ProvidedSlots[0], njHeavyArmor.ProvidedSlots[0] };


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        internal static void ChangesModulesAndAcid()
        {
            try
            {


                ChangeModulePictures();
                RemoveAcidAsVulnerability();
                CreateNanoVestAbilityAndStatus();
                CreateHealingMultiplierAbility();
                CreateParalysisDamageResistance();
                ModifyPoisonResVest();
                ModifyBlastAndFireResVests();
                ChangeVestsNJTemplates();
                CreateAcidResistantVest();
                CreateNanotechVest();
                AdjustResearches();
                RemoveRepairKitFromPure();
                AdjustAcidDamage();
                MakeVestsOnlyForOrganicMeatbags();
                MakeMistRepellerLegModule();
                CreateNanotechFieldkit();
                CreateAcidDisabledStatus();
                ChangeArchaelogyLab();
            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        private static void ChangeArchaelogyLab()
        {
            try
            {
                PhoenixFacilityDef archlab = DefCache.GetDef<PhoenixFacilityDef>("ArcheologyLab_PhoenixFacilityDef");

                archlab.FacilityLimitPerBase = 1;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void AdjustAcidDamage()
        {
            try
            {
                //nerfed on 13/11
                DefCache.GetDef<DamagePayloadEffectDef>("E_Element0 [SwarmerAcidExplosion_Die_AbilityDef]").DamagePayload.DamageKeywords[1].Value = 20;

                DefCache.GetDef<WeaponDef>("AcidSwarmer_Torso_BodyPartDef").DamagePayload.DamageKeywords[1].Value = 10;

                //this is reduced in Stronger Pandorans to 20 acid
                ApplyDamageEffectAbilityDef aWormDamage = DefCache.GetDef<ApplyDamageEffectAbilityDef>("AcidwormExplode_AbilityDef");

                aWormDamage.DamagePayload.DamageKeywords = new List<DamageKeywordPair>()
                {
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.BlastKeyword, Value = 10 },
                new DamageKeywordPair{DamageKeywordDef = Shared.SharedDamageKeywords.AcidKeyword, Value = 20 },
                };

                //All Acheron acid attacks reduced by 10.

                /*[TFTV @ 7/13/2023 2:15:32 PM] AN_AcidGrenade_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AN_AcidHandGun_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] KS_Redemptor_WeaponDef does 5 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] FS_AssaultGrenadeLauncher_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] PX_AcidCannon_WeaponDef does 40 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] PX_AcidAssaultRifle_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Mutoid_Head_AcidSpray_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronAchlys_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronAchlysChampion_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Acheron_Arms_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcheronPrime_Arms_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Chiron_Abdomen_Acid_Mortar_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Crabman_LeftHand_Acid_Grenade_WeaponDef does 10 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Crabman_LeftHand_Acid_EliteGrenade_WeaponDef does 20 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Siren_Torso_AcidSpitter_WeaponDef does 30 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] Siren_Torso_Orichalcum_WeaponDef does 40 acid damage
[TFTV @ 7/13/2023 2:15:32 PM] AcidSwarmer_Torso_BodyPartDef does 30 acid damage*/



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }




        }

        private static void MakeMistRepellerLegModule()
        {
            try
            {

                TacticalItemDef gooRepeller = DefCache.GetDef<TacticalItemDef>("PX_GooRepeller_Attachment_ItemDef");
                TacticalItemDef mistRepeller = DefCache.GetDef<TacticalItemDef>("SY_MistRepeller_Attachment_ItemDef");
                mistRepeller.RequiredSlotBinds = gooRepeller.RequiredSlotBinds;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void RemoveRepairKitFromPure()
        {

            try
            {
                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");
                WeaponDef grenade = DefCache.GetDef<WeaponDef>("PX_HandGrenade_WeaponDef");
                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>().Where(tc => tc.Data.EquipmentItems.Any(ei => ei == repairKit)))
                {


                    List<ItemDef> itemDefs = tacCharacterDef.Data.EquipmentItems.ToList();
                    itemDefs.Remove(repairKit);
                    itemDefs.Add(grenade);
                    tacCharacterDef.Data.EquipmentItems = itemDefs.ToArray();
                    //  TFTVLogger.Always($"removed {repairKit.name} and gave {grenade.name} to {tacCharacterDef.name}");

                }





            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        private static void CreateNanoVestAbilityAndStatus()
        {
            string skillName = "NanoVest_AbilityDef";
            ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("CloseQuarters_AbilityDef");
            ApplyStatusAbilityDef nanoVestAbility = Helper.CreateDefFromClone(
                source,
                "{FEF02379-A90F-4670-8FD7-574CDCB5753F}",
                skillName);
            nanoVestAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                "{15458996-77C2-4F9B-8E31-0DD1A6D77571}",
                skillName);
            nanoVestAbility.TargetingDataDef = DefCache.GetDef<ApplyStatusAbilityDef>("QuickAim_AbilityDef").TargetingDataDef;
            nanoVestAbility.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                "{8959A8C5-0405-4D46-8632-0CCA9EF029DB}",
                skillName);
            nanoVestAbility.ViewElementDef.ShowInInventoryItemTooltip = true;

            nanoVestAbility.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
            nanoVestAbility.ViewElementDef.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
            nanoVestAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("module_nanovest_ability.png");
            nanoVestAbility.ViewElementDef.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;

            string statusName = "NanoVest_StatusDef";
            ItemSlotStatsModifyStatusDef nanoVestBuffStatus = Helper.CreateDefFromClone(
                DefCache.GetDef<ItemSlotStatsModifyStatusDef>("E_Status [ElectricReinforcement_AbilityDef]"),
                "{57E033FC-FECD-4A9E-8AE6-CC17FB5116A9}",
                statusName);
            nanoVestBuffStatus.Visuals = Helper.CreateDefFromClone(
                nanoVestAbility.ViewElementDef,
                "{8F111A9C-020C-4166-9444-1211CF517884}",
                statusName);

            nanoVestBuffStatus.Duration = -1;

            nanoVestBuffStatus.Visuals.DisplayName1.LocalizationKey = "NANOVEST_ABILITY_NAME";
            nanoVestBuffStatus.Visuals.Description.LocalizationKey = "NANOVEST_ABILITY_DESCRIPTION";
            nanoVestBuffStatus.Visuals.LargeIcon = nanoVestAbility.ViewElementDef.LargeIcon;
            nanoVestBuffStatus.Visuals.SmallIcon = nanoVestAbility.ViewElementDef.LargeIcon;
            nanoVestBuffStatus.StatsModifications = new ItemSlotModification[]
            {
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddMax,
            Value = 10f,
            ShowsNotification = false,
            NotifyOnce = false
        },
        new ItemSlotModification()
        {
            Type = StatType.Health,
            ModificationType = StatModificationType.AddRestrictedToBounds,
            Value = 10f,
            ShowsNotification = true,
            NotifyOnce = true
        }
            };

            nanoVestAbility.StatusDef = nanoVestBuffStatus;
            TFTVAircraftReworkMain.NanoVestStatusDef = nanoVestBuffStatus;
        }

        internal static void ModifyBlastAndFireResVests()
        {
            try
            {


                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                blastVest.Abilities = new AbilityDef[] { fireVest.Abilities[0], blastVest.Abilities[0] };
                blastVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_blastresvest.png");
                blastVest.ViewElementDef.InventoryIcon = blastVest.ViewElementDef.LargeIcon;
                // TFTVAircraftRework.BlastVestResistance = (DamageMultiplierAbilityDef)blastVest.Abilities[0];
                // TFTVAircraftRework.FireVestResistance = (DamageMultiplierAbilityDef)fireVest.Abilities[0];
                TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)blastVest.Abilities[0]);
                TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)fireVest.Abilities[0]);



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void ChangeVestsNJTemplates()
        {
            try
            {

                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");


                List<TacCharacterDef> tacCharacterDefs = new List<TacCharacterDef>()
                {
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault7_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Assault7_recruitable_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Sniper6_CharacterTemplateDef"),
                    DefCache.GetDef<TacCharacterDef>("NJ_Sniper7_CharacterTemplateDef"),



                };


                foreach (TacCharacterDef tacCharacterDef in tacCharacterDefs)
                {
                    // TFTVLogger.Always($"{tacCharacterDef.name} has fireVest");

                    List<ItemDef> bodypartItems = tacCharacterDef.Data.BodypartItems.ToList();
                    bodypartItems.Remove(fireVest);
                    bodypartItems.Add(blastVest);
                }


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }


        internal static void CreateNanotechVest()
        {
            try
            {

                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");

                FactionTagDef nJTag = DefCache.GetDef<FactionTagDef>("NewJerico_FactionTagDef");
                FactionTagDef pXTag = DefCache.GetDef<FactionTagDef>("PhoenixPoint_FactionTagDef");


                if (fireVest.Tags.Contains(nJTag))
                {
                    fireVest.Tags.Remove(nJTag);

                }

                if (!fireVest.Tags.Contains(pXTag))
                {
                    fireVest.Tags.Add(pXTag);

                }

                TacticalItemDef newNanoVest = Helper.CreateDefFromClone(fireVest, "{D07B639A-E1F4-46F4-91BB-1CCDCCCE8EC1}", "NanotechVest");
                newNanoVest.ViewElementDef = Helper.CreateDefFromClone(fireVest.ViewElementDef, "{0F1BD9BA-1895-46C7-90AF-26FB92D702F6}", "Nanotech_ViewElement");


                newNanoVest.Abilities = new AbilityDef[] { DefCache.GetDef<ApplyStatusAbilityDef>("NanoVest_AbilityDef") };
                newNanoVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_nanovest.png");
                newNanoVest.ViewElementDef.DisplayName1.LocalizationKey = "NANOVEST_NAME";
                newNanoVest.ViewElementDef.DisplayName2.LocalizationKey = "NANOVEST_NAME";
                newNanoVest.ViewElementDef.Description.LocalizationKey = "NANOVEST_DESCRIPTION";
                newNanoVest.ViewElementDef.InventoryIcon = newNanoVest.ViewElementDef.LargeIcon;

                newNanoVest.ManufactureTech = 20;
                newNanoVest.ManufactureMaterials = 30;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void CreateAcidResistantVest()
        {
            try
            {
                TacticalItemDef fireVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                fireVest.Abilities = new AbilityDef[] { DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef") };
                fireVest.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_fireresvest.png");
                fireVest.ViewElementDef.InventoryIcon = fireVest.ViewElementDef.LargeIcon;
                TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)fireVest.Abilities[0]);

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        internal static void CreateNanotechFieldkit()
        {
            try
            {

                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");
                HealAbilityDef repairKitAbility = DefCache.GetDef<HealAbilityDef>("FieldRepairKit_AbilityDef");

                Sprite nanotechFieldkitAbilityIcon = Helper.CreateSpriteFromImageFile("nanotechfieldkit.png");

                repairKit.ManufactureMaterials = 30;
                repairKit.ManufactureTech = 10;


                //need to create a new heal ability

                string nameAbility = "DoTMedkit";
                string gUIDAbility = "{180418BE-8DC8-467E-80FE-D012A51BE5A9}";
                HealAbilityDef sourceHealAbility = DefCache.GetDef<HealAbilityDef>("Medkit_AbilityDef");
                HealAbilityDef newDoTMedkitAbility = Helper.CreateDefFromClone(sourceHealAbility, gUIDAbility, nameAbility);
                TacticalTargetingDataDef newTargetingData = Helper.CreateDefFromClone(sourceHealAbility.TargetingDataDef, "{CF5A9AD8-94A1-4DBE-9AF6-F90C56851437}", nameAbility);
                newDoTMedkitAbility.TargetingDataDef = newTargetingData;
                newTargetingData.Origin.TargetTags.Clear();



                newDoTMedkitAbility.ViewElementDef = Helper.CreateDefFromClone(sourceHealAbility.ViewElementDef, "{DB136772-7CDF-4FC4-B07B-72867E43E16E}", nameAbility);

                newDoTMedkitAbility.ViewElementDef.InventoryIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.LargeIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.SmallIcon = nanotechFieldkitAbilityIcon;
                newDoTMedkitAbility.ViewElementDef.DisplayName1.LocalizationKey = "KEY_REPAIR_KIT_ABILITY_NAME";
                newDoTMedkitAbility.ViewElementDef.Description.LocalizationKey = "KEY_REPAIR_KIT_ABILITY_DESCRIPTION";

                repairKit.Abilities[0] = newDoTMedkitAbility;


                //modify ability
                //need new MultiEffectDef. Copy from CureSpray, because it has everything we need.

                //Make Cure Spray/Cure Cloud remove acid
                string abilityName = "AcidStatusRemover";
                StatusRemoverEffectDef sourceStatusRemoverEffect = DefCache.GetDef<StatusRemoverEffectDef>("StrainedRemover_EffectDef");
                StatusRemoverEffectDef newAcidStatusRemoverEffect = Helper.CreateDefFromClone(sourceStatusRemoverEffect, "0AE26C25-A67D-4F2F-B036-F7649B26B695", abilityName);
                newAcidStatusRemoverEffect.StatusToRemove = "Acid";



                string nameMultiEffect = "DoTMedkitMultiEffect";
                string gUIDMultiEffect = "{5B0EBBAE-F126-418C-B6F2-7E2FA44EBFBD}";
                MultiEffectDef sourceMultiEffect = DefCache.GetDef<MultiEffectDef>("Cure_MultiEffectDef");
                MultiEffectDef newMultiEffect = Helper.CreateDefFromClone(sourceMultiEffect, gUIDMultiEffect, nameMultiEffect);

                List<EffectDef> effectDefsList = newMultiEffect.EffectDefs.ToList();
                effectDefsList.Add(newAcidStatusRemoverEffect);
                newMultiEffect.EffectDefs = effectDefsList.ToArray();

                //  TFTVLogger.Always($"{newMultiEffect.EffectDefs.Count()}");

                OrEffectConditionDef sourceOrEffectCondition = DefCache.GetDef<OrEffectConditionDef>("CanBeHealed_StandardMedkit_ApplicationCondition");

                OrEffectConditionDef newEffectCondtions = Helper.CreateDefFromClone(sourceOrEffectCondition, "{ECFC2136-17BA-4FD0-A5BA-B9A1C456353E}", "DoTMedkitEffectCondtiions");

                newEffectCondtions.OrConditions = new EffectConditionDef[]
                {
                TFTVCommonMethods.CreateNewStatusEffectCondition("{0D32B04B-8EAA-4C76-9F24-F92F0FE8CD74}", DefCache.GetDef<StatusDef>("ActorStunned_StatusDef")),//need to replace with new status
                TFTVCommonMethods.CreateNewStatusEffectCondition("{BF5726D7-5E9C-4145-85E8-79545CBB3261}", DefCache.GetDef<StatusDef>("Acid_StatusDef")),
               TFTVCommonMethods.CreateNewStatusEffectCondition("{177E042A-B8F8-4302-9520-CC0610C045B0}", DefCache.GetDef<StatusDef>("Blinded_StatusDef")),
                TFTVCommonMethods.CreateNewStatusEffectCondition("{A054A669-8C7B-4005-8749-BA6CD71163CA}", DefCache.GetDef<StatusDef>("Slowed_StatusDef")),
               TFTVCommonMethods.CreateNewStatusEffectCondition("{F574791A-FAD0-4E1F-9295-5F2A3D9AAB2C}", DefCache.GetDef<StatusDef>("Trembling_StatusDef")),
                TFTVCommonMethods.CreateNewStatusEffectCondition("{A66DC742-B60F-409B-8B63-2D6AC7B5AD1D}", DefCache.GetDef<StatusDef>("Bleed_StatusDef")),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasParalysisStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasParalysedStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasInfectedStatus_ApplicationCondition"),
                DefCache.GetDef< ActorHasStatusEffectConditionDef>("HasPoisonStatus_ApplicationCondition"),


            };

                LocalizedTextBind nanotTechDescription = new LocalizedTextBind("KEY_REPAIR_KIT_ABILITY_DESCRIPTION");

                // string effectDescriptionText = "Removes all acid, bleeding, blind, paralyzed, poisoned, slowed, stun, trembling, viral status from the target.";
                ConditionalHealEffect conditionalHealEffect = new ConditionalHealEffect()

                {
                    HealerConditions = new EffectConditionDef[] { },
                    TargetGenerationConditions = new EffectConditionDef[] { newEffectCondtions },
                    AdditionalEffectDef = newMultiEffect,
                    EffectDescription = nanotTechDescription
                };

                newDoTMedkitAbility.HealEffects = new List<ConditionalHealEffect>() { conditionalHealEffect };
                newDoTMedkitAbility.GeneralHealAmount = 0.1f;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void CreateHealingMultiplierAbility()
        {
            try
            {
                DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef healingMultiplierAbility = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "{39D33BA7-726A-417F-9DC7-42CD4E6762FD}", "ExtraHealing_DamageMultiplierAbilityDef");
                healingMultiplierAbility.DamageTypeDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Healing_StandardDamageTypeEffectDef");
                healingMultiplierAbility.Multiplier = 1.25f;
                healingMultiplierAbility.MultiplierType = DamageMultiplierType.Incoming;
                healingMultiplierAbility.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "{63C00610-5CAE-4152-9002-7A0F7C90AE30}", "ExtraHealing_ViewElementDef");
                healingMultiplierAbility.ViewElementDef.DisplayName1.LocalizationKey = "EXTRAHEALING_NAME";
                healingMultiplierAbility.ViewElementDef.Description.LocalizationKey = "EXTRAHEALING_DESCRIPTION";
                healingMultiplierAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_ExpertHealer-2.png");
                healingMultiplierAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_PersonalTrack_ExpertHealer-2.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateParalysisDamageResistance()
        {
            try
            {
                DamageMultiplierAbilityDef damageMultiplierAbilityDefSource = DefCache.GetDef<DamageMultiplierAbilityDef>("EMPResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef ParalysisNotShcokResistance = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource, "{A044047F-A462-46FC-B06A-191181B67800}", "ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");
                ParalysisNotShcokResistance.DamageTypeDef = DefCache.GetDef<DamageOverTimeDamageTypeEffectDef>("Paralysis_DamageOverTimeDamageTypeEffectDef");
                ParalysisNotShcokResistance.Multiplier = 0.5f;
                ParalysisNotShcokResistance.ViewElementDef = Helper.CreateDefFromClone(damageMultiplierAbilityDefSource.ViewElementDef, "{F157A5A2-16A0-491A-ABE8-6CF88DEBE1DF}", "ParalysisNotShockImmunityResistance_ViewElementDef");
                ParalysisNotShcokResistance.ViewElementDef.DisplayName1.LocalizationKey = "RESISTANCE_TO_PARALYSIS_NAME";
                ParalysisNotShcokResistance.ViewElementDef.Description.LocalizationKey = "RESISTANCE_TO_PARALYSIS_DESCRIPTION";
                ParalysisNotShcokResistance.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                ParalysisNotShcokResistance.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ParalysisImmunity.png");
                // TFTVAircraftRework.ParalysysVestResistance = ParalysisNotShcokResistance;
                TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add(ParalysisNotShcokResistance);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void ModifyPoisonResVest()
        {
            try
            {
                TacticalItemDef poisonVest = DefCache.GetDef<TacticalItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef");
                DamageMultiplierAbilityDef ParalysisNotShcokResistance = DefCache.GetDef<DamageMultiplierAbilityDef>("ParalysisNotShockImmunityResistance_DamageMultiplierAbilityDef");

                //Not working correctly
                //DamageMultiplierAbilityDef ExtraHealing = DefCache.GetDef<DamageMultiplierAbilityDef>("ExtraHealing_DamageMultiplierAbilityDef");

                poisonVest.Abilities = new AbilityDef[] { poisonVest.Abilities[0], ParalysisNotShcokResistance };
                // TFTVAircraftRework.PoisonVestResistance = (DamageMultiplierAbilityDef)poisonVest.Abilities[0];
                TFTVAircraftReworkMain.VestResistanceMultiplierAbilities.Add((DamageMultiplierAbilityDef)poisonVest.Abilities[0]);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        internal static void RemoveAcidAsVulnerability()
        {
            try
            {

                DefCache.GetDef<DamageMultiplierStatusDef>("BionicVulnerabilities_StatusDef").DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                DefCache.GetDef<DamageMultiplierStatusDef>("BionicVulnerabilities_StatusDef").Multiplier = 1;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }



        }

        internal static void AdjustResearches()
        {
            try
            {
                ResearchDef terrorSentinelResearch = DefCache.GetDef<ResearchDef>("PX_Alien_TerrorSentinel_ResearchDef");
                ManufactureResearchRewardDef advNanotechRewards = DefCache.GetDef<ManufactureResearchRewardDef>("SYN_NanoTech_ResearchDef_ManufactureResearchRewardDef_0");
                ManufactureResearchRewardDef newRewardsForTerrorSentinel = Helper.CreateDefFromClone(advNanotechRewards, "{41636380-9889-4D4A-8E0A-8D32A9196DD1}", terrorSentinelResearch.name + "ManuReward");

                ResearchDef reverseEngineeringMVS = DefCache.GetDef<ResearchDef>("PX_SY_MultiVisualSensor_Attachment_ItemDef_ResearchDef");

                ResearchDef reverseEngineeringMotionDetector = DefCache.GetDef<ResearchDef>("PX_SY_MotionDetector_Attachment_ItemDef_ResearchDef");

                ResearchDef reverseEngineeringAcidVest = DefCache.GetDef<ResearchDef>("PX_NJ_FireResistanceVest_Attachment_ItemDef_ResearchDef");

                ResearchDbDef pxResearch = DefCache.GetDef<ResearchDbDef>("pp_ResearchDB");

                if (pxResearch.Researches.Contains(reverseEngineeringMVS))
                {
                    pxResearch.Researches.Remove(reverseEngineeringMVS);
                }

                if (pxResearch.Researches.Contains(reverseEngineeringMotionDetector))
                {
                    pxResearch.Researches.Remove(reverseEngineeringMotionDetector);
                }

                if (pxResearch.Researches.Contains(reverseEngineeringAcidVest))
                {
                    pxResearch.Researches.Remove(reverseEngineeringAcidVest);
                }

                //Moving Motion Detection Module to Terror Sentinel Autopsy               
                terrorSentinelResearch.Unlocks = new ResearchRewardDef[] { terrorSentinelResearch.Unlocks[0], newRewardsForTerrorSentinel };

                //Remove adv nanotech buff and add Repair Kit to manufacturing reward

                ResearchDef advNanotechRes = DefCache.GetDef<ResearchDef>("SYN_NanoTech_ResearchDef");
                //  advNanotechRes.ViewElementDef.BenefitsText = new LocalizedTextBind() { }; // DefCache.GetDef<ResearchViewElementDef>("PX_ShardGun_ViewElementDef").BenefitsText;
                advNanotechRes.Unlocks = new ResearchRewardDef[] { advNanotechRes.Unlocks[0] };

                EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");


                //removing nanokit from Bionic Reserach
                ManufactureResearchRewardDef bionicsReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_Bionics1_ResearchDef_ManufactureResearchRewardDef_0");
                bionicsReward.Items = new ItemDef[] { bionicsReward.Items[0], bionicsReward.Items[1], bionicsReward.Items[2] };


                TacticalItemDef newNanoVest = DefCache.GetDef<TacticalItemDef>("NanotechVest");

                List<ItemDef> manuRewards = new List<ItemDef>() { repairKit, newNanoVest };
                advNanotechRewards.Items = manuRewards.ToArray();

                TacticalItemDef blastVest = DefCache.GetDef<TacticalItemDef>("PX_BlastResistanceVest_Attachment_ItemDef");
                ManufactureResearchRewardDef njFireReward = DefCache.GetDef<ManufactureResearchRewardDef>("NJ_PurificationTech_ResearchDef_ManufactureResearchRewardDef_0");
                List<ItemDef> itemDefs = new List<ItemDef>(njFireReward.Items) { blastVest };
                njFireReward.Items = itemDefs.ToArray();
                //remove NJ Fire Resistance tech, folding it into fire tech

                ResearchDef fireTech = DefCache.GetDef<ResearchDef>("NJ_PurificationTech_ResearchDef");

                /* List<ResearchRewardDef> fireTechRewards = fireTech.Unlocks.ToList();
                 fireTechRewards.Add(njFireResReward);
                 fireTech.Unlocks = fireTechRewards.ToArray();*/

                ResearchDbDef njResearch = DefCache.GetDef<ResearchDbDef>("nj_ResearchDB");
                njResearch.Researches.Remove(DefCache.GetDef<ResearchDef>("NJ_FireResistanceTech_ResearchDef"));

                //Fireworm unlocks Vidar
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_AGL_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Fireworm_ResearchDef";

                //Blast res research changed to acid res, because blast vest moved to NJ Fire Tech research
                //Acidworm unlocks BlastResTech, which is now AcidResTech
                TacticalItemDef acidVest = DefCache.GetDef<TacticalItemDef>("NJ_FireResistanceVest_Attachment_ItemDef");
                ManufactureResearchRewardDef pxBlastResReward = DefCache.GetDef<ManufactureResearchRewardDef>("PX_BlastResistanceVest_ResearchDef_ManufactureResearchRewardDef_0");
                pxBlastResReward.Items = new ItemDef[] { acidVest };
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_BlastResistanceVest_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = "PX_Alien_Acidworm_ResearchDef";

                DefCache.GetDef<ResearchDef>("PX_Alien_Acidworm_ResearchDef").ViewElementDef.BenefitsText.LocalizationKey = "PX_ALIEN_ACIDWORM_RESEARCHDEF_BENEFITS";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        internal static void ChangeModulePictures()
        {
            try
            {


                TacticalItemDef nightVisionModule = DefCache.GetDef<TacticalItemDef>("SY_MultiVisualSensor_Attachment_ItemDef");
                nightVisionModule.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("modules_nightvision.png");
                nightVisionModule.ViewElementDef.InventoryIcon = nightVisionModule.ViewElementDef.LargeIcon;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }



        internal static void CreateAcidImmunity()
        {
            try
            {
                string abilityName = "AcidImmunityAbility";
                string gUID = "{4915CA1F-5DA2-4F7D-9455-BC775EA1D8CB}";
                // string characterProgressionGUID = "AA24A50E-C61A-4CD8-97FE-3F8BAC5F7BAA";
                string viewElementGUID = "{85B86FF6-3EB4-492A-9775-D01611DEDE5B}";

                DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);

                /*newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");*/
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACID_IMMUNITY_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "ACID_IMMUNITY_DESCRIPTION";
                newAbility.ViewElementDef.ShowInStatusScreen = true;
                newAbility.ViewElementDef.ShowInFreeAimMode = true;

                newAbility.Multiplier = 0.0f;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void FixPriestScream()
        {
            try
            {
                AIAbilityNumberOfTargetsConsiderationDef numberOfTargetsConsiderationDefSource = DefCache.GetDef<AIAbilityNumberOfTargetsConsiderationDef>("Siren_PsychicScreamNumberOfTargets_AIConsiderationDef");

                AIAbilityNumberOfTargetsConsiderationDef newNumberOfTargetsConsideration = Helper.CreateDefFromClone(numberOfTargetsConsiderationDefSource, "{EBF0A605-B3DA-45C8-88CD-8CB9832B584E}", "PsychicScreamNumberOfTargets_AIConsiderationDef");
                AIActionMoveAndExecuteAbilityDef priestMoveAndScreamAIAction = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MoveAndDoPsychicScream_AIActionDef");
                priestMoveAndScreamAIAction.Evaluations[0].Considerations[1].Consideration = newNumberOfTargetsConsideration;
                newNumberOfTargetsConsideration.Ability = priestMoveAndScreamAIAction.AbilityToExecute;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void ModifyChironWormAndAoETargeting()
        {
            try
            {
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [AreaStun_AbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [AreaStun_AbilityDef]").Origin.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchMortar_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchMortar_ShootAbilityDef]").Origin.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchGoo_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchPoisonWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchAcidWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [LaunchFireWorm_ShootAbilityDef]").Target.CullTargetTags = new GameTagsList { DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef") };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }



        }

        //Maybe will be used later

        internal static void CreateRoboticSelfRestoreAbility()
        {
            try
            {
                CreateRoboticHealingStatus();
                CreateRoboticHealingAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateRoboticHealingAbility()
        {
            try
            {

                string abilityGUID = "{5056F0F1-0FDE-4C5B-B69D-A436310CC72E}";

                string abilityName = "RoboticSelfRepair_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef ability = Helper.CreateDefFromClone(
                    source,
                   abilityGUID,
                    abilityName);
                ability.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "{76838266-6249-46AF-A541-66065F102BD5}",
                    abilityName);
                ability.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "{C464B230-D5D9-4798-A765-CF2398B3A49C}",
                    abilityName);
                ability.StatModifications = new ItemStatModification[] { };
                ability.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                ability.ViewElementDef.DisplayName1.LocalizationKey = "ROBOTIC_SELF_REPAIR_TITLE";
                ability.ViewElementDef.Description.LocalizationKey = "ROBOTIC_SELF_REPAIR_ABILITY_TEXT";

                Sprite icon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                ability.ViewElementDef.LargeIcon = icon;
                ability.ViewElementDef.SmallIcon = icon;

                TFTVAncients.SelfRepairAbility = ability;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateRoboticHealingStatus()
        {
            try
            {
                //Creating status effect to show that Guardian will repair a body part next turn. Need to create a status to show small icon.

                DamageMultiplierStatusDef sourceAbilityStatusDef = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                string statusSelfRepairAbilityName = "RoboticSelfRepair_AddAbilityStatusDef";
                DamageMultiplierStatusDef statusSelfRepairAbilityDef = Helper.CreateDefFromClone(sourceAbilityStatusDef, "609D0304-8BA3-4103-BC0D-6BE440E69F3D", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.EffectName = "SelfRoboticRepair";
                statusSelfRepairAbilityDef.ApplicationConditions = new EffectConditionDef[] { };
                statusSelfRepairAbilityDef.Visuals = Helper.CreateDefFromClone(sourceAbilityStatusDef.Visuals, "36414ABA-B535-4C4C-AADD-2F3A64D5101C", statusSelfRepairAbilityName);
                statusSelfRepairAbilityDef.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                statusSelfRepairAbilityDef.VisibleOnPassiveBar = true;
                statusSelfRepairAbilityDef.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                statusSelfRepairAbilityDef.Visuals.DisplayName1.LocalizationKey = "ROBOTIC_SELF_REPAIR_TITLE";
                statusSelfRepairAbilityDef.Visuals.Description.LocalizationKey = "ROBOTIC_SELF_REPAIR_TEXT_TEXT";
                statusSelfRepairAbilityDef.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusSelfRepairAbilityDef.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                statusSelfRepairAbilityDef.Multiplier = 1;

                TFTVAncients.RoboticSelfRepairStatus = statusSelfRepairAbilityDef;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void AddMissingElectronicTags()
        {
            try
            {
                ItemMaterialTagDef electronic = DefCache.GetDef<ItemMaterialTagDef>("Electronic_ItemMaterialTagDef");

                TacticalItemDef juggHead = DefCache.GetDef<TacticalItemDef>("NJ_Jugg_BIO_Helmet_BodyPartDef");

                foreach (TacticalItemDef tacticalItemDef in Repo.GetAllDefs<TacticalItemDef>().Where(ti => ti.Tags.Contains(Shared.SharedGameTags.BionicalTag)))
                {
                    if (tacticalItemDef.Tags.CanAdd(electronic))
                    {
                        tacticalItemDef.Tags.Add(electronic);
                        // TFTVLogger.Always($"added electronic tag to {tacticalItemDef.name}");

                    }

                }

                foreach (GroundVehicleWeaponDef groundVehicleWeaponDef in Repo.GetAllDefs<GroundVehicleWeaponDef>())
                {
                    if (groundVehicleWeaponDef.Tags.CanAdd(electronic))
                    {
                        groundVehicleWeaponDef.Tags.Add(electronic);
                        //   TFTVLogger.Always($"added electronic tag to {groundVehicleWeaponDef.name}");

                    }


                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void ModifyDecoyAbility()
        {
            try
            {
                SpawnActorAbilityDef decoyAbility = DefCache.GetDef<SpawnActorAbilityDef>("Decoy_AbilityDef");
                decoyAbility.UseSelfAsTemplate = false;

                TacCharacterDef dcoyTacCharacter = DefCache.GetDef<TacCharacterDef>("SY_Decoy_TacCharacterDef");

                ClassTagDef assaultClassTag = DefCache.GetDef<ClassTagDef>("Assault_ClassTagDef");
                ActorDeploymentTagDef deploymentTagDef = DefCache.GetDef<ActorDeploymentTagDef>("1x1_Grunt_DeploymentTagDef");


                GameTagDef decoyTag = TFTVCommonMethods.CreateNewTag("DecoyTag", "{55D78B77-AE12-452B-B3FB-BB559DDBF8AE}");

                WeaponDef ares = DefCache.GetDef<WeaponDef>("PX_AssaultRifle_WeaponDef");
                /*   string aresDummyName = "AresForDecoy";
                   WeaponDef aresDummy = Helper.CreateDefFromClone(ares, "{EF6669E4-4BFB-4A39-98DE-F6D6FB366BE9}", aresDummyName);
                   aresDummy.ViewElementDef = Helper.CreateDefFromClone(ares.ViewElementDef, "{B246C149-6D59-4EC8-A3E6-008A919AD2F6}", aresDummyName);
                   aresDummy.CrateSpawnWeight = 0;                
                   aresDummy.Tags.Add(decoyTag);
                */
                dcoyTacCharacter.Data.EquipmentItems = new ItemDef[] { ares };
                TacticalActorDef dcoy = DefCache.GetDef<TacticalActorDef>("Decoy_ActorDef");
                dcoy.EnduranceToHealthMultiplier = 20;

                List<GameTagDef> gameTagDefs = new List<GameTagDef>(dcoy.GameTags) { assaultClassTag, deploymentTagDef, decoyTag };

                dcoyTacCharacter.Data.GameTags = gameTagDefs.ToArray();
                //  OnActorDazedEffectStatus.ShouldApplyEffect

                TacticalNavigationComponentDef navigationSource = DefCache.GetDef<TacticalNavigationComponentDef>("Soldier_NavigationDef");
                //  navigationSource.CreateNavObstacle = false;

                TacticalNavigationComponentDef newNavigation = Helper.CreateDefFromClone(navigationSource, "{AAED2DCB-6269-42D0-ADCF-474576B16258}", "DecoyNavigationComponentDef");
                newNavigation.CreateNavObstacle = false;

                ComponentSetDef componentSetDef = DefCache.GetDef<ComponentSetDef>("Decoy_Template_ComponentSetDef");
                componentSetDef.Components[4] = newNavigation;

                RagdollDieAbilityDef dieAbilityDef = DefCache.GetDef<RagdollDieAbilityDef>("Decoy_Die_AbilityDef");
                dieAbilityDef.EventOnActivate = new TacticalEventDef();

                string hintDecoyPlacedName = "HintDecoyPlaced";
                string hintDecoyPlacedGUID = "{E86C3A8A-B3E3-4A52-9BEB-1FFFE1506F60}";
                string hintDecoyPlacedTitle = "HINT_DECOYPLACED_TITLE";
                string hintDecoyPlacedText = "HINT_DECOYPLACED_TEXT";

                TFTVHints.HintDefs.CreateNewTacticalHint(hintDecoyPlacedName, HintTrigger.AbilityExecuted, decoyAbility.name, hintDecoyPlacedTitle, hintDecoyPlacedText, 4, true, hintDecoyPlacedGUID, "decoy_hint.jpg");

                IsDefHintConditionDef conditionDef = DefCache.GetDef<IsDefHintConditionDef>(decoyAbility.name + "_HintConditionDef");
                conditionDef.TargetDef = decoyAbility;

                string hintDecoyDiscoveredName = "HintDecoyDiscovered";
                string hintDecoyDiscoveredGUID = "{D75AC0EA-89C1-4DF7-8E67-CFD83F8F6ED1}";
                string hintDecoyDiscoveredTitle = "HINT_DECOYDISCOVERED_TITLE";
                string hintDecoyDiscoveredText = "HINT_DECOYDISCOVERED_TEXT";
                TFTVHints.HintDefs.CreateNewTacticalHint(hintDecoyDiscoveredName, HintTrigger.Manual, decoyTag.name, hintDecoyDiscoveredTitle, hintDecoyDiscoveredText, 1, true, hintDecoyDiscoveredGUID, "decoy_removed_hint.jpg");

                string hintDecoyScyllaName = "HintDecoyScylla";
                string hintDecoyScyllaGUID = "{06D96E1B-758C-4178-9D9B-13A40686E90F}";
                string hintDecoyScyllaTitle = "HINT_DECOYSCYLLA_TITLE";
                string hintDecoyScyllaText = "HINT_DECOYSCYLLA_TEXT";
                TFTVHints.HintDefs.CreateNewTacticalHint(hintDecoyScyllaName, HintTrigger.ActorDied, dcoyTacCharacter.name, hintDecoyScyllaTitle, hintDecoyScyllaText, 0, true, hintDecoyScyllaGUID, "decoy_removed_hint.jpg");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }




        internal static void ScyllaAcheronsChironsAndCyclops()
        {
            try
            {
                CreateAcidImmunity();
                ChangesToAcherons();
                ModifyPalaceGuardians();
                ModifyScyllaAIAndHeads();
                MedAndBigMonstersSquishers();
                ModifyGuardianAIandStomp();
                CreateNewScreamForCyclops();
                ChangeHeavyLegsScyllaAbdomen();
                //  MakeUmbraNotObstacle();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static void ChangeHeavyLegsScyllaAbdomen()
        {
            try
            {
                TacCharacterDef scylla6 = DefCache.GetDef<TacCharacterDef>("Scylla6_FrenzyArmorSmashHeavySpawn_AlienMutationVariationDef");
                scylla6.Data.BodypartItems[5] = DefCache.GetDef<TacticalItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateNewScreamForCyclops()
        {
            try
            {
                string abilityName = "CyclopsScream";

                ApplyDamageEffectAbilityDef guardianStomp = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Guardian_Stomp_AbilityDef");
                PsychicScreamAbilityDef screamAbilitySource = DefCache.GetDef<PsychicScreamAbilityDef>("Siren_PsychicScream_AbilityDef");
                //blast damage is limited by scenery 
                PsychicScreamAbilityDef newScreamAbility = Helper.CreateDefFromClone(screamAbilitySource, "{7CD25D07-441C-4E57-8680-FB7B06E9DDE5}", abilityName);
                newScreamAbility.ViewElementDef = Helper.CreateDefFromClone(screamAbilitySource.ViewElementDef, "{86253E40-9034-4089-A71E-1C1D78B28ECE}", abilityName);
                newScreamAbility.TargetingDataDef = Helper.CreateDefFromClone(screamAbilitySource.TargetingDataDef, "{495F4879-654D-49D9-A08A-16C752B98DEC}", abilityName);
                newScreamAbility.DamagePayload.AoeRadius = 7;
                newScreamAbility.TargetingDataDef.Origin.Range = 7;

                newScreamAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ANC_CyclopsScream.png");
                newScreamAbility.ViewElementDef.SmallIcon = Helper.CreateSpriteFromImageFile("ANC_CyclopsScream.png");
                newScreamAbility.ViewElementDef.DisplayName1.LocalizationKey = "CYCLOPS_SCREAM_TITLE";
                newScreamAbility.ViewElementDef.Description.LocalizationKey = "CYCLOPS_SCREAM_DESCRIPTION";

                //Only to show in the UI
                newScreamAbility.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
                {
                    new DamageKeywordPair { DamageKeywordDef = Shared.SharedDamageKeywords.PsychicKeyword, Value = 1 },
                };


                AIAbilityNumberOfTargetsConsiderationDef numberOfTargetsConsiderationDefSource = DefCache.GetDef<AIAbilityNumberOfTargetsConsiderationDef>("Siren_PsychicScreamNumberOfTargets_AIConsiderationDef");

                AIAbilityNumberOfTargetsConsiderationDef newNumberOfTargetsConsideration = Helper.CreateDefFromClone(numberOfTargetsConsiderationDefSource, "{5C5D22BC-0E48-4697-9525-0AA7BBE0D06B}", abilityName);
                newNumberOfTargetsConsideration.Ability = newScreamAbility;

                TacticalActorDef cyclops = DefCache.GetDef<TacticalActorDef>("MediumGuardian_ActorDef");
                List<AbilityDef> cyclopsAbilities = new List<AbilityDef>(cyclops.Abilities.ToList());
                cyclopsAbilities.Remove(guardianStomp);
                cyclopsAbilities.Add(newScreamAbility);
                cyclops.Abilities = cyclopsAbilities.ToArray();

                newScreamAbility.ActionPointCost = 0.5f;
                newScreamAbility.WillPointCost = 0.0f;
                newScreamAbility.UsesPerTurn = 1;



                DefCache.GetDef<TacActorSimpleAbilityAnimActionDef>("E_Stomp [MediumGuardian_AnimActionsDef]").AbilityDefs = new AbilityDef[] { newScreamAbility };

                AIActionMoveAndExecuteAbilityDef moveAndStompAIAction = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("MediumGuardian_MoveAndDoStomp_AIActionDef");
                moveAndStompAIAction.AbilityToExecute = newScreamAbility;
                moveAndStompAIAction.Evaluations[0].Considerations[0].Consideration = newNumberOfTargetsConsideration;
                moveAndStompAIAction.Weight = 20.0f;

                DefCache.GetDef<AIAttackPositionConsiderationDef>("MediumGuardian_StompAttackPosition_AIConsiderationDef").AbilityDef = newScreamAbility;

                DefCache.GetDef<AIAbilityDisabledStateConsiderationDef>("MediumGuardian_StompAbilityEnabled_AIConsiderationDef").Ability = newScreamAbility;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        internal static void ModifyGuardianAIandStomp()
        {
            try
            {

                AIActorMovementZoneTargetGeneratorDef movementZoneTargetGeneratorDef = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_MovementZone_AITargetGeneratorDef");
                AIActorMovementZoneTargetGeneratorDef halfZone = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_ActionZoneHalf_AITargetGeneratorDef");
                AIActorMovementZoneTargetGeneratorDef actionZone = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_ActionZone_AITargetGeneratorDef");


                //   DefCache.GetDef<ApplyDamageEffectAbilityDef>("Guardian_Stomp_AbilityDef").IgnoreFriendlies = true;
                AIActionsTemplateDef mediumGuardianAITemplate = DefCache.GetDef<AIActionsTemplateDef>("MediumGuardian_AIActionsTemplateDef");

                AIActionDef queenAdvance = DefCache.GetDef<AIActionDef>("Queen_Advance_AIActionDef");
                AIActionDef guardianAdvance = Helper.CreateDefFromClone(queenAdvance, "{BC0497A4-ED7A-427C-910F-35B453B5F205}", "Guardian_Advance_AIActionDef");
                guardianAdvance.Weight = 1.0f;


                guardianAdvance.Evaluations[0].TargetGeneratorDef = DefCache.GetDef<AIActorMovementZoneTargetGeneratorDef>("MediumGuardian_MovementZone_AITargetGeneratorDef");

                List<AIActionDef> aIActions = new List<AIActionDef>(mediumGuardianAITemplate.ActionDefs.ToList())
                {
                    guardianAdvance
                };
                mediumGuardianAITemplate.ActionDefs = aIActions.ToArray();

                TacAIActorDef cyclopsAIActor = DefCache.GetDef<TacAIActorDef>("MediumGuardian_AIActorDef");
                cyclopsAIActor.TurnOrderPriority = 1000;

                AIActionDef aggresiveAdvance = DefCache.GetDef<AIActionDef>("MediumGuardian_Advance_Aggressive_AIActionDef");



                AIActionMoveAndAttackDef moveAndShootAIActionDef = DefCache.GetDef<AIActionMoveAndAttackDef>("MediumGuardian_MoveAndShoot_AIActionDef");


                moveAndShootAIActionDef.Evaluations[1].TargetGeneratorDef = actionZone;
                moveAndShootAIActionDef.Weight = 100.0f;
                aggresiveAdvance.Weight = 2.0f;


                CaterpillarMoveAbilityDef caterPillarMoveDef = DefCache.GetDef<CaterpillarMoveAbilityDef>("ScyllaSquisher");

                movementZoneTargetGeneratorDef.MoveAbilityDef = caterPillarMoveDef;
                halfZone.MoveAbilityDef = caterPillarMoveDef;
                actionZone.MoveAbilityDef = caterPillarMoveDef;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ModifyPalaceGuardians()
        {
            try
            {
                TacCharacterDef gatekeeper1 = DefCache.GetDef<TacCharacterDef>("Queen_Gatekeeper_TacCharacterDef");
                TacCharacterDef gatekeeper2 = DefCache.GetDef<TacCharacterDef>("Queen_Gatekeeper2_TacCharacterDef");

                WeaponDef scyllaCannon = DefCache.GetDef<WeaponDef>("Queen_Arms_Gun_WeaponDef");
                WeaponDef scyllaSmashers = DefCache.GetDef<WeaponDef>("Queen_Arms_Smashers_WeaponDef");
                ItemDef scyllaHeavyLegs = DefCache.GetDef<ItemDef>("Queen_Legs_Heavy_ItemDef");
                ItemDef scyllaAgileLegs = DefCache.GetDef<ItemDef>("Queen_Legs_Agile_ItemDef");
                ItemDef scyllaBelcherAbdomen = DefCache.GetDef<ItemDef>("Queen_Abdomen_Belcher_BodyPartDef");
                ItemDef scyllaSpawnerAbdomen = DefCache.GetDef<ItemDef>("Queen_Abdomen_Spawner_BodyPartDef");


                List<ItemDef> gateKeeper1BodyParts = new List<ItemDef>(gatekeeper1.Data.BodypartItems.ToList()) { scyllaCannon, scyllaBelcherAbdomen };
                gateKeeper1BodyParts.Remove(scyllaSmashers);
                gateKeeper1BodyParts.Remove(scyllaSpawnerAbdomen);

                gatekeeper1.Data.BodypartItems = gateKeeper1BodyParts.ToArray();
                gatekeeper1.Data.Speed = 6;

                List<ItemDef> gateKeeper2BodyParts = new List<ItemDef>(gatekeeper2.Data.BodypartItems.ToList()) { scyllaHeavyLegs, scyllaBelcherAbdomen };
                gateKeeper2BodyParts.Remove(scyllaAgileLegs);
                gateKeeper2BodyParts.Remove(scyllaSpawnerAbdomen);
                gatekeeper2.Data.BodypartItems = gateKeeper2BodyParts.ToArray();
                gatekeeper2.Data.Speed = 6;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ModifyScyllaAIAndHeads()
        {
            try
            {
                //  AIActionMoveAndExecuteAbilityDef queenMoveAndPrepareShooting = DefCache.GetDef<AIActionMoveAndExecuteAbilityDef>("Queen_MoveAndPrepareShooting_AIActionDef");
                //  queenMoveAndPrepareShooting.EarlyExitConsiderations = new AIAdjustedConsideration[] { };

                DefCache.GetDef<ItemSlotDef>("Queen_Abdomen_SlotDef").DisplayName.LocalizationKey = "KEY_ABDOMEN_NAME";

                AIAbilityDisabledStateConsiderationDef canUsePrepareShootConsideration = DefCache.GetDef<AIAbilityDisabledStateConsiderationDef>("Queen_CanUsePrepareShoot_AIConsiderationDef");
                canUsePrepareShootConsideration.IgnoredStates = canUsePrepareShootConsideration.IgnoredStates.AddItem("EquipmentNotSelected").ToArray();
                // DefCache.GetDef<AdditionalEffectShootAbilityDef>("Queen_GunsFire_ShootAbilityDef").ActionPointCost = 0.0f;
                DefCache.GetDef<StartPreparingShootAbilityDef>("Queen_StartPreparing_AbilityDef").UsableOnNonSelectedEquipment = true;

                WeaponDef queenLeftBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_LeftArmGun_WeaponDef");
                WeaponDef queenRightBlastWeapon = DefCache.GetDef<WeaponDef>("Queen_RightArmGun_WeaponDef");

                WeaponDef arms = DefCache.GetDef<WeaponDef>("Queen_Arms_Gun_WeaponDef");
                arms.DamagePayload.ObjectMultiplier = 5;

                queenRightBlastWeapon.DamagePayload.ProjectileVisuals = queenLeftBlastWeapon.DamagePayload.ProjectileVisuals;

                WeaponDef headSpitter = DefCache.GetDef<WeaponDef>("Queen_Head_Spitter_Goo_WeaponDef");
                // DamageKeywordDef blast = DefCache.GetDef<DamageKeywordDef>("Blast_DamageKeywordDataDef");
                StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                //switch to blast damage from goo damage
                headSpitter.DamagePayload.DamageType = blastDamage;
                //increase blast annd poison damage to 40 from 30
                headSpitter.DamagePayload.DamageKeywords[0].Value = 40;
                headSpitter.DamagePayload.DamageKeywords[2].Value = 40;
                //shouldn't make a difference
                headSpitter.DamagePayload.AoeRadius = 2f;

                //Reduce Move and SpitGoo/SonicBlast weight, so she also uses Smashers sometimes
                DefCache.GetDef<AIActionDef>("Queen_MoveAndSpitGoo_AIActionDef").Weight = 50.0f;
                DefCache.GetDef<AIActionDef>("Queen_MoveAndSonicBlast_AIActionDef").Weight = 50.0f;
                DefCache.GetDef<AIActionDef>("Queen_MoveAndPrepareShooting_AIActionDef").Weight = 10.0f;


                //Reduce range of Sonic and Spitter Heads from 20 to 15 so that cannons are more effective
                WeaponDef headSonic = DefCache.GetDef<WeaponDef>("Queen_Head_Sonic_WeaponDef");
                headSpitter.DamagePayload.Range = 15;
                headSonic.DamagePayload.Range = 15;

                DefCache.GetDef<AIActionDef>("Queen_Recover_AIActionDef").Weight = 0.01f;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void MedAndBigMonstersSquishers()
        {
            try
            {
                //Create new Caterpillar ability to (eventually) show different View elements (for now just hidden) 
                string abilityName = "ScyllaSquisher";
                string abilityGUID = "{B7EBE715-69CE-4163-8E7D-88034ED4DE2A}";
                // string viewElementGUID = "{C74C16D0-98DB-4717-B5E8-D04004151A69}";
                CaterpillarMoveAbilityDef source = DefCache.GetDef<CaterpillarMoveAbilityDef>("CaterpillarMoveAbilityDef");
                CaterpillarMoveAbilityDef scyllaCaterpillarAbility = Helper.CreateDefFromClone(source, abilityGUID, abilityName);
                scyllaCaterpillarAbility.ViewElementDef = (TacticalAbilityViewElementDef)Repo.GetDef("6333fa2e-6e95-8124-48ea-8f7a60a2e22c"); //"Move_AbilityViewDef" //Helper.CreateDefFromClone(source.ViewElementDef, viewElementGUID, abilityName);
                                                                                                                                              //  scyllaCaterpillarAbility.ViewElementDef.ShowInStatusScreen = false;

                //Make all small critters and things not an obstacle for Scylla, MedMonster (Chiron, Cyclops), Acheron movement

                TacticalNavigationComponentDef spiderDroneNav = DefCache.GetDef<TacticalNavigationComponentDef>("SpiderDrone_NavigationDef");
                spiderDroneNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef wormNav = DefCache.GetDef<TacticalNavigationComponentDef>("Fireworm_NavigationDef");
                wormNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef faceHuggerNav = DefCache.GetDef<TacticalNavigationComponentDef>("Facehugger_NavigationDef");
                faceHuggerNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef swarmerNav = DefCache.GetDef<TacticalNavigationComponentDef>("Swarmer_NavigationDef");
                swarmerNav.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret1 = DefCache.GetDef<TacticalNavigationComponentDef>("NJ_TechTurret_NavigationDef");
                turret1.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret2 = DefCache.GetDef<TacticalNavigationComponentDef>("NJ_PRCRTechTurret_NavigationDef");
                turret2.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };

                TacticalNavigationComponentDef turret3 = DefCache.GetDef<TacticalNavigationComponentDef>("PX_LaserTechTurret_NavigationDef");
                turret3.ignoreObstacleFor = new string[] { "BigMonster", "MedMonster", "ArmadilloWormsDestroyer", "MedMonsterHigh" };


                TacticalAbilityDef fireImmunity = DefCache.GetDef<TacticalAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                TacticalAbilityDef poisonImmunity = DefCache.GetDef<TacticalAbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                TacticalAbilityDef acidImmunity = DefCache.GetDef<TacticalAbilityDef>("AcidImmunityAbility");

                //Scylla and Chirons with Heavy Legs, as well as all Cyclops get caterpillar ability + fire immunity
                foreach (TacCharacterDef tacCharacterDef in Repo.GetAllDefs<TacCharacterDef>())
                {
                    //  TFTVLogger.Always($"{tacCharacterDef.name}");
                    List<TacticalAbilityDef> monsterAbilities = new List<TacticalAbilityDef>(tacCharacterDef.Data.Abilites.ToList());

                    if (
                        (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<TacticalItemDef>("Queen_Legs_Heavy_ItemDef")))
                        || (tacCharacterDef.name.Contains("Chiron")
                        && !tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<TacticalItemDef>("Chiron_Legs_Agile_ItemDef")))
                        || tacCharacterDef.name.StartsWith("MediumGuardian")

                        )
                    {
                        //   TFTVLogger.Always($"adding caterpillar ability to {tacCharacterDef.name}");
                        monsterAbilities.Add(scyllaCaterpillarAbility);
                        monsterAbilities.Add(fireImmunity);
                    }

                    if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_FireWorm_Launcher_WeaponDef")))
                    {


                        if (!monsterAbilities.Contains(fireImmunity))
                        {
                            monsterAbilities.Add(fireImmunity);
                        }

                    }
                    else if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_PoisonWorm_Launcher_WeaponDef")))
                    {
                        monsterAbilities.Add(poisonImmunity);

                    }
                    else if (tacCharacterDef.Data.BodypartItems.Contains(DefCache.GetDef<WeaponDef>("Chiron_Abdomen_AcidWorm_Launcher_WeaponDef")))
                    {
                        monsterAbilities.Add(acidImmunity);

                    }


                    tacCharacterDef.Data.Abilites = monsterAbilities.ToArray();



                }

                //ensure that small critters and things have the damagedByCaterpillarTracks_Tag; if check in case BetterEnemies is active
                GameTagDef damagedByCaterpillar = DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef");

                foreach (TacticalActorDef actor in Repo.GetAllDefs<TacticalActorDef>().Where(a => a.name.Contains("worm") || a.name.Contains("SpiderDrone") || a.name.Contains("TechTurret")))
                {
                    if (!actor.GameTags.Contains(damagedByCaterpillar))
                    {
                        actor.GameTags.Add(damagedByCaterpillar);
                    }
                }

                TacticalDemolitionComponentDef demoCyclops = DefCache.GetDef<TacticalDemolitionComponentDef>("MediumGuardian_DemolitionComponentDef");

                //increase size of demo rectangle to squash worms and stuff
                demoCyclops.RectangleSize = new Vector3
                {
                    x = 2.5f,
                    y = 2.6f,
                    z = 2.9f,
                };

                TacticalDemolitionComponentDef demoChiron = DefCache.GetDef<TacticalDemolitionComponentDef>("Chiron_DemolitionComponentDef");

                demoChiron.SphereCenter = new Vector3(0f, 0f, 0f);
                demoChiron.CapsuleStart = new Vector3(0f, 1f, 0f);
                demoChiron.CapsuleEnd = new Vector3(0f, 3f, 0f);




                //improve special Scylla cheat skill to avoid getting damaged from squishing things that blow up

                DamageMultiplierStatusDef scyllaImmunitySource = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [Queen_GunsFire_ShootAbilityDef]");
                DamageMultiplierStatusDef newScyllaImmunityStatus = Helper.CreateDefFromClone(scyllaImmunitySource, "{D4CF7113-AF7D-42CA-BBF6-2CB06B8DB31E}", abilityName);

                TacStatusEffectDef scyllaImmunityEffect = DefCache.GetDef<TacStatusEffectDef>("E_MakeImmuneToBlastDamageEffect [Queen_GunsFire_ShootAbilityDef]");
                TacStatusEffectDef newScyllaImmunityEffect = Helper.CreateDefFromClone(scyllaImmunityEffect, "{6FDC9695-F561-446E-9D8F-D9AF29A35F0F}", abilityName);

                newScyllaImmunityEffect.StatusDef = newScyllaImmunityStatus;


                // not good, makes them immune to fire...
                /*   StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef"); 
                   AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                   DamageMultiplierStatusDef scyllaCheatSkill = DefCache.GetDef<DamageMultiplierStatusDef>("E_BlastImmunityStatus [Queen_GunsFire_ShootAbilityDef]");

                   List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>(scyllaCheatSkill.DamageTypeDefs) { fireDamage, acidDamage };
                   scyllaCheatSkill.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();*/




            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ModifyCratesToAddArmor()
        {
            try
            {
                GameTagDef armourTag = DefCache.GetDef<GameTagDef>("ArmourItem_TagDef");
                GameTagDef synedrion = DefCache.GetDef<GameTagDef>("Synedrion_FactionTagDef");
                GameTagDef anu = DefCache.GetDef<GameTagDef>("Anu_FactionTagDef");
                GameTagDef nj = DefCache.GetDef<GameTagDef>("NewJerico_FactionTagDef");

                List<ItemDef> synArmors = new List<ItemDef>() {

                DefCache.GetDef<ItemDef>("SY_MistRepeller_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_MotionDetector_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_MultiVisualSensor_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_PoisonResistanceVest_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_Neon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_Neon_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_Neon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_WhiteNeon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_WhiteNeon_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_WhiteNeon_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Infiltrator_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("SY_Sniper_Torso_BodyPartDef"),
                };

                List<ItemDef> njArmors = new List<ItemDef>() {
                DefCache.GetDef<ItemDef>("NJ_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Heavy_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Sniper_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_FireResistanceVest_Attachment_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Helmet_ALN_BodyPartDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Legs_ALN_ItemDef"),
                DefCache.GetDef<ItemDef>("NJ_Technician_Torso_ALN_BodyPartDef"),
                };

                List<ItemDef> anuArmors = new List<ItemDef>() {
                DefCache.GetDef<ItemDef>("AN_Berserker_Helmet_Viking_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Legs_Viking_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Torso_Viking_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Assault_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Helmet_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Berserker_Torso_BodyPartDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Legs_ItemDef"),
                DefCache.GetDef<ItemDef>("AN_Priest_Torso_BodyPartDef"),
                };

                foreach (TacticalItemDef item in anuArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;
                }

                foreach (TacticalItemDef item in njArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;
                }

                foreach (TacticalItemDef item in synArmors)
                {
                    item.CrateSpawnWeight = 200;
                    item.IsPickable = true;

                }

                InventoryComponentDef anuCrates = DefCache.GetDef<InventoryComponentDef>("Crate_AN_InventoryComponentDef");
                anuCrates.ItemDefs.AddRangeToArray(anuArmors.ToArray());


                InventoryComponentDef njCrates = DefCache.GetDef<InventoryComponentDef>("Crate_NJ_InventoryComponentDef");
                njCrates.ItemDefs.AddRangeToArray(njArmors.ToArray());

                InventoryComponentDef synCrates = DefCache.GetDef<InventoryComponentDef>("Crate_SY_InventoryComponentDef");
                synCrates.ItemDefs.AddRangeToArray(synArmors.ToArray());

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        public static void AddLoadingScreens()
        {
            try
            {
                LoadingScreenArtCollectionDef loadingScreenArtCollectionDef = DefCache.GetDef<LoadingScreenArtCollectionDef>("LoadingScreenArtCollectionDef");

                Sprite forsaken = Helper.CreateSpriteFromImageFile("fo_squad.jpg");
                Sprite pure = Helper.CreateSpriteFromImageFile("squad_pu.jpg");

                loadingScreenArtCollectionDef.LoadingScreenImages.Clear();

                /*    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisAnu_1_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisNJ_1_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisNJ_2_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisOther_1_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("CrisisSyn_1_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("DebateNJ_1_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_1_scarab_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_2_armadillo_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_3_aspida_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("Encounter_4_Kaos_Buggy_uinomipmaps.jpg"));
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile("UI_KaosMarket_Image_uinomipmaps.jpg"));*/

                for (int i = 1; i <= 25; i++)
                {
                    string fileName = $"loading_screen{i}.jpg";
                    loadingScreenArtCollectionDef.LoadingScreenImages.Add(Helper.CreateSpriteFromImageFile(fileName));
                }


                loadingScreenArtCollectionDef.LoadingScreenImages.Add(forsaken);
                loadingScreenArtCollectionDef.LoadingScreenImages.Add(pure);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddTips()
        {
            try
            {
                LoadingTipsRepositoryDef loadingTipsRepositoryDef = DefCache.GetDef<LoadingTipsRepositoryDef>("LoadingTipsRepositoryDef");
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_1" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_2" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_3" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_4" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_5" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_6" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_7" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_8" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_9" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_10" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_11" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_12" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_13" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_14" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_15" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_16" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_17" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_18" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_19" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_20" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_21" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_22" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_23" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_24" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_25" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_26" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_27" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_28" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_29" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_30" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_31" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_32" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_33" });
                loadingTipsRepositoryDef.GeoscapeLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_GEOSCAPE_34" });

                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_1" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_2" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_3" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_4" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_5" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_6" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_7" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_8" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_9" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_10" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_11" });
                loadingTipsRepositoryDef.TacticalLoadingTips.Add(new LocalizedTextBind() { LocalizationKey = "TFTV_TIP_TACTICAL_12" });
                loadingTipsRepositoryDef.TacticalLoadingTips[3].LocalizationKey = "KEY_TACTICAL_LOADING_TIP_4_TFTV";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void FixSpikeShootingArmShootingWhenDisabled()
        {
            try
            {


                UnusableHandStatusDef unUsableLeftHandStatus = DefCache.GetDef<UnusableHandStatusDef>("UnusableLeftHand_StatusDef");

                string statusName = "BrokenSpikeShooterStatus";

                DamageMultiplierStatusDef sourceStatus = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                DamageMultiplierStatusDef newBrokenSpikeShooterStatus = Helper.CreateDefFromClone(sourceStatus
                    ,
                    "D3B1B274-C7D0-4655-BC38-3B1747C15888",
                    statusName);

                newBrokenSpikeShooterStatus.EffectName = "BrokenSpikeShooter";
                newBrokenSpikeShooterStatus.DamageTypeDefs = new DamageTypeBaseEffectDef[] { };
                newBrokenSpikeShooterStatus.Visuals = Helper.CreateDefFromClone(sourceStatus.Visuals, "{F6D6A97E-B658-4B6B-AAAF-C517CAB3AB9C}", statusName);
                newBrokenSpikeShooterStatus.Multiplier = 1.0f;

                string statusApplicationConditionName = "NoBrokenSpikeShooterStatusCondition";

                ActorHasStatusEffectConditionDef sourceApplicationCondition =
                    DefCache.GetDef<ActorHasStatusEffectConditionDef>("HasBleedStatus_ApplicationCondition");

                ActorHasStatusEffectConditionDef newApplicationCondition = Helper.CreateDefFromClone(
                    sourceApplicationCondition,
                    "{C54C98C2-4987-4B2C-B983-6E64C1E9E457}",
                    statusApplicationConditionName);

                newApplicationCondition.StatusDef = newBrokenSpikeShooterStatus;
                newApplicationCondition.HasStatus = false;

                unUsableLeftHandStatus.ApplicationConditions = new EffectConditionDef[] { newApplicationCondition };

                DefCache.GetDef<ShootAbilityDef>("ShootPoisonSpike_ShootAbilityDef").DisablingStatuses = new StatusDef[]
               {
                newBrokenSpikeShooterStatus
               };


                /*    ApplyStatusAbilityDef unusableLeftHandAbility = DefCache.GetDef<ApplyStatusAbilityDef>("UnusableLeftHand_AbilityDef");
                    unusableLeftHandAbility.DisablingStatuses = new StatusDef[]
                   {
                    newBrokenSpikeShooterStatus
                   };*/

                TFTVStamina.BrokenSpikeShooterStatus = newBrokenSpikeShooterStatus;

                /*  WeaponDef spikeShooter = DefCache.GetDef<WeaponDef>("AN_Berserker_Shooter_LeftArm_WeaponDef");

                  // ApplyStatusAbilityDef unusableRightHandAbility = DefCache.GetDef<ApplyStatusAbilityDef>("UnusableRightHand_AbilityDef");

                  // spikeShooter.Abilities[1] = unusableRightHandAbility;


                  //UnusableLeftHand_StatusDef
                  //   spikeShooter.Abilities[0].

                  //  spikeShooter.BreakParentOnDisable = true;
                  spikeShooter.BehaviorOnDisable = EDisableBehavior.Disable;
                  spikeShooter.HandsToUse = 1;*/

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void FixNoXPCaptureAcheron()
        {
            try
            {
                ActorHasStatusFactionObjectiveDef captureAcheronObjective = (ActorHasStatusFactionObjectiveDef)Repo.GetDef("2f3ea3b1-49b1-7cbe-ef82-07a75d820259");
                captureAcheronObjective.MissionObjectiveData.ExperienceReward = 150;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void FixUnarmedAspida()
        {
            try
            {
                DefCache.GetDef<TacCharacterDef>("SY_AspidaInfested_TacCharacterDef").Data.EquipmentItems
                        = new ItemDef[] { DefCache.GetDef<ItemDef>("SY_Aspida_Arms_GroundVehicleWeaponDef") };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static void RemoveScyllaAndNodeResearches()
        {
            try
            {
                ResearchDbDef researchDbDef = DefCache.GetDef<ResearchDbDef>("aln_ResearchDB");

                ResearchDef scylla1Research = DefCache.GetDef<ResearchDef>("ALN_Scylla1_ResearchDef");
                ResearchDef scylla2Research = DefCache.GetDef<ResearchDef>("ALN_Scylla2_ResearchDef");
                ResearchDef scylla3Research = DefCache.GetDef<ResearchDef>("ALN_Scylla3_ResearchDef");
                ResearchDef scylla4Research = DefCache.GetDef<ResearchDef>("ALN_Scylla4_ResearchDef");
                ResearchDef scylla5Research = DefCache.GetDef<ResearchDef>("ALN_Scylla5_ResearchDef");
                ResearchDef scylla6Research = DefCache.GetDef<ResearchDef>("ALN_Scylla6_ResearchDef");
                ResearchDef scylla7Research = DefCache.GetDef<ResearchDef>("ALN_Scylla7_ResearchDef");
                ResearchDef scylla8Research = DefCache.GetDef<ResearchDef>("ALN_Scylla8_ResearchDef");
                ResearchDef scylla9Research = DefCache.GetDef<ResearchDef>("ALN_Scylla9_ResearchDef");
                ResearchDef scylla10Research = DefCache.GetDef<ResearchDef>("ALN_Scylla10_ResearchDef");
                ResearchDef nodeResearch = DefCache.GetDef<ResearchDef>("ALN_CorruptionNode_ResearchDef");

                researchDbDef.Researches.RemoveAll(r => r.name.Contains("ALN_Scylla"));
                researchDbDef.Researches.Remove(nodeResearch);

                ResearchDef alnMFSoldiersResearch = DefCache.GetDef<ResearchDef>("ALN_MindfraggedSoldiers_ResearchDef");
                alnMFSoldiersResearch.RevealRequirements = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").RevealRequirements;
                alnMFSoldiersResearch.InitialStates = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").InitialStates;
                alnMFSoldiersResearch.Priority = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").Priority;
                alnMFSoldiersResearch.ResearchCost = DefCache.GetDef<ResearchDef>("ALN_CrabmanBasic_ResearchDef").ResearchCost;



            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ModifyRecruitsCost()
        {
            try
            {
                GameDifficultyLevelDef veryhard = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                //Hero
                GameDifficultyLevelDef hard = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                //Standard
                GameDifficultyLevelDef standard = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                //Easy
                GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");


                easy.RecruitCostPerLevelMultiplier = 0.5f;
                standard.RecruitCostPerLevelMultiplier = 0.75f;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateEtermesStatuses()
        {
            try
            {
                CreateEtermesProtectionStatus();
                CreateEtermesVulnerability();

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateEtermesProtectionStatus()
        {
            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "EtermesProtectionStatus";
                string gUID = "35EC0B4B-C0C7-4EB4-B3F0-64E71507CB6D";
                string gUIDVisuals = "EB475735-E388-49BE-80B6-6AA6907C9138";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.75f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);
                damageTypeBaseEffectDefs.Add(TFTVMeleeDamage.MeleeStandardDamageType);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "ETERMES_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ETERMES_PROTECTION_DESCRIPTION";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void CreateEtermesVulnerability()
        {
            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "EtermesVulnerabilityStatus";
                string gUID = "B5135532-82F2-48B3-8B2A-3B3433D438AF";
                string gUIDVisuals = "30F37D69-5629-403E-A610-6B245B7665CD";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1.25f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);
                damageTypeBaseEffectDefs.Add(TFTVMeleeDamage.MeleeStandardDamageType);


                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.DisplayName1.LocalizationKey = "ETERMES_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ETERMES_VULNERABILITY_DESCRIPTION";
                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }




        public static void CreateRookieProtectionStatus()
        {
            try
            {

                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");


                string statusName = "RookieProtectionStatus";
                string gUID = "B7F811AF-D919-462D-8045-D42C08B1706D";
                string gUIDVisuals = "DD77459C-6B4E-42B7-81C9-B425EB305E3B";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 0.5f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);
                damageTypeBaseEffectDefs.Add(TFTVMeleeDamage.MeleeStandardDamageType);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_2-2.png");


                newStatus.Visuals.DisplayName1.LocalizationKey = "ROOKIE_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ROOKIE_PROTECTION_DESCRIPTION";

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void CreateRookieVulnerability()
        {
            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");
                StandardDamageTypeEffectDef standardDamageTypeEffectDef = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                AcidDamageTypeEffectDef acidDamage = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                string statusName = "RookieVulnerabilityStatus";
                string gUID = "C8468900-F4A0-4E47-92B2-AA7CBEB9EE13";
                string gUIDVisuals = "3F3697B6-487B-4610-A2B0-B2A17AA67C72";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.Hidden;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1.5f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                List<DamageTypeBaseEffectDef> damageTypeBaseEffectDefs = new List<DamageTypeBaseEffectDef>();
                damageTypeBaseEffectDefs.AddRange(newStatus.DamageTypeDefs);
                damageTypeBaseEffectDefs.Add(fireDamage);
                damageTypeBaseEffectDefs.Add(standardDamageTypeEffectDef);
                damageTypeBaseEffectDefs.Add(acidDamage);

                newStatus.DamageTypeDefs = damageTypeBaseEffectDefs.ToArray();

                newStatus.Visuals.DisplayName1.LocalizationKey = "ROOKIE_VULNERABILITY_NAME";
                newStatus.Visuals.Description.LocalizationKey = "ROOKIE_VULNERABILITY_DESCRIPTION";
                newStatus.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
                newStatus.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("UI_AbilitiesIcon_HunkerDown_1-2.png");
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void RemovePirateKing()
        {
            try
            {

                GeoscapeEventDef ProgSynIntroWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY0_WIN_GeoscapeEventDef");
                ProgSynIntroWin.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Clear();

                GeoscapeEventDef fireBirdMiss = DefCache.GetDef<GeoscapeEventDef>("PROG_SY2_MISS_GeoscapeEventDef");
                fireBirdMiss.GeoscapeEventData.Choices[0].Outcome.StartMission.WonEventID = "PROG_SY3_WIN";

                GeoscapeEventDef pirateKingWin = DefCache.GetDef<GeoscapeEventDef>("PROG_SY3_WIN_GeoscapeEventDef");
                pirateKingWin.GeoscapeEventData.Title.LocalizationKey = "PROG_SY2_WIN_TITLE";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void ChangeMyrmidonAndFirewormResearchRewards()
        {
            try
            {
                ResearchDef myrmidonResearch = DefCache.GetDef<ResearchDef>("PX_Alien_Swarmer_ResearchDef");
                DefCache.GetDef<ExistingResearchRequirementDef>("PX_LightSniperRifle_ResearchDef_ExistingResearchRequirementDef_0").ResearchID = myrmidonResearch.Id;
                myrmidonResearch.Resources.Add(ResourceType.Supplies, 100);
                myrmidonResearch.Resources.Add(ResourceType.Materials, 100);

                DefCache.GetDef<ResearchDef>("PX_Alien_Fireworm_ResearchDef").Resources.Add(ResourceType.Supplies, 150);
                DefCache.GetDef<ResearchDef>("PX_Alien_SwarmerEgg_ResearchDef").Resources.Add(ResourceType.Supplies, 400);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void CreateFireQuenchers()
        {
            try
            {
                CloneFireImmunityAbility();
                CreateFireQuencherStatus();
                CreateFireQuencherAbility();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }






        //cloning the DamageMultiplierAbilityDef fire immunity ability because because through Status effect only achieving immunity for body part
        //Need to clone to make it invisible in status panel
        public static void CloneFireImmunityAbility()
        {
            try
            {
                string abilityName = "FireImmunityInvisibleAbility";
                string gUID = "9A55315E-4694-4D95-8811-476C524EBAAE";
                // string characterProgressionGUID = "AA24A50E-C61A-4CD8-97FE-3F8BAC5F7BAA";
                string viewElementGUID = "231F088F-A4F0-4E6D-BC78-614AD0EF4594";



                DamageMultiplierAbilityDef source = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");
                DamageMultiplierAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);


                /*newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");*/
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.ShowInStatusScreen = false;
                newAbility.ViewElementDef.ShowInFreeAimMode = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void CreateFireQuencherAbility()
        {
            try
            {
                string abilityName = "FireQuencherAbility";
                string gUID = "020679B9-A7AD-45F9-BCD5-0EC13FB0D396";
                string characterProgressionGUID = "EEBE2E43-C8CC-4E05-9777-149FC0DBB874";
                string viewElementGUID = "AA346C20-3163-4A95-AD9B-E9C5678CB282";

                DamageMultiplierStatusDef status = DefCache.GetDef<DamageMultiplierStatusDef>("FireQuencherStatus");

                ApplyStatusAbilityDef source = DefCache.GetDef<ApplyStatusAbilityDef>("BionicDamageMultipliers_AbilityDef");
                ApplyStatusAbilityDef newAbility = Helper.CreateDefFromClone(
                    source,
                   gUID,
                    abilityName);


                newAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    characterProgressionGUID,
                   abilityName + "CharacterProgression");
                newAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    viewElementGUID,
                    abilityName + "ViewElement");
                newAbility.ViewElementDef.ShowInStatusScreen = true;
                newAbility.ViewElementDef.ShowInFreeAimMode = true;
                //  newAbility.ViewElementDef.ShowInStatusScreen = false;

                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                newAbility.ViewElementDef.DisplayName1.LocalizationKey = "FIRE_QUENCHER_NAME";
                newAbility.ViewElementDef.Description.LocalizationKey = "FIRE_QUENCHER_DESCRIPTION";
                newAbility.ViewElementDef.LargeIcon = fireImmunity.ViewElementDef.LargeIcon;
                newAbility.ViewElementDef.SmallIcon = fireImmunity.ViewElementDef.SmallIcon;
                newAbility.StatusDef = status;
                newAbility.TargetApplicationConditions = new EffectConditionDef[] { };
                newAbility.StatusApplicationTrigger = StatusApplicationTrigger.ActorEnterPlay;
                newAbility.StatusSource = StatusSource.AbilitySource;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateFireQuencherStatus()
        {

            try
            {
                StandardDamageTypeEffectDef fireDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Fire_StandardDamageTypeEffectDef");

                string statusName = "FireQuencherStatus";
                string gUID = "CC8B3A1B-E25D-43F4-9469-52FBE6F9C926";
                string gUIDVisuals = "2B927AA0-7CA5-473D-9847-31718002B552";

                DamageMultiplierStatusDef source = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");
                DamageMultiplierStatusDef newStatus = Helper.CreateDefFromClone(
                    source,
                    gUID,
                    statusName);
                newStatus.EffectName = statusName;
                newStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                newStatus.VisibleOnPassiveBar = true;
                newStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnBodyPartStatusList;
                newStatus.ApplicationConditions = new EffectConditionDef[] { };

                newStatus.Visuals = Helper.CreateDefFromClone(
                    source.Visuals,
                    gUIDVisuals,
                    statusName + "Visuals");
                newStatus.Multiplier = 1f;
                newStatus.MultiplierType = DamageMultiplierType.Incoming;
                newStatus.Range = -1;
                newStatus.DamageTypeDefs = new StandardDamageTypeEffectDef[] { };

                DamageMultiplierAbilityDef fireImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("FireImmunity_DamageMultiplierAbilityDef");

                newStatus.Visuals.LargeIcon = fireImmunity.ViewElementDef.LargeIcon;
                newStatus.Visuals.SmallIcon = fireImmunity.ViewElementDef.SmallIcon;


                newStatus.Visuals.DisplayName1.LocalizationKey = "FIRE_QUENCHER_NAME";
                newStatus.Visuals.Description.LocalizationKey = "FIRE_QUENCHER_DESCRIPTION";
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void Testing()
        {
            try
            {
                AddingSafetyConsiderationToMoveAndAttack();
                AddingSafetyConsiderationToRandomMove();
                AddingSafetyConsiderationToRegularAdvance();
                ModifySafetyConsiderationDef();
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ModifySafetyConsiderationDef()
        {
            try
            {
                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");

                safePositionConsideration.HighCoverProtection = 1f;
                safePositionConsideration.LowCoverProtection = 0.5f;
                safePositionConsideration.NoneCoverProtection = 0f;
                safePositionConsideration.VisionScoreWhenVisibleByAllEnemies = 1f;
                safePositionConsideration.VisionRange = 20;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }


        public static void AddingSafetyConsiderationToMoveAndAttack()
        {
            try
            {

                AIActionMoveAndAttackDef moveAndAttack = DefCache.GetDef<AIActionMoveAndAttackDef>("MoveAndShoot_AIActionDef");

                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");


                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = moveAndAttack.Evaluations[1].Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = moveAndAttack.Evaluations[1].Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                moveAndAttack.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();





            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddingSafetyConsiderationToRegularAdvance()
        {
            try
            {
                AIActionMoveToPositionDef advanceNormal = DefCache.GetDef<AIActionMoveToPositionDef>("Advance_Normal_AIActionDef");

                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");


                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = advanceNormal.Evaluations.First().Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = advanceNormal.Evaluations.First().Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                advanceNormal.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddingSafetyConsiderationToRandomMove()
        {
            try
            {

                AIActionMoveToPositionDef moveToRandom = DefCache.GetDef<AIActionMoveToPositionDef>("MoveToRandomWaypoint_AIActionDef");


                AISafePositionConsiderationDef safePositionConsideration = DefCache.GetDef<AISafePositionConsiderationDef>("DefenseSafePosition_AIConsiderationDef");



                AIAdjustedConsideration aIAdjustedConsideration = new AIAdjustedConsideration()
                {
                    Consideration = safePositionConsideration,
                    ScoreCurve = moveToRandom.Evaluations.First().Considerations.First().ScoreCurve
                };

                List<AIAdjustedConsideration> aIAdjustedConsiderations = moveToRandom.Evaluations.First().Considerations.ToList();
                aIAdjustedConsiderations.Add(aIAdjustedConsideration);
                moveToRandom.Evaluations.First().Considerations = aIAdjustedConsiderations.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void AddLoreEntries()
        {
            try
            {
                CreateAlistairLoreEntry();
                CreateOlenaLoreEntry();
                CreateBennuLoreEntry();
                CreateHelenaLoreEntry();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateHelenaLoreEntry()
        {
            try
            {
                string gUID = "217EB721-52E6-4401-90D1-3287D6CC8DC2";
                string name = "Helena_Lore";
                string title = "TFTV_LORE_HELENA_TITLE";
                string description = "TFTV_LORE_HELAN_DESCRIPTION";
                string pic = "lore_helena.jpg";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("HelenaOnOlena").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateBennuLoreEntry()
        {
            try
            {
                string gUID = "0A67EB59-5E9B-46A9-95EE-EC6C47417B7C";
                string name = "Bennu_Lore";
                string title = "TFTV_LORE_BENNU_TITLE";
                string description = "TFTV_LORE_BENNU_DESCRIPTION";
                string pic = "lore_bennu.jpg";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_2").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static void CreateOlenaLoreEntry()
        {
            try
            {
                string gUID = "38ACBF41-7D2D-479F-981E-10FED4FC6800";
                string name = "Olena_Lore";
                string title = "TFTV_LORE_OLENA_TITLE";
                string description = "TFTV_LORE_OLENA_DESCRIPTION";
                string pic = "lore_olena.jpg";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_1").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }



        public static void CreateAlistairLoreEntry()
        {
            try
            {
                string gUID = "B955090F-62E0-41F2-9036-3548A1DC5F46";
                string name = "Alistair_Lore";
                string title = "TFTV_LORE_ALISTAIR_TITLE";
                string description = "TFTV_LORE_ALISTAIR_DESCRIPTION";
                string pic = "lore_alistair.jpg";
                GeoPhoenixpediaEntryDef alistairEntry = CreateLoreEntry(name, gUID, title, description, pic);
                DefCache.GetDef<GeoscapeEventDef>("IntroBetterGeo_0").GeoscapeEventData.Choices[0].Outcome.GivePhoenixpediaEntries = new List<GeoPhoenixpediaEntryDef>() { alistairEntry };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static GeoPhoenixpediaEntryDef CreateLoreEntry(string name, string gUID, string title, string description, string pic)
        {
            try
            {
                GeoPhoenixpediaEntryDef source = DefCache.GetDef<GeoPhoenixpediaEntryDef>("AntediluvianArchaeology_GeoPhoenixpediaEntryDef");
                GeoPhoenixpediaEntryDef newLoreEntry = Helper.CreateDefFromClone(source, gUID, name);
                newLoreEntry.Category = PhoenixpediaCategoryType.Lore;
                newLoreEntry.Entry.Title.LocalizationKey = title;
                newLoreEntry.Entry.Description.LocalizationKey = description;
                newLoreEntry.Entry.DetailsImage = Helper.CreateSpriteFromImageFile(pic);
                return newLoreEntry;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static void FixMyrmidonFlee()
        {
            try
            {
                AIActionsTemplateDef swarmerAI = DefCache.GetDef<AIActionsTemplateDef>("Swarmer_AIActionsTemplateDef");
                AIActionDef flee = DefCache.GetDef<AIActionDef>("Flee_AIActionDef");

                List<AIActionDef> aIActionDefs = new List<AIActionDef>(swarmerAI.ActionDefs)
                {
                    flee
                };
                swarmerAI.ActionDefs = aIActionDefs.ToArray();

                TacticalActorDef swarmer = DefCache.GetDef<TacticalActorDef>("Swarmer_ActorDef");

                ExitMissionAbilityDef exitMissionAbilityDef = DefCache.GetDef<ExitMissionAbilityDef>("ExitMission_AbilityDef");


                List<AbilityDef> abilityDefs = new List<AbilityDef>();
                abilityDefs = swarmer.Abilities.ToList();
                abilityDefs.Add(exitMissionAbilityDef);
                swarmer.Abilities = abilityDefs.ToArray();


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void SyphonAttackFix()
        {
            try
            {
                DefCache.GetDef<SyphoningDamageKeywordDataDef>("Syphon_DamageKeywordDataDef").SyphonBasedOnHealthDamageDealt = false;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        public static void AddContributionPointsToPriestAndTech()
        {
            try
            {
                DefCache.GetDef<MindControlAbilityDef>("Priest_MindControl_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("InducePanic_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<InstilFrenzyAbilityDef>("Priest_InstilFrenzy_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<PsychicScreamAbilityDef>("Priest_PsychicScream_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowPRCRTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("ThrowLaserTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("ElectricReinforcement_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployLaserTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployPRCRTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<TacticalAbilityDef>("DeployTurret_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<ApplyStatusAbilityDef>("ManualControl_AbilityDef").ContributionPointsOnUse = 500;
                DefCache.GetDef<HealAbilityDef>("FieldMedic_AbilityDef").ContributionPointsOnUse = 500;
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }





        public static void AllowMedkitsToTargetMutoidsAndChangesToMutoidSkillSet()
        {
            try
            {
                //Allow medkits to target Mutoids
                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Medkit_AbilityDef]").Origin.CullTargetTags.Clear();

                //Skill toss per Belial's suggestions

                //need to change/clone CharacterProgressionData 
                AbilityCharacterProgressionDef sourceFirstLevel = DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [GooImmunity_AbilityDef]");


                DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [VirusResistant_DamageMultiplierAbilityDef]").MutagenCost = 10;
                AbilityCharacterProgressionDef demolitionStanceCPD =
                    Helper.CreateDefFromClone(sourceFirstLevel, "F4DA4D75-8FCE-4414-BB88-7A065A45105C", "E_CharacterProgressionData [Demolition_AbilityDef]");



                AbilityCharacterProgressionDef mindControlImmunityCPD = DefCache.GetDef<AbilityCharacterProgressionDef>("E_CharacterProgressionData [MindControlImmunity_AbilityDef]");
                mindControlImmunityCPD.MutagenCost = 15;
                mindControlImmunityCPD.SkillPointCost = 0;
                mindControlImmunityCPD.RequiredSpeed = 0;
                mindControlImmunityCPD.RequiredStrength = 0;
                mindControlImmunityCPD.RequiredWill = 0;

                AbilityCharacterProgressionDef poisonImmunityCPD = Helper.CreateDefFromClone(sourceFirstLevel, "67418B3A-C666-41CE-B504-853C6C705284", "E_CharacterProgressionData [PoisonImmunity_AbilityDef]");
                poisonImmunityCPD.MutagenCost = 20;
                DamageMultiplierAbilityDef poisonImmunity = DefCache.GetDef<DamageMultiplierAbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                poisonImmunity.CharacterProgressionData = poisonImmunityCPD;


                AbilityCharacterProgressionDef acidResistanceCPD = Helper.CreateDefFromClone(sourceFirstLevel, "03367F73-97B9-4E65-919B-D31DF147EAA0", "E_CharacterProgressionData [AcidResistance_AbilityDef]");
                acidResistanceCPD.MutagenCost = 25;
                DamageMultiplierAbilityDef acidResistance = DefCache.GetDef<DamageMultiplierAbilityDef>("AcidResistant_DamageMultiplierAbilityDef");
                acidResistance.CharacterProgressionData = acidResistanceCPD;

                AbilityCharacterProgressionDef leapCPD = Helper.CreateDefFromClone(sourceFirstLevel, "99339FAB-3FA5-4472-89B6-52A816464637", "E_CharacterProgressionData [RocketLeap_AbilityDef]");
                leapCPD.MutagenCost = 30;

                JetJumpAbilityDef leap = DefCache.GetDef<JetJumpAbilityDef>("Exo_Leap_AbilityDef");
                leap.CharacterProgressionData = leapCPD;




                ApplyStatusAbilityDef demolitionAbility = DefCache.GetDef<ApplyStatusAbilityDef>("DemolitionMan_AbilityDef");
                demolitionAbility.CharacterProgressionData = demolitionStanceCPD;



                AbilityTrackDef arthronAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [ArthronSpecializationDef]");
                //    AbilityTrackDef tritonAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [TritonSpecializationDef]");
                AbilityTrackDef sirenAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [SirenSpecializationDef]");
                AbilityTrackDef scyllaAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [ScyllaSpecializationDef]");
                AbilityTrackDef acheronAbilityTrack = DefCache.GetDef<AbilityTrackDef>("E_AbilityTrack [AcheronSpecializationDef]");



                arthronAbilityTrack.AbilitiesByLevel[0].Ability = DefCache.GetDef<DamageMultiplierAbilityDef>("VirusResistant_DamageMultiplierAbilityDef");
                arthronAbilityTrack.AbilitiesByLevel[2].Ability = poisonImmunity;
                arthronAbilityTrack.AbilitiesByLevel[4].Ability = DefCache.GetDef<ApplyEffectAbilityDef>("MistBreather_AbilityDef");

                //Reduce cost of Mutoid Syphon attack to 1AP
                DefCache.GetDef<BashAbilityDef>("Mutoid_Syphon_Strike_AbilityDef").ActionPointCost = 0.25f;


                scyllaAbilityTrack.AbilitiesByLevel[0].Ability = demolitionAbility;
                scyllaAbilityTrack.AbilitiesByLevel[4].Ability = leap;

                sirenAbilityTrack.AbilitiesByLevel[3].Ability = acidResistance;

                acheronAbilityTrack.AbilitiesByLevel[1].Ability = DefCache.GetDef<ApplyStatusAbilityDef>("MindControlImmunity_AbilityDef");

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void RemoveCensusResearch()
        {
            try
            {
                DefCache.GetDef<ResearchDbDef>("pp_ResearchDB").Researches.Remove(DefCache.GetDef<ResearchDef>("PX_SDI_ResearchDef"));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void ChangesToAcherons()
        {
            try
            {
                ChangesAcheronResearches();
                CreateAcheronAbilitiesAndStatus();
                ChangesAcheronTemplates();
                ChangesAcheronsAI();
                ChangesAcheronAbilities();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangesAcheronAbilities()
        {
            try
            {
                string nameOfNotMetallicConditionTag = "Organic_ApplicationCondition";
                ActorHasTagEffectConditionDef sourceHasTagConditionDef = DefCache.GetDef<ActorHasTagEffectConditionDef>("HasHumanTag_ApplicationCondition");
                ActorHasTagEffectConditionDef organicEffectConditionDef = Helper.CreateDefFromClone(sourceHasTagConditionDef, "E1ADF8A5-746D-4176-9FFA-99296F96B9BE", nameOfNotMetallicConditionTag);
                organicEffectConditionDef.GameTag = DefCache.GetDef<SubstanceTypeTagDef>("Organic_SubstanceTypeTagDef");

                StatMultiplierStatusDef trembling = DefCache.GetDef<StatMultiplierStatusDef>("Trembling_StatusDef");
                trembling.ApplicationConditions = new EffectConditionDef[] { organicEffectConditionDef };

                DefCache.GetDef<CallReinforcementsAbilityDef>("Acheron_CallReinforcements_AbilityDef").WillPointCost = 10;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangesAcheronsAI()
        {
            try
            {
                //Adjusting Delirium Spray
                AINonHealthDamageAttackPositionConsiderationDef deliriumSprayConsideration = DefCache.GetDef<AINonHealthDamageAttackPositionConsiderationDef>("Acheron_CorruptionSprayAttackPosition_AIConsiderationDef");
                deliriumSprayConsideration.EnemyMask = PhoenixPoint.Tactical.AI.ActorType.Combatant;
                AcidStatusDef acidStatus = DefCache.GetDef<AcidStatusDef>("Acid_StatusDef");
                deliriumSprayConsideration.DamageTypeStatusDef = acidStatus;

                //Removes exclusions to metallic and ancients targets
                HasTagSuitabilityDef deliriumSprayExcludedTags = DefCache.GetDef<HasTagSuitabilityDef>("E_TargetSuitability [Acheron_CorruptionTagsTargetsSuitability_AIConsiderationDef]");
                List<GameTagDef> deliriumSprayCheckTargetsByTag = deliriumSprayExcludedTags.GameTagDefs.ToList();
                deliriumSprayCheckTargetsByTag.Add(DefCache.GetDef<SubstanceTypeTagDef>("Organic_SubstanceTypeTagDef"));
                deliriumSprayExcludedTags.GameTagDefs = deliriumSprayCheckTargetsByTag.ToArray();
                deliriumSprayExcludedTags.HasTag = true;

                //Adjusting GooSpray
                AINonHealthDamageAttackPositionConsiderationDef gooSprayConsideration = DefCache.GetDef<AINonHealthDamageAttackPositionConsiderationDef>("Acheron_GooSprayAttackPosition_AIConsiderationDef");
                gooSprayConsideration.EnemyMask = PhoenixPoint.Tactical.AI.ActorType.Combatant;
                deliriumSprayConsideration.DamageTypeStatusDef = acidStatus;



            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void ChangesAcheronResearches()
        {
            try
            {

                //Researches
                ResearchDef acheronResearch1 = DefCache.GetDef<ResearchDef>("ALN_Acheron1_ResearchDef");
                ResearchDef acheronResearch2 = DefCache.GetDef<ResearchDef>("ALN_Acheron2_ResearchDef");
                ResearchDef acheronResearch3 = DefCache.GetDef<ResearchDef>("ALN_Acheron3_ResearchDef");
                ResearchDef acheronResearch4 = DefCache.GetDef<ResearchDef>("ALN_Acheron4_ResearchDef");
                ResearchDef acheronResearch5 = DefCache.GetDef<ResearchDef>("ALN_Acheron5_ResearchDef");
                ResearchDef acheronResearch6 = DefCache.GetDef<ResearchDef>("ALN_Acheron6_ResearchDef");



                ExistingResearchRequirementDef acheronResearchReq2 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron2_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq3 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron3_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq4 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron4_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq5 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron5_ResearchDef_ExistingResearchRequirementDef_0");
                ExistingResearchRequirementDef acheronResearchReq6 = DefCache.GetDef<ExistingResearchRequirementDef>("ALN_Acheron6_ResearchDef_ExistingResearchRequirementDef_0");

                //Acheron Prime will require heavy Chirons
                acheronResearchReq2.ResearchID = "ALN_Chiron2_ResearchDef";
                //Acheron Ascepius & Acheron Achlys will require Goo Chirons
                acheronResearchReq3.ResearchID = "ALN_Chiron7_ResearchDef";
                acheronResearchReq5.ResearchID = "ALN_Chiron7_ResearchDef";
                //Ascepius and Achlys Champions will require Bombard Chirons
                acheronResearchReq4.ResearchID = "ALN_Chiron9_ResearchDef";
                acheronResearchReq6.ResearchID = "ALN_Chiron9_ResearchDef";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void CreateAcheronAbilitiesAndStatus()
        {
            try
            {
                //Create Acheron Harbinger ability, to be used as a flag/counter when calculating chances of getting Void Touched
                string acheronHarbingerAbilityName = "Acheron_Harbinger_AbilityDef";
                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("SelfDefenseSpecialist_AbilityDef");
                PassiveModifierAbilityDef acheronHarbingerAbility = Helper.CreateDefFromClone(
                    source,
                    "3ABB6347-5ABA-4B4D-B786-C962B7A0540C",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "336671D9-281F-4985-8F7A-8EF424EF1FB8",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "FB0B38FC-CDB7-4EDF-9E39-89111528A84B",
                    acheronHarbingerAbilityName);
                acheronHarbingerAbility.StatModifications = new ItemStatModification[0];
                acheronHarbingerAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                acheronHarbingerAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACHERON_HARBINGER_NAME";
                acheronHarbingerAbility.ViewElementDef.Description.LocalizationKey = "ACHERON_HARBINGER_DESCRIPTION";
                acheronHarbingerAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                acheronHarbingerAbility.ViewElementDef.SmallIcon = acheronHarbingerAbility.ViewElementDef.LargeIcon;

                //Creating Tributary to the Void, to spread TBTV on nearby allies
                string acheronTributaryAbilityName = "Acheron_Tributary_AbilityDef";
                PassiveModifierAbilityDef acheronTributaryAbility = Helper.CreateDefFromClone(
                    source,
                    "2CDB184A-4E8D-4E9A-B957-983A1FD23313",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "0770EB67-52CD-4E17-9A3B-CB6C91E86BC5",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "A82EFDD3-8ED7-46C8-8B52-D8051910419D",
                    acheronTributaryAbilityName);
                acheronTributaryAbility.StatModifications = new ItemStatModification[0];
                acheronTributaryAbility.ItemTagStatModifications = new EquipmentItemTagStatModification[0];
                acheronTributaryAbility.ViewElementDef.DisplayName1.LocalizationKey = "ACHERON_TRIBUTARY_NAME";
                acheronTributaryAbility.ViewElementDef.Description.LocalizationKey = "ACHERON_TRIBUTARY_DESCRIPTION";
                acheronTributaryAbility.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("Void-04P.png");
                acheronTributaryAbility.ViewElementDef.SmallIcon = acheronTributaryAbility.ViewElementDef.LargeIcon;



                //Creating special status that will allow Umbra to target the character
                /*  string umbraTargetStatusDefName = "TBTV_Target";
                  DamageMultiplierStatusDef sourceForTargetAbility = DefCache.GetDef<DamageMultiplierStatusDef>("BionicResistances_StatusDef");

                  DamageMultiplierStatusDef umbraTargetStatus = Helper.CreateDefFromClone(
                     sourceForTargetAbility,
                     "0C4558E8-2791-4669-8F5B-2DA1D20B2ADD",
                     umbraTargetStatusDefName);

                  umbraTargetStatus.EffectName = "UmbraTarget";
                  umbraTargetStatus.Visuals = Helper.CreateDefFromClone(
                      sourceForTargetAbility.Visuals,
                      "49A5DC8D-50B9-4CCC-A3D4-7576A1DDD375",
                      umbraTargetStatus.EffectName);
                  umbraTargetStatus.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                  umbraTargetStatus.VisibleOnPassiveBar = true;
                  umbraTargetStatus.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                  umbraTargetStatus.Visuals.DisplayName1.LocalizationKey = "VOID_BLIGHT_NAME";
                  umbraTargetStatus.Visuals.Description.LocalizationKey = "VOID_BLIGHT_DESCRIPTION";
                  umbraTargetStatus.Visuals.LargeIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.LargeIcon;
                  umbraTargetStatus.Visuals.SmallIcon = DefCache.GetDef<DeathBelcherAbilityDef>("Oilcrab_Die_DeathBelcher_AbilityDef").ViewElementDef.SmallIcon;*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ChangesAcheronTemplates()
        {
            try
            {
                TacCharacterDef acheron = DefCache.GetDef<TacCharacterDef>("Acheron_TacCharacterDef");
                TacCharacterDef acheronPrime = DefCache.GetDef<TacCharacterDef>("AcheronPrime_TacCharacterDef");
                TacCharacterDef acheronAsclepius = DefCache.GetDef<TacCharacterDef>("AcheronAsclepius_TacCharacterDef");
                TacCharacterDef acheronAsclepiusChampion = DefCache.GetDef<TacCharacterDef>("AcheronAsclepiusChampion_TacCharacterDef");
                TacCharacterDef acheronAchlys = DefCache.GetDef<TacCharacterDef>("AcheronAchlys_TacCharacterDef");
                TacCharacterDef acheronAchlysChampion = DefCache.GetDef<TacCharacterDef>("AcheronAchlysChampion_TacCharacterDef");

                acheron.DeploymentCost = 180;
                acheronPrime.DeploymentCost = 240;
                acheronAsclepius.DeploymentCost = 310;
                acheronAchlys.DeploymentCost = 310;
                acheronAsclepiusChampion.DeploymentCost = 350;
                acheronAchlysChampion.DeploymentCost = 350;

                //Adding Harbinger of the Void to Acheron, Acheron Prime and Acheron Asclepius Champion
                //Adding Co-Delirium To Acheron Prime
                PassiveModifierAbilityDef harbinger = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Harbinger_AbilityDef");
                PassiveModifierAbilityDef tributary = DefCache.GetDef<PassiveModifierAbilityDef>("Acheron_Tributary_AbilityDef");
                ApplyStatusAbilityDef coDeliriumAbility = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_CoCorruption_AbilityDef");

                List<TacticalAbilityDef> acheronBasicAbilities = acheron.Data.Abilites.ToList();
                acheronBasicAbilities.Add(harbinger);
                acheron.Data.Abilites = acheronBasicAbilities.ToArray();

                List<TacticalAbilityDef> acheronPrimeBasicAbilities = acheronPrime.Data.Abilites.ToList();
                acheronPrimeBasicAbilities.Add(harbinger);
                acheronPrimeBasicAbilities.Add(coDeliriumAbility);
                acheronPrime.Data.Abilites = acheronPrimeBasicAbilities.ToArray();

                /* List<TacticalAbilityDef> acheronAsclepiusChampionBasicAbilities = acheronAsclepiusChampion.Data.Abilites.ToList();
                 acheronAsclepiusChampionBasicAbilities.Add(harbinger);
                 acheronAsclepiusChampion.Data.Abilites = acheronAsclepiusChampionBasicAbilities.ToArray();*/

                List<TacticalAbilityDef> acheronAchlysChampionBasicAbilities = acheronAchlysChampion.Data.Abilites.ToList();
                acheronAchlysChampionBasicAbilities.Add(tributary);
                acheronAchlysChampion.Data.Abilites = acheronAchlysChampionBasicAbilities.ToArray();

                //Removes leap from all Acherons
                /*
                DefCache.GetDef<TacticalItemDef>("Acheron_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlys_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAsclepius_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAsclepiusChampion_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_RearRightLeg_BodyPartDef").Abilities = new AbilityDef[] { };*/

                //Removing reinforcements from Acheron, Acheron Prime, Acheron Achlys and Acheron Achlys Champion
                DefCache.GetDef<TacticalItemDef>("Acheron_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Head_BodyPartDef").Abilities = new AbilityDef[] { };
                DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_Head_BodyPartDef").Abilities = new AbilityDef[] { };

                //Limiting Delirium cloud to one use per turn
                ApplyDamageEffectAbilityDef deliriumCloud = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Acheron_CorruptionCloud_AbilityDef");
                ApplyStatusAbilityDef pepperCloud = DefCache.GetDef<ApplyStatusAbilityDef>("Acheron_PepperCloud_ApplyStatusAbilityDef");
                deliriumCloud.UsesPerTurn = 1;
                ApplyEffectAbilityDef confusionCloud = DefCache.GetDef<ApplyEffectAbilityDef>("Acheron_ParalyticCloud_AbilityDef");
                ResurrectAbilityDef resurrectAbility = DefCache.GetDef<ResurrectAbilityDef>("Acheron_ResurrectAbilityDef");

                //Removing Restore Armor from Acheron Prime
                DefCache.GetDef<TacticalItemDef>("AcheronPrime_Husk_BodyPartDef").Abilities = new AbilityDef[] { pepperCloud };

                ApplyDamageEffectAbilityDef corrosiveCloud = DefCache.GetDef<ApplyDamageEffectAbilityDef>("Acheron_CorrosiveCloud_AbilityDef");

                //Removes CorrosiveCloud from AchlysChampion
                TacticalItemDef achlysChampionHusk = DefCache.GetDef<TacticalItemDef>("AcheronAchlysChampion_Husk_BodyPartDef");
                List<AbilityDef> achlysChampionHuskAbilities = achlysChampionHusk.Abilities.ToList();
                achlysChampionHuskAbilities.Remove(corrosiveCloud);
                achlysChampionHusk.Abilities = achlysChampionHuskAbilities.ToArray();

                //Remove Confusion cloud from Acheron Achlys
                TacticalItemDef achlysHusk = DefCache.GetDef<TacticalItemDef>("AcheronAchlys_Husk_BodyPartDef");
                List<AbilityDef> achlysHuskAbilities = achlysChampionHusk.Abilities.ToList();
                achlysHuskAbilities.Remove(confusionCloud);
                achlysChampionHusk.Abilities = achlysHuskAbilities.ToArray();

                //Adjust Acheron leap so it can only be used once per turn and doesn't cost any AP
                JetJumpAbilityDef acheronLeap = DefCache.GetDef<JetJumpAbilityDef>("Acheron_Leap_AbilityDef");
                acheronLeap.UsesPerTurn = 1;
                //acheronLeap.ActionPointCost = 0;

                //Removing Resurrect and Delirium Clouds from Asclepius Husks

                TacticalItemDef asclepiusChampionHusk = DefCache.GetDef<TacticalItemDef>("AcheronAsclepiusChampion_Husk_BodyPartDef");
                List<AbilityDef> asclepiusChampionHuskAbilities = asclepiusChampionHusk.Abilities.ToList();
                asclepiusChampionHuskAbilities.Remove(resurrectAbility);
                asclepiusChampionHuskAbilities.Remove(deliriumCloud);
                asclepiusChampionHusk.Abilities = asclepiusChampionHuskAbilities.ToArray();


                TacticalItemDef asclepiusHusk = DefCache.GetDef<TacticalItemDef>("AcheronAsclepius_Husk_BodyPartDef");
                List<AbilityDef> asclepiusHuskAbilities = asclepiusHusk.Abilities.ToList();
                asclepiusHuskAbilities.Remove(resurrectAbility);
                asclepiusHuskAbilities.Remove(deliriumCloud);
                asclepiusHusk.Abilities = asclepiusHuskAbilities.ToArray();


                DamageKeywordDef poison = DefCache.GetDef<DamageKeywordDef>("Poisonous_DamageKeywordDataDef");
                DamageKeywordDef acid = DefCache.GetDef<DamageKeywordDef>("Acid_DamageKeywordDataDef");
                DamageKeywordDef standard = DefCache.GetDef<DamageKeywordDef>("Damage_DamageKeywordDataDef");

                DamageKeywordDef blast = DefCache.GetDef<DamageKeywordDef>("Blast_DamageKeywordDataDef");
                StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                WeaponDef spitArmsAcheronAchlysChampion = DefCache.GetDef<WeaponDef>("AcheronAchlysChampion_Arms_WeaponDef");
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords[1].DamageKeywordDef = blast;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords[1].Value = 30;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = poison,
                    Value = 30
                });
                spitArmsAcheronAchlysChampion.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 20
                });
                spitArmsAcheronAchlysChampion.DamagePayload.DamageType = blastDamage;
                spitArmsAcheronAchlysChampion.DamagePayload.AoeRadius = 2f;
                spitArmsAcheronAchlysChampion.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;
                spitArmsAcheronAchlysChampion.HandsToUse = 2;


                WeaponDef spitArmsAcheronAsclepiusChampion = DefCache.GetDef<WeaponDef>("AcheronAsclepiusChampion_Arms_WeaponDef");

                spitArmsAcheronAsclepiusChampion.HandsToUse = 2;

                WeaponDef achlysArms = DefCache.GetDef<WeaponDef>("AcheronAchlys_Arms_WeaponDef");

                achlysArms.HandsToUse = 2;

                //   string guid = "2B294E66-1BE9-425B-B088-F5A9075167A6";
                WeaponDef neuroArmsCopy = new WeaponDef();//Repo.CreateDef<WeaponDef>(guid);
                ReflectionHelper.CopyFields(achlysArms, neuroArmsCopy);
                ReflectionHelper.CopyFields(spitArmsAcheronAchlysChampion, achlysArms);
                ReflectionHelper.CopyFields(neuroArmsCopy, spitArmsAcheronAsclepiusChampion);

                DamageKeywordDef mistDamageKeyword = DefCache.GetDef<DamageKeywordDef>("Mist_DamageKeywordEffectorDef");
                SpawnVoxelDamageTypeEffectDef mistDamageTypeEffect = DefCache.GetDef<SpawnVoxelDamageTypeEffectDef>("Mist_SpawnVoxelDamageTypeEffectDef");

                // Change_AcheronCorruptiveSpray();
                //Add acid and mist, increase range
                WeaponDef acheronArms = DefCache.GetDef<WeaponDef>("Acheron_Arms_WeaponDef");
                acheronArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 10
                });
                acheronArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = mistDamageKeyword,
                    Value = 1

                });

                acheronArms.DamagePayload.DamageType = mistDamageTypeEffect;
                acheronArms.DamagePayload.AoeRadius = 5;
                acheronArms.DamagePayload.Range = 30;
                acheronArms.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;
                acheronArms.HandsToUse = 2;
                //   acheronArms.Abilities[0] = DefCache.GetDef<ShootAbilityDef>("MistLaunch_ShootAbilityDef"); 


                WeaponDef acheronPrimeArms = DefCache.GetDef<WeaponDef>("AcheronPrime_Arms_WeaponDef");
                acheronPrimeArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = acid,
                    Value = 20
                });
                acheronPrimeArms.DamagePayload.DamageKeywords.Add(new DamageKeywordPair
                {
                    DamageKeywordDef = mistDamageKeyword,
                    Value = 1

                });

                acheronPrimeArms.DamagePayload.DamageType = mistDamageTypeEffect;
                acheronPrimeArms.DamagePayload.AoeRadius = 5;
                acheronPrimeArms.DamagePayload.Range = 30;
                acheronPrimeArms.DamagePayload.DamageDeliveryType = DamageDeliveryType.Cone;
                acheronPrimeArms.HandsToUse = 2;

                DefCache.GetDef<ShootAbilityDef>("Acheron_GooSpray_ShootAbilityDef").UsesPerTurn = 2;
                DefCache.GetDef<ShootAbilityDef>("Acheron_CorruptiveSpray_AbilityDef").UsesPerTurn = 2;
                DefCache.GetDef<ShootAbilityDef>("Acheron_ParalyticSpray_AbilityDef").UsesPerTurn = 2;

                ItemSlotDef frontLeftLegSlot = (ItemSlotDef)Repo.GetDef("fdbaba54-5dd8-69f4-d85a-02cb6e17d1ea"); //Acheron_FrontLeftLeg_SlotDef
                ItemSlotDef frontRightLegSlot = (ItemSlotDef)Repo.GetDef("2f4339a5-b4e3-c184-f841-efe653dac7b2"); //Acheron_FrontRightLeg_SlotDef

                frontLeftLegSlot.ProvidesHand = false;
                frontRightLegSlot.ProvidesHand = false;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        /*   public static void ChangeInfestationDefs()
           {
               try
               {
                   AlienRaidsSetupDef raidsSetup = DefCache.GetDef<AlienRaidsSetupDef>("_AlienRaidsSetupDef");
                   raidsSetup.RaidBands[0].RollResultMax = 60;
                   raidsSetup.RaidBands[1].RollResultMax = 80;
                   raidsSetup.RaidBands[2].RollResultMax = 100;
                   raidsSetup.RaidBands[3].RollResultMax = 130;
                   raidsSetup.RaidBands[4].RollResultMax = 9999;
                   raidsSetup.RaidBands[4].AircraftTypesAllowed = 0;
               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);

               }
           }*/
        public static void ModifyMissionDefsToReplaceNeutralWithBandit()
        {
            try
            {
                PPFactionDef banditFaction = DefCache.GetDef<PPFactionDef>("NEU_Bandits_FactionDef");

                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    // TFTVLogger.Always("The first foreach went ok");


                    foreach (MutualParticipantsRelations relations in missionTypeDef.ParticipantsRelations)
                    {
                        // TFTVLogger.Always("The second foreach went ok");
                        if (relations.FirstParticipant == TacMissionParticipant.Player && relations.MutualRelation == FactionRelation.Enemy)
                        {
                            //   TFTVLogger.Always("The if inside the second foreach went ok");

                            if (missionTypeDef.ParticipantsData != null)
                            {
                                foreach (TacMissionTypeParticipantData data in missionTypeDef.ParticipantsData)
                                {
                                    //TFTVLogger.Always("The third foreach went Ok");

                                    if (data.ParticipantKind == relations.SecondParticipant)
                                    {
                                        // TFTVLogger.Always("The if inside the third foreach went ok");
                                        if (data.FactionDef != null)
                                        {
                                            if (missionTypeDef.name == "StoryAN1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryNJ_Chain1_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StoryPX13_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN0_CustomMissionTypeDef" ||
                                                missionTypeDef.name == "StorySYN4_CustomMissionTypeDef" ||
                                                    missionTypeDef.name == "StorySYN5_CustomMissionTypeDef")
                                            {
                                                data.FactionDef = banditFaction;
                                                //  TFTVLogger.Always("In mission " + missionTypeDef.name + " the enemy faction is " + data.FactionDef.name);
                                            }
                                        }
                                    }
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
        public static void ChangesAmbushMissions()
        {
            try
            {

                //Changing ambush missions so that all of them have crates
                CustomMissionTypeDef AmbushALN = DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef");
                CustomMissionTypeDef sourceScavCratesALN = DefCache.GetDef<CustomMissionTypeDef>("ScavCratesALN_CustomMissionTypeDef");



                // FactionObjectiveDef pickResourceCratesObjective = DefCache.GetDef<FactionObjectiveDef>("PickResourceItems_CustomMissionObjective");

                List<CustomMissionTypeDef> ambushMissions = new List<CustomMissionTypeDef>()
                {
                    DefCache.GetDef<CustomMissionTypeDef>("AmbushAlien_CustomMissionTypeDef"),
                    DefCache.GetDef<CustomMissionTypeDef>("AmbushAN_CustomMissionTypeDef"),
                    DefCache.GetDef<CustomMissionTypeDef>("AmbushBandits_CustomMissionTypeDef"),
DefCache.GetDef<CustomMissionTypeDef>("AmbushFallen_CustomMissionTypeDef"),
DefCache.GetDef<CustomMissionTypeDef>("AmbushNJ_CustomMissionTypeDef"),
DefCache.GetDef<CustomMissionTypeDef>("AmbushPure_CustomMissionTypeDef"),
DefCache.GetDef<CustomMissionTypeDef>("AmbushPure_CustomMissionTypeDef"),
DefCache.GetDef<CustomMissionTypeDef>("AmbushSY_CustomMissionTypeDef")
            };

                foreach (CustomMissionTypeDef ambush in ambushMissions)
                {
                    ambush.ParticipantsData[0].ReinforcementsTurns.Max = 2;
                    ambush.ParticipantsData[0].ReinforcementsTurns.Min = 2;
                    ambush.CratesDeploymentPointsRange = sourceScavCratesALN.CratesDeploymentPointsRange;
                    ambush.MissionSpecificCrates = sourceScavCratesALN.MissionSpecificCrates;
                    ambush.FactionItemsRange = sourceScavCratesALN.FactionItemsRange;
                    ambush.CratesDeploymentPointsRange.Min = 30;
                    ambush.CratesDeploymentPointsRange.Max = 50;
                    ambush.CustomObjectives[2] = sourceScavCratesALN.CustomObjectives[2];
                    //ambush.Tags.Add(DefCache.GetDef<MissionTagDef>("Contains_ResourceItems_MissionTagDef"));
                    ambush.Outcomes = sourceScavCratesALN.Outcomes;
                    //VOLAND TESTING
                    ambush.ClearMissionOnCancel = true;
                    ambush.MandatoryMission = true;
                }

                //Reduce XP for Ambush mission
                SurviveTurnsFactionObjectiveDef surviveAmbush_CustomMissionObjective = DefCache.GetDef<SurviveTurnsFactionObjectiveDef>("SurviveAmbush_CustomMissionObjective");
                surviveAmbush_CustomMissionObjective.MissionObjectiveData.ExperienceReward = 100;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ModifyPandoranProgress()
        {
            try
            {
                // All sources of evolution due to scaling removed, leaving only evolution per day
                // Additional source of evolution will be number of surviving Pandoran colonies, modulated by difficulty level
                GameDifficultyLevelDef veryhard = DefCache.GetDef<GameDifficultyLevelDef>("VeryHard_GameDifficultyLevelDef");
                //Hero
                GameDifficultyLevelDef hard = DefCache.GetDef<GameDifficultyLevelDef>("Hard_GameDifficultyLevelDef");
                //Standard
                GameDifficultyLevelDef standard = DefCache.GetDef<GameDifficultyLevelDef>("Standard_GameDifficultyLevelDef");
                //Easy
                GameDifficultyLevelDef easy = DefCache.GetDef<GameDifficultyLevelDef>("Easy_GameDifficultyLevelDef");

                veryhard.NestLimitations.MaxNumber = 3; //vanilla 6
                veryhard.NestLimitations.HoursBuildTime = 90; //vanilla 45
                veryhard.LairLimitations.MaxNumber = 3; // vanilla 5
                veryhard.LairLimitations.MaxConcurrent = 3; //vanilla 4
                veryhard.LairLimitations.HoursBuildTime = 100; //vanilla 50
                veryhard.CitadelLimitations.HoursBuildTime = 180; //vanilla 60
                veryhard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                veryhard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                veryhard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                veryhard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                veryhard.ApplyInfestationOutcomeChange = 0;
                veryhard.ApplyDamageHavenOutcomeChange = 0;
                veryhard.StartingSquadTemplate[0] = hard.TutorialStartingSquadTemplate[1];
                veryhard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[2];

                //Making HPC residual
                veryhard.MinPopulationThreshold = 3;
                hard.MinPopulationThreshold = 3;
                standard.MinPopulationThreshold = 3;
                easy.MinPopulationThreshold = 3;


                veryhard.RecruitCostPerLevelMultiplier = 0.4f;


                // PX_Jacob_Tutorial2_TacCharacterDef replace [3], with hard starting squad [1]
                // PX_Sophia_Tutorial2_TacCharacterDef replace [1], with hard starting squad [2]

                //reducing evolution per day because there other sources of evolution points now
                veryhard.EvolutionProgressPerDay = 70; //vanilla 100

                hard.NestLimitations.MaxNumber = 3; //vanilla 5
                hard.NestLimitations.HoursBuildTime = 90; //vanilla 50
                hard.LairLimitations.MaxNumber = 3; // vanilla 4
                hard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                hard.LairLimitations.HoursBuildTime = 100; //vanilla 80
                hard.CitadelLimitations.HoursBuildTime = 180; //vanilla 100
                hard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                hard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                hard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                hard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                hard.ApplyInfestationOutcomeChange = 0;
                hard.ApplyDamageHavenOutcomeChange = 0;
                hard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                hard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                hard.RecruitCostPerLevelMultiplier = 0.3f;

                //reducing evolution per day because there other sources of evolution points now
                hard.EvolutionProgressPerDay = 50; //vanilla 70; moved from 60 in Update#6


                standard.NestLimitations.MaxNumber = 3; //vanilla 4
                standard.NestLimitations.HoursBuildTime = 90; //vanilla 55
                standard.LairLimitations.MaxNumber = 3; // vanilla 3
                standard.LairLimitations.MaxConcurrent = 3; //vanilla 3
                standard.LairLimitations.HoursBuildTime = 100; //vanilla 120
                standard.CitadelLimitations.HoursBuildTime = 180; //vanilla 145
                standard.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                standard.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                standard.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                standard.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                standard.ApplyDamageHavenOutcomeChange = 0;
                standard.ApplyInfestationOutcomeChange = 0;
                standard.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                standard.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                //reducing evolution per day because there other sources of evolution points now
                standard.EvolutionProgressPerDay = 40; //vanilla 55


                easy.NestLimitations.HoursBuildTime = 90; //vanilla 60 
                easy.LairLimitations.HoursBuildTime = 100; // vanilla 150
                easy.CitadelLimitations.HoursBuildTime = 180; // vanilla 180
                easy.EvolutionPointsGainOnMissionLoss = 0; //vanilla 10
                easy.AlienBaseTypeEvolutionParams[0].EvolutionPerDestroyedBase = 0; //vanilla 10
                easy.AlienBaseTypeEvolutionParams[1].EvolutionPerDestroyedBase = 0; //vanilla 20
                easy.AlienBaseTypeEvolutionParams[2].EvolutionPerDestroyedBase = 0; //vanilla 40
                easy.ApplyInfestationOutcomeChange = 0;
                easy.ApplyDamageHavenOutcomeChange = 0;
                easy.StartingSquadTemplate[1] = hard.TutorialStartingSquadTemplate[1];
                easy.StartingSquadTemplate[3] = hard.TutorialStartingSquadTemplate[2];

                //keeping evolution per day because low enough already
                easy.EvolutionProgressPerDay = 35; //vanilla 35

                //Remove faction diplo penalties for not destroying revealed PCs and increase rewards for haven leader
                GeoAlienBaseTypeDef nestType = DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef lairType = DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef citadelType = DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef");
                GeoAlienBaseTypeDef palaceType = DefCache.GetDef<GeoAlienBaseTypeDef>("Palace_GeoAlienBaseTypeDef");

                nestType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                nestType.HavenLeaderDiplomacyReward = 12; //vanilla 8 
                nestType.PhoenixBaseAttackCounterPerDay = 5;
                lairType.PhoenixBaseAttackCounterPerDay = 10;
                citadelType.PhoenixBaseAttackCounterPerDay = 20;

                lairType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                lairType.HavenLeaderDiplomacyReward = 16; //vanilla 12 
                citadelType.FactionDiplomacyPenaltyPerHaven = 0; //vanilla -1
                citadelType.HavenLeaderDiplomacyReward = 20; //vanilla 16 

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void InjectAlistairAhsbyLines()
        {
            try
            {
                //Alistair speaks about Symes after completing Symes Retreat
                GeoscapeEventDef alistairOnSymes1 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes1", "PROG_PX10_WIN_TITLE", "KEY_ALISTAIRONSYMES_1_DESCRIPTION", null);
                alistairOnSymes1.GeoscapeEventData.Flavour = "IntroducingSymes";

                //Alistair speaks about Barnabas after Barnabas asks for help
                GeoscapeEventDef alistairOnBarnabas = TFTVCommonMethods.CreateNewEvent("AlistairOnBarnabas", "PROG_CH0_TITLE", "KEY_ALISTAIRONBARNABAS_DESCRIPTION", null);
                alistairOnBarnabas.GeoscapeEventData.Flavour = "DLC4_Generic_NJ";

                //Alistair speaks about Symes after Antarctica discovery
                GeoscapeEventDef alistairOnSymes2 = TFTVCommonMethods.CreateNewEvent("AlistairOnSymes2", "PROG_PX1_WIN_TITLE", "KEY_ALISTAIRONSYMES_2_DESCRIPTION", null);
                alistairOnSymes2.GeoscapeEventData.Flavour = "AntarcticSite_Victory";

                AlistairRoadsEvent();
                CreateEventMessagesFromTheVoid();
                CreateBehemothPattern();
                CreateTrappedInMist();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void InjectOlenaKimLines()

        {
            try
            {
                //Helena reveal about Olena
                GeoscapeEventDef helenaOnOlena = TFTVCommonMethods.CreateNewEvent("HelenaOnOlena", "PROG_LE0_WIN_TITLE", "KEY_OLENA_HELENA_DESCRIPTION", null);
                //Olena about West
                GeoscapeEventDef olenaOnWest = TFTVCommonMethods.CreateNewEvent("OlenaOnWest", "PROG_NJ1_WIN_TITLE", "KEY_OLENAONWEST_DESCRIPTION", null);
                //Olena about Synod
                GeoscapeEventDef olenaOnSynod = TFTVCommonMethods.CreateNewEvent("OlenaOnSynod", "PROG_AN6_WIN2_TITLE", "KEY_OLENAONSYNOD_DESCRIPTION", null);
                //Olena about the Ancients
                GeoscapeEventDef olenaOnAncients = TFTVCommonMethods.CreateNewEvent("OlenaOnAncients", "KEY_OLENAONANCIENTS_TITLE", "KEY_OLENAONANCIENTS_DESCRIPTION", null);
                //Olena about the Behemeoth
                GeoscapeEventDef olenaOnBehemoth = TFTVCommonMethods.CreateNewEvent("OlenaOnBehemoth", "PROG_FS1_WIN_TITLE", "KEY_OLENAONBEHEMOTH_DESCRIPTION", null);
                //Olena about Alistair - missing an event hook!!
                GeoscapeEventDef olenaOnAlistair = TFTVCommonMethods.CreateNewEvent("OlenaOnAlistair", "", "KEY_OLENAONALISTAIR_DESCRIPTION", null);
                //Olena about Symes
                GeoscapeEventDef olenaOnSymes = TFTVCommonMethods.CreateNewEvent("OlenaOnSymes", "PROG_PX1_WIN_TITLE", "KEY_OLENAONSYMES_DESCRIPTION", null);
                //Olena about ending 
                GeoscapeEventDef olenaOnEnding = TFTVCommonMethods.CreateNewEventWithFixedGUID("OlenaOnEnding", "KEY_ALISTAIR_ROADS_TITLE", "KEY_OLENAONENDING_DESCRIPTION", null, "{DBC7B84C-FC51-4704-ACFB-8413DC6C616C}");
                //Olena about Bionics Lab sabotage
                GeoscapeEventDef olenaOnBionicsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnBionicsLabSabotage", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_CHOICE_0_OUTCOME", null);
                //Olena about Mutations Lab sabotage
                GeoscapeEventDef olenaOnMutationsLabSabotage = TFTVCommonMethods.CreateNewEvent("OlenaOnMutationsLabSabotage", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0_OUTCOME", null);
                //Olena First LOTA Event 
                TFTVCommonMethods.CreateNewEvent("OlenaLotaStart", "TFTV_LOTA_START_EVENT_TITLE", "TFTV_LOTA_START_EVENT_DESCRIPTION", null);

                /*olenaOnEnding.EventTypes = new GeoscapeEventType[] { GeoscapeEventType.Undefined};

                List<GeoscapeEventType> list = olenaOnEnding.EventTypes.ToList<GeoscapeEventType>();
               

                    TFTVLogger.Always($"{list.Count}");

                foreach(GeoscapeEventType eventType in list) 
                {
                    TFTVLogger.Always($"{eventType.GetName()}");
                
                }*/


                CreateEventFirstFlyer();
                CreateEventFirstHavenTarget();
                CreateEventFirstHavenAttack();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void AlistairRoadsEvent()
        {
            try
            {
                string title = "KEY_ALISTAIR_ROADS_TITLE";
                string description = "KEY_ALISTAIR_ROADS_DESCRIPTION";
                string passToOlena = "OlenaOnEnding";

                string startingEvent = "AlistairRoads";
                string afterWest = "AlistairRoadsNoWest";
                string afterSynedrion = "AlistairRoadsNoSynedrion";
                string afterAnu = "AlistairRoadsNoAnu";
                string afterVirophage = "AlistairRoadsNoVirophage";

                string questionAboutWest = "KEY_ALISTAIRONWEST_CHOICE";
                string questionAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_CHOICE";
                string questionAboutAnu = "KEY_ALISTAIRONANU_CHOICE";
                string questionAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_CHOICE";
                //   string questionAboutHelena = "KEY_ALISTAIRONHELENA_CHOICE";
                string noMoreQuestions = "KEY_ALISTAIR_ROADS_ALLDONE";

                string answerAboutWest = "KEY_ALISTAIRONWEST_DESCRIPTION";
                string answerAboutSynedrion = "KEY_ALISTAIRONSYNEDRION_DESCRIPTION";
                string answerAboutAnu = "KEY_ALISTAIRONANU_DESCRIPTION";
                string answerAboutVirophage = "KEY_ALISTAIRONVIROPHAGE_DESCRIPTION";
                //   string answerAboutHelena = "KEY_ALISTAIRONHELENA_DESCRIPTION";
                string promptMoreQuestions = "KEY_ALISTAIR_ROADS_DESCRIPTION_2";

                GeoscapeEventDef alistairRoads = TFTVCommonMethods.CreateNewEventWithFixedGUID(startingEvent, title, description, null, "{4228E183-2167-4E03-9D28-D620BC47C4D4}");
                GeoscapeEventDef alistairRoadsAfterWest = TFTVCommonMethods.CreateNewEventWithFixedGUID(afterWest, title, promptMoreQuestions, null, "{210A6D83-7FF6-4F4A-A9F6-509003E26446}");
                GeoscapeEventDef alistairRoadsAfterSynedrion = TFTVCommonMethods.CreateNewEventWithFixedGUID(afterSynedrion, title, promptMoreQuestions, null, "{3563AC82-7683-4F47-A09F-B27E56116427}");
                GeoscapeEventDef alistairRoadsAfterAnu = TFTVCommonMethods.CreateNewEventWithFixedGUID(afterAnu, title, promptMoreQuestions, null, "{9934068A-F0F2-4497-8EA5-169FA4243523}");
                GeoscapeEventDef alistairRoadsAfterVirophage = TFTVCommonMethods.CreateNewEventWithFixedGUID(afterVirophage, title, promptMoreQuestions, null, "{90CEABA2-00F4-4E44-85A9-D3F707C8AC4E}");


                // List<GeoscapeEventDef> geoscapeEventDefs = new List<GeoscapeEventDef>() {alistairRoadsAfterWest, alistairRoadsAfterSynedrion, alistairRoadsAfterAnu, alistairRoadsAfterVirophage};

                alistairRoads.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoads.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoads, questionAboutWest, answerAboutWest, afterWest);
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoads, questionAboutSynedrion, answerAboutSynedrion, afterSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoads, questionAboutAnu, answerAboutAnu, afterAnu);

                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterWest.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoadsAfterWest, questionAboutSynedrion, answerAboutSynedrion, afterSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoadsAfterWest, questionAboutAnu, answerAboutAnu, afterAnu);
                TFTVCommonMethods.GenerateGeoEventChoiceWithEventTrigger(alistairRoadsAfterWest, questionAboutVirophage, answerAboutVirophage, afterVirophage);

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutAnu, answerAboutAnu);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterSynedrion, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterAnu, questionAboutVirophage, answerAboutVirophage);

                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Text.LocalizationKey = noMoreQuestions;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[0].Outcome.TriggerEncounterID = passToOlena;
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutWest, answerAboutWest);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutSynedrion, answerAboutSynedrion);
                TFTVCommonMethods.GenerateGeoEventChoice(alistairRoadsAfterVirophage, questionAboutAnu, answerAboutAnu);


                /*  alistairRoads.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                  alistairRoads.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                  alistairRoads.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;*/

                /* alistairRoadsAfterWest.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterSynedrion;
                 alistairRoadsAfterWest.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                 alistairRoadsAfterWest.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;*/

                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterAnu;
                alistairRoadsAfterSynedrion.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;



                alistairRoadsAfterAnu.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterAnu.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterVirophage;



                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[1].Outcome.TriggerEncounterID = afterWest;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[2].Outcome.TriggerEncounterID = afterSynedrion;
                alistairRoadsAfterVirophage.GeoscapeEventData.Choices[3].Outcome.TriggerEncounterID = afterAnu;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void CreateIntro()
        {
            try
            {
                string introEvent_0 = "IntroBetterGeo_0";
                string introEvent_1 = "IntroBetterGeo_1";
                string introEvent_2 = "IntroBetterGeo_2";
                GeoscapeEventDef intro0 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_0, "BG_INTRO_0_TITLE", "BG_INTRO_0_DESCRIPTION", null);
                GeoscapeEventDef intro1 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_1, "BG_INTRO_1_TITLE", "BG_INTRO_1_DESCRIPTION", null);
                intro1.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO1_CHOICE_1";
                GeoscapeEventDef intro2 = TFTVCommonMethods.CreateNewEvent(
                    introEvent_2, "BG_INTRO_2_TITLE", "BG_INTRO_2_DESCRIPTION", null);
                intro2.GeoscapeEventData.Choices[0].Text.LocalizationKey = "BG_INTRO_2_CHOICE_0";


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        public static void RemoveCorruptionDamageBuff()
        {
            try
            {
                CorruptionStatusDef corruption_StatusDef = DefCache.GetDef<CorruptionStatusDef>("Corruption_StatusDef");
                corruption_StatusDef.Multiplier = 0.0f;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        public static void ChangesToMedbay()
        {
            try
            {
                HealFacilityComponentDef e_HealMedicalBay_PhoenixFacilityDe = DefCache.GetDef<HealFacilityComponentDef>("E_Heal [MedicalBay_PhoenixFacilityDef]");
                e_HealMedicalBay_PhoenixFacilityDe.BaseHeal = 16;
                PhoenixFacilityDef medbay = DefCache.GetDef<PhoenixFacilityDef>("MedicalBay_PhoenixFacilityDef");
                medbay.ConstructionTimeDays = 1.5f;
                medbay.ResourceCost = new ResourcePack
                {
                    new ResourceUnit { Type = ResourceType.Materials, Value = 200 },
                    new ResourceUnit { Type = ResourceType.Tech, Value = 50 }
                };

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        public static void MistOnAllMissions()
        {
            try
            {
                foreach (CustomMissionTypeDef missionTypeDef in Repo.GetAllDefs<CustomMissionTypeDef>())
                {
                    missionTypeDef.SpawnMistAtLevelStart = true;
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        internal static void CreateUmbraImmunities()
        {
            try
            {
                AbilityDef paralysisImmunity = DefCache.GetDef<AbilityDef>("ParalysisNotShockImmunity_DamageMultiplierAbilityDef");
                AbilityDef poisonImmunity = DefCache.GetDef<AbilityDef>("PoisonImmunity_DamageMultiplierAbilityDef");
                AbilityDef psychicResistance = DefCache.GetDef<AbilityDef>("PsychicResistant_DamageMultiplierAbilityDef");

                AddAttackBoostStatusDef corruptionAttack = DefCache.GetDef<AddAttackBoostStatusDef>("CorruptionAttack_StatusDef");

                List<AbilityDef> abilityDefs = new List<AbilityDef>() { paralysisImmunity, poisonImmunity, psychicResistance };

                TacticalActorDef oilcrabDef = DefCache.GetDef<TacticalActorDef>("Oilcrab_ActorDef");
                oilcrabDef.StatusEffects.Add(corruptionAttack);

                List<AbilityDef> ocAbilities = new List<AbilityDef>(oilcrabDef.Abilities);
                ocAbilities.AddRange(abilityDefs);
                oilcrabDef.Abilities = ocAbilities.ToArray();

                TacticalActorDef oilfishDef = DefCache.GetDef<TacticalActorDef>("Oilfish_ActorDef");
                List<AbilityDef> ofAbilities = new List<AbilityDef>(oilfishDef.Abilities);
                ofAbilities.AddRange(abilityDefs);
                oilfishDef.Abilities = ofAbilities.ToArray();
                oilfishDef.StatusEffects.Add(corruptionAttack);
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static void ChangeUmbra()

        {
            try
            {
                TFTVUITactical.Enemies.SetUmbraIcons();


                ViewElementDef umbraFishViewElement = DefCache.GetDef<ViewElementDef>("ViewElement [Oilfish_ViewElementDef]");
                ViewElementDef umbraCrabViewElement = DefCache.GetDef<ViewElementDef>("ViewElement [Oilcrab_ViewElementDef]");



                Sprite umbraArthronIcon = TFTVUITactical.Enemies.GetUmbraArthronIcon();
                Sprite umbraTritonIcon = TFTVUITactical.Enemies.GetUmbraTritonIcon();

                umbraCrabViewElement.SmallIcon = umbraArthronIcon;
                umbraCrabViewElement.LargeIcon = umbraArthronIcon;

                umbraFishViewElement.SmallIcon = umbraTritonIcon;
                umbraFishViewElement.LargeIcon = umbraTritonIcon;

                Sprite umbraArthronRepresentation = Helper.CreateSpriteFromImageFile("Crabman_Oil_Portrait_uinomipmaps.png");
                Sprite umbraTritonRepresenation = Helper.CreateSpriteFromImageFile("Fishman_Oil_Portrait_uinomipmaps.png");

                RandomValueEffectConditionDef randomValueFishUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralFishmen_FactionEffectDef]");
                RandomValueEffectConditionDef randomValueCrabUmbra = DefCache.GetDef<RandomValueEffectConditionDef>("E_RandomValue [UmbralCrabmen_FactionEffectDef]");
                randomValueCrabUmbra.ThresholdValue = 0;
                randomValueFishUmbra.ThresholdValue = 0;
                EncounterVariableResearchRequirementDef sourceVarResReq =
                   DefCache.GetDef<EncounterVariableResearchRequirementDef>("NJ_Bionics1_ResearchDef_EncounterVariableResearchRequirementDef_0");
                //Changing Umbra Crab and Triton to appear after SDI event 3;
                ResearchDef umbraCrabResearch = DefCache.GetDef<ResearchDef>("ALN_CrabmanUmbra_ResearchDef");

                //Creating new Research Requirement, requiring a variable to be triggered  
                string variableUmbraALNResReq = "Umbra_Encounter_Variable";
                EncounterVariableResearchRequirementDef variableResReqUmbra = Helper.CreateDefFromClone(sourceVarResReq, "0CCC30E0-4DB1-44CD-9A60-C1C8F6588C8A", "UmbraResReqDef");
                variableResReqUmbra.VariableName = variableUmbraALNResReq;
                // This changes the Umbra reserach so that 2 conditions have to be fulfilled: 1) a) nest has to be researched, or b) exotic material has to be found
                // (because 1)a) is fufilled at start of the game, b) is redundant but harmless), and 2) a special variable has to be triggered, assigned to event sdi3
                umbraCrabResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraCrabResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraCrabResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Now same thing for Triton Umbra, but it will use same variable because we want them to appear at the same time
                ResearchDef umbraFishResearch = DefCache.GetDef<ResearchDef>("ALN_FishmanUmbra_ResearchDef");
                umbraFishResearch.RevealRequirements.Operation = ResearchContainerOperation.ALL;
                umbraFishResearch.RevealRequirements.Container[0].Operation = ResearchContainerOperation.ANY;
                umbraFishResearch.RevealRequirements.Container[1].Requirements[0] = variableResReqUmbra;
                //Because Triton research has 2 requirements in the second container, we set them to any
                umbraFishResearch.RevealRequirements.Container[1].Operation = ResearchContainerOperation.ANY;

                ViewElementDef oilCrabViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [Oilcrab_Torso_BodyPartDef]");
                oilCrabViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_NAME";
                oilCrabViewElementDef.Description.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_DESCRIPTION";
                oilCrabViewElementDef.SmallIcon = umbraArthronRepresentation;
                oilCrabViewElementDef.LargeIcon = umbraArthronRepresentation;
                oilCrabViewElementDef.InventoryIcon = umbraArthronRepresentation;

                ViewElementDef oilFishViewElementDef = DefCache.GetDef<ViewElementDef>("E_View [Oilfish_Torso_BodyPartDef]");
                oilFishViewElementDef.DisplayName1.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_NAME";
                oilFishViewElementDef.Description.LocalizationKey = "TFTV_KEY_UMBRA_TARGET_DISPLAY_DESCRIPTION";
                oilFishViewElementDef.SmallIcon = umbraTritonRepresenation;
                oilFishViewElementDef.LargeIcon = umbraTritonRepresenation;
                oilFishViewElementDef.InventoryIcon = umbraTritonRepresenation;

                TacticalPerceptionDef oilCrabPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Oilcrab_PerceptionDef");
                TacticalPerceptionDef oilFishPerceptionDef = DefCache.GetDef<TacticalPerceptionDef>("Oilfish_PerceptionDef");
                oilCrabPerceptionDef.PerceptionRange = 30.0f;
                oilFishPerceptionDef.PerceptionRange = 30.0f;
                //
                AddAbilityStatusDef oilTritonAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilFish_AddAbilityStatusDef");
                oilTritonAddAbilityStatus.ApplicationConditions = new EffectConditionDef[] { };
                AddAbilityStatusDef oilCrabAddAbilityStatus = DefCache.GetDef<AddAbilityStatusDef>("OilCrab_AddAbilityStatusDef");
                oilCrabAddAbilityStatus.ApplicationConditions = new EffectConditionDef[] { };

                TacticalActorDef oilfishActorDef = DefCache.GetDef<TacticalActorDef>("Oilfish_ActorDef");
                TacticalActorDef oilcrabActorDef = DefCache.GetDef<TacticalActorDef>("Oilcrab_ActorDef");

                oilcrabActorDef.GameTags.Remove(DefCache.GetDef<ClassTagDef>("Crabman_ClassTagDef"));
                oilfishActorDef.GameTags.Remove(DefCache.GetDef<ClassTagDef>("Fishman_ClassTagDef"));

                CreateUmbraImmunities();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }
        public static void Create_VoidOmen_Events()

        {
            for (int x = 0; x < 20; x++)
            {
                GeoscapeEventDef geoscapeEventDef = TFTVCommonMethods.CreateNewEvent($"VoidOmen_{x}", "", "", null);
                geoscapeEventDef.GeoscapeEventData.Flavour = "IntroducingSymes";
            }


            TFTVCommonMethods.CreateNewEvent("IntroVoidOmen", "", "", null);

        }

        public static void AugmentationEventsDefs()
        {
            try
            {
                //ID all the factions for later
                GeoFactionDef phoenixPoint = DefCache.GetDef<GeoFactionDef>("Phoenix_GeoPhoenixFactionDef");
                GeoFactionDef newJericho = DefCache.GetDef<GeoFactionDef>("NewJericho_GeoFactionDef");
                GeoFactionDef anu = DefCache.GetDef<GeoFactionDef>("Anu_GeoFactionDef");
                GeoFactionDef synedrion = DefCache.GetDef<GeoFactionDef>("Synedrion_GeoFactionDef");

                //Anu pissed at player for doing Bionics
                GeoscapeEventDef anuPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed1", "ANU_PISSED_BIONICS_TITLE", "ANU_PISSED_BIONICS_TEXT_GENERAL_0", "ANU_PISSED_BIONICS_CHOICE_0_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_PISSED_BIONICS_CHOICE_0";

                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(synedrion, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_1", "ANU_PISSED_BIONICS_CHOICE_1_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -8));
                anuPissedAtBionics.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(anuPissedAtBionics, "ANU_PISSED_BIONICS_CHOICE_2", "ANU_PISSED_BIONICS_CHOICE_2_OUTCOME");
                anuPissedAtBionics.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_Anu_Pissed_Made_Promise", 1, true));


                //Anu really pissed at player for doing Bionics
                GeoscapeEventDef anuReallyPissedAtBionics = TFTVCommonMethods.CreateNewEvent("Anu_Pissed2", "ANU_REALLY_PISSED_BIONICS_TITLE", "ANU_REALLY_PISSED_BIONICS_TEXT_GENERAL_0", null);
                anuReallyPissedAtBionics.GeoscapeEventData.Leader = "AN_Synod";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Text.LocalizationKey = "ANU_REALLY_PISSED_BIONICS_CHOICE_0";
                anuReallyPissedAtBionics.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, -6));

                //NJ pissed at player for doing Mutations
                GeoscapeEventDef nJPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed1", "NJ_PISSED_MUTATIONS_TITLE", "NJ_PISSED_MUTATIONS_TEXT_GENERAL_0", "NJ_PISSED_MUTATIONS_CHOICE_0_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_PISSED_MUTATIONS_CHOICE_0";
                nJPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -5));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_1", "NJ_PISSED_MUTATIONS_CHOICE_1_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -8));
                nJPissedAtMutations.GeoscapeEventData.Choices[1].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(anu, phoenixPoint, +2));
                TFTVCommonMethods.GenerateGeoEventChoice(nJPissedAtMutations, "NJ_PISSED_MUTATIONS_CHOICE_2", "NJ_PISSED_MUTATIONS_CHOICE_2_OUTCOME");
                nJPissedAtMutations.GeoscapeEventData.Choices[2].Outcome.VariablesChange.Add(TFTVCommonMethods.GenerateVariableChange("BG_NJ_Pissed_Made_Promise", 1, true));

                //NJ really pissed at player for doing Mutations
                GeoscapeEventDef nJReallyPissedAtMutations = TFTVCommonMethods.CreateNewEvent("NJ_Pissed2", "NJ_REALLY_PISSED_MUTATIONS_TITLE", "NJ_REALLY_PISSED_MUTATIONS_TEXT_GENERAL_0", null);
                nJReallyPissedAtMutations.GeoscapeEventData.Leader = "NJ_TW";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Text.LocalizationKey = "NJ_REALLY_PISSED_MUTATIONS_CHOICE_0";
                nJReallyPissedAtMutations.GeoscapeEventData.Choices[0].Outcome.Diplomacy.Add(TFTVCommonMethods.GenerateDiplomacyOutcome(newJericho, phoenixPoint, -6));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Story events:

        public static void CreateEventFirstFlyer()
        {
            try
            {
                string eventID = "OlenaOnFirstFlyer";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_FLYER_TITLE", "OLENA_ON_FIRST_FLYER_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenTarget()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenTarget";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_FIRST_HAVEN_TARGET_TITLE", "OLENA_ON_FIRST_HAVEN_TARGET_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventFirstHavenAttack()
        {
            try
            {
                string eventID = "OlenaOnFirstHavenAttack";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "FIRST_HAVEN_ATTACK_TITLE", "FIRST_HAVEN_ATTACK_TEXT", "FIRST_HAVEN_ATTACK_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateEventMessagesFromTheVoid()
        {
            try
            {
                string eventID = "AlistairOnMessagesFromTheVoid";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "AFTER_YE_SIGNAL_TITLE", "AFTER_YE_SIGNAL_TEXT", "AFTER_YE_SIGNAL_OUTCOME");
                newEvent.GeoscapeEventData.EventID = eventID;
                newEvent.GeoscapeEventData.Choices[0].Text.LocalizationKey = "AFTER_YE_SIGNAL_CHOICE";

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateBehemothPattern()
        {
            try
            {
                string eventID = "OlenaOnBehemothPattern";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "BEHEMOTH_PATTERN_TITLE", "BEHEMOTH_PATTERN_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static void CreateTrappedInMist()
        {
            try
            {
                string eventID = "OlenaOnHavenInfested";
                GeoscapeEventDef newEvent = TFTVCommonMethods.CreateNewEvent(eventID + "_GeoscapeEventDef", "OLENA_ON_HAVEN_INFESTED_TITLE", "OLENA_ON_HAVEN_INFESTED_TEXT", null);
                newEvent.GeoscapeEventData.EventID = eventID;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Leftovers, not used.
        /// </summary>

        internal static void TestingKnockBackRepositionAlternative()
        {
            try
            {
                string nameKnockBack = "KnockBackAbility";
                string gUIDAbility = "{B4238D2D-3E25-4EE5-A3C0-23CFED493D42}";

                RepositionAbilityDef source = DefCache.GetDef<RepositionAbilityDef>("Dash_AbilityDef");
                RepositionAbilityDef newKnockBackAbility = Helper.CreateDefFromClone(source, gUIDAbility, nameKnockBack);
                newKnockBackAbility.ActionPointCost = 0.0f;
                newKnockBackAbility.WillPointCost = 0.0f;
                newKnockBackAbility.UsesPerTurn = -1;
                newKnockBackAbility.EventOnActivate = new TacticalEventDef();
                newKnockBackAbility.AmountOfMovementToUseAsRange = 0;
                // newKnockBackAbility.FumblePerc = 0;
                newKnockBackAbility.TraitsRequired = new string[] { };
                //    newKnockBackAbility.HeightToWidth = 0.01f;
                //  newKnockBackAbility.TesellationPoints = 10;
                // newKnockBackAbility.UseLeapAnimation = true;


                string gUIDTargeting = "{8B266029-F014-4514-865A-C51201944385}";
                TacticalTargetingDataDef tacticalTargetingDataDef = Helper.CreateDefFromClone(source.TargetingDataDef, gUIDTargeting, nameKnockBack);
                tacticalTargetingDataDef.Origin.Range = 3;

                /*   string gUIDAnim = "{B1ADC473-1AD8-431F-8953-953E4CB3E584}";
                   TacActorJumpAbilityAnimActionDef animSource = DefCache.GetDef<TacActorJumpAbilityAnimActionDef>("E_JetJump [Soldier_Utka_AnimActionsDef]");
                   TacActorJumpAbilityAnimActionDef knockBackAnimation = Helper.CreateDefFromClone(animSource, gUIDAnim, nameKnockBack);
                   TacActorNavAnimActionDef someAnimations = DefCache.GetDef<TacActorNavAnimActionDef>("E_CrabmanNav [Crabman_AnimActionsDef]");
                   TacActorSimpleReactionAnimActionDef hurtReaction = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction [Crabman_AnimActionsDef]");
                   /*  knockBackAnimation.Clip = hurtReaction.GetAllClips().First();
                     knockBackAnimation.ClipEnd = someAnimations.FallNoSupport.Stop;
                     knockBackAnimation.ClipStart = hurtReaction.GetAllClips().First();*/
                /*  knockBackAnimation.Clip = someAnimations.JetJump.Loop;
                  knockBackAnimation.ClipEnd = hurtReaction.GetAllClips().First();
                  knockBackAnimation.ClipStart = someAnimations.JetJump.Loop;

                  knockBackAnimation.AbilityDefs = new AbilityDef[] { newKnockBackAbility };



                  TacActorAnimActionsDef crabAnimActions = DefCache.GetDef<TacActorAnimActionsDef>("Crabman_AnimActionsDef");
                  List<TacActorAnimActionBaseDef> crabAnimations = new List<TacActorAnimActionBaseDef>(crabAnimActions.AnimActions.ToList());
                  crabAnimations.Add(knockBackAnimation);
                  crabAnimActions.AnimActions = crabAnimations.ToArray();*/


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void TestingKnockBack()
        {
            try
            {
                string nameKnockBack = "KnockBackAbility";
                string gUIDAbility = "{B4238D2D-3E25-4EE5-A3C0-23CFED493D42}";

                JetJumpAbilityDef source = DefCache.GetDef<JetJumpAbilityDef>("JetJump_AbilityDef");
                JetJumpAbilityDef newKnockBackAbility = Helper.CreateDefFromClone(source, gUIDAbility, nameKnockBack);
                newKnockBackAbility.ActionPointCost = 0.0f;
                newKnockBackAbility.WillPointCost = 0.0f;
                newKnockBackAbility.FumblePerc = 0;
                newKnockBackAbility.TraitsRequired = new string[] { };
                newKnockBackAbility.HeightToWidth = 0.01f;
                //  newKnockBackAbility.TesellationPoints = 10;
                // newKnockBackAbility.UseLeapAnimation = true;


                string gUIDTargeting = "{8B266029-F014-4514-865A-C51201944385}";
                TacticalTargetingDataDef tacticalTargetingDataDef = Helper.CreateDefFromClone(source.TargetingDataDef, gUIDTargeting, nameKnockBack);
                tacticalTargetingDataDef.Origin.Range = 1;

                string gUIDAnim = "{B1ADC473-1AD8-431F-8953-953E4CB3E584}";
                TacActorJumpAbilityAnimActionDef animSource = DefCache.GetDef<TacActorJumpAbilityAnimActionDef>("E_JetJump [Soldier_Utka_AnimActionsDef]");
                TacActorJumpAbilityAnimActionDef knockBackAnimation = Helper.CreateDefFromClone(animSource, gUIDAnim, nameKnockBack);
                TacActorNavAnimActionDef someAnimations = DefCache.GetDef<TacActorNavAnimActionDef>("E_CrabmanNav [Crabman_AnimActionsDef]");
                TacActorSimpleReactionAnimActionDef hurtReaction = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction [Crabman_AnimActionsDef]");
                /*  knockBackAnimation.Clip = hurtReaction.GetAllClips().First();
                  knockBackAnimation.ClipEnd = someAnimations.FallNoSupport.Stop;
                  knockBackAnimation.ClipStart = hurtReaction.GetAllClips().First();*/
                knockBackAnimation.Clip = someAnimations.JetJump.Loop;
                knockBackAnimation.ClipEnd = hurtReaction.GetAllClips().First();
                knockBackAnimation.ClipStart = someAnimations.JetJump.Loop;

                knockBackAnimation.AbilityDefs = new AbilityDef[] { newKnockBackAbility };



                TacActorAnimActionsDef crabAnimActions = DefCache.GetDef<TacActorAnimActionsDef>("Crabman_AnimActionsDef");
                List<TacActorAnimActionBaseDef> crabAnimations = new List<TacActorAnimActionBaseDef>(crabAnimActions.AnimActions.ToList());
                crabAnimations.Add(knockBackAnimation);
                crabAnimActions.AnimActions = crabAnimations.ToArray();


            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        //Causes too many issues
        internal static void MakeUmbraNotObstacle()
        {
            try
            {

                TacticalActorDef oilcrab = DefCache.GetDef<TacticalActorDef>("Oilcrab_ActorDef");
                TacticalActorDef oilfish = DefCache.GetDef<TacticalActorDef>("Oilfish_ActorDef");

                DefCache.GetDef<TacticalNavigationComponentDef>("Oilcrab_NavigationDef").CreateNavObstacle = false;
                DefCache.GetDef<TacticalNavigationComponentDef>("Oilfish_NavigationDef").CreateNavObstacle = false;

                DieAbilityDef source = DefCache.GetDef<DieAbilityDef>("ArmadilloHulk_DieAbilityDef");
                DieAbilityDef newDieAbility = Helper.CreateDefFromClone(source, "{8654CB01-602D-4204-8A03-2BA50999C1B8}", "DieNoRagDoll");

                RagdollDieAbilityDef oilMonsterRagDollDie = DefCache.GetDef<RagdollDieAbilityDef>("OilMonster_Die_AbilityDef");

                newDieAbility.EventOnActivate = oilMonsterRagDollDie.EventOnActivate;
                newDieAbility.DeathEffect = oilMonsterRagDollDie.DeathEffect;
                newDieAbility.DestroyItems = false;

                oilcrab.Abilities[4] = newDieAbility;
                oilfish.Abilities[4] = newDieAbility;


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void ReEnableFlinching()
        {
            try
            {
                // TacticalLevelController

                TacticalLevelControllerDef tacticalLevelControllerDef = DefCache.GetDef<TacticalLevelControllerDef>("TacticalLevelControllerDef");
                tacticalLevelControllerDef.UseFlinching = true;

                AddonsComponentDef fishmanAddons = DefCache.GetDef<AddonsComponentDef>("Fishman_AddonsComponentDef");
                TacActorSimpleReactionAnimActionDef fishReactionAnim = DefCache.GetDef<TacActorSimpleReactionAnimActionDef>("E_Hurt_Reaction_01Hands [Fishman_AnimActionsDef]");
                TacActorAnimActionsDef fishAnimations = DefCache.GetDef<TacActorAnimActionsDef>("Fishman_AnimActionsDef");
                fishAnimations.DefaultReactionClip = fishReactionAnim.GetAllClips().First();

                //  fishmanAddons.InitialRagdollMode = CollidersRagdollActivationMode.Ragdoll;

                RagdollDummyDef ragdollDummyDef = DefCache.GetDef<RagdollDummyDef>("Generic_RagdollDummyDef");
                ragdollDummyDef.FlinchForceMultiplier = 200f; //2f //4f //5f
                                                              //    ragdollDummyDef.OverrideAngularDrag = 40;
                                                              //   ragdollDummyDef.OverrideDrag = 10;
                ragdollDummyDef.FlinchForceMultiplierSecondary = 50f;
                //   ragdollDummyDef.LeashDamper = 1f;
                ComponentSetDef crabmanComponent = DefCache.GetDef<ComponentSetDef>("Crabman_Template_ComponentSetDef");


                List<ObjectDef> crabComponentSetDefs = new List<ObjectDef>(crabmanComponent.Components);
                crabComponentSetDefs.Insert(crabComponentSetDefs.Count - 2, ragdollDummyDef);
                crabmanComponent.Components = crabComponentSetDefs.ToArray();


                ComponentSetDef fishmanComponent = DefCache.GetDef<ComponentSetDef>("Fishman_ComponentSetDef");


                List<ObjectDef> fishComponentSetDefs = new List<ObjectDef>(fishmanComponent.Components);
                fishComponentSetDefs.Insert(fishComponentSetDefs.Count - 2, ragdollDummyDef);
                fishmanComponent.Components = fishComponentSetDefs.ToArray();


            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void CreateCyclopsScreamStatus()
        {
            try
            {
                BleedStatusDef sourceBleedStatusDef = DefCache.GetDef<BleedStatusDef>("Bleed_StatusDef");

                string statusScreamedLevel1Name = "CyclopsScreamLevel1_BleedStatusDef";
                BleedStatusDef statusScreamedLevel1 = Helper.CreateDefFromClone(sourceBleedStatusDef, "{73C5B78E-E9CB-4558-95AA-807B7AE2755A}", statusScreamedLevel1Name);
                statusScreamedLevel1.EffectName = "CyclopsScreamLevel1";
                statusScreamedLevel1.ApplicationConditions = new EffectConditionDef[] { };
                statusScreamedLevel1.Visuals = Helper.CreateDefFromClone(sourceBleedStatusDef.Visuals, "{A7BADADA-F936-4D28-B171-A4A770A673E7}", statusScreamedLevel1Name);
                statusScreamedLevel1.VisibleOnHealthbar = TacStatusDef.HealthBarVisibility.AlwaysVisible;
                statusScreamedLevel1.VisibleOnPassiveBar = true;
                statusScreamedLevel1.VisibleOnStatusScreen = TacStatusDef.StatusScreenVisibility.VisibleOnStatusesList;
                statusScreamedLevel1.Visuals.DisplayName1.LocalizationKey = "SCREAMED_LEVEL1_TITLE";
                statusScreamedLevel1.Visuals.Description.LocalizationKey = "SCREAMED_LEVEL1_TEXT";
                statusScreamedLevel1.Visuals.LargeIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");
                statusScreamedLevel1.Visuals.SmallIcon = Helper.CreateSpriteFromImageFile("TFTV_status_self_repair.png");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        internal static void CreateSpawnCrabmanAbility()
        {
            try
            {


                string abilityName = "SpawnerySpawnAbility";
                string abilityGUID = "{7FBEB256-1F44-4F3A-8B3A-17A8A8AF9F8F}";

                SpawnActorAbilityDef source = DefCache.GetDef<SpawnActorAbilityDef>("Queen_SpawnFacehugger_AbilityDef");
                SpawnActorAbilityDef newSpawnerySpawnAbility = Helper.CreateDefFromClone(source, abilityGUID, abilityName);

                newSpawnerySpawnAbility.WillPointCost = 0;
                newSpawnerySpawnAbility.AnimType = -1;
                newSpawnerySpawnAbility.EndsTurn = true;

                TacCharacterDef crabman = DefCache.GetDef<TacCharacterDef>("Crabman3_AdvancedCharger_AlienMutationVariationDef");
                newSpawnerySpawnAbility.TacCharacterDef = crabman;
                ComponentSetDef crabmanComponent = DefCache.GetDef<ComponentSetDef>("Crabman_Template_ComponentSetDef");
                newSpawnerySpawnAbility.ActorComponentSetDef = crabmanComponent;
                newSpawnerySpawnAbility.PlaySpawningActorAnimation = true;
                newSpawnerySpawnAbility.FacePosition = false;
                newSpawnerySpawnAbility.OverrideDefaultActionAnimation = false;
                newSpawnerySpawnAbility.WaitsForActionEnd = false;

                TacCharacterDef spawnery = DefCache.GetDef<TacCharacterDef>("SpawningPoolCrabman_AlienMutationVariationDef");
                spawnery.Data.Abilites = new TacticalAbilityDef[] { newSpawnerySpawnAbility };

                AIActionsTemplateDef spawneryAIActionsTemplate = DefCache.GetDef<AIActionsTemplateDef>("SpawningPool_AIActionsTemplateDef");

                string aIActionName = "SpawnerySpawnAIAction";
                string aIActionGUID = "{0598ABF5-6ECF-4AB8-BF3F-DF15636B633A}";
                AIActionExecuteAbilityDef sourceExecuteAbilityAction = DefCache.GetDef<AIActionExecuteAbilityDef>("Queen_SpawnFacehugger_AIActionDef");
                AIActionExecuteAbilityDef spawneryAIAction = Helper.CreateDefFromClone(sourceExecuteAbilityAction, aIActionGUID, aIActionName);


                string aIConsiderationGUID = "{41BE5653-27D3-456D-A76C-E54F8744DAF7}";
                AIAbilityMaxUsesInTheTurnConsiderationDef sourceAbilityMaxUseConsiderion = DefCache.GetDef<AIAbilityMaxUsesInTheTurnConsiderationDef>("Queen_SpawnFacehuggerNotUsed_AIConsiderationDef");
                AIAbilityMaxUsesInTheTurnConsiderationDef spawneryAIConsideration = Helper.CreateDefFromClone(sourceAbilityMaxUseConsiderion, aIConsiderationGUID, aIActionName);


                string aITargetGeneratorGUID = "{20CBA94D-ADF0-42DA-BE90-182096E1B119}";
                AISpawnActorPositionTargetGeneratorDef sourceTargetGenerator = DefCache.GetDef<AISpawnActorPositionTargetGeneratorDef>("Queen_SpawnActorPosition_AITargetGeneratorDef");
                AISpawnActorPositionTargetGeneratorDef spawneryTargetGenerator = Helper.CreateDefFromClone(sourceTargetGenerator, aITargetGeneratorGUID, aIActionName);

                //  List<AIActionDef> aIActionDefs = new List<AIActionDef>(spawneryAIActionsTemplate.ActionDefs.ToList()) { spawneryAIAction };
                //   spawneryAIActionsTemplate.ActionDefs = aIActionDefs.ToArray();

                spawneryAIActionsTemplate.ActionDefs = new AIActionDef[] { spawneryAIAction };

                spawneryAIAction.AbilityDefs = new TacticalAbilityDef[] { newSpawnerySpawnAbility };
                spawneryAIAction.Weight = 1000;
                spawneryAIAction.EarlyExitConsiderations = new AIAdjustedConsideration[] { };
                spawneryAIAction.Evaluations[0].TargetGeneratorDef = spawneryTargetGenerator;
                spawneryAIAction.Evaluations[0].Considerations.RemoveAt(0);

                spawneryTargetGenerator.SpawnActorAbility = newSpawnerySpawnAbility;


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
        }
    }


}
