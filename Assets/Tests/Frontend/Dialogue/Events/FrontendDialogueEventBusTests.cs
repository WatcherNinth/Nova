using NUnit.Framework;
using System.Collections.Generic;
using FrontendEngine.Dialogue.Events;
using FrontendEngine.Dialogue.Models;

namespace Tests.Frontend.Dialogue.Events
{
    /// <summary>
    /// FrontendDialogueEventBus 单元测试
    /// 覆盖范围:
    ///   1. 事件发送和接收
    ///   2. 参数验证
    ///   3. 多订阅者处理
    ///   4. 异常处理
    /// </summary>
    public class FrontendDialogueEventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            // 清除前一个测试的订阅
            FrontendDialogueEventBus.ClearAllSubscriptions();
        }

        [TearDown]
        public void TearDown()
        {
            // 清除测试订阅
            FrontendDialogueEventBus.ClearAllSubscriptions();
        }

        #region 对话显示事件测试

        [Test]
        public void OnRequestDialogueDisplay_WithValidData_ShouldInvokeSubscriber()
        {
            // Arrange
            var displayData = new DialogueDisplayData
            {
                Character = new CharacterDisplayInfo { Name = "Alice" },
                Text = "Hello World"
            };
            var invoked = false;

            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => invoked = true;

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(displayData);

            // Assert
            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnRequestDialogueDisplay_WithNullData_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                FrontendDialogueEventBus.RaiseRequestDialogueDisplay(null)
            );
        }

        [Test]
        public void OnRequestDialogueDisplay_WithMultipleSubscribers_ShouldInvokeAll()
        {
            // Arrange
            var displayData = new DialogueDisplayData();
            var count = 0;

            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => count++;
            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => count++;
            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => count++;

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(displayData);

            // Assert
            Assert.AreEqual(3, count);
        }

        [Test]
        public void OnRequestDialogueDisplay_ShouldPassCorrectData()
        {
            // Arrange
            var originalData = new DialogueDisplayData
            {
                Character = new CharacterDisplayInfo { Name = "Bob", Id = "bob_001" },
                Text = "Test dialogue"
            };

            DialogueDisplayData receivedData = null;
            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => receivedData = data;

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(originalData);

            // Assert
            Assert.AreEqual(originalData.Character.Name, receivedData.Character.Name);
            Assert.AreEqual(originalData.Text, receivedData.Text);
        }

        #endregion

        #region 选项显示事件测试

        [Test]
        public void OnRequestChoicesDisplay_WithValidData_ShouldInvokeSubscriber()
        {
            // Arrange
            var choices = new List<DialogueChoice>
            {
                new DialogueChoice { Id = "1", DisplayText = "Choice A" },
                new DialogueChoice { Id = "2", DisplayText = "Choice B" }
            };
            var invoked = false;

            FrontendDialogueEventBus.OnRequestChoicesDisplay += (list) => invoked = true;

            // Act
            FrontendDialogueEventBus.RaiseRequestChoicesDisplay(choices);

            // Assert
            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnRequestChoicesDisplay_WithNullList_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                FrontendDialogueEventBus.RaiseRequestChoicesDisplay(null)
            );
        }

        [Test]
        public void OnRequestChoicesDisplay_WithEmptyList_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                FrontendDialogueEventBus.RaiseRequestChoicesDisplay(new List<DialogueChoice>())
            );
        }

        [Test]
        public void OnRequestChoicesDisplay_ShouldPassCorrectData()
        {
            // Arrange
            var originalChoices = new List<DialogueChoice>
            {
                new DialogueChoice { Id = "opt1", DisplayText = "Option 1", TargetPhaseId = "phase_2" },
                new DialogueChoice { Id = "opt2", DisplayText = "Option 2", TargetPhaseId = "phase_3" }
            };

            List<DialogueChoice> receivedChoices = null;
            FrontendDialogueEventBus.OnRequestChoicesDisplay += (choices) => receivedChoices = choices;

            // Act
            FrontendDialogueEventBus.RaiseRequestChoicesDisplay(originalChoices);

            // Assert
            Assert.AreEqual(2, receivedChoices.Count);
            Assert.AreEqual("opt1", receivedChoices[0].Id);
            Assert.AreEqual("phase_2", receivedChoices[0].TargetPhaseId);
        }

        #endregion

        #region 清除事件测试

        [Test]
        public void OnRequestDialogueClear_ShouldInvokeSubscriber()
        {
            // Arrange
            var invoked = false;
            FrontendDialogueEventBus.OnRequestDialogueClear += () => invoked = true;

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueClear();

            // Assert
            Assert.IsTrue(invoked);
        }

        #endregion

        #region 用户输入事件测试

        [Test]
        public void OnUserSelectChoice_WithValidData_ShouldInvokeSubscriber()
        {
            // Arrange
            var choice = new DialogueChoice { Id = "choice_1", DisplayText = "Selected" };
            var invoked = false;

            FrontendDialogueEventBus.OnUserSelectChoice += (c) => invoked = true;

            // Act
            FrontendDialogueEventBus.RaiseUserSelectChoice(choice);

            // Assert
            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnUserSelectChoice_WithNullChoice_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                FrontendDialogueEventBus.RaiseUserSelectChoice(null)
            );
        }

        [Test]
        public void OnUserRequestAdvance_ShouldInvokeSubscriber()
        {
            // Arrange
            var invoked = false;
            FrontendDialogueEventBus.OnUserRequestAdvance += () => invoked = true;

            // Act
            FrontendDialogueEventBus.RaiseUserRequestAdvance();

            // Assert
            Assert.IsTrue(invoked);
        }

        #endregion

        #region 订阅/取消订阅测试

        [Test]
        public void Unsubscribe_ShouldNotInvokeAfterUnsubscribe()
        {
            // Arrange
            var count = 0;
            void Handler(DialogueDisplayData data) => count++;

            FrontendDialogueEventBus.OnRequestDialogueDisplay += Handler;
            FrontendDialogueEventBus.OnRequestDialogueDisplay -= Handler;

            var displayData = new DialogueDisplayData();

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(displayData);

            // Assert
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ClearAllSubscriptions_ShouldRemoveAllSubscribers()
        {
            // Arrange
            var count = 0;

            FrontendDialogueEventBus.OnRequestDialogueDisplay += (data) => count++;
            FrontendDialogueEventBus.OnRequestChoicesDisplay += (choices) => count++;
            FrontendDialogueEventBus.OnUserSelectChoice += (choice) => count++;

            FrontendDialogueEventBus.ClearAllSubscriptions();

            // Act
            FrontendDialogueEventBus.RaiseRequestDialogueDisplay(new DialogueDisplayData());
            FrontendDialogueEventBus.RaiseRequestChoicesDisplay(new List<DialogueChoice> 
            { 
                new DialogueChoice { Id = "1", DisplayText = "Test" } 
            });
            FrontendDialogueEventBus.RaiseUserSelectChoice(new DialogueChoice { Id = "1", DisplayText = "Test" });

            // Assert
            Assert.AreEqual(0, count);
        }

        #endregion
    }
}
