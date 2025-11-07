using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Gère l'UI de sélection de langue et l'écran de disclaimer
    /// Étape 1 : Choix de la langue
    /// Étape 2 : Informations de formation et disclaimer
    /// </summary>
    public class LanguageSelectionUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool showOnStart = true; // Affichage automatique au démarrage
        [SerializeField] private float animationDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color accentColor = new Color(0.1f, 0.8f, 0.6f);
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // UI Document reference
        private UIDocument uiDocument;
        private VisualElement root;

        // Panels
        private VisualElement languageSelectionPanel;
        private VisualElement disclaimerPanel;

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
            // Récupérer ou ajouter UIDocument
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[LanguageSelectionUI] PanelSettings is null! Please assign it in the inspector.");
            }

            // NE PAS charger le UXML - on crée tout programmatiquement
            // Forcer visualTreeAsset à null pour éviter les conflits
            if (uiDocument.visualTreeAsset != null)
            {
                if (debugMode) Debug.LogWarning("[LanguageSelectionUI] Clearing UXML to avoid conflicts with programmatic creation");
                uiDocument.visualTreeAsset = null;
            }
        }

        void Start()
        {
            // Récupérer les références
            localizationManager = LocalizationManager.Instance;
            wiseTwinManager = WiseTwinManager.Instance;
            uiManager = WiseTwinUIManager.Instance;

            // Find or create TutorialUI
            tutorialUI = FindAnyObjectByType<TutorialUI>();
            if (tutorialUI == null)
            {
                // Create a new GameObject for TutorialUI if it doesn't exist
                var tutorialGO = new GameObject("TutorialUI");
                tutorialUI = tutorialGO.AddComponent<TutorialUI>();

                // Pass our PanelSettings to the TutorialUI
                if (uiDocument != null && uiDocument.panelSettings != null)
                {
                    tutorialUI.SetPanelSettings(uiDocument.panelSettings);
                }

                if (debugMode) Debug.Log("[LanguageSelectionUI] TutorialUI created");
            }

            // Subscribe to tutorial completion event
            if (tutorialUI != null)
            {
                tutorialUI.OnTutorialCompleted += OnTutorialCompleted;
            }

            if (showOnStart)
            {
                // Initialiser et afficher immédiatement
                Initialize();
                ShowLanguageSelection();

                // Charger les métadonnées si disponibles
                StartCoroutine(LoadMetadataWhenReady());
            }
        }

        IEnumerator LoadMetadataWhenReady()
        {
            // Désactiver les boutons de langue pendant le chargement
            SetLanguageButtonsEnabled(false);

            int waitFrames = 0;
            const int maxWaitFrames = 300; // 5 seconds at 60fps

            // Attendre que WiseTwinManager soit prêt
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

            // Récupérer les métadonnées
            trainingMetadata = wiseTwinManager.MetadataLoader.GetMetadata();
            if (trainingMetadata == null)
            {
                Debug.LogError("[LanguageSelectionUI] Metadata is NULL after loading!");
            }

            // Réactiver les boutons de langue
            SetLanguageButtonsEnabled(true);
        }

        void SetLanguageButtonsEnabled(bool enabled)
        {
            if (languageSelectionPanel == null) return;

            var englishButton = languageSelectionPanel.Q<Button>("lang-button-en");
            var frenchButton = languageSelectionPanel.Q<Button>("lang-button-fr");

            if (englishButton != null)
            {
                englishButton.SetEnabled(enabled);
            }
            if (frenchButton != null)
            {
                frenchButton.SetEnabled(enabled);
            }
        }


        void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[LanguageSelectionUI] Already initialized!");
                return;
            }

            if (uiDocument == null)
            {
                Debug.LogError("[LanguageSelectionUI] UIDocument is null!");
                return;
            }

            // Créer la structure UI
            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[LanguageSelectionUI] Root visual element is null!");
                return;
            }

            // IMPORTANT: Configurer le pickingMode pour que l'UI reçoive les événements de clic
            root.pickingMode = PickingMode.Position;
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Root configured. PickingMode: {root.pickingMode}. Current children: {root.childCount}");

            // Clear everything first
            root.Clear();
            if (debugMode) Debug.Log("[LanguageSelectionUI] Root cleared");

            // Créer directement la structure UI (plus fiable)
            if (debugMode) Debug.Log("[LanguageSelectionUI] Creating UI structure");
            SetupUIStructure();

            isInitialized = true;
            if (debugMode) Debug.Log($"[LanguageSelectionUI] Initialized successfully. Root now has {root.childCount} children");
        }

        void SetupUIFromUXML()
        {
            // Récupérer les panels depuis le UXML
            languageSelectionPanel = root.Q<VisualElement>("language-selection-panel");
            disclaimerPanel = root.Q<VisualElement>("disclaimer-panel");

            if (languageSelectionPanel == null || disclaimerPanel == null)
            {
                Debug.LogError("[LanguageSelectionUI] Could not find panels in UXML! Creating programmatically...");
                // Fallback to programmatic creation
                SetupUIStructure();
                return;
            }

            Debug.Log("[LanguageSelectionUI] UXML panels found successfully");

            // Configurer les boutons de langue
            var englishBtn = root.Q<Button>("btn-english");
            var frenchBtn = root.Q<Button>("btn-french");

            if (englishBtn != null)
            {
                englishBtn.clicked += () => OnLanguageButtonClicked("en");
            }

            if (frenchBtn != null)
            {
                frenchBtn.clicked += () => OnLanguageButtonClicked("fr");
            }

            // Configurer les boutons du disclaimer
            var backBtn = root.Q<Button>("back-button");
            var startBtn = root.Q<Button>("start-button");

            if (backBtn != null)
            {
                backBtn.clicked += OnBackToLanguageSelection;
            }

            if (startBtn != null)
            {
                startBtn.clicked += OnStartTraining;
            }

            Debug.Log("[LanguageSelectionUI] UI configured from UXML");
        }

        void SetupUIStructure()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Setting up UI structure");

            // Créer le panel de sélection de langue
            CreateLanguageSelectionPanel();

            // Créer le panel de disclaimer
            CreateDisclaimerPanel();

            if (debugMode) Debug.Log($"[LanguageSelectionUI] UI structure created. Root has {root.childCount} children");
        }

        void CreateLanguageSelectionPanel()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Creating language selection panel");

            // Container principal - directement sur root
            languageSelectionPanel = new VisualElement();
            languageSelectionPanel.name = "language-selection-panel";
            languageSelectionPanel.style.position = Position.Absolute;
            languageSelectionPanel.style.width = Length.Percent(100);
            languageSelectionPanel.style.height = Length.Percent(100);
            languageSelectionPanel.style.backgroundColor = new Color(0, 0, 0, 0.95f); // Noir opaque
            languageSelectionPanel.style.display = DisplayStyle.None;
            languageSelectionPanel.style.alignItems = Align.Center;
            languageSelectionPanel.style.justifyContent = Justify.Center;
            // IMPORTANT : Bloquer les raycasts pour ne pas cliquer à travers
            languageSelectionPanel.pickingMode = PickingMode.Position;

            // Container pour le contenu avec style épuré
            var contentContainer = new VisualElement();
            contentContainer.style.width = 500;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.paddingTop = 40;
            contentContainer.style.paddingBottom = 40;
            contentContainer.style.paddingLeft = 40;
            contentContainer.style.paddingRight = 40;
            // Fond gris foncé avec légère transparence
            contentContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.98f);
            contentContainer.style.borderTopLeftRadius = 15;
            contentContainer.style.borderTopRightRadius = 15;
            contentContainer.style.borderBottomLeftRadius = 15;
            contentContainer.style.borderBottomRightRadius = 15;
            // Bordure subtile
            contentContainer.style.borderTopWidth = 1;
            contentContainer.style.borderBottomWidth = 1;
            contentContainer.style.borderLeftWidth = 1;
            contentContainer.style.borderRightWidth = 1;
            contentContainer.style.borderTopColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderBottomColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderLeftColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderRightColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);

            // Container pour les boutons de langue - en colonne
            var languageButtons = new VisualElement();
            languageButtons.style.flexDirection = FlexDirection.Column;
            languageButtons.style.width = Length.Percent(100);

            // Bouton Français
            var frenchButton = CreateLanguageButton("Français", "FR", "fr");
            languageButtons.Add(frenchButton);

            // Espacement entre les boutons
            var spacer = new VisualElement();
            spacer.style.height = 15;
            languageButtons.Add(spacer);

            // Bouton Anglais
            var englishButton = CreateLanguageButton("English", "GB", "en");
            languageButtons.Add(englishButton);

            contentContainer.Add(languageButtons);
            languageSelectionPanel.Add(contentContainer);
            root.Add(languageSelectionPanel);

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Language panel created with {languageButtons.childCount} buttons");
        }

        Button CreateLanguageButton(string label, string flag, string langCode)
        {
            var button = new Button(() => OnLanguageButtonClicked(langCode));
            button.name = $"lang-button-{langCode}";

            // Style liste conventionnel - pleine largeur
            button.style.width = Length.Percent(100);
            button.style.height = 60;
            button.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            button.style.borderTopLeftRadius = 8;
            button.style.borderTopRightRadius = 8;
            button.style.borderBottomLeftRadius = 8;
            button.style.borderBottomRightRadius = 8;
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            button.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            button.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            button.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.paddingLeft = 20;
            button.style.paddingRight = 20;

            // Language name - aligné à gauche
            var langLabel = new Label(label);
            langLabel.style.fontSize = 20;
            langLabel.style.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            langLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            langLabel.style.flexGrow = 1;
            button.Add(langLabel);

            // Hover effect - subtil
            button.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                button.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                button.style.borderTopColor = accentColor;
                button.style.borderBottomColor = accentColor;
                button.style.borderLeftColor = accentColor;
                button.style.borderRightColor = accentColor;
            });

            button.RegisterCallback<MouseLeaveEvent>((evt) =>
            {
                button.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 1f);
                button.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                button.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                button.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                button.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            });

            return button;
        }

        void CreateDisclaimerPanel()
        {
            // Container principal
            disclaimerPanel = new VisualElement();
            disclaimerPanel.name = "disclaimer-panel";
            disclaimerPanel.style.position = Position.Absolute;
            disclaimerPanel.style.width = Length.Percent(100);
            disclaimerPanel.style.height = Length.Percent(100);
            disclaimerPanel.style.backgroundColor = backgroundColor;
            disclaimerPanel.style.display = DisplayStyle.None;
            disclaimerPanel.style.alignItems = Align.Center;
            disclaimerPanel.style.justifyContent = Justify.Center;
            // IMPORTANT : Bloquer les raycasts pour ne pas cliquer à travers
            disclaimerPanel.pickingMode = PickingMode.Position;

            // Container pour le contenu
            var contentContainer = new VisualElement();
            contentContainer.style.width = 900;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.maxHeight = Length.Percent(90);
            contentContainer.style.paddingTop = 50;
            contentContainer.style.paddingBottom = 50;
            contentContainer.style.paddingLeft = 50;
            contentContainer.style.paddingRight = 50;
            contentContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            contentContainer.style.borderTopLeftRadius = 20;
            contentContainer.style.borderTopRightRadius = 20;
            contentContainer.style.borderBottomLeftRadius = 20;
            contentContainer.style.borderBottomRightRadius = 20;

            // ScrollView pour le contenu - masquer la scrollbar
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden; // Masquer la scrollbar mais garder le scroll
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // Titre de la formation (sera mis à jour dynamiquement)
            var titleLabel = new Label("Formation Title");
            titleLabel.name = "training-title";
            titleLabel.style.fontSize = 36;
            titleLabel.style.color = accentColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            // Gérer le débordement du texte
            titleLabel.style.whiteSpace = WhiteSpace.Normal; // Permet au texte de passer à la ligne
            titleLabel.style.width = Length.Percent(100); // Utilise toute la largeur disponible
            titleLabel.style.overflow = Overflow.Hidden; // Cache le débordement si nécessaire
            scrollView.Add(titleLabel);

            // Description
            var descLabel = new Label("Formation description");
            descLabel.name = "training-description";
            descLabel.style.fontSize = 18;
            descLabel.style.color = Color.white;
            descLabel.style.marginBottom = 30;
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            scrollView.Add(descLabel);

            // Informations (durée, difficulté)
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Row;
            infoContainer.style.justifyContent = Justify.Center;
            infoContainer.style.marginBottom = 30;

            var durationLabel = new Label("Duration: 30 min");
            durationLabel.name = "training-duration";
            durationLabel.style.fontSize = 16;
            durationLabel.style.color = Color.white;
            durationLabel.style.marginRight = 30;
            infoContainer.Add(durationLabel);

            var difficultyLabel = new Label("Difficulty: Intermediate");
            difficultyLabel.name = "training-difficulty";
            difficultyLabel.style.fontSize = 16;
            difficultyLabel.style.color = Color.white;
            infoContainer.Add(difficultyLabel);

            scrollView.Add(infoContainer);

            // Séparateur
            var separator = new VisualElement();
            separator.style.height = 2;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            separator.style.marginTop = 20;
            separator.style.marginBottom = 20;
            scrollView.Add(separator);

            // Disclaimer
            var disclaimerTitle = new Label("Important Information");
            disclaimerTitle.name = "disclaimer-title";
            disclaimerTitle.style.fontSize = 24;
            disclaimerTitle.style.color = primaryColor;
            disclaimerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            disclaimerTitle.style.marginBottom = 15;
            scrollView.Add(disclaimerTitle);

            var disclaimerText = new Label();
            disclaimerText.name = "disclaimer-text";
            disclaimerText.style.fontSize = 16;
            disclaimerText.style.color = new Color(0.9f, 0.9f, 0.9f);
            disclaimerText.style.whiteSpace = WhiteSpace.Normal;
            disclaimerText.style.marginBottom = 20;
            scrollView.Add(disclaimerText);

            contentContainer.Add(scrollView);

            // Boutons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.marginTop = 30;

            // Bouton Retour
            var backButton = new Button(() => OnBackToLanguageSelection());
            backButton.text = "Back";
            backButton.name = "back-button";
            backButton.style.width = 150;
            backButton.style.height = 50;
            backButton.style.fontSize = 18;
            backButton.style.marginRight = 20;
            backButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            backButton.style.color = Color.white;
            backButton.style.borderTopLeftRadius = 10;
            backButton.style.borderTopRightRadius = 10;
            backButton.style.borderBottomLeftRadius = 10;
            backButton.style.borderBottomRightRadius = 10;
            buttonContainer.Add(backButton);

            // Bouton Commencer
            var startButton = new Button(() => OnStartTraining());
            startButton.text = "Start Training";
            startButton.name = "start-button";
            startButton.style.width = 200;
            startButton.style.height = 50;
            startButton.style.fontSize = 18;
            startButton.style.backgroundColor = accentColor;
            startButton.style.color = Color.white;
            startButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            startButton.style.borderTopLeftRadius = 10;
            startButton.style.borderTopRightRadius = 10;
            startButton.style.borderBottomLeftRadius = 10;
            startButton.style.borderBottomRightRadius = 10;
            buttonContainer.Add(startButton);

            contentContainer.Add(buttonContainer);
            disclaimerPanel.Add(contentContainer);
            root.Add(disclaimerPanel);
        }

        void OnLanguageButtonClicked(string langCode)
        {
            selectedLanguage = langCode;

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Language selected: {langCode}");

            // Définir la langue dans le LocalizationManager
            if (localizationManager != null)
            {
                localizationManager.SetLanguage(langCode);
            }

            // Déclencher l'événement
            OnLanguageSelected?.Invoke(langCode);

            // Passer au disclaimer
            ShowDisclaimer();
        }

        void ShowDisclaimer()
        {
            // Mettre à jour les textes du disclaimer selon la langue
            UpdateDisclaimerTexts();

            // Transition
            StartCoroutine(TransitionToDisclaimer());
        }

        void UpdateDisclaimerTexts()
        {
            string lang = selectedLanguage;

            // Valeurs par défaut
            string title = lang == "fr" ? "Formation Test" : "Training Test";
            string description = lang == "fr" ? "Formation interactive de test" : "Interactive test training";
            string duration = "30 minutes";
            string difficulty = lang == "fr" ? "Débutant" : "Beginner";

            // Essayer de charger depuis les métadonnées si disponibles
            if (trainingMetadata != null)
            {
                // Extraire titre multilingue
                title = GetLocalizedMetadataValue(trainingMetadata, "title", lang, title);
                // Extraire description multilingue
                description = GetLocalizedMetadataValue(trainingMetadata, "description", lang, description);
                // Durée et difficulté ne sont pas multilingues
                duration = GetMetadataValue<string>(trainingMetadata, "duration", duration);
                difficulty = GetMetadataValue<string>(trainingMetadata, "difficulty", difficulty);
            }

            // Titre
            var titleLabel = disclaimerPanel.Q<Label>("training-title");
            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            // Description
            var descLabel = disclaimerPanel.Q<Label>("training-description");
            if (descLabel != null)
            {
                descLabel.text = description;
            }

            // Durée
            var durationLabel = disclaimerPanel.Q<Label>("training-duration");
            if (durationLabel != null)
            {
                string durationText = lang == "fr" ? $"Durée : {duration}" : $"Duration: {duration}";
                durationLabel.text = durationText;
            }

            // Difficulté (traduire si nécessaire)
            var difficultyLabel = disclaimerPanel.Q<Label>("training-difficulty");
            if (difficultyLabel != null)
            {
                string displayDifficulty = TranslateDifficulty(difficulty, lang);
                string diffText = lang == "fr" ? $"Difficulté : {displayDifficulty}" : $"Difficulty: {displayDifficulty}";
                difficultyLabel.text = diffText;
            }

            // Titre disclaimer
            var disclaimerTitle = disclaimerPanel.Q<Label>("disclaimer-title");
            if (disclaimerTitle != null)
            {
                disclaimerTitle.text = lang == "fr" ? "Informations Importantes" : "Important Information";
            }

            // Texte disclaimer
            var disclaimerText = disclaimerPanel.Q<Label>("disclaimer-text");
            if (disclaimerText != null)
            {
                // Try to load disclaimer from metadata first
                string customDisclaimer = null;
                if (wiseTwinManager != null && wiseTwinManager.MetadataLoader != null)
                {
                    customDisclaimer = wiseTwinManager.MetadataLoader.GetDisclaimer(lang);
                }

                // Use custom disclaimer if available, otherwise use default
                if (!string.IsNullOrEmpty(customDisclaimer))
                {
                    disclaimerText.text = customDisclaimer;
                }
                else
                {
                    // Fallback to default disclaimer text
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

            // Boutons
            var backButton = disclaimerPanel.Q<Button>("back-button");
            if (backButton != null)
            {
                backButton.text = lang == "fr" ? "Retour" : "Back";
            }

            var startButton = disclaimerPanel.Q<Button>("start-button");
            if (startButton != null)
            {
                startButton.text = lang == "fr" ? "Commencer" : "Start Training";
            }
        }

        T GetMetadataValue<T>(Dictionary<string, object> data, string key, T defaultValue)
        {
            if (data != null && data.ContainsKey(key))
            {
                var value = data[key];
                if (value is T typedValue)
                {
                    return typedValue;
                }
                else if (value != null)
                {
                    // Essayer de convertir
                    try
                    {
                        return (T)System.Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }

        string GetLocalizedMetadataValue(Dictionary<string, object> data, string key, string language, string defaultValue)
        {
            if (data == null || !data.ContainsKey(key))
                return defaultValue;

            var value = data[key];

            // Si c'est déjà une string simple (ancien format), la retourner
            if (value is string simpleString)
                return simpleString;

            // Si c'est un objet avec des langues {en: "...", fr: "..."}
            if (value is Dictionary<string, object> localizedDict)
            {
                // Essayer de récupérer la langue demandée
                if (localizedDict.ContainsKey(language) && localizedDict[language] != null)
                    return localizedDict[language].ToString();

                // Fallback sur l'anglais
                if (localizedDict.ContainsKey("en") && localizedDict["en"] != null)
                    return localizedDict["en"].ToString();
            }
            // Newtonsoft peut retourner un JObject
            else if (value != null && value.GetType().FullName.Contains("JObject"))
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                    var localizedObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (localizedObj != null)
                    {
                        if (localizedObj.ContainsKey(language))
                            return localizedObj[language];
                        if (localizedObj.ContainsKey("en"))
                            return localizedObj["en"];
                    }
                }
                catch { }
            }

            return defaultValue;
        }

        string TranslateDifficulty(string difficulty, string language)
        {
            // Si la langue est le français, retourner tel quel
            if (language == "fr")
                return difficulty;

            // Traduire du français vers l'anglais
            switch (difficulty.ToLower())
            {
                case "facile":
                    return "Easy";
                case "intermédiaire":
                    return "Intermediate";
                case "avancé":
                    return "Advanced";
                case "expert":
                    return "Expert";
                default:
                    // Si c'est déjà en anglais ou inconnu, retourner tel quel
                    return difficulty;
            }
        }

        void OnBackToLanguageSelection()
        {
            StartCoroutine(TransitionToLanguageSelection());
        }

        void OnStartTraining()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Showing tutorial");

            // Hide disclaimer and show tutorial
            StartCoroutine(TransitionToTutorial());
        }

        void OnTutorialCompleted()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Tutorial completed, starting training");

            // Déclencher l'événement
            OnTrainingStarted?.Invoke();

            // Masquer les panels
            StartCoroutine(HideAllPanels());

            // Afficher le HUD de formation
            ShowTrainingHUD();

            // Démarrer la formation dans le UIManager (optionnel, selon tes besoins)
            if (uiManager != null)
            {
                // uiManager.StartTraining(); // Commenté car on utilise notre nouveau HUD
            }
        }

        void ShowTrainingHUD()
        {
            // Chercher ou créer le TrainingHUD
            var trainingHUD = TrainingHUD.Instance;
            if (trainingHUD == null)
            {
                // Créer le HUD s'il n'existe pas
                var hudGO = new GameObject("TrainingHUD");
                trainingHUD = hudGO.AddComponent<TrainingHUD>();
            }

            // NOTE: Auto-detection is no longer needed
            // ProgressionManager automatically initializes the total from metadata scenarios

            // Afficher le HUD
            trainingHUD.Show();

            if (debugMode) Debug.Log("[LanguageSelectionUI] Training HUD shown");
        }

        public void ShowLanguageSelection()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[LanguageSelectionUI] Not initialized yet! Initializing now...");
                Initialize();
            }

            if (languageSelectionPanel != null)
            {
                if (debugMode) Debug.Log("[LanguageSelectionUI] Showing language selection panel");
                languageSelectionPanel.style.display = DisplayStyle.Flex;
                IsDisplaying = true;
                // Désactiver temporairement le fade pour tester
                languageSelectionPanel.style.opacity = 1;
                //StartCoroutine(FadeIn(languageSelectionPanel));
            }
            else
            {
                Debug.LogError("[LanguageSelectionUI] Language selection panel is null!");
            }
        }

        IEnumerator TransitionToDisclaimer()
        {
            // Fade out language panel
            if (languageSelectionPanel != null)
            {
                yield return FadeOut(languageSelectionPanel);
                languageSelectionPanel.style.display = DisplayStyle.None;
            }

            // Fade in disclaimer panel
            if (disclaimerPanel != null)
            {
                disclaimerPanel.style.display = DisplayStyle.Flex;
                yield return FadeIn(disclaimerPanel);
            }
        }

        IEnumerator TransitionToTutorial()
        {
            // Fade out disclaimer panel
            if (disclaimerPanel != null)
            {
                yield return FadeOut(disclaimerPanel);
                disclaimerPanel.style.display = DisplayStyle.None;
            }

            // Show tutorial
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
            // Fade out disclaimer panel
            if (disclaimerPanel != null)
            {
                yield return FadeOut(disclaimerPanel);
                disclaimerPanel.style.display = DisplayStyle.None;
            }

            // Fade in language panel
            if (languageSelectionPanel != null)
            {
                languageSelectionPanel.style.display = DisplayStyle.Flex;
                yield return FadeIn(languageSelectionPanel);
            }
        }

        IEnumerator HideAllPanels()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Hiding all panels...");

            if (disclaimerPanel != null && disclaimerPanel.style.display == DisplayStyle.Flex)
            {
                yield return FadeOut(disclaimerPanel);
                disclaimerPanel.style.display = DisplayStyle.None;
            }

            if (languageSelectionPanel != null && languageSelectionPanel.style.display == DisplayStyle.Flex)
            {
                yield return FadeOut(languageSelectionPanel);
                languageSelectionPanel.style.display = DisplayStyle.None;
            }

            // IMPORTANT: Hide the root element too to remove the background
            if (root != null)
            {
                root.style.display = DisplayStyle.None;
                if (debugMode) Debug.Log("[LanguageSelectionUI] Root element hidden");
            }

            // IMPORTANT : Marquer comme non affiché pour permettre les clics 3D
            IsDisplaying = false;

            // Or alternatively, clear the root entirely
            // root?.Clear();
        }

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

        /// <summary>
        /// Pour les tests dans l'éditeur
        /// </summary>
        [ContextMenu("Test Show Language Selection")]
        public void TestShowLanguageSelection()
        {
            Initialize();
            ShowLanguageSelection();
        }
    }
}