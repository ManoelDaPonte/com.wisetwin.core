using System;
using System.Collections.Generic;
using UnityEngine;

namespace WiseTwin.Data
{
    /// <summary>
    /// Structure de données complète pour les analytics de formation
    /// Cette classe définit le format JSON exact qui sera envoyé à React
    /// </summary>
    [Serializable]
    public class TrainingAnalyticsData
    {
        public string sessionId;
        public string trainingId;
        public string startTime; // ISO 8601 format
        public string endTime;   // ISO 8601 format
        public float totalDuration; // en secondes
        public string completionStatus; // "completed", "abandoned", "in_progress"
        public List<InteractionRecord> interactions;
        public AnalyticsSummary summary;

        public TrainingAnalyticsData()
        {
            interactions = new List<InteractionRecord>();
            summary = new AnalyticsSummary();
        }
    }

    /// <summary>
    /// Enregistrement d'une interaction individuelle
    /// </summary>
    [Serializable]
    public class InteractionRecord
    {
        public string interactionId;
        public string type; // "question", "procedure", "text"
        public string subtype; // "multiple_choice", "single_choice", "sequential", "informative"
        public string objectId;
        public string startTime; // ISO 8601
        public string endTime;   // ISO 8601
        public float duration;   // en secondes
        public int attempts;
        public bool success;
        public Dictionary<string, object> data; // Données spécifiques au type

        public InteractionRecord()
        {
            data = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Résumé statistique de la session
    /// </summary>
    [Serializable]
    public class AnalyticsSummary
    {
        public int totalInteractions;
        public int successfulInteractions;
        public int failedInteractions;
        public float averageTimePerInteraction;
        public int totalAttempts;
        public int totalFailedAttempts;
        public float successRate; // Pourcentage
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Question
    /// </summary>
    [Serializable]
    public class QuestionAnalyticsData
    {
        public string questionText;
        public List<string> options;
        public List<int> correctAnswers;
        public List<List<int>> userAnswers; // Historique de toutes les tentatives
        public bool firstAttemptCorrect;
        public float finalScore;

        public QuestionAnalyticsData()
        {
            options = new List<string>();
            correctAnswers = new List<int>();
            userAnswers = new List<List<int>>();
        }
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Procédure
    /// </summary>
    [Serializable]
    public class ProcedureAnalyticsData
    {
        public int stepNumber;
        public int totalSteps;
        public string instruction;
        public int hintsUsed;
        public int wrongClicks;
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Texte
    /// </summary>
    [Serializable]
    public class TextAnalyticsData
    {
        public string textContent;
        public float timeDisplayed;
        public bool readComplete;
        public float scrollPercentage;
    }

    /// <summary>
    /// Types d'interactions supportés dans les analytics
    /// </summary>
    public static class InteractionTypes
    {
        public const string Question = "question";
        public const string Procedure = "procedure";
        public const string Text = "text";
    }

    /// <summary>
    /// Sous-types d'interactions
    /// </summary>
    public static class InteractionSubtypes
    {
        // Questions
        public const string MultipleChoice = "multiple_choice";
        public const string SingleChoice = "single_choice";

        // Procedures
        public const string Sequential = "sequential";
        public const string Parallel = "parallel";

        // Text
        public const string Informative = "informative";
        public const string Tutorial = "tutorial";
    }

    /// <summary>
    /// Status de complétion
    /// </summary>
    public static class CompletionStatus
    {
        public const string InProgress = "in_progress";
        public const string Completed = "completed";
        public const string Abandoned = "abandoned";
        public const string Failed = "failed";
    }

    /// <summary>
    /// Helper pour TypeScript/React
    /// Cette section documente la structure pour l'équipe React
    /// </summary>
    public static class TypeScriptInterface
    {
        public const string InterfaceDefinition = @"
// TypeScript Interface for React
interface TrainingAnalytics {
  sessionId: string;
  trainingId: string;
  startTime: string; // ISO 8601
  endTime: string;   // ISO 8601
  totalDuration: number; // seconds
  completionStatus: 'completed' | 'abandoned' | 'in_progress' | 'failed';
  interactions: InteractionRecord[];
  summary: AnalyticsSummary;
}

interface InteractionRecord {
  interactionId: string;
  type: 'question' | 'procedure' | 'text';
  subtype: string;
  objectId: string;
  startTime: string;
  endTime: string;
  duration: number;
  attempts: number;
  success: boolean;
  data: {
    // For questions
    questionText?: string;
    options?: string[];
    correctAnswers?: number[];
    userAnswers?: number[][];
    firstAttemptCorrect?: boolean;
    finalScore?: number;

    // For procedures
    stepNumber?: number;
    totalSteps?: number;
    instruction?: string;
    hintsUsed?: number;
    wrongClicks?: number;

    // For text
    textContent?: string;
    timeDisplayed?: number;
    readComplete?: boolean;
    scrollPercentage?: number;
  };
}

interface AnalyticsSummary {
  totalInteractions: number;
  successfulInteractions: number;
  failedInteractions: number;
  averageTimePerInteraction: number;
  totalAttempts: number;
  totalFailedAttempts: number;
  successRate: number; // percentage
}
";
    }

    /// <summary>
    /// Exemple d'utilisation pour React
    /// </summary>
    public static class ReactExample
    {
        public const string ExampleCode = @"
// React Example Usage
window.ReceiveTrainingAnalytics = (analyticsData: TrainingAnalytics) => {
  console.log('Training completed', analyticsData);

  // Calculate metrics
  const difficultQuestions = analyticsData.interactions
    .filter(i => i.type === 'question' && !i.data.firstAttemptCorrect);

  const averageQuestionTime = analyticsData.interactions
    .filter(i => i.type === 'question')
    .reduce((acc, i) => acc + i.duration, 0) /
    analyticsData.interactions.filter(i => i.type === 'question').length;

  // Send to backend
  fetch('/api/training/analytics', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(analyticsData)
  });

  // Update UI
  setTrainingResults(analyticsData);
};
";
    }
}