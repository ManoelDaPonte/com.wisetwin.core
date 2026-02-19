using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

#if UNITY_EDITOR

namespace WiseTwin.Editor.DialogueEditor
{
    /// <summary>
    /// Visual representation of a dialogue node in the GraphView.
    /// Supports Start, Dialogue, Choice, and End node types with inline editing.
    /// </summary>
    public class DialogueNodeView : Node
    {
        public string NodeId { get; private set; }
        public string NodeType { get; private set; }

        private DialogueGraphView graphView;
        private Port inputPort;
        private Port outputPort;
        private Dictionary<string, Port> choiceOutputPorts = new Dictionary<string, Port>();

        // Editable fields
        private TextField speakerFieldEN;
        private TextField speakerFieldFR;
        private TextField textFieldEN;
        private TextField textFieldFR;
        private TextField promptFieldEN;
        private TextField promptFieldFR;

        // Choice data (kept in sync with UI)
        private List<DialogueChoiceEditorData> choices = new List<DialogueChoiceEditorData>();
        private VisualElement choicesContainer;
        private int choiceCounter = 0;

        public DialogueNodeView(DialogueNodeEditorData data, DialogueGraphView graphView)
        {
            this.graphView = graphView;
            NodeId = data.id;
            NodeType = data.type;

            // Copy choices data
            if (data.choices != null)
            {
                choices = new List<DialogueChoiceEditorData>(data.choices);
                choiceCounter = choices.Count;
            }

            SetupNodeAppearance();
            SetupPorts();
            SetupContent(data);

            // Ensure the extension container (content area) is expanded
            expanded = true;
            RefreshExpandedState();
            RefreshPorts();
        }

        private void SetupNodeAppearance()
        {
            title = GetNodeTitle();

            // Set node color based on type
            Color titleColor;
            switch (NodeType)
            {
                case "start":
                    titleColor = new Color(0.2f, 0.7f, 0.3f, 1f); // Green
                    break;
                case "dialogue":
                    titleColor = new Color(0.2f, 0.5f, 0.9f, 1f); // Blue
                    break;
                case "choice":
                    titleColor = new Color(0.9f, 0.6f, 0.1f, 1f); // Orange
                    break;
                case "end":
                    titleColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
                    break;
                default:
                    titleColor = Color.gray;
                    break;
            }

            // Apply title bar color
            var titleContainer = this.Q("title");
            if (titleContainer != null)
            {
                titleContainer.style.backgroundColor = titleColor;
            }

            // Set minimum width
            style.minWidth = 280;
        }

        private string GetNodeTitle()
        {
            switch (NodeType)
            {
                case "start": return "START";
                case "dialogue": return "Dialogue";
                case "choice": return "Choice";
                case "end": return "END";
                default: return NodeType;
            }
        }

        private void SetupPorts()
        {
            switch (NodeType)
            {
                case "start":
                    // Start: no input, 1 output
                    outputPort = CreateOutputPort("output", "Next");
                    break;

                case "dialogue":
                    // Dialogue: 1 input (multi), 1 output
                    inputPort = CreateInputPort("input", "In");
                    outputPort = CreateOutputPort("output", "Next");
                    break;

                case "choice":
                    // Choice: 1 input (multi), N outputs (1 per choice)
                    inputPort = CreateInputPort("input", "In");
                    // Choice output ports are created in SetupContent
                    break;

                case "end":
                    // End: 1 input (multi), no output
                    inputPort = CreateInputPort("input", "In");
                    break;
            }
        }

        private Port CreateInputPort(string portName, string label)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            port.portName = portName;
            port.portColor = new Color(0.7f, 0.9f, 1f, 1f);
            inputContainer.Add(port);
            return port;
        }

