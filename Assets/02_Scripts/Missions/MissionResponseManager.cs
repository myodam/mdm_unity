using System;
using System.IO;
using UnityEngine;

namespace MdmUnity.Missions
{
    public class MissionResponseManager : MonoBehaviour
    {
        [SerializeField] private bool _saveResponseJson = true;
        [SerializeField] private string _responseDirectoryName = "MissionResponses";
        [TextArea(5, 15)] [SerializeField] private string _debugResponseJson = string.Empty;

        private MissionCheckResponse _latestResponse;
        private string _lastResponseJson = string.Empty;
        private string _lastResponseFilePath = string.Empty;

        public event Action<MissionCheckResponse> ResponseUpdated;

        public bool HasResponse => _latestResponse != null;
        public MissionCheckResponse LatestResponse => _latestResponse;
        public string LastResponseJson => _lastResponseJson;
        public string LastResponseFilePath => _lastResponseFilePath;
        public string ResponseDirectoryPath => Path.Combine(Application.persistentDataPath, _responseDirectoryName);

        public bool TryApplyResponseJson(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                Debug.LogWarning("MissionResponseManager: response JSON is empty.");
                return false;
            }

            MissionCheckResponse response;
            try
            {
                response = JsonUtility.FromJson<MissionCheckResponse>(responseJson);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MissionResponseManager: failed to parse response JSON. error={exception.Message}");
                return false;
            }

            if (response == null)
            {
                Debug.LogWarning("MissionResponseManager: parsed response is null.");
                return false;
            }

            _latestResponse = response;
            _lastResponseJson = responseJson;

            if (_saveResponseJson)
            {
                _lastResponseFilePath = SaveResponseJson(responseJson, response);
                Debug.Log($"MissionResponseManager: response JSON saved. path={_lastResponseFilePath}");
            }
            else
            {
                _lastResponseFilePath = string.Empty;
            }

            ResponseUpdated?.Invoke(_latestResponse);
            Debug.Log($"MissionResponseManager: response updated. currentSceneId={response.currentSceneId}, nextAction={response.nextAction}, success={response.success}, sceneCleared={response.sceneCleared}");
            return true;
        }

        public void ClearResponse()
        {
            _latestResponse = null;
            _lastResponseJson = string.Empty;
            _lastResponseFilePath = string.Empty;
        }

        [ContextMenu("Apply Debug Response Json")]
        public void ApplyDebugResponseJson()
        {
            TryApplyResponseJson(_debugResponseJson);
        }

        [ContextMenu("Open Response Directory")]
        public void OpenResponseDirectory()
        {
            string directoryPath = ResponseDirectoryPath;
            Directory.CreateDirectory(directoryPath);
            Application.OpenURL(new Uri(directoryPath).AbsoluteUri);
        }

        [ContextMenu("Open Last Response File")]
        public void OpenLastResponseFile()
        {
            if (string.IsNullOrWhiteSpace(_lastResponseFilePath) || !File.Exists(_lastResponseFilePath))
            {
                Debug.LogWarning("MissionResponseManager: last response file does not exist.");
                return;
            }

            Application.OpenURL(new Uri(_lastResponseFilePath).AbsoluteUri);
        }

        private string SaveResponseJson(string responseJson, MissionCheckResponse response)
        {
            string directoryPath = ResponseDirectoryPath;
            Directory.CreateDirectory(directoryPath);

            string sceneId = string.IsNullOrWhiteSpace(response.currentSceneId) ? "unknown_scene" : response.currentSceneId;
            string action = string.IsNullOrWhiteSpace(response.nextAction) ? "unknown_action" : response.nextAction;
            string fileName = $"mission_response_{sceneId}_{action}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(filePath, responseJson);
            return filePath;
        }
    }
}
