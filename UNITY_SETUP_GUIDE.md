# 🚀 Guide de Setup Unity - WiseTwin Core

## Étape 0 : Préparation

### Vérifications :
- ✅ Unity 2021.3+ (idéalement Unity 6)
- ✅ Package WiseTwin Core importé
- ✅ Package Newtonsoft.Json installé
- ✅ UI Toolkit disponible

---

## 📋 Étape 1 : Créer le Panel Settings (OBLIGATOIRE)

### Dans Project Window :
1. **Clic droit** dans `Assets/`
2. **Create** → **UI Toolkit** → **Panel Settings Asset**
3. **Nommer** : `WiseTwinPanelSettings`
4. **Laisser les paramètres par défaut**

> ⚠️ Sans PanelSettings, l'UI ne s'affichera pas !

---

## 🎮 Étape 2 : Créer la structure de GameObjects

### Dans la Hierarchy, créer cette structure exacte :

```
📦 === SYSTÈME PRINCIPAL ===
└── WiseTwinCore (GameObject vide)
    Position: (0, 0, 0)

📦 === UI SYSTÈME ===
├── UI_Welcome (GameObject vide)
│   Position: (0, 0, 0)
│
└── UI_Training (GameObject vide)
    Position: (0, 0, 0)

📦 === OBJETS 3D ===
├── TrainingObject_01 (Créer un Cube)
│   Position: (-2, 0, 0)
│   Scale: (1, 1, 1)
│
└── TrainingObject_02 (Créer une Sphere)
    Position: (2, 0, 0)
    Scale: (1, 1, 1)
```

---

## 🔧 Étape 3 : Configurer WiseTwinCore

### Sur le GameObject `WiseTwinCore` :

1. **Ajouter Component** → `WiseTwinManager`
   - Enable Debug Logs: ✅
   - Use Production Mode: ❌

2. **Ajouter Component** → `MetadataLoader`
   - Les paramètres se configurent automatiquement

3. **Ajouter Component** → `TrainingCompletionNotifier`
   - Enable Debug Logs: ✅

4. **Ajouter Component** → `LocalizationManager`

---

## 🎨 Étape 4 : Configurer UI_Welcome (Sélection de langue)

### Sur le GameObject `UI_Welcome` :

1. **Ajouter Component** → `UI Document`
   - Panel Settings: Glisser `WiseTwinPanelSettings`
   - Source Asset: Laisser vide (sera chargé automatiquement)

2. **Ajouter Component** → `LanguageSelectionUI`
   - Show On Start: ❌ (IMPORTANT: doit être false)
   - Animation Duration: 0.3
   - Primary Color: (0.2, 0.4, 0.8)
   - Accent Color: (0.1, 0.8, 0.6)

---

## 📊 Étape 5 : Configurer UI_Training (HUD + Questions)

### Sur le GameObject `UI_Training` :

1. **Ajouter Component** → `UI Document`
   - Panel Settings: Glisser `WiseTwinPanelSettings`
   - Source Asset: Laisser vide

2. **Ajouter Component** → `WiseTwinUIManager`
   - Show Debug Info: ✅
   - Primary Color: (0.2, 0.4, 0.8)
   - Accent Color: (0.1, 0.8, 0.6)

3. **Ajouter Component** → `ContentDisplayManager`
   - Debug Mode: ✅

4. **Désactiver le GameObject** UI_Training (décocher la case en haut)

---

## 🎯 Étape 6 : Configurer les objets 3D interactifs

### Sur `TrainingObject_01` (Cube) :

1. **Vérifier** qu'il a un `Box Collider`
   - Is Trigger: ❌ (IMPORTANT: doit être false)

2. **Ajouter Component** → `ObjectMetadataMapper`
   - Metadata Id: `object_1`
   - Auto Detect Id: ❌
   - Show Name Label: ✅

3. **Ajouter Component** → `InteractableObject`
   - Use Mouse Click: ✅
   - Highlight On Hover: ✅
   - Content Type: Question
   - Debug Mode: ✅

### Sur `TrainingObject_02` (Sphere) :

1. **Vérifier** qu'il a un `Sphere Collider`
   - Is Trigger: ❌

2. **Ajouter Component** → `ObjectMetadataMapper`
   - Metadata Id: `object_2`

3. **Ajouter Component** → `InteractableObject`
   - (mêmes paramètres que le cube)

---

## 📝 Étape 7 : Créer le fichier de métadonnées

### Dans Project Window :

1. **Créer le dossier** `Assets/StreamingAssets/` (s'il n'existe pas)

2. **Créer un fichier** `unity-project-metadata.json` avec ce contenu :

