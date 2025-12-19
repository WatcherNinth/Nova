# Phase 3 UI组件使用指南

## ✅ 已完成的5个UI组件

### 1. DialogueUIPanel.cs - 主面板控制器
**功能**: 管理所有对话UI子组件，协调显示流程

**关键方法**:
- `OnDialogueDisplayRequested()` - 接收对话显示请求
- `DisplayNextDialogue()` - 显示下一条对话
- `OnUserClickAdvance()` - 用户推进对话
- `ClearDialogue()` - 清除所有对话UI

### 2. DialogueTextBox.cs - 文本显示
**功能**: 显示角色名和对话文本，支持打字机效果

**关键功能**:
- ✅ 打字机效果 (可配置速度)
- ✅ 点击跳过动画
- ✅ 点击推进对话
- ✅ 显示点击提示图标

**配置参数**:
- `typewriterSpeed` - 打字速度 (字符/秒)
- `allowSkipAnimation` - 是否允许跳过
- `clickIndicator` - 点击提示图标

### 3. CharacterView.cs - 立绘显示
**功能**: 显示角色立绘，支持淡入淡出和缩放

**关键功能**:
- ✅ 自动加载立绘资源 (从Resources文件夹)
- ✅ 淡入淡出动画
- ✅ 缩放动画
- ✅ 透明度控制

**资源路径规则**:
```
Resources/Characters/{CharacterName}/{expression}.png

例如:
Resources/Characters/Alice/default.png
Resources/Characters/Alice/happy.png
Resources/Characters/Bob/default.png
```

### 4. SceneView.cs - 背景管理
**功能**: 显示和切换场景背景，控制光照

**关键功能**:
- ✅ 背景切换动画 (交叉淡化)
- ✅ 光照强度控制
- ✅ 背景透明度控制

**资源路径规则**:
```
Resources/Scenes/{SceneName}/{time}.png

例如:
Resources/Scenes/Office/day.png
Resources/Scenes/Office/night.png
```

### 5. ChoiceButtonGroup.cs - 选项按钮
**功能**: 动态生成选项按钮，处理用户选择

**关键功能**:
- ✅ 动态生成按钮
- ✅ 按优先级排序
- ✅ 禁用状态支持
- ✅ 逐个淡入动画

---

## 🎨 Unity场景设置

### Hierarchy结构示例

```
Canvas
└── DialogueUIPanel
    ├── Background (可选的黑色半透明背景)
    ├── SceneView (场景背景)
    │   └── BackgroundImage (Image)
    ├── CharacterView (角色立绘)
    │   └── CharacterImage (Image)
    ├── TextBox (文本框)
    │   ├── CharacterName (TextMeshProUGUI)
    │   ├── DialogueContent (TextMeshProUGUI)
    │   ├── ClickIndicator (Image - 箭头图标)
    │   └── ClickArea (Button - 透明，覆盖整个文本框)
    └── ChoiceButtonGroup (选项组)
        └── ButtonContainer (Vertical Layout Group)
```

### Inspector配置

#### DialogueUIPanel 组件设置:
```
[子组件引用]
- Text Box: 拖入 TextBox
- Character Views: 拖入 CharacterView (可多个)
- Scene View: 拖入 SceneView
- Choice Button Group: 拖入 ChoiceButtonGroup

[显示控制]
- Auto Hide: true (无对话时自动隐藏)
- Panel Canvas Group: 拖入自身的 CanvasGroup

[调试]
- Debug Logging: true (开发时打开)
```

#### DialogueTextBox 组件设置:
```
[UI引用]
- Character Name Text: 拖入角色名 TextMeshProUGUI
- Dialogue Content Text: 拖入对话内容 TextMeshProUGUI
- Click Indicator: 拖入点击提示图标 GameObject
- Click Area Button: 拖入透明 Button组件

[打字机效果]
- Enable Typewriter Effect: true
- Typewriter Speed: 30 (字符/秒)
- Allow Skip Animation: true
```

#### CharacterView 组件设置:
```
[UI引用]
- Character Image: 拖入 Image 组件
- Canvas Group: 自动添加

[位置配置]
- Assigned Position: Center (或 Left/Right)

[动画设置]
- Fade Duration: 0.3
- Scale Duration: 0.2

[资源加载]
- Resource Base Path: "Characters/"
```

#### ChoiceButtonGroup 组件设置:
```
[UI引用]
- Button Container: 拖入布局容器
- Choice Button Prefab: 拖入按钮预制体
- Canvas Group: 自动添加

[按钮样式]
- Normal Color: White
- Disabled Color: Gray
- Hover Color: Light Blue

[动画设置]
- Button Fade In Duration: 0.2
- Button Show Interval: 0.1 (逐个显示间隔)
```

---

## 🎭 预制体创建

### ChoiceButton 预制体

创建 `ChoiceButton.prefab`:

```
ChoiceButton (GameObject)
├── Button (Component)
├── Image (Component - 背景)
└── Text (TextMeshProUGUI - 显示选项文字)
```

