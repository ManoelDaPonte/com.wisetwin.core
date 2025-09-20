using UnityEngine;
using System.Collections.Generic;
using WiseTwin.Analytics;

namespace WiseTwin.Test
{
    /// <summary>
    /// Composant de test pour vérifier le système d'analytics
    /// À attacher sur un GameObject pour tester dans Unity Editor
    /// </summary>
    public class TestAnalytics : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool debugOutput = true;

        void Start()
        {
            if (runTestOnStart)
            {
                RunCompleteTest();
            }
        }

        [ContextMenu("Run Complete Analytics Test")]
        public void RunCompleteTest()
        {
            Debug.Log("=== STARTING ANALYTICS TEST ===");

            // S'assurer que TrainingAnalytics existe
            if (TrainingAnalytics.Instance == null)
            {
                var analyticsGO = new GameObject("TrainingAnalytics");
                analyticsGO.AddComponent<TrainingAnalytics>();
                if (debugOutput) Debug.Log("Created TrainingAnalytics instance");
            }

            // Reset pour un test propre
            TrainingAnalytics.Instance.ResetAnalytics();
            TrainingAnalytics.Instance.SetDebugMode(debugOutput);

            // Simuler quelques interactions
            SimulateQuestionInteraction();
            SimulateProcedureInteraction();
            SimulateTextInteraction();

            // Compléter la formation
            TrainingAnalytics.Instance.CompleteTraining("completed");

            // Exporter et afficher les résultats
            string analyticsJson = TrainingAnalytics.Instance.ExportAnalytics();
            Debug.Log($"=== ANALYTICS JSON OUTPUT ===\n{analyticsJson}");

            Debug.Log("=== ANALYTICS TEST COMPLETED ===");
        }

        void SimulateQuestionInteraction()
        {
            if (debugOutput) Debug.Log("Simulating question interaction...");

            var questionData = new QuestionInteractionData
            {
                questionText = "Which programming languages are object-oriented?",
                options = new List<string> { "Python", "HTML", "Java", "CSS", "C++" },
                correctAnswers = new List<int> { 0, 2, 4 }
            };

            // Première tentative (incorrecte)
            questionData.AddUserAttempt(new List<int> { 0, 1 });

            // Deuxième tentative (correcte)
            questionData.AddUserAttempt(new List<int> { 0, 2, 4 });
            questionData.finalScore = 100f;

            TrainingAnalytics.Instance.TrackQuestionInteraction("test_object_1", "question_1", questionData);
            TrainingAnalytics.Instance.IncrementCurrentInteractionAttempts();
            TrainingAnalytics.Instance.IncrementCurrentInteractionAttempts();
            TrainingAnalytics.Instance.EndCurrentInteraction(true);

            if (debugOutput) Debug.Log("Question interaction completed");
        }

        void SimulateProcedureInteraction()
        {
            if (debugOutput) Debug.Log("Simulating procedure interaction...");

            // Étape 1
            var step1Data = new ProcedureInteractionData
            {
                stepNumber = 1,
                totalSteps = 3,
                instruction = "Click on the red valve",
                hintsUsed = 0,
                wrongClicks = 2
            };
            TrainingAnalytics.Instance.TrackProcedureStep("valve_01", step1Data);
            TrainingAnalytics.Instance.EndCurrentInteraction(true);

            // Étape 2
            var step2Data = new ProcedureInteractionData
            {
                stepNumber = 2,
                totalSteps = 3,
                instruction = "Turn the blue handle",
                hintsUsed = 1,
                wrongClicks = 0
            };
            TrainingAnalytics.Instance.TrackProcedureStep("handle_01", step2Data);
            TrainingAnalytics.Instance.EndCurrentInteraction(true);

            // Étape 3
            var step3Data = new ProcedureInteractionData
            {
                stepNumber = 3,
                totalSteps = 3,
                instruction = "Press the green button",
                hintsUsed = 0,
                wrongClicks = 1
            };
            TrainingAnalytics.Instance.TrackProcedureStep("button_01", step3Data);
            TrainingAnalytics.Instance.EndCurrentInteraction(true);

            if (debugOutput) Debug.Log("Procedure interaction completed");
        }

        void SimulateTextInteraction()
        {
            if (debugOutput) Debug.Log("Simulating text interaction...");

            var textData = new TextInteractionData
            {
                textContent = "Safety Instructions",
                timeDisplayed = 15.5f,
                readComplete = true,
                scrollPercentage = 100f
            };

            TrainingAnalytics.Instance.TrackTextDisplay("info_panel_1", textData);

            if (debugOutput) Debug.Log("Text interaction completed");
        }

        [ContextMenu("Test Export Only")]
        public void TestExportOnly()
        {
            if (TrainingAnalytics.Instance == null)
            {
                Debug.LogError("TrainingAnalytics instance not found!");
                return;
            }

            string json = TrainingAnalytics.Instance.ExportAnalytics();
            Debug.Log($"Current Analytics State:\n{json}");
        }

        [ContextMenu("Test Training Completion")]
        public void TestTrainingCompletion()
        {
            // Chercher ou créer le notifier
            var notifier = FindFirstObjectByType<TrainingCompletionNotifier>();
            if (notifier == null)
            {
                var notifierGO = new GameObject("TrainingCompletionNotifier");
                notifier = notifierGO.AddComponent<TrainingCompletionNotifier>();
                if (debugOutput) Debug.Log("Created TrainingCompletionNotifier instance");
            }

            // Appeler la méthode de complétion
            notifier.FormationCompleted("Test Training");
            Debug.Log("Training completion notification sent");
        }
    }
}