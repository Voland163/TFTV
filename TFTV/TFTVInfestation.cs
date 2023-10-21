using Base;
using Base.Core;
using Base.Levels;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Events;
using PhoenixPoint.Geoscape.Events.Eventus;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Objectives;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVInfestation
    {
        public static SharedData sharedData = GameUtl.GameComponent<SharedData>();
        //  private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        internal static GeoHavenDefenseMission DefenseMission = null;
        internal static GeoSite GeoSiteForInfestation = null;
        internal static GeoSite GeoSiteForScavenging = null;
        public static string InfestedHavensVariable = "Number_of_Infested_Havens";
        public static string LivingWeaponsAcquired = "Living_Weapons_Acquired";
        public static int roll = 0;

        public static bool InfestationMissionWon = false;


        
        //force Corruption of the Mind to spawn in a haven covered in Mist

        [HarmonyPatch(typeof(GeoEventChoiceOutcome), "GetRandomSiteForEncounter")]
        public static class GeoEventChoiceOutcome_GetRandomSiteForEncounter_CorruptionOfMind_Patch
        {

            public static void Postfix(ref GeoSite __result, GeoLevelController level, string encounterID, GeoSite contextSite, EarthUnits range, GeoActor rangeReference, bool nearestSite = false, bool removeCurrent = true)
            {
                try 
                {
                    if (encounterID == "PROG_FS3_MISS") 
                    {
                        GeoSite anuHaven = __result;
                        __result = null;

                        if (!anuHaven.IsInMist) 
                        {

                            List<GeoSite> list = level.EventSystem.GetValidSitesForEvent(encounterID).Where(gs=>gs.IsInMist).ToList();
                            if (removeCurrent)
                            {
                                list.Remove(contextSite);
                            }

                            if (list.Count > 0)
                            {
                                anuHaven = list.GetRandomElement();
                            }
                        }

                        level.EventSystem.SetVariable("TrappedInTheMistTriggered", 1);
                        level.EventSystem.SetVariable("Number_of_Infested_Havens", level.EventSystem.GetVariable(InfestedHavensVariable) + 1);
                        level.AlienFaction.InfestHaven(anuHaven);
                        anuHaven.RevealSite(level.PhoenixFaction);
                        anuHaven.RefreshVisuals();

                        /* DiplomaticGeoFactionObjective cyclopsObjective = new DiplomaticGeoFactionObjective(controller.PhoenixFaction, controller.PhoenixFaction)
                  {
                      Title = new LocalizedTextBind("BUILD_CYCLOPS_OBJECTIVE"),
                      Description = new LocalizedTextBind("BUILD_CYCLOPS_OBJECTIVE"),
                  };
                  cyclopsObjective.IsCriticalPath = true;
                  controller.PhoenixFaction.AddObjective(cyclopsObjective);*/


                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                   // return true;
                }
            }

        }



        // Copied and adapted from Mad´s Assorted Adjustments


        // Store mission for other patches
        [HarmonyPatch(typeof(GeoHavenDefenseMission), "UpdateGeoscapeMissionState")]
        public static class GeoHavenDefenseMission_UpdateGeoscapeMissionState_StoreMission_Patch
        {


            public static void Prefix(GeoHavenDefenseMission __instance)
            {
                DefenseMission = __instance;

            }

            public static void Postfix()
            {
                DefenseMission = null;
                roll = 0;

            }
        }

        [HarmonyPatch(typeof(GeoSite), "DestroySite")]
        public static class GeoSite_DestroySite_Patch_ConvertDestructionToInfestation
        {


            public static bool Prefix(GeoSite __instance)
            {
                try
                {
                    if (__instance.Type == GeoSiteType.Haven)
                    {
                        // TFTVLogger.Always("DestroySite method called");
                        TFTVLogger.Always("infestation variable is " + __instance.GeoLevel.EventSystem.GetVariable("Infestation_Encounter_Variable"));

                        if (TFTVInfestationStory.HavenPopulation != 0)
                        {
                            InfestationMissionWon = true;
                        }
                        string faction = __instance.Owner.GetPPName();
                        //  TFTVLogger.Always(faction);
                        if (DefenseMission == null)
                        {

                            return true;
                        }

                        IGeoFactionMissionParticipant attacker = DefenseMission.GetEnemyFaction();
                        if (roll == 0)
                        {
                            roll = UnityEngine.Random.Range(1, 7 + TFTVReleaseOnly.DifficultyOrderConverter(__instance.GeoLevel.CurrentDifficultyLevel.Order));
                            TFTVLogger.Always("Infestation roll is " + roll);
                        }

                        int[] rolledVoidOmens = TFTVVoidOmens.CheckFordVoidOmensInPlay(__instance.GeoLevel);
                        if (attacker.PPFactionDef == sharedData.AlienFactionDef && __instance.IsInMist && __instance.GeoLevel.EventSystem.GetVariable("Infestation_Encounter_Variable") > 0
                         && (roll >= 6 || rolledVoidOmens.Contains(17) || __instance.GeoLevel.EventSystem.GetVariable("TrappedInTheMistTriggered") != 1))//to make infestation more likely
                        {
                            GeoSiteForInfestation = __instance;
                            __instance.ActiveMission = null;
                            __instance.ActiveMission?.Cancel();

                            TFTVLogger.Always("We got to here, defense mission should be successful and haven should look infested");
                            __instance.GeoLevel.EventSystem.SetVariable("Number_of_Infested_Havens", __instance.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable) + 1);

                            __instance.RefreshVisuals();

                            if (__instance.GeoLevel.EventSystem.GetVariable("TrappedInTheMistTriggered") != 1)
                            {
                                GeoscapeEventContext geoscapeEventContext = new GeoscapeEventContext(__instance.GeoLevel.AlienFaction, __instance.GeoLevel.PhoenixFaction);
                                __instance.GeoLevel.EventSystem.SetVariable("TrappedInTheMistTriggered", 1);
                                __instance.GeoLevel.EventSystem.TriggerGeoscapeEvent("OlenaOnHavenInfested", geoscapeEventContext);
                            }


                            return false;
                        }
                        //   int roll2 = UnityEngine.Random.Range(0, 10);
                        /*   if (!__instance.IsInMist && rolledVoidOmens.Contains(12) && roll2 > 5)
                           {
                               GeoSiteForScavenging = __instance;
                               TFTVLogger.Always(__instance.SiteName.LocalizeEnglish() + "ready to spawn a scavenging site");
                           }*/

                        TFTVLogger.Always("Defense mission is not null, the conditions for infestation were not fulfilled, so return true");
                        return true;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }


        [HarmonyPatch(typeof(GeoscapeLog), "AddEntry")]
        public static class GeoscapeLog_AddEntry_Patch_ConvertDestructionToInfestation
        {
            public static void Prefix(GeoscapeLogEntry entry)
            {
                try
                {
                    //TFTVLogger.Always("AddEntry method invoked");

                    if (GeoSiteForInfestation != null && GeoSiteForInfestation.SiteName != null && entry != null && entry.Parameters != null && entry.Parameters.Contains(GeoSiteForInfestation.SiteName))
                    {
                        //  TFTVLogger.Always("Attempting to add infestation entry to Log");
                        if (entry == null)
                        {
                            //    TFTVLogger.Always("Failed because entry is null");
                        }
                        else
                        {
                            if (entry.Text == null)
                            {
                                //    TFTVLogger.Always("Failed because entry.text is null");

                            }
                            else
                            {
                                string succumbedToInfestation = TFTVCommonMethods.ConvertKeyToString("KEY_LOG_ENTRY_SUCCUMB_INFESTATION");

                                // TFTVLogger.Always("Entry.text is not null");
                                entry.Text = new LocalizedTextBind($"{GeoSiteForInfestation.Owner} {DefenseMission.Haven.Site.Name} {succumbedToInfestation}", true);
                                // TFTVLogger.Always("The following entry to Log was added" + GeoSiteForInfestation.Owner + " " + DefenseMission.Haven.Site.Name + " has succumbed to Pandoran infestation!");

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

        [HarmonyPatch(typeof(GeoscapeLog), "Map_SiteMissionEnded")]
        public static class GeoscapeLog_Map_SiteMissionEnded_Patch_ConvertDestructionToInfestation
        {

            public static void Postfix(GeoSite site, GeoMission mission)
            {
                try
                {

                    if (GeoSiteForInfestation != null && site == GeoSiteForInfestation && mission is GeoHavenDefenseMission)
                    {
                        TFTVLogger.Always("GeoSiteForInfestation is " + GeoSiteForInfestation.name);
                        site.GeoLevel.AlienFaction.InfestHaven(GeoSiteForInfestation);
                        TFTVLogger.Always("We got to here, haven should be infested!");
                        GeoSiteForInfestation = null;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(InfestedHavenOutcomeDataBind), "ModalShowHandler")]

        public static class InfestedHavenOutcomeDataBind_Patch_ConvertDestructionToInfestation
        {
            // [Obsolete]
            public static bool Prefix(InfestedHavenOutcomeDataBind __instance, UIModal modal, bool ____shown, UIModal ____modal)
            {

                try
                {
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    TFTVLogger.Always("InfestationMissionWon is " + InfestationMissionWon);
                    if (InfestationMissionWon)
                    {
                        if (!____shown)
                        {
                            ____shown = true;
                            if (____modal == null)
                            {
                                ____modal = modal;
                                ____modal.OnModalHide += __instance.ModalHideHandler;
                            }

                            GeoInfestationCleanseMission geoInfestationCleanseMission = (GeoInfestationCleanseMission)modal.Data;
                            GeoSite site = geoInfestationCleanseMission.Site;

                            GeoFaction originalOwner = null;
                            foreach (GeoFaction faction in site.GeoLevel.Factions)
                            {
                                if (faction.PPFactionDef.ShortName == TFTVInfestationStory.OriginalOwner)
                                {
                                    originalOwner = faction;
                                }

                            }

                            List<GeoHaven> geoHavens = originalOwner.Havens.ToList();

                            int populationSaved = (int)(TFTVInfestationStory.HavenPopulation * 0.2);
                            List<GeoHaven> havenToReceiveRefugees = new List<GeoHaven>();

                            foreach (GeoHaven haven in geoHavens)
                            {
                                if (Vector3.Distance(haven.Site.WorldPosition, site.WorldPosition) <= 1.5
                                    && haven.isActiveAndEnabled && !haven.IsInfested && haven.Site != site)
                                {
                                    havenToReceiveRefugees.Add(haven);
                                }
                            }


                            string dynamicDescription = "";


                            string destroyedMonstrosity = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_MONSTROSITY");
                            string around = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_AROUND");
                            string fortunateFew = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_FORTUNATE");
                            string nodeDesroyed = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_NODE_DESTROYED_TITLE");

                            if (havenToReceiveRefugees.Count > 0)
                            {
                                dynamicDescription = $"{around} {(Mathf.RoundToInt(populationSaved / 100)) * 100} {fortunateFew}";

                            }


                            Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                            description.GetComponent<I2.Loc.Localize>().enabled = false;
                            description.text = $"{destroyedMonstrosity} {dynamicDescription}";
                            Text title = __instance.TopBar.Title;
                            title.GetComponent<I2.Loc.Localize>().enabled = false;
                            title.text = nodeDesroyed;


                            __instance.Background.sprite = Helper.CreateSpriteFromImageFile("NodeAlt.jpg");
                            Sprite icon = __instance.CommonResources.GetFactionInfo(site.Owner).Icon;
                            __instance.TopBar.Icon.sprite = icon;
                            __instance.TopBar.Subtitle.text = site.LocalizedSiteName;
                            GeoFactionRewardApplyResult applyResult = geoInfestationCleanseMission.Reward.ApplyResult;
                            __instance.AttitudeChange.SetAttitudes(applyResult.Diplomacy);
                            //  __instance.Rewards.SetItems(InfestationRewardGenerator(1));
                            __instance.Rewards.SetReward(geoInfestationCleanseMission.Reward);

                            TFTVLogger.Always("InfestedHavensVariable before method is " + site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable));
                            site.GeoLevel.EventSystem.SetVariable(InfestedHavensVariable, site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable) - 1);
                            TFTVLogger.Always("InfestedHavensVariable is " + site.GeoLevel.EventSystem.GetVariable(InfestedHavensVariable));
                            //  site.GeoLevel.EventSystem.SetVariable(LivingWeaponsAcquired, site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) + 1);                       

                            if (havenToReceiveRefugees.Count > 0)
                            {
                                TFTVLogger.Always("There are havens that can receive refugees");

                                foreach (GeoHaven haven in havenToReceiveRefugees)
                                {
                                    int refugeesToHaven = populationSaved / havenToReceiveRefugees.Count() - UnityEngine.Random.Range(0, 50);
                                    if (refugeesToHaven > 0)
                                    {
                                        haven.Population += refugeesToHaven;

                                        string survivorsFrom = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SURVIVORS_FROM");
                                        string haveFledTo = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_FLED_TO");


                                        GeoscapeLogEntry entry = new GeoscapeLogEntry
                                        {   
                                            Text = new LocalizedTextBind($"{refugeesToHaven} {survivorsFrom} {site.LocalizedSiteName} {haveFledTo} {haven.Site.LocalizedSiteName}", true)
                                        };
                                        typeof(GeoscapeLog).GetMethod("AddEntry", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(site.GeoLevel.Log, new object[] { entry, null });

                                    }

                                }
                            }

                            TFTVInfestationStory.HavenPopulation = 0;
                            TFTVInfestationStory.OriginalOwner = "";
                            TFTVLogger.Always("LivingWeaponsAcquired variables is " + site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired));

                            /*
                             * KEY_GRAMMAR_PRONOUNS_SHE
                             * KEY_GRAMMAR_PRONOUNS_HER
                             * KEY_GRAMMAR_PRONOUNS_HE
                             * KEY_GRAMMAR_PRONOUNS_HIM
                             * KEY_GRAMMAR_PRONOUNS_THEY
                             * KEY_GRAMMAR_PRONOUNS_THEM
                             * KEY_GRAMMAR_PLURAL_SUFFIX
                             * KEY_GRAMMAR_SINGLE_SUFFIX
                             */

                            string pronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_THEY");//"they";
                            string nameMainCharacter = TFTVCommonMethods.ConvertKeyToString("KEY_TEXT_PHOENIX_OPERATIVES"); //"Phoenix operatives";
                            string plural = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_SINGLE_SUFFIX");
                            string possesivePronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_THEM"); ;
                            string havenName = site.LocalizedSiteName;

                            if (FindCharactersOnSite(site).Count() > 0)
                            {
                                if (FindCharactersOnSite(site).First().Identity.Sex == GeoCharacterSex.Female)
                                {
                                    pronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_SHE");
                                    possesivePronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_HER");
                                }
                                else
                                {
                                    pronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_HE");
                                    possesivePronoun = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PRONOUNS_HIM");
                                }
                                nameMainCharacter = FindCharactersOnSite(site).First().DisplayName;
                                plural = TFTVCommonMethods.ConvertKeyToString("KEY_GRAMMAR_PLURAL_SUFFIX");
                            }


                            GeoscapeEventDef LW1Miss = DefCache.GetDef<GeoscapeEventDef>("PROG_LW1_WIN_GeoscapeEventDef");
                            GeoscapeEventDef LW2Miss = DefCache.GetDef<GeoscapeEventDef>("PROG_LW2_WIN_GeoscapeEventDef");
                            GeoscapeEventDef LW3Miss = DefCache.GetDef<GeoscapeEventDef>("PROG_LW3_WIN_GeoscapeEventDef");

                            string firstHavenDescription0 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_FIRST_HAVEN0");
                            string firstHavenDescription1 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_FIRST_HAVEN1");
                            string firstHavenDescription2 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_FIRST_HAVEN2");
                            

                            LocalizedTextBind lWDescription1 = new LocalizedTextBind($"{firstHavenDescription0} {havenName} {firstHavenDescription1} {nameMainCharacter} \n {firstHavenDescription2}", true);

                            string secondHavenDescription0 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SECOND_HAVEN0");
                            string secondHavenDescription1 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SECOND_HAVEN1");
                            string secondHavenDescription2 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SECOND_HAVEN2");
                            string secondHavenDescription3 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SECOND_HAVEN3");
                            string secondHavenDescription4 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_SECOND_HAVEN4");

                            LocalizedTextBind lWDescription2 = new LocalizedTextBind($"{secondHavenDescription0} {nameMainCharacter} {secondHavenDescription1}{plural} {secondHavenDescription2} {pronoun} {secondHavenDescription3}{plural} {secondHavenDescription4}", true);

                            string thirdHavenDescription0 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN0");
                            string thirdHavenDescription1 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN1");
                            string thirdHavenDescription2 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN2");
                            string thirdHavenDescription3 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN3");
                            string thirdHavenDescription4 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN4");
                            string thirdHavenDescription5 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN5");
                            string thirdHavenDescription6 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN6");
                            string thirdHavenDescription7 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN7");
                            string thirdHavenDescription8 = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_THIRD_HAVEN8");
                           
                            LocalizedTextBind lWDescription3text = new LocalizedTextBind($"{thirdHavenDescription0} {havenName} {thirdHavenDescription1} {nameMainCharacter} {thirdHavenDescription2}{plural} {thirdHavenDescription3}{plural} {thirdHavenDescription4}", true);

                            LocalizedTextBind lWDescription3outcome = new LocalizedTextBind($"{thirdHavenDescription5} {nameMainCharacter}{thirdHavenDescription6} {possesivePronoun} {thirdHavenDescription7} {pronoun} {thirdHavenDescription8}", true);


                            if (site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) == 0)
                            {
                                LW1Miss.GeoscapeEventData.Description[0].General = lWDescription1;
                                GeoscapeEventContext context = new GeoscapeEventContext(site, site.GeoLevel.PhoenixFaction);
                                site.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_LW1_WIN", context);
                                site.GeoLevel.EventSystem.SetVariable(LivingWeaponsAcquired, 1);
                            }
                            else if (site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) == 1)
                            {
                                LW2Miss.GeoscapeEventData.Description[0].General = lWDescription2;
                                GeoscapeEventContext context = new GeoscapeEventContext(site, site.GeoLevel.PhoenixFaction);
                                site.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_LW2_WIN", context);
                                site.GeoLevel.EventSystem.SetVariable(LivingWeaponsAcquired, 2);

                            }
                            else if (site.GeoLevel.EventSystem.GetVariable(LivingWeaponsAcquired) == 2)
                            {
                                LW3Miss.GeoscapeEventData.Description[0].General = lWDescription3text;
                                LW3Miss.GeoscapeEventData.Choices[0].Outcome.OutcomeText.General = lWDescription3outcome;
                                GeoscapeEventContext context = new GeoscapeEventContext(site, site.GeoLevel.PhoenixFaction);
                                site.GeoLevel.EventSystem.TriggerGeoscapeEvent("PROG_LW3_WIN", context);
                                site.GeoLevel.EventSystem.SetVariable(LivingWeaponsAcquired, 3);
                            }
                        }
                        InfestationMissionWon = false;


                        return false;
                    }
                    else
                    {
                      

                        Text description = __instance.GetComponentInChildren<DescriptionController>().Description;
                        description.GetComponent<I2.Loc.Localize>().enabled = false;
                        description.text = TFTVCommonMethods.ConvertKeyToString("KEY_INFESTATION_MISSION_FAILED_TEXT"); //"We failed to destroy the monstrosity that has taken over the inhabitants of the haven.";
                        Text title = __instance.TopBar.Title;
                        title.GetComponent<I2.Loc.Localize>().enabled = false;
                        title.text = TFTVCommonMethods.ConvertKeyToString("KEY_MISSION_SUMMARY_FAILED");


                        __instance.Background.sprite = Helper.CreateSpriteFromImageFile("Node.jpg");



                        return true;

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }

            }
        }

        public static List<GeoCharacter> FindCharactersOnSite(GeoSite site)
        {
            try
            {

                List<GeoCharacter> eligibleCharacters = new List<GeoCharacter>();
                List<GeoCharacter> orderedOperatives = new List<GeoCharacter>();

                //  TFTVLogger.Always("There are " + site.GetPlayerVehiclesOnSite().Count() + " player vehicles on site");

                foreach (GeoVehicle geoVehicle in site.GetPlayerVehiclesOnSite())
                {
                    foreach (GeoCharacter geoCharacter in geoVehicle.Soldiers)
                    {
                        //  TFTVLogger.Always("There are " + geoVehicle.Soldiers.Count() + " soldiers in vehicle " + geoVehicle.Name);


                        if (geoCharacter.TemplateDef.IsHuman)
                        {

                            eligibleCharacters.Add(geoCharacter);

                            // TFTVLogger.Always("Character " + geoCharacter.DisplayName + " added to list");
                        }

                    }


                }


                if (eligibleCharacters.Count > 0)
                {
                    orderedOperatives = eligibleCharacters.OrderByDescending(e => e.LevelProgression.Experience).ToList();

                }

                // TFTVLogger.Always("ordeded operatives counts " + orderedOperatives.Count);

                return orderedOperatives;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }
            throw new InvalidOperationException();
        }
    }


}

