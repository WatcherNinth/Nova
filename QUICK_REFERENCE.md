# Phase 1 + Phase 2 - 快速参考

## 📦 创建的文件总览

### Phase 1: 事件系统 + 数据模型 (6个文件)

#### 事件系统 (1个)
- `Frontend/Dialogue/Events/FrontendDialogueEventBus.cs` - 5个事件

#### 数据模型 (5个)
- `Frontend/Dialogue/Models/DialogueDisplayData.cs` - 对话数据
- `Frontend/Dialogue/Models/CharacterDisplayInfo.cs` - 角色信息
- `Frontend/Dialogue/Models/SceneDisplayInfo.cs` - 场景信息
- `Frontend/Dialogue/Models/DialogueChoice.cs` - 选项数据
- `Frontend/Dialogue/Models/DialogueEffect.cs` - 特效 + 枚举

### Phase 2: 适配器 + 协调器 (3个文件)

#### 适配器 (2个)
- `InterrorgationLevelScript/MidLayer/Dialogue/DialogueLogicAdapter.cs` - 后端→前端
- `InterrorgationLevelScript/MidLayer/Dialogue/DialogueUIAdapter.cs` - 前端→后端

#### 协调器 (1个修改)
- `InterrorgationLevelScript/MidLayer/Game_UI_Coordinator.cs` - 集成适配器

### 测试文件 (4个)
- `Tests/Frontend/Dialogue/Events/FrontendDialogueEventBusTests.cs` - 16个测试
- `Tests/Frontend/Dialogue/Models/DialogueModelsTests.cs` - 26个测试
- `Tests/Interrorgation/MidLayer/Dialogue/DialogueAdaptersTests.cs` - 22个测试
- `Tests/Interrorgation/Integration/DialogueSystemIntegrationTests.cs` - 18个测试

### 示例和文档
- `Examples/DialogueSystemExample.cs` - 5个使用示例
- `Frontend/Dialogue/README.md` - 快速开始指南
- `Frontend/Dialogue/IMPLEMENTATION_SUMMARY.md` - 详细文档
- `PHASE_1_2_COMPLETION_REPORT.md` - 完成报告

---

## 🔑 核心API速查

### 发送事件 (后端→前端)

```csharp
// 发送对话
FrontendDialogueEventBus.RaiseRequestDialogueDisplay(DisplayData);

// 发送选项
FrontendDialogueEventBus.RaiseRequestChoicesDisplay(List<Choice>);

// 清除UI
FrontendDialogueEventBus.RaiseRequestDialogueClear();
```

### 订阅事件 (前端监听)

```csharp
FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => {};
FrontendDialogueEventBus.OnRequestChoicesDisplay += (choices) => {};
FrontendDialogueEventBus.OnRequestDialogueClear += () => {};
```

### 前端→后端

```csharp
// 用户选择选项
FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

// 用户要求推进
FrontendDialogueEventBus.RaiseUserRequestAdvance();
```

### 后端订阅用户输入

```csharp
FrontendDialogueEventBus.OnUserSelectChoice += (choice) => {};
FrontendDialogueEventBus.OnUserRequestAdvance += () => {};
```

---

## 📊 数据模型速查

### DialogueDisplayData
```csharp
{
    Character: CharacterDisplayInfo,     // 角色
    Scene: SceneDisplayInfo,             // 场景
    Text: string,                        // 对话文本
    Effects: List<DialogueEffect>,       // 特效列表
    IsAutoAdvance: bool,                 // 自动推进?
    AutoAdvanceDelay: float,             // 推进延迟
    SourceLineIndex: int                 // 原始行号
}
```

### CharacterDisplayInfo
```csharp
{
    Id: string,                          // 角色ID
    Name: string,                        // 角色名
    SpriteResourcePath: string,          // 立绘路径
    Position: CharacterPosition,         // 屏幕位置
    IsVisible: bool,                     // 是否显示
    Alpha: float,                        // 透明度
    Scale: float                         // 缩放
}
```

### DialogueChoice
```csharp
{
    Id: string,                          // 选项ID
    DisplayText: string,                 // 显示文本
    TargetPhaseId: string,               // 目标阶段
    IsDisabled: bool,                    // 禁用?
    DisabledReason: string,              // 禁用原因
    Priority: int,                       // 优先级
    Metadata: Dictionary                 // 元数据
}
```

### DialogueEffect
```csharp
{
    Type: DialogueEffectType,            // 特效类型
    Duration: float,                     // 持续时间
    PlayOnShow: bool,                    // 显示时播放?
    Parameters: Dictionary               // 特效参数
}
```

---

## ⚡ 最常见的用法

### 场景1: 显示对话

