using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR

public class WiseTwinEditor : EditorWindow
{
    // Centralized data container
    private WiseTwin.Editor.WiseTwinEditorData data;
    private string settingsFilePath;

    // UI State
    private int selectedTab = 0;
    private string[] tabNames = { "General Settings", "Metadata Config", "Scenario Configuration", "Dialogue", "Video" };
    
    
    [MenuItem("WiseTwin/WiseTwin Editor")]
    public static void ShowWindow()
    {
        WiseTwinEditor window = GetWindow<WiseTwinEditor>("WiseTwin Editor");
        window.minSize = new Vector2(700, 600);
        window.Show();
    }
    
    void OnEnable()
    {
        // Initialize data container
        data = new WiseTwin.Editor.WiseTwinEditorData();

        settingsFilePath = Path.Combine(Application.persistentDataPath, "WiseTwinSettings.json");
        LoadSettings();
        InitializeSceneId();

        // Synchroniser automatiquement avec WiseTwinManager au chargement
        EditorApplication.delayCall += () =>
        {
            SyncWithSceneManager();
        };
        LoadExistingJSONContent();
        InitializeUnityContent();
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
                    data.useLocalMode = (bool)settingsDict["useLocalMode"];
                if (settingsDict.ContainsKey("azureApiUrl"))
                    data.azureApiUrl = settingsDict["azureApiUrl"].ToString();
                if (settingsDict.ContainsKey("containerId"))
                    data.containerId = settingsDict["containerId"].ToString();
                if (settingsDict.ContainsKey("buildType"))
                    data.buildType = settingsDict["buildType"].ToString();

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
                ["useLocalMode"] = data.useLocalMode,
                ["azureApiUrl"] = data.azureApiUrl,
                ["containerId"] = data.containerId,
                ["buildType"] = data.buildType
            };

