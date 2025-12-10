using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using LogicEngine.LevelGraph;
using LogicEngine.Validation;
using UnityEngine;
using LogicEngine.Templates;

namespace LogicEngine.Parser
{
    public static class LevelGraphParser
    {
        /// <summary>
        /// 总解析入口
        /// </summary>
        public static LevelGraphData Parse(string jsonText)
        {
            // 1. 解析 Json 根对象
            JObject root = JObject.Parse(jsonText);
            LevelGraphData levelGraph = new LevelGraphData();

            // 2. 解析 Universal Nodes
            levelGraph.universalNodesData = ParseUniversalNodes(root);

            // 3. 解析 Phases
            levelGraph.phasesData = ParsePhases(root);

            // 4. 解析三个自定义结构
            levelGraph.nodeChoiceGroupData = ParseNodeChoiceGroup(root);
            levelGraph.nodeMutexGroupData = ParseNodeMutexGroup(root);
            levelGraph.entityListData = ParseEntityList(root);
            levelGraph.specialTemplateData = ParseSpecialTemplate(root);

            return levelGraph;
        }

        public static LevelGraphData TryParseAndInit(string jsonText)
        {
            var currentLevelGraph = Parse(jsonText);
            try
            {
                // D. [初始化运行数据] (生成 nodeLookup 等)
                // 这一步必须在 SelfCheck 之前，否则子模块查不到数据
                currentLevelGraph.InitializeRuntimeData();

                // E. 执行权威验证 (Validate)
                // 子模块现在可以通过 LevelTestManager.Instance.CurrentLevelGraph.nodeLookup 访问数据了
                ValidationResult result = currentLevelGraph.SelfCheck();

                // F. 输出结果
                if (result.IsValid)
                {
                    Debug.Log($"[LevelGraphParser] json验证通过。\n{result}");
                }
                else
                {
                    Debug.LogError($"[LevelGraphParser] json验证失败！\n{result}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelGraphParser] 在进行TryParseAndInit时发生严重异常: {ex.Message}\n{ex.StackTrace}");
            }
            return currentLevelGraph;
        }

        // =========================================================
        // 2. Universal Nodes 解析
        // =========================================================
        private static Dictionary<string, NodeData> ParseUniversalNodes(JObject root)
        {
            var result = new Dictionary<string, NodeData>();

            if (root.TryGetValue("universal_nodes", out JToken nodesToken) && nodesToken is JObject nodesObj)
            {
                foreach (var prop in nodesObj.Properties())
                {
                    string nodeId = prop.Name;
                    JToken nodeJson = prop.Value;

                    // 调用外部存在的 NodeParser
                    NodeData nodeData = NodeParser.Parse(nodeId, nodeJson);
                    result.Add(nodeId, nodeData);
                }
            }

            return result;
        }

        // =========================================================
        // 3. Phases 解析 (匹配 phase + 数字)
        // =========================================================
        private static Dictionary<string, PhaseData> ParsePhases(JObject root)
        {
            var result = new Dictionary<string, PhaseData>();

            // 正则表达式匹配以 phase 开头，后跟至少一个数字的键
            Regex phaseRegex = new Regex(@"^phase\d+$");

            foreach (var prop in root.Properties())
            {
                if (phaseRegex.IsMatch(prop.Name))
                {
                    string phaseId = prop.Name;
                    JToken phaseJson = prop.Value;

                    // 调用外部存在的 PhaseParser
                    PhaseData phaseData = PhaseParser.Parse(phaseId, phaseJson);
                    result.Add(phaseId, phaseData);
                }
            }

            return result;
        }

        // =========================================================
        // 4.1 Node Choice Group 解析 (重写版)
        // =========================================================
        private static NodeChoiceGroupData ParseNodeChoiceGroup(JObject root)
        {
            var groupData = new NodeChoiceGroupData();

            // 获取根节点下的 node_choice_group 对象
            if (root.TryGetValue("node_choice_group", out JToken mainToken) && mainToken is JObject mainObj)
            {
                // 遍历每一个组 (例如 "test_group")
                foreach (var groupProp in mainObj.Properties())
                {
                    string groupId = groupProp.Name;
                    JToken groupContent = groupProp.Value;

                    var itemList = new List<NodeChoiceItem>();

                    // 遍历组内的每一个选项 (例如 "这里将要出现的选项对应的论点")
                    if (groupContent is JObject groupContentObj)
                    {
                        foreach (var itemProp in groupContentObj.Properties())
                        {
                            string targetNodeId = itemProp.Name; // 这是 Key，即目标节点ID
                            JToken itemJson = itemProp.Value;

                            // 反序列化 Item 内容
                            NodeChoiceItem item = itemJson.ToObject<NodeChoiceItem>();

                            if (item != null)
                            {
                                // [核心] 将 Key 注入到 Item 的属性中
                                item.TargetNodeId = targetNodeId;
                                itemList.Add(item);
                            }
                        }
                    }

                    // 将生成的列表加入字典
                    groupData.Data.Add(groupId, itemList);
                }
            }

            return groupData;
        }

        // =========================================================
        // 4.2 Node Mutex Group 解析 (修正版)
        // =========================================================
        private static NodeMutexGroupData ParseNodeMutexGroup(JObject root)
        {
            var groupData = new NodeMutexGroupData();

            if (root.TryGetValue("nodes_mutex_group", out JToken token) && token is JObject groupObj)
            {
                foreach (var groupProp in groupObj.Properties())
                {
                    string groupId = groupProp.Name;
                    JObject groupContent = groupProp.Value as JObject;

                    if (groupContent == null) continue;

                    var itemList = new List<NodeMutexItem>();

                    foreach (var itemProp in groupContent.Properties())
                    {
                        string itemKey = itemProp.Name; // 例如 "nodes_1"
                        JToken itemValue = itemProp.Value;

                        var mutexItem = new NodeMutexItem();
                        mutexItem.KeyId = itemKey;

                        if (itemValue.Type == JTokenType.String)
                        {
                            // [修正] 字符串直接作为 NodeID
                            mutexItem.SingleNodeId = itemValue.ToString();
                            mutexItem.GroupNodeIds = null;
                        }
                        else if (itemValue.Type == JTokenType.Array)
                        {
                            // [修正] 数组作为 ID 列表
                            mutexItem.SingleNodeId = null;
                            mutexItem.GroupNodeIds = itemValue.ToObject<List<string>>();
                        }

                        itemList.Add(mutexItem);
                    }

                    groupData.Data.Add(groupId, itemList);
                }
            }

            return groupData;
        }

        // =========================================================
        // 4.3 Entity List 解析
        // =========================================================
        private static EntityListData ParseEntityList(JObject root)
        {
            var listData = new EntityListData();

            if (root.TryGetValue("entity_list", out JToken token) && token is JObject listObj)
            {
                // 利用 Json.NET 的 ToObject 直接反序列化到字典结构
                var dict = listObj.ToObject<Dictionary<string, EntityItem>>();

                if (dict != null)
                {
                    listData.Data = dict;
                }
            }

            return listData;
        }

        private static Dictionary<string, TemplateData> ParseSpecialTemplate(JObject root)
        {
            var result = new Dictionary<string, TemplateData>();

            if (root.TryGetValue("special_node_template", out JToken nodesToken) && nodesToken is JObject templatesObj)
            {
                foreach (var prop in templatesObj.Properties())
                {
                    string templateId = prop.Name;
                    JToken templateJson = prop.Value;
                    // 调用外部存在的 NodeParser
                    TemplateData templateData = TemplateParser.Parse(templateJson);
                    result.Add(templateId, templateData);
                }
            }

            return result;
        }
    }
}