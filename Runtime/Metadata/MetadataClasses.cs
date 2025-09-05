using System.Collections.Generic;

namespace WiseTwin
{
    // Classes de données pour les différents types de contenu
    [System.Serializable]
    public class QuestionData
    {
        public string title = "Question Interactive";
        public string text = "Voici votre question...";
        public QuestionType type = QuestionType.MultipleChoice;
        public List<string> options = new List<string>();
        public int correctAnswerIndex = 0;
        public bool correctAnswer = true;
        public string correctFeedback = "Correct ! Bien joué.";
        public string incorrectFeedback = "Incorrect. Essayez encore.";
    }

    [System.Serializable]
    public class MediaData
    {
        public string title;
        public string description;
        public string mediaUrl;
        public string duration;
        public string type;
    }

    [System.Serializable]
    public class DialogueData
    {
        public string[] lines;
        public string[] choices;
        public string character;
        public string emotion;
    }

    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        TextInput
    }
}

// Classes partagées entre Editor et Runtime
[System.Serializable]
public class FormationMetadataComplete
{
    public string id;
    public string title;
    public string description;
    public string version;
    public string category;
    public string duration;
    public string difficulty;
    public List<string> tags;
    public string imageUrl;
    public List<object> modules;
    public List<string> objectives;
    public List<string> prerequisites;
    public string createdAt;
    public string updatedAt;
    public Dictionary<string, Dictionary<string, object>> unity; // SIMPLIFIÉ : directement les objets
}

// Classe pour la réponse API
[System.Serializable]
public class ApiResponse
{
    public bool success;
    public FormationMetadataComplete data;
    public string error;
}