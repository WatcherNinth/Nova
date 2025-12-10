using System;

namespace Interrorgation.MidLayer
{
    public static class UIEventDispatcher
    {        
        public static event Action<string> OnPlayerSubmitInput;
        public static void DispatchPlayerSubmitInput(string input)
        {
            OnPlayerSubmitInput?.Invoke(input);
        }
    }
}