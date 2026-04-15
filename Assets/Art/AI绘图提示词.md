# 钟山下 — AI 绘图提示词手册

## 一、角色设定参考

### 女主角（Player）
- 长棕色波浪卷发，戴圆框眼镜
- 米色/卡其色针织开衫，内搭蓝灰色连衣裙（白色条纹装饰）
- 蓝色蝴蝶结领带
- 棕色斜挎小方包
- 白色短袜 + 棕色小皮鞋
- 气质：文静、温柔、书卷气

### 男配角（NPC 同学）
- 棕色短发，微卷，自然蓬松
- 米色/卡其色连帽卫衣
- 深蓝色牛仔裤
- 帆布运动鞋
- 手持书本
- 气质：阳光、随和、邻家男孩

---

## 二、技术规格

### 精灵表格式要求
| 项目 | 规格 |
|------|------|
| 排列方式 | **2×2 网格**（2列 × 2行） |
| 单帧尺寸 | **1000 × 1000 px** |
| 总图尺寸 | **2000 × 2000 px** |
| 背景 | **透明 (PNG-24 + Alpha)** |
| 帧编号 | 左上=帧0, 右上=帧1, 左下=帧2, 右下=帧3 |
| 文件命名 | `PlayerWalkSprites.png`（替换现有文件） |

### 如需更多帧（推荐 6~8 帧）
| 项目 | 规格 |
|------|------|
| 排列方式 | **4×2 网格**（4列 × 2行） |
| 单帧尺寸 | **1000 × 1000 px** |
| 总图尺寸 | **4000 × 2000 px** |
| 代码改动 | 需在 Unity 中重新切片 Sprite Editor |

### 角色在帧中的位置
- 角色居中，脚底对齐到帧底部约 10% 高度处
- 角色高度占帧高度的 **75~85%**
- 左右留出足够空白，确保动态动作不被裁切

---

## 三、Idle 动画提示词

### 🎯 核心 Idle 提示词（4帧呼吸循环）

**英文版（推荐用于 Midjourney / Stable Diffusion / DALL-E）：**

```
2D game character sprite sheet, 2x2 grid layout, 4 frames of idle breathing animation.

Character: young college girl, long wavy brown hair, round glasses, beige cardigan over blue-gray pleated dress with white stripe trim, blue bow tie at collar, brown leather crossbody satchel bag, white ankle socks, brown leather shoes.

Style: anime chibi / super-deformed (SD), large head-to-body ratio approximately 2.5:1, cute and rounded proportions, soft cel-shading, clean outlines, game asset style.

Animation cycle: subtle breathing loop.
- Frame 1 (top-left): neutral standing pose, arms relaxed at sides
- Frame 2 (top-right): very slight inhale, body rises up ~2px, hair has minimal movement
- Frame 3 (bottom-left): full inhale position, slight shoulder raise, hair sways gently to the right
- Frame 4 (bottom-right): exhale returning to neutral, hair sways back to center

Facing direction: 3/4 front-facing, looking slightly to the right.
Background: transparent (checkerboard).
Each frame is exactly 1000x1000 pixels, character centered in each cell.
Consistent character proportions, coloring, and line weight across all 4 frames.
Minimal but visible frame-to-frame differences for smooth looping animation.
```

**中文版（用于通义万相 / 即梦 / LiblibAI 等）：**

```
2D游戏角色精灵表，2×2网格排列，4帧待机呼吸动画。

角色：大学女生，长棕色波浪卷发，圆框眼镜，米色针织开衫搭配蓝灰色百褶连衣裙（白色条纹装饰），蓝色蝴蝶结领带，棕色斜挎小皮包，白色短袜，棕色小皮鞋。

风格：Q版/二头身半动漫风格，头身比约2.5:1，可爱圆润比例，柔和赛璐珞着色，干净线条，游戏素材风格。

动画循环：微妙的呼吸循环。
- 帧1（左上）：自然站立，双手放松垂于身侧
- 帧2（右上）：微微吸气，身体略微上升，头发轻微晃动
- 帧3（左下）：吸气顶点，肩膀微抬，头发向右轻摆
- 帧4（右下）：呼气回到自然状态，头发回摆

朝向：3/4正面微侧，略朝向右方。
背景：透明。
每帧精确1000×1000像素，角色在每个格子中居中。
保持4帧之间角色比例、颜色、线条粗细完全一致。
帧间差异微小但可见，确保循环动画流畅。
```

