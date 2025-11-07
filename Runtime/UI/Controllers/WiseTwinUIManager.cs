using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Main UI Manager for WiseTwin training system using UI Toolkit
    /// Compatible with Unity 6000+
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WiseTwinUIManager : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private StyleSheet customStyleSheet;
        [SerializeField] private PanelSettings panelSettings;
        
        [Header("UI Settings")]
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color accentColor = new Color(0.1f, 0.8f, 0.6f);
        [SerializeField] private float animationDuration = 0.3f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // UI Document reference
        private UIDocument uiDocument;
        
        // UI Elements
        private VisualElement root;
        private VisualElement hudContainer;
        private VisualElement questionModal;
        private VisualElement notificationContainer;
        private VisualElement progressPanel;
        
        // Progress tracking
        private Label titleLabel;
        private Label progressLabel;
        private VisualElement progressBar;
        private Label timerLabel;
        private Label debugLabel;
        
        // Question UI
        private Label questionText;
        private VisualElement optionsContainer;
        private Button submitButton;
        
        // Notification queue
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private bool isShowingNotification = false;
        
        // Singleton
        public static WiseTwinUIManager Instance { get; private set; }
        
        // Events
        public System.Action<int> OnAnswerSelected;
        public System.Action OnQuestionSubmitted;
        
        private float trainingStartTime;
        private int currentProgress = 0;
        private int totalSteps = 100;
        private int selectedAnswerIndex = -1;
        
        void Awake()
        {
            Debug.Log("[WiseTwinUIManager] Awake called");
            
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Get or add UIDocument component
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    Debug.Log("[WiseTwinUIManager] Adding UIDocument component");
                    uiDocument = gameObject.AddComponent<UIDocument>();
                }
                
                // Apply panel settings if provided
                if (panelSettings != null && uiDocument != null)
                {
                    Debug.Log("[WiseTwinUIManager] Applying panel settings");
                    uiDocument.panelSettings = panelSettings;
                }
                else if (panelSettings == null)
                {
                    Debug.LogWarning("[WiseTwinUIManager] No Panel Settings assigned! UI might not display correctly.");
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            Debug.Log("[WiseTwinUIManager] Start called - Initializing UI");
            StartCoroutine(InitializeUIDelayed());
        }
        
        IEnumerator InitializeUIDelayed()
        {
            // Wait one frame to ensure everything is ready
            yield return null;
            
            InitializeUI();
            
            if (showDebugInfo)
            {
                Debug.Log("[WiseTwinUIManager] UI Initialization complete");
                Debug.Log($"[WiseTwinUIManager] Root element children count: {root?.childCount ?? 0}");
            }
        }
        
        void InitializeUI()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[WiseTwinUIManager] UIDocument is null!");
                return;
            }
            
            // Get or create root
            root = uiDocument.rootVisualElement;
            
            if (root == null)
            {
                Debug.LogError("[WiseTwinUIManager] Root visual element is null!");
                return;
            }
            
            Debug.Log($"[WiseTwinUIManager] Root element exists. Current children: {root.childCount}");

            // Don't clear if there's already content (like LanguageSelectionUI)
            if (root.childCount == 0)
            {
                SetupUIStructure();
            }
            else
            {
                Debug.Log("[WiseTwinUIManager] Root already has content, skipping UI structure setup");
                // Just get references to existing elements if needed
                questionModal = root.Q<VisualElement>("question-modal");
                notificationContainer = root.Q<VisualElement>("notification-container");
                progressPanel = root.Q<VisualElement>("progress-panel");
                hudContainer = root.Q<VisualElement>("hud-container");
            }
            
            // Apply custom stylesheet if provided
            if (customStyleSheet != null)
            {
                root.styleSheets.Add(customStyleSheet);
                Debug.Log("[WiseTwinUIManager] Custom stylesheet applied");
            }
            
            // Add debug info if enabled
            if (showDebugInfo)
            {
                CreateDebugPanel();
            }
        }
        
        void SetupUIStructure()
        {
            Debug.Log("[WiseTwinUIManager] Setting up UI structure");
            
            // Setup root container
            root.style.position = Position.Absolute;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.flexDirection = FlexDirection.Column;

            // IMPORTANT: Par défaut, ne pas bloquer les clics sur la scène 3D
            // Les éléments interactifs spécifiques activeront leur picking au besoin
            root.pickingMode = PickingMode.Ignore;
            
            // Add a semi-transparent background for testing
            if (showDebugInfo)
            {
                root.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            }
            
            // Create main containers
            CreateHUD();
            CreateQuestionModal();
            CreateNotificationContainer();
            CreateProgressPanel();

            Debug.Log($"[WiseTwinUIManager] UI structure created. Total elements: {root.childCount}");
        }
        
        void CreateDebugPanel()
        {
            var debugPanel = new VisualElement();
            debugPanel.style.position = Position.Absolute;
            debugPanel.style.bottom = 120;
            debugPanel.style.left = 20;
            debugPanel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            debugPanel.style.paddingLeft = 10;
            debugPanel.style.paddingRight = 10;
            debugPanel.style.paddingTop = 10;
            debugPanel.style.paddingBottom = 10;
            debugPanel.style.borderTopLeftRadius = 8;
            debugPanel.style.borderTopRightRadius = 8;
            debugPanel.style.borderBottomLeftRadius = 8;
            debugPanel.style.borderBottomRightRadius = 8;
            
            debugLabel = new Label("UI System Active");
            debugLabel.style.color = Color.green;
            debugLabel.style.fontSize = 12;
            debugPanel.Add(debugLabel);
            
            root.Add(debugPanel);
        }
        
        void CreateHUD()
        {
            Debug.Log("[WiseTwinUIManager] Creating HUD");
            
            hudContainer = new VisualElement();
            hudContainer.name = "hud-container";
            hudContainer.style.position = Position.Absolute;
            hudContainer.style.top = 20;
            hudContainer.style.left = 20;
            hudContainer.style.right = 20;
            hudContainer.style.height = 60;
            hudContainer.style.flexDirection = FlexDirection.Row;
            hudContainer.style.justifyContent = Justify.SpaceBetween;
            hudContainer.pickingMode = PickingMode.Ignore; // Ne pas bloquer les clics 3D
            
            // Left section - Title and Info
            var leftSection = new VisualElement();
            leftSection.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            leftSection.style.paddingLeft = 20;
            leftSection.style.paddingRight = 20;
            leftSection.style.paddingTop = 15;
            leftSection.style.paddingBottom = 15;
            leftSection.style.borderTopLeftRadius = 10;
            leftSection.style.borderTopRightRadius = 10;
            leftSection.style.borderBottomLeftRadius = 10;
            leftSection.style.borderBottomRightRadius = 10;
            
            titleLabel = new Label("WiseTwin Training");
            titleLabel.style.fontSize = 24;
            titleLabel.style.color = Color.white;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftSection.Add(titleLabel);
            
            // Right section - Timer
            var rightSection = new VisualElement();
            rightSection.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            rightSection.style.paddingLeft = 20;
            rightSection.style.paddingRight = 20;
            rightSection.style.paddingTop = 15;
            rightSection.style.paddingBottom = 15;
            rightSection.style.borderTopLeftRadius = 10;
            rightSection.style.borderTopRightRadius = 10;
            rightSection.style.borderBottomLeftRadius = 10;
            rightSection.style.borderBottomRightRadius = 10;
            
            timerLabel = new Label("00:00");
            timerLabel.style.fontSize = 20;
            timerLabel.style.color = accentColor;
            timerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightSection.Add(timerLabel);
            
            hudContainer.Add(leftSection);
            hudContainer.Add(rightSection);
            root.Add(hudContainer);
            
            Debug.Log("[WiseTwinUIManager] HUD created");
        }
        
        void CreateQuestionModal()
        {
            Debug.Log("[WiseTwinUIManager] Creating Question Modal");
            
            // Modal background
            questionModal = new VisualElement();
            questionModal.name = "question-modal";
            questionModal.style.position = Position.Absolute;
            questionModal.style.top = 0;
            questionModal.style.left = 0;
            questionModal.style.right = 0;
            questionModal.style.bottom = 0;
            questionModal.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            questionModal.style.display = DisplayStyle.None; // Hidden by default
            questionModal.style.alignItems = Align.Center;
            questionModal.style.justifyContent = Justify.Center;

            // Le modal doit capturer les clics quand visible
            questionModal.pickingMode = PickingMode.Position;
            
            // Modal content
            var modalContent = new VisualElement();
            modalContent.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            modalContent.style.width = 600;
            modalContent.style.maxWidth = Length.Percent(90);
            modalContent.style.maxHeight = Length.Percent(80);
            modalContent.style.paddingLeft = 30;
            modalContent.style.paddingRight = 30;
            modalContent.style.paddingTop = 30;
            modalContent.style.paddingBottom = 30;
            modalContent.style.borderTopLeftRadius = 20;
            modalContent.style.borderTopRightRadius = 20;
            modalContent.style.borderBottomLeftRadius = 20;
            modalContent.style.borderBottomRightRadius = 20;
            modalContent.style.borderLeftWidth = 3;
            modalContent.style.borderRightWidth = 3;
            modalContent.style.borderTopWidth = 3;
            modalContent.style.borderBottomWidth = 3;
            modalContent.style.borderLeftColor = primaryColor;
            modalContent.style.borderRightColor = primaryColor;
            modalContent.style.borderTopColor = primaryColor;
            modalContent.style.borderBottomColor = primaryColor;
            
            // Question text
            questionText = new Label("Question will appear here");
            questionText.style.fontSize = 22;
            questionText.style.color = Color.white;
            questionText.style.marginBottom = 25;
            questionText.style.whiteSpace = WhiteSpace.Normal;
            modalContent.Add(questionText);
            
            // Options container
            optionsContainer = new VisualElement();
            optionsContainer.style.marginBottom = 20;
            modalContent.Add(optionsContainer);
            
            // Submit button
            submitButton = new Button(() => SubmitAnswer());
            submitButton.text = "Submit Answer";
            submitButton.style.marginTop = 20;
            submitButton.style.height = 50;
            submitButton.style.fontSize = 18;
            submitButton.style.backgroundColor = primaryColor;
            submitButton.style.color = Color.white;
            submitButton.style.borderTopLeftRadius = 10;
            submitButton.style.borderTopRightRadius = 10;
            submitButton.style.borderBottomLeftRadius = 10;
            submitButton.style.borderBottomRightRadius = 10;
            submitButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Add hover effect to submit button
            submitButton.RegisterCallback<MouseEnterEvent>((evt) => {
                submitButton.style.backgroundColor = primaryColor * 1.2f;
            });
            submitButton.RegisterCallback<MouseLeaveEvent>((evt) => {
                submitButton.style.backgroundColor = primaryColor;
            });
            
            modalContent.Add(submitButton);
            
            // Close button
            var closeButton = new Button(() => HideQuestion());
            closeButton.text = "X";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 10;
            closeButton.style.right = 10;
            closeButton.style.width = 30;
            closeButton.style.height = 30;
            closeButton.style.fontSize = 20;
            closeButton.style.backgroundColor = Color.clear;
            closeButton.style.color = Color.white;
            modalContent.Add(closeButton);
            
            questionModal.Add(modalContent);
            root.Add(questionModal);
            
            Debug.Log("[WiseTwinUIManager] Question Modal created");
        }
        
        void CreateNotificationContainer()
        {
            Debug.Log("[WiseTwinUIManager] Creating Notification Container");
            
            notificationContainer = new VisualElement();
            notificationContainer.name = "notification-container";
            notificationContainer.style.position = Position.Absolute;
            notificationContainer.style.top = 100;
            notificationContainer.style.right = 20;
            notificationContainer.style.width = 350;
            notificationContainer.pickingMode = PickingMode.Ignore; // Ne pas bloquer les clics 3D
            root.Add(notificationContainer);
        }
        
        void CreateProgressPanel()
        {
            Debug.Log("[WiseTwinUIManager] Creating Progress Panel");
            
            progressPanel = new VisualElement();
            progressPanel.name = "progress-panel";
            progressPanel.style.position = Position.Absolute;
            progressPanel.style.bottom = 20;
            progressPanel.style.left = 20;
            progressPanel.style.right = 20;
            progressPanel.style.height = 80;
            progressPanel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            progressPanel.style.paddingLeft = 20;
            progressPanel.style.paddingRight = 20;
            progressPanel.style.paddingTop = 15;
            progressPanel.style.paddingBottom = 15;
            progressPanel.style.borderTopLeftRadius = 15;
            progressPanel.style.borderTopRightRadius = 15;
            progressPanel.style.borderBottomLeftRadius = 15;
            progressPanel.style.borderBottomRightRadius = 15;
            
            // Progress label
            progressLabel = new Label("Progress: 0%");
            progressLabel.style.color = Color.white;
            progressLabel.style.fontSize = 16;
            progressLabel.style.marginBottom = 10;
            progressPanel.Add(progressLabel);
            
            // Progress bar container
            var progressBarContainer = new VisualElement();
            progressBarContainer.style.height = 20;
            progressBarContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            progressBarContainer.style.borderTopLeftRadius = 10;
            progressBarContainer.style.borderTopRightRadius = 10;
            progressBarContainer.style.borderBottomLeftRadius = 10;
            progressBarContainer.style.borderBottomRightRadius = 10;
            
            // Progress bar fill
            progressBar = new VisualElement();
            progressBar.style.height = Length.Percent(100);
            progressBar.style.width = Length.Percent(0);
            progressBar.style.backgroundColor = accentColor;
            progressBar.style.borderTopLeftRadius = 10;
            progressBar.style.borderTopRightRadius = 10;
            progressBar.style.borderBottomLeftRadius = 10;
            progressBar.style.borderBottomRightRadius = 10;
            
            progressBarContainer.Add(progressBar);
            progressPanel.Add(progressBarContainer);
            
            root.Add(progressPanel);
            
            Debug.Log("[WiseTwinUIManager] Progress Panel created");
        }
        
        #region Public API
        
        public void SetTrainingTitle(string title)
        {
            if (titleLabel != null)
            {
                titleLabel.text = title;
                Debug.Log($"[WiseTwinUIManager] Title set to: {title}");
            }
        }
        
        /// <summary>
        /// Récupère l'index de la réponse sélectionnée
        /// </summary>
        public int GetSelectedAnswerIndex()
        {
            return selectedAnswerIndex;
        }

        public void ShowQuestion(string questionTextStr, string[] options, QuestionType type = QuestionType.MultipleChoice)
        {
            Debug.Log($"[WiseTwinUIManager] Showing question: {questionTextStr}");

            if (questionText != null)
            {
                questionText.text = questionTextStr;
            }

            // Clear previous options
            optionsContainer?.Clear();
            selectedAnswerIndex = -1;
            
            if (type == QuestionType.MultipleChoice)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int index = i;
                    var option = CreateOptionButton(options[i], index);
                    option.clicked += () => {
                        selectedAnswerIndex = index;
                        HighlightSelectedOption(index);
                    };
                    optionsContainer.Add(option);
                }
            }
            else if (type == QuestionType.TrueFalse)
            {
                var trueButton = CreateOptionButton("True", 0);
                trueButton.clicked += () => {
                    selectedAnswerIndex = 0;
                    HighlightSelectedOption(0);
                };
                optionsContainer.Add(trueButton);
                
                var falseButton = CreateOptionButton("False", 1);
                falseButton.clicked += () => {
                    selectedAnswerIndex = 1;
                    HighlightSelectedOption(1);
                };
                optionsContainer.Add(falseButton);
            }
            
            // Show modal with animation
            ShowModal(questionModal);
        }
        
        Button CreateOptionButton(string text, int index)
        {
            var button = new Button();
            button.text = text;
            button.name = $"option-{index}";
            button.style.marginBottom = 10;
            button.style.height = 45;
            button.style.fontSize = 16;
            button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            button.style.color = Color.white;
            button.style.borderTopLeftRadius = 8;
            button.style.borderTopRightRadius = 8;
            button.style.borderBottomLeftRadius = 8;
            button.style.borderBottomRightRadius = 8;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Hover effect
            button.RegisterCallback<MouseEnterEvent>((evt) => {
                if (!button.ClassListContains("selected"))
                {
                    button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                }
            });
            
            button.RegisterCallback<MouseLeaveEvent>((evt) => {
                if (!button.ClassListContains("selected"))
                {
                    button.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                }
            });
            
            return button;
        }
        
        void HighlightSelectedOption(int index)
        {
            // Clear all selections
            foreach (var child in optionsContainer.Children())
            {
                if (child is Button btn)
                {
                    btn.RemoveFromClassList("selected");
                    btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                    btn.style.borderLeftColor = Color.clear;
                    btn.style.borderRightColor = Color.clear;
                    btn.style.borderTopColor = Color.clear;
                    btn.style.borderBottomColor = Color.clear;
                    btn.style.borderLeftWidth = 0;
                    btn.style.borderRightWidth = 0;
                    btn.style.borderTopWidth = 0;
                    btn.style.borderBottomWidth = 0;
                }
            }
            
            // Highlight selected
            var selected = optionsContainer.Q<Button>($"option-{index}");
            if (selected != null)
            {
                selected.AddToClassList("selected");
                selected.style.backgroundColor = primaryColor * 0.8f;
                selected.style.borderLeftColor = accentColor;
                selected.style.borderRightColor = accentColor;
                selected.style.borderTopColor = accentColor;
                selected.style.borderBottomColor = accentColor;
                selected.style.borderLeftWidth = 2;
                selected.style.borderRightWidth = 2;
                selected.style.borderTopWidth = 2;
                selected.style.borderBottomWidth = 2;
            }
            
            OnAnswerSelected?.Invoke(index);
            Debug.Log($"[WiseTwinUIManager] Option {index} selected");
        }
        
        void SubmitAnswer()
        {
            if (selectedAnswerIndex < 0)
            {
                ShowNotification("Please select an answer first!", NotificationType.Warning);
                return;
            }
            
            OnQuestionSubmitted?.Invoke();
            HideModal(questionModal);
            Debug.Log("[WiseTwinUIManager] Answer submitted");
        }
        
        public void HideQuestion()
        {
            HideModal(questionModal);
        }
        
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            Debug.Log($"[WiseTwinUIManager] Showing notification: {message}");
            
            var notification = new NotificationData
            {
                message = message,
                type = type,
                duration = duration
            };
            
            notificationQueue.Enqueue(notification);
            
            if (!isShowingNotification)
            {
                StartCoroutine(ProcessNotificationQueue());
            }
        }
        
        IEnumerator ProcessNotificationQueue()
        {
            isShowingNotification = true;
            
            while (notificationQueue.Count > 0)
            {
                var notif = notificationQueue.Dequeue();
                yield return ShowNotificationCoroutine(notif);
            }
            
            isShowingNotification = false;
        }
        
        IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            // Check if notification container exists
            if (notificationContainer == null)
            {
                Debug.LogWarning("[WiseTwinUIManager] Notification container not ready, skipping notification");
                yield break;
            }
            
            // Create notification element
            var notification = new VisualElement();
            notification.style.backgroundColor = GetNotificationColor(data.type);
            notification.style.paddingLeft = 15;
            notification.style.paddingRight = 15;
            notification.style.paddingTop = 12;
            notification.style.paddingBottom = 12;
            notification.style.marginBottom = 10;
            notification.style.borderTopLeftRadius = 8;
            notification.style.borderTopRightRadius = 8;
            notification.style.borderBottomLeftRadius = 8;
            notification.style.borderBottomRightRadius = 8;
            notification.style.opacity = 0;
            
            var label = new Label(data.message);
            label.style.color = Color.white;
            label.style.fontSize = 14;
            notification.Add(label);
            
            notificationContainer.Add(notification);
            
            // Fade in
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                notification.style.opacity = Mathf.Lerp(0, 1, elapsed / animationDuration);
                yield return null;
            }
            notification.style.opacity = 1;
            
            // Wait
            yield return new WaitForSeconds(data.duration);
            
            // Fade out
            elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                notification.style.opacity = Mathf.Lerp(1, 0, elapsed / animationDuration);
                yield return null;
            }
            
            notificationContainer.Remove(notification);
        }
        
        Color GetNotificationColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    return new Color(0.1f, 0.6f, 0.3f, 0.9f);
                case NotificationType.Error:
                    return new Color(0.8f, 0.2f, 0.2f, 0.9f);
                case NotificationType.Warning:
                    return new Color(0.8f, 0.6f, 0.1f, 0.9f);
                default:
                    return new Color(0.2f, 0.4f, 0.8f, 0.9f);
            }
        }
        
        public void UpdateProgress(int current, int total)
        {
            currentProgress = current;
            totalSteps = total;
            float percentage = total > 0 ? (float)current / total * 100f : 0f;
            
            if (progressLabel != null)
            {
                progressLabel.text = $"Progress: {percentage:F0}%";
            }
            
            if (progressBar != null)
            {
                progressBar.style.width = Length.Percent(percentage);
            }
            
            Debug.Log($"[WiseTwinUIManager] Progress updated: {current}/{total} ({percentage:F0}%)");
        }
        
        public void StartTraining()
        {
            trainingStartTime = Time.time;
            UpdateProgress(0, totalSteps);
            ShowNotification("Training Started!", NotificationType.Success);
            Debug.Log("[WiseTwinUIManager] Training started");
        }
        
        public void CompleteTraining()
        {
            UpdateProgress(totalSteps, totalSteps);
            ShowNotification("Training Completed! 🎉", NotificationType.Success, 5f);
            
            // Trigger completion in WiseTwinManager
            if (WiseTwin.WiseTwinManager.Instance != null)
            {
                WiseTwin.WiseTwinManager.Instance.CompleteTraining();
            }
            
            Debug.Log("[WiseTwinUIManager] Training completed");
        }
        
        void ShowModal(VisualElement modal)
        {
            if (modal == null) return;
            
            modal.style.display = DisplayStyle.Flex;
            modal.style.opacity = 0;
            StartCoroutine(FadeIn(modal));
        }
        
        void HideModal(VisualElement modal)
        {
            if (modal == null) return;
            
            StartCoroutine(FadeOutAndHide(modal));
        }
        
        IEnumerator FadeIn(VisualElement element)
        {
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(0, 1, elapsed / animationDuration);
                yield return null;
            }
            element.style.opacity = 1;
        }
        
        IEnumerator FadeOutAndHide(VisualElement element)
        {
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                element.style.opacity = Mathf.Lerp(1, 0, elapsed / animationDuration);
                yield return null;
            }
            element.style.display = DisplayStyle.None;
        }
        
        #endregion
        
        void Update()
        {
            // Update timer
            if (timerLabel != null && trainingStartTime > 0)
            {
                float elapsed = Time.time - trainingStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60);
                int seconds = Mathf.FloorToInt(elapsed % 60);
                timerLabel.text = $"{minutes:00}:{seconds:00}";
            }
            
            // Update debug info
            if (showDebugInfo && debugLabel != null)
            {
                debugLabel.text = $"UI Active | Progress: {currentProgress}/{totalSteps} | Queue: {notificationQueue.Count}";
            }
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        // Test method for debugging
        [ContextMenu("Test Show Question")]
        public void TestShowQuestion()
        {
            ShowQuestion(
                "This is a test question?", 
                new string[] { "Option A", "Option B", "Option C", "Option D" },
                QuestionType.MultipleChoice
            );
        }
        
        [ContextMenu("Test Show Notification")]
        public void TestShowNotification()
        {
            ShowNotification("Test notification!", NotificationType.Info);
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    [System.Serializable]
    public class NotificationData
    {
        public string message;
        public NotificationType type;
        public float duration;
    }
}