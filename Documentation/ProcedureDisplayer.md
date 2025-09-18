# ProcedureDisplayer UI

## Description
Guide l'utilisateur à travers une séquence d'interactions avec des objets 3D dans un ordre spécifique, avec surbrillance visuelle et validation automatique par clic.

## Structure de données attendue

### Mode Hybride (Recommandé) 🎯
Combine la flexibilité du drag & drop Unity avec la richesse des textes depuis les métadatas.

**Dans Unity Inspector:**
1. Activer "Use Drag & Drop Sequence"
2. Définir la clé de procédure (ex: `procedure_startup`)
3. Glisser les GameObjects dans l'ordre voulu

**Dans les métadatas JSON:**
```json
{
  "procedure_startup": {
    "title": {
      "en": "Procedure Title",
      "fr": "Titre de la procédure"
    },
    "description": {
      "en": "What this procedure accomplishes",
      "fr": "Ce que cette procédure accomplit"
    },
    "step_1": {
      "instruction": {
        "en": "Detailed instruction for step 1",
        "fr": "Instruction détaillée pour l'étape 1"
      },
      "validation": {
        "en": "Confirmation message when completed",
        "fr": "Message de confirmation une fois terminé"
      },
      "hint": {
        "en": "Help text if user is stuck",
        "fr": "Texte d'aide si l'utilisateur est bloqué"
      }
    },
    "step_2": { ... },
    "step_3": { ... }
  }
}
```

**Note:** L'`objectId` n'est PAS nécessaire en mode hybride - les GameObjects sont définis via l'Inspector.

### Mode Metadata Complet
```json
{
  "procedure_maintenance": {
    "title": { ... },
    "description": { ... },
    "step_1": {
      "objectId": "red_cube",  // Requis en mode metadata
      "instruction": { ... },
      "validation": { ... },
      "hint": { ... }
    }
  }
}
```

## Fonctionnalités
- **Surbrillance visuelle** : Jaune avec pulsation sur l'objet actif
- **UI moderne** : Interface verticale élégante sur le côté droit
- **Validation intuitive** : Clic direct sur l'objet surligné
- **Barre de progression** : Suivi visuel de l'avancement
- **Support multilingue** : Français/Anglais automatique
- **Feedback interactif** : Scale et émission au survol
- **Reset automatique** : Recommence si clic hors séquence

## Utilisation dans Unity

### Configuration rapide
1. Sur l'objet déclencheur (ex: yellow_capsule):
   - Ajouter `InteractableObject` component
   - Définir `Content Type = Procedure`
   - Activer `Use Drag & Drop Sequence`
   - Définir `Procedure Key = procedure_startup`
   - Glisser les GameObjects dans `Sequence Objects`

2. Sur chaque objet de la séquence:
   - Ajouter `ObjectMetadataMapper` component
   - Définir un `Metadata Id` unique

3. Dans le JSON metadata:
   - Créer la structure avec les textes pour chaque étape
   - Les étapes correspondent à l'ordre des GameObjects

## Exemple concret

**Inspector Unity:**
```
Yellow Capsule
├── InteractableObject
│   ├── Content Type: Procedure
│   ├── Use Drag & Drop: ✓
│   ├── Procedure Key: "procedure_startup"
│   └── Sequence Objects:
│       [0] Red Cube (GameObject)
│       [1] Blue Sphere (GameObject)
│       [2] Green Cylinder (GameObject)
│       [3] Yellow Capsule (GameObject)
```

**Métadatas JSON:**
```json
{
  "yellow_capsule": {
    "procedure_startup": {
      "title": {
        "en": "🚀 Quantum Reactor Startup",
        "fr": "🚀 Démarrage du réacteur quantique"
      },
      "step_1": {
        "instruction": {
          "en": "Activate the power core",
          "fr": "Activez le cœur d'énergie"
        }
      },
      "step_2": { ... },
      "step_3": { ... },
      "step_4": { ... }
    }
  }
}
```

## Comportement
- **Clic sur objet surligné** → Valide et passe à l'étape suivante
- **Clic ailleurs** → Reset complet de la procédure
- **Fin de procédure** → Incrémente le compteur de progression
- **Feedback visuel** → Flash blanc lors de la validation