```csharp
// 后端 (NodeLogicManager)
var lines = new List<string> {
    "[FadeInOut] Alice: Welcome!",
    "Bob: Hello there."
};
GameEventDispatcher.DispatchDialogueGenerated(lines);

// 前端 (DialogueUIPanel)
FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => {
    ShowCharacter(data.Character);
    PlayEffects(data.Effects);
    DisplayText(data.Text);
};
```

### 场景2: 显示选项

```csharp
// 后端
var choices = new List<(string, string)> {
    ("choice_1", "Accept"),
    ("choice_2", "Decline")
};
logicAdapter.ProcessChoices(choices, "phase_2");

// 前端
FrontendDialogueEventBus.OnRequestChoicesDisplay += (choices) => {
    foreach (var choice in choices) {
        CreateButton(choice);
    }
};
```

### 场景3: 处理用户选择

```csharp
// 前端 (ChoiceButtonGroup)
void OnChoiceClicked(DialogueChoice choice) {
    FrontendDialogueEventBus.RaiseUserSelectChoice(choice);
}

// 中间层 (DialogueUIAdapter)
FrontendDialogueEventBus.OnUserSelectChoice += (choice) => {
    ClearDialogue();
    SendToBackend(choice.Id);
};
```

---

## 🎨 特效标记快速参考

### 格式
```
[EffectType|param=value] CharacterName: Text
```

### 常见用法
```
[FadeInOut] Alice: 淡入淡出
[Shake] Bob: 抖动
[ScaleUp] Carol: 放大
[SlideIn] Dave: 平移进入
[TypewriterEffect] Eve: 打字效果

[FadeInOut|duration=2.0] Alice: 2秒淡入
[Shake|intensity=0.5] Bob: 低强度抖动
[FadeInOut][Shake] Carol: 组合特效
```

### 所有特效类型
```
None, FadeInOut, BounceIn, SlideIn, Shake,
ScaleUp, TypewriterEffect, Flash, RotateIn
```

---

## 🧪 运行测试

```
# 在Unity中
Window > Test Framework > Test Runner > Run All

# 或命令行
unity -projectPath . -runTests

# 过滤测试
Filter: "Frontend.Dialogue" → 前端对话测试
Filter: "Integration" → 集成测试
```

---

## ⚠️ 常见陷阱

| 问题 | 原因 | 解决 |
|------|------|------|
| 事件没有触发 | 没有订阅 | 在 OnEnable 中订阅 |
| 空指针异常 | 模型未初始化 | `new DialogueDisplayData()` 自动初始化 |
| 后端没收到选择 | 没有调用 Raise 方法 | 检查 `RaiseUserSelectChoice()` 调用 |
| 适配器找不到 | Game_UI_Coordinator 初始化失败 | 检查同一 GameObject 或 Inspector 设置 |
| 特效不工作 | 标记格式错误 | 检查 `[Type]` 大小写和参数格式 |

---

## 📈 代码统计

| 类别 | 数量 |
|------|------|
| 生产代码文件 | 10 |
| 生产代码行数 | ~1,250 |
| 测试文件 | 4 |
| 测试用例 | 82 |
| 测试代码行数 | ~800 |
| 文档文件 | 3 |
| 文档行数 | ~1,200 |
| **总计** | **~3,250** |

---

## ✅ 验收检查表

使用此清单验证实现:

```
□ 编译无错误: No Errors, No Warnings
□ 所有测试通过: 82/82 tests pass
□ 事件系统完整: 5个事件都能发送和接收
□ 数据模型完整: 5个模型都正确初始化
□ 适配器工作: 双向转换都正确
□ 协调器集成: Game_UI_Coordinator 初始化无误
□ 文档完整: README + SUMMARY + Example 都有
□ 向后兼容: UIEventDispatcher 路径还能用
```

---

## 🚀 下一步 (Phase 3)

实现 5 个 UI 组件:

```
Frontend/Dialogue/UI/
├── DialogueUIPanel.cs        (主控制器)
├── DialogueTextBox.cs        (文本显示)
├── CharacterView.cs          (立绘管理)
├── SceneView.cs              (背景管理)
└── ChoiceButtonGroup.cs      (选项按钮)
```

---

## 📞 快速查找

需要...？查看...

| 需要 | 查看 |
|------|------|
| API 使用方式 | README.md |
| 完整设计说明 | IMPLEMENTATION_SUMMARY.md |
| 实际代码示例 | DialogueSystemExample.cs |
| 测试示例 | Tests/** 目录 |
| 完成度报告 | PHASE_1_2_COMPLETION_REPORT.md |
| 特定类的代码 | 对应 .cs 文件 |

---

**版本**: 1.0  
**状态**: ✅ 完成且编译通过  
**最后更新**: 2025.12.19
