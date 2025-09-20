using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace WiseTwin.Analytics
{
    /// <summary>
    /// Gestionnaire singleton pour collecter et exporter les métriques de formation
    /// Maintient une structure JSON plate avec toutes les interactions
    /// </summary>
    public class TrainingAnalytics : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool autoExportOnCompletion = true;

        // Singleton
        public static TrainingAnalytics Instance { get; private set; }

        // Session data
        private string sessionId;
        private string trainingId;
        private string startTime;
        private string endTime;
        private float totalDuration;
        private string completionStatus = "in_progress";

        // Interactions tracking
        private List<InteractionData> interactions;
        private InteractionData currentInteraction;
        private Dictionary<string, float> moduleStartTimes;

        // Summary statistics
        private int totalInteractions = 0;
        private int successfulInteractions = 0;
        private int failedInteractions = 0;
        private int totalAttempts = 0;
        private int totalFailedAttempts = 0; // Nouveau : compter les tentatives échouées

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Ne pas appliquer DontDestroyOnLoad si on est dans WiseTwinSystem
                // C'est le parent WiseTwinSystem qui gère la persistance
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                // Pas de warning si on est enfant de WiseTwinSystem
                InitializeSession();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void InitializeSession()
        {
            sessionId = Guid.NewGuid().ToString();
            trainingId = Application.productName;
            startTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            interactions = new List<InteractionData>();
            moduleStartTimes = new Dictionary<string, float>();

            // Réinitialiser les statistiques
            totalInteractions = 0;
            successfulInteractions = 0;
            failedInteractions = 0;
            totalAttempts = 0;
            totalFailedAttempts = 0;

            Debug.Log($"[TrainingAnalytics] Session initialized: {sessionId} for training: {trainingId}");
        }

        /// <summary>
        /// Démarre le tracking d'une nouvelle interaction
        /// </summary>
        public InteractionData StartInteraction(string objectId, string type, string subtype = "")
        {
            // Terminer l'interaction précédente si elle existe
            if (currentInteraction != null)
            {
                EndCurrentInteraction(false);
            }

            string interactionId = $"{objectId}_{type}_{DateTime.UtcNow.Ticks}";
            currentInteraction = new InteractionData(interactionId, type, subtype, objectId);

            Debug.Log($"[TrainingAnalytics] Started interaction: {interactionId} (type: {type}, subtype: {subtype})");

            return currentInteraction;
        }

        /// <summary>
        /// Termine l'interaction en cours
        /// </summary>
        public void EndCurrentInteraction(bool success)
        {
            if (currentInteraction != null)
            {
                currentInteraction.EndInteraction(success);
                interactions.Add(currentInteraction);

                // Mettre à jour les statistiques
                totalInteractions++;
                totalAttempts += currentInteraction.attempts;

                if (success)
                {
                    successfulInteractions++;
                    // Pour une interaction réussie, les tentatives échouées = attempts - 1
                    if (currentInteraction.attempts > 1)
                    {
                        totalFailedAttempts += (currentInteraction.attempts - 1);
                    }
                }
                else
                {
                    failedInteractions++;
                    // Pour une interaction échouée, toutes les tentatives sont des échecs
                    totalFailedAttempts += currentInteraction.attempts;
                }

                Debug.Log($"[TrainingAnalytics] Ended interaction: {currentInteraction.interactionId} - Success: {success}, Duration: {currentInteraction.duration}s");

                currentInteraction = null;
            }
        }

        /// <summary>
        /// Ajoute des données à l'interaction en cours
        /// </summary>
        public void AddDataToCurrentInteraction(string key, object value)
        {
            if (currentInteraction != null)
            {
                currentInteraction.AddData(key, value);
            }
        }

        /// <summary>
        /// Incrémente le nombre de tentatives pour l'interaction en cours
        /// </summary>
        public void IncrementCurrentInteractionAttempts()
        {
            currentInteraction?.IncrementAttempts();
        }

        /// <summary>
        /// Track une interaction de question
        /// </summary>
        public void TrackQuestionInteraction(string objectId, string questionId, QuestionInteractionData questionData)
        {
            var interaction = StartInteraction(objectId, "question", questionData.correctAnswers.Count > 1 ? "multiple_choice" : "single_choice");

            // Convertir les données spécifiques en dictionnaire
            var dataDict = questionData.ToDictionary();
            foreach (var kvp in dataDict)
            {
                interaction.AddData(kvp.Key, kvp.Value);
            }

            // Ne pas initialiser attempts ici car userAnswers est encore vide
            // Les attempts seront incrémentés à chaque validation de réponse

            // La question sera terminée plus tard quand l'utilisateur validera
        }

        /// <summary>
        /// Track une étape de procédure
        /// </summary>
        public void TrackProcedureStep(string objectId, ProcedureInteractionData procedureData)
        {
            var interaction = StartInteraction(objectId, "procedure", "sequential");

            var dataDict = procedureData.ToDictionary();
            foreach (var kvp in dataDict)
            {
                interaction.AddData(kvp.Key, kvp.Value);
            }

            // Une procédure compte comme une tentative
            interaction.attempts = 1;

            // Les procédures sont généralement terminées immédiatement après chaque étape
        }

        /// <summary>
        /// Track l'affichage d'un texte
        /// </summary>
        public void TrackTextDisplay(string objectId, TextInteractionData textData)
        {
            var interaction = StartInteraction(objectId, "text", "informative");

            var dataDict = textData.ToDictionary();
            foreach (var kvp in dataDict)
            {
                interaction.AddData(kvp.Key, kvp.Value);
            }

            // Les textes sont considérés comme "réussis" s'ils ont été affichés
            EndCurrentInteraction(true);
        }

        /// <summary>
        /// Marque la formation comme terminée
        /// </summary>
        public void CompleteTraining(string status = "completed")
        {
            endTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            completionStatus = status;

            // Calculer la durée totale
            if (DateTime.TryParse(startTime, out DateTime start) && DateTime.TryParse(endTime, out DateTime end))
            {
                totalDuration = (float)(end - start).TotalSeconds;
            }

            if (debugMode)
            {
                Debug.Log($"[TrainingAnalytics] Training completed: {status}");
                Debug.Log($"[TrainingAnalytics] Total interactions: {totalInteractions}, Success rate: {GetSuccessRate():F2}%");
            }

            if (autoExportOnCompletion)
            {
                string analytics = ExportAnalytics();
                if (debugMode)
                {
                    Debug.Log($"[TrainingAnalytics] Exported analytics: {analytics}");
                }
            }
        }

        /// <summary>
        /// Exporte toutes les analytics en JSON
        /// </summary>
        public string ExportAnalytics()
        {
            var analyticsData = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["trainingId"] = trainingId,
                ["startTime"] = startTime,
                ["endTime"] = endTime ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ["totalDuration"] = totalDuration,
                ["completionStatus"] = completionStatus,
                ["interactions"] = interactions.Select(i => i.ToDictionary()).ToList(),
                ["summary"] = new Dictionary<string, object>
                {
                    ["totalInteractions"] = totalInteractions,
                    ["successfulInteractions"] = successfulInteractions,
                    ["failedInteractions"] = failedInteractions,
                    ["averageTimePerInteraction"] = GetAverageInteractionTime(),
                    ["totalAttempts"] = totalAttempts,
                    ["totalFailedAttempts"] = totalFailedAttempts,
                    ["successRate"] = GetSuccessRate()
                }
            };

            return JsonConvert.SerializeObject(analyticsData, Formatting.Indented);
        }

        /// <summary>
        /// Obtient les analytics sous forme de dictionnaire
        /// </summary>
        public Dictionary<string, object> GetAnalyticsDictionary()
        {
            return new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["trainingId"] = trainingId,
                ["startTime"] = startTime,
                ["endTime"] = endTime ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ["totalDuration"] = totalDuration,
                ["completionStatus"] = completionStatus,
                ["interactions"] = interactions.Select(i => i.ToDictionary()).ToList(),
                ["summary"] = new Dictionary<string, object>
                {
                    ["totalInteractions"] = totalInteractions,
                    ["successfulInteractions"] = successfulInteractions,
                    ["failedInteractions"] = failedInteractions,
                    ["averageTimePerInteraction"] = GetAverageInteractionTime(),
                    ["totalAttempts"] = totalAttempts,
                    ["totalFailedAttempts"] = totalFailedAttempts,
                    ["successRate"] = GetSuccessRate()
                }
            };
        }

        /// <summary>
        /// Reset les analytics pour une nouvelle session
        /// </summary>
        public void ResetAnalytics()
        {
            InitializeSession();

            if (debugMode)
            {
                Debug.Log("[TrainingAnalytics] Analytics reset");
            }
        }

        // Méthodes utilitaires
        private float GetAverageInteractionTime()
        {
            if (interactions.Count == 0) return 0;
            return interactions.Average(i => i.duration);
        }

        private float GetSuccessRate()
        {
            if (totalInteractions == 0) return 0;
            return (float)successfulInteractions / totalInteractions * 100f;
        }

        /// <summary>
        /// Obtient l'interaction en cours (pour modification externe)
        /// </summary>
        public InteractionData GetCurrentInteraction()
        {
            return currentInteraction;
        }

        /// <summary>
        /// Obtient toutes les interactions
        /// </summary>
        public List<InteractionData> GetAllInteractions()
        {
            return new List<InteractionData>(interactions);
        }

        /// <summary>
        /// Active/désactive le mode debug
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }

        // Pour les tests
        [ContextMenu("Test Export Analytics")]
        public void TestExport()
        {
            // Ajouter quelques interactions de test
            var questionData = new QuestionInteractionData
            {
                questionText = "Test question?",
                options = new List<string> { "A", "B", "C" },
                correctAnswers = new List<int> { 0, 2 }
            };
            questionData.AddUserAttempt(new List<int> { 0 });
            questionData.AddUserAttempt(new List<int> { 0, 2 });
            TrackQuestionInteraction("test_object", "q1", questionData);
            EndCurrentInteraction(true);

            CompleteTraining();

            string json = ExportAnalytics();
            Debug.Log($"[TrainingAnalytics] Test Export:\n{json}");
        }
    }
}