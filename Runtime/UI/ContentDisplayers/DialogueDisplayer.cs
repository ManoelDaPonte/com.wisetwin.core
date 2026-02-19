using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using WiseTwin.Analytics;

namespace WiseTwin.UI
{
    /// <summary>
    /// Afficheur spécialisé pour les dialogues interactifs (arbre de conversation avec PNJ).
    /// Style chat-bubble avec choix, feedback visuel, et tracking analytics.
    /// </summary>
    public class DialogueDisplayer : MonoBehaviour, IContentDisplayer
    {
        public event Action<string> OnClosed;
        public event Action<string, bool> OnCompleted;

        private string currentObjectId;
        private VisualElement rootElement;
        private VisualElement modalContainer;
        private VisualElement dialogueBox;
        private VisualElement contextContainer; // Previous dialogue context shown above choices
        private Label contextSpeakerLabel;
        private Label contextTextLabel;
        private Label speakerLabel;
        private Label textLabel;
        private VisualElement choicesContainer;
        private Button continueButton;

        // Dialogue state
        private DialogueTreeData dialogueTree;
        private DialogueNodeRuntime currentNode;
        private DialogueNodeRuntime lastDialogueNode; // Remember last dialogue for context
        private bool isProcessing = false;

        // Analytics
        private DialogueInteractionData dialogueInteractionData;
        private float dialogueStartTime;

        public void Display(string objectId, Dictionary<string, object> contentData, VisualElement root)
        {
            currentObjectId = objectId;
            rootElement = root;

            // Block character controls
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
                character.SetControlsEnabled(false);

            // Subscribe to language changes
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }

            // Parse dialogue tree
            dialogueTree = DialogueTreeData.FromDictionary(contentData);
            if (dialogueTree == null || string.IsNullOrEmpty(dialogueTree.startNodeId))
            {
                Debug.LogError("[DialogueDisplayer] Invalid dialogue data - no start node");
                Close();
                return;
            }

            // Initialize analytics
            InitializeAnalytics();

            // Build UI
            CreateDialogueUI();

            // Start from the first node
            var startNode = dialogueTree.GetStartNode();
            if (startNode != null && startNode.type == "start" && !string.IsNullOrEmpty(startNode.nextNodeId))
            {
                NavigateToNode(startNode.nextNodeId);
            }
            else
            {
                Debug.LogError("[DialogueDisplayer] Could not find start node or start node has no next");
                Close();
            }
        }

        private void InitializeAnalytics()
        {
            dialogueStartTime = Time.time;

            dialogueInteractionData = new DialogueInteractionData
            {
                dialogueKey = currentObjectId,
                objectId = currentObjectId,
                totalChoiceNodes = dialogueTree.CountChoiceNodes()
            };

            // Start analytics interaction
            if (TrainingAnalytics.Instance != null)
            {
                TrainingAnalytics.Instance.StartInteraction(currentObjectId, "dialogue", "branching");
            }
        }

        private void CreateDialogueUI()
        {
            rootElement.Clear();

            // Modal overlay
            modalContainer = new VisualElement();
            modalContainer.style.position = Position.Absolute;
            modalContainer.style.width = Length.Percent(100);
            modalContainer.style.height = Length.Percent(100);
            modalContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            modalContainer.style.alignItems = Align.Center;
            modalContainer.style.justifyContent = Justify.Center;

            // Dialogue box
            dialogueBox = new VisualElement();
            dialogueBox.style.width = 700;
            dialogueBox.style.maxWidth = Length.Percent(90);
            dialogueBox.style.maxHeight = Length.Percent(80);
            dialogueBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            dialogueBox.style.borderTopLeftRadius = 20;
            dialogueBox.style.borderTopRightRadius = 20;
            dialogueBox.style.borderBottomLeftRadius = 20;
            dialogueBox.style.borderBottomRightRadius = 20;
            dialogueBox.style.paddingTop = 30;
            dialogueBox.style.paddingBottom = 30;
            dialogueBox.style.paddingLeft = 35;
            dialogueBox.style.paddingRight = 35;
            dialogueBox.style.borderTopWidth = 2;
            dialogueBox.style.borderBottomWidth = 2;
            dialogueBox.style.borderLeftWidth = 2;
            dialogueBox.style.borderRightWidth = 2;
            dialogueBox.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            dialogueBox.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            dialogueBox.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            dialogueBox.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);