```json
{
  "title": "Formation Test WiseTwin",
  "description": "Formation de démonstration",
  "duration": "15 minutes",
  "difficulty": "Débutant",
  "unity": {
    "object_1": {
      "name": "Cube de formation",
      "question_1": {
        "text": {
          "fr": "Quelle est la forme de cet objet?",
          "en": "What is the shape of this object?"
        },
        "type": "multiple-choice",
        "options": {
          "fr": ["Un cube", "Une sphère", "Un cylindre"],
          "en": ["A cube", "A sphere", "A cylinder"]
        },
        "correctAnswer": 0,
        "feedback": {
          "fr": "Bravo! C'est bien un cube.",
          "en": "Great! It's indeed a cube."
        }
      }
    },
    "object_2": {
      "name": "Sphère de formation",
      "question_1": {
        "text": {
          "fr": "De quelle couleur est cet objet?",
          "en": "What color is this object?"
        },
        "type": "multiple-choice",
        "options": {
          "fr": ["Rouge", "Blanc", "Bleu"],
          "en": ["Red", "White", "Blue"]
        },
        "correctAnswer": 1,
        "feedback": {
          "fr": "Correct! La sphère est blanche.",
          "en": "Correct! The sphere is white."
        }
      }
    }
  }
}
```

---

## 🎮 Étape 8 : Créer le script de contrôle principal

### ⚠️ Pour Unity 6 : Configuration du système d'Input

Unity 6 utilise le nouveau Input System par défaut. Deux options :

**Option 1** : Garder le nouveau Input System (recommandé)
- Le script ci-dessous utilise le nouveau système

**Option 2** : Utiliser l'ancien système
- Aller dans `Edit > Project Settings > Player > Other Settings > Active Input Handling`
- Sélectionner "Both" au lieu de "Input System Package (New)"

### Créer un nouveau script C# :

**Fichier** : `Assets/Scripts/MyTrainingController.cs`

```csharp
using UnityEngine;
using WiseTwin;
using System.Collections;
using UnityEngine.InputSystem; // Pour Unity 6

public class MyTrainingController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiWelcome;
    [SerializeField] private GameObject uiTraining;

    [Header("3D Objects")]
    [SerializeField] private GameObject[] trainingObjects;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private LanguageSelectionUI languageUI;
    private WiseTwinUIManager uiManager;
    private ContentDisplayManager contentDisplayManager;
    private WiseTwinManager wiseTwinManager;

    void Start()
    {
        // Récupérer les références
        languageUI = uiWelcome.GetComponent<LanguageSelectionUI>();
        uiManager = uiTraining.GetComponent<WiseTwinUIManager>();
        contentDisplayManager = ContentDisplayManager.Instance;
        wiseTwinManager = WiseTwinManager.Instance;

        // S'abonner aux événements
        if (languageUI != null)
        {
            languageUI.OnLanguageSelected += OnLanguageSelected;
            languageUI.OnTrainingStarted += OnTrainingStarted;
        }

        // Désactiver les interactions au départ
        SetObjectsInteractable(false);

        // Démarrer la formation après un court délai
        StartCoroutine(StartTrainingSequence());
    }

    IEnumerator StartTrainingSequence()
    {
        // Attendre que tout soit initialisé
        yield return new WaitForSeconds(1f);

        // Afficher la sélection de langue
        Debug.Log("🚀 Affichage de la sélection de langue");
        languageUI.ShowLanguageSelection();
    }

    void OnLanguageSelected(string language)
    {
        Debug.Log($"📝 Langue sélectionnée: {language}");
        wiseTwinManager.SetPreferredLanguage(language);
    }

    void OnTrainingStarted()
    {
        Debug.Log("🎮 Formation démarrée!");

        // Basculer les UI
        StartCoroutine(SwitchToTrainingUI());
    }

    IEnumerator SwitchToTrainingUI()
    {
        // Petit délai pour l'animation
        yield return new WaitForSeconds(0.5f);

        // Désactiver UI Welcome
        uiWelcome.SetActive(false);

        // Activer UI Training
        uiTraining.SetActive(true);

        // Activer les objets interactifs
        SetObjectsInteractable(true);

        // Afficher le HUD
        if (uiManager != null)
        {
            uiManager.StartTraining();
            uiManager.UpdateProgress(0, 2); // 0 sur 2 questions
        }

        // Message de bienvenue
        string lang = wiseTwinManager.GetPreferredLanguage();
        string message = lang == "fr"
            ? "Cliquez sur les objets pour répondre aux questions!"
            : "Click on objects to answer questions!";

        uiManager.ShowNotification(message, NotificationType.Info);
    }

    void SetObjectsInteractable(bool enabled)
    {
        foreach (var obj in trainingObjects)
        {
            if (obj != null)
            {
                var interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.SetInteractionEnabled(enabled);

                    // Debug pour vérifier l'activation
                    if (debugMode)
                    {
                        Debug.Log($"🎯 Objet {obj.name}: interaction {(enabled ? "activée" : "désactivée")}");
                    }
                }
            }
        }
    }

    void Update()
    {
        // Pour Unity 6, utiliser le nouveau Input System
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            SimulateAPISubmission();
        }

        // Si tu préfères l'ancien système, va dans:
        // Edit > Project Settings > Player > Active Input Handling > Both
        // Puis utilise: if (Input.GetKeyDown(KeyCode.T))
    }

    void SimulateAPISubmission()
    {
        Debug.Log("📡 === SIMULATION ENVOI API ===");

        // TODO: Récupérer les résultats depuis ContentDisplayManager
        Debug.Log($"📊 Formation terminée");

        // Déclencher la notification de fin
        if (wiseTwinManager != null)
        {
            wiseTwinManager.CompleteTraining();
        }

        // Notification UI
        uiManager.ShowNotification("📡 Données envoyées à l'API!", NotificationType.Success, 5f);
        uiManager.CompleteTraining();

        Debug.Log("📡 === FIN SIMULATION ===");
    }

    void OnDestroy()
    {
        // Se désabonner des événements
        if (languageUI != null)
        {
            languageUI.OnLanguageSelected -= OnLanguageSelected;
            languageUI.OnTrainingStarted -= OnTrainingStarted;
        }
    }
}
```

