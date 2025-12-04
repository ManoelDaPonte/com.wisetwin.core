using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace WiseTwin
{
    /// <summary>
    /// Singleton manager that sets up video click handlers on 3D objects
    /// Reads video triggers from metadata and adds VideoClickHandler components
    /// </summary>
    public class VideoTriggerManager : MonoBehaviour
    {
        public static VideoTriggerManager Instance { get; private set; }

        private List<VideoTriggerData> videoTriggers = new List<VideoTriggerData>();
        private List<VideoClickHandler> activeHandlers = new List<VideoClickHandler>();

        // Debug
        private bool EnableDebugLogs => WiseTwinManager.Instance != null && WiseTwinManager.Instance.EnableDebugLogs;

        void DebugLog(string message)
        {
            if (EnableDebugLogs)
                Debug.Log($"[VideoTriggerManager] {message}");
        }

        void Awake()
        {
            // Check if instance is null OR if it points to a destroyed object (can happen in editor between play sessions)
            if (Instance == null || Instance.gameObject == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize video triggers from metadata
        /// Call this after MetadataLoader has loaded the metadata
        /// </summary>
        public void InitializeFromMetadata(List<object> videoTriggersData)
        {
            if (videoTriggersData == null || videoTriggersData.Count == 0)
            {
                DebugLog("No video triggers in metadata");
                return;
            }

            // Clear existing handlers
            ClearHandlers();

            // Parse video triggers
            foreach (var triggerObj in videoTriggersData)
            {
                try
                {
                    Dictionary<string, object> triggerDict = null;

                    if (triggerObj is JObject jObj)
                    {
                        triggerDict = jObj.ToObject<Dictionary<string, object>>();
                    }
                    else if (triggerObj is Dictionary<string, object> dict)
                    {
                        triggerDict = dict;
                    }

                    if (triggerDict != null)
                    {
                        var data = VideoTriggerData.FromDictionary(triggerDict);
                        if (!string.IsNullOrEmpty(data.targetObjectName))
                        {
                            videoTriggers.Add(data);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    DebugLog($"Failed to parse video trigger: {e.Message}");
                }
            }

            DebugLog($"Loaded {videoTriggers.Count} video trigger(s)");

            // Setup handlers on scene objects
            SetupHandlers();
        }

        /// <summary>
        /// Setup VideoClickHandler components on target objects in the scene
        /// </summary>
        void SetupHandlers()
        {
            foreach (var trigger in videoTriggers)
            {
                // Use FindGameObjectByName to find inactive objects too
                GameObject targetObject = FindGameObjectByName(trigger.targetObjectName);
                if (targetObject != null)
                {
                    // Add handler if not already present
                    var handler = targetObject.GetComponent<VideoClickHandler>();
                    if (handler == null)
                    {
                        handler = targetObject.AddComponent<VideoClickHandler>();
                    }
                    handler.Initialize(trigger);
                    activeHandlers.Add(handler);

                    DebugLog($"Added video handler to '{trigger.targetObjectName}' (active: {targetObject.activeInHierarchy})");
                }
                else
                {
                    DebugLog($"Target object '{trigger.targetObjectName}' not found in scene");
                }
            }
        }

        /// <summary>
        /// Find a GameObject by name, including inactive objects
        /// </summary>
        GameObject FindGameObjectByName(string name)
        {
            // First try the fast method for active objects
            GameObject obj = GameObject.Find(name);
            if (obj != null) return obj;

            // Search in all root objects of all loaded scenes (includes inactive)
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject rootObj in scene.GetRootGameObjects())
                {
                    GameObject found = FindInChildren(rootObj.transform, name);
                    if (found != null) return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively search for a GameObject by name in children
        /// </summary>
        GameObject FindInChildren(Transform parent, string name)
        {
            if (parent.name == name)
                return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject found = FindInChildren(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// Remove all active handlers
        /// </summary>
        public void ClearHandlers()
        {
            foreach (var handler in activeHandlers)
            {
                if (handler != null)
                {
                    Destroy(handler);
                }
            }
            activeHandlers.Clear();
            videoTriggers.Clear();
        }

        void OnDestroy()
        {
            ClearHandlers();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
