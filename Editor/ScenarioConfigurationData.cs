using System;
using System.Collections.Generic;
using UnityEngine;

namespace WiseTwin.Editor
{
    /// <summary>
    /// Data classes for scenario configuration in WiseTwinEditor
    /// </summary>

    [Serializable]
    public enum ScenarioType
    {
        Question,
        Procedure,
        Text
    }

    [Serializable]
    public class ScenarioConfiguration
    {
        public string id = "scenario_1";
        public ScenarioType type = ScenarioType.Question;

        // Question data - now supports multiple questions per scenario
        public List<QuestionScenarioData> questions = new List<QuestionScenarioData>();

        // Procedure data
        public ProcedureScenarioData procedureData = new ProcedureScenarioData();

        // Text data
        public TextScenarioData textData = new TextScenarioData();

        public ScenarioConfiguration()
        {
            // Initialize with one empty question by default
            questions = new List<QuestionScenarioData> { new QuestionScenarioData() };
            procedureData = new ProcedureScenarioData();
            textData = new TextScenarioData();
        }
    }

    [Serializable]
    public class QuestionScenarioData
    {
        public string questionTextEN = "";
        public string questionTextFR = "";
        public List<string> optionsEN = new List<string> { "Option 1", "Option 2" };
        public List<string> optionsFR = new List<string> { "Option 1", "Option 2" };
        public List<int> correctAnswers = new List<int> { 0 };
        public bool isMultipleChoice = false;
        public string feedbackEN = "";
        public string feedbackFR = "";
        public string incorrectFeedbackEN = "";
        public string incorrectFeedbackFR = "";
        public string hintEN = "";
        public string hintFR = "";
    }

    [Serializable]
    public class ProcedureScenarioData
    {
        public string titleEN = "";
        public string titleFR = "";
        public string descriptionEN = "";
        public string descriptionFR = "";
        public List<ProcedureStep> steps = new List<ProcedureStep>();
        public List<FakeObject> fakeObjects = new List<FakeObject>();
    }

    [Serializable]
    public class ProcedureStep
    {
        public string textEN = "";
        public string textFR = "";
        public GameObject targetObject = null;
        public string targetObjectName = "";
        public Color highlightColor = Color.yellow;
        public bool useBlinking = true;
        public string hintEN = "";
        public string hintFR = "";
        // NEW: Manual validation for this step
        public bool requireManualValidation = false;
        // NEW: Image support for this step
        public Sprite imageEN = null;  // Image for English
        public Sprite imageFR = null;  // Image for French
        public string imagePathEN = "";  // Path to store in JSON
        public string imagePathFR = "";  // Path to store in JSON
        // NEW: Fake objects specific to this step
        public List<FakeObject> fakeObjects = new List<FakeObject>();
    }

    [Serializable]
    public class FakeObject
    {
        public GameObject fakeObject = null;
        public string fakeObjectName = "";
        public string errorMessageEN = "Wrong object!";
        public string errorMessageFR = "Mauvais objet !";
    }

    [Serializable]
    public class TextScenarioData
    {
        public string titleEN = "";
        public string titleFR = "";
        public string contentEN = "";
        public string contentFR = "";
    }
}
