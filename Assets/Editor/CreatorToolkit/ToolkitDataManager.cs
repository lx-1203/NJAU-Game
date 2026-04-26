using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zhongshan.CreatorToolkit
{
    /// <summary>
    /// 为编辑器工具链提供安全的数据加载和保存功能
    /// </summary>
    public static class ToolkitDataManager
    {
        private static readonly string DialoguesFolderPath = Path.Combine(Application.streamingAssetsPath, "Dialogues");
        private static readonly string EventsFolderPath = Path.Combine(Application.dataPath, "Resources/Data/Events");

        // ======= 对话读取与保存 =======

        /// <summary>
        /// 获取所有对话文件列表
        /// </summary>
        public static List<string> GetAllDialogueFiles()
        {
            EnsureDirectoryExists(DialoguesFolderPath);
            string[] files = Directory.GetFiles(DialoguesFolderPath, "*.json");
            List<string> result = new List<string>();
            foreach (var file in files)
            {
                result.Add(Path.GetFileName(file));
            }
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
    }
}
