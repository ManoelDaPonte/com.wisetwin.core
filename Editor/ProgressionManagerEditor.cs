using UnityEngine;
using UnityEditor;
using WiseTwin;

namespace WiseTwin.Editor
{
    [CustomEditor(typeof(ProgressionManager))]
    public class ProgressionManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty autoDetectOnStart;
        private SerializedProperty detectionMethod;
        private SerializedProperty searchParent;
        private SerializedProperty progressionSequence;
        private SerializedProperty visibilityMode;
        private SerializedProperty showCompletedObjects;
        private SerializedProperty hideCompletedObjects;
        private SerializedProperty allowBackTracking;
        private SerializedProperty resetOnStart;
        private SerializedProperty requireSuccessForAll;
        private SerializedProperty maxAttemptsPerObject;
        private SerializedProperty debugMode;

        private void OnEnable()
        {
            autoDetectOnStart = serializedObject.FindProperty("autoDetectOnStart");
            detectionMethod = serializedObject.FindProperty("detectionMethod");
            searchParent = serializedObject.FindProperty("searchParent");
            progressionSequence = serializedObject.FindProperty("progressionSequence");
            visibilityMode = serializedObject.FindProperty("visibilityMode");
            showCompletedObjects = serializedObject.FindProperty("showCompletedObjects");
            hideCompletedObjects = serializedObject.FindProperty("hideCompletedObjects");
            allowBackTracking = serializedObject.FindProperty("allowBackTracking");
            resetOnStart = serializedObject.FindProperty("resetOnStart");
            requireSuccessForAll = serializedObject.FindProperty("requireSuccessForAll");
            maxAttemptsPerObject = serializedObject.FindProperty("maxAttemptsPerObject");
            debugMode = serializedObject.FindProperty("debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProgressionManager manager = (ProgressionManager)target;

            // Header avec style
            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            EditorGUILayout.LabelField("Progression Manager", headerStyle);
            EditorGUILayout.Space(5);

            // Section Auto-Detection
            EditorGUILayout.LabelField("Auto-Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoDetectOnStart, new GUIContent("Auto Detect On Start", "Détecter automatiquement tous les InteractableObjects au démarrage"));
            EditorGUILayout.PropertyField(detectionMethod, new GUIContent("Detection Method", "Méthode de recherche des objets"));

            // Afficher le champ searchParent uniquement si ChildrenOnly est sélectionné
            if (detectionMethod.enumValueIndex == (int)DetectionMethod.ChildrenOnly)
            {
                EditorGUILayout.PropertyField(searchParent, new GUIContent("Search Parent", "Parent GameObject pour la recherche"));
            }

            EditorGUILayout.Space(5);

            // Bouton d'auto-détection avec style
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = new Color(0.2f, 0.8f, 0.3f);

            if (GUILayout.Button("🔍 Auto-Detect InteractableObjects", buttonStyle, GUILayout.Height(30)))
            {
                manager.AutoDetectInteractableObjects();
                EditorUtility.SetDirty(target);
                Debug.Log($"[ProgressionManager] Auto-detection completed! Found {manager.ProgressionSequence.Count} objects.");
            }

            EditorGUILayout.Space(10);

            // Section Configuration
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(progressionSequence, new GUIContent("Progression Sequence", "Liste ordonnée des objets (remplie automatiquement si auto-detect activé)"));

            // Afficher le nombre d'objets dans la séquence
            if (progressionSequence.arraySize > 0)
            {
                EditorGUILayout.HelpBox($"📊 {progressionSequence.arraySize} objets dans la séquence", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Aucun objet dans la séquence. Utilisez Auto-Detect ou ajoutez-les manuellement.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(visibilityMode, new GUIContent("Visibility Mode", "Mode d'affichage des objets non actifs"));

            EditorGUILayout.PropertyField(hideCompletedObjects, new GUIContent("Hide Completed Objects", "Cacher les objets complétés (un seul objet visible à la fois)"));

            // Afficher showCompletedObjects uniquement si hideCompletedObjects est désactivé
            if (!hideCompletedObjects.boolValue)
            {
                EditorGUILayout.PropertyField(showCompletedObjects, new GUIContent("Show Completed Objects", "Afficher les objets complétés normalement"));
            }

            EditorGUILayout.PropertyField(allowBackTracking, new GUIContent("Allow Back Tracking", "Permettre de revenir en arrière"));
            EditorGUILayout.PropertyField(resetOnStart, new GUIContent("Reset On Start", "Réinitialiser la progression au démarrage"));

            EditorGUILayout.Space(10);

            // Section Options de complétion
            EditorGUILayout.LabelField("Completion Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requireSuccessForAll, new GUIContent("Require Success For All", "Exiger la réussite pour passer à l'étape suivante"));
            EditorGUILayout.PropertyField(maxAttemptsPerObject, new GUIContent("Max Attempts Per Object", "Nombre d'essais max par objet (0 = illimité)"));

            EditorGUILayout.Space(10);

            // Section Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugMode, new GUIContent("Debug Mode", "Active les logs de debug"));

            EditorGUILayout.Space(10);

            // Section Runtime Controls (uniquement en mode Play)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("▶️ Start", GUILayout.Height(25)))
                {
                    manager.StartProgression();
                }

                if (GUILayout.Button("⏹️ Stop", GUILayout.Height(25)))
                {
                    manager.StopProgression();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("⏮️ Previous", GUILayout.Height(25)))
                {
                    manager.MoveToPreviousStep();
                }

                if (GUILayout.Button("⏭️ Next", GUILayout.Height(25)))
                {
                    manager.MoveToNextStep();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Afficher les infos de progression en runtime
                if (manager.IsProgressionActive)
                {
                    EditorGUILayout.HelpBox($"📍 Étape actuelle : {manager.CurrentStepIndex + 1}/{manager.TotalSteps} ({manager.ProgressPercentage:F0}%)", MessageType.Info);

                    var currentObj = manager.GetCurrentObject();
                    if (currentObj != null)
                    {
                        EditorGUILayout.LabelField("Objet actuel :", currentObj.name);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Progression inactive", MessageType.None);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
