using UnityEngine;

namespace WiseTwin
{
    /// <summary>
    /// Utility class for finding GameObjects, including inactive ones.
    /// Unity's GameObject.Find() only finds active objects, which causes issues
    /// when objects are activated during gameplay.
    /// </summary>
    public static class GameObjectFinder
    {
        /// <summary>
        /// Finds a GameObject by name, including inactive objects in the scene hierarchy.
        /// </summary>
        /// <param name="name">The name of the GameObject to find</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        public static GameObject FindByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // First try the fast path for active objects
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null)
                return activeObj;

            // Search through all transforms including inactive ones
            // FindObjectsOfType with includeInactive=true finds inactive objects in the scene
            Transform[] allTransforms = Object.FindObjectsOfType<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                if (t.name == name)
                    return t.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Finds a GameObject by name using path syntax (e.g., "Parent/Child/Target"),
        /// including inactive objects. Only the first segment is searched in the scene;
        /// subsequent segments are resolved as children.
        /// </summary>
        /// <param name="path">The path to the GameObject (forward-slash separated)</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        public static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // If no path separator, use simple name search
            if (!path.Contains("/"))
                return FindByName(path);

            string[] parts = path.Split('/');

            // Find the root object (including inactive)
            GameObject current = FindByName(parts[0]);
            if (current == null)
                return null;

            // Traverse the path
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null)
                    return null;
                current = child.gameObject;
            }

            return current;
        }
    }
}
