using System.Collections.Generic;
using System.Linq;
using Interrorgation.MidLayer;
using LogicEngine.LevelGraph;
using Newtonsoft.Json.Linq;

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

        public List<string> GetCurrentScopeStack()
        {
            return _scopeStack;
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

            // 2. 如果目标已经在栈里，说明玩家在重复点击，不做改变
            if (_scopeStack.Contains(targetNodeId))
            {
                return;
            }

            // 3. [深度计算] 检查新节点是否是当前栈顶节点的“依赖项”
            // 如果 A 依赖 B，而当前栈顶是 A，玩家点了 B，那么栈应该变成 [A, B]
            // 此处之后要加递归搜索
            string currentTopId = _scopeStack.Last();
            if (IsDependencyRecursive(targetNodeId, currentTopId))
            {
                PushScope(targetNodeId); // 增加深度
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
                        // 注意：这里调用 TryProveNode 和 OnProveSuccess 可能会导致它变为 Submitted，那样下一次循环就会把它弹出
                        bool success = _logicManager.TryProveNode(topId);
                        if (!success)
                        {
                            // 依然搞不定，链条断了，停止回溯
                            break;
                        }
                        // 如果可以证明，执行成功逻辑 (isAutoResolve = true)，状态变成了 Submitted
                        _logicManager.OnProveSuccess(topId, isAutoResolve: true);
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

        /// <summary>
        /// 判断 child 是否是 parent 的直接或间接依赖。只有RequiredTrue才能进，scope只显示必要的依赖
        /// </summary>
        /// <param name="childId"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private bool IsDependencyOf(string childId, string parentId)
        {
            if (!_mindMapManager.TryGetNode(parentId, out var parentNode)) return false;

            return parentNode.r_NodeData.Logic.GeneratedDependencyNodes.Contains(childId);
        }

        /// <summary>
        /// 递归判断 child 是否是 parent 的直接或间接依赖
        /// </summary>
        /// <param name="childId"></param>
        /// <param name="parentId"></param>
        /// <param name="visited">防止循环引用</param>
        /// <returns></returns>
        private bool IsDependencyRecursive(string childId, string parentId, HashSet<string> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<string>();
            }

            if (visited.Contains(parentId)) return false;
            visited.Add(parentId);

            if (!_mindMapManager.TryGetNode(parentId, out var parentNode)) return false;

            if (parentNode.r_NodeData.Logic.GeneratedDependencyNodes.Contains(childId)) return true;

            foreach (var depId in parentNode.r_NodeData.Logic.GeneratedDependencyNodes)
            {
                if (IsDependencyRecursive(childId, depId, visited)) return true;
            }

            return false;
        }
    }
}