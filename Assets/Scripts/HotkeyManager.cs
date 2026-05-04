using System;
using System.Collections.Generic;
using UnityEngine;

public enum HotkeyActionId
{
    OpenSettings,
    ToggleInfoPanel,
    ToggleInventory,
    ToggleActionMenu,
    ToggleTalentPanel,
    ToggleMissionPanel,
    ToggleDebugConsole
}

[Serializable]
public struct HotkeyBinding
{
    public KeyCode key;
    public bool ctrl;
    public bool shift;
    public bool alt;

    public HotkeyBinding(KeyCode keyCode, bool requireCtrl = false, bool requireShift = false, bool requireAlt = false)
    {
        key = keyCode;
        ctrl = requireCtrl;
        shift = requireShift;
        alt = requireAlt;
    }

    public bool IsUnbound => key == KeyCode.None;

    public bool MatchesDown()
    {
        if (IsUnbound || !Input.GetKeyDown(key))
        {
            return false;
        }

        return ctrl == IsCtrlHeld()
            && shift == IsShiftHeld()
            && alt == IsAltHeld();
    }

    public string ToDisplayString()
    {
        if (IsUnbound)
        {
            return "未绑定";
        }

        List<string> parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (shift) parts.Add("Shift");
        if (alt) parts.Add("Alt");
        parts.Add(HotkeyManager.GetKeyDisplayName(key));
        return string.Join("+", parts);
    }

    public static bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    public static bool IsShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    public static bool IsAltHeld()
    {
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }
}

public static class HotkeyManager
{
    private static readonly HotkeyActionId[] ConfigurableActions =
    {
        HotkeyActionId.OpenSettings,
        HotkeyActionId.ToggleInfoPanel,
        HotkeyActionId.ToggleInventory,
        HotkeyActionId.ToggleActionMenu,
        HotkeyActionId.ToggleTalentPanel,
        HotkeyActionId.ToggleMissionPanel,
        HotkeyActionId.ToggleDebugConsole
    };

    private static readonly KeyCode[] ModifierKeys =
    {
        KeyCode.LeftControl,
        KeyCode.RightControl,
        KeyCode.LeftShift,
        KeyCode.RightShift,
        KeyCode.LeftAlt,
        KeyCode.RightAlt
    };

    private static readonly KeyCode[] RebindableKeys = BuildRebindableKeys();

    public static IReadOnlyList<HotkeyActionId> GetConfigurableActions()
    {
        return ConfigurableActions;
    }

    public static string GetActionLabel(HotkeyActionId action)
    {
        switch (action)
        {
            case HotkeyActionId.OpenSettings: return "打开设置";
            case HotkeyActionId.ToggleInfoPanel: return "信息面板";
            case HotkeyActionId.ToggleInventory: return "背包";
            case HotkeyActionId.ToggleActionMenu: return "行动菜单";
            case HotkeyActionId.ToggleTalentPanel: return "成长面板";
            case HotkeyActionId.ToggleMissionPanel: return "任务面板";
            case HotkeyActionId.ToggleDebugConsole: return "调试控制台";
            default: return action.ToString();
        }
    }

    public static HotkeyBinding GetDefaultBinding(HotkeyActionId action)
    {
        switch (action)
        {
            case HotkeyActionId.OpenSettings: return new HotkeyBinding(KeyCode.F1);
            case HotkeyActionId.ToggleInfoPanel: return new HotkeyBinding(KeyCode.Tab);
            case HotkeyActionId.ToggleInventory: return new HotkeyBinding(KeyCode.I);
            case HotkeyActionId.ToggleActionMenu: return new HotkeyBinding(KeyCode.Alpha1);
            case HotkeyActionId.ToggleTalentPanel: return new HotkeyBinding(KeyCode.Alpha2);
            case HotkeyActionId.ToggleMissionPanel: return new HotkeyBinding(KeyCode.J);
            case HotkeyActionId.ToggleDebugConsole: return new HotkeyBinding(KeyCode.D, true, true);
            default: return new HotkeyBinding(KeyCode.None);
        }
    }