**组件配置**:
- Button: Transition = Color Tint
- Image: 使用UI按钮素材
- TextMeshProUGUI: 居中对齐，字体大小适中

---

## 🚀 快速测试

### 1. 创建测试脚本

创建 `TestDialogueSystem.cs`:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class TestDialogueSystem : MonoBehaviour
{
    void Start()
    {
        // 等待1秒后测试
        Invoke(nameof(TestDialogue), 1f);
    }

    void TestDialogue()
    {
        // 生成测试对话
        var dialogues = new List<string>
        {
            "Alice: 欢迎来到调查室。",
            "Alice: 我们有一些线索需要讨论。",
            "Bob: 我发现了一些可疑的东西！",
            "Alice: 让我们仔细看看。"
        };

        // 触发对话显示
        GameEventDispatcher.DispatchDialogueGenerated(dialogues);
    }
}
```

### 2. 测试选项显示

```csharp
void TestChoices()
{
    var choices = new List<DialogueChoice>
    {
        new DialogueChoice
        {
            Id = "choice_1",
            DisplayText = "继续调查",
            TargetPhaseId = "phase_2",
            Priority = 10
        },
        new DialogueChoice
        {
            Id = "choice_2",
            DisplayText = "询问证人",
            TargetPhaseId = "phase_3",
            Priority = 20
        },
        new DialogueChoice
        {
            Id = "choice_3",
            DisplayText = "查看证据",
            TargetPhaseId = "phase_4",
            Priority = 5,
            IsDisabled = true,
            DisabledReason = "需要调查等级3"
        }
    };

    FrontendDialogueEventBus.RaiseRequestChoicesDisplay(choices);
}
```

---

## 📦 资源准备

### 必需的资源

1. **立绘图片** (放在 `Resources/Characters/` 下):
   ```
   Resources/
   └── Characters/
       ├── Alice/
       │   ├── default.png
       │   ├── happy.png
       │   └── sad.png
       └── Bob/
           ├── default.png
           └── surprised.png
   ```

2. **场景背景** (放在 `Resources/Scenes/` 下):
   ```
   Resources/
   └── Scenes/
       ├── Office/
       │   ├── day.png
       │   └── night.png
       └── Street/
           └── default.png
   ```

3. **UI素材**:
   - 对话框背景图
   - 按钮素材
   - 点击提示图标 (小箭头)

---

## 🔧 常见问题

### Q1: 对话不显示？
**检查**:
1. DialogueUIPanel 是否已添加到场景
2. 子组件是否正确引用
3. 是否订阅了 FrontendDialogueEventBus 事件
4. 查看Console是否有错误日志

### Q2: 立绘不显示？
**检查**:
1. 资源路径是否正确 (必须在 Resources/ 文件夹下)
2. 图片格式是否支持 (推荐PNG)
3. CharacterView 的 `Resource Base Path` 是否正确
4. 查看Debug日志查看资源加载信息

### Q3: 文本显示不完整？
**检查**:
1. TextMeshProUGUI 的 RectTransform 是否足够大
2. 字体大小是否合适
3. Overflow 设置 (建议用 Overflow)

### Q4: 按钮无法点击？
**检查**:
1. Button 组件是否添加
2. CanvasGroup 的 `Blocks Raycasts` 是否为 true
3. Button 的 `Interactable` 是否为 true
4. 是否有其他UI覆盖在按钮上层

---

## ✅ 完整数据流验证

```
【测试流程】
1. 运行游戏
2. 后端触发对话: GameEventDispatcher.DispatchDialogueGenerated(lines)
3. Game_UI_Coordinator 接收
4. DialogueLogicAdapter 转换数据
5. FrontendDialogueEventBus 发送事件
6. DialogueUIPanel 接收并显示

【UI更新顺序】
1. SceneView 更新背景 (如有)
2. CharacterView 淡入立绘
3. DialogueTextBox 显示文本 (打字机效果)
4. 用户点击推进
5. 显示下一条对话

【选项流程】
1. 后端生成选项数据
2. FrontendDialogueEventBus.RaiseRequestChoicesDisplay()
3. ChoiceButtonGroup 创建按钮
4. 用户点击选项
5. FrontendDialogueEventBus.RaiseUserSelectChoice()
6. DialogueUIAdapter 转发到后端
7. 后端处理选择，生成新对话
```

---

## 🎯 下一步 (Phase 4)

Phase 3 完成后，可以进行:

1. **完整集成测试**
   - 创建测试场景
   - 测试完整对话流程
   - 测试选项交互

2. **性能优化**
   - 对象池化按钮
   - 资源预加载
   - 动画优化

3. **视觉效果增强**
   - DOTween 集成
   - 更复杂的过渡动画
   - 音效集成

4. **特效系统实现**
   - 根据 DialogueEffect 播放动画
   - 特效配置系统

---

**Phase 3 完成！** 所有UI组件已实现，可以开始在Unity中搭建场景和测试了。
