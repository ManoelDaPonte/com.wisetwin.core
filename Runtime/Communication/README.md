# WiseTwin Communication Module

## 📋 Description
Module gérant toutes les communications entre Unity et les applications web externes (React/Next.js).

## 🔧 Scripts

### TrainingCompletionNotifier.cs
Notifie l'application web parent quand une formation est terminée.

**Utilisation:**
```csharp
using WiseTwin.Communication;

public class MyTrainingController : MonoBehaviour
{
    private TrainingCompletionNotifier notifier;

    void Start()
    {
        notifier = GetComponent<TrainingCompletionNotifier>();
    }

    public void OnTrainingComplete()
    {
        // Envoyer la notification avec les données
        var trainingData = new TrainingResultData
        {
            completionTime = Time.time,
            score = 85,
            answers = GetUserAnswers()
        };

        notifier.FormationCompleted("Formation_Securite", trainingData);
    }
}
```

### WebBridge.cs (à créer)
Pont de communication bidirectionnel Unity ↔ Web.

**Fonctionnalités prévues:**
```csharp
// Envoyer des données vers le web
WebBridge.SendToWeb("eventName", jsonData);

// Recevoir des données du web
WebBridge.OnWebMessage += (eventName, data) => {
    switch(eventName)
    {
        case "startTraining":
            StartTraining(data);
            break;
        case "updateSettings":
            UpdateSettings(data);
            break;
    }
};
```

## 🌐 Communication WebGL

### JavaScript Interop
Pour WebGL, utilise les fonctions JavaScript externes:
```javascript
// Dans index.html ou fichier JS externe
window.NotifyFormationCompleted = function(trainingName, jsonData) {
    // Envoyer à l'application React/Next.js
    window.parent.postMessage({
        type: 'training-completed',
        training: trainingName,
        data: JSON.parse(jsonData)
    }, '*');
};
```

### Mode Éditeur
En mode éditeur, les notifications sont loggées dans la console Unity pour le débogage.

## 📡 API REST (Production)
Communication avec l'API Azure/Next.js:
```csharp
// Configuration dans WiseTwinManager
apiBaseUrl: "https://votre-api.com/api/unity"
containerId: "votre-container-id"

// Endpoints disponibles
POST /api/unity/metadata - Récupérer les métadonnées
POST /api/unity/completion - Notifier la complétion
POST /api/unity/progress - Envoyer la progression
GET /api/unity/user - Récupérer les infos utilisateur
```

## 🔒 Sécurité
- Validation des données entrantes
- Sanitization des strings avant envoi
- Authentification par token (si configuré)
- CORS configuré pour domaines autorisés

## 📊 Format des Données

### Données de Complétion
```json
{
    "trainingId": "formation-001",
    "userId": "user-123",
    "completedAt": "2024-01-15T10:30:00Z",
    "duration": 1800,
    "score": 85,
    "results": {
        "questions": [
            {
                "id": "q1",
                "answer": "A",
                "correct": true,
                "timeSpent": 45
            }
        ],
        "procedures": [
            {
                "id": "p1",
                "completed": true,
                "steps": 5
            }
        ]
    }
}
```