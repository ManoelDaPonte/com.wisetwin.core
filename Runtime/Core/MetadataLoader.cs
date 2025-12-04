using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using WiseTwin;

public class MetadataLoader : MonoBehaviour
{
    [Header("🌐 Configuration (managed by WiseTwinManager)")]
    [SerializeField, Tooltip("Use Azure Storage directly instead of API")]
    public bool useAzureStorageDirect = false;
    [SerializeField, Tooltip("Azure Storage URL (for direct access)")]
    public string azureStorageUrl = "https://yourstorage.blob.core.windows.net/";
    [SerializeField, Tooltip("API URL for Azure metadata (via Next.js)")]
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
    
    [Header("📋 Scene Info")]
    [Tooltip("Scene name (auto-detected from active scene)")]
    private string sceneName;
    
    // Events simples
    public System.Action OnLoadStarted;
    public System.Action<Dictionary<string, object>> OnMetadataLoaded;
    public System.Action<string> OnLoadError;

    // Données chargées
    private Dictionary<string, object> loadedMetadata;
    private Dictionary<string, object> unityData; // Legacy format support
    private List<ScenarioData> scenarios;
    private TrainingSettings settings;
    private List<object> videoTriggers; // Video trigger configurations
    private bool isLoading = false;
    
    // Singleton
    public static MetadataLoader Instance { get; private set; }

    // Propriétés publiques
    public bool IsLoaded => loadedMetadata != null;
    public bool IsLoading => isLoading;
    public string SceneName => sceneName;
    public Dictionary<string, object> GetMetadata() => loadedMetadata;
    public Dictionary<string, object> GetUnityData() => unityData; // Legacy
    public List<ScenarioData> GetScenarios() => scenarios;
    public TrainingSettings GetSettings() => settings;
    public int GetScenarioCount() => scenarios?.Count ?? 0;
    public bool HasScenarios() => scenarios != null && scenarios.Count > 0;
    public List<object> GetVideoTriggers() => videoTriggers;
    public bool HasVideoTriggers() => videoTriggers != null && videoTriggers.Count > 0;
    
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

