# Project Memory - 钟山下 (Unity)

## MCP Server (Unity MCP)
启动命令:
```
C:\Users\lenovo\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0\LocalCache\local-packages\Python313\Scripts\uvx.exe --prerelease explicit --from "mcpforunityserver>=0.0.0a0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080
```

MCP 协议: Streamable HTTP, endpoint `http://localhost:8080/mcp`
- 初始化: POST `/mcp` with `method: "initialize"`
- 获取 session ID: 从 response header `mcp-session-id` 读取
- 后续请求: 带上 `mcp-session-id` header

## 项目结构
- Unity 2022.x 项目
- 场景加载顺序: SplashScreen -> LoadingScreen -> TitleScreen -> GameScene
- Ctrl+P 始终从 SplashScreen 开始 (已配置 PlayModeStartScene)
- 所有 UI 通过代码动态创建 (TitleScreenManager.cs)

## Canvas 层级规范 (sortingOrder)
- 50: 任务追踪UI (MissionTrackerCanvas) - 右侧悬浮任务列表
- 100: HUD 主界面 (HUDCanvas)
- 150: 任务通知弹窗 (MissionNotificationCanvas) - 右上角滑入通知
- 150: 成就弹窗通知 (AchievementUI)
- 180: 信息面板 (InfoPanelCanvas) - 个人信息/人际关系/任务
- 200: 对话系统 (DialogueCanvas)
- 200: 商店面板 (ShopCanvas)
- 200: 社团面板 (ClubPanelCanvas)
- 200: 任务面板 (MissionPanelCanvas) - 查看所有任务
- 200: 结局展示 (EndingUI)
- 200: 调试控制台 (DebugConsole)
- 250: 游戏内设置面板 (SettingsCanvas)
- 300: 成就回顾面板 (AchievementUI Review)
- 300: 标题界面设置面板 (SettingsCanvas)
- 500: 考试UI (ExamUICanvas)

**原则**: 数值越大越靠前，弹窗/对话类UI使用200+，考试等强制交互使用500

