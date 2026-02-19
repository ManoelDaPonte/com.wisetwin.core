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
    /// GraphView canvas for the dialogue editor.
    /// Supports zoom, pan, grid background, minimap, and node manipulation.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        private DialogueEditorWindow parentWindow;
        private int nodeCounter = 0;
        private MiniMap miniMap;

        public DialogueGraphView(DialogueEditorWindow window)
        {
            parentWindow = window;

            // Setup zoom and pan
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            // Pan with middle mouse (default) + right mouse drag
            var contentDragger = new ContentDragger();
            contentDragger.activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.RightMouse
            });
            this.AddManipulator(contentDragger);

            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Grid background
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Minimap
            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            Add(miniMap);

            // Style
            styleSheets.Add(CreateGraphStyleSheet());

            // Handle edge creation
            graphViewChanged = OnGraphViewChanged;
        }

        private StyleSheet CreateGraphStyleSheet()
        {
            // Return empty stylesheet - styles are applied inline on nodes
            var sheet = ScriptableObject.CreateInstance<StyleSheet>();
            return sheet;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    // Edges are already added by the system
                }
            }

            parentWindow?.SetDirty();
            return change;
        }

        /// <summary>
        /// Define which ports can connect to which.
        /// Output ports connect to input ports only.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (port.direction != startPort.direction &&
                    port.node != startPort.node &&
                    port.portType == startPort.portType)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        /// <summary>
        /// Create a new node of the given type at the specified position.
        /// </summary>
        public DialogueNodeView CreateNode(string nodeType, Vector2 position)
        {
            nodeCounter++;
            string nodeId = $"node_{nodeCounter:D3}";

            var nodeData = new DialogueNodeEditorData
            {
                id = nodeId,
                type = nodeType,
                position = position
            };

            // Add default choices for choice nodes
            if (nodeType == "choice")
            {
                nodeData.choices.Add(new DialogueChoiceEditorData
                {
                    id = $"{nodeId}_choice_0",
                    textEN = "Option 1",
                    textFR = "Option 1",
                    portName = "choice_0"
                });
                nodeData.choices.Add(new DialogueChoiceEditorData
                {
                    id = $"{nodeId}_choice_1",
                    textEN = "Option 2",
                    textFR = "Option 2",
                    portName = "choice_1"
                });
            }

            var nodeView = new DialogueNodeView(nodeData, this);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            AddElement(nodeView);

            parentWindow?.SetDirty();
            return nodeView;
        }

        /// <summary>
        /// Load graph from editor data.
        /// </summary>
        public void LoadGraph(DialogueGraphEditorData graphData)
        {
            // Clear existing
            DeleteElements(graphElements.ToList());
            nodeCounter = 0;

            if (graphData == null) return;

            // Create nodes
            var nodeViews = new Dictionary<string, DialogueNodeView>();
            foreach (var nodeData in graphData.nodes)
            {
                // Track max node counter
                if (nodeData.id.StartsWith("node_"))
                {
                    if (int.TryParse(nodeData.id.Substring(5), out int num))
                    {
                        nodeCounter = Mathf.Max(nodeCounter, num);
                    }
                }

                var nodeView = new DialogueNodeView(nodeData, this);
                nodeView.SetPosition(new Rect(nodeData.position, Vector2.zero));
                AddElement(nodeView);
                nodeViews[nodeData.id] = nodeView;
            }

            // Create edges
            foreach (var edgeData in graphData.edges)
            {
                if (!nodeViews.TryGetValue(edgeData.fromNodeId, out var fromNode))
                {
                    Debug.LogWarning($"[DialogueGraphView] Edge from node not found: {edgeData.fromNodeId}");
                    continue;
                }
                if (!nodeViews.TryGetValue(edgeData.toNodeId, out var toNode))
                {
                    Debug.LogWarning($"[DialogueGraphView] Edge to node not found: {edgeData.toNodeId}");
                    continue;
                }

                Port outputPort = fromNode.GetOutputPort(edgeData.fromPortName);
                Port inputPort = toNode.GetInputPort();

                if (outputPort == null)
                {
                    Debug.LogWarning($"[DialogueGraphView] Output port not found: {edgeData.fromNodeId}.{edgeData.fromPortName}");
                    continue;
                }
                if (inputPort == null)
                {
                    Debug.LogWarning($"[DialogueGraphView] Input port not found on: {edgeData.toNodeId}");
                    continue;
                }

                var edge = outputPort.ConnectTo(inputPort);
                AddElement(edge);
            }
        }

        /// <summary>
        /// Save current graph to editor data.
        /// </summary>
        public DialogueGraphEditorData SaveGraph()
        {
            var graphData = new DialogueGraphEditorData();

            // Save nodes
            foreach (var element in graphElements)
            {
                if (element is DialogueNodeView nodeView)
                {
                    var nodeData = nodeView.SaveData();
                    nodeData.position = nodeView.GetPosition().position;
                    graphData.nodes.Add(nodeData);
                }
            }

            // Save edges
            foreach (var element in graphElements)
            {
                if (element is Edge edge)
                {
                    var fromNode = edge.output?.node as DialogueNodeView;
                    var toNode = edge.input?.node as DialogueNodeView;

                    if (fromNode != null && toNode != null)
                    {
                        graphData.edges.Add(new DialogueEdgeData
                        {
                            fromNodeId = fromNode.NodeId,
                            fromPortName = edge.output.portName,
                            toNodeId = toNode.NodeId,
                            toPortName = "input"
                        });
                    }
                }
            }

            return graphData;
        }

        /// <summary>
        /// Mark the graph as dirty (unsaved changes).
        /// </summary>
        public void SetDirty()
        {
            parentWindow?.SetDirty();
        }

        /// <summary>
        /// Check if a Start node already exists.
        /// </summary>
        public bool HasStartNode()
        {
            foreach (var element in graphElements)
            {
                if (element is DialogueNodeView nodeView && nodeView.NodeType == "start")
                    return true;
            }
            return false;
        }
    }
}

#endif
