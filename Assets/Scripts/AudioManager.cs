using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string DefaultGameBgmPath = "Audio/BGM/bgm";
    private const string TitleBgmPath = DefaultGameBgmPath;
    private const float DefaultFadeDuration = 0.8f;

    private readonly Dictionary<LocationId, string> locationBgmPaths = new Dictionary<LocationId, string>
    {
        { LocationId.Dormitory, DefaultGameBgmPath },
        { LocationId.TeachingBuilding, DefaultGameBgmPath },
        { LocationId.Library, DefaultGameBgmPath },
        { LocationId.Canteen, DefaultGameBgmPath },
        { LocationId.Playground, DefaultGameBgmPath },
        { LocationId.Store, DefaultGameBgmPath },
        { LocationId.ExpressStation, DefaultGameBgmPath },
        { LocationId.TakeoutStation, DefaultGameBgmPath }
    };

    private readonly Dictionary<string, string> sfxAliases = new Dictionary<string, string>
    {
        { "button_click", "Audio/SFX/UI/ButtonClick" },
        { "button_hover", "Audio/SFX/UI/ButtonHover" },
        { "panel_open", "Audio/SFX/UI/PanelOpen" },
        { "panel_close", "Audio/SFX/UI/PanelClose" },
        { "map_move", "Audio/SFX/MapMove" },
        { "purchase", "Audio/SFX/Purchase" },
        { "item_use", "Audio/SFX/ItemUse" },
        { "dialogue_next", "Audio/SFX/DialogueNext" }
    };

    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private readonly HashSet<string> missingClipWarnings = new HashSet<string>();

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private AudioSource sfxSource;
    private Coroutine fadeRoutine;
    private LocationManager subscribedLocationManager;
    private SettingsManager subscribedSettingsManager;
    private string currentBgmPath;
    private float musicVolume = 0.7f;
    private float sfxVolume = 0.8f;
    private float lastAppliedListenerVolume = -1f;

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("AudioManager");
        return obj.AddComponent<AudioManager>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildAudioSources();
        PreloadCommonSFX();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SubscribeSettings();
        TrySubscribeLocationManager();
        PlayDefaultBGMForScene(SceneManager.GetActiveScene().name, 0f);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeSettings();
        UnsubscribeLocationManager();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        TrySubscribeSettings();
    }

    public void PlayBGM(string resourcePath, float fadeDuration = DefaultFadeDuration, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            StopBGM(fadeDuration);
            return;
        }

        AudioClip clip = LoadClip(resourcePath);
        if (clip == null)
        {
            if (resourcePath != DefaultGameBgmPath)
            {
                PlayBGM(DefaultGameBgmPath, fadeDuration, loop);
            }
            return;
        }

        if (currentBgmPath == resourcePath && activeMusicSource.clip == clip && activeMusicSource.isPlaying)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            activeMusicSource.loop = loop;
            activeMusicSource.volume = musicVolume;
            return;
        }

        currentBgmPath = resourcePath;
        inactiveMusicSource.clip = clip;
        inactiveMusicSource.loop = loop;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeBGM(fadeDuration));
    }

    public void PlayCurrentLocationBGM(float fadeDuration = DefaultFadeDuration)
    {
        if (GameState.Instance == null)
        {
            PlayBGM(DefaultGameBgmPath, fadeDuration);
            return;
        }

        PlayLocationBGM(GameState.Instance.CurrentLocation, fadeDuration);
    }

    public void RefreshBGMForActiveScene(float fadeDuration = DefaultFadeDuration)
    {
        EnsureSceneAudioListener();
        TrySubscribeSettings();
        TrySubscribeLocationManager();
        PlayDefaultBGMForScene(SceneManager.GetActiveScene().name, fadeDuration);
    }

    public void PlayLocationBGM(LocationId locationId, float fadeDuration = DefaultFadeDuration)
    {
        string path = locationBgmPaths.TryGetValue(locationId, out string bgmPath) ? bgmPath : DefaultGameBgmPath;
        PlayBGM(path, fadeDuration);
    }

    public void StopBGM(float fadeDuration = DefaultFadeDuration)
    {
        currentBgmPath = string.Empty;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOutActiveBGM(fadeDuration));
    }

    public void PlaySFX(string sfxId, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(sfxId))
        {
            return;
        }

        string resourcePath = sfxAliases.TryGetValue(sfxId, out string aliasPath) ? aliasPath : sfxId;
        AudioClip clip = LoadClip(resourcePath);
        PlaySFXClip(clip, volumeScale);
    }

    public void PlaySFXClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null || sfxVolume <= 0f)
        {
            return;
        }

        sfxSource.volume = 1f;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
    }

    public void RegisterSFXAlias(string sfxId, string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(sfxId) || string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        sfxAliases[sfxId] = resourcePath;
        LoadClip(resourcePath);
    }

    private void PreloadCommonSFX()
    {
        LoadClip("Audio/SFX/UI/ButtonClick");
        LoadClip("Audio/SFX/UI/ButtonHover");
    }

    private void BuildAudioSources()
    {
        musicSourceA = CreateAudioSource("BGM_A");
        musicSourceB = CreateAudioSource("BGM_B");
        sfxSource = CreateAudioSource("SFX");

        musicSourceA.loop = true;
        musicSourceB.loop = true;
        sfxSource.loop = false;
        musicSourceA.priority = 0;
        musicSourceB.priority = 0;
        sfxSource.priority = 64;
        sfxSource.volume = 1f;

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject sourceObject = new GameObject(name);
        sourceObject.transform.SetParent(transform, false);
        sourceObject.hideFlags = HideFlags.HideInHierarchy;

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.hideFlags = HideFlags.HideInInspector;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneAudioListener();
        TrySubscribeSettings();
        TrySubscribeLocationManager();
        PlayDefaultBGMForScene(scene.name, DefaultFadeDuration);
    }

    private void PlayDefaultBGMForScene(string sceneName, float fadeDuration)
    {
        if (sceneName == "SplashScreen" || sceneName == "LoadingScreen")
        {
            StopBGM(0f);
            return;
        }

        if (sceneName == "TitleScreen")
        {
            PlayBGM(TitleBgmPath, fadeDuration);
            return;
        }

        if (sceneName == "GameScene")
        {
            TrySubscribeLocationManager();
            PlayCurrentLocationBGM(fadeDuration);
            return;
        }

        PlayBGM(DefaultGameBgmPath, fadeDuration);
    }

    private void TrySubscribeLocationManager()
    {
        if (LocationManager.Instance == null || subscribedLocationManager == LocationManager.Instance)
        {
            return;
        }

        UnsubscribeLocationManager();
        subscribedLocationManager = LocationManager.Instance;
        subscribedLocationManager.OnLocationChanged += HandleLocationChanged;
    }

    private void UnsubscribeLocationManager()
    {
        if (subscribedLocationManager == null)
        {
            return;
        }

        subscribedLocationManager.OnLocationChanged -= HandleLocationChanged;
        subscribedLocationManager = null;
    }

    private void HandleLocationChanged(LocationId from, LocationId to)
    {
        PlaySFX("map_move");
        PlayLocationBGM(to);
    }

    private void SubscribeSettings()
    {
        TrySubscribeSettings();
        ApplyVolumesFromSettings();
    }

    private void TrySubscribeSettings()
    {
        if (SettingsManager.Instance == null || subscribedSettingsManager == SettingsManager.Instance)
        {
            return;
        }

        UnsubscribeSettings();
        subscribedSettingsManager = SettingsManager.Instance;
        subscribedSettingsManager.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        subscribedSettingsManager.OnSFXVolumeChanged += HandleSFXVolumeChanged;
        ApplyVolumesFromSettings();
    }

    private void UnsubscribeSettings()
    {
        if (subscribedSettingsManager == null)
        {
            return;
        }

        subscribedSettingsManager.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
        subscribedSettingsManager.OnSFXVolumeChanged -= HandleSFXVolumeChanged;
        subscribedSettingsManager = null;
    }

    private void HandleMusicVolumeChanged(float ignored)
    {
        ApplyVolumesFromSettings();
    }

    private void HandleSFXVolumeChanged(float ignored)
    {
        ApplyVolumesFromSettings();
    }

    private void ApplyVolumesFromSettings()
    {
        SettingsManager settingsManager = SettingsManager.Instance;
        if (settingsManager != null)
        {
            settingsManager.EnsureLoaded();
        }

        SettingsData settings = settingsManager != null ? settingsManager.CurrentSettings : null;
        musicVolume = settings != null ? Mathf.Clamp01(settings.GetEffectiveMusicVolume()) : Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.7f));
        sfxVolume = settings != null ? Mathf.Clamp01(settings.GetEffectiveSFXVolume()) : Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 0.8f));
        EnsureAudioListenerIsAudible();

        if (activeMusicSource != null)
        {
            activeMusicSource.volume = activeMusicSource.isPlaying ? musicVolume : 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 1f;
        }
    }

    private AudioClip LoadClip(string resourcePath)
    {
        if (clipCache.TryGetValue(resourcePath, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            clipCache[resourcePath] = clip;
            return clip;
        }

        if (missingClipWarnings.Add(resourcePath))
        {
            Debug.LogWarning($"[AudioManager] Audio clip not found: Resources/{resourcePath}. Add an audio file there to enable this sound.");
        }

        return null;
    }

    private IEnumerator FadeBGM(float duration)
    {
        duration = Mathf.Max(0f, duration);
        AudioSource oldSource = activeMusicSource;
        AudioSource newSource = inactiveMusicSource;

        if (duration <= 0f)
        {
            oldSource.Stop();
            oldSource.volume = 0f;
            newSource.volume = musicVolume;
            SwapMusicSources();
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        float oldStartVolume = oldSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            oldSource.volume = Mathf.Lerp(oldStartVolume, 0f, t);
            newSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        oldSource.Stop();
        oldSource.volume = 0f;
        newSource.volume = musicVolume;
        SwapMusicSources();
        fadeRoutine = null;
    }

    private IEnumerator FadeOutActiveBGM(float duration)
    {
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f || activeMusicSource == null || !activeMusicSource.isPlaying)
        {
            if (activeMusicSource != null)
            {
                activeMusicSource.Stop();
                activeMusicSource.volume = 0f;
            }

            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        float startVolume = activeMusicSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        activeMusicSource.Stop();
        activeMusicSource.volume = 0f;
        fadeRoutine = null;
    }

    private void SwapMusicSources()
    {
        AudioSource oldActive = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = oldActive;
    }

    private void EnsureAudioListenerIsAudible()
    {
        EnsureSceneAudioListener();

        float targetVolume;
        if (SettingsManager.Instance != null && SettingsManager.Instance.CurrentSettings != null)
        {
            targetVolume = SettingsManager.Instance.CurrentSettings.isMuted ? 0f : 1f;
        }
        else if (AudioListener.volume <= 0.001f && musicVolume > 0.001f)
        {
            targetVolume = 1f;
        }
        else
        {
            targetVolume = AudioListener.volume;
        }

        if (!Mathf.Approximately(lastAppliedListenerVolume, targetVolume) || !Mathf.Approximately(AudioListener.volume, targetVolume))
        {
            AudioListener.volume = targetVolume;
            lastAppliedListenerVolume = targetVolume;
        }
    }

    private void EnsureSceneAudioListener()
    {
        if (FindObjectOfType<AudioListener>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
    }
}