## 关键脚本
- `Assets/Scripts/TitleScreenManager.cs` - 标题界面管理器 (视频背景+菜单)
- `Assets/Scripts/RippleEffect.cs` - 水涟漪特效 (点击触发)
- `Assets/Scripts/FontManager.cs` - TMP 字体全局管理 (中文字体)
- `Assets/Scripts/SceneLoader.cs` - 场景加载器
- `Assets/Scripts/DialogueSystem.cs` - 对话引擎 (数据驱动+状态机+分支选项+效果应用+NPCEventHub订阅+IDialogueTrigger实现)
- `Assets/Scripts/DialogueData.cs` - 对话数据模型 (DialogueNode/Choice/Effect)
- `Assets/Scripts/DialogueParser.cs` - JSON加载+条件表达式解析
- `Assets/Scripts/DialogueUIBuilder.cs` - 对话UI构建器 (面板+选项按钮)
- `Assets/Scripts/NPCController.cs` - NPC 控制器 (绑定NPCData, 互动优先级: 菜单→EventHub→dialogueId→旧模式)
- `Assets/Scripts/NPCData.cs` - NPC 数据模型 (NPCData/NPCRelationshipData/SocialActionDefinition/枚举/IRelationshipExtension)
- `Assets/Scripts/NPCDatabase.cs` - NPC 数据库 (JSON加载, 查询NPC/社交行动/日程地点)
- `Assets/Scripts/NPCManager.cs` - NPC 管理器 (按时间段创建/刷新可见NPC, 管理NPCController生命周期)
- `Assets/Scripts/AffinitySystem.cs` - 好感度系统 (公式: base×charm×personality×repeatDecay, 6级等级, 自然衰减)
- `Assets/Scripts/NPCEventHub.cs` - NPC 事件中枢 (解耦NPC互动与对话系统, DialogueRequest事件)
- `Assets/Scripts/NPCInteractionMenu.cs` - NPC 互动菜单 (NPC列表+好感度条+社交行动按钮+恋爱按钮, 纯代码UI)
- `Assets/Scripts/RomanceData.cs` - 恋爱数据模型 (RomanceState/RomanceEndingTier/BreakupReason/RomanceRecord)
- `Assets/Scripts/RomanceSystem.cs` - 恋爱系统核心 (状态机/健康度/回合结算/分手/结局)
- `Assets/Scripts/ConfessionSystem.cs` - 告白系统 (成功率公式/执行/复合/对话触发)
- `Assets/Scripts/RomanceBridge.cs` - 恋爱桥接器 (IRelationshipExtension实现, AffinitySystem<->RomanceSystem同步)
- `Assets/Scripts/ActionSystem.cs` - 行动系统 (12种行动定义+执行+效果应用, isGlobal标记全局行动, moneyCost走EconomyManager, 自习2AP学力+7, 校园跑1AP体魄+1/压力-3/心情+2, 出校门2AP全局行动, 睡觉1AP压力-5结束回合, 新增背单词1AP)
- `Assets/Scripts/LocationData.cs` - 地点数据模型 (LocationId枚举8地点, LocationDefinition, LocationLink邻接关系)
- `Assets/Scripts/LocationManager.cs` - 地点管理器单例 (8地点定义+邻接图, 导航MoveTo免费不消耗AP, GetAvailableActions地点过滤+全局行动, NPC分布)
- `Assets/Scripts/CampusMapUI.cs` - 校园地图UI (纯C#类, 节点图+连接线, 当前地点高亮, NPC指示, 点击打开详情面板)
- `Assets/Scripts/LocationDetailPanel.cs` - 地点详情浮动面板 (纯C#类, 行动预览+NPC列表+移动消耗+前往/取消按钮)
- `Assets/Scripts/TurnManager.cs` - 回合管理器 (回合/学期/学年推进, 含经济结算+考试拦截+补考检查+事件调度挂钩)
- `Assets/Scripts/GameState.cs` - 游戏状态 (行动点/回合/学期/学年/金钱/当前地点, Money允许负数, CurrentLocation属性+OnLocationChanged事件, DefaultActionPoints=20, MaxRoundsPerSemester=5)
- `Assets/Scripts/PlayerAttributes.cs` - 玩家属性 (学力/魅力/体魄/领导力/压力/心情/黑暗值/负罪感/幸运)
- `Assets/Scripts/EconomyManager.cs` - 经济管理器 (Earn/Spend/交易流水/回合结算/学期学费)
- `Assets/Scripts/DebtSystem.cs` - 债务系统 (4级阈值: 200/0/-2000/-5000, 透支惩罚压力+10)
- `Assets/Scripts/ShopSystem.cs` - 商店系统 (17种商品, 6分类, 债务限购, 通过EconomyManager交易)
- `Assets/Scripts/ShopUIBuilder.cs` - 商店UI构建器 (独立Canvas, 分类+商品列表+交易弹窗, 纯代码)
- `Assets/Scripts/InfoPanelBuilder.cs` - 信息面板UI构建器 (独立Canvas sortingOrder=180, 三个子面板: 个人信息/人际关系/任务, 顶部导航栏切换)
- `Assets/Scripts/InfoPanelManager.cs` - 信息面板管理器 (数据绑定+事件订阅+实时刷新, 单例模式)
- `Assets/Scripts/HUDManager.cs` - HUD管理器 (数据绑定+动态底栏按钮+地图UI集成+透支变红+商店按钮+社团按钮+信息按钮+债务订阅+社团回合结算+事件期间按钮锁定)
- `Assets/Scripts/JobSelectionUI.cs` - 兼职实习UI (独立Canvas sortingOrder=200, 实习/副业选项)
- `Assets/Scripts/PhysicalTestUI.cs` - 体侧测试UI (独立Canvas sortingOrder=500)
- `Assets/Scripts/ConfirmDialogUI.cs` - 确认弹窗UI (独立Canvas sortingOrder=600)
- `Assets/Scripts/ShopUIBuilder.cs` - 商店UI构建器 (独立Canvas, 分类+商品列表+交易弹窗, 纯代码)
- `Assets/Scripts/HUDManager.cs` - HUD管理器 (数据绑定+动态底栏按钮+地图UI集成+透支变红+商店按钮+社团按钮+债务订阅+社团回合结算+事件期间按钮锁定)
- `Assets/Scripts/HUDBuilder.cs` - HUD构建器 (纯代码UI构建, 含NPCInteractionMenu+商店+社团面板+警告图标)
- `Assets/Scripts/PlayerController.cs` - 玩家控制器 (移动/跳跃/动画, 对话和事件期间锁定移动)
- `Assets/Scripts/GameSceneInitializer.cs` - 游戏场景初始化器

## 设置系统 (SettingsSystem)
- `Assets/Scripts/SettingsData.cs` - 设置数据模型 (音频/显示/游戏性配置, 序列化到PlayerPrefs)
- `Assets/Scripts/SettingsManager.cs` - 设置管理器单例 (保存/加载/应用设置, 事件通知, F1快捷键)
- `Assets/Scripts/SettingsUIBuilder.cs` - 设置UI构建器 (纯代码, Canvas sortingOrder=250/300, 音量/分辨率/全屏/UI缩放/文本速度)
- 入口: 标题界面"设置"按钮, 游戏内F1键, 暂停菜单"设置"项
- 持久化: PlayerPrefs (Settings_* 键名前缀)
- 设置项: 主音量/音乐音量/音效音量/静音, 全屏模式/分辨率/UI缩放, 文本速度/语言(预留)
- Canvas层级: 标题界面300, 游戏内250
- 快捷键: F1打开设置, Esc关闭设置

## 社团系统 (ClubSystem)
- `Assets/Scripts/ClubDefinitions.cs` - 社团数据模型 (ClubDefinition/JoinRequirement/PromotionRank/PromotionPath/PartyMembershipStage/ClubMembership/IClubMinigame)
- `Assets/Scripts/ClubSystem.cs` - 社团系统核心 (加入条件检查/退出机制/活动限制/晋升/入党/NPC好感联动/ISaveable存档, 独立处理AP与属性, 金钱走EconomyManager)
- `Assets/Scripts/ClubPanelBuilder.cs` - 社团面板UI构建器 (独立Canvas sortingOrder=200, 左侧列表+右侧详情+入党进度条)
- `Assets/Scripts/ClubPanelManager.cs` - 社团面板控制器 (列表分组:已加入/可加入/特殊, 详情刷新, 条件提示, 按钮绑定)
- `Assets/Resources/Data/ClubData.json` - 社团JSON配置 (9社团+3晋升路径+6入党阶段+加入条件+官方组织标记)

## 信息面板系统 (InfoPanelSystem)
- `Assets/Scripts/InfoPanelBuilder.cs` - 信息面板UI构建器 (独立Canvas sortingOrder=180, 纯代码构建)
- `Assets/Scripts/InfoPanelManager.cs` - 信息面板管理器 (单例模式, 数据绑定+事件订阅+实时刷新)
- 三个子面板:
  - **个人信息面板**: 基础信息(姓名/性别/专业/时间/年龄) + 核心属性(学力/魅力/体魄/领导力, 使用AttributeBar) + 状态值(压力/心情进度条) + 隐性属性(黑暗值/负罪感/幸运) + 学业信息(GPA/学分) + 经济信息(金钱/债务等级) + 社团信息(已加入社团/职务/入党进度)
  - **人际关系面板**: 左侧NPC列表(按好感度降序, 星级显示) + 右侧详情区(好感度进度条/关系等级/恋爱状态/性格偏好/最近3次互动记录) + 社交互动按钮(打开NPCInteractionMenu)
  - **任务面板**: 已由独立任务系统实现，信息面板中的任务标签可作为快捷入口
- 顶部导航栏: 三个标签按钮快速切换, 标签激活时高亮显示
- 入口: HUD底栏"信息"按钮 (btnInfo)
- 数据源: GameState, PlayerAttributes, AffinitySystem, NPCDatabase, ExamSystem, ClubSystem, EconomyManager, DebtSystem
- 事件订阅: GameState.OnStateChanged, PlayerAttributes.OnAttributesChanged, AffinitySystem.OnAffinityChanged (实时刷新)
- 互动记录: AffinitySystem扩展GetRecentInteractions方法, NPCRelationshipData新增interactionHistory字段(List<InteractionRecord>), 存档支持
- 初始化顺序: 在GameSceneInitializer中, HUDManager之后初始化InfoPanelManager

## 任务系统 (MissionSystem)
- `Assets/Scripts/MissionData.cs` - 任务数据模型 (MissionType/MissionStatus/MissionObjectiveType/MissionRewardType枚举, MissionDefinition/MissionObjective/MissionReward/MissionRuntimeData/MissionSaveData)
- `Assets/Scripts/MissionSystem.cs` - 任务系统核心单例 (JSON加载/触发条件检查/进度追踪/完成判定/奖励发放/ISaveable存档集成)
- `Assets/Scripts/MissionUI.cs` - 任务UI管理器 (DontDestroyOnLoad, 右上角通知弹窗+右侧任务追踪+任务完成弹窗)
- `Assets/Scripts/MissionPanelBuilder.cs` - 任务面板构建器 (独立Canvas sortingOrder=200, 查看所有任务: 进行中/已完成, J键打开)
- `Assets/Resources/Data/missions.json` - 任务JSON配置 (8个示例任务: 主线M001~M008, 支线M003/M004/M006/M007)
- 任务类型:
  - **MainStory**: 主线任务, 强制推进剧情, 自动接取, 不可放弃
  - **SideQuest**: 支线任务, 可选内容, 手动接取, 可放弃
- 触发方式: 自动触发 (满足条件时自动解锁, autoAccept=true则自动接取)
- 目标类型: ReachRound/ReachSemester/AttributeThreshold/MoneyThreshold/NPCAffinityThreshold/JoinClub/ActionCount/PassExam/CompleteEvent/Custom
- 奖励类型: Money(金钱)/Attribute(属性)/Unlock(解锁标记)/Item(物品,预留)
- UI组件:
  - **右上角通知**: 任务解锁/接取/完成/失败时滑入弹窗 (Canvas sortingOrder=150, 队列机制, 3秒自动消失)
  - **右侧追踪**: 显示进行中任务的目标进度 (Canvas sortingOrder=50, 实时更新, 无任务时隐藏)
  - **完成弹窗**: 任务完成时弹出奖励详情 (中央弹窗, 缩放动画, 显示奖励列表)
  - **任务面板**: J键打开, 查看所有任务 (进行中/已完成分组, 显示任务类型/描述/目标进度)
- 事件订阅: TurnManager(回合推进/超时检测) + ActionSystem(行动计数) + AffinitySystem(好感度) + ClubSystem(加入社团) + ExamSystem(考试通过) + EventHistory(事件完成)
- 存档集成: 实现ISaveable接口, SaveData.missionData字段, 保存activeMissions/completedMissionIds/failedMissionIds
- 初始化顺序: MissionSystem → MissionUI → MissionPanelBuilder (在GameSceneInitializer中, 惩罚系统之后, Provider注入之前)
- HUD集成: HUDBuilder.btnMission按钮 → HUDManager绑定 → 打开MissionPanelBuilder
- 快捷键: J键打开任务面板, ESC关闭任务面板

## 存档系统 (SaveSystem)
- `Assets/Scripts/SaveSystem/ISaveable.cs` - 可存档接口 (SaveToData/LoadFromData)
- `Assets/Scripts/SaveSystem/SaveData.cs` - 存档数据结构 (GameState+PlayerAttributes+NPC关系+事件+成就+课程+交易)
- `Assets/Scripts/SaveSystem/SaveManager.cs` - 存档管理器 (JSON序列化, 4槽位: auto+3手动, 自动存档)
- `Assets/Scripts/SaveSystem/SaveLoadUI.cs` - 存档/读档UI (纯代码, 槽位卡片+确认弹窗)
- `Assets/Scripts/SaveSystem/NewGamePlusData.cs` - 多周目传承数据 (传承公式/好感/金钱/周目解锁)
- `Assets/Scripts/SaveSystem/NewGamePlusManager.cs` - 多周目管理器 (毕业记录/传承应用/功能解锁)

## 开发者调试工具「钟山台」(DebugConsole)
- `Assets/Scripts/DebugConsole/DebugConsoleManager.cs` - 调试主控 (Ctrl+Shift+D / ~~~ 唤起, 快照系统, 日志系统)
- `Assets/Scripts/DebugConsole/DebugConsoleUI.cs` - 调试UI构建 (半透明覆盖, 9个Tab模块, 8个预设按钮)
- `Assets/Scripts/DebugConsole/DebugPresets.cs` - 8个快速预设 (满属性/巅峰/黑暗/摆烂/贫困/恋爱/新手/空白)
- `Assets/Scripts/DebugConsole/DebugModules/AttributeModule.cs` - 属性面板 (9个滑块+金钱输入)
- `Assets/Scripts/DebugConsole/DebugModules/TimeModule.cs` - 时间控制 (跳转+3种快进模式)
- `Assets/Scripts/DebugConsole/DebugModules/EndingSimModule.cs` - 结局模拟器 (占位)
- `Assets/Scripts/DebugConsole/DebugModules/EventModule.cs` - 事件控制台 (强制触发→EnqueueEvent, 跳过→RecordEvent标记已触发, Refresh显示队列状态)
- `Assets/Scripts/DebugConsole/DebugModules/NPCModule.cs` - NPC控制台 (占位)
- `Assets/Scripts/DebugConsole/DebugModules/EconomyModule.cs` - 经济控制台 (金钱修改+快速按钮)
- `Assets/Scripts/DebugConsole/DebugModules/FormulaModule.cs` - 公式验证器 (传承公式计算)
- `Assets/Scripts/DebugConsole/DebugModules/SnapshotModule.cs` - 快照系统 (内存快照, 比存档更轻量)
- `Assets/Scripts/DebugConsole/DebugModules/LogModule.cs` - 日志追踪器 (实时日志+分类过滤)

## 核心玩法循环
- 地点导航: 点击地图节点 → LocationDetailPanel → "前往"按钮 → LocationManager.MoveTo(扣AP) → 刷新地图+底栏
- 行动系统: 底栏动态按钮(按当前地点过滤) → ActionSystem 执行 → 属性变化
- 社团系统: 底栏"社团"按钮 → 社团面板 → 加入/退出/活动 → ClubSystem独立处理AP与属性
- 回合推进: 行动点耗尽 → TurnManager 延迟0.5s → GameState.AdvanceRound() → ClubSystem.OnRoundEnd()(晋升/入党阶段推进)
- 时间流转: 回合→月份→学期→学年→毕业 (大一到大四, 每学期5回合, 共40回合)
- 睡觉特殊: 消耗1行动点+清空剩余 → 立即触发回合推进
- 地点移动消耗: 免费移动，不消耗行动点
- 底栏按钮: 静态按钮(HUDBuilder创建)被HideStaticButtons()隐藏, 改由RefreshBottomBar()动态创建
- 初始化顺序: SaveManager → NewGamePlusManager → GameState → PlayerAttributes → LocationManager → ActionSystem → ClubSystem → EconomyManager → DebtSystem → ShopSystem → RomanceSystem → ConfessionSystem → CampusRunSystem → PhysicalTestSystem → AchievementSystem → AchievementUI → SemesterSummarySystem → EndingDeterminer → TurnManager → ExamSystem → CheatingSystem → EventHistory → EventScheduler → DialogueSystem → EventExecutor → SettingsManager → NPCEventHub → NPCDatabase → AffinitySystem → RomanceBridge → NPCManager → LocationZoneDetector → NewsSystem → TalentSystem → TalentUI → JobSystem → PenaltySystem → MissionSystem → MissionUI → MissionPanelBuilder → Provider注入 → HUDManager → InfoPanelManager → PauseMenu → DebugConsoleManager(仅Debug构建)

## 存档系统
- 存档路径: `Application.persistentDataPath/saves/` (autosave.json + save_1~3.json)
- 自动存档: 每回合推进后 TurnManager → SaveManager.AutoSave()
- 标题界面: "继续游戏"加载自动存档, "载入游戏"打开SaveLoadUI
- 游戏场景: 底栏"存档"按钮 → SaveLoadUI.Show(true) 打开存档界面
- ISaveable接口: 实现 SaveToData/LoadFromData 即可自动接入存档（FindObjectsOfType 自动发现）
- 已实现 ISaveable: GameState, PlayerAttributes, EconomyManager(交易日志), ClubSystem(社团/入党), EventHistory(事件记录/标记/黑暗值)
- 多周目传承: newgameplus.json, 毕业时记录→新周目应用传承公式, NewGamePlusManager 在 GameSceneInitializer 中初始化
- 传承比例: 首→二10%, 二→三15%, 三→四20%, 四→五+25%

## 调试工具「钟山台」
- 唤起: Ctrl+Shift+D 或连续输入 ~~~
- 仅在 Debug/Development 构建中激活 (#if DEVELOPMENT_BUILD || UNITY_EDITOR)
- 9大模块: 属性/时间/结局/事件/NPC/经济/公式/快照/日志
- 已接入真实数据模块: 属性(滑块调节), 经济(金钱修改), NPC(好感度滑块+恋爱状态), 结局(实时预测结局+星级), 事件(强制触发/跳过), 时间(跳转/快进)
- 8个预设: 满属性/巅峰/黑暗/摆烂/贫困/恋爱/新手/空白
- Canvas sortingOrder=200 (覆盖所有UI)

## 金钱经济系统
- 初始金钱: ¥8000, GameState.Money 为唯一真值源 (允许负数，支持债务)
- EconomyManager: 交易记录层, Earn()/Spend() 均通过 GameState.AddMoney() 修改余额
  - TransactionType: 19种收支类型 (LivingExpense/Tuition/Food/Daily/Clothing 等)
  - 回合结算: 生活费+1500/回合, 学期开始学费-5000
  - 班委工资: 学生会副主席/主席 500/回合, 干事/部长 200/回合 (通过 ClubSystem 检查职务)
- DebtSystem: 4级债务阈值
  - Normal (≥200) → FoodRestricted (<200) → Overdrafted (<0) → LoanTrigger (<-2000) → Bankruptcy (<-5000)
  - 透支惩罚: 每回合 Stress += 10
  - 破产对接结局: Bankruptcy 时设置 EventHistory flag "bankruptcy_triggered" + 触发 EndingUI 显示强制结局
  - 通过 IDebtEventTrigger 接口解耦事件响应
- ShopSystem: 17种商品, 6分类 (食品/日用/服装/学习/娱乐/社交)
  - 债务限购: FoodRestricted → 仅≤¥3食品; Overdrafted → 仅食品+学习
  - 购买走 EconomyManager.Spend() 记录交易
- ShopUIBuilder: 独立 Canvas (sortingOrder=200), 纯代码 UI
- HUD 显示: 金钱透支→红色, <200→橙色+⚠图标

## 校园地图与场景导航系统
- 8个校园地点: 教学楼/图书馆/宿舍/食堂/操场/教超/快递站/外卖站
- 地点枚举: LocationId (TeachingBuilding/Library/Dormitory/Canteen/Playground/Supermarket/ExpressStation/TakeoutStation)
- 邻接图: 保留邻接关系定义，但移动全部免费（不消耗AP）
- LocationManager: 单例管理器, 硬编码8地点+邻接关系, MoveTo()不消耗AP+更新GameState.CurrentLocation+触发OnLocationChanged
- GetAvailableActions(): 返回当前地点绑定的行动 + 所有isGlobal=true的全局行动(如睡觉)
- CampusMapUI: 纯C#类(非MonoBehaviour), 在HUD centerPanel构建节点图, 当前地点金色高亮, NPC数量指示
- LocationDetailPanel: 纯C#类, 浮动详情面板, 展示行动预览(含属性效果)+NPC列表+移动消耗+前往/取消按钮
- HUDManager集成: InitMapUI()创建地图, RefreshBottomBar()动态生成行动按钮, HideStaticButtons()隐藏旧按钮
- 12种行动: study(2AP)/attend_class(2AP)/social(1AP)/play_game(1AP)/sleep(全局,1AP)/goout(全局,2AP)/eat(1AP)/exercise(1AP)/sports_test(2AP)/shop(1AP)/pickup_express(1AP)/order_takeout(1AP)/memorize_words(背单词,全局,1AP)
- NPC分布: 静态分配(林知秋→宿舍, 苏小晴→食堂, 周然→宿舍, 谢凌云→图书馆), 预留NPCScheduleManager动态日程

## 对话系统 (数据驱动)
- 架构: DialogueParser(数据) + DialogueUIBuilder(UI) + DialogueSystem(引擎)
- JSON 对话文件: `StreamingAssets/Dialogues/*.json`
- 对话节点: ID前缀"D", speaker/portrait/content/next/choices
- 选项分支: 最多4个, 条件判断(属性/金钱), 效果应用(属性/金钱变化)
- 条件语法: "学力>=80 AND 心情>50", 支持 AND/OR
- 特殊模式: speaker="" → 旁白, speaker="_inner" → 内心独白(斜体)
- 事件: OnDialogueStart/OnDialogueEnd/OnDialogueEnded/OnChoiceMade
- 兼容: 保留旧 StartDialogue(string, string[]) 接口
- NPC 入口: NPCData.dialogueId → JSON对话, 无则回退 greetingLines
- Canvas: sortingOrder=200 (高于 HUD 的 100)

## NPC 数据库与好感度系统
- 数据源: `Resources/Data/npc_database.json` (4个NPC + 11种社交行动)
- NPC: 林知秋(室友A/内向), 苏小晴(室友B/外向), 周然(室友C/随和), 谢凌云(学姐/神秘)
- 好感度等级: Stranger(0) → Acquaintance(20) → Friend(40) → CloseFriend(60) → BestFriend(80) → Lover(80+romance)
- 公式: actual_delta = base × charm_coeff × personality_match × repeat_decay
  - charm_coeff = 1 + 魅力/100
  - personality_match: 喜欢1.5 / 中性1.0 / 不喜欢0.5
  - repeat_decay = max(0.3, 1 - 0.15 × repeatCount)
- 自然衰减: 每回合按等级 0/0/-1/-2/-3/-4, 连续3回合无互动额外-1
- 社交行动: greet(1AP), chat(2AP), eat_together(2AP+¥30), give_gift(1AP+¥100), help(3AP), hang_out(3AP+¥80), deep_talk(3AP), study_together(2AP), go_party(2AP+¥50), karaoke(2AP+¥60), encourage(1AP)
- 行动解锁: 按好感度等级门槛, 如 deep_talk 需 Friend 以上
- 事件架构: NPCEventHub 解耦互动与对话, AffinitySystem 发射 OnAffinityChanged/OnAffinityLevelChanged/OnInteractionCompleted
- NPC日程: 按时间段(Morning/Afternoon/Evening)出现在不同地点
- 时间段映射: AP>=4→Morning, AP>=2→Afternoon, else→Evening
- IRelationshipExtension: 由 RomanceBridge 实现，连接 AffinitySystem 与 RomanceSystem

## 恋爱系统 (RomanceSystem)
- `Assets/Scripts/RomanceData.cs` - 恋爱数据定义 (RomanceState/RomanceEndingTier/BreakupReason枚举 + RomanceRecord类, 含isCheating/cheatingRounds劈腿字段)
- `Assets/Scripts/RomanceSystem.cs` - 恋爱核心单例 (状态机/健康度/回合结算/分手/结局判定, 实现IRomanceProvider接口)
- `Assets/Scripts/ConfessionSystem.cs` - 告白系统单例 (成功率公式/执行告白/复合逻辑/对话触发/劈腿标记)
- `Assets/Scripts/RomanceBridge.cs` - 桥接器 (实现IRelationshipExtension, AffinitySystem<->RomanceSystem双向同步)
- 状态机: None -> Crushing(好感>=60) -> Dating(告白成功) -> BrokenUp/Hostile(分手)
  - Cooldown: 告白失败/分手后冷却4回合, 冷却结束后若好感>=60恢复Crushing
  - BrokenUp: 分手后冷却4回合(每回合递减), 冷却结束后可复合
  - Hostile: 劈腿被发现，不可恢复
- 告白公式: 50% + Charm*0.2% - Stress*0.1% + (affinity-80)*1% + locationBonus + npcBonus, clamp [20%, 95%]
  - 复合成功率 = 正常成功率 * 0.7
  - 告白消耗: 2AP
- 恋爱健康度: 初始70, 范围0~100
  - 未互动: 每回合 -8; 连续4回合未互动 -> 触发分手
  - 约会: +10; 纪念日互动 +15, 纪念日未互动 -20
  - 归零 -> 触发分手
- 回合结算: 恋爱中每回合 Mood+3, Stress-2, Money-20
- 纪念日: 每8回合一次
- 分手触发入口:
  - HealthZero: 健康度归零自动触发
  - ConsecutiveNoInteract: 连续4回合未互动自动触发
  - CheatingDiscovered: 劈腿被发现→所有Dating变Hostile
  - PlayerInitiated: NPCInteractionMenu"分手"按钮 → TriggerPlayerBreakup()
  - NPCInitiated: ProcessRoundEnd()中好感度<50自动触发
  - SpecialEvent: 事件系统调用 TriggerSpecialBreakup()
  - 分手惩罚: Mood-10, Stress+5
- 劈腿检测: 告白成功时若已有Dating对象→标记isCheating, 每回合递增概率(20%+回合数*10%)检测发现
- 复合条件: BrokenUp + 冷却结束 + 好感>=70 + 分手次数<2 + 未复合过
- 结局等级: 5*Engaged / 4*Sweet / 3*Confused / 2*BrokenUp / 1*Single
- IRomanceProvider实装: RomanceSystem实现IRomanceProvider, 已注入SemesterSummarySystem
  - HasPartner(): 任一NPC处于Dating状态
  - GetPartnerName(): 第一个Dating NPC的显示名
  - GetRomanceLevel(): 最高Dating NPC的RomanceEndingTier数值
- EndingDeterminer对接: HasPartner/RomanceLevel_GreaterOrEqual条件已接入真实数据
- 事件: OnRomanceStateChanged / OnRomanceHealthChanged / OnConfessionResult
- NPCInteractionMenu: 按恋爱状态动态显示告白/约会/分手/复合按钮 (粉紫色调, 分手红色调)
- RomanceState枚举: 定义在RomanceData.cs, NPCData.cs中旧定义已移除

## 社团系统
- 数据源: `Resources/Data/ClubData.json` (9社团+3晋升路径+6入党阶段+加入条件+官方组织标记)
- 社团: 跑协(体魄), 篮球社(体魄,需体魄≥80), 吉他社(魅力), 辩论社(学力,需学力≥90), 青协(领导力), 学生会(领导力,需魅力≥90或领导力≥60), 动漫社(心情), 校团委(领导力,不占名额,官方), 党建班(领导力,不占名额,官方)
- 名额规则: 最多加入2个占名额社团, 校团委和党建班不占名额(occupiesSlot=false)
- 加入条件: JoinRequirement[] (属性门槛, 支持AND/OR逻辑), 退出冷却期2回合
- 社团活动: 独立于ActionSystem, activityAPCost=2, 金钱走EconomyManager.Spend()记录交易, 每社团每回合最多活动1次
- 活动属性效果(策划值): 跑协(体魄+8/心情+3/压力-5/¥20), 篮球社(体魄+6/魅力+3/压力-3/¥30), 吉他社(魅力+5/心情+8/压力-3/¥50), 辩论社(学力+6/领导力+3/压力+2), 青协(魅力+4/领导力+3/心情+2), 学生会(领导力+5/魅力+2/压力+5), 动漫社(魅力+3/心情+10/压力-5/¥30), 校团委(领导力+5/学力+2/压力+3), 党建班(领导力+4/学力+3/压力+2)
- NPC好感联动: 社团活动后同社团NPC好感+3~5 (通过AffinitySystem)
- 退出机制: 退出惩罚领导力-5, 退出学生会额外-10, 官方组织(校团委/党建班)不可主动退出, 退出冷却2回合
- 被动退出: 连续5回合未活动 → 自动踢出+压力+5 (官方组织豁免)
- 晋升路径: 3种路径
  - standard: 干事(0AP)→部长(1AP,10回合)→社长(2AP,20回合)
  - student_union: 干事→部长→副主席(2AP)→主席(3AP)
  - youth_league: 干事→部长→副书记(2AP)
- 晋升条件: 在社回合数 + 属性要求, 每回合自动检查(OnRoundEnd)
- 职务行动点扣减: 所有社团职务的apCost之和 → GameState.PositionAPCost → 减少每回合可用行动点
- 入党流程: 6阶段 (未申请→入党申请书→入党积极分子→发展对象→预备党员→正式党员)
  - 申请条件: 领导力≥60 + 学力≥60 + GPA≥2.5 + 负罪感≤30 + 全局回合≥12
  - 阶段推进: 按全局回合数自动推进(OnRoundEnd), 负罪感>30阻塞推进
- 存档集成: ClubSystem实现ISaveable, 序列化joinedClubs/入党进度/退出冷却/不活跃计数
- 事件: OnClubStateChanged/OnClubJoined/OnClubLeft/OnPromoted/OnPartyStageChanged
- UI: 独立Canvas(sortingOrder=200), 左侧列表(已加入/可加入/特殊分组) + 右侧详情 + 入党进度条 + 条件提示文本
- HUD集成: 底栏"社团"按钮→ClubPanelManager.OpenPanel(), 回合推进→ClubSystem.OnRoundEnd()

## 字体系统
- FontManager 单例 (DontDestroyOnLoad)
- TMP 全局 Fallback 字体自动配置
- 字体资源: `Resources/Fonts/`

## 资源
- 视频背景: `StreamingAssets/Start screen.mp4`
- 游戏标题图: `Resources/GameLogo.png` (需设为 Sprite 类型)
- 水涟漪 Shader: `Assets/Shaders/WaterRipple.shader`
- 对话数据: `StreamingAssets/Dialogues/*.json` (JSON格式)
- NPC头像: `Resources/NPCSprite.png` (可按NPC扩展)
- NPC数据库: `Resources/Data/npc_database.json` (NPC定义+社交行动定义)
- 社团数据: `Resources/Data/ClubData.json` (社团定义+晋升路径+入党阶段)
- 考试题库: `Resources/ExamData/question_bank.json` (12科目78组234题)
- 课程表: `Resources/ExamData/course_schedule.json` (8学期40门课121学分)
- 结局数据: `Resources/Data/endings.json` (24个结局定义, 8层优先级)
- 成就数据: `Resources/Data/achievements.json` (20个成就定义)
- 事件数据: `Resources/Data/Events/*.json` (main_events/fixed_events/conditional_events/dark_events)
- 天赋数据: `Resources/Data/talents.json`
- 新闻数据: `Resources/Data/news.json`
- 体测数据: `Resources/Data/physical_tests.json`
- 兼职数据: `Resources/Data/jobs.json`

## 考试与绩点系统 (ExamSystem)
- `Assets/Scripts/Exam/ExamData.cs` - 数据类 (ExamType/CheatResult枚举, CourseDefinition/ExamQuestion/QuestionGroup/ExamResult/SemesterGPA)
- `Assets/Scripts/Exam/IExamResultProvider.cs` - 考试结果查询接口 (供结局/天赋系统使用)
- `Assets/Scripts/Exam/GPACalculator.cs` - GPA计算器 (纯静态, 分数→绩点映射, 学期/累积GPA)
- `Assets/Scripts/Exam/ExamSystem.cs` - 考试系统单例 (题库加载/考试流程/通过率计算/成绩记录/IExamResultProvider实现)
- `Assets/Scripts/Exam/CheatingSystem.cs` - 作弊系统单例 (30%被抓/属性惩罚/累计开除)
- `Assets/Scripts/Exam/ExamUIBuilder.cs` - 考试UI构建器 (纯代码, Canvas sortingOrder=500)
- `Assets/Scripts/Exam/ExamUIManager.cs` - 考试UI管理器 (状态机/答题交互/动画/成绩单)
- 考试触发:
  - 期中考试: TurnManager在第20回合(学期中间)拦截推进 → ExamSystem.StartMidtermExam() → 取ceil(N/2)门课 → 成绩独立记录(不影响GPA)
  - 期末考试: TurnManager在第40回合(学期末)拦截推进 → 考试UI → 答题 → 成绩单 → 继续推进
  - 补考触发: 新学期第1回合自动检测挂科课程
- 证书考试:
  - CET4(大学英语四级): 基础通过率30%, 大一下可触发, 通过后解锁CET6, ExamSystem.StartCET4Exam()
  - CET6(大学英语六级): 基础通过率20%, 需CET4通过, ExamSystem.StartCET6Exam()
  - 计算机等级考试: 基础通过率40%, ExamSystem.StartComputerLevelExam()
  - 证书考试: 单门课3题, credits=0(不影响GPA), 通过后设置状态标记
  - 查询: IsCET4Passed / IsCET6Passed / IsComputerLevelPassed
- 通过率公式: 基础(50%/按类型) + 自习次数×10%(封顶3次=+30%) + 答题修正(对+5%/错-10%/全对+5%) + 属性修正(-压力×0.1%+幸运×0.1%-负罪感×0.08%), clamp(5%, 99%)
- 补考GPA回写: 补考通过后 ProcessMakeupResults() → UpdateSemesterGPAHistory() 更新原学期成绩和GPA
- GPA映射: 90+→4.0, 80+→3.0, 70+→2.0, 60+→1.0, <60→0(挂科)
- 作弊: 30%被抓→该科0分+黑暗值+10+负罪感+15+压力+20; 累计被抓≥2次→强制结局「学术不端·开除」
- 事件: OnSingleExamFinished / OnExamCompleted / OnExpulsionTriggered
- HUD集成: 顶栏GPA显示(3.5+金色/2.0+浅蓝/<2.0红色)

## 学期总结系统 (SemesterSummarySystem)
- `Assets/Scripts/IGameDataProviders.cs` - Provider接口 (IGPAProvider/INPCRelationshipProvider/IClubMembershipProvider/IEconomyProvider/IRomanceProvider) + 数据类 (CourseGrade/NPCRelationInfo)
- `Assets/Scripts/SemesterSummaryData.cs` - 数据模型 (SemesterGrade枚举D~S, SemesterSummaryData, AttributeSnapshot)
- `Assets/Scripts/SemesterSummarySystem.cs` - 学期总结核心单例 (评分/快照/行动统计/Provider调度, 已实装真实Provider)
- `Assets/Scripts/SemesterSummaryUI.cs` - 学期总结UI面板 (全屏覆盖, ScrollRect, 成绩单+属性变化+NPC好感+成就+评分明细+等级动画)
- Provider注入: GameSceneInitializer初始化末尾调用InjectRealProviders()
  - RealGPAProvider → ExamSystem.Instance (回退Study/25f)
  - RealNPCProvider → AffinitySystem.Instance + NPCDatabase.Instance
  - RealClubProvider → ClubSystem.Instance
  - RealEconomyProvider → EconomyManager.Instance + GameState.Instance
  - RealRomanceProvider → RomanceSystem.Instance + NPCDatabase.Instance
- 评分公式: 学业分(GPA×1000) + 人际分(NPC好感总和) + 体育分(min(physique×2.5,200)) + 成就分 - 扣分项(stress×2)
- 等级: S(≥6000) / A(≥4500) / B(≥3000) / C(≥1500) / D(<1500)
- 毕业总评: Σ(学期分×年度权重), 权重: 大一×1.0 / 大二×1.5 / 大三×2.0 / 大四×2.5
- 触发时机: TurnManager.OnRoundAdvanced → NextSemester/NextYear/Graduated
- 行动统计: studyCount/socialCount/goOutCount/sleepCount/totalMoneySpent, 订阅ActionSystem.OnActionExecuted
- Provider模式: 默认Provider用模拟数据(GPA=Study/25), 子系统实装后可替换
- HUD集成: HUDManager.OnRoundAdvanced → ShowSemesterSummary() → SemesterSummaryUI.Show()
- Canvas: sortingOrder=100

## 结局判定系统 (EndingDeterminer)
- `Assets/Scripts/EndingData.cs` - 数据模型 (EndingLayer枚举0~7, EndingConditionType枚举~20种, EndingCondition, EndingDefinition, EndingResult)
- `Assets/Scripts/EndingDeterminer.cs` - 结局判定核心单例 (8层优先级, JSON加载, 条件求值, 已接入所有子系统)
- `Assets/Scripts/EndingUI.cs` - 结局展示面板 (全屏黑底, TypeWriter+星级动画+CG占位+统计网格+天赋点弹跳)
- `Assets/Resources/Data/endings.json` - 24个结局定义 (END_001~END_024, 覆盖8层)
- 8层优先级: Layer0(强制坏结局) → Layer1~6(条件结局) → Layer7(保底)
- 条件类型: GPA/属性/好感度/社团/金钱/恋爱/成就/作弊/时间/AlwaysTrue等
- 条件数据源(已全部接入):
  - HasPartner → RomanceSystem (遍历NPC查Dating状态)
  - RomanceLevel → RomanceSystem.GetRomanceHealth (最高恋爱健康度)
  - IsStudentCouncilPresident → ClubSystem (student_council最高职位)
  - IsPartyMember → ClubSystem.CurrentPartyStage (最终阶段=正式党员)
  - HasNationalScholarship → EventHistory.GetFlag("HasNationalScholarship")
  - CheatingCount → CheatingSystem.CaughtCount
  - SlackingValue → 运行时计算: max(0, 100 - Study - studyCount*2)
  - MentalHealth → 运行时计算: max(0, Mood - Stress)
  - GPA → ExamSystem.GetCumulativeGPA() (回退Study/25f)
  - achievementCount → AchievementSystem.GetUnlockedCount()
- 星级: 0~7★, 天赋点: 7★=10 / 6★=8 / 5★=6 / 4★=5 / 3★=4 / 2★=2 / 1★=1 / 0★=0
- 统计数据: EndingResult含totalStudyCount/totalSocialCount/totalGoOutCount/totalSleepCount/totalMoneySpent/finalGPA/achievementCount/totalRounds
- HUD集成: HUDManager.OnRoundAdvanced(Graduated) → ShowGraduationEnding() → EndingUI.Show()
- Canvas: sortingOrder=200

## 成就系统 (AchievementSystem)
- `Assets/Scripts/AchievementData.cs` - 数据模型 (AchievementConditionType枚举20种, AchievementCondition, AchievementDefinition, AchievementSaveData)
- `Assets/Scripts/AchievementSystem.cs` - 成就系统核心单例 (JSON加载/条件检查/ISaveable存档集成/学期追踪)
- `Assets/Scripts/AchievementUI.cs` - 成就UI管理器 (DontDestroyOnLoad, 弹窗通知+回顾面板)
- `Assets/Resources/Data/achievements.json` - 20个成就定义 (ACH_001~ACH_020, 属性/行动/金钱/时间等)
- 弹窗通知: 成就解锁→右上角滑入弹窗, 队列机制, Canvas sortingOrder=150, blocksRaycasts=false
- 回顾面板: 标题界面"成就"按钮→AchievementUI.ShowReviewPanel(), Canvas sortingOrder=300
  - 已解锁: 金色边框+🏆+名称+描述+✓绿色
  - 未解锁: 灰色边框+❓+??????+🔒灰色
- 持久化: 实现ISaveable接口, 通过SaveManager统一存档, 支持多槽位隔离
- 条件类型: Study/Charm/Physique/Leadership≥X, ActionCount≥X, Money≥X, GPA≥X, Semester/Year, NPC好感, SemesterGrade等
- 触发时机: 行动执行后(ActionSystem.OnActionExecuted) + 回合推进后(TurnManager) + 学期结算前(SemesterSummarySystem) + 考试完成后(ExamSystem)
- 事件: OnAchievementUnlocked(AchievementDefinition)
- 学期成就追踪: currentSemesterAchievements, 学期切换时自动调用ResetSemesterAchievements()
- 标题界面集成: TitleScreenManager.OnTopIconClicked(index==1) → AchievementUI.ShowReviewPanel()
- 修复记录 (2026-04-23):
  - 实现ISaveable接口, 从PlayerPrefs迁移到SaveData存档系统
  - 修复SemesterGrade条件逻辑: 从"检查最后一个学期"改为"检查是否存在任一学期达标"
  - 修复ActionSystem订阅失败问题: 增加协程重试机制
  - 集成学期重置: SemesterSummarySystem在学期切换时调用ResetSemesterAchievements()
  - 多触发点: TurnManager回合推进后、ExamSystem考试完成后、SemesterSummarySystem学期结算前均触发检测


## 游戏事件系统 (EventSystem)
- `Assets/Scripts/GameEventData.cs` - 事件数据模型 (EventType/EventPriority/TriggerPhase枚举, EventTriggerCondition/EventDialogue/EventEffect/EventChoice/EventDefinition)
- `Assets/Scripts/IDialogueTrigger.cs` - 对话触发接口 (解耦事件系统与DialogueSystem, IsActive/ShowDialogue/ShowChoices)
- `Assets/Scripts/EventScheduler.cs` - 事件调度器单例 (JSON加载/条件检查/优先级排序/队列管理/行为通知)
- `Assets/Scripts/EventHistory.cs` - 事件历史单例 (触发记录/标记系统/黑暗值追踪)
- `Assets/Scripts/EventExecutor.cs` - 事件执行器单例 (通过IDialogueTrigger桥接对话/效果应用/事件链/历史记录)
- 4种事件类型:
  - Fixed (FE): 固定事件, 可重复触发
  - MainStory (ME): 主线事件, 强制优先级最高
  - Conditional (CE): 条件事件, 满足属性/金钱/先决事件等条件触发
  - Dark (DE): 黑暗事件, 行为触发+条件检查, 影响黑暗值
- 优先级: Forced(0) > MainStory(1) > Conditional(2) > Fixed(3), 数值越小越优先
- 触发阶段: RoundStart(回合开始) / ActionComplete(行动完成后) / RoundEnd(回合结束)
- 条件系统: 时间(学年/学期/回合) + 属性(>=/<=/>/</==/!=) + 金钱 + 好感度(AffinityCondition已实现) + 先决事件 + 黑暗值 + 行为触发
- AffinityCondition: 通过AffinitySystem.Instance查询NPC好感度, 支持minValue(数值)和minLevel(等级枚举)双条件(AND语义)
- 效果类型: attribute(属性修改) / money(金钱修改) / flag(标记设置) / darkness(黑暗值修改) / unlock(解锁标记)
- 事件链: EventDefinition.chainEventIds → 执行完毕后自动入队后续事件
- 回合流程集成:
  - TurnManager.HandleActionExecuted() → CheckAndTriggerEvents(ActionComplete)
  - TurnManager.DoAdvanceRound() → CheckAndTriggerEvents(RoundEnd) → AdvanceRound() → CheckAndTriggerEvents(RoundStart)
- HUD集成: 事件触发时禁用行动按钮, 事件队列清空后恢复
- PlayerController集成: EventExecutor.IsExecuting 时锁定玩家移动
- 事件: OnEventTriggered / OnEventCompleted (EventScheduler), OnEventRecorded (EventHistory)
- 初始化顺序: EventHistory → EventScheduler(+LoadEvents) → DialogueSystem → EventExecutor
- 数据源: `Resources/Data/Events/main_events.json`, `fixed_events.json`, `conditional_events.json`, `dark_events.json`
