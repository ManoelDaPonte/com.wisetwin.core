using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using WiseTwin.UI;

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

            selectedMode = ControlMode.KeyboardMouse;
            ControlModeSettings.SetMode(ControlMode.KeyboardMouse);

            // Réactiver le UIDocument s'il a été désactivé
            if (uiDocument != null && !uiDocument.enabled)
            {
                uiDocument.enabled = true;
            }

            if (root == null)
            {
                root = uiDocument.rootVisualElement;
                root.pickingMode = PickingMode.Ignore;
            }

            if (IsDisplaying && tutorialPanel != null && tutorialPanel.style.display == DisplayStyle.Flex)
            {
                return;
            }

            if (tutorialPanel != null && tutorialPanel.parent != null)
            {
                root.Remove(tutorialPanel);
            }

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
            tutorialPanel.style.backgroundColor = UIStyles.BgDeep;
            tutorialPanel.style.alignItems = Align.Center;
            tutorialPanel.style.justifyContent = Justify.Center;
            tutorialPanel.pickingMode = PickingMode.Position;

            var contentContainer = new VisualElement();
            contentContainer.style.width = 720;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.maxHeight = Length.Percent(90);
            UIStyles.ApplyCardStyle(contentContainer, UIStyles.RadiusXL);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // Setup minimal scrollbar
            scrollView.RegisterCallback<AttachToPanelEvent>(evt => UIStyles.ApplyMinimalScrollbar(scrollView));
            scrollView.RegisterCallback<GeometryChangedEvent>(evt => UIStyles.ApplyMinimalScrollbar(scrollView));

            var scrollContent = new VisualElement();
            scrollContent.style.paddingTop = UIStyles.Space3XL;
            scrollContent.style.paddingBottom = UIStyles.Space3XL;
            scrollContent.style.paddingLeft = UIStyles.Space3XL;
            scrollContent.style.paddingRight = UIStyles.Space3XL;

            // Title
            var titleLabel = UIStyles.CreateTitle(GetText("title"), UIStyles.Font2XL);
            titleLabel.style.marginBottom = UIStyles.SpaceXL;
            scrollContent.Add(titleLabel);

            // Control mode selection
            CreateControlModeSelection(scrollContent);

            scrollContent.Add(UIStyles.CreateSeparator(UIStyles.SpaceLG));

            // Movement section
            var movementSection = new VisualElement();
            movementSection.style.marginBottom = UIStyles.SpaceLG;

            var moveTitleLabel = CreateSectionTitle(GetText("movement_title"));
            movementSection.Add(moveTitleLabel);

            movementDescLabel = UIStyles.CreateBodyText(GetMovementDesc(), UIStyles.FontBase);
            movementDescLabel.style.color = UIStyles.TextSecondary;
            movementSection.Add(movementDescLabel);
            scrollContent.Add(movementSection);

            scrollContent.Add(UIStyles.CreateSeparator(UIStyles.SpaceLG));

            // Procedures
            CreateSection(scrollContent, GetText("procedures_title"), GetText("procedures_desc"));
            scrollContent.Add(UIStyles.CreateSeparator(UIStyles.SpaceLG));

            // Questions
            CreateSection(scrollContent, GetText("questions_title"), GetText("questions_desc"));
            scrollContent.Add(UIStyles.CreateSeparator(UIStyles.SpaceLG));

            // Interface
            CreateSection(scrollContent, GetText("interface_title"), GetText("interface_desc"));

            // Next button
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.marginTop = UIStyles.Space2XL;

            var startButton = UIStyles.CreatePrimaryButton(GetText("start_button"), () => OnStartButtonClicked());
            startButton.style.width = 280;
            startButton.style.height = 52;
            buttonContainer.Add(startButton);

            scrollContent.Add(buttonContainer);
            scrollView.Add(scrollContent);
            contentContainer.Add(scrollView);
            tutorialPanel.Add(contentContainer);
            root.Add(tutorialPanel);
        }

        void CreateControlModeSelection(VisualElement parent)
        {
            var modeLabel = CreateSectionTitle(GetText("control_mode_title"));
            modeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            parent.Add(modeLabel);

            var cardsRow = new VisualElement();
            cardsRow.style.flexDirection = FlexDirection.Row;
            cardsRow.style.justifyContent = Justify.Center;
            cardsRow.style.marginBottom = UIStyles.SpaceMD;

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
            spacer.style.width = UIStyles.SpaceLG;
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
            card.style.width = 260;
            UIStyles.SetPadding(card, UIStyles.SpaceLG);
            UIStyles.SetBorderRadius(card, UIStyles.RadiusMD);
            UIStyles.SetBorderWidth(card, 2);
            card.style.alignItems = Align.Center;
            card.pickingMode = PickingMode.Position;

            ApplyControlCardStyle(card, isSelected);

            var iconLabel = new Label(icon);
            iconLabel.style.fontSize = UIStyles.FontXL;
            iconLabel.style.color = UIStyles.TextPrimary;
            iconLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconLabel.style.marginBottom = UIStyles.SpaceSM;
            iconLabel.pickingMode = PickingMode.Ignore;
            card.Add(iconLabel);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = UIStyles.FontBase;
            titleLabel.style.color = UIStyles.TextPrimary;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.pickingMode = PickingMode.Ignore;
            card.Add(titleLabel);

            return card;
        }

        void ApplyControlCardStyle(VisualElement card, bool isSelected)
        {
            Color borderColor = isSelected ? UIStyles.Accent : UIStyles.BorderSubtle;
            UIStyles.SetBorderColor(card, borderColor);
            card.style.backgroundColor = isSelected
                ? new Color(UIStyles.Accent.r, UIStyles.Accent.g, UIStyles.Accent.b, 0.12f)
                : UIStyles.BgInput;
        }

        void SelectControlMode(ControlMode mode)
        {
            selectedMode = mode;
            ControlModeSettings.SetMode(mode);

            if (keyboardCard != null)
                ApplyControlCardStyle(keyboardCard, mode == ControlMode.KeyboardMouse);
            if (mouseCard != null)
                ApplyControlCardStyle(mouseCard, mode == ControlMode.MouseOnly);

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

        Label CreateSectionTitle(string text)
        {
            var label = new Label(text);
            label.style.fontSize = UIStyles.FontMD;
            label.style.color = UIStyles.Info;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = UIStyles.SpaceSM;
            return label;
        }

        void CreateSection(VisualElement parent, string title, string description)
        {
            var section = new VisualElement();
            section.style.marginBottom = UIStyles.SpaceLG;

            section.Add(CreateSectionTitle(title));

            var descLabel = UIStyles.CreateBodyText(description, UIStyles.FontBase);
            descLabel.style.color = UIStyles.TextSecondary;
            section.Add(descLabel);

            parent.Add(section);
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

            // Désactiver le UIDocument pour ne pas bloquer les clics sur le HUD
            if (uiDocument != null)
            {
                uiDocument.enabled = false;
            }

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
