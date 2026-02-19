using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WiseTwin
{
    /// <summary>
    /// Runtime data structures for the dialogue system (branching conversation tree).
    /// Parsed from metadata JSON at runtime by DialogueDisplayer.
    /// </summary>

    [Serializable]
    public class DialogueTreeData
    {
        public string title_en;
        public string title_fr;
        public string startNodeId;
        public List<DialogueNodeRuntime> nodes;

        private Dictionary<string, DialogueNodeRuntime> nodeMap;

        public void BuildNodeMap()
        {
            nodeMap = new Dictionary<string, DialogueNodeRuntime>();
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.id))
                    nodeMap[node.id] = node;
            }
        }

        public DialogueNodeRuntime GetNode(string nodeId)
        {
            if (nodeMap == null) BuildNodeMap();
            if (nodeMap != null && nodeMap.TryGetValue(nodeId, out var node))
                return node;
            return null;
        }

        public DialogueNodeRuntime GetStartNode()
        {
            return GetNode(startNodeId);
        }

        public int CountChoiceNodes()
        {
            if (nodes == null) return 0;
            int count = 0;
            foreach (var node in nodes)
            {
                if (node.type == "choice") count++;
            }
            return count;
        }

        /// <summary>
        /// Parse a dialogue tree from a Dictionary (converted from JObject).
        /// </summary>
        public static DialogueTreeData FromDictionary(Dictionary<string, object> data)
        {
            var tree = new DialogueTreeData();

            // Parse title
            if (data.TryGetValue("title", out var titleObj))
            {
                var titleDict = ConvertToDict(titleObj);
                if (titleDict != null)
                {
                    tree.title_en = GetStr(titleDict, "en");
                    tree.title_fr = GetStr(titleDict, "fr");
                }
            }

            // Parse startNodeId
            if (data.TryGetValue("startNodeId", out var startObj))
                tree.startNodeId = startObj?.ToString();

            // Parse nodes
            tree.nodes = new List<DialogueNodeRuntime>();
            if (data.TryGetValue("nodes", out var nodesObj))
            {
                IEnumerable<object> nodesList = null;
                if (nodesObj is JArray jArray)
                    nodesList = jArray.ToObject<List<object>>();
                else if (nodesObj is List<object> list)
                    nodesList = list;

                if (nodesList != null)
                {
                    foreach (var nodeObj in nodesList)
                    {
                        var nodeDict = ConvertToDict(nodeObj);
                        if (nodeDict != null)
                            tree.nodes.Add(DialogueNodeRuntime.FromDictionary(nodeDict));
                    }
                }
            }

            tree.BuildNodeMap();
            return tree;
        }

        private static Dictionary<string, object> ConvertToDict(object obj)
        {
            if (obj is Dictionary<string, object> dict) return dict;
            if (obj is JObject jObj) return jObj.ToObject<Dictionary<string, object>>();
            return null;
        }

        private static string GetStr(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val)) return val?.ToString() ?? "";
            return "";
        }
    }

    [Serializable]
    public class DialogueNodeRuntime
    {
        public string id;
        public string type; // "start", "dialogue", "choice", "end"

        // For start and dialogue nodes
        public string nextNodeId;

        // For dialogue nodes
        public string speaker_en;
        public string speaker_fr;
        public string text_en;
        public string text_fr;

        // For choice nodes
        public string choiceText_en; // Prompt text
        public string choiceText_fr;
        public List<DialogueChoiceRuntime> choices;

        public static DialogueNodeRuntime FromDictionary(Dictionary<string, object> data)
        {
            var node = new DialogueNodeRuntime();

            node.id = GetStr(data, "id");
            node.type = GetStr(data, "type");
            node.nextNodeId = GetStr(data, "nextNodeId");

            // Parse speaker
            if (data.TryGetValue("speaker", out var speakerObj))
            {
                var speakerDict = ConvertToDict(speakerObj);
                if (speakerDict != null)
                {
                    node.speaker_en = GetStr(speakerDict, "en");
                    node.speaker_fr = GetStr(speakerDict, "fr");
                }
            }

            // Parse text
            if (data.TryGetValue("text", out var textObj))
            {
                var textDict = ConvertToDict(textObj);
                if (textDict != null)
                {
                    node.text_en = GetStr(textDict, "en");
                    node.text_fr = GetStr(textDict, "fr");
                }
                // For choice nodes, this is the prompt text
                if (node.type == "choice")
                {
                    node.choiceText_en = node.text_en;
                    node.choiceText_fr = node.text_fr;
                }
            }

            // Parse choices (for choice nodes)
            if (data.TryGetValue("choices", out var choicesObj))
            {
                node.choices = new List<DialogueChoiceRuntime>();

                IEnumerable<object> choicesList = null;
                if (choicesObj is JArray jArray)
                    choicesList = jArray.ToObject<List<object>>();
                else if (choicesObj is List<object> list)
                    choicesList = list;

                if (choicesList != null)
                {
                    foreach (var choiceObj in choicesList)
                    {
                        var choiceDict = ConvertToDict(choiceObj);
                        if (choiceDict != null)
                            node.choices.Add(DialogueChoiceRuntime.FromDictionary(choiceDict));
                    }
                }
            }

            return node;
        }

        private static Dictionary<string, object> ConvertToDict(object obj)
        {
            if (obj is Dictionary<string, object> dict) return dict;
            if (obj is JObject jObj) return jObj.ToObject<Dictionary<string, object>>();
            return null;
        }

        private static string GetStr(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val)) return val?.ToString() ?? "";
            return "";
        }
    }

    [Serializable]
    public class DialogueChoiceRuntime
    {
        public string id;
        public string text_en;
        public string text_fr;
        public bool isCorrect;
        public string nextNodeId;

        public static DialogueChoiceRuntime FromDictionary(Dictionary<string, object> data)
        {
            var choice = new DialogueChoiceRuntime();

            choice.id = GetStr(data, "id");
            choice.nextNodeId = GetStr(data, "nextNodeId");

            // Parse isCorrect
            if (data.TryGetValue("isCorrect", out var correctObj))
            {
                if (correctObj is bool b)
                    choice.isCorrect = b;
                else
                    bool.TryParse(correctObj?.ToString(), out choice.isCorrect);
            }

            // Parse text
            if (data.TryGetValue("text", out var textObj))
            {
                var textDict = ConvertToDict(textObj);
                if (textDict != null)
                {
                    choice.text_en = GetStr(textDict, "en");
                    choice.text_fr = GetStr(textDict, "fr");
                }
            }

            return choice;
        }

        private static Dictionary<string, object> ConvertToDict(object obj)
        {
            if (obj is Dictionary<string, object> dict) return dict;
            if (obj is JObject jObj) return jObj.ToObject<Dictionary<string, object>>();
            return null;
        }

        private static string GetStr(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val)) return val?.ToString() ?? "";
            return "";
        }
    }
}
