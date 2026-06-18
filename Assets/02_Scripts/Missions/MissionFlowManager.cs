using System;
using System.Collections;
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

        [SerializeField] private MissionDefinition _currentMissionDefinition;
        [SerializeField] private MissionPostManager _missionPostManager;
        [SerializeField] private MissionResponseManager _missionResponseManager;
        [SerializeField] private MonoBehaviour _poseCaptureBehaviour;
        [SerializeField] private Text _messageText;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private SceneMissionEntry[] _sceneMissions;
        [SerializeField] private bool _requestBeforeMissionOnStart = true;
        [Min(0.0f)] [SerializeField] private float _readyDelaySec = 0.5f;
        [Min(0.0f)] [SerializeField] private float _retryDelaySec = 0.5f;
        [Min(0.0f)] [SerializeField] private float _nextSceneDelaySec = 0.5f;
        [Min(0.0f)] [SerializeField] private float _endingFadeDurationSec = 2.0f;

        private IMissionPoseCapture _poseCapture;
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

            ShowMessage(response.message);
            string nextAction = NormalizeAction(response.nextAction);

            if (string.IsNullOrEmpty(nextAction))
            {
                Debug.LogWarning("MissionFlowManager: nextAction is empty.");
                return;
            }

            StartActionRoutine(HandleNextAction(response, nextAction));
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
                    yield return new WaitForSeconds(_nextSceneDelaySec);
                    MoveToNextScene(response.nextSceneId);
                    break;

                case EndingAction:
                    yield return FadeOutAndQuit();
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

        private void MoveToNextScene(string nextSceneId)
        {
            if (string.IsNullOrWhiteSpace(nextSceneId))
            {
                Debug.LogWarning("MissionFlowManager: nextSceneId is empty.");
                return;
            }

            SceneMissionEntry entry = FindSceneMissionEntry(nextSceneId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.UnitySceneName))
            {
                Debug.LogWarning($"MissionFlowManager: scene mapping is missing. nextSceneId={nextSceneId}");
                return;
            }

            SceneManager.LoadScene(entry.UnitySceneName);
        }

        private IEnumerator FadeOutAndQuit()
        {
            if (_fadeCanvasGroup == null)
            {
                yield return new WaitForSeconds(_endingFadeDurationSec);
                Application.Quit();
                yield break;
            }

            _fadeCanvasGroup.blocksRaycasts = true;
            _fadeCanvasGroup.interactable = true;

            float elapsedTime = 0.0f;
            float startAlpha = _fadeCanvasGroup.alpha;
            float duration = Mathf.Max(0.001f, _endingFadeDurationSec);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1.0f, elapsedTime / duration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = 1.0f;
            Application.Quit();
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

        private void ShowMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message ?? string.Empty;
            }
        }

        private void StartActionRoutine(IEnumerator routine)
        {
            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
            }

            _actionCoroutine = StartCoroutine(routine);
        }

        private string NormalizeAction(string nextAction)
        {
            return string.IsNullOrWhiteSpace(nextAction) ? string.Empty : nextAction.Trim().ToUpperInvariant();
        }
    }
}



