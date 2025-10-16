using AK.Wwise;
using Base.Audio;
using Base.Entities;
using Base.Eventus;
using Base.Utils;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Eventus.Contexts;
using PhoenixPoint.Tactical.Eventus.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using static RootMotion.FinalIK.AimPoser;
using Object = UnityEngine.Object;

namespace TFTV
{

   


    internal class TFTVAudio
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        /// <summary>
        ///     Provides a Harmony based hook that replaces the stock Wwise playback path with a
        ///     fully managed alternative that can map <see cref="AudioEventData"/> instances to
        ///     arbitrary <see cref="AudioClip"/> assets.
        /// </summary>
        [HarmonyPatch]
        public static class ExternalAudioInjector
        {
            private static readonly ConditionalWeakTable<AudioManager, AudioManagerHook> Hooks = new ConditionalWeakTable<AudioManager, AudioManagerHook>();

            private static readonly Dictionary<TacticalEventDef, IExternalAudioPlayback> Registrations =
                new Dictionary<TacticalEventDef, IExternalAudioPlayback>();

            private static readonly MethodInfo GetEventusManagerMethod =
                AccessTools.Method(typeof(AudioManager), "GetEventusManager", Type.EmptyTypes);

            private static GameObject _fallbackEmitter;
            private static AudioListener _managedListener;
            private static string _fallbackAnchorName;

            static ExternalAudioInjector()
            {
                FallbackToOriginalHandler = true;
            }

            /// <summary>
            ///     Gets or sets whether events without an explicit external registration should fall back to
            ///     the original Wwise handler. Defaults to <c>false</c> which effectively mutes unregistered events.
            /// </summary>
            public static bool FallbackToOriginalHandler { get; set; }

            /// <summary>
            ///     Registers a fixed <see cref="AudioClip"/> for the supplied tactical event definition.
            /// </summary>
            public static void RegisterClip(
    TacticalEventDef eventDef,
    AudioClip clip,
    float volume = 1f,
    bool loop = false,
    float spatialBlend = 0f,
    Action<AudioSource> configureSource = null)
            {
                if (eventDef == null)
                    throw new ArgumentNullException(nameof(eventDef));

                if (clip == null)
                    throw new ArgumentNullException(nameof(clip));

                Registrations[eventDef] = new ExternalAudioClipPlayback(
                    (ctx, evt) => clip,              // replaced (_, _) => clip
                    volume,
                    loop,
                    spatialBlend,
                    (source, ctx, evt) =>            // replaced (source, _, _) => ...
                    {
                        if (configureSource != null)
                            configureSource(source);
                    });
            }


            /// <summary>
            ///     Loads an <see cref="AudioClip"/> from disk and registers it for the supplied tactical event definition.
            /// </summary>
            public static AudioClip RegisterClipFromFile(
                TacticalEventDef eventDef,
                string filePath,
                float volume = 1f,
                bool loop = false,
                float spatialBlend = 1f,
                Action<AudioSource> configureSource = null,
                AudioType audioType = AudioType.UNKNOWN,
                bool streamAudio = false)
            {
                AudioClip clip = LoadClipFromFile(filePath, audioType, streamAudio);
                RegisterClip(eventDef, clip, volume, loop, spatialBlend, configureSource);
                return clip;
            }

            /// <summary>
            ///      Registers a delegate that resolves a clip at runtime for the given tactical event definition.
            /// </summary>
            public static void RegisterResolver(
                TacticalEventDef eventDef,
                Func<AudioEventData, BaseEventContext, AudioClip> clipResolver,
                float volume = 1f,
                bool loop = false,
                float spatialBlend = 1f,
                Action<AudioSource, AudioEventData, BaseEventContext> configureSource = null)
            {
                if (eventDef == null)
                {
                    throw new ArgumentNullException(nameof(eventDef));
                }

                if (clipResolver == null)
                {
                    throw new ArgumentNullException(nameof(clipResolver));
                }

                Registrations[eventDef] = new ExternalAudioClipPlayback(
                    clipResolver,
                    volume,
                    loop,
                    spatialBlend,
                    configureSource);
            }

