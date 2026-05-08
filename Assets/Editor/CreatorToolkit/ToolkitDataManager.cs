using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zhongshan.CreatorToolkit
{
    /// <summary>
    /// 为编辑器工具链提供安全的数据加载和保存功能
    /// </summary>
    public static class ToolkitDataManager
    {
        public sealed class DialogueFileInfo
        {
            public string fileName;
            public string relativePath;
            public string dialogueId;
            public string displayName;
            public int nodeCount;

            public override string ToString()
            {
                return displayName;
            }
        }

        private static readonly string DialoguesFolderPath = Path.Combine(Application.streamingAssetsPath, "Dialogues");
        private static readonly string EventsFolderPath = Path.Combine(Application.dataPath, "Resources/Data/Events");
        private static readonly string MissionsFilePath = Path.Combine(Application.dataPath, "Resources/Data/missions.json");
        private static readonly string NpcDatabaseFilePath = Path.Combine(Application.dataPath, "Resources/Data/npc_database.json");

        // ======= 对话读取与保存 =======

        /// <summary>
        /// 获取所有对话文件列表
        /// </summary>
        public static List<DialogueFileInfo> GetAllDialogueFiles()
        {
            EnsureDirectoryExists(DialoguesFolderPath);
            string[] files = Directory.GetFiles(DialoguesFolderPath, "*.json", SearchOption.AllDirectories);
            List<DialogueFileInfo> result = new List<DialogueFileInfo>();
            foreach (var file in files)
            {
                DialogueFileInfo info = BuildDialogueFileInfo(file);
                if (info != null)
                {
                    result.Add(info);
                }
            }

            result = result
                .OrderBy(info => info.displayName)
                .ThenBy(info => info.fileName)
                .ToList();

            return result;
        }

        /// <summary>
        /// 加载特定对话文件，使用现有的 DialogueDataWrapper
        /// 注：实际类型依赖于项目中的 DialogueData 类定义
        /// </summary>
        public static DialogueDataWrapper LoadDialogue(string fileName)
        {
            string path = Path.Combine(DialoguesFolderPath, fileName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<DialogueDataWrapper>(json);
        }

        /// <summary>
        /// 保存对话数据回文件
        /// </summary>
        public static void SaveDialogue(string fileName, DialogueDataWrapper data)
        {
            EnsureDirectoryExists(DialoguesFolderPath);
            string path = Path.Combine(DialoguesFolderPath, fileName);
            string json = JsonUtility.ToJson(data, true); // true for pretty print
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 创建一个新的对话文件
        /// </summary>
        public static void CreateNewDialogueFile(string dialogueId)
        {
            DialogueData[] dataArray = new DialogueData[1];
            dataArray[0] = new DialogueData
            {
                id = dialogueId,
                nodes = new DialogueNode[0]
            };
            DialogueDataWrapper newWrapper = new DialogueDataWrapper
            {
                dialogues = dataArray
            };
            SaveDialogue(dialogueId + ".json", newWrapper);
        }

        // ======= 任务读取与保存 =======

        public static MissionListWrapper LoadMissionList()
        {
            if (!File.Exists(MissionsFilePath)) return null;

            string json = File.ReadAllText(MissionsFilePath);
            return JsonUtility.FromJson<MissionListWrapper>(json);
        }

        public static void SaveMissionList(MissionListWrapper data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(MissionsFilePath, json);
            AssetDatabase.Refresh();
        }

        // ======= NPC 数据库读取与保存 =======

        public static NPCDatabaseRoot LoadNPCDatabase()
        {
            if (!File.Exists(NpcDatabaseFilePath)) return null;

            string json = File.ReadAllText(NpcDatabaseFilePath);
            return JsonUtility.FromJson<NPCDatabaseRoot>(json);
        }

        public static void SaveNPCDatabase(NPCDatabaseRoot data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(NpcDatabaseFilePath, json);
            AssetDatabase.Refresh();
        }

        // ======= 事件读取与保存 =======

        public static EventDatabaseRoot LoadEventDatabase(string fileName)
        {
            string path = Path.Combine(EventsFolderPath, fileName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<EventDatabaseRoot>(json);
        }

        public static void SaveEventDatabase(string fileName, EventDatabaseRoot data)
        {
            EnsureDirectoryExists(EventsFolderPath);
            string path = Path.Combine(EventsFolderPath, fileName);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static DialogueFileInfo BuildDialogueFileInfo(string absolutePath)
        {
            string fileName = Path.GetFileName(absolutePath);
            string relativePath = Path.GetRelativePath(DialoguesFolderPath, absolutePath).Replace("\\", "/");
            string displayName = Path.GetFileNameWithoutExtension(fileName);
            int nodeCount = 0;

            try
            {
                string json = File.ReadAllText(absolutePath);
                DialogueDataWrapper wrapper = JsonUtility.FromJson<DialogueDataWrapper>(json);
                DialogueData wrapperDialogue = wrapper != null && wrapper.dialogues != null && wrapper.dialogues.Length > 0
                    ? wrapper.dialogues[0]
                    : null;
                DialogueData singleDialogue = wrapperDialogue ?? JsonUtility.FromJson<DialogueData>(json);
                if (singleDialogue != null)
                {
                    if (!string.IsNullOrWhiteSpace(singleDialogue.id))
                    {
                        displayName = GetDialogueDisplayName(singleDialogue.id, singleDialogue.nodes, fileName);
                    }

                    nodeCount = singleDialogue.nodes != null ? singleDialogue.nodes.Length : 0;
                    return new DialogueFileInfo
                    {
                        fileName = fileName,
                        relativePath = relativePath,
                        dialogueId = string.IsNullOrWhiteSpace(singleDialogue.id) ? Path.GetFileNameWithoutExtension(fileName) : singleDialogue.id,
                        displayName = displayName,
                        nodeCount = nodeCount
                    };
                }
            }
            catch
            {
                // Fall back to filename-based display below.
            }

            return new DialogueFileInfo
            {
                fileName = fileName,
                relativePath = relativePath,
                dialogueId = Path.GetFileNameWithoutExtension(fileName),
                displayName = GetDialogueDisplayName(Path.GetFileNameWithoutExtension(fileName), null, fileName),
                nodeCount = nodeCount
            };
        }

        private static string GetDialogueDisplayName(string dialogueId, DialogueNode[] nodes, string fileName)
        {
            string speakerName = GetPrimarySpeakerName(nodes);
            if (!string.IsNullOrWhiteSpace(speakerName))
            {
                return speakerName;
            }

            switch (dialogueId)
            {
                case "lin_zhiqiu": return "林知秋";
                case "su_xiaoting": return "苏小晴";
                case "zhou_ran": return "周然";
                case "xie_lingyun": return "谢凌云";
                case "sample": return "示例对话";
                default:
                    return Path.GetFileNameWithoutExtension(fileName);
            }
        }

        private static string GetPrimarySpeakerName(DialogueNode[] nodes)
        {
            if (nodes == null)
            {
                return null;
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                string speaker = nodes[i]?.speaker;
                if (!string.IsNullOrWhiteSpace(speaker) && speaker != "_inner")
                {
                    return speaker;
                }
            }

            return null;
        }
    }
}
