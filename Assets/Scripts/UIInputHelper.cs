using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class UIInputHelper
{
    public static bool IsConfirmPressed()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    public static void FocusSelectable(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        selectable.Select();
    }

    public static bool TryClick(Button button)
    {
        if (button == null || !button.IsActive() || !button.interactable)
        {
            return false;
        }

        button.onClick.Invoke();
        return true;
    }
}
