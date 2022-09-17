using Base.Core;
using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.View.DataObjects;
using PhoenixPoint.Geoscape.View.ViewControllers.BaseRecruits;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Animations;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.Mist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVDeliriumPerks
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        internal static bool doNotLocalize = false;
        private static readonly SharedData sharedData = GameUtl.GameComponent<SharedData>();

        private static readonly TacticalAbilityDef hyperalgesiaAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Hyperalgesia_AbilityDef"));
        private static readonly TacticalAbilityDef feralAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Feral_AbilityDef"));
        private static readonly TacticalAbilityDef bloodthirstyAbilityDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Bloodthirsty_AbilityDef"));

        private static readonly TacticalAbilityDef fasterSynapsesDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("FasterSynapses_AbilityDef"));
        private static readonly TacticalAbilityDef anxietyDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("AnxietyAbilityDef"));
      
        private static readonly TacticalAbilityDef oneOfThemDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("OneOfThemPassive_AbilityDef"));
        private static readonly TacticalAbilityDef wolverineDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Wolverine_AbilityDef"));
        private static readonly TacticalAbilityDef derealizationDef = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("DerealizationIgnorePain_AbilityDef"));
        private static readonly StatusDef frenzy = Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Frenzy_StatusDef"));
        private static readonly StatusDef anxiety = Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("Anxiety_StatusDef"));
        private static readonly StatusDef mistResistance = Repo.GetAllDefs<StatusDef>().FirstOrDefault(sd => sd.name.Equals("MistResistance_StatusDef"));
        private static readonly GameTagDef mistResistanceTag = Repo.GetAllDefs<GameTagDef>().FirstOrDefault(sd => sd.name.Equals("OneOfUsMistResistance_GameTagDef"));
      

        

        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_DeliriumPerks_Patch
        {
                     
            public static void Postfix(TacticalActorBase actor)
            {
                try
                {             
                    if (actor.TacticalFaction.Faction.BaseDef == sharedData.PhoenixFactionDef)
                    {
                        TacticalActor tacticalActor = actor as TacticalActor;

                        if (actor.GetAbilityWithDef<Ability>(fasterSynapsesDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(frenzy);
                            TFTVLogger.Always(actor.DisplayName + " with " + fasterSynapsesDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(anxietyDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(anxiety);
                            TFTVLogger.Always(actor.DisplayName + " with " + anxietyDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(oneOfThemDef) != null)
                        {
                            tacticalActor.Status.ApplyStatus(mistResistance);
                            tacticalActor.GameTags.Add(mistResistanceTag, GameTagAddMode.ReplaceExistingExclusive);
                            TFTVLogger.Always(actor.DisplayName + " with " + oneOfThemDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(wolverineDef) != null)
                        {
                            tacticalActor.AddAbility(wolverineDef, actor);
                            TFTVLogger.Always(actor.DisplayName + " with " + wolverineDef.name);
                        }

                        if (actor.GetAbilityWithDef<Ability>(derealizationDef) != null)
                        {
                            
                            tacticalActor.CharacterStats.Endurance.Value.ModificationValue -= 5;
                            tacticalActor.CharacterStats.Endurance.Max.ModificationValue -= 5;
                            tacticalActor.UpdateStats();


                            // tacticalActor.AddAbility(Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(sd => sd.name.Equals("IgnorePain_AbilityDef")), actor);

                            TFTVLogger.Always(actor.DisplayName + " with " + derealizationDef.name);

                        }

                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalAbility), "FumbleActionCheck")]
        public static class TacticalAbility_FumbleActionCheck_Patch
        {
            private static readonly TacticalAbilityDef feral = Repo.GetAllDefs<TacticalAbilityDef>().FirstOrDefault(tad => tad.name.Equals("Feral_AbilityDef"));

            public static void Postfix(TacticalAbility __instance, ref bool __result)
            {
               
                try
                {
                    
                    if (__instance.TacticalActor.GetAbilityWithDef<TacticalAbility>(feral) != null && __instance.Source is Equipment)
                    {
                        __result = UnityEngine.Random.Range(0, 100) < 10;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        //Dtony's Delirium perks patch
        [HarmonyPatch(typeof(RecruitsListElementController), "SetRecruitElement")]
        public static class RecruitsListElementController_SetRecruitElement_Patch
        {
            public static bool Prefix(RecruitsListElementController __instance, RecruitsListEntryData entryData, List<RowIconTextController> ____abilityIcons)
            {
                try
                {
                    if (____abilityIcons == null)
                    {
                        ____abilityIcons = new List<RowIconTextController>();
                        if (__instance.PersonalTrackRoot.transform.childCount < entryData.PersonalTrackAbilities.Count())
                        {
                            RectTransform parent = __instance.PersonalTrackRoot.GetComponent<RectTransform>();
                            RowIconTextController source = parent.GetComponentInChildren<RowIconTextController>();
                            parent.DetachChildren();
                            source.Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);
                            for (int i = 0; i < entryData.PersonalTrackAbilities.Count(); i++)
                            {
                                RowIconTextController entry = UnityEngine.Object.Instantiate(source, parent, true);
                            }
                        }
                        UIUtil.GetComponentsFromContainer(__instance.PersonalTrackRoot.transform, ____abilityIcons);
                    }
                    __instance.RecruitData = entryData;
                    __instance.RecruitName.SetSoldierData(entryData.Recruit);
                    BC_SetAbilityIcons(entryData.PersonalTrackAbilities.ToList(), ____abilityIcons);
                    if (entryData.SuppliesCost != null && __instance.CostText != null && __instance.CostColorController != null)
                    {
                        __instance.CostText.text = entryData.SuppliesCost.ByResourceType(ResourceType.Supplies).RoundedValue.ToString();
                        __instance.CostColorController.SetWarningActive(!entryData.IsAffordable, true);
                    }
                    __instance.NavHolder.RefreshNavigation();
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }


            private static void BC_SetAbilityIcons(List<TacticalAbilityViewElementDef> abilities, List<RowIconTextController> abilityIcons)
            {
                foreach (RowIconTextController rowIconTextController in abilityIcons)
                {
                    rowIconTextController.gameObject.SetActive(false);
                }
                for (int i = 0; i < abilities.Count; i++)
                {
                    abilityIcons[i].gameObject.SetActive(true);
                    abilityIcons[i].SetController(abilities[i].LargeIcon, abilities[i].DisplayName1, abilities[i].Description);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "OnAnotherActorDeath")]
        public static class TacticalActor_OnAnotherActorDeath_Patch
        {
            
            public static void Postfix(TacticalActor __instance, DeathReport death)
            {
                DefRepository Repo = GameUtl.GameComponent<DefRepository>();
                try
                {
                   
                    TacticalAbility hyperAlgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                    TacticalAbility feral = __instance.GetAbilityWithDef<TacticalAbility>(feralAbilityDef);
                    TacticalAbility bloodthirsty = __instance.GetAbilityWithDef<TacticalAbility>(bloodthirstyAbilityDef);

                    if (hyperAlgesia != null)
                    {
                        TacticalFaction tacticalFaction = death.Actor.TacticalFaction;                        
                        int willPointWorth = death.Actor.TacticalActorBaseDef.WillPointWorth;
                        if (death.Actor.TacticalFaction == __instance.TacticalFaction)
                        {
                            __instance.CharacterStats.WillPoints.Add(willPointWorth);
                        }
                    }
                    if (feral != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.ActionPoints.Add(__instance.CharacterStats.ActionPoints.Max/4);
                    }
                    if (bloodthirsty != null && __instance == death.Killer)
                    {
                        __instance.CharacterStats.Health.AddRestrictedToMax(death.Actor.Health.Max/2);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static void Postfix(TacticalActor __instance, DamageResult damageResult)
            {

                DefRepository Repo = GameUtl.GameComponent<DefRepository>();
                try
                {
                    bool receivedDamage = false;
    
                    TacticalAbility hyperalgesia = __instance.GetAbilityWithDef<TacticalAbility>(hyperalgesiaAbilityDef);
                  //  TacticalAbility derealization = __instance.GetAbilityWithDef<TacticalAbility>(derealizationDef);
                    if (__instance.IsAlive && hyperalgesia != null && !receivedDamage)
                    {
                        __instance.CharacterStats.WillPoints.Subtract(1);
                        receivedDamage = true;
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

