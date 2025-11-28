using UnityEngine;
using System.IO;
using LogicEngine.LevelGraph;
using LogicEngine.Parser;
using LogicEngine.Validation;

namespace LogicEngine.Tests
{
    [ExecuteInEditMode]
    public class LevelTestManager : MonoBehaviour
    {
        // ==========================================
        // 稳健的单例模式 (适配 Edit Mode)
        // ==========================================
        private static LevelTestManager _instance;

        public static LevelTestManager Instance
        {
            get
            {
                // 如果为空，尝试在场景中寻找
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<LevelTestManager>();
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private void OnEnable()
        {
            _instance = this;
        }

        // ==========================================
        // 配置
        // ==========================================
        [Header("Path Settings")]
        [Tooltip("JSON文件所在的文件夹路径（相对于 Assets 文件夹），例如: Resources/Levels")]
        public string relativePath = "Resources/Levels";

        [Header("Runtime Status")]
        [Tooltip("当前正在被验证的 LevelGraph，子模块可通过 Singleton 访问此字段")]
        public LevelGraphData CurrentLevelGraph;

        // ==========================================
        // 核心逻辑
        // ==========================================

        /// <summary>
        /// 读取指定路径下所有 JSON 并执行验证
        /// </summary>
        public void RunAllTests()
        {
            // [关键修改] 强制刷新单例引用
            // 既然是通过 Inspector 按钮调用到这个实例的方法，说明 this 一定存在。
            // 强制赋值给 Instance，确保后续子模块访问 Instance 时绝对不为空。
            _instance = this;

            // 1. 获取完整路径
            string fullPath = Path.Combine(Application.dataPath, relativePath);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"[LevelTestManager] 找不到文件夹路径: {fullPath}");
                return;
            }

            // 2. 获取所有 json 文件
            string[] files = Directory.GetFiles(fullPath, "*.json");
            Debug.Log($"<color=cyan>[LevelTestManager] 开始批量测试。在路径 [{relativePath}] 下找到 {files.Length} 个文件。</color>");

            int successCount = 0;

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                
                try
                {
                    // A. 清空上一轮的数据
                    CurrentLevelGraph = null;

                    // B. 读取并解析
                    string jsonText = File.ReadAllText(filePath);
                    LevelGraphData parsedGraph = LevelGraphParser.Parse(jsonText);

                    // C. [注入单例数据]
                    // 此时 Instance 已经指向当前脚本，我们将数据填入
                    CurrentLevelGraph = parsedGraph;
                    
                    // D. [初始化运行数据] (生成 nodeLookup 等)
                    // 这一步必须在 SelfCheck 之前，否则子模块查不到数据
                    CurrentLevelGraph.InitializeRuntimeData();

                    // E. 执行权威验证 (Validate)
                    // 子模块现在可以通过 LevelTestManager.Instance.CurrentLevelGraph.nodeLookup 访问数据了
                    ValidationResult result = CurrentLevelGraph.SelfCheck(fileName);

                    // F. 输出结果
                    if (result.IsValid)
                    {
                        Debug.Log($"[LevelTestManager] 文件 <color=green>{fileName}</color> 验证通过。\n{result}");
                        successCount++;
                    }
                    else
                    {
                        Debug.LogError($"[LevelTestManager] 文件 <color=red>{fileName}</color> 验证失败！\n{result}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LevelTestManager] 处理文件 {fileName} 时发生严重异常: {ex.Message}\n{ex.StackTrace}");
                }
            }

            // G. 最终清理：验证结束后清空引用，防止 Inspector 显示旧数据误导
            CurrentLevelGraph = null;
            Debug.Log($"<color=cyan>[LevelTestManager] 测试结束。通过率: {successCount}/{files.Length}</color>");
        }
    }
}