            /// <summary>
            ///     Registers a custom playback delegate for the supplied tactical event definition.
            ///     The delegate should return <c>true</c> when it consumes the event.
            /// </summary>
            public static void RegisterHandler(
                TacticalEventDef eventDef,
                Func<AudioEventData, BaseEventContext, bool> playbackHandler)
            {
                if (eventDef == null)
                {
                    throw new ArgumentNullException(nameof(eventDef));
                }

                if (playbackHandler == null)
                {
                    throw new ArgumentNullException(nameof(playbackHandler));
                }

                Registrations[eventDef] = new DelegateAudioPlayback(playbackHandler);
            }

            public static void EnsureHooksOnExistingManagers()
            {
                MethodInfoCache methodCache = MethodInfoCache.ForAudioManager();
                if (methodCache == null)
                {
                    return;
                }

                AudioManager[] managers = Resources.FindObjectsOfTypeAll<AudioManager>();
                if (managers == null || managers.Length == 0)
                {
                    return;
                }

                foreach (AudioManager manager in managers)
                {
                    if (manager == null)
                    {
                        continue;
                    }

                    EventusManager eventusManager = ResolveEventusManager(manager);
                    if (eventusManager == null)
                    {
                        continue;
                    }

                    if (!Hooks.TryGetValue(manager, out AudioManagerHook hook))
                    {
                        hook = new AudioManagerHook(manager, methodCache);
                        Hooks.Add(manager, hook);
                    }

                    hook.Install(eventusManager);
                }
            }

            /// <summary>
            ///     Clears any custom registration for the provided event definition.
            /// </summary>
            public static bool Unregister(TacticalEventDef eventDef)
            {
                if (eventDef == null)
                {
                    throw new ArgumentNullException(nameof(eventDef));
                }

                return Registrations.Remove(eventDef);
            }

            /// <summary>
            ///     Removes all registered mappings.
            /// </summary>
            public static void ClearRegistrations()
            {
                Registrations.Clear();
            }

            /// <summary>
            ///     Loads an <see cref="AudioClip"/> from the specified file path.
            /// </summary>
            public static AudioClip LoadClipFromFile(string filePath, AudioType audioType = AudioType.UNKNOWN, bool streamAudio = false)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path must be provided", nameof(filePath));
                }

                string absolutePath = Path.GetFullPath(filePath);
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException("Audio file not found", absolutePath);
                }

                if (audioType == AudioType.UNKNOWN)
                {
                    audioType = GuessAudioType(Path.GetExtension(absolutePath));
                }

                if (audioType == AudioType.UNKNOWN)
                {
                    throw new NotSupportedException($"Unable to determine audio type for '{absolutePath}'. Specify the AudioType explicitly.");
                }

                string uri = new Uri(absolutePath).AbsoluteUri;

                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
                {
                    if (request.downloadHandler is DownloadHandlerAudioClip downloadHandler)
                    {
                        downloadHandler.streamAudio = streamAudio;
                    }

                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        Thread.Sleep(1);
                    }

