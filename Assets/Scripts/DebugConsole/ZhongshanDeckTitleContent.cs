using System;
using System.Collections.Generic;

[Serializable]
public class ZhongshanDeckTitleContent
{
    public ZhongshanDeckHomepageContent homepage = new ZhongshanDeckHomepageContent();
    public ZhongshanDeckCreditsContent credits = new ZhongshanDeckCreditsContent();
    public ZhongshanDeckChangelogContent changelog = new ZhongshanDeckChangelogContent();
    public ZhongshanDeckTutorialContent tutorial = new ZhongshanDeckTutorialContent();

    public void EnsureInitialized()
    {
        homepage ??= new ZhongshanDeckHomepageContent();
        credits ??= new ZhongshanDeckCreditsContent();
        changelog ??= new ZhongshanDeckChangelogContent();
        tutorial ??= new ZhongshanDeckTutorialContent();

        homepage.EnsureInitialized();
        credits.EnsureInitialized();
        changelog.EnsureInitialized();
        tutorial.EnsureInitialized();
    }

    public ZhongshanDeckTitleContent Clone()
    {
        return new ZhongshanDeckTitleContent
        {
            homepage = homepage != null ? homepage.Clone() : new ZhongshanDeckHomepageContent(),
            credits = credits != null ? credits.Clone() : new ZhongshanDeckCreditsContent(),
            changelog = changelog != null ? changelog.Clone() : new ZhongshanDeckChangelogContent(),
            tutorial = tutorial != null ? tutorial.Clone() : new ZhongshanDeckTutorialContent()
        };
    }
}

[Serializable]
public class ZhongshanDeckHomepageContent
{
    public string hintMessage = "点击任意位置继续";
    public string changelogButtonLabel = "更新日志";
    public string settingsPanelTitle = "设置";
    public string settingsBackButtonLabel = "返回";
    public List<ZhongshanDeckMenuActionLabel> mainMenuItems = new List<ZhongshanDeckMenuActionLabel>();
    public List<ZhongshanDeckIconEntry> topIcons = new List<ZhongshanDeckIconEntry>();
    public List<ZhongshanDeckHomepageLayoutItem> layoutItems = new List<ZhongshanDeckHomepageLayoutItem>();

