using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.Missions;
using PhoenixPoint.Geoscape.Entities.Missions.Outcomes;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Modal;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Home.View.ViewModules;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    internal class TFTVAncients
    {
        // commented out for release #13
      /*  private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static readonly DamageMultiplierStatusDef AddAutoRepairStatusAbility = DefCache.GetDef<DamageMultiplierStatusDef>("AutoRepair_AddAbilityStatusDef");

        private static readonly WeaponDef rightDrill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
        private static readonly WeaponDef rightShield = DefCache.GetDef<WeaponDef>("HumanoidGuardian_RightShield_WeaponDef");
        private static readonly EquipmentDef leftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
        private static readonly WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
        private static readonly EquipmentDef leftCrystalShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_CrystalShield_EquipmentDef");

        private static readonly ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
        private static readonly ClassTagDef hopliteTag = DefCache.GetDef<ClassTagDef>("HumanoidGuardian_ClassTagDef");

        private static readonly PassiveModifierAbilityDef ancientsPowerUpAbility = DefCache.GetDef<PassiveModifierAbilityDef>("AncientMaxPower_AbilityDef");
        private static readonly DamageMultiplierStatusDef ancientsPowerUpStatus = DefCache.GetDef<DamageMultiplierStatusDef>("AncientsPoweredUp");


        //This is the number of previous encounters with Ancients. It is added to the Difficulty to determine the number of fully repaired MediumGuardians in battle
        public static int AncientsEncounterCounter = 0;
        public static string AncientsEncounterVariableName = "Ancients_Encounter_Global_Variable";
        public static int HoplitesKilled = 0;
        private static readonly AlertedStatusDef AlertedStatus = DefCache.GetDef<AlertedStatusDef>("Alerted_StatusDef");
        private static readonly DamageMultiplierStatusDef CyclopsDefenseStatus = DefCache.GetDef<DamageMultiplierStatusDef>("CyclopsDefense_StatusDef");
        private static readonly StanceStatusDef AncientGuardianStealthStatus = DefCache.GetDef<StanceStatusDef>("AncientGuardianStealth_StatusDef");


        [HarmonyPatch(typeof(RewardsController), "SetResources")]
        public static class RewardsController_SetResources_Patch
        {

            public static void Postfix(ResourcePack reward, RewardsController __instance)
            {
                try
                {
                  
                    foreach(ResourceUnit resourceUnit in reward) 
                    {
                        if (resourceUnit.Type == ResourceType.ProteanMutane) 
                        {
                            UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                            Resolution resolution = Screen.currentResolution;
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            float resolutionFactorHeight = (float)resolution.height / 1080f;


                            Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                            Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("ProteanMutaneRes").GetComponent<Transform>().Find("Requirement_Icon");          
                            Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("ProteanMutaneRes").GetComponent<Transform>().Find("Requirement_Text");
                                                    
                            Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                            Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);

                            exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                            // exoticResourceTextCopy.GetComponent<Text>().text = DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestProteanMissionOutcomeDef").Resources[0].Value.ToString();
                            exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                            exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                            exoticResourceTextCopy.Translate(new Vector3(0f, -10f*resolutionFactorHeight, 0f));
                          
                            __instance.NoResourcesText.gameObject.SetActive(false);
                            __instance.ResourcesRewardsParentObject.SetActive(true);

                        }
                        else if(resourceUnit.Type == ResourceType.LivingCrystals)
                        {
                            UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                            Resolution resolution = Screen.currentResolution;
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            float resolutionFactorHeight = (float)resolution.height / 1080f;

                            Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                            Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("LivingCrystalsRes").GetComponent<Transform>().Find("Requirement_Icon");
                            Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("LivingCrystalsRes").GetComponent<Transform>().Find("Requirement_Text");


                            Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                            Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);

                            exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                               // DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestCrystalMissionOutcomeDef").Resources[0].Value.ToString();
                            exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                            exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                            exoticResourceTextCopy.Translate(new Vector3(0f, -10f * resolutionFactorHeight, 0f));
                          
                            __instance.NoResourcesText.gameObject.SetActive(false);
                            __instance.ResourcesRewardsParentObject.SetActive(true);

                        }
                        else if (resourceUnit.Type == ResourceType.Orichalcum)
                        {
                            UIModuleInfoBar uIModuleInfoBar = (UIModuleInfoBar)UnityEngine.Object.FindObjectOfType(typeof(UIModuleInfoBar));

                            Resolution resolution = Screen.currentResolution;
                            float resolutionFactorWidth = (float)resolution.width / 1920f;
                            float resolutionFactorHeight = (float)resolution.height / 1080f;


                            Transform tInfoBar = uIModuleInfoBar.PopulationBarRoot.transform.parent?.transform;
                            Transform exoticResourceIcon = tInfoBar.GetComponent<Transform>().Find("OrichalcumRes").GetComponent<Transform>().Find("Requirement_Icon");
                            Transform exoticResourceText = tInfoBar.GetComponent<Transform>().Find("OrichalcumRes").GetComponent<Transform>().Find("Requirement_Text");


                            Transform exoticResourceIconCopy = UnityEngine.Object.Instantiate(exoticResourceIcon, __instance.ResourcesRewardsParentObject.transform);
                            Transform exoticResourceTextCopy = UnityEngine.Object.Instantiate(exoticResourceText, __instance.ResourcesRewardsParentObject.transform);

                            exoticResourceTextCopy.GetComponent<Text>().text = reward.Values[0].Value.ToString();
                            //DefCache.GetDef<ResourceMissionOutcomeDef>("AncientsHarvestOrichalcumMissionOutcomeDef").Resources[0].Value.ToString();
                            exoticResourceTextCopy.SetParent(exoticResourceIconCopy);
                            exoticResourceIconCopy.localScale = new Vector3(1.5f, 1.5f, 1f);
                            exoticResourceTextCopy.Translate(new Vector3(0f, -10f * resolutionFactorHeight, 0f));

                            __instance.NoResourcesText.gameObject.SetActive(false);
                            __instance.ResourcesRewardsParentObject.SetActive(true);

                        }
                    }
                 
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoVehicle), "get_CanHarvestFromSites")]
        public static class GeoVehicle_get_CanHarvestFromSites_Patch
        {

            public static void Postfix(ref bool __result)
            {
                try
                {

                    __result = false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(AncientGuardianGuardAbility), "GetDisabledStateInternal")]
        public static class AncientGuardianGuardAbility_GetDisabledStateInternal_Patch
        {

            public static void Postfix(ref GeoAbilityDisabledState __result)
            {
                try
                {

                    __result = GeoAbilityDisabledState.RequirementsNotMet;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(GeoMission), "ApplyOutcomes")]
        public static class GeoMission_ModifyMissionData_CheckAncients_Patch
        {

            public static void Postfix(GeoMission __instance)
            {
                try
                {
                    GeoLevelController controller = __instance.Level;

                    if (__instance.MissionDef.SaveDefaultName == "AncientRuin")
                    {
                      
                        controller.EventSystem.SetVariable(AncientsEncounterVariableName, controller.EventSystem.GetVariable(AncientsEncounterVariableName) + 1);
                        TFTVLogger.Always(AncientsEncounterVariableName + " is now " + controller.EventSystem.GetVariable(AncientsEncounterVariableName));

                        List<GeoVehicle> geoVehicles = __instance.Site.Vehicles.ToList();
                        foreach (GeoVehicle vehicle in geoVehicles)
                        {
                            vehicle.EndCollectingFromCurrentSite();

                        }

                        // __instance.Site.Type = GeoSiteType.AncientRefinery;
                        //  __instance.Site.DisableSite();

                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        public static bool CheckIfAncientsPresent(TacticalLevelController controller)
        {
            try
            {
                if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                {
                    TFTVLogger.Always("Ancients present");
                    return true;

                }
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        public static void AdjustAncientsOnDeployment(TacticalLevelController controller)
        {
            ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
            try
            {
                TFTVLogger.Always("AdjustAncientsOnDeployment method invoked");
                TacticalFaction ancients = controller.GetFactionByCommandName("anc");
                CyclopsDefenseStatus.Multiplier = 0.5f;
                List<TacticalActor> damagedGuardians = new List<TacticalActor>();
                int countUndamagedGuardians = AncientsEncounterCounter + controller.Difficulty.Order;

                foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                {
                    TFTVLogger.Always("Found tacticalactorbase");
                    if (tacticalActorBase is TacticalActor && !tacticalActorBase.HasGameTag(cyclopsTag))
                    {
                        TFTVLogger.Always("Found hoplite");
                        TacticalActor guardian = tacticalActorBase as TacticalActor;
                        if (damagedGuardians.Count() + countUndamagedGuardians < ancients.Actors.Count())
                        {
                            damagedGuardians.Add(guardian);
                        }
                        guardian.CharacterStats.WillPoints.Set(guardian.CharacterStats.WillPoints.IntMax / 3);
                        guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                        guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);

                    }
                    else if (tacticalActorBase is TacticalActor cyclops && tacticalActorBase.HasGameTag(cyclopsTag))
                    {
                        TFTVLogger.Always("Found cyclops");
                        tacticalActorBase.Status.ApplyStatus(CyclopsDefenseStatus);
                        cyclops.CharacterStats.WillPoints.Set(cyclops.CharacterStats.WillPoints.IntMax / 4);
                        cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                        cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                    }
                }

                foreach (TacticalActor tacticalActor in damagedGuardians)
                {
                    UnityEngine.Random.InitState((int)Stopwatch.GetTimestamp());
                    int roll = UnityEngine.Random.Range(1, 101);
                    TFTVLogger.Always("The roll is " + roll);


                    foreach (Equipment item in tacticalActor.Equipments.Equipments)
                    {
                        if (item.TacticalItemDef.Equals(beamHead))
                        {
                            if (roll > 45)
                            {
                                item.DestroyAll();
                            }
                        }
                        else if (item.TacticalItemDef.Equals(rightShield) || item.TacticalItemDef.Equals(rightDrill))
                        {
                            if (roll <= 45)
                            {
                                item.DestroyAll();
                            }
                        }
                        else if (item.TacticalItemDef.Equals(leftShield) || item.TacticalItemDef.Equals(leftCrystalShield))
                        {
                            if (roll + 10 * countUndamagedGuardians >= 65)
                            {
                                item.DestroyAll();
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


        public static void CheckCyclopsDefense()
        {
            try
            {
                CyclopsDefenseStatus.Multiplier = 0.5f + HoplitesKilled * 0.1f;
                TFTVLogger.Always("Cyclops Defense level is " + CyclopsDefenseStatus.Multiplier);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }

        public static TacticalItem[] CheckGuardianBodyParts(TacticalActor actor)
        {
            try
            {
                TacticalItem[] equipment = new TacticalItem[3];

                foreach (Equipment item in actor.Equipments.Equipments)
                {
                    if (item.TacticalItemDef.Equals(beamHead))
                    {
                        equipment[0] = item;
                    }
                    else if (item.TacticalItemDef.Equals(rightShield) || item.TacticalItemDef.Equals(rightDrill))
                    {
                        equipment[1] = item;

                    }
                    else if (item.TacticalItemDef.Equals(leftShield) || item.TacticalItemDef.Equals(leftCrystalShield))
                    {
                        equipment[2] = item;
                    }
                }
                return equipment;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new TacticalItem[3];
            }
        }

        public static void AdjustAutomataStats(TacticalLevelController controller)
        {

            try
            {
                TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                {
                    if (tacticalActorBase is TacticalActor guardian && tacticalActorBase.HasGameTag(hopliteTag) && !guardian.Status.HasStatus(AncientGuardianStealthStatus))
                    {


                        if (guardian.CharacterStats.WillPoints >= 30)
                        {
                            if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                            {
                                guardian.AddAbility(ancientsPowerUpAbility, guardian);
                                guardian.Status.ApplyStatus(ancientsPowerUpStatus);
                            }
                        }
                        else
                        {
                            if (guardian.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                            {
                                guardian.RemoveAbility(ancientsPowerUpAbility);
                                guardian.Status.Statuses.Remove(guardian.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));
                            }
                            guardian.CharacterStats.WillPoints.Add(5);
                        }
                        guardian.CharacterStats.Speed.SetMax(guardian.CharacterStats.WillPoints.IntValue);
                        guardian.CharacterStats.Speed.Set(guardian.CharacterStats.WillPoints.IntValue);
                    }
                    else if (tacticalActorBase is TacticalActor cyclops && tacticalActorBase.HasGameTag(cyclopsTag))
                    {
                        if (cyclops.CharacterStats.WillPoints >= 40)
                        {
                            if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) == null)
                            {
                                cyclops.AddAbility(ancientsPowerUpAbility, cyclops);
                                cyclops.Status.ApplyStatus(ancientsPowerUpStatus);
                            }
                        }
                        else
                        {
                            if (cyclops.GetAbilityWithDef<PassiveModifierAbility>(ancientsPowerUpAbility) != null)
                            {
                                cyclops.RemoveAbility(ancientsPowerUpAbility);
                                cyclops.Status.Statuses.Remove(cyclops.Status.GetStatusByName(ancientsPowerUpStatus.EffectName));
                            }
                        }
                        if (cyclops.HasStatus(AlertedStatus))
                        {
                            cyclops.CharacterStats.WillPoints.Add(5);
                        }
                        cyclops.CharacterStats.Speed.SetMax(cyclops.CharacterStats.WillPoints.IntValue);
                        cyclops.CharacterStats.Speed.Set(cyclops.CharacterStats.WillPoints.IntValue);
                    }

                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);

            }

        }


        //set resource cost of excavation (now exploration)
        [HarmonyPatch(typeof(ExcavateAbility), "GetResourceCost")]

        public static class TFTV_GeoAbility_GetResourceCost
        {
            public static void Postfix(ref ResourcePack __result)
            {
                try
                {
                    __result = new ResourcePack() { new ResourceUnit(ResourceType.Materials, value: 20), new ResourceUnit(ResourceType.Tech, value: 5) };

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        //removes icon + text of resource requirement if resource is not required
        [HarmonyPatch(typeof(SiteContextualMenuDescriptionController), "SetResourcesText")]

        public static class TFTV_ResourceDisplayController_SetDisplayedResource
        {
            public static void Postfix(Text textField)
            {
                try
                {

                    if (textField.text == "0")
                    {
                        textField.transform.parent.gameObject.SetActive(value: false);
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);

                }
            }
        }

        [HarmonyPatch(typeof(TacticalFaction), "RequestEndTurn")]
        public static class TacticalFaction_RequestEndTurn_AncientsSelfRepair_Patch
        {
            public static void Postfix(TacticalFaction __instance)
            {
                try
                {
                    if (CheckIfAncientsPresent(__instance.TacticalLevel))
                    {
                        if (__instance.TacticalLevel.TurnNumber > 0 && __instance.Equals(__instance.TacticalLevel.GetFactionByCommandName("PX")))
                        {
                            CheckForAutoRepairAbility(__instance.TacticalLevel);
                            AdjustAutomataStats(__instance.TacticalLevel);
                        }


                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(DamageKeyword), "ProcessKeywordDataInternal")]
        internal static class TFTV_DamageKeyword_ProcessKeywordDataInternal_DamageResistant_patch
        {
            public static void Postfix(ref DamageAccumulation.TargetData data)
            {
                try
                {

                    if (data.Target.GetActor() != null && data.Target.GetActor().Status != null && data.Target.GetActor().Status.HasStatus(AncientGuardianStealthStatus))
                    {
                        //  TFTVLogger.Always("Statis check passed");

                        float multiplier = 0.1f;

                        data.DamageResult.HealthDamage = Math.Min(data.Target.GetHealth(), data.DamageResult.HealthDamage * multiplier);
                        data.AmountApplied = Math.Min(data.Target.GetHealth(), data.AmountApplied * multiplier);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }



        [HarmonyPatch(typeof(TacticalLevelController), "ActorDied")]
        public static class TacticalLevelController_ActorDied_Ancients_Patch
        {
            public static void Postfix(TacticalLevelController __instance, DeathReport deathReport)
            {
                ClassTagDef cyclopsTag = DefCache.GetDef<ClassTagDef>("MediumGuardian_ClassTagDef");
                try
                {
                    if (CheckIfAncientsPresent(__instance))
                    {
                        TacticalFaction ancients = __instance.GetFactionByCommandName("anc");

                        if (deathReport.Actor is TacticalActor)
                        {
                            TacticalActor actor = deathReport.Actor as TacticalActor;
                            if (actor.TacticalFaction == ancients)
                            {
                                foreach (TacticalActorBase allyTacticalActorBase in ancients.Actors)
                                {
                                    if (allyTacticalActorBase is TacticalActor && allyTacticalActorBase != actor)
                                    {
                                        TacticalActor actorAlly = allyTacticalActorBase as TacticalActor;
                                        float magnitude = 5;

                                        if ((actorAlly.Pos - actor.Pos).magnitude <= magnitude)
                                        {
                                            TFTVLogger.Always("Actor in range and will be receiving power from dead friendly");
                                            actorAlly.CharacterStats.WillPoints.Add(5);

                                            if ((CheckGuardianBodyParts(actorAlly)[0] == null
                                            || CheckGuardianBodyParts(actorAlly)[1] == null
                                            || CheckGuardianBodyParts(actorAlly)[2] == null))
                                            {
                                                TFTVLogger.Always("Actor in range and missing bodyparts, getting spare parts");
                                                if (!actorAlly.HasStatus(AddAutoRepairStatusAbility) && !actorAlly.HasGameTag(cyclopsTag))
                                                {
                                                    actorAlly.Status.ApplyStatus(AddAutoRepairStatusAbility);
                                                    TFTVLogger.Always("AutoRepairStatus added to " + actorAlly.name);
                                                }

                                            }
                                            else
                                            {
                                                if (actorAlly.GetHealth() < actorAlly.TotalMaxHealth)
                                                {
                                                    if (actorAlly.GetHealth() + 50 >= actorAlly.TotalMaxHealth)
                                                    {
                                                        actorAlly.Health.Set(actorAlly.TotalMaxHealth);
                                                    }
                                                    else
                                                    {
                                                        actorAlly.Health.Set(actorAlly.GetHealth() + 50);
                                                    }

                                                }

                                            }
                                        }

                                    }
                                }

                                if (!actor.HasGameTag(cyclopsTag))
                                {
                                    if (CyclopsDefenseStatus.Multiplier <= 0.9f)
                                    {
                                        CyclopsDefenseStatus.Multiplier += 0.1f;
                                    }
                                    else
                                    {
                                        CyclopsDefenseStatus.Multiplier = 1;
                                    }
                                    HoplitesKilled++;
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

        public static void CheckForAutoRepairAbility(TacticalLevelController controller)
        {
            try
            {

                TacticalFaction ancients = controller.GetFactionByCommandName("anc");

                foreach (TacticalActorBase tacticalActorBase in ancients.Actors)
                {
                    if (tacticalActorBase is TacticalActor)
                    {
                        TacticalActor actor = tacticalActorBase as TacticalActor;

                        if (actor.HasStatus(AddAutoRepairStatusAbility))
                        {
                            Weapon drill = new Weapon();
                            Weapon shield = new Weapon();
                            Equipment livingShield = new Equipment();
                            Equipment orichalcumShield = new Equipment();

                            foreach (Equipment item in actor.Equipments.Equipments)
                            {
                                if (item.TacticalItemDef == rightDrill)
                                {
                                    drill = item as Weapon;

                                }
                                else if (item.TacticalItemDef == rightShield)
                                {
                                    shield = item as Weapon;

                                }
                                else if (item.TacticalItemDef == leftCrystalShield)
                                {
                                    livingShield = item;

                                }
                                else if (item.TacticalItemDef == leftShield)
                                {
                                    orichalcumShield = item;
                                }
                            }

                            TFTVLogger.Always("Actor has spare parts, making repairs");
                            actor.Status.Statuses.Remove(actor.Status.GetStatusByName(AddAutoRepairStatusAbility.EffectName));
                            if (CheckGuardianBodyParts(actor)[0] == null)
                            {
                                actor.Equipments.AddItem(beamHead);
                            }
                            else if (CheckGuardianBodyParts(actor)[1] == null && livingShield != null)
                            {
                                actor.Equipments.AddItem(rightDrill);
                            }
                            else if (CheckGuardianBodyParts(actor)[1] == null && orichalcumShield != null)
                            {
                                actor.Equipments.AddItem(rightShield);
                            }
                            else if (CheckGuardianBodyParts(actor)[2] == null && drill != null)
                            {
                                actor.Equipments.AddItem(leftCrystalShield);
                            }
                            else if (CheckGuardianBodyParts(actor)[2] == null && shield != null)
                            {
                                actor.Equipments.AddItem(leftShield);
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


        //Adapted Lucus solution to avoid Ancient Automata receiving WP penalty on ally death
        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_HumanEnemies_Patch
        {
            public static void Prefix(TacticalActor __instance, DeathReport death, out int __state)
            {
                __state = 0; //Set this to zero so that the method still works for other actors.

                //Postfix checks for relevant GameTags then saves and zeroes the WPWorth of the dying actor before main method is executed.

                GameTagsList<GameTagDef> RelevantTags = new GameTagsList<GameTagDef> { cyclopsTag, hopliteTag };
                if (__instance.TacticalFaction == death.Actor.TacticalFaction && death.Actor.HasGameTags(RelevantTags, false))
                {
                    __state = death.Actor.TacticalActorBaseDef.WillPointWorth;
                    death.Actor.TacticalActorBaseDef.WillPointWorth = 0;
                }

            }

            public static void Postfix(TacticalActor __instance, DeathReport death, int __state)
            {

                //Postfix will remove necessary Willpoints from allies and restore WPWorth's value to the def of the dying actor.
                if (__instance.TacticalFaction == death.Actor.TacticalFaction)
                {
                    foreach (GameTagDef Tag in death.Actor.GameTags)
                    {
                        if (Tag == cyclopsTag || Tag == hopliteTag)
                        {
                            //Death has no effect on allies
                            death.Actor.TacticalActorBaseDef.WillPointWorth = __state;
                        }
                    }
                }

            }
        }*/



    }
}

