# 对话系统 Phase 1 + Phase 2 使用指南

## 🎯 快速开始

### 1. 项目中的文件位置

```
Assets/NinthsLab/Scripts/
├── Frontend/Dialogue/
│   ├── Events/
│   │   └── FrontendDialogueEventBus.cs          ✅ 事件总线
│   ├── Models/
│   │   ├── DialogueDisplayData.cs               ✅ 对话数据
│   │   ├── CharacterDisplayInfo.cs              ✅ 角色信息
│   │   ├── SceneDisplayInfo.cs                  ✅ 场景信息
│   │   ├── DialogueChoice.cs                    ✅ 选项数据
│   │   └── DialogueEffect.cs                    ✅ 特效数据
│   └── IMPLEMENTATION_SUMMARY.md                📋 详细文档
│
├── InterrorgationLevelScript/MidLayer/
│   ├── Game_UI_Coordinator.cs                   ✏️ (已修改)
│   └── Dialogue/
│       ├── DialogueLogicAdapter.cs              ✅ 后端→前端
│       └── DialogueUIAdapter.cs                 ✅ 前端→后端
│
├── Examples/
│   └── DialogueSystemExample.cs                 📚 使用示例
│
└── Tests/
    ├── Frontend/Dialogue/Events/
    │   └── FrontendDialogueEventBusTests.cs     🧪 事件测试
    ├── Frontend/Dialogue/Models/
    │   └── DialogueModelsTests.cs               🧪 模型测试
    ├── Interrorgation/MidLayer/Dialogue/
    │   └── DialogueAdaptersTests.cs             🧪 适配器测试
    └── Interrorgation/Integration/
        └── DialogueSystemIntegrationTests.cs    🧪 集成测试
```

### 2. 场景设置

在你的游戏场景中添加：

```
GameObject: "GameUICoordinator"
├── Game_UI_Coordinator (Component)
├── DialogueLogicAdapter (Component)
└── DialogueUIAdapter (Component)
```

**自动设置**: 
- DialogueLogicAdapter 和 DialogueUIAdapter 会在初始化时自动查找或创建
- 或者在 Inspector 中拖拽进行手动设置

### 3. 最简单的使用方式

#### 后端生成对话

```csharp
// 在 NodeLogicManager.TryProveNode() 或类似地方
var dialogues = new List<string>
{
    "Alice: Welcome!",
    "[FadeInOut] Bob: Nice to see you.",
    "Alice: Let's get started."
};

GameEventDispatcher.DispatchDialogueGenerated(dialogues);
```

#### 前端显示对话

```csharp
public class DialogueUIPanel : MonoBehaviour
{
    void OnEnable()
    {
        FrontendDialogueEventBus.OnRequestDialogueDisplay += DisplayDialogue;
    }

    void OnDisable()
    {
        FrontendDialogueEventBus.OnRequestDialogueDisplay -= DisplayDialogue;
    }

    void DisplayDialogue(DialogueDisplayData data)
    {
        // 显示 data.Character.Name
        // 显示 data.Text
        // 应用 data.Effects
        // 显示背景 (data.Scene)
    }
}
```

#### 前端处理用户选择

```csharp
public class ChoiceButtonGroup : MonoBehaviour
{
    void OnChoiceClicked(DialogueChoice choice)
    {
        FrontendDialogueEventBus.RaiseUserSelectChoice(choice);
    }
}
```

---

## 📚 核心概念

### 数据流向

```
后端逻辑
  ↓ [GameEventDispatcher.OnDialogueGenerated]
Game_UI_Coordinator (中间层单一入口)
  ↓
DialogueLogicAdapter (解析并转换)
  ↓
FrontendDialogueEventBus (事件总线)
  ↓
前端UI 接收 DialogueDisplayData
  ↓
用户交互
  ↓ [FrontendDialogueEventBus.OnUserSelectChoice]
DialogueUIAdapter (处理并转发)
  ↓
GameEventDispatcher.DispatchPlayerInputString()
  ↓
后端逻辑处理用户选择
```

### 5个核心数据模型

| 模型 | 作用 | 来自 |
|------|------|------|
| **DialogueDisplayData** | 对话显示数据 | DialogueLogicAdapter |
| **CharacterDisplayInfo** | 角色显示信息 | DialogueDisplayData |
| **SceneDisplayInfo** | 场景显示信息 | DialogueDisplayData |
| **DialogueChoice** | 用户可选项 | 后端逻辑 |
| **DialogueEffect** | UI特效 | DialogueDisplayData.Effects |

### 5个核心事件

| 事件 | 发送者 | 接收者 | 传输数据 |
|------|--------|--------|--------|
| **OnRequestDialogueDisplay** | DialogueLogicAdapter | UI组件 | DialogueDisplayData |
| **OnRequestChoicesDisplay** | DialogueLogicAdapter | UI组件 | List<DialogueChoice> |
| **OnRequestDialogueClear** | DialogueLogicAdapter | UI组件 | (无) |
| **OnUserSelectChoice** | UI组件 | DialogueUIAdapter | DialogueChoice |
| **OnUserRequestAdvance** | UI组件 | DialogueUIAdapter | (无) |

