# CLAUDE.md - WiseTwin Core Package

## Package Overview

**WiseTwin Core** (`com.wisetwin.core`) is a Unity package for creating interactive training/learning experiences. It supports questions, step-by-step procedures, text content, interactive dialogues (branching conversation trees), video triggers, and multi-language (EN/FR). The package integrates with Azure for metadata loading in production and provides comprehensive analytics tracking.

- **Unity Version**: 2021.3+
- **Package Format**: Unity Package Manager (UPM)
- **Namespace**: `WiseTwin` (Runtime), `WiseTwin.Editor` (Editor)
- **Dependencies**: `com.unity.nuget.newtonsoft-json` (3.2.1), TextMeshPro, UI, InputSystem

---

## Architecture

### Singleton System

Three core singletons auto-discover each other at runtime. They are placed as children of a `WiseTwinSystem` GameObject:

| Singleton | Role |
|-----------|------|
| `WiseTwinManager` | Main entry point, coordinator, metadata lifecycle |
| `MetadataLoader` | Loads training metadata (local StreamingAssets or Azure API) |
| `TrainingCompletionNotifier` | WebGL communication with parent web app |

Additional singletons:
- `LocalizationManager` - Language management (EN/FR), persists via PlayerPrefs
- `TrainingAnalytics` - Collects interaction data, exports JSON

### Data Flow

```
Editor (WiseTwinEditor) → JSON metadata → StreamingAssets
                                            ↓
MetadataLoader → loads at runtime (local or Azure)
                    ↓
ContentDisplayManager → dispatches to displayers
                    ↓
QuestionDisplayer / ProcedureDisplayer / TextDisplayer / DialogueDisplayer / VideoDisplayer
                    ↓
TrainingAnalytics → tracks all interactions → exports JSON
```

### Assembly Definitions

| Assembly | File | Scope | References |
|----------|------|-------|------------|
| `WiseTwin.Runtime` | `Runtime/WiseTwin.Runtime.asmdef` | All platforms | TextMeshPro, UI, InputSystem |
| `WiseTwin.Editor` | `Editor/WiseTwin.Editor.asmdef` | Editor only | WiseTwin.Runtime |

---

## File Map

### Core Runtime (`Runtime/Core/`)
- `WiseTwinManager.cs` - Main manager singleton, events: `OnMetadataReady`, `OnMetadataError`, `OnTrainingCompleted`
- `MetadataLoader.cs` - Loads metadata from local/Azure, supports legacy + scenario formats
- `ScenarioData.cs` - `ScenarioData` class (id, type, question/procedure/text/dialogue JObjects), `TrainingSettings`
- `DialogueData.cs` - Runtime dialogue structures: `DialogueTreeData`, `DialogueNodeRuntime`, `DialogueChoiceRuntime` (parsed from JSON at runtime)
- `LocalizationManager.cs` - Language singleton, `CurrentLanguage`, `OnLanguageChanged` event
- `VideoTriggerData.cs` - Runtime data for video triggers, language-aware URL selection
- `VideoTriggerManager.cs` - Sets up `VideoClickHandler` on target GameObjects
- `WiseTwinSystemManager.cs` - Optional system manager

### Analytics (`Runtime/Analytics/`)
- `TrainingAnalytics.cs` - Singleton, tracks interactions, procedure steps, exports JSON
- `InteractionData.cs` - Data structures: `InteractionData`, `QuestionInteractionData`, `ProcedureInteractionData`, `ProcedureStepData`, `TextInteractionData`, `DialogueInteractionData`, `DialogueChoiceRecord`

### Data (`Runtime/Data/`)
- `MetadataClasses.cs` - `FormationMetadataComplete`, `LocalizedString`, `ApiResponse`
- `ContentTypes.cs` - Enums: `ContentType` (Question, Procedure, Text, Dialogue), `QuestionType`, `MediaType`, `DifficultyLevel`, `ProgressState`
- `TrainingAnalyticsData.cs` - Export data format

