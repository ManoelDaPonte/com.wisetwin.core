using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Dialogue management tab for WiseTwinEditor.
    /// Lists all dialogues, allows creation, and opens the visual graph editor.
    /// </summary>
    public static class WiseTwinEditorDialogueTab
    {
        public static void Draw(WiseTwinEditorData data)
        {
            EditorGUILayout.LabelField("Dialogue Configuration", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Create and manage branching dialogue trees. Each dialogue can be linked to a Dialogue-type scenario in the Scenario Configuration tab.", MessageType.Info);
            EditorGUILayout.Space();

            // Add dialogue button
            if (GUILayout.Button("+ Add New Dialogue", GUILayout.Height(30)))
            {
                var newDialogue = new DialogueScenarioData
                {
                    dialogueId = $"dialogue_{data.dialogues.Count + 1}",
                    titleEN = "New Dialogue",
                    titleFR = "Nouveau Dialogue"
                };
                data.dialogues.Add(newDialogue);
                data.selectedDialogueIndex = data.dialogues.Count - 1;
            }

            EditorGUILayout.Space();

            if (data.dialogues.Count == 0)
            {
                EditorGUILayout.HelpBox("No dialogues yet. Click 'Add New Dialogue' to get started!", MessageType.Info);
                return;
            }

            // Dialogue list
            EditorGUILayout.LabelField($"Dialogues ({data.dialogues.Count})", EditorStyles.boldLabel);

            for (int i = 0; i < data.dialogues.Count; i++)
            {
                var dialogue = data.dialogues[i];
                EditorGUILayout.BeginHorizontal("box");

                // Select/Toggle button
                bool isSelected = (data.selectedDialogueIndex == i);
                GUI.backgroundColor = isSelected ? new Color(0.4f, 0.8f, 1f) : Color.white;
                string title = !string.IsNullOrEmpty(dialogue.titleEN) ? dialogue.titleEN : dialogue.dialogueId;
                string hasGraph = !string.IsNullOrEmpty(dialogue.graphDataJSON) ? " [Graph]" : "";
                string buttonText = isSelected ? $"v {i + 1}. {title}{hasGraph}" : $"> {i + 1}. {title}{hasGraph}";
                if (GUILayout.Button(buttonText, GUILayout.Height(25)))
                {
                    data.selectedDialogueIndex = isSelected ? -1 : i;
                }
                GUI.backgroundColor = Color.white;

                // Open graph editor
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Edit Graph", GUILayout.Width(90), GUILayout.Height(25)))
                {
                    DialogueEditor.DialogueEditorWindow.OpenWithDialogue(dialogue);
                }
                GUI.backgroundColor = Color.white;

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Dialogue", $"Delete dialogue '{dialogue.dialogueId}'?", "Delete", "Cancel"))
                    {
                        data.dialogues.RemoveAt(i);
                        if (data.selectedDialogueIndex >= data.dialogues.Count)
                            data.selectedDialogueIndex = data.dialogues.Count - 1;
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Edit selected dialogue
            if (data.selectedDialogueIndex >= 0 && data.selectedDialogueIndex < data.dialogues.Count)
            {
                DrawDialogueDetails(data.dialogues[data.selectedDialogueIndex]);
            }
        }

        private static void DrawDialogueDetails(DialogueScenarioData dialogue)
        {
            EditorGUILayout.LabelField("Edit Dialogue", EditorStyles.boldLabel);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            dialogue.dialogueId = EditorGUILayout.TextField("Dialogue ID", dialogue.dialogueId);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
            dialogue.titleEN = EditorGUILayout.TextField("  EN", dialogue.titleEN);
            dialogue.titleFR = EditorGUILayout.TextField("  FR", dialogue.titleFR);

            EditorGUILayout.Space();

            // Graph status
            if (!string.IsNullOrEmpty(dialogue.graphDataJSON))
            {
                EditorGUILayout.HelpBox("Graph data is configured.", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Open Graph Editor", GUILayout.Height(25)))
                {
                    DialogueEditor.DialogueEditorWindow.OpenWithDialogue(dialogue);
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                if (GUILayout.Button("Clear Graph Data", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear Graph", "Are you sure you want to clear the graph data?", "Clear", "Cancel"))
                    {
                        dialogue.graphDataJSON = "";
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No graph data. Open the Graph Editor to create a dialogue tree.", MessageType.Warning);

                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Open Graph Editor", GUILayout.Height(30)))
                {
                    DialogueEditor.DialogueEditorWindow.OpenWithDialogue(dialogue);
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}

#endif
