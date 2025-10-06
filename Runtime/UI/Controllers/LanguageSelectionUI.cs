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

            // Assigner le PanelSettings s'il n'est pas déjà assigné
            if (uiDocument.panelSettings == null)
            {
                // Charger le PanelSettings depuis Resources
                var panelSettings = Resources.Load<PanelSettings>("WiseTwinPanelSettings");
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                    if (debugMode) Debug.Log("[LanguageSelectionUI] PanelSettings loaded from Resources");
                }
                else
                {
                    Debug.LogError("[LanguageSelectionUI] Could not find WiseTwinPanelSettings in Resources folder!");
                }
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
            // Attendre que WiseTwinManager soit prêt
            while (wiseTwinManager == null || !wiseTwinManager.IsMetadataLoaded)
            {
                yield return null;
                if (wiseTwinManager == null)
                {
                    wiseTwinManager = WiseTwinManager.Instance;
                }
            }

            // Récupérer les métadonnées
            trainingMetadata = wiseTwinManager.MetadataLoader.GetMetadata();
            if (debugMode && trainingMetadata != null)
            {
                Debug.Log($"[LanguageSelectionUI] Metadata loaded with {trainingMetadata.Count} entries");
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

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Root exists. Current children: {root.childCount}");

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

            // Container pour le contenu avec style carte moderne - plus compact sans titre
            var contentContainer = new VisualElement();
            contentContainer.style.width = 700;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.paddingTop = 50;
            contentContainer.style.paddingBottom = 50;
            contentContainer.style.paddingLeft = 50;
            contentContainer.style.paddingRight = 50;
            // Fond gris foncé avec légère transparence
            contentContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.98f);
            contentContainer.style.borderTopLeftRadius = 25;
            contentContainer.style.borderTopRightRadius = 25;
            contentContainer.style.borderBottomLeftRadius = 25;
            contentContainer.style.borderBottomRightRadius = 25;
            // Bordure subtile
            contentContainer.style.borderTopWidth = 1;
            contentContainer.style.borderBottomWidth = 1;
            contentContainer.style.borderLeftWidth = 1;
            contentContainer.style.borderRightWidth = 1;
            contentContainer.style.borderTopColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderBottomColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderLeftColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderRightColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);

            // Pas de titre, les drapeaux sont suffisamment explicites

            // Container pour les boutons de langue
            var languageButtons = new VisualElement();
            languageButtons.style.flexDirection = FlexDirection.Row;
            languageButtons.style.justifyContent = Justify.SpaceAround;
            languageButtons.style.width = Length.Percent(100);

            // Bouton Anglais - Utiliser des images au lieu d'emoji
            var englishButton = CreateLanguageButton("English", "GB", "en");
            languageButtons.Add(englishButton);

            // Espacement entre les boutons
            var spacer = new VisualElement();
            spacer.style.width = 40;
            languageButtons.Add(spacer);

            // Bouton Français
            var frenchButton = CreateLanguageButton("Français", "FR", "fr");
            languageButtons.Add(frenchButton);

            contentContainer.Add(languageButtons);
            languageSelectionPanel.Add(contentContainer);
            root.Add(languageSelectionPanel);

            if (debugMode) Debug.Log($"[LanguageSelectionUI] Language panel created with {languageButtons.childCount} buttons");
        }

        Button CreateLanguageButton(string label, string flag, string langCode)
        {
            var button = new Button(() => OnLanguageButtonClicked(langCode));
            button.style.width = 250;
            button.style.height = 150;
            // Fond avec gradient simulé (plus sombre en bas)
            button.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            button.style.borderTopLeftRadius = 15;
            button.style.borderTopRightRadius = 15;
            button.style.borderBottomLeftRadius = 15;
            button.style.borderBottomRightRadius = 15;
            button.style.borderTopWidth = 3;
            button.style.borderBottomWidth = 3;
            button.style.borderLeftWidth = 3;
            button.style.borderRightWidth = 3;
            // Bordure avec couleur accent
            button.style.borderTopColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            button.style.borderBottomColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            button.style.borderLeftColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            button.style.borderRightColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            button.style.flexDirection = FlexDirection.Column;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            // Ombre portée
            button.style.paddingTop = 20;
            button.style.paddingBottom = 20;
            button.style.paddingLeft = 20;
            button.style.paddingRight = 20;

            // Container pour centrer le contenu
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.alignItems = Align.Center;
            contentContainer.style.justifyContent = Justify.Center;

            // Utiliser du texte stylisé au lieu d'emoji pour le drapeau
            var flagContainer = new VisualElement();
            flagContainer.style.width = 80;
            flagContainer.style.height = 60;
            flagContainer.style.backgroundColor = GetFlagColor(flag);
            flagContainer.style.borderTopLeftRadius = 8;
            flagContainer.style.borderTopRightRadius = 8;
            flagContainer.style.borderBottomLeftRadius = 8;
            flagContainer.style.borderBottomRightRadius = 8;
            flagContainer.style.marginBottom = 15;
            flagContainer.style.alignItems = Align.Center;
            flagContainer.style.justifyContent = Justify.Center;

            // Texte du code pays
            var flagText = new Label(flag);
            flagText.style.fontSize = 24;
            flagText.style.color = Color.white;
            flagText.style.unityFontStyleAndWeight = FontStyle.Bold;
            flagText.style.unityTextAlign = TextAnchor.MiddleCenter;
            flagContainer.Add(flagText);

            contentContainer.Add(flagContainer);

            // Language name - Centré
            var langLabel = new Label(label);
            langLabel.style.fontSize = 22;
            langLabel.style.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            langLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            langLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            langLabel.style.width = Length.Percent(100);
            contentContainer.Add(langLabel);

            button.Add(contentContainer);

            // Hover effect - plus visible
            button.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                button.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                button.style.scale = new Scale(new Vector3(1.08f, 1.08f, 1f));
                button.style.borderTopColor = accentColor;
                button.style.borderBottomColor = accentColor;
                button.style.borderLeftColor = accentColor;
                button.style.borderRightColor = accentColor;
            });

            button.RegisterCallback<MouseLeaveEvent>((evt) =>
            {
                button.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 1f);
                button.style.scale = new Scale(Vector3.one);
                button.style.borderTopColor = new Color(0.3f, 0.5f, 0.9f, 1f);
                button.style.borderBottomColor = new Color(0.3f, 0.5f, 0.9f, 1f);
                button.style.borderLeftColor = new Color(0.3f, 0.5f, 0.9f, 1f);
                button.style.borderRightColor = new Color(0.3f, 0.5f, 0.9f, 1f);
            });

            return button;
        }

        Color GetFlagColor(string countryCode)
        {
            switch(countryCode)
            {
                case "GB":
                    return new Color(0.0f, 0.14f, 0.49f, 1f); // Bleu britannique
                case "FR":
                    return new Color(0.0f, 0.14f, 0.58f, 1f); // Bleu français
                default:
                    return new Color(0.3f, 0.3f, 0.3f, 1f); // Gris par défaut
            }
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

            // ScrollView pour le contenu
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;

            // Titre de la formation (sera mis à jour dynamiquement)
            var titleLabel = new Label("Formation Title");
            titleLabel.name = "training-title";
            titleLabel.style.fontSize = 36;
            titleLabel.style.color = accentColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
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

            var durationLabel = new Label("⏱️ Duration: 30 min");
            durationLabel.name = "training-duration";
            durationLabel.style.fontSize = 16;
            durationLabel.style.color = Color.white;
            durationLabel.style.marginRight = 30;
            infoContainer.Add(durationLabel);

            var difficultyLabel = new Label("📊 Difficulty: Intermediate");
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

            // Si pas de métadonnées, utiliser des valeurs par défaut
            string title = "Formation Test";
            string description = "Formation interactive de test";
            string duration = "30 minutes";
            string difficulty = "Débutant";

            // Essayer de charger depuis les métadonnées si disponibles
            if (trainingMetadata != null)
            {
                title = GetMetadataValue<string>(trainingMetadata, "title", title);
                description = GetMetadataValue<string>(trainingMetadata, "description", description);
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
                string durationText = lang == "fr" ? $"⏱️ Durée : {duration}" : $"⏱️ Duration: {duration}";
                durationLabel.text = durationText;
            }

            // Difficulté
            var difficultyLabel = disclaimerPanel.Q<Label>("training-difficulty");
            if (difficultyLabel != null)
            {
                // Traduire la difficulté si nécessaire
                string localizedDifficulty = difficulty;
                if (lang == "fr")
                {
                    switch(difficulty.ToLower())
                    {
                        case "easy": localizedDifficulty = "Facile"; break;
                        case "intermediate": localizedDifficulty = "Intermédiaire"; break;
                        case "hard": localizedDifficulty = "Avancé"; break;
                        case "very hard": localizedDifficulty = "Expert"; break;
                    }
                }
                string diffText = lang == "fr" ? $"📊 Difficulté : {localizedDifficulty}" : $"📊 Difficulty: {difficulty}";
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

        void OnBackToLanguageSelection()
        {
            StartCoroutine(TransitionToLanguageSelection());
        }

        void OnStartTraining()
        {
            if (debugMode) Debug.Log("[LanguageSelectionUI] Starting training");

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

            // Détecter automatiquement les objets interactables
            trainingHUD.AutoDetectInteractables();

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