using Base.AI;
using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{
    internal class TFTVStamina
    {
        // Setting Stamina to zero if character suffered a disabled limb during tactical

        //A list of operatives that get disabled limbs. This list is cleared when the game is exited, so saving a game in tactical, exiting the game and reloading will probably make the game "forget" the character was ever injured.
        //public static List<int> charactersWithBrokenLimbs = new List<int>();
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static Dictionary<int, List<string>> charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();

        // This first patch is to "register" the injury in the above list
        [HarmonyPatch(typeof(BodyPartAspect), "OnSetToDisabled")]
        internal static class BodyPartAspect_OnSetToDisabled_patch
        {
         /*   public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.StaminaPenaltyFromInjury;
            }*/

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(BodyPartAspect __instance)
            {
                if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                {                 
                    TacticalItem base_OwnerItem = (TacticalItem)AccessTools.Property(typeof(TacticalItemAspectBase), "OwnerItem").GetValue(__instance, null);
                    int unitId = base_OwnerItem.TacticalActorBase.GeoUnitId;
              
                    ItemSlotDef itemSlotDef = base_OwnerItem.ItemDef.RequiredSlotBinds[0].RequiredSlot as ItemSlotDef;

                    string bodyPart = itemSlotDef.SlotName;

                    if (base_OwnerItem.TacticalActor.IsAlive && !base_OwnerItem.TacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                    {
                        if (!charactersWithDisabledBodyParts.ContainsKey(unitId))
                        {
                            charactersWithDisabledBodyParts.Add(unitId, new List<string> { bodyPart });
                            TFTVLogger.Always(base_OwnerItem.TacticalActor.GetDisplayName() + " has a disabled " + bodyPart);
                        }
                        else if (charactersWithDisabledBodyParts.ContainsKey(unitId) && !charactersWithDisabledBodyParts[unitId].Contains(bodyPart) && bodyPart != null)
                        {

                            charactersWithDisabledBodyParts[unitId].Add(bodyPart);
                            TFTVLogger.Always(base_OwnerItem.TacticalActor.GetDisplayName() + " has a disabled " + bodyPart);
                        }
                    }
                }
            }
        }

        public static void CheckBrokenLimbs(List<GeoCharacter> geoCharacters, GeoLevelController controller)
        {
            try
            {
            
                GameTagDef bionicalTag = GameUtl.GameComponent<SharedData>().SharedGameTags.BionicalTag;


                foreach (GeoCharacter geoCharacter in geoCharacters)
                {                
                    List<GeoItem> bionicItems = new List<GeoItem>();
                    foreach (GeoItem geoItem in geoCharacter.ArmourItems)
                    {
                        if (geoItem.ItemDef.Tags.Contains(bionicalTag))
                        {
                            geoCharacter.RestoreBodyPart(geoItem);
                            bionicItems.Add(geoItem);
                            //TFTVLogger.Always("Bionic is " + geoItem.ItemDef.name);
                        }
                    }

                    if (charactersWithDisabledBodyParts.ContainsKey(geoCharacter.Id))
                    {
                        List<string> disabledBodyParts = charactersWithDisabledBodyParts[geoCharacter.Id].ToList();

                        foreach (string bodyPart in disabledBodyParts)
                        {
                            MethodInfo damageBodyPart = typeof(GeoCharacter).GetMethod("DamageBodyPart", BindingFlags.Instance | BindingFlags.NonPublic);
                            damageBodyPart.Invoke(geoCharacter, new object[] { bodyPart, 200 });
                            TFTVLogger.Always(geoCharacter.DisplayName + " has a disabled " + bodyPart + ", setting its HP to Zero");

                        }

                        if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                        {
                            TFTVCommonMethods.SetStaminaToZero(geoCharacter);
                        }
                    }
                }

                charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        //When getting a mutation, the character's Stamina is to 0
        [HarmonyPatch(typeof(UIModuleMutationSection), "ApplyMutation")]
        public static class UIModuleMutationSection_ApplyMutation_SetStaminaTo0_patch
        {

         /*   public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.StaminaPenaltyFromInjury;
            }*/

            public static void Postfix(IAugmentationUIModule ____parentModule)
            {
                try
                {
                 //   TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                    {
                        ____parentModule.CurrentCharacter.Fatigue.Stamina.SetToMin();
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        //When getting an augment, the character's Stamina is set to 0
        [HarmonyPatch(typeof(UIModuleBionics), "OnAugmentApplied")]
        public static class UIModuleBionics_OnAugmentApplied_SetStaminaTo0_patch
        {
        /*    public static bool Prepare()
            {
                TFTVConfig config = TFTVMain.Main.Config;
                return config.StaminaPenaltyFromInjury;
            }*/


            public static void Postfix(UIModuleBionics __instance)
            {
                try
                {
                 //   TFTVConfig config = TFTVMain.Main.Config;
                    GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                    if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                    {
                        //set Stamina to zero after installing a bionic
                        __instance.CurrentCharacter.Fatigue.Stamina.SetToMin();
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
