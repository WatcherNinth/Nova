# 对话系统测试指南

## 快速测试步骤

### 1️⃣ 准备资源文件

在 `Assets/Resources/Characters/` 下创建角色文件夹：

```
Resources/
└── Characters/
    ├── 安·李/
    │   ├── default.png      ← 必需
    │   ├── happy.png        ← 可选
    │   ├── sad.png          ← 可选
    │   ├── angry.png        ← 可选
    │   ├── serious.png      ← 可选
    │   ├── surprised.png    ← 可选
    │   └── determined.png   ← 可选
    └── 安乔/
        ├── default.png      ← 必需
        ├── happy.png        ← 可选
        ├── sad.png          ← 可选
        ├── angry.png        ← 可选
        └── surprised.png    ← 可选
```

**没有资源也能测试**，只是看不到立绘图片（会在Console显示警告）。

---

### 2️⃣ 配置场景

#### 场景中必需的组件

确保你的场景中已配置以下组件（参考 PHASE_3_UI_GUIDE.md）：

1. **Game_UI_Coordinator** - 中间层协调器
2. **DialogueUIPanel** - 对话UI主面板
3. **DialogueLogicAdapter** - 适配器（挂载在Coordinator上）

如果还没有配置，请先按照 PHASE_3_UI_GUIDE.md 搭建UI。

---

### 3️⃣ 添加测试脚本

1. 在场景中创建空GameObject，命名为 `DialogueTester`
2. 挂载脚本：`Demo_V2_DialogueTester.cs`
3. Inspector中选择测试场景（默认Basic）

---

### 4️⃣ 运行测试

#### 方法A：按键触发

1. 点击Unity的 **Play** 按钮运行游戏
2. 按 **空格键** 触发测试对话
3. 观察对话是否正确显示

#### 方法B：Inspector按钮

1. 运行游戏
2. 选中 `DialogueTester` GameObject
3. 在Inspector中点击 **🎬 测试: Basic** 按钮

---

## 测试场景说明

### Basic - 基础对话测试

测试内容：
- ✅ 多角色对话（安·李、安乔）
- ✅ 双换行符分割段落
- ✅ 自动显示立绘
- ✅ 旁白显示（无立绘）

预期结果：
- 对话依次显示
- 角色立绘自动加载（如有资源）
- 打字机效果播放
- 点击推进到下一条对话

---

### WithExpressions - 表情测试

测试内容：
- ✅ `[立绘:happy]` 标记
- ✅ `[立绘:sad]` 标记
- ✅ `[立绘:angry]` 标记
- ✅ 表情切换

预期结果：
- 角色立绘根据标记切换表情
- 资源路径：`Characters/安乔/happy.png`

---

### OffScreen - 画面外对话

测试内容：
- ✅ `[画面外]` 标记
- ✅ 有角色名但不显示立绘

预期结果：
- 显示角色名
- 显示对话文本
- **不显示立绘**

---

### HideSprite - 隐藏立绘

测试内容：
- ✅ `[隐藏立绘]` 标记
- ✅ 清除所有立绘

预期结果：
- 执行标记后，所有立绘隐藏
- 只显示旁白文本
- 下一条对话重新显示立绘

---

### Mixed - 综合测试

测试内容：
- ✅ 所有功能混合使用
- ✅ 真实使用场景模拟

---

## 检查清单

### ✅ 对话显示正常

- [ ] 角色名正确显示
- [ ] 对话文本正确显示
- [ ] 打字机效果播放
- [ ] 点击可以推进对话

### ✅ 立绘显示正常

- [ ] 立绘图片加载成功
- [ ] 立绘位置正确（居中）
- [ ] 淡入动画播放
- [ ] 表情切换正常

### ✅ 特殊标记工作

- [ ] `[立绘:表情名]` 能切换表情
- [ ] `[画面外]` 不显示立绘
- [ ] `[隐藏立绘]` 清除立绘
- [ ] 旁白不显示立绘

---

## 常见问题排查

### 问题1: 对话没有显示

**原因**：
- Scene中缺少必需组件
- DialogueUIPanel未配置

**解决**：
1. 检查Console是否有错误日志
2. 确认 `Game_UI_Coordinator` 在场景中
3. 确认 `DialogueLogicAdapter` 已挂载
4. 按照 PHASE_3_UI_GUIDE.md 重新配置

---

### 问题2: 立绘无法加载

**原因**：
- 资源路径不对
- 资源文件名不对
- Resources文件夹位置不对

**解决**：
1. 检查Console警告：`[CharacterView] 未找到立绘资源: Characters/XXX/XXX`
2. 确认文件夹名与角色名**完全一致**（区分大小写）
3. 确认在 `Assets/Resources/Characters/` 下
4. 确认文件格式为 `.png`

---

### 问题3: 表情切换失败

**原因**：
- 表情文件不存在
- 文件名与标记不匹配

**解决**：
1. 检查文件名：`[立绘:happy]` → 需要 `happy.png`
2. 如果文件不存在，会回退到 `default.png`

---

### 问题4: 特殊标记不生效

**原因**：
- 标记格式错误
- 标记位置不对

**解决**：
- `[画面外]` 必须在行首
- `[立绘:表情]` 必须在行首
- `[隐藏立绘]` 必须在行首
- 标记后面的内容按正常格式解析

---

## 调试技巧

### 启用调试日志

在Inspector中勾选 `DialogueLogicAdapter.debugLogging = true`

**日志示例**：
```
[DialogueLogicAdapter] 处理对话 (3 段)
[DialogueLogicAdapter] → 安·李: 让我们继续先前的假设...
[DialogueLogicAdapter] → 安乔: 嗯……
[CharacterView] 成功加载立绘: Characters/安·李/default
```

---

### 测试真实JSON格式

点击Inspector中的 **📜 测试真实JSON格式** 按钮，测试从实际demo_v2.json提取的对话格式。

---

## 下一步

### 与真实游戏流程集成

测试通过后，你的游戏逻辑（如 `NodeLogicManager.TryProveNode()`）会自动调用：

```csharp
GameEventDispatcher.DispatchDialogueGenerated(dialogues);
```

对话会自动显示到UI上，**无需额外代码**！

---

## 快速测试命令

```csharp
// 在任何脚本中调用，立即显示测试对话
var dialogues = new List<string>
{
    "安·李：\n你好。",
    "[立绘:happy]安乔：\n太好了！"
};
GameEventDispatcher.DispatchDialogueGenerated(dialogues);
```

---

## 总结

**最简单的测试步骤**：
1. ✅ 准备立绘资源（或跳过，只测试文本）
2. ✅ 挂载 `Demo_V2_DialogueTester` 到场景
3. ✅ 运行游戏，按空格键
4. ✅ 观察对话显示效果

**所有测试都可以在没有立绘资源的情况下进行，立绘只是额外的视觉效果！**
