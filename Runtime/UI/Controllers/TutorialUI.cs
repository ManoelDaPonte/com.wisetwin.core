using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Displays tutorial instructions after the disclaimer.
    /// Includes control mode selection (keyboard+mouse vs mouse-only).
    /// Shows controls and interface explanations before training starts.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float animationDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color accentColor = new Color(0.1f, 0.8f, 0.6f);
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // UI References
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement tutorialPanel;

        // State
        private string currentLanguage = "en";
        private ControlMode selectedMode = ControlMode.KeyboardMouse;
        public bool IsDisplaying { get; private set; } = false;

        // UI references for dynamic updates
        private VisualElement keyboardCard;
        private VisualElement mouseCard;
        private Label movementDescLabel;

        // Events
        public System.Action OnTutorialCompleted;

        // Singleton
        public static TutorialUI Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            if (uiDocument.visualTreeAsset != null)
            {
                uiDocument.visualTreeAsset = null;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        public void SetPanelSettings(PanelSettings settings)
        {
            if (uiDocument != null && settings != null)
            {
                uiDocument.panelSettings = settings;
            }
        }

        public void Show(string languageCode = "")
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                currentLanguage = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            }
            else
            {
                currentLanguage = languageCode;
            }

            // Reset control mode to default on each show (fresh start)
            selectedMode = ControlMode.KeyboardMouse;
            ControlModeSettings.SetMode(ControlMode.KeyboardMouse);

            if (root == null)
            {
                root = uiDocument.rootVisualElement;
            }

            if (IsDisplaying && tutorialPanel != null && tutorialPanel.style.display == DisplayStyle.Flex)
            {
                return;
            }

            if (tutorialPanel != null && tutorialPanel.parent != null)
            {
                root.Remove(tutorialPanel);
            }

            // Block all player controls
            PlayerControls.SetEnabled(false);

            CreateTutorialPanel();
            IsDisplaying = true;

            StartCoroutine(FadeIn());

            if (debugMode) Debug.Log($"[TutorialUI] Tutorial shown in {currentLanguage}");
        }

        public void Hide()
        {
            StartCoroutine(FadeOutAndHide());
        }

        void CreateTutorialPanel()
        {
            tutorialPanel = new VisualElement();
            tutorialPanel.name = "tutorial-panel";
            tutorialPanel.style.position = Position.Absolute;
            tutorialPanel.style.width = Length.Percent(100);
            tutorialPanel.style.height = Length.Percent(100);
            tutorialPanel.style.backgroundColor = backgroundColor;
            tutorialPanel.style.alignItems = Align.Center;
            tutorialPanel.style.justifyContent = Justify.Center;
            tutorialPanel.pickingMode = PickingMode.Position;

            var contentContainer = new VisualElement();
            contentContainer.style.width = 750;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.maxHeight = Length.Percent(90);
            contentContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.98f);
            contentContainer.style.borderTopLeftRadius = 15;
            contentContainer.style.borderTopRightRadius = 15;
            contentContainer.style.borderBottomLeftRadius = 15;
            contentContainer.style.borderBottomRightRadius = 15;
            contentContainer.style.borderTopWidth = 1;
            contentContainer.style.borderBottomWidth = 1;
            contentContainer.style.borderLeftWidth = 1;
            contentContainer.style.borderRightWidth = 1;
            contentContainer.style.borderTopColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderBottomColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderLeftColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
            contentContainer.style.borderRightColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var scrollContent = new VisualElement();
            scrollContent.style.paddingTop = 40;
            scrollContent.style.paddingBottom = 40;
            scrollContent.style.paddingLeft = 45;
            scrollContent.style.paddingRight = 45;

            // Title
            var titleLabel = new Label(GetText("title"));
            titleLabel.style.fontSize = 28;
            titleLabel.style.color = accentColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 25;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            scrollContent.Add(titleLabel);

            // Control mode selection
            CreateControlModeSelection(scrollContent);

            CreateSeparator(scrollContent);

            // Movement section (adapts to selected mode)
            var movementSection = new VisualElement();
            movementSection.style.marginBottom = 20;

            var moveTitleLabel = new Label(GetText("movement_title"));
            moveTitleLabel.style.fontSize = 20;
            moveTitleLabel.style.color = primaryColor;
            moveTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            moveTitleLabel.style.marginBottom = 8;
            movementSection.Add(moveTitleLabel);

            movementDescLabel = new Label(GetMovementDesc());
            movementDescLabel.style.fontSize = 15;
            movementDescLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            movementDescLabel.style.whiteSpace = WhiteSpace.Normal;
            movementSection.Add(movementDescLabel);
            scrollContent.Add(movementSection);

            CreateSeparator(scrollContent);

            // Procedures
            CreateSection(scrollContent, GetText("procedures_title"), GetText("procedures_desc"));
            CreateSeparator(scrollContent);

            // Questions
            CreateSection(scrollContent, GetText("questions_title"), GetText("questions_desc"));
            CreateSeparator(scrollContent);

            // Interface
            CreateSection(scrollContent, GetText("interface_title"), GetText("interface_desc"));

            // Next button
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.marginTop = 30;

            var startButton = new Button(() => OnStartButtonClicked());
            startButton.text = GetText("start_button");
            startButton.style.width = 300;
            startButton.style.height = 55;
            startButton.style.fontSize = 19;
            startButton.style.backgroundColor = accentColor;
            startButton.style.color = Color.white;
            startButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            startButton.style.borderTopLeftRadius = 10;
            startButton.style.borderTopRightRadius = 10;
            startButton.style.borderBottomLeftRadius = 10;
            startButton.style.borderBottomRightRadius = 10;
            buttonContainer.Add(startButton);

            scrollContent.Add(buttonContainer);
            scrollView.Add(scrollContent);
            contentContainer.Add(scrollView);
            tutorialPanel.Add(contentContainer);
            root.Add(tutorialPanel);
        }

        void CreateControlModeSelection(VisualElement parent)
        {
            var modeLabel = new Label(GetText("control_mode_title"));
            modeLabel.style.fontSize = 18;
            modeLabel.style.color = primaryColor;
            modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            modeLabel.style.marginBottom = 12;
            modeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            parent.Add(modeLabel);

            var cardsRow = new VisualElement();
            cardsRow.style.flexDirection = FlexDirection.Row;
            cardsRow.style.justifyContent = Justify.Center;
            cardsRow.style.marginBottom = 10;

            keyboardCard = CreateControlModeCard(
                GetText("mode_keyboard_title"),
                "WASD",
                true
            );
            keyboardCard.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                SelectControlMode(ControlMode.KeyboardMouse);
            });
            cardsRow.Add(keyboardCard);

            var spacer = new VisualElement();
            spacer.style.width = 20;
            cardsRow.Add(spacer);

            mouseCard = CreateControlModeCard(
                GetText("mode_mouse_title"),
                GetText("mode_mouse_icon"),
                false
            );
            mouseCard.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                SelectControlMode(ControlMode.MouseOnly);
            });
            cardsRow.Add(mouseCard);

            parent.Add(cardsRow);
        }

        VisualElement CreateControlModeCard(string title, string icon, bool isSelected)
        {
            var card = new VisualElement();
            card.style.width = 280;
            card.style.paddingTop = 18;
            card.style.paddingBottom = 18;
            card.style.paddingLeft = 15;
            card.style.paddingRight = 15;
            card.style.borderTopLeftRadius = 12;
            card.style.borderTopRightRadius = 12;
            card.style.borderBottomLeftRadius = 12;
            card.style.borderBottomRightRadius = 12;
            card.style.borderTopWidth = 2;
            card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth = 2;
            card.style.borderRightWidth = 2;
            card.style.alignItems = Align.Center;
            card.pickingMode = PickingMode.Position;

            ApplyCardStyle(card, isSelected);

            var iconLabel = new Label(icon);
            iconLabel.style.fontSize = 26;
            iconLabel.style.color = Color.white;
            iconLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconLabel.style.marginBottom = 8;
            iconLabel.pickingMode = PickingMode.Ignore;
            card.Add(iconLabel);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 16;
            titleLabel.style.color = Color.white;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.pickingMode = PickingMode.Ignore;
            card.Add(titleLabel);

            return card;
        }

        void ApplyCardStyle(VisualElement card, bool isSelected)
        {
            Color borderColor = isSelected ? accentColor : new Color(0.3f, 0.3f, 0.35f, 0.5f);
            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;
            card.style.backgroundColor = isSelected
                ? new Color(0.1f, 0.25f, 0.2f, 1f)
                : new Color(0.18f, 0.18f, 0.22f, 1f);
        }

        void SelectControlMode(ControlMode mode)
        {
            selectedMode = mode;
            ControlModeSettings.SetMode(mode);

            if (keyboardCard != null)
                ApplyCardStyle(keyboardCard, mode == ControlMode.KeyboardMouse);
            if (mouseCard != null)
                ApplyCardStyle(mouseCard, mode == ControlMode.MouseOnly);

            if (movementDescLabel != null)
                movementDescLabel.text = GetMovementDesc();

            if (debugMode) Debug.Log($"[TutorialUI] Control mode selected: {mode}");
        }

        string GetMovementDesc()
        {
            if (selectedMode == ControlMode.MouseOnly)
            {
                return currentLanguage == "fr"
                    ? "Clic gauche sur le sol pour vous deplacer. Clic droit maintenu pour tourner la camera. Molette pour zoomer."
                    : "Left-click on the ground to move. Hold right-click to orbit the camera. Scroll wheel to zoom.";
            }
            return GetText("movement_desc");
        }

        void CreateSection(VisualElement parent, string title, string description)
        {
            var section = new VisualElement();
            section.style.marginBottom = 20;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 20;
            titleLabel.style.color = primaryColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            section.Add(titleLabel);

            var descLabel = new Label(description);
            descLabel.style.fontSize = 15;
            descLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            section.Add(descLabel);

            parent.Add(section);
        }

        void CreateSeparator(VisualElement parent)
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            separator.style.marginTop = 12;
            separator.style.marginBottom = 12;
            parent.Add(separator);
        }

        string GetText(string key)
        {
            if (currentLanguage == "fr")
            {
                return key switch
                {
                    "title" => "Comment utiliser la formation",
                    "control_mode_title" => "Mode de controle",
                    "mode_keyboard_title" => "Clavier + Souris",
                    "mode_mouse_title" => "Souris uniquement",
                    "mode_mouse_icon" => "Clic",
                    "movement_title" => "Deplacement & Camera",
                    "movement_desc" => "WASD ou fleches pour se deplacer. Clic droit maintenu pour regarder autour. Molette pour zoomer.",
                    "procedures_title" => "Procedures",
                    "procedures_desc" => "Cliquez sur les objets qui clignotent pour valider les etapes. Si plusieurs clignotent, trouvez le bon !",
                    "questions_title" => "Questions",
                    "questions_desc" => "Lisez attentivement. L'indication sous les reponses precise si c'est un choix unique ou multiple.",
                    "interface_title" => "Interface",
                    "interface_desc" => "La barre en haut montre votre progression. Le bouton rouge permet de recommencer la formation.",
                    "start_button" => "Suivant",
                    _ => key
                };
            }
            else
            {
                return key switch
                {
                    "title" => "How to Use the Training",
                    "control_mode_title" => "Control mode",
                    "mode_keyboard_title" => "Keyboard + Mouse",
                    "mode_mouse_title" => "Mouse Only",
                    "mode_mouse_icon" => "Click",
                    "movement_title" => "Movement & Camera",
                    "movement_desc" => "WASD or arrow keys to move. Hold right-click to look around. Scroll wheel to zoom.",
                    "procedures_title" => "Procedures",
                    "procedures_desc" => "Click on blinking objects to validate steps. If multiple are blinking, find the correct one!",
                    "questions_title" => "Questions",
                    "questions_desc" => "Read carefully. The label below answers indicates single choice or multiple choice.",
                    "interface_title" => "Interface",
                    "interface_desc" => "The top bar shows your progress. The red button lets you restart the training.",
                    "start_button" => "Next",
                    _ => key
                };
            }
        }

        void OnStartButtonClicked()
        {
            if (debugMode) Debug.Log("[TutorialUI] Next button clicked");
            OnTutorialCompleted?.Invoke();
            Hide();
        }

        IEnumerator FadeIn()
        {
            if (tutorialPanel == null) yield break;

            tutorialPanel.style.opacity = 0;
            tutorialPanel.style.display = DisplayStyle.Flex;

            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                tutorialPanel.style.opacity = Mathf.Lerp(0, 1, elapsed / animationDuration);
                yield return null;
            }

            tutorialPanel.style.opacity = 1;
        }

        IEnumerator FadeOutAndHide()
        {
            if (tutorialPanel == null) yield break;

            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                tutorialPanel.style.opacity = Mathf.Lerp(1, 0, elapsed / animationDuration);
                yield return null;
            }

            tutorialPanel.style.display = DisplayStyle.None;
            IsDisplaying = false;

            // Re-enable controls
            PlayerControls.SetEnabled(true);

            if (debugMode) Debug.Log("[TutorialUI] Tutorial hidden");
        }

        void OnLanguageChanged(string newLanguage)
        {
            currentLanguage = newLanguage;

            if (IsDisplaying && tutorialPanel != null && tutorialPanel.style.display == DisplayStyle.Flex)
            {
                if (tutorialPanel.parent != null)
                {
                    root.Remove(tutorialPanel);
                }

                CreateTutorialPanel();
                tutorialPanel.style.opacity = 1;
            }
        }
    }
}
