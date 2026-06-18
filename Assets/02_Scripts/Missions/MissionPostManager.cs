using System;
using System.IO;
using UnityEngine;

namespace MdmUnity.Missions
{
    public class MissionPostManager : MonoBehaviour
    {
        [SerializeField] private MissionDefinition _missionDefinition;
        [SerializeField] private BackendApiClient _backendApiClient;
        [SerializeField] private MissionResponseManager _missionResponseManager;
        [SerializeField] private bool _saveRequestJson = true;
        [SerializeField] private bool _postWhenEndpointAvailable = true;
        [SerializeField] private string _requestDirectoryName = "MissionRequests";

        private string _lastRequestJson = string.Empty;
        private string _lastRequestFilePath = string.Empty;

        public MissionDefinition MissionDefinition => _missionDefinition;
        public string LastRequestJson => _lastRequestJson;
        public string LastRequestFilePath => _lastRequestFilePath;

        public void SetMissionDefinition(MissionDefinition missionDefinition)
        {
            _missionDefinition = missionDefinition;
        }

        private void Awake()
        {
            if (_backendApiClient == null)
            {
                _backendApiClient = FindAnyObjectByType<BackendApiClient>();
            }


        }

        [ContextMenu("Create Before Mission Request")]
        public void CreateBeforeMissionRequest()
        {
            if (!HasMissionDefinition())
            {
                return;
            }

            MissionPoseRequest request = MissionRequestBuilder.BuildBeforeMission(_missionDefinition);
            SubmitRequest("before_mission", request);
        }

        public string CreateMissionResultRequest(MissionPoseFrame[] poseFrames)
        {
            if (!HasMissionDefinition())
            {
                return string.Empty;
            }

            MissionPoseRequest request = MissionRequestBuilder.BuildMissionResult(_missionDefinition, poseFrames);
            return SubmitRequest("mission_result", request);
        }

        private bool HasMissionDefinition()
        {
            if (_missionDefinition != null)
            {
                return true;
            }

            Debug.LogWarning("MissionPostManager: MissionDefinition reference is missing.");
            return false;
        }

        private string SubmitRequest(string requestName, MissionPoseRequest request)
        {
            _lastRequestJson = JsonUtility.ToJson(request, true);

            if (_saveRequestJson)
            {
                _lastRequestFilePath = SaveRequestJson(requestName, _lastRequestJson);
                Debug.Log($"MissionPostManager: request JSON saved. type={requestName}, path={_lastRequestFilePath}");
            }
            else
            {
                _lastRequestFilePath = string.Empty;
            }

            if (_postWhenEndpointAvailable && _backendApiClient != null)
            {
                if (_backendApiClient.HasMissionEndpoint)
                {
                    StartCoroutine(_backendApiClient.PostMissionRequestJson(_lastRequestJson, HandlePostSuccess, HandlePostFailure));
                }
                else
                {
                    Debug.Log("MissionPostManager: endpoint URL is empty. Request JSON was created but POST was skipped.");
                }
            }

            return _lastRequestFilePath;
        }

        private string SaveRequestJson(string requestName, string json)
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, _requestDirectoryName);
            Directory.CreateDirectory(directoryPath);

            string safeRequestName = string.IsNullOrWhiteSpace(requestName) ? "mission_request" : requestName;
            string sceneId = _missionDefinition != null ? _missionDefinition.SceneId : "unknown_scene";
            string fileName = $"{safeRequestName}_{sceneId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(filePath, json);
            return filePath;
        }

        private void HandlePostSuccess(string responseText)
        {
            Debug.Log($"MissionPostManager: POST succeeded. response={responseText}");

            if (_missionResponseManager != null)
            {
                _missionResponseManager.TryApplyResponseJson(responseText);
            }
            else
            {
                Debug.LogWarning("MissionPostManager: MissionResponseManager reference is missing. Response was not stored.");
            }
        }

        private void HandlePostFailure(string errorMessage)
        {
            Debug.LogWarning($"MissionPostManager: POST failed. error={errorMessage}");
        }
    }
}