### UI Displayers (`Runtime/UI/ContentDisplayers/`)
- `ContentDisplayManager.cs` - Coordinator, dispatches scenarios to appropriate displayer
- `QuestionDisplayer.cs` - Question rendering with single/multiple choice, validation, feedback
- `ProcedureDisplayer.cs` - Step-by-step procedure with 3 validation types (click, manual, zone)
- `TextDisplayer.cs` - Simple text/information display
- `DialogueDisplayer.cs` - Interactive branching dialogue with chat-bubble UI, evaluated/neutral choices, analytics
- `VideoDisplayer.cs` - Fullscreen video player overlay
- `TrainingCompletionUI.cs` - Training completion screen

### UI Controllers (`Runtime/UI/Controllers/`)
- `WiseTwinUIManager.cs` - Main UI coordination
- `TrainingHUD.cs` - HUD overlay (timer, progress bar)
- `LanguageSelectionUI.cs` - Language picker
- `TutorialUI.cs` - Tutorial display

### UI Components (`Runtime/UI/Components/`)
- `ProcedureStepClickHandler.cs` - Temporary component for click-based step validation (raycast + hover feedback)
- `ProcedureZoneTrigger.cs` - Temporary component for zone-based step validation (OnTriggerEnter with CharacterController)
- `VideoClickHandler.cs` - Click detection for video-enabled 3D objects
- `ProgressionManager.cs` - Scenario progression tracking

### Camera (`Runtime/Camera/`)
- `FirstPersonCharacter.cs` - First-person character controller

### Communication (`Runtime/Communication/`)
- `TrainingCompletionNotifier.cs` - WebGL completion notification
- `WiseTwinAuthManager.cs` - Authentication manager

### Editor (`Editor/`)
- `WiseTwinEditor.cs` - Main editor window (Window > WiseTwin > WiseTwin Editor), 5 tabs
- `WiseTwinEditorData.cs` - Editor data container
- `WiseTwinEditorGeneralTab.cs` - General settings tab
- `WiseTwinEditorMetadataTab.cs` - Metadata config tab
- `WiseTwinEditorScenariosTab.cs` - Scenario editor + `ScenarioImportWindow`
- `WiseTwinEditorDialogueTab.cs` - Dialogue management tab (create, edit, delete dialogues, open graph editor)
- `WiseTwinEditorVideoTab.cs` - Video triggers tab
- `ScenarioConfigurationData.cs` - Editor data classes: `ScenarioConfiguration`, `DialogueScenarioData`, `ProcedureStep`, `ValidationType` enum, `FakeObject`, etc.
- `ValidationZonePrefabCreator.cs` - Menu item to create validation zone prefab
- `WiseTwinBuildProcessor.cs` - Auto-configures settings for WebGL builds

### Editor - Dialogue Graph Editor (`Editor/DialogueEditor/`)
- `DialogueGraphData.cs` - Editor data model: `DialogueGraphEditorData`, `DialogueNodeEditorData`, `DialogueChoiceEditorData`, `DialogueEdgeData`
- `DialogueEditorWindow.cs` - EditorWindow with toolbar (add nodes, save). Access via `WiseTwin > Dialogue Graph Editor` or from Dialogue tab
- `DialogueGraphView.cs` - GraphView canvas with zoom, pan (middle/right mouse), grid, minimap
- `DialogueNodeView.cs` - Visual nodes (Start/Dialogue/Choice/End) with inline EN/FR text fields, dynamic choice ports
- `DialogueGraphSerializer.cs` - Serialization: editor format (with positions) <-> runtime JSON format, custom `Vector2Converter` for Newtonsoft.Json

### Plugins
- `Plugins/WebGL/WiseTwinWebGL.jslib` - JS interop (`NotifyFormationCompleted`, `GetUrlParameter`)

