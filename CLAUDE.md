# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

WiseTwin Core is a Unity Package for creating interactive training experiences with Unity-Web integration. This is a Unity Package Manager (UPM) package following Unity's package structure conventions.

## Unity Package Structure

This is a Unity Package located at `Packages/com.wisetwin.core/`:

-   **Runtime/**: Core runtime scripts (MetadataLoader, WiseTwinManager, TrainingCompletionNotifier)
-   **Editor/**: Unity Editor tools and windows (WiseTwinEditor)
-   **UI/**: UI components and example training controllers
-   **package.json**: Package manifest defining dependencies and Unity version requirements

## Key Architecture Components

### Core Systems

1. **MetadataLoader** (Runtime/MetadataLoader.cs)

    - Singleton that loads training metadata from local JSON or Azure API
    - Manages Unity object content (questions, dialogues, media)
    - Provides typed access to content via `GetContentForObject<T>()`
    - Auto-switches between Local/Production modes

2. **WiseTwinManager** (Runtime/WiseTwinManager.cs)

    - Central singleton manager for the entire system
    - Coordinates MetadataLoader and TrainingCompletionNotifier
    - Provides simplified API for accessing training data
    - Manages debug settings and production/local mode switching

3. **TrainingCompletionNotifier** (Runtime/TrainingCompletionNotifier.cs)
    - Handles training completion notifications
    - WebGL: Communicates with parent web application via JavaScript
    - Editor: Shows debug logs for testing

### Editor Tools

**WiseTwinEditor** (Editor/WiseTwinEditor.cs)

-   Unified editor window combining settings and metadata management
-   Three main tabs: General Settings, Metadata Config, Unity Objects
-   Handles JSON generation and export to StreamingAssets
-   Manages Azure API configuration for production deployments

## Development Commands

Since this is a Unity package, there are no traditional build/test commands. Development workflow:

### Unity Editor Operations

-   Open WiseTwin Editor: Menu → `WiseTwin > WiseTwin Editor`
-   Setup scene components: Use "Setup Scene" button in WiseTwin Editor
-   Generate metadata: Use Metadata Config tab → Export & Preview

### Testing in Unity

-   Local testing: Set to Local Mode in WiseTwin Editor
-   Use TestTrainingCompletionNotifier component for completion flow testing
-   Check Unity Console for WiseTwin debug logs (enable in General Settings)

### Building for Deployment

-   WebGL Build: File → Build Settings → WebGL → Build
-   Standalone Build: For local development testing only
-   Ensure Production Mode is enabled for final builds

## Assembly Definitions

The package uses two assembly definitions:

-   **WiseTwin.Runtime.asmdef**: Runtime code assembly
-   **WiseTwin.Editor.asmdef**: Editor-only code assembly

This separation ensures editor code is excluded from builds.

## Data Flow

1. **Metadata Loading**:

    - Local Mode: Loads from `StreamingAssets/{project-name}-metadata.json`
    - Production Mode: Fetches from Azure API using configured URL

2. **Content Access Pattern**:

    ```csharp
    // Get data through WiseTwinManager singleton
    var data = WiseTwin.WiseTwinManager.Instance.GetDataForObject("objectId");
    // Or directly through MetadataLoader
    var content = MetadataLoader.Instance.GetContentForObject<QuestionContent>("objectId", "contentKey");
    ```

3. **Completion Notification**:
    - Call `TrainingCompletionNotifier.FormationCompleted()`
    - WebGL: Triggers JavaScript `NotifyFormationCompleted()`
    - Editor: Logs to Unity Console

## Important Conventions

-   All manager components use singleton pattern with `Instance` property
-   Debug logging controlled via WiseTwinManager.EnableDebugLogs
-   Metadata JSON structure must include "unity" section with object IDs as keys
-   Component auto-discovery uses `FindFirstObjectByType<T>()` for Unity 2023+

## Dependencies

-   Unity 2021.3 or later
-   Newtonsoft.Json (com.unity.nuget.newtonsoft-json: 3.2.1)
-   No external build tools or package managers required

Je travaille avec unity 6000.1.9f1 toutes les informations que tu me donnes doivent etre compatible avec cette version d'unity. Tu pourras faire des recherches directements sur internet afin de recuperer les dernieres informations et les meilleurs.

Ce projet est une librarie Unity qui me permettra d'importer des fonctionnalites communes dans chacun de mes projets Unity. Le but :

-   Avoir une connection avec mon application nextjs.
-   Uniformiser les UI de mes differerents projets.

Dans l'idee le workflow ideal :

-   On a un nouveau projet
-   On place les elements 3D
-   On defini les modalites des formations (questionnaires, QCM, Procédures...)
-   On remplie le fichier metadata avec le wisetwin manager
-   On bind les objects de notre scene a des questions ou alors des procedures ou autres ....
-   Quand on appuie sur cette objet en runtime alors on affiche l'UI associé.
-   Quand l'utilisateur fini la formation -> on envoie des infos a notre api react.
-   On evaluera, le temps, les reponses pour chacune des questions qui seront renvoye a react.
