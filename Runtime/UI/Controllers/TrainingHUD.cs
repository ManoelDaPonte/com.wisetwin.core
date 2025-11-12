using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// HUD minimaliste pour afficher le timer et la progression pendant la formation
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
        private Button nextScenarioButton;
        private Button helpButton;
        private Button resetButton;

        // Pulse effect
        private Coroutine pulseCoroutine;

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

            // Container principal - barre horizontale en haut (plus large et haute)
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
            hudContainer.style.paddingRight = 20;
            hudContainer.style.display = DisplayStyle.None;
            hudContainer.pickingMode = PickingMode.Position; // Allow picking buttons inside

            // ===== Section 1: Boutons utilitaires (help et reset) =====
            var utilitySection = new VisualElement();
            utilitySection.style.flexDirection = FlexDirection.Row;
            utilitySection.style.alignItems = Align.Center;
            utilitySection.style.marginRight = 12;

            // Bouton Help (?)
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
            helpButton.style.marginRight = 6;
            helpButton.style.paddingTop = 0;
            helpButton.style.paddingBottom = 0;
            helpButton.style.paddingLeft = 0;
            helpButton.style.paddingRight = 0;
            utilitySection.Add(helpButton);

            // Bouton Reset (↻)
            resetButton = new Button(() => OnResetButtonClicked());
            resetButton.text = "↻";
            resetButton.name = "reset-button";
            resetButton.style.width = 38;
            resetButton.style.height = 38;
            resetButton.style.fontSize = 22;
            resetButton.style.backgroundColor = new Color(0.5f, 0.3f, 0.2f, 0.8f);
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
            utilitySection.Add(resetButton);

            hudContainer.Add(utilitySection);

            // ===== Section 2: Barre de progression (plus large) =====
            var progressSection = new VisualElement();
            progressSection.style.flexGrow = 1;
            progressSection.style.flexDirection = FlexDirection.Column;
            progressSection.style.justifyContent = Justify.Center;
            progressSection.style.marginLeft = 5;
            progressSection.style.marginRight = 18;

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

            // ===== Section 3: Bouton "Next Scenario" avec icône play =====
            var buttonSection = new VisualElement();
            buttonSection.style.alignItems = Align.Center;

            // Bouton ">" (next icon)
            nextScenarioButton = new Button(() => OnNextScenarioButtonClicked());
            nextScenarioButton.name = "next-scenario-button";
            nextScenarioButton.text = ">"; // Next arrow icon (ASCII)
            nextScenarioButton.style.width = 38;
            nextScenarioButton.style.height = 38;
            nextScenarioButton.style.fontSize = 18;
            nextScenarioButton.style.backgroundColor = progressColor;
            nextScenarioButton.style.color = Color.white;
            nextScenarioButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            nextScenarioButton.style.borderTopLeftRadius = 19;
            nextScenarioButton.style.borderTopRightRadius = 19;
            nextScenarioButton.style.borderBottomLeftRadius = 19;
            nextScenarioButton.style.borderBottomRightRadius = 19;
            nextScenarioButton.style.paddingTop = 0;
            nextScenarioButton.style.paddingBottom = 0;
            nextScenarioButton.style.paddingLeft = 0;
            nextScenarioButton.style.paddingRight = 0;
            nextScenarioButton.SetEnabled(false); // Disabled by default

            // Style for disabled state
            nextScenarioButton.style.opacity = 0.5f;

            buttonSection.Add(nextScenarioButton);

            hudContainer.Add(buttonSection);

            root.Add(hudContainer);

            if (debugMode) Debug.Log("[TrainingHUD] HUD created with new design");
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

        // New scenario-based methods
        /// <summary>
        /// Called when a scenario starts - disables the next button and stops pulse
        /// </summary>
        public void OnScenarioStarted()
        {
            // Stop pulse effect while scenario is active
            StopPulseEffect();

            if (nextScenarioButton != null)
            {
                nextScenarioButton.SetEnabled(false);
                nextScenarioButton.style.opacity = 0.5f;

                if (debugMode) Debug.Log("[TrainingHUD] Next scenario button disabled (scenario in progress)");
            }
        }

        /// <summary>
        /// Called when a scenario is completed - increments progress, enables the next button, and starts pulse
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

            // Activer le bouton "Next Scenario"
            if (nextScenarioButton != null)
            {
                nextScenarioButton.SetEnabled(true);
                nextScenarioButton.style.opacity = 1f;

                // Start pulse effect to draw attention
                StartPulseEffect();

                if (debugMode) Debug.Log("[TrainingHUD] Next scenario button enabled with pulse effect");
            }
        }

        /// <summary>
        /// Called when all scenarios are completed - hides the next button
        /// </summary>
        public void OnAllScenariosCompleted()
        {
            if (nextScenarioButton != null)
            {
                nextScenarioButton.style.display = DisplayStyle.None;

                if (debugMode) Debug.Log("[TrainingHUD] All scenarios completed - button hidden");
            }

            // Show completion UI
            OnTrainingCompleted();
        }

        /// <summary>
        /// Called when next scenario button is clicked
        /// </summary>
        void OnNextScenarioButtonClicked()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Next scenario button clicked");

            // Stop pulse effect
            StopPulseEffect();

            // Disable button until next scenario is completed
            if (nextScenarioButton != null)
            {
                nextScenarioButton.SetEnabled(false);
                nextScenarioButton.style.opacity = 0.5f;
            }

            // Tell progression manager to move to next scenario
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.MoveToNextScenario();
            }
            else
            {
                Debug.LogError("[TrainingHUD] ProgressionManager.Instance is null!");
            }
        }

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
        /// Called when reset button (↻) is clicked
        /// </summary>
        void OnResetButtonClicked()
        {
            if (debugMode) Debug.Log("[TrainingHUD] Reset button clicked");

            // Reset player position via WiseTwinManager
            if (WiseTwinManager.Instance != null)
            {
                WiseTwinManager.Instance.ResetPlayerPosition();
            }
            else
            {
                Debug.LogWarning("[TrainingHUD] WiseTwinManager.Instance is null - cannot reset position");
            }
        }

        /// <summary>
        /// Start pulse effect on the next scenario button
        /// </summary>
        void StartPulseEffect()
        {
            if (nextScenarioButton == null) return;

            // Stop existing pulse if any
            StopPulseEffect();

            // Start new pulse coroutine
            pulseCoroutine = StartCoroutine(PulseCoroutine());

            if (debugMode) Debug.Log("[TrainingHUD] Pulse effect started");
        }

        /// <summary>
        /// Stop pulse effect on the next scenario button
        /// </summary>
        void StopPulseEffect()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;

                // Reset scale to normal
                if (nextScenarioButton != null)
                {
                    nextScenarioButton.style.scale = new Scale(Vector3.one);
                }

                if (debugMode) Debug.Log("[TrainingHUD] Pulse effect stopped");
            }
        }

        /// <summary>
        /// Coroutine that animates the pulse effect
        /// </summary>
        IEnumerator PulseCoroutine()
        {
            float pulseSpeed = 1f; // 1 second per cycle
            float pulseAmount = 0.08f; // ±8% scale

            while (true)
            {
                float time = Time.time * pulseSpeed;
                float scale = 1f + Mathf.Sin(time * Mathf.PI * 2f) * pulseAmount;

                if (nextScenarioButton != null)
                {
                    nextScenarioButton.style.scale = new Scale(new Vector3(scale, scale, 1f));
                }

                yield return null;
            }
        }

        /// <summary>
        /// Get localized text for the UI
        /// </summary>
        string GetLocalizedText(string key)
        {
            // Note: next_scenario uses ASCII arrow (>) instead of text for better UX
            return key switch
            {
                "next_scenario" => ">",
                _ => key
            };
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

                // Enable the button at the start so user can begin the first scenario
                if (nextScenarioButton != null)
                {
                    nextScenarioButton.SetEnabled(true);
                    nextScenarioButton.style.opacity = 1f;

                    // Start pulse effect to indicate user can begin
                    StartPulseEffect();
                }

                // Update button text with current language
                UpdateButtonText();

                if (debugMode) Debug.Log($"[TrainingHUD] Initialized for {totalScenarios} scenarios");
            }
        }

        /// <summary>
        /// Called when language changes - updates UI text
        /// </summary>
        void OnLanguageChanged(string newLanguage)
        {
            UpdateButtonText();
            if (debugMode) Debug.Log($"[TrainingHUD] Language changed to: {newLanguage}, button text updated");
        }

        /// <summary>
        /// Update button text based on current language
        /// </summary>
        void UpdateButtonText()
        {
            if (nextScenarioButton != null)
            {
                nextScenarioButton.text = GetLocalizedText("next_scenario");
            }
        }
    }
}