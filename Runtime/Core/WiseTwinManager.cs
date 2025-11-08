using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace WiseTwin
{
    /// <summary>
    /// Main entry point for WiseTwin training system.
    /// Centralizes access to MetadataLoader and TrainingCompletionNotifier.
    /// </summary>
    public class WiseTwinManager : MonoBehaviour
    {
        [Header("🎯 WiseTwin Manager")]
        [SerializeField, Tooltip("Enable debug logs for this component")]
        private bool enableDebugLogs = true;
        [SerializeField, Tooltip("Log prefix for easy filtering")]
        private string logPrefix = "[WiseTwinManager]";
        
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
        public string SceneName => GetSceneName();
        
        // Events
        public System.Action<Dictionary<string, object>> OnMetadataReady;
        public System.Action<string> OnMetadataError;
        public System.Action OnTrainingCompleted;

        // Player spawn tracking
        private Vector3 initialPlayerPosition;
        private Quaternion initialPlayerRotation;
        private bool playerSpawnPositionSaved = false;
        
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

            // Save player's initial spawn position
            SavePlayerSpawnPosition();
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
            var scenarios = metadataLoader?.GetScenarios();
            int count = scenarios?.Count ?? 0;
            DebugLog($"📦 Metadata loaded successfully. Scenarios: {count}");
            OnMetadataReady?.Invoke(metadata);
        }
        
        void OnMetadataLoadError(string error)
        {
            DebugLog($"❌ Metadata load error: {error}");
            OnMetadataError?.Invoke(error);
        }
        
        #region Public API - Easy access methods
        
        /// <summary>
        /// Get data for a specific Unity object (Legacy - use scenario-based system instead)
        /// </summary>
        /// <param name="objectId">Unity object identifier</param>
        /// <returns>Object data dictionary or null if not found</returns>
        [System.Obsolete("This method is for legacy InteractableObject system. Use ProgressionManager with scenario-based metadata instead.")]
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
        /// Get typed content for a specific Unity object (Legacy - use scenario-based system instead)
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="objectId">Unity object identifier</param>
        /// <param name="contentKey">Optional content key within object</param>
        /// <returns>Typed content or null if not found</returns>
        [System.Obsolete("This method is for legacy InteractableObject system. Use ProgressionManager with scenario-based metadata instead.")]
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
        /// Get all available Unity object IDs (Legacy - use scenario-based system instead)
        /// </summary>
        /// <returns>List of object identifiers</returns>
        [System.Obsolete("This method is for legacy InteractableObject system. Use MetadataLoader.GetScenarios() for scenario-based training instead.")]
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
            
            DebugLog($"🎉 Training completed: {trainingName ?? SceneName}");
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
        
        /// <summary>
        /// Save the player's current position as the spawn point
        /// Called automatically on Start, but can be called manually to update
        /// </summary>
        public void SavePlayerSpawnPosition()
        {
            var player = FindFirstObjectByType<FirstPersonCharacter>();
            if (player != null)
            {
                initialPlayerPosition = player.transform.position;
                initialPlayerRotation = player.transform.rotation;
                playerSpawnPositionSaved = true;
                DebugLog($"💾 Player spawn position saved: {initialPlayerPosition}");
            }
            else
            {
                DebugLog("⚠️ Cannot save spawn position: FirstPersonCharacter not found in scene");
            }
        }

        /// <summary>
        /// Reset the player to their initial spawn position
        /// Useful if player gets stuck or wants to restart positioning
        /// </summary>
        public void ResetPlayerPosition()
        {
            if (!playerSpawnPositionSaved)
            {
                DebugLog("⚠️ Cannot reset player: spawn position not saved yet");
                SavePlayerSpawnPosition(); // Try to save it now
                return;
            }

            var player = FindFirstObjectByType<FirstPersonCharacter>();
            if (player != null)
            {
                var characterController = player.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    // Disable CharacterController to teleport properly
                    characterController.enabled = false;
                    player.transform.position = initialPlayerPosition;
                    player.transform.rotation = initialPlayerRotation;
                    characterController.enabled = true;

                    DebugLog($"↻ Player reset to spawn position: {initialPlayerPosition}");
                }
                else
                {
                    // No CharacterController, just move directly
                    player.transform.position = initialPlayerPosition;
                    player.transform.rotation = initialPlayerRotation;
                    DebugLog($"↻ Player reset to spawn position (no CharacterController): {initialPlayerPosition}");
                }
            }
            else
            {
                DebugLog("❌ Cannot reset player: FirstPersonCharacter not found in scene");
            }
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
            status.AppendLine($"Scene: {SceneName}");
            status.AppendLine($"MetadataLoader: {(metadataLoader != null ? "✅" : "❌")}");
            status.AppendLine($"CompletionNotifier: {(completionNotifier != null ? "✅" : "❌")}");
            status.AppendLine($"Test Mode: {(IsProductionMode() ? "❌ Production" : "✅ Local")}");
            status.AppendLine($"Metadata Loaded: {(IsMetadataLoaded ? "✅" : "❌")}");
            
            if (IsMetadataLoaded)
            {
                var scenarios = metadataLoader.GetScenarios();
                if (scenarios != null && scenarios.Count > 0)
                {
                    status.AppendLine($"Scenarios: {scenarios.Count}");
                    foreach (var scenario in scenarios)
                    {
                        status.AppendLine($"  • {scenario.id} ({scenario.type})");
                    }
                }
                else
                {
                    status.AppendLine("Scenarios: None");
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
        /// Get current scene name
        /// </summary>
        /// <returns>Scene name</returns>
        string GetSceneName()
        {
            // Use metadata loader's scene name if available
            if (metadataLoader != null && !string.IsNullOrEmpty(metadataLoader.SceneName))
            {
                return metadataLoader.SceneName;
            }

            // Fallback to current active scene
            string sceneName = SceneManager.GetActiveScene().name;
            return !string.IsNullOrEmpty(sceneName) ? sceneName : "default-scene";
        }
        
        #endregion
        
        /// <summary>
        /// Update settings on all components
        /// </summary>
        void UpdateComponentSettings()
        {
            // Les composants gèrent maintenant leurs propres settings
            // Cette méthode est conservée pour compatibilité future

            if (metadataLoader != null)
            {
                metadataLoader.UpdateSettingsFromManager();
            }
        }
        
        void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{logPrefix} {message}");
            }
        }

        /// <summary>
        /// Toggle debug logs for this component
        /// </summary>
        public void SetDebugEnabled(bool enabled)
        {
            enableDebugLogs = enabled;
            DebugLog($"Debug logs {(enabled ? "enabled" : "disabled")}");
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
            GUILayout.Label($"Scene: {SceneName}");
            GUILayout.Label($"Metadata: {(IsMetadataLoaded ? "✅ Loaded" : "❌ Loading...")}");
            
            if (IsMetadataLoaded)
            {
                var scenarios = metadataLoader?.GetScenarios();
                int count = scenarios?.Count ?? 0;
                GUILayout.Label($"Scenarios: {count}");
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