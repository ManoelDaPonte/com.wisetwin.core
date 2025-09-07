# WiseTwin Core Package

Un package Unity réutilisable contenant les utilitaires et composants core pour les projets WiseTwin.

## Installation

### Via Package Manager avec Git URL

1. Ouvrez Unity Package Manager (`Window` > `Package Manager`)
2. Cliquez sur `+` et sélectionnez `Add package from git URL...`
3. Entrez l'URL : `https://github.com/[username]/wisetwin-package.git?path=/Packages/com.wisetwin.core`

### Via manifest.json

Ajoutez cette ligne dans `Packages/manifest.json` :

```json
{
  "dependencies": {
    "com.wisetwin.core": "https://github.com/[username]/wisetwin-package.git?path=/Packages/com.wisetwin.core"
  }
}
```

## Composants

### TrainingCompletionNotifier

Composant pour notifier la fin d'une formation à une application React parente.

**Usage :**
```csharp
using WiseTwin;

var notifier = GetComponent<TrainingCompletionNotifier>();
notifier.FormationCompleted("Mon Training");
```

### TestCompletionKey

Composant de test permettant de déclencher la notification de fin via une touche clavier.

**Fonctionnalités :**
- Test de notification par touche (défaut: Y)
- Recherche automatique du TrainingCompletionNotifier
- Interface GUI de debug

### Metadata System

Système complet de gestion des métadonnées d'objets Unity avec chargement depuis JSON externe.

**Composants :**
- `MetadataManager` : Gestionnaire principal des métadonnées
- `MetadataLoader` : Chargement des données JSON
- `MetadataObjectBinder` : Association objets/métadonnées
- `MetadataClasses` : Classes de données

## Compatibilité

- Unity 2021.3+
- Support WebGL pour les notifications JavaScript
- Newtonsoft.Json package requis (installé automatiquement)

## Dépendances

- `com.unity.nuget.newtonsoft-json`: 3.2.1+

## Note technique

Les scripts Metadata utilisent `UnityEngine.Networking` (legacy) pour les requêtes HTTP. Pour les nouveaux projets, considérez migrer vers `UnityWebRequest` moderne.

## Contribution

Ce package est maintenu pour les projets WiseTwin internes.