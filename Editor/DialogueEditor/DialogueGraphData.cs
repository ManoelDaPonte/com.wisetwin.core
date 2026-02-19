using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

namespace WiseTwin.Editor.DialogueEditor
{
    /// <summary>
    /// Editor-side data model for the dialogue graph.
    /// Stores nodes, edges, and positions for the visual editor.
    /// Serialized to/from JSON for persistence.
    /// </summary>

    [Serializable]
    public class DialogueGraphEditorData
    {
        public List<DialogueNodeEditorData> nodes = new List<DialogueNodeEditorData>();
        public List<DialogueEdgeData> edges = new List<DialogueEdgeData>();
    }

    [Serializable]
    public class DialogueNodeEditorData
    {
        public string id;
        public string type; // "start", "dialogue", "choice", "end"
        public Vector2 position;

        // Dialogue node fields
        public string speakerEN = "";
        public string speakerFR = "";
        public string textEN = "";
        public string textFR = "";

        // Choice node fields
        public string promptTextEN = "";
        public string promptTextFR = "";
        public List<DialogueChoiceEditorData> choices = new List<DialogueChoiceEditorData>();
    }

    [Serializable]
    public class DialogueChoiceEditorData
    {
        public string id;
        public string textEN = "";
        public string textFR = "";
        public bool isCorrect = false;

        // The port name for this choice (used for edge mapping)
        public string portName;
    }

    [Serializable]
    public class DialogueEdgeData
    {
        public string fromNodeId;
        public string fromPortName; // "output" for start/dialogue, "choice_0", "choice_1" etc for choice nodes
        public string toNodeId;
        public string toPortName; // always "input"
    }
}

#endif
