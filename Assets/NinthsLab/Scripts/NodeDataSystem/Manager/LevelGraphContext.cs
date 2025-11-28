using System.Collections.Generic;
using System.Linq;
using LogicEngine.LevelGraph;
using UnityEngine;

namespace LogicEngine
{
    /// <summary>
    /// 任何持有 LevelGraphData 的管理器都需要实现此接口
    /// </summary>
    public interface ILevelGraphProvider
    {
        /// <summary>
        /// 获取当前的 Graph 数据
        /// </summary>
        LevelGraphData GetLevelGraph();

        /// <summary>
        /// 优先级：数值越大，获取优先级越高。
        /// 例如：LevelTestManager 可以设为 100， 实际游戏的 GameManager 设为 0。
        /// 这样在测试环境下，测试器的数据会覆盖游戏的数据。
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// 全局唯一的访问入口。
    /// 无论是在测试器里，还是在游戏运行时，都通过 LevelGraphContext.CurrentGraph 获取数据。
    /// </summary>
    public static class LevelGraphContext
    {
        // 存储所有已注册的提供者
        private static readonly List<ILevelGraphProvider> _providers = new List<ILevelGraphProvider>();

        /// <summary>
        /// 获取当前最高优先级的 LevelGraphData。
        /// 如果没有提供者或数据为空，返回 null。
        /// </summary>
        public static LevelGraphData CurrentGraph
        {
            get
            {
                if (_providers.Count == 0) return null;

                // 1. 按优先级降序排序 (Priority 大的在前)
                // 2. 找到第一个 GetLevelGraph() 返回不为 null 的数据
                foreach (var provider in _providers.OrderByDescending(p => p.Priority))
                {
                    var graph = provider.GetLevelGraph();
                    if (graph != null)
                    {
                        return graph;
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// 注册提供者 (通常在 OnEnable 调用)
        /// </summary>
        public static void Register(ILevelGraphProvider provider)
        {
            if (!_providers.Contains(provider))
            {
                _providers.Add(provider);
                // Debug.Log($"[LevelGraphContext] Provider Registered: {provider.GetType().Name}, Priority: {provider.Priority}");
            }
        }

        /// <summary>
        /// 注销提供者 (通常在 OnDisable 调用)
        /// </summary>
        public static void Unregister(ILevelGraphProvider provider)
        {
            if (_providers.Contains(provider))
            {
                _providers.Remove(provider);
            }
        }
    }
}