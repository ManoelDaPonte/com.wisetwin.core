# WiseTwin Core Package

A Unity package for creating interactive training experiences with seamless Unity ↔ Web integration.

## 🎯 Overview

WiseTwin Core enables you to build Unity training applications that can communicate with web platforms (React/JavaScript). The package provides metadata management, content loading from external sources, and completion notifications for training scenarios.

## 📦 Installation

### Via Package Manager (Git URL)

1. Open Unity Package Manager (`Window` > `Package Manager`)
2. Click `+` and select `Add package from git URL...`
3. Enter: `https://github.com/ManoelDaPonte/com.wisetwin.core.git`

### Via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.wisetwin.core": "https://github.com/ManoelDaPonte/com.wisetwin.core.git"
  }
}
```

## 🚀 Quick Start

### 1. Initial Setup

1. Import the WiseTwin Core package
2. Open **WiseTwin > WiseTwin Manager**
3. Configure your environment settings (Local/Production mode)
4. Click **"Setup Scene"** to add required components

### 2. Create Training Metadata

1. In WiseTwin Manager, go to **"Metadata Manager"** tab
2. Click **"Open Metadata Manager Window"**
3. Fill in your training information:
   - Title, description, duration
   - Difficulty level and tags
   - Unity content (questions, interactions, etc.)
4. Click **"Generate Metadata"** to export to StreamingAssets

### 3. Load Data in Your Scripts

```csharp
using WiseTwin;

public class MyTrainingScript : MonoBehaviour
{
    void Start()
    {
        // Wait for metadata to load
        MetadataLoader.Instance.OnMetadataLoaded += OnDataReady;
    }
    
    void OnDataReady(Dictionary<string, object> metadata)
    {
        // Get data for a specific Unity object
        var questionData = MetadataLoader.Instance.GetDataForObject("cube_rouge");
        
        // Or get typed content
        var question = MetadataLoader.Instance.GetContentForObject<QuestionContent>("cube_rouge", "question_1");
        
        if (question != null)
        {
            Debug.Log($"Question: {question.text}");
        }
    }
}
```

### 4. Complete the Training

```csharp
public class TrainingController : MonoBehaviour
{
    public TrainingCompletionNotifier completionNotifier;
    
