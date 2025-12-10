using System;
using AIEngine.Network;

// 事件委托：定义事件的签名，接收AIResponseData对象
public delegate void OnAIResponseReceived(AIResponseData responseData);

/// <summary>
/// 负责分发AI响应数据的静态事件分发器。
/// 游戏中的各个模块可以通过订阅此事件来接收和处理AI的反馈。
/// </summary>
public static class AIResponseDispatcher
{
    /// <summary>
    /// 当AI响应数据被接收并准备好进行处理时触发的事件。
    /// 订阅者应在此事件中注册它们的处理方法。
    /// </summary>
    public static event OnAIResponseReceived OnResponseReceived;

    /// <summary>
    /// 触发OnResponseReceived事件，将AI响应数据分发给所有订阅者。
    /// 此方法应在主线程中调用，以确保Unity API的线程安全访问。
    /// </summary>
    /// <param name="responseData">要分发的AI响应数据。</param>
    public static void Dispatch(AIResponseData responseData)
    {
        // ?.Invoke() 语法会在事件没有订阅者时安全地不执行任何操作
        OnResponseReceived?.Invoke(responseData);
    }
}