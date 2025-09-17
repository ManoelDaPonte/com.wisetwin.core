# WiseTwin UI Module

## 📋 Description
Module gérant l'interface utilisateur et les interactions visuelles pour les formations WiseTwin.

## 🎨 Architecture UI
Utilise le nouveau **UI Toolkit** (Unity 6) pour des interfaces modernes et performantes.

## 🔧 Controllers

### WiseTwinUIManager.cs
Gestionnaire principal de l'interface utilisateur.

**Utilisation:**
```csharp
using WiseTwin.UI;

public class SceneSetup : MonoBehaviour
{
    void Start()
    {
        var uiManager = WiseTwinUIManager.Instance;

        // Afficher un panel de question
        uiManager.ShowQuestionPanel(questionData);

        // Afficher une procédure
        uiManager.ShowProcedurePanel(procedureData);

        // Masquer tous les panels
        uiManager.HideAllPanels();
    }
}
```

### TrainingController.cs
Contrôle le flux d'une session de formation.

**Utilisation:**
```csharp
var controller = GetComponent<TrainingController>();

// Démarrer la formation
controller.StartTraining("formation-001");

// Écouter les événements
controller.OnQuestionAnswered += (question, answer) => {
    Debug.Log($"Question {question.id} répondue: {answer}");
};

controller.OnTrainingCompleted += (results) => {
    Debug.Log($"Formation terminée! Score: {results.score}");
};
```

### InteractiveController.cs
Gère les interactions avec les objets 3D.

**Utilisation:**
```csharp
// Sur un GameObject interactif
var interactive = gameObject.AddComponent<InteractiveController>();
interactive.objectId = "extincteur_01";
interactive.contentType = ContentType.Procedure;

// Événements d'interaction
interactive.OnInteract += () => {
    Debug.Log("Objet cliqué!");
};

interactive.OnHighlight += (highlighted) => {
    // Changer la couleur ou ajouter un outline
};
```

## 🎯 Components

### QuestionPanel.cs (à créer)
Composant pour afficher les questions.

**Fonctionnalités:**
- QCM avec boutons
- Vrai/Faux
- Saisie de texte
- Timer optionnel
- Feedback immédiat

### ProcedurePanel.cs (à créer)
Composant pour les procédures étape par étape.

**Fonctionnalités:**
- Navigation entre étapes
- Validation de chaque étape
- Barre de progression
- Médias associés (images/vidéos)

### ObjectHighlight.cs (à créer)
Système de surbrillance pour objets interactifs.

**Effets disponibles:**
- Outline shader
- Changement de couleur
- Pulsation
- Particules
- Flèche indicatrice

## 🎨 UI Toolkit (USS Styles)

### WiseTwin.uss
Feuille de style principale:
```css
.wisetwin-panel {
    background-color: rgba(0, 0, 0, 0.8);
    border-radius: 10px;
    padding: 20px;
    margin: 10px;
}

.wisetwin-question-text {
    font-size: 18px;
    color: white;
    margin-bottom: 15px;
    -unity-font-style: bold;
}

.wisetwin-button {
    background-color: #2196F3;
    color: white;
    border-radius: 5px;
    padding: 10px 20px;
    margin: 5px;
    transition: background-color 0.3s;
}

.wisetwin-button:hover {
    background-color: #1976D2;
}

.wisetwin-button:active {
    background-color: #0D47A1;
}

.wisetwin-correct {
    background-color: #4CAF50;
}

.wisetwin-incorrect {
    background-color: #F44336;
}
```

## 📱 Responsive Design
- Adaptation automatique selon la résolution
- Support portrait/paysage
- Scaling pour mobile/tablette/desktop

## 🎮 Input System
Utilise le nouveau **Input System** d'Unity 6:
```csharp
// Configuration des contrôles
- Mouse/Touch: Sélection d'objets
- Keyboard: Navigation dans les menus
- Gamepad: Support optionnel
- VR Controllers: Pour formations VR
```

## 🔄 Animations
Utilise le système d'animation UI Toolkit:
```csharp
// Fade in d'un panel
panel.style.opacity = 0;
panel.experimental.animation.Start(
    new StyleValues { opacity = 1 },
    500 // durée en ms
);

// Slide depuis le bas
panel.transform.position = new Vector3(0, -100, 0);
panel.experimental.animation.Start(
    new StyleValues {
        transformOrigin = TransformOrigin.Center,
        translate = new Translate(0, 0)
    },
    300
);
```

## 🌐 Localisation
Support multi-langues intégré:
```csharp
// Changer la langue
UIManager.SetLanguage("fr");

// Textes localisés
var welcomeText = Localization.Get("ui.welcome");
```