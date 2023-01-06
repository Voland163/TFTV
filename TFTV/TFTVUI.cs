using Base;
using Base.Core;
using Base.Entities.Statuses;
using Base.Levels;
using Base.UI.VideoPlayback;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Home.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.UI;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVUI
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //This method changes how WP are displayed in the Edit personnel screen, to show effects of Delirium on WP
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;

        public static UIModuleCharacterProgression hookToProgressionModule = null;
        public static GeoCharacter hookToCharacter = null;
        internal static bool moduleInfoBarAdjustmentsExecuted = false;
       // public static bool showFaceNotHelmet = true;


        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color blue = new Color32(62, 12, 224, 255);
        internal static Color green = new Color32(12, 224, 30, 255);
        internal static Color anu = new Color(0.9490196f, 0.0f, 1.0f, 1.0f);
        internal static Color nj = new Color(0.156862751f, 0.6156863f, 1.0f, 1.0f);
        internal static Color syn = new Color(0.160784319f, 0.8862745f, 0.145098045f, 1.0f);

       
        [HarmonyPatch(typeof(Utils), "GetDamageKeywordValue")]
        public static class Utils_GetDamageKeywordValue_Patch
        {
            public static void Postfix(DamagePayload payload, DamageKeywordDef damageKeyword, TacticalActor tacticalActor, ref float __result)
            {
                try
                {
                    // TFTVLogger.Always("GetDamageKeywordValue invoked");
                    StandardDamageTypeEffectDef projectileDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Projectile_StandardDamageTypeEffectDef");
                    StandardDamageTypeEffectDef blastDamage = DefCache.GetDef<StandardDamageTypeEffectDef>("Blast_StandardDamageTypeEffectDef");

                    if (tacticalActor != null && (damageKeyword.DamageTypeDef == projectileDamage || damageKeyword.DamageTypeDef == blastDamage)) //&& damageKeyword is PiercingDamageKeywordDataDef == false) 
                    {

                        float numberOfMutations = 0;

                     //   TFTVLogger.Always("GetDamageKeywordValue check passed");

                        foreach (TacticalItem armourItem in tacticalActor.BodyState.GetArmourItems())
                        {
                            if (armourItem.GameTags.Contains(mutationTag))
                            {
                                numberOfMutations++;
                            }
                        }

                        if (numberOfMutations > 0)
                        {
                           // TFTVLogger.Always("damage value is " + payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage));

                           __result = payload.GenerateDamageValue(tacticalActor.CharacterStats.BonusAttackDamage) * (1f + (numberOfMutations * 2) / 100 * (float)tacticalActor.CharacterStats.Corruption);
                          //  TFTVLogger.Always("result is " + __result +", damage increase is " + (1f + (((numberOfMutations * 2) / 100) * (float)tacticalActor.CharacterStats.Corruption)));
                        }

                    }       
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        //Adapted from Mad´s Assorted Adjustments

        [HarmonyPatch(typeof(UIModuleInfoBar), "Init")]
        public static class UIModuleInfoBar_Init_Patch
        {
            public static void Prefix(UIModuleInfoBar __instance, GeoscapeViewContext ____context)
            {
                try
                {
                    if (moduleInfoBarAdjustmentsExecuted)
                    {
                        return;
                    }

                    Resolution resolution = Screen.currentResolution;

                    // TFTVLogger.Always("Resolution is " + Screen.currentResolution.width);
                    float resolutionFactorWidth = (float)resolution.width / 1920f;
                    //   TFTVLogger.Always("ResolutionFactorWidth is " + resolutionFactorWidth);
                    float resolutionFactorHeight = (float)resolution.height / 1080f;
                    //   TFTVLogger.Always("ResolutionFactorHeight is " + resolutionFactorHeight);

                    // Declutter
                    Transform tInfoBar = __instance.PopulationBarRoot.transform.parent?.transform;

                    //Use this to catch the ToolTip
                    Transform[] thingsToUse = new Transform[2];

                    __instance.PopulationTooltip.enabled = false;

                    foreach (Transform t in tInfoBar.GetComponentsInChildren<Transform>())
                    {
                    
                        if (t.name == "TooltipCatcher")
                        {
                            if (t.GetComponent<UITooltipText>().TipKey.LocalizeEnglish() == "Stores - used space / capacity of all stores facilities")
                            {
                                thingsToUse[0] = t;
                            }
                        }

                        // Hide useless icons at production and research
                        if (t.name == "UI_Clock")
                        {
                            t.gameObject.SetActive(false);
                        }
                        //Add Delirium and Pandoran evolution icons, as well as factions icons.
                        if (t.name == "Requirement_Icon")
                        {
                            Image icon = t.gameObject.GetComponent<Image>();
                            if (icon.sprite.name == "Geoscape_UICanvasIcons_Actions_EditSquad")
                            {
                                icon.sprite = Helper.CreateSpriteFromImageFile("Void-04P.png");
                                t.gameObject.name = "DeliriumIcon";
                                t.parent = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");
                                t.Translate(new Vector3(30f * resolutionFactorWidth, 0f, 0f));
                                t.localScale = new Vector3(1.3f, 1.3f, 1f);
                                t.gameObject.SetActive(false);
                                //  icon.color = purple;

                                //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}");

                                Transform pandoranEvolution = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                pandoranEvolution.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                                pandoranEvolution.gameObject.GetComponent<Image>().color = red;
                                pandoranEvolution.gameObject.name = "PandoranEvolutionIcon";
                                // pandoranEvolution.localScale = new Vector3(0.9f, 0.9f, 1);
                                pandoranEvolution.Translate(new Vector3(110f * resolutionFactorWidth, 0f, 0f));
                                // pandoranEvolution.Translate(80f*resolutionFactor, 0f, 0f, t);
                                pandoranEvolution.gameObject.SetActive(false);


                                Transform anuDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                anuDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                anuDiploInfoIcon.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().color = anu;
                                anuDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Anu.png");
                                anuDiploInfoIcon.gameObject.name = "AnuIcon";
                                anuDiploInfoIcon.gameObject.SetActive(false);

                                Transform njDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                njDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                njDiploInfoIcon.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                                njDiploInfoIcon.gameObject.GetComponent<Image>().color = nj;
                                njDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_NewJericho.png");
                                njDiploInfoIcon.gameObject.name = "NJIcon";
                                njDiploInfoIcon.gameObject.SetActive(false);

                                Transform synDiploInfoIcon = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                                synDiploInfoIcon.localScale = new Vector3(1f, 1f, 1f);
                                synDiploInfoIcon.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                                synDiploInfoIcon.gameObject.GetComponent<Image>().color = syn;
                                synDiploInfoIcon.gameObject.GetComponent<Image>().sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Synedrion.png");
                                synDiploInfoIcon.gameObject.name = "SynIcon";
                                synDiploInfoIcon.gameObject.SetActive(false);
                                //  anuDiploInfo.gameObject.GetComponent<Image>().color = red;

                            }

                            // t.name = "ODI_icon";
                            // TFTVLogger.Always("Req_Icon name is " + icon.sprite.name);
                        }

                        if (t.name == "UI_underlight")
                        {
                            if (t.parent.name == "StoresRes")
                            {
                                thingsToUse[1] = t;
                            }


                            // TFTVLogger.Always("Parent of UI_underlight " + t.parent.name);


                            // separator.position = anuDiploInfoIcon.position - new Vector3(-100, 0, 0);
                        }

                        //Create separators to hold Delirium and Pandoran Evolution icons
                        if (t.name == "Separator")
                        {
                            Transform separator = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator.Translate(new Vector3(0f, 12f * resolutionFactorHeight, 0f));
                            separator.gameObject.name = "ODISeparator1";
                            separator.gameObject.SetActive(false);
                            // separator.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                            Transform separator2 = UnityEngine.Object.Instantiate(t, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                            separator2.Translate(new Vector3(180f * resolutionFactorWidth, 12f * resolutionFactorHeight, 0f));
                            separator2.gameObject.name = "ODISeparator2";
                            separator2.gameObject.SetActive(false);
                            //  separator2.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                        }
                        // Remove skull icon
                        if (t.name == "skull")
                        {
                            t.gameObject.SetActive(false);
                        }

                        // Removed tiled gameover bar
                        if (t.name == "tiled_gameover")
                        {

                            t.gameObject.SetActive(false);
                        }

                        //Remove other bits and pieces of doomsday clock
                        if (t.name == "alive_mask" || t.name == "alive_animation" ||
                            t.name.Contains("alive_animated") || t.name == "dead" || t.name.Contains("death"))
                        {

                            t.gameObject.SetActive(false);
                        }

                        //    TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " root position " + "x: " + t.root.position.x);
                        //   TFTVLogger.Always($"[UIModuleInfoBar_Init_PREFIX] Transform.name: {t.name}" + " right " + "x: " + t.right.x);

                    }



                    Transform deliriumTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing Delirium tooltip";
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    deliriumTooltip.gameObject.name = "DeliriumTooltip";
                    deliriumTooltip.gameObject.SetActive(false);
                    //TFTVLogger.Always("Got here");

                    Transform evolutionTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = "testing Pandoran Evolution tooltip";
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    evolutionTooltip.gameObject.name = "PandoranEvolutionTooltip";
                    evolutionTooltip.gameObject.SetActive(false);

                    //Create percentages next to each faction icon

                    Transform anuDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    anuDiploInfo.Translate(new Vector3(210f * resolutionFactorWidth, 0f, 0f));
                    anuDiploInfo.gameObject.name = "AnuPercentage";
                    anuDiploInfo.gameObject.SetActive(false);
                    // anuDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    // anuDiploInfo.gameObject.SetActive(false);

                    Transform njDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    njDiploInfo.Translate(new Vector3(320f * resolutionFactorWidth, 0f, 0f));
                    njDiploInfo.gameObject.name = "NjPercentage";
                    njDiploInfo.gameObject.SetActive(false);
                    njDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploInfo = UnityEngine.Object.Instantiate(__instance.PopulationPercentageText.transform, tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    synDiploInfo.Translate(new Vector3(430f * resolutionFactorWidth, 0f, 0f));
                    synDiploInfo.gameObject.name = "SynPercentage";
                    synDiploInfo.gameObject.SetActive(false);
                    //   synDiploInfo.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));
                    //Create highlights for new elements

                    Transform deliriumIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("DeliriumIcon"));
                    deliriumIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    deliriumIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform PandoranEvolutionIconHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    PandoranEvolutionIconHL.localScale = new Vector3(0.6f, 0.6f, 0f);
                    PandoranEvolutionIconHL.Translate(new Vector3(0f, -20f * resolutionFactorHeight, 1));


                    Transform anuDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuPercentage"));
                    anuDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));


                    Transform njDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NjPercentage"));
                    njDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // njDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    Transform synDiploHL = UnityEngine.Object.Instantiate(thingsToUse[1], tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynPercentage"));
                    synDiploHL.Translate(new Vector3(-10 * resolutionFactorWidth, -15 * resolutionFactorHeight, 1));
                    // synDiploHL.SetParent(tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter"));

                    __instance.PopulationPercentageText.gameObject.SetActive(false);

                    // Set a flag so that this whole stuff is only done ONCE
                    // Otherwise the visual transformations are repeated everytime leading to weird results
                    // This is reset on every level change (see below)
                    moduleInfoBarAdjustmentsExecuted = true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
        public static class PhoenixGame_RunGameLevel_Patch
        {
            public static void Prefix()
            {
                moduleInfoBarAdjustmentsExecuted = false;
            }
        }


        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdatePopulation")]
        public static class TFTV_ODI_meter_patch
        {
            public static void Postfix(UIModuleInfoBar __instance, GeoscapeViewContext ____context, LayoutGroup ____layoutGroup)
            {

                try
                {
                    //  TFTVLogger.Always("Running UpdatePopulation");

                    GeoLevelController controller = ____context.Level;

                    List<GeoAlienBase> listOfAlienBases = controller.AlienFaction.Bases.ToList();

                    int nests = 0;
                    int lairs = 0;
                    int citadels = 0;


                    foreach (GeoAlienBase alienBase in listOfAlienBases)
                    {
                        if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Nest_GeoAlienBaseTypeDef")))
                        {
                            nests++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Lair_GeoAlienBaseTypeDef")))
                        {
                            lairs++;
                        }
                        else if (alienBase.AlienBaseTypeDef.Equals(DefCache.GetDef<GeoAlienBaseTypeDef>("Citadel_GeoAlienBaseTypeDef")))
                        {
                            citadels++;
                        }

                    }


                    int pEPerDay = nests + lairs * 2 + citadels * 3 + controller.EventSystem.GetVariable(TFTVInfestation.InfestedHavensVariable) * 2;
                    //max, not counting IH, is 3 + 6 + 9 = 18
                    //>=66%, evo high, so 12+
                    //<66% >33%, evo normal, 6+ 
                    //<33%, evo slow, else


                    Transform tInfoBar = __instance.PopulationBarRoot.transform.parent?.transform;
                    Transform populationBar = tInfoBar.GetComponent<Transform>().Find("PopulationDoom_Meter");

                    //     TFTVLogger.Always("Got here");


                    Transform anuInfo = populationBar.GetComponent<Transform>().Find("AnuPercentage");
                    anuInfo.gameObject.GetComponent<Text>().text = $"<color=#f200ff>{____context.Level.AnuFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";


                    Transform njInfo = populationBar.GetComponent<Transform>().Find("NjPercentage");
                    njInfo.gameObject.GetComponent<Text>().text = $"<color=#289eff>{____context.Level.NewJerichoFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";


                    Transform synInfo = populationBar.GetComponent<Transform>().Find("SynPercentage");
                    synInfo.gameObject.GetComponent<Text>().text = $"<color=#28e225>{____context.Level.SynedrionFaction.Diplomacy.GetDiplomacy(____context.Level.PhoenixFaction)}%</color>";

                    Transform anuIcon = populationBar.GetComponent<Transform>().Find("AnuIcon");
                    Transform njIcon = populationBar.GetComponent<Transform>().Find("NJIcon");
                    Transform synIcon = populationBar.GetComponent<Transform>().Find("SynIcon");

                    //   TFTVLogger.Always("Got here 2");

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("AN_Discovered_DiplomacyStateTagDef")))
                    {
                        anuInfo.gameObject.SetActive(true);
                        anuIcon.gameObject.SetActive(true);
                    }

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("NJ_Discovered_DiplomacyStateTagDef")))
                    {
                        njInfo.gameObject.SetActive(true);
                        njIcon.gameObject.SetActive(true);

                    }
                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("SY_Discovered_DiplomacyStateTagDef")))
                    {
                        synInfo.gameObject.SetActive(true);
                        synIcon.gameObject.SetActive(true);

                    }

                    //   TFTVLogger.Always("Got here 3");
                    Transform deliriumIconHolder = populationBar.GetComponent<Transform>().Find("DeliriumIcon");
                    Image deliriumIcon = deliriumIconHolder.GetComponent<Image>();
                    Transform separator = populationBar.GetComponent<Transform>().Find("ODISeparator1");

                    Transform separator2 = populationBar.GetComponent<Transform>().Find("ODISeparator2");

                    //    TFTVLogger.Always("Got here 4");

                    string deliriumToolTipText = "";
                    if (controller.EventSystem.GetEventRecord("SDI_10")?.SelectedChoice == 0 || TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                    {
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        populationBar.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumIcon.sprite = TFTVDefsRequiringReinjection.VoidIcon;
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status equal to their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }
                    else if (controller.EventSystem.GetEventRecord("SDI_06")?.SelectedChoice == 0)
                    {
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Phalf.png");
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status of up to half of their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }
                    else if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0)
                    {
                        // TFTVLogger.Always("Got to SDI01");
                        deliriumIcon.sprite = Helper.CreateSpriteFromImageFile("Void-04Pthird.png");
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        deliriumIconHolder.gameObject.SetActive(true);
                        deliriumToolTipText = "<color=#ec9006><b>-Our operatives can now be afflicted with a Delirium status of up to a third of their Willpower</b></color>";
                        separator.gameObject.SetActive(true);
                        separator2.gameObject.SetActive(true);
                    }

                    if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(10))
                    {
                        deliriumToolTipText += "\n-<i>No limit to Delirium, regardless of ODI level</i> Void Omen is in effect.";
                    }


                    if (controller.EventSystem.GetEventRecord("SDI_09")?.SelectedChoice == 0)
                    {
                        deliriumToolTipText += "\n-Evolved Umbra sighted.";
                    }
                    else if (controller.EventSystem.GetVariable("UmbraResearched") == 1)
                    {
                        deliriumToolTipText += "\n-Sightings of Umbra have been reported";
                    }
                    if (controller.EventSystem.GetEventRecord("SDI_07")?.SelectedChoice == 0)
                    {
                        deliriumToolTipText += "\n-Havens in the Mist can become infested instead of destroyed when attacked by Pandorans. Infested havens accelerate Pandoran evolution.";
                    }


                    Transform deliriumTooltip = populationBar.GetComponent<Transform>().Find("DeliriumIcon").GetComponent<Transform>().Find("DeliriumTooltip");
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = deliriumToolTipText;
                    deliriumTooltip.gameObject.SetActive(true);
                    //TFTVLogger.Always("Got here");




                    if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0 && controller.EventSystem.GetEventRecord("PROG_FS2_WIN")?.SelectedChoice == 0)
                    {
                        deliriumIconHolder.gameObject.SetActive(false);
                    }

                    Transform evolutionIconHolder = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon");
                    Image evolutionIcon = evolutionIconHolder.GetComponent<Image>();

                    Transform evolutionTooltip = populationBar.GetComponent<Transform>().Find("PandoranEvolutionIcon").GetComponent<Transform>().Find("PandoranEvolutionTooltip");
                    string evolutionToolTipText = "Based on reports and field observations, we estimate that the Pandorans are evolving ";
                    if (controller.PhoenixFaction.Research.HasCompleted("PX_Alien_EvolvedAliens_ResearchDef"))
                    {
                        // TFTVLogger.Always("Got here 5");
                        evolutionIconHolder.gameObject.SetActive(true);
                        populationBar.gameObject.SetActive(true);
                        __instance.PopulationBarRoot.gameObject.SetActive(true);
                        evolutionTooltip.gameObject.SetActive(true);

                        if (pEPerDay >= 12)
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_fast.png");
                            evolutionToolTipText += "<b>very rapidly</b>. We must destroy Pandoran Colonies and Infested Havens before we are overwhelmed!";
                        }
                        else if (pEPerDay >= 6)
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_medium.png");
                            evolutionToolTipText += "<b>rapidly</b>. We must keep the number of Pandoran Colonies and Infested Havens in check.";
                        }
                        else
                        {
                            evolutionIcon.sprite = Helper.CreateSpriteFromImageFile("FactionIcons_Aliens_Evo_slow.png");
                            evolutionToolTipText += ". We are monitoring the situation and will report any newly discovered Pandoran Colonies.";
                        }
                    }


                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = evolutionToolTipText;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(UIModuleCharacterProgression), "GetStarBarValuesDisplayString")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleCharacterProgression_RefreshStatPanel_patch
        {
            private static void Postfix(GeoCharacter ____character, ref string __result, CharacterBaseAttribute attribute, int currentAttributeValue)
            {
                try
                {
                    ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
                    float bonusSpeed = 0;
                    float bonusWillpower = 0;
                    float bonusStrength = 0;

                    //  GeoLevelController level = (GeoLevelController)UnityEngine.Object.FindObjectOfType(typeof(GeoLevelController));

                    foreach (ICommonItem armorItem in ____character.ArmourItems)
                    {
                        TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                        if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                        {
                            bonusSpeed += tacticalItemDef.BodyPartAspectDef.Speed;
                            bonusWillpower += tacticalItemDef.BodyPartAspectDef.WillPower;
                            bonusStrength += tacticalItemDef.BodyPartAspectDef.Endurance;
                        }
                    }

                    if (____character.Progression != null)
                    {
                        foreach (TacticalAbilityDef ability in ____character.Progression.Abilities)
                        {
                            PassiveModifierAbilityDef passiveModifierAbilityDef = ability as PassiveModifierAbilityDef;
                            if (!(passiveModifierAbilityDef == null))
                            {
                                ItemStatModification[] statModifications = passiveModifierAbilityDef.StatModifications;
                                foreach (ItemStatModification statModifier in statModifications)
                                {
                                    if (statModifier.TargetStat == StatModificationTarget.Endurance && statModifier.Modification == StatModificationType.AddMax)
                                    {
                                        bonusStrength += statModifier.Value;
                                    }
                                    else if (statModifier.TargetStat == StatModificationTarget.Willpower && statModifier.Modification == StatModificationType.AddMax)
                                    {
                                        bonusWillpower += statModifier.Value;
                                    }
                                    else if (statModifier.TargetStat == StatModificationTarget.Speed)
                                    {
                                        bonusSpeed += statModifier.Value;
                                    }

                                }
                            }


                            if (ability == derealization)
                            {
                                bonusStrength -= 5;
                            }

                        }


                        foreach (PassiveModifierAbilityDef passiveModifier in ____character.PassiveModifiers)
                        {
                            ItemStatModification[] statModifications = passiveModifier.StatModifications;
                            foreach (ItemStatModification statModifier2 in statModifications)
                            {
                                if (statModifier2.TargetStat == StatModificationTarget.Endurance)
                                {
                                    bonusStrength += statModifier2.Value;
                                }
                                else if (statModifier2.TargetStat == StatModificationTarget.Willpower)
                                {
                                    bonusWillpower += statModifier2.Value;
                                }
                                else if (statModifier2.TargetStat == StatModificationTarget.Speed)
                                {
                                    bonusSpeed += statModifier2.Value;
                                }

                            }
                        }
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Strength))
                    {
                        if (bonusStrength > 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#50c878>{currentAttributeValue + bonusStrength}</color>)";
                        }
                        else if (bonusStrength < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#cc0000>{currentAttributeValue + bonusStrength}</color>)";
                        }
                        else
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
                        }

                    }


                    if (attribute.Equals(CharacterBaseAttribute.Speed))
                    {

                        if (bonusSpeed > 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#50c878>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else if (bonusSpeed < 0)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                    $" (<color=#cc0000>{currentAttributeValue + bonusSpeed}</color>)";
                        }
                        else
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
                        }
                    }

                    if (attribute.Equals(CharacterBaseAttribute.Will))
                    {
                        if (____character.CharacterStats.Corruption > TFTVDelirium.CalculateStaminaEffectOnDelirium(____character) && TFTVVoidOmens.VoidOmensCheck[3] == false)
                        {
                            __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(CharacterBaseAttribute.Will)}" +
                                $"<color=#da5be3> ({currentAttributeValue + bonusWillpower - ____character.CharacterStats.Corruption.Value + TFTVDelirium.CalculateStaminaEffectOnDelirium(____character)}</color>)";
                        }
                        else
                        {
                            if (bonusWillpower > 0)
                            {

                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                            $" (<color=#50c878>{currentAttributeValue + bonusWillpower}</color>)";

                            }
                            else if (bonusWillpower < 0)
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}" +
                                        $" (<color=#cc0000>{currentAttributeValue + bonusWillpower}</color>)";
                            }
                            else
                            {
                                __result = $"{currentAttributeValue} / {____character.Progression.GetMaxBaseStat(attribute)}";
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

        [HarmonyPatch(typeof(UIModuleCharacterProgression), "Awake")]

        internal static class UIModuleCharacterProgression_Awake_patch

        {
            public static void Postfix(UIModuleCharacterProgression __instance)
            {
                try
                {
                    hookToProgressionModule = __instance;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }

        [HarmonyPatch(typeof(UIModuleSoldierEquip), "RefreshWeightSlider")]
        internal static class UIModuleSoldierEquip_RefreshWeightSlider_Patch
        {
            private static readonly ApplyStatusAbilityDef derealization = DefCache.GetDef<ApplyStatusAbilityDef>("DerealizationIgnorePain_AbilityDef");
            private static void Prefix(ref int maxWeight, UIModuleSoldierEquip __instance)
            {
                try
                {
                    if (hookToCharacter != null && !__instance.IsVehicle && !hookToCharacter.TemplateDef.IsMutog)
                    {

                        float bonusStrength = 0;
                        float bonusToCarry = 1;

                        foreach (ICommonItem armorItem in hookToCharacter.ArmourItems)
                        {
                            TacticalItemDef tacticalItemDef = armorItem.ItemDef as TacticalItemDef;
                            if (!(tacticalItemDef == null) && !(tacticalItemDef.BodyPartAspectDef == null))
                            {
                                bonusStrength += tacticalItemDef.BodyPartAspectDef.Endurance;
                            }
                        }

                        if (hookToCharacter.Progression != null)
                        {
                            foreach (TacticalAbilityDef ability in hookToCharacter.Progression.Abilities)
                            {
                                PassiveModifierAbilityDef passiveModifierAbilityDef = ability as PassiveModifierAbilityDef;
                                if (!(passiveModifierAbilityDef == null))
                                {
                                    ItemStatModification[] statModifications = passiveModifierAbilityDef.StatModifications;
                                    foreach (ItemStatModification statModifier in statModifications)
                                    {
                                        if (statModifier.TargetStat == StatModificationTarget.Endurance && statModifier.Modification == StatModificationType.AddMax)
                                        {
                                            bonusStrength += statModifier.Value;
                                            // TFTVLogger.Always("The TacticalAbilityDef is " + ability.name + ". It modifies Endurance, giving " + statModifier.Value + ", " +
                                            //    "making the total bonus to Strength " + bonusStrength);
                                        }


                                        if (statModifier.TargetStat == StatModificationTarget.CarryWeight && statModifier.Modification == StatModificationType.MultiplyMax)
                                        {
                                            bonusToCarry += statModifier.Value - 1;
                                        }
                                    }
                                }

                                if (ability == derealization)
                                {
                                    bonusStrength -= 5;

                                }
                            }

                            foreach (PassiveModifierAbilityDef passiveModifier in hookToCharacter.PassiveModifiers)
                            {
                                ItemStatModification[] statModifications = passiveModifier.StatModifications;
                                foreach (ItemStatModification statModifier2 in statModifications)
                                {
                                    if (statModifier2.TargetStat == StatModificationTarget.Endurance)
                                    {
                                        bonusStrength += statModifier2.Value;
                                    }
                                    if (statModifier2.TargetStat == StatModificationTarget.CarryWeight)
                                    {
                                        bonusToCarry += statModifier2.Value;
                                    }

                                }
                            }

                        }

                        maxWeight += (int)(bonusStrength * bonusToCarry);
                        hookToProgressionModule.StatChanged();
                        //   hookToProgressionModule.RefreshStats();
                        //hookToProgressionModule.SetStatusesPanel();
                        hookToProgressionModule.RefreshStatPanel();
                        //TFTVLogger.Always("Max weight is " + maxWeight + ". Bonus Strength is " + bonusStrength + ". Bonus to carry is " + bonusToCarry);

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(UIStateEditSoldier), "RequestRefreshCharacterData")]
        internal static class UIStateEditSoldier_RequestRefreshCharacterData_Patch
        {

            private static void Postfix(ref bool ____uiCharacterAnimationResetNeeded)
            {
                try
                {

                    ____uiCharacterAnimationResetNeeded = false;

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(CharacterStats), "get_CorruptionProgressRel")]
        internal static class TFTV_UI_CharacterStats_DeliriumFace_patch
        {
            private static void Postfix(ref float __result, CharacterStats __instance)
            {
                try
                {

                    if (__instance.Corruption > 0 && hookToCharacter!=null)
                    {
                        GeoCharacter character = hookToCharacter;

                        if (__instance.Corruption - TFTVDelirium.CalculateStaminaEffectOnDelirium(character) > 0)
                        {

                            __result = ((character.CharacterStats.Corruption - (TFTVDelirium.CalculateStaminaEffectOnDelirium(character))) / 20);


                        }
                        else
                        {
                            __result = 0.05f;
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }

        /* [HarmonyPatch(typeof(CorruptionSettingsDef), "CalculateCorruptionShaderValue")]

         internal static class TFTV_UI_CorruptionSettingsDef_DeliriumFace_patch
         {
             private static void Prefix(float corruption01)
             {
                 try
                 {
                     if (hookToCharacter != null)
                     {
                         GeoCharacter geoCharacter = hookToCharacter;
                         if (geoCharacter.CharacterStats.Corruption > 0)
                         {
                             // corruption01 = ((geoCharacter.CharacterStats.Corruption-(geoCharacter.Fatigue.Stamina/10))/ geoCharacter.CharacterStats.WillPoints.IntMax)*0.25f;

                           //  TFTVLogger.Always("This character is " + geoCharacter.DisplayName + " has CorruptionProgressRel of " + geoCharacter.CharacterStats.CorruptionProgressRel
                           //      + " Delirium of " + geoCharacter.CharacterStats.Corruption + " and WP of " + geoCharacter.CharacterStats.WillPoints.IntMax + " and floatcorruption is " + corruption01);

                         }

                     }




                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }


         }*/



        [HarmonyPatch(typeof(UIModuleSoldierCustomization), "OnNewCharacter")]

        internal static class TFTV_UI_UIModuleSoldierCustomization_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

            private static void Postfix(GeoCharacter newCharacter)
            {
                try
                {

                    //  TFTVLogger.Always("Checking that OnNewCharacter is launched");
                    if (newCharacter != null && (newCharacter.TemplateDef.IsHuman || newCharacter.TemplateDef.IsMutoid))
                    {
                        //    TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is human or mutoid");

                        UIModuleSoldierCustomization uIModuleSoldierCustomizationLocal = (UIModuleSoldierCustomization)UnityEngine.Object.FindObjectOfType(typeof(UIModuleSoldierCustomization));
                        uIModuleSoldierCustomization = uIModuleSoldierCustomizationLocal;
                        uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;

                        if (newCharacter != null && (!newCharacter.TemplateDef.IsHuman || newCharacter.IsMutoid))
                        {

                            uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                            uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                            //  TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is mutoid");

                        }
                        else if (newCharacter != null && newCharacter.TemplateDef.IsHuman)
                        {
                            // TFTVLogger.Always("character is " + newCharacter.DisplayName + " and is human");
                            bool hasAugmentedHead = false;
                            foreach (GeoItem bionic in (newCharacter.ArmourItems))
                            {
                                if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                                && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                                {
                                    hasAugmentedHead = true;
                                }
                            }

                            if (hasAugmentedHead)
                            {
                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                //   TFTVLogger.Always("character is " + newCharacter.DisplayName + " and has augmented head");
                            }
                            else
                            {
                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;
                                //   TFTVLogger.Always("character is " + newCharacter.DisplayName + " and does not have an augmented head");
                            }
                        }
                        /* else
                         {
                             uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;
                         }*/

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }


        }



        public static UIModuleSoldierCustomization uIModuleSoldierCustomization = null;


        [HarmonyPatch(typeof(UIStateSoldierCustomization), "EnterState")]
        internal static class TFTV_UIStateSoldierCustomization_DisplaySoldier_HelmetToggle_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");


            public static void Postfix()
            {
                try
                {
                    //  TFTVLogger.Always("Trying to set helmets off if character has mutated head");
                    if (hookToCharacter != null && (hookToCharacter.TemplateDef.IsHuman || hookToCharacter.TemplateDef.IsMutoid))
                    {
                        //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human or mutoid");
                        if (hookToCharacter != null && (!hookToCharacter.TemplateDef.IsHuman || hookToCharacter.IsMutoid))
                        {
                            //     TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is mutoid");
                            uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                            uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;

                        }
                        else if (hookToCharacter != null && hookToCharacter.TemplateDef.IsHuman)
                        {
                            //    TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and is human");
                            bool hasAugmentedHead = false;
                            foreach (GeoItem bionic in (hookToCharacter.ArmourItems))
                            {
                                if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                                && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                                {
                                    hasAugmentedHead = true;
                                }
                            }

                            if (hasAugmentedHead)
                            {
                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = false;
                                uIModuleSoldierCustomization.HideHelmetToggle.isOn = false;
                                //   TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and has augmented head");
                            }
                            else
                            {
                                uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;

                                //    TFTVLogger.Always("character is " + hookToCharacter.DisplayName + " and does not have an augmented head");
                            }
                        }
                        /* else
                         {
                             uIModuleSoldierCustomization.HideHelmetToggle.interactable = true;
                         }*/

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

        }


        [HarmonyPatch(typeof(UIModuleActorCycle), "DisplaySoldier", new Type[] { typeof(GeoCharacter), typeof(bool), typeof(bool), typeof(bool) })]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleActorCycle_DisplaySoldier_patch
        {
            private static readonly GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;
            private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
            private static readonly ItemSlotDef headSlot = DefCache.GetDef<ItemSlotDef>("Human_Head_SlotDef");

            /*  public static bool Prepare()
              {
                  TFTVConfig config = TFTVMain.Main.Config;
                  return config.helmentsOff;
              }*/

            private static bool Prefix(UIModuleActorCycle __instance, List<UnitDisplayData> ____units,
                CharacterClassWorldDisplay ____classWorldDisplay,
                GeoCharacter character, bool showHelmet, bool resetAnimation, bool addWeapon)
            {
                try
                {
                    if (character.TemplateDef.IsMutog || character.TemplateDef.IsMutoid || character.TemplateDef.IsVehicle)
                    {
                        return true;
                    }


                    if (character != null && character.TemplateDef.IsHuman && !character.IsMutoid && !character.TemplateDef.IsMutog && !character.TemplateDef.IsVehicle)
                    {

                        bool hasAugmentedHead = false;

                        foreach (GeoItem bionic in character.ArmourItems)
                        {

                            if ((bionic.CommonItemData.ItemDef.Tags.Contains(bionicalTag) || bionic.CommonItemData.ItemDef.Tags.Contains(mutationTag))
                                && bionic.CommonItemData.ItemDef.RequiredSlotBinds[0].RequiredSlot == headSlot)
                            {
                                hasAugmentedHead = true;

                            }
                        }

                        if (!hasAugmentedHead)
                        {
                            UnitDisplayData unitDisplayData = ____units.FirstOrDefault((UnitDisplayData u) => u.BaseObject == character);
                            if (unitDisplayData == null)
                            {
                                return true;
                            }


                            ____classWorldDisplay.SetDisplay(character.GetClassViewElementDefs(), (float)character.CharacterStats.Corruption > 0f);


                            if (uIModuleSoldierCustomization == null || uIModuleSoldierCustomization.HideHelmetToggle.isOn)
                            {
                                __instance.DisplaySoldier(unitDisplayData, resetAnimation, addWeapon, showHelmet = false);
                                return false;
                            }
                            else
                            {
                                return true;

                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
                return true;
            }
        }



        //This changes display of Delirium bar in personnel edit screen to show current Delirium value vs max delirium value the character can have
        // taking into account ODI level and bionics
        [HarmonyPatch(typeof(UIModuleCharacterProgression), "SetStatusesPanel")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        internal static class BG_UIModuleCharacterProgression_SetStatusesPanel_patch
        {

            private static void Postfix(UIModuleCharacterProgression __instance, GeoCharacter ____character)
            {
                try
                {
                    hookToCharacter = ____character;

                    if (____character.CharacterStats.Corruption > 0f)

                    {
                        float delirium = ____character.CharacterStats.Corruption.IntValue;
                        if (TFTVDelirium.CalculateMaxCorruption(____character) < ____character.CharacterStats.Corruption.IntValue)
                        {
                            delirium = (TFTVDelirium.CalculateMaxCorruption(____character));
                        }

                        __instance.CorruptionSlider.minValue = 0f;
                        __instance.CorruptionSlider.maxValue = TFTVDelirium.CalculateMaxCorruption(____character);
                        __instance.CorruptionSlider.value = delirium;
                        __instance.CorruptionStatText.text = $"{delirium}/{Mathf.RoundToInt(__instance.CorruptionSlider.maxValue)}";

                        int num = (int)(float)____character.Fatigue.Stamina;
                        int num2 = (int)(float)____character.Fatigue.Stamina.Max;
                        __instance.StaminaSlider.minValue = 0f;
                        __instance.StaminaSlider.maxValue = num2;
                        __instance.StaminaSlider.value = num;
                        if (num != num2)
                        {
                            string deliriumReducedStamina = "";
                            for (int i = 0; i < TFTVDelirium.CalculateStaminaEffectOnDelirium(____character); i++)
                            {
                                deliriumReducedStamina += "-";

                            }
                            __instance.StaminaStatText.text = $"<color=#da5be3>{deliriumReducedStamina}</color>" + num + "/" + num2;
                        }
                        else
                        {
                            __instance.StaminaStatText.text = "<color=#da5be3> ---- </color>" + num.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //Adapted from Mad's Assorted Adjustments, all hail the Great Mad!
        [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
        public static class TFTV_PhoenixGame_RunGameLevel_SkipLogos_Patch
        {
            public static bool Prefix(PhoenixGame __instance, LevelSceneBinding levelSceneBinding, ref IEnumerator<NextUpdate> __result)
            {
                TFTVConfig config = TFTVMain.Main.Config;

                try
                {
                    if (config.SkipMovies)
                    {

                        if (levelSceneBinding == __instance.Def.IntroLevelSceneDef.Binding)
                        {
                            __result = Enumerable.Empty<NextUpdate>().GetEnumerator();
                            return false;
                        }

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

        [HarmonyPatch(typeof(UIStateHomeScreenCutscene), "EnterState")]
        public static class TFTV_PhoenixGame_RunGameLevel_SkipIntro_Patch
        {
            public static void Postfix(UIStateHomeScreenCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
            {
                TFTVConfig config = TFTVMain.Main.Config;
                if (config.SkipMovies)
                {
                    try
                    {
                        if (____sourcePlaybackDef == null)
                        {
                            return;
                        }

                        if (____sourcePlaybackDef.ResourcePath.Contains("Game_Intro_Cutscene"))
                        {
                            typeof(UIStateHomeScreenCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UIStateTacticalCutscene), "EnterState")]
        public static class TFTV_PhoenixGame_RunGameLevel_SkipLanding_Patch
        {
            public static void Postfix(UIStateTacticalCutscene __instance, VideoPlaybackSourceDef ____sourcePlaybackDef)
            {
                TFTVConfig config = TFTVMain.Main.Config;
                if (config.SkipMovies)
                {
                    try
                    {
                        if (____sourcePlaybackDef == null)
                        {
                            return;
                        }
                        if (____sourcePlaybackDef.ResourcePath.Contains("LandingSequences"))
                        {
                            typeof(UIStateTacticalCutscene).GetMethod("OnCancel", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(__instance, null);
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
}
