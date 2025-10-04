using UnityEngine;
using System.Runtime.InteropServices;

namespace WiseTwin
{
    /// <summary>
    /// Gère l'authentification et le token JWT pour la communication avec React
    /// </summary>
    public class WiseTwinAuthManager : MonoBehaviour
    {
        private static WiseTwinAuthManager instance;
        public static WiseTwinAuthManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("WiseTwinAuthManager");
                    instance = go.AddComponent<WiseTwinAuthManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private string authToken = "";

        // Import des fonctions JavaScript - Commenté car plus utilisé dans la version optimisée
        // [DllImport("__Internal")]
        // private static extern void StoreAuthToken(string token);

        // [DllImport("__Internal")]
        // private static extern string GetAuthToken();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Définit le token d'authentification reçu depuis React
        /// Cette méthode sera appelée par React via SendMessage
        /// </summary>
        /// <param name="token">Le token JWT</param>
        public void SetAuthToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[WiseTwinAuthManager] Received empty auth token");
                return;
            }

            authToken = token;
            Debug.Log($"[WiseTwinAuthManager] Auth token set successfully");

            // Stocker également dans JavaScript pour accès global - Désactivé dans la version optimisée
            // #if UNITY_WEBGL && !UNITY_EDITOR
            //     StoreAuthToken(token);
            // #endif
        }

        /// <summary>
        /// Récupère le token d'authentification actuel
        /// </summary>
        /// <returns>Le token JWT ou une chaîne vide</returns>
        public string GetToken()
        {
            // En WebGL, essayer de récupérer depuis JavaScript d'abord - Désactivé dans la version optimisée
            // #if UNITY_WEBGL && !UNITY_EDITOR
            //     if (string.IsNullOrEmpty(authToken))
            //     {
            //         try
            //         {
            //             authToken = GetAuthToken();
            //         }
            //         catch (System.Exception e)
            //         {
            //             Debug.LogError($"[WiseTwinAuthManager] Error getting token from JS: {e.Message}");
            //         }
            //     }
            // #endif

            return authToken;
        }

        /// <summary>
        /// Vérifie si un token est disponible
        /// </summary>
        public bool HasToken()
        {
            return !string.IsNullOrEmpty(GetToken());
        }

        /// <summary>
        /// Réinitialise le token (pour déconnexion par exemple)
        /// </summary>
        public void ClearToken()
        {
            authToken = "";

            // #if UNITY_WEBGL && !UNITY_EDITOR
            //     StoreAuthToken("");
            // #endif

            Debug.Log("[WiseTwinAuthManager] Auth token cleared");
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}