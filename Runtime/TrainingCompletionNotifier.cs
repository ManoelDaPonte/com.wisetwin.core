using UnityEngine;
using System.Runtime.InteropServices;

namespace WiseTwin
{
    /// <summary>
    /// Handles training completion notifications to web applications.
    /// Includes debug testing capabilities controlled by WiseTwinManager settings.
    /// </summary>
    public class TrainingCompletionNotifier : MonoBehaviour
    {
        
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
            if (WiseTwinManager.Instance != null && WiseTwinManager.Instance.EnableDebugLogs)
            {
                Debug.Log($"[TrainingCompletionNotifier] {message}");
            }
            else if (WiseTwinManager.Instance == null)
            {
                // Fallback logging when WiseTwinManager not available
                Debug.Log($"[TrainingCompletionNotifier] {message}");
            }
        }
        
    }
}