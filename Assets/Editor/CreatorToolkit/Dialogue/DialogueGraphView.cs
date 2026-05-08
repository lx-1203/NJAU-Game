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
                title = "起始节点",
                GUID = Guid.NewGuid().ToString(),
                NodeId = "start",
                EntryPoint = true
            };

            var generatedPort = GeneratePort(entryNode, Direction.Output);
            generatedPort.portName = "下一句";
            entryNode.outputContainer.Add(generatedPort);

            entryNode.capabilities &= ~Capabilities.Movable;
            entryNode.capabilities &= ~Capabilities.Deletable;

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
            inputPort.portName = "输入";
            dialogueNode.inputContainer.Add(inputPort);

            // 默认的顺序推进连接
            var nextPort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
            nextPort.portName = "下一句";
            dialogueNode.outputContainer.Add(nextPort);

            var button = new Button(() => { AddChoicePort(dialogueNode); })
            {
                text = "新增选项"
            };
            dialogueNode.titleContainer.Add(button);

            var idField = new TextField("节点 ID") { value = nodeName, name = "idField" };
            idField.RegisterValueChangedCallback(evt => {
                dialogueNode.NodeId = evt.newValue;
                dialogueNode.title = evt.newValue;
            });
            dialogueNode.mainContainer.Add(idField);

            var speakerField = new TextField("说话人") { value = "", name = "speakerField" };
            speakerField.RegisterValueChangedCallback(evt => { dialogueNode.Speaker = evt.newValue; });
            dialogueNode.mainContainer.Add(speakerField);

            var portraitField = new TextField("立绘/头像") { value = "", name = "portraitField" };
            portraitField.RegisterValueChangedCallback(evt => { dialogueNode.Portrait = evt.newValue; });
            dialogueNode.mainContainer.Add(portraitField);

            var contentField = new TextField("内容") { value = "", multiline = true, name = "contentField" };
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
                ? $"选项 {outputPortCount}" 
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
