using Base.Core;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers.AugmentationScreen;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV
{


    internal class TFTVStamina
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        // Setting Stamina to zero if character suffered a disabled limb during tactical

        public static DamageMultiplierStatusDef BrokenSpikeShooterStatus;

        //A list of operatives that get disabled limbs. This list is cleared when the game is exited, so saving a game in tactical, exiting the game and reloading will probably make the game "forget" the character was ever injured.
        //public static List<int> charactersWithBrokenLimbs = new List<int>();
        private static readonly SharedData Shared = TFTVMain.Shared;
        public static Dictionary<int, List<string>> charactersWithDisabledBodyParts = new Dictionary<int, List<string>>();

        [HarmonyPatch(typeof(BodyPartAspect), nameof(BodyPartAspect.OnSetToEnable))]
        internal static class BodyPartAspect_OnSetToEnable_patch
        {
            public static void Postfix(BodyPartAspect __instance)
            {
                try
                {
                    TacticalItem base_OwnerItem = (TacticalItem)AccessTools.Property(typeof(TacticalItemAspectBase), "OwnerItem").GetValue(__instance, null);
                    TacticalActor tacticalActor = base_OwnerItem.TacticalActor;

                    if (__instance.BodyPartAspectDef.name.Equals("E_BodyPartAspect [AN_Berserker_Shooter_LeftArm_WeaponDef]"))
                    {
                        UnusableHandStatusDef unUsableLeftHandStatus = DefCache.GetDef<UnusableHandStatusDef>("UnusableLeftHand_StatusDef");

                        if (tacticalActor.HasStatus(BrokenSpikeShooterStatus))
                        {
                            TFTVLogger.Always($"removing {BrokenSpikeShooterStatus.name} from {tacticalActor.name}, because {__instance.BodyPartAspectDef.name} is reenabled");
                            tacticalActor.Status.UnapplyStatus(tacticalActor.Status.GetStatusByName(BrokenSpikeShooterStatus.EffectName));
                        }

                        if (!tacticalActor.HasStatus(unUsableLeftHandStatus))
                        {
                            TFTVLogger.Always($"adding {unUsableLeftHandStatus.name}");
                            tacticalActor.Status.ApplyStatus(unUsableLeftHandStatus);
                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }





        // This first patch is to "register" the injury in the above list
        [HarmonyPatch(typeof(TacticalActor), nameof(TacticalActor.ShouldChangeAspectStats))]
        internal static class TacticalActor_ShouldChangeAspectStats_patch
        {

            public static void Postfix(TacticalActor __instance, TacticalItemAspectBase aspect)
            {
                try
                {

                    //   TFTVLogger.Always($"{__instance.DisplayName} ShouldChangeAspectStats called!");

 

                    TacticalItem tacticalItem = aspect.OwnerItem;

                    TacticalActor tacticalActor = __instance;
                    
                    //  FreezeAspectStatsStatus
                    if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                    {                      
                        int unitId = tacticalActor.GeoUnitId;

                        ItemSlotDef itemSlotDef = tacticalItem.ItemDef.RequiredSlotBinds[0].RequiredSlot as ItemSlotDef;

                        string bodyPart = itemSlotDef.SlotName;

                        if (tacticalActor.IsAlive && !tacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                        {
                            if (!charactersWithDisabledBodyParts.ContainsKey(unitId))
                            {
                                charactersWithDisabledBodyParts.Add(unitId, new List<string> { bodyPart });
                                TFTVLogger.Always($"{tacticalActor.DisplayName} has a disabled {bodyPart}");
                            }
                            else if (charactersWithDisabledBodyParts.ContainsKey(unitId) && !charactersWithDisabledBodyParts[unitId].Contains(bodyPart) && bodyPart != null)
                            {
                                charactersWithDisabledBodyParts[unitId].Add(bodyPart);
                                TFTVLogger.Always($"{tacticalActor.DisplayName} has a disabled {bodyPart}");
                            }
                        }
                    }


                    if (tacticalItem.BodyPartAspect!=null && tacticalItem.BodyPartAspect.BodyPartAspectDef.name.Equals("E_BodyPartAspect [AN_Berserker_Shooter_LeftArm_WeaponDef]"))
                    {
                        if (tacticalActor.HasStatus(DefCache.GetDef<FreezeAspectStatsStatusDef>("IgnorePain_StatusDef")))
                        {
                            return;
                        }

                        UnusableHandStatusDef unUsableLeftHandStatus = DefCache.GetDef<UnusableHandStatusDef>("UnusableLeftHand_StatusDef");

                        if (!tacticalActor.HasStatus(BrokenSpikeShooterStatus))
                        {
                            TFTVLogger.Always($"adding {BrokenSpikeShooterStatus.name} to {tacticalActor.name}, because {tacticalItem.BodyPartAspect.BodyPartAspectDef.name} is disabled");
                            tacticalActor.Status.ApplyStatus(BrokenSpikeShooterStatus);
                        }

                        if (tacticalActor.HasStatus(unUsableLeftHandStatus))
                        {
                            TFTVLogger.Always($"Removing {unUsableLeftHandStatus.name}");
                            tacticalActor.Status.UnapplyStatus(tacticalActor.Status.GetStatusByName(unUsableLeftHandStatus.EffectName));
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
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

                    foreach (GeoItem geoItem in geoCharacter.ArmourItems)
                    {
                        if (geoItem.ItemDef.Tags.Contains(bionicalTag))
                        {
                            IEnumerable<ItemSlotDef> slotsInItemDef =
                                geoItem.ItemDef.SubAddons.SelectMany((AddonDef.SubaddonBind i) =>
                                i.SubAddon.RequiredSlotBinds.Select((AddonDef.RequiredSlotBind t) =>
                                t.RequiredSlot)).Concat(geoItem.ItemDef.RequiredSlotBinds.Select((AddonDef.RequiredSlotBind t) => t.RequiredSlot)).OfType<ItemSlotDef>();

                            foreach (ItemSlotDef slot in slotsInItemDef)
                            {
                                FieldInfo bodyPartHealthFieldInfo = typeof(GeoCharacter).GetField("_bodypartHealth", BindingFlags.Instance | BindingFlags.NonPublic);
                                MethodInfo aggregateBodyPartHealthMethodInfo = typeof(GeoCharacter).GetMethod("AggregateBodyPartHealth", BindingFlags.Instance | BindingFlags.NonPublic);


                                List<StatResult> _bodypartHealth = (List<StatResult>)bodyPartHealthFieldInfo.GetValue(geoCharacter);

                                StatResult statResult = _bodypartHealth.FirstOrDefault((StatResult s) => s.Name.Equals(slot.SlotName + "_Health", StringComparison.InvariantCultureIgnoreCase));

                                if (statResult == null) 
                                {
                                    continue;
                                }

                                if (statResult.Value != 0)
                                {
                                    statResult.Value = statResult.Max;
                                    aggregateBodyPartHealthMethodInfo.Invoke(geoCharacter, null);
                                }
                                else
                                {
                                    TFTVLogger.Always($"{geoCharacter.DisplayName} has broken bionic {geoItem.ItemDef.name}");
                                }

                            }
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
        [HarmonyPatch(typeof(UIModuleMutationSection), "ApplyMutation")] //VERIFIED
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
        public static void SetStaminaToZeroOnBionicApplied(UIModuleBionics uIModuleBionics) 
        {
            try
            {
                //   TFTVConfig config = TFTVMain.Main.Config;
                GeoLevelController controller = GameUtl.CurrentLevel().GetComponent<GeoLevelController>();

                if (TFTVNewGameOptions.StaminaPenaltyFromInjurySetting)
                {
                    //set Stamina to zero after installing a bionic
                    uIModuleBionics.CurrentCharacter.Fatigue.Stamina.SetToMin();
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

      

    }
}
