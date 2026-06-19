using System;

namespace MdmUnity.Missions
{
    public static class MissionRequestBuilder
    {
        public static MissionPoseRequest BuildBeforeMission(MissionDefinition missionDefinition)
        {
            ValidateMissionDefinition(missionDefinition);

            return new MissionPoseRequest
            {
                storyId = missionDefinition.StoryId,
                sceneId = missionDefinition.SceneId,
                beforeMission = true,
                missionType = string.Empty,
                captureDurationSec = 0,
                sampleFps = 0,
                poseFrames = Array.Empty<MissionPoseFrame>()
            };
        }

        public static MissionPoseRequest BuildMissionResult(MissionDefinition missionDefinition, MissionPoseFrame[] poseFrames)
        {
            ValidateMissionDefinition(missionDefinition);

            return new MissionPoseRequest
            {
                storyId = missionDefinition.StoryId,
                sceneId = missionDefinition.SceneId,
                beforeMission = false,
                missionType = missionDefinition.MissionType,
                captureDurationSec = missionDefinition.CaptureDurationSec,
                sampleFps = missionDefinition.SampleFps,
                poseFrames = poseFrames ?? Array.Empty<MissionPoseFrame>()
            };
        }

        public static MissionPoseRequest Build(MissionDefinition missionDefinition, MissionPoseFrame[] poseFrames)
        {
            return BuildMissionResult(missionDefinition, poseFrames);
        }

        private static void ValidateMissionDefinition(MissionDefinition missionDefinition)
        {
            if (missionDefinition == null)
            {
                throw new ArgumentNullException(nameof(missionDefinition));
            }
        }
    }
}
