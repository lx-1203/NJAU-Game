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

## 关键脚本
- `Assets/Scripts/TitleScreenManager.cs` - 标题界面管理器 (视频背景+菜单)
- `Assets/Scripts/RippleEffect.cs` - 水涟漪特效 (点击触发)
- `Assets/Scripts/FontManager.cs` - TMP 字体全局管理 (中文字体)
- `Assets/Scripts/SceneLoader.cs` - 场景加载器
- `Assets/Scripts/DialogueSystem.cs` - 对话系统 (TextMeshProUGUI)
- `Assets/Scripts/NPCController.cs` - NPC 控制器

## 字体系统
- FontManager 单例 (DontDestroyOnLoad)
- TMP 全局 Fallback 字体自动配置
- 字体资源: `Resources/Fonts/`

## 资源
- 视频背景: `StreamingAssets/Start screen.mp4`
- 游戏标题图: `Resources/GameLogo.png` (需设为 Sprite 类型)
- 水涟漪 Shader: `Assets/Shaders/WaterRipple.shader`
