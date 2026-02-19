using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR

namespace WiseTwin.Editor.DialogueEditor
{
    /// <summary>
    /// Custom JSON converter for Unity's Vector2 to avoid self-referencing loop
    /// on properties like normalized, which contain another Vector2.
    /// </summary>
    internal class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            float x = obj["x"]?.Value<float>() ?? 0f;
            float y = obj["y"]?.Value<float>() ?? 0f;
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// Handles serialization between:
    /// - Editor graph data (DialogueGraphEditorData) <-> JSON string (for persistence in DialogueScenarioData.graphDataJSON)
    /// - Editor graph data -> Runtime JSON format (for metadata export)
    /// </summary>
    public static class DialogueGraphSerializer
    {
        private static readonly JsonSerializerSettings EditorSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Vector2Converter() }
        };

        /// <summary>
        /// Serialize editor graph data to JSON string for storage.
        /// This is the editor-side format that preserves node positions.
        /// </summary>
        public static string SerializeEditorData(DialogueGraphEditorData graphData)
        {
            return JsonConvert.SerializeObject(graphData, EditorSerializerSettings);
        }

        /// <summary>
        /// Deserialize editor graph data from JSON string.
        /// </summary>
        public static DialogueGraphEditorData DeserializeEditorData(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                // First check if this is runtime format (has "startNodeId" key)
                // Runtime format won't deserialize correctly into editor format
                // because the field names differ (speakerEN vs speaker, etc.)
                var peek = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (peek != null && peek.ContainsKey("startNodeId"))
                {
                    // This is runtime format - import it
                    return TryImportFromRuntimeFormat(json);
                }

                // Try as editor format
                var result = JsonConvert.DeserializeObject<DialogueGraphEditorData>(json, EditorSerializerSettings);
                if (result != null && result.nodes != null && result.nodes.Count > 0)
                    return result;

                // Fallback to runtime format import
                return TryImportFromRuntimeFormat(json);
            }
            catch
            {
                // Last resort - try to import from runtime format
                return TryImportFromRuntimeFormat(json);
            }
        }

        /// <summary>
        /// Convert editor graph data to runtime JSON format (for metadata export).
        /// This produces the format consumed by DialogueDisplayer at runtime.
        /// </summary>
        public static Dictionary<string, object> ConvertToRuntimeFormat(DialogueGraphEditorData graphData, string titleEN, string titleFR)
        {
            var result = new Dictionary<string, object>();

            // Title
            result["title"] = new Dictionary<string, string>
            {
                ["en"] = titleEN ?? "",
                ["fr"] = titleFR ?? ""
            };

            // Build edge lookup: fromNodeId+fromPortName -> toNodeId
            var edgeLookup = new Dictionary<string, string>();
            foreach (var edge in graphData.edges)
            {
                string key = $"{edge.fromNodeId}:{edge.fromPortName}";
                edgeLookup[key] = edge.toNodeId;
            }

            // Find start node
            string startNodeId = null;
            var nodesJson = new List<object>();

            foreach (var node in graphData.nodes)
            {
                var nodeDict = new Dictionary<string, object>();
                nodeDict["id"] = node.id;
                nodeDict["type"] = node.type;

                switch (node.type)
                {
                    case "start":
                        startNodeId = node.id;
                        string startNext = GetNextNodeId(edgeLookup, node.id, "output");
                        if (!string.IsNullOrEmpty(startNext))
                            nodeDict["nextNodeId"] = startNext;
                        break;

                    case "dialogue":
                        nodeDict["speaker"] = new Dictionary<string, string>
                        {
                            ["en"] = node.speakerEN ?? "",
                            ["fr"] = node.speakerFR ?? ""
                        };
                        nodeDict["text"] = new Dictionary<string, string>
                        {
                            ["en"] = node.textEN ?? "",
                            ["fr"] = node.textFR ?? ""
                        };
                        string dialogueNext = GetNextNodeId(edgeLookup, node.id, "output");
                        if (!string.IsNullOrEmpty(dialogueNext))
                            nodeDict["nextNodeId"] = dialogueNext;
                        break;

                    case "choice":
                        nodeDict["text"] = new Dictionary<string, string>
                        {
                            ["en"] = node.promptTextEN ?? "",
                            ["fr"] = node.promptTextFR ?? ""
                        };
                        var choicesJson = new List<object>();
                        for (int i = 0; i < node.choices.Count; i++)
                        {
                            var choice = node.choices[i];
                            string portName = choice.portName ?? $"choice_{i}";
                            string choiceNext = GetNextNodeId(edgeLookup, node.id, portName);

                            choicesJson.Add(new Dictionary<string, object>
                            {
                                ["id"] = choice.id ?? $"choice_{i}",
                                ["text"] = new Dictionary<string, string>
                                {
                                    ["en"] = choice.textEN ?? "",
                                    ["fr"] = choice.textFR ?? ""
                                },
                                ["isCorrect"] = choice.isCorrect,
                                ["nextNodeId"] = choiceNext ?? ""
                            });
                        }
                        nodeDict["choices"] = choicesJson;
                        break;

                    case "end":
                        // End nodes have no additional fields
                        break;
                }

                nodesJson.Add(nodeDict);
            }

            result["startNodeId"] = startNodeId ?? "";
            result["nodes"] = nodesJson;

            return result;
        }

        /// <summary>
        /// Try to import from runtime JSON format (when loading existing metadata).
        /// Converts runtime format back to editor format with auto-layout positions.
        /// </summary>
        public static DialogueGraphEditorData TryImportFromRuntimeFormat(string json)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (dict == null || !dict.ContainsKey("nodes"))
                    return null;

                var graphData = new DialogueGraphEditorData();

                var nodesArray = dict["nodes"] as Newtonsoft.Json.Linq.JArray;
                if (nodesArray == null) return null;

                float yOffset = 0;
                var nodeIds = new HashSet<string>();

                foreach (var nodeToken in nodesArray)
                {
                    var nodeDict = nodeToken.ToObject<Dictionary<string, object>>();
                    var node = new DialogueNodeEditorData();

                    node.id = nodeDict.ContainsKey("id") ? nodeDict["id"]?.ToString() : "";
                    node.type = nodeDict.ContainsKey("type") ? nodeDict["type"]?.ToString() : "";
                    node.position = new Vector2(250, yOffset);
                    yOffset += 200;

                    nodeIds.Add(node.id);

                    switch (node.type)
                    {
                        case "start":
                            node.position = new Vector2(100, 100);
                            if (nodeDict.ContainsKey("nextNodeId"))
                            {
                                string nextId = nodeDict["nextNodeId"]?.ToString();
                                if (!string.IsNullOrEmpty(nextId))
                                {
                                    graphData.edges.Add(new DialogueEdgeData
                                    {
                                        fromNodeId = node.id,
                                        fromPortName = "output",
                                        toNodeId = nextId,
                                        toPortName = "input"
                                    });
                                }
                            }
                            break;

                        case "dialogue":
                            if (nodeDict.ContainsKey("speaker"))
                            {
                                var speakerDict = GetLocDict(nodeDict["speaker"]);
                                node.speakerEN = GetStr(speakerDict, "en");
                                node.speakerFR = GetStr(speakerDict, "fr");
                            }
                            if (nodeDict.ContainsKey("text"))
                            {
                                var textDict = GetLocDict(nodeDict["text"]);
                                node.textEN = GetStr(textDict, "en");
                                node.textFR = GetStr(textDict, "fr");
                            }
                            if (nodeDict.ContainsKey("nextNodeId"))
                            {
                                string nextId = nodeDict["nextNodeId"]?.ToString();
                                if (!string.IsNullOrEmpty(nextId))
                                {
                                    graphData.edges.Add(new DialogueEdgeData
                                    {
                                        fromNodeId = node.id,
                                        fromPortName = "output",
                                        toNodeId = nextId,
                                        toPortName = "input"
                                    });
                                }
                            }
                            break;

                        case "choice":
                            if (nodeDict.ContainsKey("text"))
                            {
                                var textDict = GetLocDict(nodeDict["text"]);
                                node.promptTextEN = GetStr(textDict, "en");
                                node.promptTextFR = GetStr(textDict, "fr");
                            }
                            if (nodeDict.ContainsKey("choices"))
                            {
                                var choicesArray = nodeDict["choices"] as Newtonsoft.Json.Linq.JArray;
                                if (choicesArray != null)
                                {
                                    for (int i = 0; i < choicesArray.Count; i++)
                                    {
                                        var choiceDict = choicesArray[i].ToObject<Dictionary<string, object>>();
                                        var choice = new DialogueChoiceEditorData();
                                        choice.id = choiceDict.ContainsKey("id") ? choiceDict["id"]?.ToString() : $"choice_{i}";
                                        choice.portName = $"choice_{i}";

                                        if (choiceDict.ContainsKey("text"))
                                        {
                                            var ctextDict = GetLocDict(choiceDict["text"]);
                                            choice.textEN = GetStr(ctextDict, "en");
                                            choice.textFR = GetStr(ctextDict, "fr");
                                        }
                                        if (choiceDict.ContainsKey("isCorrect"))
                                        {
                                            if (choiceDict["isCorrect"] is bool b)
                                                choice.isCorrect = b;
                                            else
                                                bool.TryParse(choiceDict["isCorrect"]?.ToString(), out choice.isCorrect);
                                        }

                                        node.choices.Add(choice);

                                        // Add edge for this choice
                                        if (choiceDict.ContainsKey("nextNodeId"))
                                        {
                                            string nextId = choiceDict["nextNodeId"]?.ToString();
                                            if (!string.IsNullOrEmpty(nextId))
                                            {
                                                graphData.edges.Add(new DialogueEdgeData
                                                {
                                                    fromNodeId = node.id,
                                                    fromPortName = choice.portName,
                                                    toNodeId = nextId,
                                                    toPortName = "input"
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case "end":
                            break;
                    }

                    graphData.nodes.Add(node);
                }

                return graphData;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DialogueGraphSerializer] Failed to import runtime format: {e.Message}");
                return null;
            }
        }

        private static string GetNextNodeId(Dictionary<string, string> edgeLookup, string nodeId, string portName)
        {
            string key = $"{nodeId}:{portName}";
            return edgeLookup.TryGetValue(key, out var nextId) ? nextId : null;
        }

        private static Dictionary<string, object> GetLocDict(object obj)
        {
            if (obj is Dictionary<string, object> dict) return dict;
            if (obj is Newtonsoft.Json.Linq.JObject jObj) return jObj.ToObject<Dictionary<string, object>>();
            return new Dictionary<string, object>();
        }

        private static string GetStr(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";
        }
    }
}

#endif
