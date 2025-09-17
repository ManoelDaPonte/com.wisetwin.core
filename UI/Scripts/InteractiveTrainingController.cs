using UnityEngine;
using System.Collections.Generic;
using WiseTwin;
using WiseTwin.UI;
using Newtonsoft.Json;
using System.Linq;

/// <summary>
/// Système de formation interactive utilisant les metadata WiseTwin
/// Charge les questions depuis les metadata et les déclenche au clic sur les objets
/// </summary>
public class WiseTwinInteractiveTraining : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private LayerMask interactableLayer = -1; // Layer pour les objets cliquables
    
    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 2f;
    
    [Header("Status (Read Only)")]
    [SerializeField] private bool metadataLoaded = false;
    [SerializeField] private int totalQuestionsLoaded = 0;
    
    // Références
    private WiseTwinUIManager uiManager;
    private WiseTwinManager wiseTwinManager;
    
    // Données des questions par objet
    private Dictionary<string, List<QuestionData>> objectQuestions = new Dictionary<string, List<QuestionData>>();
    private Dictionary<string, GameObject> interactiveObjects = new Dictionary<string, GameObject>();
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    
    // État actuel
    private string currentObjectId;
    private int currentQuestionIndex = 0;
    private int selectedAnswer = -1;
    private int totalCorrectAnswers = 0;
    private int totalQuestionsAnswered = 0;
    
    // Structure pour stocker les questions
    [System.Serializable]
    public class QuestionData
    {
        public string objectId;
        public string questionId;
        public string text;
        public string type;
        public string[] options;
        public int correctAnswer;
        public string feedback;
        public string incorrectFeedback;
    }
    
    void Start()
    {
        InitializeReferences();
        
        if (autoLoadOnStart)
        {
            LoadMetadataQuestions();
        }
        
        // Trouver et enregistrer tous les objets interactifs
        RegisterInteractiveObjects();
    }
    
    void InitializeReferences()
    {
        // Récupérer le UI Manager
        uiManager = WiseTwinUIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogError("[WiseTwinInteractiveTraining] WiseTwinUIManager not found!");
            return;
        }
        
        // Récupérer le WiseTwin Manager
        wiseTwinManager = WiseTwinManager.Instance;
        if (wiseTwinManager == null)
        {
            Debug.LogWarning("[WiseTwinInteractiveTraining] WiseTwinManager not found. Will use fallback data.");
        }
        
        // S'abonner aux événements UI
        uiManager.OnAnswerSelected += OnAnswerSelected;
        uiManager.OnQuestionSubmitted += OnQuestionSubmitted;
        
        Debug.Log("[WiseTwinInteractiveTraining] Initialized successfully");
    }
    
    void RegisterInteractiveObjects()
    {
        // Chercher les objets "Red Cube" et "Blue Sphere" dans la scène
        GameObject redCube = GameObject.Find("Red Cube");
        GameObject blueSphere = GameObject.Find("Blue Sphere");
        
        if (redCube != null)
        {
            RegisterObject(redCube, "red_cube");
            Debug.Log("[WiseTwinInteractiveTraining] Registered Red Cube");
        }
        
        if (blueSphere != null)
        {
            RegisterObject(blueSphere, "blue_sphere");
            Debug.Log("[WiseTwinInteractiveTraining] Registered Blue Sphere");
        }
        
        // Chercher d'autres objets potentiels basés sur les metadata
        if (wiseTwinManager != null && wiseTwinManager.IsMetadataLoaded)
        {
            var objectIds = wiseTwinManager.GetAvailableObjectIds();
            foreach (var id in objectIds)
            {
                // Convertir l'ID en nom GameObject potentiel (red_cube -> Red Cube)
                string potentialName = ConvertIdToGameObjectName(id);
                GameObject obj = GameObject.Find(potentialName);
                
                if (obj != null && !interactiveObjects.ContainsKey(id))
                {
                    RegisterObject(obj, id);
                    Debug.Log($"[WiseTwinInteractiveTraining] Auto-registered {potentialName} as {id}");
                }
            }
        }
    }
    
    void RegisterObject(GameObject obj, string objectId)
    {
        // Ajouter à la liste des objets interactifs
        interactiveObjects[objectId] = obj;
        
        // Sauvegarder le matériau original
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterials[obj] = renderer.material;
        }
        
        // Ajouter un collider si nécessaire
        if (obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
            Debug.Log($"[WiseTwinInteractiveTraining] Added collider to {obj.name}");
        }
    }
    
    string ConvertIdToGameObjectName(string id)
    {
        // Convertir snake_case en Title Case
        // red_cube -> Red Cube
        string[] parts = id.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }
        }
        return string.Join(" ", parts);
    }
    
    public void LoadMetadataQuestions()
    {
        Debug.Log("[WiseTwinInteractiveTraining] Loading questions from metadata...");
        
        if (wiseTwinManager == null || !wiseTwinManager.IsMetadataLoaded)
        {
            Debug.LogWarning("[WiseTwinInteractiveTraining] Metadata not ready, using example data");
            LoadExampleQuestions();
            return;
        }
        
        // Obtenir la langue préférée
        string language = wiseTwinManager.GetPreferredLanguage();
        Debug.Log($"[WiseTwinInteractiveTraining] Using language: {language}");
        
        // Parcourir tous les objets disponibles dans les metadata
        var objectIds = wiseTwinManager.GetAvailableObjectIds();
        totalQuestionsLoaded = 0;
        
        foreach (var objectId in objectIds)
        {
            var objectData = wiseTwinManager.GetDataForObject(objectId);
            if (objectData == null) continue;
            
            List<QuestionData> questions = new List<QuestionData>();
            
            // Parcourir toutes les clés de l'objet pour trouver les questions
            foreach (var kvp in objectData)
            {
                string key = kvp.Key;
                
                // Si la clé contient "question", c'est probablement une question
                if (key.ToLower().Contains("question"))
                {
                    try
                    {
                        // Convertir en JSON pour faciliter le parsing
                        string json = JsonConvert.SerializeObject(kvp.Value);
                        var questionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        
                        if (questionDict != null)
                        {
                            QuestionData question = ParseQuestionFromMetadata(questionDict, objectId, key, language);
                            if (question != null)
                            {
                                questions.Add(question);
                                totalQuestionsLoaded++;
                                Debug.Log($"[WiseTwinInteractiveTraining] Loaded question '{key}' for {objectId}");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[WiseTwinInteractiveTraining] Error parsing question {key}: {e.Message}");
                    }
                }
            }
            
            if (questions.Count > 0)
            {
                objectQuestions[objectId] = questions;
                Debug.Log($"[WiseTwinInteractiveTraining] Loaded {questions.Count} questions for {objectId}");
            }
        }
        
        metadataLoaded = true;
        
        // Mettre à jour le titre de la formation
        string title = wiseTwinManager.GetProjectInfo("title");
        if (!string.IsNullOrEmpty(title))
        {
            uiManager.SetTrainingTitle(title);
        }
        
        // Notification
        if (totalQuestionsLoaded > 0)
        {
            uiManager.ShowNotification(
                $"✅ {totalQuestionsLoaded} questions chargées depuis les metadata!", 
                NotificationType.Success, 
                3f
            );
            
            // Instruction pour l'utilisateur
            uiManager.ShowNotification(
                "📍 Cliquez sur un objet pour commencer!", 
                NotificationType.Info, 
                5f
            );
        }
        else
        {
            Debug.LogWarning("[WiseTwinInteractiveTraining] No questions found in metadata");
            LoadExampleQuestions();
        }
    }
    
    QuestionData ParseQuestionFromMetadata(Dictionary<string, object> data, string objectId, string questionId, string language)
    {
        var question = new QuestionData
        {
            objectId = objectId,
            questionId = questionId
        };
        
        try
        {
            // Récupérer le texte (peut être multilingue)
            if (data.ContainsKey("text"))
            {
                question.text = ExtractLocalizedText(data["text"], language);
            }
            
            // Type de question
            if (data.ContainsKey("type"))
            {
                question.type = data["type"].ToString();
            }
            
            // Options (peut être multilingue)
            if (data.ContainsKey("options"))
            {
                question.options = ExtractLocalizedOptions(data["options"], language);
            }
            
            // Réponse correcte
            if (data.ContainsKey("correctAnswer"))
            {
                question.correctAnswer = System.Convert.ToInt32(data["correctAnswer"]);
            }
            
            // Feedback (peut être multilingue)
            if (data.ContainsKey("feedback"))
            {
                question.feedback = ExtractLocalizedText(data["feedback"], language);
            }
            
            if (data.ContainsKey("incorrectFeedback"))
            {
                question.incorrectFeedback = ExtractLocalizedText(data["incorrectFeedback"], language);
            }
            
            // Vérifier que la question est valide
            if (string.IsNullOrEmpty(question.text))
            {
                return null;
            }
            
            // Pour les questions true/false, créer les options si elles n'existent pas
            if (question.type == "true-false" && (question.options == null || question.options.Length == 0))
            {
                question.options = language == "fr" ? 
                    new string[] { "Vrai", "Faux" } : 
                    new string[] { "True", "False" };
            }
            
            return question;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WiseTwinInteractiveTraining] Error parsing question data: {e.Message}");
            return null;
        }
    }
    
    string ExtractLocalizedText(object textData, string language)
    {
        if (textData is string)
        {
            return textData.ToString();
        }
        
        try
        {
            var textDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(textData)
            );
            
            if (textDict != null)
            {
                // Essayer la langue demandée
                if (textDict.ContainsKey(language))
                {
                    return textDict[language];
                }
                // Fallback vers l'anglais
                if (textDict.ContainsKey("en"))
                {
                    return textDict["en"];
                }
                // Prendre la première langue disponible
                if (textDict.Count > 0)
                {
                    return textDict.Values.First();
                }
            }
        }
        catch { }
        
        return textData?.ToString() ?? "";
    }
    
    string[] ExtractLocalizedOptions(object optionsData, string language)
    {
        if (optionsData is string[])
        {
            return optionsData as string[];
        }
        
        try
        {
            var optionsDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                JsonConvert.SerializeObject(optionsData)
            );
            
            if (optionsDict != null)
            {
                if (optionsDict.ContainsKey(language))
                {
                    return optionsDict[language];
                }
                if (optionsDict.ContainsKey("en"))
                {
                    return optionsDict["en"];
                }
                if (optionsDict.Count > 0)
                {
                    return optionsDict.Values.First();
                }
            }
        }
        catch { }
        
        // Essayer de parser comme un tableau simple
        try
        {
            return JsonConvert.DeserializeObject<string[]>(
                JsonConvert.SerializeObject(optionsData)
            );
        }
        catch { }
        
        return new string[0];
    }
    
    void LoadExampleQuestions()
    {
        // Questions de secours si les metadata ne sont pas disponibles
        Debug.Log("[WiseTwinInteractiveTraining] Loading example questions as fallback");
        
        // Questions pour le cube rouge
        var redCubeQuestions = new List<QuestionData>
        {
            new QuestionData
            {
                objectId = "red_cube",
                questionId = "q1",
                text = "De quelle couleur est ce cube ?",
                type = "multiple-choice",
                options = new string[] { "Rouge", "Bleu", "Vert" },
                correctAnswer = 0,
                feedback = "Correct ! C'est rouge.",
                incorrectFeedback = "Regardez mieux la couleur !"
            }
        };
        
        // Questions pour la sphère bleue
        var blueSphereQuestions = new List<QuestionData>
        {
            new QuestionData
            {
                objectId = "blue_sphere",
                questionId = "q1",
                text = "Cette forme est-elle une sphère ?",
                type = "true-false",
                options = new string[] { "Vrai", "Faux" },
                correctAnswer = 0,
                feedback = "Exactement ! C'est une sphère.",
                incorrectFeedback = "Regardez attentivement la forme !"
            }
        };
        
        objectQuestions["red_cube"] = redCubeQuestions;
        objectQuestions["blue_sphere"] = blueSphereQuestions;
        
        totalQuestionsLoaded = 2;
        metadataLoaded = true;
    }
    
    void Update()
    {
        // Détection du clic sur les objets
        if (Input.GetMouseButtonDown(0))
        {
            HandleObjectClick();
        }
        
        // Debug: Afficher les objets disponibles
        if (showDebugInfo && Input.GetKeyDown(KeyCode.I))
        {
            ShowAvailableObjects();
        }
    }
    
    void HandleObjectClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, interactableLayer))
        {
            GameObject clickedObject = hit.collider.gameObject;
            
            // Trouver l'ID de l'objet cliqué
            foreach (var kvp in interactiveObjects)
            {
                if (kvp.Value == clickedObject)
                {
                    OnObjectClicked(kvp.Key, clickedObject);
                    break;
                }
            }
        }
    }
    
    void OnObjectClicked(string objectId, GameObject obj)
    {
        Debug.Log($"[WiseTwinInteractiveTraining] Object clicked: {objectId}");
        
        // Vérifier si cet objet a des questions
        if (!objectQuestions.ContainsKey(objectId))
        {
            uiManager.ShowNotification(
                $"Aucune question disponible pour {obj.name}", 
                NotificationType.Warning
            );
            return;
        }
        
        // Mettre en surbrillance l'objet
        HighlightObject(obj);
        
        // Démarrer les questions pour cet objet
        StartObjectQuestions(objectId);
    }
    
    void StartObjectQuestions(string objectId)
    {
        currentObjectId = objectId;
        currentQuestionIndex = 0;
        selectedAnswer = -1;
        
        var questions = objectQuestions[objectId];
        
        Debug.Log($"[WiseTwinInteractiveTraining] Starting {questions.Count} questions for {objectId}");
        
        // Notification
        string objectName = interactiveObjects[objectId].name;
        uiManager.ShowNotification(
            $"📋 Questions pour : {objectName}", 
            NotificationType.Info, 
            2f
        );
        
        // Afficher la première question
        ShowCurrentQuestion();
    }
    
    void ShowCurrentQuestion()
    {
        if (string.IsNullOrEmpty(currentObjectId) || !objectQuestions.ContainsKey(currentObjectId))
        {
            Debug.LogError("[WiseTwinInteractiveTraining] No current object selected");
            return;
        }
        
        var questions = objectQuestions[currentObjectId];
        
        if (currentQuestionIndex >= questions.Count)
        {
            // Fin des questions pour cet objet
            OnObjectQuestionsComplete();
            return;
        }
        
        var question = questions[currentQuestionIndex];
        
        // Déterminer le type de question
        QuestionType qType = QuestionType.MultipleChoice;
        if (question.type == "true-false")
        {
            qType = QuestionType.TrueFalse;
        }
        
        // Afficher la question
        uiManager.ShowQuestion(question.text, question.options, qType);
        
        Debug.Log($"[WiseTwinInteractiveTraining] Showing question {currentQuestionIndex + 1}/{questions.Count}");
    }
    
    void OnAnswerSelected(int index)
    {
        selectedAnswer = index;
        Debug.Log($"[WiseTwinInteractiveTraining] Answer selected: {index}");
    }
    
    void OnQuestionSubmitted()
    {
        if (selectedAnswer < 0)
        {
            uiManager.ShowNotification("⚠️ Sélectionnez une réponse!", NotificationType.Warning);
            return;
        }
        
        var questions = objectQuestions[currentObjectId];
        var currentQuestion = questions[currentQuestionIndex];
        
        bool isCorrect = selectedAnswer == currentQuestion.correctAnswer;
        
        // Statistiques
        totalQuestionsAnswered++;
        if (isCorrect)
        {
            totalCorrectAnswers++;
        }
        
        // Feedback
        if (isCorrect)
        {
            uiManager.ShowNotification(currentQuestion.feedback, NotificationType.Success, 3f);
        }
        else
        {
            uiManager.ShowNotification(currentQuestion.incorrectFeedback, NotificationType.Error, 3f);
        }
        
        // Passer à la question suivante
        currentQuestionIndex++;
        selectedAnswer = -1;
        
        // Afficher la prochaine question après un délai
        Invoke(nameof(ShowCurrentQuestion), 2f);
    }
    
    void OnObjectQuestionsComplete()
    {
        var objectName = interactiveObjects[currentObjectId].name;
        
        uiManager.ShowNotification(
            $"✅ Questions terminées pour {objectName}!", 
            NotificationType.Success, 
            3f
        );
        
        // Retirer la surbrillance
        RemoveHighlight(interactiveObjects[currentObjectId]);
        
        // Mettre à jour la progression globale
        UpdateGlobalProgress();
        
        // Réinitialiser
        currentObjectId = null;
        currentQuestionIndex = 0;
        
        // Vérifier si toutes les questions sont complétées
        CheckIfAllComplete();
    }
    
    void UpdateGlobalProgress()
    {
        int totalQuestions = 0;
        foreach (var kvp in objectQuestions)
        {
            totalQuestions += kvp.Value.Count;
        }
        
        uiManager.UpdateProgress(totalQuestionsAnswered, totalQuestions);
    }
    
    void CheckIfAllComplete()
    {
        int totalQuestions = 0;
        foreach (var kvp in objectQuestions)
        {
            totalQuestions += kvp.Value.Count;
        }
        
        if (totalQuestionsAnswered >= totalQuestions)
        {
            OnTrainingComplete();
        }
        else
        {
            uiManager.ShowNotification(
                "📍 Cliquez sur un autre objet pour continuer", 
                NotificationType.Info, 
                4f
            );
        }
    }
    
    void OnTrainingComplete()
    {
        float score = (float)totalCorrectAnswers / totalQuestionsAnswered * 100f;
        
        string message = $"🎉 Formation terminée!\n" +
                        $"Score: {score:F0}%\n" +
                        $"Réponses correctes: {totalCorrectAnswers}/{totalQuestionsAnswered}";
        
        uiManager.ShowNotification(message, NotificationType.Success, 6f);
        uiManager.CompleteTraining();
        
        Debug.Log($"[WiseTwinInteractiveTraining] Training complete - Score: {score:F0}%");
    }
    
    void HighlightObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Créer un matériau émissif
            Material highlightMat = new Material(renderer.material);
            highlightMat.EnableKeyword("_EMISSION");
            highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            renderer.material = highlightMat;
        }
    }
    
    void RemoveHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.ContainsKey(obj))
        {
            renderer.material = originalMaterials[obj];
        }
    }
    
    void ShowAvailableObjects()
    {
        Debug.Log("[WiseTwinInteractiveTraining] === Available Objects ===");
        foreach (var kvp in objectQuestions)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value.Count} questions");
        }
        
        string info = $"Objets disponibles:\n";
        foreach (var kvp in objectQuestions)
        {
            info += $"• {kvp.Key}: {kvp.Value.Count} questions\n";
        }
        
        uiManager.ShowNotification(info, NotificationType.Info, 5f);
    }
    
    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnAnswerSelected -= OnAnswerSelected;
            uiManager.OnQuestionSubmitted -= OnQuestionSubmitted;
        }
        
        // Restaurer les matériaux originaux
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                Renderer renderer = kvp.Key.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = kvp.Value;
                }
            }
        }
    }
    
    // Méthodes publiques pour contrôle externe
    
    [ContextMenu("Reload Metadata Questions")]
    public void ReloadQuestions()
    {
        objectQuestions.Clear();
        LoadMetadataQuestions();
    }
    
    [ContextMenu("Show Debug Info")]
    public void ShowDebugInfo()
    {
        ShowAvailableObjects();
    }
}