using UnityEditor;
using UnityEngine;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection.Editor
{
  [CustomEditor(typeof(MissionPoseRequestBuilder))]
  public class MissionPoseRequestBuilderEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var builder = (MissionPoseRequestBuilder)target;
      EditorGUILayout.Space();

      using (new EditorGUI.DisabledScope(!Application.isPlaying || builder.IsCapturing))
      {
        if (GUILayout.Button("측정 시작"))
        {
          builder.StartCapture();
        }
      }

      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox("Play 모드에서 측정 시작 버튼을 사용할 수 있습니다.", MessageType.Info);
      }
      else if (builder.IsCapturing)
      {
        EditorGUILayout.HelpBox($"측정 중... 수집 프레임: {builder.CapturedFrameCount}", MessageType.None);
        Repaint();
      }
      else if (!string.IsNullOrEmpty(builder.LastJsonFilePath))
      {
        EditorGUILayout.HelpBox($"마지막 측정 완료. 수집 프레임: {builder.CapturedFrameCount}\nJSON 파일: {builder.LastJsonFilePath}", MessageType.None);

        if (GUILayout.Button("JSON 파일 선택"))
        {
          EditorUtility.RevealInFinder(builder.LastJsonFilePath);
        }
      }
    }
  }
}
