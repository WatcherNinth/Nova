using UnityEngine;
using Newtonsoft.Json.Linq;
using LogicEngine; // 引用你的逻辑命名空间
using LogicEngine.Validation;

public class NodeParserTester : MonoBehaviour
{
    [Header("在此处粘贴 JSON 数据 (记得外层要加 {})")]
    [TextArea(15, 50)] // 提供一个大的文本框
    public string jsonInput;

    void Start()
    {
        if (string.IsNullOrEmpty(jsonInput))
        {
            Debug.LogWarning("JSON Input is empty.");
            return;
        }

        try
        {
            // 1. 将文本解析为 JObject
            JObject rootObject = JObject.Parse(jsonInput);

            // 2. 遍历根对象下的每一个节点 (通常测试时只有一个 key，如 "test_node")
            foreach (var property in rootObject.Properties())
            {
                string nodeId = property.Name;     // 例如 "test_node"
                JToken nodeContent = property.Value; // 对应的内容对象

                Debug.Log($"<color=cyan>Start Parsing Node: {nodeId}</color>");

                // 3. 调用静态解析类
                NodeData nodeData = NodeParser.Parse(nodeId, nodeContent);

                // === 新增：自检调用 ===
                var validationResult = nodeData.SelfCheck($"Node_{nodeId}");

                if (!validationResult.IsValid)
                {
                    Debug.LogError($"<color=red>自检失败:</color>\n{validationResult.ToString()}");
                }
                else
                {
                    Debug.Log("<color=green>自检通过。</color>");
                }

                // 4. 输出解析结果到 Console
                PrintNodeData(nodeData);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"解析过程中发生错误: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 将 NodeData 的内容详细打印到 Console
    /// </summary>
    private void PrintNodeData(NodeData data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"<b>=== Node ID: {data.Id} ===</b>");

        // 1. Basic Info
        sb.AppendLine($"<b>[Basic Info]</b>");
        sb.AppendLine($"  Description: {data.Basic.Description}");
        sb.AppendLine($"  IsWrong: {data.Basic.IsWrong}");

        // 2. AI Info
        sb.AppendLine($"<b>[AI Info]</b>");
        sb.AppendLine($"  Prompt: {(string.IsNullOrEmpty(data.AI.Prompt) ? "None" : data.AI.Prompt)}");
        sb.AppendLine($"  Entities Count: {data.AI.Entities.Count}");
        foreach (var entity in data.AI.Entities) sb.AppendLine($"    - {entity}");
        sb.AppendLine($"  Extra Input Samples: {data.AI.ExtraInputSamples.Count}");

        // 3. Logic Info
        sb.AppendLine($"<b>[Logic Info]</b>");
        sb.AppendLine($"  Mutex Group: {data.Logic.MutexGroup ?? "None"}");
        sb.AppendLine($"  Extra Mutex List: {data.Logic.ExtraMutexList.Count} items");
        sb.AppendLine($"  Is Auto Verified: {data.Logic.IsAutoVerified}");
        sb.AppendLine($"  Override Mutex Trigger: {(data.Logic.OverrideMutexTrigger != null ? "Exists (JToken)" : "Null")}");
        sb.AppendLine($"  Depends On: {(data.Logic.DependsOn != null ? "Exists (JToken)" : "Null")}");

        // 4. Template Info
        sb.AppendLine($"<b>[Template Info]</b>");
        sb.AppendLine($"  Special Template ID: {data.Template.SpecialTemplateId ?? "None"}");
        // 注意：这里 Data 目前是 null，直到你实现了 TemplateParser
        sb.AppendLine($"  Template Data: {(data.Template.Template != null ? "Parsed Object" : "Null (Pending Implementation)")}");

        // 5. Dialogue Info
        sb.AppendLine($"<b>[Dialogue Info]</b>");
        sb.AppendLine($"  On Proven: {(data.Dialogue.OnProven != null ? "Check OK" : "Missing")}");
        if (data.Dialogue.OnProven != null) sb.AppendLine($"    -> Content Sample: {data.Dialogue.OnProven.ToString(Newtonsoft.Json.Formatting.None)}");

        sb.AppendLine($"  On Pending: {(data.Dialogue.OnPending != null ? "Check OK" : "Missing")}");
        sb.AppendLine($"  On Mutex: {(data.Dialogue.OnMutex != null ? "Check OK" : "Missing")}");

        Debug.Log(sb.ToString());
    }
}