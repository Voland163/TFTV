using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVUI.Personnel
{
    internal class MercenariesNoAugment
    {

        [HarmonyPatch(typeof(EditUnitButtonsController), "SetContextButtonVisibility")]
        internal static class TFTV_EditUnitButtonsController_SetContextButtonVisibility_Patch
        {
            private static void Postfix(EditUnitButtonsController __instance)
            {
                try
                {
                    ShadeMutationBionics(__instance);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        private static void ShadeMutationBionics(EditUnitButtonsController controller)
        {
            try
            {
                UIModuleActorCycle actorCycle = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.View?.GeoscapeModules?.ActorCycleModule;

                GeoCharacter geoCharacter = actorCycle?.CurrentCharacter;
                if (geoCharacter == null)
                {
                    return;
                }

                PhoenixGeneralButton mutationButton = controller.MutationButton;
                PhoenixGeneralButton bionicsButton = controller.BionicsButton;

                bool mutationAvailable = Traverse.Create(controller).Field("_mutationAvailable").GetValue<bool>();
                bool bionicsAvailable = Traverse.Create(controller).Field("_bionicsAvailable").GetValue<bool>();

                if (!mutationAvailable && !bionicsAvailable)
                {
                    return;
                }

                Text[] texts = controller.GetComponentsInChildren<Text>(true);

                Text bionicsText = bionicsAvailable
                    ? texts.FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_AUMGENTATION_ACTION"))
                    : null;

                Text mutateText = mutationAvailable
                    ? texts.FirstOrDefault(c => c.text == TFTVCommonMethods.ConvertKeyToString("KEY_GEOSCAPE_MUTATE"))
                    : null;

                // Reset vanilla/normal visual state first.
                if (bionicsAvailable && bionicsText != null)
                {
                    bionicsButton.SetInteractable(true);
                    UITooltipText tooltip = bionicsButton.gameObject.GetComponent<UITooltipText>();
                    if (tooltip != null)
                    {
                        tooltip.enabled = false;
                    }

                    bionicsText.color = new Color(0.820f, 0.859f, 0.914f);
                }

                if (mutationAvailable && mutateText != null)
                {
                    mutationButton.SetInteractable(true);
                    UITooltipText tooltip = mutationButton.gameObject.GetComponent<UITooltipText>();
                    if (tooltip != null)
                    {
                        tooltip.enabled = false;
                    }

                    mutateText.color = new Color(0.820f, 0.859f, 0.914f);
                }

                TFTVConfig config = TFTVMain.Main.Config;

                if (geoCharacter.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag)
                    && !config.MercsCanBeAugmented)
                {
                    ApplyAugmentationBlockedState(mutationButton, mutateText);
                    ApplyAugmentationBlockedState(bionicsButton, bionicsText);
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ApplyAugmentationBlockedState(PhoenixGeneralButton button, Text text)
        {
            try
            {

                if (button == null || text == null)
                {
                    return;
                }

                button.SetInteractable(false);

                UITooltipText tooltip = button.gameObject.GetComponent<UITooltipText>();
                if (tooltip == null)
                {
                    tooltip = button.gameObject.AddComponent<UITooltipText>();
                }

                tooltip.TipText = TFTVCommonMethods.ConvertKeyToString("KEY_ABILITY_NOAUGMENTATONS");
                tooltip.enabled = true;

                text.color = Color.gray;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [HarmonyPatch(typeof(EditUnitButtonsController), "GoToMutateScreen")]
        internal static class TFTV_EditUnitButtonsController_GoToMutateScreen_Patch
        {
            private static bool Prefix(EditUnitButtonsController __instance)
            {
                return CanCurrentCharacterUseAugmentations(__instance);
            }
        }

        [HarmonyPatch(typeof(EditUnitButtonsController), "GoToBionicsScreen")]
        internal static class TFTV_EditUnitButtonsController_GoToBionicsScreen_Patch
        {
            private static bool Prefix(EditUnitButtonsController __instance)
            {
                return CanCurrentCharacterUseAugmentations(__instance);
            }
        }

        private static bool CanCurrentCharacterUseAugmentations(EditUnitButtonsController controller)
        {
            UIModuleActorCycle actorCycle = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.View?.GeoscapeModules?.ActorCycleModule;

            GeoCharacter character = actorCycle?.CurrentCharacter;
            if (character == null)
            {
                return true;
            }

            TFTVConfig config = TFTVMain.Main.Config;

            return !character.TemplateDef.GetGameTags().Contains(TFTVChangesToDLC5.MercenaryTag)
                || config.MercsCanBeAugmented;
        }

    }
}
