using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WiseTwin
{
    /// <summary>
    /// Panneau de transition entre les scénarios.
    /// Affiche un écran centré avec titre, sous-titre et bouton d'action.
    /// Deux modes : Start (démarrage) et Transition (entre scénarios).
    /// </summary>
    public class ScenarioTransitionPanel : MonoBehaviour
    {
        public static ScenarioTransitionPanel Instance { get; private set; }

        public event Action OnActionButtonClicked;

        // UI
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement backdrop;
        private VisualElement panel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Button actionButton;

        // State
        private bool isVisible = false;
        public bool IsVisible => isVisible;

        // Animation
        private const float FadeDuration = 0.3f;
        private Coroutine fadeCoroutine;

        // Accent color matching the rest of the UI
        private static readonly Color AccentColor = new Color(0.1f, 0.8f, 0.6f, 1f);

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupUIDocument();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Assigne le PanelSettings (appelé par le créateur du GO)
        /// </summary>
        public void SetPanelSettings(PanelSettings settings)
        {
            if (uiDocument != null)
            {
                uiDocument.panelSettings = settings;
            }
        }

        void SetupUIDocument()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }
            uiDocument.visualTreeAsset = null; // UI construite en code

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            CreatePanel();
        }

        void CreatePanel()
        {
            root.Clear();
            root.pickingMode = PickingMode.Ignore;

            // Backdrop plein écran
            backdrop = new VisualElement();
            backdrop.name = "transition-backdrop";
            backdrop.style.position = Position.Absolute;
            backdrop.style.left = 0;
            backdrop.style.top = 0;
            backdrop.style.width = Length.Percent(100);
            backdrop.style.height = Length.Percent(100);
            backdrop.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            backdrop.style.justifyContent = Justify.Center;
            backdrop.style.alignItems = Align.Center;
            backdrop.pickingMode = PickingMode.Position;
            backdrop.style.display = DisplayStyle.None;

            // Panneau central
            panel = new VisualElement();
            panel.name = "transition-panel";
            panel.style.width = 500;
            panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            panel.style.borderTopLeftRadius = 20;
            panel.style.borderTopRightRadius = 20;
            panel.style.borderBottomLeftRadius = 20;
            panel.style.borderBottomRightRadius = 20;
            panel.style.borderLeftWidth = 2;
            panel.style.borderRightWidth = 2;
            panel.style.borderTopWidth = 2;
            panel.style.borderBottomWidth = 2;
            panel.style.borderLeftColor = AccentColor;
            panel.style.borderRightColor = AccentColor;
            panel.style.borderTopColor = AccentColor;
            panel.style.borderBottomColor = AccentColor;
            panel.style.paddingTop = 40;
            panel.style.paddingBottom = 40;
            panel.style.paddingLeft = 40;
            panel.style.paddingRight = 40;
            panel.style.alignItems = Align.Center;

            // Titre
            titleLabel = new Label();
            titleLabel.name = "transition-title";
            titleLabel.style.fontSize = 28;
            titleLabel.style.color = AccentColor;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.marginBottom = 15;
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(titleLabel);

            // Sous-titre
            subtitleLabel = new Label();
            subtitleLabel.name = "transition-subtitle";
            subtitleLabel.style.fontSize = 16;
            subtitleLabel.style.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitleLabel.style.marginBottom = 30;
            subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(subtitleLabel);

            // Bouton d'action
            actionButton = new Button();
            actionButton.name = "transition-action-button";
            actionButton.style.width = 350;
            actionButton.style.height = 60;
            actionButton.style.fontSize = 20;
            actionButton.style.backgroundColor = AccentColor;
            actionButton.style.color = Color.white;
            actionButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionButton.style.borderTopLeftRadius = 12;
            actionButton.style.borderTopRightRadius = 12;
            actionButton.style.borderBottomLeftRadius = 12;
            actionButton.style.borderBottomRightRadius = 12;
            actionButton.style.borderLeftWidth = 0;
            actionButton.style.borderRightWidth = 0;
            actionButton.style.borderTopWidth = 0;
            actionButton.style.borderBottomWidth = 0;
            actionButton.clicked += OnButtonClicked;
            panel.Add(actionButton);

            backdrop.Add(panel);
            root.Add(backdrop);

            // S'abonner aux changements de langue
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        /// <summary>
        /// Affiche le panneau de démarrage de la formation
        /// </summary>
        public void ShowStartPanel(int totalScenarios)
        {
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            titleLabel.text = GetLocalizedText("start_title", lang);
            subtitleLabel.text = GetLocalizedText("start_subtitle", lang);
            actionButton.text = GetLocalizedText("start_button", lang);

            ShowInternal();
        }

        /// <summary>
        /// Affiche le panneau de transition entre scénarios
        /// </summary>
        public void ShowTransitionPanel(int completedIndex, int totalScenarios)
        {
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            string titleTemplate = GetLocalizedText("transition_title", lang);
            titleLabel.text = string.Format(titleTemplate, completedIndex + 1, totalScenarios);

            subtitleLabel.text = GetLocalizedText("transition_subtitle", lang);
            actionButton.text = GetLocalizedText("transition_button", lang);

            ShowInternal();
        }

        /// <summary>
        /// Cache le panneau
        /// </summary>
        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;

            // Réactiver les contrôles du joueur
            SetPlayerControlsEnabled(true);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAndHide());
        }

        void ShowInternal()
        {
            isVisible = true;

            // Bloquer les contrôles du joueur
            SetPlayerControlsEnabled(false);

            if (backdrop != null)
            {
                backdrop.style.display = DisplayStyle.Flex;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());
        }

        void OnButtonClicked()
        {
            if (!isVisible) return;
            OnActionButtonClicked?.Invoke();
        }

        void SetPlayerControlsEnabled(bool enabled)
        {
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(enabled);
            }
        }

        void OnLanguageChanged(string newLanguage)
        {
            // Si le panneau est visible, rafraîchir les textes
            // On ne peut pas savoir quel mode était affiché, donc on ne rafraîchit pas automatiquement
            // Les textes seront corrects au prochain Show
        }

        IEnumerator FadeIn()
        {
            if (backdrop == null) yield break;

            backdrop.style.opacity = 0;
            float elapsed = 0f;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                backdrop.style.opacity = Mathf.Lerp(0f, 1f, elapsed / FadeDuration);
                yield return null;
            }

            backdrop.style.opacity = 1f;
            fadeCoroutine = null;
        }

        IEnumerator FadeOutAndHide()
        {
            if (backdrop == null) yield break;

            float elapsed = 0f;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                backdrop.style.opacity = Mathf.Lerp(1f, 0f, elapsed / FadeDuration);
                yield return null;
            }

            backdrop.style.opacity = 0f;
            backdrop.style.display = DisplayStyle.None;
            fadeCoroutine = null;
        }

        // Dictionnaire de traductions
        static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "start_title", new Dictionary<string, string>
                {
                    { "en", "Ready to Begin" },
                    { "fr", "Pr\u00eat \u00e0 commencer" }
                }
            },
            {
                "start_subtitle", new Dictionary<string, string>
                {
                    { "en", "Click to start training" },
                    { "fr", "Cliquez pour d\u00e9marrer" }
                }
            },
            {
                "start_button", new Dictionary<string, string>
                {
                    { "en", "Start Training" },
                    { "fr", "D\u00e9marrer la formation" }
                }
            },
            {
                "transition_title", new Dictionary<string, string>
                {
                    { "en", "Scenario {0}/{1} Complete" },
                    { "fr", "Sc\u00e9nario {0}/{1} termin\u00e9" }
                }
            },
            {
                "transition_subtitle", new Dictionary<string, string>
                {
                    { "en", "Click to continue" },
                    { "fr", "Cliquez pour continuer" }
                }
            },
            {
                "transition_button", new Dictionary<string, string>
                {
                    { "en", "Continue" },
                    { "fr", "Continuer" }
                }
            }
        };

        string GetLocalizedText(string key, string lang)
        {
            if (Translations.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var text)) return text;
                if (dict.TryGetValue("en", out var fallback)) return fallback;
            }
            return key;
        }
    }
}
