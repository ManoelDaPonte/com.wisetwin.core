# WiseTwin Analytics System

## 🎯 Vue d'ensemble

Le système d'analytics WiseTwin collecte des métriques détaillées pendant les sessions de formation et génère un format JSON optimisé pour React.

**⚠️ IMPORTANT** : Les données utilisent des **CLÉS uniquement** (pas de texte brut) pour :
- ✅ Supporter le multilangue sans duplication de données
- ✅ Permettre l'agrégation des statistiques indépendamment de la langue
- ✅ Réduire la taille des données envoyées
- ✅ Faciliter la jointure avec les métadonnées

## 📊 Principe de fonctionnement

### Architecture des données

```
Unity envoie :                     React fait la jointure :
{
  questionKey: "question_2",   →   metadata.unity[objectId].question_2.text[lang]
  objectId: "red_cube",        →   metadata.unity["red_cube"].question_2
  correctAnswers: [1, 3],
  userAnswers: [[1, 3]]
}
```

**Le texte complet est récupéré via les métadonnées**, pas dans les analytics.

---

## 📦 Structure des données

### Format JSON principal

```json
{
  "sessionId": "abc-123-def-456",
  "trainingId": "SampleScene1",
  "startTime": "2025-01-20T10:00:00Z",
  "endTime": "2025-01-20T10:15:30Z",
  "totalDuration": 930,
  "completionStatus": "completed",
  "interactions": [...],
  "summary": {
    "totalInteractions": 15,
    "successfulInteractions": 12,
    "failedInteractions": 3,
    "averageTimePerInteraction": 62,
    "totalAttempts": 18,
    "totalFailedAttempts": 3,
    "successRate": 80,
    "score": 85.5
  }
}
```

---

## 🔍 Types d'interactions

### 1. Questions (QCM / Choix unique ou multiple)

#### Format envoyé par Unity

```json
{
  "interactionId": "red_cube_question_2_1737368400000",
  "type": "question",
  "subtype": "multiple_choice",
  "objectId": "red_cube",
  "startTime": "2025-01-20T10:00:00Z",
  "endTime": "2025-01-20T10:02:15Z",
  "duration": 135,
  "attempts": 2,
  "success": true,
  "data": {
    "questionKey": "question_2",
    "objectId": "red_cube",
    "correctAnswers": [1, 3],
    "userAnswers": [[0, 1], [1, 3]],
    "firstAttemptCorrect": false,
    "finalScore": 100
  }
}
```

#### Jointure avec les métadonnées

```typescript
// Récupérer le texte de la question
const questionMeta = metadata.unity[interaction.data.objectId][interaction.data.questionKey];
const questionText = questionMeta.text[currentLanguage]; // "Combien de faces a un cube ?"
const options = questionMeta.options[currentLanguage];   // ["4 faces", "6 faces", "8 faces"]

// Exemple d'affichage
console.log(`Question: ${questionText}`);
console.log(`Options: ${options.join(', ')}`);
console.log(`Correct answers: ${interaction.data.correctAnswers.map(i => options[i]).join(', ')}`);
console.log(`User tried: ${interaction.data.userAnswers.length} times`);
console.log(`First attempt correct: ${interaction.data.firstAttemptCorrect}`);
```

#### Champs garantis

| Champ | Type | Description |
|-------|------|-------------|
| `questionKey` | string | Clé de la question (ex: "question_2") |
| `objectId` | string | ID de l'objet Unity |
| `correctAnswers` | number[] | Indices des réponses correctes |
| `userAnswers` | number[][] | Historique des tentatives (tableau de tableaux) |
| `firstAttemptCorrect` | boolean | true si réussi du premier coup |
| `finalScore` | number | 100 si `firstAttemptCorrect`, sinon 0 |

---

### 2. Procédures (Séquences d'actions)

#### Format envoyé par Unity

