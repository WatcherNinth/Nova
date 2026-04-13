using UnityEngine;
using LogicEngine.LevelLogic;

namespace Interrorgation.Test
{
    /// <summary>
    /// 用于在 Inspector 中展示 PlayerMindMapManager 运行时数据的代理脚本
    /// </summary>
    public class MindMapDebugProxy : MonoBehaviour
    {
        public PlayerMindMapManager TargetManager => InterrorgationLevelManager.Instance?.playerMindMapManager;

        [Header("设置")]
        public bool autoRefresh = true;

        void Update()
        {
            // 只是为了在 Inspector 中触发重绘，不需要逻辑
        }
    }
}