### Prefabs
- `Prefabs/WiseTwinSystem.prefab` - Main system GameObject
- `Prefabs/Player.prefab` - First-person character

---

## Scenario System

### Types

| Type | Displayer | Description |
|------|-----------|-------------|
| `question` | `QuestionDisplayer` | Single/multiple choice with feedback, hints, sequential questions |
| `procedure` | `ProcedureDisplayer` | Ordered steps with 3D object interaction |
| `text` | `TextDisplayer` | Information display with title + content |
| `dialogue` | `DialogueDisplayer` | Branching conversation tree with NPC, evaluated or neutral choices |

### Dialogue System

Interactive branching conversations built with a visual node graph editor. Supports 4 node types:

| Node | Input Ports | Output Ports | Content |
|------|------------|-------------|---------|
| **Start** | 0 | 1 | Entry point (1 per graph) |
| **Dialogue** | 1 (Multi) | 1 | Speaker name EN/FR + text EN/FR |
| **Choice** | 1 (Multi) | N (1 per option) | Prompt EN/FR + N options with text EN/FR + optional `isCorrect` flag |
| **End** | 1 (Multi) | 0 | Exit point |

**Evaluated vs Neutral choices:**
- If at least one choice has `isCorrect = true` → **evaluated**: green/red feedback (800ms delay), tracked in analytics
- If no choice has `isCorrect = true` → **neutral**: blue highlight (300ms), no correct/incorrect judgment

**Context display:** When showing a Choice node, the previous Dialogue node's speaker and text are displayed above the choices in a quote-style box, so the user remembers the NPC's question.

**Loops:** Supported naturally - a Dialogue node can point back to a previous Choice node (hub-and-spoke pattern).

### Procedure Validation Types

Each procedure step has a `validationType`:

| Value | Behavior | Editor Enum |
|-------|----------|-------------|
| `click` | Player clicks highlighted 3D object | `ValidationType.Click` |
| `manual` | Player clicks "Validate Step" button | `ValidationType.Manual` |
| `zone` | Player walks into a trigger zone (CharacterController) | `ValidationType.Zone` |

Zone validation uses `ProcedureZoneTrigger` component added at runtime to the zone GameObject. The zone object must have a Collider set to `isTrigger`. Use menu `WiseTwin > Create Validation Zone Prefab` to generate a prefab with:
- **SphereCollider** (isTrigger, radius 1.5m, centered at Y=1)
- **GroundDisc** - Flat transparent green cylinder (URP/Built-in compatible)
- **GlowRing** - LineRenderer drawing a 64-segment glowing green circle on the perimeter (additive material)
- **UpwardGlow** - ParticleSystem (Cone shape, edge emission, billboard mode) with Transform rotated -90 on X to emit upward

### Procedure Features
- **Fake objects**: Wrong objects highlighted alongside correct one, show custom error message on click
- **Step images**: Optional images per step (EN/FR) with zoom overlay
- **Highlight + blinking**: Configurable per step (color, blinking on/off)
- **Analytics**: Tracks duration, wrong clicks per step, perfect completion

---

## JSON Metadata Format

### Complete Structure

```json
{
  "id": "scene-name",
  "title": { "en": "Training Title", "fr": "Titre Formation" },
  "description": { "en": "...", "fr": "..." },
  "version": "1.0.0",
  "duration": "30 minutes",
  "difficulty": "Intermediate",
  "tags": ["safety", "training"],
  "imageUrl": "",
  "scenarios": [
    {
      "id": "scenario_1",
      "type": "question",
      "question": { ... }
    },
    {
      "id": "scenario_2",
      "type": "procedure",
      "procedure": { ... }
    },
    {
      "id": "scenario_3",
      "type": "text",
      "text": { ... }
    },
    {
      "id": "scenario_4",
      "type": "dialogue",
      "dialogue": { ... }
    }
  ],
  "videoTriggers": [
    {
      "targetObjectName": "Tower_1",
      "videoUrl": { "en": "https://example.com/en.mp4", "fr": "https://example.com/fr.mp4" }
    }
  ],
  "settings": {
    "allowPause": true,
    "showTimer": true,
    "showProgress": true
  },
  "unity": { ... }
}
```

