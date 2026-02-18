# Changelog

All notable changes to the WiseTwin Core Package will be documented in this file.

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
