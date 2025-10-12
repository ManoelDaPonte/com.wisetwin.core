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
    /// </summary>
    [Serializable]
    public class QuestionInteractionData
    {
        // Configuration statique pour les logs
        public static bool EnableDebugLogs { get; set; } = false;

        public string questionText;
        public List<string> options;
        public List<int> correctAnswers;
        public List<List<int>> userAnswers; // Historique de toutes les tentatives
        public bool firstAttemptCorrect;
        public float finalScore;

        public QuestionInteractionData()
        {
            options = new List<string>();
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
                ["questionText"] = questionText,
                ["options"] = options,
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
    /// Données spécifiques pour les procédures
    /// </summary>
    [Serializable]
    public class ProcedureInteractionData
    {
        public int stepNumber;
        public int totalSteps;
        public string title;
        public string instruction;
        public int hintsUsed;
        public int wrongClicks;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["stepNumber"] = stepNumber,
                ["totalSteps"] = totalSteps,
                ["title"] = title,
                ["instruction"] = instruction,
                ["hintsUsed"] = hintsUsed,
                ["wrongClicks"] = wrongClicks
            };
        }
    }

    /// <summary>
    /// Données spécifiques pour l'affichage de texte
    /// </summary>
    [Serializable]
    public class TextInteractionData
    {
        public string textContent;
        public float timeDisplayed;
        public bool readComplete;
        public float scrollPercentage;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["textContent"] = textContent?.Substring(0, Math.Min(100, textContent?.Length ?? 0)) + "...",
                ["timeDisplayed"] = timeDisplayed,
                ["readComplete"] = readComplete,
                ["scrollPercentage"] = scrollPercentage
            };
        }
    }
}