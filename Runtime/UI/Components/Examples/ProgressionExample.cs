using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WiseTwin.Examples
{
    /// <summary>
    /// Exemple d'utilisation du ProgressionManager
    /// Affiche une UI simple de progression avec feedback visuel
    /// </summary>
    public class ProgressionExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProgressionManager progressionManager;

        [Header("UI Elements (Optional)")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Settings")]
        [SerializeField] private bool showDebugMessages = true;

        void OnEnable()
        {
            if (progressionManager == null)
            {
                Debug.LogError("[ProgressionExample] ProgressionManager not assigned!");
                return;
            }

            // S'abonner aux événements
            progressionManager.OnStepActivated += HandleStepActivated;
            progressionManager.OnStepCompleted += HandleStepCompleted;
            progressionManager.OnProgressionCompleted += HandleProgressionCompleted;
            progressionManager.OnMaxAttemptsReached += HandleMaxAttemptsReached;
        }

        void OnDisable()
        {
            if (progressionManager == null) return;

            // Se désabonner des événements
            progressionManager.OnStepActivated -= HandleStepActivated;
            progressionManager.OnStepCompleted -= HandleStepCompleted;
            progressionManager.OnProgressionCompleted -= HandleProgressionCompleted;
            progressionManager.OnMaxAttemptsReached -= HandleMaxAttemptsReached;
        }

        void Start()
        {
            // Cacher le panneau de complétion au démarrage
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }

            UpdateProgressUI();
        }

        /// <summary>
        /// Appelé quand une nouvelle étape devient active
        /// </summary>
        void HandleStepActivated(int stepIndex, InteractableObject obj)
        {
            if (showDebugMessages)
            {
                Debug.Log($"✓ Étape {stepIndex + 1}/{progressionManager.TotalSteps} activée : {obj.name}");
            }

            UpdateProgressUI();

            // Afficher un message à l'utilisateur
            if (messageText != null)
            {
                messageText.text = $"Étape {stepIndex + 1} : Cliquez sur {obj.name}";
                messageText.color = Color.white;
            }
        }

        /// <summary>
        /// Appelé quand une étape est complétée (succès ou échec)
        /// </summary>
        void HandleStepCompleted(int stepIndex, InteractableObject obj, bool success)
        {
            if (success)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"✓ Étape {stepIndex + 1} réussie : {obj.name}");
                }

                // Afficher un message de succès
                if (messageText != null)
                {
                    messageText.text = $"✓ Étape {stepIndex + 1} réussie !";
                    messageText.color = Color.green;
                }
            }
            else
            {
                if (showDebugMessages)
                {
                    Debug.Log($"✗ Étape {stepIndex + 1} échouée : {obj.name}");
                }

                // Afficher un message d'échec
                if (messageText != null)
                {
                    messageText.text = $"✗ Étape {stepIndex + 1} échouée, réessayez";
                    messageText.color = Color.red;
                }

                // Afficher le nombre d'essais
                int attempts = progressionManager.GetAttemptCount(
                    obj.GetComponent<ObjectMetadataMapper>()?.MetadataId ?? ""
                );

                if (attempts > 1 && messageText != null)
                {
                    messageText.text += $" (Essai {attempts})";
                }
            }

            UpdateProgressUI();
        }

        /// <summary>
        /// Appelé quand toute la progression est terminée
        /// </summary>
        void HandleProgressionCompleted()
        {
            if (showDebugMessages)
            {
                Debug.Log("🎉 Formation terminée avec succès !");
            }

            // Afficher l'écran de complétion
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }

            if (messageText != null)
            {
                messageText.text = "🎉 Formation terminée !";
                messageText.color = Color.yellow;
            }

            // Vous pouvez ici :
            // - Envoyer les résultats à votre API
            // - Afficher les statistiques
            // - Débloquer un certificat
            // - etc.

            SendCompletionToAPI();
        }

        /// <summary>
        /// Appelé quand le nombre max d'essais est atteint
        /// </summary>
        void HandleMaxAttemptsReached(InteractableObject obj, int attemptCount)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"⚠️ Nombre max d'essais atteint pour {obj.name} ({attemptCount} essais)");
            }

            if (messageText != null)
            {
                messageText.text = $"⚠️ Trop d'essais pour {obj.name}. Besoin d'aide ?";
                messageText.color = Color.yellow;
            }

            // Vous pouvez ici :
            // - Afficher une aide supplémentaire
            // - Proposer de voir un tutoriel
            // - Permettre de passer cette étape
            // - Contacter un formateur
        }

        /// <summary>
        /// Met à jour l'UI de progression
        /// </summary>
        void UpdateProgressUI()
        {
            if (progressionManager == null) return;

            // Mettre à jour le texte de progression
            if (progressText != null)
            {
                int current = progressionManager.CurrentStepIndex + 1;
                int total = progressionManager.TotalSteps;
                float percentage = progressionManager.ProgressPercentage;

                progressText.text = $"Progression : {current}/{total} ({percentage:F0}%)";
            }

            // Mettre à jour la barre de progression
            if (progressBar != null)
            {
                progressBar.value = progressionManager.ProgressPercentage / 100f;
            }
        }

        /// <summary>
        /// Envoie les résultats de la formation à votre API
        /// </summary>
        void SendCompletionToAPI()
        {
            // Exemple de données à envoyer
            var completionData = new
            {
                trainingId = "autoclave-kenitra-LOTO",
                completedSteps = progressionManager.TotalSteps,
                totalAttempts = GetTotalAttempts(),
                duration = Time.time, // Remplacer par un timer réel
                timestamp = System.DateTime.UtcNow
            };

            // Ici, utilisez votre méthode d'envoi vers l'API
            // Par exemple avec TrainingCompletionNotifier
            var completionNotifier = FindFirstObjectByType<TrainingCompletionNotifier>();
            if (completionNotifier != null)
            {
                completionNotifier.FormationCompleted();
            }
            else if (showDebugMessages)
            {
                Debug.LogWarning("[ProgressionExample] TrainingCompletionNotifier not found in scene");
            }

            if (showDebugMessages)
            {
                Debug.Log($"Données envoyées à l'API : {UnityEngine.JsonUtility.ToJson(completionData)}");
            }
        }

        /// <summary>
        /// Calcule le nombre total d'essais pour toutes les étapes
        /// </summary>
        int GetTotalAttempts()
        {
            int total = 0;

            foreach (var obj in progressionManager.ProgressionSequence)
            {
                if (obj != null)
                {
                    var mapper = obj.GetComponent<ObjectMetadataMapper>();
                    if (mapper != null)
                    {
                        total += progressionManager.GetAttemptCount(mapper.MetadataId);
                    }
                }
            }

            return total;
        }

        #region Public Methods (pour appeler depuis d'autres scripts ou UI)

        /// <summary>
        /// Redémarre la progression depuis le début
        /// </summary>
        public void RestartProgression()
        {
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }

            if (progressionManager != null)
            {
                progressionManager.StartProgression();
            }
        }

        /// <summary>
        /// Quitte la progression
        /// </summary>
        public void QuitProgression()
        {
            if (progressionManager != null)
            {
                progressionManager.StopProgression();
            }

            // Retourner au menu principal, changer de scène, etc.
            Debug.Log("Quitter la progression");
        }

        #endregion
    }
}
