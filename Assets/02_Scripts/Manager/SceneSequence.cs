using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneSequence : MonoBehaviour
{
    [Header("UI References")]
    public Image displayImage;
    public GameObject sequenceCanvas;

    [Header("Story Images")]
    public Sprite[] storySprites;

    [Header("Sequence Settings")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private bool _fadeInAfterSequence = true;
    [Min(0.0f)] [SerializeField] private float _fadeInDurationSec = 1.0f;
    [SerializeField] private UnityEvent _sequenceCompleted;

    private int _currentIndex = 0;
    private Coroutine _sequenceCoroutine;
    private bool _hasPlayed = false;

    private void Start()
    {
        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(false);
        }

        InitializeFade();

        if (_playOnStart)
        {
            StartSequence();
        }
    }

    public void StartSequence()
    {
        if (_hasPlayed)
        {
            return;
        }

        if (storySprites == null || storySprites.Length == 0)
        {
            Debug.LogError("SceneSequence: Story sprites are missing.");
            return;
        }

        if (displayImage == null)
        {
            Debug.LogError("SceneSequence: Display image reference is missing.");
            return;
        }

        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
        }

        _hasPlayed = true;
        _sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(true);
        }

        for (_currentIndex = 0; _currentIndex < storySprites.Length; _currentIndex++)
        {
            displayImage.sprite = storySprites[_currentIndex];

            bool isLastImage = _currentIndex == storySprites.Length - 1;
            float waitTime = isLastImage ? 2.0f : 1.5f;

            yield return new WaitForSeconds(waitTime);
        }

        yield return EndSequenceRoutine();
    }

    private IEnumerator EndSequenceRoutine()
    {
        _sequenceCoroutine = null;

        if (sequenceCanvas != null)
        {
            sequenceCanvas.SetActive(false);
        }

        if (_fadeInAfterSequence)
        {
            yield return FadeInRoutine();
        }

        _sequenceCompleted?.Invoke();
    }

    private void InitializeFade()
    {
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        _fadeCanvasGroup.alpha = 0.0f;
        _fadeCanvasGroup.interactable = false;
        _fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeInRoutine()
    {
        if (_fadeCanvasGroup == null)
        {
            yield break;
        }

        _fadeCanvasGroup.alpha = 1.0f;
        _fadeCanvasGroup.interactable = true;
        _fadeCanvasGroup.blocksRaycasts = true;

        float elapsedTime = 0.0f;
        float duration = Mathf.Max(0.001f, _fadeInDurationSec);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / duration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = 0.0f;
        _fadeCanvasGroup.interactable = false;
        _fadeCanvasGroup.blocksRaycasts = false;
    }
}