```json
{
  "interactionId": "yellow_capsule_procedure_startup_1737368500000",
  "type": "procedure",
  "subtype": "sequential",
  "objectId": "yellow_capsule",
  "startTime": "2025-01-20T10:05:00Z",
  "endTime": "2025-01-20T10:08:30Z",
  "duration": 210,
  "attempts": 1,
  "success": true,
  "data": {
    "procedureKey": "procedure_startup",
    "objectId": "yellow_capsule",
    "totalSteps": 4,
    "totalWrongClicks": 3,
    "totalDuration": 210,
    "perfectCompletion": false,
    "finalScore": 0,
    "steps": [
      {
        "stepNumber": 1,
        "stepKey": "step_1",
        "targetObjectId": "red_cube",
        "completed": true,
        "duration": 45.2,
        "wrongClicksOnThisStep": 2
      },
      {
        "stepNumber": 2,
        "stepKey": "step_2",
        "targetObjectId": "blue_sphere",
        "completed": true,
        "duration": 38.7,
        "wrongClicksOnThisStep": 0
      },
      {
        "stepNumber": 3,
        "stepKey": "step_3",
        "targetObjectId": "green_cylinder",
        "completed": true,
        "duration": 52.1,
        "wrongClicksOnThisStep": 1
      },
      {
        "stepNumber": 4,
        "stepKey": "step_4",
        "targetObjectId": "orange_cone",
        "completed": true,
        "duration": 74.0,
        "wrongClicksOnThisStep": 0
      }
    ]
  }
}
```

#### Jointure avec les métadonnées

```typescript
// Récupérer les infos de la procédure
const procedureMeta = metadata.unity[interaction.objectId][interaction.data.procedureKey];
const procedureTitle = procedureMeta.title[currentLanguage]; // "Quantum Reactor Startup Sequence"
const procedureDescription = procedureMeta.description[currentLanguage];

console.log(`Procedure: ${procedureTitle}`);
console.log(`Total steps: ${interaction.data.totalSteps}`);
console.log(`Total errors: ${interaction.data.totalWrongClicks}`);
console.log(`Perfect completion: ${interaction.data.perfectCompletion}`);

// Itérer sur les étapes
interaction.data.steps.forEach(step => {
  const stepMeta = procedureMeta[step.stepKey];
  const stepInstruction = stepMeta.instruction[currentLanguage];

  console.log(`Step ${step.stepNumber}: ${stepInstruction}`);
  console.log(`  - Duration: ${step.duration}s`);
  console.log(`  - Errors: ${step.wrongClicksOnThisStep}`);
  console.log(`  - Target: ${step.targetObjectId}`);
});
```

#### Champs garantis

**Procédure globale :**

| Champ | Type | Description |
|-------|------|-------------|
| `procedureKey` | string | Clé de la procédure (ex: "procedure_startup") |
| `objectId` | string | ID de l'objet Unity |
| `totalSteps` | number | Nombre total d'étapes |
| `totalWrongClicks` | number | Erreurs sur toute la procédure |
| `totalDuration` | number | Durée totale en secondes |
| `perfectCompletion` | boolean | true si aucune erreur |
| `finalScore` | number | 100 si `perfectCompletion`, sinon 0 |
| `steps` | ProcedureStep[] | Toutes les étapes |

**Chaque étape (`ProcedureStep`) :**

| Champ | Type | Description |
|-------|------|-------------|
| `stepNumber` | number | Numéro de l'étape (1, 2, 3...) |
| `stepKey` | string | Clé de l'étape (ex: "step_1") |
| `targetObjectId` | string | ID de l'objet à cliquer |
| `completed` | boolean | true si complétée |
| `duration` | number | Durée de l'étape en secondes |
| `wrongClicksOnThisStep` | number | Erreurs sur cette étape uniquement |

---

### 3. Texte informatif

#### Format envoyé par Unity

```json
{
  "interactionId": "green_cylinder_text_content_1737368600000",
  "type": "text",
  "subtype": "informative",
  "objectId": "green_cylinder",
  "startTime": "2025-01-20T10:10:00Z",
  "endTime": "2025-01-20T10:10:45Z",
  "duration": 45,
  "attempts": 1,
  "success": true,
  "data": {
    "contentKey": "text_content",
    "objectId": "green_cylinder",
    "timeDisplayed": 45.2,
    "readComplete": true,
    "scrollPercentage": 100,
    "finalScore": 100
  }
}
```

#### Jointure avec les métadonnées

```typescript
// Récupérer le contenu texte
const textMeta = metadata.unity[interaction.data.objectId][interaction.data.contentKey];
const title = textMeta.title[currentLanguage];
const content = textMeta.content[currentLanguage];

console.log(`Title: ${title}`);
console.log(`Content: ${content.substring(0, 100)}...`);
console.log(`Time displayed: ${interaction.data.timeDisplayed}s`);
console.log(`Read complete: ${interaction.data.readComplete}`);
console.log(`Scrolled: ${interaction.data.scrollPercentage}%`);
```

