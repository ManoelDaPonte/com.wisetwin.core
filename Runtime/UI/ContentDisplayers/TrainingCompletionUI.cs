using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

namespace WiseTwin.UI
{
    /// <summary>
    /// Interface de complétion de formation affichée quand l'utilisateur termine tous les modules
    /// </summary>
    public class TrainingCompletionUI : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private bool showConfetti = true;
        [SerializeField] private float displayDuration = 5f; // Durée avant fermeture automatique

        private UIDocument uiDocument;
        private VisualElement rootElement;
        private VisualElement modalContainer;

        // Stats pour afficher
        private float totalTime = 0f;
        private int totalInteractions = 0;

        // Singleton
        public static TrainingCompletionUI Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupUIDocument();
        }

        void SetupUIDocument()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            // Assigner le PanelSettings si nécessaire
            if (uiDocument.panelSettings == null)
            {
                #if UNITY_EDITOR
                var panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/WiseTwinPanelSettings.asset");
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                }
                #endif
            }

            rootElement = uiDocument.rootVisualElement;

            // Configuration de base
            rootElement.style.position = Position.Absolute;
            rootElement.style.width = Length.Percent(100);
            rootElement.style.height = Length.Percent(100);
            rootElement.pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Affiche l'écran de complétion de formation
        /// </summary>
        public void ShowCompletionScreen(float trainingTime, int modulesCompleted)
        {
            totalTime = trainingTime;
            totalInteractions = modulesCompleted;

            CreateCompletionUI();

            // Notifier que la formation est terminée
            NotifyTrainingCompletion();

            // Auto-close après un délai
            StartCoroutine(AutoCloseAfterDelay());
        }

        void CreateCompletionUI()
        {
            // Clear root
            rootElement.Clear();
            rootElement.pickingMode = PickingMode.Position;

            // Container modal avec animation d'entrée
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.9f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de succès principale
            var successBox = new VisualElement();
            successBox.style.width = 600;
            successBox.style.maxWidth = Length.Percent(90);
            successBox.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            successBox.style.borderTopLeftRadius = 30;
            successBox.style.borderTopRightRadius = 30;
            successBox.style.borderBottomLeftRadius = 30;
            successBox.style.borderBottomRightRadius = 30;
            successBox.style.paddingTop = 50;
            successBox.style.paddingBottom = 50;
            successBox.style.paddingLeft = 50;
            successBox.style.paddingRight = 50;

            // Bordure lumineuse de succès
            successBox.style.borderTopWidth = 3;
            successBox.style.borderBottomWidth = 3;
            successBox.style.borderLeftWidth = 3;
            successBox.style.borderRightWidth = 3;
            successBox.style.borderTopColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderBottomColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderLeftColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);
            successBox.style.borderRightColor = new Color(0.1f, 0.9f, 0.5f, 0.8f);

            // Trophée ou icône de succès
            var successIcon = new Label("🏆");
            successIcon.style.fontSize = 72;
            successIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            successIcon.style.marginBottom = 20;
            successBox.Add(successIcon);

            // Message de félicitations
            var congratsTitle = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Félicitations !"
                : "Congratulations!");
            congratsTitle.style.fontSize = 42;
            congratsTitle.style.color = new Color(0.1f, 0.9f, 0.5f, 1f);
            congratsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            congratsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            congratsTitle.style.marginBottom = 15;
            successBox.Add(congratsTitle);

            // Message de succès
            var successMessage = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Formation terminée avec succès !"
                : "Training completed successfully!");
            successMessage.style.fontSize = 24;
            successMessage.style.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            successMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
            successMessage.style.marginBottom = 30;
            successBox.Add(successMessage);

            // Ligne de séparation
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            separator.style.marginTop = 20;
            separator.style.marginBottom = 20;
            successBox.Add(separator);

            // Statistiques de la session
            var statsContainer = new VisualElement();
            statsContainer.style.marginBottom = 30;

            // Temps total
            var timeLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"⏱ Temps total : {FormatTime(totalTime)}"
                : $"⏱ Total time: {FormatTime(totalTime)}");
            timeLabel.style.fontSize = 18;
            timeLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            timeLabel.style.marginBottom = 10;
            statsContainer.Add(timeLabel);

            // Modules complétés
            var modulesLabel = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? $"✅ Modules complétés : {totalInteractions}"
                : $"✅ Modules completed: {totalInteractions}");
            modulesLabel.style.fontSize = 18;
            modulesLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            modulesLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statsContainer.Add(modulesLabel);

            successBox.Add(statsContainer);

            // Score ou performance
            var scoreContainer = new VisualElement();
            scoreContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
            scoreContainer.style.borderTopLeftRadius = 15;
            scoreContainer.style.borderTopRightRadius = 15;
            scoreContainer.style.borderBottomLeftRadius = 15;
            scoreContainer.style.borderBottomRightRadius = 15;
            scoreContainer.style.paddingTop = 20;
            scoreContainer.style.paddingBottom = 20;
            scoreContainer.style.marginBottom = 30;

            var scoreLabel = new Label("100%");
            scoreLabel.style.fontSize = 48;
            scoreLabel.style.color = new Color(0.1f, 0.9f, 0.5f, 1f);
            scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            scoreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            scoreContainer.Add(scoreLabel);

            var scoreText = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Score parfait !"
                : "Perfect score!");
            scoreText.style.fontSize = 16;
            scoreText.style.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            scoreText.style.unityTextAlign = TextAnchor.MiddleCenter;
            scoreContainer.Add(scoreText);

            successBox.Add(scoreContainer);

            // Bouton de fermeture
            var closeButton = new Button(() => Close());
            closeButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Terminer"
                : "Finish";
            closeButton.style.width = 200;
            closeButton.style.height = 50;
            closeButton.style.fontSize = 18;
            closeButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.5f, 1f);
            closeButton.style.color = Color.white;
            closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeButton.style.borderTopLeftRadius = 25;
            closeButton.style.borderTopRightRadius = 25;
            closeButton.style.borderBottomLeftRadius = 25;
            closeButton.style.borderBottomRightRadius = 25;
            closeButton.style.alignSelf = Align.Center;

            // Hover effect
            closeButton.RegisterCallback<MouseEnterEvent>((evt) => {
                closeButton.style.backgroundColor = new Color(0.15f, 0.9f, 0.6f, 1f);
                closeButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
            });

            closeButton.RegisterCallback<MouseLeaveEvent>((evt) => {
                closeButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.5f, 1f);
                closeButton.style.scale = new Scale(Vector3.one);
            });

            successBox.Add(closeButton);

            // Message de redirection
            var redirectMessage = new Label(LocalizationManager.Instance?.CurrentLanguage == "fr"
                ? "Cette fenêtre se fermera automatiquement..."
                : "This window will close automatically...");
            redirectMessage.style.fontSize = 12;
            redirectMessage.style.color = new Color(0.5f, 0.5f, 0.55f, 0.8f);
            redirectMessage.style.unityFontStyleAndWeight = FontStyle.Italic;
            redirectMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
            redirectMessage.style.marginTop = 20;
            successBox.Add(redirectMessage);

            modalContainer.Add(successBox);
            rootElement.Add(modalContainer);

            // Animation d'entrée (scale)
            successBox.style.scale = new Scale(new Vector3(0.8f, 0.8f, 1f));
            successBox.RegisterCallback<GeometryChangedEvent>((evt) => {
                successBox.style.scale = new Scale(Vector3.one);
            });
        }

        void NotifyTrainingCompletion()
        {
            // Utiliser le TrainingCompletionNotifier
            var completionNotifier = FindObjectOfType<TrainingCompletionNotifier>();
            if (completionNotifier != null)
            {
                completionNotifier.FormationCompleted(Application.productName);
                Debug.Log("[TrainingCompletionUI] Training completion notified to web application");
            }
            else
            {
                // Si pas trouvé, en créer un temporairement
                var tempNotifier = new GameObject("TempCompletionNotifier").AddComponent<TrainingCompletionNotifier>();
                tempNotifier.FormationCompleted(Application.productName);
                Destroy(tempNotifier.gameObject, 1f);
                Debug.Log("[TrainingCompletionUI] Created temporary notifier for completion");
            }
        }

        IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            Close();
        }

        void Close()
        {
            rootElement?.Clear();
            rootElement.pickingMode = PickingMode.Ignore;
        }

        string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return $"{minutes:00}:{secs:00}";
        }
    }
}