### Question Scenario

```json
{
  "id": "quiz_1",
  "type": "question",
  "question": {
    "questionText": { "en": "What is...?", "fr": "Qu'est-ce que...?" },
    "options": {
      "en": ["Option A", "Option B", "Option C"],
      "fr": ["Option A", "Option B", "Option C"]
    },
    "correctAnswers": [1],
    "isMultipleChoice": false,
    "feedback": { "en": "Correct!", "fr": "Correct !" },
    "incorrectFeedback": { "en": "Try again", "fr": "Réessayez" },
    "hint": { "en": "Think about...", "fr": "Pensez à..." }
  }
}
```

Multiple questions in one scenario use `"questions": [...]` array instead of `"question"`.

### Procedure Scenario

```json
{
  "id": "proc_1",
  "type": "procedure",
  "procedure": {
    "title": { "en": "Safety Procedure", "fr": "Procédure de sécurité" },
    "description": { "en": "Follow these steps", "fr": "Suivez ces étapes" },
    "steps": [
      {
        "text": { "en": "Click the valve", "fr": "Cliquez sur la vanne" },
        "targetObjectName": "Valve_01",
        "validationType": "click",
        "highlightColor": "#FFFF00",
        "useBlinking": true,
        "fakeObjects": [
          {
            "objectName": "Valve_02",
            "errorMessage": { "en": "Wrong valve!", "fr": "Mauvaise vanne !" }
          }
        ]
      },
      {
        "text": { "en": "Walk to safety zone", "fr": "Dirigez-vous vers la zone" },
        "targetObjectName": "",
        "validationType": "zone",
        "zoneObjectName": "SafetyZone_1"
      },
      {
        "text": { "en": "Read the instructions", "fr": "Lisez les instructions" },
        "targetObjectName": "",
        "validationType": "manual",
        "imagePath": { "en": "instructions_en.png", "fr": "instructions_fr.png" }
      }
    ],
    "fakeObjects": []
  }
}
```

**Backward compatibility**: Old JSON with `"requireManualValidation": true` is auto-converted to `"validationType": "manual"`. Missing `validationType` defaults to `"click"`.

### Text Scenario

```json
{
  "id": "info_1",
  "type": "text",
  "text": {
    "title": { "en": "Safety Info", "fr": "Info Sécurité" },
    "content": { "en": "Important information...", "fr": "Information importante..." }
  }
}
```

### Dialogue Scenario

```json
{
  "id": "dialogue_1",
  "type": "dialogue",
  "dialogue": {
    "title": { "en": "Safety Briefing", "fr": "Briefing sécurité" },
    "startNodeId": "node_001",
    "nodes": [
      {
        "id": "node_001",
        "type": "start",
        "nextNodeId": "node_002"
      },
      {
        "id": "node_002",
        "type": "dialogue",
        "speaker": { "en": "Safety Officer", "fr": "Agent de sécurité" },
        "text": { "en": "Are you ready?", "fr": "Êtes-vous prêt ?" },
        "nextNodeId": "node_003"
      },
      {
        "id": "node_003",
        "type": "choice",
        "text": { "en": "How do you respond?", "fr": "Comment répondez-vous ?" },
        "choices": [
          {
            "id": "choice_a",
            "text": { "en": "Yes, ready!", "fr": "Oui, prêt !" },
            "isCorrect": true,
            "nextNodeId": "node_004"
          },
          {
            "id": "choice_b",
            "text": { "en": "I don't care", "fr": "Je m'en fiche" },
            "isCorrect": false,
            "nextNodeId": "node_005"
          }
        ]
      },
      { "id": "node_004", "type": "end" },
      {
        "id": "node_005",
        "type": "dialogue",
        "speaker": { "en": "Safety Officer", "fr": "Agent de sécurité" },
        "text": { "en": "Let me ask again...", "fr": "Laissez-moi reposer la question..." },
        "nextNodeId": "node_003"
      }
    ]
  }
}
```

