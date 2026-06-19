using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class TitleSceneClickLoader : MonoBehaviour
{
    [SerializeField] private string _targetSceneName = "Loby";

    private bool _isLoading;

    private void Update()
    {
        if (_isLoading)
        {
            return;
        }

        if (!WasClickOrTouchPressed())
        {
            return;
        }

        LoadTargetScene();
    }

    private bool WasClickOrTouchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }
        }
#endif

        return false;
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(_targetSceneName))
        {
            Debug.LogError("TitleSceneClickLoader: Target scene name is missing.");
            return;
        }

        _isLoading = true;
        SceneManager.LoadScene(_targetSceneName);
    }
}
