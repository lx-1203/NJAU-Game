using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Zhongshan.CreatorToolkit.Dialogue;
using Zhongshan.CreatorToolkit.Event;

namespace Zhongshan.CreatorToolkit
{
    public class CreatorToolkitWindow : EditorWindow
    {
        private VisualElement contentContainer;
        private DialogueGraphView graphView;

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

            // Toolbar
            var toolbar = new UnityEditor.UIElements.Toolbar();
            root.Add(toolbar);

            var dialogueBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("Dialogue")) { text = "对话编辑器" };
            var eventBtn = new UnityEditor.UIElements.ToolbarButton(() => ShowTab("Event")) { text = "事件编辑器" };

            toolbar.Add(dialogueBtn);
            toolbar.Add(eventBtn);

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

            var files = ToolkitDataManager.GetAllDialogueFiles();
            var listView = new ListView(files, 25,
                () => new Label(),
                (e, i) => (e as Label).text = files[i]);

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
                    string fileName = listView.selectedItem.ToString();
                    string dialogueId = fileName.Replace(".json", "");
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
            var enumerator = selection.GetEnumerator();
            if (enumerator.MoveNext())
            {
                string fileName = enumerator.Current.ToString();
                var saveHandler = new DialogueSaveHandler(graphView);
                saveHandler.LoadDialogue(fileName);
            }
        }
    }
}
