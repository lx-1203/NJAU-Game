using UnityEngine;
using System;

/// <summary>
/// 玩家属性数据类 —— 管理学力、魅力、体魄、领导力、压力、心情等角色属性
/// </summary>
public class PlayerAttributes : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static PlayerAttributes Instance { get; private set; }

    // ========== 事件 ==========
    /// <summary>任何属性发生变化时触发</summary>
    public event Action OnAttributesChanged;

    // ========== 常量 ==========
    /// <summary>状态值上限（压力/心情）</summary>
    public const int MaxStatusValue = 100;

    // ========== 属性定义 ==========

    /// <summary>单个属性的完整信息</summary>
    [Serializable]
    public class AttributeInfo
    {
        public string name;        // 属性名称
        public Color barColor;     // 进度条颜色
        public int value;          // 当前值
        public int maxValue;       // 上限值
        public bool isPercentage;  // 是否以百分比形式显示（压力/心情）

        public AttributeInfo(string name, Color color, int value, int maxValue, bool isPercentage = false)
        {
            this.name = name;
            this.barColor = color;
            this.value = value;
            this.maxValue = maxValue;
            this.isPercentage = isPercentage;
        }

        /// <summary>归一化进度 0~1</summary>
        public float NormalizedValue => maxValue > 0 ? (float)value / maxValue : 0f;
    }

    // ========== 内部字段 ==========
    [Header("核心属性")]
    [SerializeField] private int study = 10;       // 学力
    [SerializeField] private int charm = 5;        // 魅力
    [SerializeField] private int physique = 8;     // 体魄
    [SerializeField] private int leadership = 3;   // 领导力

    [Header("状态值 (0-100)")]
    [SerializeField] private int stress = 20;      // 压力
    [SerializeField] private int mood = 70;        // 心情

    [Header("隐性属性")]
    [SerializeField] private int darkness = 0;       // 黑暗值
    [SerializeField] private int guilt = 0;          // 负罪感
    [SerializeField] private int luck = 50;          // 幸运

    // ========== 属性颜色定义 ==========
    private static readonly Color ColorStudy      = new Color(0.30f, 0.60f, 1.00f); // 蓝色 - 学力
    private static readonly Color ColorCharm      = new Color(1.00f, 0.45f, 0.65f); // 粉色 - 魅力
    private static readonly Color ColorPhysique   = new Color(0.30f, 0.85f, 0.40f); // 绿色 - 体魄
    private static readonly Color ColorLeadership = new Color(1.00f, 0.75f, 0.20f); // 金色 - 领导力
    private static readonly Color ColorStress     = new Color(0.90f, 0.30f, 0.30f); // 红色 - 压力
    private static readonly Color ColorMood       = new Color(0.55f, 0.85f, 0.95f); // 天蓝 - 心情
    private static readonly Color ColorDarkness   = new Color(0.40f, 0.10f, 0.50f); // 紫色 - 黑暗值
    private static readonly Color ColorGuilt      = new Color(0.50f, 0.50f, 0.50f); // 灰色 - 负罪感
    private static readonly Color ColorLuck       = new Color(1.00f, 0.90f, 0.30f); // 金色 - 幸运

    // ========== 属性访问器 ==========

    public int Study
    {
        get => study;
        set { study = Mathf.Max(0, value); NotifyChanged(); }
    }

    public int Charm
    {
        get => charm;
        set { charm = Mathf.Max(0, value); NotifyChanged(); }
    }

    public int Physique
    {
        get => physique;
        set { physique = Mathf.Max(0, value); NotifyChanged(); }
    }

    public int Leadership
    {
        get => leadership;
        set { leadership = Mathf.Max(0, value); NotifyChanged(); }
    }

    public int Stress
    {
        get => stress;
        set { stress = Mathf.Clamp(value, 0, MaxStatusValue); NotifyChanged(); }
    }

    public int Mood
    {
        get => mood;
        set { mood = Mathf.Clamp(value, 0, MaxStatusValue); NotifyChanged(); }
    }

    public int Darkness
    {
        get => darkness;
        set { darkness = Mathf.Max(0, value); NotifyChanged(); }
    }

    public int Guilt
    {
        get => guilt;
        set { guilt = Mathf.Clamp(value, 0, MaxStatusValue); NotifyChanged(); }
    }

    public int Luck
    {
        get => luck;
        set { luck = Mathf.Clamp(value, 0, MaxStatusValue); NotifyChanged(); }
    }

    // ========== 便捷方法 ==========

    /// <summary>
    /// 获取所有属性的信息列表，用于 HUD 属性条渲染
    /// 核心属性的 maxValue 暂定为 100（可扩展为动态上限）
    /// </summary>
    public AttributeInfo[] GetAllAttributes()
    {
        return new AttributeInfo[]
        {
            new AttributeInfo("学力", ColorStudy,      study,      100, false),
            new AttributeInfo("魅力", ColorCharm,      charm,      100, false),
            new AttributeInfo("体魄", ColorPhysique,   physique,   100, false),
            new AttributeInfo("领导力", ColorLeadership, leadership, 100, false),
            new AttributeInfo("压力", ColorStress,     stress,     MaxStatusValue, true),
            new AttributeInfo("心情", ColorMood,       mood,       MaxStatusValue, true),
        };
    }

    /// <summary>增加属性值</summary>
    public void AddAttribute(string attrName, int amount)
    {
        switch (attrName)
        {
            case "学力":   Study += amount; break;
            case "魅力":   Charm += amount; break;
            case "体魄":   Physique += amount; break;
            case "领导力": Leadership += amount; break;
            case "压力":   Stress += amount; break;
            case "心情":   Mood += amount; break;
            case "黑暗值": Darkness += amount; break;
            case "负罪感": Guilt += amount; break;
            case "幸运":   Luck += amount; break;
            default:
                Debug.LogWarning($"[PlayerAttributes] 未知属性名: {attrName}");
                break;
        }
    }

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void NotifyChanged()
    {
        OnAttributesChanged?.Invoke();
    }

    // ========== ISaveable 实现 ==========

    /// <summary>将玩家属性写入存档数据</summary>
    public void SaveToData(SaveData data)
    {
        data.study = study;
        data.charm = charm;
        data.physique = physique;
        data.leadership = leadership;
        data.stress = stress;
        data.mood = mood;
        data.darkness = darkness;
        data.guilt = guilt;
        data.luck = luck;
    }

    /// <summary>从存档数据恢复玩家属性（通过属性访问器校验值域）</summary>
    public void LoadFromData(SaveData data)
    {
        Study = data.study;
        Charm = data.charm;
        Physique = data.physique;
        Leadership = data.leadership;
        Stress = data.stress;
        Mood = data.mood;
        Darkness = data.darkness;
        Guilt = data.guilt;
        Luck = data.luck;
    }

    /// <summary>批量设置所有属性（供调试工具使用）</summary>
    public void SetAll(int study, int charm, int physique, int leadership, int stress, int mood,
                       int darkness = -1, int guilt = -1, int luck = -1)
    {
        this.study = Mathf.Max(0, study);
        this.charm = Mathf.Max(0, charm);
        this.physique = Mathf.Max(0, physique);
        this.leadership = Mathf.Max(0, leadership);
        this.stress = Mathf.Clamp(stress, 0, MaxStatusValue);
        this.mood = Mathf.Clamp(mood, 0, MaxStatusValue);
        if (darkness >= 0) this.darkness = Mathf.Max(0, darkness);
        if (guilt >= 0) this.guilt = Mathf.Clamp(guilt, 0, MaxStatusValue);
        if (luck >= 0) this.luck = Mathf.Clamp(luck, 0, MaxStatusValue);
        NotifyChanged();
    }
}
