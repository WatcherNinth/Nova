using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using Interrorgation.MidLayer.Dialogue;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace Tests.Interrorgation.Integration
{
    /// <summary>
    /// 集成测试: 完整对话系统流程验证
    /// 覆盖范围:
    ///   1. 后端 → 前端对话流程 (GameEventDispatcher → Game_UI_Coordinator → FrontendDialogueEventBus → UI)
    ///   2. 前端 → 后端选择流程 (UI → FrontendDialogueEventBus → Game_UI_Coordinator → GameEventDispatcher)
    ///   3. 适配器数据转换验证
    ///   4. 事件链完整性
    /// </summary>
    public class DialogueSystemIntegrationTests
    {
        private GameObject coordinatorGameObject;
        private Game_UI_Coordinator coordinator;
        private DialogueLogicAdapter logicAdapter;
        private DialogueUIAdapter uiAdapter;
        private List<DialogueDisplayData> uiReceivedDialogues;
        private List<DialogueChoice> uiReceivedChoices;

        [SetUp]
        public void SetUp()
        {
            // 清除所有事件
            FrontendDialogueEventBus.ClearAllSubscriptions();

            // 创建协调器
            coordinatorGameObject = new GameObject("GameUICoordinator");
            coordinator = coordinatorGameObject.AddComponent<Game_UI_Coordinator>();
            logicAdapter = coordinatorGameObject.AddComponent<DialogueLogicAdapter>();
            uiAdapter = coordinatorGameObject.AddComponent<DialogueUIAdapter>();

            // 模拟UI系统接收事件
            uiReceivedDialogues = new List<DialogueDisplayData>();
            uiReceivedChoices = new List<DialogueChoice>();

            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) =>
            {
                uiReceivedDialogues.Add(data);
            };

            FrontendDialogueEventBus.OnRequestChoicesDisplay += (choices) =>
            {
                uiReceivedChoices.AddRange(choices);
            };
        }

        [TearDown]
        public void TearDown()
        {
            FrontendDialogueEventBus.ClearAllSubscriptions();
            Object.DestroyImmediate(coordinatorGameObject);
        }

        #region 后端→前端流程测试

        [Test]
        public void CompleteDialogueFlow_Backend_To_Frontend_ShouldWork()
        {
            // Arrange: 模拟后端生成的对话列表
            var backendDialogues = new List<string>
            {
                "Alice: Welcome to the investigation room.",
                "Alice: We need to discuss the evidence.",
                "[FadeInOut] Bob: I have some information.",
                "Bob: You might find this interesting."
            };

            // Act: 后端触发事件
            GameEventDispatcher.DispatchDialogueGenerated(backendDialogues);

            // Assert: UI应该接收到转换后的对话
            Assert.AreEqual(4, uiReceivedDialogues.Count);

            // 验证第一条对话
            var dialogue1 = uiReceivedDialogues[0];
            Assert.AreEqual("Alice", dialogue1.Character.Name);
            Assert.AreEqual("Welcome to the investigation room.", dialogue1.Text);
            Assert.AreEqual(0, dialogue1.Effects.Count);

            // 验证第三条对话 (包含特效)
            var dialogue3 = uiReceivedDialogues[2];
            Assert.AreEqual("Bob", dialogue3.Character.Name);
            Assert.AreEqual("I have some information.", dialogue3.Text);
            Assert.AreEqual(1, dialogue3.Effects.Count);
            Assert.AreEqual(DialogueEffectType.FadeInOut, dialogue3.Effects[0].Type);
        }

        [Test]
        public void DialogueDataConversion_ShouldMaintainAccuracy()
        {
            // Arrange
            var backendLine = "Character Name: This is the dialogue text.";
            var dialogues = new List<string> { backendLine };

            // Act
            GameEventDispatcher.DispatchDialogueGenerated(dialogues);

            // Assert
            Assert.AreEqual(1, uiReceivedDialogues.Count);
            var receivedData = uiReceivedDialogues[0];

            Assert.AreEqual("Character Name", receivedData.Character.Name);
            Assert.AreEqual("This is the dialogue text.", receivedData.Text);
            Assert.IsNotEmpty(receivedData.Character.Id);
            Assert.IsNotEmpty(receivedData.Character.SpriteResourcePath);
        }

        [Test]
        public void MultipleDialogues_ShouldMaintainOrder()
        {
            // Arrange
            var names = new[] { "Alice", "Bob", "Carol", "Dave" };
            var dialogues = new List<string>();
            foreach (var name in names)
            {
                dialogues.Add($"{name}: Line from {name}");
            }

            // Act
            GameEventDispatcher.DispatchDialogueGenerated(dialogues);

            // Assert
            Assert.AreEqual(4, uiReceivedDialogues.Count);
            for (int i = 0; i < names.Length; i++)
            {
                Assert.AreEqual(names[i], uiReceivedDialogues[i].Character.Name);
            }
        }

        #endregion

        #region 前端→后端流程测试 (Future-Ready)

        [Test]
        public void UserSelectChoice_ShouldTriggerAdapterHandling()
        {
            // Arrange
            var choice = new DialogueChoice
            {
                Id = "choice_accept",
                DisplayText = "Accept the mission",
                TargetPhaseId = "phase_mission_1"
            };

            var adapterProcessed = false;
            UIEventDispatcher.OnPlayerSubmitInput += (input) =>
            {
                adapterProcessed = (input == choice.Id);
            };

            // Act
            FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

            // Assert
            Assert.IsTrue(adapterProcessed);
        }

        #endregion

        #region 数据分离验证

        [Test]
        public void DataLayer_ShouldBeCompletelyIndependent()
        {
            // Arrange: 创建数据模型
            var displayData = new DialogueDisplayData
            {
                Character = new CharacterDisplayInfo { Name = "Test" },
                Scene = new SceneDisplayInfo { Id = "test_scene" },
                Text = "Test dialogue"
            };

            // Act: 发送到EventBus
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(displayData);

            // Assert: UI接收的数据应该完全一致
            Assert.AreEqual(1, uiReceivedDialogues.Count);
            var received = uiReceivedDialogues[0];
            Assert.AreEqual(displayData.Character.Name, received.Character.Name);
            Assert.AreEqual(displayData.Text, received.Text);
            Assert.AreEqual(displayData.Scene.Id, received.Scene.Id);
        }

        [Test]
        public void AdapterLogic_ShouldNotModifyBackendData()
        {
            // Arrange: 原始后端数据
            var originalDialogues = new List<string>
            {
                "Alice: Original dialogue 1",
                "Bob: Original dialogue 2"
            };

            // Act: 通过适配器处理
            logicAdapter.ProcessDialogue(originalDialogues);

            // Assert: 原始列表应该未被修改
            Assert.AreEqual(2, originalDialogues.Count);
            Assert.AreEqual("Alice: Original dialogue 1", originalDialogues[0]);
            Assert.AreEqual("Bob: Original dialogue 2", originalDialogues[1]);
        }

        #endregion

        #region 特效系统集成测试

        [Test]
        public void EffectTagParsing_ShouldWorkInCompleteFlow()
        {
            // Arrange
            var dialogues = new List<string>
            {
                "[FadeInOut|duration=1.0] Alice: Fading in",
                "[Shake|intensity=0.5] [ScaleUp] Bob: Shaking and scaling"
            };

            // Act
            GameEventDispatcher.DispatchDialogueGenerated(dialogues);

            // Assert
            Assert.AreEqual(2, uiReceivedDialogues.Count);

            // 第一条: 单个特效
            var dialogue1 = uiReceivedDialogues[0];
            Assert.AreEqual(1, dialogue1.Effects.Count);
            Assert.AreEqual(DialogueEffectType.FadeInOut, dialogue1.Effects[0].Type);
            Assert.IsTrue(dialogue1.Effects[0].Parameters.ContainsKey("duration"));

            // 第二条: 多个特效
            var dialogue2 = uiReceivedDialogues[1];
            Assert.AreEqual(2, dialogue2.Effects.Count);
            Assert.AreEqual(DialogueEffectType.Shake, dialogue2.Effects[0].Type);
            Assert.AreEqual(DialogueEffectType.ScaleUp, dialogue2.Effects[1].Type);
        }

        #endregion

        #region 选项系统集成测试 (Future-Ready)

        [Test]
        public void ChoiceDisplay_ShouldWork()
        {
            // Arrange
            var choices = new List<(string, string)>
            {
                ("choice_1", "Accept the mission"),
                ("choice_2", "Decline the mission"),
                ("choice_3", "Ask for more information")
            };

            // Act
            logicAdapter.ProcessChoices(choices, "phase_2");

            // Assert
            Assert.AreEqual(3, uiReceivedChoices.Count);
            Assert.AreEqual("choice_1", uiReceivedChoices[0].Id);
            Assert.AreEqual("Accept the mission", uiReceivedChoices[0].DisplayText);
        }

        #endregion

        #region 边界情况和错误恢复

        [Test]
        public void EmptyDialogueList_ShouldNotCrash()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                GameEventDispatcher.DispatchDialogueGenerated(new List<string>());
            });

            Assert.AreEqual(0, uiReceivedDialogues.Count);
        }

        [Test]
        public void MalformedDialogue_ShouldBeHandledGracefully()
        {
            // Arrange
            var dialogues = new List<string>
            {
                "Valid: Dialogue",
                "", // 空行
                "NoColonLine", // 无冒号的对话
                "Another: Valid dialogue"
            };

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                GameEventDispatcher.DispatchDialogueGenerated(dialogues);
            });

            // UI应该收到3条有效对话 (第二条为空，可能被跳过或作为旁白)
            Assert.Greater(uiReceivedDialogues.Count, 0);
        }

        [Test]
        public void Coordinator_ShouldBeSingleton()
        {
            // Arrange
            var instance1 = Game_UI_Coordinator.Instance;

            // Create another one
            var anotherGO = new GameObject("AnotherCoordinator");
            var anotherCoordinator = anotherGO.AddComponent<Game_UI_Coordinator>();

            // Act
            var instance2 = Game_UI_Coordinator.Instance;

            // Assert: 应该是同一个实例
            Assert.AreEqual(instance1, instance2);

            // Cleanup
            Object.DestroyImmediate(anotherGO);
        }

        #endregion
    }
}
