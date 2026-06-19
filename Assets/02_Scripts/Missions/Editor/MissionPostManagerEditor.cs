using System.IO;
using MdmUnity.Missions;
using UnityEditor;
using UnityEngine;

namespace MdmUnity.Missions.Editor
{
    [CustomEditor(typeof(MissionPostManager))]
    public class MissionPostManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _missionResponseManagerProperty;

        private void OnEnable()
        {
            _missionResponseManagerProperty = serializedObject.FindProperty("_missionResponseManager");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MissionPostManager manager = (MissionPostManager)target;

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("미션 전 POST"))
                {
                    manager.CreateBeforeMissionRequest();
                }
            }

            if (GUILayout.Button("POST 파일 열기"))
            {
                OpenPostFile(manager);
            }

            if (GUILayout.Button("응답 파일 열기"))
            {
                OpenResponseFile();
            }
        }

        private static void OpenPostFile(MissionPostManager manager)
        {
            string filePath = manager.LastRequestFilePath;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Debug.LogWarning("MissionPostManagerEditor: POST request file does not exist yet.");
                return;
            }

            EditorUtility.RevealInFinder(filePath);
        }

        private void OpenResponseFile()
        {
            MissionResponseManager responseManager = _missionResponseManagerProperty.objectReferenceValue as MissionResponseManager;
            if (responseManager == null)
            {
                Debug.LogWarning("MissionPostManagerEditor: MissionResponseManager reference is missing.");
                return;
            }

            string filePath = responseManager.LastResponseFilePath;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Debug.LogWarning("MissionPostManagerEditor: response file does not exist yet.");
                return;
            }

            EditorUtility.RevealInFinder(filePath);
        }
    }
}