            InitializeSceneName();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeSceneName()
    {
        // Get the active scene name
        sceneName = SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "default-scene";
            DebugLog($"⚠️ Could not get scene name, using fallback: {sceneName}");
        }
        else
        {
            DebugLog($"📋 Scene name detected: {sceneName}");
        }
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
            DebugLog($"📋 Scene Name: {sceneName}");
        }

        LoadMetadata();
    }
    
    public void LoadMetadata()
    {
        bool useLocalMode = GetUseLocalMode();
        DebugLog($"🔄 Starting metadata load - Mode: {(useLocalMode ? "Local" : "Production")}");

        isLoading = true;
        OnLoadStarted?.Invoke();

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
        
        string fileName = $"{sceneName}-metadata.json";
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
                isLoading = false;
                OnLoadError?.Invoke(error);
            }
        }
        else
        {
            string error = $"❌ Aucun fichier trouvé pour '{sceneName}'";
            DebugLog(error);
            isLoading = false;
            OnLoadError?.Invoke(error);
        }

        isLoading = false;
        yield return null;
    }
    
    IEnumerator LoadFromAzure()
    {
        if (useAzureStorageDirect)
        {
            yield return LoadFromAzureStorageDirect();
        }
        else
        {
            yield return LoadFromAzureAPI();
        }
    }

    IEnumerator LoadFromAzureStorageDirect()
    {
        DebugLog("☁️ Chargement direct depuis Azure Storage...");

        // Construction de l'URL Azure Storage
        // Format: https://storage.blob.core.windows.net/container/buildType/projectName-metadata.json
        string fileName = $"{sceneName}-metadata.json";
        string url = $"{azureStorageUrl.TrimEnd('/')}/{containerId}/{buildType}/{fileName}";

        DebugLog($"📡 Azure Storage URL: {url}");

        for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
        {
            DebugLog($"🔄 Tentative {attempt + 1}/{maxRetryAttempts}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)requestTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    DebugLog($"✅ Réponse reçue ({request.downloadHandler.text.Length} caractères)");

                    try
                    {
                        // Les données d'Azure Storage sont directement le JSON
                        ProcessJSON(request.downloadHandler.text);
                        DebugLog("✅ Métadonnées Azure Storage chargées avec succès");
                        isLoading = false;
                        yield break;
                    }
                    catch (System.Exception e)
                    {
                        DebugLog($"❌ Erreur parsing JSON: {e.Message}");
                    }
                }
                else
                {
                    DebugLog($"❌ Erreur: {request.error} (Code: {request.responseCode})");

                    // Si erreur 404, essayer sans le buildType dans le chemin
                    if (request.responseCode == 404 && attempt == 0)
                    {
                        string altUrl = $"{azureStorageUrl.TrimEnd('/')}/{containerId}/{fileName}";
                        DebugLog($"🔄 Tentative avec URL alternative: {altUrl}");

                        using (UnityWebRequest altRequest = UnityWebRequest.Get(altUrl))
                        {
                            altRequest.timeout = (int)requestTimeout;
                            yield return altRequest.SendWebRequest();

                            if (altRequest.result == UnityWebRequest.Result.Success)
                            {
                                ProcessJSON(altRequest.downloadHandler.text);
                                DebugLog("✅ Métadonnées trouvées avec URL alternative");
                                isLoading = false;
                                yield break;
                            }
                        }
                    }
                }
            }

            if (attempt < maxRetryAttempts - 1)
            {
                DebugLog($"⏳ Attente de {retryDelay}s...");
                yield return new WaitForSeconds(retryDelay);
            }
        }

        string error = "❌ Impossible de charger depuis Azure Storage";
        DebugLog(error);
        isLoading = false;
        OnLoadError?.Invoke(error);
    }

    IEnumerator LoadFromAzureAPI()
    {
        DebugLog("🌐 Chargement depuis l'API Next.js...");

        string url = BuildAPIUrl();
        DebugLog($"📡 API URL: {url}");
        
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
                                isLoading = false;
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
                            isLoading = false;
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
        isLoading = false;
        OnLoadError?.Invoke(finalError);
    }
    
    string BuildAPIUrl()
    {
        string url = apiBaseUrl;
        List<string> parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(sceneName))
            parameters.Add($"buildName={UnityWebRequest.EscapeURL(sceneName)}");
        
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

            // Extract settings
            if (loadedMetadata.ContainsKey("settings"))
            {
                string settingsJson = JsonConvert.SerializeObject(loadedMetadata["settings"]);
                settings = JsonConvert.DeserializeObject<TrainingSettings>(settingsJson);
                DebugLog($"⚙️ Settings loaded");
            }
            else
            {
                settings = new TrainingSettings();
                DebugLog("⚙️ Using default settings");
            }

            // Extract scenarios (new format)
            if (loadedMetadata.ContainsKey("scenarios"))
            {
                string scenariosJson = JsonConvert.SerializeObject(loadedMetadata["scenarios"]);
                scenarios = JsonConvert.DeserializeObject<List<ScenarioData>>(scenariosJson);
                DebugLog($"🎯 Scenarios loaded: {scenarios.Count} scenarios found");

                // Log scenario types for debugging
                foreach (var scenario in scenarios)
                {
                    DebugLog($"  - {scenario.id}: {scenario.type}");
                }
            }
            else
            {
                DebugLog("⚠️ No 'scenarios' section found in metadata");
                scenarios = new List<ScenarioData>();
            }

            unityData = new Dictionary<string, object>();

            // Extract video triggers
            if (loadedMetadata.ContainsKey("videoTriggers"))
            {
                try
                {
                    var triggersObj = loadedMetadata["videoTriggers"];
                    if (triggersObj is JArray jArray)
                    {
                        videoTriggers = jArray.ToObject<List<object>>();
                    }
                    else if (triggersObj is List<object> list)
                    {
                        videoTriggers = list;
                    }
                    DebugLog($"🎬 Video triggers loaded: {videoTriggers?.Count ?? 0} trigger(s)");
                }
                catch (System.Exception e)
                {
                    DebugLog($"⚠️ Error parsing video triggers: {e.Message}");
                    videoTriggers = new List<object>();
                }
            }
            else
            {
                videoTriggers = new List<object>();
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
        isLoading = false;
        LoadMetadata();
    }
    
    public List<string> GetAvailableObjectIds()
    {
        return unityData != null ? new List<string>(unityData.Keys) : new List<string>();
    }

    /// <summary>
    /// Get a scenario by index
    /// </summary>
    public ScenarioData GetScenario(int index)
    {
        if (scenarios == null || index < 0 || index >= scenarios.Count)
        {
            DebugLog($"⚠️ Invalid scenario index: {index} (total: {scenarios?.Count ?? 0})");
            return null;
        }

        return scenarios[index];
    }

    /// <summary>
    /// Get localized disclaimer text
    /// </summary>
    public string GetDisclaimer(string languageCode = "en")
    {
        if (loadedMetadata != null && loadedMetadata.ContainsKey("disclaimer"))
        {
            try
            {
                var disclaimerObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(loadedMetadata["disclaimer"]));

                if (disclaimerObj.ContainsKey(languageCode))
                {
                    return disclaimerObj[languageCode];
                }

                // Fallback to English if requested language not found
                if (disclaimerObj.ContainsKey("en"))
                {
                    return disclaimerObj["en"];
                }
            }
            catch (System.Exception e)
            {
                DebugLog($"⚠️ Error parsing disclaimer: {e.Message}");
            }
        }

        return null; // No disclaimer in metadata
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
        GUILayout.Label($"Scene: {sceneName}");
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
