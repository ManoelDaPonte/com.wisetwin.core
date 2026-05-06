using UnityEngine;
using UnityEditor;
using WiseTwin.Debugging;

namespace WiseTwin.Editor
{
    /// <summary>
    /// Menu shortcut to add a ScoreDebugMonitor to the active scene. The monitor is parented
    /// to WiseTwinSystem if it exists, otherwise placed at the scene root.
    /// </summary>
    public static class ScoreDebugMonitorMenu
    {
        [MenuItem("WiseTwin/Debug/Add Score Monitor to Scene")]
        public static void AddToScene()
        {
            // If a monitor already exists, just select it.
            var existing = Object.FindFirstObjectByType<ScoreDebugMonitor>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[ScoreDebugMonitor] Already present in scene — selecting existing instance.");
                return;
            }

            var go = new GameObject("ScoreDebugMonitor");
            go.AddComponent<ScoreDebugMonitor>();

            var wisetwinSystem = GameObject.Find("WiseTwinSystem");
            if (wisetwinSystem != null)
            {
                go.transform.SetParent(wisetwinSystem.transform);
                Debug.Log("[ScoreDebugMonitor] Added under WiseTwinSystem.");
            }
            else
            {
                Debug.Log("[ScoreDebugMonitor] WiseTwinSystem not found — added at scene root.");
            }

            Undo.RegisterCreatedObjectUndo(go, "Add ScoreDebugMonitor");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
