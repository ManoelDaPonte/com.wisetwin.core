# Analytics vs Content Types - Guide de clarification

## Vue d'ensemble

Le système WiseTwin utilise deux ensembles de structures de données distinctes :

1. **Content Types** (`ContentTypes.cs`) - Pour la définition et le stockage du contenu
2. **Analytics Data** (`TrainingAnalyticsData.cs`) - Pour le tracking et l'envoi des métriques

## Content Types (ContentTypes.cs)

**Usage** : Définition du contenu de formation, stockage dans les métadonnées

### Classes principales :
- `QuestionContent` : Structure d'une question (texte, options, réponses correctes)
- `ProcedureContent` : Définition d'une procédure avec ses étapes
- `DialogueContent` : Contenu narratif/dialogue
- `TrainingResultData` : ANCIENNE structure de résultats (remplacée par Analytics)

### Quand utiliser :
- Pour créer/éditer du contenu dans l'éditeur Unity
- Pour charger les métadonnées depuis JSON
- Pour définir la structure des formations

## Analytics Data (TrainingAnalyticsData.cs)

**Usage** : Tracking en temps réel et envoi des métriques à React

### Classes principales :
- `TrainingAnalyticsData` : Structure complète des analytics
- `InteractionRecord` : Enregistrement d'une interaction
- `QuestionAnalyticsData` : Métriques détaillées des questions
- `ProcedureAnalyticsData` : Métriques des procédures
- `TextAnalyticsData` : Métriques de lecture

### Quand utiliser :
- Pour tracker les interactions utilisateur
- Pour envoyer des données à React
- Pour analyser les performances

## Différences clés

| Aspect | Content Types | Analytics Data |
|--------|--------------|-----------------|
| **But** | Définir le contenu | Tracker l'usage |
| **Moment** | Création/Édition | Runtime/Session |
| **Données** | Structure statique | Métriques dynamiques |
| **Stockage** | JSON métadonnées | Mémoire puis React |
| **Exemples** | Questions, étapes | Temps, tentatives, scores |

## Flow de données

```
1. CRÉATION (Editor)
   └─> ContentTypes.cs (QuestionContent, ProcedureContent)
       └─> Sauvegarde dans metadata.json

2. RUNTIME (Jeu)
   └─> Chargement depuis metadata.json
       └─> Affichage via UI Displayers
           └─> Tracking avec TrainingAnalytics
               └─> Génération de TrainingAnalyticsData

3. ENVOI (Complétion)
   └─> TrainingAnalyticsData
       └─> JSON sérialisé
           └─> SendTrainingAnalytics (WebGL)
               └─> React reçoit les données
```

## Classes obsolètes

⚠️ **À ne plus utiliser** :
- `TrainingResultData` : Remplacée par `TrainingAnalyticsData`
- `QuestionResult` : Remplacée par `QuestionAnalyticsData`
- `ProcedureResult` : Remplacée par `ProcedureAnalyticsData`

Ces classes restent pour la compatibilité mais ne sont pas utilisées dans le nouveau système.

## Pour React

La structure à utiliser côté React est définie dans `TrainingAnalyticsData.cs` :
- Voir `TypeScriptInterface.InterfaceDefinition` pour les types TypeScript
- Voir `ReactExample.ExampleCode` pour l'utilisation

## Migration

Si vous utilisez encore les anciennes classes :
1. Remplacez `TrainingResultData` par `TrainingAnalyticsData`
2. Utilisez `TrainingAnalytics.Instance` pour le tracking
3. Les données sont automatiquement envoyées à la complétion

## Questions fréquentes

**Q: Dois-je utiliser QuestionContent ou QuestionAnalyticsData ?**
- Pour définir une question : `QuestionContent`
- Pour tracker les réponses : `QuestionAnalyticsData`

**Q: Où sont stockées les analytics ?**
- En mémoire via `TrainingAnalytics` singleton
- Envoyées à React à la fin via WebGL

**Q: Puis-je modifier TrainingResultData ?**
- Non, utilisez le nouveau système `TrainingAnalyticsData`