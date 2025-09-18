using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class MetadataLoader : MonoBehaviour
{
    [Header("🌐 Configuration (managed by WiseTwinManager)")]
    [SerializeField, Tooltip("API URL for Azure metadata")]
    public string apiBaseUrl = "https://your-domain.com/api/unity/metadata";
    [SerializeField, Tooltip("Container ID for Azure")]
    public string containerId = "";
    [SerializeField, Tooltip("Build type identifier")]
    public string buildType = "wisetrainer";
    [SerializeField, Tooltip("Request timeout in seconds")]
    public float requestTimeout = 30f;
    [SerializeField, Tooltip("Maximum retry attempts")]
    public int maxRetryAttempts = 3;
    [SerializeField, Tooltip("Delay between retries")]
    public float retryDelay = 2f;

    [Header("🔧 Debug Settings")]
    [SerializeField, Tooltip("Enable debug logs for this component")]
    private bool enableDebugLogs = true;
    [SerializeField, Tooltip("Log prefix for easy filtering")]
    private string logPrefix = "[MetadataLoader]";
    
    [Header("📋 Project Info")]
    [Tooltip("Project name (auto-generated from Unity project)")]
    private string projectName;
    
    // Events simples
    public System.Action<Dictionary<string, object>> OnMetadataLoaded;
    public System.Action<string> OnLoadError;
    
    // Données chargées
    private Dictionary<string, object> loadedMetadata;
    private Dictionary<string, object> unityData;
    
    // Singleton
    public static MetadataLoader Instance { get; private set; }
    
    // Propriétés publiques
    public bool IsLoaded => loadedMetadata != null;
    public string ProjectName => projectName;
    public Dictionary<string, object> GetMetadata() => loadedMetadata;
    public Dictionary<string, object> GetUnityData() => unityData;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Only apply DontDestroyOnLoad if this is on a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeProjectName();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeProjectName()
    {
        // Try to get project name from WiseTwinManager first
        if (WiseTwin.WiseTwinManager.Instance != null)
        {
            string managerProjectName = WiseTwin.WiseTwinManager.Instance.ProjectName;
            if (!string.IsNullOrEmpty(managerProjectName))
            {
                projectName = managerProjectName;
                DebugLog($"📋 Project name from WiseTwinManager: {projectName}");
                return;
            }
        }
        
        // Fallback to Unity project name
        projectName = Application.productName;
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = "unity-project";
        }
        DebugLog($"📋 Project name (fallback): {projectName}");
    }
    
    void Start()
    {
        // Debug pour vérifier la configuration
        bool isProduction = !GetUseLocalMode();
        DebugLog($"🚀 MetadataLoader Start - Production Mode: {isProduction}");
        if (isProduction)
        {
            DebugLog($"📡 API URL: {apiBaseUrl}");
            DebugLog($"📦 Container ID: {containerId}");
            DebugLog($"🏗️ Build Type: {buildType}");
            DebugLog($"📋 Build Name: {projectName}");
        }

        LoadMetadata();
    }
    
    public void LoadMetadata()
    {
        bool useLocalMode = GetUseLocalMode();
        DebugLog($"🔄 Starting metadata load - Mode: {(useLocalMode ? "Local" : "Production")}");
        
        if (useLocalMode)
        {
            StartCoroutine(LoadLocalMetadata());
        }
        else
        {
            StartCoroutine(LoadFromAzure());
        }
    }
    
    IEnumerator LoadLocalMetadata()
    {
        DebugLog("📂 Chargement des métadonnées locales...");
        
        string fileName = $"{projectName}-metadata.json";
        string[] possiblePaths = {
            Path.Combine(Application.streamingAssetsPath, fileName),
            Path.Combine(Application.streamingAssetsPath, "metadata.json"),
            Path.Combine(Application.persistentDataPath, fileName)
        };
        
        string foundPath = null;
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                foundPath = path;
                DebugLog($"✅ Fichier trouvé: {path}");
                break;
            }
        }
        
        if (foundPath != null)
        {
            try
            {
                string jsonContent = File.ReadAllText(foundPath);
                ProcessJSON(jsonContent);
                DebugLog("✅ Métadonnées locales chargées avec succès");
            }
            catch (System.Exception e)
            {
                string error = $"❌ Erreur lecture fichier local: {e.Message}";
                DebugLog(error);
                OnLoadError?.Invoke(error);
            }
        }
        else
        {
            string error = $"❌ Aucun fichier trouvé pour '{projectName}'";
            DebugLog(error);
            OnLoadError?.Invoke(error);
        }
        
        yield return null;
    }
    
    IEnumerator LoadFromAzure()
    {
        DebugLog("🌐 Chargement depuis Azure...");
        
        string url = BuildAPIUrl();
        DebugLog($"📡 URL: {url}");
        
        for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
        {
            DebugLog($"🔄 Tentative {attempt + 1}/{maxRetryAttempts}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        DebugLog($"✅ Réponse reçue ({request.downloadHandler.text.Length} caractères)");
                        
                        // Parser la réponse de l'API
                        var apiResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                        
                        if (apiResponse.ContainsKey("success") && (bool)apiResponse["success"])
                        {
                            if (apiResponse.ContainsKey("data"))
                            {
                                string metadataJson = JsonConvert.SerializeObject(apiResponse["data"]);
                                ProcessJSON(metadataJson);
                                DebugLog("✅ Métadonnées Azure chargées avec succès");
                                yield break; // Succès, on sort
                            }
                        }
                        else if (apiResponse.ContainsKey("error"))
                        {
                            throw new System.Exception(apiResponse["error"].ToString());
                        }
                        else
                        {
                            // Peut-être que la réponse est directement les métadonnées
                            ProcessJSON(request.downloadHandler.text);
                            DebugLog("✅ Métadonnées Azure chargées avec succès (format direct)");
                            yield break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        DebugLog($"❌ Erreur parsing réponse: {e.Message}");
                        DebugLog($"📄 Contenu: {request.downloadHandler.text.Substring(0, Mathf.Min(200, request.downloadHandler.text.Length))}...");
                    }
                }
                else
                {
                    DebugLog($"❌ Erreur réseau: {request.error} (Code: {request.responseCode})");
                }
            }
            
            // Attendre avant la prochaine tentative
            if (attempt < maxRetryAttempts - 1)
            {
                DebugLog($"⏳ Attente de {retryDelay}s...");
                yield return new WaitForSeconds(retryDelay);
            }
        }
        
        // Toutes les tentatives ont échoué
        string finalError = "❌ Impossible de charger depuis Azure après toutes les tentatives";
        DebugLog(finalError);
        OnLoadError?.Invoke(finalError);
    }
    
    string BuildAPIUrl()
    {
        string url = apiBaseUrl;
        List<string> parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(projectName))
            parameters.Add($"buildName={UnityWebRequest.EscapeURL(projectName)}");
        
        if (!string.IsNullOrEmpty(buildType))
            parameters.Add($"buildType={UnityWebRequest.EscapeURL(buildType)}");
        
        if (!string.IsNullOrEmpty(containerId))
            parameters.Add($"containerId={UnityWebRequest.EscapeURL(containerId)}");
        
        if (parameters.Count > 0)
        {
            url += "?" + string.Join("&", parameters.ToArray());
        }
        
        return url;
    }
    
    void ProcessJSON(string jsonContent)
    {
        try
        {
            // Parser le JSON complet
            loadedMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            
            // Extraire la section Unity si elle existe
            if (loadedMetadata.ContainsKey("unity"))
            {
                unityData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(loadedMetadata["unity"]));
                
                DebugLog($"📊 Section Unity trouvée avec {unityData.Count} objets");
            }
            else
            {
                DebugLog("⚠️ Aucune section 'unity' trouvée dans les métadonnées");
                unityData = new Dictionary<string, object>();
            }
            
            // Notifier le succès
            OnMetadataLoaded?.Invoke(loadedMetadata);
        }
        catch (System.Exception e)
        {
            string error = $"❌ Erreur parsing JSON: {e.Message}";
            DebugLog(error);
            OnLoadError?.Invoke(error);
        }
    }
    
    // API publique simple pour récupérer les données d'un objet
    public Dictionary<string, object> GetDataForObject(string objectId)
    {
        if (unityData == null || !unityData.ContainsKey(objectId))
        {
            DebugLog($"⚠️ Aucune donnée trouvée pour: {objectId}");
            return null;
        }
        
        try
        {
            var objectData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                JsonConvert.SerializeObject(unityData[objectId]));
            
            DebugLog($"✅ Données récupérées pour: {objectId}");
            return objectData;
        }
        catch (System.Exception e)
        {
            DebugLog($"❌ Erreur récupération données pour {objectId}: {e.Message}");
            return null;
        }
    }
    
    // Méthode générique pour récupérer un contenu typé
    public T GetContentForObject<T>(string objectId, string contentKey = null) where T : class
    {
        var objectData = GetDataForObject(objectId);
        if (objectData == null) return null;
        
        try
        {
            object targetData = objectData;
            
            // Si une clé spécifique est demandée
            if (!string.IsNullOrEmpty(contentKey) && objectData.ContainsKey(contentKey))
            {
                targetData = objectData[contentKey];
            }
            
            string json = JsonConvert.SerializeObject(targetData);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (System.Exception e)
        {
            DebugLog($"❌ Erreur parsing contenu typé pour {objectId}: {e.Message}");
            return null;
        }
    }
    
    // Méthodes utilitaires
    public void ReloadMetadata()
    {
        DebugLog("🔄 Rechargement des métadonnées...");
        loadedMetadata = null;
        unityData = null;
        LoadMetadata();
    }
    
    public List<string> GetAvailableObjectIds()
    {
        return unityData != null ? new List<string>(unityData.Keys) : new List<string>();
    }
    
    public string GetProjectInfo(string key)
    {
        if (loadedMetadata != null && loadedMetadata.ContainsKey(key))
        {
            return loadedMetadata[key].ToString();
        }
        return "";
    }
    
    /// <summary>
    /// Update settings from WiseTwinManager
    /// </summary>
    public void UpdateSettingsFromManager()
    {
        // Settings are now managed by WiseTwinManager
        // This method exists for future extensibility
    }
    
    bool GetUseLocalMode()
    {
        if (WiseTwin.WiseTwinManager.Instance != null)
        {
            return !WiseTwin.WiseTwinManager.Instance.IsProductionMode();
        }
        
        // Fallback: assume local mode if WiseTwinManager not available
        return true;
    }
    
    bool ShouldLogDebug()
    {
        // Use local debug setting
        return enableDebugLogs;
    }

    void DebugLog(string message)
    {
        if (ShouldLogDebug())
        {
            Debug.Log($"{logPrefix} {message}");
        }
    }
    
    // Interface de debug simple
    void OnGUI()
    {
        if (!ShouldLogDebug()) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.BeginVertical("box");
        
        GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontStyle = FontStyle.Bold;
        
        GUILayout.Label("🎯 MetadataLoader", boldStyle);
        bool useLocalMode = GetUseLocalMode();
        GUILayout.Label($"Mode: {(useLocalMode ? "Local" : "Production")}");
        GUILayout.Label($"Project: {projectName}");
        GUILayout.Label($"Loaded: {(IsLoaded ? "✅" : "❌")}");
        
        if (IsLoaded)
        {
            GUILayout.Label($"Unity Objects: {unityData.Count}");
            GUILayout.Label($"Title: {GetProjectInfo("title")}");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔄 Reload"))
        {
            ReloadMetadata();
        }
        
        GUILayout.Label("Mode controlled by WiseTwinManager", GUI.skin.box);
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
