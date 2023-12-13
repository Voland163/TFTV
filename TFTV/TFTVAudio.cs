using Base.Eventus;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Eventus.Contexts;
using PhoenixPoint.Tactical.Eventus.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
    internal class TFTVAudio
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;



        private static readonly List<GameTagDef> _palaceMissionGameTagsToCheck = new List<GameTagDef>()
                 {
                 DefCache.GetDef<GameTagDef>("TaxiarchNergal_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Zhara_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Stas_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Nikolai_TacCharacterDef_GameTagDef"),
                 DefCache.GetDef<GameTagDef>("Richter_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Harlson_TacCharacterDef_GameTagDef"),
                DefCache.GetDef<GameTagDef>("Sofia_TacCharacterDef_GameTagDef"),
                 };

        [HarmonyPatch(typeof(TacActorHeadMutationsFilterDef), "ShouldPlayEvent")]
        public static class UTacActorHeadMutationsFilterDef_ShouldPlayEvent_patch
        {
            public static void Postfix(BaseEventContext context, ref bool __result)
            {
                try
                {
                    if (!(context is TacActorEventContext tacActorEventContext))
                    {

                    }
                    else
                    {
                        GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");
                        TFTVConfig config = TFTVMain.Main.Config;

                        if (tacActorEventContext.Actor.HasGameTag(humanTag) && config.NoBarks || _palaceMissionGameTagsToCheck.Any(gt => tacActorEventContext.Actor.GameTags.Contains(gt)))
                        {
                            //  TFTVLogger.Always($"stopping bark from {tacActorEventContext.Actor.name}");
                            __result = false;
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
    }
}