    public void EnsureInitialized()
    {
        hintMessage ??= "点击任意位置继续";
        changelogButtonLabel ??= "更新日志";
        settingsPanelTitle ??= "设置";
        settingsBackButtonLabel ??= "返回";
        mainMenuItems ??= new List<ZhongshanDeckMenuActionLabel>();
        topIcons ??= new List<ZhongshanDeckIconEntry>();
        layoutItems ??= new List<ZhongshanDeckHomepageLayoutItem>();

        if (mainMenuItems.Count == 0)
        {
            mainMenuItems.AddRange(ZhongshanDeckTitleContentDefaults.CreateHomepageMenuItems());
        }

        if (topIcons.Count == 0)
        {
            topIcons.AddRange(ZhongshanDeckTitleContentDefaults.CreateHomepageTopIcons());
        }

        ZhongshanDeckTitleContentDefaults.EnsureHomepageLayoutItems(layoutItems);

        for (int i = 0; i < mainMenuItems.Count; i++)
        {
            mainMenuItems[i]?.EnsureInitialized();
        }

        for (int i = 0; i < topIcons.Count; i++)
        {
            topIcons[i]?.EnsureInitialized();
        }

        for (int i = 0; i < layoutItems.Count; i++)
        {
            layoutItems[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckHomepageContent Clone()
    {
        ZhongshanDeckHomepageContent clone = new ZhongshanDeckHomepageContent
        {
            hintMessage = hintMessage,
            changelogButtonLabel = changelogButtonLabel,
            settingsPanelTitle = settingsPanelTitle,
            settingsBackButtonLabel = settingsBackButtonLabel,
            mainMenuItems = new List<ZhongshanDeckMenuActionLabel>(),
            topIcons = new List<ZhongshanDeckIconEntry>(),
            layoutItems = new List<ZhongshanDeckHomepageLayoutItem>()
        };

        for (int i = 0; i < mainMenuItems.Count; i++)
        {
            if (mainMenuItems[i] != null)
            {
                clone.mainMenuItems.Add(mainMenuItems[i].Clone());
            }
        }

        for (int i = 0; i < topIcons.Count; i++)
        {
            if (topIcons[i] != null)
            {
                clone.topIcons.Add(topIcons[i].Clone());
            }
        }

        for (int i = 0; i < layoutItems.Count; i++)
        {
            if (layoutItems[i] != null)
            {
                clone.layoutItems.Add(layoutItems[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public enum ZhongshanDeckLayoutAnchor
{
    Center,
    TopLeft,
    TopCenter,
    TopRight,
    LeftCenter,
    RightCenter,
    BottomLeft,
    BottomCenter,
    BottomRight
}

[Serializable]
public class ZhongshanDeckHomepageLayoutItem
{
    public string key;
    public string displayName;
    public ZhongshanDeckLayoutAnchor anchor = ZhongshanDeckLayoutAnchor.Center;
    public UnityEngine.Vector2 anchoredPosition;
    public UnityEngine.Vector2 size = new UnityEngine.Vector2(100f, 100f);
    public bool visible = true;
    public bool locked;

    public void EnsureInitialized()
    {
        key ??= string.Empty;
        displayName ??= key ?? string.Empty;
        size.x = UnityEngine.Mathf.Max(24f, size.x);
        size.y = UnityEngine.Mathf.Max(24f, size.y);
    }

    public ZhongshanDeckHomepageLayoutItem Clone()
    {
        return new ZhongshanDeckHomepageLayoutItem
        {
            key = key,
            displayName = displayName,
            anchor = anchor,
            anchoredPosition = anchoredPosition,
            size = size,
            visible = visible,
            locked = locked
        };
    }
}

[Serializable]
public class ZhongshanDeckMenuActionLabel
{
    public string actionId;
    public string label;

    public void EnsureInitialized()
    {
        actionId ??= string.Empty;
        label ??= string.Empty;
    }

    public ZhongshanDeckMenuActionLabel Clone()
    {
        return new ZhongshanDeckMenuActionLabel
        {
            actionId = actionId,
            label = label
        };
    }
}

[Serializable]
public class ZhongshanDeckIconEntry
{
    public string actionId;
    public string label;
    public string tooltip;

    public void EnsureInitialized()
    {
        actionId ??= string.Empty;
        label ??= string.Empty;
        tooltip ??= string.Empty;
    }

    public ZhongshanDeckIconEntry Clone()
    {
        return new ZhongshanDeckIconEntry
        {
            actionId = actionId,
            label = label,
            tooltip = tooltip
        };
    }
}

[Serializable]
public class ZhongshanDeckCreditsContent
{
    public string panelTitle = "制作人";
    public string tabLabel = "STAFF";
    public string footerText = "感谢每一次选择、每一次反馈，以及每一个把钟山下玩下去的周目。";
    public List<ZhongshanDeckCreditsEntry> entries = new List<ZhongshanDeckCreditsEntry>();

    public void EnsureInitialized()
    {
        panelTitle ??= "制作人";
        tabLabel ??= "STAFF";
        footerText ??= "感谢每一次选择、每一次反馈，以及每一个把钟山下玩下去的周目。";
        entries ??= new List<ZhongshanDeckCreditsEntry>();

        if (entries.Count == 0)
        {
            entries.AddRange(ZhongshanDeckTitleContentDefaults.CreateCreditsEntries());
        }

        for (int i = 0; i < entries.Count; i++)
        {
            entries[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckCreditsContent Clone()
    {
        ZhongshanDeckCreditsContent clone = new ZhongshanDeckCreditsContent
        {
            panelTitle = panelTitle,
            tabLabel = tabLabel,
            footerText = footerText,
            entries = new List<ZhongshanDeckCreditsEntry>()
        };

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                clone.entries.Add(entries[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ZhongshanDeckCreditsEntry
{
    public string title;
    public string role;
    public string description;
    public List<string> tags = new List<string>();

    public void EnsureInitialized()
    {
        title ??= string.Empty;
        role ??= string.Empty;
        description ??= string.Empty;
        tags ??= new List<string>();
    }

    public ZhongshanDeckCreditsEntry Clone()
    {
        return new ZhongshanDeckCreditsEntry
        {
            title = title,
            role = role,
            description = description,
            tags = new List<string>(tags ?? new List<string>())
        };
    }
}

[Serializable]
public class ZhongshanDeckChangelogContent
{
    public string panelTitle = "更新日志";
    public List<ZhongshanDeckChangelogSection> sections = new List<ZhongshanDeckChangelogSection>();

    public void EnsureInitialized()
    {
        panelTitle ??= "更新日志";
        sections ??= new List<ZhongshanDeckChangelogSection>();

        if (sections.Count == 0)
        {
            sections.AddRange(ZhongshanDeckTitleContentDefaults.CreateChangelogSections());
        }

        for (int i = 0; i < sections.Count; i++)
        {
            sections[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckChangelogContent Clone()
    {
        ZhongshanDeckChangelogContent clone = new ZhongshanDeckChangelogContent
        {
            panelTitle = panelTitle,
            sections = new List<ZhongshanDeckChangelogSection>()
        };

        for (int i = 0; i < sections.Count; i++)
        {
            if (sections[i] != null)
            {
                clone.sections.Add(sections[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ZhongshanDeckChangelogSection
{
    public string heading;
    public List<string> bulletLines = new List<string>();
    public string note;

    public void EnsureInitialized()
    {
        heading ??= string.Empty;
        bulletLines ??= new List<string>();
        note ??= string.Empty;
    }

    public ZhongshanDeckChangelogSection Clone()
    {
        return new ZhongshanDeckChangelogSection
        {
            heading = heading,
            bulletLines = new List<string>(bulletLines ?? new List<string>()),
            note = note
        };
    }
}

[Serializable]
public class ZhongshanDeckTutorialContent
{
    public string panelTitle = "新生手册";
    public List<ZhongshanDeckTutorialCategoryData> categories = new List<ZhongshanDeckTutorialCategoryData>();

    public void EnsureInitialized()
    {
        panelTitle ??= "新生手册";
        categories ??= new List<ZhongshanDeckTutorialCategoryData>();

        if (categories.Count == 0)
        {
            categories.AddRange(ZhongshanDeckTitleContentDefaults.CreateTutorialCategories());
        }

        for (int i = 0; i < categories.Count; i++)
        {
            categories[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckTutorialContent Clone()
    {
        ZhongshanDeckTutorialContent clone = new ZhongshanDeckTutorialContent
        {
            panelTitle = panelTitle,
            categories = new List<ZhongshanDeckTutorialCategoryData>()
        };

        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i] != null)
            {
                clone.categories.Add(categories[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ZhongshanDeckTutorialCategoryData
{
    public string name;
    public List<ZhongshanDeckTutorialEntryData> entries = new List<ZhongshanDeckTutorialEntryData>();

    public void EnsureInitialized()
    {
        name ??= string.Empty;
        entries ??= new List<ZhongshanDeckTutorialEntryData>();
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckTutorialCategoryData Clone()
    {
        ZhongshanDeckTutorialCategoryData clone = new ZhongshanDeckTutorialCategoryData
        {
            name = name,
            entries = new List<ZhongshanDeckTutorialEntryData>()
        };

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                clone.entries.Add(entries[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ZhongshanDeckTutorialEntryData
{
    public string title;
    public string lead;
    public string description;
    public List<string> highlights = new List<string>();

    public void EnsureInitialized()
    {
        title ??= string.Empty;
        lead ??= string.Empty;
        description ??= string.Empty;
        highlights ??= new List<string>();
    }

    public ZhongshanDeckTutorialEntryData Clone()
    {
        return new ZhongshanDeckTutorialEntryData
        {
            title = title,
            lead = lead,
            description = description,
            highlights = new List<string>(highlights ?? new List<string>())
        };
    }
}

public static class ZhongshanDeckTitleContentDefaults
{
    public const string LayoutLogo = "logo";
    public const string LayoutMainMenu = "main_menu";
    public const string LayoutTopIcons = "top_icons";
    public const string LayoutChangelog = "changelog_button";
    public const string LayoutHint = "hint_text";
    public const string LayoutVersion = "version_text";

    public static List<ZhongshanDeckCreditsEntry> CreateCreditsEntries()
    {
        return new List<ZhongshanDeckCreditsEntry>
        {
            CreateCreditsEntry("总制作", "项目策划 / 系统统筹", "负责《钟山下》的整体玩法框架、大学生活循环、系统节奏与内容整合。", "玩法框架", "系统节奏", "内容整合"),
            CreateCreditsEntry("程序实现", "Unity / 纯代码 UI / 存档系统", "实现标题界面、HUD、任务、成就、考试、社团、恋爱、事件、存档等核心系统，并保持各模块可独立维护。", "Unity", "C#", "模块化"),
            CreateCreditsEntry("美术与界面", "视觉风格 / 界面布局", "以手账、纸张、校园笔记为主视觉参考，统一首页入口、教程、成就、CG 与制作人界面的阅读体验。", "手账风", "纸张界面", "校园感"),
            CreateCreditsEntry("文本与世界观", "剧情设定 / 人物关系", "围绕大学四年成长主题，搭建人物关系、校园事件、结局路线与不同价值取舍。", "大学四年", "人物关系", "多结局"),
            CreateCreditsEntry("特别鸣谢", "测试 / 反馈 / 灵感", "感谢所有参与测试、提供反馈、提出想法与陪伴项目迭代的人。", "测试反馈", "灵感来源", "持续迭代")
        };
    }

    public static List<ZhongshanDeckMenuActionLabel> CreateHomepageMenuItems()
    {
        return new List<ZhongshanDeckMenuActionLabel>
        {
            CreateMenuItem("continue", "继续游戏"),
            CreateMenuItem("start", "开始游戏"),
            CreateMenuItem("load", "载入游戏"),
            CreateMenuItem("settings", "设  置"),
            CreateMenuItem("quit", "退出游戏")
        };
    }

    public static List<ZhongshanDeckIconEntry> CreateHomepageTopIcons()
    {
        return new List<ZhongshanDeckIconEntry>
        {
            CreateTopIcon("tutorial", "教程", "游戏教程"),
            CreateTopIcon("achievement", "成就", "成就"),
            CreateTopIcon("gallery", "CG", "游戏CG"),
            CreateTopIcon("credits", "制作人", "制作人详情")
        };
    }

    public static void EnsureHomepageLayoutItems(List<ZhongshanDeckHomepageLayoutItem> layoutItems)
    {
        if (layoutItems == null)
        {
            return;
        }

        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutLogo, "Logo", ZhongshanDeckLayoutAnchor.Center, new UnityEngine.Vector2(0f, 200f), new UnityEngine.Vector2(900f, 330f)));
        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutMainMenu, "主菜单", ZhongshanDeckLayoutAnchor.Center, new UnityEngine.Vector2(0f, -100f), new UnityEngine.Vector2(420f, 480f)));
        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutTopIcons, "右上图标", ZhongshanDeckLayoutAnchor.TopRight, new UnityEngine.Vector2(-24f, -20f), new UnityEngine.Vector2(292f, 80f)));
        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutChangelog, "更新日志按钮", ZhongshanDeckLayoutAnchor.BottomLeft, new UnityEngine.Vector2(44f, 46f), new UnityEngine.Vector2(300f, 74f)));
        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutHint, "点击提示", ZhongshanDeckLayoutAnchor.BottomCenter, new UnityEngine.Vector2(0f, 130f), new UnityEngine.Vector2(800f, 80f)));
        EnsureLayoutItem(layoutItems, CreateHomepageLayoutItem(LayoutVersion, "版本号", ZhongshanDeckLayoutAnchor.BottomRight, new UnityEngine.Vector2(-16f, 12f), new UnityEngine.Vector2(200f, 36f)));
    }

    public static List<ZhongshanDeckChangelogSection> CreateChangelogSections()
    {
        return new List<ZhongshanDeckChangelogSection>
        {
            new ZhongshanDeckChangelogSection
            {
                heading = "更新补丁 1.90",
                bulletLines = new List<string>
                {
                    "同学录增加Q版CG栏目",
                    "创意工坊的立绘编辑页面增加“Q版头像”设置",
                    "修复了状态事件的选项数量不正确的问题",
                    "修复了状态事件的点击无法进入下一句的问题",
                    "修复了“已养成性格”卡住的问题",
                    "完善了大量百科错误",
                    "修复了开放人格对职业潜力的错误加成",
                    "调整了各挚友特性加性格倾向的数值",
                    "修复跑步小游戏中点击跳过按钮导致双倍奖励的bug",
                    "修复了部分文本错误"
                },
                note = "后续版本会继续补充首页入口、同学录和系统体验优化。"
            }
        };
    }

    public static List<ZhongshanDeckTutorialCategoryData> CreateTutorialCategories()
    {
        return new List<ZhongshanDeckTutorialCategoryData>
        {
            CreateTutorialCategory("属性",
                CreateTutorialEntry("智力", "决定课程学习、考试通过率与部分学术事件。", "智力越高，学习类行动带来的成长越稳定，也更容易解锁偏学术路线的内容。", "课程成绩", "考试修正", "学术事件"),
                CreateTutorialEntry("情商", "影响社交对话、关系推进与部分组织活动。", "情商主要作用在角色互动和分支选择，很多人物线与组织机会都会检查这项数值。", "社交判定", "人物剧情", "组织互动"),
                CreateTutorialEntry("体魄", "决定体测、运动、部分兼职与高压状态下的稳定性。", "体魄不仅决定运动收益，也会影响你在高压力阶段是否容易崩盘。", "体测表现", "运动收益", "抗压稳定"),
                CreateTutorialEntry("精力", "每天可支配行动能力的直接体现。", "精力不足会让你无法连续高强度安排学习、社交和兼职，需要用休息和节奏管理来维持。", "行动安排", "休息恢复", "效率上限"),
                CreateTutorialEntry("零花钱", "覆盖消费、部分活动门槛与经济路线发展。", "钱不只是资源，还会影响很多机会是否出现。部分事件能让你快速赚钱，也可能快速负债。", "商店消费", "活动门槛", "债务风险"),
                CreateTutorialEntry("成就", "记录阶段性达成，用来回看你的关键进展。", "成就既是收藏，也经常是路线完成度的旁证，能帮助你判断当前周目的发展方向。", "路线进度", "关键节点", "收集目标")),
            CreateTutorialCategory("机制",
                CreateTutorialEntry("行动回合", "每个阶段都有固定行动次数。", "学习、社交、兼职、探索都会消耗回合，首页教程建议你优先熟悉每回合的机会成本。", "阶段规划", "行动消耗", "路线节奏"),
                CreateTutorialEntry("课程与考试", "课程成绩会在学期末集中结算。", "平时学习、专项训练和临时抱佛脚都会进入考试判定，但收益和风险不同。", "平时积累", "考试结算", "高风险补救"),
                CreateTutorialEntry("压力与心情", "长期失衡会拖慢成长，甚至触发负面链条。", "不要只追单一属性。高压低心情会降低稳定性，很多负面事件都从这里开始。", "状态管理", "负面事件", "恢复手段")),
            CreateTutorialCategory("方法论",
                CreateTutorialEntry("前期思路", "先建立一条稳定增长线，再考虑扩张。", "开局推荐先确定 1 到 2 个主目标，比如学业线加社交线，避免什么都做导致资源分散。", "主线选择", "资源集中", "前期稳态"),
                CreateTutorialEntry("中期转向", "根据事件、人物和经济情况调整路径。", "中期最容易因为新机会而分心。教程建议只在回报明显超过当前路线时再切换。", "机会判断", "路径切换", "收益比较"),
                CreateTutorialEntry("补短板", "不要让明显短板卡住关键节点。", "某些系统会检查最低门槛。与其追求极致数值，不如保证关键属性不过低。", "门槛检查", "低风险推进", "容错空间")),
            CreateTutorialCategory("人格",
                CreateTutorialEntry("性格取向", "你的选择会逐步塑造角色气质。", "很多选项不会立刻给出巨大收益，但会持续影响人物评价、事件风格和后续分支。", "人物印象", "分支语气", "长期累积"),
                CreateTutorialEntry("动力与热情", "决定你能否持续推进长期目标。", "短期高收益不一定适合长期路线。热情和动力更像耐久值，决定你能走多远。", "长期路线", "持续投入", "发展韧性")),
            CreateTutorialCategory("专长",
                CreateTutorialEntry("能力专精", "围绕一项主属性构筑专长最有效。", "专长不是平均加点，而是把行动、人物与资源集中到一条能持续放大的线。", "属性联动", "路线强化", "收益放大"),
                CreateTutorialEntry("跨界组合", "少量副属性能显著提高路线手感。", "比如学业线搭配一点情商，能让许多人物事件更顺；兼职线搭配体魄，容错更高。", "副属性支持", "事件兼容", "容错增强")),
            CreateTutorialCategory("人物",
                CreateTutorialEntry("角色关系", "人物不只是剧情对象，也是资源与信息来源。", "关系推进后，很多人物会带来独有行动、特殊事件或成长捷径。", "专属事件", "隐藏机会", "互动收益"),
                CreateTutorialEntry("好感管理", "不要只看短期涨幅，要看后续解锁。", "有些人物前期收益不高，但后续路线价值很大。教程面板建议优先观察他们能解锁什么。", "解锁条件", "长期收益", "路线价值")),
            CreateTutorialCategory("职业",
                CreateTutorialEntry("兼职选择", "兼职是钱和成长的交换。", "低门槛兼职适合保底，高门槛兼职更适合中后期冲收益。选择时看你缺的是钱、属性还是事件。", "保底收入", "高门槛回报", "路线匹配"),
                CreateTutorialEntry("发展方向", "职业倾向会反向影响你的养成重点。", "如果你想走更现实的功利路线，经济和执行相关属性的比重要尽早提上来。", "养成重点", "路线风格", "资源倾斜")),
            CreateTutorialCategory("人生观",
                CreateTutorialEntry("价值取舍", "每次选择都在定义你想成为什么样的人。", "成长并不只看面板变大。不同价值取向会让同一事件出现完全不同的结果。", "事件分歧", "路线气质", "结局影响"),
                CreateTutorialEntry("长短期平衡", "眼前收益和长线结果经常冲突。", "教程建议你先想清楚这一周目最想验证什么，再决定是否为了即时收益打破原计划。", "即时收益", "长期布局", "周目目标")),
            CreateTutorialCategory("其他",
                CreateTutorialEntry("存档与回看", "关键节点前后都值得留一个档。", "很多系统是连锁反应式的，保留关键节点存档会让你更容易验证不同路线。", "关键节点", "分支对比", "路线实验"),
                CreateTutorialEntry("首页入口", "教程、成就、CG 和制作人信息都在首页右上角。", "教程适合新开局前快速复习，成就回看适合复盘当前周目，两个入口会持续补充。", "教程入口", "成就回看", "开局复习"))
        };
    }

    private static ZhongshanDeckCreditsEntry CreateCreditsEntry(string title, string role, string description, params string[] tags)
    {
        return new ZhongshanDeckCreditsEntry
        {
            title = title,
            role = role,
            description = description,
            tags = tags != null ? new List<string>(tags) : new List<string>()
        };
    }

    private static ZhongshanDeckTutorialCategoryData CreateTutorialCategory(string name, params ZhongshanDeckTutorialEntryData[] entries)
    {
        return new ZhongshanDeckTutorialCategoryData
        {
            name = name,
            entries = entries != null ? new List<ZhongshanDeckTutorialEntryData>(entries) : new List<ZhongshanDeckTutorialEntryData>()
        };
    }

    private static ZhongshanDeckTutorialEntryData CreateTutorialEntry(string title, string lead, string description, params string[] highlights)
    {
        return new ZhongshanDeckTutorialEntryData
        {
            title = title,
            lead = lead,
            description = description,
            highlights = highlights != null ? new List<string>(highlights) : new List<string>()
        };
    }

    private static ZhongshanDeckMenuActionLabel CreateMenuItem(string actionId, string label)
    {
        return new ZhongshanDeckMenuActionLabel
        {
            actionId = actionId,
            label = label
        };
    }

    private static ZhongshanDeckIconEntry CreateTopIcon(string actionId, string label, string tooltip)
    {
        return new ZhongshanDeckIconEntry
        {
            actionId = actionId,
            label = label,
            tooltip = tooltip
        };
    }

    private static ZhongshanDeckHomepageLayoutItem CreateHomepageLayoutItem(string key, string displayName, ZhongshanDeckLayoutAnchor anchor, UnityEngine.Vector2 anchoredPosition, UnityEngine.Vector2 size)
    {
        return new ZhongshanDeckHomepageLayoutItem
        {
            key = key,
            displayName = displayName,
            anchor = anchor,
            anchoredPosition = anchoredPosition,
            size = size,
            visible = true
        };
    }

    private static void EnsureLayoutItem(List<ZhongshanDeckHomepageLayoutItem> layoutItems, ZhongshanDeckHomepageLayoutItem fallback)
    {
        for (int i = 0; i < layoutItems.Count; i++)
        {
            ZhongshanDeckHomepageLayoutItem existing = layoutItems[i];
            if (existing != null && string.Equals(existing.key, fallback.key, StringComparison.Ordinal))
            {
                existing.displayName = string.IsNullOrWhiteSpace(existing.displayName) ? fallback.displayName : existing.displayName;
                existing.EnsureInitialized();
                return;
            }
        }

        layoutItems.Add(fallback);
    }
}