For **neutral choices** (no evaluation), omit `isCorrect` or set all choices to `false`.

---

## Public API

### WiseTwinManager
```csharp
WiseTwinManager.Instance.MetadataLoader          // Access metadata
WiseTwinManager.Instance.CompleteTraining(name)   // Complete training + notify WebGL
WiseTwinManager.Instance.ReloadMetadata()         // Reload from source
WiseTwinManager.Instance.SavePlayerSpawnPosition()
WiseTwinManager.Instance.ResetPlayerPosition()
WiseTwinManager.Instance.SetPreferredLanguage("fr")
```

### MetadataLoader
```csharp
MetadataLoader.Instance.GetScenarios()            // List<ScenarioData>
MetadataLoader.Instance.GetScenario(index)         // ScenarioData
MetadataLoader.Instance.GetSettings()              // TrainingSettings
MetadataLoader.Instance.GetVideoTriggers()         // List<object>
MetadataLoader.Instance.IsLoaded                   // bool
```

### LocalizationManager
```csharp
LocalizationManager.Instance.CurrentLanguage       // "en" or "fr"
LocalizationManager.Instance.SetLanguage("fr")
LocalizationManager.Instance.OnLanguageChanged += (lang) => { ... };
LocalizationManager.Instance.GetLocalizedText(data, fallback)
```

### TrainingAnalytics
```csharp
TrainingAnalytics.Instance.StartInteraction(objectId, type, subtype)
TrainingAnalytics.Instance.EndCurrentInteraction(success)
TrainingAnalytics.Instance.LogAttempt(isCorrect)
TrainingAnalytics.Instance.StartProcedureInteraction(objectId, procedureKey, totalSteps)
TrainingAnalytics.Instance.AddProcedureStepData(stepData)
TrainingAnalytics.Instance.CompleteProcedureInteraction(perfect, wrongClicks, duration)
TrainingAnalytics.Instance.ExportAnalytics()       // JSON string
```

### ContentDisplayManager
```csharp
ContentDisplayManager.Instance.DisplayScenario(scenarioIndex)
```

### IContentDisplayer Interface
```csharp
public interface IContentDisplayer
{
    void Display(string objectId, Dictionary<string, object> contentData, VisualElement root);
    event Action<string> OnClosed;
    event Action<string, bool> OnCompleted;
}
```

---

## Language System

All text uses localized dictionaries: `{"en": "English text", "fr": "Texte français"}`

- `LocalizationManager.Instance.CurrentLanguage` for current language code
- `OnLanguageChanged` event triggers UI refresh
- UI components subscribe and update displayed content
- Video triggers fall back to other language if current is empty

---

## Analytics Tracking

### Key Rules
- `firstAttemptCorrect` in QuestionDisplayer is set on FIRST validation only
- Each retry increments `attempts` but preserves initial correctness state
- Zone validation steps always report 0 wrong clicks
- Procedure tracks per-step and total wrong clicks, duration, and perfect completion
- Dialogue tracks every choice made (`choiceHistory`), including wrong answers, with timestamps and `wasCorrect` flags
- Neutral dialogue choices (no `isCorrect` set) are recorded but don't affect the score

### Data Export Structure
```json
{
  "sessionId": "uuid",
  "trainingId": "scene-name",
  "startTime": "ISO8601",
  "endTime": "ISO8601",
  "totalDuration": 120.5,
  "completionStatus": "completed",
  "interactions": [ ... ],
  "summary": {
    "totalInteractions": 5,
    "successfulInteractions": 4,
    "failedInteractions": 1,
    "totalAttempts": 8,
    "totalFailedAttempts": 3
  }
}
```

