using UnityEngine;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Main entry point for WiseTwin training system.
    /// Centralizes access to MetadataLoader and TrainingCompletionNotifier.
    /// </summary>
    public class WiseTwinManager : MonoBehaviour
    {
        [Header("🎯 WiseTwin Manager")]
        [SerializeField, Tooltip("Enable debug logs for development")]
        private bool enableDebugLogs = true;
        
        [SerializeField, Tooltip("Use production mode (Azure API + web notifications)")]
        private bool useProductionMode = false;
        
        [Header("📋 References")]
        [SerializeField, Tooltip("MetadataLoader component (auto-found if empty)")]
        private MetadataLoader metadataLoader;
        
        [SerializeField, Tooltip("TrainingCompletionNotifier component (auto-found if empty)")]
        private TrainingCompletionNotifier completionNotifier;
        
        
        // Singleton
        public static WiseTwinManager Instance { get; private set; }
        
        // Public Properties
        public MetadataLoader MetadataLoader => metadataLoader;
        public TrainingCompletionNotifier CompletionNotifier => completionNotifier;
        
        // Public Properties for settings
        public bool EnableDebugLogs => enableDebugLogs;
        public bool IsProductionMode() => useProductionMode;
        
        // Quick access properties
        public bool IsMetadataLoaded => metadataLoader != null && metadataLoader.IsLoaded;
        public string ProjectName => GetProjectName();
        
        // Events
        public System.Action<Dictionary<string, object>> OnMetadataReady;
        public System.Action<string> OnMetadataError;
        public System.Action OnTrainingCompleted;
        
        void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComponents();
            }
            else
            {
                DebugLog("WiseTwinManager instance already exists. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            DebugLog("🎯 WiseTwin Manager initialized successfully");
            
            // Subscribe to metadata loader events if available
            if (metadataLoader != null)
            {
                metadataLoader.OnMetadataLoaded += OnMetadataLoaded;
                metadataLoader.OnLoadError += OnMetadataLoadError;
            }
        }
        
        void InitializeComponents()
        {
            DebugLog("🔍 Searching for WiseTwin components...");
            
            // Find MetadataLoader
            if (metadataLoader == null)
            {
                metadataLoader = FindFirstObjectByType<MetadataLoader>();
                if (metadataLoader != null)
                {
                    DebugLog("✅ MetadataLoader found and linked");
                }
                else
                {
                    DebugLog("⚠️ MetadataLoader not found in scene");
                }
            }
            
            // Find TrainingCompletionNotifier
            if (completionNotifier == null)
            {
                completionNotifier = FindFirstObjectByType<TrainingCompletionNotifier>();
                if (completionNotifier != null)
                {
                    DebugLog("✅ TrainingCompletionNotifier found and linked");
                }
                else
                {
                    DebugLog("⚠️ TrainingCompletionNotifier not found in scene");
                }
            }
            
            // Update component settings
            UpdateComponentSettings();
        }
        
        void OnMetadataLoaded(Dictionary<string, object> metadata)
        {
            DebugLog($"📦 Metadata loaded successfully. Available objects: {GetAvailableObjectIds().Count}");
            OnMetadataReady?.Invoke(metadata);
        }
        
        void OnMetadataLoadError(string error)
        {
            DebugLog($"❌ Metadata load error: {error}");
            OnMetadataError?.Invoke(error);
        }
        
        #region Public API - Easy access methods
        
        /// <summary>
        /// Get data for a specific Unity object
        /// </summary>
        /// <param name="objectId">Unity object identifier</param>
        /// <returns>Object data dictionary or null if not found</returns>
        public Dictionary<string, object> GetDataForObject(string objectId)
        {
            if (metadataLoader == null)
            {
                DebugLog($"❌ Cannot get data for '{objectId}': MetadataLoader not available");
                return null;
            }
            
            return metadataLoader.GetDataForObject(objectId);
        }
        
        /// <summary>
        /// Get typed content for a specific Unity object
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="objectId">Unity object identifier</param>
        /// <param name="contentKey">Optional content key within object</param>
        /// <returns>Typed content or null if not found</returns>
        public T GetContentForObject<T>(string objectId, string contentKey = null) where T : class
        {
            if (metadataLoader == null)
            {
                DebugLog($"❌ Cannot get content for '{objectId}': MetadataLoader not available");
                return null;
            }
            
            return metadataLoader.GetContentForObject<T>(objectId, contentKey);
        }
        
        /// <summary>
        /// Get all available Unity object IDs
        /// </summary>
        /// <returns>List of object identifiers</returns>
        public List<string> GetAvailableObjectIds()
        {
            if (metadataLoader == null)
            {
                DebugLog("❌ Cannot get object IDs: MetadataLoader not available");
                return new List<string>();
            }
            
            return metadataLoader.GetAvailableObjectIds();
        }
        
        /// <summary>
        /// Get project metadata information
        /// </summary>
        /// <param name="key">Metadata key (title, description, version, etc.)</param>
        /// <returns>Metadata value or empty string</returns>
        public string GetProjectInfo(string key)
        {
            if (metadataLoader == null)
            {
                DebugLog($"❌ Cannot get project info '{key}': MetadataLoader not available");
                return "";
            }
            
            return metadataLoader.GetProjectInfo(key);
        }
        
        /// <summary>
        /// Complete the training and notify the web application
        /// </summary>
        /// <param name="trainingName">Optional training name</param>
        public void CompleteTraining(string trainingName = null)
        {
            if (completionNotifier == null)
            {
                DebugLog("❌ Cannot complete training: TrainingCompletionNotifier not available");
                return;
            }
            
            DebugLog($"🎉 Training completed: {trainingName ?? ProjectName}");
            completionNotifier.FormationCompleted(trainingName);
            OnTrainingCompleted?.Invoke();
        }
        
        /// <summary>
        /// Test training completion (development only)
        /// </summary>
        public void TestCompletion()
        {
            if (completionNotifier != null)
            {
                completionNotifier.TestCompletion();
            }
            else
            {
                DebugLog("❌ Cannot test completion: TrainingCompletionNotifier not available");
            }
        }
        
        /// <summary>
        /// Reload metadata from source
        /// </summary>
        public void ReloadMetadata()
        {
            if (metadataLoader == null)
            {
                DebugLog("❌ Cannot reload metadata: MetadataLoader not available");
                return;
            }
            
            DebugLog("🔄 Reloading metadata...");
            metadataLoader.ReloadMetadata();
        }
        
        #endregion
        
        #region Development Helpers
        
        /// <summary>
        /// Get system status for debugging
        /// </summary>
        /// <returns>Status information</returns>
        public string GetSystemStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine($"🎯 WiseTwin Manager Status");
            status.AppendLine($"Project: {ProjectName}");
            status.AppendLine($"MetadataLoader: {(metadataLoader != null ? "✅" : "❌")}");
            status.AppendLine($"CompletionNotifier: {(completionNotifier != null ? "✅" : "❌")}");
            status.AppendLine($"Test Mode: {(IsProductionMode() ? "❌ Production" : "✅ Local")}");
            status.AppendLine($"Metadata Loaded: {(IsMetadataLoaded ? "✅" : "❌")}");
            
            if (IsMetadataLoaded)
            {
                var objectIds = GetAvailableObjectIds();
                status.AppendLine($"Available Objects: {objectIds.Count}");
                foreach (var id in objectIds)
                {
                    status.AppendLine($"  • {id}");
                }
            }
            
            return status.ToString();
        }
        
        /// <summary>
        /// Force component refresh (useful after scene changes)
        /// </summary>
        public void RefreshComponents()
        {
            DebugLog("🔄 Refreshing WiseTwin components...");
            
            // Clear current references
            metadataLoader = null;
            completionNotifier = null;
            
            // Find components again
            InitializeComponents();
            
            // Re-subscribe to events
            if (metadataLoader != null)
            {
                metadataLoader.OnMetadataLoaded += OnMetadataLoaded;
                metadataLoader.OnLoadError += OnMetadataLoadError;
            }
        }
        
        /// <summary>
        /// Set preferred language for training content
        /// </summary>
        /// <param name="languageCode">Language code (e.g. "en", "fr")</param>
        public void SetPreferredLanguage(string languageCode)
        {
            PlayerPrefs.SetString("WiseTwin_Language", languageCode);
            PlayerPrefs.Save();
            DebugLog($"🌐 Language preference set to: {languageCode}");
        }
        
        /// <summary>
        /// Get preferred language for training content
        /// </summary>
        /// <returns>Language code or system default</returns>
        public string GetPreferredLanguage()
        {
            string savedLanguage = PlayerPrefs.GetString("WiseTwin_Language", "");
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                return savedLanguage;
            }
            
            // Fallback to system language
            SystemLanguage systemLang = Application.systemLanguage;
            switch (systemLang)
            {
                case SystemLanguage.French:
                    return "fr";
                default:
                    return "en";
            }
        }
        
        /// <summary>
        /// Set custom project name (overrides Unity's productName)
        /// </summary>
        /// <param name="projectName">Custom project name</param>
        public void SetCustomProjectName(string projectName)
        {
            PlayerPrefs.SetString("WiseTwin_ProjectName", projectName);
            PlayerPrefs.Save();
            DebugLog($"📋 Custom project name set to: {projectName}");
        }
        
        /// <summary>
        /// Get project name (custom or Unity's productName)
        /// </summary>
        /// <returns>Project name</returns>
        string GetProjectName()
        {
            // Try custom project name first
            string customName = PlayerPrefs.GetString("WiseTwin_ProjectName", "");
            if (!string.IsNullOrEmpty(customName))
            {
                return customName;
            }
            
            // Use metadata loader's name if available
            if (metadataLoader != null && !string.IsNullOrEmpty(metadataLoader.ProjectName))
            {
                return metadataLoader.ProjectName;
            }
            
            // Fallback to Unity's product name
            return !string.IsNullOrEmpty(Application.productName) ? Application.productName : "unity-project";
        }
        
        #endregion
        
        /// <summary>
        /// Update settings on all components
        /// </summary>
        void UpdateComponentSettings()
        {
            if (completionNotifier != null)
            {
                completionNotifier.UpdateSettingsFromManager();
            }
            
            if (metadataLoader != null)
            {
                metadataLoader.UpdateSettingsFromManager();
            }
        }
        
        void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[WiseTwinManager] {message}");
            }
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (metadataLoader != null)
            {
                metadataLoader.OnMetadataLoaded -= OnMetadataLoaded;
                metadataLoader.OnLoadError -= OnMetadataLoadError;
            }
        }
        
        #region Inspector GUI (Development)
        
        void OnGUI()
        {
            if (!enableDebugLogs) return;
            
            // Simple debug overlay
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
            boldStyle.fontStyle = FontStyle.Bold;
            
            GUILayout.Label("🎯 WiseTwin Manager", boldStyle);
            GUILayout.Label($"Project: {ProjectName}");
            GUILayout.Label($"Metadata: {(IsMetadataLoaded ? "✅ Loaded" : "❌ Loading...")}");
            
            if (IsMetadataLoaded)
            {
                GUILayout.Label($"Objects: {GetAvailableObjectIds().Count}");
                GUILayout.Label($"Title: {GetProjectInfo("title")}");
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("🎉 Test Completion"))
            {
                TestCompletion();
            }
            
            if (GUILayout.Button("🔄 Reload Data"))
            {
                ReloadMetadata();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
}