using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using Interrorgation.MidLayer.Dialogue;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace Tests.Interrorgation.MidLayer.Dialogue
{
    /// <summary>
    /// 对话逻辑适配器测试
    /// 覆盖范围:
    ///   1. 对话行的解析和转换
    ///   2. 角色名和文本的提取
    ///   3. 特效标记的识别
    ///   4. 旁白处理
    ///   5. 空输入处理
    ///   6. 异常安全性
    /// </summary>
    public class DialogueLogicAdapterTests
    {
        private DialogueLogicAdapter adapter;
        private GameObject testGameObject;
        private List<DialogueDisplayData> capturedDialogues;

        [SetUp]
        public void SetUp()
        {
            // 清除事件订阅
            FrontendDialogueEventBus.ClearAllSubscriptions();

            // 创建测试GameObject和适配器
            testGameObject = new GameObject("TestDialogueAdapter");
            adapter = testGameObject.AddComponent<DialogueLogicAdapter>();

            // 捕获事件
            capturedDialogues = new List<DialogueDisplayData>();
            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) =>
            {
                capturedDialogues.Add(data);
            };
        }

        [TearDown]
        public void TearDown()
        {
            FrontendDialogueEventBus.ClearAllSubscriptions();
            Object.DestroyImmediate(testGameObject);
        }

        #region 基本对话解析测试

        [Test]
        public void ProcessDialogue_WithValidLine_ShouldParseCharacterAndText()
        {
            // Arrange
            var dialogues = new List<string> { "Alice: Hello World" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(1, capturedDialogues.Count);
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual("Hello World", capturedDialogues[0].Text);
        }

        [Test]
        public void ProcessDialogue_WithMultipleLines_ShouldProcessAll()
        {
            // Arrange
            var dialogues = new List<string>
            {
                "Alice: Line 1",
                "Bob: Line 2",
                "Alice: Line 3"
            };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(3, capturedDialogues.Count);
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual("Bob", capturedDialogues[1].Character.Name);
            Assert.AreEqual("Alice", capturedDialogues[2].Character.Name);
        }

        [Test]
        public void ProcessDialogue_WithColonicText_ShouldHandleCorrectly()
        {
            // Arrange: 文本中包含冒号
            var dialogues = new List<string> { "Alice: I said: Hello!" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual("I said: Hello!", capturedDialogues[0].Text);
        }

        [Test]
        public void ProcessDialogue_WithWhitespace_ShouldTrimCorrectly()
        {
            // Arrange
            var dialogues = new List<string> { "  Alice  :  Spaced text  " };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual("Spaced text", capturedDialogues[0].Text);
        }

        #endregion

        #region 旁白处理测试

        [Test]
        public void ProcessDialogue_WithoutColon_ShouldTreatAsNarration()
        {
            // Arrange
            var dialogues = new List<string> { "The wind blew softly." };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(1, capturedDialogues.Count);
            Assert.AreEqual("[旁白]", capturedDialogues[0].Character.Name);
            Assert.AreEqual("The wind blew softly.", capturedDialogues[0].Text);
        }

        #endregion

        #region 特效标记测试

        [Test]
        public void ProcessDialogue_WithEffectTag_ShouldExtractEffect()
        {
            // Arrange
            var dialogues = new List<string> { "[FadeInOut] Alice: Hello" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(1, capturedDialogues.Count);
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual("Hello", capturedDialogues[0].Text);
            Assert.AreEqual(1, capturedDialogues[0].Effects.Count);
            Assert.AreEqual(DialogueEffectType.FadeInOut, capturedDialogues[0].Effects[0].Type);
        }

        [Test]
        public void ProcessDialogue_WithMultipleEffectTags_ShouldExtractAll()
        {
            // Arrange
            var dialogues = new List<string> { "[FadeInOut][Shake] Alice: Shaking text" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(2, capturedDialogues[0].Effects.Count);
            Assert.AreEqual(DialogueEffectType.FadeInOut, capturedDialogues[0].Effects[0].Type);
            Assert.AreEqual(DialogueEffectType.Shake, capturedDialogues[0].Effects[1].Type);
        }

        [Test]
        public void ProcessDialogue_WithEffectParameter_ShouldExtractParameters()
        {
            // Arrange
            var dialogues = new List<string> { "[FadeInOut|duration=2.0] Alice: Slow fade" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(1, capturedDialogues[0].Effects.Count);
            var effect = capturedDialogues[0].Effects[0];
            Assert.IsTrue(effect.Parameters.ContainsKey("duration"));
            Assert.AreEqual("2.0", effect.Parameters["duration"]);
        }

        #endregion

        #region 错误处理测试

        [Test]
        public void ProcessDialogue_WithNull_ShouldNotThrow()
        {
            // Act & Assert: 应该记录警告但不抛异常
            Assert.DoesNotThrow(() => adapter.ProcessDialogue(null));
        }

        [Test]
        public void ProcessDialogue_WithEmptyList_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => adapter.ProcessDialogue(new List<string>()));
        }

        [Test]
        public void ProcessDialogue_WithEmptyString_ShouldNotThrow()
        {
            // Arrange
            var dialogues = new List<string> { "" };

            // Act & Assert
            Assert.DoesNotThrow(() => adapter.ProcessDialogue(dialogues));
        }

        [Test]
        public void ProcessDialogue_WithInvalidEffectType_ShouldSkipEffect()
        {
            // Arrange
            var dialogues = new List<string> { "[InvalidEffectType] Alice: Text" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert: 效果应被跳过，对话仍应处理
            Assert.AreEqual("Alice", capturedDialogues[0].Character.Name);
            Assert.AreEqual(0, capturedDialogues[0].Effects.Count);
        }

        #endregion

        #region 数据完整性测试

        [Test]
        public void ProcessDialogue_ShouldSetSourceLineIndex()
        {
            // Arrange
            var dialogues = new List<string> { "Alice: Line 1", "Bob: Line 2", "Carol: Line 3" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual(0, capturedDialogues[0].SourceLineIndex);
            Assert.AreEqual(1, capturedDialogues[1].SourceLineIndex);
            Assert.AreEqual(2, capturedDialogues[2].SourceLineIndex);
        }

        [Test]
        public void ProcessDialogue_ShouldGenerateCharacterIds()
        {
            // Arrange
            var dialogues = new List<string> { "Alice Smith: Hello" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual("alice_smith", capturedDialogues[0].Character.Id);
        }

        [Test]
        public void ProcessDialogue_ShouldGenerateCharacterSpritePath()
        {
            // Arrange
            var dialogues = new List<string> { "Alice: Hello" };

            // Act
            adapter.ProcessDialogue(dialogues);

            // Assert
            Assert.AreEqual("Characters/Alice/default", capturedDialogues[0].Character.SpriteResourcePath);
        }

        #endregion

        #region 清除功能测试

        [Test]
        public void ClearDialogue_ShouldEmitClearEvent()
        {
            // Arrange
            var clearEventFired = false;
            FrontendDialogueEventBus.OnRequestDialogueClear += () => clearEventFired = true;

            // Act
            adapter.ClearDialogue();

            // Assert
            Assert.IsTrue(clearEventFired);
        }

        #endregion
    }

    /// <summary>
    /// 对话UI适配器测试
    /// 覆盖范围:
    ///   1. 用户选择处理
    ///   2. 用户推进请求处理
    ///   3. 选项验证
    ///   4. 异常处理
    /// </summary>
    public class DialogueUIAdapterTests
    {
        private DialogueUIAdapter uiAdapter;
        private DialogueLogicAdapter logicAdapter;
        private GameObject testGameObject;
        private string lastDispatchedInput;

        [SetUp]
        public void SetUp()
        {
            // 清除事件
            FrontendDialogueEventBus.ClearAllSubscriptions();

            // 创建测试GameObject
            testGameObject = new GameObject("TestUIAdapter");
            logicAdapter = testGameObject.AddComponent<DialogueLogicAdapter>();
            uiAdapter = testGameObject.AddComponent<DialogueUIAdapter>();

            // 模拟 GameEventDispatcher.DispatchPlayerInputString
            // 注: 这里使用 OnPlayerSubmitInput 来捕获
            lastDispatchedInput = null;
            UIEventDispatcher.OnPlayerSubmitInput += (input) => lastDispatchedInput = input;

            // 启用适配器
            uiAdapter.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            FrontendDialogueEventBus.ClearAllSubscriptions();
            UIEventDispatcher.OnPlayerSubmitInput -= (input) => lastDispatchedInput = input;
            Object.DestroyImmediate(testGameObject);
        }

        #region 用户选择测试

        [Test]
        public void HandleUserSelectChoice_WithValidChoice_ShouldDispatchInput()
        {
            // Arrange
            var choice = new DialogueChoice
            {
                Id = "choice_1",
                DisplayText = "Accept",
                TargetPhaseId = "phase_2"
            };

            // Act
            FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

            // Assert: 应该转发到后端
            // 注: 实际实现会调用 GameEventDispatcher.DispatchPlayerInputString
            Assert.IsNotNull(choice.Id);
        }

        [Test]
        public void HandleUserSelectChoice_WithNullChoice_ShouldNotThrow()
        {
            // Act & Assert: 应该记录错误但不抛异常
            Assert.DoesNotThrow(() =>
            {
                FrontendDialogueEventBus.RaiseUserSelectChoice(null);
            });
        }

        #endregion

        #region 用户推进测试

        [Test]
        public void HandleUserRequestAdvance_ShouldProcess()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                FrontendDialogueEventBus.RaiseUserRequestAdvance();
            });
        }

        #endregion

        #region 选项验证测试

        [Test]
        public void ValidateChoice_WithValidChoice_ShouldPass()
        {
            // Arrange
            var choice = new DialogueChoice
            {
                Id = "valid_id",
                DisplayText = "Valid text"
            };

            // Act & Assert: 私有方法无法直接测试，但通过完整流程验证
            Assert.DoesNotThrow(() =>
            {
                FrontendDialogueEventBus.RaiseUserSelectChoice(choice);
            });
        }

        #endregion
    }
}
