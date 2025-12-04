using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Video Configuration tab for WiseTwinEditor
    /// Allows configuring video triggers on 3D objects
    /// </summary>
    public static class WiseTwinEditorVideoTab
    {
        public static void Draw(WiseTwinEditorData data)
        {
            EditorGUILayout.LabelField("Video Configuration", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "Configure videos to play when clicking on 3D objects. " +
                "Drag & drop GameObjects from your scene, then add video URLs for each language. " +
                "If one language is missing, the other will be used as fallback.",
                MessageType.Info);
            EditorGUILayout.Space();

            // Add video trigger button
            if (GUILayout.Button("+ Add Video Trigger", GUILayout.Height(30)))
            {
                data.videoTriggers.Add(new VideoTriggerConfiguration());
                data.selectedVideoTriggerIndex = data.videoTriggers.Count - 1;
            }

            EditorGUILayout.Space();

            if (data.videoTriggers.Count == 0)
            {
                EditorGUILayout.HelpBox("No video triggers yet. Click 'Add Video Trigger' to get started!", MessageType.Info);
                return;
            }

            // Video triggers list
            EditorGUILayout.LabelField($"Video Triggers ({data.videoTriggers.Count})", EditorStyles.boldLabel);

            for (int i = 0; i < data.videoTriggers.Count; i++)
            {
                DrawVideoTriggerItem(data, i);
            }

            EditorGUILayout.Space();

            // Edit selected video trigger
            if (data.selectedVideoTriggerIndex >= 0 && data.selectedVideoTriggerIndex < data.videoTriggers.Count)
            {
                DrawVideoTriggerEditor(data.videoTriggers[data.selectedVideoTriggerIndex]);
            }
        }

        private static void DrawVideoTriggerItem(WiseTwinEditorData data, int index)
        {
            var trigger = data.videoTriggers[index];

            EditorGUILayout.BeginHorizontal("box");

            // Select/Toggle button
            bool isSelected = (data.selectedVideoTriggerIndex == index);
            GUI.backgroundColor = isSelected ? new Color(0.4f, 0.8f, 1f) : Color.white;

            string objectName = string.IsNullOrEmpty(trigger.targetObjectName) ? "(No object)" : trigger.targetObjectName;
            string buttonText = isSelected ? $">> {index + 1}. {objectName}" : $"{index + 1}. {objectName}";

            if (GUILayout.Button(buttonText, GUILayout.Height(25)))
            {
                data.selectedVideoTriggerIndex = isSelected ? -1 : index;
            }
            GUI.backgroundColor = Color.white;

            // Status indicators
            bool hasEN = !string.IsNullOrEmpty(trigger.videoUrlEN);
            bool hasFR = !string.IsNullOrEmpty(trigger.videoUrlFR);

            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
            statusStyle.normal.textColor = (hasEN || hasFR) ? Color.green : Color.gray;
            EditorGUILayout.LabelField(hasEN ? "EN" : "--", statusStyle, GUILayout.Width(25));
            EditorGUILayout.LabelField(hasFR ? "FR" : "--", statusStyle, GUILayout.Width(25));

            // Delete button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Video Trigger",
                    $"Delete video trigger for '{objectName}'?", "Delete", "Cancel"))
                {
                    data.videoTriggers.RemoveAt(index);
                    if (data.selectedVideoTriggerIndex >= data.videoTriggers.Count)
                    {
                        data.selectedVideoTriggerIndex = data.videoTriggers.Count - 1;
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawVideoTriggerEditor(VideoTriggerConfiguration trigger)
        {
            EditorGUILayout.LabelField("Edit Video Trigger", EditorStyles.boldLabel);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            // Target Object (drag & drop)
            EditorGUILayout.LabelField("Target Object", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Drag a GameObject from your scene here. When clicked during training, the video will play.", MessageType.None);

            EditorGUI.BeginChangeCheck();
            trigger.targetObject = (GameObject)EditorGUILayout.ObjectField(
                "3D Object",
                trigger.targetObject,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck() && trigger.targetObject != null)
            {
                trigger.targetObjectName = trigger.targetObject.name;
            }

            // Object name field (for manual edit or display)
            trigger.targetObjectName = EditorGUILayout.TextField("Object Name", trigger.targetObjectName);

            EditorGUILayout.Space();

            // Video URLs
            EditorGUILayout.LabelField("Video URLs", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Enter public video URLs. If one language is missing, the other will be used as fallback.", MessageType.None);

            GUIStyle urlStyle = new GUIStyle(EditorStyles.textField);
            urlStyle.wordWrap = false;

            EditorGUILayout.LabelField("  English Video URL:", EditorStyles.miniLabel);
            trigger.videoUrlEN = EditorGUILayout.TextField(trigger.videoUrlEN);

            EditorGUILayout.LabelField("  French Video URL:", EditorStyles.miniLabel);
            trigger.videoUrlFR = EditorGUILayout.TextField(trigger.videoUrlFR);

            EditorGUILayout.Space();

            // Validation status
            bool hasObject = !string.IsNullOrEmpty(trigger.targetObjectName);
            bool hasAnyUrl = !string.IsNullOrEmpty(trigger.videoUrlEN) || !string.IsNullOrEmpty(trigger.videoUrlFR);

            if (!hasObject)
            {
                EditorGUILayout.HelpBox("Please assign a target object.", MessageType.Warning);
            }
            else if (!hasAnyUrl)
            {
                EditorGUILayout.HelpBox("Please add at least one video URL.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Video trigger is ready!", MessageType.Info);
            }
        }
    }
}

#endif
