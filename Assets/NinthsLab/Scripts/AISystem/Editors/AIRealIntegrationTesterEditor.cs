#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LogicEngine.LevelLogic;
using System.Collections.Generic;
using System.Linq; // [必需] 用于 ToList()

[CustomEditor(typeof(AIRealIntegrationTester))]
public class AIRealIntegrationTesterEditor : Editor
{
    // 用于保存填空题的临时输入
    private Dictionary<string, string> _templateInputs = new Dictionary<string, string>();

    public override void OnInspectorGUI()
    {
        AIRealIntegrationTester tester = (AIRealIntegrationTester)target;

        // ... (头部UI保持不变) ...
        EditorGUILayout.LabelField("🎮 游戏控制台", EditorStyles.boldLabel);
        GUI.enabled = !Application.isPlaying;
        tester.targetFileName = EditorGUILayout.TextField("剧本文件名", tester.targetFileName);
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("加载/重置游戏 (Initialize)")) tester.InitializeGame();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先点击 Play 运行游戏！", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🤖 AI 交互", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("玩家输入:");
        tester.playerInput = EditorGUILayout.TextArea(tester.playerInput, GUILayout.Height(50));
        
        GUI.backgroundColor = tester.isWaitingResponse ? Color.gray : Color.green;
        if (GUILayout.Button(tester.isWaitingResponse ? "等待 AI 响应..." : "发送消息 (Send)")) tester.SendInputToAI();
        GUI.backgroundColor = Color.white;

        if (!string.IsNullOrEmpty(tester.lastAIReasoning))
        {
            EditorGUILayout.LabelField("AI 反馈:", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(tester.lastAIReasoning, MessageType.None);
        }

        EditorGUILayout.Space(10);

        // 绘制状态
        DrawGameState(tester);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("📜 系统日志", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(tester.statusLog, GUILayout.Height(100));
        
        if (Application.isPlaying) Repaint();
    }

    private void DrawGameState(AIRealIntegrationTester tester)
    {
        // [修改] 分别获取两个 Manager 的数据
        var mindMap = tester.GetMindMapData();
        var phaseMgr = tester.GetPhaseData();

        if (mindMap == null || phaseMgr == null) return;

        EditorGUILayout.LabelField("📊 游戏状态监控", EditorStyles.boldLabel);
        GUI.color = Color.white;

        // ==========================================
        // 1. 阶段流转控制 (Phase Flow) -> 数据来源: PhaseManager
        // ==========================================
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("阶段状态 (Phases):", EditorStyles.boldLabel);

        if (phaseMgr.RunTimePhaseStatusMap != null)
        {
            var phaseList = phaseMgr.RunTimePhaseStatusMap.ToList(); 
            
            foreach (var kvp in phaseList)
            {
                string phaseId = kvp.Key;
                RuntimePhaseStatus status = kvp.Value;

                EditorGUILayout.BeginHorizontal();
                string icon = status == RuntimePhaseStatus.Active ? "▶️" : 
                              status == RuntimePhaseStatus.Completed ? "✅" : 
                              status == RuntimePhaseStatus.Paused ? "⏸️" : "🔒";
                
                if (status == RuntimePhaseStatus.Active) GUI.color = Color.green;
                EditorGUILayout.LabelField($"{icon} {phaseId} ({status})");
                GUI.color = Color.white;

                if (status == RuntimePhaseStatus.Paused)
                {
                    if (GUILayout.Button("切回 (Resume)", GUILayout.Width(100)))
                    {
                        tester.SwitchPhase(phaseId);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(5);

        // B. 显示解锁的新阶段 (强制选择/分支)
        if (tester.pendingPhaseChoices.Count > 0)
        {
            EditorGUILayout.HelpBox("检测到阶段完成！请选择下一步：", MessageType.Warning);
            foreach (var choice in tester.pendingPhaseChoices)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // 橙色按钮
                if (GUILayout.Button($"🚀 进入: {choice.name} ({choice.id})", GUILayout.Height(30)))
                {
                    tester.SwitchPhase(choice.id);
                }
                GUI.backgroundColor = Color.white;
            }
        }
        EditorGUILayout.EndVertical();

        // --- 已发现的选项 (Nodes) ---
        EditorGUILayout.LabelField("已发现的选项 (Nodes):", EditorStyles.boldLabel);
        
        if (mindMap.RunTimeNodeDataMap != null)
        {
            var nodeList = mindMap.RunTimeNodeDataMap.Values.ToList();

            foreach (var node in nodeList)
            {
                if (node.Status == RunTimeNodeStatus.Discovered)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(node.r_NodeData.Basic.Description, GUILayout.Width(200));
                    
                    GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                    if (GUILayout.Button("提交/证明"))
                    {
                        tester.SubmitNodeOption(node.Id);
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
                else if (node.Status == RunTimeNodeStatus.Submitted)
                {
                    EditorGUILayout.LabelField($"✅ {node.r_NodeData.Basic.Description}");
                }
            }
        }

        EditorGUILayout.Space(5);

        // ==========================================
        // 3. 已发现的模板 (Templates) -> 数据来源: MindMapManager
        // ==========================================
        EditorGUILayout.LabelField("已发现的填空 (Templates):", EditorStyles.boldLabel);
        
        if (mindMap.RunTimeTemplateDataMap != null)
        {
            var tmplList = mindMap.RunTimeTemplateDataMap.Values.ToList();

            foreach (var tmpl in tmplList)
            {
                if (tmpl.Status == RunTimeTemplateDataStatus.Discovered)
                {
                    DrawTemplateItem(tester, tmpl);
                }
            }
        }
    }

    private void DrawTemplateItem(AIRealIntegrationTester tester, RuntimeTemplateData tmpl)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"📄 {tmpl.r_TemplateData.RawText}", EditorStyles.wordWrappedLabel);
        
        string templateId = tmpl.Id;
        if (!_templateInputs.ContainsKey(templateId)) _templateInputs[templateId] = "";

        EditorGUILayout.BeginHorizontal();
        _templateInputs[templateId] = EditorGUILayout.TextField(_templateInputs[templateId]);
        
        if (GUILayout.Button("验证", GUILayout.Width(60)))
        {
            tester.SubmitTemplateAnswer(templateId, _templateInputs[templateId]);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("提示：逗号分隔，如: 十五楼,血迹", MessageType.None);
        EditorGUILayout.EndVertical();
    }
}
#endif