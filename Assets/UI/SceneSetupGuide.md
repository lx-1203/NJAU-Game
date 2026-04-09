# Unity 开始界面场景设置指南

## 场景设置步骤

### 1. 创建新场景
1. 在Unity编辑器中，点击菜单：`文件 > 新建场景`
2. 选择 `Basic 2D` 模板
3. 保存场景到：`Assets/Scenes/StartMenu/StartMenuScene.unity`
   - 点击菜单：`文件 > 另存为`，选择对应路径

### 2. 设置画布（Canvas）
1. 在层级窗口（Hierarchy）中右键点击，选择：`UI > 画布`
2. 选中创建好的 Canvas 对象
3. 在检查器窗口（Inspector）中设置以下属性：
   - **渲染模式（Render Mode）**：`屏幕空间 - 覆盖（Screen Space - Overlay）`
   - **Canvas Scaler 组件**：
     - **UI缩放模式（UI Scale Mode）**：`随屏幕尺寸缩放（Scale With Screen Size）`
     - **参考分辨率（Reference Resolution）**：`1920 x 1080`
     - **屏幕匹配模式（Screen Match Mode）**：`匹配宽度或高度（Match Width or Height）`
     - **匹配值（Match）**：`0.5`

### 3. 添加事件系统（EventSystem）
1. 在层级窗口中右键点击，选择：`UI > 事件系统`
2. 确保事件系统对象包含以下组件：
   - EventSystem 组件
   - StandaloneInputModule 组件

### 4. 创建UI管理器对象
1. 在层级窗口中右键点击，选择：`创建空对象`
2. 重命名为：`UIManager`
3. 通过检查器窗口的"添加组件"按钮，添加以下脚本：
   - `UIManager`
   - `UIEventManager`
   - `UIAnimator`
   - `UIVisualEffects`
   - `DeviceAdapter`
   - `UIPerformanceOptimizer`

### 5. 创建UI配置资源
1. 在项目窗口（Project）中右键点击，选择：`创建 > UI > Layout Config`
2. 重命名为：`UILayoutConfig`
3. 在检查器窗口中设置配置参数

### 6. 创建UI元素

#### 主菜单面板
1. 在 Canvas 下右键，选择 `UI > 面板`，命名为 `MainMenuPanel`
2. 设置属性：
   - **RectTransform**：锚点预设选择 `拉伸, 拉伸`（Stretch, Stretch）
   - **颜色（Color）**：`(0.1, 0.1, 0.15, 0.9)`

#### 游戏标题
1. 在 MainMenuPanel 下右键，选择 `UI > 文本 - TextMeshPro`
2. 命名为 `GameTitle`
3. 设置属性：
   - **文本（Text）**：`游戏名称`
   - **字体大小（Font Size）**：`72`
   - **对齐方式（Alignment）**：`居中`
   - **位置（Position）**：`(0, 200, 0)`

#### 副标题
1. 在 MainMenuPanel 下右键，选择 `UI > 文本 - TextMeshPro`，命名为 `GameSubtitle`
2. 设置属性：
   - **文本（Text）**：`一个精彩的2D游戏`
   - **字体大小（Font Size）**：`36`
   - **位置（Position）**：`(0, 120, 0)`

#### 按钮面板
1. 在 MainMenuPanel 下右键，选择 `UI > 面板`，命名为 `ButtonPanel`
2. 设置属性：
   - **大小（Size）**：`(300, 300)`
   - **位置（Position）**：`(0, -50, 0)`

#### 按钮创建
1. 在 ButtonPanel 下右键，选择 `UI > 按钮 - TextMeshPro`，命名为 `StartButton`
2. 设置属性：
   - **大小（Size）**：`(250, 60)`
   - **位置（Position）**：`(0, 80, 0)`
   - **按钮文本**：`开始游戏`

3. 按照同样方式创建其他按钮：
   - `SettingsButton`：位置 `(0, 0, 0)`，文本：`设置`
   - `AboutButton`：位置 `(0, -80, 0)`，文本：`关于`
   - `QuitButton`：位置 `(0, -160, 0)`，文本：`退出`

#### 版权信息
1. 在 MainMenuPanel 下右键，选择 `UI > 文本 - TextMeshPro`，命名为 `CreditsText`
2. 设置属性：
   - **文本（Text）**：`© 2024 游戏工作室`
   - **字体大小（Font Size）**：`16`
   - **位置（Position）**：`(0, -300, 0)`

### 7. 设置面板引用
1. 在层级窗口中选中 UIManager 对象
2. 在检查器窗口中，将以下对象拖拽到对应字段：
   - **mainMenuPanel**：拖入 MainMenuPanel
   - **startGameButton**：拖入 StartButton
   - **settingsButton**：拖入 SettingsButton
   - **aboutButton**：拖入 AboutButton
   - **quitButton**：拖入 QuitButton

