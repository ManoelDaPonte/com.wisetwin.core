# Guide de Test - WiseTwin UI System

## 🎯 Objectif
Ce guide explique comment tester les différents composants UI du package WiseTwin Core.

## 🚀 Test Rapide

### 1. Setup Minimal - Sélection de Langue

1. **Créer un GameObject vide** dans votre scène
   - Nom suggéré : "WiseTwinUITest"

2. **Ajouter le composant `WiseTwinUITestRunner`**
   - Chemin : `Packages/com.wisetwin.core/Tests/Runtime/WiseTwinUITestRunner.cs`

3. **Configuration dans l'inspecteur :**
   - **Test Mode** : `Language Selection`
   - **Auto Start** : ✅
   - **Use Minimal Setup** : ✅
   - **Panel Settings** : Assigner `WiseTwinPanelSettings.asset`

4. **Lancer la scène (Play)**
   - La sélection de langue devrait apparaître automatiquement

## 📋 Modes de Test Disponibles

### Language Selection
- **But** : Tester uniquement l'interface de sélection de langue
- **Setup** : Minimal (juste LanguageSelectionUI)
- **Attendu** :
  - Fond noir avec carte centrale
  - Deux boutons avec drapeaux 🇬🇧 🇫🇷
  - Transition vers le disclaimer après sélection

### Full System
- **But** : Tester l'intégration complète
- **Setup** : Tous les managers (WiseTwinManager, LocalizationManager, UIManager)
- **Attendu** :
  - Sélection de langue
  - Disclaimer
  - Système prêt pour la formation

### UI Toolkit Basic
- **But** : Vérifier que UI Toolkit fonctionne
- **Setup** : Aucun
- **Attendu** :
  - Fond bleu foncé
  - Boîte centrale avec texte "UI Toolkit Works! ✅"

### Question Modal
- **But** : Tester l'affichage des questions
- **Setup** : WiseTwinUIManager
- **Attendu** :
  - Modal de question avec 4 options
  - Boutons interactifs

## 🔧 Configuration Avancée

### Panel Settings
Si le `WiseTwinPanelSettings.asset` n'existe pas :

1. **Créer** : Assets > Create > UI Toolkit > Panel Settings
2. **Configurer** :
   ```
   Scale Mode: Scale With Screen Size
   Reference Resolution: 1920x1080
   Screen Match Mode: 0.5
   Sort Order: 0
   ```
3. **Sauvegarder** dans Assets/

### Debug Options
Dans l'inspecteur de `WiseTwinUITestRunner` :
- **Enable Debug Logs** : Affiche les logs de test
- **Auto Start** : Lance le test automatiquement
- **Use Minimal Setup** : Utilise le minimum de composants

## 🧪 Tests Manuels via Context Menu

Sur le GameObject avec `WiseTwinUITestRunner`, clic droit sur le composant :

- **Force Show Language Selection** : Affiche manuellement la sélection
- **Test Show Question** : Affiche une question de test
- **Clear UI** : Nettoie complètement l'interface

## 🐛 Troubleshooting

### L'UI ne s'affiche pas
1. **Vérifier le Panel Settings** est assigné
2. **Console** : Chercher les erreurs rouges
3. **UI Toolkit Debugger** : Window > UI Toolkit > Debugger

### Barres grises au lieu des boutons
1. **Panel Settings manquant** - Assigner dans l'inspecteur
2. **Conflit UXML** - Le script nettoie automatiquement maintenant

### Texte non visible
1. **Vérifier la résolution** de Game View
2. **Zoom** de la Game View à 1x

### Messages de debug non désirés
1. **Décocher** "Enable Debug Logs" dans l'inspecteur
2. **LanguageSelectionUI** : Décocher "Debug Mode"

## 📝 Exemples de Code

### Test programmatique simple
```csharp
// Créer un test minimal
GameObject testGO = new GameObject("UITest");
UIDocument uiDoc = testGO.AddComponent<UIDocument>();
uiDoc.panelSettings = Resources.Load<PanelSettings>("WiseTwinPanelSettings");
LanguageSelectionUI langUI = testGO.AddComponent<LanguageSelectionUI>();
```

### Forcer l'affichage
```csharp
// Récupérer et afficher manuellement
var langUI = FindObjectOfType<LanguageSelectionUI>();
if (langUI != null)
{
    langUI.ShowLanguageSelection();
}
```

## 📦 Structure des Tests

```
Packages/com.wisetwin.core/Tests/
├── Runtime/
│   └── WiseTwinUITestRunner.cs    # Script de test principal
├── UI_TESTING_GUIDE.md            # Ce guide
└── Editor/                        # Tests unitaires (futur)
```

## ✅ Checklist de Validation

- [ ] La sélection de langue s'affiche
- [ ] Les drapeaux sont visibles
- [ ] Les boutons réagissent au survol
- [ ] La sélection déclenche le disclaimer
- [ ] Le bouton "Commencer" ferme l'UI
- [ ] Pas de messages debug si désactivé
- [ ] L'UI se cache après validation

## 🔄 Réinitialisation

Pour refaire un test propre :
1. Supprimer le GameObject de test
2. Recréer avec les étapes du début
3. S'assurer que le Panel Settings est assigné