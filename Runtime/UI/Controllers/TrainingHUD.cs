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

            // Container principal - barre horizontale en haut
            hudContainer = new VisualElement();
            hudContainer.name = "training-hud";
            hudContainer.style.position = Position.Absolute;
            hudContainer.style.top = 10;
            hudContainer.style.left = Length.Percent(50);
            hudContainer.style.translate = new Translate(-200, 0);
            hudContainer.style.width = 400;
            hudContainer.style.height = 45;
            hudContainer.style.backgroundColor = backgroundColor;
            hudContainer.style.borderTopLeftRadius = 22;
            hudContainer.style.borderTopRightRadius = 22;
            hudContainer.style.borderBottomLeftRadius = 22;
            hudContainer.style.borderBottomRightRadius = 22;
            hudContainer.style.flexDirection = FlexDirection.Row;
            hudContainer.style.alignItems = Align.Center;
            hudContainer.style.paddingLeft = 20;
            hudContainer.style.paddingRight = 20;
            hudContainer.style.display = DisplayStyle.None;
            hudContainer.pickingMode = PickingMode.Ignore;

            // Section gauche - Timer
            var timerSection = new VisualElement();
            timerSection.style.flexDirection = FlexDirection.Row;
            timerSection.style.alignItems = Align.Center;
            timerSection.style.width = Length.Percent(30);

            // Icône timer
            var timerIcon = new Label("⏱");
            timerIcon.style.fontSize = 18;
            timerIcon.style.marginRight = 8;
            timerIcon.style.color = textColor;
            timerSection.Add(timerIcon);

            // Label timer
            timerLabel = new Label("00:00");
            timerLabel.style.fontSize = 16;
            timerLabel.style.color = textColor;
            timerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            timerSection.Add(timerLabel);

            hudContainer.Add(timerSection);

            // Section centre - Barre de progression
            var progressSection = new VisualElement();
            progressSection.style.flexGrow = 1;
            progressSection.style.flexDirection = FlexDirection.Column;
            progressSection.style.justifyContent = Justify.Center;
            progressSection.style.marginLeft = 15;
            progressSection.style.marginRight = 15;

            // Label de progression
            progressLabel = new Label("0 / 0");
            progressLabel.style.fontSize = 12;
            progressLabel.style.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f);
            progressLabel.style.marginBottom = 4;
            progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            progressSection.Add(progressLabel);

            // Barre de progression
            progressBar = new VisualElement();
            progressBar.style.height = 6;
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

            // Section droite - Bouton "Scénario suivant"
            var buttonSection = new VisualElement();
            buttonSection.style.marginLeft = 10;
            buttonSection.style.alignItems = Align.Center;

            // Bouton "Scénario suivant"
            nextScenarioButton = new Button(() => OnNextScenarioButtonClicked());
            nextScenarioButton.name = "next-scenario-button";
            nextScenarioButton.text = GetLocalizedText("next_scenario");
            nextScenarioButton.style.height = 32;
            nextScenarioButton.style.paddingLeft = 15;
            nextScenarioButton.style.paddingRight = 15;
            nextScenarioButton.style.fontSize = 14;
            nextScenarioButton.style.backgroundColor = progressColor;
            nextScenarioButton.style.color = Color.white;
            nextScenarioButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            nextScenarioButton.style.borderTopLeftRadius = 16;
            nextScenarioButton.style.borderTopRightRadius = 16;
            nextScenarioButton.style.borderBottomLeftRadius = 16;
            nextScenarioButton.style.borderBottomRightRadius = 16;
            nextScenarioButton.SetEnabled(false); // Disabled by default

            // Style for disabled state
            nextScenarioButton.style.opacity = 0.5f;

            buttonSection.Add(nextScenarioButton);

            hudContainer.Add(buttonSection);

            root.Add(hudContainer);

            if (debugMode) Debug.Log("[TrainingHUD] HUD created");
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
        /// Called when a scenario starts - disables the next button
        /// </summary>
        public void OnScenarioStarted()
        {
            if (nextScenarioButton != null)
            {
                nextScenarioButton.SetEnabled(false);
                nextScenarioButton.style.opacity = 0.5f;

                if (debugMode) Debug.Log("[TrainingHUD] Next scenario button disabled (scenario in progress)");
            }
        }

        /// <summary>
        /// Called when a scenario is completed - increments progress and enables the next button
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

                if (debugMode) Debug.Log("[TrainingHUD] Next scenario button enabled");
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
        /// Get localized text for the UI
        /// </summary>
        string GetLocalizedText(string key)
        {
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            if (lang == "fr")
            {
                return key switch
                {
                    "next_scenario" => "Scénario suivant ▶",
                    _ => key
                };
            }
            else
            {
                return key switch
                {
                    "next_scenario" => "Next Scenario ▶",
                    _ => key
                };
            }
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