# WiseTwin Core Module

## 📋 Description
Module principal gérant l'architecture singleton et la coordination entre tous les composants WiseTwin.

## 🔧 Scripts

### WiseTwinManager.cs
Gestionnaire principal du système WiseTwin. Coordonne tous les sous-systèmes.

**Utilisation:**
```csharp
// Accès au singleton
var manager = WiseTwin.Core.WiseTwinManager.Instance;

// Récupérer des données pour un objet
var data = manager.GetDataForObject("cube_rouge");

// Écouter les événements
manager.OnMetadataReady += (metadata) => {
    Debug.Log("Metadata chargées!");
};
```

### MetadataLoader.cs
Charge les métadonnées depuis des sources locales (JSON) ou distantes (API Azure).

**Utilisation:**
```csharp
// Le MetadataLoader est géré automatiquement par WiseTwinManager
// Accès direct si nécessaire
var loader = WiseTwin.Core.MetadataLoader.Instance;

// Forcer un rechargement
loader.ReloadMetadata();

// Récupérer du contenu typé
var question = loader.GetContentForObject<QuestionContent>("objet_id", "question_1");
```

## 🏗️ Architecture
- **Pattern Singleton** : Assure une instance unique
- **Event-driven** : Communication par événements
- **Mode Local/Production** : Switch automatique selon configuration

## 📦 Dépendances
- WiseTwin.Data
- Unity.TextMeshPro (optionnel)
- Newtonsoft.Json (pour le parsing JSON)