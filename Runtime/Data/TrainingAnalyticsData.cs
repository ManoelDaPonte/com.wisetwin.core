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
        public float score; // Score final basé sur les performances (0-100%)
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Question
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class QuestionAnalyticsData
    {
        public string questionKey;          // Clé pour retrouver la question dans les métadonnées
        public string objectId;             // ID de l'objet pour retrouver dans metadata.unity[objectId][questionKey]
        public List<int> correctAnswers;
        public List<List<int>> userAnswers; // Historique de toutes les tentatives
        public bool firstAttemptCorrect;
        public float finalScore;

        public QuestionAnalyticsData()
        {
            correctAnswers = new List<int>();
            userAnswers = new List<List<int>>();
        }
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Procédure
    /// Représente la procédure complète avec toutes ses étapes
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class ProcedureAnalyticsData
    {
        public string procedureKey;             // Clé de la procédure (ex: "procedure_startup")
        public string objectId;                 // ID de l'objet pour retrouver dans metadata.unity[objectId][procedureKey]
        public int totalSteps;                  // Nombre total d'étapes
        public List<ProcedureStepAnalyticsData> steps; // Toutes les étapes
        public int totalWrongClicks;            // Erreurs totales sur toute la procédure
        public float totalDuration;             // Durée totale
        public bool perfectCompletion;          // true si aucune erreur
        public float finalScore;                // Score final (100 si parfait, 0 sinon)

        public ProcedureAnalyticsData()
        {
            steps = new List<ProcedureStepAnalyticsData>();
        }
    }

    /// <summary>
    /// Données pour une étape individuelle d'une procédure
    /// </summary>
    [Serializable]
    public class ProcedureStepAnalyticsData
    {
        public int stepNumber;                  // Numéro de l'étape
        public string stepKey;                  // Clé de l'étape (ex: "step_1")
        public string targetObjectId;           // ID de l'objet cible
        public bool completed;                  // true si complétée
        public float duration;                  // Durée de l'étape
        public int wrongClicksOnThisStep;       // Erreurs sur cette étape uniquement
    }

    /// <summary>
    /// Données spécifiques pour une interaction de type Texte
    /// Utilise des clés au lieu de texte pour permettre la jointure avec les métadonnées
    /// </summary>
    [Serializable]
    public class TextAnalyticsData
    {
        public string contentKey;               // Clé du contenu texte (ex: "text_content")
        public string objectId;                 // ID de l'objet pour retrouver dans metadata.unity[objectId][contentKey]
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
// IMPORTANT: Toutes les données utilisent des CLÉS au lieu de texte
// Pour obtenir le texte, faire la jointure avec les métadonnées :
// metadata.unity[objectId][contentKey].text[language]

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
    // For questions - Utilise des clés uniquement
    questionKey?: string;           // Ex: 'question_2' pour metadata.unity[objectId].question_2
    objectId?: string;              // Pour retrouver dans metadata.unity[objectId]
    correctAnswers?: number[];      // Indices des réponses correctes
    userAnswers?: number[][];       // Historique des tentatives
    firstAttemptCorrect?: boolean;
    finalScore?: number;

    // For procedures - Procédure complète avec toutes les étapes
    procedureKey?: string;          // Ex: 'procedure_startup' pour metadata.unity[objectId].procedure_startup
    totalSteps?: number;            // Nombre total d'étapes
    steps?: ProcedureStep[];        // Toutes les étapes de la procédure
    totalWrongClicks?: number;      // Erreurs totales
    totalDuration?: number;         // Durée totale de la procédure
    perfectCompletion?: boolean;    // true si aucune erreur
    finalScore?: number;

    // For text - Utilise des clés uniquement
    contentKey?: string;            // Ex: 'text_content' pour metadata.unity[objectId].text_content
    timeDisplayed?: number;
    readComplete?: boolean;
    scrollPercentage?: number;
  };
}

interface ProcedureStep {
  stepNumber: number;               // 1, 2, 3, 4...
  stepKey: string;                  // Ex: 'step_1' pour metadata.unity[objectId].procedure_startup.step_1
  targetObjectId: string;           // ID de l'objet à cliquer
  completed: boolean;
  duration: number;
  wrongClicksOnThisStep: number;
}

interface AnalyticsSummary {
  totalInteractions: number;
  successfulInteractions: number;
  failedInteractions: number;
  averageTimePerInteraction: number;
  totalAttempts: number;
  totalFailedAttempts: number;
  successRate: number; // percentage
  score: number; // final score based on performance (0-100%)
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
window.ReceiveTrainingAnalytics = (analyticsData: TrainingAnalytics, metadata: any) => {
  console.log('Training completed', analyticsData);

  // Exemple de jointure avec les métadonnées pour récupérer le texte
  analyticsData.interactions.forEach(interaction => {
    if (interaction.type === 'question') {
      const questionKey = interaction.data.questionKey;
      const objectId = interaction.data.objectId;

      // Récupérer le texte de la question depuis les métadonnées
      const questionText = metadata.unity[objectId][questionKey].text[currentLanguage];
      const options = metadata.unity[objectId][questionKey].options[currentLanguage];

      console.log('Question:', questionText);
      console.log('Options:', options);
      console.log('User answers:', interaction.data.userAnswers);
    }

    if (interaction.type === 'procedure') {
      const procedureKey = interaction.data.procedureKey;
      const objectId = interaction.objectId;

      // Récupérer le titre de la procédure depuis les métadonnées
      const procedureTitle = metadata.unity[objectId][procedureKey].title[currentLanguage];

      console.log('Procedure:', procedureTitle);
      console.log('Steps:', interaction.data.steps);
      console.log('Perfect completion:', interaction.data.perfectCompletion);
    }

    if (interaction.type === 'text') {
      const contentKey = interaction.data.contentKey;
      const objectId = interaction.data.objectId;

      // Récupérer le contenu texte depuis les métadonnées
      const textContent = metadata.unity[objectId][contentKey].content[currentLanguage];

      console.log('Text read:', textContent.substring(0, 100) + '...');
      console.log('Read complete:', interaction.data.readComplete);
    }
  });

  // Calculate metrics
  const difficultQuestions = analyticsData.interactions
    .filter(i => i.type === 'question' && !i.data.firstAttemptCorrect);

  const averageQuestionTime = analyticsData.interactions
    .filter(i => i.type === 'question')
    .reduce((acc, i) => acc + i.duration, 0) /
    analyticsData.interactions.filter(i => i.type === 'question').length;

  // Send to backend (les données sont déjà normalisées avec des clés)
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