using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.Dialogue
{
    public class DialogueGraphView : GraphView
    {
        private DialogueGraphNode entryNode;

        public DialogueGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 澧炲姞鑳屾櫙缃戞牸
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddStyles();
            AddEntryNode();
        }

        private void AddStyles()
        {
            var styleSheet = Resources.Load<StyleSheet>("DialogueGraph");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning("[CreatorToolkit] Missing Resources/DialogueGraph.uss, using default GraphView styles.");
            }
        }

        private void AddEntryNode()
        {
            entryNode = new DialogueGraphNode
            {
                title = "璧峰鐐?(START)",
                GUID = Guid.NewGuid().ToString(),
                NodeId = "start",
                EntryPoint = true
            };

            var generatedPort = GeneratePort(entryNode, Direction.Output);
            generatedPort.portName = "Next";
            entryNode.outputContainer.Add(generatedPort);

            entryNode.capabilities &= ~Capabilities.Movable; // 绂佹鍒犻櫎鍜岃繃搴︾Щ鍔?            entryNode.capabilities &= ~Capabilities.Deletable;

            entryNode.SetPosition(new Rect(100, 200, 100, 150));
            AddElement(entryNode);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        public Port GeneratePort(DialogueGraphNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
        }

        public DialogueGraphNode CreateDialogueNode(string nodeName, Vector2 position)
        {
            var dialogueNode = new DialogueGraphNode
            {
                title = nodeName,
                NodeId = nodeName,
                GUID = Guid.NewGuid().ToString()
            };

            var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            dialogueNode.inputContainer.Add(inputPort);

            // Add an output port by default (for normal "next" connection)
            var nextPort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
            nextPort.portName = "Next";
            dialogueNode.outputContainer.Add(nextPort);

            var button = new Button(() => { AddChoicePort(dialogueNode); })
            {
                text = "鏂板閫夐」 (Choice)"
            };
            dialogueNode.titleContainer.Add(button);

            // UI Fields
            var idField = new TextField("Node ID:") { value = nodeName, name = "idField" };
            idField.RegisterValueChangedCallback(evt => {
                dialogueNode.NodeId = evt.newValue;
                dialogueNode.title = evt.newValue;
            });
            dialogueNode.mainContainer.Add(idField);

            var speakerField = new TextField("Speaker:") { value = "", name = "speakerField" };
            speakerField.RegisterValueChangedCallback(evt => { dialogueNode.Speaker = evt.newValue; });
            dialogueNode.mainContainer.Add(speakerField);

            var portraitField = new TextField("Portrait:") { value = "", name = "portraitField" };
            portraitField.RegisterValueChangedCallback(evt => { dialogueNode.Portrait = evt.newValue; });
            dialogueNode.mainContainer.Add(portraitField);

            var contentField = new TextField("Content:") { value = "", multiline = true, name = "contentField" };
            contentField.RegisterValueChangedCallback(evt => { dialogueNode.ContentText = evt.newValue; });
            dialogueNode.mainContainer.Add(contentField);

            dialogueNode.SetPosition(new Rect(position, new Vector2(250, 200)));
            AddElement(dialogueNode);
            return dialogueNode;
        }

        public void AddChoicePort(DialogueGraphNode node, string overridePortName = "")
        {
            var generatedPort = GeneratePort(node, Direction.Output, Port.Capacity.Single);
            
            var oldLabel = generatedPort.contentContainer.Q<Label>("type");
            if (oldLabel != null) generatedPort.contentContainer.Remove(oldLabel);

            var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            var choicePortName = string.IsNullOrEmpty(overridePortName) 
                ? $"Choice {outputPortCount}" 
                : overridePortName;
            
            var textField = new TextField
            {
                name = string.Empty,
                value = choicePortName
            };
            textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
            generatedPort.contentContainer.Add(new Label("  "));
            generatedPort.contentContainer.Add(textField);

            var deleteButton = new Button(() => RemovePort(node, generatedPort))
            {
                text = "X"
            };
            generatedPort.contentContainer.Add(deleteButton);

            generatedPort.portName = choicePortName;
            node.outputContainer.Add(generatedPort);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private void RemovePort(DialogueGraphNode node, Port generatedPort)
        {
            var targetEdge = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);
            
            if (targetEdge.Any())
            {
                var edge = targetEdge.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdge.First());
            }

            node.outputContainer.Remove(generatedPort);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }
    }
}
