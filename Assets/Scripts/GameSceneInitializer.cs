using UnityEngine;

/// <summary>
/// 游戏场景初始化器
/// 确保对话系统存在，并在运行时补充创建缺失的NPC
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("NPC 设置")]
    [SerializeField] private Vector3 npcPosition = new Vector3(4f, -2.3f, 0f);

    private void Start()
    {
        // 初始化对话系统
        SetupDialogueSystem();

        // 如果场景中没有 NPC，才动态创建（正常情况下场景已自带）
        if (FindObjectOfType<NPCController>() == null)
        {
            CreateNPC();
        }
    }

    private void SetupDialogueSystem()
    {
        if (DialogueSystem.Instance == null)
        {
            GameObject dialogueObj = new GameObject("DialogueSystem");
            dialogueObj.AddComponent<DialogueSystem>();
        }
    }

    private void CreateNPC()
    {
        GameObject npc = new GameObject("NPC_Boy");
        npc.transform.position = npcPosition;

        SpriteRenderer sr = npc.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        npc.AddComponent<NPCController>();

        Debug.Log("NPC 已动态创建在位置: " + npcPosition);
    }
}
