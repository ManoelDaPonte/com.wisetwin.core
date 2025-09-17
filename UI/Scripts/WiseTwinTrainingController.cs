using UnityEngine;
using System.Collections.Generic;
using WiseTwin;
using WiseTwin.UI;

/// <summary>
/// Example training controller showing how to use WiseTwinUIManager with your existing WiseTwin system
/// </summary>
public class WiseTwinTrainingController : MonoBehaviour
{
    [Header("Training Configuration")]
    [SerializeField] private string trainingTitle = "Autoclave Safety Training";
    [SerializeField] private GameObject[] interactiveObjects;
    
    private WiseTwinUIManager uiManager;
    private int currentQuestionIndex = 0;
    private List<QuestionData> questions = new List<QuestionData>();
    private int correctAnswers = 0;
    
    // Example question data structure
    [System.Serializable]
    public class QuestionData
    {
        public string objectId;
        public string questionText;
        public string[] options;
        public int correctAnswer;
        public string feedback;
        public string incorrectFeedback;
    }
    
    void Start()
    {
        // Initialize UI Manager
        uiManager = WiseTwinUIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogError("[WiseTwinTrainingController] UI Manager not found! Please add WiseTwinUIManager to the scene.");
            return;
        }
        
        // Set training title
        uiManager.SetTrainingTitle(trainingTitle);
        
        // Subscribe to UI events
        uiManager.OnAnswerSelected += OnAnswerSelected;
        uiManager.OnQuestionSubmitted += OnQuestionSubmitted;
        
        // Load questions from metadata when ready
        if (WiseTwinManager.Instance != null)
        {
            WiseTwinManager.Instance.OnMetadataReady += OnMetadataLoaded;
            
            // If metadata already loaded
            if (WiseTwinManager.Instance.IsMetadataLoaded)
            {
                OnMetadataLoaded(null);
            }
        }
        
