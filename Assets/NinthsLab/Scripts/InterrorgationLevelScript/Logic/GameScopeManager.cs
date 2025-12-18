using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using LogicEngine.LevelGraph;
using Interrorgation.MidLayer;

namespace LogicEngine.LevelLogic
{
    public class GameScopeManager
    {
        private PlayerMindMapManager _mindMapManager;
        private NodeLogicManager _logicManager;

        // Scope 栈：List 的末尾是栈顶（当前最关注的节点）
        // 例如：[A, B, C] -> 目标是 A，卡在 B，当前正在解决 C
        private List<string> _scopeStack = new List<string>();

        public GameScopeManager(PlayerMindMapManager mindMapManager)
        {
            _mindMapManager = mindMapManager;
        }

        // 需要注入 LogicManager 以便调用证明逻辑
        public void SetLogicManager(NodeLogicManager logicManager)
        {
            _logicManager = logicManager;
        }

        public string GetCurrentScopeNode()
        {
            return _scopeStack.Count > 0 ? _scopeStack.Last() : null;
        }

        /// <summary>
        /// 当尝试证明节点失败时调用。
        /// 逻辑：判断是增加深度，还是切换目标。
        /// </summary>
        public void UpdateScopeOnFail(string targetNodeId)
        {
            // 1. 如果栈为空，直接入栈
            if (_scopeStack.Count == 0)
            {
                PushScope(targetNodeId);
                return;
            }

            // 2. 如果目标已经在栈里，说明玩家在重复点击，不做改变，或者截断到该位置
            if (_scopeStack.Contains(targetNodeId))
            {
                // 譬如 [A, B, C]，玩家点了 B，这说明玩家放弃了 C，想回到 B 的层面
                // 此时应该把 C 弹出，变成 [A, B]
                while (_scopeStack.Last() != targetNodeId)
                {
                    _scopeStack.RemoveAt(_scopeStack.Count - 1);
                }
                NotifyScopeChanged();
                return;
            }

            // 3. [深度计算] 检查新节点是否是当前栈顶节点的“依赖项”
            // 如果 A 依赖 B，而当前栈顶是 A，玩家点了 B，那么栈应该变成 [A, B]
            string currentTopId = _scopeStack.Last();
            if (IsDependencyOf(targetNodeId, currentTopId))
            {
                PushScope(targetNodeId); // 增加深度
            }
            else
            {
                // 4. 如果没关系，说明玩家换了个毫不相干的目标，重置栈
                // 比如当前是 [A, B]，玩家点了 D (和A,B没关系)
                _scopeStack.Clear();
                PushScope(targetNodeId); // 切换目标
            }
        }

        /// <summary>
        /// 当某个节点成功证明后调用。
        /// 逻辑：尝试回溯结算栈中的节点（连锁反应）。
        /// </summary>
        public void ResolveScopeChain(string provenNodeId)
        {
            // 如果栈是空的，没啥好结算的
            if (_scopeStack.Count == 0) return;

            // 1. 如果证明的节点不在栈里，也不影响栈（除非它是栈里某节点的依赖，被自动验证处理了）
            // 这里我们主要处理栈内的连锁。
            
            // 我们从栈顶（最深处）开始检查，看能不能往回缩
            // 只要栈顶的节点变成 Submitted 了，就把它弹出去，继续看下一个
            bool changed = false;
            
            // 循环检查栈顶
            while (_scopeStack.Count > 0)
            {
                string topId = _scopeStack.Last();
                
                // 获取节点状态
                if (_mindMapManager.TryGetNode(topId, out var node))
                {
                    if (node.Status == RunTimeNodeStatus.Submitted)
                    {
                        // 栈顶已经搞定了，弹出
                        _scopeStack.RemoveAt(_scopeStack.Count - 1);
                        changed = true;
                        continue; 
                    }
                    else
                    {
                        // 栈顶还没搞定，尝试利用新的局势去证明它
                        // (这会触发递归，但 TryProveNode 内部有防死循环机制)
                        // 注意：这里调用 TryProveNode 可能会导致它变为 Submitted，那样下一次循环就会把它弹出
                        bool success = _logicManager.TryProveNode(topId, isAutoResolve: true);
                        if (!success)
                        {
                            // 依然搞不定，链条断了，停止回溯
                            break;
                        }
                        // 如果 success 为 true，状态变成了 Submitted，下一次循环会处理弹出
                    }
                }
                else
                {
                    // 异常数据，移除
                    _scopeStack.RemoveAt(_scopeStack.Count - 1);
                    changed = true;
                }
            }

            if (changed)
            {
                NotifyScopeChanged();
            }
        }

        private void PushScope(string nodeId)
        {
            _scopeStack.Add(nodeId);
            NotifyScopeChanged();
        }

        private void NotifyScopeChanged()
        {
            // 发送整个栈给 UI，方便显示面包屑导航 (A > B > C)
            GameEventDispatcher.DispatchScopeStackChanged(new List<string>(_scopeStack));
        }

        // --- 辅助：判断 child 是否是 parent 的直接或间接依赖 ---
        private bool IsDependencyOf(string childId, string parentId)
        {
            if (!_mindMapManager.TryGetNode(parentId, out var parentNode)) return false;
            
            // 这里只做简单的直接依赖检查，或者一层 BFS
            // 也可以解析 Logic.DependsOn 的 JSON 结构
            // 为了简化，我们假设 logicManager 提供了解析服务，或者我们简单遍历 JSON
            
            var dependsOn = parentNode.r_NodeData.Logic?.DependsOn;
            if (dependsOn == null) return false;

            string jsonStr = dependsOn.ToString();
            // 这是一个比较粗暴但有效的判断：如果依赖 JSON 字符串里包含了 childId，就认为是依赖
            // 精确做法是递归解析 JToken，但在 ID 命名规范的情况下，字符串包含通常足够
            return jsonStr.Contains(childId);
        }
    }
}