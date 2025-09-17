using UnityEngine;
using System.Runtime.InteropServices;

namespace WiseTwin
{
    /// <summary>
    /// Handles training completion notifications to web applications.
    /// Includes debug testing capabilities with individual logging control.
    /// </summary>
    public class TrainingCompletionNotifier : MonoBehaviour
    {
        [Header("🔧 Debug Settings")]
        [SerializeField, Tooltip("Enable debug logs for this component")]
        private bool enableDebugLogs = true;
        [SerializeField, Tooltip("Log prefix for easy filtering")]
        private string logPrefix = "[TrainingCompletionNotifier]";

        // JavaScript interop for WebGL
        [DllImport("__Internal")]
        private static extern void NotifyFormationCompleted();
        
        void Start()
        {
            // Get settings from WiseTwinManager
            UpdateSettingsFromManager();
        }
        
        
        /// <summary>
        /// Main method to complete training and notify web application
        /// </summary>
        /// <param name="trainingName">Optional training name</param>
        public void FormationCompleted(string trainingName = null)
        {
            if (ShouldUseProductionMode())
            {
                // Production mode: Send to JavaScript
                #if UNITY_WEBGL && !UNITY_EDITOR
                    NotifyFormationCompleted();
                    LogDebug("📡 Training completion sent to web application");
                #else
                    LogDebug("⚠️ Production mode but not in WebGL build - notification not sent");
                #endif
            }
            else
            {
                // Local mode: Debug log only
                LogDebug($"🧪 Local Mode: Training '{trainingName ?? GetProjectName()}' completed");
            }
        }
        
        /// <summary>
        /// Test completion notification (development only)
        /// </summary>
        public void TestCompletion()
        {
            LogDebug("🧪 Testing completion notification");
            FormationCompleted("Test Training");
        }
        
        /// <summary>
        /// Update settings from WiseTwinManager
        /// </summary>
        public void UpdateSettingsFromManager()
        {
            // Settings are now managed entirely by WiseTwinManager
            // This method exists for future extensibility
        }
        
        bool ShouldUseProductionMode()
        {
            if (WiseTwinManager.Instance != null)
            {
                return WiseTwinManager.Instance.IsProductionMode();
            }
            
            // Fallback: check if we're in WebGL build
            #if UNITY_WEBGL && !UNITY_EDITOR
                return true;
            #else
                return false;
            #endif
        }
        
        string GetProjectName()
        {
            if (WiseTwinManager.Instance != null)
            {
                return WiseTwinManager.Instance.ProjectName;
            }
            
            // Fallback if WiseTwinManager not available
            string projectName = Application.productName;
            return string.IsNullOrEmpty(projectName) ? "Unity Training" : projectName;
        }
        
        void LogDebug(string message)
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
            LogDebug($"Debug logs {(enabled ? "enabled" : "disabled")}");
        }
        
    }
}