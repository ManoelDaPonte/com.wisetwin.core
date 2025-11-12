using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Displays tutorial instructions after the disclaimer
    /// Shows controls and interface explanations before training starts
    /// Can be reopened at any time via the help button
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

        // Singleton
        public static TutorialUI Instance { get; private set; }

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

            // S'abonner aux changements de langue
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        void OnDestroy()
        {
            // Se désabonner des événements
            if (Instance == this)
            {
                Instance = null;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
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
        /// Can be called multiple times to reopen the tutorial
        /// </summary>
        public void Show(string languageCode = "")
        {
            // Use current language from LocalizationManager if not specified
            if (string.IsNullOrEmpty(languageCode))
            {
                currentLanguage = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            }
            else
            {
                currentLanguage = languageCode;
            }

            if (root == null)
            {
                root = uiDocument.rootVisualElement;
            }

            // Si le panel existe déjà et qu'on est en train de l'afficher, ne rien faire
            if (IsDisplaying && tutorialPanel != null && tutorialPanel.style.display == DisplayStyle.Flex)
            {
                if (debugMode) Debug.Log("[TutorialUI] Tutorial already displaying");
                return;
            }

            // Recréer le panel avec la langue actuelle
            if (tutorialPanel != null && tutorialPanel.parent != null)
            {
                root.Remove(tutorialPanel);
            }

            // Bloquer les contrôles du personnage pendant le tutorial
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(false);
            }

            CreateTutorialPanel();
            IsDisplaying = true;

            // Fade in animation
            StartCoroutine(FadeIn());

            if (debugMode) Debug.Log($"[TutorialUI] Tutorial shown in {currentLanguage}");
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

            // Section 2: Interactive Procedures
            CreateSection(contentContainer, GetText("procedures_title"), GetText("procedures_desc"));

            // Separator
            CreateSeparator(contentContainer);

            // Section 3: Questions
            CreateSection(contentContainer, GetText("questions_title"), GetText("questions_desc"));

            // Separator
            CreateSeparator(contentContainer);

            // Section 4: Interface
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
                    "movement_title" => "Déplacement & Caméra",
                    "movement_desc" => "Utilisez WASD ou les flèches pour vous déplacer. Maintenez le clic droit de la souris et déplacez-la pour regarder autour de vous. Utilisez la molette de la souris pour zoomer : zoomez complètement pour passer en vue première personne.",
                    "procedures_title" => "Procédures interactives",
                    "procedures_desc" => "Les objets qui clignotent doivent être cliqués pour valider une étape. Attention : si plusieurs objets clignotent en même temps, vous devez trouver et cliquer sur le bon objet !",
                    "questions_title" => "Questions",
                    "questions_desc" => "Lisez attentivement les questions. L'indication sous les réponses vous précise s'il s'agit d'un choix unique (une seule réponse) ou d'un choix multiple (plusieurs réponses possibles).",
                    "interface_title" => "Interface & Résultats",
                    "interface_desc" => "Le chronomètre en haut suit votre temps de formation. La barre de progression montre votre avancement. Cliquez sur le bouton '>' pour passer au prochain scénario. Le bouton '?' vous permet de rouvrir ce tutoriel à tout moment. Le bouton '↻' vous ramène à votre position de départ si vous êtes bloqué. Votre score détaillé s'affichera à la fin de la formation.",
                    "start_button" => "Suivant",
                    _ => key
                };
            }
            else // English
            {
                return key switch
                {
                    "title" => "How to Use the Training",
                    "movement_title" => "Movement & Camera",
                    "movement_desc" => "Use WASD or arrow keys to move. Hold right mouse button and move the mouse to look around. Use the mouse wheel to zoom: zoom in completely to switch to first-person view.",
                    "procedures_title" => "Interactive Procedures",
                    "procedures_desc" => "Blinking objects must be clicked to validate a step. Beware: if multiple objects are blinking at the same time, you must find and click the correct one!",
                    "questions_title" => "Questions",
                    "questions_desc" => "Read the questions carefully. The instruction below the answers tells you if it's a single choice (one answer) or multiple choice (several answers possible).",
                    "interface_title" => "Interface & Results",
                    "interface_desc" => "The timer at the top tracks your training time. The progress bar shows your advancement. Click the '>' button to move to the next scenario. The '?' button allows you to reopen this tutorial at any time. The '↻' button resets your position to the starting point if you get stuck. Your detailed score will be displayed at the end of the training.",
                    "start_button" => "Next",
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

            // Débloquer les contrôles du personnage
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(true);
            }

            if (debugMode) Debug.Log("[TutorialUI] Tutorial hidden");
        }

        /// <summary>
        /// Called when language changes - refresh the tutorial if it's currently displayed
        /// </summary>
        void OnLanguageChanged(string newLanguage)
        {
            currentLanguage = newLanguage;

            // Si le tutoriel est actuellement affiché, le recréer avec la nouvelle langue
            if (IsDisplaying && tutorialPanel != null && tutorialPanel.style.display == DisplayStyle.Flex)
            {
                if (debugMode) Debug.Log($"[TutorialUI] Language changed to {newLanguage}, refreshing tutorial");

                // Recréer le panel sans bloquer à nouveau les contrôles (déjà bloqués)
                if (tutorialPanel.parent != null)
                {
                    root.Remove(tutorialPanel);
                }

                CreateTutorialPanel();
                tutorialPanel.style.opacity = 1; // Déjà affiché, pas de fade
            }
        }
    }
}