### 8. 创建设置面板
1. 在 Canvas 下右键，选择 `UI > 面板`，命名为 `SettingsPanel`
2. 设置属性：
   - **RectTransform**：锚点预设选择 `居中`
   - **大小（Size）**：`(600, 400)`
   - **位置（Position）**：`(0, 0, 0)`
   - 取消勾选对象名称旁的 **激活复选框**（设为未激活状态）

3. 在 SettingsPanel 下添加以下UI元素：
   - 设置标题（文本 - TextMeshPro）
   - 音量滑块（`UI > 滑动条`）
   - 全屏开关（`UI > 开关`）
   - 返回按钮（`UI > 按钮 - TextMeshPro`）

### 9. 创建关于面板
1. 在 Canvas 下右键，选择 `UI > 面板`，命名为 `AboutPanel`
2. 设置属性：
   - **RectTransform**：锚点预设选择 `居中`
   - **大小（Size）**：`(600, 400)`
   - **位置（Position）**：`(0, 0, 0)`
   - 取消勾选对象名称旁的 **激活复选框**（设为未激活状态）

3. 在 AboutPanel 下添加以下UI元素：
   - 关于标题（文本 - TextMeshPro）
   - 游戏描述（文本 - TextMeshPro）
   - 版本信息（文本 - TextMeshPro）
   - 返回按钮（`UI > 按钮 - TextMeshPro`）

### 10. 更新UIManager引用
1. 在层级窗口中选中 UIManager 对象
2. 在检查器窗口中，将以下对象拖拽到对应字段：
   - **settingsPanel**：拖入 SettingsPanel
   - **aboutPanel**：拖入 AboutPanel

---

## 脚本配置

### UIManager 配置
- 设置所有面板和按钮的引用
- 配置设置面板的滑块和开关

### UIEventManager 配置
- 设置音效（可选）
- 启用震动反馈（可选）

### UIAnimator 配置
- 设置动画持续时间
- 配置缓动曲线

### DeviceAdapter 配置
- 设置设备类型阈值
- 配置不同设备的缩放因子

### UIPerformanceOptimizer 配置
- 启用批处理优化
- 设置对象池大小

---

## 测试步骤

1. **保存场景**：按 `Ctrl + S` 确保所有修改都已保存
2. **设置启动场景**：
   - 点击菜单：`文件 > 生成设置`
   - 将 StartMenuScene 拖入"生成中的场景"列表
   - 确保其序号为 0（即第一个加载的场景）
3. **运行游戏**：
   - 点击编辑器顶部的 **播放按钮**（▶）
   - 检查UI是否正确显示
   - 测试按钮交互是否正常

---

## 故障排除

### 常见问题

#### 问题：UI没有显示
- 检查 Canvas 的渲染模式是否为"屏幕空间 - 覆盖"
- 确保 Canvas Scaler 的 UI 缩放模式设置正确
- 检查UI元素是否处于激活状态（检查器顶部的复选框）

#### 问题：按钮没有反应
- 确保场景中存在 EventSystem 对象
- 检查按钮的 OnClick 事件是否已注册对应方法
- 检查 UIManager 脚本是否正确挂载到对象上

#### 问题：动画不播放
- 检查 UIAnimator 脚本是否已挂载
- 确保动画持续时间设置合理（不为0）
- 查看控制台窗口（`窗口 > 通用 > 控制台`）是否有错误信息

#### 问题：响应式适配不工作
- 检查 DeviceAdapter 脚本是否已挂载
- 确认 CanvasScaler 配置正确
- 使用游戏视图的分辨率下拉菜单测试不同分辨率

---

## 响应式测试

在游戏视图（Game View）顶部的分辨率下拉菜单中切换以下分辨率进行测试：

1. **1920x1080** — 桌面端
2. **1024x768** — 平板端
3. **720x1280** — 手机端

---

## 自定义主题

要自定义UI主题，按以下步骤操作：

1. 修改 UILayoutConfig 资源中的颜色参数
2. 调整字体和字号
3. 修改按钮样式和面板外观

---

## 优化提示

- 使用精灵图集（Sprite Atlas）减少 Draw Call
- 优化字体资源，避免加载过多字体
- 使用对象池管理频繁创建销毁的UI元素
- 合理拆分 Canvas，避免单个 Canvas 下元素过多导致重建开销

---

## 注意事项

- 确保已通过包管理器安装 TextMeshPro 包（`窗口 > 包管理器`）
- 所有脚本都需要正确的命名空间引用
- 养成定期保存场景和项目的习惯（`Ctrl + S`）
- 使用性能分析器（`窗口 > 分析 > 性能分析器`）监控运行时性能

---

## 获取帮助

如果遇到问题，请按以下步骤排查：

1. 打开控制台窗口（`窗口 > 通用 > 控制台`）查看错误信息
2. 验证所有引用都已正确拖拽设置（检查器中无 `None` 或 `Missing` 标记）
3. 确保所有脚本都已正确挂载到对应的游戏对象上
4. 参考技术文档：`Assets/UI/UIDesignDocumentation.md`
