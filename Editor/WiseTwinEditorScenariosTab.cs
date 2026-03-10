using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Scenarios Configuration tab for WiseTwinEditor
    /// Improved UX with color-coded types, collapsible sections, question reordering, and better visual hierarchy
    /// </summary>
    public static class WiseTwinEditorScenariosTab
    {
        // Color scheme per scenario type
        private static readonly Color QuestionColor = new Color(0.35f, 0.55f, 0.95f);    // Blue
        private static readonly Color ProcedureColor = new Color(0.3f, 0.75f, 0.45f);     // Green
        private static readonly Color TextColor = new Color(0.95f, 0.65f, 0.25f);         // Orange
        private static readonly Color DialogueColor = new Color(0.7f, 0.45f, 0.9f);       // Purple

        // Foldout state tracking (persists during editor session)
        private static Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        // Accordion group tracking: only one section open per group
        private static Dictionary<string, string> _accordionStates = new Dictionary<string, string>();

        private static bool GetFoldout(string key, bool defaultValue = false)
        {
            if (!_foldoutStates.ContainsKey(key)) _foldoutStates[key] = defaultValue;
            return _foldoutStates[key];
        }
        private static void SetFoldout(string key, bool value) => _foldoutStates[key] = value;

        /// <summary>
        /// Accordion: only one section open per group. Returns true if this section is the open one.
        /// </summary>
        private static bool IsAccordionOpen(string group, string section)
        {
            if (!_accordionStates.ContainsKey(group)) _accordionStates[group] = section;
            return _accordionStates[group] == section;
        }
        private static void SetAccordionOpen(string group, string section)
        {
            _accordionStates[group] = section;
        }
        private static void ToggleAccordion(string group, string section)
        {
            if (_accordionStates.ContainsKey(group) && _accordionStates[group] == section)
                _accordionStates[group] = ""; // close all
            else
                _accordionStates[group] = section;
        }

        private static Color GetTypeColor(ScenarioType type)
        {
            switch (type)
            {
                case ScenarioType.Question: return QuestionColor;
                case ScenarioType.Procedure: return ProcedureColor;
                case ScenarioType.Text: return TextColor;
                case ScenarioType.Dialogue: return DialogueColor;
                default: return Color.gray;
            }
        }

        private static string GetTypeIcon(ScenarioType type)
        {
            switch (type)
            {
                case ScenarioType.Question: return "?";
                case ScenarioType.Procedure: return "#";
                case ScenarioType.Text: return "T";
                case ScenarioType.Dialogue: return "D";
                default: return " ";
            }
        }

        private static string GetScenarioSummary(ScenarioConfiguration scenario)
        {
            switch (scenario.type)
            {
                case ScenarioType.Question:
                    int qCount = scenario.questions?.Count ?? 0;
                    return $"{qCount} question{(qCount > 1 ? "s" : "")}";
                case ScenarioType.Procedure:
                    int sCount = scenario.procedureData?.steps?.Count ?? 0;
                    string title = !string.IsNullOrEmpty(scenario.procedureData?.titleFR)
                        ? scenario.procedureData.titleFR
                        : scenario.procedureData?.titleEN ?? "";
                    if (title.Length > 30) title = title.Substring(0, 27) + "...";
                    return $"{sCount} step{(sCount > 1 ? "s" : "")}" + (title != "" ? $" - {title}" : "");
                case ScenarioType.Text:
                    string tTitle = !string.IsNullOrEmpty(scenario.textData?.titleFR)
                        ? scenario.textData.titleFR
                        : scenario.textData?.titleEN ?? "";
                    if (tTitle.Length > 40) tTitle = tTitle.Substring(0, 37) + "...";
                    return tTitle != "" ? tTitle : "(empty)";
                case ScenarioType.Dialogue:
                    string dTitle = !string.IsNullOrEmpty(scenario.dialogueData?.titleFR)
                        ? scenario.dialogueData.titleFR
                        : scenario.dialogueData?.titleEN ?? "";
                    if (dTitle.Length > 40) dTitle = dTitle.Substring(0, 37) + "...";
                    return dTitle != "" ? dTitle : "(empty)";
                default:
                    return "";
            }
        }

        public static void Draw(WiseTwinEditorData data)
        {
            // Header
            EditorGUILayout.LabelField("Scenario Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space(2);

            // Top toolbar
            EditorGUILayout.BeginHorizontal();

            // Add scenario dropdown
            if (GUILayout.Button("+ Add Scenario", GUILayout.Height(28), GUILayout.Width(140)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Question"), false, () => AddScenario(data, ScenarioType.Question));
                menu.AddItem(new GUIContent("Procedure"), false, () => AddScenario(data, ScenarioType.Procedure));
                menu.AddItem(new GUIContent("Text"), false, () => AddScenario(data, ScenarioType.Text));
                menu.AddItem(new GUIContent("Dialogue"), false, () => AddScenario(data, ScenarioType.Dialogue));
                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();

            // Legend
            DrawLegend();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (data.scenarios.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenarios yet. Click '+ Add Scenario' to get started.", MessageType.Info);
                return;
            }

            // Scenario list
            EditorGUILayout.LabelField($"Scenarios ({data.scenarios.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            for (int i = 0; i < data.scenarios.Count; i++)
            {
                DrawScenarioListItem(data, i);
            }

            EditorGUILayout.Space(8);

            // Edit selected scenario
            if (data.selectedScenarioIndex >= 0 && data.selectedScenarioIndex < data.scenarios.Count)
            {
                DrawSeparator();
                EditorGUILayout.Space(4);
                DrawScenarioEditor(data.scenarios[data.selectedScenarioIndex], data);
            }
        }

        private static void AddScenario(WiseTwinEditorData data, ScenarioType type)
        {
            var scenario = new ScenarioConfiguration();
            scenario.type = type;
            scenario.id = $"scenario_{data.scenarios.Count + 1}";
            data.scenarios.Add(scenario);
            data.selectedScenarioIndex = data.scenarios.Count - 1;
        }

        private static void DrawLegend()
        {
            var types = new[] { ScenarioType.Question, ScenarioType.Procedure, ScenarioType.Text, ScenarioType.Dialogue };
            foreach (var type in types)
            {
                Color c = GetTypeColor(type);
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = c;
                GUILayout.Label($" {GetTypeIcon(type)} {type} ", EditorStyles.miniButton, GUILayout.Height(18));
                GUI.backgroundColor = prevBg;
                GUILayout.Space(2);
            }
        }

        private static void DrawScenarioListItem(WiseTwinEditorData data, int index)
        {
            var scenario = data.scenarios[index];
            bool isSelected = (data.selectedScenarioIndex == index);
            Color typeColor = GetTypeColor(scenario.type);
            string summary = GetScenarioSummary(scenario);

            // Main row
            EditorGUILayout.BeginHorizontal();

            // Color bar (type indicator)
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = typeColor;
            GUILayout.Box("", GUILayout.Width(4), GUILayout.Height(28));
            GUI.backgroundColor = prevBg;

            // Move up
            GUI.enabled = index > 0;
            if (GUILayout.Button("^", GUILayout.Width(22), GUILayout.Height(28)))
            {
                SwapScenarios(data, index, index - 1);
            }
            GUI.enabled = true;

            // Move down
            GUI.enabled = index < data.scenarios.Count - 1;
            if (GUILayout.Button("v", GUILayout.Width(22), GUILayout.Height(28)))
            {
                SwapScenarios(data, index, index + 1);
            }
            GUI.enabled = true;

            // Main button (select/deselect)
            GUI.backgroundColor = isSelected ? new Color(typeColor.r, typeColor.g, typeColor.b, 0.3f) : Color.white;

            // Build label
            string typeTag = $"[{scenario.type}]";
            string label = isSelected
                ? $"  {index + 1}. {scenario.id}  {typeTag}  {summary}"
                : $"  {index + 1}. {scenario.id}  {typeTag}  {summary}";

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            btnStyle.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;

            if (GUILayout.Button(label, btnStyle, GUILayout.Height(28)))
            {
                data.selectedScenarioIndex = isSelected ? -1 : index;
            }
            GUI.backgroundColor = Color.white;

            // Duplicate button
            if (GUILayout.Button("C", GUILayout.Width(22), GUILayout.Height(28)))
            {
                DuplicateScenario(data, index);
            }

            // Delete button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Delete Scenario", $"Delete scenario '{scenario.id}'?", "Delete", "Cancel"))
                {
                    data.scenarios.RemoveAt(index);
                    if (data.selectedScenarioIndex >= data.scenarios.Count)
                        data.selectedScenarioIndex = data.scenarios.Count - 1;
                    else if (data.selectedScenarioIndex == index)
                        data.selectedScenarioIndex = -1;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void SwapScenarios(WiseTwinEditorData data, int from, int to)
        {
            var temp = data.scenarios[from];
            data.scenarios[from] = data.scenarios[to];
            data.scenarios[to] = temp;
            if (data.selectedScenarioIndex == from)
                data.selectedScenarioIndex = to;
            else if (data.selectedScenarioIndex == to)
                data.selectedScenarioIndex = from;
        }

        private static void DuplicateScenario(WiseTwinEditorData data, int index)
        {
            var original = data.scenarios[index];
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
            var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<ScenarioConfiguration>(json);
            copy.id = original.id + "_copy";
            data.scenarios.Insert(index + 1, copy);
            data.selectedScenarioIndex = index + 1;
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2), new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.Space(2);
        }

        // ============= SCENARIO EDITOR =============

        private static void DrawScenarioEditor(ScenarioConfiguration scenario, WiseTwinEditorData data)
        {
            Color typeColor = GetTypeColor(scenario.type);

            // Header with colored background
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.15f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField($"Edit: {scenario.id}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Basic info
            scenario.id = EditorGUILayout.TextField("Scenario ID", scenario.id);
            scenario.type = (ScenarioType)EditorGUILayout.EnumPopup("Type", scenario.type);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);

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

        // ============= QUESTIONS EDITOR =============

        private static void DrawQuestionsEditor(List<QuestionScenarioData> questions)
        {
            // Questions list with accordion header + add button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Questions ({questions.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Question", GUILayout.Height(22), GUILayout.Width(120)))
            {
                questions.Add(new QuestionScenarioData());
                // Auto-open the new question
                SetAccordionOpen("questions_accordion", $"q_{questions.Count - 1}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            for (int i = 0; i < questions.Count; i++)
            {
                DrawQuestionItem(questions, i);
                EditorGUILayout.Space(2);
            }
        }

        private static void DrawQuestionItem(List<QuestionScenarioData> questions, int index)
        {
            var question = questions[index];
            string accordionGroup = "questions_accordion";
            string sectionKey = $"q_{index}";

            // Question box with visible colored background
            bool isOpen = IsAccordionOpen(accordionGroup, sectionKey);
            Rect qRect = EditorGUILayout.BeginVertical("box");
            if (isOpen)
                EditorGUI.DrawRect(qRect, new Color(0.2f, 0.3f, 0.55f, 0.15f));

            EditorGUILayout.BeginHorizontal();
            string arrow = isOpen ? "v" : ">";
            string previewText = !string.IsNullOrEmpty(question.questionTextFR)
                ? question.questionTextFR
                : question.questionTextEN;
            if (previewText.Length > 60) previewText = previewText.Substring(0, 57) + "...";
            if (string.IsNullOrEmpty(previewText)) previewText = "(empty question)";

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.richText = true;

            if (GUILayout.Button($"{arrow}  Q{index + 1}: {previewText}", headerStyle))
            {
                ToggleAccordion(accordionGroup, sectionKey);
            }

            // Reorder buttons
            GUI.enabled = index > 0;
            if (GUILayout.Button("^", GUILayout.Width(22), GUILayout.Height(18)))
            {
                var temp = questions[index];
                questions[index] = questions[index - 1];
                questions[index - 1] = temp;
            }
            GUI.enabled = true;

            GUI.enabled = index < questions.Count - 1;
            if (GUILayout.Button("v", GUILayout.Width(22), GUILayout.Height(18)))
            {
                var temp = questions[index];
                questions[index] = questions[index + 1];
                questions[index + 1] = temp;
            }
            GUI.enabled = true;

            // Delete button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
            {
                if (questions.Count > 1 || EditorUtility.DisplayDialog("Delete Question", "Remove the last question from this scenario?", "Delete", "Cancel"))
                {
                    questions.RemoveAt(index);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Accordion content - only one question open at a time
            if (IsAccordionOpen(accordionGroup, sectionKey))
            {
                EditorGUILayout.Space(2);
                DrawQuestionFields(question);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawQuestionFields(QuestionScenarioData question)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

            // Question text
            EditorGUILayout.LabelField("Question Text", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
            question.questionTextEN = EditorGUILayout.TextArea(question.questionTextEN, textAreaStyle, GUILayout.Height(50));
            EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
            question.questionTextFR = EditorGUILayout.TextArea(question.questionTextFR, textAreaStyle, GUILayout.Height(50));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            // Multiple choice toggle
            question.isMultipleChoice = EditorGUILayout.Toggle("Multiple Choice", question.isMultipleChoice);

            EditorGUILayout.Space(4);

            // Options
            EditorGUILayout.LabelField("Answer Options", EditorStyles.miniBoldLabel);
            int optionsCount = Mathf.Max(question.optionsEN.Count, question.optionsFR.Count);

            for (int i = 0; i < optionsCount; i++)
            {
                while (question.optionsEN.Count <= i) question.optionsEN.Add("");
                while (question.optionsFR.Count <= i) question.optionsFR.Add("");

                bool isCorrect = question.correctAnswers.Contains(i);

                // Option row with colored indicator
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = isCorrect ? new Color(0.3f, 0.85f, 0.4f, 0.3f) : Color.white;
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = prevBg;

                EditorGUILayout.BeginHorizontal();

                // Correct toggle
                bool newIsCorrect = EditorGUILayout.Toggle(isCorrect, GUILayout.Width(16));
                if (newIsCorrect != isCorrect)
                {
                    if (newIsCorrect)
                    {
                        if (!question.isMultipleChoice) question.correctAnswers.Clear();
                        question.correctAnswers.Add(i);
                    }
                    else
                    {
                        question.correctAnswers.Remove(i);
                    }
                }

                EditorGUILayout.LabelField(isCorrect ? $"Option {i + 1} (correct)" : $"Option {i + 1}",
                    isCorrect ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(130));

                GUILayout.FlexibleSpace();

                // Delete option
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (optionsCount > 2 && GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
                {
                    question.optionsEN.RemoveAt(i);
                    question.optionsFR.RemoveAt(i);
                    question.correctAnswers.Remove(i);
                    for (int j = 0; j < question.correctAnswers.Count; j++)
                    {
                        if (question.correctAnswers[j] > i) question.correctAnswers[j]--;
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                question.optionsEN[i] = EditorGUILayout.TextField("EN", question.optionsEN[i]);
                question.optionsFR[i] = EditorGUILayout.TextField("FR", question.optionsFR[i]);
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Option", GUILayout.Height(20)))
            {
                question.optionsEN.Add("");
                question.optionsFR.Add("");
            }

            EditorGUILayout.Space(4);

            // Feedback - collapsible
            string fbKey = $"fb_{question.GetHashCode()}";
            bool fbOpen = GetFoldout(fbKey, false);
            if (GUILayout.Button(fbOpen ? "v Feedback Messages" : "> Feedback Messages", EditorStyles.boldLabel))
            {
                SetFoldout(fbKey, !fbOpen);
            }

            if (fbOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Correct Feedback", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
                question.feedbackEN = EditorGUILayout.TextArea(question.feedbackEN, textAreaStyle, GUILayout.Height(45));
                EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
                question.feedbackFR = EditorGUILayout.TextArea(question.feedbackFR, textAreaStyle, GUILayout.Height(45));

                EditorGUILayout.Space(2);

                EditorGUILayout.LabelField("Incorrect Feedback", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
                question.incorrectFeedbackEN = EditorGUILayout.TextArea(question.incorrectFeedbackEN, textAreaStyle, GUILayout.Height(45));
                EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
                question.incorrectFeedbackFR = EditorGUILayout.TextArea(question.incorrectFeedbackFR, textAreaStyle, GUILayout.Height(45));
                EditorGUI.indentLevel--;
            }
        }

        // ============= PROCEDURE EDITOR =============

        private static void DrawProcedureEditor(ProcedureScenarioData procedure)
        {
            string accordionGroup = "proc_accordion";

            // === GENERAL SECTION (accordion) ===
            bool generalOpen = IsAccordionOpen(accordionGroup, "general");
            DrawAccordionHeader("General (Title & Description)", generalOpen, () => ToggleAccordion(accordionGroup, "general"));

            if (generalOpen)
            {
                Rect genRect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(genRect, new Color(0.2f, 0.4f, 0.25f, 0.15f));

                EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                procedure.titleEN = EditorGUILayout.TextField("EN", procedure.titleEN);
                procedure.titleFR = EditorGUILayout.TextField("FR", procedure.titleFR);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(2);

                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
                procedure.descriptionEN = EditorGUILayout.TextArea(procedure.descriptionEN, textAreaStyle, GUILayout.Height(45));
                EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
                procedure.descriptionFR = EditorGUILayout.TextArea(procedure.descriptionFR, textAreaStyle, GUILayout.Height(45));
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);

            // === STEPS SECTION (accordion) ===
            bool stepsOpen = IsAccordionOpen(accordionGroup, "steps");
            EditorGUILayout.BeginHorizontal();
            DrawAccordionHeader($"Steps ({procedure.steps.Count})", stepsOpen, () => ToggleAccordion(accordionGroup, "steps"));
            if (stepsOpen && GUILayout.Button("+ Add Step", GUILayout.Height(22), GUILayout.Width(100)))
            {
                procedure.steps.Add(new ProcedureStep());
            }
            EditorGUILayout.EndHorizontal();

            if (stepsOpen)
            {
                EditorGUILayout.Space(2);
                for (int i = 0; i < procedure.steps.Count; i++)
                {
                    DrawProcedureStep(procedure.steps[i], i, procedure.steps);
                }
            }
        }

        private static void DrawAccordionHeader(string title, bool isOpen, System.Action onClick)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = isOpen ? new Color(0.45f, 0.65f, 0.85f) : new Color(0.55f, 0.55f, 0.55f);
            GUIStyle headerStyle = new GUIStyle(GUI.skin.button);
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 12;

            if (GUILayout.Button($"  {(isOpen ? "v" : ">")}  {title}", headerStyle, GUILayout.Height(26)))
            {
                onClick?.Invoke();
            }
            GUI.backgroundColor = prevBg;
        }

        private static void DrawProcedureStep(ProcedureStep step, int index, List<ProcedureStep> steps)
        {
            string accordionGroup = "steps_accordion";
            string sectionKey = $"step_{index}";
            bool isOpen = IsAccordionOpen(accordionGroup, sectionKey);

            // Preview text
            string preview = !string.IsNullOrEmpty(step.textFR) ? step.textFR : step.textEN;
            if (preview.Length > 50) preview = preview.Substring(0, 47) + "...";
            if (string.IsNullOrEmpty(preview)) preview = "(empty step)";

            string valBadge = step.validationType == ValidationType.Click ? "[Click]" :
                              step.validationType == ValidationType.Manual ? "[Manual]" : "[Zone]";

            // Box with visible colored background
            Rect stepRect = EditorGUILayout.BeginVertical("box");
            if (isOpen)
                EditorGUI.DrawRect(stepRect, new Color(0.2f, 0.4f, 0.25f, 0.15f));

            // Header
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button($"{(isOpen ? "v" : ">")}  Step {index + 1} {valBadge}: {preview}", EditorStyles.boldLabel))
            {
                ToggleAccordion(accordionGroup, sectionKey);
            }

            // Reorder
            GUI.enabled = index > 0;
            if (GUILayout.Button("^", GUILayout.Width(22), GUILayout.Height(18)))
            {
                var temp = steps[index]; steps[index] = steps[index - 1]; steps[index - 1] = temp;
            }
            GUI.enabled = true;

            GUI.enabled = index < steps.Count - 1;
            if (GUILayout.Button("v", GUILayout.Width(22), GUILayout.Height(18)))
            {
                var temp = steps[index]; steps[index] = steps[index + 1]; steps[index + 1] = temp;
            }
            GUI.enabled = true;

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(18)))
            {
                steps.RemoveAt(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Expanded content
            if (isOpen)
            {
                EditorGUILayout.Space(2);

                // Validation type FIRST so user picks mode before seeing irrelevant fields
                step.validationType = (ValidationType)EditorGUILayout.EnumPopup("Validation", step.validationType);

                // Target Object (only for Click - for Manual/Zone it's not relevant)
                if (step.validationType == ValidationType.Click)
                {
                    EditorGUI.BeginChangeCheck();
                    step.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", step.targetObject, typeof(GameObject), true);
                    if (EditorGUI.EndChangeCheck() && step.targetObject != null)
                        step.targetObjectName = step.targetObject.name;
                    step.targetObjectName = EditorGUILayout.TextField("Object Name", step.targetObjectName);
                }

                // Zone fields
                if (step.validationType == ValidationType.Zone)
                {
                    EditorGUI.BeginChangeCheck();
                    step.zoneObject = (GameObject)EditorGUILayout.ObjectField("Zone Object", step.zoneObject, typeof(GameObject), true);
                    if (EditorGUI.EndChangeCheck() && step.zoneObject != null)
                        step.zoneObjectName = step.zoneObject.name;
                    step.zoneObjectName = EditorGUILayout.TextField("Zone Name", step.zoneObjectName);
                }

                // Step text
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUILayout.LabelField("Text", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
                step.textEN = EditorGUILayout.TextArea(step.textEN, textAreaStyle, GUILayout.Height(40));
                EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
                step.textFR = EditorGUILayout.TextArea(step.textFR, textAreaStyle, GUILayout.Height(40));
                EditorGUI.indentLevel--;

                // Highlight, Blink, Images, Fake Objects: only for Click validation
                if (step.validationType == ValidationType.Click)
                {
                    EditorGUILayout.Space(2);

                    // Highlight
                    EditorGUILayout.BeginHorizontal();
                    step.highlightColor = EditorGUILayout.ColorField("Highlight", step.highlightColor, GUILayout.Width(250));
                    step.useBlinking = EditorGUILayout.Toggle("Blink", step.useBlinking);
                    EditorGUILayout.EndHorizontal();

                    // Images (collapsible)
                    string imgKey = $"img_{index}_{step.GetHashCode()}";
                    bool imgOpen = GetFoldout(imgKey, false);
                    if (GUILayout.Button(imgOpen ? "v Images (Optional)" : "> Images (Optional)", EditorStyles.miniLabel))
                    {
                        SetFoldout(imgKey, !imgOpen);
                    }
                    if (imgOpen)
                    {
                        EditorGUI.indentLevel++;
                        step.imageEN = (Sprite)EditorGUILayout.ObjectField("Image EN", step.imageEN, typeof(Sprite), false);
                        step.imageFR = (Sprite)EditorGUILayout.ObjectField("Image FR", step.imageFR, typeof(Sprite), false);
                        if (step.imageEN != null) step.imagePathEN = AssetDatabase.GetAssetPath(step.imageEN);
                        if (step.imageFR != null) step.imagePathFR = AssetDatabase.GetAssetPath(step.imageFR);
                        EditorGUI.indentLevel--;
                    }

                    // Fake objects (collapsible)
                    string sfKey = $"sfake_{index}_{step.GetHashCode()}";
                    bool sfOpen = GetFoldout(sfKey, false);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(sfOpen ? $"v Fake Objects ({step.fakeObjects.Count})" : $"> Fake Objects ({step.fakeObjects.Count})", EditorStyles.miniLabel))
                    {
                        SetFoldout(sfKey, !sfOpen);
                    }
                    if (sfOpen && GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(14)))
                    {
                        step.fakeObjects.Add(new FakeObject());
                    }
                    EditorGUILayout.EndHorizontal();

                    if (sfOpen)
                    {
                        for (int i = 0; i < step.fakeObjects.Count; i++)
                        {
                            DrawFakeObject(step.fakeObjects[i], i, step.fakeObjects);
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        private static void DrawFakeObject(FakeObject fake, int index, List<FakeObject> fakes)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Fake #{index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
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
                fake.fakeObjectName = fake.fakeObject.name;
            fake.fakeObjectName = EditorGUILayout.TextField("Object Name", fake.fakeObjectName);
            fake.errorMessageEN = EditorGUILayout.TextField("Error EN", fake.errorMessageEN);
            fake.errorMessageFR = EditorGUILayout.TextField("Error FR", fake.errorMessageFR);

            EditorGUILayout.EndVertical();
        }

        // ============= TEXT EDITOR =============

        private static void DrawTextEditor(TextScenarioData text)
        {
            string accordionGroup = "text_accordion";

            // === GENERAL SECTION ===
            bool generalOpen = IsAccordionOpen(accordionGroup, "general");
            DrawAccordionHeader("General (Title)", generalOpen, () => ToggleAccordion(accordionGroup, "general"));

            if (generalOpen)
            {
                Rect tgRect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(tgRect, new Color(0.55f, 0.35f, 0.1f, 0.15f));

                EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                text.titleEN = EditorGUILayout.TextField("EN", text.titleEN);
                text.titleFR = EditorGUILayout.TextField("FR", text.titleFR);
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);

            // === CONTENT SECTION ===
            bool contentOpen = IsAccordionOpen(accordionGroup, "content");
            DrawAccordionHeader("Content", contentOpen, () => ToggleAccordion(accordionGroup, "content"));

            if (contentOpen)
            {
                Rect tcRect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(tcRect, new Color(0.55f, 0.35f, 0.1f, 0.15f));

                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("EN", EditorStyles.miniLabel);
                text.contentEN = EditorGUILayout.TextArea(text.contentEN, textAreaStyle, GUILayout.Height(120));
                EditorGUILayout.LabelField("FR", EditorStyles.miniLabel);
                text.contentFR = EditorGUILayout.TextArea(text.contentFR, textAreaStyle, GUILayout.Height(120));
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }
        }

        // ============= DIALOGUE EDITOR =============

        private static void DrawDialogueEditor(DialogueScenarioData dialogue, WiseTwinEditorData data)
        {
            string accordionGroup = "dialogue_accordion";

            // === GENERAL SECTION ===
            bool generalOpen = IsAccordionOpen(accordionGroup, "general");
            DrawAccordionHeader("General (Title & Link)", generalOpen, () => ToggleAccordion(accordionGroup, "general"));

            if (generalOpen)
            {
                Rect dgRect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(dgRect, new Color(0.35f, 0.2f, 0.5f, 0.15f));

                // Select existing dialogue
                if (data.dialogues.Count > 0)
                {
                    string[] options = new string[data.dialogues.Count + 1];
                    options[0] = "(None - configure inline)";
                    int currentSelection = 0;

                    for (int i = 0; i < data.dialogues.Count; i++)
                    {
                        var d = data.dialogues[i];
                        string title = !string.IsNullOrEmpty(d.titleEN) ? d.titleEN : d.dialogueId;
                        options[i + 1] = $"{d.dialogueId} - {title}";
                        if (d.dialogueId == dialogue.dialogueId) currentSelection = i + 1;
                    }

                    int newSelection = EditorGUILayout.Popup("Link to Dialogue", currentSelection, options);
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

                EditorGUILayout.Space(2);

                EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                dialogue.titleEN = EditorGUILayout.TextField("EN", dialogue.titleEN);
                dialogue.titleFR = EditorGUILayout.TextField("FR", dialogue.titleFR);
                EditorGUI.indentLevel--;

                dialogue.dialogueId = EditorGUILayout.TextField("Dialogue ID", dialogue.dialogueId);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);

            // === GRAPH EDITOR SECTION ===
            bool graphOpen = IsAccordionOpen(accordionGroup, "graph");
            DrawAccordionHeader("Graph Editor", graphOpen, () => ToggleAccordion(accordionGroup, "graph"));

            if (graphOpen)
            {
                Rect grRect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(grRect, new Color(0.35f, 0.2f, 0.5f, 0.15f));

                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Open Graph Editor", GUILayout.Height(28)))
                {
                    WiseTwin.Editor.DialogueEditor.DialogueEditorWindow.OpenWithDialogue(dialogue);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(2);

                if (!string.IsNullOrEmpty(dialogue.graphDataJSON))
                    EditorGUILayout.HelpBox("Graph data configured. Open Graph Editor to modify.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("No graph data yet. Open Graph Editor to create a dialogue tree.", MessageType.Warning);

                EditorGUILayout.EndVertical();
            }
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
            EditorGUILayout.LabelField("Import Scenarios from JSON", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("Paste your scenarios JSON array below. You can import one or multiple scenarios at once.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paste from Clipboard"))
            {
                jsonContent = EditorGUIUtility.systemCopyBuffer;
            }
            if (GUILayout.Button("Clear"))
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
            if (GUILayout.Button("Import Scenarios", GUILayout.Height(35)))
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

                try
                {
                    scenariosJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(jsonContent);
                }
                catch
                {
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

                        if (scenarioDict.ContainsKey("id"))
                            scenario.id = scenarioDict["id"]?.ToString();

                        if (scenarioDict.ContainsKey("type"))
                        {
                            string typeStr = scenarioDict["type"]?.ToString();
                            if (System.Enum.TryParse<ScenarioType>(typeStr, true, out var type))
                                scenario.type = type;
                        }

                        switch (scenario.type)
                        {
                            case ScenarioType.Question:
                                if (scenarioDict.ContainsKey("question"))
                                {
                                    scenario.questions.Clear();
                                    var questionData = new QuestionScenarioData();
                                    LoadQuestionDataFromJSON(questionData, scenarioDict["question"]);
                                    scenario.questions.Add(questionData);
                                    targetData.scenarios.Add(scenario);
                                    importedCount++;
                                }
                                else if (scenarioDict.ContainsKey("questions"))
                                {
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
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ScenarioImport] Failed to import scenario: {e.Message}");
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

            if (questionDict.ContainsKey("questionText"))
            {
                var textDict = GetDictionary(questionDict["questionText"]);
                question.questionTextEN = GetString(textDict, "en");
                question.questionTextFR = GetString(textDict, "fr");
            }

            if (questionDict.ContainsKey("options"))
            {
                var optionsDict = GetDictionary(questionDict["options"]);
                question.optionsEN = GetStringList(optionsDict, "en");
                question.optionsFR = GetStringList(optionsDict, "fr");
            }

            if (questionDict.ContainsKey("correctAnswers"))
            {
                var correctAnswersObj = questionDict["correctAnswers"];
                if (correctAnswersObj is Newtonsoft.Json.Linq.JArray jArray)
                    question.correctAnswers = jArray.ToObject<List<int>>();
            }

            if (questionDict.ContainsKey("isMultipleChoice"))
                question.isMultipleChoice = System.Convert.ToBoolean(questionDict["isMultipleChoice"]);

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

            if (procedureDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(procedureDict["title"]);
                procedure.titleEN = GetString(titleDict, "en");
                procedure.titleFR = GetString(titleDict, "fr");
            }

            if (procedureDict.ContainsKey("description"))
            {
                var descDict = GetDictionary(procedureDict["description"]);
                procedure.descriptionEN = GetString(descDict, "en");
                procedure.descriptionFR = GetString(descDict, "fr");
            }

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
                            if (ColorUtility.TryParseHtmlString(colorStr, out var color))
                                step.highlightColor = color;
                        }

                        if (stepDict.ContainsKey("useBlinking"))
                            step.useBlinking = System.Convert.ToBoolean(stepDict["useBlinking"]);

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

            if (textDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(textDict["title"]);
                text.titleEN = GetString(titleDict, "en");
                text.titleFR = GetString(titleDict, "fr");
            }

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

            if (dialogueDict.ContainsKey("title"))
            {
                var titleDict = GetDictionary(dialogueDict["title"]);
                dialogue.titleEN = GetString(titleDict, "en");
                dialogue.titleFR = GetString(titleDict, "fr");
            }

            dialogue.graphDataJSON = jObject.ToString(Newtonsoft.Json.Formatting.Indented);

            if (string.IsNullOrEmpty(dialogue.dialogueId))
                dialogue.dialogueId = $"dialogue_{targetData.dialogues.Count + 1}";
        }

        private Dictionary<string, object> GetDictionary(object obj)
        {
            if (obj == null) return new Dictionary<string, object>();
            if (obj is Dictionary<string, object> dict) return dict;
            if (obj is Newtonsoft.Json.Linq.JObject jObj) return jObj.ToObject<Dictionary<string, object>>();
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
                    return jArray.ToObject<List<string>>();
                else if (value is List<object> objList)
                    return objList.Select(o => o?.ToString() ?? "").ToList();
            }
            return new List<string>();
        }
    }
}

#endif
