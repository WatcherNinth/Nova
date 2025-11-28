using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogicEngine.Parser;

namespace LogicEngine.Tests
{
    /// <summary>
    /// PhaseParser 的测试驱动脚本
    /// 挂载到场景中任意 GameObject 上即可使用
    /// </summary>
    public class PhaseParserTest : MonoBehaviour
    {
        [Header("测试数据配置")]
        [Tooltip("在此处粘贴完整的 JSON 结构，包含 Phase Key")]
        [TextArea(15, 30)]
        public string jsonContent;

        private void Start()
        {
            RunTest();
        }

        private void RunTest()
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Debug.LogError("[PhaseParserTest] JSON 内容为空，请在 Inspector 中填写。");
                return;
            }

            Debug.Log("<color=cyan>[PhaseParserTest] 开始测试解析...</color>");

            try
            {
                // 1. 将文本转换为 JObject (模拟读取文件)
                JObject rootObject = JObject.Parse(jsonContent);

                // 2. 遍历 JSON 中的所有 Key (通常关卡文件包含多个 Phase，这里模拟遍历过程)
                foreach (var property in rootObject.Properties())
                {
                    string phaseId = property.Name;
                    JToken phaseToken = property.Value;

                    Debug.Log($"---------------- 正在解析: {phaseId} ----------------");

                    // 3. 调用静态解析类
                    PhaseData result = PhaseParser.Parse(phaseId, phaseToken);

                    // 4. 输出结果验证
                    if (result != null)
                    {
                        PrintPhaseData(phaseId, result);
                    }
                    else
                    {
                        Debug.LogError($"[PhaseParserTest] 阶段 {phaseId} 解析失败，返回了 null。");
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.LogError($"[PhaseParserTest] JSON 格式错误: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PhaseParserTest] 未知错误: {ex}");
            }
        }

        /// <summary>
        /// 打印 PhaseData 的详细信息到控制台
        /// </summary>
        private void PrintPhaseData(string id, PhaseData data)
        {
            string log = $"<b>[解析成功] ID: {id}</b>\n";
            log += $"Name: {data.Name}\n";
            log += $"IsHidden: {data.IsHidden}\n";

            // 打印依赖
            log += $"DependsOn (Raw): {data.DependsOn?.ToString(Formatting.None) ?? "None"}\n";

            // 打印对话
            string startDiag = data.Dialogue?.OnPhaseStart != null ? "Exist" : "Null";
            string endDiag = data.Dialogue?.OnPhaseComplete != null ? "Exist" : "Null";
            log += $"Dialogue: Start=[{startDiag}], End=[{endDiag}]\n";

            // 打印节点
            log += $"Nodes Count: {data.Nodes?.Count ?? 0}\n";
            if (data.Nodes != null)
            {
                foreach (var kvp in data.Nodes)
                {
                    log += $"  - Node Key: {kvp.Key}, Data Type: {kvp.Value.GetType().Name}\n";
                }
            }

            // 打印完成条件
            log += $"Completion Nodes: [{string.Join(", ", data.CompletionNodes)}]";

            Debug.Log(log);
        }
    }
}