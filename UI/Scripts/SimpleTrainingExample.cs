using UnityEngine;
using System.Collections.Generic;
using WiseTwin.UI;

/// <summary>
/// Exemple simple et autonome d'utilisation de WiseTwinUIManager
/// Fonctionne sans dépendance au système WiseTwin complet
/// </summary>
public class SimpleTrainingExample : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string trainingTitle = "Formation Autoclave - Test";
    [SerializeField] private bool autoStart = true;
    
    [Header("Objets Interactifs (optionnel)")]
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject sphere;
    
    private WiseTwinUIManager uiManager;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private int selectedAnswer = -1;
    
    // Questions d'exemple basées sur votre document QCM
    private List<Question> questions = new List<Question>();
    
    [System.Serializable]
    public class Question
    {
        public string text;
        public string[] options;
        public int correctAnswer;
        public string feedback;
        public string incorrectFeedback;
        public GameObject linkedObject; // Objet 3D associé
    }
    
    void Start()
    {
        // Récupérer le UI Manager
        uiManager = WiseTwinUIManager.Instance;
        
        if (uiManager == null)
        {
            Debug.LogError("[SimpleTrainingExample] WiseTwinUIManager n'est pas dans la scène!");
            return;
        }
        
        // Configurer les événements
        uiManager.OnAnswerSelected += OnAnswerSelected;
        uiManager.OnQuestionSubmitted += OnQuestionSubmitted;
        
        // Charger les questions
        LoadQuestions();
        
        // Démarrer automatiquement si configuré
        if (autoStart)
        {
            StartTraining();
        }
    }
    
    void LoadQuestions()
    {
        // Question 1 - Autorisation autoclave
        questions.Add(new Question
        {
            text = "Qui est autorisé à utiliser l'autoclave ?",
            options = new string[] {
                "Toute personne formée en production",
                "Toute personne habilitée à l'utilisation d'appareils sous pression",
                "Le chef d'équipe uniquement",
                "Un opérateur expérimenté, même sans habilitation"
            },
            correctAnswer = 1,
            feedback = "✅ Correct ! L'habilitation \"appareil sous pression\" est obligatoire pour des raisons de sécurité.",
            incorrectFeedback = "❌ Incorrect. Seules les personnes habilitées peuvent utiliser l'autoclave.",
            linkedObject = cube
        });
        
        // Question 2 - LOTO
        questions.Add(new Question
        {
            text = "Avant d'ouvrir l'autoclave, que doit-on impérativement appliquer ?",
            options = new string[] {
                "Le LOTO (consignation énergie)",
                "Une simple coupure électrique",
                "Le port des gants thermiques uniquement",
                "Rien, si le cycle est terminé"
            },
            correctAnswer = 0,
            feedback = "✅ Excellent ! Le LOTO est essentiel pour garantir la sécurité.",
            incorrectFeedback = "❌ Attention ! Le LOTO (Lock Out Tag Out) est obligatoire avant toute intervention.",
            linkedObject = sphere
        });
        
        // Question 3 - Produits inflammables
        questions.Add(new Question
        {
            text = "Pourquoi est-il interdit d'utiliser des produits inflammables sur le verre avant passage à l'autoclave ?",
            options = new string[] {
                "Ils peuvent s'évaporer et provoquer un incendie/explosion",
                "Ils dégradent la qualité du pare-brise",
                "Ils sont coûteux",
                "Ils compliquent le nettoyage de l'autoclave"
            },
            correctAnswer = 0,
            feedback = "✅ Correct ! La haute température peut créer un risque d'explosion.",
            incorrectFeedback = "❌ Le risque principal est l'incendie ou l'explosion à haute température.",
            linkedObject = cube
        });
        
        // Question 4 - Cycle de nettoyage
        questions.Add(new Question
        {
            text = "Tous les combien de cycles de production un cycle de nettoyage à vide doit-il être effectué ?",
            options = new string[] {
                "Tous les 10 cycles",
                "Tous les 20 cycles",
                "Tous les 30 cycles",
                "Une fois par mois uniquement"
            },
            correctAnswer = 2,
            feedback = "✅ Parfait ! Un cycle de nettoyage tous les 30 cycles maintient la performance.",
            incorrectFeedback = "❌ Le cycle de nettoyage doit être effectué tous les 30 cycles de production.",
            linkedObject = sphere
        });
        
        // Question 5 - Ligne d'urgence
        questions.Add(new Question
        {
            text = "À quoi sert la ligne d'urgence installée à l'intérieur de l'autoclave ?",
            options = new string[] {
                "À couper immédiatement toutes les énergies en cas de danger",
                "À signaler la fin de cycle",
                "À lancer un cycle de nettoyage automatique",
                "À ouvrir la porte de l'autoclave automatiquement"
            },
            correctAnswer = 0,
            feedback = "✅ Exact ! C'est un dispositif de sécurité crucial en cas d'urgence.",
            incorrectFeedback = "❌ La ligne d'urgence sert à couper toutes les énergies en cas de danger.",
            linkedObject = cube
        });
        
        Debug.Log($"[SimpleTrainingExample] {questions.Count} questions chargées");
    }
    
    public void StartTraining()
    {
        Debug.Log("[SimpleTrainingExample] Démarrage de la formation");
        
        // Réinitialiser
        currentQuestionIndex = 0;
        correctAnswers = 0;
        selectedAnswer = -1;
        
        // Configurer l'UI
        uiManager.SetTrainingTitle(trainingTitle);
        uiManager.StartTraining();
        uiManager.UpdateProgress(0, questions.Count);
        
        // Message de bienvenue
        uiManager.ShowNotification("🎯 Bienvenue dans la formation Autoclave!", NotificationType.Info, 3f);
        
        // Première question après un délai
        Invoke(nameof(ShowNextQuestion), 2f);
    }
    
    void ShowNextQuestion()
    {
        if (currentQuestionIndex < questions.Count)
        {
            var question = questions[currentQuestionIndex];
            
            // Mettre en évidence l'objet associé si présent
            if (question.linkedObject != null)
            {
                HighlightObject(question.linkedObject);
            }
            
            // Afficher la question
            uiManager.ShowQuestion(
                question.text,
                question.options,
                QuestionType.MultipleChoice
            );
            
            // Mettre à jour la progression
            uiManager.UpdateProgress(currentQuestionIndex, questions.Count);
            
            Debug.Log($"[SimpleTrainingExample] Question {currentQuestionIndex + 1}/{questions.Count}");
        }
        else
        {
            CompleteTraining();
        }
    }
    
    void HighlightObject(GameObject obj)
    {
        if (obj == null) return;
        
        // Exemple simple : faire pulser l'objet
        StartCoroutine(PulseObject(obj));
    }
    
    System.Collections.IEnumerator PulseObject(GameObject obj)
    {
        var originalScale = obj.transform.localScale;
        float duration = 0.5f;
        float elapsed = 0;
        
        // Grossir
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);
            yield return null;
        }
        
        // Rétrécir
        elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }
        
        obj.transform.localScale = originalScale;
    }
    
    void OnAnswerSelected(int index)
    {
        selectedAnswer = index;
        Debug.Log($"[SimpleTrainingExample] Réponse sélectionnée : {index}");
    }
    
    void OnQuestionSubmitted()
    {
        if (selectedAnswer < 0)
        {
            uiManager.ShowNotification("⚠️ Veuillez sélectionner une réponse!", NotificationType.Warning);
            return;
        }
        
        var currentQuestion = questions[currentQuestionIndex];
        bool isCorrect = selectedAnswer == currentQuestion.correctAnswer;
        
        // Feedback
        if (isCorrect)
        {
            correctAnswers++;
            uiManager.ShowNotification(currentQuestion.feedback, NotificationType.Success, 4f);
        }
        else
        {
            uiManager.ShowNotification(currentQuestion.incorrectFeedback, NotificationType.Error, 4f);
        }
        
        // Question suivante
        currentQuestionIndex++;
        selectedAnswer = -1;
        
        // Attendre avant la prochaine question
        Invoke(nameof(ShowNextQuestion), 3f);
    }
    
    void CompleteTraining()
    {
        // Calculer le score
        float score = (float)correctAnswers / questions.Count * 100f;
        
        // Message de fin
        string message = $"🎉 Formation terminée!\n" +
                        $"Score: {score:F0}%\n" +
                        $"Réponses correctes: {correctAnswers}/{questions.Count}";
        
        uiManager.ShowNotification(message, NotificationType.Success, 6f);
        uiManager.UpdateProgress(questions.Count, questions.Count);
        uiManager.CompleteTraining();
        
        Debug.Log($"[SimpleTrainingExample] Formation complétée - Score: {score:F0}%");
    }
    
    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnAnswerSelected -= OnAnswerSelected;
            uiManager.OnQuestionSubmitted -= OnQuestionSubmitted;
        }
    }
    
    // Méthodes publiques pour contrôle externe
    
    [ContextMenu("Recommencer la formation")]
    public void RestartTraining()
    {
        StartTraining();
    }
    
    [ContextMenu("Passer à la question suivante")]
    public void SkipToNext()
    {
        currentQuestionIndex++;
        ShowNextQuestion();
    }
}