#### Champs garantis

| Champ | Type | Description |
|-------|------|-------------|
| `contentKey` | string | Clé du contenu (ex: "text_content") |
| `objectId` | string | ID de l'objet Unity |
| `timeDisplayed` | number | Durée d'affichage en secondes |
| `readComplete` | boolean | true si lu complètement |
| `scrollPercentage` | number | Pourcentage de scroll (0-100) |
| `finalScore` | number | Toujours 100 pour les textes |

---

## 🔗 Intégration côté React

### Réception des données

```typescript
// Interface TypeScript
interface TrainingAnalytics {
  sessionId: string;
  trainingId: string;
  startTime: string;
  endTime: string;
  totalDuration: number;
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
  data: any; // Voir détails par type ci-dessus
}

// Fonction à définir dans votre application React
window.ReceiveTrainingAnalytics = (analyticsData: TrainingAnalytics, metadata: any) => {
  console.log('Training analytics received:', analyticsData);

  // Traiter les données avec jointure
  processAnalytics(analyticsData, metadata);

  // Envoyer à votre API backend
  fetch('/api/training/analytics', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(analyticsData)
  });
};
```

### Exemple de traitement complet

```typescript
function processAnalytics(analytics: TrainingAnalytics, metadata: any) {
  const currentLanguage = 'fr'; // ou 'en'

  analytics.interactions.forEach(interaction => {
    switch (interaction.type) {
      case 'question':
        const questionMeta = metadata.unity[interaction.data.objectId][interaction.data.questionKey];
        const questionText = questionMeta.text[currentLanguage];
        const options = questionMeta.options[currentLanguage];

        console.log({
          question: questionText,
          options: options,
          correctAnswers: interaction.data.correctAnswers.map(i => options[i]),
          userAttempts: interaction.data.userAnswers.length,
          success: interaction.data.firstAttemptCorrect
        });
        break;

      case 'procedure':
        const procedureMeta = metadata.unity[interaction.objectId][interaction.data.procedureKey];
        const procedureTitle = procedureMeta.title[currentLanguage];

        console.log({
          procedure: procedureTitle,
          steps: interaction.data.steps.map(step => ({
            instruction: procedureMeta[step.stepKey].instruction[currentLanguage],
            duration: step.duration,
            errors: step.wrongClicksOnThisStep
          })),
          perfectCompletion: interaction.data.perfectCompletion
        });
        break;

      case 'text':
        const textMeta = metadata.unity[interaction.data.objectId][interaction.data.contentKey];
        const title = textMeta.title[currentLanguage];

        console.log({
          title: title,
          timeDisplayed: interaction.data.timeDisplayed,
          readComplete: interaction.data.readComplete
        });
        break;
    }
  });
}
```

---

## 📈 Exemples d'analyses

### Questions les plus difficiles

```typescript
const difficultQuestions = analytics.interactions
  .filter(i => i.type === 'question' && !i.data.firstAttemptCorrect)
  .map(i => {
    const meta = metadata.unity[i.data.objectId][i.data.questionKey];
    return {
      question: meta.text[currentLanguage],
      attempts: i.data.userAnswers.length,
      duration: i.duration
    };
  })
  .sort((a, b) => b.attempts - a.attempts);

console.log('Top 5 difficult questions:', difficultQuestions.slice(0, 5));
```

### Temps moyen par type d'interaction

```typescript
const avgTimes = {
  questions: 0,
  procedures: 0,
  texts: 0
};

['question', 'procedure', 'text'].forEach(type => {
  const interactions = analytics.interactions.filter(i => i.type === type);
  const totalTime = interactions.reduce((sum, i) => sum + i.duration, 0);
  avgTimes[`${type}s`] = totalTime / interactions.length;
});

console.log('Average times:', avgTimes);
```

### Taux de succès par procédure

```typescript
const procedureStats = analytics.interactions
  .filter(i => i.type === 'procedure')
  .map(i => {
    const meta = metadata.unity[i.objectId][i.data.procedureKey];
    return {
      name: meta.title[currentLanguage],
      perfectCompletion: i.data.perfectCompletion,
      totalErrors: i.data.totalWrongClicks,
      duration: i.data.totalDuration,
      steps: i.data.steps.length
    };
  });

const perfectCompletionRate = procedureStats.filter(p => p.perfectCompletion).length / procedureStats.length * 100;
console.log(`Perfect completion rate: ${perfectCompletionRate}%`);
```

