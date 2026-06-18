using UnityEngine;

namespace MdmUnity.Missions
{
    [CreateAssetMenu(fileName = "MissionDefinition", menuName = "MDM/Missions/Mission Definition")]
    public class MissionDefinition : ScriptableObject
    {
        [SerializeField] private string _storyId = "heungbu_nolbu";
        [SerializeField] private string _sceneId = "scene_001";
        [SerializeField] private string _missionType = "protect_swallow";
        [Min(1)] [SerializeField] private int _captureDurationSec = 5;
        [Min(1)] [SerializeField] private int _sampleFps = 5;

        public string StoryId => _storyId;
        public string SceneId => _sceneId;
        public string MissionType => _missionType;
        public int CaptureDurationSec => _captureDurationSec;
        public int SampleFps => _sampleFps;
    }
}