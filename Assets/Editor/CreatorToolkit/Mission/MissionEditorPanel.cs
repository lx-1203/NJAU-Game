using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.Mission
{
    public class MissionEditorPanel : VisualElement
    {
        private MissionListWrapper currentWrapper;
        private MissionDefinition selectedMission;
        private Vector2 missionListScroll;
        private Vector2 inspectorScroll;

        public MissionEditorPanel()
        {
            style.flexGrow = 1;

            var splitView = new TwoPaneSplitView(0, 280, TwoPaneSplitViewOrientation.Horizontal);
            Add(splitView);

            var leftPanel = new IMGUIContainer(DrawMissionList) { style = { flexGrow = 1 } };
            var rightPanel = new IMGUIContainer(DrawMissionInspector) { style = { flexGrow = 1 } };

            splitView.Add(leftPanel);
            splitView.Add(rightPanel);

            LoadData();
        }

        private void LoadData()
        {
            currentWrapper = ToolkitDataManager.LoadMissionList() ?? new MissionListWrapper
            {
                missions = new List<MissionDefinition>()
            };
            currentWrapper.missions ??= new List<MissionDefinition>();

            if (selectedMission != null)
            {
                selectedMission = currentWrapper.missions.Find(m => m.missionId == selectedMission.missionId);
            }
        }

        private void DrawMissionList()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("任务列表", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Height(24)))
            {
                LoadData();
            }

            if (GUILayout.Button("+ 新任务", GUILayout.Height(24)))
            {
                CreateMission();
            }
            GUILayout.EndHorizontal();

            if (currentWrapper == null || currentWrapper.missions == null)
            {
                EditorGUILayout.HelpBox("missions.json 读取失败。", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            missionListScroll = GUILayout.BeginScrollView(missionListScroll);
            foreach (var mission in currentWrapper.missions)
            {
                GUI.color = selectedMission == mission ? new Color(0.65f, 0.9f, 1f) : Color.white;
                if (GUILayout.Button($"{mission.missionId} | {mission.missionName}", EditorStyles.toolbarButton))
                {
                    selectedMission = mission;
                    GUI.FocusControl(null);
                }
                GUI.color = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawMissionInspector()
        {
            GUILayout.BeginVertical();

            if (selectedMission == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("请选择或新建一个任务。", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("任务详情", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("保存", GUILayout.Width(100), GUILayout.Height(28)))
            {
                SaveData();
            }

            GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
            if (GUILayout.Button("删除", GUILayout.Width(100), GUILayout.Height(28)))
            {
                DeleteSelectedMission();
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

            selectedMission.missionId = EditorGUILayout.TextField("Mission ID", selectedMission.missionId);
            selectedMission.missionName = EditorGUILayout.TextField("任务名称", selectedMission.missionName);
            selectedMission.description = EditorGUILayout.TextField("描述", selectedMission.description, GUILayout.MinHeight(40));
            selectedMission.type = (MissionType)EditorGUILayout.EnumPopup("任务类型", selectedMission.type);
            selectedMission.priority = EditorGUILayout.IntField("优先级", selectedMission.priority);
            selectedMission.timeLimit = EditorGUILayout.IntField("时间限制", selectedMission.timeLimit);
            selectedMission.autoAccept = EditorGUILayout.Toggle("自动接取", selectedMission.autoAccept);
            selectedMission.canAbandon = EditorGUILayout.Toggle("可放弃", selectedMission.canAbandon);

            DrawTriggerConditions(selectedMission);
            DrawPrerequisites(selectedMission);
            DrawObjectives(selectedMission);
            DrawRewards(selectedMission);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTriggerConditions(MissionDefinition mission)
        {
            mission.triggerConditions ??= new List<MissionTriggerCondition>();
            GUILayout.Space(10);
            GUILayout.Label("触发条件", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 添加触发条件", GUILayout.Width(140)))
            {
                mission.triggerConditions.Add(new MissionTriggerCondition
                {
                    conditionType = "Round",
                    comparisonOperator = ">=",
                    minValue = 1
                });
            }

            for (int i = 0; i < mission.triggerConditions.Count; i++)
            {
                var condition = mission.triggerConditions[i];
                GUILayout.BeginVertical("box");
                condition.conditionType = DrawStringPopup("条件类型", condition.conditionType,
                    new[] { "Round", "Semester", "Attribute", "Money", "NPCAffinity", "Event" });
                condition.targetId = EditorGUILayout.TextField("目标ID", condition.targetId);
                condition.comparisonOperator = DrawStringPopup("比较符", condition.comparisonOperator,
                    new[] { ">=", "<=", ">", "<", "==", "!=" });
                condition.minValue = EditorGUILayout.IntField("最小值", condition.minValue);
                condition.maxValue = EditorGUILayout.IntField("最大值", condition.maxValue);

                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("移除该条件", GUILayout.Width(120)))
                {
                    mission.triggerConditions.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndVertical();
            }
        }

        private void DrawPrerequisites(MissionDefinition mission)
        {
            mission.prerequisiteMissions ??= new List<string>();
            GUILayout.Space(10);
            GUILayout.Label("前置任务", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 添加前置任务", GUILayout.Width(140)))
            {
                mission.prerequisiteMissions.Add(string.Empty);
            }

            for (int i = 0; i < mission.prerequisiteMissions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                mission.prerequisiteMissions[i] = EditorGUILayout.TextField($"前置 {i + 1}", mission.prerequisiteMissions[i]);
                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    mission.prerequisiteMissions.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawObjectives(MissionDefinition mission)
        {
            mission.objectives ??= new List<MissionObjective>();
            GUILayout.Space(10);
            GUILayout.Label("任务目标", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 添加目标", GUILayout.Width(120)))
            {
                mission.objectives.Add(new MissionObjective
                {
                    objectiveId = $"{mission.missionId}_OBJ{mission.objectives.Count + 1}",
                    type = MissionObjectiveType.ActionCount,
                    targetValue = 1
                });
            }

            for (int i = 0; i < mission.objectives.Count; i++)
            {
                var objective = mission.objectives[i];
                GUILayout.BeginVertical("box");
                objective.objectiveId = EditorGUILayout.TextField("Objective ID", objective.objectiveId);
                objective.type = (MissionObjectiveType)EditorGUILayout.EnumPopup("目标类型", objective.type);
                objective.description = EditorGUILayout.TextField("描述", objective.description);
                objective.targetId = EditorGUILayout.TextField("目标ID", objective.targetId);
                objective.targetValue = EditorGUILayout.IntField("目标值", objective.targetValue);

                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("移除目标", GUILayout.Width(100)))
                {
                    mission.objectives.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndVertical();
            }
        }

        private void DrawRewards(MissionDefinition mission)
        {
            mission.rewards ??= new List<MissionReward>();
            GUILayout.Space(10);
            GUILayout.Label("任务奖励", EditorStyles.boldLabel);

            if (GUILayout.Button("+ 添加奖励", GUILayout.Width(120)))
            {
                mission.rewards.Add(new MissionReward
                {
                    type = MissionRewardType.Money,
                    value = 100
                });
            }

            for (int i = 0; i < mission.rewards.Count; i++)
            {
                var reward = mission.rewards[i];
                GUILayout.BeginVertical("box");
                reward.type = (MissionRewardType)EditorGUILayout.EnumPopup("奖励类型", reward.type);
                reward.targetId = EditorGUILayout.TextField("目标ID", reward.targetId);
                reward.value = EditorGUILayout.IntField("数值", reward.value);
                reward.description = EditorGUILayout.TextField("描述", reward.description);

                GUI.backgroundColor = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("移除奖励", GUILayout.Width(100)))
                {
                    mission.rewards.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndVertical();
            }
        }

        private void CreateMission()
        {
            currentWrapper ??= new MissionListWrapper { missions = new List<MissionDefinition>() };
            currentWrapper.missions ??= new List<MissionDefinition>();

            string missionId = $"M{currentWrapper.missions.Count + 1:000}";
            var mission = new MissionDefinition
            {
                missionId = missionId,
                missionName = "新任务",
                description = string.Empty,
                type = MissionType.SideQuest,
                priority = currentWrapper.missions.Count + 1,
                triggerConditions = new List<MissionTriggerCondition>(),
                prerequisiteMissions = new List<string>(),
                objectives = new List<MissionObjective>(),
                rewards = new List<MissionReward>(),
                autoAccept = false,
                canAbandon = true
            };

            currentWrapper.missions.Add(mission);
            selectedMission = mission;
        }

        private void DeleteSelectedMission()
        {
            if (selectedMission == null || currentWrapper?.missions == null)
                return;

            if (!EditorUtility.DisplayDialog("删除任务", $"确定删除任务 {selectedMission.missionId} 吗？", "删除", "取消"))
                return;

            currentWrapper.missions.Remove(selectedMission);
            selectedMission = null;
            SaveData();
        }

        private void SaveData()
        {
            ToolkitDataManager.SaveMissionList(currentWrapper);
            EditorUtility.DisplayDialog("保存成功", "missions.json 已保存。", "确定");
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
