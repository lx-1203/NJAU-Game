using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 精灵序列帧动画组件
/// 支持 Inspector 拖拽配置，也支持运行时从 Resources 自动加载
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AnimationClip
    {
        public string name;           // 动画名称
        public Sprite[] frames;      // 帧数组
        public float frameRate = 8f;  // 每秒帧数
        public bool loop = true;     // 是否循环

        [HideInInspector] public float timer;
        [HideInInspector] public int currentFrame;
    }

    [Header("动画列表")]
    [SerializeField] private List<AnimationClip> animations = new List<AnimationClip>();

    private SpriteRenderer spriteRenderer;
    private AnimationClip currentClip;
    private bool isPlaying;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 从 Resources 路径加载 Sprite Sheet 并注册为动画
    /// 例: LoadFromResources("PlayerWalkSprites", "Idle", 4f, true)
    /// 会加载 Resources/PlayerWalkSprites.png 切片后的所有子精灵
    /// </summary>
    public void LoadFromResources(string resourcePath, string clipName, float frameRate = 8f, bool loop = true)
    {
        // 加载该路径下的所有子精灵
        Sprite[] allSprites = Resources.LoadAll<Sprite>(resourcePath);
        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogWarning($"[SpriteSheetAnimator] Resources/{resourcePath} 未找到精灵或未切片！");
            return;
        }

        // 按名称排序确保帧顺序正确 (name_0, name_1, name_2 ...)
        allSprites = allSprites.OrderBy(s => s.name).ToArray();

        // 检查是否已有同名动画，有则替换
        var existing = animations.Find(c => c.name == clipName);
        if (existing != null)
        {
            existing.frames = allSprites;
            existing.frameRate = frameRate;
            existing.loop = loop;
        }
        else
        {
            var clip = new AnimationClip
            {
                name = clipName,
                frames = allSprites,
                frameRate = frameRate,
                loop = loop
            };
            animations.Add(clip);
        }

        Debug.Log($"[SpriteSheetAnimator] 加载动画 '{clipName}': {allSprites.Length} 帧 (来源: Resources/{resourcePath})");
    }

    private void Update()
    {
        if (!isPlaying || currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0)
            return;

        // 累加计时器
        currentClip.timer += Time.deltaTime;

        // 计算帧间隔
        float frameInterval = 1f / currentClip.frameRate;

        // 检查是否到达下一帧
        while (currentClip.timer >= frameInterval)
        {
            currentClip.timer -= frameInterval;
            currentClip.currentFrame++;

            // 循环处理
            if (currentClip.currentFrame >= currentClip.frames.Length)
            {
                if (currentClip.loop)
                    currentClip.currentFrame = 0;
                else
                {
                    currentClip.currentFrame = currentClip.frames.Length - 1;
                    isPlaying = false;
                    return;
                }
            }

            // 显示当前帧
            spriteRenderer.sprite = currentClip.frames[currentClip.currentFrame];
        }
    }

    /// <summary>
    /// 播放指定动画
    /// </summary>
    public void Play(string clipName, bool forceRestart = false)
    {
        // 查找动画
        AnimationClip clip = animations.Find(c => c.name == clipName);
        if (clip == null || clip.frames == null || clip.frames.Length == 0)
        {
            Debug.LogWarning($"动画 '{clipName}' 未找到或帧为空！");
            return;
        }

        // 如果是当前动画且未强制重启，忽略
        if (!forceRestart && isPlaying && currentClip != null && currentClip.name == clipName)
            return;

        // 切换动画
        currentClip = clip;
        currentClip.currentFrame = 0;
        currentClip.timer = 0f;
        isPlaying = true;

        // 显示第一帧
        if (currentClip.frames.Length > 0)
            spriteRenderer.sprite = currentClip.frames[0];
    }

    /// <summary>
    /// 停止当前动画
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 暂停当前动画
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 恢复当前动画
    /// </summary>
    public void Resume()
    {
        if (currentClip != null)
            isPlaying = true;
    }

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// 当前动画名称
    /// </summary>
    public string CurrentClipName => currentClip?.name;
}
