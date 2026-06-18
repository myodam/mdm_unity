using UnityEngine;

namespace MdmUnity.Missions
{
    public class PoseLandmarkArmFollower : MonoBehaviour
    {
        private enum ArmSide
        {
            Left,
            Right,
        }

        [SerializeField] private MonoBehaviour _poseSourceBehaviour;
        [SerializeField] private Transform _upperArmSegment;
        [SerializeField] private Transform _forearmSegment;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private ArmSide _armSide = ArmSide.Left;
        [SerializeField] private float _worldDepthFromCamera = 5.0f;
        [SerializeField] private float _segmentThickness = 0.12f;
        [SerializeField] private bool _mirrorX = true;
        [SerializeField, Range(0.0f, 1.0f)] private float _visibilityThreshold = 0.3f;
        [SerializeField] private float _smoothTime = 0.06f;

        private IMissionPoseLandmarkSource _poseSource;
        private Renderer[] _renderers;
        private Vector3 _shoulderVelocity;
        private Vector3 _elbowVelocity;
        private Vector3 _wristVelocity;
        private Vector3 _smoothedShoulder;
        private Vector3 _smoothedElbow;
        private Vector3 _smoothedWrist;
        private bool _hasSmoothedPoints;
        private bool _warnedMissingSource;

        private void Awake()
        {
            _poseSource = _poseSourceBehaviour as IMissionPoseLandmarkSource;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (_poseSource == null)
            {
                WarnMissingSource();
                SetVisible(false);
                return;
            }

            if (_worldCamera == null || _upperArmSegment == null || _forearmSegment == null)
            {
                SetVisible(false);
                return;
            }

            if (!_poseSource.TryGetLatestPoseFrame(out MissionPoseFrame poseFrame) ||
                !TryGetArmLandmarks(poseFrame, out MissionPoseLandmark shoulder, out MissionPoseLandmark elbow, out MissionPoseLandmark wrist))
            {
                SetVisible(false);
                _hasSmoothedPoints = false;
                return;
            }

            Vector3 shoulderPoint = ConvertToWorldPoint(shoulder);
            Vector3 elbowPoint = ConvertToWorldPoint(elbow);
            Vector3 wristPoint = ConvertToWorldPoint(wrist);
            SmoothPoints(ref shoulderPoint, ref elbowPoint, ref wristPoint);

            SetVisible(true);
            ApplySegment(_upperArmSegment, shoulderPoint, elbowPoint);
            ApplySegment(_forearmSegment, elbowPoint, wristPoint);
        }

        private bool TryGetArmLandmarks(
            MissionPoseFrame poseFrame,
            out MissionPoseLandmark shoulder,
            out MissionPoseLandmark elbow,
            out MissionPoseLandmark wrist)
        {
            shoulder = null;
            elbow = null;
            wrist = null;

            if (poseFrame?.landmarks == null)
            {
                return false;
            }

            MissionPoseLandmarks landmarks = poseFrame.landmarks;
            if (_armSide == ArmSide.Left)
            {
                shoulder = landmarks.leftShoulder;
                elbow = landmarks.leftElbow;
                wrist = landmarks.leftWrist;
            }
            else
            {
                shoulder = landmarks.rightShoulder;
                elbow = landmarks.rightElbow;
                wrist = landmarks.rightWrist;
            }

            return IsVisible(shoulder) && IsVisible(elbow) && IsVisible(wrist);
        }

        private bool IsVisible(MissionPoseLandmark landmark)
        {
            return landmark != null && landmark.visibility >= _visibilityThreshold;
        }

        private Vector3 ConvertToWorldPoint(MissionPoseLandmark landmark)
        {
            float viewportX = _mirrorX ? 1.0f - landmark.x : landmark.x;
            float viewportY = 1.0f - landmark.y;
            return _worldCamera.ViewportToWorldPoint(new Vector3(Mathf.Clamp01(viewportX), Mathf.Clamp01(viewportY), _worldDepthFromCamera));
        }

        private void SmoothPoints(ref Vector3 shoulderPoint, ref Vector3 elbowPoint, ref Vector3 wristPoint)
        {
            if (_smoothTime <= 0.0f || !_hasSmoothedPoints)
            {
                _smoothedShoulder = shoulderPoint;
                _smoothedElbow = elbowPoint;
                _smoothedWrist = wristPoint;
                _hasSmoothedPoints = true;
                return;
            }

            _smoothedShoulder = Vector3.SmoothDamp(_smoothedShoulder, shoulderPoint, ref _shoulderVelocity, _smoothTime);
            _smoothedElbow = Vector3.SmoothDamp(_smoothedElbow, elbowPoint, ref _elbowVelocity, _smoothTime);
            _smoothedWrist = Vector3.SmoothDamp(_smoothedWrist, wristPoint, ref _wristVelocity, _smoothTime);

            shoulderPoint = _smoothedShoulder;
            elbowPoint = _smoothedElbow;
            wristPoint = _smoothedWrist;
        }

        private void ApplySegment(Transform segment, Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 direction = endPoint - startPoint;
            float length = direction.magnitude;

            if (length <= 0.001f)
            {
                segment.localScale = Vector3.zero;
                return;
            }

            segment.position = (startPoint + endPoint) * 0.5f;
            segment.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            segment.localScale = new Vector3(_segmentThickness, length * 0.5f, _segmentThickness);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = visible;
                }
            }
        }

        private void WarnMissingSource()
        {
            if (_warnedMissingSource)
            {
                return;
            }

            _warnedMissingSource = true;
            Debug.LogWarning("PoseLandmarkArmFollower: pose source is missing or does not implement IMissionPoseLandmarkSource.");
        }
    }
}
