using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

#if UNITY_EDITOR

public class WiseTwinEditor : EditorWindow
{
    // Settings stored as simple variables
    private bool useLocalMode = true;
    private string azureApiUrl = "https://your-domain.com/api/unity/metadata";
    private string containerId = "";
    private string buildType = "wisetrainer";
    private float requestTimeout = 30f;
    private int maxRetryAttempts = 3;
    private bool enableDebugLogs = true;
    private string settingsFilePath;
    
    // Metadata fields (integrated from MetadataManager)
    [Header("Project Settings")]
    public string projectTitle = "Training Test";
    public string projectDescription = "Training description";
    public string projectVersion = "1.0.0";
    public int durationMinutes = 30;
    public int difficultyIndex = 1;
    public string imageUrl = "";
    
    // Constantes pour les options de difficulté
    private readonly string[] difficultyOptions = { "Easy", "Intermediate", "Hard", "Very Hard" };
    
    [Header("Advanced Settings")]
    public List<string> tags = new List<string> { "training", "interactive" };
    
    [Header("Export Settings")]
    public bool includeTimestamp = true;
    
    // Metadata UI State
    private string unityContentJSON = "";
    private bool isUnityContentValid = true;
    private Vector2 unityContentScrollPosition;
    private bool hasLoadedExistingJSON = false;
    private string currentLoadedFile = "";
    private string projectId; // Auto-generated from Unity project name
    
    // UI State
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private string[] tabNames = { "General Settings", "Metadata Config", "Unity Objects" };
    
    // Language selector
    private int selectedLanguageIndex = 0;
    private readonly string[] supportedLanguages = { "English", "Français" };
    private readonly string[] languageCodes = { "en", "fr" };
    
    
    [MenuItem("WiseTwin/WiseTwin Editor")]
    public static void ShowWindow()
    {
        WiseTwinEditor window = GetWindow<WiseTwinEditor>("WiseTwin Editor");
        window.minSize = new Vector2(700, 600);
        window.Show();
    }
    
    void OnEnable()
    {
        settingsFilePath = Path.Combine(Application.persistentDataPath, "WiseTwinSettings.json");
        LoadSettings();
        LoadLanguagePreference();
        InitializeProjectId();
        LoadExistingJSONContent();
        InitializeUnityContent();
    }
    
    void LoadLanguagePreference()
    {
        string savedLanguage = PlayerPrefs.GetString("WiseTwin_Language", "en");
        for (int i = 0; i < languageCodes.Length; i++)
        {
            if (languageCodes[i] == savedLanguage)
            {
                selectedLanguageIndex = i;
                break;
            }
        }
    }
    
    void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(settingsFilePath);
                var settingsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
                
