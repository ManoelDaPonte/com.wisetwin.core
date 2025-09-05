using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WiseTwin
{
    public class MetadataObjectBinder : MonoBehaviour
    {
        [Header("🎯 Configuration")]
        [Tooltip("ID de l'objet dans le fichier metadata. Si vide, utilise le nom du GameObject.")]
        public string objectId = "";
        
        [Header("🔍 Debug")]
        public bool enableDebugLogs = true;
        
        // Données chargées
        private Dictionary<string, object> objectData;
        private bool isDataLoaded = false;
        private bool isWaitingForLoader = false;
        
        // Événement simple pour notifier du chargement des données
        public System.Action<Dictionary<string, object>> OnDataLoaded;
        
        void Start()
        {
            InitializeObjectId();
            SetupMetadataLoaderConnection();
        }
        
        void InitializeObjectId()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                objectId = gameObject.name.Replace(" ", "_");
                DebugLog($"Object ID: {objectId}");
            }
        }
        
        void SetupMetadataLoaderConnection()
        {
            if (MetadataLoader.Instance != null)
            {
                if (MetadataLoader.Instance.IsLoaded)
                {
                    DebugLog("MetadataLoader déjà chargé - chargement immédiat");
                    LoadObjectData();
                }
                else
                {
                    DebugLog("En attente du chargement des métadonnées");
                    MetadataLoader.Instance.OnMetadataLoaded += OnMetadataLoaderReady;
                    MetadataLoader.Instance.OnLoadError += OnMetadataLoaderError;
                    isWaitingForLoader = true;
                }
            }
            else
            {
                DebugLog("MetadataLoader.Instance non trouvé - retry dans 0.5s");
                Invoke(nameof(RetryMetadataLoaderConnection), 0.5f);
                isWaitingForLoader = true;
            }
        }
        
        void RetryMetadataLoaderConnection()
        {
            if (!isDataLoaded && isWaitingForLoader)
            {
                DebugLog("Nouvelle tentative de connexion au MetadataLoader");
                SetupMetadataLoaderConnection();
            }
        }
        
        void OnMetadataLoaderReady(Dictionary<string, object> metadata)
        {
            DebugLog("MetadataLoader prêt - chargement des données");
            isWaitingForLoader = false;
            LoadObjectData();
        }
        
        void OnMetadataLoaderError(string error)
        {
            DebugLog($"Erreur MetadataLoader: {error}");
            isWaitingForLoader = false;
        }
        
        void LoadObjectData()
        {
            if (MetadataLoader.Instance == null || !MetadataLoader.Instance.IsLoaded)
            {
                DebugLog("MetadataLoader non disponible");
                return;
            }
            
            objectData = GetObjectDataFromMetadata();
            
            if (objectData != null && objectData.Count > 0)
            {
                isDataLoaded = true;
                DebugLog($"Données chargées: {objectData.Count} éléments");
                OnDataLoaded?.Invoke(objectData);
            }
            else
            {
                DebugLog($"Aucune donnée trouvée pour '{objectId}'");
            }
        }
        
        Dictionary<string, object> GetObjectDataFromMetadata()
        {
            try
            {
                var metadata = MetadataLoader.Instance.GetMetadata();
                
                if (metadata.ContainsKey("unity"))
                {
                    var unitySection = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(
                        JsonConvert.SerializeObject(metadata["unity"]));
                    
                    if (unitySection.ContainsKey(objectId))
                    {
                        return unitySection[objectId];
                    }
                }
                
                return null;
            }
            catch (System.Exception e)
            {
                DebugLog($"Erreur: {e.Message}");
                return null;
            }
        }
        
        // API publique simple pour accéder aux données
        
        public T GetData<T>(string key, T defaultValue = default(T))
        {
            if (!isDataLoaded || objectData == null || !objectData.ContainsKey(key))
                return defaultValue;
            
            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)objectData[key].ToString();
                
                string json = JsonConvert.SerializeObject(objectData[key]);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return defaultValue;
            }
        }
        
        public bool HasData(string key)
        {
            return isDataLoaded && objectData != null && objectData.ContainsKey(key);
        }
        
        public Dictionary<string, object> GetAllData()
        {
            return isDataLoaded && objectData != null ? new Dictionary<string, object>(objectData) : new Dictionary<string, object>();
        }
        
        public void ReloadData()
        {
            LoadObjectData();
        }
        
        void DebugLog(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MetadataObjectBinder:{objectId}] {message}");
        }
        
        void OnDestroy()
        {
            CancelInvoke();
            
            if (MetadataLoader.Instance != null)
            {
                MetadataLoader.Instance.OnMetadataLoaded -= OnMetadataLoaderReady;
                MetadataLoader.Instance.OnLoadError -= OnMetadataLoaderError;
            }
        }
    }
}