---

## 四、Walk 动画提示词

### 🚶 行走动画（建议单独生成一张精灵表）

**英文版：**

```
2D game character sprite sheet, 4x2 grid layout, 8 frames of side-view walk cycle animation.

Character: young college girl, long wavy brown hair, round glasses, beige cardigan over blue-gray pleated dress with white stripe trim, blue bow tie at collar, brown leather crossbody satchel bag, white ankle socks, brown leather shoes.

Style: anime chibi / super-deformed (SD), large head-to-body ratio approximately 2.5:1, cute and rounded proportions, soft cel-shading, clean outlines, game asset style.

Animation cycle: 8-frame walk cycle, side view (facing right).
- Frame 1: contact pose, right foot forward, left foot back
- Frame 2: right foot passes under body (down position)
- Frame 3: right foot pushes off, left leg swings forward
- Frame 4: left foot passing, both feet close together (passing position)
- Frame 5: left foot forward contact, right foot back
- Frame 6: left foot passes under body (down position)
- Frame 7: left foot pushes off, right leg swings forward
- Frame 8: right foot passing, both feet close together (passing position)

Hair and skirt should have secondary motion (slight bounce/sway with each step).
Bag swings gently with the walking rhythm.
Background: transparent.
Each frame 1000x1000 pixels, character centered, feet aligned at consistent ground line.
```

**中文版：**

```
2D游戏角色精灵表，4×2网格排列，8帧侧面行走循环动画。

角色：大学女生，长棕色波浪卷发，圆框眼镜，米色针织开衫搭配蓝灰色百褶连衣裙，蓝色蝴蝶结领带，棕色斜挎小皮包，白色短袜，棕色小皮鞋。

风格：Q版/二头身半动漫风格，头身比约2.5:1，可爱圆润比例，游戏素材风格。

动画循环：8帧行走循环，侧面视角（面朝右）。
- 帧1：右脚向前着地，左脚在后（接触姿态）
- 帧2：重心下移，右脚在身体下方
- 帧3：右脚蹬地，左腿向前摆动
- 帧4：双脚交叉经过中间位置
- 帧5：左脚向前着地，右脚在后
- 帧6：重心下移，左脚在身体下方
- 帧7：左脚蹬地，右腿向前摆动
- 帧8：双脚交叉回到中间位置

头发和裙摆随步伐有轻微飘动/弹跳的二次运动。
挎包随行走节奏轻微摆动。
背景：透明。
每帧1000×1000像素，角色居中，脚底对齐在统一地面线上。
```

---

## 五、NPC 同学 Idle 提示词

**英文版：**

```
2D game character sprite sheet, 2x2 grid layout, 4 frames of idle breathing animation.

Character: young college boy, short messy brown hair, slightly curly, beige/khaki pullover hoodie with front pocket, dark blue denim jeans, canvas sneakers, holding a book in left hand at his side.

Style: anime chibi / super-deformed (SD), large head-to-body ratio approximately 2.5:1, cute and rounded proportions, soft cel-shading, clean outlines, game asset style. Must match the same art style as the female protagonist.

Animation cycle: subtle idle loop.
- Frame 1: neutral standing, relaxed posture
- Frame 2: slight weight shift, barely noticeable lean
- Frame 3: micro head tilt, hair sways slightly
- Frame 4: returns to neutral position

Facing direction: 3/4 front-facing, looking slightly to the left (mirroring the female character).
Background: transparent.
Each frame 1000x1000 pixels.
Consistent proportions across all frames.
```