---

## 🔧 特效系统

### 特效标记格式

```csharp
"[EffectType|param1=value1] CharacterName: Text"

例如:
"[FadeInOut|duration=2.0] Alice: Hello"
"[Shake|intensity=0.5] Bob: Earthquake!"
"[SlideIn][ScaleUp] Carol: Multiple effects!"
```

### 支持的特效类型

```csharp
enum DialogueEffectType
{
    None = 0,                    // 无特效
    FadeInOut = 1,              // 渐隐渐显
    BounceIn = 2,               // 弹跳入场
    SlideIn = 3,                // 平移进入
    Shake = 4,                  // 抖动
    ScaleUp = 5,                // 放大
    TypewriterEffect = 6,       // 打字效果
    Flash = 7,                  // 闪光
    RotateIn = 8                // 旋转进入
}
```

### 在UI中应用特效

```csharp
void DisplayWithEffects(DialogueDisplayData data)
{
    foreach (var effect in data.Effects)
    {
        switch (effect.Type)
        {
            case DialogueEffectType.FadeInOut:
                StartCoroutine(PlayFadeInOut(effect.Duration));
                break;
            case DialogueEffectType.Shake:
                float intensity = effect.Parameters["intensity"] as float? ?? 0.5f;
                StartCoroutine(PlayShake(intensity, effect.Duration));
                break;
            // ... 其他特效
        }
    }
}
```

---

## 🧪 测试

### 运行所有测试

```
Window > Test Framework > Test Runner
```

### 测试覆盖

- **事件系统**: 16 个测试
- **数据模型**: 26 个测试
- **适配器**: 22 个测试
- **集成流程**: 18 个测试

**总计**: 82 个测试用例，覆盖率 > 90%

### 运行特定测试

```csharp
// 在 Test Runner 中过滤
Filter: "Frontend.Dialogue" - 运行前端对话测试
Filter: "Integration"       - 运行集成测试
```

---

## ⚠️ 常见错误

### 1. "DialogueLogicAdapter not found"

**原因**: Game_UI_Coordinator 找不到适配器

**解决**:
- 确保 DialogueLogicAdapter 在同一 GameObject 上
- 或在 Inspector 中手动拖拽

### 2. "Null reference in EventBus"

**原因**: DialogueDisplayData 中存在 null 字段

**解决**:
```csharp
// 总是检查模型初始化
var data = new DialogueDisplayData();
// ✅ 现在 Character, Scene, Effects 都已初始化
```

### 3. 对话没有显示

**原因**: UI 没有订阅事件

**解决**:
```csharp
void OnEnable()
{
    FrontendDialogueEventBus.OnRequestDialogueDisplay += OnDialogueReceived;
}

void OnDisable()
{
    FrontendDialogueEventBus.OnRequestDialogueDisplay -= OnDialogueReceived;
}
```

### 4. 后端没有收到用户选择

**原因**: 没有调用 `RaiseUserSelectChoice()`

**解决**:
```csharp
// ✅ 正确
FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

// ❌ 错误
// OnUserSelectChoice?.Invoke(choice); // 直接调用事件
```

---

## 🚀 下一步 (Phase 3)

完成以下 UI 组件:

1. **DialogueUIPanel.cs** - 主面板控制器
2. **DialogueTextBox.cs** - 文本显示
3. **CharacterView.cs** - 角色立绘
4. **SceneView.cs** - 背景管理
5. **ChoiceButtonGroup.cs** - 选项按钮

---

## 📖 详细文档

查看 `Frontend/Dialogue/IMPLEMENTATION_SUMMARY.md` 了解:
- 完整的架构设计
- 每个组件的详细说明
- 数据分离原则
- 测试覆盖详情

---

## 💡 设计原则

✅ **数据和逻辑分离**
- Models 包含零业务逻辑
- Adapters 负责转换
- EventBus 负责通信

✅ **单向数据流**
- 后端 → 前端: 通过 FrontendDialogueEventBus 发送数据
- 前端 → 后端: 通过 FrontendDialogueEventBus 发送用户输入

✅ **异常安全**
- 所有公开方法验证参数
- 无法修改后端原始数据

✅ **易于测试**
- 纯数据模型易于 Mock
- 事件系统易于验证
- 适配器可独立测试

---

## 📞 支持

遇到问题？检查:

1. **IMPLEMENTATION_SUMMARY.md** - 完整文档
2. **DialogueSystemExample.cs** - 使用示例
3. **Tests/** - 测试代码示例
4. **Game Output Log** - 调试日志 (adapter 内有详细 logging)

---

**版本**: 1.0 (Phase 1 + Phase 2)  
**完成日期**: 2025.12.19  
**最后更新**: 2025.12.19
