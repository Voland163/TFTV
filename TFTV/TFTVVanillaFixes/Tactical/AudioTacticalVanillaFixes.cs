using Base.Audio;
using Base.Core;
using Base.Eventus;
using HarmonyLib;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class AudioTacticalVanillaFixes
    {
        private static bool _musicVolumeAncientMapAdjusted = false;

        [HarmonyPatch(typeof(AudioManager), "PlayEvent")] //VERIFIED
        public static class AudioManager_PlayEvent_patch
        {
            public static void Prefix(AudioManager __instance, AudioEventData eventData, BaseEventContext context)
            {
                try
                {
                    if (GameUtl.CurrentLevel() != null && GameUtl.CurrentLevel().GetComponent<TacticalLevelController>() != null)
                    {
                        TacticalLevelController controller = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

                        if (TFTVAncients.CheckIfAncientMap(controller) && !_musicVolumeAncientMapAdjusted)
                        {
                            //if (eventData.Event.Name == "TacticalMusicEnemyTurn" || eventData.Event.Name == "TacticalMusicPlayerTurn")
                            //  {
                            if (__instance.MasterVolumeRTPC.GetGlobalValue() > 0.25f && __instance.MusicVolumeRTPC.GetGlobalValue() > 0.25f)
                            {
                                __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 0.25f);
                                _musicVolumeAncientMapAdjusted = true;

                                //  AKRESULT result = AkSoundEngine.SetRTPCValue(eventData.Event.Id, 0.01f, __instance.MusicVolumeRTPC.Id);
                                // AKRESULT aKRESULT = AkSoundEngine.SetRTPCValue("", eventData.Event.Id, 0.01f);

                                TFTVLogger.Always($"Ancients map: music reduced to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
                            }//AKRESULT: {result}");
                        }
                        else if (!TFTVAncients.CheckIfAncientMap(controller) && _musicVolumeAncientMapAdjusted)
                        {
                            __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 4f);

                            _musicVolumeAncientMapAdjusted = false;

                            TFTVLogger.Always($"resetting music to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
                        }

                        //  AkSoundEngine
                        //  }
                        return;
                    }

                    if (_musicVolumeAncientMapAdjusted)
                    {
                        __instance.SetAudioLevel(MixerKey.Music, __instance.MasterVolumeRTPC.GetGlobalValue() * 4f);

                        _musicVolumeAncientMapAdjusted = false;
                        TFTVLogger.Always($"resetting music to {__instance.MasterVolumeRTPC.GetGlobalValue()}");
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
