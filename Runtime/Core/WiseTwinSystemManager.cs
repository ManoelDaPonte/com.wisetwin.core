using UnityEngine;

namespace WiseTwin
{
    /// <summary>
    /// Gestionnaire principal du système WiseTwin
    /// Place ce composant sur le GameObject parent contenant tous les systèmes WiseTwin
    /// </summary>
    public class WiseTwinSystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool debugMode = false;

        // Singleton
        public static WiseTwinSystemManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                // Appliquer DontDestroyOnLoad sur le GameObject parent
                if (persistAcrossScenes && transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                    if (debugMode) Debug.Log("[WiseTwinSystemManager] System will persist across scenes");
                }

                ValidateChildComponents();
            }
            else
            {
                // Un autre système existe déjà, détruire celui-ci
                if (debugMode) Debug.Log("[WiseTwinSystemManager] Another instance exists, destroying this one");
                Destroy(gameObject);
            }
        }

        void ValidateChildComponents()
        {
            // Vérifier que les composants essentiels sont présents
            var trainingHUD = GetComponentInChildren<TrainingHUD>();
            if (trainingHUD == null && debugMode)
            {
                Debug.LogWarning("[WiseTwinSystemManager] TrainingHUD not found in children");
            }

            var contentDisplayManager = GetComponentInChildren<UI.ContentDisplayManager>();
            if (contentDisplayManager == null && debugMode)
            {
                Debug.LogWarning("[WiseTwinSystemManager] ContentDisplayManager not found in children");
            }

            var analytics = GetComponentInChildren<Analytics.TrainingAnalytics>();
            if (analytics == null)
            {
                // Créer TrainingAnalytics s'il n'existe pas
                var analyticsGO = new GameObject("TrainingAnalytics");
                analyticsGO.transform.SetParent(transform);
                analyticsGO.AddComponent<Analytics.TrainingAnalytics>();
                Debug.Log("[WiseTwinSystemManager] Created TrainingAnalytics component");
            }

            if (debugMode)
            {
                int componentCount = GetComponentsInChildren<MonoBehaviour>().Length;
                Debug.Log($"[WiseTwinSystemManager] System initialized with {componentCount} components");
            }
        }

        /// <summary>
        /// Réinitialise le système (utile entre les formations)
        /// </summary>
        public void ResetSystem()
        {
            // Réinitialiser les analytics
            if (Analytics.TrainingAnalytics.Instance != null)
            {
                Analytics.TrainingAnalytics.Instance.ResetAnalytics();
            }

            // Réinitialiser le HUD
            if (TrainingHUD.Instance != null)
            {
                TrainingHUD.Instance.Hide();
            }

            if (debugMode) Debug.Log("[WiseTwinSystemManager] System reset");
        }

        /// <summary>
        /// Détruit complètement le système
        /// </summary>
        public void DestroySystem()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
        }
    }
}