### Statistiques de lecture

```typescript
const readingStats = analytics.interactions
  .filter(i => i.type === 'text')
  .map(i => ({
    timeDisplayed: i.data.timeDisplayed,
    readComplete: i.data.readComplete,
    scrollPercentage: i.data.scrollPercentage
  }));

const avgReadTime = readingStats.reduce((sum, r) => sum + r.timeDisplayed, 0) / readingStats.length;
const completeReadRate = readingStats.filter(r => r.readComplete).length / readingStats.length * 100;

console.log(`Average reading time: ${avgReadTime}s`);
console.log(`Complete read rate: ${completeReadRate}%`);
```

---

## ✅ Avantages du système avec clés

### 1. Multilangue cohérent
```typescript
// Même utilisateur, langues différentes
const user1_fr = { questionKey: "question_2", userAnswers: [[1, 3]] };
const user1_en = { questionKey: "question_2", userAnswers: [[1, 3]] };

// Les deux peuvent être agrégés car même questionKey
// Pas de duplication de "Combien de faces a un cube ?" vs "How many faces does a cube have?"
```

### 2. Taille des données réduite
```typescript
// Ancien système (avec texte)
{
  questionText: "Combien de faces a un cube ? Sélectionnez toutes les bonnes réponses.",
  options: ["4 faces", "6 faces", "8 faces", "12 faces"],
  // ~120 caractères
}

// Nouveau système (avec clés)
{
  questionKey: "question_2",
  objectId: "red_cube"
  // ~30 caractères = 75% de réduction
}
```

### 3. Agrégation facile
```typescript
// Compter combien d'utilisateurs ont répondu correctement à question_2
const successRate = analytics
  .filter(a => a.interactions.some(i =>
    i.data.questionKey === "question_2" && i.data.firstAttemptCorrect
  )).length / totalUsers;

// Fonctionne indépendamment de la langue !
```

---

## 🔒 Garanties du système

### Données toujours présentes

Chaque interaction contient **TOUJOURS** :
- ✅ `interactionId` : Identifiant unique
- ✅ `type` : "question" | "procedure" | "text"
- ✅ `subtype` : Sous-type spécifique
- ✅ `objectId` : ID de l'objet Unity
- ✅ `startTime` / `endTime` : Timestamps ISO 8601
- ✅ `duration` : Durée en secondes
- ✅ `attempts` : Nombre de tentatives
- ✅ `success` : Boolean de réussite
- ✅ `data.finalScore` : Score final (0-100)

### Timestamps

- Format : ISO 8601 UTC (ex: "2025-01-20T10:15:30Z")
- Timezone : Toujours UTC
- Précision : Seconde

### IDs uniques

- `sessionId` : UUID unique par session
- `interactionId` : `{objectId}_{contentKey}_{timestamp}`

---

## 📝 Notes importantes

1. **Pas de texte dans les analytics** : Tout le texte est dans les métadonnées
2. **Jointure obligatoire** : Utilisez `metadata.unity[objectId][contentKey]` pour récupérer le texte
3. **Procédures groupées** : Une procédure = 1 interaction (pas N interactions)
4. **Score binaire** : 100 si parfait, 0 sinon (pour questions et procédures)
5. **Compatibilité** : Unity 2021.3+ / Unity 6000+

---

## 🐛 Debugging

### Activer les logs détaillés

```csharp
TrainingAnalytics.Instance.SetDebugMode(true);
```

### Exporter manuellement

```csharp
var json = TrainingAnalytics.Instance.ExportAnalytics();
Debug.Log(json);
```

### Vérifier la structure

```typescript
console.log('Session ID:', analytics.sessionId);
console.log('Total interactions:', analytics.interactions.length);
console.log('Score:', analytics.summary.score);
```

---

## 📞 Support

Pour toute question sur l'intégration côté React :
- Vérifiez que vous avez accès aux métadonnées (`metadata.unity`)
- Assurez-vous que les clés (`questionKey`, `procedureKey`, `contentKey`) existent dans les métadonnées
- Testez la jointure avec un seul exemple avant de traiter toutes les données
