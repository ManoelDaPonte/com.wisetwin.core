using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WiseTwin
{
    /// <summary>
    /// Runtime data structures for the dialogue system (branching conversation tree).
    /// Mono-language: all text fields are flat strings.
    /// Parsed from metadata JSON at runtime by DialogueDisplayer.
    /// </summary>

    [Serializable]
    public class DialogueTreeData
    {
        public string title;
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

        public static DialogueTreeData FromDictionary(Dictionary<string, object> data)
        {
            var tree = new DialogueTreeData();

            tree.title = LocalizedValueReader.ReadString(data, "title");

            if (data.TryGetValue("startNodeId", out var startObj))
                tree.startNodeId = startObj?.ToString();

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
    }

    [Serializable]
    public class DialogueNodeRuntime
    {
        public string id;
        public string type; // "start", "dialogue", "choice", "end"

        // For start and dialogue nodes
        public string nextNodeId;

        // For dialogue nodes
        public string speaker;
        public string text;

        // For choice nodes (prompt text is stored in 'text')
        public List<DialogueChoiceRuntime> choices;

        public static DialogueNodeRuntime FromDictionary(Dictionary<string, object> data)
        {
            var node = new DialogueNodeRuntime();

            node.id = GetStr(data, "id");
            node.type = GetStr(data, "type");
            node.nextNodeId = GetStr(data, "nextNodeId");

            node.speaker = LocalizedValueReader.ReadString(data, "speaker");
            node.text = LocalizedValueReader.ReadString(data, "text");

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
        public string text;
        public bool isCorrect;
        public string nextNodeId;

        public static DialogueChoiceRuntime FromDictionary(Dictionary<string, object> data)
        {
            var choice = new DialogueChoiceRuntime();

            choice.id = GetStr(data, "id");
            choice.nextNodeId = GetStr(data, "nextNodeId");

            if (data.TryGetValue("isCorrect", out var correctObj))
            {
                if (correctObj is bool b)
                    choice.isCorrect = b;
                else
                    bool.TryParse(correctObj?.ToString(), out choice.isCorrect);
            }

            choice.text = LocalizedValueReader.ReadString(data, "text");

            return choice;
        }

        private static string GetStr(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val)) return val?.ToString() ?? "";
            return "";
        }
    }
}
