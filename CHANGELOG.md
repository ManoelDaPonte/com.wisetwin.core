# Changelog

All notable changes to the WiseTwin Core Package will be documented in this file.

## [1.2.0] - 2026-02-19

### Added
- **Dialogue System** - New `dialogue` scenario type for interactive branching conversations with NPCs
  - Visual node graph editor (`WiseTwin > Dialogue Graph Editor`) using Unity's GraphView API
  - 4 node types: Start, Dialogue, Choice, End - connected visually with drag-and-drop edges
  - Inline editing of EN/FR text fields directly on nodes
  - Dynamic choice management: add/remove options, toggle correct/incorrect per choice
  - Toolbar with colored node creation buttons and save functionality
  - Canvas with zoom, pan (middle mouse + right mouse drag), grid background, and minimap
  - Auto-import of runtime JSON format back into editor graph with auto-layout
- **DialogueDisplayer** - Runtime UI for dialogues with chat-bubble style
  - Modal overlay with speaker name, dialogue text, Continue button, and choice buttons
  - **Context display**: previous NPC dialogue shown above choices in a quote-style box
  - **Evaluated choices**: green/red visual feedback (800ms) when choices have `isCorrect` flags
  - **Neutral choices**: blue highlight (300ms) when no choice is marked correct - no judgment
  - Supports loops (NPC can redirect back to previous choice nodes)
  - Language change support during active dialogue
  - Player controls blocked during dialogue (same pattern as QuestionDisplayer)
- **Dialogue Analytics** - `DialogueInteractionData` and `DialogueChoiceRecord` classes
  - Tracks every choice made with `choiceNodeId`, `selectedChoiceId`, `wasCorrect`, `timestamp`
  - Computes `correctChoices`, `incorrectChoices`, `finalScore`, `completedDialogue`
- **Dialogue Editor Tab** - New "Dialogue" tab in WiseTwin Editor window
  - Create, edit, delete dialogue configurations
  - "Open Graph Editor" button per dialogue
  - Dialogue linking in Scenario Configuration tab via dropdown
- **Custom Vector2 JSON Converter** - Prevents Newtonsoft.Json self-referencing loop on Unity's Vector2

### Changed
- `ScenarioConfigurationData.cs` - Added `Dialogue` to `ScenarioType` enum, added `DialogueScenarioData` class
- `ContentTypes.cs` - Added `Dialogue` to `ContentType` enum
- `ScenarioData.cs` - Added `dialogue` JObject field and case in `GetContentData()`
- `ContentDisplayManager.cs` - Registers `DialogueDisplayer`, handles `"dialogue"` scenario type
- `InteractionData.cs` - Added `DialogueInteractionData` and `DialogueChoiceRecord`
- `WiseTwinEditor.cs` - Added Dialogue tab (now 5 tabs), JSON round-trip for dialogue data
- `WiseTwinEditorData.cs` - Added `dialogues` list and `selectedDialogueIndex`
- `WiseTwinEditorScenariosTab.cs` - Added dialogue scenario editor with dropdown and graph editor button
- `TrainingCompletionUI.cs` - Added try-catch around PanelSettings assignment to prevent AssertionException

## [1.1.0] - 2026-02-18

### Added
- **Zone Trigger Validation** - New procedure step validation type where the player walks into a trigger zone to validate the step (in addition to existing Click and Manual types)
  - `ProcedureZoneTrigger.cs` component detects player entry via `CharacterController` + `OnTriggerEnter`
  - `ValidationType` enum (`Click`, `Manual`, `Zone`) replaces the old `requireManualValidation` boolean
  - `zoneObjectName` field on procedure steps to reference zone GameObjects in the scene
- **Validation Zone Prefab Creator** - Editor menu `WiseTwin > Create Validation Zone Prefab` generates a ready-to-use zone prefab with:
  - SphereCollider (isTrigger)
  - Transparent green ground disc
  - Glowing green ring (LineRenderer) on the perimeter
  - Upward particle effect from the circle edge
- **Package CLAUDE.md** - Comprehensive documentation for AI-assisted development, covering architecture, all components, JSON format, public API, and conventions

### Changed
- `ProcedureDisplayer.cs` - Refactored step validation logic from if/else to switch-based dispatch supporting click/manual/zone types
- `ScenarioConfigurationData.cs` - Replaced `requireManualValidation` bool with `ValidationType` enum + zone fields
- `WiseTwinEditorScenariosTab.cs` - Replaced manual validation toggle with `ValidationType` dropdown, shows zone object field when Zone is selected
- `WiseTwinEditor.cs` - JSON export uses `validationType` string instead of `requireManualValidation` boolean

### Backward Compatibility
- Old JSON metadata with `"requireManualValidation": true` is automatically converted to `"validationType": "manual"` on import
- Missing `validationType` field defaults to `"click"`
- Runtime `ProcedureStep.requireManualValidation` kept as computed read-only property for code compatibility

## [1.0.0] - Initial Release

### Features
- Scenario-based training system (Question, Procedure, Text)
- Multi-language support (EN/FR) with LocalizationManager
- Click and Manual validation for procedure steps
- Fake objects system for procedure steps
- Step images with zoom overlay
- Video trigger system (click 3D object to play video)
- Training analytics tracking and JSON export
- Azure API / local StreamingAssets metadata loading
- WebGL integration for web-based training
- WiseTwin Editor window for visual configuration
- Training HUD with timer and progress bar
