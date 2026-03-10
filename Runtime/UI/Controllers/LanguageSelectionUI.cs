using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using WiseTwin.UI;

namespace WiseTwin
{
    /// <summary>
    /// Gère l'UI de sélection de langue et l'écran d'informations de formation.
    /// Étape 1 : Choix de la langue
    /// Étape 2 : Informations de formation et disclaimer
    /// Étape 3 : Tutorial (via TutorialUI)
    /// Étape 4 : Start button
    /// </summary>
    public class LanguageSelectionUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private float animationDuration = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // UI Document
        private UIDocument uiDocument;
        private VisualElement root;

        // Panels
        private VisualElement languageSelectionPanel;
        private VisualElement disclaimerPanel;
        private VisualElement startPanel;

        // Références
        private LocalizationManager localizationManager;
        private WiseTwinManager wiseTwinManager;
        private WiseTwinUIManager uiManager;
        private TutorialUI tutorialUI;

        // Métadonnées
        private Dictionary<string, object> trainingMetadata;

        // État
        private string selectedLanguage = "";
        public bool IsDisplaying { get; private set; } = false;
        private bool isInitialized = false;

        // Events
        public System.Action<string> OnLanguageSelected;
        public System.Action OnTrainingStarted;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[LanguageSelectionUI] PanelSettings is null! Please assign it in the inspector.");
            }

            if (uiDocument.visualTreeAsset != null)
            {
                uiDocument.visualTreeAsset = null;
            }
        }

        void Start()
        {
            localizationManager = LocalizationManager.Instance;
            wiseTwinManager = WiseTwinManager.Instance;
            uiManager = WiseTwinUIManager.Instance;

            tutorialUI = FindAnyObjectByType<TutorialUI>();
            if (tutorialUI == null)
            {
                var tutorialGO = new GameObject("TutorialUI");
                tutorialUI = tutorialGO.AddComponent<TutorialUI>();

                if (uiDocument != null && uiDocument.panelSettings != null)
                {
                    tutorialUI.SetPanelSettings(uiDocument.panelSettings);
                }

                if (debugMode) Debug.Log("[LanguageSelectionUI] TutorialUI created");
            }

            if (tutorialUI != null)
            {
                tutorialUI.OnTutorialCompleted += OnTutorialCompleted;
            }

            if (showOnStart)
            {
                Initialize();
                ShowLanguageSelection();
                StartCoroutine(LoadMetadataWhenReady());
            }
        }

        IEnumerator LoadMetadataWhenReady()
        {
            SetLanguageButtonsEnabled(false);

            int waitFrames = 0;
            const int maxWaitFrames = 300;

            while (wiseTwinManager == null || !wiseTwinManager.IsMetadataLoaded)
            {
                yield return null;
                waitFrames++;

                if (wiseTwinManager == null)
                {
                    wiseTwinManager = WiseTwinManager.Instance;
                }

                if (waitFrames >= maxWaitFrames)
                {
                    Debug.LogError("[LanguageSelectionUI] Timeout waiting for metadata!");
                    SetLanguageButtonsEnabled(true);
                    yield break;
                }
            }

            trainingMetadata = wiseTwinManager.MetadataLoader.GetMetadata();
            SetLanguageButtonsEnabled(true);
        }

        void SetLanguageButtonsEnabled(bool enabled)
        {
            if (languageSelectionPanel == null) return;

            var englishButton = languageSelectionPanel.Q<Button>("lang-button-en");
            var frenchButton = languageSelectionPanel.Q<Button>("lang-button-fr");

            if (englishButton != null) englishButton.SetEnabled(enabled);
            if (frenchButton != null) frenchButton.SetEnabled(enabled);
        }

        void Initialize()
        {
            if (isInitialized) return;

            if (uiDocument == null)
            {
                Debug.LogError("[LanguageSelectionUI] UIDocument is null!");
                return;
            }

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[LanguageSelectionUI] Root visual element is null!");
                return;
            }

            root.pickingMode = PickingMode.Position;
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.Clear();

            CreateLanguageSelectionPanel();
            CreateDisclaimerPanel();
            CreateStartPanel();

            isInitialized = true;
        }

        // =====================================================================
        // PANEL 1: Language Selection
        // =====================================================================

        void CreateLanguageSelectionPanel()
        {
            languageSelectionPanel = new VisualElement();
            languageSelectionPanel.name = "language-selection-panel";
            UIStyles.ApplyBackdropHeavyStyle(languageSelectionPanel);
            languageSelectionPanel.style.display = DisplayStyle.None;

            var card = new VisualElement();
            card.style.width = 460;
            card.style.maxWidth = Length.Percent(90);
            UIStyles.ApplyCardStyle(card, UIStyles.RadiusXL);
            UIStyles.SetPadding(card, UIStyles.Space3XL);

            // Language buttons
            var buttonsContainer = new VisualElement();
            buttonsContainer.style.width = Length.Percent(100);

            buttonsContainer.Add(CreateLanguageButton("Français", "fr"));

            var spacer = new VisualElement();
            spacer.style.height = UIStyles.SpaceMD;
            buttonsContainer.Add(spacer);

            buttonsContainer.Add(CreateLanguageButton("English", "en"));

            card.Add(buttonsContainer);
            languageSelectionPanel.Add(card);
            root.Add(languageSelectionPanel);
        }

        Button CreateLanguageButton(string label, string langCode)
        {
            var button = new Button(() => OnLanguageButtonClicked(langCode));
            button.name = $"lang-button-{langCode}";

            button.style.width = Length.Percent(100);
            button.style.height = 58;
            button.style.backgroundColor = UIStyles.BgInput;
            UIStyles.SetBorderRadius(button, UIStyles.RadiusMD);
            UIStyles.SetBorderWidth(button, 2);
            UIStyles.SetBorderColor(button, UIStyles.BorderDefault);
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.paddingLeft = UIStyles.SpaceXL;
            button.style.paddingRight = UIStyles.SpaceXL;

            var langLabel = new Label(label);
            langLabel.style.fontSize = UIStyles.FontLG;
            langLabel.style.color = UIStyles.TextPrimary;
            langLabel.style.flexGrow = 1;
            langLabel.pickingMode = PickingMode.Ignore;
            button.Add(langLabel);

            // Arrow indicator
            var arrow = new Label("\u203A");
            arrow.style.fontSize = UIStyles.FontXL;
            arrow.style.color = UIStyles.TextMuted;
            arrow.pickingMode = PickingMode.Ignore;
            button.Add(arrow);

            button.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                button.style.backgroundColor = UIStyles.BgInputHover;
                UIStyles.SetBorderColor(button, UIStyles.Accent);
                arrow.style.color = UIStyles.Accent;
            });

            button.RegisterCallback<MouseLeaveEvent>((evt) =>
            {
                button.style.backgroundColor = UIStyles.BgInput;
                UIStyles.SetBorderColor(button, UIStyles.BorderDefault);
                arrow.style.color = UIStyles.TextMuted;
            });

            return button;
        }

        // =====================================================================
        // PANEL 2: Disclaimer / Info
        // =====================================================================

        void CreateDisclaimerPanel()
        {
            disclaimerPanel = new VisualElement();
            disclaimerPanel.name = "disclaimer-panel";
            UIStyles.ApplyBackdropHeavyStyle(disclaimerPanel);
            disclaimerPanel.style.backgroundColor = UIStyles.BgDeep;
            disclaimerPanel.style.display = DisplayStyle.None;

            var card = new VisualElement();
            card.style.width = 850;
            card.style.maxWidth = Length.Percent(90);
            card.style.maxHeight = Length.Percent(90);
            UIStyles.ApplyCardStyle(card, UIStyles.RadiusXL);
            card.style.paddingTop = UIStyles.Space4XL;
            card.style.paddingBottom = UIStyles.Space2XL;
            card.style.paddingLeft = UIStyles.Space4XL;
            card.style.paddingRight = UIStyles.Space4XL;

            // ScrollView
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            scrollView.RegisterCallback<AttachToPanelEvent>(evt => UIStyles.ApplyMinimalScrollbar(scrollView));
            scrollView.RegisterCallback<GeometryChangedEvent>(evt => UIStyles.ApplyMinimalScrollbar(scrollView));

            // Title
            var titleLabel = UIStyles.CreateTitle("", UIStyles.Font3XL);
            titleLabel.name = "training-title";
            titleLabel.style.marginBottom = UIStyles.SpaceLG;
            scrollView.Add(titleLabel);

            // Description
            var descLabel = UIStyles.CreateBodyText("", UIStyles.FontMD);
            descLabel.name = "training-description";
            descLabel.style.color = UIStyles.TextSecondary;
            descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descLabel.style.marginBottom = UIStyles.SpaceXL;
            scrollView.Add(descLabel);

            // Info badges row
            var infoRow = new VisualElement();
            infoRow.style.flexDirection = FlexDirection.Row;
            infoRow.style.justifyContent = Justify.Center;
            infoRow.style.marginBottom = UIStyles.SpaceXL;

            var durationLabel = UIStyles.CreateMutedText("", UIStyles.FontBase);
            durationLabel.name = "training-duration";
            durationLabel.style.color = UIStyles.TextSecondary;
            durationLabel.style.marginRight = UIStyles.Space2XL;
            infoRow.Add(durationLabel);

            var difficultyLabel = UIStyles.CreateMutedText("", UIStyles.FontBase);
            difficultyLabel.name = "training-difficulty";
            difficultyLabel.style.color = UIStyles.TextSecondary;
            infoRow.Add(difficultyLabel);

            scrollView.Add(infoRow);

            // Separator
            scrollView.Add(UIStyles.CreateSeparator(UIStyles.SpaceLG));

            // Disclaimer title
            var disclaimerTitle = new Label();
            disclaimerTitle.name = "disclaimer-title";
            disclaimerTitle.style.fontSize = UIStyles.FontXL;
            disclaimerTitle.style.color = UIStyles.Info;
            disclaimerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            disclaimerTitle.style.marginBottom = UIStyles.SpaceMD;
            scrollView.Add(disclaimerTitle);

            // Disclaimer text
            var disclaimerText = UIStyles.CreateBodyText("", UIStyles.FontBase);
            disclaimerText.name = "disclaimer-text";
            disclaimerText.style.color = UIStyles.TextSecondary;
            disclaimerText.style.marginBottom = UIStyles.SpaceLG;
            scrollView.Add(disclaimerText);

            card.Add(scrollView);

            // Buttons
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = UIStyles.SpaceXL;
            buttonRow.style.flexShrink = 0;

            var backButton = UIStyles.CreateSecondaryButton("Back", () => OnBackToLanguageSelection());
            backButton.name = "back-button";
            backButton.style.width = 150;
            backButton.style.marginRight = UIStyles.SpaceLG;
            buttonRow.Add(backButton);

            var nextButton = UIStyles.CreatePrimaryButton("Next", () => OnStartTraining());
            nextButton.name = "start-button";
            nextButton.style.width = 200;
            buttonRow.Add(nextButton);

            card.Add(buttonRow);
            disclaimerPanel.Add(card);
            root.Add(disclaimerPanel);
        }

        // =====================================================================
        // PANEL 3: Start Button
        // =====================================================================

        void CreateStartPanel()
        {
            startPanel = new VisualElement();
            startPanel.name = "start-panel";
            UIStyles.ApplyBackdropStyle(startPanel);
            startPanel.style.display = DisplayStyle.None;

            // Big round start button
            var startButton = new Button(() => OnStartButtonClicked());
            startButton.style.width = 180;
            startButton.style.height = 180;
            startButton.style.fontSize = UIStyles.Font3XL;
            startButton.style.backgroundColor = UIStyles.Accent;
            startButton.style.color = UIStyles.TextOnAccent;
            startButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            UIStyles.SetBorderRadius(startButton, UIStyles.RadiusPill);
            UIStyles.SetBorderWidth(startButton, 3);
            UIStyles.SetBorderColor(startButton, UIStyles.AccentHover);

            // Update text based on language
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            startButton.text = lang == "fr" ? "GO" : "GO";

            startButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                startButton.style.backgroundColor = UIStyles.AccentHover;
                startButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
            });
            startButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                startButton.style.backgroundColor = UIStyles.Accent;
                startButton.style.scale = new Scale(Vector3.one);
            });

            startPanel.Add(startButton);
            root.Add(startPanel);
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        void OnLanguageButtonClicked(string langCode)
        {
            selectedLanguage = langCode;

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Language selected: {langCode}");

            if (localizationManager != null)
            {
                localizationManager.SetLanguage(langCode);
            }

            OnLanguageSelected?.Invoke(langCode);
            ShowDisclaimer();
        }

        void ShowDisclaimer()
        {
            UpdateDisclaimerTexts();
            StartCoroutine(TransitionToDisclaimer());
        }

        void UpdateDisclaimerTexts()
        {
            string lang = selectedLanguage;

            string title = lang == "fr" ? "Formation Test" : "Training Test";
            string description = lang == "fr" ? "Formation interactive de test" : "Interactive test training";
            string duration = "30 minutes";
            string difficulty = lang == "fr" ? "Débutant" : "Beginner";

            if (trainingMetadata != null)
            {
                title = GetLocalizedMetadataValue(trainingMetadata, "title", lang, title);
                description = GetLocalizedMetadataValue(trainingMetadata, "description", lang, description);
                duration = GetMetadataValue<string>(trainingMetadata, "duration", duration);
                difficulty = GetMetadataValue<string>(trainingMetadata, "difficulty", difficulty);
            }

            var titleLabel = disclaimerPanel.Q<Label>("training-title");
            if (titleLabel != null) titleLabel.text = title;

            var descLabel = disclaimerPanel.Q<Label>("training-description");
            if (descLabel != null) descLabel.text = description;

            var durationLabel = disclaimerPanel.Q<Label>("training-duration");
            if (durationLabel != null)
            {
                durationLabel.text = lang == "fr" ? $"Durée : {duration}" : $"Duration: {duration}";
            }

            var difficultyLabel = disclaimerPanel.Q<Label>("training-difficulty");
            if (difficultyLabel != null)
            {
                string displayDifficulty = TranslateDifficulty(difficulty, lang);
                difficultyLabel.text = lang == "fr" ? $"Difficulté : {displayDifficulty}" : $"Difficulty: {displayDifficulty}";
            }

            var disclaimerTitle = disclaimerPanel.Q<Label>("disclaimer-title");
            if (disclaimerTitle != null)
            {
                disclaimerTitle.text = lang == "fr" ? "Informations Importantes" : "Important Information";
            }

            var disclaimerText = disclaimerPanel.Q<Label>("disclaimer-text");
            if (disclaimerText != null)
            {
                string customDisclaimer = null;
                if (wiseTwinManager != null && wiseTwinManager.MetadataLoader != null)
                {
                    customDisclaimer = wiseTwinManager.MetadataLoader.GetDisclaimer(lang);
                }

                if (!string.IsNullOrEmpty(customDisclaimer))
                {
                    disclaimerText.text = customDisclaimer;
                }
                else
                {
                    if (lang == "fr")
                    {
                        disclaimerText.text =
                            "• Cette formation collecte vos temps de réponse pour personnaliser votre expérience d'apprentissage.\n\n" +
                            "• Assurez-vous d'avoir le temps nécessaire devant vous (environ " + duration + ").\n\n" +
                            "• Pour une expérience optimale, évitez les interruptions pendant la formation.\n\n" +
                            "• Vos données sont utilisées uniquement pour améliorer votre parcours de formation.";
                    }
                    else
                    {
                        disclaimerText.text =
                            "• This training collects your response times to personalize your learning experience.\n\n" +
                            "• Please ensure you have the necessary time available (approximately " + duration + ").\n\n" +
                            "• For an optimal experience, avoid interruptions during the training.\n\n" +
                            "• Your data is used solely to improve your training journey.";
                    }
                }
            }

            var backButton = disclaimerPanel.Q<Button>("back-button");
            if (backButton != null) backButton.text = lang == "fr" ? "Retour" : "Back";

            var startButton = disclaimerPanel.Q<Button>("start-button");
            if (startButton != null) startButton.text = lang == "fr" ? "Suivant" : "Next";
        }

        void OnBackToLanguageSelection()
        {
            StartCoroutine(TransitionToLanguageSelection());
        }

        void OnStartTraining()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Showing tutorial");
            StartCoroutine(TransitionToTutorial());
        }

        void OnTutorialCompleted()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Tutorial completed, showing start button");

            if (startPanel != null)
            {
                startPanel.style.display = DisplayStyle.Flex;
                startPanel.style.opacity = 1;
            }
        }

        void OnStartButtonClicked()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Start button clicked - starting training");

            OnTrainingStarted?.Invoke();
            StartCoroutine(HideAllPanelsAndStart());
        }

        // =====================================================================
        // TRANSITIONS
        // =====================================================================

        IEnumerator TransitionToDisclaimer()
        {
            if (languageSelectionPanel != null)
            {
                yield return FadeOut(languageSelectionPanel);
                languageSelectionPanel.style.display = DisplayStyle.None;
            }

            if (disclaimerPanel != null)
            {
                disclaimerPanel.style.display = DisplayStyle.Flex;
                yield return FadeIn(disclaimerPanel);
            }
        }

        IEnumerator TransitionToTutorial()
        {
            if (disclaimerPanel != null)
            {
                yield return FadeOut(disclaimerPanel);
                disclaimerPanel.style.display = DisplayStyle.None;
            }

            if (tutorialUI != null)
            {
                tutorialUI.Show(selectedLanguage);
            }
            else
            {
                Debug.LogError("[LanguageSelectionUI] TutorialUI is null! Skipping to training start.");
                OnTutorialCompleted();
            }
        }

        IEnumerator TransitionToLanguageSelection()
        {
            if (disclaimerPanel != null)
            {
                yield return FadeOut(disclaimerPanel);
                disclaimerPanel.style.display = DisplayStyle.None;
            }

            if (languageSelectionPanel != null)
            {
                languageSelectionPanel.style.display = DisplayStyle.Flex;
                yield return FadeIn(languageSelectionPanel);
            }
        }

        IEnumerator HideAllPanelsAndStart()
        {
            if (startPanel != null)
            {
                yield return FadeOut(startPanel);
                startPanel.style.display = DisplayStyle.None;
            }

            if (root != null)
            {
                root.style.display = DisplayStyle.None;
                root.pickingMode = PickingMode.Ignore;
            }

            if (uiDocument != null)
            {
                uiDocument.enabled = false;
            }

            IsDisplaying = false;

            ControlModeSettings.ApplyToPlayer();
            ShowTrainingHUD();

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.StartProgression();
            }
        }

        // =====================================================================
        // HUD & DISPLAY
        // =====================================================================

        void ShowTrainingHUD()
        {
            var trainingHUD = TrainingHUD.Instance;
            if (trainingHUD == null)
            {
                var hudGO = new GameObject("TrainingHUD");
                trainingHUD = hudGO.AddComponent<TrainingHUD>();
            }

            trainingHUD.Show();

            if (debugMode) Debug.Log("[LanguageSelectionUI] Training HUD shown");
        }

        public void ShowLanguageSelection()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            PlayerControls.SetEnabled(false);

            if (languageSelectionPanel != null)
            {
                languageSelectionPanel.style.display = DisplayStyle.Flex;
                languageSelectionPanel.style.opacity = 1;
                IsDisplaying = true;
            }
        }

        // =====================================================================
        // ANIMATIONS
        // =====================================================================

        IEnumerator FadeIn(VisualElement element)
        {
            element.style.opacity = 0;
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(0, 1, elapsed / animationDuration);
                yield return null;
            }
            element.style.opacity = 1;
        }

        IEnumerator FadeOut(VisualElement element)
        {
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(1, 0, elapsed / animationDuration);
                yield return null;
            }
            element.style.opacity = 0;
        }

        // =====================================================================
        // UTILITY
        // =====================================================================

        T GetMetadataValue<T>(Dictionary<string, object> data, string key, T defaultValue)
        {
            if (data != null && data.ContainsKey(key))
            {
                var value = data[key];
                if (value is T typedValue) return typedValue;
                if (value != null)
                {
                    try { return (T)System.Convert.ChangeType(value, typeof(T)); }
                    catch { return defaultValue; }
                }
            }
            return defaultValue;
        }

        string GetLocalizedMetadataValue(Dictionary<string, object> data, string key, string language, string defaultValue)
        {
            if (data == null || !data.ContainsKey(key))
                return defaultValue;

            var value = data[key];

            if (value is string simpleString)
                return simpleString;

            if (value is Dictionary<string, object> localizedDict)
            {
                if (localizedDict.ContainsKey(language) && localizedDict[language] != null)
                    return localizedDict[language].ToString();
                if (localizedDict.ContainsKey("en") && localizedDict["en"] != null)
                    return localizedDict["en"].ToString();
            }
            else if (value != null && value.GetType().FullName.Contains("JObject"))
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    var localizedObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (localizedObj != null)
                    {
                        if (localizedObj.ContainsKey(language)) return localizedObj[language];
                        if (localizedObj.ContainsKey("en")) return localizedObj["en"];
                    }
                }
                catch { }
            }

            return defaultValue;
        }

        string TranslateDifficulty(string difficulty, string language)
        {
            if (language == "fr") return difficulty;

            switch (difficulty.ToLower())
            {
                case "facile": return "Easy";
                case "intermédiaire": return "Intermediate";
                case "avancé": return "Advanced";
                case "expert": return "Expert";
                default: return difficulty;
            }
        }

        [ContextMenu("Test Show Language Selection")]
        public void TestShowLanguageSelection()
        {
            Initialize();
            ShowLanguageSelection();
        }
    }
}
