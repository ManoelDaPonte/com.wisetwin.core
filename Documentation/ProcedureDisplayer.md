# ProcedureDisplayer UI

## Description
Guide l'utilisateur à travers une séquence d'interactions avec des objets 3D dans un ordre spécifique.

## Structure de données attendue

### Mode Metadata
```json
{
  "procedure_maintenance": {
    "title": {
      "en": "Procedure Title",
      "fr": "Titre de la procédure"
    },
    "description": {
      "en": "Procedure description",
      "fr": "Description de la procédure"
    },
    "step_1": {
      "objectId": "red_cube",  // ID metadata de l'objet à interagir
      "instruction": {
        "en": "Click on the red cube",
        "fr": "Cliquez sur le cube rouge"
      },
      "validation": {
        "en": "Cube has been selected",
        "fr": "Le cube a été sélectionné"
      },
      "hint": {
        "en": "Look for the red object",
        "fr": "Cherchez l'objet rouge"
      }
    },
    "step_2": { ... },
    "step_3": { ... }
  }
}
```

### Mode Drag & Drop (Inspector Unity)
1. Activer "Use Drag & Drop Sequence" dans l'Inspector
2. Définir le titre et la description directement
3. Glisser les GameObjects dans l'ordre souhaité
4. Les instructions seront génériques ou définies dans metadata

## Fonctionnalités
- Surbrillance jaune avec pulsation sur l'objet actif
- UI verticale sur le côté droit de l'écran
- Clic direct sur l'objet surligné pour valider
- Boutons "Valider" et "Passer" comme alternatives
- Barre de progression
- Support multilingue
- Feedback visuel au survol (scale + émission)

## Utilisation dans Unity
1. Configurer `ContentType.Procedure` sur l'InteractableObject
2. Choisir entre:
   - **Mode Metadata**: Définir les étapes dans le JSON
   - **Mode Drag & Drop**: Utiliser l'Inspector pour la séquence
3. Les objets sont surlignés automatiquement dans l'ordre
4. L'utilisateur peut cliquer sur l'objet ou utiliser les boutons
5. Événement `OnCompleted` déclenché après la dernière étape

## Interaction
- **Clic sur objet**: Valide automatiquement l'étape
- **Bouton Valider**: Alternative au clic sur objet
- **Bouton Passer**: Saute l'étape actuelle
- **Feedback hover**: L'objet grossit légèrement au survol