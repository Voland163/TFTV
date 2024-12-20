using Base.Entities;
using Base.Eventus;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Eventus.Contexts;
using PhoenixPoint.Tactical.Eventus.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV
{
    internal class TFTVAudio
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        [HarmonyPatch(typeof(HasTagsEventFilterDef), "ShouldPlayEvent")]
        public static class HasTagsEventFilterDef_ShouldPlayEvent_patch
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
                        //GameTagDef humanTag = DefCache.GetDef<GameTagDef>("Human_TagDef");
                        GameTagDef mutoidTag = DefCache.GetDef<GameTagDef>("Mutoid_TagDef");
                        TFTVConfig config = TFTVMain.Main.Config;

                        ActorComponentDef actorComponentDef = tacActorEventContext.Actor.ActorDef;

                        if (actorComponentDef.name.Equals("Oilcrab_ActorDef") || actorComponentDef.name.Equals("Oilfish_ActorDef"))
                        {
                            //  TFTVLogger.Always($"HasTagsEventFilterDef: bark from {tacActorEventContext.Actor.name}");
                            __result = false;
                        }


                        if (config.NoBarks && tacActorEventContext.Actor.Health.Value > 0 &&
                            tacActorEventContext.Actor.HasGameTag(mutoidTag))
                        {
                            //  TFTVLogger.Always($"HasTagsEventFilterDef: stopping bark from {tacActorEventContext.Actor.name}");
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
        public static class TacActorHeadMutationsFilterDef_ShouldPlayEvent_patch
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
                        //  GameTagDef mutoidTag = DefCache.GetDef<GameTagDef>("Mutoid_TagDef");
                        TFTVConfig config = TFTVMain.Main.Config;

                        TacticalActorBase tacticalActorBase = tacActorEventContext.Actor;

                       /* TFTVLogger.Always($"TacActorHeadMutationsFilterDef: bark from {tacticalActorBase.DisplayName} {_palaceMissionGameTagsToCheck.Any(gt => tacticalActorBase.GameTags.Contains(gt))}");
                   
                        foreach (GameTagDef gameTagDef in tacticalActorBase.GameTags)
                        {
                            TFTVLogger.Always($"{tacticalActorBase.DisplayName} has tag {gameTagDef.name}");
                        }*/

                        if (config.NoBarks && tacActorEventContext.Actor.Health.Value > 0 &&
                            tacActorEventContext.Actor.HasGameTag(humanTag) ||
                            _palaceMissionGameTagsToCheck.Any(gt => tacticalActorBase.GameTags.Contains(gt)))
                        {
                           // TFTVLogger.Always($"TacActorHeadMutationsFilterDef: stopping bark from {tacticalActorBase.DisplayName}");
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
