using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// HUD minimaliste pour afficher le timer et la progression pendant la formation
    /// Layout: [restart (rouge)] [barre de progression] [help (?)]
    /// </summary>
    public class TrainingHUD : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private float fadeInDuration = 0.5f;

        [Header("Style")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        [SerializeField] private Color progressColor = new Color(0.1f, 0.8f, 0.6f, 1f);
        [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // UI Elements
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement hudContainer;
        private Label timerLabel;
        private Label progressLabel;
        private VisualElement progressBar;
        private VisualElement progressFill;
        private Button helpButton;
        private Button resetButton;
        private VisualElement confirmationOverlay;

        // State
        private float startTime;
        private int currentProgress = 0;
        private int totalObjects = 0;
        private bool isVisible = false;
        private HashSet<string> completedObjects = new HashSet<string>(); // Pour éviter la triche

        // Singleton
        public static TrainingHUD Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Ne pas appliquer DontDestroyOnLoad si on est dans WiseTwinSystem
                // C'est le parent WiseTwinSystem qui gère la persistance
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                // Pas de warning si on est enfant de WiseTwinSystem
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Setup UIDocument
            SetupUIDocument();

            // S'abonner aux changements de langue
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        void OnDestroy()
        {
            // Se désabonner des événements
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            if (showOnStart)
            {
                Show();
            }
        }

        void SetupUIDocument()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[TrainingHUD] PanelSettings is null! Please assign it in the inspector.");
            }

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[TrainingHUD] Root visual element is null!");
                return;
            }

            CreateHUD();
        }

        void CreateHUD()
        {
            // Clear root
            root.Clear();
            root.pickingMode = PickingMode.Ignore; // Ne pas bloquer les clics

            // Container principal - barre horizontale en haut
            hudContainer = new VisualElement();
            hudContainer.name = "training-hud";
            hudContainer.style.position = Position.Absolute;
            hudContainer.style.top = 10;
            hudContainer.style.left = Length.Percent(50);
            hudContainer.style.translate = new Translate(-325, 0); // 650/2
            hudContainer.style.width = 650;
            hudContainer.style.height = 52;
            hudContainer.style.backgroundColor = backgroundColor;
            hudContainer.style.borderTopLeftRadius = 26;
            hudContainer.style.borderTopRightRadius = 26;
            hudContainer.style.borderBottomLeftRadius = 26;
            hudContainer.style.borderBottomRightRadius = 26;
            hudContainer.style.flexDirection = FlexDirection.Row;
            hudContainer.style.alignItems = Align.Center;
            hudContainer.style.paddingLeft = 12;
            hudContainer.style.paddingRight = 12;
            hudContainer.style.display = DisplayStyle.None;
            hudContainer.pickingMode = PickingMode.Position; // Allow picking buttons inside

            // ===== Section 1: Bouton Restart (gauche, rouge) =====
            resetButton = new Button(() => OnResetButtonClicked());
            resetButton.text = "\u21BB";
            resetButton.name = "reset-button";
            resetButton.style.width = 38;
            resetButton.style.height = 38;
            resetButton.style.fontSize = 22;
            resetButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            resetButton.style.color = Color.white;
            resetButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            resetButton.style.borderTopLeftRadius = 19;
            resetButton.style.borderTopRightRadius = 19;
            resetButton.style.borderBottomLeftRadius = 19;
            resetButton.style.borderBottomRightRadius = 19;
            resetButton.style.paddingTop = 0;
            resetButton.style.paddingBottom = 0;
            resetButton.style.paddingLeft = 0;
            resetButton.style.paddingRight = 0;
            resetButton.style.marginRight = 12;
            hudContainer.Add(resetButton);

            // ===== Section 2: Barre de progression (centre, flex grow) =====
            var progressSection = new VisualElement();
            progressSection.style.flexGrow = 1;
            progressSection.style.flexDirection = FlexDirection.Column;
            progressSection.style.justifyContent = Justify.Center;

            // Label de progression
            progressLabel = new Label("0 / 0");
            progressLabel.style.fontSize = 12;
            progressLabel.style.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f);
            progressLabel.style.marginBottom = 4;
            progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            progressSection.Add(progressLabel);

            // Barre de progression
            progressBar = new VisualElement();
            progressBar.style.height = 7;
            progressBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);
            progressBar.style.borderTopLeftRadius = 3;
            progressBar.style.borderTopRightRadius = 3;
            progressBar.style.borderBottomLeftRadius = 3;
            progressBar.style.borderBottomRightRadius = 3;

            // Remplissage de la barre
            progressFill = new VisualElement();
            progressFill.style.height = Length.Percent(100);
            progressFill.style.width = Length.Percent(0);
            progressFill.style.backgroundColor = progressColor;
            progressFill.style.borderTopLeftRadius = 3;
            progressFill.style.borderTopRightRadius = 3;
            progressFill.style.borderBottomLeftRadius = 3;
            progressFill.style.borderBottomRightRadius = 3;
            progressBar.Add(progressFill);

            progressSection.Add(progressBar);
            hudContainer.Add(progressSection);

            // ===== Section 3: Bouton Help (droite) =====
            helpButton = new Button(() => OnHelpButtonClicked());
            helpButton.text = "?";
            helpButton.name = "help-button";
            helpButton.style.width = 38;
            helpButton.style.height = 38;
            helpButton.style.fontSize = 20;
            helpButton.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
            helpButton.style.color = Color.white;
            helpButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            helpButton.style.borderTopLeftRadius = 19;
            helpButton.style.borderTopRightRadius = 19;
            helpButton.style.borderBottomLeftRadius = 19;
            helpButton.style.borderBottomRightRadius = 19;
            helpButton.style.paddingTop = 0;
            helpButton.style.paddingBottom = 0;
            helpButton.style.paddingLeft = 0;
            helpButton.style.paddingRight = 0;
            helpButton.style.marginLeft = 12;
            hudContainer.Add(helpButton);

            root.Add(hudContainer);

            // Confirmation dialog (hidden by default)
            CreateRestartConfirmationDialog();

            if (debugMode) Debug.Log("[TrainingHUD] HUD created");
        }

        void CreateRestartConfirmationDialog()
        {
            confirmationOverlay = new VisualElement();
            confirmationOverlay.name = "restart-confirmation";
            confirmationOverlay.style.position = Position.Absolute;
            confirmationOverlay.style.width = Length.Percent(100);
            confirmationOverlay.style.height = Length.Percent(100);
            confirmationOverlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            confirmationOverlay.style.alignItems = Align.Center;
            confirmationOverlay.style.justifyContent = Justify.Center;
            confirmationOverlay.style.display = DisplayStyle.None;
            confirmationOverlay.pickingMode = PickingMode.Position;

            var dialog = new VisualElement();
            dialog.style.width = 420;
            dialog.style.paddingTop = 30;
            dialog.style.paddingBottom = 30;
            dialog.style.paddingLeft = 40;
            dialog.style.paddingRight = 40;
            dialog.style.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.98f);
            dialog.style.borderTopLeftRadius = 15;
            dialog.style.borderTopRightRadius = 15;
            dialog.style.borderBottomLeftRadius = 15;
            dialog.style.borderBottomRightRadius = 15;
            dialog.style.borderTopWidth = 2;
            dialog.style.borderBottomWidth = 2;
            dialog.style.borderLeftWidth = 2;
            dialog.style.borderRightWidth = 2;
            dialog.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            dialog.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            dialog.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            dialog.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            dialog.style.alignItems = Align.Center;

            var warningIcon = new Label("\u26A0");
            warningIcon.style.fontSize = 40;
            warningIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            warningIcon.style.marginBottom = 15;
            dialog.Add(warningIcon);

            var messageLabel = new Label();
            messageLabel.name = "restart-message";
            messageLabel.style.fontSize = 18;
            messageLabel.style.color = Color.white;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.marginBottom = 25;
            dialog.Add(messageLabel);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            var cancelButton = new Button(() => HideRestartConfirmation());
            cancelButton.name = "cancel-restart-button";
            cancelButton.style.width = 140;
            cancelButton.style.height = 45;
            cancelButton.style.fontSize = 16;
            cancelButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 0.9f);
            cancelButton.style.color = Color.white;
            cancelButton.style.borderTopLeftRadius = 8;
            cancelButton.style.borderTopRightRadius = 8;
            cancelButton.style.borderBottomLeftRadius = 8;
            cancelButton.style.borderBottomRightRadius = 8;
            cancelButton.style.marginRight = 15;
            buttonRow.Add(cancelButton);

            var restartButton = new Button(() => ConfirmRestart());
            restartButton.name = "confirm-restart-button";
            restartButton.style.width = 140;
            restartButton.style.height = 45;
            restartButton.style.fontSize = 16;
            restartButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            restartButton.style.color = Color.white;
            restartButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            restartButton.style.borderTopLeftRadius = 8;
            restartButton.style.borderTopRightRadius = 8;
            restartButton.style.borderBottomLeftRadius = 8;
            restartButton.style.borderBottomRightRadius = 8;
            buttonRow.Add(restartButton);

            dialog.Add(buttonRow);
            confirmationOverlay.Add(dialog);
            root.Add(confirmationOverlay);

            UpdateConfirmationTexts();
        }

        void UpdateConfirmationTexts()
        {
            if (confirmationOverlay == null) return;

            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            var messageLabel = confirmationOverlay.Q<Label>("restart-message");
            if (messageLabel != null)
            {
                messageLabel.text = lang == "fr"
                    ? "Voulez-vous vraiment recommencer ?\nToute la progression sera perdue."
                    : "Are you sure you want to restart?\nAll progress will be lost.";
            }

            var cancelBtn = confirmationOverlay.Q<Button>("cancel-restart-button");
            if (cancelBtn != null)
            {
                cancelBtn.text = lang == "fr" ? "Annuler" : "Cancel";
            }

            var restartBtn = confirmationOverlay.Q<Button>("confirm-restart-button");
            if (restartBtn != null)
            {
                restartBtn.text = lang == "fr" ? "Recommencer" : "Restart";
            }
        }

        public void Show()
        {
            if (hudContainer == null) return;

            isVisible = true;
            hudContainer.style.display = DisplayStyle.Flex;
            StartCoroutine(FadeIn());
            startTime = Time.time;

            if (debugMode) Debug.Log("[TrainingHUD] HUD shown");
        }

        public void Hide()
        {
            if (hudContainer == null) return;

            isVisible = false;
            StartCoroutine(FadeOut());

            if (debugMode) Debug.Log("[TrainingHUD] HUD hidden");
        }

        public void SetTotalObjects(int total)
        {
            totalObjects = total;
            UpdateProgressDisplay();

            if (debugMode) Debug.Log($"[TrainingHUD] Total objects set to {total}");
        }

        public void UpdateProgress(int completed)
        {
            currentProgress = completed;
            UpdateProgressDisplay();
        }

        public void IncrementProgress()
        {
            // Méthode legacy sans ID d'objet (pour compatibilité)
            IncrementProgressForObject(null);
        }

        public void IncrementProgressForObject(string objectId)
        {
            // Si on a un ID d'objet, vérifier qu'il n'a pas déjà été complété
            if (!string.IsNullOrEmpty(objectId))
            {
                if (completedObjects.Contains(objectId))
                {
                    Debug.LogWarning($"[TrainingHUD] Object {objectId} already completed - ignoring to prevent cheating");
                    return;
                }
                completedObjects.Add(objectId);
            }

            // Ne pas incrémenter si on a déjà atteint le maximum
            if (currentProgress >= totalObjects)
            {
                Debug.LogWarning($"[TrainingHUD] Progress already at maximum ({currentProgress}/{totalObjects})");
                return;
            }

            currentProgress++;
            UpdateProgressDisplay();

            if (debugMode)
            {
                Debug.Log($"[TrainingHUD] Progress: {currentProgress}/{totalObjects} (Object: {objectId ?? "unknown"})");
            }

            // Vérifier si on a terminé tous les modules
            if (currentProgress >= totalObjects && totalObjects > 0)
            {
                OnTrainingCompleted();
            }
        }

        void UpdateProgressDisplay()
        {
            if (progressLabel != null)
            {
                progressLabel.text = $"{currentProgress} / {totalObjects}";
            }

            if (progressFill != null && totalObjects > 0)
            {
                float percentage = (float)currentProgress / totalObjects * 100f;
                // S'assurer que le pourcentage ne dépasse pas 100%
                percentage = Mathf.Clamp(percentage, 0f, 100f);
                progressFill.style.width = Length.Percent(percentage);

                // Changer la couleur quand c'est terminé
                if (currentProgress >= totalObjects)
                {
                    progressFill.style.backgroundColor = new Color(0.2f, 0.9f, 0.4f, 1f);
                }
            }
        }

        void Update()
        {
            if (!isVisible || timerLabel == null) return;

            // Mettre à jour le timer
            float elapsed = Time.time - startTime;
            int minutes = Mathf.FloorToInt(elapsed / 60);
            int seconds = Mathf.FloorToInt(elapsed % 60);
            timerLabel.text = $"{minutes:00}:{seconds:00}";
        }

        IEnumerator FadeIn()
        {
            hudContainer.style.opacity = 0;
            float elapsed = 0;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float opacity = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
                hudContainer.style.opacity = opacity;
                yield return null;
            }

            hudContainer.style.opacity = 1;
        }

        IEnumerator FadeOut()
        {
            float elapsed = 0;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float opacity = Mathf.Lerp(1, 0, elapsed / fadeInDuration);
                hudContainer.style.opacity = opacity;
                yield return null;
            }

            hudContainer.style.opacity = 0;
            hudContainer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// DEPRECATED: Auto-detection is no longer needed - ProgressionManager automatically sets the total from metadata
        /// </summary>
        [System.Obsolete("AutoDetectInteractables is deprecated. ProgressionManager automatically initializes the total from metadata scenarios.")]
        public void AutoDetectInteractables()
        {
            Debug.LogWarning("[TrainingHUD] AutoDetectInteractables is deprecated. The total is automatically set by ProgressionManager from metadata.");
        }

        // Pour les tests
        [ContextMenu("Test Show HUD")]
        public void TestShow()
        {
            SetTotalObjects(5);
            Show();
        }

        [ContextMenu("Test Increment Progress")]
        public void TestIncrement()
        {
            IncrementProgress();
        }

        void OnTrainingCompleted()
        {
            if (debugMode) Debug.Log($"[TrainingHUD] Training completed! {currentProgress}/{totalObjects} modules done");

            // Calculer le temps total
            float totalTime = Time.time - startTime;

            // S'assurer que TrainingAnalytics existe avant de créer l'UI de complétion
            if (Analytics.TrainingAnalytics.Instance == null)
            {
                var analyticsGO = new GameObject("TrainingAnalytics");
                analyticsGO.AddComponent<Analytics.TrainingAnalytics>();
            }

            // Chercher ou créer l'UI de complétion
            var completionUI = FindFirstObjectByType<UI.TrainingCompletionUI>();
            if (completionUI == null)
            {
                // Créer l'UI de complétion s'il n'existe pas
                GameObject completionGO = new GameObject("TrainingCompletionUI");
                completionUI = completionGO.AddComponent<UI.TrainingCompletionUI>();

                // Ajouter UIDocument
                var uiDoc = completionGO.AddComponent<UIDocument>();

                // Essayer de trouver et assigner le PanelSettings du HUD actuel
                if (uiDocument != null && uiDocument.panelSettings != null)
                {
                    uiDoc.panelSettings = uiDocument.panelSettings;
                    if (debugMode) Debug.Log("[TrainingHUD] PanelSettings assigned to TrainingCompletionUI from TrainingHUD");
                }
            }

            // IMPORTANT: Afficher l'écran de complétion avec les statistiques
            if (completionUI != null)
            {
                completionUI.ShowCompletionScreen(totalTime, totalObjects);
                if (debugMode) Debug.Log("[TrainingHUD] Completion screen displayed successfully");
            }
            else
            {
                Debug.LogError("[TrainingHUD] Failed to create or find TrainingCompletionUI!");
            }
        }

        #region Scenario Methods

        /// <summary>
        /// Called when a scenario starts
        /// </summary>
        public void OnScenarioStarted()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Scenario started");
        }

        /// <summary>
        /// Called when a scenario is completed - increments progress
        /// </summary>
        public void OnScenarioCompleted()
        {
            // Incrémenter le compteur de progression
            if (currentProgress < totalObjects)
            {
                currentProgress++;
                UpdateProgressDisplay();

                if (debugMode) Debug.Log($"[TrainingHUD] Progress incremented: {currentProgress}/{totalObjects}");
            }
        }

        /// <summary>
        /// Legacy method - no-op since next button was removed.
        /// Transitions between scenarios are handled by ScenarioTransitionPanel.
        /// </summary>
        public void SetNextButtonVisible(bool visible)
        {
            // No-op
        }

        /// <summary>
        /// Called when all scenarios are completed
        /// </summary>
        public void OnAllScenariosCompleted()
        {
            if (debugMode) Debug.Log("[TrainingHUD] All scenarios completed");

            // Show completion UI
            OnTrainingCompleted();
        }

        /// <summary>
        /// Initialize for scenario-based progression
        /// </summary>
        public void InitializeForScenarios()
        {
            if (ProgressionManager.Instance != null)
            {
                int totalScenarios = ProgressionManager.Instance.TotalScenarios;
                SetTotalObjects(totalScenarios);

                if (debugMode) Debug.Log($"[TrainingHUD] Initialized for {totalScenarios} scenarios");
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// Called when help button (?) is clicked
        /// </summary>
        void OnHelpButtonClicked()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Help button clicked");

            // Show tutorial UI
            if (TutorialUI.Instance != null)
            {
                TutorialUI.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[TrainingHUD] TutorialUI.Instance is null - cannot show tutorial");
            }
        }

        /// <summary>
        /// Called when restart button is clicked - shows confirmation dialog
        /// </summary>
        void OnResetButtonClicked()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Restart button clicked - showing confirmation");
            ShowRestartConfirmation();
        }

        void ShowRestartConfirmation()
        {
            UpdateConfirmationTexts();

            if (confirmationOverlay != null)
            {
                confirmationOverlay.style.display = DisplayStyle.Flex;
            }

            // Bloquer les contrôles du personnage
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(false);
            }
        }

        void HideRestartConfirmation()
        {
            if (confirmationOverlay != null)
            {
                confirmationOverlay.style.display = DisplayStyle.None;
            }

            // Réactiver les contrôles du personnage
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(true);
            }
        }

        void ConfirmRestart()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Restart confirmed - reloading scene");

            // Destroy the entire WiseTwinSystem hierarchy (includes this TrainingHUD and all singletons)
            var rootGO = transform.root.gameObject;
            Destroy(rootGO);

            // Destroy any other persistent objects not under WiseTwinSystem
            var transitionPanel = FindFirstObjectByType<ScenarioTransitionPanel>();
            if (transitionPanel != null) Destroy(transitionPanel.gameObject);

            // Reload the current scene for a clean restart
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion

        /// <summary>
        /// Called when language changes - updates confirmation dialog texts
        /// </summary>
        void OnLanguageChanged(string newLanguage)
        {
            UpdateConfirmationTexts();
            if (debugMode) Debug.Log($"[TrainingHUD] Language changed to: {newLanguage}");
        }
    }
}