---

## 六、跨动画一致性保障方案

> **核心问题**：AI 每次生成都有随机性，Idle、Walk、NPC 等不同精灵表之间
> 角色的脸型、身材比例、颜色、线条风格很容易跑偏。

### 🔒 策略一：锁定「角色设定图」作为唯一参考源

**不管用哪个 AI 工具，每次生成时都必须上传同一张参考图。**

推荐的参考图上传方案：
```
参考图1（必传）: Resources/PlayerWalkSprites.png 中的帧0（左上角那帧）
                 —— 这是 Q版比例的「基准姿态」
参考图2（可选）: Resources/PlayerSprite.png
                 —— 这是全身立绘，辅助 AI 理解服装细节
```

> 每次生成新动画时，提示词中加入：
> "The character must look identical to the reference image.
>  Same face, same proportions, same clothing colors, same line weight."

### 🔒 策略二：固定「风格锚定词」

在所有提示词中**始终保留**这段不变的风格描述，一个字都不要改：

```
【风格锚定段（复制粘贴到每个提示词中）】

Style anchor: anime chibi, super-deformed, head-to-body ratio 2.5:1,
soft cel-shading, 2px black outlines, pastel color palette,
flat shadows with minimal gradient, game sprite asset,
white highlight in eyes, rounded face shape, small nose dot.

Color key:
- Hair: #5C3D2E warm brown
- Cardigan: #C4A882 beige
- Dress: #8FA4B8 blue-gray
- Bow tie: #4A6FA5 blue
- Bag: #7B5B3A brown leather
- Socks: #FFFFFF white
- Shoes: #5C3828 dark brown
- Skin: #F5E0D0 light peach
- Eyes: #6B4226 brown
- Glasses frame: #3D3D3D dark gray
```

> **为什么要写 hex 色值？** AI 对 "beige" "brown" 这类模糊词的理解每次不同，
> 但精确色号能大幅收敛颜色的一致性范围。

### 🔒 策略三：逐帧生成 → 手动拼合（最可控）

**不要一次性让 AI 生成完整精灵表**，而是逐帧生成：

```
工作流程：
1. 生成「帧0 - 基准站姿」 ← 反复调整直到满意
2. 以帧0的输出图作为参考图，生成「帧1」← 只改变姿态描述
3. 以帧0的输出图作为参考图，生成「帧2」← 同上
4. 以帧0的输出图作为参考图，生成「帧3」← 同上
   ⚠️ 注意：始终用帧0作参考，不要用帧1去生成帧2，
   否则误差会像传话游戏一样逐帧累积放大。
5. 在 Photoshop/GIMP 中手动拼合成精灵表
```

逐帧生成时的提示词模板：
```
[上传帧0作为参考图]

Same character as reference image. Exact same art style, proportions,
colors, and line weight. Only change the pose:

Pose: [这里描述当前帧的姿势]

Single character, centered, transparent background.
1000x1000 pixels.
```

### 🔒 策略四：后期统一校色

即使以上步骤都做了，不同帧之间的颜色仍可能有轻微偏差。
最后在 Photoshop/GIMP 中做统一处理：

```
后期校色流程：
1. 将所有帧排列对比 → 找出颜色最准确的一帧作为「标准帧」
2. 用「匹配颜色」(Image → Adjustments → Match Color) 将其他帧向标准帧对齐
3. 检查并手动调整：
   - 皮肤色调是否统一
   - 头发颜色是否一致
   - 眼镜框颜色是否跑偏
   - 服装各部分的明度/饱和度
4. 统一线条：如果某些帧的描边粗细不同，用 Filter → Other → Minimum/Maximum 微调
5. 确认角色身高一致：在 Photoshop 中叠加对比每帧的头顶到脚底高度
```

### 🔒 策略五：为不同动画建立「关联提示词」

生成 Walk 动画时，显式声明它与 Idle 是同一角色：

