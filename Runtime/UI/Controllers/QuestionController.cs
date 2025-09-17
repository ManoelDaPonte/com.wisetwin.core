using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WiseTwin
{
    /// <summary>
    /// Contrôleur pour gérer l'affichage et la validation des questions
    /// Gère le multilingue et envoie les résultats au système de tracking
    /// </summary>
    public class QuestionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool autoShowNextQuestion = false;
        [SerializeField] private float delayBeforeNext = 2f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Références
        private WiseTwinUIManager uiManager;
        private LocalizationManager localizationManager;
        private WiseTwinManager wiseTwinManager;

        // Question actuelle
        private string currentObjectId;
        private string currentQuestionKey;
        private Dictionary<string, object> currentQuestionData;
        private int correctAnswerIndex = -1;
        private float questionStartTime;
        private int attemptCount = 0;

        // Tracking des résultats
        private List<QuestionResult> sessionResults = new List<QuestionResult>();

        // Singleton
        public static QuestionController Instance { get; private set; }

        // Events
        public System.Action<QuestionResult> OnQuestionAnswered;
        public System.Action<List<QuestionResult>> OnAllQuestionsCompleted;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // Récupérer les références
            uiManager = WiseTwinUIManager.Instance;
            if (uiManager == null)
            {
                Debug.LogError("[QuestionController] WiseTwinUIManager not found!");
            }

            localizationManager = LocalizationManager.Instance;
            if (localizationManager == null)
            {
                Debug.LogWarning("[QuestionController] LocalizationManager not found, using default language");
            }

            wiseTwinManager = WiseTwinManager.Instance;

            // S'abonner aux événements UI
            if (uiManager != null)
            {
                uiManager.OnAnswerSelected += OnAnswerSelected;
                uiManager.OnQuestionSubmitted += OnQuestionSubmitted;
            }
        }

        /// <summary>
        /// Affiche une question depuis les métadonnées
        /// </summary>
        public void ShowQuestion(string objectId, string questionKey, Dictionary<string, object> questionData)
        {
            if (debugMode) Debug.Log($"[QuestionController] Showing question: {objectId}.{questionKey}");

            // Sauvegarder les données actuelles
            currentObjectId = objectId;
            currentQuestionKey = questionKey;
            currentQuestionData = questionData;
            attemptCount = 0;
            questionStartTime = Time.time;

            // Extraire les données localisées
            string lang = localizationManager != null ? localizationManager.CurrentLanguage : "en";

            // Question text
            string questionText = ExtractLocalizedField(questionData, "text", lang);

            // Type de question
            string questionType = questionData.ContainsKey("type") ? questionData["type"].ToString() : "multiple-choice";

            // Options (pour QCM)
            List<string> options = ExtractLocalizedOptions(questionData, "options", lang);

            // Réponse correcte
            if (questionData.ContainsKey("correctAnswer"))
            {
                if (int.TryParse(questionData["correctAnswer"].ToString(), out int index))
                {
                    correctAnswerIndex = index;
                }
            }

            // Afficher dans l'UI
            if (uiManager != null)
            {
                if (questionType == "multiple-choice")
                {
                    uiManager.ShowQuestion(questionText, options.ToArray(), QuestionType.MultipleChoice);
                }
                else if (questionType == "true-false")
                {
                    // Pour true/false, créer les options automatiquement
                    string[] tfOptions = GetTrueFalseOptions(lang);
                    uiManager.ShowQuestion(questionText, tfOptions, QuestionType.TrueFalse);
                }
                else
                {
                    // Type non supporté pour l'instant
                    Debug.LogWarning($"[QuestionController] Question type '{questionType}' not yet supported");
                }
            }
        }

        string ExtractLocalizedField(Dictionary<string, object> data, string fieldName, string language)
        {
            if (!data.ContainsKey(fieldName)) return "";

            var fieldData = data[fieldName];

            // Si c'est directement une string
            if (fieldData is string simpleText)
            {
                return simpleText;
            }

            // Si c'est un dictionnaire de langues
            if (fieldData is Dictionary<string, object> localizedData)
            {
                // Essayer la langue demandée
                if (localizedData.ContainsKey(language))
                {
                    return localizedData[language]?.ToString() ?? "";
                }

                // Fallback to English
                if (localizedData.ContainsKey("en"))
                {
                    return localizedData["en"]?.ToString() ?? "";
                }

                // Prendre la première disponible
                if (localizedData.Count > 0)
                {
                    return localizedData.First().Value?.ToString() ?? "";
                }
            }

            return "";
        }

        List<string> ExtractLocalizedOptions(Dictionary<string, object> data, string fieldName, string language)
        {
            var result = new List<string>();

            if (!data.ContainsKey(fieldName)) return result;

            var optionsData = data[fieldName];

            // Si c'est directement une liste
            if (optionsData is List<object> simpleList)
            {
                foreach (var item in simpleList)
                {
                    result.Add(item?.ToString() ?? "");
                }
                return result;
            }

            // Si c'est un dictionnaire de langues contenant des listes
            if (optionsData is Dictionary<string, object> localizedOptions)
            {
                // Essayer la langue demandée
                if (localizedOptions.ContainsKey(language))
                {
                    var langOptions = localizedOptions[language];

                    // Newtonsoft.Json peut retourner un JArray
                    if (langOptions is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        foreach (var item in jArray)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                    else if (langOptions is List<object> list)
                    {
                        foreach (var item in list)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                }

                // Fallback to English si aucune option trouvée
                if (result.Count == 0 && localizedOptions.ContainsKey("en"))
                {
                    var enOptions = localizedOptions["en"];

                    if (enOptions is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        foreach (var item in jArray)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                    else if (enOptions is List<object> list)
                    {
                        foreach (var item in list)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                }
            }

            return result;
        }

        string[] GetTrueFalseOptions(string language)
        {
            // Options true/false selon la langue
            if (language == "fr")
            {
                return new string[] { "Vrai", "Faux" };
            }
            else
            {
                return new string[] { "True", "False" };
            }
        }

        void OnAnswerSelected(int answerIndex)
        {
            if (debugMode) Debug.Log($"[QuestionController] Answer selected: {answerIndex}");
            // L'utilisateur a sélectionné une réponse (mais pas encore validé)
        }

        void OnQuestionSubmitted()
        {
            if (debugMode) Debug.Log($"[QuestionController] Question submitted");

            // Récupérer l'index de la réponse sélectionnée depuis l'UI
            // (Normalement stocké via OnAnswerSelected, mais on peut aussi le demander à l'UI)
            ValidateAnswer();
        }

        void ValidateAnswer()
        {
            attemptCount++;
            float timeSpent = Time.time - questionStartTime;

            // Récupérer la réponse sélectionnée (déjà stockée via OnAnswerSelected)
            int selectedAnswer = uiManager != null ? uiManager.GetSelectedAnswerIndex() : -1;

            bool isCorrect = (selectedAnswer == correctAnswerIndex);

            string lang = localizationManager != null ? localizationManager.CurrentLanguage : "en";

            // Créer le résultat
            var result = new QuestionResult
            {
                questionId = $"{currentObjectId}.{currentQuestionKey}",
                question = ExtractLocalizedField(currentQuestionData, "text", lang),
                userAnswer = selectedAnswer.ToString(),
                correctAnswer = correctAnswerIndex.ToString(),
                isCorrect = isCorrect,
                timeSpent = timeSpent,
                attempts = attemptCount,
                score = isCorrect ? 10 : 0,
                maxScore = 10,
                answeredAt = System.DateTime.Now
            };

            sessionResults.Add(result);

            // Afficher le feedback
            ShowFeedback(isCorrect);

            // Déclencher l'événement
            OnQuestionAnswered?.Invoke(result);

            // Passer à la question suivante si configuré
            if (autoShowNextQuestion && isCorrect)
            {
                Invoke(nameof(ShowNextQuestion), delayBeforeNext);
            }
        }

        void ShowFeedback(bool isCorrect)
        {
            string lang = localizationManager != null ? localizationManager.CurrentLanguage : "en";
            string feedback;

            if (isCorrect)
            {
                feedback = ExtractLocalizedField(currentQuestionData, "feedback", lang);
                if (string.IsNullOrEmpty(feedback))
                {
                    feedback = lang == "fr" ? "Bonne réponse !" : "Correct!";
                }

                if (uiManager != null)
                {
                    uiManager.ShowNotification(feedback, NotificationType.Success);
                }
            }
            else
            {
                feedback = ExtractLocalizedField(currentQuestionData, "incorrectFeedback", lang);
                if (string.IsNullOrEmpty(feedback))
                {
                    feedback = lang == "fr" ? "Mauvaise réponse. Essayez encore !" : "Incorrect. Try again!";
                }

                if (uiManager != null)
                {
                    uiManager.ShowNotification(feedback, NotificationType.Error);
                }
            }
        }

        void ShowNextQuestion()
        {
            // TODO: Implémenter la logique pour passer à la question suivante
            // Pour l'instant, on ferme juste la question actuelle
            if (uiManager != null)
            {
                uiManager.HideQuestion();
            }
        }

        /// <summary>
        /// Récupère tous les résultats de la session
        /// </summary>
        public List<QuestionResult> GetSessionResults()
        {
            return new List<QuestionResult>(sessionResults);
        }

        /// <summary>
        /// Réinitialise les résultats de la session
        /// </summary>
        public void ResetSession()
        {
            sessionResults.Clear();
            currentObjectId = null;
            currentQuestionKey = null;
            currentQuestionData = null;
            correctAnswerIndex = -1;
            attemptCount = 0;
        }

        /// <summary>
        /// Calcule le score total de la session
        /// </summary>
        public float GetSessionScore()
        {
            if (sessionResults.Count == 0) return 0;

            int totalScore = sessionResults.Sum(r => r.score);
            int maxScore = sessionResults.Sum(r => r.maxScore);

            return maxScore > 0 ? (float)totalScore / maxScore * 100f : 0f;
        }

        void OnDestroy()
        {
            // Se désabonner des événements
            if (uiManager != null)
            {
                uiManager.OnAnswerSelected -= OnAnswerSelected;
                uiManager.OnQuestionSubmitted -= OnQuestionSubmitted;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Pour les tests dans l'éditeur
        /// </summary>
        [ContextMenu("Test Question")]
        public void TestQuestion()
        {
            var testData = new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, object>
                {
                    ["en"] = "Test question in English?",
                    ["fr"] = "Question de test en français?"
                },
                ["type"] = "multiple-choice",
                ["options"] = new Dictionary<string, object>
                {
                    ["en"] = new List<object> { "Option A", "Option B", "Option C" },
                    ["fr"] = new List<object> { "Option A", "Option B", "Option C" }
                },
                ["correctAnswer"] = 1,
                ["feedback"] = new Dictionary<string, object>
                {
                    ["en"] = "Well done!",
                    ["fr"] = "Bien joué!"
                }
            };

            ShowQuestion("test_object", "test_question", testData);
        }
    }
}