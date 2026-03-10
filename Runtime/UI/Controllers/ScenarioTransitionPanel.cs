using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using WiseTwin.UI;

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
            uiDocument.visualTreeAsset = null;

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
            UIStyles.ApplyBackdropStyle(backdrop);
            backdrop.style.display = DisplayStyle.None;

            // Panneau central
            panel = new VisualElement();
            panel.name = "transition-panel";
            panel.style.width = 500;
            panel.style.maxWidth = Length.Percent(90);
            UIStyles.ApplyCardStyle(panel, UIStyles.RadiusXL);
            UIStyles.SetBorderWidth(panel, 2);
            UIStyles.SetBorderColor(panel, UIStyles.Accent);
            UIStyles.SetPadding(panel, UIStyles.Space3XL);
            panel.style.alignItems = Align.Center;

            // Titre
            titleLabel = UIStyles.CreateTitle("", UIStyles.Font2XL);
            titleLabel.name = "transition-title";
            titleLabel.style.marginBottom = UIStyles.SpaceLG;
            panel.Add(titleLabel);

            // Sous-titre
            subtitleLabel = UIStyles.CreateSubtitle("", UIStyles.FontBase);
            subtitleLabel.name = "transition-subtitle";
            subtitleLabel.style.color = UIStyles.TextMuted;
            subtitleLabel.style.marginBottom = UIStyles.Space2XL;
            panel.Add(subtitleLabel);

            // Bouton d'action
            actionButton = UIStyles.CreatePrimaryButton("");
            actionButton.name = "transition-action-button";
            actionButton.style.width = 320;
            actionButton.style.maxWidth = Length.Percent(100);
            actionButton.style.height = 56;
            actionButton.style.fontSize = UIStyles.FontLG;
            actionButton.clicked += OnButtonClicked;
            panel.Add(actionButton);

            backdrop.Add(panel);
            root.Add(backdrop);

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        public void ShowStartPanel(int totalScenarios)
        {
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            titleLabel.text = GetLocalizedText("start_title", lang);
            subtitleLabel.text = GetLocalizedText("start_subtitle", lang);
            actionButton.text = GetLocalizedText("start_button", lang);

            ShowInternal();
        }

        public void ShowTransitionPanel(int completedIndex, int totalScenarios)
        {
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            string titleTemplate = GetLocalizedText("transition_title", lang);
            titleLabel.text = string.Format(titleTemplate, completedIndex + 1, totalScenarios);

            subtitleLabel.text = GetLocalizedText("transition_subtitle", lang);
            actionButton.text = GetLocalizedText("transition_button", lang);

            ShowInternal();
        }

        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;

            SetPlayerControlsEnabled(true);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAndHide());
        }

        void ShowInternal()
        {
            isVisible = true;

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
            PlayerControls.SetEnabled(enabled);
        }

        void OnLanguageChanged(string newLanguage)
        {
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
