using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour les procédures (étapes à suivre)
    /// </summary>
    public class ProcedureDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private int currentStepIndex = 0;
        private List<Dictionary<string, object>> steps;
        private bool[] stepCompleted;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;
            currentStepIndex = 0;

            // Obtenir la langue actuelle
            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";

            // Extraire les données de la procédure
            string title = ExtractLocalizedText(contentData, "title", lang);
            steps = ExtractSteps(contentData, "steps");
            stepCompleted = new bool[steps.Count];

            // Créer l'UI
            CreateProcedureUI(title);
        }

        void CreateProcedureUI(string title)
        {
            // Clear root
            rootElement.Clear();

            // Container modal
            var modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;
            modalContainer.pickingMode = PickingMode.Position;

            // Boîte de procédure
            var procedureBox = new VisualElement();
            procedureBox.style.width = 800;
            procedureBox.style.maxWidth = Length.Percent(90);
            procedureBox.style.height = 600;
            procedureBox.style.maxHeight = Length.Percent(85);
            procedureBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            procedureBox.style.borderTopLeftRadius = 25;
            procedureBox.style.borderTopRightRadius = 25;
            procedureBox.style.borderBottomLeftRadius = 25;
            procedureBox.style.borderBottomRightRadius = 25;
            procedureBox.style.flexDirection = FlexDirection.Column;

            // Header
            var header = CreateHeader(title);
            procedureBox.Add(header);

            // Progress bar
            var progressBar = CreateProgressBar();
            procedureBox.Add(progressBar);

            // Content area
            var contentArea = new ScrollView();
            contentArea.name = "content-area";
            contentArea.style.flexGrow = 1;
            contentArea.style.paddingTop = 20;
            contentArea.style.paddingBottom = 20;
            contentArea.style.paddingLeft = 40;
            contentArea.style.paddingRight = 40;
            procedureBox.Add(contentArea);

            // Footer avec boutons
            var footer = CreateFooter();
            procedureBox.Add(footer);

            modalContainer.Add(procedureBox);
            rootElement.Add(modalContainer);

            // Afficher la première étape
            DisplayStep(currentStepIndex);
        }

        VisualElement CreateHeader(string title)
        {
            var header = new VisualElement();
            header.style.paddingTop = 30;
            header.style.paddingBottom = 20;
            header.style.paddingLeft = 40;
            header.style.paddingRight = 40;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // Titre
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 28;
            titleLabel.style.color = Color.white;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(titleLabel);

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
            header.Add(closeButton);

            return header;
        }

        VisualElement CreateProgressBar()
        {
            var progressContainer = new VisualElement();
            progressContainer.name = "progress-container";
            progressContainer.style.paddingTop = 15;
            progressContainer.style.paddingBottom = 15;
            progressContainer.style.paddingLeft = 40;
            progressContainer.style.paddingRight = 40;
            progressContainer.style.borderBottomWidth = 1;
            progressContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            // Label d'étape
            var stepLabel = new Label();
            stepLabel.name = "step-label";
            stepLabel.style.fontSize = 16;
            stepLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            stepLabel.style.marginBottom = 10;
            stepLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            progressContainer.Add(stepLabel);

            // Barre de progression
            var progressBar = new VisualElement();
            progressBar.style.height = 8;
            progressBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);
            progressBar.style.borderTopLeftRadius = 4;
            progressBar.style.borderTopRightRadius = 4;
            progressBar.style.borderBottomLeftRadius = 4;
            progressBar.style.borderBottomRightRadius = 4;

            var progressFill = new VisualElement();
            progressFill.name = "progress-fill";
            progressFill.style.height = Length.Percent(100);
            progressFill.style.width = Length.Percent(0);
            progressFill.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            progressFill.style.borderTopLeftRadius = 4;
            progressFill.style.borderTopRightRadius = 4;
            progressFill.style.borderBottomLeftRadius = 4;
            progressFill.style.borderBottomRightRadius = 4;
            progressBar.Add(progressFill);

            progressContainer.Add(progressBar);
            return progressContainer;
        }

        VisualElement CreateFooter()
        {
            var footer = new VisualElement();
            footer.style.paddingTop = 20;
            footer.style.paddingBottom = 30;
            footer.style.paddingLeft = 40;
            footer.style.paddingRight = 40;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;

            // Bouton Précédent
            var prevButton = new Button(() => PreviousStep());
            prevButton.name = "prev-button";
            prevButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "◀ Précédent" : "◀ Previous";
            prevButton.style.width = 150;
            prevButton.style.height = 45;
            prevButton.style.fontSize = 16;
            prevButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            prevButton.style.color = Color.white;
            prevButton.style.borderTopLeftRadius = 10;
            prevButton.style.borderTopRightRadius = 10;
            prevButton.style.borderBottomLeftRadius = 10;
            prevButton.style.borderBottomRightRadius = 10;
            footer.Add(prevButton);

            // Checkbox de validation
            var validationContainer = new VisualElement();
            validationContainer.style.flexDirection = FlexDirection.Row;
            validationContainer.style.alignItems = Align.Center;

            var checkbox = new Toggle();
            checkbox.name = "step-checkbox";
            checkbox.style.marginRight = 10;
            checkbox.RegisterValueChangedCallback(evt => {
                stepCompleted[currentStepIndex] = evt.newValue;
                UpdateNextButtonState();
            });
            validationContainer.Add(checkbox);

            var checkLabel = new Label();
            checkLabel.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Étape complétée" : "Step completed";
            checkLabel.style.fontSize = 16;
            checkLabel.style.color = Color.white;
            validationContainer.Add(checkLabel);

            footer.Add(validationContainer);

            // Bouton Suivant
            var nextButton = new Button(() => NextStep());
            nextButton.name = "next-button";
            nextButton.text = LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Suivant ▶" : "Next ▶";
            nextButton.style.width = 150;
            nextButton.style.height = 45;
            nextButton.style.fontSize = 16;
            nextButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            nextButton.style.color = Color.white;
            nextButton.style.borderTopLeftRadius = 10;
            nextButton.style.borderTopRightRadius = 10;
            nextButton.style.borderBottomLeftRadius = 10;
            nextButton.style.borderBottomRightRadius = 10;
            footer.Add(nextButton);

            return footer;
        }

        void DisplayStep(int index)
        {
            if (index < 0 || index >= steps.Count) return;

            string lang = LocalizationManager.Instance?.CurrentLanguage ?? "en";
            var step = steps[index];

            // Mettre à jour le contenu
            var contentArea = rootElement.Q<ScrollView>("content-area");
            if (contentArea != null)
            {
                contentArea.Clear();

                // Titre de l'étape
                string stepTitle = ExtractLocalizedText(step, "title", lang);
                var titleLabel = new Label(stepTitle);
                titleLabel.style.fontSize = 22;
                titleLabel.style.color = new Color(0.1f, 0.8f, 0.6f, 1f);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.marginBottom = 20;
                contentArea.Add(titleLabel);

                // Description de l'étape
                string description = ExtractLocalizedText(step, "description", lang);
                var descLabel = new Label(description);
                descLabel.style.fontSize = 18;
                descLabel.style.color = Color.white;
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.marginBottom = 20;
                contentArea.Add(descLabel);

                // Point de validation
                string validation = ExtractLocalizedText(step, "validation", lang);
                if (!string.IsNullOrEmpty(validation))
                {
                    var validationBox = new VisualElement();
                    validationBox.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                    validationBox.style.paddingTop = 15;
                    validationBox.style.paddingBottom = 15;
                    validationBox.style.paddingLeft = 20;
                    validationBox.style.paddingRight = 20;
                    validationBox.style.borderTopLeftRadius = 10;
                    validationBox.style.borderTopRightRadius = 10;
                    validationBox.style.borderBottomLeftRadius = 10;
                    validationBox.style.borderBottomRightRadius = 10;
                    validationBox.style.borderLeftWidth = 3;
                    validationBox.style.borderLeftColor = new Color(0.8f, 0.6f, 0.1f, 1f);

                    var validationLabel = new Label("⚠️ " + validation);
                    validationLabel.style.fontSize = 16;
                    validationLabel.style.color = new Color(1f, 0.9f, 0.6f);
                    validationLabel.style.whiteSpace = WhiteSpace.Normal;
                    validationBox.Add(validationLabel);

                    contentArea.Add(validationBox);
                }
            }

            // Mettre à jour la progression
            UpdateProgress();

            // Mettre à jour la checkbox
            var checkbox = rootElement.Q<Toggle>("step-checkbox");
            if (checkbox != null)
            {
                checkbox.value = stepCompleted[index];
            }

            // Mettre à jour les boutons
            UpdateNavigationButtons();
        }

        void UpdateProgress()
        {
            // Label d'étape
            var stepLabel = rootElement.Q<Label>("step-label");
            if (stepLabel != null)
            {
                string text = LocalizationManager.Instance?.CurrentLanguage == "fr"
                    ? $"Étape {currentStepIndex + 1} sur {steps.Count}"
                    : $"Step {currentStepIndex + 1} of {steps.Count}";
                stepLabel.text = text;
            }

            // Barre de progression
            var progressFill = rootElement.Q<VisualElement>("progress-fill");
            if (progressFill != null)
            {
                float progress = (float)(currentStepIndex + 1) / steps.Count * 100f;
                progressFill.style.width = Length.Percent(progress);
            }
        }

        void UpdateNavigationButtons()
        {
            // Bouton précédent
            var prevButton = rootElement.Q<Button>("prev-button");
            if (prevButton != null)
            {
                prevButton.SetEnabled(currentStepIndex > 0);
                prevButton.style.opacity = currentStepIndex > 0 ? 1f : 0.5f;
            }

            // Bouton suivant
            var nextButton = rootElement.Q<Button>("next-button");
            if (nextButton != null)
            {
                bool isLastStep = currentStepIndex >= steps.Count - 1;
                nextButton.text = isLastStep
                    ? (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Terminer ✓" : "Complete ✓")
                    : (LocalizationManager.Instance?.CurrentLanguage == "fr" ? "Suivant ▶" : "Next ▶");

                UpdateNextButtonState();
            }
        }

        void UpdateNextButtonState()
        {
            var nextButton = rootElement.Q<Button>("next-button");
            if (nextButton != null)
            {
                bool canProceed = stepCompleted[currentStepIndex];
                nextButton.SetEnabled(canProceed);
                nextButton.style.opacity = canProceed ? 1f : 0.5f;
            }
        }

        void PreviousStep()
        {
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                DisplayStep(currentStepIndex);
            }
        }

        void NextStep()
        {
            if (!stepCompleted[currentStepIndex]) return;

            if (currentStepIndex < steps.Count - 1)
            {
                currentStepIndex++;
                DisplayStep(currentStepIndex);
            }
            else
            {
                // Procédure terminée
                bool allCompleted = true;
                foreach (bool completed in stepCompleted)
                {
                    if (!completed)
                    {
                        allCompleted = false;
                        break;
                    }
                }

                OnCompleted?.Invoke(currentObjectId, allCompleted);
                Close();
            }
        }

        public void Close()
        {
            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        // Méthodes utilitaires
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

        List<Dictionary<string, object>> ExtractSteps(Dictionary<string, object> data, string key)
        {
            var result = new List<Dictionary<string, object>>();

            if (data.ContainsKey(key) && data[key] is List<object> stepsList)
            {
                foreach (var step in stepsList)
                {
                    if (step is Dictionary<string, object> stepDict)
                    {
                        result.Add(stepDict);
                    }
                }
            }

            // Si pas d'étapes, créer une étape par défaut
            if (result.Count == 0)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, object>
                    {
                        ["en"] = "Step 1",
                        ["fr"] = "Étape 1"
                    },
                    ["description"] = new Dictionary<string, object>
                    {
                        ["en"] = "No procedure steps defined.",
                        ["fr"] = "Aucune étape de procédure définie."
                    }
                });
            }

            return result;
        }
    }
}