    void CompleteTraining()
    {
        // Notify the web application that training is complete
        completionNotifier.FormationCompleted();
    }
}
```

## 🔧 Components

### Core Components

#### **MetadataLoader** (Singleton)
- **Purpose**: Loads training metadata from local JSON or Azure API
- **Key Methods**:
  - `GetDataForObject(string objectId)`: Get raw data for Unity object
  - `GetContentForObject<T>(string objectId, string contentKey)`: Get typed content
  - `ReloadMetadata()`: Refresh metadata
- **Configuration**: Auto-configured via WiseTwin Manager settings

#### **TrainingCompletionNotifier**
- **Purpose**: Notifies web application when training is completed
- **Usage**: Call `FormationCompleted()` when user completes training
- **WebGL**: Uses JavaScript interop (`NotifyFormationCompleted()`)
- **Editor**: Shows debug logs for testing

#### **TestTrainingCompletionNotifier**
- **Purpose**: Development tool for testing completion notifications
- **Features**: Press configurable key (default: Y) to test completion
- **UI**: Shows on-screen debug information

### Editor Tools

#### **WiseTwin Manager**
Central hub for managing your WiseTwin project:

- **General Settings Tab**:
  - Local vs Production mode configuration
  - Azure API settings (for production)
  - Debug and timeout settings
  - Scene component overview

- **Metadata Manager Tab**:
  - Quick access to Metadata Manager window
  - Training configuration interface

#### **Metadata Manager**
Comprehensive tool for creating training metadata:

- **Configuration Tab**: Basic training info (title, description, difficulty)
- **Unity Objects Tab**: Define interactive content (questions, media, dialogues)
- **Export & Preview Tab**: Generate and preview metadata JSON

## 📋 Metadata Structure

The metadata JSON follows this structure:

```json
{
  "id": "my-training",
  "title": "My Interactive Training",
  "description": "Learn Unity basics through interaction",
  "version": "1.0.0",
  "category": "Intermediate",
  "duration": "30 minutes",
  "difficulty": "Intermediate",
  "tags": ["unity", "interactive", "3d"],
  "imageUrl": "https://example.com/image.jpg",
  "unity": {
    "cube_rouge": {
      "question_1": {
        "text": "What color is this cube?",
        "type": "multiple-choice",
        "options": ["Red", "Blue", "Green"],
        "correctAnswer": 0,
        "feedback": "Correct! It's red.",
        "incorrectFeedback": "Look closer at the color!"
      }
    },
    "sphere_bleue": {
      "interaction_1": {
        "type": "dialogue",
        "character": "Teacher",
        "text": "Click on this sphere to learn more!"
      }
    }
  }
}
```

## 🏗️ Typical Workflow

### For New Projects

1. **Setup**:
   - Import WiseTwin Core package
   - Run WiseTwin Manager > Setup Scene

2. **Configure**:
   - Set Local/Production mode in WiseTwin Manager
   - Create training metadata using Metadata Manager

3. **Develop**:
   - Build your Unity training scene
   - Use `MetadataLoader.Instance.GetDataForObject()` to load content
   - Implement your training logic

4. **Test**:
   - Use TestTrainingCompletionNotifier for local testing
   - Verify completion notification works

5. **Deploy**:
   - Switch to Production mode
   - Build for WebGL
   - Deploy with your web application

### For Existing Projects

1. **Import**: Add WiseTwin Core package
2. **Migrate**: Use WiseTwin Manager to configure existing components
3. **Enhance**: Add metadata loading to existing scripts

## 🌐 Local vs Production Mode

### Local Mode
- Loads metadata from `StreamingAssets/{project-name}-metadata.json`
- Completion notifications show as debug logs
- Includes TestTrainingCompletionNotifier for development

### Production Mode
- Loads metadata from Azure API
- Completion notifications sent to parent web application
- Optimized for WebGL deployment

Switch modes easily in **WiseTwin Manager > General Settings**.

## 🔍 Content Types

The package supports flexible content types in the Unity section:

### Questions
```json
{
  "text": "Your question here",
  "type": "multiple-choice|true-false|text-input",
  "options": ["Option 1", "Option 2"],
  "correctAnswer": 0,
  "feedback": "Correct message",
  "incorrectFeedback": "Wrong answer message"
}
```

### Media
```json
{
  "title": "Video Tutorial",
  "description": "Learn the basics",
  "mediaUrl": "https://example.com/video.mp4",
  "type": "video|audio|image",
  "duration": "5 minutes"
}
```

### Dialogues
```json
{
  "character": "Teacher",
  "lines": ["Welcome!", "Let's start learning."],
  "choices": ["Continue", "Skip"],
  "emotion": "happy"
}
```

### Custom Content
The Unity section supports any JSON structure - define what your training needs!

## 🛠️ API Reference

### MetadataLoader

```csharp
// Singleton access
MetadataLoader.Instance

// Events
OnMetadataLoaded?.Invoke(Dictionary<string, object> metadata)
OnLoadError?.Invoke(string error)

// Methods
Dictionary<string, object> GetDataForObject(string objectId)
T GetContentForObject<T>(string objectId, string contentKey = null)
void ReloadMetadata()
List<string> GetAvailableObjectIds()
string GetProjectInfo(string key)

// Properties
bool IsLoaded
string ProjectName
```

### TrainingCompletionNotifier

```csharp
// Methods
void FormationCompleted(string trainingName = null)
```

## 🔧 Requirements

- **Unity**: 2021.3 or later
- **Platforms**: WebGL (primary), Standalone (development)
- **Dependencies**: Newtonsoft.Json (auto-installed)

## 🐛 Troubleshooting

### Common Issues

**Q: Metadata not loading**
- Check WiseTwin Manager > General Settings for correct mode
- Verify JSON file exists in StreamingAssets (Local mode)
- Check Azure API configuration (Production mode)

**Q: Completion notification not working**
- Ensure TrainingCompletionNotifier is in the scene
- Check mode setting (Local shows logs, Production sends to web)
- Verify your web application is listening for the notification

**Q: Scene setup incomplete**
- Use WiseTwin Manager > Setup Scene to add missing components
- Check scene component overview in General Settings tab

### Debug Tips

- Enable debug logs in WiseTwin Manager > General Settings
- Use TestTrainingCompletionNotifier for testing completion flow
- Check Unity Console for detailed WiseTwin logs
- Use Metadata Manager preview to validate JSON structure

## 📞 Support

For technical support or questions about WiseTwin Core:

1. Check this README for common solutions
2. Review Unity Console logs with debug enabled
3. Use WiseTwin Manager diagnostic tools
4. Contact your development team lead

---

**Made with ❤️ for interactive Unity training experiences**