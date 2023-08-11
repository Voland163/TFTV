using Base.Core;
using Base.Defs;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Geoscape.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVUIGeoMap
    {
        // private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        //This method changes how WP are displayed in the Edit personnel screen, to show effects of Delirium on WP
        private static readonly GameTagDef mutationTag = GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        //  public static Dictionary<int, List<string>> CurrentlyHiddenInv = new Dictionary<int, List<string>>();
        //  public static Dictionary<int, List<string>> CurrentlyAvailableInv = new Dictionary<int, List<string>>();



        //   public static UIModuleCharacterProgression hookToProgressionModule = null;
        //  public static GeoCharacter hookToCharacter = null;
        internal static bool moduleInfoBarAdjustmentsExecuted = false;
        // public static bool showFaceNotHelmet = true;

        internal static Color red = new Color32(192, 32, 32, 255);
        internal static Color purple = new Color32(149, 23, 151, 255);
        internal static Color blue = new Color32(62, 12, 224, 255);
        internal static Color green = new Color32(12, 224, 30, 255);
        internal static Color anu = new Color(0.9490196f, 0.0f, 1.0f, 1.0f);
        internal static Color nj = new Color(0.156862751f, 0.6156863f, 1.0f, 1.0f);
        internal static Color syn = new Color(0.160784319f, 0.8862745f, 0.145098045f, 1.0f);


        //Adapted from Mad´s Assorted Adjustments; this patch changes Geoescape UI

        [HarmonyPatch(typeof(UIModuleInfoBar), "Init")]
        public static class TFTV_UIModuleInfoBar_Init_GeoscapeUI_Patch
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
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Delirium tooltip";
                    deliriumTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    deliriumTooltip.gameObject.name = "DeliriumTooltip";
                    deliriumTooltip.gameObject.SetActive(false);
                    //TFTVLogger.Always("Got here");

                    Transform evolutionTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("PandoranEvolutionIcon"));
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Pandoran Evolution tooltip";
                    evolutionTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    evolutionTooltip.gameObject.name = "PandoranEvolutionTooltip";
                    evolutionTooltip.gameObject.SetActive(false);

                    Transform anuTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                     Find("PopulationDoom_Meter").GetComponent<Transform>().Find("AnuIcon"));
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = "Anu tooltip";
                    anuTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    anuTooltip.gameObject.name = "AnuTooltip";
                    anuTooltip.gameObject.SetActive(false);

                    Transform njTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("NJIcon"));
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipText = "nj tooltip";
                    njTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    njTooltip.gameObject.name = "NJTooltip";
                    njTooltip.gameObject.SetActive(false);

                    Transform synTooltip = UnityEngine.Object.Instantiate(thingsToUse[0], tInfoBar.GetComponent<Transform>().
                    Find("PopulationDoom_Meter").GetComponent<Transform>().Find("SynIcon"));
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipText = "syn tooltip";
                    synTooltip.gameObject.GetComponent<UITooltipText>().TipKey.LocalizationKey = "";
                    synTooltip.gameObject.name = "SynTooltip";
                    synTooltip.gameObject.SetActive(false);


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


        //Patch to ensure that patch above is only run once
        [HarmonyPatch(typeof(PhoenixGame), "RunGameLevel")]
        public static class PhoenixGame_RunGameLevel_Patch
        {
            public static void Prefix()
            {
                moduleInfoBarAdjustmentsExecuted = false;
            }
        }

        //Second patch to update Geoscape UI
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

                    Transform anuTooltip = populationBar.GetComponent<Transform>().Find("AnuIcon").GetComponent<Transform>().Find("AnuTooltip");
                    Transform njTooltip = populationBar.GetComponent<Transform>().Find("NJIcon").GetComponent<Transform>().Find("NJTooltip");
                    Transform synTooltip = populationBar.GetComponent<Transform>().Find("SynIcon").GetComponent<Transform>().Find("SynTooltip");


                    string anuToolTipText = "<b>The Disciples of Anu</b>";
                    string njToolTipText = "<b>New Jericho</b>";
                    string synToolTipText = "<b>Synedrion</b>";

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("AN_Discovered_DiplomacyStateTagDef")))
                    {
                        anuInfo.gameObject.SetActive(true);
                        anuIcon.gameObject.SetActive(true);
                        anuTooltip.gameObject.SetActive(true);

                        anuTooltip.gameObject.GetComponent<UITooltipText>().TipText = anuToolTipText + "\n" + CreateTextForAnuTooltipText(controller);

                    }

                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("NJ_Discovered_DiplomacyStateTagDef")))
                    {
                        njInfo.gameObject.SetActive(true);
                        njIcon.gameObject.SetActive(true);
                        njTooltip.gameObject.SetActive(true);

                        njTooltip.gameObject.GetComponent<UITooltipText>().TipText = njToolTipText + "\n" + CreateTextForNJTooltipText(controller);
                    }
                    if (controller.PhoenixFaction.GameTags.Contains(DefCache.GetDef<DiplomacyStateTagDef>("SY_Discovered_DiplomacyStateTagDef")))
                    {
                        synInfo.gameObject.SetActive(true);
                        synIcon.gameObject.SetActive(true);
                        synTooltip.gameObject.SetActive(true);

                        synTooltip.gameObject.GetComponent<UITooltipText>().TipText = synToolTipText + "\n" + CreateTextForSynTooltipText(controller);
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
                        deliriumToolTipText += "\n-<i>No limit to Delirium, regardless of ODI level</i>  Void Omen is in effect.";
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




                    /* if (controller.EventSystem.GetEventRecord("SDI_01")?.SelectedChoice == 0 && controller.EventSystem.GetEventRecord("PROG_FS2_WIN")?.SelectedChoice == 0)
                     {
                         deliriumIconHolder.gameObject.SetActive(false);
                     }*/

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

        public static string CreateTextForAnuTooltipText(GeoLevelController controller)
        {
            try
            {
                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.AnuFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();


                if (controller.EventSystem.GetEventRecord("PROG_AN6")?.SelectedChoice == 1 || controller.EventSystem.GetEventRecord("PROG_AN6_2")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN4")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the second special mission offered by this faction (will be offered again at 49%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_AN6_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_AN6_WIN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN4_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_AN2_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }


                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static string CreateTextForSynTooltipText(GeoLevelController controller)
        {
            try
            {
                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.SynedrionFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();
                int polyCounter = controller.EventSystem.GetVariable("Polyphonic");
                int terraCounter = controller.EventSystem.GetVariable("Terraformers");

                if (controller.EventSystem.GetEventRecord("PROG_SY4_T")?.SelectedChoice == 1 || controller.EventSystem.GetEventRecord("PROG_SY4_P")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }

                else if (controller.EventSystem.GetEventRecord("PROG_SY1")?.SelectedChoice == 2)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_SY4_WIN1")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_SY4_WIN2")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_SY3_WIN")?.SelectedChoice != null)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_SY1_WIN1")?.SelectedChoice != null || controller.EventSystem.GetEventRecord("PROG_SY1_WIN2")?.SelectedChoice != null)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }

                if (polyCounter > terraCounter)
                {
                    text += "\n-Through Phoenix Project influence, the Polyphonic tendency is currently ascendant in Synedrion";

                }
                else if (polyCounter < terraCounter)
                {
                    text += "\n-Through Phoenix Project influence, the Terraformers are currently ascendant in Synedrion";
                }



                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

        public static string CreateTextForNJTooltipText(GeoLevelController controller)
        {
            try
            {
                // TFTVLogger.Always($"Checking NJ Diplo status {controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice}");

                string text = "";
                GeoFaction phoenix = controller.PhoenixFaction;
                PartyDiplomacyStateEntry relation = controller.NewJerichoFaction.Diplomacy.GetDiplomacyStateEntry(phoenix);
                text = relation.StateText.Localize();


                if (controller.EventSystem.GetEventRecord("PROG_NJ3")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the third special mission offered by this faction (will be offered again at 74%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ2")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the second special mission offered by this faction (will be offered again at 49%)";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ1")?.SelectedChoice == 1)
                {
                    text += "\n-You have postponed the first special mission offered by this faction (will be offered again at 24%)";
                }

                if (controller.EventSystem.GetEventRecord("PROG_NJ3_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed all the special missions for this faction; you have full access to their research tree";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 0 || controller.EventSystem.GetEventRecord("PROG_NJ2__WIN")?.SelectedChoice == 1)
                {
                    text += "\n-You have completed the second special mission for this faction; you will gain access to any technology researched by the faction";
                }
                else if (controller.EventSystem.GetEventRecord("PROG_NJ1_WIN")?.SelectedChoice == 0)
                {
                    text += "\n-You have completed the first special misssion for this faction; all their havens have been revealed to you";
                }


                return text;

            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }


        }

    }
}
