using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Metadata Config tab for WiseTwinEditor
    /// </summary>
    public static class WiseTwinEditorMetadataTab
    {
        public static void Draw(WiseTwinEditorData data)
        {
            // Basic Settings
            EditorGUILayout.LabelField("📋 Basic Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // Scene ID (read-only, based on active scene)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Scene Name", data.sceneId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox($"Metadata will be saved as: {data.sceneId}-metadata.json", MessageType.Info);

            // Titre multilingue
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("📝 Title (Multilingual)", EditorStyles.boldLabel);
            data.projectTitleEN = EditorGUILayout.TextField("🇬🇧 English", data.projectTitleEN);
            data.projectTitleFR = EditorGUILayout.TextField("🇫🇷 Français", data.projectTitleFR);

            // Description multilingue
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("📄 Description (Multilingual)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("🇬🇧 English", EditorStyles.miniLabel);
            data.projectDescriptionEN = EditorGUILayout.TextArea(data.projectDescriptionEN, GUILayout.Height(60));
            EditorGUILayout.LabelField("🇫🇷 Français", EditorStyles.miniLabel);
            data.projectDescriptionFR = EditorGUILayout.TextArea(data.projectDescriptionFR, GUILayout.Height(60));

            data.projectVersion = EditorGUILayout.TextField("Version", data.projectVersion);

            // Duration in minutes (numeric field)
            EditorGUILayout.BeginHorizontal();
            data.durationMinutes = EditorGUILayout.IntField("Duration (minutes)", data.durationMinutes);
            EditorGUILayout.LabelField($"→ {data.durationMinutes} minutes", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Difficulty (dropdown)
            data.difficultyIndex = EditorGUILayout.Popup("Difficulty", data.difficultyIndex, data.difficultyOptions);

            data.imageUrl = EditorGUILayout.TextField("Image URL", data.imageUrl);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Advanced Settings
            EditorGUILayout.LabelField("⚙️ Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            DrawStringList("🏷️ Tags", data.tags);

            EditorGUILayout.EndVertical();
        }

        private static void DrawStringList(string label, List<string> list)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField($"  [{i}]", list[i]);
                if (GUILayout.Button("❌", GUILayout.Width(25)))
                {
                    list.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button($"➕ Add {label}"))
            {
                list.Add("");
            }

            EditorGUILayout.Space();
        }
    }
}

#endif
