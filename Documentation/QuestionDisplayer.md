# QuestionDisplayer UI

## Description
Affiche des questions à choix multiples avec support pour plusieurs questions séquentielles sur un même objet.

## Structure de données attendue

```json
{
  "question_1": {
    "text": {
      "en": "Question text in English",
      "fr": "Texte de la question en français"
    },
    "type": "multiple-choice",
    "options": {
      "en": ["Option 1", "Option 2", "Option 3"],
      "fr": ["Option 1", "Option 2", "Option 3"]
    },
    "correctAnswer": 0,  // Index de la bonne réponse (0-based)
    "feedback": {
      "en": "Success message",
      "fr": "Message de succès"
    },
    "incorrectFeedback": {
      "en": "Try again message",
      "fr": "Message d'erreur"
    }
  },
  "question_2": { ... },
  "question_3": { ... }
}
```

## Fonctionnalités
- Questions multiples séquentielles (question_1, question_2, etc.)
- Support multilingue (en/fr)
- Feedback immédiat pour bonnes/mauvaises réponses
- Possibilité de réessayer en cas d'erreur
- Indicateur de progression
- Navigation entre questions

## Utilisation dans Unity
1. Ajouter `ContentType.Question` sur l'InteractableObject
2. Définir les questions dans le metadata JSON
3. Les questions s'affichent dans l'ordre numérique
4. L'utilisateur peut réessayer jusqu'à trouver la bonne réponse
5. Événement `OnCompleted` déclenché après la dernière question