#if UNITY_2020_2_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                    if (request.isNetworkError || request.isHttpError)
#endif
                    {
                        throw new InvalidOperationException($"Failed to load audio clip from '{absolutePath}': {request.error}");
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request) ?? throw new InvalidOperationException($"Failed to decode audio clip from '{absolutePath}'.");
                    clip.name = Path.GetFileNameWithoutExtension(absolutePath);
                    return clip;
                }
            }

            [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Init))]
            [HarmonyPostfix]
            private static void AudioManagerInitPostfix(AudioManager __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                EventusManager eventusManager = ResolveEventusManager(__instance);
                if (eventusManager == null)
                {
                    return;
                }

                MethodInfoCache methodCache = MethodInfoCache.ForAudioManager();
                if (methodCache == null)
                {
                    return;
                }

                if (Hooks.TryGetValue(__instance, out AudioManagerHook existingHook))
                {
                    existingHook.Install(eventusManager);
                    return;
                }

                AudioManagerHook hook = new AudioManagerHook(__instance, methodCache);
                hook.Install(eventusManager);
                Hooks.Add(__instance, hook);
            }

            [HarmonyPatch(typeof(AudioManager), "OnDestroy")]
            [HarmonyPrefix]
            private static void AudioManagerOnDestroyPrefix(AudioManager __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                EventusManager eventusManager = ResolveEventusManager(__instance);
                if (eventusManager == null)
                {
                    return;
                }

                if (Hooks.TryGetValue(__instance, out AudioManagerHook hook))
                {
                    hook.Uninstall(eventusManager);
                    Hooks.Remove(__instance);
                }
            }

            private static bool TryHandle(AudioManager manager, AudioEventData audioEvent, BaseEventContext context)
            {
                if (audioEvent == null)
                {
                    return false;
                }

                TacticalEventDef eventDef = audioEvent.EventDef as TacticalEventDef;
                if (eventDef == null)
                {
                    return false;
                }

                if (!Registrations.TryGetValue(eventDef, out IExternalAudioPlayback playback))
                {
                    return false;
                }

                return playback.TryPlay(manager, audioEvent, context ?? new SimpleEventContext(manager.gameObject));
            }

            private static Transform ResolveEmitter(AudioManager manager, BaseEventContext context)
            {
                if (context is IWorldEventContext worldContext && worldContext.TargetTransform)
                {
                    return worldContext.TargetTransform;
                }

                Transform senderTransform = context?.SenderTransform;
                if (senderTransform)
                {
                    return senderTransform;
                }

                if (manager != null && manager.transform)
                {
                    return manager.transform;
                }

                return EnsureFallbackEmitter(manager, context).transform;
            }

            private static GameObject EnsureFallbackEmitter(AudioManager manager, BaseEventContext context)
            {
                if (_fallbackEmitter != null)
                {
                    if (_fallbackEmitter)
                    {
                        SyncFallbackEmitterAnchor(manager, context);
                        return _fallbackEmitter;
                    }

                    _fallbackEmitter = null;
                }

                _fallbackEmitter = new GameObject("ExternalAudioEmitter");
                UnityEngine.Object.DontDestroyOnLoad(_fallbackEmitter);
                _fallbackEmitter.hideFlags = HideFlags.HideAndDontSave;
                _fallbackEmitter.AddComponent<AudioSource>();
                SyncFallbackEmitterAnchor(manager, context);
                return _fallbackEmitter;
            }

            private static void SyncFallbackEmitterAnchor(AudioManager manager, BaseEventContext context)
            {
                if (_fallbackEmitter == null)
                {
                    return;
                }

                Transform anchor = ResolveFallbackAnchor(manager, context);
                if (anchor != null)
                {
                    if (_fallbackEmitter.transform.parent != anchor)
                    {
                        if (_fallbackEmitter.transform.parent != null)
                        {
                            AudioSource source = _fallbackEmitter.GetComponent<AudioSource>();
                            if (source != null && source.isPlaying)
                            {
                                source.Stop();
                            }
                        }

                        _fallbackEmitter.transform.SetParent(anchor, false);
                        _fallbackEmitter.transform.localPosition = Vector3.zero;
                        _fallbackEmitter.transform.localRotation = Quaternion.identity;
                        _fallbackEmitter.transform.localScale = Vector3.one;

                        string anchorScene = anchor.gameObject.scene.IsValid() ? anchor.gameObject.scene.name : "<no-scene>";
                        string anchorName = $"{anchor.name} (scene: {anchorScene}, id: {anchor.GetInstanceID()})";
                        if (_fallbackAnchorName != anchorName)
                        {
                            _fallbackAnchorName = anchorName;
                        }
                    }
                }
                else if (_fallbackEmitter.transform.parent != null)
                {
                    _fallbackEmitter.transform.SetParent(null, false);
                    _fallbackAnchorName = null;
                }
            }

            private static Transform ResolveFallbackAnchor(AudioManager manager, BaseEventContext context)
            {
                Camera managerCamera = manager?.GetComponentInChildren<Camera>();
                if (managerCamera != null)
                {
                    return managerCamera.transform;
                }

                if (context is IWorldEventContext worldContext)
                {
                    Transform worldTarget = worldContext.TargetTransform;
                    if (worldTarget)
                    {
                        Camera contextCamera = worldTarget.GetComponentInChildren<Camera>();
                        if (contextCamera != null)
                        {
                            return contextCamera.transform;
                        }
                    }
                }

                if (Camera.main != null)
                {
                    return Camera.main.transform;
                }

                Camera anyCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                if (anyCamera != null)
                {
                    return anyCamera.transform;
                }

                return manager != null ? manager.transform : null;
            }

            private static AudioListener EnsureManagedAudioListener(AudioManager manager, BaseEventContext context)
            {
                if (_managedListener != null)
                {
                    if (_managedListener && _managedListener.isActiveAndEnabled)
                    {
                        ActivateManagedAudioListener(_managedListener);
                        return _managedListener;
                    }

                    _managedListener = null;
                }

                AudioListener listener = (manager?.GetComponentInChildren<AudioListener>()) ?? UnityEngine.Object.FindObjectOfType<AudioListener>();
                TFTVLogger.Always($"found a listener? {listener != null}");

                if (listener != null && listener.gameObject != _fallbackEmitter)
                {
                    if (!listener.isActiveAndEnabled)
                    {
                        listener = null;
                    }
                }

                if (listener != null)
                {
                    ActivateManagedAudioListener(listener);
                    return _managedListener;
                }

                GameObject fallback = EnsureFallbackEmitter(manager, context);
                listener = fallback.GetComponent<AudioListener>();
                if (listener == null)
                {
                    listener = fallback.AddComponent<AudioListener>();
                }

                if (!listener.enabled)
                {
                    listener.enabled = true;
                }

                if (!listener.gameObject.activeSelf)
                {
                    listener.gameObject.SetActive(true);
                }

                ActivateManagedAudioListener(listener);
                return _managedListener;
            }

            private static void ActivateManagedAudioListener(AudioListener listener)
            {
                if (listener == null)
                {
                    return;
                }

                _managedListener = listener;

                if (AudioListener.pause)
                {
                    AudioListener.pause = false;
                }

                bool restartOutput = false;

                if (AudioSettings.Mobile.stopAudioOutputOnMute)
                {
                    AudioSettings.Mobile.stopAudioOutputOnMute = false;
                    restartOutput = true;
                }

                TFTVLogger.Always($"AudioSettings.Mobile.muteState: {AudioSettings.Mobile.muteState}");

                if (AudioSettings.Mobile.muteState)
                {
                   // AudioSettings.Mobile.muteState = false;
                    restartOutput = true;
                }

                if (restartOutput)
                {
                    AudioSettings.Mobile.StartAudioOutput();
                }
            }

            private static EventusManager ResolveEventusManager(AudioManager manager)
            {
                if (manager == null)
                {
                    return null;
                }

                if (GetEventusManagerMethod != null)
                {
                    try
                    {
                        return (EventusManager)GetEventusManagerMethod.Invoke(manager, Array.Empty<object>());
                    }
                    catch
                    {
                        // ignored - fall back to component lookup below
                    }
                }

                return manager.GetComponent<EventusManager>();
            }

            private static AudioType GuessAudioType(string extension)
            {
                if (string.IsNullOrEmpty(extension))
                {
                    return AudioType.UNKNOWN;
                }

                switch (extension.ToLowerInvariant())
                {
                    case ".wav":
                    case ".wave":
                        return AudioType.WAV;
                    case ".ogg":
                    case ".oga":
                        return AudioType.OGGVORBIS;
                    case ".mp3":
                        return AudioType.MPEG;
                    case ".aif":
                    case ".aiff":
                        return AudioType.AIFF;
                    default:
                        return AudioType.UNKNOWN;
                }
            }

            private sealed class AudioManagerHook
            {
                private readonly AudioManager _manager;
                private readonly MethodInfoCache _methodCache;
                private readonly EventusManager.EventusHandler _dispatcher;

                private EventusManager.EventusHandler _originalHandler;

                public AudioManagerHook(AudioManager manager, MethodInfoCache methodCache)
                {
                    _manager = manager;
                    _methodCache = methodCache;
                    _dispatcher = Dispatch;
                    _originalHandler = methodCache.CreateDelegate(manager);
                }

                public void Install(EventusManager eventusManager)
                {
                    if (eventusManager == null)
                    {
                        return;
                    }

                    if (_originalHandler == null)
                    {
                        _originalHandler = _methodCache.CreateDelegate(_manager);
                    }

                    if (_originalHandler != null)
                    {
                        eventusManager.UnregisterHandler(typeof(AudioEventData), _originalHandler);
                    }

                    eventusManager.RegisterHandler(typeof(AudioEventData), _dispatcher);
                }

                public void Uninstall(EventusManager eventusManager)
                {
                    if (eventusManager == null)
                    {
                        return;
                    }

                    eventusManager.UnregisterHandler(typeof(AudioEventData), _dispatcher);

                    if (_originalHandler != null)
                    {
                        eventusManager.RegisterHandler(typeof(AudioEventData), _originalHandler);
                    }
                }

                private void Dispatch(BaseEventData eventData, BaseEventContext context)
                {
                    AudioEventData audioEventData = eventData as AudioEventData;
                    bool handled = TryHandle(_manager, audioEventData, context);
                    if (!handled && (FallbackToOriginalHandler || audioEventData == null))
                    {
                        _originalHandler?.Invoke(eventData, context);
                    }
                }
            }

            private interface IExternalAudioPlayback
            {
                bool TryPlay(AudioManager manager, AudioEventData eventData, BaseEventContext context);
            }

            private sealed class ExternalAudioClipPlayback : IExternalAudioPlayback
            {
                private readonly Func<AudioEventData, BaseEventContext, AudioClip> _clipResolver;
                private readonly float _volume;
                private readonly bool _loop;
                private readonly float _spatialBlend;
                private readonly Action<AudioSource, AudioEventData, BaseEventContext> _configureSource;

                public ExternalAudioClipPlayback(
                    Func<AudioEventData, BaseEventContext, AudioClip> clipResolver,
                    float volume,
                    bool loop,
                    float spatialBlend,
                    Action<AudioSource, AudioEventData, BaseEventContext> configureSource)
                {
                    _clipResolver = clipResolver;
                    _volume = Mathf.Clamp01(volume);
                    _loop = loop;
                    _spatialBlend = Mathf.Clamp01(spatialBlend);
                    _configureSource = configureSource;
                }

                public bool TryPlay(AudioManager manager, AudioEventData eventData, BaseEventContext context)
                {

                    TFTVLogger.Always($"TryPlay: {eventData?.EventDef?.name}");

                    EnsureManagedAudioListener(manager, context);

                    TFTVLogger.Always($"_managedListener==null: {_managedListener==null}");

                    AudioClip clip = _clipResolver(eventData, context);

                    TFTVLogger.Always($"clip {clip == null}");

                    if (clip == null)
                    {
                        return false;
                    }

                    Transform emitter = ResolveEmitter(manager, context);
                    
                    TFTVLogger.Always($"emitter == null {emitter == null}");

                    if (emitter == null)
                    {
                        return false;
                    }

                    AudioSource audioSource = emitter.GetComponent<AudioSource>();

                    TFTVLogger.Always($"emitter?.name: {emitter?.name}");
                    TFTVLogger.Always($"audioSource == null {audioSource == null}");

                  /*  if (audioSource == null)
                    {


                        audioSource = emitter.gameObject.AddComponent<AudioSource>();
                        audioSource.playOnAwake = false;
                    }

                    TFTVLogger.Always($"audioSource.ignoreListenerPause {audioSource?.ignoreListenerPause}");

                    audioSource.ignoreListenerPause = true;
                    audioSource.ignoreListenerVolume = true;

                    if (_fallbackEmitter != null && emitter == _fallbackEmitter.transform)
                    {
                        AudioSource fallbackSource = _fallbackEmitter.GetComponent<AudioSource>();
                        if (fallbackSource != null)
                        {
                            fallbackSource.ignoreListenerPause = true;
                            fallbackSource.ignoreListenerVolume = true;
                        }
                    }*/

                    var go = new GameObject("RuntimeAudio_" + clip.name, typeof(Transform));
                    go.transform.position = new Vector3(10.5f,1,15.5f);
                    var src = go.AddComponent<AudioSource>();

                    src.clip = clip;
                    src.volume = 100;// _volume;
                    src.loop = _loop;
                    src.spatialBlend = _spatialBlend; // 0 = 2D, 1 = 3D
                    src.playOnAwake = false;
                    src.mute = false;
                    src.enabled = true;
                    src.ignoreListenerPause = true;
                    src.ignoreListenerVolume = true;
                    src.PlayOneShot(clip, 100);


                 /*   audioSource.spatialBlend = _spatialBlend;
                    audioSource.loop = _loop;
                    audioSource.volume = _volume;
                    TFTVLogger.Always($"audioSource.spatialBlend: {audioSource.spatialBlend}");
                    TFTVLogger.Always($"_configureSource == null {_configureSource == null}");
                    _configureSource?.Invoke(audioSource, eventData, context);

                    if (_loop)
                    {
                        if (audioSource.clip != clip)
                        {
                            audioSource.Stop();
                            audioSource.clip = clip;
                        }

                        audioSource.Play();
                    }
                    else
                    {
                        audioSource.clip = null;
                        audioSource.PlayOneShot(clip);
                    }*/
                    TFTVLogger.Always($"got all the way to the end");

                    return true;
                }
            }

            private sealed class DelegateAudioPlayback : IExternalAudioPlayback
            {
                private readonly Func<AudioEventData, BaseEventContext, bool> _handler;

                public DelegateAudioPlayback(Func<AudioEventData, BaseEventContext, bool> handler)
                {
                    _handler = handler;
                }

                public bool TryPlay(AudioManager manager, AudioEventData eventData, BaseEventContext context)
                {
                    return _handler(eventData, context);
                }
            }

            private sealed class MethodInfoCache
            {
                private static readonly string[] MethodNames = { "OnPlayEvent", "PlayEvent" };

                private readonly MethodInfo _methodInfo;
                private readonly ParameterInfo[] _parameters;

                private MethodInfoCache(MethodInfo methodInfo)
                {
                    _methodInfo = methodInfo;
                    _parameters = methodInfo?.GetParameters() ?? Array.Empty<ParameterInfo>();
                }

                public static MethodInfoCache ForAudioManager()
                {
                    MethodInfo handlerInvoke = typeof(EventusManager.EventusHandler).GetMethod("Invoke");
                    Type[] handlerParameters = handlerInvoke?.GetParameters()?.Select(p => p.ParameterType).ToArray();

                    MethodInfo method = FindMethod(handlerParameters);

                    if (method == null && handlerParameters != null && handlerParameters.Length > 0)
                    {
                        Type[] audioSpecificParameters = (Type[])handlerParameters.Clone();
                        audioSpecificParameters[0] = typeof(AudioEventData);
                        method = FindMethod(audioSpecificParameters);
                    }

                    if (method == null)
                    {
                        return null;
                    }

                    return new MethodInfoCache(method);
                }

                private static MethodInfo FindMethod(Type[] parameterTypes)
                {
                    if (parameterTypes == null)
                    {
                        return null;
                    }

                    foreach (string name in MethodNames)
                    {
                        MethodInfo method = AccessTools.Method(typeof(AudioManager), name, parameterTypes);
                        if (method != null)
                        {
                            return method;
                        }
                    }

                    return null;
                }

                public EventusManager.EventusHandler CreateDelegate(AudioManager manager)
                {
                    if (_methodInfo == null || manager == null)
                    {
                        return null;
                    }

                    try
                    {
                        EventusManager.EventusHandler handler = (EventusManager.EventusHandler)Delegate.CreateDelegate(
                            typeof(EventusManager.EventusHandler),
                            manager,
                            _methodInfo,
                            throwOnBindFailure: false);

                        if (handler != null)
                        {
                            return handler;
                        }
                    }
                    catch
                    {
                        // Fall through to wrapper creation when a direct delegate cannot be created.
                    }

                    if (_parameters.Length != 2)
                    {
                        return null;
                    }

                    Type eventParameterType = _parameters[0].ParameterType;
                    Type contextParameterType = _parameters[1].ParameterType;

                    return (eventData, context) =>
                    {
                        if (eventParameterType != null && !IsArgumentCompatible(eventParameterType, eventData))
                        {
                            return;
                        }

                        if (contextParameterType != null && !IsArgumentCompatible(contextParameterType, context))
                        {
                            return;
                        }

                        _methodInfo.Invoke(manager, new object[] { eventData, context });
                    };
                }

                private static bool IsArgumentCompatible(Type parameterType, object argument)
                {
                    if (argument == null)
                    {
                        return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
                    }

                    return parameterType.IsInstanceOfType(argument);
                }
            }
        }


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