    public static HotkeyBinding GetBinding(SettingsData settings, HotkeyActionId action)
    {
        if (settings == null)
        {
            return GetDefaultBinding(action);
        }

        switch (action)
        {
            case HotkeyActionId.OpenSettings: return settings.openSettingsHotkey;
            case HotkeyActionId.ToggleInfoPanel: return settings.toggleInfoHotkey;
            case HotkeyActionId.ToggleInventory: return settings.toggleInventoryHotkey;
            case HotkeyActionId.ToggleActionMenu: return settings.toggleActionMenuHotkey;
            case HotkeyActionId.ToggleTalentPanel: return settings.toggleTalentHotkey;
            case HotkeyActionId.ToggleMissionPanel: return settings.toggleMissionHotkey;
            case HotkeyActionId.ToggleDebugConsole: return settings.toggleDebugConsoleHotkey;
            default: return GetDefaultBinding(action);
        }
    }

    public static void SetBinding(SettingsData settings, HotkeyActionId action, HotkeyBinding binding)
    {
        if (settings == null)
        {
            return;
        }

        switch (action)
        {
            case HotkeyActionId.OpenSettings:
                settings.openSettingsHotkey = binding;
                break;
            case HotkeyActionId.ToggleInfoPanel:
                settings.toggleInfoHotkey = binding;
                break;
            case HotkeyActionId.ToggleInventory:
                settings.toggleInventoryHotkey = binding;
                break;
            case HotkeyActionId.ToggleActionMenu:
                settings.toggleActionMenuHotkey = binding;
                break;
            case HotkeyActionId.ToggleTalentPanel:
                settings.toggleTalentHotkey = binding;
                break;
            case HotkeyActionId.ToggleMissionPanel:
                settings.toggleMissionHotkey = binding;
                break;
            case HotkeyActionId.ToggleDebugConsole:
                settings.toggleDebugConsoleHotkey = binding;
                break;
        }
    }

    public static bool IsPressed(HotkeyActionId action)
    {
        SettingsData settings = SettingsManager.Instance != null ? SettingsManager.Instance.CurrentSettings : null;
        HotkeyBinding binding = GetBinding(settings, action);
        return binding.MatchesDown();
    }

    public static string GetDisplayString(SettingsData settings, HotkeyActionId action)
    {
        return GetBinding(settings, action).ToDisplayString();
    }

    public static bool TryGetRebindInput(out HotkeyBinding binding)
    {
        binding = new HotkeyBinding(KeyCode.None);
        bool ctrl = HotkeyBinding.IsCtrlHeld();
        bool shift = HotkeyBinding.IsShiftHeld();
        bool alt = HotkeyBinding.IsAltHeld();

        for (int i = 0; i < RebindableKeys.Length; i++)
        {
            KeyCode key = RebindableKeys[i];
            if (!Input.GetKeyDown(key))
            {
                continue;
            }

            binding = new HotkeyBinding(key, ctrl, shift, alt);
            return true;
        }

        return false;
    }

    public static bool AreEqual(HotkeyBinding a, HotkeyBinding b)
    {
        return a.key == b.key
            && a.ctrl == b.ctrl
            && a.shift == b.shift
            && a.alt == b.alt;
    }

    public static string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.BackQuote: return "~";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Return: return "Enter";
            case KeyCode.KeypadEnter: return "NumEnter";
            case KeyCode.Space: return "Space";
            default: return key.ToString();
        }
    }

    private static KeyCode[] BuildRebindableKeys()
    {
        Array values = Enum.GetValues(typeof(KeyCode));
        List<KeyCode> keys = new List<KeyCode>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            KeyCode key = (KeyCode)values.GetValue(i);
            string keyName = key.ToString();
            if (key == KeyCode.None || Array.IndexOf(ModifierKeys, key) >= 0)
            {
                continue;
            }

            if (keyName.StartsWith("Mouse", StringComparison.Ordinal)
                || keyName.StartsWith("Joystick", StringComparison.Ordinal))
            {
                continue;
            }

            keys.Add(key);
        }

        return keys.ToArray();
    }
}
