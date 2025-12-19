using UnityEngine;
using System.Collections.Generic;
using Interrorgation.MidLayer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NinthsLab.Tests
{
    /// <summary>
    /// demo_v2.json 格式对话测试器
    /// 用于测试对话显示系统，支持立绘控制标记
    /// 
    /// 使用方法:
    /// 1. 挂载到场景中任意GameObject
    /// 2. 确保场景中已配置 DialogueUIPanel
    /// 3. 运行游戏，按空格键或点击Inspector中的按钮
    /// </summary>
    public class Demo_V2_DialogueTester : MonoBehaviour
    {
        [Header("控制")]
        [Tooltip("自动播放测试对话")]
        public bool autoPlay = false;

        [Tooltip("触发测试的按键")]
        public KeyCode triggerKey = KeyCode.Space;

        [Header("测试对话")]
        [Tooltip("选择要测试的场景")]
        public TestScenario currentScenario = TestScenario.Basic;

        [Header("状态")]
        [SerializeField]
        private bool isPlaying = false;

        public enum TestScenario
        {
            Basic,              // 基础对话
            WithExpressions,    // 带表情
            OffScreen,          // 画面外
            HideSprite,         // 隐藏立绘
            Mixed               // 混合测试
        }

        private void Start()
        {
            if (autoPlay)
            {
                Invoke("TriggerTest", 1f);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                TriggerTest();
            }
        }

        /// <summary>
        /// 触发测试对话
        /// </summary>
        [ContextMenu("触发测试")]
        public void TriggerTest()
        {
            if (isPlaying)
            {
                Debug.LogWarning("[Demo_V2_Tester] 对话正在播放中，请等待完成");
                return;
            }

            List<string> dialogues = GetTestDialogues();
            
            if (dialogues == null || dialogues.Count == 0)
            {
                Debug.LogError("[Demo_V2_Tester] 测试对话为空");
                return;
            }

            Debug.Log($"[Demo_V2_Tester] 开始测试场景: {currentScenario} ({dialogues.Count} 段对话)");
            isPlaying = true;

            // 触发后端事件
            GameEventDispatcher.DispatchDialogueGenerated(dialogues);

            // 设置自动重置标记
            Invoke("ResetPlayingState", 5f);
        }

        private void ResetPlayingState()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 获取测试对话内容
        /// </summary>
        private List<string> GetTestDialogues()
        {
            switch (currentScenario)
            {
                case TestScenario.Basic:
                    return GetBasicDialogues();
                
                case TestScenario.WithExpressions:
                    return GetExpressionsDialogues();
                
                case TestScenario.OffScreen:
                    return GetOffScreenDialogues();
                
                case TestScenario.HideSprite:
                    return GetHideSpriteDialogues();
                
                case TestScenario.Mixed:
                    return GetMixedDialogues();
                
                default:
                    return GetBasicDialogues();
            }
        }

        /// <summary>
        /// 测试1: 基础对话 (模拟 demo_v2.json 格式)
        /// </summary>
        private List<string> GetBasicDialogues()
        {
            return new List<string>
            {
                "安·李：\n让我们继续先前的假设。死者是从十五楼坠楼而死。\n以此为基点来看待十五楼的血迹。\n\n安乔：\n嗯……\n",
                
                "安·李：\n十五楼的血液是伪造的，不属于死者——这也是有充分可能的。\n\n安·李：\n因为十五楼与地面同时存在的两处血液，有一个致命的矛盾。",
                
                "安乔：\n矛盾？",
                
                "安·李：\n如果死者是在十五楼死于斩首，那么在十五楼喷溅完血液后，在地面就不应该还能流出那么多血。\n\n安·李：\n而如果死者是在十五楼死于坠楼，头颅是在那之后才被割下的——那么十五楼就不应该有血迹。",
                
                "（两人陷入了沉思）",
                
                "安乔：\n确实……这么一想，是这个道理。\n我之前完全没有注意到这个问题。"
            };
        }

        /// <summary>
        /// 测试2: 带表情的对话
        /// </summary>
        private List<string> GetExpressionsDialogues()
        {
            return new List<string>
            {
                "安·李：\n早上好。",
                
                "[立绘:happy]安乔：\n太好了！我们有新的线索！",
                
                "[立绘:sad]安·李：\n这很遗憾……受害者是我们认识的人。",
                
                "[立绘:angry]安乔：\n我一定要找出真相！",
                
                "安·李：\n冷静，我们一步步来。"
            };
        }

        /// <summary>
        /// 测试3: 画面外对话（有角色名但不显示立绘）
        /// </summary>
        private List<string> GetOffScreenDialogues()
        {
            return new List<string>
            {
                "安·李：\n你在哪里？",
                
                "[画面外]安乔：\n我在门外！等我一下！",
                
                "（脚步声传来）",
                
                "安乔：\n我回来了！找到了重要证据！",
                
                "安·李：\n让我看看。"
            };
        }

        /// <summary>
        /// 测试4: 隐藏立绘
        /// </summary>
        private List<string> GetHideSpriteDialogues()
        {
            return new List<string>
            {
                "安·李：\n我们需要仔细思考一下。",
                
                "[隐藏立绘]（两人沉默地思考着案情）",
                
                "[隐藏立绘]（时钟滴答作响）",
                
                "安乔：\n我想到了！",
                
                "安·李：\n说说看。"
            };
        }

        /// <summary>
        /// 测试5: 混合测试（所有功能）
        /// </summary>
        private List<string> GetMixedDialogues()
        {
            return new List<string>
            {
                "安·李：\n案件开始调查。",
                
                "[立绘:serious]安·李：\n这是一个复杂的案件。",
                
                "[画面外]安乔：\n（门外传来）等等我！",
                
                "安乔：\n我来了！",
                
                "[立绘:surprised]安乔：\n什么？！这怎么可能！",
                
                "[隐藏立绘]（震惊的沉默）",
                
                "[立绘:determined]安·李：\n不管怎样，我们必须查明真相。",
                
                "（调查继续）"
            };
        }

        /// <summary>
        /// 测试真实的 demo_v2.json 对话
        /// 从实际的JSON节点中提取的对话
        /// </summary>
        [ContextMenu("测试真实JSON对话")]
        public void TestRealJsonDialogue()
        {
            var dialogues = new List<string>
            {
                "安·李：\n让我们继续先前的假设。死者是从十五楼坠楼而死。\n以此为基点来看待十五楼的血迹。\n\n安乔：\n嗯……\n",
                
                "安·李：\n十五楼的血液是伪造的，不属于死者——这也是有充分可能的。\n\n安·李：\n因为十五楼与地面同时存在的两处血液，有一个致命的矛盾。\n\n安乔：\n矛盾？\n\n安·李：\n如果死者是在十五楼死于斩首，那么在十五楼喷溅完血液后，在地面就不应该还能流出那么多血。\n\n安·李：\n而如果死者是在十五楼死于坠楼，头颅是在那之后才被割下的——那么十五楼就不应该有血迹。\n\n安·李：\n不论如何，十五楼的血液都极有可能是伪装的，至少这点肯定错不了。\n\n安乔：\n确实……这么一想，是这个道理。\n我之前完全没有注意到这个问题。\n"
            };

            Debug.Log("[Demo_V2_Tester] 测试真实JSON格式对话");
            GameEventDispatcher.DispatchDialogueGenerated(dialogues);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 自定义Inspector编辑器
    /// </summary>
    [UnityEditor.CustomEditor(typeof(Demo_V2_DialogueTester))]
    public class Demo_V2_DialogueTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("快速测试", UnityEditor.EditorStyles.boldLabel);

            Demo_V2_DialogueTester tester = (Demo_V2_DialogueTester)target;

            // 大按钮
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"🎬 测试: {tester.currentScenario}", GUILayout.Height(40)))
            {
                if (Application.isPlaying)
                {
                    tester.TriggerTest();
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("提示", "请先运行游戏！", "确定");
                }
            }
            GUI.backgroundColor = Color.white;

            UnityEditor.EditorGUILayout.Space(5);

            // 真实JSON测试按钮
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📜 测试真实JSON格式", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    tester.TestRealJsonDialogue();
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("提示", "请先运行游戏！", "确定");
                }
            }
            GUI.backgroundColor = Color.white;

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.HelpBox(
                "测试步骤:\n" +
                "1. 确保场景中有 DialogueUIPanel\n" +
                "2. 准备角色立绘资源 (Resources/Characters/角色名/)\n" +
                "3. 运行游戏\n" +
                "4. 按空格键或点击上方按钮\n\n" +
                "测试场景:\n" +
                "• Basic - 基础多角色对话\n" +
                "• WithExpressions - 表情切换\n" +
                "• OffScreen - 画面外对话\n" +
                "• HideSprite - 隐藏立绘\n" +
                "• Mixed - 所有功能混合",
                UnityEditor.MessageType.Info
            );
        }
    }
#endif
}