```
This is the WALK animation for the same character whose IDLE animation
is shown in the reference image. The character must be absolutely
identical — same face, same outfit, same proportions, same art style.
Only the pose and leg movement should differ.
```

生成 NPC 时，声明它与 Player 是同一画风：

```
This character exists in the same game as the reference character.
They must share the exact same art style: same chibi proportions
(2.5:1 head-to-body), same line weight, same shading technique,
same level of detail. Only the character design (hair, clothing) differs.
```

---

## 七、Krea.ai 专用技巧

### 基本用法
1. 上传 `PlayerWalkSprites.png` 的帧0截图 作为 Style Reference
2. 在提示词中使用英文版本（Krea 对英文支持更好）
3. 调节 AI Strength/Creativity 为中等偏低（40~60%），保持与参考图的相似度

### 推荐工作流
```
Step 1: Enhance 模式上传帧0 → 微调确认 Krea 能复现你的角色风格
Step 2: Generate 模式 → 逐帧生成 Idle 的其他帧
Step 3: 每帧都上传帧0作为参考，只改 pose 描述
Step 4: 拼合成精灵表
```

---

## 八、各 AI 工具一致性对比

| 工具 | 一致性控制能力 | 推荐度 | 关键技巧 |
|------|-------------|--------|---------|
| **Krea.ai** | ★★★☆ 中等 | ✅ 你当前的选择 | 上传参考图 + 低创意度 |
| **Midjourney** | ★★★★ 较强 | ✅ 推荐 | `--cref` 角色参考 + `--sref` 风格参考 |
| **Stable Diffusion** | ★★★★★ 最强 | ✅ 最佳（但门槛高） | 训练 LoRA + ControlNet 控制姿势 |
| **ChatGPT/DALL-E** | ★★☆☆ 较弱 | ⚠️ 适合原型 | 同一对话连续生成 + 上传参考图 |
| **通义万相** | ★★★☆ 中等 | ⚠️ 中文友好 | 角色参考 + 风格一致性选项 |

> **终极建议**：如果你追求高一致性又不想折腾 Stable Diffusion，
> 考虑用 **Midjourney** 的 `--cref` 功能，它是目前最平衡「易用性 vs 一致性」的方案。

---

## 九、生成后处理清单

1. **检查透明背景** — 确保 PNG 导出时保留 Alpha 通道
2. **统一帧尺寸** — 每帧必须精确 1000×1000 px
3. **对齐脚底线** — 所有帧的角色脚底在同一水平线上
4. **检查一致性** — 角色大小、颜色、线条粗细在所有帧间保持一致
5. **去除多余元素** — 确保无水印、无文字、无背景残留
6. **拼合精灵表** — 按网格排列拼合成一张大图
7. **导入 Unity** — 替换 `Resources/PlayerWalkSprites.png`
8. **Unity 设置** —
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: **Multiple**
   - Filter Mode: **Point (no filter)**（保持像素清晰）
   - 打开 Sprite Editor → 切片为 4/8 帧
   - 切片命名格式: `PlayerIdle_0`, `PlayerIdle_1`, ...

---

## 十、文件替换对照表

| 用途 | 当前文件 | 替换文件名 | 帧数 |
|------|----------|------------|------|
| 玩家 Idle | `Resources/PlayerWalkSprites.png` | 同名替换 | 4帧 |
| 玩家 Walk | 暂无（与Idle共用） | `Resources/PlayerWalkSprites_Walk.png` (新建) | 6~8帧 |
| 玩家立绘 | `Resources/PlayerSprite.png` | 同名替换（如需更新） | 1帧 |
| NPC Idle | 暂无 | `Resources/NPCWalkSprites.png` (新建) | 4帧 |
| NPC 立绘 | `Resources/NPCSprite.png` | 同名替换（如需更新） | 1帧 |

> **注意**: 如果行走动画单独一张精灵表，需要修改 `PlayerController.cs` 中的 `ConfigureAnimations()` 方法，让 Walk 动画从新的资源路径加载。
