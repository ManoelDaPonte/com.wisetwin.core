using UnityEngine;
using UnityEditor;
using WiseTwin;

namespace WiseTwin.EditorExtensions
{
    /// <summary>
    /// Custom Editor pour InteractableObject
    /// Affiche les options de procédure uniquement quand le type Procedure est sélectionné
    /// </summary>
    [CustomEditor(typeof(InteractableObject))]
    public class InteractableObjectEditor : UnityEditor.Editor
    {
        SerializedProperty contentType;
        SerializedProperty useDragDropSequence;
        SerializedProperty enableYellowHighlight;
        SerializedProperty procedureSequence;
        SerializedProperty specificContentKey;
        SerializedProperty debugMode;

        // Reset script properties
        SerializedProperty useResetScript;
        SerializedProperty resetScript;

        // Visual feedback properties
        SerializedProperty highlightOnHover;
        SerializedProperty hoverMode;
        SerializedProperty hoverColor;
        SerializedProperty emissionIntensity;
        SerializedProperty showCursorChange;
        SerializedProperty hoverMaterial;

        void OnEnable()
        {
            // Content properties
            contentType = serializedObject.FindProperty("contentType");
            useDragDropSequence = serializedObject.FindProperty("useDragDropSequence");
            enableYellowHighlight = serializedObject.FindProperty("enableYellowHighlight");
            procedureSequence = serializedObject.FindProperty("procedureSequence");
            specificContentKey = serializedObject.FindProperty("specificContentKey");
            debugMode = serializedObject.FindProperty("debugMode");

            // Reset properties
            useResetScript = serializedObject.FindProperty("useResetScript");
            resetScript = serializedObject.FindProperty("resetScript");

            // Visual properties
            highlightOnHover = serializedObject.FindProperty("highlightOnHover");
            hoverMode = serializedObject.FindProperty("hoverMode");
            hoverColor = serializedObject.FindProperty("hoverColor");
            emissionIntensity = serializedObject.FindProperty("emissionIntensity");
            showCursorChange = serializedObject.FindProperty("showCursorChange");
            hoverMaterial = serializedObject.FindProperty("hoverMaterial");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);

            // Visual Feedback Section
            EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(highlightOnHover);

            if (highlightOnHover.boolValue)
            {
                EditorGUILayout.PropertyField(hoverMode);

                var mode = (InteractableObject.HoverMode)hoverMode.enumValueIndex;
                switch (mode)
                {
                    case InteractableObject.HoverMode.MultiplyColor:
                    case InteractableObject.HoverMode.OverrideColor:
                        EditorGUILayout.PropertyField(hoverColor);
                        break;
                    case InteractableObject.HoverMode.EmissionBoost:
                        EditorGUILayout.PropertyField(hoverColor);
                        EditorGUILayout.PropertyField(emissionIntensity);
                        break;
                    case InteractableObject.HoverMode.MaterialSwap:
                        EditorGUILayout.PropertyField(hoverMaterial);
                        break;
                }

                EditorGUILayout.PropertyField(showCursorChange);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Content Type Section
            EditorGUILayout.LabelField("Content Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(contentType);

            ContentType selectedType = (ContentType)contentType.enumValueIndex;

            // Afficher les options spécifiques selon le type
            switch (selectedType)
            {
                case ContentType.Procedure:
                    ShowProcedureOptions();
                    break;
                case ContentType.Question:
                    ShowQuestionOptions();
                    break;
                case ContentType.Text:
                    ShowTextOptions();
                    break;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Debug Section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(debugMode);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        void ShowProcedureOptions()
        {
            EditorGUILayout.Space(5);

            // Toggle pour activer/désactiver le clignotement jaune
            EditorGUILayout.PropertyField(enableYellowHighlight,
                new GUIContent("Enable Yellow Highlight",
                "Enable to highlight procedure objects in yellow. When disabled, objects won't be highlighted."));

            // Toggle pour utiliser le drag & drop
            EditorGUILayout.PropertyField(useDragDropSequence,
                new GUIContent("Use Drag & Drop Sequence",
                "Enable to manually define the procedure sequence by dragging GameObjects"));

            if (useDragDropSequence.boolValue)
            {
                EditorGUI.indentLevel++;

                // Clé de la procédure dans les métadatas
                EditorGUILayout.PropertyField(specificContentKey,
                    new GUIContent("Procedure Key", "Key in metadata for procedure texts (e.g., 'procedure_maintenance')"));

                EditorGUILayout.Space(5);

                // Liste des GameObjects
                EditorGUILayout.PropertyField(procedureSequence,
                    new GUIContent("Sequence Objects", "Drag GameObjects here in order of the procedure steps"),
                    true);

                if (procedureSequence.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("Add GameObjects to define the procedure sequence. " +
                        "Text instructions will come from metadata.", MessageType.Info);
                }
                else
                {
                    // Afficher un aperçu de la séquence
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Sequence Preview:", EditorStyles.miniBoldLabel);
                    for (int i = 0; i < procedureSequence.arraySize; i++)
                    {
                        var obj = procedureSequence.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                        if (obj != null)
                        {
                            EditorGUILayout.LabelField($"  Step {i + 1}: {obj.name}", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.HelpBox("Instructions for each step will be loaded from metadata (step_1, step_2, etc.)",
                        MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                // Mode metadata classique
                EditorGUILayout.PropertyField(specificContentKey,
                    new GUIContent("Procedure Key (Optional)",
                    "Leave empty to use first procedure found in metadata"));

                EditorGUILayout.HelpBox("Procedure will be loaded from metadata file (objects and texts). " +
                    "Enable 'Use Drag & Drop Sequence' to define objects manually.", MessageType.Info);
            }

            // Reset Script Options
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Reset Options", EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(useResetScript,
                new GUIContent("Use Reset Script",
                "Enable to call a custom reset script when the procedure restarts (user clicks outside sequence)"));

            if (useResetScript.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(resetScript,
                    new GUIContent("Reset Script",
                    "MonoBehaviour implementing IProcedureReset interface"));

                if (resetScript.objectReferenceValue != null)
                {
                    MonoBehaviour script = resetScript.objectReferenceValue as MonoBehaviour;
                    if (script != null)
                    {
                        // Vérifier si le script implémente l'interface
                        if (!(script is WiseTwin.UI.IProcedureReset))
                        {
                            EditorGUILayout.HelpBox("The selected script must implement IProcedureReset interface!", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Reset script will be called when procedure restarts.", MessageType.Info);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        void ShowQuestionOptions()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(specificContentKey,
                new GUIContent("Question Key (Optional)",
                "Leave empty to use all questions found in metadata"));

            EditorGUILayout.HelpBox("Questions will be loaded from metadata file. " +
                "Multiple questions with keys like 'question_1', 'question_2' will be shown sequentially.",
                MessageType.Info);
        }

        void ShowTextOptions()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(specificContentKey,
                new GUIContent("Text Key (Optional)",
                "Leave empty to use first text content found in metadata"));

            EditorGUILayout.HelpBox("Text content will be loaded from metadata file. " +
                "Supports markdown-like formatting.", MessageType.Info);
        }
    }
}