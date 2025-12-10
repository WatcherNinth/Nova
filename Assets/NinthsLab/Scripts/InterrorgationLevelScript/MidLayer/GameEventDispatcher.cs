using System;

namespace Interrorgation.MidLayer
{
    public static class GameEventDispatcher
    {
        /// <summary>
        /// GameEventDispatcher: 玩家输入内容
        /// </summary>
        public static event Action<string> OnPlayerInputString;

        public static void DispatchPlayerInputString(string input)
        {
            OnPlayerInputString?.Invoke(input);
        }
    }
}