namespace MdmUnity.Missions
{
    public interface IMissionPoseLandmarkSource
    {
        public bool TryGetLatestPoseFrame(out MissionPoseFrame poseFrame);
    }
}
