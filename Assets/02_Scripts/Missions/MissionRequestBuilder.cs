using System;

namespace MdmUnity.Missions
{
    public static class MissionRequestBuilder
    {
        public static MissionPoseRequest Build(MissionDefinition missionDefinition, MissionPoseFrame[] poseFrames)
        {
            if (missionDefinition == null)
            {
                throw new ArgumentNullException(nameof(missionDefinition));
            }

            return new MissionPoseRequest
            {
                storyId = missionDefinition.StoryId,
                sceneId = missionDefinition.SceneId,
                missionType = missionDefinition.MissionType,
                captureDurationSec = missionDefinition.CaptureDurationSec,
                sampleFps = missionDefinition.SampleFps,
                poseFrames = poseFrames ?? Array.Empty<MissionPoseFrame>()
            };
        }
    }
}