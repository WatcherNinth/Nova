using System;

namespace Interrorgation.UI.UIState
{
    public interface IUIStateHandler<TState> where TState : struct, Enum
    {
        void OnStateEnter(TState state);
        void OnStateExit(TState state);
    }
}