using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Linq;
using WiseTwin.Analytics;

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
        private List<string> currentOptionTexts;
        private string currentQuestionText;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;

            // Si contentData contient déjà les données de question directement
            if (contentData.ContainsKey("text"))
            {
                // Question unique passée directement
                questionKeys = null;
                allObjectData = null;
                DisplaySingleQuestion(contentData);
            }
            else
            {
                // Potentiellement plusieurs questions
                allObjectData = contentData;
                questionKeys = new List<string>();

                // Chercher toutes les clés de questions (question_1, question_2, etc.)
                foreach (var key in contentData.Keys)
                {
                    if (key.StartsWith("question_"))
                    {
                        questionKeys.Add(key);
                    }
                }

                // Trier les questions par ordre numérique
                questionKeys.Sort((a, b) =>
                {
                    int numA = int.Parse(a.Replace("question_", ""));
                    int numB = int.Parse(b.Replace("question_", ""));
                    return numA.CompareTo(numB);
                });

                if (ContentDisplayManager.Instance?.DebugMode ?? false)
                {
                    LogDebug($"Found {questionKeys.Count} questions for {objectId}");
                }

                if (questionKeys.Count > 0)
                {
                    currentQuestionIndex = 0;
                    CreateQuestionUI();
                    DisplayCurrentQuestion();
                }
                else
                {
                    if (ContentDisplayManager.Instance?.DebugMode ?? false)
                    {
                        LogError($"No questions found for {objectId}");
                    }
                }
            }
        }

        private void DisplaySingleQuestion(Dictionary<string, object> contentData)
        {
            hasAnswered = false;
            isValidating = false; // Réinitialiser le flag de validation
            selectedAnswerIndex = -1;
            selectedAnswerIndexes.Clear();

            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Displaying single question for {currentObjectId}");
            }

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données de la question
            string questionText = ExtractLocalizedText(contentData, "text", lang);
            var options = ExtractLocalizedList(contentData, "options", lang);
            currentQuestionText = questionText;
            currentOptionTexts = options;

            // Vérifier le mode de sélection (single ou multiple)
            string selectionMode = contentData.ContainsKey("selectionMode")
                ? ExtractString(contentData, "selectionMode")
                : "single";
            isMultipleChoice = (selectionMode == "multiple");

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
                // Pour réponse unique
                correctAnswerIndex = ExtractInt(contentData, "correctAnswer");
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

            // Bouton fermer (X)
            var closeButton = new Button(() => Close());
            closeButton.text = "✕";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 15;
            closeButton.style.right = 15;
            closeButton.style.width = 35;
            closeButton.style.height = 35;
            closeButton.style.fontSize = 24;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 17;
            closeButton.style.borderTopRightRadius = 17;
            closeButton.style.borderBottomLeftRadius = 17;
            closeButton.style.borderBottomRightRadius = 17;
            questionBox.Add(closeButton);

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
            optionsContainer.style.marginBottom = 30;
            optionsContainer.style.maxHeight = 400; // Hauteur max avant scroll
            optionsContainer.style.flexGrow = 1;
            questionBox.Add(optionsContainer);

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

            // Bouton Valider
            validateButton = new Button(() => ValidateAnswer());
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

            string currentKey = questionKeys[currentQuestionIndex];
            if (ContentDisplayManager.Instance?.DebugMode ?? false)
            {
                LogDebug($"Displaying question {currentQuestionIndex + 1}/{questionKeys.Count}: {currentKey}");
            }

            // Mise à jour de l'indicateur de progression
            if (questionKeys.Count > 1)
            {
                progressLabel.text = $"Question {currentQuestionIndex + 1} / {questionKeys.Count}";
                progressLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                progressLabel.style.display = DisplayStyle.None;
            }

            if (allObjectData.ContainsKey(currentKey))
            {
                var questionData = allObjectData[currentKey];

                // Convertir en Dictionary si nécessaire
                Dictionary<string, object> questionDict = null;
                if (questionData is Dictionary<string, object> dict)
                {
                    questionDict = dict;
                }
                else if (questionData != null)
                {
                    try
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(questionData);
                        questionDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
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

                    // Extraire les données de la question
                    string questionText = ExtractLocalizedText(questionDict, "text", lang);
                    var options = ExtractLocalizedList(questionDict, "options", lang);

                    // Vérifier le mode de sélection pour chaque question
                    string selectionMode = questionDict.ContainsKey("selectionMode")
                        ? ExtractString(questionDict, "selectionMode")
                        : "single";
                    isMultipleChoice = (selectionMode == "multiple");

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
                        correctAnswerIndex = ExtractInt(questionDict, "correctAnswer");
                    }

                    currentFeedback = ExtractLocalizedText(questionDict, "feedback", lang);
                    currentIncorrectFeedback = ExtractLocalizedText(questionDict, "incorrectFeedback", lang);

                    // Mettre à jour l'UI
                    questionLabel.text = questionText;
                    currentQuestionText = questionText;
                    currentOptionTexts = options;

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

            // Bouton fermer (X)
            var closeButton = new Button(() => Close());
            closeButton.text = "✕";
            closeButton.style.position = Position.Absolute;
            closeButton.style.top = 15;
            closeButton.style.right = 15;
            closeButton.style.width = 35;
            closeButton.style.height = 35;
            closeButton.style.fontSize = 24;
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton.style.color = Color.white;
            closeButton.style.borderTopLeftRadius = 17;
            closeButton.style.borderTopRightRadius = 17;
            closeButton.style.borderBottomLeftRadius = 17;
            closeButton.style.borderBottomRightRadius = 17;
            questionBox.Add(closeButton);

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

            // Bouton Valider
            validateButton = new Button(() => ValidateAnswer());
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

            // Afficher le feedback
            feedbackContainer.style.display = DisplayStyle.Flex;
            feedbackLabel.text = isCorrect ? currentFeedback : currentIncorrectFeedback;

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
                        // Success = true seulement si correct du premier coup
                        TrainingAnalytics.Instance.EndCurrentInteraction(currentQuestionData.firstAttemptCorrect);
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
                feedbackContainer.style.backgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.3f);
                feedbackContainer.style.borderTopWidth = 2;
                feedbackContainer.style.borderBottomWidth = 2;
                feedbackContainer.style.borderLeftWidth = 2;
                feedbackContainer.style.borderRightWidth = 2;
                feedbackContainer.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                feedbackContainer.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);

                // Permettre de réessayer - changer le texte du bouton
                validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Réessayer" : "Try Again";
                validateButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.1f, 1f);

                // Réinitialiser le flag après un court délai pour permettre une nouvelle tentative
                validateButton.schedule.Execute(() => {
                    isValidating = false;
                }).ExecuteLater(300); // 300ms de délai
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

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        // Méthodes utilitaires pour extraire les données
        string ExtractLocalizedText(Dictionary<string, object> data, string key, string language)
        {
            if (!data.ContainsKey(key)) return "";

            var textData = data[key];

            // Si c'est directement une string
            if (textData is string simpleText) return simpleText;

            // Si c'est un Dictionary
            if (textData is Dictionary<string, object> localizedText)
            {
                if (localizedText.ContainsKey(language))
                    return localizedText[language]?.ToString() ?? "";
                if (localizedText.ContainsKey("en"))
                    return localizedText["en"]?.ToString() ?? "";
            }
            // Si c'est un JObject de Newtonsoft
            else if (textData != null && textData.GetType().FullName.Contains("JObject"))
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(textData);
                var localizedJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (localizedJObject != null)
                {
                    if (localizedJObject.ContainsKey(language))
                        return localizedJObject[language];
                    if (localizedJObject.ContainsKey("en"))
                        return localizedJObject["en"];
                }
            }

            return "";
        }

        List<string> ExtractLocalizedList(Dictionary<string, object> data, string key, string language)
        {
            var result = new List<string>();
            if (!data.ContainsKey(key)) return result;

            var listData = data[key];

            // Si c'est directement une liste
            if (listData is List<object> simpleList)
            {
                foreach (var item in simpleList)
                {
                    result.Add(item?.ToString() ?? "");
                }
            }
            // Si c'est un JArray de Newtonsoft
            else if (listData != null && listData.GetType().FullName.Contains("JArray"))
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
                var array = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
                if (array != null) result.AddRange(array);
            }
            // Si c'est un dictionnaire de langues
            else if (listData is Dictionary<string, object> localizedLists)
            {
                if (localizedLists.ContainsKey(language))
                {
                    var langData = localizedLists[language];
                    if (langData is List<object> langList)
                    {
                        foreach (var item in langList)
                        {
                            result.Add(item?.ToString() ?? "");
                        }
                    }
                    else if (langData != null && langData.GetType().FullName.Contains("JArray"))
                    {
                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(langData);
                        var array = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
                        if (array != null) result.AddRange(array);
                    }
                }
            }
            // Si c'est un JObject contenant les langues
            else if (listData != null && listData.GetType().FullName.Contains("JObject"))
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
                var localizedJObjectLists = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                if (localizedJObjectLists != null && localizedJObjectLists.ContainsKey(language))
                {
                    result.AddRange(localizedJObjectLists[language]);
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

            // Créer les données de la question
            currentQuestionData = new QuestionInteractionData();
            currentQuestionData.questionText = currentQuestionText;
            currentQuestionData.options = currentOptionTexts != null ? new List<string>(currentOptionTexts) : new List<string>();
            // IMPORTANT : Créer une COPIE de la liste pour éviter que les Clear() ultérieurs ne vident les données
            currentQuestionData.correctAnswers = isMultipleChoice ? new List<int>(correctAnswerIndexes) : new List<int> { correctAnswerIndex };

            // Démarrer l'interaction
            string questionId = currentQuestionIndex >= 0 && questionKeys != null
                ? $"{currentObjectId}_{questionKeys[currentQuestionIndex]}"
                : $"{currentObjectId}_question";

            var subtype = isMultipleChoice ? "multiple_choice" : "single_choice";

            LogDebug($"Initializing tracking for question: {questionId}");
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