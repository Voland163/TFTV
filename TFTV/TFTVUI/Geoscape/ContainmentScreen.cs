using Base.Core;
using Base.Defs;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Entities.Research.Reward;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewStates;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels.FactionEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Geoscape
{
    internal class ContainmentScreen
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static GameObject infoPanel;
        private static Text infoText;

        private static string _vivisectedText = "VIVISECTED";
        private static string _autopsiedText = "AUTOPSIED";
        private static string _perDayText = "per day:";
        private static string _containmentSlotsText = "Containment slots occupied:";

        private static bool _localizedStringPopulated = false;

        private static void PopulateLocalizedStrings()
        {
            try
            {
                if (!_localizedStringPopulated)
                {
                    _vivisectedText = TFTVCommonMethods.ConvertKeyToString("TFTV_VIVISECTED");
                    _autopsiedText = TFTVCommonMethods.ConvertKeyToString("TFTV_AUTOPSIED");
                    _perDayText = TFTVCommonMethods.ConvertKeyToString("TFTV_PER_DAY");
                    _containmentSlotsText = TFTVCommonMethods.ConvertKeyToString("TFTV_CONTAINMENT_SLOTS_OCCUPIED");
                    _localizedStringPopulated = true;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        public static void RemoveContainmentInfoPanel()
        {
            try
            {
                if (infoPanel != null)
                {
                    UnityEngine.Object.Destroy(infoPanel);
                    infoPanel = null;
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        [HarmonyPatch(typeof(UIStateRosterAliens), "OnActorCycled")] //VERIFIED
        public static class TFTV_UIStateRosterAliens_OnActorCycled_patch
        {
            public static void Postfix(UIStateRosterAliens __instance)
            {
                try
                {
                    GetInfoAboutAlien();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        private static Font _cachedFont = null;

        private const float _verticalOffset = -60;

        private static void InitializeInfoPanel()
        {
            try
            {
                PopulateLocalizedStrings();

                if (infoPanel != null) return;

                float offset = 0;
                if (!TFTVNewGameOptions.LimitedHarvestingSetting)
                {
                    offset = _verticalOffset;
                }

                infoPanel = new GameObject("InfoPanel");
                Canvas canvas = infoPanel.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler canvasScaler = infoPanel.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                infoPanel.AddComponent<GraphicRaycaster>();

                // Add a black background
                GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
                backgroundObject.transform.SetParent(infoPanel.transform);
                Image backgroundImage = backgroundObject.AddComponent<Image>();
                backgroundImage.color = new Color(0, 0, 0, 0.7f);
                RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
                backgroundRect.sizeDelta = new Vector2(230, 200);
                backgroundRect.anchoredPosition = new Vector2(280, -30 + offset);

                GameObject descriptionObject = new GameObject("DescriptionText");
                descriptionObject.transform.SetParent(backgroundObject.transform);
                Text descriptionText = descriptionObject.AddComponent<Text>();
                descriptionText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                descriptionText.fontSize = 10;
                descriptionText.alignment = TextAnchor.UpperLeft;
                descriptionText.color = Color.white;
                descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
                RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
                descriptionRect.sizeDelta = new Vector2(220, 100);
                descriptionRect.anchoredPosition = new Vector2(0, 40);

                GameObject volumeObject = new GameObject("VolumeText");
                volumeObject.transform.SetParent(backgroundObject.transform);
                Text volumeText = volumeObject.AddComponent<Text>();
                volumeText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                volumeText.fontSize = 12;
                volumeText.alignment = TextAnchor.UpperLeft;
                volumeText.color = Color.white;
                RectTransform volumeRect = volumeObject.GetComponent<RectTransform>();
                volumeRect.sizeDelta = new Vector2(220, 30);
                volumeRect.anchoredPosition = new Vector2(0, -40);

                // Create icon object
                GameObject iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(backgroundObject.transform);
                Image iconImage = iconObject.AddComponent<Image>();
                iconImage.preserveAspect = true;
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(20, 20);
                iconRect.anchoredPosition = new Vector2(-105, -55);

                GameObject mutagenTextObject = new GameObject("MutagenText");
                mutagenTextObject.transform.SetParent(backgroundObject.transform);
                Text mutagenText = mutagenTextObject.AddComponent<Text>();
                mutagenText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                mutagenText.fontSize = 12;
                mutagenText.alignment = TextAnchor.UpperLeft;
                mutagenText.color = Color.white;
                RectTransform mutagenTextRect = mutagenTextObject.GetComponent<RectTransform>();
                mutagenTextRect.sizeDelta = new Vector2(200, 30);
                mutagenTextRect.anchoredPosition = new Vector2(5, -60);

                // Create autopsied/vivisected text object
                GameObject statusObject = new GameObject("StatusText");
                statusObject.transform.SetParent(backgroundObject.transform);
                Text statusText = statusObject.AddComponent<Text>();
                statusText.font = _cachedFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                statusText.fontSize = 12;
                statusText.alignment = TextAnchor.UpperLeft;
                statusText.color = Color.white;
                RectTransform statusRect = statusObject.GetComponent<RectTransform>();
                statusRect.sizeDelta = new Vector2(220, 30);
                statusRect.anchoredPosition = new Vector2(0, -90);

                // Store references to the text components
                infoText = descriptionText;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static int CalculateFontSize(string text)
        {
            // TFTVLogger.Always($"length: {text.Length}");

            if (text.Length <= 200)
            {
                return 12;
            }
            else if (text.Length <= 400)
            {
                return 10;
            }
            else
            {
                return 8;
            }
        }

        public static void GetInfoAboutAlien()
        {
            try
            {
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();
                GeoPhoenixFaction phoenixFaction = controller.PhoenixFaction;
                UIModuleActorCycle actorCycleModule = controller.View.GeoscapeModules.ActorCycleModule;
                GeoUnitDescriptor current = actorCycleModule.GetCurrent<GeoUnitDescriptor>();

                int volume = current.Volume;
                float mutagenPerDay = (float)phoenixFaction.GetHarvestingUnitResourceAmount(current, ResourceType.Mutagen) / 10;
                bool vivisected = false;
                bool autopsied = false;
                string description = "";

                foreach (ResearchElement alnResearch in controller.AlienFaction.Research.FactionResearches)
                {
                    if (alnResearch.ResearchDef.Unlocks.Any(u => u is UnitTemplateResearchRewardDef templateReward && templateReward.Template == current.UnitType.TemplateDef))
                    {
                        description = alnResearch.ResearchDef.ViewElementDef.CompleteText.Localize();
                    }
                }

                if (description == "" && current.UnitType.TemplateDef == DefCache.GetDef<TacCharacterDef>("AcidwormTest_AlienMutationVariationDef"))
                {
                    description = TFTVCommonMethods.ConvertKeyToString("ALN_ACIDWORM_RESEARCHDEF_COMPLETE");
                }

                foreach (TacticalFactionEffectDef buff in phoenixFaction.ActorModifierEffects)
                {
                    if (buff.ActorEffectDef is TacStatusEffectDef tacStatusEffectDef && tacStatusEffectDef.StatusDef is DamageMultiplierStatusDef damageMultiplierStatusDef)
                    {
                        if (damageMultiplierStatusDef.OutgoingDamageTargetTags.Any(t => current.UnitType.TemplateDef.ClassTag == t))
                        {
                            vivisected = true;
                            autopsied = false;
                            break;
                        }
                    }
                }

                if (!autopsied)
                {
                    foreach (ResearchElement researchElement in phoenixFaction.Research.Completed)
                    {
                        if (researchElement.GetRevealRequirements().Any(r => r is ActorResearchRequirement researchRequirement
                        && researchRequirement.RequirementDef is ActorResearchRequirementDef actorResearchRequirementDef
                        && actorResearchRequirementDef.Actor != null && actorResearchRequirementDef.Actor.GameTags != null && actorResearchRequirementDef.Actor.GameTags.Contains(current.UnitType.TemplateDef.ClassTag)))
                        {
                            autopsied = true;
                            break;
                        }
                    }
                }

                //   string info = $"{current.GetName()}, {description}\n volume: {volume}, mutagens per day: {mutagenPerDay}, vivisected: {vivisected}, autopsied {autopsied}";
                //   TFTVLogger.Always(info);

                // Initialize and update the info panel
                InitializeInfoPanel();
                //infoText.text = info;
                infoText.fontSize = CalculateFontSize(description);


                // Update the text components
                infoPanel.transform.Find("Background").Find("DescriptionText").GetComponent<Text>().text = description;
                infoPanel.transform.Find("Background").Find("VolumeText").GetComponent<Text>().text = $"{_containmentSlotsText} {volume}";
                infoPanel.transform.Find("Background").Find("MutagenText").GetComponent<Text>().text = $"{_perDayText} {mutagenPerDay}";

                // Update the status text
                string status = "";
                if (autopsied && !vivisected)
                {
                    status = _autopsiedText;
                }
                else if (vivisected)
                {
                    status = _vivisectedText;
                }
                infoPanel.transform.Find("Background").Find("StatusText").GetComponent<Text>().text = status;

                // Set the icon sprite (assuming you have a sprite for the icon)
                Sprite iconSprite = DefCache.GetDef<ResourceViewElementDef>("MutagenResourceViewElementDef").Visual;
                infoPanel.transform.Find("Background").Find("Icon").GetComponent<Image>().sprite = iconSprite;
                infoPanel.transform.Find("Background").Find("Icon").GetComponent<Image>().color = DefCache.GetDef<UIColorsDef>("UIColors_MutagenCost_Def").PrimaryUIColor;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        [HarmonyPatch(typeof(GeoRosterItem))]
        [HarmonyPatch("Init", typeof(GeoUnitDescriptor), typeof(IGeoCharacterContainer), typeof(GeoFaction))] //VERIFIED
        public static class GeoRosterItemPatch
        {
            public static void Postfix(GeoRosterItem __instance, IGeoCharacterContainer characterContainer)
            {
                try
                {


                    // UIModuleGeneralPersonelRoster uIModuleGeneralPersonelRoster = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.GeneralPersonelRosterModule;
                    //  uIModuleGeneralPersonelRoster.RosterList.gameObject.SetActive(true);

                    RectTransform rectTransform = __instance.RowButton.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name.Contains("SlotContainer_Layout"));

                    if (rectTransform == null)
                    {
                        return;
                    }

                    GeoRosterAlienContainmentItem geoRosterAlienContainmentItem = __instance.RowButton.GetComponent<GeoRosterAlienContainmentItem>();

                    if (geoRosterAlienContainmentItem == null)
                    {
                        return;
                    }

                    // TFTVLogger.Always($"rectTransform.sizeDelta.x {rectTransform.sizeDelta.x}");
                    if (rectTransform.sizeDelta.x == 1250)
                    {
                        float sizeToCut = rectTransform.sizeDelta.x * 1 / 3;
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - sizeToCut, rectTransform.sizeDelta.y);


                        geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition =
                            new Vector2(geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition.x - sizeToCut, geoRosterAlienContainmentItem.KillAlienButton.GetComponent<RectTransform>().anchoredPosition.y);

                    }
                    if (_cachedFont == null)
                    {
                        _cachedFont = __instance.CharacterName.font;
                        // TFTVLogger.Always($"_cachedFont.name: {_cachedFont.name}");
                    }


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