        private Port CreateOutputPort(string portName, string label)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = portName;
            port.portColor = new Color(1f, 0.9f, 0.7f, 1f);
            outputContainer.Add(port);
            return port;
        }

        private void SetupContent(DialogueNodeEditorData data)
        {
            var contentContainer = new VisualElement();
            contentContainer.style.paddingTop = 5;
            contentContainer.style.paddingBottom = 5;
            contentContainer.style.paddingLeft = 10;
            contentContainer.style.paddingRight = 10;

            switch (NodeType)
            {
                case "start":
                    var startLabel = new Label("Entry point of the dialogue");
                    startLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    startLabel.style.fontSize = 11;
                    contentContainer.Add(startLabel);
                    break;

                case "dialogue":
                    SetupDialogueContent(contentContainer, data);
                    break;

                case "choice":
                    SetupChoiceContent(contentContainer, data);
                    break;

                case "end":
                    var endLabel = new Label("End of dialogue branch");
                    endLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    endLabel.style.fontSize = 11;
                    contentContainer.Add(endLabel);
                    break;
            }

            // ID label at bottom
            var idLabel = new Label($"ID: {NodeId}");
            idLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            idLabel.style.fontSize = 10;
            idLabel.style.marginTop = 5;
            contentContainer.Add(idLabel);

            // Add to mainContainer instead of extensionContainer for reliable display
            mainContainer.Add(contentContainer);
        }

        private void SetupDialogueContent(VisualElement container, DialogueNodeEditorData data)
        {
            // Speaker EN
            var speakerLabelEN = new Label("Speaker (EN):");
            speakerLabelEN.style.fontSize = 11;
            speakerLabelEN.style.color = new Color(0.6f, 0.8f, 1f, 1f);
            container.Add(speakerLabelEN);

            speakerFieldEN = new TextField();
            speakerFieldEN.value = data.speakerEN ?? "";
            speakerFieldEN.style.marginBottom = 3;
            container.Add(speakerFieldEN);

            // Speaker FR
            var speakerLabelFR = new Label("Speaker (FR):");
            speakerLabelFR.style.fontSize = 11;
            speakerLabelFR.style.color = new Color(0.6f, 0.8f, 1f, 1f);
            container.Add(speakerLabelFR);

            speakerFieldFR = new TextField();
            speakerFieldFR.value = data.speakerFR ?? "";
            speakerFieldFR.style.marginBottom = 5;
            container.Add(speakerFieldFR);

            // Text EN
            var textLabelEN = new Label("Text (EN):");
            textLabelEN.style.fontSize = 11;
            textLabelEN.style.color = new Color(0.6f, 0.8f, 1f, 1f);
            container.Add(textLabelEN);

            textFieldEN = new TextField();
            textFieldEN.multiline = true;
            textFieldEN.value = data.textEN ?? "";
            textFieldEN.style.minHeight = 40;
            textFieldEN.style.marginBottom = 3;
            container.Add(textFieldEN);

            // Text FR
            var textLabelFR = new Label("Text (FR):");
            textLabelFR.style.fontSize = 11;
            textLabelFR.style.color = new Color(0.6f, 0.8f, 1f, 1f);
            container.Add(textLabelFR);

            textFieldFR = new TextField();
            textFieldFR.multiline = true;
            textFieldFR.value = data.textFR ?? "";
            textFieldFR.style.minHeight = 40;
            container.Add(textFieldFR);
        }

        private void SetupChoiceContent(VisualElement container, DialogueNodeEditorData data)
        {
            // Prompt EN
            var promptLabelEN = new Label("Prompt (EN):");
            promptLabelEN.style.fontSize = 11;
            promptLabelEN.style.color = new Color(1f, 0.8f, 0.5f, 1f);
            container.Add(promptLabelEN);

            promptFieldEN = new TextField();
            promptFieldEN.value = data.promptTextEN ?? "";
            promptFieldEN.style.marginBottom = 3;
            container.Add(promptFieldEN);

            // Prompt FR
            var promptLabelFR = new Label("Prompt (FR):");
            promptLabelFR.style.fontSize = 11;
            promptLabelFR.style.color = new Color(1f, 0.8f, 0.5f, 1f);
            container.Add(promptLabelFR);

            promptFieldFR = new TextField();
            promptFieldFR.value = data.promptTextFR ?? "";
            promptFieldFR.style.marginBottom = 8;
            container.Add(promptFieldFR);

            // Choices container
            choicesContainer = new VisualElement();
            container.Add(choicesContainer);

            // Add existing choices
            foreach (var choice in choices)
            {
                AddChoiceUI(choice);
            }

            // Add choice button
            var addButton = new Button(() => AddNewChoice());
            addButton.text = "+ Add Choice";
            addButton.style.marginTop = 5;
            addButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.3f, 1f);
            addButton.style.color = Color.white;
            container.Add(addButton);
        }

        private void AddNewChoice()
        {
            choiceCounter++;
            var choice = new DialogueChoiceEditorData
            {
                id = $"{NodeId}_choice_{choices.Count}",
                textEN = $"Option {choices.Count + 1}",
                textFR = $"Option {choices.Count + 1}",
                portName = $"choice_{choices.Count}"
            };

            choices.Add(choice);
            AddChoiceUI(choice);

            RefreshExpandedState();
            RefreshPorts();

            graphView?.SetDirty();
        }

        private void AddChoiceUI(DialogueChoiceEditorData choice)
        {
            var choiceContainer = new VisualElement();
            choiceContainer.style.borderTopWidth = 1;
            choiceContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            choiceContainer.style.paddingTop = 5;
            choiceContainer.style.paddingBottom = 5;
            choiceContainer.style.marginBottom = 3;

            // Header with correct toggle and remove button
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 3;

            var correctToggle = new Toggle("Correct");
            correctToggle.value = choice.isCorrect;
            correctToggle.RegisterValueChangedCallback(evt =>
            {
                choice.isCorrect = evt.newValue;
                graphView?.SetDirty();
            });
            correctToggle.style.flexGrow = 1;
            header.Add(correctToggle);

            var removeButton = new Button(() => RemoveChoice(choice, choiceContainer));
            removeButton.text = "X";
            removeButton.style.width = 25;
            removeButton.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            removeButton.style.color = Color.white;
            header.Add(removeButton);

            choiceContainer.Add(header);

            // Text fields
            var textEN = new TextField("EN");
            textEN.value = choice.textEN ?? "";
            textEN.RegisterValueChangedCallback(evt => { choice.textEN = evt.newValue; graphView?.SetDirty(); });
            textEN.style.marginBottom = 2;
            choiceContainer.Add(textEN);

            var textFR = new TextField("FR");
            textFR.value = choice.textFR ?? "";
            textFR.RegisterValueChangedCallback(evt => { choice.textFR = evt.newValue; graphView?.SetDirty(); });
            choiceContainer.Add(textFR);

            choicesContainer.Add(choiceContainer);

            // Create output port for this choice
            string portName = choice.portName ?? $"choice_{choices.IndexOf(choice)}";
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = portName;
            port.portColor = choice.isCorrect ? new Color(0.3f, 0.9f, 0.4f, 1f) : new Color(1f, 0.5f, 0.3f, 1f);

            // Note: do NOT override port.Q<Label>().text - it breaks port.portName
            // which is needed for edge save/load. The portName IS the display text.

            // Update port color when correct toggle changes
            correctToggle.RegisterValueChangedCallback(evt =>
            {
                port.portColor = evt.newValue ? new Color(0.3f, 0.9f, 0.4f, 1f) : new Color(1f, 0.5f, 0.3f, 1f);
            });

            outputContainer.Add(port);
            choiceOutputPorts[portName] = port;
        }

        private void RemoveChoice(DialogueChoiceEditorData choice, VisualElement choiceUI)
        {
            if (choices.Count <= 1) return; // Keep at least one choice

            // Remove connected edges
            string portName = choice.portName;
            if (choiceOutputPorts.TryGetValue(portName, out var port))
            {
                var connectedEdges = port.connections.ToList();
                foreach (var edge in connectedEdges)
                {
                    graphView.RemoveElement(edge);
                }
                outputContainer.Remove(port);
                choiceOutputPorts.Remove(portName);
            }

            choices.Remove(choice);
            choicesContainer.Remove(choiceUI);

            RefreshExpandedState();
            RefreshPorts();

            graphView?.SetDirty();
        }

        /// <summary>
        /// Get the output port by name.
        /// </summary>
        public Port GetOutputPort(string portName)
        {
            if (portName == "output" && outputPort != null)
                return outputPort;

            if (choiceOutputPorts.TryGetValue(portName, out var port))
                return port;

            return null;
        }

        /// <summary>
        /// Get the input port.
        /// </summary>
        public Port GetInputPort()
        {
            return inputPort;
        }

        /// <summary>
        /// Save this node's data.
        /// </summary>
        public DialogueNodeEditorData SaveData()
        {
            var data = new DialogueNodeEditorData
            {
                id = NodeId,
                type = NodeType,
                position = GetPosition().position
            };

            switch (NodeType)
            {
                case "dialogue":
                    data.speakerEN = speakerFieldEN?.value ?? "";
                    data.speakerFR = speakerFieldFR?.value ?? "";
                    data.textEN = textFieldEN?.value ?? "";
                    data.textFR = textFieldFR?.value ?? "";
                    break;

                case "choice":
                    data.promptTextEN = promptFieldEN?.value ?? "";
                    data.promptTextFR = promptFieldFR?.value ?? "";
                    data.choices = new List<DialogueChoiceEditorData>(choices);
                    break;
            }

            return data;
        }

        public void SetDirty()
        {
            graphView?.SetDirty();
        }
    }
}

#endif
