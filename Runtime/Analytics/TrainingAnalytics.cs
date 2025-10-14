using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

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
            trainingId = SceneManager.GetActiveScene().name;
            startTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            interactions = new List<InteractionData>();
            moduleStartTimes = new Dictionary<string, float>();

            // Réinitialiser les statistiques
            totalInteractions = 0;
            successfulInteractions = 0;
            failedInteractions = 0;
            totalAttempts = 0;
            totalFailedAttempts = 0;

            LogDebug($"Session initialized: {sessionId} for training: {trainingId}");
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

            LogDebug($"Started interaction: {interactionId} (type: {type}, subtype: {subtype})");

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

                LogDebug($"Ended interaction: {currentInteraction.interactionId} - Success: {success}, Duration: {currentInteraction.duration}s");

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
        /// Démarre le tracking d'une procédure complète (toutes les étapes seront accumulées)
        /// </summary>
        public void StartProcedureInteraction(string objectId, string procedureKey, int totalSteps)
        {
            // Terminer l'interaction précédente si elle existe
            if (currentInteraction != null)
            {
                EndCurrentInteraction(false);
            }

            string interactionId = $"{objectId}_{procedureKey}_{DateTime.UtcNow.Ticks}";
            currentInteraction = new InteractionData(interactionId, "procedure", "sequential", objectId);

            // Initialiser les données de la procédure
            var procedureData = new ProcedureInteractionData
            {
                procedureKey = procedureKey,
                objectId = objectId,
                totalSteps = totalSteps,
                steps = new List<ProcedureStepData>()
            };

            // Stocker les données initiales
            var dataDict = procedureData.ToDictionary();
            foreach (var kvp in dataDict)
            {
                currentInteraction.AddData(kvp.Key, kvp.Value);
            }

            currentInteraction.attempts = 1;

            LogDebug($"Started procedure interaction: {interactionId} (key: {procedureKey}, steps: {totalSteps})");
        }

        /// <summary>
        /// Ajoute une étape complétée à la procédure en cours
        /// </summary>
        public void AddProcedureStepData(ProcedureStepData stepData)
        {
            if (currentInteraction == null || currentInteraction.type != "procedure")
            {
                LogError("Cannot add procedure step: no procedure interaction in progress");
                return;
            }

            // Récupérer la liste des étapes existantes
            if (currentInteraction.data.ContainsKey("steps") && currentInteraction.data["steps"] is List<Dictionary<string, object>> stepsList)
            {
                stepsList.Add(stepData.ToDictionary());
            }
            else
            {
                // Créer une nouvelle liste si elle n'existe pas
                var newStepsList = new List<Dictionary<string, object>> { stepData.ToDictionary() };
                currentInteraction.data["steps"] = newStepsList;
            }

            LogDebug($"Added step {stepData.stepNumber} to procedure (wrong clicks: {stepData.wrongClicksOnThisStep})");
        }

        /// <summary>
        /// Termine le tracking de la procédure en cours avec les statistiques finales
        /// </summary>
        public void CompleteProcedureInteraction(bool perfectCompletion, int totalWrongClicks, float totalDuration)
        {
            if (currentInteraction == null || currentInteraction.type != "procedure")
            {
                LogError("Cannot complete procedure: no procedure interaction in progress");
                return;
            }

            // Mettre à jour les données finales
            currentInteraction.AddData("perfectCompletion", perfectCompletion);
            currentInteraction.AddData("totalWrongClicks", totalWrongClicks);
            currentInteraction.AddData("totalDuration", totalDuration);

            // Calculer le nombre d'étapes parfaites (sans erreurs)
            int perfectStepsCount = 0;
            int totalSteps = 0;

            if (currentInteraction.data.ContainsKey("steps") &&
                currentInteraction.data["steps"] is List<Dictionary<string, object>> stepsList)
            {
                totalSteps = stepsList.Count;
                foreach (var step in stepsList)
                {
                    if (step.ContainsKey("wrongClicksOnThisStep"))
                    {
                        int wrongClicks = 0;
                        if (step["wrongClicksOnThisStep"] is int wrongClicksInt)
                            wrongClicks = wrongClicksInt;
                        else if (step["wrongClicksOnThisStep"] is long wrongClicksLong)
                            wrongClicks = (int)wrongClicksLong;

                        if (wrongClicks == 0)
                            perfectStepsCount++;
                    }
                }
            }

            // Stocker le nombre d'étapes parfaites pour les statistiques
            currentInteraction.AddData("perfectStepsCount", perfectStepsCount);
            currentInteraction.AddData("totalSteps", totalSteps);

            // Le finalScore représente maintenant le pourcentage d'étapes parfaites
            // Chaque étape parfaite vaut 100 points
            float finalScore = totalSteps > 0 ? (perfectStepsCount * 100f) : 100f;
            currentInteraction.AddData("finalScore", finalScore);

            LogDebug($"Completing procedure - Perfect steps: {perfectStepsCount}/{totalSteps}, Wrong clicks: {totalWrongClicks}, Duration: {totalDuration}s");

            // Terminer l'interaction (succès si au moins une étape complétée)
            EndCurrentInteraction(totalSteps > 0);
        }

        /// <summary>
        /// Track l'affichage d'un texte
        /// </summary>
        public void TrackTextDisplay(string objectId, string contentKey, TextInteractionData textData)
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
                LogDebug($"Training completed: {status}");
                LogDebug($"Total interactions: {GetTotalInteractions()}, Success rate: {GetSuccessRate():F2}%");
            }

            if (autoExportOnCompletion)
            {
                string analytics = ExportAnalytics();
                if (debugMode)
                {
                    LogDebug($"Exported analytics: {analytics}");
                }
            }
        }

        /// <summary>
        /// Exporte toutes les analytics en JSON
        /// </summary>
        public string ExportAnalytics()
        {
            // Récupérer la version depuis les métadonnées
            string version = null;
            if (MetadataLoader.Instance != null && MetadataLoader.Instance.IsLoaded)
            {
                version = MetadataLoader.Instance.GetProjectInfo("version");
            }

            var analyticsData = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["trainingId"] = trainingId,
                ["version"] = version ?? "unknown",
                ["startTime"] = startTime,
                ["endTime"] = endTime ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ["totalDuration"] = totalDuration,
                ["completionStatus"] = completionStatus,
                ["interactions"] = interactions.Select(i => i.ToDictionary()).ToList(),
                ["summary"] = new Dictionary<string, object>
                {
                    ["totalInteractions"] = GetTotalInteractions(),
                    ["successfulInteractions"] = GetSuccessfulInteractions(),
                    ["failedInteractions"] = GetFailedInteractions(),
                    ["averageTimePerInteraction"] = GetAverageInteractionTime(),
                    ["totalAttempts"] = totalAttempts,
                    ["totalFailedAttempts"] = totalFailedAttempts,
                    ["successRate"] = GetSuccessRate(),
                    ["score"] = CalculateScore()
                }
            };

            return JsonConvert.SerializeObject(analyticsData, Formatting.Indented);
        }

        /// <summary>
        /// Obtient les analytics sous forme de dictionnaire
        /// </summary>
        public Dictionary<string, object> GetAnalyticsDictionary()
        {
            // Récupérer la version depuis les métadonnées
            string version = null;
            if (MetadataLoader.Instance != null && MetadataLoader.Instance.IsLoaded)
            {
                version = MetadataLoader.Instance.GetProjectInfo("version");
            }

            return new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["trainingId"] = trainingId,
                ["version"] = version ?? "unknown",
                ["startTime"] = startTime,
                ["endTime"] = endTime ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ["totalDuration"] = totalDuration,
                ["completionStatus"] = completionStatus,
                ["interactions"] = interactions.Select(i => i.ToDictionary()).ToList(),
                ["summary"] = new Dictionary<string, object>
                {
                    ["totalInteractions"] = GetTotalInteractions(),
                    ["successfulInteractions"] = GetSuccessfulInteractions(),
                    ["failedInteractions"] = GetFailedInteractions(),
                    ["averageTimePerInteraction"] = GetAverageInteractionTime(),
                    ["totalAttempts"] = totalAttempts,
                    ["totalFailedAttempts"] = totalFailedAttempts,
                    ["successRate"] = GetSuccessRate(),
                    ["score"] = CalculateScore()
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
                LogDebug("Analytics reset");
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
            int total = GetTotalInteractions();
            if (total == 0) return 0;
            return (float)GetSuccessfulInteractions() / total * 100f;
        }

        /// <summary>
        /// Calcule un score basé sur le nombre d'interactions réussies du premier coup
        /// Pour les procédures, chaque étape compte individuellement
        /// Score = (nombre de points / nombre total d'interactions) × 100
        /// </summary>
        public float CalculateScore()
        {
            if (interactions.Count == 0) return 100f;

            int totalPoints = 0;
            int totalInteractionWeight = 0;

            foreach (var interaction in interactions)
            {
                if (interaction.type == "procedure")
                {
                    // Pour les procédures, chaque étape compte
                    int totalSteps = GetIntValue(interaction.data, "totalSteps", 0);
                    int perfectSteps = GetIntValue(interaction.data, "perfectStepsCount", 0);

                    totalInteractionWeight += totalSteps;
                    totalPoints += perfectSteps;
                }
                else
                {
                    // Pour les autres interactions (question, text), 1 point si parfait
                    totalInteractionWeight += 1;

                    if (interaction.data != null && interaction.data.ContainsKey("finalScore"))
                    {
                        float finalScore = GetFloatValue(interaction.data, "finalScore", 0f);
                        totalPoints += finalScore >= 100f ? 1 : 0;
                    }
                    else
                    {
                        totalPoints += 1; // Par défaut parfait
                    }
                }
            }

            if (totalInteractionWeight == 0) return 100f;

            // Score = (points obtenus / poids total) × 100
            return (float)totalPoints / totalInteractionWeight * 100f;
        }

        /// <summary>
        /// Obtient le nombre total d'erreurs (tentatives échouées)
        /// </summary>
        public int GetTotalErrors()
        {
            return totalFailedAttempts;
        }

        /// <summary>
        /// Obtient le nombre total d'interactions (en comptant chaque étape de procédure)
        /// </summary>
        public int GetTotalInteractions()
        {
            int total = 0;

            foreach (var interaction in interactions)
            {
                if (interaction.type == "procedure")
                {
                    // Pour les procédures, compter le nombre d'étapes
                    total += GetIntValue(interaction.data, "totalSteps", 0);
                }
                else
                {
                    // Pour les autres interactions, compter 1
                    total += 1;
                }
            }

            return total;
        }

        /// <summary>
        /// Obtient le nombre d'interactions réussies du premier coup (parfaites)
        /// Pour les procédures, compte chaque étape parfaite
        /// successfulInteractions + failedInteractions = totalInteractions
        /// </summary>
        public int GetSuccessfulInteractions()
        {
            int perfectCount = 0;

            foreach (var interaction in interactions)
            {
                if (interaction.type == "procedure")
                {
                    // Pour les procédures, compter les étapes parfaites
                    perfectCount += GetIntValue(interaction.data, "perfectStepsCount", 0);
                }
                else
                {
                    // Pour les autres interactions, vérifier le finalScore
                    if (interaction.data != null && interaction.data.ContainsKey("finalScore"))
                    {
                        float finalScore = GetFloatValue(interaction.data, "finalScore", 0f);
                        if (finalScore >= 100f)
                            perfectCount++;
                    }
                    else
                    {
                        perfectCount++; // Par défaut parfait
                    }
                }
            }

            return perfectCount;
        }

        /// <summary>
        /// Obtient le nombre d'interactions ratées (avec erreurs)
        /// Pour les procédures, compte chaque étape avec erreurs
        /// successfulInteractions + failedInteractions = totalInteractions
        /// </summary>
        public int GetFailedInteractions()
        {
            return GetTotalInteractions() - GetSuccessfulInteractions();
        }

        /// <summary>
        /// [DEPRECATED] Utiliser GetSuccessfulInteractions() à la place
        /// Obtient le nombre d'interactions parfaites (sans erreur, du premier coup)
        /// Pour les procédures, compte le nombre d'étapes parfaites
        /// </summary>
        public int GetPerfectInteractions()
        {
            return GetSuccessfulInteractions();
        }

        /// <summary>
        /// [DEPRECATED] Utiliser GetFailedInteractions() à la place
        /// Obtient le nombre d'interactions avec erreurs (réussies mais après des tentatives ratées)
        /// Pour les procédures, compte les étapes avec erreurs
        /// </summary>
        public int GetInteractionsWithErrors()
        {
            return GetFailedInteractions();
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
            // Ajouter quelques interactions de test avec la nouvelle API (clés uniquement)
            var questionData = new QuestionInteractionData
            {
                questionKey = "test_question_1",
                objectId = "test_object",
                correctAnswers = new List<int> { 0, 2 }
            };
            questionData.AddUserAttempt(new List<int> { 0 });
            questionData.AddUserAttempt(new List<int> { 0, 2 });
            TrackQuestionInteraction("test_object", "test_object_test_question_1", questionData);
            EndCurrentInteraction(true);

            CompleteTraining();

            string json = ExportAnalytics();
            LogDebug($"Test Export:\n{json}");
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[TrainingAnalytics] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TrainingAnalytics] {message}");
        }

        // Helper methods pour extraire des valeurs des dictionnaires
        private int GetIntValue(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (dict == null || !dict.ContainsKey(key)) return defaultValue;

            if (dict[key] is int intValue)
                return intValue;
            else if (dict[key] is long longValue)
                return (int)longValue;
            else if (dict[key] is float floatValue)
                return (int)floatValue;
            else if (dict[key] is double doubleValue)
                return (int)doubleValue;

            return defaultValue;
        }

        private float GetFloatValue(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (dict == null || !dict.ContainsKey(key)) return defaultValue;

            if (dict[key] is float floatValue)
                return floatValue;
            else if (dict[key] is double doubleValue)
                return (float)doubleValue;
            else if (dict[key] is int intValue)
                return intValue;
            else if (dict[key] is long longValue)
                return longValue;

            return defaultValue;
        }
    }
}