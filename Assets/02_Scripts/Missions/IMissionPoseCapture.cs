namespace MdmUnity.Missions
{
    public interface IMissionPoseCapture
    {
        bool IsCapturing { get; }
        void StartCapture();
    }
}
