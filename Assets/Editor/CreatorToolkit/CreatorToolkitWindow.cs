using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using Zhongshan.CreatorToolkit.Dialogue;
using Zhongshan.CreatorToolkit.Event;
using Zhongshan.CreatorToolkit.Mission;
using Zhongshan.CreatorToolkit.NPC;

namespace Zhongshan.CreatorToolkit
{
    public class CreatorToolkitWindow : EditorWindow
    {
        private VisualElement contentContainer;
        private DialogueGraphView graphView;
        private Label dialogueListSummaryLabel;

        [MenuItem("钟山台/造物主 (Creator Toolkit)/剧情与事件编辑器", false, 0)]
        public static void OpenWindow()
        {
            CreatorToolkitWindow wnd = GetWindow<CreatorToolkitWindow>();
            wnd.titleContent = new GUIContent("Creator Toolkit");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            // Toolbar
            var toolbar = new UnityEditor.UIElements.Toolbar();
            root.Add(toolbar);

            var dialogueBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("Dialogue")) { text = "对话编辑器" };
            var eventBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("Event")) { text = "事件编辑器" };
            var missionBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("Mission")) { text = "任务编辑器" };
            var npcBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("NPC")) { text = "NPC编辑器" };

            toolbar.Add(dialogueBtn);
            toolbar.Add(eventBtn);
            toolbar.Add(missionBtn);
            toolbar.Add(npcBtn);

            contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            root.Add(contentContainer);

            ShowTab("Dialogue");
        }

        private void ShowTab(string tabName)
        {
            contentContainer.Clear();

            if (tabName == "Dialogue")
            {
                ConstructDialogueTab();
            }
            else if (tabName == "Event")
            {
                var eventPanel = new EventEditorPanel();
                eventPanel.StretchToParentSize();
                contentContainer.Add(eventPanel);
            }
            else if (tabName == "Mission")
            {
                var missionPanel = new MissionEditorPanel();
                missionPanel.StretchToParentSize();
                contentContainer.Add(missionPanel);
            }
            else if (tabName == "NPC")
            {
                var npcPanel = new NPCDatabaseEditorPanel();
                npcPanel.StretchToParentSize();
                contentContainer.Add(npcPanel);
            }
        }

        private void ConstructDialogueTab()
        {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            contentContainer.Add(splitView);

            // Left Panel (List of JSON files)
            var leftPanel = new VisualElement();
            var title = new Label("对话文件列表");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftPanel.Add(title);

            var newFileBtn = new Button(() =>
            {
                // This is a simple popup or we can just create a generic name and let the user rename later
                string newId = "new_dialogue_" + System.DateTime.Now.ToString("HHmmss");
                ToolkitDataManager.CreateNewDialogueFile(newId);
                ConstructDialogueTab(); // Refresh tab
            }) { text = "+ 新建对话" };
            leftPanel.Add(newFileBtn);

            dialogueListSummaryLabel = new Label();
            dialogueListSummaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            dialogueListSummaryLabel.style.marginBottom = 4;
            leftPanel.Add(dialogueListSummaryLabel);

            var files = ToolkitDataManager.GetAllDialogueFiles();
            dialogueListSummaryLabel.text = $"当前共 {files.Count} 份对话文件（会递归扫描子目录）";

            var listView = new ListView(files, 44,
                () =>
                {
                    var item = new VisualElement();
                    item.style.flexDirection = FlexDirection.Column;
                    item.style.paddingLeft = 6;
                    item.style.paddingRight = 6;
                    item.style.paddingTop = 4;
                    item.style.paddingBottom = 4;

                    var nameLabel = new Label();
                    nameLabel.name = "displayName";
                    nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    item.Add(nameLabel);

                    var metaLabel = new Label();
                    metaLabel.name = "metaInfo";
                    metaLabel.style.fontSize = 11;
                    metaLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                    item.Add(metaLabel);
                    return item;
                },
                (e, i) =>
                {
                    var info = files[i];
                    e.Q<Label>("displayName").text = info.displayName;
                    e.Q<Label>("metaInfo").text = $"{info.fileName}  |  {info.nodeCount} 个节点";
                    e.tooltip = string.IsNullOrWhiteSpace(info.relativePath) ? info.fileName : info.relativePath;
                });

            listView.style.flexGrow = 1;
            listView.selectionChanged += OnDialogueSelected;
            leftPanel.Add(listView);

            // Right Panel (GraphView)
            var rightPanel = new VisualElement();
            graphView = new DialogueGraphView
            {
                name = "Dialogue Graph"
            };
            graphView.StretchToParentSize();
            rightPanel.Add(graphView);

            // Toolbar for GraphView
            var graphToolbar = new UnityEditor.UIElements.Toolbar();
            var createNodeBtn = new UnityEditor.UIElements.ToolbarButton(() =>
            {
                graphView.CreateDialogueNode("NewNode", new Vector2(300, 300));
            }) { text = "新建节点" };

            var saveBtn = new UnityEditor.UIElements.ToolbarButton(() =>
            {
                if (listView.selectedItem != null)
                {
                    var selectedFile = listView.selectedItem as ToolkitDataManager.DialogueFileInfo;
                    if (selectedFile == null)
                    {
                        EditorUtility.DisplayDialog("提示", "当前选中的对话文件数据无效。", "确定");
                        return;
                    }

                    string fileName = selectedFile.fileName;
                    string dialogueId = selectedFile.dialogueId;
                    var saveHandler = new DialogueSaveHandler(graphView);
                    saveHandler.SaveDialogue(fileName, dialogueId);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先在左侧选择一个要保存的对话文件！", "确定");
                }
            }) { text = "保存图表" };

            graphToolbar.Add(createNodeBtn);
            graphToolbar.Add(saveBtn);
            rightPanel.Add(graphToolbar);

            splitView.Add(leftPanel);
            splitView.Add(rightPanel);
        }
        private void OnDialogueSelected(IEnumerable<object> selection)
        {
            ToolkitDataManager.DialogueFileInfo selectedFile = selection.OfType<ToolkitDataManager.DialogueFileInfo>().FirstOrDefault();
            if (selectedFile != null)
            {
                var saveHandler = new DialogueSaveHandler(graphView);
                saveHandler.LoadDialogue(selectedFile.fileName);
            }
        }
    }
}
