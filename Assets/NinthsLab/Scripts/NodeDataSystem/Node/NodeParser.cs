using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using LogicEngine.Nodes;
using LogicEngine.Templates; // 假设 TemplateData 在这里

namespace LogicEngine
{
    public static class NodeParser
    {
        /// <summary>
        /// 将单个节点的 JSON 数据解析为 NodeData 对象
        /// </summary>
        /// <param name="nodeId">节点的键值（ID）</param>
        /// <param name="json">该节点对应的 JSON 对象</param>
        /// <returns>解析完成的 NodeData</returns>
        public static NodeData Parse(string nodeId, JToken json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json), $"Node JSON cannot be null for ID: {nodeId}");
            }

            // 1. 检查必要属性是否存在
            ValidateMandatoryFields(nodeId, json);

            // 2. 构建主对象
            var nodeData = new NodeData
            {
                Id = nodeId,
                Basic = ParseBasicInfo(json),
                AI = ParseAIInfo(json),
                Logic = ParseLogicInfo(json),
                Template = ParseTemplateInfo(json),
                Dialogue = ParseDialogueInfo(json)
            };

            return nodeData;
        }

        private static void ValidateMandatoryFields(string nodeId, JToken json)
        {
            if (json["description"] == null)
            {
                throw new Exception($"[NodeParser] Missing mandatory attribute 'description' in node: {nodeId}");
            }

            if (json["dialogue"] == null)
            {
                throw new Exception($"[NodeParser] Missing mandatory attribute 'dialogue' in node: {nodeId}");
            }
        }

        private static NodeBasicInfo ParseBasicInfo(JToken json)
        {
            return new NodeBasicInfo
            {
                // 必要属性，前面已校验
                Description = json["description"]!.ToString(),
                
                // 可选属性，默认为 false
                IsWrong = json["is_wrong"]?.ToObject<bool>() ?? false
            };
        }

        private static NodeAIInfo ParseAIInfo(JToken json)
        {
            return new NodeAIInfo
            {
                Prompt = json["prompt"]?.ToString(),
                
                // 使用 SafeToList 扩展方法或手动处理 null 转换为空列表
                Entities = JsonToList(json["entities"]),
                
                ExtraInputSamples = JsonToList(json["extra_input_sample"])
            };
        }

        private static NodeLogicInfo ParseLogicInfo(JToken json)
        {
            return new NodeLogicInfo
            {
                MutexGroup = json["mutex_group"]?.ToString(),
                
                ExtraMutexList = JsonToList(json["extra_mutex_list"]),
                
                // 复杂结构暂存为 JToken
                OverrideMutexTrigger = json["override_mutex_trigger"],
                
                // 复杂结构暂存为 JToken
                DependsOn = json["depends_on"],
                
                IsAutoVerified = json["is_auto_verified"]?.ToObject<bool>() ?? false
            };
        }

        private static NodeTemplateInfo ParseTemplateInfo(JToken json)
        {
            var info = new NodeTemplateInfo
            {
                SpecialTemplateId = json["special_node_template"]?.ToString()
            };

            var templateJson = json["template"];
            if (templateJson != null && templateJson.HasValues)
            {
                info.Template = TemplateParser.Parse(templateJson);
            }
            return info;
        }

        private static NodeDialogueInfo ParseDialogueInfo(JToken json)
        {
            // dialogue 及其子项较为特殊，虽然 dialogue 必须存在，但内部子项可能不存在
            var dialogueJson = json["dialogue"];

            return new NodeDialogueInfo
            {
                OnProven = dialogueJson["on_proven"],
                OnPending = dialogueJson["on_pending"],
                OnMutex = dialogueJson["on_mutex"]
            };
        }

        /// <summary>
        /// 辅助方法：将 JToken 安全转换为 List<string>，如果为 null 则返回空列表
        /// </summary>
        private static List<string> JsonToList(JToken token)
        {
            if (token == null)
            {
                return new List<string>();
            }
            return token.ToObject<List<string>>() ?? new List<string>();
        }
    }
}