            // Context container (shows previous dialogue text above choices)
            contextContainer = new VisualElement();
            contextContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            contextContainer.style.borderTopLeftRadius = 10;
            contextContainer.style.borderTopRightRadius = 10;
            contextContainer.style.borderBottomLeftRadius = 10;
            contextContainer.style.borderBottomRightRadius = 10;
            contextContainer.style.paddingTop = 12;
            contextContainer.style.paddingBottom = 12;
            contextContainer.style.paddingLeft = 15;
            contextContainer.style.paddingRight = 15;
            contextContainer.style.marginBottom = 18;
            contextContainer.style.borderLeftWidth = 3;
            contextContainer.style.borderLeftColor = new Color(0.1f, 0.8f, 0.6f, 0.6f);
            contextContainer.style.display = DisplayStyle.None;

            contextSpeakerLabel = new Label();
            contextSpeakerLabel.style.fontSize = 16;
            contextSpeakerLabel.style.color = new Color(0.1f, 0.8f, 0.6f, 0.7f);
            contextSpeakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            contextSpeakerLabel.style.marginBottom = 5;
            contextContainer.Add(contextSpeakerLabel);

            contextTextLabel = new Label();
            contextTextLabel.style.fontSize = 16;
            contextTextLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            contextTextLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            contextTextLabel.style.whiteSpace = WhiteSpace.Normal;
            contextContainer.Add(contextTextLabel);

            dialogueBox.Add(contextContainer);

            // Speaker name
            speakerLabel = new Label();
            speakerLabel.style.fontSize = 22;
            speakerLabel.style.color = new Color(0.1f, 0.8f, 0.6f, 1f); // Accent green
            speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            speakerLabel.style.marginBottom = 15;
            dialogueBox.Add(speakerLabel);

            // Text content
            textLabel = new Label();
            textLabel.style.fontSize = 20;
            textLabel.style.color = Color.white;
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.marginBottom = 25;
            dialogueBox.Add(textLabel);

            // Choices container (for choice nodes)
            choicesContainer = new VisualElement();
            choicesContainer.style.marginBottom = 15;
            dialogueBox.Add(choicesContainer);

            // Continue button (for dialogue nodes)
            continueButton = new Button();
            continueButton.style.height = 50;
            continueButton.style.backgroundColor = new Color(0.1f, 0.8f, 0.6f, 1f);
            continueButton.style.color = Color.white;
            continueButton.style.fontSize = 18;
            continueButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            continueButton.style.borderTopLeftRadius = 10;
            continueButton.style.borderTopRightRadius = 10;
            continueButton.style.borderBottomLeftRadius = 10;
            continueButton.style.borderBottomRightRadius = 10;
            continueButton.style.borderTopWidth = 0;
            continueButton.style.borderBottomWidth = 0;
            continueButton.style.borderLeftWidth = 0;
            continueButton.style.borderRightWidth = 0;
            continueButton.style.display = DisplayStyle.None;
            dialogueBox.Add(continueButton);

