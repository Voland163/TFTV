using Base;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using HarmonyLib;
using PhoenixPoint.Common.ContextHelp;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Addons;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Abilities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.ContextHelp;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PhoenixPoint.Common.Entities.Items.ItemManufacturing;
using static PhoenixPoint.Geoscape.Entities.PhoenixBases.GeoPhoenixBaseTemplate;

namespace TFTV
{
    internal class TFTVExperimental
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        private static bool _usingEchoHead = false;

        [HarmonyPatch(typeof(TacticalActor), "GetDefaultShootAbility")]
        public static class TFTV_TacticalActor_GetDefaultShootAbility
        {
            public static void Postfix(TacticalActor __instance, ref ShootAbility __result)
            {
                try
                {
                    ShootAbilityDef stabilityTaurusShootAbilityDef = (ShootAbilityDef)Repo.GetDef("76ae9352-1343-4b95-964c-036341b6a0eb");
                    ShootAbilityDef stabilityMissileShootAbilityDef = (ShootAbilityDef)Repo.GetDef("df2e83d1-8688-4e47-8559-cc6a9f9906d1");

                    if (__instance.GetAbilityWithDef<ShootAbility>(stabilityTaurusShootAbilityDef) != null) 
                    {
                        __result = __instance.GetAbilityWithDef<ShootAbility>(stabilityTaurusShootAbilityDef);
                    }
                    else if (__instance.GetAbilityWithDef<ShootAbility>(stabilityMissileShootAbilityDef) != null)
                    {
                        __result = __instance.GetAbilityWithDef<ShootAbility>(stabilityMissileShootAbilityDef);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(TacContextHelpManager), "OnStatusApplied")]
        public static class TFTV_TacContextHelpManager_OnApply
        {
            public static bool Prefix(TacContextHelpManager __instance, Status status)
            {
                try
                {               
                    if (!(status is TacStatus tacStatus) || tacStatus.StatusComponent == null)
                    {
                        TFTVLogger.Always($"status null! returning to avoid softlock");
                        return false;
                    }

                    return true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(SlotStateStatus), "OnApply")]
        public static class TFTV_SlotStateStatus_OnApply
        {
            public static bool Prefix(SlotStateStatus __instance, StatusComponent statusComponent)
            {
                try
                {
               
                    if (__instance.Applied)
                    {
                        return true;
                    }

                    string targetSlotName = TacUtil.GetStatusTargetSlotName(__instance);

                    if (targetSlotName.IsNullOrEmpty())
                    {
                        TFTVLogger.Always($"{__instance.SlotStateStatusDef.name} status failed to apply to! Target slot not supplied! TFTV Kludge to prevent Softlock");
                        return false;
                    }

                    return true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(TacticalContribution), "AddContribution")]
        public static class TFTV_TacticalContribution_AddContribution
        {
            public static void Postfix(TacticalContribution __instance, int cp, TacticalActorBase ____actor)
            {
                try
                {

                    if (cp <= 0)
                    {
                        return;
                    }

                    if (!____actor.Status.HasStatus<MindControlStatus>() || ____actor.Status.GetStatus<MindControlStatus>().ControllerActor == null)
                    {
                        return;
                    }

                    TacticalActor controllingActor = ____actor.Status.GetStatus<MindControlStatus>().ControllerActor;

                    // TFTVLogger.Always($"{controllingActor.name} has {controllingActor.Contribution.Contribution} CP");

                    FieldInfo contributionFieldInfo = typeof(TacticalContribution).GetField("_contribution", BindingFlags.NonPublic | BindingFlags.Instance);

                    TacticalContribution controllingActorContribution = controllingActor.Contribution;

                    int controllingActorContributionValue = controllingActorContribution.Contribution + cp / 2;

                    contributionFieldInfo.SetValue(controllingActorContribution, controllingActorContributionValue);

                    // TFTVLogger.Always($"{controllingActor.name} now has {controllingActor.Contribution.Contribution} CP");

                    Debug.Log($"+{cp} cp for {controllingActor.name} (through Mind Controlled Unit).");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalItem), "SetToDisabled")]
        public static class TFTV_TacticalItem_SetToDisabled
        {
            public static void Prefix(TacticalItem __instance)
            {
                try
                {
                    TacticalActor tacActor = __instance.TacticalActor;

                    GroundVehicleWeaponDef meph = (GroundVehicleWeaponDef)Repo.GetDef("49723d28-b373-3bc4-7918-21e87a72c585");
                    GroundVehicleWeaponDef obliterator = (GroundVehicleWeaponDef)Repo.GetDef("ffb34012-b1fd-4b24-8236-ba2eb23db0b7");

                    if (__instance.ItemDef == obliterator && tacActor != null)
                    {
                        TFTVLogger.Always($"it's the obliterator");

                        if (tacActor.Equipments.Equipments.Any(e => e.ItemDef == meph && e.Enabled))
                        {
                            TFTVLogger.Always($"Obliterator destroyed, removing meph");
                            tacActor.Equipments.RemoveItem(meph).Destroy();
                            TFTVLogger.Always($"should be destroyed");
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(Addon), "Destroy")]
        public static class TFTV_Addon_RemoveItem
        {
            public static bool Prefix(Addon __instance, ref List<Addon> __result)
            {
                try
                {
                    if (__instance == null)
                    {
                        __result = new List<Addon>();

                        return false;
                    }
                    return true;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(InventoryComponent), "RemoveItem", new Type[] { typeof(ItemDef) })]
        public static class TFTV_InventoryComponent_RemoveItem
        {
            public static bool Prefix(InventoryComponent __instance, ItemDef itemDef, ref Item __result)
            {
                try
                {
                    Item item = __instance.GetItem(itemDef);

                    TFTVLogger.Always($"item null? {item == null} {item?.ItemDef?.name}");

                    if (item != null)
                    {
                        __instance.RemoveItem(item);
                    }

                    __result = item;

                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalAbility), "get_EquipmentWithTags")]
        public static class TFTV_TacticalAbility_get_EquipmentWithTags
        {
            public static void Postfix(TacticalAbility __instance, ref Equipment __result)
            {
                try
                {
                    if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                    {
                        if (__instance.SelectedEquipment!=null && __instance.SelectedEquipment.GameTags.Contains(DefCache.GetDef<GameTagDef>("SilencedWeapon_TagDef")))
                        {
                            __result = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(ShootAbility), "Activate")]
        public static class TFTV_ShootAbility_Activate
        {
            public static void Prefix(ShootAbility __instance)
            {
                try
                {
                    if (__instance.TacticalAbilityDef == DefCache.GetDef<ShootAbilityDef>("EchoHead_ShootAbilityDef"))
                    {
                        _usingEchoHead = true;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(Weapon), "IsAttackSilent")]
        public static class TFTV_Weapon_IsAttackSilent
        {
            public static void Postfix(Weapon __instance, ref bool __result)
            {
                try
                {
                    if (_usingEchoHead)
                    {
                        __result = true;
                        _usingEchoHead = false;
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(TacticalFactionVision), "LocateRandomEnemyIfNeeded")]
        public static class TFTV_TacticalFactionVision_LocateRandomEnemyIfNeeded
        {
            public static bool Prefix(TacticalFactionVision __instance)
            {
                try
                {
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        public static void CheckSquashing(TacticalAbility ability, TacticalActor tacticalActor)
        {
            try
            {

                if (ability == null || tacticalActor == null)
                {
                    return;
                }


                if (ability is CaterpillarMoveAbility caterpillarMoveAbility
                    && caterpillarMoveAbility.TacticalDemolition != null && caterpillarMoveAbility.TacticalDemolition.TacticalDemolitionComponentDef.DemolitionBodyShape == TacticalDemolitionComponentDef.DemolitionShape.Rectangle)
                {

                }
                else
                {
                    return;
                }


                Vector3 direction = tacticalActor.NavigationComponent.Direction;

                Vector3 frontcentre = tacticalActor.transform.position + direction;

                Type type = typeof(CaterpillarMoveAbility);

                // Get the field info for _actorBases
                FieldInfo fieldInfo = type.GetField("_actorBases", BindingFlags.NonPublic | BindingFlags.Instance);

                // Get the value of _actorBases
                HashSet<TacticalActorBase> actorBases = (HashSet<TacticalActorBase>)fieldInfo.GetValue(caterpillarMoveAbility);

                List<TacticalActor> nearbyActors = tacticalActor.TacticalLevel.Map.GetActors<TacticalActor>().
                    Where(a => a.IsAlive && a.HasGameTag(DefCache.GetDef<GameTagDef>("DamageByCaterpillarTracks_TagDef"))
                    && (a.Pos - frontcentre).sqrMagnitude < 1)
                    .ToList();

                foreach (TacticalActor actor in nearbyActors)
                {
                    if (actorBases.Contains(actor))
                    {
                        continue;
                    }

                    TFTVLogger.Always($"squashing {actor.name}");

                    ApplyDamageEffectAbility ability1 = actor.GetAbility<ApplyDamageEffectAbility>();

                    if (ability1 != null)
                    {
                        ability1.Activate();
                        actorBases.Add(actor);
                        fieldInfo.SetValue(caterpillarMoveAbility, actorBases);
                    }
                    else
                    {
                        actor.Health.SetToMin();
                        actorBases.Add(actor);
                        fieldInfo.SetValue(caterpillarMoveAbility, actorBases);
                    }
                }
            }

            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }








        /* 

         [HarmonyPatch(typeof(TacticalFactionVision), "IncrementKnownCounterToAll")]
         public static class TFTV_TacticalFactionVision_IncrementKnownCounterToAll
         {
             public static void Prefix(TacticalFactionVision __instance, TacticalActorBase actor, KnownState type, int counterValue, bool notifyChange)
             {
                 try
                 {
                     TFTVLogger.Always($"IncrementKnownCounterToAll run {actor.DisplayName}, {type}, {counterValue}, {notifyChange}");

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }
 */

        /* [HarmonyPatch(typeof(CaptureActorResearchRequirement), "IsValidUnit", typeof(GeoUnitDescriptor))]
         public static class TFTV_CaptureActorResearchRequirement_IsValidUnit
         {
             public static void Postfix(CaptureActorResearchRequirement __instance, GeoUnitDescriptor unit, bool __result)
             {
                 try
                 {
                     TFTVLogger.Always($"{__instance.CaptureRequirementDef.name} for {unit.GetName()}, valid? {__result}");


                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /* [HarmonyPatch(typeof(MoveAbility), "GetTargetDataFor")]
         public static class TFTV_MoveAbility_GetTargetDataFor
         {
             public static void Prefix(MoveAbility __instance, TacticalPathRequest pathRequest)
             {
                 try
                 {
                     TFTVLogger.Always($"MoveAbility.GetTargetDataFor {__instance.TacticalActor.DisplayName}, pathRequest null? {pathRequest==null}. " +
                         $"Controlled by player? {__instance.TacticalActor.IsControlledByPlayer}. Is vehicle? {__instance.TacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag)}");

                     if (pathRequest != null && __instance.TacticalActor.IsControlledByPlayer && __instance.TacticalActor.HasGameTag(Shared.SharedGameTags.VehicleTag))
                     {
                         TacticalNavigationComponent component = __instance.TacticalActor.TacticalNav;
                         component.CurrentPath = component.CreatePathRequest();

                         TFTVLogger.Always($"Creating path request for {__instance.TacticalActor.DisplayName}");
                     }
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        public static void CheckActorReserchRequirement()
        {
            try
            {

                TacticalActorDef actorDef = DefCache.GetDef<TacticalActorDef>("Crabman_ActorDef");
                TacticalActorDef actorDef2 = DefCache.GetDef<TacticalActorDef>("Siren_ActorDef");
                GameTagDef tagRequirement = DefCache.GetDef<GameTagDef>("ViralBodypart_TagDef");
                TacCharacterDef tacCharacterDef = DefCache.GetDef<TacCharacterDef>("Crabman39_EliteViralCommando_AlienMutationVariationDef");
                TacCharacterDef tacCharacterDef2 = DefCache.GetDef<TacCharacterDef>("Siren3_InjectorBuffer_AlienMutationVariationDef");
                IEnumerable<TacticalItemDef> bodyparts = tacCharacterDef.GetTemplateBodyparts();
                IEnumerable<TacticalItemDef> bodyparts2 = tacCharacterDef2.GetTemplateBodyparts();


                bool valid = ActorResearchRequirementDef.IsValidActorForTag(actorDef, bodyparts, null, tagRequirement);
                bool valid2 = ActorResearchRequirementDef.IsValidActorForTag(actorDef2, bodyparts2, null, tagRequirement);

                TFTVLogger.Always($"is {actorDef.name} valid for {tagRequirement.name}? {valid}");
                TFTVLogger.Always($"is {actorDef2.name} valid for {tagRequirement.name}? {valid2}");


            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        /*   bool IsValidActorForTag(TacticalActorDef actorDef, IEnumerable<TacticalItemDef> bodyparts, TacticalActorDef actorRequirement, GameTagDef tagRequirement)

          // CaptureActorResearchRequirement

                 [HarmonyPatch(typeof(ActorResearchRequirementDef), "GetDisabledStateText", typeof(GeoAbilityTarget))]
           public static class TFTV_ActorResearchRequirementDef_GetDisabledStateText
           {
               public static void Postfix(ActorResearchRequirementDef __instance, ref string __result)
               {
                   try
                   {
                       if (__instance.GeoAbility is LaunchBehemothMissionAbility)
                       {
                           __result = "Behemoth is submerged!";
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/


        /*    public void WireResearchEvents()
        {
            GameUtl.GameComponent<DefRepository>().GetAllDefs<ResearchDbDef>();
            IEnumerable<ResearchElement> completed = this.Completed;
            foreach (ResearchElement researchElement in this.AllResearchesArray.GetEnumerator<ResearchElement>())
            {
                bool flag = true;
                string[] invalidatedBy = researchElement.ResearchDef.InvalidatedBy;
                for (int i = 0; i < invalidatedBy.Length; i++)
                {
                    string invalidateID = invalidatedBy[i];
                    if (completed.Any((ResearchElement r) => string.Equals(r.ResearchID, invalidateID, StringComparison.OrdinalIgnoreCase)))
                    {
                        flag = false;
                        researchElement.State = ResearchState.Hidden;
                        break;
                    }
                }
                if (flag)
                {
                    researchElement.InitializeRequirements(researchElement.ResearchDef.RequirementsDefs);
                }
            }
        }*/

        [HarmonyPatch(typeof(GeoAbilityView), "GetDisabledStateText", typeof(GeoAbilityTarget))]
        public static class TFTV_GeoAbilityView_GetDisabledStateText
        {
            public static void Postfix(GeoAbilityView __instance, ref string __result)
            {
                try
                {
                    if (__instance.GeoAbility is LaunchBehemothMissionAbility)
                    {
                        __result = "Behemoth is submerged!";
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        [HarmonyPatch(typeof(ItemManufacturing), "FinishManufactureItem")]
        public static class TFTV_ItemManufacturing_FinishManufactureItem
        {
            public static void Postfix(ItemManufacturing __instance, ManufactureQueueItem element)
            {
                try
                {
                    //  TFTVLogger.Always($"{element.ManufacturableItem.Name}, {element.ManufacturableItem.RelatedItemDef.name}");


                    if (element.ManufacturableItem.RelatedItemDef.name.Equals("PP_MaskedManticore_VehicleItemDef"))
                    {
                        TFTVHints.GeoscapeHints.TriggerBehemothMissionHint(GameUtl.CurrentLevel().GetComponent<GeoLevelController>());

                    }

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }






        [HarmonyPatch(typeof(LaunchBehemothMissionAbility), "GetDisabledStateInternal")]
        public static class TFTV_LaunchBehemothMissionAbility_GetDisabledStateInternal_patch
        {

            public static void Postfix(LaunchBehemothMissionAbility __instance, ref GeoAbilityDisabledState __result)
            {
                try
                {
                    if (__instance.GeoLevel.AlienFaction.Behemoth == null || __instance.GeoLevel.AlienFaction.Behemoth != null &&
                        (__instance.GeoLevel.AlienFaction.Behemoth.CurrentBehemothStatus == GeoBehemothActor.BehemothStatus.Dormant))
                    {
                        __result = GeoAbilityDisabledState.RequirementsNotMet;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        /*   [HarmonyPatch(typeof(BreachEntrance), "OnAllPlotParcelsLoaded")]
           public static class TFTV_BreachEntrance_OnAllPlotParcelsLoaded_patch
           {

               public static void Prefix(BreachEntrance __instance, List<Transform> parcels, Transform myParcel, ref MapPlot plot)
               {
                   try
                   {                 
                       plot.RemainingBreachPoints = 1;
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/


        [HarmonyPatch(typeof(LaunchBehemothMissionAbility), "ActivateInternal")]
        public static class TFTV_LaunchBehemothMissionAbility_ActivateInternal_patch
        {

            public static void Prefix(LaunchBehemothMissionAbility __instance)
            {
                try
                {
                    TFTVHints.GeoscapeHints.TriggerBehemothDeployHint(__instance.GeoLevel);
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(GeoPhoenixBaseTemplate), "CreateBaseLayout")]
        public static class TFTV_GeoPhoenixBaseTemplate_CreateBaseLayout_patch
        {

            public static bool Prefix(GeoPhoenixBaseTemplate __instance, ref GeoPhoenixBaseLayout __result, GeoPhoenixBaseLayoutDef layoutDef, int randomSeed)
            {
                try
                {
                    PhoenixFacilityDef accessLift = DefCache.GetDef<PhoenixFacilityDef>("AccessLift_PhoenixFacilityDef");


                    UnityEngine.Random.InitState(randomSeed);
                    PhoenixBaseLayoutEdge entrance = PhoenixBaseLayoutEdge.Bottom;
                    GeoPhoenixBaseLayout geoPhoenixBaseLayout = new GeoPhoenixBaseLayout(layoutDef, entrance);
                    Vector2Int randomElement = geoPhoenixBaseLayout.GetBuildableTiles().GetRandomElement();
                    geoPhoenixBaseLayout.PlaceBaseEntrance(randomElement);
                    IList<PhoenixFacilityData> list = __instance.FacilityData.ToList().Shuffle();
                    List<Vector2Int> list2 = new List<Vector2Int>();

                    int facilitiesCount = list.Count;


                    while (list.Count > 0)
                    {
                        PhoenixFacilityData phoenixFacilityData = null;

                        if (list.Count == list.Count - 2) //place hangar 3rd
                        {
                            IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef.Size == 2);
                            phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());

                        }
                        else if (list.Count == list.Count - 4) //place access lift with at least one space to hangar
                        {
                            IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef.Size == 1 && f.FacilityDef == accessLift);
                            phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());

                        }
                        else //place anything except access lift or hangar in between
                        {
                            IEnumerable<PhoenixFacilityData> source = list.Where((PhoenixFacilityData f) => f.FacilityDef.Size == 1 && f.FacilityDef != accessLift);
                            phoenixFacilityData = ((!source.Any()) ? list.Last() : source.First());
                        }

                        list2.Clear();
                        geoPhoenixBaseLayout.GetBuildableTilesForFacility(phoenixFacilityData.FacilityDef, list2);
                        if (list2.Count == 0)
                        {
                            Debug.LogError("No tiles available for placing facility " + phoenixFacilityData.FacilityDef.name + "!");
                        }
                        else
                        {
                            GeoPhoenixFacility geoPhoenixFacility = geoPhoenixBaseLayout.PlaceFacility(position: list2.GetRandomElement(), facilityDef: phoenixFacilityData.FacilityDef, dontPlaceCorridors: false);
                            if (__instance.ApplyFullDamageOnFacilities)
                            {
                                geoPhoenixFacility.ApplyFullDamageOnFacility();
                            }
                            else if (phoenixFacilityData.StartingHealth < 100)
                            {
                                geoPhoenixFacility.DamageFacility(100 - phoenixFacilityData.StartingHealth);
                            }
                        }

                        if (0 == 0)
                        {
                            list.Remove(phoenixFacilityData);
                        }
                    }

                    int num = __instance.BlockedTiles.RandomValue();
                    for (int i = 0; i < num; i++)
                    {
                        ICollection<Vector2Int> rockPlacableTiles = geoPhoenixBaseLayout.GetRockPlacableTiles();
                        if (rockPlacableTiles.Count() == 0)
                        {
                            Debug.LogWarning("No place left to place rock in phoenix base!");
                            break;
                        }

                        Vector2Int randomElement3 = rockPlacableTiles.GetRandomElement();
                        geoPhoenixBaseLayout.PlaceRock(randomElement3);
                    }

                    __result = geoPhoenixBaseLayout;

                    return false;
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }

        //.GetDamageModifierForDistance

        // AmbushOutcomeDataBind

        //GeoMission



        /*   [HarmonyPatch(typeof(GeoscapeTutorial), "Map_SiteInspectedChanged")]
           public static class TFTV_GeoscapeTutorial_Map_SiteInspectedChanged_patch
           {

               public static void Postfix(GeoscapeTutorial __instance, GeoSite inspectedSite, GeoFaction inspector)
               {
                   try
                   {
                      // if(inspectedSite.) { }


                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }


           public static void GeoTutorialStepTest(GeoLevelController controller)
           {
               try 
               {
                   UIModuleTutorialModal uIModuleTutorial = controller.View.GeoscapeModules.TutorialModule;

                   controller.View.MainUILayer.SetActiveState("");

                  uIModuleTutorial.



               }
               catch (Exception e)
               {
                   TFTVLogger.Error(e);
                   throw;
               }
           }*/





        //EffectDef.ConditionsMet
        /*     [HarmonyPatch(typeof(OptionsComponent), "StartSaveOptions")]
             public static class OptionsComponent_StartSaveOptions_patch
         {

             public static void Prefix(OptionsComponent __instance)
             {
                 try
                 {
                     TFTVLogger.Always($"looking at template {__instance.name}");


                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/




        /* [HarmonyPatch(typeof(SquadMemberPortraitController), "SetSoldierPortrait")]
         public static class SquadMemberPortraitController_SetSoldierPortrait_patch
         {

             public static bool Prefix(SquadMemberPortraitController __instance, SquadMemberScrollerController.PortraitSprites portraitSprites)
             {
                 try
                 {
                     if (Application.platform == RuntimePlatform.OSXPlayer ||
                  Application.platform == RuntimePlatform.OSXEditor)
                     {
                         //TFTVLogger.Always($"Unholy Mac detected!");
                         Image[] background = __instance.SoldierPortraitImages.Background;
                         for (int i = 0; i < background.Length; i++)
                         {
                             //  background[i].sprite = portraitSprites.Background;
                             background[i].material = null;

                         }

                         __instance.UpdatePortrait(portraitSprites);
                         background = __instance.SoldierPortraitImages.Foreground;
                         foreach (Image image in background)
                         {
                             BasicTween component = image.GetComponent<BasicTween>();
                             image.sprite = null;
                             image.color = Color.clear;
                             if (__instance.Actor.IsAlive)
                             {
                                 if (!(component == null))
                                 {
                                     if (Utl.LesserThan(__instance.Actor.Health.Ratio, __instance.LowHealthRatio))
                                     {
                                         image.sprite = __instance.LowHealthSprite;
                                         component.StartTween();
                                     }
                                     else
                                     {
                                         component.StopTween();
                                     }
                                 }
                             }
                             else
                             {
                                 if (component != null)
                                 {
                                     component.StopTween();
                                 }

                                 image.sprite = __instance.DeathSprite;
                                 image.color = __instance.DeathColor;
                             }
                         }

                         background = __instance.SoldierPortraitImages.CorruptionEffect;
                         foreach (Image obj in background)
                         {
                             bool active = __instance.Actor.Status.HasStatus<CorruptionStatus>();
                             obj.gameObject.SetActive(active);
                         }

                         return false;

                     }
                     return true;
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/




        /* [HarmonyPatch(typeof(UIInventorySlotSideButton), "OnSideButtonPressed")]
         public static class UIInventorySlotSideButton_OnSideButtonPressed_patch
         {

             public static void Prefix(UIInventorySlotSideButton __instance, GeneralState ____currentState, UIModuleSoldierEquip ____parentModule, ItemDef ____itemToProduce)
             {
                 try
                 {
                     ICommonItem commonItem = null;
                     TFTVLogger.Always($"OnSideButtonPressed, current state {____currentState.State}, action {____currentState.Action}");

                     TFTVLogger.Always($"parent module Storage list is {____parentModule.StorageList} and the count of Unfiltered items is " +
                         $"{____parentModule.StorageList.UnfilteredItems.Count}");

                     TFTVLogger.Always($"item to produce {____itemToProduce.name}");

                     commonItem = ____parentModule.StorageList.UnfilteredItems.Where((ICommonItem item) => item.ItemDef == ____itemToProduce).FirstOrDefault().GetSingleItem();
                     TFTVLogger.Always($"common item null? {commonItem==null}");

                    // commonItem = _parentModule.StorageList.UnfilteredItems.Where((ICommonItem item) => item.ItemDef == _itemToProduce).FirstOrDefault().GetSingleItem();

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/

        /*  GeoPhoenixFaction

               private void OnMaxDiplomacyStateChanged(GeoFaction faction, GeoFaction towards, PartyDiplomacyState state, PartyDiplomacyState previousState)
          {
              PhoenixDiplomacySettings diplomacySetting = FactionDef.FactionsDiplomacySettings.FirstOrDefault((PhoenixDiplomacySettings s) => s.FactionDef == faction.Def.PPFactionDef);
              if (diplomacySetting != null)
              {
                  PhoenixDiplomacySettings.DiplomacyStateSettings diplomacyStateSettings = diplomacySetting.StateSettings.FirstOrDefault((PhoenixDiplomacySettings.DiplomacyStateSettings s) => s.State == diplomacySetting.StartingState);
                  if (diplomacyStateSettings?.Tag != null && !base.GameTags.Contains(diplomacyStateSettings.Tag))
                  {
                      AddTag(diplomacyStateSettings.Tag);
                  }
              }

              _level.AchievmentTracker.CheckDiplomacyProgress(state, faction);
          }


          protected void ShareResearchWithAllies(ResearchElement research)
          {
              if (!Def.CanShareResearch || !(research.ResearchDef.Faction == Def))
              {
                  return;
              }

              foreach (GeoFaction item in GetFactionsWithMinDiplomacyState(PartyDiplomacyState.Allied))
              {
                  if (item.Research != null && research.IsAvailableToFaction(item) && !item.Research.Completed.Any((ResearchElement r) => r.ResearchID == research.ResearchID))
                  {
                      item.Research.GiveResearch(research);
                      Research.SendOnResearchObtainedTelemetricsEvent(research.ResearchDef, item, "Diplomacy");
                  }
              }
          }


          */

        //  internal static Color purple = new Color32(149, 23, 151, 255);




        /*    [HarmonyPatch(typeof(TacCharacterDef), "ApplyPogression")]
            public static class GeoMission_ApplyPogression_patch
            {

                public static void Prefix(TacCharacterDef __instance, TacCharacterData data)
                {
                    try
                    {
                        TFTVLogger.Always($"looking at template {__instance.name}");

                        SpecializationDef specializationDef = GameUtl.GameComponent<DefRepository>().GetAllDefs<SpecializationDef>().FirstOrDefault((SpecializationDef s) => s.ClassTag == __instance.ClassTag);

                        TFTVLogger.Always($"specializationDef is {specializationDef.name}");

                        if (!(specializationDef == null))
                        {

                            List<TacticalAbilityDef> second = specializationDef.GetAbilitiesTillLevel(data.LevelProgression.Level).ToList();
                            TFTVLogger.Always($"second count is {second.Count}");
                            data.Abilites = data.Abilites.Concat(second).Distinct().ToArray();
                        }

                    }

                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/
        /*  [HarmonyPatch(typeof(GeoMission), "DistributeActorDeploymentWeight")]
          public static class GeoMission_DistributeActorDeploymentWeight_patch
          {

              public static bool Prefix(GeoMission __instance, List<IDeployableUnit> units, List<MissionDeployParams> deployLimits, TacMissionTypeParticipantData.DeploymentRuleData deploymentRule, ref List<ActorDeployData> __result)
              {
                  try
                  {
                      TFTVLogger.Always($"Running DistributeActorDeploymentWeight");

                      Dictionary<ClassTagDef, List<IDeployableUnit>> dictionary = new Dictionary<ClassTagDef, List<IDeployableUnit>>();
                      Dictionary<ClassTagDef, float> dictionary2 = new Dictionary<ClassTagDef, float>();
                      foreach (IDeployableUnit unit2 in units)
                      {
                          if (unit2.ClassTag == null)
                          {
                              Debug.LogError($"Unit '{unit2}' does not have a ClassTagDef, skipping!");
                              continue;
                          }

                          foreach (ClassTagDef classTag in unit2.ClassTags)
                          {
                              if (!dictionary.ContainsKey(classTag))
                              {
                                  dictionary.Add(classTag, new List<IDeployableUnit>());
                              }
                              TFTVLogger.Always($"adding class {classTag.name}");
                              dictionary[classTag].Add(unit2);
                          }
                      }

                      TFTVLogger.Always($"got here; deployLimits count: {deployLimits.Count()}");

                      foreach (MissionDeployParams deployLimit in deployLimits)
                      {
                          ClassTagDef actorTag = deployLimit.Limit.ActorTag;

                          TFTVLogger.Always($"actorTag is {actorTag.name}");                  
                      }


                      foreach (MissionDeployParams deployLimit in deployLimits)
                      {
                          ClassTagDef actorTag = deployLimit.Limit.ActorTag;

                          TFTVLogger.Always($"actorTag is {actorTag.name}");

                          dictionary.TryGetValue(actorTag, out var value);
                          int num = value?.Count ?? 1;
                          dictionary2.Add(actorTag, deployLimit.Weight / (float)num);
                      }

                      TFTVLogger.Always($"got here2");
                      List<ActorDeployData> list = new List<ActorDeployData>();
                      foreach (IDeployableUnit unit in units)
                      {
                          float num2 = 0f;
                          foreach (ClassTagDef classTag2 in unit.ClassTags)
                          {
                              dictionary2.TryGetValue(classTag2, out var value2);
                              num2 = Mathf.Max(num2, value2);
                          }

                          ActorDeployData actorDeployData = unit.GenerateActorDeployData();
                          TacMissionTypeParticipantData.DeploymentRuleData.UnitDeploymentOverride unitDeploymentOverride = deploymentRule.OverrideUnitDeployment.FirstOrDefault((TacMissionTypeParticipantData.DeploymentRuleData.UnitDeploymentOverride t) => unit.ClassTags.Contains(t.ClassTag));
                          if (unitDeploymentOverride != null)
                          {
                              actorDeployData.DeployCost = unitDeploymentOverride.OverrideDeployment;
                          }

                          if (!actorDeployData.Unique)
                          {
                              actorDeployData.ChanceWeight = num2;
                          }

                          list.Add(actorDeployData);
                      }

                      __result = list;

                      TFTVLogger.Always($"result count: {__result.Count}");

                      return false;


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /*  [HarmonyPatch(typeof(EncounterVarResearchReward), "GiveReward")]
          public static class EncounterVarResearchReward_GiveReward_patch
          {

              public static void Postfix(EncounterVarResearchReward __instance, GeoFaction faction)
              {
                  try
                  {
                      EncounterVarResearchRewardDef def = __instance.BaseDef as EncounterVarResearchRewardDef;

                      TFTVLogger.Always($"{def.name} {def.VariableName} {faction.Name.Localize()}");
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/












        /*   [HarmonyPatch(typeof(GeoPhoenixpedia), "AddItemEntry")]
           public static class GeoPhoenixpedia_ProcessGeoscapeInstanceData_patch
           {

               public static void Prefix(GeoPhoenixpedia __instance, ItemDef item)
               {
                   try
                   {
                       TFTVLogger.Always($"Running GeoPhoenixpedia.AddItemEntry");
                       TFTVLogger.Always($"{item}");


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/







        //   









        /*   [HarmonyPatch(typeof(EnterBaseAbility), "GetTargetDisabledStateInternal")]
           public static class EnterBaseAbility_GetValidTargets_patch
           {

               public static void Postfix(EnterBaseAbility __instance, GeoAbilityTarget target, GeoAbilityTargetDisabledState __result)
               {
                   try
                   {
                       TFTVLogger.Always($"GetTargetDisabledStateInternal for ability {__instance.GeoscapeAbilityDef.name}");


                       if (__result==GeoAbilityTargetDisabledState.NotDisabled && target.Actor is GeoSite site && site.ActiveMission != null && site.CharactersCount > 0)
                       {
                           UIModuleSiteContextualMenu uIModuleSiteContextualMenu = __instance.GeoLevel.View.GeoscapeModules.SiteContextualMenuModule;


                           FieldInfo fieldInfoListSiteContextualMenuItem = typeof(UIModuleSiteContextualMenu).GetField("_menuItems", BindingFlags.NonPublic | BindingFlags.Instance);
                           List<SiteContextualMenuItem> menuItems = fieldInfoListSiteContextualMenuItem.GetValue(uIModuleSiteContextualMenu) as List<SiteContextualMenuItem>;

                           foreach(SiteContextualMenuItem menuItem in menuItems) 
                           { 
                           if(menuItem.ItemText.text == __instance.View.ViewElementDef.DisplayName1.Localize()) 
                               {
                                   menuItem.ItemText.text = "DEPLOY TO DEFEND BASE";


                               }

                           }


                       }


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/





        /*  public static int KludgeStartingWeight = 0;
          public static int KludgeCurrentWeight = 0;


          [HarmonyPatch(typeof(UIStateInventory), "RefreshUI")]
          public static class UIStateInventory_RefreshUI_patch
          {
              public static void Postfix(UIStateInventory __instance, TacticalActor ____secondaryActor)
              {
                  try
                  {
                      TFTVLogger.Always($"RefreshUI");

                      MethodInfo refreshCostMessageMethod = typeof(UIStateInventory).GetMethod("RefreshCostMessage", BindingFlags.Instance | BindingFlags.NonPublic);

                      ApplyStatusAbilityDef rfAAbility = DefCache.GetDef<ApplyStatusAbilityDef>("ReadyForAction_AbilityDef");
                      ChangeAbilitiesCostStatusDef rfAStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_ReadyForActionStatus [ReadyForAction_AbilityDef]");

                      if (KludgeStartingWeight == KludgeCurrentWeight && ____secondaryActor!=null)
                      {                       
                          if (__instance.PrimaryActor.GetAbilityWithDef<ApplyStatusAbility>(rfAAbility) != null && !__instance.PrimaryActor.HasStatus(rfAStatus))
                          {
                              __instance.PrimaryActor.Status.ApplyStatus(rfAStatus);
                              TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA ability but is missing the status, personal inventory case");
                              refreshCostMessageMethod.Invoke(__instance, null);

                          }
                      }
                      else if(KludgeStartingWeight > KludgeCurrentWeight && ____secondaryActor != null && __instance.PrimaryActor.HasStatus(rfAStatus)) 
                      {

                          __instance.PrimaryActor.Status.UnapplyStatus(__instance.PrimaryActor.Status.GetStatusByName(rfAStatus.EffectName));
                          TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA status, but is taking items away from inventory");
                          refreshCostMessageMethod.Invoke(__instance, null);

                      }


                      //  TFTVLogger.Always($"KludgeCurrentWeight is {KludgeCurrentWeight}, will set to 0");
                      //  KludgeCurrentWeight = 0;
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }
        */


        /*    [HarmonyPatch(typeof(UIStateInventory), "InitInventory")]
            public static class UIStateInventory_InitInventory_patch
            {

                public static bool Prefix(UIStateInventory __instance, TacticalActor ____secondaryActor)
                {
                    try
                    {
                        if (____secondaryActor == KludgeActor)
                        {

                            TFTVLogger.Always($"InitInventory prefix {____secondaryActor?.DisplayName}");

                            return false;
                        }
                        else
                        {

                            return true;

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }


            [HarmonyPatch(typeof(UIStateInventory), "InitVehicleInventory")]
            public static class UIStateInventory_InitVehicleInventory_patch
            {

                public static bool Prefix(UIStateInventory __instance, TacticalActor ____secondaryActor)
                {
                    try
                    {
                        if (____secondaryActor == KludgeActor)
                        {

                            TFTVLogger.Always($"InitVehicleInventory prefix {____secondaryActor?.DisplayName}");

                            return false;
                        }
                        else 
                        {

                            return true;

                        }
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }*/

        /*   [HarmonyPatch(typeof(UIStateInventory), "ExitState")]
           public static class UIStateInventory_RefreshCostMessage_patch
           {
               public static void Postfix(UIStateInventory __instance)
               {
                   try
                   {
                      // TFTVLogger.Always($"Exit State");
                       ApplyStatusAbilityDef rfAAbility = DefCache.GetDef<ApplyStatusAbilityDef>("ReadyForAction_AbilityDef");
                       ChangeAbilitiesCostStatusDef rfAStatus = DefCache.GetDef<ChangeAbilitiesCostStatusDef>("E_ReadyForActionStatus [ReadyForAction_AbilityDef]");

                       if (__instance.PrimaryActor.GetAbilityWithDef<ApplyStatusAbility>(rfAAbility) != null && !__instance.PrimaryActor.HasStatus(rfAStatus))
                       {
                           __instance.PrimaryActor.Status.ApplyStatus(rfAStatus);
                           TFTVLogger.Always($"{__instance.PrimaryActor.name} has the RfA ability but is missing the status");
                       }

                    //   KludgeCurrentWeight = 0;
                    //   KludgeStartingWeight = 0;

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*  [HarmonyPatch(typeof(UIModuleSoldierEquip), "GetPrimaryWeight")]
          public static class UIModuleSoldierEquip_GetPrimaryWeight_patch
          {
              public static void Postfix(UIModuleSoldierEquip __instance, int __result)
              {
                  try

                  {
                      TFTVLogger.Always($"GetPrimaryWeight");
                      TFTVLogger.Always($"kludgeWeight is {KludgeStartingWeight} and result is {__result}");

                      if (KludgeStartingWeight != 0)
                      {
                          KludgeCurrentWeight = __result;
                          TFTVLogger.Always($"setting from GetPrimaryWeight KludgeCurrentWeight to {KludgeCurrentWeight}");
                      }
                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/




        /*
        [HarmonyPatch(typeof(UIStateInventory), "EnterState")]
         public static class UIStateInventory_EnterState_patch
         {
             public static void Postfix(UIStateInventory __instance, TacticalActor ____secondaryActor, InventoryComponent ____groundInventory, bool ____isSecondaryVehicleInventory)
             {
                 try
                 {

                     TFTVLogger.Always($"primary actor is {__instance.PrimaryActor.DisplayName}, groundInventory actor is {____groundInventory?.Actor?.name}, is secondary vehicle inventory: {____isSecondaryVehicleInventory}");

                     if(____groundInventory.Actor is TacticalActor actor && actor == ____secondaryActor) 
                     {
                        MethodInfo methodCreateGroundInventory = typeof(UIStateInventory).GetMethod("CreateGroundInventory", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodResetInventoryQueries = typeof(UIStateInventory).GetMethod("ResetInventoryQueries", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodRefreshStorageLabel = typeof(UIStateInventory).GetMethod("RefreshStorageLabel", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodInitInitialItems = typeof(UIStateInventory).GetMethod("InitInitialItems", BindingFlags.Instance | BindingFlags.NonPublic);

                        MethodInfo methodSetupGroundMarkers = typeof(UIStateInventory).GetMethod("SetupGroundMarkers", BindingFlags.Instance | BindingFlags.NonPublic);


                        ____groundInventory = (InventoryComponent)methodCreateGroundInventory.Invoke(__instance, null);

                        methodResetInventoryQueries.Invoke(__instance, null);
                        methodSetupGroundMarkers.Invoke(__instance, null);
                        methodRefreshStorageLabel.Invoke(__instance, null);
                        methodInitInitialItems.Invoke(__instance, null);


                        //
                        // __instance.ResetInventoryQueries();
                        TFTVLogger.Always($"ground inventory set active to false");

                     }

                     TFTVLogger.Always($"{__instance.PrimaryActor.GetAbility<InventoryAbility>()?.TacticalAbilityDef?.name}");

                     foreach (TacticalAbilityTarget target in __instance.PrimaryActor.GetAbility<InventoryAbility>().GetTargets())
                     {
                         InventoryComponent inventoryComponent = target.InventoryComponent;

                         TFTVLogger.Always($"inventory component {inventoryComponent?.name}");

                         if (inventoryComponent.GetType() != typeof(EquipmentComponent))
                         {
                             TFTVLogger.Always($" {inventoryComponent?.name} is no equipmentComponent");

                             TacticalActor tacticalActor = inventoryComponent.Actor as TacticalActor;
                             if (tacticalActor != null && TacUtil.CanTradeWith(__instance.PrimaryActor, tacticalActor))
                             {
                                 TFTVLogger.Always($"{__instance.PrimaryActor.DisplayName} can trade with {tacticalActor.DisplayName}");
                                 InventoryAbility ability = tacticalActor.GetAbility<InventoryAbility>();
                                 if (ability != null && !(ability.GetDisabledState() != AbilityDisabledState.NotDisabled))
                                 {
                                     TFTVLogger.Always($"{tacticalActor.DisplayName} inventory ability is not null");
                                 }
                             }


                         }
                     }


                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }
        */











        /*   [HarmonyPatch(typeof(UIModuleWeaponSelection), "HandleEquipments")]
            public static class UIModuleWeaponSelection_HandleEquipments_patch
           {
               public static void Postfix(UIModuleWeaponSelection __instance, Equipment ____selectedEquipment)
               {
                   try
                   {
                       EquipmentDef repairKit = DefCache.GetDef<EquipmentDef>("FieldRepairKit_EquipmentDef");

                       if (____selectedEquipment.EquipmentDef==repairKit) 
                       {
                           TFTVLogger.Always("");
                           __instance.DamageTypeVisualsTemplate.DamageTypeIcon.gameObject.SetActive(false);
                           __instance.DamageTypeVisualsTemplate.DamageText.gameObject.SetActive(false);


                       }



                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/





        // UIModuleAbilities




        /* [HarmonyPatch(typeof(TacticalActorBase), "GetDamageMultiplierFor")]
         public static class TacticalActorBase_GetDamageMultiplierFor_patch
         {
             public static void Postfix(TacticalActorBase __instance, ref float __result, DamageTypeBaseEffectDef damageType)
             {
                 try
                 {
                     AcidDamageTypeEffectDef acidDamageTypeEffectDef = DefCache.GetDef<AcidDamageTypeEffectDef>("Acid_DamageOverTimeDamageTypeEffectDef");

                     if (damageType == acidDamageTypeEffectDef)
                     {
                         TFTVLogger.Always($"GetDamageMultiplierFor  {__instance.name} and result is {__result}");
                         __result = 1;
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/


        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetPureDamageBonusFor")]
          public static class DamageAccumulation_GetPureDamageBonusFor_patch
          {
              public static void Postfix(DamageAccumulation __instance, IDamageReceiver target, float __result)
              {
                  try
                  {
                      if (__result != 0)
                      {

                          TFTVLogger.Always($"GetPureDamageBonusFor {target.GetDisplayName()}, result is {__result}");
                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/





        /*     [HarmonyPatch(typeof(AddStatusDamageKeywordData), "ProcessKeywordDataInternal")]
          public static class AddStatusDamageKeywordData_ProcessKeywordDataInternal_Patch
          {
              public static void Postfix(AddStatusDamageKeywordData __instance, DamageAccumulation.TargetData data)
              {
                  try
                  {
                      if (__instance.DamageKeywordDef == Shared.SharedDamageKeywords.AcidKeyword)
                      {
                          TFTVLogger.Always($"target {data.Target.GetSlotName()}");

                          if (data.Target is ItemSlot)
                          {

                              ItemSlot itemSlot = (ItemSlot) data.Target;

                              if (itemSlot.DisplayName == "LEG")
                              {
                                  TacticalActor tacticalActor = data.Target.GetActor() as TacticalActor;

                                  itemSlot = tacticalActor.BodyState.GetSlot("Legs");
                                  TFTVLogger.Always($"itemslot name now {itemSlot.GetSlotName()}");
                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }
                              else
                              {
                                  TFTVLogger.Always($"target {data.Target.GetSlotName()} is itemslot {itemSlot.DisplayName}");

                                  TacticalItem tacticalItem = itemSlot.GetAllDirectItems(onlyBodyparts: true).FirstOrDefault();
                                  if (tacticalItem != null && tacticalItem.GameTags.Contains(Shared.SharedGameTags.BionicalTag))
                                  {
                                      TFTVLogger.Always($"Found bionic item {tacticalItem.DisplayName}");
                                      data.Target.GetActor().RemoveAbilitiesFromSource(tacticalItem);
                                      // SlotStateStatusDef source = DefCache.GetDef<SlotStateStatusDef>("DisabledElectronicsAcidSlot_StatusDef");

                                  }
                              }

                          }


                      }

                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }

          */




        /*   [HarmonyPatch(typeof(DamageKeyword), "AddKeywordStatus")]
             public static class DamageOverTimeResistanceStatus_ApplyResistance_Patch
             {
                 public static void Postfix(IDamageReceiver recv, DamageAccumulation.TargetData data, StatusDef statusDef, int value, object customStatusTarget = null)
                 {
                     try
                     {


                       TFTVLogger.Always($"AddKeywordStatus value {value}");


                     }
                     catch (Exception e)
                     {
                         TFTVLogger.Error(e);
                         throw;
                     }
                 }
             }*/




        /* [HarmonyPatch(typeof(DamageAccumulation), "AddTargetStatus")]
         public static class DamageAccumulation_AddTargetStatus_Patch
         {
             public static void Prefix(DamageAccumulation __instance, StatusDef statusDef, int tacStatusValue, IDamageReceiver target)
             {
                 try
                 {


                     if (statusDef == DefCache.GetDef<AcidStatusDef>("Acid_StatusDef"))
                     {




                         TFTVLogger.Always($"tacstatusvalue is {tacStatusValue}");
                     }


                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*  [HarmonyPatch(typeof(DamageOverTimeStatus), "GetDamageMultiplier")]
          public static class DamageOverTimeStatus_GetDamageMultiplier_Patch
          {
              public static void Postfix(DamageOverTimeStatus __instance, ref float __result)
              {
                  try
                  {
                      TFTVLogger.Always($"GetDamageMultiplier for {__instance.DamageOverTimeStatusDef.name} and result is {__result}");

                      AcidStatusDef acidDamage = DefCache.GetDef<AcidStatusDef>("Acid_StatusDef");

                      if (__instance.DamageOverTimeStatusDef == acidDamage) 
                      {
                          TFTVLogger.Always($"dot status acid {__result}");
                          __result = 1;
                          TFTVLogger.Always($"new dot status acid {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /*  [HarmonyPatch(typeof(DamageAccumulation), "GetSourceDamageMultiplier")]
          public static class DamageAccumulation_GetSourceDamageMultiplier_Patch
          {
              public static void Postfix(DamageAccumulation __instance, DamageTypeBaseEffectDef damageType, float __result)
              {
                  try
                  {
                      if (!damageType.name.Equals("Projectile_StandardDamageTypeEffectDef"))
                          {

                          TFTVLogger.Always($"source actor {__instance?.SourceActor?.name} damageType is {damageType.name} and multiplier is {__result}");
                      }


                  }
                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                      throw;
                  }
              }
          }*/

        /* [HarmonyPatch(typeof(Equipment), "SetActive")]
         public static class Equipment_RemoveAbilitiesFromSource_Patch
         {
             public static void Postfix(Equipment __instance, bool active)
             {
                 try
                 {
                     TFTVLogger.Always($"equipment is {__instance.DisplayName}, and is it active? {active}");




                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                     throw;
                 }
             }
         }*/



        /*   [HarmonyPatch(typeof(TacticalItem), "RemoveAbilitiesFromActor")]
           public static class TacticalItem_RemoveAbilitiesFromActor_patch
           {
               public static void Prefix(TacticalItem __instance)
               {
                   try
                   {
                       TFTVLogger.Always($"RemoveAbilitiesFromActor from item {__instance.ItemDef.name}");


                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*   [HarmonyPatch(typeof(ActorComponent), "RemoveAbilitiesFromSource")]
           public static class ActorComponent_RemoveAbilitiesFromSource_patch
           {
               public static void Prefix(ActorComponent __instance, object source)
               {
                   try
                   {
                       TFTVLogger.Always($"RemoveAbilitiesFromSource from {__instance.name} with source {source}");

                       foreach (Ability item in __instance.GetAbilities<Ability>().Where((Ability a) => a.Source == source).ToList())
                       {
                           TFTVLogger.Always($"ability is {item.AbilityDef.name} and it's source is {item.Source}, while parameter source is {source}");
                       }

                   }
                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                       throw;
                   }
               }
           }*/



        /*
                [HarmonyPatch(typeof(SlotStateStatus), "SetAbilitiesState")]
                public static class SlotStateStatus_GetDamageMultiplierFor_patch
                {
                    public static void Prefix(SlotStateStatus __instance, ItemSlot ____targetSlot)
                    {
                        try
                        {
                            TFTVLogger.Always($"Gets at least to here {__instance.Source}");

                            foreach (TacticalItem allDirectItem in ____targetSlot.GetAllDirectItems(onlyBodyparts: true))
                            {
                                if (__instance.SlotStateStatusDef.BodypartsEnabled && !allDirectItem.Enabled)
                                {
                                    TFTVLogger.Always($"landed here: looking at {allDirectItem.ItemDef.name}");
                                }
                                else if (!__instance.SlotStateStatusDef.BodypartsEnabled && allDirectItem.Enabled)
                                {
                                    TFTVLogger.Always($"landed in the else if: looking at {allDirectItem.ItemDef.name}");
                                }


                            }
                        }
                        catch (Exception e)
                        {
                            TFTVLogger.Error(e);
                            throw;
                        }
                    }
                }*/

        /*    [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbilityDef")]
            public static class AbilitySummaryData_ProcessHealAbilityDef_Patch
            {
                public static void Postfix(AbilitySummaryData __instance, HealAbilityDef healAbilityDef)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbilityDef running");
                        if ((bool)healAbilityDef.GeneralHealSummary && healAbilityDef.GeneralHealAmount > 0f)
                        {
                            TFTVLogger.Always($"{healAbilityDef.GeneralHealSummary} and {healAbilityDef.GeneralHealAmount}");

                        }

                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }

            [HarmonyPatch(typeof(AbilitySummaryData), "ProcessHealAbility")]
            public static class AbilitySummaryData_ProcessHealAbility_Patch
            {
                public static void Prefix(AbilitySummaryData __instance, HealAbility healAbility)
                {
                    try
                    {
                        TFTVLogger.Always($"ProcessHealAbility running");
                        TFTVLogger.Always($"Keywords count is {__instance.Keywords.Count}");

                        if (__instance.Keywords.Count() > 0)
                        {
                            KeywordData keywordData = __instance.Keywords.First((KeywordData kd) => kd.Id == "GeneralHeal");

                            if (keywordData == null)
                            {
                                TFTVLogger.Always("somehow null!");
                            }
                        }



                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                        throw;
                    }
                }
            }
        */


        /*   internal virtual void TriggerHurt(DamageResult damageResult)
           {
               var hurtReactionAbility = GetAbility<TacticalHurtReactionAbility>();
               if (IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
               {
                   return;
               }

               bool useModFlinching = true; // Use a global flag for the mod 
               if (useModFlinching && _ragdollDummy != null && _ragdollDummy.CanFlinch)
               {
                   DoTriggerHurt(damageResult, damageResult.forceHurt);
                   return;
               }

               _pendingHurtDamage = damageResult;
               if (_waitingForHurtReactionCrt == null || _waitingForHurtReactionCrt.Stopped)
               {
                   _waitingForHurtReactionCrt = Timing.Start(PollForPendingHurtReaction(damageResult.forceHurt));
               }
           }*/


        /*
        [HarmonyPatch(typeof(TacticalActor), "TriggerHurt")]
        public static class TacticalActor_TriggerHurt_Patch
        {
            public static bool Prefix(TacticalActor __instance, DamageResult damageResult, RagdollDummy ____ragdollDummy, IUpdateable ____waitingForHurtReactionCrt,
                DamageResult ____pendingHurtDamage)
            {
                try
                {


                    MethodInfo doTriggerHurtMethod = typeof(TacticalActor).GetMethod("DoTriggerHurt", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo pollForPendingHurtReaction = typeof(TacticalActor).GetMethod("PollForPendingHurtReaction", BindingFlags.NonPublic | BindingFlags.Instance); 



                 var hurtReactionAbility = __instance.GetAbility<TacticalHurtReactionAbility>();



                    if (__instance.IsDead || (hurtReactionAbility != null && hurtReactionAbility.TacticalHurtReactionAbilityDef.TriggerOnDamage && hurtReactionAbility.IsEnabled(IgnoredAbilityDisabledStatesFilter.IgnoreNoValidTargetsFilter)))
                    {
                        TFTVLogger.Always("Early exit triggers");
                        return true;
                    }

                    bool useModFlinching = true; // Use a global flag for the mod 
                    if (useModFlinching && ____ragdollDummy != null && ____ragdollDummy.CanFlinch)
                    {
                        doTriggerHurtMethod.Invoke(__instance, new object[] { damageResult, damageResult.forceHurt });
                        TFTVLogger.Always("Takes to do trigger hurt method");

                        return false;
                    }

                    ____pendingHurtDamage = damageResult;
                    if (____waitingForHurtReactionCrt == null || ____waitingForHurtReactionCrt.Stopped)
                    {
                        TFTVLogger.Always("waiting for hurt reaction or it is stopped");
                        object[] parameters = new object[] { damageResult.forceHurt };
                        //Timing timingInstance = new Timing();
                        ____waitingForHurtReactionCrt = __instance.Timing.Start((IEnumerator<NextUpdate>)pollForPendingHurtReaction.Invoke(__instance, parameters));

                    }


                    return false;

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }




        [HarmonyPatch(typeof(TacticalActor), "SetFlinchingEnabled")]
        public static class TacticalActor_AddFlinch_Patch
        {
            public static void Postfix(TacticalActor __instance, ref RagdollDummy ____ragdollDummy)
            {
                try
                {
                    TFTVLogger.Always($"SetFlinchingEnabled invoked");
                    ____ragdollDummy.SetFlinchingEnabled(true);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }


        [HarmonyPatch(typeof(RagdollDummy), "AddFlinch")]
        public static class RagdollDummy_AddFlinch_Patch
        {
            public static void Prefix(RagdollDummy __instance, float ____ragdollBlendTimeTotal)
            {
                try
                {
                    TFTVLogger.Always($"AddFlinch invoked prefix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
            public static void Postfix(RagdollDummy __instance, float ____ragdollBlendTimeTotal, Vector3 force, CastHit hit)
            {
                try
                {
                    RagdollDummyDef ragdollDummyDef = DefCache.GetDef<RagdollDummyDef>("Generic_RagdollDummyDef");
                    TFTVLogger.Always($"AddFlinch invoked postfix, ragdollBlendtimeTotal is {____ragdollBlendTimeTotal}. original force is {force}, the hit body part is {hit.Collider?.attachedRigidbody?.name}" +
                        $" mass is {hit.Collider?.attachedRigidbody?.mass}, force applied on first hit is {force*ragdollDummyDef.FlinchForceMultiplier}");







                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }



        [HarmonyPatch(typeof(RagdollDummy), "get_CanFlinch")]
        public static class RagdollDummy_SetFlinchingEnabled_Patch
        {
            public static void Postfix(RagdollDummy __instance, ref bool __result)
            {
                try
                {
                    TFTVLogger.Always($"get_CanFlinch invoked for {__instance?.Actor?.name} and result is {__result}");

                    __result = true;

                    TFTVLogger.Always($"And now result is {__result}");




                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
        */




        /*private bool OnProjectileHit(CastHit hit, Vector3 dir)
        {
            if (hit.Collider.gameObject.CompareTag("WindowPane"))
            {
                return false;
            }

            if (Projectile != null)
            {
                Projectile.OnProjectileHit(hit);
            }

            AffectTarget(hit, dir);
            if (DamagePayload.StopOnFirstHit)
            {
                return true;
            }

            if (DamagePayload.StopWhenNoRemainingDamage)
            {
                DamageAccumulation damageAccum = _damageAccum;
                return damageAccum == null || !damageAccum.HasRemainingDamage;
            }

            _damageAccum?.ResetToInitalAmount();
            return false;
        }*/







        public static Vector3 FindPushToTile(TacticalActor attacker, TacticalActor defender, int numTiles)
        {

            try
            {


                Vector3 diff = defender.Pos - attacker.Pos;
                Vector3 pushToPosition = defender.Pos + numTiles * diff.normalized;

                // TFTVLogger.Always($"attacker position is {attacker.Pos} and defender position is {defender.Pos}, so difference is {diff} and pushtoposition is {pushToPosition}");



                return pushToPosition;

            }


            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        /*  [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

          public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
          {
              public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {
                              TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                              if (tacticalActor != null)
                              {
                                  tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                     TFTVLogger.Always($", added {knockBackAbility.name} to {tacticalActor.name}");
                              }
                          }
                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

              public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
              {
                  try
                  {
                      RepositionAbilityDef knockBackAbility = DefCache.GetDef<RepositionAbilityDef>("KnockBackAbility");
                      BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                      if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                      {
                             TFTVLogger.Always($", ability is {ability.TacticalAbilityDef.name}");

                          if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                          {

                              TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                              TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                              if (tacticalActor != null && tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                              {
                                  RepositionAbility knockBack = tacticalActor.GetAbilityWithDef<RepositionAbility>(knockBackAbility);

                                  IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                  TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                  TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                  foreach (TacticalAbilityTarget target in targets)
                                  {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                      if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 2)).magnitude <= 1f)
                                      {
                                          TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                          pushPosition = target;

                                      }
                                  }


                                  //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                  //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                  //  moveAbility.Activate(pushPosition);

                                  knockBack.Activate(pushPosition);



                                  TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                              }
                          }
                      }

                      if (ability.TacticalAbilityDef == knockBackAbility)
                      {
                          __instance.RemoveAbility(ability);

                      }
                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }

          }

          */


        /* [HarmonyPatch(typeof(TacticalActor), "OnAbilityExecuteFinished")]

           public static class TacticalActor_OnAbilityExecuteFinished_KnockBack_Experiment_patch
           {
               public static void Prefix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {
                      // TFTVLogger.Always($"ability {ability.TacticalAbilityDef.name} executed by {__instance.DisplayName}");

                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");
                       if (ability.TacticalAbilityDef!=null && ability.TacticalAbilityDef == strikeAbility)
                       {
                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                           //    TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;

                               if (tacticalActor != null)
                               {
                                   tacticalActor.AddAbility(knockBackAbility, tacticalActor);
                                //   TFTVLogger.Always($", added {knockBackAbility.name} to {tacticalActor.name}");
                               }
                           }
                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

               public static void Postfix(TacticalAbility ability, TacticalActor __instance, object parameter)
               {
                   try
                   {


                       JetJumpAbilityDef knockBackAbility = DefCache.GetDef<JetJumpAbilityDef>("KnockBackAbility");
                       BashAbilityDef strikeAbility = DefCache.GetDef<BashAbilityDef>("BashStrike_AbilityDef");

                       if (ability.TacticalAbilityDef != null && ability.TacticalAbilityDef == strikeAbility)
                       {
                        //   TFTVLogger.Always($", ability is {ability.TacticalAbilityDef.name}");

                           if (parameter is TacticalAbilityTarget abilityTarget && abilityTarget.GetTargetActor() != null)
                           {

                              // TFTVLogger.Always($", target is {abilityTarget.GetTargetActor()}");

                               TacticalActor tacticalActor = abilityTarget.GetTargetActor() as TacticalActor;



                               if (tacticalActor != null && tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility) != null && tacticalActor.IsAlive)
                               {
                                   JetJumpAbility knockBack = tacticalActor.GetAbilityWithDef<JetJumpAbility>(knockBackAbility);

                                   IEnumerable<TacticalAbilityTarget> targets = knockBack.GetTargets();

                                   TacticalAbilityTarget pushPosition = new TacticalAbilityTarget();
                                   TacticalAbilityTarget attack = parameter as TacticalAbilityTarget;

                                   foreach (TacticalAbilityTarget target in targets)  
                                   {
                                      // TFTVLogger.Always($"possible position {target.PositionToApply} and magnitude is {(target.PositionToApply - FindPushToTile(__instance, tacticalActor)).magnitude} ");

                                       if ((target.PositionToApply - FindPushToTile(__instance, tacticalActor, 1)).magnitude <= 1f) 
                                       {
                                           TFTVLogger.Always($"chosen position {target.PositionToApply}");

                                           pushPosition = target;

                                       }
                                   }


                                   //  MoveAbilityDef moveAbilityDef = DefCache.GetDef<MoveAbilityDef>("Move_AbilityDef");

                                   //  MoveAbility moveAbility = tacticalActor.GetAbilityWithDef<MoveAbility>(moveAbilityDef);
                                   //  moveAbility.Activate(pushPosition);

                                   knockBack.Activate(pushPosition);



                                   TFTVLogger.Always($"knocback executed position should be {pushPosition.GetActorOrWorkingPosition()}");

                               }
                           }
                       }

                       if (ability.TacticalAbilityDef == knockBackAbility)
                       {
                           __instance.RemoveAbility(ability);

                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }

           }


        */









        /*  [HarmonyPatch(typeof(AIFaction), "GetActionScore")]

           public static class TFTV_Experimental_AIActionMoveAndEscape_GetModuleBonusByType_AdjustFARMRecuperationModule_patch
           {
               public static void Prefix(AIFaction __instance, AIAction action, IAIActor actor, object context, LazyCache<AIConsiderationDef, AIConsideration> ____considerationsCache)
               {
                   try
                   {
                       if (action.ActionDef.name == "Flee_AIActionDef")
                       {
                           StatusDef autoRepairStatusDef = DefCache.GetDef<StatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
                           TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                           foreach (TacticalActor tacticalActor in tacticalLevelController.GetFactionByCommandName("pu").TacticalActors)
                           {
                               if (tacticalActor.HasStatus(autoRepairStatusDef))
                               {
                                   TFTVLogger.Always($"{tacticalActor.name} has autorepair status");
                               }


                           }


                           float num = action.ActionDef.Weight;
                           TFTVLogger.Always($"get action score for action {action.ActionDef.name} with a weight of {num}");
                           AIAdjustedConsideration[] earlyExitConsiderations = action.ActionDef.EarlyExitConsiderations;

                           foreach (AIAdjustedConsideration aIAdjustedConsideration in earlyExitConsiderations)
                           {

                               if (aIAdjustedConsideration.Consideration == null)
                               {
                                   throw new InvalidOperationException($"Missing consideration for {actor} at {action.ActionDef.name}");
                               }

                               float time = ____considerationsCache.Get(aIAdjustedConsideration.Consideration).Evaluate(actor, null, context);
                               float num2 = aIAdjustedConsideration.ScoreCurve.Evaluate(time);

                               num *= num2;

                               TFTVLogger.Always($"early consideration is {aIAdjustedConsideration.Consideration.name} and num2 is {num2}, so score is now {num}");
                               if (num < 0.0001f)
                               {
                                   TFTVLogger.Always($"aIAdjustedConsideration {aIAdjustedConsideration.Consideration.name} reduced score to nearly 0");
                                   break;

                               }
                           }

                       }



                     /*  if (action.ActionDef.name == "MoveAndQuickAim_AIActionDef")
                       {
                           StatusDef autoRepairStatusDef = DefCache.GetDef<StatusDef>("RoboticSelfRepair_AddAbilityStatusDef");
                           TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                           ApplyStatusAbilityDef quickaim = DefCache.GetDef<ApplyStatusAbilityDef>("BC_QuickAim_AbilityDef");

                           foreach (TacticalActor tacticalActor in tacticalLevelController.GetFactionByCommandName("pu").TacticalActors) 
                           {
                               if (tacticalActor.GetAbilityWithDef <ApplyStatusAbility> (quickaim)!=null) 
                               {
                                   TFTVLogger.Always($"{tacticalActor.name} has quickaim ability");
                               }


                           }


                           float num = action.ActionDef.Weight;
                           TFTVLogger.Always($"get action score for action {action.ActionDef.name} with a weight of {num}");
                           AIAdjustedConsideration[] earlyExitConsiderations = action.ActionDef.EarlyExitConsiderations;

                           foreach (AIAdjustedConsideration aIAdjustedConsideration in earlyExitConsiderations)
                           {

                               if (aIAdjustedConsideration.Consideration == null)
                               {
                                   throw new InvalidOperationException($"Missing consideration for {actor} at {action.ActionDef.name}");
                               }

                               float time = ____considerationsCache.Get(aIAdjustedConsideration.Consideration).Evaluate(actor, null, context);
                               float num2 = aIAdjustedConsideration.ScoreCurve.Evaluate(time);

                               num *= num2;

                               TFTVLogger.Always($"early consideration is {aIAdjustedConsideration.Consideration.name} and num2 is {num2}, so score is now {num}");
                               if (num < 0.0001f)
                               {
                                   TFTVLogger.Always($"aIAdjustedConsideration {aIAdjustedConsideration.Consideration.name} reduced score to nearly 0");
                                   break;

                               }
                           }

                       }
                   }

                   catch (Exception e)
                   {
                       TFTVLogger.Error(e);
                   }

               }
           }*/









        /* [HarmonyPatch(typeof(GeoHavenLeader), "CanTradeWithFaction")]

         public static class TFTV_Experimental_GeoHavenLeader_CanTradeWithFaction_EnableTradingWhenNotAtWar_patch
         {
             public static void Postfix(GeoHavenLeader __instance, IDiplomaticParty faction, ref bool __result)
             {
                 try
                 {
                     MethodInfo getRelationMethod = AccessTools.Method(typeof(GeoHavenLeader), "GetRelationWith");
                     PartyDiplomacy.Relation relation = (PartyDiplomacy.Relation)getRelationMethod.Invoke(__instance, new object[] { faction });

                     __result = relation.Diplomacy > -50;

                 }

                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }

             }
         }*/








        /*  [HarmonyPatch(typeof(GeoHaven), "GetResourceTrading")]
          public static class TFTV_Experimental_GeoHaven_GetResourceTrading_IncreaseCostDiplomacy_patch
          {
              public static void Postfix(GeoHaven __instance, ref List<HavenTradingEntry> __result)
              {
                  try
                  {
                      GeoFaction phoenixFaction = __instance.Site.GeoLevel.PhoenixFaction;
                      PartyDiplomacy.Relation relation = __instance.Leader.Diplomacy.GetRelation(phoenixFaction);
                      float multiplier = 1f;
                      List<HavenTradingEntry> offeredTrade = new List<HavenTradingEntry>(__result);

                      if (relation.Diplomacy > -50 && relation.Diplomacy <= -25)
                      {
                          multiplier = 0.5f;
                      }
                      else if (relation.Diplomacy > -25 && relation.Diplomacy <= 0)
                      {
                          multiplier = 0.75f;
                          TFTVLogger.Always("GetResourceTrading");
                      }

                      for (int i = 0; i < offeredTrade.Count; i++)
                      {
                         HavenTradingEntry havenTradingEntry = offeredTrade[i];
                          offeredTrade[i] = new HavenTradingEntry
                          {
                              HavenOfferQuantity = (int)(havenTradingEntry.HavenOfferQuantity*multiplier),
                              HavenOffers = havenTradingEntry.HavenOffers,
                              HavenWants = havenTradingEntry.HavenWants,
                              ResourceStock = havenTradingEntry.ResourceStock,
                              HavenReceiveQuantity = havenTradingEntry.HavenReceiveQuantity,
                          };
                          TFTVLogger.Always("New value is " + offeredTrade[i].HavenOfferQuantity);
                      }

                      __result = offeredTrade;

                  }

                  catch (Exception e)
                  {
                      TFTVLogger.Error(e);
                  }

              }
          }*/














        //  public static float Score = 0;
        //  public static List<float> ScoresBeforeCulling = new List<float>();
        //  public static int CounterAIActionsInfluencedBySafetyConsideration = 0;









        /* [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
         public static class GeoMission_PrepareLevel_VOObjectives_Patch
         {
             public static void Postfix(TacMissionData missionData, GeoMission __instance)
             {
                 try
                 {
                    // TFTVLogger.Always("PrepareLevel invoked");
                     GeoLevelController controller = __instance.Level;
                     List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                     List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                     // Remove faction objectives that correspond to void omens that are not in play
                     for (int i = listOfFactionObjectives.Count - 1; i >= 0; i--)
                     {
                         FactionObjectiveDef objective = listOfFactionObjectives[i];
                         if (objective.name.StartsWith("VOID_OMEN_TITLE_"))
                         {
                             int vo = int.Parse(objective.name.Substring("VOID_OMEN_TITLE_".Length));
                             if (!TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                             {
                                 TFTVLogger.Always("Removing VO " + vo + " from faction objectives");
                                 listOfFactionObjectives.RemoveAt(i);
                             }
                         }
                     }

                     // Add faction objectives for void omens that are in play
                     foreach (int vo in voidOmens)
                     {
                         if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                         {
                             if (!listOfFactionObjectives.Any(o => o.name == "VOID_OMEN_TITLE_" + vo))
                             {
                                 TFTVLogger.Always("Adding VO " + vo + " to faction objectives");
                                 listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                             }
                         }
                     }

                     missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/




        /* [HarmonyPatch(typeof(GeoMission), "PrepareLevel")]
         public static class GeoMission_ModifyMissionData_AddVOObjectives_Patch
         {
             public static void Postfix(TacMissionData missionData, GeoMission __instance)
             {
                 try
                 {
                     TFTVLogger.Always("ModifyMissionData invoked");
                     GeoLevelController controller = __instance.Level;
                     List<int> voidOmens = new List<int> { 3, 5, 7, 10, 15, 16, 19 };

                     foreach (int vo in voidOmens)
                     {
                         if (TFTVVoidOmens.CheckFordVoidOmensInPlay(controller).Contains(vo))
                         {
                             TFTVLogger.Always("VO " + vo + " found");
                             List<FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();

                             if (!listOfFactionObjectives.Contains(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo)))
                             {
                                 listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("VOID_OMEN_TITLE_" + vo));
                                 missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();
                             }
                         }
                     }

                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/











    }
    /* [HarmonyPatch(typeof(AIStrategicPositionConsideration), "Evaluate")]

        public static class AIStrategicPositionConsideration_Evaluate_Experiment_patch
        {
            public static void Postfix(AIStrategicPositionConsideration __instance, float __result)
            {
                try
                {
                    if (__instance.BaseDef.name == "StrategicPosition_AIConsiderationDef" && __result != 1)
                    {

                        TFTVLogger.Always("StrategicPosition_AIConsiderationDef " + __result);
                        Score = __result;
                    }
                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }*/

}


