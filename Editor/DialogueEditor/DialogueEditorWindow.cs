using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR

namespace WiseTwin.Editor.DialogueEditor
{
    /// <summary>
    /// EditorWindow that hosts the DialogueGraphView for visual dialogue editing.
    /// Provides toolbar with node creation buttons and save functionality.
    /// </summary>
    public class DialogueEditorWindow : EditorWindow
    {
        private DialogueGraphView graphView;
        private DialogueScenarioData currentDialogue;
        private bool isDirty = false;

        [MenuItem("WiseTwin/Dialogue Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueEditorWindow>("Dialogue Graph Editor");
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// Open the editor with a specific dialogue loaded.
        /// </summary>
        public static void OpenWithDialogue(DialogueScenarioData dialogue)
        {
            var window = GetWindow<DialogueEditorWindow>("Dialogue Graph Editor");
            window.minSize = new Vector2(800, 600);
            // Ensure graphView exists (OnEnable may not run if window was already open)
            if (window.graphView == null)
            {
                window.rootVisualElement.Clear();
                window.rootVisualElement.style.flexDirection = FlexDirection.Column;
                window.rootVisualElement.style.flexGrow = 1;
                window.ConstructToolbar();
                window.ConstructGraphView();
            }
            window.LoadDialogue(dialogue);
            window.Show();
            window.Focus();
        }

        void OnEnable()
        {
            // Use vertical flex layout so toolbar sits on top and graphView fills the rest
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1;

            ConstructToolbar();
            ConstructGraphView();
        }

        void OnDisable()
        {
            if (isDirty && currentDialogue != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes in the dialogue graph. Save before closing?",
                    "Save", "Discard"))
                {
                    SaveGraph();
                }
            }

            if (graphView != null && rootVisualElement.Contains(graphView))
            {
                rootVisualElement.Remove(graphView);
            }
        }

        private void ConstructGraphView()
        {
            graphView = new DialogueGraphView(this);
            graphView.style.flexGrow = 1; // Fill remaining space below toolbar
            rootVisualElement.Add(graphView);
        }

        private void ConstructToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f);

            // Node creation buttons
            var startButton = CreateToolbarButton("+ Start", new Color(0.2f, 0.7f, 0.3f, 1f), () =>
            {
                if (graphView.HasStartNode())
                {
                    EditorUtility.DisplayDialog("Error", "Only one Start node is allowed per dialogue.", "OK");
                    return;
                }
                graphView.CreateNode("start", GetCenterPosition());
            });
            toolbar.Add(startButton);

            var dialogueButton = CreateToolbarButton("+ Dialogue", new Color(0.2f, 0.5f, 0.9f, 1f), () =>
            {
                graphView.CreateNode("dialogue", GetCenterPosition());
            });
            toolbar.Add(dialogueButton);

            var choiceButton = CreateToolbarButton("+ Choice", new Color(0.9f, 0.6f, 0.1f, 1f), () =>
            {
                graphView.CreateNode("choice", GetCenterPosition());
            });
            toolbar.Add(choiceButton);

            var endButton = CreateToolbarButton("+ End", new Color(0.8f, 0.2f, 0.2f, 1f), () =>
            {
                graphView.CreateNode("end", GetCenterPosition());
            });
            toolbar.Add(endButton);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Save button
            var saveButton = CreateToolbarButton("Save", new Color(0.1f, 0.7f, 0.5f, 1f), SaveGraph);
            saveButton.style.minWidth = 80;
            toolbar.Add(saveButton);

            // Info label
            var infoLabel = new Label();
            infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            infoLabel.style.marginLeft = 10;
            infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (currentDialogue != null)
                infoLabel.text = $"Editing: {currentDialogue.dialogueId}";
            else
                infoLabel.text = "No dialogue loaded";
            toolbar.Add(infoLabel);

            // Toolbar height is fixed, not flexible
            toolbar.style.flexShrink = 0;
            rootVisualElement.Add(toolbar);
        }

        private Button CreateToolbarButton(string text, Color color, Action onClick)
        {
            var button = new Button(onClick);
            button.text = text;
            button.style.backgroundColor = color;
            button.style.color = Color.white;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.height = 24;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        private Vector2 GetCenterPosition()
        {
            var center = graphView.contentViewContainer.WorldToLocal(
                graphView.contentViewContainer.parent.worldBound.center);
            // Add some random offset to avoid stacking
            center += new Vector2(
                UnityEngine.Random.Range(-50, 50),
                UnityEngine.Random.Range(-50, 50));
            return center;
        }

        /// <summary>
        /// Load a dialogue into the graph editor.
        /// </summary>
        public void LoadDialogue(DialogueScenarioData dialogue)
        {
            currentDialogue = dialogue;
            isDirty = false;

            if (!string.IsNullOrEmpty(dialogue.graphDataJSON))
            {
                var graphData = DialogueGraphSerializer.DeserializeEditorData(dialogue.graphDataJSON);
                if (graphData != null)
                {
                    graphView.LoadGraph(graphData);
                }
                else
                {
                    // Empty graph - create a default start node
                    CreateDefaultGraph();
                }
            }
            else
            {
                CreateDefaultGraph();
            }

            // Update toolbar info
            UpdateToolbarInfo();
        }

        private void CreateDefaultGraph()
        {
            graphView.LoadGraph(null); // Clear
            graphView.CreateNode("start", new Vector2(100, 200));
            graphView.CreateNode("end", new Vector2(600, 200));
        }

        /// <summary>
        /// Save the current graph to the dialogue data.
        /// </summary>
        private void SaveGraph()
        {
            if (currentDialogue == null)
            {
                EditorUtility.DisplayDialog("Error", "No dialogue is currently loaded.", "OK");
                return;
            }

            var graphData = graphView.SaveGraph();

            // Store editor format (preserves positions + explicit edges)
            // Runtime conversion happens only at metadata export time
            currentDialogue.graphDataJSON = DialogueGraphSerializer.SerializeEditorData(graphData);

            isDirty = false;

            Debug.Log($"[DialogueEditor] Saved dialogue graph: {currentDialogue.dialogueId}");
            ShowNotification(new GUIContent("Graph saved!"), 1.5f);
        }

        private void UpdateToolbarInfo()
        {
            // Find and update the info label anywhere in the root
            var labels = rootVisualElement.Query<Label>().ToList();
            foreach (var label in labels)
            {
                if (label.text != null && (label.text.StartsWith("Editing:") || label.text.StartsWith("No dialogue")))
                {
                    if (currentDialogue != null)
                        label.text = $"Editing: {currentDialogue.dialogueId}";
                    else
                        label.text = "No dialogue loaded";
                    break;
                }
            }
        }

        /// <summary>
        /// Called by the GraphView when changes are made.
        /// </summary>
        public new void SetDirty()
        {
            isDirty = true;
        }
    }
}

#endif
