using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

// ========================================================================
//  校园新闻系统 —— 每回合开始时展示校园报纸
//  包含：头条、热搜、树洞、通知、广告
//  支持动态新闻（基于玩家状态）和连载新闻
// ========================================================================

#region 数据模型

public enum NewsType
{
    Headline,   // 头条
    Trending,   // 热搜
    Gossip,     // 树洞
    Notice,     // 通知
    Ad          // 广告
}

[Serializable]
public class NewsItem
{
    public string id;
    public NewsType type;
    public string title;
    public string content;
    public string author;           // 头条用
    public string anonymousId;      // 树洞用
    public int likes;               // 树洞用
    public float hotValue;          // 热搜用（万）
    public string hotTag;           // 热搜标签
    public int targetYear;          // 0=任意
    public int targetSemester;      // 0=任意
    public int targetRound;         // 0=任意
    public string seriesId;         // 连载ID
    public int seriesOrder;         // 连载顺序

    public NewsItem() { }

    public NewsItem(NewsType type, string title, string content)
    {
        this.type = type;
        this.title = title;
        this.content = content;
    }
}

#endregion

// ========================================================================

/// <summary>
/// 校园新闻系统 —— 每回合开始时生成并展示新闻
/// </summary>
public class NewsSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static NewsSystem Instance { get; private set; }

    // ========== 事件 ==========
    public event Action OnNewsDismissed;

    // ========== 状态 ==========
    private bool isShowing = false;
    public bool IsShowing => isShowing;

    // ========== 已显示记录 ==========
    private HashSet<string> shownSeriesIds = new HashSet<string>();

    // ========== 匿名ID池 ==========
    private static readonly string[] AnonymousIds = new string[]
    {
        "匿名柠檬精", "匿名卷王", "匿名摆烂人", "匿名小透明", "匿名干饭人",
        "匿名夜猫子", "匿名早八困难户", "匿名社恐", "匿名社牛", "匿名咸鱼",
        "匿名学霸", "匿名学渣", "匿名月光族", "匿名养生达人", "匿名追星girl",
        "匿名游戏宅", "匿名图书馆钉子户", "匿名食堂测评师", "匿名操场独行侠",
        "匿名快递收割机", "匿名奶茶续命者", "匿名PPT战神", "匿名DDL战士",
        "匿名选课抢手", "匿名占座王", "匿名外卖依赖症", "匿名被窝哲学家"
    };

    // ========== UI 引用 ==========
    private GameObject newsCanvas;

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

    // ========== 公共接口 ==========

    /// <summary>
    /// 显示当前回合的新闻（由 HUDManager/TurnManager 在回合开始时调用）
    /// </summary>
    public void ShowNews()
    {
        if (isShowing) return;
        if (GameState.Instance == null) return;

        int year = GameState.Instance.CurrentYear;
        int semester = GameState.Instance.CurrentSemester;
        int round = GameState.Instance.CurrentRound;

        List<NewsItem> news = GenerateNewsForRound(year, semester, round);
        BuildNewsUI(news, year, semester, round);
        isShowing = true;
    }

    /// <summary>
    /// 关闭新闻界面
    /// </summary>
    public void DismissNews()
    {
        if (newsCanvas != null)
        {
            Destroy(newsCanvas);
            newsCanvas = null;
        }
        isShowing = false;
        OnNewsDismissed?.Invoke();
    }

    // ========== 新闻生成 ==========

    private List<NewsItem> GenerateNewsForRound(int year, int semester, int round)
    {
        List<NewsItem> news = new List<NewsItem>();

        // 1. 头条（固定1条）
        news.Add(GenerateHeadline(year, semester, round));

        // 2. 热搜（2固定 + 1动态 = 3条）
        news.AddRange(GenerateFixedTrending(year, semester, round));
        NewsItem dynamicTrending = GenerateDynamicTrending();
        if (dynamicTrending != null) news.Add(dynamicTrending);

        // 3. 树洞（1~2固定 + 条件触发）
        news.AddRange(GenerateGossip(year, semester, round));

        // 4. 通知（条件触发）
        news.AddRange(GenerateNotices(year, semester, round));

        // 5. 广告（随机0~1条）
        NewsItem ad = GenerateAd(year, semester, round);
        if (ad != null) news.Add(ad);

        // 6. 连载新闻
        news.AddRange(GenerateSeriesNews(year, semester, round));

        return news;
    }

    // ========== 头条生成 ==========

    private NewsItem GenerateHeadline(int year, int semester, int round)
    {
        // 大一上学期有5个回合的固定头条
        if (year == 1 && semester == 1)
        {
            switch (round)
            {
                case 1:
                    return new NewsItem(NewsType.Headline,
                        "热烈欢迎2024级新同学！今日南农迎来3000余名新生",
                        "钟山脚下又添新面孔。今年的新生中，最远的来自新疆，最近的就住在学校对面小区。校长在开学典礼上表示：\"希望同学们珍惜四年时光，不要等到毕业才后悔。\"")
                    { author = "南农青年报记者 小明" };
                case 2:
                    return new NewsItem(NewsType.Headline,
                        "国庆七天乐！但你的选课选好了吗？",
                        "据统计，本届新生中有47%的同学在国庆假期前一天才完成选课。教务处温馨提醒：热门课程先到先得，\"水课\"也是有灵魂的。")
                    { author = "南农青年报记者 小红" };
                case 3:
                    return new NewsItem(NewsType.Headline,
                        "期中考试来袭！图书馆座位一位难求",
                        "随着期中考试周临近，图书馆日均入馆人次突破历史新高。心理咨询中心提醒：合理安排学习时间，不要给自己太大压力。")
                    { author = "南农青年报记者 小华" };
                case 4:
                    return new NewsItem(NewsType.Headline,
                        "期末季 + 四六级！南农学子迎来最忙十二月",
                        "十二月的南农校园弥漫着焦虑与奶茶的气息。CET-4考试将于本月举行，而期末考试也近在咫尺。后勤处贴心提醒：天冷了记得加衣服。")
                    { author = "南农青年报记者 小李" };
                case 5:
                    return new NewsItem(NewsType.Headline,
                        "大一上学期即将落幕！你的第一张成绩单准备好了吗？",
                        "一学期转瞬即逝。回望这几个月，有人收获了友情和成长，有人在迷茫中摸索方向。辅导员提醒：回家注意安全，别忘了给家人带一只南农烧鸡。")
                    { author = "南农青年报记者 小刘" };
            }
        }

        // 其他学期的通用头条
        return GenerateGenericHeadline(year, semester, round);
    }

    private NewsItem GenerateGenericHeadline(int year, int semester, int round)
    {
        string yearName = year switch
        {
            1 => "大一", 2 => "大二", 3 => "大三", 4 => "大四", _ => $"第{year}年"
        };
        string semName = semester == 1 ? "上学期" : "下学期";

        // 按回合位置生成不同主题
        if (round == 1)
        {
            return new NewsItem(NewsType.Headline,
                $"{yearName}{semName}开学啦！新学期新气象",
                $"又是一个新学期的开始，{yearName}的同学们已经是\"老生\"了。教务处提醒：别忘了确认课表，新学期一起加油！")
            { author = "南农青年报" };
        }
        else if (round == 3)
        {
            return new NewsItem(NewsType.Headline,
                $"期中考试周来临，你准备好了吗？",
                $"{yearName}{semName}期中考试即将开始，图书馆再次人满为患。加油，少年！")
            { author = "南农青年报" };
        }
        else if (round == 5)
        {
            return new NewsItem(NewsType.Headline,
                $"{yearName}{semName}即将结束，期末冲刺！",
                $"本学期进入尾声，期末考试在即。回顾这个学期，你收获了什么？")
            { author = "南农青年报" };
        }
        else
        {
            // 中间回合的随机头条
            string[] midHeadlines = new string[]
            {
                "校园文化节精彩纷呈，各社团大显身手",
                "图书馆新增自习位500个，再也不用抢座了",
                "食堂推出新菜品，同学们排队品尝",
                "校园绿化升级，钟山脚下更添一抹新绿",
                "校运动会报名启动，各学院积极备战"
            };
            int idx = (year * 10 + semester * 5 + round) % midHeadlines.Length;
            return new NewsItem(NewsType.Headline, midHeadlines[idx],
                "校园生活丰富多彩，每一天都有新的故事在发生。")
            { author = "南农青年报" };
        }
    }

    // ========== 固定热搜 ==========

    private List<NewsItem> GenerateFixedTrending(int year, int semester, int round)
    {
        var trending = new List<NewsItem>();

        // 大一上学期有特定固定热搜
        if (year == 1 && semester == 1)
        {
            switch (round)
            {
                case 1:
                    trending.Add(new NewsItem(NewsType.Trending, "", "#大一新生军训第一天# \"教官好帅但太阳好毒\"") { hotValue = 98.2f, hotTag = "新" });
                    trending.Add(new NewsItem(NewsType.Trending, "", "#宿舍分配结果# \"室友打呼噜怎么办在线等急\"") { hotValue = 76.5f, hotTag = "新" });
                    return trending;
                case 2:
                    trending.Add(new NewsItem(NewsType.Trending, "", "#选课大战# \"全校都在抢那门《影视鉴赏》\"") { hotValue = 156.7f, hotTag = "热" });
                    trending.Add(new NewsItem(NewsType.Trending, "", "#国庆回家还是留校# \"我妈说想我了，行吧\"") { hotValue = 89.3f, hotTag = "新" });
                    return trending;
                case 3:
                    trending.Add(new NewsItem(NewsType.Trending, "", "#期中考试倒计时# \"谁的高数还没开始复习\"") { hotValue = 203.1f, hotTag = "爆" });
                    trending.Add(new NewsItem(NewsType.Trending, "", "#班委竞选名场面# \"竞选宣言比脱口秀还好笑\"") { hotValue = 91.7f, hotTag = "热" });
                    return trending;
                case 4:
                    trending.Add(new NewsItem(NewsType.Trending, "", "#四级考试祈福贴# \"转发这条锦鲤保过四级\"") { hotValue = 312.5f, hotTag = "爆" });
                    trending.Add(new NewsItem(NewsType.Trending, "", "#期末突击学习法# \"一个通宵顶一个学期？\"") { hotValue = 187.9f, hotTag = "热" });
                    return trending;
                case 5:
                    trending.Add(new NewsItem(NewsType.Trending, "", "#期末考试出分了# \"GPA多少不重要，活着就好\"") { hotValue = 445.2f, hotTag = "爆" });
                    trending.Add(new NewsItem(NewsType.Trending, "", "#寒假计划大赏# \"从减肥到学车到躺平三步曲\"") { hotValue = 178.6f, hotTag = "热" });
                    return trending;
            }
        }

        // 通用热搜
        string[][] genericTrending = new string[][]
        {
            new[] { "#学期开始了# \"新学期flag立起来\"", "88.5" },
            new[] { "#食堂排队日常# \"今天又是在食堂浪费生命的一天\"", "65.3" },
            new[] { "#图书馆占座大战# \"我的位置被人抢了！\"", "72.1" },
            new[] { "#校园跑打卡# \"今天你跑了吗\"", "45.6" },
            new[] { "#考试周倒计时# \"求抱佛脚攻略\"", "134.8" },
            new[] { "#宿舍夜谈# \"聊到凌晨三点的都是什么话题\"", "56.9" },
        };

        int seed = year * 100 + semester * 10 + round;
        System.Random rng = new System.Random(seed);
        int i1 = rng.Next(genericTrending.Length);
        int i2 = (i1 + 1 + rng.Next(genericTrending.Length - 1)) % genericTrending.Length;

        trending.Add(new NewsItem(NewsType.Trending, "", genericTrending[i1][0]) { hotValue = float.Parse(genericTrending[i1][1]), hotTag = "热" });
        trending.Add(new NewsItem(NewsType.Trending, "", genericTrending[i2][0]) { hotValue = float.Parse(genericTrending[i2][1]), hotTag = "新" });

        return trending;
    }

    // ========== 动态热搜 ==========

    private NewsItem GenerateDynamicTrending()
    {
        if (PlayerAttributes.Instance == null) return null;

        int study = PlayerAttributes.Instance.Study;
        int charm = PlayerAttributes.Instance.Charm;
        int physique = PlayerAttributes.Instance.Physique;
        int stress = PlayerAttributes.Instance.Stress;
        int mood = PlayerAttributes.Instance.Mood;
        int guilt = PlayerAttributes.Instance.Guilt;
        int money = GameState.Instance != null ? GameState.Instance.Money : 0;

        // 按优先级检查条件
        if (mood <= 30)
            return new NewsItem(NewsType.Trending, "", "#关注大学生情绪# \"如果你不开心，请看这篇\"") { hotValue = 61f, hotTag = "热" };
        if (stress >= 70)
            return new NewsItem(NewsType.Trending, "", "#大学生心理健康# \"你最近还好吗？\"") { hotValue = 55f, hotTag = "热" };
        if (guilt >= 50)
            return new NewsItem(NewsType.Trending, "", "#校园不良风气# \"听说有人在做一些不太正当的事\"") { hotValue = 41f, hotTag = "新" };

        // NPC好感度检查
        if (AffinitySystem.Instance != null && NPCDatabase.Instance != null)
        {
            var allNPCs = NPCDatabase.Instance.GetAllNPCIds();
            foreach (string npcId in allNPCs)
            {
                int affinity = AffinitySystem.Instance.GetAffinity(npcId);
                if (affinity >= 60)
                {
                    string npcName = NPCDatabase.Instance.GetNPCName(npcId);
                    return new NewsItem(NewsType.Trending, "", $"#校园CP预警# \"{npcName}和某同学走得好近啊\"") { hotValue = 95f, hotTag = "爆" };
                }
            }
        }

        if (study >= 150)
            return new NewsItem(NewsType.Trending, "", "#南农学霸养成记# \"有人绩点已经快满了？\"") { hotValue = 67f, hotTag = "热" };
        if (charm >= 120)
            return new NewsItem(NewsType.Trending, "", "#校园风云人物# \"有个大一新生好像很受欢迎\"") { hotValue = 82f, hotTag = "热" };
        if (physique >= 130)
            return new NewsItem(NewsType.Trending, "", "#晨跑达人# \"每天操场都能看到同一个人在跑步\"") { hotValue = 38f, hotTag = "新" };
        if (money <= 500 && money >= 0)
            return new NewsItem(NewsType.Trending, "", "#月底吃土人# \"离发生活费还有X天，撑住\"") { hotValue = 73f, hotTag = "热" };
        if (money >= 5000)
            return new NewsItem(NewsType.Trending, "", "#南农土豪# \"有人的生活费是我的三倍？？\"") { hotValue = 48f, hotTag = "新" };

        // 社团相关
        if (ClubSystem.Instance != null && ClubSystem.Instance.GetJoinedClubIds().Count > 0)
        {
            return new NewsItem(NewsType.Trending, "", "#社团招新战报# \"XX社团今年新人好活跃\"") { hotValue = 33f, hotTag = "新" };
        }

        // 默认
        return new NewsItem(NewsType.Trending, "", "#校园日常# \"今天也是平平无奇的一天\"") { hotValue = 25f, hotTag = "新" };
    }

    // ========== 树洞 ==========

    private List<NewsItem> GenerateGossip(int year, int semester, int round)
    {
        var gossips = new List<NewsItem>();
        System.Random rng = new System.Random(year * 1000 + semester * 100 + round * 10 + DateTime.Now.Second);

        // 固定树洞池
        string[][] gossipPool = new string[][]
        {
            new[] { "刚到学校就有学长学姐来宿舍推销英语课，我都不好意思拒绝……感觉钱包在哭泣", "287" },
            new[] { "军训教官说站军姿不许动，然后一只蚊子在我脸上开了个party", "534" },
            new[] { "食堂阿姨今天心情好，给我打了超多饭！感觉可以吃三天！", "189" },
            new[] { "刚才在校门口看到有人拎着一整只南农烧鸡进来的，那个香味我追了三条街", "423" },
            new[] { "选课系统崩了三次，我选的课全被抢光了，现在课表上全是8点的课", "612" },
            new[] { "班会上辅导员让每个人做自我介绍，我紧张到把自己名字说错了……社死现场", "445" },
            new[] { "我爸打电话问我以后打算考研还是考公，我说我打算先活过期中考试", "567" },
            new[] { "今天在食堂吃到了黄教授烧饼，一口下去仿佛看见了满分的高数卷子（错觉）", "423" },
            new[] { "四级还剩两周，我的词汇量还停留在abandon。每次翻开单词书，命运都在暗示我放弃", "723" },
            new[] { "今天在图书馆看到一个人同时铺开了五本书，像是在摆阵法，期末结界！", "534" },
            new[] { "出分了。不想说话。GPA和我的心情一样低迷", "634" },
            new[] { "一学期结束了，最大的收获是认识了一群好室友，虽然他们打呼噜但我爱他们", "876" },
            new[] { "有没有人知道图书馆三楼那个总是占座的学长是谁？每天都比我早到太卷了吧", "276" },
            new[] { "室友昨晚做梦喊'选A！选A！'，我们都惊醒了，他说梦见在考四级", "445" },
            new[] { "寒假flag：1.减肥 2.学车 3.看完10本书。（和上学期开学flag一模一样）", "567" },
        };

        // 随机抽取1~2条固定树洞
        int count = rng.Next(1, 3);
        List<int> used = new List<int>();
        for (int i = 0; i < count && i < gossipPool.Length; i++)
        {
            int idx;
            do { idx = rng.Next(gossipPool.Length); } while (used.Contains(idx));
            used.Add(idx);

            string anonId = AnonymousIds[rng.Next(AnonymousIds.Length)];
            gossips.Add(new NewsItem(NewsType.Gossip, "", gossipPool[idx][0])
            {
                anonymousId = anonId,
                likes = int.Parse(gossipPool[idx][1])
            });
        }

        // 条件触发树洞
        if (PlayerAttributes.Instance != null)
        {
            int money = GameState.Instance != null ? GameState.Instance.Money : 0;

            if (money < 0)
            {
                gossips.Add(new NewsItem(NewsType.Gossip, "", "听说有人余额负数了？大学生真的要学会理财啊")
                { anonymousId = AnonymousIds[rng.Next(AnonymousIds.Length)], likes = 289 });
            }
        }

        return gossips;
    }

    // ========== 通知 ==========

    private List<NewsItem> GenerateNotices(int year, int semester, int round)
    {
        var notices = new List<NewsItem>();

        if (round == 1)
        {
            notices.Add(new NewsItem(NewsType.Notice, "【教务处】",
                $"新学期课程表已发布，请同学们及时查看并做好上课准备。"));
        }

        if (round == 3)
        {
            notices.Add(new NewsItem(NewsType.Notice, "【学工处】",
                "期中学习情况摸底将于本月进行，请各位同学认真对待。"));
        }

        if (round == 5)
        {
            notices.Add(new NewsItem(NewsType.Notice, "【学工处】",
                "本学期成绩已发布，请同学们登录教务系统查询。对成绩有疑问的同学可在规定时间内申请复查。"));
        }

        // 条件通知
        if (PlayerAttributes.Instance != null && PlayerAttributes.Instance.Mood < 40)
        {
            notices.Add(new NewsItem(NewsType.Notice, "【心理咨询中心】",
                "若感到焦虑或情绪低落，欢迎预约心理咨询（工作日9:00-17:00），你不是一个人在战斗。"));
        }

        if (PlayerAttributes.Instance != null && PlayerAttributes.Instance.Stress >= 80)
        {
            notices.Add(new NewsItem(NewsType.Notice, "【辅导员提醒】",
                "注意劳逸结合，适当运动放松，有困难随时找老师聊聊。"));
        }

        return notices;
    }

    // ========== 广告 ==========

    private NewsItem GenerateAd(int year, int semester, int round)
    {
        // 50%概率出现广告
        if (UnityEngine.Random.value > 0.5f) return null;

        string[] ads = new string[]
        {
            "校门口文具店开学大促！买满50送南农定制笔记本！——学长温馨提示：笔记本质量一般但胜在情怀",
            "学长的CET-4真题解析笔记，手写版限量30份！先到先得！——绝对不是去年没过的学长写的（大概）",
            "校门口奶茶店新品上市！\"学霸特调\"——据说喝了能多背50个单词（效果因人而异）",
            "二手教材交易群：物美价廉！买到就是赚到！——备注：书上的笔记是附赠的，不是瑕疵",
            "校内驾校招生中！大学生专属优惠价！——学不会包退（退的是学费不是驾照）",
        };

        int idx = (year * 10 + semester * 5 + round) % ads.Length;
        return new NewsItem(NewsType.Ad, "推广", ads[idx]);
    }

    // ========== 连载新闻 ==========

    private List<NewsItem> GenerateSeriesNews(int year, int semester, int round)
    {
        var series = new List<NewsItem>();

        // 食堂之王争霸赛（大一上学期连载）
        if (year == 1 && semester == 1)
        {
            string[][] foodSeries = new string[][]
            {
                new[] { "食堂窗口人气排行出炉！", "经过一周非正式统计，一食堂3号窗口以日均排队47人的成绩荣登榜首。其秘密武器：阿姨手不抖。" },
                new[] { "二食堂反击战！新菜单曝光", "二食堂推出\"南农特色套餐\"，据说灵感来源于黄教授烧饼。一食堂表示不服。" },
                new[] { "食堂大战白热化：出现了神秘黑马", "三食堂角落一个不起眼的小窗口突然爆火，原因是老板会记住每个同学的口味。" },
                new[] { "食堂投票结果揭晓", "经过全校投票，\"最佳食堂\"称号授予……全部三个食堂。因为评委老师说\"手心手背都是肉\"。" },
                new[] { "食堂故事完结篇", "学期结束了，大家最想念的还是食堂阿姨那句\"够不够？再给你加点\"。" },
            };

            if (round >= 1 && round <= 5)
            {
                series.Add(new NewsItem(NewsType.Gossip,
                    foodSeries[round - 1][0], foodSeries[round - 1][1])
                {
                    anonymousId = "南农美食家",
                    likes = 200 + round * 50,
                    seriesId = "food_war",
                    seriesOrder = round
                });
            }
        }

        // 校园猫咪观察日记（大一下学期连载）
        if (year == 1 && semester == 2)
        {
            string[][] catSeries = new string[][]
            {
                new[] { "校园新来了一只橘猫", "教学楼旁出现一只橘猫，已被三个学院的同学分别起了不同的名字。猫：你们商量好了再叫我。" },
                new[] { "橘猫有名字了：绩点", "经全校投票，校园橘猫正式命名为\"绩点\"。取名者表示：\"希望我的绩点能像它一样圆润饱满\"。" },
                new[] { "绩点（猫）期中考试成绩公布", "绩点同学本月在五个教室旁听了课程，出勤率比某些同学还高。辅导员考虑给它发奖学金。" },
                new[] { "绩点（猫）的圣诞节", "有同学给绩点织了一条小围巾，绩点表示——叼着围巾跑了。" },
                new[] { "绩点（猫）的寒假安排", "大家都回家了，但绩点留校了。后勤叔叔表示会照顾好它。下学期见，绩点！" },
            };

            if (round >= 1 && round <= 5)
            {
                series.Add(new NewsItem(NewsType.Gossip,
                    catSeries[round - 1][0], catSeries[round - 1][1])
                {
                    anonymousId = "绩点观察员",
                    likes = 300 + round * 80,
                    seriesId = "campus_cat",
                    seriesOrder = round
                });
            }
        }

        return series;
    }

    // ========== UI 构建 ==========

    private void BuildNewsUI(List<NewsItem> news, int year, int semester, int round)
    {
        if (newsCanvas != null) Destroy(newsCanvas);

        // Canvas
        newsCanvas = new GameObject("NewsCanvas");
        Canvas canvas = newsCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        newsCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        newsCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        newsCanvas.AddComponent<GraphicRaycaster>();

        // 半透明背景遮罩
        GameObject overlay = CreateUIElement("Overlay", newsCanvas.transform);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);

        // 报纸面板（居中）
        GameObject paper = CreateUIElement("Paper", overlay.transform);
        RectTransform paperRT = paper.GetComponent<RectTransform>();
        paperRT.anchorMin = new Vector2(0.1f, 0.05f);
        paperRT.anchorMax = new Vector2(0.9f, 0.95f);
        paperRT.sizeDelta = Vector2.zero;
        Image paperImg = paper.AddComponent<Image>();
        paperImg.color = new Color(0.98f, 0.95f, 0.87f, 1f); // 米黄色纸张

        // 报头
        string yearName = year switch { 1 => "大一", 2 => "大二", 3 => "大三", 4 => "大四", _ => $"第{year}年" };
        string semName = semester == 1 ? "上学期" : "下学期";
        int issueNum = (year - 1) * 10 + (semester - 1) * 5 + round;

        GameObject header = CreateUIElement("Header", paper.transform);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 0.9f);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.sizeDelta = Vector2.zero;
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.6f, 0.15f, 0.15f, 1f); // 红色报头

        TextMeshProUGUI headerText = CreateTMP(header.transform, "HeaderText");
        headerText.text = $"南农青年报  第{issueNum}期\n{yearName}{semName} 第{round}回合";
        headerText.fontSize = 28;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAlignmentOptions.Center;
        RectTransform headerTextRT = headerText.GetComponent<RectTransform>();
        headerTextRT.anchorMin = Vector2.zero;
        headerTextRT.anchorMax = Vector2.one;
        headerTextRT.sizeDelta = Vector2.zero;

        // 内容区域（ScrollView）
        GameObject scrollArea = CreateUIElement("ScrollArea", paper.transform);
        RectTransform scrollRT = scrollArea.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.08f);
        scrollRT.anchorMax = new Vector2(1, 0.9f);
        scrollRT.sizeDelta = Vector2.zero;

        ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        Image scrollBg = scrollArea.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0f); // 透明

        // Viewport
        GameObject viewport = CreateUIElement("Viewport", scrollArea.transform);
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 15, 15);
        vlg.spacing = 12;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = vpRT;

        // 填充新闻内容
        foreach (var item in news)
        {
            switch (item.type)
            {
                case NewsType.Headline:
                    BuildHeadlineItem(content.transform, item);
                    break;
                case NewsType.Trending:
                    BuildTrendingItem(content.transform, item);
                    break;
                case NewsType.Gossip:
                    BuildGossipItem(content.transform, item);
                    break;
                case NewsType.Notice:
                    BuildNoticeItem(content.transform, item);
                    break;
                case NewsType.Ad:
                    BuildAdItem(content.transform, item);
                    break;
            }
        }

        // 底部按钮 "开始新的一天"
        GameObject btnArea = CreateUIElement("ButtonArea", paper.transform);
        RectTransform btnAreaRT = btnArea.GetComponent<RectTransform>();
        btnAreaRT.anchorMin = new Vector2(0.3f, 0.01f);
        btnAreaRT.anchorMax = new Vector2(0.7f, 0.07f);
        btnAreaRT.sizeDelta = Vector2.zero;

        Image btnBg = btnArea.AddComponent<Image>();
        btnBg.color = new Color(0.85f, 0.55f, 0.1f, 1f); // 金橙色

        Button btn = btnArea.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(DismissNews);

        TextMeshProUGUI btnText = CreateTMP(btnArea.transform, "BtnText");
        btnText.text = "开始新的一天";
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        RectTransform btnTextRT = btnText.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
    }

    // ========== 新闻条目UI ==========

    private void BuildHeadlineItem(Transform parent, NewsItem item)
    {
        // 头条区域
        GameObject container = CreateNewsContainer(parent, new Color(1f, 0.97f, 0.93f, 1f));

        // 标签
        TextMeshProUGUI label = CreateTMP(container.transform, "Label");
        label.text = "头条";
        label.fontSize = 16;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.8f, 0.2f, 0.2f);
        LayoutElement labelLE = label.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredHeight = 24;

        // 标题
        TextMeshProUGUI title = CreateTMP(container.transform, "Title");
        title.text = item.title;
        title.fontSize = 22;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.15f, 0.15f, 0.15f);
        LayoutElement titleLE = title.gameObject.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 30;

        // 正文
        TextMeshProUGUI body = CreateTMP(container.transform, "Body");
        body.text = item.content;
        body.fontSize = 16;
        body.color = new Color(0.3f, 0.3f, 0.3f);
        LayoutElement bodyLE = body.gameObject.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = 60;

        // 作者
        if (!string.IsNullOrEmpty(item.author))
        {
            TextMeshProUGUI author = CreateTMP(container.transform, "Author");
            author.text = $"—— {item.author}";
            author.fontSize = 14;
            author.fontStyle = FontStyles.Italic;
            author.color = new Color(0.5f, 0.5f, 0.5f);
            author.alignment = TextAlignmentOptions.Right;
            LayoutElement authorLE = author.gameObject.AddComponent<LayoutElement>();
            authorLE.preferredHeight = 20;
        }
    }

    private void BuildTrendingItem(Transform parent, NewsItem item)
    {
        GameObject container = CreateUIElement("Trending", parent);
        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(20, 20, 4, 4);
        LayoutElement containerLE = container.AddComponent<LayoutElement>();
        containerLE.preferredHeight = 30;

        // 热搜标签
        if (!string.IsNullOrEmpty(item.hotTag))
        {
            TextMeshProUGUI tag = CreateTMP(container.transform, "Tag");
            tag.text = item.hotTag;
            tag.fontSize = 14;
            tag.fontStyle = FontStyles.Bold;
            tag.color = item.hotTag == "爆" ? new Color(1f, 0.3f, 0.1f) : new Color(1f, 0.5f, 0f);
            LayoutElement tagLE = tag.gameObject.AddComponent<LayoutElement>();
            tagLE.preferredWidth = 30;
        }

        // 内容
        TextMeshProUGUI content = CreateTMP(container.transform, "Content");
        content.text = item.content;
        content.fontSize = 16;
        content.color = new Color(0.2f, 0.2f, 0.2f);
        LayoutElement contentLE = content.gameObject.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1;

        // 热度值
        TextMeshProUGUI hot = CreateTMP(container.transform, "Hot");
        hot.text = $"{item.hotValue:F1}万";
        hot.fontSize = 14;
        hot.color = new Color(0.6f, 0.6f, 0.6f);
        hot.alignment = TextAlignmentOptions.Right;
        LayoutElement hotLE = hot.gameObject.AddComponent<LayoutElement>();
        hotLE.preferredWidth = 60;
    }

    private void BuildGossipItem(Transform parent, NewsItem item)
    {
        GameObject container = CreateNewsContainer(parent, new Color(0.95f, 0.95f, 0.98f, 1f));

        // 连载标签
        if (!string.IsNullOrEmpty(item.seriesId))
        {
            TextMeshProUGUI seriesLabel = CreateTMP(container.transform, "SeriesLabel");
            seriesLabel.text = $"[连载·{item.title}]";
            seriesLabel.fontSize = 14;
            seriesLabel.fontStyle = FontStyles.Bold;
            seriesLabel.color = new Color(0.4f, 0.2f, 0.6f);
            LayoutElement slLE = seriesLabel.gameObject.AddComponent<LayoutElement>();
            slLE.preferredHeight = 20;
        }

        // 匿名ID + 内容
        string displayId = !string.IsNullOrEmpty(item.anonymousId) ? item.anonymousId : AnonymousIds[UnityEngine.Random.Range(0, AnonymousIds.Length)];
        TextMeshProUGUI gossipText = CreateTMP(container.transform, "GossipText");
        gossipText.text = $"<b>{displayId}：</b>{(string.IsNullOrEmpty(item.seriesId) ? item.content : item.content)}";
        gossipText.fontSize = 16;
        gossipText.color = new Color(0.25f, 0.25f, 0.3f);
        LayoutElement gtLE = gossipText.gameObject.AddComponent<LayoutElement>();
        gtLE.preferredHeight = 40;

        // 点赞数
        TextMeshProUGUI likes = CreateTMP(container.transform, "Likes");
        likes.text = $"[{item.likes}赞]";
        likes.fontSize = 14;
        likes.color = new Color(0.6f, 0.6f, 0.6f);
        likes.alignment = TextAlignmentOptions.Right;
        LayoutElement likesLE = likes.gameObject.AddComponent<LayoutElement>();
        likesLE.preferredHeight = 20;
    }

    private void BuildNoticeItem(Transform parent, NewsItem item)
    {
        GameObject container = CreateNewsContainer(parent, new Color(1f, 0.95f, 0.95f, 1f));

        TextMeshProUGUI noticeText = CreateTMP(container.transform, "NoticeText");
        noticeText.text = $"<color=#CC0000><b>{item.title}</b></color> {item.content}";
        noticeText.fontSize = 15;
        noticeText.color = new Color(0.3f, 0.2f, 0.2f);
        LayoutElement ntLE = noticeText.gameObject.AddComponent<LayoutElement>();
        ntLE.preferredHeight = 40;
    }

    private void BuildAdItem(Transform parent, NewsItem item)
    {
        GameObject container = CreateNewsContainer(parent, new Color(0.92f, 0.92f, 0.92f, 1f));

        TextMeshProUGUI adLabel = CreateTMP(container.transform, "AdLabel");
        adLabel.text = "推广";
        adLabel.fontSize = 12;
        adLabel.color = new Color(0.6f, 0.6f, 0.6f);
        LayoutElement alLE = adLabel.gameObject.AddComponent<LayoutElement>();
        alLE.preferredHeight = 18;

        TextMeshProUGUI adText = CreateTMP(container.transform, "AdText");
        adText.text = item.content;
        adText.fontSize = 14;
        adText.color = new Color(0.4f, 0.4f, 0.4f);
        LayoutElement atLE = adText.gameObject.AddComponent<LayoutElement>();
        atLE.preferredHeight = 35;
    }

    // ========== UI 辅助 ==========

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private GameObject CreateNewsContainer(Transform parent, Color bgColor)
    {
        GameObject container = CreateUIElement("NewsContainer", parent);
        Image bg = container.AddComponent<Image>();
        bg.color = bgColor;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 8, 8);
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return container;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }
}
