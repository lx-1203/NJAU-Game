using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.NPC
{
    public class NPCDatabaseEditorPanel : VisualElement
    {
        private NPCDatabaseRoot databaseRoot;
        private NPCData selectedNpc;
        private SocialActionDefinition selectedAction;
        private EditorMode editorMode = EditorMode.NPC;
        private Vector2 listScroll;
        private Vector2 inspectorScroll;

        private enum EditorMode
        {
            NPC,
            SocialAction
        }

        public NPCDatabaseEditorPanel()
        {
            style.flexGrow = 1;

            var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);

            var leftPanel = new IMGUIContainer(DrawLeftPanel) { style = { flexGrow = 1 } };
            var rightPanel = new IMGUIContainer(DrawInspector) { style = { flexGrow = 1 } };

            splitView.Add(leftPanel);
            splitView.Add(rightPanel);

            LoadData();
        }

        private void LoadData()
        {
            databaseRoot = ToolkitDataManager.LoadNPCDatabase() ?? new NPCDatabaseRoot();
            databaseRoot.npcs ??= Array.Empty<NPCData>();
            databaseRoot.socialActions ??= Array.Empty<SocialActionDefinition>();

            if (selectedNpc != null)
            {
                selectedNpc = Array.Find(databaseRoot.npcs, npc => npc.id == selectedNpc.id);
            }

            if (selectedAction != null)
            {
                selectedAction = Array.Find(databaseRoot.socialActions, action => action.id == selectedAction.id);
            }
        }

        private void DrawLeftPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("NPC 数据库", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            bool npcMode = GUILayout.Toggle(editorMode == EditorMode.NPC, "NPC", EditorStyles.toolbarButton);
            bool actionMode = GUILayout.Toggle(editorMode == EditorMode.SocialAction, "社交行动", EditorStyles.toolbarButton);
            if (npcMode) editorMode = EditorMode.NPC;
            if (actionMode) editorMode = EditorMode.SocialAction;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Height(24)))
            {
                LoadData();
            }

            if (GUILayout.Button(editorMode == EditorMode.NPC ? "+ 新NPC" : "+ 新行动", GUILayout.Height(24)))
            {
                if (editorMode == EditorMode.NPC)
                    CreateNpc();
                else
                    CreateSocialAction();
            }
            GUILayout.EndHorizontal();

            listScroll = GUILayout.BeginScrollView(listScroll);
            if (editorMode == EditorMode.NPC)
            {
                foreach (var npc in databaseRoot.npcs)
                {
                    GUI.color = selectedNpc == npc ? new Color(0.65f, 0.9f, 1f) : Color.white;
                    if (GUILayout.Button($"{npc.displayName} ({npc.id})", EditorStyles.toolbarButton))
                    {
                        selectedNpc = npc;
                        selectedAction = null;
                    }
                    GUI.color = Color.white;
                }
            }
            else
            {
                foreach (var action in databaseRoot.socialActions)
                {
                    GUI.color = selectedAction == action ? new Color(0.65f, 0.9f, 1f) : Color.white;
                    if (GUILayout.Button($"{action.displayName} ({action.id})", EditorStyles.toolbarButton))
                    {
                        selectedAction = action;
                        selectedNpc = null;
                    }
                    GUI.color = Color.white;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawInspector()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(editorMode == EditorMode.NPC ? "NPC 详情" : "社交行动详情", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存数据库", GUILayout.Width(120), GUILayout.Height(28)))
            {
                SaveData();
            }
            GUILayout.EndHorizontal();

            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

            if (editorMode == EditorMode.NPC)
            {
                DrawNpcInspector();
            }
            else
            {
                DrawSocialActionInspector();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawNpcInspector()
        {
            if (selectedNpc == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请选择或新建一个 NPC。", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            selectedNpc.id = EditorGUILayout.TextField("NPC ID", selectedNpc.id);
            selectedNpc.displayName = EditorGUILayout.TextField("显示名", selectedNpc.displayName);
            selectedNpc.type = DrawStringPopup("类型", selectedNpc.type,
                new[] { "Roommate", "Senior", "Classmate", "Teacher", "Other" });
            selectedNpc.personality = DrawStringPopup("性格", selectedNpc.personality,
                new[] { "Introvert", "Extrovert", "Easygoing", "Mysterious", "Cheerful", "Serious" });
            selectedNpc.description = EditorGUILayout.TextField("简介", selectedNpc.description, GUILayout.MinHeight(40));
            selectedNpc.dialogueId = EditorGUILayout.TextField("对话ID", selectedNpc.dialogueId);
            selectedNpc.portraitId = EditorGUILayout.TextField("头像ID", selectedNpc.portraitId);

            selectedNpc.likedActionIds = DrawStringArray("喜欢的行动", selectedNpc.likedActionIds, "添加喜欢行动");
            selectedNpc.dislikedActionIds = DrawStringArray("不喜欢的行动", selectedNpc.dislikedActionIds, "添加厌恶行动");
            selectedNpc.greetingLines = DrawStringArray("打招呼台词", selectedNpc.greetingLines, "添加台词");

            DrawSchedule(selectedNpc);

            GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
            if (GUILayout.Button("删除该 NPC", GUILayout.Width(110)))
            {
                DeleteSelectedNpc();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawSocialActionInspector()
        {
            if (selectedAction == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请选择或新建一个社交行动。", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            selectedAction.id = EditorGUILayout.TextField("行动 ID", selectedAction.id);
            selectedAction.displayName = EditorGUILayout.TextField("显示名", selectedAction.displayName);
            selectedAction.actionPointCost = EditorGUILayout.IntField("AP 消耗", selectedAction.actionPointCost);
            selectedAction.moneyCost = EditorGUILayout.IntField("金钱消耗", selectedAction.moneyCost);
            selectedAction.minAffinityLevel = DrawStringPopup("最低好感等级", selectedAction.minAffinityLevel,
                new[] { "Stranger", "Acquaintance", "Friend", "CloseFriend", "BestFriend", "Lover" });
            selectedAction.baseAffinityMin = EditorGUILayout.IntField("基础好感下限", selectedAction.baseAffinityMin);
            selectedAction.baseAffinityMax = EditorGUILayout.IntField("基础好感上限", selectedAction.baseAffinityMax);
            selectedAction.attributeEffects = DrawAttributeEffects(selectedAction.attributeEffects);

            GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
            if (GUILayout.Button("删除该行动", GUILayout.Width(110)))
            {
                DeleteSelectedAction();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawSchedule(NPCData npc)
        {
            var schedule = npc.schedule != null ? new List<NPCScheduleEntry>(npc.schedule) : new List<NPCScheduleEntry>();
            GUILayout.Space(10);
            GUILayout.Label("日程表", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加日程", GUILayout.Width(120)))
            {
                schedule.Add(new NPCScheduleEntry { timeSlot = "Morning", location = string.Empty });
            }

            for (int i = 0; i < schedule.Count; i++)
            {
                GUILayout.BeginVertical("box");
                schedule[i].timeSlot = DrawStringPopup("时间段", schedule[i].timeSlot,
                    new[] { "Morning", "Afternoon", "Evening" });
                schedule[i].location = EditorGUILayout.TextField("地点", schedule[i].location);
                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("移除日程", GUILayout.Width(100)))
                {
                    schedule.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndVertical();
            }

            npc.schedule = schedule.ToArray();
        }

        private string[] DrawStringArray(string label, string[] source, string addButtonLabel)
        {
            var values = source != null ? new List<string>(source) : new List<string>();
            GUILayout.Space(10);
            GUILayout.Label(label, EditorStyles.boldLabel);
            if (GUILayout.Button($"+ {addButtonLabel}", GUILayout.Width(140)))
            {
                values.Add(string.Empty);
            }

            for (int i = 0; i < values.Count; i++)
            {
                GUILayout.BeginHorizontal();
                values[i] = EditorGUILayout.TextField($"{label} {i + 1}", values[i]);
                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    values.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            return values.ToArray();
        }

        private AttributeEffect[] DrawAttributeEffects(AttributeEffect[] source)
        {
            var effects = source != null ? new List<AttributeEffect>(source) : new List<AttributeEffect>();
            GUILayout.Space(10);
            GUILayout.Label("属性效果", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加属性效果", GUILayout.Width(140)))
            {
                effects.Add(new AttributeEffect("心情", 1));
            }

            for (int i = 0; i < effects.Count; i++)
            {
                GUILayout.BeginHorizontal("box");
                effects[i].attributeName = EditorGUILayout.TextField(effects[i].attributeName, GUILayout.Width(140));
                effects[i].amount = EditorGUILayout.IntField(effects[i].amount, GUILayout.Width(80));
                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    effects.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            return effects.ToArray();
        }

        private void CreateNpc()
        {
            var npcs = new List<NPCData>(databaseRoot.npcs ?? Array.Empty<NPCData>());
            var npc = new NPCData
            {
                id = $"NPC_New_{npcs.Count + 1}",
                displayName = "新角色",
                type = "Other",
                personality = "Easygoing",
                description = string.Empty,
                portraitId = string.Empty,
                dialogueId = string.Empty,
                likedActionIds = Array.Empty<string>(),
                dislikedActionIds = Array.Empty<string>(),
                greetingLines = Array.Empty<string>(),
                schedule = Array.Empty<NPCScheduleEntry>()
            };
            npcs.Add(npc);
            databaseRoot.npcs = npcs.ToArray();
            selectedNpc = npc;
            editorMode = EditorMode.NPC;
        }

        private void CreateSocialAction()
        {
            var actions = new List<SocialActionDefinition>(databaseRoot.socialActions ?? Array.Empty<SocialActionDefinition>());
            var action = new SocialActionDefinition
            {
                id = $"new_action_{actions.Count + 1}",
                displayName = "新行动",
                actionPointCost = 1,
                moneyCost = 0,
                minAffinityLevel = "Stranger",
                baseAffinityMin = 1,
                baseAffinityMax = 3,
                attributeEffects = Array.Empty<AttributeEffect>()
            };
            actions.Add(action);
            databaseRoot.socialActions = actions.ToArray();
            selectedAction = action;
            editorMode = EditorMode.SocialAction;
        }

        private void DeleteSelectedNpc()
        {
            if (selectedNpc == null)
                return;

            if (!EditorUtility.DisplayDialog("删除 NPC", $"确定删除 {selectedNpc.displayName} 吗？", "删除", "取消"))
                return;

            var npcs = new List<NPCData>(databaseRoot.npcs);
            npcs.Remove(selectedNpc);
            databaseRoot.npcs = npcs.ToArray();
            selectedNpc = null;
            SaveData();
        }

        private void DeleteSelectedAction()
        {
            if (selectedAction == null)
                return;

            if (!EditorUtility.DisplayDialog("删除社交行动", $"确定删除 {selectedAction.displayName} 吗？", "删除", "取消"))
                return;

            var actions = new List<SocialActionDefinition>(databaseRoot.socialActions);
            actions.Remove(selectedAction);
            databaseRoot.socialActions = actions.ToArray();
            selectedAction = null;
            SaveData();
        }

        private void SaveData()
        {
            ToolkitDataManager.SaveNPCDatabase(databaseRoot);
            EditorUtility.DisplayDialog("保存成功", "npc_database.json 已保存。", "确定");
            LoadData();
        }

        private string DrawStringPopup(string label, string currentValue, string[] options)
        {
            int index = Array.IndexOf(options, currentValue);
            if (index < 0) index = 0;
            index = EditorGUILayout.Popup(label, index, options);
            return options[index];
        }
    }
}
