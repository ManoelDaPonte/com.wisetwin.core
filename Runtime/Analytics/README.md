# WiseTwin Analytics System

## Vue d'ensemble

Le système d'analytics WiseTwin collecte automatiquement des métriques détaillées pendant les sessions de formation, créant un format JSON standardisé pour l'analyse côté React.

## Structure des données

### Format JSON principal
```json
{
  "sessionId": "unique-guid",
  "trainingId": "training-name",
  "startTime": "2025-01-20T10:00:00Z",
  "endTime": "2025-01-20T10:15:30Z",
  "totalDuration": 930,
  "completionStatus": "completed",
  "interactions": [...],
  "summary": {...}
}
```

### Types d'interactions

#### 1. Questions (QCM)
```json
{
  "interactionId": "object_question_1",
  "type": "question",
  "subtype": "multiple_choice",
  "data": {
    "questionText": "...",
    "options": [...],
    "correctAnswers": [0, 2],
    "userAnswers": [[0], [0, 2]],
    "firstAttemptCorrect": false,
    "finalScore": 100
  }
}
```

#### 2. Procédures
```json
{
  "interactionId": "valve_step_1",
  "type": "procedure",
  "subtype": "sequential",
  "data": {
    "stepNumber": 1,
    "totalSteps": 3,
    "instruction": "Turn off the valve",
    "hintsUsed": 0,
    "wrongClicks": 2
  }
}
```

#### 3. Texte informatif
```json
{
  "interactionId": "info_text",
  "type": "text",
  "subtype": "informative",
  "data": {
    "textContent": "Safety instructions...",
    "timeDisplayed": 15.5,
    "readComplete": true,
    "scrollPercentage": 100
  }
}
```

## Métriques collectées

### Niveau Formation
- **Temps total** : Durée complète de la session
- **Taux de complétion** : Pourcentage d'interactions terminées
- **Score global** : Moyenne pondérée des succès
- **Nombre de tentatives** : Total des essais sur toutes les interactions

### Niveau Interaction
- **Temps de début/fin** : Timestamps ISO 8601
- **Durée** : Temps en secondes
- **Tentatives** : Nombre d'essais
- **Succès** : Boolean de réussite
- **Données spécifiques** : Selon le type d'interaction

## Intégration côté React

### Réception des données
```javascript
// Fonction à définir dans votre application React
window.ReceiveTrainingAnalytics = (analyticsData) => {
  console.log('Training analytics received:', analyticsData);

  // Stocker localement
  localStorage.setItem('training_analytics', JSON.stringify(analyticsData));

  // Envoyer à votre API
  fetch('/api/analytics', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(analyticsData)
  });
};
```

### Données garanties

Chaque interaction contient TOUJOURS :
- `interactionId` : Identifiant unique
- `type` : Type d'interaction (question/procedure/text)
- `subtype` : Sous-type spécifique
- `objectId` : ID de l'objet Unity associé
- `startTime` / `endTime` : Timestamps
- `duration` : Durée en secondes
- `attempts` : Nombre de tentatives
- `success` : Réussite ou non
- `data` : Objet avec données spécifiques

## Test du système

### Dans Unity Editor

1. Ajouter le composant `TestAnalytics` à un GameObject
2. Cocher "Run Test On Start" ou utiliser le menu contextuel
3. Vérifier les logs dans la Console Unity

### Format d'export

Les données sont exportées automatiquement lors de :
- Complétion de la formation
- Appel manuel à `TrainingAnalytics.Instance.ExportAnalytics()`

## Exemples d'utilisation

### Analyse des questions difficiles
```javascript
const difficultQuestions = analyticsData.interactions
  .filter(i => i.type === 'question')
  .filter(i => i.data.firstAttemptCorrect === false)
  .map(i => i.data.questionText);
```

### Temps moyen par procédure
```javascript
const procedureTimes = analyticsData.interactions
  .filter(i => i.type === 'procedure')
  .map(i => i.duration);

const avgTime = procedureTimes.reduce((a,b) => a+b, 0) / procedureTimes.length;
```

### Taux de lecture des textes
```javascript
const readingRate = analyticsData.interactions
  .filter(i => i.type === 'text')
  .filter(i => i.data.readComplete === true)
  .length / analyticsData.interactions.filter(i => i.type === 'text').length;
```

## Configuration

Le système est automatique et ne nécessite aucune configuration. Pour désactiver le debug :

```csharp
TrainingAnalytics.Instance.SetDebugMode(false);
```

## Notes importantes

- Les données sont collectées automatiquement sans action supplémentaire
- Le format JSON est garanti stable pour faciliter le parsing
- Toutes les timestamps sont en UTC (format ISO 8601)
- Les IDs sont uniques par session
- Le système est compatible avec Unity 2021.3+