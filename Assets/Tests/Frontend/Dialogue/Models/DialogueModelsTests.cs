using NUnit.Framework;
using System.Collections.Generic;
using FrontendEngine.Dialogue.Models;

namespace Tests.Frontend.Dialogue.Models
{
    /// <summary>
    /// 对话显示数据模型测试
    /// 覆盖范围:
    ///   1. 数据初始化
    ///   2. 属性设置
    ///   3. 字符串表示
    /// </summary>
    public class DialogueDisplayDataTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var data = new DialogueDisplayData();

            // Assert
            Assert.IsNotNull(data.Character);
            Assert.IsNotNull(data.Scene);
            Assert.IsNotNull(data.Effects);
            Assert.AreEqual(0, data.Effects.Count);
            Assert.IsFalse(data.IsAutoAdvance);
            Assert.AreEqual(0f, data.AutoAdvanceDelay);
            Assert.AreEqual(-1, data.SourceLineIndex);
        }

        [Test]
        public void SetProperties_ShouldWorkCorrectly()
        {
            // Arrange
            var data = new DialogueDisplayData
            {
                Text = "Test dialogue",
                IsAutoAdvance = true,
                AutoAdvanceDelay = 2.5f,
                SourceLineIndex = 5
            };

            // Assert
            Assert.AreEqual("Test dialogue", data.Text);
            Assert.IsTrue(data.IsAutoAdvance);
            Assert.AreEqual(2.5f, data.AutoAdvanceDelay);
            Assert.AreEqual(5, data.SourceLineIndex);
        }

        [Test]
        public void ToString_ShouldProvideDebugInfo()
        {
            // Arrange
            var data = new DialogueDisplayData
            {
                Character = new CharacterDisplayInfo { Name = "Alice" },
                Text = "Hello",
                SourceLineIndex = 3
            };

            // Act
            var str = data.ToString();

            // Assert
            Assert.IsTrue(str.Contains("Dialogue"));
            Assert.IsTrue(str.Contains("Alice"));
            Assert.IsTrue(str.Contains("Hello"));
            Assert.IsTrue(str.Contains("3"));
        }

        [Test]
        public void AddEffects_ShouldPopulateEffectsList()
        {
            // Arrange
            var data = new DialogueDisplayData();
            var effect1 = new DialogueEffect { Type = DialogueEffectType.FadeInOut };
            var effect2 = new DialogueEffect { Type = DialogueEffectType.ScaleUp };

            // Act
            data.Effects.Add(effect1);
            data.Effects.Add(effect2);

            // Assert
            Assert.AreEqual(2, data.Effects.Count);
            Assert.AreEqual(DialogueEffectType.FadeInOut, data.Effects[0].Type);
            Assert.AreEqual(DialogueEffectType.ScaleUp, data.Effects[1].Type);
        }
    }

    /// <summary>
    /// 角色显示信息模型测试
    /// </summary>
    public class CharacterDisplayInfoTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var character = new CharacterDisplayInfo();

            // Assert
            Assert.AreEqual("", character.Id);
            Assert.AreEqual("", character.Name);
            Assert.AreEqual("", character.SpriteResourcePath);
            Assert.AreEqual(CharacterPosition.Center, character.Position);
            Assert.IsTrue(character.IsVisible);
            Assert.AreEqual(1f, character.Alpha);
            Assert.AreEqual(1f, character.Scale);
        }

        [Test]
        public void SetProperties_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var character = new CharacterDisplayInfo
            {
                Id = "alice_001",
                Name = "Alice",
                SpriteResourcePath = "Characters/Alice/happy",
                Position = CharacterPosition.Left,
                IsVisible = false,
                Alpha = 0.5f,
                Scale = 1.2f
            };

            // Assert
            Assert.AreEqual("alice_001", character.Id);
            Assert.AreEqual("Alice", character.Name);
            Assert.AreEqual("Characters/Alice/happy", character.SpriteResourcePath);
            Assert.AreEqual(CharacterPosition.Left, character.Position);
            Assert.IsFalse(character.IsVisible);
            Assert.AreEqual(0.5f, character.Alpha);
            Assert.AreEqual(1.2f, character.Scale);
        }

        [Test]
        public void ToString_ShouldProvideDebugInfo()
        {
            // Arrange
            var character = new CharacterDisplayInfo
            {
                Name = "Bob",
                Position = CharacterPosition.Right,
                IsVisible = true
            };

            // Act
            var str = character.ToString();

            // Assert
            Assert.IsTrue(str.Contains("Bob"));
            Assert.IsTrue(str.Contains("Right"));
            Assert.IsTrue(str.Contains("True"));
        }
    }

    /// <summary>
    /// 场景显示信息模型测试
    /// </summary>
    public class SceneDisplayInfoTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var scene = new SceneDisplayInfo();

            // Assert
            Assert.AreEqual("", scene.Id);
            Assert.AreEqual("", scene.BackgroundResourcePath);
            Assert.IsTrue(scene.IsBackgroundVisible);
            Assert.AreEqual(1f, scene.BackgroundAlpha);
            Assert.AreEqual(1f, scene.LightIntensity);
            Assert.IsTrue(scene.ShowDialogueBoxBackground);
        }

        [Test]
        public void SetProperties_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var scene = new SceneDisplayInfo
            {
                Id = "office_day",
                BackgroundResourcePath = "Scenes/Office/day",
                IsBackgroundVisible = false,
                BackgroundAlpha = 0.8f,
                LightIntensity = 0.7f,
                ShowDialogueBoxBackground = false
            };

            // Assert
            Assert.AreEqual("office_day", scene.Id);
            Assert.AreEqual("Scenes/Office/day", scene.BackgroundResourcePath);
            Assert.IsFalse(scene.IsBackgroundVisible);
            Assert.AreEqual(0.8f, scene.BackgroundAlpha);
            Assert.AreEqual(0.7f, scene.LightIntensity);
            Assert.IsFalse(scene.ShowDialogueBoxBackground);
        }
    }

    /// <summary>
    /// 对话选项模型测试
    /// </summary>
    public class DialogueChoiceTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var choice = new DialogueChoice();

            // Assert
            Assert.AreEqual("", choice.Id);
            Assert.AreEqual("", choice.DisplayText);
            Assert.AreEqual("", choice.TargetPhaseId);
            Assert.IsFalse(choice.IsDisabled);
            Assert.AreEqual("", choice.DisabledReason);
            Assert.AreEqual(0, choice.Priority);
            Assert.IsNotNull(choice.Metadata);
        }

        [Test]
        public void SetProperties_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var choice = new DialogueChoice
            {
                Id = "choice_a",
                DisplayText = "Accept the offer",
                TargetPhaseId = "phase_2_accept",
                IsDisabled = false,
                Priority = 10
            };

            // Assert
            Assert.AreEqual("choice_a", choice.Id);
            Assert.AreEqual("Accept the offer", choice.DisplayText);
            Assert.AreEqual("phase_2_accept", choice.TargetPhaseId);
            Assert.IsFalse(choice.IsDisabled);
            Assert.AreEqual(10, choice.Priority);
        }

        [Test]
        public void Metadata_ShouldAllowArbitraryData()
        {
            // Arrange
            var choice = new DialogueChoice();

            // Act
            choice.Metadata["custom_key"] = "custom_value";
            choice.Metadata["requirement"] = "has_item_1";

            // Assert
            Assert.AreEqual("custom_value", choice.Metadata["custom_key"]);
            Assert.AreEqual("has_item_1", choice.Metadata["requirement"]);
        }

        [Test]
        public void DisabledChoice_ShouldStorReason()
        {
            // Arrange & Act
            var choice = new DialogueChoice
            {
                Id = "choice_locked",
                DisplayText = "Locked choice",
                IsDisabled = true,
                DisabledReason = "Requires: Investigation Level 5"
            };

            // Assert
            Assert.IsTrue(choice.IsDisabled);
            Assert.AreEqual("Requires: Investigation Level 5", choice.DisabledReason);
        }
    }

    /// <summary>
    /// 对话特效模型测试
    /// </summary>
    public class DialogueEffectTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var effect = new DialogueEffect();

            // Assert
            Assert.AreEqual(DialogueEffectType.None, effect.Type);
            Assert.AreEqual(0.5f, effect.Duration);
            Assert.IsTrue(effect.PlayOnShow);
            Assert.IsNotNull(effect.Parameters);
            Assert.AreEqual(0, effect.Parameters.Count);
        }

        [Test]
        public void SetProperties_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var effect = new DialogueEffect
            {
                Type = DialogueEffectType.TypewriterEffect,
                Duration = 1.5f,
                PlayOnShow = false
            };

            // Assert
            Assert.AreEqual(DialogueEffectType.TypewriterEffect, effect.Type);
            Assert.AreEqual(1.5f, effect.Duration);
            Assert.IsFalse(effect.PlayOnShow);
        }

        [Test]
        public void Parameters_ShouldAllowArbitraryConfiguration()
        {
            // Arrange
            var effect = new DialogueEffect { Type = DialogueEffectType.Shake };

            // Act
            effect.Parameters["intensity"] = 0.5f;
            effect.Parameters["frequency"] = 10f;

            // Assert
            Assert.AreEqual(0.5f, effect.Parameters["intensity"]);
            Assert.AreEqual(10f, effect.Parameters["frequency"]);
        }

        [Test]
        public void DialogueEffectType_ShouldHaveMultipleTypes()
        {
            // Assert: 验证枚举值存在
            Assert.AreEqual(0, (int)DialogueEffectType.None);
            Assert.AreEqual(1, (int)DialogueEffectType.FadeInOut);
            Assert.AreEqual(6, (int)DialogueEffectType.TypewriterEffect);
        }
    }
}
