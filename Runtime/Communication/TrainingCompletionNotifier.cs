using UnityEngine;
using System.Runtime.InteropServices;
using WiseTwin.Analytics;

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

        // JavaScript interop for WebGL - Simplified to single call
        [DllImport("__Internal")]
        private static extern void SendTrainingCompleted(string jsonData);
        
        void Start()
        {
            // Initialization if needed
        }
        
        
        /// <summary>
        /// Main method to complete training and notify web application
        /// </summary>
        /// <param name="trainingName">Optional training name</param>
        public void FormationCompleted(string trainingName = null)
        {
            // Marquer la formation comme terminée dans l'analytics
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.CompleteTraining("completed");
            }

            // Toujours envoyer les analytics en WebGL, peu importe le mode
            #if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log("[TrainingCompletionNotifier] Sending training completion data");
                SendAnalytics();
                Debug.Log("[TrainingCompletionNotifier] Training completion sent successfully");
            #else
                // En mode éditeur, afficher les analytics dans la console
                LogDebug($"📊 Training '{trainingName ?? GetProjectName()}' completed");
                if (TrainingAnalytics.Instance != null)
                {
                    string analytics = TrainingAnalytics.Instance.ExportAnalytics();
                    LogDebug($"📊 Analytics data:\n{analytics}");
                }
            #endif
        }

        /// <summary>
        /// Envoie les analytics complètes à l'application web (version simplifiée)
        /// </summary>
        void SendAnalytics()
        {
            if (TrainingAnalytics.Instance == null)
            {
                LogDebug("⚠️ TrainingAnalytics not available - no analytics sent");
                return;
            }

            string analyticsJson = TrainingAnalytics.Instance.ExportAnalytics();

            #if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log($"[TrainingCompletionNotifier] Sending training completion data ({analyticsJson.Length} chars)");

                // Envoyer tout en un seul appel - React gérera le token
                SendTrainingCompleted(analyticsJson);
                Debug.Log("[TrainingCompletionNotifier] 📊 Training completion data sent");
            #else
                LogDebug($"📊 Analytics would be sent in WebGL build:\n{analyticsJson}");
            #endif
        }
        
        /// <summary>
        /// Test completion notification (development only)
        /// </summary>
        public void TestCompletion()
        {
            LogDebug("🧪 Testing completion notification");
            FormationCompleted("Test Training");
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