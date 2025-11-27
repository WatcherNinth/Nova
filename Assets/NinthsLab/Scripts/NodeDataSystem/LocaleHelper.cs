    /// <summary>
    /// 本地化辅助类（预留）。
    /// 目前仅直接返回Key，后续可以在此对接LocalizationManager。
    /// </summary>
    public static class LocaleHelper
    {
        public static string GetText(string key)
        {
            // TODO: 这里接入实际的本地化系统
            // return LocalizationManager.Get(key);
            return key;
        }
    }