        // Start the training
        StartTraining();
    }
    
    void OnMetadataLoaded(Dictionary<string, object> metadata)
    {
        Debug.Log("[WiseTwinTrainingController] Metadata loaded, extracting questions...");
        
        // Get language preference
        string language = WiseTwinManager.Instance.GetPreferredLanguage();
        
        // Example: Load questions from your QCM document
        // In a real scenario, you would parse your metadata here
        LoadExampleQuestions(language);
        
        // Update UI with training info
        string title = WiseTwinManager.Instance.GetProjectInfo("title");
        if (!string.IsNullOrEmpty(title))
        {
            uiManager.SetTrainingTitle(title);
        }
        
        uiManager.ShowNotification("Training data loaded successfully!", NotificationType.Success);
    }
    
    void LoadExampleQuestions(string language)
    {
        // Example questions based on your QCM document
        questions.Clear();
        
        // Question 1
        questions.Add(new QuestionData
        {
            objectId = "autoclave_control",
            questionText = language == "fr" ? 
                "Qui est autorisé à utiliser l'autoclave ?" : 
                "Who is authorized to use the autoclave?",
            options = language == "fr" ? 
                new[] { 
                    "Toute personne formée en production",
                    "Toute personne habilitée à l'utilisation d'appareils sous pression",
                    "Le chef d'équipe uniquement",
                    "Un opérateur expérimenté, même sans habilitation"
                } :
                new[] {
                    "Anyone trained in production",
                    "Anyone authorized to use pressure equipment",
                    "Team leader only",
                    "An experienced operator, even without authorization"
                },
            correctAnswer = 1,
            feedback = language == "fr" ? 
                "Correct ! L'habilitation est obligatoire." : 
                "Correct! Authorization is mandatory.",
            incorrectFeedback = language == "fr" ?
                "Incorrect. L'habilitation appareil sous pression est obligatoire." :
                "Incorrect. Pressure equipment authorization is mandatory."
        });
        
        // Question 2
        questions.Add(new QuestionData
        {
            objectId = "autoclave_safety",
            questionText = language == "fr" ?
                "Avant d'ouvrir l'autoclave, que doit-on impérativement appliquer ?" :
                "Before opening the autoclave, what must be applied?",
            options = language == "fr" ?
                new[] {
                    "Le LOTO (consignation énergie)",
                    "Une simple coupure électrique",
                    "Le port des gants thermiques uniquement",
                    "Rien, si le cycle est terminé"
                } :
                new[] {
                    "LOTO (energy lockout)",
                    "A simple electrical shutdown",
                    "Thermal gloves only",
                    "Nothing, if the cycle is complete"
                },
            correctAnswer = 0,
            feedback = language == "fr" ?
                "Excellent ! Le LOTO est essentiel pour la sécurité." :
                "Excellent! LOTO is essential for safety.",
            incorrectFeedback = language == "fr" ?
                "Attention ! Le LOTO est obligatoire avant toute intervention." :
                "Warning! LOTO is mandatory before any intervention."
        });
        
        // Add more questions as needed...
    }
    
    void StartTraining()
    {
        // Check if questions are loaded before starting
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[WiseTwinTrainingController] No questions loaded, loading defaults...");
            LoadExampleQuestions("fr"); // Load default questions in French
        }
        
        uiManager.StartTraining();
        uiManager.UpdateProgress(0, questions.Count);
        
        // Show welcome notification
        uiManager.ShowNotification("Welcome to the training! Let's begin.", NotificationType.Info, 4f);
        
        // Start with first question after a delay
        Invoke(nameof(ShowNextQuestion), 2f);
    }
    
    void ShowNextQuestion()
    {
        // Safety check
        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("[WiseTwinTrainingController] No questions available!");
            uiManager.ShowNotification("No questions available!", NotificationType.Error);
            return;
        }
        
        if (currentQuestionIndex < questions.Count)
        {
            var question = questions[currentQuestionIndex];
            
            // Update progress
            uiManager.UpdateProgress(currentQuestionIndex, questions.Count);
            
            // Highlight interactive object if available
            HighlightObject(question.objectId);
            
            // Show the question
            uiManager.ShowQuestion(
                question.questionText, 
                question.options, 
                QuestionType.MultipleChoice
            );
        }
        else
        {
            // Training completed
            CompleteTraining();
        }
    }
    
    void HighlightObject(string objectId)
    {
        // Find and highlight the corresponding 3D object
        foreach (var obj in interactiveObjects)
        {
            if (obj.name == objectId)
            {
                // Add highlight effect (example with outline or emission)
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Store original material
                    StartCoroutine(HighlightCoroutine(renderer));
                }
                
                // Optional: Move camera to focus on object
                // Camera.main.transform.LookAt(obj.transform);
            }
        }
    }
    
    System.Collections.IEnumerator HighlightCoroutine(Renderer renderer)
    {
        var originalColor = renderer.material.color;
        var highlightColor = new Color(1f, 1f, 0.5f, 1f); // Yellow highlight
        
        // Pulse effect
        float duration = 0.5f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 2, 1);
            renderer.material.color = Color.Lerp(originalColor, highlightColor, t);
            yield return null;
        }
        
        renderer.material.color = originalColor;
    }
    
    private int selectedAnswerIndex = -1;
    
    void OnAnswerSelected(int index)
    {
        selectedAnswerIndex = index;
        Debug.Log($"[WiseTwinTrainingController] Answer selected: {index}");
    }
    
    void OnQuestionSubmitted()
    {
        // Check if we have questions loaded
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[WiseTwinTrainingController] No questions loaded yet");
            uiManager.ShowNotification("Questions are still loading...", NotificationType.Warning);
            return;
        }
        
        // Check if current question index is valid
        if (currentQuestionIndex >= questions.Count)
        {
            Debug.LogWarning("[WiseTwinTrainingController] Invalid question index");
            return;
        }
        
        if (selectedAnswerIndex < 0)
        {
            uiManager.ShowNotification("Please select an answer first!", NotificationType.Warning);
            return;
        }
        
        var currentQuestion = questions[currentQuestionIndex];
        bool isCorrect = selectedAnswerIndex == currentQuestion.correctAnswer;
        
        if (isCorrect)
        {
            correctAnswers++;
            uiManager.ShowNotification(currentQuestion.feedback, NotificationType.Success, 3f);
            
            // Log to WiseTwin system
            Debug.Log($"[WiseTwin] Correct answer for question {currentQuestionIndex + 1}");
        }
        else
        {
            uiManager.ShowNotification(currentQuestion.incorrectFeedback, NotificationType.Error, 3f);
            
            // Log incorrect answer
            Debug.Log($"[WiseTwin] Incorrect answer for question {currentQuestionIndex + 1}");
        }
        
        // Move to next question
        currentQuestionIndex++;
        selectedAnswerIndex = -1;
        
        // Show next question after a delay
        Invoke(nameof(ShowNextQuestion), 2f);
    }
    
    void CompleteTraining()
    {
        // Calculate score
        float score = (float)correctAnswers / questions.Count * 100f;
        
        // Show completion message
        string message = $"Training completed!\nScore: {score:F0}%\nCorrect answers: {correctAnswers}/{questions.Count}";
        uiManager.ShowNotification(message, NotificationType.Success, 5f);
        
        // Update final progress
        uiManager.UpdateProgress(questions.Count, questions.Count);
        
        // Complete training in UI
        uiManager.CompleteTraining();
        
        // This will trigger the WiseTwinManager completion
        // which sends notification to your Next.js app
        Debug.Log($"[WiseTwin] Training completed with score: {score:F0}%");
    }
    
    void OnDestroy()
    {
        // Cleanup
        if (uiManager != null)
        {
            uiManager.OnAnswerSelected -= OnAnswerSelected;
            uiManager.OnQuestionSubmitted -= OnQuestionSubmitted;
        }
        
        if (WiseTwinManager.Instance != null)
        {
            WiseTwinManager.Instance.OnMetadataReady -= OnMetadataLoaded;
        }
    }
    
    #region Public Methods for External Triggers
    
    /// <summary>
    /// Trigger a specific question when user interacts with an object
    /// </summary>
    public void TriggerQuestionForObject(GameObject interactedObject)
    {
        // Find question associated with this object
        for (int i = 0; i < questions.Count; i++)
        {
            if (questions[i].objectId == interactedObject.name)
            {
                currentQuestionIndex = i;
                ShowNextQuestion();
                break;
            }
        }
    }
    
    /// <summary>
    /// Skip current question (for testing)
    /// </summary>
    public void SkipQuestion()
    {
        currentQuestionIndex++;
        selectedAnswerIndex = -1;
        ShowNextQuestion();
    }
    
    /// <summary>
    /// Restart the training
    /// </summary>
    public void RestartTraining()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;
        selectedAnswerIndex = -1;
        StartTraining();
    }
    
    #endregion
}