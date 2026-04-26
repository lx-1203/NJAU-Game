using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.Event
{
    public class EventEditorPanel : VisualElement
    {
        private EventDatabaseRoot currentDatabase;
        private string currentFileName;
        private EventDefinition selectedEvent;
        private Vector2 scrollPos;

        public EventEditorPanel()
        {
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            this.Add(splitView);

            var leftPanel = new VisualElement();
            var rightPanel = new IMGUIContainer(DrawRightPanel) { style = { flexGrow = 1 } };

            var title = new Label("事件类型库");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftPanel.Add(title);

            // Mock categories
            string[] files = { "main_events.json", "fixed_events.json", "conditional_events.json", "dark_events.json" };
            var listView = new ListView(files, 25, () => new Label(), (e, i) => (e as Label).text = files[i]);
            listView.selectionChanged += OnFileSelected;
            listView.style.flexGrow = 1;
            leftPanel.Add(listView);

            splitView.Add(leftPanel);
            splitView.Add(rightPanel);
        }

        private void OnFileSelected(IEnumerable<object> selection)
        {
            var enumerator = selection.GetEnumerator();
            if (enumerator.MoveNext())
            {
                currentFileName = enumerator.Current.ToString();
                currentDatabase = ToolkitDataManager.LoadEventDatabase(currentFileName);
                selectedEvent = null;
            }
        }

        private void DrawRightPanel()
        {
            if (currentDatabase == null || currentDatabase.events == null)
            {
                GUILayout.Label("请在左侧选择一个事件分类文件...", EditorStyles.boldLabel);
                return;
            }

            GUILayout.BeginHorizontal();

            // Middle Column: Event List in this file
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label($"[{currentFileName}] 事件列表", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 新增事件"))
            {
                var list = new List<EventDefinition>(currentDatabase.events);
                list.Add(new EventDefinition { id = "NEW_EVENT", priority = 1 });
                currentDatabase.events = list.ToArray();
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var evt in currentDatabase.events)
            {
                GUI.color = selectedEvent == evt ? Color.cyan : Color.white;
                if (GUILayout.Button(evt.id, EditorStyles.toolbarButton))
                {
                    selectedEvent = evt;
                    GUI.FocusControl(null); // Unfocus any text fields to apply changes
                }
                GUI.color = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Right Column: Editor
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            if (selectedEvent != null)
            {
                DrawEventDetails(selectedEvent);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawEventDetails(EventDefinition evt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前编辑: " + evt.id, EditorStyles.largeLabel);
            if (GUILayout.Button("保存该类别文件", GUILayout.Width(150), GUILayout.Height(30)))
            {
                ToolkitDataManager.SaveEventDatabase(currentFileName, currentDatabase);
                EditorUtility.DisplayDialog("保存成功", $"{currentFileName} 保存完毕！", "确定");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            evt.id = EditorGUILayout.TextField("Event ID", evt.id);
            // 这里以后可以改成枚举下拉框来保证安全，比如 EventType/TriggerPhase
            evt.eventType = EditorGUILayout.TextField("Event Type", evt.eventType);
            evt.priority = EditorGUILayout.IntField("Priority", evt.priority);
            // evt.triggerPhase = EditorGUILayout.TextField("Trigger Phase", evt.triggerPhase); // moved to trigger
            evt.isForced = EditorGUILayout.Toggle("Is Forced?", evt.isForced);
            evt.isRepeatable = EditorGUILayout.Toggle("Is Repeatable?", evt.isRepeatable);

            GUILayout.Space(15);
            GUILayout.Label("=== 触发条件 (Conditions) ===", EditorStyles.boldLabel);
            if (evt.trigger == null)
            {
                if (GUILayout.Button("添加条件限制块", GUILayout.Width(200))) evt.trigger = new EventTriggerCondition();
            }
            else
            {
                GUILayout.BeginVertical("helpbox");
                evt.trigger.phase = EditorGUILayout.TextField("Trigger Phase", evt.trigger.phase);
                evt.trigger.year = EditorGUILayout.IntField("Year", evt.trigger.year);
                evt.trigger.semester = EditorGUILayout.IntField("Semester", evt.trigger.semester);
                evt.trigger.roundMin = EditorGUILayout.IntField("Min Round", evt.trigger.roundMin);
                evt.trigger.roundMax = EditorGUILayout.IntField("Max Round", evt.trigger.roundMax);

                GUILayout.Label("属性要求 (Attribute Conditions)");
                if (GUILayout.Button("+ 增加属性条件"))
                {
                    var list = evt.trigger.attributeConditions == null ? new List<AttributeCondition>() : new List<AttributeCondition>(evt.trigger.attributeConditions);
                    list.Add(new AttributeCondition());
                    evt.trigger.attributeConditions = list.ToArray();
                }

                if (evt.trigger.attributeConditions != null)
                {
                    for (int i = 0; i < evt.trigger.attributeConditions.Length; i++)
                    {
                        GUILayout.BeginHorizontal();
                        var attr = evt.trigger.attributeConditions[i];
                        attr.attributeName = EditorGUILayout.TextField(attr.attributeName, GUILayout.Width(80));
                        attr.comparison = EditorGUILayout.TextField(attr.comparison, GUILayout.Width(50));
                        attr.value = EditorGUILayout.IntField(attr.value, GUILayout.Width(80));
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            var list = new List<AttributeCondition>(evt.trigger.attributeConditions);
                            list.RemoveAt(i);
                            evt.trigger.attributeConditions = list.ToArray();
                            break;
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(5);
                if (GUILayout.Button("移除条件块", GUILayout.Width(100))) evt.trigger = null;
                GUILayout.EndVertical();
            }

            GUILayout.Space(15);
            GUILayout.Label("=== 对话及效果 (Dialogues & Effects) ===", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 增加一行对话"))
            {
                var list = evt.dialogues == null ? new List<EventDialogue>() : new List<EventDialogue>(evt.dialogues);
                list.Add(new EventDialogue());
                evt.dialogues = list.ToArray();
            }

            if (evt.dialogues != null)
            {
                for (int i = 0; i < evt.dialogues.Length; i++)
                {
                    GUILayout.BeginHorizontal("box");
                    var d = evt.dialogues[i];
                    GUILayout.BeginVertical();
                    d.speaker = EditorGUILayout.TextField("Speaker", d.speaker);
                    string content = (d.lines != null && d.lines.Length > 0) ? d.lines[0] : "";
                    content = EditorGUILayout.TextField("Content", content);
                    d.lines = new string[] { content };
                    GUILayout.EndVertical();
                    if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(40)))
                    {
                        var list = new List<EventDialogue>(evt.dialogues);
                        list.RemoveAt(i);
                        evt.dialogues = list.ToArray();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}