using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using LogicEngine.LevelGraph;

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

            return levelGraph;
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
        // 4.1 Node Choice Group 解析
        // =========================================================
        private static NodeChoiceGroupData ParseNodeChoiceGroup(JObject root)
        {
            var groupData = new NodeChoiceGroupData();

            if (root.TryGetValue("node_choice_group", out JToken token) && token is JObject groupObj)
            {
                // 利用 Json.NET 的 ToObject 直接反序列化到字典结构
                // 因为 NodeChoiceItem 的字段已经标记了 [JsonProperty]
                var dict = groupObj.ToObject<Dictionary<string, Dictionary<string, NodeChoiceItem>>>();
                
                if (dict != null)
                {
                    groupData.Data = dict;
                }
            }

            return groupData;
        }

        // =========================================================
        // 4.2 Node Mutex Group 解析 (手动处理 String/List 混合类型)
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

                    var innerDict = new Dictionary<string, NodeMutexItem>();

                    foreach (var itemProp in groupContent.Properties())
                    {
                        string itemId = itemProp.Name;
                        JToken itemValue = itemProp.Value;

                        var mutexItem = new NodeMutexItem();

                        // 判断值类型
                        if (itemValue.Type == JTokenType.String)
                        {
                            // 如果是字符串，存入 Description
                            mutexItem.Description = itemValue.ToString();
                        }
                        else if (itemValue.Type == JTokenType.Array)
                        {
                            // 如果是数组，存入 RelatedNodeIds
                            mutexItem.RelatedNodeIds = itemValue.ToObject<List<string>>();
                        }

                        innerDict.Add(itemId, mutexItem);
                    }

                    groupData.Data.Add(groupId, innerDict);
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
    }
}