/*
 public static void AddDrillBack(TacticalLevelController controller)
 {
     try
     {
         if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
         {
             TFTVLogger.Always("Found ancients");

             BashAbilityDef drillBash =DefCache.GetDef<BashAbilityDef>("Guardian_Drill_AbilityDef");
             ShootAbilityDef beam = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");

             WeaponDef drill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
             EquipmentDef leftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
             WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
             Equipment head = new Equipment();

             foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("anc").TacticalActors)
             {
                 bool foundLeftShield = false;

                 foreach (Equipment item in tacticalActor.Equipments.Equipments)
                 {
                     if (item.TacticalItemDef.Equals(leftShield))
                     {
                         TFTVLogger.Always("Found leftShield");
                         foundLeftShield = true;

                     }

                     if (item.TacticalItemDef.Equals(beamHead))
                     {
                         TFTVLogger.Always("Found beam");
                         head = item;

                     }

                 }

                 if (!foundLeftShield) 
                 {
                    TFTVLogger.Always("Found a driller");
                  //   if (drill == null)
                  //   {
                        TFTVLogger.Always("Should add a drill");
                        tacticalActor.Equipments.AddItem(drillSave);
                 //    }


                 }

             }
         }
     }
     catch (Exception e)
     {
         TFTVLogger.Error(e);
     }
 }

private static Equipment drillSave = new Equipment(); 
[HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
public static class TacticalLevelController_ActorEnteredPlay_Ancients_Patch
{
    public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
    {
        try
        {
            if (__instance.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
            {
                TacticalFaction ancients = __instance.GetFactionByCommandName("anc");

                if (actor is TacticalActor && actor.TacticalFaction == ancients) 
                {
                    BashAbilityDef drillBash = DefCache.GetDef<BashAbilityDef>("Guardian_Drill_AbilityDef");
                    ShootAbilityDef beam = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");

                    WeaponDef drillDef = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
                    WeaponDef beamDef = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                    Equipment head = new Equipment();
                    Equipment drill = new Equipment();


                    TacticalActor tacticalActor = actor as TacticalActor;

                    bool foundDriller = false;

                    foreach (Equipment item in tacticalActor.Equipments.Equipments)
                    {
                        if (item.TacticalItemDef.Equals(drillDef))
                        {
                            TFTVLogger.Always("Found driller");
                            foundDriller = true;
                            drill = item;

                        }

                        if (item.TacticalItemDef.Equals(beamDef))
                        {
                            TFTVLogger.Always("Found beam");
                            head = item;

                        }

                    }

                    if (foundDriller)
                    {

                        TFTVLogger.Always("Drill removed");
                        if (drill != null)
                        {
                            drillSave = drill;
                            drill.DestroyAll();
                        }

                    }




                }
            }

        }
        catch (Exception e)
        {
            TFTVLogger.Error(e);
        }
    }*/





