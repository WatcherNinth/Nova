using System.Collections.Generic;
using UnityEngine;
using Interrorgation.MidLayer;
using System.Linq;

namespace LogicEngine.LevelLogic
{
    /// <summary>
    /// 模板逻辑管理器 (后端)
    /// 负责处理玩家提交的模板答案验证，并触发后续的逻辑流程。
    /// </summary>
    public class TemplateLogicManager
    {
        private PlayerMindMapManager _mindMapManager;
        private NodeLogicManager _nodeLogicManager;

        public TemplateLogicManager(PlayerMindMapManager mindMap, NodeLogicManager nodeLogic)
        {
            _mindMapManager = mindMap;
            _nodeLogicManager = nodeLogic;

            // 订阅事件
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer += HandleTemplateSubmit;
        }

        public void Dispose()
        {
            GameEventDispatcher.OnPlayerSubmitTemplateAnswer -= HandleTemplateSubmit;
        }

        private void HandleTemplateSubmit(string templateId, List<string> inputs)
        {
            Debug.Log($"[TemplateLogicManager] 收到提交请求: ID={templateId}, 输入=[{string.Join(", ", inputs)}]");

            // 1. 获取全局 Graph
            var graph = LevelGraphContext.CurrentGraph;
            if (graph == null) return;

            if (!graph.allTemplates.TryGetValue(templateId, out var templateData))
            {
                Debug.LogError($"[TemplateLogicManager] 找不到 ID 为 {templateId} 的模板定义。");
                return;
            }

            // 2. 转义玩家输入 (Entity Name -> Entity ID)
            List<string> translatedInputs = new List<string>();
            for (int i = 0; i < inputs.Count; i++)
            {
                string rawInput = inputs[i];

                if (templateData.DropdownOptions.ContainsKey(i))
                {
                    // 下拉框：保持原样（选项值本身就是正确答案）
                    translatedInputs.Add(rawInput);
                }
                else
                {
                    // 实体输入框：调用 EntityListData 通用方法反查 ID
                    string entityId = graph.entityListData?.TryGetEntityIdByName(rawInput);
                    if (string.IsNullOrEmpty(entityId))
                    {
                        Debug.Log($"[TemplateLogicManager] 验证失败：输入 '{rawInput}' 无法匹配任何已知实体的名称或别名。");
                        return; // 转义失败，直接判定为错
                    }
                    translatedInputs.Add(entityId);
                }
            }

            // 3. 提交转义后的 ID 给 TemplateData 进行纯粹的值匹配
            string targetNodeId = null;
            foreach (var answer in templateData.Answers)
            {
                if (answer.ValidateAnswer(translatedInputs))
                {
                    targetNodeId = answer.TargetId;
                    break;
                }
            }

            // 4. 处理结果
            var isSuccess = !string.IsNullOrEmpty(targetNodeId);
            if (isSuccess)
            {
                Debug.Log($"<color=green>[TemplateLogicManager] 模板验证通过！</color> 目标节点: {targetNodeId}");
                if (_nodeLogicManager != null)
                {
                    _nodeLogicManager.TryProveNode(targetNodeId);
                }
            }
            else
            {
                Debug.Log($"<color=yellow>[TemplateLogicManager] 模板验证失败：没有匹配的正确答案（ID 匹配不符）。</color>");
            }

            var context = new GameEventDispatcher.TemplateSettlementContext
            {
                IsSuccess = isSuccess,
                TemplateId = templateId,
                TargetNodeId = targetNodeId
            };
            GameEventDispatcher.DispatchTemplateSettlement(context);
        }
    }
}