---

## 🔗 Étape 9 : Connecter le script

1. **Créer un GameObject vide** : `GameController`
2. **Ajouter le script** `MyTrainingController`
3. **Dans l'Inspector**, assigner :
   - UI Welcome: Glisser `UI_Welcome`
   - UI Training: Glisser `UI_Training`
   - Training Objects: Size = 2
     - Element 0: Glisser `TrainingObject_01`
     - Element 1: Glisser `TrainingObject_02`

---

## ✅ Étape 10 : Tester !

### Ordre de test :

1. **Play** dans Unity
2. **Sélection de langue** devrait apparaître
3. **Choisir une langue** (Français ou English)
4. **Lire le disclaimer** et cliquer "Commencer"
5. **Le HUD** devrait apparaître avec timer et progression
6. **Cliquer sur le Cube** → Question apparaît
7. **Répondre** à la question → Feedback
8. **Cliquer sur la Sphère** → Autre question
9. **Appuyer sur T** → Simule l'envoi API (voir Console)

---

## 🐛 Dépannage

| Problème | Solution |
|----------|----------|
| UI n'apparaît pas | Vérifier PanelSettings assigné sur UI Document |
| Objets non cliquables | Voir section "Objets non cliquables" ci-dessous |
| Questions non trouvées | Vérifier metadata.json dans StreamingAssets |
| Erreur NullReference | Vérifier toutes les références dans GameController |
| Erreur Input.GetKeyDown | Unity 6 : Aller dans Project Settings > Player > Active Input Handling > Both |
| Hover ne fonctionne pas | Vérifier : 1) Renderer sur l'objet, 2) Debug Mode activé dans InteractableObject, 3) Highlight On Hover coché |

### 🖱️ Objets non cliquables - Checklist complète

1. **Vérifier la Camera**
   - Il doit y avoir une Camera avec le tag "MainCamera"
   - La Camera doit voir les objets (dans le Frustum)

2. **Vérifier les Colliders**
   - L'objet DOIT avoir un Collider (Box, Sphere, etc.)
   - Le Collider NE DOIT PAS être en mode Trigger (Is Trigger = ❌)
   - Le Collider doit être activé (enabled)

3. **Vérifier les Layers**
   - L'objet ne doit pas être sur un layer "Ignore Raycast"
   - La Camera doit pouvoir voir le layer de l'objet

4. **Vérifier l'UI**
   - L'UI ne doit pas bloquer les clics (pickingMode = Ignore sur le root)
   - Pas de Canvas en mode "Screen Space - Overlay" qui bloque tout

5. **Vérifier InteractableObject**
   - Le component doit être présent et activé
   - "Use Mouse Click" doit être coché (✅)
   - Après "Commencer", vérifier dans la Console que l'interaction est activée

6. **Script de Debug**
   - Ajouter temporairement le script `ClickDebugger.cs` sur l'objet
   - Lance le jeu et clique sur l'objet
   - Regarde la Console pour voir ce qui se passe

7. **EventSystem**
   - S'il y a un EventSystem dans la scène, vérifier qu'il ne bloque pas
   - Essayer de le désactiver temporairement pour tester

---

## 📌 Checklist finale

- [ ] PanelSettings créé et assigné
- [ ] WiseTwinCore avec 4 composants
- [ ] UI_Welcome avec LanguageSelectionUI
- [ ] UI_Training avec UIManager + ContentDisplayManager
- [ ] UI_Training désactivé au départ
- [ ] Objets 3D avec ObjectMetadataMapper + InteractableObject
- [ ] metadata.json dans StreamingAssets
- [ ] MyTrainingController connecté
- [ ] Test complet réussi

---

## 🎉 Félicitations !

Votre formation WiseTwin est maintenant fonctionnelle !