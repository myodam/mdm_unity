using System;

namespace MdmUnity.Missions
{
    [Serializable]
    public class MissionPoseRequest
    {
        public string storyId;
        public string sceneId;
        public bool beforeMission;
        public string missionType;
        public int captureDurationSec;
        public int sampleFps;
        public MissionPoseFrame[] poseFrames;
    }

    [Serializable]
    public class MissionPoseFrame
    {
        public float timestamp;
        public MissionPoseLandmarks landmarks;
    }

    [Serializable]
    public class MissionPoseLandmarks
    {
        public MissionPoseLandmark leftShoulder;
        public MissionPoseLandmark rightShoulder;
        public MissionPoseLandmark leftElbow;
        public MissionPoseLandmark rightElbow;
        public MissionPoseLandmark leftWrist;
        public MissionPoseLandmark rightWrist;
    }

    [Serializable]
    public class MissionPoseLandmark
    {
        public float x;
        public float y;
        public float visibility;
    }
}
