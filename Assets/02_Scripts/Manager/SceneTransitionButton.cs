using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneTransitionButton : MonoBehaviour
{
    public string targetSceneName = "Scene_01";

    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickTransition);
        }
    }

    public void OnClickTransition()
    {
        // 씬 전환 애니메이션이 있다면 여기서 실행하거나
        // 단순히 다음 씬을 로드합니다.
        SceneManager.LoadScene(targetSceneName);
    }
}
