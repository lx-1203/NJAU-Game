# 项目记忆文档

> 本文档用于持久化记录项目的关键信息，包括时间戳、TODO列表和用户偏好。
> 每次项目变动时应及时更新本文档。

---

## 用户偏好

| 偏好项 | 值 |
|--------|-----|
| 默认语言 | 中文 |
| 文档更新策略 | 每次项目变动及时更新记忆文档 |
| 重大改动编辑方式 | 采用 spec-coding 的形式进行编辑（先写规格说明，再实现代码） |

---

## 时间线 / 变更日志

| 时间戳 | 事件 | 详情 |
|--------|------|------|
| 2026-04-05 13:05 | 项目初始化记忆文档 | 创建 MEMORY.md，记录用户偏好 |
| 2026-04-05 13:05 | 安装 unity-developer skill | 从 antigravity-skills 仓库获取并配置到 .kilo/skill/unity-developer/SKILL.md |

---

## TODO 列表

### 进行中

_（暂无）_

### 待办

_（暂无）_

### 已完成

- [x] 创建项目记忆文档 (MEMORY.md)
- [x] 配置 unity-developer skill

---

## 项目结构备注

```
My/                          # Unity 项目根目录
├── .kilo/                   # Kilo 配置目录
│   └── skill/
│       └── unity-developer/ # Unity 开发专用 Skill
│           └── SKILL.md
├── Assets/
│   ├── Scripts/
│   │   ├── UIManager.cs
│   │   ├── UIEventManager.cs
│   │   ├── DeviceAdapter.cs
│   │   ├── UISceneGenerator.cs
│   │   └── UIPerformanceOptimizer.cs
│   └── UI/
│       └── UILayoutConfig.cs
├── Packages/
├── ProjectSettings/
└── MEMORY.md                # 本记忆文档
```

---

## Spec-Coding 规范

当进行重大改动时，遵循以下流程：

1. **需求规格** - 明确定义要实现的功能和约束条件
2. **技术方案** - 设计架构、接口和数据流
3. **变更范围** - 列出所有受影响的文件和模块
4. **实现步骤** - 分步骤编写代码
5. **验证标准** - 定义测试和验收标准
6. **记忆更新** - 完成后更新本文档的时间线和TODO列表