---

## Editor Window

Accessed via `Window > WiseTwin > WiseTwin Editor` (or `WiseTwin > WiseTwin Editor` menu).

### Tabs
1. **General Settings** - Title, description, version, difficulty, duration, tags, image URL
2. **Metadata Config** - Local/Production mode, Azure API URL, container ID, build type
3. **Scenario Configuration** - Add/edit/reorder scenarios, import from JSON
4. **Dialogue** - Create/edit/delete dialogues, open visual graph editor
5. **Video** - Add video triggers (drag GameObject + set URLs)

### Dialogue Graph Editor
Accessed via `WiseTwin > Dialogue Graph Editor` menu or "Open Graph Editor" button in Dialogue tab.

- **Toolbar**: Colored buttons to add Start (green), Dialogue (blue), Choice (orange), End (red) nodes + Save button
- **Canvas**: Zoom (scroll), pan (middle mouse or right mouse drag), grid background, minimap
- **Nodes**: Inline editable fields for speaker, text, choices (EN/FR). Choice nodes support dynamic add/remove of options with correct toggle
- **Save**: Converts graph to runtime JSON format stored in `DialogueScenarioData.graphDataJSON`
- **Import**: Automatically imports runtime JSON format back into editor graph with auto-layout

### Bottom Actions
- **Preview JSON** - Opens preview window with generated JSON
- **Generate Metadata** - Saves `{sceneName}-metadata.json` to StreamingAssets

### Prefab Creation
- `WiseTwin > Create Validation Zone Prefab` - Creates a zone prefab with trigger collider, visual cylinder, and particle effects

---

## WebGL Integration

- `WiseTwinWebGL.jslib` defines `NotifyFormationCompleted()` and `GetUrlParameter()`
- `WiseTwinBuildProcessor` auto-sets production mode on WebGL builds
- `TrainingCompletionNotifier` calls JS functions to communicate with parent web app

---

## Conventions

### Naming
- Runtime scripts: PascalCase (e.g., `MetadataLoader.cs`)
- Editor scripts: PascalCase (e.g., `WiseTwinEditor.cs`)
- Data classes: `*Data` suffix (e.g., `ScenarioData.cs`)
- Manager singletons: `*Manager` suffix (e.g., `WiseTwinManager.cs`)
- Displayers: `*Displayer` suffix (e.g., `ProcedureDisplayer.cs`)

### Debug Logging
All logs are prefixed with component name:
- `[WiseTwinManager]`, `[MetadataLoader]`, `[TrainingAnalytics]`
- `[ProcedureDisplayer]`, `[QuestionDisplayer]`, `[DialogueDisplayer]`, `[VideoDisplayer]`
- `[DialogueEditor]`, `[DialogueGraphView]`, `[DialogueGraphSerializer]`
- `[ProcedureZoneTrigger]`, `[VideoTriggerManager]`, `[VideoClickHandler]`

### Singleton Pattern
```csharp
public static T Instance { get; private set; }
void Awake() {
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
    // DontDestroyOnLoad handled by parent WiseTwinSystem
}
```

### Adding New Scripts
- Runtime scripts go in `Runtime/` (included in `WiseTwin.Runtime` assembly)
- Editor scripts go in `Editor/` (included in `WiseTwin.Editor` assembly, editor-only)
- Runtime cannot reference Editor assemblies

---

## Known Quirks

1. Metadata loader uses active scene name for file lookup - scenes must have proper names
2. Language changes may require manual UI refresh in some displayers
3. Analytics data is in-memory only - export before long sessions
4. Multiple WiseTwinManager instances across scenes: earlier ones destroy themselves
5. Editor settings saved to PersistentDataPath (separate from scene data)
6. `ProcedureStepClickHandler` uses new Input System (`Mouse.current`) for hover/click detection
7. Zone triggers require the player to have a `CharacterController` component (not Rigidbody)
