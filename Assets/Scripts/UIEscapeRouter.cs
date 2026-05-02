using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class UIEscapeRouter : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || PauseMenuUI.WasEscapeHandledThisFrame())
        {
            return;
        }

        if (UIBackActionRouter.TryHandleBackAction())
        {
            return;
        }

        PauseMenuUI.TryOpenFromEscape();
    }
}
