using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace TFTV
{
    class TFTVAnniversaryCheats
    {

        [HarmonyPatch(typeof(UIModuleMutate), "InitCharacterInfo")]
        static class UIModuleMutate_InitCharacterInfo_Patch
        {
            // reflect the private fields/properties we need
            static readonly FieldInfo _augmentSectionsFI =
                AccessTools.Field(typeof(UIModuleMutate), "_augmentSections");
            static readonly FieldInfo _currentAugCountFI =
                AccessTools.Field(typeof(UIModuleMutate), "_currentCharacterAugmentsAmount");
            static readonly PropertyInfo CurrentCharacterPI =
                AccessTools.Property(typeof(UIModuleMutate), "CurrentCharacter");
            static readonly FieldInfo LockedBionicsKeyFI =
                AccessTools.Field(typeof(UIModuleMutate), "LockedDueToBionicsKey");
            static readonly FieldInfo LockedLimitKeyFI =
                AccessTools.Field(typeof(UIModuleMutate), "LockedDueToLimitKey");
            static readonly FieldInfo XoutOfYFI =
                AccessTools.Field(typeof(UIModuleMutate), "XoutOfY");
            static readonly FieldInfo MutationsAvailableValueFI =
                AccessTools.Field(typeof(UIModuleMutate), "MutationsAvailableValue");

            static bool Prefix(UIModuleMutate __instance)
            {
                try
                {
                    // if the config flag is off, just run the original
                    if (!TFTVMain.Main.Config.AllowFullAugmentations)
                        return true;

                    // runtime limit
                    int limit = 3;

                    // pull out the private fields
                    var augmentSections = (Dictionary<AddonSlotDef, UIModuleMutationSection>)
                        _augmentSectionsFI.GetValue(__instance);
                    var currentChar = (GeoCharacter)CurrentCharacterPI.GetValue(__instance);

                    // count how many they have
                    int count = AugmentScreenUtilities.GetNumberOfAugments(currentChar);
                    _currentAugCountFI.SetValue(__instance, count);

                    bool hasFreeSlot = count < limit;

                    // grab the tags
                    var tags = GameUtl.GameComponent<SharedData>().SharedGameTags;
                    var bionicsTag = tags.BionicalTag;
                    var mutationTag = tags.AnuMutationTag;

                    // 1) reset each slot
                    foreach (var kv in augmentSections)
                    {
                        var slotDef = kv.Key;
                        var section = kv.Value;

                        string reasonKey = null;
                        var existing = AugmentScreenUtilities.GetAugmentAtSlot(currentChar, slotDef);
                        bool isBionic = existing != null && existing.Tags.Contains(bionicsTag);
                        bool isMutation = existing != null && existing.Tags.Contains(mutationTag);

                        AugumentSlotState state;
                        if (isBionic)
                        {
                            state = AugumentSlotState.BlockedByPermenantAugument;
                            reasonKey = ((LocalizedTextBind)LockedBionicsKeyFI.GetValue(__instance)).LocalizationKey;
                        }
                        else if (!hasFreeSlot && !isMutation)
                        {
                            state = AugumentSlotState.AugumentationLimitReached;
                            reasonKey = ((LocalizedTextBind)LockedLimitKeyFI.GetValue(__instance)).LocalizationKey;
                        }
                        else
                        {
                            state = AugumentSlotState.Available;
                        }

                        section.ResetContainer(state, reasonKey);
                    }

                    // 2) mark used mutations
                    foreach (var armor in currentChar.ArmourItems.Where(t =>
                        t.ItemDef.Tags.Contains(mutationTag)))
                    {
                        foreach (var bind in armor.ItemDef.RequiredSlotBinds)
                        {
                            if (augmentSections.TryGetValue(bind.RequiredSlot, out var sec))
                                sec.SetMutationUsed(armor.ItemDef);
                        }
                    }

                    // 3) update the "X out of Y" text
                    string text = ((LocalizedTextBind)XoutOfYFI.GetValue(__instance)).Localize();
                    text = text.Replace("{0}", count.ToString())
                               .Replace("{1}", limit.ToString());
                    var uiText = (Text)MutationsAvailableValueFI.GetValue(__instance);
                    uiText.text = text;
                    uiText.GetComponent<UIColorController>()
                          .SetWarningActive(limit <= count);

                    // skip the original entirely
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    // on error, fall back to vanilla behavior
                    return true;
                }
            }
        }


        [HarmonyPatch(typeof(UIModuleBionics), "InitCharacterInfo")]
        static class UIModuleBionics_InitCharacterInfo_Patch
        {
            // Cache all the FieldInfos/PropertyInfos we'll need
            static readonly FieldInfo _augmentSectionsFI =
                AccessTools.Field(typeof(UIModuleBionics), "_augmentSections");
            static readonly FieldInfo _currentAugCountFI =
                AccessTools.Field(typeof(UIModuleBionics), "_currentCharacterAugmentsAmount");
            static readonly PropertyInfo CurrentCharacterPI =
                AccessTools.Property(typeof(UIModuleBionics), "CurrentCharacter");
            static readonly FieldInfo LockedMutationKeyFI =
                AccessTools.Field(typeof(UIModuleBionics), "LockedDueToMutationKey");
            static readonly FieldInfo LockedLimitKeyFI =
                AccessTools.Field(typeof(UIModuleBionics), "LockedDueToLimitKey");
            static readonly FieldInfo XoutOfYFI =
                AccessTools.Field(typeof(UIModuleBionics), "XoutOfY");
            static readonly FieldInfo AugmentsAvailableValueFI =
                AccessTools.Field(typeof(UIModuleBionics), "AugmentsAvailableValue");

            static bool Prefix(UIModuleBionics __instance)
            {
                try
                {
                    TFTVConfig config = TFTVMain.Main.Config;

                    if (!config.AllowFullAugmentations) 
                    {
                        return true;
                    }

                    // ① Decide limit at runtime
                    int limit = 3;

                    // ② Grab private fields & props
                    var augmentSections = (Dictionary<AddonSlotDef, UIModuleMutationSection>)
                        _augmentSectionsFI.GetValue(__instance);
                    var currentChar = (GeoCharacter)CurrentCharacterPI.GetValue(__instance);

                    // ③ Count current augments
                    int count = AugmentScreenUtilities.GetNumberOfAugments(currentChar);
                    _currentAugCountFI.SetValue(__instance, count);

                    bool hasFreeSlot = count < limit;

                    // ④ Reset each slot’s state
                    foreach (var kv in augmentSections)
                    {
                        var slotDef = kv.Key;
                        var section = kv.Value;

                        string reasonKey = null;
                        var existing = AugmentScreenUtilities.GetAugmentAtSlot(currentChar, slotDef);
                        bool isMutation = existing != null && existing.Tags.Contains(
                            GameUtl.GameComponent<SharedData>().SharedGameTags.AnuMutationTag);

                        AugumentSlotState state;
                        if (existing != null && isMutation)
                        {
                            state = AugumentSlotState.BlockedByPermenantAugument;
                            reasonKey = ((LocalizedTextBind)LockedMutationKeyFI.GetValue(__instance)).LocalizationKey;
                        }
                        else if (!hasFreeSlot)
                        {
                            state = AugumentSlotState.AugumentationLimitReached;
                            reasonKey = ((LocalizedTextBind)LockedLimitKeyFI.GetValue(__instance)).LocalizationKey;
                        }
                        else
                        {
                            state = AugumentSlotState.Available;
                        }

                        section.ResetContainer(state, reasonKey);
                    }

                    // ⑤ Mark which ones are already used
                    foreach (var armor in currentChar.ArmourItems.Where(t =>
                             t.ItemDef.Tags.Contains(
                               GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag)))
                    {
                        foreach (var bind in armor.ItemDef.RequiredSlotBinds)
                        {
                            if (augmentSections.TryGetValue(bind.RequiredSlot, out var sec))
                                sec.SetMutationUsed(armor.ItemDef);
                        }
                    }

                    // ⑥ Update the “X out of Y” text
                    string text = ((LocalizedTextBind)XoutOfYFI.GetValue(__instance)).Localize();
                    text = text.Replace("{0}", count.ToString())
                               .Replace("{1}", limit.ToString());
                    var uiText = (Text)AugmentsAvailableValueFI.GetValue(__instance);
                    uiText.text = text;
                    uiText.GetComponent<UIColorController>().SetWarningActive(hasFreeSlot);

                    // ⑦ **Skip** the original entirely
                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }




}
