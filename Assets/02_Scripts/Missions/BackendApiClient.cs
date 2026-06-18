using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MdmUnity.Missions
{
    public class BackendApiClient : MonoBehaviour
    {
        [SerializeField] private string _missionEndpointUrl = string.Empty;
        [Min(1)] [SerializeField] private int _timeoutSec = 10;

        public bool HasMissionEndpoint => !string.IsNullOrWhiteSpace(_missionEndpointUrl);
        public string MissionEndpointUrl => _missionEndpointUrl;

        public void SetMissionEndpointUrl(string missionEndpointUrl)
        {
            _missionEndpointUrl = missionEndpointUrl ?? string.Empty;
        }

        public IEnumerator PostMissionRequestJson(string json, Action<string> onSuccess, Action<string> onFailure)
        {
            if (!HasMissionEndpoint)
            {
                onFailure?.Invoke("Mission endpoint URL is empty.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                onFailure?.Invoke("Request JSON is empty.");
                yield break;
            }

            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(_missionEndpointUrl, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(1, _timeoutSec);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(responseText);
                    yield break;
                }

                onFailure?.Invoke($"{request.responseCode}: {request.error}\n{responseText}");
            }
        }
    }
}
