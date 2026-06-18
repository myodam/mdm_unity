using System;
using System.Collections.Generic;
using System.IO;
using MdmUnity.Missions;
using TaskNormalizedLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
  public class MissionPoseRequestBuilder : MonoBehaviour, IMissionPoseCapture
  {
    private const int LeftShoulderIndex = 11;
    private const int RightShoulderIndex = 12;
    private const int LeftElbowIndex = 13;
    private const int RightElbowIndex = 14;
    private const int LeftWristIndex = 15;
    private const int RightWristIndex = 16;

    [SerializeField] private PoseLandmarkerRunner _poseLandmarkerRunner;
    [SerializeField] private MissionPostManager _missionPostManager;
    [SerializeField] private float _captureDurationSec = 5.0f;
    [SerializeField] private int _sampleFps = 5;
    [SerializeField] private bool _logEachSample = false;

    private readonly object _landmarkLock = new object();
    private readonly List<MissionPoseFrame> _poseFrames = new List<MissionPoseFrame>();

    private LandmarkSet _latestLandmarks;
    private bool _hasLatestLandmarks;
    private bool _isCapturing;
    private float _captureStartTime;
    private float _nextSampleTime;
    private string _lastJsonFilePath = string.Empty;

    public bool IsCapturing => _isCapturing;
    public int CapturedFrameCount => _poseFrames.Count;
    public string LastJsonFilePath => _lastJsonFilePath;

    private void Awake()
    {
      if (_poseLandmarkerRunner == null)
      {
        _poseLandmarkerRunner = FindAnyObjectByType<PoseLandmarkerRunner>();
      }

      if (_missionPostManager == null)
      {
        _missionPostManager = FindAnyObjectByType<MissionPostManager>();
      }
    }

    private void OnEnable()
    {
      if (_poseLandmarkerRunner != null)
      {
        _poseLandmarkerRunner.OnPoseLandmarksDetected += HandlePoseLandmarksDetected;
      }
      else
      {
        Debug.LogWarning("MissionPoseRequestBuilder: PoseLandmarkerRunner reference is missing.");
      }
    }

    private void OnDisable()
    {
      if (_poseLandmarkerRunner != null)
      {
        _poseLandmarkerRunner.OnPoseLandmarksDetected -= HandlePoseLandmarksDetected;
      }
    }

    private void Update()
    {
      if (!_isCapturing)
      {
        return;
      }

      var currentTime = Time.time;
      if (currentTime >= _nextSampleTime)
      {
        TryAddSample(currentTime - _captureStartTime);
        _nextSampleTime += 1.0f / Mathf.Max(1, _sampleFps);
      }

      if (currentTime - _captureStartTime >= _captureDurationSec)
      {
        CompleteCapture();
      }
    }

    public void StartCapture()
    {
      if (_isCapturing)
      {
        Debug.LogWarning("MissionPoseRequestBuilder: capture is already running.");
        return;
      }

      _poseFrames.Clear();
      _lastJsonFilePath = string.Empty;
      _captureStartTime = Time.time;
      _nextSampleTime = _captureStartTime;
      _isCapturing = true;

      Debug.Log($"MissionPoseRequestBuilder: pose capture started. duration={_captureDurationSec}, sampleFps={_sampleFps}");
    }

    private void HandlePoseLandmarksDetected(PoseLandmarkerResult result)
    {
      if (!TryCreateLandmarkSet(result, out var landmarkSet))
      {
        lock (_landmarkLock)
        {
          _hasLatestLandmarks = false;
        }
        return;
      }

      lock (_landmarkLock)
      {
        _latestLandmarks = landmarkSet;
        _hasLatestLandmarks = true;
      }
    }

    private void TryAddSample(float elapsedTime)
    {
      LandmarkSet landmarkSet;
      lock (_landmarkLock)
      {
        if (!_hasLatestLandmarks)
        {
          return;
        }

        landmarkSet = _latestLandmarks;
      }

      var frame = new MissionPoseFrame
      {
        timestamp = (float)Math.Round(elapsedTime, 3),
        landmarks = new MissionPoseLandmarks
        {
          leftShoulder = landmarkSet.LeftShoulder,
          rightShoulder = landmarkSet.RightShoulder,
          leftElbow = landmarkSet.LeftElbow,
          rightElbow = landmarkSet.RightElbow,
          leftWrist = landmarkSet.LeftWrist,
          rightWrist = landmarkSet.RightWrist,
        },
      };

      _poseFrames.Add(frame);

      if (_logEachSample)
      {
        Debug.Log($"MissionPoseRequestBuilder sample[{_poseFrames.Count}]: {JsonUtility.ToJson(frame)}");
      }
    }

    private void CompleteCapture()
    {
      _isCapturing = false;

      if (_missionPostManager != null)
      {
        _lastJsonFilePath = _missionPostManager.CreateMissionResultRequest(_poseFrames.ToArray());
        Debug.Log($"MissionPoseRequestBuilder: mission request created. frameCount={_poseFrames.Count}, path={_lastJsonFilePath}");
        return;
      }

      var capture = new PoseCaptureDto
      {
        captureDurationSec = Mathf.RoundToInt(_captureDurationSec),
        sampleFps = Mathf.Max(1, _sampleFps),
        poseFrames = _poseFrames.ToArray(),
      };

      var json = JsonUtility.ToJson(capture, true);
      _lastJsonFilePath = SaveJsonFile(json);
      Debug.LogWarning($"MissionPoseRequestBuilder: MissionPostManager reference is missing. Pose-only debug JSON was saved. frameCount={_poseFrames.Count}, path={_lastJsonFilePath}");
    }

    private string SaveJsonFile(string json)
    {
      var directoryPath = Path.Combine(Application.persistentDataPath, "PoseCaptures");
      Directory.CreateDirectory(directoryPath);

      var fileName = $"pose_capture_{DateTime.Now:yyyyMMdd_HHmmss}.json";
      var filePath = Path.Combine(directoryPath, fileName);
      File.WriteAllText(filePath, json);
      return filePath;
    }

    private bool TryCreateLandmarkSet(PoseLandmarkerResult result, out LandmarkSet landmarkSet)
    {
      landmarkSet = default;

      if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
      {
        return false;
      }

      var pose = result.poseLandmarks[0];
      if (pose.landmarks == null || pose.landmarks.Count <= RightWristIndex)
      {
        return false;
      }

      landmarkSet = new LandmarkSet
      {
        LeftShoulder = CreateLandmarkDto(pose.landmarks[LeftShoulderIndex]),
        RightShoulder = CreateLandmarkDto(pose.landmarks[RightShoulderIndex]),
        LeftElbow = CreateLandmarkDto(pose.landmarks[LeftElbowIndex]),
        RightElbow = CreateLandmarkDto(pose.landmarks[RightElbowIndex]),
        LeftWrist = CreateLandmarkDto(pose.landmarks[LeftWristIndex]),
        RightWrist = CreateLandmarkDto(pose.landmarks[RightWristIndex]),
      };
      return true;
    }

    private MissionPoseLandmark CreateLandmarkDto(TaskNormalizedLandmark landmark)
    {
      return new MissionPoseLandmark
      {
        x = landmark.x,
        y = landmark.y,
        visibility = landmark.visibility ?? 0.0f,
      };
    }

    private struct LandmarkSet
    {
      public MissionPoseLandmark LeftShoulder;
      public MissionPoseLandmark RightShoulder;
      public MissionPoseLandmark LeftElbow;
      public MissionPoseLandmark RightElbow;
      public MissionPoseLandmark LeftWrist;
      public MissionPoseLandmark RightWrist;
    }

    [Serializable]
    private class PoseCaptureDto
    {
      public int captureDurationSec;
      public int sampleFps;
      public MissionPoseFrame[] poseFrames;
    }
  }
}