            string jsonContent = JsonConvert.SerializeObject(settingsDict, Formatting.Indented);
            File.WriteAllText(settingsFilePath, jsonContent);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WiseTwin] Could not save settings: {e.Message}");
        }
    }
    
    
    void InitializeSceneId()
    {
        // Get current scene name
        data.sceneId = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(data.sceneId))
        {
            data.sceneId = "default-scene";
        }
    }
    
    void LoadExistingJSONContent()
    {
        string targetFileName = $"{data.sceneId}-metadata.json";

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
                data.currentLoadedFile = Path.GetFileName(foundPath);
                data.hasLoadedExistingJSON = true;

            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error loading JSON: {e.Message}");
                data.hasLoadedExistingJSON = false;
            }
        }
        else
        {
            data.hasLoadedExistingJSON = false;
        }
    }
    
    void ParseExistingJSON(string jsonContent)
    {
        try
        {
            var metadata = JsonConvert.DeserializeObject<FormationMetadataComplete>(jsonContent);

            // Load basic data - gérer les objets multilingues
            if (metadata.title != null)
            {
                data.projectTitleEN = metadata.title.en ?? "Training Test";
                data.projectTitleFR = metadata.title.fr ?? "Formation Test";
            }
            if (metadata.description != null)
            {
                data.projectDescriptionEN = metadata.description.en ?? "Training description";
                data.projectDescriptionFR = metadata.description.fr ?? "Description de la formation";
            }
            if (!string.IsNullOrEmpty(metadata.version)) data.projectVersion = metadata.version;
            if (!string.IsNullOrEmpty(metadata.imageUrl)) data.imageUrl = metadata.imageUrl;

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
                data.tags = new List<string>(metadata.tags);

            // 🎯 IMPORTANT: Extract Unity section (object content)
            if (metadata.unity != null)
            {
                // Convert Unity content directly to formatted JSON
                data.unityContentJSON = JsonConvert.SerializeObject(metadata.unity, Formatting.Indented);
                ValidateUnityContent();

            }
            else
            {
                InitializeUnityContent();
            }

            // 🎯 NEW: Load scenarios if present
            if (metadata.scenarios != null && metadata.scenarios.Count > 0)
            {
                LoadScenariosFromJSON(metadata.scenarios);
                Debug.Log($"✅ Loaded {data.scenarios.Count} scenarios from metadata");
            }

            // 🎬 Load video triggers if present
            if (metadata.videoTriggers != null && metadata.videoTriggers.Count > 0)
            {
                LoadVideoTriggersFromJSON(metadata.videoTriggers);
                Debug.Log($"✅ Loaded {data.videoTriggers.Count} video triggers from metadata");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error parsing existing JSON: {e.Message}");
            InitializeUnityContent();
        }
    }

    void LoadVideoTriggersFromJSON(List<object> videoTriggersJSON)
    {
        data.videoTriggers.Clear();

        foreach (var triggerObj in videoTriggersJSON)
        {
            try
            {
                var triggerDict = triggerObj as Dictionary<string, object>;
                if (triggerDict == null)
                {
                    var jObject = Newtonsoft.Json.Linq.JObject.FromObject(triggerObj);
                    triggerDict = jObject.ToObject<Dictionary<string, object>>();
                }

                var trigger = new WiseTwin.Editor.VideoTriggerConfiguration();

                // Load target object name
                if (triggerDict.ContainsKey("targetObjectName"))
                {
                    trigger.targetObjectName = triggerDict["targetObjectName"]?.ToString() ?? "";
                }

                // Load video URLs
                if (triggerDict.ContainsKey("videoUrl"))
                {
                    var urlObj = triggerDict["videoUrl"];
                    Dictionary<string, object> urlDict = null;

                    if (urlObj is Newtonsoft.Json.Linq.JObject jUrlObj)
                    {
                        urlDict = jUrlObj.ToObject<Dictionary<string, object>>();
                    }
                    else if (urlObj is Dictionary<string, object> dict)
                    {
                        urlDict = dict;
                    }

                    if (urlDict != null)
                    {
                        if (urlDict.ContainsKey("en"))
                            trigger.videoUrlEN = urlDict["en"]?.ToString() ?? "";
                        if (urlDict.ContainsKey("fr"))
                            trigger.videoUrlFR = urlDict["fr"]?.ToString() ?? "";
                    }
                }

                data.videoTriggers.Add(trigger);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to load video trigger: {e.Message}");
            }
        }
    }

    void LoadScenariosFromJSON(List<object> scenariosJSON)
    {
        data.scenarios.Clear();

        foreach (var scenarioObj in scenariosJSON)
        {
            try
            {
                var scenarioDict = scenarioObj as Dictionary<string, object>;
                if (scenarioDict == null)
                {
                    var jObject = Newtonsoft.Json.Linq.JObject.FromObject(scenarioObj);
                    scenarioDict = jObject.ToObject<Dictionary<string, object>>();
                }

                var scenario = new WiseTwin.Editor.ScenarioConfiguration();

                // Load basic fields
                if (scenarioDict.ContainsKey("id"))
                    scenario.id = scenarioDict["id"]?.ToString();

                if (scenarioDict.ContainsKey("type"))
                {
                    string typeStr = scenarioDict["type"]?.ToString();
                    if (Enum.TryParse<WiseTwin.Editor.ScenarioType>(typeStr, true, out var type))
                        scenario.type = type;
                }

                // Load content based on type
                switch (scenario.type)
                {
                    case WiseTwin.Editor.ScenarioType.Question:
                        scenario.questions.Clear();
                        if (scenarioDict.ContainsKey("question"))
                        {
                            // Single question
                            var questionData = new WiseTwin.Editor.QuestionScenarioData();
                            LoadQuestionDataFromJSON(questionData, scenarioDict["question"]);
                            scenario.questions.Add(questionData);
                        }
                        else if (scenarioDict.ContainsKey("questions"))
                        {
                            // Multiple questions
                            var questionsArray = scenarioDict["questions"] as Newtonsoft.Json.Linq.JArray;
                            if (questionsArray != null)
                            {
                                foreach (var questionObj in questionsArray)
                                {
                                    var questionData = new WiseTwin.Editor.QuestionScenarioData();
                                    LoadQuestionDataFromJSON(questionData, questionObj);
                                    scenario.questions.Add(questionData);
                                }
                            }
                        }
                        break;

                    case WiseTwin.Editor.ScenarioType.Procedure:
                        if (scenarioDict.ContainsKey("procedure"))
                            LoadProcedureDataFromJSON(scenario.procedureData, scenarioDict["procedure"]);
                        break;

                    case WiseTwin.Editor.ScenarioType.Text:
                        if (scenarioDict.ContainsKey("text"))
                            LoadTextDataFromJSON(scenario.textData, scenarioDict["text"]);
                        break;

                    case WiseTwin.Editor.ScenarioType.Dialogue:
                        if (scenarioDict.ContainsKey("dialogue"))
                            LoadDialogueDataFromJSON(scenario.dialogueData, scenarioDict["dialogue"]);
                        break;
                }

                data.scenarios.Add(scenario);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to load scenario: {e.Message}");
            }
        }
    }

    void LoadQuestionDataFromJSON(WiseTwin.Editor.QuestionScenarioData question, object questionObj)
    {
        var jObject = Newtonsoft.Json.Linq.JObject.FromObject(questionObj);
        var questionDict = jObject.ToObject<Dictionary<string, object>>();

        // Load question text
        if (questionDict.ContainsKey("questionText"))
        {
            var textDict = GetDictionary(questionDict["questionText"]);
            question.questionTextEN = GetString(textDict, "en");
            question.questionTextFR = GetString(textDict, "fr");
        }

        // Load options
        if (questionDict.ContainsKey("options"))
        {
            var optionsDict = GetDictionary(questionDict["options"]);
            question.optionsEN = GetStringList(optionsDict, "en");
            question.optionsFR = GetStringList(optionsDict, "fr");
        }

        // Load correct answers
        if (questionDict.ContainsKey("correctAnswers"))
        {
            var correctAnswersObj = questionDict["correctAnswers"];
            if (correctAnswersObj is Newtonsoft.Json.Linq.JArray jArray)
            {
                question.correctAnswers = jArray.ToObject<List<int>>();
            }
        }

        // Load flags
        if (questionDict.ContainsKey("isMultipleChoice"))
            question.isMultipleChoice = Convert.ToBoolean(questionDict["isMultipleChoice"]);

        // Load feedback
        if (questionDict.ContainsKey("feedback"))
        {
            var feedbackDict = GetDictionary(questionDict["feedback"]);
            question.feedbackEN = GetString(feedbackDict, "en");
            question.feedbackFR = GetString(feedbackDict, "fr");
        }

        if (questionDict.ContainsKey("incorrectFeedback"))
        {
            var feedbackDict = GetDictionary(questionDict["incorrectFeedback"]);
            question.incorrectFeedbackEN = GetString(feedbackDict, "en");
            question.incorrectFeedbackFR = GetString(feedbackDict, "fr");
        }

        // Load hint (reset to empty if not present)
        if (questionDict.ContainsKey("hint"))
        {
            var hintDict = GetDictionary(questionDict["hint"]);
            question.hintEN = GetString(hintDict, "en");
            question.hintFR = GetString(hintDict, "fr");
        }
        else
        {
            question.hintEN = "";
            question.hintFR = "";
        }
    }

    void LoadProcedureDataFromJSON(WiseTwin.Editor.ProcedureScenarioData procedure, object procedureObj)
    {
        var jObject = Newtonsoft.Json.Linq.JObject.FromObject(procedureObj);
        var procedureDict = jObject.ToObject<Dictionary<string, object>>();

        // Load title
        if (procedureDict.ContainsKey("title"))
        {
            var titleDict = GetDictionary(procedureDict["title"]);
            procedure.titleEN = GetString(titleDict, "en");
            procedure.titleFR = GetString(titleDict, "fr");
        }

        // Load description
        if (procedureDict.ContainsKey("description"))
        {
            var descDict = GetDictionary(procedureDict["description"]);
            procedure.descriptionEN = GetString(descDict, "en");
            procedure.descriptionFR = GetString(descDict, "fr");
        }

        // Load steps
        if (procedureDict.ContainsKey("steps"))
        {
            LoadProcedureStepsFromJSON(procedure.steps, procedureDict["steps"]);
        }

        // Load fake objects
        if (procedureDict.ContainsKey("fakeObjects"))
        {
            LoadFakeObjectsFromJSON(procedure.fakeObjects, procedureDict["fakeObjects"]);
        }
    }

    void LoadProcedureStepsFromJSON(List<WiseTwin.Editor.ProcedureStep> steps, object stepsObj)
    {
        steps.Clear();

        if (stepsObj is Newtonsoft.Json.Linq.JArray jArray)
        {
            foreach (var stepObj in jArray)
            {
                var step = new WiseTwin.Editor.ProcedureStep();
                var stepDict = stepObj.ToObject<Dictionary<string, object>>();

                // Load text
                if (stepDict.ContainsKey("text"))
                {
                    var textDict = GetDictionary(stepDict["text"]);
                    step.textEN = GetString(textDict, "en");
                    step.textFR = GetString(textDict, "fr");
                }

                // Load target object name
                if (stepDict.ContainsKey("targetObjectName"))
                    step.targetObjectName = stepDict["targetObjectName"]?.ToString();

                // Load highlight color
                if (stepDict.ContainsKey("highlightColor"))
                {
                    string colorHex = stepDict["highlightColor"]?.ToString();
                    if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color color))
                        step.highlightColor = color;
                }

                // Load blinking
                if (stepDict.ContainsKey("useBlinking"))
                    step.useBlinking = Convert.ToBoolean(stepDict["useBlinking"]);

                // Load validation type (with backward compat for requireManualValidation)
                if (stepDict.ContainsKey("validationType"))
                {
                    string valTypeStr = stepDict["validationType"]?.ToString();
                    if (Enum.TryParse<WiseTwin.Editor.ValidationType>(valTypeStr, true, out var valType))
                        step.validationType = valType;
                }
                else if (stepDict.ContainsKey("requireManualValidation"))
                {
                    step.validationType = Convert.ToBoolean(stepDict["requireManualValidation"])
                        ? WiseTwin.Editor.ValidationType.Manual
                        : WiseTwin.Editor.ValidationType.Click;
                }

                // Load zone object name
                if (stepDict.ContainsKey("zoneObjectName"))
                    step.zoneObjectName = stepDict["zoneObjectName"]?.ToString();

                // Load image paths
                if (stepDict.ContainsKey("imagePath"))
                {
                    var imagePathDict = GetDictionary(stepDict["imagePath"]);
                    step.imagePathEN = GetString(imagePathDict, "en");
                    step.imagePathFR = GetString(imagePathDict, "fr");
                    // Note: Actual Sprite objects need to be loaded manually in editor from these paths
                }

                // Load hint (reset to empty if not present)
                if (stepDict.ContainsKey("hint"))
                {
                    var hintDict = GetDictionary(stepDict["hint"]);
                    step.hintEN = GetString(hintDict, "en");
                    step.hintFR = GetString(hintDict, "fr");
                }
                else
                {
                    step.hintEN = "";
                    step.hintFR = "";
                }

                // NEW: Load fake objects for this step
                if (stepDict.ContainsKey("fakeObjects"))
                {
                    LoadFakeObjectsFromJSON(step.fakeObjects, stepDict["fakeObjects"]);
                }

                steps.Add(step);
            }
        }
    }

    void LoadFakeObjectsFromJSON(List<WiseTwin.Editor.FakeObject> fakeObjects, object fakeObjectsObj)
    {
        fakeObjects.Clear();

        if (fakeObjectsObj is Newtonsoft.Json.Linq.JArray jArray)
        {
            foreach (var fakeObj in jArray)
            {
                var fake = new WiseTwin.Editor.FakeObject();
                var fakeDict = fakeObj.ToObject<Dictionary<string, object>>();

                // Load object name
                if (fakeDict.ContainsKey("objectName"))
                    fake.fakeObjectName = fakeDict["objectName"]?.ToString();

                // Load error message
                if (fakeDict.ContainsKey("errorMessage"))
                {
                    var errorDict = GetDictionary(fakeDict["errorMessage"]);
                    fake.errorMessageEN = GetString(errorDict, "en");
                    fake.errorMessageFR = GetString(errorDict, "fr");
                }

                fakeObjects.Add(fake);
            }
        }
    }

    void LoadTextDataFromJSON(WiseTwin.Editor.TextScenarioData text, object textObj)
    {
        var jObject = Newtonsoft.Json.Linq.JObject.FromObject(textObj);
        var textDict = jObject.ToObject<Dictionary<string, object>>();

        // Load title
        if (textDict.ContainsKey("title"))
        {
            var titleDict = GetDictionary(textDict["title"]);
            text.titleEN = GetString(titleDict, "en");
            text.titleFR = GetString(titleDict, "fr");
        }

        // Load content
        if (textDict.ContainsKey("content"))
        {
            var contentDict = GetDictionary(textDict["content"]);
            text.contentEN = GetString(contentDict, "en");
            text.contentFR = GetString(contentDict, "fr");
        }
    }

    void LoadDialogueDataFromJSON(WiseTwin.Editor.DialogueScenarioData dialogue, object dialogueObj)
    {
        var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dialogueObj);
        var dialogueDict = jObject.ToObject<Dictionary<string, object>>();

        // Load title
        if (dialogueDict.ContainsKey("title"))
        {
            var titleDict = GetDictionary(dialogueDict["title"]);
            dialogue.titleEN = GetString(titleDict, "en");
            dialogue.titleFR = GetString(titleDict, "fr");
        }

        // Store the entire dialogue JSON for the graph editor
        dialogue.graphDataJSON = jObject.ToString(Formatting.Indented);

        // Generate a dialogue ID if not already set
        if (string.IsNullOrEmpty(dialogue.dialogueId))
        {
            dialogue.dialogueId = $"dialogue_{data.dialogues.Count + 1}";
        }

        // Also add to the dialogues list if not already there
        bool exists = false;
        foreach (var d in data.dialogues)
        {
            if (d.dialogueId == dialogue.dialogueId)
            {
                exists = true;
                // Update existing
                d.titleEN = dialogue.titleEN;
                d.titleFR = dialogue.titleFR;
                d.graphDataJSON = dialogue.graphDataJSON;
                break;
            }
        }
        if (!exists)
        {
            data.dialogues.Add(new WiseTwin.Editor.DialogueScenarioData
            {
                dialogueId = dialogue.dialogueId,
                titleEN = dialogue.titleEN,
                titleFR = dialogue.titleFR,
                graphDataJSON = dialogue.graphDataJSON
            });
        }
    }

    // Helper methods for JSON parsing
    Dictionary<string, object> GetDictionary(object obj)
    {
        if (obj is Dictionary<string, object> dict)
            return dict;

        if (obj is Newtonsoft.Json.Linq.JObject jObj)
            return jObj.ToObject<Dictionary<string, object>>();

        return new Dictionary<string, object>();
    }

    string GetString(Dictionary<string, object> dict, string key)
    {
        return dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";
    }

    List<string> GetStringList(Dictionary<string, object> dict, string key)
    {
        if (!dict.ContainsKey(key))
            return new List<string>();

        var obj = dict[key];
        if (obj is Newtonsoft.Json.Linq.JArray jArray)
            return jArray.ToObject<List<string>>();

        return new List<string>();
    }
    
    void ParseDurationFromString(string durationStr)
    {
        try
        {
            // Extract numbers from string (e.g., "30 minutes" -> 30)
            string numbersOnly = System.Text.RegularExpressions.Regex.Match(durationStr, @"\d+").Value;
            if (!string.IsNullOrEmpty(numbersOnly))
            {
                data.durationMinutes = int.Parse(numbersOnly);
            }
        }
        catch
        {
            data.durationMinutes = 30; // default value
        }
    }

    void ParseDifficultyFromString(string difficultyStr)
    {
        for (int i = 0; i < data.difficultyOptions.Length; i++)
        {
            if (string.Equals(data.difficultyOptions[i], difficultyStr, System.StringComparison.OrdinalIgnoreCase))
            {
                data.difficultyIndex = i;
                return;
            }
        }
        data.difficultyIndex = 1; // Default "Intermediate"
    }
    
    void InitializeUnityContent()
    {
        if (string.IsNullOrEmpty(data.unityContentJSON))
        {
            data.unityContentJSON = JsonConvert.SerializeObject(new Dictionary<string, object>(), Formatting.Indented);
        }
        ValidateUnityContent();
    }

    void ValidateUnityContent()
    {
        try
        {
            if (!string.IsNullOrEmpty(data.unityContentJSON))
            {
                JsonConvert.DeserializeObject(data.unityContentJSON);
                data.isUnityContentValid = true;
            }
        }
        catch
        {
            data.isUnityContentValid = false;
        }
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawLoadedFileInfo();
        DrawTabs();

        data.scrollPosition = EditorGUILayout.BeginScrollView(data.scrollPosition);

        switch (selectedTab)
        {
            case 0:
                WiseTwin.Editor.WiseTwinEditorGeneralTab.Draw(data, this);
                break;
            case 1:
                WiseTwin.Editor.WiseTwinEditorMetadataTab.Draw(data);
                break;
            case 2:
                WiseTwin.Editor.WiseTwinEditorScenariosTab.Draw(data);
                break;
            case 3:
                WiseTwin.Editor.WiseTwinEditorDialogueTab.Draw(data);
                break;
            case 4:
                WiseTwin.Editor.WiseTwinEditorVideoTab.Draw(data);
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
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Centralized management for WiseTwin Unity projects", EditorStyles.helpBox);
        EditorGUILayout.Space();
    }
    
    void DrawLoadedFileInfo()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"🎯 Scene: {data.sceneId} (current active scene)", EditorStyles.boldLabel);

        if (data.hasLoadedExistingJSON)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Loaded file: {data.currentLoadedFile}", EditorStyles.helpBox);
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

    // ============================================================
    // Tab drawing methods have been extracted to separate files:
    // - WiseTwinEditorGeneralTab.cs
    // - WiseTwinEditorMetadataTab.cs
    // - WiseTwinEditorScenariosTab.cs
    // ============================================================

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
            id = data.sceneId,
            title = new LocalizedString(data.projectTitleEN, data.projectTitleFR),
            description = new LocalizedString(data.projectDescriptionEN, data.projectDescriptionFR),
            version = data.projectVersion,
            duration = $"{data.durationMinutes} minutes", // Auto formatting
            difficulty = data.difficultyOptions[data.difficultyIndex], // Get from dropdown (déjà en français)
            tags = new List<string>(data.tags),
            imageUrl = data.imageUrl,
            modules = new List<object>(),
            createdAt = data.includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "",
            updatedAt = data.includeTimestamp ? System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : ""
        };

        // 🎯 SIMPLIFIED STRUCTURE: Unity contains objects directly (legacy)
        try
        {
            if (!string.IsNullOrEmpty(data.unityContentJSON) && data.isUnityContentValid)
            {
                // Parse JSON directly to unity section
                metadata.unity = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(data.unityContentJSON);
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

        // 🎯 NEW: Convert scenarios to JSON format
        if (data.scenarios != null && data.scenarios.Count > 0)
        {
            metadata.scenarios = ConvertScenariosToJSON();

            // Add default settings
            metadata.settings = new Dictionary<string, object>
            {
                ["allowPause"] = true,
                ["showTimer"] = true,
                ["showProgress"] = true
            };
        }

        // 🎬 Convert video triggers to JSON format
        if (data.videoTriggers != null && data.videoTriggers.Count > 0)
        {
            metadata.videoTriggers = ConvertVideoTriggersToJSON();
        }

        return metadata;
    }

    List<object> ConvertVideoTriggersToJSON()
    {
        var videoTriggersJSON = new List<object>();

        foreach (var trigger in data.videoTriggers)
        {
            // Skip if no object name
            if (string.IsNullOrEmpty(trigger.targetObjectName))
                continue;

            // Skip if no URLs
            if (string.IsNullOrEmpty(trigger.videoUrlEN) && string.IsNullOrEmpty(trigger.videoUrlFR))
                continue;

            videoTriggersJSON.Add(new Dictionary<string, object>
            {
                ["targetObjectName"] = trigger.targetObjectName,
                ["videoUrl"] = new Dictionary<string, string>
                {
                    ["en"] = trigger.videoUrlEN ?? "",
                    ["fr"] = trigger.videoUrlFR ?? ""
                }
            });
        }

        return videoTriggersJSON;
    }

    List<object> ConvertScenariosToJSON()
    {
        var scenariosJSON = new List<object>();

        foreach (var scenario in data.scenarios)
        {
            var scenarioDict = new Dictionary<string, object>
            {
                ["id"] = scenario.id,
                ["type"] = scenario.type.ToString().ToLower()
            };

            // Add content based on type
            switch (scenario.type)
            {
                case WiseTwin.Editor.ScenarioType.Question:
                    // Export as "question" if single, "questions" if multiple
                    if (scenario.questions.Count == 1)
                    {
                        scenarioDict["question"] = ConvertQuestionDataToJSON(scenario.questions[0]);
                    }
                    else if (scenario.questions.Count > 1)
                    {
                        var questionsArray = new List<object>();
                        foreach (var q in scenario.questions)
                        {
                            questionsArray.Add(ConvertQuestionDataToJSON(q));
                        }
                        scenarioDict["questions"] = questionsArray;
                    }
                    break;

                case WiseTwin.Editor.ScenarioType.Procedure:
                    scenarioDict["procedure"] = ConvertProcedureDataToJSON(scenario.procedureData);
                    break;

                case WiseTwin.Editor.ScenarioType.Text:
                    scenarioDict["text"] = ConvertTextDataToJSON(scenario.textData);
                    break;

                case WiseTwin.Editor.ScenarioType.Dialogue:
                    scenarioDict["dialogue"] = ConvertDialogueDataToJSON(scenario.dialogueData);
                    break;
            }

            scenariosJSON.Add(scenarioDict);
        }

        return scenariosJSON;
    }

    Dictionary<string, object> ConvertQuestionDataToJSON(WiseTwin.Editor.QuestionScenarioData question)
    {
        var questionDict = new Dictionary<string, object>
        {
            ["questionText"] = new Dictionary<string, string>
            {
                ["en"] = question.questionTextEN,
                ["fr"] = question.questionTextFR
            },
            ["options"] = new Dictionary<string, object>
            {
                ["en"] = new List<string>(question.optionsEN),
                ["fr"] = new List<string>(question.optionsFR)
            },
            ["correctAnswers"] = new List<int>(question.correctAnswers),
            ["isMultipleChoice"] = question.isMultipleChoice
        };

        // Add feedback if provided
        if (!string.IsNullOrEmpty(question.feedbackEN) || !string.IsNullOrEmpty(question.feedbackFR))
        {
            questionDict["feedback"] = new Dictionary<string, string>
            {
                ["en"] = question.feedbackEN,
                ["fr"] = question.feedbackFR
            };
        }

        if (!string.IsNullOrEmpty(question.incorrectFeedbackEN) || !string.IsNullOrEmpty(question.incorrectFeedbackFR))
        {
            questionDict["incorrectFeedback"] = new Dictionary<string, string>
            {
                ["en"] = question.incorrectFeedbackEN,
                ["fr"] = question.incorrectFeedbackFR
            };
        }

        // Add hint if provided
        if (!string.IsNullOrEmpty(question.hintEN) || !string.IsNullOrEmpty(question.hintFR))
        {
            questionDict["hint"] = new Dictionary<string, string>
            {
                ["en"] = question.hintEN,
                ["fr"] = question.hintFR
            };
        }

        return questionDict;
    }

    Dictionary<string, object> ConvertProcedureDataToJSON(WiseTwin.Editor.ProcedureScenarioData procedure)
    {
        var procedureDict = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, string>
            {
                ["en"] = procedure.titleEN,
                ["fr"] = procedure.titleFR
            },
            ["description"] = new Dictionary<string, string>
            {
                ["en"] = procedure.descriptionEN,
                ["fr"] = procedure.descriptionFR
            },
            ["steps"] = ConvertProcedureStepsToJSON(procedure.steps)
        };

        // Add fake objects if any
        if (procedure.fakeObjects != null && procedure.fakeObjects.Count > 0)
        {
            procedureDict["fakeObjects"] = ConvertFakeObjectsToJSON(procedure.fakeObjects);
        }

        return procedureDict;
    }

    List<object> ConvertProcedureStepsToJSON(List<WiseTwin.Editor.ProcedureStep> steps)
    {
        var stepsJSON = new List<object>();

        foreach (var step in steps)
        {
            var stepDict = new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, string>
                {
                    ["en"] = step.textEN,
                    ["fr"] = step.textFR
                },
                ["targetObjectName"] = step.targetObjectName,
                ["highlightColor"] = ColorToHex(step.highlightColor),
                ["useBlinking"] = step.useBlinking,
                ["validationType"] = step.validationType.ToString().ToLower()
            };

            // Add zone object name if validation type is Zone
            if (step.validationType == WiseTwin.Editor.ValidationType.Zone && !string.IsNullOrEmpty(step.zoneObjectName))
            {
                stepDict["zoneObjectName"] = step.zoneObjectName;
            }

            // Add image paths if they exist
            if (!string.IsNullOrEmpty(step.imagePathEN) || !string.IsNullOrEmpty(step.imagePathFR))
            {
                stepDict["imagePath"] = new Dictionary<string, string>
                {
                    ["en"] = step.imagePathEN ?? "",
                    ["fr"] = step.imagePathFR ?? ""
                };
            }

            // Note: Hints removed for procedures - not exported to JSON anymore

            // NEW: Add fake objects for this step if any
            if (step.fakeObjects != null && step.fakeObjects.Count > 0)
            {
                stepDict["fakeObjects"] = ConvertFakeObjectsToJSON(step.fakeObjects);
            }

            stepsJSON.Add(stepDict);
        }

        return stepsJSON;
    }

    List<object> ConvertFakeObjectsToJSON(List<WiseTwin.Editor.FakeObject> fakeObjects)
    {
        var fakeObjectsJSON = new List<object>();

        foreach (var fake in fakeObjects)
        {
            if (string.IsNullOrEmpty(fake.fakeObjectName))
                continue;

            fakeObjectsJSON.Add(new Dictionary<string, object>
            {
                ["objectName"] = fake.fakeObjectName,
                ["errorMessage"] = new Dictionary<string, string>
                {
                    ["en"] = fake.errorMessageEN,
                    ["fr"] = fake.errorMessageFR
                }
            });
        }

        return fakeObjectsJSON;
    }

    Dictionary<string, object> ConvertTextDataToJSON(WiseTwin.Editor.TextScenarioData text)
    {
        return new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, string>
            {
                ["en"] = text.titleEN,
                ["fr"] = text.titleFR
            },
            ["content"] = new Dictionary<string, string>
            {
                ["en"] = text.contentEN,
                ["fr"] = text.contentFR
            }
        };
    }

    Dictionary<string, object> ConvertDialogueDataToJSON(WiseTwin.Editor.DialogueScenarioData dialogue)
    {
        // If we have graph data JSON from the visual editor, parse and use it
        if (!string.IsNullOrEmpty(dialogue.graphDataJSON))
        {
            try
            {
                var graphData = JsonConvert.DeserializeObject<Dictionary<string, object>>(dialogue.graphDataJSON);
                if (graphData != null)
                    return graphData;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WiseTwinEditor] Failed to parse dialogue graph JSON: {e.Message}");
            }
        }

        // Fallback: create a minimal dialogue structure
        return new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, string>
            {
                ["en"] = dialogue.titleEN,
                ["fr"] = dialogue.titleFR
            },
            ["startNodeId"] = "node_001",
            ["nodes"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["id"] = "node_001",
                    ["type"] = "start",
                    ["nextNodeId"] = "node_002"
                },
                new Dictionary<string, object>
                {
                    ["id"] = "node_002",
                    ["type"] = "end"
                }
            }
        };
    }

    string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
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

        string fileName = $"{data.sceneId}-metadata.json";
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
        data.currentLoadedFile = fileName;
        data.hasLoadedExistingJSON = true;
    }
    
    
    async void DownloadMetadataFromAPI()
    {
        if (string.IsNullOrEmpty(data.azureApiUrl) || string.IsNullOrEmpty(data.containerId))
        {
            EditorUtility.DisplayDialog("Error",
                "Please configure API URL and Container ID first!",
                "OK");
            return;
        }

        // Construire l'URL avec les paramètres
        string url = $"{data.azureApiUrl}?buildName={UnityEngine.Networking.UnityWebRequest.EscapeURL(data.sceneId)}" +
                     $"&buildType={UnityEngine.Networking.UnityWebRequest.EscapeURL(data.buildType)}" +
                     $"&containerId={UnityEngine.Networking.UnityWebRequest.EscapeURL(data.containerId)}";

        EditorUtility.DisplayProgressBar("Downloading Metadata", "Connecting to API...", 0.1f);

        try
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.Timeout = System.TimeSpan.FromSeconds(30);

                Debug.Log($"📥 Downloading from: {url}");

                var response = await client.GetAsync(url);

                EditorUtility.DisplayProgressBar("Downloading Metadata", "Receiving data...", 0.5f);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    // Parser la réponse API
                    var apiResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                    string metadataJson;

                    // Si l'API retourne {success: true, data: {...}}
                    if (apiResponse.ContainsKey("success") && apiResponse.ContainsKey("data"))
                    {
                        metadataJson = JsonConvert.SerializeObject(apiResponse["data"], Formatting.Indented);
                    }
                    else
                    {
                        // Sinon on prend la réponse directement
                        metadataJson = jsonContent;
                    }

                    // Sauvegarder dans StreamingAssets
                    string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                    if (!Directory.Exists(streamingAssetsPath))
                    {
                        Directory.CreateDirectory(streamingAssetsPath);
                    }

                    string fileName = $"{data.sceneId}-metadata.json";
                    string filePath = Path.Combine(streamingAssetsPath, fileName);

                    File.WriteAllText(filePath, metadataJson);

                    EditorUtility.DisplayProgressBar("Downloading Metadata", "Saved to StreamingAssets!", 1f);

                    AssetDatabase.Refresh();

                    Debug.Log($"✅ Metadata downloaded and saved to: {filePath}");

                    EditorUtility.ClearProgressBar();

                    // Afficher le succès et proposer de passer en mode Local
                    if (EditorUtility.DisplayDialog("Success",
                        $"Metadata downloaded successfully!\n\nSaved to: StreamingAssets/{fileName}\n\n" +
                        "Do you want to switch to Local Mode now?",
                        "Yes, switch to Local", "No"))
                    {
                        data.useLocalMode = true;
                        ApplySettingsToScene();
                    }
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    string error = $"API Error: {response.StatusCode} - {response.ReasonPhrase}";
                    Debug.LogError($"❌ {error}");

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Response: {responseContent}");

                    EditorUtility.DisplayDialog("Download Failed", error, "OK");
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"❌ Download failed: {e.Message}");
            EditorUtility.DisplayDialog("Download Failed",
                $"Failed to download metadata:\n{e.Message}",
                "OK");
        }
    }

    void SyncWithSceneManager()
    {
        // Synchroniser l'état de l'éditeur avec le WiseTwinManager de la scène
        WiseTwin.WiseTwinManager manager = FindFirstObjectByType<WiseTwin.WiseTwinManager>();
        if (manager != null)
        {
            SerializedObject managerSO = new SerializedObject(manager);
            SerializedProperty prodModeProp = managerSO.FindProperty("useProductionMode");
            if (prodModeProp != null)
            {
                // Lire l'état actuel du manager et synchroniser l'éditeur
                data.useLocalMode = !prodModeProp.boolValue;
                Debug.Log($"[WiseTwinEditor] Synchronisé avec WiseTwinManager: Mode {(data.useLocalMode ? "Local" : "Production")}");
            }
        }
    }

    void ApplyLocalModeToManager()
    {
        // Chercher le WiseTwinManager dans la scène
        WiseTwin.WiseTwinManager manager = FindFirstObjectByType<WiseTwin.WiseTwinManager>();
        if (manager != null)
        {
            // Appliquer le mode Production/Local
            SerializedObject managerSO = new SerializedObject(manager);
            SerializedProperty prodModeProp = managerSO.FindProperty("useProductionMode");
            if (prodModeProp != null)
            {
                prodModeProp.boolValue = !data.useLocalMode;  // Inverser car useLocalMode est l'opposé de useProductionMode
                managerSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);

                // Marquer la scène comme modifiée pour forcer la sauvegarde
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

                Debug.Log($"✅ WiseTwinManager: Mode {(data.useLocalMode ? "Local" : "Production")} appliqué automatiquement et scène marquée pour sauvegarde");
            }
        }
        else
        {
            Debug.LogWarning("❌ WiseTwinManager not found in scene! Please add WiseTwinManager to apply mode settings.");
        }
    }

    void ApplySettingsToScene()
    {
        // Appliquer le mode local/production
        ApplyLocalModeToManager();

        // Chercher le MetadataLoader dans la scène
        MetadataLoader loader = FindFirstObjectByType<MetadataLoader>();
        if (loader != null)
        {
            // Appliquer les paramètres API
            loader.useAzureStorageDirect = data.useAzureStorageDirect;
            loader.azureStorageUrl = data.azureStorageUrl;
            loader.apiBaseUrl = data.azureApiUrl;
            loader.containerId = data.containerId;
            loader.buildType = data.buildType;

            Debug.Log($"✅ MetadataLoader configured:");
            Debug.Log($"   - Mode: {(data.useLocalMode ? "Local" : "Production")}");
            Debug.Log($"   - Azure Direct: {data.useAzureStorageDirect}");
            if (data.useAzureStorageDirect)
            {
                Debug.Log($"   - Storage URL: {data.azureStorageUrl}");
            }
            else
            {
                Debug.Log($"   - API URL: {data.azureApiUrl}");
            }
            Debug.Log($"   - Container ID: {data.containerId}");
            Debug.Log($"   - Build Type: {data.buildType}");

            EditorUtility.SetDirty(loader);
        }
        else
        {
            Debug.LogWarning("❌ MetadataLoader not found in scene!");
        }

        // Sauvegarder les changements
        SaveSettings();

        EditorUtility.DisplayDialog("Success",
            $"Settings applied to scene!\n\nMode: {(data.useLocalMode ? "Local" : "Production")}\n" +
            $"API: {data.azureApiUrl}\n" +
            $"Container: {data.containerId}",
            "OK");
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