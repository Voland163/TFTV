using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using System;
using System.Reflection;
using UnityEngine;

namespace TFTV
{
    internal class TFTVTradingRecruiting
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [HarmonyPatch(typeof(GeoHavenLeader), "CanRecruitWithFaction")]

        public static class TFTV_Experimental_GeoHavenLeader_CanRecruitWithFaction_EnableRecruitingWhenNotAtWar_patch
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
        }

        [HarmonyPatch(typeof(GeoHaven), "GetRecruitCost")]
        public static class TFTV_Experimental_GeoHaven_GetRecruitCost_IncreaseCostDiplomacy_patch
        {
            public static void Postfix(GeoHaven __instance, ref ResourcePack __result, GeoFaction forFaction)
            {
                try
                {
                    GeoHavenLeader leader = __instance.Leader;
                    MethodInfo getRelationMethod = AccessTools.Method(typeof(GeoHavenLeader), "GetRelationWith");
                    PartyDiplomacy.Relation relation = (PartyDiplomacy.Relation)getRelationMethod.Invoke(leader, new object[] { forFaction });
                    ResourcePack price = new ResourcePack(__result);
                    float multiplier = 1f;
                    if (relation.Diplomacy > -50 && relation.Diplomacy <= -25)
                    {
                        multiplier = 1.5f;
                    }
                    else if (relation.Diplomacy > -25 && relation.Diplomacy <= 0)
                    {
                        multiplier = 1.25f;

                    }

                    for (int i = 0; i < price.Count; i++)
                    {
                        //  TFTVLogger.Always("Price component is " + price[i].Type + " amount " + price[i].Value);
                        ResourceUnit resourceUnit = price[i];
                        price[i] = new ResourceUnit(resourceUnit.Type, resourceUnit.Value * multiplier);
                    }

                    __result = price;


                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }

            }
        }


        [HarmonyPatch(typeof(GeoHaven), "CheckShouldSpawnRecruit")]
        public static class TFTV_Experimental_GeoHaven_CheckShouldSpawnRecruit_IncreaseCostDiplomacy_patch
        {
            public static void Postfix(GeoHaven __instance, ref bool __result, float modifier)
            {
                try
                {
                    if (__result == false)
                    {
                        if (!__instance.IsRecruitmentEnabled || !__instance.ZonesStats.CanGenerateRecruit)
                        {
                            __result = false;
                        }
                        else
                        {
                            GeoFaction phoenixFaction = __instance.Site.GeoLevel.PhoenixFaction;
                            PartyDiplomacy.Relation relation = __instance.Leader.Diplomacy.GetRelation(phoenixFaction);
                            if (relation.Diplomacy <= 0 && relation.Diplomacy > -50)
                            {
                                int num = __instance.HavenDef.RecruitmentBaseChance;
                                num = Mathf.RoundToInt((float)num * modifier);


                                __result = UnityEngine.Random.Range(0, 100) < num;
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

    }



}
