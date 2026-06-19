using System;

namespace MdmUnity.Missions
{
    [Serializable]
    public class MissionCheckResponse
    {
        public bool success;
        public bool sceneCleared;
        public string currentSceneId;
        public string nextSceneId;
        public string nextAction;
        public float score;
        public string reasonCode;
        public string message;
        public string errorCode;
        public string warningCode;
    }
}