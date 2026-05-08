using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight ambient events for campus locations.
/// These events make locations feel alive without needing JSON event authoring.
/// </summary>
public class LocationRandomEventSystem : MonoBehaviour
{
    public static LocationRandomEventSystem Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float moveEventChance = 0.28f;
    [SerializeField, Range(0f, 1f)] private float actionEventChance = 0.18f;
    [SerializeField] private int minActionsBetweenEvents = 2;
    [SerializeField] private bool useLegacyAmbientEvents = false;

    private readonly Dictionary<LocationId, List<LocationAmbientEvent>> eventsByLocation =
        new Dictionary<LocationId, List<LocationAmbientEvent>>();

    private int actionsSinceLastEvent = 99;
    private int lastEventRound = -1;
    private LocationId? lastEventLocation;

    private sealed class LocationAmbientEvent
    {
        public LocationId location;
        public string speaker;
        public string[] lines;
        public AttributeEffect[] effects;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitEvents();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged -= HandleLocationChanged;
        }

        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= HandleActionExecuted;
        }
    }

    private void Subscribe()
    {
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged -= HandleLocationChanged;
            LocationManager.Instance.OnLocationChanged += HandleLocationChanged;
        }

        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= HandleActionExecuted;
            ActionSystem.Instance.OnActionExecuted += HandleActionExecuted;
        }
    }

    private void HandleLocationChanged(LocationId from, LocationId to)
    {
        TryTrigger(to, moveEventChance);
    }

    private void HandleActionExecuted(ActionDefinition action)
    {
        actionsSinceLastEvent++;

        if (GameState.Instance == null)
        {
            return;
        }

        TryTrigger(GameState.Instance.CurrentLocation, actionEventChance);
    }

    private void TryTrigger(LocationId location, float chance)
    {
        if (!useLegacyAmbientEvents)
        {
            return;
        }

        if (!CanTrigger(location, chance))
        {
            return;
        }

        if (!eventsByLocation.TryGetValue(location, out List<LocationAmbientEvent> events) || events.Count == 0)
        {
            return;
        }

        LocationAmbientEvent selected = events[UnityEngine.Random.Range(0, events.Count)];
        ApplyEffects(selected.effects);

        if (DialogueSystem.Instance != null && selected.lines != null && selected.lines.Length > 0)
        {
            DialogueSystem.Instance.StartDialogue(selected.speaker, selected.lines);
        }

        actionsSinceLastEvent = 0;
        lastEventLocation = location;
        lastEventRound = GameState.Instance != null ? GameState.Instance.CurrentRound : -1;
    }

    private bool CanTrigger(LocationId location, float chance)
    {
        if (UnityEngine.Random.value > chance)
        {
            return false;
        }

        if (actionsSinceLastEvent < minActionsBetweenEvents)
        {
            return false;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            return false;
        }

        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting)
        {
            return false;
        }

        if (GameState.Instance != null &&
            lastEventLocation.HasValue &&
            lastEventLocation.Value == location &&
            lastEventRound == GameState.Instance.CurrentRound)
        {
            return false;
        }

        return true;
    }

    private void ApplyEffects(AttributeEffect[] effects)
    {
        if (effects == null || PlayerAttributes.Instance == null)
        {
            return;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            AttributeEffect effect = effects[i];
            if (effect != null)
            {
                PlayerAttributes.Instance.AddAttribute(effect.attributeName, effect.amount);
            }
        }
    }

    private void Add(LocationId location, string speaker, string[] lines, params AttributeEffect[] effects)
    {
        if (!eventsByLocation.TryGetValue(location, out List<LocationAmbientEvent> events))
        {
            events = new List<LocationAmbientEvent>();
            eventsByLocation[location] = events;
        }

        events.Add(new LocationAmbientEvent
        {
            location = location,
            speaker = speaker,
            lines = lines,
            effects = effects
        });
    }

    private void InitEvents()
    {
        eventsByLocation.Clear();

        Add(LocationId.Dormitory, "室友", new[]
        {
            "你刚回宿舍，就听见室友在讨论今晚要不要一起整理寝室。",
            "桌面清爽了一点，连心情都轻了些。"
        }, new AttributeEffect("心情", 2), new AttributeEffect("压力", -2));

        Add(LocationId.Dormitory, "旁白", new[]
        {
            "宿舍楼道里传来熟悉的笑声。",
            "大学生活好像就是由这些零碎的小声音拼起来的。"
        }, new AttributeEffect("魅力", 1));

        Add(LocationId.Canteen, "旁白", new[]
        {
            "排队时，你听到隔壁桌聊起最近的课程和社团招新。",
            "这些传闻也许以后会派上用场。"
        }, new AttributeEffect("魅力", 1), new AttributeEffect("心情", 1));

        Add(LocationId.Canteen, "食堂阿姨", new[]
        {
            "今天来得巧，刚出锅。",
            "热腾腾的一餐让你恢复了不少精神。"
        }, new AttributeEffect("心情", 2), new AttributeEffect("体魄", 1));

        Add(LocationId.TeachingBuilding, "任课老师", new[]
        {
            "你在走廊遇到任课老师，被顺手点拨了一个容易错的知识点。",
            "有些问题，好像突然就想通了。"
        }, new AttributeEffect("学力", 2), new AttributeEffect("压力", 1));

        Add(LocationId.TeachingBuilding, "同学", new[]
        {
            "课间有人问你要不要一起做小组作业。",
            "你记下了对方的联系方式。"
        }, new AttributeEffect("领导力", 1), new AttributeEffect("魅力", 1));

        Add(LocationId.Library, "旁白", new[]
        {
            "图书馆靠窗的位置还空着。",
            "阳光落在书页上，你难得安静地读完了一整节。"
        }, new AttributeEffect("学力", 2), new AttributeEffect("压力", -1));

        Add(LocationId.Library, "学姐", new[]
        {
            "你在书架旁遇到一位学姐，她推荐了一本很适合入门的参考书。",
            "你把书名记进了备忘录。"
        }, new AttributeEffect("学力", 2), new AttributeEffect("魅力", 1));

        Add(LocationId.Playground, "旁白", new[]
        {
            "晚风从操场边吹过，跑道上还有人在慢跑。",
            "你活动了一下肩颈，压力散去不少。"
        }, new AttributeEffect("压力", -3), new AttributeEffect("心情", 2));

        Add(LocationId.Playground, "体育委员", new[]
        {
            "体育委员提醒大家最近可以提前练练体测项目。",
            "你跟着做了几组热身。"
        }, new AttributeEffect("体魄", 1));

        Add(LocationId.Store, "店员", new[]
        {
            "教超新上了一批文具和速食。",
            "你挑了几样实用的小东西。"
        }, new AttributeEffect("心情", 1));

        Add(LocationId.Store, "旁白", new[]
        {
            "货架尽头贴着社团活动的海报。",
            "你多看了两眼，记下了报名时间。"
        }, new AttributeEffect("领导力", 1));

        Add(LocationId.ExpressStation, "驿站同学", new[]
        {
            "快递站今天有点忙，你顺手帮忙把几个包裹摆正。",
            "对方笑着向你道谢。"
        }, new AttributeEffect("领导力", 1), new AttributeEffect("心情", 1));

        Add(LocationId.ExpressStation, "旁白", new[]
        {
            "你在货架上找到自己的包裹，里面是之前买的学习资料。",
            "看来今晚可以继续推进计划了。"
        }, new AttributeEffect("学力", 1));

        Add(LocationId.TakeoutStation, "外卖员", new[]
        {
            "外卖员赶时间，把几份餐交给你帮忙看一下。",
            "短短几分钟，你认识了两个同楼的同学。"
        }, new AttributeEffect("魅力", 1), new AttributeEffect("心情", 1));

        Add(LocationId.TakeoutStation, "旁白", new[]
        {
            "外卖站的灯光亮着，空气里混着夜宵的香味。",
            "偶尔偷个懒，也不是世界末日。"
        }, new AttributeEffect("心情", 2), new AttributeEffect("压力", -1));
    }
}
