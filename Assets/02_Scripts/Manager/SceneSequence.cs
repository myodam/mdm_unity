using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneSequence : MonoBehaviour
{
    [Header("UI References")]
    public Image displayImage;
    public GameObject sequenceCanvas;

    [Header("Story Images")]
    public Sprite[] storySprites;

    private int currentIndex = 0;
    private Coroutine sequenceCoroutine;

    void Start()
    {
        // 씬 시작 시 이미지를 가려둡니다.
        // Canvas 전체가 아니라 Image 오브젝트를 가려야 버튼이 보입니다.
        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(false);
        }
    }

    public void StartSequence()
    {
        Debug.Log("StartSequence() 버튼이 클릭되었습니다!");

        if (storySprites == null || storySprites.Length == 0)
        {
            Debug.LogError("스토리 이미지가 등록되지 않았습니다! Inspector에서 Story Sprites에 이미지를 넣어주세요.");
            return;
        }

        if (displayImage == null)
        {
            Debug.LogError("Display Image가 등록되지 않았습니다!");
            return;
        }

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }
        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        Debug.Log("이미지 시퀀스 재생 시작");

        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(true);
        }

        for (currentIndex = 0; currentIndex < storySprites.Length; currentIndex++)
        {
            displayImage.sprite = storySprites[currentIndex];
            
            bool isLastImage = (currentIndex == storySprites.Length - 1);
            float waitTime = isLastImage ? 2.0f : 1.5f;

            Debug.Log($"{currentIndex + 1}번째 이미지 표시 중... ({waitTime}초 대기)");
            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("시퀀스 종료");
        EndSequence();
    }

    private void EndSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(false);
        }
    }
}
