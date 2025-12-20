#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using LogicEngine.LevelLogic;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(AIRealIntegrationTester))]
public class AIRealIntegrationTesterEditor : Editor
{
    private Dictionary<string, string> _templateInputs = new Dictionary<string, string>();
    private bool _showAllNodes = false;

    public override void OnInspectorGUI()
    {
        AIRealIntegrationTester tester = (AIRealIntegrationTester)target;

        // --- 顶部控制 ---
        EditorGUILayout.LabelField("🎮 游戏控制台", EditorStyles.largeLabel);
        GUI.enabled = !Application.isPlaying;
        tester.targetFileName = EditorGUILayout.TextField("剧本文件", tester.targetFileName);
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("加载/重置游戏")) tester.InitializeGame();

        if (!Application.isPlaying) return;

        GUILayout.Space(10);

        // --- 1. Scope 监控 ---
        DrawScopeSection(tester);

        // --- 2. Phase 并行切换监控 ---
        DrawPhaseSection(tester);

        // --- 3. AI 交互 ---
        DrawInputSection(tester);

        // --- 4. 节点与填空 ---
        DrawNodeSection(tester);
        DrawTemplateSection(tester);

        // --- 5. Log ---
        GUILayout.Space(10);
        EditorGUILayout.LabelField("📜 日志", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(tester.statusLog, GUILayout.Height(150));

        if (Application.isPlaying) Repaint();
    }

    private void DrawScopeSection(AIRealIntegrationTester tester)
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("🔍 Scope (关注深度):", EditorStyles.boldLabel);
        
        if (tester.currentScopeStack != null && tester.currentScopeStack.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var scopeId in tester.currentScopeStack)
            {
                if (GUILayout.Button(scopeId, EditorStyles.miniButton)) { }
                GUILayout.Label(">", GUILayout.Width(10));
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField("🟢 全局 (Global)", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPhaseSection(AIRealIntegrationTester tester)
    {
        var phaseMgr = tester.GetPhaseManager();
        if (phaseMgr == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("📅 阶段管理 (并行切换测试):", EditorStyles.boldLabel);

        // 获取所有阶段状态的快照
        var phaseList = phaseMgr.RunTimePhaseStatusMap.ToList(); 

        foreach (var kvp in phaseList)
        {
            string phaseId = kvp.Key;
            RuntimePhaseStatus status = kvp.Value;

            if (status == RuntimePhaseStatus.Locked) continue;

            EditorGUILayout.BeginHorizontal();
            
            if (status == RuntimePhaseStatus.Active)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"▶️ {phaseId} (进行中)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else if (status == RuntimePhaseStatus.Paused)
            {
                GUI.color = new Color(0.6f, 0.8f, 1f); // 浅蓝
                EditorGUILayout.LabelField($"⏸️ {phaseId} (已暂停)");
                GUI.color = Color.white;
                
                // [核心验证点] 允许随时切回已暂停的阶段
                if (GUILayout.Button("切换至此 (Switch)", GUILayout.Width(120)))
                {
                    tester.SwitchPhase(phaseId);
                }
            }
            else if (status == RuntimePhaseStatus.Completed)
            {
                EditorGUILayout.LabelField($"✅ {phaseId} (已完成)");
                
                // [新增] 允许切换到已完成的阶段（用于测试并行路径）
                if (GUILayout.Button("重新进入 (Reenter)", GUILayout.Width(120)))
                {
                    tester.SwitchPhase(phaseId);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        // 显示新解锁的阶段 (Pending)
        if (tester.pendingPhaseChoices.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("✨ 新阶段已解锁！你可以开启新线：", MessageType.Warning);
            // [修复] 创建快照，避免在迭代中修改集合
            var pendingCopy = new List<(string id, string name)>(tester.pendingPhaseChoices);
            foreach (var choice in pendingCopy)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // 橙色
                if (GUILayout.Button($"🚀 开启: {choice.name} ({choice.id})"))
                {
                    tester.SwitchPhase(choice.id);
                }
                GUI.backgroundColor = Color.white;
            }
        }

        // [新增] 快速切换面板：显示所有可切换的阶段
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("⚡ 快速切换 (所有可用目标):", EditorStyles.boldLabel);
        var switchTargets = phaseMgr.GetAvailableSwitchTargets();
        if (switchTargets.Count > 0)
        {
            EditorGUILayout.BeginHorizontal("helpbox");
            foreach (var target in switchTargets)
            {
                string statusIcon = target.status switch
                {
                    "New" => "✨",
                    "Paused" => "⏸️",
                    _ => "❓"
                };
                
                if (GUILayout.Button($"{statusIcon} {target.id}", GUILayout.Width(150)))
                {
                    tester.SwitchPhase(target.id);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("无可切换的目标（所有阶段都已解锁或进行中）", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawInputSection(AIRealIntegrationTester tester)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("💬 AI 输入", EditorStyles.boldLabel);
        tester.playerInput = EditorGUILayout.TextArea(tester.playerInput, GUILayout.Height(40));
        
        GUI.backgroundColor = tester.isWaitingResponse ? Color.gray : new Color(0.4f, 1f, 0.4f);
        if (GUILayout.Button(tester.isWaitingResponse ? "发送中..." : "发送 (Send)", GUILayout.Height(30)))
        {
            tester.SendInputToAI();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawNodeSection(AIRealIntegrationTester tester)
    {
        var mindMap = tester.GetMindMap();
        if (mindMap == null) return;

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🧩 节点列表:", EditorStyles.boldLabel);
        _showAllNodes = EditorGUILayout.ToggleLeft("显示未发现", _showAllNodes, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical("box");
        var nodeList = mindMap.RunTimeNodeDataMap.Values.ToList();

        foreach (var node in nodeList)
        {
            if (node.Status == RunTimeNodeStatus.Hidden && !_showAllNodes) continue;

            EditorGUILayout.BeginHorizontal();
            if (node.Status == RunTimeNodeStatus.Submitted)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✅", GUILayout.Width(20));
                EditorGUILayout.LabelField(node.r_NodeData.Basic.Description);
                GUI.color = Color.white;
            }
            else if (node.IsInvalidated)
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("❌", GUILayout.Width(20));
                EditorGUILayout.LabelField(node.r_NodeData.Basic.Description + " (失效)");
                GUI.color = Color.white;
            }
            else if (node.Status == RunTimeNodeStatus.Discovered)
            {
                EditorGUILayout.LabelField("⚪", GUILayout.Width(20));
                EditorGUILayout.LabelField(node.r_NodeData.Basic.Description, GUILayout.Width(200));
                if (GUILayout.Button("提交")) tester.SubmitNodeOption(node.Id);
            }
            else // Hidden
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("🔒 " + node.Id);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawTemplateSection(AIRealIntegrationTester tester)
    {
        var mindMap = tester.GetMindMap();
        if (mindMap == null) return;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("📝 填空题:", EditorStyles.boldLabel);
        var tmplList = mindMap.RunTimeTemplateDataMap.Values.ToList();

        foreach (var tmpl in tmplList)
        {
            if (tmpl.Status == RunTimeTemplateDataStatus.Discovered)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField(tmpl.r_TemplateData.RawText, EditorStyles.wordWrappedLabel);
                
                string id = tmpl.Id;
                if (!_templateInputs.ContainsKey(id)) _templateInputs[id] = "";

                EditorGUILayout.BeginHorizontal();
                _templateInputs[id] = EditorGUILayout.TextField(_templateInputs[id]);
                if (GUILayout.Button("验证", GUILayout.Width(50)))
                {
                    tester.SubmitTemplateAnswer(id, _templateInputs[id]);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif