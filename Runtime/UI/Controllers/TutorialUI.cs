using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Displays tutorial instructions after the disclaimer
    /// Shows controls and interface explanations before training starts
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
        public bool IsDisplaying { get; private set; } = false;

        // Events
        public System.Action OnTutorialCompleted;

        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Force visualTreeAsset to null to avoid conflicts
            if (uiDocument.visualTreeAsset != null)
            {
                if (debugMode) Debug.LogWarning("[TutorialUI] Clearing UXML to avoid conflicts");
                uiDocument.visualTreeAsset = null;
            }
        }

        /// <summary>
        /// Set the PanelSettings for this UI (used when created programmatically)
        /// </summary>
        public void SetPanelSettings(PanelSettings settings)
        {
            if (uiDocument != null && settings != null)
            {
                uiDocument.panelSettings = settings;
                if (debugMode) Debug.Log("[TutorialUI] PanelSettings assigned programmatically");
            }
        }

        /// <summary>
        /// Show the tutorial with the specified language
        /// </summary>
        public void Show(string languageCode = "en")
        {
            currentLanguage = languageCode;

            if (root == null)
            {
                root = uiDocument.rootVisualElement;
                root.Clear();
            }

            CreateTutorialPanel();
            IsDisplaying = true;

            // Fade in animation
            StartCoroutine(FadeIn());

            if (debugMode) Debug.Log($"[TutorialUI] Tutorial shown in {languageCode}");
        }

        /// <summary>
        /// Hide the tutorial
        /// </summary>
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

            // Content container
            var contentContainer = new VisualElement();
            contentContainer.style.width = 800;
            contentContainer.style.maxWidth = Length.Percent(90);
            contentContainer.style.paddingTop = 50;
            contentContainer.style.paddingBottom = 50;
            contentContainer.style.paddingLeft = 50;
            contentContainer.style.paddingRight = 50;
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

            // Title
            var titleLabel = new Label(GetText("title"));
            titleLabel.style.fontSize = 32;
            titleLabel.style.color = accentColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 40;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            contentContainer.Add(titleLabel);

            // Section 1: Movement
            CreateSection(contentContainer, GetText("movement_title"), GetText("movement_desc"));

            // Separator
            CreateSeparator(contentContainer);

            // Section 2: Scenario Navigation
            CreateSection(contentContainer, GetText("navigation_title"), GetText("navigation_desc"));

            // Separator
            CreateSeparator(contentContainer);

            // Section 3: Interface
            CreateSection(contentContainer, GetText("interface_title"), GetText("interface_desc"));

            // Start button
            var buttonContainer = new VisualElement();
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.marginTop = 40;

            var startButton = new Button(() => OnStartButtonClicked());
            startButton.text = GetText("start_button");
            startButton.style.width = 350;
            startButton.style.height = 65;
            startButton.style.fontSize = 20;
            startButton.style.backgroundColor = accentColor;
            startButton.style.color = Color.white;
            startButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            startButton.style.borderTopLeftRadius = 10;
            startButton.style.borderTopRightRadius = 10;
            startButton.style.borderBottomLeftRadius = 10;
            startButton.style.borderBottomRightRadius = 10;
            buttonContainer.Add(startButton);

            contentContainer.Add(buttonContainer);
            tutorialPanel.Add(contentContainer);
            root.Add(tutorialPanel);
        }

        void CreateSection(VisualElement parent, string title, string description)
        {
            var section = new VisualElement();
            section.style.marginBottom = 25;

            // Title
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 22;
            titleLabel.style.color = primaryColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;

            section.Add(titleLabel);

            // Description
            var descLabel = new Label(description);
            descLabel.style.fontSize = 16;
            descLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            section.Add(descLabel);

            parent.Add(section);
        }

        void CreateSeparator(VisualElement parent)
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            separator.style.marginTop = 15;
            separator.style.marginBottom = 15;
            parent.Add(separator);
        }

        string GetText(string key)
        {
            if (currentLanguage == "fr")
            {
                return key switch
                {
                    "title" => "Comment utiliser la formation",
                    "movement_title" => "Déplacement",
                    "movement_desc" => "Utilisez les touches WASD ou les flèches pour vous déplacer. Déplacez la souris pour regarder autour de vous et explorer l'environnement.",
                    "navigation_title" => "Navigation des scénarios",
                    "navigation_desc" => "Cliquez sur le bouton 'Scénario suivant' en haut de l'écran pour progresser dans la formation. Complétez chaque scénario avant de passer au suivant.",
                    "interface_title" => "Interface",
                    "interface_desc" => "Le chronomètre en haut suit votre temps de formation. La barre de progression montre votre avancement.",
                    "start_button" => "Commencer la formation",
                    _ => key
                };
            }
            else // English
            {
                return key switch
                {
                    "title" => "How to Use the Training",
                    "movement_title" => "Movement",
                    "movement_desc" => "Use WASD keys or arrow keys to move. Move the mouse to look around and explore the environment.",
                    "navigation_title" => "Scenario Navigation",
                    "navigation_desc" => "Click the 'Next Scenario' button at the top of the screen to progress through the training. Complete each scenario before moving to the next.",
                    "interface_title" => "Interface",
                    "interface_desc" => "The timer at the top tracks your training time. The progress bar shows your advancement.",
                    "start_button" => "Start Training",
                    _ => key
                };
            }
        }

        void OnStartButtonClicked()
        {
            if (debugMode) Debug.Log("[TutorialUI] Start button clicked");
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
                float t = elapsed / animationDuration;
                tutorialPanel.style.opacity = Mathf.Lerp(0, 1, t);
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
                float t = elapsed / animationDuration;
                tutorialPanel.style.opacity = Mathf.Lerp(1, 0, t);
                yield return null;
            }

            tutorialPanel.style.display = DisplayStyle.None;
            IsDisplaying = false;

            if (debugMode) Debug.Log("[TutorialUI] Tutorial hidden");
        }
    }
}
