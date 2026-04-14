using UnityEngine;
using LogicEngine.LevelLogic;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;
using System.Collections.Generic;
using System.Linq;

namespace LogicEngine.Test
{
    /// <summary>
    /// InterrorgationLevelManager 的多功能测试脚本 (正式版)
    /// </summary>
    public class InterrorgationLevelTester : MonoBehaviour
    {
        [Header("Level Load Settings")]
        public string LevelName = "demo_v2";
        [Tooltip("是否在 Start 时自动加载当前关卡")]
        public bool AutoLoadOnStart = false;

        private void Start()
        {
            if (AutoLoadOnStart)
            {
                TestLoadLevel();
            }
        }

        #region Level Loading
        public void TestLoadLevel()
        {
            var manager = InterrorgationLevelManager.Instance;
            if (manager == null) return;

            Debug.Log($"<color=cyan>[Tester]</color> 正在加载: {LevelName}");
            manager.LoadLevel(LevelName);

            var graph = manager.GetLevelGraph();
            if (graph != null)
            {
                Debug.Log($"<color=green>[Tester] 加载成功!</color> 节点数: {graph.universalNodesData.Count + graph.phasesData.Values.Sum(p => p.Nodes.Count)}");
            }
        }
        #endregion

        #region Template Events
        
        [Header("Template Discovery Test")]
        [Tooltip("模拟后端发现的新模板 ID")]
        public string DiscoverTemplateId = "tpl_001";

        /// <summary>
        /// 模拟 UI 层向 Logic 层分发“发现了新模板”
        /// </summary>
        public void TestDispatchDiscoveredTemplates()
        {
            var manager = InterrorgationLevelManager.Instance;
            if (manager == null || manager.GetLevelGraph() == null)
            {
                Debug.LogWarning("[Tester] 请先加载关卡，否则无法找到对应的模板数据引用。");
                return;
            }

            // 从数据源中寻找对应的模板引用
            var graph = manager.GetLevelGraph();
            if (graph.allTemplates.TryGetValue(DiscoverTemplateId, out var templateData))
            {
                var runtimeData = new RuntimeTemplateData(DiscoverTemplateId, templateData, RunTimeTemplateDataStatus.Discovered);
                Debug.Log($"<color=orange>[Tester]</color> 模拟分发发现新模板: {DiscoverTemplateId}");
                GameEventDispatcher.DispatchDiscoveredNewTemplates(new List<string> { DiscoverTemplateId });
            }
            else
            {
                Debug.LogError($"[Tester] 关卡数据中不存在 ID 为 {DiscoverTemplateId} 的模板。");
            }
        }        
        
        [Header("Template Submit Test")]
        [Tooltip("模拟玩家提交的模板 ID")]
        public string SubmitTemplateId = "tpl_001";
        [Tooltip("模拟玩家填写的各项内容")]
        public List<string> SubmitAnswers = new List<string> { "答案A", "答案B" };


        /// <summary>
        /// 模拟 UI 层向 Logic 层提交“模板答案”
        /// </summary>
        public void TestDispatchPlayerSubmitTemplate()
        {
            Debug.Log($"<color=orange>[Tester]</color> 模拟提交模板答案: ID={SubmitTemplateId}, Answers=[{string.Join(", ", SubmitAnswers)}]");
            UIEventDispatcher.DispatchPlayerSubmitTemplateAnswer(SubmitTemplateId, SubmitAnswers);
        }

        #endregion
    }
}
