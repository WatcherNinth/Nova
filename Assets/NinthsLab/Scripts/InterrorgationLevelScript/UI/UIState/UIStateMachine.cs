using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interrorgation.UI.UIState
{
    public class UIStateMachine<TState> where TState : struct, Enum
    {
        public TState CurrentState { get; private set; }
        
        private readonly Dictionary<TState, List<TState>> _transitions;
        private readonly IUIStateHandler<TState> _handler;

        public event Action<TState, TState> OnStateChanged;

        public UIStateMachine(IUIStateHandler<TState> handler, TState initialState, Dictionary<TState, List<TState>> transitions)
        {
            _handler = handler;
            CurrentState = initialState;
            _transitions = transitions ?? new Dictionary<TState, List<TState>>();
        }

        public bool CanTransitionTo(TState target)
        {
            return _transitions.TryGetValue(CurrentState, out var targets) && targets.Contains(target);
        }

        public bool TryTransitionTo(TState target)
        {
            if (!CanTransitionTo(target))
            {
                Debug.LogWarning($"[UIStateMachine] 非法状态转换: {CurrentState} -> {target}");
                return false;
            }
            PerformTransition(target);
            return true;
        }

        public void ForceSet(TState target)
        {
            PerformTransition(target);
        }

        public void InitializeState(TState state)
        {
            CurrentState = state;
            _handler?.OnStateEnter(state);
        }

        private void PerformTransition(TState target)
        {
            var from = CurrentState;
            if (EqualityComparer<TState>.Default.Equals(from, target))
            {
                // 状态没有实际变化，不触发事件
                return;
            }
            
            CurrentState = target;
            _handler?.OnStateExit(from);
            _handler?.OnStateEnter(target);
            OnStateChanged?.Invoke(from, target);
        }
    }
}