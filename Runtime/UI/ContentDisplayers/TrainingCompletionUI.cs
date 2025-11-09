using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.SceneManagement;

namespace WiseTwin.UI
{
    /// <summary>
    /// Interface de complétion de formation affichée quand l'utilisateur termine tous les modules
    /// </summary>
    public class TrainingCompletionUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private PanelSettings panelSettings;

        private UIDocument uiDocument;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        // Stats pour afficher
        private float totalTime = 0f;
        private int totalInteractions = 0;

        // Singleton
        public static TrainingCompletionUI Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupUIDocument();
        }

        void SetupUIDocument()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Assigner le PanelSettings depuis l'inspector
            if (uiDocument.panelSettings == null)
            {
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                }
                else
                {
                    Debug.LogWarning("[TrainingCompletionUI] PanelSettings is null! Please assign it in the inspector.");
                }
            }

            rootElement = uiDocument.rootVisualElement;

            // Configuration de base
            rootElement.style.position = Position.Absolute;
            rootElement.style.width = Length.Percent(100);
            rootElement.style.height = Length.Percent(100);
            rootElement.pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Affiche l'écran de complétion de formation
        /// </summary>
        public void ShowCompletionScreen(float trainingTime, int modulesCompleted)
        {
            Debug.Log($"[TrainingCompletionUI] Showing completion screen - Time: {trainingTime}s, Modules: {modulesCompleted}");

            totalTime = trainingTime;
            totalInteractions = modulesCompleted;

            // IMPORTANT : Forcer le HUD à 100% avant d'afficher l'UI de completion
            if (TrainingHUD.Instance != null)
            {
                TrainingHUD.Instance.UpdateProgress(modulesCompleted);
                Debug.Log($"[TrainingCompletionUI] Forced HUD to 100%: {modulesCompleted}/{modulesCompleted}");
            }

            // Bloquer les contrôles du personnage pendant l'écran de fin
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(false);
                Debug.Log("[TrainingCompletionUI] Player controls disabled - cannot move or look around");
            }
            else
            {
                Debug.LogWarning("[TrainingCompletionUI] FirstPersonCharacter not found - controls may not be blocked!");
            }

            CreateCompletionUI();

            // Notifier que la formation est terminée
            NotifyTrainingCompletion();

            Debug.Log("[TrainingCompletionUI] Completion screen displayed - User must quit manually");
        }

        void CreateCompletionUI()
        {
            // Clear root
            rootElement.Clear();
            rootElement.pickingMode = PickingMode.Position;

            // Container modal avec animation d'entrée
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.9f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de succès principale
            var successBox = new VisualElement();
            successBox.style.width = 600;
            successBox.style.maxWidth = Length.Percent(90);
            successBox.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            successBox.style.borderTopLeftRadius = 30;
            successBox.style.borderTopRightRadius = 30;
            successBox.style.borderBottomLeftRadius = 30;
            successBox.style.borderBottomRightRadius = 30;
            successBox.style.paddingTop = 50;
            successBox.style.paddingBottom = 50;
            successBox.style.paddingLeft = 50;
            successBox.style.paddingRight = 50;

            // Bordure lumineuse de succès
            successBox.style.borderTopWidth = 3;
            successBox.style.borderBottomWidth = 3;
            successBox.style.borderLeftWidth = 3;
            successBox.style.borderRightWidth = 3;
            successBox.style.borderTopColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderBottomColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderLeftColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderRightColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);

            // Bouton fermer (X) en haut à droite
            var closeButton = new Button(() => CloseCompletionUI());
            closeButton.text = "X";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 15;
            closeButton.style.right = 15;
            closeButton.style.width = 35;
            closeButton.style.height = 35;
            closeButton.style.fontSize = 24;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 17;
            closeButton.style.borderTopRightRadius = 17;
            closeButton.style.borderBottomLeftRadius = 17;
            closeButton.style.borderBottomRightRadius = 17;
            successBox.Add(closeButton);

            // Message de félicitations
            var congratsTitle = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Félicitations !"
                : "Congratulations!");
            congratsTitle.style.fontSize = 42;
            congratsTitle.style.color = new Color(0.1f, 0.9f, 0.5f, 1f);
            congratsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            congratsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            congratsTitle.style.marginBottom = 15;
            successBox.Add(congratsTitle);

            // Message de succès
            var successMessage = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Formation terminée avec succès !"
                : "Training completed successfully!");
            successMessage.style.fontSize = 24;
            successMessage.style.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            successMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
            successMessage.style.marginBottom = 30;
            successBox.Add(successMessage);

            // Ligne de séparation
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            separator.style.marginTop = 20;
            separator.style.marginBottom = 20;
            successBox.Add(separator);

            // Statistiques de la session
            var statsContainer = new VisualElement();
            statsContainer.style.marginBottom = 30;

            // Temps total
            var timeLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"⏱ Temps total : {FormatTime(totalTime)}"
                : $"⏱ Total time: {FormatTime(totalTime)}");
            timeLabel.style.fontSize = 18;
            timeLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            timeLabel.style.marginBottom = 10;
            statsContainer.Add(timeLabel);

            // Modules complétés
            var modulesLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"✅ Modules complétés : {totalInteractions}"
                : $"✅ Modules completed: {totalInteractions}");
            modulesLabel.style.fontSize = 18;
            modulesLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            modulesLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statsContainer.Add(modulesLabel);

            successBox.Add(statsContainer);

            // Score ou performance
            float finalScore = 100f;
            int analyticsInteractions = 0;
            int successfulInteractionsCount = 0;
            int failedInteractionsCount = 0;

            if (Analytics.TrainingAnalytics.Instance != null)
            {
                finalScore = Analytics.TrainingAnalytics.Instance.CalculateScore();
                analyticsInteractions = Analytics.TrainingAnalytics.Instance.GetTotalInteractions();
                successfulInteractionsCount = Analytics.TrainingAnalytics.Instance.GetSuccessfulInteractions();
                failedInteractionsCount = Analytics.TrainingAnalytics.Instance.GetFailedInteractions();
            }

            // Section détails des interactions (juste avant le score)
            if (analyticsInteractions > 0)
            {
                var interactionsDetailContainer = new VisualElement();
                interactionsDetailContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.4f);
                interactionsDetailContainer.style.borderTopLeftRadius = 10;
                interactionsDetailContainer.style.borderTopRightRadius = 10;
                interactionsDetailContainer.style.borderBottomLeftRadius = 10;
                interactionsDetailContainer.style.borderBottomRightRadius = 10;
                interactionsDetailContainer.style.paddingTop = 15;
                interactionsDetailContainer.style.paddingBottom = 15;
                interactionsDetailContainer.style.paddingLeft = 20;
                interactionsDetailContainer.style.paddingRight = 20;
                interactionsDetailContainer.style.marginBottom = 20;

                // Titre de la section
                var detailTitle = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Détails des interactions"
                    : "Interaction details");
                detailTitle.style.fontSize = 16;
                detailTitle.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                detailTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
                detailTitle.style.marginBottom = 10;
                detailTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                interactionsDetailContainer.Add(detailTitle);

                // Total interactions
                var totalInteractionsLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? $"Total : {analyticsInteractions}"
                    : $"Total: {analyticsInteractions}");
                totalInteractionsLabel.style.fontSize = 16;
                totalInteractionsLabel.style.color = new Color(0.75f, 0.75f, 0.8f, 1f);
                totalInteractionsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                totalInteractionsLabel.style.marginBottom = 5;
                interactionsDetailContainer.Add(totalInteractionsLabel);

                // Interactions réussies (du premier coup, sans erreur)
                var successfulLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? $"Réussies : {successfulInteractionsCount}"
                    : $"Successful: {successfulInteractionsCount}");
                successfulLabel.style.fontSize = 16;
                successfulLabel.style.color = new Color(0.1f, 0.85f, 0.45f, 1f);
                successfulLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                successfulLabel.style.marginBottom = 5;
                interactionsDetailContainer.Add(successfulLabel);

                // Interactions ratées (si > 0)
                if (failedInteractionsCount > 0)
                {
                    var failedLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                        ? $"Ratées : {failedInteractionsCount}"
                        : $"Failed: {failedInteractionsCount}");
                    failedLabel.style.fontSize = 16;
                    failedLabel.style.color = new Color(0.9f, 0.4f, 0.3f, 1f);
                    failedLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    interactionsDetailContainer.Add(failedLabel);
                }

                successBox.Add(interactionsDetailContainer);
            }

            var scoreContainer = new VisualElement();
            scoreContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
            scoreContainer.style.borderTopLeftRadius = 15;
            scoreContainer.style.borderTopRightRadius = 15;
            scoreContainer.style.borderBottomLeftRadius = 15;
            scoreContainer.style.borderBottomRightRadius = 15;
            scoreContainer.style.paddingTop = 20;
            scoreContainer.style.paddingBottom = 20;
            scoreContainer.style.marginBottom = 30;

            var scoreLabel = new Label($"{Mathf.RoundToInt(finalScore)}%");
            scoreLabel.style.fontSize = 48;

            // Couleur du score selon la performance
            if (finalScore >= 90f)
                scoreLabel.style.color = new Color(0.1f, 0.9f, 0.5f, 1f); // Vert
            else if (finalScore >= 70f)
                scoreLabel.style.color = new Color(0.9f, 0.7f, 0.1f, 1f); // Orange
            else
                scoreLabel.style.color = new Color(0.9f, 0.3f, 0.2f, 1f); // Rouge

            scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            scoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            scoreContainer.Add(scoreLabel);

            string scoreMessage = "";
            if (finalScore >= 100f)
            {
                scoreMessage = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Score parfait !"
                    : "Perfect score!";
            }
            else if (finalScore >= 90f)
            {
                scoreMessage = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Excellent travail !"
                    : "Excellent work!";
            }
            else if (finalScore >= 70f)
            {
                scoreMessage = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Bon travail !"
                    : "Good job!";
            }
            else
            {
                scoreMessage = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? "Peut mieux faire"
                    : "Room for improvement";
            }

            var scoreText = new Label(scoreMessage);
            scoreText.style.fontSize = 16;
            scoreText.style.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            scoreText.style.unityTextAlign = TextAnchor.MiddleCenter;
            scoreContainer.Add(scoreText);

            successBox.Add(scoreContainer);

            // Message informatif pour indiquer qu'on peut quitter ou explorer
            var infoMessage = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Vous pouvez maintenant fermer cette fenêtre pour explorer l'environnement 3D ou quitter la formation."
                : "You can now close this window to explore the 3D environment or quit the training.");
            infoMessage.style.fontSize = 14;
            infoMessage.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            infoMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
            infoMessage.style.marginTop = 20;
            infoMessage.style.paddingTop = 15;
            infoMessage.style.paddingBottom = 10;
            infoMessage.style.paddingLeft = 20;
            infoMessage.style.paddingRight = 20;
            infoMessage.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.5f);
            infoMessage.style.borderTopLeftRadius = 10;
            infoMessage.style.borderTopRightRadius = 10;
            infoMessage.style.borderBottomLeftRadius = 10;
            infoMessage.style.borderBottomRightRadius = 10;
            infoMessage.style.whiteSpace = WhiteSpace.Normal;
            successBox.Add(infoMessage);

            modalContainer.Add(successBox);
            rootElement.Add(modalContainer);

            // Animation d'entrée (scale)
            successBox.style.scale = new Scale(new Vector3(0.8f, 0.8f, 1f));
            successBox.RegisterCallback<GeometryChangedEvent>((evt) => {
                successBox.style.scale = new Scale(Vector3.one);
            });
        }

        void NotifyTrainingCompletion()
        {
            Debug.Log("[TrainingCompletionUI] Sending training completion notification...");

            // S'assurer que les analytics sont complètes avant l'envoi
            if (Analytics.TrainingAnalytics.Instance != null)
            {
                // Marquer la formation comme complétée si ce n'est pas déjà fait
                Analytics.TrainingAnalytics.Instance.CompleteTraining("completed");
                Debug.Log("[TrainingCompletionUI] Training marked as completed in analytics");
            }

            // Toujours essayer de notifier, peu importe le mode
            // Le notifier décidera lui-même s'il doit envoyer ou non
            var completionNotifier = FindFirstObjectByType<TrainingCompletionNotifier>();
            if (completionNotifier != null)
            {
                completionNotifier.FormationCompleted(SceneManager.GetActiveScene().name);
                Debug.Log("[TrainingCompletionUI] Completion notification sent");
            }
            else
            {
                Debug.Log("[TrainingCompletionUI] TrainingCompletionNotifier not found - training completed locally only");
            }
        }



        string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return $"{minutes:00}:{secs:00}";
        }

        /// <summary>
        /// Ferme l'UI de completion et réactive les contrôles du personnage
        /// </summary>
        void CloseCompletionUI()
        {
            Debug.Log("[TrainingCompletionUI] Closing completion UI - User can now explore");

            // Réactiver les contrôles du personnage
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(true);
            }

            // Nettoyer l'UI
            rootElement?.Clear();
            rootElement.pickingMode = PickingMode.Ignore;
        }
    }
}