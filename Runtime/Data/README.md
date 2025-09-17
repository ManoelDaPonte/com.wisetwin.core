# WiseTwin Data Module

## 📋 Description
Module de données définissant toutes les structures et classes de données utilisées dans WiseTwin.

## 🔧 Classes

### MetadataClasses.cs
Définit les structures de données pour les métadonnées de formation.

**Classes principales:**
```csharp
// Structure de métadonnées de formation
[Serializable]
public class TrainingMetadata
{
    public string id;
    public string title;
    public string description;
    public string version;
    public int duration;
    public string difficulty;
    public List<string> tags;
    public Dictionary<string, object> unity;
}

// Contenu de question
[Serializable]
public class QuestionContent
{
    public string text;
    public string type; // "multiple-choice", "true-false", "text-input"
    public List<string> options;
    public int correctAnswer;
    public string feedback;
    public string incorrectFeedback;
}

// Contenu de procédure
[Serializable]
public class ProcedureContent
{
    public string title;
    public List<ProcedureStep> steps;
    public float estimatedTime;
}

// Étape de procédure
[Serializable]
public class ProcedureStep
{
    public int order;
    public string instruction;
    public string validation;
    public string mediaUrl;
}
```

### ContentTypes.cs (à créer)
Types de contenu supportés pour les formations.

**Énumérations:**
```csharp
public enum ContentType
{
    Question,
    Procedure,
    Media,
    Dialogue,
    Custom
}

public enum QuestionType
{
    MultipleChoice,
    TrueFalse,
    TextInput,
    DragAndDrop
}

public enum MediaType
{
    Image,
    Video,
    Audio,
    Model3D
}
```

## 📊 Format JSON
Exemple de structure de métadonnées:
```json
{
    "id": "formation-001",
    "title": "Formation Sécurité",
    "unity": {
        "extincteur": {
            "procedure_1": {
                "type": "procedure",
                "title": "Utilisation extincteur",
                "steps": [...]
            }
        }
    }
}
```

## 🔄 Sérialisation
Utilise Newtonsoft.Json pour une sérialisation/désérialisation flexible des données complexes.