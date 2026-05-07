using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEventManager : MonoBehaviour
{
    public static UIEventManager Instance { get; private set; }

    [Header("Event Settings")]
    public bool enableSoundEffects = true;
    public bool enableHapticFeedback = false;

    [Header("Optional Clips")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;

    private const float ButtonScanInterval = 0.5f;

    private readonly Dictionary<Button, ButtonEventData> buttonEvents = new Dictionary<Button, ButtonEventData>();
    private AudioSource fallbackAudioSource;
    private float nextButtonScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static UIEventManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("UIEventManager");
        return obj.AddComponent<UIEventManager>();
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
        InitializeFallbackAudio();
        ScanButtons();
    }

    private void Start()
    {
        ApplySettingsVolume();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSFXVolumeChanged += HandleSfxVolumeChanged;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextButtonScanTime)
        {
            return;
        }

        nextButtonScanTime = Time.unscaledTime + ButtonScanInterval;
        RemoveDestroyedButtons();
        ScanButtons();
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSFXVolumeChanged -= HandleSfxVolumeChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterButtonClick(Button button, Action<Button> callback)
    {
        EnsureButtonRegistered(button);
        if (button != null && buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.AddClickListener(callback);
        }
    }

    public void RegisterButtonHover(Button button, Action<Button> callback)
    {
        EnsureButtonRegistered(button);
        if (button != null && buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.AddHoverListener(callback);
        }
    }

    public void RegisterButtonExit(Button button, Action<Button> callback)
    {
        EnsureButtonRegistered(button);
        if (button != null && buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.AddExitListener(callback);
        }
    }

    public void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        foreach (Button button in buttonEvents.Keys)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    public void NotifyButtonClick(Button button)
    {
        if (!IsUsableButton(button))
        {
            return;
        }

        PlayButtonClickSound();
        TriggerHapticFeedback();

        UIAnimator animator = FindObjectOfType<UIAnimator>();
        if (animator != null)
        {
            animator.ButtonPressEffect(button);
        }

        if (buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.InvokeClick(button);
        }
    }

    public void NotifyButtonHover(Button button)
    {
        if (!IsUsableButton(button))
        {
            return;
        }

        PlayButtonHoverSound();

        if (buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.InvokeHover(button);
        }
    }

    public void NotifyButtonExit(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (buttonEvents.TryGetValue(button, out ButtonEventData eventData))
        {
            eventData.InvokeExit(button);
        }
    }

    private void InitializeFallbackAudio()
    {
        fallbackAudioSource = gameObject.AddComponent<AudioSource>();
        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.spatialBlend = 0f;
        fallbackAudioSource.volume = 0.8f;
    }

    private void ApplySettingsVolume()
    {
        if (fallbackAudioSource == null)
        {
            return;
        }

        SettingsData settings = SettingsManager.Instance != null ? SettingsManager.Instance.CurrentSettings : null;
        fallbackAudioSource.volume = settings != null ? settings.GetEffectiveSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        if (fallbackAudioSource != null)
        {
            fallbackAudioSource.volume = volume;
        }
    }

    private void ScanButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            EnsureButtonRegistered(button);
        }
    }

    private void EnsureButtonRegistered(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (!buttonEvents.ContainsKey(button))
        {
            buttonEvents[button] = new ButtonEventData();
        }

        UIButtonSfxProxy proxy = button.GetComponent<UIButtonSfxProxy>();
        if (proxy == null)
        {
            proxy = button.gameObject.AddComponent<UIButtonSfxProxy>();
        }

        proxy.Configure(this, button);
    }

    private void RemoveDestroyedButtons()
    {
        List<Button> staleButtons = null;
        foreach (Button button in buttonEvents.Keys)
        {
            if (button != null)
            {
                continue;
            }

            if (staleButtons == null)
            {
                staleButtons = new List<Button>();
            }

            staleButtons.Add(button);
        }

        if (staleButtons == null)
        {
            return;
        }

        foreach (Button button in staleButtons)
        {
            buttonEvents.Remove(button);
        }
    }

    private bool IsUsableButton(Button button)
    {
        return button != null && button.IsActive() && button.interactable;
    }

    private void PlayButtonClickSound()
    {
        if (!enableSoundEffects)
        {
            return;
        }

        AudioManager audioManager = AudioManager.EnsureInstance();
        if (audioManager != null)
        {
            if (buttonClickSound != null)
            {
                audioManager.PlaySFXClip(buttonClickSound);
            }
            else
            {
                audioManager.PlaySFX("button_click");
            }

            return;
        }

        if (buttonClickSound != null && fallbackAudioSource != null)
        {
            fallbackAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void PlayButtonHoverSound()
    {
        if (!enableSoundEffects)
        {
            return;
        }

        AudioManager audioManager = AudioManager.EnsureInstance();
        if (audioManager != null)
        {
            if (buttonHoverSound != null)
            {
                audioManager.PlaySFXClip(buttonHoverSound, 0.65f);
            }

            return;
        }

        if (buttonHoverSound != null && fallbackAudioSource != null)
        {
            fallbackAudioSource.PlayOneShot(buttonHoverSound, 0.65f);
        }
    }

    private void TriggerHapticFeedback()
    {
        if (enableHapticFeedback && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
    }

    [Serializable]
    private class ButtonEventData
    {
        private event Action<Button> onClick;
        private event Action<Button> onHover;
        private event Action<Button> onExit;

        public void InvokeClick(Button button)
        {
            onClick?.Invoke(button);
        }

        public void InvokeHover(Button button)
        {
            onHover?.Invoke(button);
        }

        public void InvokeExit(Button button)
        {
            onExit?.Invoke(button);
        }

        public void AddClickListener(Action<Button> callback)
        {
            onClick += callback;
        }

        public void AddHoverListener(Action<Button> callback)
        {
            onHover += callback;
        }

        public void AddExitListener(Action<Button> callback)
        {
            onExit += callback;
        }
    }
}

public class UIButtonSfxProxy : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private UIEventManager manager;
    private Button button;

    public void Configure(UIEventManager eventManager, Button targetButton)
    {
        manager = eventManager;
        button = targetButton;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (manager == null)
        {
            manager = UIEventManager.EnsureInstance();
        }

        manager.NotifyButtonClick(button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager == null)
        {
            manager = UIEventManager.EnsureInstance();
        }

        manager.NotifyButtonHover(button);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager == null)
        {
            manager = UIEventManager.EnsureInstance();
        }

        manager.NotifyButtonExit(button);
    }
}
