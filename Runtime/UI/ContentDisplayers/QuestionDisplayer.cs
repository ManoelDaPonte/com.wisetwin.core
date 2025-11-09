using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using WiseTwin.Analytics;
using Newtonsoft.Json.Linq;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour les questions (QCM, Vrai/Faux, etc.)
    /// Supporte les questions multiples séquentielles
    /// </summary>
    public class QuestionDisplayer : MonoBehaviour, IContentDisplayer
    {
        [Header("🔧 Debug Settings")]
        [SerializeField, Tooltip("Enable debug logs for this component")]
        private bool enableDebugLogs = false;

        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;
        private int selectedAnswerIndex = -1;
        private List<int> selectedAnswerIndexes = new List<int>(); // Pour les réponses multiples
        private int correctAnswerIndex; // Pour réponse unique
        private List<int> correctAnswerIndexes = new List<int>(); // Pour réponses multiples
        private bool hasAnswered = false;
        private bool isMultipleChoice = false; // Détermine si c'est un QCM multiple ou unique

        // Pour gérer les questions séquentielles
        private List<string> questionKeys;
        private int currentQuestionIndex = 0;
        private Dictionary<string, object> allObjectData;

        // Références UI pour mise à jour
        private Label questionLabel;
        private Label progressLabel;
        private VisualElement optionsContainer;
        private Button validateButton;
        private VisualElement feedbackContainer;
        private Label feedbackLabel;
        private string currentFeedback;
        private string currentIncorrectFeedback;

        // Analytics tracking
        private QuestionInteractionData currentQuestionData;
        private string currentQuestionKey; // Clé de la question pour tracking

        // Store content data for language change updates
        private Dictionary<string, object> storedContentData;
        private Dictionary<string, object> currentQuestionData_Raw; // Current question raw data

        // Interface implementation
        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;

            // Bloquer les contrôles du personnage pendant l'affichage de la question
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(false);
            }

            // Store content data for language changes
            storedContentData = contentData;

            // Subscribe to language changes
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }

            // Si contentData contient déjà les données de question directement (nouveau format)
            if (contentData.ContainsKey("questionText") || contentData.ContainsKey("options"))
            {
                // Question unique passée directement
                questionKeys = null;
                allObjectData = null;
                DisplaySingleQuestion(contentData);
            }
            // NEW: Support for "questions" array format
            else if (contentData.ContainsKey("questions"))
            {
                var questionsValue = contentData["questions"];
                if (questionsValue is Newtonsoft.Json.Linq.JArray questionsArray && questionsArray.Count > 0)
                {
                    // Convert JArray to list of dictionaries
                    allObjectData = new Dictionary<string, object>();
                    questionKeys = new List<string>();

                    for (int i = 0; i < questionsArray.Count; i++)
                    {
                        string questionKey = $"question_{i + 1}";
                        var questionDict = questionsArray[i].ToObject<Dictionary<string, object>>();
                        allObjectData[questionKey] = questionDict;
                        questionKeys.Add(questionKey);
                    }

                    if (questionKeys.Count > 0)
                    {
                        currentQuestionIndex = 0;
                        CreateQuestionUI(); // Create UI elements before displaying
                        DisplayCurrentQuestion();
                    }
                    else
                    {
                        Debug.LogError("[QuestionDisplayer] No questions found in array");
                    }
                }
                else if (questionsValue is List<object> questionsList && questionsList.Count > 0)
                {
                    // Handle as list of objects
                    allObjectData = new Dictionary<string, object>();
                    questionKeys = new List<string>();

                    for (int i = 0; i < questionsList.Count; i++)
                    {
                        string questionKey = $"question_{i + 1}";
                        allObjectData[questionKey] = questionsList[i];
                        questionKeys.Add(questionKey);
                    }

                    if (questionKeys.Count > 0)
                    {
                        currentQuestionIndex = 0;
                        CreateQuestionUI(); // Create UI elements before displaying
                        DisplayCurrentQuestion();
                    }
                }
                else
                {
                    Debug.LogError("[QuestionDisplayer] Invalid 'questions' format");
                }
            }
            else
            {
                Debug.LogError($"[QuestionDisplayer] No valid question format found for {objectId}. Expected 'questionTextEN/FR' or 'questions' array.");
            }
        }

        private void DisplaySingleQuestion(Dictionary<string, object> contentData)
        {
            // Store raw question data for language updates
            currentQuestionData_Raw = contentData;

            hasAnswered = false;
            isValidating = false; // Réinitialiser le flag de validation
            selectedAnswerIndex = -1;
            selectedAnswerIndexes.Clear();

            // Réinitialiser le bouton (désactivé au départ)
            if (validateButton != null)
            {
                validateButton.SetEnabled(false);
                validateButton.style.opacity = 0.5f;
            }

            // Masquer le feedback container
            if (feedbackContainer != null)
            {
                feedbackContainer.style.display = DisplayStyle.None;
            }

            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Displaying single question for {currentObjectId}");
            }

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données de la question (nouveau format uniquement)
            string questionText = ExtractLocalizedText(contentData, "questionText", lang);
            var options = ExtractLocalizedList(contentData, "options", lang);
            currentQuestionKey = "question"; // Clé unique pour question simple

            // Vérifier le mode de sélection (nouveau format uniquement)
            isMultipleChoice = contentData.ContainsKey("isMultipleChoice") && contentData["isMultipleChoice"] is bool b && b;

            // Gérer les réponses correctes selon le mode
            if (isMultipleChoice)
            {
                // Pour les réponses multiples, on peut avoir un tableau ou une string avec des virgules
                correctAnswerIndexes.Clear();
                if (contentData.ContainsKey("correctAnswers"))
                {
                    var correctAnswers = contentData["correctAnswers"];
                    LogDebug($"[MULTIPLE CHOICE] correctAnswers type: {correctAnswers?.GetType()?.FullName ?? "null"}");

                    if (correctAnswers is Newtonsoft.Json.Linq.JArray jarray)
                    {
                        correctAnswerIndexes = jarray.Select(x => (int)(long)x).ToList();
                        LogDebug($"Parsed as JArray: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else if (correctAnswers is List<object> list)
                    {
                        correctAnswerIndexes = list.Select(x => Convert.ToInt32(x)).ToList();
                        LogDebug($"Parsed as List<object>: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else if (correctAnswers is object[] objArray)
                    {
                        correctAnswerIndexes = objArray.Select(x => Convert.ToInt32(x)).ToList();
                        LogDebug($"Parsed as object[]: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else if (correctAnswers is string str)
                    {
                        correctAnswerIndexes = str.Split(',').Select(x => int.Parse(x.Trim())).ToList();
                        LogDebug($"Parsed as string: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else if (correctAnswers is int[] intArray)
                    {
                        correctAnswerIndexes = intArray.ToList();
                        LogDebug($"Parsed as int[]: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else if (correctAnswers is long[] longArray)
                    {
                        correctAnswerIndexes = longArray.Select(x => (int)x).ToList();
                        LogDebug($"Parsed as long[]: {string.Join(", ", correctAnswerIndexes)}");
                    }
                    else
                    {
                        // Fallback : essayer de sérialiser/désérialiser
                        LogWarning($"Unhandled correctAnswers type: {correctAnswers?.GetType()?.FullName}, attempting JSON conversion");
                        try
                        {
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(correctAnswers);
                            correctAnswerIndexes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(json);
                            LogDebug($"Parsed via JSON fallback: {string.Join(", ", correctAnswerIndexes)}");
                        }
                        catch (System.Exception e)
                        {
                            LogError($"Failed to parse correctAnswers: {e.Message}");
                        }
                    }

                    LogDebug($"Final correctAnswers for multiple choice: {string.Join(", ", correctAnswerIndexes)}");
                }
                else
                {
                    LogWarning("No correctAnswers field found for multiple choice question!");
                }
            }
            else
            {
                // Pour réponse unique - lire depuis correctAnswers[0]
                if (contentData.ContainsKey("correctAnswers"))
                {
                    var correctAnswers = contentData["correctAnswers"];
                    if (correctAnswers is Newtonsoft.Json.Linq.JArray jarray && jarray.Count > 0)
                    {
                        correctAnswerIndex = (int)(long)jarray[0];
                    }
                    else if (correctAnswers is List<object> list && list.Count > 0)
                    {
                        correctAnswerIndex = Convert.ToInt32(list[0]);
                    }
                    else if (correctAnswers is int[] intArray && intArray.Length > 0)
                    {
                        correctAnswerIndex = intArray[0];
                    }
                }
            }

            string feedback = ExtractLocalizedText(contentData, "feedback", lang);
            string incorrectFeedback = ExtractLocalizedText(contentData, "incorrectFeedback", lang);
            string questionType = ExtractString(contentData, "type");

            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Question: {questionText}");
                LogDebug($"Options count: {options?.Count ?? 0}");
            }

            // Créer l'UI pour question unique
            CreateSingleQuestionUI(questionText, options, questionType, feedback, incorrectFeedback);

            // Initialiser le tracking analytics
            InitializeQuestionTracking();
        }

        private void CreateQuestionUI()
        {
            // Clear root
            rootElement.Clear();

            // Container modal
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de question
            var questionBox = new VisualElement();
            questionBox.style.width = 700;
            questionBox.style.maxWidth = Length.Percent(90);
            questionBox.style.maxHeight = Length.Percent(80);
            questionBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            questionBox.style.overflow = Overflow.Hidden; // Cacher le dépassement
            questionBox.style.borderTopLeftRadius = 25;
            questionBox.style.borderTopRightRadius = 25;
            questionBox.style.borderBottomLeftRadius = 25;
            questionBox.style.borderBottomRightRadius = 25;
            questionBox.style.paddingTop = 40;
            questionBox.style.paddingBottom = 40;
            questionBox.style.paddingLeft = 40;
            questionBox.style.paddingRight = 40;

            // Indicateur de progression si plusieurs questions
            progressLabel = new Label();
            progressLabel.style.fontSize = 16;
            progressLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            progressLabel.style.marginBottom = 10;
            progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            questionBox.Add(progressLabel);

            // Texte de la question
            questionLabel = new Label();
            questionLabel.style.fontSize = 24;
            questionLabel.style.color = Color.white;
            questionLabel.style.marginBottom = 35;
            questionLabel.style.whiteSpace = WhiteSpace.Normal;
            questionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            questionBox.Add(questionLabel);

            // Container des options avec scroll
            optionsContainer = new ScrollView();
            optionsContainer.style.marginBottom = 10;
            optionsContainer.style.maxHeight = 400; // Hauteur max avant scroll
            optionsContainer.style.flexGrow = 1;
            questionBox.Add(optionsContainer);

            // Label d'instruction (choix unique/multiple) - APRÈS les options
            var instructionLabel = new Label();
            instructionLabel.name = "instruction-label";
            instructionLabel.style.fontSize = 14;
            instructionLabel.style.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            instructionLabel.style.marginTop = 5;
            instructionLabel.style.marginBottom = 15;
            instructionLabel.style.paddingTop = 8;
            instructionLabel.style.paddingBottom = 8;
            instructionLabel.style.paddingLeft = 12;
            instructionLabel.style.paddingRight = 12;
            instructionLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.4f);
            instructionLabel.style.borderTopLeftRadius = 6;
            instructionLabel.style.borderTopRightRadius = 6;
            instructionLabel.style.borderBottomLeftRadius = 6;
            instructionLabel.style.borderBottomRightRadius = 6;
            instructionLabel.style.whiteSpace = WhiteSpace.Normal;
            instructionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            instructionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            questionBox.Add(instructionLabel);

            // Zone de feedback (cachée au début)
            feedbackContainer = new VisualElement();
            feedbackContainer.name = "feedback-container";
            feedbackContainer.style.display = DisplayStyle.None;
            feedbackContainer.style.marginTop = 20;
            feedbackContainer.style.paddingTop = 20;
            feedbackContainer.style.paddingBottom = 20;
            feedbackContainer.style.paddingLeft = 20;
            feedbackContainer.style.paddingRight = 20;
            feedbackContainer.style.borderTopLeftRadius = 10;
            feedbackContainer.style.borderTopRightRadius = 10;
            feedbackContainer.style.borderBottomLeftRadius = 10;
            feedbackContainer.style.borderBottomRightRadius = 10;

            feedbackLabel = new Label();
            feedbackLabel.name = "feedback-text";
            feedbackLabel.style.fontSize = 18;
            feedbackLabel.style.color = Color.white;
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            feedbackContainer.Add(feedbackLabel);
            questionBox.Add(feedbackContainer);

            // Bouton Valider (désactivé par défaut jusqu'à sélection)
            validateButton = new Button(ValidateAnswer);
            validateButton.name = "validate-button";
            validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Valider" : "Validate";

            validateButton.style.height = 50;
            validateButton.style.fontSize = 18;
            validateButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            validateButton.style.color = Color.white;
            validateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            validateButton.style.borderTopLeftRadius = 10;
            validateButton.style.borderTopRightRadius = 10;
            validateButton.style.borderBottomLeftRadius = 10;
            validateButton.style.borderBottomRightRadius = 10;
            validateButton.style.marginTop = 20;
            validateButton.SetEnabled(false); // Désactivé par défaut
            validateButton.style.opacity = 0.5f;
            questionBox.Add(validateButton);

            modalContainer.Add(questionBox);
            rootElement.Add(modalContainer);
        }

        private void DisplayCurrentQuestion()
        {
            // Réinitialiser le flag de validation pour chaque nouvelle question
            isValidating = false;

            if (currentQuestionIndex >= questionKeys.Count)
            {
                // Toutes les questions ont été répondues
                OnCompleted?.Invoke(currentObjectId, true);
                Close();
                return;
            }

            hasAnswered = false;
            selectedAnswerIndex = -1;
            selectedAnswerIndexes.Clear();

            // Réinitialiser le bouton (désactivé pour la nouvelle question)
            if (validateButton != null)
            {
                validateButton.SetEnabled(false);
                validateButton.style.opacity = 0.5f;
            }

            // Masquer le feedback container
            if (feedbackContainer != null)
            {
                feedbackContainer.style.display = DisplayStyle.None;
            }

            string currentKey = questionKeys[currentQuestionIndex];
            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Displaying question {currentQuestionIndex + 1}/{questionKeys.Count}: {currentKey}");
            }

            // Mise à jour de l'indicateur de progression
            if (progressLabel != null)
            {
                if (questionKeys.Count > 1)
                {
                    progressLabel.text = $"Question {currentQuestionIndex + 1} / {questionKeys.Count}";
                    progressLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    progressLabel.style.display = DisplayStyle.None;
                }
            }

            if (allObjectData.ContainsKey(currentKey))
            {
                var questionData = allObjectData[currentKey];

                // Convertir en Dictionary si nécessaire
                Dictionary<string, object> questionDict = null;
                if (questionData is Dictionary<string, object> dict)
                {
                    questionDict = dict;
                    // Store raw question data for language updates
                    currentQuestionData_Raw = dict;
                }
                else if (questionData != null)
                {
                    try
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(questionData);
                        questionDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        // Store raw question data for language updates
                        currentQuestionData_Raw = questionDict;
                    }
                    catch (System.Exception e)
                    {
                        if (ContentDisplayManager.Instance?.DebugMode ?? false)
                        {
                            LogError($"Failed to convert question data: {e.Message}");
                        }
                    }
                }

                if (questionDict != null)
                {
                    // Obtenir la langue actuelle
                    string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

                    // Extraire les données de la question (nouveau format uniquement)
                    string questionText = ExtractLocalizedText(questionDict, "questionText", lang);
                    var options = ExtractLocalizedList(questionDict, "options", lang);

                    // Vérifier le mode de sélection (nouveau format uniquement)
                    isMultipleChoice = questionDict.ContainsKey("isMultipleChoice") && questionDict["isMultipleChoice"] is bool b && b;

                    // Gérer les réponses correctes selon le mode
                    if (isMultipleChoice)
                    {
                        correctAnswerIndexes.Clear();
                        if (questionDict.ContainsKey("correctAnswers"))
                        {
                            var correctAnswers = questionDict["correctAnswers"];
                            LogDebug($"[SEQ MULTIPLE CHOICE] correctAnswers type: {correctAnswers?.GetType()?.FullName ?? "null"}");

                            if (correctAnswers is Newtonsoft.Json.Linq.JArray jarray)
                            {
                                correctAnswerIndexes = jarray.Select(x => (int)(long)x).ToList();
                                LogDebug($"Parsed as JArray: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else if (correctAnswers is List<object> list)
                            {
                                correctAnswerIndexes = list.Select(x => Convert.ToInt32(x)).ToList();
                                LogDebug($"Parsed as List<object>: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else if (correctAnswers is object[] objArray)
                            {
                                correctAnswerIndexes = objArray.Select(x => Convert.ToInt32(x)).ToList();
                                LogDebug($"Parsed as object[]: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else if (correctAnswers is string str)
                            {
                                correctAnswerIndexes = str.Split(',').Select(x => int.Parse(x.Trim())).ToList();
                                LogDebug($"Parsed as string: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else if (correctAnswers is int[] intArray)
                            {
                                correctAnswerIndexes = intArray.ToList();
                                LogDebug($"Parsed as int[]: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else if (correctAnswers is long[] longArray)
                            {
                                correctAnswerIndexes = longArray.Select(x => (int)x).ToList();
                                LogDebug($"Parsed as long[]: {string.Join(", ", correctAnswerIndexes)}");
                            }
                            else
                            {
                                // Fallback : essayer de sérialiser/désérialiser
                                LogWarning($"Unhandled correctAnswers type: {correctAnswers?.GetType()?.FullName}, attempting JSON conversion");
                                try
                                {
                                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(correctAnswers);
                                    correctAnswerIndexes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(json);
                                    LogDebug($"Parsed via JSON fallback: {string.Join(", ", correctAnswerIndexes)}");
                                }
                                catch (System.Exception e)
                                {
                                    LogError($"Failed to parse correctAnswers: {e.Message}");
                                }
                            }

                            LogDebug($"Final correctAnswers for seq multiple choice: {string.Join(", ", correctAnswerIndexes)}");
                        }
                    }
                    else
                    {
                        // Pour réponse unique - lire depuis correctAnswers[0]
                        if (questionDict.ContainsKey("correctAnswers"))
                        {
                            var correctAnswers = questionDict["correctAnswers"];
                            if (correctAnswers is Newtonsoft.Json.Linq.JArray jarray && jarray.Count > 0)
                            {
                                correctAnswerIndex = (int)(long)jarray[0];
                            }
                            else if (correctAnswers is List<object> list && list.Count > 0)
                            {
                                correctAnswerIndex = Convert.ToInt32(list[0]);
                            }
                            else if (correctAnswers is int[] intArray && intArray.Length > 0)
                            {
                                correctAnswerIndex = intArray[0];
                            }
                        }
                    }

                    currentFeedback = ExtractLocalizedText(questionDict, "feedback", lang);
                    currentIncorrectFeedback = ExtractLocalizedText(questionDict, "incorrectFeedback", lang);

                    // Mettre à jour l'UI
                    questionLabel.text = questionText;
                    currentQuestionKey = currentKey; // Stocker la clé pour le tracking

                    // Mettre à jour le label d'instruction
                    var instructionLabel = modalContainer?.Q<Label>("instruction-label");
                    if (instructionLabel != null)
                    {
                        if (isMultipleChoice)
                        {
                            instructionLabel.text = lang == "fr"
                                ? "Vous pouvez sélectionner plusieurs réponses"
                                : "You can select multiple answers";
                        }
                        else
                        {
                            instructionLabel.text = lang == "fr"
                                ? "Sélectionnez une seule réponse"
                                : "Select only one answer";
                        }
                    }

                    // Clear options container et recréer les options
                    optionsContainer.Clear();
                    for (int i = 0; i < options.Count; i++)
                    {
                        int index = i;
                        var optionButton = CreateOptionButton(options[i], index);
                        optionsContainer.Add(optionButton);
                    }

                    // Réinitialiser les sélections
                    selectedAnswerIndex = -1;
                    selectedAnswerIndexes.Clear();

                    // Réinitialiser le feedback
                    feedbackContainer.style.display = DisplayStyle.None;

                    // Réinitialiser le bouton valider
                    validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Valider" : "Validate";
                    validateButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
                    validateButton.clicked -= NextQuestion;
                    validateButton.clicked -= ValidateAnswer;
                    validateButton.clicked += ValidateAnswer;

                    // Initialiser le tracking pour cette question
                    InitializeQuestionTracking();
                }
            }
        }

        private void CreateSingleQuestionUI(string questionText, List<string> options, string type, string feedback, string incorrectFeedback)
        {
            currentFeedback = feedback;
            currentIncorrectFeedback = incorrectFeedback;

            // Clear root
            rootElement.Clear();

            // Container modal
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de question
            var questionBox = new VisualElement();
            questionBox.style.width = 700;
            questionBox.style.maxWidth = Length.Percent(90);
            questionBox.style.maxHeight = Length.Percent(80);
            questionBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            questionBox.style.overflow = Overflow.Hidden; // Cacher le dépassement
            questionBox.style.borderTopLeftRadius = 25;
            questionBox.style.borderTopRightRadius = 25;
            questionBox.style.borderBottomLeftRadius = 25;
            questionBox.style.borderBottomRightRadius = 25;
            questionBox.style.paddingTop = 40;
            questionBox.style.paddingBottom = 40;
            questionBox.style.paddingLeft = 40;
            questionBox.style.paddingRight = 40;

            // Texte de la question
            questionLabel = new Label(questionText);
            questionLabel.style.fontSize = 24;
            questionLabel.style.color = Color.white;
            questionLabel.style.marginBottom = 35;
            questionLabel.style.whiteSpace = WhiteSpace.Normal;
            questionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            questionBox.Add(questionLabel);

            // Container des options avec scroll
            optionsContainer = new ScrollView();
            optionsContainer.style.marginBottom = 30;
            optionsContainer.style.maxHeight = 400; // Hauteur max avant scroll
            optionsContainer.style.flexGrow = 1;

            // Créer les boutons d'options
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var optionButton = CreateOptionButton(options[i], index);
                optionsContainer.Add(optionButton);
            }

            questionBox.Add(optionsContainer);

            // Label d'instruction (choix unique/multiple) - APRÈS les options
            var instructionLabel = new Label();
            instructionLabel.name = "instruction-label";
            instructionLabel.style.fontSize = 14;
            instructionLabel.style.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            instructionLabel.style.marginTop = 5;
            instructionLabel.style.marginBottom = 15;
            instructionLabel.style.paddingTop = 8;
            instructionLabel.style.paddingBottom = 8;
            instructionLabel.style.paddingLeft = 12;
            instructionLabel.style.paddingRight = 12;
            instructionLabel.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.4f);
            instructionLabel.style.borderTopLeftRadius = 6;
            instructionLabel.style.borderTopRightRadius = 6;
            instructionLabel.style.borderBottomLeftRadius = 6;
            instructionLabel.style.borderBottomRightRadius = 6;
            instructionLabel.style.whiteSpace = WhiteSpace.Normal;
            instructionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            instructionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;

            // Mettre à jour le texte selon le mode (sans icônes)
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            if (isMultipleChoice)
            {
                instructionLabel.text = lang == "fr"
                    ? "Vous pouvez sélectionner plusieurs réponses"
                    : "You can select multiple answers";
            }
            else
            {
                instructionLabel.text = lang == "fr"
                    ? "Sélectionnez une seule réponse"
                    : "Select only one answer";
            }
            questionBox.Add(instructionLabel);

            // Zone de feedback (cachée au début)
            feedbackContainer = new VisualElement();
            feedbackContainer.name = "feedback-container";
            feedbackContainer.style.display = DisplayStyle.None;
            feedbackContainer.style.marginTop = 20;
            feedbackContainer.style.paddingTop = 20;
            feedbackContainer.style.paddingBottom = 20;
            feedbackContainer.style.paddingLeft = 20;
            feedbackContainer.style.paddingRight = 20;
            feedbackContainer.style.borderTopLeftRadius = 10;
            feedbackContainer.style.borderTopRightRadius = 10;
            feedbackContainer.style.borderBottomLeftRadius = 10;
            feedbackContainer.style.borderBottomRightRadius = 10;

            feedbackLabel = new Label();
            feedbackLabel.name = "feedback-text";
            feedbackLabel.style.fontSize = 18;
            feedbackLabel.style.color = Color.white;
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            feedbackContainer.Add(feedbackLabel);

            questionBox.Add(feedbackContainer);

            // Bouton Valider (désactivé par défaut jusqu'à sélection)
            validateButton = new Button(ValidateAnswer);
            validateButton.name = "validate-button";
            validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Valider" : "Validate";

            validateButton.style.height = 50;
            validateButton.style.fontSize = 18;
            validateButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            validateButton.style.color = Color.white;
            validateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            validateButton.style.borderTopLeftRadius = 10;
            validateButton.style.borderTopRightRadius = 10;
            validateButton.style.borderBottomLeftRadius = 10;
            validateButton.style.borderBottomRightRadius = 10;
            validateButton.style.marginTop = 20;
            validateButton.SetEnabled(false); // Désactivé par défaut
            validateButton.style.opacity = 0.5f;

            questionBox.Add(validateButton);

            modalContainer.Add(questionBox);
            rootElement.Add(modalContainer);
        }

        VisualElement CreateOptionButton(string text, int index)
        {
            // Container pour l'option (contiendra le checkbox/radio + texte)
            var optionContainer = new VisualElement();
            optionContainer.style.flexDirection = FlexDirection.Row;
            optionContainer.style.alignItems = Align.Center;
            optionContainer.style.marginBottom = 12;
            optionContainer.style.paddingTop = 12;
            optionContainer.style.paddingBottom = 12;
            optionContainer.style.paddingLeft = 15;
            optionContainer.style.paddingRight = 15;
            optionContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            optionContainer.style.minHeight = 50; // Hauteur minimum pour éviter l'écrasement
            optionContainer.style.borderTopLeftRadius = 10;
            optionContainer.style.borderTopRightRadius = 10;
            optionContainer.style.borderBottomLeftRadius = 10;
            optionContainer.style.borderBottomRightRadius = 10;
            optionContainer.style.borderTopWidth = 2;
            optionContainer.style.borderBottomWidth = 2;
            optionContainer.style.borderLeftWidth = 2;
            optionContainer.style.borderRightWidth = 2;
            optionContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            optionContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            optionContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            optionContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            optionContainer.style.cursor = StyleKeyword.Auto;
            optionContainer.pickingMode = PickingMode.Position;
            optionContainer.name = $"option-{index}";

            // Indicateur visuel (cercle pour radio, carré pour checkbox)
            var indicator = new VisualElement();
            indicator.name = "indicator";
            indicator.style.width = 24;
            indicator.style.height = 24;
            indicator.style.marginRight = 12;
            indicator.style.borderTopWidth = 2;
            indicator.style.borderBottomWidth = 2;
            indicator.style.borderLeftWidth = 2;
            indicator.style.borderRightWidth = 2;
            indicator.style.borderTopColor = Color.white;
            indicator.style.borderBottomColor = Color.white;
            indicator.style.borderLeftColor = Color.white;
            indicator.style.borderRightColor = Color.white;
            indicator.style.backgroundColor = Color.clear;

            if (isMultipleChoice)
            {
                // Checkbox (carré)
                indicator.style.borderTopLeftRadius = 4;
                indicator.style.borderTopRightRadius = 4;
                indicator.style.borderBottomLeftRadius = 4;
                indicator.style.borderBottomRightRadius = 4;
            }
            else
            {
                // Radio button (cercle)
                indicator.style.borderTopLeftRadius = 12;
                indicator.style.borderTopRightRadius = 12;
                indicator.style.borderBottomLeftRadius = 12;
                indicator.style.borderBottomRightRadius = 12;
            }

            // Texte de l'option
            var label = new Label(text);
            label.style.fontSize = 18;
            label.style.color = Color.white;
            label.style.flexGrow = 1;
            label.style.flexShrink = 1;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Clip;

            optionContainer.Add(indicator);
            optionContainer.Add(label);

            // Event click
            optionContainer.RegisterCallback<ClickEvent>((evt) => {
                if (!hasAnswered)
                {
                    SelectOption(index);
                }
            });

            // Hover effect
            optionContainer.RegisterCallback<MouseEnterEvent>((evt) => {
                if (!hasAnswered)
                {
                    optionContainer.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                    optionContainer.style.cursor = StyleKeyword.Auto;
                }
            });

            optionContainer.RegisterCallback<MouseLeaveEvent>((evt) => {
                if (!hasAnswered && !optionContainer.ClassListContains("selected"))
                {
                    optionContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
                }
            });

            return optionContainer;
        }

        void SelectOption(int index)
        {
            if (hasAnswered) return;

            // Masquer le feedback d'erreur quand l'utilisateur change sa sélection
            if (feedbackContainer != null && feedbackContainer.style.display == DisplayStyle.Flex)
            {
                feedbackContainer.style.display = DisplayStyle.None;
                // Réinitialiser le feedback visuel sur les options
                ResetOptionsVisualFeedback();
            }

            if (isMultipleChoice)
            {
                // Mode checkbox - peut sélectionner/désélectionner plusieurs options
                if (selectedAnswerIndexes.Contains(index))
                {
                    selectedAnswerIndexes.Remove(index);
                }
                else
                {
                    selectedAnswerIndexes.Add(index);
                }
            }
            else
            {
                // Mode radio - une seule sélection
                selectedAnswerIndex = index;
                selectedAnswerIndexes.Clear();
                selectedAnswerIndexes.Add(index);
            }

            // Mettre à jour l'UI
            UpdateOptionsUI();

            // Activer le bouton Continuer dès qu'une sélection valide est faite
            bool hasValidSelection = isMultipleChoice ? selectedAnswerIndexes.Count > 0 : selectedAnswerIndex >= 0;
            if (validateButton != null && hasValidSelection)
            {
                validateButton.SetEnabled(true);
                validateButton.style.opacity = 1f;
            }
        }

        void UpdateOptionsUI()
        {
            var allOptions = optionsContainer.Query<VisualElement>().Build().ToList();

            for (int i = 0; i < allOptions.Count; i++)
            {
                var option = optionsContainer.Q<VisualElement>($"option-{i}");
                if (option != null)
                {
                    var indicator = option.Q<VisualElement>("indicator");
                    bool isSelected = isMultipleChoice ? selectedAnswerIndexes.Contains(i) : selectedAnswerIndex == i;

                    if (isSelected)
                    {
                        option.AddToClassList("selected");
                        option.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                        option.style.borderTopColor = new Color(0.2f, 0.6f, 1f, 1f);
                        option.style.borderBottomColor = new Color(0.2f, 0.6f, 1f, 1f);
                        option.style.borderLeftColor = new Color(0.2f, 0.6f, 1f, 1f);
                        option.style.borderRightColor = new Color(0.2f, 0.6f, 1f, 1f);

                        // Remplir l'indicateur
                        indicator.style.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f);
                    }
                    else
                    {
                        option.RemoveFromClassList("selected");
                        option.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
                        option.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                        option.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                        option.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                        option.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);

                        // Vider l'indicateur
                        indicator.style.backgroundColor = Color.clear;
                    }
                }
            }
        }

        private bool isValidating = false; // Protection contre les doubles clics

        /// <summary>
        /// Réinitialise le feedback visuel sur les options (retire les checkmarks/crosses et les couleurs de feedback)
        /// </summary>
        void ResetOptionsVisualFeedback()
        {
            var allOptionElements = optionsContainer.Query<VisualElement>().Where(e => e.name != null && e.name.StartsWith("option-")).ToList();

            for (int i = 0; i < allOptionElements.Count; i++)
            {
                var option = optionsContainer.Q<VisualElement>($"option-{i}");
                if (option == null) continue;

                // Retirer les checkmarks/crossmarks si présents
                var checkmark = option.Q<Label>("checkmark");
                if (checkmark != null)
                {
                    option.Remove(checkmark);
                }

                var crossmark = option.Q<Label>("crossmark");
                if (crossmark != null)
                {
                    option.Remove(crossmark);
                }
            }

            // Mettre à jour l'affichage des options (va appliquer les couleurs par défaut)
            UpdateOptionsUI();
        }

        /// <summary>
        /// Affiche un feedback visuel sur les options après validation
        /// Colore les bonnes réponses en vert et les mauvaises sélections en rouge
        /// </summary>
        void ShowAnswerFeedback(bool userAnsweredCorrectly)
        {
            // Parcourir toutes les options pour les colorer
            int optionCount = isMultipleChoice ? correctAnswerIndexes.Count : 1;
            // Pour obtenir le nombre total d'options, on utilise le nombre d'enfants dans optionsContainer
            var allOptionElements = optionsContainer.Query<VisualElement>().Where(e => e.name != null && e.name.StartsWith("option-")).ToList();

            for (int i = 0; i < allOptionElements.Count; i++)
            {
                var option = optionsContainer.Q<VisualElement>($"option-{i}");
                if (option == null) continue;

                var indicator = option.Q<VisualElement>("indicator");
                if (indicator == null) continue;

                // Déterminer si cette option est correcte
                bool isCorrectOption = isMultipleChoice
                    ? correctAnswerIndexes.Contains(i)
                    : (i == correctAnswerIndex);

                // Déterminer si cette option a été sélectionnée par l'utilisateur
                bool isUserSelected = isMultipleChoice
                    ? selectedAnswerIndexes.Contains(i)
                    : (i == selectedAnswerIndex);

                // Appliquer le style en fonction du statut
                if (isCorrectOption)
                {
                    // Option correcte - toujours afficher en vert
                    option.style.backgroundColor = new Color(0.1f, 0.5f, 0.3f, 0.4f);
                    option.style.borderTopColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    option.style.borderBottomColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    option.style.borderLeftColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    option.style.borderRightColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    indicator.style.backgroundColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    indicator.style.borderTopColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    indicator.style.borderBottomColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    indicator.style.borderLeftColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    indicator.style.borderRightColor = new Color(0.1f, 0.8f, 0.4f, 1f);

                    // Ajouter un label "✓" pour indiquer que c'est correct
                    var checkmark = option.Q<Label>("checkmark");
                    if (checkmark == null)
                    {
                        checkmark = new Label("✓");
                        checkmark.name = "checkmark";
                        checkmark.style.fontSize = 20;
                        checkmark.style.color = new Color(0.1f, 0.8f, 0.4f, 1f);
                        checkmark.style.unityFontStyleAndWeight = FontStyle.Bold;
                        checkmark.style.marginLeft = 10;
                        option.Add(checkmark);
                    }
                }
                else if (isUserSelected)
                {
                    // Option incorrecte sélectionnée par l'utilisateur - afficher en rouge
                    option.style.backgroundColor = new Color(0.5f, 0.1f, 0.1f, 0.4f);
                    option.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    option.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    option.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    option.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    indicator.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    indicator.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    indicator.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    indicator.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    indicator.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);

                    // Ajouter un label "✗" pour indiquer que c'est incorrect
                    var crossmark = option.Q<Label>("crossmark");
                    if (crossmark == null)
                    {
                        crossmark = new Label("✗");
                        crossmark.name = "crossmark";
                        crossmark.style.fontSize = 20;
                        crossmark.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                        crossmark.style.unityFontStyleAndWeight = FontStyle.Bold;
                        crossmark.style.marginLeft = 10;
                        option.Add(crossmark);
                    }
                }
                else
                {
                    // Option non sélectionnée et incorrecte - griser légèrement
                    option.style.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.6f);
                    option.style.borderTopColor = new Color(0.25f, 0.25f, 0.28f, 0.8f);
                    option.style.borderBottomColor = new Color(0.25f, 0.25f, 0.28f, 0.8f);
                    option.style.borderLeftColor = new Color(0.25f, 0.25f, 0.28f, 0.8f);
                    option.style.borderRightColor = new Color(0.25f, 0.25f, 0.28f, 0.8f);
                    indicator.style.backgroundColor = Color.clear;
                }
            }
        }

        void ValidateAnswer()
        {
            if (hasAnswered) return;

            // Protection contre les doubles appels rapides
            if (isValidating) return;
            isValidating = true;

            // Enregistrer la tentative dans l'analytics
            if (currentQuestionData != null)
            {
                var attemptIndexes = isMultipleChoice ? selectedAnswerIndexes : new List<int> { selectedAnswerIndex };
                currentQuestionData.AddUserAttempt(attemptIndexes);
                // IncrementCurrentInteractionAttempts incrémente le compteur d'attempts
                TrainingAnalytics.Instance?.IncrementCurrentInteractionAttempts();

                // Debug pour vérifier le nombre de tentatives
                LogDebug($"Attempt #{TrainingAnalytics.Instance?.GetCurrentInteraction()?.attempts} for question");
            }

            bool isCorrect = false;

            if (isMultipleChoice)
            {
                // Pour les réponses multiples, vérifier si les sélections correspondent exactement
                if (selectedAnswerIndexes.Count == 0) return;

                selectedAnswerIndexes.Sort();
                correctAnswerIndexes.Sort();

                if (ContentDisplayManager.Instance?.DebugMode ?? false)
                {
                    LogDebug($"Selected answers: {string.Join(", ", selectedAnswerIndexes)}");
                    LogDebug($"Correct answers: {string.Join(", ", correctAnswerIndexes)}");
                }

                isCorrect = selectedAnswerIndexes.Count == correctAnswerIndexes.Count &&
                           selectedAnswerIndexes.SequenceEqual(correctAnswerIndexes);
            }
            else
            {
                // Pour réponse unique
                if (selectedAnswerIndex < 0) return;
                isCorrect = selectedAnswerIndex == correctAnswerIndex;
            }

            // Afficher le feedback visuel sur les options
            ShowAnswerFeedback(isCorrect);

            // Afficher le feedback textuel seulement s'il n'est pas vide
            string feedbackText = isCorrect ? currentFeedback : currentIncorrectFeedback;
            if (!string.IsNullOrWhiteSpace(feedbackText))
            {
                feedbackContainer.style.display = DisplayStyle.Flex;
                feedbackLabel.text = feedbackText;
            }
            else
            {
                feedbackContainer.style.display = DisplayStyle.None;
            }

            if (isCorrect)
            {
                hasAnswered = true;

                // Terminer le tracking de cette question avec succès
                if (currentQuestionData != null)
                {
                    // Score = 100 seulement si correct du premier coup, sinon 0
                    currentQuestionData.finalScore = currentQuestionData.firstAttemptCorrect ? 100f : 0f;
                    // Mettre à jour les données avant de terminer
                    if (TrainingAnalytics.Instance != null)
                    {
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", currentQuestionData.finalScore);
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("userAnswers", currentQuestionData.userAnswers);
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("firstAttemptCorrect", currentQuestionData.firstAttemptCorrect);
                        // FIX: Success = true car la réponse finale est correcte (même si pas du premier coup)
                        // Le score reste à 0 si firstAttemptCorrect = false, mais on compte l'interaction comme réussie
                        TrainingAnalytics.Instance.EndCurrentInteraction(true);
                    }
                }
                feedbackContainer.style.backgroundColor = new Color(0.1f, 0.6f, 0.3f, 0.3f);
                feedbackContainer.style.borderTopWidth = 2;
                feedbackContainer.style.borderBottomWidth = 2;
                feedbackContainer.style.borderLeftWidth = 2;
                feedbackContainer.style.borderRightWidth = 2;
                feedbackContainer.style.borderTopColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                feedbackContainer.style.borderBottomColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                feedbackContainer.style.borderLeftColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                feedbackContainer.style.borderRightColor = new Color(0.1f, 0.8f, 0.4f, 1f);

                // Si on a plusieurs questions, passer à la suivante
                if (questionKeys != null && questionKeys.Count > 1)
                {
                    // Afficher un bouton "Question suivante" au lieu de valider
                    validateButton.text = currentQuestionIndex < questionKeys.Count - 1
                        ? (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Question suivante" : "Next Question")
                        : (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Terminer" : "Finish");
                    validateButton.clicked -= ValidateAnswer;
                    validateButton.clicked += NextQuestion;
                }
                else
                {
                    // Question unique - changer le bouton en "Continuer"
                    validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Continuer" : "Continue";
                    validateButton.clicked -= ValidateAnswer;
                    validateButton.clicked += () => {
                        OnCompleted?.Invoke(currentObjectId, true);
                        Close();
                    };
                }
            }
            else
            {
                // Réponse incorrecte - afficher feedback d'erreur et bloquer
                hasAnswered = true;

                // Terminer le tracking de cette question avec échec (mais ne pas compter comme erreur totale si retry)
                if (currentQuestionData != null)
                {
                    // Score = 0 car pas correct du premier coup
                    currentQuestionData.finalScore = 0f;
                    // Mettre à jour les données avant de terminer
                    if (TrainingAnalytics.Instance != null)
                    {
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", currentQuestionData.finalScore);
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("userAnswers", currentQuestionData.userAnswers);
                        TrainingAnalytics.Instance.AddDataToCurrentInteraction("firstAttemptCorrect", false);
                        // On marque success = true quand même pour ne pas bloquer la progression
                        TrainingAnalytics.Instance.EndCurrentInteraction(true);
                    }
                }

                feedbackContainer.style.backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.3f);
                feedbackContainer.style.borderTopWidth = 2;
                feedbackContainer.style.borderBottomWidth = 2;
                feedbackContainer.style.borderLeftWidth = 2;
                feedbackContainer.style.borderRightWidth = 2;
                feedbackContainer.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);

                // Changer le bouton en "Suivant" - pas de retry
                if (questionKeys != null && questionKeys.Count > 1)
                {
                    // Afficher un bouton "Question suivante" au lieu de valider
                    validateButton.text = currentQuestionIndex < questionKeys.Count - 1
                        ? (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Question suivante" : "Next Question")
                        : (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Terminer" : "Finish");
                    validateButton.clicked -= ValidateAnswer;
                    validateButton.clicked += NextQuestion;
                }
                else
                {
                    // Question unique - changer le bouton en "Continuer"
                    validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Continuer" : "Continue";
                    validateButton.clicked -= ValidateAnswer;
                    validateButton.clicked += () => {
                        OnCompleted?.Invoke(currentObjectId, true);
                        Close();
                    };
                }
            }
        }

        /// <summary>
        /// Nouvelle fonction: Continue sans validation ni feedback
        /// Enregistre la réponse et passe directement à la suite
        /// </summary>
        void ContinueToNext()
        {
            if (hasAnswered) return;

            // Vérifier qu'une sélection a été faite
            if (isMultipleChoice)
            {
                if (selectedAnswerIndexes.Count == 0) return;
            }
            else
            {
                if (selectedAnswerIndex < 0) return;
            }

            hasAnswered = true;

            // Enregistrer la réponse dans les analytics
            if (currentQuestionData != null)
            {
                var attemptIndexes = isMultipleChoice ? selectedAnswerIndexes : new List<int> { selectedAnswerIndex };
                currentQuestionData.AddUserAttempt(attemptIndexes);

                // Vérifier si la réponse est correcte pour les analytics
                bool isCorrect = false;
                if (isMultipleChoice)
                {
                    selectedAnswerIndexes.Sort();
                    correctAnswerIndexes.Sort();
                    isCorrect = selectedAnswerIndexes.Count == correctAnswerIndexes.Count &&
                               selectedAnswerIndexes.SequenceEqual(correctAnswerIndexes);
                }
                else
                {
                    isCorrect = selectedAnswerIndex == correctAnswerIndex;
                }

                // Enregistrer le score dans analytics
                currentQuestionData.finalScore = isCorrect ? 100f : 0f;

                if (TrainingAnalytics.Instance != null)
                {
                    TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", currentQuestionData.finalScore);
                    TrainingAnalytics.Instance.AddDataToCurrentInteraction("userAnswers", currentQuestionData.userAnswers);
                    TrainingAnalytics.Instance.AddDataToCurrentInteraction("firstAttemptCorrect", isCorrect);
                    TrainingAnalytics.Instance.EndCurrentInteraction(isCorrect);
                }
            }

            // Passer à la question suivante ou terminer (sans feedback visuel)
            if (questionKeys != null && questionKeys.Count > 1)
            {
                // Plusieurs questions: passer à la suivante
                currentQuestionIndex++;
                if (currentQuestionIndex >= questionKeys.Count)
                {
                    // Toutes les questions terminées
                    OnCompleted?.Invoke(currentObjectId, true);
                    Close();
                }
                else
                {
                    // Afficher la question suivante
                    DisplayCurrentQuestion();
                }
            }
            else
            {
                // Question unique: terminer
                OnCompleted?.Invoke(currentObjectId, true);
                Close();
            }
        }

        void NextQuestion()
        {
            // Réinitialiser le flag de validation pour la prochaine question
            isValidating = false;
            currentQuestionIndex++;
            if (currentQuestionIndex >= questionKeys.Count)
            {
                // Toutes les questions terminées
                OnCompleted?.Invoke(currentObjectId, true);
                Close();
            }
            else
            {
                // Afficher la question suivante
                DisplayCurrentQuestion();
            }
        }

        public void Close()
        {
            // Réinitialiser tous les flags
            isValidating = false;
            hasAnswered = false;

            // Débloquer les contrôles du personnage
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
            {
                character.SetControlsEnabled(true);
            }

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        void OnDestroy()
        {
            // Unsubscribe from language changes
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        /// <summary>
        /// Called when language changes - updates question text and options
        /// </summary>
        void OnLanguageChanged(string newLanguage)
        {
            // Don't update if already answered or if no question data stored
            if (hasAnswered || currentQuestionData_Raw == null)
                return;

            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Language changed to {newLanguage}, updating question display");
            }

            // Extract updated texts with new language
            string questionText = ExtractLocalizedText(currentQuestionData_Raw, "questionText", newLanguage);
            List<string> options = ExtractLocalizedList(currentQuestionData_Raw, "options", newLanguage);

            // Update UI elements
            if (questionLabel != null)
            {
                questionLabel.text = questionText;
            }

            // Update options
            if (optionsContainer != null && options.Count > 0)
            {
                var optionButtons = optionsContainer.Query<Button>().ToList();
                for (int i = 0; i < options.Count && i < optionButtons.Count; i++)
                {
                    var button = optionButtons[i];
                    var label = button.Q<Label>();
                    if (label != null)
                    {
                        label.text = options[i];
                    }
                }
            }

            // Update validate button text
            if (validateButton != null)
            {
                validateButton.text = newLanguage == "fr" ? "Valider" : "Validate";
            }
        }

        // Méthodes utilitaires pour extraire les données
        string ExtractLocalizedText(Dictionary<string, object> data, string key, string language)
        {
            // Format: key: { "en": "...", "fr": "..." }
            if (data.ContainsKey(key))
            {
                var value = data[key];

                // Handle JObject (nested localization)
                if (value is JObject jobj)
                {
                    if (jobj.ContainsKey(language))
                    {
                        return jobj[language]?.ToString() ?? "";
                    }
                    if (jobj.ContainsKey("en"))
                    {
                        return jobj["en"]?.ToString() ?? "";
                    }
                }
                // Handle Dictionary<string, object> (nested localization)
                else if (value is Dictionary<string, object> dict)
                {
                    if (dict.ContainsKey(language))
                    {
                        return dict[language]?.ToString() ?? "";
                    }
                    if (dict.ContainsKey("en"))
                    {
                        return dict["en"]?.ToString() ?? "";
                    }
                }
                // Handle direct string value (non-localized)
                else if (value is string str)
                {
                    return str;
                }
            }

            return "";
        }

        List<string> ExtractLocalizedList(Dictionary<string, object> data, string key, string language)
        {
            var result = new List<string>();

            // Format: key: { "en": [...], "fr": [...] }
            if (data.ContainsKey(key))
            {
                var value = data[key];

                // Handle JObject with nested arrays
                if (value is JObject jobj)
                {
                    JArray arrayData = null;
                    if (jobj.ContainsKey(language))
                    {
                        arrayData = jobj[language] as JArray;
                    }
                    else if (jobj.ContainsKey("en"))
                    {
                        arrayData = jobj["en"] as JArray;
                    }

                    if (arrayData != null)
                    {
                        foreach (var item in arrayData)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                }
                // Handle Dictionary with nested arrays
                else if (value is Dictionary<string, object> dict)
                {
                    object arrayData = null;
                    if (dict.ContainsKey(language))
                    {
                        arrayData = dict[language];
                    }
                    else if (dict.ContainsKey("en"))
                    {
                        arrayData = dict["en"];
                    }

                    if (arrayData is JArray jarray)
                    {
                        foreach (var item in jarray)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                    else if (arrayData is List<object> list)
                    {
                        foreach (var item in list)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                }
                // Handle direct array (non-localized)
                else if (value is JArray jarray)
                {
                    foreach (var item in jarray)
                    {
                        result.Add(item?.ToString() ?? "");
                    }
                }
                else if (value is List<object> list)
                {
                    foreach (var item in list)
                    {
                        result.Add(item?.ToString() ?? "");
                    }
                }
            }

            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"ExtractLocalizedList for '{key}' in '{language}': found {result.Count} items");
            }

            return result;
        }

        int ExtractInt(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key))
            {
                if (data[key] is int intValue) return intValue;
                if (data[key] is long longValue) return (int)longValue;
                if (data[key] is float floatValue) return (int)floatValue;
                if (data[key] is double doubleValue) return (int)doubleValue;
                if (int.TryParse(data[key]?.ToString(), out int parsed)) return parsed;
            }
            return 0;
        }

        string ExtractString(Dictionary<string, object> data, string key)
        {
            return data.ContainsKey(key) ? data[key]?.ToString() ?? "" : "";
        }

        // Méthode pour initialiser le tracking analytics
        void InitializeQuestionTracking()
        {
            if (TrainingAnalytics.Instance == null)
            {
                LogWarning("TrainingAnalytics not available - creating instance");
                var analyticsGO = new GameObject("TrainingAnalytics");
                analyticsGO.AddComponent<Analytics.TrainingAnalytics>();
            }

            // Créer les données de la question avec clés uniquement (pas de texte)
            currentQuestionData = new QuestionInteractionData();
            currentQuestionData.questionKey = currentQuestionKey; // Clé pour jointure avec metadata
            currentQuestionData.objectId = currentObjectId; // ObjectId pour retrouver dans metadata
            // IMPORTANT : Créer une COPIE de la liste pour éviter que les Clear() ultérieurs ne vident les données
            currentQuestionData.correctAnswers = isMultipleChoice ? new List<int>(correctAnswerIndexes) : new List<int> { correctAnswerIndex };

            // Démarrer l'interaction
            string questionId = $"{currentObjectId}_{currentQuestionKey}";

            var subtype = isMultipleChoice ? "multiple_choice" : "single_choice";

            LogDebug($"Initializing tracking for question: {questionId} (key: {currentQuestionKey})");
            TrainingAnalytics.Instance.TrackQuestionInteraction(currentObjectId, questionId, currentQuestionData);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[QuestionDisplayer] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[QuestionDisplayer] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[QuestionDisplayer] {message}");
        }
    }
}