using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MdmUnity.Missions
{
    public class MissionFlowManager : MonoBehaviour
    {
        private const string ReadyAction = "READY";
        private const string RetryAction = "RETRY";
        private const string NextSceneAction = "NEXT_SCENE";
        private const string EndingAction = "ENDING";

        [Serializable]
        private class SceneMissionEntry
        {
            [SerializeField] private string _sceneId;
            [SerializeField] private string _unitySceneName;
            [SerializeField] private MissionDefinition _missionDefinition;

            public string SceneId => _sceneId;
            public string UnitySceneName => _unitySceneName;
            public MissionDefinition MissionDefinition => _missionDefinition;
        }

        [Serializable]
        private class VoiceLineEntry
        {
            [SerializeField] private string _message;
            [SerializeField] private AudioClip _clip;

            public string Message => _message;
            public AudioClip Clip => _clip;
        }

        [SerializeField] private MissionDefinition _currentMissionDefinition;
        [SerializeField] private MissionPostManager _missionPostManager;
        [SerializeField] private MissionResponseManager _missionResponseManager;
        [SerializeField] private MonoBehaviour _poseCaptureBehaviour;
        [SerializeField] private UnityEngine.Object _messageText;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private Texture2D _endingTexture;
        [SerializeField] private VoiceLineEntry[] _voiceLines;
        [Range(0.0f, 2.0f)] [SerializeField] private float _voiceVolume = 1.0f;
        [SerializeField] private SceneMissionEntry[] _sceneMissions;
        [SerializeField] private bool _requestBeforeMissionOnStart = true;
        [SerializeField] private bool _quitOnEnding = true;
        [Min(0.0f)] [SerializeField] private float _readyDelaySec = 0.5f;
        [Min(0.0f)] [SerializeField] private float _retryDelaySec = 0.5f;
        [Min(0.0f)] [SerializeField] private float _nextSceneDelaySec = 1.0f;
        [Min(0.0f)] [SerializeField] private float _nextSceneFadeDurationSec = 1.0f;
        [Min(0.0f)] [SerializeField] private float _endingFadeDurationSec = 2.0f;
        [Min(0.0f)] [SerializeField] private float _endingImageFadeDurationSec = 1.0f;
        [Min(0.0f)] [SerializeField] private float _endingImageDisplayDurationSec = 5.0f;
        [Min(1.0f)] [SerializeField] private float _typewriterCharactersPerSecond = 25.0f;

        private IMissionPoseCapture _poseCapture;
        private CanvasGroup _endingImageCanvasGroup;
        private RawImage _endingRawImage;
        private AudioSource _voiceAudioSource;
        private Coroutine _actionCoroutine;

        private void Awake()
        {
            if (_missionPostManager == null)
            {
                _missionPostManager = FindAnyObjectByType<MissionPostManager>();
            }

            if (_missionResponseManager == null)
            {
                _missionResponseManager = FindAnyObjectByType<MissionResponseManager>();
            }

            ResolvePoseCapture();
            ApplyCurrentMissionDefinition();
            InitializeFade();
            InitializeVoiceAudioSource();
        }

        private void OnEnable()
        {
            if (_missionResponseManager != null)
            {
                _missionResponseManager.ResponseUpdated += HandleResponseUpdated;
            }
        }

        private void OnDisable()
        {
            if (_missionResponseManager != null)
            {
                _missionResponseManager.ResponseUpdated -= HandleResponseUpdated;
            }
        }

        private void Start()
        {
            if (_requestBeforeMissionOnStart)
            {
                RequestBeforeMission();
            }
        }

        public void RequestBeforeMission()
        {
            if (_missionPostManager == null)
            {
                Debug.LogWarning("MissionFlowManager: MissionPostManager reference is missing.");
                return;
            }

            ApplyCurrentMissionDefinition();
            _missionPostManager.CreateBeforeMissionRequest();
        }

        public void SetCurrentMissionDefinition(MissionDefinition missionDefinition)
        {
            _currentMissionDefinition = missionDefinition;
            ApplyCurrentMissionDefinition();
        }

        private void HandleResponseUpdated(MissionCheckResponse response)
        {
            if (response == null)
            {
                return;
            }

            string nextAction = NormalizeAction(response.nextAction);
            if (string.IsNullOrEmpty(nextAction))
            {
                Debug.LogWarning("MissionFlowManager: nextAction is empty.");
                return;
            }

            StartActionRoutine(ShowMessageAndHandleNextAction(response, nextAction));
        }

        private IEnumerator ShowMessageAndHandleNextAction(MissionCheckResponse response, string nextAction)
        {
            yield return ShowMessageRoutine(response.message);
            yield return HandleNextAction(response, nextAction);
        }

        private IEnumerator ShowMessageRoutine(string message)
        {
            if (_messageText == null)
            {
                yield break;
            }

            string text = message ?? string.Empty;
            SetMessageText(string.Empty);

            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            PlayVoiceForMessage(text);

            float delaySec = 1.0f / Mathf.Max(1.0f, _typewriterCharactersPerSecond);
            WaitForSeconds wait = new WaitForSeconds(delaySec);

            for (int i = 0; i < text.Length; i++)
            {
                SetMessageText(text.Substring(0, i + 1));
                yield return wait;
            }
        }

        private void PlayVoiceForMessage(string message)
        {
            AudioClip clip = FindVoiceClip(message);
            if (clip == null)
            {
                return;
            }

            InitializeVoiceAudioSource();
            if (_voiceAudioSource == null)
            {
                Debug.LogWarning("MissionFlowManager: voice AudioSource is missing.");
                return;
            }

            _voiceAudioSource.Stop();
            _voiceAudioSource.volume = 1.0f;
            _voiceAudioSource.PlayOneShot(clip, _voiceVolume);
        }

        private void InitializeVoiceAudioSource()
        {
            if (_voiceAudioSource == null)
            {
                _voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }

            _voiceAudioSource.playOnAwake = false;
            _voiceAudioSource.loop = false;
            _voiceAudioSource.spatialBlend = 0.0f;
            _voiceAudioSource.dopplerLevel = 0.0f;
        }

        private AudioClip FindVoiceClip(string message)
        {
            if (_voiceLines == null || string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            string normalizedMessage = message.Trim();
            for (int i = 0; i < _voiceLines.Length; i++)
            {
                VoiceLineEntry entry = _voiceLines[i];
                if (entry == null || entry.Clip == null || string.IsNullOrWhiteSpace(entry.Message))
                {
                    continue;
                }

                if (string.Equals(entry.Message.Trim(), normalizedMessage, StringComparison.Ordinal))
                {
                    return entry.Clip;
                }
            }

            return null;
        }

        private void SetMessageText(string message)
        {
            Component messageComponent = _messageText as Component;
            if (messageComponent == null)
            {
                Debug.LogWarning("MissionFlowManager: Message Text reference is not a Component.");
                return;
            }

            System.Reflection.PropertyInfo textProperty = messageComponent.GetType().GetProperty("text");
            if (textProperty == null || !textProperty.CanWrite)
            {
                Debug.LogWarning("MissionFlowManager: Message Text component does not expose writable text property.");
                return;
            }

            textProperty.SetValue(messageComponent, message ?? string.Empty);
        }

        private IEnumerator HandleNextAction(MissionCheckResponse response, string nextAction)
        {
            switch (nextAction)
            {
                case ReadyAction:
                    yield return new WaitForSeconds(_readyDelaySec);
                    StartMissionCapture();
                    break;

                case RetryAction:
                    yield return new WaitForSeconds(_retryDelaySec);
                    StartMissionCapture();
                    break;

                case NextSceneAction:
                    yield return MoveToNextSceneRoutine(response.nextSceneId);
                    break;

                case EndingAction:
                    if (_quitOnEnding)
                    {
                        yield return PlayEndingRoutine();
                    }

                    break;

                default:
                    Debug.LogWarning($"MissionFlowManager: unsupported nextAction={nextAction}");
                    break;
            }
        }

        private void StartMissionCapture()
        {
            ResolvePoseCapture();

            if (_poseCapture == null)
            {
                Debug.LogWarning("MissionFlowManager: pose capture reference is missing.");
                return;
            }

            if (_poseCapture.IsCapturing)
            {
                Debug.LogWarning("MissionFlowManager: pose capture is already running.");
                return;
            }

            _poseCapture.StartCapture();
        }

        private IEnumerator MoveToNextSceneRoutine(string nextSceneId)
        {
            if (string.IsNullOrWhiteSpace(nextSceneId))
            {
                Debug.LogWarning("MissionFlowManager: nextSceneId is empty.");
                yield break;
            }

            SceneMissionEntry entry = FindSceneMissionEntry(nextSceneId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.UnitySceneName))
            {
                Debug.LogWarning($"MissionFlowManager: scene mapping is missing. nextSceneId={nextSceneId}");
                yield break;
            }

            if (entry.MissionDefinition != null)
            {
                SetCurrentMissionDefinition(entry.MissionDefinition);
            }

            yield return new WaitForSeconds(_nextSceneDelaySec);
            yield return FadeOutRoutine(_nextSceneFadeDurationSec);
            SceneManager.LoadScene(entry.UnitySceneName);
        }

        private IEnumerator PlayEndingRoutine()
        {
            if (_fadeCanvasGroup == null)
            {
                yield return new WaitForSeconds(_endingFadeDurationSec);
                QuitGame();
                yield break;
            }

            yield return FadeOutRoutine(_endingFadeDurationSec);

            if (_endingTexture != null)
            {
                PrepareEndingImage();
                yield return FadeCanvasGroupRoutine(_endingImageCanvasGroup, 0.0f, 1.0f, _endingImageFadeDurationSec);
                yield return new WaitForSeconds(_endingImageDisplayDurationSec);
            }

            QuitGame();
        }

        private IEnumerator FadeOutRoutine(float durationSec)
        {
            if (_fadeCanvasGroup == null)
            {
                yield break;
            }

            _fadeCanvasGroup.blocksRaycasts = true;
            _fadeCanvasGroup.interactable = true;
            yield return FadeCanvasGroupRoutine(_fadeCanvasGroup, _fadeCanvasGroup.alpha, 1.0f, durationSec);
        }

        private IEnumerator FadeCanvasGroupRoutine(CanvasGroup canvasGroup, float startAlpha, float targetAlpha, float durationSec)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float elapsedTime = 0.0f;
            float duration = Mathf.Max(0.001f, durationSec);
            canvasGroup.alpha = startAlpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void PrepareEndingImage()
        {
            if (_endingImageCanvasGroup != null)
            {
                _endingImageCanvasGroup.alpha = 0.0f;
                _endingImageCanvasGroup.gameObject.SetActive(true);
                _endingImageCanvasGroup.transform.SetAsLastSibling();
                return;
            }

            GameObject endingImageObject = new GameObject("Ending Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CanvasGroup));
            endingImageObject.transform.SetParent(_fadeCanvasGroup.transform.parent, false);
            endingImageObject.transform.SetAsLastSibling();

            RectTransform rectTransform = endingImageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _endingRawImage = endingImageObject.GetComponent<RawImage>();
            _endingRawImage.texture = _endingTexture;
            _endingRawImage.color = Color.white;
            _endingRawImage.raycastTarget = false;

            _endingImageCanvasGroup = endingImageObject.GetComponent<CanvasGroup>();
            _endingImageCanvasGroup.alpha = 0.0f;
            _endingImageCanvasGroup.blocksRaycasts = false;
            _endingImageCanvasGroup.interactable = false;
        }

        private SceneMissionEntry FindSceneMissionEntry(string sceneId)
        {
            if (_sceneMissions == null)
            {
                return null;
            }

            for (int i = 0; i < _sceneMissions.Length; i++)
            {
                SceneMissionEntry entry = _sceneMissions[i];
                if (entry != null && string.Equals(entry.SceneId, sceneId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private void ResolvePoseCapture()
        {
            if (_poseCaptureBehaviour != null)
            {
                _poseCapture = _poseCaptureBehaviour as IMissionPoseCapture;
                if (_poseCapture == null)
                {
                    Debug.LogWarning("MissionFlowManager: Pose Capture Behaviour does not implement IMissionPoseCapture.");
                }

                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMissionPoseCapture poseCapture)
                {
                    _poseCaptureBehaviour = behaviours[i];
                    _poseCapture = poseCapture;
                    return;
                }
            }
        }

        private void ApplyCurrentMissionDefinition()
        {
            if (_missionPostManager != null && _currentMissionDefinition != null)
            {
                _missionPostManager.SetMissionDefinition(_currentMissionDefinition);
            }
        }

        private void InitializeFade()
        {
            if (_fadeCanvasGroup == null)
            {
                return;
            }

            _fadeCanvasGroup.alpha = 0.0f;
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }

        private void StartActionRoutine(IEnumerator routine)
        {
            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
            }

            _actionCoroutine = StartCoroutine(routine);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private string NormalizeAction(string nextAction)
        {
            return string.IsNullOrWhiteSpace(nextAction) ? string.Empty : nextAction.Trim().ToUpperInvariant();
        }
    }
}







