using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LogicEngine.Phase;

namespace LogicEngine.Parser
{
    /// <summary>
    /// 用于解析 PhaseData 的静态工具类
    /// </summary>
    public static class PhaseParser
    {
        /// <summary>
        /// 解析单个阶段的 Json 数据
        /// </summary>
        /// <param name="phaseId">阶段的唯一标识符（即Json中的Key）</param>
        /// <param name="json">该阶段对应的 Json 对象（即Json中的Value）</param>
        /// <returns>解析完成的 PhaseData 对象</returns>
        public static PhaseData Parse(string phaseId, JToken json)
        {
            if (json == null || json.Type == JTokenType.Null)
            {
                // 如果传入的json为空，返回null或抛出异常，视具体错误处理策略而定
                // 这里选择记录日志并返回空对象，避免空引用崩溃
                Console.WriteLine($"[PhaseParser] 警告：阶段 {phaseId} 的数据为空。");
                return null;
            }

            try
            {
                var phaseData = new PhaseData();

                // 1. 解析基础属性
                phaseData.Name = json["name"]?.ToString() ?? string.Empty;
                phaseData.IsHidden = json["is_hidden"]?.ToObject<bool>() ?? false;

                // 2. 解析依赖项 (depends_on) - 直接存储 JToken
                phaseData.DependsOn = json["depends_on"];

                // 3. 解析对话配置 (dialogue)
                phaseData.Dialogue = ParseDialogueInfo(json["dialogue"]);

                // 4. 解析完成条件节点列表 (completion_nodes)
                var completionNodesToken = json["completion_nodes"];
                if (completionNodesToken != null && completionNodesToken.Type == JTokenType.Array)
                {
                    phaseData.CompletionNodes = completionNodesToken.ToObject<List<string>>();
                }
                else
                {
                    phaseData.CompletionNodes = new List<string>();
                }

                // 5. 解析节点字典 (nodes) - 核心逻辑
                phaseData.Nodes = new Dictionary<string, NodeData>();
                var nodesToken = json["nodes"];

                if (nodesToken != null && nodesToken.Type == JTokenType.Object)
                {
                    foreach (JProperty child in nodesToken.Children<JProperty>())
                    {
                        string nodeId = child.Name;
                        JToken nodeJson = child.Value;

                        // 调用外部的 NodeParser 进行具体节点的解析
                        NodeData nodeData = NodeParser.Parse(nodeId, nodeJson);
                        
                        if (nodeData != null)
                        {
                            phaseData.Nodes.Add(nodeId, nodeData);
                        }
                        else
                        {
                            Console.WriteLine($"[PhaseParser] 警告：阶段 {phaseId} 中的节点 {nodeId} 解析失败。");
                        }
                    }
                }

                return phaseData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PhaseParser] 错误：解析阶段 {phaseId} 时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 内部辅助方法：解析对话信息
        /// </summary>
        private static PhaseDialogueInfo ParseDialogueInfo(JToken dialogueToken)
        {
            var dialogueInfo = new PhaseDialogueInfo();

            if (dialogueToken != null && dialogueToken.Type == JTokenType.Object)
            {
                // 根据需求，这里只提取 JToken，不做深度解析
                dialogueInfo.OnPhaseStart = dialogueToken["on_phase_start"];
                dialogueInfo.OnPhaseComplete = dialogueToken["on_phase_complete"];
            }

            return dialogueInfo;
        }
    }
}