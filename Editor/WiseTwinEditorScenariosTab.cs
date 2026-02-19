using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Scenarios Configuration tab for WiseTwinEditor
    /// </summary>
    public static class WiseTwinEditorScenariosTab
    {
        public static void Draw(WiseTwinEditorData data)
        {
            EditorGUILayout.LabelField("🎬 Scenario Configuration", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Configure your training scenarios visually. Choose type (Question/Procedure/Text/Dialogue), configure parameters, and export to metadata.json.", MessageType.Info);
            EditorGUILayout.Space();

            // Add scenario button
            if (GUILayout.Button("➕ Add New Scenario", GUILayout.Height(30)))
            {
                data.scenarios.Add(new ScenarioConfiguration());
                data.selectedScenarioIndex = data.scenarios.Count - 1;
            }

            EditorGUILayout.Space();

            if (data.scenarios.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenarios yet. Click 'Add New Scenario' to get started!", MessageType.Info);
                return;
            }

            // Scenario list with delete buttons and drag & drop
            EditorGUILayout.LabelField($"Scenarios ({data.scenarios.Count})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag scenarios to reorder them. The order will be used in the ProgressionManager.", MessageType.Info);

            for (int i = 0; i < data.scenarios.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Drag handle
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
                if (GUILayout.RepeatButton("≡", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    // Start dragging
                    if (Event.current.type == EventType.MouseDown)
                    {
                        data.draggingScenarioIndex = i;
                    }
                }
                GUI.backgroundColor = Color.white;

                // Move up button
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    var temp = data.scenarios[i];
                    data.scenarios[i] = data.scenarios[i - 1];
                    data.scenarios[i - 1] = temp;
                    if (data.selectedScenarioIndex == i)
                        data.selectedScenarioIndex = i - 1;
                    else if (data.selectedScenarioIndex == i - 1)
                        data.selectedScenarioIndex = i;
                }
                GUI.enabled = true;

                // Move down button
                GUI.enabled = i < data.scenarios.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    var temp = data.scenarios[i];
                    data.scenarios[i] = data.scenarios[i + 1];
                    data.scenarios[i + 1] = temp;
                    if (data.selectedScenarioIndex == i)
                        data.selectedScenarioIndex = i + 1;
                    else if (data.selectedScenarioIndex == i + 1)
                        data.selectedScenarioIndex = i;
                }
                GUI.enabled = true;

                // Select/Toggle button
                bool isSelected = (data.selectedScenarioIndex == i);
                GUI.backgroundColor = isSelected ? new Color(0.4f, 0.8f, 1f) : Color.white;
                string buttonText = isSelected ? $"▼ {i + 1}. {data.scenarios[i].id} ({data.scenarios[i].type})" : $"▶ {i + 1}. {data.scenarios[i].id} ({data.scenarios[i].type})";
                if (GUILayout.Button(buttonText, GUILayout.Height(25)))
                {
                    // Toggle: si déjà sélectionné, replier (-1), sinon ouvrir (i)
                    data.selectedScenarioIndex = isSelected ? -1 : i;
                }
                GUI.backgroundColor = Color.white;

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑", GUILayout.Width(30), GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Scenario", $"Delete scenario '{data.scenarios[i].id}'?", "Delete", "Cancel"))
                    {
                        data.scenarios.RemoveAt(i);
                        if (data.selectedScenarioIndex >= data.scenarios.Count)
                        {
                            data.selectedScenarioIndex = data.scenarios.Count - 1;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Edit selected scenario
            if (data.selectedScenarioIndex >= 0 && data.selectedScenarioIndex < data.scenarios.Count)
            {
                DrawScenarioEditor(data.scenarios[data.selectedScenarioIndex], data);
            }
        }

        private static void DrawScenarioEditor(ScenarioConfiguration scenario, WiseTwinEditorData data)
        {
            EditorGUILayout.LabelField("Edit Scenario", EditorStyles.boldLabel);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            // Basic info
            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            scenario.id = EditorGUILayout.TextField("Scenario ID", scenario.id);
            scenario.type = (ScenarioType)EditorGUILayout.EnumPopup("Type", scenario.type);

            EditorGUILayout.Space();

            // Type-specific fields
            switch (scenario.type)
            {
                case ScenarioType.Question:
                    DrawQuestionsEditor(scenario.questions);
                    break;
                case ScenarioType.Procedure:
                    DrawProcedureEditor(scenario.procedureData);
                    break;
                case ScenarioType.Text:
                    DrawTextEditor(scenario.textData);
                    break;
                case ScenarioType.Dialogue:
                    DrawDialogueEditor(scenario.dialogueData, data);
                    break;
            }
        }

        private static void DrawQuestionsEditor(List<QuestionScenarioData> questions)
        {
            EditorGUILayout.LabelField("Questions Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"{questions.Count} question(s) in this scenario. Questions will be displayed sequentially.", MessageType.Info);

            EditorGUILayout.Space();

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Add Question", GUILayout.Height(25)))
            {
                questions.Add(new QuestionScenarioData());
            }
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (questions.Count > 1 && GUILayout.Button($"➖ Remove Last Question", GUILayout.Height(25)))
            {
                questions.RemoveAt(questions.Count - 1);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Draw each question
            for (int i = 0; i < questions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"━━━ Question {i + 1} / {questions.Count} ━━━", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                DrawQuestionEditor(questions[i], i, questions);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            }
        }

        private static void DrawQuestionEditor(QuestionScenarioData question, int index, List<QuestionScenarioData> allQuestions)
        {
            EditorGUILayout.LabelField("Question Configuration", EditorStyles.boldLabel);

            // Question text (larger area with word wrap)
            EditorGUILayout.LabelField("Question Text", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("  (English)", EditorStyles.miniLabel);
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            question.questionTextEN = EditorGUILayout.TextArea(question.questionTextEN, textAreaStyle, GUILayout.Height(80));
            EditorGUILayout.LabelField("  (Français)", EditorStyles.miniLabel);
            question.questionTextFR = EditorGUILayout.TextArea(question.questionTextFR, textAreaStyle, GUILayout.Height(80));

            EditorGUILayout.Space();

            // Question type
            question.isMultipleChoice = EditorGUILayout.Toggle("Multiple Choice (plusieurs réponses)", question.isMultipleChoice);

            EditorGUILayout.Space();

            // Options
            EditorGUILayout.LabelField("Options", EditorStyles.miniBoldLabel);

            int optionsCount = Mathf.Max(question.optionsEN.Count, question.optionsFR.Count);

            for (int i = 0; i < optionsCount; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));

                // Toggle pour correct answer
                bool isCorrect = question.correctAnswers.Contains(i);
                bool newIsCorrect = EditorGUILayout.Toggle("Correct", isCorrect, GUILayout.Width(80));
                if (newIsCorrect != isCorrect)
                {
                    if (newIsCorrect)
                    {
                        if (!question.isMultipleChoice)
                        {
                            question.correctAnswers.Clear();
                        }
                        question.correctAnswers.Add(i);
                    }
                    else
                    {
                        question.correctAnswers.Remove(i);
                    }
                }

                // Delete button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    question.optionsEN.RemoveAt(i);
                    question.optionsFR.RemoveAt(i);
                    question.correctAnswers.Remove(i);
                    // Adjust indices in correctAnswers
                    for (int j = 0; j < question.correctAnswers.Count; j++)
                    {
                        if (question.correctAnswers[j] > i)
                        {
                            question.correctAnswers[j]--;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Ensure lists have enough elements
                while (question.optionsEN.Count <= i) question.optionsEN.Add("");
                while (question.optionsFR.Count <= i) question.optionsFR.Add("");

                question.optionsEN[i] = EditorGUILayout.TextField("  EN", question.optionsEN[i]);
                question.optionsFR[i] = EditorGUILayout.TextField("  FR", question.optionsFR[i]);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("➕ Add Option"))
            {
                question.optionsEN.Add("");
                question.optionsFR.Add("");
            }

            EditorGUILayout.Space();

            // Feedback (larger area with word wrap)
            EditorGUILayout.LabelField("Feedback Messages", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Success Feedback (English):", EditorStyles.miniLabel);
            question.feedbackEN = EditorGUILayout.TextArea(question.feedbackEN, textAreaStyle, GUILayout.Height(60));
            EditorGUILayout.LabelField("Success Feedback (Français):", EditorStyles.miniLabel);
            question.feedbackFR = EditorGUILayout.TextArea(question.feedbackFR, textAreaStyle, GUILayout.Height(60));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Error Feedback (English):", EditorStyles.miniLabel);
            question.incorrectFeedbackEN = EditorGUILayout.TextArea(question.incorrectFeedbackEN, textAreaStyle, GUILayout.Height(60));
            EditorGUILayout.LabelField("Error Feedback (Français):", EditorStyles.miniLabel);
            question.incorrectFeedbackFR = EditorGUILayout.TextArea(question.incorrectFeedbackFR, textAreaStyle, GUILayout.Height(60));
        }

        private static void DrawProcedureEditor(ProcedureScenarioData procedure)
        {
            EditorGUILayout.LabelField("Procedure Configuration", EditorStyles.boldLabel);

            // Title & Description
            EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
            procedure.titleEN = EditorGUILayout.TextField("  EN", procedure.titleEN);
            procedure.titleFR = EditorGUILayout.TextField("  FR", procedure.titleFR);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            EditorGUILayout.LabelField("  (English)", EditorStyles.miniLabel);
            procedure.descriptionEN = EditorGUILayout.TextArea(procedure.descriptionEN, textAreaStyle, GUILayout.Height(60));
            EditorGUILayout.LabelField("  (Français)", EditorStyles.miniLabel);
            procedure.descriptionFR = EditorGUILayout.TextArea(procedure.descriptionFR, textAreaStyle, GUILayout.Height(60));

            EditorGUILayout.Space();

            // Steps
            EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use arrows to reorder steps. The order will be used in the procedure execution.", MessageType.Info);

            for (int i = 0; i < procedure.steps.Count; i++)
            {
                DrawProcedureStep(procedure.steps[i], i, procedure.steps);
            }

            if (GUILayout.Button("➕ Add Step", GUILayout.Height(25)))
            {
                procedure.steps.Add(new ProcedureStep());
            }

            EditorGUILayout.Space();

            // Fake Objects
            EditorGUILayout.LabelField("Fake Objects (Errors)", EditorStyles.boldLabel);
            for (int i = 0; i < procedure.fakeObjects.Count; i++)
            {
                DrawFakeObject(procedure.fakeObjects[i], i, procedure.fakeObjects);
            }

            if (GUILayout.Button("➕ Add Fake Object", GUILayout.Height(25)))
            {
                procedure.fakeObjects.Add(new FakeObject());
            }
        }

        private static void DrawProcedureStep(ProcedureStep step, int index, List<ProcedureStep> steps)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Step {index + 1}", EditorStyles.boldLabel);

            // Move up button
            GUI.enabled = index > 0;
            if (GUILayout.Button("↑", GUILayout.Width(25)))
            {
                var temp = steps[index];
                steps[index] = steps[index - 1];
                steps[index - 1] = temp;
            }
            GUI.enabled = true;

            // Move down button
            GUI.enabled = index < steps.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(25)))
            {
                var temp = steps[index];
                steps[index] = steps[index + 1];
                steps[index + 1] = temp;
            }
            GUI.enabled = true;

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                steps.RemoveAt(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Target Object
            EditorGUI.BeginChangeCheck();
            step.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", step.targetObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && step.targetObject != null)
            {
                step.targetObjectName = step.targetObject.name;
            }
            step.targetObjectName = EditorGUILayout.TextField("Object Name", step.targetObjectName);

            // Step text (larger area with word wrap)
            EditorGUILayout.LabelField("Text", EditorStyles.miniLabel);
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            EditorGUILayout.LabelField("  EN:", EditorStyles.miniLabel);
            step.textEN = EditorGUILayout.TextArea(step.textEN, textAreaStyle, GUILayout.Height(50));
            EditorGUILayout.LabelField("  FR:", EditorStyles.miniLabel);
            step.textFR = EditorGUILayout.TextArea(step.textFR, textAreaStyle, GUILayout.Height(50));

            // Highlight
            step.highlightColor = EditorGUILayout.ColorField("Highlight Color", step.highlightColor);
            step.useBlinking = EditorGUILayout.Toggle("Use Blinking", step.useBlinking);

            // Validation type
            step.validationType = (ValidationType)EditorGUILayout.EnumPopup("Validation Type", step.validationType);

            // Zone object field (only shown when Zone validation is selected)
            if (step.validationType == ValidationType.Zone)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                step.zoneObject = (GameObject)EditorGUILayout.ObjectField("Zone Object", step.zoneObject, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck() && step.zoneObject != null)
                {
                    step.zoneObjectName = step.zoneObject.name;
                }
                step.zoneObjectName = EditorGUILayout.TextField("Zone Object Name", step.zoneObjectName);
                EditorGUILayout.HelpBox("The zone object must have a Collider set to 'Is Trigger'. Use WiseTwin > Create Validation Zone Prefab to generate one.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            // Image support
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Images (Optional)", EditorStyles.miniBoldLabel);
            step.imageEN = (Sprite)EditorGUILayout.ObjectField("Image EN", step.imageEN, typeof(Sprite), false);
            step.imageFR = (Sprite)EditorGUILayout.ObjectField("Image FR", step.imageFR, typeof(Sprite), false);

            // Store image paths for JSON export
            if (step.imageEN != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(step.imageEN);
                step.imagePathEN = assetPath;
                EditorGUILayout.HelpBox($"EN Image Path: {assetPath}", MessageType.None);
            }
            if (step.imageFR != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(step.imageFR);
                step.imagePathFR = assetPath;
                EditorGUILayout.HelpBox($"FR Image Path: {assetPath}", MessageType.None);
            }

            EditorGUILayout.Space();

            // Fake Objects for this step
            EditorGUILayout.LabelField("Fake Objects for this step", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Objects that will be highlighted along with the correct one. Clicking them shows an error.", MessageType.Info);

            for (int i = 0; i < step.fakeObjects.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Fake #{i + 1}", GUILayout.Width(80));
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    step.fakeObjects.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                step.fakeObjects[i].fakeObject = (GameObject)EditorGUILayout.ObjectField("GameObject", step.fakeObjects[i].fakeObject, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck() && step.fakeObjects[i].fakeObject != null)
                {
                    step.fakeObjects[i].fakeObjectName = step.fakeObjects[i].fakeObject.name;
                }
                step.fakeObjects[i].fakeObjectName = EditorGUILayout.TextField("Object Name", step.fakeObjects[i].fakeObjectName);
                step.fakeObjects[i].errorMessageEN = EditorGUILayout.TextField("Error EN", step.fakeObjects[i].errorMessageEN);
                step.fakeObjects[i].errorMessageFR = EditorGUILayout.TextField("Error FR", step.fakeObjects[i].errorMessageFR);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("➕ Add Fake Object to this step", GUILayout.Height(20)))
            {
                step.fakeObjects.Add(new FakeObject());
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private static void DrawFakeObject(FakeObject fake, int index, List<FakeObject> fakes)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Fake #{index + 1}", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                fakes.RemoveAt(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            fake.fakeObject = (GameObject)EditorGUILayout.ObjectField("GameObject", fake.fakeObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && fake.fakeObject != null)
            {
                fake.fakeObjectName = fake.fakeObject.name;
            }
            fake.fakeObjectName = EditorGUILayout.TextField("Object Name", fake.fakeObjectName);

            EditorGUILayout.LabelField("Error Message", EditorStyles.miniLabel);
            fake.errorMessageEN = EditorGUILayout.TextField("  EN", fake.errorMessageEN);
            fake.errorMessageFR = EditorGUILayout.TextField("  FR", fake.errorMessageFR);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private static void DrawDialogueEditor(DialogueScenarioData dialogue, WiseTwinEditorData data)
        {
            EditorGUILayout.LabelField("Dialogue Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure a branching dialogue tree. Use the Dialogue tab to create/edit dialogues in the visual graph editor.", MessageType.Info);

            EditorGUILayout.Space();

            // Select existing dialogue or create inline
            if (data.dialogues.Count > 0)
            {
                // Build dropdown options
                string[] dialogueOptions = new string[data.dialogues.Count + 1];
                dialogueOptions[0] = "(None - configure inline)";
                int currentSelection = 0;

                for (int i = 0; i < data.dialogues.Count; i++)
                {
                    var d = data.dialogues[i];
                    string title = !string.IsNullOrEmpty(d.titleEN) ? d.titleEN : d.dialogueId;
                    dialogueOptions[i + 1] = $"{d.dialogueId} - {title}";

                    if (d.dialogueId == dialogue.dialogueId)
                        currentSelection = i + 1;
                }

                int newSelection = EditorGUILayout.Popup("Link to Dialogue", currentSelection, dialogueOptions);
                if (newSelection != currentSelection)
                {
                    if (newSelection == 0)
                    {
                        dialogue.dialogueId = "";
                        dialogue.graphDataJSON = "";
                    }
                    else
                    {
                        var selected = data.dialogues[newSelection - 1];
                        dialogue.dialogueId = selected.dialogueId;
                        dialogue.titleEN = selected.titleEN;
                        dialogue.titleFR = selected.titleFR;
                        dialogue.graphDataJSON = selected.graphDataJSON;
                    }
                }
            }

            EditorGUILayout.Space();

            // Title
            EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
            dialogue.titleEN = EditorGUILayout.TextField("  EN", dialogue.titleEN);
            dialogue.titleFR = EditorGUILayout.TextField("  FR", dialogue.titleFR);

            EditorGUILayout.Space();

            // Dialogue ID
            dialogue.dialogueId = EditorGUILayout.TextField("Dialogue ID", dialogue.dialogueId);

            EditorGUILayout.Space();

            // Open graph editor button
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Open Graph Editor", GUILayout.Height(30)))
            {
                WiseTwin.Editor.DialogueEditor.DialogueEditorWindow.OpenWithDialogue(dialogue);
            }
            GUI.backgroundColor = Color.white;

            // Show status
            if (!string.IsNullOrEmpty(dialogue.graphDataJSON))
            {
                EditorGUILayout.HelpBox("Graph data is configured. Open the Graph Editor to modify.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No graph data yet. Open the Graph Editor to create a dialogue tree.", MessageType.Warning);
            }
        }

        private static void DrawTextEditor(TextScenarioData text)
        {
            EditorGUILayout.LabelField("Text/Information Configuration", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
            text.titleEN = EditorGUILayout.TextField("  EN", text.titleEN);
            text.titleFR = EditorGUILayout.TextField("  FR", text.titleFR);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Content", EditorStyles.miniBoldLabel);
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            EditorGUILayout.LabelField("  (English)", EditorStyles.miniLabel);
            text.contentEN = EditorGUILayout.TextArea(text.contentEN, textAreaStyle, GUILayout.Height(150));
            EditorGUILayout.LabelField("  (Français)", EditorStyles.miniLabel);
            text.contentFR = EditorGUILayout.TextArea(text.contentFR, textAreaStyle, GUILayout.Height(150));
        }
    }

    // ============= SCENARIO IMPORT WINDOW =============

    public class ScenarioImportWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string jsonContent = "";
        private WiseTwinEditorData targetData;

        public static void ShowWindow(WiseTwinEditorData data)
        {
            ScenarioImportWindow window = GetWindow<ScenarioImportWindow>("Import Scenarios");
            window.targetData = data;
            window.minSize = new Vector2(700, 600);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("📥 Import Scenarios from JSON", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Paste your scenarios JSON array below. You can import one or multiple scenarios at once.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Paste from Clipboard"))
            {
                jsonContent = EditorGUIUtility.systemCopyBuffer;
            }
            if (GUILayout.Button("🧹 Clear"))
            {
                jsonContent = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("JSON Content:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            jsonContent = EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
            if (GUILayout.Button("✅ Import Scenarios", GUILayout.Height(35)))
            {
                ImportScenarios();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ImportScenarios()
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                EditorUtility.DisplayDialog("Error", "Please paste JSON content first!", "OK");
                return;
            }

            try
            {
                List<object> scenariosJSON = null;

                // Try to parse as array first, if that fails, try as single object
                try
                {
                    scenariosJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(jsonContent);
                }
                catch
                {
                    // If array parsing fails, try parsing as a single object
                    var singleScenario = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(jsonContent);
                    if (singleScenario != null)
                    {
                        scenariosJSON = new List<object> { singleScenario };
                        Debug.Log("[ScenarioImport] Parsed as single scenario object");
                    }
                }

                if (scenariosJSON == null || scenariosJSON.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No scenarios found in JSON!", "OK");
                    return;
                }

                // Use the same loading logic as the main editor
                int importedCount = 0;
                foreach (var scenarioObj in scenariosJSON)
                {
                    try
                    {
                        var scenarioDict = scenarioObj as Dictionary<string, object>;
                        if (scenarioDict == null)
                        {
                            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(scenarioObj);
                            scenarioDict = jObject.ToObject<Dictionary<string, object>>();
                        }

                        var scenario = new ScenarioConfiguration();

                        // Load basic fields
                        if (scenarioDict.ContainsKey("id"))
                            scenario.id = scenarioDict["id"]?.ToString();

                        if (scenarioDict.ContainsKey("type"))
                        {
                            string typeStr = scenarioDict["type"]?.ToString();
                            if (System.Enum.TryParse<ScenarioType>(typeStr, true, out var type))
                                scenario.type = type;
                        }

                        // Load content based on type
                        switch (scenario.type)
                        {
                            case ScenarioType.Question:
                                // Support both "question" (single object) and "questions" (array)
                                if (scenarioDict.ContainsKey("question"))
                                {
                                    // Single question
                                    scenario.questions.Clear();
                                    var questionData = new QuestionScenarioData();
                                    LoadQuestionDataFromJSON(questionData, scenarioDict["question"]);
                                    scenario.questions.Add(questionData);
                                    targetData.scenarios.Add(scenario);
                                    importedCount++;
                                }
                                else if (scenarioDict.ContainsKey("questions"))
                                {
                                    // Multiple questions in one scenario
                                    var questionsArray = scenarioDict["questions"] as Newtonsoft.Json.Linq.JArray;
                                    if (questionsArray != null && questionsArray.Count > 0)
                                    {
                                        scenario.questions.Clear();
                                        foreach (var questionObj in questionsArray)
                                        {
                                            var questionData = new QuestionScenarioData();
                                            LoadQuestionDataFromJSON(questionData, questionObj);
                                            scenario.questions.Add(questionData);
                                        }
                                        targetData.scenarios.Add(scenario);
                                        importedCount++;
                                    }
                                }
                                break;

                            case ScenarioType.Procedure:
                                if (scenarioDict.ContainsKey("procedure"))
                                {
                                    LoadProcedureDataFromJSON(scenario.procedureData, scenarioDict["procedure"]);
                                    targetData.scenarios.Add(scenario);
                                    importedCount++;
                                }
                                break;

                            case ScenarioType.Text:
                                if (scenarioDict.ContainsKey("text"))
                                {
                                    LoadTextDataFromJSON(scenario.textData, scenarioDict["text"]);
                                    targetData.scenarios.Add(scenario);
                                    importedCount++;
                                }
                                break;

                            case ScenarioType.Dialogue:
                                if (scenarioDict.ContainsKey("dialogue"))
                                {
                                    LoadDialogueDataFromJSON(scenario.dialogueData, scenarioDict["dialogue"]);
                                    targetData.scenarios.Add(scenario);
                                    importedCount++;
                                }
                                break;
                        }

                        // Note: importedCount is incremented inside each case now
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"⚠️ Failed to import scenario: {e.Message}");
                    }
                }

                EditorUtility.DisplayDialog("Success",
                    $"Successfully imported {importedCount} scenario(s)!\n\nThey have been added to your scenario list.",
                    "OK");

                Close();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Error",
                    $"Failed to parse JSON:\n\n{e.Message}\n\nPlease check your JSON format.",
                    "OK");
            }
        }

        // ============= JSON LOADING HELPERS =============

        private void LoadQuestionDataFromJSON(QuestionScenarioData question, object questionObj)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(questionObj);
            var questionDict = jObject.ToObject<Dictionary<string, object>>();

            // Load question text
            if (questionDict.ContainsKey("questionText"))
            {
                var textDict = GetDictionary(questionDict["questionText"]);
                question.questionTextEN = GetString(textDict, "en");
                question.questionTextFR = GetString(textDict, "fr");
            }

            // Load options
            if (questionDict.ContainsKey("options"))
            {
                var optionsDict = GetDictionary(questionDict["options"]);
                question.optionsEN = GetStringList(optionsDict, "en");
                question.optionsFR = GetStringList(optionsDict, "fr");
            }

            // Load correct answers
            if (questionDict.ContainsKey("correctAnswers"))
            {
                var correctAnswersObj = questionDict["correctAnswers"];
                if (correctAnswersObj is Newtonsoft.Json.Linq.JArray jArray)
                {
                    question.correctAnswers = jArray.ToObject<List<int>>();
                }
            }

            // Load flags
            if (questionDict.ContainsKey("isMultipleChoice"))
                question.isMultipleChoice = System.Convert.ToBoolean(questionDict["isMultipleChoice"]);

            // Load feedback
            if (questionDict.ContainsKey("feedback"))
            {
                var feedbackDict = GetDictionary(questionDict["feedback"]);
                question.feedbackEN = GetString(feedbackDict, "en");
                question.feedbackFR = GetString(feedbackDict, "fr");
            }

            if (questionDict.ContainsKey("incorrectFeedback"))
            {
                var feedbackDict = GetDictionary(questionDict["incorrectFeedback"]);
                question.incorrectFeedbackEN = GetString(feedbackDict, "en");
                question.incorrectFeedbackFR = GetString(feedbackDict, "fr");
            }

            // Load hint (reset to empty if not present)
            if (questionDict.ContainsKey("hint"))
            {
                var hintDict = GetDictionary(questionDict["hint"]);
                question.hintEN = GetString(hintDict, "en");
                question.hintFR = GetString(hintDict, "fr");
            }
            else
            {
                question.hintEN = "";
                question.hintFR = "";
            }
        }

        private void LoadProcedureDataFromJSON(ProcedureScenarioData procedure, object procedureObj)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(procedureObj);
            var procedureDict = jObject.ToObject<Dictionary<string, object>>();

            // Load title
            if (procedureDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(procedureDict["title"]);
                procedure.titleEN = GetString(titleDict, "en");
                procedure.titleFR = GetString(titleDict, "fr");
            }

            // Load description
            if (procedureDict.ContainsKey("description"))
            {
                var descDict = GetDictionary(procedureDict["description"]);
                procedure.descriptionEN = GetString(descDict, "en");
                procedure.descriptionFR = GetString(descDict, "fr");
            }

            // Load steps
            if (procedureDict.ContainsKey("steps"))
            {
                var stepsArray = procedureDict["steps"] as Newtonsoft.Json.Linq.JArray;
                if (stepsArray != null)
                {
                    foreach (var stepObj in stepsArray)
                    {
                        var stepDict = ((Newtonsoft.Json.Linq.JObject)stepObj).ToObject<Dictionary<string, object>>();
                        var step = new ProcedureStep();

                        if (stepDict.ContainsKey("text"))
                        {
                            var textDict = GetDictionary(stepDict["text"]);
                            step.textEN = GetString(textDict, "en");
                            step.textFR = GetString(textDict, "fr");
                        }

                        if (stepDict.ContainsKey("targetObjectName"))
                            step.targetObjectName = stepDict["targetObjectName"]?.ToString();

                        if (stepDict.ContainsKey("highlightColor"))
                        {
                            string colorStr = stepDict["highlightColor"]?.ToString();
                            if (UnityEngine.ColorUtility.TryParseHtmlString(colorStr, out var color))
                                step.highlightColor = color;
                        }

                        if (stepDict.ContainsKey("useBlinking"))
                            step.useBlinking = System.Convert.ToBoolean(stepDict["useBlinking"]);

                        // Load validation type (with backward compat for requireManualValidation)
                        if (stepDict.ContainsKey("validationType"))
                        {
                            string valTypeStr = stepDict["validationType"]?.ToString();
                            if (System.Enum.TryParse<ValidationType>(valTypeStr, true, out var valType))
                                step.validationType = valType;
                        }
                        else if (stepDict.ContainsKey("requireManualValidation"))
                        {
                            step.validationType = System.Convert.ToBoolean(stepDict["requireManualValidation"])
                                ? ValidationType.Manual
                                : ValidationType.Click;
                        }

                        if (stepDict.ContainsKey("zoneObjectName"))
                            step.zoneObjectName = stepDict["zoneObjectName"]?.ToString();

                        if (stepDict.ContainsKey("imagePath"))
                        {
                            var imagePathDict = GetDictionary(stepDict["imagePath"]);
                            step.imagePathEN = GetString(imagePathDict, "en");
                            step.imagePathFR = GetString(imagePathDict, "fr");
                            // Note: We can't load Sprite objects from paths during import,
                            // they need to be manually reassigned in the editor
                        }

                        if (stepDict.ContainsKey("hint"))
                        {
                            var hintDict = GetDictionary(stepDict["hint"]);
                            step.hintEN = GetString(hintDict, "en");
                            step.hintFR = GetString(hintDict, "fr");
                        }
                        else
                        {
                            step.hintEN = "";
                            step.hintFR = "";
                        }

                        // NEW: Load fake objects for this step
                        if (stepDict.ContainsKey("fakeObjects"))
                        {
                            var fakesArray = stepDict["fakeObjects"] as Newtonsoft.Json.Linq.JArray;
                            if (fakesArray != null)
                            {
                                foreach (var fakeObj in fakesArray)
                                {
                                    var fakeDict = ((Newtonsoft.Json.Linq.JObject)fakeObj).ToObject<Dictionary<string, object>>();
                                    var fake = new FakeObject();

                                    if (fakeDict.ContainsKey("objectName"))
                                        fake.fakeObjectName = fakeDict["objectName"]?.ToString();

                                    if (fakeDict.ContainsKey("errorMessage"))
                                    {
                                        var errorDict = GetDictionary(fakeDict["errorMessage"]);
                                        fake.errorMessageEN = GetString(errorDict, "en");
                                        fake.errorMessageFR = GetString(errorDict, "fr");
                                    }

                                    step.fakeObjects.Add(fake);
                                }
                            }
                        }

                        procedure.steps.Add(step);
                    }
                }
            }

            // Load fake objects
            if (procedureDict.ContainsKey("fakeObjects"))
            {
                var fakesArray = procedureDict["fakeObjects"] as Newtonsoft.Json.Linq.JArray;
                if (fakesArray != null)
                {
                    foreach (var fakeObj in fakesArray)
                    {
                        var fakeDict = ((Newtonsoft.Json.Linq.JObject)fakeObj).ToObject<Dictionary<string, object>>();
                        var fake = new FakeObject();

                        if (fakeDict.ContainsKey("objectName"))
                            fake.fakeObjectName = fakeDict["objectName"]?.ToString();

                        if (fakeDict.ContainsKey("errorMessage"))
                        {
                            var errorDict = GetDictionary(fakeDict["errorMessage"]);
                            fake.errorMessageEN = GetString(errorDict, "en");
                            fake.errorMessageFR = GetString(errorDict, "fr");
                        }

                        procedure.fakeObjects.Add(fake);
                    }
                }
            }
        }

        private void LoadTextDataFromJSON(TextScenarioData text, object textObj)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(textObj);
            var textDict = jObject.ToObject<Dictionary<string, object>>();

            // Load title
            if (textDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(textDict["title"]);
                text.titleEN = GetString(titleDict, "en");
                text.titleFR = GetString(titleDict, "fr");
            }

            // Load content
            if (textDict.ContainsKey("content"))
            {
                var contentDict = GetDictionary(textDict["content"]);
                text.contentEN = GetString(contentDict, "en");
                text.contentFR = GetString(contentDict, "fr");
            }
        }

        private void LoadDialogueDataFromJSON(DialogueScenarioData dialogue, object dialogueObj)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dialogueObj);
            var dialogueDict = jObject.ToObject<Dictionary<string, object>>();

            // Load title
            if (dialogueDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(dialogueDict["title"]);
                dialogue.titleEN = GetString(titleDict, "en");
                dialogue.titleFR = GetString(titleDict, "fr");
            }

            // Store the entire dialogue JSON for the graph editor
            dialogue.graphDataJSON = jObject.ToString(Newtonsoft.Json.Formatting.Indented);

            // Generate a dialogue ID if not already set
            if (string.IsNullOrEmpty(dialogue.dialogueId))
            {
                dialogue.dialogueId = $"dialogue_{targetData.dialogues.Count + 1}";
            }
        }

        private Dictionary<string, object> GetDictionary(object obj)
        {
            if (obj == null) return new Dictionary<string, object>();

            if (obj is Dictionary<string, object> dict)
                return dict;

            if (obj is Newtonsoft.Json.Linq.JObject jObj)
                return jObj.ToObject<Dictionary<string, object>>();

            return new Dictionary<string, object>();
        }

        private string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.ContainsKey(key))
                return dict[key]?.ToString() ?? "";
            return "";
        }

        private List<string> GetStringList(Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.ContainsKey(key))
            {
                var value = dict[key];
                if (value is Newtonsoft.Json.Linq.JArray jArray)
                {
                    return jArray.ToObject<List<string>>();
                }
                else if (value is List<object> objList)
                {
                    return objList.Select(o => o?.ToString() ?? "").ToList();
                }
            }
            return new List<string>();
        }
    }
}

#endif
