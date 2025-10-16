using Base.Audio;
using Base.Entities;
using Base.Eventus;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Eventus;
using PhoenixPoint.Tactical.Eventus.Contexts;
using PhoenixPoint.Tactical.Eventus.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

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
                    (ctx, evt) => clip,
                    volume,
                    loop,
                    spatialBlend,
                    (source, ctx, evt) =>
                    {
                        if (configureSource != null)
                            configureSource(source);
                    });

                LogClipDetails("RegisterClip", clip, eventDef);
                bool clipReady = EnsureClipPlayable(clip, clip.name);
                TFTVLogger.Always($"RegisterClip: EnsureClipPlayable returned {clipReady}");
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
                TFTVLogger.Always($"RegisterClipFromFile: event='{eventDef?.name ?? "<null>"}', file='{filePath}', streamAudio={streamAudio}, audioType={audioType}");
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

                TFTVLogger.Always($"RegisterResolver: registered resolver for '{eventDef.name}' (volume={volume}, loop={loop}, spatialBlend={spatialBlend})");
            }


            private static bool EnsureClipPlayable(AudioClip clip, string clipLabel)
            {
                if (clip == null)
                {
                    TFTVLogger.Always("ExternalAudioInjector: Provided clip is null.");
                    return false;
                }

                string clipName = !string.IsNullOrEmpty(clipLabel) ? clipLabel : clip.name;
                AudioDataLoadState initialState = clip.loadState;
                switch (initialState) 
                {
                    case AudioDataLoadState.Loaded:
                        return true;
                    case AudioDataLoadState.Loading:
                        return true;
                    case AudioDataLoadState.Failed:
                        Debug.LogError($"ExternalAudioInjector: Audio clip '{clipName}' previously failed to load (state={initialState}).");
                        return false;
                }

                bool loadRequested;
               
                try
                {
                    loadRequested = clip.LoadAudioData();
                }

                catch (Exception ex)
                {
                    TFTVLogger.Always($"ExternalAudioInjector: Exception while loading audio clip '{clipName}': {ex}");
                    return false;
                }

                if (!loadRequested)
                {
                    TFTVLogger.Always($"ExternalAudioInjector: LoadAudioData returned false for clip '{clipName}'.");
                    return false;
                }


                AudioDataLoadState state = clip.loadState;
                if (state == AudioDataLoadState.Failed)
                {
                    TFTVLogger.Always($"ExternalAudioInjector: Audio clip '{clipName}' failed to load (state={state}).");
                    return false;
                }

                if (state == AudioDataLoadState.Unloaded)
                {
                    TFTVLogger.Always($"EnsureClipPlayable: clip '{clipName}' reports Unloaded immediately after LoadAudioData; awaiting Unity async load.");
                }

                return true;      
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


                TFTVLogger.Always($"LoadClipFromFile: loading '{absolutePath}' (audioType={audioType}, streamAudio={streamAudio})");

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
                    LogClipDetails("LoadClipFromFile", clip);
                    bool clipReady = EnsureClipPlayable(clip, clip.name);
                    TFTVLogger.Always($"LoadClipFromFile: EnsureClipPlayable returned {clipReady}");
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
                    TFTVLogger.Always("TryHandle: audioEvent is null");
                    return false;
                }

                TacticalEventDef eventDef = audioEvent.EventDef as TacticalEventDef;
                if (eventDef == null)
                {
                    TFTVLogger.Always($"TryHandle: EventDef '{audioEvent.EventDef?.name ?? "<unknown>"}' is not TacticalEventDef");
                    return false;
                }

                TFTVLogger.Always($"TryHandle: resolving handler for event '{eventDef.name}' ({eventDef.GetInstanceID()})");

                if (!Registrations.TryGetValue(eventDef, out IExternalAudioPlayback playback))
                {
                    TFTVLogger.Always($"TryHandle: no registration for '{eventDef.name}'");
                    return false;
                }

                TFTVLogger.Always($"TryHandle: invoking playback for '{eventDef.name}', context type: {context?.GetType().FullName ?? "<null>"}");
                return playback.TryPlay(manager, audioEvent, context ?? new SimpleEventContext(manager.gameObject));
            }

            private static Transform ResolveEmitter(AudioManager manager, BaseEventContext context)
            {
                if (context is IWorldEventContext worldContext && worldContext.TargetTransform)
                {
                    TFTVLogger.Always($"ResolveEmitter: using world target '{worldContext.TargetTransform.name}'");
                    return worldContext.TargetTransform;
                }

                Transform senderTransform = context?.SenderTransform;
                if (senderTransform)
                {
                    TFTVLogger.Always($"ResolveEmitter: using sender transform '{senderTransform.name}'");
                    return senderTransform;
                }

                if (manager != null && manager.transform)
                {
                    TFTVLogger.Always($"ResolveEmitter: using AudioManager transform '{manager.transform.name}'");
                    return manager.transform;
                }

                TFTVLogger.Always("ResolveEmitter: falling back to shared emitter");
                return EnsureFallbackEmitter(manager, context).transform;
            }

            private static GameObject EnsureFallbackEmitter(AudioManager manager, BaseEventContext context)
            {
                if (_fallbackEmitter != null)
                {
                    if (_fallbackEmitter)
                    {
                        TFTVLogger.Always("EnsureFallbackEmitter: reusing existing fallback emitter");
                        SyncFallbackEmitterAnchor(manager, context);
                        EnsureSoundPlayer(_fallbackEmitter.transform);
                        return _fallbackEmitter;
                    }

                    TFTVLogger.Always("EnsureFallbackEmitter: cached fallback emitter destroyed, recreating");
                    _fallbackEmitter = null;
                }

                _fallbackEmitter = new GameObject("ExternalAudioEmitter");
                UnityEngine.Object.DontDestroyOnLoad(_fallbackEmitter);
                _fallbackEmitter.hideFlags = HideFlags.HideAndDontSave;
                TFTVLogger.Always("EnsureFallbackEmitter: created new fallback emitter GameObject");
                EnsureSoundPlayer(_fallbackEmitter.transform);
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
                            TFTVLogger.Always("SyncFallbackEmitterAnchor: reparenting fallback emitter");
                            AudioSource source = EnsureAudioSource(_fallbackEmitter.transform, 1f);
                            if (source != null && source.isPlaying)
                            {
                                TFTVLogger.Always("SyncFallbackEmitterAnchor: stopping playback before reparenting fallback emitter");
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
                            TFTVLogger.Always($"SyncFallbackEmitterAnchor: fallback emitter now anchored to {anchorName}");
                        }
                    }
                }
                else if (_fallbackEmitter.transform.parent != null)
                {
                    TFTVLogger.Always("SyncFallbackEmitterAnchor: clearing fallback emitter parent");
                    _fallbackEmitter.transform.SetParent(null, false);
                    _fallbackAnchorName = null;
                }
            }

            private static Transform ResolveFallbackAnchor(AudioManager manager, BaseEventContext context)
            {
                Camera managerCamera = manager?.GetComponentInChildren<Camera>();
                if (managerCamera != null)
                {
                    TFTVLogger.Always($"ResolveFallbackAnchor: using AudioManager camera '{managerCamera.name}'");
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
                            TFTVLogger.Always($"ResolveFallbackAnchor: using context camera '{contextCamera.name}'");
                            return contextCamera.transform;
                        }
                    }
                }

                if (Camera.main != null)
                {
                    TFTVLogger.Always($"ResolveFallbackAnchor: using Camera.main '{Camera.main.name}'");
                    return Camera.main.transform;
                }

                Camera anyCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                if (anyCamera != null)
                {
                    TFTVLogger.Always($"ResolveFallbackAnchor: using found camera '{anyCamera.name}'");
                    return anyCamera.transform;
                }

                TFTVLogger.Always("ResolveFallbackAnchor: falling back to AudioManager transform");
                return manager != null ? manager.transform : null;
            }

            private static AudioListener EnsureManagedAudioListener(AudioManager manager, BaseEventContext context)
            {
                if (_managedListener != null)
                {
                    if (_managedListener && _managedListener.isActiveAndEnabled)
                    {
                        TFTVLogger.Always($"EnsureManagedAudioListener: reusing active listener '{_managedListener.name}'");
                        ActivateManagedAudioListener(_managedListener);
                        return _managedListener;
                    }

                    TFTVLogger.Always("EnsureManagedAudioListener: cached listener inactive, clearing");
                    _managedListener = null;
                }

                AudioListener listener = (manager?.GetComponentInChildren<AudioListener>()) ?? UnityEngine.Object.FindObjectOfType<AudioListener>();

                if (listener != null && listener.gameObject != _fallbackEmitter)
                {
                    if (!listener.isActiveAndEnabled)
                    {
                        TFTVLogger.Always($"EnsureManagedAudioListener: found listener '{listener.name}' but it is disabled");
                        listener = null;
                    }
                }

                if (listener != null)
                {
                    TFTVLogger.Always($"EnsureManagedAudioListener: using listener '{listener.name}'");
                    ActivateManagedAudioListener(listener);
                    return _managedListener;
                }

                GameObject fallback = EnsureFallbackEmitter(manager, context);
                listener = fallback.GetComponent<AudioListener>();
                if (listener == null)
                {
                    TFTVLogger.Always("EnsureManagedAudioListener: adding AudioListener to fallback emitter");
                    listener = fallback.AddComponent<AudioListener>();
                }

                if (!listener.enabled)
                {
                    TFTVLogger.Always("EnsureManagedAudioListener: enabling fallback AudioListener");
                    listener.enabled = true;
                }

                if (!listener.gameObject.activeSelf)
                {
                    TFTVLogger.Always("EnsureManagedAudioListener: activating fallback AudioListener GameObject");
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

                TFTVLogger.Always($"ActivateManagedAudioListener: listener '{listener.name}'");
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

                TFTVLogger.Always($"ActivateManagedAudioListener: AudioSettings.Mobile.muteState={AudioSettings.Mobile.muteState}");

                if (AudioSettings.Mobile.muteState)
                {
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

            private static void LogClipDetails(string stage, AudioClip clip, TacticalEventDef eventDef = null)
            {
                string eventName = eventDef != null ? eventDef.name : "<unbound>";
                if (clip == null)
                {
                    TFTVLogger.Always($"{stage}: event='{eventName}' clip=<null>");
                    return;
                }

                AudioDataLoadState loadState = clip.loadState;
                TFTVLogger.Always(
                    $"{stage}: event='{eventName}', clip='{clip.name}', loadState={loadState}, length={clip.length}s, samples={clip.samples}, channels={clip.channels}, frequency={clip.frequency}Hz, loadInBackground={clip.loadInBackground}, preloadAudioData={clip.preloadAudioData}");
            }

            private static ParticleSpawnSoundPlayer EnsureSoundPlayer(Transform emitter)
            {
                if (emitter == null)
                {
                    TFTVLogger.Always("EnsureSoundPlayer: emitter is null");
                    return null;
                }

                ParticleSpawnSoundPlayer soundPlayer = emitter.GetComponent<ParticleSpawnSoundPlayer>();
                if (soundPlayer == null)
                {
                    TFTVLogger.Always($"EnsureSoundPlayer: adding ParticleSpawnSoundPlayer to '{emitter.name}'");
                    soundPlayer = emitter.gameObject.AddComponent<ParticleSpawnSoundPlayer>();
                    soundPlayer.DoNotRepeatClips = false;
                    soundPlayer.WaitForClipToEnd = false;
                }
                else
                {
                    TFTVLogger.Always($"EnsureSoundPlayer: found existing ParticleSpawnSoundPlayer on '{emitter.name}'");
                }

                return soundPlayer;
            }

            private static AudioSource EnsureAudioSource(Transform emitter, float volume)
            {
                ParticleSpawnSoundPlayer soundPlayer = EnsureSoundPlayer(emitter);
                AudioSource audioSource = soundPlayer?.AudioSource;
                if (audioSource == null)
                {
                    TFTVLogger.Always($"EnsureAudioSource: ParticleSpawnSoundPlayer on '{emitter?.name ?? "<null>"}' has no AudioSource");
                    return null;
                }

                soundPlayer.Volume = volume;
                audioSource.playOnAwake = false;
                audioSource.volume = volume;
                TFTVLogger.Always($"EnsureAudioSource: configured AudioSource on '{emitter.name}' with volume {volume}");
                return audioSource;
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
                    TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: entered playback pipeline");
                    EnsureManagedAudioListener(manager, context);

                    TacticalEventDef tacticalEvent = eventData?.EventDef as TacticalEventDef;
                    AudioClip clip = _clipResolver(eventData, context);
                    if (clip == null)
                    {
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: clip resolver returned null");
                        return false;
                    }

                    LogClipDetails("ExternalAudioClipPlayback.TryPlay: resolver result", clip, tacticalEvent);
                    if (!EnsureClipPlayable(clip, clip.name))
                    {
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: clip is not ready for playback");
                        return false;
                    }
                    if (clip.loadState != AudioDataLoadState.Loaded)
                    {
                        AudioDataLoadState playbackState = clip.loadState;
                        Debug.LogError($"ExternalAudioInjector: Audio clip '{clip.name}' is not ready (state={playbackState}). Skipping playback.");
                        TFTVLogger.Always($"ExternalAudioClipPlayback.TryPlay: clip '{clip.name}' not ready for playback (state={playbackState})");
                        return false;
                    }

                    Transform emitter = ResolveEmitter(manager, context);
                    if (emitter == null)
                    {
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: ResolveEmitter returned null");
                        return false;
                    }

                    TFTVLogger.Always($"ExternalAudioClipPlayback.TryPlay: using emitter '{emitter.name}'");
                    AudioSource audioSource = EnsureAudioSource(emitter, _volume);
                    if (audioSource == null)
                    {
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: EnsureAudioSource returned null");
                        return false;
                    }

                    audioSource.ignoreListenerPause = true;
                    audioSource.ignoreListenerVolume = true;
                    audioSource.spatialBlend = _spatialBlend;
                    audioSource.loop = _loop;
                    audioSource.volume = _volume;
                    TFTVLogger.Always($"ExternalAudioClipPlayback.TryPlay: configured AudioSource (loop={_loop}, spatialBlend={_spatialBlend}, volume={_volume})");
                    _configureSource?.Invoke(audioSource, eventData, context);

                    if (_loop)
                    {
                        if (audioSource.clip != clip)
                        {
                            TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: assigning looping clip and restarting playback");
                            audioSource.Stop();
                            audioSource.clip = clip;
                        }

                        audioSource.Play();
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: started looping playback");
                    }
                    else
                    {
                        audioSource.clip = null;
                        TFTVLogger.Always("ExternalAudioClipPlayback.TryPlay: firing one-shot clip");
                        audioSource.PlayOneShot(clip, _volume);
                    }

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
