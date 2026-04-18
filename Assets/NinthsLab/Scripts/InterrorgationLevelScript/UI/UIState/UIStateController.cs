using System;
using System.Collections.Generic;
using Interrorgation.MidLayer;
using Interrorgation.UI.UISequence;
using UnityEngine;

namespace Interrorgation.UI.UIState
{
    public abstract class UIStateController<TState> : MonoBehaviour, IUIStateHandler<TState>, IUIBacklogModule
        where TState : struct, Enum
    {
        protected UIStateMachine<TState> StateMachine;

        private readonly List<Action> _backlog = new List<Action>();

        /// <summary>
        /// 子类定义：该 UI 是否处于"可响应事件表现"的状态。
        /// 默认实现：枚举值名称包含 "Shown" 或 "Active" 或 "Animating" 视为活跃。
        /// 子类可 override 以自定义判断逻辑。
        /// </summary>
        public virtual bool IsVisualActive { get; protected set; } = false;

        public TState CurrentState => StateMachine.CurrentState;

        /// <summary>
        /// 尝试转换到指定状态
        /// </summary>
        public bool TryTransitionTo(TState target)
        {
            return StateMachine.TryTransitionTo(target);
        }

        /// <summary>
        /// 强制设置状态（忽略转换约束）
        /// </summary>
        public void ForceSet(TState target)
        {
            StateMachine.ForceSet(target);
        }

        /// <summary>
        /// 子类实现：定义状态转换表
        /// </summary>
        protected abstract Dictionary<TState, List<TState>> DefineTransitions();

        /// <summary>
        /// 子类实现：定义初始状态
        /// </summary>
        protected abstract TState InitialState { get; }

        /// <summary>
        /// 子类实现：判断当前状态是否算"视觉活跃"
        /// </summary>
        protected abstract bool CheckIsVisualActive(TState state);

        protected virtual void Awake()
        {
            StateMachine = new UIStateMachine<TState>(this, InitialState, DefineTransitions());
            StateMachine.InitializeState(InitialState);
            IsVisualActive = CheckIsVisualActive(InitialState);
        }

        protected virtual void OnEnable()
        {
            SubscribeEvents();
            StateMachine.OnStateChanged += HandleStateChanged;
        }

        protected virtual void OnDisable()
        {
            UnsubscribeEvents();
            StateMachine.OnStateChanged -= HandleStateChanged;
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        #region IUIStateHandler<TState>
        public virtual void OnStateEnter(TState state) { }
        public virtual void OnStateExit(TState state) { }
        #endregion

        #region IUIBacklogModule
        public void RecordBacklog(Action action)
        {
            _backlog.Add(action);
        }

        public void ProcessBacklog()
        {
            if (_backlog.Count == 0) return;
            Debug.Log($"[{GetType().Name}] 回放积压表现 (Count: {_backlog.Count})");
            foreach (var action in _backlog) action?.Invoke();
            _backlog.Clear();
        }
        #endregion

        #region 积压调度

        /// <summary>
        /// 统一的积压调度方法。
        /// 如果当前视觉活跃，执行 action；否则存入积压。
        /// 无论哪种情况都立即 DispatchActionCompleted(actionId)。
        /// </summary>
        protected void DispatchOrBacklog(Action action, string actionId)
        {
            if (IsVisualActive)
            {
                action();
            }
            else
            {
                RecordBacklog(action);
            }
            UIEventDispatcher.DispatchActionCompleted(actionId);
        }
        #endregion

        protected virtual void HandleStateChanged(TState from, TState to)
        {
            IsVisualActive = CheckIsVisualActive(to);

            // 当从不活跃 -> 活跃时，自动回放积压
            bool fromActive = CheckIsVisualActive(from);
            if (!fromActive && IsVisualActive)
            {
                ProcessBacklog();
            }
        }
    }
}