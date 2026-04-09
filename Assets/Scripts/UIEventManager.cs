using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class UIEventManager : MonoBehaviour
{
    [Header("Event Settings")]
    [Tooltip("是否启用音效反馈")]
    public bool enableSoundEffects = true;
    
    [Tooltip("是否启用震动反馈")]
    public bool enableHapticFeedback = true;
    
    [Tooltip("按钮点击音效")]
    public AudioClip buttonClickSound;
    
    [Tooltip("按钮悬停音效")]
    public AudioClip buttonHoverSound;
    
    [Header("References")]
    private AudioSource audioSource;
    private Dictionary<Button, ButtonEventData> buttonEvents = new Dictionary<Button, ButtonEventData>();
    
    private void Awake()
    {
        InitializeAudio();
        FindAllButtons();
    }
    
    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
    }
    
    private void FindAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            SetupButtonEvents(button);
        }
    }
    
    private void SetupButtonEvents(Button button)
    {
        ButtonEventData eventData = new ButtonEventData();
        
        // 注册点击事件
        button.onClick.AddListener(() => OnButtonClick(button));
        
        // 获取Button的Selectable组件来注册悬停事件
        Selectable selectable = button.GetComponent<Selectable>();
        if (selectable != null)
        {
            // 使用EventTrigger来处理悬停事件
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }
            
            // 添加进入事件
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnButtonHover(button));
            trigger.triggers.Add(enterEntry);
            
            // 添加离开事件
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnButtonExit(button));
            trigger.triggers.Add(exitEntry);
        }
        
        buttonEvents[button] = eventData;
    }
    
    #region Button Event Handlers
    
    private void OnButtonClick(Button button)
    {
        PlayButtonClickSound();
        TriggerHapticFeedback();
        
        // 触发按钮按下动画
        UIAnimator animator = FindObjectOfType<UIAnimator>();
        if (animator != null)
        {
            animator.ButtonPressEffect(button);
        }
        
        // 触发自定义事件
        ButtonEventData eventData = buttonEvents[button];
        eventData.InvokeClick(button);
        
        Debug.Log($"Button clicked: {button.gameObject.name}");
    }
    
    private void OnButtonHover(Button button)
    {
        PlayButtonHoverSound();
        
        // 触发悬停动画效果
        ButtonEventData eventData = buttonEvents[button];
        eventData.InvokeHover(button);
        
        Debug.Log($"Button hovered: {button.gameObject.name}");
    }
    
    private void OnButtonExit(Button button)
    {
        // 触发离开动画效果
        ButtonEventData eventData = buttonEvents[button];
        eventData.InvokeExit(button);
        
        Debug.Log($"Button exited: {button.gameObject.name}");
    }
    
    #endregion
    
    #region Feedback Methods
    
    private void PlayButtonClickSound()
    {
        if (enableSoundEffects && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    private void PlayButtonHoverSound()
    {
        if (enableSoundEffects && buttonHoverSound != null)
        {
            audioSource.PlayOneShot(buttonHoverSound);
        }
    }
    
    private void TriggerHapticFeedback()
    {
        if (enableHapticFeedback && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
    }
    
    #endregion
    
    #region Public API
    
    public void RegisterButtonClick(Button button, Action<Button> callback)
    {
        if (buttonEvents.ContainsKey(button))
        {
            buttonEvents[button].AddClickListener(callback);
        }
        else
        {
            Debug.LogWarning($"Button {button.gameObject.name} not found in event manager");
        }
    }
    
    public void RegisterButtonHover(Button button, Action<Button> callback)
    {
        if (buttonEvents.ContainsKey(button))
        {
            buttonEvents[button].AddHoverListener(callback);
        }
        else
        {
            Debug.LogWarning($"Button {button.gameObject.name} not found in event manager");
        }
    }
    
    public void RegisterButtonExit(Button button, Action<Button> callback)
    {
        if (buttonEvents.ContainsKey(button))
        {
            buttonEvents[button].AddExitListener(callback);
        }
        else
        {
            Debug.LogWarning($"Button {button.gameObject.name} not found in event manager");
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
            button.interactable = interactable;
        }
    }
    
    #endregion
    
    #region Button Event Data Class
    
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
    
    #endregion
}