using System;
using System.Collections.Generic;
using UnityEngine;

namespace WiseTwin.Analytics
{
    /// <summary>
    /// Représente les données d'une interaction utilisateur dans la formation
    /// Structure plate pour faciliter l'analyse et l'export
    /// </summary>
    [Serializable]
    public class InteractionData
    {
        public string interactionId;
        public string type; // "question", "procedure", "text"
        public string subtype; // "multiple_choice", "single_choice", "sequential", etc.
        public string objectId;
        public string startTime; // ISO 8601 format
        public string endTime; // ISO 8601 format
        public float duration; // en secondes
        public int attempts;
        public bool success;
        public Dictionary<string, object> data; // Données spécifiques au type d'interaction

        public InteractionData(string id, string interactionType, string interactionSubtype, string objId)
        {
            interactionId = id;
            type = interactionType;
            subtype = interactionSubtype;
            objectId = objId;
            startTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            attempts = 0;
            success = false;
            data = new Dictionary<string, object>();
        }

        public void EndInteraction(bool wasSuccessful)
        {
            endTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            success = wasSuccessful;

            // Calculer la durée
            if (DateTime.TryParse(startTime, out DateTime start) && DateTime.TryParse(endTime, out DateTime end))
            {
                duration = (float)(end - start).TotalSeconds;
            }
        }

        public void IncrementAttempts()
        {
            attempts++;
        }

        public void AddData(string key, object value)
        {
            data[key] = value;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["interactionId"] = interactionId,
                ["type"] = type,
                ["subtype"] = subtype,
                ["objectId"] = objectId,
                ["startTime"] = startTime,
                ["endTime"] = endTime,
                ["duration"] = duration,
                ["attempts"] = attempts,
                ["success"] = success,
                ["data"] = data
            };

            return dict;
        }
    }

    /// <summary>
    /// Données spécifiques pour les questions
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class QuestionInteractionData
    {
        // Configuration statique pour les logs
        public static bool EnableDebugLogs { get; set; } = false;

        public string questionKey;          // Ex: "question_2" - clé pour jointure avec metadata
        public string objectId;             // Ex: "red_cube" - pour retrouver dans metadata.unity[objectId][questionKey]
        public List<int> correctAnswers;
        public List<List<int>> userAnswers; // Historique de toutes les tentatives
        public bool firstAttemptCorrect;
        public float finalScore;

        public QuestionInteractionData()
        {
            correctAnswers = new List<int>();
            userAnswers = new List<List<int>>();
            firstAttemptCorrect = false;
            finalScore = 0f;
        }

        public void AddUserAttempt(List<int> attempt)
        {
            userAnswers.Add(new List<int>(attempt));

            // Vérifier si c'est correct au premier essai
            if (userAnswers.Count == 1)
            {
                firstAttemptCorrect = CheckIfCorrect(attempt);
                LogDebug($"First attempt check - Answer: {string.Join(",", attempt)}, Correct: {string.Join(",", correctAnswers)}, Result: {firstAttemptCorrect}");
            }

            // Mettre à jour le score seulement si correct du premier coup
            if (CheckIfCorrect(attempt) && userAnswers.Count == 1)
            {
                finalScore = 100f;
            }
        }

        private bool CheckIfCorrect(List<int> attempt)
        {
            if (attempt.Count != correctAnswers.Count) return false;

            var sortedAttempt = new List<int>(attempt);
            var sortedCorrect = new List<int>(correctAnswers);
            sortedAttempt.Sort();
            sortedCorrect.Sort();

            for (int i = 0; i < sortedAttempt.Count; i++)
            {
                if (sortedAttempt[i] != sortedCorrect[i]) return false;
            }

            return true;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["questionKey"] = questionKey,
                ["objectId"] = objectId,
                ["correctAnswers"] = correctAnswers,
                ["userAnswers"] = userAnswers,
                ["firstAttemptCorrect"] = firstAttemptCorrect,
                ["finalScore"] = finalScore
            };
        }

        private static void LogDebug(string message)
        {
            if (EnableDebugLogs)
            {
                Debug.Log($"[QuestionInteractionData] {message}");
            }
        }
    }

    /// <summary>
    /// Données spécifiques pour les procédures (procédure complète avec toutes ses étapes)
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class ProcedureInteractionData
    {
        public string procedureKey;         // Ex: "procedure_startup" - clé pour jointure avec metadata
        public string objectId;             // Ex: "yellow_capsule" - pour retrouver dans metadata.unity[objectId][procedureKey]
        public int totalSteps;              // Nombre total d'étapes
        public List<ProcedureStepData> steps; // Toutes les étapes de la procédure
        public int totalWrongClicks;        // Nombre total d'erreurs sur toute la procédure
        public float totalDuration;         // Durée totale de la procédure
        public bool perfectCompletion;      // true si aucune erreur

        public ProcedureInteractionData()
        {
            steps = new List<ProcedureStepData>();
            totalWrongClicks = 0;
            totalDuration = 0f;
            perfectCompletion = false;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["procedureKey"] = procedureKey,
                ["objectId"] = objectId,
                ["totalSteps"] = totalSteps,
                ["steps"] = steps.ConvertAll(s => s.ToDictionary()),
                ["totalWrongClicks"] = totalWrongClicks,
                ["totalDuration"] = totalDuration,
                ["perfectCompletion"] = perfectCompletion
            };
        }
    }

    /// <summary>
    /// Données pour une étape individuelle d'une procédure
    /// </summary>
    [Serializable]
    public class ProcedureStepData
    {
        public int stepNumber;              // Numéro de l'étape (1, 2, 3, 4...)
        public string stepKey;              // Ex: "step_1" - clé pour jointure avec metadata
        public string targetObjectId;       // Ex: "red_cube" - objet à cliquer pour cette étape
        public bool completed;              // true si l'étape a été complétée
        public float duration;              // Durée de cette étape en secondes
        public int wrongClicksOnThisStep;   // Nombre d'erreurs sur cette étape uniquement

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["stepNumber"] = stepNumber,
                ["stepKey"] = stepKey,
                ["targetObjectId"] = targetObjectId,
                ["completed"] = completed,
                ["duration"] = duration,
                ["wrongClicksOnThisStep"] = wrongClicksOnThisStep
            };
        }
    }

    /// <summary>
    /// Données spécifiques pour l'affichage de texte
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class TextInteractionData
    {
        public string contentKey;           // Ex: "text_content" - clé pour jointure avec metadata
        public string objectId;             // Ex: "green_cylinder" - pour retrouver dans metadata.unity[objectId][contentKey]
        public float timeDisplayed;
        public bool readComplete;
        public float scrollPercentage;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["contentKey"] = contentKey,
                ["objectId"] = objectId,
                ["timeDisplayed"] = timeDisplayed,
                ["readComplete"] = readComplete,
                ["scrollPercentage"] = scrollPercentage
            };
        }
    }
}