            modalContainer.Add(dialogueBox);
            rootElement.Add(modalContainer);
        }

        private void NavigateToNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                EndDialogue(true);
                return;
            }

            currentNode = dialogueTree.GetNode(nodeId);
            if (currentNode == null)
            {
                Debug.LogError($"[DialogueDisplayer] Node not found: {nodeId}");
                EndDialogue(true);
                return;
            }

            switch (currentNode.type)
            {
                case "dialogue":
                    DisplayDialogueNode(currentNode);
                    break;
                case "choice":
                    DisplayChoiceNode(currentNode);
                    break;
                case "end":
                    EndDialogue(true);
                    break;
                case "start":
                    // Start nodes just redirect
                    NavigateToNode(currentNode.nextNodeId);
                    break;
                default:
                    Debug.LogWarning($"[DialogueDisplayer] Unknown node type: {currentNode.type}");
                    EndDialogue(true);
                    break;
            }
        }

        private void DisplayDialogueNode(DialogueNodeRuntime node)
        {
            string lang = GetCurrentLanguage();

            // Remember this dialogue node for context when showing choices
            lastDialogueNode = node;

            // Hide context (not needed for dialogue nodes)
            contextContainer.style.display = DisplayStyle.None;

            // Show speaker
            string speaker = lang == "fr" ? node.speaker_fr : node.speaker_en;
            if (string.IsNullOrEmpty(speaker))
                speaker = lang == "fr" ? node.speaker_en : node.speaker_fr; // fallback
            speakerLabel.text = speaker;
            speakerLabel.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;

            // Show text
            string text = lang == "fr" ? node.text_fr : node.text_en;
            if (string.IsNullOrEmpty(text))
                text = lang == "fr" ? node.text_en : node.text_fr; // fallback
            textLabel.text = text;

            // Hide choices, show continue button
            choicesContainer.Clear();
            choicesContainer.style.display = DisplayStyle.None;

            string continueText = lang == "fr" ? "Continuer" : "Continue";
            continueButton.text = continueText;
            continueButton.style.display = DisplayStyle.Flex;
            continueButton.clickable = new Clickable(() =>
            {
                if (isProcessing) return;
                NavigateToNode(node.nextNodeId);
            });
        }

        private void DisplayChoiceNode(DialogueNodeRuntime node)
        {
            string lang = GetCurrentLanguage();

            // Show previous dialogue context if available
            if (lastDialogueNode != null)
            {
                string ctxSpeaker = lang == "fr" ? lastDialogueNode.speaker_fr : lastDialogueNode.speaker_en;
                if (string.IsNullOrEmpty(ctxSpeaker))
                    ctxSpeaker = lang == "fr" ? lastDialogueNode.speaker_en : lastDialogueNode.speaker_fr;

                string ctxText = lang == "fr" ? lastDialogueNode.text_fr : lastDialogueNode.text_en;
                if (string.IsNullOrEmpty(ctxText))
                    ctxText = lang == "fr" ? lastDialogueNode.text_en : lastDialogueNode.text_fr;

                if (!string.IsNullOrEmpty(ctxText))
                {
                    contextSpeakerLabel.text = ctxSpeaker ?? "";
                    contextSpeakerLabel.style.display = string.IsNullOrEmpty(ctxSpeaker) ? DisplayStyle.None : DisplayStyle.Flex;
                    contextTextLabel.text = $"\"{ctxText}\"";
                    contextContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    contextContainer.style.display = DisplayStyle.None;
                }
            }
            else
            {
                contextContainer.style.display = DisplayStyle.None;
            }

            // Show prompt text
            string prompt = lang == "fr" ? node.choiceText_fr : node.choiceText_en;
            if (string.IsNullOrEmpty(prompt))
                prompt = lang == "fr" ? node.choiceText_en : node.choiceText_fr;
            speakerLabel.text = "";
            speakerLabel.style.display = DisplayStyle.None;
            textLabel.text = prompt;

            // Hide continue button
            continueButton.style.display = DisplayStyle.None;

            // Show choices
            choicesContainer.Clear();
            choicesContainer.style.display = DisplayStyle.Flex;

            if (node.choices == null) return;

            // Determine if this is an evaluated choice node (at least one choice marked correct)
            bool isEvaluated = IsEvaluatedChoiceNode(node);

            foreach (var choice in node.choices)
            {
                var choiceButton = CreateChoiceButton(choice, node, isEvaluated);
                choicesContainer.Add(choiceButton);
            }
        }

        /// <summary>
        /// A choice node is "evaluated" if at least one choice has isCorrect = true.
        /// If no choice is marked correct, the node is "neutral" (no feedback).
        /// </summary>
        private bool IsEvaluatedChoiceNode(DialogueNodeRuntime node)
        {
            if (node.choices == null) return false;
            foreach (var choice in node.choices)
            {
                if (choice.isCorrect) return true;
            }
            return false;
        }

        private VisualElement CreateChoiceButton(DialogueChoiceRuntime choice, DialogueNodeRuntime parentNode, bool isEvaluated)
        {
            string lang = GetCurrentLanguage();

            string choiceText = lang == "fr" ? choice.text_fr : choice.text_en;
            if (string.IsNullOrEmpty(choiceText))
                choiceText = lang == "fr" ? choice.text_en : choice.text_fr;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            container.style.borderTopLeftRadius = 10;
            container.style.borderTopRightRadius = 10;
            container.style.borderBottomLeftRadius = 10;
            container.style.borderBottomRightRadius = 10;
            container.style.paddingTop = 15;
            container.style.paddingBottom = 15;
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.marginBottom = 8;
            container.style.borderTopWidth = 2;
            container.style.borderBottomWidth = 2;
            container.style.borderLeftWidth = 2;
            container.style.borderRightWidth = 2;
            container.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            container.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            container.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
            container.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);

            var label = new Label(choiceText);
            label.style.fontSize = 18;
            label.style.color = Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1;

            container.Add(label);

            // Hover effects
            container.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (!isProcessing)
                {
                    container.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f);
                    container.style.borderTopColor = new Color(0.2f, 0.6f, 1f, 0.5f);
                    container.style.borderBottomColor = new Color(0.2f, 0.6f, 1f, 0.5f);
                    container.style.borderLeftColor = new Color(0.2f, 0.6f, 1f, 0.5f);
                    container.style.borderRightColor = new Color(0.2f, 0.6f, 1f, 0.5f);
                }
            });

            container.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (!isProcessing)
                {
                    container.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
                    container.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    container.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    container.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                    container.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f, 1f);
                }
            });

            // Click handler
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (isProcessing) return;
                isProcessing = true;

                // Record analytics
                float timestamp = Time.time - dialogueStartTime;
                dialogueInteractionData.RecordChoice(parentNode.id, choice.id, choice.isCorrect, timestamp);

                if (TrainingAnalytics.Instance != null)
                {
                    TrainingAnalytics.Instance.IncrementCurrentInteractionAttempts();
                }

                if (isEvaluated)
                {
                    // Evaluated choice: show green/red feedback, then navigate after delay
                    if (choice.isCorrect)
                    {
                        container.style.backgroundColor = new Color(0.1f, 0.5f, 0.3f, 0.4f);
                        container.style.borderTopColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                        container.style.borderBottomColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                        container.style.borderLeftColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                        container.style.borderRightColor = new Color(0.1f, 0.8f, 0.4f, 1f);
                    }
                    else
                    {
                        container.style.backgroundColor = new Color(0.5f, 0.1f, 0.1f, 0.4f);
                        container.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                        container.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                        container.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                        container.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    }

                    rootElement.schedule.Execute(() =>
                    {
                        isProcessing = false;
                        NavigateToNode(choice.nextNodeId);
                    }).ExecuteLater(800);
                }
                else
                {
                    // Neutral choice: brief highlight, navigate quickly
                    container.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f, 0.4f);
                    container.style.borderTopColor = new Color(0.3f, 0.6f, 0.9f, 1f);
                    container.style.borderBottomColor = new Color(0.3f, 0.6f, 0.9f, 1f);
                    container.style.borderLeftColor = new Color(0.3f, 0.6f, 0.9f, 1f);
                    container.style.borderRightColor = new Color(0.3f, 0.6f, 0.9f, 1f);

                    rootElement.schedule.Execute(() =>
                    {
                        isProcessing = false;
                        NavigateToNode(choice.nextNodeId);
                    }).ExecuteLater(300);
                }
            });

            return container;
        }

        private void EndDialogue(bool success)
        {
            // Complete analytics
            dialogueInteractionData.Complete();

            if (TrainingAnalytics.Instance != null)
            {
                var data = dialogueInteractionData.ToDictionary();
                foreach (var kvp in data)
                {
                    TrainingAnalytics.Instance.AddDataToCurrentInteraction(kvp.Key, kvp.Value);
                }
                TrainingAnalytics.Instance.AddDataToCurrentInteraction("finalScore", dialogueInteractionData.finalScore);
                TrainingAnalytics.Instance.EndCurrentInteraction(success);
            }

            OnCompleted?.Invoke(currentObjectId, success);

            // Show brief completion then close
            string lang = GetCurrentLanguage();
            string completeText = lang == "fr" ? "Dialogue terminé" : "Dialogue complete";
            textLabel.text = completeText;
            speakerLabel.style.display = DisplayStyle.None;
            choicesContainer.style.display = DisplayStyle.None;

            string closeText = lang == "fr" ? "Fermer" : "Close";
            continueButton.text = closeText;
            continueButton.style.display = DisplayStyle.Flex;
            continueButton.clickable = new Clickable(() => Close());
        }

        public void Close()
        {
            isProcessing = false;

            // Unblock character controls
            var character = FindFirstObjectByType<FirstPersonCharacter>();
            if (character != null)
                character.SetControlsEnabled(true);

            // Unsubscribe from language changes
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;

            rootElement?.Clear();
            OnClosed?.Invoke(currentObjectId);
        }

        private void OnLanguageChanged(string newLanguage)
        {
            // Refresh current node display
            if (currentNode == null) return;

            switch (currentNode.type)
            {
                case "dialogue":
                    DisplayDialogueNode(currentNode);
                    break;
                case "choice":
                    DisplayChoiceNode(currentNode);
                    break;
            }
        }

        private string GetCurrentLanguage()
        {
            if (LocalizationManager.Instance != null)
                return LocalizationManager.Instance.CurrentLanguage;
            return "en";
        }

        void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
}