                if (settingsDict.ContainsKey("useLocalMode"))
                    useLocalMode = (bool)settingsDict["useLocalMode"];
                if (settingsDict.ContainsKey("azureApiUrl"))
                    azureApiUrl = settingsDict["azureApiUrl"].ToString();
                if (settingsDict.ContainsKey("containerId"))
                    containerId = settingsDict["containerId"].ToString();
                if (settingsDict.ContainsKey("buildType"))
                    buildType = settingsDict["buildType"].ToString();
                if (settingsDict.ContainsKey("requestTimeout"))
                    requestTimeout = Convert.ToSingle(settingsDict["requestTimeout"]);
                if (settingsDict.ContainsKey("maxRetryAttempts"))
                    maxRetryAttempts = Convert.ToInt32(settingsDict["maxRetryAttempts"]);
                if (settingsDict.ContainsKey("enableDebugLogs"))
                    enableDebugLogs = (bool)settingsDict["enableDebugLogs"];
                    
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WiseTwin] Could not load settings: {e.Message}");
            }
        }
    }
    
    void SaveSettings()
    {
        try
        {
            var settingsDict = new Dictionary<string, object>
            {
                ["useLocalMode"] = useLocalMode,
                ["azureApiUrl"] = azureApiUrl,
                ["containerId"] = containerId,
                ["buildType"] = buildType,
                ["requestTimeout"] = requestTimeout,
                ["maxRetryAttempts"] = maxRetryAttempts,
                ["enableDebugLogs"] = enableDebugLogs
            };
            
            string jsonContent = JsonConvert.SerializeObject(settingsDict, Formatting.Indented);
            File.WriteAllText(settingsFilePath, jsonContent);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WiseTwin] Could not save settings: {e.Message}");
        }
    }
    
    
    void InitializeProjectId()
    {
        // Try to get custom project name first
        string savedProjectName = PlayerPrefs.GetString("WiseTwin_ProjectName", "");
        if (!string.IsNullOrEmpty(savedProjectName))
        {
            projectId = savedProjectName;
            return;
        }
        
        // Fallback to Unity project name
        projectId = Application.productName;
        if (string.IsNullOrEmpty(projectId))
        {
            projectId = "unity-project";
        }
    }
    
    void LoadExistingJSONContent()
    {
        
        string targetFileName = $"{projectId}-metadata.json";
        
        // Possible paths to search for JSON file
        string[] possiblePaths = {
            Path.Combine(Application.streamingAssetsPath, targetFileName),
            Path.Combine(Application.streamingAssetsPath, "metadata.json"), // Fallback
        };
        
        string foundPath = null;
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                foundPath = path;
                break;
            }
        }
        
        if (foundPath != null)
        {
            try
            {
                string jsonContent = File.ReadAllText(foundPath);
                ParseExistingJSON(jsonContent);
                currentLoadedFile = Path.GetFileName(foundPath);
                hasLoadedExistingJSON = true;
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error loading JSON: {e.Message}");
                hasLoadedExistingJSON = false;
            }
        }
        else
        {
            hasLoadedExistingJSON = false;
        }
    }
    
    void ParseExistingJSON(string jsonContent)
    {
        try
        {
            var metadata = JsonConvert.DeserializeObject<FormationMetadataComplete>(jsonContent);
            
            // Load basic data
            if (!string.IsNullOrEmpty(metadata.title)) projectTitle = metadata.title;
            if (!string.IsNullOrEmpty(metadata.description)) projectDescription = metadata.description;
            if (!string.IsNullOrEmpty(metadata.version)) projectVersion = metadata.version;
            if (!string.IsNullOrEmpty(metadata.imageUrl)) imageUrl = metadata.imageUrl;
            
            // Parse duration (extract numbers)
            if (!string.IsNullOrEmpty(metadata.duration))
            {
                ParseDurationFromString(metadata.duration);
            }
            
            // Parse difficulty (find index)
            if (!string.IsNullOrEmpty(metadata.difficulty))
            {
                ParseDifficultyFromString(metadata.difficulty);
            }
            
            // Load lists
            if (metadata.tags != null && metadata.tags.Count > 0)
                tags = new List<string>(metadata.tags);
            
            // 🎯 IMPORTANT: Extract Unity section (object content)
            if (metadata.unity != null)
            {
                // Convert Unity content directly to formatted JSON
                unityContentJSON = JsonConvert.SerializeObject(metadata.unity, Formatting.Indented);
                ValidateUnityContent();
                
            }
            else
            {
                InitializeUnityContent();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error parsing existing JSON: {e.Message}");
            InitializeUnityContent();
        }
    }
    
    void ParseDurationFromString(string durationStr)
    {
        try
        {
            // Extract numbers from string (e.g., "30 minutes" -> 30)
            string numbersOnly = System.Text.RegularExpressions.Regex.Match(durationStr, @"\d+").Value;
            if (!string.IsNullOrEmpty(numbersOnly))
            {
                durationMinutes = int.Parse(numbersOnly);
            }
        }
        catch
        {
            durationMinutes = 30; // default value
        }
    }
    
    void ParseDifficultyFromString(string difficultyStr)
    {
        for (int i = 0; i < difficultyOptions.Length; i++)
        {
            if (string.Equals(difficultyOptions[i], difficultyStr, System.StringComparison.OrdinalIgnoreCase))
            {
                difficultyIndex = i;
                return;
            }
        }
        difficultyIndex = 1; // Default "Intermediate"
    }
    
    void InitializeUnityContent()
    {
        if (string.IsNullOrEmpty(unityContentJSON))
        {
            unityContentJSON = JsonConvert.SerializeObject(new Dictionary<string, object>(), Formatting.Indented);
        }
        ValidateUnityContent();
    }
    
    void ValidateUnityContent()
    {
        try
        {
            if (!string.IsNullOrEmpty(unityContentJSON))
            {
                JsonConvert.DeserializeObject(unityContentJSON);
                isUnityContentValid = true;
            }
        }
        catch
        {
            isUnityContentValid = false;
        }
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawLoadedFileInfo();
        DrawTabs();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        switch (selectedTab)
        {
            case 0:
                DrawGeneralSettingsTab();
                break;
            case 1:
                DrawMetadataConfigTab();
                break;
            case 2:
                DrawUnityObjectsTab();
                break;
        }
        
        EditorGUILayout.EndScrollView();
        
        DrawBottomButtons();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), new Color(0.3f, 0.6f, 1f));
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("🎯", GUILayout.Width(30));
        EditorGUILayout.LabelField("WiseTwin Editor", EditorStyles.largeLabel);
        
        // Language selector in top-right corner
        GUILayout.FlexibleSpace();
        GUILayout.Label("🌐", GUILayout.Width(20));
        int newLanguageIndex = EditorGUILayout.Popup(selectedLanguageIndex, supportedLanguages, GUILayout.Width(100));
        if (newLanguageIndex != selectedLanguageIndex)
        {
            selectedLanguageIndex = newLanguageIndex;
            // Save language preference for runtime
            PlayerPrefs.SetString("WiseTwin_Language", languageCodes[selectedLanguageIndex]);
            PlayerPrefs.Save();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Centralized management for WiseTwin Unity projects", EditorStyles.helpBox);
        EditorGUILayout.Space();
    }
    
    void DrawLoadedFileInfo()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"🎯 Project: {projectId} (based on Unity name)", EditorStyles.boldLabel);
        
        if (hasLoadedExistingJSON)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Loaded file: {currentLoadedFile}", EditorStyles.helpBox);
            if (GUILayout.Button("🔄 Reload", GUILayout.Width(100)))
            {
                LoadExistingJSONContent();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField("ℹ️ No JSON file found. Creating new content.", EditorStyles.helpBox);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    
    void DrawTabs()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
        EditorGUILayout.Space();
    }
    
    void DrawGeneralSettingsTab()
    {
        EditorGUILayout.LabelField("🔧 General Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Environment Mode
        EditorGUILayout.LabelField("Environment Configuration", EditorStyles.boldLabel);
        bool newUseLocalMode = EditorGUILayout.Toggle("Use Local Mode", useLocalMode);
        if (newUseLocalMode != useLocalMode)
        {
            useLocalMode = newUseLocalMode;
            EditorUtility.SetDirty(this);
        }
        
        EditorGUILayout.HelpBox(
            useLocalMode ? 
            "🏠 Local Mode: Will load metadata from StreamingAssets folder" : 
            "☁️ Production Mode: Will load metadata from Azure API", 
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Azure Configuration (only show if not in local mode)
        if (!useLocalMode)
        {
            EditorGUILayout.LabelField("☁️ Azure Configuration", EditorStyles.boldLabel);
            azureApiUrl = EditorGUILayout.TextField("API Base URL", azureApiUrl);
            containerId = EditorGUILayout.TextField("Container ID", containerId);
            buildType = EditorGUILayout.TextField("Build Type", buildType);
            
            EditorGUILayout.Space(10);
        }
        
        // Debug Settings
        EditorGUILayout.LabelField("🐛 Debug Configuration", EditorStyles.boldLabel);
        enableDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs", enableDebugLogs);
        requestTimeout = EditorGUILayout.FloatField("Request Timeout (s)", requestTimeout);
        maxRetryAttempts = EditorGUILayout.IntField("Max Retry Attempts", maxRetryAttempts);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Project Information
        EditorGUILayout.LabelField("📋 Project Information", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Project Name", Application.productName);
        EditorGUILayout.TextField("Company Name", Application.companyName);
        EditorGUILayout.TextField("Unity Version", Application.unityVersion);
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
    }
    
    
    void DrawMetadataConfigTab()
    {
        // Basic Settings
        EditorGUILayout.LabelField("📋 Basic Configuration", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Project ID modifiable
        string newProjectId = EditorGUILayout.TextField("Project ID", projectId);
        if (newProjectId != projectId)
        {
            projectId = newProjectId;
            // Save custom project name
            PlayerPrefs.SetString("WiseTwin_ProjectName", projectId);
            PlayerPrefs.Save();
            EditorUtility.SetDirty(this);
        }
        EditorGUILayout.HelpBox("This ID will be used for metadata filename and across all components", MessageType.Info);
        
        projectTitle = EditorGUILayout.TextField("Title", projectTitle);
        
        EditorGUILayout.LabelField("Description");
        projectDescription = EditorGUILayout.TextArea(projectDescription, GUILayout.Height(60));
        
        projectVersion = EditorGUILayout.TextField("Version", projectVersion);
        
        // Duration in minutes (numeric field)
        EditorGUILayout.BeginHorizontal();
        durationMinutes = EditorGUILayout.IntField("Duration (minutes)", durationMinutes);
        EditorGUILayout.LabelField($"→ {durationMinutes} minutes", EditorStyles.miniLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        // Difficulty (dropdown)
        difficultyIndex = EditorGUILayout.Popup("Difficulty", difficultyIndex, difficultyOptions);
        
        imageUrl = EditorGUILayout.TextField("Image URL", imageUrl);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        // Advanced Settings
        EditorGUILayout.LabelField("⚙️ Advanced Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        DrawStringList("🏷️ Tags", tags);
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawUnityObjectsTab()
    {
        EditorGUILayout.LabelField("🎮 Unity Objects Configuration", EditorStyles.boldLabel);
        
        // Unity Content Editor
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✏️ Unity Content Editor (JSON)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Define objects and their contents (questions, interactions, etc.)", EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy", GUILayout.Width(80)))
        {
            EditorGUIUtility.systemCopyBuffer = unityContentJSON;
            ShowNotification(new GUIContent("JSON copied!"));
        }
        if (GUILayout.Button("📥 Paste", GUILayout.Width(80)))
        {
            unityContentJSON = EditorGUIUtility.systemCopyBuffer;
            ValidateUnityContent();
        }
        if (GUILayout.Button("🧹 Clear", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to clear Unity content?", "Yes", "Cancel"))
            {
                unityContentJSON = "{}";
                ValidateUnityContent();
            }
        }
        if (GUILayout.Button("🎯 Example", GUILayout.Width(80)))
        {
            LoadExampleUnityContent();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        unityContentScrollPosition = EditorGUILayout.BeginScrollView(unityContentScrollPosition, GUILayout.Height(300));
        
        GUI.color = isUnityContentValid ? Color.white : new Color(1f, 0.8f, 0.8f);
        string newContent = EditorGUILayout.TextArea(unityContentJSON, GUILayout.ExpandHeight(true));
        GUI.color = Color.white;
        
        if (newContent != unityContentJSON)
        {
            unityContentJSON = newContent;
            ValidateUnityContent();
        }
        
        EditorGUILayout.EndScrollView();
        
        if (!isUnityContentValid)
        {
            EditorGUILayout.HelpBox("❌ Invalid JSON! Check syntax.", MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("✅ Valid JSON", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("💡 Tips", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Use Unity object IDs as main keys", EditorStyles.helpBox);
        EditorGUILayout.LabelField("• For multi-language: use {\"en\": \"English text\", \"fr\": \"Texte français\"}", EditorStyles.helpBox);
        EditorGUILayout.LabelField("• Language is selected automatically from the top-right dropdown", EditorStyles.helpBox);
        EditorGUILayout.LabelField("• Examples: questions, dialogues, instructions, media, etc.", EditorStyles.helpBox);
        
        EditorGUILayout.EndVertical();
    }
    
    void LoadExampleUnityContent()
    {
        var exampleContent = new Dictionary<string, Dictionary<string, object>>
        {
            ["red_cube"] = new Dictionary<string, object>
            {
                ["question_1"] = new Dictionary<string, object>
                {
                    ["text"] = new Dictionary<string, object>
                    {
                        ["en"] = "What color is this cube?",
                        ["fr"] = "De quelle couleur est ce cube ?"
                    },
                    ["type"] = "multiple-choice",
                    ["options"] = new Dictionary<string, object>
                    {
                        ["en"] = new string[] { "Red", "Blue", "Green" },
                        ["fr"] = new string[] { "Rouge", "Bleu", "Vert" }
                    },
                    ["correctAnswer"] = 0,
                    ["feedback"] = new Dictionary<string, object>
                    {
                        ["en"] = "Correct! It's red.",
                        ["fr"] = "Correct ! C'est rouge."
                    },
                    ["incorrectFeedback"] = new Dictionary<string, object>
                    {
                        ["en"] = "Look closer at the color!",
                        ["fr"] = "Regardez mieux la couleur !"
                    }
                }
            },
            ["blue_sphere"] = new Dictionary<string, object>
            {
                ["question_1"] = new Dictionary<string, object>
                {
                    ["text"] = new Dictionary<string, object>
                    {
                        ["en"] = "Is this shape a sphere?",
                        ["fr"] = "Cette forme est-elle une sphère ?"
                    },
                    ["type"] = "true-false",
                    ["correctAnswer"] = 1,
                    ["feedback"] = new Dictionary<string, object>
                    {
                        ["en"] = "Exactly! It's a sphere.",
                        ["fr"] = "Exactement ! C'est une sphère."
                    },
                    ["incorrectFeedback"] = new Dictionary<string, object>
                    {
                        ["en"] = "Look carefully at the shape!",
                        ["fr"] = "Regardez attentivement la forme !"
                    }
                }
            }
        };
        
        unityContentJSON = JsonConvert.SerializeObject(exampleContent, Formatting.Indented);
        ValidateUnityContent();
        ShowNotification(new GUIContent("Multi-language example loaded!"));
    }
    
    void DrawStringList(string label, List<string> list)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i] = EditorGUILayout.TextField($"  [{i}]", list[i]);
            if (GUILayout.Button("❌", GUILayout.Width(25)))
            {
                list.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button($"➕ Add {label}"))
        {
            list.Add("");
        }
        
        EditorGUILayout.Space();
    }
    
    void DrawBottomButtons()
    {
        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔍 Preview JSON", GUILayout.Height(30)))
        {
            ShowJSONPreview();
        }
        
        if (GUILayout.Button("💾 Generate Metadata", GUILayout.Height(30)))
        {
            GenerateMetadata();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    FormationMetadataComplete GenerateCompleteMetadata()
    {
        var metadata = new FormationMetadataComplete
        {
            id = projectId,
            title = projectTitle,
            description = projectDescription,
            version = projectVersion,
            duration = $"{durationMinutes} minutes", // Auto formatting
            difficulty = difficultyOptions[difficultyIndex], // Get from dropdown
            tags = new List<string>(tags),
            imageUrl = imageUrl,
            modules = new List<object>(),
            createdAt = includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "",
            updatedAt = includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : ""
        };
        
        // 🎯 SIMPLIFIED STRUCTURE: Unity contains objects directly
        try
        {
            if (!string.IsNullOrEmpty(unityContentJSON) && isUnityContentValid)
            {
                // Parse JSON directly to unity section
                metadata.unity = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(unityContentJSON);
            }
            else
            {
                // If no Unity content, create empty section
                metadata.unity = new Dictionary<string, Dictionary<string, object>>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing Unity content: {e.Message}");
            metadata.unity = new Dictionary<string, Dictionary<string, object>>();
        }
        
        return metadata;
    }
    
    void ShowJSONPreview()
    {
        var metadata = GenerateCompleteMetadata();
        string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        
        MetadataPreviewWindow.ShowWindow(json);
    }
    
    void GenerateMetadata()
    {
        // Create StreamingAssets folder if it doesn't exist
        string streamingAssetsPath = Application.streamingAssetsPath;
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
        }
        
        var metadata = GenerateCompleteMetadata();
        string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        
        string fileName = $"{projectId}-metadata.json";
        string fullPath = Path.Combine(streamingAssetsPath, fileName);
        
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Generation Successful", 
            $"Metadata generated successfully!\n\n" +
            $"📁 File: {fileName}\n" +
            $"📍 Location: StreamingAssets/\n" +
            $"🎯 File will be automatically included in Unity build.", 
            "Perfect!");
            
        // Mark as loaded for next opening
        currentLoadedFile = fileName;
        hasLoadedExistingJSON = true;
    }
    
    
    void OnDisable()
    {
        SaveSettings();
    }
}

// Preview window
public class MetadataPreviewWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string jsonContent;
    
    public static void ShowWindow(string json)
    {
        MetadataPreviewWindow window = GetWindow<MetadataPreviewWindow>("JSON Preview");
        window.jsonContent = json;
        window.minSize = new Vector2(500, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("📋 Metadata JSON Preview", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy JSON"))
        {
            EditorGUIUtility.systemCopyBuffer = jsonContent;
            ShowNotification(new GUIContent("JSON copied to clipboard!"));
        }
        if (GUILayout.Button("💾 Save as..."))
        {
            string path = EditorUtility.SaveFilePanel("Save JSON", "", "metadata", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, jsonContent);
                ShowNotification(new GUIContent($"Saved: {path}"));
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}

#endif