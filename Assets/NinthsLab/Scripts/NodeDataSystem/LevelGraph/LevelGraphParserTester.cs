using UnityEngine;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;

namespace LogicEngine.Tests
{
    public class LevelGraphParserTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("在这里粘贴完整的 Level JSON 数据")]
        [TextArea(15, 30)]
        public string jsonInput = @"
{
    ""universal_nodes"": {
        ""node_start"": { ""type"": ""start"", ""content"": ""dummy"" }
    },
    ""phase1"": {
        ""sub_node_1"": { ""type"": ""action"", ""content"": ""dummy"" }
    },
    ""node_choice_group"": {
        ""test_group"": {
            ""arg_choice_1"": {
                ""text_override"": ""覆盖文本测试"",
                ""is_hidden"": true,
                ""display_anyway"": false
            }
        }
    },
    ""nodes_mutex_group"": {
        ""test_mutex_group"": {
            ""node_1"": ""这是互斥组的单项描述字符串"",
            ""node_group_1"": [
                ""sub_mutex_a"",
                ""sub_mutex_b""
            ]
        }
    },
    ""entity_list"": {
        ""keyword_king"": {
            ""name"": ""国王"",
            ""prompt"": ""这是一个关于国王的提示词"",
            ""alias"": [""陛下"", ""老国王""]
        }
    }
}";

        void Start()
        {
            if (string.IsNullOrEmpty(jsonInput))
            {
                Debug.LogWarning("[LevelGraphParserTest] JSON Input is empty.");
                return;
            }

            RunParserTest();
        }

        [ContextMenu("Run Test Manually")]
        public void RunParserTest()
        {
            Debug.Log($"<color=cyan>[LevelGraphParserTest] 开始解析测试...</color>");

            try
            {
                // 1. 调用解析器
                LevelGraphData graph = LevelGraphParser.Parse(jsonInput);

                if (graph == null)
                {
                    Debug.LogError("[LevelGraphParserTest] 解析返回了 null！");
                    return;
                }

                // 2. 验证 Universal Nodes
                if (graph.universalNodesData != null)
                {
                    Debug.Log($"[Universal Nodes] Count: {graph.universalNodesData.Count}");
                    foreach (var kvp in graph.universalNodesData)
                    {
                        Debug.Log($" - Node: {kvp.Key}");
                    }
                }

                // 3. 验证 Phases
                if (graph.phasesData != null)
                {
                    Debug.Log($"[Phases] Count: {graph.phasesData.Count}");
                    foreach (var kvp in graph.phasesData)
                    {
                        Debug.Log($" - Phase: {kvp.Key}");
                    }
                }

                // 4. 验证 Node Choice Group
                // 注意：这里使用了小写开头的字段名 nodeChoiceGroupData
                if (graph.nodeChoiceGroupData != null && graph.nodeChoiceGroupData.Data != null)
                {
                    Debug.Log($"[Choice Groups] Group Count: {graph.nodeChoiceGroupData.Data.Count}");
                    if (graph.nodeChoiceGroupData.Data.TryGetValue("test_group", out var optionList))
                    {
                        // 现在是 List 遍历
                        foreach (var item in optionList)
                        {
                            Debug.Log($" - Group [test_group] Option -> TargetNode: '{item.TargetNodeId}' | OverrideText: '{item.TextOverride}'");
                        }
                    }
                }

                // 5. 验证 Node Mutex Group
                // 注意：这里使用了小写开头的字段名 nodeMutexGroupData
                if (graph.nodeMutexGroupData != null && graph.nodeMutexGroupData.Data != null)
                {
                    Debug.Log($"[Mutex Groups] Group Count: {graph.nodeMutexGroupData.Data.Count}");
                    if (graph.nodeMutexGroupData.Data.TryGetValue("test_mutex_group", out var mutexDict))
                    {
                        // 测试字符串类型
                        if (mutexDict.TryGetValue("node_1", out var itemStr))
                        {
                            Debug.Log($" - Item [node_1] (String): {itemStr.Description}");
                        }

                        // 测试列表类型
                        if (mutexDict.TryGetValue("node_group_1", out var itemList))
                        {
                            string listContent = itemList.RelatedNodeIds != null ? string.Join(", ", itemList.RelatedNodeIds) : "null";
                            Debug.Log($" - Item [node_group_1] (List): IsGroup={itemList.IsGroupList}, Content=[{listContent}]");
                        }
                    }
                }

                // 6. 验证 Entity List
                // 注意：这里使用了小写开头的字段名 entityListData
                if (graph.entityListData != null && graph.entityListData.Data != null)
                {
                    Debug.Log($"[Entity List] Entity Count: {graph.entityListData.Data.Count}");
                    if (graph.entityListData.Data.TryGetValue("keyword_king", out var entity))
                    {
                        string aliases = entity.Alias != null ? string.Join(", ", entity.Alias) : "";
                        Debug.Log($" - Entity [keyword_king]: Name='{entity.Name}', Prompt='{entity.Prompt}', Alias=[{aliases}]");
                    }
                }

                Debug.Log($"<color=green>[LevelGraphParserTest] 测试完成。</color>");

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelGraphParserTest] 解析过程中发生异常: \n{ex}");
            }
        }
    }
}