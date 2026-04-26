using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.Dialogue
{
    public class DialogueSaveHandler
    {
        private DialogueGraphView graphView;
        private List<Edge> edges => graphView.edges.ToList();
        private List<DialogueGraphNode> nodes => graphView.nodes.ToList().Cast<DialogueGraphNode>().ToList();

        public DialogueSaveHandler(DialogueGraphView graphView)
        {
            this.graphView = graphView;
        }

        public void SaveDialogue(string fileName, string dialogueId)
        {
            if (!nodes.Any()) return;

            var dialogueWrapper = new DialogueDataWrapper
            {
                dialogues = new DialogueData[]
                {
                    new DialogueData
                    {
                        id = dialogueId,
                        nodes = new DialogueNode[0]
                    }
                }
            };

            var connectedEdges = edges.Where(x => x.input.node != null).ToArray();
            var savedNodes = new List<DialogueNode>();

            foreach (var graphNode in nodes)
            {
                if (graphNode.EntryPoint) continue;

                var dialogueNode = new DialogueNode
                {
                    id = graphNode.NodeId,
                    speaker = graphNode.Speaker,
                    portrait = graphNode.Portrait,
                    content = graphNode.ContentText,
                    choices = new DialogueChoice[0]
                };

                // 处理 Default Next 连接
                var nextPort = graphNode.outputContainer.Query<Port>().Where(p => p.portName == "Next").First();
                var nextEdge = connectedEdges.FirstOrDefault(e => e.output == nextPort);
                if (nextEdge != null)
                {
                    dialogueNode.next = (nextEdge.input.node as DialogueGraphNode).NodeId;
                }
                else
                {
                    dialogueNode.next = null;
                }

                // 处理 Choices 连接
                var choicePorts = graphNode.outputContainer.Query<Port>().Where(p => p.portName != "Next").ToList();
                if (choicePorts.Count > 0)
                {
                    var choicesList = new List<DialogueChoice>();
                    for (int i = 0; i < choicePorts.Count; i++)
                    {
                        var cPort = choicePorts[i];
                        var choiceEdge = connectedEdges.FirstOrDefault(e => e.output == cPort);

                        // 尝试保留原来的选项数据 (如果有的话，主要为了保留 Condition / Effects)
                        DialogueChoice originalChoice = null;
                        if (graphNode.Choices != null && i < graphNode.Choices.Count)
                        {
                            originalChoice = graphNode.Choices[i];
                        }

                        var choiceData = new DialogueChoice
                        {
                            text = cPort.portName,
                            next = choiceEdge != null ? (choiceEdge.input.node as DialogueGraphNode).NodeId : null,
                            condition = originalChoice?.condition,
                            conditionHint = originalChoice?.conditionHint,
                            effects = originalChoice?.effects
                        };
                        choicesList.Add(choiceData);
                    }
                    dialogueNode.choices = choicesList.ToArray();
                }

                savedNodes.Add(dialogueNode);
            }

            dialogueWrapper.dialogues[0].nodes = savedNodes.ToArray();
            ToolkitDataManager.SaveDialogue(fileName, dialogueWrapper);
            Debug.Log($"[{fileName}] 对话保存成功！");
        }

        public void LoadDialogue(string fileName)
        {
            var dataWrapper = ToolkitDataManager.LoadDialogue(fileName);
            if (dataWrapper == null || dataWrapper.dialogues == null || dataWrapper.dialogues.Length == 0)
            {
                EditorUtility.DisplayDialog("加载失败", "对话数据为空或解析失败", "确定");
                return;
            }

            ClearGraph();
            GenerateNodes(dataWrapper.dialogues[0]);
            ConnectNodes(dataWrapper.dialogues[0]);
        }

        private void ClearGraph()
        {
            var nodesToRemove = nodes.Where(n => !n.EntryPoint).ToList();
            var edgesToRemove = edges;

            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }
            foreach (var node in nodesToRemove)
            {
                graphView.RemoveElement(node);
            }
        }

        private void GenerateNodes(DialogueData data)
        {
            float startX = 300;
            float startY = 200;
            int i = 0;

            foreach (var nodeData in data.nodes)
            {
                // 简单的网格排版
                Vector2 pos = new Vector2(startX + (i % 3) * 350, startY + (i / 3) * 300);

                var graphNode = graphView.CreateDialogueNode(nodeData.id, pos);
                graphNode.Speaker = nodeData.speaker;
                graphNode.Portrait = nodeData.portrait;
                graphNode.ContentText = nodeData.content;

                // 将数据填入 UI
                var speakerField = graphNode.mainContainer.Q<TextField>("speakerField");
                if (speakerField != null) speakerField.value = nodeData.speaker;

                var contentField = graphNode.mainContainer.Q<TextField>("contentField");
                if (contentField != null) contentField.value = nodeData.content;

                // 生成 Choices 端口
                if (nodeData.choices != null && nodeData.choices.Length > 0)
                {
                    graphNode.Choices = nodeData.choices.ToList();
                    foreach (var choice in nodeData.choices)
                    {
                        graphView.AddChoicePort(graphNode, choice.text);
                    }
                }

                i++;
            }
        }

        private void ConnectNodes(DialogueData data)
        {
            var nodeDict = nodes.Where(n => !n.EntryPoint).ToDictionary(n => n.NodeId, n => n);

            foreach (var nodeData in data.nodes)
            {
                if (!nodeDict.TryGetValue(nodeData.id, out var graphNode)) continue;

                // 连 Next
                if (!string.IsNullOrEmpty(nodeData.next) && nodeDict.TryGetValue(nodeData.next, out var targetNextNode))
                {
                    var outputPort = graphNode.outputContainer.Query<Port>().Where(p => p.portName == "Next").First();
                    var inputPort = targetNextNode.inputContainer.Q<Port>();
                    LinkNodes(outputPort, inputPort);
                }

                // 连 Choices
                if (nodeData.choices != null)
                {
                    var choicePorts = graphNode.outputContainer.Query<Port>().Where(p => p.portName != "Next").ToList();
                    for (int i = 0; i < nodeData.choices.Length && i < choicePorts.Count; i++)
                    {
                        var choice = nodeData.choices[i];
                        if (!string.IsNullOrEmpty(choice.next) && nodeDict.TryGetValue(choice.next, out var targetChoiceNode))
                        {
                            LinkNodes(choicePorts[i], targetChoiceNode.inputContainer.Q<Port>());
                        }
                    }
                }
            }
        }

        private void LinkNodes(Port output, Port input)
        {
            var edge = new Edge
            {
                output = output,
                input = input
            };
            edge?.input.Connect(edge);
            edge?.output.Connect(edge);
            graphView.AddElement(edge);
        }
    }
}
