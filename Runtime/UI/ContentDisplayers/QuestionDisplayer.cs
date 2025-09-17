using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour les questions (QCM, Vrai/Faux, etc.)
    /// </summary>
    public class QuestionDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;
        private int selectedAnswerIndex = -1;
        private int correctAnswerIndex;
        private bool hasAnswered = false;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;
            hasAnswered = false;
            selectedAnswerIndex = -1;

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données de la question
            string questionText = ExtractLocalizedText(contentData, "text", lang);
            var options = ExtractLocalizedList(contentData, "options", lang);
            correctAnswerIndex = ExtractInt(contentData, "correctAnswer");
            string feedback = ExtractLocalizedText(contentData, "feedback", lang);
            string incorrectFeedback = ExtractLocalizedText(contentData, "incorrectFeedback", lang);
            string questionType = ExtractString(contentData, "type");

            // Créer l'UI
            CreateQuestionUI(questionText, options, questionType, feedback, incorrectFeedback);
        }

        void CreateQuestionUI(string questionText, List<string> options, string type, string feedback, string incorrectFeedback)
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
            closeButton.style.borderTopWidth = 0;
            closeButton.style.borderBottomWidth = 0;
            closeButton.style.borderLeftWidth = 0;
            closeButton.style.borderRightWidth = 0;
            questionBox.Add(closeButton);

            // Texte de la question
            var questionLabel = new Label(questionText);
            questionLabel.style.fontSize = 24;
            questionLabel.style.color = Color.white;
            questionLabel.style.marginBottom = 35;
            questionLabel.style.whiteSpace = WhiteSpace.Normal;
            questionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            questionBox.Add(questionLabel);

            // Container des options
            var optionsContainer = new VisualElement();
            optionsContainer.style.marginBottom = 30;

            // Créer les boutons d'options
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var optionButton = CreateOptionButton(options[i], index, type == "true-false");
                optionsContainer.Add(optionButton);
            }

            questionBox.Add(optionsContainer);

            // Zone de feedback (cachée au début)
            var feedbackContainer = new VisualElement();
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

            var feedbackLabel = new Label();
            feedbackLabel.name = "feedback-text";
            feedbackLabel.style.fontSize = 18;
            feedbackLabel.style.color = Color.white;
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            feedbackContainer.Add(feedbackLabel);

            questionBox.Add(feedbackContainer);

            // Bouton Valider
            var validateButton = new Button(() => ValidateAnswer(feedback, incorrectFeedback));
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

        Button CreateOptionButton(string text, int index, bool isTrueFalse)
        {
            var button = new Button(() => SelectOption(index));
            button.name = $"option-{index}";
            button.text = text;
            button.style.marginBottom = 12;
            button.style.height = 50;
            button.style.fontSize = 18;
            button.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            button.style.color = Color.white;
            button.style.borderTopLeftRadius = 10;
            button.style.borderTopRightRadius = 10;
            button.style.borderBottomLeftRadius = 10;
            button.style.borderBottomRightRadius = 10;
            button.style.borderTopWidth = 2;
            button.style.borderBottomWidth = 2;
            button.style.borderLeftWidth = 2;
            button.style.borderRightWidth = 2;
            button.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            button.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            button.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            button.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            button.style.unityTextAlign = TextAnchor.MiddleCenter;

            // Hover effect
            button.RegisterCallback<MouseEnterEvent>((evt) => {
                if (!hasAnswered && !button.ClassListContains("selected"))
                {
                    button.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                }
            });

            button.RegisterCallback<MouseLeaveEvent>((evt) => {
                if (!hasAnswered && !button.ClassListContains("selected"))
                {
                    button.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
                }
            });

            return button;
        }

        void SelectOption(int index)
        {
            if (hasAnswered) return;

            selectedAnswerIndex = index;

            // Désélectionner tous les boutons
            var allOptions = rootElement.Query<Button>(className: null).Build();
            foreach (var option in allOptions)
            {
                if (option.name.StartsWith("option-"))
                {
                    option.RemoveFromClassList("selected");
                    option.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
                    option.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    option.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    option.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    option.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                }
            }

            // Sélectionner le bouton cliqué
            var selectedButton = rootElement.Q<Button>($"option-{index}");
            if (selectedButton != null)
            {
                selectedButton.AddToClassList("selected");
                selectedButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
                selectedButton.style.borderTopColor = new Color(0.1f, 0.8f, 0.6f, 1f);
                selectedButton.style.borderBottomColor = new Color(0.1f, 0.8f, 0.6f, 1f);
                selectedButton.style.borderLeftColor = new Color(0.1f, 0.8f, 0.6f, 1f);
                selectedButton.style.borderRightColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            }
        }

        void ValidateAnswer(string correctFeedback, string incorrectFeedback)
        {
            if (hasAnswered || selectedAnswerIndex < 0) return;

            hasAnswered = true;
            bool isCorrect = selectedAnswerIndex == correctAnswerIndex;

            // Afficher le feedback
            var feedbackContainer = rootElement.Q<VisualElement>("feedback-container");
            var feedbackText = rootElement.Q<Label>("feedback-text");

            if (feedbackContainer != null && feedbackText != null)
            {
                feedbackContainer.style.display = DisplayStyle.Flex;
                feedbackText.text = isCorrect ? correctFeedback : incorrectFeedback;

                if (isCorrect)
                {
                    feedbackContainer.style.backgroundColor = new Color(0.1f, 0.6f, 0.3f, 0.3f);
                    feedbackContainer.style.borderTopWidth = 2;
                    feedbackContainer.style.borderBottomWidth = 2;
                    feedbackContainer.style.borderLeftWidth = 2;
                    feedbackContainer.style.borderRightWidth = 2;
                    feedbackContainer.style.borderTopColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    feedbackContainer.style.borderBottomColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    feedbackContainer.style.borderLeftColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    feedbackContainer.style.borderRightColor = new Color(0.1f, 0.8f, 0.4f, 1f);
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
                }
            }

            // Changer le bouton en "Continuer"
            var validateButton = rootElement.Q<Button>("validate-button");
            if (validateButton != null)
            {
                validateButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Continuer" : "Continue";
                validateButton.clicked -= null;
                validateButton.clicked += () => {
                    OnCompleted?.Invoke(currentObjectId, isCorrect);
                    Close();
                };
            }

            // Déclencher l'événement de complétion
            OnCompleted?.Invoke(currentObjectId, isCorrect);
        }

        public void Close()
        {
            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        // Méthodes utilitaires pour extraire les données
        string ExtractLocalizedText(Dictionary<string, object> data, string key, string language)
        {
            if (!data.ContainsKey(key)) return "";

            var textData = data[key];
            if (textData is string simpleText) return simpleText;

            if (textData is Dictionary<string, object> localizedText)
            {
                if (localizedText.ContainsKey(language))
                    return localizedText[language]?.ToString() ?? "";
                if (localizedText.ContainsKey("en"))
                    return localizedText["en"]?.ToString() ?? "";
            }

            return "";
        }

        List<string> ExtractLocalizedList(Dictionary<string, object> data, string key, string language)
        {
            var result = new List<string>();
            if (!data.ContainsKey(key)) return result;

            var listData = data[key];

            if (listData is List<object> simpleList)
            {
                foreach (var item in simpleList)
                {
                    result.Add(item?.ToString() ?? "");
                }
            }
            else if (listData is Dictionary<string, object> localizedLists)
            {
                if (localizedLists.ContainsKey(language) && localizedLists[language] is List<object> langList)
                {
                    foreach (var item in langList)
                    {
                        result.Add(item?.ToString() ?? "");
                    }
                }
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
    }
}